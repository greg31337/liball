namespace odf;

/// <summary>
/// Describes an inert database resource, file, or server connection.
/// </summary>
public class OdfDatabaseConnection
{
    /// <summary>The connection description kind.</summary>
    public OdfDatabaseConnectionKind Kind;
    /// <summary>The connection or file resource IRI.</summary>
    public string Href = "";
    /// <summary>The XLink type token.</summary>
    public string LinkType = "";
    /// <summary>The XLink show behavior.</summary>
    public string Show = "";
    /// <summary>The XLink actuate behavior.</summary>
    public string Actuate = "";
    /// <summary>The file-based database media type.</summary>
    public string MediaType = "";
    /// <summary>The file-based database filename extension.</summary>
    public string Extension = "";
    /// <summary>The server database type or driver token.</summary>
    public string DatabaseType = "";
    /// <summary>The server host name.</summary>
    public string HostName = "";
    /// <summary>The lexical server port.</summary>
    public string Port = "";
    /// <summary>The server local-socket path.</summary>
    public string LocalSocket = "";
    /// <summary>The server database name.</summary>
    public string DatabaseName = "";
    /// <summary>Whether the resource IRI is external to the package.</summary>
    public bool IsExternal;
    /// <summary>The resolved package entry path, when package-relative.</summary>
    public string PackagePath = "";
    /// <summary>Whether the resolved package entry exists.</summary>
    public bool PackageEntryExists;
    /// <summary>The underlying connection description element.</summary>
    public OdfNode? Node;
    internal string OriginalHref = "";
    internal string OriginalLinkType = "";
    internal string OriginalShow = "";
    internal string OriginalActuate = "";
    internal string OriginalMediaType = "";
    internal string OriginalExtension = "";
    internal string OriginalDatabaseType = "";
    internal string OriginalHostName = "";
    internal string OriginalPort = "";
    internal string OriginalLocalSocket = "";
    internal string OriginalDatabaseName = "";
}
