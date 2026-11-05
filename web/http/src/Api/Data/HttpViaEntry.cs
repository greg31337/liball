using System;

namespace http;

/// <summary>
/// Represents one HTTP Via field entry.
/// </summary>
public class HttpViaEntry
{
    /// <summary>
    /// The received protocol name or version.
    /// </summary>
    public string ReceivedProtocol = "";

    /// <summary>
    /// The received-by host or pseudonym.
    /// </summary>
    public string ReceivedBy = "";

    /// <summary>
    /// Indicates that a received-by port is present.
    /// </summary>
    public bool HasPort;

    /// <summary>
    /// The received-by port.
    /// </summary>
    public int Port;

    /// <summary>
    /// The comment octets without surrounding parentheses, or <c>null</c> when absent.
    /// </summary>
    public byte[]? Comment;
}
