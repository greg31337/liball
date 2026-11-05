namespace sdp;

/// <summary>Represents one ordered codec format parameter.</summary>
public class SdpFormatParameter
{
    /// <summary>Contains the parameter name with its original spelling.</summary>
    public string Name = string.Empty;
    /// <summary>Indicates whether an equals-delimited value was present.</summary>
    public bool HasValue = false;
    /// <summary>Contains the parameter value, including an explicitly empty value.</summary>
    public string Value = string.Empty;
    /// <summary>Contains the original parameter text.</summary>
    public string RawText = string.Empty;
}
