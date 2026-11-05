using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownNodeOperationsTests
{
    [Test]
    public void ConstructorAndFacade_ExposeTheRequestedNode()
    {
        MarkdownNode node = new MarkdownNode();
        IMarkdownNodeOperations direct = new MarkdownNodeOperations(node);
        IMarkdownDataServices services = new Markdown();
        IMarkdownNodeOperations fromFacade = services.CreateNodeOperations(node);

        Assert.That(direct.GetNode(), Is.SameAs(node));
        Assert.That(fromFacade.GetNode(), Is.SameAs(node));
        Assert.That(() => new MarkdownNodeOperations(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.CreateNodeOperations(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ChildInsertion_ReparentsAndProvidesStableNavigation()
    {
        MarkdownNode parent = new MarkdownNode();
        MarkdownNode previousParent = new MarkdownNode();
        MarkdownNode first = new MarkdownNode();
        MarkdownNode second = new MarkdownNode();
        MarkdownNode third = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(parent);

        new MarkdownNodeOperations(previousParent).AppendChild(second);
        operations.AppendChild(first);
        operations.AppendChild(second);
        operations.PrependChild(third);

        Assert.That(operations.GetChildCount(), Is.EqualTo(3));
        Assert.That(operations.HasChildren(), Is.True);
        Assert.That(operations.GetFirstChild(), Is.SameAs(third));
        Assert.That(operations.GetLastChild(), Is.SameAs(second));
        Assert.That(operations.GetChildAt(1), Is.SameAs(first));
        Assert.That(previousParent.Children, Is.Empty);

        IMarkdownNodeOperations firstOperations = new MarkdownNodeOperations(first);
        Assert.That(firstOperations.HasParent(), Is.True);
        Assert.That(firstOperations.GetParent(), Is.SameAs(parent));
        Assert.That(firstOperations.GetPreviousSibling(), Is.SameAs(third));
        Assert.That(firstOperations.GetNextSibling(), Is.SameAs(second));
        Assert.That(firstOperations.GetIndexInParent(), Is.EqualTo(1));

        MarkdownNode[] snapshot = operations.GetChildren();
        snapshot[0] = new MarkdownNode();
        Assert.That(operations.GetFirstChild(), Is.SameAs(third));
    }

    [Test]
    public void IndexedAndRelativeInsertion_MoveExistingChildrenWithoutDuplicates()
    {
        MarkdownNode parent = new MarkdownNode();
        MarkdownNode first = new MarkdownNode();
        MarkdownNode second = new MarkdownNode();
        MarkdownNode third = new MarkdownNode();
        MarkdownNode fourth = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(parent);

        operations.AppendChild(first);
        operations.AppendChild(third);
        operations.InsertChildAt(1, second);
        operations.InsertChildAfter(third, fourth);
        operations.InsertChildBefore(first, fourth);

        Assert.That(operations.GetChildren(), Is.EqualTo(new[] { fourth, first, second, third }));
        Assert.That(fourth.Parent, Is.SameAs(parent));
        Assert.That(operations.GetChildCount(), Is.EqualTo(4));
    }

    [Test]
    public void ReplaceChild_DetachesOldChildAndMovesReplacementSafely()
    {
        MarkdownNode parent = new MarkdownNode();
        MarkdownNode otherParent = new MarkdownNode();
        MarkdownNode first = new MarkdownNode();
        MarkdownNode oldChild = new MarkdownNode();
        MarkdownNode replacement = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(parent);

        operations.AppendChild(first);
        operations.AppendChild(oldChild);
        new MarkdownNodeOperations(otherParent).AppendChild(replacement);

        MarkdownNode removed = operations.ReplaceChild(oldChild, replacement);

        Assert.That(removed, Is.SameAs(oldChild));
        Assert.That(oldChild.Parent, Is.Null);
        Assert.That(replacement.Parent, Is.SameAs(parent));
        Assert.That(otherParent.Children, Is.Empty);
        Assert.That(operations.GetChildren(), Is.EqualTo(new[] { first, replacement }));

        Assert.That(operations.ReplaceChild(first, replacement), Is.SameAs(first));
        Assert.That(operations.GetChildren(), Is.EqualTo(new[] { replacement }));
        Assert.That(first.Parent, Is.Null);
        Assert.That(operations.ReplaceChild(replacement, replacement), Is.SameAs(replacement));
    }

    [Test]
    public void TreeEdits_RejectCyclesAndInvalidTargets()
    {
        MarkdownNode parent = new MarkdownNode();
        MarkdownNode child = new MarkdownNode();
        MarkdownNode missing = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(parent);
        operations.AppendChild(child);

        Assert.That(() => operations.AppendChild(parent), Throws.TypeOf<ArgumentException>());
        Assert.That(() => new MarkdownNodeOperations(child).AppendChild(parent), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.InsertChildAt(-1, missing), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.InsertChildAt(3, missing), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.InsertChildBefore(missing, new MarkdownNode()), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.InsertChildAfter(missing, new MarkdownNode()), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.ReplaceChild(missing, new MarkdownNode()), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.ReplaceChild(child, parent), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.AppendChild(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void RemovalAndDetach_UpdateBothSidesOfRelationships()
    {
        MarkdownNode parent = new MarkdownNode();
        MarkdownNode first = new MarkdownNode();
        MarkdownNode second = new MarkdownNode();
        MarkdownNode third = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(parent);
        operations.AppendChild(first);
        operations.AppendChild(second);
        operations.AppendChild(third);

        Assert.That(operations.RemoveChild(second), Is.True);
        Assert.That(second.Parent, Is.Null);
        Assert.That(operations.RemoveChild(second), Is.False);
        Assert.That(operations.RemoveChildAt(0), Is.SameAs(first));
        Assert.That(first.Parent, Is.Null);
        Assert.That(new MarkdownNodeOperations(third).Detach(), Is.True);
        Assert.That(new MarkdownNodeOperations(third).Detach(), Is.False);
        Assert.That(new MarkdownNodeOperations(third).GetIndexInParent(), Is.EqualTo(-1));

        operations.AppendChild(first);
        operations.AppendChild(second);
        operations.ClearChildren();
        Assert.That(operations.HasChildren(), Is.False);
        Assert.That(first.Parent, Is.Null);
        Assert.That(second.Parent, Is.Null);
    }

    [Test]
    public void AttributeOperations_ReturnCopiesAndSynchronizeTypedData()
    {
        MarkdownNode heading = Markdown.Parse("# Heading").Root.Children[0];
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(heading);

        operations.SetAttribute("id", "original");
        operations.SetAttribute("level", "3");

        Assert.That(operations.GetAttributeCount(), Is.EqualTo(2));
        Assert.That(operations.HasAttributes(), Is.True);
        Assert.That(operations.ContainsAttribute("id"), Is.True);
        Assert.That(operations.GetAttributeValue("id"), Is.EqualTo("original"));
        Assert.That(heading.Data.Heading!.Id, Is.EqualTo("original"));
        Assert.That(heading.Data.Heading.Level, Is.EqualTo(3));

        MarkdownAttribute copy = operations.GetAttribute("id")!;
        MarkdownAttribute[] copies = operations.GetAttributes();
        copy.Value = "changed";
        copies[0].Value = "changed again";
        Assert.That(operations.GetAttributeValue("id"), Is.EqualTo("original"));

        Assert.That(operations.RemoveAttribute("id"), Is.True);
        Assert.That(operations.GetAttribute("id"), Is.Null);
        Assert.That(heading.Data.Heading.Id, Is.Empty);
        operations.ClearAttributes();
        Assert.That(operations.HasAttributes(), Is.False);
        Assert.That(heading.Data.Heading.Level, Is.EqualTo(0));

        Assert.That(() => operations.SetAttribute("", "value"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.SetAttribute("name", null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void NodeDiagnostics_AreCopiedClearedAndClonedWithTheNode()
    {
        MarkdownNode node = new MarkdownNode();
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(node);
        MarkdownDiagnostic diagnostic = MarkdownDiagnostic.Warning(
            "W001",
            "warning",
            MarkdownSourceSpan.Create(1, 1, 2, 3, 1, 4));

        operations.AddDiagnostic(diagnostic);
        operations.AddDiagnosticValues(
            MarkdownDiagnosticSeverity.Error,
            "E001",
            "error",
            MarkdownSourceSpan.Create(4, 1, 5, 6, 1, 7));
        diagnostic.Code = "changed";

        MarkdownDiagnostic[] returned = operations.GetDiagnostics();
        returned[0].Code = "returned change";
        Assert.That(operations.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(operations.HasDiagnostics(), Is.True);
        Assert.That(operations.GetDiagnostics()[0].Code, Is.EqualTo("W001"));

        MarkdownNode clone = node.CloneNode();
        clone.Diagnostics.Items[0].Code = "clone change";
        Assert.That(node.Diagnostics.Items[0].Code, Is.EqualTo("W001"));

        operations.ClearDiagnostics();
        Assert.That(operations.GetDiagnosticCount(), Is.EqualTo(0));
        Assert.That(operations.HasDiagnostics(), Is.False);
        Assert.That(() => operations.AddDiagnostic(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => operations.AddDiagnosticValues(MarkdownDiagnosticSeverity.Warning, null!, "message", new MarkdownSourceSpan()),
            Throws.TypeOf<ArgumentNullException>());
    }
}
