using System;
using System.Collections.Generic;

namespace webp;

internal class WebpAlphaDecodeResult
{
    internal bool IsValid;
    internal WebpAlphaHeader Header;
    internal byte[] Alpha;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpAlphaDecodeResult()
    {
        IsValid = true;
        Header = new WebpAlphaHeader();
        Alpha = Array.Empty<byte>();
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
