using System;
using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixLogContractTests
{
    private string LogDirectory = "";

    [SetUp]
    public void CreateLogDirectory()
    {
        LogDirectory = Path.Combine(Path.GetTempPath(), "fix-log-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(LogDirectory);
    }

    [TearDown]
    public void RemoveLogDirectory()
    {
        if (Directory.Exists(LogDirectory)) Directory.Delete(LogDirectory, true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImplementationsShareCallerEntryOwnership(bool fileBacked)
    {
        string path = Path.Combine(LogDirectory, "session.audit");
        IFixLog log = fileBacked ? new FileFixLog(path, true) : new NullFixLog();
        byte[] bytes = Encoding.ASCII.GetBytes("8=FIX.4.4\u000135=A\u0001553=user\u0001554=secret\u0001");
        LogEntry entry = new LogEntry
        {
            TimestampUtc = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc),
            Id = new SessionId { BeginString = FixBeginStrings.Fix44, SenderCompID = "SENDER", TargetCompID = "TARGET" },
            Direction = LogDirection.Incoming,
            EventKind = SessionEventKind.MessageReceived,
            Bytes = bytes,
            Text = "received",
            IsRedacted = false
        };
        byte[] original = bytes.ToArray();

        log.WriteLog(entry);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Bytes, Is.SameAs(bytes));
            Assert.That(entry.Bytes, Is.EqualTo(original));
            Assert.That(entry.Text, Is.EqualTo("received"));
            Assert.That(entry.IsRedacted, Is.False);
            Assert.That(File.Exists(path), Is.EqualTo(fileBacked));
            if (fileBacked)
            {
                string text = File.ReadAllText(path);
                Assert.That(text, Does.Contain("553=***"));
                Assert.That(text, Does.Not.Contain("secret"));
            }
        });
    }
}
