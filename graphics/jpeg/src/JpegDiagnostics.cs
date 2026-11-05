using System.Collections.Generic;

namespace jpeg;

internal class JpegDiagnosticCollection
{
    internal List<JpegDiagnostic> Items = new List<JpegDiagnostic>();
    internal int InfoCount;
    internal int WarningCount;
    internal int ErrorCount;
    internal bool HasWarnings;
    internal bool HasErrors;

    internal JpegDiagnostic Add(JpegDiagnosticSeverity severity, JpegDiagnosticCode code, string? message, long offset, JpegMarker marker)
    {
        JpegDiagnostic diagnostic = new JpegDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        if (message == null)
        {
            diagnostic.Message = "";
        }
        else
        {
            diagnostic.Message = message;
        }

        diagnostic.Offset = offset;
        diagnostic.Marker = marker;
        Items.Add(diagnostic);
        CountSeverity(severity);
        return diagnostic;
    }

    internal JpegDiagnostic AddInfo(JpegDiagnosticCode code, string? message, long offset, JpegMarker marker)
    {
        return Add(JpegDiagnosticSeverity.Info, code, message, offset, marker);
    }

    internal JpegDiagnostic AddWarning(JpegDiagnosticCode code, string? message, long offset, JpegMarker marker)
    {
        return Add(JpegDiagnosticSeverity.Warning, code, message, offset, marker);
    }

    internal JpegDiagnostic AddError(JpegDiagnosticCode code, string? message, long offset, JpegMarker marker)
    {
        return Add(JpegDiagnosticSeverity.Error, code, message, offset, marker);
    }

    internal void CopyTo(List<JpegDiagnostic> target)
    {
        if (target == null)
        {
            throw new System.ArgumentNullException("target");
        }

        foreach (JpegDiagnostic diagnostic in Items)
        {
            target.Add(diagnostic);
        }
    }

    internal void Clear()
    {
        Items.Clear();
        InfoCount = 0;
        WarningCount = 0;
        ErrorCount = 0;
        HasWarnings = false;
        HasErrors = false;
    }

    internal void CountSeverity(JpegDiagnosticSeverity severity)
    {
        if (severity == JpegDiagnosticSeverity.Info)
        {
            InfoCount = InfoCount + 1;
            return;
        }

        if (severity == JpegDiagnosticSeverity.Warning)
        {
            WarningCount = WarningCount + 1;
            HasWarnings = true;
            return;
        }

        if (severity == JpegDiagnosticSeverity.Error)
        {
            ErrorCount = ErrorCount + 1;
            HasErrors = true;
        }
    }
}
