namespace sdp;

/// <summary>Represents an image-attribute constraint expression.</summary>
public class SdpImageAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains a numeric payload token or <c>*</c>.</summary>
    public string PayloadType = string.Empty;
    /// <summary>Indicates whether send constraints were present.</summary>
    public bool HasSendConstraints = false;
    /// <summary>Contains the original send constraint expression.</summary>
    public string SendConstraints = string.Empty;
    /// <summary>Indicates whether receive constraints were present.</summary>
    public bool HasReceiveConstraints = false;
    /// <summary>Contains the original receive constraint expression.</summary>
    public string ReceiveConstraints = string.Empty;
}
