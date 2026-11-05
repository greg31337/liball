namespace fix;

/// <summary>Defines TLS and FIXS transport settings without executable callbacks.</summary>
public class SessionTlsSettings
{
    /// <summary>Whether TLS is enabled.</summary>
    public bool Enabled;
    /// <summary>The expected server name.</summary>
    public string TargetHost = "";
    /// <summary>The local certificate path.</summary>
    public string CertificatePath = "";
    /// <summary>The local certificate password.</summary>
    public string CertificatePassword = "";
    /// <summary>Whether the peer must provide a certificate.</summary>
    public bool RequirePeerCertificate = true;
    /// <summary>Whether certificate revocation is checked.</summary>
    public bool CheckCertificateRevocation = true;
    /// <summary>The negotiated ALPN identifier.</summary>
    public string Alpn = "fix";
}
