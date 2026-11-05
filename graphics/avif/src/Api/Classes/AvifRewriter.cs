using System;
using System.Collections.Generic;
using System.IO;

namespace avif;

/// <summary>Edits an existing AVIF container without re-encoding its image payloads.</summary>
/// <remarks>
/// Source bytes and staged metadata are copied. Successful no-op rewrites preserve the complete
/// source byte-for-byte, while edited output is rebuilt and validated according to the options.
/// </remarks>
public class AvifRewriter : IAvifRewriter, IAvifDiagnosticSource
{
    AvifRewriteOptions? options;
    AvifRewriteEdits? edits;
    AvifContainerValidationResult? validation;
    DiagnosticLog? diagnostics;
    byte[]? source;
    byte[]? finishedData;
    AvifResultCode code;
    uint nextItemId;
    uint nextGroupId;
    bool normalize;
    bool optionsValid;
    bool opened;
    bool finished;
    bool closed;

    /// <summary>Creates an unopened rewriter with preservation-first defaults.</summary>
    /// <returns>The initialized rewriter.</returns>
    internal static AvifRewriter Create()
    {
        return new AvifRewriter(new AvifRewriteOptions());
    }

    /// <summary>Creates an unopened rewriter with a snapshot of caller-selected options.</summary>
    /// <param name="options">The rewrite and preservation options to snapshot.</param>
    public AvifRewriter(AvifRewriteOptions options)
    {
        Initialize(options);
    }

    /// <summary>Creates a rewriter with caller-selected options.</summary>
    /// <returns>The initialized rewriter.</returns>
    internal static AvifRewriter Create(AvifRewriteOptions options)
    {
        return new AvifRewriter(options);
    }

    /// <summary>Returns an independent copy of the active rewrite options.</summary>
    /// <returns>The current rewrite and preservation options.</returns>
    public AvifRewriteOptions? GetOptions()
    {
        return CopyOptions(options);
    }

