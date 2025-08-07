using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Exponential backoff retry policy implementation using Polly
    /// </summary>
    public class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly RetryPolicy _config;
        private readonly ILogger _logger;
        private readonly IAsyncPolicy _policy;

        public int MaxAttempts => _config.MaxAttempts;

        public ExponentialBackoffRetryPolicy(RetryPolicy config, ILogger<ExponentialBackoffRetryPolicy> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _policy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<Exception>(ex => ShouldRetryException(ex))
                .WaitAndRetryAsync(
                    retryCount: _config.MaxAttempts - 1, // Polly doesn't count the initial attempt
                    sleepDurationProvider: retryAttempt => _config.GetDelay(retryAttempt),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry attempt {RetryCount} in {Delay}ms due to: {ExceptionMessage}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                    });
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await _policy.ExecuteAsync(async () =>
                {
                    var result = await operation();
                    
                    // For ApiResponse types, check if we should retry based on status code
                    if (result is ApiResponse apiResponse && !apiResponse.IsSuccess)
                    {
                        if (_config.RetryableStatusCodes.Contains(apiResponse.StatusCode))
                        {
                            throw new HttpRequestException($"API returned retryable status code {apiResponse.StatusCode}: {apiResponse.ErrorMessage}");
                        }
                    }
                    
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All retry attempts failed");
                throw;
            }
        }

        public bool ShouldRetry(Exception exception, int attemptNumber)
        {
            if (attemptNumber >= MaxAttempts)
                return false;

            return ShouldRetryException(exception);
        }

        public TimeSpan GetDelay(int attemptNumber)
        {
            return _config.GetDelay(attemptNumber);
        }

        private bool ShouldRetryException(Exception exception)
        {
            return exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   _config.RetryableExceptions.Any(type => type.IsAssignableFrom(exception.GetType()));
        }
    }

    /// <summary>
    /// Fixed delay retry policy implementation
    /// </summary>
    public class FixedDelayRetryPolicy : IRetryPolicy
    {
        private readonly RetryPolicy _config;
        private readonly ILogger _logger;
        private readonly IAsyncPolicy _policy;

        public int MaxAttempts => _config.MaxAttempts;

        public FixedDelayRetryPolicy(RetryPolicy config, ILogger<FixedDelayRetryPolicy> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _policy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<Exception>(ex => ShouldRetryException(ex))
                .WaitAndRetryAsync(
                    retryCount: _config.MaxAttempts - 1,
                    sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_config.BaseDelayMilliseconds),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry attempt {RetryCount} in {Delay}ms due to: {ExceptionMessage}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                    });
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await _policy.ExecuteAsync(async () =>
                {
                    var result = await operation();
                    
                    // For ApiResponse types, check if we should retry based on status code
                    if (result is ApiResponse apiResponse && !apiResponse.IsSuccess)
                    {
                        if (_config.RetryableStatusCodes.Contains(apiResponse.StatusCode))
                        {
                            throw new HttpRequestException($"API returned retryable status code {apiResponse.StatusCode}: {apiResponse.ErrorMessage}");
                        }
                    }
                    
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All retry attempts failed");
                throw;
            }
        }

        public bool ShouldRetry(Exception exception, int attemptNumber)
        {
            if (attemptNumber >= MaxAttempts)
                return false;

            return ShouldRetryException(exception);
        }

        public TimeSpan GetDelay(int attemptNumber)
        {
            return TimeSpan.FromMilliseconds(_config.BaseDelayMilliseconds);
        }

        private bool ShouldRetryException(Exception exception)
        {
            return exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   _config.RetryableExceptions.Any(type => type.IsAssignableFrom(exception.GetType()));
        }
    }

    /// <summary>
    /// No retry policy - fails immediately
    /// </summary>
    public class NoRetryPolicy : IRetryPolicy
    {
        private readonly ILogger _logger;

        public int MaxAttempts => 1;

        public NoRetryPolicy(ILogger<NoRetryPolicy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await operation();
        }

        public bool ShouldRetry(Exception exception, int attemptNumber)
        {
            return false;
        }

        public TimeSpan GetDelay(int attemptNumber)
        {
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Factory for creating retry policies
    /// </summary>
    public class RetryPolicyFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public RetryPolicyFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Create a retry policy based on configuration
        /// </summary>
        /// <param name="config">Retry policy configuration</param>
        /// <returns>Configured retry policy</returns>
        public IRetryPolicy CreateRetryPolicy(RetryPolicy config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.MaxAttempts <= 1)
            {
                var noRetryLogger = _loggerFactory.CreateLogger<RetryPolicyFactory>();
                noRetryLogger.LogDebug("Creating NoRetryPolicy (MaxAttempts: {MaxAttempts})", config.MaxAttempts);
                return new NoRetryPolicy(_loggerFactory.CreateLogger<NoRetryPolicy>());
            }

            if (config.UseExponentialBackoff)
            {
                var expLogger = _loggerFactory.CreateLogger<RetryPolicyFactory>();
                expLogger.LogDebug("Creating ExponentialBackoffRetryPolicy (MaxAttempts: {MaxAttempts}, BaseDelay: {BaseDelay}ms)",
                    config.MaxAttempts, config.BaseDelayMilliseconds);
                return new ExponentialBackoffRetryPolicy(config, _loggerFactory.CreateLogger<ExponentialBackoffRetryPolicy>());
            }
            else
            {
                var fixedLogger = _loggerFactory.CreateLogger<RetryPolicyFactory>();
                fixedLogger.LogDebug("Creating FixedDelayRetryPolicy (MaxAttempts: {MaxAttempts}, Delay: {Delay}ms)",
                    config.MaxAttempts, config.BaseDelayMilliseconds);
                return new FixedDelayRetryPolicy(config, _loggerFactory.CreateLogger<FixedDelayRetryPolicy>());
            }
        }

        /// <summary>
        /// Create default retry policy with exponential backoff
        /// </summary>
        /// <returns>Default retry policy</returns>
        public IRetryPolicy CreateDefaultRetryPolicy()
        {
            var defaultConfig = new RetryPolicy
            {
                MaxAttempts = 3,
                BaseDelayMilliseconds = 1000,
                UseExponentialBackoff = true,
                BackoffMultiplier = 2.0,
                MaxDelayMilliseconds = 30000
            };

            // Add common retryable status codes
            defaultConfig.RetryableStatusCodes.AddRange(new[] { 429, 502, 503, 504 });
            
            // Add common retryable exceptions
            defaultConfig.RetryableExceptions.AddRange(new[] 
            {
                typeof(HttpRequestException),
                typeof(TaskCanceledException),
                typeof(TimeoutException)
            });

            return CreateRetryPolicy(defaultConfig);
        }
    }
}