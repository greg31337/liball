using System.Text;
using email;

namespace email.Tests;

public class EmailEditorTests
{
    [Test]
    public void MessageBodyWrapAndUnwrapPreserveIdentityAndTrackNetDirtyState()
    {
        EmailMessage message = ParseMessage("Subject: edit\r\nContent-Type: text/plain\r\n\r\nbody");
        MimeEntity original = message.GetBody()!;

        MimeEntity wrapper = new EmailEditor().WrapInMultipartWithMessageAndSubtype(message, "mixed");

        Assert.That(message.GetBody(), Is.SameAs(wrapper));
        Assert.That(wrapper.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(wrapper.GetChildren(), Is.EqualTo(new[] { original }));
        Assert.That(original!.GetParent(), Is.SameAs(wrapper));
        Assert.That(original.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));

        MimeEntity unwrapped = new EmailEditor().UnwrapMultipartWithMessage(message);

        Assert.That(unwrapped, Is.SameAs(original));
        Assert.That(message.GetBody(), Is.SameAs(original));
        Assert.That(original.HasParent(), Is.False);
        Assert.That(wrapper.GetChildCount(), Is.Zero);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void IndexedPartWrapAndUnwrapDirtyOnlyTheChangedBranchAndOwner()
    {
        EmailMessage message = ParseMultipartMessage();
        MimeEntity root = message.GetBody()!;
        MimeEntity first = root!.GetChildAt(0);
        MimeEntity second = root.GetChildAt(1);

        MimeEntity wrapper = new EmailEditor().WrapInMultipartWithParentAndPartIndexAndSubtype(root, 1, "alternative");

        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, wrapper }));
        Assert.That(wrapper.GetChildren(), Is.EqualTo(new[] { second }));
        Assert.That(wrapper.GetParent(), Is.SameAs(root));
        Assert.That(second.GetParent(), Is.SameAs(wrapper));
        Assert.That(first.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(second.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(wrapper.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));

        MimeEntity unwrapped = new EmailEditor().UnwrapMultipartWithParentAndPartIndex(root, 1);

        Assert.That(unwrapped, Is.SameAs(second));
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.That(second.GetParent(), Is.SameAs(root));
        Assert.That(wrapper.HasParent(), Is.False);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void PartOperationsPreserveOrderOwnershipAndCanRestoreCleanState()
    {
        EmailMessage message = ParseMultipartMessage();
        MimeEntity root = message.GetBody()!;
        MimeEntity first = root!.GetChildAt(0);
        MimeEntity second = root.GetChildAt(1);
        MimeEntity added = new EmailEntityFactory().CreateTextWithValue("added");

        Assert.That(new EmailEditor().AddPart(root, added), Is.SameAs(added));
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, second, added }));
        Assert.That(added.GetParent(), Is.SameAs(root));
        Assert.That(message.IsSemanticDirty(), Is.True);

        Assert.That(new EmailEditor().MovePart(root, 2, 1), Is.SameAs(added));
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, added, second }));
        Assert.That(new EmailEditor().MovePart(root, 1, 2), Is.SameAs(added));
        Assert.That(new EmailEditor().RemovePartAt(root, 2), Is.SameAs(added));
        Assert.That(added.HasParent(), Is.False);
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        MimeEntity inserted = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("inserted", "html", "us-ascii");
        Assert.That(new EmailEditor().InsertPart(root, 1, inserted), Is.SameAs(inserted));
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, inserted, second }));
        Assert.That(new EmailEditor().RemovePartAt(root, 1), Is.SameAs(inserted));
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        MimeEntity replacement = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1, 2, 3 });
        Assert.That(new EmailEditor().ReplacePartAt(root, 0, replacement), Is.SameAs(first));
        Assert.That(first.HasParent(), Is.False);
        Assert.That(replacement.GetParent(), Is.SameAs(root));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(new EmailEditor().ReplacePartAt(root, 0, first), Is.SameAs(replacement));
        Assert.That(replacement.HasParent(), Is.False);
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        Assert.That(new EmailEditor().MovePart(root, 0, 0), Is.SameAs(first));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void InvalidStructuralEditsAreRejectedWithoutPartialMutation()
    {
        EmailMessage empty = new EmailMessageOperations().CreateWith();
        EmailMessage message = ParseMultipartMessage();
        MimeEntity root = message.GetBody()!;
        MimeEntity first = root!.GetChildAt(0);
        MimeEntity second = root.GetChildAt(1);
        MimeEntity other = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("owned") });
        MimeEntity owned = other.GetChildAt(0);

        Assert.Throws<InvalidOperationException>(() => new EmailEditor().WrapInMultipartWithMessageAndSubtype(empty, "mixed"));
        Assert.Throws<ArgumentException>(() => new EmailEditor().WrapInMultipartWithParentAndPartIndexAndSubtype(root, 0, "bad subtype"));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().UnwrapMultipartWithMessage(message));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().UnwrapMultipartWithParentAndPartIndex(root, 0));
        Assert.Throws<ArgumentException>(() => new EmailEditor().AddPart(root, owned));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().AddPart(first, new EmailEntityFactory().CreateTextWithValue("invalid")));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEditor().RemovePartAt(root, 2));

        Assert.That(root.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.That(first.GetParent(), Is.SameAs(root));
        Assert.That(second.GetParent(), Is.SameAs(root));
        Assert.That(owned.GetParent(), Is.SameAs(other));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void FixedAritySecurityContainersAllowReplacementButRejectCountChanges()
    {
        MimeEntity content = new EmailEntityFactory().CreateTextWithValue("signed body");
        MimeEntity signature = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-signature");
        MimeEntity signed = SecurityContainer(MimeEntityKind.Signed, "signed", content, signature);
        MimeEntity replacement = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 2 }, "application", "pkcs7-signature");
        EmailMessage signedMessage = new EmailMessageOperations().CreateWith();
        signedMessage.SetBody(signed);

        Assert.Throws<InvalidOperationException>(() => new EmailEditor().AddPart(signed, new EmailEntityFactory().CreateTextWithValue("third")));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().RemovePartAt(signed, 0));
        Assert.Throws<InvalidOperationException>(() => new EmailEditor().UnwrapMultipartWithMessage(signedMessage));

        MimeEntity removed = new EmailEditor().ReplacePartAt(signed, 1, replacement);

        Assert.That(removed, Is.SameAs(signature));
        Assert.That(removed.HasParent(), Is.False);
        Assert.That(signed.GetChildren(), Is.EqualTo(new[] { content, replacement }));
        Assert.That(signed.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
    }

    static EmailMessage ParseMultipartMessage()
    {
        return ParseMessage(
            "Subject: parts\r\n"
            + "Content-Type: multipart/mixed; boundary=outer\r\n\r\n"
            + "--outer\r\nContent-Type: text/plain\r\n\r\nfirst\r\n"
            + "--outer\r\nContent-Type: text/html\r\n\r\n<p>second</p>\r\n"
            + "--outer--\r\n");
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static MimeEntity SecurityContainer(MimeEntityKind kind, string subtype, MimeEntity first, MimeEntity second)
    {
        EmailMediaType mediaType = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("multipart/" + subtype), EmailStructuredValueParseMode.Strict).GetValue();
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), mediaType, null, new List<MimeEntity> { first, second }, null);
    }
}
