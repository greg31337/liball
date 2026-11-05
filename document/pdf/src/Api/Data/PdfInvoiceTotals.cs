using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Stores the monetary totals displayed on an invoice.
/// </summary>
public class PdfInvoiceTotals
{
    /// <summary>
    /// Stores the sum of line subtotals before discounts and tax.
    /// </summary>
    public double Subtotal;
    /// <summary>
    /// Stores the total discount amount subtracted from the subtotal.
    /// </summary>
    public double DiscountTotal;
    /// <summary>
    /// Stores the total tax amount added after discounts.
    /// </summary>
    public double TaxTotal;
    /// <summary>
    /// Stores the final amount due after discounts and tax.
    /// </summary>
    public double GrandTotal;

    /// <summary>
    /// Initializes a new set of invoice totals.
    /// </summary>
    /// <param name="subtotal">The subtotal to use for this operation.</param>
    /// <param name="discountTotal">The discount total to use for this operation.</param>
    /// <param name="taxTotal">The tax total to use for this operation.</param>
    /// <param name="grandTotal">The grand total to use for this operation.</param>
    public PdfInvoiceTotals(double subtotal, double discountTotal, double taxTotal, double grandTotal)
    {
        ValidateNotNegative(subtotal, "subtotal", "Invoice subtotal cannot be negative.");
        ValidateNotNegative(discountTotal, "discountTotal", "Invoice discount total cannot be negative.");
        ValidateNotNegative(taxTotal, "taxTotal", "Invoice tax total cannot be negative.");
        ValidateNotNegative(grandTotal, "grandTotal", "Invoice grand total cannot be negative.");

        Subtotal = subtotal;
        DiscountTotal = discountTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
    }

    /// <summary>
    /// Calculates invoice totals from the supplied lines in their current order.
    /// </summary>
    /// <param name="lines">The lines to use for this operation.</param>
    /// <returns>The resulting invoice totals.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfInvoiceTotals FromLines(IReadOnlyList<PdfInvoiceLine> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        double subtotal = 0;
        double discountTotal = 0;
        double taxTotal = 0;
        for (int index = 0; index < lines.Count; index++)
        {
            PdfInvoiceLine line = lines[index];
            if (line == null)
            {
                throw new ArgumentNullException("lines");
            }

            subtotal += line.GetSubtotal();
            discountTotal += line.Discount;
            taxTotal += line.GetTaxAmount();
        }

        double grandTotal = subtotal - discountTotal + taxTotal;
        return new PdfInvoiceTotals(subtotal, discountTotal, taxTotal, grandTotal);
    }

    internal static void ValidateNotNegative(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
