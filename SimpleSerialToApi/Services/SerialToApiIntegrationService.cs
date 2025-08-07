using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Queues;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Integration example showing how to use the queue system with serial communication
    /// </summary>
    public class SerialToApiIntegrationService
    {
        private readonly IQueueManager _queueManager;
        private readonly ISerialCommunicationService _serialService;
        private readonly IConfigurationService _configService;
        private readonly HttpClient _httpClient;
        private IMessageQueue<MappedApiData>? _apiDataQueue;

        public SerialToApiIntegrationService(
            IQueueManager queueManager,
            ISerialCommunicationService serialService,
            IConfigurationService configService,
            HttpClient httpClient)
        {
            _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Initialize the queue system and start processing
        /// </summary>
        public async Task InitializeAsync()
        {
            // Create the API data queue
            var queueConfig = new QueueConfiguration
            {
                Name = "ApiDataQueue",
                MaxSize = 1000,
                BatchSize = 10,
                BatchTimeoutMs = 5000,
                RetryCount = 3,
                RetryIntervalMs = 5000,
                EnablePriority = true,
                ProcessorThreadCount = 2,
                EnableAsync = true
            };

            _apiDataQueue = _queueManager.CreateQueue<MappedApiData>("ApiDataQueue", queueConfig);

            // Create and start the processor
            var processor = new ApiDataQueueProcessor(_httpClient, _configService);
            await _queueManager.StartProcessingAsync("ApiDataQueue", processor);

            // Subscribe to serial data events
            _serialService.DataReceived += OnSerialDataReceived;

            Console.WriteLine("Queue system initialized and ready to process API data");
        }

        /// <summary>
        /// Handle received serial data by queuing it for API processing
        /// </summary>
        private async void OnSerialDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (_apiDataQueue == null || e.Data == null) return;

            try
            {
                // Convert serial data to mapped API data (simplified example)
                var mappedData = new MappedApiData
                {
                    EndpointName = "SensorDataEndpoint",
                    Payload = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "rawData", Convert.ToBase64String(e.Data) },
                        { "textData", e.DataAsText },
                        { "hexData", e.DataAsHex },
                        { "timestamp", e.Timestamp }
                    },
                    Priority = 5 // Normal priority
                };

                // Create queue message with priority based on data importance
                var priority = DetermineMessagePriority(e);
                var queueMessage = new QueueMessage<MappedApiData>(mappedData, priority);

                // Add metadata for tracking
                queueMessage.Metadata["DataLength"] = e.Data.Length;
                queueMessage.Metadata["ReceivedAt"] = e.Timestamp;
                queueMessage.Metadata["DataFormat"] = DetectDataFormat(e);

                // Enqueue for processing
                var success = await _apiDataQueue.EnqueueAsync(queueMessage);
                
                if (!success)
                {
                    Console.WriteLine($"Failed to enqueue message - queue may be full");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing serial data: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine message priority based on serial data characteristics
        /// </summary>
        private int DetermineMessagePriority(SerialDataReceivedEventArgs data)
        {
            // Example priority logic based on data content
            if (data.DataAsText.Contains("EMERGENCY") || data.DataAsText.Contains("ALERT")) return 10; // Highest priority
            if (data.DataAsText.Contains("CRITICAL")) return 8;
            if (data.DataAsText.Contains("WARNING")) return 6;
            return 5; // Normal priority
        }

        /// <summary>
        /// Detect data format based on content
        /// </summary>
        private string DetectDataFormat(SerialDataReceivedEventArgs data)
        {
            // Simple format detection logic
            if (data.DataAsText.StartsWith("{") && data.DataAsText.EndsWith("}"))
                return "JSON";
            if (data.Data.All(b => char.IsAsciiHexDigit((char)b) || char.IsWhiteSpace((char)b)))
                return "HEX";
            return "TEXT";
        }

        /// <summary>
        /// Get current queue statistics for monitoring
        /// </summary>
        public async Task<QueueStatistics?> GetQueueStatisticsAsync()
        {
            return await _queueManager.GetQueueStatisticsAsync("ApiDataQueue");
        }

        /// <summary>
        /// Get queue health status
        /// </summary>
        public QueueHealthStatus GetQueueHealth()
        {
            return _queueManager.GetQueueHealth("ApiDataQueue");
        }

        /// <summary>
        /// Shutdown the queue system gracefully
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Unsubscribe from events
            _serialService.DataReceived -= OnSerialDataReceived;

            // Stop processing
            await _queueManager.StopProcessingAsync("ApiDataQueue");

            Console.WriteLine("Queue system shutdown complete");
        }
    }
}