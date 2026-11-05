using System.Text;
using email;

namespace email.Tests;

public class EmailBodySelectorTests
{
    [Test]
    public void DirectPlainBodyIsAnHtmlPreferenceFallback()
    {
        MimeEntity plain = Plain("plain");

        EmailBodySelection selection = new EmailBodySelector().SelectWithRoot(plain);

        Assert.That(selection.HasBody(), Is.True);
        Assert.That(selection.GetBody(), Is.SameAs(plain));
        Assert.That(selection.GetPreference(), Is.EqualTo(EmailBodyPreference.Html));
        Assert.That(selection.GetQuality(), Is.EqualTo(EmailBodySelectionQuality.Fallback));
        Assert.That(selection.GetReason(), Is.EqualTo(EmailBodySelectionReason.FallbackMediaType));
        Assert.That(selection.GetPath()!.GetDisplayText(), Is.EqualTo("root"));
        Assert.That(selection.GetAlternatives(), Is.EqualTo(new MimeEntity[] { plain }));
        Assert.That(selection.GetContextCount(), Is.Zero);
        Assert.That(new EmailBodySelector().GetPlainTextWithRoot(plain), Is.SameAs(plain));
        Assert.That(new EmailBodySelector().GetHtmlWithRoot(plain), Is.Null);
    }

    [Test]
    public void AlternativeSelectsLastRequestedTypeAndReturnsEveryCandidate()
    {
        MimeEntity firstPlain = Plain("first");
        MimeEntity html = Html("<p>html</p>");
        MimeEntity lastPlain = Plain("last");
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { firstPlain, html, lastPlain });

        EmailBodySelection htmlSelection = new EmailBodySelector().SelectWithRoot(alternative);
        EmailBodySelection plainSelection = new EmailBodySelector().SelectWithRootAndPreference(alternative, EmailBodyPreference.PlainText);

        Assert.That(htmlSelection.GetBody(), Is.SameAs(html));
        Assert.That(htmlSelection.GetQuality(), Is.EqualTo(EmailBodySelectionQuality.Preferred));
        Assert.That(htmlSelection.GetReason(), Is.EqualTo(EmailBodySelectionReason.PreferredMediaType));
        Assert.That(htmlSelection.GetPath()!.GetDisplayText(), Is.EqualTo("root/2"));
        Assert.That(htmlSelection.GetAlternatives(), Is.EqualTo(new MimeEntity[] { firstPlain, html, lastPlain }));
        Assert.That(htmlSelection.HasContext(EmailBodySelectionContext.Alternative), Is.True);
        Assert.That(plainSelection.GetBody(), Is.SameAs(lastPlain));
        Assert.That(new EmailBodySelector().GetHtmlWithRoot(alternative), Is.SameAs(html));
        Assert.That(new EmailBodySelector().GetPlainTextWithRoot(alternative), Is.SameAs(lastPlain));

