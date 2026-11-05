using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogAsyncBatchWorkflowTests
{
    [Test]
    public void LoggerFlush_DrainsAsyncQueueThroughBatchBufferInOrder()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink batch = BatchLogSink.WithSize(inner, 4);
        LogSink async = AsyncLogSink.WithCapacity(batch, 4);

        logger.AddSink(async);
        LogResult firstResult = logger.Info("first");
        LogEvent firstEvent = logger.LastEvent!;
        LogResult secondResult = logger.Warn("second");
        LogResult flushResult = logger.Flush();
        LogEvent[] events = MemoryLogSink.Events(inner);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(flushResult.IsSuccess, Is.True);
        Assert.That(AsyncLogSink.Count(async), Is.EqualTo(0));
        Assert.That(BatchLogSink.Count(batch), Is.EqualTo(0));
        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0], Is.SameAs(firstEvent));
        Assert.That(events[1], Is.SameAs(logger.LastEvent));
        Assert.That(events[0].RenderedMessage, Is.EqualTo("first"));
        Assert.That(events[1].RenderedMessage, Is.EqualTo("second"));
    }

    [Test]
    public void DropOldestBackpressure_CountsDroppedQueuedEventThroughLoggerPipelineAndSink()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink async = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropOldest);

        logger.AddSink(async);
        LogResult firstResult = logger.Info("first");
        LogResult secondResult = logger.Info("second");
        LogResult drainResult = logger.Drain();

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.DroppedCount, Is.EqualTo(1));
        Assert.That(logger.DroppedCount, Is.EqualTo(1));
        Assert.That(logger.Pipeline.DroppedCount, Is.EqualTo(1));
        Assert.That(async.DroppedCount, Is.EqualTo(1));
        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0].RenderedMessage, Is.EqualTo("second"));
    }

    [Test]
    public void DropNewestBackpressure_DropsWriteAndKeepsPreviouslyQueuedEvent()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink async = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropNewest);

        logger.AddSink(async);
        LogResult firstResult = logger.Info("first");
        LogEvent firstEvent = logger.LastEvent!;
        LogResult secondResult = logger.Info("second");
        LogResult drainResult = logger.Drain();

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsDropped, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.SameAs(firstEvent));
        Assert.That(logger.DroppedCount, Is.EqualTo(1));
        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0].RenderedMessage, Is.EqualTo("first"));
    }

    [Test]
    public void Close_DrainsPendingAsyncAndBatchWorkBeforeDisablingSinks()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink batch = BatchLogSink.WithSize(inner, 8);
        LogSink async = AsyncLogSink.WithCapacity(batch, 8);

        logger.AddSink(async);
        LogResult writeResult = logger.Info("pending");
        LogResult closeResult = logger.Close();
        LogResult afterCloseResult = logger.Info("after close");

        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(closeResult.IsSuccess, Is.True);
        Assert.That(afterCloseResult.IsDropped, Is.True);
        Assert.That(async.IsClosed, Is.True);
        Assert.That(batch.IsClosed, Is.True);
        Assert.That(inner.IsClosed, Is.True);
        Assert.That(AsyncLogSink.Count(async), Is.EqualTo(0));
        Assert.That(BatchLogSink.Count(batch), Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0].RenderedMessage, Is.EqualTo("pending"));
    }

    [Test]
    public void FlushFailure_PropagatesFromQueuedAsyncInnerSinkAndClearsPendingQueue()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = LogSink.Named("failure");
        LogSink async = AsyncLogSink.WithCapacity(inner, 4);

        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        logger.AddSink(async);
        LogResult writeResult = logger.Error("queued");
        LogResult flushResult = logger.Flush();

        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(flushResult.IsFailure, Is.True);
        Assert.That(flushResult.Code, Is.EqualTo("inner.failed"));
        Assert.That(AsyncLogSink.Count(async), Is.EqualTo(0));
        Assert.That(inner.WrittenCount, Is.EqualTo(1));
        Assert.That(async.LastResult.IsFailure, Is.True);
    }
}
