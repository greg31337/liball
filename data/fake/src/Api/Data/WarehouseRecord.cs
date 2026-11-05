using System;

namespace fake;

/// <summary>Represents an explicitly fictional warehouse fixture.</summary>
/// <remarks>The address is synthetic and does not claim that a logistics facility exists.</remarks>
public class WarehouseRecord
{
    /// <summary>The warehouse-code-shaped value correlated with <c>Address</c>.</summary>
    public string Code = string.Empty;

    /// <summary>The project-authored fictional warehouse name.</summary>
    public string Name = string.Empty;

    /// <summary>The bin-location-shaped value.</summary>
    public string Bin = string.Empty;

    /// <summary>The synthetic warehouse address.</summary>
    public AddressRecord Address = new AddressRecord();

    /// <summary>Whether the warehouse is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the record claims a real facility exists, always <c>false</c>.</summary>
    public bool RepresentsRealFacility;

    internal WarehouseRecord()
    {
    }
}
