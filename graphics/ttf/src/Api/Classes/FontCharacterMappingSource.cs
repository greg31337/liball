namespace ttf;

/// <summary>Describes one selected cmap encoding record and subtable.</summary>
public class FontCharacterMappingSource : IFontCharacterMappingSource
{
    int sourceIndex;
    int encodingRecordIndex;
    int priorityRank;
    bool variationSelectorSource;
    SfntCmapEncodingRecord record;

    internal FontCharacterMappingSource(
        int sourceIndex,
        int encodingRecordIndex,
        int priorityRank,
        bool variationSelectorSource,
        SfntCmapEncodingRecord record)
    {
        this.sourceIndex = sourceIndex;
        this.encodingRecordIndex = encodingRecordIndex;
        this.priorityRank = priorityRank;
        this.variationSelectorSource = variationSelectorSource;
        this.record = record;
    }

    /// <summary>Gets the source index in public selection order.</summary>
    /// <returns>The zero-based source index.</returns>
    public int GetSourceIndex() { return sourceIndex; }

    /// <summary>Gets the encoding record index in cmap table order.</summary>
    /// <returns>The zero-based encoding record index.</returns>
    public int GetEncodingRecordIndex() { return encodingRecordIndex; }

    /// <summary>Gets the OpenType platform identifier.</summary>
    /// <returns>The platform identifier.</returns>
    public int GetPlatformId() { return record.GetPlatformId(); }

    /// <summary>Gets the platform-specific encoding identifier.</summary>
    /// <returns>The encoding identifier.</returns>
    public int GetEncodingId() { return record.GetEncodingId(); }

    /// <summary>Gets the cmap subtable format.</summary>
    /// <returns>The format number.</returns>
    public int GetFormat() { return record.GetSubtable().GetFormat(); }

    /// <summary>Gets the cmap subtable language value.</summary>
    /// <returns>The language value.</returns>
    public long GetLanguage() { return record.GetSubtable().GetLanguage(); }

    /// <summary>Gets the subtable offset relative to the cmap table.</summary>
    /// <returns>The relative subtable offset.</returns>
    public long GetSubtableOffset() { return record.GetSubtableOffset(); }

    /// <summary>Gets the nominal selection priority rank.</summary>
    /// <returns>The rank, or negative one for the variation-selector source.</returns>
    public int GetPriorityRank() { return priorityRank; }

    /// <summary>Reports whether this is the format 14 variation-selector source.</summary>
    /// <returns>True for the variation-selector source.</returns>
    public bool IsVariationSelectorSource() { return variationSelectorSource; }
}
