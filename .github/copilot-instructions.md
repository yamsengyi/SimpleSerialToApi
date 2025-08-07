# SimpleSerialToApi - .NET 8 WPF Serial Communication Application

SimpleSerialToApi is a Windows-based .NET 8 WPF application that collects data from serial devices and forwards it to APIs through an internal message queue with configurable mappings. The project requirements are documented in Korean in `doc/PRD.md`.

**IMPORTANT**: This repository may be in early development stages. If no source code exists yet, follow the project initialization steps to create the initial project structure based on the PRD specifications.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Development Environment Setup
- Install .NET 8 SDK:
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify installation: `dotnet --version` (should show 8.x.x)
- Install Visual Studio 2022 or Visual Studio Code with C# extension
- Ensure Windows SDK is available for WPF development

### Project Initialization (First Time Setup)
If the repository contains only documentation:
- **On Windows with Visual Studio**: Use File > New > Project > WPF App (.NET) with .NET 8.0
- **Command line alternative**: 
  - `dotnet new console -n SimpleSerialToApi -f net8.0` (then manually convert to WPF)
  - `dotnet new classlib -n SimpleSerialToApi.Core -f net8.0`
  - `dotnet new xunit -n SimpleSerialToApi.Tests -f net8.0`
  - `dotnet new sln -n SimpleSerialToApi`
  - `dotnet sln add SimpleSerialToApi/SimpleSerialToApi.csproj`
  - `dotnet sln add SimpleSerialToApi.Core/SimpleSerialToApi.Core.csproj`
  - `dotnet sln add SimpleSerialToApi.Tests/SimpleSerialToApi.Tests.csproj`
- **Manual WPF conversion**: Edit SimpleSerialToApi.csproj to include:
  ```xml
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  ```

### Building the Application
- `dotnet restore` -- takes 1-3 seconds for existing projects, up to 60 seconds for first-time restore. NEVER CANCEL. Set timeout to 5+ minutes.
- `dotnet build` -- takes 10-15 seconds for simple projects, up to 3 minutes for complex builds. NEVER CANCEL. Set timeout to 10+ minutes.
- `dotnet build --configuration Release` -- for production builds. Takes 20-30 seconds to 5 minutes. NEVER CANCEL. Set timeout to 15+ minutes.

### Running Tests
- `dotnet test` -- takes 10-30 seconds for basic tests, up to 2 minutes for comprehensive test suites. NEVER CANCEL. Set timeout to 10+ minutes.
- `dotnet test --configuration Release --logger trx` -- for CI/CD integration

### Running the Application
- Development: `dotnet run --project SimpleSerialToApi`
- Release: `dotnet run --project SimpleSerialToApi --configuration Release`
- Direct executable: Navigate to `SimpleSerialToApi/bin/Debug/net8.0-windows/` and run `SimpleSerialToApi.exe`

## Validation

### Manual Testing Requirements
ALWAYS perform these validation scenarios after making changes:
1. **Serial Port Configuration**: Test serial port settings (COM port, baud rate, parity, stop bits)
   - Use Device Manager to verify available COM ports
   - Test with serial port emulator tools (e.g., com0com, Eltima Virtual Serial Port)
2. **Device Initialization**: Send initialization commands and verify ACK/NACK responses
   - Test HEX and TEXT format initialization commands
   - Verify timeout handling for non-responsive devices
3. **Data Flow**: Verify serial data is received, parsed, and queued correctly
   - Test with simulated serial data streams
   - Verify data parsing accuracy and error handling
4. **API Integration**: Test API endpoint connectivity and data transmission
   - Test with real API endpoints and mock endpoints
   - Verify authentication (Bearer token, Basic Auth) handling
   - Test retry logic for API failures
5. **Message Queue**: Verify queue processing, retry logic, and error handling
   - Load test with 1000+ concurrent messages
   - Test queue overflow scenarios and backpressure
6. **Configuration**: Test App.Config changes and hot-reload functionality
   - Modify API endpoints, serial port settings, mapping rules
   - Verify configuration validation and error reporting

### Critical Testing Scenarios
**Serial Communication Validation**:
```csharp
// Test serial port availability
var availablePorts = System.IO.Ports.SerialPort.GetPortNames();
// Test with known good/bad configurations
// Test timeout scenarios and error recovery
```

**API Integration Validation**:
- Test API connectivity: `curl -X POST https://api-endpoint.com/test -H "Authorization: Bearer token"`
- Verify JSON payload format matches API requirements
- Test network failure scenarios and retry mechanisms

### Required Testing Steps
- ALWAYS run `dotnet test` before committing changes
- ALWAYS test with at least one serial device connection scenario
- ALWAYS verify API endpoint connectivity with sample data
- Test configuration file changes and application behavior
- Verify logging functionality captures serial communication and API calls

### CI/CD Validation
- Run `dotnet format --verify-no-changes` to ensure code formatting
- Run `dotnet build --configuration Release --no-restore` for production readiness
- Ensure all tests pass: `dotnet test --configuration Release --no-build`

## Key Architecture Components

### Project Structure
```
SimpleSerialToApi/
├── SimpleSerialToApi/           # WPF Application Project
│   ├── MainWindow.xaml         # Main UI
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Views/                  # Additional Views
│   ├── App.config              # Configuration file
│   └── App.xaml                # Application entry
├── SimpleSerialToApi.Core/     # Core Business Logic
│   ├── Services/               # Serial, API, Queue services
│   ├── Models/                 # Data models
│   ├── Configuration/          # Config management
│   └── Interfaces/             # Service contracts
├── SimpleSerialToApi.Tests/    # Unit Tests
│   ├── Services/               # Service tests
│   └── Models/                 # Model tests
└── SimpleSerialToApi.sln       # Solution file
```

