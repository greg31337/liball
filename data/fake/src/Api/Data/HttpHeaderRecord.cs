namespace fake;

/// <summary>Represents a synthetic HTTP field name and value without message framing.</summary>
/// <remarks>The record is inert data and does not represent authorization, a live cookie, or a network response.</remarks>
public class HttpHeaderRecord
{
    /// <summary>The validated HTTP field-name token.</summary>
    public string Name = string.Empty;

    /// <summary>The injection-safe visible ASCII field value.</summary>
    public string Value = string.Empty;

    /// <summary>The field name, a colon and space, and the field value.</summary>
    public string Text = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the value represents a live credential, always <c>false</c>.</summary>
    public bool RepresentsLiveCredential;

    internal HttpHeaderRecord()
    {
    }
}
