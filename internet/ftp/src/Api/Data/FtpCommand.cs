namespace ftp;

/// <summary>
/// Represents one parsed or outbound FTP command.
/// </summary>
public class FtpCommand
{
    /// <summary>
    /// The recognized command kind, or <see cref="FtpCommandKind.Unknown"/>.
    /// </summary>
    public FtpCommandKind Kind;

    /// <summary>
    /// The command verb with its original or canonical spelling.
    /// </summary>
    public string Verb;

    /// <summary>
    /// The optional command argument; null means absent and an empty string means present but empty.
    /// </summary>
    public string? Argument;

    /// <summary>
    /// The original command text without its line terminator, when retained.
    /// </summary>
    public string RawText;

    /// <summary>
    /// Indicates that the argument contains credentials or other secret material.
    /// </summary>
    public bool Sensitive;

    /// <summary>
    /// Creates an empty, unknown command.
    /// </summary>
    public FtpCommand()
    {
        Kind = FtpCommandKind.Unknown;
        Verb = "";
        Argument = null;
        RawText = "";
        Sensitive = false;
    }
}

