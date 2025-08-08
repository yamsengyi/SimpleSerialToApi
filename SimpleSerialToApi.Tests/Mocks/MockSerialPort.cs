using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of serial port for testing
    /// </summary>
    public class MockSerialPort : IDisposable
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public int ReadTimeout { get; set; } = 5000;
        public int WriteTimeout { get; set; } = 5000;
        public bool IsOpen { get; private set; } = false;

        private readonly Queue<byte[]> _dataQueue = new Queue<byte[]>();
        private readonly List<byte[]> _sentData = new List<byte[]>();

        public event EventHandler<EventArgs>? DataReceived;
        public event EventHandler<EventArgs>? ErrorReceived;

        public void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("Port is already open");
            
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException("Port is not open");

            var data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);
            _sentData.Add(data);
        }

        public void Write(string text)
        {
            if (!IsOpen)
                throw new InvalidOperationException("Port is not open");

            var data = Encoding.UTF8.GetBytes(text);
            _sentData.Add(data);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException("Port is not open");

            if (_dataQueue.Count == 0)
                return 0;

            var data = _dataQueue.Dequeue();
            var readCount = Math.Min(count, data.Length);
            Array.Copy(data, 0, buffer, offset, readCount);
            return readCount;
        }

        public string ReadExisting()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Port is not open");

            var allData = new List<byte>();
            while (_dataQueue.Count > 0)
            {
                var data = _dataQueue.Dequeue();
                allData.AddRange(data);
            }

            return Encoding.UTF8.GetString(allData.ToArray());
        }

        /// <summary>
        /// Simulate receiving data from serial port
        /// </summary>
        public void SimulateDataReceived(byte[] data)
        {
            _dataQueue.Enqueue(data);
            DataReceived?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Simulate receiving text data from serial port
        /// </summary>
        public void SimulateDataReceived(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            SimulateDataReceived(data);
        }

        /// <summary>
        /// Get all data that was sent through this mock port
        /// </summary>
        public List<byte[]> GetSentData()
        {
            return new List<byte[]>(_sentData);
        }

        /// <summary>
        /// Clear the sent data history
        /// </summary>
        public void ClearSentData()
        {
            _sentData.Clear();
        }

        /// <summary>
        /// Simulate an error
        /// </summary>
        public void SimulateError()
        {
            ErrorReceived?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Close();
            _dataQueue.Clear();
            _sentData.Clear();
            GC.SuppressFinalize(this);
        }
    }
}