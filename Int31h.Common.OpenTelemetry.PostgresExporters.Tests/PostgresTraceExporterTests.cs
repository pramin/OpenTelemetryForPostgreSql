using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using OpenTelemetry;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

public class PostgresTraceExporterTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PostgresTraceExporter(null!));
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
        var exporter = new PostgresTraceExporter(options);

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
        var exporter = new PostgresTraceExporter(options);
        var emptyBatch = new Batch<Activity>(Array.Empty<Activity>(), 0);

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
        var exporter = new PostgresTraceExporter(options);

        // Act & Assert
        exporter.Dispose(); // Should not throw
    }
}