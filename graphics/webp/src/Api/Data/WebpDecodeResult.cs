using System.Collections.Generic;

namespace webp;

/// <summary>Contains a decoded image model and any diagnostics produced while reading it.</summary>
public class WebpDecodeResult
{
    /// <summary>Indicates whether decoding completed without error-severity diagnostics.</summary>
    public bool IsValid;
    /// <summary>The partially or fully decoded image; it remains available when diagnostics report invalid input.</summary>
    public WebpImage Image;
    /// <summary>The warnings and errors encountered while parsing or decoding the input.</summary>
    public List<WebpDiagnostic> Diagnostics;

    /// <summary>Creates an initially valid result with an empty image and diagnostic collection.</summary>
    public WebpDecodeResult()
    {
        IsValid = true;
        Image = new WebpImage();
        Diagnostics = new List<WebpDiagnostic>();
    }

    /// <summary>Checks the current diagnostic collection for at least one error.</summary>
    /// <returns><c>true</c> when an error-severity diagnostic is present; otherwise <c>false</c>.</returns>
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
