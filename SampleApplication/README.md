# Sample Application - OpenTelemetry PostgreSQL Exporters

This sample application demonstrates how to use the `Int31h.Common.OpenTelemetry.PostgresExporters` library to export OpenTelemetry traces, metrics, and logs to a PostgreSQL database.

## What the Sample Does

The sample simulates an e-commerce order processing system that:

- **🛒 Processes Orders**: Simulates order validation, pricing calculation, payment processing, and shipping
- **📊 Generates Metrics**: Tracks request counts, durations, and success/failure rates
- **🔍 Creates Traces**: Shows distributed tracing across multiple operations
- **📝 Logs Events**: Captures important events and errors throughout the process
- **⚠️ Handles Errors**: Demonstrates error scenarios and how they appear in telemetry

## Features Demonstrated

### Traces
- Nested spans showing parent-child relationships
- Span attributes with business context (order IDs, prices, tracking numbers)
- Status tracking (success/error scenarios)
- Distributed tracing correlation

### Metrics
- Counter metrics for tracking request totals
- Histogram metrics for measuring request durations
- Tags/dimensions for filtering and grouping
- Automatic periodic export

### Logs
- Structured logging with different severity levels
- Trace correlation (logs linked to traces)
- Contextual attributes and properties
- Error logging with exception details

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose (for PostgreSQL)

### 1. Start PostgreSQL

From the repository root directory:

```bash
# Start PostgreSQL container
docker-compose up -d

# Verify it's running
docker-compose ps
```

This starts PostgreSQL with these default credentials:
- **Host**: localhost
- **Port**: 5432
- **Database**: testdb
- **Username**: user
- **Password**: password

### 2. Build and Run the Sample

**Option 1: Using the convenience script (recommended)**

```bash
# From the repository root - this handles everything
./run-sample.sh
```

**Option 2: Manual steps**

```bash
# Start PostgreSQL container
docker-compose up -d

# Wait for it to be ready
sleep 10

# Run the sample
cd SampleApplication
dotnet run
```

The sample will:
1. Configure OpenTelemetry with PostgreSQL exporters
2. Generate realistic telemetry data
3. Export everything to PostgreSQL
4. Show you SQL queries to explore the data

### 3. Explore the Data

After running the sample, connect to your PostgreSQL database and run the suggested queries:

```sql
-- View traces
SELECT operation_name, status_code, duration_ns/1000000 as duration_ms, attributes
FROM sample_telemetry.traces ORDER BY start_time DESC LIMIT 10;

-- View metrics
SELECT metric_name, metric_type, metric_value, attributes
FROM sample_telemetry.metrics ORDER BY timestamp DESC LIMIT 10;

-- View logs
SELECT severity_text, body, trace_id, attributes
FROM sample_telemetry.logs ORDER BY timestamp DESC LIMIT 10;

-- See trace-log correlation
SELECT t.operation_name, l.severity_text, l.body
FROM sample_telemetry.traces t
JOIN sample_telemetry.logs l ON t.trace_id = l.trace_id
WHERE t.operation_name = 'process_order' LIMIT 5;
```

## Configuration Options

### Command Line Arguments

You can customize the database connection and schema:

```bash
# Custom connection string
dotnet run "Host=myhost;Database=mydb;Username=myuser;Password=mypass;"

# Custom connection string and schema
dotnet run "Host=myhost;Database=mydb;Username=myuser;Password=mypass;" "my_schema"
```

### Code Configuration

The sample shows several configuration patterns:

```csharp
// Basic configuration
var exporterOptions = new PostgresExporterOptions
{
    ConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=user;Password=password;",
    SchemaName = "sample_telemetry",
    AutoCreateDatabaseObjects = true,
    BatchSize = 10,
    ExportTimeoutMs = 30000
};

// Resource configuration
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("SampleApplication", "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "development",
            ["service.instance.id"] = Environment.MachineName,
            ["service.namespace"] = "samples"
        }))
```

## Understanding the Output

When you run the sample, you'll see:

```
🚀 OpenTelemetry PostgreSQL Exporters Sample Application
========================================================
📊 Using PostgreSQL connection: Host=localhost;Port=5432;Database=testdb;Username=user;Password=***
📋 Using schema: sample_telemetry

✅ OpenTelemetry configured with PostgreSQL exporters
🔄 Starting telemetry generation...

📦 Processing order ORD-001
✅ Successfully processed order ORD-001 in 245ms
📦 Processing order ORD-002
✅ Successfully processed order ORD-002 in 423ms
📦 Processing order ORD-003
❌ Failed to process order ORD-003
⚠️  Simulating error scenarios for demonstration
🔥 External API call failed
🗄️  Database operation failed

⏳ Waiting for telemetry to be exported...
✅ Sample completed! Check your PostgreSQL database for telemetry data.

📋 Sample PostgreSQL queries to view the exported data:
...
```

## Database Schema

The sample automatically creates these tables:

- **`sample_telemetry.traces`**: Distributed tracing data
- **`sample_telemetry.metrics`**: Metrics and measurements  
- **`sample_telemetry.logs`**: Log records with trace correlation

## Cleanup

```bash
# Stop PostgreSQL container
docker-compose down

# Remove volumes (deletes all data)
docker-compose down -v
```

## Next Steps

1. **Explore the Data**: Use the provided SQL queries to understand the exported telemetry
2. **Modify the Sample**: Change the business logic to see how it affects telemetry
3. **Add Your Own Telemetry**: Integrate the library into your own applications
4. **Production Setup**: Configure proper connection strings, error handling, and security

## Troubleshooting

### Common Issues

**Cannot connect to PostgreSQL**
- Ensure Docker is running: `docker --version`
- Check container status: `docker-compose ps`
- Verify port availability: `netstat -an | grep 5432`

**No data in database**
- Check the application logs for export errors
- Verify the schema name matches your queries
- Ensure `AutoCreateDatabaseObjects = true`

**Build errors**
- Restore packages: `dotnet restore`
- Check .NET version: `dotnet --version` (should be 8.0+)

### Getting Help

- Check the main library documentation in the repository README
- Review the integration tests for additional examples
- Open an issue if you encounter bugs or have questions