### Critical Components (Based on PRD)
- **SerialCommunicationService**: Handles System.IO.Ports operations
- **MessageQueueService**: Internal queue for processing serial data (target: 1000+ concurrent messages)
- **ApiIntegrationService**: HTTP client for REST API calls using HttpClient
- **ConfigurationService**: App.Config management using ConfigurationManager
- **DataMappingService**: Maps serial data to API payload format
- **LoggingService**: Comprehensive logging for serial, queue, and API operations

### Performance Requirements
- Serial data parsing and API transmission: < 1 second
- Message queue capacity: 1000+ concurrent messages
- Auto-reconnection for serial and network failures
- Configuration hot-reload (if possible without restart)

## Common Tasks

### Adding New Device Support
1. Extend `IDeviceProtocol` interface in SimpleSerialToApi.Core
2. Implement device-specific parsing in `Services/Devices/`
3. Update App.Config with new device mappings
4. Add corresponding unit tests
5. Test with actual device or mock serial data

### API Endpoint Configuration
1. Update App.Config `<appSettings>` section:
   ```xml
   <add key="ApiEndpoint" value="https://your-api.com/data" />
   <add key="ApiMethod" value="POST" />
   <add key="AuthType" value="Bearer" />
   <add key="AuthToken" value="your-token" />
   ```
2. Restart application or implement hot-reload mechanism
3. Test API connectivity with sample payload

### Debugging Serial Communication
- Use built-in logging to monitor serial port activity
- Verify COM port availability in Device Manager
- Test with serial port simulators for development
- Check baud rate, parity, and stop bit configurations match device specs

### Building for Distribution
- `dotnet publish -c Release -r win-x64 --self-contained false`
- Verify App.Config is included in output directory
- Test on target Windows machine without .NET 8 SDK installed

## Timing Expectations

### Build Operations
- Initial restore: 1-60 seconds depending on project complexity. NEVER CANCEL. Set timeout to 5+ minutes.
- Clean build: 10 seconds to 3 minutes depending on project size. NEVER CANCEL. Set timeout to 10+ minutes.
- Incremental build: 3-15 seconds for small changes. NEVER CANCEL. Set timeout to 5+ minutes.
- Release build: 20 seconds to 5 minutes for full optimization. NEVER CANCEL. Set timeout to 15+ minutes.

### Test Operations  
- Unit tests: 10-30 seconds for basic test suites. NEVER CANCEL. Set timeout to 10+ minutes.
- Integration tests: 30 seconds to 3 minutes for comprehensive testing. NEVER CANCEL. Set timeout to 15+ minutes.

### Critical Warnings
- DO NOT cancel long-running operations prematurely
- .NET builds may appear to hang during package restoration - wait for completion
- Serial port operations may require elevated permissions on some Windows systems
- WPF applications require Windows environment - Linux/Mac development limited to core libraries only

## Development Notes

### Dependencies to Expect
- **System.IO.Ports** (Serial communication) - Add via `dotnet add package System.IO.Ports`
- **Microsoft.Extensions.Configuration** (App.Config management)
- **Microsoft.Extensions.Configuration.Json** (JSON configuration support)
- **Microsoft.Extensions.Logging** (Logging framework)
- **System.Net.Http** (API integration)
- **Newtonsoft.Json** or **System.Text.Json** (JSON serialization)
- **Microsoft.Extensions.DependencyInjection** (Dependency injection)
- **Microsoft.Extensions.Hosting** (Background service hosting)

### Package Installation Commands
```bash
# These commands take 3-15 seconds each. NEVER CANCEL. Set timeout to 5+ minutes.
dotnet add SimpleSerialToApi package System.IO.Ports
dotnet add SimpleSerialToApi package Microsoft.Extensions.Configuration
dotnet add SimpleSerialToApi package Microsoft.Extensions.Configuration.Json  
dotnet add SimpleSerialToApi package Microsoft.Extensions.Logging
dotnet add SimpleSerialToApi package Microsoft.Extensions.DependencyInjection
dotnet add SimpleSerialToApi package Microsoft.Extensions.Hosting
dotnet add SimpleSerialToApi package Newtonsoft.Json
```

### Common Issues and Solutions
- **Serial port access denied**: Run application as administrator or check port permissions
- **Serial port not found**: Verify COM port exists in Device Manager, check driver installation
- **API authentication failures**: Verify AuthToken format and endpoint configuration in App.Config
- **Message queue overflow**: Implement backpressure mechanisms and monitor queue depth
- **Configuration not loading**: Ensure App.config is in output directory and properly formatted
- **WPF threading issues**: Use Dispatcher for UI updates from background threads
- **Serial port already in use**: Ensure proper disposal of SerialPort objects and check for zombie processes
- **Network timeout on API calls**: Implement proper HttpClient timeout and retry policies

### Development Environment Constraints
- **Windows Required**: Serial port functionality and WPF require Windows environment
- **Linux/Mac Limitations**: Core business logic can be developed, but serial communication testing requires Windows
- **Virtual Machines**: Serial port passthrough may be required for VM development
- **Debugging Serial Communication**: Use serial port monitors (e.g., Free Serial Port Monitor) to debug data flow

### Performance Testing Commands
```bash
# Test message queue performance
dotnet run --configuration Release -- --test-mode --message-count 1000

# Monitor memory usage during high load
dotnet-counters monitor --process-name SimpleSerialToApi

# Profile CPU usage
dotnet-trace collect --process-name SimpleSerialToApi
```

Always build, test, and validate your changes thoroughly before committing. The application handles real-time serial communication and API integration - reliability is critical.