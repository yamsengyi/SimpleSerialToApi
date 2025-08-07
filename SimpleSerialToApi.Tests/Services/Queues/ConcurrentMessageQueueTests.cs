using FluentAssertions;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Queues
{
    /// <summary>
    /// Tests for ConcurrentMessageQueue implementation
    /// </summary>
    public class ConcurrentMessageQueueTests
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
        public async Task EnqueueAsync_ShouldAddMessageToQueue()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            var message = new QueueMessage<string>("test payload");

            // Act
            var result = await queue.EnqueueAsync(message);

            // Assert
            result.Should().BeTrue();
            queue.Count.Should().Be(1);
            queue.IsEmpty.Should().BeFalse();
        }

        [Fact]
        public async Task DequeueAsync_ShouldReturnAndRemoveMessage()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            var originalMessage = new QueueMessage<string>("test payload");
            await queue.EnqueueAsync(originalMessage);

            // Act
            var dequeuedMessage = await queue.DequeueAsync();

            // Assert
            dequeuedMessage.Should().NotBeNull();
            dequeuedMessage!.Payload.Should().Be("test payload");
            dequeuedMessage.Status.Should().Be(MessageStatus.Processing);
            dequeuedMessage.ProcessingStartTime.Should().NotBeNull();
            queue.ProcessingCount.Should().Be(1);
        }

        [Fact]
        public async Task DequeueAsync_WhenEmpty_ShouldReturnNull()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Act
            var message = await queue.DequeueAsync();

            // Assert
            message.Should().BeNull();
        }

        [Fact]
        public async Task DequeueBatchAsync_ShouldReturnMultipleMessages()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            
            var messages = new[]
            {
                new QueueMessage<string>("message 1"),
                new QueueMessage<string>("message 2"),
                new QueueMessage<string>("message 3")
            };

            foreach (var msg in messages)
            {
                await queue.EnqueueAsync(msg);
            }

            // Act
            var batch = await queue.DequeueBatchAsync(2);

            // Assert
            batch.Should().HaveCount(2);
            batch.All(m => m.Status == MessageStatus.Processing).Should().BeTrue();
            queue.ProcessingCount.Should().Be(2);
            queue.Count.Should().Be(1); // One remaining
        }

        [Fact]
        public async Task EnqueueAsync_WhenAtCapacity_ShouldReturnFalse()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.MaxSize = 2;
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Fill to capacity
            await queue.EnqueueAsync(new QueueMessage<string>("message 1"));
            await queue.EnqueueAsync(new QueueMessage<string>("message 2"));

            // Act
            var result = await queue.EnqueueAsync(new QueueMessage<string>("message 3"));

            // Assert
            result.Should().BeFalse();
            queue.Count.Should().Be(2);
        }

        [Fact]
        public async Task RequeueAsync_ShouldReturnMessageToQueue()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            var message = new QueueMessage<string>("test payload");
            await queue.EnqueueAsync(message);
            var dequeuedMessage = await queue.DequeueAsync();

            // Act
            var result = await queue.RequeueAsync(dequeuedMessage!);

            // Assert
            result.Should().BeTrue();
            dequeuedMessage!.RetryCount.Should().Be(1);
            dequeuedMessage.Status.Should().Be(MessageStatus.Queued);
        }

        [Fact]
        public async Task RequeueAsync_WhenMaxRetriesExceeded_ShouldMoveToDeadLetter()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.RetryCount = 2;
            using var queue = new ConcurrentMessageQueue<string>(config);
            var message = new QueueMessage<string>("test payload");
            await queue.EnqueueAsync(message);
            var dequeuedMessage = await queue.DequeueAsync();

            // Exceed retry count
            dequeuedMessage!.RetryCount = 2;

            // Act
            var result = await queue.RequeueAsync(dequeuedMessage);

            // Assert
            result.Should().BeTrue();
            dequeuedMessage.Status.Should().Be(MessageStatus.DeadLetter);
            queue.ProcessingCount.Should().Be(0);
            
            var deadLetterMessages = await queue.GetDeadLetterMessagesAsync();
            deadLetterMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task PriorityQueue_ShouldProcessHigherPriorityFirst()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.EnablePriority = true;
            using var queue = new ConcurrentMessageQueue<string>(config);

            var lowPriorityMsg = new QueueMessage<string>("low priority", priority: 1);
            var highPriorityMsg = new QueueMessage<string>("high priority", priority: 10);
            var mediumPriorityMsg = new QueueMessage<string>("medium priority", priority: 5);

            // Enqueue in mixed order
            await queue.EnqueueAsync(lowPriorityMsg);
            await queue.EnqueueAsync(highPriorityMsg);
            await queue.EnqueueAsync(mediumPriorityMsg);

            // Act & Assert
            var firstMessage = await queue.DequeueAsync();
            firstMessage!.Payload.Should().Be("high priority");

            var secondMessage = await queue.DequeueAsync();
            secondMessage!.Payload.Should().Be("medium priority");

            var thirdMessage = await queue.DequeueAsync();
            thirdMessage!.Payload.Should().Be("low priority");
        }

        [Fact]
        public async Task GetStatistics_ShouldReturnAccurateMetrics()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Add some messages
            await queue.EnqueueAsync(new QueueMessage<string>("message 1"));
            await queue.EnqueueAsync(new QueueMessage<string>("message 2"));
            var message = await queue.DequeueAsync();

            // Mark one as completed
            queue.MarkMessageCompleted(message!.MessageId, TimeSpan.FromMilliseconds(50));

            // Act
            var stats = queue.GetStatistics();

            // Assert
            stats.QueuedCount.Should().Be(1);
            stats.ProcessingCount.Should().Be(0);
            stats.CompletedCount.Should().Be(1);
            stats.AverageProcessingTimeMs.Should().Be(50.0);
        }

        [Fact]
        public async Task ClearAsync_ShouldRemoveAllMessages()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            await queue.EnqueueAsync(new QueueMessage<string>("message 1"));
            await queue.EnqueueAsync(new QueueMessage<string>("message 2"));

            // Act
            await queue.ClearAsync();

            // Assert
            queue.Count.Should().Be(0);
            queue.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public async Task Events_ShouldFireCorrectly()
        {
            // Arrange
            var config = CreateTestConfiguration();
            using var queue = new ConcurrentMessageQueue<string>(config);
            
            var enqueuedEventFired = false;
            var dequeuedEventFired = false;
            var deadLetterEventFired = false;

            queue.MessageEnqueued += (s, e) => enqueuedEventFired = true;
            queue.MessageDequeued += (s, e) => dequeuedEventFired = true;
            queue.MessageMovedToDeadLetter += (s, e) => deadLetterEventFired = true;

            var message = new QueueMessage<string>("test");

            // Act
            await queue.EnqueueAsync(message);
            var dequeuedMessage = await queue.DequeueAsync();
            await queue.MoveToDeadLetterAsync(dequeuedMessage!);

            // Assert
            enqueuedEventFired.Should().BeTrue();
            dequeuedEventFired.Should().BeTrue();
            deadLetterEventFired.Should().BeTrue();
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentOperations_ShouldWork()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.MaxSize = 1000;
            using var queue = new ConcurrentMessageQueue<string>(config);

            const int taskCount = 10;
            const int messagesPerTask = 10;

            // Act
            var tasks = Enumerable.Range(0, taskCount).Select(async taskId =>
            {
                for (int i = 0; i < messagesPerTask; i++)
                {
                    var message = new QueueMessage<string>($"Task-{taskId}-Message-{i}");
                    await queue.EnqueueAsync(message);
                }
            });

            await Task.WhenAll(tasks);

            // Assert
            queue.Count.Should().Be(taskCount * messagesPerTask);

            // Dequeue all and verify
            var allMessages = new List<QueueMessage<string>>();
            while (!queue.IsEmpty)
            {
                var message = await queue.DequeueAsync();
                if (message != null)
                {
                    allMessages.Add(message);
                }
            }

            allMessages.Should().HaveCount(taskCount * messagesPerTask);
            allMessages.Select(m => m.Payload).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Properties_ShouldReturnCorrectValues()
        {
            // Arrange
            var config = CreateTestConfiguration("MyQueue");
            config.MaxSize = 50;
            using var queue = new ConcurrentMessageQueue<string>(config);

            // Assert
            queue.Name.Should().Be("MyQueue");
            queue.MaxCapacity.Should().Be(50);
            queue.IsEmpty.Should().BeTrue();
            queue.IsFull.Should().BeFalse();
        }
    }
}