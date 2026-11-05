namespace iso20022;

/// <summary>Identifies the role and serialization format of one generated output file.</summary>
public enum GenerationFileKind
{
    /// <summary>The generated-file role is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>An XML Schema 1.0 document.</summary>
    Xsd = 1,
    /// <summary>A JSON Schema Draft 2020-12 document.</summary>
    JsonSchema = 2,
    /// <summary>A generated C# source file.</summary>
    CSharp = 3,
    /// <summary>An internal metadata, codec, factory, or registry source file.</summary>
    Metadata = 4,
    /// <summary>A deterministic generation manifest file.</summary>
    Manifest = 5
}
