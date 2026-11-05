using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTreeOperationsPublicTests
{
    [Test]
    public void ConstructorAndFacade_ExposePublicTreeOperations()
    {
        IMarkdownTreeOperations direct = new MarkdownTreeOperations();
        IMarkdownDataServices services = new Markdown();
        IMarkdownTreeOperations fromFacade = services.CreateTreeOperations();
        MarkdownDocument document = Markdown.Parse("text");

        Assert.That(direct.FindFirstInDocument(document, MarkdownNodeKind.Paragraph), Is.Not.Null);
        Assert.That(fromFacade.FindFirstInDocument(document, MarkdownNodeKind.Paragraph), Is.Not.Null);
        Assert.That(fromFacade, Is.Not.SameAs(services.CreateTreeOperations()));
    }

    [Test]
    public void PublicCloneOperations_DeepCopyNodesDocumentsAndDiagnostics()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("# Heading\n\nText *emphasis*.");
        MarkdownNode heading = operations.FindFirstInDocument(document, MarkdownNodeKind.Heading)!;
        IMarkdownNodeOperations headingOperations = new MarkdownNodeOperations(heading);
        headingOperations.SetAttribute("id", "heading");
        headingOperations.AddDiagnosticValues(
            MarkdownDiagnosticSeverity.Warning,
            "NODE001",
            "node warning",
            heading.Span);
        document.Diagnostics.AddWarning("DOC001", "document warning", heading.Span);

        MarkdownNode headingClone = operations.CloneNode(heading);
        MarkdownDocument documentClone = operations.CloneDocument(document);

        headingOperations.SetAttribute("id", "changed");
        heading.Diagnostics.Items[0].Code = "changed";
        document.Diagnostics.Items[0].Code = "changed";

        Assert.That(headingClone, Is.Not.SameAs(heading));
        Assert.That(headingClone.Parent, Is.Null);
        Assert.That(new MarkdownNodeOperations(headingClone).GetAttributeValue("id"), Is.EqualTo("heading"));
        Assert.That(headingClone.Diagnostics.Items[0].Code, Is.EqualTo("NODE001"));
        Assert.That(documentClone.Root, Is.Not.SameAs(document.Root));
        Assert.That(documentClone.Root.Children[0].Parent, Is.SameAs(documentClone.Root));
        Assert.That(documentClone.Diagnostics.Items[0].Code, Is.EqualTo("DOC001"));
        Assert.That(documentClone.Root.Children[0].Diagnostics.Items[0].Code, Is.EqualTo("NODE001"));
    }

    [Test]
    public void KindSearch_IncludesRootAndReturnsStablePreorderMatches()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("# First\n\nText one.\n\n## Second\n\nText two.");
        MarkdownNode[] headings = operations.FindAllInDocument(document, MarkdownNodeKind.Heading);

        Assert.That(operations.FindFirstInDocument(document, MarkdownNodeKind.Document), Is.SameAs(document.Root));
        Assert.That(operations.FindFirstInNode(document.Root, MarkdownNodeKind.Heading), Is.SameAs(headings[0]));
        Assert.That(headings.Length, Is.EqualTo(2));
        Assert.That(headings[0].Literal, Is.EqualTo("First"));
        Assert.That(headings[1].Literal, Is.EqualTo("Second"));
        Assert.That(operations.FindAllInNode(headings[0], MarkdownNodeKind.Heading), Is.EqualTo(new[] { headings[0] }));
        Assert.That(operations.FindAllInDocument(document, MarkdownNodeKind.Table), Is.Empty);
        Assert.That(operations.FindFirstInDocument(document, MarkdownNodeKind.Table), Is.Null);

        headings[0] = new MarkdownNode();
        Assert.That(operations.FindFirstInDocument(document, MarkdownNodeKind.Heading)!.Literal, Is.EqualTo("First"));
    }

    [Test]
    public void Walk_UsesPreorderEnterAndPostorderLeaveContexts()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("Text *emphasis*.");
        List<string> enters = new List<string>();
        List<string> leaves = new List<string>();
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                enters.Add(
                    context.Depth.ToString()
                    + ":"
                    + context.ChildIndex.ToString()
                    + ":"
                    + context.Node.Kind.ToString());
                return MarkdownTreeWalkResult.Continue;
            },
            context => leaves.Add(context.Node.Kind.ToString()));

        MarkdownTreeWalkResult result = operations.WalkDocument(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(enters, Is.EqualTo(new[]
        {
            "0:-1:Document",
            "1:0:Paragraph",
            "2:0:Text",
            "2:1:Emphasis",
            "3:0:Text",
            "2:2:Text"
        }));
        Assert.That(leaves, Is.EqualTo(new[]
        {
            "Text",
            "Text",
            "Emphasis",
            "Text",
            "Paragraph",
            "Document"
        }));
    }

    [Test]
    public void Walk_ContinueVisitsAllNodes()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("Text *emphasis*.");
        int enterCount = 0;
        int leaveCount = 0;
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                enterCount = enterCount + 1;
                return MarkdownTreeWalkResult.Continue;
            },
            context => leaveCount = leaveCount + 1);

        MarkdownTreeWalkResult result = operations.WalkNode(document.Root, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(enterCount, Is.EqualTo(6));
        Assert.That(leaveCount, Is.EqualTo(6));
    }

    [Test]
    public void Walk_SkipChildrenLeavesCurrentNodeAndContinuesSiblings()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("First paragraph.\n\nSecond paragraph.");
        List<string> events = new List<string>();
        MarkdownNode firstParagraph = document.Root.Children[0];
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                events.Add("enter:" + context.Node.Kind.ToString() + ":" + context.ChildIndex.ToString());
                if (context.Node == firstParagraph)
                {
                    return MarkdownTreeWalkResult.SkipChildren;
                }

                return MarkdownTreeWalkResult.Continue;
            },
            context => events.Add("leave:" + context.Node.Kind.ToString() + ":" + context.ChildIndex.ToString()));

        MarkdownTreeWalkResult result = operations.WalkDocument(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(events, Does.Contain("leave:Paragraph:0"));
        Assert.That(events, Does.Contain("enter:Paragraph:1"));
        Assert.That(events.FindAll(item => item == "enter:Text:0").Count, Is.EqualTo(1));
    }

    [Test]
    public void Walk_StopHaltsWithoutLeaveCallbacks()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownDocument document = Markdown.Parse("First.\n\nSecond.");
        List<string> events = new List<string>();
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                events.Add("enter:" + context.Node.Kind.ToString());
                if (context.Node.Kind == MarkdownNodeKind.Paragraph)
                {
                    return MarkdownTreeWalkResult.Stop;
                }

                return MarkdownTreeWalkResult.Continue;
            },
            context => events.Add("leave:" + context.Node.Kind.ToString()));

        MarkdownTreeWalkResult result = operations.WalkDocument(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Stop));
        Assert.That(events, Is.EqualTo(new[] { "enter:Document", "enter:Paragraph" }));
    }

    [Test]
    public void CallbackAdapter_AllowsMissingCallbacksAndRejectsMissingContexts()
    {
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(null, null);
        MarkdownVisitContext context = new MarkdownVisitContext(new MarkdownNode(), null, 0, -1);

        Assert.That(visitor.VisitEnter(context), Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(() => visitor.VisitLeave(context), Throws.Nothing);
        Assert.That(() => visitor.VisitEnter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => visitor.VisitLeave(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void PublicTreeOperations_RejectMissingInputs()
    {
        IMarkdownTreeOperations operations = new MarkdownTreeOperations();
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(null, null);

        Assert.That(() => operations.CloneNode(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.CloneDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.FindFirstInNode(null!, MarkdownNodeKind.Text), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.FindFirstInDocument(null!, MarkdownNodeKind.Text), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.FindAllInNode(null!, MarkdownNodeKind.Text), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.FindAllInDocument(null!, MarkdownNodeKind.Text), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.WalkNode(null!, visitor), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.WalkDocument(null!, visitor), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.WalkNode(new MarkdownNode(), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.WalkDocument(new MarkdownDocument(), null!), Throws.TypeOf<ArgumentNullException>());
    }
}
