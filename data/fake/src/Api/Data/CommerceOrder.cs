namespace fake;

/// <summary>Represents an arithmetically consistent synthetic commerce order.</summary>
/// <remarks>The order does not represent a purchase, payment obligation, fulfillment request, or inventory reservation.</remarks>
public class CommerceOrder
{
    /// <summary>The synthetic order-number shape.</summary>
    public string Number = string.Empty;

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

    /// <summary>The synthetic shipping address used by <c>Shipment</c>.</summary>
    public AddressRecord ShippingAddress = new AddressRecord();

    /// <summary>The synthetic billing address correlated by <c>BillingMatchesShipping</c>.</summary>
    public AddressRecord BillingAddress = new AddressRecord();

    /// <summary>Whether billing and shipping deliberately reference the same address record.</summary>
    public bool BillingMatchesShipping;

    /// <summary>The correlated synthetic shipment fixture.</summary>
    public ShipmentRecord Shipment = new ShipmentRecord();

    /// <summary>The synthetic marketplace seller associated with the order.</summary>
    public MarketplaceSellerRecord Seller = new MarketplaceSellerRecord();

    /// <summary>The seller identifier copied from <c>Seller</c> and <c>Shipment</c>.</summary>
    public string SellerIdentifier = string.Empty;

    /// <summary>The synthetic purchase-order number copied to <c>Shipment</c>.</summary>
    public string PurchaseOrderNumber = string.Empty;

    /// <summary>The generated lifecycle status.</summary>
    public CommerceOrderStatus Status;

    /// <summary>The captured time at which the synthetic order fixture begins.</summary>
    public System.DateTimeOffset CreatedAt;

    /// <summary>Whether <c>ConfirmedAt</c> applies to the selected status.</summary>
    public bool HasConfirmedAt;

    /// <summary>The confirmation time when <c>HasConfirmedAt</c> is true.</summary>
    public System.DateTimeOffset ConfirmedAt;

    /// <summary>Whether <c>ShippedAt</c> applies to the selected status.</summary>
    public bool HasShippedAt;

    /// <summary>The shipment time when <c>HasShippedAt</c> is true.</summary>
    public System.DateTimeOffset ShippedAt;

    /// <summary>Whether <c>DeliveredAt</c> applies to the selected status.</summary>
    public bool HasDeliveredAt;

    /// <summary>The delivery time when <c>HasDeliveredAt</c> is true.</summary>
    public System.DateTimeOffset DeliveredAt;

    /// <summary>Whether <c>CancelledAt</c> applies to the selected status.</summary>
    public bool HasCancelledAt;

    /// <summary>The cancellation time when <c>HasCancelledAt</c> is true.</summary>
    public System.DateTimeOffset CancelledAt;

    /// <summary>Whether the order is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the order represents a submitted purchase, always <c>false</c>.</summary>
    public bool IsSubmitted;

    internal CommerceOrder()
    {
    }
}
