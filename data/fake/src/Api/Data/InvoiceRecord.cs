using System;

namespace fake;

/// <summary>Represents an arithmetically correlated synthetic invoice fixture.</summary>
/// <remarks>The record is not a demand for payment, tax advice, or evidence of a sale.</remarks>
public class InvoiceRecord
{
    /// <summary>The synthetic invoice-number shape.</summary>
    public string Number = string.Empty;

    /// <summary>The ISO 4217 currency code used for every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The amount before tax, discount, and fees.</summary>
    public decimal Subtotal;

    /// <summary>The decimal tax rate, where <c>0.10</c> means ten percent.</summary>
    public decimal TaxRate;

    /// <summary>The rounded tax amount.</summary>
    public decimal TaxAmount;

    /// <summary>The decimal discount rate, where <c>0.10</c> means ten percent.</summary>
    public decimal DiscountRate;

    /// <summary>The rounded discount amount.</summary>
    public decimal DiscountAmount;

    /// <summary>The rounded fee amount.</summary>
    public decimal FeeAmount;

    /// <summary>The rounded total equal to subtotal plus tax and fee minus discount.</summary>
    public decimal Total;

    /// <summary>The balance due, equal to <c>Total</c> for the unpaid fixture.</summary>
    public decimal BalanceDue;

    /// <summary>The captured reference time used as the issue time.</summary>
    public DateTimeOffset IssuedOn;

    /// <summary>The deterministic due time thirty days after <c>IssuedOn</c>.</summary>
    public DateTimeOffset DueOn;

    /// <summary>Whether the fixture is marked paid, always <c>false</c>.</summary>
    public bool IsPaid;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    internal InvoiceRecord()
    {
    }
}
