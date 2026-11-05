using System.Text;
using email;

namespace email.Tests;

public class EmailMboxWriterTests
{
    [Test]
    public void WritesDeterministicMboxrdEnvelopeAndReversiblyEscapesBodyLines()
    {
        EmailMessage message = Parse("Subject: quoting\n\nFrom plain\n>From already quoted\nordinary\n");
        using MemoryStream output = new MemoryStream();
        EmailMboxWriter writer = new EmailMboxWriterOperations().CreateWithOutput(output);

        EmailMboxWriteResult result = writer.AppendWithEnvelope(message, "sender@example.test", new DateTimeOffset(1997, 11, 21, 9, 55, 6, TimeSpan.Zero));

        const string envelope = "From sender@example.test Fri Nov 21 09:55:06 1997\n";
        string archive = Text(output.ToArray());
        Assert.That(archive, Is.EqualTo(envelope + "Subject: quoting\n\n>From plain\n>>From already quoted\nordinary\n\n"));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetFormat(), Is.EqualTo(EmailMboxFormat.Mboxrd));
        Assert.That(result.GetEscapedFromLineCount(), Is.EqualTo(2));
        Assert.That(result.GetEnvelopeLineRange()!.EqualsRange(new EmailRange(0, Bytes(envelope).Length)), Is.True);
        Assert.That(result.GetMessageRange()!.GetStartOffset(), Is.EqualTo(Bytes(envelope).Length));
        Assert.That(result.GetArchiveRange()!.GetLength(), Is.EqualTo(output.Length));

        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(output.ToArray(), false));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(Body(reader.GetCurrent()!), Is.EqualTo("From plain\n>From already quoted\nordinary\n\n"));
        Assert.That(reader.GetCurrent()!.GetEnvelopeSenderText(), Is.EqualTo("sender@example.test"));
        Assert.That(reader.GetCurrent()!.GetEnvelopeDateText(), Is.EqualTo("Fri Nov 21 09:55:06 1997"));
    }

    [Test]
    public void AppliesTheSelectedMboxoEscapingConventionOnlyInsideTheBody()
    {
        EmailMessage message = Parse("Subject: escaping\n>From malformed header\n\nFrom plain\n>From quoted\n");
        EmailMboxWriterOptions options = new EmailMboxWriterOptions();
        options.SetFormat(EmailMboxFormat.Mboxo);
        options.SetEnvelopeSender("sender@example.test");
        using MemoryStream output = new MemoryStream();
        EmailMboxWriter writer = new EmailMboxWriterOperations().CreateWithOutputAndOptions(output, options);

        EmailMboxWriteResult result = writer.Append(message);
        string archive = Text(output.ToArray());

        Assert.That(result.GetEscapedFromLineCount(), Is.EqualTo(1));
        Assert.That(archive, Does.Contain("\n>From malformed header\n\n>From plain\n>From quoted\n"));
        EmailMboxReaderOptions readOptions = new EmailMboxReaderOptions();
        readOptions.SetFormat(EmailMboxFormat.Mboxo);
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(output.ToArray(), false), readOptions);
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(Body(reader.GetCurrent()!), Is.EqualTo(">From malformed header\n\nFrom plain\nFrom quoted\n\n"));
    }

    [Test]
    public void AppendsAtTheEndOfAnExistingArchiveWithACompleteBoundary()
    {
        const string existing = "From old@example.test Thu Jan  1 00:00:00 1970\nSubject: old\n\nold";
        MemoryStream output = new MemoryStream();
        output.Write(Bytes(existing));
        output.Position = 2;
        EmailMboxWriter writer = new EmailMboxWriterOperations().CreateWithOutput(output);
        EmailMessage message = Parse("Subject: new\n\nnew\n");

        EmailMboxWriteResult result = writer.AppendWithEnvelope(message, "new@example.test", DateTimeOffset.UnixEpoch);

        Assert.That(result.GetPaddingByteCount(), Is.EqualTo(2));
        Assert.That(result.GetArchiveRange()!.GetStartOffset(), Is.EqualTo(Bytes(existing).LongLength + 2));
        Assert.That(Text(output.ToArray()), Does.StartWith(existing + "\n\nFrom new@example.test Thu Jan  1 00:00:00 1970\n"));
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(output.ToArray(), false));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("old"));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("new"));
        Assert.That(reader.ReadNext(), Is.False);
    }

    [Test]
    public void FlushesOnlyWhenRequestedOrConfiguredAndHonorsStreamOwnership()
    {
        MboxTrackingStream manualStream = new MboxTrackingStream();
        EmailMboxWriterOptions manualOptions = new EmailMboxWriterOptions();
        manualOptions.SetLeaveOpen(false);
        EmailMboxWriter manual = new EmailMboxWriterOperations().CreateWithOutputAndOptions(manualStream, manualOptions);
        manual.Append(Parse("Subject: manual\n\nbody\n"));
        Assert.That(manualStream.GetFlushCount(), Is.Zero);

        manual.Flush();
        Assert.That(manualStream.GetFlushCount(), Is.EqualTo(1));
        manual.Close();
        Assert.That(manualStream.GetFlushCount(), Is.EqualTo(2));
        Assert.That(manualStream.IsClosed(), Is.True);
        Assert.Throws<ObjectDisposedException>(() => manual.Append(Parse("Subject: late\n\nbody\n")));

        MboxTrackingStream automaticStream = new MboxTrackingStream();
        EmailMboxWriterOptions automaticOptions = new EmailMboxWriterOptions();
        automaticOptions.SetAutoFlush(true);
        EmailMboxWriter automatic = new EmailMboxWriterOperations().CreateWithOutputAndOptions(automaticStream, automaticOptions);
        automatic.Append(Parse("Subject: automatic\n\nbody\n"));
        Assert.That(automaticStream.GetFlushCount(), Is.EqualTo(1));
        automatic.Close();
        Assert.That(automaticStream.IsClosed(), Is.False);
    }

    [Test]
    public void EnforcesLimitsBeforeWritingAnyPartOfAnEntryAndSnapshotsOptions()
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        limits.SetMaxArchiveBytes(20);
        limits.SetMaxEntryBytes(20);
        limits.SetMaxEntryCount(1);
        EmailMboxWriterOptions supplied = new EmailMboxWriterOptions();
        supplied.SetLimits(limits);
        supplied.SetFormat(EmailMboxFormat.Mboxo);
        using MemoryStream output = new MemoryStream();
        EmailMboxWriter writer = new EmailMboxWriterOperations().CreateWithOutputAndOptions(output, supplied);
        supplied.SetFormat(EmailMboxFormat.Mboxrd);
        limits.SetMaxArchiveBytes(1000);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => writer.Append(Parse("Subject: too large\n\nbody\n")));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxMboxEntryBytes"));
        Assert.That(output.Length, Is.Zero);
        Assert.That(writer.GetOptions().GetFormat(), Is.EqualTo(EmailMboxFormat.Mboxo));
        Assert.That(writer.GetEntryCount(), Is.Zero);
    }

    static EmailMessage Parse(string value)
    {
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        return parsed.GetMessage()!;
    }

    static string Body(EmailMboxEntry entry)
    {
        return Text(entry.GetMessage()!.GetBody()!.GetContent()!.Materialize(10000));
    }

    static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }
    static string Text(byte[] value) { return Encoding.UTF8.GetString(value); }
}

public class MboxTrackingStream : MemoryStream
{
    int flushCount;
    bool closed;

    public MboxTrackingStream()
    {
        flushCount = 0;
        closed = false;
    }

    public int GetFlushCount() { return flushCount; }
    public bool IsClosed() { return closed; }

    public override void Flush()
    {
        flushCount++;
        base.Flush();
    }

    public override void Close()
    {
        closed = true;
        base.Close();
    }
}
