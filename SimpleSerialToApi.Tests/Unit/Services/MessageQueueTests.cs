using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Unit.Services
{
    [TestClass]
    public class MessageQueueTests : TestBase
    {
        private ConcurrentMessageQueue<string>? _queue;
        private QueueConfiguration? _config;

        [TestInitialize]
        public void Setup()
        {
            _config = new QueueConfiguration { MaxSize = 100 };
            _queue = new ConcurrentMessageQueue<string>(_config);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _queue?.Dispose();
        }

        [TestMethod]
        public async Task EnqueueAsync_WithValidMessage_ShouldReturnTrue()
        {
            // Arrange
            var message = new QueueMessage<string> { Payload = "Test Message" };

            // Act
            var result = await _queue!.EnqueueAsync(message);

            // Assert
            result.Should().BeTrue();
            _queue.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task DequeueAsync_WithMessages_ShouldReturnMessage()
        {
            // Arrange
            var originalMessage = new QueueMessage<string> { Payload = "Test Message" };
            await _queue!.EnqueueAsync(originalMessage);

            // Act
            var dequeuedMessage = await _queue.DequeueAsync();

            // Assert
            dequeuedMessage.Should().NotBeNull();
            dequeuedMessage!.Payload.Should().Be("Test Message");
            _queue.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task DequeueAsync_WithEmptyQueue_ShouldReturnNull()
        {
            // Act
            var result = await _queue!.DequeueAsync();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public async Task EnqueueAsync_WhenQueueIsFull_ShouldReturnFalse()
        {
            // Arrange - Fill queue to capacity
            var smallConfig = new QueueConfiguration { MaxSize = 2 };
            using var smallQueue = new ConcurrentMessageQueue<string>(smallConfig);

            await smallQueue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 1" });
            await smallQueue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 2" });

            // Act
            var result = await smallQueue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 3" });

            // Assert
            result.Should().BeFalse();
            smallQueue.Count.Should().Be(2);
        }

        [TestMethod]
        public async Task ConcurrentOperations_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task>();
            var messageCount = 100;

            // Act - Enqueue messages concurrently
            for (int i = 0; i < messageCount; i++)
            {
                var messageIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    var message = new QueueMessage<string> { Payload = $"Message {messageIndex}" };
                    await _queue!.EnqueueAsync(message);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            _queue!.Count.Should().Be(messageCount);
        }

        [TestMethod]
        public async Task DequeueAsync_ConcurrentReaders_ShouldNotReturnDuplicates()
        {
            // Arrange
            var messageCount = 50;
            var readerCount = 5;

            // Fill queue
            for (int i = 0; i < messageCount; i++)
            {
                await _queue!.EnqueueAsync(new QueueMessage<string> { Payload = $"Message {i}" });
            }

            var receivedMessages = new List<string>();
            var lockObject = new object();

            // Act - Start multiple readers
            var readerTasks = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                readerTasks[i] = Task.Run(async () =>
                {
                    while (true)
                    {
                        var message = await _queue.DequeueAsync();
                        if (message == null) break;

                        lock (lockObject)
                        {
                            receivedMessages.Add(message.Payload);
                        }
                    }
                });
            }

            await Task.WhenAll(readerTasks);

            // Assert
            receivedMessages.Should().HaveCount(messageCount);
            receivedMessages.Should().OnlyHaveUniqueItems("No message should be returned twice");
            _queue.Count.Should().Be(0);
        }

        [TestMethod]
        public void Count_ShouldReflectCurrentQueueSize()
        {
            // Arrange & Act - Initial state
            _queue!.Count.Should().Be(0);

            // Add items
            var task1 = _queue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 1" });
            task1.Wait();
            _queue.Count.Should().Be(1);

            var task2 = _queue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 2" });
            task2.Wait();
            _queue.Count.Should().Be(2);

            // Remove item
            var dequeueTask = _queue.DequeueAsync();
            dequeueTask.Wait();
            _queue.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task EnqueueAsync_WithNullMessage_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Func<Task> act = async () => await _queue!.EnqueueAsync(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConcurrentMessageQueue<string>(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithInvalidMaxSize_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidConfig = new QueueConfiguration { MaxSize = 0 };

            // Act & Assert
            Action act = () => new ConcurrentMessageQueue<string>(invalidConfig);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task Clear_ShouldEmptyQueue()
        {
            // Arrange
            await _queue!.EnqueueAsync(new QueueMessage<string> { Payload = "Message 1" });
            await _queue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 2" });
            await _queue.EnqueueAsync(new QueueMessage<string> { Payload = "Message 3" });

            // Act
            _queue.Clear();

            // Assert
            _queue.Count.Should().Be(0);
            var message = await _queue.DequeueAsync();
            message.Should().BeNull();
        }

        [TestMethod]
        public async Task Peek_ShouldReturnFirstMessageWithoutRemoving()
        {
            // Arrange
            var firstMessage = new QueueMessage<string> { Payload = "First Message" };
            var secondMessage = new QueueMessage<string> { Payload = "Second Message" };
            
            await _queue!.EnqueueAsync(firstMessage);
            await _queue.EnqueueAsync(secondMessage);

            // Act
            var peekedMessage = _queue.Peek();

            // Assert
            peekedMessage.Should().NotBeNull();
            peekedMessage!.Payload.Should().Be("First Message");
            _queue.Count.Should().Be(2); // Should not remove the message
        }

        [TestMethod]
        public void Peek_WithEmptyQueue_ShouldReturnNull()
        {
            // Act
            var result = _queue!.Peek();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public async Task IsEmpty_ShouldReflectQueueState()
        {
            // Arrange & Act - Initial state
            _queue!.IsEmpty.Should().BeTrue();

            // Add item
            await _queue.EnqueueAsync(new QueueMessage<string> { Payload = "Message" });
            _queue.IsEmpty.Should().BeFalse();

            // Remove item
            await _queue.DequeueAsync();
            _queue.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public async Task IsFull_ShouldReflectQueueState()
        {
            // Arrange
            var smallConfig = new QueueConfiguration { MaxSize = 1 };
            using var smallQueue = new ConcurrentMessageQueue<string>(smallConfig);

            // Act & Assert - Initial state
            smallQueue.IsFull.Should().BeFalse();

            // Fill queue
            await smallQueue.EnqueueAsync(new QueueMessage<string> { Payload = "Message" });
            smallQueue.IsFull.Should().BeTrue();

            // Remove item
            await smallQueue.DequeueAsync();
            smallQueue.IsFull.Should().BeFalse();
        }

        [TestMethod]
        public async Task DequeueAsync_WithTimeout_ShouldRespectTimeout()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(100);

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _queue!.DequeueAsync(timeout);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            result.Should().BeNull();
            elapsed.Should().BeGreaterOrEqualTo(timeout);
        }

        [TestMethod]
        public async Task DequeueAsync_WithTimeoutAndMessage_ShouldReturnImmediately()
        {
            // Arrange
            var message = new QueueMessage<string> { Payload = "Test" };
            await _queue!.EnqueueAsync(message);
            var timeout = TimeSpan.FromSeconds(10); // Long timeout

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _queue.DequeueAsync(timeout);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            result.Should().NotBeNull();
            result!.Payload.Should().Be("Test");
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Should return quickly
        }

        [TestMethod]
        public void Dispose_ShouldReleaseResources()
        {
            // Arrange
            var disposableQueue = new ConcurrentMessageQueue<string>(_config!);

            // Act & Assert - Should not throw
            Action act = () => disposableQueue.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task HighLoad_ShouldMaintainPerformance()
        {
            // Arrange
            var largeConfig = new QueueConfiguration { MaxSize = 10000 };
            using var largeQueue = new ConcurrentMessageQueue<string>(largeConfig);
            var messageCount = 1000;

            // Act
            var startTime = DateTime.UtcNow;

            // Enqueue messages
            var enqueueTasks = Enumerable.Range(0, messageCount)
                .Select(i => largeQueue.EnqueueAsync(new QueueMessage<string> { Payload = $"Message {i}" }))
                .ToArray();
            await Task.WhenAll(enqueueTasks);

            // Dequeue messages
            var dequeueTasks = Enumerable.Range(0, messageCount)
                .Select(_ => largeQueue.DequeueAsync())
                .ToArray();
            var results = await Task.WhenAll(dequeueTasks);

            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "High load operations should complete quickly");
            results.Should().NotContain(r => r == null, "All messages should be retrieved");
            results.Select(r => r!.Payload).Should().OnlyHaveUniqueItems();
            largeQueue.Count.Should().Be(0);
        }
    }
}