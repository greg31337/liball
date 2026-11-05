using System.Text;
using email;

namespace email.Tests;

public class EmailAttachmentFinderTests
{
    [Test]
    public void EnumerateClassifiesComplexTreeInDepthFirstOrder()
    {
        AttachmentTree tree = CreateTree();

        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithMessage(tree.Message);

        Assert.That(Entities(views), Is.EqualTo(new MimeEntity[]
        {
            tree.Plain,
            tree.Html,
            tree.InlineImage,
            tree.Pdf,
            tree.SignedBody,
            tree.Signature,
            tree.EncryptionControl,
            tree.EncryptedPayload,
            tree.NestedMessageEntity,
            tree.NestedBody,
            tree.NestedAttachment
        }));
        Assert.That(Kinds(views), Is.EqualTo(new[]
        {
            EmailAttachmentKind.Body,
            EmailAttachmentKind.Body,
            EmailAttachmentKind.InlineResource,
            EmailAttachmentKind.Attachment,
            EmailAttachmentKind.Attachment,
            EmailAttachmentKind.Signature,
            EmailAttachmentKind.EncryptionControl,
            EmailAttachmentKind.EncryptedPayload,
            EmailAttachmentKind.NestedMessage,
            EmailAttachmentKind.Body,
            EmailAttachmentKind.Attachment
        }));
    }

    [Test]
    public void ViewsExposePathsAndDecodedFilenameMetadata()
    {
        AttachmentTree tree = CreateTree();
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRoot(tree.Root);

        EmailAttachmentView inline = ViewFor(views, tree.InlineImage);
        EmailAttachmentView pdf = ViewFor(views, tree.Pdf);
        EmailAttachmentView nestedFile = ViewFor(views, tree.NestedAttachment);

        Assert.That(inline.GetPath().GetDisplayText(), Is.EqualTo("root/1/2"));
        Assert.That(inline.IsInlineResource(), Is.True);
        Assert.That(inline.GetFilename(), Is.EqualTo("logo.png"));
        Assert.That(inline.HasContentId(), Is.True);
        Assert.That(inline.GetContentLocation(), Is.EqualTo("images/logo.png"));
        Assert.That(pdf.GetPath().GetDisplayText(), Is.EqualTo("root/2"));
        Assert.That(pdf.GetFilename(), Is.EqualTo("café.pdf"));
        Assert.That(pdf.GetMediaType()!.IsType("application", "pdf"), Is.True);
        Assert.That(pdf.GetDisposition()!.IsAttachment(), Is.True);
        Assert.That(nestedFile.GetPath().GetDisplayText(), Is.EqualTo("root/5/1/2"));
        Assert.That(nestedFile.GetFilename(), Is.EqualTo("notes.txt"));
    }

    [Test]
    public void FilterHelpersKeepBodiesResourcesCryptoAndDownloadsDistinct()
    {
        AttachmentTree tree = CreateTree();

        Assert.That(Entities(new EmailAttachmentFinder().GetBodiesWithRoot(tree.Root)), Is.EqualTo(new MimeEntity[] { tree.Plain, tree.Html, tree.NestedBody }));
        Assert.That(Entities(new EmailAttachmentFinder().GetInlineResourcesWithMessage(tree.Message)), Is.EqualTo(new MimeEntity[] { tree.InlineImage }));
        Assert.That(Entities(new EmailAttachmentFinder().GetCryptographicPartsWithRoot(tree.Root)), Is.EqualTo(new MimeEntity[] { tree.Signature, tree.EncryptionControl, tree.EncryptedPayload }));
        Assert.That(Entities(new EmailAttachmentFinder().GetNestedMessagesWithMessage(tree.Message)), Is.EqualTo(new MimeEntity[] { tree.NestedMessageEntity }));
        Assert.That(Entities(new EmailAttachmentFinder().FindWithRoot(tree.Root)), Is.EqualTo(new MimeEntity[] { tree.Pdf, tree.SignedBody, tree.NestedMessageEntity, tree.NestedAttachment }));
    }

    [Test]
    public void FilenameHeuristicOverridesTextBodyAndRelatedAttachmentContext()
    {
        MimeEntity namedText = ParseEntity("Content-Type: text/plain; name=download.txt\r\n\r\ntext");
        MimeEntity attachedResource = ParseEntity("Content-Type: image/png\r\nContent-Disposition: attachment; filename=image.png\r\nContent-ID: <image@example.test>\r\n\r\npng");
        MimeEntity html = Html("<p>body</p>");
        MimeEntity related = Multipart("related", new List<MimeEntity> { html, attachedResource });
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { namedText, related });

        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRoot(root);

