namespace svg.Tests;

public class SvgPatchTests
{
    [Test]
    public void StablePathsRoundTripConnectedNodesAndRejectMissingPaths()
    {
        SvgDocument document = Load("<svg><g><rect/><circle/></g><path/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode circle = group.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(new SvgPatchPath().Get(document.DocumentNode), Is.EqualTo("/"));
            Assert.That(new SvgPatchPath().Get(root), Is.EqualTo("/0"));
            Assert.That(new SvgPatchPath().Get(group), Is.EqualTo("/0/0"));
            Assert.That(new SvgPatchPath().Get(circle), Is.EqualTo("/0/0/1"));
            Assert.That(new SvgPatchPath().Resolve(document, "/0/0/1"), Is.SameAs(circle));
            Assert.That(new SvgPatchPath().Resolve(document, "/0/9"), Is.Null);
            Assert.That(new SvgPatchPath().Resolve(document, "/0/x"), Is.Null);
            Assert.That(new SvgPatchPath().Resolve(document, "/0/"), Is.Null);
        });
    }

    [Test]
    public void MixedStructuralDiffAppliesAndInverseRestoresOriginalDocument()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgNodeApi nodes = new SvgNodeApi();
        SvgDocument before = Load("<svg xmlns='http://www.w3.org/2000/svg' id='one' class='a b'><g>old</g><rect data-x='1'/></svg>");
        SvgDocument original = Load("<svg xmlns='http://www.w3.org/2000/svg' id='one' class='a b'><g>old</g><rect data-x='1'/></svg>");
        SvgDocument after = Load("<svg xmlns='http://www.w3.org/2000/svg' class='b a' id='two'><g>new</g><circle data-y='2'/><path/></svg>");

        SvgPatch patch = api.Diff(before, after);
        SvgPatchResult applied = api.ApplyPatch(before, patch);

        Assert.Multiple(() =>
        {
            Assert.That(patch.ExpectedVersion, Is.Zero);
            Assert.That(patch.Operations.Select(operation => operation.Kind), Is.EqualTo(new[]
            {
                SvgPatchOperationKind.AttributeMove,
                SvgPatchOperationKind.AttributeReplace,
                SvgPatchOperationKind.AttributeReplace,
                SvgPatchOperationKind.NodeValue,
                SvgPatchOperationKind.NodeReplace,
                SvgPatchOperationKind.NodeInsert
            }));
            Assert.That(applied.Success, Is.True);
            Assert.That(applied.AppliedOperations, Is.EqualTo(patch.Operations.Count));
            Assert.That(applied.Inverse, Is.Not.Null);
            Assert.That(nodes.DeepEquals(before.DocumentNode, after.DocumentNode), Is.True);
            Assert.That(api.Diff(before, after).Operations, Is.Empty);
        });

        SvgPatchResult undone = applied.Inverse.Apply(before);

        Assert.Multiple(() =>
        {
            Assert.That(undone.Success, Is.True);
            Assert.That(nodes.DeepEquals(before.DocumentNode, original.DocumentNode), Is.True);
            Assert.That(before.DocumentElement.Attributes.Select(attribute => attribute.LocalName), Is.EqualTo(new[] { "xmlns", "id", "class" }));
            Assert.That(before.DocumentElement.Children.Select(node => node.LocalName), Is.EqualTo(new[] { "g", "rect" }));
        });
    }

    [Test]
    public void AttributeDiffUsesExpandedNamesAndPreservesTargetOrder()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument before = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:a' id='one' x:key='old' class='shape'/>");
        SvgDocument after = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:a' class='shape' x:key='new' data-extra='yes'/>");
        SvgPatch patch = api.Diff(before, after);

        SvgPatchResult result = patch.Apply(before);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(before.DocumentElement.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "xmlns", "xmlns:x", "class", "x:key", "data-extra" }));
            Assert.That(before.DocumentElement.Attributes.Single(attribute => attribute.NamespaceUri == "urn:a" && attribute.LocalName == "key").Value, Is.EqualTo("new"));
            Assert.That(before.DocumentElement.Attributes.Any(attribute => attribute.LocalName == "id"), Is.False);
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.AttributeMove), Is.True);
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.AttributeInsert), Is.True);
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.AttributeRemove), Is.True);
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.AttributeReplace), Is.True);
        });
    }

    [Test]
    public void NamespaceEquivalentPrefixesDoNotCreateSemanticPatchOperations()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument first = factory.CreateDocument();
        SvgDocument second = factory.CreateDocument();
        SvgNode firstRoot = factory.CreateElement("a:item", "a", "item", "urn:test");
        SvgNode secondRoot = factory.CreateElement("b:item", "b", "item", "urn:test");
        mutations.AppendAttribute(firstRoot, factory.CreateAttribute("a:key", "a", "key", "urn:attr", "value"));
        mutations.AppendAttribute(secondRoot, factory.CreateAttribute("b:key", "b", "key", "urn:attr", "value"));
        mutations.AppendChild(first.DocumentNode, firstRoot);
        mutations.AppendChild(second.DocumentNode, secondRoot);

        SvgPatch patch = SvgPatch.CreateDiff(first, second);

        Assert.That(patch.Operations, Is.Empty);
    }

    [Test]
    public void VersionConflictReturnsDiagnosticWithoutMutatingDocument()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g>old</g></svg>");
        SvgDocument desired = Load("<svg><g>new</g></svg>");
        SvgPatch patch = api.Diff(document, desired);
        SvgNode comment = api.CreateComment("edit");
        mutations.AppendChild(document.DocumentElement, comment);
        long version = document.Version;

        SvgPatchResult result = patch.Apply(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPatchError.VersionConflict));
            Assert.That(result.OperationIndex, Is.EqualTo(-1));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.PatchConflict));
            Assert.That(result.Diagnostics.Single().InstancePath, Is.EqualTo("/"));
            Assert.That(document.Version, Is.EqualTo(version));
            Assert.That(document.DocumentElement.Children.Last(), Is.SameAs(comment));
            Assert.That(document.DocumentElement.Children[0].Children[0].Value, Is.EqualTo("old"));
        });
    }

    [Test]
    public void ValidationChecksDependentOperationsWithoutMutatingTheDocument()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgNodeApi nodes = new SvgNodeApi();
        SvgDocument document = Load("<svg id='one'><g>old</g><rect/></svg>");
        SvgDocument original = Load("<svg id='one'><g>old</g><rect/></svg>");
        SvgDocument desired = Load("<svg class='new'><g>changed</g><circle/><path/></svg>");
        SvgPatch patch = api.Diff(document, desired);

        SvgPatchResult validation = api.ValidatePatch(document, patch);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.AppliedOperations, Is.Zero);
            Assert.That(validation.Inverse, Is.Null);
            Assert.That(nodes.DeepEquals(document.DocumentNode, original.DocumentNode), Is.True);
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
        });
    }

    [Test]
    public void LatePreconditionConflictRollsBackEarlierPatchOperationsAtomically()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument document = Load("<svg><g>old</g><rect/></svg>");
        SvgDocument desired = Load("<svg><g>new</g><circle/></svg>");
        SvgPatch patch = api.Diff(document, desired);
        SvgNode conflicting = document.DocumentElement.Children[1];
        conflicting.LocalName = "path";
        conflicting.QualifiedName = "path";

        SvgPatchResult result = patch.Apply(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPatchError.PreconditionFailed));
            Assert.That(result.OperationIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.PatchConflict));
            Assert.That(result.Diagnostics.Single().InstancePath, Is.EqualTo("/0/1"));
            Assert.That(document.DocumentElement.Children[0].Children[0].Value, Is.EqualTo("old"));
            Assert.That(document.DocumentElement.Children[1], Is.SameAs(conflicting));
            Assert.That(conflicting.LocalName, Is.EqualTo("path"));
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
        });
    }

    [Test]
    public void PatchRefusesToRunInsideAnActiveTransaction()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument document = Load("<svg/>");
        SvgDocument desired = Load("<svg><g/></svg>");
        SvgPatch patch = api.Diff(document, desired);
        SvgChangeSet transaction = api.BeginChangeSet(document);

        SvgPatchResult result = patch.Apply(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPatchError.ActiveTransaction));
            Assert.That(document.DocumentElement.Children, Is.Empty);
        });

        transaction.Rollback();
    }

    [Test]
    public void MissingOperationCollectionReturnsStableNonMutatingFailures()
    {
        SvgDocument document = Load("<svg><g/></svg>");
        SvgPatch patch = new SvgPatch();
        patch.Operations = null;
        patch.Diagnostics = null;

        SvgPatchResult applied = new SvgDocumentApi().ApplyPatch(document, patch);
        SvgPatchResult validated = new SvgDocumentApi().ValidatePatch(document, patch);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Error, Is.EqualTo(SvgPatchError.InvalidArgument));
            Assert.That(applied.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(validated.Error, Is.EqualTo(SvgPatchError.InvalidArgument));
            Assert.That(validated.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(patch.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(document.DocumentElement.Children, Has.Count.EqualTo(1));
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
