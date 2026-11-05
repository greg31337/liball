using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfInvoiceLine"/>.
/// </summary>
public interface IPdfInvoiceLine
{
    /// <summary>
    /// Sets the discount used for subsequent PDF generation.
    /// </summary>
    /// <param name="discount">The discount to use for this operation.</param>
    void SetDiscount(double discount);

    /// <summary>
    /// Sets the tax rate used for subsequent PDF generation.
    /// </summary>
    /// <param name="taxRate">The tax rate to use for this operation.</param>
    void SetTaxRate(double taxRate);

    /// <summary>
    /// Returns the subtotal calculated from the object's current state.
    /// </summary>
    /// <returns>The quantity multiplied by the unit price.</returns>
    double GetSubtotal();

    /// <summary>
    /// Returns the tax amount calculated from the object's current state.
    /// </summary>
    /// <returns>The tax applied to the nonnegative subtotal remaining after the discount.</returns>
    double GetTaxAmount();

    /// <summary>
    /// Returns the total calculated from the object's current state.
    /// </summary>
    /// <returns>The subtotal minus the discount plus the calculated tax.</returns>
    double GetTotal();
}
