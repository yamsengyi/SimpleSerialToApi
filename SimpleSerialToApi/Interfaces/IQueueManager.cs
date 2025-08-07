using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Interface for managing multiple message queues
    /// </summary>
    public interface IQueueManager : IDisposable
    {
        /// <summary>
        /// Creates a new queue with the specified configuration
        /// </summary>
        /// <typeparam name="T">Type of messages for the queue</typeparam>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="config">Queue configuration</param>
        /// <returns>The created queue</returns>
        IMessageQueue<T> CreateQueue<T>(string queueName, QueueConfiguration config);

        /// <summary>
        /// Gets an existing queue by name
        /// </summary>
        /// <typeparam name="T">Type of messages for the queue</typeparam>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>The queue or null if not found</returns>
        IMessageQueue<T>? GetQueue<T>(string queueName);

        /// <summary>
        /// Removes and disposes a queue
        /// </summary>
        /// <param name="queueName">Name of the queue to remove</param>
        /// <returns>True if queue was found and removed</returns>
        Task<bool> RemoveQueueAsync(string queueName);

        /// <summary>
        /// Starts processing for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="processor">Processor to use for the queue</param>
        /// <returns>True if processing was started</returns>
        Task<bool> StartProcessingAsync<T>(string queueName, IQueueProcessor<T> processor);

        /// <summary>
        /// Stops processing for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>True if processing was stopped</returns>
        Task<bool> StopProcessingAsync(string queueName);

        /// <summary>
        /// Gets the health status of a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>Health status of the queue</returns>
        QueueHealthStatus GetQueueHealth(string queueName);

        /// <summary>
        /// Gets statistics for all queues
        /// </summary>
        /// <returns>Dictionary of queue statistics by queue name</returns>
        Task<Dictionary<string, QueueStatistics>> GetAllQueueStatisticsAsync();

        /// <summary>
        /// Gets statistics for a specific queue
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>Queue statistics or null if queue not found</returns>
        Task<QueueStatistics?> GetQueueStatisticsAsync(string queueName);

        /// <summary>
        /// Gets a list of all queue names
        /// </summary>
        /// <returns>List of queue names</returns>
        IReadOnlyList<string> GetQueueNames();

        /// <summary>
        /// Checks if a queue exists
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>True if queue exists</returns>
        bool QueueExists(string queueName);

        /// <summary>
        /// Pauses processing for all queues
        /// </summary>
        Task PauseAllAsync();

        /// <summary>
        /// Resumes processing for all queues
        /// </summary>
        Task ResumeAllAsync();

        /// <summary>
        /// Gets the total number of managed queues
        /// </summary>
        int QueueCount { get; }

        /// <summary>
        /// Event raised when a queue is created
        /// </summary>
        event EventHandler<string>? QueueCreated;

        /// <summary>
        /// Event raised when a queue is removed
        /// </summary>
        event EventHandler<string>? QueueRemoved;

        /// <summary>
        /// Event raised when processing starts for a queue
        /// </summary>
        event EventHandler<string>? ProcessingStarted;

        /// <summary>
        /// Event raised when processing stops for a queue
        /// </summary>
        event EventHandler<string>? ProcessingStopped;

        /// <summary>
        /// Event raised when a queue health status changes
        /// </summary>
        event EventHandler<(string QueueName, QueueHealthStatus Status)>? QueueHealthChanged;
    }
}