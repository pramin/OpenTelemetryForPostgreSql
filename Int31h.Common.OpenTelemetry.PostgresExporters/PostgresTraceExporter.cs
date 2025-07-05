using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// OpenTelemetry trace exporter for PostgreSQL
/// </summary>
public class PostgresTraceExporter : BaseExporter<Activity>
{
    private readonly PostgresExporterOptions _options;
    private readonly DatabaseSetup _databaseSetup;
    private readonly ILogger<PostgresTraceExporter>? _logger;

    public PostgresTraceExporter(PostgresExporterOptions options, ILogger<PostgresTraceExporter>? logger = null)
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

    public override ExportResult Export(in Batch<Activity> batch)
    {
        try
        {
            return ExportAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export traces");
            return ExportResult.Failure;
        }
    }

    private async Task<ExportResult> ExportAsync(Batch<Activity> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return ExportResult.Success;

        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            const string sql = $@"
                INSERT INTO {{0}}.traces 
                (trace_id, span_id, parent_span_id, operation_name, start_time, end_time, 
                 duration_ns, status_code, status_message, attributes, events, resource)
                VALUES (@trace_id, @span_id, @parent_span_id, @operation_name, @start_time, @end_time,
                        @duration_ns, @status_code, @status_message, @attributes, @events, @resource)";

            var formattedSql = string.Format(sql, _options.SchemaName);

            foreach (var activity in batch)
            {
                await using var command = new NpgsqlCommand(formattedSql, connection, transaction);

                var traceId = activity.TraceId.ToString();
                var spanId = activity.SpanId.ToString();
                var parentSpanId = activity.ParentSpanId != default ? activity.ParentSpanId.ToString() : null;

                command.Parameters.AddWithValue("@trace_id", traceId);
                command.Parameters.AddWithValue("@span_id", spanId);
                command.Parameters.AddWithValue("@parent_span_id", (object?)parentSpanId ?? DBNull.Value);
                command.Parameters.AddWithValue("@operation_name", activity.OperationName ?? string.Empty);
                command.Parameters.AddWithValue("@start_time", activity.StartTimeUtc);
                command.Parameters.AddWithValue("@end_time", activity.StartTimeUtc.Add(activity.Duration));
                command.Parameters.AddWithValue("@duration_ns", activity.Duration.Ticks * 100); // Convert to nanoseconds
                command.Parameters.AddWithValue("@status_code", (int)activity.Status);
                command.Parameters.AddWithValue("@status_message", (object?)activity.StatusDescription ?? DBNull.Value);

                // Serialize attributes
                var attributes = new Dictionary<string, object>();
                foreach (var tag in activity.TagObjects)
                {
                    attributes[tag.Key] = tag.Value ?? string.Empty;
                }
                command.Parameters.AddWithValue("@attributes", JsonSerializer.Serialize(attributes));

                // Serialize events
                var events = activity.Events.Select(e => new
                {
                    Name = e.Name,
                    Timestamp = e.Timestamp.UtcDateTime,
                    Attributes = e.Tags.ToDictionary(t => t.Key, t => t.Value ?? string.Empty)
                }).ToArray();
                command.Parameters.AddWithValue("@events", JsonSerializer.Serialize(events));

                // Serialize resource
                var resource = new Dictionary<string, object>();
                if (activity.Source != null)
                {
                    resource["service.name"] = activity.Source.Name;
                    if (!string.IsNullOrEmpty(activity.Source.Version))
                        resource["service.version"] = activity.Source.Version;
                }
                command.Parameters.AddWithValue("@resource", JsonSerializer.Serialize(resource));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger?.LogDebug("Successfully exported {Count} traces", batch.Count);
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export traces to PostgreSQL");
            return ExportResult.Failure;
        }
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