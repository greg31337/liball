using System.Text;
using email;

namespace email.Tests;

public class EmailMboxReaderTests
{
    [Test]
    public void ReadsEntriesWithEnvelopeMetadataAbsoluteOffsetsAndMboxrdUnescaping()
    {
        const string firstEnvelope = "From sender@example.test Fri Nov 21 09:55:06 1997\n";
        const string firstMessage = "Subject: first\n\n>From escaped\n>>From quoted\nend\n";
        const string secondEnvelope = "From other@example.test Sat Nov 22 10:00:00 1997\n";
        const string secondMessage = "Subject: second\n\nsecond body\n";
        byte[] source = Bytes(firstEnvelope + firstMessage + secondEnvelope + secondMessage);
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(source, false));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry first = reader.GetCurrent()!;
        long secondOffset = Bytes(firstEnvelope + firstMessage).LongLength;

        Assert.That(first!.GetIndex(), Is.Zero);
        Assert.That(first.GetFormat(), Is.EqualTo(EmailMboxFormat.Mboxrd));
        Assert.That(first.GetArchiveRange().EqualsRange(new EmailRange(0, secondOffset)), Is.True);
        Assert.That(first.GetEnvelopeLineRange()!.EqualsRange(new EmailRange(0, Bytes(firstEnvelope).Length)), Is.True);
        Assert.That(first.GetMessageRange().EqualsRange(new EmailRange(Bytes(firstEnvelope).Length, Bytes(firstMessage).Length)), Is.True);
        Assert.That(Text(first.GetEnvelopeLine()!), Is.EqualTo(firstEnvelope));
        Assert.That(first.GetEnvelopeSenderText(), Is.EqualTo("sender@example.test"));
        Assert.That(first.GetEnvelopeDateText(), Is.EqualTo("Fri Nov 21 09:55:06 1997"));
        Assert.That(first.GetMessage()!.GetEnvelope()!.HasMboxFromLine(), Is.True);
        Assert.That(first.GetMessage()!.GetEnvelope()!.GetSender()!.GetLocalPart(), Is.EqualTo("sender"));
        Assert.That(first.GetMessage()!.GetEnvelope()!.GetSender()!.GetDomain(), Is.EqualTo("example.test"));
        Assert.That(first.GetMessage()!.GetEnvelope()!.HasMboxFromLineRange(), Is.False);
        Assert.That(Body(first), Is.EqualTo("From escaped\n>From quoted\nend\n"));
        Assert.That(first.GetUnescapedFromLineCount(), Is.EqualTo(2));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry second = reader.GetCurrent()!;
        Assert.That(second!.GetIndex(), Is.EqualTo(1));
        Assert.That(second.GetArchiveRange().EqualsRange(new EmailRange(secondOffset, source.LongLength - secondOffset)), Is.True);
        Assert.That(second.GetMessageRange().GetStartOffset(), Is.EqualTo(secondOffset + Bytes(secondEnvelope).Length));
        Assert.That(Body(second), Is.EqualTo("second body\n"));
        Assert.That(reader.IsComplete(), Is.True);
        Assert.That(reader.ReadNext(), Is.False);
        Assert.That(reader.GetEntryCount(), Is.EqualTo(2));
        Assert.That(reader.GetConsumedByteCount(), Is.EqualTo(source.LongLength));
    }

    [Test]
    public void MboxoOnlyUnescapesOneQuotedFormWhileMboxrdIsReversible()
    {
        const string source = "From sender@example.test Fri Nov 21 09:55:06 1997\nSubject: quoting\n\n>From plain\n>>From already quoted\n";
        EmailMboxReaderOptions mboxoOptions = new EmailMboxReaderOptions();
        mboxoOptions.SetFormat(EmailMboxFormat.Mboxo);
        EmailMboxReader mboxo = new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(Bytes(source), false), mboxoOptions);
        EmailMboxReader mboxrd = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(Bytes(source), false));

        Assert.That(mboxo.ReadNext(), Is.True);
        Assert.That(mboxrd.ReadNext(), Is.True);

        Assert.That(Body(mboxo.GetCurrent()!), Is.EqualTo("From plain\n>>From already quoted\n"));
        Assert.That(mboxo.GetCurrent()!.GetUnescapedFromLineCount(), Is.EqualTo(1));
        Assert.That(Body(mboxrd.GetCurrent()!), Is.EqualTo("From plain\n>From already quoted\n"));
        Assert.That(mboxrd.GetCurrent()!.GetUnescapedFromLineCount(), Is.EqualTo(2));
    }

    [Test]
    public void QuotedFromLinesAreOnlyDecodedAfterTheHeaderBodySeparator()
    {
        const string envelope = "From sender@example.test Fri Nov 21 09:55:06 1997\n";
        const string message = "Subject: first\n>From malformed header line\n\n>From body line\n";
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(Bytes(envelope + message), false));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry entry = reader.GetCurrent()!;
        string logicalSource = Text(entry!.GetMessage()!.GetSourceInfo()!.GetContent()!.Materialize(1000));

        Assert.That(logicalSource, Does.Contain(">From malformed header line"));
        Assert.That(logicalSource, Does.Contain("\nFrom body line\n"));
        Assert.That(entry.GetUnescapedFromLineCount(), Is.EqualTo(1));
    }

    [Test]
    public void RecoversLeadingMessageWithoutEnvelopeAndContinuesAtTheNextEntry()
    {
        const string recovered = "Subject: recovered\n\nfirst body\n";
        const string envelope = "From next@example.test Fri Nov 21 09:55:06 1997\n";
        const string next = "Subject: next\n\nnext body\n";
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(Bytes(recovered + envelope + next), false));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry first = reader.GetCurrent()!;
        Assert.That(first!.WasRecovered(), Is.True);
        Assert.That(first.HasEnvelopeLine(), Is.False);
        Assert.That(first.GetMessageRange().EqualsRange(new EmailRange(0, Bytes(recovered).Length)), Is.True);
        Assert.That(first.GetIssues().ContainsCode(EmailIssueCode.MboxMessageRecovered), Is.True);
        Assert.That(first.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("recovered"));

        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetCurrent()!.WasRecovered(), Is.False);
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("next"));
    }

    [Test]
    public void MalformedEnvelopeAndMessageRemainInspectableWithoutBlockingLaterEntries()
    {
        const string malformed = "From broken\nBroken header\n";
        const string valid = "From valid@example.test Fri Nov 21 09:55:06 1997\nSubject: valid\n\nbody\n";
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(Bytes(malformed + valid), false));

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry first = reader.GetCurrent()!;
        Assert.That(first!.HasEnvelopeLine(), Is.True);
        Assert.That(first.GetIssues().ContainsCode(EmailIssueCode.MboxEnvelopeMalformed), Is.True);
        Assert.That(first.GetIssues().ContainsCode(EmailIssueCode.MboxMessageMalformed), Is.True);
        Assert.That(first.IsSuccess(), Is.False);

        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry second = reader.GetCurrent()!;
        Assert.That(second!.IsSuccess(), Is.True);
        Assert.That(second.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("valid"));
        Assert.That(reader.GetIssues().ContainsCode(EmailIssueCode.MboxMessageMalformed), Is.True);
    }

    [Test]
    public void ReadsNonSeekableInputIncrementallyAndHonorsStreamOwnership()
    {
        string first = "From first@example.test Fri Nov 21 09:55:06 1997\nSubject: first\n\n" + new string('a', 200) + "\n";
        string second = "From second@example.test Sat Nov 22 10:00:00 1997\nSubject: second\n\n" + new string('b', 400) + "\n";
        byte[] source = Bytes(first + second);
        TrackingChunkStream stream = new TrackingChunkStream(source, 7);
        EmailMboxReaderOptions options = new EmailMboxReaderOptions();
        options.SetReadBufferBytes(11);
        options.SetLeaveOpen(false);
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInputAndOptions(stream, options);

        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetReadByteCount(), Is.LessThan(source.LongLength));
        Assert.That(stream.GetMaximumRequestedBytes(), Is.LessThanOrEqualTo(11));
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("first"));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.IsComplete(), Is.True);
        reader.Close();
        Assert.That(stream.IsClosed(), Is.True);
        Assert.Throws<ObjectDisposedException>(() => reader.ReadNext());
    }

    [Test]
    public void EnforcesArchiveEntryAndEntryCountLimits()
    {
        byte[] source = Bytes(
            "From one@example.test Fri Nov 21 09:55:06 1997\nSubject: one\n\nbody\n"
            + "From two@example.test Sat Nov 22 10:00:00 1997\nSubject: two\n\nbody\n");

        EmailMboxReaderOptions archiveOptions = OptionsWithLimits(source.LongLength - 1, source.LongLength, 2);
        EmailMboxReader archiveReader = new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(source, false), archiveOptions);
        EmailLimitException archive = Assert.Throws<EmailLimitException>(() =>
        {
            while (archiveReader.ReadNext()) { }
        });

        EmailMboxReaderOptions entryOptions = OptionsWithLimits(source.LongLength, 20, 2);
        EmailLimitException entry = Assert.Throws<EmailLimitException>(() => new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(source, false), entryOptions).ReadNext());

        EmailMboxReaderOptions countOptions = OptionsWithLimits(source.LongLength, source.LongLength, 1);
        EmailMboxReader countReader = new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(source, false), countOptions);
        Assert.That(countReader.ReadNext(), Is.True);
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => countReader.ReadNext());

        Assert.That(archive.GetLimitName(), Is.EqualTo("MaxMboxArchiveBytes"));
        Assert.That(entry.GetLimitName(), Is.EqualTo("MaxMboxEntryBytes"));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxMboxEntryCount"));
    }

    [Test]
    public void SupportsCrLfLfAndBareCrLinesAndDefensivelySnapshotsOptions()
    {
        const string source = "From first@example.test Fri Nov 21 09:55:06 1997\rSubject: first\r\rbody\r"
            + "From second@example.test Sat Nov 22 10:00:00 1997\r\nSubject: second\n\nbody\n";
        EmailMboxReaderOptions supplied = new EmailMboxReaderOptions();
        supplied.SetFormat(EmailMboxFormat.Mboxo);
        supplied.SetReadBufferBytes(3);
        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInputAndOptions(new MemoryStream(Bytes(source), false), supplied);
        supplied.SetFormat(EmailMboxFormat.Mboxrd);
        supplied.SetReadBufferBytes(100);

        Assert.That(reader.GetOptions().GetFormat(), Is.EqualTo(EmailMboxFormat.Mboxo));
        Assert.That(reader.GetOptions().GetReadBufferBytes(), Is.EqualTo(3));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("first"));
        Assert.That(reader.ReadNext(), Is.True);
        Assert.That(reader.GetCurrent()!.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("second"));
        Assert.That(reader.GetConsumedByteCount(), Is.EqualTo(Bytes(source).LongLength));

        Assert.Throws<ArgumentOutOfRangeException>(() => supplied.SetFormat((EmailMboxFormat)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => supplied.SetReadBufferBytes(0));
        Assert.Throws<ArgumentNullException>(() => new EmailMboxReaderOperations().CreateWithInput(null!));
    }

    static EmailMboxReaderOptions OptionsWithLimits(long archiveBytes, long entryBytes, int entryCount)
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        limits.SetMaxArchiveBytes(archiveBytes);
        limits.SetMaxEntryBytes(entryBytes);
        limits.SetMaxEntryCount(entryCount);
        EmailMboxReaderOptions options = new EmailMboxReaderOptions();
        options.SetLimits(limits);
        options.SetReadBufferBytes(8);
        return options;
    }

    static string Body(EmailMboxEntry entry)
    {
        return Text(entry.GetMessage()!.GetBody()!.GetContent()!.Materialize(10000));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}

public class TrackingChunkStream : Stream
{
    byte[] source;
    int offset;
    int maximumChunk;
    int maximumRequested;
    bool closed;

    public TrackingChunkStream(byte[] source, int maximumChunk)
    {
        this.source = source;
        this.maximumChunk = maximumChunk;
        offset = 0;
        maximumRequested = 0;
        closed = false;
    }

    public int GetMaximumRequestedBytes() { return maximumRequested; }
    public bool IsClosed() { return closed; }
    public override bool CanRead { get { return !closed; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
    public override void Flush() { }

    public override int Read(byte[] buffer, int bufferOffset, int count)
    {
        if (closed) throw new ObjectDisposedException("TrackingChunkStream");
        if (count > maximumRequested) maximumRequested = count;
        if (offset >= source.Length) return 0;
        int take = Math.Min(count, Math.Min(maximumChunk, source.Length - offset));
        Array.Copy(source, offset, buffer, bufferOffset, take);
        offset += take;
        return take;
    }

    public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

    protected override void Dispose(bool disposing)
    {
        closed = true;
        base.Dispose(disposing);
    }
}
