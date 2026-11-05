using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one locally available XSD artifact and its normalized registry identity and metadata.</summary>
public class SchemaArtifact
{
    /// <summary>The source artifact provenance and integrity metadata.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The logical source name used in diagnostics and local dependency resolution.</summary>
    public string SourceName = string.Empty;
    /// <summary>The normalized absolute base URI used to resolve local includes and imports.</summary>
    public string BaseUri = string.Empty;
    /// <summary>The exact target namespace, or an empty value for a no-namespace schema.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The exact ISO 20022 message identifier represented by the schema when applicable.</summary>
    public MessageIdentifier? MessageIdentifier;
    /// <summary>The exact independently versioned schema value used in the registry key.</summary>
    public string Version = string.Empty;
    /// <summary>The exact market-practice or processing profile used in the registry key.</summary>
    public string Profile = string.Empty;
    /// <summary>The expected document root element local name when known.</summary>
    public string RootElement = string.Empty;
    /// <summary>The immutable-by-convention encoded XSD content.</summary>
    public byte[] Content = System.Array.Empty<byte>();
    /// <summary>The explicit precedence used to resolve otherwise equivalent registration candidates.</summary>
    public int Precedence;
    /// <summary>Whether this schema is an entry point rather than only an included or imported dependency.</summary>
    public bool IsEntryPoint;
    /// <summary>The ordered schema-location values declared by XSD include elements.</summary>
    public List<string> IncludeLocations = new List<string>();
    /// <summary>The ordered namespace values declared by XSD import elements.</summary>
    public List<string> ImportNamespaceUris = new List<string>();
    /// <summary>The ordered schema-location values declared by XSD import elements.</summary>
    public List<string> ImportLocations = new List<string>();
    /// <summary>The global element qualified names declared by the schema.</summary>
    public List<QualifiedNameData> GlobalElements = new List<QualifiedNameData>();
    /// <summary>The global simple and complex type qualified names declared by the schema.</summary>
    public List<QualifiedNameData> GlobalTypes = new List<QualifiedNameData>();
    /// <summary>The normalized simple and schema data types, including their facets.</summary>
    public List<DataTypeDefinition> DataTypes = new List<DataTypeDefinition>();
    /// <summary>The normalized complex message components declared by the schema.</summary>
    public List<MessageComponentDefinition> MessageComponents = new List<MessageComponentDefinition>();
    /// <summary>The normalized local and global message elements declared by the schema.</summary>
    public List<MessageElementDefinition> MessageElements = new List<MessageElementDefinition>();
    /// <summary>The normalized reusable and inline choice groups declared by the schema.</summary>
    public List<ChoiceDefinition> Choices = new List<ChoiceDefinition>();
    /// <summary>The normalized wildcard content models declared directly by the schema.</summary>
    public List<ExternalSchemaDefinition> ExternalSchemas =
        new List<ExternalSchemaDefinition>();
    /// <summary>The normalized message definitions inferred from schema roots and namespaces.</summary>
    public List<MessageDefinitionMetadata> MessageDefinitions = new List<MessageDefinitionMetadata>();
    /// <summary>The repository-derived fallback metadata retained when the XSD omits semantic detail.</summary>
    public List<ExternalSchemaDefinition> RepositoryFallbackDefinitions =
        new List<ExternalSchemaDefinition>();
}
