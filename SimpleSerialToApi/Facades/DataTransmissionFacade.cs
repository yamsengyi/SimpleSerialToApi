using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 데이터 전송 처리를 위한 파사드 구현
    /// </summary>
    public class DataTransmissionFacade : IDataTransmissionFacade
    {
        private readonly ILogger<DataTransmissionFacade> _logger;
        private readonly SerialCommunicationService _serialService;
        private readonly SimpleQueueService _queueService;
        private readonly SimpleHttpService _httpService;
        private readonly DataMappingService _dataMappingService;
        private readonly SerialMonitorService _serialMonitorService;
        private readonly ApiMonitorService _apiMonitorService;
        private readonly IQueueManager _queueManager;
        private readonly IQueueProcessor<MappedApiData> _apiDataQueueProcessor;
        private readonly IDataMappingFacade _dataMappingFacade;

        private int _queueCount;

        public DataTransmissionFacade(
            ILogger<DataTransmissionFacade> logger,
            SerialCommunicationService serialService,
            SimpleQueueService queueService,
            SimpleHttpService httpService,
            DataMappingService dataMappingService,
            SerialMonitorService serialMonitorService,
            ApiMonitorService apiMonitorService,
            IQueueManager queueManager,
            IQueueProcessor<MappedApiData> apiDataQueueProcessor,
            IDataMappingFacade dataMappingFacade)
        {
            _logger = logger;
            _serialService = serialService;
            _queueService = queueService;
            _httpService = httpService;
            _dataMappingService = dataMappingService;
            _serialMonitorService = serialMonitorService;
            _apiMonitorService = apiMonitorService;
            _queueManager = queueManager;
            _apiDataQueueProcessor = apiDataQueueProcessor;
            _dataMappingFacade = dataMappingFacade;
        }

        public int QueueCount => _queueCount;

        public event EventHandler<int>? QueueCountChanged;
        public event EventHandler<string>? StatusChanged;

        public async Task OnSerialDataReceivedAsync(byte[] data)
        {
            try
            {
                var dataString = Encoding.UTF8.GetString(data);
                _serialMonitorService.LogSerialReceived(dataString);

                var messages = _queueService.ExtractMessages(data).ToList();
                if (messages.Count == 0 && !_queueService.IsFrameInProgress)
                {
                    messages.Add(dataString);
                }

                foreach (var message in messages)
                {
                    await _dataMappingService.ProcessDataAsync(message, DataSource.Serial);
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateQueueCount();
                    StatusChanged?.Invoke(this, $"Data received. Queue: {QueueCount}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received serial data");
            }
        }

        public async Task OnMappingProcessedAsync(MappingProcessedEventArgs e)
        {
            _serialMonitorService.LogMappingResult(e.OriginalData, e.Result.ProcessedData, e.Scenario.Name);

            if (e.Result.Success)
            {
                if (e.Result.TransmissionType == TransmissionType.Api)
                {
                    await ProcessApiTransmission(e.Result, e.Scenario);
                }
                else if (e.Result.TransmissionType == TransmissionType.Serial)
                {
                    await ProcessSerialTransmission(e.Result, e.Scenario);
                }
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusChanged?.Invoke(this, $"Mapped: {e.Scenario.Name}");
            });
        }

        public async Task OnSimulatedDataReceivedAsync(SimulatedSerialDataEventArgs e)
        {
            try
            {
                _serialMonitorService.LogSerialReceived($"[SIM] {e.DataString}");

                var messages = _queueService.ExtractMessages(e.Data).ToList();
                if (messages.Count == 0 && !_queueService.IsFrameInProgress)
                {
                    messages.Add(e.DataString);
                }

                foreach (var message in messages)
                {
                    await _dataMappingService.ProcessDataAsync(message, DataSource.Serial);
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateQueueCount();
                    StatusChanged?.Invoke(this, $"Simulation data processed. Queue: {QueueCount} (Scenario: {e.Scenario})");
                });

                _logger.LogDebug("Processed simulation data: '{Data}' (Scenario: {Scenario})",
                    e.DataString, e.Scenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simulated data: {Data}", e.DataString);
            }
        }

        public async Task TestApiAsync(string testApiUrl)
        {
            try
            {
                var requestId = _apiMonitorService.LogApiRequest("GET", testApiUrl, "API Connection Test");

                _httpService.SetApiUrl(testApiUrl);
                var success = await _httpService.TestConnectionAsync();

                var statusCode = success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError;
                _apiMonitorService.LogApiResponse(requestId, statusCode,
                    success ? "Connection OK" : "Connection Failed");

                StatusChanged?.Invoke(this, success ? "API Connection OK" : "API Connection Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                _apiMonitorService.LogApiError("test", ex);
                StatusChanged?.Invoke(this, "API Test Error");
            }
        }

        public void UpdateQueueCount()
        {
            try
            {
                _queueCount = _queueManager.GetMessageCount("ApiDataQueue");
                QueueCountChanged?.Invoke(this, _queueCount);
                _logger.LogDebug("Queue count updated: {Count}", _queueCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue count");
                _queueCount = 0;
            }
        }

        public async Task InitializeQueueProcessingAsync()
        {
            try
            {
                const string queueName = "ApiDataQueue";
                var queueConfig = new QueueConfiguration
                {
                    MaxSize = 1000,
                    BatchSize = 10,
                    BatchTimeoutMs = 1000,
                    RetryCount = 3,
                    RetryIntervalMs = 5000,
                    EnablePriority = false,
                    ProcessorThreadCount = 1,
                    EnableAsync = true,
                    Name = queueName
                };

                _queueManager.CreateQueue<MappedApiData>(queueName, queueConfig);
                var success = await _queueManager.StartProcessingAsync(queueName, _apiDataQueueProcessor);

                if (!success)
                {
                    _logger.LogError("Failed to start queue processing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing queue processing");
            }
        }

        public async Task ClearQueueAsync()
        {
            try
            {
                var queueNames = _queueManager.GetQueueNames();
                foreach (var queueName in queueNames)
                {
                    await _queueManager.ClearQueueAsync(queueName);
                }
                UpdateQueueCount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing message queue");
            }
        }

        public void ClearLogs()
        {
            try
            {
                _serialMonitorService.ClearLogs();
                _apiMonitorService.ClearLogs();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing monitor logs");
            }
        }

        private async Task ProcessApiTransmission(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                var apiData = new MappedApiData
                {
                    EndpointName = "default",
                    ApiEndpoint = _dataMappingFacade.GetApiEndpointForScenario(scenario, result.ProcessedData),
                    ApiMethod = scenario.ApiMethod ?? "POST",
                    ContentType = scenario.ContentType ?? "application/json",
                    Payload = new Dictionary<string, object>
                    {
                        { "data", result.ProcessedData },
                        { "originalData", result.OriginalData }
                    },
                    CreatedAt = DateTime.Now,
                    MessageId = Guid.NewGuid().ToString(),
                    Priority = 5,
                    RetryCount = 0,
                    MaxRetries = 3,
                    OriginalParsedData = new ParsedData
                    {
                        DeviceId = "device001",
                        DataSource = "serial",
                        Timestamp = DateTime.Now,
                        OriginalData = new RawSerialData(
                            Encoding.UTF8.GetBytes(result.OriginalData),
                            "TEXT", "device001", "COM1")
                    }
                };

                var queueMessage = new QueueMessage<MappedApiData>
                {
                    MessageId = apiData.MessageId,
                    Payload = apiData,
                    Priority = apiData.Priority,
                    EnqueueTime = DateTime.UtcNow,
                    Status = MessageStatus.Queued,
                    RetryCount = 0
                };

                var queue = _queueManager.GetQueue<MappedApiData>("ApiDataQueue");
                if (queue != null)
                {
                    await queue.EnqueueAsync(queueMessage);

                    var requestId = _apiMonitorService.LogApiRequest(result.ApiMethod,
                        scenario.ApiEndpoint ?? "unknown", result.ProcessedData);
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.Accepted,
                        "Queued for processing", null, 0);
                }
                else
                {
                    _logger.LogError("API data queue not found - falling back to direct transmission");
                    await ProcessApiTransmissionFallback(result, scenario);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing API data for scenario '{ScenarioName}'", scenario.Name);
                await ProcessApiTransmissionFallback(result, scenario);
            }
        }

        private async Task ProcessApiTransmissionFallback(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                var requestId = _apiMonitorService.LogApiRequest(result.ApiMethod, result.ApiEndpoint, result.ProcessedData);

                var apiUrl = _dataMappingFacade.GetApiEndpointForScenario(scenario, result.ProcessedData);

                var startTime = DateTime.Now;
                bool success = await _httpService.SendJsonAsync(result.ProcessedData);
                var responseTime = (long)(DateTime.Now - startTime).TotalMilliseconds;

                if (success)
                {
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.OK,
                        "Data transmitted successfully", null, responseTime);
                }
                else
                {
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.BadRequest,
                        "API transmission failed", null, responseTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API transmission for scenario '{ScenarioName}'", scenario.Name);
                _apiMonitorService.LogApiError(_apiMonitorService.LogApiRequest(result.ApiMethod, result.ApiEndpoint), ex);
            }
        }

        private async Task ProcessSerialTransmission(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                if (!_serialService.IsConnected)
                {
                    _logger.LogWarning("Cannot transmit serial data - not connected to serial port");
                    return;
                }

                string dataToSend = result.ProcessedData?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(dataToSend))
                {
                    _logger.LogWarning("No data to transmit for scenario '{ScenarioName}'", scenario.Name);
                    return;
                }

                bool success = await _serialService.SendTextAsync(dataToSend);

                if (success)
                {
                    _serialMonitorService.LogSerialSent(dataToSend);
                }
                else
                {
                    _logger.LogError("Serial transmission failed for scenario '{ScenarioName}'", scenario.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during serial transmission for scenario '{ScenarioName}'", scenario.Name);
            }
        }
    }
}
