namespace geo;

/// <summary>Identifies an angular or linear unit used by geographic operations.</summary>
public enum SpatialUnit
{
    /// <summary>Radians.</summary>
    Radians = 0,

    /// <summary>Degrees of arc.</summary>
    Degrees = 1,

    /// <summary>Metres.</summary>
    Meters = 2,

    /// <summary>Kilometres.</summary>
    Kilometers = 3,

    /// <summary>International statute miles.</summary>
    Miles = 4,

    /// <summary>International nautical miles.</summary>
    NauticalMiles = 5,

    /// <summary>International feet.</summary>
    Feet = 6
}
