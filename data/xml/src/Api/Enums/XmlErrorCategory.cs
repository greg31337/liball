namespace xml;

/// <summary>
/// Classifies an XML library error.
/// </summary>
public enum XmlErrorCategory
{
    /// <summary>
    /// Classifies a general library error.
    /// </summary>
    General = 0,
    /// <summary>
    /// Classifies an XML syntax or well-formedness error.
    /// </summary>
    Xml = 1,
    /// <summary>
    /// Classifies a namespace error.
    /// </summary>
    Namespace = 2,
    /// <summary>
    /// Classifies a document type definition error.
    /// </summary>
    Dtd = 3,
    /// <summary>
    /// Classifies a validity error.
    /// </summary>
    Validation = 4,
    /// <summary>
    /// Classifies a resource resolution error.
    /// </summary>
    Resource = 5,
    /// <summary>
    /// Classifies a security limit error.
    /// </summary>
    Limit = 6,
    /// <summary>
    /// Classifies an XInclude or XPointer error.
    /// </summary>
    Include = 7,
    /// <summary>
    /// Classifies a serialization error.
    /// </summary>
    Serialization = 8,
    /// <summary>
    /// Classifies a canonicalization error.
    /// </summary>
    Canonicalization = 9,
    /// <summary>
    /// Classifies an XML Schema error.
    /// </summary>
    Schema = 10,
    /// <summary>
    /// Classifies a static XPath error.
    /// </summary>
    XPathStatic = 11,
    /// <summary>
    /// Classifies an XPath type error.
    /// </summary>
    XPathType = 12,
    /// <summary>
    /// Classifies a dynamic XPath error.
    /// </summary>
    XPathDynamic = 13,
    /// <summary>
    /// Classifies a static XSLT error.
    /// </summary>
    XsltStatic = 14,
    /// <summary>
    /// Classifies an XSLT type error.
    /// </summary>
    XsltType = 15,
    /// <summary>
    /// Classifies a dynamic XSLT error.
    /// </summary>
    XsltDynamic = 16
}
