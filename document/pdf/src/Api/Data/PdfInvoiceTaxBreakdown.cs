using System;

namespace pdf;

/// <summary>
/// Stores one tax-rate subtotal calculated from invoice lines.
/// </summary>
public class PdfInvoiceTaxBreakdown
{
    /// <summary>Stores the decimal tax rate.</summary>
    public double TaxRate;
    /// <summary>Stores the amount subject to this tax rate after discounts.</summary>
    public double TaxableAmount;
    /// <summary>Stores the resulting tax amount.</summary>
    public double TaxAmount;

    /// <summary>Initializes one invoice tax-rate subtotal.</summary>
    /// <param name="taxRate">The non-negative decimal tax rate.</param>
    /// <param name="taxableAmount">The non-negative taxable amount.</param>
    /// <param name="taxAmount">The non-negative tax amount.</param>
    public PdfInvoiceTaxBreakdown(double taxRate, double taxableAmount, double taxAmount)
    {
        ValidateNotNegative(taxRate, "taxRate");
        ValidateNotNegative(taxableAmount, "taxableAmount");
        ValidateNotNegative(taxAmount, "taxAmount");
        TaxRate = taxRate;
        TaxableAmount = taxableAmount;
        TaxAmount = taxAmount;
    }

    internal static void ValidateNotNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Invoice tax breakdown values must be non-negative and finite.");
        }
    }
}
