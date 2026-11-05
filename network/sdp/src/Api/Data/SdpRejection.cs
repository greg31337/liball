namespace sdp;

/// <summary>Describes why one offered media description was rejected.</summary>
public class SdpRejection
{
    /// <summary>Contains the zero-based media index.</summary>
    public int MediaIndex = -1;
    /// <summary>Contains the media identifier when available.</summary>
    public string MediaId = string.Empty;
    /// <summary>Identifies a recognized rejection reason.</summary>
    public SdpRejectionReason Reason = SdpRejectionReason.Unknown;
    /// <summary>Contains an extension rejection-reason token.</summary>
    public string ReasonToken = string.Empty;
    /// <summary>Identifies the field, attribute, or nested value that caused rejection when available.</summary>
    public SdpSourcePath Path = new SdpSourcePath();
    /// <summary>Contains a human-readable detail that excludes sensitive values.</summary>
    public string Detail = string.Empty;
}
