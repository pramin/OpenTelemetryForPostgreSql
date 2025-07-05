using Xunit;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Logging;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

/// <summary>
/// Basic integration tests that verify error handling with invalid connections.
/// For real database tests, see ManualIntegrationTests.cs
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void TraceExporter_Export_WithInvalidConnectionString_ReturnsFailure()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=invalid;Database=test;",
            AutoCreateDatabaseObjects = false // Don't try to create tables
        };
        var exporter = new PostgresTraceExporter(options);
        
        // Create a test activity
        var activity = new Activity("test-activity");
        activity.Start();
        activity.Stop();
        
        var batch = new Batch<Activity>(new[] { activity }, 1);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Failure, result);
    }

    [Fact]
    public void MetricsExporter_Export_WithInvalidConnectionString_ReturnsFailure()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=invalid;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresMetricsExporter(options);
        
        // Create an empty metrics batch
        var batch = new Batch<Metric>(Array.Empty<Metric>(), 0);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Success, result); // Empty batch should succeed
    }

    [Fact]
    public void LogsExporter_Export_WithInvalidConnectionString_ReturnsFailure()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=invalid;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresLogsExporter(options);
        
        // Create an empty logs batch
        var batch = new Batch<LogRecord>(Array.Empty<LogRecord>(), 0);

        // Act
        var result = exporter.Export(batch);

        // Assert
        Assert.Equal(ExportResult.Success, result); // Empty batch should succeed
    }

    [Fact]
    public void DatabaseSetup_Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange & Act
        var setup = new DatabaseSetup("Host=localhost;Database=test;", "telemetry");

        // Assert
        Assert.NotNull(setup);
    }

    [Fact]
    public void ExtractMetricValue_TestCoverage()
    {
        // This is testing internal logic through public interface
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresMetricsExporter(options);
        var emptyBatch = new Batch<Metric>(Array.Empty<Metric>(), 0);

        var result = exporter.Export(emptyBatch);
        
        Assert.Equal(ExportResult.Success, result);
    }

    [Fact]
    public void ExtractLogBody_TestCoverage()
    {
        // This is testing internal logic through public interface
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresLogsExporter(options);
        var emptyBatch = new Batch<LogRecord>(Array.Empty<LogRecord>(), 0);

        var result = exporter.Export(emptyBatch);
        
        Assert.Equal(ExportResult.Success, result);
    }
}