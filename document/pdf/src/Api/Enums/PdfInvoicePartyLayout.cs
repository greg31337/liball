namespace pdf;

/// <summary>
/// Selects how seller, buyer, and shipping parties are arranged on an invoice.
/// </summary>
public enum PdfInvoicePartyLayout
{
    /// <summary>Places each available party in a full-width vertical sequence.</summary>
    Stacked = 1,
    /// <summary>Places seller and buyer side by side and shipping below them.</summary>
    TwoColumn = 2,
    /// <summary>Places seller, buyer, and shipping in three adjacent columns.</summary>
    ThreeColumn = 3
}
