using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace SimpleSerialToApi.Tests
{
    /// <summary>
    /// Base class for all tests providing common setup and dependency injection
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ILogger Logger;
        protected readonly TestContext TestContext;

        protected TestBase()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();
            TestContext = new TestContext();
        }

        /// <summary>
        /// Configure services for testing. Override in derived classes for custom setup.
        /// </summary>
        /// <param name="services">Service collection to configure</param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder => builder.AddConsole());

            // Add test configuration
            services.AddSingleton<IConfiguration>(CreateTestConfiguration());
        }

        /// <summary>
        /// Create test configuration with default values
        /// </summary>
        /// <returns>Configuration instance for testing</returns>
        protected virtual IConfiguration CreateTestConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("SerialPort", "COM1"),
                    new KeyValuePair<string, string?>("BaudRate", "9600"),
                    new KeyValuePair<string, string?>("DataBits", "8"),
                    new KeyValuePair<string, string?>("Parity", "None"),
                    new KeyValuePair<string, string?>("StopBits", "One"),
                    new KeyValuePair<string, string?>("ReadTimeout", "5000"),
                    new KeyValuePair<string, string?>("WriteTimeout", "5000"),
                    new KeyValuePair<string, string?>("LogLevel", "Information"),
                    new KeyValuePair<string, string?>("MaxQueueSize", "1000"),
                    new KeyValuePair<string, string?>("BatchSize", "10"),
                    new KeyValuePair<string, string?>("RetryCount", "3"),
                    new KeyValuePair<string, string?>("RetryInterval", "1000")
                });

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public virtual void Dispose()
        {
            ServiceProvider?.Dispose();
            TestContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Test context for maintaining test state
    /// </summary>
    public class TestContext : IDisposable
    {
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public void Dispose()
        {
            Properties.Clear();
            GC.SuppressFinalize(this);
        }
    }
}