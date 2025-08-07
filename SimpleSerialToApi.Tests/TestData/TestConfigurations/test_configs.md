# Test Configuration Files

## test_config.json
```json
{
  "SerialSettings": {
    "PortName": "COM1",
    "BaudRate": 9600,
    "DataBits": 8,
    "Parity": "None",
    "StopBits": "One",
    "ReadTimeout": 5000,
    "WriteTimeout": 5000
  },
  "MessageQueueSettings": {
    "MaxQueueSize": 1000,
    "BatchSize": 10,
    "RetryCount": 3,
    "RetryInterval": 1000
  },
  "ApiEndpoints": [
    {
      "Name": "TemperatureEndpoint",
      "Url": "https://api.test.com/temperature",
      "Method": "POST",
      "Timeout": 30000,
      "Headers": {
        "Authorization": "Bearer test-token",
        "Content-Type": "application/json"
      }
    },
    {
      "Name": "PressureEndpoint",
      "Url": "https://api.test.com/pressure", 
      "Method": "POST",
      "Timeout": 30000,
      "Headers": {
        "Authorization": "Bearer test-token",
        "Content-Type": "application/json"
      }
    }
  ],
  "MappingRules": [
    {
      "SourceField": "temperature",
      "TargetField": "temp",
      "DataType": "decimal"
    },
    {
      "SourceField": "humidity",
      "TargetField": "humidity",
      "DataType": "decimal"
    },
    {
      "SourceField": "pressure",
      "TargetField": "pressure",
      "DataType": "decimal"
    }
  ]
}
```

## performance_test_config.json
```json
{
  "SerialSettings": {
    "PortName": "COM1",
    "BaudRate": 115200,
    "DataBits": 8,
    "Parity": "None",
    "StopBits": "One",
    "ReadTimeout": 1000,
    "WriteTimeout": 1000
  },
  "MessageQueueSettings": {
    "MaxQueueSize": 10000,
    "BatchSize": 100,
    "RetryCount": 1,
    "RetryInterval": 100
  }
}
```