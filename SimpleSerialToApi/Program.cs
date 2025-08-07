using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("SimpleSerialToApi - Step 03 Configuration Management");
            Console.WriteLine("=================================================");
            
            // Setup dependency injection and logging
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application started - Step 03 Configuration Management implementation");
            
            // Demonstrate Configuration Service
            await DemonstrateConfigurationManagement(serviceProvider, logger);
            
            // Demonstrate Serial Communication Service (still working)
            await DemonstrateSerialCommunication(serviceProvider, logger);
            
            Console.WriteLine("\nConfiguration Management implementation complete. Press any key to exit...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(configure => configure.AddConsole());
            
            // Register Configuration Service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register Serial Communication Service
            services.AddTransient<ISerialCommunicationService, SerialCommunicationService>();
        }

        private static async Task DemonstrateConfigurationManagement(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            try
            {
                using var configService = serviceProvider.GetRequiredService<IConfigurationService>();
                
                Console.WriteLine("✓ Configuration Service registered");
                
                // Subscribe to configuration changed events
                configService.ConfigurationChanged += (sender, e) =>
                {
                    Console.WriteLine($"🔄 Configuration changed: {e.SectionName} - {e.ChangeDescription}");
                };
                
                // Demonstrate app settings access
                Console.WriteLine("\n📋 Application Settings:");
                Console.WriteLine($"   Serial Port: {configService.GetAppSetting("SerialPort")}");
                Console.WriteLine($"   Baud Rate: {configService.GetAppSetting("BaudRate")}");
                Console.WriteLine($"   Log Level: {configService.GetAppSetting("LogLevel")}");
                
                // Demonstrate configuration validation
                Console.WriteLine("\n🔍 Configuration Validation:");
                var isValid = configService.ValidateConfiguration();
                Console.WriteLine($"   Configuration is valid: {isValid}");
                
                // Demonstrate application configuration
                Console.WriteLine("\n⚙️ Application Configuration:");
                var appConfig = configService.ApplicationConfig;
                Console.WriteLine($"   Serial Settings: {appConfig.SerialSettings.PortName} @ {appConfig.SerialSettings.BaudRate} baud");
                Console.WriteLine($"   Message Queue: Max {appConfig.MessageQueueSettings.MaxQueueSize}, Batch {appConfig.MessageQueueSettings.BatchSize}");
                Console.WriteLine($"   API Endpoints: {appConfig.ApiEndpoints.Count} configured");
                Console.WriteLine($"   Mapping Rules: {appConfig.MappingRules.Count} configured");
                
                // Display API endpoints
                if (appConfig.ApiEndpoints.Any())
                {
                    Console.WriteLine("\n🌐 API Endpoints:");
                    foreach (var endpoint in appConfig.ApiEndpoints)
                    {
                        Console.WriteLine($"   - {endpoint.Name}: {endpoint.Method} {endpoint.Url} (Auth: {endpoint.AuthType}, Timeout: {endpoint.Timeout}ms)");
                    }
                }
                else
                {
                    Console.WriteLine("\n🌐 API Endpoints: None configured in current environment");
                }
                
                // Display mapping rules
                if (appConfig.MappingRules.Any())
                {
                    Console.WriteLine("\n🔗 Mapping Rules:");
                    foreach (var rule in appConfig.MappingRules)
                    {
                        Console.WriteLine($"   - {rule.SourceField} → {rule.TargetField} ({rule.DataType}) [Required: {rule.IsRequired}]");
                        if (!string.IsNullOrEmpty(rule.Converter))
                            Console.WriteLine($"     Converter: {rule.Converter}");
                        if (!string.IsNullOrEmpty(rule.DefaultValue))
                            Console.WriteLine($"     Default: {rule.DefaultValue}");
                    }
                }
                else
                {
                    Console.WriteLine("\n🔗 Mapping Rules: None configured in current environment");
                }
                
                // Demonstrate configuration reload
                Console.WriteLine("\n🔄 Testing Configuration Reload:");
                configService.ReloadConfiguration();
                
                // Demonstrate encryption/decryption (informational only)
                Console.WriteLine("\n🔐 Configuration Security Features:");
                Console.WriteLine("   Encryption/Decryption methods available for sensitive sections");
                Console.WriteLine("   Note: Actual encryption disabled in demo for compatibility");
                
                Console.WriteLine("✓ Configuration Management Service demonstration complete");
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during configuration management demonstration");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        private static async Task DemonstrateSerialCommunication(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            try
            {
                using var serialService = serviceProvider.GetRequiredService<ISerialCommunicationService>();
                
                Console.WriteLine("\n📡 Serial Communication (using configuration):");
                Console.WriteLine($"✓ Configuration loaded: Port {serialService.ConnectionSettings.PortName}, " +
                    $"Baud {serialService.ConnectionSettings.BaudRate}");
                
                // Subscribe to events
                serialService.DataReceived += (sender, e) =>
                {
                    Console.WriteLine($"📨 Data received: {e.DataAsText} (HEX: {e.DataAsHex})");
                };
                
                serialService.ConnectionStatusChanged += (sender, e) =>
                {
                    Console.WriteLine($"🔌 Connection status changed: {e.PortName} - {e.Message} (Connected: {e.IsConnected})");
                    if (e.Exception != null)
                    {
                        Console.WriteLine($"   Error: {e.Exception.Message}");
                    }
                };
                
                Console.WriteLine("✓ Event handlers registered");
                
                // Get available ports
                var availablePorts = serialService.GetAvailablePorts();
                Console.WriteLine($"✓ Available ports: {string.Join(", ", availablePorts)} (Total: {availablePorts.Length})");
                
                // Attempt connection (will likely fail without physical device)
                Console.WriteLine($"⚡ Attempting connection to {serialService.ConnectionSettings.PortName}...");
                var connected = await serialService.ConnectAsync();
                
                if (connected)
                {
                    Console.WriteLine("✓ Serial port connected successfully");
                    
                    // Test sending data
                    var textSent = await serialService.SendTextAsync("HELLO\r\n");
                    Console.WriteLine($"✓ Text send result: {textSent}");
                    
                    // Wait a moment for any responses
                    await Task.Delay(2000);
                    
                    // Disconnect
                    await serialService.DisconnectAsync();
                    Console.WriteLine("✓ Serial port disconnected");
                }
                else
                {
                    Console.WriteLine("⚠️ Could not connect to serial port (expected without physical device)");
                }
                
                Console.WriteLine("✓ Serial Communication Service demonstration complete");
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during serial communication demonstration");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
    }
}