using System.Collections.Generic;

namespace sdp;

/// <summary>Creates minimal SDP session and media descriptions from explicit caller choices.</summary>
public interface ISdpSessionFactory
{
    /// <summary>Creates a minimal session description with required origin, name, connection, and timing fields.</summary>
    /// <param name="origin">The origin identity, version, network, and unicast address.</param>
    /// <param name="sessionName">The nonempty session name.</param>
    /// <param name="connection">The session-level connection address.</param>
    /// <param name="timing">The initial timing period and any attached recurrences.</param>
    /// <returns>A new session description containing independent copies of the supplied values.</returns>
    SdpSessionDescription CreateSession(SdpOrigin origin, string sessionName, SdpConnectionData connection, SdpTiming timing);

    /// <summary>Creates a minimal media description with an explicit transport and format list.</summary>
    /// <param name="mediaType">The registered or extension media-type token.</param>
    /// <param name="port">The transport port from zero through 65535.</param>
    /// <param name="protocol">The slash-delimited transport-protocol token.</param>
    /// <param name="formats">The ordered nonempty list of unique format tokens.</param>
    /// <returns>A new media description containing independent copies of the supplied values.</returns>
    SdpMediaDescription CreateMedia(string mediaType, int port, string protocol, List<string> formats);
}
