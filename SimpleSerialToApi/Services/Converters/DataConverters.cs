using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Globalization;
using System.Linq;

namespace SimpleSerialToApi.Services.Converters
{
    /// <summary>
    /// Converter for temperature units
    /// </summary>
    public class TemperatureConverter : IDataConverter
    {
        public string Name => "TemperatureConverter";

        public Type[] SupportedTypes => new[] { typeof(double), typeof(decimal), typeof(float), typeof(int) };

        public object Convert(object input, ConversionContext context)
        {
            if (input == null)
            {
                return 0.0;
            }

            // Convert input to double
            double temperature;
            try
            {
                temperature = System.Convert.ToDouble(input, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return input; // Return original if conversion fails
            }

            // Get conversion parameters
            var sourceUnit = GetParameter(context, "sourceUnit", "celsius").ToLowerInvariant();
            var targetUnit = GetParameter(context, "targetUnit", "celsius").ToLowerInvariant();

            if (sourceUnit == targetUnit)
            {
                return temperature; // No conversion needed
            }

            // Convert to celsius first (standard unit)
            double celsius = sourceUnit switch
            {
                "fahrenheit" or "f" => (temperature - 32) * 5.0 / 9.0,
                "kelvin" or "k" => temperature - 273.15,
                "rankine" or "r" => (temperature - 491.67) * 5.0 / 9.0,
                _ => temperature // Assume celsius
            };

            // Convert from celsius to target unit
            double result = targetUnit switch
            {
                "fahrenheit" or "f" => (celsius * 9.0 / 5.0) + 32,
                "kelvin" or "k" => celsius + 273.15,
                "rankine" or "r" => (celsius + 273.15) * 9.0 / 5.0,
                _ => celsius // Default to celsius
            };

            return result;
        }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            return IsNumericType(sourceType) && IsNumericType(targetType);
        }

        public ValidationResult ValidateParameters(ConversionContext context)
        {
            var result = new ValidationResult { IsValid = true };

            var sourceUnit = GetParameter(context, "sourceUnit", "celsius").ToLowerInvariant();
            var targetUnit = GetParameter(context, "targetUnit", "celsius").ToLowerInvariant();

            var validUnits = new[] { "celsius", "c", "fahrenheit", "f", "kelvin", "k", "rankine", "r" };

            if (!Array.Exists(validUnits, u => u == sourceUnit))
            {
                result.AddError($"Invalid source temperature unit: {sourceUnit}");
            }

            if (!Array.Exists(validUnits, u => u == targetUnit))
            {
                result.AddError($"Invalid target temperature unit: {targetUnit}");
            }

            return result;
        }

        private string GetParameter(ConversionContext context, string key, string defaultValue)
        {
            return context.Parameters.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(double) || type == typeof(decimal) || 
                   type == typeof(float) || type == typeof(int) ||
                   type == typeof(long) || type == typeof(short);
        }
    }

    /// <summary>
    /// Converter for date/time formats
    /// </summary>
    public class DateTimeConverter : IDataConverter
    {
        public string Name => "DateTimeConverter";

        public Type[] SupportedTypes => new[] { typeof(DateTime), typeof(string), typeof(long), typeof(int) };

        public object Convert(object input, ConversionContext context)
        {
            if (input == null)
            {
                return DateTime.MinValue;
            }

            var sourceFormat = GetParameter(context, "sourceFormat", "string");
            var targetFormat = GetParameter(context, "targetFormat", "string");
            var inputFormat = GetParameter(context, "inputFormat", "yyyy-MM-dd HH:mm:ss");

            try
            {
                DateTime dateTime;

                // Parse input based on source format
                switch (sourceFormat.ToLowerInvariant())
                {
                    case "unix":
                    case "timestamp":
                        var timestamp = System.Convert.ToInt64(input);
                        dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                        break;
                    case "unixmilli":
                        var timestampMilli = System.Convert.ToInt64(input);
                        dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilli).DateTime;
                        break;
                    case "string":
                    default:
                        var inputString = input.ToString() ?? string.Empty;
                        dateTime = string.IsNullOrEmpty(inputFormat) ? 
                            DateTime.Parse(inputString, CultureInfo.InvariantCulture) :
                            DateTime.ParseExact(inputString, inputFormat, CultureInfo.InvariantCulture);
                        break;
                }