        Assert.That(ViewFor(views, namedText).GetKind(), Is.EqualTo(EmailAttachmentKind.Attachment));
        Assert.That(ViewFor(views, attachedResource).GetKind(), Is.EqualTo(EmailAttachmentKind.Attachment));
        Assert.That(ViewFor(views, attachedResource).IsInlineResource(), Is.False);

        MimeEntity extensionDisposition = ParseEntity("Content-Type: application/octet-stream\r\nContent-Disposition: x-preview\r\n\r\ndata");
        Assert.That(new EmailAttachmentFinder().EnumerateWithRoot(extensionDisposition)[0].IsAttachment(), Is.True);
    }

    [Test]
    public void ExplicitMultipartAttachmentIsProjectedAsOneDownload()
    {
        MimeEntity bundle = ParseEntity(
            "Content-Type: multipart/x-bundle; boundary=x\r\n"
            + "Content-Disposition: attachment; filename=bundle.mime\r\n\r\n"
            + "--x\r\nContent-Type: text/plain\r\n\r\ninside\r\n--x--\r\n");

        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRoot(bundle);

        Assert.That(views.Count, Is.EqualTo(1));
        Assert.That(views[0].GetEntity(), Is.SameAs(bundle));
        Assert.That(views[0].IsDownloadable(), Is.True);
        Assert.That(views[0].GetFilename(), Is.EqualTo("bundle.mime"));
    }

    [Test]
    public void ProjectionAndReturnedListsAreIndependentSnapshots()
    {
        AttachmentTree tree = CreateTree();

        List<EmailAttachmentView> first = new EmailAttachmentFinder().EnumerateWithRoot(tree.Root);
        first.Clear();
        List<EmailAttachmentView> second = new EmailAttachmentFinder().EnumerateWithRoot(tree.Root);

        Assert.That(second.Count, Is.EqualTo(11));
        Assert.That(new EmailAttachmentFinder().FindWithRoot(tree.Root).Count, Is.EqualTo(4));
    }

    [Test]
    public void FinderEnforcesTraversalBoundsAndValidatesMessages()
    {
        AttachmentTree tree = CreateTree();

        EmailLimitException depth = Assert.Throws<EmailLimitException>(() => new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 2, 30));
        Assert.That(depth.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailAttachmentFinder().FindWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 5, 5));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxTraversalEntityCount"));

        Assert.Throws<ArgumentNullException>(() => new EmailAttachmentFinder().EnumerateWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailAttachmentFinder().FindWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentException>(() => new EmailAttachmentFinder().EnumerateWithMessage(new EmailMessageOperations().CreateWith()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, -1, 30));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 5, 0));
    }

    [Test]
    public void ViewValidatesConstructionAndReportsRolePredicates()
    {
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 });
        EmailPath path = new EmailPathOperations().CreateRoot();
        EmailAttachmentView signature = new EmailAttachmentView(entity, path, EmailAttachmentKind.Signature, null);

        Assert.That(signature.IsCryptographicPart(), Is.True);
        Assert.That(signature.IsDownloadable(), Is.False);
        Assert.That(signature.IsBody(), Is.False);
        Assert.That(signature.IsNestedMessage(), Is.False);
        Assert.That(signature.HasFilename(), Is.False);
        Assert.Throws<ArgumentNullException>(() => new EmailAttachmentView(null!, path, EmailAttachmentKind.Attachment, null));
        Assert.Throws<ArgumentNullException>(() => new EmailAttachmentView(entity, null!, EmailAttachmentKind.Attachment, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailAttachmentView(entity, path, (EmailAttachmentKind)99, null));
        Assert.Throws<ArgumentException>(() => new EmailAttachmentView(entity, path, EmailAttachmentKind.Attachment, ""));
    }

    static AttachmentTree CreateTree()
    {
        MimeEntity plain = Plain("plain");
        MimeEntity html = Html("<p>html</p>");
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { plain, html });
        MimeEntity inlineImage = ParseEntity("Content-Type: image/png; name=logo.png\r\nContent-ID: <logo@example.test>\r\nContent-Location: images/logo.png\r\n\r\npng");
        MimeEntity related = Multipart("related", new List<MimeEntity> { alternative, inlineImage });
        MimeEntity pdf = ParseEntity("Content-Type: application/pdf\r\nContent-Disposition: attachment; filename*=iso-8859-1''caf%E9.pdf\r\n\r\npdf");
        MimeEntity signedBody = Plain("signed body");
        MimeEntity signature = ParseEntity("Content-Type: application/pgp-signature; name=signature.asc\r\n\r\nsignature");
        MimeEntity signed = ProtocolContainer(MimeEntityKind.Signed, "signed", signedBody, signature);
        MimeEntity encryptionControl = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 1 }, "application", "pgp-encrypted");
        MimeEntity encryptedPayload = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[] { 2 }, "application", "octet-stream");
        MimeEntity encrypted = ProtocolContainer(MimeEntityKind.Encrypted, "encrypted", encryptionControl, encryptedPayload);
        MimeEntity nestedBody = Plain("nested body");
        MimeEntity nestedAttachment = ParseEntity("Content-Type: text/plain; name=notes.txt\r\n\r\nnotes");
        MimeEntity nestedRoot = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { nestedBody, nestedAttachment });
        EmailMessage nestedMessage = new EmailMessageOperations().CreateWith();
        nestedMessage.SetBody(nestedRoot);
        MimeEntity nestedMessageEntity = new EmailEntityFactory().CreateNestedMessage(nestedMessage);
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { related, pdf, signed, encrypted, nestedMessageEntity });
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(root);
        return new AttachmentTree(message, root, plain, html, inlineImage, pdf, signedBody, signature, encryptionControl, encryptedPayload, nestedMessageEntity, nestedBody, nestedAttachment);
    }

    static MimeEntity Plain(string value)
    {
        return new EmailEntityFactory().CreateTextWithValue(value);
    }

    static MimeEntity Html(string value)
    {
        return new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(value, "html", "us-ascii");
    }

    static MimeEntity Multipart(string subtype, List<MimeEntity> children)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), Media("multipart/" + subtype), null, children, null);
    }

    static MimeEntity ProtocolContainer(MimeEntityKind kind, string subtype, MimeEntity first, MimeEntity second)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(kind, new EmailHeadersOperations().CreateWith(), Media("multipart/" + subtype), null, new List<MimeEntity> { first, second }, null);
    }

    static EmailMediaType Media(string value)
    {
        return new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailStructuredValueParseMode.Strict).GetValue();
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.ASCII.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }

    static List<MimeEntity> Entities(List<EmailAttachmentView> views)
    {
        return views.Select(view => view.GetEntity()).ToList();
    }

    static List<EmailAttachmentKind> Kinds(List<EmailAttachmentView> views)
    {
        return views.Select(view => view.GetKind()).ToList();
    }

    static EmailAttachmentView ViewFor(List<EmailAttachmentView> views, MimeEntity entity)
    {
        return views.Single(view => ReferenceEquals(view.GetEntity(), entity));
    }
}

