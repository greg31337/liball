using System.Collections.Generic;

namespace fix;

/// <summary>Defines one Orchestra business-process category.</summary>
public class OrchestraCategory
{
    /// <summary>The category name.</summary>
    public string Name = "";
    /// <summary>The containing section name.</summary>
    public string Section = "";
    /// <summary>The FIXML filename.</summary>
    public string FixmlFileName = "";
    /// <summary>The categorized component kind.</summary>
    public string ComponentType = "";
    /// <summary>The included FIXML filename kind.</summary>
    public string IncludeFile = "";
    /// <summary>The physical encodings in source order.</summary>
    public List<OrchestraEncoding> Encodings = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
