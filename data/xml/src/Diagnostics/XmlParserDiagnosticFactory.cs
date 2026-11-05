namespace xml;

internal static class XmlParserDiagnosticFactory
{
    internal static XmlWellFormednessException CreateXmlWellFormednessError(string message, int offset, string? sourceUri, SourceTracker? tracker, IXmlErrorReporter? reporter)
    {
        string fullMessage = message + " at UTF-16 offset " + offset + ".";
        SourceLocation location;
        if (tracker == null)
        {
            location = new SourceLocation();
            location.SourceUri = sourceUri;
            location.CharacterOffset = offset;
            location.ByteOffset = -1;
            location.Line = 1;
            location.Column = offset + 1;
        }
        else
        {
            location = tracker.TrackedLocationAt(offset);
        }

        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.MalformedMarkup;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = fullMessage;
        diagnostic.Range = range;
        if (reporter != null)
        {
            reporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlWellFormednessException(fullMessage, diagnostic, location);
    }
}
