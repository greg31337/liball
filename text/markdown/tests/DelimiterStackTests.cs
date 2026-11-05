using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class DelimiterStackTests
{
    [Test]
    public void Delimiter_StoresRunMetadata()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create("*", 2, 5, true, false, node);

        Assert.That(delimiter.Marker, Is.EqualTo("*"));
        Assert.That(delimiter.Length, Is.EqualTo(2));
        Assert.That(delimiter.OriginalLength, Is.EqualTo(2));
        Assert.That(delimiter.Position, Is.EqualTo(5));
        Assert.That(delimiter.CanOpen, Is.True);
        Assert.That(delimiter.CanClose, Is.False);
        Assert.That(delimiter.Active, Is.True);
        Assert.That(delimiter.Node, Is.SameAs(node));
        Assert.That(delimiter.HasRemainingDelimiter(), Is.True);

        delimiter.UseDelimiterCharacters(1);

        Assert.That(delimiter.Length, Is.EqualTo(1));

        delimiter.Deactivate();

        Assert.That(delimiter.Active, Is.False);
    }

    [Test]
    public void DelimiterStack_PushesPopsAndLinksDelimiters()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter first = stack.PushMarker("*", 1, 0, true, false);
        MarkdownDelimiter second = stack.PushMarker("_", 2, 3, false, true);

        Assert.That(stack.GetCount(), Is.EqualTo(2));
        Assert.That(stack.IsEmpty(), Is.False);
        Assert.That(stack.Peek(), Is.SameAs(second));
        Assert.That(first.Next, Is.SameAs(second));
        Assert.That(second.Previous, Is.SameAs(first));

        MarkdownDelimiter? popped = stack.Pop();

        Assert.That(popped, Is.SameAs(second));
        Assert.That(stack.Peek(), Is.SameAs(first));
        Assert.That(first.Next, Is.Null);
        Assert.That(second.Previous, Is.Null);
        Assert.That(second.Next, Is.Null);

        stack.Pop();

        Assert.That(stack.IsEmpty(), Is.True);
        Assert.That(stack.Pop(), Is.Null);
    }

    [Test]
    public void DelimiterStack_RemovesMiddleDelimiterAndRelinksNeighbors()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter first = stack.PushMarker("*", 1, 0, true, false);
        MarkdownDelimiter second = stack.PushMarker("_", 1, 1, true, true);
        MarkdownDelimiter third = stack.PushMarker("~", 2, 2, false, true);
        MarkdownDelimiter stranger = MarkdownDelimiter.Create("*", 1, 9, true, true);

        Assert.That(stack.Remove(second), Is.True);

        Assert.That(stack.GetCount(), Is.EqualTo(2));
        Assert.That(first.Next, Is.SameAs(third));
        Assert.That(third.Previous, Is.SameAs(first));
        Assert.That(second.Previous, Is.Null);
        Assert.That(second.Next, Is.Null);
        Assert.That(stack.Remove(stranger), Is.False);
    }

    [Test]
    public void DelimiterStack_RemovesUpToDelimiterFromTop()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter first = stack.PushMarker("*", 1, 0, true, false);
        MarkdownDelimiter second = stack.PushMarker("_", 1, 1, true, false);
        MarkdownDelimiter third = stack.PushMarker("~", 2, 2, true, true);

        int removed = stack.RemoveUpTo(second);

        Assert.That(removed, Is.EqualTo(2));
        Assert.That(stack.GetCount(), Is.EqualTo(1));
        Assert.That(stack.Peek(), Is.SameAs(first));
        Assert.That(stack.Contains(first), Is.True);
        Assert.That(stack.Contains(second), Is.False);
        Assert.That(stack.Contains(third), Is.False);
    }

    [Test]
    public void DelimiterStack_FindsMatchingActiveOpeners()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter starOpener = stack.PushMarker("*", 1, 0, true, false);
        MarkdownDelimiter underscoreOpener = stack.PushMarker("_", 2, 1, true, false);
        MarkdownDelimiter inactiveStar = stack.PushMarker("*", 1, 2, true, false);
        inactiveStar.Deactivate();
        MarkdownDelimiter starCloser = stack.PushMarker("*", 1, 3, false, true);
        MarkdownDelimiter nonClosingDelimiter = stack.PushMarker("*", 1, 4, true, false);

        Assert.That(stack.FindMatchingOpener(starCloser), Is.SameAs(starOpener));
        Assert.That(stack.FindMatchingOpenerByMarker("_"), Is.SameAs(underscoreOpener));
        Assert.That(stack.FindMatchingOpener(nonClosingDelimiter), Is.Null);
    }

    [Test]
    public void DelimiterStack_DeactivatesOpenersByMarker()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter first = stack.PushMarker("[", 1, 0, true, false);
        MarkdownDelimiter second = stack.PushMarker("*", 1, 1, true, false);
        MarkdownDelimiter third = stack.PushMarker("[", 1, 2, true, false);
        MarkdownDelimiter closer = stack.PushMarker("[", 1, 3, false, true);

        int count = stack.DeactivateOpeners("[");

        Assert.That(count, Is.EqualTo(2));
        Assert.That(first.Active, Is.False);
        Assert.That(second.Active, Is.True);
        Assert.That(third.Active, Is.False);
        Assert.That(closer.Active, Is.True);
        Assert.That(stack.FindMatchingOpenerByMarker("["), Is.Null);
    }

    [Test]
    public void DelimiterStack_ReturnsDelimitersFromBottomToTop()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter emphasis = stack.PushMarker("*", 1, 0, true, true);
        MarkdownDelimiter strong = stack.PushMarker("_", 2, 1, true, true);
        MarkdownDelimiter strikethrough = stack.PushMarker("~", 2, 2, true, true);
        MarkdownDelimiter link = stack.PushMarker("[", 1, 3, true, false);

        MarkdownDelimiter[] delimiters = stack.GetDelimiters();

        Assert.That(delimiters.Length, Is.EqualTo(4));
        Assert.That(delimiters[0], Is.SameAs(emphasis));
        Assert.That(delimiters[1], Is.SameAs(strong));
        Assert.That(delimiters[2], Is.SameAs(strikethrough));
        Assert.That(delimiters[3], Is.SameAs(link));

        stack.Clear();

        Assert.That(stack.IsEmpty(), Is.True);
        Assert.That(emphasis.Next, Is.Null);
        Assert.That(link.Previous, Is.Null);
    }

    [Test]
    public void InlineContext_CreatesDelimiterStack()
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownInlineParseContext context = MarkdownInlineParseContext.Create(parseContext, paragraph, "text");

        Assert.That(context.Delimiters, Is.Not.Null);
        Assert.That(context.Delimiters.IsEmpty(), Is.True);
    }

    [Test]
    public void DelimiterStack_RejectsInvalidArguments()
    {
        MarkdownDelimiterStack stack = new MarkdownDelimiterStack();
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create("*", 1, 0, true, true);

        stack.Push(delimiter);

        Assert.That(() => MarkdownDelimiter.Create(null!, 1, 0, true, true), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownDelimiter.Create("", 1, 0, true, true), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownDelimiter.Create("*", 0, 0, true, true), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownDelimiter.Create("*", 1, -1, true, true), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownDelimiter.Create("*", 1, 0, true, true, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => delimiter.UseDelimiterCharacters(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => delimiter.UseDelimiterCharacters(2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownDelimiter.ValidateMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownDelimiter.ValidateMarker(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => stack.Push(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Push(delimiter), Throws.TypeOf<ArgumentException>());
        Assert.That(() => stack.Remove(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.RemoveUpTo(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Contains(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.FindMatchingOpener((MarkdownDelimiter)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.FindMatchingOpenerByMarker((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.FindMatchingOpenerByMarker(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => stack.DeactivateOpeners(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.DeactivateOpeners(""), Throws.TypeOf<ArgumentException>());
    }
}
