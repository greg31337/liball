namespace ttf;

/// <summary>Describes semantic support for one bounded SFNT table.</summary>
public enum FontTableSupportState
{
    /// <summary>The table tag is not recognized.</summary>
    Unknown = 0,

    /// <summary>The table and its parser-defined version are supported.</summary>
    Supported = 1,

    /// <summary>A compatible higher minor version is parsed using its supported prefix.</summary>
    CompatibleExtension = 2,

    /// <summary>The table is known but its version is available only as raw data.</summary>
    RawOnly = 3,

    /// <summary>The table technology is recognized but has no semantic implementation.</summary>
    Unsupported = 4
}
