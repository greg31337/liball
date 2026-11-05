using System.Text;
using email;

namespace email.Tests;

public class MimeEntityTests
{
    [Test]
    public void LeafRetainsCommonMetadataContentSourceAndIssues()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", Bytes(" application/octet-stream"), EmailHeaderValueKind.MediaType) });
        EmailParameterList parameters = Parameters();
        EmailDisposition disposition = new EmailDisposition("attachment", parameters, Bytes("attachment"), Position());
        EmailMessageId contentId = new EmailMessageId("part", "example.test", false, Bytes("<part@example.test>"), Position(), false, false);
        EmailContentLanguage language = new EmailContentLanguage(new List<string> { "en", "fr" }, Bytes("en, fr"), Position());
        EmailContent raw = Content("raw");
        EmailContent decoded = Content("decoded");
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.MimeStructureMalformed, "retained issue"));

        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, headers, Media("application", "octet-stream"), disposition, "x-custom", contentId, "cid:part@example.test", language, "sample part", raw, decoded, new List<MimeEntity>(), null, null, null, new EmailRange(20, 30), issues);

        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(entity.GetHeaders(), Is.SameAs(headers));
        Assert.That(entity.GetMediaType()!.IsType("application", "octet-stream"), Is.True);
        Assert.That(entity.GetDisposition(), Is.SameAs(disposition));
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("x-custom"));
        Assert.That(entity.HasKnownTransferEncoding(), Is.False);
        Assert.Throws<InvalidOperationException>(() => entity.GetTransferEncodingKind());
        Assert.That(entity.GetContentId(), Is.SameAs(contentId));
        Assert.That(entity.GetContentLocation(), Is.EqualTo("cid:part@example.test"));
        Assert.That(entity.GetContentLanguage(), Is.SameAs(language));
        Assert.That(entity.GetContentDescription(), Is.EqualTo("sample part"));
        Assert.That(entity.GetContent(), Is.SameAs(raw));
        Assert.That(entity.GetRawContent(), Is.SameAs(raw));
        Assert.That(entity.GetDecodedContent(), Is.SameAs(decoded));
        Assert.That(entity.GetSourceRange()!.EqualsRange(new EmailRange(20, 30)), Is.True);
        Assert.That(entity.GetOwnIssues().GetCount(), Is.EqualTo(1));
        Assert.That(entity.IsContainer(), Is.False);
    }

    [TestCase(MimeEntityKind.PartialMessage, "message", "partial")]
    [TestCase(MimeEntityKind.ExternalBody, "message", "external-body")]
    [TestCase(MimeEntityKind.DeliveryStatus, "message", "delivery-status")]
    [TestCase(MimeEntityKind.DispositionNotification, "message", "disposition-notification")]
    [TestCase(MimeEntityKind.FeedbackReport, "message", "feedback-report")]
    [TestCase(MimeEntityKind.Calendar, "text", "calendar")]
    [TestCase(MimeEntityKind.Tnef, "application", "ms-tnef")]
    public void SpecializedContentKindsRequireTheirMatchingMediaTypes(MimeEntityKind kind, string type, string subtype)
    {
        MimeEntity entity = ContentEntity(kind, type, subtype);

        Assert.That(entity.GetKind(), Is.EqualTo(kind));
        Assert.That(entity.GetMediaType()!.IsType(type, subtype), Is.True);
        Assert.That(entity.HasContent(), Is.True);
        Assert.That(entity.GetChildCount(), Is.Zero);
        Assert.Throws<ArgumentException>(() => ContentEntity(kind, "application", "octet-stream"));
    }

    [Test]
    public void MultipartPreservesOrderedChildrenParentLinksPreambleAndEpilogue()
    {
        MimeEntity first = Leaf("text", "plain");
        MimeEntity second = Leaf("text", "html");
        EmailContent preamble = Content("preamble");
        EmailContent epilogue = Content("epilogue");
        MimeEntity multipart = FullEntity(MimeEntityKind.Multipart, Media("multipart", "alternative"), null!, null!, new List<MimeEntity> { first, second }, null!, preamble, epilogue);
        List<MimeEntity> returned = multipart.GetChildren();
        returned.Clear();

        Assert.That(multipart.IsContainer(), Is.True);
        Assert.That(multipart.GetChildCount(), Is.EqualTo(2));
        Assert.That(multipart.GetChildAt(0), Is.SameAs(first));
        Assert.That(multipart.GetChildAt(1), Is.SameAs(second));
        Assert.That(first.GetParent(), Is.SameAs(multipart));
        Assert.That(second.GetParent(), Is.SameAs(multipart));
        Assert.That(multipart.GetPreamble(), Is.SameAs(preamble));
        Assert.That(multipart.GetEpilogue(), Is.SameAs(epilogue));
    }

    [Test]
    public void NestedMessageKindHoldsOneMessageAndNoDirectContent()
    {
        EmailMessage nested = new EmailMessageOperations().CreateWith();
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), null, new List<MimeEntity>(), nested);

        Assert.That(entity.HasNestedMessage(), Is.True);
        Assert.That(entity.GetNestedMessage(), Is.SameAs(nested));
        Assert.That(entity.HasContent(), Is.False);
        Assert.That(entity.GetChildCount(), Is.Zero);
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), Content("bad"), new List<MimeEntity>(), nested));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), null, new List<MimeEntity>(), null));
    }

    [TestCase(MimeEntityKind.Signed, "signed")]
    [TestCase(MimeEntityKind.Encrypted, "encrypted")]
    public void SignedAndEncryptedContainersAlwaysHaveExactlyTwoChildren(MimeEntityKind kind, string subtype)
    {
        MimeEntity first = Leaf("application", "octet-stream");
        MimeEntity second = Leaf("application", "pkcs7-signature");
        MimeEntity container = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), Media("multipart", subtype), null, new List<MimeEntity> { first, second }, null);
        MimeEntity replacement = Leaf("application", "pgp-signature");

        MimeEntity removed = container.ReplaceChildAt(1, replacement);

        Assert.That(removed, Is.SameAs(second));
        Assert.That(removed.HasParent(), Is.False);
        Assert.That(replacement.GetParent(), Is.SameAs(container));
        Assert.Throws<InvalidOperationException>(() => container.AddChild(Leaf("text", "plain")));
        Assert.Throws<InvalidOperationException>(() => container.RemoveChildAt(0));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), Media("multipart", subtype), null, new List<MimeEntity> { Leaf("text", "plain") }, null));
    }

    [Test]
    public void MultipartChildMutationsPreserveOwnershipOrderAndRejectCycles()
    {
        MimeEntity root = Multipart("mixed");
        MimeEntity first = Leaf("text", "plain");
        MimeEntity second = Leaf("text", "html");
        MimeEntity otherParent = Multipart("alternative");

        root.AddChild(first);
        root.InsertChild(0, second);
        root.MoveChild(0, 1);

        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.Throws<ArgumentException>(() => otherParent.AddChild(first));
        Assert.Throws<ArgumentException>(() => root.AddChild(first));
        Assert.Throws<ArgumentException>(() => root.AddChild(root));

        MimeEntity removed = root.RemoveChildAt(0);
        Assert.That(removed, Is.SameAs(first));
        Assert.That(removed.HasParent(), Is.False);
        otherParent.AddChild(removed);
        Assert.That(removed.GetParent(), Is.SameAs(otherParent));

        Assert.Throws<ArgumentException>(() => second.AddChild(root));
    }

    [Test]
    public void ShapeInvariantsSeparateContentContainersAndNestedMessages()
    {
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("text", "plain"), null, new List<MimeEntity>(), null));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("text", "plain"), Content("body"), new List<MimeEntity> { Leaf("text", "plain") }, null));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), Media("multipart", "mixed"), Content("body"), new List<MimeEntity>(), null));
        Assert.Throws<ArgumentException>(() => FullEntity(MimeEntityKind.Leaf, Media("text", "plain"), Content("body"), null!, new List<MimeEntity>(), null!, Content("bad"), null!));

        MimeEntity decodedOnly = FullEntity(MimeEntityKind.Leaf, Media("text", "plain"), null!, Content("decoded"), new List<MimeEntity>(), null!, null!, null!);
        Assert.That(decodedOnly.HasContent(), Is.True);
        Assert.That(decodedOnly.GetContent(), Is.SameAs(decodedOnly.GetDecodedContent()));
    }

    [Test]
    public void MediaTypeDiscriminantRejectsRecognizedKindMismatches()
    {
        Assert.Throws<ArgumentException>(() => ContentEntity(MimeEntityKind.Leaf, "multipart", "mixed"));
        Assert.Throws<ArgumentException>(() => ContentEntity(MimeEntityKind.Leaf, "text", "calendar"));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), Media("multipart", "signed"), null, new List<MimeEntity>(), null));
        Assert.Throws<ArgumentException>(() => new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "partial"), null, new List<MimeEntity>(), new EmailMessageOperations().CreateWith()));
        Assert.Throws<ArgumentException>(() => ContentEntity(MimeEntityKind.Tnef, "application", "octet-stream"));
    }

    [Test]
    public void KnownAndExtensionTransferEncodingsAreRepresentedWithoutLoss()
    {
        MimeEntity base64 = FullMetadataLeaf("BaSe64");
        MimeEntity extension = FullMetadataLeaf("x-vendor-encoding");

        Assert.That(base64.HasKnownTransferEncoding(), Is.True);
        Assert.That(base64.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(extension.HasKnownTransferEncoding(), Is.False);
        Assert.That(extension.GetTransferEncodingName(), Is.EqualTo("x-vendor-encoding"));
        Assert.Throws<ArgumentException>(() => FullMetadataLeaf("bad encoding"));
        Assert.Throws<ArgumentException>(() => FullMetadataLeaf("bad/encoding"));
    }

    [Test]
    public void IssuesAggregateHeadersAndDescendantsDefensively()
    {
        EmailIssueList childIssues = new EmailIssueList();
        childIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.MimeStructureMalformed, "child"));
        MimeEntity child = FullEntity(MimeEntityKind.Leaf, Media("text", "plain"), Content("body"), null!, new List<MimeEntity>(), null!, null!, null!, childIssues);
        EmailIssueList parentIssues = new EmailIssueList();
        parentIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.MessageStructureInvalid, "parent"));
        MimeEntity parent = FullEntity(MimeEntityKind.Multipart, Media("multipart", "mixed"), null!, null!, new List<MimeEntity> { child }, null!, null!, null!, parentIssues);

        EmailIssueList aggregate = parent.GetIssues();
        aggregate.Clear();

        Assert.That(parent.GetOwnIssues().GetCount(), Is.EqualTo(1));
        Assert.That(parent.GetIssues().GetCount(), Is.EqualTo(2));
    }

    [Test]
    public void MessageBodyIntegrationTracksReplacementAndRejectsInvalidRootsAndCycles()
    {
        MimeEntity original = Leaf("text", "plain");
        EmailContent source = new EmailContentOperations().CreateMemory(new byte[100], EmailContentOwnership.Borrowed, false);
        EmailSourceInfo info = new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(source, new EmailRange(0, 100), new EmailRange(0, 40), new EmailRange(40, 2), new EmailRange(42, 58), null, true);
        EmailMessage message = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), original, new EmailEnvelopeOperations().CreateWith(), info, new EmailIssueList());
        MimeEntity replacement = Leaf("text", "html");

        Assert.That(message.GetBody(), Is.SameAs(original));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        message.SetBody(replacement);

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(message.GetCleanRanges(), Has.Count.EqualTo(1));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 42)), Is.True);

        MimeEntity parent = Multipart("mixed");
        MimeEntity owned = Leaf("text", "plain");
        parent.AddChild(owned);
        Assert.Throws<ArgumentException>(() => message.SetBody(owned));

        EmailMessage cyclic = new EmailMessageOperations().CreateWith();
        MimeEntity nestedSelf = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), null, new List<MimeEntity>(), cyclic);
        Assert.Throws<ArgumentException>(() => cyclic.SetBody(nestedSelf));
    }

    static MimeEntity Leaf(string type, string subtype)
    {
        return ContentEntity(MimeEntityKind.Leaf, type, subtype);
    }

    static MimeEntity Multipart(string subtype)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), Media("multipart", subtype), null, new List<MimeEntity>(), null);
    }

    static MimeEntity ContentEntity(MimeEntityKind kind, string type, string subtype)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), Media(type, subtype), Content("body"), new List<MimeEntity>(), null);
    }

    static MimeEntity FullMetadataLeaf(string transferEncoding)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "octet-stream"), null, transferEncoding, null, null, null, null, Content("raw"), null, new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    static MimeEntity FullEntity(MimeEntityKind kind, EmailMediaType mediaType, EmailContent raw, EmailContent decoded, List<MimeEntity> children, EmailMessage nested, EmailContent preamble, EmailContent epilogue)
    {
        return FullEntity(kind, mediaType, raw, decoded, children, nested, preamble, epilogue, new EmailIssueList());
    }

    static MimeEntity FullEntity(MimeEntityKind kind, EmailMediaType mediaType, EmailContent raw, EmailContent decoded, List<MimeEntity> children, EmailMessage nested, EmailContent preamble, EmailContent epilogue, EmailIssueList issues)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, new EmailHeadersOperations().CreateWith(), mediaType, null, null, null, null, null, null, raw, decoded, children, nested, preamble, epilogue, null, issues);
    }

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, Parameters(), Bytes(type + "/" + subtype), Position(), false);
    }

    static EmailParameterList Parameters()
    {
        return new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }

    static EmailContent Content(string value)
    {
        return new EmailContentOperations().CreateMemory(Bytes(value), EmailContentOwnership.Borrowed, false);
    }

    static EmailSourcePosition Position()
    {
        return new EmailSourcePosition(0, 1, 1);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
