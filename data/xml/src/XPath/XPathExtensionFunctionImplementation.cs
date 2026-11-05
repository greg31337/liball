using System.Threading.Tasks;

namespace xml;

internal class XPathExtensionFunctionResolution
{
    internal IXPathFunction? Implementation;

    internal XPathExtensionFunctionResolution(IXPathFunction? implementation)
    {
        Implementation = implementation;
    }
}

internal class XPathExtensionFunctionImplementation : IXPathFunction
{
    internal IXPathFunction Implementation;
    internal XmlExtensionPolicy Policy;

    internal XPathExtensionFunctionImplementation(IXPathFunction implementation, XmlExtensionPolicy policy)
    {
        Implementation = implementation;
        Policy = policy;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XmlExtensionRuntimeState state = context.ExtensionState == null ? new XmlExtensionRuntimeState() : context.ExtensionState;
        context.ExtensionState = state;
        if (context.Cancellation != null && context.Cancellation.IsXmlCancellationRequested())
        {
            throw new System.OperationCanceledException("XPath extension execution was canceled.");
        }

        if (!Policy.AllowReentrancy && state.Depth > 0)
        {
            throw CreateXPathExtensionError("Reentrant extension execution is denied by policy.");
        }

        if (state.InvocationCount >= Policy.MaximumInvocations)
        {
            throw CreateXPathExtensionError("The extension invocation limit was exceeded.");
        }

        state.InvocationCount = state.InvocationCount + 1;
        state.Depth = state.Depth + 1;
        Task<XdmSequence> pending = Task.Run(() => Implementation.InvokeXPathFunction(binding, arguments, context));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        state.Depth = state.Depth - 1;
        if (pending.Status == TaskStatus.RanToCompletion && pending.Result != null)
        {
            if (context.Cancellation != null && context.Cancellation.IsXmlCancellationRequested())
            {
                throw new System.OperationCanceledException("XPath extension execution was canceled.");
            }

            return pending.Result;
        }

        string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
        throw CreateXPathExtensionError("The extension function failed: " + detail);
    }

    internal XPathDynamicException CreateXPathExtensionError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.FOER0000;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
