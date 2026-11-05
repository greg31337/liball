using System.Collections.Generic;

namespace webp;

internal class WebpVp8LDecodeResult
{
    internal bool IsValid;
    internal WebpVp8LHeader Header;
    internal WebpPixelBuffer PixelBuffer;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LDecodeResult()
    {
        IsValid = true;
        Header = new WebpVp8LHeader();
        PixelBuffer = new WebpPixelBuffer();
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
