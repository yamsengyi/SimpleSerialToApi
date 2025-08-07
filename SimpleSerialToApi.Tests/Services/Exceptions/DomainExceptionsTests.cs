using SimpleSerialToApi.Services.Exceptions;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Exceptions
{
    public class DomainExceptionsTests
    {
        [Fact]
        public void SerialCommunicationException_ShouldInitializeCorrectly()
        {
            // Arrange
            var portName = "COM1";
            var errorType = SerialErrorType.PortNotFound;
            var message = "Port not found";

            // Act
            var exception = new SerialCommunicationException(portName, errorType, message);

            // Assert
            Assert.Equal(portName, exception.PortName);
            Assert.Equal(errorType, exception.ErrorType);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.AdditionalData);
        }

        [Fact]
        public void SerialCommunicationException_WithAdditionalData_ShouldInitializeCorrectly()
        {
            // Arrange
            var portName = "COM1";
            var errorType = SerialErrorType.ReadTimeout;
            var message = "Read timeout occurred";
            var additionalData = "Timeout after 5000ms";

            // Act
            var exception = new SerialCommunicationException(portName, errorType, message, additionalData);

            // Assert
            Assert.Equal(portName, exception.PortName);
            Assert.Equal(errorType, exception.ErrorType);
            Assert.Equal(message, exception.Message);
            Assert.Equal(additionalData, exception.AdditionalData);
        }

        [Fact]
        public void SerialCommunicationException_ShouldThrowArgumentNullException_WhenPortNameIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SerialCommunicationException(null!, SerialErrorType.PortNotFound, "Test message"));
        }

        [Fact]
        public void SerialCommunicationException_ToString_ShouldIncludeAllDetails()
        {
            // Arrange
            var portName = "COM1";
            var errorType = SerialErrorType.PortNotFound;
            var message = "Port not found";
            var additionalData = "Additional context";
            var innerException = new InvalidOperationException("Inner exception");

            var exception = new SerialCommunicationException(portName, errorType, message, additionalData, innerException);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"SerialCommunicationException: {errorType} on port {portName}: {message}", result);
            Assert.Contains($"Additional Data: {additionalData}", result);
            Assert.Contains("Inner Exception:", result);
        }

        [Fact]
        public void ApiCommunicationException_ShouldInitializeCorrectly()
        {
            // Arrange
            var endpointName = "TestEndpoint";
            var endpointUrl = "https://api.test.com";
            var httpMethod = "POST";
            var statusCode = 500;
            var message = "Server error";
            var responseContent = "Internal server error";

            // Act
            var exception = new ApiCommunicationException(endpointName, endpointUrl, httpMethod, statusCode, message, responseContent);

            // Assert
            Assert.Equal(endpointName, exception.EndpointName);
            Assert.Equal(endpointUrl, exception.EndpointUrl);
            Assert.Equal(httpMethod, exception.HttpMethod);
            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal(message, exception.Message);
            Assert.Equal(responseContent, exception.ResponseContent);
        }

        [Fact]
        public void ApiCommunicationException_ShouldThrowArgumentNullException_WhenEndpointNameIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ApiCommunicationException(null!, "https://test.com", "GET", "Test message"));
        }

        [Fact]
        public void ApiCommunicationException_ToString_ShouldIncludeAllDetails()
        {
            // Arrange
            var endpointName = "TestEndpoint";
            var endpointUrl = "https://api.test.com";
            var httpMethod = "POST";
            var statusCode = 404;
            var message = "Not found";
            var responseContent = "Resource not found";
            var responseTime = TimeSpan.FromMilliseconds(1500);

            var exception = new ApiCommunicationException(endpointName, endpointUrl, httpMethod, statusCode, message, responseContent, responseTime);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"ApiCommunicationException: {httpMethod} {endpointName} ({endpointUrl}): {message}", result);
            Assert.Contains($"Status: {statusCode}", result);
            Assert.Contains($"Response Time: {responseTime.TotalMilliseconds}ms", result);
            Assert.Contains($"Response Content: {responseContent}", result);
        }

        [Fact]
        public void ConfigurationException_ShouldInitializeCorrectly()
        {
            // Arrange
            var sectionName = "AppSettings";
            var settingName = "DatabaseConnection";
            var message = "Invalid connection string";

            // Act
            var exception = new ConfigurationException(sectionName, settingName, message);

            // Assert
            Assert.Equal(sectionName, exception.SectionName);
            Assert.Equal(settingName, exception.SettingName);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.SettingValue);
            Assert.Null(exception.ExpectedType);
        }

        [Fact]
        public void ConfigurationException_WithValueAndType_ShouldInitializeCorrectly()
        {
            // Arrange
            var sectionName = "AppSettings";
            var settingName = "Timeout";
            var settingValue = "invalid_number";
            var expectedType = "int";
            var message = "Invalid integer value";

            // Act
            var exception = new ConfigurationException(sectionName, settingName, settingValue, expectedType, message);

            // Assert
            Assert.Equal(sectionName, exception.SectionName);
            Assert.Equal(settingName, exception.SettingName);
            Assert.Equal(settingValue, exception.SettingValue);
            Assert.Equal(expectedType, exception.ExpectedType);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void ConfigurationException_ShouldThrowArgumentNullException_WhenSectionNameIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ConfigurationException(null!, "SettingName", "Test message"));
        }

        [Fact]
        public void ConfigurationException_ToString_ShouldIncludeAllDetails()
        {
            // Arrange
            var sectionName = "AppSettings";
            var settingName = "Timeout";
            var settingValue = "invalid_number";
            var expectedType = "int";
            var message = "Invalid integer value";

            var exception = new ConfigurationException(sectionName, settingName, settingValue, expectedType, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"ConfigurationException: {sectionName}.{settingName}: {message}", result);
            Assert.Contains($"Value: '{settingValue}'", result);
            Assert.Contains($"Expected Type: {expectedType}", result);
        }

        [Fact]
        public void QueueOperationException_ShouldInitializeCorrectly()
        {
            // Arrange
            var queueName = "TestQueue";
            var operation = "Enqueue";
            var message = "Queue is full";

            // Act
            var exception = new QueueOperationException(queueName, operation, message);

            // Assert
            Assert.Equal(queueName, exception.QueueName);
            Assert.Equal(operation, exception.Operation);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.MessageCount);
            Assert.Null(exception.QueueCapacity);
        }

        [Fact]
        public void QueueOperationException_WithCapacityInfo_ShouldInitializeCorrectly()
        {
            // Arrange
            var queueName = "TestQueue";
            var operation = "Enqueue";
            var messageCount = 100;
            var queueCapacity = 50;
            var message = "Queue overflow";

            // Act
            var exception = new QueueOperationException(queueName, operation, messageCount, queueCapacity, message);

            // Assert
            Assert.Equal(queueName, exception.QueueName);
            Assert.Equal(operation, exception.Operation);
            Assert.Equal(messageCount, exception.MessageCount);
            Assert.Equal(queueCapacity, exception.QueueCapacity);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void DataParsingException_ShouldInitializeCorrectly()
        {
            // Arrange
            var dataFormat = "JSON";
            var message = "Invalid JSON format";

            // Act
            var exception = new DataParsingException(dataFormat, message);

            // Assert
            Assert.Equal(dataFormat, exception.DataFormat);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.RawData);
            Assert.Null(exception.ParserName);
            Assert.Null(exception.DataPosition);
        }

        [Fact]
        public void DataParsingException_WithFullContext_ShouldInitializeCorrectly()
        {
            // Arrange
            var dataFormat = "HEX";
            var rawData = "FF AA BB CC";
            var parserName = "HexDataParser";
            var dataPosition = 2;
            var message = "Invalid hex byte";

            // Act
            var exception = new DataParsingException(dataFormat, rawData, parserName, dataPosition, message);

            // Assert
            Assert.Equal(dataFormat, exception.DataFormat);
            Assert.Equal(rawData, exception.RawData);
            Assert.Equal(parserName, exception.ParserName);
            Assert.Equal(dataPosition, exception.DataPosition);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void DataParsingException_ShouldThrowArgumentNullException_WhenDataFormatIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DataParsingException(null!, "Test message"));
        }

        [Fact]
        public void DataParsingException_ToString_ShouldIncludeAllDetails()
        {
            // Arrange
            var dataFormat = "XML";
            var rawData = "<root><invalid></root>";
            var parserName = "XmlDataParser";
            var dataPosition = 15;
            var message = "Missing closing tag";

            var exception = new DataParsingException(dataFormat, rawData, parserName, dataPosition, message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains($"DataParsingException: Failed to parse {dataFormat} data: {message}", result);
            Assert.Contains($"Parser: {parserName}", result);
            Assert.Contains($"Position: {dataPosition}", result);
            Assert.Contains($"Raw Data: {rawData}", result);
        }

        [Theory]
        [InlineData(SerialErrorType.PortNotFound)]
        [InlineData(SerialErrorType.PortAccessDenied)]
        [InlineData(SerialErrorType.CommunicationLost)]
        [InlineData(SerialErrorType.ReadTimeout)]
        public void SerialErrorType_ShouldHaveAllExpectedValues(SerialErrorType errorType)
        {
            // Act & Assert - Just verify the enum values exist
            Assert.True(Enum.IsDefined(typeof(SerialErrorType), errorType));
        }
    }
}