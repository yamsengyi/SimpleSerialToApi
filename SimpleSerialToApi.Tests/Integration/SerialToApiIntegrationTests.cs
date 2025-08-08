using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Services.Parsers;
using SimpleSerialToApi.Tests.Mocks;
using SimpleSerialToApi.Tests.TestData;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Integration
{
    [TestClass]
    public class SerialToApiIntegrationTests : TestBase
    {
        private IServiceProvider? _serviceProvider;
        private MockHttpMessageHandler? _mockHttpHandler;
        private MockSerialPort? _mockSerialPort;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Create mocks
            _mockHttpHandler = new MockHttpMessageHandler();
            _mockSerialPort = new MockSerialPort();

            // Register services
            services.AddSingleton<IDataParsingService, DataParsingService>();
            services.AddSingleton<IApiClientService>(provider => 
            {
                var httpClient = new HttpClient(_mockHttpHandler);
                var logger = provider.GetRequiredService<ILogger<HttpApiClientService>>();
                return new HttpApiClientService(httpClient, logger);
            });

            // Add logging
            services.AddLogging(builder => builder.AddConsole());

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
            _mockHttpHandler?.Dispose();
            _mockSerialPort?.Dispose();
        }

        [TestMethod]
        public async Task SerialDataReceived_ShouldParseAndTransmitToApi()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();
            
            _mockHttpHandler!.SetResponse(MockHttpResponses.Success());

            // Simulate serial data
            var temperatureData = "TEMP:23.5C;HUMID:55.2%";
            var rawData = new RawSerialData
            {
                Data = System.Text.Encoding.UTF8.GetBytes(temperatureData),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "TEMP_SENSOR_01"
            };

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            // 1. Parse the serial data
            var parsedData = parsingService.Parse(rawData, parsingRule);

            // 2. Map to API format
            var apiData = new
            {
                DeviceId = parsedData!.DeviceId,
                Timestamp = parsedData.ParsedTime,
                Temperature = parsedData.Fields["temperature"],
                Humidity = parsedData.Fields["humidity"]
            };

            // 3. Send to API
            var response = await apiService.PostAsync("temperature-data", apiData);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(200);

            // Verify HTTP request was made
            var requests = _mockHttpHandler.GetRequests();
            requests.Should().HaveCount(1);
            requests[0].Method.ToString().Should().Be("POST");
        }

        [TestMethod]
        public async Task MultipleSerialSources_ShouldProcessIndependently()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            // Queue multiple successful responses
            for (int i = 0; i < 3; i++)
            {
                _mockHttpHandler!.QueueResponse(MockHttpResponses.Success($"{{\"id\": {i}}}"));
            }

            var temperatureRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var pressureRule = TestDataGenerator.GeneratePressureParsingRule();

            // Create data from different sources
            var tempData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var pressureData = TestDataGenerator.GeneratePressureData(1013.25m);
            var tempData2 = TestDataGenerator.GenerateTemperatureData(22.0m, 55.0m);

            // Act
            var results = new List<ApiResponse>();

            // Process temperature data 1
            var parsed1 = parsingService.Parse(tempData, temperatureRule);
            if (parsed1 != null)
            {
                results.Add(await apiService.PostAsync("temperature", parsed1));
            }

            // Process pressure data
            var parsed2 = parsingService.Parse(pressureData, pressureRule);
            if (parsed2 != null)
            {
                results.Add(await apiService.PostAsync("pressure", parsed2));
            }

            // Process temperature data 2
            var parsed3 = parsingService.Parse(tempData2, temperatureRule);
            if (parsed3 != null)
            {
                results.Add(await apiService.PostAsync("temperature", parsed3));
            }

            // Assert
            results.Should().HaveCount(3);
            results.Should().OnlyContain(r => r.IsSuccess);

            var requests = _mockHttpHandler!.GetRequests();
            requests.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task ApiFailure_ShouldNotAffectSubsequentRequests()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            // Set up responses - first fails, second succeeds
            _mockHttpHandler!.QueueResponse(MockHttpResponses.InternalServerError());
            _mockHttpHandler.QueueResponse(MockHttpResponses.Success());

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var data1 = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var data2 = TestDataGenerator.GenerateTemperatureData(26.0m, 65.0m);

            // Act
            var parsed1 = parsingService.Parse(data1, parsingRule);
            var response1 = await apiService.PostAsync("temperature", parsed1!);

            var parsed2 = parsingService.Parse(data2, parsingRule);
            var response2 = await apiService.PostAsync("temperature", parsed2!);

            // Assert
            response1.IsSuccess.Should().BeFalse();
            response1.StatusCode.Should().Be(500);

            response2.IsSuccess.Should().BeTrue();
            response2.StatusCode.Should().Be(200);
        }

        [TestMethod]
        public async Task LargeDataVolume_ShouldProcessWithinTimeLimit()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            // Set up successful responses
            for (int i = 0; i < 100; i++)
            {
                _mockHttpHandler!.QueueResponse(MockHttpResponses.Success());
            }

            var testData = TestDataGenerator.GeneratePerformanceTestData(100);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var startTime = DateTime.UtcNow;
            var successfulCount = 0;

            foreach (var rawData in testData)
            {
                var parsed = parsingService.Parse(rawData, parsingRule);
                if (parsed != null)
                {
                    var response = await apiService.PostAsync("bulk-data", parsed);
                    if (response.IsSuccess)
                    {
                        successfulCount++;
                    }
                }
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30), "Should process 100 items within 30 seconds");
            successfulCount.Should().BeGreaterThan(50, "Should successfully process majority of items");
        }

        [TestMethod]
        public async Task DataIntegrity_ShouldPreserveAllFields()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            string? capturedRequestBody = null;
            _mockHttpHandler!.SetResponseFunction(request =>
            {
                // Capture the request body for validation
                if (request.Content != null)
                {
                    capturedRequestBody = request.Content.ReadAsStringAsync().Result;
                }
                return MockHttpResponses.Success();
            });

            var originalTemp = 23.75m;
            var originalHumidity = 67.5m;
            var testData = TestDataGenerator.GenerateTemperatureData(originalTemp, originalHumidity);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var parsed = parsingService.Parse(testData, parsingRule);
            var apiPayload = new
            {
                DeviceId = parsed!.DeviceId,
                Temperature = parsed.Fields["temperature"],
                Humidity = parsed.Fields["humidity"],
                Timestamp = parsed.ParsedTime
            };

            var response = await apiService.PostAsync("data-integrity", apiPayload);

            // Assert
            response.IsSuccess.Should().BeTrue();
            capturedRequestBody.Should().NotBeNullOrEmpty();

            // Verify data integrity in the transmitted JSON
            capturedRequestBody.Should().Contain($"\"Temperature\":{originalTemp}");
            capturedRequestBody.Should().Contain($"\"Humidity\":{originalHumidity}");
            capturedRequestBody.Should().Contain($"\"DeviceId\":\"{testData.DeviceId}\"");
        }

        [TestMethod]
        public async Task ConcurrentApiCalls_ShouldAllSucceed()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            // Set up multiple responses
            for (int i = 0; i < 10; i++)
            {
                _mockHttpHandler!.QueueResponse(MockHttpResponses.Success($"{{\"id\": {i}}}"));
            }

            var testData = TestDataGenerator.GenerateTestMessages(10);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var tasks = testData.Select(async rawData =>
            {
                var parsed = parsingService.Parse(rawData, parsingRule);
                if (parsed != null)
                {
                    return await apiService.PostAsync("concurrent-test", parsed);
                }
                return null;
            }).Where(t => t != null).Cast<Task<ApiResponse>>();

            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().HaveCount(10);
            responses.Should().OnlyContain(r => r.IsSuccess);

            var requests = _mockHttpHandler!.GetRequests();
            requests.Should().HaveCount(10);
        }

        [TestMethod]
        public async Task NetworkTimeout_ShouldHandleGracefully()
        {
            // Arrange
            var apiService = _serviceProvider!.GetRequiredService<IApiClientService>();

            _mockHttpHandler!.SetResponse(MockHttpResponses.Timeout());

            var testPayload = new { Temperature = 25.0, DeviceId = "TEST" };

            // Act
            var response = await apiService.PostAsync("timeout-test", testPayload);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(408);
        }

        [TestMethod]
        public async Task MalformedSerialData_ShouldNotBreakPipeline()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            _mockHttpHandler!.SetResponse(MockHttpResponses.Success());

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var validData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var invalidData = TestDataGenerator.GenerateCorruptedData();

            // Act
            var validParsed = parsingService.Parse(validData, parsingRule);
            var invalidParsed = parsingService.Parse(invalidData, parsingRule);

            // Assert
            validParsed.Should().NotBeNull();
            invalidParsed.Should().BeNull();

            // Valid data should still work after invalid data
            if (validParsed != null)
            {
                var response = await apiService.PostAsync("resilience-test", validParsed);
                response.IsSuccess.Should().BeTrue();
            }
        }

        [TestMethod]
        public async Task HexDataFormat_ShouldParseAndTransmit()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            _mockHttpHandler!.SetResponse(MockHttpResponses.Success());

            var hexData = TestDataGenerator.GenerateTemperatureDataHex(24.5m, 58.0m);
            
            var hexParsingRule = new ParsingRule
            {
                Name = "HexTemperatureSensor",
                Pattern = "", // Hex parsing uses different logic
                Fields = new[] { "temperature", "humidity" },
                DataFormat = "HEX",
                DeviceType = "TEMPERATURE_SENSOR"
            };

            // Act
            var parsed = parsingService.Parse(hexData, hexParsingRule);

            // Assert
            parsed.Should().NotBeNull("Hex data should be parseable");
            parsed!.DeviceId.Should().Be(hexData.DeviceId);

            // If parsing succeeded, try API transmission
            var response = await apiService.PostAsync("hex-data", parsed);
            response.IsSuccess.Should().BeTrue();
        }
    }
}