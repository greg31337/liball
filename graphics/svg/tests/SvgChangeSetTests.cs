namespace svg.Tests;

public class SvgChangeSetTests
{
    [Test]
    public void CommitRetainsEditsAndCreatesReversedInverseDescriptions()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g id='old'>before</g><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rect = root.Children[1];
        SvgNode circle = documents.CreateElement("circle", "", "circle", SvgNamespaces.Svg);
        SvgAttribute replacement = documents.CreateAttribute("class", "", "class", "", "shape");
        SvgChangeSet changes = documents.BeginChangeSet(document);

        mutations.InsertChild(root, 1, circle);
        mutations.MoveChild(rect, group, 1);
        mutations.ReplaceAttribute(group, group.Attributes[0], replacement);
        mutations.SetTextContent(group, "after");
        bool committed = changes.Commit();
        SvgChangeSet inverse = changes.CreateInverse();

        Assert.Multiple(() =>
        {
            Assert.That(committed, Is.True);
            Assert.That(changes.Completed, Is.True);
            Assert.That(changes.Committed, Is.True);
            Assert.That(changes.RolledBack, Is.False);
            Assert.That(changes.StartVersion, Is.Zero);
            Assert.That(changes.EndVersion, Is.EqualTo(4));
            Assert.That(changes.Changes.Select(change => change.Kind), Is.EqualTo(new[]
            {
                SvgChangeKind.NodeInsert,
                SvgChangeKind.NodeMove,
                SvgChangeKind.AttributeReplace,
                SvgChangeKind.Text
            }));
            Assert.That(changes.Changes[0].Node, Is.SameAs(circle));
            Assert.That(changes.Changes[1].OldParent, Is.SameAs(root));
            Assert.That(changes.Changes[1].NewParent, Is.SameAs(group));
            Assert.That(changes.Changes[2].RemovedAttribute.Value, Is.EqualTo("old"));
            Assert.That(changes.Changes[3].OldValue, Is.EqualTo("before"));
            Assert.That(changes.Changes[3].NewValue, Is.EqualTo("after"));
            Assert.That(root.Children, Is.EqualTo(new[] { group, circle }));
            Assert.That(group.Children.Single().Value, Is.EqualTo("after"));
            Assert.That(inverse.Completed, Is.True);
            Assert.That(inverse.Changes.Select(change => change.Kind), Is.EqualTo(new[]
            {
                SvgChangeKind.Text,
                SvgChangeKind.AttributeReplace,
                SvgChangeKind.NodeMove,
                SvgChangeKind.NodeRemove
            }));
            Assert.That(inverse.Changes[1].Attribute.Value, Is.EqualTo("old"));
            Assert.That(inverse.Changes[1].RemovedAttribute, Is.SameAs(replacement));
        });

