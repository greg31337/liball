using System.Text;
using email;

namespace email.Tests;

public class EmailTraversalSearchTests
{
    [Test]
    public void MediaTypeSearchIsCaseInsensitiveAndUsesDepthFirstOrder()
    {
        SearchTree tree = CreateTree();

        List<MimeEntity> matches = new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(tree.Root, "APPLICATION", "pdf");

        Assert.That(matches, Is.EqualTo(new MimeEntity[] { tree.First, tree.Last }));
        Assert.That(new EmailTraversal().FindByMediaTypeWithMessageAndTopLevelTypeAndSubtype(tree.Message, "image", "PNG"), Is.EqualTo(new MimeEntity[] { tree.Nested }));

        matches.Clear();
        Assert.That(new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(tree.Root, "application", "pdf").Count, Is.EqualTo(2));
    }

    [Test]
    public void DispositionAndKindSearchIncludeNestedMessageStructures()
    {
        SearchTree tree = CreateTree();

        Assert.That(new EmailTraversal().FindByDispositionWithMessageAndDispositionType(tree.Message, "ATTACHMENT"), Is.EqualTo(new MimeEntity[] { tree.First, tree.Last }));
        Assert.That(new EmailTraversal().FindByDispositionWithRootAndDispositionType(tree.Root, "inline"), Is.EqualTo(new MimeEntity[] { tree.Nested }));
        Assert.That(new EmailTraversal().FindByKindWithRootAndKind(tree.Root, MimeEntityKind.Multipart), Is.EqualTo(new MimeEntity[] { tree.Root }));
        Assert.That(new EmailTraversal().FindByKindWithRootAndKind(tree.Root, MimeEntityKind.Message), Is.EqualTo(new MimeEntity[] { tree.NestedMessageEntity }));
        Assert.That(new EmailTraversal().FindByKindWithMessageAndKind(tree.Message, MimeEntityKind.Leaf), Is.EqualTo(new MimeEntity[] { tree.First, tree.Nested, tree.Last }));
    }

    [Test]
    public void FilenameSearchUsesDispositionThenMediaTypeFallbackAndCharset()
    {
        SearchTree tree = CreateTree();

        Assert.That(new EmailTraversal().FindByFilenameWithRootAndFilename(tree.Root, "report.pdf"), Is.EqualTo(new MimeEntity[] { tree.First }));
        Assert.That(new EmailTraversal().FindByFilenameWithRootAndFilename(tree.Root, "café.png"), Is.EqualTo(new MimeEntity[] { tree.Nested }));
        Assert.That(new EmailTraversal().FindByFilenameWithRootAndFilename(tree.Root, "ignored.pdf"), Is.Empty);
        Assert.That(new EmailTraversal().FindByFilenameWithRootAndFilename(tree.Root, "REPORT.PDF"), Is.Empty);
    }

    [Test]
    public void ContentIdSearchAcceptsBracketedOrBareIdsAndFindsDuplicates()
    {
        SearchTree tree = CreateTree();

        Assert.That(new EmailTraversal().FindByContentIdWithRootAndContentId(tree.Root, "report@EXAMPLE.TEST"), Is.EqualTo(new MimeEntity[] { tree.First, tree.Last }));
        Assert.That(new EmailTraversal().FindByContentIdWithMessageAndContentId(tree.Message, "<logo@example.test>"), Is.EqualTo(new MimeEntity[] { tree.Nested }));
        Assert.That(new EmailTraversal().FindByContentIdWithRootAndContentId(tree.Root, "REPORT@example.test"), Is.Empty);
    }

    [Test]
    public void ContentLocationSearchIsExactAndReturnsAllMatches()
    {
        SearchTree tree = CreateTree();

        Assert.That(new EmailTraversal().FindByContentLocationWithRootAndContentLocation(tree.Root, "shared/report"), Is.EqualTo(new MimeEntity[] { tree.First, tree.Last }));
        Assert.That(new EmailTraversal().FindByContentLocationWithMessageAndContentLocation(tree.Message, "images/logo.png"), Is.EqualTo(new MimeEntity[] { tree.Nested }));
        Assert.That(new EmailTraversal().FindByContentLocationWithRootAndContentLocation(tree.Root, "SHARED/report"), Is.Empty);
    }

