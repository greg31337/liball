using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class BracketStackTests
{
    [Test]
    public void Bracket_StoresLinkAndImageMetadata()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create("[", 1, 3, true, false);
        MarkdownBracket link = MarkdownBracket.Create(3, false, node, delimiter);
        MarkdownBracket image = MarkdownBracket.Create(7, true);

        Assert.That(link.Position, Is.EqualTo(3));
        Assert.That(link.IsImage, Is.False);
        Assert.That(link.Active, Is.True);
        Assert.That(link.Node, Is.SameAs(node));
        Assert.That(link.Delimiter, Is.SameAs(delimiter));
        Assert.That(image.IsImage, Is.True);

        link.Deactivate();

        Assert.That(link.Active, Is.False);
    }

    [Test]
    public void BracketStack_PushesPopsAndLinksBrackets()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket first = stack.PushPosition(0, false);
        MarkdownBracket second = stack.PushPosition(2, true);

        Assert.That(stack.GetCount(), Is.EqualTo(2));
        Assert.That(stack.IsEmpty(), Is.False);
        Assert.That(stack.Peek(), Is.SameAs(second));
        Assert.That(first.Next, Is.SameAs(second));
        Assert.That(second.Previous, Is.SameAs(first));

        MarkdownBracket? popped = stack.Pop();

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
    public void BracketStack_RemovesMiddleBracketAndRelinksNeighbors()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket first = stack.PushPosition(0, false);
        MarkdownBracket second = stack.PushPosition(1, true);
        MarkdownBracket third = stack.PushPosition(2, false);
        MarkdownBracket stranger = MarkdownBracket.Create(9, false);

        Assert.That(stack.Remove(second), Is.True);

        Assert.That(stack.GetCount(), Is.EqualTo(2));
        Assert.That(first.Next, Is.SameAs(third));
        Assert.That(third.Previous, Is.SameAs(first));
        Assert.That(second.Previous, Is.Null);
        Assert.That(second.Next, Is.Null);
        Assert.That(stack.Remove(stranger), Is.False);
    }

    [Test]
    public void BracketStack_RemovesUpToBracketFromTop()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket first = stack.PushPosition(0, false);
        MarkdownBracket second = stack.PushPosition(1, true);
        MarkdownBracket third = stack.PushPosition(2, false);

        int removed = stack.RemoveUpTo(second);

        Assert.That(removed, Is.EqualTo(2));
        Assert.That(stack.GetCount(), Is.EqualTo(1));
        Assert.That(stack.Peek(), Is.SameAs(first));
        Assert.That(stack.Contains(first), Is.True);
        Assert.That(stack.Contains(second), Is.False);
        Assert.That(stack.Contains(third), Is.False);
    }

    [Test]
    public void BracketStack_FindsActiveOpeners()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket link = stack.PushPosition(0, false);
        MarkdownBracket inactiveImage = stack.PushPosition(1, true);
        MarkdownBracket image = stack.PushPosition(2, true);
        inactiveImage.Deactivate();

        Assert.That(stack.FindActiveOpener(), Is.SameAs(image));
        Assert.That(stack.FindActiveOpenerByImageKind(true), Is.SameAs(image));
        Assert.That(stack.FindActiveOpenerByImageKind(false), Is.SameAs(link));

        image.Deactivate();

        Assert.That(stack.FindActiveOpenerByImageKind(true), Is.Null);
        Assert.That(stack.FindActiveOpener(), Is.SameAs(link));
    }

    [Test]
    public void BracketStack_DeactivatesLinkOpenersOnly()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket firstLink = stack.PushPosition(0, false);
        MarkdownBracket image = stack.PushPosition(1, true);
        MarkdownBracket secondLink = stack.PushPosition(2, false);

        int count = stack.DeactivateLinkOpeners();

        Assert.That(count, Is.EqualTo(2));
        Assert.That(firstLink.Active, Is.False);
        Assert.That(image.Active, Is.True);
        Assert.That(secondLink.Active, Is.False);
        Assert.That(stack.FindActiveOpenerByImageKind(false), Is.Null);
        Assert.That(stack.FindActiveOpenerByImageKind(true), Is.SameAs(image));
    }

    [Test]
    public void BracketStack_ReturnsBracketsFromBottomToTop()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket first = stack.PushPosition(0, false);
        MarkdownBracket second = stack.PushPosition(1, true);
        MarkdownBracket third = stack.PushPosition(2, false);

        MarkdownBracket[] brackets = stack.GetBrackets();

        Assert.That(brackets.Length, Is.EqualTo(3));
        Assert.That(brackets[0], Is.SameAs(first));
        Assert.That(brackets[1], Is.SameAs(second));
        Assert.That(brackets[2], Is.SameAs(third));

        stack.Clear();

        Assert.That(stack.IsEmpty(), Is.True);
        Assert.That(first.Next, Is.Null);
        Assert.That(third.Previous, Is.Null);
    }

    [Test]
    public void BracketStack_StoresAssociatedNodeAndDelimiter()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create("[", 1, 4, true, false);

        MarkdownBracket bracket = stack.PushNodeWithDelimiter(4, false, node, delimiter);

        Assert.That(bracket.Node, Is.SameAs(node));
        Assert.That(bracket.Delimiter, Is.SameAs(delimiter));
        Assert.That(stack.Peek(), Is.SameAs(bracket));
    }

    [Test]
    public void InlineContext_CreatesBracketStack()
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownInlineParseContext context = MarkdownInlineParseContext.Create(parseContext, paragraph, "text");

        Assert.That(context.Brackets, Is.Not.Null);
        Assert.That(context.Brackets.IsEmpty(), Is.True);
    }

    [Test]
    public void BracketStack_RejectsInvalidArguments()
    {
        MarkdownBracketStack stack = new MarkdownBracketStack();
        MarkdownBracket bracket = MarkdownBracket.Create(0, false);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create("[", 1, 0, true, false);

        stack.Push(bracket);

        Assert.That(() => MarkdownBracket.Create(-1, false), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownBracket.Create(-1, false, node), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownBracket.Create(0, false, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBracket.Create(-1, false, node, delimiter), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownBracket.Create(0, false, null!, delimiter), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBracket.Create(0, false, node, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Push(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Push(bracket), Throws.TypeOf<ArgumentException>());
        Assert.That(() => stack.PushNode(0, false, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.PushNodeWithDelimiter(0, false, node, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Remove(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.RemoveUpTo(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Contains(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
