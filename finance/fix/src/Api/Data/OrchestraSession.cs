using System.Collections.Generic;

namespace fix;

/// <summary>Defines one session configuration in an Orchestra interface.</summary>
public class OrchestraSession
{
    /// <summary>The session name.</summary>
    public string Name = "";
    /// <summary>The actor or endpoint role.</summary>
    public string Role = "";
    /// <summary>The referenced service name.</summary>
    public string Service = "";
    /// <summary>The activation time in source lexical form.</summary>
    public string ActivationTime = "";
    /// <summary>The deactivation time in source lexical form.</summary>
    public string DeactivationTime = "";
    /// <summary>The security keys in RFC 7468 textual form.</summary>
    public string SecurityKeys = "";
    /// <summary>The referenced protocol names in stack order.</summary>
    public List<string> Protocols = [];
    /// <summary>Session identifiers retained by name.</summary>
    public Dictionary<string, string> Identifiers = [];
    /// <summary>Session identifiers in source order, including duplicate names.</summary>
    public List<OrchestraIdentifier> IdentifierEntries = [];
    /// <summary>Session-specific service definitions and overrides.</summary>
    public List<OrchestraService> ServiceOverrides = [];
    /// <summary>Session-specific user-interface protocol definitions and overrides.</summary>
    public List<OrchestraProtocol> UserInterfaceOverrides = [];
    /// <summary>Session-specific encoding definitions and overrides.</summary>
    public List<OrchestraProtocol> EncodingOverrides = [];
    /// <summary>Session-specific session-protocol definitions and overrides.</summary>
    public List<OrchestraProtocol> SessionProtocolOverrides = [];
    /// <summary>Session-specific generic protocol definitions and overrides.</summary>
    public List<OrchestraProtocol> ProtocolOverrides = [];
    /// <summary>Session-specific transport definitions and overrides.</summary>
    public List<OrchestraProtocol> TransportOverrides = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
