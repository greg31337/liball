using System.Text;
using email;

namespace email.Tests;

public class EmailParserTests
{
    [Test]
    public void ByteArrayParsingBuildsHeadersAndExactSourceRanges()
    {
        byte[] input = Bytes("Subject: hello\r\nX-Test: two\r\n\r\nbody");

        EmailParseResult result = new EmailParser().ParseWithInputUsingValueArray(input);
        EmailMessage message = result.GetMessage()!;
        EmailSourceInfo source = message!.GetSourceInfo()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(input.Length));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(input.Length));
        Assert.That(message.GetHeaders()!.GetCount(), Is.EqualTo(2));
        Assert.That(message.GetHeaders()!.GetAt(0).GetName(), Is.EqualTo("Subject"));
        Assert.That(message.HasBody(), Is.True);
        Assert.That(Text(message.GetBody()!.GetContent()!.Materialize(100)), Is.EqualTo("body"));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(source!.GetMessageRange()!.EqualsRange(new EmailRange(0, input.Length)), Is.True);
        Assert.That(source.GetHeaderBlockRange()!.EqualsRange(new EmailRange(0, 29)), Is.True);
        Assert.That(source.GetSeparatorRange()!.EqualsRange(new EmailRange(29, 2)), Is.True);
        Assert.That(source.GetBodyRange()!.EqualsRange(new EmailRange(31, 4)), Is.True);

        input[31] = (byte)'B';
        Assert.That(Text(source.GetContent()!.Materialize(100)), Does.EndWith("Body"));
    }

    [Test]
    public void ReadOnlyMemoryParsingRetainsAnIndependentOwnedSource()
    {
        byte[] input = Bytes("A: one\n\npayload");

        EmailParseResult result = new EmailParser().ParseWithInputUsingReadOnlyMemory(new ReadOnlyMemory<byte>(input));
        EmailContent source = result.GetMessage()!.GetSourceInfo()!.GetContent()!;
        input[8] = (byte)'X';

        Assert.That(source!.GetKind(), Is.EqualTo(EmailContentKind.Memory));
        Assert.That(source.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(Text(source.Materialize(100)), Is.EqualTo("A: one\n\npayload"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNonCanonicalLineEnding), Is.True);
    }

    [Test]
    public void SeekableStreamParsingStartsAtItsCurrentPositionAndBorrowsIt()
    {
        byte[] prefix = Bytes("skip:");
        byte[] messageBytes = Bytes("Subject: stream\r\n\r\nbody");
        MemoryStream stream = new MemoryStream(prefix.Concat(messageBytes).ToArray());
        stream.Position = prefix.Length;

        EmailParseResult result = new EmailParser().ParseWithInputUsingStream(stream);
        EmailContent source = result.GetMessage()!.GetSourceInfo()!.GetContent()!;

        Assert.That(result.GetSourceByteCount(), Is.EqualTo(messageBytes.Length));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(source!.GetKind(), Is.EqualTo(EmailContentKind.Stream));
        Assert.That(source.GetOwnership(), Is.EqualTo(EmailContentOwnership.Borrowed));
        Assert.That(Text(source.Materialize(100)), Is.EqualTo("Subject: stream\r\n\r\nbody"));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void NonSeekableStreamParsingCreatesARepeatableOwnedSource()
    {
        ParserNonSeekableStream stream = new ParserNonSeekableStream(Bytes("X-Test: value\r\n\r\ncontent"), 3);

        EmailParseResult result = new EmailParser().ParseWithInputUsingStream(stream);
        EmailContent source = result.GetMessage()!.GetSourceInfo()!.GetContent()!;

        Assert.That(source!.GetKind(), Is.EqualTo(EmailContentKind.Memory));
        Assert.That(source.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(source.IsRepeatable(), Is.True);
        Assert.That(Text(source.Materialize(100)), Is.EqualTo("X-Test: value\r\n\r\ncontent"));
        Assert.That(Text(source.Materialize(100)), Is.EqualTo("X-Test: value\r\n\r\ncontent"));
        Assert.That(stream.GetTotalBytesRead(), Is.EqualTo(result.GetSourceByteCount()));
    }

    [Test]
    public void NonPreservingParsingDoesNotRetainTheInputSource()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateConformingTolerant();

        EmailParseResult result = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Subject: transient\r\n\r\nbody"), options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetMessage()!.HasSourceInfo(), Is.False);
        Assert.That(result.GetMessage()!.GetHeaders()!.GetAt(0).GetDecodedValue(), Is.EqualTo("transient"));
        Assert.That(result.GetMessage()!.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
    }

    [Test]
    public void IncompleteInputReturnsAPartialMessageOnlyWhenPolicyAllowsIt()
    {
        byte[] input = Bytes("Subject: no separator\r\n");

        EmailParseResult tolerant = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.IsComplete(), Is.False);
        Assert.That(tolerant.IsSuccess(), Is.False);
        Assert.That(tolerant.HasMessage(), Is.True);
        Assert.That(tolerant.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(tolerant.GetMessage()!.GetSourceInfo()!.IsComplete(), Is.False);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
        Assert.That(strict.IsComplete(), Is.False);
        Assert.That(strict.HasMessage(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
    }

    [Test]
    public void MessageAndHeaderLimitsApplyAcrossBufferedAndStreamingInputs()
    {
        EmailParseOptions messageOptions = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits messageLimits = messageOptions.GetLimits();
        messageLimits.GetParsingLimits().SetMaxMessageBytes(5);
        messageOptions.SetLimits(messageLimits);
        EmailParseOptions headerOptions = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits headerLimits = headerOptions.GetLimits();
        headerLimits.GetParsingLimits().SetMaxHeaderBytes(4);
        headerOptions.SetLimits(headerLimits);
        EmailCompatibilityOptions headerCompatibility = headerOptions.GetCompatibilityOptions();
        headerCompatibility.SetReturnPartialResults(false);
        headerOptions.SetCompatibilityOptions(headerCompatibility);

        Assert.Throws<EmailLimitException>(() => new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("123456"), messageOptions));
        Assert.Throws<EmailLimitException>(() => new EmailParser().ParseWithInputAndOptionsUsingStreamAndEmailParseOptions(new ParserNonSeekableStream(Bytes("123456"), 2), messageOptions));
        Assert.Throws<EmailLimitException>(() => new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("A: value\r\n\r\n"), headerOptions));
    }

    [Test]
    public void TolerantParsingRetainsMalformedContentPrefixedHeadersWithoutApplyingMutationValidation()
    {
        byte[] input = Bytes("Content-").Concat(new byte[] { 1 }).Concat(Bytes(": value\r\n\r\nbody")).ToArray();

        EmailParseResult result = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
        Assert.That(result.GetMessage()!.GetHeaders()!.GetAt(0).GetNameBytes(), Is.EqualTo(Bytes("Content-").Concat(new byte[] { 1 })));
        Assert.That(Text(result.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(100)), Is.EqualTo("body"));
    }

    [Test]
    public void InvalidParserInputsAreRejected()
    {
        MemoryStream closed = new MemoryStream(Bytes("A:b\r\n\r\n"));
        closed.Close();

        Assert.Throws<ArgumentNullException>(() => new EmailParser().ParseWithInputUsingValueArray((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => new EmailParser().ParseWithInputUsingStream((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("A:b\r\n\r\n"), null!));
        Assert.Throws<ArgumentException>(() => new EmailParser().ParseWithInputUsingStream(closed));
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

public class ParserNonSeekableStream : Stream
{
    byte[] value;
    int maximumRead;
    int position;

    public ParserNonSeekableStream(byte[] value, int maximumRead)
    {
        this.value = value;
        this.maximumRead = maximumRead;
        position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public int GetTotalBytesRead()
    {
        return position;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (position >= value.Length)
        {
            return 0;
        }

        int available = value.Length - position;
        int read = Math.Min(Math.Min(count, maximumRead), available);
        Array.Copy(value, position, buffer, offset, read);
        position += read;
        return read;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
