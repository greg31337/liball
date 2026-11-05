namespace fix;

/// <summary>Controls message framing and recovery behavior.</summary>
public class FrameOptions
{
    /// <summary>The maximum accepted frame size in bytes.</summary>
    public int MaxFrameSize = FixDefaults.MaxMessageSize;
    /// <summary>Whether invalid prefix bytes may be skipped to find a frame.</summary>
    public bool RecoverFromJunk;
    /// <summary>Whether a TagValue BodyLength must be valid before a frame is returned.</summary>
    public bool VerifyBodyLength = true;
    /// <summary>Whether a TagValue CheckSum must be valid before a frame is returned.</summary>
    public bool VerifyCheckSum = true;
}
