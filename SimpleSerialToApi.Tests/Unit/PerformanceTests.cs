using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleSerialToApi.Services.Parsers;
using SimpleSerialToApi.Tests.TestData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Unit
{
    [TestClass]
    public class PerformanceTests : TestBase
    {
        [TestMethod]
        public async Task DataParsing_With1000Messages_ShouldCompleteInTime()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var service = new DataParsingService(mockLogger.Object);
            var messages = TestDataGenerator.GenerateTestMessages(1000);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var successCount = 0;
            foreach (var message in messages)
            {
                var parsed = service.Parse(message, parsingRule);
                if (parsed != null)
                {
                    successCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
                $"Parsing took too long: {stopwatch.ElapsedMilliseconds}ms");
            successCount.Should().Be(1000, "All messages should be parsed successfully");
        }

        [TestMethod]
        public async Task ConcurrentParsing_ShouldMaintainPerformance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var service = new DataParsingService(mockLogger.Object);
            var messages = TestDataGenerator.GenerateTestMessages(500);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var tasks = messages.Select(async message =>
            {
                return await Task.Run(() => service.Parse(message, parsingRule));
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
                $"Concurrent parsing took too long: {stopwatch.ElapsedMilliseconds}ms");
            
            var successCount = results.Count(r => r != null);
            successCount.Should().Be(500, "All messages should be parsed successfully");
        }

        [TestMethod]
        public void MemoryUsage_WithLargeDataSet_ShouldNotExceedLimit()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var service = new DataParsingService(mockLogger.Object);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < 1000; i++)
            {
                var message = TestDataGenerator.GenerateTemperatureData(
                    (decimal)(20 + (i % 30)), 
                    (decimal)(40 + (i % 40)));
                
                var parsed = service.Parse(message, parsingRule);
                
                // Force garbage collection every 100 iterations
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            memoryIncrease.Should().BeLessThan(50 * 1024 * 1024, // 50MB limit
                $"Memory usage increased by {memoryIncrease / (1024 * 1024)}MB which is too much");
        }

        [TestMethod]
        public async Task QueueThroughput_ShouldHandle1000MessagesPerSecond()
        {
            // Arrange
            var config = new SimpleSerialToApi.Models.QueueConfiguration { MaxSize = 10000 };
            var queue = new SimpleSerialToApi.Services.Queues.ConcurrentMessageQueue<string>(config);
            
            var messageCount = 1000;
            var messages = Enumerable.Range(0, messageCount)
                .Select(i => new SimpleSerialToApi.Models.QueueMessage<string> 
                { 
                    Payload = $"Performance test message {i}" 
                })
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act - Enqueue
            var enqueueTasks = messages.Select(m => queue.EnqueueAsync(m));
            await Task.WhenAll(enqueueTasks);
            
            var enqueueTime = stopwatch.Elapsed;

            // Act - Dequeue
            stopwatch.Restart();
            var dequeueTasks = Enumerable.Range(0, messageCount)
                .Select(_ => queue.DequeueAsync());
            var results = await Task.WhenAll(dequeueTasks);
            
            var dequeueTime = stopwatch.Elapsed;
            stopwatch.Stop();

            // Assert
            enqueueTime.Should().BeLessThan(TimeSpan.FromSeconds(1), 
                $"Enqueuing {messageCount} messages took {enqueueTime.TotalMilliseconds}ms");
            
            dequeueTime.Should().BeLessThan(TimeSpan.FromSeconds(1), 
                $"Dequeuing {messageCount} messages took {dequeueTime.TotalMilliseconds}ms");
            
            results.Should().NotContain(r => r == null);
            results.Should().HaveCount(messageCount);

            queue.Dispose();
        }

        [TestMethod]
        public async Task ApiClientThroughput_WithMockServer_ShouldMaintainPerformance()
        {
            // Arrange
            var mockHandler = new SimpleSerialToApi.Tests.Mocks.MockHttpMessageHandler();
            
            // Set up successful responses
            for (int i = 0; i < 100; i++)
            {
                mockHandler.QueueResponse(SimpleSerialToApi.Tests.Mocks.MockHttpResponses.Success());
            }

            var httpClient = new System.Net.Http.HttpClient(mockHandler);
            var mockLogger = new Mock<ILogger<SimpleSerialToApi.Services.HttpApiClientService>>();
            var apiService = new SimpleSerialToApi.Services.HttpApiClientService(httpClient, mockLogger.Object);

            var payloads = Enumerable.Range(0, 100)
                .Select(i => new { Id = i, Temperature = 20.0 + i, Timestamp = DateTime.UtcNow })
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            var tasks = payloads.Select(payload => apiService.PostAsync("performance-test", payload));
            var responses = await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
                $"100 API calls took {stopwatch.ElapsedMilliseconds}ms which is too long");
            
            responses.Should().OnlyContain(r => r.IsSuccess);

            httpClient.Dispose();
            apiService.Dispose();
        }

        [TestMethod]
        public void RegexPerformance_WithComplexPattern_ShouldBeEfficient()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var service = new DataParsingService(mockLogger.Object);
            
            var complexPattern = @"DEVICE:([A-Z0-9]+);TEMP:([0-9.]+)C;HUMID:([0-9.]+)%;PRESS:([0-9.]+)Pa;VOLT:([0-9.]+)V;TIME:(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})";
            var complexRule = new SimpleSerialToApi.Models.ParsingRule
            {
                Name = "ComplexSensor",
                Pattern = complexPattern,
                Fields = new[] { "device_id", "temperature", "humidity", "pressure", "voltage", "timestamp" },
                DataFormat = "TEXT",
                DeviceType = "COMPLEX_SENSOR"
            };

            var testData = "DEVICE:SENSOR001;TEMP:23.5C;HUMID:65.2%;PRESS:1013.25Pa;VOLT:12.4V;TIME:2023-01-01T12:30:45";
            var messages = Enumerable.Range(0, 1000)
                .Select(_ => new SimpleSerialToApi.Models.RawSerialData
                {
                    Data = System.Text.Encoding.UTF8.GetBytes(testData),
                    DataFormat = "TEXT",
                    ReceivedTime = DateTime.UtcNow,
                    DeviceId = "COMPLEX_DEVICE"
                })
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            var successCount = 0;
            foreach (var message in messages)
            {
                var result = service.Parse(message, complexRule);
                if (result != null && result.Fields.Count == 6)
                {
                    successCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
                $"Complex regex parsing took {stopwatch.ElapsedMilliseconds}ms which is too long");
            
            successCount.Should().Be(1000, "All complex messages should be parsed");
        }

        [TestMethod]
        public async Task EndToEndThroughput_ShouldProcessDataWithinSLA()
        {
            // Arrange
            var mockHandler = new SimpleSerialToApi.Tests.Mocks.MockHttpMessageHandler();
            for (int i = 0; i < 50; i++)
            {
                mockHandler.QueueResponse(SimpleSerialToApi.Tests.Mocks.MockHttpResponses.Success());
            }

            var httpClient = new System.Net.Http.HttpClient(mockHandler);
            var parsingLogger = new Mock<ILogger<DataParsingService>>();
            var apiLogger = new Mock<ILogger<SimpleSerialToApi.Services.HttpApiClientService>>();
            
            var parsingService = new DataParsingService(parsingLogger.Object);
            var apiService = new SimpleSerialToApi.Services.HttpApiClientService(httpClient, apiLogger.Object);
            
            var testMessages = TestDataGenerator.GenerateTestMessages(50);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            var stopwatch = Stopwatch.StartNew();

            // Act - Complete end-to-end processing
            var processedCount = 0;
            foreach (var rawMessage in testMessages)
            {
                // 1. Parse
                var parsed = parsingService.Parse(rawMessage, parsingRule);
                if (parsed != null)
                {
                    // 2. Map to API format
                    var apiData = new
                    {
                        DeviceId = parsed.DeviceId,
                        Temperature = parsed.Fields["temperature"],
                        Humidity = parsed.Fields["humidity"],
                        Timestamp = parsed.ParsedTime
                    };

                    // 3. Transmit
                    var response = await apiService.PostAsync("end-to-end-test", apiData);
                    if (response.IsSuccess)
                    {
                        processedCount++;
                    }
                }
            }

            stopwatch.Stop();

            // Assert - Should process within SLA (1 second per message max)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50000, 
                $"End-to-end processing took {stopwatch.ElapsedMilliseconds}ms for 50 messages");
            
            processedCount.Should().Be(50, "All messages should be processed successfully");

            httpClient.Dispose();
            apiService.Dispose();
        }

        [TestMethod]
        public void LargePayload_ShouldHandleEfficientlyWithinMemoryLimits()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var service = new DataParsingService(mockLogger.Object);
            
            var largeDataMessage = TestDataGenerator.GenerateLargeData(100000); // 100KB
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            
            var initialMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = service.Parse(largeDataMessage, parsingRule);

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
                $"Large data parsing took {stopwatch.ElapsedMilliseconds}ms");
            
            memoryUsed.Should().BeLessThan(500 * 1024 * 1024, // 500MB limit
                $"Memory usage was {memoryUsed / (1024 * 1024)}MB which is excessive");

            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        public async Task StressTest_MultipleComponentsConcurrent_ShouldMaintainStability()
        {
            // Arrange
            var config = new SimpleSerialToApi.Models.QueueConfiguration { MaxSize = 1000 };
            var queue = new SimpleSerialToApi.Services.Queues.ConcurrentMessageQueue<string>(config);
            
            var mockLogger = new Mock<ILogger<DataParsingService>>();
            var parsingService = new DataParsingService(mockLogger.Object);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            var messageCount = 100;
            var concurrentTasks = 10;
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Stress test with concurrent operations
            var allTasks = new List<Task>();

            // Producer tasks
            for (int i = 0; i < concurrentTasks; i++)
            {
                var taskIndex = i;
                allTasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < messageCount / concurrentTasks; j++)
                    {
                        var message = TestDataGenerator.GenerateTemperatureData(
                            20 + taskIndex, 50 + j);
                        
                        var parsed = parsingService.Parse(message, parsingRule);
                        if (parsed != null)
                        {
                            var queueMessage = new SimpleSerialToApi.Models.QueueMessage<string>
                            {
                                Payload = $"Task{taskIndex}-Message{j}"
                            };
                            await queue.EnqueueAsync(queueMessage);
                        }
                    }
                }));
            }

            // Consumer tasks
            for (int i = 0; i < concurrentTasks; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    var processedCount = 0;
                    while (processedCount < messageCount / concurrentTasks)
                    {
                        var message = await queue.DequeueAsync(TimeSpan.FromMilliseconds(100));
                        if (message != null)
                        {
                            processedCount++;
                        }
                    }
                }));
            }

            await Task.WhenAll(allTasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
                $"Stress test took {stopwatch.ElapsedMilliseconds}ms");
            
            queue.Count.Should().Be(0, "All messages should be processed");

            queue.Dispose();
        }
    }
}