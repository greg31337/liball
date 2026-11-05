using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfInvoice"/>.
/// </summary>
public interface IPdfInvoice
{
    /// <summary>Returns the underlying invoice document for resource and metadata configuration.</summary>
    /// <returns>The invoice document.</returns>
    PdfDocument GetDocument();

    /// <summary>Selects invoice or credit-note rendering.</summary>
    /// <param name="documentType">The business document type.</param>
    void SetDocumentType(PdfInvoiceDocumentType documentType);

    /// <summary>Sets the original invoice reference and reason displayed by a credit note.</summary>
    /// <param name="originalInvoiceNumber">The credited invoice number.</param>
    /// <param name="reason">The reason for issuing the credit.</param>
    void SetCreditNoteReference(string originalInvoiceNumber, string reason);

    /// <summary>Uses a registered embeddable TrueType font for all invoice text.</summary>
    /// <param name="font">The font registered with the invoice document.</param>
    void SetEmbeddedFont(PdfTrueTypeFont font);

    /// <summary>Configures caller-supplied Factur-X/ZUGFeRD XML and automatic PDF/A-3b preparation.</summary>
    /// <param name="metadata">The standardized hybrid-invoice profile metadata.</param>
    /// <param name="xmlData">The complete caller-supplied UN/CEFACT CII XML bytes.</param>
    /// <param name="pdfAOptions">PDF/A-3b output-intent options with a complete ICC profile.</param>
    void SetFacturXData(PdfFacturXMetadata metadata, byte[] xmlData, PdfAPreparationOptions pdfAOptions);

    /// <summary>
    /// Sets the seller used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetSeller(PdfParty party);

    /// <summary>
    /// Sets the buyer used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetBuyer(PdfParty party);

    /// <summary>
    /// Sets the shipping used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetShipping(PdfParty party);

    /// <summary>Selects the seller, buyer, and shipping layout.</summary>
    /// <param name="layout">The party layout.</param>
    void SetPartyLayout(PdfInvoicePartyLayout layout);

    /// <summary>
    /// Sets the invoice details used for subsequent PDF generation.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number to use for this operation.</param>
    /// <param name="invoiceDate">The invoice date to use for this operation.</param>
    /// <param name="dueDate">The due date to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetInvoiceDetails(string invoiceNumber, string invoiceDate, string dueDate);

    /// <summary>Sets invoice details using typed dates formatted at build time.</summary>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <param name="invoiceDate">The invoice date.</param>
    /// <param name="dueDate">The due date.</param>
    void SetInvoiceDetailsWithDates(string invoiceNumber, DateTime invoiceDate, DateTime dueDate);

    /// <summary>
    /// Sets the currency symbol used for subsequent PDF generation.
    /// </summary>
    /// <param name="currencySymbol">The currency symbol to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetCurrencySymbol(string currencySymbol);

    /// <summary>Sets deterministic locale-aware formatting options.</summary>
    /// <param name="options">The formatting options copied by the invoice.</param>
    void SetFormattingOptions(PdfInvoiceFormattingOptions options);

    /// <summary>Formats a currency value using the current invoice configuration.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The formatted currency text.</returns>
    string FormatInvoiceCurrency(double value);

    /// <summary>Formats an ordinary number using the current invoice configuration.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The formatted number text.</returns>
    string FormatInvoiceNumber(double value);

    /// <summary>Formats a date using the current invoice configuration.</summary>
    /// <param name="value">The date value.</param>
    /// <returns>The formatted date text.</returns>
    string FormatInvoiceDate(DateTime value);

    /// <summary>
    /// Adds a line to this invoice.
    /// </summary>
    /// <param name="line">The line to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddLine(PdfInvoiceLine line);

    /// <summary>
    /// Sets the totals used for subsequent PDF generation.
    /// </summary>
    /// <param name="totals">The totals to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetTotals(PdfInvoiceTotals totals);

    /// <summary>Enables an itemized tax-rate breakdown in the rendered invoice.</summary>
    void IncludeTaxBreakdown();

    /// <summary>Returns tax-rate subtotals calculated from current invoice lines.</summary>
    /// <returns>The tax-rate subtotals in ascending rate order.</returns>
    IReadOnlyList<PdfInvoiceTaxBreakdown> GetTaxBreakdown();

    /// <summary>
    /// Sets the payment terms used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetPaymentTerms(string text);

    /// <summary>Sets structured payment method and reference details.</summary>
    /// <param name="method">The payment method.</param>
    /// <param name="reference">The payment reference.</param>
    void SetPaymentMethod(string method, string reference);

    /// <summary>
    /// Sets the remittance instructions used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetRemittanceInstructions(string text);

    /// <summary>Sets structured remittance account details.</summary>
    /// <param name="payee">The remittance payee.</param>
    /// <param name="accountIdentifier">The account identifier.</param>
    /// <param name="bankIdentifier">The bank or routing identifier.</param>
    void SetRemittanceAccount(string payee, string accountIdentifier, string bankIdentifier);

    /// <summary>
    /// Sets the notes used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetNotes(string text);

    /// <summary>
    /// Sets the logo used for subsequent PDF generation.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetLogo(PdfImage image, double width, double height);

    /// <summary>Selects the logo position relative to the title.</summary>
    /// <param name="placement">The logo placement.</param>
    void SetLogoPlacement(PdfInvoiceLogoPlacement placement);

    /// <summary>
    /// Builds an invoice from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
