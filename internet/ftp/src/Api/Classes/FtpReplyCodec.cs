using System.IO;
using System.Text;

namespace ftp;

/// <summary>
/// Provides strict, bounded UTF-8 FTP reply reading, writing, and classification.
/// </summary>
public class FtpReplyCodec : IFtpReplyCodec
{
    internal FtpReplyParser Parser;
    internal FtpReplyWriter Writer;
    internal FtpReplyStreamReader StreamReader;

    /// <summary>
    /// Creates an FTP reply codec using strict framing and bounded protocol defaults.
    /// </summary>
    public FtpReplyCodec()
    {
        Parser = new FtpReplyParser(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines, true);
        Writer = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);
        StreamReader = new FtpReplyStreamReader(
            FtpDefaults.MaximumReplyBytes,
            FtpDefaults.MaximumReplyLines,
            FtpDefaults.MaximumControlLineBytes,
            true);
    }

    /// <summary>
    /// Reads exactly one strict single-line or multiline UTF-8 reply from a stream.
    /// </summary>
    /// <param name="source">The stream containing FTP control-channel reply bytes.</param>
    /// <returns>The parsed reply.</returns>
    /// <exception cref="System.ArgumentNullException">The source is null.</exception>
    /// <exception cref="FtpProtocolException">The reply is malformed, truncated, or too large.</exception>
    public FtpReply ReadReply(Stream source)
    {
        byte[] bytes = StreamReader.ReadReplyBytes(source);
        return Parser.ParseEncodedReply(bytes, Encoding.UTF8);
    }

    /// <summary>
    /// Writes one complete standards-conforming UTF-8 reply to a stream.
    /// </summary>
    /// <param name="destination">The destination control-channel stream.</param>
    /// <param name="reply">The reply to format and write.</param>
    /// <exception cref="System.ArgumentNullException">The destination or reply is null.</exception>
    /// <exception cref="FtpProtocolException">The reply is malformed, unsafe, or too large.</exception>
    public void WriteReply(Stream destination, FtpReply reply)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        byte[] bytes = Writer.FormatEncodedReply(reply, Encoding.UTF8);
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Classifies a reply without throwing for unknown or malformed numeric codes.
    /// </summary>
    /// <param name="reply">The reply to classify.</param>
    /// <returns>The classification, which is also stored on the reply.</returns>
    /// <exception cref="System.ArgumentNullException">The reply is null.</exception>
    public FtpReplyClassification ClassifyReply(FtpReply reply)
    {
        return FtpReplyClassifier.ApplyClassification(reply);
    }
}
