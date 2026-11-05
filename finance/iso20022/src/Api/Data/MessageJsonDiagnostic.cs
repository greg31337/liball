namespace iso20022;

/// <summary>Describes one deterministic warning produced while converting ISO 20022 XML and JSON forms.</summary>
public class MessageJsonDiagnostic
{
    /// <summary>The stable diagnostic code.</summary>
    public string Code = string.Empty;
    /// <summary>The human-readable diagnostic message.</summary>
    public string Message = string.Empty;
    /// <summary>The local-name path of the affected content.</summary>
    public string Path = string.Empty;
}
