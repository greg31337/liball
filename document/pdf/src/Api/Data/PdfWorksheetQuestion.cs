using System;

namespace pdf;

/// <summary>Stores one worksheet prompt and its response space.</summary>
public class PdfWorksheetQuestion
{
    /// <summary>Stores the question prompt.</summary>
    public string Prompt = string.Empty;
    /// <summary>Stores the number of ruled response lines.</summary>
    public int ResponseLines;

    /// <summary>Initializes a worksheet question.</summary>
    /// <param name="prompt">The question prompt.</param>
    /// <param name="responseLines">The positive number of response lines.</param>
    public PdfWorksheetQuestion(string prompt, int responseLines)
    {
        if (prompt == null) { throw new ArgumentNullException("prompt"); }
        if (responseLines <= 0) { throw new ArgumentOutOfRangeException("responseLines", "Worksheet response lines must be positive."); }
        Prompt = prompt;
        ResponseLines = responseLines;
    }
}
