namespace rtp;

/// <summary>
/// Contains an RFC 6051 rapid RTCP resynchronization request.
/// </summary>
public class RtcpRapidResynchronizationRequest
{
    /// <summary>Whether rapid synchronization is requested.</summary>
    public bool Requested;

    internal RtcpRapidResynchronizationRequest Copy()
    {
        RtcpRapidResynchronizationRequest copy = new RtcpRapidResynchronizationRequest();
        copy.Requested = Requested;
        return copy;
    }
}
