namespace sdp;

/// <summary>Contains an ICE username fragment and password.</summary>
public class SdpIceCredentials
{
    /// <summary>Contains the ICE username fragment.</summary>
    public string UsernameFragment = string.Empty;
    /// <summary>Contains the ICE password and must be treated as sensitive data.</summary>
    public string Password = string.Empty;
}
