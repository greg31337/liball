using System.Collections.Generic;

namespace webp;

internal class WebpRiffParseResult
{
    internal bool IsValid;
    internal string RiffFourCc;
    internal string FormatFourCc;
    internal uint DeclaredSize;
    internal long DeclaredFileLength;
    internal List<WebpChunkInfo> Chunks;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpRiffParseResult()
    {
        IsValid = true;
        RiffFourCc = "";
        FormatFourCc = "";
        DeclaredSize = 0;
        DeclaredFileLength = 0;
        Chunks = new List<WebpChunkInfo>();
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
