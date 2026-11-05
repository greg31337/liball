namespace fake;

/// <summary>Selects the lifecycle state of a synthetic commerce order.</summary>
public enum CommerceOrderStatus
{
    /// <summary>The order-shaped fixture has not been confirmed.</summary>
    Pending = 0,

    /// <summary>The fixture has a confirmation time but no shipment time.</summary>
    Confirmed = 1,

    /// <summary>The fixture has confirmation and shipment times.</summary>
    Shipped = 2,

    /// <summary>The fixture has confirmation, shipment, and delivery times.</summary>
    Delivered = 3,

    /// <summary>The fixture has a cancellation time and no fulfillment times.</summary>
    Cancelled = 4
}
