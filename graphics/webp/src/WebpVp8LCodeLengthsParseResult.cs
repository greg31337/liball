using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8LCodeLengthsParseResult
{
    internal bool IsValid;
    internal int[] CodeLengths;
    internal int MaxSymbol;
    internal WebpVp8LPrefixCodeTable? Table;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LCodeLengthsParseResult()
    {
        IsValid = true;
        CodeLengths = Array.Empty<int>();
        MaxSymbol = 0;
        Table = null;
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
