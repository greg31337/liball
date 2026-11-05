using System;
using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixLogTests
{
    private string LogDirectory = "";

    [SetUp]
    public void CreateLogDirectory()
    {
        LogDirectory = Path.Combine(Path.GetTempPath(), "fix-log-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(LogDirectory);
    }

    [TearDown]
    public void RemoveLogDirectory()
    {
        if (Directory.Exists(LogDirectory))
        {
            Directory.Delete(LogDirectory, true);
        }
    }

    [Test]
    public void NullLogDiscardsEntriesWithoutMutatingThem()
    {
        LogEntry entry = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "received", Wire("8=FIX.4.4|35=D|"));
        byte[] expectedBytes = entry.Bytes.ToArray();
        IFixLog log = new NullFixLog();

        log.WriteLog(entry);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Bytes, Is.EqualTo(expectedBytes));
            Assert.That(entry.Text, Is.EqualTo("received"));
            Assert.That(entry.IsRedacted, Is.False);
        });
    }

    [Test]
    public void FileLogAppendsStructuredIncomingOutgoingAndEventEntries()
    {
        string path = Path.Combine(LogDirectory, "session.audit");
        FileFixLog log = new FileFixLog(path, false);
        LogEntry incoming = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "received\tmessage", Wire("8=FIX.4.4|35=D|49=BUY|"));
        LogEntry outgoing = Entry(LogDirection.Outgoing, SessionEventKind.MessageSent, "sent", Wire("8=FIX.4.4|35=8|49=SELL|"));
        LogEntry sessionEvent = Entry(LogDirection.Event, SessionEventKind.Connected, "connected\nready", []);

        log.WriteLog(incoming);
        log.WriteLog(outgoing);
        log.WriteLog(sessionEvent);

        string[] lines = File.ReadAllLines(path);
        string[] incomingFields = lines[0].Split('\t');
        string[] outgoingFields = lines[1].Split('\t');
        string[] eventFields = lines[2].Split('\t');
        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(3));
            Assert.That(incomingFields, Has.Length.EqualTo(10));
            Assert.That(incomingFields[0], Is.EqualTo(incoming.TimestampUtc.ToString("O")));
            Assert.That(incomingFields[1], Is.EqualTo(((int)LogDirection.Incoming).ToString()));
            Assert.That(incomingFields[2], Is.EqualTo(((int)SessionEventKind.MessageReceived).ToString()));
            Assert.That(incomingFields[3], Is.EqualTo("FIX.4.4"));
            Assert.That(incomingFields[4], Is.EqualTo("BUY"));
            Assert.That(incomingFields[5], Is.EqualTo("SELL"));
            Assert.That(incomingFields[6], Is.EqualTo("ORDERS"));
            Assert.That(incomingFields[7], Is.EqualTo("0"));
            Assert.That(incomingFields[8], Is.EqualTo("received\\tmessage"));
            Assert.That(incomingFields[9], Is.EqualTo("8=FIX.4.4|35=D|49=BUY|"));
            Assert.That(outgoingFields[1], Is.EqualTo(((int)LogDirection.Outgoing).ToString()));
            Assert.That(outgoingFields[8], Is.EqualTo("sent"));
            Assert.That(outgoingFields[9], Is.EqualTo("8=FIX.4.4|35=8|49=SELL|"));
            Assert.That(eventFields[1], Is.EqualTo(((int)LogDirection.Event).ToString()));
            Assert.That(eventFields[2], Is.EqualTo(((int)SessionEventKind.Connected).ToString()));
            Assert.That(eventFields[8], Is.EqualTo("connected\\nready"));
            Assert.That(eventFields[9], Is.Empty);
        });
    }

    [Test]
    public void SensitiveCredentialsAndBinaryDataAreRedactedWithoutMutatingCallerBytes()
    {
        string path = Path.Combine(LogDirectory, "redacted.audit");
        byte[] wire = Combine(
            Wire("8=FIX.4.4|35=A|49=BUY|553=alice|554=secret|925=new-secret|90=3|91="),
            [65, 1, 66],
            Wire("|95=3|96="),
            [0, 1, 255],
            Wire("|1401=3|1402="),
            [88, 1, 89],
            Wire("|1403=3|1404="),
            [90, 1, 91],
            Wire("|"));
        byte[] original = wire.ToArray();
        LogEntry entry = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "logon", wire);

        new FileFixLog(path, true).WriteLog(entry);

        string line = File.ReadAllText(path);
        string[] fields = line.TrimEnd('\r', '\n').Split('\t');
        Assert.Multiple(() =>
        {
            Assert.That(fields[7], Is.EqualTo("1"));
            Assert.That(fields[9], Does.Contain("49=BUY|"));
            Assert.That(fields[9], Does.Contain("553=***|"));
            Assert.That(fields[9], Does.Contain("554=***|"));
            Assert.That(fields[9], Does.Contain("925=***|"));
            Assert.That(fields[9], Does.Contain("90=***|91=***|"));
            Assert.That(fields[9], Does.Contain("95=***|96=***|"));
            Assert.That(fields[9], Does.Contain("1401=***|1402=***|"));
            Assert.That(fields[9], Does.Contain("1403=***|1404=***|"));
            Assert.That(line, Does.Not.Contain("alice"));
            Assert.That(line, Does.Not.Contain("secret"));
            Assert.That(entry.Bytes, Is.EqualTo(original));
            Assert.That(entry.IsRedacted, Is.False);
        });
    }

    [Test]
    public void RedactionCanBeDisabledForExactWireAuditing()
    {
        string path = Path.Combine(LogDirectory, "unredacted.audit");
        LogEntry entry = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "logon", Wire("8=FIX.4.4|35=A|553=alice|554=secret|"));

        new FileFixLog(path, false).WriteLog(entry);

        string[] fields = File.ReadAllText(path).TrimEnd('\r', '\n').Split('\t');
        Assert.Multiple(() =>
        {
            Assert.That(fields[7], Is.EqualTo("0"));
            Assert.That(fields[9], Does.Contain("553=alice|554=secret|"));
        });
    }

    [Test]
    public void MalformedWireIsWhollyRedactedWithoutLeakingItsContent()
    {
        string path = Path.Combine(LogDirectory, "malformed.audit");
        LogEntry entry = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "malformed", Encoding.ASCII.GetBytes("8=FIX.4.4\u0001554=secret"));

        new FileFixLog(path, true).WriteLog(entry);

        string line = File.ReadAllText(path);
        string[] fields = line.TrimEnd('\r', '\n').Split('\t');
        Assert.Multiple(() =>
        {
            Assert.That(fields[7], Is.EqualTo("1"));
            Assert.That(fields[9], Is.EqualTo("<redacted-malformed-fix-message>"));
            Assert.That(line, Does.Not.Contain("secret"));
        });
    }

    [TestCase("8=FIX.4.4|35=A|96=secret|")]
    [TestCase("8=FIX.4.4|35=A|95=2|96=secret|")]
    [TestCase("8=FIX.4.4|35=A|95=6|96=secret")]
    public void OrphanedOrLengthMismatchedSensitiveDataFailsClosed(string message)
    {
        string path = Path.Combine(LogDirectory, "malformed-sensitive.audit");
        LogEntry entry = Entry(LogDirection.Incoming, SessionEventKind.MessageReceived, "malformed", Wire(message));

        new FileFixLog(path, true).WriteLog(entry);

        string line = File.ReadAllText(path);
        string[] fields = line.TrimEnd('\r', '\n').Split('\t');
        Assert.Multiple(() =>
        {
            Assert.That(fields[7], Is.EqualTo("1"));
            Assert.That(fields[9], Is.EqualTo("<redacted-malformed-fix-message>"));
            Assert.That(line, Does.Not.Contain("secret"));
        });
    }

    [Test]
    public void MissingPathAndEntryAreRejected()
    {
        FileFixLog log = new FileFixLog(Path.Combine(LogDirectory, "validation.audit"), true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixStoreException>(() => new FileFixLog("", true));
            Assert.Throws<FixStoreException>(() => log.WriteLog(null!));
        });
    }

    [Test]
    public void AuditEscapingCoversBackslashesCarriageReturnsAndBinaryBytes()
    {
        string escaped = FileFixLog.EscapeFixLogBytes([92, 13, 0, 31, 127, 255]);
        string emptyText = FileFixLog.EscapeFixLogText(null!);
        byte[] emptyBytes = FileFixLog.CloneFixLogBytes(null!);

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("\\\\\\r\\x00\\x1F\\x7F\\xFF"));
            Assert.That(emptyText, Is.Empty);
            Assert.That(emptyBytes, Is.Empty);
        });
    }

    private static LogEntry Entry(LogDirection direction, SessionEventKind eventKind, string text, byte[] bytes)
    {
        LogEntry entry = new LogEntry();
        entry.TimestampUtc = new DateTime(2026, 8, 30, 12, 34, 56, DateTimeKind.Utc);
        entry.Id.BeginString = "FIX.4.4";
        entry.Id.SenderCompID = "BUY";
        entry.Id.TargetCompID = "SELL";
        entry.Id.Qualifier = "ORDERS";
        entry.Direction = direction;
        entry.EventKind = eventKind;
        entry.Text = text;
        entry.Bytes = bytes;
        return entry;
    }

    private static byte[] Wire(string text)
    {
        return Encoding.ASCII.GetBytes(text.Replace('|', (char)1));
    }

    private static byte[] Combine(params byte[][] parts)
    {
        int length = parts.Sum(part => part.Length);
        byte[] combined = new byte[length];
        int offset = 0;
        foreach (byte[] part in parts)
        {
            Buffer.BlockCopy(part, 0, combined, offset, part.Length);
            offset += part.Length;
        }
        return combined;
    }
}
