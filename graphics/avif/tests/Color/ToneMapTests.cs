using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif.Tests;

public class ToneMapTests
{
    [Test]
    public void AutomaticPreferredAlternateReconstructsAndScalesGainMap()
    {
        AvifImage baseImage = CreateIdentityImage(
            2, 1, 64, 64, 64, 128, true);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        AvifGainMap gainMap = AvifGainMap.Create(
            gainImage,
            CreateMetadata(1, true, 0, 1));
        AvifToneMap toneMap = AvifToneMap.Create(baseImage, gainMap, true);
        AvifRenderOptions options = new AvifRenderOptions();
        options.DisplayHeadroom = 2;
        options.PreserveAlpha = false;

        AvifPixelBuffer output = toneMap.Render(options);

        Assert.Multiple(() =>
        {
            Assert.That(toneMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(toneMap.GetBaseImage(), Is.SameAs(baseImage));
            Assert.That(toneMap.IsAlternateImagePreferred(), Is.True);
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetWidth(), Is.EqualTo(2));
            Assert.That(output.GetHeight(), Is.EqualTo(1));
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.003));
            Assert.That(ReadHalf(output, 1, 0),
                Is.EqualTo(128.0 / 255).Within(0.003));
            Assert.That(ReadHalf(output, 0, 3), Is.EqualTo(1));
        });

        output.Close();
        baseImage.Close();
        gainImage.Close();
    }

    [Test]
    public void AlternateGroupPreferenceSelectsBaseUnlessApplicationIsForced()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        AvifToneMap toneMap = AvifToneMap.Create(
            baseImage,
            AvifGainMap.Create(gainImage, CreateMetadata(1, true, 0, 1)),
            false);
        AvifRenderOptions automaticOptions = new AvifRenderOptions();
        automaticOptions.DisplayHeadroom = 2;
        AvifRenderOptions forcedOptions = new AvifRenderOptions();
        forcedOptions.ToneMapMode = AvifToneMapMode.ApplyGainMap;
        forcedOptions.DisplayHeadroom = 2;

        AvifPixelBuffer automatic = toneMap.Render(automaticOptions);
        AvifPixelBuffer forced = toneMap.Render(forcedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(toneMap.IsAlternateImagePreferred(), Is.False);
            Assert.That(automatic.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(automatic, 0, 0),
                Is.EqualTo(64.0 / 255).Within(0.002));
            Assert.That(forced.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(forced, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.003));
        });

        automatic.Close();
        forced.Close();
        baseImage.Close();
        gainImage.Close();
    }

    [Test]
    public void OrderedItemRelationshipsResolveAlternatePreference()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        AvifGainMap gainMap = AvifGainMap.Create(
            gainImage,
            CreateMetadata(1, true, 0, 1));
        AvifEntityGroup preferredGroup = new AvifEntityGroup(
            AvifEntityGroupKind.Alternate,
            "altr",
            0,
            0,
            20,
            new List<uint>() { 3, 1 },
            Array.Empty<byte>());
        AvifEntityGroup baseFirstGroup = new AvifEntityGroup(
            AvifEntityGroupKind.Alternate,
            "altr",
            0,
            0,
            21,
            new List<uint>() { 1, 3 },
            Array.Empty<byte>());

        AvifToneMap preferred = AvifToneMap.Create(
            baseImage, gainMap, 3, new uint[] { 1, 2 }, preferredGroup);
        AvifToneMap baseFirst = AvifToneMap.Create(
            baseImage, gainMap, 3, new uint[] { 1, 2 }, baseFirstGroup);
        AvifToneMap invalid = AvifToneMap.Create(
            baseImage, gainMap, 3, new uint[] { 1, 1 }, preferredGroup);

        Assert.Multiple(() =>
        {
            Assert.That(preferred.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(preferred.IsAlternateImagePreferred(), Is.True);
            Assert.That(preferred.GetToneMapItemId(), Is.EqualTo(3));
            Assert.That(preferred.GetBaseItemId(), Is.EqualTo(1));
            Assert.That(preferred.GetGainMapItemId(), Is.EqualTo(2));
            Assert.That(baseFirst.IsAlternateImagePreferred(), Is.False);
            Assert.That(invalid.GetCode(), Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(invalid.Render(new AvifRenderOptions()).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
        });

        baseImage.Close();
        gainImage.Close();
    }

    [Test]
    public void UnsupportedDerivationFallsBackOnlyForAutomaticPolicy()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        byte[] unsupportedPayload = new byte[6];
        unsupportedPayload[0] = 1;
        AvifToneMap toneMap = AvifToneMap.Create(
            baseImage,
            AvifGainMap.Create(
                gainImage,
                new AvifGainMapMetadata(unsupportedPayload)));
        AvifRenderOptions automaticOptions = new AvifRenderOptions();
        automaticOptions.DisplayHeadroom = 2;
        AvifRenderOptions baseOptions = new AvifRenderOptions();
        baseOptions.ToneMapMode = AvifToneMapMode.BaseImage;
        AvifRenderOptions forcedOptions = new AvifRenderOptions();
        forcedOptions.ToneMapMode = AvifToneMapMode.ApplyGainMap;
        forcedOptions.DisplayHeadroom = 2;

        AvifPixelBuffer automatic = toneMap.Render(automaticOptions);
        AvifPixelBuffer baseOutput = toneMap.Render(baseOptions);
        AvifPixelBuffer forced = toneMap.Render(forcedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(toneMap.GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(automatic.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(automatic, 0, 0),
                Is.EqualTo(64.0 / 255).Within(0.002));
            Assert.That(baseOutput.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(forced.GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
        });

        automatic.Close();
        baseOutput.Close();
        forced.Close();
        baseImage.Close();
        gainImage.Close();
    }

    [Test]
    public void AutomaticPolicyDoesNotHideMalformedMetadata()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        AvifToneMap toneMap = AvifToneMap.Create(
            baseImage,
            AvifGainMap.Create(
                gainImage,
                new AvifGainMapMetadata(new byte[6])));

        AvifPixelBuffer output = toneMap.Render(new AvifRenderOptions());

        Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));

        output.Close();
        baseImage.Close();
        gainImage.Close();
    }

    [Test]
    public void BaseHeadroomEndpointAvoidsUnneededUnsupportedColorConversion()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage gainImage = CreateGainMapImage(1, 1, 255);
        AvifToneMap toneMap = AvifToneMap.Create(
            baseImage,
            AvifGainMap.Create(
                gainImage,
                CreateMetadata(1, false, 0, 1)));
        AvifRenderOptions automaticOptions = new AvifRenderOptions();
        AvifRenderOptions forcedOptions = new AvifRenderOptions();
        forcedOptions.ToneMapMode = AvifToneMapMode.ApplyGainMap;

        AvifPixelBuffer automatic = toneMap.Render(automaticOptions);
        AvifPixelBuffer forced = toneMap.Render(forcedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(automatic, 0, 0),
                Is.EqualTo(64.0 / 255).Within(0.002));
            Assert.That(forced.GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
        });

        automatic.Close();
        forced.Close();
        baseImage.Close();
        gainImage.Close();
    }

    private static AvifGainMapMetadata CreateMetadata(
        int maximum,
        bool useBaseColorSpace,
        uint baseHeadroom,
        uint alternateHeadroom)
    {
        byte[] payload = new byte[62];
        payload[5] = useBaseColorSpace ? (byte)64 : (byte)0;
        int offset = 6;
        WriteUnsigned(payload, offset, baseHeadroom, 1);
        offset += 8;
        WriteUnsigned(payload, offset, alternateHeadroom, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, maximum, 1);
        offset += 8;
        WriteUnsigned(payload, offset, 1, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        return new AvifGainMapMetadata(payload);
    }

    private static void WriteSigned(
        byte[] payload,
        int offset,
        int numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(offset, 4), numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4), denominator);
    }

    private static void WriteUnsigned(
        byte[] payload,
        int offset,
        uint numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset, 4), numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4), denominator);
    }

    private static AvifImage CreateIdentityImage(
        int width,
        int height,
        byte red,
        byte green,
        byte blue,
        byte alpha,
        bool hasAlpha)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            hasAlpha ? AvifAlphaMode.Straight : AvifAlphaMode.None,
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
        if (hasAlpha)
        {
            FillPlane(image.GetPlane(AvifPlaneKind.Alpha), alpha);
        }
        return image;
    }

    private static AvifImage CreateGainMapImage(
        int width,
        int height,
        byte value)
    {
        AvifImage image = AvifImage.Create(
            width, height, 8, AvifChromaFormat.Monochrome, false);
        FillPlane(image.GetPlane(AvifPlaneKind.Y), value);
        return image;
    }

    private static void FillPlane(AvifPlane? plane, byte value)
    {
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
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
