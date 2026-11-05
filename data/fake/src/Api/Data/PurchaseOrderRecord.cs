using System;

namespace fake;

/// <summary>Represents an arithmetically correlated synthetic purchase-order fixture.</summary>
/// <remarks>The record is not an approved order, invoice, or commitment to purchase.</remarks>
public class PurchaseOrderRecord
{
    /// <summary>The synthetic purchase-order-number shape.</summary>
    public string Number = string.Empty;

    /// <summary>The positive number of units ordered.</summary>
    public int Quantity;

    /// <summary>The numeric price of one unit.</summary>
    public decimal UnitPrice;

    /// <summary>The ISO 4217 currency code used for every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The rounded amount equal to unit price multiplied by quantity.</summary>
    public decimal Subtotal;

    /// <summary>The decimal tax rate.</summary>
    public decimal TaxRate;

    /// <summary>The rounded tax amount.</summary>
    public decimal TaxAmount;

    /// <summary>The decimal discount rate.</summary>
    public decimal DiscountRate;

    /// <summary>The rounded discount amount.</summary>
    public decimal DiscountAmount;

    /// <summary>The rounded fee amount.</summary>
    public decimal FeeAmount;

    /// <summary>The rounded total equal to subtotal plus tax and fee minus discount.</summary>
    public decimal Total;

    /// <summary>The locale-formatted display of <c>UnitPrice</c>.</summary>
    public string UnitPriceDisplay = string.Empty;

    /// <summary>The locale-formatted display of <c>Subtotal</c>.</summary>
    public string SubtotalDisplay = string.Empty;

    /// <summary>The locale-formatted display of <c>Total</c>.</summary>
    public string TotalDisplay = string.Empty;

    /// <summary>The captured reference time used as the creation time.</summary>
    public DateTimeOffset CreatedOn;

    /// <summary>The deterministic expected time fourteen days after creation.</summary>
    public DateTimeOffset ExpectedOn;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents an approved purchase, always <c>false</c>.</summary>
    public bool RepresentsApprovedPurchase;

    internal PurchaseOrderRecord()
    {
    }
}
