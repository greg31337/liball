namespace sip;

/// <summary>Holds peer certificate and SIP domain identity data.</summary>
public class SipCertificateIdentity
{
    /// <summary>The certificate subject.</summary>
    public string? Subject;
    /// <summary>The DNS and SIP domain identities.</summary>
    public string[] Domains = new string[0];
    /// <summary>The leaf certificate bytes.</summary>
    public byte[] CertificateBytes = new byte[0];
    /// <summary>Indicates a valid certificate chain.</summary>
    public bool ChainValid;
    /// <summary>Indicates a valid peer name.</summary>
    public bool NameValid;
    /// <summary>Indicates mutual TLS authentication.</summary>
    public bool MutualTls;
}
