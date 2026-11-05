using System;

namespace html;

internal class HtmlResultDiagnostics
{
    internal static HtmlDiagnosticCode GetPrimaryCode(
        HtmlDiagnostic[] diagnostics, HtmlDiagnosticCode fallback)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            HtmlDiagnostic diagnostic = diagnostics[index];
            if (diagnostic == null)
            {
                throw new ArgumentException(
                    "A diagnostic collection cannot contain null.",
                    "diagnostics");
            }

            if (diagnostic.GetSeverity() == HtmlDiagnosticSeverity.Error
                || diagnostic.GetSeverity() == HtmlDiagnosticSeverity.Fatal)
            {
                return diagnostic.GetCode();
            }

            index = index + 1;
        }

        if (diagnostics.Length > 0)
        {
            return diagnostics[0].GetCode();
        }

        return fallback;
    }

    internal static long GetTotalCount(int retainedCount,
        long droppedCount)
    {
        return (long)retainedCount + droppedCount;
    }

    internal static bool ContainsCode(HtmlDiagnostic[] diagnostics,
        HtmlDiagnosticCode code)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetCode() == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
