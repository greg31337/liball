using token;

namespace token.Tests;

public class TokenBufferTests
{
    [Test]
    public void ReaderReadsAndSkipsSequentialBytes()
    {
        TokenBufferReader reader = new TokenBufferReader(new byte[] { 10, 20, 30, 40, 50 }, 5);

        TokenBytesResult first = reader.Read(2);
        TokenResult skipped = reader.Skip(1);
        TokenBytesResult last = reader.Read(2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Bytes, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(skipped.Success, Is.True);
            Assert.That(last.Success, Is.True);
            Assert.That(last.Bytes, Is.EqualTo(new byte[] { 40, 50 }));
            Assert.That(reader.GetLength(), Is.EqualTo(5));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
            Assert.That(reader.GetRemainingLength(), Is.Zero);
        });
    }

    [Test]
    public void ReaderFailureAtEndDoesNotAdvancePosition()
    {
        TokenBufferReader reader = new TokenBufferReader(new byte[] { 1, 2 }, 2);
        reader.Skip(1);

        TokenBytesResult result = reader.Read(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("unexpected_end_of_buffer"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
            Assert.That(reader.GetPosition(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReaderRejectsInputBeyondConfiguredLimit()
    {
        TokenBufferReader reader = new TokenBufferReader(new byte[] { 1, 2, 3 }, 2);

        TokenBytesResult read = reader.Read(1);
        TokenResult skip = reader.Skip(1);

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsLimitExceeded(), Is.True);
            Assert.That(read.Success, Is.False);
            Assert.That(read.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(read.Error.Position, Is.EqualTo(2));
            Assert.That(skip.Success, Is.False);
            Assert.That(skip.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ReaderSupportsEmptyReadsAtEnd()
    {
        TokenBufferReader reader = new TokenBufferReader(Array.Empty<byte>(), 0);

        TokenBytesResult result = reader.Read(0);
        TokenResult skipped = reader.Skip(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(skipped.Success, Is.True);
            Assert.That(reader.IsLimitExceeded(), Is.False);
        });
    }

    [Test]
    public void ReaderRejectsInvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new TokenBufferReader(null, 1), Throws.ArgumentNullException);
            Assert.That(() => new TokenBufferReader(Array.Empty<byte>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new TokenBufferReader(Array.Empty<byte>(), 0).Read(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new TokenBufferReader(Array.Empty<byte>(), 0).Skip(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void WriterAppendsBytesAndSpansAcrossBufferGrowth()
    {
        TokenBufferWriter writer = new TokenBufferWriter(40);
        byte[] middle = Enumerable.Range(1, 24).Select(value => (byte)value).ToArray();

        TokenResult first = writer.Write((byte)0);
        TokenResult second = writer.WriteBytes(middle);
        TokenResult third = writer.WriteSpan(new ReadOnlySpan<byte>(new byte[] { 25, 26 }));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(third.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(Enumerable.Range(0, 27).Select(value => (byte)value)));
            Assert.That(writer.GetLength(), Is.EqualTo(27));
            Assert.That(writer.GetRemainingCapacity(), Is.EqualTo(13));
        });
    }

    [Test]
    public void WriterLimitFailureIsAtomicAndAllowsLaterWrites()
    {
        TokenBufferWriter writer = new TokenBufferWriter(4);
        writer.WriteBytes(new byte[] { 1, 2 });

        TokenResult failed = writer.WriteBytes(new byte[] { 3, 4, 5 });
        TokenResult recovered = writer.WriteBytes(new byte[] { 3, 4 });

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(failed.Error.Position, Is.EqualTo(2));
            Assert.That(recovered.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(writer.GetRemainingCapacity(), Is.Zero);
        });
    }

    [Test]
    public void WriterSupportsZeroLimitAndEmptyWrites()
    {
        TokenBufferWriter writer = new TokenBufferWriter(0);

        TokenResult empty = writer.WriteBytes(Array.Empty<byte>());
        TokenResult full = writer.Write((byte)1);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(full.Success, Is.False);
            Assert.That(writer.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void WriterRejectsInvalidArguments()
    {
        TokenBufferWriter writer = new TokenBufferWriter(1);

        Assert.Multiple(() =>
        {
            Assert.That(() => new TokenBufferWriter(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => writer.WriteBytes((byte[])null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void WriterToArrayReturnsIndependentCopies()
    {
        TokenBufferWriter writer = new TokenBufferWriter(2);
        writer.WriteBytes(new byte[] { 1, 2 });

        byte[] first = writer.ToArray();
        first[0] = 9;

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
    }
}
