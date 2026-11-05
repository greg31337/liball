using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace avif;

internal static class AvifDocumentBuilder
{
    internal static AvifDocument CreateArgumentError()
    {
        List<AvifDiagnostic> findings = new List<AvifDiagnostic>();
        findings.Add(AvifDiagnostic.Create(
            AvifResultCode.InvalidArgument,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Api,
            "The source byte array is null."));
        return new AvifDocument(
            AvifResultCode.InvalidArgument,
            null,
            null,
            new List<AvifBox>(),
            new List<AvifBox>(),
            new List<AvifImageItem>(),
            new List<AvifItemReference>(),
            new List<AvifEntityGroup>(),
            findings);
    }

    internal static AvifDocument Create(
        ReadOnlySpan<byte> source,
        AvifContainerValidationResult validation)
    {
        List<AvifBox> topLevelBoxes = new List<AvifBox>();
        List<AvifBox> boxes = new List<AvifBox>();
        if (validation.Boxes != null)
        {
            int boxIndex = 0;
            while (boxIndex < validation.Boxes.Boxes.Count)
            {
                AvifBox box = CreateBox(
                    source,
                    validation.Boxes.Boxes[boxIndex],
                    string.Empty,
                    boxes);
                topLevelBoxes.Add(box);
                boxIndex++;
            }
        }

        AvifFileInfo? fileInfo = CreateFileInfo(validation.FileType, validation.Meta);
        AvifMovie? movie = validation.Movie == null
            ? null : new AvifMovie(validation.Movie);
        List<AvifItemReference> references = CreateReferences(validation.Meta);
        List<AvifEntityGroup> groups = CreateGroups(validation.Meta);
        List<AvifImageItem> items = CreateItems(validation.Meta);
        List<AvifDiagnostic> findings = CreateFindings(validation.Diagnostics);
        return new AvifDocument(
            validation.Code,
            fileInfo,
            movie,
            topLevelBoxes,
            boxes,
            items,
            references,
            groups,
            findings);
    }

    internal static AvifBox CreateBox(
        ReadOnlySpan<byte> source,
        IsoBox box,
        string parentPath,
        List<AvifBox> flat)
    {
        string path = parentPath + "/" + box.Type;
        List<AvifBox> children = new List<AvifBox>();
        int version = box.Version;
        int flags = box.Flags;
        bool fullBox = box.HasFullBoxFields;
        if (!fullBox && IsFullBoxType(box.Type))
        {
            IsoFullBox parsed = IsoFullBox.Parse(source, box);
            if (parsed.Code == AvifResultCode.Success)
            {
                fullBox = true;
                version = parsed.Version;
                flags = parsed.Flags;
            }
        }

        AvifBox converted = new AvifBox(
            box.Type,
            path,
            box.Offset,
            box.Size,
            box.HeaderSize,
            box.UsesLargeSize,
            box.ExtendsToEnd,
            box.IsUuid,
            !IsKnownBoxType(box.Type),
            fullBox,
            version,
            flags,
            box.UserType,
            children);
        flat.Add(converted);
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            AvifBox child = CreateBox(source, box.Children[childIndex], path, flat);
            converted.AddChild(child);
            childIndex++;
        }

