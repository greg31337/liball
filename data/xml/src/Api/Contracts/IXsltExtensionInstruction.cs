namespace xml;

/// <summary>
/// Executes an XSLT extension instruction using data-only inputs and outputs.
/// </summary>
public interface IXsltExtensionInstruction
{
    /// <summary>
    /// Executes one registered extension instruction.
    /// </summary>
    /// <param name="binding">The registered extension binding.</param>
    /// <param name="content">The evaluated instruction content.</param>
    /// <param name="context">The dynamic XPath context.</param>
    /// <returns>The extension result sequence.</returns>
    XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context);
}
