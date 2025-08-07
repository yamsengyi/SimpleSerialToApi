using FluentAssertions;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Queues;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Queues
{
    /// <summary>
    /// Performance tests for queue system to validate Step 05 requirements
    /// </summary>
    public class QueuePerformanceTests
    {
        private QueueConfiguration CreateTestConfiguration(string name = "PerfTestQueue")
        {
            return new QueueConfiguration
            {
                Name = name,
                MaxSize = 2000, // Allow for performance testing
                BatchSize = 50,
                BatchTimeoutMs = 1000,
                RetryCount = 3,
                RetryIntervalMs = 100,
                EnablePriority = false,
                ProcessorThreadCount = 2,
                EnableAsync = true
            };
        }

        [Fact]
        public async Task Performance_Should_Handle_1000_ConcurrentMessages()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            const int messageCount = 1000;

            var stopwatch = Stopwatch.StartNew();

            // Act - Enqueue 1000 messages
            var enqueueTasks = Enumerable.Range(0, messageCount).Select(async i =>
            {
                var message = new QueueMessage<string>($"Performance test message {i}");
                return await queue.EnqueueAsync(message);
            });

            var enqueueResults = await Task.WhenAll(enqueueTasks);
            stopwatch.Stop();

            // Assert
            enqueueResults.All(r => r).Should().BeTrue("All messages should be enqueued successfully");
            queue.Count.Should().Be(messageCount);

            var stats = queue.GetStatistics();
            stats.QueuedCount.Should().Be(messageCount);
            
            // Performance requirement: Should handle 1000+ messages
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Should enqueue 1000 messages in less than 10 seconds");
            
            Console.WriteLine($"Enqueued {messageCount} messages in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Performance_Should_ProcessMessages_FastEnough()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            const int messageCount = 100;

            // Enqueue messages
            for (int i = 0; i < messageCount; i++)
            {
                await queue.EnqueueAsync(new QueueMessage<string>($"Message {i}"));
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Dequeue and "process" all messages
            int processedCount = 0;
            while (processedCount < messageCount)
            {
                var batch = await queue.DequeueBatchAsync(10);
                foreach (var message in batch)
                {
                    // Simulate processing
                    queue.MarkMessageCompleted(message.MessageId, TimeSpan.FromMilliseconds(1));
                    processedCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            processedCount.Should().Be(messageCount);
            
            // Performance requirement: 100+ messages per second
            var messagesPerSecond = messageCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            messagesPerSecond.Should().BeGreaterThan(100, "Should process more than 100 messages per second");
            
            var stats = queue.GetStatistics();
            stats.CompletedCount.Should().Be(messageCount);
            
            Console.WriteLine($"Processed {messageCount} messages in {stopwatch.ElapsedMilliseconds}ms ({messagesPerSecond:F2} msg/sec)");
        }

        [Fact]
        public async Task Performance_MessageLatency_Should_BeLow()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Act & Assert
            for (int i = 0; i < 10; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                var message = new QueueMessage<string>($"Latency test message {i}");
                await queue.EnqueueAsync(message);
                var dequeuedMessage = await queue.DequeueAsync();
                
                stopwatch.Stop();

                dequeuedMessage.Should().NotBeNull();
                
                // Performance requirement: < 100ms average latency
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, $"Message {i} latency should be less than 100ms");
            }
        }

        [Fact]
        public void Performance_MemoryUsage_Should_BeReasonable()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Measure memory before
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);

            // Act - Add 1000 messages
            for (int i = 0; i < 1000; i++)
            {
                var message = new QueueMessage<string>($"Memory test message {i} with some additional data to make it more realistic");
                queue.EnqueueAsync(message).Wait();
            }

            // Measure memory after
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            // Assert
            var stats = queue.GetStatistics();
            queue.Count.Should().Be(1000);
            
            // Performance requirement: < 200MB for 1000 messages
            var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
            memoryUsedMB.Should().BeLessThan(200, "Memory usage should be less than 200MB for 1000 messages");
            
            Console.WriteLine($"Memory used for 1000 messages: {memoryUsedMB:F2}MB");
        }

        [Fact]
        public async Task Performance_ThreadSafety_UnderLoad()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.MaxSize = 5000;
            using var queue = new ConcurrentMessageQueue<string>(config);
            
            const int threadCount = 10;
            const int messagesPerThread = 50;
            const int totalMessages = threadCount * messagesPerThread;

            var stopwatch = Stopwatch.StartNew();

            // Act - Multiple threads enqueuing and dequeuing concurrently
            var tasks = Enumerable.Range(0, threadCount).Select(async threadId =>
            {
                // Each thread enqueues messages
                for (int i = 0; i < messagesPerThread; i++)
                {
                    var message = new QueueMessage<string>($"Thread-{threadId}-Message-{i}");
                    await queue.EnqueueAsync(message);
                }

                // And dequeues some
                for (int i = 0; i < messagesPerThread / 2; i++)
                {
                    var message = await queue.DequeueAsync();
                    if (message != null)
                    {
                        queue.MarkMessageCompleted(message.MessageId, TimeSpan.FromMilliseconds(1));
                    }
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var stats = queue.GetStatistics();
            var totalEnqueued = queue.Count + queue.ProcessingCount + (int)stats.CompletedCount;
            
            totalEnqueued.Should().Be(totalMessages, "All messages should be accounted for");
            
            // Should complete without deadlocks or exceptions
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Should complete concurrent operations quickly");
            
            Console.WriteLine($"Concurrent operations with {threadCount} threads completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Integration_QueueManager_WithMultipleQueues()
        {
            // Arrange
            using var queueManager = new QueueManager();
            
            var queue1Config = CreateTestConfiguration("Queue1");
            var queue2Config = CreateTestConfiguration("Queue2");
            
            var queue1 = queueManager.CreateQueue<string>("Queue1", queue1Config);
            var queue2 = queueManager.CreateQueue<int>("Queue2", queue2Config);

            // Act - Add messages to both queues
            const int messagesPerQueue = 100;
            
            for (int i = 0; i < messagesPerQueue; i++)
            {
                await queue1.EnqueueAsync(new QueueMessage<string>($"String message {i}"));
                await queue2.EnqueueAsync(new QueueMessage<int>(i));
            }

            // Assert
            queue1.Count.Should().Be(messagesPerQueue);
            queue2.Count.Should().Be(messagesPerQueue);
            queueManager.QueueCount.Should().Be(2);

            var allStats = await queueManager.GetAllQueueStatisticsAsync();
            allStats.Should().HaveCount(2);
            allStats["Queue1"].QueuedCount.Should().Be(messagesPerQueue);
            allStats["Queue2"].QueuedCount.Should().Be(messagesPerQueue);

            // Health should be good
            queueManager.GetQueueHealth("Queue1").Should().Be(QueueHealthStatus.Healthy);
            queueManager.GetQueueHealth("Queue2").Should().Be(QueueHealthStatus.Healthy);
        }
    }
}