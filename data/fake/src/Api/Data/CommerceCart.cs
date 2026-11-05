namespace fake;

/// <summary>Represents an arithmetically consistent synthetic shopping cart.</summary>
/// <remarks>The cart does not represent a customer session, inventory reservation, or offer.</remarks>
public class CommerceCart
{
    /// <summary>The independently generated line items.</summary>
    public CommerceLineItem[] Items = new CommerceLineItem[0];

    /// <summary>The number of entries in <c>Items</c>.</summary>
    public int LineCount;

    /// <summary>The sum of all item quantities.</summary>
    public int Quantity;

    /// <summary>The ISO 4217 currency code shared by every line and total.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The sum of rounded line subtotals.</summary>
    public decimal Subtotal;

    /// <summary>The decimal discount rate.</summary>
    public decimal DiscountRate;

    /// <summary>The rounded discount calculated from <c>Subtotal</c>.</summary>
    public decimal DiscountAmount;

    /// <summary>The subtotal after subtracting <c>DiscountAmount</c>.</summary>
    public decimal TaxableAmount;

    /// <summary>The decimal tax rate.</summary>
    public decimal TaxRate;

    /// <summary>The rounded tax calculated from <c>TaxableAmount</c>.</summary>
    public decimal TaxAmount;

    /// <summary>The generated shipping charge.</summary>
    public decimal ShippingAmount;

    /// <summary>The rounded sum of taxable amount, tax, and shipping.</summary>
    public decimal Total;

    /// <summary>Whether the cart is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    internal CommerceCart()
    {
    }
}
