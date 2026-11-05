using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8LImageDataDecodeResult
{
    internal bool IsValid;
    internal int Width;
    internal int Height;
    internal WebpVp8LColor[] Pixels;
    internal WebpPixelBuffer PixelBuffer;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LImageDataDecodeResult()
    {
        IsValid = true;
        Width = 0;
        Height = 0;
        Pixels = Array.Empty<WebpVp8LColor>();
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
