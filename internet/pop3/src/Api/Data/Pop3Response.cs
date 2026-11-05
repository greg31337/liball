namespace pop3;

/// <summary>Describes a POP3 response line.</summary>
public class Pop3Response
{
    /// <summary>The kind of response represented by the line.</summary>
    public Pop3ResponseKind Kind;

    /// <summary>The human-readable response text without the status marker.</summary>
    public string Text = "";

    /// <summary>The ordered levels of an extended response code, or an empty array when no code is present.</summary>
    public string[] ResponseCodeLevels = new string[0];

    /// <summary>The greeting timestamp used by APOP, or <c>null</c> when no timestamp is present.</summary>
    public string? GreetingTimestamp;

    /// <summary>Indicates whether the response is associated with multiline data.</summary>
    public bool IsMultiline;

    /// <summary>An advisory multiline octet count, or <c>-1</c> when the peer did not provide one.</summary>
    public long AdvisoryOctetCount = -1;
}
