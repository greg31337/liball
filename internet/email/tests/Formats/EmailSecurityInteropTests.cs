using System.Text;
using email;

namespace email.Tests;

public class EmailSecurityInteropTests
{
    // RFC 4134, sections 3.1 and 3.2 / Appendix A.
    const string Rfc4134BerData = "MIAGCSqGSIb3DQEHAaCAJIAEBFRoaXMEGCBpcyBzb21lIHNhbXBsZSBjb250ZW50LgAAAAAAAA==";
    const string Rfc4134DerData = "MCsGCSqGSIb3DQEHAaAeBBxUaGlzIGlzIHNvbWUgc2FtcGxlIGNvbnRlbnQu";

    // RFC 9580, Appendix A.11.5.
    const string Rfc9580AeadGcmMessage = "-----BEGIN PGP MESSAGE-----\r\n\r\nwzwGGgcDCwMI6dOXhbIHAAj/tC58SD70iERXyzcmubPbn/d25fTZpAlS4kRymIUa\r\nv/91Jt8t1VRBdXmneZ/SaQIHAwb8uUSQvLmLvcnRBsYJAmaUD3LontwhtVlrFXax\r\nAe0Pn/xvxtZbv9JNzQeQlm5tHoWjAFN4TLHYtqBpnvEhVaeyrWJYUxtXZR/Xd3kS\r\n+pXjXZtAIW9ppMJI2yj/QzHxYykHOZ5v+Q==\r\n-----END PGP MESSAGE-----\r\n";

    // Generated with OpenSSL 3.6.3 `openssl cms -sign -binary -nodetach
    // -outform DER`, using a synthetic, non-operational self-signed test key.
    const string OpenSslOpaqueSignedData = "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";
    // RFC 4134, section 4.2 / Appendix A: RSA with SHA-1 SignedData.
    const string Rfc4134RsaSha1SignedData = "MIIDUgYJKoZIhvcNAQcCoIIDQzCCAz8CAQExCzAJBgUrDgMCGgUAMCsGCSqGSIb3DQEHAaAeBBxUaGlzIGlzIHNvbWUgc2FtcGxlIGNvbnRlbnQuoIICMDCCAiwwggGVoAMCAQICEEY0a8eAAFa8EdNuLsQQs7AwDQYJKoZIhvcNAQEFBQAwEjEQMA4GA1UEAxMHQ2FybFJTQTAeFw05OTA5MTkwMTA4NDdaFw0zOTEyMzEyMzU5NTlaMBMxETAPBgNVBAMTCEFsaWNlUlNBMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDgiXM5jdj19eiHdjl/TrAFu1OD3g+3q9x9x3UpDQUubRLfpoYm1NJvqlgp/Jfs+oJRDzCAvrFQnkZE8Sy72DLPxmhvB9mwYKy+7jQJahP19wUFk99eujVW2WH/GX/Jgeb4bOqHQHDvrG0sdJ8t+lU6uZl3AqZIUoxO81c4V3RXXwIDAQABo4GBMH8wDAYDVR0TAQH/BAIwADAOBgNVHQ8BAf8EBAMCBsAwHwYDVR0jBBgwFoAU6eCQJ6x4IHqa00zyQjdOIq6eOLswHQYDVR0OBBYEFHfStNG3TIqKo85Fnc7sPKA64/9QMB8GA1UdEQQYMBaBFEFsaWNlUlNBQGV4YW1wbGUuY29tMA0GCSqGSIb3DQEBBQUAA4GBAD5wR6hIzBNYj8pRcWtONhhdBH6AsY1NzMqjj8x9Vsi8z26zHFmpIKoFgahOJa2ncBR1L/XHm9EO6WPSZLfGZm5zIVTf9LolXX1J05RrIjZ0c7hK7C9k7dM90qdCxeg3irTbn2fkvZ/5/nTv6vnuY2rYP0slCbXYGnau65vbSbAiMYHLMIHIAgEBMCYwEjEQMA4GA1UEAxMHQ2FybFJTQQIQRjRrx4AAVrwR024uxBCzsDAJBgUrDgMCGgUAMA0GCSqGSIb3DQEBAQUABIGALyOC0vMJX7gMWOtOnb+JmoHldcSRPdPQ1Xu21f6UoYqs48SE9c1gTieV9s8AhnZ1Pyvw59QCZ6f1x40WBKWztefZMvAk7+cgRNWfB8VTJPrOAR0PFxOnKpWdK+QDlRQL6TkNus5unJ4M6JjmVRPUaG/QB9eisWJM44+v/eDVXcc=";

