using System.Buffers.Binary;

namespace avif.Tests;

public class MinimizedImageBoxTests
{
    [Test]
    public void ParserExpandsCompactAv1ImageIntoEquivalentItemModel()
    {
        byte[] source = CreateDocument("mif3", IsoBoxType.GetValue("avif"), false, false, false, false);

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetHandlerType(), Is.EqualTo("pict"));
            Assert.That(result.Meta.GetPrimaryItemId(), Is.EqualTo(1));
            Assert.That(result.Meta.GetItemCount(), Is.EqualTo(1));
            Assert.That(result.Meta.GetPropertyCount(), Is.EqualTo(4));
        });

        ParsedItem? primary = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem();
        ParsedItemLocation? location = (primary ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();
        ParsedItemExtent? extent = (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0);
        EncodedItemExtractionResult extracted = result.Meta.ExtractEncodedItem(source, 1);
        Assert.Multiple(() =>
        {
            Assert.That(primary.GetItemType(), Is.EqualTo("av01"));
            Assert.That(primary.GetPropertyCount(), Is.EqualTo(4));
            Assert.That((primary.GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetAv1Configuration(), Is.Not.Null);
            Assert.That(((primary.GetPropertyAssociation(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetSpatialExtents() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(2));
            Assert.That(((primary.GetPropertyAssociation(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetSpatialExtents() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(3));
            Assert.That((extent ?? throw new InvalidOperationException("Invalid internal state.")).HasResolvedRange(), Is.True);
            Assert.That(extent.GetResolvedLength(), Is.EqualTo(1));
            Assert.That(extracted.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 0x12 }));
        });
    }

    [Test]
    public void ParserExpandsAlphaIccExifAndXmpChunksAndReferences()
    {
        byte[] source = CreateDocument("mif3", IsoBoxType.GetValue("avif"), true, true, true, true);

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(4));
            Assert.That(result.Meta.GetReferenceCount(), Is.EqualTo(4));
            Assert.That((result.Meta.FindItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((result.Meta.FindItem(6) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("Exif"));
            Assert.That((result.Meta.FindItem(7) ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(), Is.EqualTo("application/rdf+xml"));
            Assert.That(((result.Meta.GetProperty(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetColourInformation() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That((result.Meta.ExtractEncodedItem(source, 2).Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 0x33 }));
            Assert.That((result.Meta.ExtractEncodedItem(source, 6).Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 0x44 }));
            Assert.That((result.Meta.ExtractEncodedItem(source, 7).Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 0x55 }));
        });
    }

    [Test]
    public void ParserRejectsWrongBrandAndTrailingChunkBytes()
    {
        byte[] wrongBrand = CreateDocument("avif", IsoBoxType.GetValue("avif"), false, false, false, false);
        byte[] trailing = CreateDocument("mif3", IsoBoxType.GetValue("avif"), false, false, false, false);
        System.Array.Resize(ref trailing, trailing.Length + 1);
        BinaryPrimitives.WriteUInt32BigEndian(trailing.AsSpan(16, 4), (uint)(trailing.Length - 16));

        MetaParseResult brandResult = MetaBoxParser.Parse(wrongBrand, CreateLimits());
        MetaParseResult trailingResult = MetaBoxParser.Parse(trailing, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(brandResult.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(trailingResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void ValidatorAcceptsMif3MiniWithoutARegularMetaBox()
    {
        byte[] source = CreateDocument("mif3", IsoBoxType.GetValue("avif"), false, false, false, false);
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = false;

        AvifContainerValidationResult result = AvifContainerValidator.Validate(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.FileType ?? throw new InvalidOperationException("Invalid internal state.")).MajorBrand, Is.EqualTo("mif3"));
            Assert.That((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroMinorVersionUsesExplicitAv1CodecTypes()
    {
        byte[] source = CreateDocument("mif3", 0, false, false, false, false, true);

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        Assert.That(((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("av01"));
    }

    internal static byte[] CreateDocument(
        string majorBrand,
        uint minorVersion,
        bool alpha,
        bool icc,
        bool exif,
        bool xmp,
        bool explicitTypes = false)
    {
        byte[] fileType = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(fileType.AsSpan(0, 4), 16);
        WriteFourCc(fileType.AsSpan(4, 4), "ftyp");
        WriteFourCc(fileType.AsSpan(8, 4), majorBrand);
        BinaryPrimitives.WriteUInt32BigEndian(fileType.AsSpan(12, 4), minorVersion);

        byte[] payload = CreateMiniPayload(alpha, icc, exif, xmp, explicitTypes);
        byte[] mini = new byte[payload.Length + 8];
        BinaryPrimitives.WriteUInt32BigEndian(mini.AsSpan(0, 4), (uint)mini.Length);
        WriteFourCc(mini.AsSpan(4, 4), "mini");
        payload.CopyTo(mini.AsSpan(8));
        byte[] result = new byte[fileType.Length + mini.Length];
        fileType.CopyTo(result.AsSpan());
        mini.CopyTo(result.AsSpan(fileType.Length));
        return result;
    }

    static byte[] CreateMiniPayload(bool alpha, bool icc, bool exif, bool xmp, bool explicitTypes)
    {
        byte[] header = new byte[64];
        BitWriter writer = BitWriter.Create(header.Length);
        writer.WriteBits(header, 0, 2);
        writer.WriteBit(header, explicitTypes ? 1 : 0);
        writer.WriteBit(header, 0);
        writer.WriteBit(header, 1);
        writer.WriteBit(header, alpha ? 1 : 0);
        writer.WriteBit(header, 1);
        writer.WriteBit(header, 0);
        writer.WriteBit(header, icc ? 1 : 0);
        writer.WriteBit(header, exif ? 1 : 0);
        writer.WriteBit(header, xmp ? 1 : 0);
        writer.WriteBits(header, 1, 2);
        writer.WriteBits(header, 0, 3);
        writer.WriteBit(header, 0);
        writer.WriteBits(header, 1, 7);
        writer.WriteBits(header, 2, 7);
        writer.WriteBit(header, 0);
        writer.WriteBit(header, 0);
        writer.WriteBit(header, 0);
        if (alpha)
        {
            writer.WriteBit(header, 1);
        }

        writer.WriteBits(header, 1, 8);
        writer.WriteBits(header, 13, 8);
        writer.WriteBits(header, 6, 8);
        if (explicitTypes)
        {
            writer.WriteBits(header, IsoBoxType.GetValue("av01"), 32);
            writer.WriteBits(header, IsoBoxType.GetValue("av1C"), 32);
        }

        if (icc || exif || xmp)
        {
            writer.WriteBit(header, 0);
        }

        writer.WriteBit(header, 0);
        writer.WriteBit(header, 0);
        if (icc)
        {
            writer.WriteBits(header, 1, 10);
        }

        writer.WriteBits(header, 4, 3);
        writer.WriteBits(header, 0, 15);
        if (alpha)
        {
            writer.WriteBits(header, 1, 15);
            writer.WriteBits(header, 4, 3);
        }

        if (exif || xmp)
        {
            writer.WriteBit(header, 0);
        }

        if (exif)
        {
            writer.WriteBits(header, 0, 10);
        }

        if (xmp)
        {
            writer.WriteBits(header, 0, 10);
        }

        while (!writer.IsByteAligned())
        {
            writer.WriteBit(header, 0);
        }

        byte[] mainConfig = new byte[] { 0x81, 0, 0x0e, 0 };
        byte[] alphaConfig = new byte[] { 0x81, 0, 0x1c, 0 };
        int headerLength = (int)(writer.BitPosition / 8);
        int chunkLength = 4 + (alpha ? 4 : 0) + (icc ? 2 : 0) + (alpha ? 1 : 0) + 1 + (exif ? 1 : 0) + (xmp ? 1 : 0);
        byte[] payload = new byte[headerLength + chunkLength];
        header.AsSpan(0, headerLength).CopyTo(payload);
        int offset = headerLength;
        mainConfig.CopyTo(payload.AsSpan(offset));
        offset += 4;
        if (alpha)
        {
            alphaConfig.CopyTo(payload.AsSpan(offset));
            offset += 4;
        }

        if (icc)
        {
            payload[offset++] = 1;
            payload[offset++] = 2;
        }

        if (alpha)
        {
            payload[offset++] = 0x33;
        }

        payload[offset++] = 0x12;
        if (exif)
        {
            payload[offset++] = 0x44;
        }

        if (xmp)
        {
            payload[offset] = 0x55;
        }

        return payload;
    }

    static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    static AvifSecurityLimits CreateLimits()
    {
        return new AvifSecurityLimits();
    }
}
