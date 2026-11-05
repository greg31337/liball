using System;

namespace pdf;

/// <summary>
/// Represents an invoice line used by the PDF generation API.
/// </summary>
public class PdfInvoiceLine : IPdfInvoiceLine
{
    /// <summary>
    /// Stores the description used by this object.
    /// </summary>
    internal string Description = string.Empty;
    /// <summary>
    /// Stores the nonnegative quantity billed by this line.
    /// </summary>
    internal double Quantity;
    /// <summary>
    /// Stores the nonnegative price for one unit.
    /// </summary>
    internal double UnitPrice;
    /// <summary>
    /// Stores the absolute discount amount subtracted before tax is calculated.
    /// </summary>
    internal double Discount;
    /// <summary>
    /// Stores the tax rate as a decimal fraction, such as 0.2 for 20 percent.
    /// </summary>
    internal double TaxRate;

    /// <summary>
    /// Initializes a new invoice line.
    /// </summary>
    /// <param name="description">The description to use for this operation.</param>
    /// <param name="quantity">The quantity to use for this operation.</param>
    /// <param name="unitPrice">The unit price to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfInvoiceLine(string description, double quantity, double unitPrice)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        ValidateNotNegative(quantity, "quantity", "Invoice line quantity cannot be negative.");
        ValidateNotNegative(unitPrice, "unitPrice", "Invoice line unit price cannot be negative.");

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = 0;
        TaxRate = 0;
    }

    /// <summary>
    /// Sets the discount used for subsequent PDF generation.
    /// </summary>
    /// <param name="discount">The discount to use for this operation.</param>
    public void SetDiscount(double discount)
    {
        ValidateNotNegative(discount, "discount", "Invoice line discount cannot be negative.");
        if (discount > GetSubtotal())
        {
            throw new ArgumentOutOfRangeException("discount", "Invoice line discount cannot exceed its subtotal.");
        }

        Discount = discount;
    }

    /// <summary>
    /// Sets the tax rate used for subsequent PDF generation.
    /// </summary>
    /// <param name="taxRate">The tax rate to use for this operation.</param>
    public void SetTaxRate(double taxRate)
    {
        ValidateNotNegative(taxRate, "taxRate", "Invoice line tax rate cannot be negative.");
        TaxRate = taxRate;
    }

    /// <summary>
    /// Returns the subtotal calculated from the object's current state.
    /// </summary>
    /// <returns>The quantity multiplied by the unit price.</returns>
    public double GetSubtotal()
    {
        return Quantity * UnitPrice;
    }

    /// <summary>
    /// Returns the tax amount calculated from the object's current state.
    /// </summary>
    /// <returns>The tax applied to the nonnegative subtotal remaining after the discount.</returns>
    public double GetTaxAmount()
    {
        double taxable = GetSubtotal() - Discount;
        if (taxable < 0)
        {
            taxable = 0;
        }

        return taxable * TaxRate;
    }

    /// <summary>
    /// Returns the total calculated from the object's current state.
    /// </summary>
    /// <returns>The subtotal minus the discount plus the calculated tax.</returns>
    public double GetTotal()
    {
        return GetSubtotal() - Discount + GetTaxAmount();
    }

    internal static void ValidateNotNegative(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
