using System.Collections.Generic;

namespace rtp;

internal class RtpInboundMediaGroup
{
    internal uint sourceIdentifier;
    internal uint timestamp;
    internal List<RtpPacket> packets = new List<RtpPacket>();
}
