namespace odf;

/// <summary>Describes a dependency on another tracked spreadsheet change.</summary>
public class OdfSpreadsheetChangeDependency
{
    /// <summary>The referenced change identifier.</summary>
    public string Id = "";
    /// <summary>The underlying <c>table:dependency</c> element.</summary>
    public OdfNode? Node;
}