        return converted;
    }

    internal static bool IsFullBoxType(string type)
    {
        return type == "meta"
            || type == "hdlr"
            || type == "pitm"
            || type == "iloc"
            || type == "iinf"
            || type == "infe"
            || type == "ipma"
            || type == "iref"
            || type == "dref"
            || type == "url "
            || type == "urn "
            || type == "ispe"
            || type == "pixi"
            || type == "auxC"
            || type == "auxi"
            || type == "altr"
            || type == "ster"
            || type == "reve"
            || type == "ndwt"
            || type == "cmin"
            || type == "cmex"
            || type == "mvhd"
            || type == "tkhd"
            || type == "mdhd"
            || type == "vmhd"
            || type == "elst"
            || type == "stsd"
            || type == "stts"
            || type == "stsc"
            || type == "stsz"
            || type == "stco"
            || type == "co64"
            || type == "stss"
            || type == "sdtp"
            || type == "sgpd"
            || type == "sbgp";
    }

    internal static bool IsKnownBoxType(string type)
    {
        return type == "ftyp"
            || type == "free"
            || type == "skip"
            || type == "mdat"
            || type == "mini"
            || type == "meta"
            || type == "moov"
            || type == "mvhd"
            || type == "trak"
            || type == "tkhd"
            || type == "tref"
            || type == "edts"
            || type == "elst"
            || type == "mdia"
            || type == "mdhd"
            || type == "minf"
            || type == "vmhd"
            || type == "stbl"
            || type == "stsd"
            || type == "av01"
            || type == "stts"
            || type == "stsc"
            || type == "stsz"
            || type == "stco"
            || type == "co64"
            || type == "stss"
            || type == "sdtp"
            || type == "sgpd"
            || type == "sbgp"
            || type == "hdlr"
            || type == "pitm"
            || type == "iloc"
            || type == "iinf"
            || type == "infe"
            || type == "iprp"
            || type == "ipco"
            || type == "ipma"
            || type == "iref"
            || type == "dinf"
            || type == "dref"
            || type == "url "
            || type == "urn "
            || type == "idat"
            || type == "grpl"
            || type == "altr"
            || type == "ster"
            || type == "auxl"
            || type == "thmb"
            || type == "dimg"
            || type == "prem"
            || type == "cdsc"
            || type == "av1C"
            || type == "auxi"
            || type == "ispe"
            || type == "pixi"
            || AvifContainerValidator.IsSupportedProperty(type);
    }

    internal static AvifFileInfo? CreateFileInfo(FileTypeBox? fileType, ParsedMeta? meta)
    {
        if (fileType == null && meta == null)
        {
            return null;
        }

        string majorBrand = fileType == null ? string.Empty : fileType.MajorBrand;
        uint minorVersion = fileType == null ? 0 : fileType.MinorVersion;
        List<string> compatibleBrands = fileType == null
            ? new List<string>()
            : new List<string>(fileType.CompatibleBrands);
        string handlerType = meta == null ? string.Empty : meta.GetHandlerType();
        string handlerName = meta == null ? string.Empty : meta.GetHandlerName();
        bool hasPrimary = meta != null && meta.HasPrimaryItem();
        uint primaryItemId = hasPrimary ? (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId() : 0;
        return new AvifFileInfo(
            majorBrand,
            minorVersion,
            compatibleBrands,
            handlerType,
            handlerName,
            hasPrimary,
            primaryItemId);
    }

    internal static List<AvifImageItem> CreateItems(ParsedMeta? meta)
    {
        List<AvifImageItem> items = new List<AvifImageItem>();
        if (meta == null)
        {
            return items;
        }

        ParsedItemDependencyGraph? graph = meta.GetItemDependencyGraph();
        int itemIndex = 0;
        while (itemIndex < meta.GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            List<AvifItemExtent> extents = new List<AvifItemExtent>();
            if ((item ?? throw new InvalidOperationException("Invalid internal state.")).HasLocation())
            {
                ParsedItemLocation? location = item.GetLocation();
                int extentIndex = 0;
                while (extentIndex < (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount())
                {
                    ParsedItemExtent? extent = location.GetExtent(extentIndex);
                    extents.Add(new AvifItemExtent(
                        (extent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex(),
                        extent.GetExtentOffset(),
                        extent.GetExtentLength(),
                        extent.HasResolvedRange(),
                        extent.GetResolvedOffset(),
                        extent.GetResolvedLength()));
                    extentIndex++;
                }
            }

            List<AvifItemProperty> properties = new List<AvifItemProperty>();
            int propertyIndex = 0;
            while (propertyIndex < item.GetPropertyCount())
            {
                properties.Add(new AvifItemProperty(
                    item.GetPropertyAssociation(propertyIndex)));
                propertyIndex++;
            }

            List<AvifItemReference> outgoing = CreateItemReferences(item, true);
            List<AvifItemReference> incoming = CreateItemReferences(item, false);
            List<uint> groupIds = new List<uint>();
            int groupIndex = 0;
            while (groupIndex < item.GetEntityGroupCount())
            {
                ParsedEntityGroup? group = item.GetEntityGroup(groupIndex);
                groupIds.Add((group ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId());
                groupIndex++;
            }

            ParsedItemDependencyNode? node = graph == null
                ? null
                : graph.FindNode(item.GetItemId());
            bool selfContained = node != null && node.IsSelfContained();
            items.Add(new AvifImageItem(
                item,
                meta.GetItemDiscovery().FindRole(item.GetItemId()),
                selfContained,
                extents,
                properties,
                outgoing,
                incoming,
                groupIds));
            itemIndex++;
        }

        return items;
    }

    internal static List<AvifItemReference> CreateItemReferences(
        ParsedItem item,
        bool outgoing)
    {
        List<AvifItemReference> references = new List<AvifItemReference>();
        int count = outgoing
            ? item.GetOutgoingReferenceCount()
            : item.GetIncomingReferenceCount();
        int index = 0;
        while (index < count)
        {
            ParsedItemReference? reference = outgoing
                ? item.GetOutgoingReference(index)
                : item.GetIncomingReference(index);
            references.Add(CreateReference(reference));
            index++;
        }

        return references;
    }

    internal static List<AvifItemReference> CreateReferences(ParsedMeta? meta)
    {
        List<AvifItemReference> references = new List<AvifItemReference>();
        if (meta == null)
        {
            return references;
        }

        int index = 0;
        while (index < meta.GetReferenceCount())
        {
            references.Add(CreateReference(meta.GetReference(index)));
            index++;
        }

        return references;
    }

    internal static AvifItemReference CreateReference(ParsedItemReference? reference)
    {
        List<uint> targets = new List<uint>();
        int targetIndex = 0;
        while (targetIndex < (reference ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetCount())
        {
            targets.Add(reference.GetTargetItemId(targetIndex));
            targetIndex++;
        }

        return new AvifItemReference(
            ConvertReferenceKind(reference.GetKind()),
            reference.GetBoxType(),
            reference.GetFromItemId(),
            targets);
    }

    internal static AvifItemReferenceKind ConvertReferenceKind(
        ParsedItemReferenceKind kind)
    {
        if (kind == ParsedItemReferenceKind.Auxiliary)
        {
            return AvifItemReferenceKind.Auxiliary;
        }

        if (kind == ParsedItemReferenceKind.Thumbnail)
        {
            return AvifItemReferenceKind.Thumbnail;
        }

        if (kind == ParsedItemReferenceKind.DerivedImage)
        {
            return AvifItemReferenceKind.DerivedImage;
        }

        if (kind == ParsedItemReferenceKind.Premultiplied)
        {
            return AvifItemReferenceKind.Premultiplied;
        }

        if (kind == ParsedItemReferenceKind.ContentDescription)
        {
            return AvifItemReferenceKind.ContentDescription;
        }

        return AvifItemReferenceKind.Unknown;
    }

    internal static List<AvifEntityGroup> CreateGroups(ParsedMeta? meta)
    {
        List<AvifEntityGroup> groups = new List<AvifEntityGroup>();
        if (meta == null)
        {
            return groups;
        }

        int groupIndex = 0;
        while (groupIndex < meta.GetEntityGroupCount())
        {
            ParsedEntityGroup? group = meta.GetEntityGroup(groupIndex);
            List<uint> entityIds = new List<uint>();
            int entityIndex = 0;
            while (entityIndex < (group ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityCount())
            {
                entityIds.Add(group.GetEntityId(entityIndex));
                entityIndex++;
            }

            groups.Add(new AvifEntityGroup(
                ConvertGroupKind(group.GetKind()),
                group.GetGroupingType(),
                group.GetVersion(),
                group.GetFlags(),
                group.GetGroupId(),
                entityIds,
                group.GetRawData()));
            groupIndex++;
        }

        return groups;
    }

    internal static AvifEntityGroupKind ConvertGroupKind(ParsedEntityGroupKind kind)
    {
        if (kind == ParsedEntityGroupKind.Alternate)
        {
            return AvifEntityGroupKind.Alternate;
        }

        if (kind == ParsedEntityGroupKind.Stereo)
        {
            return AvifEntityGroupKind.Stereo;
        }

        return AvifEntityGroupKind.Unknown;
    }

    internal static List<AvifDiagnostic> CreateFindings(DiagnosticLog diagnostics)
    {
        List<AvifDiagnostic> findings = new List<AvifDiagnostic>();
        if (diagnostics == null)
        {
            return findings;
        }

        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            AvifDiagnostic finding = diagnostics.Diagnostics[index];
            findings.Add(CopyFinding(finding));
            index++;
        }

        return findings;
    }

    internal static AvifDiagnostic CopyFinding(AvifDiagnostic finding)
    {
        return AvifDiagnostic.Create(
            finding.Code,
            finding.Severity,
            finding.Category,
            finding.Message,
            finding.ByteOffset,
            finding.BitOffset,
            finding.BoxPath,
            finding.ItemId,
            finding.TrackId,
            finding.FrameIndex,
            finding.TileIndex,
            finding.Av1SyntaxPath,
            finding.StandardReference);
    }
}

internal static class AvifDocumentDumper
{
    internal static string Dump(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("result ");
        builder.Append(document.GetCode().ToString());
        builder.Append('\n');
        AppendFile(builder, document.GetFileInfo());
        builder.Append(DumpBoxes(document));
        builder.Append(DumpItems(document));
        builder.Append(DumpExtents(document));
        builder.Append(DumpProperties(document));
        builder.Append(DumpReferences(document));
        builder.Append(DumpGroups(document));
        builder.Append(DumpFindings(document));
        return builder.ToString();
    }

    internal static void AppendFile(StringBuilder builder, AvifFileInfo? info)
    {
        builder.Append("file");
        if (info == null)
        {
            builder.Append(" unavailable\n");
            return;
        }

        builder.Append(" major=");
        builder.Append(Sanitize(info.GetMajorBrand()));
        builder.Append(" minor=");
        AppendUInt(builder, info.GetMinorVersion());
        builder.Append(" brands=");
        int brandIndex = 0;
        while (brandIndex < info.GetCompatibleBrandCount())
        {
            if (brandIndex != 0)
            {
                builder.Append(',');
            }

            builder.Append(Sanitize(info.GetCompatibleBrand(brandIndex)));
            brandIndex++;
        }

        builder.Append(" handler=");
        builder.Append(Sanitize(info.GetHandlerType()));
        builder.Append(" primary=");
        AppendUInt(builder, info.GetPrimaryItemId());
        builder.Append('\n');
    }

    internal static string DumpBoxes(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("boxes ");
        AppendInt(builder, document.GetBoxCount());
        builder.Append('\n');
        int index = 0;
        while (index < document.GetTopLevelBoxCount())
        {
            AppendBox(builder, document.GetTopLevelBox(index), 0);
            index++;
        }

        return builder.ToString();
    }

    internal static void AppendBox(StringBuilder builder, AvifBox? box, int depth)
    {
        builder.Append("box depth=");
        AppendInt(builder, depth);
        builder.Append(" path=");
        builder.Append(Sanitize((box ?? throw new InvalidOperationException("Invalid internal state.")).GetPath()));
        builder.Append(" offset=");
        AppendLong(builder, box.GetOffset());
        builder.Append(" size=");
        AppendLong(builder, box.GetSize());
        builder.Append(" header=");
        AppendInt(builder, box.GetHeaderSize());
        if (box.IsFullBox())
        {
            builder.Append(" version=");
            AppendInt(builder, box.GetVersion());
            builder.Append(" flags=");
            AppendInt(builder, box.GetFlags());
        }

        if (box.IsUnknown())
        {
            builder.Append(" unknown=true");
        }

        builder.Append('\n');
        int index = 0;
        while (index < box.GetChildCount())
        {
            AppendBox(builder, box.GetChild(index), depth + 1);
            index++;
        }
    }

    internal static string DumpItems(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("items ");
        AppendInt(builder, document.GetItemCount());
        builder.Append('\n');
        int index = 0;
        while (index < document.GetItemCount())
        {
            AvifImageItem? item = document.GetItem(index);
            builder.Append("item id=");
            AppendUInt(builder, (item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            builder.Append(" type=");
            builder.Append(Sanitize(item.GetItemType()));
            builder.Append(" primary=");
            builder.Append(item.IsPrimary() ? "true" : "false");
            builder.Append(" hidden=");
            builder.Append(item.IsHidden() ? "true" : "false");
            builder.Append(" coded=");
            builder.Append(item.IsCodedImage() ? "true" : "false");
            builder.Append(" derived=");
            builder.Append(item.IsDerivedImage() ? "true" : "false");
            builder.Append(" metadata=");
            builder.Append(item.IsMetadataItem() ? "true" : "false");
            builder.Append(" auxiliary=");
            builder.Append(item.IsAuxiliaryImage() ? "true" : "false");
            builder.Append(" selfContained=");
            builder.Append(item.IsSelfContained() ? "true" : "false");
            builder.Append(" name=");
            builder.Append(Sanitize(item.GetName()));
            builder.Append('\n');
            index++;
        }

        return builder.ToString();
    }

    internal static string DumpExtents(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        int total = 0;
        int itemIndex = 0;
        while (itemIndex < document.GetItemCount())
        {
            total += (document.GetItem(itemIndex) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount();
            itemIndex++;
        }

        builder.Append("extents ");
        AppendInt(builder, total);
        builder.Append('\n');
        itemIndex = 0;
        while (itemIndex < document.GetItemCount())
        {
            AvifImageItem? item = document.GetItem(itemIndex);
            int extentIndex = 0;
            while (extentIndex < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount())
            {
                AvifItemExtent? extent = item.GetExtent(extentIndex);
                builder.Append("extent item=");
                AppendUInt(builder, item.GetItemId());
                builder.Append(" index=");
                AppendULong(builder, (extent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex());
                builder.Append(" offset=");
                AppendULong(builder, extent.GetOffset());
                builder.Append(" length=");
                AppendULong(builder, extent.GetLength());
                builder.Append(" resolved=");
                builder.Append(extent.IsResolved() ? "true" : "false");
                if (extent.IsResolved())
                {
                    builder.Append(" sourceOffset=");
                    AppendLong(builder, extent.GetResolvedOffset());
                    builder.Append(" sourceLength=");
                    AppendLong(builder, extent.GetResolvedLength());
                }

                builder.Append('\n');
                extentIndex++;
            }

            itemIndex++;
        }

        return builder.ToString();
    }

    internal static string DumpProperties(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        int total = 0;
        int itemIndex = 0;
        while (itemIndex < document.GetItemCount())
        {
            total += (document.GetItem(itemIndex) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount();
            itemIndex++;
        }

        builder.Append("properties ");
        AppendInt(builder, total);
        builder.Append('\n');
        itemIndex = 0;
        while (itemIndex < document.GetItemCount())
        {
            AvifImageItem? item = document.GetItem(itemIndex);
            int propertyIndex = 0;
            while (propertyIndex < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
            {
                AvifItemProperty? property = item.GetProperty(propertyIndex);
                builder.Append("property item=");
                AppendUInt(builder, item.GetItemId());
                builder.Append(" index=");
                AppendInt(builder, (property ?? throw new InvalidOperationException("Invalid internal state.")).GetIndex());
                builder.Append(" type=");
                builder.Append(Sanitize(property.GetTypeName()));
                builder.Append(" essential=");
                builder.Append(property.IsEssential() ? "true" : "false");
                builder.Append(" known=");
                builder.Append(property.IsKnown() ? "true" : "false");
                builder.Append(" size=");
                AppendInt(builder, property.GetRawData().Length);
                builder.Append(" summary=");
                builder.Append(Sanitize(property.GetSummary()));
                builder.Append('\n');
                propertyIndex++;
            }

            itemIndex++;
        }

        return builder.ToString();
    }

    internal static string DumpReferences(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("references ");
        AppendInt(builder, document.GetReferenceCount());
        builder.Append('\n');
        int index = 0;
        while (index < document.GetReferenceCount())
        {
            AvifItemReference? reference = document.GetReference(index);
            builder.Append("reference type=");
            builder.Append(Sanitize((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName()));
            builder.Append(" kind=");
            builder.Append(reference.GetKind().ToString());
            builder.Append(" from=");
            AppendUInt(builder, reference.GetFromItemId());
            builder.Append(" targets=");
            int targetIndex = 0;
            while (targetIndex < reference.GetTargetCount())
            {
                if (targetIndex != 0)
                {
                    builder.Append(',');
                }

                AppendUInt(builder, reference.GetTargetItemId(targetIndex));
                targetIndex++;
            }

            builder.Append('\n');
            index++;
        }

        return builder.ToString();
    }

    internal static string DumpGroups(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("groups ");
        AppendInt(builder, document.GetGroupCount());
        builder.Append('\n');
        int index = 0;
        while (index < document.GetGroupCount())
        {
            AvifEntityGroup? group = document.GetGroup(index);
            builder.Append("group id=");
            AppendUInt(builder, (group ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId());
            builder.Append(" type=");
            builder.Append(Sanitize(group.GetTypeName()));
            builder.Append(" kind=");
            builder.Append(group.GetKind().ToString());
            builder.Append(" version=");
            AppendInt(builder, group.GetVersion());
            builder.Append(" flags=");
            AppendInt(builder, group.GetFlags());
            builder.Append(" entities=");
            int entityIndex = 0;
            while (entityIndex < group.GetEntityCount())
            {
                if (entityIndex != 0)
                {
                    builder.Append(',');
                }

                AppendUInt(builder, group.GetEntityId(entityIndex));
                entityIndex++;
            }

            builder.Append('\n');
            index++;
        }

        return builder.ToString();
    }

    internal static string DumpFindings(AvifDocument document)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("findings ");
        AppendInt(builder, document.GetFindingCount());
        builder.Append('\n');
        int index = 0;
        while (index < document.GetFindingCount())
        {
            AvifDiagnostic? finding = document.GetFinding(index);
            builder.Append("finding severity=");
            builder.Append((finding ?? throw new InvalidOperationException("Invalid internal state.")).Severity.ToString());
            builder.Append(" code=");
            builder.Append(finding.Code.ToString());
            builder.Append(" category=");
            builder.Append(finding.Category.ToString());
            builder.Append(" box=");
            builder.Append(Sanitize(finding.BoxPath));
            builder.Append(" item=");
            AppendUInt(builder, finding.ItemId);
            builder.Append(" offset=");
            AppendLong(builder, finding.ByteOffset);
            builder.Append(" standard=");
            builder.Append(Sanitize(finding.StandardReference));
            builder.Append(" message=");
            builder.Append(Sanitize(finding.Message));
            builder.Append('\n');
            index++;
        }

        return builder.ToString();
    }

    internal static string Sanitize(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    internal static void AppendInt(StringBuilder builder, int value)
    {
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AppendUInt(StringBuilder builder, uint value)
    {
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AppendLong(StringBuilder builder, long value)
    {
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AppendULong(StringBuilder builder, ulong value)
    {
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }
}
