using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Services.Logging;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Logging
{
    public class LoggerExtensionsTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public LoggerExtensionsTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void LogSerialCommunication_WithData_ShouldLogInformation()
        {
            // Arrange
            var port = "COM1";
            var action = "Connect";
            var data = "Connection established";

            // Act
            _mockLogger.Object.LogSerialCommunication(port, action, data);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Serial Connect on COM1: Connection established");
        }

        [Fact]
        public void LogSerialCommunication_WithoutData_ShouldLogInformation()
        {
            // Arrange
            var port = "COM1";
            var action = "Disconnect";

            // Act
            _mockLogger.Object.LogSerialCommunication(port, action);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Serial Disconnect on COM1");
        }

        [Fact]
        public void LogSerialError_ShouldLogError()
        {
            // Arrange
            var port = "COM2";
            var action = "Read";
            var exception = new InvalidOperationException("Read failed");
            var additionalContext = "Timeout occurred";

            // Act
            _mockLogger.Object.LogSerialError(port, action, exception, additionalContext);

            // Assert
            VerifyLogCalled(LogLevel.Error, "Serial Read failed on COM2. Context: Timeout occurred", exception);
        }

        [Fact]
        public void LogApiTransaction_WithStatusCode_ShouldLogInformation()
        {
            // Arrange
            var endpoint = "https://api.test.com/data";
            var method = "POST";
            var duration = TimeSpan.FromMilliseconds(1500);
            var success = true;
            var statusCode = 200;

            // Act
            _mockLogger.Object.LogApiTransaction(endpoint, method, duration, success, statusCode);

            // Assert
            VerifyLogCalled(LogLevel.Information, "API POST https://api.test.com/data completed in 1500ms - Success: True, StatusCode: 200");
        }

        [Fact]
        public void LogApiTransaction_WithoutStatusCode_ShouldLogInformation()
        {
            // Arrange
            var endpoint = "https://api.test.com/data";
            var method = "GET";
            var duration = TimeSpan.FromMilliseconds(800);
            var success = false;

            // Act
            _mockLogger.Object.LogApiTransaction(endpoint, method, duration, success);

            // Assert
            VerifyLogCalled(LogLevel.Information, "API GET https://api.test.com/data completed in 800ms - Success: False");
        }

        [Fact]
        public void LogApiError_WithFullDetails_ShouldLogError()
        {
            // Arrange
            var endpoint = "https://api.test.com/error";
            var method = "POST";
            var exception = new HttpRequestException("Request failed");
            var statusCode = 500;
            var responseContent = "Internal server error";

            // Act
            _mockLogger.Object.LogApiError(endpoint, method, exception, statusCode, responseContent);

            // Assert
            VerifyLogCalled(LogLevel.Error, "API POST https://api.test.com/error failed with status 500. Response: Internal server error", exception);
        }

        [Fact]
        public void LogQueueOperation_ShouldLogDebug()
        {
            // Arrange
            var operation = "Enqueue";
            var messageCount = 5;
            var queueName = "TestQueue";

            // Act
            _mockLogger.Object.LogQueueOperation(operation, messageCount, queueName);

            // Assert
            VerifyLogCalled(LogLevel.Debug, "Queue Enqueue: 5 messages in TestQueue");
        }

        [Fact]
        public void LogQueueError_WithMessageCount_ShouldLogError()
        {
            // Arrange
            var queueName = "ErrorQueue";
            var operation = "Dequeue";
            var exception = new InvalidOperationException("Queue is empty");
            var messageCount = 0;

            // Act
            _mockLogger.Object.LogQueueError(queueName, operation, exception, messageCount);

            // Assert
            VerifyLogCalled(LogLevel.Error, "Queue Dequeue failed for ErrorQueue with 0 messages", exception);
        }

        [Fact]
        public void LogDataProcessing_WithFullDetails_ShouldLogInformation()
        {
            // Arrange
            var operation = "Parse";
            var dataType = "JSON";
            var dataSize = 1024;
            var processingTime = TimeSpan.FromMilliseconds(50);

            // Act
            _mockLogger.Object.LogDataProcessing(operation, dataType, dataSize, processingTime);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Data Parse for JSON - Size: 1024 bytes - Duration: 50ms");
        }

        [Fact]
        public void LogDataProcessing_MinimalDetails_ShouldLogInformation()
        {
            // Arrange
            var operation = "Validate";
            var dataType = "XML";

            // Act
            _mockLogger.Object.LogDataProcessing(operation, dataType);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Data Validate for XML");
        }

        [Fact]
        public void LogConfigurationChange_WithOldAndNewValues_ShouldLogInformation()
        {
            // Arrange
            var sectionName = "Database";
            var settingName = "ConnectionString";
            var oldValue = "old_connection";
            var newValue = "new_connection";

            // Act
            _mockLogger.Object.LogConfigurationChange(sectionName, settingName, oldValue, newValue);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Configuration changed: Database.ConnectionString from 'old_connection' to 'new_connection'");
        }

        [Fact]
        public void LogConfigurationChange_LoadAction_ShouldLogInformation()
        {
            // Arrange
            var sectionName = "Api";
            var settingName = "Endpoint";
            var newValue = "https://new.api.com";

            // Act
            _mockLogger.Object.LogConfigurationChange(sectionName, settingName, newValue: newValue);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Configuration loaded: Api.Endpoint = 'https://new.api.com'");
        }

        [Fact]
        public void LogConfigurationError_ShouldLogError()
        {
            // Arrange
            var sectionName = "Logging";
            var settingName = "Level";
            var exception = new FormatException("Invalid log level");

            // Act
            _mockLogger.Object.LogConfigurationError(sectionName, settingName, exception);

            // Assert
            VerifyLogCalled(LogLevel.Error, "Configuration error in Logging.Level", exception);
        }

        [Fact]
        public void LogPerformanceMetric_WithAllDetails_ShouldLogInformation()
        {
            // Arrange
            var metricName = "ResponseTime";
            var value = 150.5;
            var unit = "ms";
            var context = "API Call";

            // Act
            _mockLogger.Object.LogPerformanceMetric(metricName, value, unit, context);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Performance: ResponseTime = 150.5 ms (Context: API Call)");
        }

        [Fact]
        public void LogPerformanceMetric_WithoutUnit_ShouldLogInformation()
        {
            // Arrange
            var metricName = "ProcessedItems";
            var value = 42.0;

            // Act
            _mockLogger.Object.LogPerformanceMetric(metricName, value);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Performance: ProcessedItems = 42");
        }

        [Fact]
        public void LogSecurityEvent_Successful_ShouldLogInformation()
        {
            // Arrange
            var eventType = "Authentication";
            var description = "User logged in";
            var userName = "testuser";
            var success = true;

            // Act
            _mockLogger.Object.LogSecurityEvent(eventType, description, userName, success);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Security Authentication: User logged in - User: testuser, Success: True");
        }

        [Fact]
        public void LogSecurityEvent_Failed_ShouldLogWarning()
        {
            // Arrange
            var eventType = "Authorization";
            var description = "Access denied";
            var success = false;

            // Act
            _mockLogger.Object.LogSecurityEvent(eventType, description, success: success);

            // Assert
            VerifyLogCalled(LogLevel.Warning, "Security Authorization: Access denied - Success: False");
        }

        [Fact]
        public void LogUserAction_WithFullDetails_ShouldLogInformation()
        {
            // Arrange
            var action = "Click";
            var component = "ConnectButton";
            var details = "Serial connection initiated";

            // Act
            _mockLogger.Object.LogUserAction(action, component, details);

            // Assert
            VerifyLogCalled(LogLevel.Information, "User Click in ConnectButton - Serial connection initiated");
        }

        [Fact]
        public void LogUserAction_MinimalDetails_ShouldLogInformation()
        {
            // Arrange
            var action = "Navigate";

            // Act
            _mockLogger.Object.LogUserAction(action);

            // Assert
            VerifyLogCalled(LogLevel.Information, "User Navigate");
        }

        [Fact]
        public void LogApplicationEvent_WithDuration_ShouldLogInformation()
        {
            // Arrange
            var eventType = "Startup";
            var description = "Application initialized";
            var duration = TimeSpan.FromSeconds(2.5);

            // Act
            _mockLogger.Object.LogApplicationEvent(eventType, description, duration);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Application Startup: Application initialized - Duration: 2500ms");
        }

        [Fact]
        public void LogApplicationEvent_WithoutDuration_ShouldLogInformation()
        {
            // Arrange
            var eventType = "Shutdown";
            var description = "Application closing";

            // Act
            _mockLogger.Object.LogApplicationEvent(eventType, description);

            // Assert
            VerifyLogCalled(LogLevel.Information, "Application Shutdown: Application closing");
        }

        [Theory]
        [InlineData(LogCategories.SerialCommunication, "SerialComm")]
        [InlineData(LogCategories.ApiCommunication, "ApiComm")]
        [InlineData(LogCategories.DataProcessing, "DataProc")]
        [InlineData(LogCategories.Configuration, "Config")]
        [InlineData(LogCategories.UserInterface, "UI")]
        [InlineData(LogCategories.Performance, "Perf")]
        [InlineData(LogCategories.Security, "Security")]
        public void LogCategories_ShouldHaveCorrectValues(string actual, string expected)
        {
            // Assert
            Assert.Equal(expected, actual);
        }

        private void VerifyLogCalled(LogLevel expectedLevel, string expectedMessage, Exception? expectedException = null)
        {
            _mockLogger.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                $"Expected log call with level {expectedLevel} and message containing '{expectedMessage}' was not made");
        }
    }
}