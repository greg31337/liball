using System;
using System.Collections.Generic;

namespace avif;

internal static class MinimizedImageBox
{
    internal static MetaParseResult ParseAndExpand(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        IsoBoxParseResult boxes,
        FileTypeBox? fileType)
    {
        if (box == null || box.Type != "mini" || boxes == null || fileType == null)
        {
            return new MetaParseResult(AvifResultCode.InvalidArgument, null, boxes);
        }

        bool avifMinorVersion = fileType.MinorVersion == IsoBoxType.GetValue("avif");
        if (fileType.Code != AvifResultCode.Success
            || fileType.MajorBrand != "mif3"
            || (!avifMinorVersion && fileType.MinorVersion != 0))
        {
            return Fail(boxes, box, AvifResultCode.NonConformingData,
                "A minimized AVIF requires major brand mif3 and a zero or avif minor version.");
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            box.PayloadSize,
            source.Length);
        if (!range.Success || box.PayloadSize > int.MaxValue)
        {
            return Fail(boxes, box, AvifResultCode.InvalidContainer,
                "The MinimizedImageBox payload is out of bounds.");
        }

        ReadOnlySpan<byte> payload = source.Slice((int)box.PayloadOffset, (int)box.PayloadSize);
        BitReader reader = BitReader.Create(payload.Length);
        int version = (int)reader.ReadBits(payload, 2);
        bool explicitCodecTypes = reader.ReadBit(payload) != 0;
        bool floatingPoint = reader.ReadBit(payload) != 0;
        bool fullRange = reader.ReadBit(payload) != 0;
        bool hasAlpha = reader.ReadBit(payload) != 0;
        bool explicitCicp = reader.ReadBit(payload) != 0;
        bool hasHdr = reader.ReadBit(payload) != 0;
        bool hasIcc = reader.ReadBit(payload) != 0;
        bool hasExif = reader.ReadBit(payload) != 0;
        bool hasXmp = reader.ReadBit(payload) != 0;
        int chromaSubsampling = (int)reader.ReadBits(payload, 2);
        int orientation = (int)reader.ReadBits(payload, 3) + 1;
        bool largeDimensions = reader.ReadBit(payload) != 0;
        int dimensionBits = largeDimensions ? 15 : 7;
        int width = (int)reader.ReadBits(payload, dimensionBits) + 1;
        int height = (int)reader.ReadBits(payload, dimensionBits) + 1;
        bool horizontalCentered = false;
        bool verticalCentered = false;
        if (chromaSubsampling == 1 || chromaSubsampling == 2)
        {
            horizontalCentered = reader.ReadBit(payload) != 0;
        }

        if (chromaSubsampling == 1)
        {
            verticalCentered = reader.ReadBit(payload) != 0;
        }

        if (version != 0 || reader.Code != AvifResultCode.Success)
        {
            return Fail(boxes, box, AvifResultCode.InvalidContainer,
                "The MinimizedImageBox header is malformed or uses an unsupported version.");
        }

        if (floatingPoint)
        {
            return Fail(boxes, box, AvifResultCode.NonConformingData,
                "Floating-point samples are not valid for an AVIF minimized image.");
        }

        int bitDepth = 8;
        bool highBitDepth = reader.ReadBit(payload) != 0;
        if (highBitDepth)
        {
            bitDepth = (int)reader.ReadBits(payload, 3) + 9;
        }

        bool alphaPremultiplied = false;
        if (hasAlpha)
        {
            alphaPremultiplied = reader.ReadBit(payload) != 0;
        }

        int primaries = hasIcc ? 2 : 1;
        int transfer = hasIcc ? 2 : 13;
        int matrix = chromaSubsampling == 0 ? 2 : 6;
        if (explicitCicp)
        {
            primaries = (int)reader.ReadBits(payload, 8);
            transfer = (int)reader.ReadBits(payload, 8);
            matrix = (int)reader.ReadBits(payload, 8);
        }

        string itemType = "av01";
        string configurationType = "av1C";
        if (explicitCodecTypes)
        {
            itemType = ReadFourCc(reader, payload);
            configurationType = ReadFourCc(reader, payload);
        }

        if (reader.Code != AvifResultCode.Success
            || itemType != "av01"
            || configurationType != "av1C"
            || (avifMinorVersion && explicitCodecTypes)
            || (!avifMinorVersion && !explicitCodecTypes)
            || hasHdr)
        {
            AvifResultCode code = hasHdr
                ? AvifResultCode.UnsupportedFeature
                : AvifResultCode.NonConformingData;
            return Fail(boxes, box, code,
                hasHdr
                    ? "HDR MinimizedImageBox expansion is not supported."
                    : "The MinimizedImageBox codec signaling is not AVIF-compatible.");
        }

        if (chromaSubsampling == 2 && horizontalCentered)
        {
            return Fail(boxes, box, AvifResultCode.NonConformingData,
                "AV1 does not represent horizontally centered 4:2:2 chroma.");
        }

        bool largeMetadata = false;
        if (hasIcc || hasExif || hasXmp)
        {
            largeMetadata = reader.ReadBit(payload) != 0;
        }

        bool largeConfiguration = reader.ReadBit(payload) != 0;
        bool largeItemData = reader.ReadBit(payload) != 0;
        int metadataBits = largeMetadata ? 20 : 10;
        int configurationBits = largeConfiguration ? 12 : 3;
        int itemDataBits = largeItemData ? 28 : 15;
        long iccSize = hasIcc ? (long)reader.ReadBits(payload, metadataBits) + 1 : 0;
        long mainConfigurationSize = (long)reader.ReadBits(payload, configurationBits);
        long mainDataSize = (long)reader.ReadBits(payload, itemDataBits) + 1;
        long alphaDataSize = hasAlpha ? (long)reader.ReadBits(payload, itemDataBits) : 0;
        long alphaConfigurationSize = hasAlpha && alphaDataSize != 0
            ? (long)reader.ReadBits(payload, configurationBits)
            : 0;
        bool compressedMetadata = false;
        if (hasExif || hasXmp)
        {
            compressedMetadata = reader.ReadBit(payload) != 0;
        }

        long exifSize = hasExif ? (long)reader.ReadBits(payload, metadataBits) + 1 : 0;
        long xmpSize = hasXmp ? (long)reader.ReadBits(payload, metadataBits) + 1 : 0;
        AvifResultCode alignCode = reader.AlignToByte(payload, true);
        if (reader.Code != AvifResultCode.Success || alignCode != AvifResultCode.Success)
        {
            return Fail(boxes, box, AvifResultCode.InvalidContainer,
                "The MinimizedImageBox size fields or padding are malformed.");
        }

        if (compressedMetadata)
        {
            return Fail(boxes, box, AvifResultCode.UnsupportedFeature,
                "Compressed Exif and XMP chunks are not supported.");
        }

        if (mainConfigurationSize != 4
            || (alphaConfigurationSize != 0 && alphaConfigurationSize != 4))
        {
            return Fail(boxes, box, AvifResultCode.NonConformingData,
                "Compact AV1 item configuration chunks must contain exactly four bytes.");
        }

        long dataOffset = reader.BitPosition / 8;
        long expected = mainConfigurationSize + alphaConfigurationSize + iccSize
            + alphaDataSize + mainDataSize + exifSize + xmpSize;
        if (dataOffset > payload.Length || expected != payload.Length - dataOffset)
        {
            return Fail(boxes, box, AvifResultCode.InvalidContainer,
                "The MinimizedImageBox chunks do not exactly consume its payload.");
        }

        AvifResultCode limitsCode = CheckLimits(
            boxes.Resources,
            width,
            height,
            orientation,
            hasAlpha,
            hasExif,
            hasXmp,
            iccSize,
            exifSize,
            xmpSize);
        if (limitsCode != AvifResultCode.Success)
        {
            return Fail(boxes, box, limitsCode,
                "The MinimizedImageBox exceeds a configured resource limit.");
        }

        int cursor = (int)dataOffset;
        byte[] mainConfiguration = payload.Slice(cursor, 4).ToArray();
        cursor += 4;
        byte[] alphaConfiguration = mainConfiguration;
        if (alphaConfigurationSize == 4)
        {
            alphaConfiguration = payload.Slice(cursor, 4).ToArray();
            cursor += 4;
        }

        int iccOffset = cursor;
        cursor += (int)iccSize;
        int alphaOffset = cursor;
        cursor += (int)alphaDataSize;
        int mainOffset = cursor;
        cursor += (int)mainDataSize;
        int exifOffset = cursor;
        cursor += (int)exifSize;
        int xmpOffset = cursor;

        return Expand(
            source,
            box,
            boxes,
            width,
            height,
            bitDepth,
            chromaSubsampling,
            horizontalCentered,
            verticalCentered,
            fullRange,
            primaries,
            transfer,
            matrix,
            orientation,
            hasAlpha,
            alphaPremultiplied,
            mainConfiguration,
            alphaConfiguration,
            iccOffset,
            iccSize,
            alphaOffset,
            alphaDataSize,
            mainOffset,
            mainDataSize,
            exifOffset,
            exifSize,
            xmpOffset,
            xmpSize);
    }

