# SimpleSerialToApi - WPF User Interface

This document describes the WPF user interface implementation for SimpleSerialToApi, completed as part of Step 07.

## Overview

The WPF UI provides a comprehensive interface for managing serial communication, API integration, and system monitoring. It follows the MVVM (Model-View-ViewModel) architectural pattern for maintainable and testable code.

## Key Features

### Dashboard Tab
- **Application Status**: Real-time status display with start/stop controls
- **Serial Communication Panel**: Connection status, port settings, message statistics
- **API Status Panel**: Endpoint connectivity, success rates, response times
- **Queue & Statistics**: Message processing metrics, queue health monitoring

### Settings Tab
- **Serial Port Configuration**: COM port selection, baud rate, parity, data bits
- **API Settings**: Endpoint URLs, authentication, timeout settings
- **Queue Configuration**: Queue size limits, batch processing, retry policies
- **Connection Testing**: Built-in test functions for serial and API connections

### Monitoring Tab
- **API Endpoint Status**: Real-time status of all configured endpoints
- **Performance Charts**: Placeholder for performance visualization (requires LiveCharts)

### Log Viewer
- **Real-time Logging**: Expandable panel showing application logs
- **Log Filtering**: Filter by level (INFO, WARN, ERROR)
- **Log Export**: Export logs to file functionality

## Architecture

### MVVM Components

#### ViewModels
- `MainViewModel`: Primary application controller
- `SerialStatusViewModel`: Serial communication management
- `ApiStatusViewModel`: API endpoint monitoring
- `QueueStatusViewModel`: Message queue management
- `SettingsViewModel`: Configuration management

#### Infrastructure
- `ViewModelBase`: Base class implementing INotifyPropertyChanged
- `RelayCommand`: ICommand implementation for MVVM
- `Messenger`: Inter-ViewModel communication system

#### Custom Controls
- `StatusIndicator`: LED-style status display
- `LogViewer`: High-performance log viewing control
- `ProgressRing`: Processing status indicator
- `NotificationPanel`: Alert and notification display

#### Data Converters
- `StatusToColorConverter`: Maps connection status to colors
- `ApplicationStateToColorConverter`: Maps application state to colors
- `LogLevelToColorConverter`: Maps log levels to colors
- `BooleanToVisibilityConverter`: Boolean to visibility conversion
- `InverseBooleanConverter`: Boolean inversion for UI logic

## Usage Instructions

### Building and Running

**Prerequisites:**
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (recommended) or Visual Studio Code

**Build Commands:**
```bash
dotnet restore
dotnet build
dotnet run
```

**For Release:**
```bash
dotnet build --configuration Release
dotnet run --configuration Release
```

### Application Workflow

1. **Start the Application**: Launch using `dotnet run` or execute the built executable
2. **Configure Settings**: Go to Settings tab to configure serial ports and API endpoints
3. **Test Connections**: Use the test buttons to verify serial and API connectivity
4. **Start Processing**: Click Start on the Dashboard to begin data processing
5. **Monitor Status**: Use Dashboard and Monitoring tabs to track system health
6. **Review Logs**: Expand Log Viewer to see detailed application activity

### Configuration

The application uses the existing configuration system from previous steps:
- Serial port settings are loaded from `App.config`
- API endpoints are configured through the Settings UI
- Queue parameters can be adjusted in real-time

### Error Handling

- Connection failures are displayed in the UI with appropriate status indicators
- Errors are logged to the Log Viewer with timestamp and details
- Retry mechanisms are configurable through the Settings tab

## Technical Details

### Data Binding
- All UI updates use WPF data binding for reactive updates
- ObservableCollections provide automatic UI refresh for lists
- Commands enable MVVM-compliant user interactions

### Threading
- UI updates are marshaled to the UI thread using `Dispatcher.Invoke`
- Background processing doesn't block the UI
- Timer-based updates for real-time metrics

### Performance
- Log viewer uses virtualization for large log volumes
- Queue status updates are throttled to prevent UI flooding
- Efficient data binding minimizes unnecessary updates

### Extensibility
- Custom controls can be easily added
- New ViewModels can be registered through dependency injection
- Messaging system allows loose coupling between components

## Deployment

### Windows Deployment
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

### Requirements
- Target machine needs .NET 8 Runtime
- Serial port drivers must be installed for physical devices
- Network connectivity required for API endpoints

## Development Notes

- Code follows MVVM best practices
- All ViewModels implement proper disposal patterns
- Custom controls use templating for customization
- Comprehensive error handling and logging

## Future Enhancements

Potential improvements for future versions:
- Chart integration using LiveCharts or similar
- Dark/Light theme support
- Advanced log filtering and search
- Configuration import/export
- Multi-language support
- Advanced notification system

## Troubleshooting

### Common Issues

1. **Serial Port Access Denied**
   - Run application as administrator
   - Check if port is in use by another application

2. **API Connection Failures**
   - Verify network connectivity
   - Check firewall settings
   - Validate API endpoint URLs

3. **UI Not Updating**
   - Check if services are properly registered in DI container
   - Verify data binding expressions
   - Review log output for exceptions

### Debug Mode
The application provides extensive logging. Enable console logging to see detailed debug information.

## License
This implementation follows the same license as the main SimpleSerialToApi project.