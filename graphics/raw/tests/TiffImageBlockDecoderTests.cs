using raw;

namespace raw.Tests;

public class TiffImageBlockDecoderTests
{
    [Test]
    public void ByteCompressionDispatchRestoresPredictorForEverySupportedCodec()
    {
        byte[] expected = new byte[] { 1, 2, 4, 8, 10, 20, 30, 40 };
        TiffPredictorParameters predictor = Predictor(TiffPredictor.Horizontal);
        byte[] predicted = (byte[])expected.Clone();
        TiffPredictorCodec.Encode(predicted, predictor, new RawLimits(), CancellationToken.None);
        byte[] packBits = PackBitsCodec.Encode(predicted, new RawLimits(), CancellationToken.None);
        byte[] lzw = LzwCodec.EncodeTiff(expected, 1, predictor, new RawLimits(), CancellationToken.None);
        byte[] adobeDeflate = DeflateCodec.EncodeTiff(expected, RawCompression.AdobeDeflate, predictor, new RawLimits(), CancellationToken.None);
        byte[] deflate = DeflateCodec.EncodeTiff(expected, RawCompression.Deflate, predictor, new RawLimits(), CancellationToken.None);
        (ulong Code, RawCompression Compression, byte[] Encoded)[] cases = new[]
        {
            (1UL, RawCompression.None, predicted),
            (32773UL, RawCompression.PackBits, packBits),
            (5UL, RawCompression.Lzw, lzw),
            (8UL, RawCompression.AdobeDeflate, adobeDeflate),
            (32946UL, RawCompression.Deflate, deflate)
        };

        foreach ((ulong code, RawCompression compression, byte[] encoded) in cases)
        {
            TiffDecodedBlock result = TiffImageBlockDecoder.Decode(encoded, Parameters(code, predictor), new RawLimits(), CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(result.Compression, Is.EqualTo(compression));
                Assert.That(result.Bytes, Is.EqualTo(expected));
                Assert.That(result.BaselineJpeg, Is.Null);
                Assert.That(result.LosslessJpeg, Is.Null);
            });
        }
    }

    [Test]
    public void CompleteNewStyleJpegDispatchesByFrameProcess()
    {
        BaselineJpegImage baselineSource = GrayImage(8, 8, Enumerable.Repeat((byte)143, 64).ToArray());
        byte[] baseline = BaselineJpegEncoder.Encode(baselineSource, new BaselineJpegEncodeParameters(100, 1, 1, 0), new RawLimits(), CancellationToken.None);
        LosslessJpegImage losslessSource = LosslessImage(new ushort[] { 10, 11, 20, 22 });
        byte[] lossless = LosslessJpegEncoder.Encode(losslessSource, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);

        TiffDecodedBlock first = TiffImageBlockDecoder.Decode(baseline, Parameters(7, null, 8, 8, 1), new RawLimits(), CancellationToken.None);
        TiffDecodedBlock second = TiffImageBlockDecoder.Decode(lossless, Parameters(7, null, 2, 2, 1), new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Compression, Is.EqualTo(RawCompression.BaselineJpeg));
            Assert.That((first.BaselineJpeg ?? throw new System.InvalidOperationException("A required value is unavailable.")).Planes.Single().Samples, Is.All.EqualTo(143));
            Assert.That(first.LosslessJpeg, Is.Null);
            Assert.That(second.Compression, Is.EqualTo(RawCompression.LosslessJpeg));
            Assert.That((second.LosslessJpeg ?? throw new System.InvalidOperationException("A required value is unavailable.")).Planes.Single().Samples, Is.EqualTo(new ushort[] { 10, 11, 20, 22 }));
            Assert.That(second.BaselineJpeg, Is.Null);
        });
    }

    [Test]
    public void NewStyleAbbreviatedImageMergesSharedJpegTables()
    {
        BaselineJpegImage source = GrayImage(8, 8, Enumerable.Repeat((byte)91, 64).ToArray());
        byte[] complete = BaselineJpegEncoder.Encode(source, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(complete, new RawLimits(), CancellationToken.None);
        int frameOffset = (document.Frames.Single().Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset;
        byte[] tables = complete[..frameOffset].Concat(new byte[] { 0xff, 0xd9 }).ToArray();
        byte[] image = new byte[] { 0xff, 0xd8 }.Concat(complete[frameOffset..]).ToArray();
        TiffImageBlockDecodeParameters parameters = Parameters(7, null, 8, 8, 1);
        parameters.JpegTables = tables;

        TiffDecodedBlock result = TiffImageBlockDecoder.Decode(image, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.SharedJpegTablesApplied, Is.True);
            Assert.That(result.LegacyJpegPrefixApplied, Is.False);
            Assert.That((result.BaselineJpeg ?? throw new System.InvalidOperationException("A required value is unavailable.")).Planes.Single().Samples, Is.All.EqualTo(91));
        });
    }

    [Test]
    public void LegacyEntropyOnlyStripUsesExplicitJpegHeaderPrefix()
    {
        BaselineJpegImage source = GrayImage(8, 8, Enumerable.Repeat((byte)177, 64).ToArray());
        byte[] complete = BaselineJpegEncoder.Encode(source, new BaselineJpegEncodeParameters(100, 1, 1, 0), new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(complete, new RawLimits(), CancellationToken.None);
        int entropyOffset = document.Scans.Single().EntropyOffset;
        int eoiOffset = document.Markers.Single(marker => marker.Code == 0xd9).Offset;
        byte[] prefix = complete[..entropyOffset];
        byte[] entropy = complete[entropyOffset..eoiOffset];
        TiffImageBlockDecodeParameters parameters = Parameters(6, null, 8, 8, 1);
        parameters.LegacyJpegPrefix = prefix;

        TiffDecodedBlock result = TiffImageBlockDecoder.Decode(entropy, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.LegacyJpegPrefixApplied, Is.True);
            Assert.That(result.SharedJpegTablesApplied, Is.False);
            Assert.That(result.Compression, Is.EqualTo(RawCompression.BaselineJpeg));
            Assert.That((result.BaselineJpeg ?? throw new System.InvalidOperationException("A required value is unavailable.")).Planes.Single().Samples, Is.All.EqualTo(177));
        });
    }

    [Test]
    public void JpegGeometryPredictorsAndAbbreviatedInputsAreValidated()
    {
        BaselineJpegImage source = GrayImage(8, 8, new byte[64]);
        byte[] complete = BaselineJpegEncoder.Encode(source, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(complete, new RawLimits(), CancellationToken.None);
        int frameOffset = (document.Frames.Single().Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset;
        byte[] abbreviated = new byte[] { 0xff, 0xd8 }.Concat(complete[frameOffset..]).ToArray();
        TiffImageBlockDecodeParameters wrongGeometry = Parameters(7, null, 9, 8, 1);
        TiffImageBlockDecodeParameters predictor = Parameters(7, Predictor(TiffPredictor.Horizontal), 8, 8, 1);
        TiffImageBlockDecodeParameters legacy = Parameters(6, null, 8, 8, 1);
        TiffImageBlockDecodeParameters badTables = Parameters(7, null, 8, 8, 1);
        badTables.JpegTables = new byte[] { 1, 2, 3 };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(complete, wrongGeometry, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(complete, predictor, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(abbreviated, Parameters(7, null, 8, 8, 1), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(new byte[] { 1, 2 }, legacy, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(complete, badTables, new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void UnsupportedCompressionLengthsLimitsNullsAndCancellationAreRejected()
    {
        TiffPredictorParameters predictor = Predictor(TiffPredictor.None);
        RawLimits limits = new RawLimits();
        limits.MaxFileSizeBytes = 7;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => TiffImageBlockDecoder.Decode(new byte[8], Parameters(999, predictor), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(new byte[7], Parameters(1, predictor), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => TiffImageBlockDecoder.Decode(new byte[8], Parameters(1, predictor), limits, CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(null, Parameters(1, predictor), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => TiffImageBlockDecoder.Decode(new byte[8], null, new RawLimits(), CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => TiffImageBlockDecoder.Decode(new byte[8], Parameters(1, predictor), new RawLimits(), cancellation.Token));
        });
    }

    private static TiffImageBlockDecodeParameters Parameters(ulong code, TiffPredictorParameters predictor)
    {
        return Parameters(code, predictor, 0, 0, 0);
    }

    private static TiffImageBlockDecodeParameters Parameters(ulong code, TiffPredictorParameters? predictor, int width, int height, int components)
    {
        return new TiffImageBlockDecodeParameters(code, predictor, 1, null, null, width, height, components);
    }

    private static TiffPredictorParameters Predictor(TiffPredictor predictor)
    {
        return new TiffPredictorParameters(predictor, 4, 2, 1, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);
    }

    private static BaselineJpegImage GrayImage(int width, int height, byte[] samples)
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = samples;
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.Planes.Add(plane);
        return image;
    }

    private static LosslessJpegImage LosslessImage(ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = 1;
        plane.Width = 2;
        plane.Height = 2;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = samples;
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 12;
        image.Planes.Add(plane);
        return image;
    }
}