        List<MimeEntity> returned = new EmailBodySelector().GetAlternativesWithRoot(alternative);
        returned.Clear();
        Assert.That(new EmailBodySelector().GetAlternativesWithRoot(alternative).Count, Is.EqualTo(3));
    }

    [Test]
    public void RelatedHonorsStartAndDoesNotPromoteResources()
    {
        MimeEntity related = ParseEntity(
            "Content-Type: multipart/related; boundary=x; start=\"<body@example.test>\"\r\n\r\n"
            + "--x\r\nContent-Type: image/png\r\nContent-ID: <resource@example.test>\r\n\r\npng\r\n"
            + "--x\r\nContent-Type: text/html\r\nContent-ID: <body@EXAMPLE.TEST>\r\n\r\n<p>body</p>\r\n"
            + "--x--\r\n");
        MimeEntity html = related.GetChildAt(1);

        EmailBodySelection selection = new EmailBodySelector().SelectWithRoot(related);

        Assert.That(selection.GetBody(), Is.SameAs(html));
        Assert.That(selection.GetPath()!.GetDisplayText(), Is.EqualTo("root/2"));
        Assert.That(selection.GetAlternatives(), Is.EqualTo(new MimeEntity[] { html }));
        Assert.That(selection.HasContext(EmailBodySelectionContext.RelatedRoot), Is.True);

        MimeEntity defaultRoot = ParseEntity(
            "Content-Type: multipart/related; boundary=x\r\n\r\n"
            + "--x\r\nContent-Type: image/png\r\n\r\npng\r\n"
            + "--x\r\nContent-Type: text/html\r\n\r\n<p>resource</p>\r\n"
            + "--x--\r\n");
        Assert.That(new EmailBodySelector().SelectWithRoot(defaultRoot).HasBody(), Is.False);
    }

    [Test]
    public void MixedSkipsAttachmentsAndUsesOnlyTheFirstBodyStructure()
    {
        MimeEntity attachment = ParseEntity("Content-Type: text/plain\r\nContent-Disposition: attachment\r\n\r\nattached");
        MimeEntity plain = Plain("plain");
        MimeEntity html = Html("<p>html</p>");
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { plain, html });
        MimeEntity later = Plain("later");
        MimeEntity mixed = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { attachment, alternative, later });

        EmailBodySelection selection = new EmailBodySelector().SelectWithRoot(mixed);

        Assert.That(selection.GetBody(), Is.SameAs(html));
        Assert.That(selection.GetAlternatives(), Is.EqualTo(new MimeEntity[] { plain, html }));
        Assert.That(selection.GetPath()!.GetDisplayText(), Is.EqualTo("root/2/2"));
        Assert.That(selection.GetContexts(), Is.EqualTo(new[] { EmailBodySelectionContext.Mixed, EmailBodySelectionContext.Alternative }));
    }

    [Test]
    public void SignedSelectsContentAndEncryptedReportsUnavailable()
    {
        MimeEntity signedPlain = Plain("signed");
        MimeEntity signature = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1, 2 }, "application", "pgp-signature");
        MimeEntity signed = Container(MimeEntityKind.Signed, "signed", signedPlain, signature);

        EmailBodySelection signedSelection = new EmailBodySelector().SelectWithRootAndPreference(signed, EmailBodyPreference.PlainText);

        Assert.That(signedSelection.GetBody(), Is.SameAs(signedPlain));
        Assert.That(signedSelection.GetPath()!.GetDisplayText(), Is.EqualTo("root/1"));
        Assert.That(signedSelection.HasContext(EmailBodySelectionContext.SignedContent), Is.True);

        MimeEntity control = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-encrypted");
        MimeEntity ciphertext = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 2 }, "application", "octet-stream");
        MimeEntity encrypted = Container(MimeEntityKind.Encrypted, "encrypted", control, ciphertext);
        EmailBodySelection encryptedSelection = new EmailBodySelector().SelectWithRoot(encrypted);

        Assert.That(encryptedSelection.HasBody(), Is.False);
        Assert.That(encryptedSelection.GetQuality(), Is.EqualTo(EmailBodySelectionQuality.Unavailable));
        Assert.That(encryptedSelection.GetReason(), Is.EqualTo(EmailBodySelectionReason.EncryptedContentUnavailable));
        Assert.That(encryptedSelection.HasPath(), Is.False);
        Assert.That(encryptedSelection.GetAlternatives(), Is.Empty);
    }

    [Test]
    public void NestedAndUnknownContainersRetainSelectionContextAndPath()
    {
        MimeEntity nestedHtml = Html("<p>nested</p>");
        EmailMessage nestedMessage = new EmailMessageOperations().CreateWith();
        nestedMessage.SetBody(nestedHtml);
        MimeEntity nestedEntity = new EmailEntityFactory().CreateNestedMessage(nestedMessage);
        MimeEntity unknown = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("x-wrapper", new List<MimeEntity> { new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 }), nestedEntity, Plain("later") });
        EmailMessage outer = new EmailMessageOperations().CreateWith();
        outer.SetBody(unknown);

        EmailBodySelection selection = new EmailBodySelector().SelectWithMessage(outer);

        Assert.That(selection.GetBody(), Is.SameAs(nestedHtml));
        Assert.That(selection.GetPath()!.GetDisplayText(), Is.EqualTo("root/2/1"));
        Assert.That(selection.GetContexts(), Is.EqualTo(new[] { EmailBodySelectionContext.UnknownMultipart, EmailBodySelectionContext.NestedMessage }));
        Assert.That(new EmailBodySelector().GetHtmlWithMessage(outer), Is.SameAs(nestedHtml));
    }

    [Test]
    public void UnsupportedBodiesAndBoundsReturnExplicitOutcomes()
    {
        MimeEntity binary = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 });
        EmailBodySelection unavailable = new EmailBodySelector().SelectWithRoot(binary);

        Assert.That(unavailable.HasBody(), Is.False);
        Assert.That(unavailable.GetReason(), Is.EqualTo(EmailBodySelectionReason.NoSupportedBody));

        MimeEntity deep = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>
        {
            new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { Plain("deep") })
        });
        EmailLimitException depth = Assert.Throws<EmailLimitException>(() => new EmailBodySelector().SelectWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(deep, EmailBodyPreference.Html, 1, 3));
        Assert.That(depth.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailBodySelector().GetAlternativesWithRootAndMaximumDepthAndMaximumEntityCount(deep, 2, 2));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxTraversalEntityCount"));
    }

    [Test]
    public void SelectorAndSelectionValidateArgumentsAndIndexes()
    {
        MimeEntity plain = Plain("plain");
        EmailMessage empty = new EmailMessageOperations().CreateWith();

        Assert.Throws<ArgumentNullException>(() => new EmailBodySelector().SelectWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailBodySelector().SelectWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentException>(() => new EmailBodySelector().SelectWithMessage(empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBodySelector().SelectWithRootAndPreference(plain, (EmailBodyPreference)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBodySelector().SelectWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(plain, EmailBodyPreference.Html, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBodySelector().SelectWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(plain, EmailBodyPreference.Html, 0, 0));

        EmailBodySelection selection = new EmailBodySelector().SelectWithRoot(plain);
        Assert.That(new EmailBodySelector().SelectWithRootAndMaximumDepthAndMaximumEntityCount(plain, 0, 1).GetBody(), Is.SameAs(plain));
        Assert.Throws<ArgumentOutOfRangeException>(() => selection.GetAlternativeAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => selection.GetContextAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => selection.HasContext((EmailBodySelectionContext)99));
    }

    static MimeEntity Plain(string value)
    {
        return new EmailEntityFactory().CreateTextWithValue(value);
    }

    static MimeEntity Html(string value)
    {
        return new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(value, "html", "us-ascii");
    }

    static MimeEntity Container(MimeEntityKind kind, string subtype, MimeEntity first, MimeEntity second)
    {
        EmailMediaType mediaType = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("multipart/" + subtype), EmailStructuredValueParseMode.Strict).GetValue();
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), mediaType, null, new List<MimeEntity> { first, second }, null);
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.ASCII.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
