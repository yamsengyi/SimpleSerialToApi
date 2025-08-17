using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThreadingTimer = System.Threading.Timer;

namespace SimpleSerialToApi.Services.Queues
{
    /// <summary>
    /// Thread-safe message queue implementation using ConcurrentQueue
    /// </summary>
    /// <typeparam name="T">Type of messages stored in the queue</typeparam>
    public class ConcurrentMessageQueue<T> : IMessageQueue<T>
    {
        private readonly ConcurrentQueue<QueueMessage<T>> _mainQueue;
        private readonly ConcurrentQueue<QueueMessage<T>> _deadLetterQueue;
        private readonly ConcurrentDictionary<string, QueueMessage<T>> _processingMessages;
        private readonly QueueConfiguration _configuration;
        private readonly QueueStatistics _statistics;
        private readonly SemaphoreSlim _semaphore;
        private readonly object _statsLock = new object();
        private readonly ThreadingTimer _statisticsUpdateTimer;
        private volatile bool _disposed = false;

        /// <summary>
        /// Event raised when a message is enqueued
        /// </summary>
        public event EventHandler<QueueMessage<T>>? MessageEnqueued;

        /// <summary>
        /// Event raised when a message is dequeued
        /// </summary>
        public event EventHandler<QueueMessage<T>>? MessageDequeued;

        /// <summary>
        /// Event raised when a message is moved to dead letter queue
        /// </summary>
        public event EventHandler<QueueMessage<T>>? MessageMovedToDeadLetter;

        /// <summary>
        /// Event raised when queue reaches capacity
        /// </summary>
        public event EventHandler<EventArgs>? QueueFull;

        /// <summary>
        /// Event raised when queue becomes empty
        /// </summary>
        public event EventHandler<EventArgs>? QueueEmpty;

        /// <summary>
        /// Current number of messages in the queue
        /// </summary>
        public int Count => _mainQueue.Count;

        /// <summary>
        /// Current number of messages being processed
        /// </summary>
        public int ProcessingCount => _processingMessages.Count;

        /// <summary>
        /// Whether the queue is empty
        /// </summary>
        public bool IsEmpty => _mainQueue.IsEmpty && _processingMessages.IsEmpty;

        /// <summary>
        /// Whether the queue is at capacity
        /// </summary>
        public bool IsFull => Count >= MaxCapacity;

        /// <summary>
        /// Maximum capacity of the queue
        /// </summary>
        public int MaxCapacity => _configuration.MaxSize;

        /// <summary>
        /// Name of the queue
        /// </summary>
        public string Name => _configuration.Name;

        /// <summary>
        /// Creates a new concurrent message queue
        /// </summary>
        /// <param name="configuration">Queue configuration</param>
        public ConcurrentMessageQueue(QueueConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mainQueue = new ConcurrentQueue<QueueMessage<T>>();
            _deadLetterQueue = new ConcurrentQueue<QueueMessage<T>>();
            _processingMessages = new ConcurrentDictionary<string, QueueMessage<T>>();
            _statistics = new QueueStatistics();
            _semaphore = new SemaphoreSlim(configuration.MaxSize, configuration.MaxSize);

            // Update statistics every 5 seconds
            _statisticsUpdateTimer = new ThreadingTimer(UpdateStatistics, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Adds a message to the queue
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        /// <returns>True if message was successfully enqueued</returns>
        public async Task<bool> EnqueueAsync(QueueMessage<T> message)
        {
            if (_disposed) return false;
            if (message == null) return false;

            // Check capacity
            if (!await _semaphore.WaitAsync(0))
            {
                QueueFull?.Invoke(this, EventArgs.Empty);
                return false;
            }

            try
            {
                message.EnqueueTime = DateTime.UtcNow;
                message.Status = MessageStatus.Queued;

                if (_configuration.EnablePriority)
                {
                    // For priority queues, we need a more sophisticated approach
                    // For now, we'll use simple FIFO and handle priority during dequeue
                    _mainQueue.Enqueue(message);
                }
                else
                {
                    _mainQueue.Enqueue(message);
                }

                lock (_statsLock)
                {
                    _statistics.PeakQueueSize = Math.Max(_statistics.PeakQueueSize, Count);
                }

                MessageEnqueued?.Invoke(this, message);

                return true;
            }
            catch
            {
                _semaphore.Release();
                return false;
            }
        }

        /// <summary>
        /// Removes and returns the next message from the queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next message or null if queue is empty</returns>
        public async Task<QueueMessage<T>?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return null;

            QueueMessage<T>? message = null;

            if (_configuration.EnablePriority)
            {
                message = DequeueWithPriority();
            }
            else
            {
                _mainQueue.TryDequeue(out message);
            }

            if (message != null)
            {
                message.ProcessingStartTime = DateTime.UtcNow;
                message.Status = MessageStatus.Processing;
                _processingMessages.TryAdd(message.MessageId, message);

                MessageDequeued?.Invoke(this, message);
                _semaphore.Release();

                if (_mainQueue.IsEmpty)
                {
                    QueueEmpty?.Invoke(this, EventArgs.Empty);
                }
            }

            return message;
        }

        /// <summary>
        /// Removes and returns a batch of messages from the queue
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of messages (may be empty)</returns>
        public async Task<List<QueueMessage<T>>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            if (_disposed) return new List<QueueMessage<T>>();

            var messages = new List<QueueMessage<T>>();
            int actualBatchSize = Math.Min(batchSize, _configuration.BatchSize);

            for (int i = 0; i < actualBatchSize && !cancellationToken.IsCancellationRequested; i++)
            {
                var message = await DequeueAsync(cancellationToken);
                if (message == null) break;
                messages.Add(message);
            }

            return messages;
        }

