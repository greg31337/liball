using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes convergence or reverse-engineering relationships to an industry message model.</summary>
public class ConvergenceDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The convergence documentation name.</summary>
    public string Name = string.Empty;
    /// <summary>The purpose and scope of the convergence documentation.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the documentation.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The industry message sets participating in the convergence documentation.</summary>
    public List<RepositoryReference> IndustryMessageSets = new List<RepositoryReference>();
    /// <summary>The ordered external or earlier-standard source items.</summary>
    public List<RepositoryReference> SourceItems = new List<RepositoryReference>();
    /// <summary>The ordered ISO 20022 target items.</summary>
    public List<RepositoryReference> TargetItems = new List<RepositoryReference>();
    /// <summary>Structured semantic markup describing the mappings.</summary>
    public List<SemanticMarkupData> SemanticMarkup = new List<SemanticMarkupData>();
    /// <summary>Unrecognized convergence fields and future mapping forms.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the convergence documentation.</summary>
    public SourceLocation Location = new SourceLocation();
}
