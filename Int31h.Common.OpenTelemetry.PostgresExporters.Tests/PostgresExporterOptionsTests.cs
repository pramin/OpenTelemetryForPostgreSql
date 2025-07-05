using Xunit;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

public class PostgresExporterOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new PostgresExporterOptions();

        // Assert
        Assert.Equal("telemetry", options.SchemaName);
        Assert.True(options.AutoCreateDatabaseObjects);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(30000, options.ExportTimeoutMs);
        Assert.Equal(string.Empty, options.ConnectionString);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new PostgresExporterOptions();

        // Act
        options.ConnectionString = "test-connection";
        options.SchemaName = "custom_schema";
        options.AutoCreateDatabaseObjects = false;
        options.BatchSize = 50;
        options.ExportTimeoutMs = 15000;

        // Assert
        Assert.Equal("test-connection", options.ConnectionString);
        Assert.Equal("custom_schema", options.SchemaName);
        Assert.False(options.AutoCreateDatabaseObjects);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(15000, options.ExportTimeoutMs);
    }
}