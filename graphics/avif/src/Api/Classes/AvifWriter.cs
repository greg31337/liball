using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace avif;

/// <summary>Builds and finalizes an AVIF item container with explicit lifecycle control.</summary>
/// <remarks>
/// Images and metadata are copied when added. Finalization is deterministic and idempotent, and
/// destination streams remain caller-owned.
/// </remarks>
public class AvifWriter : IAvifWriter, IAvifDiagnosticSource
{
    List<CanonicalItemPayload>? items;
    List<CanonicalItemReference>? references;
    List<CanonicalEntityGroup>? groups;
    List<byte[]>? opaqueTopLevelBoxes;
    List<AvifWriterImageInfo>? imageInfos;
    Dictionary<uint, long>? hdrMetadataObuBytes;
    AvifEncoderOptions? options;
    DiagnosticLog? diagnostics;
    byte[]? finishedData;
    AvifResultCode code;
    uint nextItemId;
    uint nextGroupId;
    uint primaryItemId;
    AvifWriterTimedSequence? timedSequence;
    long metadataBytes;
    int imageCount;
    bool optionsValid;
    bool finished;
    bool closed;

    /// <summary>Creates an empty writer with default encoder and authoring options.</summary>
    /// <returns>The initialized writer.</returns>
    internal static AvifWriter Create()
    {
        return new AvifWriter(new AvifEncoderOptions());
    }

    /// <summary>Creates an empty writer with a snapshot of caller-selected options.</summary>
    /// <param name="options">The encoder options and resource limits to snapshot.</param>
    public AvifWriter(AvifEncoderOptions options)
    {
        Initialize(options);
    }

    /// <summary>Creates an empty writer with caller-selected options.</summary>
    /// <returns>The initialized writer.</returns>
    internal static AvifWriter Create(AvifEncoderOptions options)
    {
        return new AvifWriter(options);
    }

    /// <summary>Returns the result of the most recent writer operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns an independent copy of the active authoring controls.</summary>
    /// <returns>The active options, or null after closure.</returns>
    public AvifEncoderOptions? GetOptions()
    {
        return closed ? null : CopyOptions(options);
    }

