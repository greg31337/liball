namespace geo;

/// <summary>Identifies the selected well-known binary encoding flavor.</summary>
public enum WkbFlavor
{
    /// <summary>Strict OGC well-known binary.</summary>
    Ogc = 0,

    /// <summary>Extended well-known binary with an optional SRID flag.</summary>
    Ewkb = 1
}
