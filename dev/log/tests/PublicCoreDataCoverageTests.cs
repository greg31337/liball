using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicCoreDataCoverageTests
{
    [Test]
    public void PublicEnums_ExposeEveryDefinedValueInStableOrder()
    {
        Assert.That(Enum.GetValues<LogBackpressureMode>(), Is.EqualTo(new LogBackpressureMode[]
        {
            LogBackpressureMode.Block,
            LogBackpressureMode.DropNewest,
            LogBackpressureMode.DropOldest
        }));
        Assert.That(Enum.GetValues<LogDecision>(), Is.EqualTo(new LogDecision[]
        {
            LogDecision.Neutral,
            LogDecision.Include,
            LogDecision.Exclude
        }));
        Assert.That(Enum.GetValues<LogFieldOrder>(), Is.EqualTo(new LogFieldOrder[]
        {
            LogFieldOrder.Captured,
            LogFieldOrder.Name
        }));
        Assert.That(Enum.GetValues<LogLevel>(), Is.EqualTo(new LogLevel[]
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal,
            LogLevel.Off
        }));
        Assert.That(Enum.GetValues<LogRollingInterval>(), Is.EqualTo(new LogRollingInterval[]
        {
            LogRollingInterval.None,
            LogRollingInterval.Hour,
            LogRollingInterval.Day,
            LogRollingInterval.Month,
            LogRollingInterval.Year
        }));
        Assert.That(Enum.GetValues<LogTimestampMode>(), Is.EqualTo(new LogTimestampMode[]
        {
            LogTimestampMode.Utc,
            LogTimestampMode.Local
        }));
        Assert.That(Enum.GetValues<LogValueKind>(), Is.EqualTo(new LogValueKind[]
        {
            LogValueKind.Null,
            LogValueKind.String,
            LogValueKind.Bool,
            LogValueKind.Integer,
            LogValueKind.Decimal,
            LogValueKind.FloatingPoint,
            LogValueKind.DateTime,
            LogValueKind.Duration,
            LogValueKind.Exception,
            LogValueKind.ObjectText,
            LogValueKind.Array,
            LogValueKind.Dictionary,
            LogValueKind.Raw
        }));
    }

    [Test]
    public void LogValueConstructor_PopulatesEveryFieldAndCopiesCollections()
    {
        ILogValueFactory factory = new LogFactory();
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 26, 1, 2, 3, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromSeconds(4);
        InvalidOperationException exceptionValue = new InvalidOperationException("failure");
        LogValue[] array = new LogValue[] { factory.CreateIntValue(5) };
        Dictionary<string, LogValue> dictionary = new Dictionary<string, LogValue>();
        dictionary["item"] = factory.CreateStringValue("value");

        LogValue value = new LogValue(LogValueKind.Raw, "text", true, long.MaxValue, decimal.MaxValue, double.MaxValue, timestamp, duration, exceptionValue, array, dictionary, "{\"ok\":true}");
        array[0] = factory.CreateIntValue(6);
        dictionary.Clear();

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Raw));
        Assert.That(value.TextValue, Is.EqualTo("text"));
        Assert.That(value.BoolValue, Is.True);
        Assert.That(value.IntegerValue, Is.EqualTo(long.MaxValue));
        Assert.That(value.DecimalValue, Is.EqualTo(decimal.MaxValue));
        Assert.That(value.FloatingPointValue, Is.EqualTo(double.MaxValue));
        Assert.That(value.DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(value.DurationValue, Is.EqualTo(duration));
        Assert.That(value.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(value.ArrayValue, Is.Not.SameAs(array));
        Assert.That(value.ArrayValue[0].IntegerValue, Is.EqualTo(5));
        Assert.That(value.DictionaryValue, Is.Not.SameAs(dictionary));
        Assert.That(value.DictionaryValue["item"].TextValue, Is.EqualTo("value"));
        Assert.That(value.RawValue, Is.EqualTo("{\"ok\":true}"));
        Assert.That(value.IsTruncated, Is.False);
        Assert.That(value.OriginalLength, Is.EqualTo(11));
        Assert.That(value.CapturedLength, Is.EqualTo(11));
        Assert.That(value.MaximumLength, Is.EqualTo(4096));
        Assert.That(value.CapturedDepth, Is.Zero);

        LogValue normalizedNulls = new LogValue(LogValueKind.Null, null!, false, 0, 0m, 0d, DateTimeOffset.MinValue, TimeSpan.Zero, null, null!, null!, null!);
        Assert.That(normalizedNulls.TextValue, Is.Empty);
        Assert.That(normalizedNulls.ArrayValue, Is.Empty);
        Assert.That(normalizedNulls.DictionaryValue, Is.Empty);
        Assert.That(normalizedNulls.RawValue, Is.Empty);
    }

    [Test]
    public void LogFieldAndLogEventConstructors_ExposeValidationAndEveryEventField()
    {
        ILogValueFactory factory = new LogFactory();
        LogValue fieldValue = factory.CreateStringValue("tenant-1");
        LogField field = new LogField("TenantId", fieldValue);
        LogField nullField = new LogField("Optional", null);
        LogField invalidField = new LogField("bad name", fieldValue);
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 26, 2, 3, 4, TimeSpan.Zero);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");

        LogEvent logEvent = new LogEvent(
            timestamp,
            LogLevel.Error,
            "Application.Worker",
            42,
            "WorkFailed",
            "Failed {TenantId}",
            "Failed tenant-1",
            new LogField[] { field },
            exceptionValue,
            "trace",
            "span",
            "parent",
            7,
            11,
            "machine",
            "application",
            "Worker.cs",
            "Run",
            123);

        Assert.That(field.Name, Is.EqualTo("TenantId"));
        Assert.That(field.Value, Is.SameAs(fieldValue));
        Assert.That(field.IsValid, Is.True);
        Assert.That(field.ErrorMessage, Is.Empty);
        Assert.That(nullField.Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(nullField.IsValid, Is.True);
        Assert.That(invalidField.Name, Is.Empty);
        Assert.That(invalidField.Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(invalidField.IsValid, Is.False);
        Assert.That(invalidField.ErrorMessage, Is.Not.Empty);
        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.EventId, Is.EqualTo(42));
        Assert.That(logEvent.EventName, Is.EqualTo("WorkFailed"));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Failed {TenantId}"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Failed tenant-1"));
        Assert.That(logEvent.Fields, Has.Length.EqualTo(1));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logEvent.InvalidFieldCount, Is.Zero);
        Assert.That(logEvent.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(logEvent.TraceId, Is.EqualTo("trace"));
        Assert.That(logEvent.SpanId, Is.EqualTo("span"));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo("parent"));
        Assert.That(logEvent.ThreadId, Is.EqualTo(7));
        Assert.That(logEvent.ProcessId, Is.EqualTo(11));
        Assert.That(logEvent.MachineName, Is.EqualTo("machine"));
        Assert.That(logEvent.ApplicationName, Is.EqualTo("application"));
        Assert.That(logEvent.SourceFile, Is.EqualTo("Worker.cs"));
        Assert.That(logEvent.SourceMemberName, Is.EqualTo("Run"));
        Assert.That(logEvent.SourceLineNumber, Is.EqualTo(123));
        Assert.That(logEvent.IsValid, Is.True);
        Assert.That(logEvent.ErrorMessage, Is.Empty);

        LogEvent invalidEvent = new LogEvent(timestamp, LogLevel.Info, "bad name", 0, "", "", "", new LogField[] { invalidField }, null, "", "", "", -1, -2, "", "", "", "", -3);
        Assert.That(invalidEvent.IsValid, Is.False);
        Assert.That(invalidEvent.ErrorMessage, Is.Not.Empty);
        Assert.That(invalidEvent.Fields, Is.Empty);
        Assert.That(invalidEvent.InvalidFieldCount, Is.EqualTo(1));
        Assert.That(invalidEvent.ThreadId, Is.Zero);
        Assert.That(invalidEvent.ProcessId, Is.Zero);
        Assert.That(invalidEvent.SourceLineNumber, Is.Zero);
    }

    [Test]
    public void LogResultConstructor_RepresentsEveryResultStateAndNormalizesCounts()
    {
        LogResult success = new LogResult(true, false, false, false, null, null, -1, -2, -3);
        LogResult failure = new LogResult(false, true, false, false, "failed", "failure", 0, int.MaxValue, 0);
        LogResult dropped = new LogResult(false, false, true, false, "dropped", "drop", int.MaxValue, 0, 0);
        LogResult diagnostic = new LogResult(true, false, false, true, "diagnostic", "detail", 0, 0, int.MaxValue);

        Assert.That(success.IsSuccess, Is.True);
        Assert.That(success.IsFailure, Is.False);
        Assert.That(success.IsDropped, Is.False);
        Assert.That(success.IsDiagnostic, Is.False);
        Assert.That(success.Code, Is.Empty);
        Assert.That(success.Message, Is.Empty);
        Assert.That(success.DroppedCount, Is.Zero);
        Assert.That(success.FailureCount, Is.Zero);
        Assert.That(success.DiagnosticCount, Is.Zero);
        Assert.That(failure.IsFailure, Is.True);
        Assert.That(failure.Code, Is.EqualTo("failed"));
        Assert.That(failure.Message, Is.EqualTo("failure"));
        Assert.That(failure.FailureCount, Is.EqualTo(int.MaxValue));
        Assert.That(dropped.IsDropped, Is.True);
        Assert.That(dropped.DroppedCount, Is.EqualTo(int.MaxValue));
        Assert.That(diagnostic.IsSuccess, Is.True);
        Assert.That(diagnostic.IsDiagnostic, Is.True);
        Assert.That(diagnostic.DiagnosticCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void PublicOperations_PopulateConfigurationDiagnosticAndEventSourceEntryFields()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        LogConfigurationDiagnostic warning = new LogConfiguration().Validate()[0];
        LogConfiguration invalidConfiguration = new LogConfiguration();
        invalidConfiguration.Name("bad name");
        invalidConfiguration.AddSink(componentFactory.CreateMemorySink("memory", 0));
        LogConfigurationDiagnostic error = invalidConfiguration.Validate()[0];

        Assert.That(warning.Severity, Is.EqualTo("warning"));
        Assert.That(warning.Code, Is.EqualTo("configuration.sinks.empty"));
        Assert.That(warning.Message, Is.EqualTo("no sinks are configured"));
        Assert.That(warning.Path, Is.EqualTo("sinks"));
        Assert.That(warning.IsError, Is.False);
        Assert.That(warning.IsWarning, Is.True);
        Assert.That(error.Severity, Is.EqualTo("error"));
        Assert.That(error.Code, Is.EqualTo("configuration.logger.name.invalid"));
        Assert.That(error.Message, Is.Not.Empty);
        Assert.That(error.Path, Is.EqualTo("loggerName"));
        Assert.That(error.IsError, Is.True);
        Assert.That(error.IsWarning, Is.False);

        LogEventSourceEntry? captured = null;
        LogSink eventSourceSink = componentFactory.CreateEventSourceSink("Public.Api.Coverage", componentFactory.CreateTextFormatter(), entry => captured = entry);
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 26, 3, 4, 5, TimeSpan.Zero);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = new LogEvent(timestamp, LogLevel.Warn, "Application.Worker", 17, "Processed", "Processed", "Processed item", new LogField[0], exceptionValue, "", "", "", 1, 2, "", "", "", "", 0);

        LogResult result = eventSourceSink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Timestamp, Is.EqualTo("2026-08-26T03:04:05.0000000Z"));
        Assert.That(captured.Level, Is.EqualTo("Warn"));
        Assert.That(captured.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(captured.EventId, Is.EqualTo(17));
        Assert.That(captured.EventName, Is.EqualTo("Processed"));
        Assert.That(captured.Message, Is.EqualTo("Processed item"));
        Assert.That(captured.Payload, Does.Contain("Processed item"));
        Assert.That(captured.Exception, Does.Contain("InvalidOperationException"));
    }
}
