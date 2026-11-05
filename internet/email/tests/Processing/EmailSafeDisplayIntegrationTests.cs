using System.Text;
using email;

namespace email.Tests;

public class EmailSafeDisplayIntegrationTests
{
    [Test]
    public void ComplexTreeKeepsSelectionResourcesAttachmentsAndSecurityRolesDistinct()
    {
        ComplexDisplayTree tree = CreateComplexTree();

        EmailDisplayModel display = new EmailDisplayBuilder().BuildWithRoot(tree.Root);
        EmailBodySelection selection = display.GetBodySelection();
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRoot(tree.Root);

        Assert.That(selection.GetBody(), Is.SameAs(tree.Html));
        Assert.That(selection.GetAlternatives(), Is.EqualTo(new[] { tree.Plain, tree.Html }));
        Assert.That(selection.GetContexts(), Is.EqualTo(new[]
        {
            EmailBodySelectionContext.Mixed,
            EmailBodySelectionContext.SignedContent,
            EmailBodySelectionContext.RelatedRoot,
            EmailBodySelectionContext.Alternative
        }));
        Assert.That(display.GetHtml(), Does.Contain("cid:Logo%40EXAMPLE.TEST"));
        Assert.That(display.GetHtml(), Does.Not.Contain("javascript"));
        Assert.That(display.GetHtml(), Does.Not.Contain("remote.invalid"));
        Assert.That(display.GetHtml(), Does.Not.Contain("<script"));
        Assert.That(display.GetMissingRelatedReferences().Select(item => item.GetNormalizedValue()), Is.EqualTo(new[] { "missing@example.test" }));
        Assert.That(display.GetMissingRelatedReferences()[0].GetSourceEntity(), Is.SameAs(tree.Html));

        Assert.That(ViewFor(views, tree.Logo).GetKind(), Is.EqualTo(EmailAttachmentKind.InlineResource));
        Assert.That(ViewFor(views, tree.DuplicateLogo).GetKind(), Is.EqualTo(EmailAttachmentKind.InlineResource));
        Assert.That(ViewFor(views, tree.RelatedAttachment).GetKind(), Is.EqualTo(EmailAttachmentKind.Attachment));
        Assert.That(ViewFor(views, tree.NamedInline).GetKind(), Is.EqualTo(EmailAttachmentKind.Attachment));
        Assert.That(ViewFor(views, tree.OrphanCid).GetKind(), Is.EqualTo(EmailAttachmentKind.InlineResource));
        Assert.That(display.GetInlineResourceCount(), Is.EqualTo(3));
        Assert.That(display.GetAttachmentCount(), Is.EqualTo(4));

        EmailDisplaySecuritySummary security = display.GetSecuritySummary();
        Assert.That(security.GetSignedContentCount(), Is.EqualTo(1));
        Assert.That(security.GetSignaturePartCount(), Is.EqualTo(1));
        Assert.That(security.GetEncryptedContentCount(), Is.EqualTo(1));
        Assert.That(security.GetEncryptionPartCount(), Is.EqualTo(2));
        Assert.That(security.ClaimsVerification(), Is.False);
        Assert.That(display.GetWarnings().ContainsCode(EmailIssueCode.HtmlRemoteContentBlocked), Is.True);
        Assert.That(display.GetWarnings().ContainsCode(EmailIssueCode.HtmlUnsafeUrlBlocked), Is.True);
        Assert.That(display.GetWarnings().ContainsCode(EmailIssueCode.DisplayRelatedResourceMissing), Is.True);

        EmailRelatedIndex related = new EmailRelatedIndexOperations().BuildWithRoot(tree.Root);
        Assert.That(related.ResolveContentId("cid:Logo%40EXAMPLE.TEST"), Is.SameAs(tree.Logo));
        Assert.That(related.GetDuplicateCount(), Is.EqualTo(1));
    }

    [Test]
    public void CharsetTransferDecodingAndFlowedTextFeedAnalysisAndSnippet()
    {
        ComplexDisplayTree tree = CreateComplexTree();
        EmailDisplayOptions options = new EmailDisplayOptions();
        options.SetBodyPreference(EmailBodyPreference.PlainText);

        EmailDisplayModel display = new EmailDisplayBuilder().BuildWithRootAndOptions(tree.Root, options);

        Assert.That(display.GetText(), Is.EqualTo("Café flowedcontinuation\r\n>quotedreply\r\n-- \r\nSignature"));
        Assert.That(display.GetSourceTextDecodeResult()!.GetSelectedCharset(), Is.EqualTo("windows-1252"));
        Assert.That(display.GetTextAnalysis().GetVisibleText(), Is.EqualTo("Café flowedcontinuation"));
        Assert.That(display.GetTextAnalysis().GetQuotedText(), Is.EqualTo(">quotedreply"));
        Assert.That(display.GetTextAnalysis().GetSignatureText(), Is.EqualTo("-- \r\nSignature"));
        Assert.That(display.GetSnippet(), Is.EqualTo("Café flowedcontinuation"));
        Assert.That(display.GetIssues().ContainsCode(EmailIssueCode.TextQuotedReplyDetected), Is.True);
        Assert.That(display.GetIssues().ContainsCode(EmailIssueCode.TextSignatureDetected), Is.True);
    }

