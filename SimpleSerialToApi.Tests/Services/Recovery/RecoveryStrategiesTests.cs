using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Exceptions;
using SimpleSerialToApi.Services.Recovery;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Recovery
{
    public class RecoveryStrategiesTests
    {
        [Fact]
        public void RecoveryContext_ShouldInitializeWithDefaults()
        {
            // Act
            var context = new RecoveryContext();

            // Assert
            Assert.NotNull(context.Properties);
            Assert.Empty(context.Properties);
            Assert.Equal(string.Empty, context.OperationName);
            Assert.Equal(0, context.AttemptNumber);
            Assert.Equal(TimeSpan.Zero, context.ElapsedTime);
            Assert.Null(context.LastException);
        }

        [Fact]
        public void RecoveryResult_ShouldInitializeWithDefaults()
        {
            // Act
            var result = new RecoveryResult<bool>();

            // Assert
            Assert.False(result.Success);
            Assert.False(result.Result);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.Exception);
            Assert.Equal(TimeSpan.Zero, result.RecoveryTime);
            Assert.Null(result.RecoveryStrategy);
        }

        public class SerialConnectionRecoveryStrategyTests
        {
            private readonly Mock<ISerialCommunicationService> _mockSerialService;
            private readonly Mock<ILogger<SerialConnectionRecoveryStrategy>> _mockLogger;
            private readonly SerialConnectionRecoveryStrategy _strategy;

            public SerialConnectionRecoveryStrategyTests()
            {
                _mockSerialService = new Mock<ISerialCommunicationService>();
                _mockLogger = new Mock<ILogger<SerialConnectionRecoveryStrategy>>();
                _strategy = new SerialConnectionRecoveryStrategy(_mockSerialService.Object, _mockLogger.Object);
            }

            [Fact]
            public void Constructor_ShouldThrowArgumentNullException_WhenSerialServiceIsNull()
            {
                // Arrange, Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    new SerialConnectionRecoveryStrategy(null!, _mockLogger.Object));
            }

            [Fact]
            public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
            {
                // Arrange, Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    new SerialConnectionRecoveryStrategy(_mockSerialService.Object, null!));
            }

            [Fact]
            public void Properties_ShouldHaveCorrectValues()
            {
                // Assert
                Assert.Equal(5, _strategy.MaxAttempts);
                Assert.Equal("SerialConnectionRecovery", _strategy.StrategyName);
            }

            [Theory]
            [InlineData(typeof(SerialCommunicationException), true)]
            [InlineData(typeof(UnauthorizedAccessException), true)]
            [InlineData(typeof(IOException), true)]
            [InlineData(typeof(ArgumentException), false)]
            [InlineData(typeof(NotSupportedException), false)]
            public void CanHandle_ShouldReturnCorrectResult(Type exceptionType, bool expectedResult)
            {
                // Arrange
                var exception = (Exception)Activator.CreateInstance(exceptionType, "Test exception")!;

                // Act
                var result = _strategy.CanHandle(exception);

                // Assert
                Assert.Equal(expectedResult, result);
            }

            [Fact]
            public void CanHandle_InvalidOperationExceptionWithPortMessage_ShouldReturnTrue()
            {
                // Arrange
                var exception = new InvalidOperationException("The port is already open");

                // Act
                var result = _strategy.CanHandle(exception);

                // Assert
                Assert.True(result);
            }

            [Fact]
            public void CanHandle_InvalidOperationExceptionWithoutPortMessage_ShouldReturnFalse()
            {
                // Arrange
                var exception = new InvalidOperationException("Something else happened");

                // Act
                var result = _strategy.CanHandle(exception);

                // Assert
                Assert.False(result);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_SerialException_ShouldHandleSpecificErrorTypes()
            {
                // Arrange
                var serialException = new SerialCommunicationException("COM1", SerialErrorType.PortNotFound, "Port not found");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                _mockSerialService.Setup(x => x.ConnectionSettings)
                    .Returns(new SerialConnectionSettings { PortName = "COM1" });

                // Act
                var result = await _strategy.AttemptRecoveryAsync(serialException, context);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("SerialConnectionRecovery", result.RecoveryStrategy);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_UnauthorizedAccessException_ShouldWaitAndRetry()
            {
                // Arrange
                var exception = new UnauthorizedAccessException("Access denied");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                _mockSerialService.Setup(x => x.ConnectAsync())
                    .ReturnsAsync(true);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Act
                var result = await _strategy.AttemptRecoveryAsync(exception, context);

                stopwatch.Stop();

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(stopwatch.ElapsedMilliseconds >= 4000); // Should wait at least 5 seconds, accounting for test timing
            }

            [Fact]
            public async Task AttemptRecoveryAsync_IOException_ShouldHandleIOErrors()
            {
                // Arrange
                var exception = new IOException("Device not ready");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                _mockSerialService.Setup(x => x.ConnectAsync())
                    .ReturnsAsync(false);

                // Act
                var result = await _strategy.AttemptRecoveryAsync(exception, context);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("SerialConnectionRecovery", result.RecoveryStrategy);
                Assert.True(result.RecoveryTime > TimeSpan.Zero);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_RecoveryException_ShouldReturnFailedResult()
            {
                // Arrange
                var exception = new SerialCommunicationException("COM1", SerialErrorType.PortNotFound, "Port not found");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                _mockSerialService.Setup(x => x.ConnectAsync())
                    .ThrowsAsync(new InvalidOperationException("Recovery failed"));

                // Act
                var result = await _strategy.AttemptRecoveryAsync(exception, context);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.Success);
                Assert.Contains("Recovery failed", result.ErrorMessage ?? string.Empty);
            }
        }

        public class ApiConnectionRecoveryStrategyTests
        {
            private readonly Mock<ILogger<ApiConnectionRecoveryStrategy>> _mockLogger;
            private readonly ApiConnectionRecoveryStrategy _strategy;

            public ApiConnectionRecoveryStrategyTests()
            {
                _mockLogger = new Mock<ILogger<ApiConnectionRecoveryStrategy>>();
                _strategy = new ApiConnectionRecoveryStrategy(_mockLogger.Object);
            }

            [Fact]
            public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
            {
                // Arrange, Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    new ApiConnectionRecoveryStrategy(null!));
            }

            [Fact]
            public void Properties_ShouldHaveCorrectValues()
            {
                // Assert
                Assert.Equal(3, _strategy.MaxAttempts);
                Assert.Equal("ApiConnectionRecovery", _strategy.StrategyName);
            }

            [Theory]
            [InlineData(typeof(ApiCommunicationException), true)]
            [InlineData(typeof(HttpRequestException), true)]
            [InlineData(typeof(TaskCanceledException), true)]
            [InlineData(typeof(TimeoutException), true)]
            [InlineData(typeof(ArgumentException), false)]
            [InlineData(typeof(NotSupportedException), false)]
            public void CanHandle_ShouldReturnCorrectResult(Type exceptionType, bool expectedResult)
            {
                // Arrange
                var exception = exceptionType == typeof(ApiCommunicationException) 
                    ? new ApiCommunicationException("Test", "http://test.com", "GET", "Test message")
                    : (Exception)Activator.CreateInstance(exceptionType, "Test exception")!;

                // Act
                var result = _strategy.CanHandle(exception);

                // Assert
                Assert.Equal(expectedResult, result);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_ApiException_RateLimited_ShouldWaitLonger()
            {
                // Arrange
                var apiException = new ApiCommunicationException("Test", "http://test.com", "GET", 429, "Rate limited");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Act
                var result = await _strategy.AttemptRecoveryAsync(apiException, context);

                stopwatch.Stop();

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(stopwatch.ElapsedMilliseconds >= 50000); // Should wait about 60 seconds, accounting for test timing variance
            }

            [Fact]
            public async Task AttemptRecoveryAsync_ApiException_ServerError_ShouldWaitModerately()
            {
                // Arrange
                var apiException = new ApiCommunicationException("Test", "http://test.com", "POST", 500, "Internal server error");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Act
                var result = await _strategy.AttemptRecoveryAsync(apiException, context);

                stopwatch.Stop();

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(stopwatch.ElapsedMilliseconds >= 25000); // Should wait about 30 seconds
            }

            [Fact]
            public async Task AttemptRecoveryAsync_HttpException_ShouldWaitAndRetry()
            {
                // Arrange
                var httpException = new HttpRequestException("Network error");
                var context = new RecoveryContext { AttemptNumber = 1 };
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Act
                var result = await _strategy.AttemptRecoveryAsync(httpException, context);

                stopwatch.Stop();

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(stopwatch.ElapsedMilliseconds >= 10000); // Should wait about 15 seconds
            }

            [Fact]
            public async Task AttemptRecoveryAsync_TimeoutException_ShouldAllowRetry()
            {
                // Arrange
                var timeoutException = new TimeoutException("Operation timed out");
                var context = new RecoveryContext { AttemptNumber = 1 };

                // Act
                var result = await _strategy.AttemptRecoveryAsync(timeoutException, context);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.Equal("ApiConnectionRecovery", result.RecoveryStrategy);
            }
        }

        public class RecoveryManagerTests
        {
            private readonly Mock<ILogger<RecoveryManager>> _mockLogger;
            private readonly Mock<IRecoveryStrategy<bool>> _mockStrategy1;
            private readonly Mock<IRecoveryStrategy<bool>> _mockStrategy2;
            private readonly RecoveryManager _recoveryManager;

            public RecoveryManagerTests()
            {
                _mockLogger = new Mock<ILogger<RecoveryManager>>();
                _mockStrategy1 = new Mock<IRecoveryStrategy<bool>>();
                _mockStrategy2 = new Mock<IRecoveryStrategy<bool>>();
                
                var strategies = new[] { _mockStrategy1.Object, _mockStrategy2.Object };
                _recoveryManager = new RecoveryManager(_mockLogger.Object, strategies);
            }

            [Fact]
            public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
            {
                // Arrange, Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    new RecoveryManager(null!, new List<IRecoveryStrategy<bool>>()));
            }

            [Fact]
            public void Constructor_ShouldAcceptNullStrategies()
            {
                // Arrange, Act & Assert
                var manager = new RecoveryManager(_mockLogger.Object, null);
                Assert.NotNull(manager);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_MatchingStrategy_ShouldUseStrategy()
            {
                // Arrange
                var testException = new InvalidOperationException("Test exception");
                var operationName = "TestOperation";
                
                _mockStrategy1.Setup(x => x.CanHandle(testException)).Returns(true);
                _mockStrategy1.Setup(x => x.MaxAttempts).Returns(2);
                _mockStrategy1.Setup(x => x.StrategyName).Returns("TestStrategy");
                _mockStrategy1.Setup(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()))
                    .ReturnsAsync(new RecoveryResult<bool> { Success = true, Result = true });

                _mockStrategy2.Setup(x => x.CanHandle(testException)).Returns(false);

                // Act
                var result = await _recoveryManager.AttemptRecoveryAsync<bool>(testException, operationName);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(result.Result);
                Assert.Equal("TestStrategy", result.RecoveryStrategy);
                
                _mockStrategy1.Verify(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()), Times.Once);
                _mockStrategy2.Verify(x => x.AttemptRecoveryAsync(It.IsAny<Exception>(), It.IsAny<RecoveryContext>()), Times.Never);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_NoMatchingStrategy_ShouldReturnFailure()
            {
                // Arrange
                var testException = new InvalidOperationException("Test exception");
                var operationName = "TestOperation";
                
                _mockStrategy1.Setup(x => x.CanHandle(testException)).Returns(false);
                _mockStrategy2.Setup(x => x.CanHandle(testException)).Returns(false);

                // Act
                var result = await _recoveryManager.AttemptRecoveryAsync<bool>(testException, operationName);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.Success);
                Assert.Contains("All recovery strategies failed", result.ErrorMessage ?? string.Empty);
            }

            [Fact]
            public async Task AttemptRecoveryAsync_StrategyFailsButHasMoreAttempts_ShouldRetry()
            {
                // Arrange
                var testException = new InvalidOperationException("Test exception");
                var operationName = "TestOperation";
                
                _mockStrategy1.Setup(x => x.CanHandle(testException)).Returns(true);
                _mockStrategy1.Setup(x => x.MaxAttempts).Returns(3);
                _mockStrategy1.Setup(x => x.StrategyName).Returns("TestStrategy");
                
                var callCount = 0;
                _mockStrategy1.Setup(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()))
                    .ReturnsAsync(() =>
                    {
                        callCount++;
                        return callCount < 3 
                            ? new RecoveryResult<bool> { Success = false, ErrorMessage = "Attempt failed" }
                            : new RecoveryResult<bool> { Success = true, Result = true };
                    });

                // Act
                var result = await _recoveryManager.AttemptRecoveryAsync<bool>(testException, operationName);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(result.Result);
                
                _mockStrategy1.Verify(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()), Times.Exactly(3));
            }

            [Fact]
            public async Task AttemptRecoveryAsync_StrategyThrowsException_ShouldContinueToNextAttempt()
            {
                // Arrange
                var testException = new InvalidOperationException("Test exception");
                var operationName = "TestOperation";
                
                _mockStrategy1.Setup(x => x.CanHandle(testException)).Returns(true);
                _mockStrategy1.Setup(x => x.MaxAttempts).Returns(2);
                _mockStrategy1.Setup(x => x.StrategyName).Returns("TestStrategy");
                
                var callCount = 0;
                _mockStrategy1.Setup(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()))
                    .Returns(() =>
                    {
                        callCount++;
                        if (callCount == 1)
                        {
                            throw new InvalidOperationException("Strategy failed");
                        }
                        return Task.FromResult(new RecoveryResult<bool> { Success = true, Result = true });
                    });

                // Act
                var result = await _recoveryManager.AttemptRecoveryAsync<bool>(testException, operationName);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.True(result.Result);
                
                _mockStrategy1.Verify(x => x.AttemptRecoveryAsync(testException, It.IsAny<RecoveryContext>()), Times.Exactly(2));
            }
        }
    }
}