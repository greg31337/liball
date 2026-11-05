namespace pop3;

/// <summary>Describes the safe session context supplied to a POP3 server extension.</summary>
public class Pop3ExtensionContext
{
    /// <summary>The current POP3 session state.</summary>
    public Pop3SessionState State;

    /// <summary>The capabilities available in the current session.</summary>
    public Pop3CapabilitySet Capabilities = new Pop3CapabilitySet();

    /// <summary>Indicates whether the current transport is protected.</summary>
    public bool IsSecure;

    /// <summary>Indicates whether the session has authenticated successfully.</summary>
    public bool IsAuthenticated;

    /// <summary>Indicates whether UTF8 message mode is enabled.</summary>
    public bool IsUtf8Enabled;

    /// <summary>The selected language tag, or <c>null</c> when the server default is active.</summary>
    public string? SelectedLanguage;
}
