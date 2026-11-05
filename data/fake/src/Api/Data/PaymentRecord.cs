using System;

namespace fake;

/// <summary>Represents a correlated synthetic payment calculation.</summary>
/// <remarks>The record is not an authorization, settlement, transfer instruction, or balance assertion.</remarks>
public class PaymentRecord
{
    /// <summary>The synthetic transaction reference.</summary>
    public string Reference = string.Empty;

    /// <summary>The correlated invoice-number shape.</summary>
    public string InvoiceNumber = string.Empty;

    /// <summary>The selected payment transaction category.</summary>
    public FinancialTransactionType Type;

    /// <summary>The project-authored description correlated with <c>Type</c>.</summary>
    public string Description = string.Empty;

    /// <summary>The optional synthetic check number, or an empty string for non-check categories.</summary>
    public string CheckNumber = string.Empty;

    /// <summary>The ISO 4217 currency code used for every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The amount before tax, discount, and fees.</summary>
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

    /// <summary>The rounded payment amount.</summary>
    public decimal Amount;

    /// <summary>The synthetic balance before subtracting <c>Amount</c>.</summary>
    public decimal BalanceBefore;

    /// <summary>The synthetic balance after subtracting <c>Amount</c>.</summary>
    public decimal BalanceAfter;

    /// <summary>The captured reference time associated with the fixture.</summary>
    public DateTimeOffset OccurredOn;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the fixture represents an authorization, always <c>false</c>.</summary>
    public bool IsAuthorized;

    /// <summary>Whether the fixture represents settled funds, always <c>false</c>.</summary>
    public bool IsSettled;

    internal PaymentRecord()
    {
    }
}
