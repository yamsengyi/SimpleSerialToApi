using System.Configuration;

namespace SimpleSerialToApi.Configuration
{
    /// <summary>
    /// Configuration section for API mappings
    /// </summary>
    public class ApiMappingSection : ConfigurationSection
    {
        [ConfigurationProperty("endpoints", IsDefaultCollection = false)]
        public ApiEndpointElementCollection Endpoints
        {
            get { return (ApiEndpointElementCollection)this["endpoints"]; }
        }

        [ConfigurationProperty("mappingRules", IsDefaultCollection = false)]
        public MappingRuleElementCollection MappingRules
        {
            get { return (MappingRuleElementCollection)this["mappingRules"]; }
        }
    }

    /// <summary>
    /// Configuration section for parsing rules
    /// </summary>
    public class ParsingRulesSection : ConfigurationSection
    {
        [ConfigurationProperty("rules", IsDefaultCollection = false)]
        public ParsingRuleElementCollection Rules
        {
            get { return (ParsingRuleElementCollection)this["rules"]; }
        }

        [ConfigurationProperty("converters", IsDefaultCollection = false)]
        public ConverterElementCollection Converters
        {
            get { return (ConverterElementCollection)this["converters"]; }
        }
    }

    /// <summary>
    /// Configuration section for message queue settings
    /// </summary>
    public class MessageQueueSection : ConfigurationSection
    {
        [ConfigurationProperty("maxQueueSize", DefaultValue = 1000)]
        public int MaxQueueSize
        {
            get { return (int)this["maxQueueSize"]; }
            set { this["maxQueueSize"] = value; }
        }

        [ConfigurationProperty("batchSize", DefaultValue = 10)]
        public int BatchSize
        {
            get { return (int)this["batchSize"]; }
            set { this["batchSize"] = value; }
        }

        [ConfigurationProperty("retryCount", DefaultValue = 3)]
        public int RetryCount
        {
            get { return (int)this["retryCount"]; }
            set { this["retryCount"] = value; }
        }

        [ConfigurationProperty("retryInterval", DefaultValue = 5000)]
        public int RetryInterval
        {
            get { return (int)this["retryInterval"]; }
            set { this["retryInterval"] = value; }
        }
    }

    /// <summary>
    /// Collection of API endpoint configuration elements
    /// </summary>
    public class ApiEndpointElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ApiEndpointElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ApiEndpointElement)element).Name;
        }

        public ApiEndpointElement this[int index]
        {
            get { return (ApiEndpointElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public ApiEndpointElement this[string name]
        {
            get { return (ApiEndpointElement)BaseGet(name); }
        }
    }

    /// <summary>
    /// Collection of mapping rule configuration elements
    /// </summary>
    public class MappingRuleElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MappingRuleElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MappingRuleElement)element).SourceField;
        }

        public MappingRuleElement this[int index]
        {
            get { return (MappingRuleElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public MappingRuleElement this[string sourceField]
        {
            get { return (MappingRuleElement)BaseGet(sourceField); }
        }
    }

    /// <summary>
    /// Configuration element for API endpoints
    /// </summary>
    public class ApiEndpointElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("url", IsRequired = true)]
        public string Url
        {
            get { return (string)this["url"]; }
            set { this["url"] = value; }
        }

        [ConfigurationProperty("method", DefaultValue = "POST")]
        public string Method
        {
            get { return (string)this["method"]; }
            set { this["method"] = value; }
        }

        [ConfigurationProperty("authType", DefaultValue = "")]
        public string AuthType
        {
            get { return (string)this["authType"]; }
            set { this["authType"] = value; }
        }

        [ConfigurationProperty("authToken", DefaultValue = "")]
        public string AuthToken
        {
            get { return (string)this["authToken"]; }
            set { this["authToken"] = value; }
        }

        [ConfigurationProperty("timeout", DefaultValue = 30000)]
        public int Timeout
        {
            get { return (int)this["timeout"]; }
            set { this["timeout"] = value; }
        }
    }

    /// <summary>
    /// Configuration element for mapping rules
    /// </summary>
    public class MappingRuleElement : ConfigurationElement
    {
        [ConfigurationProperty("sourceField", IsRequired = true, IsKey = true)]
        public string SourceField
        {
            get { return (string)this["sourceField"]; }
            set { this["sourceField"] = value; }
        }

        [ConfigurationProperty("targetField", IsRequired = true)]
        public string TargetField
        {
            get { return (string)this["targetField"]; }
            set { this["targetField"] = value; }
        }

        [ConfigurationProperty("dataType", DefaultValue = "string")]
        public string DataType
        {
            get { return (string)this["dataType"]; }
            set { this["dataType"] = value; }
        }

        [ConfigurationProperty("converter", DefaultValue = "")]
        public string Converter
        {
            get { return (string)this["converter"]; }
            set { this["converter"] = value; }
        }

        [ConfigurationProperty("defaultValue", DefaultValue = "")]
        public string DefaultValue
        {
            get { return (string)this["defaultValue"]; }
            set { this["defaultValue"] = value; }
        }

        [ConfigurationProperty("isRequired", DefaultValue = false)]
        public bool IsRequired
        {
            get { return (bool)this["isRequired"]; }
            set { this["isRequired"] = value; }
        }
    }

    /// <summary>
    /// Collection of parsing rule configuration elements
    /// </summary>
    public class ParsingRuleElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ParsingRuleElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ParsingRuleElement)element).Name;
        }

        public ParsingRuleElement this[int index]
        {
            get { return (ParsingRuleElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public ParsingRuleElement this[string name]
        {
            get { return (ParsingRuleElement)BaseGet(name); }
        }
    }

    /// <summary>
    /// Collection of converter configuration elements
    /// </summary>
    public class ConverterElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConverterElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConverterElement)element).Name;
        }

        public ConverterElement this[int index]
        {
            get { return (ConverterElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public ConverterElement this[string name]
        {
            get { return (ConverterElement)BaseGet(name); }
        }
    }

    /// <summary>
    /// Configuration element for parsing rules
    /// </summary>
    public class ParsingRuleElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("pattern", IsRequired = true)]
        public string Pattern
        {
            get { return (string)this["pattern"]; }
            set { this["pattern"] = value; }
        }

        [ConfigurationProperty("fields", IsRequired = true)]
        public string Fields
        {
            get { return (string)this["fields"]; }
            set { this["fields"] = value; }
        }

        [ConfigurationProperty("dataTypes", IsRequired = true)]
        public string DataTypes
        {
            get { return (string)this["dataTypes"]; }
            set { this["dataTypes"] = value; }
        }

        [ConfigurationProperty("dataFormat", DefaultValue = "TEXT")]
        public string DataFormat
        {
            get { return (string)this["dataFormat"]; }
            set { this["dataFormat"] = value; }
        }

        [ConfigurationProperty("priority", DefaultValue = 1)]
        public int Priority
        {
            get { return (int)this["priority"]; }
            set { this["priority"] = value; }
        }
    }

    /// <summary>
    /// Configuration element for converters
    /// </summary>
    public class ConverterElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("sourceUnit", DefaultValue = "")]
        public string SourceUnit
        {
            get { return (string)this["sourceUnit"]; }
            set { this["sourceUnit"] = value; }
        }

        [ConfigurationProperty("targetUnit", DefaultValue = "")]
        public string TargetUnit
        {
            get { return (string)this["targetUnit"]; }
            set { this["targetUnit"] = value; }
        }

        [ConfigurationProperty("formula", DefaultValue = "")]
        public string Formula
        {
            get { return (string)this["formula"]; }
            set { this["formula"] = value; }
        }
    }
}