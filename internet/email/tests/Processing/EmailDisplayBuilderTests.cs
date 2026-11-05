using System.Text;
using email;

namespace email.Tests;

public class EmailDisplayBuilderTests
{
    [Test]
    public void BuildsSanitizedHtmlTextAndRelatedAttachmentProjection()
    {
        MimeEntity root = ParseDisplayTree();

        EmailDisplayModel model = new EmailDisplayBuilder().BuildWithRoot(root);

        Assert.That(model.HasSelectedBody(), Is.True);
        Assert.That(model.GetBodySelection().GetBody()!.GetMediaType()!.IsType("text", "html"), Is.True);
        Assert.That(model.GetText(), Is.EqualTo("Hello world"));
        Assert.That(model.GetSnippet(), Is.EqualTo("Hello world"));
        Assert.That(model.GetHtml(), Is.EqualTo("<p>Hello <strong>world</strong></p><img><img src=\"cid:logo@example.test\"><img src=\"cid:missing@example.test\">"));
        Assert.That(model.GetAlternativeCount(), Is.EqualTo(2));
        Assert.That(model.GetInlineResourceCount(), Is.EqualTo(1));
        Assert.That(model.GetAttachmentCount(), Is.EqualTo(1));
        Assert.That(model.GetMissingRelatedReferences().Count, Is.EqualTo(1));
        Assert.That(model.GetWarnings().ContainsCode(EmailIssueCode.HtmlRemoteContentBlocked), Is.True);
        Assert.That(model.GetWarnings().ContainsCode(EmailIssueCode.DisplayRelatedResourceMissing), Is.True);
        Assert.That(model.HasSourceTextDecodeResult(), Is.True);
        Assert.That(model.HasHtmlSanitizeResult(), Is.True);
        Assert.That(model.GetHtmlSanitizeResult()!.WasModified(), Is.True);
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void PlainTextPreferenceSuppressesHtmlProjection()
    {
        EmailDisplayOptions options = new EmailDisplayOptions();
        options.SetBodyPreference(EmailBodyPreference.PlainText);

        EmailDisplayModel model = new EmailDisplayBuilder().BuildWithRootAndOptions(ParseDisplayTree(), options);

        Assert.That(model.GetBodySelection().GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(model.GetText(), Is.EqualTo("Plain fallback"));
        Assert.That(model.HasHtml(), Is.False);
        Assert.That(model.HasHtmlSanitizeResult(), Is.False);
    }

    [Test]
    public void SecuritySummaryReportsStructureWithoutClaimingVerification()
    {
        MimeEntity signed = ParseEntity(
            "Content-Type: multipart/signed; boundary=s; protocol=application/pgp-signature\r\n\r\n"
            + "--s\r\nContent-Type: text/plain\r\n\r\nsigned body\r\n"
            + "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");

        EmailDisplaySecuritySummary summary = new EmailDisplayBuilder().BuildWithRoot(signed).GetSecuritySummary();

        Assert.That(summary.GetSignedContentCount(), Is.EqualTo(1));
        Assert.That(summary.GetSignaturePartCount(), Is.EqualTo(1));
        Assert.That(summary.HasSignedContent(), Is.True);
        Assert.That(summary.ClaimsVerification(), Is.False);
        Assert.That(summary.HasEncryptedContent(), Is.False);

        MimeEntity encrypted = ParseEntity(
            "Content-Type: multipart/encrypted; boundary=e; protocol=application/pgp-encrypted\r\n\r\n"
            + "--e\r\nContent-Type: application/pgp-encrypted\r\n\r\nVersion: 1\r\n"
            + "--e\r\nContent-Type: application/octet-stream\r\n\r\nencrypted\r\n--e--\r\n");
        EmailDisplaySecuritySummary encryptedSummary = new EmailDisplayBuilder().BuildWithRoot(encrypted).GetSecuritySummary();
        Assert.That(encryptedSummary.GetEncryptedContentCount(), Is.EqualTo(1));
        Assert.That(encryptedSummary.GetEncryptionPartCount(), Is.EqualTo(2));
        Assert.That(encryptedSummary.HasEncryptedContent(), Is.True);

        MimeEntity report = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("report body") });
        Assert.That(new EmailDisplayBuilder().BuildWithRoot(report).GetSecuritySummary().GetReportPartCount(), Is.EqualTo(1));
    }

    [Test]
    public void MessageWithoutBodyReturnsUnavailableProjectionAndWarning()
    {
        EmailDisplayModel model = new EmailDisplayBuilder().BuildWithMessage(new EmailMessageOperations().CreateWith());

        Assert.That(model.HasSelectedBody(), Is.False);
        Assert.That(model.GetText(), Is.Empty);
        Assert.That(model.HasHtml(), Is.False);
        Assert.That(model.GetWarnings().ContainsCode(EmailIssueCode.DisplayBodyUnavailable), Is.True);
        Assert.That(model.IsSuccess(), Is.True);
    }

    [Test]
    public void DisplayOptionsAndCollectionsAreDefensiveAndLimitsApply()
    {
        EmailDisplayOptions options = new EmailDisplayOptions();
        EmailTextAnalysisOptions analysis = options.GetAnalysisOptions();
        analysis.SetMaximumSnippetCharacters(1);
        Assert.That(options.GetAnalysisOptions().GetMaximumSnippetCharacters(), Is.Not.EqualTo(1));

        EmailDisplayModel model = new EmailDisplayBuilder().BuildWithRootAndOptions(ParseDisplayTree(), options);
        model.GetAlternatives().Clear();
        model.GetInlineResources().Clear();
        model.GetAttachments().Clear();
        model.GetIssues().Clear();
        Assert.That(model.GetAlternativeCount(), Is.EqualTo(2));
        Assert.That(model.GetInlineResourceCount(), Is.EqualTo(1));
        Assert.That(model.GetAttachmentCount(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetAttachment(1));
        Assert.Throws<ArgumentNullException>(() => new EmailDisplayBuilder().BuildWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDisplayBuilder().BuildWithRootAndOptions(ParseDisplayTree(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentNullException>(() => options.SetHtmlSanitizeOptions(null!));

        EmailDisplayOptions limited = new EmailDisplayOptions();
        limited.SetMaximumEntityCount(1);
        AssertLimit("MaxTraversalEntityCount", () => new EmailDisplayBuilder().BuildWithRootAndOptions(ParseDisplayTree(), limited));
    }

    static MimeEntity ParseDisplayTree()
    {
        const string source = "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: multipart/alternative; boundary=a\r\n\r\n"
            + "--a\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nPlain fallback\r\n"
            + "--a\r\nContent-Type: multipart/related; boundary=r\r\n\r\n"
            + "--r\r\nContent-Type: text/html; charset=utf-8\r\n\r\n"
            + "<p onclick='bad()'>Hello <strong>world</strong></p><script>bad</script>"
            + "<img src='https://remote.test/a'><img src='cid:logo@example.test'><img src='cid:missing@example.test'>\r\n"
            + "--r\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\nPNG\r\n--r--\r\n"
            + "--a--\r\n"
            + "--m\r\nContent-Type: application/pdf\r\nContent-Disposition: attachment; filename=file.pdf\r\n\r\nPDF\r\n"
            + "--m--\r\n";
        return ParseEntity(source);
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source));
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetEntity()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
