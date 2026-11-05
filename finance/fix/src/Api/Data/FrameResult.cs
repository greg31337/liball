namespace fix;

/// <summary>Contains the explicit outcome of a framing operation.</summary>
public class FrameResult
{
    /// <summary>The framing status.</summary>
    public FrameStatus Status = FrameStatus.Incomplete;
    /// <summary>The byte offset at which the frame starts.</summary>
    public int FrameOffset;
    /// <summary>The complete frame length in bytes.</summary>
    public int FrameLength;
    /// <summary>The number of source bytes consumed, including recovered prefix bytes.</summary>
    public int Consumed;
    /// <summary>The total number of source bytes required when input is incomplete.</summary>
    public int Required;
    /// <summary>Whether the frame uses a Simple Open Framing Header.</summary>
    public bool IsSofh;
    /// <summary>The SOFH encoding identifier, or zero for TagValue framing without SOFH.</summary>
    public ushort EncodingType;
    /// <summary>The source offset associated with an error, or <c>-1</c> when unavailable.</summary>
    public int ErrorOffset = -1;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
