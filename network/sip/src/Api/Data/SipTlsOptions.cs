namespace sip;

/// <summary>Holds TLS identity, trust, and mutual-authentication options.</summary>
public class SipTlsOptions
{
    /// <summary>The expected server name.</summary>
    public string? ServerName;
    /// <summary>The local certificate bytes.</summary>
    public byte[] CertificateBytes = new byte[0];
    /// <summary>The local private-key bytes.</summary>
    public byte[] PrivateKeyBytes = new byte[0];
    /// <summary>The additional trusted root certificate bytes.</summary>
    public byte[][] TrustedRoots = new byte[0][];
    /// <summary>Requires a client certificate.</summary>
    public bool RequireClientCertificate;
    /// <summary>Requires platform chain and name validation.</summary>
    public bool ValidatePeer = true;
}
