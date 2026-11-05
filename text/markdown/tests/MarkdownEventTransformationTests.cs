using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownEventTransformationTests
{
    [Test]
    public void EventTransform_CanRewriteTextLiterals()
    {
        MarkdownEvent[] events = MarkdownEventReader.ReadAll("hello *world*");
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].IsEnter() && events[index].NodeKind == MarkdownNodeKind.Text)
            {
                events[index].SetLiteral(events[index].Literal.ToUpperInvariant());
            }

            index = index + 1;
        }

        MarkdownDocument document = MarkdownEventWriter.WriteAll(events);

        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("HELLO *WORLD*\n"));
        Assert.That(PlainTextRenderer.Render(document), Is.EqualTo("HELLO WORLD\n"));
    }

    [Test]
    public void EventTransform_CanRewriteLinkAttributes()
    {
        MarkdownEvent[] events = MarkdownEventReader.ReadAll("[docs](/old \"Old\")");
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].IsEnter() && events[index].NodeKind == MarkdownNodeKind.Link)
            {
                events[index].SetAttribute("destination", "/new");
                events[index].SetAttribute("title", "New");
            }

            index = index + 1;
        }

        MarkdownDocument document = MarkdownEventWriter.WriteAll(events);

        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("[docs](/new \"New\")\n"));
        Assert.That(HtmlRenderer.Render(document), Is.EqualTo("<p><a href=\"/new\" title=\"New\">docs</a></p>\n"));
    }

    [Test]
    public void EventTransform_CanRemoveSubtree()
    {
        MarkdownEvent[] events = MarkdownEventReader.ReadAll("keep ![drop](/drop.png) done");
        List<MarkdownEvent> transformed = new List<MarkdownEvent>();
        int skipDepth = -1;
        int index = 0;
        while (index < events.Length)
        {
            MarkdownEvent item = events[index];
            if (skipDepth >= 0)
            {
                if (item.IsLeave() && item.Depth == skipDepth)
                {
                    skipDepth = -1;
                }
            }
            else if (item.IsEnter() && item.NodeKind == MarkdownNodeKind.Image)
            {
                skipDepth = item.Depth;
            }
            else
            {
                transformed.Add(item);
            }

            index = index + 1;
        }

        MarkdownDocument document = MarkdownEventWriter.WriteAll(transformed.ToArray());

        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("keep  done\n"));
        Assert.That(MarkdownAnalyzer.Analyze(document).GetImageCount(), Is.EqualTo(0));
    }

    [Test]
    public void EventTransform_CanInsertInlineEvents()
    {
        MarkdownEvent[] events = MarkdownEventReader.ReadAll("alpha");
        MarkdownEventWriter writer = new MarkdownEventWriter();
        int index = 0;
        while (index < events.Length)
        {
            MarkdownEvent item = events[index];
            if (item.IsLeave() && item.NodeKind == MarkdownNodeKind.Paragraph)
            {
                MarkdownEvent enterText = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Text);
                MarkdownEvent leaveText = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Text);
                enterText.SetLiteral(" beta");
                writer.Write(enterText);
                writer.Write(leaveText);
            }

            writer.Write(item);
            index = index + 1;
        }

        MarkdownDocument document = writer.Finish();

        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("alpha beta\n"));
    }
}
