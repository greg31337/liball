namespace pdf;

/// <summary>
/// Selects the invoice-logo position relative to the invoice title and details.
/// </summary>
public enum PdfInvoiceLogoPlacement
{
    /// <summary>Places the logo above the invoice title.</summary>
    AboveTitle = 1,
    /// <summary>Places the logo to the left of the invoice title.</summary>
    LeftOfTitle = 2,
    /// <summary>Places the logo to the right of the invoice title.</summary>
    RightOfTitle = 3
}
