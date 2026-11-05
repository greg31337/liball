namespace fix;

/// <summary>Identifies a supported FIX Orchestra schema generation.</summary>
public enum OrchestraVersion
{
    /// <summary>The Orchestra version is not known.</summary>
    Unknown = 0,
    /// <summary>FIX Orchestra version 1.0.</summary>
    Version10 = 10,
    /// <summary>FIX Orchestra version 1.1.</summary>
    Version11 = 11
}
