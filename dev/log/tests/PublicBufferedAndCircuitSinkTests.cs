using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicBufferedAndCircuitSinkTests
{
    [Test]
    public void AsyncSink_AllBackpressureModesHonorCapacityAndPreserveOrder()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent first = Event(logFactory, "first");
        LogEvent second = Event(logFactory, "second");
        LogSink newestInner = logFactory.CreateMemorySink("newest-inner", 0);
        LogSink oldestInner = logFactory.CreateMemorySink("oldest-inner", 0);
        LogSink blockInner = logFactory.CreateMemorySink("block-inner", 0);
        LogSink dropNewest = logFactory.CreateAsyncSink("drop-newest", newestInner, 0, LogBackpressureMode.DropNewest);
        LogSink dropOldest = logFactory.CreateAsyncSink("drop-oldest", oldestInner, 1, LogBackpressureMode.DropOldest);
        LogSink block = logFactory.CreateAsyncSink("block", blockInner, 1, LogBackpressureMode.Block);

        Assert.That(dropNewest.Write(first).IsSuccess, Is.True);
        LogResult newestResult = dropNewest.Write(second);
        Assert.That(newestResult.IsDropped, Is.True);
        Assert.That(newestResult.Message, Is.EqualTo("async queue full"));
        Assert.That(logFactory.GetAsyncSinkEvents(dropNewest), Is.EqualTo(new LogEvent[] { first }));
        Assert.That(((ILogSinkState)dropNewest).GetDroppedCount(), Is.EqualTo(1));

        Assert.That(dropOldest.Write(first).IsSuccess, Is.True);
        LogResult oldestResult = dropOldest.Write(second);
        Assert.That(oldestResult.IsSuccess, Is.True);
        Assert.That(oldestResult.DroppedCount, Is.EqualTo(1));
        Assert.That(oldestResult.Message, Is.EqualTo("async queue dropped oldest"));
        Assert.That(logFactory.GetAsyncSinkEvents(dropOldest), Is.EqualTo(new LogEvent[] { second }));
        Assert.That(((ILogSinkState)dropOldest).GetDroppedCount(), Is.EqualTo(1));

        Assert.That(block.Write(first).IsSuccess, Is.True);
        Assert.That(block.Write(second).IsSuccess, Is.True);
        Assert.That(logFactory.GetMemorySinkEvents(blockInner), Is.EqualTo(new LogEvent[] { first }));
        Assert.That(logFactory.GetAsyncSinkEvents(block), Is.EqualTo(new LogEvent[] { second }));

        LogEvent[] snapshot = logFactory.GetAsyncSinkEvents(block);
        snapshot[0] = first;
        Assert.That(logFactory.GetAsyncSinkEvents(block)[0], Is.SameAs(second));
        Assert.That(block.Drain().IsSuccess, Is.True);
        Assert.That(logFactory.GetAsyncSinkEvents(block), Is.Empty);
        Assert.That(logFactory.GetMemorySinkEvents(blockInner), Is.EqualTo(new LogEvent[] { first, second }));
    }

    [Test]
    public void BatchSink_HandlesNormalizedExactAndPartialBatchBoundaries()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent first = Event(logFactory, "first");
        LogEvent second = Event(logFactory, "second");
        LogEvent third = Event(logFactory, "third");
        LogSink exactInner = logFactory.CreateMemorySink("exact-inner", 0);
        LogSink exact = logFactory.CreateBatchSink("exact", exactInner, 2);
        LogSink normalizedInner = logFactory.CreateMemorySink("normalized-inner", 0);
        LogSink normalized = logFactory.CreateBatchSink("normalized", normalizedInner, 0);

        Assert.That(exact.Write(first).IsSuccess, Is.True);
        Assert.That(logFactory.GetBatchSinkEvents(exact), Is.EqualTo(new LogEvent[] { first }));
        LogEvent[] snapshot = logFactory.GetBatchSinkEvents(exact);
        snapshot[0] = second;
        Assert.That(logFactory.GetBatchSinkEvents(exact)[0], Is.SameAs(first));
        Assert.That(exact.Write(second).IsSuccess, Is.True);
        Assert.That(logFactory.GetBatchSinkEvents(exact), Is.Empty);
        Assert.That(logFactory.GetMemorySinkEvents(exactInner), Is.EqualTo(new LogEvent[] { first, second }));

        Assert.That(exact.Write(third).IsSuccess, Is.True);
        Assert.That(logFactory.GetBatchSinkEvents(exact), Is.EqualTo(new LogEvent[] { third }));
        Assert.That(exact.Flush().IsSuccess, Is.True);
        Assert.That(logFactory.GetBatchSinkEvents(exact), Is.Empty);
        Assert.That(logFactory.GetMemorySinkEvents(exactInner), Is.EqualTo(new LogEvent[] { first, second, third }));

        Assert.That(normalized.Write(first).IsSuccess, Is.True);
        Assert.That(logFactory.GetBatchSinkEvents(normalized), Is.Empty);
        Assert.That(logFactory.GetMemorySinkEvents(normalizedInner), Is.EqualTo(new LogEvent[] { first }));
    }

    [Test]
    public void BufferedSinks_ReportInnerAndMissingSinkFailures()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent first = Event(logFactory, "first");
        LogEvent second = Event(logFactory, "second");
        LogSink failingAsyncInner = logFactory.CreateDelegateSink(
            "async-failure",
            logEvent => new LogResult(false, true, false, false, "inner.async.failed", "failed", 0, 1, 0));
        LogSink failingBatchInner = logFactory.CreateDelegateSink(
            "batch-failure",
            logEvent => new LogResult(false, true, false, false, "inner.batch.failed", "failed", 0, 1, 0));
        LogSink asyncSink = logFactory.CreateAsyncSink("async", failingAsyncInner, 1, LogBackpressureMode.Block);
        LogSink batchSink = logFactory.CreateBatchSink("batch", failingBatchInner, 2);

        Assert.That(asyncSink.Write(first).IsSuccess, Is.True);
        LogResult asyncFailure = asyncSink.Write(second);
        Assert.That(asyncFailure.IsFailure, Is.True);
        Assert.That(asyncFailure.Code, Is.EqualTo("inner.async.failed"));
        Assert.That(logFactory.GetAsyncSinkEvents(asyncSink), Is.Empty);
        Assert.That(((ILogSinkState)asyncSink).GetFailureCount(), Is.EqualTo(1));

        Assert.That(batchSink.Write(first).IsSuccess, Is.True);
        LogResult batchFailure = batchSink.Write(second);
        Assert.That(batchFailure.IsFailure, Is.True);
        Assert.That(batchFailure.Code, Is.EqualTo("inner.batch.failed"));
        Assert.That(batchFailure.FailureCount, Is.EqualTo(2));
        Assert.That(logFactory.GetBatchSinkEvents(batchSink), Is.Empty);
        Assert.That(((ILogSinkState)batchSink).GetFailureCount(), Is.EqualTo(2));

        LogSink missingAsync = logFactory.CreateAsyncSink("missing-async", null, 1, LogBackpressureMode.Block);
        LogSink missingBatch = logFactory.CreateBatchSink("missing-batch", null, 1);
        LogSink missingCircuit = logFactory.CreateCircuitBreakerSink("missing-circuit", null, 1);
        Assert.That(missingAsync.Write(first).Code, Is.EqualTo("sink.async.inner.missing"));
        Assert.That(missingBatch.Write(first).Code, Is.EqualTo("sink.batch.inner.missing"));
        Assert.That(missingCircuit.Write(first).Code, Is.EqualTo("sink.circuit.inner.missing"));
        Assert.That(missingCircuit.Write(second).IsDropped, Is.True);
    }

    [Test]
    public void CircuitBreaker_RecoversFromTransientFailureThenOpensAfterConsecutiveFailures()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        int attempts = 0;
        LogSink inner = logFactory.CreateDelegateSink(
            "scripted",
            logEvent =>
            {
                attempts = attempts + 1;
                if (attempts == 2)
                {
                    return new LogResult(true, false, false, false, "", "", 0, 0, 0);
                }

                return new LogResult(false, true, false, false, "inner.failed", "failed", 0, 1, 0);
            });
        LogSink breaker = logFactory.CreateCircuitBreakerSink("breaker", inner, 2);
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline pipeline = logger;
        pipeline.AddSink(breaker);

        LogResult firstFailure = logger.WriteWithValues(LogLevel.Error, "first", null);
        LogSinkHealth afterFailure = pipeline.Health().Sinks[0];
        LogResult recovered = logger.WriteWithValues(LogLevel.Error, "recovered", null);
        LogSinkHealth afterRecovery = pipeline.Health().Sinks[0];
        LogResult nextFailure = logger.WriteWithValues(LogLevel.Error, "third", null);
        LogResult openingFailure = logger.WriteWithValues(LogLevel.Error, "fourth", null);
        LogResult loggerOpenResult = logger.WriteWithValues(LogLevel.Error, "fifth", null);
        LogResult openResult = breaker.Write(Event(logFactory, "sixth"));
        LogSinkHealth openHealth = pipeline.Health().Sinks[0];

        Assert.That(firstFailure.IsFailure, Is.True);
        Assert.That(afterFailure.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(1));
        Assert.That(afterFailure.IsCircuitBreakerOpen, Is.False);
        Assert.That(recovered.IsSuccess, Is.True);
        Assert.That(afterRecovery.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(0));
        Assert.That(afterRecovery.IsCircuitBreakerOpen, Is.False);
        Assert.That(nextFailure.IsFailure, Is.True);
        Assert.That(openingFailure.IsFailure, Is.True);
        Assert.That(loggerOpenResult.IsDropped, Is.True);
        Assert.That(loggerOpenResult.Message, Is.EqualTo("log event disabled"));
        Assert.That(openResult.IsDropped, Is.True);
        Assert.That(openResult.Message, Is.EqualTo("circuit breaker open"));
        Assert.That(attempts, Is.EqualTo(4));
        Assert.That(openHealth.IsCircuitBreakerOpen, Is.True);
        Assert.That(openHealth.CircuitBreakerFailureThreshold, Is.EqualTo(2));
        Assert.That(openHealth.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(2));
        Assert.That(openHealth.FailureCount, Is.EqualTo(3));
        Assert.That(openHealth.DroppedCount, Is.EqualTo(1));
    }

    [Test]
    public void ShutdownTimeout_CanFailThenDrainAndRecursivelyCloseNestedSinks()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogSink memory = logFactory.CreateMemorySink("memory", 0);
        LogSink batch = logFactory.CreateBatchSink("batch", memory, 3);
        LogSink asyncSink = logFactory.CreateAsyncSink("async", batch, 4, LogBackpressureMode.Block);
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline pipeline = logger;
        pipeline.AddSink(asyncSink);
        logger.WriteWithValues(LogLevel.Info, "pending", null);
        pipeline.SetShutdownTimeout(TimeSpan.Zero);

        LogResult timedOut = pipeline.Close();

        Assert.That(timedOut.IsFailure, Is.True);
        Assert.That(timedOut.Code, Is.EqualTo("sink.shutdown.timeout"));
        Assert.That(((ILogSinkState)asyncSink).GetIsClosed(), Is.False);
        Assert.That(pipeline.Health().PendingEventCount, Is.EqualTo(1));
        Assert.That(logFactory.GetMemorySinkEvents(memory), Is.Empty);

        pipeline.SetShutdownTimeout(TimeSpan.FromSeconds(1));
        Assert.That(pipeline.Drain().IsSuccess, Is.True);
        Assert.That(logFactory.GetAsyncSinkEvents(asyncSink), Is.Empty);
        Assert.That(logFactory.GetBatchSinkEvents(batch), Is.Empty);
        Assert.That(logFactory.GetMemorySinkEvents(memory), Has.Length.EqualTo(1));
        Assert.That(pipeline.Close().IsSuccess, Is.True);
        Assert.That(pipeline.Close().IsSuccess, Is.True);
        Assert.That(((ILogSinkState)asyncSink).GetIsClosed(), Is.True);
        Assert.That(((ILogSinkState)batch).GetIsClosed(), Is.True);
        Assert.That(((ILogSinkState)memory).GetIsClosed(), Is.True);
    }

    [Test]
    public void NestedWrappers_AggregateRecursiveFlushAndCloseResults()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LifecycleSink destination = new LifecycleSink();
        LogSink wrapped = logFactory.WrapSink(destination);
        LogSink breaker = logFactory.CreateCircuitBreakerSink("breaker", wrapped, 2);
        LogSink batch = logFactory.CreateBatchSink("batch", breaker, 2);
        LogSink asyncSink = logFactory.CreateAsyncSink("async", batch, 4, LogBackpressureMode.Block);
        LogEvent first = Event(logFactory, "first");
        LogEvent second = Event(logFactory, "second");
        LogEvent third = Event(logFactory, "third");
        asyncSink.Write(first);
        asyncSink.Write(second);
        asyncSink.Write(third);

        LogResult flush = asyncSink.Flush();
        LogResult close = asyncSink.Close();

        Assert.That(flush.IsDiagnostic, Is.True);
        Assert.That(flush.Code, Is.EqualTo("inner.flush"));
        Assert.That(destination.Events, Is.EqualTo(new LogEvent[] { first, second, third }));
        Assert.That(logFactory.GetAsyncSinkEvents(asyncSink), Is.Empty);
        Assert.That(logFactory.GetBatchSinkEvents(batch), Is.Empty);
        Assert.That(close.IsFailure, Is.True);
        Assert.That(close.Code, Is.EqualTo("inner.close"));
        Assert.That(destination.FlushCount, Is.EqualTo(1));
        Assert.That(destination.CloseCount, Is.EqualTo(1));
        Assert.That(((ILogSinkState)asyncSink).GetIsClosed(), Is.False);
        Assert.That(((ILogSinkState)batch).GetIsClosed(), Is.False);
        Assert.That(((ILogSinkState)breaker).GetIsClosed(), Is.False);
        Assert.That(((ILogSinkState)wrapped).GetIsClosed(), Is.False);
    }

    private static LogEvent Event(ILogFactory factory, string message)
    {
        return factory.CreateEvent(LogLevel.Info, "Application", message, message, new LogField[0]);
    }

    private sealed class LifecycleSink : ILogSink
    {
        public LogEvent[] Events;
        public int FlushCount;
        public int DrainCount;
        public int CloseCount;

        public LifecycleSink()
        {
            Events = new LogEvent[0];
            FlushCount = 0;
            DrainCount = 0;
            CloseCount = 0;
        }

        public string GetName()
        {
            return "lifecycle";
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return level != LogLevel.Off;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return new LogResult(false, true, false, false, "inner.null", "event is null", 0, 1, 0);
            }

            LogEvent[] copy = new LogEvent[Events.Length + 1];
            int index = 0;
            while (index < Events.Length)
            {
                copy[index] = Events[index];
                index = index + 1;
            }

            copy[Events.Length] = logEvent;
            Events = copy;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Flush()
        {
            FlushCount = FlushCount + 1;
            return new LogResult(true, false, false, true, "inner.flush", "flush diagnostic", 0, 0, 1);
        }

        public LogResult Drain()
        {
            DrainCount = DrainCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Close()
        {
            CloseCount = CloseCount + 1;
            return new LogResult(false, true, false, false, "inner.close", "close failed", 0, 1, 0);
        }
    }
}
