using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Factory for creating and managing HTTP clients with connection pooling
    /// </summary>
    public class ApiClientFactory : IApiClientFactory, IDisposable
    {
        private readonly IConfigurationService _configService;
        private readonly ILogger<ApiClientFactory> _logger;
        private readonly ConcurrentDictionary<string, HttpClient> _clients;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public ApiClientFactory(IConfigurationService configService, ILogger<ApiClientFactory> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clients = new ConcurrentDictionary<string, HttpClient>();
        }

        public HttpClient CreateClient(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            var endpointConfig = GetEndpointConfiguration(endpointName);
            if (endpointConfig == null)
            {
                throw new InvalidOperationException($"Endpoint configuration not found: {endpointName}");
            }

            var httpClientConfig = GetHttpClientConfiguration();
            var client = CreateConfiguredHttpClient(endpointConfig, httpClientConfig);

            _logger.LogDebug("Created new HTTP client for endpoint {EndpointName}", endpointName);
            return client;
        }

        public HttpClient GetClient(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            return _clients.GetOrAdd(endpointName, name =>
            {
                lock (_lock)
                {
                    // Double-check locking pattern
                    if (_clients.TryGetValue(name, out var existingClient))
                        return existingClient;

                    var newClient = CreateClient(name);
                    return newClient;
                }
            });
        }

        public void RemoveClient(string endpointName)
        {
            if (_clients.TryRemove(endpointName, out var client))
            {
                client.Dispose();
            }
        }

        public void ClearAllClients()
        {
            var clientsToDispose = new List<HttpClient>();

            foreach (var kvp in _clients)
            {
                clientsToDispose.Add(kvp.Value);
            }

            _clients.Clear();

            foreach (var client in clientsToDispose)
            {
                client.Dispose();
            }

        }

        private ApiEndpointConfig? GetEndpointConfiguration(string endpointName)
        {
            try
            {
                var appConfig = _configService.ApplicationConfig;
                return appConfig.ApiEndpoints.FirstOrDefault(e => 
                    e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get endpoint configuration for {EndpointName}", endpointName);
                return null;
            }
        }

        private HttpClientConfiguration GetHttpClientConfiguration()
        {
            try
            {
                // Try to get HTTP client configuration from app settings
                var timeoutStr = _configService.GetAppSetting("DefaultTimeout");
                var maxRequestsStr = _configService.GetAppSetting("MaxConcurrentRequests");
                var enableCompressionStr = _configService.GetAppSetting("EnableCompression");

                var config = new HttpClientConfiguration();

                if (!string.IsNullOrEmpty(timeoutStr) && int.TryParse(timeoutStr, out var timeout))
                {
                    config.TimeoutSeconds = timeout / 1000; // Convert from milliseconds
                }

                if (!string.IsNullOrEmpty(maxRequestsStr) && int.TryParse(maxRequestsStr, out var maxRequests))
                {
                    config.MaxConcurrentRequests = maxRequests;
                }

                if (!string.IsNullOrEmpty(enableCompressionStr) && bool.TryParse(enableCompressionStr, out var enableCompression))
                {
                    config.EnableCompression = enableCompression;
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load HTTP client configuration, using defaults");
                return new HttpClientConfiguration();
            }
        }

        private HttpClient CreateConfiguredHttpClient(ApiEndpointConfig endpointConfig, HttpClientConfiguration httpConfig)
        {
            var handler = new HttpClientHandler();

            // Configure compression
            if (httpConfig.EnableCompression)
            {
                handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            }

            // Configure proxy if specified
            if (httpConfig.Proxy != null && !string.IsNullOrEmpty(httpConfig.Proxy.Address))
            {
                var proxy = new System.Net.WebProxy(httpConfig.Proxy.Address);
                
                if (!string.IsNullOrEmpty(httpConfig.Proxy.Username))
                {
                    proxy.Credentials = new System.Net.NetworkCredential(
                        httpConfig.Proxy.Username, 
                        httpConfig.Proxy.Password);
                }
                else if (httpConfig.Proxy.UseDefaultCredentials)
                {
                    proxy.UseDefaultCredentials = true;
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            // Configure SSL settings
            if (httpConfig.Ssl != null)
            {
                if (httpConfig.Ssl.IgnoreCertificateErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }
            }

            var client = new HttpClient(handler);

            // Configure timeout
            client.Timeout = TimeSpan.FromSeconds(endpointConfig.Timeout > 0 
                ? endpointConfig.Timeout / 1000 
                : httpConfig.TimeoutSeconds);

            // Add default headers from endpoint config
            foreach (var header in endpointConfig.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            // Add default headers from HTTP config
            foreach (var header in httpConfig.DefaultHeaders)
            {
                if (!client.DefaultRequestHeaders.Contains(header.Key))
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            // Set base address
            if (!string.IsNullOrEmpty(endpointConfig.Url))
            {
                try
                {
                    var uri = new Uri(endpointConfig.Url);
                    client.BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set base address for endpoint {EndpointName}, using full URL", endpointConfig.Name);
                }
            }

            return client;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ClearAllClients();
                _disposed = true;
                _logger.LogDebug("ApiClientFactory disposed");
            }
        }
    }
}