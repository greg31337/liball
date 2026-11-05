namespace iso20022;

/// <summary>Represents a prefix-independent XML qualified name while retaining its source prefix.</summary>
public class QualifiedNameData
{
    /// <summary>The namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The local name.</summary>
    public string LocalName = string.Empty;
    /// <summary>The source or preferred prefix.</summary>
    public string Prefix = string.Empty;
}
