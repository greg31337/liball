using System.Text;

namespace odf.Tests;

public class ValidationServicesContractTests
{
    [TestCase(OdfValidationMode.Strict, true, false, false)]
    [TestCase(OdfValidationMode.Complete, true, true, true)]
    [TestCase(OdfValidationMode.Recovery, true, true, true)]
    [TestCase(OdfValidationMode.PackageOnly, true, false, false)]
    [TestCase(OdfValidationMode.XmlOnly, false, true, false)]
    [TestCase(OdfValidationMode.SemanticOnly, false, false, true)]
    [TestCase(OdfValidationMode.Profile, true, true, true)]
    public void ValidatorExecutesEveryValidationMode(
        OdfValidationMode mode,
        bool expectsPackage,
        bool expectsXml,
        bool expectsSemantics)
    {
        OdfValidationOptions options = new OdfValidationOptions
        {
            Mode = mode,
            Version = OdfVersion.Version14
        };
        IOdfValidator validator = new OdfValidator();

        OdfDiagnostic[] diagnostics = validator.ValidateDocument(
            InvalidAtEveryLayer(),
            options);
        string[] codes = diagnostics.Select(item => item.Code).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                codes.Contains(OdfDiagnosticCodes.InvalidPackage),
                Is.EqualTo(expectsPackage));
            Assert.That(
                codes.Contains(OdfDiagnosticCodes.UnexpectedElement),
                Is.EqualTo(expectsXml));
            Assert.That(
                codes.Contains(OdfDiagnosticCodes.InvalidStyleReference),
                Is.EqualTo(expectsSemantics));
            if (mode == OdfValidationMode.Strict)
            {
                Assert.That(diagnostics, Has.Length.EqualTo(1));
            }
            if (mode == OdfValidationMode.Recovery)
            {
                Assert.That(
                    diagnostics.All(
                        item => item.Severity == OdfValidationSeverity.Warning),
                    Is.True);
            }
        });
    }

    [TestCase(OdfValidationSeverity.Information)]
    [TestCase(OdfValidationSeverity.Warning)]
    [TestCase(OdfValidationSeverity.Error)]
    [TestCase(OdfValidationSeverity.Fatal)]
    public void ValidatorPreservesEveryDiagnosticField(
        OdfValidationSeverity severity)
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfDiagnostic source = new OdfDiagnostic
        {
            Severity = severity,
            Code = "ODF-CONTRACT-181",
            Message = "contract diagnostic",
            Location = CompleteLocation("content.xml", "/office:document[1]"),
            RelatedLocation = CompleteLocation("styles.xml", "/office:styles[1]"),
            HasRelatedLocation = true,
            Version = OdfVersion.Version13,
            SpecificationSection = "contract section",
            IsRecoverable = severity != OdfValidationSeverity.Fatal
        };
        document.Package!.Diagnostics = [source];
        OdfValidationOptions options = new OdfValidationOptions
        {
            Mode = OdfValidationMode.PackageOnly,
            IncludeWarnings = true,
            IncludeInformation = true
        };
        IOdfValidator validator = new OdfValidator();

        OdfDiagnostic diagnostic = validator.ValidateDocument(document, options)
            .Single(item => item.Code == source.Code);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic, Is.Not.SameAs(source));
            Assert.That(diagnostic.Severity, Is.EqualTo(source.Severity));
            Assert.That(diagnostic.Code, Is.EqualTo(source.Code));
            Assert.That(diagnostic.Message, Is.EqualTo(source.Message));
            AssertLocation(diagnostic.Location, source.Location);
            Assert.That(diagnostic.HasRelatedLocation, Is.True);
            AssertLocation(diagnostic.RelatedLocation, source.RelatedLocation);
            Assert.That(diagnostic.Version, Is.EqualTo(source.Version));
            Assert.That(
                diagnostic.SpecificationSection,
                Is.EqualTo(source.SpecificationSection));
            Assert.That(diagnostic.IsRecoverable, Is.EqualTo(source.IsRecoverable));
        });
    }

    [TestCase(OdfCompatibilityProfiles.StrictOasis, 7)]
    [TestCase(OdfCompatibilityProfiles.Interoperable, 7)]
    [TestCase(OdfCompatibilityProfiles.LibreOfficeExtended, 3)]
    [TestCase(OdfCompatibilityProfiles.OpenOfficeExtended, 6)]
    [TestCase(OdfCompatibilityProfiles.MicrosoftOfficeExtended, 5)]
    [TestCase(OdfCompatibilityProfiles.PreserveExisting, 0)]
    public void ValidatorAppliesEveryCompatibilityProfile(
        string profile,
        int expectedDiagnostics)
    {
        OdfValidationOptions options = new OdfValidationOptions
        {
            Mode = OdfValidationMode.Profile,
            CompatibilityProfile = profile,
            ValidatePackage = false,
            ValidateXml = false,
            ValidateSemantics = false
        };
        IOdfValidator validator = new OdfValidator();

        OdfDiagnostic[] diagnostics = validator.ValidateDocument(
            ExtensionDocument(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(expectedDiagnostics));
            Assert.That(
                diagnostics.All(
                    item => item.Code
                        == OdfDiagnosticCodes.FeatureNotAvailable),
                Is.True);
            Assert.That(
                diagnostics.All(
                    item => item.Message.Contains(profile)),
                Is.True);
        });
    }

    [Test]
    public void RepairServiceExposesAndAppliesEveryPackageRepair()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage package = document.Package!;
        IOdfPackageWriter writer = new OdfPackageWriter();
        IOdfRepairService service = new OdfRepairService();

        writer.RemovePackageEntry(package, OdfPartNames.Mimetype);
        AssertPackageRepair(
            service,
            document,
            OdfRepairKind.PackageMimetype,
            OdfPartNames.Mimetype);

        writer.RemovePackageEntry(package, OdfPartNames.Manifest);
        AssertPackageRepair(
            service,
            document,
            OdfRepairKind.PackageManifest,
            OdfPartNames.Manifest);

        RemoveManifestEntry(package.Manifest!, OdfPartNames.Content);
        AssertPackageRepair(
            service,
            document,
            OdfRepairKind.ManifestSynchronization,
            OdfPartNames.Manifest);
    }

    [Test]
    public void RepairServiceExposesAndAppliesEveryXmlRepair()
    {
        OdfDocument document = RepairableXmlDocument();
        IOdfRepairService service = new OdfRepairService();

        OdfRepairAnalysis analysis = service.ProposeRepairs(document);
        OdfRepairKind[] kinds = analysis.Proposals
            .Select(item => item.Kind)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(analysis.SafeProposalCount, Is.EqualTo(4));
            Assert.That(analysis.UnsafeProposalCount, Is.Zero);
            Assert.That(
                kinds,
                Is.EquivalentTo(new[]
                {
                    OdfRepairKind.NamespaceBinding,
                    OdfRepairKind.XmlIdCache,
                    OdfRepairKind.XmlIdValue,
                    OdfRepairKind.BrokenReference
                }));
            Assert.That(analysis.Proposals.All(item => item.Id.Length > 0), Is.True);
            Assert.That(
                analysis.Proposals.All(item => item.Description.Length > 0),
                Is.True);
            Assert.That(analysis.Proposals.All(item => item.IsSafe), Is.True);
            Assert.That(analysis.Proposals.All(item => !item.IsApplied), Is.True);
        });

        OdfRepairResult result = service.ApplyRepairs(
            document,
            analysis.Proposals);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.AppliedProposalCount, Is.EqualTo(4));
            Assert.That(result.SkippedProposalCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Mutations, Has.Length.EqualTo(5));
            Assert.That(
                result.Mutations.Select(item => item.Kind),
                Does.Contain(OdfRepairKind.NamespaceBinding));
            Assert.That(
                result.Mutations.Select(item => item.Kind),
                Does.Contain(OdfRepairKind.XmlIdCache));
            Assert.That(
                result.Mutations.Select(item => item.Kind),
                Does.Contain(OdfRepairKind.XmlIdValue));
            Assert.That(
                result.Mutations.Select(item => item.Kind),
                Does.Contain(OdfRepairKind.BrokenReference));
            Assert.That(
                result.Mutations.All(item => item.ProposalId.Length > 0),
                Is.True);
            Assert.That(
                result.Mutations.All(item => item.Description.Length > 0),
                Is.True);
            Assert.That(analysis.Proposals.All(item => item.IsApplied), Is.True);
        });
    }

    [Test]
    public void RepairServiceSkipsUnsafeAndRejectsStaleRepairs()
    {
        OdfDocument unsafeDocument = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode unsafeRoot = unsafeDocument.Parts[0].Root!;
        OdfNode conflicting = Element("urn:example:first", "item", "shared");
        conflicting.Attributes =
        [
            Attribute("urn:example:second", "value", "shared", "x")
        ];
        AppendChildren(unsafeRoot, [conflicting]);
        IOdfRepairService service = new OdfRepairService();
        OdfRepairAnalysis unsafeAnalysis = service.ProposeRepairs(unsafeDocument);
        OdfRepairProposal unsafeProposal = unsafeAnalysis.Proposals.Single();

        OdfRepairResult skipped = service.ApplyRepairs(
            unsafeDocument,
            unsafeAnalysis.Proposals);

        Assert.Multiple(() =>
        {
            Assert.That(unsafeProposal.Kind, Is.EqualTo(OdfRepairKind.NamespaceBinding));
            Assert.That(unsafeProposal.IsSafe, Is.False);
            Assert.That(unsafeAnalysis.SafeProposalCount, Is.Zero);
            Assert.That(unsafeAnalysis.UnsafeProposalCount, Is.EqualTo(1));
            Assert.That(skipped.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(skipped.Changed, Is.False);
            Assert.That(skipped.AppliedProposalCount, Is.Zero);
            Assert.That(skipped.SkippedProposalCount, Is.EqualTo(1));
            Assert.That(skipped.Mutations, Is.Empty);
            Assert.That(skipped.Diagnostics, Is.Empty);
        });

        OdfDocument staleDocument = RepairableReferenceDocument();
        OdfRepairProposal staleProposal = service.ProposeRepairs(staleDocument)
            .Proposals.Single();
        OdfAttribute reference = staleDocument.Parts[0].Root!
            .Children.Last().Attributes.Single();
        reference.Value = "#changed";

        OdfRepairResult conflict = service.ApplyRepairs(
            staleDocument,
            [staleProposal]);

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(conflict.Changed, Is.False);
            Assert.That(conflict.AppliedProposalCount, Is.Zero);
            Assert.That(conflict.SkippedProposalCount, Is.EqualTo(1));
            Assert.That(conflict.Mutations, Is.Empty);
            Assert.That(conflict.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                conflict.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.EditConflict));
            AssertLocation(
                conflict.Diagnostics[0].Location,
                staleProposal.Location);
            Assert.That(reference.Value, Is.EqualTo("#changed"));
        });
    }

    [Test]
    public void VersionServiceAnalyzesEveryKnownVersionMigration()
    {
        OdfVersion[] versions =
        [
            OdfVersion.Version10,
            OdfVersion.Version11,
            OdfVersion.Version12,
            OdfVersion.Version13,
            OdfVersion.Version14
        ];
        IOdfVersionService service = new OdfVersionService();

        foreach (OdfVersion source in versions)
        {
            foreach (OdfVersion target in versions)
            {
                OdfDocument document = VersionedDocument(source);

                OdfVersionAnalysis analysis = service.AnalyzeVersionMigration(
                    document,
                    target);
                int expectedSteps = Math.Abs((int)target - (int)source);

                Assert.Multiple(() =>
                {
                    Assert.That(analysis.IsSupported, Is.True);
                    Assert.That(analysis.IsValid, Is.True);
                    Assert.That(analysis.SourceVersion, Is.EqualTo(source));
                    Assert.That(analysis.TargetVersion, Is.EqualTo(target));
                    Assert.That(
                        analysis.IsUpgrade,
                        Is.EqualTo((int)source < (int)target));
                    Assert.That(
                        analysis.IsDowngrade,
                        Is.EqualTo((int)source > (int)target));
                    Assert.That(analysis.CanConvertWithoutLoss, Is.True);
                    Assert.That(analysis.PreservesFutureContent, Is.False);
                    Assert.That(analysis.VersionWasInferred, Is.False);
                    Assert.That(analysis.Steps, Has.Length.EqualTo(expectedSteps));
                    Assert.That(analysis.Diagnostics, Is.Empty);
                });

                OdfVersion current = source;
                foreach (OdfVersionMigrationStep step in analysis.Steps)
                {
                    int direction = analysis.IsDowngrade ? -1 : 1;
                    OdfVersion next = (OdfVersion)((int)current + direction);
                    Assert.Multiple(() =>
                    {
                        Assert.That(step.SourceVersion, Is.EqualTo(current));
                        Assert.That(step.TargetVersion, Is.EqualTo(next));
                        Assert.That(step.IsDowngrade, Is.EqualTo(analysis.IsDowngrade));
                        Assert.That(step.Changes, Is.Not.Empty);
                        Assert.That(
                            step.Changes.All(
                                item => item.Kind != OdfVersionChangeKind.Unknown),
                            Is.True);
                        Assert.That(
                            step.Changes.All(
                                item => item.Category
                                    != OdfVersionChangeCategory.Unknown),
                            Is.True);
                        Assert.That(
                            step.AddedCount
                                + step.RemovedCount
                                + step.ChangedCount,
                            Is.GreaterThan(0));
                        Assert.That(step.IncompatibleConstructCount, Is.Zero);
                    });
                    current = next;
                }
            }
        }
    }

    [Test]
    public void VersionServiceReportsLocatedDowngradeLoss()
    {
        OdfDocument document = VersionedDocument(OdfVersion.Version14);
        OdfAttribute attribute = new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Drawing,
                LocalName = "decorative",
                Prefix = OdfNamespaces.DrawingPrefix
            },
            Value = "true",
            Location = CompleteLocation(
                OdfPartNames.Content,
                "/office:document-content[1]/@draw:decorative")
        };
        OdfNode root = document.Parts[0].Root!;
        root.Attributes = [.. root.Attributes, attribute];
        IOdfVersionService service = new OdfVersionService();

        OdfVersionAnalysis analysis = service.AnalyzeVersionMigration(
            document,
            OdfVersion.Version13);
        OdfDiagnostic diagnostic = analysis.Diagnostics.Single(
            item => item.Code == OdfDiagnosticCodes.DowngradeLoss);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.IsValid, Is.False);
            Assert.That(analysis.IsSupported, Is.True);
            Assert.That(analysis.IsUpgrade, Is.False);
            Assert.That(analysis.IsDowngrade, Is.True);
            Assert.That(analysis.CanConvertWithoutLoss, Is.False);
            Assert.That(analysis.SourceVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(analysis.TargetVersion, Is.EqualTo(OdfVersion.Version13));
            Assert.That(analysis.Steps, Has.Length.EqualTo(1));
            Assert.That(analysis.Steps[0].IsDowngrade, Is.True);
            Assert.That(
                analysis.Steps[0].IncompatibleConstructCount,
                Is.EqualTo(1));
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Error));
            Assert.That(diagnostic.Message, Does.Contain("decorative"));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(diagnostic.Location.NodePath, Does.Contain("@draw:decorative"));
            Assert.That(diagnostic.Location.Line, Is.EqualTo(11));
            Assert.That(diagnostic.Location.Column, Is.EqualTo(13));
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(diagnostic.SpecificationSection, Is.Not.Empty);
            Assert.That(diagnostic.IsRecoverable, Is.False);
        });
    }

    [Test]
    public void VersionServiceValidatesTargetsAndFutureVersionPreservation()
    {
        IOdfVersionService service = new OdfVersionService();
        OdfDocument document = VersionedDocument(OdfVersion.Version13);
        OdfValidationOptions options = new OdfValidationOptions
        {
            Version = OdfVersion.Version14
        };

        OdfVersionAnalysis validation = service.ValidateDocumentVersion(
            document,
            options);

        OdfDocument future = VersionedDocument(OdfVersion.Future);
        future.IsFutureVersion = true;
        foreach (OdfPart part in future.Parts)
        {
            part.IsFutureVersion = true;
        }
        OdfVersionAnalysis preservation = service.AnalyzeVersionMigration(
            future,
            OdfVersion.Future);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.IsUpgrade, Is.True);
            Assert.That(validation.TargetVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(validation.Steps, Has.Length.EqualTo(1));
            Assert.That(preservation.IsValid, Is.True);
            Assert.That(preservation.IsSupported, Is.False);
            Assert.That(preservation.PreservesFutureContent, Is.True);
            Assert.That(preservation.CanConvertWithoutLoss, Is.False);
            Assert.That(preservation.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                preservation.Diagnostics[0].Severity,
                Is.EqualTo(OdfValidationSeverity.Warning));
        });
    }

    static void AssertPackageRepair(
        IOdfRepairService service,
        OdfDocument document,
        OdfRepairKind kind,
        string partPath)
    {
        OdfRepairAnalysis analysis = service.ProposeRepairs(document);
        OdfRepairProposal proposal = analysis.Proposals.Single(
            item => item.Kind == kind);

        Assert.Multiple(() =>
        {
            Assert.That(proposal.Id, Is.Not.Empty);
            Assert.That(proposal.Kind, Is.EqualTo(kind));
            Assert.That(proposal.Description, Is.Not.Empty);
            Assert.That(proposal.Location.PartPath, Is.EqualTo(partPath));
            Assert.That(proposal.IsSafe, Is.True);
            Assert.That(proposal.IsApplied, Is.False);
            Assert.That(proposal.AfterValue, Is.Not.Empty);
        });

        OdfRepairResult result = service.ApplyRepairs(document, [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.AppliedProposalCount, Is.EqualTo(1));
            Assert.That(result.SkippedProposalCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Mutations, Has.Length.EqualTo(1));
            Assert.That(result.Mutations[0].ProposalId, Is.EqualTo(proposal.Id));
            Assert.That(result.Mutations[0].Kind, Is.EqualTo(kind));
            Assert.That(result.Mutations[0].Description, Is.Not.Empty);
            Assert.That(result.Mutations[0].Location.PartPath, Is.EqualTo(partPath));
            if (kind == OdfRepairKind.PackageManifest)
            {
                Assert.That(result.Mutations[0].BeforeValue, Is.Empty);
                Assert.That(
                    result.Mutations[0].AfterValue,
                    Is.EqualTo("manifest.xml"));
            }
            else
            {
                Assert.That(
                    result.Mutations[0].BeforeValue,
                    Is.EqualTo(proposal.BeforeValue));
                Assert.That(
                    result.Mutations[0].AfterValue,
                    Is.EqualTo(proposal.AfterValue));
            }
            Assert.That(proposal.IsApplied, Is.True);
        });
    }

    static OdfLocation CompleteLocation(string partPath, string nodePath)
    {
        return new OdfLocation
        {
            PartPath = partPath,
            NodePath = nodePath,
            XmlId = "contract-id",
            Line = 11,
            Column = 13,
            ByteOffset = 101,
            TextOffset = 17,
            TextLength = 19
        };
    }

    static void AssertLocation(OdfLocation actual, OdfLocation expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.SameAs(expected));
            Assert.That(actual.PartPath, Is.EqualTo(expected.PartPath));
            Assert.That(actual.NodePath, Is.EqualTo(expected.NodePath));
            Assert.That(actual.XmlId, Is.EqualTo(expected.XmlId));
            Assert.That(actual.Line, Is.EqualTo(expected.Line));
            Assert.That(actual.Column, Is.EqualTo(expected.Column));
            Assert.That(actual.ByteOffset, Is.EqualTo(expected.ByteOffset));
            Assert.That(actual.TextOffset, Is.EqualTo(expected.TextOffset));
            Assert.That(actual.TextLength, Is.EqualTo(expected.TextLength));
        });
    }

    static OdfDocument InvalidAtEveryLayer()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Package!.IsValid = false;
        OdfPart part = document.Parts[0];
        part.Path = OdfPartNames.Styles;
        OdfNode root = part.Root!;
        root.NamespaceDeclarations =
        [
            .. root.NamespaceDeclarations,
            new OdfNamespaceDeclaration
            {
                Prefix = OdfNamespaces.TextPrefix,
                NamespaceName = OdfNamespaces.Text
            }
        ];
        OdfNode text = root.Children[0].Children[0];
        OdfNode paragraph = Element(
            OdfNamespaces.Text,
            "p",
            OdfNamespaces.TextPrefix);
        paragraph.Attributes =
        [
            Attribute(
                OdfNamespaces.Text,
                "style-name",
                OdfNamespaces.TextPrefix,
                "MissingStyle")
        ];
        AppendChildren(text, [paragraph]);
        return document;
    }

    static OdfDocument ExtensionDocument()
    {
        string xml = "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:lo='urn:org:documentfoundation:names:experimental:office:xmlns:loext:1.0'"
            + " xmlns:oo='http://openoffice.org/2009/office'"
            + " xmlns:ms='http://schemas.microsoft.com/office/word/2012/wordml'"
            + " xmlns:ext='urn:example:extension' office:version='1.4'>"
            + "<lo:root-feature/><office:body><office:text><text:p lo:flag='yes' ms:flag='yes'>"
            + "<lo:feature/><oo:feature/><ms:feature/><ext:feature/>"
            + "</text:p></office:text></office:body></office:document>";
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            "profile.fodt",
            new OdfLoadOptions());
        return OdfDocumentAssembly.AssembleFlatDocument(nodes);
    }

    static OdfDocument RepairableXmlDocument()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode root = document.Parts[0].Root!;
        OdfNode namespaceNode = Element(
            "urn:example:repair",
            "widget",
            "ext");
        OdfNode invalidIdNode = Element("", "invalid-id", "");
        invalidIdNode.Attributes =
        [
            Attribute(
                OdfNamespaces.Xml,
                "id",
                OdfNamespaces.XmlPrefix,
                "1bad")
        ];
        invalidIdNode.XmlId = "stale-cache";
        OdfNode invalidReferenceNode = Element("", "invalid-reference", "");
        invalidReferenceNode.Attributes =
        [
            Attribute("", "target-id", "", "1bad")
        ];
        OdfNode target = IdElement("target", "good");
        OdfNode brokenReferenceNode = Element("", "broken-reference", "");
        brokenReferenceNode.Attributes =
        [
            Attribute("", "target-id", "", "#good")
        ];
        AppendChildren(
            root,
            [
                namespaceNode,
                invalidIdNode,
                invalidReferenceNode,
                target,
                brokenReferenceNode
            ]);
        return document;
    }

    static OdfDocument RepairableReferenceDocument()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        OdfNode root = document.Parts[0].Root!;
        OdfNode target = IdElement("target", "target");
        OdfNode source = Element("", "source", "");
        source.Attributes = [Attribute("", "target-id", "", "#target")];
        AppendChildren(root, [target, source]);
        return document;
    }

    static OdfDocument VersionedDocument(OdfVersion version)
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfVersionInfo info = VersionInfo(version);
        document.VersionInfo = info;
        foreach (OdfPart part in document.Parts)
        {
            part.VersionInfo = VersionInfo(version);
        }
        return document;
    }

    static OdfVersionInfo VersionInfo(OdfVersion version)
    {
        int minor = (int)version - 1;
        string lexical = "1." + minor;
        if (version == OdfVersion.Future)
        {
            minor = 5;
            lexical = "1.5";
        }
        return new OdfVersionInfo
        {
            LexicalValue = lexical,
            Version = version,
            Major = 1,
            Minor = minor,
            IsValid = true,
            IsFuture = version == OdfVersion.Future
        };
    }

    static OdfNode IdElement(string localName, string id)
    {
        OdfNode result = Element("", localName, "");
        result.Attributes =
        [
            Attribute(OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, id)
        ];
        result.XmlId = id;
        return result;
    }

    static OdfNode Element(
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

    static OdfAttribute Attribute(
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

    static void AppendChildren(OdfNode parent, OdfNode[] additions)
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

    static void RemoveManifestEntry(OdfManifest manifest, string path)
    {
        manifest.Entries = manifest.Entries
            .Where(item => item.FullPath != path)
            .ToArray();
    }
}
