namespace sip;

/// <summary>
/// Identifies the recognized form of a URI carried by SIP.
/// </summary>
public enum SipUriScheme
{
    /// <summary>
    /// The URI scheme has not been determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The SIP scheme.
    /// </summary>
    Sip = 1,

    /// <summary>
    /// The secure SIPS scheme.
    /// </summary>
    Sips = 2,

    /// <summary>
    /// The telephone-number TEL scheme.
    /// </summary>
    Tel = 3,

    /// <summary>
    /// An absolute URI with another scheme.
    /// </summary>
    Absolute = 4
}
