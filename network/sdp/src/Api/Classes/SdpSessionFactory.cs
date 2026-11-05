using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Creates minimal SDP session and media descriptions from explicit caller choices.</summary>
public class SdpSessionFactory : ISdpSessionFactory
{
    /// <summary>Initializes an SDP session factory.</summary>
    public SdpSessionFactory()
    {
    }

    /// <summary>Creates a minimal session description with required origin, name, connection, and timing fields.</summary>
    /// <param name="origin">The origin identity, version, network, and unicast address.</param>
    /// <param name="sessionName">The nonempty session name.</param>
    /// <param name="connection">The session-level connection address.</param>
    /// <param name="timing">The initial timing period and any attached recurrences.</param>
    /// <returns>A new session description containing independent copies of the supplied values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a supplied value cannot form a valid core SDP field.</exception>
    public SdpSessionDescription CreateSession(SdpOrigin origin, string sessionName, SdpConnectionData connection, SdpTiming timing)
    {
        if (origin == null)
        {
            throw new ArgumentNullException("origin");
        }
        if (sessionName == null)
        {
            throw new ArgumentNullException("sessionName");
        }
        if (connection == null)
        {
            throw new ArgumentNullException("connection");
        }
        if (timing == null)
        {
            throw new ArgumentNullException("timing");
        }
        return SdpSessionFactoryCore.CreateSession(origin, sessionName, connection, timing);
    }

    /// <summary>Creates a minimal media description with an explicit transport and format list.</summary>
    /// <param name="mediaType">The registered or extension media-type token.</param>
    /// <param name="port">The transport port from zero through 65535.</param>
    /// <param name="protocol">The slash-delimited transport-protocol token.</param>
    /// <param name="formats">The ordered nonempty list of unique format tokens.</param>
    /// <returns>A new media description containing independent copies of the supplied values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the media type, protocol, or format-list argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a token or format collection cannot form a valid core media field.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is outside its supported range.</exception>
    public SdpMediaDescription CreateMedia(string mediaType, int port, string protocol, List<string> formats)
    {
        if (mediaType == null)
        {
            throw new ArgumentNullException("mediaType");
        }
        if (protocol == null)
        {
            throw new ArgumentNullException("protocol");
        }
        if (formats == null)
        {
            throw new ArgumentNullException("formats");
        }
        if (port < 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException("port");
        }
        return SdpSessionFactoryCore.CreateMedia(mediaType, port, protocol, formats);
    }
}
