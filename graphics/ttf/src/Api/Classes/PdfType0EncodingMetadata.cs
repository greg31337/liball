namespace ttf;

/// <summary>Describes a predefined Identity CMap for a Type 0 font.</summary>
public class PdfType0EncodingMetadata : IPdfType0EncodingMetadata
{
    string encodingName;
    PdfWritingMode writingMode;

    internal PdfType0EncodingMetadata(
        string encodingName,
        PdfWritingMode writingMode)
    {
        this.encodingName = encodingName;
        this.writingMode = writingMode;
    }

    /// <summary>Gets the predefined PDF CMap name.</summary>
    /// <returns><c>Identity-H</c> or <c>Identity-V</c>.</returns>
    public string GetEncodingName()
    {
        return encodingName;
    }

    /// <summary>Gets the writing mode selected by the CMap.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    public PdfWritingMode GetWritingMode()
    {
        return writingMode;
    }

    /// <summary>Gets the PDF CMap writing-mode value.</summary>
    /// <returns>Zero for horizontal writing or one for vertical writing.</returns>
    public int GetWMode()
    {
        return writingMode == PdfWritingMode.Horizontal ? 0 : 1;
    }

    /// <summary>Gets the fixed character-code width.</summary>
    /// <returns>Two bytes.</returns>
    public int GetCodeByteCount()
    {
        return 2;
    }

    /// <summary>Gets the first code in the Identity codespace.</summary>
    /// <returns>Zero.</returns>
    public int GetCodeSpaceMinimum()
    {
        return 0;
    }

    /// <summary>Gets the last code in the Identity codespace.</summary>
    /// <returns>65535.</returns>
    public int GetCodeSpaceMaximum()
    {
        return ushort.MaxValue;
    }
}