    [Test]
    public void VerifiesOpenSslOpaqueSignedDataWithStandardAttributes()
    {
        EmailSmimeVerificationOptions options = new EmailSmimeVerificationOptions();
        options.SetVerifyCertificateChain(false);

        EmailSmimeVerificationResult result = new EmailSmimeVerifier().VerifyOpaqueWithCmsAndOptions(Convert.FromBase64String(OpenSslOpaqueSignedData), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSignerCount(), Is.EqualTo(1));
            Assert.That(result.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.Pass));
            Assert.That(result.GetContent(), Is.EqualTo(Encoding.ASCII.GetBytes("Content-Type: text/plain\r\n\r\nOpenSSL CMS interoperability\r\n")));
        });
    }

    [Test]
    public void ParsesRfc4134BerAndDerDataVectorsAndRejectsTruncation()
    {
        byte[] berBytes = Convert.FromBase64String(Rfc4134BerData);
        byte[] derBytes = Convert.FromBase64String(Rfc4134DerData);
        EmailCmsReadResult ber = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(berBytes);
        EmailCmsReadResult der = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(derBytes);
        EmailCmsReadResult truncatedBer = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(berBytes.AsSpan(0, berBytes.Length - 1));
        EmailCmsReadResult truncatedDer = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(derBytes.AsSpan(0, derBytes.Length - 1));

        Assert.Multiple(() =>
        {
            Assert.That(ber.IsSuccess(), Is.True);
            Assert.That(ber.GetContentInfo()!.GetEncodingKind(), Is.EqualTo(EmailCmsEncodingKind.Ber));
            Assert.That(ber.GetContentInfo()!.GetDataContent(), Is.EqualTo(Encoding.ASCII.GetBytes("This is some sample content.")));
            Assert.That(ber.GetIssues().ContainsCode(EmailIssueCode.CmsBerEncodingAccepted), Is.True);
            Assert.That(der.IsSuccess(), Is.True);
            Assert.That(der.GetContentInfo()!.GetEncodingKind(), Is.EqualTo(EmailCmsEncodingKind.Der));
            Assert.That(der.GetContentInfo()!.GetDataContent(), Is.EqualTo(ber.GetContentInfo()!.GetDataContent()));
            Assert.That(truncatedBer.IsSuccess(), Is.False);
            Assert.That(truncatedDer.IsSuccess(), Is.False);
        });
    }

    [Test]
    public void ParsesRfc9580AeadGcmPacketSequenceAndRejectsTruncation()
    {
        EmailOpenPgpReadResult result = new EmailOpenPgpReader().ParseWithInput(Encoding.ASCII.GetBytes(Rfc9580AeadGcmMessage));
        EmailOpenPgpSessionKeyPacket sessionKey = result.GetPacketAt(0).GetSessionKey()!;
        byte[] packets = result.GetArmor()!.GetData();
        EmailOpenPgpReadResult truncated = new EmailOpenPgpReader().ParseWithInput(packets.AsSpan(0, packets.Length - 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPacketCount(), Is.EqualTo(2));
            Assert.That(sessionKey!.GetVersion(), Is.EqualTo(6));
            Assert.That(sessionKey.GetSymmetricAlgorithm(), Is.EqualTo(EmailOpenPgpAlgorithmsConstants.SymmetricAes128));
            Assert.That(sessionKey.GetAeadAlgorithm(), Is.EqualTo(EmailOpenPgpAlgorithmsConstants.AeadGcm));
            Assert.That(sessionKey.GetStringToKeySpecifier(), Is.EqualTo(Convert.FromHexString("0308E9D39785B2070008FF")));
            Assert.That(result.GetPacketAt(1).GetKind(), Is.EqualTo(EmailOpenPgpPacketKind.SymmetricallyEncryptedIntegrityProtectedData));
            Assert.That(result.GetPacketAt(1).GetBody().Take(4), Is.EqualTo(new byte[] { 2, 7, 3, 6 }));
            Assert.That(truncated.IsSuccess(), Is.False);
            Assert.That(truncated.GetIssues().ContainsCode(EmailIssueCode.OpenPgpPacketMalformed), Is.True);
        });
    }

    [Test]
    public void RejectsRfc4134Sha1DowngradeUnlessHistoricVerificationIsEnabled()
    {
        byte[] cms = Convert.FromBase64String(Rfc4134RsaSha1SignedData);
        EmailSmimeVerificationOptions rejectedOptions = new EmailSmimeVerificationOptions();
        rejectedOptions.SetVerifyCertificateChain(false);
        EmailSmimeVerificationResult rejected = new EmailSmimeVerifier().VerifyOpaqueWithCmsAndOptions(cms, rejectedOptions);

        EmailSmimeVerificationOptions historicOptions = new EmailSmimeVerificationOptions();
        historicOptions.SetVerifyCertificateChain(false);
        EmailSmimeAlgorithmPolicy policy = historicOptions.GetAlgorithmPolicy();
        policy.SetAllowSha1Verification(true);
        policy.SetMinimumRsaKeyBits(1024);
        historicOptions.SetAlgorithmPolicy(policy);
        EmailSmimeVerificationResult accepted = new EmailSmimeVerifier().VerifyOpaqueWithCmsAndOptions(cms, historicOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.PolicyRejected));
            Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.SmimeAlgorithmPolicyRejected), Is.True);
            Assert.That(accepted.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.Pass));
            Assert.That(accepted.GetSignerAt(0).GetDigestAlgorithm(), Is.EqualTo(EmailSmimeDigestAlgorithm.Sha1));
        });
    }
}
