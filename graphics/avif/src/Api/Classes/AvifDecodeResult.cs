using System.Collections.Generic;

namespace avif;

/// <summary>Contains a one-call image decode result and inspected container metadata.</summary>
public class AvifDecodeResult : IAvifDecodeResult, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifImage? image;
    AvifDocument? document;
    uint selectedItemId;
    uint selectedTrackId;
    int selectedFrameIndex;
    AvifFrameTiming? selectedFrameTiming;
    AvifSample? selectedSequenceSample;
    List<AvifDecodedAuxiliary> auxiliaries;
    AvifGainMap? gainMap;
    AvifToneMap? toneMap;
    List<AvifImage> progressiveLayers;
    List<AvifMetadata> metadata;

    /// <summary>Creates a decode result for an adapter or test implementation.</summary>
    /// <param name="code">The decode outcome.</param>
    /// <param name="image">The decoded image, or null when unavailable.</param>
    /// <param name="document">The inspected document, or null when unavailable.</param>
    public AvifDecodeResult(
        AvifResultCode code,
        AvifImage? image,
        AvifDocument? document)
    {
        this.code = code;
        this.image = image;
        this.document = document;
        selectedItemId = 0;
        selectedTrackId = 0;
        selectedFrameIndex = -1;
        selectedFrameTiming = null;
        selectedSequenceSample = null;
        auxiliaries = new List<AvifDecodedAuxiliary>();
        gainMap = null;
        toneMap = null;
        progressiveLayers = new List<AvifImage>();
        metadata = new List<AvifMetadata>();
    }

    /// <summary>Returns the decode outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the decoded planar image.</summary>
    /// <returns>The image, or null when decoding failed.</returns>
    public AvifImage? GetImage()
    {
        return image;
    }

    /// <summary>Returns the inspected source document.</summary>
    /// <returns>The parsed document, or null for an invalid source argument.</returns>
    public AvifDocument? GetDocument()
    {
        return document;
    }

    /// <summary>Returns the selected item identifier.</summary>
    /// <returns>The item identifier, or zero for a timed or unavailable selection.</returns>
    public uint GetSelectedItemId()
    {
        return selectedItemId;
    }

    /// <summary>Returns the selected timed-media track identifier.</summary>
    /// <returns>The track identifier, or zero for an item selection.</returns>
    public uint GetSelectedTrackId()
    {
        return selectedTrackId;
    }

    /// <summary>Returns the selected sequence frame index.</summary>
    /// <returns>The zero-based frame index, or minus one for an item selection.</returns>
    public int GetSelectedFrameIndex()
    {
        return selectedFrameIndex;
    }

    /// <summary>Returns timing for the selected sequence frame.</summary>
    /// <returns>The timing, or null for an item selection.</returns>
    public AvifFrameTiming? GetSelectedFrameTiming()
    {
        return selectedFrameTiming;
    }

    /// <summary>Returns the selected sequence sample description.</summary>
    /// <returns>The sample, or null for an item selection.</returns>
    public AvifSample? GetSelectedSequenceSample()
    {
        return selectedSequenceSample;
    }

    /// <summary>Returns the number of decoded auxiliary images.</summary>
    /// <returns>The auxiliary output count.</returns>
    public int GetDecodedAuxiliaryCount()
    {
        return auxiliaries.Count;
    }

    /// <summary>Returns one decoded auxiliary output.</summary>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The output, or null for an invalid index.</returns>
    public AvifDecodedAuxiliary? GetDecodedAuxiliary(int index)
    {
        if (index < 0 || index >= auxiliaries.Count)
        {
            return null;
        }
        AvifDecodedAuxiliary source = auxiliaries[index];
        return new AvifDecodedAuxiliary(
            source.Kind,
            source.TypeName,
            source.ItemId,
            source.MasterItemId,
            source.TrackId,
            source.MasterTrackId,
            source.Premultiplied,
            source.Image);
    }

    /// <summary>Returns the first decoded alpha auxiliary.</summary>
    /// <returns>The caller-owned alpha image, or null when absent.</returns>
    public AvifImage? GetAlphaImage()
    {
        return FindAuxiliaryImage(AvifAuxiliaryKind.Alpha);
    }

    /// <summary>Returns the first decoded depth auxiliary.</summary>
    /// <returns>The caller-owned depth image, or null when absent.</returns>
    public AvifImage? GetDepthImage()
    {
        return FindAuxiliaryImage(AvifAuxiliaryKind.Depth);
    }

    /// <summary>Returns the decoded gain-map association.</summary>
    /// <returns>The gain map, or null when the selection has no gain map.</returns>
    public AvifGainMap? GetGainMap()
    {
        return gainMap;
    }

    /// <summary>Returns the decoded tone-map alternative.</summary>
    /// <returns>The tone-map association, or null when absent.</returns>
    public AvifToneMap? GetToneMap()
    {
        return toneMap;
    }

    /// <summary>Returns the number of decoded progressive layers.</summary>
    /// <returns>The progressive-layer count.</returns>
    public int GetProgressiveLayerCount()
    {
        return progressiveLayers.Count;
    }

    /// <summary>Returns one decoded progressive layer.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The caller-owned layer image, or null for an invalid index.</returns>
    public AvifImage? GetProgressiveLayer(int index)
    {
        return index < 0 || index >= progressiveLayers.Count
            ? null : progressiveLayers[index];
    }

    /// <summary>Returns the number of metadata items associated with the selection.</summary>
    /// <returns>The associated metadata count.</returns>
    public int GetAssociatedMetadataCount()
    {
        return metadata.Count;
    }

    /// <summary>Returns one associated metadata item with independently owned payload bytes.</summary>
    /// <param name="index">The zero-based metadata index.</param>
    /// <returns>The metadata, or null for an invalid index.</returns>
    public AvifMetadata? GetAssociatedMetadata(int index)
    {
        return index < 0 || index >= metadata.Count
            ? null : metadata[index];
    }

    /// <summary>Returns diagnostics produced while inspecting, decoding, or rendering.</summary>
    /// <returns>The structured decode diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            document,
            code,
            AvifDiagnosticCategory.Rendering,
            "The image decode or rendering operation did not complete successfully.");
    }

    internal void SetItemSelection(uint itemId)
    {
        selectedItemId = itemId;
    }

    internal void SetSequenceSelection(
        uint trackId,
        int frameIndex,
        AvifFrameTiming? timing,
        AvifSample? sample)
    {
        selectedTrackId = trackId;
        selectedFrameIndex = frameIndex;
        selectedFrameTiming = timing;
        selectedSequenceSample = sample;
    }

    internal void AddAuxiliaryOutput(AvifDecodedAuxiliary auxiliary)
    {
        auxiliaries.Add(auxiliary);
    }

    internal void SetGainMapOutput(AvifGainMap value)
    {
        gainMap = value;
    }

    internal void SetToneMapOutput(AvifToneMap value)
    {
        toneMap = value;
    }

    internal void AddProgressiveLayerOutput(AvifImage layer)
    {
        progressiveLayers.Add(layer);
    }

    internal void AddMetadataOutput(AvifMetadata value)
    {
        metadata.Add(value);
    }

    AvifImage? FindAuxiliaryImage(AvifAuxiliaryKind kind)
    {
        int index = 0;
        while (index < auxiliaries.Count)
        {
            if (auxiliaries[index].Kind == kind)
            {
                return auxiliaries[index].Image;
            }
            index++;
        }
        return null;
    }
}
