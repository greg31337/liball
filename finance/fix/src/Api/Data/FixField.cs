namespace fix;

/// <summary>Represents one ordered FIX field and its unmodified byte value.</summary>
public class FixField
{
    /// <summary>The numeric FIX tag.</summary>
    public int Tag;
    /// <summary>The field value bytes without the tag, equals sign, or delimiter.</summary>
    public byte[] Value = [];
    /// <summary>The field's zero-based position in the original message.</summary>
    public int WireIndex = -1;
    /// <summary>The byte offset of the field in the original input, or <c>-1</c> when unavailable.</summary>
    public int SourceOffset = -1;
    /// <summary>The complete encoded field length including its delimiter.</summary>
    public int SourceLength;
    /// <summary>Whether the active dictionary does not define this tag.</summary>
    public bool IsUnknown;
    /// <summary>Whether the value belongs to a length-associated data field.</summary>
    public bool IsEncodedData;
}
