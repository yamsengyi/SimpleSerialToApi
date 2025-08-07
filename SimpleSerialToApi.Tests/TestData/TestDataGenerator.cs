using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSerialToApi.Tests.TestData
{
    /// <summary>
    /// Utility class for generating test data for various test scenarios
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generate temperature sensor data in TEXT format
        /// </summary>
        public static RawSerialData GenerateTemperatureData(decimal temperature, decimal humidity)
        {
            return new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes($"TEMP:{temperature}C;HUMID:{humidity}%"),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "TEST_DEVICE_01"
            };
        }

        /// <summary>
        /// Generate temperature sensor data in HEX format
        /// </summary>
        public static RawSerialData GenerateTemperatureDataHex(decimal temperature, decimal humidity)
        {
            // Convert temperature and humidity to hex bytes (simplified example)
            var tempBytes = BitConverter.GetBytes((float)temperature);
            var humidBytes = BitConverter.GetBytes((float)humidity);
            
            var data = new byte[12];
            data[0] = 0x01; // Header
            data[1] = 0x02; // Command: Temperature data
            Array.Copy(tempBytes, 0, data, 2, 4);
            Array.Copy(humidBytes, 0, data, 6, 4);
            data[10] = 0x03; // Footer
            data[11] = CalculateChecksum(data, 0, 10);

            return new RawSerialData
            {
                Data = data,
                DataFormat = "HEX",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "TEST_DEVICE_01"
            };
        }

        /// <summary>
        /// Generate pressure sensor data
        /// </summary>
        public static RawSerialData GeneratePressureData(decimal pressure)
        {
            return new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes($"PRESS:{pressure}Pa"),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "PRESSURE_SENSOR_01"
            };
        }

        /// <summary>
        /// Generate multiple test messages with random data
        /// </summary>
        public static List<RawSerialData> GenerateTestMessages(int count)
        {
            var messages = new List<RawSerialData>();

            for (int i = 0; i < count; i++)
            {
                var temperature = (decimal)(_random.NextDouble() * 50); // 0-50°C
                var humidity = (decimal)(_random.NextDouble() * 100); // 0-100%
                
                messages.Add(GenerateTemperatureData(temperature, humidity));
            }

            return messages;
        }

        /// <summary>
        /// Generate performance test data with specified count
        /// </summary>
        public static List<RawSerialData> GeneratePerformanceTestData(int messageCount)
        {
            var messages = new List<RawSerialData>();

            for (int i = 0; i < messageCount; i++)
            {
                var temperature = (decimal)(20 + (_random.NextDouble() * 10)); // 20-30°C
                var humidity = (decimal)(40 + (_random.NextDouble() * 20)); // 40-60%
                
                messages.Add(GenerateTemperatureData(temperature, humidity));
                
                // Add some variety
                if (i % 10 == 0)
                {
                    var pressure = (decimal)(1000 + (_random.NextDouble() * 200)); // 1000-1200 Pa
                    messages.Add(GeneratePressureData(pressure));
                }
            }

            return messages;
        }

        /// <summary>
        /// Generate invalid/corrupted data for error testing
        /// </summary>
        public static RawSerialData GenerateCorruptedData()
        {
            var corruptedData = new byte[_random.Next(1, 50)];
            _random.NextBytes(corruptedData);

            return new RawSerialData
            {
                Data = corruptedData,
                DataFormat = "UNKNOWN",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "CORRUPTED_SOURCE"
            };
        }

        /// <summary>
        /// Generate empty data for edge case testing
        /// </summary>
        public static RawSerialData GenerateEmptyData()
        {
            return new RawSerialData
            {
                Data = Array.Empty<byte>(),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "EMPTY_SOURCE"
            };
        }

        /// <summary>
        /// Generate large data payload for stress testing
        /// </summary>
        public static RawSerialData GenerateLargeData(int sizeInBytes = 10000)
        {
            var largeData = new byte[sizeInBytes];
            
            // Fill with repeated pattern
            var pattern = Encoding.UTF8.GetBytes("LARGE_DATA_PATTERN:");
            for (int i = 0; i < sizeInBytes; i++)
            {
                largeData[i] = pattern[i % pattern.Length];
            }

            return new RawSerialData
            {
                Data = largeData,
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "LARGE_DATA_SOURCE"
            };
        }

        /// <summary>
        /// Generate sample parsed data for API mapping tests
        /// </summary>
        public static ParsedSerialData GenerateParsedTemperatureData(decimal temperature, decimal humidity)
        {
            return new ParsedSerialData
            {
                DeviceId = "TEST_DEVICE_01",
                ParsedTime = DateTime.UtcNow,
                Fields = new Dictionary<string, object>
                {
                    ["temperature"] = temperature,
                    ["humidity"] = humidity,
                    ["unit"] = "celsius",
                    ["timestamp"] = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// Generate sample API data for transmission tests
        /// </summary>
        public static MappedApiData GenerateApiData(string endpoint, object payload)
        {
            return new MappedApiData
            {
                EndpointName = endpoint,
                Url = $"https://api.test.com/{endpoint}",
                Method = "POST",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Authorization"] = "Bearer test-token"
                },
                CreatedTime = DateTime.UtcNow,
                RetryCount = 0
            };
        }

        /// <summary>
        /// Generate parsing rules for different data formats
        /// </summary>
        public static ParsingRule GenerateTemperatureParsingRule()
        {
            return new ParsingRule
            {
                Name = "TemperatureSensor",
                Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
                Fields = new[] { "temperature", "humidity" },
                DataFormat = "TEXT",
                DeviceType = "TEMPERATURE_SENSOR"
            };
        }

        /// <summary>
        /// Generate parsing rules for pressure sensor
        /// </summary>
        public static ParsingRule GeneratePressureParsingRule()
        {
            return new ParsingRule
            {
                Name = "PressureSensor",
                Pattern = @"PRESS:([0-9.]+)Pa",
                Fields = new[] { "pressure" },
                DataFormat = "TEXT",
                DeviceType = "PRESSURE_SENSOR"
            };
        }

        /// <summary>
        /// Generate API endpoint configurations for testing
        /// </summary>
        public static List<ApiEndpointConfig> GenerateTestApiEndpoints()
        {
            return new List<ApiEndpointConfig>
            {
                new ApiEndpointConfig
                {
                    Name = "TemperatureEndpoint",
                    Url = "https://api.test.com/temperature",
                    Method = "POST",
                    Timeout = 30000,
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer test-token"
                    }
                },
                new ApiEndpointConfig
                {
                    Name = "PressureEndpoint", 
                    Url = "https://api.test.com/pressure",
                    Method = "POST",
                    Timeout = 30000,
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer test-token"
                    }
                }
            };
        }

        /// <summary>
        /// Calculate simple checksum for hex data validation
        /// </summary>
        private static byte CalculateChecksum(byte[] data, int start, int length)
        {
            byte checksum = 0;
            for (int i = start; i < start + length; i++)
            {
                checksum ^= data[i];
            }
            return checksum;
        }
    }
}