using System;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownNodeDataTests
{
    [Test]
    public void EveryNodeKind_HasMatchingTypedData()
    {
        MarkdownNodeKind[] kinds = (MarkdownNodeKind[])Enum.GetValues(typeof(MarkdownNodeKind));

        int index = 0;
        while (index < kinds.Length)
        {
            MarkdownNode node = MarkdownNode.Create(kinds[index]);

            Assert.That(node.Data, Is.Not.Null, kinds[index].ToString());
            Assert.That(node.Data.Kind, Is.EqualTo(kinds[index]), kinds[index].ToString());
            index = index + 1;
        }
    }

    [Test]
    public void MetadataBearingKinds_InitializeTheirFeatureContracts()
    {
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Heading).Data.Heading, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.List).Data.List, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.ListItem).Data.ListItem, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.IndentedCodeBlock).Data.CodeBlock!.IsFenced, Is.False);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock).Data.CodeBlock!.IsFenced, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Link).Data.Link, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Image).Data.Image, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition).Data.Reference, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Table).Data.Table, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.TableCell).Data.TableCell, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.TaskListItem).Data.TaskListItem, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.FootnoteDefinition).Data.Footnote!.IsDefinition, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.FootnoteReference).Data.Footnote!.IsDefinition, Is.False);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.FrontMatter).Data.FrontMatter, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.MathSpan).Data.Math!.IsBlock, Is.False);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.MathBlock).Data.Math!.IsBlock, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.DiagramBlock).Data.Diagram, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.AdmonitionBlock).Data.Admonition, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.CustomContainerBlock).Data.Container, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.CustomFencedBlock).Data.Custom!.IsBlock, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.CustomInline).Data.Custom!.IsBlock, Is.False);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.AbbreviationDefinition).Data.Abbreviation!.IsDefinition, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Abbreviation).Data.Abbreviation!.IsDefinition, Is.False);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Emoji).Data.Emoji, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.Attribute).Data.Attribute, Is.Not.Null);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.ExtensionBlock).Data.Extension!.IsBlock, Is.True);
        Assert.That(MarkdownNode.Create(MarkdownNodeKind.ExtensionInline).Data.Extension!.IsBlock, Is.False);
    }

    [Test]
    public void ParsedCoreAndGfmNodes_ExposeTypedMetadata()
    {
        MarkdownDocument common = Markdown.Parse(
            "# Heading\n\n~~~ csharp\ncode\n~~~\n\n<https://example.com> and [reference][target] and ![image][picture]\n\n[target]: /docs 'Docs'\n[picture]: /image.png");

        MarkdownNode heading = FindFirst(common.Root, MarkdownNodeKind.Heading)!;
        MarkdownNode code = FindFirst(common.Root, MarkdownNodeKind.FencedCodeBlock)!;
        MarkdownNode autolink = FindLink(common.Root, true)!;
        MarkdownNode referenceLink = FindLink(common.Root, false)!;
        MarkdownNode image = FindFirst(common.Root, MarkdownNodeKind.Image)!;
        MarkdownNode definition = FindFirst(common.Root, MarkdownNodeKind.LinkReferenceDefinition)!;

        Assert.That(heading.Data.Heading!.Level, Is.EqualTo(1));
        Assert.That(code.Data.CodeBlock!.Info, Is.EqualTo("csharp"));
        Assert.That(code.Data.CodeBlock.FenceMarker, Is.EqualTo("~"));
        Assert.That(code.Data.CodeBlock.FenceLength, Is.EqualTo(3));
        Assert.That(autolink.Data.Link!.Destination, Is.EqualTo("https://example.com"));
        Assert.That(autolink.Data.Link.IsAutolink, Is.True);
        Assert.That(referenceLink.Data.Link!.ReferenceLabel, Is.EqualTo("target"));
        Assert.That(referenceLink.Data.Link.Title, Is.EqualTo("Docs"));
        Assert.That(image.Data.Image!.ReferenceLabel, Is.EqualTo("picture"));
        Assert.That(image.Data.Image.Destination, Is.EqualTo("/image.png"));
        Assert.That(definition.Data.Reference!.NormalizedLabel, Is.EqualTo("target"));

        MarkdownDocument gfm = Markdown.Parse(
            "- [x] complete\n\n| Left | Right |\n| :--- | ---: |\n| one | two |",
            MarkdownPipeline.GfmPreset());
        MarkdownNode task = FindFirst(gfm.Root, MarkdownNodeKind.TaskListItem)!;
        MarkdownNode list = FindFirst(gfm.Root, MarkdownNodeKind.List)!;
        MarkdownNode table = FindFirst(gfm.Root, MarkdownNodeKind.Table)!;
        MarkdownNode cell = FindFirst(gfm.Root, MarkdownNodeKind.TableCell)!;

        Assert.That(task.Data.TaskListItem!.IsChecked, Is.True);
        Assert.That(task.Data.ListItem!.Marker, Is.EqualTo("-"));
        Assert.That(list.Data.List!.IsOrdered, Is.False);
        Assert.That(list.Data.List.Marker, Is.EqualTo("-"));
        Assert.That(list.Data.List.IsTight, Is.True);
        Assert.That(table.Data.Table!.ColumnCount, Is.EqualTo(2));
        Assert.That(cell.Data.TableCell!.Alignment, Is.EqualTo("left"));
    }

    [Test]
    public void FeatureAttributes_UpdateSpecializedContracts()
    {
        MarkdownNode frontMatter = MarkdownNode.Create(MarkdownNodeKind.FrontMatter);
        frontMatter.SetAttribute("format", "yaml");
        frontMatter.SetAttribute("delimiter", "---");
        Assert.That(frontMatter.Data.FrontMatter!.Format, Is.EqualTo("yaml"));
        Assert.That(frontMatter.Data.FrontMatter.Delimiter, Is.EqualTo("---"));

        MarkdownNode footnote = MarkdownNode.Create(MarkdownNodeKind.FootnoteReference);
        footnote.SetAttribute("label", "Note");
        footnote.SetAttribute("normalizedLabel", "note");
        footnote.SetAttribute("resolved", "true");
        Assert.That(footnote.Data.Footnote!.Label, Is.EqualTo("Note"));
        Assert.That(footnote.Data.Footnote.NormalizedLabel, Is.EqualTo("note"));
        Assert.That(footnote.Data.Footnote.IsResolved, Is.True);

        MarkdownNode diagram = MarkdownNode.Create(MarkdownNodeKind.DiagramBlock);
        diagram.SetAttribute("diagramType", "mermaid");
        diagram.SetAttribute("info", "mermaid flow");
        Assert.That(diagram.Data.Diagram!.DiagramType, Is.EqualTo("mermaid"));
        Assert.That(diagram.Data.Diagram.Info, Is.EqualTo("mermaid flow"));

        MarkdownNode admonition = MarkdownNode.Create(MarkdownNodeKind.AdmonitionBlock);
        admonition.SetAttribute("style", "alert");
        admonition.SetAttribute("type", "warning");
        admonition.SetAttribute("title", "Careful");
        Assert.That(admonition.Data.Admonition!.Style, Is.EqualTo("alert"));
        Assert.That(admonition.Data.Admonition.Type, Is.EqualTo("warning"));
        Assert.That(admonition.Data.Admonition.Title, Is.EqualTo("Careful"));

        MarkdownNode container = MarkdownNode.Create(MarkdownNodeKind.CustomContainerBlock);
        container.SetAttribute("name", "details");
        container.SetAttribute("title", "More");
        Assert.That(container.Data.Container!.Name, Is.EqualTo("details"));
        Assert.That(container.Data.Container.Title, Is.EqualTo("More"));

        MarkdownNode custom = MarkdownNode.Create(MarkdownNodeKind.CustomInline);
        custom.SetAttribute("name", "underline");
        custom.SetAttribute("openingDelimiter", "++");
        custom.SetAttribute("closingDelimiter", "++");
        Assert.That(custom.Data.Custom!.Name, Is.EqualTo("underline"));
        Assert.That(custom.Data.Custom.OpeningDelimiter, Is.EqualTo("++"));
        Assert.That(custom.Data.Custom.ClosingDelimiter, Is.EqualTo("++"));

        MarkdownNode math = MarkdownNode.Create(MarkdownNodeKind.MathBlock);
        math.SetAttribute("delimiter", "$$");
        Assert.That(math.Data.Math!.Delimiter, Is.EqualTo("$$"));

        MarkdownExtensionNode extension = MarkdownExtensionNode.Create(
            "sample",
            MarkdownNodeKind.ExtensionInline);
        Assert.That(extension.Node.Data.Extension!.Name, Is.EqualTo("sample"));
    }

    [Test]
    public void KindChangesRemovalAndCloning_KeepMetadataConsistentAndDetached()
    {
        MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem);
        item.SetAttribute("marker", "-");
        item.SetKind(MarkdownNodeKind.TaskListItem);
        item.SetAttribute("checked", "true");

        Assert.That(item.Data.Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(item.Data.ListItem!.Marker, Is.EqualTo("-"));
        Assert.That(item.Data.TaskListItem!.IsChecked, Is.True);

        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        heading.SetAttribute("level", "2");
        heading.SetAttribute("id", "original");
        MarkdownNode clone = heading.CloneNode();
        clone.Data.Heading!.Id = "clone";

        Assert.That(heading.Data.Heading!.Id, Is.EqualTo("original"));
        Assert.That(clone.Data.Heading.Level, Is.EqualTo(2));

        Assert.That(heading.RemoveAttribute("id"), Is.True);
        Assert.That(heading.Data.Heading.Id, Is.Empty);
        heading.ClearAttributes();
        Assert.That(heading.Data.Heading.Level, Is.EqualTo(0));
    }

    private static MarkdownNode? FindFirst(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            MarkdownNode? found = FindFirst(node.Children[index], kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }

    private static MarkdownNode? FindLink(MarkdownNode node, bool autolink)
    {
        if (node.Kind == MarkdownNodeKind.Link && node.Data.Link!.IsAutolink == autolink)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            MarkdownNode? found = FindLink(node.Children[index], autolink);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }
}
