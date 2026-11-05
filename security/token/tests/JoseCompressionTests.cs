using System.IO.Compression;
using System.Text;
using token;

namespace token.Tests;

public class JoseCompressionTests
{
    [Test]
    public void DecompressMatchesRfc7520Section59Vector()
    {
        byte[] expected = Encoding.UTF8.GetBytes(
            "You can trust us to stick with you through thick and "
            + "thin–to the bitter end. And you can trust us to "
            + "keep any secret of yours–closer than you keep it "
            + "yourself. But you cannot trust us to let you face trouble "
            + "alone, and go off without a word. We are your friends, Frodo.");
        byte[] compressed = Decode(
            "bY_BDcIwDEVX-QNU3QEOrIA4pqlDokYxchxVvbEDGzIJbioOSJwc-f___HPjBu"
            + "8KVFpVtAplVE1-wZo0YjNZo3C7R5v72pV5f5X382VWjYQpqZKAyjziZOr2B7kQ"
            + "PSy6oZIXUnDYbVKN4jNXi2u0yB7t1qSHTjmMODf9QgvrDzfTIQXnyQRuUya4zI"
            + "WG3vTOdir0v7BRHFYWq3k1k1A_gSDJqtcBF-GZxw8");

        TokenBytesResult result = JoseCompression.Decompress(
            compressed,
            JoseCompressionAlgorithms.Deflate,
            EnabledPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void CompressAndDecompressInteroperateWithDeflateStream()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "Compressible plaintext. Compressible plaintext. "
            + "Compressible plaintext.");
        TokenPolicy policy = EnabledPolicy();

        TokenBytesResult compressed = JoseCompression.Compress(
            plaintext,
            JoseCompressionAlgorithms.Deflate,
            policy);
        byte[] platformPlaintext;
        using (MemoryStream input = new MemoryStream(compressed.Bytes))
        using (DeflateStream decompressor = new DeflateStream(
            input,
            CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            decompressor.CopyTo(output);
            platformPlaintext = output.ToArray();
        }

        byte[] platformCompressed;
        using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream compressor = new DeflateStream(
                output,
                CompressionLevel.SmallestSize,
                true))
            {
                compressor.Write(plaintext);
            }

            platformCompressed = output.ToArray();
        }

