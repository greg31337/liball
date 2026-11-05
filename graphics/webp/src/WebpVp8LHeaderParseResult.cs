using System.Collections.Generic;

namespace webp;

internal class WebpVp8LHeaderParseResult
{
    internal bool IsValid;
    internal WebpVp8LHeader Header;
    internal WebpVp8LBitReader? Reader;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LHeaderParseResult()
    {
        IsValid = true;
        Header = new WebpVp8LHeader();
        Reader = null;
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
