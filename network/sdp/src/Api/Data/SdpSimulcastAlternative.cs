namespace sdp;

/// <summary>Represents one RID alternative within a simulcast stream.</summary>
public class SdpSimulcastAlternative
{
    /// <summary>Contains the referenced RID identifier.</summary>
    public string RidId = string.Empty;
    /// <summary>Indicates that the alternative is initially paused.</summary>
    public bool Paused = false;
}
