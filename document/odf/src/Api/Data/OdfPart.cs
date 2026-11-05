namespace odf;

/// <summary>
/// Represents one materialized standard XML part or a flat XML document.
/// </summary>
public class OdfPart
{
    /// <summary>
    /// The package-relative part path, or an empty string for a flat document.
    /// </summary>
    public string Path = "";
    /// <summary>
    /// The media type associated with the part.
    /// </summary>
    public string MediaType = "";
    /// <summary>
    /// Top-level XML nodes in source order.
    /// </summary>
    public OdfNode[] Nodes = [];
    /// <summary>
    /// The standard office document element.
    /// </summary>
    public OdfNode? Root;
    /// <summary>
    /// The root local name.
    /// </summary>
    public string RootName = "";
    /// <summary>
    /// Direct element sections in source order.
    /// </summary>
    public OdfNode[] Sections = [];
    /// <summary>
    /// Direct sections retained without a standard mapping for this root.
    /// </summary>
    public OdfNode[] PassthroughNodes = [];
    /// <summary>
    /// The scripts section, when present.
    /// </summary>
    public OdfNode? Scripts;
    /// <summary>
    /// The font-face declarations section, when present.
    /// </summary>
    public OdfNode? FontFaceDeclarations;
    /// <summary>
    /// The named styles section, when present.
    /// </summary>
    public OdfNode? Styles;
    /// <summary>
    /// The automatic styles section, when present.
    /// </summary>
    public OdfNode? AutomaticStyles;
    /// <summary>
    /// The master styles section, when present.
    /// </summary>
    public OdfNode? MasterStyles;
    /// <summary>
    /// The metadata section, when present.
    /// </summary>
    public OdfNode? Metadata;
    /// <summary>
    /// The settings section, when present.
    /// </summary>
    public OdfNode? Settings;
    /// <summary>
    /// The document body section, when present.
    /// </summary>
    public OdfNode? Body;
    /// <summary>
    /// The version declared by this XML root, when present.
    /// </summary>
    public OdfVersionInfo? VersionInfo;
    /// <summary>
    /// Whether this part represents a flat <c>office:document</c> root.
    /// </summary>
    public bool IsFlatDocument;
    /// <summary>
    /// Whether the declared version is newer than the versions known by this SDK.
    /// </summary>
    public bool IsFutureVersion;
    internal OdfPartRootMap? RootMap;
}
