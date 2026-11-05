namespace pop3;

/// <summary>Contains the values supplied by an APOP authentication attempt.</summary>
public class Pop3ApopRequest
{
    /// <summary>The POP3 user name.</summary>
    public string Username = "";

    /// <summary>The greeting timestamp used to calculate the digest.</summary>
    public string Timestamp = "";

    /// <summary>The hexadecimal MD5 digest supplied by the client.</summary>
    public string Digest = "";
}
