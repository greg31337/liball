using raw;

namespace raw.Tests;

public class DeflateTests
{
    [TestCase(RawCompression.Deflate)]
    [TestCase(RawCompression.AdobeDeflate)]
    public void TiffIdentifiersProduceCompleteZlibStreams(RawCompression compression)
    {
        byte[] source = System.Text.Encoding.ASCII.GetBytes("TIFF Deflate and Adobe Deflate both use a zlib wrapper.");
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.None, source.Length, 1, 1, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);

        byte[] encoded = DeflateCodec.EncodeTiff(source, compression, parameters, new RawLimits(), CancellationToken.None);
        byte[] independentlyDecoded = Inflate(encoded, true);
        byte[] decoded = DeflateCodec.DecodeTiff(encoded, compression, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[0] & 0x0f, Is.EqualTo(8));
            Assert.That((encoded[0] * 256 + encoded[1]) % 31, Is.Zero);
            Assert.That(independentlyDecoded, Is.EqualTo(source));
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void KnownZlibVectorDecodesWithoutDependingOnTheEncoder()
    {
        byte[] encoded = new byte[] { 0x78, 0x9c, 0xcb, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00, 0x06, 0x2c, 0x02, 0x15 };

        byte[] decoded = DeflateCodec.Decode(encoded, 5, true, new RawLimits(), CancellationToken.None);

        Assert.That(System.Text.Encoding.ASCII.GetString(decoded), Is.EqualTo("hello"));
    }