        /// <summary>
        /// Returns a message to the queue for retry
        /// </summary>
        /// <param name="message">Message to requeue</param>
        /// <returns>True if message was successfully requeued</returns>
        public async Task<bool> RequeueAsync(QueueMessage<T> message)
        {
            if (_disposed || message == null) return false;

            // Remove from processing
            _processingMessages.TryRemove(message.MessageId, out _);

            // Check if we should retry
            if (message.RetryCount >= _configuration.RetryCount)
            {
                return await MoveToDeadLetterAsync(message);
            }

            // Calculate retry delay with exponential backoff
            var delay = TimeSpan.FromMilliseconds(
                _configuration.RetryIntervalMs * Math.Pow(2, message.RetryCount));

            // Schedule retry
            message.RetryCount++;
            message.Status = MessageStatus.Queued;
            message.ProcessingStartTime = null;
            message.LastProcessingAttempt = DateTime.UtcNow;

            // Wait for retry delay
            await Task.Delay(delay);

            return await EnqueueAsync(message);
        }

        /// <summary>
        /// Moves a message to the dead letter queue
        /// </summary>
        /// <param name="message">Message to move</param>
        /// <returns>True if message was successfully moved</returns>
        public async Task<bool> MoveToDeadLetterAsync(QueueMessage<T> message)
        {
            if (_disposed || message == null) return false;

            // Remove from processing
            _processingMessages.TryRemove(message.MessageId, out _);

            message.Status = MessageStatus.DeadLetter;
            _deadLetterQueue.Enqueue(message);

            lock (_statsLock)
            {
                _statistics.DeadLetterCount++;
            }

            MessageMovedToDeadLetter?.Invoke(this, message);
            return true;
        }

        /// <summary>
        /// Peeks at the next message without removing it
        /// </summary>
        /// <returns>The next message or null if queue is empty</returns>
        public async Task<QueueMessage<T>?> PeekAsync()
        {
            if (_disposed) return null;

            if (_configuration.EnablePriority)
            {
                return PeekWithPriority();
            }
            else
            {
                _mainQueue.TryPeek(out var message);
                return message;
            }
        }

        /// <summary>
        /// Clears all messages from the queue
        /// </summary>
        public async Task ClearAsync()
        {
            if (_disposed) return;

            // Clear main queue
            while (_mainQueue.TryDequeue(out var message))
            {
                _semaphore.Release();
            }

            // Clear processing messages
            _processingMessages.Clear();

            lock (_statsLock)
            {
                _statistics.QueuedCount = 0;
                _statistics.ProcessingCount = 0;
            }
        }

        /// <summary>
        /// Gets messages from dead letter queue
        /// </summary>
        /// <param name="count">Maximum number of messages to retrieve</param>
        /// <returns>List of dead letter messages</returns>
        public async Task<List<QueueMessage<T>>> GetDeadLetterMessagesAsync(int count = 100)
        {
            if (_disposed) return new List<QueueMessage<T>>();

            var messages = new List<QueueMessage<T>>();
            var tempQueue = new Queue<QueueMessage<T>>();

            // Dequeue messages to return them
            for (int i = 0; i < count && _deadLetterQueue.TryDequeue(out var message); i++)
            {
                messages.Add(message);
                tempQueue.Enqueue(message);
            }

            // Put them back
            while (tempQueue.Count > 0)
            {
                _deadLetterQueue.Enqueue(tempQueue.Dequeue());
            }

            return messages;
        }

