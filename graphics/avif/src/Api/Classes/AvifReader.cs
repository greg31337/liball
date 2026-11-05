using System;
using System.Collections.Generic;
using System.IO;

namespace avif;

/// <summary>Provides stateful inspection, decode, and encoded-item access for one complete AVIF source.</summary>
/// <remarks>
/// Byte-array input is copied. Stream input is buffered on the first parse and the stream is never
/// closed. Returned images and byte arrays are independent caller-owned results.
/// </remarks>
public class AvifReader : IAvifReader, IAvifDiagnosticSource
{
    byte[]? source;
    Stream? sourceStream;
    RandomAccessInput? randomAccessInput;
    AvifDecoderOptions? options;
    AvifContainerValidationResult? validation;
    AvifDocument? document;
    AvifResultCode code;
    AvifResultCode initializationCode;
    AvifByteRange[]? missingRanges;
    bool sourceLoaded;
    bool parsed;
    bool closed;

    /// <summary>Creates a reader over a private copy of a complete byte array.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader Create(byte[]? source)
    {
        return new AvifReader(source, new AvifDecoderOptions());
    }

    /// <summary>Creates a reader over a private source copy with caller-selected decode options.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    public AvifReader(byte[]? source, AvifDecoderOptions? options)
    {
        Initialize(source, null, null, options);
    }

