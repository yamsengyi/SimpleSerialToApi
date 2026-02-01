# Quick Reference: JSON Reload Fix

## Problem
JSON 설정 파일(`data-mapping-scenarios.json`)이 설정 변경 후 재시작 없이 리로드되지 않는 문제

## Solution Summary

### Files Changed
1. `SimpleSerialToApi/Services/DataMappingService.cs` (+11 lines)
2. `SimpleSerialToApi/ViewModels/MainViewModel.cs` (+49 lines)
3. `MERGE_NOTES.md` (New, 219 lines) - Korean merge guide
4. `PR_SUMMARY.md` (New, 210 lines) - English technical summary

**Total**: 4 files changed, 489 insertions(+)

### Key Changes

#### 1. DataMappingService.cs
```csharp
// NEW: Public method to reload scenarios from JSON
public bool ReloadScenariosFromFile()
```

#### 2. MainViewModel.cs
```csharp
// NEW: Subscribe to configuration changes
_configurationService.ConfigurationChanged += OnConfigurationChanged;

// NEW: Event handler for auto-reload
private void OnConfigurationChanged(object? sender, Interfaces.ConfigurationChangedEventArgs e)

// MODIFIED: Reload before opening window
private void OpenDataMapping()

// MODIFIED: Unsubscribe on dispose
public void Dispose()
```

## How It Works

### Auto-reload Triggers

1. **Configuration Change** → Auto-reload
   - User changes App.config settings (serial port, etc.)
   - `ConfigurationChanged` event fires
   - JSON scenarios reload automatically

2. **Open Mapping Window** → Fresh reload
   - User opens DataMappingWindow
   - JSON scenarios reload from file
   - Latest data always displayed

3. **Manual Save** → No change
   - User saves scenarios via UI
   - Already handled by existing code

## Testing (Manual - Windows Required)

```
✅ Change serial settings → Check status: "Configuration reloaded..."
✅ Modify JSON externally → Open window → Verify changes shown
✅ Save scenarios → Change settings → Scenarios retained
✅ Multiple open/close cycles → No memory leaks
```

## Merge Conflicts Guide

### If conflicts in MainViewModel.cs:

**Constructor** - Keep this line:
```csharp
_configurationService.ConfigurationChanged += OnConfigurationChanged;
```

**Event Handlers** - Keep entire method:
```csharp
private void OnConfigurationChanged(object? sender, ...)
```

**OpenDataMapping()** - Keep reload logic:
```csharp
if (_dataMappingService.ReloadScenariosFromFile())
{
    InitializeMappingScenarios();
    ...
}
```

**Dispose()** - Keep unsubscribe:
```csharp
_configurationService.ConfigurationChanged -= OnConfigurationChanged;
```

### If conflicts in DataMappingService.cs:

**Keep new method**:
```csharp
public bool ReloadScenariosFromFile()
```

## Full Documentation

- **Korean**: See `MERGE_NOTES.md`
- **English**: See `PR_SUMMARY.md`

## Commits

1. `5660ddb` - Initial plan
2. `4188082` - Add automatic JSON scenario reload on configuration change
3. `c08aa98` - Reload scenarios from JSON when opening DataMappingWindow
4. `4edbada` - Add comprehensive documentation for JSON reload fix

---
**Date**: 2026-02-01
