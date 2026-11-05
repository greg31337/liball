using System;

namespace fake;

/// <summary>Represents a correlated synthetic shipping route.</summary>
/// <remarks>The route is descriptive fixture data and does not represent carrier routing or geographic verification.</remarks>
public class ShippingRoute
{
    /// <summary>The origin warehouse code.</summary>
    public string OriginWarehouseCode = string.Empty;

    /// <summary>The origin address country code.</summary>
    public string OriginCountryCode = string.Empty;

    /// <summary>The destination address country code.</summary>
    public string DestinationCountryCode = string.Empty;

    /// <summary>The destination subdivision code.</summary>
    public string DestinationSubdivisionCode = string.Empty;

    /// <summary>The destination postal-code-shaped value.</summary>
    public string DestinationPostalCode = string.Empty;

    /// <summary>A compact label composed from the origin and destination codes.</summary>
    public string Label = string.Empty;

    /// <summary>Whether the origin and destination country codes differ.</summary>
    public bool IsInternational;

    internal ShippingRoute()
    {
    }
}
