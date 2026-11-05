using System.Collections.Generic;

namespace webp;

internal class WebpVp8LMetaPrefixParseResult
{
    internal bool IsValid;
    internal bool HasEntropyImage;
    internal WebpVp8LEntropyImage EntropyImage;
    internal List<WebpVp8LPrefixCodeGroup> PrefixCodeGroups;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LMetaPrefixParseResult()
    {
        IsValid = true;
        HasEntropyImage = false;
        EntropyImage = new WebpVp8LEntropyImage();
        PrefixCodeGroups = new List<WebpVp8LPrefixCodeGroup>();
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
