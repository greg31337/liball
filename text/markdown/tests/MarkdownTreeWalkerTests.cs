using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTreeWalkerTests
{
    [Test]
    public void Walk_VisitsNodesInPreorderAndLeavesInPostorder()
    {
        MarkdownDocument document = CreateSampleDocument();
        List<string> events = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            events.Add("enter:" + context.Depth.ToString() + ":" + context.ChildIndex.ToString() + ":" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.LeaveNode = context =>
        {
            events.Add("leave:" + context.Node.Kind.ToString());
        };

        MarkdownTreeWalkResult result = MarkdownTreeWalker.Walk(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(events, Is.EqualTo(new string[]
        {
            "enter:0:-1:Document",
            "enter:1:0:Paragraph",
            "enter:2:0:Text",
            "leave:Text",
            "enter:2:1:Emphasis",
            "enter:3:0:Text",
            "leave:Text",
            "leave:Emphasis",
            "leave:Paragraph",
            "leave:Document"
        }));
    }

    [Test]
    public void Walk_PopulatesParentAndRootContext()
    {
        MarkdownDocument document = CreateSampleDocument();
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode text = paragraph.GetChild(0);
        List<string> observations = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            if (context.Node == document.Root)
            {
                observations.Add(context.IsRoot().ToString());
                observations.Add((context.Parent == null).ToString());
            }
            else if (context.Node == text)
            {
                observations.Add(context.IsRoot().ToString());
                observations.Add((context.Parent == paragraph).ToString());
            }

            return MarkdownTreeWalkResult.Continue;
        };

        MarkdownTreeWalker.Walk(document, visitor);

        Assert.That(observations, Is.EqualTo(new string[]
        {
            "True",
            "True",
            "False",
            "True"
        }));
    }

    [Test]
    public void Visitor_DispatchesCategoryAndExtensionCallbacks()
    {
        MarkdownNode root = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode table = MarkdownNode.Create(MarkdownNodeKind.Table);
        MarkdownNode strike = MarkdownNode.Create(MarkdownNodeKind.Strikethrough);
        root.AppendChild(table);
        table.AppendChild(strike);
        List<string> events = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            events.Add("node:" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.EnterDocument = context =>
        {
            events.Add("document:" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.EnterBlock = context =>
        {
            events.Add("block:" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.EnterInline = context =>
        {
            events.Add("inline:" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.EnterExtension = context =>
        {
            events.Add("extension:" + context.Node.Kind.ToString());
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.LeaveExtension = context =>
        {
            events.Add("leave-extension:" + context.Node.Kind.ToString());
        };
        visitor.LeaveBlock = context =>
        {
            events.Add("leave-block:" + context.Node.Kind.ToString());
        };
        visitor.LeaveInline = context =>
        {
            events.Add("leave-inline:" + context.Node.Kind.ToString());
        };
        visitor.LeaveDocument = context =>
        {
            events.Add("leave-document:" + context.Node.Kind.ToString());
        };
        visitor.LeaveNode = context =>
        {
            events.Add("leave-node:" + context.Node.Kind.ToString());
        };

        MarkdownTreeWalker.Walk(root, visitor);

        Assert.That(events, Is.EqualTo(new string[]
        {
            "node:Document",
            "document:Document",
            "node:Table",
            "block:Table",
            "extension:Table",
            "node:Strikethrough",
            "inline:Strikethrough",
            "extension:Strikethrough",
            "leave-extension:Strikethrough",
            "leave-inline:Strikethrough",
            "leave-node:Strikethrough",
            "leave-extension:Table",
            "leave-block:Table",
            "leave-node:Table",
            "leave-document:Document",
            "leave-node:Document"
        }));
    }

    [Test]
    public void Walk_SkipChildrenLeavesNodeWithoutVisitingDescendants()
    {
        MarkdownDocument document = CreateSampleDocument();
        List<string> events = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            events.Add("enter:" + context.Node.Kind.ToString());
            if (context.Node.Kind == MarkdownNodeKind.Paragraph)
            {
                return MarkdownTreeWalkResult.SkipChildren;
            }

            return MarkdownTreeWalkResult.Continue;
        };
        visitor.LeaveNode = context =>
        {
            events.Add("leave:" + context.Node.Kind.ToString());
        };

        MarkdownTreeWalkResult result = MarkdownTreeWalker.Walk(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(events, Is.EqualTo(new string[]
        {
            "enter:Document",
            "enter:Paragraph",
            "leave:Paragraph",
            "leave:Document"
        }));
    }

    [Test]
    public void Walk_StopHaltsTraversal()
    {
        MarkdownDocument document = CreateSampleDocument();
        List<string> events = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            events.Add("enter:" + context.Node.Kind.ToString());
            if (context.Node.Kind == MarkdownNodeKind.Paragraph)
            {
                return MarkdownTreeWalkResult.Stop;
            }

            return MarkdownTreeWalkResult.Continue;
        };
        visitor.LeaveNode = context =>
        {
            events.Add("leave:" + context.Node.Kind.ToString());
        };

        MarkdownTreeWalkResult result = MarkdownTreeWalker.Walk(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Stop));
        Assert.That(events, Is.EqualTo(new string[]
        {
            "enter:Document",
            "enter:Paragraph"
        }));
    }

    [Test]
    public void VisitContext_ClassifiesCurrentNode()
    {
        MarkdownVisitContext document = new MarkdownVisitContext(MarkdownNode.Create(MarkdownNodeKind.Document), null, 0, -1);
        MarkdownVisitContext paragraph = new MarkdownVisitContext(MarkdownNode.Create(MarkdownNodeKind.Paragraph), null, 0, -1);
        MarkdownVisitContext text = new MarkdownVisitContext(MarkdownNode.Create(MarkdownNodeKind.Text), null, 0, -1);
        MarkdownVisitContext table = new MarkdownVisitContext(MarkdownNode.Create(MarkdownNodeKind.Table), null, 0, -1);

        Assert.That(document.IsDocument(), Is.True);
        Assert.That(document.IsBlock(), Is.False);
        Assert.That(paragraph.IsBlock(), Is.True);
        Assert.That(text.IsInline(), Is.True);
        Assert.That(table.IsExtension(), Is.True);
    }

    [Test]
    public void Visitor_AndWalkerRejectInvalidArguments()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        MarkdownVisitContext context = new MarkdownVisitContext(node, null, 0, -1);

        Assert.That(() => MarkdownTreeWalker.Walk((MarkdownDocument)null!, visitor), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTreeWalker.Walk(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTreeWalker.Walk((MarkdownNode)null!, visitor), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTreeWalker.Walk(node, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownVisitContext(null!, null, 0, -1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownVisitContext(node, null, -1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownVisitContext(node, null, 0, -2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => visitor.VisitEnter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => visitor.VisitLeave(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(visitor.VisitEnter(context), Is.EqualTo(MarkdownTreeWalkResult.Continue));
    }

    static MarkdownDocument CreateSampleDocument()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode emphasis = MarkdownNode.Create(MarkdownNodeKind.Emphasis);
        MarkdownNode emphasizedText = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("hello");
        emphasizedText.SetLiteral("world");
        emphasis.AppendChild(emphasizedText);
        paragraph.AppendChild(text);
        paragraph.AppendChild(emphasis);
        document.Root.AppendChild(paragraph);

        return document;
    }
}
