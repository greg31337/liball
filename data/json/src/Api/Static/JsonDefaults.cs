namespace json;

/// <summary>Provides safe default resource limits and JSON formatting values.</summary>
public static class JsonDefaults
{
    /// <summary>The default maximum number of UTF-8 bytes in one document.</summary>
    public const int MaximumDocumentBytes = 16_777_216;

    /// <summary>The default maximum number of caller-owned input bytes retained internally.</summary>
    public const int MaximumInputBufferBytes = 1_048_576;

    /// <summary>The default maximum number of raw UTF-8 bytes in one token.</summary>
    public const int MaximumTokenBytes = 1_048_576;

    /// <summary>The default maximum number of UTF-8 bytes in one decoded string.</summary>
    public const int MaximumDecodedStringBytes = 1_048_576;

    /// <summary>The default maximum number of UTF-16 code units in one decoded string.</summary>
    public const int MaximumDecodedStringCodeUnits = 1_048_576;

    /// <summary>The default maximum number of characters in one JSON number.</summary>
    public const int MaximumNumberCharacters = 1_024;

    /// <summary>The default maximum JSON container nesting depth.</summary>
    public const int MaximumNestingDepth = 64;

    /// <summary>The default maximum number of items in one JSON array.</summary>
    public const int MaximumArrayItems = 1_000_000;

    /// <summary>The default maximum number of members in one JSON object.</summary>
    public const int MaximumObjectMembers = 1_000_000;

    /// <summary>The default maximum number of values and members in a document model.</summary>
    public const int MaximumDomNodes = 1_000_000;

    /// <summary>The default maximum number of UTF-8 bytes emitted by one operation.</summary>
    public const int MaximumOutputBytes = 16_777_216;

    /// <summary>The default number of bytes allocated for stream buffering.</summary>
    public const int StreamBufferBytes = 16_384;

    /// <summary>The default maximum number of source bytes retained in a diagnostic excerpt.</summary>
    public const int MaximumDiagnosticExcerptBytes = 256;

    /// <summary>The default indentation inserted for each nested pretty-printed container.</summary>
    public const string Indentation = "  ";

    /// <summary>The default line ending used by pretty-printed output.</summary>
    public const string NewLine = "\n";

    /// <summary>Indicates whether pretty-printed output contains a space after each name separator.</summary>
    public const bool SpaceAfterColon = true;
}
