namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifDecodeResult"/>.</summary>
public interface IAvifDecodeResult
{
    /// <summary>Returns the decode outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the decoded planar image.</summary>
    /// <returns>The image, or null when decoding failed.</returns>
    AvifImage? GetImage();

    /// <summary>Returns the inspected source document.</summary>
    /// <returns>The parsed document, or null for an invalid source argument.</returns>
    AvifDocument? GetDocument();

    /// <summary>Returns the selected item identifier.</summary>
    /// <returns>The item identifier, or zero for a timed or unavailable selection.</returns>
    uint GetSelectedItemId();

    /// <summary>Returns the selected timed-media track identifier.</summary>
    /// <returns>The track identifier, or zero for an item selection.</returns>
    uint GetSelectedTrackId();

    /// <summary>Returns the selected sequence frame index.</summary>
    /// <returns>The zero-based frame index, or minus one for an item selection.</returns>
    int GetSelectedFrameIndex();

    /// <summary>Returns timing for the selected sequence frame.</summary>
    /// <returns>The timing, or null for an item selection.</returns>
    AvifFrameTiming? GetSelectedFrameTiming();

    /// <summary>Returns the selected sequence sample description.</summary>
    /// <returns>The sample, or null for an item selection.</returns>
    AvifSample? GetSelectedSequenceSample();

    /// <summary>Returns the number of decoded auxiliary images.</summary>
    /// <returns>The auxiliary output count.</returns>
    int GetDecodedAuxiliaryCount();

    /// <summary>Returns one decoded auxiliary output.</summary>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The output, or null for an invalid index.</returns>
    AvifDecodedAuxiliary? GetDecodedAuxiliary(int index);

    /// <summary>Returns the first decoded alpha auxiliary.</summary>
    /// <returns>The caller-owned alpha image, or null when absent.</returns>
    AvifImage? GetAlphaImage();

    /// <summary>Returns the first decoded depth auxiliary.</summary>
    /// <returns>The caller-owned depth image, or null when absent.</returns>
    AvifImage? GetDepthImage();

    /// <summary>Returns the decoded gain-map association.</summary>
    /// <returns>The gain map, or null when the selection has no gain map.</returns>
    AvifGainMap? GetGainMap();

    /// <summary>Returns the decoded tone-map alternative.</summary>
    /// <returns>The tone-map association, or null when absent.</returns>
    AvifToneMap? GetToneMap();

    /// <summary>Returns the number of decoded progressive layers.</summary>
    /// <returns>The progressive-layer count.</returns>
    int GetProgressiveLayerCount();

    /// <summary>Returns one decoded progressive layer.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The caller-owned layer image, or null for an invalid index.</returns>
    AvifImage? GetProgressiveLayer(int index);

    /// <summary>Returns the number of metadata items associated with the selection.</summary>
    /// <returns>The associated metadata count.</returns>
    int GetAssociatedMetadataCount();

    /// <summary>Returns one associated metadata item with independently owned payload bytes.</summary>
    /// <param name="index">The zero-based metadata index.</param>
    /// <returns>The metadata, or null for an invalid index.</returns>
    AvifMetadata? GetAssociatedMetadata(int index);
}
