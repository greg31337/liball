namespace iso20022;

/// <summary>Maps one exact repository or XSD concept to its XML, C#, shape, and output identities.</summary>
public class GenerationSymbol
{
    /// <summary>The stable repository XMI or schema declaration identifier.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The exact source concept or declaration type name.</summary>
    public string SourceType = string.Empty;
    /// <summary>The normalized repository entity kind when the source is a repository concept.</summary>
    public RepositoryEntityKind RepositoryKind;
    /// <summary>The source artifact name or URI.</summary>
    public string SourceName = string.Empty;
    /// <summary>The exact source XML namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The exact source or generated XML local name.</summary>
    public string XmlName = string.Empty;
    /// <summary>The generated C# namespace.</summary>
    public string CSharpNamespace = string.Empty;
    /// <summary>The generated C# identifier, including deterministic collision disambiguation.</summary>
    public string CSharpName = string.Empty;
    /// <summary>The deterministic structural shape identifier.</summary>
    public string ShapeIdentifier = string.Empty;
    /// <summary>The generated file path that owns the symbol.</summary>
    public string OutputPath = string.Empty;
    /// <summary>The zero-based stable order within the complete generated symbol table.</summary>
    public int Order;
    /// <summary>Whether the generated C# name required deterministic collision disambiguation.</summary>
    public bool HasCollision;
}