                // Convert to target format
                return targetFormat.ToLowerInvariant() switch
                {
                    "unix" or "timestamp" => ((DateTimeOffset)dateTime).ToUnixTimeSeconds(),
                    "unixmilli" => ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds(),
                    "iso8601" => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    _ => dateTime.ToString(GetParameter(context, "outputFormat", "yyyy-MM-dd HH:mm:ss"), CultureInfo.InvariantCulture)
                };
            }
            catch (Exception)
            {
                return input; // Return original if conversion fails
            }
        }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            var supportedTypes = new[] { typeof(DateTime), typeof(string), typeof(long), typeof(int) };
            return Array.Exists(supportedTypes, t => t == sourceType || t.IsAssignableFrom(sourceType)) &&
                   Array.Exists(supportedTypes, t => t == targetType || t.IsAssignableFrom(targetType));
        }

        public ValidationResult ValidateParameters(ConversionContext context)
        {
            var result = new ValidationResult { IsValid = true };

            var sourceFormat = GetParameter(context, "sourceFormat", "string").ToLowerInvariant();
            var targetFormat = GetParameter(context, "targetFormat", "string").ToLowerInvariant();
            var inputFormat = GetParameter(context, "inputFormat", "");

            var validFormats = new[] { "string", "unix", "timestamp", "unixmilli", "iso8601" };

            if (!Array.Exists(validFormats, f => f == sourceFormat))
            {
                result.AddError($"Invalid source date format: {sourceFormat}");
            }

            if (!Array.Exists(validFormats, f => f == targetFormat))
            {
                result.AddError($"Invalid target date format: {targetFormat}");
            }

            // Validate input format if specified
            if (!string.IsNullOrEmpty(inputFormat) && sourceFormat == "string")
            {
                try
                {
                    DateTime.Now.ToString(inputFormat, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    result.AddError($"Invalid input date format: {inputFormat}");
                }
            }

            return result;
        }

        private string GetParameter(ConversionContext context, string key, string defaultValue)
        {
            return context.Parameters.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    /// <summary>
    /// Converter for numeric formats and scales
    /// </summary>
    public class NumericConverter : IDataConverter
    {
        public string Name => "NumericConverter";

        public Type[] SupportedTypes => new[] { typeof(double), typeof(decimal), typeof(float), typeof(int), typeof(long), typeof(string) };

        public object Convert(object input, ConversionContext context)
        {
            if (input == null)
            {
                return 0;
            }

            try
            {
                // Convert input to double first
                double numericValue = System.Convert.ToDouble(input, CultureInfo.InvariantCulture);

                // Apply scale factor
                var scaleStr = GetParameter(context, "scale", "1");
                if (double.TryParse(scaleStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale))
                {
                    numericValue *= scale;
                }

                // Apply offset
                var offsetStr = GetParameter(context, "offset", "0");
                if (double.TryParse(offsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
                {
                    numericValue += offset;
                }

                // Apply precision (decimal places)
                var precisionStr = GetParameter(context, "precision", "");
                if (int.TryParse(precisionStr, out var precision) && precision >= 0)
                {
                    numericValue = Math.Round(numericValue, precision);
                }

                // Convert to target type
                var targetType = GetParameter(context, "targetType", "double").ToLowerInvariant();
                return targetType switch
                {
                    "int" or "integer" => System.Convert.ToInt32(numericValue),
                    "long" => System.Convert.ToInt64(numericValue),
                    "float" => System.Convert.ToSingle(numericValue),
                    "decimal" => System.Convert.ToDecimal(numericValue),
                    "string" => numericValue.ToString(GetParameter(context, "format", "F2"), CultureInfo.InvariantCulture),
                    _ => numericValue // Default to double
                };
            }
            catch (Exception)
            {
                return input; // Return original if conversion fails
            }
        }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            var numericTypes = new[] { typeof(double), typeof(decimal), typeof(float), 
                                     typeof(int), typeof(long), typeof(short), typeof(string) };
            return Array.Exists(numericTypes, t => t == sourceType || t.IsAssignableFrom(sourceType));
        }

        public ValidationResult ValidateParameters(ConversionContext context)
        {
            var result = new ValidationResult { IsValid = true };

            var scale = GetParameter(context, "scale", "1");
            if (!string.IsNullOrEmpty(scale) && !double.TryParse(scale, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                result.AddError($"Invalid scale factor: {scale}");
            }

            var offset = GetParameter(context, "offset", "0");
            if (!string.IsNullOrEmpty(offset) && !double.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                result.AddError($"Invalid offset value: {offset}");
            }

            var precision = GetParameter(context, "precision", "");
            if (!string.IsNullOrEmpty(precision) && 
                (!int.TryParse(precision, out var p) || p < 0))
            {
                result.AddError($"Invalid precision value: {precision}");
            }

            return result;
        }

        private string GetParameter(ConversionContext context, string key, string defaultValue)
        {
            return context.Parameters.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    /// <summary>
    /// Converter for string manipulations
    /// </summary>
    public class StringConverter : IDataConverter
    {
        public string Name => "StringConverter";

        public Type[] SupportedTypes => new[] { typeof(string), typeof(object) };

        public object Convert(object input, ConversionContext context)
        {
            if (input == null)
            {
                return string.Empty;
            }

            var inputString = input.ToString() ?? string.Empty;
            var operation = GetParameter(context, "operation", "").ToLowerInvariant();

            try
            {
                return operation switch
                {
                    "uppercase" or "upper" => inputString.ToUpperInvariant(),
                    "lowercase" or "lower" => inputString.ToLowerInvariant(),
                    "trim" => inputString.Trim(),
                    "trimstart" => inputString.TrimStart(),
                    "trimend" => inputString.TrimEnd(),
                    "reverse" => new string(inputString.ToCharArray().Reverse().ToArray()),
                    "substring" => ExtractSubstring(inputString, context),
                    "replace" => ReplaceString(inputString, context),
                    "pad" => PadString(inputString, context),
                    _ => inputString // No operation
                };
            }
            catch (Exception)
            {
                return inputString; // Return original if conversion fails
            }
        }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            // String converter can handle any input type
            return true;
        }

        public ValidationResult ValidateParameters(ConversionContext context)
        {
            var result = new ValidationResult { IsValid = true };

            var operation = GetParameter(context, "operation", "").ToLowerInvariant();
            var validOperations = new[] { "", "uppercase", "upper", "lowercase", "lower", "trim", "trimstart", 
                                        "trimend", "reverse", "substring", "replace", "pad" };

            if (!Array.Exists(validOperations, op => op == operation))
            {
                result.AddError($"Invalid string operation: {operation}");
            }

            // Validate operation-specific parameters
            switch (operation)
            {
                case "substring":
                    var start = GetParameter(context, "start", "0");
                    var length = GetParameter(context, "length", "");
                    
                    if (!int.TryParse(start, out _))
                    {
                        result.AddError($"Invalid substring start position: {start}");
                    }
                    
                    if (!string.IsNullOrEmpty(length) && !int.TryParse(length, out _))
                    {
                        result.AddError($"Invalid substring length: {length}");
                    }
                    break;

                case "pad":
                    var padLength = GetParameter(context, "length", "");
                    if (string.IsNullOrEmpty(padLength) || !int.TryParse(padLength, out _))
                    {
                        result.AddError("Pad operation requires a valid length parameter");
                    }
                    break;
            }

            return result;
        }

        private string GetParameter(ConversionContext context, string key, string defaultValue)
        {
            return context.Parameters.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private string ExtractSubstring(string input, ConversionContext context)
        {
            var startStr = GetParameter(context, "start", "0");
            var lengthStr = GetParameter(context, "length", "");

            if (!int.TryParse(startStr, out var start) || start < 0)
            {
                return input;
            }

            if (start >= input.Length)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(lengthStr))
            {
                return input.Substring(start);
            }

            if (int.TryParse(lengthStr, out var length) && length > 0)
            {
                var maxLength = Math.Min(length, input.Length - start);
                return input.Substring(start, maxLength);
            }

            return input;
        }

        private string ReplaceString(string input, ConversionContext context)
        {
            var oldValue = GetParameter(context, "oldValue", "");
            var newValue = GetParameter(context, "newValue", "");

            if (string.IsNullOrEmpty(oldValue))
            {
                return input;
            }

            return input.Replace(oldValue, newValue);
        }

        private string PadString(string input, ConversionContext context)
        {
            var lengthStr = GetParameter(context, "length", "");
            var padChar = GetParameter(context, "padChar", " ");
            var direction = GetParameter(context, "direction", "left").ToLowerInvariant();

            if (!int.TryParse(lengthStr, out var length) || length <= input.Length)
            {
                return input;
            }

            var paddingChar = padChar.Length > 0 ? padChar[0] : ' ';

            return direction switch
            {
                "right" => input.PadRight(length, paddingChar),
                _ => input.PadLeft(length, paddingChar) // Default to left padding
            };
        }
    }
}