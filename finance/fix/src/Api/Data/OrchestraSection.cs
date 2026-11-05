using System.Collections.Generic;

namespace fix;

/// <summary>Defines one large-grained Orchestra business section.</summary>
public class OrchestraSection
{
    /// <summary>The section name.</summary>
    public string Name = "";
    /// <summary>Whether a display order was supplied.</summary>
    public bool HasDisplayOrder;
    /// <summary>The display order.</summary>
    public int DisplayOrder;
    /// <summary>The display order in source lexical form.</summary>
    public string DisplayOrderText = "";
    /// <summary>The FIXML filename.</summary>
    public string FixmlFileName = "";
    /// <summary>The physical encodings in source order.</summary>
    public List<OrchestraEncoding> Encodings = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
