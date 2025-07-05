namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// Configuration options for PostgreSQL exporters
/// </summary>
public class PostgresExporterOptions
{
    /// <summary>
    /// PostgreSQL connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Schema name for telemetry tables (default: 'telemetry')
    /// </summary>
    public string SchemaName { get; set; } = "telemetry";

    /// <summary>
    /// Whether to automatically create required database objects
    /// </summary>
    public bool AutoCreateDatabaseObjects { get; set; } = true;

    /// <summary>
    /// Batch size for exporting data
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Export timeout in milliseconds
    /// </summary>
    public int ExportTimeoutMs { get; set; } = 30000;
}
