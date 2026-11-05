using System.Text;

namespace odf.Tests;

public class OdfDocumentEditorTests
{
    [Test]
    public void EnforcesSessionLimitsAndRejectsStalePreconditionsAtomically()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument limitedDocument = ReadFlat(
            "<office:body><office:text><text:p>One</text:p>"
                + "<text:p>Two</text:p></office:text></office:body>");
        OdfOperation first = OperationLogModel.CreateSetTextOperation(
            limitedDocument,
            limitedDocument.TextBlocks[0].Node!,
            "First changed",
            1);
        first.Id = "limited-first";
        OdfOperation second = OperationLogModel.CreateSetTextOperation(
            limitedDocument,
            limitedDocument.TextBlocks[1].Node!,
            "Second changed",
            2);
        second.Id = "limited-second";
        OdfEditSession limited = editor.BeginEditSession(
            limitedDocument,
            "limited-session",
            CommitOptions(),
            1);
        OdfEditResult firstStage = editor.StageEditOperation(limited, first);
        OdfEditResult limitedStage = editor.StageEditOperation(
            limited, second);
        OdfEditResult limitedDiscard = editor.DiscardEditSession(limited);

        OdfDocument staleDocument = ReadFlat(
            "<office:body><office:text><text:p>Original</text:p>"
                + "</office:text></office:body>");
        OdfNode staleParagraph = staleDocument.TextBlocks[0].Node!;
        OdfOperation stagedOperation =
            OperationLogModel.CreateSetTextOperation(
                staleDocument,
                staleParagraph,
                "Staged",
                1);
        stagedOperation.Id = "stale-operation";
        OdfEditSession stale = editor.BeginEditSession(
            staleDocument,
            "stale-session",
            CommitOptions(),
            2);
        editor.StageEditOperation(stale, stagedOperation);
        IOdfChangeTracker tracker = new OdfChangeTracker();
        OdfOperation concurrent = tracker.RecordSetTextOperation(
            staleDocument,
            staleParagraph,
            "Concurrent");
        OdfEditResult concurrentResult = tracker.ReplayOperations(
            staleDocument,
            [concurrent]);
        OdfEditResult staleCommit = editor.CommitEditSession(stale);
        OdfEditResult staleDiscard = editor.DiscardEditSession(stale);

