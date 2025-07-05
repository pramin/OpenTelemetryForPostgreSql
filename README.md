# OpenTelemetryForPostgreSql

OpenTelemetry Exporters for PostgreSQL - Export traces, metrics, and logs to PostgreSQL databases.

## Overview

The `Int31h.Common.OpenTelemetry.PostgresExporters` library provides OpenTelemetry exporters that write telemetry data (traces, metrics, and logs) to PostgreSQL databases. This enables you to store and analyze your application's telemetry data using SQL queries and PostgreSQL's powerful analytics capabilities.

## Features

- **Trace Exporter**: Export OpenTelemetry traces to PostgreSQL
- **Metrics Exporter**: Export OpenTelemetry metrics to PostgreSQL  
- **Logs Exporter**: Export OpenTelemetry logs to PostgreSQL
- **Automatic Database Setup**: Automatically creates required tables and schema
- **Configurable**: Fully configurable connection parameters and export options
- **Robust Error Handling**: Includes comprehensive error handling and logging
- **High Test Coverage**: 80%+ code coverage with comprehensive unit tests

## Installation

```bash
dotnet add package Int31h.Common.OpenTelemetry.PostgresExporters
```

## Usage

### Configuration

Create a configuration object:

```csharp
var options = new PostgresExporterOptions
{
    ConnectionString = "Host=localhost;Database=telemetry;Username=user;Password=pass;",
    SchemaName = "telemetry", // Optional, defaults to "telemetry"
    AutoCreateDatabaseObjects = true, // Optional, defaults to true
    BatchSize = 100, // Optional, defaults to 100
    ExportTimeoutMs = 30000 // Optional, defaults to 30000
};
```

### Using the Exporters

#### Traces

```csharp
var tracerProvider = TracerProviderBuilder.Create()
    .AddSource("MyApplication")
    .AddProcessor(sp => new PostgresTraceExporter(options))
    .Build();
```

#### Metrics

```csharp
var meterProvider = MeterProviderBuilder.Create()
    .AddMeter("MyApplication")
    .AddReader(new PeriodicExportingMetricReader(
        new PostgresMetricsExporter(options), 
        options.ExportTimeoutMs))
    .Build();
```

#### Logs

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddOpenTelemetry(options =>
        options.AddProcessor(sp => new PostgresLogsExporter(exporterOptions))));
```

### Database Schema

The library automatically creates the following tables in the specified schema:

#### Traces Table
```sql
CREATE TABLE telemetry.traces (
    trace_id VARCHAR(32) NOT NULL,
    span_id VARCHAR(16) NOT NULL,
    parent_span_id VARCHAR(16),
    operation_name TEXT NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    duration_ns BIGINT NOT NULL,
    status_code INTEGER NOT NULL,
    status_message TEXT,
    attributes JSONB,
    events JSONB,
    resource JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    PRIMARY KEY (trace_id, span_id)
);
```

#### Metrics Table
```sql
CREATE TABLE telemetry.metrics (
    id BIGSERIAL PRIMARY KEY,
    metric_name TEXT NOT NULL,
    metric_type TEXT NOT NULL,
    metric_value DOUBLE PRECISION,
    metric_unit TEXT,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    attributes JSONB,
    resource JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

#### Logs Table
```sql
CREATE TABLE telemetry.logs (
    id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    severity_number INTEGER,
    severity_text TEXT,
    body TEXT,
    trace_id VARCHAR(32),
    span_id VARCHAR(16),
    attributes JSONB,
    resource JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | "" | PostgreSQL connection string |
| `SchemaName` | string | "telemetry" | Database schema name for tables |
| `AutoCreateDatabaseObjects` | bool | true | Whether to auto-create tables/schema |
| `BatchSize` | int | 100 | Batch size for exporting data |
| `ExportTimeoutMs` | int | 30000 | Export timeout in milliseconds |

## Requirements

- .NET 8.0 or later
- PostgreSQL 12.0 or later
- OpenTelemetry 1.12.0 or later

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.