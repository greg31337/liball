using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using email;

namespace email.Tests;

[NonParallelizable]
public class EmailSmimeEnvelopeTests
{
    const string CertificateOneDer = "MIIDMDCCAhigAwIBAgIUShBj/lYiGT9rySOZHVol+SxeUJswDQYJKoZIhvcNAQELBQAwITEfMB0GA1UEAwwWRW52ZWxvcGUgUmVjaXBpZW50IG9uZTAeFw0yNjA4MTEwMDE0MzNaFw0zNjA4MDgwMDE0MzNaMCExHzAdBgNVBAMMFkVudmVsb3BlIFJlY2lwaWVudCBvbmUwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC4XQ693x7NsFdSeMcCQfCpUCEUeLBO3skXTk72V2omhG6fWsr1WL7+wSyjLRYaTh7XIl+0nai/+eKBxXw2DqidoCk+AU4bKZltt4RbObYKTwJL2MaEXvV5XgGg8G2VgGHlz+EVU9Z/mD7795KQ5jEJLXQDJNeEiMhfzxMwm5M/QaXLmqqA5lwoB8eJplw3/44pkUlHjn2it0qjN8UhPLVUADkpHV7+QFZ2PFBHaDXUKKODgDiGzTmGkgrL3jc33RjHPOsCrBpDFy/vH+hf7EJMnIjmN2+cPAOhAUacRZ9KivxuRfXK2pHKJN/SnPfXe1k+6/25fdYJ24/Bx648cZ15AgMBAAGjYDBeMB8GA1UdIwQYMBaAFB0K8NsnuffP6TDdxBlz/cGnk/rCMAwGA1UdEwEB/wQCMAAwDgYDVR0PAQH/BAQDAgUgMB0GA1UdDgQWBBQdCvDbJ7n3z+kw3cQZc/3Bp5P6wjANBgkqhkiG9w0BAQsFAAOCAQEAe/TNV9HE2pP/nxhOy2FNvWjVaOd1j8fUj9ZAmS68YhXt9MeBnoiP//C/OHcPsTbrwBJPD6QtUfrKCK7ZiP/6tcQ9WNr0/TLWkpozR6NXPh1pi/66CW2Yb/JzEGbsYL2Krl5D5k6mauw4evhBwosILFO3oIgi3NbnvR3dnSRGU0e5HSFh9TIvC5FCU6a+WIbsRopnS2e5WgxdKFvtzmJ24dzYVY1KQhz4Jc6BkmAb508O1uE5V5AcmEBkxRhK4WSlwBuCJ1m/kgSDlKUxpV84USuJ5FoH0XaB2pjo+1LbpV4vX2LK+6gBpS8fAigiAo7+CooL8PClipXxObFiFqETGA==";
    const string PrivateKeyOneDer = "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC4XQ693x7NsFdSeMcCQfCpUCEUeLBO3skXTk72V2omhG6fWsr1WL7+wSyjLRYaTh7XIl+0nai/+eKBxXw2DqidoCk+AU4bKZltt4RbObYKTwJL2MaEXvV5XgGg8G2VgGHlz+EVU9Z/mD7795KQ5jEJLXQDJNeEiMhfzxMwm5M/QaXLmqqA5lwoB8eJplw3/44pkUlHjn2it0qjN8UhPLVUADkpHV7+QFZ2PFBHaDXUKKODgDiGzTmGkgrL3jc33RjHPOsCrBpDFy/vH+hf7EJMnIjmN2+cPAOhAUacRZ9KivxuRfXK2pHKJN/SnPfXe1k+6/25fdYJ24/Bx648cZ15AgMBAAECggEABScS8lRFa+iuST1Jnpd2MzijcuaqCabWxl3nazPVPJY0zZE2r1pOZfNVeEEssFMXnbV+37ExMYvL1+Y5Sv0xdoN36niB3fkw4MhYLwDOjeEPRpWKMtIumJP7qqt4/JW/C0Te3Mg7kNRbHJKtITJROBk7CnaYootqHamrJI+RIOShAc7TZvvLwBqgP/8EXMZTzPn7+l3Rpt7AzI1uujml6LCwhd94+OKi3cqAEn/NuL9jagIoK6Lmx6zOxll8Hk5GEhVGIFniI97aX0Ez3291F4bGR29gz+4gFuv8gkoNhRGbWlJn2PQHLvnqSMyrpd9ro/qrw5PzkdBSS6o3zwxKUwKBgQDjag1UCDBtaIyQc15Po6Y2G1dq0lqEoSKdibNL6J2GeMxgXFSpSIQfFbOXGgZ9/J9pExvcuQv2WuPuy1LWbsDLAMOxx/XAF75II0ptw7ABWZTSaYgQ0IcEOMu4FCRq3RimppCaXUq1CcDXQiVL3+g7avDPtlpqo23J2Ha49yakjwKBgQDPia2YG6OmixOeoEMlrS5ECWJglxMV2euyQhtcYzUsHQ7hDqyJ7u8Yeqs5e1kfJCl03YddYUl8O+upu2d7nB8JWasFROTTTOHFh1k1t3kwApKd45cWInXFaGsRC0A1nlY2mjqHnQ9CWatjGimZqWqXvMGuqUyR2787IFykY6hxdwKBgAd0HmO2sP+lAZZqcZjioDv9wWu64USfk2becIX4ZdFWfDxcSloSCmKKb4xwSe4vLXpgKfWeCwpUCEx3VYu5rY/5wnA9QYy+OnpazcGzPD2PFhWfZ9pFO2ekBOlPn7Hw9THhG7QzgpNUcoGbpWVLPW1VH/du3p/II30OHzYIw7d5AoGBAMlfR6c9uk7Qx5q02jJmlWvCx0f9jrE3H/F5H19Hi8Jf9538RQIZarmP3TALNXB4ABm7EatTyJ/Y7FNm2YQg+iRycvTL4lP8V1z4dWEfw2EIPgchW+pISYngRXLmPclI91JH197aUIIKy7d/HTpSHnFY332AHCGy3RybzAheAoUFAoGBAKKgFd2S3LugQz+ZCIxb0YfYPffolk07oknTyEnLBNac/kprqspVizSgV2JYOEt23PNkTdckzEJ4r+tpqTMCZPrM27wM1KkJvS4G142pD6R+JfI63EakoIZhq6LhBnnV/GD6HogY4AtdrL2BiQz8WMBuEY4DARXY/n7XOZ5lDcw9";
    const string CertificateTwoDer = "MIIDMDCCAhigAwIBAgIULufQoIRDaNITS2ZtL07GEvqgSfgwDQYJKoZIhvcNAQELBQAwITEfMB0GA1UEAwwWRW52ZWxvcGUgUmVjaXBpZW50IHR3bzAeFw0yNjA4MTEwMDE0MzNaFw0zNjA4MDgwMDE0MzNaMCExHzAdBgNVBAMMFkVudmVsb3BlIFJlY2lwaWVudCB0d28wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC1rFIbyBDz71K3OW0PoF10le53Q7OcnaJhCtt4v9MhK7kFKMLxCyJo+5KGfUkGGcVBUfXopMD8HibyxSnyTGgwKj1oUQSKbcKpE+4Y361oK15M79mEzNf06xmwdJn5V3ielOrLsibWXto8C+Imwnqm2BSRlBUE+hFfo4EXlVxc/Bgs3c3HOHo0QqXtUuV6Xo0k0u+YZVaL7Q0qQx6CNCfT3WByr/DSsPL3i+q9+/8tMCyl62h0gZVS36AynrTND95Gm9q3MFYNFOjU+6xn/9Z9Ed29+m8bK04wMvC8Dltv2pGLcHMKUIZoFHe+npaQoUaRgc7plvooPU0LzSNJmHWpAgMBAAGjYDBeMB8GA1UdIwQYMBaAFO8QVCXZmjgw+IC2vgsoG5+J3zUwMAwGA1UdEwEB/wQCMAAwDgYDVR0PAQH/BAQDAgUgMB0GA1UdDgQWBBTvEFQl2Zo4MPiAtr4LKBufid81MDANBgkqhkiG9w0BAQsFAAOCAQEAkRLhxrZSw5bODCsCV4XNkAZ6dNcuvms4MAUDu9CNQTQ6femYe18COxS/ca0WLQWE8T8m6j7Ymv16zy0VDjQUdRBVUuQu41r6uODohCXvpBHctsumPte3w9i+hvJMRHRLIFwPI7VmyOr+mHkHzeGNGhRy6Ksk/1Kd1HbfXGYm4952bP8otiUWcPGdVoY3VGKDPgUg6ueQ43sY+TG5J1OLgbdFKn3uW5h00MRUE6HUxNi/7DMgF06EAD00jjt6H+p8RtRkvko190St4Ug+UyfrK59HJj5vvvTqhmFcIJxNJTQ3WcnhAjeRAcq9nhwYhEjxIDQ1zZFaR9riAMYqc0cILg==";
    const string PrivateKeyTwoDer = "MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQC1rFIbyBDz71K3OW0PoF10le53Q7OcnaJhCtt4v9MhK7kFKMLxCyJo+5KGfUkGGcVBUfXopMD8HibyxSnyTGgwKj1oUQSKbcKpE+4Y361oK15M79mEzNf06xmwdJn5V3ielOrLsibWXto8C+Imwnqm2BSRlBUE+hFfo4EXlVxc/Bgs3c3HOHo0QqXtUuV6Xo0k0u+YZVaL7Q0qQx6CNCfT3WByr/DSsPL3i+q9+/8tMCyl62h0gZVS36AynrTND95Gm9q3MFYNFOjU+6xn/9Z9Ed29+m8bK04wMvC8Dltv2pGLcHMKUIZoFHe+npaQoUaRgc7plvooPU0LzSNJmHWpAgMBAAECggEACWU9ZIJsSLFP2Hn30fRmi5OqyYWO0cK1QHTh9NS22ThRppqnukpBH/zNZHcw7PKC6RCRZH62HFPS39X33WK7i1d5Ot6LGuhM8Ow+8WUKqssouAHk1fa3acs0K5QG19urjp1U/JkyDlQez+GCyKO2LtAYquQQUjmR5x2lnM/0qY+pqzpdYQOxBoKEbJdnRiv5tV1nm/5q3nLBsW9UVBiPdEQ2nEUPbcQNW/n5DkXkzCXEE/9ho5jIPC0S3u0DW2jIb+GfcRVhkO7BR10eEPOp+udaArh9L482lmdmehccUV8gbBw6JQwKB9F+dGcpDpdLLRT5KQP2LC539mYQiIb8IQKBgQDrx/4yqY/iLyJav+3QENqV6n5Hm7dxV1Qzv7u9ilG0Krdib+EzzUPr7a2uQVseywe1SP27z8yaMsWR5OLVOAgl12tY8vgjRVs4r/+Mg2vZ/b2PYx+WsNjmjChUaxyAjeFbwefRgl7DjK8cR1srlPOHwCQpEFYvtC7wiTAVw3g2DwKBgQDFQIPqa4aaEb/Li/kItGX2uUo1rav3kdSownhxBjf8qXnD93PSW/kttVLhBnn5AOHAi5ja8hG+7icEvIWFHUcT073zw9rxphCIZ/nm1iWXF3/X95G9BODyMZZteZyTJGHPoEuIQf0cMiQ6l6uKX1ptUQ1qYaC57SjWM2eOiWKQxwKBgECTJJaBhAuhdxo9qIcspoEamP1Js4MSNNh7cZKmuFvMuy71j0sds7WBAelQswELABBxbjSq49FziFK9yjY1Q14B0r+ow7b9Ps+WvDmioL4yVWFY94TQ6Iaz6jO8pjC7KvZzc+cYepW6CReIn3qfL9eFEyI//CrSgWyVVhsPzvOXAoGAFw1KHVxBl8HZQ0VxbiX/AI148bsBLOabDrnjb3oFkv6PX2qM25mqDkBxliC2onYK7ACkdXnyvviAENqjVAIu609ElLdf3/vDVN2p4+NE81pos2mYW5m1XDiVVufud3e6ApS+HwdvOBtYBozxHDO2kq31Pmh4ayIfMqFBSfQeU8MCgYByUO6JcXUaJNAw90bX6eoUQVq0uLZAkP8xPXc59IhPlYzSVaMBnU9I3HFHbw8RVcMhpnm2SQgLSix73j/4NHI6ECZMn5sBQ+pmFc7adMAQK/NNBUOyWE9LqsjpV56BbQX7+gi9gCgsrNAh6Ng75MFIFpaokhyvttVOdwSLMXS9mQ==";

