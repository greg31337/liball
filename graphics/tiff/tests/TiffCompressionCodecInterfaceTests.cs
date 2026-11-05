using System;

namespace tiff.Tests;

[TestFixture]
public class TiffCompressionCodecInterfaceTests
{
    [Test]
    public void Decode_UsesCustomCodecForRecognizedExternalCompression()
    {
        TestCompressionCodec codec = new TestCompressionCodec(TiffCompression.Jpeg);
        codec.DecodeOutput = new byte[] { 3, 2, 1 };
        TiffCodecContext context = CreateContext(TiffCompression.Jpeg, 1);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 8, 9 }, codec);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 3, 2, 1 }));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(1));
        Assert.That(codec.LastDecodeSource, Is.EqualTo(new byte[] { 8, 9 }));
        Assert.That(TiffCodec.IsCompressionSupported(TiffCompression.Jpeg, codec), Is.True);
        Assert.That(TiffCodec.CanPassThroughRawCompression(TiffCompression.Jpeg, codec), Is.False);
    }

    [Test]
    public void EncodeAndDecode_CustomCodecRemainInsideCommonPredictorPipeline()
    {
        TestCompressionCodec codec = new TestCompressionCodec(TiffCompression.Jpeg);
        codec.CopyEncodeSource = true;
        codec.DecodeOutput = new byte[] { 10, 5, 5 };
        TiffCodecContext context = CreateContext(TiffCompression.Jpeg, 2);

        TiffCodecResult encoded = TiffCodec.Encode(context, new byte[] { 10, 15, 20 }, codec);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes, codec);

        Assert.That(encoded.Success, Is.True);
        Assert.That(codec.LastEncodeSource, Is.EqualTo(new byte[] { 10, 5, 5 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 10, 15, 20 }));
    }

    [Test]
    public void Decode_FallsBackToBuiltInCodecWhenCustomCodecDoesNotSupportCompression()
    {
        TestCompressionCodec codec = new TestCompressionCodec(TiffCompression.Jpeg);
        TiffCodecContext context = CreateContext(TiffCompression.None, 1);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 4, 5, 6 }, codec);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Decode_ReportsInvalidCustomCodecResultWithoutThrowing()
    {
        TestCompressionCodec codec = new TestCompressionCodec(TiffCompression.Jpeg);
        codec.ReturnNullDecodeResult = true;
        TiffCodecContext context = CreateContext(TiffCompression.Jpeg, 1);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 8, 9 }, codec);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void Decode_RejectsWrongDecodedByteCountFromCustomCodec()
    {
        TestCompressionCodec codec = new TestCompressionCodec(TiffCompression.Jpeg);
        codec.DecodeOutput = new byte[] { 1, 2 };
        TiffCodecContext context = CreateContext(TiffCompression.Jpeg, 1);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 8, 9 }, codec);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffCodecContext CreateContext(TiffCompression compression, int predictor)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 3;
        info.Length = 1;
        info.RowsPerStrip = 1;
        info.BitsPerSample = new int[] { 8 };
        info.Compression = compression;
        info.Predictor = predictor;
        return TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());
    }

    class TestCompressionCodec : ITiffCompressionCodec
    {
        public TiffCompression SupportedCompression;
        public byte[] DecodeOutput;
        public bool CopyEncodeSource;
        public bool ReturnNullDecodeResult;
        public int DecodeCallCount;
        public byte[] LastDecodeSource;
        public byte[] LastEncodeSource;

        public TestCompressionCodec(TiffCompression supportedCompression)
        {
            SupportedCompression = supportedCompression;
            DecodeOutput = new byte[0];
            CopyEncodeSource = false;
            ReturnNullDecodeResult = false;
            DecodeCallCount = 0;
            LastDecodeSource = new byte[0];
            LastEncodeSource = new byte[0];
        }

        public bool CanDecode(TiffCompression compression)
        {
            return compression == SupportedCompression;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return compression == SupportedCompression;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            LastDecodeSource = source.ToArray();
            if (ReturnNullDecodeResult)
            {
                return null!;
            }

            return TiffCodecResult.Create(DecodeOutput, new TiffDiagnostics());
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            LastEncodeSource = source.ToArray();
            if (CopyEncodeSource)
            {
                return TiffCodecResult.Create(LastEncodeSource, new TiffDiagnostics());
            }

            return TiffCodecResult.Create(new byte[0], new TiffDiagnostics());
        }
    }
}
