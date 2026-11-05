
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeWalkerTests
{
    [Test]
    public void RelationshipSnapshotsUseDeterministicTreeOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("root");
        HtmlNode lead = document.CreateText("lead");
        HtmlNode first = document.CreateElement("first");
        HtmlNode separator = document.CreateComment("separator");
        HtmlNode second = document.CreateElement("second");
        HtmlNode tail = document.CreateText("tail");
        HtmlNode nested = document.CreateElement("nested");
        HtmlNode deep = document.CreateText("deep");
        HtmlNode secondChild = document.CreateElement("second-child");
        nested.InitializeChildrenStorage(new[] { deep });
        first.InitializeChildrenStorage(new[] { nested });
        second.InitializeChildrenStorage(new[] { secondChild });
        root.InitializeChildrenStorage(new[]
        {
            lead, first, separator, second, tail
        });

        HtmlNode[] siblings = second.GetSiblings();
        HtmlNode[] elementSiblings = second.GetElementSiblings();
        HtmlNode[] ancestors = deep.GetAncestors();
        HtmlNode[] descendants = root.GetDescendants();
        siblings[0] = second;
        ancestors[0] = root;
        descendants[0] = root;

        Assert.Multiple(() =>
        {
            Assert.That(second.GetSiblings(), Is.EqualTo(new[]
            {
                lead, first, separator, tail
            }));
            Assert.That(elementSiblings, Is.EqualTo(new[] { first }));
            Assert.That(first.GetPreviousElementSibling(), Is.Null);
            Assert.That(first.GetNextElementSibling(), Is.SameAs(second));
            Assert.That(second.GetPreviousElementSibling(), Is.SameAs(first));
            Assert.That(second.GetNextElementSibling(), Is.Null);
            Assert.That(deep.GetAncestors(), Is.EqualTo(new[]
            {
                nested, first, root
            }));
            Assert.That(root.GetDescendants(), Is.EqualTo(new[]
            {
                lead, first, nested, deep, separator, second, secondChild, tail
            }));
            Assert.That(deep.GetRoot(), Is.SameAs(root));
            Assert.That(root.GetRoot(), Is.SameAs(root));
            Assert.That(deep.GetDepth(), Is.EqualTo(3));
            Assert.That(root.GetDepth(), Is.Zero);
            Assert.That(root.Contains(root), Is.True);
            Assert.That(root.Contains(deep), Is.True);
            Assert.That(first.Contains(deep), Is.True);
            Assert.That(deep.Contains(first), Is.False);
            Assert.That(second.Contains(deep), Is.False);
        });

        Assert.Throws<ArgumentNullException>(() => root.Contains(null));
        Assert.That(document.GetMutationVersionStorage(), Is.Zero);
    }

    [Test]
    public void DepthFirstWalkReportsBalancedEnterAndExitPhases()
    {
        TreeFixture tree = CreateWalkTree();
        List<string> events = new();

        HtmlTreeWalkResult result = tree.Root.WalkDepthFirst(
            (node, phase, depth) =>
            {
                string marker = phase == HtmlTreeVisitPhase.Enter ? "+" : "-";
                events.Add(marker + node.GetName() + ":" + depth);
                return true;
            });

        Assert.Multiple(() =>
        {
            Assert.That(events, Is.EqualTo(new[]
            {
                "+root:0", "+a:1", "+c:2", "-c:2", "-a:1",
                "+b:1", "-b:1", "-root:0"
            }));
            Assert.That(result.GetStatus(),
                Is.EqualTo(HtmlTreeWalkStatus.Completed));
            Assert.That(result.GetVisitCount(), Is.EqualTo(4));
            Assert.That(result.IsCompleted(), Is.True);
            Assert.That(result.WasStopped(), Is.False);
            Assert.That(result.WasLimitExceeded(), Is.False);
        });
    }

    [Test]
    public void DepthFirstExitCallbackCanStopBeforeLaterSiblings()
    {
        TreeFixture tree = CreateWalkTree();
        List<string> events = new();

        HtmlTreeWalkResult result = tree.Root.WalkDepthFirst(
            (node, phase, depth) =>
            {
                events.Add(node.GetName() + ":" + phase);
                return node != tree.A || phase != HtmlTreeVisitPhase.Exit;
            });

        Assert.Multiple(() =>
        {
            Assert.That(events, Is.EqualTo(new[]
            {
                "root:Enter", "a:Enter", "c:Enter", "c:Exit", "a:Exit"
            }));
            Assert.That(result.WasStopped(), Is.True);
            Assert.That(result.GetVisitCount(), Is.EqualTo(3));
            Assert.That(events.Any(value => value.StartsWith("b:")), Is.False);
        });
    }

    [Test]
    public void BreadthFirstWalkUsesASnapshotTakenBeforeCallbacks()
    {
        TreeFixture tree = CreateWalkTree();
        List<string> visited = new();

        HtmlTreeWalkResult result = tree.Root.WalkBreadthFirst((node, depth) =>
        {
            visited.Add(node.GetName() + ":" + depth);
            if (node == tree.Root)
            {
                tree.Root.Empty();
            }

            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(visited, Is.EqualTo(new[]
            {
                "root:0", "a:1", "b:1", "c:2"
            }));
            Assert.That(result.IsCompleted(), Is.True);
            Assert.That(result.GetVisitCount(), Is.EqualTo(4));
            Assert.That(tree.Root.GetChildren(), Is.Empty);
            Assert.That(tree.A.GetParent(), Is.Null);
            Assert.That(tree.B.GetParent(), Is.Null);
        });
    }

    [Test]
    public void BreadthFirstCallbackCanStopEarly()
    {
        TreeFixture tree = CreateWalkTree();
        List<HtmlNode> visited = new();

        HtmlTreeWalkResult result = tree.Root.WalkBreadthFirst((node, depth) =>
        {
            visited.Add(node);
            return node != tree.B;
        });

        Assert.Multiple(() =>
        {
            Assert.That(visited, Is.EqualTo(new[]
            {
                tree.Root, tree.A, tree.B
            }));
            Assert.That(result.GetStatus(), Is.EqualTo(HtmlTreeWalkStatus.Stopped));
            Assert.That(result.GetVisitCount(), Is.EqualTo(3));
            Assert.That(result.WasStopped(), Is.True);
        });
    }

    [Test]
    public void WalkLimitsStopBeforeAnAdditionalNodeVisit()
    {
        TreeFixture depthTree = CreateWalkTree();
        List<string> depthEvents = new();
        HtmlTreeWalkResult depth = depthTree.Root.WalkDepthFirstWithLimit(
            (node, phase, nodeDepth) =>
            {
                depthEvents.Add((phase == HtmlTreeVisitPhase.Enter ? "+" : "-")
                    + node.GetName());
                return true;
            }, 3);

        TreeFixture breadthTree = CreateWalkTree();
        List<string> breadthNodes = new();
        HtmlTreeWalkResult breadth = breadthTree.Root.WalkBreadthFirstWithLimit(
            (node, nodeDepth) =>
            {
                breadthNodes.Add(node.GetName());
                return true;
            }, 3);

        Assert.Multiple(() =>
        {
            Assert.That(depthEvents, Is.EqualTo(new[]
            {
                "+root", "+a", "+c", "-c", "-a", "-root"
            }));
            Assert.That(depth.WasLimitExceeded(), Is.True);
            Assert.That(depth.GetVisitCount(), Is.EqualTo(3));
            Assert.That(breadthNodes, Is.EqualTo(new[] { "root", "a", "b" }));
            Assert.That(breadth.WasLimitExceeded(), Is.True);
            Assert.That(breadth.GetVisitCount(), Is.EqualTo(3));
        });

        HtmlTreeWalkResult exact = CreateWalkTree().Root.WalkDepthFirstWithLimit(
            (node, phase, nodeDepth) => true, 4);
        Assert.That(exact.IsCompleted(), Is.True);
    }

    [Test]
    public void WalkArgumentsRejectNullCallbacksAndNonPositiveLimits()
    {
        HtmlNode root = CreateWalkTree().Root;

        Assert.Throws<ArgumentNullException>(() => root.WalkDepthFirst(
            (Func<HtmlNode, HtmlTreeVisitPhase, int, bool>)null));
        Assert.Throws<ArgumentNullException>(() => root.WalkBreadthFirst(
            (Func<HtmlNode, int, bool>)null));
        Assert.Throws<ArgumentOutOfRangeException>(() => root.WalkDepthFirstWithLimit(
            (node, phase, depth) => true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => root.WalkBreadthFirstWithLimit(
            (node, depth) => true, -1));
    }

    static TreeFixture CreateWalkTree()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = document.CreateElement("root");
        HtmlNode a = document.CreateElement("a");
        HtmlNode b = document.CreateElement("b");
        HtmlNode c = document.CreateElement("c");
        a.InitializeChildrenStorage(new[] { c });
        root.InitializeChildrenStorage(new[] { a, b });
        return new TreeFixture(document, root, a, b, c);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    class TreeFixture
    {
        public HtmlDocument Document { get; }
        public HtmlNode Root { get; }
        public HtmlNode A { get; }
        public HtmlNode B { get; }
        public HtmlNode C { get; }

        public TreeFixture(HtmlDocument document, HtmlNode root, HtmlNode a,
            HtmlNode b, HtmlNode c)
        {
            Document = document;
            Root = root;
            A = a;
            B = b;
            C = c;
        }
    }
}
