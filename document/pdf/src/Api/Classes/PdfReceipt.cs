using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Represents a receipt used by the PDF generation API.
/// </summary>
public class PdfReceipt : IPdfReceipt
{
    /// <summary>
    /// Stores the merchant used by this object.
    /// </summary>
    internal string Merchant = string.Empty;
    /// <summary>
    /// Stores the receipt number used by this object.
    /// </summary>
    internal string ReceiptNumber = string.Empty;
    /// <summary>
    /// Stores the date text used by this object.
    /// </summary>
    internal string DateText = string.Empty;
    /// <summary>
    /// Stores the notes used by this object.
    /// </summary>
    internal string Notes = string.Empty;
    /// <summary>
    /// Stores the currency symbol used by this object.
    /// </summary>
    internal string CurrencySymbol = "$";
    internal List<string> _descriptions = new List<string>();
    internal List<double> _quantities = new List<double>();
    internal List<double> _unitPrices = new List<double>();
    /// <summary>
    /// Exposes the descriptions currently stored by this object.
    /// </summary>
    internal IReadOnlyList<string> Descriptions = new List<string>();
    internal bool Built;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();

    /// <summary>
    /// Initializes a new receipt.
    /// </summary>
    /// <param name="merchant">The merchant to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfReceipt(string merchant)
    {
        if (merchant == null)
        {
            throw new ArgumentNullException("merchant");
        }

        Merchant = merchant;
        ReceiptNumber = string.Empty;
        DateText = string.Empty;
        Notes = string.Empty;
        CurrencySymbol = "$";
        _descriptions = new List<string>();
        _quantities = new List<double>();
        _unitPrices = new List<double>();
        Descriptions = _descriptions;
        Built = false;
        Document = new PdfDocument();
    }

    /// <summary>
    /// Sets the receipt details used for subsequent PDF generation.
    /// </summary>
    /// <param name="receiptNumber">The receipt number to use for this operation.</param>
    /// <param name="dateText">The date text to use for this operation.</param>
    public void SetReceiptDetails(string receiptNumber, string dateText)
    {
        EnsureNotBuilt();
        ValidateText(receiptNumber, "receiptNumber");
        ValidateText(dateText, "dateText");
        ReceiptNumber = receiptNumber;
        DateText = dateText;
    }

    /// <summary>
    /// Sets the currency symbol used for subsequent PDF generation.
    /// </summary>
    /// <param name="currencySymbol">The currency symbol to use for this operation.</param>
    public void SetCurrencySymbol(string currencySymbol)
    {
        EnsureNotBuilt();
        ValidateText(currencySymbol, "currencySymbol");
        CurrencySymbol = currencySymbol;
    }

    /// <summary>
    /// Sets the notes used for subsequent PDF generation.
    /// </summary>
    /// <param name="notes">The notes to use for this operation.</param>
    public void SetNotes(string notes)
    {
        EnsureNotBuilt();
        ValidateText(notes, "notes");
        Notes = notes;
    }

    /// <summary>
    /// Adds a line to this receipt.
    /// </summary>
    /// <param name="description">The description to use for this operation.</param>
    /// <param name="quantity">The quantity to use for this operation.</param>
    /// <param name="unitPrice">The unit price to use for this operation.</param>
    public void AddLine(string description, double quantity, double unitPrice)
    {
        EnsureNotBuilt();
        ValidateText(description, "description");
        ValidateNotNegative(quantity, "quantity", "Receipt quantity cannot be negative.");
        ValidateNotNegative(unitPrice, "unitPrice", "Receipt unit price cannot be negative.");

        _descriptions.Add(description);
        _quantities.Add(quantity);
        _unitPrices.Add(unitPrice);
    }

    internal string FormatCurrency(double value)
    {
        return CurrencySymbol + value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the subtotal calculated from the object's current state.
    /// </summary>
    /// <returns>The sum of each quantity multiplied by its unit price.</returns>
    public double GetSubtotal()
    {
        double subtotal = 0;
        for (int index = 0; index < _descriptions.Count; index++)
        {
            subtotal += _quantities[index] * _unitPrices[index];
        }

        return subtotal;
    }

    /// <summary>
    /// Builds a receipt from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfLayoutDocument layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Receipt80(), new PdfPageMargins(10, 10, 10, 10));
            PdfParagraphStyle titleStyle = PdfParagraphStyle.Create(PdfTextStyle.WithFont(12, PdfFont.Bold), 15, 0, 0, 0, PdfTextAlignment.Center);
            PdfParagraphStyle textStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(8, PdfFont.Regular), 10, 0, 0, 0);
            PdfParagraphStyle boldStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(8, PdfFont.Bold), 10, 0, 0, 0);

            layout.AddTextBlock(new PdfTextBlock(Merchant, titleStyle));
            if (ReceiptNumber.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("Receipt " + ReceiptNumber, textStyle));
            }

            if (DateText.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock(DateText, textStyle));
            }

            layout.AddBlock(PdfBlock.WithSize(0, 6));
            AddLines(layout, textStyle, boldStyle);
            layout.AddBlock(PdfBlock.WithSize(0, 6));
            layout.AddTextBlock(new PdfTextBlock("Total " + FormatCurrency(GetSubtotal()), boldStyle));
            if (Notes.Length > 0)
            {
                layout.AddBlock(PdfBlock.WithSize(0, 6));
                layout.AddTextBlock(new PdfTextBlock(Notes, textStyle));
            }

            layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void AddLines(PdfLayoutDocument layout, PdfParagraphStyle textStyle, PdfParagraphStyle boldStyle)
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 116, 34, 56 });
        PdfTableRow header = table.AddHeaderRow();
        header.AddTextBlock(new PdfTextBlock("Item", boldStyle));
        header.AddTextBlock(new PdfTextBlock("Qty", boldStyle));
        header.AddTextBlock(new PdfTextBlock("Total", boldStyle));

        for (int index = 0; index < _descriptions.Count; index++)
        {
            PdfTableRow row = table.AddRow();
            row.AddTextBlock(new PdfTextBlock(_descriptions[index], textStyle));
            row.AddTextBlock(new PdfTextBlock(_quantities[index].ToString("0.##", CultureInfo.InvariantCulture), textStyle));
            row.AddTextBlock(new PdfTextBlock(FormatCurrency(_quantities[index] * _unitPrices[index]), textStyle));
        }

        layout.AddTableBlock(table);
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal void ValidateNotNegative(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Receipt has already been built.");
        }
    }
}
