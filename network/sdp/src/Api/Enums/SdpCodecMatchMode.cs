namespace sdp;

/// <summary>
/// Specifies how codec capabilities are compared.
/// </summary>
public enum SdpCodecMatchMode
{
    /// <summary>The codec match mode is unknown or was not specified.</summary>
    Unknown = 0,

    /// <summary>Require all compared codec parameters to match exactly.</summary>
    Strict = 1,

    /// <summary>Allow compatible parameter differences defined by the codec profile.</summary>
    Compatible = 2
}