        Assert.That(changes.Commit(), Is.False);
    }

    [Test]
    public void RollbackRestoresIdentityOrderOwnershipTextVersionsAndIndexes()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g id='original'>before</g><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rect = root.Children[1];
        SvgNode originalText = group.Children[0];
        SvgAttribute originalId = group.Attributes[0];
        SvgNode circle = documents.CreateElement("circle", "", "circle", SvgNamespaces.Svg);
        SvgAttribute replacement = documents.CreateAttribute("class", "", "class", "", "changed");
        SvgChangeSet changes = documents.BeginChangeSet(document);

        mutations.MoveChild(rect, group, 1);
        mutations.SetTextContent(group, "changed");
        mutations.AppendChild(root, circle);
        mutations.ReplaceAttribute(group, originalId, replacement);
        Assert.That(document.Version, Is.EqualTo(4));

        bool rolledBack = changes.Rollback();

        Assert.Multiple(() =>
        {
            Assert.That(rolledBack, Is.True);
            Assert.That(changes.Completed, Is.True);
            Assert.That(changes.Committed, Is.False);
            Assert.That(changes.RolledBack, Is.True);
            Assert.That(changes.EndVersion, Is.Zero);
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(root.Children, Is.EqualTo(new[] { group, rect }));
            Assert.That(group.Children, Is.EqualTo(new[] { originalText }));
            Assert.That(originalText.Value, Is.EqualTo("before"));
            Assert.That(group.Attributes, Is.EqualTo(new[] { originalId }));
            Assert.That(originalId.OwnerElement, Is.SameAs(group));
            Assert.That(originalId.OwnerDocument, Is.SameAs(document));
            Assert.That(rect.Parent, Is.SameAs(root));
            Assert.That(rect.OwnerDocument, Is.SameAs(document));
            Assert.That(circle.Parent, Is.Null);
            Assert.That(circle.OwnerDocument, Is.Null);
            Assert.That(replacement.OwnerElement, Is.Null);
            Assert.That(replacement.OwnerDocument, Is.Null);
            Assert.That(document.Index.GetById("original"), Is.SameAs(group));
            Assert.That(document.Index.GetByClass("changed"), Is.Empty);
        });

        Assert.That(changes.Rollback(), Is.False);
    }

    [Test]
    public void FailedAndNoOpMutationsDoNotCreateChanges()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g>same</g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode text = group.Children[0];
        SvgChangeSet changes = documents.BeginChangeSet(document);

        SvgMutationResult noTextChange = mutations.SetTextContent(text, "same");
        SvgMutationResult noMove = mutations.MoveChild(group, document.DocumentElement, 0);
        SvgMutationResult failed = mutations.RemoveChild(group, documents.CreateText("missing"));

        Assert.Multiple(() =>
        {
            Assert.That(noTextChange.Success, Is.True);
            Assert.That(noMove.Success, Is.True);
            Assert.That(failed.Success, Is.False);
            Assert.That(changes.Changes, Is.Empty);
            Assert.That(document.Version, Is.Zero);
        });

        changes.Rollback();
    }

    [Test]
    public void TransactionsAreExclusiveUntilCompletion()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgDocument document = Load("<svg/>");
        SvgChangeSet first = documents.BeginChangeSet(document);

        Assert.Throws<InvalidOperationException>(() => documents.BeginChangeSet(document));
        Assert.Throws<InvalidOperationException>(() => first.CreateInverse());
        Assert.That(first.Commit(), Is.True);

        SvgChangeSet second = documents.BeginChangeSet(document);

        Assert.That(second.Rollback(), Is.True);
    }

    [Test]
    public void CrossDocumentMoveRollbackRestoresBothDocuments()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument source = Load("<svg><g><rect/></g></svg>");
        SvgDocument target = Load("<svg><circle/></svg>");
        SvgNode group = source.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgChangeSet changes = documents.BeginChangeSet(target);

        SvgMutationResult moved = mutations.MoveChild(group, target.DocumentElement, 1);
        bool rolledBack = changes.Rollback();

        Assert.Multiple(() =>
        {
            Assert.That(moved.Success, Is.True);
            Assert.That(rolledBack, Is.True);
            Assert.That(source.Version, Is.Zero);
            Assert.That(target.Version, Is.Zero);
            Assert.That(source.DocumentElement.Children, Is.EqualTo(new[] { group }));
            Assert.That(target.DocumentElement.Children.Single().LocalName, Is.EqualTo("circle"));
            Assert.That(group.Parent, Is.SameAs(source.DocumentElement));
            Assert.That(group.OwnerDocument, Is.SameAs(source));
            Assert.That(rect.OwnerDocument, Is.SameAs(source));
        });
    }

    [Test]
    public void RemoveAndAttributeOperationsRecordExactOwnersAndIndexes()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg id='root'><g class='a'/><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rect = root.Children[1];
        SvgAttribute id = root.Attributes[0];
        SvgAttribute groupClass = group.Attributes[0];
        SvgChangeSet changes = documents.BeginChangeSet(document);

        mutations.RemoveChild(root, rect);
        mutations.MoveAttribute(groupClass, root, 1);
        mutations.RemoveAttribute(root, id);
        changes.Commit();

        Assert.Multiple(() =>
        {
            Assert.That(changes.Changes.Select(change => change.Kind), Is.EqualTo(new[]
            {
                SvgChangeKind.NodeRemove,
                SvgChangeKind.AttributeMove,
                SvgChangeKind.AttributeRemove
            }));
            Assert.That(changes.Changes[0].OldParent, Is.SameAs(root));
            Assert.That(changes.Changes[0].OldIndex, Is.EqualTo(1));
            Assert.That(changes.Changes[1].OldParent, Is.SameAs(group));
            Assert.That(changes.Changes[1].NewParent, Is.SameAs(root));
            Assert.That(changes.Changes[2].Attribute, Is.SameAs(id));
            Assert.That(changes.Changes[2].OldIndex, Is.Zero);
        });
    }

    [Test]
    public void RollbackRestoresDetachedImportedNodeOwnershipAndMetadata()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument source = Load("<svg><path><rect/></path></svg>");
        SvgDocument target = Load("<svg/>");
        SvgNode imported = documents.ImportNode(target, source.DocumentElement.Children[0], true);
        long originalVersion = imported.Version;
        SvgChangeSet changes = documents.BeginChangeSet(target);

        mutations.AppendChild(target.DocumentElement, imported);
        mutations.SetTextContent(imported, "temporary");
        changes.Rollback();

        Assert.Multiple(() =>
        {
            Assert.That(target.DocumentElement.Children, Is.Empty);
            Assert.That(imported.Parent, Is.Null);
            Assert.That(imported.OwnerDocument, Is.SameAs(target));
            Assert.That(imported.Children.Single().LocalName, Is.EqualTo("rect"));
            Assert.That(imported.Children[0].OwnerDocument, Is.SameAs(target));
            Assert.That(imported.IsDirty, Is.False);
            Assert.That(imported.Version, Is.EqualTo(originalVersion));
            Assert.That(target.Version, Is.Zero);
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
