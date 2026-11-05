namespace xml;

/// <summary>
/// Applies XInclude and XPointer processing to an XML document.
/// </summary>
public interface IXmlIncludeProcessor
{
    /// <summary>
    /// Processes includes in a document.
    /// </summary>
    /// <param name="document">The document to process.</param>
    /// <param name="options">The include and fixup options.</param>
    /// <param name="resolver">The external resource resolver.</param>
    /// <returns>The processed document.</returns>
    XmlDocument ProcessXmlIncludes(XmlDocument document, XmlIncludeOptions options, IXmlResourceResolver resolver);
}

