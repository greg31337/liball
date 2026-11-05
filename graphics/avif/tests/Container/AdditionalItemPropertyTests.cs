using System.Buffers.Binary;

namespace avif.Tests;

public class AdditionalItemPropertyTests
{
    [Test]
    public void ColourInformationParsesCicpIccAndOpaqueVariants()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteNclx(writer, 1, 13, 6, true, 0);
            WriteNclc(writer, 9, 16, 9);
            WriteColourData(writer, "rICC", new byte[] { 1, 2, 3 });
            WriteColourData(writer, "prof", new byte[] { 4, 5 });
            WriteColourData(writer, "zzzz", new byte[] { 6, 7 });
        }, new int[] { 1, 2, 3, 4, 5 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedColourInformation? nclx = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation();
        ParsedColourInformation? nclc = (result.Meta.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation();
        ParsedColourInformation? restricted = (result.Meta.GetProperty(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation();
        ParsedColourInformation? unrestricted = (result.Meta.GetProperty(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation();
        ParsedColourInformation? opaque = (result.Meta.GetProperty(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation();
        byte[] restrictedData = (restricted ?? throw new InvalidOperationException("Invalid internal state.")).GetData();
        source.AsSpan().Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((nclx ?? throw new InvalidOperationException("Invalid internal state.")).GetColourType(), Is.EqualTo("nclx"));
            Assert.That(nclx.HasCicpValues(), Is.True);
            Assert.That(nclx.HasFullRangeFlag(), Is.True);
            Assert.That(nclx.GetColourPrimaries(), Is.EqualTo(1));
            Assert.That(nclx.GetTransferCharacteristics(), Is.EqualTo(13));
            Assert.That(nclx.GetMatrixCoefficients(), Is.EqualTo(6));
            Assert.That(nclx.IsFullRange(), Is.True);
            Assert.That((nclc ?? throw new InvalidOperationException("Invalid internal state.")).HasCicpValues(), Is.True);
            Assert.That(nclc.HasFullRangeFlag(), Is.False);
            Assert.That(restricted.IsIccProfile(), Is.True);
            Assert.That(restricted.IsRestrictedIccProfile(), Is.True);
            Assert.That(restrictedData, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That((unrestricted ?? throw new InvalidOperationException("Invalid internal state.")).IsIccProfile(), Is.True);
            Assert.That(unrestricted.IsRestrictedIccProfile(), Is.False);
            Assert.That((opaque ?? throw new InvalidOperationException("Invalid internal state.")).GetColourType(), Is.EqualTo("zzzz"));
            Assert.That(opaque.GetData(), Is.EqualTo(new byte[] { 6, 7 }));
        });
    }

    [Test]
    public void ColourInformationRejectsMalformedCicpEmptyIccAndIccLimit()
    {
        MetaParseResult shortBox = ParseSingleProperty(writer =>
            WriteColourData(writer, "nclx", new byte[] { 0, 1 }));
        MetaParseResult reserved = ParseSingleProperty(writer =>
            WriteNclx(writer, 1, 13, 6, true, 1));
        MetaParseResult emptyIcc = ParseSingleProperty(writer =>
            WriteColourData(writer, "prof", System.Array.Empty<byte>()));
        byte[] source = BuildDocument(writer =>
        {
            WriteColourData(writer, "rICC", new byte[] { 1, 2, 3 });
        }, new int[] { 1 });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxIccBytes = 2;
        MetaParseResult limited = MetaBoxParser.Parse(source, limits);

        Assert.Multiple(() =>
        {
            Assert.That(shortBox.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(emptyIcc.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void PixelAspectRatioAndCleanApertureExposeValidatedFractions()
    {
        byte[] source = BuildDocument(writer =>
        {
            writer.BeginBox("pasp");
            WriteUInt32(writer, 4);
            WriteUInt32(writer, 3);
            writer.EndBox();
            WriteCleanAperture(writer, 1920, 1, 1080, 1, -1, 2, 3, 4);
        }, new int[] { 1, 2 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedPixelAspectRatio? ratio = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelAspectRatio();
        ParsedCleanAperture? aperture = (result.Meta.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetCleanAperture();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((ratio ?? throw new InvalidOperationException("Invalid internal state.")).GetHorizontalSpacing(), Is.EqualTo(4));
            Assert.That(ratio.GetVerticalSpacing(), Is.EqualTo(3));
            Assert.That((aperture ?? throw new InvalidOperationException("Invalid internal state.")).GetWidthNumerator(), Is.EqualTo(1920));
            Assert.That(aperture.GetWidthDenominator(), Is.EqualTo(1));
            Assert.That(aperture.GetHeightNumerator(), Is.EqualTo(1080));
            Assert.That(aperture.GetHeightDenominator(), Is.EqualTo(1));
            Assert.That(aperture.GetHorizontalOffsetNumerator(), Is.EqualTo(-1));
            Assert.That(aperture.GetHorizontalOffsetDenominator(), Is.EqualTo(2));
            Assert.That(aperture.GetVerticalOffsetNumerator(), Is.EqualTo(3));
            Assert.That(aperture.GetVerticalOffsetDenominator(), Is.EqualTo(4));
        });
    }

    [Test]
    public void PixelAspectRatioAndCleanApertureRejectInvalidValuesAndSizes()
    {
        MetaParseResult zeroRatio = ParseSingleProperty(writer =>
        {
            writer.BeginBox("pasp");
            WriteUInt32(writer, 0);
            WriteUInt32(writer, 1);
            writer.EndBox();
        });
        MetaParseResult trailingRatio = ParseSingleProperty(writer =>
        {
            writer.BeginBox("pasp");
            WriteUInt32(writer, 1);
            WriteUInt32(writer, 1);
            writer.Write(new byte[] { 0 });
            writer.EndBox();
        });
        MetaParseResult zeroDenominator = ParseSingleProperty(writer =>
            WriteCleanAperture(writer, 10, 0, 10, 1, 0, 1, 0, 1));
        MetaParseResult oversizedNumerator = ParseSingleProperty(writer =>
            WriteCleanAperture(writer, 2147483648U, 1, 10, 1, 0, 1, 0, 1));

        Assert.Multiple(() =>
        {
            Assert.That(zeroRatio.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailingRatio.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(zeroDenominator.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(oversizedNumerator.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void RotationAndMirrorParseAxesAndRejectReservedBits()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteByteProperty(writer, "irot", 3);
            WriteByteProperty(writer, "imir", 1);
        }, new int[] { 1, 2 });
        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedImageRotation? rotation = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetImageRotation();
        ParsedImageMirror? mirror = (result.Meta.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetImageMirror();
        MetaParseResult reservedRotation = ParseSingleProperty(writer =>
            WriteByteProperty(writer, "irot", 4));
        MetaParseResult reservedMirror = ParseSingleProperty(writer =>
            WriteByteProperty(writer, "imir", 2));
        MetaParseResult trailing = ParseSingleProperty(writer =>
        {
            writer.BeginBox("irot");
            writer.Write(new byte[] { 0, 0 });
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((rotation ?? throw new InvalidOperationException("Invalid internal state.")).GetQuarterTurnsCounterClockwise(), Is.EqualTo(3));
            Assert.That(rotation.GetDegreesCounterClockwise(), Is.EqualTo(270));
            Assert.That((mirror ?? throw new InvalidOperationException("Invalid internal state.")).GetAxis(), Is.EqualTo(1));
            Assert.That(mirror.ExchangesLeftAndRight(), Is.True);
            Assert.That(reservedRotation.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(reservedMirror.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void AuxiliaryTypeParsesUtf8TypeAndOpaqueSubtypes()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteAuxiliaryType(
                writer,
                0,
                0,
                "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha",
                new byte[] { 7, 8 });
        }, new int[] { 1 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedAuxiliaryType? auxiliary = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryType();
        byte[] subtypes = (auxiliary ?? throw new InvalidOperationException("Invalid internal state.")).GetSubtypes();
        source.AsSpan().Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                auxiliary.GetTypeValue(),
                Is.EqualTo("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
            Assert.That(subtypes, Is.EqualTo(new byte[] { 7, 8 }));
        });
    }

    [Test]
    public void AuxiliaryTypePreservesUnsupportedVersionAndRejectsMalformedSyntax()
    {
        MetaParseResult version = ParseSingleProperty(writer =>
            WriteAuxiliaryType(writer, 1, 0, "alpha", System.Array.Empty<byte>()));
        MetaParseResult flags = ParseSingleProperty(writer =>
            WriteAuxiliaryType(writer, 0, 1, "alpha", System.Array.Empty<byte>()));
        MetaParseResult empty = ParseSingleProperty(writer =>
            WriteAuxiliaryType(writer, 0, 0, string.Empty, System.Array.Empty<byte>()));
        MetaParseResult unterminated = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("auxC", 0, 0);
            writer.Write(new byte[] { (byte)'a' });
            writer.EndBox();
        });
        MetaParseResult invalidUtf8 = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("auxC", 0, 0);
            writer.Write(new byte[] { 0xC0, 0 });
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((version.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryType(), Is.Null);
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(empty.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(unterminated.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(invalidUtf8.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void Av1LayerPropertiesParseSelectorsAndSmallOrLargeIndexes()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteByteProperty(writer, "a1op", 31);
            writer.BeginBox("lsel");
            WriteUInt16(writer, 65535);
            writer.EndBox();
            WriteLayerIndex(writer, false, new uint[] { 12, 34, 0 });
            WriteLayerIndex(writer, true, new uint[] { 70000, 80000, 90000 });
        }, new int[] { 1, 2, 3, 4 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedOperatingPointSelector? operatingPoint = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetOperatingPointSelector();
        ParsedLayerSelector? selector = (result.Meta.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerSelector();
        ParsedAv1LayeredImageIndex? small = (result.Meta.GetProperty(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetLayeredImageIndex();
        ParsedAv1LayeredImageIndex? large = (result.Meta.GetProperty(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetLayeredImageIndex();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((operatingPoint ?? throw new InvalidOperationException("Invalid internal state.")).GetOperatingPointIndex(), Is.EqualTo(31));
            Assert.That((selector ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerId(), Is.EqualTo(65535));
            Assert.That(selector.AllowsProgressiveRendering(), Is.True);
            Assert.That((small ?? throw new InvalidOperationException("Invalid internal state.")).UsesLargeSizes(), Is.False);
            Assert.That(small.GetLayerSize(0), Is.EqualTo(12));
            Assert.That(small.GetIndexedLayerCount(), Is.EqualTo(2));
            Assert.That(small.GetLayerCount(), Is.EqualTo(3));
            Assert.That(small.GetIndexedByteCount(), Is.EqualTo(46));
            Assert.That(small.GetLayerSize(3), Is.Zero);
            Assert.That((large ?? throw new InvalidOperationException("Invalid internal state.")).UsesLargeSizes(), Is.True);
            Assert.That(large.GetLayerSize(2), Is.EqualTo(90000));
            Assert.That(large.GetLayerCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void Av1LayerPropertiesRejectOutOfRangeAndNonCanonicalLayouts()
    {
        MetaParseResult operatingPoint = ParseSingleProperty(writer =>
            WriteByteProperty(writer, "a1op", 32));
        MetaParseResult layer = ParseSingleProperty(writer =>
        {
            writer.BeginBox("lsel");
            WriteUInt16(writer, 4);
            writer.EndBox();
        });
        MetaParseResult reserved = ParseSingleProperty(writer =>
        {
            writer.BeginBox("a1lx");
            writer.Write(new byte[] { 2, 0, 1, 0, 0, 0, 0 });
            writer.EndBox();
        });
        MetaParseResult zeroGap = ParseSingleProperty(writer =>
            WriteLayerIndex(writer, false, new uint[] { 1, 0, 2 }));
        MetaParseResult wrongLength = ParseSingleProperty(writer =>
        {
            writer.BeginBox("a1lx");
            writer.Write(new byte[] { 1, 0, 0, 0, 0, 0, 0 });
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(operatingPoint.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(layer.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroGap.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(wrongLength.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void NclxAndOperatingPointSelectorsReconcileWithEmbeddedSequenceHeader()
    {
        byte[] sequence = BuildGeneralSequenceObu();
        byte[] configuration = CreateConfiguration(
            1, 8, 1, false, false, false, false, false, 0, sequence);
        MetaParseResult matching = ParseConfigurationProperties(
            configuration,
            1,
            13,
            0,
            true,
            0);
        MetaParseResult primariesMismatch = ParseConfigurationProperties(
            configuration,
            9,
            13,
            0,
            true,
            0);
        MetaParseResult rangeMismatch = ParseConfigurationProperties(
            configuration,
            1,
            13,
            0,
            false,
            0);
        MetaParseResult operatingPointMismatch = ParseConfigurationProperties(
            configuration,
            1,
            13,
            0,
            true,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                ((((matching.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetAv1Configuration() ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetConfigSequenceHeader() ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetOperatingPointCount(),
                Is.EqualTo(1));
            Assert.That(primariesMismatch.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(rangeMismatch.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(operatingPointMismatch.Code, Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    private static MetaParseResult ParseConfigurationProperties(
        byte[] configuration,
        int primaries,
        int transfer,
        int matrix,
        bool fullRange,
        int operatingPoint)
    {
        byte[] source = BuildDocument(writer =>
        {
            writer.BeginBox("av1C");
            writer.Write(configuration);
            writer.EndBox();
            WriteNclx(writer, primaries, transfer, matrix, fullRange, 0);
            WriteByteProperty(writer, "a1op", operatingPoint);
        }, new int[] { 1, 2, 3 });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static MetaParseResult ParseSingleProperty(Action<IsoBoxWriter> writeProperty)
    {
        byte[] source = BuildDocument(writeProperty, new int[] { 1 });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static byte[] BuildDocument(
        Action<IsoBoxWriter> writeProperties,
        int[] propertyIndices)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 131072);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer);
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writeProperties(writer);
        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 1);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { (byte)propertyIndices.Length });
        int index = 0;
        while (index < propertyIndices.Length)
        {
            writer.Write(new byte[] { (byte)propertyIndices[index] });
            index++;
        }

        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 1);
        writer.BeginFullBox("infe", 3, 0);
        byte[] entry = new byte[11];
        BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), 1);
        WriteFourCc(entry.AsSpan(6, 4), "av01");
        writer.Write(entry);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteNclx(
        IsoBoxWriter writer,
        int primaries,
        int transfer,
        int matrix,
        bool fullRange,
        int reserved)
    {
        writer.BeginBox("colr");
        WriteFourCc(writer, "nclx");
        WriteUInt16(writer, primaries);
        WriteUInt16(writer, transfer);
        WriteUInt16(writer, matrix);
        writer.Write(new byte[] { (byte)((fullRange ? 128 : 0) | reserved) });
        writer.EndBox();
    }

    private static void WriteNclc(
        IsoBoxWriter writer,
        int primaries,
        int transfer,
        int matrix)
    {
        writer.BeginBox("colr");
        WriteFourCc(writer, "nclc");
        WriteUInt16(writer, primaries);
        WriteUInt16(writer, transfer);
        WriteUInt16(writer, matrix);
        writer.EndBox();
    }

    private static void WriteColourData(
        IsoBoxWriter writer,
        string type,
        byte[] data)
    {
        writer.BeginBox("colr");
        WriteFourCc(writer, type);
        writer.Write(data);
        writer.EndBox();
    }

    private static void WriteCleanAperture(
        IsoBoxWriter writer,
        uint widthNumerator,
        uint widthDenominator,
        uint heightNumerator,
        uint heightDenominator,
        int horizontalNumerator,
        uint horizontalDenominator,
        int verticalNumerator,
        uint verticalDenominator)
    {
        writer.BeginBox("clap");
        WriteUInt32(writer, widthNumerator);
        WriteUInt32(writer, widthDenominator);
        WriteUInt32(writer, heightNumerator);
        WriteUInt32(writer, heightDenominator);
        WriteInt32(writer, horizontalNumerator);
        WriteUInt32(writer, horizontalDenominator);
        WriteInt32(writer, verticalNumerator);
        WriteUInt32(writer, verticalDenominator);
        writer.EndBox();
    }

    private static void WriteAuxiliaryType(
        IsoBoxWriter writer,
        int version,
        int flags,
        string type,
        byte[] subtypes)
    {
        writer.BeginFullBox("auxC", version, flags);
        writer.Write(System.Text.Encoding.UTF8.GetBytes(type));
        writer.Write(new byte[] { 0 });
        writer.Write(subtypes);
        writer.EndBox();
    }

    private static void WriteLayerIndex(
        IsoBoxWriter writer,
        bool large,
        uint[] sizes)
    {
        writer.BeginBox("a1lx");
        writer.Write(new byte[] { (byte)(large ? 1 : 0) });
        int index = 0;
        while (index < sizes.Length)
        {
            if (large)
            {
                WriteUInt32(writer, sizes[index]);
            }
            else
            {
                WriteUInt16(writer, (int)sizes[index]);
            }

            index++;
        }

        writer.EndBox();
    }

    private static void WriteByteProperty(IsoBoxWriter writer, string type, int value)
    {
        writer.BeginBox(type);
        writer.Write(new byte[] { (byte)value });
        writer.EndBox();
    }

    private static byte[] CreateConfiguration(
        int profile,
        int level,
        int tier,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        int chromaSamplePosition,
        byte[] configObus)
    {
        byte[] configuration = new byte[4 + configObus.Length];
        configuration[0] = 0x81;
        configuration[1] = (byte)(profile << 5 | level);
        configuration[2] = (byte)(tier << 7
            | (highBitDepth ? 64 : 0)
            | (twelveBit ? 32 : 0)
            | (monochrome ? 16 : 0)
            | (subsamplingX ? 8 : 0)
            | (subsamplingY ? 4 : 0)
            | chromaSamplePosition);
        configuration[3] = 0;
        configObus.CopyTo(configuration, 4);
        return configuration;
    }

    private static byte[] BuildGeneralSequenceObu()
    {
        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 1, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 5);
        writer.WriteBits(payload, 0, 12);
        writer.WriteBits(payload, 8, 5);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 6, 4);
        writer.WriteBits(payload, 5, 4);
        writer.WriteBits(payload, 127, 7);
        writer.WriteBits(payload, 63, 6);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 4);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 8);
        writer.WriteBits(payload, 13, 8);
        writer.WriteBits(payload, 0, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        byte[] sizedPayload = payload[..(int)(writer.BitPosition / 8)];
        byte[] obu = new byte[sizedPayload.Length + 2];
        obu[0] = 10;
        obu[1] = (byte)sizedPayload.Length;
        sizedPayload.CopyTo(obu, 2);
        return obu;
    }

    private static void WriteFourCc(IsoBoxWriter writer, string value)
    {
        byte[] bytes = new byte[4];
        WriteFourCc(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static void WriteUInt16(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteInt32(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 262144;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxMetadataBytes = 65536;
        limits.MaxIccBytes = 32768;
        limits.MaxTemporaryBytes = 131072;
        return limits;
    }
}
