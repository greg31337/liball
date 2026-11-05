using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmTaskListTests
{
    [Test]
    public void TaskListRule_ParsesGfmTaskItems()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "- [ ] todo\n" +
            "- [x] done\n" +
            "- [X] caps",
            MarkdownPipeline.GfmPreset());

        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode todo = list.GetChild(0);
        MarkdownNode done = list.GetChild(1);
        MarkdownNode caps = list.GetChild(2);

        Assert.That(todo.Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(todo.GetAttributeValue("checked"), Is.EqualTo("false"));
        Assert.That(todo.GetChild(0).Literal, Is.EqualTo("todo"));
        Assert.That(done.Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(done.GetAttributeValue("checked"), Is.EqualTo("true"));
        Assert.That(done.GetChild(0).Literal, Is.EqualTo("done"));
        Assert.That(caps.GetAttributeValue("checked"), Is.EqualTo("true"));
    }

    [Test]
    public void TaskListRule_DoesNotParseWithoutGfmFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse("- [x] done", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode item = document.Root.GetChild(0).GetChild(0);

        Assert.That(item.Kind, Is.EqualTo(MarkdownNodeKind.ListItem));
        Assert.That(item.GetChild(0).Literal, Is.EqualTo("[x] done"));
    }

    [Test]
    public void TaskListRule_ParsesOrderedTaskItems()
    {
        MarkdownDocument document = MarkdownParser.Parse("3. [x] done", MarkdownPipeline.GfmPreset());
        MarkdownNode item = document.Root.GetChild(0).GetChild(0);

        Assert.That(item.Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(item.GetAttributeValue("checked"), Is.EqualTo("true"));
        Assert.That(MarkdownRenderer.Render(document, MarkdownPipeline.GfmPreset()), Is.EqualTo("3. [x] done\n"));
    }

    [Test]
    public void TaskListRenderers_RenderHtmlAndMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "- [x] done\n" +
            "- [ ] todo",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string text = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<ul>\n" +
            "<li><input checked=\"\" disabled=\"\" type=\"checkbox\"> done</li>\n" +
            "<li><input disabled=\"\" type=\"checkbox\"> todo</li>\n" +
            "</ul>\n"));
        Assert.That(markdown, Is.EqualTo(
            "- [x] done\n" +
            "- [ ] todo\n"));
        Assert.That(text, Is.EqualTo("done\ntodo\n"));
    }

    [Test]
    public void TaskListRenderer_UsesGfmCheckboxMarkupInXhtmlMode()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .UseXhtml()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("- [x] done", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Does.Contain("<input checked=\"\" disabled=\"\" type=\"checkbox\">"));
    }

    [Test]
    public void TaskListRule_HelperMethodsClassifyMarkers()
    {
        MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem);
        List<string> lines = new List<string>();
        lines.Add("[x] done");

        Assert.That(GfmBlockRules.IsTaskListMarkerLine("[ ] todo"), Is.True);
        Assert.That(GfmBlockRules.IsTaskListMarkerLine("[x] done"), Is.True);
        Assert.That(GfmBlockRules.IsTaskListMarkerLine("[X] done"), Is.True);
        Assert.That(GfmBlockRules.IsTaskListMarkerLine("[y] nope"), Is.False);
        Assert.That(GfmBlockRules.IsTaskListMarkerChecked("[X] done"), Is.True);
        Assert.That(GfmBlockRules.RemoveTaskListMarker("[ ] todo"), Is.EqualTo("todo"));
        Assert.That(GfmBlockRules.GetTaskListMarkerContentStart("[x] done"), Is.EqualTo(4));
        Assert.That(GfmBlockRules.TryApplyTaskListMarker(item, lines, MarkdownPipeline.GfmPreset()), Is.True);
        Assert.That(item.Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(lines[0], Is.EqualTo("done"));
        Assert.That(MarkdownRenderer.GetTaskListMarkdownMarker(item), Is.EqualTo("[x]"));
        Assert.That(HtmlRenderer.IsTaskListItemChecked(item), Is.True);
    }

    [Test]
    public void TaskListRule_RejectsInvalidArguments()
    {
        MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem);
        List<string> lines = new List<string>();
        HtmlRenderContext htmlContext = new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.GfmPreset());

        Assert.That(() => GfmBlockRules.TryApplyTaskListMarker(null!, lines, MarkdownPipeline.GfmPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.TryApplyTaskListMarker(item, null!, MarkdownPipeline.GfmPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.TryApplyTaskListMarker(item, lines, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.IsTaskListMarkerLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.IsTaskListMarkerChecked(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.RemoveTaskListMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.GetTaskListMarkerContentStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderTaskListItem(null!, item), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderTaskListItem(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteTaskListCheckbox(null!, item), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteTaskListCheckbox(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.IsTaskListItemChecked(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTaskListMarkdownMarker(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
