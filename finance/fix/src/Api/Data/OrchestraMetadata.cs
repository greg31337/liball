using System.Collections.Generic;

namespace fix;

/// <summary>Contains repository identity, version, and publication metadata.</summary>
public class OrchestraMetadata
{
    /// <summary>The repository name.</summary>
    public string Name = "";
    /// <summary>The FIX version represented by the repository.</summary>
    public string FixVersion = "";
    /// <summary>The publication release label.</summary>
    public string Release = "";
    /// <summary>Whether an extension-pack number is present.</summary>
    public bool HasExtensionPack;
    /// <summary>The extension-pack number.</summary>
    public int ExtensionPack;
    /// <summary>The publication date in source lexical form.</summary>
    public string PublicationDate = "";
    /// <summary>The source namespace URI.</summary>
    public string NamespaceUri = "";
    /// <summary>The namespace associated with repository definitions.</summary>
    public string RepositoryNamespace = "";
    /// <summary>The external repository specification URI.</summary>
    public string SpecificationUri = "";
    /// <summary>The ordered Dublin Core and extension metadata terms.</summary>
    public List<OrchestraMetadataTerm> Terms = [];
}