        TokenBytesResult decompressed = JoseCompression.Decompress(
            platformCompressed,
            JoseCompressionAlgorithms.Deflate,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(compressed);
            Assert.That(platformPlaintext, Is.EqualTo(plaintext));
            AssertSuccess(decompressed);
            Assert.That(decompressed.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void EmptyPlaintextRoundTrips()
    {
        TokenPolicy policy = EnabledPolicy();

        TokenBytesResult compressed = JoseCompression.Compress(
            Array.Empty<byte>(),
            JoseCompressionAlgorithms.Deflate,
            policy);
        TokenBytesResult decompressed = JoseCompression.Decompress(
            compressed.Bytes,
            JoseCompressionAlgorithms.Deflate,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(compressed);
            Assert.That(compressed.Bytes, Is.Empty);
            AssertSuccess(decompressed);
            Assert.That(decompressed.Bytes, Is.Empty);
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        byte[] plaintext = Container(Encoding.UTF8.GetBytes(
            "selected span selected span selected span"));
        TokenPolicy policy = EnabledPolicy();

        TokenBytesResult compressed = JoseCompression.Compress(
            plaintext.AsSpan(1, plaintext.Length - 2),
            JoseCompressionAlgorithms.Deflate,
            policy);
        byte[] compressedContainer = Container(compressed.Bytes);
        TokenBytesResult decompressed = JoseCompression.Decompress(
            compressedContainer.AsSpan(1, compressed.Bytes.Length),
            JoseCompressionAlgorithms.Deflate,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(decompressed);
            Assert.That(decompressed.Bytes, Is.EqualTo(plaintext[1..^1]));
            Assert.That(plaintext[0], Is.Zero);
            Assert.That(plaintext[^1], Is.Zero);
            Assert.That(compressedContainer[0], Is.Zero);
            Assert.That(compressedContainer[^1], Is.Zero);
        });
    }

    [Test]
    public void CompressionIsDisabledByDefault()
    {
        TokenPolicy disabled = new TokenPolicy();
        byte[] published = Decode("AwA");

        Assert.Multiple(() =>
        {
            AssertFailure(JoseCompression.Compress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                disabled), "compression_disabled");
            AssertFailure(JoseCompression.Decompress(
                published,
                JoseCompressionAlgorithms.Deflate,
                disabled), "compression_disabled");
        });
    }

    [Test]
    public void AbsoluteExpansionLimitStopsDecompression()
    {
        byte[] plaintext = Repeated(4096, 65);
        TokenPolicy permissive = EnabledPolicy();
        permissive.MaximumCompressionRatio = 1000;
        TokenBytesResult compressed = JoseCompression.Compress(
            plaintext,
            JoseCompressionAlgorithms.Deflate,
            permissive);
        TokenPolicy limited = EnabledPolicy();
        limited.MaximumDecompressedSizeInBytes = 1024;
        limited.MaximumCompressionRatio = 1000;

        TokenBytesResult result = JoseCompression.Decompress(
            compressed.Bytes,
            JoseCompressionAlgorithms.Deflate,
            limited);

        AssertFailure(result, "decompression_limit_exceeded");
    }

    [Test]
    public void CompressedInputLimitStopsDecompression()
    {
        TokenPolicy policy = EnabledPolicy();
        policy.MaximumTokenSizeInBytes = 3;

        TokenBytesResult result = JoseCompression.Decompress(
            new byte[] { 1, 2, 3, 4 },
            JoseCompressionAlgorithms.Deflate,
            policy);

        AssertFailure(result, "decompression_limit_exceeded");
    }

    [Test]
    public void CompressionRatioLimitStopsDecompression()
    {
        byte[] plaintext = Repeated(4096, 65);
        TokenPolicy permissive = EnabledPolicy();
        permissive.MaximumCompressionRatio = 1000;
        TokenBytesResult compressed = JoseCompression.Compress(
            plaintext,
            JoseCompressionAlgorithms.Deflate,
            permissive);
        TokenPolicy limited = EnabledPolicy();
        limited.MaximumCompressionRatio = 2;

        TokenBytesResult result = JoseCompression.Decompress(
            compressed.Bytes,
            JoseCompressionAlgorithms.Deflate,
            limited);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Bytes.Length * 2, Is.LessThan(plaintext.Length));
            AssertFailure(result, "decompression_limit_exceeded");
        });
    }

    [Test]
    public void ExactExpansionLimitIsAccepted()
    {
        byte[] plaintext = Repeated(1024, 90);
        TokenPolicy permissive = EnabledPolicy();
        permissive.MaximumCompressionRatio = 1000;
        TokenBytesResult compressed = JoseCompression.Compress(
            plaintext,
            JoseCompressionAlgorithms.Deflate,
            permissive);
        TokenPolicy exact = EnabledPolicy();
        exact.MaximumDecompressedSizeInBytes = plaintext.Length;
        exact.MaximumCompressionRatio = 1000;

        TokenBytesResult result = JoseCompression.Decompress(
            compressed.Bytes,
            JoseCompressionAlgorithms.Deflate,
            exact);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void CompressionInputAndOutputLimitsAreEnforced()
    {
        TokenPolicy inputLimited = EnabledPolicy();
        inputLimited.MaximumDecompressedSizeInBytes = 15;
        TokenPolicy outputLimited = EnabledPolicy();
        outputLimited.MaximumTokenSizeInBytes = 2;

        Assert.Multiple(() =>
        {
            AssertFailure(JoseCompression.Compress(
                Sequence(16),
                JoseCompressionAlgorithms.Deflate,
                inputLimited), "compression_limit_exceeded");
            AssertFailure(JoseCompression.Compress(
                Sequence(256),
                JoseCompressionAlgorithms.Deflate,
                outputLimited), "compression_limit_exceeded");
        });
    }

    [Test]
    public void InvalidCompressedDataReturnsStableFailure()
    {
        TokenPolicy policy = EnabledPolicy();

        Assert.Multiple(() =>
        {
            TokenBytesResult empty = JoseCompression.Decompress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                policy);
            AssertSuccess(empty);
            Assert.That(empty.Bytes, Is.Empty);
            AssertFailure(JoseCompression.Decompress(
                new byte[] { 255, 255, 255, 255 },
                JoseCompressionAlgorithms.Deflate,
                policy), "decompression_failed");
        });
    }

    [Test]
    public void InvalidPoliciesAndAlgorithmsReturnStableFailures()
    {
        TokenPolicy invalidTokenLimit = EnabledPolicy();
        invalidTokenLimit.MaximumTokenSizeInBytes = 0;
        TokenPolicy invalidOutputLimit = EnabledPolicy();
        invalidOutputLimit.MaximumDecompressedSizeInBytes = 0;
        TokenPolicy invalidRatio = EnabledPolicy();
        invalidRatio.MaximumCompressionRatio = 0;

        Assert.Multiple(() =>
        {
            AssertFailure(JoseCompression.Compress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                invalidTokenLimit), "invalid_compression_policy");
            AssertFailure(JoseCompression.Compress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                invalidOutputLimit), "invalid_compression_policy");
            AssertFailure(JoseCompression.Compress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                invalidRatio), "invalid_compression_policy");
            AssertFailure(JoseCompression.Compress(
                Array.Empty<byte>(),
                "def",
                EnabledPolicy()), "unknown_compression_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenPolicy policy = EnabledPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(() => JoseCompression.Compress(
                null,
                JoseCompressionAlgorithms.Deflate,
                policy), Throws.ArgumentNullException);
            Assert.That(() => JoseCompression.Compress(
                Array.Empty<byte>(),
                null,
                policy), Throws.ArgumentNullException);
            Assert.That(() => JoseCompression.Compress(
                Array.Empty<byte>(),
                JoseCompressionAlgorithms.Deflate,
                null), Throws.ArgumentNullException);
            Assert.That(() => JoseCompression.Decompress(
                null,
                JoseCompressionAlgorithms.Deflate,
                policy), Throws.ArgumentNullException);
            Assert.That(() => JoseCompression.Decompress(
                new byte[] { 3, 0 },
                null,
                policy), Throws.ArgumentNullException);
            Assert.That(() => JoseCompression.Decompress(
                new byte[] { 3, 0 },
                JoseCompressionAlgorithms.Deflate,
                null), Throws.ArgumentNullException);
        });
    }

    static TokenPolicy EnabledPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowCompression = true;
        return policy;
    }

    static byte[] Decode(string text)
    {
        TokenBytesResult result = Base64Url.Decode(text);
        Assert.That(result.Success, Is.True);
        return result.Bytes;
    }

    static byte[] Sequence(int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)index;
            index += 1;
        }

        return result;
    }

    static byte[] Repeated(int length, byte value)
    {
        byte[] result = new byte[length];
        Array.Fill(result, value);
        return result;
    }

    static byte[] Container(byte[] value)
    {
        byte[] result = new byte[value.Length + 2];
        value.CopyTo(result, 1);
        return result;
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.Not.Null);
        });
    }

    static void AssertFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
