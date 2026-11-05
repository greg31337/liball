using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using email;

namespace email.Tests;

[NonParallelizable]
public class EmailProtectedHeadersTests
{
    [Test]
    public void ConstructsClearPayloadWithoutMutatingTheMessage()
    {
        EmailMessage message = Message();
        message.GetHeaders()!.Add(Header("MIME-Version", " 1.0"));
        message.GetBody()!.GetHeaders()!.Add(Header("X-Part-Metadata", " local"));

        EmailProtectedHeaderConstructionResult result = new EmailProtectedHeaders().Construct(message, EmailProtectedHeaderIntent.Clear);
        MimeEntity payload = result.GetPayload();
        EmailHeaders outer = result.GetOuterHeaders();
        string wire = Encoding.ASCII.GetString(new EmailWriter().ToArrayWithEntity(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetIssues().GetCount(), Is.Zero);
            Assert.That(Text(payload.GetMediaType()!.GetParameters().GetFirst("hp")!.GetValueBytes()), Is.EqualTo("clear"));
            Assert.That(payload.GetHeaders()!.GetFirst("From")!.GetDecodedValue(), Is.EqualTo("Alice <alice@example.test>"));
            Assert.That(payload.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
            Assert.That(payload.GetHeaders()!.Contains("HP-Outer"), Is.False);
            Assert.That(payload.GetHeaders()!.Contains("X-Part-Metadata"), Is.False);
            Assert.That(outer.GetCountNamed("From"), Is.EqualTo(1));
            Assert.That(outer.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
            Assert.That(outer.Contains("MIME-Version"), Is.False);
            Assert.That(message.GetBody()!.GetHeaders()!.Contains("X-Part-Metadata"), Is.True);
            Assert.That(message.GetBody()!.GetMediaType()!.GetParameters().GetFirst("hp"), Is.Null);
            Assert.That(wire, Does.Contain("hp=clear"));
        });
    }

    [Test]
    public void AppliesCipherBaselineAndClassifiesProtectedOccurrences()
    {
        EmailMessage message = Message();
        message.GetHeaders()!.Add(Header("Comments", " private comment"));
        message.GetHeaders()!.Add(Header("Keywords", " alpha, beta"));
        EmailProtectedHeaderConstructionResult built = new EmailProtectedHeaders().Construct(message, EmailProtectedHeaderIntent.Cipher);
        MimeEntity payload = built.GetPayload();
        EmailHeaders outer = built.GetOuterHeaders();

        EmailProtectedHeaderExtractionResult extracted = new EmailProtectedHeaders().Extract(
            outer,
            payload,
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.OpenPgpMime, true, true, true));

        Assert.Multiple(() =>
        {
            Assert.That(built.IsSuccess(), Is.True);
            Assert.That(Text(payload.GetMediaType()!.GetParameters().GetFirst("hp")!.GetValueBytes()), Is.EqualTo("cipher"));
            Assert.That(outer.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("[...]"));
            Assert.That(outer.Contains("Comments"), Is.False);
            Assert.That(outer.Contains("Keywords"), Is.False);
            Assert.That(payload.GetHeaders()!.GetCountNamed("HP-Outer"), Is.EqualTo(3));
            Assert.That(payload.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
            Assert.That(extracted.IsSuccess(), Is.True);
            Assert.That(extracted.GetProtocol(), Is.EqualTo(EmailProtectedHeaderProtocol.OpenPgpMime));
            Assert.That(extracted.GetIntent(), Is.EqualTo(EmailProtectedHeaderIntent.Cipher));
            Assert.That(extracted.GetEntryCount(), Is.EqualTo(extracted.GetEntries().Count));
            Assert.That(extracted.GetMismatchCount(), Is.Zero);
            Assert.That(State(extracted, "From"), Is.EqualTo(EmailProtectedHeaderState.SignedOnly));
            Assert.That(State(extracted, "To"), Is.EqualTo(EmailProtectedHeaderState.SignedOnly));
            Assert.That(State(extracted, "Subject"), Is.EqualTo(EmailProtectedHeaderState.SignedAndEncrypted));
            Assert.That(State(extracted, "Comments"), Is.EqualTo(EmailProtectedHeaderState.SignedAndEncrypted));
            Assert.That(State(extracted, "Keywords"), Is.EqualTo(EmailProtectedHeaderState.SignedAndEncrypted));
            Assert.That(extracted.GetReplyHeaders().GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
        });
    }

    [Test]
    public void ReportsOuterAndFromMismatchesAndBuildsSafeViews()
    {
        EmailProtectedHeaderConstructionResult built = new EmailProtectedHeaders().Construct(Message(), EmailProtectedHeaderIntent.Cipher);
        EmailHeaders forgedOuter = built.GetOuterHeaders();
        forgedOuter.ReplaceAll("From", Header("From", " Mallory <mallory@example.test>"));
        forgedOuter.ReplaceAll("Subject", Header("Subject", " forged subject"));

        EmailProtectedHeaderExtractionResult unsigned = new EmailProtectedHeaders().Extract(
            forgedOuter,
            built.GetPayload(),
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.Smime, true, false, false));
        EmailProtectedHeaderExtractionResult bound = new EmailProtectedHeaders().Extract(
            forgedOuter,
            built.GetPayload(),
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.Smime, true, true, true));
        EmailProtectedHeaderMismatch mismatch = unsigned.GetMismatches().First(value => value.GetKind() == EmailProtectedHeaderMismatchKind.OuterValueMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.IsSuccess(), Is.True);
            Assert.That(unsigned.GetMismatches().Select(value => value.GetKind()), Does.Contain(EmailProtectedHeaderMismatchKind.FromAddressMismatch));
            Assert.That(unsigned.GetMismatches().Count(value => value.GetKind() == EmailProtectedHeaderMismatchKind.OuterValueMismatch), Is.EqualTo(2));
            Assert.That(unsigned.GetMismatchAt(0), Is.Not.Null);
            Assert.That(mismatch.GetFieldName(), Is.Not.Empty);
            Assert.That(mismatch.HasExpectedValue(), Is.True);
            Assert.That(mismatch.GetExpectedValue(), Is.Not.Empty);
            Assert.That(mismatch.HasActualValue(), Is.True);
            Assert.That(mismatch.GetActualValue(), Is.Not.Empty);
            Assert.That(unsigned.GetIssues().ContainsCode(EmailIssueCode.ProtectedHeaderOuterMismatch), Is.True);
            Assert.That(unsigned.GetIssues().ContainsCode(EmailIssueCode.ProtectedHeaderFromMismatch), Is.True);
            Assert.That(unsigned.GetDisplayHeaders().GetFirst("From")!.GetDecodedValue(), Does.Contain("Mallory"));
            Assert.That(unsigned.GetReplyHeaders().GetFirst("From")!.GetDecodedValue(), Does.Contain("Alice"));
            Assert.That(State(unsigned, "From"), Is.EqualTo(EmailProtectedHeaderState.Unprotected));
            Assert.That(State(unsigned, "Subject"), Is.EqualTo(EmailProtectedHeaderState.EncryptedOnly));
            Assert.That(bound.GetDisplayHeaders().GetFirst("From")!.GetDecodedValue(), Does.Contain("Alice"));
            Assert.That(bound.GetMismatchCount(), Is.EqualTo(unsigned.GetMismatchCount()));
        });
    }

    [Test]
    public void RejectsMalformedIntentRecordsAndFalseCipherClaims()
    {
        EmailProtectedHeaderConstructionResult built = new EmailProtectedHeaders().Construct(Message(), EmailProtectedHeaderIntent.Cipher);
        MimeEntity duplicateIntent = built.GetPayload();
        duplicateIntent.SetMediaType(new EmailMediaTypeParser().ParseWithInputAndMode(Bytes("text/plain; hp=cipher; hp=clear"), EmailStructuredValueParseMode.Strict).GetValue());
        EmailProtectedHeaderExtractionResult duplicate = new EmailProtectedHeaders().Extract(
            built.GetOuterHeaders(), duplicateIntent,
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.Smime, true, true, true));

        MimeEntity malformedRecord = built.GetPayload();
        malformedRecord.GetHeaders()!.Add(Header("HP-Outer", " Content-Type: text/html"));
        EmailProtectedHeaderExtractionResult malformed = new EmailProtectedHeaders().Extract(
            built.GetOuterHeaders(), malformedRecord,
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.OpenPgpMime, true, false, false));
        EmailProtectedHeaderExtractionResult cleartextCipher = new EmailProtectedHeaders().Extract(
            built.GetOuterHeaders(), built.GetPayload(),
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.OpenPgpMime, false, false, false));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsRecognized(), Is.False);
            Assert.That(duplicate.IsSuccess(), Is.False);
            Assert.That(duplicate.GetIssues().ContainsCode(EmailIssueCode.ProtectedHeaderMalformed), Is.True);
            Assert.That(malformed.IsSuccess(), Is.False);
            Assert.That(malformed.GetRecordedOuterHeaders().GetCount(), Is.Zero);
            Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.ProtectedHeaderMalformed), Is.True);
            Assert.That(cleartextCipher.IsSuccess(), Is.False);
            Assert.That(cleartextCipher.GetIssues().ContainsCode(EmailIssueCode.ProtectedHeaderIntentMismatch), Is.True);
        });
    }

    [Test]
    public void RoundTripsTheSameProtectedPayloadThroughSmimeAndPgpMime()
    {
        EmailProtectedHeaderConstructionResult built = new EmailProtectedHeaders().Construct(Message(), EmailProtectedHeaderIntent.Clear);
        MimeEntity payload = built.GetPayload();
        byte[] canonical = new EmailWriter().ToArrayWithEntity(payload);

        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest("CN=Protected Header Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509SignatureGenerator certificateSigner = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.Create(request.SubjectName, certificateSigner, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), new byte[] { 1 });
        EmailSmimeSigningResult smimeSigned = new EmailSmimeSigner().SignOpaqueWithContentAndSigners(canonical,
        [
            new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(certificate, rsa, EmailSmimeDigestAlgorithm.Sha256)
        ]);
        EmailSmimeVerificationOptions smimeOptions = new EmailSmimeVerificationOptions();
        smimeOptions.SetVerifyCertificateChain(false);
        EmailSmimeVerificationResult smimeVerified = new EmailSmimeVerifier().VerifyOpaqueWithCmsAndOptions(smimeSigned.GetCmsBytes(), smimeOptions);
        EmailProtectedHeaderExtractionResult smime = new EmailProtectedHeaders().Extract(
            built.GetOuterHeaders(), ParseEntity(smimeVerified.GetContent()!),
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.Smime, false, smimeVerified.IsSuccess(), true));

        byte[] keyId = Convert.FromHexString("0123456789ABCDEF");
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        EmailOpenPgpMimeOptions pgpOptions = new EmailOpenPgpMimeOptions();
        pgpOptions.SetSignatureCreationTime(1700000000);
        EmailOpenPgpSigningResult pgpSigned = new EmailOpenPgpMimeSigner().SignWithContentAndCredentialAndOptions(payload, new EmailOpenPgpSigningCredential(keyId, 19, 8, ecdsa), pgpOptions);
        EmailOpenPgpVerificationResult pgpVerified = new EmailOpenPgpMimeVerifier().VerifyWithRootAndKeysAndOptions(
            ParseEntity(pgpSigned.GetMimeEntityBytes()), [new EmailOpenPgpVerificationKey(keyId, 19, ecdsa)], pgpOptions);
        EmailProtectedHeaderExtractionResult pgp = new EmailProtectedHeaders().Extract(
            built.GetOuterHeaders(), ParseEntity(pgpVerified.GetContent()),
            new EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol.OpenPgpMime, false, pgpVerified.IsSuccess(), true));

        Assert.Multiple(() =>
        {
            Assert.That(smimeSigned.IsSuccess(), Is.True);
            Assert.That(smimeVerified.IsSuccess(), Is.True);
            Assert.That(smime.IsSuccess(), Is.True);
            Assert.That(smime.GetProtectedHeaders().GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
            Assert.That(State(smime, "Subject"), Is.EqualTo(EmailProtectedHeaderState.SignedOnly));
            Assert.That(pgpSigned.IsSuccess(), Is.True);
            Assert.That(pgpVerified.IsSuccess(), Is.True);
            Assert.That(pgp.IsSuccess(), Is.True);
            Assert.That(pgp.GetProtectedHeaders().GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("private subject"));
            Assert.That(State(pgp, "Subject"), Is.EqualTo(EmailProtectedHeaderState.SignedOnly));
        });
    }

    static EmailMessage Message()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(Header("From", " Alice <alice@example.test>"));
        headers.Add(Header("To", " Bob <bob@example.test>"));
        headers.Add(Header("Subject", " private subject"));
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, new EmailEntityFactory().CreateTextWithValue("protected body"), new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
    }

    static EmailHeader Header(string name, string value)
    {
        return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes(value), new EmailHeaderRegistry().GetValueKind(name));
    }

    static EmailProtectedHeaderState State(EmailProtectedHeaderExtractionResult result, string name)
    {
        return result.GetEntries().Single(value => string.Equals(value.GetHeader().GetName(), name, StringComparison.OrdinalIgnoreCase)).GetState();
    }

    static MimeEntity ParseEntity(byte[] bytes)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(bytes);
        Assert.That(result.IsComplete(), Is.True);
        return result.GetEntity()!;
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }
    static string Text(byte[] value) { return Encoding.ASCII.GetString(value); }
}
