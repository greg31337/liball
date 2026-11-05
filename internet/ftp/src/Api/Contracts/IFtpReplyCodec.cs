using System.IO;

namespace ftp;

/// <summary>
/// Reads, writes, and classifies bounded FTP replies.
/// </summary>
public interface IFtpReplyCodec
{
    /// <summary>
    /// Reads one complete single-line or multiline reply from a stream.
    /// </summary>
    /// <param name="source">The control-channel source stream.</param>
    /// <returns>The parsed reply.</returns>
    FtpReply ReadReply(Stream source);

    /// <summary>
    /// Writes one complete standards-conforming reply to a stream.
    /// </summary>
    /// <param name="destination">The control-channel destination stream.</param>
    /// <param name="reply">The reply to write.</param>
    void WriteReply(Stream destination, FtpReply reply);

    /// <summary>
    /// Classifies a parsed reply by result category and functional group.
    /// </summary>
    /// <param name="reply">The reply to classify.</param>
    /// <returns>The exception-less classification.</returns>
    FtpReplyClassification ClassifyReply(FtpReply reply);
}

