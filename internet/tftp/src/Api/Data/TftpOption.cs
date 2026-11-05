namespace tftp;

/// <summary>
/// Holds one TFTP option name and its textual value.
/// </summary>
public class TftpOption
{
    /// <summary>
    /// The option name as it appears on the wire.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The option value as it appears on the wire.
    /// </summary>
    public string Value = "";
}
