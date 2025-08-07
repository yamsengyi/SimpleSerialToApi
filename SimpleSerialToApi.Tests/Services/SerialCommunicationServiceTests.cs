using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using System.Configuration;

namespace SimpleSerialToApi.Tests.Services
{
    public class SerialCommunicationServiceTests
    {
        private readonly Mock<ILogger<SerialCommunicationService>> _mockLogger;
        private readonly SerialCommunicationService _serialService;

        public SerialCommunicationServiceTests()
        {
            _mockLogger = new Mock<ILogger<SerialCommunicationService>>();
            _serialService = new SerialCommunicationService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_Should_InitializeCorrectly()
        {
            // Arrange & Act
            var service = new SerialCommunicationService(_mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsConnected.Should().BeFalse();
            service.ConnectionSettings.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange, Act & Assert
            Action act = () => new SerialCommunicationService(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*logger*");
        }

        [Fact]
        public void ConnectionSettings_Should_LoadFromAppConfig()
        {
            // Arrange & Act
            var settings = _serialService.ConnectionSettings;

            // Assert
            settings.Should().NotBeNull();
            settings.PortName.Should().NotBeNullOrEmpty();
            settings.BaudRate.Should().BeGreaterThan(0);
            settings.DataBits.Should().BeGreaterThan(0);
            settings.ReadTimeout.Should().BeGreaterThan(0);
            settings.WriteTimeout.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetAvailablePorts_Should_ReturnStringArray()
        {
            // Arrange & Act
            var ports = _serialService.GetAvailablePorts();

            // Assert
            ports.Should().NotBeNull();
            ports.Should().BeOfType<string[]>();
        }

        [Fact]
        public async Task SendDataAsync_Should_ReturnFalse_WhenDataIsNull()
        {
            // Arrange, Act & Assert
            var result = await _serialService.SendDataAsync(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendDataAsync_Should_ReturnFalse_WhenDataIsEmpty()
        {
            // Arrange, Act & Assert
            var result = await _serialService.SendDataAsync(Array.Empty<byte>());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendDataAsync_Should_ReturnFalse_WhenNotConnected()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03 };

            // Act
            var result = await _serialService.SendDataAsync(data);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendTextAsync_Should_ReturnFalse_WhenTextIsNull()
        {
            // Arrange, Act & Assert
            var result = await _serialService.SendTextAsync(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendTextAsync_Should_ReturnFalse_WhenTextIsEmpty()
        {
            // Arrange, Act & Assert
            var result = await _serialService.SendTextAsync("");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendTextAsync_Should_ReturnFalse_WhenNotConnected()
        {
            // Arrange & Act
            var result = await _serialService.SendTextAsync("Test message");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeDeviceAsync_Should_ReturnFalse_WhenNotConnected()
        {
            // Arrange & Act
            var result = await _serialService.InitializeDeviceAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Dispose_Should_NotThrow()
        {
            // Arrange & Act
            Action act = () => _serialService.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void IsConnected_Should_ReturnFalse_Initially()
        {
            // Arrange & Act
            var isConnected = _serialService.IsConnected;

            // Assert
            isConnected.Should().BeFalse();
        }

        [Fact]
        public void ConnectionSettings_Should_HaveValidDefaults()
        {
            // Arrange & Act
            var settings = _serialService.ConnectionSettings;

            // Assert
            settings.PortName.Should().NotBeNullOrEmpty();
            settings.BaudRate.Should().Be(9600);
            settings.DataBits.Should().Be(8);
            settings.Parity.Should().Be(System.IO.Ports.Parity.None);
            settings.StopBits.Should().Be(System.IO.Ports.StopBits.One);
            settings.Handshake.Should().Be(System.IO.Ports.Handshake.None);
            settings.ReadTimeout.Should().Be(5000);
            settings.WriteTimeout.Should().Be(5000);
        }

        [Fact]
        public void Events_Should_BeInitializedCorrectly()
        {
            // Arrange
            var dataReceivedEventFired = false;
            var connectionStatusEventFired = false;

            // Act & Assert - Events should be assignable without throwing
            Action actDataReceived = () => _serialService.DataReceived += (sender, args) => dataReceivedEventFired = true;
            Action actConnectionStatus = () => _serialService.ConnectionStatusChanged += (sender, args) => connectionStatusEventFired = true;

            actDataReceived.Should().NotThrow();
            actConnectionStatus.Should().NotThrow();
        }

        [Fact] 
        public async Task ConnectAsync_Should_HandleMultipleConcurrentCalls()
        {
            // Arrange
            var tasks = new List<Task<bool>>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_serialService.ConnectAsync());
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All calls should complete without exceptions
            results.Should().NotBeNull();
            results.Length.Should().Be(10);
        }

        [Fact]
        public async Task DisconnectAsync_Should_HandleMultipleConcurrentCalls()
        {
            // Arrange & Act
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_serialService.DisconnectAsync());
            }

            // Assert - Should not throw
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }
    }
}