namespace geo;

/// <summary>Identifies the strategy used to make an invalid geometry valid.</summary>
public enum RepairMethod
{
    /// <summary>Build valid geometry from fully noded linework.</summary>
    Linework = 0,

    /// <summary>Repair shells and holes according to their structure.</summary>
    Structure = 1
}
