using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownNodeTreeTests
{
    [Test]
    public void AppendChild_SetsParentAndSiblingNavigation()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(first);
        parent.AppendChild(second);

        Assert.That(parent.GetChildCount(), Is.EqualTo(2));
        Assert.That(parent.GetFirstChild(), Is.SameAs(first));
        Assert.That(parent.GetLastChild(), Is.SameAs(second));
        Assert.That(first.Parent, Is.SameAs(parent));
        Assert.That(second.Parent, Is.SameAs(parent));
        Assert.That(first.GetPreviousSibling(), Is.Null);
        Assert.That(first.GetNextSibling(), Is.SameAs(second));
        Assert.That(second.GetPreviousSibling(), Is.SameAs(first));
        Assert.That(second.GetNextSibling(), Is.Null);
    }

    [Test]
    public void InsertOperations_PlaceChildrenAtExpectedIndexes()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode third = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode fourth = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(second);
        parent.PrependChild(first);
        parent.InsertAfter(second, fourth);
        parent.InsertBefore(fourth, third);

        Assert.That(parent.GetChild(0), Is.SameAs(first));
        Assert.That(parent.GetChild(1), Is.SameAs(second));
        Assert.That(parent.GetChild(2), Is.SameAs(third));
        Assert.That(parent.GetChild(3), Is.SameAs(fourth));
        Assert.That(parent.IndexOfChild(third), Is.EqualTo(2));
    }

    [Test]
    public void InsertChild_CanMoveChildWithinSameParent()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode third = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(first);
        parent.AppendChild(second);
        parent.AppendChild(third);
        parent.InsertChild(3, first);

        Assert.That(parent.GetChild(0), Is.SameAs(second));
        Assert.That(parent.GetChild(1), Is.SameAs(third));
        Assert.That(parent.GetChild(2), Is.SameAs(first));
        Assert.That(first.Parent, Is.SameAs(parent));
    }

    [Test]
    public void InsertChild_ReparentsFromOldParent()
    {
        MarkdownNode oldParent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode newParent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        oldParent.AppendChild(child);
        newParent.AppendChild(child);

        Assert.That(oldParent.GetChildCount(), Is.EqualTo(0));
        Assert.That(newParent.GetChildCount(), Is.EqualTo(1));
        Assert.That(newParent.GetChild(0), Is.SameAs(child));
        Assert.That(child.Parent, Is.SameAs(newParent));
    }

    [Test]
    public void RemoveOperations_ClearParentReferences()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(first);
        parent.AppendChild(second);

        Assert.That(parent.RemoveChild(first), Is.True);
        Assert.That(first.Parent, Is.Null);
        Assert.That(parent.GetChildCount(), Is.EqualTo(1));

        MarkdownNode removed = parent.RemoveChildAt(0);

        Assert.That(removed, Is.SameAs(second));
        Assert.That(second.Parent, Is.Null);
        Assert.That(parent.GetChildCount(), Is.EqualTo(0));
        Assert.That(parent.RemoveChild(first), Is.False);
    }

    [Test]
    public void ClearChildren_AndDetach_UpdateParentReferences()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(first);
        parent.AppendChild(second);

        Assert.That(first.Detach(), Is.True);
        Assert.That(first.Parent, Is.Null);
        Assert.That(parent.GetChildCount(), Is.EqualTo(1));

        parent.ClearChildren();

        Assert.That(second.Parent, Is.Null);
        Assert.That(parent.GetChildCount(), Is.EqualTo(0));
        Assert.That(second.Detach(), Is.False);
    }

    [Test]
    public void IsAncestorOf_ReportsAncestorRelationship()
    {
        MarkdownNode root = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode grandchild = MarkdownNode.Create(MarkdownNodeKind.Text);

        root.AppendChild(child);
        child.AppendChild(grandchild);

        Assert.That(root.IsAncestorOf(grandchild), Is.True);
        Assert.That(child.IsAncestorOf(grandchild), Is.True);
        Assert.That(grandchild.IsAncestorOf(root), Is.False);
    }

    [Test]
    public void TreeOperations_RejectInvalidArguments()
    {
        MarkdownNode parent = MarkdownNode.Create(MarkdownNodeKind.Document);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode stranger = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        parent.AppendChild(child);

        Assert.That(() => parent.AppendChild(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => parent.InsertChild(-1, stranger), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => parent.InsertChild(2, stranger), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => parent.AppendChild(parent), Throws.TypeOf<ArgumentException>());
        Assert.That(() => child.AppendChild(parent), Throws.TypeOf<ArgumentException>());
        Assert.That(() => parent.InsertBefore(null!, stranger), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => parent.InsertBefore(stranger, child), Throws.TypeOf<ArgumentException>());
        Assert.That(() => parent.InsertAfter(null!, stranger), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => parent.InsertAfter(stranger, child), Throws.TypeOf<ArgumentException>());
        Assert.That(() => parent.RemoveChild(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => parent.RemoveChildAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => parent.RemoveChildAt(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => parent.IndexOfChild(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => parent.IsAncestorOf(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
