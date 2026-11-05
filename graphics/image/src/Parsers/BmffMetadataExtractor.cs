using System;
using System.Collections.Generic;
using System.Text;

namespace image;

internal class BmffMetadataExtractor
{
    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<BmffBox> boxes, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || boxes == null)
        {
            return;
        }

        BmffMetadataState state = new BmffMetadataState();
        BmffBox? meta = FindFirstBox(boxes, "meta");
        if (meta != null)
        {
            DecodeMeta(document, reader, meta, state, options);
            ResolveItems(document, reader, state, options);
            AddItemDirectories(document, state);
            AddFrames(document, state, options);
            ExtractItemPayloads(document, reader, state, options);
        }

        ExtractJumbfBoxes(document, reader, boxes, options);
        if (document.Format != null && document.Format.Kind == ImageFormatKind.Cr3)
        {
            Cr3MetadataExtractor.Extract(document, reader, boxes, options);
        }
    }

    internal static void DecodeMeta(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox meta, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        BmffBox? idat = FindDirectChild(meta, "idat");
        if (idat != null)
        {
            state.IdatDataOffset = idat.DataOffset;
            state.IdatDataLength = idat.DataSize;
        }

        BmffBox? pitm = FindDirectChild(meta, "pitm");
        if (pitm != null)
        {
            DecodePrimaryItem(document, reader, pitm, state, options);
        }

        BmffBox? iinf = FindDirectChild(meta, "iinf");
        if (iinf != null)
        {
            foreach (BmffBox infe in iinf.Children)
            {
                if (infe != null && infe.Type == "infe" && state.Items.Count < GetMaxCount(options))
                {
                    DecodeItemInfo(document, reader, infe, state, options);
                }
            }
        }

        BmffBox? iloc = FindDirectChild(meta, "iloc");
        if (iloc != null)
        {
            DecodeItemLocations(document, reader, iloc, state, options);
        }

        BmffBox? iprp = FindDirectChild(meta, "iprp");
        if (iprp != null)
        {
            DecodeItemProperties(document, reader, iprp, state, options);
        }

        BmffBox? iref = FindDirectChild(meta, "iref");
        if (iref != null)
        {
            DecodeItemReferences(document, reader, iref, state, options);
        }
    }

    internal static void DecodePrimaryItem(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, options, "bmff.pitm.payload.limit");
        if (data.Length < 6)
        {
            AddTruncated(document, box, "bmff.pitm.truncated", "ISO BMFF primary item box was truncated.", 6);
            return;
        }

        BmffByteReader input = new BmffByteReader(data);
        int version = input.ReadByte();
        input.ReadUnsigned(3);
        if (version != 0 && version != 1)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.pitm.version.unsupported", "ISO BMFF primary item version was not supported.", "pitm");
            return;
        }

        state.PrimaryItemId = version == 0 ? input.ReadUnsigned16() : input.ReadUnsigned32();
        if (!input.Valid)
        {
            AddTruncated(document, box, "bmff.pitm.truncated", "ISO BMFF primary item identifier was truncated.", version == 0 ? 6 : 8);
        }
    }

    internal static void DecodeItemInfo(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, options, "bmff.infe.payload.limit");
        if (data.Length < 8)
        {
            AddTruncated(document, box, "bmff.infe.truncated", "ISO BMFF item info entry was truncated.", 8);
            return;
        }

        BmffByteReader input = new BmffByteReader(data);
        int version = input.ReadByte();
        uint flags = (uint)input.ReadUnsigned(3);
        BmffItem item = new BmffItem();
        item.Hidden = (flags & 1) != 0;
        if (version == 2 || version == 3)
        {
            item.Id = version == 2 ? input.ReadUnsigned16() : input.ReadUnsigned32();
            item.ProtectionIndex = (int)input.ReadUnsigned16();
            item.Type = input.ReadFourCc();
            item.Name = input.ReadNullTerminatedLatin1();
            if (item.Type == "mime")
            {
                item.ContentType = input.ReadNullTerminatedLatin1();
                if (input.Remaining() > 0)
                {
                    item.ContentEncoding = input.ReadNullTerminatedLatin1();
                }
            }
            else if (item.Type == "uri ")
            {
                item.ContentType = input.ReadNullTerminatedLatin1();
            }
        }
        else if (version == 0 || version == 1)
        {
            item.Id = input.ReadUnsigned16();
            item.ProtectionIndex = (int)input.ReadUnsigned16();
            item.Name = input.ReadNullTerminatedLatin1();
            item.ContentType = input.ReadNullTerminatedLatin1();
            if (input.Remaining() > 0)
            {
                item.ContentEncoding = input.ReadNullTerminatedLatin1();
            }

            item.Type = item.ContentType.Length > 0 ? "mime" : "";
        }
        else
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.infe.version.unsupported", "ISO BMFF item info version was not supported.", "infe");
            return;
        }

        if (!input.Valid)
        {
            AddTruncated(document, box, "bmff.infe.truncated", "ISO BMFF item info entry fields were truncated.", (long)box.DataSize);
        }

        BmffItem? existing = FindItem(state, item.Id);
        if (existing == null)
        {
            state.Items.Add(item);
        }
        else
        {
            CopyItemInfo(existing, item);
        }
    }

    internal static void DecodeItemLocations(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, options, "bmff.iloc.payload.limit");
        BmffByteReader input = new BmffByteReader(data);
        if (!input.CanRead(8))
        {
            AddTruncated(document, box, "bmff.iloc.truncated", "ISO BMFF item location header was truncated.", 8);
            return;
        }

        int version = input.ReadByte();
        input.ReadUnsigned(3);
        if (version < 0 || version > 2)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.iloc.version.unsupported", "ISO BMFF item location version was not supported.", "iloc");
            return;
        }

        int sizes = input.ReadByte();
        int sizes2 = input.ReadByte();
        int offsetSize = (sizes >> 4) & 15;
        int lengthSize = sizes & 15;
        int baseOffsetSize = (sizes2 >> 4) & 15;
        int indexSize = version == 1 || version == 2 ? sizes2 & 15 : 0;
        if (!ValidIntegerSize(offsetSize) || !ValidIntegerSize(lengthSize) || !ValidIntegerSize(baseOffsetSize) || !ValidIntegerSize(indexSize))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.iloc.field-size.invalid", "ISO BMFF item location used an unsupported integer field size.", "iloc", box.DataOffset + 4, 2);
            return;
        }

        ulong itemCount = version < 2 ? input.ReadUnsigned16() : input.ReadUnsigned32();
        ulong maximum = BmffBrandDetector.ToUnsignedCount(GetMaxCount(options));
        if (itemCount > maximum)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.iloc.item.count.limit", "ISO BMFF item location count exceeded the configured tag count limit.", "iloc", box.DataOffset + 6, BmffBoxTraverser.ToDiagnosticLength(itemCount));
            itemCount = maximum;
        }

        ulong itemIndex = 0;
        while (itemIndex < itemCount && input.Valid)
        {
            uint itemId = version < 2 ? input.ReadUnsigned16() : input.ReadUnsigned32();
            int constructionMethod = 0;
            if (version == 1 || version == 2)
            {
                constructionMethod = (int)(input.ReadUnsigned16() & 15);
            }

            int dataReferenceIndex = (int)input.ReadUnsigned16();
            ulong baseOffset = input.ReadUnsigned(baseOffsetSize);
            ulong extentCount = input.ReadUnsigned16();
            if (extentCount > maximum)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.iloc.extent.count.limit", "ISO BMFF item extent count exceeded the configured tag count limit.", "iloc", box.DataOffset + input.Position - 2, BmffBoxTraverser.ToDiagnosticLength(extentCount));
                return;
            }

            BmffItem? item = GetOrCreateItem(state, itemId, options);
            if (item == null)
            {
                return;
            }

            ulong extentIndex = 0;
            while (extentIndex < extentCount && input.Valid)
            {
                BmffExtent extent = new BmffExtent();
                extent.ConstructionMethod = constructionMethod;
                extent.DataReferenceIndex = dataReferenceIndex;
                extent.Index = input.ReadUnsigned(indexSize);
                extent.Offset = input.ReadUnsigned(offsetSize);
                extent.Length = input.ReadUnsigned(lengthSize);
                ResolveExtent(document, state, box, extent, baseOffset, reader.AccessibleLength());
                item.Extents.Add(extent);
                extentIndex = extentIndex + 1;
            }

            itemIndex = itemIndex + 1;
        }

        if (!input.Valid)
        {
            AddTruncated(document, box, "bmff.iloc.truncated", "ISO BMFF item location fields were truncated.", (long)box.DataSize);
        }
    }

    internal static void ResolveExtent(ImageMetadataDocument document, BmffMetadataState state, BmffBox box, BmffExtent extent, ulong baseOffset, long fileLength)
    {
        if (extent.DataReferenceIndex != 0)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.iloc.external-reference.unsupported", "ISO BMFF external item data reference was not followed.", "iloc");
            return;
        }

        ulong origin = 0;
        if (extent.ConstructionMethod == 1)
        {
            if (state.IdatDataOffset < 0)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.iloc.idat.missing", "ISO BMFF item location referenced a missing idat box.", "iloc", box.DataOffset, (long)box.DataSize);
                return;
            }

            origin = (ulong)state.IdatDataOffset;
        }
        else if (extent.ConstructionMethod != 0)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.iloc.construction.unsupported", "ISO BMFF item location construction method was not supported.", "iloc");
            return;
        }

        if (baseOffset > ulong.MaxValue - extent.Offset || origin > ulong.MaxValue - baseOffset - extent.Offset)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.iloc.offset.overflow", "ISO BMFF item extent offset overflowed.", "iloc", box.DataOffset, (long)box.DataSize);
            return;
        }

        ulong absolute = origin + baseOffset + extent.Offset;
        if (extent.ConstructionMethod == 1)
        {
            ulong relative = baseOffset + extent.Offset;
            if (relative > state.IdatDataLength || extent.Length > state.IdatDataLength - relative)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.iloc.idat.extent.truncated", "ISO BMFF item extent exceeded the idat payload.", "iloc", BmffBoxTraverser.ToDiagnosticLength(absolute), BmffBoxTraverser.ToDiagnosticLength(extent.Length));
                return;
            }
        }

        if (absolute > 9223372036854775807 || absolute > (ulong)fileLength || extent.Length > (ulong)fileLength - absolute)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.iloc.extent.truncated", "ISO BMFF item extent exceeded the available source bytes.", "iloc", BmffBoxTraverser.ToDiagnosticLength(absolute), BmffBoxTraverser.ToDiagnosticLength(extent.Length));
            return;
        }

        extent.AbsoluteOffset = (long)absolute;
        extent.Resolved = true;
    }

    internal static void DecodeItemProperties(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox iprp, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        BmffBox? ipco = FindDirectChild(iprp, "ipco");
        if (ipco != null)
        {
            int index = 1;
            foreach (BmffBox propertyBox in ipco.Children)
            {
                if (propertyBox != null && index <= GetMaxCount(options))
                {
                    BmffItemProperty property = DecodeProperty(document, reader, propertyBox, index, options);
                    state.Properties.Add(property);
                }

                index = index + 1;
            }
        }

        foreach (BmffBox child in iprp.Children)
        {
            if (child != null && child.Type == "ipma")
            {
                DecodePropertyAssociations(document, reader, child, state, options);
            }
        }
    }

    internal static BmffItemProperty DecodeProperty(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, int index, ImageMetadataReaderOptions options)
    {
        BmffItemProperty property = new BmffItemProperty();
        property.Index = index;
        property.Type = box.Type;
        property.Box = box;
        byte[] data = ReadBoxData(document, reader, box, options, "bmff.property.payload.limit");
        BmffByteReader input = new BmffByteReader(data);

        if (box.Type == "ispe")
        {
            if (RequirePropertyBytes(document, box, data, 12))
            {
                input.ReadUnsigned32();
                property.Width = ToDimension(document, box, input.ReadUnsigned32(), "width");
                property.Height = ToDimension(document, box, input.ReadUnsigned32(), "height");
            }
        }
        else if (box.Type == "pixi")
        {
            if (RequirePropertyBytes(document, box, data, 5))
            {
                input.ReadUnsigned32();
                property.ChannelCount = input.ReadByte();
                if (property.ChannelCount > input.Remaining())
                {
                    AddTruncated(document, box, "bmff.pixi.truncated", "ISO BMFF pixel information channels were truncated.", 5 + property.ChannelCount);
                }
                else
                {
                    int channel = 0;
                    while (channel < property.ChannelCount)
                    {
                        int bits = input.ReadByte();
                        if (property.BitDepth == 0 || bits > property.BitDepth)
                        {
                            property.BitDepth = bits;
                        }

                        channel = channel + 1;
                    }
                }
            }
        }
        else if (box.Type == "irot")
        {
            if (RequirePropertyBytes(document, box, data, 1))
            {
                property.RotationDegrees = (input.ReadByte() & 3) * 90;
            }
        }
        else if (box.Type == "imir")
        {
            if (RequirePropertyBytes(document, box, data, 1))
            {
                property.MirrorAxis = input.ReadByte() & 1;
            }
        }
        else if (box.Type == "clap")
        {
            if (RequirePropertyBytes(document, box, data, 32))
            {
                property.CleanWidthNumerator = input.ReadUnsigned32();
                property.CleanWidthDenominator = input.ReadUnsigned32();
                property.CleanHeightNumerator = input.ReadUnsigned32();
                property.CleanHeightDenominator = input.ReadUnsigned32();
                property.HorizontalOffsetNumerator = input.ReadSigned32();
                property.HorizontalOffsetDenominator = input.ReadUnsigned32();
                property.VerticalOffsetNumerator = input.ReadSigned32();
                property.VerticalOffsetDenominator = input.ReadUnsigned32();
            }
        }
        else if (box.Type == "pasp")
        {
            if (RequirePropertyBytes(document, box, data, 8))
            {
                property.HorizontalSpacing = input.ReadUnsigned32();
                property.VerticalSpacing = input.ReadUnsigned32();
            }
        }
        else if (box.Type == "colr")
        {
            DecodeColorProperty(document, box, input, property);
        }
        else if (box.Type == "auxC")
        {
            if (RequirePropertyBytes(document, box, data, 5))
            {
                input.ReadUnsigned32();
                property.AuxiliaryType = input.ReadNullTerminatedLatin1();
                if (!input.Valid)
                {
                    AddTruncated(document, box, "bmff.auxc.type.truncated", "ISO BMFF auxiliary type string was not terminated.", (long)box.DataSize);
                }
            }
        }

        return property;
    }

    internal static void DecodeColorProperty(ImageMetadataDocument document, BmffBox box, BmffByteReader input, BmffItemProperty property)
    {
        if (!input.CanRead(4))
        {
            AddTruncated(document, box, "bmff.colr.truncated", "ISO BMFF colour information type was truncated.", 4);
            return;
        }

        property.ColorType = input.ReadFourCc();
        if (property.ColorType == "nclx" || property.ColorType == "nclc")
        {
            int required = property.ColorType == "nclx" ? 7 : 6;
            if (!input.CanRead(required))
            {
                AddTruncated(document, box, "bmff.colr.nclx.truncated", "ISO BMFF coded color information was truncated.", 4 + required);
                return;
            }

            property.ColorPrimaries = (int)input.ReadUnsigned16();
            property.TransferCharacteristics = (int)input.ReadUnsigned16();
            property.MatrixCoefficients = (int)input.ReadUnsigned16();
            if (property.ColorType == "nclx")
            {
                property.FullRange = (input.ReadByte() & 128) != 0;
            }
        }
        else if (property.ColorType == "rICC" || property.ColorType == "prof")
        {
            property.ProfileData = input.ReadRemainingBytes();
        }
    }

    internal static void DecodePropertyAssociations(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, options, "bmff.ipma.payload.limit");
        BmffByteReader input = new BmffByteReader(data);
        if (!input.CanRead(8))
        {
            AddTruncated(document, box, "bmff.ipma.truncated", "ISO BMFF item property association header was truncated.", 8);
            return;
        }

        int version = input.ReadByte();
        uint flags = (uint)input.ReadUnsigned(3);
        if (version != 0 && version != 1)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.ipma.version.unsupported", "ISO BMFF item property association version was not supported.", "ipma");
            return;
        }

        bool largeIndex = (flags & 1) != 0;
        ulong entryCount = input.ReadUnsigned32();
        ulong maximum = BmffBrandDetector.ToUnsignedCount(GetMaxCount(options));
        if (entryCount > maximum)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.ipma.entry.count.limit", "ISO BMFF property association entry count exceeded the configured tag count limit.", "ipma", box.DataOffset + 4, BmffBoxTraverser.ToDiagnosticLength(entryCount));
            entryCount = maximum;
        }

        ulong entryIndex = 0;
        while (entryIndex < entryCount && input.Valid)
        {
            uint itemId = version < 1 ? input.ReadUnsigned16() : input.ReadUnsigned32();
            int associationCount = input.ReadByte();
            BmffItem? item = GetOrCreateItem(state, itemId, options);
            int associationIndex = 0;
            while (associationIndex < associationCount && input.Valid)
            {
                uint value = largeIndex ? input.ReadUnsigned16() : (uint)input.ReadByte();
                BmffPropertyAssociation association = new BmffPropertyAssociation();
                association.Essential = largeIndex ? (value & 32768) != 0 : (value & 128) != 0;
                association.Index = largeIndex ? (int)(value & 32767) : (int)(value & 127);
                if (association.Index > 0 && item != null)
                {
                    item.Properties.Add(association);
                }

                associationIndex = associationIndex + 1;
            }

            entryIndex = entryIndex + 1;
        }

        if (!input.Valid)
        {
            AddTruncated(document, box, "bmff.ipma.truncated", "ISO BMFF item property associations were truncated.", (long)box.DataSize);
        }
    }

    internal static void DecodeItemReferences(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox iref, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] header = ReadPrefix(document, reader, iref.DataOffset, iref.DataSize, 1, "iref");
        int version = header.Length == 1 ? header[0] : 0;
        if (version != 0 && version != 1)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmff.iref.version.unsupported", "ISO BMFF item reference version was not supported.", "iref");
            return;
        }

        foreach (BmffBox child in iref.Children)
        {
            if (child == null || state.References.Count >= GetMaxCount(options))
            {
                continue;
            }

            byte[] data = ReadBoxData(document, reader, child, options, "bmff.iref.payload.limit");
            BmffByteReader input = new BmffByteReader(data);
            BmffItemReference reference = new BmffItemReference();
            reference.Type = child.Type;
            reference.FromItemId = version == 0 ? input.ReadUnsigned16() : input.ReadUnsigned32();
            ulong referenceCount = input.ReadUnsigned16();
            ulong maximum = BmffBrandDetector.ToUnsignedCount(GetMaxCount(options));
            if (referenceCount > maximum)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.iref.count.limit", "ISO BMFF item reference count exceeded the configured tag count limit.", child.Type, child.DataOffset, BmffBoxTraverser.ToDiagnosticLength(referenceCount));
                referenceCount = maximum;
            }

            ulong index = 0;
            while (index < referenceCount && input.Valid)
            {
                reference.ToItemIds.Add(version == 0 ? input.ReadUnsigned16() : input.ReadUnsigned32());
                index = index + 1;
            }

            if (!input.Valid)
            {
                AddTruncated(document, child, "bmff.iref.truncated", "ISO BMFF item reference was truncated.", (long)child.DataSize);
            }
            else
            {
                state.References.Add(reference);
            }
        }
    }

    internal static void ResolveItems(ImageMetadataDocument document, BoundedStreamReader reader, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        foreach (BmffItem item in state.Items)
        {
            item.Primary = item.Id == state.PrimaryItemId;
            foreach (BmffPropertyAssociation association in item.Properties)
            {
                BmffItemProperty? property = FindProperty(state, association.Index);
                if (property != null)
                {
                    ApplyProperty(item, property);
                    ExtractIccProperty(document, property, item, options);
                }
                else if (association.Essential)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.ipma.property.missing", "ISO BMFF item referenced a missing essential property.", "ipma", -1, association.Index);
                }
            }
        }

        foreach (BmffItemReference reference in state.References)
        {
            BmffItem? from = FindItem(state, reference.FromItemId);
            if (from == null)
            {
                continue;
            }

            if (reference.Type == "thmb")
            {
                from.Thumbnail = true;
            }
            else if (reference.Type == "auxl")
            {
                from.Auxiliary = true;
            }
        }
    }

    internal static void ApplyProperty(BmffItem item, BmffItemProperty property)
    {
        if (property.Type == "ispe")
        {
            item.Width = property.Width;
            item.Height = property.Height;
        }
        else if (property.Type == "pixi")
        {
            item.ChannelCount = property.ChannelCount;
            item.BitDepth = property.BitDepth;
        }
        else if (property.Type == "irot")
        {
            item.RotationDegrees = property.RotationDegrees;
        }
        else if (property.Type == "imir")
        {
            item.MirrorAxis = property.MirrorAxis;
        }
        else if (property.Type == "colr")
        {
            item.ColorType = property.ColorType;
            item.ColorPrimaries = property.ColorPrimaries;
            item.TransferCharacteristics = property.TransferCharacteristics;
            item.MatrixCoefficients = property.MatrixCoefficients;
            item.FullRange = property.FullRange;
        }
        else if (property.Type == "auxC")
        {
            item.AuxiliaryType = property.AuxiliaryType;
            item.Auxiliary = true;
        }
    }

    internal static void AddItemDirectories(ImageMetadataDocument document, BmffMetadataState state)
    {
        if (state.Items.Count == 0)
        {
            return;
        }

        MetadataDirectory root = document.AddDirectoryWithName(MetadataFamily.Bmff, "Items", "BMFF Items");
        root.Path = "BMFF/Items";
        root.Offset = -1;
        root.Length = 0;
        AddTag(root, "Bmff.Items.Count", "Item Count", MetadataValue.FromInteger(state.Items.Count), -1, 0, "");
        AddTag(root, "Bmff.Items.PrimaryItemId", "Primary Item ID", MetadataValue.FromUnsignedInteger(state.PrimaryItemId), -1, 0, "");

        int repeat = 0;
        foreach (BmffItem item in OrderedItems(state))
        {
            MetadataDirectory directory = root.AddChildWithName(MetadataFamily.Bmff, "Item", "BMFF Item " + item.Id.ToString());
            directory.Path = root.Path + "/" + item.Id.ToString();
            directory.RepeatIndex = repeat;
            SetItemRange(directory, item);
            AddItemTags(directory, item, state);
            repeat = repeat + 1;
        }
    }

    internal static void AddItemTags(MetadataDirectory directory, BmffItem item, BmffMetadataState state)
    {
        AddTag(directory, "Bmff.Item.Id", "Item ID", MetadataValue.FromUnsignedInteger(item.Id), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.Type", "Item Type", MetadataValue.FromString(item.Type), directory.Offset, 4, item.Type);
        AddTag(directory, "Bmff.Item.Name", "Item Name", MetadataValue.FromString(item.Name), directory.Offset, 0, item.Name);
        AddTag(directory, "Bmff.Item.ContentType", "Content Type", MetadataValue.FromString(item.ContentType), directory.Offset, 0, item.ContentType);
        AddTag(directory, "Bmff.Item.ContentEncoding", "Content Encoding", MetadataValue.FromString(item.ContentEncoding), directory.Offset, 0, item.ContentEncoding);
        AddTag(directory, "Bmff.Item.Primary", "Primary", MetadataValue.FromBoolean(item.Primary), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.Hidden", "Hidden", MetadataValue.FromBoolean(item.Hidden), directory.Offset, 0, "");
        AddTag(directory, "Bmff.Item.Thumbnail", "Thumbnail", MetadataValue.FromBoolean(item.Thumbnail), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.Auxiliary", "Auxiliary", MetadataValue.FromBoolean(item.Auxiliary), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.AuxiliaryType", "Auxiliary Type", MetadataValue.FromString(item.AuxiliaryType), directory.Offset, 0, item.AuxiliaryType);
        AddTag(directory, "Bmff.Item.Width", "Width", MetadataValue.FromInteger(item.Width), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.Height", "Height", MetadataValue.FromInteger(item.Height), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.ChannelCount", "Channel Count", MetadataValue.FromInteger(item.ChannelCount), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.BitDepth", "Bit Depth", MetadataValue.FromInteger(item.BitDepth), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.RotationDegreesCounterClockwise", "Rotation Degrees Counter-Clockwise", MetadataValue.FromInteger(item.RotationDegrees), directory.Offset, directory.Length, item.RotationDegrees.ToString());
        AddTag(directory, "Bmff.Item.MirrorAxis", "Mirror Axis", MetadataValue.FromInteger(item.MirrorAxis), directory.Offset, directory.Length, MirrorAxisName(item.MirrorAxis));
        if (IsImageItem(item))
        {
            ImageOrientation orientation = ImageMetadataNormalizerCore.OrientationFromHeifTransform(item.RotationDegrees, item.MirrorAxis);
            AddTag(directory, "Bmff.Item.Orientation", "Orientation", MetadataValue.FromInteger((int)orientation), directory.Offset, directory.Length, orientation.ToString());
        }
        AddTag(directory, "Bmff.Item.ColorType", "Color Type", MetadataValue.FromString(item.ColorType), directory.Offset, directory.Length, item.ColorType);
        AddTag(directory, "Bmff.Item.ColorPrimaries", "Color Primaries", MetadataValue.FromInteger(item.ColorPrimaries), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.TransferCharacteristics", "Transfer Characteristics", MetadataValue.FromInteger(item.TransferCharacteristics), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.MatrixCoefficients", "Matrix Coefficients", MetadataValue.FromInteger(item.MatrixCoefficients), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.FullRange", "Full Range", MetadataValue.FromBoolean(item.FullRange), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.ExtentCount", "Extent Count", MetadataValue.FromInteger(item.Extents.Count), directory.Offset, directory.Length, "");
        AddTag(directory, "Bmff.Item.TotalExtentLength", "Total Extent Length", MetadataValue.FromUnsignedInteger(TotalExtentLength(item)), directory.Offset, directory.Length, "");
        AddPropertyIndexTag(directory, item);
        AddReferenceTags(directory, item, state);
        AddCropTags(directory, item, state);
    }

    internal static void AddPropertyIndexTag(MetadataDirectory directory, BmffItem item)
    {
        MetadataValue value = new MetadataValue();
        value.Kind = MetadataValueKind.StringList;
        foreach (BmffPropertyAssociation association in item.Properties)
        {
            string text = association.Index.ToString();
            if (association.Essential)
            {
                text = text + " (essential)";
            }

            value.AddTextValue(text);
        }

        AddTag(directory, "Bmff.Item.PropertyAssociations", "Property Associations", value, directory.Offset, directory.Length, "");
    }

    internal static void AddReferenceTags(MetadataDirectory directory, BmffItem item, BmffMetadataState state)
    {
        foreach (BmffItemReference reference in state.References)
        {
            if (reference.FromItemId == item.Id)
            {
                ulong[] targetIds = new ulong[reference.ToItemIds.Count];
                int targetIndex = 0;
                foreach (uint target in reference.ToItemIds)
                {
                    targetIds[targetIndex] = target;
                    targetIndex = targetIndex + 1;
                }

                AddTag(directory, "Bmff.Item.Reference." + reference.Type, reference.Type + " References", MetadataValue.FromUnsignedIntegerArray(targetIds), directory.Offset, directory.Length, "");
            }
        }
    }

    internal static void AddCropTags(MetadataDirectory directory, BmffItem item, BmffMetadataState state)
    {
        foreach (BmffPropertyAssociation association in item.Properties)
        {
            BmffItemProperty? property = FindProperty(state, association.Index);
            if (property != null && property.Type == "clap")
            {
                AddTag(directory, "Bmff.Item.CleanApertureWidth", "Clean Aperture Width", MetadataValue.FromRational(property.CleanWidthNumerator, property.CleanWidthDenominator), property.Box.DataOffset, 8, "");
                AddTag(directory, "Bmff.Item.CleanApertureHeight", "Clean Aperture Height", MetadataValue.FromRational(property.CleanHeightNumerator, property.CleanHeightDenominator), property.Box.DataOffset + 8, 8, "");
                AddTag(directory, "Bmff.Item.HorizontalOffset", "Horizontal Offset", MetadataValue.FromSignedRational(property.HorizontalOffsetNumerator, property.HorizontalOffsetDenominator), property.Box.DataOffset + 16, 8, "");
                AddTag(directory, "Bmff.Item.VerticalOffset", "Vertical Offset", MetadataValue.FromSignedRational(property.VerticalOffsetNumerator, property.VerticalOffsetDenominator), property.Box.DataOffset + 24, 8, "");
            }
            else if (property != null && property.Type == "pasp")
            {
                AddTag(directory, "Bmff.Item.PixelAspectRatio", "Pixel Aspect Ratio", MetadataValue.FromRational(property.HorizontalSpacing, property.VerticalSpacing), property.Box.DataOffset, 8, "");
            }
        }
    }

    internal static void AddFrames(ImageMetadataDocument document, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        foreach (BmffItem item in OrderedItems(state))
        {
            if (!IsImageItem(item))
            {
                continue;
            }

            if (document.Frames.Count >= GetMaxFrameCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.frame.count.limit", "ISO BMFF image item count exceeded the configured frame limit.", "meta", -1, document.Frames.Count);
                break;
            }

            ImageMetadataFrame frame = new ImageMetadataFrame();
            frame.Role = ItemRole(item);
            frame.ContainerItemId = item.Id;
            frame.Width = item.Width;
            frame.Height = item.Height;
            frame.CanvasWidth = item.Width;
            frame.CanvasHeight = item.Height;
            frame.ChannelCount = item.ChannelCount;
            frame.BitDepth = item.BitDepth;
            frame.HasAlpha = IsAlphaAuxiliary(item);
            frame.ColorSpace = ItemColorSpace(item);
            frame.Orientation = ImageMetadataNormalizerCore.OrientationFromHeifTransform(item.RotationDegrees, item.MirrorAxis);
            document.AddFrame(frame);
            MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Bmff, "Item", "BMFF Image Item");
            directory.Path = "BMFF/Frames/" + frame.Index.ToString();
            SetItemRange(directory, item);
            AddTag(directory, "Bmff.Frame.ItemId", "Item ID", MetadataValue.FromUnsignedInteger(item.Id), directory.Offset, directory.Length, "");
            AddTag(directory, "Bmff.Frame.Primary", "Primary", MetadataValue.FromBoolean(item.Primary), directory.Offset, directory.Length, "");
            AddTag(directory, "Bmff.Frame.Thumbnail", "Thumbnail", MetadataValue.FromBoolean(item.Thumbnail), directory.Offset, directory.Length, "");
            AddTag(directory, "Bmff.Frame.Auxiliary", "Auxiliary", MetadataValue.FromBoolean(item.Auxiliary), directory.Offset, directory.Length, "");
            AddTag(directory, "Bmff.Frame.Orientation", "Orientation", MetadataValue.FromInteger((int)frame.Orientation), directory.Offset, directory.Length, frame.Orientation.ToString());
        }

        ApplyFrameParents(document, state);
    }

    internal static void ExtractItemPayloads(ImageMetadataDocument document, BoundedStreamReader reader, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        foreach (BmffItem item in state.Items)
        {
            if (IsExifItem(item))
            {
                ExtractExifItem(document, reader, item, state, options);
            }
            else if (IsXmpItem(item))
            {
                ExtractXmpItem(document, reader, item, state, options);
            }
            else if (IsJumbfItem(item))
            {
                ExtractJumbfItem(document, reader, item, state, options);
            }

            if (item.Thumbnail)
            {
                ExtractThumbnailItem(document, reader, item, state, options);
            }
        }
    }

    internal static void ExtractExifItem(ImageMetadataDocument document, BoundedStreamReader reader, BmffItem item, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        ulong declaredLength = TotalExtentLength(item);
        byte[] data = ReadItemData(document, reader, item, GetMaxProfileBytes(options), "bmff.exif.profile.limit");
        long itemOffset = FirstExtentOffset(item);
        if (data.Length == 0 && declaredLength > 0)
        {
            MetadataProfile limited = CreateProfile(MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.BMFF.Item", "EXIF", "image/tiff", itemOffset, BmffBoxTraverser.ToDiagnosticLength(declaredLength), data);
            ApplyProfileItemIdentity(limited, item, state, document, "cdsc", ImageMetadataRole.Unknown);
            document.AddProfile(limited);
            return;
        }

        if (data.Length < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.exif.offset.truncated", "ISO BMFF EXIF item offset field was truncated.", "Exif", itemOffset, 4);
            return;
        }

        uint tiffRelativeOffset = BmffBoxTraverser.ReadUnsigned32(CopyRange(data, 0, 4));
        ulong tiffStartValue = 4 + (ulong)tiffRelativeOffset;
        if (tiffStartValue > (ulong)data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.exif.offset.invalid", "ISO BMFF EXIF item TIFF offset exceeded the item data.", "Exif", itemOffset, data.Length);
            return;
        }

        int tiffStart = (int)tiffStartValue;
        byte[] tiff = CopyRange(data, tiffStart, data.Length - tiffStart);
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.BMFF.Item", "EXIF", "image/tiff", itemOffset + tiffStart, tiff.Length, tiff);
        ApplyProfileItemIdentity(profile, item, state, document, "cdsc", ImageMetadataRole.Unknown);
        document.AddProfile(profile);
        MetadataDirectory directory = AddPayloadDirectory(document, item, "EXIF", MetadataFamily.Exif);
        AddTag(directory, "Bmff.Exif.ItemId", "Item ID", MetadataValue.FromUnsignedInteger(item.Id), itemOffset, data.Length, "");
        AddTag(directory, "Bmff.Exif.TiffOffset", "TIFF Offset", MetadataValue.FromUnsignedInteger(tiffRelativeOffset), itemOffset, 4, "");
        AddTag(directory, "Bmff.Exif.ByteOrder", "TIFF Byte Order", MetadataValue.FromString(JpegExifExtractor.TiffByteOrderName(tiff)), itemOffset + tiffStart, PrefixLength(tiff, 2), JpegExifExtractor.TiffByteOrderName(tiff));
        if (!JpegExifExtractor.HasTiffHeader(tiff))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.exif.tiff.header.invalid", "ISO BMFF EXIF item did not contain a recognized TIFF header.", "Exif", itemOffset + tiffStart, tiff.Length);
        }

        CaptureItemRawBlock(document, item, "Exif", "EXIF Item", data, options);
    }

    internal static void ExtractXmpItem(ImageMetadataDocument document, BoundedStreamReader reader, BmffItem item, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        if (!ShouldParseXmp(options))
        {
            return;
        }

        long limit = GetMaxXmlBytes(options);
        ulong declaredLength = TotalExtentLength(item);
        byte[] data = ReadItemData(document, reader, item, limit, "bmff.xmp.profile.limit");
        long offset = FirstExtentOffset(item);
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.BMFF.Item", "XMP", item.ContentType.Length > 0 ? item.ContentType : "application/rdf+xml", offset, BmffBoxTraverser.ToDiagnosticLength(declaredLength), data);
        ApplyProfileItemIdentity(profile, item, state, document, "cdsc", ImageMetadataRole.Unknown);
        if (data.Length > 0 || declaredLength == 0)
        {
            profile.Text = Encoding.UTF8.GetString(data);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "BMFF/Item/XMP");
        MetadataDirectory directory = AddPayloadDirectory(document, item, "XMP", MetadataFamily.Xmp);
        AddTag(directory, "Bmff.Xmp.ItemId", "Item ID", MetadataValue.FromUnsignedInteger(item.Id), offset, data.Length, "");
        AddTag(directory, "Bmff.Xmp.PacketByteLength", "Packet Byte Length", MetadataValue.FromInteger(data.Length), offset, data.Length, "");
        if (data.Length > 0 && !MetadataStringReader.IsValidUtf8(data))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.xmp.utf8.invalid", "ISO BMFF XMP item was not valid UTF-8.", "mime", offset, data.Length);
        }

        CaptureItemRawBlock(document, item, "mime", "XMP Item", data, options);
    }

    internal static void ExtractJumbfItem(ImageMetadataDocument document, BoundedStreamReader reader, BmffItem item, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        ulong declaredLength = TotalExtentLength(item);
        byte[] data = ReadItemData(document, reader, item, GetMaxProfileBytes(options), "bmff.jumbf.profile.limit");
        long offset = FirstExtentOffset(item);
        MetadataProfileKind kind = IsC2paItem(item) ? MetadataProfileKind.C2pa : MetadataProfileKind.Jumbf;
        string key = kind == MetadataProfileKind.C2pa ? "C2pa.BMFF.Item" : "Jumbf.BMFF.Item";
        string name = kind == MetadataProfileKind.C2pa ? "C2PA" : "JUMBF";
        MetadataProfile profile = CreateProfile(kind, MetadataFamily.Container, key, name, item.ContentType.Length > 0 ? item.ContentType : "application/jumbf", offset, BmffBoxTraverser.ToDiagnosticLength(declaredLength), data);
        ApplyProfileItemIdentity(profile, item, state, document, "cdsc", ImageMetadataRole.Unknown);
        document.AddProfile(profile);
        MetadataDirectory directory = AddPayloadDirectory(document, item, name, MetadataFamily.Container);
        AddTag(directory, "Bmff.Jumbf.ItemId", "Item ID", MetadataValue.FromUnsignedInteger(item.Id), offset, data.Length, "");
        AddTag(directory, "Bmff.Jumbf.ByteLength", "Byte Length", MetadataValue.FromInteger(data.Length), offset, data.Length, "");
        CaptureItemRawBlock(document, item, item.Type, name + " Item", data, options);
    }

    internal static void ExtractThumbnailItem(ImageMetadataDocument document, BoundedStreamReader reader, BmffItem item, BmffMetadataState state, ImageMetadataReaderOptions options)
    {
        ulong declaredLength = TotalExtentLength(item);
        byte[] data = ReadItemData(document, reader, item, GetMaxProfileBytes(options), "bmff.thumbnail.profile.limit");
        long offset = FirstExtentOffset(item);
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, "Thumbnail.BMFF.Item", "Thumbnail", ItemMimeType(item), offset, BmffBoxTraverser.ToDiagnosticLength(declaredLength), data);
        ApplyProfileItemIdentity(profile, item, state, document, "thmb", ImageMetadataRole.Thumbnail);
        document.AddProfile(profile);
        CaptureItemRawBlock(document, item, item.Type, "Thumbnail Item", data, options);
    }

    internal static void ExtractIccProperty(ImageMetadataDocument document, BmffItemProperty property, BmffItem item, ImageMetadataReaderOptions options)
    {
        if (property == null || property.ProfileExtracted || property.ProfileData.Length == 0 || !ShouldParseIcc(options))
        {
            return;
        }

        property.ProfileExtracted = true;
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.BMFF.colr", "ICC Profile", "application/vnd.iccprofile", property.Box.DataOffset + 4, property.ProfileData.Length, new byte[0]);
        ApplyProfileItemIdentity(profile, item, null, document, "", ImageMetadataRole.Unknown);
        if (property.ProfileData.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = CopyRange(property.ProfileData, 0, property.ProfileData.Length);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.icc.profile.limit", "ISO BMFF ICC property exceeded the configured profile limit.", "colr", profile.Offset, profile.Length);
        }

        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, options, "BMFF/colr/ICC", "colr");
    }

    internal static void ExtractJumbfBoxes(ImageMetadataDocument document, BoundedStreamReader reader, List<BmffBox> boxes, ImageMetadataReaderOptions options)
    {
        foreach (BmffBox box in boxes)
        {
            if (box == null)
            {
                continue;
            }

            if (box.Type == "jumb")
            {
                ExtractJumbfBox(document, reader, box, options);
            }

            ExtractJumbfBoxes(document, reader, box.Children, options);
        }
    }

    internal static void ExtractJumbfBox(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        long length = BmffBoxTraverser.ToDiagnosticLength(box.Size);
        byte[] data = ReadRange(document, reader, box.Offset, length, GetMaxProfileBytes(options), "bmff.jumbf.profile.limit", "jumb");
        bool c2pa = ContainsC2paDescription(reader, box, options);
        MetadataProfile profile = CreateProfile(c2pa ? MetadataProfileKind.C2pa : MetadataProfileKind.Jumbf, MetadataFamily.Container, c2pa ? "C2pa.BMFF.jumb" : "Jumbf.BMFF.jumb", c2pa ? "C2PA" : "JUMBF", "application/jumbf", box.Offset, length, data);
        document.AddProfile(profile);
        if (CaptureRawBlocks(options))
        {
            MetadataRawBlock raw = document.AddRawBlockWithRange(MetadataFamily.Container, "jumb", c2pa ? "C2PA JUMBF" : "JUMBF", box.Offset, length);
            if (CaptureRawBlockData(options))
            {
                if (length <= GetMaxRawBlockBytes(options))
                {
                    raw.Data = ReadRange(document, reader, box.Offset, length, GetMaxRawBlockBytes(options), "bmff.jumbf.raw.limit", "jumb");
                }
                else
                {
                    MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.jumbf.raw.limit", "ISO BMFF JUMBF raw block exceeded the configured raw block limit.", "jumb", box.Offset, length);
                }
            }
        }
    }

    internal static bool ContainsC2paDescription(BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        BmffBox? description = FindFirstBox(box.Children, "jumd");
        if (description == null)
        {
            return false;
        }

        byte[] data = ReadBoxData(null, reader, description, options, "");
        byte[] text = Encoding.ASCII.GetBytes("c2pa");
        int index = 0;
        while (index <= data.Length - text.Length)
        {
            int match = 0;
            while (match < text.Length && ToLowerAscii(data[index + match]) == text[match])
            {
                match = match + 1;
            }

            if (match == text.Length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static byte[] ReadItemData(ImageMetadataDocument document, BoundedStreamReader reader, BmffItem item, long limit, string limitCode)
    {
        ulong total = TotalExtentLength(item);
        if (total > BmffBoxTraverser.ToUnsignedLength(limit) || total > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, limitCode, "ISO BMFF item payload exceeded the configured limit.", item.Type, FirstExtentOffset(item), BmffBoxTraverser.ToDiagnosticLength(total));
            return new byte[0];
        }

        byte[] result = new byte[(int)total];
        int output = 0;
        foreach (BmffExtent extent in item.Extents)
        {
            if (!extent.Resolved || extent.Length > 2147483647)
            {
                return new byte[0];
            }

            if (!reader.SeekAbsolute(extent.AbsoluteOffset) || !reader.CanRead((long)extent.Length))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.item.extent.read.truncated", "ISO BMFF item extent could not be read.", item.Type, extent.AbsoluteOffset, BmffBoxTraverser.ToDiagnosticLength(extent.Length));
                return new byte[0];
            }

            byte[] part = reader.ReadBytes((int)extent.Length);
            int index = 0;
            while (index < part.Length)
            {
                result[output] = part[index];
                output = output + 1;
                index = index + 1;
            }
        }

        return result;
    }

    internal static byte[] ReadBoxData(ImageMetadataDocument? document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options, string limitCode)
    {
        if (box == null)
        {
            return new byte[0];
        }

        return ReadRange(document, reader, box.DataOffset, BmffBoxTraverser.ToDiagnosticLength(box.DataSize), BmffBoxTraverser.GetMaxSegmentBytes(options), limitCode, box.Type);
    }

    internal static byte[] ReadRange(ImageMetadataDocument? document, BoundedStreamReader reader, long offset, long length, long limit, string limitCode, string blockType)
    {
        if (reader == null || offset < 0 || length < 0)
        {
            return new byte[0];
        }

        if (length > limit || length > 2147483647)
        {
            if (document != null && limitCode.Length > 0)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, limitCode, "ISO BMFF payload exceeded the configured limit.", blockType, offset, length);
            }

            return new byte[0];
        }

        if (!reader.SeekAbsolute(offset) || !reader.CanRead(length))
        {
            if (document != null)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.payload.truncated", "ISO BMFF payload could not be read.", blockType, offset, length);
            }

            return new byte[0];
        }

        return reader.ReadBytes((int)length);
    }

    internal static byte[] ReadPrefix(ImageMetadataDocument document, BoundedStreamReader reader, long offset, ulong available, int count, string blockType)
    {
        if (available < (ulong)count || !reader.SeekAbsolute(offset) || !reader.CanRead(count))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff." + blockType + ".truncated", "ISO BMFF box fields were truncated.", blockType, offset, count);
            return new byte[0];
        }

        return reader.ReadBytes(count);
    }

    internal static void CaptureItemRawBlock(ImageMetadataDocument document, BmffItem item, string blockType, string name, byte[] data, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        foreach (BmffExtent extent in item.Extents)
        {
            if (extent.Resolved)
            {
                MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Bmff, blockType, name, extent.AbsoluteOffset, BmffBoxTraverser.ToDiagnosticLength(extent.Length));
                if (CaptureRawBlockData(options) && item.Extents.Count == 1 && data.Length == (long)extent.Length && data.Length <= GetMaxRawBlockBytes(options))
                {
                    block.Data = CopyRange(data, 0, data.Length);
                }
            }
        }
    }

    internal static MetadataProfile CreateProfile(MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length, byte[] data)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = mimeType;
        profile.Offset = offset;
        profile.Length = length;
        profile.Data = data == null ? new byte[0] : data;
        return profile;
    }

    internal static MetadataDirectory AddPayloadDirectory(ImageMetadataDocument document, BmffItem item, string name, MetadataFamily family)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(family, name, name);
        directory.Path = "BMFF/Items/" + item.Id.ToString() + "/" + name;
        directory.Offset = FirstExtentOffset(item);
        directory.Length = BmffBoxTraverser.ToDiagnosticLength(TotalExtentLength(item));
        return directory;
    }

    internal static List<BmffItem> OrderedItems(BmffMetadataState state)
    {
        List<BmffItem> result = new List<BmffItem>();
        BmffItem? primary = FindItem(state, state.PrimaryItemId);
        if (primary != null)
        {
            result.Add(primary);
        }

        foreach (BmffItem item in state.Items)
        {
            if (item != primary)
            {
                result.Add(item);
            }
        }

        return result;
    }

    internal static BmffItem? GetOrCreateItem(BmffMetadataState state, uint id, ImageMetadataReaderOptions options)
    {
        BmffItem? item = FindItem(state, id);
        if (item != null)
        {
            return item;
        }

        if (state.Items.Count >= GetMaxCount(options))
        {
            return null;
        }

        item = new BmffItem();
        item.Id = id;
        state.Items.Add(item);
        return item;
    }

    internal static BmffItem? FindItem(BmffMetadataState state, uint id)
    {
        if (state == null)
        {
            return null;
        }

        foreach (BmffItem item in state.Items)
        {
            if (item != null && item.Id == id)
            {
                return item;
            }
        }

        return null;
    }

    internal static BmffItemProperty? FindProperty(BmffMetadataState state, int index)
    {
        if (state == null)
        {
            return null;
        }

        foreach (BmffItemProperty property in state.Properties)
        {
            if (property != null && property.Index == index)
            {
                return property;
            }
        }

        return null;
    }

    internal static BmffBox? FindFirstBox(List<BmffBox>? boxes, string type)
    {
        if (boxes == null)
        {
            return null;
        }

        foreach (BmffBox box in boxes)
        {
            if (box != null && box.Type == type)
            {
                return box;
            }

            if (box != null)
            {
                BmffBox? child = FindFirstBox(box.Children, type);
                if (child != null)
                {
                    return child;
                }
            }
        }

        return null;
    }

    internal static BmffBox? FindDirectChild(BmffBox? parent, string type)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (BmffBox box in parent.Children)
        {
            if (box != null && box.Type == type)
            {
                return box;
            }
        }

        return null;
    }

    internal static void CopyItemInfo(BmffItem target, BmffItem source)
    {
        target.Type = source.Type;
        target.Name = source.Name;
        target.ContentType = source.ContentType;
        target.ContentEncoding = source.ContentEncoding;
        target.Hidden = source.Hidden;
        target.ProtectionIndex = source.ProtectionIndex;
    }

    internal static void SetItemRange(MetadataDirectory directory, BmffItem item)
    {
        directory.Offset = FirstExtentOffset(item);
        directory.Length = BmffBoxTraverser.ToDiagnosticLength(TotalExtentLength(item));
    }

    internal static ulong TotalExtentLength(BmffItem item)
    {
        ulong total = 0;
        if (item == null)
        {
            return total;
        }

        foreach (BmffExtent extent in item.Extents)
        {
            if (extent.Length > ulong.MaxValue - total)
            {
                return ulong.MaxValue;
            }

            total = total + extent.Length;
        }

        return total;
    }

    internal static long FirstExtentOffset(BmffItem item)
    {
        if (item == null)
        {
            return -1;
        }

        foreach (BmffExtent extent in item.Extents)
        {
            if (extent.Resolved)
            {
                return extent.AbsoluteOffset;
            }
        }

        return -1;
    }

    internal static bool IsImageItem(BmffItem item)
    {
        if (item == null || item.Width <= 0 || item.Height <= 0)
        {
            return false;
        }

        return item.Type != "Exif" && item.Type != "mime" && item.Type != "uri " && item.Type != "jumb";
    }

    internal static bool IsExifItem(BmffItem item)
    {
        return item != null && item.Type == "Exif";
    }

    internal static bool IsXmpItem(BmffItem item)
    {
        if (item == null || item.Type != "mime")
        {
            return false;
        }

        string contentType = Lower(item.ContentType);
        string name = Lower(item.Name);
        return contentType == "application/rdf+xml"
            || contentType == "application/xml"
            || contentType == "text/xml"
            || name.IndexOf("xmp", StringComparison.Ordinal) >= 0;
    }

    internal static bool IsJumbfItem(BmffItem item)
    {
        if (item == null)
        {
            return false;
        }

        string contentType = Lower(item.ContentType);
        return item.Type == "jumb"
            || contentType == "application/jumbf"
            || contentType == "application/c2pa"
            || contentType.IndexOf("c2pa", StringComparison.Ordinal) >= 0;
    }

    internal static bool IsC2paItem(BmffItem item)
    {
        if (item == null)
        {
            return false;
        }

        return Lower(item.ContentType).IndexOf("c2pa", StringComparison.Ordinal) >= 0
            || Lower(item.Name).IndexOf("c2pa", StringComparison.Ordinal) >= 0;
    }

    internal static bool IsAlphaAuxiliary(BmffItem item)
    {
        return item != null && Lower(item.AuxiliaryType).IndexOf("alpha", StringComparison.Ordinal) >= 0;
    }

    internal static bool IsDepthAuxiliary(BmffItem item)
    {
        if (item == null)
        {
            return false;
        }

        string auxiliaryType = Lower(item.AuxiliaryType);
        return auxiliaryType.IndexOf("depth", StringComparison.Ordinal) >= 0
            || auxiliaryType.IndexOf("disparity", StringComparison.Ordinal) >= 0
            || auxiliaryType.IndexOf("auxid:2", StringComparison.Ordinal) >= 0;
    }

    internal static ImageMetadataRole ItemRole(BmffItem item)
    {
        if (item == null) return ImageMetadataRole.Unknown;
        if (item.Primary) return ImageMetadataRole.Primary;
        if (item.Thumbnail) return ImageMetadataRole.Thumbnail;
        if (IsAlphaAuxiliary(item)) return ImageMetadataRole.AuxiliaryAlpha;
        if (IsDepthAuxiliary(item)) return ImageMetadataRole.AuxiliaryDepth;
        if (item.Auxiliary) return ImageMetadataRole.Auxiliary;
        return ImageMetadataRole.Page;
    }

    internal static int FrameIndexForItem(ImageMetadataDocument document, uint itemId)
    {
        if (document == null || document.Frames == null)
        {
            return -1;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.ContainerItemId == itemId)
            {
                return frame.Index;
            }
        }

        return -1;
    }

    internal static int ReferencedFrameIndex(ImageMetadataDocument document, BmffMetadataState? state, uint itemId, string referenceType)
    {
        if (state == null)
        {
            return -1;
        }

        foreach (BmffItemReference reference in state.References)
        {
            if (reference != null && reference.FromItemId == itemId && reference.Type == referenceType)
            {
                foreach (uint target in reference.ToItemIds)
                {
                    int frameIndex = FrameIndexForItem(document, target);
                    if (frameIndex >= 0)
                    {
                        return frameIndex;
                    }
                }
            }
        }

        return -1;
    }

    internal static void ApplyFrameParents(ImageMetadataDocument document, BmffMetadataState state)
    {
        if (document == null || state == null)
        {
            return;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.ContainerItemId >= 0 && frame.ContainerItemId <= uint.MaxValue)
            {
                uint itemId = (uint)frame.ContainerItemId;
                if (frame.Role == ImageMetadataRole.Thumbnail)
                {
                    frame.ParentFrameIndex = ReferencedFrameIndex(document, state, itemId, "thmb");
                }
                else if (frame.Role == ImageMetadataRole.Auxiliary || frame.Role == ImageMetadataRole.AuxiliaryAlpha || frame.Role == ImageMetadataRole.AuxiliaryDepth)
                {
                    frame.ParentFrameIndex = ReferencedFrameIndex(document, state, itemId, "auxl");
                }
            }
        }

        foreach (MetadataProfile profile in document.Profiles)
        {
            if (profile != null && profile.SourceFrameIndex < 0 && profile.ContainerItemId >= 0 && profile.ContainerItemId <= uint.MaxValue)
            {
                profile.SourceFrameIndex = FrameIndexForItem(document, (uint)profile.ContainerItemId);
            }
        }
    }

    internal static void ApplyProfileItemIdentity(MetadataProfile profile, BmffItem item, BmffMetadataState? state, ImageMetadataDocument document, string referenceType, ImageMetadataRole role)
    {
        if (profile == null || item == null)
        {
            return;
        }

        profile.ContainerItemId = item.Id;
        if (role != ImageMetadataRole.Unknown)
        {
            profile.Role = role;
        }

        if (referenceType.Length > 0)
        {
            profile.SourceFrameIndex = ReferencedFrameIndex(document, state, item.Id, referenceType);
        }
        else
        {
            profile.SourceFrameIndex = FrameIndexForItem(document, item.Id);
        }
    }

    internal static ImageColorSpaceKind ItemColorSpace(BmffItem item)
    {
        if (item == null || item.ColorType.Length == 0)
        {
            return ImageColorSpaceKind.Unknown;
        }

        if (item.ColorType == "rICC" || item.ColorType == "prof")
        {
            return ImageColorSpaceKind.Rgb;
        }

        if (item.ColorPrimaries == 12)
        {
            return ImageColorSpaceKind.DisplayP3;
        }

        if (item.ColorPrimaries == 9)
        {
            return ImageColorSpaceKind.WideGamutRgb;
        }

        if (item.MatrixCoefficients == 0)
        {
            return ImageColorSpaceKind.Rgb;
        }

        return ImageColorSpaceKind.YCbCr;
    }

    internal static string ItemMimeType(BmffItem item)
    {
        if (item == null)
        {
            return "application/octet-stream";
        }

        if (item.ContentType.Length > 0)
        {
            return item.ContentType;
        }

        if (item.Type == "av01")
        {
            return "image/avif";
        }

        if (item.Type == "hvc1" || item.Type == "hev1" || item.Type == "grid")
        {
            return "image/heic";
        }

        if (item.Type == "jpeg")
        {
            return "image/jpeg";
        }

        return "application/octet-stream";
    }

    internal static string MirrorAxisName(int axis)
    {
        if (axis == 0)
        {
            return "Vertical";
        }

        if (axis == 1)
        {
            return "Horizontal";
        }

        return "";
    }

    internal static string Lower(string value)
    {
        return value == null ? "" : value.ToLowerInvariant();
    }

    internal static bool ValidIntegerSize(int size)
    {
        return size >= 0 && size <= 8;
    }

    internal static bool RequirePropertyBytes(ImageMetadataDocument document, BmffBox box, byte[] data, int required)
    {
        if (data != null && data.Length >= required)
        {
            return true;
        }

        AddTruncated(document, box, "bmff." + box.Type + ".truncated", "ISO BMFF " + box.Type + " property was truncated.", required);
        return false;
    }

    internal static int ToDimension(ImageMetadataDocument document, BmffBox box, uint value, string name)
    {
        if (value > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.ispe." + name + ".limit", "ISO BMFF image dimension exceeded the supported integer range.", "ispe", box.DataOffset, value);
            return 0;
        }

        return (int)value;
    }

    internal static void AddTruncated(ImageMetadataDocument document, BmffBox box, string code, string message, long required)
    {
        if (document == null || box == null)
        {
            return;
        }

        MetadataDiagnostic.AddTruncated(document.Diagnostics, code, message, box.Type, box.DataOffset, required);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int count)
    {
        if (data == null || offset < 0 || count <= 0 || offset > data.Length)
        {
            return new byte[0];
        }

        int actual = count;
        if (actual > data.Length - offset)
        {
            actual = data.Length - offset;
        }

        byte[] result = new byte[actual];
        int index = 0;
        while (index < actual)
        {
            result[index] = data[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int PrefixLength(byte[] data, int maximum)
    {
        if (data == null)
        {
            return 0;
        }

        return data.Length < maximum ? data.Length : maximum;
    }

    internal static byte ToLowerAscii(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return (byte)(value + 32);
        }

        return value;
    }

    internal static int GetMaxCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long GetMaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }
}
