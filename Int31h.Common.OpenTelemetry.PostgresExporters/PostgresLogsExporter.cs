using OpenTelemetry;
using OpenTelemetry.Logs;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// OpenTelemetry logs exporter for PostgreSQL
/// </summary>
public class PostgresLogsExporter : BaseExporter<LogRecord>
{
    private readonly PostgresExporterOptions _options;
    private readonly DatabaseSetup _databaseSetup;
    private readonly ILogger<PostgresLogsExporter>? _logger;

    public PostgresLogsExporter(PostgresExporterOptions options, ILogger<PostgresLogsExporter>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _databaseSetup = new DatabaseSetup(options.ConnectionString, options.SchemaName, 
            logger as ILogger<DatabaseSetup>);
        _logger = logger;

        if (_options.AutoCreateDatabaseObjects)
        {
            Task.Run(async () => await _databaseSetup.EnsureDatabaseSetupAsync());
        }
    }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        try
        {
            return ExportAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export logs");
            return ExportResult.Failure;
        }
    }

    private async Task<ExportResult> ExportAsync(Batch<LogRecord> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return ExportResult.Success;

        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            const string sql = $@"
                INSERT INTO {{0}}.logs 
                (timestamp, severity_number, severity_text, body, trace_id, span_id, attributes, resource)
                VALUES (@timestamp, @severity_number, @severity_text, @body, @trace_id, @span_id, @attributes, @resource)";

            var formattedSql = string.Format(sql, _options.SchemaName);

            foreach (var logRecord in batch)
            {
                await using var command = new NpgsqlCommand(formattedSql, connection, transaction);

                command.Parameters.AddWithValue("@timestamp", logRecord.Timestamp);
                command.Parameters.AddWithValue("@severity_number", (int)(logRecord.LogLevel));
                command.Parameters.AddWithValue("@severity_text", logRecord.LogLevel.ToString());
                
                // Extract log body
                var body = ExtractLogBody(logRecord);
                command.Parameters.AddWithValue("@body", body ?? string.Empty);

                // Extract trace context if available
                var traceId = logRecord.TraceId != default ? logRecord.TraceId.ToString() : null;
                var spanId = logRecord.SpanId != default ? logRecord.SpanId.ToString() : null;
                
                command.Parameters.AddWithValue("@trace_id", (object?)traceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@span_id", (object?)spanId ?? DBNull.Value);

                // Serialize attributes
                var attributes = new Dictionary<string, object>();
                if (logRecord.Attributes != null)
                {
                    foreach (var kvp in logRecord.Attributes)
                    {
                        attributes[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }
                command.Parameters.AddWithValue("@attributes", JsonSerializer.Serialize(attributes));

                // Serialize resource
                var resource = new Dictionary<string, object>();
                if (logRecord.CategoryName != null)
                    resource["category"] = logRecord.CategoryName;
                if (logRecord.EventId.Id != 0)
                    resource["event.id"] = logRecord.EventId.Id;
                if (!string.IsNullOrEmpty(logRecord.EventId.Name))
                    resource["event.name"] = logRecord.EventId.Name;

                command.Parameters.AddWithValue("@resource", JsonSerializer.Serialize(resource));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger?.LogDebug("Successfully exported {Count} log records", batch.Count);
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export logs to PostgreSQL");
            return ExportResult.Failure;
        }
    }

    private static string? ExtractLogBody(LogRecord logRecord)
    {
        if (logRecord.FormattedMessage != null)
            return logRecord.FormattedMessage;

        // Try to extract body from formatted message or attributes
        if (logRecord.Attributes != null)
        {
            foreach (var kvp in logRecord.Attributes)
            {
                if (kvp.Key == "{OriginalFormat}" && kvp.Value != null)
                    return kvp.Value.ToString();
            }
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup if needed
        }
        base.Dispose(disposing);
    }
}