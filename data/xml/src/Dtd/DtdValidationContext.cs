using System.Collections.Generic;

namespace xml;

internal class DtdValidationContext
{
    internal SourceTracker? Tracker;
    internal IXmlErrorReporter? ErrorReporter;
    internal bool AccumulateDiagnostics;
    internal List<XmlDiagnostic> Diagnostics;

    internal DtdValidationContext(SourceTracker? tracker, IXmlErrorReporter? errorReporter, bool accumulateDiagnostics)
    {
        Tracker = tracker;
        ErrorReporter = errorReporter;
        AccumulateDiagnostics = accumulateDiagnostics;
        Diagnostics = new List<XmlDiagnostic>();
    }

    internal void ReportDtdValidationError(string message, int offset)
    {
        SourceLocation location;
        SourceTracker? optionalTracker = Tracker;
        if (optionalTracker == null)
        {
            location = new SourceLocation();
            location.CharacterOffset = offset;
            location.ByteOffset = -1;
            location.Line = 1;
            location.Column = offset + 1;
        }
        else
        {
            location = optionalTracker.TrackedLocationAt(offset);
        }

        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.DtdValidity;
        diagnostic.Category = XmlErrorCategory.Validation;
        diagnostic.Severity = XmlDiagnosticSeverity.Error;
        diagnostic.Message = message;
        diagnostic.Range = range;
        Diagnostics.Add(diagnostic);
        IXmlErrorReporter? reporter = ErrorReporter;
        if (reporter != null)
        {
            reporter.ReportXmlDiagnostic(diagnostic);
        }

        if (!AccumulateDiagnostics)
        {
            throw new XmlValidationException(message, diagnostic, location);
        }
    }

    internal XmlDiagnostic[] SnapshotDtdValidationDiagnostics()
    {
        return Diagnostics.ToArray();
    }
}
