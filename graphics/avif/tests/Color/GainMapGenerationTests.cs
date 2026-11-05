using System.Buffers.Binary;

namespace avif.Tests;

public class GainMapGenerationTests
{
    [Test]
    public void GeneratedGainMapReconstructsBrighterAlternate()
    {
        AvifImage baseImage = CreateIdentityImage(2, 1, 64, 64, 64);
        AvifImage alternateImage = CreateIdentityImage(2, 1, 128, 128, 128);

        AvifGainMap gainMap = AvifColorConverter.Create().ComputeGainMap(
            baseImage, alternateImage);
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        AvifPixelBuffer reconstructed = AvifColorConverter.Create().ApplyGainMap(
            baseImage,
            gainMap,
            (float)(metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetAlternateHdrHeadroomRatio());

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(2));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(1));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth(), Is.EqualTo(8));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetChromaFormat(),
                Is.EqualTo(AvifChromaFormat.Yuv444));
            Assert.That(metadata.IsMultichannel(), Is.True);
            Assert.That(metadata.UsesBaseColorSpace(), Is.True);
            Assert.That(metadata.GetAlternateHdrHeadroom(),
                Is.GreaterThan(metadata.GetBaseHdrHeadroom()));
            Assert.That(reconstructed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(reconstructed, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.006));
            Assert.That(ReadHalf(reconstructed, 1, 2),
                Is.EqualTo(128.0 / 255).Within(0.006));
        });

        reconstructed.Close();
        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void PerChannelInverseParametersReconstructDistinctAlternateChannels()
    {
        AvifImage baseImage = CreateIdentityImage(1, 1, 64, 64, 64);
        AvifImage alternateImage = CreateIdentityImage(1, 1, 128, 64, 32);

        AvifGainMap gainMap = AvifColorConverter.Create().ComputeGainMap(
            baseImage, alternateImage);
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        AvifPixelBuffer reconstructed = AvifColorConverter.Create().ApplyGainMap(
            baseImage,
            gainMap,
            (float)(metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetAlternateHdrHeadroomRatio());

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.GetGainMapMinimum(0), Is.GreaterThan(0));
            Assert.That(metadata.GetGainMapMinimum(1),
                Is.EqualTo(0).Within(0.000001));
            Assert.That(metadata.GetGainMapMaximum(2), Is.LessThan(0));
            Assert.That(ReadHalf(reconstructed, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.007));
            Assert.That(ReadHalf(reconstructed, 0, 1),
                Is.EqualTo(64.0 / 255).Within(0.007));
            Assert.That(ReadHalf(reconstructed, 0, 2),
                Is.EqualTo(32.0 / 255).Within(0.007));
        });

        reconstructed.Close();
        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void DescendingHeadroomGainMapReconstructsDarkerAlternate()
    {
        AvifImage baseImage = CreateIdentityImage(1, 1, 128, 128, 128);
        AvifImage alternateImage = CreateIdentityImage(1, 1, 64, 64, 64);

        AvifGainMap gainMap = AvifColorConverter.Create().ComputeGainMap(
            baseImage, alternateImage);
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        AvifPixelBuffer reconstructed = AvifColorConverter.Create().ApplyGainMap(
            baseImage,
            gainMap,
            (float)(metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetAlternateHdrHeadroomRatio());

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.GetBaseHdrHeadroom(),
                Is.GreaterThan(metadata.GetAlternateHdrHeadroom()));
            Assert.That(metadata.GetWeight(
                metadata.GetAlternateHdrHeadroomRatio()), Is.EqualTo(-1));
            Assert.That(ReadHalf(reconstructed, 0, 0),
                Is.EqualTo(64.0 / 255).Within(0.006));
            Assert.That(ReadHalf(reconstructed, 0, 1),
                Is.EqualTo(64.0 / 255).Within(0.006));
            Assert.That(ReadHalf(reconstructed, 0, 2),
                Is.EqualTo(64.0 / 255).Within(0.006));
        });

        reconstructed.Close();
        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void ExplicitOutputCreatesScaledMonochromeGainMap()
    {
        AvifImage baseImage = CreateIdentityImage(4, 2, 64, 64, 64);
        AvifImage alternateImage = CreateIdentityImage(4, 2, 128, 128, 128);

        AvifGainMap gainMap = AvifColorConverter.Create().ComputeGainMapWithEncoding(
            baseImage,
            alternateImage,
            1,
            1,
            10,
            AvifChromaFormat.Monochrome,
            new AvifSecurityLimits());
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        AvifPixelBuffer reconstructed = AvifColorConverter.Create().ApplyGainMap(
            baseImage,
            gainMap,
            (float)(metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetAlternateHdrHeadroomRatio());

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(1));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(1));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth(), Is.EqualTo(10));
            Assert.That((gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetChromaFormat(),
                Is.EqualTo(AvifChromaFormat.Monochrome));
            Assert.That(metadata.IsMultichannel(), Is.False);
            Assert.That(ReadHalf(reconstructed, 3, 0),
                Is.EqualTo(128.0 / 255).Within(0.007));
        });

        reconstructed.Close();
        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void GeneratedMetadataPayloadIsStableAndWriterReady()
    {
        AvifImage baseImage = CreateIdentityImage(1, 1, 64, 64, 64);
        AvifImage alternateImage = CreateIdentityImage(1, 1, 128, 64, 32);
        AvifGainMap gainMap = AvifColorConverter.Create().ComputeGainMap(
            baseImage, alternateImage);
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();

        byte[] first = (metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetData();
        byte[] expected = (byte[])first.Clone();
        first.AsSpan().Clear();
        byte[] second = metadata.GetData();
        AvifGainMapMetadata reparsed = new AvifGainMapMetadata(second);

        Assert.Multiple(() =>
        {
            Assert.That(expected.Length, Is.EqualTo(142));
            Assert.That(second, Is.EqualTo(expected));
            Assert.That(reparsed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(reparsed.IsMultichannel(), Is.True);
            Assert.That(reparsed.GetGainMapMinimum(0),
                Is.EqualTo(metadata.GetGainMapMinimum(0)).Within(0.000001));
            Assert.That(reparsed.GetAlternateOffset(2),
                Is.EqualTo(metadata.GetAlternateOffset(2)).Within(0.000001));
        });

        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void AlternateColorSpaceGenerationReconstructsInOutputPrimaries()
    {
        AvifImage baseImage = CreateIdentityImage(1, 1, 80, 80, 80);
        AvifImage alternateImage = CreateIdentityImage(1, 1, 120, 100, 80);
        alternateImage.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Linear,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        AvifColorConverter converter = AvifColorConverter.Create();
        AvifPixelBuffer expected = converter.ConvertWithDepth(
            alternateImage, AvifPixelFormat.Rgba, 16, true);
        AvifGainMap gainMap = converter.ComputeGainMap(
            baseImage, alternateImage);
        AvifGainMapMetadata? metadata = gainMap.GetMetadata();
        AvifPixelBuffer reconstructed = converter.ApplyGainMap(
            baseImage,
            gainMap,
            (float)(metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetAlternateHdrHeadroomRatio());
        AvifColorInfo? alternateColor = gainMap.GetAlternateColorInfo();

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.UsesBaseColorSpace(), Is.False);
            Assert.That((alternateColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt2020));
            Assert.That(reconstructed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(reconstructed, 0, 0),
                Is.EqualTo(ReadHalf(expected, 0, 0)).Within(0.008));
            Assert.That(ReadHalf(reconstructed, 0, 1),
                Is.EqualTo(ReadHalf(expected, 0, 1)).Within(0.008));
            Assert.That(ReadHalf(reconstructed, 0, 2),
                Is.EqualTo(ReadHalf(expected, 0, 2)).Within(0.008));
        });

        reconstructed.Close();
        (gainMap.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        expected.Close();
        baseImage.Close();
        alternateImage.Close();
    }

    [Test]
    public void GenerationRejectsMismatchedDimensionsAndHonorsLimits()
    {
        AvifImage baseImage = CreateIdentityImage(2, 1, 64, 64, 64);
        AvifImage wrongSize = CreateIdentityImage(1, 1, 128, 128, 128);
        AvifImage alternateImage = CreateIdentityImage(2, 1, 128, 128, 128);
        AvifColorConverter converter = AvifColorConverter.Create();
        AvifGainMap mismatch = converter.ComputeGainMap(baseImage, wrongSize);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxWidth = 1;
        AvifGainMap limited = converter.ComputeGainMapWithEncoding(
            baseImage,
            alternateImage,
            2,
            1,
            8,
            AvifChromaFormat.Yuv444,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(limited.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        baseImage.Close();
        wrongSize.Close();
        alternateImage.Close();
    }

    private static AvifImage CreateIdentityImage(
        int width,
        int height,
        byte red,
        byte green,
        byte blue)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        FillPlane(image.GetPlane(AvifPlaneKind.Y), green);
        FillPlane(image.GetPlane(AvifPlaneKind.U), blue);
        FillPlane(image.GetPlane(AvifPlaneKind.V), red);
        return image;
    }

    private static void FillPlane(AvifPlane? plane, byte value)
    {
        int y = 0;
        while (y < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(y).Fill(value);
            y++;
        }
    }

    private static double ReadHalf(
        AvifPixelBuffer buffer,
        int x,
        int channel)
    {
        Span<byte> row = buffer.GetRowSpan(0);
        int offset = x * buffer.GetBytesPerPixel() + channel * 2;
        ushort bits = BinaryPrimitives.ReadUInt16LittleEndian(
            row.Slice(offset, 2));
        return (double)BitConverter.UInt16BitsToHalf(bits);
    }
}
