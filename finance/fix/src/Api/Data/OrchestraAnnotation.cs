using System.Collections.Generic;

namespace fix;

/// <summary>Represents one purpose-qualified Orchestra annotation.</summary>
public class OrchestraAnnotation
{
    /// <summary>The source annotation kind, such as documentation or appinfo.</summary>
    public string Kind = "";
    /// <summary>The annotation purpose.</summary>
    public string Purpose = "";
    /// <summary>The content language.</summary>
    public string Language = "";
    /// <summary>The declared MIME content type.</summary>
    public string ContentType = "";
    /// <summary>The external specification URI.</summary>
    public string SpecificationUri = "";
    /// <summary>The annotation content.</summary>
    public string Content = "";
    /// <summary>The unmodified mixed XML content.</summary>
    public string XmlContent = "";
    /// <summary>The pedigree declared on the annotation container.</summary>
    public OrchestraPedigree ContainerPedigree = new OrchestraPedigree();
    /// <summary>The pedigree declared on this annotation item.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>Unknown or application-specific content retained losslessly.</summary>
    public List<OrchestraExtension> Extensions = [];
}
