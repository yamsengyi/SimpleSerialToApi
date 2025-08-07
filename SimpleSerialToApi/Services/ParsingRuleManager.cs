using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Configuration;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Manager for parsing rules with configuration support
    /// </summary>
    public class ParsingRuleManager : IParsingRuleManager
    {
        private readonly ILogger<ParsingRuleManager> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly List<ParsingRule> _rules;
        private readonly object _rulesLock = new object();

        public ParsingRuleManager(ILogger<ParsingRuleManager> logger, IConfigurationService configurationService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _rules = new List<ParsingRule>();

            LoadRulesFromConfiguration();
        }

        /// <summary>
        /// Get all parsing rules
        /// </summary>
        public List<ParsingRule> GetAllRules()
        {
            lock (_rulesLock)
            {
                return new List<ParsingRule>(_rules);
            }
        }

        /// <summary>
        /// Get parsing rules for a specific data format
        /// </summary>
        public List<ParsingRule> GetRulesForFormat(string dataFormat)
        {
            lock (_rulesLock)
            {
                return _rules.Where(r => string.Equals(r.DataFormat, dataFormat, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(r => r.Priority)
                            .ToList();
            }
        }

        /// <summary>
        /// Find the best matching rule for raw data
        /// </summary>
        public ParsingRule? FindMatchingRule(RawSerialData rawData)
        {
            if (rawData?.Data == null || rawData.Data.Length == 0)
            {
                return null;
            }

            // Get rules for the data format
            var candidateRules = !string.IsNullOrEmpty(rawData.DataFormat) ? 
                GetRulesForFormat(rawData.DataFormat) : 
                GetAllRules().OrderByDescending(r => r.Priority).ToList();

            if (candidateRules.Count == 0)
            {
                _logger.LogWarning("No parsing rules found for data format '{DataFormat}'", rawData.DataFormat);
                return null;
            }

            // For text data, try pattern matching
            if (string.Equals(rawData.DataFormat, "TEXT", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(rawData.DataFormat))
            {
                return FindTextMatchingRule(rawData, candidateRules);
            }

            // For other formats, return the highest priority rule
            var bestRule = candidateRules.FirstOrDefault();
            if (bestRule != null)
            {
                _logger.LogDebug("Selected parsing rule '{RuleName}' for {DataFormat} data", 
                    bestRule.Name, rawData.DataFormat);
            }

            return bestRule;
        }

        /// <summary>
        /// Add a new parsing rule
        /// </summary>
        public bool AddRule(ParsingRule rule)
        {
            if (rule == null)
            {
                _logger.LogWarning("Cannot add null parsing rule");
                return false;
            }

            lock (_rulesLock)
            {
                // Check if rule with same name already exists
                var existingRule = _rules.FirstOrDefault(r => r.Name == rule.Name);
                if (existingRule != null)
                {
                    _logger.LogWarning("Parsing rule with name '{RuleName}' already exists", rule.Name);
                    return false;
                }

                _rules.Add(rule);
                _rules.Sort((r1, r2) => r2.Priority.CompareTo(r1.Priority)); // Sort by priority descending

                _logger.LogInformation("Added parsing rule '{RuleName}' with priority {Priority}", 
                    rule.Name, rule.Priority);
                return true;
            }
        }

        /// <summary>
        /// Update an existing parsing rule
        /// </summary>
        public bool UpdateRule(string ruleName, ParsingRule rule)
        {
            if (string.IsNullOrWhiteSpace(ruleName) || rule == null)
            {
                return false;
            }

            lock (_rulesLock)
            {
                var index = _rules.FindIndex(r => r.Name == ruleName);
                if (index >= 0)
                {
                    _rules[index] = rule;
                    _rules.Sort((r1, r2) => r2.Priority.CompareTo(r1.Priority)); // Re-sort by priority

                    _logger.LogInformation("Updated parsing rule '{RuleName}'", ruleName);
                    return true;
                }

                _logger.LogWarning("Parsing rule '{RuleName}' not found for update", ruleName);
                return false;
            }
        }

        /// <summary>
        /// Remove a parsing rule
        /// </summary>
        public bool RemoveRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                return false;
            }

            lock (_rulesLock)
            {
                var removedCount = _rules.RemoveAll(r => r.Name == ruleName);
                if (removedCount > 0)
                {
                    _logger.LogInformation("Removed parsing rule '{RuleName}'", ruleName);
                    return true;
                }

                _logger.LogWarning("Parsing rule '{RuleName}' not found for removal", ruleName);
                return false;
            }
        }

        /// <summary>
        /// Validate all parsing rules
        /// </summary>
        public ValidationResult ValidateAllRules()
        {
            var result = new ValidationResult { IsValid = true };
            var ruleNames = new HashSet<string>();

            lock (_rulesLock)
            {
                foreach (var rule in _rules)
                {
                    // Check for duplicate names
                    if (!ruleNames.Add(rule.Name))
                    {
                        result.AddError($"Duplicate parsing rule name: {rule.Name}");
                    }

                    // Validate individual rule
                    if (string.IsNullOrWhiteSpace(rule.Name))
                    {
                        result.AddError("Parsing rule name cannot be empty");
                    }

                    if (string.IsNullOrWhiteSpace(rule.Pattern))
                    {
                        result.AddError($"Pattern is required for rule '{rule.Name}'");
                    }

                    if (rule.Fields.Count == 0)
                    {
                        result.AddError($"At least one field must be defined for rule '{rule.Name}'");
                    }

                    if (rule.Fields.Count != rule.DataTypes.Count)
                    {
                        result.AddError($"Field count ({rule.Fields.Count}) does not match data type count ({rule.DataTypes.Count}) for rule '{rule.Name}'");
                    }

                    // Validate data format
                    var validFormats = new[] { "TEXT", "HEX", "JSON", "BINARY" };
                    if (!Array.Exists(validFormats, f => string.Equals(f, rule.DataFormat, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.AddError($"Invalid data format '{rule.DataFormat}' for rule '{rule.Name}'");
                    }
                }
            }

            if (result.IsValid)
            {
                _logger.LogInformation("All parsing rules validation passed");
            }
            else
            {
                _logger.LogWarning("Parsing rules validation failed with {ErrorCount} errors", result.Errors.Count);
            }

            return result;
        }

        /// <summary>
        /// Reload rules from configuration
        /// </summary>
        public void ReloadRules()
        {
            lock (_rulesLock)
            {
                _rules.Clear();
                LoadRulesFromConfiguration();
                _logger.LogInformation("Parsing rules reloaded from configuration");
            }
        }

        /// <summary>
        /// Load parsing rules from configuration
        /// </summary>
        private void LoadRulesFromConfiguration()
        {
            try
            {
                var parsingRulesSection = _configurationService.GetSection<ParsingRulesSection>("parsingRules");
                if (parsingRulesSection?.Rules == null)
                {
                    _logger.LogWarning("No parsing rules section found in configuration");
                    return;
                }

                int loadedCount = 0;
                foreach (ParsingRuleElement ruleElement in parsingRulesSection.Rules)
                {
                    var rule = new ParsingRule
                    {
                        Name = ruleElement.Name,
                        Pattern = ruleElement.Pattern,
                        Fields = ruleElement.Fields.Split(',').Select(f => f.Trim()).ToList(),
                        DataTypes = ruleElement.DataTypes.Split(',').Select(dt => dt.Trim()).ToList(),
                        DataFormat = ruleElement.DataFormat,
                        Priority = ruleElement.Priority
                    };

                    _rules.Add(rule);
                    loadedCount++;
                }

                // Sort by priority descending
                _rules.Sort((r1, r2) => r2.Priority.CompareTo(r1.Priority));

                _logger.LogInformation("Loaded {Count} parsing rules from configuration", loadedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parsing rules from configuration");
            }
        }

        /// <summary>
        /// Find matching rule for text data using pattern matching
        /// </summary>
        private ParsingRule? FindTextMatchingRule(RawSerialData rawData, List<ParsingRule> candidateRules)
        {
            try
            {
                var textData = Encoding.UTF8.GetString(rawData.Data).Trim();
                if (string.IsNullOrEmpty(textData))
                {
                    return null;
                }

                foreach (var rule in candidateRules)
                {
                    try
                    {
                        var regex = new System.Text.RegularExpressions.Regex(rule.Pattern, 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (regex.IsMatch(textData))
                        {
                            _logger.LogDebug("Text pattern '{Pattern}' matched for rule '{RuleName}'", 
                                rule.Pattern, rule.Name);
                            return rule;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Invalid regex pattern in rule '{RuleName}': {Pattern}", 
                            rule.Name, rule.Pattern);
                    }
                }

                _logger.LogDebug("No matching text pattern found for data: '{TextData}'", textData);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matching text rule");
                return null;
            }
        }
    }
}