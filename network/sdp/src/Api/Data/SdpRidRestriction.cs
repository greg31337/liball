namespace sdp;

/// <summary>Represents one ordered RID restriction parameter.</summary>
public class SdpRidRestriction
{
    /// <summary>Contains the restriction name with its original spelling.</summary>
    public string Name = string.Empty;
    /// <summary>Indicates whether an equals-delimited value was present.</summary>
    public bool HasValue = false;
    /// <summary>Contains the restriction value.</summary>
    public string Value = string.Empty;
    /// <summary>Contains the original restriction text.</summary>
    public string RawText = string.Empty;
}
