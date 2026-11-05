namespace xml;

/// <summary>
/// Advances through normalized XML events using a pull cursor.
/// </summary>
public interface IXmlPullReader
{
    /// <summary>
    /// Starts a pull parse.
    /// </summary>
    /// <param name="input">The input to parse.</param>
    /// <param name="options">The parser options.</param>
    /// <param name="resolver">The optional resource resolver.</param>
    /// <param name="reporter">The optional diagnostic reporter.</param>
    /// <param name="cancellation">The optional cancellation source.</param>
    /// <returns>The initial reader state.</returns>
    XmlReaderState BeginXmlPull(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation);

    /// <summary>
    /// Advances to the next event.
    /// </summary>
    /// <returns>The new reader state.</returns>
    XmlReaderState AdvanceXmlPull();

    /// <summary>
    /// Gets the current reader state without advancing.
    /// </summary>
    /// <returns>The current reader state.</returns>
    XmlReaderState InspectXmlPull();

    /// <summary>
    /// Skips the current element subtree.
    /// </summary>
    /// <returns>The state after the subtree.</returns>
    XmlReaderState SkipXmlPullSubtree();

    /// <summary>
    /// Closes the reader and its owned input.
    /// </summary>
    void CloseXmlPull();
}