    [Test]
    public void EncryptsForMultipleRecipientsAndEachCanDecryptAuthenticatedContent()
    {
        using X509Certificate2 firstCertificate = CertificateOne();
        using X509Certificate2 secondCertificate = CertificateTwo();
        using RSA firstKey = PrivateKeyOne();
        using RSA secondKey = PrivateKeyTwo();
        byte[] content = Bytes("Content-Type: text/plain\r\n\r\nconfidential");

        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipients(content, new List<X509Certificate2> { firstCertificate, secondCertificate })!;
        EmailSmimeDecryptionResult first = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(encrypted!.GetCmsBytes(), Credentials(firstCertificate, firstKey))!;
        EmailSmimeDecryptionResult second = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(encrypted.GetCmsBytes(), Credentials(secondCertificate, secondKey))!;
        EmailCmsReadResult cms = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(encrypted.GetCmsBytes());
        EmailSmimeDecryptionCredential certificateOnly = new EmailSmimeDecryptionCredentialOperations().CreateWithCertificate(firstCertificate);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.IsSuccess(), Is.True);
            Assert.That(encrypted.GetRecipientCount(), Is.EqualTo(2));
            Assert.That(encrypted.GetContentAlgorithm(), Is.EqualTo(EmailSmimeContentEncryptionAlgorithm.Aes256Gcm));
            Assert.That(encrypted.GetKeyTransportAlgorithm(), Is.EqualTo(EmailSmimeKeyTransportAlgorithm.RsaOaepSha256));
            Assert.That(encrypted.IsAuthenticated(), Is.True);
            Assert.That(cms.GetContentInfo()!.GetContentKind(), Is.EqualTo(EmailCmsContentKind.AuthEnvelopedData));
            Assert.That(first!.IsSuccess(), Is.True);
            Assert.That(first.HasRecipientCertificate(), Is.True);
            Assert.That(first.IsAuthenticated(), Is.True);
            Assert.That(first.GetContent(), Is.EqualTo(content));
            Assert.That(first.GetRecipientCertificate()!.GetThumbprint(), Is.EqualTo(firstCertificate.Thumbprint));
            Assert.That(second!.IsSuccess(), Is.True);
            Assert.That(second.GetContent(), Is.EqualTo(content));
            Assert.That(second.GetRecipientCertificate()!.GetThumbprint(), Is.EqualTo(secondCertificate.Thumbprint));
            Assert.That(certificateOnly.GetCertificate().Thumbprint, Is.EqualTo(firstCertificate.Thumbprint));
            Assert.That(certificateOnly.HasPrivateKey(), Is.False);
        });

        byte[] returned = first!.GetContent()!;
        returned![0] = 0;
        Assert.That(first.GetContent(), Is.EqualTo(content));
    }

    [TestCase(EmailSmimeContentEncryptionAlgorithm.Aes128Gcm, true, EmailCmsContentKind.AuthEnvelopedData)]
    [TestCase(EmailSmimeContentEncryptionAlgorithm.Aes128Cbc, false, EmailCmsContentKind.EnvelopedData)]
    [TestCase(EmailSmimeContentEncryptionAlgorithm.Aes256Cbc, false, EmailCmsContentKind.EnvelopedData)]
    public void SupportsConfiguredAesContentAlgorithms(EmailSmimeContentEncryptionAlgorithm algorithm, bool authenticated, EmailCmsContentKind expectedKind)
    {
        using X509Certificate2 certificate = CertificateOne();
        using RSA key = PrivateKeyOne();
        EmailSmimeEncryptionOptions options = new EmailSmimeEncryptionOptions();
        options.SetContentAlgorithm(algorithm);

        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("algorithm content"), new List<X509Certificate2> { certificate }, options)!;
        EmailSmimeDecryptionResult decrypted = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(encrypted!.GetCmsBytes(), Credentials(certificate, key))!;

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.IsSuccess(), Is.True);
            Assert.That(new EmailCmsReader().ParseWithInputUsingReadOnlySpan(encrypted.GetCmsBytes()).GetContentInfo()!.GetContentKind(), Is.EqualTo(expectedKind));
            Assert.That(decrypted!.IsSuccess(), Is.True);
            Assert.That(decrypted.GetContentAlgorithm(), Is.EqualTo(algorithm));
            Assert.That(decrypted.IsAuthenticated(), Is.EqualTo(authenticated));
            Assert.That(decrypted.GetContent(), Is.EqualTo(Bytes("algorithm content")));
        });
    }

    [Test]
    public void AuthenticatedTamperingReturnsFailureWithoutPlaintext()
    {
        using X509Certificate2 certificate = CertificateOne();
        using RSA key = PrivateKeyOne();
        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipients(Bytes("authenticated content"), new List<X509Certificate2> { certificate })!;
        byte[] cms = encrypted!.GetCmsBytes();
        byte[] ciphertext = Ciphertext(cms);
        int offset = LastIndexOf(cms, ciphertext);
        cms[offset + ciphertext.Length / 2] ^= 1;

        EmailSmimeDecryptionResult result = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(cms, Credentials(certificate, key))!;

        EmailSmimeEncryptionOptions cbcOptions = new EmailSmimeEncryptionOptions();
        cbcOptions.SetContentAlgorithm(EmailSmimeContentEncryptionAlgorithm.Aes128Cbc);
        EmailSmimeEncryptionResult cbcEncrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("authenticated content"), new List<X509Certificate2> { certificate }, cbcOptions)!;
        byte[] cbcCms = cbcEncrypted.GetCmsBytes();
        byte[] cbcCiphertext = Ciphertext(cbcCms);
        int cbcOffset = LastIndexOf(cbcCms, cbcCiphertext);
        cbcCms[cbcOffset + 15] ^= 1;
        EmailSmimeDecryptionResult cbcFailure = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(cbcCms, Credentials(certificate, key))!;

        byte[] unsupportedCms = cbcEncrypted.GetCmsBytes();
        byte[] aes128CbcObjectIdentifier = Convert.FromHexString("608648016503040102");
        int algorithmOffset = LastIndexOf(unsupportedCms, aes128CbcObjectIdentifier);
        Assert.That(algorithmOffset, Is.GreaterThanOrEqualTo(0));
        unsupportedCms[algorithmOffset + aes128CbcObjectIdentifier.Length - 1] = 0x7F;
        EmailSmimeDecryptionResult unsupported = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(unsupportedCms, Credentials(certificate, key))!;

        Assert.Multiple(() =>
        {
            Assert.That(result!.IsSuccess(), Is.False);
            Assert.That(result.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.AuthenticationFailed));
            Assert.That(result.HasContent(), Is.False);
            Assert.That(result.IsAuthenticated(), Is.False);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.SmimeAuthenticationFailed), Is.True);
            Assert.That(cbcFailure.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.DecryptionFailed));
            Assert.That(cbcFailure.HasContent(), Is.False);
            Assert.That(unsupported.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.UnsupportedAlgorithm));
            Assert.That(unsupported.HasContent(), Is.False);
        });
    }

    [Test]
    public void DistinguishesMissingRecipientsFromNonOracularKeyUnwrapFailure()
    {
        using X509Certificate2 firstCertificate = CertificateOne();
        using X509Certificate2 secondCertificate = CertificateTwo();
        using RSA secondKey = PrivateKeyTwo();
        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipients(Bytes("recipient lookup"), new List<X509Certificate2> { firstCertificate })!;

        EmailSmimeDecryptionResult missing = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(encrypted!.GetCmsBytes(), Credentials(secondCertificate, secondKey))!;
        EmailSmimeDecryptionResult wrongKey = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(encrypted.GetCmsBytes(), Credentials(firstCertificate, secondKey))!;

        Assert.Multiple(() =>
        {
            Assert.That(missing!.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.RecipientNotFound));
            Assert.That(missing.HasContent(), Is.False);
            Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.SmimeRecipientNotFound), Is.True);
            Assert.That(wrongKey!.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.KeyUnwrapFailed));
            Assert.That(wrongKey.HasContent(), Is.False);
            Assert.That(wrongKey.GetIssues().ContainsCode(EmailIssueCode.SmimeDecryptionFailed), Is.True);
            Assert.That(wrongKey.GetRecipientCertificate()!.GetThumbprint(), Is.EqualTo(firstCertificate.Thumbprint));
        });
    }

    [Test]
    public void PolicyGatesLegacyKeyTransportAndContentAlgorithms()
    {
        using X509Certificate2 certificate = CertificateOne();
        using RSA key = PrivateKeyOne();
        EmailSmimeEncryptionOptions legacyOptions = new EmailSmimeEncryptionOptions();
        legacyOptions.SetKeyTransportAlgorithm(EmailSmimeKeyTransportAlgorithm.RsaPkcs1);
        EmailSmimeEncryptionResult rejectedEncryption = new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("legacy"), new List<X509Certificate2> { certificate }, legacyOptions)!;

        EmailSmimeEncryptionPolicy enabledPolicy = legacyOptions.GetPolicy();
        enabledPolicy.SetAllowRsaPkcs1Encryption(true);
        legacyOptions.SetPolicy(enabledPolicy);
        EmailSmimeEncryptionResult legacy = new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("legacy"), new List<X509Certificate2> { certificate }, legacyOptions)!;
        EmailSmimeDecryptionResult allowed = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(legacy!.GetCmsBytes(), Credentials(certificate, key))!;

        EmailSmimeDecryptionOptions rejectedOptions = new EmailSmimeDecryptionOptions();
        EmailSmimeEncryptionPolicy rejectedPolicy = rejectedOptions.GetPolicy();
        rejectedPolicy.SetAllowRsaPkcs1Decryption(false);
        rejectedOptions.SetPolicy(rejectedPolicy);
        EmailSmimeDecryptionResult rejectedDecryption = new EmailSmimeDecryptor().DecryptWithCmsAndCredentialsAndOptions(legacy.GetCmsBytes(), Credentials(certificate, key), rejectedOptions)!;

        Assert.Multiple(() =>
        {
            Assert.That(rejectedEncryption!.IsSuccess(), Is.False);
            Assert.That(rejectedEncryption.GetIssues().ContainsCode(EmailIssueCode.SmimeEncryptionPolicyRejected), Is.True);
            Assert.That(legacy.IsSuccess(), Is.True);
            Assert.That(allowed!.IsSuccess(), Is.True);
            Assert.That(allowed.GetKeyTransportAlgorithm(), Is.EqualTo(EmailSmimeKeyTransportAlgorithm.RsaPkcs1));
            Assert.That(rejectedDecryption!.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.PolicyRejected));
            Assert.That(rejectedDecryption.HasContent(), Is.False);
        });
    }

    [Test]
    public void EnforcesRecipientInputAndOperationLimits()
    {
        using X509Certificate2 firstCertificate = CertificateOne();
        using X509Certificate2 secondCertificate = CertificateTwo();
        using RSA firstKey = PrivateKeyOne();
        EmailSmimeEncryptionOptions recipientOptions = new EmailSmimeEncryptionOptions();
        EmailCryptographicLimits recipientLimits = recipientOptions.GetLimits();
        recipientLimits.SetMaxRecipientCount(1);
        recipientOptions.SetLimits(recipientLimits);

        EmailSmimeEncryptionOptions inputOptions = new EmailSmimeEncryptionOptions();
        EmailCryptographicLimits inputLimits = inputOptions.GetLimits();
        inputLimits.SetMaxInputBytes(3);
        inputOptions.SetLimits(inputLimits);

        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipients(Bytes("operations"), new List<X509Certificate2> { firstCertificate })!;
        EmailSmimeDecryptionOptions operationOptions = new EmailSmimeDecryptionOptions();
        EmailCryptographicLimits operationLimits = operationOptions.GetLimits();
        operationLimits.SetMaxOperationCount(1);
        operationOptions.SetLimits(operationLimits);

        Assert.Multiple(() =>
        {
            Assert.Throws<EmailLimitException>(() => new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("x"), new List<X509Certificate2> { firstCertificate, secondCertificate }, recipientOptions));
            Assert.Throws<EmailLimitException>(() => new EmailSmimeEncryptor().EncryptWithContentAndRecipientsAndOptions(Bytes("four"), new List<X509Certificate2> { firstCertificate }, inputOptions));
            Assert.Throws<EmailLimitException>(() => new EmailSmimeDecryptor().DecryptWithCmsAndCredentialsAndOptions(encrypted!.GetCmsBytes(), Credentials(firstCertificate, firstKey), operationOptions));
        });
    }

    [Test]
    public void DecryptsBase64MimeEntitiesAndRejectsWrongCmsKinds()
    {
        using X509Certificate2 certificate = CertificateOne();
        using RSA key = PrivateKeyOne();
        byte[] content = Bytes("MIME encrypted content");
        EmailSmimeEncryptionResult encrypted = new EmailSmimeEncryptor().EncryptWithContentAndRecipients(content, new List<X509Certificate2> { certificate })!;
        string source = "Content-Type: application/pkcs7-mime; smime-type=authEnveloped-data\r\nContent-Transfer-Encoding: base64\r\n\r\n" + Convert.ToBase64String(encrypted!.GetCmsBytes());
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());

        EmailSmimeDecryptionResult decrypted = new EmailSmimeDecryptor().DecryptWithEntityAndCredentials(parsed.GetEntity()!, Credentials(certificate, key))!;
        EmailSmimeDecryptionResult decryptedWithOptions = new EmailSmimeDecryptor().DecryptWithEntityAndCredentialsAndOptions(parsed.GetEntity()!, Credentials(certificate, key), new EmailSmimeDecryptionOptions())!;
        EmailSmimeDecryptionResult malformed = new EmailSmimeDecryptor().DecryptWithCmsAndCredentials(new EmailCmsWriter().WriteDataWithData(Bytes("wrong kind")).GetBytes(), Credentials(certificate, key))!;

        Assert.Multiple(() =>
        {
            Assert.That(decrypted!.IsSuccess(), Is.True);
            Assert.That(decrypted.GetContent(), Is.EqualTo(content));
            Assert.That(decryptedWithOptions.GetContent(), Is.EqualTo(content));
            Assert.That(malformed!.GetStatus(), Is.EqualTo(EmailSmimeDecryptionStatus.Malformed));
            Assert.That(malformed.HasContent(), Is.False);
            Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.SmimeEnvelopedDataMalformed), Is.True);
        });
    }

    static List<EmailSmimeDecryptionCredential> Credentials(X509Certificate2 certificate, RSA key)
    {
        return new List<EmailSmimeDecryptionCredential> { new EmailSmimeDecryptionCredentialOperations().CreateWithCertificateAndPrivateKey(certificate, key) };
    }

    static X509Certificate2 CertificateOne() { return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(CertificateOneDer)); }
    static X509Certificate2 CertificateTwo() { return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(CertificateTwoDer)); }
    static RSA PrivateKeyOne() { return LoadKey(PrivateKeyOneDer); }
    static RSA PrivateKeyTwo() { return LoadKey(PrivateKeyTwoDer); }

    static RSA LoadKey(string encoded)
    {
        byte[] bytes = Convert.FromBase64String(encoded);
        RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(bytes, out int read);
        if (read != bytes.Length) throw new CryptographicException("The RSA test key is malformed.");
        return rsa;
    }

    static byte[] Ciphertext(byte[] cms)
    {
        EmailCmsAsn1Node envelope = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(cms).GetContentInfo()!.GetContentNode();
        return envelope.GetChildAt(2).GetChildAt(2).GetContentBytes();
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
