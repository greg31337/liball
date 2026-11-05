namespace odf;

/// <summary>
/// Describes an inert application-level database setting.
/// </summary>
public class OdfDatabaseSetting
{
    /// <summary>The setting name.</summary>
    public string Name = "";
    /// <summary>The setting value type token.</summary>
    public string Type = "";
    /// <summary>The preserved list-valued flag.</summary>
    public string IsList = "";
    /// <summary>The lexical values in document order.</summary>
    public string[] Values = [];
    /// <summary>The underlying data-source-setting element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalType = "";
    internal string OriginalIsList = "";
    internal string[] OriginalValues = [];
}
