namespace sip;

internal static class SipCertificateIdentityCopy
{
    internal static SipCertificateIdentity? Copy(SipCertificateIdentity? source)
    {
        if (source == null)
        {
            return null;
        }
        SipCertificateIdentity result = new SipCertificateIdentity();
        result.Subject = source.Subject;
        string[] domains = source.Domains == null
            ? new string[0] : source.Domains;
        result.Domains = new string[domains.Length];
        int domainIndex = 0;
        while (domainIndex < domains.Length)
        {
            result.Domains[domainIndex] = domains[domainIndex];
            domainIndex = domainIndex + 1;
        }
        byte[] certificateBytes = source.CertificateBytes == null
            ? new byte[0] : source.CertificateBytes;
        result.CertificateBytes = new byte[certificateBytes.Length];
        System.Array.Copy(certificateBytes, result.CertificateBytes,
            certificateBytes.Length);
        result.ChainValid = source.ChainValid;
        result.NameValid = source.NameValid;
        result.MutualTls = source.MutualTls;
        return result;
    }
}
