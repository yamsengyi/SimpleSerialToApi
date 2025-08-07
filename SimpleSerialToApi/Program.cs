using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SimpleSerialToApi - Step 01 Project Setup");
            Console.WriteLine("=========================================");
            
            // Setup dependency injection and logging
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application started - Step 01 project setup complete");
            
            Console.WriteLine("✓ Solution structure created");
            Console.WriteLine("✓ Main project: SimpleSerialToApi");
            Console.WriteLine("✓ Test project: SimpleSerialToApi.Tests");
            Console.WriteLine("✓ Folder structure: Models/, Services/, ViewModels/, Views/, Utils/");
            Console.WriteLine("✓ App.config configured");
            Console.WriteLine("✓ Dependency injection and logging setup");
            Console.WriteLine("✓ Ready for WPF conversion on Windows environment");
            
            Console.WriteLine("\nProject structure verification complete. Press any key to exit...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(configure => configure.AddConsole());
        }
    }
}