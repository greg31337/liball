using System.Collections.Generic;

namespace webp;

internal class WebpVp8XParseResult
{
    internal bool IsValid;
    internal WebpVp8XHeader Header;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8XParseResult()
    {
        IsValid = true;
        Header = new WebpVp8XHeader();
        Diagnostics = new List<WebpDiagnostic>();
    }

    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].Severity == WebpDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
