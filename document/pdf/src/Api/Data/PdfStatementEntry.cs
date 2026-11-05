using System;

namespace pdf;

/// <summary>Stores one account-statement transaction.</summary>
public class PdfStatementEntry
{
    /// <summary>Stores the displayed transaction date.</summary>
    public string DateText = string.Empty;
    /// <summary>Stores the transaction description.</summary>
    public string Description = string.Empty;
    /// <summary>Stores the signed transaction amount.</summary>
    public double Amount;

    /// <summary>Initializes a statement entry.</summary>
    /// <param name="dateText">The displayed transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The signed transaction amount.</param>
    public PdfStatementEntry(string dateText, string description, double amount)
    {
        if (dateText == null) { throw new ArgumentNullException("dateText"); }
        if (description == null) { throw new ArgumentNullException("description"); }
        if (double.IsNaN(amount) || double.IsInfinity(amount)) { throw new ArgumentOutOfRangeException("amount", "Statement amount must be finite."); }
        DateText = dateText;
        Description = description;
        Amount = amount;
    }
}
