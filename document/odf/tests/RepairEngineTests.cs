using System.Text;

namespace odf.Tests;

public class RepairEngineTests
{
    [Test]
    public void ProposesAndAppliesMimetypeManifestAndManifestStateRepairs()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage package = RequirePackage(document);
        OdfPackageWriter writer = new OdfPackageWriter();
        RepairEngine engine = new RepairEngine();

        writer.RemovePackageEntry(package, OdfPartNames.Mimetype);
        RepairAnalysisResult mimetypeAnalysis = engine.ProposeRepairs(document);
        RepairProposal mimetype = FindProposal(
            mimetypeAnalysis,
            RepairKind.PackageMimetype);

        Assert.Multiple(() =>
        {
            Assert.That(mimetype.IsSafe, Is.True);
            Assert.That(HasPackageEntry(package, OdfPartNames.Mimetype), Is.False);
        });

        RepairApplicationResult mimetypeResult = engine.ApplyRepairs(
            document,
            [mimetype]);
        OdfPackageEntry mimetypeEntry = FindPackageEntry(
            package,
            OdfPartNames.Mimetype);

        Assert.Multiple(() =>
        {
            Assert.That(mimetypeResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(mimetypeResult.AppliedProposalCount, Is.EqualTo(1));
            Assert.That(mimetypeResult.Mutations, Has.Length.EqualTo(1));
            Assert.That(mimetypeResult.Mutations[0].Kind, Is.EqualTo(RepairKind.PackageMimetype));
            Assert.That(mimetypeEntry.IsFirstEntry, Is.True);
            Assert.That(mimetypeEntry.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(mimetypeEntry.LocalExtraFieldLength, Is.Zero);
            Assert.That(ReadEntry(package, OdfPartNames.Mimetype), Is.EqualTo(OdfMediaTypes.Text));
        });

        writer.RemovePackageEntry(package, OdfPartNames.Manifest);
        RepairAnalysisResult manifestAnalysis = engine.ProposeRepairs(document);
        RepairProposal manifest = FindProposal(
            manifestAnalysis,
            RepairKind.PackageManifest);
        RepairApplicationResult manifestResult = engine.ApplyRepairs(
            document,
            [manifest]);

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsSafe, Is.True);
            Assert.That(manifestResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasPackageEntry(package, OdfPartNames.Manifest), Is.True);
            Assert.That(ReadEntry(package, OdfPartNames.Manifest), Does.Contain("manifest:file-entry"));
        });

        RemoveManifestEntry(package.Manifest, OdfPartNames.Content);
        RepairAnalysisResult synchronizationAnalysis =
            engine.ProposeRepairs(document);
        RepairProposal synchronization = FindProposal(
            synchronizationAnalysis,
            RepairKind.ManifestSynchronization);

        Assert.That(
            HasManifestEntry(package.Manifest, OdfPartNames.Content),
            Is.False,
            "Analysis must not mutate the manifest.");

        RepairApplicationResult synchronizationResult = engine.ApplyRepairs(
            document,
            [synchronization]);

        Assert.Multiple(() =>
        {
            Assert.That(synchronizationResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasManifestEntry(package.Manifest, OdfPartNames.Content), Is.True);
            Assert.That(ReadEntry(package, OdfPartNames.Manifest), Does.Contain(OdfPartNames.Content));
        });
    }

    [Test]
    public void RepairsNamespacesIdsAndReferencesOnlyAfterExplicitApplication()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfPart part = document.Parts[0];
        OdfNode root = RequireRoot(part);
        OdfNode namespaceNode = Element("urn:example:repair", "widget", "ext");
        OdfNode invalidIdNode = Element("", "invalid-id", "");
        OdfAttribute invalidId = Attribute(
            OdfNamespaces.Xml,
            "id",
            OdfNamespaces.XmlPrefix,
            "1bad");
        invalidIdNode.Attributes = [invalidId];
        invalidIdNode.XmlId = "stale-cache";
        OdfNode invalidReferenceNode = Element("", "reference", "");
        OdfAttribute invalidReference = Attribute("", "target-id", "", "1bad");
        invalidReferenceNode.Attributes = [invalidReference];
        OdfNode goodIdNode = Element("", "good-id", "");
        goodIdNode.Attributes =
        [
            Attribute(OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, "good")
        ];
        goodIdNode.XmlId = "good";
        OdfNode brokenReferenceNode = Element("", "broken-reference", "");
        OdfAttribute brokenReference = Attribute("", "target-id", "", "#good");
        brokenReferenceNode.Attributes = [brokenReference];
        OdfNode cachedIdNode = Element("", "cached-id", "");
        cachedIdNode.XmlId = "cached";
        OdfNode firstDuplicate = IdElement("duplicate-one", "dup");
        OdfNode secondDuplicate = IdElement("duplicate-two", "dup");
        AppendChildren(
            root,
            [
                namespaceNode,
                invalidIdNode,
                invalidReferenceNode,
                goodIdNode,
                brokenReferenceNode,
                cachedIdNode,
                firstDuplicate,
                secondDuplicate
            ]);

        RepairEngine engine = new RepairEngine();
        RepairAnalysisResult analysis = engine.ProposeRepairs(document);
        RepairProposal invalidIdProposal = FindProposalForNode(
            analysis,
            RepairKind.XmlIdValue,
            invalidIdNode);
        RepairProposal duplicateProposal = FindProposalForNode(
            analysis,
            RepairKind.XmlIdValue,
            secondDuplicate);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.SafeProposalCount, Is.EqualTo(5));
            Assert.That(analysis.UnsafeProposalCount, Is.EqualTo(1));
            Assert.That(namespaceNode.NamespaceDeclarations, Is.Empty);
            Assert.That(invalidId.Value, Is.EqualTo("1bad"));
            Assert.That(invalidIdNode.XmlId, Is.EqualTo("stale-cache"));
            Assert.That(invalidReference.Value, Is.EqualTo("1bad"));
            Assert.That(brokenReference.Value, Is.EqualTo("#good"));
            Assert.That(FindXmlId(cachedIdNode), Is.Null);
            Assert.That(invalidIdProposal.IsSafe, Is.True);
            Assert.That(duplicateProposal.IsSafe, Is.False);
        });

        RepairApplicationResult result = engine.ApplyRepairs(
            document,
            analysis.Proposals);
        OdfAttribute? cachedId = FindXmlId(cachedIdNode);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.AppliedProposalCount, Is.EqualTo(5));
            Assert.That(result.SkippedProposalCount, Is.EqualTo(1));
            Assert.That(result.Mutations, Has.Length.EqualTo(6));
            Assert.That(namespaceNode.NamespaceDeclarations, Has.Length.EqualTo(1));
            Assert.That(namespaceNode.NamespaceDeclarations[0].Prefix, Is.EqualTo("ext"));
            Assert.That(namespaceNode.NamespaceDeclarations[0].NamespaceName, Is.EqualTo("urn:example:repair"));
            Assert.That(invalidId.Value, Is.EqualTo(invalidIdProposal.NewValue));
            Assert.That(invalidIdNode.XmlId, Is.EqualTo(invalidIdProposal.NewValue));
            Assert.That(invalidReference.Value, Is.EqualTo(invalidIdProposal.NewValue));
            Assert.That(brokenReference.Value, Is.EqualTo("good"));
            Assert.That(cachedId?.Value, Is.EqualTo("cached"));
            Assert.That(FindXmlId(firstDuplicate)?.Value, Is.EqualTo("dup"));
            Assert.That(FindXmlId(secondDuplicate)?.Value, Is.EqualTo("dup"));
            Assert.That(result.Mutations.Count(item => item.Kind == RepairKind.BrokenReference), Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsStaleRepairProposalsWithoutMutation()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfPart part = document.Parts[0];
        OdfNode root = RequireRoot(part);
        OdfNode target = IdElement("target", "target");
        OdfNode source = Element("", "source", "");
        OdfAttribute reference = Attribute("", "target-id", "", "#target");
        source.Attributes = [reference];
        AppendChildren(root, [target, source]);
        RepairEngine engine = new RepairEngine();
        RepairAnalysisResult analysis = engine.ProposeRepairs(document);
        RepairProposal proposal = FindProposal(
            analysis,
            RepairKind.BrokenReference);

        reference.Value = "#changed";
        RepairApplicationResult result = engine.ApplyRepairs(
            document,
            [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(result.Changed, Is.False);
            Assert.That(result.AppliedProposalCount, Is.Zero);
            Assert.That(result.SkippedProposalCount, Is.EqualTo(1));
            Assert.That(result.Mutations, Is.Empty);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.EditConflict));
            Assert.That(reference.Value, Is.EqualTo("#changed"));
        });
    }

    [Test]
    public void LeavesConflictingNamespaceRepairUnsafeAndUnapplied()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode root = RequireRoot(document.Parts[0]);
        OdfNode node = Element("urn:example:first", "item", "shared");
        node.Attributes =
        [
            Attribute("urn:example:second", "value", "shared", "x")
        ];
        AppendChildren(root, [node]);
        RepairEngine engine = new RepairEngine();

        RepairAnalysisResult analysis = engine.ProposeRepairs(document);
        RepairProposal proposal = FindProposal(
            analysis,
            RepairKind.NamespaceBinding);
        RepairApplicationResult result = engine.ApplyRepairs(
            document,
            analysis.Proposals);

        Assert.Multiple(() =>
        {
            Assert.That(proposal.IsSafe, Is.False);
            Assert.That(analysis.UnsafeProposalCount, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(result.SkippedProposalCount, Is.EqualTo(1));
            Assert.That(result.Mutations, Is.Empty);
            Assert.That(node.NamespaceDeclarations, Is.Empty);
        });
    }

    private static OdfPackage RequirePackage(OdfDocument document)
    {
        Assert.That(document.Package, Is.Not.Null);
        return document.Package!;
    }

    private static OdfNode RequireRoot(OdfPart part)
    {
        Assert.That(part.Root, Is.Not.Null);
        return part.Root!;
    }

    private static RepairProposal FindProposal(
        RepairAnalysisResult analysis,
        RepairKind kind)
    {
        return analysis.Proposals.Single(item => item.Kind == kind);
    }

    private static RepairProposal FindProposalForNode(
        RepairAnalysisResult analysis,
        RepairKind kind,
        OdfNode node)
    {
        return analysis.Proposals.Single(
            item => item.Kind == kind && item.Node == node);
    }

    private static OdfNode Element(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            }
        };
    }

    private static OdfNode IdElement(string localName, string id)
    {
        OdfNode result = Element("", localName, "");
        result.Attributes =
        [
            Attribute(OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, id)
        ];
        result.XmlId = id;
        return result;
    }

    private static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        return new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            },
            Value = value
        };
    }

    private static void AppendChildren(OdfNode parent, OdfNode[] additions)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + additions.Length];
        Array.Copy(parent.Children, children, parent.Children.Length);
        Array.Copy(additions, 0, children, parent.Children.Length, additions.Length);
        foreach (OdfNode child in additions)
        {
            child.Parent = parent;
        }
        parent.Children = children;
    }

    private static OdfAttribute? FindXmlId(OdfNode node)
    {
        return node.Attributes.SingleOrDefault(
            item => item.Name.NamespaceName == OdfNamespaces.Xml
                && item.Name.LocalName == "id");
    }

    private static bool HasPackageEntry(OdfPackage package, string path)
    {
        return package.Entries.Any(item => item.Path == path);
    }

    private static OdfPackageEntry FindPackageEntry(
        OdfPackage package,
        string path)
    {
        return package.Entries.Single(item => item.Path == path);
    }

    private static bool HasManifestEntry(
        OdfManifest? manifest,
        string path)
    {
        return manifest != null
            && manifest.Entries.Any(item => item.FullPath == path);
    }

    private static void RemoveManifestEntry(
        OdfManifest? manifest,
        string path)
    {
        Assert.That(manifest, Is.Not.Null);
        manifest!.Entries = manifest.Entries
            .Where(item => item.FullPath != path)
            .ToArray();
    }

    private static string ReadEntry(OdfPackage package, string path)
    {
        Assert.That(package.EntryIndex, Is.Not.Null);
        using Stream stream = package.EntryIndex!.OpenEntry(path);
        using MemoryStream output = new MemoryStream();
        stream.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
