namespace ttf;

/// <summary>Contains a complete deterministic PDF ToUnicode CMap stream.</summary>
public class PdfToUnicodeCMap : IPdfToUnicodeCMap
{
    string text;
    byte[] bytes;
    int codeByteCount;
    int mappingCount;
    int bfCharBlockCount;
    int bfRangeBlockCount;

    internal PdfToUnicodeCMap(
        string text,
        int codeByteCount,
        int mappingCount,
        int bfCharBlockCount,
        int bfRangeBlockCount)
    {
        this.text = text;
        bytes = System.Text.Encoding.ASCII.GetBytes(text);
        this.codeByteCount = codeByteCount;
        this.mappingCount = mappingCount;
        this.bfCharBlockCount = bfCharBlockCount;
        this.bfRangeBlockCount = bfRangeBlockCount;
    }

    /// <summary>Gets the deterministic ASCII CMap text.</summary>
    /// <returns>The complete stream text with LF line endings.</returns>
    public string GetText() { return text; }

    /// <summary>Copies the complete unfiltered CMap stream bytes.</summary>
    /// <returns>A new ASCII byte array.</returns>
    public byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];
        System.Array.Copy(bytes, copy, bytes.Length);
        return copy;
    }

    /// <summary>Gets the fixed source character-code width.</summary>
    /// <returns>One or two bytes.</returns>
    public int GetCodeByteCount() { return codeByteCount; }

    /// <summary>Gets the codespace minimum.</summary>
    /// <returns>Zero.</returns>
    public int GetCodeSpaceMinimum() { return 0; }

    /// <summary>Gets the codespace maximum.</summary>
    /// <returns>255 for one-byte codes or 65535 for two-byte codes.</returns>
    public int GetCodeSpaceMaximum()
    {
        return codeByteCount == 1 ? byte.MaxValue : ushort.MaxValue;
    }

    /// <summary>Gets the number of explicit character-code mappings.</summary>
    /// <returns>The validated input mapping count.</returns>
    public int GetMappingCount() { return mappingCount; }

    /// <summary>Gets the number of emitted <c>beginbfchar</c> blocks.</summary>
    /// <returns>The isolated-character block count.</returns>
    public int GetBfCharBlockCount() { return bfCharBlockCount; }

    /// <summary>Gets the number of emitted <c>beginbfrange</c> blocks.</summary>
    /// <returns>The consecutive-range block count.</returns>
    public int GetBfRangeBlockCount() { return bfRangeBlockCount; }
}
