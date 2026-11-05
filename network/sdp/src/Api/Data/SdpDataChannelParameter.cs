namespace sdp;

/// <summary>Represents one ordered data-channel mapping parameter.</summary>
public class SdpDataChannelParameter
{
    /// <summary>Contains the parameter name with its original spelling.</summary>
    public string Name = string.Empty;
    /// <summary>Indicates whether an equals-delimited value was present.</summary>
    public bool HasValue = false;
    /// <summary>Contains the parameter value.</summary>
    public string Value = string.Empty;
    /// <summary>Contains the original parameter text.</summary>
    public string RawText = string.Empty;
}
