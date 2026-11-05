using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a natural-language or formally expressed repository constraint.</summary>
public class ConstraintDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The repository constraint name.</summary>
    public string Name = string.Empty;
    /// <summary>The natural-language definition of the constraint.</summary>
    public string Definition = string.Empty;
    /// <summary>The formal expression when one is supplied.</summary>
    public string Expression = string.Empty;
    /// <summary>The language used by the formal expression.</summary>
    public string ExpressionLanguage = string.Empty;
    /// <summary>The repository concept that owns the constraint.</summary>
    public RepositoryReference Owner = new RepositoryReference();
    /// <summary>The registration and lifecycle metadata for the constraint.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The semantic markup attached directly to the constraint.</summary>
    public List<SemanticMarkupData> SemanticMarkup = new List<SemanticMarkupData>();
    /// <summary>Unrecognized constraint content.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the constraint.</summary>
    public SourceLocation Location = new SourceLocation();
}
