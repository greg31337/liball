namespace pdf;

/// <summary>
/// Selects the business document represented by an invoice helper.
/// </summary>
public enum PdfInvoiceDocumentType
{
    /// <summary>Represents an invoice that requests payment.</summary>
    Invoice = 1,
    /// <summary>Represents a credit note that reduces a prior invoice balance.</summary>
    CreditNote = 2
}