    [Test]
    public void RawDeflateUsesDeflateStreamWithoutAZlibWrapper()
    {
        byte[] source = Enumerable.Range(0, 4096).Select(value => (byte)(value * 37)).ToArray();

        byte[] raw = DeflateCodec.Encode(source, false, new RawLimits(), CancellationToken.None);
        byte[] zlib = DeflateCodec.Encode(source, true, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(Inflate(raw, false), Is.EqualTo(source));
            Assert.That(DeflateCodec.Decode(raw, source.Length, false, new RawLimits(), CancellationToken.None), Is.EqualTo(source));
            Assert.That(raw, Is.Not.EqualTo(zlib));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyRepetitiveAndIncompressibleInputsRoundTripDeterministically(bool zlibWrapper)
    {
        Random random = new Random(8219);
        byte[] randomBytes = new byte[180000];
        random.NextBytes(randomBytes);
        byte[][] sources = new byte[][]
        {
            Array.Empty<byte>(),
            Enumerable.Repeat((byte)0xa5, 120000).ToArray(),
            randomBytes
        };

        foreach (byte[] source in sources)
        {
            byte[] first = DeflateCodec.Encode(source, zlibWrapper, new RawLimits(), CancellationToken.None);
            byte[] second = DeflateCodec.Encode(source, zlibWrapper, new RawLimits(), CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first), $"determinism length {source.Length}");
                Assert.That(DeflateCodec.Decode(first, source.Length, zlibWrapper, new RawLimits(), CancellationToken.None), Is.EqualTo(source), $"round trip length {source.Length}");
            });
        }
    }

    [Test]
    public void TiffHorizontalPredictorIsAppliedBeforeCompressionAndReversedAfterInflation()
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.Horizontal, 4, 2, 3, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);
        byte[] source = new byte[]
        {
            10, 20, 30, 12, 25, 40, 15, 30, 42, 17, 31, 50,
            200, 100, 50, 190, 110, 55, 180, 120, 65, 170, 130, 80
        };
        byte[] expectedPredicted = new byte[]
        {
            10, 20, 30, 2, 5, 10, 3, 5, 2, 2, 1, 8,
            200, 100, 50, 246, 10, 5, 246, 10, 10, 246, 10, 15
        };
        byte[] original = source.ToArray();

        byte[] encoded = DeflateCodec.EncodeTiff(source, RawCompression.AdobeDeflate, parameters, new RawLimits(), CancellationToken.None);
        byte[] predicted = Inflate(encoded, true);
        byte[] decoded = DeflateCodec.DecodeTiff(encoded, RawCompression.Deflate, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(predicted, Is.EqualTo(expectedPredicted));
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(source, Is.EqualTo(original));
        });
    }

    [Test]
    public void DeclaredDecodedLengthMustMatchTheInflatedDataExactly()
    {
        byte[] source = System.Text.Encoding.ASCII.GetBytes("exact output size");
        byte[] encoded = DeflateCodec.Encode(source, true, new RawLimits(), CancellationToken.None);

        RawFormatException? tooShort = Assert.Throws<RawFormatException>(() => DeflateCodec.Decode(encoded, source.Length - 1, true, new RawLimits(), CancellationToken.None));
        RawFormatException? tooLong = Assert.Throws<RawFormatException>(() => DeflateCodec.Decode(encoded, source.Length + 1, true, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((tooShort ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((tooLong ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    [Test]
    public void CorruptZlibChecksumIsRejectedByTheBuiltInDecoder()
    {
        byte[] source = System.Text.Encoding.ASCII.GetBytes("checksum validation");
        byte[] encoded = DeflateCodec.Encode(source, true, new RawLimits(), CancellationToken.None);
        encoded[encoded.Length - 1] ^= 0x01;

        Assert.Throws<InvalidDataException>(() => DeflateCodec.Decode(encoded, source.Length, true, new RawLimits(), CancellationToken.None));
    }

    [Test]
    public void DecodeAndEncodeHonorFileDecodedAndAllocationLimits()
    {
        byte[] source = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        byte[] encoded = DeflateCodec.Encode(source, true, new RawLimits(), CancellationToken.None);
        RawLimits fileLimits = new RawLimits();
        fileLimits.MaxFileSizeBytes = encoded.Length - 1;
        RawLimits decodedLimits = new RawLimits();
        decodedLimits.MaxDecodedBytes = source.Length - 1;
        decodedLimits.MaxAllocationBytes = source.Length - 1;
        RawLimits outputLimits = new RawLimits();
        outputLimits.MaxAllocationBytes = source.Length;

        RawLimitException? fileFailure = Assert.Throws<RawLimitException>(() => DeflateCodec.Decode(encoded, source.Length, true, fileLimits, CancellationToken.None));
        RawLimitException? decodedFailure = Assert.Throws<RawLimitException>(() => DeflateCodec.Decode(encoded, source.Length, true, decodedLimits, CancellationToken.None));
        RawLimitException? inputFailure = Assert.Throws<RawLimitException>(() => DeflateCodec.Encode(source, true, decodedLimits, CancellationToken.None));
        RawLimitException? outputFailure = Assert.Throws<RawLimitException>(() => DeflateCodec.Encode(source, true, outputLimits, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((fileFailure ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((decodedFailure ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((inputFailure ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((outputFailure ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void InvalidArgumentsAndPredictorGeometryAreRejected()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.None, 4, 1, 1, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);

        RawFormatException? nullInput = Assert.Throws<RawFormatException>(() => DeflateCodec.Decode(null, 0, true, new RawLimits(), CancellationToken.None));
        RawFormatException? negativeLength = Assert.Throws<RawFormatException>(() => DeflateCodec.Decode(source, -1, true, new RawLimits(), CancellationToken.None));
        RawFormatException? compression = Assert.Throws<RawFormatException>(() => DeflateCodec.EncodeTiff(source, RawCompression.Lzw, parameters, new RawLimits(), CancellationToken.None));
        RawFormatException? geometry = Assert.Throws<RawFormatException>(() => DeflateCodec.EncodeTiff(source, RawCompression.Deflate, parameters, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((nullInput ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((negativeLength ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((compression ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((geometry ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void PreCanceledOperationsDoNotAllocateOrTransformData()
    {
        byte[] source = new byte[1000];
        byte[] encoded = DeflateCodec.Encode(source, true, new RawLimits(), CancellationToken.None);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => DeflateCodec.Encode(source, true, new RawLimits(), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() => DeflateCodec.Decode(encoded, source.Length, true, new RawLimits(), cancellation.Token));
        });
    }

    private static byte[] Inflate(byte[] source, bool zlibWrapper)
    {
        using MemoryStream input = new MemoryStream(source, false);
        using Stream decoder = zlibWrapper
            ? new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress)
            : new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        decoder.CopyTo(output);
        return output.ToArray();
    }
}
