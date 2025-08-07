using SimpleSerialToApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Interface for processing messages from queues
    /// </summary>
    /// <typeparam name="T">Type of message payload to process</typeparam>
    public interface IQueueProcessor<T>
    {
        /// <summary>
        /// Processes a single message
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <returns>Processing result</returns>
        Task<ProcessingResult> ProcessAsync(QueueMessage<T> message);

        /// <summary>
        /// Processes a batch of messages
        /// </summary>
        /// <param name="messages">Messages to process</param>
        /// <returns>Batch processing result</returns>
        Task<BatchProcessingResult> ProcessBatchAsync(List<QueueMessage<T>> messages);

        /// <summary>
        /// Determines if this processor can handle the given message
        /// </summary>
        /// <param name="message">Message to check</param>
        /// <returns>True if processor can handle the message</returns>
        bool CanProcess(QueueMessage<T> message);

        /// <summary>
        /// Maximum number of messages to process in a batch
        /// </summary>
        int MaxBatchSize { get; }

        /// <summary>
        /// Name of the processor
        /// </summary>
        string ProcessorName { get; }

        /// <summary>
        /// Whether this processor supports batch processing
        /// </summary>
        bool SupportsBatchProcessing { get; }
    }
}