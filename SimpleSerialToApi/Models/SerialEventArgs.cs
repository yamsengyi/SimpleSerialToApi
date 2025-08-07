namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Event arguments for serial data received events
    /// </summary>
    public class SerialDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public string DataAsText { get; }
        public string DataAsHex { get; }
        public DateTime Timestamp { get; }

        public SerialDataReceivedEventArgs(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            DataAsText = System.Text.Encoding.UTF8.GetString(data);
            DataAsHex = Convert.ToHexString(data);
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for serial connection status changes
    /// </summary>
    public class SerialConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string PortName { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; }

        public SerialConnectionEventArgs(bool isConnected, string portName, string message, Exception? exception = null)
        {
            IsConnected = isConnected;
            PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
            Timestamp = DateTime.Now;
        }
    }
}