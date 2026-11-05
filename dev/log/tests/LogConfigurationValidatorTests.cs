using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogConfigurationValidatorTests
{
    [Test]
    public void Validate_ReturnsNoDiagnosticsForUsableConfiguration()
    {
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("Application.Worker")
            .MinimumLevel(LogLevel.Debug)
            .AddSink(MemoryLogSink.Create())
            .AddFilter(LogFilter.FieldExists("TenantId"))
            .AddGlobalField("TenantId", "tenant-1")
            .AddEnricher(LogEnricher.Field("Service", "billing"));

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();

        Assert.That(diagnostics.Length, Is.EqualTo(0));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
        Assert.That(configuration.IsValid(), Is.True);
    }

    [Test]
    public void Validate_ReturnsWarningWhenNoSinksConfigured()
    {
        LogConfiguration configuration = LogConfiguration.Create();

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();
        LogResult result = configuration.ValidateResult();

        Assert.That(diagnostics.Length, Is.EqualTo(1));
        Assert.That(diagnostics[0].IsWarning, Is.True);
        Assert.That(diagnostics[0].Code, Is.EqualTo("configuration.sinks.empty"));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsDiagnostic, Is.True);
        Assert.That(result.DiagnosticCount, Is.EqualTo(1));
    }

    [Test]
    public void Validate_ReturnsErrorsForInvalidLoggerAndSinks()
    {
        LogSink textWriterSink = TextWriterLogSink.Create(null);
        LogSink fileSink = FileLogSink.Append("");
        LogSink delegateSink = DelegateLogSink.Create(null);
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("bad logger")
            .SetSinks(new LogSink[] { textWriterSink, fileSink, delegateSink, null! });

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();

        Assert.That(LogConfigurationDiagnostic.CountErrors(diagnostics), Is.EqualTo(5));
        Assert.That(ContainsCode(diagnostics, "configuration.logger.name.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.textwriter.missing"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.file.path.missing"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.delegate.missing"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.sink.null"), Is.True);
        Assert.That(configuration.IsValid(), Is.False);
    }

    [Test]
    public void Validate_InspectsNestedWrapperSinks()
    {
        LogSink asyncSink = AsyncLogSink.Create(MemoryLogSink.Create());
        asyncSink.AsyncInnerSink = null;
        asyncSink.AsyncQueueCapacity = 0;
        LogSink batchSink = BatchLogSink.Create(MemoryLogSink.Create());
        batchSink.BatchInnerSink = null;
        batchSink.BatchSize = 0;
        LogSink circuitSink = CircuitBreakerLogSink.Create(MemoryLogSink.Create());
        circuitSink.CircuitBreakerInnerSink = null;
        circuitSink.CircuitBreakerFailureThreshold = 0;
        LogConfiguration configuration = LogConfiguration.Create()
            .SetSinks(new LogSink[] { asyncSink, batchSink, circuitSink });

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();

        Assert.That(ContainsCode(diagnostics, "configuration.sink.async.capacity.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.async.inner.missing"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.sink.batch.size.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.batch.inner.missing"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.sink.circuit.threshold.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "sink.circuit.inner.missing"), Is.True);
    }

    [Test]
    public void Validate_ReturnsErrorsForInvalidFiltersFieldsAndEnrichers()
    {
        LogFilter unknownFilter = new LogFilter();
        unknownFilter.Kind = "unknown";
        LogFilter badFieldFilter = LogFilter.FieldExists("");
        LogEnricher unknownEnricher = new LogEnricher();
        unknownEnricher.Kind = "unknown";
        LogConfiguration configuration = LogConfiguration.Create()
            .AddSink(MemoryLogSink.Create())
            .SetFilters(new LogFilter[] { badFieldFilter, unknownFilter })
            .SetEnrichers(new LogEnricher[] { LogEnricher.FieldValueEnricher(LogField.String("", "")), unknownEnricher, null! })
            .SetGlobalFields(new LogField[] { LogField.String("", "") });

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();

        Assert.That(ContainsCode(diagnostics, "configuration.filter.field.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.filter.kind.unknown"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.enricher.field.invalid"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.enricher.kind.unknown"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.enricher.null"), Is.True);
        Assert.That(ContainsCode(diagnostics, "configuration.globalfield.invalid"), Is.True);
        Assert.That(LogConfigurationDiagnostic.CountWarnings(diagnostics), Is.EqualTo(2));
    }

    [Test]
    public void ValidateResult_ReturnsFailureForErrors()
    {
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("bad logger")
            .AddSink(FileLogSink.Append(""));

        LogResult result = configuration.ValidateResult();

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("configuration.logger.name.invalid"));
        Assert.That(result.FailureCount, Is.EqualTo(2));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(2));
    }

    [Test]
    public void Validate_ReturnsDefensiveDiagnosticCopy()
    {
        LogConfiguration configuration = LogConfiguration.Create();
        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();

        diagnostics[0] = LogConfigurationDiagnostic.Error("changed", "changed", "changed");

        Assert.That(configuration.LastDiagnostics[0].Code, Is.EqualTo("configuration.sinks.empty"));
    }

    [Test]
    public void CreateLogger_StoresValidationDiagnostics()
    {
        LogConfiguration configuration = LogConfiguration.Create();

        Logger logger = configuration.CreateLogger();

        Assert.That(logger, Is.Not.Null);
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(1));
        Assert.That(configuration.LastDiagnostics[0].Code, Is.EqualTo("configuration.sinks.empty"));
    }

    static bool ContainsCode(LogConfigurationDiagnostic[] diagnostics, string code)
    {
        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
