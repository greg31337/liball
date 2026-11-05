namespace fix;

/// <summary>Retains one ordered Dublin Core or extension metadata term.</summary>
public class OrchestraMetadataTerm
{
    /// <summary>The term namespace URI.</summary>
    public string NamespaceUri = "";
    /// <summary>The term local name.</summary>
    public string Name = "";
    /// <summary>The content language.</summary>
    public string Language = "";
    /// <summary>The text value.</summary>
    public string Value = "";
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
