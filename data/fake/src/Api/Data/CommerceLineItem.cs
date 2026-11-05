namespace fake;

/// <summary>Represents one arithmetically consistent synthetic commerce line item.</summary>
/// <remarks>The item does not identify a real product, price, or sale.</remarks>
public class CommerceLineItem
{
    /// <summary>The synthetic SKU shape.</summary>
    public string Sku = string.Empty;

    /// <summary>The fictional product name.</summary>
    public string ProductName = string.Empty;

    /// <summary>The positive unit quantity.</summary>
    public int Quantity;

    /// <summary>The price of one unit in <c>CurrencyCode</c>.</summary>
    public decimal UnitPrice;

    /// <summary>The rounded product of <c>Quantity</c> and <c>UnitPrice</c>.</summary>
    public decimal Subtotal;

    /// <summary>The allocated share of an aggregate discount.</summary>
    public decimal DiscountAmount;

    /// <summary>The allocated share of aggregate tax after discount.</summary>
    public decimal TaxAmount;

    /// <summary>The line subtotal minus discount plus tax.</summary>
    public decimal Total;

    /// <summary>The ISO 4217 currency code used by every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>Whether the line is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    internal CommerceLineItem()
    {
    }
}
