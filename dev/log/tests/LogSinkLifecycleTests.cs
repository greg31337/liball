using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogSinkLifecycleTests
{
    [Test]
    public void Flush_DrainsAsyncQueueAndFlushesInnerSink()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        sink.Write(first);
        sink.Write(second);
        LogResult result = LogSinkLifecycle.Flush(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(first));
        Assert.That(MemoryLogSink.Events(inner)[1], Is.SameAs(second));
    }

    [Test]
    public void Flush_WritesPartialBatchToInnerSink()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 3);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.Write(logEvent);
        LogResult result = LogSinkLifecycle.Flush(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(logEvent));
    }

    [Test]
    public void Close_DrainsWrapperAndClosesInnerFileSink()
    {
        string filePath = NewTempFilePath();
        LogSink fileSink = FileLogSink.Append(filePath);
        LogSink sink = AsyncLogSink.WithCapacity(fileSink, 4);

        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));
        LogResult result = LogSinkLifecycle.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.IsClosed, Is.True);
        Assert.That(sink.IsEnabled, Is.False);
        Assert.That(fileSink.IsClosed, Is.True);
        Assert.That(fileSink.FileIsOpen, Is.False);
        Assert.That(File.ReadAllText(filePath), Does.Contain("rendered"));
    }

    [Test]
    public void CloseWithZeroTimeout_ReturnsFailureWhenWorkIsPending()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);

        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogResult result = LogSinkLifecycle.CloseWithTimeout(sink, TimeSpan.Zero);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.shutdown.timeout"));
        Assert.That(sink.IsClosed, Is.False);
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(0));
    }

    [Test]
    public void DrainWithZeroTimeout_SucceedsWhenNoWorkIsPending()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);

        LogResult result = LogSinkLifecycle.DrainWithTimeout(sink, TimeSpan.Zero);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void SetShutdownTimeout_StoresNormalizedTimeoutOnNestedSinks()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 2);

        LogSinkLifecycle.SetShutdownTimeout(sink, TimeSpan.FromMilliseconds(25));

        Assert.That(sink.ShutdownTimeoutMilliseconds, Is.EqualTo(25));
        Assert.That(inner.ShutdownTimeoutMilliseconds, Is.EqualTo(25));
    }

    [Test]
    public void ClosedSinkRejectsWrites()
    {
        LogSink sink = MemoryLogSink.Create();

        LogSinkLifecycle.Close(sink);
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.closed"));
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void Pipeline_DrainFlushAndCloseApplyToEverySink()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink firstInner = MemoryLogSink.Create();
        LogSink secondInner = MemoryLogSink.Create();
        LogSink asyncSink = AsyncLogSink.WithCapacity(firstInner, 4);
        LogSink batchSink = BatchLogSink.WithSize(secondInner, 4);

        pipeline.SetSinks(new LogSink[] { asyncSink, batchSink });
        pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogResult drainResult = pipeline.Drain();
        LogResult closeResult = pipeline.Close();

        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(closeResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(firstInner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Count(secondInner), Is.EqualTo(1));
        Assert.That(asyncSink.IsClosed, Is.True);
        Assert.That(batchSink.IsClosed, Is.True);
    }

    [Test]
    public void Logger_FlushAndCloseDelegateToPipeline()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink batchSink = BatchLogSink.WithSize(inner, 4);

        logger.AddSink(batchSink);
        logger.Info("message");
        LogResult flushResult = logger.Flush();
        LogResult closeResult = logger.Close();

        Assert.That(flushResult.IsSuccess, Is.True);
        Assert.That(closeResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(batchSink.IsClosed, Is.True);
    }

    static string NewTempFilePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "log-tests", Guid.NewGuid().ToString("N"), "nested");
        return Path.Combine(directory, "events.log");
    }
}
