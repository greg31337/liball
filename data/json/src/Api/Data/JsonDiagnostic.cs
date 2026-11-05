namespace json;

/// <summary>Contains machine-readable information about a JSON operation failure.</summary>
public class JsonDiagnostic
{
    /// <summary>The stable failure category.</summary>
    public JsonDiagnosticCode Code;

    /// <summary>The human-readable failure message.</summary>
    public string? Message;

    /// <summary>The source range associated with the failure.</summary>
    public JsonSourceRange? Range;

    /// <summary>The RFC 6901 pointer at the failure location.</summary>
    public string? Pointer;

    /// <summary>The container depth at the failure location.</summary>
    public int Depth;

    /// <summary>A bounded summary of tokens valid at the failure location.</summary>
    public string? ExpectedTokens;

    /// <summary>A bounded description of the token found at the failure location.</summary>
    public string? ActualToken;

    /// <summary>An owned, bounded source-byte excerpt surrounding the failure.</summary>
    public byte[]? SourceExcerpt;
}
