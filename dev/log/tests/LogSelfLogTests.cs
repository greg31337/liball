using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogSelfLogTests
{
    [SetUp]
    public void SetUp()
    {
        LogSelfLog.Disable();
        LogSelfLog.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LogSelfLog.Disable();
        LogSelfLog.Clear();
    }

    [Test]
    public void EnableMemory_CapturesSinkFailureDiagnostics()
    {
        LogSink sink = LogSink.Named("failure");

        LogSelfLog.EnableMemory();
        sink.SetResult(LogResult.FailureCode("sink.failure", "failed"));
        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        string[] entries = LogSelfLog.Entries();

        Assert.That(entries.Length, Is.EqualTo(1));
        Assert.That(entries[0], Does.Contain("sink failure"));
        Assert.That(entries[0], Does.Contain("sink.failure"));
        Assert.That(entries[0], Does.Contain("failed"));
        Assert.That(LogSelfLog.WrittenCount(), Is.EqualTo(1));
    }

    [Test]
    public void WriteResult_CapturesDiagnosticsButIgnoresNormalDrops()
    {
        LogSelfLog.EnableMemory();

        LogSelfLog.WriteResult("source", LogResult.DiagnosticCode("diagnostic", "check"));
        LogSelfLog.WriteResult("source", LogResult.Dropped("disabled"));
        string[] entries = LogSelfLog.Entries();

        Assert.That(entries.Length, Is.EqualTo(1));
        Assert.That(entries[0], Does.Contain("diagnostic"));
        Assert.That(entries[0], Does.Contain("check"));
    }

    [Test]
    public void Disable_StopsCapturingDiagnostics()
    {
        LogSink sink = LogSink.Named("failure");

        LogSelfLog.EnableMemory();
        LogSelfLog.Disable();
        sink.SetResult(LogResult.FailureCode("sink.failure", "failed"));
        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(LogSelfLog.Entries().Length, Is.EqualTo(0));
        Assert.That(LogSelfLog.WrittenCount(), Is.EqualTo(0));
    }

    [Test]
    public void Enable_InvokesConfiguredActionAndKeepsMemoryCopy()
    {
        string captured = "";

        LogSelfLog.Enable(message => captured = message);
        LogResult result = LogSelfLog.Write("internal failure");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.EqualTo("internal failure"));
        Assert.That(LogSelfLog.Entries()[0], Is.EqualTo("internal failure"));
    }

    [Test]
    public void Write_GuardsAgainstReentrantSelfLogging()
    {
        int actionCount = 0;

        LogSelfLog.Enable(message =>
        {
            actionCount = actionCount + 1;
            LogSelfLog.Write("nested");
        });
        LogResult result = LogSelfLog.Write("outer");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(actionCount, Is.EqualTo(1));
        Assert.That(LogSelfLog.WrittenCount(), Is.EqualTo(1));
        Assert.That(LogSelfLog.DroppedReentrantCount(), Is.EqualTo(1));
        Assert.That(LogSelfLog.Entries().Length, Is.EqualTo(1));
        Assert.That(LogSelfLog.Entries()[0], Is.EqualTo("outer"));
    }

    [Test]
    public void Entries_ReturnsDefensiveCopy()
    {
        LogSelfLog.EnableMemory();
        LogSelfLog.Write("first");

        string[] entries = LogSelfLog.Entries();
        entries[0] = "changed";

        Assert.That(LogSelfLog.Entries()[0], Is.EqualTo("first"));
    }

    [Test]
    public void PipelineAndLogger_ReportFailureResultsToSelfLog()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = LogSink.Named("failure");

        LogSelfLog.EnableMemory();
        sink.SetResult(LogResult.FailureCode("sink.failure", "failed"));
        logger.AddSink(sink);
        logger.Info("message");
        string[] entries = LogSelfLog.Entries();

        Assert.That(entries.Length, Is.EqualTo(3));
        Assert.That(entries[0], Does.Contain("sink failure"));
        Assert.That(entries[1], Does.Contain("pipeline"));
        Assert.That(entries[2], Does.Contain("logger"));
    }
}
