using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailDkimVerifierTests
{
    [Test]
    public void VerifiesEverySignatureWithItsCallerSuppliedParsedKey()
    {
        using RSA firstKey = Rsa();
        using RSA secondKey = Rsa();
        byte[] body = Bytes("Message body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: signed\r\n");
        AddSignature(headers, body, firstKey, "first.test", "one", 1700000000, 0, 0);
        AddSignature(headers, body, secondKey, "second.test", "two", 1700000001, 0, 0);
        List<EmailDkimVerificationKey> keys =
        [
            Binding("first.test", "one", Imported(firstKey)),
            Binding("second.test", "two", Imported(secondKey))
        ];
        EmailDkimVerificationOptions options = VerificationOptions(1700000100);

        EmailDkimVerificationResult result = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSignatureCount(), Is.EqualTo(2));
            Assert.That(result.GetPassingSignatureCount(), Is.EqualTo(2));
            Assert.That(result.HasPassingSignature(), Is.True);
            Assert.That(result.GetSignature(0).GetSignature()!.GetSigningDomain(), Is.EqualTo("second.test"));
            Assert.That(result.GetSignature(1).GetSignature()!.GetSigningDomain(), Is.EqualTo("first.test"));
            Assert.That(result.GetSignatures().All(value => value.GetStatus() == EmailDkimVerificationStatus.Pass), Is.True);
            Assert.That(result.GetSignatures().All(value => value.IsBodyHashValid() && value.IsSignatureValid()), Is.True);
            Assert.That(result.GetSignatures().All(value => value.GetPolicyStatus() == EmailDkimVerificationPolicyStatus.Accepted), Is.True);
            Assert.That(result.GetSignatures().All(value => value.GetAuthenticationResultKind() == EmailAuthenticationResultKind.Pass), Is.True);
        });
    }

    [Test]
    public void ResolvesVerificationKeysOnDemandThroughThePublicInterface()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: resolver\r\n");
        AddSignature(headers, body, rsa, "example.test", "key", 1700000000, 0, 0);
        RecordingDkimKeyResolver resolver = new RecordingDkimKeyResolver(Binding("example.test", "key", Imported(rsa)));

        EmailDkimVerificationResult result = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, resolver, VerificationOptions(1700000100));

        Assert.That(result.GetSignature(0).GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
        EmailDkimSignatureVerificationResult signature = result.GetSignature(0);
        Assert.That(signature.GetHeader().GetRawFieldBytes(), Is.EqualTo(headers.GetAt(0).GetRawFieldBytes()));
        Assert.That(signature.GetParseResult().IsSuccess(), Is.True);
        Assert.That(signature.HasVerificationKey(), Is.True);
        Assert.That(signature.GetVerificationKey(), Is.Not.Null);
        Assert.That(signature.HasHeaderCanonicalization(), Is.True);
        Assert.That(signature.GetHeaderCanonicalization(), Is.Not.Null);
        Assert.That(resolver.GetResolutionCount(), Is.EqualTo(1));
        Assert.That(resolver.GetLastDomain(), Is.EqualTo("example.test"));
        Assert.That(resolver.GetLastSelector(), Is.EqualTo("key"));

        RecordingDkimKeyResolver defaultResolver = new RecordingDkimKeyResolver(Binding("example.test", "key", Imported(rsa)));
        EmailDkimVerificationResult defaultResult = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeyResolver(headers, body, defaultResolver);
        Assert.That(defaultResult.GetSignature(0).GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
    }

    [Test]
    public void DistinguishesBodyAndHeaderTamperingAndStillChecksLaterSignatures()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("original\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: original\r\n");
        AddSignature(headers, body, rsa, "example.test", "key", 1700000000, 0, 0);
        headers.Insert(0, new EmailHeaderOperations().CreateWithNameAndRawValue("DKIM-Signature", Bytes(" v=1; a=rsa-sha256; d=bad.test")));
        List<EmailDkimVerificationKey> keys = [Binding("example.test", "key", Imported(rsa))];
        EmailDkimVerificationOptions options = VerificationOptions(1700000100);

        EmailDkimVerificationResult bodyChanged = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, Bytes("changed\r\n"), keys, options);
        headers.GetAt(2).SetValue("changed");
        EmailDkimVerificationResult headerChanged = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, options);

        Assert.Multiple(() =>
        {
            Assert.That(bodyChanged.GetSignatureCount(), Is.EqualTo(2));
            Assert.That(bodyChanged.GetSignature(0).GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureSyntaxError));
            Assert.That(bodyChanged.GetSignature(1).GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.BodyHashMismatch));
            Assert.That(bodyChanged.GetSignature(1).GetAuthenticationResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Fail));
            Assert.That(bodyChanged.GetSignature(1).HasBodyCanonicalization(), Is.True);
            Assert.That(headerChanged.GetSignature(1).GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureMismatch));
            Assert.That(headerChanged.GetSignature(1).IsBodyHashValid(), Is.True);
            Assert.That(headerChanged.GetSignature(1).IsSignatureValid(), Is.False);
        });
    }

    [Test]
    public void EnforcesIdentityTimeKeyFlagsAndLocalPolicy()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\nextra\r\n");
        EmailHeaders identityHeaders = ParseHeaders("From: sender@example.test\r\nSubject: identity\r\n");
        identityHeaders.Insert(0, CustomSignature(identityHeaders, body, rsa, "i=user@sub.example.test;", 1700000000, 0, 0));
        EmailDkimKey strictKey = DnsKey(rsa, "t=s");
        EmailDkimVerificationOptions normal = VerificationOptions(1700000100);
        EmailDkimSignatureVerificationResult identity = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(identityHeaders, body, [Binding("example.test", "key", strictKey)], normal).GetSignature(0);

        EmailHeaders futureHeaders = ParseHeaders("From: sender@example.test\r\nSubject: future\r\n");
        AddSignature(futureHeaders, body, rsa, "example.test", "key", 1700001000, 0, 0);
        EmailDkimSignatureVerificationResult future = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(futureHeaders, body, [Binding("example.test", "key", Imported(rsa))], normal).GetSignature(0);

        EmailHeaders expiredHeaders = ParseHeaders("From: sender@example.test\r\nSubject: expired\r\n");
        AddSignature(expiredHeaders, body, rsa, "example.test", "key", 1699990000, 0, 1699991000);
        EmailDkimSignatureVerificationResult expired = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(expiredHeaders, body, [Binding("example.test", "key", Imported(rsa))], normal).GetSignature(0);

        EmailHeaders policyHeaders = ParseHeaders("From: sender@example.test\r\nSubject: policy\r\n");
        AddSignature(policyHeaders, body, rsa, "example.test", "key", 1700000000, 4, 0);
        EmailDkimKey testingKey = DnsKey(rsa, "t=y");
        EmailDkimSignatureVerificationResult warned = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(policyHeaders, body, [Binding("example.test", "key", testingKey)], normal).GetSignature(0);
        EmailDkimVerificationOptions reject = VerificationOptions(1700000100);
        reject.SetRejectTestingKeys(true);
        reject.SetRejectUnsignedBodyContent(true);
        EmailDkimSignatureVerificationResult rejected = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(policyHeaders, body, [Binding("example.test", "key", testingKey)], reject).GetSignature(0);

        Assert.Multiple(() =>
        {
            Assert.That(identity.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.IdentityDomainMismatch));
            Assert.That(identity.RequiresStrictIdentity(), Is.True);
            Assert.That(future.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.TimestampInFuture));
            Assert.That(expired.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureExpired));
            Assert.That(warned.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(warned.GetPolicyStatus(), Is.EqualTo(EmailDkimVerificationPolicyStatus.AcceptedWithWarnings));
            Assert.That(warned.IsTestingKey(), Is.True);
            Assert.That(warned.HasUnsignedBodyContent(), Is.True);
            Assert.That(rejected.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.PolicyRejected));
            Assert.That(rejected.GetPolicyStatus(), Is.EqualTo(EmailDkimVerificationPolicyStatus.Rejected));
            Assert.That(rejected.IsBodyHashValid(), Is.True);
            Assert.That(rejected.IsSignatureValid(), Is.True);
            Assert.That(rejected.GetAuthenticationResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Policy));
        });
    }

    [Test]
    public void ReportsCallerKeyApplicabilityFailures()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: keys\r\n");
        AddSignature(headers, body, rsa, "example.test", "key", 1700000000, 0, 0);
        EmailDkimVerificationOptions options = VerificationOptions(1700000100);

        EmailDkimSignatureVerificationResult missing = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [], options).GetSignature(0);
        EmailDkimSignatureVerificationResult revoked = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding("example.test", "key", DnsRecord("p="))], options).GetSignature(0);
        EmailDkimSignatureVerificationResult service = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding("example.test", "key", DnsKey(rsa, "s=calendar"))], options).GetSignature(0);
        EmailDkimSignatureVerificationResult hash = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding("example.test", "key", DnsKey(rsa, "h=sha1"))], options).GetSignature(0);

        Assert.Multiple(() =>
        {
            Assert.That(missing.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyNotFound));
            Assert.That(revoked.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyRevoked));
            Assert.That(service.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyServiceNotAllowed));
            Assert.That(hash.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyHashNotAllowed));
        });
    }

    [Test]
    public void EnforcesVerificationResourceLimits()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: limits\r\n");
        AddSignature(headers, body, rsa, "example.test", "key", 1700000000, 0, 0);
        List<EmailDkimVerificationKey> keys = [Binding("example.test", "key", Imported(rsa))];
        EmailDkimVerificationOptions signatureLimit = VerificationOptions(1700000100);
        signatureLimit.SetMaximumSignatureHeaderBytes(10);
        EmailDkimVerificationOptions bodyLimit = VerificationOptions(1700000100);
        bodyLimit.SetMaximumBodyInputBytes(1);
        EmailDkimVerificationOptions countLimit = VerificationOptions(1700000100);
        countLimit.SetMaximumSignatureCount(1);
        headers.Insert(0, headers.GetAt(0).Clone());

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, signatureLimit))!.GetLimitName(), Is.EqualTo("MaxDkimSignatureHeaderBytes"));
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, bodyLimit))!.GetLimitName(), Is.EqualTo("MaxDkimBodyInputBytes"));
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, countLimit))!.GetLimitName(), Is.EqualTo("MaxDkimSignatureCount"));
        });
    }

    static void AddSignature(EmailHeaders headers, byte[] body, RSA rsa, string domain, string selector, long timestamp, long bodyLength, long expiration)
    {
        EmailDkimSigningOptions options = SigningOptions(domain, selector, timestamp);
        if (bodyLength > 0) options.SetBodyLengthLimit(bodyLength);
        if (expiration > 0) options.SetExpiration(expiration);
        EmailDkimSigningResult signed = new EmailDkimSigner().Sign(headers, body, rsa, options);
        headers.Insert(0, signed.GetHeader());
    }

    static EmailHeader CustomSignature(EmailHeaders headers, byte[] body, RSA rsa, string extraTags, long timestamp, long expiration, long bodyLength)
    {
        EmailDkimBodyCanonicalizationResult bodyResult = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(body, EmailDkimCanonicalizationKind.Relaxed).GetResult();
        string value = " v=1; a=rsa-sha256; c=relaxed/relaxed; d=example.test; s=key; t=" + timestamp + "; " + extraTags + " h=From:Subject; bh=" + Convert.ToBase64String(bodyResult.GetHash().GetDigestBytes()) + "; b=";
        if (expiration > 0) value = value.Replace(" h=", " x=" + expiration + "; h=");
        if (bodyLength > 0) value = value.Replace(" h=", " l=" + bodyLength + "; h=");
        EmailHeader unsigned = new EmailHeaderOperations().CreateWithNameAndRawValue("DKIM-Signature", Bytes(value));
        EmailDkimSignature parsed = new EmailDkimSignatureParser().ParseWithInputAndMode(unsigned.GetRawValue(), EmailStructuredValueParseMode.Strict).GetValue()!;
        byte[] hash = SHA256.HashData(new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(headers, parsed!, unsigned).GetCanonicalizedHeaders());
        string signature = Convert.ToBase64String(rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return new EmailHeaderOperations().CreateWithNameAndRawValue("DKIM-Signature", Bytes(value + signature));
    }

    static EmailDkimSigningOptions SigningOptions(string domain, string selector, long timestamp)
    {
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();
        options.SetDomain(domain);
        options.SetSelector(selector);
        options.SetTimestamp(timestamp);
        options.AddSignedHeader("Subject");
        return options;
    }

    static EmailDkimVerificationOptions VerificationOptions(long time)
    {
        EmailDkimVerificationOptions options = new EmailDkimVerificationOptions();
        options.SetVerificationTime(time);
        options.SetAllowedClockSkewSeconds(0);
        return options;
    }

    static EmailDkimVerificationKey Binding(string domain, string selector, EmailDkimKey key)
    {
        return new EmailDkimVerificationKey(domain, selector, key);
    }

    static EmailDkimKey Imported(RSA rsa)
    {
        return new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(rsa.ExportSubjectPublicKeyInfo()).GetValue()!;
    }

    static EmailDkimKey DnsKey(RSA rsa, string tags)
    {
        return DnsRecord("v=DKIM1; p=" + Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()) + "; " + tags);
    }

    static EmailDkimKey DnsRecord(string value)
    {
        EmailDkimKeyParseResult result = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes(value), EmailStructuredValueParseMode.Strict);
        Assert.That(result.HasValue(), Is.True);
        return result.GetValue()!;
    }

    static RSA Rsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        return rsa;
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n"));
        Assert.That(result.IsComplete(), Is.True);
        return result.GetHeaders();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}

public class RecordingDkimKeyResolver : IEmailDkimKeyResolver
{
    EmailDkimVerificationKey key;
    int resolutionCount;
    string? lastDomain;
    string? lastSelector;

    public RecordingDkimKeyResolver(EmailDkimVerificationKey key)
    {
        this.key = key;
    }

    public EmailDkimVerificationKey Resolve(string domain, string selector)
    {
        resolutionCount++;
        lastDomain = domain;
        lastSelector = selector;
        if (key == null) return null!;
        if (!string.Equals(key.GetDomain(), domain, StringComparison.OrdinalIgnoreCase)) return null!;
        if (!string.Equals(key.GetSelector(), selector, StringComparison.OrdinalIgnoreCase)) return null!;
        return key;
    }

    public int GetResolutionCount() { return resolutionCount; }
    public string GetLastDomain() { return lastDomain!; }
    public string GetLastSelector() { return lastSelector!; }
}
