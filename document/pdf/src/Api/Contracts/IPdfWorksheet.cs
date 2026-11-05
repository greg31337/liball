namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfWorksheet"/>.</summary>
public interface IPdfWorksheet
{
    /// <summary>Sets worksheet instructions.</summary>
    /// <param name="instructions">The instructions text.</param>
    void SetInstructions(string instructions);
    /// <summary>Adds a worksheet question.</summary>
    /// <param name="question">The question to add.</param>
    void AddQuestion(PdfWorksheetQuestion question);
    /// <summary>Sets footer text.</summary>
    /// <param name="footer">The footer text.</param>
    void SetFooter(string footer);
    /// <summary>Builds the worksheet.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
