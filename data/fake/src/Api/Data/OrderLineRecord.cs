namespace fake;

/// <summary>Represents one arithmetically consistent synthetic order line.</summary>
/// <remarks>The line does not identify a real product, price, or sale.</remarks>
public class OrderLineRecord
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

    /// <summary>The allocated share of the order discount.</summary>
    public decimal DiscountAmount;

    /// <summary>The allocated share of order tax after discount.</summary>
    public decimal TaxAmount;

    /// <summary>The line subtotal minus discount plus tax.</summary>
    public decimal Total;

    /// <summary>The ISO 4217 currency code used by every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>Whether the line is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    internal static OrderLineRecord FromCommerceLineItem(CommerceLineItem source)
    {
        OrderLineRecord value = new OrderLineRecord();
        value.Sku = source.Sku;
        value.ProductName = source.ProductName;
        value.Quantity = source.Quantity;
        value.UnitPrice = source.UnitPrice;
        value.Subtotal = source.Subtotal;
        value.DiscountAmount = source.DiscountAmount;
        value.TaxAmount = source.TaxAmount;
        value.Total = source.Total;
        value.CurrencyCode = source.CurrencyCode;
        value.IsSynthetic = source.IsSynthetic;
        value.TestMarker = source.TestMarker;
        return value;
    }
}
