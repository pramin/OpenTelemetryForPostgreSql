using OpenTelemetry;
using OpenTelemetry.Metrics;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// OpenTelemetry metrics exporter for PostgreSQL
/// </summary>
public class PostgresMetricsExporter : BaseExporter<Metric>
{
    private readonly PostgresExporterOptions _options;
    private readonly DatabaseSetup _databaseSetup;
    private readonly ILogger<PostgresMetricsExporter>? _logger;

    public PostgresMetricsExporter(PostgresExporterOptions options, ILogger<PostgresMetricsExporter>? logger = null)
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

    public override ExportResult Export(in Batch<Metric> batch)
    {
        if (batch.Count == 0)
            return ExportResult.Success;

        try
        {
            using var connection = new NpgsqlConnection(_options.ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            const string sql = $@"
                INSERT INTO {{0}}.metrics 
                (metric_name, metric_type, metric_value, metric_unit, timestamp, attributes, resource)
                VALUES (@metric_name, @metric_type, @metric_value, @metric_unit, @timestamp, @attributes, @resource)";

            var formattedSql = string.Format(sql, _options.SchemaName);

            foreach (var metric in batch)
            {
                foreach (ref readonly var metricPoint in metric.GetMetricPoints())
                {
                    using var command = new NpgsqlCommand(formattedSql, connection, transaction);

                    command.Parameters.AddWithValue("@metric_name", metric.Name);
                    command.Parameters.AddWithValue("@metric_type", metric.MetricType.ToString());
                    
                    // Extract metric value based on type
                    double? metricValue = ExtractMetricValue(metricPoint, metric.MetricType);
                    command.Parameters.AddWithValue("@metric_value", (object?)metricValue ?? DBNull.Value);
                    
                    command.Parameters.AddWithValue("@metric_unit", (object?)metric.Unit ?? DBNull.Value);
                    command.Parameters.AddWithValue("@timestamp", metricPoint.EndTime.UtcDateTime);

                    // Serialize attributes
                    var attributes = new Dictionary<string, object>();
                    foreach (var tag in metricPoint.Tags)
                    {
                        attributes[tag.Key] = tag.Value ?? string.Empty;
                    }
                    command.Parameters.AddWithValue("@attributes", JsonSerializer.Serialize(attributes));

                    // Serialize resource (simplified)
                    var resource = new Dictionary<string, object>
                    {
                        ["metric.name"] = metric.Name,
                        ["metric.description"] = metric.Description ?? string.Empty
                    };
                    command.Parameters.AddWithValue("@resource", JsonSerializer.Serialize(resource));

                    command.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            _logger?.LogDebug("Successfully exported {Count} metrics", batch.Count);
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export metrics to PostgreSQL");
            return ExportResult.Failure;
        }
    }

    private static double? ExtractMetricValue(MetricPoint metricPoint, MetricType metricType)
    {
        return metricType switch
        {
            MetricType.LongSum => metricPoint.GetSumLong(),
            MetricType.DoubleSum => metricPoint.GetSumDouble(),
            MetricType.LongGauge => metricPoint.GetGaugeLastValueLong(),
            MetricType.DoubleGauge => metricPoint.GetGaugeLastValueDouble(),
            MetricType.Histogram => metricPoint.GetHistogramSum(),
            _ => null
        };
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