namespace iso20022;

/// <summary>Defines caller or profile policy for one supplementary payload namespace.</summary>
public class SupplementaryDataPolicy
{
    /// <summary>The exact supplementary payload namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The exact local schema selected for this namespace when available.</summary>
    public SchemaArtifact? Schema;
    /// <summary>Whether content in this namespace may be ignored when it cannot be processed.</summary>
    public bool CanIgnore;
    /// <summary>Whether content in this namespace must be understood and accepted.</summary>
    public bool MustUnderstand;
    /// <summary>Whether available payload content must be validated against the selected schema.</summary>
    public bool ValidateSchema = true;
}
