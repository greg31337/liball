namespace xml;

/// <summary>Provides the public XSLT extension-instruction adapter shell.</summary>
public class XsltExtensionInstructionAdapter : IXsltExtensionInstruction
{
    /// <summary>Creates an XSLT extension-instruction adapter.</summary>
    public XsltExtensionInstructionAdapter()
    {
    }

    /// <summary>Executes an XSLT extension instruction.</summary>
    public XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context)
    {
        return content;
    }
}
