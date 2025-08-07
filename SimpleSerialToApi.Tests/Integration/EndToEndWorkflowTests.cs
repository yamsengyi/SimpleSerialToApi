using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Services.Parsers;
using SimpleSerialToApi.Services.Queues;
using SimpleSerialToApi.Tests.Mocks;
using SimpleSerialToApi.Tests.TestData;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Integration
{
    [TestClass]
    public class EndToEndWorkflowTests : TestBase
    {
        private IServiceProvider? _serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Register actual services
            services.AddSingleton<IDataParsingService, DataParsingService>();
            services.AddSingleton<IMessageQueue<MappedApiData>, ConcurrentMessageQueue<MappedApiData>>();

            // Mock external dependencies
            services.AddSingleton(CreateMockSerialService());
            services.AddSingleton(CreateMockHttpClient());
            services.AddSingleton(CreateMockConfigurationService());

            // Add logging
            services.AddLogging(builder => builder.AddConsole());

            // Add queue configuration
            services.AddSingleton(new QueueConfiguration { MaxSize = 1000 });

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public async Task SerialDataToApiWorkflow_ShouldProcessSuccessfully()
        {
            // Arrange
            var serialService = _serviceProvider!.GetRequiredService<ISerialCommunicationService>();
            var parsingService = _serviceProvider.GetRequiredService<IDataParsingService>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            // Act & Assert
            // 1. Serial connection
            var connected = await serialService.ConnectAsync();
            connected.Should().BeTrue();

            // 2. Data parsing
            var rawData = TestDataGenerator.GenerateTemperatureData(25.5m, 60.0m);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var parsedData = parsingService.Parse(rawData, parsingRule);

            parsedData.Should().NotBeNull();
            parsedData!.Fields.Should().ContainKey("temperature");
            parsedData.Fields.Should().ContainKey("humidity");

            // 3. API transmission
            var apiData = TestDataGenerator.GenerateApiData("TestEndpoint", parsedData);
            var apiResponse = await apiService.PostAsync(apiData.EndpointName, apiData.Payload);

            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task CompleteDataPipeline_WithMultipleMessages_ShouldProcessAll()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var messageQueue = _serviceProvider.GetRequiredService<IMessageQueue<MappedApiData>>();
            var apiService = _serviceProvider.GetRequiredService<IApiClientService>();

            var testMessages = TestDataGenerator.GenerateTestMessages(10);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var processedCount = 0;
            foreach (var rawData in testMessages)
            {
                // 1. Parse data
                var parsedData = parsingService.Parse(rawData, parsingRule);
                if (parsedData != null)
                {
                    // 2. Create API data
                    var apiData = TestDataGenerator.GenerateApiData("TestEndpoint", parsedData);

                    // 3. Queue for transmission
                    var queueMessage = new QueueMessage<MappedApiData> { Payload = apiData };
                    var enqueued = await messageQueue.EnqueueAsync(queueMessage);

                    if (enqueued)
                    {
                        // 4. Dequeue and transmit
                        var queuedMessage = await messageQueue.DequeueAsync();
                        if (queuedMessage != null)
                        {
                            var response = await apiService.PostAsync(
                                queuedMessage.Payload.EndpointName, 
                                queuedMessage.Payload.Payload);
                            
                            if (response.IsSuccess)
                            {
                                processedCount++;
                            }
                        }
                    }
                }
            }

            // Assert
            processedCount.Should().Be(testMessages.Count);
            messageQueue.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task ErrorHandling_WithInvalidData_ShouldContinueProcessing()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            var validData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var invalidData = TestDataGenerator.GenerateCorruptedData();
            var emptyData = TestDataGenerator.GenerateEmptyData();

            // Act & Assert
            // Valid data should parse successfully
            var validResult = parsingService.Parse(validData, parsingRule);
            validResult.Should().NotBeNull();

            // Invalid data should return null but not throw
            var invalidResult = parsingService.Parse(invalidData, parsingRule);
            invalidResult.Should().BeNull();

            // Empty data should return null but not throw
            var emptyResult = parsingService.Parse(emptyData, parsingRule);
            emptyResult.Should().BeNull();
        }

        [TestMethod]
        public async Task HighVolumeProcessing_ShouldMaintainPerformance()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var messageQueue = _serviceProvider.GetRequiredService<IMessageQueue<MappedApiData>>();
            
            var performanceTestData = TestDataGenerator.GeneratePerformanceTestData(100);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var startTime = DateTime.UtcNow;
            var successCount = 0;

            foreach (var rawData in performanceTestData)
            {
                var parsedData = parsingService.Parse(rawData, parsingRule);
                if (parsedData != null)
                {
                    var apiData = TestDataGenerator.GenerateApiData("TestEndpoint", parsedData);
                    var queueMessage = new QueueMessage<MappedApiData> { Payload = apiData };
                    
                    var enqueued = await messageQueue.EnqueueAsync(queueMessage);
                    if (enqueued) successCount++;
                }
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "Processing should complete within 10 seconds");
            successCount.Should().BeGreaterThan(80, "Most messages should be processed successfully");
        }

        [TestMethod]
        public async Task ConfigurationChange_ShouldUpdateServices()
        {
            // Arrange
            var configService = _serviceProvider!.GetRequiredService<IConfigurationService>() as MockConfigurationService;
            var originalConfig = configService!.ApplicationConfig;

            // Act - Update configuration
            var newConfig = new ApplicationConfiguration
            {
                SerialSettings = new SerialConnectionSettings
                {
                    PortName = "COM2", // Changed from COM1
                    BaudRate = 19200  // Changed from 9600
                },
                MessageQueueSettings = originalConfig.MessageQueueSettings,
                ApiEndpoints = originalConfig.ApiEndpoints,
                MappingRules = originalConfig.MappingRules
            };

            configService.UpdateApplicationConfig(newConfig);
            configService.ReloadConfiguration();

            // Assert
            var updatedConfig = configService.ApplicationConfig;
            updatedConfig.SerialSettings.PortName.Should().Be("COM2");
            updatedConfig.SerialSettings.BaudRate.Should().Be(19200);
        }

        [TestMethod]
        public async Task SerialDeviceReconnection_ShouldHandleGracefully()
        {
            // Arrange
            var serialService = _serviceProvider!.GetRequiredService<ISerialCommunicationService>();

            // Act & Assert
            // Initial connection
            var connected1 = await serialService.ConnectAsync();
            connected1.Should().BeTrue();

            // Disconnect
            await serialService.DisconnectAsync();
            serialService.IsConnected.Should().BeFalse();

            // Reconnect
            var connected2 = await serialService.ConnectAsync();
            connected2.Should().BeTrue();
        }

        [TestMethod]
        public async Task DataValidation_ShouldRejectInvalidFormats()
        {
            // Arrange
            var parsingService = _serviceProvider!.GetRequiredService<IDataParsingService>();
            var temperatureRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var pressureRule = TestDataGenerator.GeneratePressureParsingRule();

            var temperatureData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var pressureData = TestDataGenerator.GeneratePressureData(1013.25m);

            // Act & Assert
            // Temperature data with temperature rule should work
            var tempResult = parsingService.Parse(temperatureData, temperatureRule);
            tempResult.Should().NotBeNull();

            // Temperature data with pressure rule should fail
            var invalidResult = parsingService.Parse(temperatureData, pressureRule);
            invalidResult.Should().BeNull();

            // Pressure data with pressure rule should work
            var pressureResult = parsingService.Parse(pressureData, pressureRule);
            pressureResult.Should().NotBeNull();
        }

        private ISerialCommunicationService CreateMockSerialService()
        {
            var mockLogger = new Mock<ILogger<SerialCommunicationService>>();
            return new SerialCommunicationService(mockLogger.Object);
        }

        private IApiClientService CreateMockHttpClient()
        {
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetResponse(MockHttpResponses.Success("{\"status\": \"ok\"}"));
            
            var httpClient = new HttpClient(mockHandler);
            var mockLogger = new Mock<ILogger<HttpApiClientService>>();
            
            return new HttpApiClientService(httpClient, mockLogger.Object);
        }

        private IConfigurationService CreateMockConfigurationService()
        {
            var mockLogger = new Mock<ILogger<MockConfigurationService>>();
            return new MockConfigurationService(mockLogger.Object);
        }
    }
}