using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailDkimArcSecurityVectorTests
{
    [Test]
    public void VerifiesCorrectedRfc6376AppendixAVector()
    {
        string fields =
            "DKIM-Signature: v=1; a=rsa-sha256; s=brisbane; d=example.com;\r\n" +
            "      c=simple/simple; q=dns/txt; i=joe@football.example.com;\r\n" +
            "      h=Received : From : To : Subject : Date : Message-ID;\r\n" +
            "      bh=2jUSOH9NhtVGCQWNr9BrIAPreKQjO6Sn7XIkfJVOzv8=;\r\n" +
            "      b=AuUoFEfDxTDkHlLXSZEpZj79LICEps6eda7W3deTVFOk4yAUoqOB\r\n" +
            "      4nujc7YopdG5dWLSdNg6xNAZpOPr+kHxt1IrE+NahM6L/LbvaHut\r\n" +
            "      KVdkLLkpVaVVQPzeRDI009SO2Il5Lu7rDNH6mZckBdrIx0orEtZV\r\n" +
            "      4bmp/YzhwvcubU4=;\r\n" +
            "Received: from client1.football.example.com  [192.0.2.1]\r\n" +
            "      by submitserver.example.com with SUBMISSION;\r\n" +
            "      Fri, 11 Jul 2003 21:01:54 -0700 (PDT)\r\n" +
            "From: Joe SixPack <joe@football.example.com>\r\n" +
            "To: Suzie Q <suzie@shopping.example.net>\r\n" +
            "Subject: Is dinner ready?\r\n" +
            "Date: Fri, 11 Jul 2003 21:00:37 -0700 (PDT)\r\n" +
            "Message-ID: <20030712040037.46341.5F8J@football.example.com>\r\n";
        byte[] body = Bytes("Hi.\r\n\r\nWe lost the game. Are you hungry yet?\r\n\r\nJoe.\r\n");
        EmailHeaders headers = ParseHeaders(fields);
        EmailDkimKey key = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(Convert.FromBase64String(
            "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDwIRP/UC3SBsEmGqZ9ZJW3/DkM" +
            "oGeLnQg1fWn7/zYtIxN2SnFCjxOCKG9v3b4jYfcTNh5ijSsq631uBItLa7od+v/R" +
            "tdC2UzJ1lWT947qR+Rcac2gbto/NMqJ0fzfVjH4OuKhitdY9tf6mcwGjaNBcWToI" +
            "MmPSPDdQPNUYckcQ2QIDAQAB")).GetValue()!;

        EmailDkimSignatureVerificationResult result = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeys(
            headers,
            body,
            [new EmailDkimVerificationKey("example.com", "brisbane", key!)]).GetSignature(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(result.IsBodyHashValid(), Is.True);
            Assert.That(result.IsSignatureValid(), Is.True);
            Assert.That(result.GetHeaderCanonicalization()!.GetSelectionCount(), Is.EqualTo(6));
            Assert.That(result.GetHeaderCanonicalization()!.GetSelection(0).GetHeader()!.GetName(), Is.EqualTo("Received"));
        });
    }

    [Test]
    public void CollectsTheThreeRfc8617AppendixBArcSets()
    {
        EmailHeaders headers = ParseHeaders(
            "ARC-Seal: i=3; a=rsa-sha256; cv=pass; d=clochette.example.org; s=\r\n" +
            "        clochette; t=12345; b=CU87XzXlNlk5X/yW4l73UvPUcP9ivwYWxyBWcVrRs7\r\n" +
            "        +HPx3K05nJhny2fvymbReAmOA9GTH/y+k9kEc59hAKVg==\r\n" +
            "ARC-Message-Signature: i=3; a=rsa-sha256; c=relaxed/relaxed; d=\r\n" +
            "        clochette.example.org; h=message-id:date:from:to:subject; s=\r\n" +
            "        clochette; t=12345; bh=KWSe46TZKCcDbH4klJPo+tjk5LWJnVRlP5pvjXFZY\r\n" +
            "        LQ=; b=o71vwyLsK+Wm4cOSlirXoRwzEvi0vqIjd/2/GkYFYlSd/GGfKzkAgPqxf\r\n" +
            "        K7ccBMP7Zjb/mpeggswHjEMS8x5NQ==\r\n" +
            "ARC-Authentication-Results: i=3; clochette.example.org; spf=fail\r\n" +
            "    smtp.from=jqd@d1.example; dkim=fail (512-bit key)\r\n" +
            "    header.i=@d1.example; dmarc=fail; arc=pass\r\n" +
            "ARC-Seal: i=2; a=rsa-sha256; cv=pass; d=gmail.example; s=20120806; t=\r\n" +
            "        12345; b=Zpukh/kJL4Q7Kv391FKwTepgS56dgHIcdhhJZjsalhqkFIQQAJ4T9BE\r\n" +
            "        8jjLXWpRNuh81yqnT1/jHn086RwezGw==\r\n" +
            "ARC-Message-Signature: i=2; a=rsa-sha256; c=relaxed/relaxed; d=\r\n" +
            "        gmail.example; h=message-id:date:from:to:subject; s=20120806; t=\r\n" +
            "        12345; bh=KWSe46TZKCcDbH4klJPo+tjk5LWJnVRlP5pvjXFZYLQ=; b=CVoG44\r\n" +
            "        cVZvoSs2mMig2wwqPaJ4OZS5XGMCegWqQs1wvRZJS894tJM0xO1RJLgCPsBOxdA5\r\n" +
            "        9WSqI9s9DfyKDfWg==\r\n" +
            "ARC-Authentication-Results: i=2; gmail.example; spf=fail\r\n" +
            "    smtp.from=jqd@d1.example; dkim=fail (512-bit key)\r\n" +
            "    header.i=@example.org; dmarc=fail; arc=pass\r\n" +
            "ARC-Seal: i=1; a=rsa-sha256; cv=none; d=lists.example.org; s=dk-lists;\r\n" +
            "         t=12345; b=TlCCKzgk3TrAa+G77gYYO8Fxk4q/Ml0biqduZJeOYh6+0zhwQ8u/\r\n" +
            "        lHxLi21pxu347isLSuNtvIagIvAQna9a5A==\r\n" +
            "ARC-Message-Signature: i=1; a=rsa-sha256; c=relaxed/relaxed; d=\r\n" +
            "        lists.example.org; h=message-id:date:from:to:subject; s=\r\n" +
            "        dk-lists; t=12345; bh=KWSe46TZKCcDbH4klJPo+tjk5LWJnVRlP5pvjXFZYL\r\n" +
            "        Q=; b=DsoD3n3hiwlrN1ma8IZQFgZx8EDO7Wah3hUjIEsYKuShRKYB4LwGUiKD5Y\r\n" +
            "        yHgcIwGHhSc/4+ewYqHMWDnuFxiQ==\r\n" +
            "ARC-Authentication-Results: i=1; lists.example.org; spf=pass\r\n" +
            "    smtp.mfrom=jqd@d1.example; dkim=pass (512-bit key)\r\n" +
            "    header.i=@d1.example; dmarc=pass\r\n");

        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(headers);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsStructurallyValid(), Is.True);
            Assert.That(result.GetSetCount(), Is.EqualTo(3));
            Assert.That(result.GetFieldCount(), Is.EqualTo(9));
            Assert.That(result.GetMaximumInstance(), Is.EqualTo(3));
            Assert.That(result.GetSetByInstance(1)!.GetDeclaredChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
            Assert.That(result.GetSetByInstance(2)!.GetDeclaredChainStatus(), Is.EqualTo(EmailArcChainStatus.Pass));
            Assert.That(result.GetSetByInstance(3)!.GetDeclaredChainStatus(), Is.EqualTo(EmailArcChainStatus.Pass));
        });
    }

    [Test]
    public void OversigningFromRejectsARepeatedHeaderInjection()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: legitimate@example.test\r\nSubject: protected\r\n");
        EmailDkimSigningOptions options = SigningOptions();
        options.AddSignedHeader("From");
        options.AddSignedHeader("Subject");
        new EmailDkimSigner().SignAndInsert(headers, body, rsa, options);
        EmailDkimVerificationKey key = Binding(rsa);

        EmailDkimSignatureVerificationResult before = Verify(headers, body, key);
        headers.Insert(1, Header("From", " attacker@example.test"));
        EmailDkimSignatureVerificationResult after = Verify(headers, body, key);

        Assert.Multiple(() =>
        {
            Assert.That(before.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(before.GetHeaderCanonicalization()!.GetMissingHeaderCount(), Is.EqualTo(1));
            Assert.That(after.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureMismatch));
            Assert.That(after.IsBodyHashValid(), Is.True);
            Assert.That(after.GetHeaderCanonicalization()!.GetMissingHeaderCount(), Is.Zero);
        });
    }

    [Test]
    public void DistinguishesRelaxedAndSimpleLineEndingTransformations()
    {
        using RSA rsa = Rsa();
        byte[] crlfBody = Bytes("transport body\r\n");
        EmailDkimVerificationKey key = Binding(rsa);
        EmailHeaders relaxedHeaders = ParseHeaders("From: sender@example.test\r\nSubject: transport value\r\n");
        EmailDkimSigningOptions relaxedOptions = SigningOptions();
        relaxedOptions.AddSignedHeader("Subject");
        new EmailDkimSigner().SignAndInsert(relaxedHeaders, crlfBody, rsa, relaxedOptions);
        relaxedHeaders.GetFirst("From")!.SetLineEnding(EmailLineEnding.Lf);
        relaxedHeaders.GetFirst("Subject")!.SetLineEnding(EmailLineEnding.Lf);

        EmailHeaders simpleHeaders = ParseHeaders("From: sender@example.test\r\nSubject: transport value\r\n");
        EmailDkimSigningOptions simpleOptions = SigningOptions();
        simpleOptions.AddSignedHeader("Subject");
        simpleOptions.SetHeaderCanonicalizationKind(EmailDkimCanonicalizationKind.Simple);
        simpleOptions.SetBodyCanonicalizationKind(EmailDkimCanonicalizationKind.Simple);
        new EmailDkimSigner().SignAndInsert(simpleHeaders, crlfBody, rsa, simpleOptions);
        simpleHeaders.GetFirst("From")!.SetLineEnding(EmailLineEnding.Lf);
        simpleHeaders.GetFirst("Subject")!.SetLineEnding(EmailLineEnding.Lf);

        EmailDkimSignatureVerificationResult relaxedHeadersChanged = Verify(relaxedHeaders, crlfBody, key);
        EmailDkimSignatureVerificationResult simpleHeadersChanged = Verify(simpleHeaders, crlfBody, key);
        EmailDkimSignatureVerificationResult relaxedBodyChanged = Verify(relaxedHeaders, Bytes("transport body\n"), key);

        Assert.Multiple(() =>
        {
            Assert.That(relaxedHeadersChanged.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(relaxedHeadersChanged.IsBodyHashValid(), Is.True);
            Assert.That(simpleHeadersChanged.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureMismatch));
            Assert.That(simpleHeadersChanged.IsBodyHashValid(), Is.True);
            Assert.That(relaxedBodyChanged.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.BodyHashMismatch));
            Assert.That(relaxedBodyChanged.IsBodyHashValid(), Is.False);
        });
    }

    [Test]
    public void ReportsExpiredRevokedAndTestingKeyOutcomes()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: key policy\r\n");
        EmailDkimSigningOptions signing = SigningOptions();
        signing.AddSignedHeader("Subject");
        signing.SetExpiration(1700000100);
        new EmailDkimSigner().SignAndInsert(headers, body, rsa, signing);

        EmailDkimSignatureVerificationResult expired = Verify(headers, body, Binding(rsa), VerificationOptions(1700000200));
        EmailDkimSignatureVerificationResult revoked = Verify(headers, body, new EmailDkimVerificationKey("example.test", "key", DnsRecord("v=DKIM1; p=")), VerificationOptions(1700000050));
        EmailDkimVerificationKey testingKey = new EmailDkimVerificationKey("example.test", "key", DnsKey(rsa, "t=y"));
        EmailDkimSignatureVerificationResult testing = Verify(headers, body, testingKey, VerificationOptions(1700000050));
        EmailDkimVerificationOptions rejectOptions = VerificationOptions(1700000050);
        rejectOptions.SetRejectTestingKeys(true);
        EmailDkimSignatureVerificationResult rejected = Verify(headers, body, testingKey, rejectOptions);

        Assert.Multiple(() =>
        {
            Assert.That(expired.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureExpired));
            Assert.That(revoked.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyRevoked));
            Assert.That(testing.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(testing.GetPolicyStatus(), Is.EqualTo(EmailDkimVerificationPolicyStatus.AcceptedWithWarnings));
            Assert.That(testing.IsTestingKey(), Is.True);
            Assert.That(rejected.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.PolicyRejected));
        });
    }

    [Test]
    public void DetectsUnsignedSuffixAbuseFromTheBodyLengthTag()
    {
        using RSA rsa = Rsa();
        byte[] signedBody = Bytes("safe\r\n");
        byte[] attackedBody = Bytes("safe\r\nMALICIOUS CONTENT\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: body length\r\n");
        EmailDkimSigningOptions signing = SigningOptions();
        signing.AddSignedHeader("Subject");
        signing.SetBodyLengthLimit(6);
        new EmailDkimSigner().SignAndInsert(headers, signedBody, rsa, signing);
        EmailDkimVerificationKey key = Binding(rsa);

        EmailDkimSignatureVerificationResult warned = Verify(headers, attackedBody, key);
        EmailDkimVerificationOptions rejectOptions = VerificationOptions(1700000050);
        rejectOptions.SetRejectUnsignedBodyContent(true);
        EmailDkimSignatureVerificationResult rejected = Verify(headers, attackedBody, key, rejectOptions);

        Assert.Multiple(() =>
        {
            Assert.That(warned.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(warned.GetPolicyStatus(), Is.EqualTo(EmailDkimVerificationPolicyStatus.AcceptedWithWarnings));
            Assert.That(warned.HasUnsignedBodyContent(), Is.True);
            Assert.That(warned.GetBodyCanonicalization()!.GetUnsignedCanonicalizedByteCount(), Is.GreaterThan(0));
            Assert.That(rejected.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.PolicyRejected));
            Assert.That(rejected.IsBodyHashValid(), Is.True);
            Assert.That(rejected.IsSignatureValid(), Is.True);
        });
    }

    [Test]
    public void InvalidatesSignedHeadersAndBodyButAllowsUnsignedHeaders()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("original body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: original subject\r\n");
        EmailDkimSigningOptions signing = SigningOptions();
        signing.AddSignedHeader("Subject");
        new EmailDkimSigner().SignAndInsert(headers, body, rsa, signing);
        EmailDkimVerificationKey key = Binding(rsa);

        headers.Insert(1, Header("X-Transport-Note", " added after signing"));
        EmailDkimSignatureVerificationResult unsignedHeader = Verify(headers, body, key);
        EmailDkimSignatureVerificationResult changedBody = Verify(headers, Bytes("changed body\r\n"), key);
        headers.GetFirst("Subject")!.SetValue("changed subject");
        EmailDkimSignatureVerificationResult changedHeader = Verify(headers, body, key);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedHeader.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(changedBody.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.BodyHashMismatch));
            Assert.That(changedBody.IsBodyHashValid(), Is.False);
            Assert.That(changedHeader.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.SignatureMismatch));
            Assert.That(changedHeader.IsBodyHashValid(), Is.True);
            Assert.That(changedHeader.IsSignatureValid(), Is.False);
        });
    }

    static EmailDkimSigningOptions SigningOptions()
    {
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();
        options.SetDomain("example.test");
        options.SetSelector("key");
        options.SetTimestamp(1700000000);
        return options;
    }

    static EmailDkimSignatureVerificationResult Verify(EmailHeaders headers, byte[] body, EmailDkimVerificationKey key)
    {
        return Verify(headers, body, key, VerificationOptions(1700000050));
    }

    static EmailDkimSignatureVerificationResult Verify(EmailHeaders headers, byte[] body, EmailDkimVerificationKey key, EmailDkimVerificationOptions options)
    {
        return new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [key], options).GetSignature(0);
    }

    static EmailDkimVerificationOptions VerificationOptions(long time)
    {
        EmailDkimVerificationOptions options = new EmailDkimVerificationOptions();
        options.SetVerificationTime(time);
        options.SetAllowedClockSkewSeconds(0);
        return options;
    }

    static EmailDkimVerificationKey Binding(RSA rsa)
    {
        EmailDkimKey key = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(rsa.ExportSubjectPublicKeyInfo()).GetValue()!;
        return new EmailDkimVerificationKey("example.test", "key", key!);
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

    static EmailHeader Header(string name, string value)
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValue(name, Bytes(value));
        header.SetLineEnding(EmailLineEnding.CrLf);
        return header;
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
