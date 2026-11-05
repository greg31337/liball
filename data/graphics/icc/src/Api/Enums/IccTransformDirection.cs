namespace icc;

/// <summary>
/// Identifies the requested direction of a colour transform.
/// </summary>
public enum IccTransformDirection
{
    /// <summary>
    /// Transforms device data to a Profile Connection Space.
    /// </summary>
    DeviceToPcs = 0,

    /// <summary>
    /// Transforms Profile Connection Space data to device data.
    /// </summary>
    PcsToDevice = 1,

    /// <summary>
    /// Transforms between device encodings through a compiled chain.
    /// </summary>
    DeviceToDevice = 2
}
