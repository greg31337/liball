using email;

namespace email.Tests;

public class EmailTraversalTests
{
    [Test]
    public void DepthFirstReturnsPreorderIncludingNestedMessageBodies()
    {
        TraversalTree tree = CreateTree();

        List<MimeEntity> result = new EmailTraversal().DepthFirstWithMessage(tree.Message);

        Assert.That(result, Is.EqualTo(new MimeEntity[]
        {
            tree.Root,
            tree.First,
            tree.Branch,
            tree.BranchLeaf,
            tree.NestedMessageEntity,
            tree.NestedRoot,
            tree.DeepLeaf,
            tree.Last
        }));

        result.Clear();
        Assert.That(new EmailTraversal().DepthFirstWithRoot(tree.Root).Count, Is.EqualTo(8));
    }

    [Test]
    public void BreadthFirstReturnsLevelOrderAndPreservesChildOrder()
    {
        TraversalTree tree = CreateTree();

        List<MimeEntity> result = new EmailTraversal().BreadthFirstWithRoot(tree.Root);

        Assert.That(result, Is.EqualTo(new MimeEntity[]
        {
            tree.Root,
            tree.First,
            tree.Branch,
            tree.Last,
            tree.BranchLeaf,
            tree.NestedMessageEntity,
            tree.NestedRoot,
            tree.DeepLeaf
        }));
        Assert.That(
            new EmailTraversal().TraverseWithMessageAndModeAndMaximumDepthAndMaximumEntityCount(tree.Message, EmailTraversalMode.BreadthFirst, 4, 8),
            Is.EqualTo(result));
    }

    [Test]
    public void PathAndParentLookupCrossNestedMessageOwnershipBoundary()
    {
        TraversalTree tree = CreateTree();

        Assert.That(new EmailTraversal().GetPathWithRootAndTarget(tree.Root, tree.Root)!.GetDisplayText(), Is.EqualTo("root"));
        Assert.That(new EmailTraversal().GetPathWithRootAndTarget(tree.Root, tree.BranchLeaf)!.GetDisplayText(), Is.EqualTo("root/2/1"));
        Assert.That(new EmailTraversal().GetPathWithMessageAndTarget(tree.Message, tree.NestedRoot)!.GetDisplayText(), Is.EqualTo("root/2/2/1"));
        Assert.That(new EmailTraversal().GetPathWithRootAndTarget(tree.Root, tree.DeepLeaf)!.GetDisplayText(), Is.EqualTo("root/2/2/1/1"));

        Assert.That(new EmailTraversal().GetParentWithRootAndTarget(tree.Root, tree.Root), Is.Null);
        Assert.That(new EmailTraversal().GetParentWithRootAndTarget(tree.Root, tree.BranchLeaf), Is.SameAs(tree.Branch));
        Assert.That(tree.NestedRoot.HasParent(), Is.False);
        Assert.That(new EmailTraversal().GetParentWithMessageAndTarget(tree.Message, tree.NestedRoot), Is.SameAs(tree.NestedMessageEntity));
        Assert.That(new EmailTraversal().GetParentWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(tree.Root, tree.DeepLeaf, 4, 8), Is.SameAs(tree.NestedRoot));
    }

    [Test]
    public void GetAtPathRoundTripsEveryTraversalEntityAndRejectsMissingChildren()
    {
        TraversalTree tree = CreateTree();
        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRoot(tree.Root);

        foreach (MimeEntity entity in entities)
        {
            EmailPath path = new EmailTraversal().GetPathWithRootAndTarget(tree.Root, entity)!;
            Assert.That(new EmailTraversal().GetAtPathWithRootAndPath(tree.Root, path!), Is.SameAs(entity));
        }

        Assert.That(new EmailTraversal().GetAtPathWithMessageAndPathAndMaximumDepth(tree.Message, new EmailPathOperations().CreateWithSegments(new List<int> { 1, 1, 0, 0 }), 4), Is.SameAs(tree.DeepLeaf));
        Assert.That(new EmailTraversal().GetAtPathWithRootAndPath(tree.Root, new EmailPathOperations().CreateWithSegments(new List<int> { 9 })), Is.Null);
        Assert.That(new EmailTraversal().GetAtPathWithRootAndPath(tree.Root, new EmailPathOperations().CreateWithSegments(new List<int> { 0, 0 })), Is.Null);
    }

