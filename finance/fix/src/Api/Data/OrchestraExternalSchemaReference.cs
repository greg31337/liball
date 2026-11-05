namespace fix;

/// <summary>Retains an external schema reference discovered in encoding-specific content.</summary>
public class OrchestraExternalSchemaReference
{
    /// <summary>The schema reference element name.</summary>
    public string Name = "";
    /// <summary>The referenced or target namespace URI.</summary>
    public string NamespaceUri = "";
    /// <summary>The external schema location.</summary>
    public string Location = "";
    /// <summary>The complete source XML for the reference.</summary>
    public string Xml = "";
}
