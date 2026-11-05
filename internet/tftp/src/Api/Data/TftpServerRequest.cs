using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds an immutable-by-convention snapshot of an accepted server request.
/// </summary>
public class TftpServerRequest
{
    /// <summary>
    /// The requested filename.
    /// </summary>
    public string Filename = "";

    /// <summary>
    /// The requested transfer direction.
    /// </summary>
    public TftpTransferDirection Direction = TftpTransferDirection.Read;

    /// <summary>
    /// The requested content representation.
    /// </summary>
    public TftpTransferMode Mode = TftpTransferMode.Octet;

    /// <summary>
    /// The client endpoint from which the request was received.
    /// </summary>
    public TftpEndpoint ClientEndpoint = new TftpEndpoint();

    /// <summary>
    /// The ordered options supplied by the client.
    /// </summary>
    public List<TftpOption> RequestedOptions = [];

    /// <summary>
    /// The ordered options accepted by the server.
    /// </summary>
    public List<TftpOption> EffectiveOptions = [];

    /// <summary>
    /// Indicates whether <see cref="AnnouncedSize"/> contains a transfer size.
    /// </summary>
    public bool HasAnnouncedSize = false;

    /// <summary>
    /// The announced transfer size when <see cref="HasAnnouncedSize"/> is true.
    /// </summary>
    public long AnnouncedSize = 0;
}
