using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using email;

namespace email.Tests;

[NonParallelizable]
public class EmailSmimeSignerVerifierTests
{
    const string RsaCertificateDer = "MIIDJTCCAg2gAwIBAgIUKt5utQDevSEw4DLD02DdZeJDGjowDQYJKoZIhvcNAQELBQAwGjEYMBYGA1UEAwwPVGVzdCBSU0EgU2lnbmVyMB4XDTI2MDgxMTAwMDQzOFoXDTM2MDgwODAwMDQzOFowGjEYMBYGA1UEAwwPVGVzdCBSU0EgU2lnbmVyMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvgm3nd1U9SaVoevd7D92g9laJgWVzOIvDHb4mNMyszJu0Ec3TZI5k+Utck23QGhQ3vnbH6G64Q4icMpPNZCsaLIGlqdMa3sJlWCOsOZTiOIUXLmFRW7jWqGCGf739Jenmrc5Z/HXlP0iAdipB5LIoYbY5J94wN1i67J6e/6U/8aEFpAws4Ha1HTk2SQRqd5b+myst93uipwe4H8SsAHr+JZCz6TCRECnpe8wG0i31LYN5dS2OJQBGFjmcfQZxI6/6IVaC4AG07vxD02Jc8Bh0E5nKmIWT0h2ppNs5V3jQa4vksetj1q87DbPNGi3HIpjoaYADP3u2lJHhMNevMZIgQIDAQABo2MwYTAfBgNVHSMEGDAWgBTRbthlF8UCi3KG3NxKpvlmrOXvijAPBgNVHRMBAf8EBTADAQH/MA4GA1UdDwEB/wQEAwIChDAdBgNVHQ4EFgQU0W7YZRfFAotyhtzcSqb5Zqzl74owDQYJKoZIhvcNAQELBQADggEBAB7HvxM2EhHLKBta5fKfYGSXbfGcTdCwTL0ZK6PNU8BFNzV8SUOWKCj3UBsJ8FXi2T59sZ1IH81NwzDMG6UE5WkXAFZ6QKi/cNidcenMG+Cp1IAqAnK89UA6HT9lp6bbpOFuoBUGeF/DPA/wUJ5H0LuRa1UbVev0k3W6SaIVZhfi8tYaubi9cLQeYTjRdlccATQTZ1jtqH86uQOtiOrOXXQHdX0EWIjqT7SNcRe3BCcJ1eE1CUEF430RTqx3DEAB9Li4SP0nZCQF/gLtIzeaSZPM2SEgdCQap4k7Lk/FMmbwb5vvNrQ1goM0UI+GqqsCzWN07m3Dbyof0l0Ytem+y1w=";
    const string RsaPrivateKeyDer = "MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQC+Cbed3VT1JpWh693sP3aD2VomBZXM4i8MdviY0zKzMm7QRzdNkjmT5S1yTbdAaFDe+dsfobrhDiJwyk81kKxosgaWp0xrewmVYI6w5lOI4hRcuYVFbuNaoYIZ/vf0l6eatzln8deU/SIB2KkHksihhtjkn3jA3WLrsnp7/pT/xoQWkDCzgdrUdOTZJBGp3lv6bKy33e6KnB7gfxKwAev4lkLPpMJEQKel7zAbSLfUtg3l1LY4lAEYWOZx9BnEjr/ohVoLgAbTu/EPTYlzwGHQTmcqYhZPSHamk2zlXeNBri+Sx62PWrzsNs80aLccimOhpgAM/e7aUkeEw168xkiBAgMBAAECggEAD19u3JTaeghYg2Q2rBVIK4tKGRhLkWrcnngDTS2MhOV4BF42cY56HOYlooD56XpmzFrO6ISnPuxrSc97z9v3rnJ0SPyYmHs3/77zXAkNyd1sA0I4rzC0sJCvGxY+jak09DWNLcTpCRLfDOIvbj7n/vUPF5N067kc9u8VrHEgMX6ENa/UgALBLP7OqPLI80C0G7O1nVpiMf6YyS0MEC/zSebiflexpGSfX3l8gbfqbWOQ3gnDIoFF8SmxICUuCLmphxzwSMRe3CuRitxQILyWTbOYe3eKV1mn2I5l3CKVvuv5CAv2hUWwhU6ZNiqT3x9q5kJYy0HrRh9IY9CK7vH7CQKBgQDzg2yfqJuUJ4xNpLv9Jblmpbq15kHmJhUrTjhzfy74bqG0GPTupfgpB526Uljor0ETU5q4PgQczHWAD2YPlUlN0qT2kF1pwbw2H5NeLU+zXjtkcgjDQzyFZ3C5MjMGOoLJLfhivtKSK1ev8kCkcyPqCwC/gh78Yb/8p05KHa0RLQKBgQDHyFOUhupfHxAPCzH6Y/PtqFbUN55S+s9k9o3uluFqMK+nvXZXGljTy04zmJRw2U6HbEQHF6LSxVnQGIOmLzB58XHWcgBSoc4B2eI/stxpFvvAP5Q4nVJo2Kf7QuyTzHDErPPeCLSBblEtkCb130zVNPJRSx7CLkb8g0IZsTwhJQKBgQDQ3WUJ/ICVLua0ohtsIjNHy9a9tmXHldqKbmkVB32uMJYZ913+Y/TanGG5MnB4A/n5LbiDVw4H9YbC9HJB43YgfhBoO6Mj1rwsYCDyokK+QpEGspi5gq4ibnrNHjWNnhoXnIpzh+Ds5qgrjcnMQ8YGNLHLkTwqlV0qxAXupE2RcQKBgQCEoSV7kSW/CGws0I3ZYMnrmLosINBm3kMZD+Wh42C6kp/KiCksIj2ePXYpIr5JPLq5hNKUsshRjNrXH68mgGV6TyP37a7xLKIK8I8XIrLYwX1YX90SyL6RBGRglklgxXSH3Rn46WaMFqSlzvFOgXQMhx3ngitlVjuiH7GeRN9aUQKBgQCzHCY1ZnctJghn8Pm9HfwoRfbkkJ6l2I8/tfRnWGp/tNxg8tfLzC4xt592JXWEIrJE8moa7RpQGF4ZX3OvvNim92kerTlC8TIIB3IsBGBtM6THVqWatO8x6EFU63kVkfaOatkxjOpRnBUD4CBcPiS30xFFiOOwINCHXCXY6A05oA==";
    const string EcCertificateDer = "MIIB1DCCAVqgAwIBAgIUZGOhq4om7ity/Y5BGM9kVXE7pJAwCgYIKoZIzj0EAwIwGTEXMBUGA1UEAwwOVGVzdCBFQyBTaWduZXIwHhcNMjYwODExMDAwNDM4WhcNMzYwODA4MDAwNDM4WjAZMRcwFQYDVQQDDA5UZXN0IEVDIFNpZ25lcjB2MBAGByqGSM49AgEGBSuBBAAiA2IABGtrN6gn2pVN/GmITqP8YR6ZxgyKTu9YFu4sgibskuOInpiyDcaHLA+Eov99XeZFxj6o4an9e1SnCOZL+kDDh28pJ2jyNYaODEj5tsPuG9aduLBx+848GMRdjtYQFTmpNKNjMGEwHwYDVR0jBBgwFoAUePS+WmjF0F1Hu7p+ZT8+IpI6Bx4wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAoQwHQYDVR0OBBYEFHj0vlpoxdBdR7u6fmU/PiKSOgceMAoGCCqGSM49BAMCA2gAMGUCMQDS7RBT6v74ZDDLwqzNJ5jZx9ImgyB/xc2hlaIgtrunfKGoNVfYW+WNxfbdqmcaHroCMHoScRBCVJbpIKYlqnCdY+GGhjMhrvP9Vtw42CusvhqqHUn+KrwXvl0PiUJX86FC/A==";
    const string EcPrivateKeyDer = "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDAMS1JC+meKLwyhm6gigvzt831LK3mqli0m5ix52tFB96tBNHWlnllpLHUro8TXzA2hZANiAARrazeoJ9qVTfxpiE6j/GEemcYMik7vWBbuLIIm7JLjiJ6Ysg3GhywPhKL/fV3mRcY+qOGp/XtUpwjmS/pAw4dvKSdo8jWGjgxI+bbD7hvWnbiwcfvOPBjEXY7WEBU5qTQ=";

