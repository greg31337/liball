using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicAstExhaustiveTests
{
    [Test]
    public void ParsedAst_ExposesTypedMetadataAndCategoryWrappersThroughPublicServices()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .MapEmojiShortcode("ship", "SHIP")
            .RegisterCustomContainer("panel")
            .RegisterCustomFencedBlock("chart")
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .Build();
        string source =
            "---\ntitle: Test\n---\n" +
            "# Heading\n\n" +
            "- [x] task\n\n" +
            "| A | B |\n| :--- | ---: |\n| x | y |\n\n" +
            "Term\n: Definition\n\n" +
            "*[HTML]: Hyper Text Markup Language\n\n" +
            "HTML ~~old~~ [link](/url) ![alt](/img) $x$ :ship: H^2^ H~2~ ++new++ ==mark== %%Ctrl%%\n\n" +
            "Text[^note].\n\n[^note]: note\n\n" +
            "```mermaid\ngraph TD\n```\n\n" +
            "```chart\na < b\n```\n\n" +
            "> [!NOTE]\n> Alert.\n\n" +
            "::: panel \"Panel\"\nInside.\n:::\n\n" +
            "??? details \"More\"\nContent.\n???";

        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        List<MarkdownNodeKind> visitedKinds = new List<MarkdownNodeKind>();
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                Assert.That(context.Node.Data.Kind, Is.EqualTo(context.Node.Kind));
                visitedKinds.Add(context.Node.Kind);
                return MarkdownTreeWalkResult.Continue;
            },
            null);

        Assert.That(trees.WalkDocument(document, visitor), Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(document.Diagnostics.Items, Is.Empty);
        Assert.That(visitedKinds, Does.Contain(MarkdownNodeKind.FrontMatter));
        Assert.That(Find(trees, document, MarkdownNodeKind.Heading).Data.Heading!.Level, Is.EqualTo(1));
        Assert.That(Find(trees, document, MarkdownNodeKind.TaskListItem).Data.TaskListItem!.IsChecked, Is.True);
        Assert.That(Find(trees, document, MarkdownNodeKind.Table).Data.Table!.ColumnCount, Is.EqualTo(2));
        Assert.That(Find(trees, document, MarkdownNodeKind.TableCell).Data.TableCell!.Alignment, Is.EqualTo("left"));
        Assert.That(Find(trees, document, MarkdownNodeKind.Link).Data.Link!.Destination, Is.EqualTo("/url"));
        Assert.That(Find(trees, document, MarkdownNodeKind.Image).Data.Image!.Destination, Is.EqualTo("/img"));
        Assert.That(Find(trees, document, MarkdownNodeKind.FootnoteReference).Data.Footnote!.IsResolved, Is.True);
        Assert.That(Find(trees, document, MarkdownNodeKind.Abbreviation).Data.Abbreviation!.Title, Is.EqualTo("Hyper Text Markup Language"));
        Assert.That(Find(trees, document, MarkdownNodeKind.MathSpan).Data.Math!.Delimiter, Is.EqualTo("$"));
        Assert.That(Find(trees, document, MarkdownNodeKind.DiagramBlock).Data.Diagram!.DiagramType, Is.EqualTo("mermaid"));
        Assert.That(Find(trees, document, MarkdownNodeKind.Emoji).Data.Emoji!.Replacement, Is.EqualTo("SHIP"));
        Assert.That(Find(trees, document, MarkdownNodeKind.CustomContainerBlock).Data.Container!.Name, Is.EqualTo("panel"));
        Assert.That(Find(trees, document, MarkdownNodeKind.CustomFencedBlock).Data.Custom!.Name, Is.EqualTo("chart"));
        Assert.That(Find(trees, document, MarkdownNodeKind.CustomInline).Data.Custom!.Name, Is.EqualTo("kbd"));
        Assert.That(Find(trees, document, MarkdownNodeKind.DetailsBlock).Data.Details!.Summary, Is.EqualTo("More"));

        MarkdownNode paragraph = Find(trees, document, MarkdownNodeKind.Paragraph);
        MarkdownNode text = Find(trees, document, MarkdownNodeKind.Text);
        MarkdownNode table = Find(trees, document, MarkdownNodeKind.Table);
        Assert.That(services.CreateBlockNode(paragraph).Node, Is.SameAs(paragraph));
        Assert.That(services.CreateInlineNode(text).Node, Is.SameAs(text));
        MarkdownExtensionNode extension = services.CreateExtensionNode("matrix", table);
        Assert.That(extension.Node, Is.SameAs(table));
        Assert.That(extension.ExtensionName, Is.EqualTo("matrix"));
    }

    [Test]
    public void PublicNodeEdits_KeepNavigationStableAndRenderTheEditedTree()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        MarkdownDocument document = markdown.ParseSource("one\n\ntwo\n\nthree");
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        MarkdownNode[] original = trees.FindAllInDocument(document, MarkdownNodeKind.Paragraph);
        MarkdownNode inserted = trees.CloneNode(original[0]);
        MarkdownNode replacement = trees.CloneNode(original[2]);
        services.CreateNodeOperations(inserted.Children[0]).SetLiteral("inserted");
        services.CreateNodeOperations(replacement.Children[0]).SetLiteral("replacement");
        IMarkdownNodeOperations root = services.CreateNodeOperations(document.Root);

        root.InsertChildAfter(original[0], inserted);
        MarkdownNode removedByReplacement = root.ReplaceChild(original[2], replacement);
        Assert.That(root.RemoveChild(original[0]), Is.True);

        Assert.That(removedByReplacement, Is.SameAs(original[2]));
        Assert.That(original[0].Parent, Is.Null);
        Assert.That(original[2].Parent, Is.Null);
        Assert.That(root.GetChildren(), Is.EqualTo(new[] { inserted, original[1], replacement }));
        Assert.That(services.CreateNodeOperations(inserted).GetPreviousSibling(), Is.Null);
        Assert.That(services.CreateNodeOperations(inserted).GetNextSibling(), Is.SameAs(original[1]));
        Assert.That(services.CreateNodeOperations(original[1]).GetIndexInParent(), Is.EqualTo(1));
        Assert.That(markdown.RenderMarkdownDocument(document), Is.EqualTo(
            "inserted\n\n" +
            "two\n\n" +
            "replacement\n"));
    }

    [Test]
    public void PublicNodeEdits_CoverPrependIndexedRemovalDetachAndClear()
    {
        IMarkdownDataServices services = new Markdown();
        MarkdownDocument document = new Markdown().ParseSource("one\n\ntwo");
        IMarkdownNodeOperations root = services.CreateNodeOperations(document.Root);
        MarkdownNode first = root.GetChildAt(0);
        MarkdownNode second = root.GetChildAt(1);

        root.RemoveChildAt(0);
        root.PrependChild(first);
        root.InsertChildAt(1, second);
        Assert.That(root.GetChildCount(), Is.EqualTo(2));
        Assert.That(services.CreateNodeOperations(second).Detach(), Is.True);
        Assert.That(second.Parent, Is.Null);
        root.AppendChild(second);
        root.ClearChildren();

        Assert.That(root.HasChildren(), Is.False);
        Assert.That(first.Parent, Is.Null);
        Assert.That(second.Parent, Is.Null);
    }

    [Test]
    public void PublicAttributes_AreDetachedAndSynchronizeTypedMetadata()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        MarkdownNode heading = markdown.ParseSource("# Heading").Root.Children[0];
        IMarkdownNodeOperations operations = services.CreateNodeOperations(heading);

        operations.SetAttribute("level", "3");
        operations.SetAttribute("id", "public-id");
        operations.SetAttribute("data-mode", "wide");
        MarkdownAttribute attribute = operations.GetAttribute("id")!;
        MarkdownAttribute[] attributes = operations.GetAttributes();
        attribute.Value = "changed";
        attributes[0].Value = "changed";

        Assert.That(operations.GetAttributeCount(), Is.EqualTo(3));
        Assert.That(operations.ContainsAttribute("id"), Is.True);
        Assert.That(operations.GetAttributeValue("id"), Is.EqualTo("public-id"));
        Assert.That(heading.Data.Heading!.Level, Is.EqualTo(3));
        Assert.That(heading.Data.Heading.Id, Is.EqualTo("public-id"));
        Assert.That(operations.RemoveAttribute("id"), Is.True);
        Assert.That(heading.Data.Heading.Id, Is.Empty);
        operations.ClearAttributes();
        Assert.That(operations.HasAttributes(), Is.False);
        Assert.That(heading.Data.Heading.Level, Is.EqualTo(0));
    }

    [Test]
    public void NodeDiagnosticsAndDeepClones_AreFullyDetached()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().TrackTrivia().Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline("# Title\r\n\r\nbody", pipeline);
        MarkdownNode heading = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Heading)!;
        IMarkdownNodeOperations node = services.CreateNodeOperations(heading);
        MarkdownSourceSpan span = Span(0, 7);
        node.AddDiagnosticValues(MarkdownDiagnosticSeverity.Warning, "AST001", "heading", span);
        services.CreateDocumentOperations(document).AddDiagnosticValues(
            MarkdownDiagnosticSeverity.Error,
            "DOC001",
            "document",
            span);

        MarkdownDocument clone = services.CreateTreeOperations().CloneDocument(document);
        MarkdownNode clonedHeading = services.CreateTreeOperations()
            .FindFirstInDocument(clone, MarkdownNodeKind.Heading)!;
        clone.Source = "changed";
        clone.Diagnostics.Items[0].Code = "changed";
        clonedHeading.Diagnostics.Items[0].Code = "changed";
        clonedHeading.Span.Start.Offset = 4;
        clonedHeading.Trivia!.OpeningDelimiter = "changed";

        Assert.That(node.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(node.FindDiagnosticsContainingOffset(0).Length, Is.EqualTo(1));
        Assert.That(node.FindDiagnosticsContainingOffset(7), Is.Empty);
        Assert.That(node.FindDiagnosticsIntersectingSpan(Span(6, 9)).Length, Is.EqualTo(1));
        Assert.That(document.Source, Is.EqualTo("# Title\r\n\r\nbody"));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo("DOC001"));
        Assert.That(heading.Diagnostics.Items[0].Code, Is.EqualTo("AST001"));
        Assert.That(heading.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(heading.Trivia!.OpeningDelimiter, Is.EqualTo("#"));
    }

    [Test]
    public void PublicSearchAndVisitorTraversal_UseStablePreorderContexts()
    {
        IMarkdownDataServices services = new Markdown();
        MarkdownDocument document = new Markdown().ParseSource("first *one*\n\nsecond *two*");
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        MarkdownNode[] paragraphs = trees.FindAllInDocument(document, MarkdownNodeKind.Paragraph);
        MarkdownNode[] emphasis = trees.FindAllInNode(document.Root, MarkdownNodeKind.Emphasis);
        List<string> events = new List<string>();
        MarkdownNodeVisitorAdapter visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                events.Add(
                    context.Depth.ToString()
                    + ":"
                    + context.ChildIndex.ToString()
                    + ":"
                    + context.Node.Kind.ToString());
                if (context.Node == paragraphs[0])
                {
                    return MarkdownTreeWalkResult.SkipChildren;
                }

                return MarkdownTreeWalkResult.Continue;
            },
            null);

        Assert.That(paragraphs.Length, Is.EqualTo(2));
        Assert.That(emphasis.Length, Is.EqualTo(2));
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.Emphasis), Is.SameAs(emphasis[0]));
        Assert.That(trees.FindFirstInNode(paragraphs[1], MarkdownNodeKind.Emphasis), Is.SameAs(emphasis[1]));
        Assert.That(trees.WalkDocument(document, visitor), Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(events, Is.EqualTo(new[]
        {
            "0:-1:Document",
            "1:0:Paragraph",
            "1:1:Paragraph",
            "2:0:Text",
            "2:1:Emphasis",
            "3:0:Text"
        }));
    }

    [Test]
    public void MalformedCallerEdits_AreRejectedOrRecoveredWithoutCorruptingTrees()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        MarkdownDocument document = markdown.ParseSource("paragraph");
        MarkdownNode paragraph = document.Root.Children[0];
        MarkdownNode text = paragraph.Children[0];
        IMarkdownNodeOperations root = services.CreateNodeOperations(document.Root);
        IMarkdownNodeOperations paragraphOperations = services.CreateNodeOperations(paragraph);
        MarkdownNode phantomParent = new MarkdownNode();
        MarkdownNode orphan = services.CreateTreeOperations().CloneNode(paragraph);
        orphan.Parent = phantomParent;

        root.AppendChild(orphan);

        Assert.That(orphan.Parent, Is.SameAs(document.Root));
        Assert.That(() => paragraphOperations.AppendChild(document.Root), Throws.TypeOf<ArgumentException>());
        Assert.That(() => root.AppendChild(document.Root), Throws.TypeOf<ArgumentException>());
        Assert.That(() => root.InsertChildBefore(new MarkdownNode(), orphan), Throws.TypeOf<ArgumentException>());
        Assert.That(() => root.ReplaceChild(new MarkdownNode(), orphan), Throws.TypeOf<ArgumentException>());
        Assert.That(() => services.CreateBlockNode(text), Throws.TypeOf<ArgumentException>());
        Assert.That(() => services.CreateInlineNode(paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => services.CreateExtensionNode("bad", paragraph), Throws.TypeOf<ArgumentException>());

        MarkdownNode unknown = new MarkdownNode();
        root.AppendChild(unknown);
        Assert.That(markdown.RenderHtmlDocument(document), Is.EqualTo("<p>paragraph</p>\n<p>paragraph</p>\n"));
        Assert.That(markdown.RenderDebugTreeDocument(document), Does.Contain("Unknown"));
    }

    [Test]
    public void TrackedTrivia_PreservesSourceAndRegeneratesOnlyEditedRegions()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = new Markdown();
        string source = "### Keep ###\r\n\r\nold\r\n\r\n_tail_";
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().TrackTrivia().Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        MarkdownNode[] text = trees.FindAllInDocument(document, MarkdownNodeKind.Text);
        IMarkdownNodeOperations edited = services.CreateNodeOperations(text[1]);
        IMarkdownNodeOperations editedParagraph = services.CreateNodeOperations(text[1].Parent!);
        MarkdownTrivia triviaCopy = editedParagraph.GetTrivia()!;

        Assert.That(services.CreateDocumentOperations(document).RenderSourcePreserving(), Is.EqualTo(source));
        Assert.That(services.CreateDocumentOperations(document).GetOriginalLineEnding(), Is.EqualTo("\r\n"));
        triviaCopy.Text = "caller mutation";
        Assert.That(editedParagraph.GetTriviaText(), Is.EqualTo("old"));

        edited.SetLiteral("new");
        string rendered = services.CreateDocumentOperations(document)
            .RenderSourcePreservingWithPipeline(pipeline);

        Assert.That(rendered, Is.EqualTo("### Keep ###\r\n\r\nnew\r\n\r\n_tail_"));
        Assert.That(document.Root.Trivia!.IsModified, Is.True);
        Assert.That(document.Root.Children[0].Trivia!.IsModified, Is.False);
        Assert.That(document.Root.Children[2].Trivia!.IsModified, Is.False);
    }

    public static MarkdownNode Find(
        IMarkdownTreeOperations trees,
        MarkdownDocument document,
        MarkdownNodeKind kind)
    {
        return trees.FindFirstInDocument(document, kind)!;
    }

    public static MarkdownSourceSpan Span(int start, int end)
    {
        MarkdownSourceSpan span = new MarkdownSourceSpan();
        span.Start.Offset = start;
        span.Start.Line = 1;
        span.Start.Column = start + 1;
        span.End.Offset = end;
        span.End.Line = 1;
        span.End.Column = end + 1;
        return span;
    }
}