    [Test]
    public void LookupReturnsNullForEntityOutsideTheTraversalRoot()
    {
        TraversalTree tree = CreateTree();
        MimeEntity unrelated = new EmailEntityFactory().CreateTextWithValue("unrelated");

        Assert.That(new EmailTraversal().GetPathWithRootAndTarget(tree.Root, unrelated), Is.Null);
        Assert.That(new EmailTraversal().GetParentWithRootAndTarget(tree.Root, unrelated), Is.Null);
    }

    [Test]
    public void TraversalEnforcesDepthAndEntityCountWithoutTruncation()
    {
        TraversalTree tree = CreateTree();

        Assert.That(new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 4, 8).Count, Is.EqualTo(8));
        Assert.That(new EmailTraversal().BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 4, 8).Count, Is.EqualTo(8));

        EmailLimitException depth = Assert.Throws<EmailLimitException>(() => new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 3, 8));
        Assert.That(depth.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
        Assert.That(depth.GetLimit(), Is.EqualTo(3));
        Assert.That(depth.GetObserved(), Is.EqualTo(4));

        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailTraversal().BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(tree.Root, 4, 7));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxTraversalEntityCount"));
        Assert.That(count.GetLimit(), Is.EqualTo(7));
        Assert.That(count.GetObserved(), Is.EqualTo(8));

        EmailLimitException path = Assert.Throws<EmailLimitException>(() => new EmailTraversal().GetAtPathWithRootAndPathAndMaximumDepth(tree.Root, new EmailPathOperations().CreateWithSegments(new List<int> { 1, 1, 0, 0 }), 3));
        Assert.That(path.GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
    }

    [Test]
    public void RootOnlyTraversalAcceptsTightestValidBounds()
    {
        MimeEntity root = new EmailEntityFactory().CreateTextWithValue("root");

        Assert.That(new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, 0, 1), Is.EqualTo(new MimeEntity[] { root }));
        Assert.That(new EmailTraversal().BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, 0, 1), Is.EqualTo(new MimeEntity[] { root }));
    }

    [Test]
    public void PublicMethodsValidateRootsTargetsModesAndBounds()
    {
        MimeEntity root = new EmailEntityFactory().CreateTextWithValue("root");
        EmailMessage empty = new EmailMessageOperations().CreateWith();

        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().DepthFirstWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().BreadthFirstWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentException>(() => new EmailTraversal().DepthFirstWithMessage(empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTraversal().TraverseWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, (EmailTraversalMode)2, 0, 1));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().GetAtPathWithRootAndPath((MimeEntity)null!, new EmailPathOperations().CreateRoot()));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().GetAtPathWithRootAndPath(root, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailTraversal().GetPathWithRootAndTarget(root, null!));
    }

    static TraversalTree CreateTree()
    {
        MimeEntity deepLeaf = new EmailEntityFactory().CreateTextWithValue("deep");
        MimeEntity nestedRoot = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { deepLeaf });
        EmailMessage nestedMessage = new EmailMessageOperations().CreateWith();
        nestedMessage.SetBody(nestedRoot);
        MimeEntity nestedMessageEntity = new EmailEntityFactory().CreateNestedMessage(nestedMessage);
        MimeEntity branchLeaf = new EmailEntityFactory().CreateTextWithValue("branch");
        MimeEntity branch = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { branchLeaf, nestedMessageEntity });
        MimeEntity first = new EmailEntityFactory().CreateTextWithValue("first");
        MimeEntity last = new EmailEntityFactory().CreateTextWithValue("last");
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { first, branch, last });
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(root);
        return new TraversalTree(message, root, first, branch, branchLeaf, nestedMessageEntity, nestedRoot, deepLeaf, last);
    }
}

class TraversalTree
{
    public EmailMessage Message { get; }
    public MimeEntity Root { get; }
    public MimeEntity First { get; }
    public MimeEntity Branch { get; }
    public MimeEntity BranchLeaf { get; }
    public MimeEntity NestedMessageEntity { get; }
    public MimeEntity NestedRoot { get; }
    public MimeEntity DeepLeaf { get; }
    public MimeEntity Last { get; }

    public TraversalTree(EmailMessage message, MimeEntity root, MimeEntity first, MimeEntity branch, MimeEntity branchLeaf, MimeEntity nestedMessageEntity, MimeEntity nestedRoot, MimeEntity deepLeaf, MimeEntity last)
    {
        Message = message;
        Root = root;
        First = first;
        Branch = branch;
        BranchLeaf = branchLeaf;
        NestedMessageEntity = nestedMessageEntity;
        NestedRoot = nestedRoot;
        DeepLeaf = deepLeaf;
        Last = last;
    }
}