    [Test]
    public void SignsAndVerifiesDetachedRsaWithCertificateMetadataAndChain()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        byte[] content = Bytes("Content-Type: text/plain\r\n\r\nexact signed bytes");
        List<EmailSmimeSigningCredential> signers = new List<EmailSmimeSigningCredential>
        {
            new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(certificate, rsa, EmailSmimeDigestAlgorithm.Sha256)
        };

        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(content, signers);
        EmailSmimeVerificationOptions verificationOptions = Verification(certificate);
        EmailSmimeVerificationResult verified = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, signed.GetCmsBytes(), verificationOptions);
        EmailSmimeSignerVerificationResult signer = verified.GetSignerAt(0);
        EmailSmimeSigningCredential certificateOnly = new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndDigestAlgorithm(certificate, EmailSmimeDigestAlgorithm.Sha256);

        Assert.Multiple(() =>
        {
            Assert.That(signed.IsSuccess(), Is.True);
            Assert.That(signed.IsDetached(), Is.True);
            Assert.That(signed.GetSignerCount(), Is.EqualTo(1));
            Assert.That(verified.IsSuccess(), Is.True);
            Assert.That(verified.IsDetached(), Is.True);
            Assert.That(verified.HasContent(), Is.True);
            Assert.That(verified.GetContent(), Is.EqualTo(content));
            Assert.That(signer.GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.Pass));
            Assert.That(signer.HasCertificate(), Is.True);
            Assert.That(signer.GetDigestAlgorithm(), Is.EqualTo(EmailSmimeDigestAlgorithm.Sha256));
            Assert.That(signer.GetSignatureAlgorithm(), Is.EqualTo(EmailSmimeSignatureAlgorithm.RsaPkcs1));
            Assert.That(signer.IsContentDigestValid(), Is.True);
            Assert.That(signer.IsSignatureValid(), Is.True);
            Assert.That(signer.GetCertificate()!.GetSubject(), Does.Contain("Test RSA Signer"));
            Assert.That(signer.GetCertificate()!.GetThumbprint(), Is.EqualTo(certificate.Thumbprint));
            Assert.That(signer.GetCertificate()!.GetRawData(), Is.EqualTo(certificate.RawData));
            Assert.That(signer.GetCertificate()!.GetIssuer(), Is.EqualTo(certificate.Issuer));
            Assert.That(signer.GetCertificate()!.GetSerialNumber(), Is.EqualTo(certificate.SerialNumber));
            Assert.That(signer.GetCertificate()!.GetNotBefore(), Is.EqualTo(certificate.NotBefore));
            Assert.That(signer.GetCertificate()!.GetNotAfter(), Is.EqualTo(certificate.NotAfter));
            Assert.That(signer.GetChainResult()!.IsValid(), Is.True);
            Assert.That(signer.GetChainResult()!.GetElementCount(), Is.EqualTo(1));
            Assert.That(signer.GetChainResult()!.GetElementAt(0).GetThumbprint(), Is.EqualTo(certificate.Thumbprint));
            Assert.That(certificateOnly.HasPrivateKey(), Is.False);
        });

        byte[] returned = verified.GetContent();
        returned[0] = 0;
        Assert.That(verified.GetContent(), Is.EqualTo(content));
        verificationOptions.ClearVerificationTime();
        Assert.That(verificationOptions.HasVerificationTime(), Is.False);
    }

    [Test]
    public void SignsOpaqueContentWithMultipleRsaAndEcdsaSigners()
    {
        using RSA rsa = RsaPrivateKey();
        using ECDsa ecdsa = EcPrivateKey();
        using X509Certificate2 rsaCertificate = RsaCertificate();
        using X509Certificate2 ecCertificate = EcCertificate();
        byte[] content = Bytes("opaque content");
        List<EmailSmimeSigningCredential> signers = new List<EmailSmimeSigningCredential>
        {
            new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(rsaCertificate, rsa, EmailSmimeDigestAlgorithm.Sha256),
            new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(ecCertificate, ecdsa, EmailSmimeDigestAlgorithm.Sha384)
        };

        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignOpaqueWithContentAndSignersAndOptions(content, signers, new EmailSmimeSigningOptions());
        EmailSmimeVerificationOptions options = Verification(rsaCertificate);
        options.AddCustomTrustRoot(ecCertificate);
        EmailSmimeVerificationResult verified = new EmailSmimeVerifier().VerifyOpaqueWithCmsAndOptions(signed.GetCmsBytes(), options);

        Assert.Multiple(() =>
        {
            Assert.That(signed.IsSuccess(), Is.True);
            Assert.That(signed.IsOpaque(), Is.True);
            Assert.That(signed.GetSignerCount(), Is.EqualTo(2));
            Assert.That(verified.IsSuccess(), Is.True);
            Assert.That(verified.IsDetached(), Is.False);
            Assert.That(verified.GetContent(), Is.EqualTo(content));
            Assert.That(verified.GetSignerCount(), Is.EqualTo(2));
            Assert.That(verified.GetSigners().Select(value => value.GetSignatureAlgorithm()), Is.EquivalentTo(new[] { EmailSmimeSignatureAlgorithm.RsaPkcs1, EmailSmimeSignatureAlgorithm.Ecdsa }));
            Assert.That(verified.GetSigners().Select(value => value.GetDigestAlgorithm()), Is.EquivalentTo(new[] { EmailSmimeDigestAlgorithm.Sha256, EmailSmimeDigestAlgorithm.Sha384 }));
        });
    }

    [Test]
    public void DetectsContentDigestAndSignatureCorruption()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        byte[] content = Bytes("original");
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(content, Credentials(certificate, rsa));
        EmailSmimeVerificationOptions options = Verification(certificate);

        EmailSmimeVerificationResult contentFailure = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(Bytes("changed"), signed.GetCmsBytes(), options);
        byte[] corruptedCms = signed.GetCmsBytes();
        byte[] signature = SignatureBytes(corruptedCms);
        int signatureOffset = LastIndexOf(corruptedCms, signature);
        corruptedCms[signatureOffset + signature.Length - 1] ^= 1;
        EmailSmimeVerificationResult signatureFailure = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, corruptedCms, options);

        byte[] unsupportedCms = signed.GetCmsBytes();
        byte[] rsaEncryptionObjectIdentifier = Convert.FromHexString("2A864886F70D010101");
        int algorithmOffset = LastIndexOf(unsupportedCms, rsaEncryptionObjectIdentifier);
        Assert.That(algorithmOffset, Is.GreaterThanOrEqualTo(0));
        unsupportedCms[algorithmOffset + rsaEncryptionObjectIdentifier.Length - 1] = 0x7F;
        EmailSmimeVerificationResult unsupported = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, unsupportedCms, options);

        byte[] malformedCms = signed.GetCmsBytes();
        byte[] malformedSignature = SignatureBytes(malformedCms);
        int malformedSignatureOffset = LastIndexOf(malformedCms, malformedSignature);
        Assert.That(malformedCms[malformedSignatureOffset - 4], Is.EqualTo(0x04));
        malformedCms[malformedSignatureOffset - 4] = 0x02;
        EmailSmimeVerificationResult malformed = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, malformedCms, options);

        Assert.Multiple(() =>
        {
            Assert.That(contentFailure.IsSuccess(), Is.False);
            Assert.That(contentFailure.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.DigestMismatch));
            Assert.That(contentFailure.GetIssues().ContainsCode(EmailIssueCode.SmimeDigestMismatch), Is.True);
            Assert.That(signatureFailure.IsSuccess(), Is.False);
            Assert.That(signatureFailure.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.SignatureMismatch));
            Assert.That(signatureFailure.GetSignerAt(0).IsContentDigestValid(), Is.True);
            Assert.That(signatureFailure.GetIssues().ContainsCode(EmailIssueCode.SmimeSignatureMismatch), Is.True);
            Assert.That(unsupported.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.UnsupportedAlgorithm));
            Assert.That(malformed.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.Malformed));
        });
    }

    [Test]
    public void ReportsChainFailureSeparatelyFromCryptographicValidity()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        byte[] content = Bytes("chain content");
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(content, Credentials(certificate, rsa));

        EmailSmimeVerificationResult untrusted = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignature(content, signed.GetCmsBytes());
        EmailSmimeVerificationOptions cryptographicOnlyOptions = new EmailSmimeVerificationOptions();
        cryptographicOnlyOptions.SetVerifyCertificateChain(false);
        EmailSmimeVerificationResult cryptographicOnly = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, signed.GetCmsBytes(), cryptographicOnlyOptions);

        Assert.Multiple(() =>
        {
            Assert.That(untrusted.IsSuccess(), Is.False);
            Assert.That(untrusted.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.CertificateChainInvalid));
            Assert.That(untrusted.GetSignerAt(0).IsContentDigestValid(), Is.True);
            Assert.That(untrusted.GetSignerAt(0).IsSignatureValid(), Is.True);
            Assert.That(untrusted.GetSignerAt(0).GetChainResult()!.IsValid(), Is.False);
            Assert.That(untrusted.GetSignerAt(0).GetChainResult()!.GetStatusCount(), Is.GreaterThan(0));
            Assert.That(untrusted.GetSignerAt(0).GetChainResult()!.GetStatusAt(0).GetStatus(), Is.Not.EqualTo(X509ChainStatusFlags.NoError));
            Assert.That(untrusted.GetSignerAt(0).GetChainResult()!.GetStatusAt(0).GetInformation(), Is.Not.Null);
            Assert.That(untrusted.GetIssues().ContainsCode(EmailIssueCode.SmimeCertificateChainInvalid), Is.True);
            Assert.That(cryptographicOnly.IsSuccess(), Is.True);
            Assert.That(cryptographicOnly.GetSignerAt(0).HasChainResult(), Is.False);
        });
    }

    [Test]
    public void SupportsCallerSuppliedCertificatesWhenSignedDataOmitsThem()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        byte[] content = Bytes("external certificate content");
        EmailSmimeSigningOptions signingOptions = new EmailSmimeSigningOptions();
        signingOptions.SetIncludeCertificates(false);
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSignersAndOptions(content, Credentials(certificate, rsa), signingOptions);

        EmailSmimeVerificationResult missing = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignature(content, signed.GetCmsBytes());
        EmailSmimeVerificationOptions suppliedOptions = Verification(certificate);
        suppliedOptions.AddExtraCertificate(certificate);
        EmailSmimeVerificationResult supplied = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, signed.GetCmsBytes(), suppliedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(missing.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.CertificateNotFound));
            Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.SmimeSignerCertificateMissing), Is.True);
            Assert.That(supplied.IsSuccess(), Is.True);
            Assert.That(supplied.GetSignerAt(0).GetCertificate()!.GetThumbprint(), Is.EqualTo(certificate.Thumbprint));
            Assert.That(suppliedOptions.GetCustomTrustRootCount(), Is.EqualTo(1));
            Assert.That(suppliedOptions.GetExtraCertificateCount(), Is.EqualTo(1));
            Assert.That(suppliedOptions.GetCustomTrustRootAt(0).Thumbprint, Is.EqualTo(certificate.Thumbprint));
            Assert.That(suppliedOptions.GetExtraCertificateAt(0).Thumbprint, Is.EqualTo(certificate.Thumbprint));
        });
    }

    [Test]
    public void EnforcesAlgorithmPolicyAndCryptographicLimits()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        byte[] content = Bytes("policy content");
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(content, Credentials(certificate, rsa));
        EmailSmimeVerificationOptions verificationOptions = Verification(certificate);
        EmailSmimeAlgorithmPolicy verificationPolicy = verificationOptions.GetAlgorithmPolicy();
        verificationPolicy.SetAllowSha256(false);
        verificationOptions.SetAlgorithmPolicy(verificationPolicy);
        EmailSmimeVerificationResult rejected = new EmailSmimeVerifier().VerifyDetachedWithContentAndCmsSignatureAndOptions(content, signed.GetCmsBytes(), verificationOptions);

        EmailSmimeSigningOptions signingOptions = new EmailSmimeSigningOptions();
        EmailSmimeAlgorithmPolicy signingPolicy = signingOptions.GetAlgorithmPolicy();
        signingPolicy.SetAllowRsaPkcs1(false);
        signingOptions.SetAlgorithmPolicy(signingPolicy);
        EmailSmimeSigningResult signingRejected = new EmailSmimeSigner().SignDetachedWithContentAndSignersAndOptions(content, Credentials(certificate, rsa), signingOptions);

        EmailSmimeSigningOptions limitOptions = new EmailSmimeSigningOptions();
        EmailCryptographicLimits limits = limitOptions.GetLimits();
        limits.SetMaxSignatureCount(1);
        limitOptions.SetLimits(limits);
        List<EmailSmimeSigningCredential> twoSigners = new List<EmailSmimeSigningCredential> { Credentials(certificate, rsa)[0], Credentials(certificate, rsa)[0] };

        Assert.Multiple(() =>
        {
            Assert.That(rejected.GetSignerAt(0).GetStatus(), Is.EqualTo(EmailSmimeSignerVerificationStatus.PolicyRejected));
            Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.SmimeAlgorithmPolicyRejected), Is.True);
            Assert.That(signingRejected.IsSuccess(), Is.False);
            Assert.That(signingRejected.GetIssues().ContainsCode(EmailIssueCode.SmimeAlgorithmPolicyRejected), Is.True);
            Assert.Throws<EmailLimitException>(() => new EmailSmimeSigner().SignDetachedWithContentAndSignersAndOptions(content, twoSigners, limitOptions));
        });
    }

    [Test]
    public void VerifiesExactMultipartSignedOctetsAndRejectsWrongCmsKind()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        const string signedOctets = "Content-Type: text/plain\r\nX-Exact:\tvalue\r\n\r\nsigned body";
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(Bytes(signedOctets), Credentials(certificate, rsa));
        string source =
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pkcs7-signature\"; micalg=sha-256\r\n\r\n" +
            "--s\r\n" + signedOctets + "\r\n" +
            "--s\r\nContent-Type: application/pkcs7-signature\r\nContent-Transfer-Encoding: base64\r\n\r\n" +
            Convert.ToBase64String(signed.GetCmsBytes()) + "\r\n--s--\r\n";
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailSecurityMultipart security = new EmailSecurityMultipartParser().ParseWithRoot(parsed.GetEntity()!).GetSecurityMultipart()!;

        EmailSmimeVerificationResult verified = new EmailSmimeVerifier().VerifyDetachedWithMultipartAndOptions(security!, Verification(certificate));
        EmailSmimeVerificationResult defaultVerified = new EmailSmimeVerifier().VerifyDetachedWithMultipart(security!);
        EmailSmimeVerificationResult wrongKind = new EmailSmimeVerifier().VerifyOpaqueWithCms(new EmailCmsWriter().WriteDataWithData(Bytes("not signed")).GetBytes());

        Assert.Multiple(() =>
        {
            Assert.That(verified.IsSuccess(), Is.True);
            Assert.That(verified.GetContent(), Is.EqualTo(Bytes(signedOctets)));
            Assert.That(defaultVerified.GetSignerCount(), Is.EqualTo(1));
            Assert.That(wrongKind.IsSuccess(), Is.False);
            Assert.That(wrongKind.GetSignerCount(), Is.Zero);
            Assert.That(wrongKind.GetIssues().ContainsCode(EmailIssueCode.SmimeSignedDataMalformed), Is.True);
        });
    }

    [Test]
    public void DetachedVerificationRejectsAValidSignatureWrappedWithAnUnsignedThirdPart()
    {
        using RSA rsa = RsaPrivateKey();
        using X509Certificate2 certificate = RsaCertificate();
        const string signedOctets = "Content-Type: text/plain\r\n\r\ntrusted body";
        EmailSmimeSigningResult signed = new EmailSmimeSigner().SignDetachedWithContentAndSigners(Bytes(signedOctets), Credentials(certificate, rsa));
        string source =
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pkcs7-signature\"; micalg=sha-256\r\n\r\n" +
            "--s\r\n" + signedOctets + "\r\n" +
            "--s\r\nContent-Type: application/pkcs7-signature\r\nContent-Transfer-Encoding: base64\r\n\r\n" +
            Convert.ToBase64String(signed.GetCmsBytes()) + "\r\n" +
            "--s\r\nContent-Type: text/html\r\n\r\n<script>unsigned()</script>\r\n" +
            "--s--\r\n";
        MimeEntity root = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;
        EmailSecurityMultipartParseResult framing = new EmailSecurityMultipartParser().ParseWithRoot(root);

        EmailSmimeVerificationResult verified = new EmailSmimeVerifier().VerifyDetachedWithMultipartAndOptions(framing.GetSecurityMultipart()!, Verification(certificate));

        Assert.That(framing.IsSuccess(), Is.False);
        Assert.That(framing.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartStructureMalformed), Is.True);
        Assert.That(verified.IsSuccess(), Is.False);
        Assert.That(verified.GetSignerCount(), Is.Zero);
        Assert.That(verified.GetIssues().ContainsCode(EmailIssueCode.SmimeSignedDataMalformed), Is.True);
    }

    static EmailSmimeVerificationOptions Verification(X509Certificate2 certificate)
    {
        EmailSmimeVerificationOptions options = new EmailSmimeVerificationOptions();
        options.AddCustomTrustRoot(certificate);
        options.SetVerificationTime(certificate.NotBefore.ToUniversalTime().AddDays(1));
        return options;
    }

    static List<EmailSmimeSigningCredential> Credentials(X509Certificate2 certificate, RSA rsa)
    {
        return new List<EmailSmimeSigningCredential> { new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(certificate, rsa, EmailSmimeDigestAlgorithm.Sha256) };
    }

    static X509Certificate2 RsaCertificate()
    {
        return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(RsaCertificateDer));
    }

    static X509Certificate2 EcCertificate()
    {
        return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(EcCertificateDer));
    }

    static RSA RsaPrivateKey()
    {
        RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(RsaPrivateKeyDer), out int bytesRead);
        if (bytesRead != Convert.FromBase64String(RsaPrivateKeyDer).Length) throw new CryptographicException("The RSA test key is malformed.");
        return rsa;
    }

    static ECDsa EcPrivateKey()
    {
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(EcPrivateKeyDer), out int bytesRead);
        if (bytesRead != Convert.FromBase64String(EcPrivateKeyDer).Length) throw new CryptographicException("The ECDSA test key is malformed.");
        return ecdsa;
    }

    static byte[] SignatureBytes(byte[] cms)
    {
        EmailCmsAsn1Node signedData = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(cms).GetContentInfo()!.GetContentNode();
        EmailCmsAsn1Node signerSet = signedData.GetChildAt(signedData.GetChildCount() - 1);
        EmailCmsAsn1Node signerInfo = signerSet.GetChildAt(0);
        int index = signerInfo.GetChildCount() - 1;
        if (signerInfo.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1)) index--;
        return signerInfo.GetChildAt(index).GetContentBytes();
    }

    static int LastIndexOf(byte[] value, byte[] search)
    {
        int index = value.Length - search.Length;
        while (index >= 0)
        {
            if (new ReadOnlySpan<byte>(value, index, search.Length).SequenceEqual(search)) return index;
            index--;
        }
        return -1;
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }
}
