using Xunit;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Trace;
using Dapper;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

/// <summary>
/// Manual integration tests that can be run against a real PostgreSQL instance.
/// Start the PostgreSQL container using: docker-compose up -d
/// Connection: Host=localhost;Port=5432;Database=testdb;Username=user;Password=password;
/// These tests are marked with [Fact(Skip = "Manual integration test")] by default.
/// Remove the Skip parameter to run them against a real database.
/// </summary>
public class ManualIntegrationTests
{
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=user;Password=password;";

    [Fact]
    public async Task TraceExporter_WithRealDatabase_CreatesTablesAndExportsData()
    {
        // Arrange
        var schemaName = $"test_traces_{DateTime.Now:yyyyMMdd_HHmmss}";
        var options = new PostgresExporterOptions
        {
            ConnectionString = TestConnectionString,
            SchemaName = schemaName,
            AutoCreateDatabaseObjects = true
        };
        var exporter = new PostgresTraceExporter(options);
        
        // Create a test activity with proper trace context
        var activity = new Activity("test-operation");
        
        // Set trace and span IDs manually to ensure they exist
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("test.attribute", "test-value");
        activity.SetTag("span.kind", "internal");
        activity.SetStatus(ActivityStatusCode.Ok, "Completed successfully");
        await Task.Delay(10); // Give it some duration
        activity.Stop();
        
        var batch = new Batch<Activity>(new[] { activity }, 1);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        // Verify the data was actually written to the database
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        // Check if schema and table exist
        var schemaExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)",
            new { schema = schemaName });
        Assert.True(schemaExists);

        var tableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "traces" });
        Assert.True(tableExists);

        // Check if trace data was inserted
        var traceCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM {schemaName}.traces WHERE operation_name = @operation",
            new { operation = "test-operation" });
        Assert.Equal(1, traceCount);

        // Verify specific trace data
        var traceRecord = await connection.QueryFirstOrDefaultAsync(
            $"SELECT trace_id, span_id, operation_name, attributes FROM {schemaName}.traces WHERE operation_name = @operation",
            new { operation = "test-operation" });
        
        Assert.NotNull(traceRecord);
        Assert.Equal("test-operation", traceRecord?.operation_name);
        Assert.NotNull(traceRecord?.attributes);
        
        // Cleanup
        activity.Dispose();
    }

    [Fact]
    public async Task MetricsExporter_WithRealDatabase_CreatesTablesAndExportsData()
    {
        // Arrange
        var schemaName = $"test_metrics_{DateTime.Now:yyyyMMdd_HHmmss}";
        var options = new PostgresExporterOptions
        {
            ConnectionString = TestConnectionString,
            SchemaName = schemaName,
            AutoCreateDatabaseObjects = true
        };
        var exporter = new PostgresMetricsExporter(options);
        
        // Force database setup by creating it explicitly
        var setup = new DatabaseSetup(TestConnectionString, schemaName);
        await setup.EnsureDatabaseSetupAsync();
        
        // Create an empty metrics batch
        var batch = new Batch<Metric>(Array.Empty<Metric>(), 0);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        // Verify schema and table creation
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        var schemaExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)",
            new { schema = schemaName });
        Assert.True(schemaExists);

        var tableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "metrics" });
        Assert.True(tableExists);
    }

    [Fact]
    public async Task LogsExporter_WithRealDatabase_CreatesTablesAndExportsData()
    {
        // Arrange
        var schemaName = $"test_logs_{DateTime.Now:yyyyMMdd_HHmmss}";
        var options = new PostgresExporterOptions
        {
            ConnectionString = TestConnectionString,
            SchemaName = schemaName,
            AutoCreateDatabaseObjects = true
        };
        var exporter = new PostgresLogsExporter(options);
        
        // Force database setup by creating it explicitly
        var setup = new DatabaseSetup(TestConnectionString, schemaName);
        await setup.EnsureDatabaseSetupAsync();
        
        // Create an empty logs batch
        var batch = new Batch<LogRecord>(Array.Empty<LogRecord>(), 0);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        // Verify schema and table creation
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        var schemaExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)",
            new { schema = schemaName });
        Assert.True(schemaExists);

        var tableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "logs" });
        Assert.True(tableExists);
    }

    [Fact]
    public async Task DatabaseSetup_EnsureDatabaseSetup_CreatesSchemaAndTables()
    {
        // Arrange
        var schemaName = $"integration_test_{DateTime.Now:yyyyMMdd_HHmmss}";
        var setup = new DatabaseSetup(TestConnectionString, schemaName);

        // Act
        await setup.EnsureDatabaseSetupAsync();

        // Assert
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        // Verify schema exists
        var schemaExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)",
            new { schema = schemaName });
        Assert.True(schemaExists);

        // Verify all three tables exist
        var tracesTableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "traces" });
        Assert.True(tracesTableExists);

        var metricsTableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "metrics" });
        Assert.True(metricsTableExists);

        var logsTableExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            new { schema = schemaName, table = "logs" });
        Assert.True(logsTableExists);
    }

    [Fact]
    public async Task AllExporters_WithSameConnectionString_ShareSameDatabaseObjects()
    {
        // Arrange
        var schemaName = $"shared_schema_{DateTime.Now:yyyyMMdd_HHmmss}";
        var options = new PostgresExporterOptions
        {
            ConnectionString = TestConnectionString,
            SchemaName = schemaName,
            AutoCreateDatabaseObjects = true
        };

        // Force setup first
        var setup = new DatabaseSetup(TestConnectionString, schemaName);
        await setup.EnsureDatabaseSetupAsync();

        var traceExporter = new PostgresTraceExporter(options);
        var metricsExporter = new PostgresMetricsExporter(options);
        var logsExporter = new PostgresLogsExporter(options);

        // Act - Export empty batches to trigger database setup
        var traceBatch = new Batch<Activity>(Array.Empty<Activity>(), 0);
        var metricsBatch = new Batch<Metric>(Array.Empty<Metric>(), 0);
        var logsBatch = new Batch<LogRecord>(Array.Empty<LogRecord>(), 0);

        var traceResult = traceExporter.Export(traceBatch);
        var metricsResult = metricsExporter.Export(metricsBatch);
        var logsResult = logsExporter.Export(logsBatch);

        // Assert
        Assert.Equal(ExportResult.Success, traceResult);
        Assert.Equal(ExportResult.Success, metricsResult);
        Assert.Equal(ExportResult.Success, logsResult);

        // Verify all tables exist in the same schema
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        var tableCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = @schema",
            new { schema = schemaName });
        Assert.Equal(3, tableCount); // traces, metrics, logs
    }
}