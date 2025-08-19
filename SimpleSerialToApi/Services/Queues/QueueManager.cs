using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Services.Queues
{
    /// <summary>
    /// Queue processor wrapper for managing processing state
    /// </summary>
    internal class QueueProcessorWrapper
    {
        public object Processor { get; set; } = null!;
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        public Task? ProcessingTask { get; set; }
        public bool IsRunning { get; set; }
        public Type MessageType { get; set; } = null!;
    }

    /// <summary>
    /// Queue manager implementation for managing multiple message queues
    /// </summary>
    public class QueueManager : IQueueManager
    {
        private readonly ConcurrentDictionary<string, object> _queues;
        private readonly ConcurrentDictionary<string, QueueProcessorWrapper> _processors;
        private readonly ConcurrentDictionary<string, Type> _queueTypes;
        private volatile bool _disposed = false;

        /// <summary>
        /// Event raised when a queue is created
        /// </summary>
        public event EventHandler<string>? QueueCreated;

        /// <summary>
        /// Event raised when a queue is removed
        /// </summary>
        public event EventHandler<string>? QueueRemoved;

        /// <summary>
        /// Event raised when processing starts for a queue
        /// </summary>
        public event EventHandler<string>? ProcessingStarted;

        /// <summary>
        /// Event raised when processing stops for a queue
        /// </summary>
        public event EventHandler<string>? ProcessingStopped;

        /// <summary>
        /// Event raised when a queue health status changes
        /// </summary>
        public event EventHandler<(string QueueName, QueueHealthStatus Status)>? QueueHealthChanged;

        /// <summary>
        /// Gets the total number of managed queues
        /// </summary>
        public int QueueCount => _queues.Count;

        /// <summary>
        /// Creates a new queue manager
        /// </summary>
        public QueueManager()
        {
            _queues = new ConcurrentDictionary<string, object>();
            _processors = new ConcurrentDictionary<string, QueueProcessorWrapper>();
            _queueTypes = new ConcurrentDictionary<string, Type>();
        }

        /// <summary>
        /// Gets the total number of managed queues (compatibility method)
        /// </summary>
        public int GetQueueCount()
        {
            return _queues.Count;
        }

        /// <summary>
        /// Gets the message count for a specific queue
        /// </summary>
        public int GetMessageCount(string queueName)
        {
            if (string.IsNullOrEmpty(queueName)) return 0;
            if (_queues.TryGetValue(queueName, out var queueObj))
            {
                var prop = queueObj.GetType().GetProperty("Count");
                if (prop != null && prop.GetValue(queueObj) is int count)
                    return count;
            }
            return 0;
        }

        /// <summary>
        /// Gets the capacity for a specific queue
        /// </summary>
        public int GetQueueCapacity(string queueName)
        {
            if (string.IsNullOrEmpty(queueName)) return 0;
            if (_queues.TryGetValue(queueName, out var queueObj))
            {
                var prop = queueObj.GetType().GetProperty("Capacity");
                if (prop != null && prop.GetValue(queueObj) is int capacity)
                    return capacity;
            }
            return 0;
        }

        /// <summary>
        /// Creates a new queue with the specified configuration
        /// </summary>
        /// <typeparam name="T">Type of messages for the queue</typeparam>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="config">Queue configuration</param>
        /// <returns>The created queue</returns>
        public IMessageQueue<T> CreateQueue<T>(string queueName, QueueConfiguration config)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Name = queueName;
            var queue = new ConcurrentMessageQueue<T>(config);

            if (!_queues.TryAdd(queueName, queue))
            {
                throw new InvalidOperationException($"Queue '{queueName}' already exists");
            }

            _queueTypes.TryAdd(queueName, typeof(T));
            QueueCreated?.Invoke(this, queueName);

            return queue;
        }

        /// <summary>
        /// Gets an existing queue by name
        /// </summary>
        /// <typeparam name="T">Type of messages for the queue</typeparam>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>The queue or null if not found</returns>
        public IMessageQueue<T>? GetQueue<T>(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
                return null;

            if (_queues.TryGetValue(queueName, out var queueObj) && queueObj is IMessageQueue<T> queue)
            {
                return queue;
            }

            return null;
        }

        /// <summary>
        /// Removes and disposes a queue
        /// </summary>
        /// <param name="queueName">Name of the queue to remove</param>
        /// <returns>True if queue was found and removed</returns>
        public async Task<bool> RemoveQueueAsync(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
                return false;

            // Stop processing first
            await StopProcessingAsync(queueName);

            if (_queues.TryRemove(queueName, out var queueObj))
            {
                if (queueObj is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _queueTypes.TryRemove(queueName, out _);
                QueueRemoved?.Invoke(this, queueName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts processing for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="processor">Processor to use for the queue</param>
        /// <returns>True if processing was started</returns>
        public async Task<bool> StartProcessingAsync<T>(string queueName, IQueueProcessor<T> processor)
        {
            if (string.IsNullOrEmpty(queueName) || processor == null)
                return false;

            var queue = GetQueue<T>(queueName);
            if (queue == null)
                return false;

            // Check if already processing
            if (_processors.TryGetValue(queueName, out var existingWrapper) && existingWrapper.IsRunning)
                return false;

            var cancellationTokenSource = new CancellationTokenSource();
            var processingTask = StartQueueProcessingLoop(queue, processor, cancellationTokenSource.Token);

            var wrapper = new QueueProcessorWrapper
            {
                Processor = processor,
                CancellationTokenSource = cancellationTokenSource,
                ProcessingTask = processingTask,
                IsRunning = true,
                MessageType = typeof(T)
            };

            _processors.AddOrUpdate(queueName, wrapper, (key, old) =>
            {
                old.CancellationTokenSource?.Cancel();
                old.CancellationTokenSource?.Dispose();
                return wrapper;
            });

            ProcessingStarted?.Invoke(this, queueName);
            return true;
        }

        /// <summary>
        /// Stops processing for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>True if processing was stopped</returns>
        public async Task<bool> StopProcessingAsync(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
                return false;

            if (_processors.TryRemove(queueName, out var wrapper))
            {
                wrapper.IsRunning = false;
                wrapper.CancellationTokenSource?.Cancel();

                try
                {
                    if (wrapper.ProcessingTask != null)
                    {
                        await wrapper.ProcessingTask;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                finally
                {
                    wrapper.CancellationTokenSource?.Dispose();
                }

                ProcessingStopped?.Invoke(this, queueName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the health status of a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>Health status of the queue</returns>
        public QueueHealthStatus GetQueueHealth(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
                return QueueHealthStatus.Unhealthy;

            if (!_queues.TryGetValue(queueName, out var queueObj))
                return QueueHealthStatus.Unhealthy;

            // Use reflection to get queue statistics regardless of generic type
            var getStatisticsMethod = queueObj.GetType().GetMethod("GetStatistics");
            if (getStatisticsMethod?.Invoke(queueObj, null) is QueueStatistics stats)
            {
                // Check processing health
                if (!_processors.TryGetValue(queueName, out var wrapper) || !wrapper.IsRunning)
                {
                    if (stats.QueuedCount > 0)
                        return QueueHealthStatus.Warning; // Messages accumulating but no processor
                }

                // Check queue capacity
                var isFullProperty = queueObj.GetType().GetProperty("IsFull");
                if (isFullProperty?.GetValue(queueObj) is bool isFull && isFull)
                {
                    return QueueHealthStatus.Critical;
                }

                // Check failure rate
                var totalMessages = stats.CompletedCount + stats.FailedCount;
                if (totalMessages > 10) // Only check if we have enough data
                {
                    var failureRate = (double)stats.FailedCount / totalMessages;
                    if (failureRate > 0.5) // More than 50% failure rate
                        return QueueHealthStatus.Critical;
                    if (failureRate > 0.1) // More than 10% failure rate
                        return QueueHealthStatus.Warning;
                }

                // Check processing time
                if (stats.AverageProcessingTimeMs > 10000) // More than 10 seconds average
                    return QueueHealthStatus.Warning;

                return QueueHealthStatus.Healthy;
            }

            return QueueHealthStatus.Unhealthy;
        }

        /// <summary>
        /// Gets statistics for all queues
        /// </summary>
        /// <returns>Dictionary of queue statistics by queue name</returns>
        public async Task<Dictionary<string, QueueStatistics>> GetAllQueueStatisticsAsync()
        {
            var result = new Dictionary<string, QueueStatistics>();

            foreach (var kvp in _queues)
            {
                var stats = await GetQueueStatisticsAsync(kvp.Key);
                if (stats != null)
                {
                    result[kvp.Key] = stats;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets statistics for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>Queue statistics or null if queue not found</returns>
        public async Task<QueueStatistics?> GetQueueStatisticsAsync(string queueName)
        {
            if (string.IsNullOrEmpty(queueName) || !_queues.TryGetValue(queueName, out var queueObj))
                return null;

            // Use reflection to get queue statistics regardless of generic type
            var getStatisticsMethod = queueObj.GetType().GetMethod("GetStatistics");
            return getStatisticsMethod?.Invoke(queueObj, null) as QueueStatistics;
        }

        /// <summary>
        /// Gets a list of all queue names
        /// </summary>
        /// <returns>List of queue names</returns>
        public IReadOnlyList<string> GetQueueNames()
        {
            return _queues.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a queue exists
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>True if queue exists</returns>
        public bool QueueExists(string queueName)
        {
            return !string.IsNullOrEmpty(queueName) && _queues.ContainsKey(queueName);
        }

        /// <summary>
        /// Pauses processing for all queues
        /// </summary>
        public async Task PauseAllAsync()
        {
            var tasks = _processors.Keys.Select(StopProcessingAsync);
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Resumes processing for all queues
        /// </summary>
        public async Task ResumeAllAsync()
        {
            // This would require storing processor configurations
            // For now, we'll just note that processors need to be re-added manually
            // In a full implementation, we'd store processor configurations and restart them
            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all messages from a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue to clear</param>
        /// <returns>True if queue was found and cleared</returns>
        public async Task<bool> ClearQueueAsync(string queueName)
        {
            try
            {
                if (string.IsNullOrEmpty(queueName) || !_queues.TryGetValue(queueName, out var queueObj))
                {
                    return false;
                }

                // Use reflection to call Clear method on the queue
                var clearMethod = queueObj.GetType().GetMethod("Clear");
                if (clearMethod != null)
                {
                    var result = clearMethod.Invoke(queueObj, null);
                    if (result is Task task)
                    {
                        await task;
                    }
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all messages from all queues
        /// </summary>
        /// <returns>Number of queues that were cleared</returns>
        public async Task<int> ClearAllQueuesAsync()
        {
            int clearedCount = 0;
            var queueNames = GetQueueNames();
            
            foreach (var queueName in queueNames)
            {
                if (await ClearQueueAsync(queueName))
                {
                    clearedCount++;
                }
            }
            
            return clearedCount;
        }

        #region Private Methods

        /// <summary>
        /// Main processing loop for a queue
        /// </summary>
        private async Task StartQueueProcessingLoop<T>(
            IMessageQueue<T> queue, 
            IQueueProcessor<T> processor, 
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    if (processor.SupportsBatchProcessing)
                    {
                        var messages = await queue.DequeueBatchAsync(processor.MaxBatchSize, cancellationToken);
                        if (messages.Any())
                        {
                            var batchResult = await processor.ProcessBatchAsync(messages);
                            await HandleBatchResult(queue, messages, batchResult);
                        }
                        else
                        {
                            // Wait a bit if no messages available
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                    else
                    {
                        var message = await queue.DequeueAsync(cancellationToken);
                        if (message != null)
                        {
                            var result = await processor.ProcessAsync(message);
                            await HandleSingleResult(queue, message, result);
                        }
                        else
                        {
                            // Wait a bit if no messages available
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    // Log error and continue processing
                    // In a real implementation, you'd use proper logging here
                    Console.WriteLine($"Error in queue processing loop: {ex.Message}");
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                }
            }
        }

        /// <summary>
        /// Handles the result of processing a single message
        /// </summary>
        private async Task HandleSingleResult<T>(
            IMessageQueue<T> queue,
            QueueMessage<T> message,
            ProcessingResult result)
        {
            if (queue is ConcurrentMessageQueue<T> concreteQueue)
            {
                if (result.Success)
                {
                    concreteQueue.MarkMessageCompleted(message.MessageId, result.ProcessingTime);
                }
                else
                {
                    concreteQueue.MarkMessageFailed(message.MessageId, result.ErrorMessage ?? "Unknown error");

                    if (result.ShouldRetry)
                    {
                        await queue.RequeueAsync(message);
                    }
                    else
                    {
                        await queue.MoveToDeadLetterAsync(message);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the result of processing a batch of messages
        /// </summary>
        private async Task HandleBatchResult<T>(
            IMessageQueue<T> queue,
            List<QueueMessage<T>> messages,
            BatchProcessingResult batchResult)
        {
            if (queue is ConcurrentMessageQueue<T> concreteQueue)
            {
                for (int i = 0; i < messages.Count && i < batchResult.Results.Count; i++)
                {
                    var message = messages[i];
                    var result = batchResult.Results[i];

                    await HandleSingleResult(queue, message, result);
                }
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
                try
                {
                    // Stop all processing with shorter timeout
                    var stopTasks = _processors.Keys.Select(StopProcessingAsync);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    var completedTask = Task.WhenAny(Task.WhenAll(stopTasks), timeoutTask).Result;
                    
                    if (completedTask == timeoutTask)
                    {
                        // Timeout occurred, force cancellation
                        foreach (var wrapper in _processors.Values)
                        {
                            wrapper.CancellationTokenSource?.Cancel();
                        }
                        // Give a little more time for cleanup
                        Task.Delay(1000).Wait();
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue cleanup
                    Console.WriteLine($"Error stopping processors during disposal: {ex.Message}");
                }

                // Dispose all queues
                foreach (var queue in _queues.Values.OfType<IDisposable>())
                {
                    try
                    {
                        queue.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing queue: {ex.Message}");
                    }
                }

                _queues.Clear();
                _processors.Clear();
                _queueTypes.Clear();

                _disposed = true;
            }
        }

        #endregion
    }
}