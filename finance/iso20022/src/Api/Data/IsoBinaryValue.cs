namespace iso20022;

/// <summary>Represents decoded binary content and its lexical encoding.</summary>
public class IsoBinaryValue
{
    /// <summary>The decoded bytes.</summary>
    public byte[] Value = [];
    /// <summary>The lexical encoding used by the source.</summary>
    public BinaryEncodingKind Encoding;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
