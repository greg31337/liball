namespace xml;

/// <summary>
/// Carries an immutable compiled XML Schema set handle.
/// </summary>
public class XmlSchemaSet
{
    internal CompiledXmlSchemaSet? CompiledSchemaSet;

    /// <summary>
    /// The schema documents.
    /// </summary>
    public XmlDocument[]? SchemaDocuments;
    /// <summary>
    /// The compiled target namespaces.
    /// </summary>
    public string[]? TargetNamespaces;
    /// <summary>
    /// The internal immutable compiled-set handle.
    /// </summary>
    public object? CompiledSet;
    /// <summary>
    /// The schema compilation diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// Whether schema compilation completed and the set is immutable.
    /// </summary>
    public bool IsImmutable;
}
