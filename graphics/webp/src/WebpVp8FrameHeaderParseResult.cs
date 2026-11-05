using System.Collections.Generic;

namespace webp;

internal class WebpVp8FrameHeaderParseResult
{
    internal bool IsValid;
    internal WebpVp8FrameHeader Header;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8FrameHeaderParseResult()
    {
        IsValid = true;
        Header = new WebpVp8FrameHeader();
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
