namespace pop3;

/// <summary>Describes the observable state of a POP3 client session.</summary>
public class Pop3ClientSnapshot
{
    /// <summary>The current protocol session state.</summary>
    public Pop3SessionState State;

    /// <summary>Indicates whether a transport is currently attached.</summary>
    public bool IsConnected;

    /// <summary>Indicates whether the current transport is protected.</summary>
    public bool IsSecure;

    /// <summary>Indicates whether authentication completed successfully.</summary>
    public bool IsAuthenticated;

    /// <summary>The authenticated identity, or <c>null</c> when unavailable.</summary>
    public string? AuthenticatedIdentity;

    /// <summary>The server greeting text, or <c>null</c> before a greeting is received.</summary>
    public string? Greeting;

    /// <summary>The last parsed server response, or <c>null</c> before one is received.</summary>
    public Pop3Response? LastResponse;

    /// <summary>The capabilities known for the current connection.</summary>
    public Pop3CapabilitySet Capabilities = new Pop3CapabilitySet();

    /// <summary>The current mailbox status, or <c>null</c> before a maildrop is available.</summary>
    public Pop3MailboxStatus? MailboxStatus;

    /// <summary>Indicates whether UTF8 message mode is enabled.</summary>
    public bool IsUtf8Enabled;

    /// <summary>Indicates whether UTF-8 authentication identifiers are permitted.</summary>
    public bool IsUtf8UserEnabled;

    /// <summary>The selected language tag, or <c>null</c> when the server default is active.</summary>
    public string? SelectedLanguage;

    /// <summary>The known message metadata in stable transaction order.</summary>
    public Pop3MessageInfo[] Messages = new Pop3MessageInfo[0];

    /// <summary>The known message unique identifiers in stable transaction order.</summary>
    public Pop3UniqueId[] UniqueIds = new Pop3UniqueId[0];
}
