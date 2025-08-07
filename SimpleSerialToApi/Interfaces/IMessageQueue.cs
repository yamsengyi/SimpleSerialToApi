using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Generic interface for message queue operations
    /// </summary>
    /// <typeparam name="T">Type of messages stored in the queue</typeparam>
    public interface IMessageQueue<T> : IDisposable
    {
        /// <summary>
        /// Adds a message to the queue
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        /// <returns>True if message was successfully enqueued</returns>
        Task<bool> EnqueueAsync(QueueMessage<T> message);

        /// <summary>
        /// Removes and returns the next message from the queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next message or null if queue is empty</returns>
        Task<QueueMessage<T>?> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes and returns a batch of messages from the queue
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of messages (may be empty)</returns>
        Task<List<QueueMessage<T>>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a message to the queue for retry
        /// </summary>
        /// <param name="message">Message to requeue</param>
        /// <returns>True if message was successfully requeued</returns>
        Task<bool> RequeueAsync(QueueMessage<T> message);

        /// <summary>
        /// Moves a message to the dead letter queue
        /// </summary>
        /// <param name="message">Message to move</param>
        /// <returns>True if message was successfully moved</returns>
        Task<bool> MoveToDeadLetterAsync(QueueMessage<T> message);

        /// <summary>
        /// Peeks at the next message without removing it
        /// </summary>
        /// <returns>The next message or null if queue is empty</returns>
        Task<QueueMessage<T>?> PeekAsync();

        /// <summary>
        /// Clears all messages from the queue
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Gets messages from dead letter queue
        /// </summary>
        /// <param name="count">Maximum number of messages to retrieve</param>
        /// <returns>List of dead letter messages</returns>
        Task<List<QueueMessage<T>>> GetDeadLetterMessagesAsync(int count = 100);

        /// <summary>
        /// Current number of messages in the queue
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Current number of messages being processed
        /// </summary>
        int ProcessingCount { get; }

        /// <summary>
        /// Whether the queue is empty
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Whether the queue is at capacity
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Maximum capacity of the queue
        /// </summary>
        int MaxCapacity { get; }

        /// <summary>
        /// Name of the queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets current queue statistics
        /// </summary>
        /// <returns>Queue statistics</returns>
        QueueStatistics GetStatistics();

        /// <summary>
        /// Event raised when a message is enqueued
        /// </summary>
        event EventHandler<QueueMessage<T>>? MessageEnqueued;

        /// <summary>
        /// Event raised when a message is dequeued
        /// </summary>
        event EventHandler<QueueMessage<T>>? MessageDequeued;

        /// <summary>
        /// Event raised when a message is moved to dead letter queue
        /// </summary>
        event EventHandler<QueueMessage<T>>? MessageMovedToDeadLetter;

        /// <summary>
        /// Event raised when queue reaches capacity
        /// </summary>
        event EventHandler<EventArgs>? QueueFull;

        /// <summary>
        /// Event raised when queue becomes empty
        /// </summary>
        event EventHandler<EventArgs>? QueueEmpty;
    }
}