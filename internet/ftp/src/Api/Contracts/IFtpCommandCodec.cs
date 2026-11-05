namespace ftp;

/// <summary>
/// Parses and formats validated FTP control-channel commands.
/// </summary>
public interface IFtpCommandCodec
{
    /// <summary>
    /// Parses one decoded command line without its CRLF terminator.
    /// </summary>
    /// <param name="line">The bounded command line.</param>
    /// <returns>The parsed command including unknown verbs and raw text.</returns>
    FtpCommand ParseCommand(string line);

    /// <summary>
    /// Formats one validated command with its CRLF terminator.
    /// </summary>
    /// <param name="command">The command to format.</param>
    /// <returns>The complete command line.</returns>
    string FormatCommand(FtpCommand command);
}

