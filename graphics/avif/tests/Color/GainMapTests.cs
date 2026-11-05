using System.Buffers.Binary;

namespace avif.Tests;

public class GainMapTests
{
    [Test]
    public void SingleChannelMetadataParsesAndReplicatesParameters()
    {
        GainMapChannel channel = new GainMapChannel(
            -1, 2, 2, 1, 1, 64, -1, 128);
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(
            CreateMetadata(
                false,
                true,
                0,
                1,
                2,
                1,
                new GainMapChannel[] { channel },
                0,
                0));

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.GetVersion(), Is.Zero);
            Assert.That(metadata.GetMinimumVersion(), Is.Zero);
            Assert.That(metadata.GetWriterVersion(), Is.Zero);
            Assert.That(metadata.IsMultichannel(), Is.False);
            Assert.That(metadata.UsesBaseColorSpace(), Is.True);
            Assert.That(metadata.GetBaseHdrHeadroom(), Is.Zero);
            Assert.That(metadata.GetBaseHdrHeadroomRatio(), Is.EqualTo(1));
            Assert.That(metadata.GetAlternateHdrHeadroom(), Is.EqualTo(2));
            Assert.That(metadata.GetAlternateHdrHeadroomRatio(), Is.EqualTo(4));
            Assert.That(metadata.GetGainMapMinimum(0), Is.EqualTo(-1));
            Assert.That(metadata.GetGainMapMinimum(2), Is.EqualTo(-1));
            Assert.That(metadata.GetGainMapMaximum(1), Is.EqualTo(2));
            Assert.That(metadata.GetGamma(2), Is.EqualTo(2));
            Assert.That(metadata.GetBaseOffset(1), Is.EqualTo(1.0 / 64));
            Assert.That(metadata.GetAlternateOffset(2), Is.EqualTo(-1.0 / 128));
        });
    }

    [Test]
    public void MultichannelMetadataPreservesDistinctChannelParameters()
    {
        GainMapChannel[] channels = new GainMapChannel[]
        {
            new GainMapChannel(0, 1, 1, 1, 0, 1, 0, 1),
            new GainMapChannel(-1, 2, 2, 1, 1, 10, 2, 10),
            new GainMapChannel(-2, 3, 3, 1, 3, 10, 4, 10)
        };
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(
            CreateMetadata(
                true,
                false,
                1,
                2,
                3,
                2,
                channels,
                0,
                0));

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.IsMultichannel(), Is.True);
            Assert.That(metadata.UsesBaseColorSpace(), Is.False);
            Assert.That(metadata.GetBaseHdrHeadroom(), Is.EqualTo(0.5));
            Assert.That(metadata.GetAlternateHdrHeadroom(), Is.EqualTo(1.5));
            Assert.That(metadata.GetGainMapMinimum(0), Is.Zero);
            Assert.That(metadata.GetGainMapMinimum(1), Is.EqualTo(-1));
            Assert.That(metadata.GetGainMapMinimum(2), Is.EqualTo(-2));
            Assert.That(metadata.GetGainMapMaximum(2), Is.EqualTo(3));
            Assert.That(metadata.GetBaseOffset(2), Is.EqualTo(0.3));
            Assert.That(metadata.GetAlternateOffset(1), Is.EqualTo(0.2));
        });
    }

    [Test]
    public void MetadataRejectsUnsupportedVersionsAndInvalidFractions()
    {
        GainMapChannel valid = new GainMapChannel(
            0, 1, 1, 1, 0, 1, 0, 1);
        byte[] version = CreateMetadata(
            false, true, 0, 1, 1, 1,
            new GainMapChannel[] { valid }, 0, 0);
        version[0] = 1;
        byte[] minimumVersion = CreateMetadata(
            false, true, 0, 1, 1, 1,
            new GainMapChannel[] { valid }, 1, 1);
        byte[] zeroHeadroomDenominator = CreateMetadata(
            false, true, 0, 0, 1, 1,
            new GainMapChannel[] { valid }, 0, 0);
        GainMapChannel zeroGamma = new GainMapChannel(
            0, 1, 1, 0, 0, 1, 0, 1);
        GainMapChannel reversed = new GainMapChannel(
            2, 1, 1, 1, 0, 1, 0, 1);
        byte[] trailing = CreateMetadata(
            false, true, 0, 1, 1, 1,
            new GainMapChannel[] { valid }, 0, 0);
        Array.Resize(ref trailing, trailing.Length + 1);

        Assert.Multiple(() =>
        {
            Assert.That(new AvifGainMapMetadata(version).GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(new AvifGainMapMetadata(minimumVersion).GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(new AvifGainMapMetadata(
                zeroHeadroomDenominator).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(new AvifGainMapMetadata(CreateMetadata(
                false, true, 0, 1, 1, 1,
                new GainMapChannel[] { zeroGamma }, 0, 0)).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(new AvifGainMapMetadata(CreateMetadata(
                false, true, 0, 1, 1, 1,
                new GainMapChannel[] { reversed }, 0, 0)).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(new AvifGainMapMetadata(
                trailing.AsSpan(0, 20).ToArray()).GetCode(),
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(new AvifGainMapMetadata(trailing).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    [Test]
    public void HeadroomWeightInterpolatesInLog2SpaceInBothDirections()
    {
        GainMapChannel channel = new GainMapChannel(
            0, 1, 1, 1, 0, 1, 0, 1);
        AvifGainMapMetadata ascending = new AvifGainMapMetadata(
            CreateMetadata(
                false, true, 0, 1, 2, 1,
                new GainMapChannel[] { channel }, 0, 0));
        AvifGainMapMetadata descending = new AvifGainMapMetadata(
            CreateMetadata(
                false, true, 2, 1, 0, 1,
                new GainMapChannel[] { channel }, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(ascending.GetWeight(1), Is.Zero);
            Assert.That(ascending.GetWeight(2), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(ascending.GetWeight(4), Is.EqualTo(1));
            Assert.That(ascending.GetWeight(16), Is.EqualTo(1));
            Assert.That(descending.GetWeight(1), Is.EqualTo(-1));
            Assert.That(descending.GetWeight(2), Is.EqualTo(-0.5).Within(1e-12));
            Assert.That(descending.GetWeight(4), Is.Zero);
        });
    }

    [Test]
    public void GainMapAppliesInLinearLightAndPreservesAlpha()
    {
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 128, true);
        AvifImage mapImage = CreateMonochromeMap(1, 1, 255);
        AvifGainMap gainMap = AvifGainMap.Create(
            mapImage,
            CreateSimpleMetadata(false, true, 1, 1));

        AvifPixelBuffer output = AvifColorConverter.Create()
            .ApplyGainMapWithLimits(
                baseImage,
                gainMap,
                2,
                new AvifSecurityLimits());

        Assert.Multiple(() =>
        {
            Assert.That(gainMap.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetPixelFormat(), Is.EqualTo(AvifPixelFormat.Rgba));
            Assert.That(output.IsFloatingPoint(), Is.True);
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.002));
            Assert.That(ReadHalf(output, 0, 1),
                Is.EqualTo(128.0 / 255).Within(0.002));
            Assert.That(ReadHalf(output, 0, 2),
                Is.EqualTo(128.0 / 255).Within(0.002));
            Assert.That(ReadHalf(output, 0, 3),
                Is.EqualTo(128.0 / 255).Within(0.002));
        });

        output.Close();
        baseImage.Close();
        mapImage.Close();
    }

    [Test]
    public void MultichannelParametersApplyIndependently()
    {
        GainMapChannel[] channels = new GainMapChannel[]
        {
            new GainMapChannel(0, 1, 1, 1, 0, 1, 0, 1),
            new GainMapChannel(0, 2, 1, 1, 0, 1, 0, 1),
            new GainMapChannel(0, 3, 1, 1, 0, 1, 0, 1)
        };
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(
            CreateMetadata(
                true, true, 0, 1, 1, 1, channels, 0, 0));
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage mapImage = CreateIdentityGainMap(255, 255, 255);
        AvifGainMap gainMap = AvifGainMap.Create(mapImage, metadata);

        AvifPixelBuffer output = AvifColorConverter.Create().ApplyGainMap(
            baseImage, gainMap, 2);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.002));
            Assert.That(ReadHalf(output, 0, 1),
                Is.EqualTo(256.0 / 255).Within(0.004));
            Assert.That(ReadHalf(output, 0, 2),
                Is.EqualTo(512.0 / 255).Within(0.008));
        });

        output.Close();
        baseImage.Close();
        mapImage.Close();
    }

    [Test]
    public void GammaAndOffsetsParticipateInLinearLightFormula()
    {
        GainMapChannel channel = new GainMapChannel(
            0, 2, 2, 1, 1, 10, 2, 10);
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(
            CreateMetadata(
                false,
                true,
                0,
                1,
                1,
                1,
                new GainMapChannel[] { channel },
                0,
                0));
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage mapImage = CreateMonochromeMap(1, 1, 64);
        AvifGainMap gainMap = AvifGainMap.Create(mapImage, metadata);

        AvifPixelBuffer output = AvifColorConverter.Create().ApplyGainMap(
            baseImage, gainMap, 2);

        double encoded = 64.0 / 255;
        double gainLog2 = 2 * System.Math.Sqrt(encoded);
        double expected = (64.0 / 255 + 0.1)
            * System.Math.Pow(2, gainLog2) - 0.2;
        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(expected).Within(0.004));
            Assert.That(ReadHalf(output, 0, 1),
                Is.EqualTo(expected).Within(0.004));
            Assert.That(ReadHalf(output, 0, 2),
                Is.EqualTo(expected).Within(0.004));
        });

        output.Close();
        baseImage.Close();
        mapImage.Close();
    }

    [Test]
    public void SmallerGainMapScalesAcrossBaseImageAndHeadroomPartiallyApplies()
    {
        AvifImage baseImage = CreateIdentityImage(
            2, 1, 64, 64, 64, 255, false);
        AvifImage mapImage = CreateMonochromeMap(1, 1, 255);
        AvifGainMap gainMap = AvifGainMap.Create(
            mapImage,
            CreateSimpleMetadata(false, true, 1, 1));

        AvifPixelBuffer output = AvifColorConverter.Create().ApplyGainMap(
            baseImage,
            gainMap,
            (float)System.Math.Sqrt(2));

        double expected = 64.0 / 255 * System.Math.Sqrt(2);
        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(expected).Within(0.003));
            Assert.That(ReadHalf(output, 1, 0),
                Is.EqualTo(expected).Within(0.003));
        });

        output.Close();
        baseImage.Close();
        mapImage.Close();
    }

    [Test]
    public void AlternateColorSpaceFlagRequiresAlternateColorInformation()
    {
        AvifGainMapMetadata metadata = CreateSimpleMetadata(
            false, false, 1, 1);
        AvifImage baseImage = CreateIdentityImage(
            1, 1, 64, 64, 64, 255, false);
        AvifImage firstMapImage = CreateMonochromeMap(1, 1, 255);
        AvifImage secondMapImage = CreateMonochromeMap(1, 1, 255);
        AvifGainMap missing = AvifGainMap.Create(firstMapImage, metadata);
        AvifGainMap supplied = AvifGainMap.Create(
            secondMapImage,
            metadata,
            AvifColorInfo.Create(
                AvifColorPrimaries.Bt2020,
                AvifTransferCharacteristics.Linear,
                AvifMatrixCoefficients.Bt2020NonConstantLuminance,
                AvifRange.Full));

        AvifPixelBuffer unsupported = AvifColorConverter.Create().ApplyGainMap(
            baseImage, missing, 2);
        AvifPixelBuffer output = AvifColorConverter.Create().ApplyGainMap(
            baseImage, supplied, 2);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadHalf(output, 0, 0),
                Is.EqualTo(128.0 / 255).Within(0.01));
        });

        unsupported.Close();
        output.Close();
        baseImage.Close();
        firstMapImage.Close();
        secondMapImage.Close();
    }

    private static AvifGainMapMetadata CreateSimpleMetadata(
        bool multichannel,
        bool useBaseColorSpace,
        int maximum,
        int alternateHeadroom)
    {
        GainMapChannel channel = new GainMapChannel(
            0, maximum, 1, 1, 0, 1, 0, 1);
        GainMapChannel[] channels = multichannel
            ? new GainMapChannel[] { channel, channel, channel }
            : new GainMapChannel[] { channel };
        return new AvifGainMapMetadata(CreateMetadata(
            multichannel,
            useBaseColorSpace,
            0,
            1,
            (uint)alternateHeadroom,
            1,
            channels,
            0,
            0));
    }

    private static byte[] CreateMetadata(
        bool multichannel,
        bool useBaseColorSpace,
        uint baseHeadroomNumerator,
        uint baseHeadroomDenominator,
        uint alternateHeadroomNumerator,
        uint alternateHeadroomDenominator,
        GainMapChannel[] channels,
        ushort minimumVersion,
        ushort writerVersion)
    {
        int channelCount = multichannel ? 3 : 1;
        byte[] payload = new byte[22 + channelCount * 40];
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(1, 2), minimumVersion);
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(3, 2), writerVersion);
        payload[5] = (byte)((multichannel ? 128 : 0)
            | (useBaseColorSpace ? 64 : 0));
        int offset = 6;
        WriteUnsigned(
            payload, offset, baseHeadroomNumerator, baseHeadroomDenominator);
        offset += 8;
        WriteUnsigned(
            payload,
            offset,
            alternateHeadroomNumerator,
            alternateHeadroomDenominator);
        offset += 8;
        int channel = 0;
        while (channel < channelCount)
        {
            GainMapChannel value = channels[channel];
            WriteSigned(payload, offset, value.MinimumNumerator, 1);
            offset += 8;
            WriteSigned(payload, offset, value.MaximumNumerator, 1);
            offset += 8;
            WriteUnsigned(payload, offset, value.GammaNumerator,
                value.GammaDenominator);
            offset += 8;
            WriteSigned(payload, offset, value.BaseOffsetNumerator,
                value.BaseOffsetDenominator);
            offset += 8;
            WriteSigned(payload, offset, value.AlternateOffsetNumerator,
                value.AlternateOffsetDenominator);
            offset += 8;
            channel++;
        }
        return payload;
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

    private static AvifImage CreateMonochromeMap(
        int width,
        int height,
        byte value)
    {
        AvifImage image = AvifImage.Create(
            width, height, 8, AvifChromaFormat.Monochrome, false);
        FillPlane(image.GetPlane(AvifPlaneKind.Y), value);
        return image;
    }

    private static AvifImage CreateIdentityGainMap(
        byte red,
        byte green,
        byte blue)
    {
        AvifImage image = AvifImage.Create(
            1, 1, 8, AvifChromaFormat.Yuv444, false);
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Unspecified,
            AvifTransferCharacteristics.Unspecified,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = green;
        (image.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = blue;
        (image.GetPlane(AvifPlaneKind.V) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = red;
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

    private class GainMapChannel
    {
        internal int MinimumNumerator;
        internal int MaximumNumerator;
        internal uint GammaNumerator;
        internal uint GammaDenominator;
        internal int BaseOffsetNumerator;
        internal uint BaseOffsetDenominator;
        internal int AlternateOffsetNumerator;
        internal uint AlternateOffsetDenominator;

        internal GainMapChannel(
            int minimumNumerator,
            int maximumNumerator,
            uint gammaNumerator,
            uint gammaDenominator,
            int baseOffsetNumerator,
            uint baseOffsetDenominator,
            int alternateOffsetNumerator,
            uint alternateOffsetDenominator)
        {
            MinimumNumerator = minimumNumerator;
            MaximumNumerator = maximumNumerator;
            GammaNumerator = gammaNumerator;
            GammaDenominator = gammaDenominator;
            BaseOffsetNumerator = baseOffsetNumerator;
            BaseOffsetDenominator = baseOffsetDenominator;
            AlternateOffsetNumerator = alternateOffsetNumerator;
            AlternateOffsetDenominator = alternateOffsetDenominator;
        }
    }
}
