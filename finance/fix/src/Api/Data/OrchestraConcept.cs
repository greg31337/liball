using System.Collections.Generic;

namespace fix;

/// <summary>Defines one semantic concept spanning repository definitions.</summary>
public class OrchestraConcept
{
    /// <summary>The concept name.</summary>
    public string Name = "";
    /// <summary>The referenced concept members in source order.</summary>
    public List<OrchestraConceptMember> Members = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
