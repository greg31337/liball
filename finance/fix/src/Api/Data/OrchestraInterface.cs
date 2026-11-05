using System.Collections.Generic;

namespace fix;

/// <summary>Defines services, protocols, and sessions for one Orchestra interface.</summary>
public class OrchestraInterface
{
    /// <summary>The interface name.</summary>
    public string Name = "";
    /// <summary>The declared services.</summary>
    public List<OrchestraService> Services = [];
    /// <summary>The declared protocol layers.</summary>
    public List<OrchestraProtocol> Protocols = [];
    /// <summary>The declared user-interface protocols.</summary>
    public List<OrchestraProtocol> UserInterfaces = [];
    /// <summary>The declared presentation encodings.</summary>
    public List<OrchestraProtocol> Encodings = [];
    /// <summary>The declared session protocols.</summary>
    public List<OrchestraProtocol> SessionProtocols = [];
    /// <summary>The declared transport protocols.</summary>
    public List<OrchestraProtocol> Transports = [];
    /// <summary>The declared sessions.</summary>
    public List<OrchestraSession> Sessions = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
