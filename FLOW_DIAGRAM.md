# JSON Configuration Reload - Flow Diagram

## Before Fix (Required Restart)

```
User saves scenarios to JSON
    ↓
Settings changed in App.config
    ↓
User tries to reload scenarios
    ↓
❌ Scenarios NOT reloaded (stale data)
    ↓
🔄 Application restart required
    ↓
✅ Fresh scenarios loaded
```

## After Fix (Automatic Reload)

### Flow 1: Configuration Change Auto-Reload

```
User changes settings (Serial Port, API, etc.)
    ↓
ConfigurationService.SaveSerialSettings()
    ↓
ConfigurationChanged event fired
    ↓
MainViewModel.OnConfigurationChanged() triggered
    ↓
DataMappingService.ReloadScenariosFromFile()
    ↓
LoadScenariosFromFile() reads JSON
    ↓
MainViewModel.InitializeMappingScenarios()
    ↓
UI updated with fresh data
    ↓
✅ User sees: "Configuration reloaded - mapping scenarios updated from file"
```

### Flow 2: Window Open Reload

```
User clicks "Data Mapping" button
    ↓
MainViewModel.OpenDataMapping() called
    ↓
DataMappingService.ReloadScenariosFromFile()
    ↓
LoadScenariosFromFile() reads latest JSON
    ↓
InitializeMappingScenarios() syncs UI
    ↓
DataMappingWindow created with fresh data
    ↓
Window.Show()
    ↓
✅ User sees latest scenarios (including external edits)
```

### Flow 3: Application Shutdown (Proper Cleanup)

```
Application closing
    ↓
MainViewModel.Dispose() called
    ↓
_configurationService.ConfigurationChanged -= OnConfigurationChanged
    ↓
Event subscription removed
    ↓
✅ No memory leaks
```

## Code Interaction Diagram

```
┌─────────────────────────────────────────────────────────┐
│                     User Interface                       │
│  ┌────────────────┐         ┌─────────────────────┐    │
│  │ SerialConfig   │         │ DataMappingWindow   │    │
│  │ Window         │         │                     │    │
│  └────────┬───────┘         └──────────┬──────────┘    │
│           │                            │                │
└───────────┼────────────────────────────┼────────────────┘
            │                            │
            │ Settings Changed           │ Window Opened
            ↓                            ↓
┌─────────────────────────────────────────────────────────┐
│                    MainViewModel                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │  OnConfigurationChanged()                        │  │
│  │  - Triggered by ConfigurationChanged event       │  │
│  │  - Reloads scenarios                             │  │
│  │  - Updates UI                                    │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  OpenDataMapping()                               │  │
│  │  - Reloads scenarios before opening window       │  │
│  │  - Ensures fresh data                            │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────┬─────────────────────┬────────────────────┘
               │                     │
               │ Reload Request      │ Subscribe/Unsubscribe
               ↓                     ↓
┌──────────────────────────┐  ┌────────────────────────┐
│  DataMappingService      │  │  ConfigurationService  │
│  ┌────────────────────┐  │  │  ┌──────────────────┐ │
│  │ ReloadScenarios    │  │  │  │ ConfigurationCha│ │
│  │ FromFile()         │  │  │  │ nged Event      │ │
│  │ - Public method    │  │  │  │                 │ │
│  │ - Reads JSON file  │  │  │  │ Fired when:     │ │
│  └──────┬─────────────┘  │  │  │ - SaveSerial    │ │
│         │                │  │  │   Settings()    │ │
│         ↓                │  │  │ - ReloadConfig  │ │
│  ┌────────────────────┐  │  │  │   uration()     │ │
│  │ LoadScenarios      │  │  │  └──────────────────┘ │
│  │ FromFile()         │  │  └────────────────────────┘
│  │ - Private method   │  │
│  │ - Reads JSON       │  │
│  │ - Deserializes     │  │
│  │ - Clears old       │  │
│  │ - Adds new         │  │
│  └──────┬─────────────┘  │
│         │                │
└─────────┼────────────────┘
          │
          ↓
┌──────────────────────────┐
│  data-mapping-scenarios  │
│  .json                   │
│  - Persistent storage    │
│  - User editable         │
└──────────────────────────┘
```

## Event Subscription Lifecycle

```
Application Startup
    ↓
MainViewModel Constructor
    ↓
_configurationService.ConfigurationChanged += OnConfigurationChanged
    ↓
    ⏳ Application Running ⏳
    ↓
    ├─ Configuration changes → Event fired → Auto-reload
    ├─ Window opened → Manual reload
    └─ User actions continue...
    ↓
Application Shutdown
    ↓
MainViewModel.Dispose()
    ↓
_configurationService.ConfigurationChanged -= OnConfigurationChanged
    ↓
✅ Clean shutdown
```

## Key Components Modified

```
DataMappingService.cs
├── ReloadScenariosFromFile()  [NEW PUBLIC]
│   └── LoadScenariosFromFile()  [EXISTING PRIVATE]
│       ├── File.Exists check
│       ├── File.ReadAllText
│       ├── JsonSerializer.Deserialize
│       ├── _scenarios.Clear()
│       └── _scenarios.AddRange()

MainViewModel.cs
├── Constructor
│   └── Subscribe to ConfigurationChanged  [NEW]
├── OnConfigurationChanged()  [NEW]
│   ├── ReloadScenariosFromFile()
│   ├── Dispatcher.BeginInvoke
│   └── InitializeMappingScenarios()
├── OpenDataMapping()  [MODIFIED]
│   ├── ReloadScenariosFromFile()  [NEW]
│   ├── InitializeMappingScenarios()  [NEW]
│   └── Show window
└── Dispose()  [MODIFIED]
    └── Unsubscribe from ConfigurationChanged  [NEW]
```

## Scenarios Covered

| Scenario | Before Fix | After Fix |
|----------|------------|-----------|
| Change serial settings | ❌ Stale data | ✅ Auto-reload |
| Open mapping window | ❌ Stale data | ✅ Fresh load |
| Edit JSON externally | ❌ Not reflected | ✅ Reflected on window open |
| Save scenarios | ✅ Works | ✅ Still works |
| Memory cleanup | ⚠️ Potential leak | ✅ Proper cleanup |

---

**Legend:**
- ✅ = Fixed/Working
- ❌ = Broken/Not working
- ⚠️ = Warning/Potential issue
- 🔄 = Action required
- ⏳ = Ongoing state
