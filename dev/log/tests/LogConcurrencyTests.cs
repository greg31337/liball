using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogConcurrencyTests
{
    [Test]
    public void SharedLogger_ConcurrentWritesAreAllRecorded()
    {
        int workerCount = 8;
        int writesPerWorker = 25;
        int expectedCount = workerCount * writesPerWorker;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = MemoryLogSink.Create();

        logger.AddSink(sink);
        RunConcurrent(workerCount, worker =>
        {
            int index = 0;
            while (index < writesPerWorker)
            {
                logger.InfoWithValues("worker {Worker} item {Item}", new object[] { worker, index });
                index = index + 1;
            }
        });

        Assert.That(logger.WrittenCount, Is.EqualTo(expectedCount));
        Assert.That(logger.Pipeline.WrittenCount, Is.EqualTo(expectedCount));
        Assert.That(sink.WrittenCount, Is.EqualTo(expectedCount));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(expectedCount));
    }

    [Test]
    public void ScopeFields_AreIsolatedAcrossConcurrentAsyncBranches()
    {
        int workerCount = 16;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = MemoryLogSink.Create();
        Task[] tasks = new Task[workerCount];

        logger.AddSink(sink);
        int index = 0;
        while (index < workerCount)
        {
            int worker = index;
            tasks[index] = Task.Run(async () =>
            {
                LogScope scope = logger.BeginScopeField("RequestId", "request-" + worker);
                await Task.Yield();
                logger.InfoWithValues("worker {Worker}", new object[] { worker });
                scope.Dispose();
            });
            index = index + 1;
        }

        Task.WaitAll(tasks);
        LogEvent[] events = MemoryLogSink.Events(sink);

        Assert.That(events.Length, Is.EqualTo(workerCount));
        foreach (LogEvent logEvent in events)
        {
            int worker = (int)FieldValue(logEvent.Fields, "Worker").IntegerValue;
            Assert.That(FieldValue(logEvent.Fields, "RequestId").TextValue, Is.EqualTo("request-" + worker));
        }

        Assert.That(logger.ScopeStorage.GetCurrentFields().Length, Is.EqualTo(0));
    }

    [Test]
    public void AsyncSink_ConcurrentWritesQueueAllEventsBeforeDrain()
    {
        int workerCount = 5;
        int writesPerWorker = 20;
        int expectedCount = workerCount * writesPerWorker;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink asyncSink = AsyncLogSink.WithCapacity(inner, expectedCount);

        logger.AddSink(asyncSink);
        RunConcurrent(workerCount, worker =>
        {
            int index = 0;
            while (index < writesPerWorker)
            {
                logger.InfoWithValues("queued {Worker} {Item}", new object[] { worker, index });
                index = index + 1;
            }
        });
        LogResult drainResult = logger.Drain();

        Assert.That(AsyncLogSink.Count(asyncSink), Is.EqualTo(0));
        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(expectedCount));
        Assert.That(asyncSink.WrittenCount, Is.EqualTo(expectedCount));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(expectedCount));
    }

    [Test]
    public void RollingFileSink_ConcurrentWritesProduceCompleteLinesAcrossRolledFiles()
    {
        int writeCount = 40;
        string filePath = NewTempFilePath();
        string directory = Path.GetDirectoryName(filePath)!;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, MessageOnlyFormatter(), true, LogRollingPolicy.BySize(1));

        FileLogSink.SetFlushOnWrite(sink, true);
        logger.AddSink(sink);
        RunConcurrent(writeCount, index => logger.InfoWithValues("event {Index}", new object[] { index }));
        LogResult closeResult = logger.Close();
        HashSet<string> lines = ReadLines(directory);

        Assert.That(closeResult.IsSuccess, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(writeCount));
        Assert.That(lines.Count, Is.EqualTo(writeCount));
        int index = 0;
        while (index < writeCount)
        {
            Assert.That(lines.Contains("event " + index), Is.True);
            index = index + 1;
        }
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
            if (field != null && field.Name == name)
            {
                return field.Value;
            }
        }

        return LogValue.Null();
    }

    static LogFormatter MessageOnlyFormatter()
    {
        LogFormatter formatter = LogFormatter.Custom("message", logEvent => logEvent.RenderedMessage);
        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        return formatter;
    }

    static HashSet<string> ReadLines(string directory)
    {
        HashSet<string> lines = new HashSet<string>();
        string[] files = Directory.GetFiles(directory, "events*.log");
        foreach (string file in files)
        {
            string[] fileLines = File.ReadAllLines(file);
            foreach (string line in fileLines)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    static string NewTempFilePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "log-concurrency-tests", Guid.NewGuid().ToString("N"));
        return Path.Combine(directory, "events.log");
    }
}