        Assert.Multiple(() =>
        {
            Assert.That(firstStage.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(limitedStage.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(limitedStage.Diagnostics.Single().Code,
                Is.EqualTo(OdfDiagnosticCodes.EditConflict));
            Assert.That(limited.StagedOperations, Is.Empty);
            Assert.That(limitedDiscard.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(limitedDocument.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "One", "Two" }));
            Assert.That(concurrentResult.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(staleCommit.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(staleCommit.Changed, Is.False);
            Assert.That(staleCommit.AffectedCount, Is.Zero);
            Assert.That(staleCommit.Diagnostics.Single().Code,
                Is.EqualTo(OdfDiagnosticCodes.EditConflict));
            Assert.That(staleDiscard.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(stale.IsFinished, Is.True);
            Assert.That(stale.IsDiscarded, Is.True);
            Assert.That(staleDocument.TextBlocks[0].Text,
                Is.EqualTo("Concurrent"));
        });
    }

    [Test]
    public void EditSessionsStageCommitAndDiscardThroughPublicContract()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument document = ReadFlat(
            "<office:body><office:text><text:p>Original</text:p>"
                + "</office:text></office:body>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation operation = OperationLogModel.CreateSetTextOperation(
            document,
            paragraph,
            "Committed",
            1);
        operation.Id = "public-session-operation";
        OdfEditSession session = editor.BeginEditSession(
            document,
            "public-session",
            CommitOptions(),
            4);

        OdfEditResult staged = editor.StageEditOperation(
            session,
            operation);
        session.StagedOperations[0].Value = "caller mutation";
        OdfOperation[] snapshot = editor.GetStagedEditOperations(session);
        OdfEditResult committed = editor.CommitEditSession(session);

        OdfDocument discardedDocument = ReadFlat(
            "<office:body><office:text><text:p>Kept</text:p>"
                + "</office:text></office:body>");
        OdfNode discardedParagraph = discardedDocument.TextBlocks[0].Node!;
        OdfOperation discardedOperation =
            OperationLogModel.CreateSetTextOperation(
                discardedDocument,
                discardedParagraph,
                "Discarded",
                1);
        discardedOperation.Id = "discarded-operation";
        OdfEditSession discardedSession = editor.BeginEditSession(
            discardedDocument,
            "discard-session",
            CommitOptions(),
            2);
        editor.StageEditOperation(discardedSession, discardedOperation);
        OdfEditResult discarded = editor.DiscardEditSession(
            discardedSession);

        Assert.Multiple(() =>
        {
            Assert.That(session.Id, Is.EqualTo("public-session"));
            Assert.That(session.Document, Is.SameAs(document));
            Assert.That(session.MaximumOperations, Is.EqualTo(4));
            Assert.That(staged.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(staged.Changed, Is.False);
            Assert.That(snapshot, Has.Length.EqualTo(1));
            Assert.That(snapshot[0].Value, Is.EqualTo("Committed"));
            Assert.That(committed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(committed.Mutations, Has.Length.EqualTo(1));
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.IsDiscarded, Is.False);
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("Committed"));
            Assert.That(discarded.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(discardedSession.StagedOperations, Is.Empty);
            Assert.That(discardedSession.IsFinished, Is.True);
            Assert.That(discardedSession.IsDiscarded, Is.True);
            Assert.That(discardedDocument.TextBlocks[0].Text,
                Is.EqualTo("Kept"));
        });
    }

    [Test]
    public void EditsPartsChecksBodiesAndConvertsRepresentations()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument source = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfDocument target = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfNode[] metadata = ReadNodes(
            "<office:document-meta xmlns:office='"
                + OdfNamespaces.Office
                + "' office:version='1.4'><office:meta/>"
                + "</office:document-meta>",
            OdfPartNames.Metadata);

        OdfEditResult added = editor.AddDocumentPart(
            source,
            OdfPartNames.Metadata,
            metadata,
            OdfMediaTypes.Xml);
        OdfEditResult copied = editor.CopyDocumentPart(
            source,
            target,
            OdfPartNames.Metadata,
            OdfPartNames.Metadata);
        OdfEditResult removed = editor.RemoveDocumentPart(
            target,
            OdfPartNames.Metadata);

        OdfDocument flat = ReadFlat(
            "<office:styles/><office:meta/><office:settings/>"
                + "<office:body><office:text><text:p>Converted</text:p>"
                + "</office:text></office:body>");
        OdfEditResult body = editor.CheckDocumentBodyKind(
            flat,
            OdfDocumentKind.TextTemplate);
        OdfDocument package = editor.ConvertDocumentToPackage(flat);
        OdfDocument roundTrip = editor.ConvertDocumentToFlat(package);

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasPart(source, OdfPartNames.Metadata), Is.True);
            Assert.That(HasPart(target, OdfPartNames.Metadata), Is.False);
            Assert.That(body.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(package.PackageKind,
                Is.EqualTo(OdfPackageKind.Package));
            Assert.That(roundTrip.PackageKind,
                Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(roundTrip.TextBlocks[0].Text,
                Is.EqualTo("Converted"));
        });
    }

    [Test]
    public void ConversionsAreIdempotentAndRejectPackageOnlyResources()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument flat = ReadFlat(
            "<office:styles/><office:meta/><office:settings/>"
                + "<office:body><office:text><text:p>Conversion contract"
                + "</text:p></office:text></office:body>");

        OdfDocument alreadyFlat = editor.ConvertDocumentToFlat(flat);
        OdfDocument package = editor.ConvertDocumentToPackage(flat);
        OdfDocument alreadyPackage = editor.ConvertDocumentToPackage(package);
        OdfDocument convertedFlat = editor.ConvertDocumentToFlat(package);
        OdfEditResult resource = editor.AddDocumentResource(
            package,
            "Pictures/non-flat.bin",
            [1, 2, 3],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);

        Assert.Multiple(() =>
        {
            Assert.That(alreadyFlat, Is.SameAs(flat));
            Assert.That(alreadyPackage, Is.SameAs(package));
            Assert.That(package.PackageKind,
                Is.EqualTo(OdfPackageKind.Package));
            Assert.That(convertedFlat.PackageKind,
                Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(convertedFlat.TextBlocks[0].Text,
                Is.EqualTo("Conversion contract"));
            Assert.That(resource.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(() => editor.ConvertDocumentToFlat(package),
                Throws.TypeOf<OdfEditException>()
                    .With.Message.Contains("resources"));
        });
    }

    [Test]
    public void MaintainsResourcesReferencesAndGarbageCollection()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument source = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfDocument target = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        editor.AddDocumentResource(
            source,
            "Resources/used.bin",
            [1, 2],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        editor.AddDocumentResource(
            source,
            "Resources/orphan.bin",
            [3, 4],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        OdfNode text = source.Body!.Children[0];
        new OdfTreeEditor(source.Parts[0].Nodes).SetAttribute(
            text,
            Name(OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix),
            "Resources/used.bin");

        string uniquePath = editor.CreateUniqueDocumentResourcePath(
            source,
            "Resources/used.bin");
        OdfEditResult copied = editor.CopyDocumentResource(
            source,
            target,
            "Resources/used.bin",
            "Resources/copied.bin");
        OdfEditResult removedCopy = editor.RemoveDocumentResource(
            target,
            "Resources/copied.bin");
        string[] unused = editor.FindUnusedDocumentResources(source);
        string recordedUnused = source.OrphanedResourcePaths[0];
        unused[0] = "caller mutation";
        OdfEditResult collected = editor.CollectUnusedDocumentResources(
            source);
        OdfEditResult moved = editor.MoveDocumentResource(
            source,
            "Resources/used.bin",
            "Resources/moved.bin");
        IOdfQueryService query = new OdfQueryService();
        OdfSearchMatch[] oldReferences = query.QueryReferences(
            source,
            "Resources/used.bin");
        OdfSearchMatch[] movedReferences = query.QueryReferences(
            source,
            "Resources/moved.bin");
        OdfSearchMatch[] hrefs = query.QueryField(
            source,
            Name(OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix),
            "Resources/moved.bin",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(uniquePath, Is.EqualTo("Resources/used-2.bin"));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removedCopy.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(recordedUnused, Is.EqualTo("Resources/orphan.bin"));
            Assert.That(source.OrphanedResourcePaths, Is.Empty);
            Assert.That(collected.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasEntry(source, "Resources/orphan.bin"), Is.False);
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasEntry(source, "Resources/moved.bin"), Is.True);
            Assert.That(AttributeValue(
                text,
                OdfNamespaces.XLink,
                "href"), Is.EqualTo("Resources/moved.bin"));
            Assert.That(oldReferences, Is.Empty);
            Assert.That(movedReferences, Has.Length.EqualTo(1));
            Assert.That(hrefs, Has.Length.EqualTo(1));
            Assert.That(hrefs[0].IsExact, Is.True);
        });
    }

    [Test]
    public void MaintainsIdentifiersAndPreparesCollisionSafeCopies()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument document = ReadFlat(
            "<office:body><office:text>"
                + "<draw:g draw:name='Hero' xml:id='group'>"
                + "<draw:rect draw:start-shape='group'/>"
                + "</draw:g></office:text></office:body>");
        OdfNode source = Descendant(
            document.Parts[0].Root!,
            OdfNamespaces.Drawing,
            "g");

        string uniqueName = editor.CreateUniqueDocumentName(
            document,
            "Hero");
        string uniqueIdentifier = editor.CreateUniqueDocumentIdentifier(
            document,
            "group");
        OdfNode copied = editor.PrepareCopiedDocumentSubtree(
            document,
            source);
        OdfEditResult renamed = editor.RenameDocumentIdentifier(
            document,
            "group",
            "renamed");

        Assert.Multiple(() =>
        {
            Assert.That(uniqueName, Is.EqualTo("Hero Copy 2"));
            Assert.That(uniqueIdentifier, Is.EqualTo("group-2"));
            Assert.That(copied, Is.Not.SameAs(source));
            Assert.That(AttributeValue(
                copied,
                OdfNamespaces.Xml,
                "id"), Is.EqualTo("group-2"));
            Assert.That(AttributeValue(
                copied,
                OdfNamespaces.Drawing,
                "name"), Is.EqualTo("Hero Copy 2"));
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(AttributeValue(
                source,
                OdfNamespaces.Xml,
                "id"), Is.EqualTo("renamed"));
            Assert.That(AttributeValue(
                source.Children[0],
                OdfNamespaces.Drawing,
                "start-shape"), Is.EqualTo("renamed"));
        });
    }

