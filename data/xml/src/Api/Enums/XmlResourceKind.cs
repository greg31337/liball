namespace xml;

/// <summary>
/// Identifies the purpose of an external resource.
/// </summary>
public enum XmlResourceKind
{
    /// <summary>
    /// Identifies an XML document.
    /// </summary>
    Document = 0,
    /// <summary>
    /// Identifies an external DTD subset.
    /// </summary>
    ExternalSubset = 1,
    /// <summary>
    /// Identifies an external general entity.
    /// </summary>
    GeneralEntity = 2,
    /// <summary>
    /// Identifies an external parameter entity.
    /// </summary>
    ParameterEntity = 3,
    /// <summary>
    /// Identifies an XML XInclude resource.
    /// </summary>
    XIncludeXml = 4,
    /// <summary>
    /// Identifies a text XInclude resource.
    /// </summary>
    XIncludeText = 5,
    /// <summary>
    /// Identifies an XML Catalog.
    /// </summary>
    Catalog = 6,
    /// <summary>
    /// Identifies an XML Schema document.
    /// </summary>
    Schema = 7,
    /// <summary>
    /// Identifies an XSLT stylesheet module.
    /// </summary>
    Stylesheet = 8,
    /// <summary>
    /// Identifies an XSLT package.
    /// </summary>
    Package = 9,
    /// <summary>
    /// Identifies an unparsed text resource.
    /// </summary>
    UnparsedText = 10,
    /// <summary>
    /// Identifies a collection resource.
    /// </summary>
    Collection = 11
}
