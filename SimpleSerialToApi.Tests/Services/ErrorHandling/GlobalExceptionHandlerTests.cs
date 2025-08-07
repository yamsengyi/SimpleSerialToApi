using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Services.ErrorHandling;
using SimpleSerialToApi.Services.Exceptions;
using SimpleSerialToApi.Services.Notifications;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.ErrorHandling
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
            _mockNotificationService = new Mock<INotificationService>();
            _handler = new GlobalExceptionHandler(_mockLogger.Object, _mockNotificationService.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!, _mockNotificationService.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenNotificationServiceIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(_mockLogger.Object, null!));
        }

        [Fact]
        public void HandleUnhandledException_ShouldLogCriticalError()
        {
            // Arrange
            var testException = new InvalidOperationException("Test exception");
            var context = "TestContext";

            // Act
            _handler.HandleUnhandledException(testException, context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception")),
                    testException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void HandleUnhandledException_SerialCommunicationException_ShouldUseSpecificHandler()
        {
            // Arrange
            var serialException = new SerialCommunicationException("COM1", SerialErrorType.PortNotFound, "Port not found");
            var context = "SerialTest";

            // Act
            _handler.HandleUnhandledException(serialException, context);

            // Assert
            _mockNotificationService.Verify(
                x => x.ShowError(It.Is<string>(msg => msg.Contains("시리얼 통신 오류")), serialException),
                Times.Once);
        }

        [Fact]
        public void HandleUnhandledException_ApiCommunicationException_ShouldUseSpecificHandler()
        {
            // Arrange
            var apiException = new ApiCommunicationException("TestEndpoint", "https://test.api", "POST", 500, "Server error");
            var context = "ApiTest";

            // Act
            _handler.HandleUnhandledException(apiException, context);

            // Assert
            _mockNotificationService.Verify(
                x => x.ShowError(It.Is<string>(msg => msg.Contains("API 통신 오류")), apiException),
                Times.Once);
        }

        [Fact]
        public void HandleUnhandledException_ConfigurationException_ShouldUseSpecificHandler()
        {
            // Arrange
            var configException = new ConfigurationException("TestSection", "TestSetting", "Invalid value");
            var context = "ConfigTest";

            // Act
            _handler.HandleUnhandledException(configException, context);

            // Assert
            _mockNotificationService.Verify(
                x => x.ShowError(It.Is<string>(msg => msg.Contains("설정 오류")), configException),
                Times.Once);
        }

        [Fact]
        public void HandleUnhandledException_TimeoutException_ShouldUseSpecificHandler()
        {
            // Arrange
            var timeoutException = new TimeoutException("Operation timed out");
            var context = "TimeoutTest";

            // Act
            _handler.HandleUnhandledException(timeoutException, context);

            // Assert
            _mockNotificationService.Verify(
                x => x.ShowWarning(It.Is<string>(msg => msg.Contains("작업 시간 초과"))),
                Times.Once);
        }

        [Fact]
        public void GenerateErrorReport_ShouldCreateComprehensiveReport()
        {
            // Arrange
            var testException = new InvalidOperationException("Test exception", new ArgumentException("Inner exception"));
            var context = "TestContext";
            var sender = new object();

            // Act
            var report = _handler.GenerateErrorReport(testException, context, sender);

            // Assert
            Assert.NotNull(report);
            Assert.Equal("System.InvalidOperationException", report.ExceptionType);
            Assert.Equal("Test exception", report.Message);
            Assert.Equal(context, report.Context);
            Assert.Equal(typeof(object).FullName, report.SenderType);
            Assert.Single(report.InnerExceptions);
            Assert.Equal("System.ArgumentException", report.InnerExceptions[0].ExceptionType);
            Assert.NotNull(report.SystemInfo);
            Assert.NotNull(report.ApplicationInfo);
        }

        [Fact]
        public void RegisterExceptionHandler_ShouldAllowCustomHandlers()
        {
            // Arrange
            var customHandlerCalled = false;
            var customHandler = new ExceptionHandler("Custom", (ex, ctx, sender) => 
            {
                customHandlerCalled = true;
            });

            // Act
            _handler.RegisterExceptionHandler<CustomTestException>(customHandler);
            _handler.HandleUnhandledException(new CustomTestException(), "Test");

            // Assert
            Assert.True(customHandlerCalled);
        }

        [Fact]
        public void HandleAppDomainException_ShouldHandleUnhandledExceptionEventArgs()
        {
            // Arrange
            var testException = new InvalidOperationException("Test exception");
            var eventArgs = new UnhandledExceptionEventArgs(testException, false);

            // Act
            _handler.HandleAppDomainException(this, eventArgs);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception")),
                    testException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void HandleTaskException_ShouldSetObserved()
        {
            // Arrange
            var innerException = new InvalidOperationException("Test exception");
            var aggregateException = new AggregateException(innerException);
            var eventArgs = new UnobservedTaskExceptionEventArgs(aggregateException);

            // Act
            _handler.HandleTaskException(this, eventArgs);

            // Assert
            Assert.True(eventArgs.Observed);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unobserved task exception")),
                    aggregateException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    // Custom exception for testing
    public class CustomTestException : Exception
    {
        public CustomTestException() : base("Custom test exception") { }
    }
}