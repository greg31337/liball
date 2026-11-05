using System.Text;
using email;

namespace email.Tests;

public class EmailEditorHighLevelTests
{
    [Test]
    public void AlternativesArePlacedInsideTheMixedBodyAheadOfAttachments()
    {
        EmailMessage message = ParseMessage("Subject: edit\r\nContent-Type: text/plain\r\n\r\nplain");
        MimeEntity plain = message.GetBody()!;
        MimeEntity attachment = ParseEntity("Content-Type: application/pdf\r\nContent-Disposition: attachment; filename=report.pdf\r\n\r\npdf");
        MimeEntity html = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p>html</p>", "html", "us-ascii");

        Assert.That(new EmailEditor().AddAttachment(message, attachment), Is.SameAs(attachment));
        Assert.That(new EmailEditor().AddAlternative(message, html), Is.SameAs(html));

        MimeEntity mixed = message.GetBody()!;
        MimeEntity alternative = mixed!.GetChildAt(0);
        Assert.That(mixed.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(mixed.GetChildren(), Is.EqualTo(new[] { alternative, attachment }));
        Assert.That(alternative.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(alternative.GetChildren(), Is.EqualTo(new[] { plain, html }));
        Assert.That(new EmailBodySelector().SelectWithMessage(message).GetBody(), Is.SameAs(html));
        Assert.That(new EmailAttachmentFinder().FindWithMessage(message).Select(view => view.GetEntity()), Does.Contain(attachment));
        Assert.That(plain!.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
    }

    [Test]
    public void SettingAlternativesPreservesOuterRelatedResourcesAndAttachments()
    {
        EmailMessage message = ParseMessage("Subject: layers\r\nContent-Type: text/plain\r\n\r\nold body");
        MimeEntity oldBody = message.GetBody()!;
        MimeEntity resource = ParseEntity("Content-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\nlogo");
        MimeEntity attachment = ParseEntity("Content-Type: application/zip\r\nContent-Disposition: attachment; filename=archive.zip\r\n\r\nzip");
        new EmailEditor().AddRelatedResource(message, resource);
        new EmailEditor().AddAttachment(message, attachment);
        MimeEntity replacementPlain = new EmailEntityFactory().CreateTextWithValue("new plain");
        MimeEntity replacementHtml = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<img src='cid:logo@example.test'>", "html", "us-ascii");

        MimeEntity alternatives = new EmailEditor().SetAlternatives(message, new List<MimeEntity> { replacementPlain, replacementHtml });

        MimeEntity mixed = message.GetBody()!;
        MimeEntity related = mixed!.GetChildAt(0);
        Assert.That(mixed.GetChildren(), Is.EqualTo(new[] { related, attachment }));
        Assert.That(related.GetMediaType()!.IsType("multipart", "related"), Is.True);
        Assert.That(related.GetChildren(), Is.EqualTo(new[] { alternatives, resource }));
        Assert.That(alternatives.GetChildren(), Is.EqualTo(new[] { replacementPlain, replacementHtml }));
        Assert.That(oldBody!.HasParent(), Is.False);
        Assert.That(new EmailBodySelector().GetAlternativesWithMessage(message), Is.EqualTo(new[] { replacementPlain, replacementHtml }));
        Assert.That(new EmailRelatedIndexOperations().BuildWithMessage(message).ResolveContentId("cid:logo@example.test"), Is.SameAs(resource));
        Assert.That(new EmailAttachmentFinder().GetInlineResourcesWithMessage(message).Select(view => view.GetEntity()), Does.Contain(resource));
        Assert.That(new EmailAttachmentFinder().FindWithMessage(message).Select(view => view.GetEntity()), Does.Contain(attachment));
    }

    [Test]
    public void RelatedStartSelectsWhichRootReceivesAlternativesAndNewResources()
    {
        EmailMessage message = ParseMessage(
            "Content-Type: multipart/related; boundary=r; start=\"<root@example.test>\"\r\n\r\n"
            + "--r\r\nContent-Type: image/png\r\nContent-ID: <first@example.test>\r\n\r\nfirst\r\n"
            + "--r\r\nContent-Type: text/html\r\nContent-ID: <root@example.test>\r\n\r\n<p>root</p>\r\n"
            + "--r--\r\n");
        MimeEntity related = message.GetBody()!;
        Assert.That(related!.GetMediaType()!.GetParameters().GetDecoded("start"), Is.Not.Null);
        MimeEntity firstResource = related.GetChildAt(0);
        MimeEntity htmlRoot = related.GetChildAt(1);
        MimeEntity plain = new EmailEntityFactory().CreateTextWithValue("plain");
        MimeEntity secondResource = ParseEntity("Content-Type: image/png\r\nContent-Location: images/second.png\r\n\r\nsecond");

        new EmailEditor().AddAlternative(message, plain);
        new EmailEditor().AddRelatedResource(message, secondResource);

        MimeEntity alternatives = message.GetBody()!;
        Assert.That(alternatives!.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(alternatives.GetChildren(), Is.EqualTo(new[] { related, plain }));
        Assert.That(related.GetChildAt(0), Is.SameAs(firstResource));
        Assert.That(related.GetChildAt(1), Is.SameAs(htmlRoot));
        Assert.That(related.GetChildAt(2), Is.SameAs(secondResource));
        Assert.That(new EmailBodySelector().SelectWithMessage(message).GetBody(), Is.SameAs(htmlRoot));
        Assert.That(new EmailAttachmentFinder().GetInlineResourcesWithMessage(message).Select(view => view.GetEntity()), Is.EquivalentTo(new[] { firstResource, secondResource }));
    }

    [Test]
    public void DetachedContentCanBeReusedWhileFixedArityPartsRemainAtomic()
    {
        EmailMessage message = ParseMessage("Content-Type: text/plain\r\n\r\nbody");
        MimeEntity first = ParseEntity("Content-Type: application/octet-stream\r\n\r\nfirst");
        MimeEntity second = ParseEntity("Content-Type: application/octet-stream\r\n\r\nsecond");
        new EmailEditor().AddAttachment(message, first);
        new EmailEditor().AddAttachment(message, second);
        MimeEntity mixed = message.GetBody()!;

        MimeEntity detached = new EmailEditor().DetachContent(first);

        Assert.That(detached, Is.SameAs(first));
        Assert.That(detached.HasParent(), Is.False);
        Assert.That(mixed!.GetChildren(), Is.EqualTo(new[] { mixed.GetChildAt(0), second }));
        EmailMessage destination = ParseMessage("Content-Type: text/plain\r\n\r\ndestination");
        new EmailEditor().AddAttachment(destination, detached);
        Assert.That(detached.GetParent(), Is.SameAs(destination.GetBody()));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().DetachContent(message.GetBody()!));

        MimeEntity signedContent = new EmailEntityFactory().CreateTextWithValue("signed");
        MimeEntity signature = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-signature");
        MimeEntity signed = SecurityContainer(signedContent, signature);
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().DetachContent(signature));
        Assert.That(signed.GetChildren(), Is.EqualTo(new[] { signedContent, signature }));
        Assert.That(signature.GetParent(), Is.SameAs(signed));
    }

    [Test]
    public void EmbeddedMessagesAreAttachmentsAndNestedCyclesAreRejectedAtomically()
    {
        EmailMessage outer = ParseMessage("Subject: outer\r\nContent-Type: text/plain\r\n\r\nouter");
        EmailMessage embeddedMessage = ParseMessage("Subject: embedded\r\nContent-Type: text/plain\r\n\r\nembedded");

        MimeEntity embedded = new EmailEditor().EmbedMessage(outer, embeddedMessage);

        Assert.That(embedded.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(embedded.GetNestedMessage(), Is.SameAs(embeddedMessage));
        Assert.That(embedded.GetParent(), Is.SameAs(outer.GetBody()));
        Assert.That(new EmailAttachmentFinder().GetNestedMessagesWithMessage(outer).Select(view => view.GetEntity()), Does.Contain(embedded));
        MimeEntity embeddedBody = embeddedMessage.GetBody()!;
        int outerPartCount = outer.GetBody()!.GetChildCount();

        Assert.Throws<ArgumentException>(() => new EmailEditor().EmbedMessage(outer, outer));
        Assert.Throws<ArgumentException>(() => new EmailEditor().EmbedMessage(embeddedMessage, outer));

        Assert.That(outer.GetBody()!.GetChildCount(), Is.EqualTo(outerPartCount));
        Assert.That(embeddedMessage.GetBody(), Is.SameAs(embeddedBody));
        Assert.That(embeddedBody!.HasParent(), Is.False);
    }

    [Test]
    public void InvalidHighLevelInputsLeaveTheOriginalMessageClean()
    {
        EmailMessage message = ParseMessage("Content-Type: text/plain\r\n\r\nbody");
        MimeEntity body = message.GetBody()!;
        MimeEntity unaddressable = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 });
        MimeEntity duplicate = new EmailEntityFactory().CreateTextWithValue("duplicate");

        Assert.Throws<ArgumentException>(() => new EmailEditor().AddRelatedResource(message, unaddressable));
        Assert.Throws<ArgumentException>(() => new EmailEditor().AddAlternative(message, body!));
        Assert.Throws<ArgumentException>(() => new EmailEditor().SetAlternatives(message, new List<MimeEntity>()));
        Assert.Throws<ArgumentException>(() => new EmailEditor().SetAlternatives(message, new List<MimeEntity> { duplicate, duplicate }));

        Assert.That(message.GetBody(), Is.SameAs(body));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(unaddressable.HasParent(), Is.False);
        Assert.That(duplicate.HasParent(), Is.False);
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }

    static MimeEntity SecurityContainer(MimeEntity content, MimeEntity signature)
    {
        EmailMediaType mediaType = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("multipart/signed"), EmailStructuredValueParseMode.Strict).GetValue();
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Signed, new EmailHeadersOperations().CreateWith(), mediaType, null, new List<MimeEntity> { content, signature }, null);
    }
}
