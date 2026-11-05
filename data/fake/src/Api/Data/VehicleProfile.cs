namespace fake;

/// <summary>Represents a correlated fictional motor-vehicle and route fixture.</summary>
/// <remarks>The VIN and registration are structural synthetic values and do not claim assignment, manufacture, ownership, or registration.</remarks>
public class VehicleProfile
{
    /// <summary>The project-fictional vehicle make.</summary>
    public string Make = string.Empty;

    /// <summary>The model correlated with <c>Make</c>.</summary>
    public string Model = string.Empty;

    /// <summary>The broad vehicle body type.</summary>
    public string Type = string.Empty;

    /// <summary>The fuel or propulsion type supported by the fixture.</summary>
    public string Fuel = string.Empty;

    /// <summary>The model year encoded by <c>Vin</c>.</summary>
    public int ModelYear;

    /// <summary>The checksum-valid synthetic VIN shape.</summary>
    public string Vin = string.Empty;

    /// <summary>The conspicuously synthetic registration-plate shape.</summary>
    public string RegistrationPlate = string.Empty;

    /// <summary>The reviewed invariant color name.</summary>
    public string Color = string.Empty;

    /// <summary>The fictional route name.</summary>
    public string Route = string.Empty;

    /// <summary>The broad transit mode associated with the fixture.</summary>
    public string TransitMode = string.Empty;

    /// <summary>Whether the profile is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the VIN claims assignment, always <c>false</c>.</summary>
    public bool VinIsAssigned;

    /// <summary>Whether the registration claims assignment, always <c>false</c>.</summary>
    public bool RegistrationIsAssigned;

    internal VehicleProfile()
    {
    }
}
