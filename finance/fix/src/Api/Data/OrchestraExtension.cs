namespace fix;

/// <summary>Retains one namespace-qualified extension node or attribute.</summary>
public class OrchestraExtension
{
    /// <summary>The extension namespace URI.</summary>
    public string NamespaceUri = "";
    /// <summary>The extension local name.</summary>
    public string Name = "";
    /// <summary>The source namespace prefix.</summary>
    public string Prefix = "";
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
