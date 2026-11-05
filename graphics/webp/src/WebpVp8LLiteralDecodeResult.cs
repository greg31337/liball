using System.Collections.Generic;

namespace webp;

internal class WebpVp8LLiteralDecodeResult
{
    internal bool IsValid;
    internal bool IsLiteral;
    internal int GreenSymbol;
    internal WebpVp8LColor Color;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LLiteralDecodeResult()
    {
        IsValid = true;
        IsLiteral = false;
        GreenSymbol = 0;
        Color = new WebpVp8LColor();
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
