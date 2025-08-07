using System.IO.Ports;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Configuration settings for serial port connection
    /// </summary>
    public class SerialConnectionSettings
    {
        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public int ReadTimeout { get; set; } = 5000;
        public int WriteTimeout { get; set; } = 5000;
    }
}