        /// <summary>
        /// Gets current queue statistics
        /// </summary>
        /// <returns>Queue statistics</returns>
        public QueueStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                _statistics.QueuedCount = Count;
                _statistics.ProcessingCount = ProcessingCount;
                _statistics.LastUpdated = DateTime.UtcNow;
                
                // Estimate memory usage (rough calculation)
                _statistics.MemoryUsageBytes = (Count + ProcessingCount) * 1024; // Assume ~1KB per message

                return new QueueStatistics
                {
                    QueuedCount = _statistics.QueuedCount,
                    ProcessingCount = _statistics.ProcessingCount,
                    CompletedCount = _statistics.CompletedCount,
                    FailedCount = _statistics.FailedCount,
                    DeadLetterCount = _statistics.DeadLetterCount,
                    AverageProcessingTimeMs = _statistics.AverageProcessingTimeMs,
                    Throughput = _statistics.Throughput,
                    LastUpdated = _statistics.LastUpdated,
                    StartTime = _statistics.StartTime,
                    PeakQueueSize = _statistics.PeakQueueSize,
                    MemoryUsageBytes = _statistics.MemoryUsageBytes
                };
            }
        }

        /// <summary>
        /// Marks a message as completed
        /// </summary>
        /// <param name="messageId">ID of the completed message</param>
        /// <param name="processingTime">Time taken to process the message</param>
        public void MarkMessageCompleted(string messageId, TimeSpan processingTime)
        {
            if (_processingMessages.TryRemove(messageId, out var message))
            {
                message.Status = MessageStatus.Completed;

                lock (_statsLock)
                {
                    _statistics.CompletedCount++;
                    UpdateAverageProcessingTime(processingTime.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        /// Marks a message as failed
        /// </summary>
        /// <param name="messageId">ID of the failed message</param>
        /// <param name="error">Error message</param>
        public void MarkMessageFailed(string messageId, string error)
        {
            if (_processingMessages.TryRemove(messageId, out var message))
            {
                message.Status = MessageStatus.Failed;
                message.LastError = error;

                lock (_statsLock)
                {
                    _statistics.FailedCount++;
                }
            }
        }

        #region Private Methods

        private QueueMessage<T>? DequeueWithPriority()
        {
            var tempList = new List<QueueMessage<T>>();
            QueueMessage<T>? highestPriorityMessage = null;
            int highestPriority = int.MinValue;

            // Collect messages to examine priority
            while (_mainQueue.TryDequeue(out var message))
            {
                if (message.Priority > highestPriority)
                {
                    if (highestPriorityMessage != null)
                    {
                        tempList.Add(highestPriorityMessage);
                    }
                    highestPriorityMessage = message;
                    highestPriority = message.Priority;
                }
                else
                {
                    tempList.Add(message);
                }
            }

            // Put back the non-selected messages
            foreach (var msg in tempList)
            {
                _mainQueue.Enqueue(msg);
            }

            return highestPriorityMessage;
        }

        private QueueMessage<T>? PeekWithPriority()
        {
            var tempList = new List<QueueMessage<T>>();
            QueueMessage<T>? highestPriorityMessage = null;
            int highestPriority = int.MinValue;

            // Collect all messages to examine priority
            while (_mainQueue.TryDequeue(out var message))
            {
                tempList.Add(message);
                if (message.Priority > highestPriority)
                {
                    highestPriorityMessage = message;
                    highestPriority = message.Priority;
                }
            }

            // Put back all messages
            foreach (var msg in tempList)
            {
                _mainQueue.Enqueue(msg);
            }

            return highestPriorityMessage;
        }

        private void UpdateAverageProcessingTime(double newProcessingTimeMs)
        {
            if (_statistics.CompletedCount == 1)
            {
                _statistics.AverageProcessingTimeMs = newProcessingTimeMs;
            }
            else
            {
                // Running average calculation
                _statistics.AverageProcessingTimeMs = 
                    (_statistics.AverageProcessingTimeMs * (_statistics.CompletedCount - 1) + newProcessingTimeMs) 
                    / _statistics.CompletedCount;
            }
        }

        private void UpdateStatistics(object? state)
        {
            if (_disposed) return;

            lock (_statsLock)
            {
                var now = DateTime.UtcNow;
                var elapsedSeconds = (now - _statistics.StartTime).TotalSeconds;
                
                if (elapsedSeconds > 0)
                {
                    _statistics.Throughput = _statistics.CompletedCount / elapsedSeconds;
                }

                _statistics.LastUpdated = now;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _statisticsUpdateTimer?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}