    internal static MetaParseResult Expand(
        ReadOnlySpan<byte> source,
        IsoBox box,
        IsoBoxParseResult boxes,
        int width,
        int height,
        int bitDepth,
        int chromaSubsampling,
        bool horizontalCentered,
        bool verticalCentered,
        bool fullRange,
        int primaries,
        int transfer,
        int matrix,
        int orientation,
        bool hasAlpha,
        bool alphaPremultiplied,
        byte[] mainConfiguration,
        byte[] alphaConfiguration,
        int iccOffset,
        long iccSize,
        int alphaOffset,
        long alphaSize,
        int mainOffset,
        long mainSize,
        int exifOffset,
        long exifSize,
        int xmpOffset,
        long xmpSize)
    {
        List<ParsedItemProperty> properties = new List<ParsedItemProperty>();
        ParsedItemProperty? mainConfig = CreateAv1Configuration(1, mainConfiguration);
        if (mainConfig == null
            || !MatchesMiniConfiguration(
                mainConfig.GetAv1Configuration(),
                bitDepth,
                chromaSubsampling,
                horizontalCentered,
                verticalCentered))
        {
            return Fail(boxes, box, AvifResultCode.InvalidAv1Bitstream,
                "The compact main AV1 configuration is invalid.");
        }

        properties.Add(mainConfig);
        ParsedItemProperty spatial = CreateSpatialProperty(2, width, height);
        properties.Add(spatial);
        ParsedItemProperty pixels = CreatePixelProperty(3, bitDepth, chromaSubsampling == 0 ? 1 : 3);
        properties.Add(pixels);
        ParsedItemProperty colour = CreateColourProperty(4, "nclx", primaries, transfer, matrix, fullRange, System.Array.Empty<byte>());
        properties.Add(colour);

        ParsedItemProperty? icc = null;
        if (iccSize > 0)
        {
            byte[] iccData = source.Slice((int)box.PayloadOffset + iccOffset, (int)iccSize).ToArray();
            icc = CreateColourProperty(5, "prof", 0, 0, 0, false, iccData);
            properties.Add(icc);
        }

        ParsedItemProperty? alphaConfig = null;
        ParsedItemProperty? alphaAux = null;
        ParsedItemProperty? alphaPixels = null;
        if (hasAlpha)
        {
            if (alphaSize > 0)
            {
                alphaConfig = CreateAv1Configuration(6, alphaConfiguration);
                if (alphaConfig == null
                    || !MatchesMiniConfiguration(
                        alphaConfig.GetAv1Configuration(),
                        bitDepth,
                        0,
                        false,
                        false))
                {
                    return Fail(boxes, box, AvifResultCode.InvalidAv1Bitstream,
                        "The compact alpha AV1 configuration is invalid.");
                }

                properties.Add(alphaConfig);
            }

            alphaAux = CreateAuxiliaryProperty(7);
            properties.Add(alphaAux);
            alphaPixels = CreatePixelProperty(8, bitDepth, 1);
            properties.Add(alphaPixels);
        }

        ParsedItemProperty? rotation = CreateRotationProperty(orientation);
        ParsedItemProperty? mirror = CreateMirrorProperty(orientation);
        if (rotation != null)
        {
            properties.Add(rotation);
        }

        if (mirror != null)
        {
            properties.Add(mirror);
        }

        List<ParsedPropertyAssociation> mainAssociations = new List<ParsedPropertyAssociation>();
        AddAssociation(mainAssociations, mainConfig, true);
        AddAssociation(mainAssociations, spatial, false);
        AddAssociation(mainAssociations, pixels, false);
        AddAssociation(mainAssociations, colour, true);
        AddAssociation(mainAssociations, icc, true);
        AddAssociation(mainAssociations, rotation, true);
        AddAssociation(mainAssociations, mirror, true);

        List<ParsedItem> items = new List<ParsedItem>();
        ParsedItem main = CreateItem(1, false, "av01", string.Empty)
            .WithLocation(CreateLocation(1, box.PayloadOffset + mainOffset, mainSize))
            .WithProperties(mainAssociations);
        items.Add(main);

        List<ParsedItemReference> references = new List<ParsedItemReference>();
        if (hasAlpha)
        {
            List<ParsedPropertyAssociation> alphaAssociations = new List<ParsedPropertyAssociation>();
            AddAssociation(alphaAssociations, alphaConfig, true);
            AddAssociation(alphaAssociations, spatial, false);
            AddAssociation(alphaAssociations, alphaAux, false);
            AddAssociation(alphaAssociations, alphaPixels, false);
            AddAssociation(alphaAssociations, rotation, true);
            AddAssociation(alphaAssociations, mirror, true);
            ParsedItem alpha = CreateItem(2, true, "av01", string.Empty)
                .WithLocation(CreateLocation(2, box.PayloadOffset + alphaOffset, alphaSize))
                .WithProperties(alphaAssociations);
            items.Add(alpha);
            references.Add(CreateReference(ParsedItemReferenceKind.Auxiliary, "auxl", 2, 1));
            if (alphaPremultiplied)
            {
                references.Add(CreateReference(ParsedItemReferenceKind.Premultiplied, "prem", 1, 2));
            }
        }

        if (exifSize > 0)
        {
            ParsedItem exif = CreateItem(6, true, "Exif", string.Empty)
                .WithLocation(CreateLocation(6, box.PayloadOffset + exifOffset, exifSize));
            items.Add(exif);
            references.Add(CreateReference(ParsedItemReferenceKind.ContentDescription, "cdsc", 6, 1));
        }

        if (xmpSize > 0)
        {
            ParsedItem xmp = CreateItem(7, true, "mime", "application/rdf+xml")
                .WithLocation(CreateLocation(7, box.PayloadOffset + xmpOffset, xmpSize));
            items.Add(xmp);
            references.Add(CreateReference(ParsedItemReferenceKind.ContentDescription, "cdsc", 7, 1));
        }

        WireReferences(items, references);
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            AvifResultCode consistency = MetaBoxParser.ValidateAv1PropertyConsistency(items[itemIndex]);
            if (consistency == AvifResultCode.Success)
            {
                consistency = MetaBoxParser.ValidateHdrPropertyConsistency(items[itemIndex]);
            }
            if (consistency != AvifResultCode.Success)
            {
                return Fail(boxes, box, consistency,
                    "The compact AV1 configuration disagrees with minimized image properties.");
            }

            itemIndex++;
        }

