namespace pop3;

/// <summary>Contains credentials for POP3 authentication.</summary>
public class Pop3Credentials
{
    /// <summary>The authentication identity or POP3 user name.</summary>
    public string Username = "";

    /// <summary>The password associated with the authentication identity.</summary>
    public string Password = "";

    /// <summary>The optional authorization identity requested for SASL authentication.</summary>
    public string? AuthorizationIdentity;
}
