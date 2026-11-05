namespace json;

/// <summary>Contains signed resource limits for JSON operations.</summary>
/// <remarks>A field value of zero requests the corresponding value from <see cref="JsonDefaults"/>.</remarks>
public class JsonLimits
{
    /// <summary>The maximum number of UTF-8 bytes in one document.</summary>
    public int MaximumDocumentBytes;

    /// <summary>The maximum number of caller-owned input bytes retained internally.</summary>
    public int MaximumInputBufferBytes;

    /// <summary>The maximum number of raw UTF-8 bytes in one token.</summary>
    public int MaximumTokenBytes;

    /// <summary>The maximum number of UTF-8 bytes in one decoded string.</summary>
    public int MaximumDecodedStringBytes;

    /// <summary>The maximum number of UTF-16 code units in one decoded string.</summary>
    public int MaximumDecodedStringCodeUnits;

    /// <summary>The maximum number of characters in one JSON number.</summary>
    public int MaximumNumberCharacters;

    /// <summary>The maximum JSON container nesting depth.</summary>
    public int MaximumNestingDepth;

    /// <summary>The maximum number of items in one JSON array.</summary>
    public int MaximumArrayItems;

    /// <summary>The maximum number of members in one JSON object.</summary>
    public int MaximumObjectMembers;

    /// <summary>The maximum number of values and members in a document model.</summary>
    public int MaximumDomNodes;

    /// <summary>The maximum number of UTF-8 bytes emitted by one operation.</summary>
    public int MaximumOutputBytes;

    /// <summary>The maximum number of source bytes retained in a diagnostic excerpt.</summary>
    public int MaximumDiagnosticExcerptBytes;
}
