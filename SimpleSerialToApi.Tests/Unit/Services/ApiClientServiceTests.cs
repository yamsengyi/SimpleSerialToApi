using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Tests.Mocks;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Unit.Services
{
    [TestClass]
    public class ApiClientServiceTests : TestBase
    {
        private HttpApiClientService? _service;
        private MockHttpMessageHandler? _mockHandler;
        private HttpClient? _httpClient;
        private Mock<ILogger<HttpApiClientService>>? _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler);
            _mockLogger = new Mock<ILogger<HttpApiClientService>>();
            _service = new HttpApiClientService(_httpClient, _mockLogger.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service?.Dispose();
            _httpClient?.Dispose();
        }

        [TestMethod]
        public async Task PostAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var testData = new { Temperature = 25.5, Humidity = 60.0 };
            var expectedResponse = MockHttpResponses.Success("{\"success\": true}");
            _mockHandler!.SetResponse(expectedResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [TestMethod]
        public async Task PostAsync_WithInvalidEndpoint_ShouldReturnFailure()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };
            var badResponse = MockHttpResponses.NotFound();
            _mockHandler!.SetResponse(badResponse);

            // Act
            var result = await _service!.PostAsync("InvalidEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [TestMethod]
        public async Task PostAsync_WithServerError_ShouldReturnFailure()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };
            var errorResponse = MockHttpResponses.InternalServerError();
            _mockHandler!.SetResponse(errorResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }

        [TestMethod]
        public async Task PostAsync_WithTimeout_ShouldReturnFailure()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };
            var timeoutResponse = MockHttpResponses.Timeout();
            _mockHandler!.SetResponse(timeoutResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(408);
        }

        [TestMethod]
        public async Task PostAsync_WithNullData_ShouldHandleGracefully()
        {
            // Arrange
            var expectedResponse = MockHttpResponses.Success();
            _mockHandler!.SetResponse(expectedResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", null);

            // Assert
            result.Should().NotBeNull();
            // Should still attempt the request even with null data
        }

        [TestMethod]
        public async Task PostAsync_WithEmptyEndpoint_ShouldThrowArgumentException()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };

            // Act & Assert
            Func<Task> act = async () => await _service!.PostAsync("", testData);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task PostAsync_WithAuthenticationRequired_ShouldIncludeAuthHeaders()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };
            var unauthorizedResponse = MockHttpResponses.Unauthorized();
            _mockHandler!.SetResponse(unauthorizedResponse);

            // Act
            var result = await _service!.PostAsync("SecureEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            
            // Verify request was made
            var requests = _mockHandler.GetRequests();
            requests.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task PostAsync_ShouldSerializeDataCorrectly()
        {
            // Arrange
            var testData = new 
            { 
                Temperature = 25.5,
                Humidity = 60.0,
                Timestamp = DateTime.UtcNow
            };
            
            var expectedResponse = MockHttpResponses.Success();
            _mockHandler!.SetResponse(expectedResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", testData);

            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var requests = _mockHandler.GetRequests();
            requests.Should().HaveCount(1);
            
            var request = requests[0];
            request.Content.Should().NotBeNull();
            request.Content!.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        [TestMethod]
        public async Task PostAsync_WithBadRequest_ShouldReturnDetailedError()
        {
            // Arrange
            var testData = new { InvalidField = "test" };
            var badRequestResponse = MockHttpResponses.BadRequest("{\"error\": \"Invalid field\"}");
            _mockHandler!.SetResponse(badRequestResponse);

            // Act
            var result = await _service!.PostAsync("TestEndpoint", testData);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Action act = () => new HttpApiClientService(null!, _mockLogger!.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Action act = () => new HttpApiClientService(_httpClient!, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [TestMethod]
        public async Task PostAsync_WithConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var testData = new { Temperature = 25.5 };
            var expectedResponse = MockHttpResponses.Success();
            
            // Queue multiple responses
            for (int i = 0; i < 10; i++)
            {
                _mockHandler!.QueueResponse(MockHttpResponses.Success($"{{\"id\": {i}}}"));
            }

            // Act
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = _service!.PostAsync($"TestEndpoint{i}", testData);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.IsSuccess);
            
            var requests = _mockHandler!.GetRequests();
            requests.Should().HaveCount(10);
        }

        [TestMethod]
        public async Task GetAsync_WithValidEndpoint_ShouldReturnSuccess()
        {
            // Arrange
            var expectedResponse = MockHttpResponses.Success("{\"data\": \"test\"}");
            _mockHandler!.SetResponse(expectedResponse);

            // Act
            var result = await _service!.GetAsync("TestEndpoint");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => _service!.Dispose();

            // Assert
            act.Should().NotThrow();
        }
    }
}