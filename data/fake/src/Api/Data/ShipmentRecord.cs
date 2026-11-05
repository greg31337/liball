using System;

namespace fake;

/// <summary>Represents a correlated, explicitly synthetic shipment fixture.</summary>
/// <remarks>The record does not represent carrier acceptance, tracking registration, address deliverability, or a delivery commitment.</remarks>
public class ShipmentRecord
{
    /// <summary>The synthetic destination address.</summary>
    public AddressRecord ShippingAddress = new AddressRecord();

    /// <summary>The synthetic billing address.</summary>
    public AddressRecord BillingAddress = new AddressRecord();

    /// <summary>Whether billing and shipping deliberately reference the same address record.</summary>
    public bool BillingMatchesShipping;

    /// <summary>The project-fictional carrier service.</summary>
    public ShippingService Service = new ShippingService();

    /// <summary>The fictional origin warehouse and bin.</summary>
    public WarehouseRecord Warehouse = new WarehouseRecord();

    /// <summary>The synthetic package measurements.</summary>
    public PackageRecord Package = new PackageRecord();

    /// <summary>The synthetic ISO 6346-shaped freight-container identifier.</summary>
    public ContainerIdentifierRecord Container = new ContainerIdentifierRecord();

    /// <summary>The identifier copied from <c>Container</c>.</summary>
    public string ContainerIdentifier = string.Empty;

    /// <summary>The synthetic tracking-number shape.</summary>
    public string TrackingNumber = string.Empty;

    /// <summary>The linked synthetic order number, or an empty string for a standalone shipment.</summary>
    public string OrderNumber = string.Empty;

    /// <summary>The linked synthetic purchase-order number, or an empty string for a standalone shipment.</summary>
    public string PurchaseOrderNumber = string.Empty;

    /// <summary>The linked synthetic seller identifier, or an empty string for a standalone shipment.</summary>
    public string SellerIdentifier = string.Empty;

    /// <summary>The route correlated with warehouse and shipping addresses.</summary>
    public ShippingRoute Route = new ShippingRoute();

    /// <summary>The delivery interval derived from <c>Service</c>.</summary>
    public DeliveryInterval EstimatedDelivery = new DeliveryInterval();

    /// <summary>Whether the shipment is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents carrier acceptance, always <c>false</c>.</summary>
    public bool RepresentsCarrierAcceptance;

    /// <summary>Whether the record claims its addresses are deliverable, always <c>false</c>.</summary>
    public bool RepresentsDeliverableShipment;

    internal ShipmentRecord()
    {
    }
}