    /// <summary>Returns diagnostics for the most recent AVIF authoring operation.</summary>
    /// <returns>The structured writer diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromLog(
            diagnostics,
            GetCode(),
            AvifDiagnosticCategory.Encoding,
            "The AVIF writer operation did not complete successfully.");
    }

    /// <summary>Encodes and adds a visible image item.</summary>
    /// <param name="image">The image to snapshot and encode.</param>
    /// <returns>The allocated nonzero item identifier, or zero on failure.</returns>
    public uint AddImage(AvifImage image)
    {
        return AddImageInternal(image, false);
    }

    /// <summary>Encodes and adds a hidden image item.</summary>
    /// <param name="image">The image to snapshot and encode.</param>
    /// <returns>The allocated nonzero item identifier, or zero on failure.</returns>
    public uint AddHiddenImage(AvifImage image)
    {
        return AddImageInternal(image, true);
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
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        AvifSecurityLimits limits = (options
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Limits ?? throw new InvalidOperationException("Invalid internal state.");
        if (imageCount >= limits.MaxFrameCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint itemId = PeekItemId();
        if (itemId == 0)
        {
            return 0;
        }
        AvifLowLevelItemResult authored =
            AvifLowLevelAuthoring.CreateAv1Item(
                itemId,
                payload,
                configuration,
                width,
                height,
                hidden,
                limits);
        code = authored.Code;
        if (code != AvifResultCode.Success || authored.Item == null)
        {
            return 0;
        }
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items
                ?? throw new InvalidOperationException("Invalid internal state."));
        prospective.Add(authored.Item);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        AllocateItemId();
        items.Add(authored.Item);
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state."))
            .Add(new AvifWriterImageInfo(
                itemId,
                authored.Width,
                authored.Height,
                authored.BitDepth));
        imageCount++;
        if (!hidden && primaryItemId == 0)
        {
            primaryItemId = itemId;
        }
        code = AvifResultCode.Success;
        return itemId;
    }

    /// <summary>Adds an arbitrary copied non-AV1 item to the collection.</summary>
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
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        AvifSecurityLimits limits = (options
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Limits ?? throw new InvalidOperationException("Invalid internal state.");
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
        if (payload.LongLength > limits.MaxSourceBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint itemId = AllocateItemId();
        if (itemId == 0)
        {
            return 0;
        }
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            itemId,
            itemType,
            name,
            hidden,
            payload);
        item.ContentType = contentType;
        item.ContentEncoding = contentEncoding;
        item.ItemUriType = uriType;
        (items ?? throw new InvalidOperationException("Invalid internal state."))
            .Add(item);
        code = AvifResultCode.Success;
        return itemId;
    }

    /// <summary>Adds an arbitrary directional relationship between staged items.</summary>
    /// <param name="type">The four-character reference type.</param>
    /// <param name="fromItemId">The source item identifier.</param>
    /// <param name="targetItemIds">One or more distinct target identifiers.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddItemReference(
        string type,
        uint fromItemId,
        uint[] targetItemIds)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (!AvifLowLevelAuthoring.IsValidReference(
                type,
                fromItemId,
                targetItemIds)
            || FindItem(fromItemId) == null
            || FindReference(type, fromItemId) != null)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        int index = 0;
        while (index < targetItemIds.Length)
        {
            if (FindItem(targetItemIds[index]) == null)
            {
                code = AvifResultCode.InvalidArgument;
                return code;
            }
            index++;
        }
        if ((references ?? throw new InvalidOperationException("Invalid internal state."))
                .Count >= ((options
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Limits
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        references.Add(new CanonicalItemReference(
            type,
            fromItemId,
            new List<uint>(targetItemIds)));
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds an arbitrary entity group containing staged items.</summary>
    /// <param name="type">The four-character grouping type.</param>
    /// <param name="entityIds">One or more distinct item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    public uint AddEntityGroup(string type, uint[] entityIds)
    {
        AvifResultCode state = CheckBuildingState();
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
            if (FindItem(entityIds[index]) == null)
            {
                code = AvifResultCode.InvalidArgument;
                return 0;
            }
            index++;
        }
        if ((groups ?? throw new InvalidOperationException("Invalid internal state."))
                .Count >= ((options
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Limits
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        return AddGroup(type, entityIds);
    }

    /// <summary>Adds a visible grid whose populated cells are encoded as independent hidden items.</summary>
    /// <param name="grid">The complete authoring grid to snapshot.</param>
    /// <returns>The allocated grid item identifier, or zero on failure.</returns>
    public uint AddGrid(AvifGrid grid)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        int cellCount = grid == null ? 0 : grid.GetCellCount();
        if (grid == null
            || grid.GetCode() != AvifResultCode.Success
            || !grid.IsComplete()
            || cellCount < 1)
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if (cellCount > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxTileCount
            || cellCount > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount - imageCount
            || cellCount + 1 > options.Limits.MaxItemCount - (items ?? throw new InvalidOperationException("Invalid internal state.")).Count
            || (references ?? throw new InvalidOperationException("Invalid internal state.")).Count >= options.Limits.MaxReferenceCount
            || nextItemId > uint.MaxValue - (uint)cellCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        List<CanonicalItemPayload> encodedCells =
            new List<CanonicalItemPayload>();
        List<uint> cellItemIds = new List<uint>();
        int cellIndex = 0;
        while (cellIndex < cellCount)
        {
            AvifGridCell? cell = grid.GetCell(cellIndex);
            AvifImage? image = cell == null ? null : cell.GetImage();
            uint cellItemId = nextItemId + (uint)cellIndex;
            CanonicalItemPayload? encoded = EncodeImage(
                image,
                cellItemId,
                true,
                ResolveQuality(
                    (cell ?? throw new InvalidOperationException("Invalid internal state.")).GetQuality(),
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).GridCellQuality),
                false);
            if (encoded == null)
            {
                return 0;
            }
            encodedCells.Add(encoded);
            cellItemIds.Add(cellItemId);
            cellIndex++;
        }

        uint gridItemId = nextItemId + (uint)cellCount;
        CanonicalItemPayload gridItem = CanonicalItemPayload.Create(
            gridItemId,
            "grid",
            string.Empty,
            false,
            BuildGridPayload(grid));
        gridItem.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildSpatialExtentsProperty(grid.GetWidth(), grid.GetHeight()),
            false));
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items);
        prospective.AddRange(encodedCells);
        prospective.Add(gridItem);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > options.Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        cellIndex = 0;
        while (cellIndex < cellCount)
        {
            CanonicalItemPayload encoded = encodedCells[cellIndex];
            AvifImage? image = (grid.GetCell(cellIndex) ?? throw new InvalidOperationException("Invalid internal state.")).GetImage();
            AllocateItemId();
            items.Add(encoded);
            (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
                encoded.ItemId,
                (image ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
                image.GetHeight(),
                image.GetBitDepth()));
            imageCount++;
            cellIndex++;
        }
        AllocateItemId();
        items.Add(gridItem);
        AvifImage? template = grid.GetTemplateImageValue();
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
            gridItemId,
            grid.GetWidth(),
            grid.GetHeight(),
            (template ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth()));
        references.Add(new CanonicalItemReference(
            "dimg",
            gridItemId,
            cellItemIds));
        if (primaryItemId == 0)
        {
            primaryItemId = gridItemId;
        }
        code = AvifResultCode.Success;
        return gridItemId;
    }

    /// <summary>Adds one visible AV1 item containing shared progressive layer data.</summary>
    /// <param name="layeredImage">The ordered layers and selection signaling to snapshot.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    public uint AddLayeredImage(AvifLayeredImage layeredImage)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        int layerCount = layeredImage == null
            ? 0 : layeredImage.GetLayerCount();
        if (layeredImage == null
            || layeredImage.GetCode() != AvifResultCode.Success
            || layerCount < 2
            || layerCount > 4)
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        int authoredItemCount = layeredImage != null
                && layeredImage.IsProgressive()
            ? 1 : layerCount;
        if (layerCount > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount - imageCount
            || authoredItemCount > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxItemCount - (items ?? throw new InvalidOperationException("Invalid internal state.")).Count
            || nextItemId > uint.MaxValue - (uint)(authoredItemCount - 1))
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint firstItemId = PeekItemId();
        if (firstItemId == 0)
        {
            return 0;
        }
        uint ownerItemId = firstItemId + (uint)(authoredItemCount - 1);

        List<CanonicalItemPayload> encodedLayers =
            new List<CanonicalItemPayload>();
        int layerIndex = 0;
        while (layerIndex < layerCount)
        {
            AvifLayer? layer = (layeredImage ?? throw new InvalidOperationException("Invalid internal state.")).GetLayer(layerIndex);
            CanonicalItemPayload? encoded = EncodeImage(
                layer == null ? null : layer.GetImage(),
                ownerItemId,
                false,
                ResolveQuality(
                    (layer ?? throw new InvalidOperationException("Invalid internal state.")).GetQuality(),
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).LayerQuality),
                false);
            if (encoded == null)
            {
                return 0;
            }
            encodedLayers.Add(encoded);
            layerIndex++;
        }
        AvifWriterLayeredPayload layeredPayload = BuildLayeredPayload(
            layeredImage,
            encodedLayers);
        if (layeredPayload.Code != AvifResultCode.Success)
        {
            code = layeredPayload.Code;
            return 0;
        }

        List<CanonicalItemPayload> authoredItems =
            new List<CanonicalItemPayload>();
        if ((layeredImage ?? throw new InvalidOperationException("Invalid internal state.")).IsProgressive())
        {
            authoredItems.Add(CreateLayeredItem(
                ownerItemId,
                false,
                0,
                layeredPayload,
                encodedLayers[encodedLayers.Count - 1].Properties,
                ushort.MaxValue,
                layeredImage.GetOperatingPointIndex()));
        }
        else
        {
            layerIndex = 0;
            while (layerIndex < layerCount)
            {
                uint selectedItemId = firstItemId + (uint)layerIndex;
                List<CanonicalItemPropertyAssociation> properties =
                    CreateSelectedLayerProperties(
                        encodedLayers[encodedLayers.Count - 1].Properties,
                        encodedLayers[layerIndex].Properties);
                authoredItems.Add(CreateLayeredItem(
                    selectedItemId,
                    layerIndex != layerCount - 1,
                    layerIndex == layerCount - 1 ? 0 : ownerItemId,
                    layeredPayload,
                    properties,
                    layerIndex,
                    layeredImage.GetOperatingPointIndex()));
                layerIndex++;
            }
        }
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items);
        prospective.AddRange(authoredItems);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > options.Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        int itemIndex = 0;
        while (itemIndex < authoredItems.Count)
        {
            AllocateItemId();
            CanonicalItemPayload item = authoredItems[itemIndex];
            items.Add(item);
            int imageIndex = layeredImage.IsProgressive()
                ? layerCount - 1 : itemIndex;
            AvifImage image = (layeredImage.GetLayer(imageIndex) ?? throw new InvalidOperationException("Invalid internal state.")).GetImage();
            (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
                item.ItemId,
                image.GetWidth(),
                image.GetHeight(),
                image.GetBitDepth()));
            itemIndex++;
        }
        imageCount += layerCount;
        if (primaryItemId == 0)
        {
            primaryItemId = ownerItemId;
        }
        code = AvifResultCode.Success;
        return ownerItemId;
    }

    /// <summary>Adds a tone-map derived image and its hidden gain-map dependency.</summary>
    /// <param name="itemId">The visible base image and backward-compatible fallback.</param>
    /// <param name="gainMap">The gain-map image, metadata, and alternate color information.</param>
    /// <returns>The allocated <c>tmap</c> item identifier, or zero on failure.</returns>
    public uint AddGainMap(uint itemId, AvifGainMap gainMap)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        CanonicalItemPayload? baseItem = FindImageItem(itemId);
        AvifWriterImageInfo? baseInfo = FindImageInfo(itemId);
        AvifImage? gainImage = gainMap == null ? null : gainMap.GetImage();
        byte[] metadata = gainMap == null || gainMap.GetMetadata() == null
            ? System.Array.Empty<byte>()
            : (gainMap.GetMetadata() ?? throw new InvalidOperationException("Invalid internal state.")).GetData();
        if (baseItem == null
            || baseItem.Hidden
            || baseInfo == null
            || gainMap == null
            || gainMap.GetCode() != AvifResultCode.Success
            || gainImage == null
            || metadata.Length == 0
            || FindAlternateGroup(itemId) != null)
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if ((items ?? throw new InvalidOperationException("Invalid internal state.")).Count > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxItemCount - 2
            || imageCount >= (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount
            || (references ?? throw new InvalidOperationException("Invalid internal state.")).Count >= options.Limits.MaxReferenceCount
            || nextItemId > uint.MaxValue - 1
            || metadata.LongLength > options.Limits.MaxMetadataBytes
                - metadataBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        uint gainItemId = nextItemId;
        uint toneMapItemId = nextItemId + 1;
        CanonicalItemPayload? encodedGain = EncodeImage(
            gainImage,
            gainItemId,
            true,
            ResolveQuality(
                gainMap.GetQuality(),
                (options ?? throw new InvalidOperationException("Invalid internal state.")).GainMapQuality),
            false);
        if (encodedGain == null)
        {
            return 0;
        }
        CanonicalItemPayload toneMapItem = CanonicalItemPayload.Create(
            toneMapItemId,
            "tmap",
            string.Empty,
            false,
            metadata);
        toneMapItem.Properties = CopyDerivedProperties(
            baseItem,
            baseInfo.BitDepth,
            gainMap.GetAlternateColorInfo());
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items);
        prospective.Add(encodedGain);
        prospective.Add(toneMapItem);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > options.Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint groupId = AllocateGroupId();
        if (groupId == 0)
        {
            return 0;
        }

        AllocateItemId();
        AllocateItemId();
        items.Add(encodedGain);
        items.Add(toneMapItem);
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
            gainItemId,
            gainImage.GetWidth(),
            gainImage.GetHeight(),
            gainImage.GetBitDepth()));
        imageInfos.Add(new AvifWriterImageInfo(
            toneMapItemId,
            baseInfo.Width,
            baseInfo.Height,
            baseInfo.BitDepth));
        imageCount++;
        references.Add(new CanonicalItemReference(
            "dimg",
            toneMapItemId,
            new List<uint> { itemId, gainItemId }));
        (groups ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalEntityGroup(
            "altr",
            groupId,
            new List<uint> { toneMapItemId, itemId }));
        metadataBytes += metadata.LongLength;
        code = AvifResultCode.Success;
        return toneMapItemId;
    }

    /// <summary>Adds a sample-transform derived image with encoded inputs and a visible fallback.</summary>
    /// <param name="transform">The expression, ordered inputs, and output depth to snapshot.</param>
    /// <returns>The allocated <c>sato</c> item identifier, or zero on failure.</returns>
    public uint AddSampleTransform(AvifSampleTransform transform)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        int inputCount = transform == null ? 0 : transform.GetInputCount();
        if (transform == null
            || transform.GetCode() != AvifResultCode.Success
            || transform.GetOutputBitDepth() == 0
            || !ValidateSampleTransformInputs(transform))
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if (inputCount > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount - imageCount
            || inputCount + 1 > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxItemCount - (items ?? throw new InvalidOperationException("Invalid internal state.")).Count
            || (references ?? throw new InvalidOperationException("Invalid internal state.")).Count >= options.Limits.MaxReferenceCount
            || nextItemId > uint.MaxValue - (uint)inputCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        List<CanonicalItemPayload> encodedInputs =
            new List<CanonicalItemPayload>();
        List<uint> inputItemIds = new List<uint>();
        int inputIndex = 0;
        while (inputIndex < inputCount)
        {
            uint inputItemId = nextItemId + (uint)inputIndex;
            AvifImage? input = transform.GetInputImage(inputIndex);
            CanonicalItemPayload? encoded = EncodeImage(
                input,
                inputItemId,
                inputIndex != 0,
                (options ?? throw new InvalidOperationException("Invalid internal state.")).Quality,
                false);
            if (encoded == null)
            {
                return 0;
            }
            encodedInputs.Add(encoded);
            inputItemIds.Add(inputItemId);
            inputIndex++;
        }
        uint sampleTransformItemId = nextItemId + (uint)inputCount;
        CanonicalItemPayload sampleTransformItem = CanonicalItemPayload.Create(
            sampleTransformItemId,
            "sato",
            string.Empty,
            false,
            transform.GetData());
        sampleTransformItem.Properties = CopyDerivedProperties(
            encodedInputs[0],
            transform.GetOutputBitDepth(),
            null);
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items);
        prospective.AddRange(encodedInputs);
        prospective.Add(sampleTransformItem);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > options.Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint groupId = AllocateGroupId();
        if (groupId == 0)
        {
            return 0;
        }

        inputIndex = 0;
        while (inputIndex < inputCount)
        {
            AllocateItemId();
            CanonicalItemPayload inputItem = encodedInputs[inputIndex];
            AvifImage? input = transform.GetInputImage(inputIndex);
            items.Add(inputItem);
            (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
                inputItem.ItemId,
                (input ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
                input.GetHeight(),
                input.GetBitDepth()));
            inputIndex++;
        }
        AllocateItemId();
        items.Add(sampleTransformItem);
        AvifImage? fallback = transform.GetInputImage(0);
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
            sampleTransformItemId,
            (fallback ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
            fallback.GetHeight(),
            transform.GetOutputBitDepth()));
        imageCount += inputCount;
        references.Add(new CanonicalItemReference(
            "dimg",
            sampleTransformItemId,
            inputItemIds));
        (groups ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalEntityGroup(
            "altr",
            groupId,
            new List<uint> { sampleTransformItemId, inputItemIds[0] }));
        if (primaryItemId == 0)
        {
            primaryItemId = inputItemIds[0];
        }
        code = AvifResultCode.Success;
        return sampleTransformItemId;
    }

    /// <summary>Adds one timed AV1 image sequence and a still-image fallback.</summary>
    /// <param name="sequence">The frames, timing, repetition, alpha, and sample signaling.</param>
    /// <returns>The color track identifier, or zero on failure.</returns>
    public uint AddSequence(AvifSequence sequence)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        int frameCount = sequence == null ? 0 : sequence.GetFrameCount();
        if (sequence == null || sequence.GetTimescale() == 0
            || frameCount == 0 || timedSequence != null
            || !ValidateSequenceFrames(sequence)
            || !ValidateSequenceAuthoringControls(sequence))
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        bool hasAlpha = (sequence.GetFrame(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAlphaImage() != null;
        int fallbackItemCount = hasAlpha ? 2 : 1;
        if (hasAlpha && frameCount > int.MaxValue / 2)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        int encodedFrameCount = hasAlpha ? frameCount * 2 : frameCount;
        int requiredTrackCount = hasAlpha ? 2 : 1;
        int requiredReferenceCount = hasAlpha ? 2 : 0;
        if (frameCount > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSampleCount
            || encodedFrameCount > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount - imageCount
            || fallbackItemCount > options.Limits.MaxItemCount - (items ?? throw new InvalidOperationException("Invalid internal state.")).Count
            || requiredTrackCount > options.Limits.MaxTrackCount
            || requiredReferenceCount
                > options.Limits.MaxReferenceCount - (references ?? throw new InvalidOperationException("Invalid internal state.")).Count
            || nextItemId > uint.MaxValue - (uint)(fallbackItemCount - 1))
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        AvifWriterTimedTrack colorTrack = new AvifWriterTimedTrack();
        AvifWriterTimedTrack? alphaTrack = hasAlpha
            ? new AvifWriterTimedTrack() : null;
        CanonicalItemPayload? fallbackColor = null;
        CanonicalItemPayload? fallbackAlpha = null;
        int frameIndex = 0;
        while (frameIndex < frameCount)
        {
            AvifFrame? frame = sequence.GetFrame(frameIndex);
            CanonicalItemPayload? encodedColor = EncodeImage(
                (frame ?? throw new InvalidOperationException("Invalid internal state.")).GetImage(),
                nextItemId,
                false,
                ResolveQuality(
                    frame.GetColorQuality(),
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).Quality),
                true);
            if (encodedColor == null)
            {
                return 0;
            }
            if (frameIndex == 0)
            {
                fallbackColor = CopyTimedFallback(encodedColor);
            }
            if (!AppendFrameMetadataObus(encodedColor, frame))
            {
                return 0;
            }
            if (frameIndex == 0)
            {
                InitializeTimedTrack(colorTrack, encodedColor, frame.GetImage(), false);
            }
            colorTrack.Samples.Add(new AvifWriterTimedSample(
                encodedColor.Data,
                frame));
            if (hasAlpha)
            {
                CanonicalItemPayload? encodedAlpha = EncodeImage(
                    frame.GetAlphaImage(),
                    nextItemId + 1,
                    true,
                    ResolveQuality(
                        frame.GetAlphaQuality(),
                        (options ?? throw new InvalidOperationException("Invalid internal state.")).AlphaQuality),
                    true);
                if (encodedAlpha == null)
                {
                    return 0;
                }
                if (frameIndex == 0)
                {
                    fallbackAlpha = encodedAlpha;
                    InitializeTimedTrack(
                        alphaTrack,
                        encodedAlpha,
                        frame.GetAlphaImage(),
                        true);
                }
                (alphaTrack ?? throw new InvalidOperationException("Invalid internal state.")).Samples.Add(new AvifWriterTimedSample(
                    encodedAlpha.Data,
                    frame));
            }
            frameIndex++;
        }
        if (colorTrack.Av1Configuration.Length == 0
            || hasAlpha && (alphaTrack ?? throw new InvalidOperationException("Invalid internal state.")).Av1Configuration.Length == 0)
        {
            code = AvifResultCode.EncodeFailed;
            return 0;
        }

        if (hasAlpha)
        {
            byte[] type = Encoding.UTF8.GetBytes(
                "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha");
            byte[] payload = new byte[type.Length + 1];
            type.CopyTo(payload, 0);
            (fallbackAlpha ?? throw new InvalidOperationException("Invalid internal state.")).Properties.Add(new CanonicalItemPropertyAssociation(
                BuildFullProperty("auxC", payload),
                true));
        }
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items);
        prospective.Add(fallbackColor
            ?? throw new InvalidOperationException("Invalid internal state."));
        if (fallbackAlpha != null)
        {
            prospective.Add(fallbackAlpha);
        }
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > options.Limits.MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        uint fallbackColorId = AllocateItemId();
        items.Add(fallbackColor);
        AvifImage? firstImage = (sequence.GetFrame(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetImage();
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
            fallbackColorId,
            (firstImage ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
            firstImage.GetHeight(),
            firstImage.GetBitDepth()));
        if (fallbackAlpha != null)
        {
            uint fallbackAlphaId = AllocateItemId();
            items.Add(fallbackAlpha);
            AvifImage? firstAlpha = (sequence.GetFrame(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAlphaImage();
            imageInfos.Add(new AvifWriterImageInfo(
                fallbackAlphaId,
                (firstAlpha ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(),
                firstAlpha.GetHeight(),
                firstAlpha.GetBitDepth()));
            references.Add(new CanonicalItemReference(
                "auxl",
                fallbackAlphaId,
                new List<uint> { fallbackColorId }));
        }
        imageCount += encodedFrameCount;
        if (primaryItemId == 0)
        {
            primaryItemId = fallbackColorId;
        }
        timedSequence = new AvifWriterTimedSequence();
        timedSequence.Timescale = sequence.GetTimescale();
        timedSequence.InfiniteRepetition = sequence.IsRepetitionInfinite();
        timedSequence.RepetitionCount = sequence.GetRepetitionCount();
        timedSequence.ColorTrack = colorTrack;
        timedSequence.AlphaTrack = alphaTrack;
        code = AvifResultCode.Success;
        return 1;
    }

    /// <summary>Adds a hidden thumbnail linked to an existing image item.</summary>
    /// <param name="itemId">The master image item identifier.</param>
    /// <param name="image">The thumbnail image to snapshot and encode.</param>
    /// <returns>The allocated thumbnail item identifier, or zero on failure.</returns>
    public uint AddThumbnail(uint itemId, AvifImage image)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (FindImageInfo(itemId) == null)
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if ((references ?? throw new InvalidOperationException("Invalid internal state.")).Count >= ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint thumbnailId = AddImageInternal(image, true);
        if (thumbnailId != 0)
        {
            references.Add(new CanonicalItemReference(
                "thmb",
                thumbnailId,
                new List<uint> { itemId }));
        }
        return thumbnailId;
    }

    /// <summary>Adds a hidden alpha, depth, or caller-defined auxiliary image.</summary>
    /// <param name="itemId">The master image item identifier.</param>
    /// <param name="auxiliary">The auxiliary image and relationship to snapshot.</param>
    /// <returns>The allocated auxiliary item identifier, or zero on failure.</returns>
    public uint AddAuxiliary(uint itemId, AvifAuxiliaryImage auxiliary)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        AvifWriterImageInfo? master = FindImageInfo(itemId);
        AvifImage? image = auxiliary == null ? null : auxiliary.GetSourceImage();
        if (master == null
            || auxiliary == null
            || auxiliary.GetCode() != AvifResultCode.Success
            || image == null
            || (auxiliary.GetKind() == AvifAuxiliaryKind.Alpha
                && image.GetBitDepth() != master.BitDepth))
        {
            code = AvifResultCode.InvalidArgument;
            return 0;
        }
        if (imageCount >= ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        CanonicalItemReference? premultiplied = auxiliary.IsPremultiplied()
            ? FindReference("prem", itemId) : null;
        int newReferenceCount = premultiplied == null
            ? (auxiliary.IsPremultiplied() ? 2 : 1) : 1;
        if ((references ?? throw new InvalidOperationException("Invalid internal state.")).Count > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxReferenceCount
                - newReferenceCount
            || (premultiplied != null
                && (premultiplied.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count == ushort.MaxValue))
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }

        byte[] typeBytes = Encoding.UTF8.GetBytes(
            auxiliary.GetTypeName() ?? string.Empty);
        byte[] typePayload = new byte[typeBytes.Length + 1];
        typeBytes.CopyTo(typePayload, 0);
        byte[] property = BuildFullProperty("auxC", typePayload);
        uint auxiliaryId = PeekItemId();
        if (auxiliaryId == 0)
        {
            return 0;
        }
        int defaultQuality = auxiliary.GetKind() == AvifAuxiliaryKind.Alpha
            ? (options ?? throw new InvalidOperationException("Invalid internal state.")).AlphaQuality
            : options.AuxiliaryQuality;
        CanonicalItemPayload? item = EncodeImage(
            image,
            auxiliaryId,
            true,
            ResolveQuality(auxiliary.GetQuality(), defaultQuality),
            false);
        if (item == null)
        {
            return 0;
        }
        item.Properties.Add(new CanonicalItemPropertyAssociation(property, true));
        if (!StageEncodedImage(item, image))
        {
            return 0;
        }

        references.Add(new CanonicalItemReference(
            "auxl",
            auxiliaryId,
            new List<uint> { itemId }));
        if (auxiliary.IsPremultiplied())
        {
            if (premultiplied == null)
            {
                references.Add(new CanonicalItemReference(
                    "prem",
                    itemId,
                    new List<uint> { auxiliaryId }));
            }
            else
            {
                (premultiplied.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Add(auxiliaryId);
            }
        }
        code = AvifResultCode.Success;
        return auxiliaryId;
    }

    /// <summary>Adds an alternate-image entity group in preference order.</summary>
    /// <param name="itemIds">Two or more distinct visible image item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    public uint AddAlternateGroup(uint[] itemIds)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (!ValidateGroupItems(itemIds, false))
        {
            return 0;
        }
        int groupIndex = 0;
        while (groupIndex < (groups ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            CanonicalEntityGroup group = groups[groupIndex];
            if (group.Type == "altr" && ContainsAny(group.EntityIds, itemIds))
            {
                code = AvifResultCode.InvalidArgument;
                return 0;
            }
            groupIndex++;
        }
        return AddGroup("altr", itemIds);
    }

    /// <summary>Adds an ordered left/right stereo entity group.</summary>
    /// <param name="leftItemId">The visible left-view image item identifier.</param>
    /// <param name="rightItemId">The visible right-view image item identifier.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    public uint AddStereoPair(uint leftItemId, uint rightItemId)
    {
        uint[] itemIds = new uint[] { leftItemId, rightItemId };
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (!ValidateGroupItems(itemIds, true))
        {
            return 0;
        }
        return AddGroup("ster", itemIds);
    }

    /// <summary>Adds a metadata item describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The metadata payload to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddMetadata(uint itemId, AvifMetadata metadata)
    {
        return AddMetadataToItems(
            new uint[] { itemId },
            metadata);
    }

    /// <summary>Adds one metadata item describing multiple existing image items.</summary>
    /// <param name="itemIds">One or more distinct target image item identifiers.</param>
    /// <param name="metadata">The metadata payload and item information to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddMetadataToItems(
        uint[] itemIds,
        AvifMetadata metadata)
    {
        AvifResultCode state = CheckBuildingState();
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
            if (!IsVisualImageItem(FindItem(itemIds[targetIndex])))
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
        if (data.LongLength > GetMetadataLimit(metadata.GetKind())
            || metadataBytes > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes - data.LongLength
            || itemIds.Length > (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxReferenceCount
                - (references ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint metadataItemId = AllocateItemId();
        if (metadataItemId == 0)
        {
            return 0;
        }

        CanonicalItemPayload item = CanonicalItemPayload.Create(
            metadataItemId,
            metadata.GetItemType(),
            GetMetadataName(metadata.GetKind()),
            true,
            data);
        item.ContentType = metadata.GetContentType();
        item.ContentEncoding = metadata.GetContentEncoding() ?? string.Empty;
        item.ItemUriType = metadata.GetUriType() ?? string.Empty;
        (items ?? throw new InvalidOperationException("Invalid internal state.")).Add(item);
        references.Add(new CanonicalItemReference(
            "cdsc",
            metadataItemId,
            new List<uint>(itemIds)));
        metadataBytes += data.LongLength;
        code = AvifResultCode.Success;
        return metadataItemId;
    }

    /// <summary>Adds a content-description relationship from existing metadata to an image item.</summary>
    /// <param name="metadataItemId">The staged metadata item identifier.</param>
    /// <param name="itemId">The target image item identifier.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddMetadataAssociation(
        uint metadataItemId,
        uint itemId)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemReference? reference = FindReference(
            "cdsc",
            metadataItemId);
        if (reference == null || !IsVisualImageItem(FindItem(itemId))
            || Contains(reference.ToItemIds, itemId))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if ((reference.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count
            >= ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxReferenceCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        (reference.ToItemIds ?? throw new InvalidOperationException("Invalid internal state."))
            .Add(itemId);
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Appends one opaque metadata OBU to a staged coded image item.</summary>
    /// <param name="itemId">The coded-image item identifier.</param>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddItemMetadataObu(
        uint itemId,
        AvifMetadataObu metadata)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemPayload? item = FindItem(itemId);
        if (item == null || item.ItemType != "av01"
            || metadata == null
            || metadata.GetCode() != AvifResultCode.Success)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] obu = metadata.GetEncodedObu();
        if (obu.Length == 0
            || obu.LongLength > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes - metadataBytes
            || item.Data.Length > int.MaxValue - obu.Length)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        byte[] combined = new byte[item.Data.Length + obu.Length];
        item.Data.CopyTo(combined, 0);
        obu.CopyTo(combined, item.Data.Length);
        item.Data = combined;
        item.Length = combined.Length;
        metadataBytes += obu.LongLength;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds validated Exif metadata describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="exif">The Exif metadata to snapshot.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddExif(uint itemId, AvifExif exif)
    {
        return AddMetadata(itemId, AvifMetadata.FromExif(exif));
    }

    /// <summary>Adds a validated XMP packet describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="xmp">The XMP metadata to snapshot.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    public uint AddXmp(uint itemId, AvifXmp xmp)
    {
        return AddMetadata(itemId, AvifMetadata.FromXmp(xmp));
    }

    /// <summary>Replaces the clean-aperture, rotation, mirror, and pixel-aspect properties.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="transform">The transform collection to snapshot.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetTransform(uint itemId, AvifTransform transform)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemPayload? item = FindImageItem(itemId);
        if (item == null || transform == null
            || transform.GetExifOrientation() != 0)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        List<CanonicalItemPropertyAssociation> properties =
            CopyPropertiesExcept(item, new string[]
            {
                "clap", "irot", "imir", "pasp"
            });
        AddTransformProperties(properties, transform);
        return ReplaceProperties(item, properties);
    }

    /// <summary>Replaces the supported static HDR and viewing-environment properties.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The HDR metadata to snapshot.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetHdrMetadata(
        uint itemId,
        AvifHdrMetadata metadata)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemPayload? item = FindImageItem(itemId);
        if (item == null || metadata == null)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        if (metadata.HasItuT35Payload() && item.ItemType != "av01")
        {
            code = AvifResultCode.UnsupportedFeature;
            return code;
        }
        List<byte[]> metadataObus = item.ItemType == "av01"
            ? BuildHdrMetadataObus(metadata) : new List<byte[]>();
        long previousMetadataBytes = 0;
        if (item.ItemType == "av01")
        {
            (hdrMetadataObuBytes ?? throw new InvalidOperationException("Invalid internal state."))
                .TryGetValue(item.ItemId, out previousMetadataBytes);
        }
        byte[] baseData = item.ItemType == "av01"
            ? RemoveHdrMetadataObus(item.Data) : item.Data;
        long retainedMetadataBytes = metadataBytes - previousMetadataBytes;
        long addedBytes = 0;
        int obuIndex = 0;
        while (obuIndex < metadataObus.Count)
        {
            addedBytes += metadataObus[obuIndex].LongLength;
            obuIndex++;
        }
        if (addedBytes > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes - retainedMetadataBytes
            || baseData.LongLength > int.MaxValue - addedBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        List<CanonicalItemPropertyAssociation> properties =
            CopyPropertiesExcept(item, new string[]
            {
                "clli", "mdcv", "cclv", "amve", "reve", "ndwt",
                "cmin", "cmex"
            });
        AddHdrProperties(properties, metadata);
        AvifResultCode replaceCode = ReplaceProperties(item, properties);
        if (replaceCode != AvifResultCode.Success)
        {
            return replaceCode;
        }
        if (metadataObus.Count != 0)
        {
            byte[] combined = new byte[baseData.Length + (int)addedBytes];
            baseData.CopyTo(combined, 0);
            int cursor = baseData.Length;
            obuIndex = 0;
            while (obuIndex < metadataObus.Count)
            {
                metadataObus[obuIndex].CopyTo(combined, cursor);
                cursor += metadataObus[obuIndex].Length;
                obuIndex++;
            }
            item.Data = combined;
            item.Length = combined.LongLength;
        }
        else if (item.ItemType == "av01")
        {
            item.Data = baseData;
            item.Length = baseData.LongLength;
        }
        if (item.ItemType == "av01")
        {
            (hdrMetadataObuBytes ?? throw new InvalidOperationException("Invalid internal state."))[item.ItemId] = addedBytes;
            metadataBytes = retainedMetadataBytes + addedBytes;
        }
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Adds one validated unknown nonessential property to an image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="property">The opaque property to snapshot.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode AddOpaqueProperty(
        uint itemId,
        AvifOpaqueBox property)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemPayload? item = FindItem(itemId);
        if (item == null || property == null
            || property.GetCode() != AvifResultCode.Success
            || property.IsEssential())
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] raw = property.GetRawData();
        int index = 0;
        while (index < item.Properties.Count)
        {
            if (CanonicalItemContainerWriter.RawPropertiesEqual(
                item.Properties[index].RawProperty,
                raw))
            {
                code = AvifResultCode.InvalidArgument;
                return code;
            }
            index++;
        }
        List<CanonicalItemPropertyAssociation> properties =
            new List<CanonicalItemPropertyAssociation>(item.Properties);
        properties.Add(new CanonicalItemPropertyAssociation(raw, false));
        return ReplaceProperties(item, properties);
    }

    /// <summary>Appends a safe unknown top-level box after authored media.</summary>
    /// <param name="box">The complete unknown nonessential box to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    public AvifResultCode AddOpaqueTopLevelBox(AvifOpaqueBox box)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        AvifSecurityLimits limits = (options
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Limits ?? throw new InvalidOperationException("Invalid internal state.");
        if (!AvifLowLevelAuthoring.IsSafeTopLevelBox(box, limits))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        byte[] raw = box.GetRawData();
        long total = raw.LongLength;
        int index = 0;
        while (index < (opaqueTopLevelBoxes
            ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            byte[] existing = opaqueTopLevelBoxes[index];
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
        opaqueTopLevelBoxes.Add(raw);
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Selects an existing visible image as the primary item.</summary>
    /// <param name="itemId">The image item identifier.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    public AvifResultCode SetPrimaryItem(uint itemId)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CanonicalItemPayload? item = FindItem(itemId);
        if (!IsPrimaryImageItem(item))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        primaryItemId = itemId;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Finalizes and validates the container.</summary>
    /// <returns>An independently owned encoded result; repeated calls return the same bytes.</returns>
    public AvifEncodeResult Finish()
    {
        if (closed)
        {
            return new AvifEncodeResult(
                AvifResultCode.Closed,
                System.Array.Empty<byte>());
        }
        if (!optionsValid)
        {
            code = AvifResultCode.InvalidArgument;
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        if (finished)
        {
            code = AvifResultCode.Success;
            return new AvifEncodeResult(code, finishedData);
        }
        CanonicalItemPayload? primary = FindItem(primaryItemId);
        if (!IsPrimaryImageItem(primary))
        {
            code = AvifResultCode.InvalidState;
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }

        CanonicalItemContainerOptions containerOptions =
            new CanonicalItemContainerOptions();
        containerOptions.HeaderFormat = timedSequence == null
            ? (options ?? throw new InvalidOperationException("Invalid internal state.")).HeaderFormat : AvifHeaderFormat.Full;
        containerOptions.MaximumOutputBytes = ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes;
        List<string> compatibleBrands = timedSequence == null
            ? new List<string> { "mif1", "miaf" }
            : new List<string> { "mif1", "miaf", "avis", "msf1" };
        if (timedSequence != null && AllSequenceSamplesAreSync(timedSequence))
        {
            compatibleBrands.Add("avio");
        }
        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            compatibleBrands,
            primaryItemId,
            items,
            references,
            groups,
            containerOptions);
        request.TimedSequence = timedSequence != null;
        CanonicalItemContainerWriteResult result =
            CanonicalItemContainerWriter.Write(request);
        diagnostics = result.SelfValidation == null
            ? null : result.SelfValidation.Diagnostics;
        code = result.Code;
        if (code != AvifResultCode.Success)
        {
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        byte[] encodedData = result.Data;
        if (timedSequence != null)
        {
            CanonicalTimedMovieWriteResult movie =
                CanonicalTimedMovieWriter.Append(
                    encodedData,
                    timedSequence,
                    (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes);
            code = movie.Code;
            if (code != AvifResultCode.Success)
            {
                return new AvifEncodeResult(code, System.Array.Empty<byte>());
            }
            encodedData = movie.Data;
            if (options.ValidateOutput)
            {
                AvifValidationOptions validationOptions =
                    new AvifValidationOptions();
                validationOptions.ValidateEncodedPayload = true;
                validationOptions.Limits = options.Limits;
                AvifContainerValidationResult validationResult =
                    AvifContainerValidator.Validate(
                        encodedData,
                        validationOptions);
                diagnostics = validationResult.Diagnostics;
                code = validationResult.Code;
                if (code != AvifResultCode.Success)
                {
                    return new AvifEncodeResult(
                        code,
                        System.Array.Empty<byte>());
                }
            }
        }
        byte[]? appended = AvifLowLevelAuthoring.AppendTopLevelBoxes(
            encodedData,
            opaqueTopLevelBoxes
                ?? throw new InvalidOperationException("Invalid internal state."),
            (options.Limits
                ?? throw new InvalidOperationException("Invalid internal state."))
                .MaxSourceBytes);
        if (appended == null)
        {
            code = AvifResultCode.LimitExceeded;
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        encodedData = appended;
        if (opaqueTopLevelBoxes.Count != 0 && options.ValidateOutput)
        {
            AvifValidationOptions validationOptions =
                new AvifValidationOptions();
            validationOptions.ValidateEncodedPayload = true;
            validationOptions.AllowUnknownNonEssentialData = true;
            validationOptions.Limits = options.Limits;
            AvifContainerValidationResult validationResult =
                AvifContainerValidator.Validate(
                    encodedData,
                    validationOptions);
            diagnostics = validationResult.Diagnostics;
            code = validationResult.Code;
            if (code != AvifResultCode.Success)
            {
                return new AvifEncodeResult(
                    code,
                    System.Array.Empty<byte>());
            }
        }
        finishedData = (byte[])encodedData.Clone();
        finished = true;
        return new AvifEncodeResult(code, finishedData);
    }

    /// <summary>Writes finalized bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidState before finalization, InvalidArgument, or Closed.</returns>
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

    /// <summary>Returns the number of staged image and metadata items.</summary>
    /// <returns>The staged count, or zero after closure.</returns>
    public int GetItemCount()
    {
        return closed ? 0 : (items ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns the selected primary item identifier.</summary>
    /// <returns>The nonzero identifier, or zero when none is selected.</returns>
    public uint GetPrimaryItemId()
    {
        return closed ? 0 : primaryItemId;
    }

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns><c>true</c> when bytes are available for writing.</returns>
    public bool IsFinished()
    {
        return !closed && finished;
    }

    /// <summary>Releases all staged and finalized data.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!closed)
        {
            (items ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            (references ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            (groups ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            (opaqueTopLevelBoxes
                ?? throw new InvalidOperationException("Invalid internal state."))
                .Clear();
            (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            (hdrMetadataObuBytes ?? throw new InvalidOperationException("Invalid internal state.")).Clear();
            finishedData = System.Array.Empty<byte>();
            primaryItemId = 0;
            metadataBytes = 0;
            imageCount = 0;
            timedSequence = null;
            closed = true;
            code = AvifResultCode.Closed;
        }
        return AvifResultCode.Success;
    }

    void Initialize(AvifEncoderOptions requestedOptions)
    {
        items = new List<CanonicalItemPayload>();
        references = new List<CanonicalItemReference>();
        groups = new List<CanonicalEntityGroup>();
        opaqueTopLevelBoxes = new List<byte[]>();
        imageInfos = new List<AvifWriterImageInfo>();
        hdrMetadataObuBytes = new Dictionary<uint, long>();
        diagnostics = new DiagnosticLog();
        finishedData = System.Array.Empty<byte>();
        nextItemId = 1;
        nextGroupId = uint.MaxValue;
        primaryItemId = 0;
        timedSequence = null;
        metadataBytes = 0;
        imageCount = 0;
        finished = false;
        closed = false;
        code = ValidateOptions(requestedOptions);
        options = CopyOptions(requestedOptions);
        optionsValid = code == AvifResultCode.Success;
    }

    uint AddImageInternal(AvifImage image, bool hidden)
    {
        AvifResultCode state = CheckBuildingState();
        if (state != AvifResultCode.Success)
        {
            return 0;
        }
        if (imageCount >= ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        uint itemId = PeekItemId();
        if (itemId == 0)
        {
            return 0;
        }
        CanonicalItemPayload? item = EncodeImage(
            image,
            itemId,
            hidden,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Quality,
            false);
        if (item == null)
        {
            return 0;
        }
        if (!StageEncodedImage(item, image))
        {
            return 0;
        }
        if (!hidden && primaryItemId == 0)
        {
            primaryItemId = itemId;
        }
        code = AvifResultCode.Success;
        return itemId;
    }

    bool StageEncodedImage(CanonicalItemPayload item, AvifImage image)
    {
        List<CanonicalItemPayload> prospective =
            new List<CanonicalItemPayload>(items
                ?? throw new InvalidOperationException("Invalid internal state."));
        prospective.Add(item);
        if (CanonicalItemContainerWriter.InternProperties(prospective).Count
            > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return false;
        }
        AllocateItemId();
        items.Add(item);
        (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Add(new AvifWriterImageInfo(
            item.ItemId,
            image.GetWidth(),
            image.GetHeight(),
            image.GetBitDepth()));
        imageCount++;
        code = AvifResultCode.Success;
        return true;
    }

    CanonicalItemPayload? EncodeImage(
        AvifImage? image,
        uint itemId,
        bool hidden,
        int quality,
        bool sequenceFrame)
    {
        AvifEncoderOptions? payloadOptions = CopyOptions(options);
        (payloadOptions ?? throw new InvalidOperationException("Invalid internal state.")).HeaderFormat = AvifHeaderFormat.Full;
        payloadOptions.Quality = quality;
        if (sequenceFrame
            && payloadOptions.SequencePolicy
                == AvifSequenceEncodingPolicy.Automatic)
        {
            payloadOptions.SequencePolicy = AvifSequenceEncodingPolicy.InterFrame;
        }
        AvifEncodeResult encoded = Avif.Encode(image, payloadOptions);
        code = encoded.GetCode();
        if (code != AvifResultCode.Success)
        {
            return null;
        }
        AvifReader reader = AvifReader.Create(encoded.GetData());
        code = reader.Parse();
        if (code != AvifResultCode.Success)
        {
            reader.Close();
            return null;
        }
        AvifFileInfo? fileInfo = (reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();
        AvifImageItem? source = fileInfo == null
            ? null
            : (reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(fileInfo.GetPrimaryItemId());
        if (source == null)
        {
            code = AvifResultCode.EncodeFailed;
            reader.Close();
            return null;
        }
        byte[] payload = reader.GetEncodedItem(source.GetItemId());
        if (payload.Length == 0)
        {
            code = reader.GetCode();
            if (code == AvifResultCode.Success)
            {
                code = AvifResultCode.EncodeFailed;
            }
            reader.Close();
            return null;
        }
        CanonicalItemPayload result = CanonicalItemPayload.Create(
            itemId,
            "av01",
            string.Empty,
            hidden,
            payload);
        int propertyIndex = 0;
        while (propertyIndex < source.GetPropertyCount())
        {
            AvifItemProperty? property = source.GetProperty(propertyIndex);
            result.Properties.Add(new CanonicalItemPropertyAssociation(
                (property ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData(),
                property.IsEssential()));
            propertyIndex++;
        }
        reader.Close();
        code = AvifResultCode.Success;
        return result;
    }

    static int ResolveQuality(int requested, int fallback)
    {
        return requested < 0 ? fallback : requested;
    }

    bool AppendFrameMetadataObus(
        CanonicalItemPayload item,
        AvifFrame frame)
    {
        int index = 0;
        while (index < frame.GetMetadataObuCount())
        {
            AvifMetadataObu? metadata = frame.GetMetadataObu(index);
            byte[] obu = (metadata ?? throw new InvalidOperationException("Invalid internal state."))
                .GetEncodedObu();
            if (obu.Length == 0
                || obu.LongLength > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes - metadataBytes
                || item.Data.Length > int.MaxValue - obu.Length)
            {
                code = AvifResultCode.LimitExceeded;
                return false;
            }
            byte[] combined = new byte[item.Data.Length + obu.Length];
            item.Data.CopyTo(combined, 0);
            obu.CopyTo(combined, item.Data.Length);
            item.Data = combined;
            item.Length = combined.Length;
            metadataBytes += obu.LongLength;
            index++;
        }
        return true;
    }

    static CanonicalItemPayload CopyTimedFallback(
        CanonicalItemPayload source)
    {
        CanonicalItemPayload result = CanonicalItemPayload.Create(
            source.ItemId,
            source.ItemType,
            source.Name,
            source.Hidden,
            source.Data);
        result.ContentType = source.ContentType;
        result.ContentEncoding = source.ContentEncoding;
        result.ItemUriType = source.ItemUriType;
        int index = 0;
        while (index < source.Properties.Count)
        {
            CanonicalItemPropertyAssociation property =
                source.Properties[index];
            result.Properties.Add(new CanonicalItemPropertyAssociation(
                property.RawProperty,
                property.Essential));
            index++;
        }
        return result;
    }

    AvifResultCode CheckBuildingState()
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
        if (finished)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        return AvifResultCode.Success;
    }

    uint PeekItemId()
    {
        if ((items ?? throw new InvalidOperationException("Invalid internal state.")).Count >= ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxItemCount || nextItemId == 0)
        {
            code = AvifResultCode.LimitExceeded;
            return 0;
        }
        return nextItemId;
    }

    uint AllocateItemId()
    {
        uint result = PeekItemId();
        if (result != 0)
        {
            nextItemId++;
        }
        return result;
    }

    CanonicalItemPayload? FindItem(uint itemId)
    {
        int index = 0;
        while (index < (items ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (items[index].ItemId == itemId)
            {
                return items[index];
            }
            index++;
        }
        return null;
    }

    CanonicalItemPayload? FindImageItem(uint itemId)
    {
        CanonicalItemPayload? item = FindItem(itemId);
        return IsVisualImageItem(item) ? item : null;
    }

    static bool IsPrimaryImageItem(CanonicalItemPayload? item)
    {
        return item != null
            && !item.Hidden
            && IsVisualImageItem(item);
    }

    static bool IsVisualImageItem(CanonicalItemPayload? item)
    {
        return item != null
            && (item.ItemType == "av01"
                || item.ItemType == "grid"
                || item.ItemType == "tmap"
                || item.ItemType == "sato");
    }

    AvifWriterImageInfo? FindImageInfo(uint itemId)
    {
        int index = 0;
        while (index < (imageInfos ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (imageInfos[index].ItemId == itemId)
            {
                return imageInfos[index];
            }
            index++;
        }
        return null;
    }

    CanonicalItemReference? FindReference(string type, uint fromItemId)
    {
        int index = 0;
        while (index < (references ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            CanonicalItemReference reference = references[index];
            if (reference.Type == type && reference.FromItemId == fromItemId)
            {
                return reference;
            }
            index++;
        }
        return null;
    }

    CanonicalEntityGroup? FindAlternateGroup(uint itemId)
    {
        int groupIndex = 0;
        while (groupIndex < (groups ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            CanonicalEntityGroup group = groups[groupIndex];
            if (group.Type == "altr"
                && Contains(group.EntityIds, itemId))
            {
                return group;
            }
            groupIndex++;
        }
        return null;
    }

    static bool ValidateSampleTransformInputs(
        AvifSampleTransform transform)
    {
        AvifImage? first = transform.GetInputImage(0);
        if (first == null
            || first.GetAlphaMode() != AvifAlphaMode.None
            || first.GetBitDepth() >= transform.GetIntermediateBitDepth())
        {
            return false;
        }
        int index = 1;
        while (index < transform.GetInputCount())
        {
            AvifImage? input = transform.GetInputImage(index);
            if (input == null
                || input.GetAlphaMode() != AvifAlphaMode.None
                || input.GetBitDepth() >= transform.GetIntermediateBitDepth()
                || input.GetWidth() != first.GetWidth()
                || input.GetHeight() != first.GetHeight()
                || input.GetChromaFormat() != first.GetChromaFormat()
                || input.GetChromaSamplePosition()
                    != first.GetChromaSamplePosition()
                || !ColorInformationEquals(
                    input.GetColorInfo(),
                    first.GetColorInfo())
                || !IccProfilesEqual(
                    input.GetIccProfile(),
                    first.GetIccProfile()))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static bool ValidateSequenceFrames(AvifSequence sequence)
    {
        ulong mediaDuration = 0;
        int frameIndex = 0;
        while (frameIndex < sequence.GetFrameCount())
        {
            AvifFrame? frame = sequence.GetFrame(frameIndex);
            if (frame == null
                || frame.GetCode() != AvifResultCode.Success
                || (frame.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetCode() != AvifResultCode.Success
                || frame.GetAlphaImage() != null
                    && (frame.GetAlphaImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetCode()
                        != AvifResultCode.Success
                || mediaDuration > ulong.MaxValue - frame.GetDuration())
            {
                return false;
            }
            mediaDuration += frame.GetDuration();
            int distance = frame.GetForwardKeyFrameDistance();
            if (distance >= 0)
            {
                int recoveryIndex = frameIndex + distance + 1;
                if (recoveryIndex >= sequence.GetFrameCount()
                    || !(sequence.GetFrame(recoveryIndex) ?? throw new InvalidOperationException("Invalid internal state.")).IsKeyFrame())
                {
                    return false;
                }
            }
            frameIndex++;
        }
        if (!sequence.IsRepetitionInfinite())
        {
            ulong presentations = sequence.GetRepetitionCount() + 1;
            if (presentations == 0
                || mediaDuration > ulong.MaxValue / presentations)
            {
                return false;
            }
        }
        return true;
    }

    bool ValidateSequenceAuthoringControls(AvifSequence sequence)
    {
        AvifEncoderOptions value = options
            ?? throw new InvalidOperationException("Invalid internal state.");
        if (value.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
            && sequence.GetFrameCount() > 1)
        {
            return false;
        }
        int spatialLayerCount = value.SpatialLayers.Length == 0
            ? 1 : value.SpatialLayers.Length;
        int index = 0;
        while (index < sequence.GetFrameCount())
        {
            AvifFrame frame = sequence.GetFrame(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (frame.GetTemporalId() >= value.TemporalLayerCount
                || frame.GetSpatialId() >= spatialLayerCount
                || frame.GetShowExistingFrameSlot() >= 0
                    && !value.EnableShowExistingFrames
                || frame.GetFilmGrain() != null
                    && !value.FilmGrainParametersPresent
                || frame.IsKeyFrame() && frame.GetRefreshFrameFlags() >= 0
                    && frame.GetRefreshFrameFlags() != 255
                || frame.GetShowExistingFrameSlot() >= 0
                    && frame.GetRefreshFrameFlags() != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    void InitializeTimedTrack(
        AvifWriterTimedTrack? track,
        CanonicalItemPayload encoded,
        AvifImage? image,
        bool alpha)
    {
        (track ?? throw new InvalidOperationException("Invalid internal state.")).Width = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth();
        track.Height = image.GetHeight();
        track.Alpha = alpha;
        int propertyIndex = 0;
        while (propertyIndex < encoded.Properties.Count)
        {
            byte[] raw = encoded.Properties[propertyIndex].RawProperty;
            CanonicalPropertyProbe probe =
                CanonicalItemContainerWriter.ProbeProperty(raw);
            if (probe.Code == AvifResultCode.Success
                && probe.Type == "av1C")
            {
                track.Av1Configuration = BuildTimedAv1Configuration(
                    raw,
                    encoded.Data,
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
            }
            else if (probe.Code == AvifResultCode.Success
                && probe.Type == "colr")
            {
                track.ColorProperties.Add((byte[])raw.Clone());
            }
            propertyIndex++;
        }
    }

    static byte[] BuildTimedAv1Configuration(
        byte[] configuration,
        byte[] encoded,
        AvifSecurityLimits? limits)
    {
        if (configuration.Length != 12 || limits == null)
        {
            return (byte[])configuration.Clone();
        }
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            encoded,
            limits,
            0);
        if (parsed.Code != AvifResultCode.Success)
        {
            return (byte[])configuration.Clone();
        }
        int index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            if (obu.Header.Type == Av1ObuType.SequenceHeader
                && obu.TotalSize <= int.MaxValue - configuration.Length)
            {
                byte[] result = new byte[configuration.Length + obu.TotalSize];
                configuration.CopyTo(result, 0);
                encoded.AsSpan(obu.Offset, obu.TotalSize).CopyTo(
                    result.AsSpan(configuration.Length));
                BinaryPrimitives.WriteUInt32BigEndian(
                    result.AsSpan(0, 4),
                    (uint)result.Length);
                return result;
            }
            index++;
        }
        return (byte[])configuration.Clone();
    }

    static bool AllSequenceSamplesAreSync(
        AvifWriterTimedSequence sequence)
    {
        int index = 0;
        while (index < (sequence.ColorTrack ?? throw new InvalidOperationException("Invalid internal state.")).Samples.Count)
        {
            if (!sequence.ColorTrack.Samples[index].KeyFrame)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static bool ColorInformationEquals(
        AvifColorInfo? first,
        AvifColorInfo? second)
    {
        return first != null
            && second != null
            && first.ColorPrimaries == second.ColorPrimaries
            && first.TransferCharacteristics == second.TransferCharacteristics
            && first.MatrixCoefficients == second.MatrixCoefficients
            && first.Range == second.Range;
    }

    static bool IccProfilesEqual(
        AvifIccProfile? first,
        AvifIccProfile? second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }
        byte[] firstData = first.GetData();
        byte[] secondData = second.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool ValidateGroupItems(uint[] itemIds, bool exactlyTwo)
    {
        if (itemIds == null
            || (exactlyTwo && itemIds.Length != 2)
            || (!exactlyTwo && itemIds.Length < 2))
        {
            code = AvifResultCode.InvalidArgument;
            return false;
        }
        int index = 0;
        while (index < itemIds.Length)
        {
            CanonicalItemPayload? item = FindImageItem(itemIds[index]);
            if (item == null || item.Hidden)
            {
                code = AvifResultCode.InvalidArgument;
                return false;
            }
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (itemIds[previousIndex] == itemIds[index])
                {
                    code = AvifResultCode.InvalidArgument;
                    return false;
                }
                previousIndex++;
            }
            index++;
        }
        return true;
    }

    uint AddGroup(string type, uint[] itemIds)
    {
        uint groupId = AllocateGroupId();
        if (groupId == 0)
        {
            return 0;
        }
        (groups ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalEntityGroup(
            type,
            groupId,
            new List<uint>(itemIds)));
        code = AvifResultCode.Success;
        return groupId;
    }

    uint AllocateGroupId()
    {
        while (nextGroupId != 0
            && (FindItem(nextGroupId) != null || FindGroup(nextGroupId) != null))
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

    CanonicalEntityGroup? FindGroup(uint groupId)
    {
        int index = 0;
        while (index < (groups ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (groups[index].GroupId == groupId)
            {
                return groups[index];
            }
            index++;
        }
        return null;
    }

    static bool ContainsAny(List<uint>? values, uint[] candidates)
    {
        int valueIndex = 0;
        while (valueIndex < (values ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                if (values[valueIndex] == candidates[candidateIndex])
                {
                    return true;
                }
                candidateIndex++;
            }
            valueIndex++;
        }
        return false;
    }

    static bool Contains(List<uint>? values, uint candidate)
    {
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (values[index] == candidate)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    AvifResultCode ReplaceProperties(
        CanonicalItemPayload item,
        List<CanonicalItemPropertyAssociation> properties)
    {
        if (properties.Count > byte.MaxValue)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        List<byte[]> unique = new List<byte[]>();
        int itemIndex = 0;
        while (itemIndex < (items ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            List<CanonicalItemPropertyAssociation> selected =
                items[itemIndex] == item
                    ? properties : items[itemIndex].Properties;
            int propertyIndex = 0;
            while (propertyIndex < selected.Count)
            {
                byte[] raw = selected[propertyIndex].RawProperty;
                if (CanonicalItemContainerWriter.FindPropertyIndex(unique, raw) == 0)
                {
                    unique.Add(raw);
                }
                propertyIndex++;
            }
            itemIndex++;
        }
        if (unique.Count > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxPropertyCount)
        {
            code = AvifResultCode.LimitExceeded;
            return code;
        }
        item.Properties = properties;
        code = AvifResultCode.Success;
        return code;
    }

    static List<CanonicalItemPropertyAssociation> CopyPropertiesExcept(
        CanonicalItemPayload item,
        string[] excludedTypes)
    {
        List<CanonicalItemPropertyAssociation> properties =
            new List<CanonicalItemPropertyAssociation>();
        int index = 0;
        while (index < item.Properties.Count)
        {
            CanonicalItemPropertyAssociation association = item.Properties[index];
            CanonicalPropertyProbe probe = CanonicalItemContainerWriter.ProbeProperty(
                association.RawProperty);
            if (!ContainsString(excludedTypes, probe.Type))
            {
                properties.Add(association);
            }
            index++;
        }
        return properties;
    }

    static List<CanonicalItemPropertyAssociation> CopyDerivedProperties(
        CanonicalItemPayload source,
        int outputBitDepth,
        AvifColorInfo? alternateColor)
    {
        string[] excluded = alternateColor == null
            ? new string[] { "av1C", "pixi" }
            : new string[] { "av1C", "pixi", "colr" };
        List<CanonicalItemPropertyAssociation> properties =
            CopyPropertiesExcept(source, excluded);
        int channelCount = FindPixelChannelCount(source);
        byte[] pixels = new byte[1 + channelCount];
        pixels[0] = (byte)channelCount;
        int channel = 0;
        while (channel < channelCount)
        {
            pixels[channel + 1] = (byte)outputBitDepth;
            channel++;
        }
        properties.Add(new CanonicalItemPropertyAssociation(
            BuildFullProperty("pixi", pixels),
            false));
        if (alternateColor != null)
        {
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildColourProperty(alternateColor),
                false));
        }
        return properties;
    }

    static int FindPixelChannelCount(CanonicalItemPayload item)
    {
        int index = 0;
        while (index < item.Properties.Count)
        {
            CanonicalItemPropertyAssociation association = item.Properties[index];
            CanonicalPropertyProbe probe = CanonicalItemContainerWriter.ProbeProperty(
                association.RawProperty);
            if (probe.Code == AvifResultCode.Success
                && probe.Type == "pixi"
                && association.RawProperty.Length >= 13)
            {
                int count = association.RawProperty[12];
                if (count > 0 && association.RawProperty.Length >= 13 + count)
                {
                    return count;
                }
            }
            index++;
        }
        return 1;
    }

    static byte[] BuildColourProperty(AvifColorInfo color)
    {
        byte[] payload = new byte[11];
        payload[0] = (byte)'n';
        payload[1] = (byte)'c';
        payload[2] = (byte)'l';
        payload[3] = (byte)'x';
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(4, 2),
            (ushort)color.ColorPrimaries);
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(6, 2),
            (ushort)color.TransferCharacteristics);
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(8, 2),
            (ushort)color.MatrixCoefficients);
        payload[10] = color.Range == AvifRange.Full ? (byte)128 : (byte)0;
        return BuildProperty("colr", payload);
    }

    static bool ContainsString(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static void AddTransformProperties(
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

    static void AddHdrProperties(
        List<CanonicalItemPropertyAssociation> properties,
        AvifHdrMetadata metadata)
    {
        if (metadata.HasContentLightLevel())
        {
            AvifContentLightLevel? level = metadata.GetContentLightLevel();
            byte[] payload = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(0, 2),
                (ushort)(level ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumContentLightLevel());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(2, 2),
                (ushort)level.GetMaximumFrameAverageLightLevel());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("clli", payload),
                false));
        }
        if (metadata.HasMasteringDisplay())
        {
            AvifMasteringDisplay? display = metadata.GetMasteringDisplay();
            byte[] payload = new byte[24];
            int index = 0;
            while (index < 3)
            {
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(index * 4, 2),
                    (ushort)(display ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryChromaticityX(index));
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(index * 4 + 2, 2),
                    (ushort)display.GetPrimaryChromaticityY(index));
                index++;
            }
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(12, 2),
                (ushort)(display ?? throw new InvalidOperationException("Invalid internal state.")).GetWhitePointChromaticityX());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(14, 2),
                (ushort)display.GetWhitePointChromaticityY());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(16, 4), display.GetMaximumLuminance());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(20, 4), display.GetMinimumLuminance());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("mdcv", payload),
                false));
        }
        if (metadata.HasContentColorVolume())
        {
            AddContentColorVolume(
                properties,
                metadata.GetContentColorVolume());
        }
        if (metadata.HasAmbientViewingEnvironment())
        {
            AvifAmbientViewingEnvironment? environment =
                metadata.GetAmbientViewingEnvironment();
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(0, 4), (environment ?? throw new InvalidOperationException("Invalid internal state.")).GetAmbientIlluminance());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(4, 2), (ushort)environment.GetAmbientLightX());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(6, 2), (ushort)environment.GetAmbientLightY());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildProperty("amve", payload),
                false));
        }
        if (metadata.HasReferenceViewingEnvironment())
        {
            AvifReferenceViewingEnvironment? environment =
                metadata.GetReferenceViewingEnvironment();
            byte[] payload = new byte[16];
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(0, 4), (environment ?? throw new InvalidOperationException("Invalid internal state.")).GetSurroundLuminance());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(4, 2), (ushort)environment.GetSurroundLightX());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(6, 2), (ushort)environment.GetSurroundLightY());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(8, 4), environment.GetPeripheryLuminance());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(12, 2), (ushort)environment.GetPeripheryLightX());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(14, 2), (ushort)environment.GetPeripheryLightY());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildFullProperty("reve", payload),
                false));
        }
        if (metadata.HasNominalDiffuseWhite())
        {
            byte[] payload = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(
                payload, metadata.GetNominalDiffuseWhiteLuminance());
            properties.Add(new CanonicalItemPropertyAssociation(
                BuildFullProperty("ndwt", payload),
                false));
        }
        if (metadata.HasCameraIntrinsicMatrix())
        {
            AddCameraIntrinsicProperty(
                properties,
                metadata.GetCameraIntrinsicMatrix());
        }
        if (metadata.HasCameraExtrinsicMatrix())
        {
            AddCameraExtrinsicProperty(
                properties,
                metadata.GetCameraExtrinsicMatrix());
        }
    }

    static void AddCameraIntrinsicProperty(
        List<CanonicalItemPropertyAssociation> properties,
        AvifCameraIntrinsicMatrix? matrix)
    {
        AvifCameraIntrinsicMatrix value = matrix
            ?? throw new InvalidOperationException("Invalid internal state.");
        byte[] payload = new byte[value.HasFullIntrinsics() ? 20 : 12];
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(0, 4), value.GetFocalLengthX());
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(4, 4), value.GetPrincipalPointX());
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(8, 4), value.GetPrincipalPointY());
        if (value.HasFullIntrinsics())
        {
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(12, 4), value.GetFocalLengthY());
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(16, 4), value.GetSkewFactor());
        }
        int flags = (value.HasFullIntrinsics() ? 1 : 0)
            | value.GetDenominatorShift() << 8
            | value.GetSkewDenominatorShift() << 16;
        properties.Add(new CanonicalItemPropertyAssociation(
            BuildFlaggedFullProperty("cmin", flags, payload),
            false));
    }

    static void AddCameraExtrinsicProperty(
        List<CanonicalItemPropertyAssociation> properties,
        AvifCameraExtrinsicMatrix? matrix)
    {
        AvifCameraExtrinsicMatrix value = matrix
            ?? throw new InvalidOperationException("Invalid internal state.");
        int length = (value.HasPositionX() ? 4 : 0)
            + (value.HasPositionY() ? 4 : 0)
            + (value.HasPositionZ() ? 4 : 0)
            + (value.HasOrientation()
                ? value.UsesLargeOrientationFields() ? 12 : 6 : 0)
            + (value.HasWorldCoordinateSystemId() ? 4 : 0);
        byte[] payload = new byte[length];
        int cursor = 0;
        if (value.HasPositionX())
        {
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(cursor, 4), value.GetPositionX());
            cursor += 4;
        }
        if (value.HasPositionY())
        {
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(cursor, 4), value.GetPositionY());
            cursor += 4;
        }
        if (value.HasPositionZ())
        {
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(cursor, 4), value.GetPositionZ());
            cursor += 4;
        }
        if (value.HasOrientation())
        {
            if (value.UsesLargeOrientationFields())
            {
                BinaryPrimitives.WriteInt32BigEndian(
                    payload.AsSpan(cursor, 4), value.GetQuaternionXRaw());
                BinaryPrimitives.WriteInt32BigEndian(
                    payload.AsSpan(cursor + 4, 4), value.GetQuaternionYRaw());
                BinaryPrimitives.WriteInt32BigEndian(
                    payload.AsSpan(cursor + 8, 4), value.GetQuaternionZRaw());
                cursor += 12;
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(
                    payload.AsSpan(cursor, 2), (short)value.GetQuaternionXRaw());
                BinaryPrimitives.WriteInt16BigEndian(
                    payload.AsSpan(cursor + 2, 2), (short)value.GetQuaternionYRaw());
                BinaryPrimitives.WriteInt16BigEndian(
                    payload.AsSpan(cursor + 4, 2), (short)value.GetQuaternionZRaw());
                cursor += 6;
            }
        }
        if (value.HasWorldCoordinateSystemId())
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(cursor, 4), value.GetWorldCoordinateSystemId());
        }
        int flags = (value.HasPositionX() ? 1 : 0)
            | (value.HasPositionY() ? 2 : 0)
            | (value.HasPositionZ() ? 4 : 0)
            | (value.HasOrientation() ? 8 : 0)
            | (value.UsesLargeOrientationFields() ? 16 : 0)
            | (value.HasWorldCoordinateSystemId() ? 32 : 0);
        properties.Add(new CanonicalItemPropertyAssociation(
            BuildFlaggedFullProperty("cmex", flags, payload),
            false));
    }

    static List<byte[]> BuildHdrMetadataObus(AvifHdrMetadata metadata)
    {
        List<byte[]> result = new List<byte[]>();
        if (metadata.HasContentLightLevel())
        {
            AvifContentLightLevel value = metadata.GetContentLightLevel()
                ?? throw new InvalidOperationException("Invalid internal state.");
            ParsedAv1Metadata parsed = new ParsedAv1Metadata(
                (ulong)Av1MetadataType.HdrContentLightLevel);
            parsed.ContentLightLevel = new Av1ContentLightLevelMetadata();
            parsed.ContentLightLevel.MaximumContentLightLevel =
                value.GetMaximumContentLightLevel();
            parsed.ContentLightLevel.MaximumFrameAverageLightLevel =
                value.GetMaximumFrameAverageLightLevel();
            AddHdrMetadataObu(result, parsed);
        }
        if (metadata.HasMasteringDisplay())
        {
            AvifMasteringDisplay value = metadata.GetMasteringDisplay()
                ?? throw new InvalidOperationException("Invalid internal state.");
            ParsedAv1Metadata parsed = new ParsedAv1Metadata(
                (ulong)Av1MetadataType.HdrMasteringDisplayColorVolume);
            parsed.MasteringDisplayColorVolume =
                new Av1MasteringDisplayColorVolumeMetadata();
            int index = 0;
            while (index < 3)
            {
                parsed.MasteringDisplayColorVolume.PrimaryChromaticityX[index] =
                    value.GetPrimaryChromaticityX(index);
                parsed.MasteringDisplayColorVolume.PrimaryChromaticityY[index] =
                    value.GetPrimaryChromaticityY(index);
                index++;
            }
            parsed.MasteringDisplayColorVolume.WhitePointChromaticityX =
                value.GetWhitePointChromaticityX();
            parsed.MasteringDisplayColorVolume.WhitePointChromaticityY =
                value.GetWhitePointChromaticityY();
            parsed.MasteringDisplayColorVolume.MaximumLuminance =
                value.GetMaximumLuminance();
            parsed.MasteringDisplayColorVolume.MinimumLuminance =
                value.GetMinimumLuminance();
            AddHdrMetadataObu(result, parsed);
        }
        if (metadata.HasItuT35Payload())
        {
            ParsedAv1Metadata parsed = new ParsedAv1Metadata(
                (ulong)Av1MetadataType.ItuTT35);
            parsed.ItuT35 = new Av1ItuT35Metadata();
            parsed.ItuT35.CountryCode = metadata.GetItuT35CountryCode();
            parsed.ItuT35.CountryCodeExtension =
                metadata.GetItuT35CountryCodeExtension();
            parsed.ItuT35.Payload = metadata.GetItuT35Payload();
            AddHdrMetadataObu(result, parsed);
        }
        return result;
    }

    byte[] RemoveHdrMetadataObus(byte[] source)
    {
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            source,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits,
            0);
        if (parsed.Code != AvifResultCode.Success)
        {
            return (byte[])source.Clone();
        }
        bool[] retained = new bool[parsed.Obus.Count];
        int retainedLength = 0;
        int index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            bool keep = true;
            if (obu.Header.Type == Av1ObuType.Metadata)
            {
                Av1MetadataParseResult metadata = Av1MetadataParser.ParseObu(obu);
                if (metadata.Code == AvifResultCode.Success
                    && metadata.Metadata != null
                    && (metadata.Metadata.MetadataType
                            == (ulong)Av1MetadataType.HdrContentLightLevel
                        || metadata.Metadata.MetadataType
                            == (ulong)Av1MetadataType.HdrMasteringDisplayColorVolume
                        || metadata.Metadata.MetadataType
                            == (ulong)Av1MetadataType.ItuTT35))
                {
                    keep = false;
                }
            }
            retained[index] = keep;
            if (keep)
            {
                retainedLength += obu.TotalSize;
            }
            index++;
        }
        byte[] result = new byte[retainedLength];
        int cursor = 0;
        index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            if (retained[index])
            {
                source.AsSpan(obu.Offset, obu.TotalSize).CopyTo(
                    result.AsSpan(cursor, obu.TotalSize));
                cursor += obu.TotalSize;
            }
            index++;
        }
        return result;
    }

    static void AddHdrMetadataObu(
        List<byte[]> values,
        ParsedAv1Metadata metadata)
    {
        Av1WriteResult write = Av1MetadataWriter.WriteObu(
            metadata,
            false,
            0,
            0);
        if (write.Code == AvifResultCode.Success)
        {
            values.Add(write.Data);
        }
    }

    static void AddContentColorVolume(
        List<CanonicalItemPropertyAssociation> properties,
        AvifContentColorVolume? volume)
    {
        int length = 1;
        if ((volume ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaries())
        {
            length += 24;
        }
        if (volume.HasMinimumLuminance())
        {
            length += 4;
        }
        if (volume.HasMaximumLuminance())
        {
            length += 4;
        }
        if (volume.HasAverageLuminance())
        {
            length += 4;
        }
        byte[] payload = new byte[length];
        payload[0] = (byte)((volume.HasPrimaries() ? 32 : 0)
            | (volume.HasMinimumLuminance() ? 16 : 0)
            | (volume.HasMaximumLuminance() ? 8 : 0)
            | (volume.HasAverageLuminance() ? 4 : 0));
        int cursor = 1;
        if (volume.HasPrimaries())
        {
            int index = 0;
            while (index < 3)
            {
                BinaryPrimitives.WriteInt32BigEndian(
                    payload.AsSpan(cursor, 4),
                    volume.GetPrimaryChromaticityX(index));
                BinaryPrimitives.WriteInt32BigEndian(
                    payload.AsSpan(cursor + 4, 4),
                    volume.GetPrimaryChromaticityY(index));
                cursor += 8;
                index++;
            }
        }
        if (volume.HasMinimumLuminance())
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(cursor, 4), volume.GetMinimumLuminance());
            cursor += 4;
        }
        if (volume.HasMaximumLuminance())
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(cursor, 4), volume.GetMaximumLuminance());
            cursor += 4;
        }
        if (volume.HasAverageLuminance())
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(cursor, 4), volume.GetAverageLuminance());
        }
        properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("cclv", payload),
            false));
    }

    static byte[] BuildProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 8);
        return raw;
    }

    static byte[] BuildGridPayload(AvifGrid grid)
    {
        bool large = grid.GetWidth() > ushort.MaxValue
            || grid.GetHeight() > ushort.MaxValue;
        byte[] payload = new byte[large ? 12 : 8];
        payload[1] = large ? (byte)1 : (byte)0;
        payload[2] = (byte)(grid.GetRowCount() - 1);
        payload[3] = (byte)(grid.GetColumnCount() - 1);
        if (large)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(4, 4),
                (uint)grid.GetWidth());
            BinaryPrimitives.WriteUInt32BigEndian(
                payload.AsSpan(8, 4),
                (uint)grid.GetHeight());
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(4, 2),
                (ushort)grid.GetWidth());
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(6, 2),
                (ushort)grid.GetHeight());
        }
        return payload;
    }

    static CanonicalItemPayload CreateLayeredItem(
        uint itemId,
        bool hidden,
        uint sharedDataItemId,
        AvifWriterLayeredPayload layeredPayload,
        List<CanonicalItemPropertyAssociation> baseProperties,
        int selectedLayer,
        int operatingPointIndex)
    {
        CanonicalItemPayload item = sharedDataItemId == 0
            ? CanonicalItemPayload.Create(
                itemId,
                "av01",
                string.Empty,
                hidden,
                layeredPayload.Data)
            : CanonicalItemPayload.Create(
                itemId,
                "av01",
                string.Empty,
                hidden,
                layeredPayload.Data.LongLength);
        item.SharedDataItemId = sharedDataItemId;
        item.Properties = new List<CanonicalItemPropertyAssociation>(
            baseProperties);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("a1op", new byte[] { (byte)operatingPointIndex }),
            true));
        byte[] selector = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(
            selector,
            (ushort)selectedLayer);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildProperty("lsel", selector),
            false));
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildLayerIndexProperty(layeredPayload.LayerSizes),
            false));
        return item;
    }

    static List<CanonicalItemPropertyAssociation>
        CreateSelectedLayerProperties(
            List<CanonicalItemPropertyAssociation> finalProperties,
            List<CanonicalItemPropertyAssociation> selectedProperties)
    {
        CanonicalItemPropertyAssociation? selectedExtents = null;
        int index = 0;
        while (index < selectedProperties.Count)
        {
            CanonicalPropertyProbe probe =
                CanonicalItemContainerWriter.ProbeProperty(
                    selectedProperties[index].RawProperty);
            if (probe.Code == AvifResultCode.Success
                && probe.Type == "ispe")
            {
                selectedExtents = selectedProperties[index];
                break;
            }
            index++;
        }
        List<CanonicalItemPropertyAssociation> result =
            new List<CanonicalItemPropertyAssociation>();
        index = 0;
        while (index < finalProperties.Count)
        {
            CanonicalItemPropertyAssociation property = finalProperties[index];
            CanonicalPropertyProbe probe =
                CanonicalItemContainerWriter.ProbeProperty(
                    property.RawProperty);
            result.Add(probe.Code == AvifResultCode.Success
                    && probe.Type == "ispe"
                    && selectedExtents != null
                ? selectedExtents
                : property);
            index++;
        }
        return result;
    }

    AvifWriterLayeredPayload BuildLayeredPayload(
        AvifLayeredImage? layeredImage,
        List<CanonicalItemPayload> encodedLayers)
    {
        AvifImage finalImage = ((layeredImage ?? throw new InvalidOperationException("Invalid internal state."))
            .GetLayer(layeredImage.GetLayerCount() - 1) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetImage();
        Av1WriteResult prefix = AvifOneCallCodec.CreateLayeredSequencePrefix(
            finalImage,
            layeredImage.GetOperatingPointIndex(),
            layeredImage.GetLayerCount());
        if (prefix.Code != AvifResultCode.Success)
        {
            return new AvifWriterLayeredPayload(prefix.Code);
        }

        List<byte[]> segments = new List<byte[]>();
        long totalLength = prefix.Data.LongLength;
        int index = 0;
        while (index < encodedLayers.Count)
        {
            byte[] paddingPayload = ExtractPaddingPayload(
                encodedLayers[index].Data);
            if (paddingPayload.Length == 0)
            {
                return new AvifWriterLayeredPayload(
                    AvifResultCode.EncodeFailed);
            }
            Av1WriteResult paddingResult = Av1ObuWriter.WriteObu(
                Av1ObuType.Padding,
                paddingPayload,
                true,
                0,
                index);
            if (paddingResult.Code != AvifResultCode.Success)
            {
                return new AvifWriterLayeredPayload(paddingResult.Code);
            }
            byte[] padding = paddingResult.Data;
            long segmentLength = padding.LongLength;
            if (index == 0)
            {
                segmentLength += prefix.Data.LongLength;
            }
            if (segmentLength > uint.MaxValue
                || totalLength > ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes
                    - padding.LongLength
                || totalLength > int.MaxValue - padding.LongLength)
            {
                return new AvifWriterLayeredPayload(
                    AvifResultCode.LimitExceeded);
            }
            segments.Add(padding);
            totalLength += padding.LongLength;
            index++;
        }

        byte[] data = new byte[(int)totalLength];
        prefix.Data.CopyTo(data, 0);
        int cursor = prefix.Data.Length;
        index = 0;
        while (index < segments.Count)
        {
            segments[index].CopyTo(data, cursor);
            cursor += segments[index].Length;
            index++;
        }
        uint[] sizes = new uint[encodedLayers.Count - 1];
        index = 0;
        while (index < sizes.Length)
        {
            long size = segments[index].LongLength;
            if (index == 0)
            {
                size += prefix.Data.LongLength;
            }
            sizes[index] = (uint)size;
            index++;
        }
        AvifWriterLayeredPayload result = new AvifWriterLayeredPayload(
            AvifResultCode.Success);
        result.Data = data;
        result.LayerSizes = sizes;
        return result;
    }

    byte[] ExtractPaddingPayload(byte[] encoded)
    {
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            encoded,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits,
            0);
        if (parsed.Code != AvifResultCode.Success)
        {
            return System.Array.Empty<byte>();
        }
        int index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            if (obu.Header.Type == Av1ObuType.Padding
                && obu.PayloadSize > 0)
            {
                return obu.GetPayload();
            }
            index++;
        }
        return System.Array.Empty<byte>();
    }

    static byte[] BuildLayerIndexProperty(uint[] layerSizes)
    {
        bool large = false;
        int index = 0;
        while (index < layerSizes.Length)
        {
            if (layerSizes[index] > ushort.MaxValue)
            {
                large = true;
                break;
            }
            index++;
        }
        byte[] payload = new byte[large ? 13 : 7];
        payload[0] = large ? (byte)1 : (byte)0;
        index = 0;
        while (index < 3)
        {
            uint size = index < layerSizes.Length ? layerSizes[index] : 0;
            if (large)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    payload.AsSpan(1 + index * 4, 4),
                    size);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(
                    payload.AsSpan(1 + index * 2, 2),
                    (ushort)size);
            }
            index++;
        }
        return BuildProperty("a1lx", payload);
    }

    static byte[] BuildSpatialExtentsProperty(int width, int height)
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(0, 4),
            (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(4, 4),
            (uint)height);
        return BuildFullProperty("ispe", payload);
    }

    static byte[] BuildFullProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[12 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 12);
        return raw;
    }

    static byte[] BuildFlaggedFullProperty(
        string type,
        int flags,
        byte[] payload)
    {
        byte[] raw = BuildFullProperty(type, payload);
        raw[9] = (byte)((flags >> 16) & 255);
        raw[10] = (byte)((flags >> 8) & 255);
        raw[11] = (byte)(flags & 255);
        return raw;
    }

    long GetMetadataLimit(AvifMetadataKind kind)
    {
        if (kind == AvifMetadataKind.Exif)
        {
            return ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxExifBytes;
        }
        if (kind == AvifMetadataKind.Xmp)
        {
            return ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxXmpBytes;
        }
        return ((options ?? throw new InvalidOperationException("Invalid internal state.")).Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes;
    }

    static string GetMetadataName(AvifMetadataKind kind)
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

    static AvifEncoderOptions? CopyOptions(AvifEncoderOptions? source)
    {
        if (source == null)
        {
            return null;
        }
        AvifEncoderOptions copy = new AvifEncoderOptions();
        copy.Mode = source.Mode;
        copy.Preset = source.Preset;
        copy.HeaderFormat = source.HeaderFormat;
        copy.Quality = source.Quality;
        copy.AlphaQuality = source.AlphaQuality;
        copy.GainMapQuality = source.GainMapQuality;
        copy.AuxiliaryQuality = source.AuxiliaryQuality;
        copy.GridCellQuality = source.GridCellQuality;
        copy.LayerQuality = source.LayerQuality;
        copy.Quantizer = source.Quantizer;
        copy.TargetSizeBytes = source.TargetSizeBytes;
        copy.TargetBitrate = source.TargetBitrate;
        copy.KeyframeInterval = source.KeyframeInterval;
        copy.SequencePolicy = source.SequencePolicy;
        copy.FrameLag = source.FrameLag;
        copy.EnableShowExistingFrames = source.EnableShowExistingFrames;
        copy.EnableGlobalMotion = source.EnableGlobalMotion;
        copy.EnableWarpedMotion = source.EnableWarpedMotion;
        copy.FilmGrainParametersPresent = source.FilmGrainParametersPresent;
        copy.TemporalLayerCount = source.TemporalLayerCount;
        copy.SpatialLayers = CopyWriterSpatialLayers(source.SpatialLayers);
        copy.OperatingPoints = CopyWriterOperatingPoints(source.OperatingPoints);
        copy.ProgressiveOutput = source.ProgressiveOutput;
        copy.AutoTiling = source.AutoTiling;
        copy.TileRowsLog2 = source.TileRowsLog2;
        copy.TileColumnsLog2 = source.TileColumnsLog2;
        copy.WorkerCount = source.WorkerCount;
        copy.Deterministic = source.Deterministic;
        copy.ValidateOutput = source.ValidateOutput;
        copy.Limits = ResourceGuard.CopyLimits(source.Limits);
        return copy;
    }

    static AvifResultCode ValidateOptions(AvifEncoderOptions options)
    {
        if (options == null || options.Limits == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.Mode < AvifEncoderMode.ConstantQuality
            || options.Mode > AvifEncoderMode.TargetBitrate
            || options.Preset < AvifEncoderPreset.Fastest
            || options.Preset > AvifEncoderPreset.Best
            || options.HeaderFormat < AvifHeaderFormat.Automatic
            || options.HeaderFormat > AvifHeaderFormat.Minimized
            || options.Quality < 0 || options.Quality > 100
            || options.AlphaQuality < 0 || options.AlphaQuality > 100
            || options.GainMapQuality < 0 || options.GainMapQuality > 100
            || options.AuxiliaryQuality < 0 || options.AuxiliaryQuality > 100
            || options.GridCellQuality < 0 || options.GridCellQuality > 100
            || options.LayerQuality < 0 || options.LayerQuality > 100
            || options.Quantizer < -1 || options.Quantizer > 255
            || options.TargetSizeBytes < 0 || options.TargetBitrate < 0
            || options.KeyframeInterval < 0
            || options.SequencePolicy < AvifSequenceEncodingPolicy.Automatic
            || options.SequencePolicy > AvifSequenceEncodingPolicy.InterFrame
            || options.FrameLag < 0 || options.FrameLag > 16
            || options.TemporalLayerCount < 1 || options.TemporalLayerCount > 8
            || !HasValidWriterSpatialLayers(options.SpatialLayers)
            || !HasValidWriterOperatingPoints(options.OperatingPoints)
            || options.SequencePolicy == AvifSequenceEncodingPolicy.StillPicture
                && (options.FrameLag != 0 || options.TemporalLayerCount != 1
                    || options.SpatialLayers.Length > 1)
            || options.TileRowsLog2 < 0 || options.TileRowsLog2 > 6
            || options.TileColumnsLog2 < 0 || options.TileColumnsLog2 > 6
            || options.WorkerCount < 1
            || options.WorkerCount > options.Limits.MaxWorkerCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static Av1SpatialLayerOptions[] CopyWriterSpatialLayers(
        Av1SpatialLayerOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1SpatialLayerOptions>();
        }
        Av1SpatialLayerOptions[] result =
            new Av1SpatialLayerOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1SpatialLayerOptions copy = new Av1SpatialLayerOptions();
            Av1SpatialLayerOptions value = source[index];
            if (value != null)
            {
                copy.Width = value.Width;
                copy.Height = value.Height;
                copy.Quality = value.Quality;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    static Av1OperatingPointOptions[] CopyWriterOperatingPoints(
        Av1OperatingPointOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1OperatingPointOptions>();
        }
        Av1OperatingPointOptions[] result =
            new Av1OperatingPointOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1OperatingPointOptions copy = new Av1OperatingPointOptions();
            Av1OperatingPointOptions value = source[index];
            if (value != null)
            {
                copy.Idc = value.Idc;
                copy.Level = value.Level;
                copy.Tier = value.Tier;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    static bool HasValidWriterSpatialLayers(Av1SpatialLayerOptions[] values)
    {
        if (values == null || values.Length > 4)
        {
            return false;
        }
        int previousWidth = 0;
        int previousHeight = 0;
        int index = 0;
        while (index < values.Length)
        {
            Av1SpatialLayerOptions value = values[index];
            if (value == null || value.Width <= previousWidth
                || value.Height <= previousHeight
                || value.Quality < -1 || value.Quality > 100)
            {
                return false;
            }
            previousWidth = value.Width;
            previousHeight = value.Height;
            index++;
        }
        return true;
    }

    static bool HasValidWriterOperatingPoints(
        Av1OperatingPointOptions[] values)
    {
        if (values == null || values.Length > 32)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            Av1OperatingPointOptions value = values[index];
            if (value == null || value.Idc < 0 || value.Idc > 4095
                || value.Level < -1 || value.Level > 31
                || value.Tier < 0 || value.Tier > 1)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
