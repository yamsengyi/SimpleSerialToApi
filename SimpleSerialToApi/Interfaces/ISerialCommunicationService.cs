using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Interface for serial communication service
    /// </summary>
    public interface ISerialCommunicationService : IDisposable
    {
        /// <summary>
        /// Event raised when data is received from the serial port
        /// </summary>
        event EventHandler<Models.SerialDataReceivedEventArgs>? DataReceived;

        /// <summary>
        /// Event raised when connection status changes
        /// </summary>
        event EventHandler<Models.SerialConnectionEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// Gets whether the serial port is currently connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current connection settings
        /// </summary>
        SerialConnectionSettings ConnectionSettings { get; }

        /// <summary>
        /// Connects to the serial port asynchronously
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the serial port asynchronously
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Sends raw byte data to the serial port
        /// </summary>
        /// <param name="data">Byte array to send</param>
        /// <returns>True if send successful, false otherwise</returns>
        Task<bool> SendDataAsync(byte[] data);

        /// <summary>
        /// Sends text data to the serial port
        /// </summary>
        /// <param name="text">Text to send</param>
        /// <returns>True if send successful, false otherwise</returns>
        Task<bool> SendTextAsync(string text);

        /// <summary>
        /// Initializes the connected device with initialization protocol
        /// </summary>
        /// <returns>True if initialization successful, false otherwise</returns>
        Task<bool> InitializeDeviceAsync();

        /// <summary>
        /// Gets list of available serial ports on the system
        /// </summary>
        /// <returns>Array of available port names</returns>
        string[] GetAvailablePorts();

        /// <summary>
        /// Updates the connection settings with a new port name
        /// </summary>
        /// <param name="portName">The new port name to use</param>
        void UpdatePortName(string portName);

        /// <summary>
        /// Updates the complete connection settings
        /// </summary>
        /// <param name="settings">The new connection settings</param>
        void UpdateConnectionSettings(SerialConnectionSettings settings);
    }
}