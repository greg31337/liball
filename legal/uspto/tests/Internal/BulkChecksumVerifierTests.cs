using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkChecksumVerifierTests
{
    [Test]
    public void CalculateAlwaysStreamsSha256EvenWithoutCatalogExpectation()
    {
        byte[] body = Encoding.ASCII.GetBytes("abc");
        ChecksumReadStream input = new ChecksumReadStream(body, 1);

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, Array.Empty<ChecksumExpectation>(), 2, CancellationToken.None).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Expectation.Algorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
            Assert.That(result.Expectation.Value, Is.Null);
            Assert.That(result.ActualValue, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(input.ReadCount, Is.GreaterThan(1));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void UppercaseHexExpectationIsNormalizedAndMatched()
    {
        byte[] body = Encoding.UTF8.GetBytes("USPTO checksum");
        string expected = Convert.ToHexString(SHA256.HashData(body));
        using MemoryStream input = new MemoryStream(body);

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = expected }
        }, 3, CancellationToken.None).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Expectation.Value, Is.EqualTo(expected.ToLowerInvariant()));
            Assert.That(result.ActualValue, Is.EqualTo(expected.ToLowerInvariant()));
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMatch, Is.True);
        });
    }

    [Test]
    public void Base64ExpectationIsNormalizedToHexAndMatched()
    {
        byte[] body = Encoding.UTF8.GetBytes("base64 digest");
        byte[] digest = SHA256.HashData(body);
        string expectedBase64 = Convert.ToBase64String(digest);
        using MemoryStream input = new MemoryStream(body);

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = expectedBase64 }
        }, 4, CancellationToken.None).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Expectation.Value, Is.EqualTo(Convert.ToHexString(digest).ToLowerInvariant()));
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMatch, Is.True);
        });
    }

    [Test]
    public void CatalogAlgorithmsAreCalculatedAlongsideImplicitSha256()
    {
        byte[] body = Encoding.ASCII.GetBytes("catalog algorithms");
        string md5 = Convert.ToHexString(MD5.HashData(body));
        string sha1 = Convert.ToBase64String(SHA1.HashData(body));
        using MemoryStream input = new MemoryStream(body);

        ChecksumResult[] results = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Md5, Value = md5 },
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha1, Value = sha1 }
        }, 5, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(item => item.Expectation.Algorithm), Is.EqualTo(new ChecksumAlgorithm[]
            {
                ChecksumAlgorithm.Md5,
                ChecksumAlgorithm.Sha1,
                ChecksumAlgorithm.Sha256
            }));
            Assert.That(results[0].IsMatch, Is.True);
            Assert.That(results[1].IsMatch, Is.True);
            Assert.That(results[2].IsAvailable, Is.False);
            Assert.That(results[2].IsComplete, Is.True);
            Assert.That(results[2].ActualValue, Has.Length.EqualTo(64));
        });
    }

    [TestCase(ChecksumAlgorithm.Sha256, "not-a-digest")]
    [TestCase(ChecksumAlgorithm.None, "0011")]
    public void InvalidOrUnsupportedExpectationIsExplicitlyUnavailable(ChecksumAlgorithm algorithm, string value)
    {
        using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes("content"));

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = algorithm, Value = value }
        }, 8, CancellationToken.None).First();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.IsComplete, Is.EqualTo(algorithm != ChecksumAlgorithm.None));
        });
    }

    [Test]
    public void CompletedMismatchingDigestIsExplicitlyReported()
    {
        using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes("actual"));
        string different = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("different")));

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = different }
        }, 2, CancellationToken.None).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.ActualValue, Is.Not.EqualTo(result.Expectation.Value));
            Assert.That(BulkChecksumVerifier.HasMismatch(new ChecksumResult[] { result }), Is.True);
        });
    }

    [Test]
    public void InterruptedSessionReportsAvailableChecksumAsIncomplete()
    {
        byte[] body = Encoding.ASCII.GetBytes("partial bytes");
        string expected = Convert.ToHexString(SHA256.HashData(body));
        using BulkChecksumSession session = new BulkChecksumSession(new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = expected }
        });
        session.Append(body, 0, 4);

        ChecksumResult result = session.MarkIncomplete().Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.ActualValue, Is.Null);
        });
    }

    [Test]
    public void LargeSyntheticStreamIsHashedIncrementallyWithTheConfiguredBuffer()
    {
        const long length = 10L * 1024L * 1024L + 123L;
        const int bufferSize = 4093;
        string expected = CalculateRepeatedByteSha256(length, 0x5a);
        SyntheticChecksumStream input = new SyntheticChecksumStream(length, 0x5a);

        ChecksumResult result = BulkChecksumVerifier.Calculate(input, new ChecksumExpectation[]
        {
            new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = expected }
        }, bufferSize, CancellationToken.None).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.ActualValue, Is.EqualTo(expected));
            Assert.That(input.BytesRead, Is.EqualTo(length));
            Assert.That(input.MaximumRequestedCount, Is.LessThanOrEqualTo(bufferSize));
            Assert.That(input.ReadCount, Is.GreaterThan(2000));
        });
    }

    [Test]
    public void CancellationAfterHashingBeginsStopsBeforeReadingMoreInput()
    {
        byte[] body = Encoding.ASCII.GetBytes("cancel after the first checksum block");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CancellingChecksumReadStream input = new CancellingChecksumReadStream(body, cancellation);

        Assert.Throws<OperationCanceledException>(() => BulkChecksumVerifier.Calculate(input, Array.Empty<ChecksumExpectation>(), 4, cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(input.ReadCount, Is.EqualTo(1));
            Assert.That(input.Position, Is.EqualTo(4L));
        });
    }

    private static string CalculateRepeatedByteSha256(long length, byte value)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[8192];
        Array.Fill(buffer, value);
        long remaining = length;
        while (remaining != 0L)
        {
            int count = remaining < buffer.Length ? (int)remaining : buffer.Length;
            hash.AppendData(buffer, 0, count);
            remaining -= count;
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}

internal class ChecksumReadStream : MemoryStream
{
    internal int MaximumChunkSize;
    internal int ReadCount = 0;

    internal ChecksumReadStream(byte[] body, int maximumChunkSize)
        : base(body, false)
    {
        MaximumChunkSize = maximumChunkSize;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count > MaximumChunkSize)
        {
            count = MaximumChunkSize;
        }

        ReadCount += 1;
        return base.Read(buffer, offset, count);
    }
}

internal class CancellingChecksumReadStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;
    internal int ReadCount = 0;

    internal CancellingChecksumReadStream(byte[] body, CancellationTokenSource cancellation)
        : base(body, false)
    {
        Cancellation = cancellation;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        ReadCount += 1;
        Cancellation.Cancel();
        return read;
    }
}

internal class SyntheticChecksumStream : Stream
{
    internal long Remaining;
    internal byte Value;
    internal long BytesRead = 0L;
    internal int MaximumRequestedCount = 0;
    internal int ReadCount = 0;

    internal SyntheticChecksumStream(long length, byte value)
    {
        Remaining = length;
        Value = value;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => BytesRead + Remaining;

    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCount += 1;
        if (count > MaximumRequestedCount)
        {
            MaximumRequestedCount = count;
        }

        int read = Remaining < count ? (int)Remaining : count;
        Array.Fill(buffer, Value, offset, read);
        Remaining -= read;
        BytesRead += read;
        return read;
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
