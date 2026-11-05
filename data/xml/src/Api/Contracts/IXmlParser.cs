namespace xml;

/// <summary>
/// Parses complete XML documents and balanced fragments.
/// </summary>
public interface IXmlParser
{
    /// <summary>
    /// Parses one complete XML document.
    /// </summary>
    /// <param name="input">The XML input.</param>
    /// <param name="options">The parser options.</param>
    /// <param name="resolver">The optional external resource resolver.</param>
    /// <param name="reporter">The optional diagnostic reporter.</param>
    /// <param name="cancellation">The optional cancellation source.</param>
    /// <returns>The parsed document.</returns>
    XmlDocument ParseXmlDocument(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation);

    /// <summary>
    /// Parses one balanced XML fragment.
    /// </summary>
    /// <param name="input">The XML fragment input.</param>
    /// <param name="options">The parser options and inherited fragment context.</param>
    /// <param name="resolver">The optional external resource resolver.</param>
    /// <param name="reporter">The optional diagnostic reporter.</param>
    /// <param name="cancellation">The optional cancellation source.</param>
    /// <returns>A document container holding the parsed fragment.</returns>
    XmlDocument ParseXmlFragment(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation);
}

