using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownContainerStackTests
{
    [Test]
    public void ContainerState_StoresMetadataAndCanClose()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.BlockQuote);
        MarkdownContainerState state = MarkdownContainerState.Create(node, 2, 1, ">");

        Assert.That(state.Node, Is.SameAs(node));
        Assert.That(state.Indent, Is.EqualTo(2));
        Assert.That(state.MarkerColumn, Is.EqualTo(1));
        Assert.That(state.Marker, Is.EqualTo(">"));
        Assert.That(state.IsOpen, Is.True);

        state.Close();

        Assert.That(state.IsOpen, Is.False);
    }

    [Test]
    public void ContainerState_CopyPreservesValues()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.List);
        MarkdownContainerState state = MarkdownContainerState.Create(node, 4, 0, "-");
        MarkdownContainerState copy = state.Copy();

        state.Marker = "*";
        state.Close();

        Assert.That(copy.Node, Is.SameAs(node));
        Assert.That(copy.Indent, Is.EqualTo(4));
        Assert.That(copy.Marker, Is.EqualTo("-"));
        Assert.That(copy.IsOpen, Is.True);
    }

    [Test]
    public void ContainerStack_PushPeekAndPopPreserveOrder()
    {
        MarkdownContainerStack stack = new MarkdownContainerStack();
        MarkdownNode quote = MarkdownNode.Create(MarkdownNodeKind.BlockQuote);
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.List);

        stack.PushNode(quote, 0, 0, ">");
        stack.PushNode(list, 2, 2, "-");

        Assert.That(stack.HasOpenContainers(), Is.True);
        Assert.That(stack.GetCount(), Is.EqualTo(2));
        Assert.That(stack.Peek()!.Node, Is.SameAs(list));

        MarkdownContainerState? popped = stack.Pop();

        Assert.That(popped!.Node, Is.SameAs(list));
        Assert.That(popped.IsOpen, Is.False);
        Assert.That(stack.GetCount(), Is.EqualTo(1));
        Assert.That(stack.Peek()!.Node, Is.SameAs(quote));
    }

    [Test]
    public void ContainerStack_ReturnsCopiesForStateViews()
    {
        MarkdownContainerStack stack = new MarkdownContainerStack();

        stack.PushNode(MarkdownNode.Create(MarkdownNodeKind.BlockQuote), 0, 0, ">");

        MarkdownContainerState state = stack.GetAt(0);
        MarkdownContainerState[] states = stack.GetStates();
        state.Marker = "mutated";
        states[0].Marker = "also-mutated";

        Assert.That(stack.GetAt(0).Marker, Is.EqualTo(">"));
    }

    [Test]
    public void ContainerStack_ClearEmptiesStack()
    {
        MarkdownContainerStack stack = new MarkdownContainerStack();

        stack.PushNode(MarkdownNode.Create(MarkdownNodeKind.BlockQuote), 0, 0, ">");
        stack.Clear();

        Assert.That(stack.GetCount(), Is.EqualTo(0));
        Assert.That(stack.HasOpenContainers(), Is.False);
        Assert.That(stack.Peek(), Is.Null);
        Assert.That(stack.Pop(), Is.Null);
    }

    [Test]
    public void ContainerStack_RejectsInvalidArguments()
    {
        MarkdownContainerStack stack = new MarkdownContainerStack();

        Assert.That(() => MarkdownContainerState.Create(null!, 0, 0, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownContainerState.Create(new MarkdownNode(), -1, 0, ""), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownContainerState.Create(new MarkdownNode(), 0, -1, ""), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownContainerState.Create(new MarkdownNode(), 0, 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.Push(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.PushNode(null!, 0, 0, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => stack.GetAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => stack.GetAt(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
