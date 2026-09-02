using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시리얼/API 모니터 관리를 위한 파사드 구현
    /// </summary>
    public class MonitorFacade : IMonitorFacade, INotifyPropertyChanged
    {
        private readonly ILogger<MonitorFacade> _logger;
        private readonly SerialMonitorService _serialMonitorService;
        private readonly ApiMonitorService _apiMonitorService;

        private string _serialMonitorText = string.Empty;
        private string _apiMonitorText = string.Empty;
        private bool _serialMonitorAutoScroll = true;
        private bool _apiMonitorAutoScroll = true;
        private string _serialMonitorStatus = "Ready";
        private string _apiMonitorStatus = "Ready";
        private string _serialMonitorFilter = "All";
        private string _apiMonitorFilter = "All";
        private bool _serialShowTimestamps = true;
        private bool _apiShowHeaders = false;
        private int _serialMessageCount = 0;
        private int _apiRequestCount = 0;
        private int _apiSuccessCount = 0;
        private readonly object _serialMonitorLock = new();

        public List<string> SerialMonitorFilters { get; } = new() { "All", "Data", "Errors", "Commands" };
        public List<string> ApiMonitorFilters { get; } = new() { "All", "2xx", "4xx", "5xx", "GET", "POST", "PUT", "DELETE" };

        public MonitorFacade(
            ILogger<MonitorFacade> logger,
            SerialMonitorService serialMonitorService,
            ApiMonitorService apiMonitorService)
        {
            _logger = logger;
            _serialMonitorService = serialMonitorService;
            _apiMonitorService = apiMonitorService;
        }

        public string SerialMonitorText
        {
            get => _serialMonitorText;
            set { _serialMonitorText = value; OnPropertyChanged(); }
        }

        public string ApiMonitorText
        {
            get => _apiMonitorText;
            set { _apiMonitorText = value; OnPropertyChanged(); }
        }

        public bool SerialMonitorAutoScroll
        {
            get => _serialMonitorAutoScroll;
            set { _serialMonitorAutoScroll = value; OnPropertyChanged(); }
        }

        public bool ApiMonitorAutoScroll
        {
            get => _apiMonitorAutoScroll;
            set { _apiMonitorAutoScroll = value; OnPropertyChanged(); }
        }

        public string SerialMonitorStatus
        {
            get => _serialMonitorStatus;
            set { _serialMonitorStatus = value; OnPropertyChanged(); }
        }

        public string ApiMonitorStatus
        {
            get => _apiMonitorStatus;
            set { _apiMonitorStatus = value; OnPropertyChanged(); }
        }

        public string SerialMonitorFilter
        {
            get => _serialMonitorFilter;
            set { _serialMonitorFilter = value; OnPropertyChanged(); }
        }

        public string ApiMonitorFilter
        {
            get => _apiMonitorFilter;
            set { _apiMonitorFilter = value; OnPropertyChanged(); }
        }

        public bool SerialShowTimestamps
        {
            get => _serialShowTimestamps;
            set { _serialShowTimestamps = value; OnPropertyChanged(); }
        }

        public bool ApiShowHeaders
        {
            get => _apiShowHeaders;
            set { _apiShowHeaders = value; OnPropertyChanged(); }
        }

        public string SerialMessageCount => _serialMessageCount.ToString();
        public string ApiRequestCount => _apiRequestCount.ToString();
        public string ApiSuccessRate => _apiRequestCount > 0 ? $"{(_apiSuccessCount * 100 / _apiRequestCount)}%" : "0%";

        public void OnSerialMonitorMessageAdded(MonitorMessage message)
        {
            LoadExistingSerialMessages();
        }

        public void OnApiMonitorMessageAdded(ApiMonitorMessage message)
        {
            LoadExistingApiMessages();
        }

        public void ClearSerialMonitor()
        {
            lock (_serialMonitorLock)
            {
                _serialMonitorService.Clear();
                ResetSerialMonitorDisplay();
            }
        }

        public void ClearApiMonitor()
        {
            _apiMonitorService.Clear();
            ResetApiMonitorDisplay();
        }

        public void ResetSerialMonitorDisplay()
        {
            lock (_serialMonitorLock)
            {
                SerialMonitorText = string.Empty;
                _serialMessageCount = 0;
                OnPropertyChanged(nameof(SerialMessageCount));
            }
        }

        public void ResetApiMonitorDisplay()
        {
            ApiMonitorText = string.Empty;
            _apiRequestCount = 0;
            _apiSuccessCount = 0;
            OnPropertyChanged(nameof(ApiRequestCount));
            OnPropertyChanged(nameof(ApiSuccessRate));
        }

        public void SaveSerialMonitor()
        {
            try
            {
                var fileName = $"SerialMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                File.WriteAllText(filePath, SerialMonitorText);
                _logger.LogInformation("Serial monitor saved to {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving serial monitor");
            }
        }

        public void SaveApiMonitor()
        {
            try
            {
                var fileName = $"ApiMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                File.WriteAllText(filePath, ApiMonitorText);
                _logger.LogInformation("API monitor saved to {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API monitor");
            }
        }

        public void LoadExistingSerialMessages()
        {
            try
            {
                var messages = _serialMonitorService.Messages;
                var content = string.Join(Environment.NewLine,
                    messages.Where(m => m?.FormattedMessage != null)
                           .Select(m => m.FormattedMessage));

                lock (_serialMonitorLock)
                {
                    SerialMonitorText = content;
                    _serialMessageCount = messages.Count;
                    OnPropertyChanged(nameof(SerialMessageCount));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading existing serial messages");
                SerialMonitorText = "Error loading messages: " + ex.Message;
            }
        }

        public void LoadExistingApiMessages()
        {
            var messages = _apiMonitorService.Messages;
            ApiMonitorText = string.Join(Environment.NewLine, messages.Select(m => m.FormattedMessage));
            if (!string.IsNullOrEmpty(ApiMonitorText))
                ApiMonitorText += Environment.NewLine;

            _apiRequestCount = messages.Count;
            _apiSuccessCount = messages.Count(m => m.StatusCode.HasValue &&
                (int)m.StatusCode.Value >= 200 && (int)m.StatusCode.Value < 300);

            OnPropertyChanged(nameof(ApiRequestCount));
            OnPropertyChanged(nameof(ApiSuccessRate));
        }

        /// <summary>내부 SerialMonitorService 접근자</summary>
        public SerialMonitorService GetSerialMonitorService() => _serialMonitorService;

        /// <summary>내부 ApiMonitorService 접근자</summary>
        public ApiMonitorService GetApiMonitorService() => _apiMonitorService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