    [Test]
    public void ProposesAndAppliesExplicitRepairs()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        new OdfPackageWriter().RemovePackageEntry(
            document.Package!,
            OdfPartNames.Mimetype);
        document.PackageEntries = document.Package!.Entries;

        OdfRepairAnalysis analysis = editor.ProposeDocumentRepairs(document);
        OdfRepairProposal proposal = FindRepair(
            analysis,
            OdfRepairKind.PackageMimetype);
        OdfRepairResult result = editor.ApplyDocumentRepairs(
            document,
            [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(proposal.IsSafe, Is.True);
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Mutations, Has.Length.EqualTo(1));
            Assert.That(HasEntry(document, OdfPartNames.Mimetype), Is.True);
        });
    }

    static OdfValidationOptions CommitOptions()
    {
        return new OdfValidationOptions
        {
            Mode = OdfValidationMode.PackageOnly,
            ValidateXml = false,
            ValidateSemantics = false,
            ValidateFormulas = false,
            ValidateSecurity = false
        };
    }

    static OdfDocument ReadFlat(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:draw='" + OdfNamespaces.Drawing
            + "' xmlns:xlink='" + OdfNamespaces.XLink
            + "' xmlns:xml='" + OdfNamespaces.Xml
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'>" + content + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "document-editor.fodt",
            new OdfLoadOptions());
    }

    static OdfNode[] ReadNodes(string xml, string partPath)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            new OdfLoadOptions());
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }

    static OdfNode Descendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        throw new AssertionException("The expected descendant was not found.");
    }

    static OdfNode? FindDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static bool HasPart(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].Path == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasEntry(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            if (document.PackageEntries[index].Path == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfRepairProposal FindRepair(
        OdfRepairAnalysis analysis,
        OdfRepairKind kind)
    {
        int index = 0;
        while (index < analysis.Proposals.Length)
        {
            if (analysis.Proposals[index].Kind == kind)
            {
                return analysis.Proposals[index];
            }
            index = index + 1;
        }
        throw new AssertionException("The expected repair was not proposed.");
    }
}
