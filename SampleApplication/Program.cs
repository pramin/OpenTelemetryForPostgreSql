using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Int31h.Common.OpenTelemetry.PostgresExporters;

namespace SampleApplication;

/// <summary>
/// Sample application demonstrating OpenTelemetry PostgreSQL exporters for traces, metrics, and logs.
/// </summary>
public class Program
{
    private static readonly ActivitySource ActivitySource = new("SampleApplication");
    private static readonly Meter Meter = new("SampleApplication");
    private static readonly Counter<int> RequestCounter = Meter.CreateCounter<int>("sample_requests_total", description: "Total number of sample requests");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("sample_request_duration_ms", description: "Duration of sample requests in milliseconds");

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 OpenTelemetry PostgreSQL Exporters Sample Application");
        Console.WriteLine("========================================================");
        
        // Parse command line arguments or use defaults
        var connectionString = args.Length > 0 ? args[0] : "Host=localhost;Port=5432;Database=testdb;Username=user;Password=password;";
        var schemaName = args.Length > 1 ? args[1] : "sample_telemetry";

        Console.WriteLine($"📊 Using PostgreSQL connection: {connectionString.Replace("Password=password", "Password=***")}");
        Console.WriteLine($"📋 Using schema: {schemaName}");
        Console.WriteLine();

        // Configure exporters
        var exporterOptions = new PostgresExporterOptions
        {
            ConnectionString = connectionString,
            SchemaName = schemaName,
            AutoCreateDatabaseObjects = true,
            BatchSize = 10,
            ExportTimeoutMs = 30000
        };

