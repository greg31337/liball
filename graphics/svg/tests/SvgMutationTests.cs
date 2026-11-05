namespace svg.Tests;

public class SvgMutationTests
{
    [Test]
    public void DocumentApiCreatesEveryEditableDetachedKind()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument document = api.CreateDocument();
        SvgNode element = api.CreateElement("s:rect", "s", "rect", SvgNamespaces.Svg);
        SvgNode text = api.CreateText("text");
        SvgNode cdata = api.CreateCData("cdata");
        SvgNode comment = api.CreateComment("comment");
        SvgNode instruction = api.CreateProcessingInstruction("edit", "keep");
        SvgNode doctype = api.CreateDoctype("svg", "SYSTEM 'test.dtd'");
        SvgAttribute attribute = api.CreateAttribute("xml:lang", "xml", "lang", SvgNamespaces.Xml, "en");

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement, Is.Null);
            Assert.That(element.Kind, Is.EqualTo(SvgNodeKind.Element));
            Assert.That(element.ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(text.Kind, Is.EqualTo(SvgNodeKind.Text));
            Assert.That(cdata.Kind, Is.EqualTo(SvgNodeKind.CData));
            Assert.That(comment.Kind, Is.EqualTo(SvgNodeKind.Comment));
            Assert.That(instruction.Kind, Is.EqualTo(SvgNodeKind.ProcessingInstruction));
            Assert.That(instruction.QualifiedName, Is.EqualTo("edit"));
            Assert.That(doctype.Kind, Is.EqualTo(SvgNodeKind.Doctype));
            Assert.That(attribute.AttributeKind, Is.EqualTo(SvgAttributeKind.Xml));
            Assert.That(new[] { element, text, cdata, comment, instruction, doctype }, Has.All.Matches<SvgNode>(node => node.Parent == null && node.OwnerDocument == null && !node.IsDirty));
            Assert.That(attribute.OwnerElement, Is.Null);
            Assert.That(attribute.OwnerDocument, Is.Null);
        });
    }

    [Test]
    public void AppendInsertAndRemoveMaintainOrderRootOwnershipAndVersions()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = factory.CreateDocument();
        SvgNode root = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgNode before = factory.CreateComment("before");
        SvgNode group = factory.CreateElement("g", "", "g", SvgNamespaces.Svg);
        SvgNode text = factory.CreateText("value");

        SvgMutationResult rootAdded = api.AppendChild(document.DocumentNode, root);
        SvgMutationResult commentAdded = api.InsertChild(document.DocumentNode, 0, before);
        SvgMutationResult groupAdded = api.AppendChild(root, group);
        SvgMutationResult textAdded = api.AppendChild(group, text);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { rootAdded, commentAdded, groupAdded, textAdded }, Has.All.Matches<SvgMutationResult>(result => result.Success));
            Assert.That(document.Version, Is.EqualTo(4));
            Assert.That(document.IsDirty, Is.True);
            Assert.That(document.DocumentElement, Is.SameAs(root));
            Assert.That(document.DocumentNode.Children, Is.EqualTo(new[] { before, root }));
            Assert.That(root.Children, Is.EqualTo(new[] { group }));
            Assert.That(group.Children, Is.EqualTo(new[] { text }));
            Assert.That(new[] { root.OwnerDocument, before.OwnerDocument, group.OwnerDocument, text.OwnerDocument }, Has.All.SameAs(document));
            Assert.That(text.Version, Is.EqualTo(4));
        });

        SvgMutationResult removed = api.RemoveChild(root, group);

        Assert.Multiple(() =>
        {
            Assert.That(removed.Success, Is.True);
            Assert.That(document.Version, Is.EqualTo(5));
            Assert.That(root.Children, Is.Empty);
            Assert.That(group.Parent, Is.Null);
            Assert.That(group.OwnerDocument, Is.Null);
            Assert.That(text.OwnerDocument, Is.Null);
            Assert.That(group.IsDirty, Is.True);
            Assert.That(text.IsDirty, Is.True);
        });
    }

    [Test]
    public void DocumentElementRulesAndStructuralFailuresAreAtomic()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = factory.CreateDocument();
        SvgNode first = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgNode second = factory.CreateElement("g", "", "g", SvgNamespaces.Svg);
        SvgNode text = factory.CreateText("text");
        api.AppendChild(document.DocumentNode, first);
        long version = document.Version;

        SvgMutationResult duplicateRoot = api.AppendChild(document.DocumentNode, second);
        SvgMutationResult invalidParent = api.AppendChild(text, factory.CreateText("child"));
        SvgMutationResult badIndex = api.InsertChild(first, 2, factory.CreateComment("bad"));
        SvgMutationResult documentChild = api.AppendChild(first, document.DocumentNode);
        SvgMutationResult missing = api.RemoveChild(first, text);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateRoot.Error, Is.EqualTo(SvgMutationError.MultipleDocumentElements));
            Assert.That(invalidParent.Error, Is.EqualTo(SvgMutationError.InvalidParent));
            Assert.That(badIndex.Error, Is.EqualTo(SvgMutationError.IndexOutOfRange));
            Assert.That(documentChild.Error, Is.EqualTo(SvgMutationError.InvalidChild));
            Assert.That(missing.Error, Is.EqualTo(SvgMutationError.NodeNotChild));
            Assert.That(document.Version, Is.EqualTo(version));
            Assert.That(document.DocumentNode.Children, Is.EqualTo(new[] { first }));
            Assert.That(second.Parent, Is.Null);
            Assert.That(second.OwnerDocument, Is.Null);
        });
    }

    [Test]
    public void MovePreventsCyclesReordersAndReassignsWholeSubtreesAcrossDocuments()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument first = Load("<svg><g><rect id='shape'/><circle/></g></svg>");
        SvgDocument second = Load("<svg/>");
        SvgNode firstRoot = first.DocumentElement;
        SvgNode group = firstRoot.Children[0];
        SvgNode rect = group.Children[0];
        SvgNode circle = group.Children[1];
        SvgAttribute id = rect.Attributes[0];

        SvgMutationResult reordered = api.MoveChild(circle, group, 0);
        long afterReorder = first.Version;
        SvgMutationResult noOp = api.MoveChild(circle, group, 0);
        SvgMutationResult cycle = api.MoveChild(group, rect, 0);
        SvgMutationResult moved = api.MoveChild(group, second.DocumentElement, 0);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Success, Is.True);
            Assert.That(group.Children, Is.EqualTo(new[] { circle, rect }));
            Assert.That(noOp.Success, Is.True);
            Assert.That(cycle.Error, Is.EqualTo(SvgMutationError.Cycle));
            Assert.That(afterReorder, Is.EqualTo(1));
            Assert.That(first.Version, Is.EqualTo(2));
            Assert.That(second.Version, Is.EqualTo(1));
            Assert.That(moved.Success, Is.True);
            Assert.That(firstRoot.Children, Is.Empty);
            Assert.That(second.DocumentElement.Children, Is.EqualTo(new[] { group }));
            Assert.That(group.Parent, Is.SameAs(second.DocumentElement));
            Assert.That(new[] { group.OwnerDocument, rect.OwnerDocument, circle.OwnerDocument, id.OwnerDocument }, Has.All.SameAs(second));
            Assert.That(group.Version, Is.EqualTo(second.Version));
            Assert.That(id.OwnerElement, Is.SameAs(rect));
        });
    }

    [Test]
    public void ReplaceChildDetachesOldSubtreeAndUpdatesDocumentElement()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg><rect/><circle/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode oldRect = root.Children[0];
        SvgNode replacement = factory.CreateElement("path", "", "path", SvgNamespaces.Svg);

        SvgMutationResult replaced = api.ReplaceChild(root, oldRect, replacement);
        SvgNode comment = factory.CreateComment("temporarily rootless");
        SvgMutationResult rootReplaced = api.ReplaceChild(document.DocumentNode, root, comment);
        SvgNode newRoot = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgMutationResult newRootAdded = api.AppendChild(document.DocumentNode, newRoot);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Success, Is.True);
            Assert.That(replaced.RemovedNode, Is.SameAs(oldRect));
            Assert.That(root.Children[0], Is.SameAs(replacement));
            Assert.That(oldRect.Parent, Is.Null);
            Assert.That(oldRect.OwnerDocument, Is.Null);
            Assert.That(rootReplaced.Success, Is.True);
            Assert.That(root.Parent, Is.Null);
            Assert.That(root.OwnerDocument, Is.Null);
            Assert.That(newRootAdded.Success, Is.True);
            Assert.That(document.DocumentElement, Is.SameAs(newRoot));
            Assert.That(document.DocumentNode.Children, Is.EqualTo(new[] { comment, newRoot }));
            Assert.That(document.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public void AttributeMutationsPreserveOrderExpandedNameUniquenessAndOwnership()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg id='root'><g/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgAttribute fill = factory.CreateAttribute("fill", "", "fill", "", "red");
        SvgAttribute title = factory.CreateAttribute("title", "", "title", "", "one");
        SvgAttribute replacement = factory.CreateAttribute("class", "", "class", "", "main");
        SvgAttribute duplicate = factory.CreateAttribute("id", "", "id", "", "other");

        SvgMutationResult fillAdded = api.AppendAttribute(root, fill);
        SvgMutationResult titleInserted = api.InsertAttribute(root, 1, title);
        long beforeDuplicate = document.Version;
        SvgMutationResult duplicateRejected = api.AppendAttribute(root, duplicate);
        SvgMutationResult replaced = api.ReplaceAttribute(root, title, replacement);
        SvgMutationResult moved = api.MoveAttribute(fill, group, 0);
        SvgMutationResult removed = api.RemoveAttribute(root, replacement);

        Assert.Multiple(() =>
        {
            Assert.That(fillAdded.Success, Is.True);
            Assert.That(titleInserted.Success, Is.True);
            Assert.That(duplicateRejected.Error, Is.EqualTo(SvgMutationError.DuplicateAttribute));
            Assert.That(beforeDuplicate, Is.EqualTo(2));
            Assert.That(replaced.Success, Is.True);
            Assert.That(replaced.RemovedAttribute, Is.SameAs(title));
            Assert.That(title.OwnerElement, Is.Null);
            Assert.That(title.OwnerDocument, Is.Null);
            Assert.That(moved.Success, Is.True);
            Assert.That(fill.OwnerElement, Is.SameAs(group));
            Assert.That(fill.OwnerDocument, Is.SameAs(document));
            Assert.That(group.Attributes, Is.EqualTo(new[] { fill }));
            Assert.That(removed.Success, Is.True);
            Assert.That(replacement.OwnerElement, Is.Null);
            Assert.That(replacement.OwnerDocument, Is.Null);
            Assert.That(root.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "id" }));
            Assert.That(document.Version, Is.EqualTo(5));
        });
    }

    [Test]
    public void CrossDocumentAttributeMoveInvalidatesBothDocumentsOnce()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument first = Load("<svg><rect data-key='one'/></svg>");
        SvgDocument second = Load("<svg><circle/></svg>");
        SvgNode rect = first.DocumentElement.Children[0];
        SvgNode circle = second.DocumentElement.Children[0];
        SvgAttribute attribute = rect.Attributes[0];

        SvgMutationResult moved = api.MoveAttribute(attribute, circle, 0);

        Assert.Multiple(() =>
        {
            Assert.That(moved.Success, Is.True);
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(second.Version, Is.EqualTo(1));
            Assert.That(rect.Attributes, Is.Empty);
            Assert.That(circle.Attributes, Is.EqualTo(new[] { attribute }));
            Assert.That(attribute.OwnerElement, Is.SameAs(circle));
            Assert.That(attribute.OwnerDocument, Is.SameAs(second));
            Assert.That(attribute.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void OverflowIsDetectedBeforeAnyMutation()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg/>");
        SvgNode child = factory.CreateComment("unchanged");
        document.Version = long.MaxValue;

        Assert.Throws<OverflowException>(() => api.AppendChild(document.DocumentElement, child));

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement.Children, Is.Empty);
            Assert.That(child.Parent, Is.Null);
            Assert.That(child.OwnerDocument, Is.Null);
            Assert.That(document.Version, Is.EqualTo(long.MaxValue));
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
