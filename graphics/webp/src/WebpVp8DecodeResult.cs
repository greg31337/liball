using System.Collections.Generic;

namespace webp;

internal class WebpVp8DecodeResult
{
    internal bool IsValid;
    internal WebpVp8FrameHeader FrameHeader;
    internal WebpVp8FrameHeaderData HeaderData;
    internal WebpVp8IntraModeParseResult ModeResult;
    internal WebpPixelBuffer PixelBuffer;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8DecodeResult()
    {
        IsValid = true;
        FrameHeader = new WebpVp8FrameHeader();
        HeaderData = new WebpVp8FrameHeaderData();
        ModeResult = new WebpVp8IntraModeParseResult();
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