    [Test]
    public void AdversarialHtmlIsBalancedAndCannotRetainActiveOrRemoteContent()
    {
        const string html = "<div><p onclick='run()'><strong>Visible</p>tail</strong>"
            + "<script><img src='cid:inside-script@example.test'></script>"
            + "<svg><script>active</script></svg>"
            + "<a href='jav&#x61;script:alert(1)'>link</a>"
            + "<img src='https://remote.invalid/pixel' onerror='run()' style='display:none'>"
            + "<img src='CID:asset@example.test' alt='asset'>"
            + "<img src='data:image/svg+xml,%3Csvg%3E'></div>";

        EmailHtmlSanitizeResult result = new EmailHtmlProcessor().SanitizeWithHtml(html);

        Assert.That(result.GetHtml(), Is.EqualTo("<div><p><strong>Visible</strong></p>tail<a>link</a><img><img src=\"cid:asset@example.test\" alt=\"asset\"><img></div>"));
        Assert.That(result.GetHtml(), Does.Not.Contain("script"));
        Assert.That(result.GetHtml(), Does.Not.Contain("svg"));
        Assert.That(result.GetHtml(), Does.Not.Contain("javascript"));
        Assert.That(result.GetHtml(), Does.Not.Contain("https://"));
        Assert.That(result.GetBlockedRemoteResourceCount(), Is.EqualTo(1));
        Assert.That(result.GetBlockedUnsafeUrlCount(), Is.EqualTo(2));
        Assert.That(result.GetPreservedCidResourceCount(), Is.EqualTo(1));
        Assert.That(result.GetMutations().Any(item => item.GetKind() == EmailHtmlMutationKind.RebalancedMarkup), Is.True);
    }

    [Test]
    public void ExtractedDataUrisBecomeResolvableCidResourcesBeforeSafeDisplay()
    {
        const string html = "<p>Images</p><img src='data:image/png;base64,UE5H'>"
            + "<img src='data:image/png;base64,UE5H'>"
            + "<img src='data:image/svg+xml,%3Csvg%3E'>";

        EmailHtmlDataUriResult extracted = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);
        MimeEntity rewrittenHtml = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(extracted.GetHtml(), "html", "utf-8");
        List<MimeEntity> children = new List<MimeEntity> { rewrittenHtml };
        children.AddRange(extracted.GetRelatedParts());
        MimeEntity relatedRoot = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", children);

        EmailRelatedIndex index = new EmailRelatedIndexOperations().BuildWithRoot(relatedRoot);
        EmailDisplayModel display = new EmailDisplayBuilder().BuildWithRoot(relatedRoot);