    [Test]
    public void SearchHelpersRetainTraversalDepthAndEntityCountBounds()
    {
        SearchTree tree = CreateTree();

        Assert.That(new EmailTraversal().FindByKindWithRootAndKindAndMaximumDepthAndMaximumEntityCount(tree.Root, MimeEntityKind.Leaf, 2, 5).Count, Is.EqualTo(3));

        EmailLimitException depth = Assert.Throws<EmailLimitException>(() => new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(tree.Root, "image", "png", 1, 5));
        Assert.That(depth.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
        Assert.That(depth.GetLimit(), Is.EqualTo(1));
        Assert.That(depth.GetObserved(), Is.EqualTo(2));

        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailTraversal().FindByDispositionWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(tree.Root, "attachment", 2, 4));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxTraversalEntityCount"));
        Assert.That(count.GetLimit(), Is.EqualTo(4));
        Assert.That(count.GetObserved(), Is.EqualTo(5));
    }

    [Test]
    public void SearchHelpersValidateCriteriaRootsKindsAndBounds()
    {
        SearchTree tree = CreateTree();

        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtype((MimeEntity)null!, "text", "plain"));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(tree.Root, null!, "plain"));
        Assert.Throws<ArgumentException>(() => new EmailTraversal().FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(tree.Root, "text", ""));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().FindByDispositionWithRootAndDispositionType(tree.Root, null!));
        Assert.Throws<ArgumentException>(() => new EmailTraversal().FindByFilenameWithRootAndFilename(tree.Root, ""));
        Assert.Throws<ArgumentException>(() => new EmailTraversal().FindByContentIdWithRootAndContentId(tree.Root, "not-an-id"));
        Assert.Throws<ArgumentException>(() => new EmailTraversal().FindByContentIdWithRootAndContentId(tree.Root, "   "));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().FindByContentLocationWithRootAndContentLocation(tree.Root, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().FindByKindWithRootAndKind(tree.Root, (MimeEntityKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().FindByKindWithRootAndKindAndMaximumDepthAndMaximumEntityCount(tree.Root, MimeEntityKind.Leaf, -1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().FindByKindWithRootAndKindAndMaximumDepthAndMaximumEntityCount(tree.Root, MimeEntityKind.Leaf, 2, 0));
    }

    static SearchTree CreateTree()
    {
        const string source = "Content-Type: multipart/mixed; boundary=outer\r\n\r\n"
            + "--outer\r\n"
            + "Content-Type: Application/Pdf; name=ignored.pdf\r\n"
            + "Content-Disposition: Attachment; filename=report.pdf\r\n"
            + "Content-ID: <report@Example.TEST>\r\n"
            + "Content-Location: shared/report\r\n\r\nfirst\r\n"
            + "--outer\r\n"
            + "Content-Type: message/rfc822\r\n\r\n"
            + "Content-Type: Image/PNG; name*=iso-8859-1''caf%E9.png\r\n"
            + "Content-Disposition: Inline\r\n"
            + "Content-ID: <logo@Example.TEST>\r\n"
            + "Content-Location: images/logo.png\r\n\r\nnested\r\n"
            + "--outer\r\n"
            + "Content-Type: application/pdf\r\n"
            + "Content-Disposition: attachment; filename=second.pdf\r\n"
            + "Content-ID: <report@example.test>\r\n"
            + "Content-Location: shared/report\r\n\r\nlast\r\n"
            + "--outer--\r\n";
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.ASCII.GetBytes(source));
        Assert.That(parsed.IsSuccess(), Is.True);
        MimeEntity root = parsed.GetEntity()!;
        MimeEntity first = root!.GetChildAt(0);
        MimeEntity nestedMessageEntity = root.GetChildAt(1);
        MimeEntity nested = nestedMessageEntity.GetNestedMessage()!.GetBody()!;
        MimeEntity last = root.GetChildAt(2);
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(root);
        return new SearchTree(message, root, first, nestedMessageEntity, nested!, last);
    }
}

class SearchTree
{
    public EmailMessage Message { get; }
    public MimeEntity Root { get; }
    public MimeEntity First { get; }
    public MimeEntity NestedMessageEntity { get; }
    public MimeEntity Nested { get; }
    public MimeEntity Last { get; }

    public SearchTree(EmailMessage message, MimeEntity root, MimeEntity first, MimeEntity nestedMessageEntity, MimeEntity nested, MimeEntity last)
    {
        Message = message;
        Root = root;
        First = first;
        NestedMessageEntity = nestedMessageEntity;
        Nested = nested;
        Last = last;
    }
}
