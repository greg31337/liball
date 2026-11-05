# log

`log` is a small structured logging library for .NET. Its public API is organized under `src/Api`: data stays field-based, extension seams are interfaces, and built-in behavior is exposed through interface-implementing classes with instance methods.

See [MIGRATION.md](MIGRATION.md) for mappings from common logging libraries.

## Create a logger

Create built-in components through `LogFactory`, then build an explicitly owned `Logger` from `LogConfiguration`.

```csharp
using log;

LogFactory factory = new LogFactory();
LogSink console = factory.CreateConsoleSink(null, null, null, false);

Logger logger = new LogConfiguration()
    .Name("Application.Worker")
    .MinimumLevel(LogLevel.Info)
    .AddSink(console)
    .CreateLogger();

logger.Info("Service started");
logger.Warn("Queue depth is high");
logger.Close();
```

The explicit logger replaces the former public static facade. Internal compatibility helpers remain implementation details, so consumer code has clear ownership and lifecycle.

## Interfaces and test seams

Application code can depend on the narrow `ILogger` contract. `Logger` also implements separate contracts for convenience level methods, context, pipeline control, and observable state. Custom sinks, formatters, filters, enrichers, and clocks implement the corresponding narrow interfaces.

```csharp
public class OrderProcessor
{
    public ILogger Logger;

    public OrderProcessor(ILogger logger)
    {
        Logger = logger;
    }

    public LogResult Process(int count)
    {
        return Logger.WriteWithValues(
            LogLevel.Info,
            "Processed {Count} orders",
            new object[] { count });
    }
}
```

## Structured fields and scopes

`LogFactory` normalizes application values when an explicit `LogField` is needed.

```csharp
LogFactory factory = new LogFactory();
LogField tenant = factory.CreateField("TenantId", "tenant-1");

Logger billing = logger.ForLogField(tenant);
LogScope scope = billing.BeginScopeField("RequestId", "req-123");

billing.InfoWithValues("Processed {Count} invoices", new object[] { 42 });
scope.Dispose();
```

## JSON and text output

```csharp
LogFactory factory = new LogFactory();
LogFormatter json = factory.CreateCompactJsonFormatter();
LogSink sink = factory.CreateTextWriterSink(Console.Out, json, true);

Logger logger = new LogConfiguration()
    .AddSink(sink)
    .CreateLogger();
```

The factory also creates full JSON, plain text, console text, key-value, exception, and output-template formatters.

## File rolling and retention

```csharp
LogFactory factory = new LogFactory();
LogRollingPolicy rolling = factory.CreateRollingPolicy(
    LogRollingInterval.Day,
    10_000_000);
LogRetentionPolicy retention = factory.CreateRetentionPolicy(
    7,
    TimeSpan.Zero);

LogSink file = factory.CreateFileSink(
    "logs/app.log",
    factory.CreateCompactJsonFormatter(),
    true,
    rolling,
    retention,
    true);
```

## Queuing and batching

Async and batch wrappers are explicitly drained. They do not create a background worker.

```csharp
LogFactory factory = new LogFactory();
LogSink file = factory.CreateFileSink("logs/app.log", null, true, null, null, false);
LogSink queued = factory.CreateAsyncSink(
    "async",
    file,
    1024,
    LogBackpressureMode.DropOldest);

Logger logger = new LogConfiguration()
    .AddSink(queued)
    .CreateLogger();

logger.Info("Queued until drained");
logger.Drain();
logger.Close();
```

## Runtime levels

```csharp
LogLevelSwitch levelSwitch = new LogLevelSwitch();
levelSwitch.SetLevel(LogLevel.Warn);

Logger logger = new LogConfiguration()
    .LevelSwitch(levelSwitch)
    .AddSink(new LogFactory().CreateConsoleSink(null, null, null, false))
    .CreateLogger();

logger.Info("Suppressed while the level is Warn");
levelSwitch.SetLevel(LogLevel.Debug);
logger.Info("Written after the runtime level change");
```

## JSON configuration and reload

```csharp
LogFactory factory = new LogFactory();
Logger logger = factory.LoadJson("""
{
  "name": "Application.Api",
  "minimumLevel": "Info",
  "sinks": [ { "kind": "console" } ]
}
""").CreateLogger();

LogConfigurationReload reload = factory.WatchJsonFileWithEnvironment("logsettings.json");
reload.OnLoggerReloaded(nextLogger => logger = nextLogger);
reload.Start();
```

Use `LogDiagnostics` for self-log control and `LogRedaction` for non-mutating field or event redaction.
