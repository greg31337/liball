using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicConcurrencyTests
{
    [Test]
    public void SharedLogger_MemoryAndDelegateSinksPreserveEveryWriteAndCounter()
    {
        int workerCount = 8;
        int writesPerWorker = 25;
        int expectedCount = workerCount * writesPerWorker;
        int delegateCount = 0;
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILogger writer = logger;
        ILoggerPipeline pipeline = logger;
        ILoggerState loggerState = logger;
        LogSink memory = components.CreateMemorySink("memory", 0);
        LogSink callback = components.CreateDelegateSink(
            "callback",
            logEvent =>
            {
                Interlocked.Increment(ref delegateCount);
                return new LogResult(true, false, false, false, "", "", 0, 0, 0);
            });
        pipeline.AddSink(memory);
        pipeline.AddSink(callback);

        RunConcurrent(workerCount, worker =>
        {
            int index = 0;
            while (index < writesPerWorker)
            {
                writer.WriteWithValues(LogLevel.Info, "worker {Worker} item {Item}", new object[] { worker, index });
                index = index + 1;
            }
        });

        Assert.That(loggerState.GetWrittenCount(), Is.EqualTo(expectedCount));
        Assert.That(loggerState.GetDroppedCount(), Is.Zero);
        Assert.That(loggerState.GetFailureCount(), Is.Zero);
        Assert.That(((ILogSinkState)memory).GetWrittenCount(), Is.EqualTo(expectedCount));
        Assert.That(((ILogSinkState)callback).GetWrittenCount(), Is.EqualTo(expectedCount));
        Assert.That(components.GetMemorySinkEvents(memory), Has.Length.EqualTo(expectedCount));
        Assert.That(delegateCount, Is.EqualTo(expectedCount));
    }

    [Test]
    public void SharedLevelSwitch_ConcurrentChangesProduceAccountedAcceptedAndDroppedWrites()
    {
        int roundCount = 40;
        LogResult[] results = new LogResult[roundCount * 2];
        Barrier phases = new Barrier(2);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        ILogLevelSwitch switchContract = levelSwitch;
        switchContract.SetLevel(LogLevel.Trace);
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithSwitch(levelSwitch);
        ILogger writer = logger;
        ILoggerState state = logger;
        LogSink memory = components.CreateMemorySink("levels", 0);
        ((ILoggerPipeline)logger).AddSink(memory);

        Task switchTask = Task.Factory.StartNew(
            () =>
            {
                int round = 0;
                while (round < roundCount)
                {
                    switchContract.SetLevel(LogLevel.Off);
                    phases.SignalAndWait();
                    phases.SignalAndWait();
                    switchContract.SetLevel(LogLevel.Trace);
                    phases.SignalAndWait();
                    phases.SignalAndWait();
                    round = round + 1;
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Task writeTask = Task.Factory.StartNew(
            () =>
            {
                int round = 0;
                while (round < roundCount)
                {
                    phases.SignalAndWait();
                    results[round * 2] = writer.WriteWithValues(LogLevel.Info, "disabled {Round}", new object[] { round });
                    phases.SignalAndWait();
                    phases.SignalAndWait();
                    results[round * 2 + 1] = writer.WriteWithValues(LogLevel.Info, "enabled {Round}", new object[] { round });
                    phases.SignalAndWait();
                    round = round + 1;
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Task.WaitAll(switchTask, writeTask);

        int roundIndex = 0;
        while (roundIndex < roundCount)
        {
            Assert.That(results[roundIndex * 2].IsDropped, Is.True);
            Assert.That(results[roundIndex * 2 + 1].IsSuccess, Is.True);
            roundIndex = roundIndex + 1;
        }

        Assert.That(state.GetWrittenCount(), Is.EqualTo(roundCount));
        Assert.That(state.GetDroppedCount(), Is.EqualTo(roundCount));
        Assert.That(((ILogSinkState)memory).GetWrittenCount(), Is.EqualTo(roundCount));
        Assert.That(components.GetMemorySinkEvents(memory), Has.Length.EqualTo(roundCount));
    }

    [Test]
    public void ConcurrentScopesAndSamplingFilter_KeepAsyncContextIsolated()
    {
        int workerCount = 40;
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILogger writer = logger;
        ILoggerContext context = logger;
        ILoggerPipeline pipeline = logger;
        LogSink memory = components.CreateMemorySink("scoped", 0);
        pipeline.AddFilter(components.CreateSamplingFilter(2));
        pipeline.AddSink(memory);
        Task[] tasks = new Task[workerCount];

        int index = 0;
        while (index < workerCount)
        {
            int worker = index;
            tasks[index] = Task.Run(async () =>
            {
                LogScope scope = context.BeginScopeField("RequestId", "request-" + worker);
                await Task.Yield();
                writer.WriteWithValues(LogLevel.Info, "worker {Worker}", new object[] { worker });
                scope.Dispose();
            });
            index = index + 1;
        }

        Task.WaitAll(tasks);
        LogEvent[] events = components.GetMemorySinkEvents(memory);

        Assert.That(events, Has.Length.EqualTo(workerCount / 2));
        foreach (LogEvent logEvent in events)
        {
            int worker = (int)FieldValue(logEvent.Fields, "Worker").IntegerValue;
            Assert.That(FieldValue(logEvent.Fields, "RequestId").TextValue, Is.EqualTo("request-" + worker));
        }
    }

    [Test]
    public void SelfDiagnostics_ConcurrentFailuresAreRecordedWithoutFalseReentrancyDrops()
    {
        int workerCount = 4;
        int callbackCount = 0;
        CountdownEvent firstCallbacks = new CountdownEvent(workerCount);
        ManualResetEventSlim releaseCallbacks = new ManualResetEventSlim(false);
        Barrier start = new Barrier(workerCount);
        ILogDiagnostics diagnostics = new LogDiagnostics();
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger[] loggers = new Logger[workerCount];
        Task[] tasks = new Task[workerCount];

        int index = 0;
        while (index < workerCount)
        {
            Logger logger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Worker" + index, LogLevel.Trace);
            LogSink failure = components.CreateDelegateSink(
                "failure-" + index,
                logEvent => new LogResult(false, true, false, false, "sink.failure", "failed", 0, 1, 0));
            ((ILoggerPipeline)logger).AddSink(failure);
            loggers[index] = logger;
            index = index + 1;
        }

        diagnostics.Enable(message =>
        {
            int count = Interlocked.Increment(ref callbackCount);
            if (count <= workerCount)
            {
                firstCallbacks.Signal();
            }

            releaseCallbacks.Wait(TimeSpan.FromSeconds(5));
        });

        bool allCallbacksArrived = false;
        try
        {
            index = 0;
            while (index < workerCount)
            {
                int worker = index;
                tasks[index] = Task.Factory.StartNew(
                    () =>
                    {
                        start.SignalAndWait();
                        loggers[worker].Error("failure");
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                index = index + 1;
            }

            allCallbacksArrived = firstCallbacks.Wait(TimeSpan.FromSeconds(2));
        }
        finally
        {
            releaseCallbacks.Set();
            Task.WaitAll(tasks);
        }

        int writtenCount = diagnostics.GetWrittenCount();
        string[] entries = diagnostics.GetEntries();
        int droppedReentrantCount = diagnostics.GetDroppedReentrantCount();
        diagnostics.Disable();
        diagnostics.Clear();

        Assert.That(allCallbacksArrived, Is.True);
        Assert.That(writtenCount, Is.EqualTo(workerCount * 3));
        Assert.That(callbackCount, Is.EqualTo(workerCount * 3));
        Assert.That(entries, Has.Length.EqualTo(workerCount * 3));
        Assert.That(droppedReentrantCount, Is.Zero);
    }

    [Test]
    public void QueuedAndBatchedSinks_ConcurrentWritesDrainWithoutLoss()
    {
        int workerCount = 6;
        int writesPerWorker = 20;
        int expectedCount = workerCount * writesPerWorker;
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILogger writer = logger;
        ILoggerPipeline pipeline = logger;
        LogSink asyncInner = components.CreateMemorySink("async-inner", 0);
        LogSink batchInner = components.CreateMemorySink("batch-inner", 0);
        LogSink asyncSink = components.CreateAsyncSink("async", asyncInner, expectedCount, LogBackpressureMode.DropNewest);
        LogSink batchSink = components.CreateBatchSink("batch", batchInner, 7);
        pipeline.AddSink(asyncSink);
        pipeline.AddSink(batchSink);

        RunConcurrent(workerCount, worker =>
        {
            int item = 0;
            while (item < writesPerWorker)
            {
                writer.WriteWithValues(LogLevel.Info, "item {Worker} {Item}", new object[] { worker, item });
                item = item + 1;
            }
        });

        Assert.That(components.GetAsyncSinkEvents(asyncSink), Has.Length.EqualTo(expectedCount));
        Assert.That(components.GetBatchSinkEvents(batchSink), Has.Length.EqualTo(expectedCount % 7));
        Assert.That(pipeline.Drain().IsSuccess, Is.True);
        Assert.That(components.GetAsyncSinkEvents(asyncSink), Is.Empty);
        Assert.That(components.GetBatchSinkEvents(batchSink), Is.Empty);
        Assert.That(components.GetMemorySinkEvents(asyncInner), Has.Length.EqualTo(expectedCount));
        Assert.That(components.GetMemorySinkEvents(batchInner), Has.Length.EqualTo(expectedCount));
        Assert.That(((ILogSinkState)asyncSink).GetWrittenCount(), Is.EqualTo(expectedCount));
        Assert.That(((ILogSinkState)batchSink).GetWrittenCount(), Is.EqualTo(expectedCount));
    }

    [Test]
    public void RollingFileSink_ConcurrentWritesProduceOneCompleteCopyOfEveryMessage()
    {
        int writeCount = 32;
        string directory = NewTempDirectory("rolling");
        string path = Path.Combine(directory, "events.log");
        Directory.CreateDirectory(directory);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        LogRollingPolicy rolling = components.CreateRollingPolicy(LogRollingInterval.None, 1);
        LogSink sink = components.CreateFileSink(path, new RenderedMessageFormatter(), true, rolling, null, true);

        try
        {
            RunConcurrent(writeCount, index =>
            {
                LogEvent logEvent = components.CreateEvent(LogLevel.Info, "Application", "event", "event " + index, new LogField[0]);
                Assert.That(((ILogSink)sink).Write(logEvent).IsSuccess, Is.True);
            });
            LogResult closeResult = ((ILogSink)sink).Close();
            HashSet<string> messages = ReadFileMessages(directory);

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(((ILogSinkState)sink).GetWrittenCount(), Is.EqualTo(writeCount));
            Assert.That(messages.Count, Is.EqualTo(writeCount));
            int index = 0;
            while (index < writeCount)
            {
                Assert.That(messages.Contains("event " + index), Is.True);
                index = index + 1;
            }
        }
        finally
        {
            ((ILogSink)sink).Close();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConfigurationReload_ConcurrentManualReloadsSerializeCallbacksAndCounters()
    {
        int workerCount = 8;
        int configurationCallbacks = 0;
        int loggerCallbacks = 0;
        string path = Path.Combine(NewTempDirectory("reload"), "log.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ \"name\": \"Application.Reload\", \"minimumLevel\": \"Debug\", \"sinks\": [\"memory\"] }");
        LogFactory factory = new LogFactory();
        ILogConfigurationReload reload = ((ILogFactory)factory).WatchJsonFile(path)
            .OnConfigurationReloaded(configuration => Interlocked.Increment(ref configurationCallbacks))
            .OnLoggerReloaded(logger => Interlocked.Increment(ref loggerCallbacks));

        try
        {
            RunConcurrent(workerCount, worker => Assert.That(reload.Reload().IsSuccess, Is.True));

            Assert.That(reload.GetReloadCount(), Is.EqualTo(workerCount));
            Assert.That(reload.GetFailureCount(), Is.Zero);
            Assert.That(configurationCallbacks, Is.EqualTo(workerCount));
            Assert.That(loggerCallbacks, Is.EqualTo(workerCount));
            Assert.That(reload.GetLastLogger().GetName(), Is.EqualTo("Application.Reload"));
            Assert.That(reload.GetLastLogger().GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));
            Assert.That(reload.GetIsWatching(), Is.False);
        }
        finally
        {
            reload.Dispose();
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Test]
    public void CloseDuringWrite_WaitsForInFlightWriteThenRejectsLaterWrites()
    {
        ManualResetEventSlim writeEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseWrite = new ManualResetEventSlim(false);
        ManualResetEventSlim closeStarted = new ManualResetEventSlim(false);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILogger writer = logger;
        ILoggerPipeline pipeline = logger;
        LogSink sink = components.CreateDelegateSink(
            "blocking",
            logEvent =>
            {
                writeEntered.Set();
                releaseWrite.Wait(TimeSpan.FromSeconds(5));
                return new LogResult(true, false, false, false, "", "", 0, 0, 0);
            });
        pipeline.AddSink(sink);
        Task<LogResult> writeTask = Task.Factory.StartNew(
            () => writer.WriteWithValues(LogLevel.Info, "in flight", null),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Assert.That(writeEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Task<LogResult> closeTask = Task.Factory.StartNew(
            () =>
            {
                closeStarted.Set();
                return pipeline.Close();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Assert.That(closeStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

        bool closeCompletedBeforeWrite;
        try
        {
            closeCompletedBeforeWrite = closeTask.Wait(TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            releaseWrite.Set();
        }

        Task.WaitAll(writeTask, closeTask);
        LogResult afterClose = writer.WriteWithValues(LogLevel.Info, "after close", null);

        Assert.That(closeCompletedBeforeWrite, Is.False);
        Assert.That(writeTask.Result.IsSuccess, Is.True);
        Assert.That(closeTask.Result.IsSuccess, Is.True);
        Assert.That(((ILogSinkState)sink).GetIsClosed(), Is.True);
        Assert.That(afterClose.IsDropped, Is.True);
        Assert.That(afterClose.Message, Is.EqualTo("log event disabled"));
    }

    [Test]
    public void DirectSinkCloseDuringWrite_WaitsForCallbackAndClosesAtomically()
    {
        ManualResetEventSlim writeEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseWrite = new ManualResetEventSlim(false);
        ManualResetEventSlim closeStarted = new ManualResetEventSlim(false);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        LogSink concreteSink = components.CreateDelegateSink(
            "direct-blocking",
            logEvent =>
            {
                writeEntered.Set();
                releaseWrite.Wait(TimeSpan.FromSeconds(5));
                return new LogResult(true, false, false, false, "", "", 0, 0, 0);
            });
        ILogSink sink = concreteSink;
        LogEvent first = components.CreateEvent(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = components.CreateEvent(LogLevel.Info, "Application", "second", "second", new LogField[0]);
        Task<LogResult> writeTask = Task.Factory.StartNew(
            () => sink.Write(first),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Assert.That(writeEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Task<LogResult> closeTask = Task.Factory.StartNew(
            () =>
            {
                closeStarted.Set();
                return sink.Close();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Assert.That(closeStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

        bool closeCompletedBeforeWrite;
        try
        {
            closeCompletedBeforeWrite = closeTask.Wait(TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            releaseWrite.Set();
        }

        Task.WaitAll(writeTask, closeTask);
        LogResult afterClose = sink.Write(second);

        Assert.That(closeCompletedBeforeWrite, Is.False);
        Assert.That(writeTask.Result.IsSuccess, Is.True);
        Assert.That(closeTask.Result.IsSuccess, Is.True);
        Assert.That(((ILogSinkState)concreteSink).GetIsClosed(), Is.True);
        Assert.That(afterClose.IsFailure, Is.True);
        Assert.That(afterClose.Code, Is.EqualTo("sink.closed"));
    }

    static void RunConcurrent(int workerCount, Action<int> action)
    {
        Task[] tasks = new Task[workerCount];
        int index = 0;
        while (index < workerCount)
        {
            int worker = index;
            tasks[index] = Task.Run(() => action(worker));
            index = index + 1;
        }

        Task.WaitAll(tasks);
    }

    static LogValue FieldValue(LogField[] fields, string name)
    {
        foreach (LogField field in fields)
        {
            if (field.Name == name)
            {
                return field.Value;
            }
        }

        return LogValue.Null();
    }

    static string NewTempDirectory(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-public-concurrency", Guid.NewGuid().ToString("N"), name);
    }

    static HashSet<string> ReadFileMessages(string directory)
    {
        HashSet<string> messages = new HashSet<string>();
        foreach (string file in Directory.GetFiles(directory, "events*.log"))
        {
            messages.Add(File.ReadAllText(file));
        }

        return messages;
    }

    sealed class RenderedMessageFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return logEvent.RenderedMessage;
        }
    }
}