        // Create host with OpenTelemetry configured
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Add OpenTelemetry
                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService("SampleApplication", "1.0.0")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = "development",
                            ["service.instance.id"] = Environment.MachineName,
                            ["service.namespace"] = "samples"
                        }))
                    .WithTracing(tracing => tracing
                        .AddSource(ActivitySource.Name)
                        .AddProcessor(new SimpleActivityExportProcessor(
                            PostgresExporterExtensions.CreatePostgresTraceExporter(exporterOptions))))
                    .WithMetrics(metrics => metrics
                        .AddMeter(Meter.Name)
                        .AddReader(new PeriodicExportingMetricReader(
                            PostgresExporterExtensions.CreatePostgresMetricsExporter(exporterOptions),
                            exportIntervalMilliseconds: 5000)));

                // Add logging with OpenTelemetry
                services.AddLogging(logging => logging
                    .AddOpenTelemetry(options => options
                        .AddProcessor(new SimpleLogRecordExportProcessor(
                            PostgresExporterExtensions.CreatePostgresLogsExporter(exporterOptions)))));
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        Console.WriteLine("✅ OpenTelemetry configured with PostgreSQL exporters");
        Console.WriteLine("🔄 Starting telemetry generation...");
        Console.WriteLine();

        try
        {
            // Generate sample telemetry data
            await GenerateSampleTelemetry(logger);

            Console.WriteLine();
            Console.WriteLine("⏳ Waiting for telemetry to be exported...");
            await Task.Delay(6000); // Wait for metrics export interval

            Console.WriteLine("✅ Sample completed! Check your PostgreSQL database for telemetry data.");
            Console.WriteLine();
            PrintDatabaseQueries(schemaName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error running sample application");
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
            ActivitySource.Dispose();
            Meter.Dispose();
        }
    }

    private static async Task GenerateSampleTelemetry(ILogger logger)
    {
        logger.LogInformation("🎯 Sample application starting telemetry generation");

        // Simulate processing multiple orders
        var orderIds = new[] { "ORD-001", "ORD-002", "ORD-003" };
        
        foreach (var orderId in orderIds)
        {
            try
            {
                await ProcessOrder(orderId, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process order {OrderId}, continuing with next order", orderId);
            }
            await Task.Delay(1000); // Small delay between orders
        }

        // Generate some error scenarios
        await SimulateErrorScenarios(logger);
    }

    private static async Task ProcessOrder(string orderId, ILogger logger)
    {
        using var activity = ActivitySource.StartActivity("process_order");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("order.type", "standard");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("📦 Processing order {OrderId}", orderId);

            // Validate order
            await ValidateOrder(orderId, logger);

            // Calculate pricing
            var price = await CalculatePricing(orderId, logger);
            activity?.SetTag("order.price", price);

            // Process payment
            await ProcessPayment(orderId, price, logger);

            // Ship order
            await ShipOrder(orderId, logger);

            stopwatch.Stop();
            
            // Record metrics
            RequestCounter.Add(1, [new("operation", "process_order"), new("status", "success")]);
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, [new("operation", "process_order")]);

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("✅ Successfully processed order {OrderId} in {Duration}ms", orderId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            RequestCounter.Add(1, [new("operation", "process_order"), new("status", "error")]);
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, [new("operation", "process_order")]);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "❌ Failed to process order {OrderId}", orderId);
            throw;
        }
    }

    private static async Task ValidateOrder(string orderId, ILogger logger)
    {
        using var activity = ActivitySource.StartActivity("validate_order");
        activity?.SetTag("order.id", orderId);

        logger.LogDebug("🔍 Validating order {OrderId}", orderId);
        
        // Simulate validation work
        await Task.Delay(Random.Shared.Next(100, 300));
        
        // Simulate occasional validation failures
        if (orderId == "ORD-003")
        {
            throw new InvalidOperationException("Order validation failed: invalid customer");
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        logger.LogDebug("✅ Order {OrderId} validation completed", orderId);
    }

    private static async Task<decimal> CalculatePricing(string orderId, ILogger logger)
    {
        using var activity = ActivitySource.StartActivity("calculate_pricing");
        activity?.SetTag("order.id", orderId);

        logger.LogDebug("💰 Calculating pricing for order {OrderId}", orderId);
        
        // Simulate pricing calculation
        await Task.Delay(Random.Shared.Next(50, 150));
        
        var price = Random.Shared.Next(1000, 5000) / 100m; // $10.00 to $50.00
        activity?.SetTag("calculated.price", price);
        
        logger.LogDebug("💰 Calculated price ${Price:F2} for order {OrderId}", price, orderId);
        return price;
    }

    private static async Task ProcessPayment(string orderId, decimal amount, ILogger logger)
    {
        using var activity = ActivitySource.StartActivity("process_payment");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("payment.amount", amount);
        activity?.SetTag("payment.method", "credit_card");

        logger.LogDebug("💳 Processing payment of ${Amount:F2} for order {OrderId}", amount, orderId);
        
        // Simulate payment processing
        await Task.Delay(Random.Shared.Next(200, 500));
        
        var transactionId = $"TXN-{Guid.NewGuid():N}";
        activity?.SetTag("payment.transaction_id", transactionId);
        
        logger.LogDebug("✅ Payment processed successfully for order {OrderId}, transaction {TransactionId}", orderId, transactionId);
    }

    private static async Task ShipOrder(string orderId, ILogger logger)
    {
        using var activity = ActivitySource.StartActivity("ship_order");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("shipping.carrier", "FedEx");

        logger.LogDebug("📦 Shipping order {OrderId}", orderId);
        
        // Simulate shipping process
        await Task.Delay(Random.Shared.Next(100, 200));
        
        var trackingNumber = $"TRK-{Random.Shared.Next(100000, 999999)}";
        activity?.SetTag("shipping.tracking_number", trackingNumber);
        
        logger.LogDebug("🚚 Order {OrderId} shipped with tracking number {TrackingNumber}", orderId, trackingNumber);
    }

    private static async Task SimulateErrorScenarios(ILogger logger)
    {
        logger.LogInformation("⚠️  Simulating error scenarios for demonstration");

        // Simulate a timeout error
        try
        {
            using var activity = ActivitySource.StartActivity("external_api_call");
            activity?.SetTag("api.endpoint", "https://api.example.com/users");
            
            logger.LogWarning("⏰ Simulating API timeout");
            await Task.Delay(100);
            throw new TimeoutException("External API call timed out");
        }
        catch (Exception ex)
        {
            RequestCounter.Add(1, [new("operation", "external_api"), new("status", "timeout")]);
            logger.LogError(ex, "🔥 External API call failed");
        }

        // Simulate a database error
        try
        {
            using var activity = ActivitySource.StartActivity("database_query");
            activity?.SetTag("db.statement", "SELECT * FROM orders WHERE id = ?");
            
            logger.LogWarning("💥 Simulating database error");
            await Task.Delay(50);
            throw new InvalidOperationException("Database connection failed");
        }
        catch (Exception ex)
        {
            RequestCounter.Add(1, [new("operation", "database_query"), new("status", "error")]);
            logger.LogError(ex, "🗄️  Database operation failed");
        }
    }

    private static void PrintDatabaseQueries(string schemaName)
    {
        Console.WriteLine("📋 Sample PostgreSQL queries to view the exported data:");
        Console.WriteLine();
        
        Console.WriteLine("🔍 View all traces:");
        Console.WriteLine($"SELECT operation_name, status_code, duration_ns/1000000 as duration_ms, attributes");
        Console.WriteLine($"FROM {schemaName}.traces ORDER BY start_time DESC LIMIT 10;");
        Console.WriteLine();
        
        Console.WriteLine("📊 View metrics:");
        Console.WriteLine($"SELECT metric_name, metric_type, metric_value, attributes");
        Console.WriteLine($"FROM {schemaName}.metrics ORDER BY timestamp DESC LIMIT 10;");
        Console.WriteLine();
        
        Console.WriteLine("📝 View logs:");
        Console.WriteLine($"SELECT severity_text, body, trace_id, attributes");
        Console.WriteLine($"FROM {schemaName}.logs ORDER BY timestamp DESC LIMIT 10;");
        Console.WriteLine();
        
        Console.WriteLine("🔗 Trace correlation example:");
        Console.WriteLine($"SELECT t.operation_name, l.severity_text, l.body");
        Console.WriteLine($"FROM {schemaName}.traces t");
        Console.WriteLine($"JOIN {schemaName}.logs l ON t.trace_id = l.trace_id");
        Console.WriteLine($"WHERE t.operation_name = 'process_order' LIMIT 5;");
    }
}
