namespace rtp;

/// <summary>
/// Contains an RFC 4585 picture loss indication.
/// </summary>
public class RtcpPictureLossIndication
{
    /// <summary>Whether a picture refresh is requested.</summary>
    public bool Requested;

    internal RtcpPictureLossIndication Copy()
    {
        RtcpPictureLossIndication copy = new RtcpPictureLossIndication();
        copy.Requested = Requested;
        return copy;
    }
}
