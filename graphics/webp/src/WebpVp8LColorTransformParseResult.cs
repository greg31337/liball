using System.Collections.Generic;

namespace webp;

internal class WebpVp8LColorTransformParseResult
{
    internal bool IsValid;
    internal WebpVp8LColorTransform Transform;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LColorTransformParseResult()
    {
        IsValid = true;
        Transform = new WebpVp8LColorTransform();
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
