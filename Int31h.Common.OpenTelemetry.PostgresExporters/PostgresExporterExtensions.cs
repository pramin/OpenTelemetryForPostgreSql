using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Int31h.Common.OpenTelemetry.PostgresExporters;

/// <summary>
/// Extension methods for configuring PostgreSQL exporters with OpenTelemetry builders
/// </summary>
public static class PostgresExporterExtensions
{
    /// <summary>
    /// Creates a PostgreSQL trace exporter with the specified configuration
    /// </summary>
    /// <param name="configure">Action to configure PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresTraceExporter instance</returns>
    public static PostgresTraceExporter CreatePostgresTraceExporter(
        Action<PostgresExporterOptions> configure,
        ILogger<PostgresTraceExporter>? logger = null)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new PostgresExporterOptions();
        configure(options);

        return new PostgresTraceExporter(options, logger);
    }

    /// <summary>
    /// Creates a PostgreSQL metrics exporter with the specified configuration
    /// </summary>
    /// <param name="configure">Action to configure PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresMetricsExporter instance</returns>
    public static PostgresMetricsExporter CreatePostgresMetricsExporter(
        Action<PostgresExporterOptions> configure,
        ILogger<PostgresMetricsExporter>? logger = null)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new PostgresExporterOptions();
        configure(options);

        return new PostgresMetricsExporter(options, logger);
    }

    /// <summary>
    /// Creates a PostgreSQL logs exporter with the specified configuration
    /// </summary>
    /// <param name="configure">Action to configure PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresLogsExporter instance</returns>
    public static PostgresLogsExporter CreatePostgresLogsExporter(
        Action<PostgresExporterOptions> configure,
        ILogger<PostgresLogsExporter>? logger = null)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new PostgresExporterOptions();
        configure(options);

        return new PostgresLogsExporter(options, logger);
    }

    /// <summary>
    /// Creates a PostgreSQL trace exporter with pre-configured options
    /// </summary>
    /// <param name="options">Pre-configured PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresTraceExporter instance</returns>
    public static PostgresTraceExporter CreatePostgresTraceExporter(
        PostgresExporterOptions options,
        ILogger<PostgresTraceExporter>? logger = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new PostgresTraceExporter(options, logger);
    }

    /// <summary>
    /// Creates a PostgreSQL metrics exporter with pre-configured options
    /// </summary>
    /// <param name="options">Pre-configured PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresMetricsExporter instance</returns>
    public static PostgresMetricsExporter CreatePostgresMetricsExporter(
        PostgresExporterOptions options,
        ILogger<PostgresMetricsExporter>? logger = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new PostgresMetricsExporter(options, logger);
    }

    /// <summary>
    /// Creates a PostgreSQL logs exporter with pre-configured options
    /// </summary>
    /// <param name="options">Pre-configured PostgresExporterOptions</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>PostgresLogsExporter instance</returns>
    public static PostgresLogsExporter CreatePostgresLogsExporter(
        PostgresExporterOptions options,
        ILogger<PostgresLogsExporter>? logger = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new PostgresLogsExporter(options, logger);
    }
}