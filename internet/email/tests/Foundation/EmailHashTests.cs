using System.Text;
using email;

namespace email.Tests;

public class EmailHashTests
{
    [Test]
    public void IncrementalHashingStreamSupportsSegmentsAndExplicitLifecycle()
    {
        EmailHashingStream hashing = new EmailHashingStream(EmailHashAlgorithm.Sha256);
        byte[] source = Encoding.ASCII.GetBytes("xabcx");

        hashing.Write(ReadOnlySpan<byte>.Empty);
        hashing.WriteSegment(source, 1, 3);
        EmailHashResult result = hashing.Complete();
        hashing.Dispose();
        hashing.Close();

        Assert.That(hashing.GetByteCount(), Is.EqualTo(3));
        Assert.That(hashing.IsComplete(), Is.True);
        Assert.That(result.GetLowercaseHex(), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.Throws<InvalidOperationException>(() => hashing.Write(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => hashing.Complete());

        EmailHashingStream abandoned = new EmailHashingStream(EmailHashAlgorithm.Sha1);
        abandoned.Dispose();
        Assert.That(abandoned.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => abandoned.Write(Array.Empty<byte>()));

        EmailHashingStream validation = new EmailHashingStream(EmailHashAlgorithm.Sha512);
        Assert.Throws<ArgumentNullException>(() => validation.WriteSegment(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => validation.WriteSegment(new byte[1], -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => validation.WriteSegment(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => validation.WriteSegment(new byte[1], 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => validation.WriteSegment(new byte[1], 1, 1));
        validation.Close();
    }

    [Test]
    public void SpanHashingMatchesOfficialAbcVectors()
    {
        byte[] value = Encoding.ASCII.GetBytes("abc");

        Assert.That(
            new EmailHash().ComputeExactWithContentAndAlgorithm(value, EmailHashAlgorithm.Sha256).GetLowercaseHex(),
            Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.That(
            new EmailHash().ComputeExactWithContentAndAlgorithm(value, EmailHashAlgorithm.Sha384).GetLowercaseHex(),
            Is.EqualTo("cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7"));
        Assert.That(
            new EmailHash().ComputeExactWithContentAndAlgorithm(value, EmailHashAlgorithm.Sha512).GetLowercaseHex(),
            Is.EqualTo("ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f"));
    }

    [Test]
    public void EmptyHashAndDecodedSpanUseTheSameExactBytes()
    {
        EmailHashResult empty = new EmailHash().ComputeExactWithContentAndAlgorithm(Array.Empty<byte>(), EmailHashAlgorithm.Sha256);
        EmailHashResult decoded = new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithm(Encoding.ASCII.GetBytes("abc"), EmailHashAlgorithm.Sha256);

        Assert.That(empty.GetLowercaseHex(), Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        Assert.That(empty.GetHashedByteCount(), Is.Zero);
        Assert.That(decoded.GetLowercaseHex(), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.That(decoded.GetHashedByteCount(), Is.EqualTo(3));
    }

    [Test]
    public void ExactContentHashIsIndependentOfReadChunkSizes()
    {
        byte[] value = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        EmailHashResult expected = new EmailHash().ComputeExactWithContentAndAlgorithm(value, EmailHashAlgorithm.Sha256);
        int chunkSize = 1;
        while (chunkSize <= value.Length)
        {
            HashChunkedReadStream stream = new HashChunkedReadStream(value, chunkSize);
            EmailContent content = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);

            EmailHashResult actual = new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(content, EmailHashAlgorithm.Sha256, value.Length, 7);

            Assert.That(actual.GetLowercaseHex(), Is.EqualTo(expected.GetLowercaseHex()), "chunk size " + chunkSize);
            Assert.That(actual.GetHashedByteCount(), Is.EqualTo(value.Length));
            Assert.That(stream.CanRead, Is.True);
            content.Close();
            stream.Close();
            chunkSize++;
        }
    }

    [Test]
    public void ExactContentUsesCurrentFileBytesAndLeavesCleanupToContent()
    {
        string path = Path.Combine(Path.GetTempPath(), "email-hash-test-" + Guid.NewGuid().ToString("N") + ".bin");
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("abc"));
        try
        {
            EmailContent content = new EmailContentOperations().CreateTemporaryFile(path);

            EmailHashResult result = new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytes(content, EmailHashAlgorithm.Sha256, 3);

            Assert.That(result.GetLowercaseHex(), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            Assert.That(File.Exists(path), Is.True);
            content.Close();
            Assert.That(File.Exists(path), Is.False);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DecodedStreamHashesFromCurrentPositionWithoutClosingCallerStream()
    {
        byte[] value = Encoding.ASCII.GetBytes("prefixdecoded");
        MemoryStream stream = new MemoryStream(value);
        stream.Position = 6;

        EmailHashResult result = new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(stream, EmailHashAlgorithm.Sha384, 7, 2);
        EmailHashResult expected = new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithm(Encoding.ASCII.GetBytes("decoded"), EmailHashAlgorithm.Sha384);

        Assert.That(result.GetLowercaseHex(), Is.EqualTo(expected.GetLowercaseHex()));
        Assert.That(result.GetHashedByteCount(), Is.EqualTo(7));
        Assert.That(stream.CanRead, Is.True);
        stream.Close();
    }

    [Test]
    public void KnownLengthLimitFailsBeforeConsumingInput()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        stream.Position = 1;

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() =>
            new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytes(stream, EmailHashAlgorithm.Sha256, 2));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxHashBytes"));
        Assert.That(exception.GetLimit(), Is.EqualTo(2));
        Assert.That(exception.GetObserved(), Is.EqualTo(3));
        Assert.That(stream.Position, Is.EqualTo(1));
        stream.Close();
    }

    [Test]
    public void UnknownLengthLimitIsEnforcedWhileStreaming()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4, 5 });

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() =>
            new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(stream, EmailHashAlgorithm.Sha512, 3, 10));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxHashBytes"));
        Assert.That(exception.GetLimit(), Is.EqualTo(3));
        Assert.That(exception.GetObserved(), Is.EqualTo(5));
        Assert.That(stream.CanRead, Is.True);
        stream.Close();
    }

    [Test]
    public void HashResultDefensivelyCopiesAndComparesDigest()
    {
        EmailHashResult result = new EmailHash().ComputeExactWithContentAndAlgorithm(Encoding.ASCII.GetBytes("abc"), EmailHashAlgorithm.Sha256);
        byte[] digest = result.GetDigestBytes();
        byte[] matching = result.GetDigestBytes();
        digest[0] ^= 255;

        Assert.That(result.GetAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha256));
        Assert.That(result.GetDigestByteCount(), Is.EqualTo(32));
        Assert.That(result.Matches(matching), Is.True);
        Assert.That(result.Matches(digest), Is.False);
        Assert.That(result.Matches(Array.Empty<byte>()), Is.False);
        Assert.That(result.GetLowercaseHex(), Does.StartWith("ba7816bf"));
    }

    [Test]
    public void HashResultValidatesAlgorithmDigestAndCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHashResult((EmailHashAlgorithm)10, new byte[32], 0));
        Assert.Throws<ArgumentNullException>(() => new EmailHashResult(EmailHashAlgorithm.Sha256, null!, 0));
        Assert.Throws<ArgumentException>(() => new EmailHashResult(EmailHashAlgorithm.Sha256, new byte[31], 0));
        Assert.Throws<ArgumentException>(() => new EmailHashResult(EmailHashAlgorithm.Sha384, new byte[32], 0));
        Assert.Throws<ArgumentException>(() => new EmailHashResult(EmailHashAlgorithm.Sha512, new byte[32], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHashResult(EmailHashAlgorithm.Sha256, new byte[32], -1));
    }

    [Test]
    public void HashEntryPointsValidateArguments()
    {
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(Array.Empty<byte>(), false);
        MemoryStream closed = new MemoryStream();
        closed.Close();

        Assert.Throws<ArgumentNullException>(() => new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytes((EmailContent)null!, EmailHashAlgorithm.Sha256, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytes(content, (EmailHashAlgorithm)10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytes(content, EmailHashAlgorithm.Sha256, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(content, EmailHashAlgorithm.Sha256, 0, 0));
        Assert.Throws<ArgumentNullException>(() => new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytes((Stream)null!, EmailHashAlgorithm.Sha256, 0));
        Assert.Throws<ArgumentException>(() => new EmailHash().ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytes(closed, EmailHashAlgorithm.Sha256, 0));
        content.Close();
    }
}

public class HashChunkedReadStream : MemoryStream
{
    int maximumChunkBytes;

    public HashChunkedReadStream(byte[] value, int maximumChunkBytes) : base(value, false)
    {
        this.maximumChunkBytes = maximumChunkBytes;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int limitedCount = count < maximumChunkBytes ? count : maximumChunkBytes;
        return base.Read(buffer, offset, limitedCount);
    }
}
