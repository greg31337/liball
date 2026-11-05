using System;
using System.Buffers.Binary;

namespace avif;

/// <summary>Accepts contiguous or out-of-order AVIF ranges and exposes bounded availability.</summary>
/// <remarks>
/// Appended bytes are copied. Returned documents and decoded images remain valid independently of
/// later appends, completion, or closure; decoded images are caller-owned.
/// </remarks>
public class AvifIncrementalReader : IAvifIncrementalReader, IAvifDiagnosticSource
{
    IncrementalInput? input;
    AvifDecoderOptions? options;
    AvifContainerValidationResult? validation;
    AvifDocument? document;
    AvifByteRange[]? missingRanges;
    AvifResultCode code;
    int decodedRowCount;
    bool parsed;
    bool closed;

    /// <summary>Creates an incremental reader with default decode options and source limits.</summary>
    /// <returns>The initialized incremental reader.</returns>
    internal static AvifIncrementalReader Create()
    {
        return new AvifIncrementalReader(new AvifDecoderOptions());
    }

    /// <summary>Creates an incremental reader with caller-selected decode options.</summary>
    /// <param name="options">The options to snapshot, including source and decode limits.</param>
    public AvifIncrementalReader(AvifDecoderOptions? options)
    {
        Initialize(options);
    }

    /// <summary>Creates an incremental reader with caller-selected options.</summary>
    /// <returns>The initialized incremental reader.</returns>
    internal static AvifIncrementalReader Create(AvifDecoderOptions? options)
    {
        return new AvifIncrementalReader(options);
    }