    /// <summary>Opens and validates one complete source copy.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode Open(byte[] source)
    {
        AvifResultCode state = CheckUnopenedState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (source == null)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (source.LongLength > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxSourceBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        this.source = (byte[])source.Clone();
        AvifValidationOptions validationOptions =
            AvifRemuxer.CreateValidationOptions(
                options,
                this.source.LongLength);
        validation = AvifContainerValidator.Validate(
            this.source,
            validationOptions);
        diagnostics = validation.Diagnostics;
        code = validation.Code;
        if (code != AvifResultCode.Success || validation.Meta == null)
        {
            this.source = System.Array.Empty<byte>();
            validation = null;
            return code;
        }
        nextItemId = FindNextItemId(validation.Meta);
        nextGroupId = uint.MaxValue;
        opened = true;
        return code;
    }

    /// <summary>Returns the result of the most recent rewrite operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics for the most recent rewrite operation.</summary>
    /// <returns>The structured rewrite diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromLog(
            diagnostics,
            GetCode(),
            AvifDiagnosticCategory.Rewrite,
            "The AVIF rewrite operation did not complete successfully.");
    }

    /// <summary>Selects an existing visible image as the rewritten primary item.</summary>
    /// <param name="itemId">The image item identifier.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    public AvifResultCode SetPrimaryItem(uint itemId)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        ParsedItem? item = ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        CanonicalItemPayload? addition = FindAddedItem(itemId);
        if (!IsPrimaryImageItem(item)
            && !IsPrimaryImageItemAddition(addition))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        (edits ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaryItem = itemId != (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        edits.PrimaryItemId = itemId;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds a caller-supplied conforming AV1 item without decoding or re-encoding it.</summary>
    /// <param name="payload">The complete low-overhead AV1 OBU stream to copy.</param>
    /// <param name="configuration">The matching AV1 codec configuration.</param>
    /// <param name="width">The positive coded width.</param>
    /// <param name="height">The positive coded height.</param>
    /// <param name="hidden">Whether the item is hidden.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    public uint AddAv1Item(
        byte[] payload,
        Av1CodecConfiguration configuration,
        int width,
        int height,
        bool hidden)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (!CanAddItem())
        {
            return 0;
        }
        AvifLowLevelItemResult authored =
            AvifLowLevelAuthoring.CreateAv1Item(
                nextItemId,
                payload,
                configuration,
                width,
                height,
                hidden,
                (options ?? throw new InvalidOperationException("Invalid internal state."))
                    .Limits);
        code = authored.Code;
        if (code != AvifResultCode.Success || authored.Item == null)
        {
            return 0;
        }
        uint itemId = nextItemId;
        nextItemId++;
        (edits ?? throw new InvalidOperationException("Invalid internal state."))
            .ItemAdditions.Add(authored.Item);
        code = AvifResultCode.Success;
        return itemId;
    }

    /// <summary>Adds an arbitrary copied non-AV1 item to the remuxed collection.</summary>
    /// <param name="itemType">The four-character item type.</param>
    /// <param name="name">The item name without embedded nulls.</param>
    /// <param name="contentType">The required MIME type for a <c>mime</c> item, otherwise empty.</param>
    /// <param name="contentEncoding">The optional MIME encoding, otherwise empty.</param>
    /// <param name="uriType">The required URI type for a <c>uri </c> item, otherwise empty.</param>
    /// <param name="payload">The item bytes to copy.</param>
    /// <param name="hidden">Whether the item is hidden.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    public uint AddItem(
        string itemType,
        string name,
        string contentType,
        string contentEncoding,
        string uriType,
        byte[] payload,
        bool hidden)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (!AvifLowLevelAuthoring.IsValidGenericItem(
                itemType,
                name,
                contentType,
                contentEncoding,
                uriType,
                payload))
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if (payload.LongLength > (options
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Limits.MaxSourceBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        if (!CanAddItem())
        {
            return 0;
        }
        uint itemId = nextItemId;
        nextItemId++;
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            itemId,
            itemType,
            name,
            hidden,
            payload);
        item.ContentType = contentType;
        item.ContentEncoding = contentEncoding;
        item.ItemUriType = uriType;
        (edits ?? throw new InvalidOperationException("Invalid internal state."))
            .ItemAdditions.Add(item);
        code = AvifResultCode.Success;
        return itemId;
    }

    /// <summary>Adds an arbitrary directional relationship between retained or staged items.</summary>
    /// <param name="type">The four-character reference type.</param>
    /// <param name="fromItemId">The source item identifier.</param>
    /// <param name="targetItemIds">One or more distinct target identifiers.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddItemReference(
        string type,
        uint fromItemId,
        uint[] targetItemIds)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!AvifLowLevelAuthoring.IsValidReference(
                type,
                fromItemId,
                targetItemIds)
            || !HasItem(fromItemId)
            || HasReference(type, fromItemId))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        int index = 0;
        while (index < targetItemIds.Length)
        {
            if (!HasItem(targetItemIds[index]))
            {
                code = AvifResultCode.InvalidArgument;
                return code;
            }
            index++;
        }
        if (GetReferenceCountAfterEdits()
            >= (options ?? throw new InvalidOperationException("Invalid internal state."))
                .Limits.MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        (edits ?? throw new InvalidOperationException("Invalid internal state."))
            .ReferenceAdditions.Add(new CanonicalItemReference(
                type,
                fromItemId,
                new List<uint>(targetItemIds)));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds an arbitrary entity group containing retained or staged items.</summary>
    /// <param name="type">The four-character grouping type.</param>
    /// <param name="entityIds">One or more distinct item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    public uint AddEntityGroup(string type, uint[] entityIds)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (!AvifLowLevelAuthoring.IsValidGroup(type, entityIds))
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        int index = 0;
        while (index < entityIds.Length)
        {
            if (!HasItem(entityIds[index]))
            {
                code = AvifResultCode.InvalidArgument;
                return 0;
            }
            index++;
        }
        if (((validation ?? throw new InvalidOperationException("Invalid internal state."))
                .Meta ?? throw new InvalidOperationException("Invalid internal state."))
                .GetEntityGroupCount()
            + (edits ?? throw new InvalidOperationException("Invalid internal state."))
                .GroupAdditions.Count
            >= (options ?? throw new InvalidOperationException("Invalid internal state."))
                .Limits.MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint groupId = AllocateRewriteGroupId();
        if (groupId == 0)
        {
            return 0;
        }
        edits.GroupAdditions.Add(new CanonicalEntityGroup(
            type,
            groupId,
            new List<uint>(entityIds)));
        code = AvifResultCode.Success;
        return groupId;
    }

    /// <summary>Adds one validated unknown nonessential property to any retained or staged item.</summary>
    /// <param name="itemId">The target item identifier.</param>
    /// <param name="property">The opaque property to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddOpaqueProperty(
        uint itemId,
        AvifOpaqueBox property)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!HasItem(itemId) || property == null
            || property.GetCode() != AvifResultCode.Success
            || property.IsEssential())
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] raw = property.GetRawData();
        if (HasProperty(itemId, raw))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (GetPropertyCountAfterEdits()
            >= (options ?? throw new InvalidOperationException("Invalid internal state."))
                .Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        CanonicalItemPayload? added = FindAddedItem(itemId);
        if (added != null)
        {
            added.Properties.Add(new CanonicalItemPropertyAssociation(
                raw,
                false));
        }
        else
        {
            (edits ?? throw new InvalidOperationException("Invalid internal state."))
                .PropertyAdditions.Add(new AvifRewritePropertyAddition(
                    itemId,
                    raw));
        }
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Appends a safe unknown top-level box after remuxed media.</summary>
    /// <param name="box">The complete unknown nonessential box to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddOpaqueTopLevelBox(AvifOpaqueBox box)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        AvifSecurityLimits limits = (options
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Limits;
        if (!AvifLowLevelAuthoring.IsSafeTopLevelBox(box, limits))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] raw = box.GetRawData();
        long total = raw.LongLength;
        int index = 0;
        while (index < (edits
            ?? throw new InvalidOperationException("Invalid internal state."))
            .TopLevelBoxAdditions.Count)
        {
            byte[] existing = edits.TopLevelBoxAdditions[index];
            if (CanonicalItemContainerWriter.RawPropertiesEqual(existing, raw))
            {
                code = AvifResultCode.InvalidArgument;
                return code;
            }
            total += existing.LongLength;
            index++;
        }
        if (total > limits.MaxSourceBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        edits.TopLevelBoxAdditions.Add(raw);
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds a copied metadata item describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The metadata payload to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddMetadata(uint itemId, AvifMetadata metadata)
    {
        return AddMetadataToItems(
            new uint[] { itemId },
            metadata);
    }

    /// <summary>Adds one copied metadata item describing multiple existing image items.</summary>
    /// <param name="itemIds">One or more distinct target image item identifiers.</param>
    /// <param name="metadata">The metadata payload and item information to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddMetadataToItems(
        uint[] itemIds,
        AvifMetadata metadata)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (itemIds == null || itemIds.Length == 0
            || metadata == null
            || metadata.GetCode() != AvifResultCode.Success
            || metadata.GetItemType() == "uri "
                && (metadata.GetUriType() ?? string.Empty).Length == 0)
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        int targetIndex = 0;
        while (targetIndex < itemIds.Length)
        {
            if (!IsVisualImageItem(((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state."))
                .FindItem(itemIds[targetIndex])))
            {
                code = AvifResultCode.InvalidArgument;
                return 0;
            }
            int duplicateIndex = 0;
            while (duplicateIndex < targetIndex)
            {
                if (itemIds[duplicateIndex] == itemIds[targetIndex])
                {
                    code = AvifResultCode.InvalidArgument;
                    return 0;
                }
                duplicateIndex++;
            }
            targetIndex++;
        }
        byte[] data = metadata.GetData();
        if (nextItemId == 0
            || ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount() + (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataAdditions.Count
                >= (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxItemCount
            || itemIds.Length > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxReferenceCount
                - ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount()
                - edits.MetadataAdditions.Count
                - edits.MetadataAssociations.Count
            || data.LongLength > GetMetadataLimit(metadata.GetKind())
            || GetRetainedMetadataBytes()
                > options.Limits.MaxMetadataBytes - data.LongLength
                    - GetAddedMetadataBytes())
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint itemIdentifier = nextItemId;
        nextItemId++;
        edits.MetadataAdditions.Add(new AvifRewriteMetadataAddition(
            itemIdentifier,
            itemIds,
            metadata));
        code = AvifResultCode.Success;
        return itemIdentifier;
    }

    /// <summary>Adds a content-description relationship from metadata to an image item.</summary>
    /// <param name="metadataItemId">An existing or staged metadata item identifier.</param>
    /// <param name="itemId">The target image item identifier.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddMetadataAssociation(
        uint metadataItemId,
        uint itemId)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!IsVisualImageItem(((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId)))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        int additionIndex = 0;
        while (additionIndex < (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataAdditions.Count)
        {
            AvifRewriteMetadataAddition addition =
                edits.MetadataAdditions[additionIndex];
            if (addition.ItemId == metadataItemId)
            {
                if (ContainsUInt(addition.TargetItemIds, itemId))
                {
                    code = AvifResultCode.InvalidArgument;
                    return code;
                }
                if (addition.TargetItemIds.Count
                    >= (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxReferenceCount)
                {
                    code = AvifResultCode.LimitExceeded;
                    return code;
                }
                addition.TargetItemIds.Add(itemId);
                code = AvifResultCode.Success;
                return code;
            }
            additionIndex++;
        }
        if (!HasContentDescription(metadataItemId)
            || HasMetadataAssociation(metadataItemId, itemId))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (edits.MetadataAssociations.Count
            >= (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxReferenceCount
                - ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount())
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        edits.MetadataAssociations.Add(
            new AvifRewriteMetadataAssociation(metadataItemId, itemId));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Appends one opaque metadata OBU without re-encoding a coded image item.</summary>
    /// <param name="itemId">The coded-image item identifier.</param>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddItemMetadataObu(
        uint itemId,
        AvifMetadataObu metadata)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        ParsedItem? item = ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId);
        CanonicalItemPayload? addition = FindAddedItem(itemId);
        if ((item == null || item.GetItemType() != "av01")
                && (addition == null || addition.ItemType != "av01")
            || metadata == null
            || metadata.GetCode() != AvifResultCode.Success)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] obu = metadata.GetEncodedObu();
        if (obu.Length == 0 || GetAddedMetadataBytes()
            > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxMetadataBytes - obu.LongLength)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataObuAdditions.Add(
            new AvifRewriteMetadataObuAddition(itemId, obu));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Removes metadata of one kind from an image while retaining shared associations.</summary>
    /// <param name="itemId">The described image item identifier.</param>
    /// <param name="kind">The metadata representation to remove.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode RemoveMetadata(
        uint itemId,
        AvifMetadataKind kind)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!IsVisualImageItem(((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId))
            || kind < AvifMetadataKind.Exif
            || kind > AvifMetadataKind.Custom)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        int additionIndex = (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataAdditions.Count - 1;
        while (additionIndex >= 0)
        {
            AvifRewriteMetadataAddition addition =
                edits.MetadataAdditions[additionIndex];
            if (ContainsUInt(addition.TargetItemIds, itemId)
                && addition.Kind == kind)
            {
                RemoveUInt(addition.TargetItemIds, itemId);
                if (addition.TargetItemIds.Count == 0)
                {
                    edits.MetadataAdditions.RemoveAt(additionIndex);
                }
            }
            additionIndex--;
        }
        int associationIndex = (edits ?? throw new InvalidOperationException("Invalid internal state."))
            .MetadataAssociations.Count - 1;
        while (associationIndex >= 0)
        {
            AvifRewriteMetadataAssociation association =
                edits.MetadataAssociations[associationIndex];
            ParsedItem? metadataItem = ((validation ?? throw new InvalidOperationException("Invalid internal state."))
                .Meta ?? throw new InvalidOperationException("Invalid internal state."))
                .FindItem(association.MetadataItemId);
            if (association.TargetItemId == itemId
                && metadataItem != null
                && AvifRemuxer.MetadataKindMatches(metadataItem, kind))
            {
                edits.MetadataAssociations.RemoveAt(associationIndex);
            }
            associationIndex--;
        }
        if (SourceHasMetadata(itemId, kind)
            && !HasMetadataRemoval(itemId, kind))
        {
            edits.MetadataRemovals.Add(new AvifRewriteMetadataRemoval(
                itemId,
                kind));
        }
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Replaces supported item transform properties without touching encoded pixels.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="transform">The transform collection to snapshot.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetTransform(
        uint itemId,
        AvifTransform transform)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!IsVisualImageItem(((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId))
            || transform == null
            || transform.GetExifOrientation() != 0)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        int index = 0;
        while (index < (edits ?? throw new InvalidOperationException("Invalid internal state.")).TransformEdits.Count)
        {
            if (edits.TransformEdits[index].ItemId == itemId)
            {
                edits.TransformEdits[index] = new AvifRewriteTransformEdit(
                    itemId,
                    transform);
                code = AvifResultCode.Success;
                return code;
            }
            index++;
        }
        edits.TransformEdits.Add(new AvifRewriteTransformEdit(
            itemId,
            transform));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds a compatible brand to canonical rewritten output.</summary>
    /// <param name="brand">The four-character brand.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode AddCompatibleBrand(string brand)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!IsValidBrand(brand))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        RemoveString((edits ?? throw new InvalidOperationException("Invalid internal state.")).RemovedBrands, brand);
        if (!SourceHasBrand(brand)
            && !ContainsString(edits.AddedBrands, brand))
        {
            edits.AddedBrands.Add(brand);
        }
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Removes a non-required compatible brand from rewritten output.</summary>
    /// <param name="brand">The four-character brand.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode RemoveCompatibleBrand(string brand)
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!IsValidBrand(brand))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (brand == "avif" || brand == "mif1" || brand == "miaf")
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        RemoveString((edits ?? throw new InvalidOperationException("Invalid internal state.")).AddedBrands, brand);
        if (SourceHasBrand(brand)
            && !ContainsString(edits.RemovedBrands, brand))
        {
            edits.RemovedBrands.Add(brand);
        }
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Requests canonical ordering, brands, structure, and payload placement.</summary>
    /// <returns>Success, InvalidState, or Closed.</returns>
    public AvifResultCode Normalize()
    {
        AvifResultCode state = CheckEditingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        normalize = true;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Finalizes and validates rewritten output.</summary>
    /// <returns>An independently owned encoded result.</returns>
    public AvifEncodeResult Finish()
    {
        if (closed)
        {
            return new AvifEncodeResult(
                AvifResultCode.Closed,
                System.Array.Empty<byte>());
        }
        if (!opened)
        {
            code = AvifResultCode.InvalidState;
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        if (finished)
        {
            code = AvifResultCode.Success;
            return new AvifEncodeResult(code, finishedData);
        }
        AvifRewriteOptions? rewriteOptions = CopyOptions(options);
        if (normalize)
        {
            (rewriteOptions ?? throw new InvalidOperationException("Invalid internal state.")).Canonicalize = true;
        }
        AvifRemuxResult rewritten = AvifRemuxer.Rewrite(
            source,
            rewriteOptions,
            edits);
        if (rewritten.OutputValidation != null)
        {
            diagnostics = rewritten.OutputValidation.Diagnostics;
        }
        else if (rewritten.InputValidation != null)
        {
            diagnostics = rewritten.InputValidation.Diagnostics;
        }
        code = rewritten.Code;
        if (code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        finishedData = (byte[])rewritten.Data.Clone();
        finished = true;
        return new AvifEncodeResult(code, finishedData);
    }

    /// <summary>Copies finalized rewritten bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the result.</param>
    /// <returns>Success, BufferTooSmall, InvalidState, or Closed.</returns>
    public AvifResultCode CopyResultTo(Span<byte> destination)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (!finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        code = CallerOutput.Copy(finishedData, destination);
        return code;
    }

    /// <summary>Returns the finalized rewrite byte count without copying it.</summary>
    /// <returns>The result length, or zero before successful finalization or after closure.</returns>
    public int GetResultLength()
    {
        return finished && !closed
            ? (finishedData ?? throw new InvalidOperationException("Invalid internal state.")).Length
            : 0;
    }

    /// <summary>Writes finalized bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidState, InvalidArgument, or Closed.</returns>
    public AvifResultCode Write(Stream destination)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (!finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        StreamOutput output = new StreamOutput(destination, true);
        code = output.Code;
        if (code == AvifResultCode.Success)
        {
            code = output.Write(finishedData);
        }
        if (code == AvifResultCode.Success)
        {
            code = output.Flush();
        }
        output.Close();
        return code;
    }

    /// <summary>Reports whether a source has been opened successfully.</summary>
    /// <returns><c>true</c> after a successful open and before closure.</returns>
    public bool IsOpen()
    {
        return opened && !closed;
    }

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns><c>true</c> when rewritten bytes are available.</returns>
    public bool IsFinished()
    {
        return finished && !closed;
    }

    /// <summary>Releases retained source, edits, and output bytes.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            source = System.Array.Empty<byte>();
            finishedData = System.Array.Empty<byte>();
            validation = null;
            edits = new AvifRewriteEdits();
            opened = false;
            finished = false;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    void Initialize(AvifRewriteOptions requestedOptions)
    {
        diagnostics = new DiagnosticLog();
        optionsValid = AvifRemuxer.ValidateOptions(requestedOptions);
        options = CopyOptions(requestedOptions);
        edits = new AvifRewriteEdits();
        source = System.Array.Empty<byte>();
        finishedData = System.Array.Empty<byte>();
        code = optionsValid
            ? AvifResultCode.Success : AvifResultCode.InvalidArgument;
    }

    AvifResultCode CheckUnopenedState()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (!optionsValid)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (opened || finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        return AvifResultCode.Success;
    }

    AvifResultCode CheckEditingState()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (!opened || finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        return AvifResultCode.Success;
    }

    bool HasMetadataRemoval(uint itemId, AvifMetadataKind kind)
    {
        int index = 0;
        while (index < (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataRemovals.Count)
        {
            AvifRewriteMetadataRemoval removal = edits.MetadataRemovals[index];
            if (removal.TargetItemId == itemId && removal.Kind == kind)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool SourceHasBrand(string brand)
    {
        return (validation ?? throw new InvalidOperationException("Invalid internal state.")).FileType != null
            && (validation.FileType ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand(brand);
    }

    bool SourceHasMetadata(uint itemId, AvifMetadataKind kind)
    {
        int index = 0;
        while (index < ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount())
        {
            ParsedItemReference? reference = (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReference(index);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "cdsc"
                && ContainsTarget(reference, itemId)
                && AvifRemuxer.MetadataKindMatches(
                    validation.Meta.FindItem(reference.GetFromItemId()),
                    kind))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    long GetRetainedMetadataBytes()
    {
        HashSet<uint> removed = AvifRemuxer.FindRemovedMetadataItems(
            (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta,
            edits);
        long total = 0;
        int itemIndex = 0;
        while (itemIndex < (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = validation.Meta.GetItem(itemIndex);
            if (!removed.Contains((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId())
                && HasContentDescription(item.GetItemId()))
            {
                EncodedItemExtractionResult extraction =
                    validation.Meta.ExtractEncodedItem(
                        source,
                        item.GetItemId());
                if (extraction.Code != AvifResultCode.Success
                    || extraction.Data == null
                    || total > long.MaxValue - extraction.Data.Length)
                {
                    if (extraction.Data != null)
                    {
                        extraction.Data.Close();
                    }
                    return long.MaxValue;
                }
                total += extraction.Data.Length;
                extraction.Data.Close();
            }
            itemIndex++;
        }
        return total;
    }

    bool HasContentDescription(uint itemId)
    {
        int index = 0;
        while (index < ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount())
        {
            ParsedItemReference? reference = (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReference(index);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "cdsc"
                && reference.GetFromItemId() == itemId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    long GetAddedMetadataBytes()
    {
        long total = 0;
        int index = 0;
        while (index < (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataAdditions.Count)
        {
            byte[] data = edits.MetadataAdditions[index].Data;
            if (total > long.MaxValue - data.LongLength)
            {
                return long.MaxValue;
            }
            total += data.LongLength;
            index++;
        }
        index = 0;
        while (index < edits.MetadataObuAdditions.Count)
        {
            byte[] data = edits.MetadataObuAdditions[index].Data;
            if (total > long.MaxValue - data.LongLength)
            {
                return long.MaxValue;
            }
            total += data.LongLength;
            index++;
        }
        return total;
    }

    bool HasMetadataAssociation(
        uint metadataItemId,
        uint itemId)
    {
        int index = 0;
        while (index < ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount())
        {
            ParsedItemReference? reference = ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state."))
                .GetReference(index);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "cdsc"
                && reference.GetFromItemId() == metadataItemId
                && ContainsTarget(reference, itemId))
            {
                return true;
            }
            index++;
        }
        index = 0;
        while (index < (edits ?? throw new InvalidOperationException("Invalid internal state.")).MetadataAssociations.Count)
        {
            AvifRewriteMetadataAssociation association =
                edits.MetadataAssociations[index];
            if (association.MetadataItemId == metadataItemId
                && association.TargetItemId == itemId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    long GetMetadataLimit(AvifMetadataKind kind)
    {
        if (kind == AvifMetadataKind.Exif)
        {
            return (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxExifBytes;
        }
        if (kind == AvifMetadataKind.Xmp)
        {
            return (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxXmpBytes;
        }
        return (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxMetadataBytes;
    }

    bool CanAddItem()
    {
        ParsedMeta meta = (validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state.");
        AvifRewriteEdits value = edits
            ?? throw new InvalidOperationException("Invalid internal state.");
        if (nextItemId == 0
            || meta.GetItemCount() + value.ItemAdditions.Count
                + value.MetadataAdditions.Count
                >= (options
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Limits.MaxItemCount)
        {
            code = AvifResultCode.LimitExceeded;
            return false;
        }
        return true;
    }

    bool HasItem(uint itemId)
    {
        return ((validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId) != null
            || FindAddedItem(itemId) != null;
    }

    CanonicalItemPayload? FindAddedItem(uint itemId)
    {
        int index = 0;
        while (index < (edits
            ?? throw new InvalidOperationException("Invalid internal state."))
            .ItemAdditions.Count)
        {
            CanonicalItemPayload item = edits.ItemAdditions[index];
            if (item.ItemId == itemId)
            {
                return item;
            }
            index++;
        }
        return null;
    }

    bool HasReference(string type, uint fromItemId)
    {
        ParsedMeta meta = (validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state.");
        int index = 0;
        while (index < meta.GetReferenceCount())
        {
            ParsedItemReference reference = meta.GetReference(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (reference.GetBoxType() == type
                && reference.GetFromItemId() == fromItemId)
            {
                return true;
            }
            index++;
        }
        index = 0;
        while (index < (edits
            ?? throw new InvalidOperationException("Invalid internal state."))
            .ReferenceAdditions.Count)
        {
            CanonicalItemReference reference =
                edits.ReferenceAdditions[index];
            if (reference.Type == type
                && reference.FromItemId == fromItemId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    int GetReferenceCountAfterEdits()
    {
        ParsedMeta meta = (validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state.");
        AvifRewriteEdits value = edits
            ?? throw new InvalidOperationException("Invalid internal state.");
        return meta.GetReferenceCount()
            + value.ReferenceAdditions.Count
            + value.MetadataAssociations.Count
            + value.MetadataAdditions.Count;
    }

    uint AllocateRewriteGroupId()
    {
        while (nextGroupId != 0
            && (HasItem(nextGroupId) || HasGroup(nextGroupId)))
        {
            nextGroupId--;
        }
        if (nextGroupId == 0)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint result = nextGroupId;
        nextGroupId--;
        return result;
    }

    bool HasGroup(uint groupId)
    {
        ParsedMeta meta = (validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state.");
        if (meta.FindEntityGroup(groupId) != null)
        {
            return true;
        }
        int index = 0;
        while (index < (edits
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GroupAdditions.Count)
        {
            if (edits.GroupAdditions[index].GroupId == groupId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool HasProperty(uint itemId, byte[] raw)
    {
        CanonicalItemPayload? added = FindAddedItem(itemId);
        int index = 0;
        if (added != null)
        {
            while (index < added.Properties.Count)
            {
                if (CanonicalItemContainerWriter.RawPropertiesEqual(
                        added.Properties[index].RawProperty,
                        raw))
                {
                    return true;
                }
                index++;
            }
        }
        ParsedItem? sourceItem = ((validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId);
        index = 0;
        while (sourceItem != null
            && index < sourceItem.GetPropertyCount())
        {
            ParsedPropertyAssociation association =
                sourceItem.GetPropertyAssociation(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (CanonicalItemContainerWriter.RawPropertiesEqual(
                    association.GetProperty().GetRawData(),
                    raw))
            {
                return true;
            }
            index++;
        }
        index = 0;
        while (index < (edits
            ?? throw new InvalidOperationException("Invalid internal state."))
            .PropertyAdditions.Count)
        {
            AvifRewritePropertyAddition property =
                edits.PropertyAdditions[index];
            if (property.ItemId == itemId
                && CanonicalItemContainerWriter.RawPropertiesEqual(
                    property.RawProperty,
                    raw))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    int GetPropertyCountAfterEdits()
    {
        ParsedMeta meta = (validation
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Meta ?? throw new InvalidOperationException("Invalid internal state.");
        AvifRewriteEdits value = edits
            ?? throw new InvalidOperationException("Invalid internal state.");
        int count = meta.GetPropertyCount()
            + value.PropertyAdditions.Count;
        int itemIndex = 0;
        while (itemIndex < value.ItemAdditions.Count)
        {
            count += value.ItemAdditions[itemIndex].Properties.Count;
            itemIndex++;
        }
        return count;
    }

    static uint FindNextItemId(ParsedMeta meta)
    {
        uint maximum = 0;
        int index = 0;
        while (index < meta.GetItemCount())
        {
            uint itemId = (meta.GetItem(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId();
            if (itemId > maximum)
            {
                maximum = itemId;
            }
            index++;
        }
        return maximum == uint.MaxValue ? 0 : maximum + 1;
    }

    static bool IsPrimaryImageItem(ParsedItem? item)
    {
        return IsVisualImageItem(item) && !(item ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden();
    }

    static bool IsPrimaryImageItemAddition(
        CanonicalItemPayload? item)
    {
        return item != null && !item.Hidden
            && item.ItemType == "av01";
    }

    static bool IsVisualImageItem(ParsedItem? item)
    {
        return item != null
            && (item.GetItemType() == "av01"
                || item.GetItemType() == "grid"
                || item.GetItemType() == "tmap"
                || item.GetItemType() == "sato");
    }

    static bool IsValidBrand(string brand)
    {
        return IsoBoxType.IsValid(brand) && brand != "uuid";
    }

    static bool ContainsString(List<string> values, string value)
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

    static bool ContainsUInt(List<uint> values, uint value)
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

    static void RemoveUInt(List<uint> values, uint value)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (values[index] == value)
            {
                values.RemoveAt(index);
            }
            index--;
        }
    }

    static bool ContainsTarget(
        ParsedItemReference reference,
        uint itemId)
    {
        int index = 0;
        while (index < reference.GetTargetCount())
        {
            if (reference.GetTargetItemId(index) == itemId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static void RemoveString(List<string> values, string value)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (values[index] == value)
            {
                values.RemoveAt(index);
            }
            index--;
        }
    }

    static AvifRewriteOptions? CopyOptions(AvifRewriteOptions? source)
    {
        if (source == null)
        {
            return null;
        }
        AvifRewriteOptions copy = new AvifRewriteOptions();
        copy.HeaderFormat = source.HeaderFormat;
        copy.PreserveUnknownBoxes = source.PreserveUnknownBoxes;
        copy.PreserveUnknownProperties = source.PreserveUnknownProperties;
        copy.PreserveUnknownMetadata = source.PreserveUnknownMetadata;
        copy.PreserveProtectedData = source.PreserveProtectedData;
        copy.Canonicalize = source.Canonicalize;
        copy.ValidateOutput = source.ValidateOutput;
        copy.Limits = ResourceGuard.CopyLimits(source.Limits);
        return copy;
    }
}
