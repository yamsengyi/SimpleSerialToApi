using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.ViewModels;
using System;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.UI.ViewModels
{
    [TestClass]
    public class MainViewModelTests : TestBase
    {
        private MainViewModel? _viewModel;
        private Mock<ISerialCommunicationService>? _mockSerialService;
        private Mock<IApiClientService>? _mockApiService;

        [TestInitialize]
        public void Setup()
        {
            _mockSerialService = new Mock<ISerialCommunicationService>();
            _mockApiService = new Mock<IApiClientService>();
            _viewModel = new MainViewModel(_mockSerialService.Object, _mockApiService.Object);
        }

        [TestMethod]
        public void Constructor_ShouldInitializeWithCorrectDefaults()
        {
            // Assert
            _viewModel!.IsApplicationRunning.Should().BeFalse();
            _viewModel.ApplicationStatus.Should().Be("Stopped");
            _viewModel.StartApplicationCommand.Should().NotBeNull();
            _viewModel.StopApplicationCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void StartApplicationCommand_WhenExecuted_ShouldChangeStatus()
        {
            // Arrange
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            _viewModel!.IsApplicationRunning.Should().BeFalse();

            // Act
            _viewModel.StartApplicationCommand.Execute(null);

            // Assert
            _viewModel.IsApplicationRunning.Should().BeTrue();
            _viewModel.ApplicationStatus.Should().Be("Running");
        }

        [TestMethod]
        public void StopApplicationCommand_WhenExecuted_ShouldChangeStatus()
        {
            // Arrange - Start application first
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            _mockSerialService.Setup(s => s.DisconnectAsync()).Returns(Task.CompletedTask);
            
            _viewModel!.StartApplicationCommand.Execute(null);
            _viewModel.IsApplicationRunning.Should().BeTrue();

            // Act
            _viewModel.StopApplicationCommand.Execute(null);

            // Assert
            _viewModel.IsApplicationRunning.Should().BeFalse();
            _viewModel.ApplicationStatus.Should().Be("Stopped");
        }

        [TestMethod]
        public void StartApplicationCommand_WhenSerialConnectionFails_ShouldShowError()
        {
            // Arrange
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(false);

            // Act
            _viewModel!.StartApplicationCommand.Execute(null);

            // Assert
            _viewModel.IsApplicationRunning.Should().BeFalse();
            _viewModel.ApplicationStatus.Should().Be("Connection Failed");
        }

        [TestMethod]
        public void PropertyChanged_ShouldFireForIsApplicationRunning()
        {
            // Arrange
            var propertyChanged = false;
            _viewModel!.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.IsApplicationRunning))
                    propertyChanged = true;
            };

            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);

            // Act
            _viewModel.StartApplicationCommand.Execute(null);

            // Assert
            propertyChanged.Should().BeTrue();
        }

        [TestMethod]
        public void PropertyChanged_ShouldFireForApplicationStatus()
        {
            // Arrange
            var statusChangedFired = false;
            _viewModel!.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.ApplicationStatus))
                    statusChangedFired = true;
            };

            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);

            // Act
            _viewModel.StartApplicationCommand.Execute(null);

            // Assert
            statusChangedFired.Should().BeTrue();
        }

        [TestMethod]
        public void StartApplicationCommand_CanExecute_ShouldReturnCorrectState()
        {
            // Arrange & Act - Initial state
            var canExecuteInitial = _viewModel!.StartApplicationCommand.CanExecute(null);

            // Start application
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            _viewModel.StartApplicationCommand.Execute(null);
            
            var canExecuteAfterStart = _viewModel.StartApplicationCommand.CanExecute(null);

            // Assert
            canExecuteInitial.Should().BeTrue("Should be able to start when stopped");
            canExecuteAfterStart.Should().BeFalse("Should not be able to start when running");
        }

        [TestMethod]
        public void StopApplicationCommand_CanExecute_ShouldReturnCorrectState()
        {
            // Arrange & Act - Initial state
            var canExecuteInitial = _viewModel!.StopApplicationCommand.CanExecute(null);

            // Start application
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            _viewModel.StartApplicationCommand.Execute(null);
            
            var canExecuteAfterStart = _viewModel.StopApplicationCommand.CanExecute(null);

            // Assert
            canExecuteInitial.Should().BeFalse("Should not be able to stop when already stopped");
            canExecuteAfterStart.Should().BeTrue("Should be able to stop when running");
        }

        [TestMethod]
        public async Task SerialDataReceived_ShouldUpdateUI()
        {
            // Arrange
            var dataReceivedFired = false;
            _viewModel!.SerialDataReceived += (sender, data) => dataReceivedFired = true;

            // Simulate data received event from serial service
            _mockSerialService!.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            _mockSerialService.Setup(s => s.DataReceived).Returns(Task.FromResult("TEMP:25.5C"));

            // Act
            _viewModel.StartApplicationCommand.Execute(null);

            // Simulate data received
            _mockSerialService.Raise(s => s.DataReceived += null, "TEMP:25.5C");

            // Assert
            // In a real implementation, this would verify UI updates
            dataReceivedFired.Should().BeTrue();
        }

        [TestMethod]
        public void Constructor_WithNullSerialService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainViewModel(null!, _mockApiService!.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("serialService");
        }

        [TestMethod]
        public void Constructor_WithNullApiService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainViewModel(_mockSerialService!.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("apiService");
        }

        [TestMethod]
        public void ConnectionStatus_ShouldReflectSerialServiceState()
        {
            // Arrange
            _mockSerialService!.Setup(s => s.IsConnected).Returns(false);

            // Act & Assert - Initial state
            _viewModel!.ConnectionStatus.Should().Be("Disconnected");

            // Simulate connection
            _mockSerialService.Setup(s => s.IsConnected).Returns(true);
            _mockSerialService.Setup(s => s.ConnectAsync()).ReturnsAsync(true);
            
            _viewModel.StartApplicationCommand.Execute(null);
            _viewModel.ConnectionStatus.Should().Be("Connected");
        }

        [TestMethod]
        public void MessageCount_ShouldTrackProcessedMessages()
        {
            // Arrange & Act - Initial state
            _viewModel!.ProcessedMessageCount.Should().Be(0);

            // Simulate processing messages
            _viewModel.IncrementProcessedMessages();
            _viewModel.ProcessedMessageCount.Should().Be(1);

            _viewModel.IncrementProcessedMessages();
            _viewModel.ProcessedMessageCount.Should().Be(2);
        }

        [TestMethod]
        public void ResetStatistics_ShouldClearCounters()
        {
            // Arrange
            _viewModel!.IncrementProcessedMessages();
            _viewModel.IncrementProcessedMessages();
            _viewModel.ProcessedMessageCount.Should().Be(2);

            // Act
            _viewModel.ResetStatistics();

            // Assert
            _viewModel.ProcessedMessageCount.Should().Be(0);
        }

        [TestMethod]
        public void ErrorCount_ShouldTrackErrors()
        {
            // Arrange & Act - Initial state
            _viewModel!.ErrorCount.Should().Be(0);

            // Simulate errors
            _viewModel.IncrementErrorCount();
            _viewModel.ErrorCount.Should().Be(1);

            _viewModel.IncrementErrorCount();
            _viewModel.ErrorCount.Should().Be(2);
        }

        [TestMethod]
        public void LastError_ShouldStoreLatestErrorMessage()
        {
            // Arrange
            var errorMessage = "Serial port connection failed";

            // Act
            _viewModel!.SetLastError(errorMessage);

            // Assert
            _viewModel.LastError.Should().Be(errorMessage);
            _viewModel.ErrorCount.Should().Be(1);
        }

        [TestMethod]
        public void Dispose_ShouldReleaseResources()
        {
            // Act & Assert - Should not throw
            Action act = () => _viewModel!.Dispose();
            act.Should().NotThrow();
        }
    }
}