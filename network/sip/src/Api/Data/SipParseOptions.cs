namespace sip;

/// <summary>
/// Holds compatibility choices and resource limits for codec parse operations.
/// </summary>
public class SipParseOptions
{
    /// <summary>Requires strict protocol conformance when true.</summary>
    public bool Strict = true;

    /// <summary>Preserves original text and byte forms when true.</summary>
    public bool PreserveOriginal = true;

    /// <summary>Permits obsolete header folding when true.</summary>
    public bool AllowHeaderFolding;

    /// <summary>Permits compatible non-canonical whitespace when true.</summary>
    public bool AllowCompatibleWhitespace;

    /// <summary>The maximum complete message size in bytes.</summary>
    public int MaxMessageBytes = SipDefaults.MaxMessageBytes;

    /// <summary>The maximum body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;

    /// <summary>The maximum aggregate header size in bytes.</summary>
    public int MaxHeaderBytes = SipDefaults.MaxHeaderBytes;

    /// <summary>The maximum start-line size in bytes.</summary>
    public int MaxStartLineBytes = SipDefaults.MaxStartLineBytes;

    /// <summary>The maximum individual header line size in bytes.</summary>
    public int MaxHeaderLineBytes = SipDefaults.MaxHeaderLineBytes;

    /// <summary>The maximum number of header fields.</summary>
    public int MaxHeaderCount = SipDefaults.MaxHeaderCount;

    /// <summary>The maximum URI size in bytes.</summary>
    public int MaxUriBytes = SipDefaults.MaxUriBytes;

    /// <summary>The maximum parameter count in one parsed value.</summary>
    public int MaxParameterCount = SipDefaults.MaxParameterCount;

    /// <summary>The maximum list element count in one parsed value.</summary>
    public int MaxListValueCount = SipDefaults.MaxListValueCount;

    /// <summary>The maximum nested MIME multipart depth.</summary>
    public int MaxMimeDepth = SipDefaults.MaxMimeDepth;

    /// <summary>The maximum XML element depth.</summary>
    public int MaxXmlDepth = SipDefaults.MaxXmlDepth;
}
