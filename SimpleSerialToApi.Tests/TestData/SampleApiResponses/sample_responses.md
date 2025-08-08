# Sample API Response Files

## Success Response
```json
{
  "status": "success",
  "message": "Data received successfully",
  "id": "12345",
  "timestamp": "2023-01-01T12:30:45Z"
}
```

## Error Response - Bad Request
```json
{
  "status": "error",
  "error": "Bad Request",
  "message": "Invalid temperature value",
  "code": 400,
  "timestamp": "2023-01-01T12:30:45Z"
}
```

## Error Response - Unauthorized
```json
{
  "status": "error",
  "error": "Unauthorized",
  "message": "Invalid API token",
  "code": 401,
  "timestamp": "2023-01-01T12:30:45Z"
}
```

## Error Response - Internal Server Error
```json
{
  "status": "error",
  "error": "Internal Server Error",
  "message": "Database connection failed",
  "code": 500,
  "timestamp": "2023-01-01T12:30:45Z"
}
```

## Bulk Data Response
```json
{
  "status": "success",
  "message": "Bulk data processed",
  "processed_count": 100,
  "failed_count": 2,
  "batch_id": "BATCH_2023_001",
  "timestamp": "2023-01-01T12:30:45Z"
}
```