# Fix: Auto-reload JSON Configuration After Settings Change

## Summary
This PR fixes the issue where JSON configuration files (`data-mapping-scenarios.json`) were not being reloaded after configuration changes, requiring an application restart.

## Problem Statement
**Issue (Korean)**: 기존 저장된 JSON을 설정변경후 불러오기 했을때 즉시 불러오지 않고 재시작이 필요함

**Translation**: When loading previously saved JSON after changing settings, it doesn't load immediately and requires a restart.

### Root Cause
The `LoadScenariosFromFile()` method in `DataMappingService` was only called once during the constructor. When configuration changes occurred (e.g., serial port settings), the JSON scenarios were not reloaded, leaving the application with stale data.

## Solution

### 1. Added Public Reload Method
**File**: `DataMappingService.cs`
- Added `ReloadScenariosFromFile()` public method
- Wraps the existing private `LoadScenariosFromFile()` method
- Allows external components to trigger JSON reload
- Logs successful reload operations

### 2. Auto-reload on Configuration Change
**File**: `MainViewModel.cs`
- Subscribe to `ConfigurationChanged` event from `ConfigurationService`
- Automatically reload scenarios when any configuration changes
- Update UI with fresh data via `InitializeMappingScenarios()`
- Display status message to user

### 3. Reload When Opening Mapping Window
**File**: `MainViewModel.cs`
- Reload scenarios from JSON before showing `DataMappingWindow`
- Ensures users always see the latest data
- Handles external file modifications

### 4. Proper Resource Cleanup
**File**: `MainViewModel.cs`
- Unsubscribe from `ConfigurationChanged` event in `Dispose()`
- Prevents memory leaks

## Technical Details

### Code Changes

#### DataMappingService.cs
```csharp
/// <summary>
/// Reload scenarios from file - public method for external reload triggers
/// </summary>
public bool ReloadScenariosFromFile()
{
    return LoadScenariosFromFile();
}
```

#### MainViewModel.cs Constructor
```csharp
// Subscribe to configuration change events - auto-reload JSON
_configurationService.ConfigurationChanged += OnConfigurationChanged;
```

#### MainViewModel.cs Event Handler
```csharp
private void OnConfigurationChanged(object? sender, Interfaces.ConfigurationChangedEventArgs e)
{
    if (_dataMappingService.ReloadScenariosFromFile())
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            InitializeMappingScenarios();
            Status = "Configuration reloaded - mapping scenarios updated from file";
        });
    }
}
```

#### MainViewModel.cs OpenDataMapping
```csharp
// Reload latest scenarios from JSON before opening window
if (_dataMappingService.ReloadScenariosFromFile())
{
    InitializeMappingScenarios();
}
```

#### MainViewModel.cs Dispose
```csharp
if (_configurationService != null)
{
    _configurationService.ConfigurationChanged -= OnConfigurationChanged;
}
```

## Behavior

### Scenario 1: Configuration Change Auto-reload
1. User changes serial port settings via SerialConfigWindow
2. `ConfigurationService.SaveSerialSettings()` is called
3. `ConfigurationChanged` event is raised
4. `OnConfigurationChanged()` handler executes
5. JSON scenarios are automatically reloaded
6. UI displays fresh data

### Scenario 2: Manual Window Reload
1. User clicks "Data Mapping" button
2. `OpenDataMapping()` method executes
3. Latest scenarios are loaded from JSON
4. UI collection is synchronized
5. Window displays current data

## Testing

Due to WPF/Windows requirements, automated testing requires a Windows environment. Manual testing should include:

### Test Cases
1. **Configuration Change Reload**
   - Change serial port settings and save
   - Verify status message: "Configuration reloaded - mapping scenarios updated from file"
   - Open DataMappingWindow and verify latest scenarios are shown

2. **Window Open Reload**
   - Save mapping scenarios
   - Close DataMappingWindow
   - Modify `data-mapping-scenarios.json` externally
   - Reopen DataMappingWindow
   - Verify modified content is displayed

3. **Scenario Persistence**
   - Add new mapping scenario
   - Click "저장" (Save) button
   - Change configuration settings
   - Verify scenarios are retained

4. **Memory Leak Prevention**
   - Run application
   - Change settings multiple times
   - Open/close DataMappingWindow multiple times
   - Exit application
   - Check for error/warning logs

## Performance Impact

### Positive
- No application restart required → Better UX
- Immediate configuration reflection → Higher productivity

### Considerations
- JSON file read on each configuration change
  - **Mitigated**: Small file size (max 10 scenarios)
  - **Mitigated**: Infrequent changes
  - **Mitigated**: Fast fail if file doesn't exist

- JSON file read when opening DataMappingWindow
  - **Mitigated**: Infrequent window opening
  - **Mitigated**: Fast synchronous I/O (< 10ms)

## Compatibility

### Backward Compatibility
- ✅ No existing API changes (only additions)
- ✅ Existing behavior preserved
- ✅ No breaking changes

### Forward Compatibility
- Changes are isolated and well-documented
- Easy to extend with additional reload triggers
- Compatible with future async file I/O improvements

## Merge Considerations

See `MERGE_NOTES.md` (Korean) for detailed merge conflict resolution guide.

### High-Risk Files
- `MainViewModel.cs` - Core ViewModel with many features
  - Constructor (event subscription)
  - Event handlers section (new method)
  - `OpenDataMapping()` method
  - `Dispose()` method

### Medium-Risk Files
- `DataMappingService.cs` - Mapping functionality improvements
  - New public method addition
  - Logging enhancements

## Future Improvements

1. **FileSystemWatcher**
   - Auto-reload when JSON file changes externally
   - Currently removed to prevent file locking issues

2. **Async File Loading**
   - For large JSON files support
   - Apply `async/await` pattern

3. **User Notifications**
   - Currently only logs errors
   - Consider MessageBox or Toast notifications

4. **Selective Reload**
   - Only reload on relevant configuration changes
   - Performance optimization

## Related Issues

- **Issue**: 기존 저장된 JSON을 설정변경후 불러오기 했을때 즉시 불러오지 않고 재시작이 필요함

---

**Date**: 2026-02-01  
**Version**: 1.0
