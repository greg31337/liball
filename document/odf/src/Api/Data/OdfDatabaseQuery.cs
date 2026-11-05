namespace odf;

/// <summary>
/// Describes an inert database query or query collection.
/// </summary>
public class OdfDatabaseQuery
{
    /// <summary>Whether this item is a query collection.</summary>
    public bool IsCollection;
    /// <summary>The query or collection name.</summary>
    public string Name = "";
    /// <summary>The human-readable title.</summary>
    public string Title = "";
    /// <summary>The human-readable description.</summary>
    public string Description = "";
    /// <summary>The query command text.</summary>
    public string Command = "";
    /// <summary>The preserved escape-processing flag.</summary>
    public string EscapeProcessing = "";
    /// <summary>The optional order statement.</summary>
    public string OrderStatement = "";
    /// <summary>The optional filter statement.</summary>
    public string FilterStatement = "";
    /// <summary>The nested queries and collections.</summary>
    public OdfDatabaseQuery[] Children = [];
    /// <summary>The underlying query or collection element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalTitle = "";
    internal string OriginalDescription = "";
    internal string OriginalCommand = "";
    internal string OriginalEscapeProcessing = "";
    internal string OriginalOrderStatement = "";
    internal string OriginalFilterStatement = "";
}
