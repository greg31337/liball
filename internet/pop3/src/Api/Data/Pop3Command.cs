namespace pop3;

/// <summary>Describes one POP3 command and its wire-handling requirements.</summary>
public class Pop3Command
{
    /// <summary>The command keyword, normally represented in canonical upper case.</summary>
    public string Keyword = "";

    /// <summary>The ordered command arguments without separating spaces.</summary>
    public string[] Arguments = new string[0];

    /// <summary>Indicates whether the command contains authentication or other sensitive data.</summary>
    public bool ContainsSensitiveData;

    /// <summary>Indicates whether a positive response is followed by multiline data.</summary>
    public bool ExpectsMultilineResponse;
}
