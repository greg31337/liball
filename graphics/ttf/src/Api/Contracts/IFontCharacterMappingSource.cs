namespace ttf;

/// <summary>Defines provenance operations for one selected cmap encoding record.</summary>
public interface IFontCharacterMappingSource
{
    /// <summary>Gets the source index in public selection order.</summary>
    /// <returns>The zero-based source index.</returns>
    int GetSourceIndex();

    /// <summary>Gets the encoding record index in cmap table order.</summary>
    /// <returns>The zero-based encoding record index.</returns>
    int GetEncodingRecordIndex();

    /// <summary>Gets the OpenType platform identifier.</summary>
    /// <returns>The platform identifier.</returns>
    int GetPlatformId();

    /// <summary>Gets the platform-specific encoding identifier.</summary>
    /// <returns>The encoding identifier.</returns>
    int GetEncodingId();

    /// <summary>Gets the cmap subtable format.</summary>
    /// <returns>The format number.</returns>
    int GetFormat();

    /// <summary>Gets the cmap subtable language value.</summary>
    /// <returns>The language value.</returns>
    long GetLanguage();

    /// <summary>Gets the subtable offset relative to the cmap table.</summary>
    /// <returns>The relative subtable offset.</returns>
    long GetSubtableOffset();

    /// <summary>Gets the nominal selection priority rank.</summary>
    /// <returns>The rank, or negative one for the variation-selector source.</returns>
    int GetPriorityRank();

    /// <summary>Reports whether this is the format 14 variation-selector source.</summary>
    /// <returns>True for the variation-selector source.</returns>
    bool IsVariationSelectorSource();
}