        ItemDependencyGraphCreateResult graph = ParsedItemDependencyGraph.Create(items, boxes.Resources.Limits);
        if (graph.Code != AvifResultCode.Success)
        {
            return Fail(boxes, box, graph.Code,
                "The expanded minimized item dependency graph is invalid.");
        }

        ParsedMeta meta = new ParsedMeta(
            "pict",
            string.Empty,
            true,
            1,
            items,
            properties,
            references,
            new List<ParsedDataReference>(),
            new List<ParsedEntityGroup>(),
            null,
            new List<ParsedDataBox>(),
            graph.Graph,
            boxes.Resources.Limits);
        return new MetaParseResult(AvifResultCode.Success, meta, boxes);
    }

    internal static AvifResultCode CheckLimits(
        ResourceGuard resources,
        int width,
        int height,
        int orientation,
        bool hasAlpha,
        bool hasExif,
        bool hasXmp,
        long iccSize,
        long exifSize,
        long xmpSize)
    {
        int itemCount = 1 + (hasAlpha ? 1 : 0) + (hasExif ? 1 : 0) + (hasXmp ? 1 : 0);
        int propertyCount = 4 + (iccSize > 0 ? 1 : 0) + (hasAlpha ? 3 : 0);
        if (orientation == 3 || orientation == 5 || orientation == 6
            || orientation == 7 || orientation == 8)
        {
            propertyCount++;
        }

        if (orientation == 2 || orientation == 4 || orientation == 5 || orientation == 7)
        {
            propertyCount++;
        }
        int referenceCount = (hasAlpha ? 1 : 0) + (hasExif ? 1 : 0) + (hasXmp ? 1 : 0);
        AvifResultCode code = resources.CheckImageDimensions(width, height);
        if (code == AvifResultCode.Success)
        {
            code = resources.CountItems(itemCount);
        }

        if (code == AvifResultCode.Success)
        {
            code = resources.CountProperties(propertyCount);
        }

        if (code == AvifResultCode.Success)
        {
            code = resources.CountReferences(referenceCount + (hasAlpha ? 1 : 0));
        }

        if (code == AvifResultCode.Success && iccSize > 0)
        {
            code = resources.CountIccBytes(iccSize);
        }

        if (code == AvifResultCode.Success && exifSize > 0)
        {
            code = resources.CountExifBytes(exifSize);
        }

        if (code == AvifResultCode.Success && xmpSize > 0)
        {
            code = resources.CountXmpBytes(xmpSize);
        }

        return code;
    }

    internal static ParsedItem CreateItem(uint id, bool hidden, string type, string contentType)
    {
        return new ParsedItem(id, 3, hidden, 0, type, string.Empty, contentType, string.Empty, string.Empty);
    }

    internal static ParsedItemLocation CreateLocation(uint id, long offset, long length)
    {
        List<ParsedItemExtent> extents = new List<ParsedItemExtent>();
        extents.Add(new ParsedItemExtent(0, (ulong)offset, (ulong)length, true, offset, length));
        return new ParsedItemLocation(id, 0, 0, 0, true, (ulong)length, extents);
    }

    internal static ParsedItemProperty? CreateAv1Configuration(int index, byte[] data)
    {
        IsoBox box = new IsoBox();
        box.Type = "av1C";
        box.PayloadOffset = 0;
        box.PayloadSize = data.Length;
        Av1ItemConfigurationParseResult parsed =
            Av1ItemConfigurationBox.ParsePayload(data);
        if (parsed.Code != AvifResultCode.Success)
        {
            return null;
        }

        ParsedItemProperty property = new ParsedItemProperty(index, "av1C", IsoBoxType.GetValue("av1C"), false, System.Array.Empty<byte>(), (byte[])data.Clone());
        property.SetAv1Configuration(parsed.Configuration);
        return property;
    }

    internal static bool MatchesMiniConfiguration(
        ParsedAv1Configuration? configuration,
        int bitDepth,
        int chromaSubsampling,
        bool horizontalCentered,
        bool verticalCentered)
    {
        if (configuration == null || configuration.GetBitDepth() != bitDepth)
        {
            return false;
        }

        bool expectedMonochrome = chromaSubsampling == 0;
        bool expectedX = chromaSubsampling == 0
            || chromaSubsampling == 1
            || chromaSubsampling == 2;
        bool expectedY = chromaSubsampling == 0 || chromaSubsampling == 1;
        int expectedPosition = 0;
        if (chromaSubsampling == 1 && !horizontalCentered)
        {
            expectedPosition = verticalCentered ? 1 : 2;
        }

        return configuration.IsMonochrome() == expectedMonochrome
            && configuration.HasChromaSubsamplingX() == expectedX
            && configuration.HasChromaSubsamplingY() == expectedY
            && configuration.GetChromaSamplePosition() == expectedPosition;
    }

    internal static ParsedItemProperty CreateSpatialProperty(int index, int width, int height)
    {
        ParsedItemProperty property = NewProperty(index, "ispe");
        property.SetSpatialExtents(new ParsedImageSpatialExtents((uint)width, (uint)height));
        return property;
    }

    internal static ParsedItemProperty CreatePixelProperty(int index, int bitDepth, int count)
    {
        List<ParsedPixelChannel> channels = new List<ParsedPixelChannel>();
        int channel = 0;
        while (channel < count)
        {
            channels.Add(new ParsedPixelChannel(bitDepth, 0, 0, false, 0, 0, string.Empty));
            channel++;
        }

        ParsedItemProperty property = NewProperty(index, "pixi");
        property.SetPixelInformation(new ParsedPixelInformation(false, channels));
        return property;
    }

    internal static ParsedItemProperty CreateColourProperty(int index, string type, int primaries, int transfer, int matrix, bool fullRange, byte[] data)
    {
        ParsedItemProperty property = NewProperty(index, "colr");
        AdditionalItemPropertyParseResult parsed = new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.ColourInformation = new ParsedColourInformation(type, type == "nclx", type == "nclx", primaries, transfer, matrix, fullRange, data);
        property.SetAdditionalProperties(parsed);
        return property;
    }

    internal static ParsedItemProperty CreateAuxiliaryProperty(int index)
    {
        ParsedItemProperty property = NewProperty(index, "auxC");
        AdditionalItemPropertyParseResult parsed = new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.AuxiliaryType = new ParsedAuxiliaryType("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha", System.Array.Empty<byte>());
        property.SetAdditionalProperties(parsed);
        return property;
    }

    internal static ParsedItemProperty? CreateRotationProperty(int orientation)
    {
        int angle = orientation == 3 ? 2 : orientation == 5 || orientation == 8 ? 1 : orientation == 6 || orientation == 7 ? 3 : 0;
        if (angle == 0)
        {
            return null;
        }

        ParsedItemProperty property = NewProperty(9, "irot");
        AdditionalItemPropertyParseResult parsed = new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.ImageRotation = new ParsedImageRotation(angle);
        property.SetAdditionalProperties(parsed);
        return property;
    }

    internal static ParsedItemProperty? CreateMirrorProperty(int orientation)
    {
        if (orientation != 2 && orientation != 4 && orientation != 5 && orientation != 7)
        {
            return null;
        }

        ParsedItemProperty property = NewProperty(10, "imir");
        AdditionalItemPropertyParseResult parsed = new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.ImageMirror = new ParsedImageMirror(orientation == 2 ? 1 : 0);
        property.SetAdditionalProperties(parsed);
        return property;
    }

    internal static ParsedItemProperty NewProperty(int index, string type)
    {
        return new ParsedItemProperty(index, type, IsoBoxType.GetValue(type), false, System.Array.Empty<byte>(), System.Array.Empty<byte>());
    }

    internal static void AddAssociation(List<ParsedPropertyAssociation> associations, ParsedItemProperty? property, bool essential)
    {
        if (property != null)
        {
            associations.Add(new ParsedPropertyAssociation(property.GetCanonicalIndex(), essential, property));
        }
    }

    internal static ParsedItemReference CreateReference(ParsedItemReferenceKind kind, string type, uint from, uint to)
    {
        List<uint> targets = new List<uint>();
        targets.Add(to);
        return new ParsedItemReference(kind, type, IsoBoxType.GetValue(type), from, targets);
    }

    internal static void WireReferences(List<ParsedItem> items, List<ParsedItemReference> references)
    {
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            List<ParsedItemReference> outgoing = new List<ParsedItemReference>();
            List<ParsedItemReference> incoming = new List<ParsedItemReference>();
            int referenceIndex = 0;
            while (referenceIndex < references.Count)
            {
                ParsedItemReference reference = references[referenceIndex];
                if (reference.GetFromItemId() == items[itemIndex].GetItemId())
                {
                    outgoing.Add(reference);
                }

                if (reference.GetTargetItemId(0) == items[itemIndex].GetItemId())
                {
                    incoming.Add(reference);
                }

                referenceIndex++;
            }

            items[itemIndex] = items[itemIndex].WithReferences(outgoing, incoming);
            itemIndex++;
        }
    }

    internal static string ReadFourCc(BitReader reader, ReadOnlySpan<byte> payload)
    {
        uint value = (uint)reader.ReadBits(payload, 32);
        return new FourCc(value).Text;
    }

    internal static MetaParseResult Fail(IsoBoxParseResult boxes, IsoBox box, AvifResultCode code, string message)
    {
        boxes.Diagnostics.Context.PushBox("mini");
        boxes.Diagnostics.Context.SetLocation(box.PayloadOffset, AvifDiagnostic.UnknownBitOffset);
        boxes.Diagnostics.Add(code, AvifDiagnosticSeverity.Error, AvifDiagnosticCategory.Container, message);
        boxes.Diagnostics.Context.ClearLocation();
        boxes.Diagnostics.Context.PopBox();
        return new MetaParseResult(code, null, boxes);
    }
}
