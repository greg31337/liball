using System.Collections.Generic;

namespace fix;

/// <summary>Defines one protocol, encoding, session, or transport layer.</summary>
public class OrchestraProtocol
{
    /// <summary>The protocol name.</summary>
    public string Name = "";
    /// <summary>The source protocol element kind.</summary>
    public string Kind = "";
    /// <summary>The protocol layer or purpose.</summary>
    public string Layer = "";
    /// <summary>The protocol version.</summary>
    public string Version = "";
    /// <summary>The encoding identifier.</summary>
    public string Encoding = "";
    /// <summary>The transport identifier.</summary>
    public string Transport = "";
    /// <summary>The delivery reliability.</summary>
    public string Reliability = "";
    /// <summary>The linked Orchestra repository URI.</summary>
    public string OrchestrationUri = "";
    /// <summary>The activation time in source lexical form.</summary>
    public string ActivationTime = "";
    /// <summary>The deactivation time in source lexical form.</summary>
    public string DeactivationTime = "";
    /// <summary>The deprecation time in source lexical form.</summary>
    public string DeprecatedTime = "";
    /// <summary>The transport address.</summary>
    public string Address = "";
    /// <summary>The transport message-cast mode.</summary>
    public string MessageCast = "";
    /// <summary>The transport usage designation.</summary>
    public string Use = "";
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>Protocol-specific settings retained by name.</summary>
    public Dictionary<string, string> Settings = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
