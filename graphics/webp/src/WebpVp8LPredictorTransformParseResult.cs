using System.Collections.Generic;

namespace webp;

internal class WebpVp8LPredictorTransformParseResult
{
    internal bool IsValid;
    internal WebpVp8LPredictorTransform Transform;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpVp8LPredictorTransformParseResult()
    {
        IsValid = true;
        Transform = new WebpVp8LPredictorTransform();
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
