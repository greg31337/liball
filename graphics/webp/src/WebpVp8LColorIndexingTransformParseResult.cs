using System.Collections.Generic;

namespace webp;

internal class WebpVp8LColorIndexingTransformParseResult
{
    internal bool IsValid;
    internal WebpVp8LColorIndexingTransform Transform;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LColorIndexingTransformParseResult()
    {
        IsValid = true;
        Transform = new WebpVp8LColorIndexingTransform();
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
