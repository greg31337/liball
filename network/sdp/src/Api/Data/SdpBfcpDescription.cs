using System.Collections.Generic;

namespace sdp;

/// <summary>Collects Binary Floor Control Protocol signaling.</summary>
public class SdpBfcpDescription
{
    /// <summary>Contains the floor-control role token.</summary>
    public string FloorControl = string.Empty;
    /// <summary>Contains the conference identifier.</summary>
    public string ConferenceId = string.Empty;
    /// <summary>Contains the user identifier.</summary>
    public string UserId = string.Empty;
    /// <summary>Contains floor identifiers in source order.</summary>
    public List<string> FloorIds = new List<string>();
    /// <summary>Contains supported BFCP versions in preference order.</summary>
    public List<string> Versions = new List<string>();
    /// <summary>Identifies a recognized connection setup role.</summary>
    public SdpDtlsSetupRole SetupRole = SdpDtlsSetupRole.Unknown;
    /// <summary>Contains the original setup-role token.</summary>
    public string SetupRoleToken = string.Empty;
    /// <summary>Contains the connection-reuse token.</summary>
    public string ConnectionRole = string.Empty;
}
