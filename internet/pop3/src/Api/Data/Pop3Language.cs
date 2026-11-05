namespace pop3;

/// <summary>Describes one language offered by a POP3 server.</summary>
public class Pop3Language
{
    /// <summary>The language tag used by the LANG command.</summary>
    public string Tag = "";

    /// <summary>The UTF-8 human-readable description of the language.</summary>
    public string Description = "";
}