    /// <summary>Returns the outcome of the most recent incremental operation.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return closed ? AvifResultCode.Closed : code;
    }

    /// <summary>Returns diagnostics for the current incremental parse or decode state.</summary>
    /// <returns>The structured incremental-reader diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            document,
            GetCode(),
            AvifDiagnosticCategory.Input,
            "The incremental AVIF operation did not complete successfully.");
    }

    /// <summary>Copies another contiguous source chunk into the bounded input buffer.</summary>
    /// <param name="source">The next bytes in source order.</param>
    /// <returns>Success, LimitExceeded, InvalidState after completion, or Closed.</returns>
    public AvifResultCode Append(ReadOnlySpan<byte> source)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (code == AvifResultCode.InvalidArgument
            || code == AvifResultCode.LimitExceeded)
        {
            return code;
        }
        AvifResultCode result = (input ?? throw new InvalidOperationException("Invalid internal state.")).Append(source);
        code = result;
        if (result == AvifResultCode.Success && source.Length != 0)
        {
            document = null;
            validation = null;
            missingRanges = System.Array.Empty<AvifByteRange>();
            decodedRowCount = 0;
            parsed = false;
        }
        return result;
    }

    /// <summary>Copies a bounded source range at its absolute offset.</summary>
    /// <param name="offset">The nonnegative absolute source offset.</param>
    /// <param name="source">The bytes covering that range.</param>
    /// <returns>Success, InvalidData for conflicting overlap, LimitExceeded, InvalidState, or Closed.</returns>
    public AvifResultCode AddRange(
        long offset,
        ReadOnlySpan<byte> source)
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if ((input ?? throw new InvalidOperationException("Invalid internal state.")).Code
            != AvifResultCode.Success)
        {
            code = input.Code;
            return code;
        }
        AvifResultCode result = input.AddRange(offset, source);
        code = result;
        if (result == AvifResultCode.Success && source.Length != 0)
        {
            InvalidateAvailableState();
        }
        return result;
    }

    /// <summary>Parses all currently available bytes or reports the next required range.</summary>
    /// <returns>Success, NeedMoreData, or a final parse result after completion.</returns>
    public AvifResultCode ParseAvailable()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (code == AvifResultCode.InvalidArgument
            || code == AvifResultCode.LimitExceeded)
        {
            return code;
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

        ReadOnlySpan<byte> available = (input ?? throw new InvalidOperationException("Invalid internal state.")).Buffer.AsSpan(
            0,
            input.ContiguousLength);
        AvifProbeResult probe = Avif.Probe(available);
        if (probe.GetCode() == AvifResultCode.NeedMoreData)
        {
            return WaitForMore();
        }
        if (probe.GetCode() != AvifResultCode.Success)
        {
            code = probe.GetCode();
            missingRanges = System.Array.Empty<AvifByteRange>();
            return code;
        }

        AvifValidationOptions validationOptions = CreateValidationOptions();
        validation = AvifContainerValidator.Validate(
            available,
            validationOptions);
        document = AvifDocumentBuilder.Create(available, validation);
        if (options.Cancellation.IsCancellationRequested())
        {
            document = null;
            validation = null;
            code = AvifResultCode.Cancelled;
            missingRanges = System.Array.Empty<AvifByteRange>();
            return code;
        }
        code = document.GetCode();
        if (code == AvifResultCode.Success)
        {
            parsed = true;
            missingRanges = System.Array.Empty<AvifByteRange>();
            return code;
        }
        if (!input.IsComplete && CouldBeIncomplete(code))
        {
            document = null;
            validation = null;
            return WaitForMore();
        }
        missingRanges = System.Array.Empty<AvifByteRange>();
        return code;
    }

    /// <summary>Decodes the selected image when enough source bytes are available.</summary>
    /// <returns>A caller-owned image result, or NeedMoreData while waiting.</returns>
    public AvifDecodeResult DecodeAvailable()
    {
        AvifResultCode parseCode = ParseAvailable();
        if (parseCode != AvifResultCode.Success)
        {
            decodedRowCount = 0;
            return new AvifDecodeResult(parseCode, null, document);
        }
        if ((options ?? throw new InvalidOperationException("Invalid internal state.")).Cancellation.IsCancellationRequested())
        {
            code = AvifResultCode.Cancelled;
            decodedRowCount = 0;
            return new AvifDecodeResult(code, null, document);
        }

        AvifDecodeResult decoded = AvifOneCallCodec.Decode(
            (input ?? throw new InvalidOperationException("Invalid internal state.")).Buffer.AsSpan(0, input.Length),
            options);
        code = decoded.GetCode();
        if (code == AvifResultCode.Success && decoded.GetImage() != null)
        {
            decodedRowCount = (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight();
            missingRanges = System.Array.Empty<AvifByteRange>();
        }
        else
        {
            decodedRowCount = 0;
            if (code == AvifResultCode.TruncatedData && !input.IsComplete)
            {
                code = WaitForMore();
                return new AvifDecodeResult(code, null, document);
            }
        }
        return decoded;
    }

    /// <summary>Returns the parsed document after a successful available parse.</summary>
    /// <returns>The document, or null while waiting, after invalidation, or after closing.</returns>
    public AvifDocument? GetDocument()
    {
        return closed || !parsed ? null : document;
    }

    /// <summary>Reports whether container parsing has completed successfully.</summary>
    /// <returns><c>true</c> when the current retained ranges produced a document.</returns>
    public bool IsParseAvailable()
    {
        return !closed && parsed && document != null
            && document.GetCode() == AvifResultCode.Success;
    }

    /// <summary>Reports whether one parsed item's complete resolved payload is retained.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns><c>true</c> when the item and every resolved extent are available.</returns>
    public bool IsItemAvailable(uint itemId)
    {
        AvifImageItem? item = !IsParseAvailable()
            ? null : (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        if (item == null || !item.HasLocation())
        {
            return false;
        }
        int index = 0;
        while (index < item.GetExtentCount())
        {
            AvifItemExtent? extent = item.GetExtent(index);
            if (extent == null || !extent.IsResolved()
                || !(input ?? throw new InvalidOperationException("Invalid internal state.")).IsRangeAvailable(
                    extent.GetResolvedOffset(),
                    extent.GetResolvedLength()))
            {
                return false;
            }
            index++;
        }
        return item.GetExtentCount() > 0;
    }

    /// <summary>Reports whether one selected sequence frame's complete encoded sample is retained.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns><c>true</c> when the sample range is available.</returns>
    public bool IsFrameAvailable(int frameIndex)
    {
        if (!IsParseAvailable())
        {
            return false;
        }
        AvifImageSource? imageSource =
            (document ?? throw new InvalidOperationException("Invalid internal state.")).SelectSource(
                AvifSourceSelection.ImageSequence);
        AvifTrack? track = imageSource == null
            ? null : imageSource.GetSequenceTrack();
        AvifSample? sample = track == null
            ? null : track.GetSample(frameIndex);
        return sample != null
            && sample.GetOffset() <= long.MaxValue
            && (input ?? throw new InvalidOperationException("Invalid internal state.")).IsRangeAvailable(
                (long)sample.GetOffset(),
                sample.GetSize());
    }

    /// <summary>Reports whether one grid cell's encoded dependency item is retained.</summary>
    /// <param name="itemId">The grid item identifier.</param>
    /// <param name="row">The zero-based grid row.</param>
    /// <param name="column">The zero-based grid column.</param>
    /// <returns><c>true</c> when the cell coordinates and dependency payload are available.</returns>
    public bool IsTileAvailable(
        uint itemId,
        int row,
        int column)
    {
        if (!IsParseAvailable() || validation == null
            || validation.Meta == null)
        {
            return false;
        }
        ParsedItem? item = validation.Meta.FindItem(itemId);
        uint[]? dependencies = GetGridDependencies(item);
        if (item == null || item.GetItemType() != "grid"
            || dependencies == null)
        {
            return false;
        }
        EncodedItemExtractionResult extraction = validation.Meta.ExtractEncodedItem(
            (input ?? throw new InvalidOperationException("Invalid internal state.")).Buffer.AsSpan(0, input.Length),
            itemId);
        if (extraction.Code != AvifResultCode.Success
            || extraction.Data == null)
        {
            return false;
        }
        byte[] payload = extraction.Data.GetSpan().ToArray();
        extraction.Data.Close();
        GridItemParseResult grid = GridItemParser.Parse(
            payload,
            dependencies.Length,
            (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits);
        if (grid.Code != AvifResultCode.Success
            || row < 0 || column < 0
            || row >= grid.Rows || column >= grid.Columns)
        {
            return false;
        }
        return IsItemAvailable(
            dependencies[row * grid.Columns + column]);
    }

    /// <summary>Reports whether one progressive primary-item layer is retained.</summary>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns><c>true</c> when the layer exists and its coded item payload is available.</returns>
    public bool IsLayerAvailable(int layerIndex)
    {
        if (!IsParseAvailable() || validation == null
            || validation.Meta == null)
        {
            return false;
        }
        AvifFileInfo? info = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();
        ParsedItem? item = info == null || !info.HasPrimaryItem()
            ? null : validation.Meta.FindItem(info.GetPrimaryItemId());
        ParsedAv1LayeredImageIndex? layers = FindLayerIndex(item);
        return layers != null && layerIndex >= 0
            && layerIndex < layers.GetLayerCount()
            && IsItemAvailable(
                (item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
    }

    /// <summary>Returns the number of contiguous rows produced by the most recent decode.</summary>
    /// <returns>Zero while waiting; otherwise the decoded image height.</returns>
    public int GetDecodedRowCount()
    {
        return closed ? 0 : decodedRowCount;
    }

    /// <summary>Returns source ranges required by the most recent waiting operation.</summary>
    /// <returns>An independent range array, empty when no range is pending.</returns>
    public AvifByteRange[] GetMissingRanges()
    {
        AvifByteRange[] result = new AvifByteRange[(missingRanges ?? throw new InvalidOperationException("Invalid internal state.")).Length];
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

    /// <summary>Returns the number of source bytes retained so far.</summary>
    /// <returns>The retained byte count, or zero after closing.</returns>
    public int GetReceivedByteCount()
    {
        return closed ? 0 : (input ?? throw new InvalidOperationException("Invalid internal state.")).ReceivedByteCount;
    }

    /// <summary>Returns the retained source prefix length beginning at offset zero.</summary>
    /// <returns>The contiguous byte count, or zero after closing.</returns>
    public int GetContiguousByteCount()
    {
        return closed ? 0 : (input ?? throw new InvalidOperationException("Invalid internal state.")).ContiguousLength;
    }

    /// <summary>Reports whether the caller declared that no more source bytes will arrive.</summary>
    /// <returns><c>true</c> after completion and before closure.</returns>
    public bool IsComplete()
    {
        return !closed && (input ?? throw new InvalidOperationException("Invalid internal state.")).IsComplete;
    }

    /// <summary>Declares the current source bytes complete and rejects future appends.</summary>
    /// <returns>Success, or Closed after cleanup.</returns>
    public AvifResultCode Complete()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        AvifResultCode result = (input ?? throw new InvalidOperationException("Invalid internal state.")).CompleteInput();
        code = result;
        if (result == AvifResultCode.Success)
        {
            missingRanges = System.Array.Empty<AvifByteRange>();
        }
        return result;
    }

    /// <summary>Releases all retained source, document, range, and progress state.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (closed)
        {
            return AvifResultCode.Success;
        }
        (input ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        document = null;
        validation = null;
        missingRanges = System.Array.Empty<AvifByteRange>();
        decodedRowCount = 0;
        parsed = false;
        closed = true;
        code = AvifResultCode.Closed;
        return AvifResultCode.Success;
    }

    void Initialize(AvifDecoderOptions? requestedOptions)
    {
        options = AvifReader.CopyOptions(requestedOptions);
        missingRanges = System.Array.Empty<AvifByteRange>();
        document = null;
        validation = null;
        decodedRowCount = 0;
        parsed = false;
        closed = false;
        code = AvifOneCallCodec.ValidateDecoderOptions(options);
        long maximum = code == AvifResultCode.Success
            ? (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxSourceBytes
            : 0;
        int initialCapacity = maximum < 4096 ? (int)maximum : 4096;
        input = new IncrementalInput(
            initialCapacity,
            maximum,
            code == AvifResultCode.Success
                ? (options ?? throw new InvalidOperationException("Invalid internal state.")).Limits.MaxBoxCount
                : 1);
        if (code == AvifResultCode.Success
            && input.Code != AvifResultCode.Success)
        {
            code = input.Code;
        }
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

    AvifResultCode WaitForMore()
    {
        if ((input ?? throw new InvalidOperationException("Invalid internal state.")).IsComplete)
        {
            code = AvifResultCode.TruncatedData;
            missingRanges = System.Array.Empty<AvifByteRange>();
            return code;
        }
        missingRanges = FindMissingRanges();
        code = AvifResultCode.NeedMoreData;
        return code;
    }

    AvifByteRange[] FindMissingRanges()
    {
        AvifByteRange[] coverageGaps = FindCoverageGaps();
        if (coverageGaps.Length != 0)
        {
            return coverageGaps;
        }
        ReadOnlySpan<byte> source = (input ?? throw new InvalidOperationException("Invalid internal state.")).Buffer.AsSpan(
            0,
            input.ContiguousLength);
        if (source.Length < 8)
        {
            return new AvifByteRange[]
            {
                new AvifByteRange((ulong)source.Length, (ulong)(8 - source.Length))
            };
        }

        long cursor = 0;
        while (cursor < source.Length)
        {
            if (cursor > source.Length - 8)
            {
                return CreateMissing(cursor, 8, source.Length);
            }
            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice((int)cursor, 4));
            int headerSize = size32 == 1 ? 16 : 8;
            if (cursor > source.Length - headerSize)
            {
                return CreateMissing(cursor, headerSize, source.Length);
            }
            if (size32 == 0)
            {
                return new AvifByteRange[]
                {
                    new AvifByteRange((ulong)source.Length, 1)
                };
            }
            ulong size = size32 == 1
                ? BinaryPrimitives.ReadUInt64BigEndian(
                    source.Slice((int)cursor + 8, 8))
                : size32;
            if (size < (ulong)headerSize || size > long.MaxValue
                || cursor > long.MaxValue - (long)size)
            {
                return new AvifByteRange[]
                {
                    new AvifByteRange((ulong)source.Length, 1)
                };
            }
            long end = cursor + (long)size;
            if (end > source.Length)
            {
                return new AvifByteRange[]
                {
                    new AvifByteRange(
                        (ulong)source.Length,
                        (ulong)(end - source.Length))
                };
            }
            cursor = end;
        }
        return new AvifByteRange[]
        {
            new AvifByteRange((ulong)source.Length, 8)
        };
    }

    static AvifByteRange[] CreateMissing(
        long boxOffset,
        int headerSize,
        int availableLength)
    {
        long end = boxOffset + headerSize;
        return new AvifByteRange[]
        {
            new AvifByteRange(
                (ulong)availableLength,
                (ulong)(end - availableLength))
        };
    }

    static bool CouldBeIncomplete(AvifResultCode resultCode)
    {
        return resultCode == AvifResultCode.NeedMoreData
            || resultCode == AvifResultCode.TruncatedData
            || resultCode == AvifResultCode.InvalidData
            || resultCode == AvifResultCode.InvalidContainer
            || resultCode == AvifResultCode.InvalidItem
            || resultCode == AvifResultCode.InvalidTrack
            || resultCode == AvifResultCode.InvalidAv1Bitstream
            || resultCode == AvifResultCode.NonConformingData;
    }

    AvifByteRange[] FindCoverageGaps()
    {
        IncrementalInput current = input
            ?? throw new InvalidOperationException("Invalid internal state.");
        if (current.Ranges.Count == 0 || current.Length == 0)
        {
            return System.Array.Empty<AvifByteRange>();
        }
        System.Collections.Generic.List<AvifByteRange> gaps =
            new System.Collections.Generic.List<AvifByteRange>();
        int cursor = 0;
        int index = 0;
        while (index < current.Ranges.Count)
        {
            IncrementalRange range = current.Ranges[index];
            if (range.Offset > cursor)
            {
                gaps.Add(new AvifByteRange(
                    (ulong)cursor,
                    (ulong)(range.Offset - cursor)));
            }
            cursor = range.End;
            index++;
        }
        return gaps.ToArray();
    }

    void InvalidateAvailableState()
    {
        document = null;
        validation = null;
        missingRanges = System.Array.Empty<AvifByteRange>();
        decodedRowCount = 0;
        parsed = false;
    }

    static uint[]? GetGridDependencies(ParsedItem? item)
    {
        if (item == null || item.GetItemType() != "grid")
        {
            return null;
        }
        System.Collections.Generic.List<uint> ids =
            new System.Collections.Generic.List<uint>();
        int referenceIndex = 0;
        while (referenceIndex < item.GetOutgoingReferenceCount())
        {
            ParsedItemReference? reference =
                item.GetOutgoingReference(referenceIndex);
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

    static ParsedAv1LayeredImageIndex? FindLayerIndex(
        ParsedItem? item)
    {
        if (item == null)
        {
            return null;
        }
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedAv1LayeredImageIndex? layers =
                (item.GetPropertyAssociation(index)
                    ?? throw new InvalidOperationException("Invalid internal state."))
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
}
