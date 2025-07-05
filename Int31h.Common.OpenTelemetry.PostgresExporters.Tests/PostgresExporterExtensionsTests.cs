using Xunit;
using Microsoft.Extensions.Logging;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

public class PostgresExporterExtensionsTests
{
    [Fact]
    public void CreatePostgresTraceExporter_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PostgresExporterExtensions.CreatePostgresTraceExporter((Action<PostgresExporterOptions>)null!));
    }

    [Fact]
    public void CreatePostgresTraceExporter_WithValidConfigure_CreatesInstance()
    {
        // Arrange & Act
        var exporter = PostgresExporterExtensions.CreatePostgresTraceExporter((Action<PostgresExporterOptions>)(options =>
        {
            options.ConnectionString = "Host=localhost;Database=test;";
            options.AutoCreateDatabaseObjects = false;
        }));

        // Assert
        Assert.NotNull(exporter);
    }

    [Fact]
    public void CreatePostgresTraceExporter_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PostgresExporterExtensions.CreatePostgresTraceExporter((PostgresExporterOptions)null!));
    }

    [Fact]
    public void CreatePostgresTraceExporter_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new PostgresExporterOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            AutoCreateDatabaseObjects = false
        };

        // Act
        var exporter = PostgresExporterExtensions.CreatePostgresTraceExporter(options);

        // Assert
        Assert.NotNull(exporter);
    }

    [Fact]
    public void CreatePostgresMetricsExporter_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PostgresExporterExtensions.CreatePostgresMetricsExporter((Action<PostgresExporterOptions>)null!));
    }

    [Fact]
    public void CreatePostgresMetricsExporter_WithValidConfigure_CreatesInstance()
    {
        // Arrange & Act
        var exporter = PostgresExporterExtensions.CreatePostgresMetricsExporter((Action<PostgresExporterOptions>)(options =>
        {
            options.ConnectionString = "Host=localhost;Database=test;";
            options.AutoCreateDatabaseObjects = false;
        }));

        // Assert
        Assert.NotNull(exporter);
    }

    [Fact]
    public void CreatePostgresLogsExporter_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PostgresExporterExtensions.CreatePostgresLogsExporter((Action<PostgresExporterOptions>)null!));
    }

    [Fact]
    public void CreatePostgresLogsExporter_WithValidConfigure_CreatesInstance()
    {
        // Arrange & Act
        var exporter = PostgresExporterExtensions.CreatePostgresLogsExporter((Action<PostgresExporterOptions>)(options =>
        {
            options.ConnectionString = "Host=localhost;Database=test;";
            options.AutoCreateDatabaseObjects = false;
        }));

        // Assert
        Assert.NotNull(exporter);
    }
}