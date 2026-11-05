using System;
using System.Buffers.Binary;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifIncrementalReader"/>.</summary>
public interface IAvifIncrementalReader
{
    /// <summary>Returns the outcome of the most recent incremental operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Copies another contiguous source chunk into the bounded input buffer.</summary>
    /// <param name="source">The next bytes in source order.</param>
    /// <returns>Success, LimitExceeded, InvalidState after completion, or Closed.</returns>
    AvifResultCode Append(ReadOnlySpan<byte> source);

    /// <summary>Copies a bounded source range at its absolute offset.</summary>
    /// <param name="offset">The nonnegative absolute source offset.</param>
    /// <param name="source">The bytes covering that range.</param>
    /// <returns>Success, InvalidData for conflicting overlap, LimitExceeded, InvalidState, or Closed.</returns>
    AvifResultCode AddRange(
        long offset,
        ReadOnlySpan<byte> source);

    /// <summary>Parses all currently available bytes or reports the next required range.</summary>
    /// <returns>Success, NeedMoreData, or a final parse result after completion.</returns>
    AvifResultCode ParseAvailable();

    /// <summary>Decodes the selected image when enough source bytes are available.</summary>
    /// <returns>A caller-owned image result, or NeedMoreData while waiting.</returns>
    AvifDecodeResult DecodeAvailable();

    /// <summary>Returns the parsed document after a successful available parse.</summary>
    /// <returns>The document, or null while waiting, after invalidation, or after closing.</returns>
    AvifDocument? GetDocument();

    /// <summary>Reports whether container parsing has completed successfully.</summary>
    /// <returns><c>true</c> when the current retained ranges produced a document.</returns>
    bool IsParseAvailable();

    /// <summary>Reports whether one parsed item's complete resolved payload is retained.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns><c>true</c> when the item and every resolved extent are available.</returns>
    bool IsItemAvailable(uint itemId);

    /// <summary>Reports whether one selected sequence frame's complete encoded sample is retained.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns><c>true</c> when the sample range is available.</returns>
    bool IsFrameAvailable(int frameIndex);

    /// <summary>Reports whether one grid cell's encoded dependency item is retained.</summary>
    /// <param name="itemId">The grid item identifier.</param>
    /// <param name="row">The zero-based grid row.</param>
    /// <param name="column">The zero-based grid column.</param>
    /// <returns><c>true</c> when the cell coordinates and dependency payload are available.</returns>
    bool IsTileAvailable(uint itemId, int row, int column);

    /// <summary>Reports whether one progressive primary-item layer is retained.</summary>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns><c>true</c> when the layer exists and its coded item payload is available.</returns>
    bool IsLayerAvailable(int layerIndex);

    /// <summary>Returns the number of contiguous rows produced by the most recent decode.</summary>
    /// <returns>Zero while waiting; otherwise the decoded image height.</returns>
    int GetDecodedRowCount();

    /// <summary>Returns source ranges required by the most recent waiting operation.</summary>
    /// <returns>An independent range array, empty when no range is pending.</returns>
    AvifByteRange[] GetMissingRanges();

    /// <summary>Returns the number of source bytes retained so far.</summary>
    /// <returns>The retained byte count, or zero after closing.</returns>
    int GetReceivedByteCount();

    /// <summary>Returns the retained source prefix length beginning at offset zero.</summary>
    /// <returns>The contiguous byte count, or zero after closing.</returns>
    int GetContiguousByteCount();

    /// <summary>Reports whether the caller declared that no more source bytes will arrive.</summary>
    /// <returns><c>true</c> after completion and before closure.</returns>
    bool IsComplete();

    /// <summary>Declares the current source bytes complete and rejects future appends.</summary>
    /// <returns>Success, or Closed after cleanup.</returns>
    AvifResultCode Complete();

    /// <summary>Releases all retained source, document, range, and progress state.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