    /// <summary>Creates a byte-array reader with caller-selected options.</summary>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader Create(
        byte[]? source,
        AvifDecoderOptions? options)
    {
        return new AvifReader(source, options);
    }

    /// <summary>Creates a reader that buffers a complete stream on its first parse.</summary>
    /// <param name="source">The readable stream, which remains caller-owned.</param>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader FromStream(Stream? source)
    {
        return FromStream(source, new AvifDecoderOptions());
    }

    /// <summary>Creates a stream reader with caller-selected decode options.</summary>
    /// <param name="source">The readable stream, which remains caller-owned.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader FromStream(
        Stream? source,
        AvifDecoderOptions? options)
    {
        AvifReader result = new AvifReader(null, options);
        result.Initialize(null, source, null, options);
        return result;
    }

    /// <summary>Creates a reader over a caller-owned exact-range source.</summary>
    /// <param name="source">The random-access source, which remains caller-owned.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized range reader.</returns>
    internal static AvifReader FromRandomAccessSource(
        IAvifRandomAccessSource? source,
        AvifDecoderOptions? options)
    {
        AvifReader result = new AvifReader(null, options);
        long maximumSourceBytes = options == null || options.Limits == null
            ? 0 : options.Limits.MaxSourceBytes;
        RandomAccessInput input = new RandomAccessInput(
            source,
            maximumSourceBytes);
        result.Initialize(null, null, input, options);
        return result;
    }

    /// <summary>Creates a stream reader with default options.</summary>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader Create(Stream? source)
    {
        return FromStream(source);
    }

    /// <summary>Creates a stream reader with caller-selected options.</summary>
    /// <returns>The initialized reader.</returns>
    internal static AvifReader Create(
        Stream? source,
        AvifDecoderOptions? options)
    {
        return FromStream(source, options);
    }

    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable reader result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics for the most recent inspection or decode operation.</summary>
    /// <returns>The structured reader diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            document,
            GetCode(),
            AvifDiagnosticCategory.Validation,
            "The AVIF reader operation did not complete successfully.");
    }

    /// <summary>Parses and validates the retained complete source.</summary>
    /// <returns>The parse result; repeated successful calls are harmless.</returns>
    public AvifResultCode Parse()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (initializationCode != AvifResultCode.Success)
        {
            code = initializationCode;
            return initializationCode;
        }
        if ((options ?? throw new InvalidOperationException("Invalid internal state.")).Cancellation.IsCancellationRequested())
        {
            code = AvifResultCode.Cancelled;
            return code;
        }
        if (parsed)
        {
            code = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
            return code;
        }
        if (!sourceLoaded)
        {
            AvifOneCallStreamResult read = randomAccessInput == null
                ? AvifOneCallCodec.ReadStream(
                    sourceStream,
                    options.Limits.MaxSourceBytes,
                    options.Cancellation)
                : AvifOneCallCodec.ReadInput(
                    randomAccessInput,
                    options.Limits.MaxSourceBytes,
                    options.Cancellation);
            code = read.Code;
            if (code != AvifResultCode.Success)
            {
                missingRanges = read.MissingLength <= 0
                    ? System.Array.Empty<AvifByteRange>()
                    : new AvifByteRange[]
                    {
                        new AvifByteRange(
                            (ulong)read.MissingOffset,
                            (ulong)read.MissingLength)
                    };
                return code;
            }
            source = read.Data;
            sourceLoaded = true;
            missingRanges = System.Array.Empty<AvifByteRange>();
        }

        AvifValidationOptions validationOptions = CreateValidationOptions();
        validation = AvifContainerValidator.Validate(source, validationOptions);
        if (options.Cancellation.IsCancellationRequested())
        {
            validation = null;
            code = AvifResultCode.Cancelled;
            return code;
        }
        document = AvifDocumentBuilder.Create(source, validation);
        parsed = true;
        code = document.GetCode();
        return code;
    }

    /// <summary>Returns the parsed document after <c>Parse</c>.</summary>
    /// <returns>The document, or null before parsing or after closing.</returns>
    public AvifDocument? GetDocument()
    {
        return closed || !parsed ? null : document;
    }

    /// <summary>Returns exact ranges unavailable during the most recent range-source operation.</summary>
    /// <returns>An independent range array, empty when no range is pending.</returns>
    public AvifByteRange[] GetMissingRanges()
    {
        if (closed)
        {
            return System.Array.Empty<AvifByteRange>();
        }
        AvifByteRange[] result = new AvifByteRange[
            (missingRanges ?? throw new InvalidOperationException("Invalid internal state.")).Length];
        int index = 0;
        while (index < missingRanges.Length)
        {
            result[index] = new AvifByteRange(
                missingRanges[index].GetOffset(),
                missingRanges[index].GetLength());
            index++;
        }
        return result;
    }

    /// <summary>Decodes the source selected by the configured item-or-sequence policy.</summary>
    /// <returns>A caller-owned result with associated outputs and the parsed document.</returns>
    public AvifDecodeResult DecodeSelectedImage()
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        AvifImageSource? selected = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .SelectSource((options ?? throw new InvalidOperationException("Invalid internal state.")).SourceSelection);
        if (selected == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        if (selected.GetSelection() == AvifSourceSelection.ImageSequence)
        {
            return DecodeFrame(0);
        }
        AvifImageItem? item = selected.GetPrimaryItem();
        if (item == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        return DecodeItem(item.GetItemId());
    }

    /// <summary>Decodes the declared primary image item.</summary>
    /// <returns>A caller-owned image result.</returns>
    public AvifDecodeResult DecodePrimaryImage()
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        AvifFileInfo? info = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();
        if (info == null || !info.HasPrimaryItem())
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        return DecodeItem(info.GetPrimaryItemId());
    }

    /// <summary>Decodes one image or supported derived-image item.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns>A caller-owned image result.</returns>
    public AvifDecodeResult DecodeItem(uint itemId)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        if (itemId == 0)
        {
            code = AvifResultCode.InvalidArgument;
            return FailDecode(code);
        }
        AvifImageItem? item = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        if (item != null && item.GetItemType() == "tmap")
        {
            return DecodeToneMap(itemId);
        }
        AvifDecodeResult result = DecodeItemInternal(
            itemId,
            new List<uint>(),
            0);
        if (result.GetCode() == AvifResultCode.Success)
        {
            result.SetItemSelection(itemId);
            PopulateItemOutputs(result, itemId);
            code = result.GetCode();
        }
        return result;
    }

    /// <summary>Decodes one indexed auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <param name="auxiliaryIndex">The zero-based auxiliary association index.</param>
    /// <returns>The selected caller-owned auxiliary image result.</returns>
    public AvifDecodeResult DecodeAuxiliaryItem(
        uint masterItemId,
        int auxiliaryIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        return DecodeAuxiliaryItemInternal(masterItemId, auxiliaryIndex);
    }

    /// <summary>Decodes the first alpha auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <returns>The selected caller-owned alpha result.</returns>
    public AvifDecodeResult DecodeAlpha(uint masterItemId)
    {
        return DecodeAuxiliaryKind(masterItemId, AvifAuxiliaryKind.Alpha);
    }

    /// <summary>Decodes the first depth auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <returns>The selected caller-owned depth result.</returns>
    public AvifDecodeResult DecodeDepth(uint masterItemId)
    {
        return DecodeAuxiliaryKind(masterItemId, AvifAuxiliaryKind.Depth);
    }

    /// <summary>Decodes the gain image and metadata referenced by a tone-map item.</summary>
    /// <param name="toneMapItemId">The tone-map derived item identifier.</param>
    /// <returns>A caller-owned result containing the gain-map association.</returns>
    public AvifDecodeResult DecodeGainMap(uint toneMapItemId)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        return DecodeToneMapInternal(toneMapItemId, false);
    }

    /// <summary>Decodes a tone-map derived item and its base and gain-map alternatives.</summary>
    /// <param name="toneMapItemId">The tone-map derived item identifier.</param>
    /// <returns>A caller-owned result containing the tone-map association.</returns>
    public AvifDecodeResult DecodeToneMap(uint toneMapItemId)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        return DecodeToneMapInternal(toneMapItemId, true);
    }

    /// <summary>Decodes one indexed frame from the selected timed image sequence.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>A caller-owned image result.</returns>
    public AvifDecodeResult DecodeFrame(int frameIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        AvifImageSource? imageSource = (document ?? throw new InvalidOperationException("Invalid internal state.")).SelectSource(
            AvifSourceSelection.ImageSequence);
        if (imageSource == null || frameIndex < 0
            || frameIndex >= imageSource.GetFrameCount())
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        AvifTrack? track = imageSource.GetSequenceTrack();
        AvifSample? sample = (track ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(frameIndex);
        if (sample == null || sample.GetOffset() > int.MaxValue
            || sample.GetSize() > int.MaxValue
            || sample.GetOffset() > (ulong)(source ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || sample.GetSize() > (ulong)source.Length - sample.GetOffset())
        {
            code = AvifResultCode.InvalidTrack;
            return FailDecode(code);
        }
        AvifImage? image = AvifOneCallCodec.DecodeManagedPayload(
            source.AsSpan((int)sample.GetOffset(), (int)sample.GetSize()),
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        if (image == null)
        {
            code = AvifResultCode.UnsupportedFeature;
            return FailDecode(code);
        }
        code = image.GetCode();
        if (code == AvifResultCode.Success)
        {
            List<ParsedColourInformation>? colors = FindTrackColorInformation(
                track.GetTrackId(),
                sample.GetDescriptionIndex());
            code = colors == null
                ? AvifResultCode.InvalidTrack
                : ColorMetadataResolver.Apply(image, colors, options.Limits);
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return FailDecode(code);
        }
        AvifDecodeResult result = new AvifDecodeResult(
            code,
            image,
            document);
        result.SetSequenceSelection(
            track.GetTrackId(),
            frameIndex,
            imageSource.GetFrameTiming(frameIndex),
            sample);
        PopulateSequenceOutputs(result, track, frameIndex);
        code = result.GetCode();
        return result;
    }

    /// <summary>Decodes one auxiliary track sample associated with a sequence frame.</summary>
    /// <param name="frameIndex">The zero-based sequence frame index.</param>
    /// <param name="auxiliaryIndex">The zero-based auxiliary-track index.</param>
    /// <returns>The selected caller-owned auxiliary frame result.</returns>
    public AvifDecodeResult DecodeSequenceAuxiliary(
        int frameIndex,
        int auxiliaryIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        AvifImageSource? imageSource = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .SelectSource(AvifSourceSelection.ImageSequence);
        AvifTrack? master = imageSource == null
            ? null : imageSource.GetSequenceTrack();
        AvifMovie? movie = document.GetMovie();
        AvifTrack? auxiliary = master == null || movie == null
            ? null : movie.GetAuxiliaryTrack(
                master.GetTrackId(),
                auxiliaryIndex);
        if (auxiliary == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        AvifDecodeResult result = DecodeTrackFrame(
            auxiliary,
            frameIndex);
        if (result.GetCode() != AvifResultCode.Success
            || result.GetImage() == null)
        {
            return result;
        }
        AvifSample? sample = auxiliary.GetSample(frameIndex);
        result.SetSequenceSelection(
            auxiliary.GetTrackId(),
            frameIndex,
            imageSource == null ? null : imageSource.GetFrameTiming(frameIndex),
            sample);
        result.AddAuxiliaryOutput(new AvifDecodedAuxiliary(
            auxiliary.GetAuxiliaryKind(),
            auxiliary.GetAuxiliaryType(),
            0,
            0,
            auxiliary.GetTrackId(),
            (master ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(),
            false,
            result.GetImage()
                ?? throw new InvalidOperationException("Invalid internal state.")));
        return result;
    }

    /// <summary>Decodes one progressive layer of the primary coded item.</summary>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns>A caller-owned image result.</returns>
    public AvifDecodeResult DecodeLayer(int layerIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        AvifFileInfo? info = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();
        ParsedItem? item = info == null || !info.HasPrimaryItem()
            ? null : ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(info.GetPrimaryItemId());
        if (item == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        return DecodeItemLayer(item.GetItemId(), layerIndex);
    }

    /// <summary>Decodes one progressive layer from a selected coded item.</summary>
    /// <param name="itemId">The layered coded item identifier.</param>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns>A caller-owned layer image result.</returns>
    public AvifDecodeResult DecodeItemLayer(uint itemId, int layerIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        ParsedAv1LayeredImageIndex? layers = FindLayerIndex(item);
        if (layers == null || layerIndex < 0
            || layerIndex >= layers.GetLayerCount())
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        EncodedItemExtractionResult extraction = ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(
            source,
            itemId);
        if (extraction.Code != AvifResultCode.Success || extraction.Data == null)
        {
            code = extraction.Code;
            return FailDecode(code);
        }
        int length = GetLayerPrefixLength(layers, layerIndex, extraction.Data.Length);
        AvifImage? image = length < 0
            ? null : AvifOneCallCodec.DecodeManagedPayload(
                extraction.Data.GetSpan().Slice(0, length),
                (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        extraction.Data.Close();
        if (image == null)
        {
            code = length < 0
                ? AvifResultCode.InvalidItem
                : AvifResultCode.UnsupportedFeature;
            return FailDecode(code);
        }
        code = image.GetCode();
        if (code == AvifResultCode.Success)
        {
            code = ColorMetadataResolver.Apply(
                image,
                item ?? throw new InvalidOperationException("Invalid internal state."),
                (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return FailDecode(code);
        }
        AvifDecodeResult result = new AvifDecodeResult(
            code,
            image,
            document);
        result.SetItemSelection(itemId);
        result.AddProgressiveLayerOutput(image);
        return result;
    }

    /// <summary>Decodes one row and column of a grid derived-image item.</summary>
    /// <param name="itemId">The grid item identifier.</param>
    /// <param name="row">The zero-based grid row.</param>
    /// <param name="column">The zero-based grid column.</param>
    /// <returns>A caller-owned cell image result.</returns>
    public AvifDecodeResult DecodeTile(uint itemId, int row, int column)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        uint[]? dependencies = GetGridDependencies(item);
        byte[] gridPayload = ExtractItem(itemId, false);
        if (item == null || item.GetItemType() != "grid"
            || dependencies == null || gridPayload.Length == 0)
        {
            if (code == AvifResultCode.Success)
            {
                code = AvifResultCode.NotFound;
            }
            return FailDecode(code);
        }
        AvifGrid grid = AvifGrid.Create(gridPayload, dependencies, (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        if (grid.GetCode() != AvifResultCode.Success)
        {
            code = grid.GetCode();
            return FailDecode(code);
        }
        if (row < 0 || column < 0 || row >= grid.GetRowCount()
            || column >= grid.GetColumnCount())
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        int cellIndex = row * grid.GetColumnCount() + column;
        return DecodeItem(dependencies[cellIndex]);
    }

    /// <summary>Returns timing metadata for one selected sequence frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame timing, or null when unavailable.</returns>
    public AvifFrameTiming? GetFrameTiming(int frameIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return null;
        }
        AvifImageSource? imageSource = (document ?? throw new InvalidOperationException("Invalid internal state.")).SelectSource(
            AvifSourceSelection.ImageSequence);
        AvifFrameTiming? timing = imageSource == null
            ? null : imageSource.GetFrameTiming(frameIndex);
        code = timing == null ? AvifResultCode.NotFound : AvifResultCode.Success;
        return timing;
    }

    /// <summary>Returns one metadata item associated with an image selection.</summary>
    /// <param name="itemId">The described image item identifier.</param>
    /// <param name="metadataIndex">The zero-based associated metadata index.</param>
    /// <returns>An independently owned metadata payload, or null when unavailable.</returns>
    public AvifMetadata? GetAssociatedMetadataItem(
        uint itemId,
        int metadataIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return null;
        }
        AvifMetadata? result = FindAssociatedMetadata(
            itemId,
            metadataIndex);
        code = result == null
            ? AvifResultCode.NotFound : AvifResultCode.Success;
        return result;
    }

    /// <summary>Extracts one item's encoded or derived-image payload.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns>An independent byte array, or an empty array on failure.</returns>
    public byte[] GetEncodedItem(uint itemId)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return System.Array.Empty<byte>();
        }
        return ExtractItem(itemId, true);
    }

    /// <summary>Extracts a complete resolved item payload under a caller byte bound.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    public AvifPayloadExtraction ExtractItemPayload(
        uint itemId,
        long maximumBytes)
    {
        ItemPayloadResolveResult resolved = ResolveExtractionItem(
            itemId,
            maximumBytes);
        if (resolved.Code != AvifResultCode.Success
            || resolved.Payload == null)
        {
            return FailExtraction(resolved.Code);
        }
        return ExtractResolvedPayloadSlice(
            resolved.Payload,
            0,
            resolved.Payload.GetLength(),
            maximumBytes,
            itemId,
            0,
            -1,
            -1,
            0,
            0,
            false);
    }

    /// <summary>Extracts one resolved source segment of an item payload.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="segmentIndex">The zero-based resolved segment index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction retaining the exact source range.</returns>
    public AvifPayloadExtraction ExtractItemPayloadSegment(
        uint itemId,
        int segmentIndex,
        long maximumBytes)
    {
        ItemPayloadResolveResult resolved = ResolveExtractionItem(
            itemId,
            maximumBytes);
        if (resolved.Code != AvifResultCode.Success
            || resolved.Payload == null)
        {
            return FailExtraction(resolved.Code);
        }
        ParsedItemPayloadSegment? selected = resolved.Payload.GetSegment(
            segmentIndex);
        if (selected == null)
        {
            code = AvifResultCode.NotFound;
            return FailExtraction(code);
        }
        long payloadOffset = 0;
        int index = 0;
        while (index < segmentIndex)
        {
            ParsedItemPayloadSegment? prior = resolved.Payload.GetSegment(index);
            payloadOffset += (prior ?? throw new InvalidOperationException("Invalid internal state.")).GetLength();
            index++;
        }
        return ExtractResolvedPayloadSlice(
            resolved.Payload,
            payloadOffset,
            selected.GetLength(),
            maximumBytes,
            itemId,
            0,
            -1,
            -1,
            0,
            0,
            false);
    }

    /// <summary>Extracts one complete encoded track sample under a caller byte bound.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction retaining the exact sample source range.</returns>
    public AvifPayloadExtraction ExtractTrackSamplePayload(
        uint trackId,
        int sampleIndex,
        long maximumBytes)
    {
        AvifSample? sample = FindExtractionSample(
            trackId,
            sampleIndex,
            maximumBytes);
        if (sample == null)
        {
            return FailExtraction(code);
        }
        return ExtractSourceRange(
            (long)sample.GetOffset(),
            sample.GetSize(),
            maximumBytes,
            0,
            trackId,
            sampleIndex,
            sampleIndex,
            0,
            0,
            false,
            AvifResultCode.InvalidTrack);
    }

    /// <summary>Extracts the samples required from the nearest synchronization point through a selected sample.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based final sample index.</param>
    /// <param name="maximumBytes">The maximum permitted combined byte count.</param>
    /// <returns>An owned extraction with one exact source segment per included sample.</returns>
    public AvifPayloadExtraction ExtractSampleDependencyRange(
        uint trackId,
        int sampleIndex,
        long maximumBytes)
    {
        AvifSample? requested = FindExtractionSample(
            trackId,
            sampleIndex,
            maximumBytes);
        if (requested == null)
        {
            return FailExtraction(code);
        }
        AvifMovie? movie = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie();
        AvifTrack track = ((movie ?? throw new InvalidOperationException("Invalid internal state."))
            .FindTrack(trackId) ?? throw new InvalidOperationException("Invalid internal state."));
        int firstIndex = sampleIndex;
        while (firstIndex > 0)
        {
            AvifSample? current = track.GetSample(firstIndex);
            if ((current ?? throw new InvalidOperationException("Invalid internal state.")).IsSync())
            {
                break;
            }
            firstIndex--;
        }
        List<AvifSample> samples = new List<AvifSample>();
        int index = firstIndex;
        while (index <= sampleIndex)
        {
            samples.Add(track.GetSample(index)
                ?? throw new InvalidOperationException("Invalid internal state."));
            index++;
        }
        return ExtractSampleList(
            samples,
            maximumBytes,
            trackId,
            firstIndex,
            sampleIndex);
    }

    /// <summary>Extracts one configuration OBU associated with an image item.</summary>
    /// <param name="itemId">The nonzero coded-image item identifier.</param>
    /// <param name="obuIndex">The zero-based configuration OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with its exact property source range.</returns>
    public AvifPayloadExtraction ExtractItemConfigurationObu(
        uint itemId,
        int obuIndex,
        long maximumBytes)
    {
        AvifResultCode state = ValidateExtractionArguments(
            maximumBytes,
            itemId == 0 || obuIndex < 0);
        if (state != AvifResultCode.Success)
        {
            return FailExtraction(state);
        }
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        ParsedAv1Configuration? configuration = FindItemConfiguration(item);
        if (configuration == null)
        {
            code = AvifResultCode.NotFound;
            return FailExtraction(code);
        }
        return ExtractConfigurationObu(
            configuration,
            obuIndex,
            maximumBytes,
            itemId,
            0);
    }

    /// <summary>Extracts one configuration OBU associated with a track sample description.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="descriptionIndex">The one-based sample-description index.</param>
    /// <param name="obuIndex">The zero-based configuration OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with its exact property source range.</returns>
    public AvifPayloadExtraction ExtractTrackConfigurationObu(
        uint trackId,
        int descriptionIndex,
        int obuIndex,
        long maximumBytes)
    {
        AvifResultCode state = ValidateExtractionArguments(
            maximumBytes,
            trackId == 0 || descriptionIndex < 1 || obuIndex < 0);
        if (state != AvifResultCode.Success)
        {
            return FailExtraction(state);
        }
        ParsedAv1Configuration? configuration = FindTrackConfiguration(
            trackId,
            descriptionIndex);
        if (configuration == null)
        {
            code = AvifResultCode.NotFound;
            return FailExtraction(code);
        }
        return ExtractConfigurationObu(
            configuration,
            obuIndex,
            maximumBytes,
            0,
            trackId);
    }

    /// <summary>Extracts one metadata OBU from an encoded item payload.</summary>
    /// <param name="itemId">The nonzero coded-image item identifier.</param>
    /// <param name="metadataIndex">The zero-based metadata OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with metadata type information.</returns>
    public AvifPayloadExtraction ExtractItemMetadataObu(
        uint itemId,
        int metadataIndex,
        long maximumBytes)
    {
        ItemPayloadResolveResult resolved = ResolveExtractionItem(
            itemId,
            maximumBytes);
        if (resolved.Code != AvifResultCode.Success
            || resolved.Payload == null)
        {
            return FailExtraction(resolved.Code);
        }
        if (metadataIndex < 0)
        {
            code = AvifResultCode.InvalidArgument;
            return FailExtraction(code);
        }
        return ExtractMetadataObuFromItem(
            resolved.Payload,
            metadataIndex,
            maximumBytes,
            itemId);
    }

    /// <summary>Extracts one metadata OBU from an encoded track sample.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="metadataIndex">The zero-based metadata OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with metadata type information.</returns>
    public AvifPayloadExtraction ExtractTrackMetadataObu(
        uint trackId,
        int sampleIndex,
        int metadataIndex,
        long maximumBytes)
    {
        AvifSample? sample = FindExtractionSample(
            trackId,
            sampleIndex,
            maximumBytes);
        if (sample == null)
        {
            return FailExtraction(code);
        }
        if (metadataIndex < 0)
        {
            code = AvifResultCode.InvalidArgument;
            return FailExtraction(code);
        }
        return ExtractMetadataObuFromSample(
            sample,
            metadataIndex,
            maximumBytes,
            trackId,
            sampleIndex);
    }

    /// <summary>Extracts one independently decodable progressive-layer payload segment.</summary>
    /// <param name="itemId">The nonzero layered item identifier.</param>
    /// <param name="layerIndex">The zero-based layer index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    public AvifPayloadExtraction ExtractLayerPayload(
        uint itemId,
        int layerIndex,
        long maximumBytes)
    {
        ItemPayloadResolveResult resolved = ResolveExtractionItem(
            itemId,
            maximumBytes);
        if (resolved.Code != AvifResultCode.Success
            || resolved.Payload == null)
        {
            return FailExtraction(resolved.Code);
        }
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        ParsedAv1LayeredImageIndex? layers = FindLayerIndex(item);
        if (layers == null || layerIndex < 0
            || layerIndex >= layers.GetLayerCount())
        {
            code = layerIndex < 0
                ? AvifResultCode.InvalidArgument : AvifResultCode.NotFound;
            return FailExtraction(code);
        }
        long offset = 0;
        int index = 0;
        while (index < layerIndex)
        {
            offset += layers.GetLayerSize(index);
            index++;
        }
        long length = layerIndex == layers.GetLayerCount() - 1
            ? resolved.Payload.GetLength() - offset
            : layers.GetLayerSize(layerIndex);
        return ExtractResolvedPayloadSlice(
            resolved.Payload,
            offset,
            length,
            maximumBytes,
            itemId,
            0,
            -1,
            -1,
            0,
            0,
            false);
    }

    /// <summary>Extracts an opaque container metadata item without interpreting its bytes.</summary>
    /// <param name="itemId">The nonzero custom metadata item identifier.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    public AvifPayloadExtraction ExtractUnknownMetadata(
        uint itemId,
        long maximumBytes)
    {
        AvifResultCode state = ValidateExtractionArguments(
            maximumBytes,
            itemId == 0);
        if (state != AvifResultCode.Success)
        {
            return FailExtraction(state);
        }
        AvifImageItem? item = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId);
        bool known = item != null && (item.GetItemType() == "Exif"
            || item.GetItemType() == "mime"
                && item.GetContentType() == "application/rdf+xml");
        if (item == null || !item.IsMetadataItem() || known)
        {
            code = AvifResultCode.NotFound;
            return FailExtraction(code);
        }
        return ExtractItemPayload(itemId, maximumBytes);
    }

    /// <summary>Copies one encoded item payload into a caller-provided buffer.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="destination">The destination buffer, which may be larger than the payload.</param>
    /// <returns>Success, BufferTooSmall, NotFound, or the reader state code.</returns>
    public AvifResultCode CopyEncodedItemTo(
        uint itemId,
        Span<byte> destination)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        ItemPayloadResolveResult resolved =
            (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
                ? new ItemPayloadResolveResult(AvifResultCode.NotFound, null)
                : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(itemId);
        if (resolved.Code != AvifResultCode.Success || resolved.Payload == null)
        {
            code = resolved.Code;
            return code;
        }
        long length = resolved.Payload.GetLength();
        if (length > destination.Length)
        {
            code = AvifResultCode.BufferTooSmall;
            return code;
        }
        InputReadResult copied = resolved.Payload.ReadExact(
            source,
            0,
            destination.Slice(0, (int)length));
        code = copied.Code;
        return code;
    }

    /// <summary>Writes one encoded item payload without closing the destination stream.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidArgument, or the reader state code.</returns>
    public AvifResultCode WriteEncodedItemTo(
        uint itemId,
        Stream destination)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        ItemPayloadResolveResult resolved =
            (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
                ? new ItemPayloadResolveResult(AvifResultCode.NotFound, null)
                : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(itemId);
        if (resolved.Code != AvifResultCode.Success || resolved.Payload == null)
        {
            code = resolved.Code;
            return code;
        }
        code = WritePayload(resolved.Payload, destination);
        return code;
    }

    /// <summary>Copies one complete parsed box into a caller-provided buffer.</summary>
    /// <param name="boxIndex">The zero-based depth-first box index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the box.</param>
    /// <returns>Success, BufferTooSmall, NotFound, or the reader state code.</returns>
    public AvifResultCode CopyBoxDataTo(
        int boxIndex,
        Span<byte> destination)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        AvifBox? box = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetBox(boxIndex);
        if (box == null)
        {
            code = AvifResultCode.NotFound;
            return code;
        }
        code = CopySourceRange(
            box.GetOffset(),
            box.GetSize(),
            destination,
            AvifResultCode.InvalidContainer);
        return code;
    }

    /// <summary>Writes one complete parsed box without closing the destination stream.</summary>
    /// <param name="boxIndex">The zero-based depth-first box index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidArgument, or the reader state code.</returns>
    public AvifResultCode WriteBoxDataTo(
        int boxIndex,
        Stream destination)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        AvifBox? box = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetBox(boxIndex);
        if (box == null)
        {
            code = AvifResultCode.NotFound;
            return code;
        }
        code = WriteSourceRange(
            box.GetOffset(),
            box.GetSize(),
            destination,
            AvifResultCode.InvalidContainer);
        return code;
    }

    /// <summary>Copies one resolved item extent into a caller-provided buffer.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="extentIndex">The zero-based extent index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the extent.</param>
    /// <returns>Success, BufferTooSmall, NotFound, InvalidItem, or the reader state code.</returns>
    public AvifResultCode CopyItemExtentTo(
        uint itemId,
        int extentIndex,
        Span<byte> destination)
    {
        AvifItemExtent? extent = FindExtent(itemId, extentIndex);
        if (extent == null)
        {
            return code;
        }
        code = CopySourceRange(
            extent.GetResolvedOffset(),
            extent.GetResolvedLength(),
            destination,
            AvifResultCode.InvalidItem);
        return code;
    }

    /// <summary>Writes one resolved item extent without closing the destination stream.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="extentIndex">The zero-based extent index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidItem, InvalidArgument, or the reader state code.</returns>
    public AvifResultCode WriteItemExtentTo(
        uint itemId,
        int extentIndex,
        Stream destination)
    {
        AvifItemExtent? extent = FindExtent(itemId, extentIndex);
        if (extent == null)
        {
            return code;
        }
        code = WriteSourceRange(
            extent.GetResolvedOffset(),
            extent.GetResolvedLength(),
            destination,
            AvifResultCode.InvalidItem);
        return code;
    }

    /// <summary>Copies one encoded track sample into a caller-provided buffer.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the sample.</param>
    /// <returns>Success, BufferTooSmall, NotFound, InvalidTrack, or the reader state code.</returns>
    public AvifResultCode CopySampleTo(
        uint trackId,
        int sampleIndex,
        Span<byte> destination)
    {
        AvifSample? sample = FindSample(trackId, sampleIndex);
        if (sample == null)
        {
            return code;
        }
        code = CopySourceRange(
            (long)sample.GetOffset(),
            sample.GetSize(),
            destination,
            AvifResultCode.InvalidTrack);
        return code;
    }

    /// <summary>Writes one encoded track sample without closing the destination stream.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidTrack, InvalidArgument, or the reader state code.</returns>
    public AvifResultCode WriteSampleTo(
        uint trackId,
        int sampleIndex,
        Stream destination)
    {
        AvifSample? sample = FindSample(trackId, sampleIndex);
        if (sample == null)
        {
            return code;
        }
        code = WriteSourceRange(
            (long)sample.GetOffset(),
            sample.GetSize(),
            destination,
            AvifResultCode.InvalidTrack);
        return code;
    }

    /// <summary>Clears parse and decode state while retaining the buffered source.</summary>
    /// <returns>Success, or Closed after closing the reader.</returns>
    public AvifResultCode Reset()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        validation = null;
        document = null;
        missingRanges = System.Array.Empty<AvifByteRange>();
        parsed = false;
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Releases retained source and parse state without closing a caller stream.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (closed)
        {
            return AvifResultCode.Success;
        }
        source = System.Array.Empty<byte>();
        sourceStream = null;
        if (randomAccessInput != null)
        {
            randomAccessInput.Close();
        }
        randomAccessInput = null;
        validation = null;
        document = null;
        missingRanges = System.Array.Empty<AvifByteRange>();
        sourceLoaded = false;
        parsed = false;
        closed = true;
        code = AvifResultCode.Closed;
        return AvifResultCode.Success;
    }

    AvifPayloadExtraction FailExtraction(AvifResultCode resultCode)
    {
        code = resultCode;
        return new AvifPayloadExtraction(
            resultCode,
            System.Array.Empty<byte>(),
            new List<AvifPayloadSegment>(),
            0,
            0,
            -1,
            -1,
            0,
            0,
            false);
    }

    AvifResultCode ValidateExtractionArguments(
        long maximumBytes,
        bool invalidSelection)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (maximumBytes < 0 || invalidSelection)
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        return AvifResultCode.Success;
    }

    ItemPayloadResolveResult ResolveExtractionItem(
        uint itemId,
        long maximumBytes)
    {
        AvifResultCode state = ValidateExtractionArguments(
            maximumBytes,
            itemId == 0);
        if (state != AvifResultCode.Success)
        {
            return new ItemPayloadResolveResult(state, null);
        }
        ItemPayloadResolveResult result =
            (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
                ? new ItemPayloadResolveResult(AvifResultCode.NotFound, null)
                : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(itemId);
        code = result.Code;
        return result;
    }

    AvifSample? FindExtractionSample(
        uint trackId,
        int sampleIndex,
        long maximumBytes)
    {
        AvifResultCode state = ValidateExtractionArguments(
            maximumBytes,
            trackId == 0 || sampleIndex < 0);
        if (state != AvifResultCode.Success)
        {
            return null;
        }
        return FindSample(trackId, sampleIndex);
    }

    AvifPayloadExtraction ExtractResolvedPayloadSlice(
        ParsedItemPayload payload,
        long payloadOffset,
        long payloadLength,
        long maximumBytes,
        uint itemId,
        uint trackId,
        int firstSampleIndex,
        int lastSampleIndex,
        int obuType,
        ulong metadataType,
        bool metadataTypeKnown)
    {
        if (payloadLength < 0 || payloadOffset < 0
            || payloadLength > maximumBytes
            || payloadLength > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxTemporaryBytes
            || payloadLength > int.MaxValue)
        {
            code = payloadLength < 0 || payloadOffset < 0
                ? AvifResultCode.InvalidItem : AvifResultCode.LimitExceeded;
            return FailExtraction(code);
        }
        List<ParsedItemPayloadSegment> selected =
            new List<ParsedItemPayloadSegment>();
        AvifResultCode sliceCode = payload.AppendSlice(
            payloadOffset,
            payloadLength,
            selected,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxReferenceCount);
        if (sliceCode != AvifResultCode.Success)
        {
            return FailExtraction(sliceCode);
        }
        byte[] data = new byte[(int)payloadLength];
        InputReadResult read = payload.ReadExact(
            source,
            payloadOffset,
            data);
        if (read.Code != AvifResultCode.Success)
        {
            return FailExtraction(read.Code);
        }
        List<AvifPayloadSegment> mappings =
            new List<AvifPayloadSegment>();
        long outputOffset = 0;
        int index = 0;
        while (index < selected.Count)
        {
            ParsedItemPayloadSegment segment = selected[index];
            mappings.Add(new AvifPayloadSegment(
                outputOffset,
                segment.GetSourceOffset(),
                segment.GetLength()));
            outputOffset += segment.GetLength();
            index++;
        }
        code = AvifResultCode.Success;
        return new AvifPayloadExtraction(
            code,
            data,
            mappings,
            itemId,
            trackId,
            firstSampleIndex,
            lastSampleIndex,
            obuType,
            metadataType,
            metadataTypeKnown);
    }

    AvifPayloadExtraction ExtractSourceRange(
        long offset,
        long length,
        long maximumBytes,
        uint itemId,
        uint trackId,
        int firstSampleIndex,
        int lastSampleIndex,
        int obuType,
        ulong metadataType,
        bool metadataTypeKnown,
        AvifResultCode invalidCode)
    {
        if (length < 0 || offset < 0)
        {
            return FailExtraction(invalidCode);
        }
        if (length > maximumBytes
            || length > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxTemporaryBytes
            || length > int.MaxValue)
        {
            return FailExtraction(AvifResultCode.LimitExceeded);
        }
        CheckedRangeResult range = CheckedMath.ResolveRange(
            offset,
            0,
            length,
            (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
        if (!range.Success)
        {
            return FailExtraction(invalidCode);
        }
        byte[] data = source.AsSpan((int)offset, (int)length).ToArray();
        List<AvifPayloadSegment> mappings =
            new List<AvifPayloadSegment>();
        if (length > 0)
        {
            mappings.Add(new AvifPayloadSegment(0, offset, length));
        }
        code = AvifResultCode.Success;
        return new AvifPayloadExtraction(
            code,
            data,
            mappings,
            itemId,
            trackId,
            firstSampleIndex,
            lastSampleIndex,
            obuType,
            metadataType,
            metadataTypeKnown);
    }

    AvifPayloadExtraction ExtractSampleList(
        List<AvifSample> samples,
        long maximumBytes,
        uint trackId,
        int firstSampleIndex,
        int lastSampleIndex)
    {
        long length = 0;
        int index = 0;
        while (index < samples.Count)
        {
            CheckedIntegerResult next = CheckedMath.AddInt64(
                length,
                samples[index].GetSize());
            if (!next.Success)
            {
                return FailExtraction(next.Code);
            }
            length = next.Value;
            index++;
        }
        if (length > maximumBytes
            || length > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxTemporaryBytes
            || length > int.MaxValue)
        {
            return FailExtraction(AvifResultCode.LimitExceeded);
        }
        byte[] data = new byte[(int)length];
        List<AvifPayloadSegment> mappings =
            new List<AvifPayloadSegment>();
        int outputOffset = 0;
        index = 0;
        while (index < samples.Count)
        {
            AvifSample sample = samples[index];
            CheckedRangeResult range = CheckedMath.ResolveRange(
                (long)sample.GetOffset(),
                0,
                sample.GetSize(),
                (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
            if (!range.Success)
            {
                return FailExtraction(AvifResultCode.InvalidTrack);
            }
            source.AsSpan(
                (int)sample.GetOffset(),
                (int)sample.GetSize()).CopyTo(data.AsSpan(outputOffset));
            mappings.Add(new AvifPayloadSegment(
                outputOffset,
                (long)sample.GetOffset(),
                sample.GetSize()));
            outputOffset += (int)sample.GetSize();
            index++;
        }
        code = AvifResultCode.Success;
        return new AvifPayloadExtraction(
            code,
            data,
            mappings,
            0,
            trackId,
            firstSampleIndex,
            lastSampleIndex,
            0,
            0,
            false);
    }

    static ParsedAv1Configuration? FindItemConfiguration(
        ParsedItem? item)
    {
        if (item == null)
        {
            return null;
        }
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedItemProperty property = (item.GetPropertyAssociation(index)
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty();
            ParsedAv1Configuration? configuration =
                property.GetAv1Configuration();
            if (configuration != null)
            {
                return configuration;
            }
            index++;
        }
        return null;
    }

    ParsedAv1Configuration? FindTrackConfiguration(
        uint trackId,
        int descriptionIndex)
    {
        if ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Movie == null)
        {
            return null;
        }
        int trackIndex = 0;
        while (trackIndex < (validation.Movie ?? throw new InvalidOperationException("Invalid internal state.")).Tracks.Count)
        {
            ParsedMovieTrack track = validation.Movie.Tracks[trackIndex];
            if (track.TrackId == trackId)
            {
                int index = descriptionIndex - 1;
                return index < 0
                    || index >= track.SampleTable.Descriptions.Count
                    ? null : track.SampleTable.Descriptions[index].Configuration;
            }
            trackIndex++;
        }
        return null;
    }

    AvifPayloadExtraction ExtractConfigurationObu(
        ParsedAv1Configuration configuration,
        int obuIndex,
        long maximumBytes,
        uint itemId,
        uint trackId)
    {
        byte[] configObus = configuration.GetConfigObus();
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            configObus,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits,
            0);
        if (parsed.Code != AvifResultCode.Success
            || obuIndex >= parsed.Obus.Count)
        {
            code = parsed.Code == AvifResultCode.Success
                ? AvifResultCode.NotFound : parsed.Code;
            return FailExtraction(code);
        }
        Av1Obu obu = parsed.Obus[obuIndex];
        long baseOffset = configuration.GetConfigObusSourceOffset();
        if (baseOffset < 0)
        {
            return FailExtraction(AvifResultCode.InvalidState);
        }
        CheckedIntegerResult sourceOffset = CheckedMath.AddInt64(
            baseOffset,
            obu.Offset);
        if (!sourceOffset.Success)
        {
            return FailExtraction(sourceOffset.Code);
        }
        return ExtractSourceRange(
            sourceOffset.Value,
            obu.TotalSize,
            maximumBytes,
            itemId,
            trackId,
            -1,
            -1,
            (int)obu.Header.Type,
            0,
            false,
            AvifResultCode.InvalidContainer);
    }

    AvifPayloadExtraction ExtractMetadataObuFromItem(
        ParsedItemPayload payload,
        int metadataIndex,
        long maximumBytes,
        uint itemId)
    {
        byte[] data = ReadResolvedPayloadData(payload);
        if (code != AvifResultCode.Success)
        {
            return FailExtraction(code);
        }
        Av1Obu? obu = FindMetadataObu(data, metadataIndex);
        if (obu == null)
        {
            return FailExtraction(code);
        }
        Av1MetadataParseResult metadata = Av1MetadataParser.ParseObu(obu);
        if (metadata.Code != AvifResultCode.Success
            || metadata.Metadata == null)
        {
            return FailExtraction(metadata.Code);
        }
        return ExtractResolvedPayloadSlice(
            payload,
            obu.Offset,
            obu.TotalSize,
            maximumBytes,
            itemId,
            0,
            -1,
            -1,
            (int)obu.Header.Type,
            metadata.Metadata.MetadataType,
            metadata.Metadata.IsKnownType());
    }

    AvifPayloadExtraction ExtractMetadataObuFromSample(
        AvifSample sample,
        int metadataIndex,
        long maximumBytes,
        uint trackId,
        int sampleIndex)
    {
        CheckedRangeResult range = CheckedMath.ResolveRange(
            (long)sample.GetOffset(),
            0,
            sample.GetSize(),
            (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
        if (!range.Success)
        {
            return FailExtraction(AvifResultCode.InvalidTrack);
        }
        ReadOnlySpan<byte> sampleData = source.AsSpan(
            (int)sample.GetOffset(),
            (int)sample.GetSize());
        Av1Obu? obu = FindMetadataObu(sampleData, metadataIndex);
        if (obu == null)
        {
            return FailExtraction(code);
        }
        Av1MetadataParseResult metadata = Av1MetadataParser.ParseObu(obu);
        if (metadata.Code != AvifResultCode.Success
            || metadata.Metadata == null)
        {
            return FailExtraction(metadata.Code);
        }
        long offset = (long)sample.GetOffset() + obu.Offset;
        return ExtractSourceRange(
            offset,
            obu.TotalSize,
            maximumBytes,
            0,
            trackId,
            sampleIndex,
            sampleIndex,
            (int)obu.Header.Type,
            metadata.Metadata.MetadataType,
            metadata.Metadata.IsKnownType(),
            AvifResultCode.InvalidTrack);
    }

    byte[] ReadResolvedPayloadData(ParsedItemPayload payload)
    {
        long length = payload.GetLength();
        if (length > int.MaxValue
            || length > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxTemporaryBytes)
        {
            code = AvifResultCode.LimitExceeded;
            return System.Array.Empty<byte>();
        }
        byte[] data = new byte[(int)length];
        InputReadResult read = payload.ReadExact(source, 0, data);
        code = read.Code;
        return code == AvifResultCode.Success
            ? data : System.Array.Empty<byte>();
    }

    Av1Obu? FindMetadataObu(
        ReadOnlySpan<byte> data,
        int metadataIndex)
    {
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            data,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits,
            0);
        if (parsed.Code != AvifResultCode.Success)
        {
            code = parsed.Code;
            return null;
        }
        int found = 0;
        int index = 0;
        while (index < parsed.Obus.Count)
        {
            Av1Obu obu = parsed.Obus[index];
            if (obu.Header.Type == Av1ObuType.Metadata)
            {
                if (found == metadataIndex)
                {
                    code = AvifResultCode.Success;
                    return obu;
                }
                found++;
            }
            index++;
        }
        code = AvifResultCode.NotFound;
        return null;
    }

    AvifItemExtent? FindExtent(uint itemId, int extentIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return null;
        }
        AvifImageItem? item = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifItemExtent? extent = item == null
            ? null : item.GetExtent(extentIndex);
        if (extent == null)
        {
            code = AvifResultCode.NotFound;
            return null;
        }
        if (!extent.IsResolved())
        {
            code = AvifResultCode.InvalidItem;
            return null;
        }
        return extent;
    }

    AvifSample? FindSample(uint trackId, int sampleIndex)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return null;
        }
        AvifMovie? movie = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie();
        AvifTrack? track = movie == null ? null : movie.FindTrack(trackId);
        AvifSample? sample = track == null ? null : track.GetSample(sampleIndex);
        if (sample == null)
        {
            code = AvifResultCode.NotFound;
            return null;
        }
        return sample;
    }

    AvifResultCode CopySourceRange(
        long offset,
        long length,
        Span<byte> destination,
        AvifResultCode invalidCode)
    {
        CheckedRangeResult range = CheckedMath.ResolveRange(
            offset,
            0,
            length,
            (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
        if (!range.Success)
        {
            return invalidCode;
        }
        if (length > destination.Length)
        {
            return AvifResultCode.BufferTooSmall;
        }
        source.AsSpan((int)offset, (int)length).CopyTo(destination);
        return AvifResultCode.Success;
    }

    AvifResultCode WriteSourceRange(
        long offset,
        long length,
        Stream destination,
        AvifResultCode invalidCode)
    {
        CheckedRangeResult range = CheckedMath.ResolveRange(
            offset,
            0,
            length,
            (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
        if (!range.Success)
        {
            return invalidCode;
        }
        return CallerOutput.Write(
            source.AsSpan((int)offset, (int)length),
            destination);
    }

    AvifResultCode WritePayload(
        ParsedItemPayload payload,
        Stream destination)
    {
        StreamOutput output = new StreamOutput(destination, true);
        if (output.Code != AvifResultCode.Success)
        {
            output.Close();
            return output.Code;
        }
        int index = 0;
        while (index < payload.GetSegmentCount())
        {
            ParsedItemPayloadSegment? segment = payload.GetSegment(index);
            CheckedRangeResult range = CheckedMath.ResolveRange(
                (segment ?? throw new InvalidOperationException("Invalid internal state.")).GetSourceOffset(),
                0,
                segment.GetLength(),
                (source ?? throw new InvalidOperationException("Invalid internal state.")).Length);
            if (!range.Success)
            {
                output.Close();
                return AvifResultCode.InvalidItem;
            }
            index++;
        }
        AvifResultCode result = AvifResultCode.Success;
        index = 0;
        while (index < payload.GetSegmentCount()
            && result == AvifResultCode.Success)
        {
            ParsedItemPayloadSegment? segment = payload.GetSegment(index);
            result = output.Write(source.AsSpan(
                (int)(segment ?? throw new InvalidOperationException("Invalid internal state.")).GetSourceOffset(),
                (int)segment.GetLength()));
            index++;
        }
        if (result == AvifResultCode.Success)
        {
            result = output.Flush();
        }
        output.Close();
        return result;
    }

    void Initialize(
        byte[]? requestedSource,
        Stream? requestedStream,
        RandomAccessInput? requestedRandomAccessInput,
        AvifDecoderOptions? requestedOptions)
    {
        source = System.Array.Empty<byte>();
        sourceStream = requestedStream;
        randomAccessInput = requestedRandomAccessInput;
        options = CopyOptions(requestedOptions);
        validation = null;
        document = null;
        missingRanges = System.Array.Empty<AvifByteRange>();
        sourceLoaded = false;
        parsed = false;
        closed = false;
        bool validSource = requestedSource != null
            || requestedStream != null && requestedStream.CanRead
            || requestedRandomAccessInput != null
                && requestedRandomAccessInput.Code
                    == AvifResultCode.Success;
        if (!validSource
            || AvifOneCallCodec.ValidateDecoderOptions(options)
                != AvifResultCode.Success)
        {
            code = requestedRandomAccessInput != null
                    && requestedRandomAccessInput.Code
                        != AvifResultCode.Success
                ? requestedRandomAccessInput.Code
                : AvifResultCode.InvalidArgument;
            initializationCode = code;
            return;
        }
        if ((options ?? throw new InvalidOperationException("Invalid internal state.")).Cancellation.IsCancellationRequested())
        {
            code = AvifResultCode.Cancelled;
            initializationCode = code;
            return;
        }
        if (requestedSource != null
            && requestedSource.LongLength > options.Limits.MaxSourceBytes)
        {
            code = AvifResultCode.LimitExceeded;
            initializationCode = code;
            return;
        }
        if (requestedSource != null)
        {
            source = (byte[])requestedSource.Clone();
            sourceLoaded = true;
        }
        code = AvifResultCode.Success;
        initializationCode = code;
    }

    AvifValidationOptions CreateValidationOptions()
    {
        AvifValidationOptions result = new AvifValidationOptions();
        result.Mode = (options ?? throw new InvalidOperationException("Invalid internal state.")).ValidationMode;
        result.ValidateEncodedPayload = true;
        result.DecodePixelData = false;
        result.Limits = options.Limits;
        return result;
    }

    AvifDecodeResult DecodeAuxiliaryItemInternal(
        uint masterItemId,
        int auxiliaryIndex)
    {
        AvifAuxiliaryImage? association = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAuxiliaryImage(masterItemId, auxiliaryIndex);
        if (association == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        AvifDecodeResult result = DecodeItemInternal(
            association.GetItemId(),
            new List<uint>(),
            0);
        if (result.GetCode() == AvifResultCode.Success
            && result.GetImage() != null)
        {
            result.SetItemSelection(association.GetItemId());
            result.AddAuxiliaryOutput(new AvifDecodedAuxiliary(
                association.GetKind(),
                association.GetTypeName(),
                association.GetItemId(),
                masterItemId,
                0,
                0,
                association.IsPremultiplied(),
                result.GetImage()
                    ?? throw new InvalidOperationException("Invalid internal state.")));
        }
        return result;
    }

    AvifDecodeResult DecodeAuxiliaryKind(
        uint masterItemId,
        AvifAuxiliaryKind kind)
    {
        AvifResultCode state = RequireParsed();
        if (state != AvifResultCode.Success)
        {
            return FailDecode(state);
        }
        int index = 0;
        while (index < (document ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAuxiliaryImageCount(masterItemId))
        {
            AvifAuxiliaryImage? association = document.GetAuxiliaryImage(
                masterItemId,
                index);
            if (association != null && association.GetKind() == kind)
            {
                return DecodeAuxiliaryItemInternal(masterItemId, index);
            }
            index++;
        }
        code = AvifResultCode.NotFound;
        return FailDecode(code);
    }

    AvifDecodeResult DecodeToneMapInternal(
        uint toneMapItemId,
        bool includeBase)
    {
        uint[]? dependencies = GetDerivedDependencies(toneMapItemId);
        AvifImageItem? toneMapItem = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(toneMapItemId);
        if (toneMapItem == null || toneMapItem.GetItemType() != "tmap"
            || dependencies == null || dependencies.Length < 2)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        byte[] metadataBytes = ExtractItem(toneMapItemId, false);
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(metadataBytes);
        if (metadata.GetCode() != AvifResultCode.Success)
        {
            code = metadata.GetCode();
            return FailDecode(code);
        }
        AvifDecodeResult decodedGain = DecodeItemInternal(
            dependencies[1],
            new List<uint>(),
            0);
        AvifImage? gainImage = decodedGain.GetImage();
        if (decodedGain.GetCode() != AvifResultCode.Success
            || gainImage == null)
        {
            code = decodedGain.GetCode();
            return FailDecode(code);
        }
        AvifGainMap gainMap = new AvifGainMap(gainImage, metadata, null);
        if (gainMap.GetCode() != AvifResultCode.Success)
        {
            gainImage.Close();
            code = gainMap.GetCode();
            return FailDecode(code);
        }
        if (!includeBase)
        {
            AvifDecodeResult gainResult = new AvifDecodeResult(
                AvifResultCode.Success,
                gainImage,
                document);
            gainResult.SetItemSelection(toneMapItemId);
            gainResult.SetGainMapOutput(gainMap);
            code = AvifResultCode.Success;
            return gainResult;
        }
        AvifDecodeResult decodedBase = DecodeItemInternal(
            dependencies[0],
            new List<uint>(),
            0);
        AvifImage? baseImage = decodedBase.GetImage();
        if (decodedBase.GetCode() != AvifResultCode.Success
            || baseImage == null)
        {
            gainImage.Close();
            code = decodedBase.GetCode();
            return FailDecode(code);
        }
        AvifToneMap toneMap = new AvifToneMap(
            baseImage,
            gainMap,
            IsToneMapPreferred(toneMapItemId, dependencies[0]));
        if (toneMap.GetCode() != AvifResultCode.Success)
        {
            baseImage.Close();
            gainImage.Close();
            code = toneMap.GetCode();
            return FailDecode(code);
        }
        AvifDecodeResult toneMapResult = new AvifDecodeResult(
            AvifResultCode.Success,
            baseImage,
            document);
        toneMapResult.SetItemSelection(toneMapItemId);
        toneMapResult.SetGainMapOutput(gainMap);
        toneMapResult.SetToneMapOutput(toneMap);
        PopulateMetadataOutputs(toneMapResult, toneMapItemId);
        code = AvifResultCode.Success;
        return toneMapResult;
    }

    void PopulateItemOutputs(AvifDecodeResult result, uint itemId)
    {
        PopulateAuxiliaryOutputs(result, itemId);
        PopulateMetadataOutputs(result, itemId);
        PopulateProgressiveOutputs(result, itemId);
        PopulateToneMapOutput(result, itemId);
        code = result.GetCode();
    }

    void PopulateAuxiliaryOutputs(
        AvifDecodeResult result,
        uint itemId)
    {
        int index = 0;
        int count = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAuxiliaryImageCount(itemId);
        while (index < count)
        {
            AvifDecodeResult auxiliary = DecodeAuxiliaryItemInternal(
                itemId,
                index);
            AvifDecodedAuxiliary? output = auxiliary.GetDecodedAuxiliary(0);
            if (auxiliary.GetCode() == AvifResultCode.Success
                && output != null)
            {
                result.AddAuxiliaryOutput(output);
            }
            index++;
        }
    }

    void PopulateMetadataOutputs(
        AvifDecodeResult result,
        uint itemId)
    {
        int index = 0;
        AvifMetadata? value = FindAssociatedMetadata(itemId, index);
        while (value != null)
        {
            result.AddMetadataOutput(value);
            index++;
            value = FindAssociatedMetadata(itemId, index);
        }
    }

    void PopulateProgressiveOutputs(
        AvifDecodeResult result,
        uint itemId)
    {
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        ParsedAv1LayeredImageIndex? layers = FindLayerIndex(item);
        if (layers == null || layers.GetLayerCount() < 2)
        {
            return;
        }
        int index = 0;
        while (index < layers.GetLayerCount())
        {
            AvifDecodeResult layer = DecodeItemLayer(itemId, index);
            if (layer.GetCode() == AvifResultCode.Success
                && layer.GetImage() != null)
            {
                result.AddProgressiveLayerOutput(
                    layer.GetImage()
                        ?? throw new InvalidOperationException("Invalid internal state."));
            }
            index++;
        }
    }

    void PopulateToneMapOutput(
        AvifDecodeResult result,
        uint baseItemId)
    {
        uint toneMapItemId = FindToneMapAlternative(baseItemId);
        AvifImage? baseImage = result.GetImage();
        if (toneMapItemId == 0 || baseImage == null)
        {
            return;
        }
        uint[]? dependencies = GetDerivedDependencies(toneMapItemId);
        byte[] metadataBytes = ExtractItem(toneMapItemId, false);
        AvifGainMapMetadata metadata = new AvifGainMapMetadata(metadataBytes);
        if (dependencies == null || dependencies.Length < 2
            || metadata.GetCode() != AvifResultCode.Success)
        {
            return;
        }
        AvifDecodeResult decodedGain = DecodeItemInternal(
            dependencies[1],
            new List<uint>(),
            0);
        AvifImage? gainImage = decodedGain.GetImage();
        if (decodedGain.GetCode() != AvifResultCode.Success
            || gainImage == null)
        {
            return;
        }
        AvifGainMap gainMap = new AvifGainMap(gainImage, metadata, null);
        AvifToneMap toneMap = new AvifToneMap(
            baseImage,
            gainMap,
            IsToneMapPreferred(toneMapItemId, baseItemId));
        if (gainMap.GetCode() == AvifResultCode.Success
            && toneMap.GetCode() == AvifResultCode.Success)
        {
            result.SetGainMapOutput(gainMap);
            result.SetToneMapOutput(toneMap);
        }
        else
        {
            gainImage.Close();
        }
    }

    void PopulateSequenceOutputs(
        AvifDecodeResult result,
        AvifTrack master,
        int frameIndex)
    {
        AvifMovie? movie = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .GetMovie();
        if (movie == null)
        {
            return;
        }
        int index = 0;
        while (index < movie.GetAuxiliaryTrackCount(master.GetTrackId()))
        {
            AvifTrack? auxiliary = movie.GetAuxiliaryTrack(
                master.GetTrackId(),
                index);
            if (auxiliary != null)
            {
                AvifDecodeResult decoded = DecodeTrackFrame(
                    auxiliary,
                    frameIndex);
                if (decoded.GetCode() == AvifResultCode.Success
                    && decoded.GetImage() != null)
                {
                    result.AddAuxiliaryOutput(new AvifDecodedAuxiliary(
                        auxiliary.GetAuxiliaryKind(),
                        auxiliary.GetAuxiliaryType(),
                        0,
                        0,
                        auxiliary.GetTrackId(),
                        master.GetTrackId(),
                        false,
                        decoded.GetImage()
                            ?? throw new InvalidOperationException("Invalid internal state.")));
                }
            }
            index++;
        }
    }

    AvifDecodeResult DecodeTrackFrame(
        AvifTrack track,
        int frameIndex)
    {
        AvifSample? sample = track.GetSample(frameIndex);
        if (sample == null || sample.GetOffset() > int.MaxValue
            || sample.GetSize() > int.MaxValue
            || sample.GetOffset() > (ulong)(source ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || sample.GetSize() > (ulong)source.Length - sample.GetOffset())
        {
            code = AvifResultCode.InvalidTrack;
            return FailDecode(code);
        }
        AvifImage? image = AvifOneCallCodec.DecodeManagedPayload(
            source.AsSpan((int)sample.GetOffset(), (int)sample.GetSize()),
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        if (image == null)
        {
            code = AvifResultCode.UnsupportedFeature;
            return FailDecode(code);
        }
        code = image.GetCode();
        if (code == AvifResultCode.Success)
        {
            List<ParsedColourInformation>? colors = FindTrackColorInformation(
                track.GetTrackId(),
                sample.GetDescriptionIndex());
            code = colors == null
                ? AvifResultCode.InvalidTrack
                : ColorMetadataResolver.Apply(image, colors, options.Limits);
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return FailDecode(code);
        }
        return new AvifDecodeResult(code, image, document);
    }

    AvifMetadata? FindAssociatedMetadata(
        uint itemId,
        int requestedIndex)
    {
        if (requestedIndex < 0)
        {
            return null;
        }
        int foundIndex = 0;
        int itemIndex = 0;
        while (itemIndex < (document ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            AvifImageItem? candidate = document.GetItem(itemIndex);
            if ((candidate ?? throw new InvalidOperationException("Invalid internal state.")).IsMetadataItem()
                && DescribesItem(candidate, itemId))
            {
                if (foundIndex == requestedIndex)
                {
                    byte[] payload = ExtractItem(
                        candidate.GetItemId(),
                        false);
                    if (candidate.GetItemType() == "Exif")
                    {
                        return AvifMetadata.Create(
                            AvifMetadataKind.Exif,
                            payload);
                    }
                    if (candidate.GetItemType() == "mime"
                        && candidate.GetContentType()
                            == "application/rdf+xml")
                    {
                        return AvifMetadata.Create(
                            AvifMetadataKind.Xmp,
                            payload);
                    }
                    return new AvifMetadata(
                        candidate.GetItemType(),
                        candidate.GetContentType(),
                        payload);
                }
                foundIndex++;
            }
            itemIndex++;
        }
        return null;
    }

    static bool DescribesItem(
        AvifImageItem metadata,
        uint itemId)
    {
        int referenceIndex = 0;
        while (referenceIndex < metadata.GetOutgoingReferenceCount())
        {
            AvifItemReference? reference = metadata.GetOutgoingReference(
                referenceIndex);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind()
                == AvifItemReferenceKind.ContentDescription)
            {
                int targetIndex = 0;
                while (targetIndex < reference.GetTargetCount())
                {
                    if (reference.GetTargetItemId(targetIndex) == itemId)
                    {
                        return true;
                    }
                    targetIndex++;
                }
            }
            referenceIndex++;
        }
        return false;
    }

    uint[]? GetDerivedDependencies(uint itemId)
    {
        AvifImageItem? item = (document ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId);
        if (item == null)
        {
            return null;
        }
        int referenceIndex = 0;
        while (referenceIndex < item.GetOutgoingReferenceCount())
        {
            AvifItemReference? reference = item.GetOutgoingReference(
                referenceIndex);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind()
                == AvifItemReferenceKind.DerivedImage)
            {
                uint[] result = new uint[reference.GetTargetCount()];
                int targetIndex = 0;
                while (targetIndex < result.Length)
                {
                    result[targetIndex] = reference.GetTargetItemId(
                        targetIndex);
                    targetIndex++;
                }
                return result;
            }
            referenceIndex++;
        }
        return null;
    }

    uint FindToneMapAlternative(uint baseItemId)
    {
        int index = 0;
        while (index < (document ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            AvifImageItem? item = document.GetItem(index);
            if ((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType() == "tmap")
            {
                uint[]? dependencies = GetDerivedDependencies(
                    item.GetItemId());
                if (dependencies != null && dependencies.Length >= 2
                    && dependencies[0] == baseItemId)
                {
                    return item.GetItemId();
                }
            }
            index++;
        }
        return 0;
    }

    bool IsToneMapPreferred(
        uint toneMapItemId,
        uint baseItemId)
    {
        int index = 0;
        while (index < (document ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupCount())
        {
            AvifEntityGroup? group = document.GetGroup(index);
            if ((group ?? throw new InvalidOperationException("Invalid internal state.")).GetKind()
                == AvifEntityGroupKind.Alternate)
            {
                int toneMapIndex = -1;
                int baseIndex = -1;
                int entityIndex = 0;
                while (entityIndex < group.GetEntityCount())
                {
                    uint entityId = group.GetEntityId(entityIndex);
                    if (entityId == toneMapItemId)
                    {
                        toneMapIndex = entityIndex;
                    }
                    if (entityId == baseItemId)
                    {
                        baseIndex = entityIndex;
                    }
                    entityIndex++;
                }
                if (toneMapIndex >= 0 && baseIndex >= 0)
                {
                    return toneMapIndex < baseIndex;
                }
            }
            index++;
        }
        return false;
    }

    AvifResultCode RequireParsed()
    {
        if (closed)
        {
            code = AvifResultCode.Closed;
            return code;
        }
        if (!parsed)
        {
            code = AvifResultCode.InvalidState;
            return code;
        }
        if ((document ?? throw new InvalidOperationException("Invalid internal state.")).GetCode() != AvifResultCode.Success)
        {
            code = document.GetCode();
            return code;
        }
        if ((options ?? throw new InvalidOperationException("Invalid internal state.")).Cancellation.IsCancellationRequested())
        {
            code = AvifResultCode.Cancelled;
            return code;
        }
        return AvifResultCode.Success;
    }

    AvifDecodeResult DecodeItemInternal(
        uint itemId,
        List<uint> ancestors,
        int depth)
    {
        if (depth > (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxDependencyDepth)
        {
            code = AvifResultCode.LimitExceeded;
            return FailDecode(code);
        }
        if (Contains(ancestors, itemId))
        {
            code = AvifResultCode.InvalidItem;
            return FailDecode(code);
        }
        ParsedItem? item = (validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null
            ? null : (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        if (item == null)
        {
            code = AvifResultCode.NotFound;
            return FailDecode(code);
        }
        if (item.GetProtectionIndex() != 0)
        {
            code = AvifResultCode.UnsupportedProtection;
            return FailDecode(code);
        }
        if (item.GetItemType() == "av01")
        {
            return DecodeCodedItem(item);
        }
        if (item.GetItemType() == "grid")
        {
            ancestors.Add(itemId);
            AvifDecodeResult result = DecodeGridItem(item, ancestors, depth + 1);
            ancestors.RemoveAt(ancestors.Count - 1);
            return result;
        }
        code = AvifResultCode.UnsupportedFeature;
        return FailDecode(code);
    }

    AvifDecodeResult DecodeCodedItem(ParsedItem item)
    {
        EncodedItemExtractionResult extraction = ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(
            source,
            item.GetItemId());
        if (extraction.Code != AvifResultCode.Success || extraction.Data == null)
        {
            code = extraction.Code;
            return FailDecode(code);
        }
        int payloadLength = extraction.Data.Length;
        ParsedLayerSelector? selector = FindLayerSelector(item);
        ParsedAv1LayeredImageIndex? layers = FindLayerIndex(item);
        if (selector != null
            && !selector.AllowsProgressiveRendering()
            && layers != null)
        {
            payloadLength = GetLayerPrefixLength(
                layers,
                selector.GetLayerId(),
                extraction.Data.Length);
        }
        AvifImage? image = AvifOneCallCodec.DecodeManagedPayload(
            payloadLength < 0
                ? ReadOnlySpan<byte>.Empty
                : extraction.Data.GetSpan().Slice(0, payloadLength),
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        extraction.Data.Close();
        if (image == null)
        {
            code = AvifResultCode.UnsupportedFeature;
            return FailDecode(code);
        }
        code = image.GetCode();
        if (code == AvifResultCode.Success)
        {
            code = ColorMetadataResolver.Apply(image, item, options.Limits);
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return FailDecode(code);
        }
        return new AvifDecodeResult(code, image, document);
    }

    AvifDecodeResult DecodeGridItem(
        ParsedItem item,
        List<uint> ancestors,
        int depth)
    {
        uint[]? dependencies = GetGridDependencies(item);
        byte[] payload = ExtractItem(item.GetItemId(), false);
        if (dependencies == null || payload.Length == 0)
        {
            if (code == AvifResultCode.Success)
            {
                code = AvifResultCode.InvalidItem;
            }
            return FailDecode(code);
        }
        AvifGrid grid = AvifGrid.Create(payload, dependencies, (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        if (grid.GetCode() != AvifResultCode.Success)
        {
            code = grid.GetCode();
            return FailDecode(code);
        }

        List<AvifImage> decoded = new List<AvifImage>();
        List<uint> decodedIds = new List<uint>();
        int index = 0;
        while (index < dependencies.Length)
        {
            uint dependency = dependencies[index];
            if (!Contains(decodedIds, dependency))
            {
                AvifDecodeResult cell = DecodeItemInternal(
                    dependency,
                    ancestors,
                    depth);
                if (cell.GetCode() != AvifResultCode.Success
                    || cell.GetImage() == null)
                {
                    CloseImages(decoded);
                    code = cell.GetCode();
                    return FailDecode(code);
                }
                AvifResultCode attach = grid.SetDecodedItem(
                    dependency,
                    cell.GetImage());
                if (attach != AvifResultCode.Success)
                {
                    (cell.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
                    CloseImages(decoded);
                    code = attach;
                    return FailDecode(code);
                }
                decoded.Add(cell.GetImage()
                    ?? throw new InvalidOperationException("Invalid internal state."));
                decodedIds.Add(dependency);
            }
            index++;
        }
        AvifImage rendered = grid.Render();
        CloseImages(decoded);
        code = rendered.GetCode();
        if (code == AvifResultCode.Success)
        {
            code = ColorMetadataResolver.Apply(rendered, item, options.Limits);
        }
        if (code != AvifResultCode.Success)
        {
            rendered.Close();
            return FailDecode(code);
        }
        return new AvifDecodeResult(code, rendered, document);
    }

    List<ParsedColourInformation>? FindTrackColorInformation(
        uint trackId,
        int descriptionIndex)
    {
        if ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Movie == null || descriptionIndex < 1)
        {
            return null;
        }
        int index = 0;
        while (index < (validation.Movie ?? throw new InvalidOperationException("Invalid internal state.")).Tracks.Count)
        {
            ParsedMovieTrack track = validation.Movie.Tracks[index];
            if (track.TrackId == trackId)
            {
                int zeroBasedIndex = descriptionIndex - 1;
                if (zeroBasedIndex >= track.SampleTable.Descriptions.Count)
                {
                    return null;
                }
                return track.SampleTable.Descriptions[zeroBasedIndex]
                    .ColourInformation;
            }
            index++;
        }
        return null;
    }

    byte[] ExtractItem(uint itemId, bool argumentCheck)
    {
        if (argumentCheck && itemId == 0)
        {
            code = AvifResultCode.InvalidArgument;
            return System.Array.Empty<byte>();
        }
        if ((validation ?? throw new InvalidOperationException("Invalid internal state.")).Meta == null)
        {
            code = AvifResultCode.NotFound;
            return System.Array.Empty<byte>();
        }
        EncodedItemExtractionResult extraction = (validation.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(
            source,
            itemId);
        code = extraction.Code;
        if (code != AvifResultCode.Success || extraction.Data == null)
        {
            return System.Array.Empty<byte>();
        }
        byte[] result = extraction.Data.GetSpan().ToArray();
        extraction.Data.Close();
        return result;
    }

    uint[]? GetGridDependencies(ParsedItem? item)
    {
        if (item == null || item.GetItemType() != "grid")
        {
            return null;
        }
        List<uint> ids = new List<uint>();
        int referenceIndex = 0;
        while (referenceIndex < item.GetOutgoingReferenceCount())
        {
            ParsedItemReference? reference = item.GetOutgoingReference(referenceIndex);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "dimg")
            {
                int targetIndex = 0;
                while (targetIndex < reference.GetTargetCount())
                {
                    ids.Add(reference.GetTargetItemId(targetIndex));
                    targetIndex++;
                }
            }
            referenceIndex++;
        }
        return ids.Count == 0 ? null : ids.ToArray();
    }

    static ParsedAv1LayeredImageIndex? FindLayerIndex(ParsedItem? item)
    {
        if (item == null)
        {
            return null;
        }
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedAv1LayeredImageIndex? layers = (item.GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetLayeredImageIndex();
            if (layers != null)
            {
                return layers;
            }
            index++;
        }
        return null;
    }

    static ParsedLayerSelector? FindLayerSelector(ParsedItem item)
    {
        if (item == null)
        {
            return null;
        }
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedLayerSelector? selector = (item.GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetLayerSelector();
            if (selector != null)
            {
                return selector;
            }
            index++;
        }
        return null;
    }

    static int GetLayerPrefixLength(
        ParsedAv1LayeredImageIndex layers,
        int layerIndex,
        int totalLength)
    {
        if (layerIndex == layers.GetLayerCount() - 1)
        {
            return totalLength;
        }
        ulong length = 0;
        int index = 0;
        while (index <= layerIndex)
        {
            length += layers.GetLayerSize(index);
            if (length > (ulong)totalLength)
            {
                return -1;
            }
            index++;
        }
        return (int)length;
    }

    static bool Contains(List<uint> values, uint value)
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

    static void CloseImages(List<AvifImage> images)
    {
        int index = 0;
        while (index < images.Count)
        {
            images[index].Close();
            index++;
        }
    }

    AvifDecodeResult FailDecode(AvifResultCode resultCode)
    {
        code = resultCode;
        return new AvifDecodeResult(resultCode, null, document);
    }

    internal static AvifDecoderOptions? CopyOptions(AvifDecoderOptions? source)
    {
        if (source == null)
        {
            return null;
        }
        AvifDecoderOptions copy = new AvifDecoderOptions();
        copy.ValidationMode = source.ValidationMode;
        copy.SourceSelection = source.SourceSelection;
        copy.ContentSelection = source.ContentSelection;
        copy.AllowProgressive = source.AllowProgressive;
        copy.AllowIncremental = source.AllowIncremental;
        copy.ApplyFilmGrain = source.ApplyFilmGrain;
        copy.OperatingPoint = source.OperatingPoint;
        copy.WorkerCount = source.WorkerCount;
        copy.RenderOptions = source.RenderOptions;
        copy.Limits = ResourceGuard.CopyLimits(source.Limits);
        copy.Cancellation = source.Cancellation;
        return copy;
    }
}
