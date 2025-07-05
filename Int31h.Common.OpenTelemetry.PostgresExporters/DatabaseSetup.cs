using Npgsql;
using Microsoft.Extensions.Logging;

namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// Handles database setup for PostgreSQL telemetry tables and procedures
/// </summary>
public class DatabaseSetup
{
    private readonly string _connectionString;
    private readonly string _schemaName;
    private readonly ILogger<DatabaseSetup>? _logger;

    public DatabaseSetup(string connectionString, string schemaName, ILogger<DatabaseSetup>? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        _logger = logger;
    }

    /// <summary>
    /// Ensures the database schema and tables exist
    /// </summary>
    public async Task EnsureDatabaseSetupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await CreateSchemaAsync(connection, cancellationToken);
            await CreateTracesTableAsync(connection, cancellationToken);
            await CreateMetricsTableAsync(connection, cancellationToken);
            await CreateLogsTableAsync(connection, cancellationToken);

            _logger?.LogInformation("Database setup completed successfully for schema '{SchemaName}'", _schemaName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup database for schema '{SchemaName}'", _schemaName);
            throw;
        }
    }

    private async Task CreateSchemaAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $"CREATE SCHEMA IF NOT EXISTS {_schemaName}";
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger?.LogDebug("Schema '{SchemaName}' created or already exists", _schemaName);
    }

    private async Task CreateTracesTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $@"
            CREATE TABLE IF NOT EXISTS {_schemaName}.traces (
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
            )";

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger?.LogDebug("Traces table created or already exists in schema '{SchemaName}'", _schemaName);
    }

    private async Task CreateMetricsTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $@"
            CREATE TABLE IF NOT EXISTS {_schemaName}.metrics (
                id BIGSERIAL PRIMARY KEY,
                metric_name TEXT NOT NULL,
                metric_type TEXT NOT NULL,
                metric_value DOUBLE PRECISION,
                metric_unit TEXT,
                timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
                attributes JSONB,
                resource JSONB,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            )";

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger?.LogDebug("Metrics table created or already exists in schema '{SchemaName}'", _schemaName);
    }

    private async Task CreateLogsTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $@"
            CREATE TABLE IF NOT EXISTS {_schemaName}.logs (
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
            )";

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger?.LogDebug("Logs table created or already exists in schema '{SchemaName}'", _schemaName);
    }
}