        Assert.That(extracted.GetExtractionCount(), Is.EqualTo(2));
        Assert.That(extracted.GetRelatedPartCount(), Is.EqualTo(1));
        Assert.That(extracted.GetExtraction(1).ReusedPart(), Is.True);
        Assert.That(extracted.GetRejectedDataUriCount(), Is.EqualTo(1));
        Assert.That(index.GetReferenceAt(0).GetResolvedEntity(), Is.SameAs(extracted.GetRelatedPart(0)));
        Assert.That(index.GetReferenceAt(1).GetResolvedEntity(), Is.SameAs(extracted.GetRelatedPart(0)));
        Assert.That(index.GetMissingReferences(), Is.Empty);
        Assert.That(display.GetInlineResourceCount(), Is.EqualTo(1));
        Assert.That(display.GetMissingRelatedReferences(), Is.Empty);
        Assert.That(display.GetHtml(), Does.Contain("cid:" + extracted.GetExtraction(0).GetContentId()));
        Assert.That(display.GetHtml(), Does.Not.Contain("data:"));
    }

    [Test]
    public void ComplexProjectionAndSearchRemainBoundedAndDoNotMutateParsedLeaves()
    {
        ComplexDisplayTree tree = CreateComplexTree();
        EmailTextSearchOptions searchOptions = new EmailTextSearchOptions();
        searchOptions.SetIncludeTextAttachments(true);
        searchOptions.SetMaximumMatches(2);

        EmailTextSearchResult search = new EmailTextProcessor().SearchWithRootAndQueryAndOptions(tree.Root, "signature", searchOptions);

        Assert.That(search.GetMatchCount(), Is.EqualTo(2));
        Assert.That(search.WasTruncated(), Is.True);
        Assert.That(search.GetIssues().ContainsCode(EmailIssueCode.TextSearchLimitReached), Is.True);
        Assert.That(tree.Plain.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(tree.Html.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        EmailDisplayOptions limited = new EmailDisplayOptions();
        limited.SetMaximumDepth(2);
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailDisplayBuilder().BuildWithRootAndOptions(tree.Root, limited));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
    }

    static ComplexDisplayTree CreateComplexTree()
    {
        MimeEntity plain = ParseEntity(
            "Content-Type: text/plain; charset=windows-1252; format=flowed; delsp=yes\r\n"
            + "Content-Transfer-Encoding: quoted-printable\r\n\r\n"
            + "Caf=E9 flowed=20\r\ncontinuation\r\n> quoted=20\r\n>reply\r\n--=20\r\nSignature");
        const string markup = "<div><p onclick='run()'>Current <strong>message</strong></p>"
            + "<script>signature signature hidden</script>"
            + "<a href='jav&#x61;script:alert(1)'>unsafe</a>"
            + "<img src='https://remote.invalid/pixel'>"
            + "<img src='CID:Logo%40EXAMPLE.TEST' alt='logo'>"
            + "<img src='cid:missing@example.test'></div>";
        MimeEntity html = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(markup, "html", "utf-8");
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { plain, html });
        MimeEntity logo = ParseEntity("Content-Type: image/png\r\nContent-ID: <Logo@Example.TEST>\r\nContent-Location: images/logo.png\r\n\r\nfirst");
        MimeEntity duplicateLogo = ParseEntity("Content-Type: image/png\r\nContent-ID: <Logo@example.test>\r\nContent-Location: images/logo-copy.png\r\n\r\nsecond");
        MimeEntity relatedAttachment = ParseEntity("Content-Type: image/png\r\nContent-ID: <download@example.test>\r\nContent-Disposition: attachment; filename=download.png\r\n\r\ndownload");
        MimeEntity related = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { alternative, logo, duplicateLogo, relatedAttachment });
        MimeEntity signature = ParseEntity("Content-Type: application/pgp-signature\r\n\r\nsignature signature");
        MimeEntity signed = Container(MimeEntityKind.Signed, "signed", related, signature);

        MimeEntity namedInline = ParseEntity("Content-Type: text/plain\r\nContent-Disposition: inline; filename=visible.txt\r\n\r\nsignature attachment");
        MimeEntity orphanCid = ParseEntity("Content-Type: application/octet-stream\r\nContent-ID: <orphan@example.test>\r\n\r\norphan");
        MimeEntity encryptionControl = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-encrypted");
        MimeEntity encryptedPayload = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 2 }, "application", "octet-stream");
        MimeEntity encrypted = Container(MimeEntityKind.Encrypted, "encrypted", encryptionControl, encryptedPayload);

        MimeEntity nestedAttachment = ParseEntity("Content-Type: text/plain; name=nested.txt\r\n\r\nsignature nested attachment");
        MimeEntity nestedRoot = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("nested body"), nestedAttachment });
        EmailMessage nestedMessage = new EmailMessageOperations().CreateWith();
        nestedMessage.SetBody(nestedRoot);
        MimeEntity nested = new EmailEntityFactory().CreateNestedMessage(nestedMessage);
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { signed, namedInline, orphanCid, encrypted, nested });
        return new ComplexDisplayTree(root, plain, html, logo, duplicateLogo, relatedAttachment, namedInline, orphanCid);
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

    static EmailAttachmentView ViewFor(List<EmailAttachmentView> views, MimeEntity entity)
    {
        return views.Single(view => ReferenceEquals(view.GetEntity(), entity));
    }
}

class ComplexDisplayTree
{
    public MimeEntity Root { get; }
    public MimeEntity Plain { get; }
    public MimeEntity Html { get; }
    public MimeEntity Logo { get; }
    public MimeEntity DuplicateLogo { get; }
    public MimeEntity RelatedAttachment { get; }
    public MimeEntity NamedInline { get; }
    public MimeEntity OrphanCid { get; }

    public ComplexDisplayTree(MimeEntity root, MimeEntity plain, MimeEntity html, MimeEntity logo, MimeEntity duplicateLogo, MimeEntity relatedAttachment, MimeEntity namedInline, MimeEntity orphanCid)
    {
        Root = root;
        Plain = plain;
        Html = html;
        Logo = logo;
        DuplicateLogo = duplicateLogo;
        RelatedAttachment = relatedAttachment;
        NamedInline = namedInline;
        OrphanCid = orphanCid;
    }
}
