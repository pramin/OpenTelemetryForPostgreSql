using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace Int31h.Common.OpenTelemetry.PostgresExporters.Tests;

public class DatabaseSetupTests
{
    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DatabaseSetup(null!, "schema"));
    }

    [Fact]
    public void Constructor_WithNullSchemaName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DatabaseSetup("connectionString", null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var setup = new DatabaseSetup("Host=localhost;Database=test;", "telemetry");

        // Assert
        Assert.NotNull(setup);
    }
}