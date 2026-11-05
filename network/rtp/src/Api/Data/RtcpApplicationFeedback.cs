namespace rtp;

/// <summary>
/// Contains opaque RFC 4585 application-layer feedback.
/// </summary>
public class RtcpApplicationFeedback
{
    /// <summary>The application-defined feedback control information.</summary>
    public byte[] Data = [];

    internal RtcpApplicationFeedback Copy()
    {
        RtcpApplicationFeedback copy = new RtcpApplicationFeedback();
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
