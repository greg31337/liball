namespace molecule;


/// <summary>
/// Defines the supported values for identifierentity policy.
/// </summary>
public enum IdentityPolicy
{
    /// <summary>
    /// Specifies exact.
    /// </summary>
    Exact = 0,
    /// <summary>
    /// Specifies connectivity.
    /// </summary>
    Connectivity = 1,
    /// <summary>
    /// Specifies stereo insensitive.
    /// </summary>
    StereoInsensitive = 2,
    /// <summary>
    /// Specifies charge insensitive.
    /// </summary>
    ChargeInsensitive = 3,
    /// <summary>
    /// Specifies isotope insensitive.
    /// </summary>
    IsotopeInsensitive = 4,
    /// <summary>
    /// Specifies tautomer insensitive.
    /// </summary>
    TautomerInsensitive = 5,
    /// <summary>
    /// Specifies fragment parent.
    /// </summary>
    FragmentParent = 6,
    /// <summary>
    /// Specifies charge parent.
    /// </summary>
    ChargeParent = 7,
    /// <summary>
    /// Specifies stereo parent.
    /// </summary>
    StereoParent = 8,
    /// <summary>
    /// Specifies tautomer parent.
    /// </summary>
    TautomerParent = 9,
    /// <summary>
    /// Specifies super parent.
    /// </summary>
    SuperParent = 10,
    /// <summary>
    /// Specifies isotope parent.
    /// </summary>
    IsotopeParent = 11
}
