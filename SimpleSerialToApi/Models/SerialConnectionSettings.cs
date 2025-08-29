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
        
        /// <summary>
        /// 읽기 버퍼 크기 (바이트 단위) - 기본 4KB
        /// </summary>
        public int ReadBufferSize { get; set; } = 4096;
        
        /// <summary>
        /// 쓰기 버퍼 크기 (바이트 단위) - 기본 2KB
        /// </summary>
        public int WriteBufferSize { get; set; } = 2048;

        /// <summary>
        /// 설정 비교 (PortName 제외 - 메인화면에서만 관리)
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not SerialConnectionSettings other)
                return false;

            return PortName == other.PortName &&
                   BaudRate == other.BaudRate &&
                   Parity == other.Parity &&
                   DataBits == other.DataBits &&
                   StopBits == other.StopBits &&
                   Handshake == other.Handshake &&
                   ReadTimeout == other.ReadTimeout &&
                   WriteTimeout == other.WriteTimeout &&
                   ReadBufferSize == other.ReadBufferSize &&
                   WriteBufferSize == other.WriteBufferSize;
        }

        public override int GetHashCode()
        {
            var hash1 = HashCode.Combine(PortName, BaudRate, Parity, DataBits, StopBits, Handshake, ReadTimeout, WriteTimeout);
            var hash2 = HashCode.Combine(ReadBufferSize, WriteBufferSize);
            return HashCode.Combine(hash1, hash2);
        }
    }
}