namespace odf;

/// <summary>
/// Defines resource limits applied while reading and processing OpenDocument content.
/// </summary>
public class OdfSecurityLimits
{
    /// <summary>
    /// The maximum number of entries permitted in a package.
    /// </summary>
    public int MaximumPackageEntries = 10000;
    /// <summary>
    /// The maximum total number of expanded package bytes.
    /// </summary>
    public long MaximumTotalExpandedBytes = 1073741824;
    /// <summary>
    /// The maximum expanded size of one package entry.
    /// </summary>
    public long MaximumSingleEntryBytes = 268435456;
    /// <summary>
    /// The maximum expanded-to-compressed size ratio of an entry.
    /// </summary>
    public int MaximumCompressionRatio = 100;
    /// <summary>
    /// The maximum package entry path length.
    /// </summary>
    public int MaximumPathLength = 4096;
    /// <summary>
    /// The maximum permitted XML element depth.
    /// </summary>
    public int MaximumXmlDepth = 256;
    /// <summary>
    /// The maximum number of attributes permitted on one XML element.
    /// </summary>
    public int MaximumXmlAttributesPerElement = 1024;
    /// <summary>
    /// The maximum length of one XML text value.
    /// </summary>
    public int MaximumXmlTextLength = 67108864;
    /// <summary>
    /// The maximum permitted formula syntax depth.
    /// </summary>
    public int MaximumFormulaDepth = 256;
    /// <summary>
    /// The maximum depth of recursively embedded packages.
    /// </summary>
    public int MaximumEmbeddedPackageDepth = 8;
}
