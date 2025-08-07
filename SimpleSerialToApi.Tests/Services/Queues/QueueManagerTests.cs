using FluentAssertions;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Queues;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Queues
{
    /// <summary>
    /// Tests for QueueManager implementation
    /// </summary>
    public class QueueManagerTests
    {
        private QueueConfiguration CreateTestConfiguration(string name = "TestQueue")
        {
            return new QueueConfiguration
            {
                Name = name,
                MaxSize = 100,
                BatchSize = 5,
                BatchTimeoutMs = 1000,
                RetryCount = 3,
                RetryIntervalMs = 100,
                EnablePriority = false,
                ProcessorThreadCount = 1,
                EnableAsync = true
            };
        }

        [Fact]
        public void CreateQueue_ShouldCreateAndReturnQueue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");

            // Act
            var queue = queueManager.CreateQueue<string>("TestQueue", config);

            // Assert
            queue.Should().NotBeNull();
            queue.Name.Should().Be("TestQueue");
            queueManager.QueueCount.Should().Be(1);
        }

        [Fact]
        public void CreateQueue_WithDuplicateName_ShouldThrowException()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);

            // Act & Assert
            var act = () => queueManager.CreateQueue<string>("TestQueue", config);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Queue 'TestQueue' already exists");
        }

        [Fact]
        public void GetQueue_ExistingQueue_ShouldReturnQueue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            var originalQueue = queueManager.CreateQueue<string>("TestQueue", config);

            // Act
            var retrievedQueue = queueManager.GetQueue<string>("TestQueue");

            // Assert
            retrievedQueue.Should().NotBeNull();
            retrievedQueue.Should().Be(originalQueue);
        }

        [Fact]
        public void GetQueue_NonExistentQueue_ShouldReturnNull()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act
            var queue = queueManager.GetQueue<string>("NonExistent");

            // Assert
            queue.Should().BeNull();
        }

        [Fact]
        public async Task RemoveQueueAsync_ExistingQueue_ShouldRemoveAndReturnTrue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);

            // Act
            var result = await queueManager.RemoveQueueAsync("TestQueue");

            // Assert
            result.Should().BeTrue();
            queueManager.QueueCount.Should().Be(0);
            queueManager.GetQueue<string>("TestQueue").Should().BeNull();
        }

        [Fact]
        public async Task RemoveQueueAsync_NonExistentQueue_ShouldReturnFalse()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act
            var result = await queueManager.RemoveQueueAsync("NonExistent");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void QueueExists_ExistingQueue_ShouldReturnTrue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);

            // Act & Assert
            queueManager.QueueExists("TestQueue").Should().BeTrue();
        }

        [Fact]
        public void QueueExists_NonExistentQueue_ShouldReturnFalse()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act & Assert
            queueManager.QueueExists("NonExistent").Should().BeFalse();
        }

        [Fact]
        public void GetQueueNames_WithMultipleQueues_ShouldReturnAllNames()
        {
            // Arrange
            using var queueManager = new QueueManager();
            queueManager.CreateQueue<string>("Queue1", CreateTestConfiguration("Queue1"));
            queueManager.CreateQueue<int>("Queue2", CreateTestConfiguration("Queue2"));
            queueManager.CreateQueue<string>("Queue3", CreateTestConfiguration("Queue3"));

            // Act
            var queueNames = queueManager.GetQueueNames();

            // Assert
            queueNames.Should().HaveCount(3);
            queueNames.Should().Contain("Queue1", "Queue2", "Queue3");
        }

        [Fact]
        public async Task StartProcessingAsync_WithValidProcessor_ShouldReturnTrue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);
            var processor = new TestQueueProcessor<string>();

            // Act
            var result = await queueManager.StartProcessingAsync("TestQueue", processor);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task StartProcessingAsync_WithNonExistentQueue_ShouldReturnFalse()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var processor = new TestQueueProcessor<string>();

            // Act
            var result = await queueManager.StartProcessingAsync("NonExistent", processor);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task StopProcessingAsync_WithRunningProcessor_ShouldReturnTrue()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);
            var processor = new TestQueueProcessor<string>();
            await queueManager.StartProcessingAsync("TestQueue", processor);

            // Act
            var result = await queueManager.StopProcessingAsync("TestQueue");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task StopProcessingAsync_WithNonRunningProcessor_ShouldReturnFalse()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act
            var result = await queueManager.StopProcessingAsync("NonExistent");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetQueueStatisticsAsync_ExistingQueue_ShouldReturnStatistics()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            var queue = queueManager.CreateQueue<string>("TestQueue", config);
            
            // Add a message to get some statistics
            await queue.EnqueueAsync(new QueueMessage<string>("test"));

            // Act
            var stats = await queueManager.GetQueueStatisticsAsync("TestQueue");

            // Assert
            stats.Should().NotBeNull();
            stats!.QueuedCount.Should().Be(1);
        }

        [Fact]
        public async Task GetQueueStatisticsAsync_NonExistentQueue_ShouldReturnNull()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act
            var stats = await queueManager.GetQueueStatisticsAsync("NonExistent");

            // Assert
            stats.Should().BeNull();
        }

        [Fact]
        public async Task GetAllQueueStatisticsAsync_ShouldReturnAllStatistics()
        {
            // Arrange
            using var queueManager = new QueueManager();
            queueManager.CreateQueue<string>("Queue1", CreateTestConfiguration("Queue1"));
            queueManager.CreateQueue<string>("Queue2", CreateTestConfiguration("Queue2"));

            // Act
            var allStats = await queueManager.GetAllQueueStatisticsAsync();

            // Assert
            allStats.Should().HaveCount(2);
            allStats.Should().ContainKeys("Queue1", "Queue2");
        }

        [Fact]
        public void GetQueueHealth_HealthyQueue_ShouldReturnHealthy()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");
            queueManager.CreateQueue<string>("TestQueue", config);

            // Act
            var health = queueManager.GetQueueHealth("TestQueue");

            // Assert
            health.Should().Be(QueueHealthStatus.Healthy);
        }

        [Fact]
        public void GetQueueHealth_NonExistentQueue_ShouldReturnUnhealthy()
        {
            // Arrange
            using var queueManager = new QueueManager();

            // Act
            var health = queueManager.GetQueueHealth("NonExistent");

            // Assert
            health.Should().Be(QueueHealthStatus.Unhealthy);
        }

        [Fact]
        public void Events_ShouldFireCorrectly()
        {
            // Arrange
            using var queueManager = new QueueManager();
            var config = CreateTestConfiguration("TestQueue");

            var queueCreatedEventFired = false;
            var queueRemovedEventFired = false;

            queueManager.QueueCreated += (s, queueName) => 
            {
                queueName.Should().Be("TestQueue");
                queueCreatedEventFired = true;
            };

            queueManager.QueueRemoved += (s, queueName) => 
            {
                queueName.Should().Be("TestQueue");
                queueRemovedEventFired = true;
            };

            // Act
            queueManager.CreateQueue<string>("TestQueue", config);
            var removeTask = queueManager.RemoveQueueAsync("TestQueue");
            removeTask.Wait();

            // Assert
            queueCreatedEventFired.Should().BeTrue();
            queueRemovedEventFired.Should().BeTrue();
        }
    }

    /// <summary>
    /// Test implementation of IQueueProcessor for testing purposes
    /// </summary>
    internal class TestQueueProcessor<T> : IQueueProcessor<T>
    {
        public int MaxBatchSize => 10;
        public string ProcessorName => "TestProcessor";
        public bool SupportsBatchProcessing => true;

        public Task<ProcessingResult> ProcessAsync(QueueMessage<T> message)
        {
            return Task.FromResult(ProcessingResult.CreateSuccess(TimeSpan.FromMilliseconds(10)));
        }

        public Task<BatchProcessingResult> ProcessBatchAsync(System.Collections.Generic.List<QueueMessage<T>> messages)
        {
            var result = new BatchProcessingResult();
            foreach (var message in messages)
            {
                result.Results.Add(ProcessingResult.CreateSuccess(TimeSpan.FromMilliseconds(10)));
                result.SuccessCount++;
            }
            result.TotalProcessingTime = TimeSpan.FromMilliseconds(messages.Count * 10);
            return Task.FromResult(result);
        }

        public bool CanProcess(QueueMessage<T> message)
        {
            return message != null;
        }
    }
}