using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시리얼 연결/포트 관리를 위한 파사드 구현
    /// </summary>
    public class SerialConnectionFacade : ISerialConnectionFacade, INotifyPropertyChanged
    {
        private readonly ILogger<SerialConnectionFacade> _logger;
        private readonly SerialCommunicationService _serialService;
        private readonly ComPortDiscoveryService _comPortDiscovery;

        private string _serialPort = "COM1";
        private bool _isConnected;
        private ObservableCollection<ComPortInfo> _availablePorts = new();

        public SerialConnectionFacade(
            ILogger<SerialConnectionFacade> logger,
            SerialCommunicationService serialService,
            ComPortDiscoveryService comPortDiscovery)
        {
            _logger = logger;
            _serialService = serialService;
            _comPortDiscovery = comPortDiscovery;
        }

        public string SerialPort
        {
            get => _serialPort;
            set
            {
                _serialPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SerialConnectionStatus));

                if (!string.IsNullOrWhiteSpace(value))
                    _serialService.UpdatePortName(value);
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SerialConnectionStatus));
                OnPropertyChanged(nameof(CanConnect));
                OnPropertyChanged(nameof(CanDisconnect));
            }
        }

        public ObservableCollection<ComPortInfo> AvailablePorts => _availablePorts;

        public string SerialConnectionStatus => IsConnected ? $"{SerialPort}" : "Disconnected";
        public bool CanConnect => !IsConnected;
        public bool CanDisconnect => IsConnected;

        public async Task ConnectAsync()
        {
            try
            {
                var success = await _serialService.ConnectAsync();
                IsConnected = success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to serial port");
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _serialService.DisconnectAsync();
                IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from serial port");
            }
        }

        public void RefreshPorts()
        {
            try
            {
                var portsWithDescriptions = _comPortDiscovery.GetAvailablePortsWithDescriptions();
                var currentSelectedPort = SerialPort;

                _availablePorts.Clear();
                foreach (var port in portsWithDescriptions)
                {
                    _availablePorts.Add(new ComPortInfo
                    {
                        PortName = port.Key,
                        Description = port.Value
                    });
                }

                if (string.IsNullOrEmpty(currentSelectedPort) ||
                    !_availablePorts.Any(p => p.PortName == currentSelectedPort))
                {
                    PerformSmartSelection();
                }
                else
                {
                    SerialPort = currentSelectedPort;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing COM ports");
            }
        }

        public void InitializeSmartPortSelection()
        {
            PerformSmartSelection();
        }

        public void PerformSmartSelection()
        {
            try
            {
                var smartPort = _comPortDiscovery.GetBestAvailableComPort();
                if (!string.IsNullOrEmpty(smartPort))
                {
                    SerialPort = smartPort;

                    foreach (var port in _availablePorts)
                    {
                        port.IsSmartSelected = false;
                        port.IsLastUsed = false;
                    }

                    var selectedPortInfo = _availablePorts.FirstOrDefault(p => p.PortName == smartPort);
                    if (selectedPortInfo != null)
                    {
                        selectedPortInfo.IsSmartSelected = true;
                        var lastUsedPort = System.Configuration.ConfigurationManager.AppSettings["LastUsedComPort"];
                        if (smartPort == lastUsedPort)
                            selectedPortInfo.IsLastUsed = true;
                    }
                }
                else
                {
                    _logger.LogWarning("No COM ports available for smart selection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart port selection");
            }
        }

        public async Task CheckAutoConnectAsync()
        {
            try
            {
                await Task.Delay(2000);

                var (enabled, portName) = _comPortDiscovery.GetAutoConnectSettings();
                if (enabled && !string.IsNullOrEmpty(portName))
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (!IsConnected && _availablePorts.Any(p => p.PortName == portName))
                        {
                            SerialPort = portName;
                            await Task.Delay(1000);
                            await ConnectAsync();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-connect");
            }
        }

        /// <summary>내부 SerialCommunicationService 접근자</summary>
        public SerialCommunicationService GetService() => _serialService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
