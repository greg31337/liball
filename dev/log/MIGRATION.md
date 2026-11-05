# Migration Notes

The current API uses explicit instances. Create built-in components with `LogFactory`, configure a `Logger` with `LogConfiguration`, and retain that logger for the lifetime of the application or service.

| Existing ecosystem | Familiar concept | `log` equivalent |
| --- | --- | --- |
| Serilog | `ILogger`, `ForContext` | `ILogger`, `Logger.For(...)`, and `Logger.ForField(...)` |
| Winston | transports and formats | `LogSink` and `LogFormatter` values created by `LogFactory` |
| SLF4J/Logback | categories and MDC | named child loggers and `LogScope` |
| Loguru | sinks, bind, contextualize | configured sinks, bound child loggers, and scopes |
| Zap | logger, fields, atomic level | `Logger`, `LogField`, and `LogLevelSwitch` |
| NLog | targets, rules, layouts | sinks, filters/thresholds, and formatters |
| Go `slog` | logger and handler | `Logger` and `ILogSink` |

## Basic migration shape

```csharp
LogFactory factory = new LogFactory();
LogLevelSwitch levelSwitch = new LogLevelSwitch();
levelSwitch.SetLevel(LogLevel.Info);

LogSink console = factory.CreateConsoleSink(null, null, null, false);
LogSink warnings = factory.CreateFileSink(
    "logs/warnings.log",
    factory.CreateCompactJsonFormatter(),
    true,
    null,
    null,
    false);
factory.SetSinkMinimumLevel(warnings, LogLevel.Warn);

Logger logger = new LogConfiguration()
    .Name("Application")
    .LevelSwitch(levelSwitch)
    .AddSink(console)
    .AddSink(warnings)
    .CreateLogger();

logger.InfoWithValues("Processed {Count} orders", new object[] { 3 });
logger.Close();
```

## Main changes from the earlier API

- Replace public static factory calls with methods on a retained `LogFactory` instance.
- Replace `LogConfiguration.Create()` with `new LogConfiguration()`.
- Replace `LogLevelSwitch.Create(level)` with `new LogLevelSwitch()` followed by `SetLevel(level)`.
- Replace the static global logging facade with an explicitly owned `Logger` or the narrow `ILogger` contract.
- Read mutable logger and sink state through their state-interface methods rather than public fields.
- Construct passive data through its sole public constructor, or use `LogFactory.CreateValue`, `CreateField`, and `CreateEvent` for common normalized values.

## Interface boundaries

Custom extensions continue to implement `ILogSink`, `ILogFormatter`, `ILogFilter`, `ILogEnricher`, or `ILogClock`. Concrete built-ins implement additional contracts for state, context, convenience levels, composition, and configuration; custom extensions do not need to implement those larger contracts.

```csharp
ILogSink applicationSink = new ApplicationSink();
ILogFilter applicationFilter = new ApplicationFilter();

Logger logger = new LogConfiguration()
    .AddInterfaceFilter(applicationFilter)
    .AddInterfaceSink(applicationSink)
    .CreateLogger();
```

## Async shutdown

Async and batch wrappers remain explicitly drained. Call `Drain`, `Flush`, or `Close` during shutdown; `Close` drains recursively before releasing destination resources.
