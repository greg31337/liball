namespace fake;

/// <summary>Represents an arithmetically consistent synthetic commerce invoice.</summary>
/// <remarks>The invoice is not a demand for payment, tax document, account balance, or evidence of a sale.</remarks>
public class CommerceInvoice
{
    /// <summary>The synthetic invoice-number shape.</summary>
    public string Number = string.Empty;

    /// <summary>The correlated synthetic order-number shape.</summary>
    public string OrderNumber = string.Empty;

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

    /// <summary>The amount due, equal to <c>Total</c> for this unpaid fixture.</summary>
    public decimal AmountDue;

    /// <summary>The synthetic shipping address used by <c>Shipment</c>.</summary>
    public AddressRecord ShippingAddress = new AddressRecord();

    /// <summary>The synthetic billing address correlated by <c>BillingMatchesShipping</c>.</summary>
    public AddressRecord BillingAddress = new AddressRecord();

    /// <summary>Whether billing and shipping deliberately reference the same address record.</summary>
    public bool BillingMatchesShipping;

    /// <summary>The correlated synthetic shipment fixture.</summary>
    public ShipmentRecord Shipment = new ShipmentRecord();

    /// <summary>Whether the invoice is marked paid, always <c>false</c>.</summary>
    public bool IsPaid;

    /// <summary>Whether the invoice is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    internal CommerceInvoice()
    {
    }
}
