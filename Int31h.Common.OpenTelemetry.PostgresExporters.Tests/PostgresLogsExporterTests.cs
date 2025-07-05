using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

public class PostgresLogsExporterTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PostgresLogsExporter(null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };

        // Act
        var exporter = new PostgresLogsExporter(options);

        // Assert
        Assert.NotNull(exporter);
    }

    [Fact]
    public void Export_WithEmptyBatch_ReturnsSuccess()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresLogsExporter(options);
        var emptyBatch = new Batch<LogRecord>(Array.Empty<LogRecord>(), 0);

        // Act
        var result = exporter.Export(emptyBatch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };
        var exporter = new PostgresLogsExporter(options);

        // Act & Assert
        exporter.Dispose(); // Should not throw
    }
}