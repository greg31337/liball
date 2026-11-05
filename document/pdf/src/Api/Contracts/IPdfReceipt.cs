using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfReceipt"/>.
/// </summary>
public interface IPdfReceipt
{
    /// <summary>
    /// Sets the receipt details used for subsequent PDF generation.
    /// </summary>
    /// <param name="receiptNumber">The receipt number to use for this operation.</param>
    /// <param name="dateText">The date text to use for this operation.</param>
    void SetReceiptDetails(string receiptNumber, string dateText);

    /// <summary>
    /// Sets the currency symbol used for subsequent PDF generation.
    /// </summary>
    /// <param name="currencySymbol">The currency symbol to use for this operation.</param>
    void SetCurrencySymbol(string currencySymbol);

    /// <summary>
    /// Sets the notes used for subsequent PDF generation.
    /// </summary>
    /// <param name="notes">The notes to use for this operation.</param>
    void SetNotes(string notes);

    /// <summary>
    /// Adds a line to this receipt.
    /// </summary>
    /// <param name="description">The description to use for this operation.</param>
    /// <param name="quantity">The quantity to use for this operation.</param>
    /// <param name="unitPrice">The unit price to use for this operation.</param>
    void AddLine(string description, double quantity, double unitPrice);

    /// <summary>
    /// Returns the subtotal calculated from the object's current state.
    /// </summary>
    /// <returns>The sum of each quantity multiplied by its unit price.</returns>
    double GetSubtotal();

    /// <summary>
    /// Builds a receipt from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
