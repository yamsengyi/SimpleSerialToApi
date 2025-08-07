using System;
using System.Collections.Generic;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Represents the status of a message in the queue
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// Message is waiting in the queue
        /// </summary>
        Queued,
        
        /// <summary>
        /// Message is currently being processed
        /// </summary>
        Processing,
        
        /// <summary>
        /// Message has been successfully processed
        /// </summary>
        Completed,
        
        /// <summary>
        /// Message processing failed but may be retried
        /// </summary>
        Failed,
        
        /// <summary>
        /// Message has failed permanently and moved to dead letter queue
        /// </summary>
        DeadLetter
    }

    /// <summary>
    /// Generic message wrapper for queue operations
    /// </summary>
    /// <typeparam name="T">Type of the payload</typeparam>
    public class QueueMessage<T>
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The actual data payload
        /// </summary>
        public T Payload { get; set; } = default!;

        /// <summary>
        /// Time when the message was enqueued
        /// </summary>
        public DateTime EnqueueTime { get; set; }

        /// <summary>
        /// Time when processing started (nullable if not started)
        /// </summary>
        public DateTime? ProcessingStartTime { get; set; }

        /// <summary>
        /// Number of processing attempts
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Priority for processing (higher number = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Additional metadata for the message
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Current status of the message
        /// </summary>
        public MessageStatus Status { get; set; }

        /// <summary>
        /// Last error message if processing failed
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Time of the last processing attempt
        /// </summary>
        public DateTime? LastProcessingAttempt { get; set; }

        /// <summary>
        /// Creates a new queue message
        /// </summary>
        public QueueMessage()
        {
            MessageId = Guid.NewGuid().ToString();
            EnqueueTime = DateTime.UtcNow;
            Status = MessageStatus.Queued;
            RetryCount = 0;
            Priority = 0;
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a new queue message with payload
        /// </summary>
        /// <param name="payload">Message payload</param>
        /// <param name="priority">Message priority (default: 0)</param>
        public QueueMessage(T payload, int priority = 0) : this()
        {
            Payload = payload;
            Priority = priority;
        }
    }

    /// <summary>
    /// Configuration for a message queue
    /// </summary>
    public class QueueConfiguration
    {
        /// <summary>
        /// Maximum number of messages the queue can hold
        /// </summary>
        public int MaxSize { get; set; } = 1000;

        /// <summary>
        /// Number of messages to process in a batch
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Timeout for batch processing in milliseconds
        /// </summary>
        public int BatchTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Maximum number of retry attempts for failed messages
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Base interval between retries in milliseconds
        /// </summary>
        public int RetryIntervalMs { get; set; } = 5000;

        /// <summary>
        /// Whether to enable priority-based processing
        /// </summary>
        public bool EnablePriority { get; set; } = false;

        /// <summary>
        /// Number of processor threads
        /// </summary>
        public int ProcessorThreadCount { get; set; } = 1;

        /// <summary>
        /// Whether to enable async processing
        /// </summary>
        public bool EnableAsync { get; set; } = true;

        /// <summary>
        /// Name of the queue
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistics for queue performance monitoring
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Current number of messages in the queue
        /// </summary>
        public int QueuedCount { get; set; }

        /// <summary>
        /// Number of messages currently being processed
        /// </summary>
        public int ProcessingCount { get; set; }

        /// <summary>
        /// Total number of messages processed successfully
        /// </summary>
        public long CompletedCount { get; set; }

        /// <summary>
        /// Total number of messages that failed processing
        /// </summary>
        public long FailedCount { get; set; }

        /// <summary>
        /// Number of messages in dead letter queue
        /// </summary>
        public long DeadLetterCount { get; set; }

        /// <summary>
        /// Average processing time in milliseconds
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Messages processed per second
        /// </summary>
        public double Throughput { get; set; }

        /// <summary>
        /// Time when statistics were last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Time when the queue was started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Peak queue size reached
        /// </summary>
        public int PeakQueueSize { get; set; }

        /// <summary>
        /// Current memory usage in bytes (estimated)
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Creates new queue statistics instance
        /// </summary>
        public QueueStatistics()
        {
            LastUpdated = DateTime.UtcNow;
            StartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Health status of a queue
    /// </summary>
    public enum QueueHealthStatus
    {
        /// <summary>
        /// Queue is operating normally
        /// </summary>
        Healthy,

        /// <summary>
        /// Queue has minor issues but is functional
        /// </summary>
        Warning,

        /// <summary>
        /// Queue has significant issues
        /// </summary>
        Critical,

        /// <summary>
        /// Queue is not operational
        /// </summary>
        Unhealthy
    }

    /// <summary>
    /// Result of processing a single message
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// Whether processing was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Time taken to process the message
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Whether the message should be retried
        /// </summary>
        public bool ShouldRetry { get; set; }

        /// <summary>
        /// Additional data from processing
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Creates a successful processing result
        /// </summary>
        public static ProcessingResult CreateSuccess(TimeSpan processingTime, Dictionary<string, object>? data = null)
        {
            return new ProcessingResult
            {
                Success = true,
                ProcessingTime = processingTime,
                Data = data ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a failed processing result
        /// </summary>
        public static ProcessingResult CreateFailure(string errorMessage, bool shouldRetry = true, TimeSpan processingTime = default)
        {
            return new ProcessingResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ShouldRetry = shouldRetry,
                ProcessingTime = processingTime,
                Data = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a new processing result
        /// </summary>
        public ProcessingResult()
        {
            Data = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Result of processing a batch of messages
    /// </summary>
    public class BatchProcessingResult
    {
        /// <summary>
        /// Number of messages processed successfully
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of messages that failed processing
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Total time taken for batch processing
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        /// <summary>
        /// Individual results for each message
        /// </summary>
        public List<ProcessingResult> Results { get; set; }

        /// <summary>
        /// Whether the entire batch was processed successfully
        /// </summary>
        public bool AllSuccessful => FailedCount == 0;

        /// <summary>
        /// Creates a new batch processing result
        /// </summary>
        public BatchProcessingResult()
        {
            Results = new List<ProcessingResult>();
        }
    }
}