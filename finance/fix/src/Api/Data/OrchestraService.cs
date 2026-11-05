using System.Collections.Generic;

namespace fix;

/// <summary>Defines an addressable service in an Orchestra interface.</summary>
public class OrchestraService
{
    /// <summary>The service name.</summary>
    public string Name = "";
    /// <summary>The service address.</summary>
    public string Address = "";
    /// <summary>The protocol version.</summary>
    public string Version = "";
    /// <summary>The protocol layer.</summary>
    public string Layer = "";
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
    /// <summary>The referenced protocol names.</summary>
    public List<string> Protocols = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
