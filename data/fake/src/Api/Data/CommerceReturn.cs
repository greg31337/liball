using System;

namespace fake;

/// <summary>Represents a correlated, explicitly synthetic merchandise-return fixture.</summary>
/// <remarks>The record does not represent returned goods, a refund authorization, settled funds, or a customer claim.</remarks>
public class CommerceReturn
{
    /// <summary>The return-number-shaped value.</summary>
    public string Number = string.Empty;

    /// <summary>The delivered synthetic order associated with the return.</summary>
    public CommerceOrder Order = new CommerceOrder();

    /// <summary>The order number copied from <c>Order</c>.</summary>
    public string OrderNumber = string.Empty;

    /// <summary>The SKU copied from one of the order lines.</summary>
    public string Sku = string.Empty;

    /// <summary>The positive returned quantity, no greater than the selected line quantity.</summary>
    public int Quantity;

    /// <summary>The currency code copied from the order and selected line.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The project-authored return reason.</summary>
    public string Reason = string.Empty;

    /// <summary>The project-authored refund reason when status is <c>Refunded</c>, otherwise an empty string.</summary>
    public string RefundReason = string.Empty;

    /// <summary>The lifecycle status of the return fixture.</summary>
    public CommerceReturnStatus Status;

    /// <summary>The captured request time after order delivery.</summary>
    public DateTimeOffset RequestedAt;

    /// <summary>Whether <c>ResolvedAt</c> applies to the selected status.</summary>
    public bool HasResolvedAt;

    /// <summary>The resolution time when <c>HasResolvedAt</c> is true.</summary>
    public DateTimeOffset ResolvedAt;

    /// <summary>The refund amount for <c>Refunded</c>, otherwise zero.</summary>
    public decimal RefundAmount;

    /// <summary>Whether the return is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents received returned goods, always <c>false</c>.</summary>
    public bool RepresentsReceivedGoods;

    /// <summary>Whether the record represents an authorized or settled refund, always <c>false</c>.</summary>
    public bool RepresentsSettledRefund;

    internal CommerceReturn()
    {
    }
}