class AttachmentTree
{
    public EmailMessage Message { get; }
    public MimeEntity Root { get; }
    public MimeEntity Plain { get; }
    public MimeEntity Html { get; }
    public MimeEntity InlineImage { get; }
    public MimeEntity Pdf { get; }
    public MimeEntity SignedBody { get; }
    public MimeEntity Signature { get; }
    public MimeEntity EncryptionControl { get; }
    public MimeEntity EncryptedPayload { get; }
    public MimeEntity NestedMessageEntity { get; }
    public MimeEntity NestedBody { get; }
    public MimeEntity NestedAttachment { get; }

    public AttachmentTree(EmailMessage message, MimeEntity root, MimeEntity plain, MimeEntity html, MimeEntity inlineImage, MimeEntity pdf, MimeEntity signedBody, MimeEntity signature, MimeEntity encryptionControl, MimeEntity encryptedPayload, MimeEntity nestedMessageEntity, MimeEntity nestedBody, MimeEntity nestedAttachment)
    {
        Message = message;
        Root = root;
        Plain = plain;
        Html = html;
        InlineImage = inlineImage;
        Pdf = pdf;
        SignedBody = signedBody;
        Signature = signature;
        EncryptionControl = encryptionControl;
        EncryptedPayload = encryptedPayload;
        NestedMessageEntity = nestedMessageEntity;
        NestedBody = nestedBody;
        NestedAttachment = nestedAttachment;
    }
}
