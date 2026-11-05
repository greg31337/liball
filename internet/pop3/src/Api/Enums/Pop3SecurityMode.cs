namespace pop3;

/// <summary>Specifies how transport security is established for a POP3 connection.</summary>
public enum Pop3SecurityMode
{
    /// <summary>Use an unencrypted transport without attempting TLS.</summary>
    Plaintext = 0,

    /// <summary>Upgrade with STLS when the server advertises it.</summary>
    StlsIfAvailable = 1,

    /// <summary>Require a successful STLS upgrade before protected operations.</summary>
    StlsRequired = 2,

    /// <summary>Establish TLS before exchanging any POP3 protocol bytes.</summary>
    ImplicitTls = 3
}
