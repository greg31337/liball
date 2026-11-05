namespace xml;

/// <summary>Provides the public XML parser implementation.</summary>
public class XmlParser : IXmlParser
{
    /// <summary>Creates an XML parser.</summary>
    public XmlParser()
    {
    }

    /// <summary>Parses a complete XML document.</summary>
    public XmlDocument ParseXmlDocument(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        return ExecuteXmlParse(input, options, resolver, reporter, cancellation, false);
    }

    /// <summary>Parses a balanced XML fragment.</summary>
    public XmlDocument ParseXmlFragment(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        return ExecuteXmlParse(input, options, resolver, reporter, cancellation, true);
    }

    internal XmlDocument ExecuteXmlParse(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation, bool fragment)
    {
        System.Threading.Tasks.Task<XmlDocument> pending = System.Threading.Tasks.Task.Run(() => new XmlParserEngine(input, options, resolver, reporter, cancellation, fragment).ParseXmlInput());
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        System.Exception failure = ReadXmlParseFailure(pending.Exception);
        string? optionalType = failure.GetType().FullName;
        string type = optionalType == null ? "" : optionalType;
        if (type == "xml.XmlException" || type == "xml.XmlWellFormednessException" || type == "xml.XmlNamespaceException" || type == "xml.XmlValidationException" || type == "xml.XmlResourceException" || type == "xml.XmlLimitException" || type == "System.ArgumentNullException" || type == "System.ArgumentException" || type == "System.OperationCanceledException")
        {
            throw failure;
        }

        if (type == "System.FormatException" || type == "System.Text.DecoderFallbackException" || type == "System.NotSupportedException" || type == "System.ArgumentOutOfRangeException")
        {
            throw CreateXmlParseBoundaryError(failure.Message);
        }

        throw failure;
    }

    internal System.Exception ReadXmlParseFailure(System.AggregateException? aggregate)
    {
        System.Exception failure = aggregate == null ? new System.InvalidOperationException("XML parsing failed without an exception.") : aggregate;
        while (failure.GetType().FullName == "System.AggregateException")
        {
            System.AggregateException current = (System.AggregateException)failure;
            if (current.InnerExceptions.Count == 0)
            {
                break;
            }

            failure = current.InnerExceptions[0];
        }

        return failure;
    }

    internal System.Exception CreateXmlParseBoundaryError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Message = message;
        if (IsXmlDtdValidityFailure(message))
        {
            diagnostic.Code = XmlErrorCodes.DtdValidity;
            diagnostic.Category = XmlErrorCategory.Validation;
            diagnostic.Severity = XmlDiagnosticSeverity.Error;
            return new XmlValidationException(message, diagnostic, null);
        }

        diagnostic.Code = XmlErrorCodes.MalformedMarkup;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        return new XmlWellFormednessException(message, diagnostic, null);
    }

    internal bool IsXmlDtdValidityFailure(string message)
    {
        return message.IndexOf("outside its physical entity", System.StringComparison.Ordinal) >= 0
            || message.IndexOf("inside parameter entity replacement text", System.StringComparison.Ordinal) >= 0
            || message.IndexOf("cannot occur more than once in a mixed-content declaration", System.StringComparison.Ordinal) >= 0
            || message.IndexOf("cannot contain duplicate tokens", System.StringComparison.Ordinal) >= 0
            || message.IndexOf("content model is not deterministic", System.StringComparison.Ordinal) >= 0;
    }
}
