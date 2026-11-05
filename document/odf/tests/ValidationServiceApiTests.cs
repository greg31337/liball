namespace odf.Tests;

public class ValidationServiceApiTests
{
    [Test]
    public void PublicValidatorRunsTheConfiguredValidationPipeline()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode root = document.Parts[0].Root!;
        OdfQualifiedName rootName = root.Name!;
        rootName.LocalName = "not-an-office-document";
        OdfValidationOptions options = new OdfValidationOptions
        {
            Mode = OdfValidationMode.XmlOnly
        };
        IOdfValidator validator = new OdfValidator();

        OdfDiagnostic[] diagnostics = validator.ValidateDocument(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Not.Empty);
            Assert.That(
                diagnostics.Select(item => item.Code),
                Does.Contain(OdfDiagnosticCodes.UnexpectedElement));
            Assert.That(rootName.LocalName, Is.EqualTo("not-an-office-document"));
        });
    }

    [Test]
    public void PublicRepairServicePreservesExplicitProposalAndMutationFlow()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode root = document.Parts[0].Root!;
        OdfNode target = Element("target");
        target.XmlId = "target";
        target.Attributes =
        [
            new OdfAttribute
            {
                Name = new OdfQualifiedName
                {
                    NamespaceName = OdfNamespaces.Xml,
                    LocalName = "id",
                    Prefix = OdfNamespaces.XmlPrefix
                },
                Value = "target"
            }
        ];
        OdfNode source = Element("source");
        OdfAttribute reference = new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                LocalName = "target-id"
            },
            Value = "#target"
        };
        source.Attributes = [reference];
        AppendChildren(root, [target, source]);
        IOdfRepairService service = new OdfRepairService();

        OdfRepairAnalysis analysis = service.ProposeRepairs(document);
        OdfRepairProposal proposal = analysis.Proposals.Single(
            item => item.Kind == OdfRepairKind.BrokenReference);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.SafeProposalCount, Is.EqualTo(1));
            Assert.That(analysis.UnsafeProposalCount, Is.Zero);
            Assert.That(proposal.Id, Is.EqualTo("repair-1"));
            Assert.That(proposal.IsSafe, Is.True);
            Assert.That(proposal.IsApplied, Is.False);
            Assert.That(proposal.BeforeValue, Is.EqualTo("#target"));
            Assert.That(proposal.AfterValue, Is.EqualTo("target"));
            Assert.That(reference.Value, Is.EqualTo("#target"));
        });

        OdfRepairResult applied = service.ApplyRepairs(
            document,
            [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(applied.Changed, Is.True);
            Assert.That(applied.AppliedProposalCount, Is.EqualTo(1));
            Assert.That(applied.SkippedProposalCount, Is.Zero);
            Assert.That(applied.Mutations, Has.Length.EqualTo(1));
            Assert.That(applied.Mutations[0].ProposalId, Is.EqualTo(proposal.Id));
            Assert.That(applied.Mutations[0].Kind, Is.EqualTo(OdfRepairKind.BrokenReference));
            Assert.That(applied.Mutations[0].BeforeValue, Is.EqualTo("#target"));
            Assert.That(applied.Mutations[0].AfterValue, Is.EqualTo("target"));
            Assert.That(applied.Diagnostics, Is.Empty);
            Assert.That(proposal.IsApplied, Is.True);
            Assert.That(reference.Value, Is.EqualTo("target"));
        });

        OdfRepairResult replay = service.ApplyRepairs(document, [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(replay.Changed, Is.False);
            Assert.That(replay.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.EditConflict));
        });
    }

    [Test]
    public void PublicVersionServiceExposesOrderedStructuredMigrationChanges()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.VersionInfo = null;
        foreach (OdfPart part in document.Parts)
        {
            part.VersionInfo = null;
        }
        IOdfVersionService service = new OdfVersionService();

        OdfVersionAnalysis analysis = service.AnalyzeVersionMigration(
            document,
            OdfVersion.Version14);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.IsValid, Is.True);
            Assert.That(analysis.IsSupported, Is.True);
            Assert.That(analysis.IsUpgrade, Is.True);
            Assert.That(analysis.IsDowngrade, Is.False);
            Assert.That(analysis.CanConvertWithoutLoss, Is.True);
            Assert.That(analysis.VersionWasInferred, Is.True);
            Assert.That(analysis.SourceVersion, Is.EqualTo(OdfVersion.Version10));
            Assert.That(analysis.TargetVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(analysis.Steps, Has.Length.EqualTo(4));
            Assert.That(analysis.Steps[0].SourceVersion, Is.EqualTo(OdfVersion.Version10));
            Assert.That(analysis.Steps[3].TargetVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(analysis.Steps.All(item => item.Changes.Length > 0), Is.True);
            Assert.That(
                analysis.Steps.SelectMany(item => item.Changes)
                    .All(item => item.Kind != OdfVersionChangeKind.Unknown),
                Is.True);
            Assert.That(
                analysis.Steps.SelectMany(item => item.Changes)
                    .All(item => item.Category != OdfVersionChangeCategory.Unknown),
                Is.True);
        });
    }

    [Test]
    public void PublicVersionValidationReportsLocatedPartVersionConflicts()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Parts[0].VersionInfo = new OdfVersionInfo
        {
            LexicalValue = "1.3",
            Version = OdfVersion.Version13,
            Major = 1,
            Minor = 3,
            IsValid = true
        };
        OdfValidationOptions options = new OdfValidationOptions
        {
            Version = OdfVersion.Version14
        };
        IOdfVersionService service = new OdfVersionService();

        OdfVersionAnalysis analysis = service.ValidateDocumentVersion(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.IsValid, Is.False);
            Assert.That(analysis.Diagnostics, Is.Not.Empty);
            Assert.That(analysis.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.VersionUnsupported));
            Assert.That(analysis.Diagnostics[0].Location.PartPath, Is.EqualTo(OdfPartNames.Content));
        });
    }

    private static OdfNode Element(string localName)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                LocalName = localName
            }
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
}
