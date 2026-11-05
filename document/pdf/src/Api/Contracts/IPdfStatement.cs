namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfStatement"/>.</summary>
public interface IPdfStatement
{
    /// <summary>Sets the statement period.</summary>
    /// <param name="startText">The period start.</param>
    /// <param name="endText">The period end.</param>
    void SetPeriod(string startText, string endText);
    /// <summary>Sets the opening balance.</summary>
    /// <param name="balance">The opening balance.</param>
    void SetOpeningBalance(double balance);
    /// <summary>Sets the displayed currency symbol.</summary>
    /// <param name="currencySymbol">The currency symbol.</param>
    void SetCurrencySymbol(string currencySymbol);
    /// <summary>Adds a statement transaction.</summary>
    /// <param name="entry">The transaction to add.</param>
    void AddEntry(PdfStatementEntry entry);
    /// <summary>Sets optional notes.</summary>
    /// <param name="notes">The notes text.</param>
    void SetNotes(string notes);
    /// <summary>Returns the calculated closing balance.</summary>
    /// <returns>The opening balance plus all entries.</returns>
    double GetClosingBalance();
    /// <summary>Builds the statement.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
