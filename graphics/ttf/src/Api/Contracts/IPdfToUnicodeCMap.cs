namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfToUnicodeCMap"/>.</summary>
public interface IPdfToUnicodeCMap
{
    /// <summary>Gets the deterministic ASCII CMap text.</summary>
    /// <returns>The complete stream text with LF line endings.</returns>
    string GetText();

    /// <summary>Copies the complete unfiltered CMap stream bytes.</summary>
    /// <returns>A new ASCII byte array.</returns>
    byte[] GetBytes();

    /// <summary>Gets the fixed source character-code width.</summary>
    /// <returns>One or two bytes.</returns>
    int GetCodeByteCount();

    /// <summary>Gets the codespace minimum.</summary>
    /// <returns>Zero.</returns>
    int GetCodeSpaceMinimum();

    /// <summary>Gets the codespace maximum.</summary>
    /// <returns>255 for one-byte codes or 65535 for two-byte codes.</returns>
    int GetCodeSpaceMaximum();

    /// <summary>Gets the number of explicit character-code mappings.</summary>
    /// <returns>The validated input mapping count.</returns>
    int GetMappingCount();

    /// <summary>Gets the number of emitted <c>beginbfchar</c> blocks.</summary>
    /// <returns>The isolated-character block count.</returns>
    int GetBfCharBlockCount();

    /// <summary>Gets the number of emitted <c>beginbfrange</c> blocks.</summary>
    /// <returns>The consecutive-range block count.</returns>
    int GetBfRangeBlockCount();
}
