namespace sdp;

/// <summary>Represents a connection-oriented media setup attribute.</summary>
public class SdpSetupAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Identifies a recognized connection setup role.</summary>
    public SdpDtlsSetupRole Role = SdpDtlsSetupRole.Unknown;

    /// <summary>Contains the registered or extension setup-role token.</summary>
    public string RoleToken = string.Empty;
}
