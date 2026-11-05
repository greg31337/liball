using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace avif;

internal class AvifRemuxResult
{
    internal AvifResultCode Code;
    internal byte[] Data;
    internal AvifContainerValidationResult? InputValidation;
    internal AvifContainerValidationResult? OutputValidation;
    internal bool PreservedSourceBytes;
    internal int PreservedOpaqueBoxCount;
    internal int PreservedOpaquePropertyCount;
    internal int PreservedOpaqueMetadataCount;

    internal AvifRemuxResult(AvifResultCode code)
    {
        Code = code;
        Data = System.Array.Empty<byte>();
        InputValidation = null;
        OutputValidation = null;
        PreservedSourceBytes = false;
        PreservedOpaqueBoxCount = 0;
        PreservedOpaquePropertyCount = 0;
        PreservedOpaqueMetadataCount = 0;
    }
}

internal class AvifMediaDataRelocation
{
    internal ulong OldPayloadOffset;
    internal ulong OldEndOffset;
    internal ulong NewPayloadOffset;

    internal AvifMediaDataRelocation(
        ulong oldPayloadOffset,
        ulong oldEndOffset,
        ulong newPayloadOffset)
    {
        OldPayloadOffset = oldPayloadOffset;
        OldEndOffset = oldEndOffset;
        NewPayloadOffset = newPayloadOffset;
    }
}

internal static class AvifRemuxer
{
    internal static AvifRemuxResult Rewrite(
        byte[] source,
        AvifRewriteOptions options)
    {
        return Rewrite(source, options, null);
    }

    internal static AvifRemuxResult Rewrite(
        byte[]? source,
        AvifRewriteOptions? options,
        AvifRewriteEdits? edits)
    {
        if (source == null)
        {
            return new AvifRemuxResult(AvifResultCode.InvalidArgument);
        }

        return Rewrite(source.AsSpan(), options, edits);
    }

    internal static AvifRemuxResult Rewrite(
        ReadOnlySpan<byte> source,
        AvifRewriteOptions options)
    {
        return Rewrite(source, options, null);
    }

    internal static AvifRemuxResult Rewrite(
        ReadOnlySpan<byte> source,
        AvifRewriteOptions? options,
        AvifRewriteEdits? edits)
    {
        AvifRemuxResult result = new AvifRemuxResult(AvifResultCode.InvalidArgument);
        if (!ValidateOptions(options))
        {
            return result;
        }

        AvifValidationOptions validationOptions = CreateValidationOptions(options, source.Length);
        AvifContainerValidationResult input = AvifContainerValidator.Validate(
            source,
            validationOptions);
        result.InputValidation = input;
        if (input.Code != AvifResultCode.Success || input.Meta == null)
        {
            result.Code = input.Code;
            return result;
        }

        if (CanPreserveWholeSource(options
            ?? throw new InvalidOperationException("Invalid internal state."), edits))
        {
            result.Code = AvifResultCode.Success;
            result.Data = source.ToArray();
            result.OutputValidation = input;
            result.PreservedSourceBytes = true;
            return result;
        }

        IsoBoxParseResult preservedBoxes = IsoBoxReader.Parse(
            source,
            options.Limits,
            options.PreserveUnknownBoxes);
        if (preservedBoxes.Code != AvifResultCode.Success)
        {
            result.Code = preservedBoxes.Code;
            return result;
        }

        CanonicalItemContainerRequest? request = CreateRequest(
            source,
            input,
            options,
            edits,
            result);
        if (request == null)
        {
            if (result.Code == AvifResultCode.InvalidArgument)
            {
                result.Code = AvifResultCode.RewriteFailed;
            }

            return result;
        }

        CanonicalItemContainerWriteResult rewritten = CanonicalItemContainerWriter.Write(request);
        if (rewritten.Code != AvifResultCode.Success)
        {
            result.Code = rewritten.Code;
            result.OutputValidation = rewritten.SelfValidation;
            return result;
        }

        byte[]? remuxed = AppendTimedMediaAndMovie(
            rewritten.Data,
            source,
            input,
            options);
        if (remuxed == null)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }

