using System.Collections.Generic;

namespace webp;

internal class WebpVp8LColorCacheParseResult
{
    internal bool IsValid;
    internal bool IsPresent;
    internal int CacheBits;
    internal int CacheSize;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LColorCacheParseResult()
    {
        IsValid = true;
        IsPresent = false;
        CacheBits = 0;
        CacheSize = 0;
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
