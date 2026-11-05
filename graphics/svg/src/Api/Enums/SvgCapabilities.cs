namespace svg;

/// <summary>Defines the current handling status of a feature.</summary>
public enum SvgFeatureStatus
{
    /// <summary>The portable library implements the feature.</summary>
    Implemented = 0,

    /// <summary>The feature is available only through an explicit caller callback.</summary>
    CallbackRequired = 1,

    /// <summary>The feature is retained but is not computed or rendered.</summary>
    PreserveOnly = 2,

    /// <summary>The feature is available but disabled by the active policy.</summary>
    PolicyDisabled = 3,

    /// <summary>The feature is not available.</summary>
    Unsupported = 4
}
