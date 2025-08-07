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
            Console.WriteLine("SimpleSerialToApi - Step 02 Serial Communication");
            Console.WriteLine("===============================================");
            
            // Setup dependency injection and logging
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application started - Step 02 Serial Communication implementation");
            
            // Demonstrate Serial Communication Service
            await DemonstrateSerialCommunication(serviceProvider, logger);
            
            Console.WriteLine("\nSerial Communication implementation complete. Press any key to exit...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(configure => configure.AddConsole());
            
            // Register Serial Communication Service
            services.AddTransient<ISerialCommunicationService, SerialCommunicationService>();
        }

        private static async Task DemonstrateSerialCommunication(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            try
            {
                using var serialService = serviceProvider.GetRequiredService<ISerialCommunicationService>();
                
                Console.WriteLine("✓ Serial Communication Service registered");
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
                    
                    var dataSent = await serialService.SendDataAsync(new byte[] { 0x01, 0x02, 0x03 });
                    Console.WriteLine($"✓ Binary data send result: {dataSent}");
                    
                    // Test device initialization
                    var initialized = await serialService.InitializeDeviceAsync();
                    Console.WriteLine($"✓ Device initialization result: {initialized}");
                    
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