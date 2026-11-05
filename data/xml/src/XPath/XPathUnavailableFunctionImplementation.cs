namespace xml;

internal class XPathUnavailableFunctionImplementation : IXPathFunction
{
    internal string DisplayName;

    internal XPathUnavailableFunctionImplementation(string displayName)
    {
        DisplayName = displayName;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string message = "No runtime implementation is available for XPath function " + DisplayName + ".";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPDY0002;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        throw new XPathDynamicException(message, diagnostic, null);
    }
}