        byte[]? output = AppendOpaqueTopLevelBoxes(
            remuxed,
            preservedBoxes.Boxes,
            options,
            result,
            edits);
        if (output == null)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }

        if (options.ValidateOutput)
        {
            AvifValidationOptions outputOptions = CreateValidationOptions(options, output.Length);
            result.OutputValidation = AvifContainerValidator.Validate(output, outputOptions);
            if (result.OutputValidation.Code != AvifResultCode.Success)
            {
                result.Code = result.OutputValidation.Code;
                return result;
            }
        }
        else
        {
            result.OutputValidation = rewritten.SelfValidation;
        }

        result.Code = AvifResultCode.Success;
        result.Data = output;
        return result;
    }

    internal static bool ValidateOptions(AvifRewriteOptions? options)
    {
        return options != null
            && options.Limits != null
            && ResourceGuard.ValidateLimits(options.Limits) == AvifResultCode.Success
            && (options.HeaderFormat == AvifHeaderFormat.Automatic
                || options.HeaderFormat == AvifHeaderFormat.Full
                || options.HeaderFormat == AvifHeaderFormat.Minimized
                || options.HeaderFormat == AvifHeaderFormat.PreserveExisting);
    }

    internal static bool CanPreserveWholeSource(AvifRewriteOptions options)
    {
        return CanPreserveWholeSource(options, null);
    }

    internal static bool CanPreserveWholeSource(
        AvifRewriteOptions options,
        AvifRewriteEdits? edits)
    {
        return options.HeaderFormat == AvifHeaderFormat.PreserveExisting
            && !options.Canonicalize
            && options.PreserveUnknownBoxes
            && options.PreserveUnknownProperties
            && options.PreserveUnknownMetadata
            && options.PreserveProtectedData
            && (edits == null || !edits.HasChanges());
    }

    internal static AvifValidationOptions CreateValidationOptions(
        AvifRewriteOptions? options,
        long sourceLength)
    {
        AvifValidationOptions validation = new AvifValidationOptions();
        validation.Mode = AvifValidationMode.Strict;
        validation.AllowUnknownNonEssentialData = true;
        validation.ValidateEncodedPayload = false;
        validation.DecodePixelData = false;
        validation.CollectInformation = false;
        validation.CollectWarnings = true;
        validation.StopAfterFirstError = false;
        validation.Limits = (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits;
        if (sourceLength > options.Limits.MaxSourceBytes)
        {
            validation.Limits = ResourceGuard.CopyLimits(options.Limits);
        }

        return validation;
    }

    internal static CanonicalItemContainerRequest? CreateRequest(
        ReadOnlySpan<byte> source,
        AvifContainerValidationResult input,
        AvifRewriteOptions options,
        AvifRewriteEdits? edits,
        AvifRemuxResult result)
    {
        ParsedMeta? meta = input.Meta;
        HashSet<uint> removedMetadataItemIds =
            FindRemovedMetadataItems(meta, edits);
        HashSet<uint> keptItemIds = SelectItems(
            meta,
            options,
            removedMetadataItemIds,
            result);
        if (edits != null)
        {
            int addedItemIndex = 0;
            while (addedItemIndex < edits.ItemAdditions.Count)
            {
                keptItemIds.Add(
                    edits.ItemAdditions[addedItemIndex].ItemId);
                addedItemIndex++;
            }
        }
        uint primaryItemId = edits != null && edits.HasPrimaryItem
            ? edits.PrimaryItemId : (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        if (keptItemIds == null || !keptItemIds.Contains(primaryItemId))
        {
            result.Code = AvifResultCode.UnsupportedFeature;
            return null;
        }

        List<CanonicalItemPayload> items = new List<CanonicalItemPayload>();
        int itemIndex = 0;
        while (itemIndex < (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            if (keptItemIds.Contains((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId()))
            {
                if (item.GetProtectionIndex() != 0)
                {
                    result.Code = AvifResultCode.UnsupportedProtection;
                    return null;
                }

                EncodedItemExtractionResult extracted = meta.ExtractEncodedItem(
                    source,
                    item.GetItemId());
                if (extracted.Code != AvifResultCode.Success || extracted.Data == null)
                {
                    result.Code = extracted.Code;
                    return null;
                }

                byte[] data = extracted.Data.GetSpan().ToArray();
                extracted.Data.Close();
                CanonicalItemPayload payload = CanonicalItemPayload.Create(
                    item.GetItemId(),
                    item.GetItemType(),
                    item.GetName(),
                    item.IsHidden(),
                    data);
                payload.ContentType = item.GetContentType();
                payload.ContentEncoding = item.GetContentEncoding();
                payload.ItemUriType = item.GetItemUriType();
                AppendMetadataObuEdits(payload, edits);
                if (!AddProperties(item, payload, options, result))
                {
                    return null;
                }
                AddOpaquePropertyEdits(
                    payload,
                    edits);
                AvifRewriteTransformEdit? transformEdit =
                    FindTransformEdit(edits, item.GetItemId());
                if (transformEdit != null)
                {
                    ReplaceTransformProperties(
                        payload,
                        transformEdit.Transform);
                }

                items.Add(payload);
            }

            itemIndex++;
        }

        if (edits != null)
        {
            itemIndex = 0;
            while (itemIndex < edits.ItemAdditions.Count)
            {
                CanonicalItemPayload sourceItem =
                    edits.ItemAdditions[itemIndex];
                CanonicalItemPayload payload = CanonicalItemPayload.Create(
                    sourceItem.ItemId,
                    sourceItem.ItemType,
                    sourceItem.Name,
                    sourceItem.Hidden,
                    sourceItem.Data);
                payload.ContentType = sourceItem.ContentType;
                payload.ContentEncoding = sourceItem.ContentEncoding;
                payload.ItemUriType = sourceItem.ItemUriType;
                int propertyIndex = 0;
                while (propertyIndex < sourceItem.Properties.Count)
                {
                    CanonicalItemPropertyAssociation property =
                        sourceItem.Properties[propertyIndex];
                    payload.Properties.Add(
                        new CanonicalItemPropertyAssociation(
                            property.RawProperty,
                            property.Essential));
                    propertyIndex++;
                }
                AppendMetadataObuEdits(payload, edits);
                items.Add(payload);
                itemIndex++;
            }
        }

        List<CanonicalItemReference> references = CreateReferences(
            meta,
            keptItemIds,
            edits);
        AddMetadataItems(items, references, edits);
        List<CanonicalEntityGroup> groups = CreateGroups(meta, keptItemIds);
        if (edits != null)
        {
            int groupIndex = 0;
            while (groupIndex < edits.GroupAdditions.Count)
            {
                CanonicalEntityGroup group =
                    edits.GroupAdditions[groupIndex];
                groups.Add(new CanonicalEntityGroup(
                    group.Type,
                    group.GroupId,
                    group.EntityIds
                        ?? throw new InvalidOperationException(
                            "Invalid internal state.")));
                groupIndex++;
            }
        }
        CanonicalItemContainerOptions containerOptions = new CanonicalItemContainerOptions();
        containerOptions.HeaderFormat = ResolveHeaderFormat(input.Boxes, options);
        containerOptions.MaximumOutputBytes = options.Limits.MaxSourceBytes;
        FileTypeBox? fileType = input.FileType;
        List<string> brands = fileType == null
            ? new List<string>()
            : new List<string>(fileType.CompatibleBrands);
        string majorBrand = fileType == null ? "avif" : fileType.MajorBrand;
        ApplyBrandEdits(brands, edits);
        if (edits != null && ContainsString(edits.RemovedBrands, majorBrand))
        {
            majorBrand = "avif";
        }
        uint minorVersion = fileType == null ? 0 : fileType.MinorVersion;
        CanonicalItemContainerRequest request =
            CanonicalItemContainerRequest.Create(
            majorBrand,
            minorVersion,
            brands,
            primaryItemId,
            items,
            references,
            groups,
            containerOptions);
        request.TimedSequence = input.Movie != null;
        return request;
    }

    internal static HashSet<uint> SelectItems(
        ParsedMeta? meta,
        AvifRewriteOptions options,
        HashSet<uint> removedItemIds,
        AvifRemuxResult result)
    {
        HashSet<uint> kept = new HashSet<uint>();
        int index = 0;
        while (index < (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = meta.GetItem(index);
            bool unknownMetadata = IsOpaqueMetadataItem(
                meta,
                item ?? throw new InvalidOperationException("Invalid internal state."));
            if ((removedItemIds == null
                    || !removedItemIds.Contains(item.GetItemId()))
                && (!unknownMetadata || options.PreserveUnknownMetadata))
            {
                kept.Add(item.GetItemId());
                if (unknownMetadata)
                {
                    result.PreservedOpaqueMetadataCount++;
                }
            }

            index++;
        }

        return kept;
    }

    static bool IsOpaqueMetadataItem(
        ParsedMeta meta,
        ParsedItem item)
    {
        if (IsKnownItemType(item.GetItemType()))
        {
            return false;
        }
        int index = 0;
        while (index < meta.GetReferenceCount())
        {
            ParsedItemReference? reference = meta.GetReference(index);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetBoxType() == "cdsc"
                && reference.GetFromItemId() == item.GetItemId()
                && reference.GetTargetCount() != 0)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool AddProperties(
        ParsedItem item,
        CanonicalItemPayload payload,
        AvifRewriteOptions options,
        AvifRemuxResult result)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedPropertyAssociation? association = item.GetPropertyAssociation(index);
            ParsedItemProperty property = (association ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty();
            bool known = AvifContainerValidator.HasSupportedPropertySemantics(property);
            if (known || options.PreserveUnknownProperties)
            {
                byte[] raw = property.GetRawData();
                CanonicalPropertyProbe probe = CanonicalItemContainerWriter.ProbeProperty(raw);
                if (probe.Code != AvifResultCode.Success && known)
                {
                    raw = RebuildKnownProperty(property);
                    probe = CanonicalItemContainerWriter.ProbeProperty(raw);
                }

                if (probe.Code != AvifResultCode.Success)
                {
                    result.Code = AvifResultCode.UnsupportedFeature;
                    return false;
                }

                payload.Properties.Add(new CanonicalItemPropertyAssociation(
                    raw,
                    association.IsEssential()));
                if (!known)
                {
                    result.PreservedOpaquePropertyCount++;
                }
            }

            index++;
        }

        return true;
    }

    internal static byte[] RebuildKnownProperty(ParsedItemProperty property)
    {
        string type = property.GetBoxType();
        if (type == "av1C")
        {
            byte[] compact = property.GetRawData();
            if (compact.Length == 4)
            {
                return BuildProperty(type, compact);
            }
        }
        else if (type == "ispe" && property.GetSpatialExtents() != null)
        {
            ParsedImageSpatialExtents? extents = property.GetSpatialExtents();
            byte[] payload = new byte[12];
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), (extents ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth());
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), extents.GetHeight());
            return BuildProperty(type, payload);
        }
        else if (type == "pixi" && property.GetPixelInformation() != null)
        {
            ParsedPixelInformation? pixels = property.GetPixelInformation();
            if (!(pixels ?? throw new InvalidOperationException("Invalid internal state.")).IsExtended() && pixels.GetChannelCount() <= byte.MaxValue)
            {
                byte[] payload = new byte[5 + pixels.GetChannelCount()];
                payload[4] = (byte)pixels.GetChannelCount();
                int index = 0;
                while (index < pixels.GetChannelCount())
                {
                    payload[5 + index] = (byte)(pixels.GetChannel(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth();
                    index++;
                }

                return BuildProperty(type, payload);
            }
        }
        else if (type == "colr" && property.GetColourInformation() != null)
        {
            ParsedColourInformation? colour = property.GetColourInformation();
            if ((colour ?? throw new InvalidOperationException("Invalid internal state.")).GetColourType() == "nclx")
            {
                byte[] payload = new byte[11];
                WriteFourCc(payload, "nclx");
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(4, 2),
                    (ushort)colour.GetColourPrimaries());
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(6, 2),
                    (ushort)colour.GetTransferCharacteristics());
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(8, 2),
                    (ushort)colour.GetMatrixCoefficients());
                payload[10] = colour.IsFullRange() ? (byte)128 : (byte)0;
                return BuildProperty(type, payload);
            }

            if (colour.GetColourType() == "prof" || colour.GetColourType() == "rICC")
            {
                byte[] data = colour.GetData();
                byte[] payload = new byte[4 + data.Length];
                WriteFourCc(payload, colour.GetColourType());
                data.CopyTo(payload, 4);
                return BuildProperty(type, payload);
            }
        }
        else if (type == "irot" && property.GetImageRotation() != null)
        {
            return BuildProperty(type, new byte[]
            {
                (byte)(property.GetImageRotation() ?? throw new InvalidOperationException("Invalid internal state.")).GetQuarterTurnsCounterClockwise()
            });
        }
        else if (type == "imir" && property.GetImageMirror() != null)
        {
            return BuildProperty(type, new byte[]
            {
                (byte)(property.GetImageMirror() ?? throw new InvalidOperationException("Invalid internal state.")).GetAxis()
            });
        }
        else if (type == "auxC" && property.GetAuxiliaryType() != null)
        {
            ParsedAuxiliaryType? auxiliary = property.GetAuxiliaryType();
            byte[] typeBytes = Encoding.UTF8.GetBytes((auxiliary ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeValue());
            byte[] subtypes = auxiliary.GetSubtypes();
            byte[] payload = new byte[5 + typeBytes.Length + subtypes.Length];
            typeBytes.CopyTo(payload, 4);
            payload[4 + typeBytes.Length] = 0;
            subtypes.CopyTo(payload, 5 + typeBytes.Length);
            return BuildProperty(type, payload);
        }

        return System.Array.Empty<byte>();
    }

    internal static byte[] BuildProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(0, 4), (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 8);
        return raw;
    }

    internal static void WriteFourCc(byte[] destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    internal static List<CanonicalItemReference> CreateReferences(
        ParsedMeta meta,
        HashSet<uint> keptItemIds,
        AvifRewriteEdits? edits)
    {
        List<CanonicalItemReference> references = new List<CanonicalItemReference>();
        int index = 0;
        while (index < meta.GetReferenceCount())
        {
            ParsedItemReference? reference = meta.GetReference(index);
            bool keep = keptItemIds.Contains((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetFromItemId());
            List<uint> targets = new List<uint>();
            int targetIndex = 0;
            while (targetIndex < reference.GetTargetCount())
            {
                uint target = reference.GetTargetItemId(targetIndex);
                bool removeDescription = reference.GetBoxType() == "cdsc"
                    && ShouldRemoveMetadataLink(
                        meta.FindItem(reference.GetFromItemId()),
                        target,
                        edits);
                if (!keptItemIds.Contains(target)
                    && reference.GetBoxType() != "cdsc")
                {
                    keep = false;
                }
                if (keptItemIds.Contains(target) && !removeDescription)
                {
                    targets.Add(target);
                }
                targetIndex++;
            }

            if (keep && targets.Count != 0)
            {
                references.Add(new CanonicalItemReference(
                    reference.GetBoxType(),
                    reference.GetFromItemId(),
                    targets));
            }

            index++;
        }

        if (edits != null)
        {
            index = 0;
            while (index < edits.MetadataAssociations.Count)
            {
                AvifRewriteMetadataAssociation association =
                    edits.MetadataAssociations[index];
                CanonicalItemReference? existing = FindCanonicalReference(
                    references,
                    "cdsc",
                    association.MetadataItemId);
                if (existing == null)
                {
                    references.Add(new CanonicalItemReference(
                        "cdsc",
                        association.MetadataItemId,
                        new List<uint> { association.TargetItemId }));
                }
                else if (!ContainsCanonicalTarget(
                    existing,
                    association.TargetItemId))
                {
                    (existing.ToItemIds ?? throw new InvalidOperationException("Invalid internal state."))
                        .Add(association.TargetItemId);
                }
                index++;
            }
            index = 0;
            while (index < edits.ReferenceAdditions.Count)
            {
                CanonicalItemReference addition =
                    edits.ReferenceAdditions[index];
                CanonicalItemReference? existing = FindCanonicalReference(
                    references,
                    addition.Type,
                    addition.FromItemId);
                if (existing == null)
                {
                    references.Add(new CanonicalItemReference(
                        addition.Type,
                        addition.FromItemId,
                        addition.ToItemIds
                            ?? throw new InvalidOperationException(
                                "Invalid internal state.")));
                }
                else
                {
                    int targetIndex = 0;
                    while (targetIndex < (addition.ToItemIds
                        ?? throw new InvalidOperationException(
                            "Invalid internal state.")).Count)
                    {
                        uint target = addition.ToItemIds[targetIndex];
                        if (!ContainsCanonicalTarget(existing, target))
                        {
                            (existing.ToItemIds
                                ?? throw new InvalidOperationException(
                                    "Invalid internal state."))
                                .Add(target);
                        }
                        targetIndex++;
                    }
                }
                index++;
            }
        }

        return references;
    }

    static CanonicalItemReference? FindCanonicalReference(
        List<CanonicalItemReference> references,
        string type,
        uint itemId)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].Type == type
                && references[index].FromItemId == itemId)
            {
                return references[index];
            }
            index++;
        }
        return null;
    }

    static bool ContainsCanonicalTarget(
        CanonicalItemReference reference,
        uint itemId)
    {
        int index = 0;
        while (index < (reference.ToItemIds
            ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (reference.ToItemIds[index] == itemId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static void AppendMetadataObuEdits(
        CanonicalItemPayload payload,
        AvifRewriteEdits? edits)
    {
        if (edits == null)
        {
            return;
        }
        int index = 0;
        while (index < edits.MetadataObuAdditions.Count)
        {
            AvifRewriteMetadataObuAddition addition =
                edits.MetadataObuAdditions[index];
            if (addition.ItemId == payload.ItemId)
            {
                byte[] combined = new byte[
                    payload.Data.Length + addition.Data.Length];
                payload.Data.CopyTo(combined, 0);
                addition.Data.CopyTo(combined, payload.Data.Length);
                payload.Data = combined;
                payload.Length = combined.Length;
            }
            index++;
        }
    }

    static void AddOpaquePropertyEdits(
        CanonicalItemPayload payload,
        AvifRewriteEdits? edits)
    {
        if (edits == null)
        {
            return;
        }
        int index = 0;
        while (index < edits.PropertyAdditions.Count)
        {
            AvifRewritePropertyAddition addition =
                edits.PropertyAdditions[index];
            if (addition.ItemId == payload.ItemId)
            {
                payload.Properties.Add(
                    new CanonicalItemPropertyAssociation(
                        addition.RawProperty,
                        false));
            }
            index++;
        }
    }

    internal static HashSet<uint> FindRemovedMetadataItems(
        ParsedMeta? meta,
        AvifRewriteEdits? edits)
    {
        HashSet<uint> removed = new HashSet<uint>();
        if (meta == null || edits == null || edits.MetadataRemovals.Count == 0)
        {
            return removed;
        }
        int itemIndex = 0;
        while (itemIndex < meta.GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            bool matched = false;
            bool retained = false;
            int referenceIndex = 0;
            while (referenceIndex < meta.GetReferenceCount())
            {
                ParsedItemReference? reference = meta.GetReference(referenceIndex);
                if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "cdsc"
                    && reference.GetFromItemId() == (item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId())
                {
                    int targetIndex = 0;
                    while (targetIndex < reference.GetTargetCount())
                    {
                        uint target = reference.GetTargetItemId(targetIndex);
                        if (ShouldRemoveMetadataLink(item, target, edits))
                        {
                            matched = true;
                        }
                        else
                        {
                            retained = true;
                        }
                        targetIndex++;
                    }
                }
                referenceIndex++;
            }
            if (matched && !retained)
            {
                removed.Add((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            }
            itemIndex++;
        }
        return removed;
    }

    internal static bool ShouldRemoveMetadataLink(
        ParsedItem? metadataItem,
        uint targetItemId,
        AvifRewriteEdits? edits)
    {
        if (metadataItem == null || edits == null)
        {
            return false;
        }
        int index = 0;
        while (index < edits.MetadataRemovals.Count)
        {
            AvifRewriteMetadataRemoval removal = edits.MetadataRemovals[index];
            if (removal.TargetItemId == targetItemId
                && MetadataKindMatches(metadataItem, removal.Kind))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool MetadataKindMatches(
        ParsedItem? item,
        AvifMetadataKind kind)
    {
        bool exif = (item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType() == "Exif";
        bool xmp = item.GetItemType() == "mime"
            && item.GetContentType() == "application/rdf+xml";
        if (kind == AvifMetadataKind.Exif)
        {
            return exif;
        }
        if (kind == AvifMetadataKind.Xmp)
        {
            return xmp;
        }
        return kind == AvifMetadataKind.Custom && !exif && !xmp;
    }

    internal static AvifRewriteTransformEdit? FindTransformEdit(
        AvifRewriteEdits? edits,
        uint itemId)
    {
        if (edits == null)
        {
            return null;
        }
        int index = 0;
        while (index < edits.TransformEdits.Count)
        {
            if (edits.TransformEdits[index].ItemId == itemId)
            {
                return edits.TransformEdits[index];
            }
            index++;
        }
        return null;
    }

    internal static void ReplaceTransformProperties(
        CanonicalItemPayload payload,
        AvifTransform transform)
    {
        List<CanonicalItemPropertyAssociation> properties =
            new List<CanonicalItemPropertyAssociation>();
        int index = 0;
        while (index < payload.Properties.Count)
        {
            CanonicalItemPropertyAssociation property = payload.Properties[index];
            CanonicalPropertyProbe probe =
                CanonicalItemContainerWriter.ProbeProperty(
                    property.RawProperty);
            if (probe.Code != AvifResultCode.Success
                || (probe.Type != "clap" && probe.Type != "irot"
                    && probe.Type != "imir" && probe.Type != "pasp"))
            {
                properties.Add(property);
            }
            index++;
        }
        AddTransformProperties(properties, transform);
        payload.Properties = properties;
    }

    internal static void AddTransformProperties(
        List<CanonicalItemPropertyAssociation> properties,
        AvifTransform transform)
    {
        if (transform.HasCleanAperture())
        {
            AvifCrop? crop = transform.GetCleanAperture();
            byte[] payload = new byte[32];
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(0, 4), (crop ?? throw new InvalidOperationException("Invalid internal state.")).GetWidthNumerator());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(4, 4), crop.GetWidthDenominator());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(8, 4), crop.GetHeightNumerator());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(12, 4), crop.GetHeightDenominator());
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(16, 4), crop.GetHorizontalOffsetNumerator());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(20, 4), crop.GetHorizontalOffsetDenominator());
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(24, 4), crop.GetVerticalOffsetNumerator());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(28, 4), crop.GetVerticalOffsetDenominator());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("clap", payload),
                true));
        }
        if (transform.HasRotation())
        {
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("irot", new byte[]
                {
                    (byte)transform.GetRotationQuarterTurnsCounterClockwise()
                }),
                true));
        }
        if (transform.HasMirror())
        {
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("imir", new byte[]
                {
                    (byte)transform.GetMirrorAxis()
                }),
                true));
        }
        if (transform.HasPixelAspectRatio())
        {
            AvifPixelAspectRatio ratio = transform.GetPixelAspectRatio();
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(0, 4), ratio.GetHorizontalSpacing());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(4, 4), ratio.GetVerticalSpacing());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("pasp", payload),
                false));
        }
    }

    internal static void AddMetadataItems(
        List<CanonicalItemPayload> items,
        List<CanonicalItemReference> references,
        AvifRewriteEdits? edits)
    {
        if (edits == null)
        {
            return;
        }
        int index = 0;
        while (index < edits.MetadataAdditions.Count)
        {
            AvifRewriteMetadataAddition addition =
                edits.MetadataAdditions[index];
            CanonicalItemPayload item = CanonicalItemPayload.Create(
                addition.ItemId,
                addition.ItemType,
                GetMetadataName(addition.Kind),
                true,
                addition.Data);
            item.ContentType = addition.ContentType;
            item.ContentEncoding = addition.ContentEncoding ?? string.Empty;
            item.ItemUriType = addition.ItemUriType ?? string.Empty;
            items.Add(item);
            references.Add(new CanonicalItemReference(
                "cdsc",
                addition.ItemId,
                new List<uint>(addition.TargetItemIds)));
            index++;
        }
    }

    internal static string GetMetadataName(AvifMetadataKind kind)
    {
        if (kind == AvifMetadataKind.Exif)
        {
            return "Exif";
        }
        if (kind == AvifMetadataKind.Xmp)
        {
            return "XMP";
        }
        return "metadata";
    }

    internal static void ApplyBrandEdits(
        List<string> brands,
        AvifRewriteEdits? edits)
    {
        if (edits == null)
        {
            return;
        }
        int index = brands.Count - 1;
        while (index >= 0)
        {
            if (ContainsString(edits.RemovedBrands, brands[index]))
            {
                brands.RemoveAt(index);
            }
            index--;
        }
        index = 0;
        while (index < edits.AddedBrands.Count)
        {
            if (!ContainsString(brands, edits.AddedBrands[index]))
            {
                brands.Add(edits.AddedBrands[index]);
            }
            index++;
        }
    }

    internal static bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static List<CanonicalEntityGroup> CreateGroups(
        ParsedMeta meta,
        HashSet<uint> keptItemIds)
    {
        List<CanonicalEntityGroup> groups = new List<CanonicalEntityGroup>();
        int index = 0;
        while (index < meta.GetEntityGroupCount())
        {
            ParsedEntityGroup? group = meta.GetEntityGroup(index);
            bool keep = true;
            List<uint> entities = new List<uint>();
            int entityIndex = 0;
            while (entityIndex < (group ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityCount())
            {
                uint entity = group.GetEntityId(entityIndex);
                if (!keptItemIds.Contains(entity))
                {
                    keep = false;
                }

                entities.Add(entity);
                entityIndex++;
            }

            if (keep)
            {
                groups.Add(new CanonicalEntityGroup(
                    group.GetGroupingType(),
                    group.GetGroupId(),
                    entities));
            }

            index++;
        }

        return groups;
    }

    internal static AvifHeaderFormat ResolveHeaderFormat(
        IsoBoxParseResult? boxes,
        AvifRewriteOptions options)
    {
        if (options.HeaderFormat != AvifHeaderFormat.PreserveExisting)
        {
            return options.HeaderFormat;
        }

        if (boxes != null)
        {
            int index = 0;
            while (index < boxes.Boxes.Count)
            {
                if (boxes.Boxes[index].Type == "mini")
                {
                    return AvifHeaderFormat.Minimized;
                }

                index++;
            }
        }

        return AvifHeaderFormat.Full;
    }

    internal static byte[]? AppendTimedMediaAndMovie(
        byte[] rewritten,
        ReadOnlySpan<byte> source,
        AvifContainerValidationResult input,
        AvifRewriteOptions options)
    {
        if (input.Movie == null)
        {
            return rewritten;
        }
        IsoBox? movieBox = FindTopLevelBox(input.Boxes, "moov");
        if (movieBox == null || movieBox.Size > int.MaxValue)
        {
            return null;
        }
        List<IsoBox> mediaBoxes = FindReferencedMediaDataBoxes(
            input.Boxes,
            input.Movie);
        if (mediaBoxes.Count == 0)
        {
            return null;
        }
        long additional = movieBox.Size;
        int index = 0;
        while (index < mediaBoxes.Count)
        {
            if (mediaBoxes[index].Size > int.MaxValue
                || additional > long.MaxValue - mediaBoxes[index].Size)
            {
                return null;
            }
            additional += mediaBoxes[index].Size;
            index++;
        }
        CheckedIntegerResult total = CheckedMath.AddInt64(
            rewritten.LongLength,
            additional);
        if (!total.Success
            || total.Value > options.Limits.MaxSourceBytes
            || total.Value > System.Array.MaxLength)
        {
            return null;
        }
        byte[] output = new byte[(int)total.Value];
        rewritten.CopyTo(output, 0);
        int cursor = rewritten.Length;
        List<AvifMediaDataRelocation> relocations =
            new List<AvifMediaDataRelocation>();
        index = 0;
        while (index < mediaBoxes.Count)
        {
            IsoBox media = mediaBoxes[index];
            source.Slice((int)media.Offset, (int)media.Size)
                .CopyTo(output.AsSpan(cursor, (int)media.Size));
            if (media.ExtendsToEnd)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    output.AsSpan(cursor, 4),
                    (uint)media.Size);
            }
            relocations.Add(new AvifMediaDataRelocation(
                (ulong)media.PayloadOffset,
                (ulong)media.EndOffset,
                (ulong)cursor + (uint)media.HeaderSize));
            cursor += (int)media.Size;
            index++;
        }
        byte[] movie = source.Slice(
            (int)movieBox.Offset,
            (int)movieBox.Size).ToArray();
        if (!PatchChunkOffsets(movie, movieBox, relocations))
        {
            return null;
        }
        movie.CopyTo(output, cursor);
        return output;
    }

    internal static List<IsoBox> FindReferencedMediaDataBoxes(
        IsoBoxParseResult? boxes,
        ParsedMovie movie)
    {
        List<IsoBox> result = new List<IsoBox>();
        if (boxes == null || movie == null)
        {
            return result;
        }
        int boxIndex = 0;
        while (boxIndex < boxes.Boxes.Count)
        {
            IsoBox box = boxes.Boxes[boxIndex];
            if (box.Type == "mdat" && MovieUsesMediaDataBox(movie, box))
            {
                result.Add(box);
            }
            boxIndex++;
        }
        return result;
    }

    internal static bool MovieUsesMediaDataBox(
        ParsedMovie movie,
        IsoBox mediaBox)
    {
        int trackIndex = 0;
        while (trackIndex < movie.Tracks.Count)
        {
            List<ulong> offsets = movie.Tracks[trackIndex]
                .SampleTable.ChunkOffsets;
            int offsetIndex = 0;
            while (offsetIndex < offsets.Count)
            {
                ulong offset = offsets[offsetIndex];
                if (offset >= (ulong)mediaBox.PayloadOffset
                    && offset < (ulong)mediaBox.EndOffset)
                {
                    return true;
                }
                offsetIndex++;
            }
            trackIndex++;
        }
        return false;
    }

    internal static IsoBox? FindTopLevelBox(
        IsoBoxParseResult? boxes,
        string type)
    {
        if (boxes == null)
        {
            return null;
        }
        int index = 0;
        while (index < boxes.Boxes.Count)
        {
            if (boxes.Boxes[index].Type == type)
            {
                return boxes.Boxes[index];
            }
            index++;
        }
        return null;
    }

    internal static bool PatchChunkOffsets(
        byte[] movie,
        IsoBox movieBox,
        List<AvifMediaDataRelocation> relocations)
    {
        return PatchChunkOffsetChildren(
            movie,
            movieBox,
            movieBox,
            relocations);
    }

    internal static bool PatchChunkOffsetChildren(
        byte[] movie,
        IsoBox movieBox,
        IsoBox parent,
        List<AvifMediaDataRelocation> relocations)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            IsoBox child = parent.Children[index];
            if (child.Type == "stco" || child.Type == "co64")
            {
                if (!PatchChunkOffsetBox(
                    movie,
                    movieBox,
                    child,
                    relocations))
                {
                    return false;
                }
            }
            if (child.Children.Count != 0
                && !PatchChunkOffsetChildren(
                    movie,
                    movieBox,
                    child,
                    relocations))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool PatchChunkOffsetBox(
        byte[] movie,
        IsoBox movieBox,
        IsoBox box,
        List<AvifMediaDataRelocation> relocations)
    {
        long localPayloadOffset = box.PayloadOffset - movieBox.Offset;
        int entrySize = box.Type == "stco" ? 4 : 8;
        if (localPayloadOffset < 0
            || localPayloadOffset > movie.LongLength - 8)
        {
            return false;
        }
        int cursor = (int)localPayloadOffset;
        uint entryCount = BinaryPrimitives.ReadUInt32BigEndian(
            movie.AsSpan(cursor + 4, 4));
        ulong required = 8 + (ulong)entryCount * (uint)entrySize;
        if (required != (ulong)box.PayloadSize)
        {
            return false;
        }
        cursor += 8;
        uint index = 0;
        while (index < entryCount)
        {
            ulong oldOffset = entrySize == 4
                ? BinaryPrimitives.ReadUInt32BigEndian(
                    movie.AsSpan(cursor, 4))
                : BinaryPrimitives.ReadUInt64BigEndian(
                    movie.AsSpan(cursor, 8));
            ulong newOffset = RelocateMediaOffset(oldOffset, relocations);
            if (newOffset == ulong.MaxValue
                || entrySize == 4 && newOffset > uint.MaxValue)
            {
                return false;
            }
            if (entrySize == 4)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    movie.AsSpan(cursor, 4),
                    (uint)newOffset);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(
                    movie.AsSpan(cursor, 8),
                    newOffset);
            }
            cursor += entrySize;
            index++;
        }
        return true;
    }

    internal static ulong RelocateMediaOffset(
        ulong oldOffset,
        List<AvifMediaDataRelocation> relocations)
    {
        int index = 0;
        while (index < relocations.Count)
        {
            AvifMediaDataRelocation relocation = relocations[index];
            if (oldOffset >= relocation.OldPayloadOffset
                && oldOffset < relocation.OldEndOffset)
            {
                ulong relative = oldOffset - relocation.OldPayloadOffset;
                if (relocation.NewPayloadOffset > ulong.MaxValue - relative)
                {
                    return ulong.MaxValue;
                }
                return relocation.NewPayloadOffset + relative;
            }
            index++;
        }
        return ulong.MaxValue;
    }

    internal static byte[]? AppendOpaqueTopLevelBoxes(
        byte[] rewritten,
        List<IsoBox> boxes,
        AvifRewriteOptions options,
        AvifRemuxResult result,
        AvifRewriteEdits? edits)
    {
        if (!options.PreserveUnknownBoxes
            && (edits == null || edits.TopLevelBoxAdditions.Count == 0))
        {
            return rewritten;
        }

        long additional = 0;
        int index = 0;
        while (index < boxes.Count)
        {
            if (options.PreserveUnknownBoxes
                && IsSafeOpaqueTopLevelBox(boxes[index]))
            {
                additional += boxes[index].RawData.LongLength;
            }

            index++;
        }
        if (edits != null)
        {
            index = 0;
            while (index < edits.TopLevelBoxAdditions.Count)
            {
                additional += edits.TopLevelBoxAdditions[index].LongLength;
                index++;
            }
        }

        if (additional == 0)
        {
            return rewritten;
        }

        CheckedIntegerResult size = CheckedMath.AddInt64(rewritten.LongLength, additional);
        if (!size.Success
            || size.Value > options.Limits.MaxSourceBytes
            || size.Value > System.Array.MaxLength)
        {
            return null;
        }

        byte[] output = new byte[(int)size.Value];
        rewritten.CopyTo(output, 0);
        int offset = rewritten.Length;
        index = 0;
        while (index < boxes.Count)
        {
            IsoBox box = boxes[index];
            if (options.PreserveUnknownBoxes
                && IsSafeOpaqueTopLevelBox(box))
            {
                box.RawData.CopyTo(output, offset);
                offset += box.RawData.Length;
                result.PreservedOpaqueBoxCount++;
            }

            index++;
        }
        if (edits != null)
        {
            index = 0;
            while (index < edits.TopLevelBoxAdditions.Count)
            {
                byte[] box = edits.TopLevelBoxAdditions[index];
                box.CopyTo(output, offset);
                offset += box.Length;
                result.PreservedOpaqueBoxCount++;
                index++;
            }
        }

        return output;
    }

    internal static bool IsSafeOpaqueTopLevelBox(IsoBox box)
    {
        return box != null
            && !box.ExtendsToEnd
            && box.RawData.Length == box.Size
            && !AvifDocumentBuilder.IsKnownBoxType(box.Type)
            && box.Type != "uuid";
    }

    internal static bool IsKnownItemType(string type)
    {
        return type == "av01"
            || type == "grid"
            || type == "tmap"
            || type == "sato"
            || type == "Exif"
            || type == "mime"
            || type == "uri ";
    }
}
