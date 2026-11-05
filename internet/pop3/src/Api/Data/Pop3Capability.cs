namespace pop3;

/// <summary>Describes one raw capability entry returned by the POP3 CAPA command.</summary>
public class Pop3Capability
{
    /// <summary>The capability tag as supplied by the peer.</summary>
    public string Name = "";

    /// <summary>The ordered capability parameters as supplied by the peer.</summary>
    public string[] Parameters = new string[0];
}
