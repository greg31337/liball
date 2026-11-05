using System.Text;

namespace ftp;

/// <summary>
/// Provides strict decoded FTP command parsing and formatting.
/// </summary>
public class FtpCommandCodec : IFtpCommandCodec
{
    /// <summary>
    /// Creates an FTP command codec using the protocol's bounded defaults.
    /// </summary>
    public FtpCommandCodec()
    {
    }

    /// <summary>
    /// Parses one decoded command line without its CRLF terminator.
    /// </summary>
    /// <param name="line">The command line to parse.</param>
    /// <returns>The parsed command and its retained raw text.</returns>
    /// <exception cref="System.ArgumentNullException">The line is null.</exception>
    /// <exception cref="FtpProtocolException">The line is malformed or unsafe.</exception>
    public FtpCommand ParseCommand(string line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        FtpCommandWriter.ValidateUnicodeText(line);
        if (Encoding.UTF8.GetByteCount(line) > FtpDefaults.MaximumControlLineBytes - 2)
        {
            throw new FtpProtocolException(
                "The decoded FTP command exceeded the control-line byte limit.", null);
        }
        return FtpCommandParser.ParseDecodedCommand(line);
    }

    /// <summary>
    /// Formats one command with canonical framing and its CRLF terminator.
    /// </summary>
    /// <param name="command">The command to format.</param>
    /// <returns>The complete decoded command line.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    /// <exception cref="FtpProtocolException">The command is malformed or unsafe.</exception>
    public string FormatCommand(FtpCommand command)
    {
        return FtpCommandWriter.FormatDecodedCommand(command);
    }
}
