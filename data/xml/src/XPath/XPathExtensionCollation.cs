using System.Threading.Tasks;

namespace xml;

internal class XPathExtensionCollation : IXPathCollation
{
    internal IXPathCollation Implementation;

    internal XPathExtensionCollation(IXPathCollation implementation)
    {
        Implementation = implementation;
    }

    public int CompareXPathCollation(string left, string right)
    {
        Task<int> pending = Task.Run(() => Implementation.CompareXPathCollation(left, right));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
        throw CreateXPathCollationExtensionError("The extension collation comparison failed: " + detail);
    }

    public byte[] CreateXPathCollationKey(string value)
    {
        Task<byte[]> pending = Task.Run(() => Implementation.CreateXPathCollationKey(value));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion && pending.Result != null)
        {
            return pending.Result;
        }

        string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
        throw CreateXPathCollationExtensionError("The extension collation key operation failed: " + detail);
    }

    internal XPathDynamicException CreateXPathCollationExtensionError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.FOER0000;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
