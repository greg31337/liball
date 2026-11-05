namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfType0EncodingMetadata"/>.</summary>
public interface IPdfType0EncodingMetadata
{
    /// <summary>Gets the predefined PDF CMap name.</summary>
    /// <returns><c>Identity-H</c> or <c>Identity-V</c>.</returns>
    string GetEncodingName();

    /// <summary>Gets the writing mode selected by the CMap.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    PdfWritingMode GetWritingMode();

    /// <summary>Gets the PDF CMap writing-mode value.</summary>
    /// <returns>Zero for horizontal writing or one for vertical writing.</returns>
    int GetWMode();

    /// <summary>Gets the fixed character-code width.</summary>
    /// <returns>Two bytes.</returns>
    int GetCodeByteCount();

    /// <summary>Gets the first code in the Identity codespace.</summary>
    /// <returns>Zero.</returns>
    int GetCodeSpaceMinimum();

    /// <summary>Gets the last code in the Identity codespace.</summary>
    /// <returns>65535.</returns>
    int GetCodeSpaceMaximum();
}
