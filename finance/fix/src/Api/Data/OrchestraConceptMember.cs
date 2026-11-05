using System.Collections.Generic;

namespace fix;

/// <summary>Retains one field, component, group, or message concept reference.</summary>
public class OrchestraConceptMember
{
    /// <summary>The source reference kind.</summary>
    public string Kind = "";
    /// <summary>The referenced numeric identifier in source lexical form.</summary>
    public string Id = "";
    /// <summary>The referenced name when supplied.</summary>
    public string Name = "";
    /// <summary>The referenced scenario.</summary>
    public string Scenario = "";
    /// <summary>The referenced message type.</summary>
    public string MessageType = "";
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
