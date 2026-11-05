namespace odf;

/// <summary>
/// Provides standard OpenDocument package paths and directory names.
/// </summary>
public static class OdfPartNames
{
    /// <summary>
    /// The manifest path of the package root.
    /// </summary>
    public const string Root = "/";
    /// <summary>
    /// The package media-type entry.
    /// </summary>
    public const string Mimetype = "mimetype";
    /// <summary>
    /// The content XML part.
    /// </summary>
    public const string Content = "content.xml";
    /// <summary>
    /// The styles XML part.
    /// </summary>
    public const string Styles = "styles.xml";
    /// <summary>
    /// The metadata XML part.
    /// </summary>
    public const string Metadata = "meta.xml";
    /// <summary>
    /// The settings XML part.
    /// </summary>
    public const string Settings = "settings.xml";
    /// <summary>
    /// The package manifest part.
    /// </summary>
    public const string Manifest = "META-INF/manifest.xml";
    /// <summary>
    /// The RDF package metadata manifest.
    /// </summary>
    public const string MetadataManifest = "manifest.rdf";
    /// <summary>
    /// The document-signatures part.
    /// </summary>
    public const string DocumentSignatures = "META-INF/documentsignatures.xml";
    /// <summary>
    /// The macro-signatures part used by deployed producers.
    /// </summary>
    public const string MacroSignatures = "META-INF/macrosignatures.xml";
    /// <summary>
    /// The package-signatures part used by deployed producers.
    /// </summary>
    public const string PackageSignatures = "META-INF/packagesignatures.xml";
    /// <summary>
    /// The conventional document thumbnail part.
    /// </summary>
    public const string Thumbnail = "Thumbnails/thumbnail.png";
    /// <summary>
    /// The package metadata directory.
    /// </summary>
    public const string MetaInfDirectory = "META-INF/";
    /// <summary>
    /// The conventional image-resource directory.
    /// </summary>
    public const string PicturesDirectory = "Pictures/";
    /// <summary>
    /// The conventional thumbnail directory.
    /// </summary>
    public const string ThumbnailsDirectory = "Thumbnails/";
    /// <summary>
    /// The conventional embedded-object replacement directory.
    /// </summary>
    public const string ObjectReplacementsDirectory = "ObjectReplacements/";
    /// <summary>
    /// The conventional application-configuration directory.
    /// </summary>
    public const string ConfigurationsDirectory = "Configurations2/";
    /// <summary>
    /// The conventional scripts directory.
    /// </summary>
    public const string ScriptsDirectory = "Scripts/";
}
