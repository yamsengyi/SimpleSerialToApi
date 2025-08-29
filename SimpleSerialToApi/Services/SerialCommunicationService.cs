using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System.Configuration;
using System.IO.Ports;
using System.Text;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Service for managing serial port communication
    /// </summary>
    public class SerialCommunicationService : ISerialCommunicationService
    {
        private readonly ILogger<SerialCommunicationService> _logger;
        private readonly ComPortDiscoveryService _comPortDiscovery;
        private SerialPort? _serialPort;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private bool _disposed = false;

        public event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;
        public event EventHandler<Models.SerialConnectionEventArgs>? ConnectionStatusChanged;

        public bool IsConnected => _serialPort?.IsOpen == true;
        
        public SerialConnectionSettings ConnectionSettings { get; private set; }

        public SerialCommunicationService(
            ILogger<SerialCommunicationService> logger, 
            ComPortDiscoveryService comPortDiscovery)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _comPortDiscovery = comPortDiscovery ?? throw new ArgumentNullException(nameof(comPortDiscovery));
            
            ConnectionSettings = LoadConnectionSettings();
            _logger.LogInformation("SerialCommunicationService initialized with port {PortName}", ConnectionSettings.PortName);
        }

        public async Task<bool> ConnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    _logger.LogInformation("Already connected to {PortName}", ConnectionSettings.PortName);
                    return true;
                }

                _logger.LogInformation("Attempting to connect to {PortName}", ConnectionSettings.PortName);

                _serialPort = new SerialPort
                {
                    PortName = ConnectionSettings.PortName,
                    BaudRate = ConnectionSettings.BaudRate,
                    Parity = ConnectionSettings.Parity,
                    DataBits = ConnectionSettings.DataBits,
                    StopBits = ConnectionSettings.StopBits,
                    Handshake = ConnectionSettings.Handshake,
                    ReadTimeout = ConnectionSettings.ReadTimeout,
                    WriteTimeout = ConnectionSettings.WriteTimeout,
                    ReadBufferSize = ConnectionSettings.ReadBufferSize,
                    WriteBufferSize = ConnectionSettings.WriteBufferSize
                };

                _logger.LogInformation("Serial port configured - Buffers: Read={ReadBuffer}KB, Write={WriteBuffer}KB", 
                    ConnectionSettings.ReadBufferSize / 1024.0, ConnectionSettings.WriteBufferSize / 1024.0);

                // Subscribe to data received event
                _serialPort.DataReceived += SerialPort_DataReceived;

                _serialPort.Open();

                // 연결 성공 시 마지막 사용 포트로 저장 및 자동 연결 활성화
                _comPortDiscovery.SaveLastUsedComPort(ConnectionSettings.PortName);
                _comPortDiscovery.EnableAutoConnect(ConnectionSettings.PortName);

                var eventArgs = new Models.SerialConnectionEventArgs(true, ConnectionSettings.PortName, "Connected successfully");
                ConnectionStatusChanged?.Invoke(this, eventArgs);
                _logger.LogInformation("Successfully connected to {PortName}", ConnectionSettings.PortName);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {PortName}", ConnectionSettings.PortName);
                
                var eventArgs = new Models.SerialConnectionEventArgs(false, ConnectionSettings.PortName, "Connection failed", ex);
                ConnectionStatusChanged?.Invoke(this, eventArgs);
                
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.Close();
                    
                    var eventArgs = new Models.SerialConnectionEventArgs(false, ConnectionSettings.PortName, "Disconnected");
                    ConnectionStatusChanged?.Invoke(this, eventArgs);
                    _logger.LogInformation("Disconnected from {PortName}", ConnectionSettings.PortName);
                }
                
                _serialPort?.Dispose();
                _serialPort = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect from {PortName}", ConnectionSettings.PortName);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<bool> SendDataAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                _logger.LogWarning("Attempted to send null or empty data");
                return false;
            }

            if (!IsConnected)
            {
                _logger.LogWarning("Cannot send data - not connected to {PortName}", ConnectionSettings.PortName);
                return false;
            }

            try
            {
                // 데이터가 쓰기 버퍼 크기보다 큰 경우 청크 단위로 전송
                if (data.Length > ConnectionSettings.WriteBufferSize)
                {
                    return await SendDataInChunksAsync(data);
                }
                else
                {
                    await Task.Run(() => _serialPort!.Write(data, 0, data.Length));
                    _logger.LogDebug("Sent {ByteCount} bytes to {PortName}: {Data}", 
                        data.Length, ConnectionSettings.PortName, Convert.ToHexString(data));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send data to {PortName}", ConnectionSettings.PortName);
                return false;
            }
        }

        /// <summary>
        /// 큰 데이터를 청크 단위로 분할하여 전송
        /// </summary>
        /// <param name="data">전송할 데이터</param>
        /// <returns>전송 성공 여부</returns>
        private async Task<bool> SendDataInChunksAsync(byte[] data)
        {
            try
            {
                var chunkSize = Math.Min(ConnectionSettings.WriteBufferSize - 100, 1024); // 안전 마진 확보
                var totalLength = data.Length;
                var totalChunks = (int)Math.Ceiling((double)totalLength / chunkSize);

                _logger.LogInformation("Sending large data in {ChunkCount} chunks (Total: {TotalBytes} bytes, ChunkSize: {ChunkSize} bytes)", 
                    totalChunks, totalLength, chunkSize);

                for (int i = 0; i < totalChunks; i++)
                {
                    var offset = i * chunkSize;
                    var currentChunkSize = Math.Min(chunkSize, totalLength - offset);
                    var chunk = new byte[currentChunkSize];
                    Array.Copy(data, offset, chunk, 0, currentChunkSize);

                    await Task.Run(() => _serialPort!.Write(chunk, 0, chunk.Length));
                    
                    _logger.LogDebug("Sent chunk {ChunkNumber}/{TotalChunks} ({ChunkBytes} bytes): {Data}", 
                        i + 1, totalChunks, currentChunkSize, Convert.ToHexString(chunk));

                    // 청크 간 짧은 지연으로 수신측에서 처리 시간 확보
                    if (i < totalChunks - 1)
                    {
                        await Task.Delay(10); // 10ms 지연
                    }
                }

                _logger.LogInformation("Successfully sent large data ({TotalBytes} bytes) in {ChunkCount} chunks", 
                    totalLength, totalChunks);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send data in chunks");
                return false;
            }
        }

        public async Task<bool> SendTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _logger.LogWarning("Attempted to send null or empty text");
                return false;
            }

            var data = Encoding.UTF8.GetBytes(text);
            var result = await SendDataAsync(data);
            
            if (result)
            {
                _logger.LogDebug("Sent text to {PortName}: {Text}", ConnectionSettings.PortName, text);
            }
            
            return result;
        }

        public async Task<bool> InitializeDeviceAsync()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Cannot initialize device - not connected to {PortName}", ConnectionSettings.PortName);
                return false;
            }

            try
            {
                _logger.LogInformation("Initializing device on {PortName}", ConnectionSettings.PortName);
                
                // Send initialization command (can be customized based on device requirements)
                var initCommand = Encoding.UTF8.GetBytes("INIT\r\n");
                var sendResult = await SendDataAsync(initCommand);
                
                if (!sendResult)
                {
                    _logger.LogError("Failed to send initialization command to {PortName}", ConnectionSettings.PortName);
                    return false;
                }

                // Wait for ACK response (simplified - could be enhanced with actual ACK/NACK detection)
                await Task.Delay(1000); // Wait for device response
                
                _logger.LogInformation("Device initialization completed for {PortName}", ConnectionSettings.PortName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device initialization failed for {PortName}", ConnectionSettings.PortName);
                return false;
            }
        }

        public string[] GetAvailablePorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                _logger.LogDebug("Found {PortCount} available ports: {Ports}", ports.Length, string.Join(", ", ports));
                return ports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available ports");
                return Array.Empty<string>();
            }
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    return;

                var bytesAvailable = _serialPort.BytesToRead;
                if (bytesAvailable == 0)
                    return;

                var buffer = new byte[bytesAvailable];
                var bytesRead = _serialPort.Read(buffer, 0, bytesAvailable);
                
                if (bytesRead > 0)
                {
                    var receivedData = new byte[bytesRead];
                    Array.Copy(buffer, receivedData, bytesRead);
                    
                    var eventArgs = new Models.SerialDataReceivedEventArgs(receivedData);
                    DataReceived?.Invoke(this, eventArgs);
                    
                    _logger.LogDebug("Received {ByteCount} bytes from {PortName}: {Data}", 
                        bytesRead, ConnectionSettings.PortName, Convert.ToHexString(receivedData));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received data from {PortName}", ConnectionSettings.PortName);
            }
        }

        private SerialConnectionSettings LoadConnectionSettings()
        {
            var settings = new SerialConnectionSettings();
            
            try
            {
                // 스마트 COM 포트 선택: App.config에 지정되지 않았다면 자동 선택
                var configuredPort = ConfigurationManager.AppSettings["SerialPort"];
                if (string.IsNullOrEmpty(configuredPort))
                {
                    var smartSelectedPort = _comPortDiscovery.GetBestAvailableComPort();
                    settings.PortName = smartSelectedPort ?? settings.PortName;
                    _logger.LogInformation("Smart selected COM port: {PortName}", settings.PortName);
                }
                else
                {
                    settings.PortName = configuredPort;
                    _logger.LogInformation("Using configured COM port: {PortName}", settings.PortName);
                }

                settings.BaudRate = int.TryParse(ConfigurationManager.AppSettings["BaudRate"], out var baudRate) ? baudRate : settings.BaudRate;
                settings.DataBits = int.TryParse(ConfigurationManager.AppSettings["DataBits"], out var dataBits) ? dataBits : settings.DataBits;
                settings.ReadTimeout = int.TryParse(ConfigurationManager.AppSettings["ReadTimeout"], out var readTimeout) ? readTimeout : settings.ReadTimeout;
                settings.WriteTimeout = int.TryParse(ConfigurationManager.AppSettings["WriteTimeout"], out var writeTimeout) ? writeTimeout : settings.WriteTimeout;

                // Parse enum values
                if (Enum.TryParse<Parity>(ConfigurationManager.AppSettings["Parity"], out var parity))
                    settings.Parity = parity;
                
                if (Enum.TryParse<StopBits>(ConfigurationManager.AppSettings["StopBits"], out var stopBits))
                    settings.StopBits = stopBits;
                
                if (Enum.TryParse<Handshake>(ConfigurationManager.AppSettings["Handshake"], out var handshake))
                    settings.Handshake = handshake;

                _logger.LogInformation("Serial settings loaded: {PortName}, {BaudRate} baud, {DataBits} data bits, {Parity} parity, {StopBits} stop bits",
                    settings.PortName, settings.BaudRate, settings.DataBits, settings.Parity, settings.StopBits);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading serial settings, using defaults");
            }

            return settings;
        }

        /// <summary>
        /// 현재 설정된 버퍼 크기 정보 반환
        /// </summary>
        /// <returns>버퍼 크기 정보 문자열</returns>
        public string GetBufferInfo()
        {
            return $"ReadBuffer: {ConnectionSettings.ReadBufferSize / 1024.0:F1}KB ({ConnectionSettings.ReadBufferSize} bytes), " +
                   $"WriteBuffer: {ConnectionSettings.WriteBufferSize / 1024.0:F1}KB ({ConnectionSettings.WriteBufferSize} bytes)";
        }

        /// <summary>
        /// 1회 전송 가능한 최대 문자열 길이 반환 (UTF-8 기준)
        /// </summary>
        /// <returns>최대 문자 수</returns>
        public int GetMaxSingleTransmissionLength()
        {
            // UTF-8에서 ASCII 문자는 1바이트, 한글 등은 최대 4바이트
            // 안전하게 ASCII 기준으로 계산 (1바이트/문자)
            return ConnectionSettings.WriteBufferSize - 100; // 안전 마진 확보
        }

        /// <summary>
        /// Updates the connection settings with a new port name
        /// </summary>
        /// <param name="portName">The new port name to use</param>
        public void UpdatePortName(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                _logger.LogWarning("Attempted to set empty or null port name");
                return;
            }

            if (ConnectionSettings.PortName != portName)
            {
                ConnectionSettings.PortName = portName;
                _logger.LogInformation("Port name updated to: {PortName}", portName);
            }
        }

        /// <summary>
        /// Updates the complete connection settings
        /// </summary>
        /// <param name="settings">The new connection settings</param>
        public void UpdateConnectionSettings(SerialConnectionSettings settings)
        {
            if (settings == null)
            {
                _logger.LogWarning("Attempted to set null connection settings");
                return;
            }

            ConnectionSettings.PortName = settings.PortName;
            ConnectionSettings.BaudRate = settings.BaudRate;
            ConnectionSettings.Parity = settings.Parity;
            ConnectionSettings.DataBits = settings.DataBits;
            ConnectionSettings.StopBits = settings.StopBits;
            ConnectionSettings.Handshake = settings.Handshake;
            ConnectionSettings.ReadTimeout = settings.ReadTimeout;
            ConnectionSettings.WriteTimeout = settings.WriteTimeout;

            _logger.LogInformation("Connection settings updated: {PortName}, {BaudRate} baud, {DataBits} data bits, {Parity} parity, {StopBits} stop bits",
                settings.PortName, settings.BaudRate, settings.DataBits, settings.Parity, settings.StopBits);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAsync().Wait();
                _connectionSemaphore?.Dispose();
                _disposed = true;
                _logger.LogInformation("SerialCommunicationService disposed");
            }
        }
    }
}