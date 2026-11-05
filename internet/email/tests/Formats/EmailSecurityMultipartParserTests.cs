using System.Text;
using email;

namespace email.Tests;

public class EmailSecurityMultipartParserTests
{
    [Test]
    public void RecognizesMultipartSignedAndExtractsExactNestedEntityOctets()
    {
        const string signedOctets =
            "Content-Type: multipart/mixed; boundary=inner\r\n" +
            "X-Exact:\t one\r\n" +
            "\r\n" +
            "--inner\n" +
            "Content-Type: text/plain\n" +
            "\n" +
            "signed payload\n" +
            "--inner--";
        MimeEntity root = Parse(
            "Content-Type: multipart/signed; boundary=outer; protocol=\"application/pgp-signature\"; micalg=\"pgp-sha256, pgp-sha512\"\r\n\r\n" +
            "--outer\r\n" + signedOctets + "\r\n" +
            "--outer\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n" +
            "--outer--\r\n");

        EmailSecurityMultipartParseResult result = new EmailSecurityMultipartParser().ParseWithRoot(root);
        EmailSecurityMultipart security = result.GetSecurityMultipart()!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.HasSecurityMultipart(), Is.True);
            Assert.That(security!.IsSigned(), Is.True);
            Assert.That(security.IsEncrypted(), Is.False);
            Assert.That(security.GetRoot(), Is.SameAs(root));
            Assert.That(security.HasProtocol(), Is.True);
            Assert.That(security.GetProtocol(), Is.EqualTo("application/pgp-signature"));
            Assert.That(security.GetMicAlgorithms(), Is.EqualTo(new[] { "pgp-sha256", "pgp-sha512" }));
            Assert.That(security.HasContentPart(), Is.True);
            Assert.That(security.GetContentPart(), Is.SameAs(root.GetChildAt(0)));
            Assert.That(security.GetSecurityPart(), Is.SameAs(root.GetChildAt(1)));
            Assert.That(security.GetSignedContentPart(), Is.SameAs(root.GetChildAt(0)));
            Assert.That(security.GetSignaturePart(), Is.SameAs(root.GetChildAt(1)));
            Assert.That(security.HasExactSignedOctets(), Is.True);
            Assert.That(security.GetExactSignedOctets(), Is.Not.Null);
            Assert.That(Text(security.MaterializeExactSignedOctets(1000)), Is.EqualTo(signedOctets));
            Assert.That(security.GetSignedContentPart()!.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        });

        List<string> returnedAlgorithms = security!.GetMicAlgorithms();
        returnedAlgorithms.Clear();
        Assert.That(security.GetMicAlgorithmCount(), Is.EqualTo(2));
        Assert.Throws<EmailLimitException>(() => security.MaterializeExactSignedOctets(5));
    }

    [Test]
    public void RecognizesMultipartEncryptedAndAssignsControlAndPayloadRoles()
    {
        MimeEntity root = Parse(
            "Content-Type: multipart/encrypted; boundary=e; protocol=\"application/pgp-encrypted\"\r\n\r\n" +
            "--e\r\nContent-Type: application/pgp-encrypted\r\n\r\nVersion: 1\r\n" +
            "--e\r\nContent-Type: application/octet-stream\r\n\r\nciphertext\r\n" +
            "--e--\r\n");

        EmailSecurityMultipartParseResult result = new EmailSecurityMultipartParser().ParseWithRoot(root);
        EmailSecurityMultipart security = result.GetSecurityMultipart()!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(security!.IsEncrypted(), Is.True);
            Assert.That(security.GetProtocol(), Is.EqualTo("application/pgp-encrypted"));
            Assert.That(security.GetEncryptionControlPart(), Is.SameAs(root.GetChildAt(0)));
            Assert.That(security.GetEncryptedContentPart(), Is.SameAs(root.GetChildAt(1)));
            Assert.That(security.GetMicAlgorithmCount(), Is.Zero);
            Assert.That(security.HasExactSignedOctets(), Is.False);
        });
    }

    [Test]
    public void ReportsMissingMalformedAndMismatchedSecurityParameters()
    {
        MimeEntity missing = Parse(
            "Content-Type: multipart/signed; boundary=s\r\n\r\n" +
            "--s\r\nContent-Type: text/plain\r\n\r\ncontent\r\n" +
            "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");
        EmailSecurityMultipartParseResult missingResult = new EmailSecurityMultipartParser().ParseWithRoot(missing);

        MimeEntity malformed = Parse(
            "Content-Type: multipart/signed; boundary=s; protocol=not-a-media-type; micalg=\"sha256, bad/value\"\r\n\r\n" +
            "--s\r\nContent-Type: text/plain\r\n\r\ncontent\r\n" +
            "--s\r\nContent-Type: application/pkcs7-signature\r\n\r\nsignature\r\n--s--\r\n");
        EmailSecurityMultipartParseResult malformedResult = new EmailSecurityMultipartParser().ParseWithRoot(malformed);

        MimeEntity mismatch = Parse(
            "Content-Type: multipart/encrypted; boundary=e; protocol=\"application/pgp-encrypted\"\r\n\r\n" +
            "--e\r\nContent-Type: application/octet-stream\r\n\r\ncontrol\r\n" +
            "--e\r\nContent-Type: application/octet-stream\r\n\r\npayload\r\n--e--\r\n");
        EmailSecurityMultipartParseResult mismatchResult = new EmailSecurityMultipartParser().ParseWithRoot(mismatch);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.IsSuccess(), Is.False);
            Assert.That(missingResult.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartProtocolMissing), Is.True);
            Assert.That(missingResult.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartMicAlgorithmMissing), Is.True);
            Assert.That(malformedResult.IsSuccess(), Is.False);
            Assert.That(malformedResult.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartProtocolMalformed), Is.True);
            Assert.That(malformedResult.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartMicAlgorithmMalformed), Is.True);
            Assert.That(mismatchResult.IsSuccess(), Is.False);
            Assert.That(mismatchResult.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartProtocolMismatch), Is.True);
        });
    }

    [Test]
    public void RejectsRecoveredWrongArityAndOrdinaryMultipartEntities()
    {
        MimeEntity onePart = Parse(
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pgp-signature\"; micalg=pgp-sha256\r\n\r\n" +
            "--s\r\nContent-Type: text/plain\r\n\r\ncontent\r\n--s--\r\n");
        EmailSecurityMultipartParseResult arity = new EmailSecurityMultipartParser().ParseWithRoot(onePart);
        EmailSecurityMultipartParseResult ordinary = new EmailSecurityMultipartParser().ParseWithRoot(Parse(
            "Content-Type: multipart/mixed; boundary=m\r\n\r\n--m--\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(arity.IsRecognized(), Is.True);
            Assert.That(arity.IsSuccess(), Is.False);
            Assert.That(arity.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartStructureMalformed), Is.True);
            Assert.That(ordinary.IsRecognized(), Is.False);
            Assert.That(ordinary.HasSecurityMultipart(), Is.False);
            Assert.That(ordinary.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartStructureMalformed), Is.True);
        });
    }

    [Test]
    public void InvalidatesExactSignedOctetAccessAfterTreeMutation()
    {
        MimeEntity root = Parse(
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pgp-signature\"; micalg=pgp-sha256\r\n\r\n" +
            "--s\r\nContent-Type: text/plain\r\nX-Value: original\r\n\r\ncontent\r\n" +
            "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");
        EmailSecurityMultipartParseResult before = new EmailSecurityMultipartParser().ParseWithRoot(root);
        root.GetChildAt(0).GetHeaders()!.GetFirst("X-Value")!.SetValue("changed");

        EmailSecurityMultipartParseResult after = new EmailSecurityMultipartParser().ParseWithRoot(root);

        Assert.Multiple(() =>
        {
            Assert.That(before.IsSuccess(), Is.True);
            Assert.That(before.GetSecurityMultipart()!.HasExactSignedOctets(), Is.True);
            Assert.That(after.IsSuccess(), Is.False);
            Assert.That(after.GetSecurityMultipart()!.HasExactSignedOctets(), Is.False);
            Assert.That(after.GetIssues().ContainsCode(EmailIssueCode.SecurityMultipartSignedContentUnavailable), Is.True);
            Assert.Throws<InvalidOperationException>(() => after.GetSecurityMultipart()!.MaterializeExactSignedOctets(100));
        });
    }

    [Test]
    public void CanonicalWritingCopiesAnUntouchedNestedSignedPartExactly()
    {
        const string signedOctets =
            "Content-Type: multipart/alternative; boundary=i\r\n" +
            "X-Weird :\tvalue\r\n\r\n" +
            "--i\r\nContent-Type: text/plain\r\n\r\nbody\r\n--i--";
        MimeEntity root = Parse(
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pgp-signature\"; micalg=pgp-sha256\r\n\r\n" +
            "--s\r\n" + signedOctets + "\r\n" +
            "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes("nested signed part"));
        string boundary = new EmailBoundaryGenerator().CreateCandidate(options, 0);

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(root, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Does.Contain("--" + boundary + "\r\n" + signedOctets + "\r\n--" + boundary + "\r\n"));
    }

    [Test]
    public void ParsedOriginalEntityContentSurvivesCloneAndDetach()
    {
        const string source = "Content-Type: text/plain\r\nX-Raw:\tvalue\r\n\r\nbody";
        MimeEntity parsed = Parse(source);
        MimeEntity clone = parsed.Clone();

        clone.DetachSource(1000);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.HasOriginalEntityContent(), Is.True);
            Assert.That(parsed.HasCurrentOriginalEntityContent(), Is.True);
            Assert.That(Text(parsed.GetOriginalEntityContent()!.Materialize(1000)), Is.EqualTo(source));
            Assert.That(clone.HasOriginalEntityContent(), Is.True);
            Assert.That(clone.HasCurrentOriginalEntityContent(), Is.True);
            Assert.That(Text(clone.GetOriginalEntityContent()!.Materialize(1000)), Is.EqualTo(source));
            Assert.That(clone.GetOriginalEntityContent()!.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        });
    }

    static MimeEntity Parse(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasEntity(), Is.True);
        return result.GetEntity()!;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
