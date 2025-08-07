using FluentAssertions;
using SimpleSerialToApi.Models;
using System.IO.Ports;

namespace SimpleSerialToApi.Tests.Models
{
    public class SerialConnectionSettingsTests
    {
        [Fact]
        public void SerialConnectionSettings_Should_HaveDefaultValues()
        {
            // Arrange & Act
            var settings = new SerialConnectionSettings();

            // Assert
            settings.PortName.Should().Be("COM3");
            settings.BaudRate.Should().Be(9600);
            settings.Parity.Should().Be(Parity.None);
            settings.DataBits.Should().Be(8);
            settings.StopBits.Should().Be(StopBits.One);
            settings.Handshake.Should().Be(Handshake.None);
            settings.ReadTimeout.Should().Be(5000);
            settings.WriteTimeout.Should().Be(5000);
        }

        [Fact]
        public void SerialConnectionSettings_Should_AllowPropertyModification()
        {
            // Arrange
            var settings = new SerialConnectionSettings();

            // Act
            settings.PortName = "COM1";
            settings.BaudRate = 115200;
            settings.Parity = Parity.Even;
            settings.DataBits = 7;
            settings.StopBits = StopBits.Two;
            settings.Handshake = Handshake.RequestToSend;
            settings.ReadTimeout = 3000;
            settings.WriteTimeout = 2000;

            // Assert
            settings.PortName.Should().Be("COM1");
            settings.BaudRate.Should().Be(115200);
            settings.Parity.Should().Be(Parity.Even);
            settings.DataBits.Should().Be(7);
            settings.StopBits.Should().Be(StopBits.Two);
            settings.Handshake.Should().Be(Handshake.RequestToSend);
            settings.ReadTimeout.Should().Be(3000);
            settings.WriteTimeout.Should().Be(2000);
        }
    }

    public class SerialDataReceivedEventArgsTests
    {
        [Fact]
        public void SerialDataReceivedEventArgs_Should_InitializeCorrectly()
        {
            // Arrange
            var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in bytes

            // Act
            var eventArgs = new SimpleSerialToApi.Models.SerialDataReceivedEventArgs(testData);

            // Assert
            eventArgs.Data.Should().BeEquivalentTo(testData);
            eventArgs.DataAsText.Should().Be("Hello");
            eventArgs.DataAsHex.Should().Be("48656C6C6F");
            eventArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SerialDataReceivedEventArgs_Should_ThrowArgumentNullException_WhenDataIsNull()
        {
            // Arrange, Act & Assert
            Action act = () => new SimpleSerialToApi.Models.SerialDataReceivedEventArgs(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*data*");
        }

        [Fact]
        public void SerialDataReceivedEventArgs_Should_HandleEmptyData()
        {
            // Arrange
            var emptyData = Array.Empty<byte>();

            // Act
            var eventArgs = new SimpleSerialToApi.Models.SerialDataReceivedEventArgs(emptyData);

            // Assert
            eventArgs.Data.Should().BeEmpty();
            eventArgs.DataAsText.Should().BeEmpty();
            eventArgs.DataAsHex.Should().BeEmpty();
            eventArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SerialDataReceivedEventArgs_Should_HandleBinaryData()
        {
            // Arrange
            var binaryData = new byte[] { 0x00, 0x01, 0xFF, 0x80 };

            // Act
            var eventArgs = new SimpleSerialToApi.Models.SerialDataReceivedEventArgs(binaryData);

            // Assert
            eventArgs.Data.Should().BeEquivalentTo(binaryData);
            eventArgs.DataAsHex.Should().Be("0001FF80");
            eventArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }
    }

    public class SerialConnectionEventArgsTests
    {
        [Fact]
        public void SerialConnectionEventArgs_Should_InitializeCorrectly_WithoutException()
        {
            // Arrange
            var isConnected = true;
            var portName = "COM3";
            var message = "Connection successful";

            // Act
            var eventArgs = new SerialConnectionEventArgs(isConnected, portName, message);

            // Assert
            eventArgs.IsConnected.Should().BeTrue();
            eventArgs.PortName.Should().Be(portName);
            eventArgs.Message.Should().Be(message);
            eventArgs.Exception.Should().BeNull();
            eventArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SerialConnectionEventArgs_Should_InitializeCorrectly_WithException()
        {
            // Arrange
            var isConnected = false;
            var portName = "COM3";
            var message = "Connection failed";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var eventArgs = new SerialConnectionEventArgs(isConnected, portName, message, exception);

            // Assert
            eventArgs.IsConnected.Should().BeFalse();
            eventArgs.PortName.Should().Be(portName);
            eventArgs.Message.Should().Be(message);
            eventArgs.Exception.Should().Be(exception);
            eventArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SerialConnectionEventArgs_Should_ThrowArgumentNullException_WhenPortNameIsNull()
        {
            // Arrange, Act & Assert
            Action act = () => new SerialConnectionEventArgs(true, null!, "message");
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*portName*");
        }

        [Fact]
        public void SerialConnectionEventArgs_Should_ThrowArgumentNullException_WhenMessageIsNull()
        {
            // Arrange, Act & Assert
            Action act = () => new SerialConnectionEventArgs(true, "COM3", null!);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*message*");
        }
    }
}