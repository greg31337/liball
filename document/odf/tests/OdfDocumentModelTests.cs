using System.Text;

namespace odf.Tests;

public class OdfDocumentModelTests
{
    [Test]
    public void SeparateAssemblyPublishesStandardPartsAndDocumentState()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            ReadPart(
                OdfPartNames.Content,
                "document-content",
                "1.3",
                "<office:scripts/><office:automatic-styles/>"
                    + "<ext:content-extension/><office:body><office:text/></office:body>"),
            ReadPart(
                OdfPartNames.Styles,
                "document-styles",
                "1.3",
                "<office:styles/><office:master-styles/>"),
            ReadPart(
                OdfPartNames.Metadata,
                "document-meta",
                "1.3",
                "<office:meta><meta:generator>Example Producer</meta:generator></office:meta>"),
            ReadPart(
                OdfPartNames.Settings,
                "document-settings",
                "1.3",
                "<office:settings/>"));
        OdfLoadOptions loadOptions = new OdfLoadOptions();
        OdfSaveOptions saveOptions = new OdfSaveOptions();

        OdfDocumentModel.ApplyContext(
            document,
            loadOptions,
            saveOptions,
            OdfMediaTypes.TextTemplate);

        Assert.Multiple(() =>
        {
            Assert.That(document.Parts.Select(part => part.Path), Is.EqualTo(new[]
            {
                OdfPartNames.Content,
                OdfPartNames.Styles,
                OdfPartNames.Metadata,
                OdfPartNames.Settings
            }));
            Assert.That(document.Parts.All(part => part.MediaType == OdfMediaTypes.Xml), Is.True);
            Assert.That(document.Parts[0].RootName, Is.EqualTo("document-content"));
            Assert.That(document.Parts[0].Scripts, Is.Not.Null);
            Assert.That(document.Parts[0].AutomaticStyles, Is.Not.Null);
            Assert.That(document.Parts[0].Body, Is.SameAs(document.Body));
            Assert.That(document.Parts[0].PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(document.Parts[1].Styles, Is.Not.Null);
            Assert.That(document.Parts[1].MasterStyles, Is.Not.Null);
            Assert.That(document.Parts[2].Metadata, Is.Not.Null);
            Assert.That(document.Parts[3].Settings, Is.Not.Null);
            Assert.That(document.MediaType, Is.EqualTo(OdfMediaTypes.TextTemplate));
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.TextTemplate));
            Assert.That(document.VersionInfo?.Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(document.VersionInfo?.Producer, Is.EqualTo("Example Producer"));
            Assert.That(document.PassthroughNodes, Is.EqualTo(document.Parts[0].PassthroughNodes));
            Assert.That(document.IsFutureVersion, Is.False);
        });
    }

    [TestCase("text", "", OdfDocumentKind.Text)]
    [TestCase("spreadsheet", "", OdfDocumentKind.Spreadsheet)]
    [TestCase("presentation", "", OdfDocumentKind.Presentation)]
    [TestCase("drawing", "", OdfDocumentKind.Drawing)]
    [TestCase("chart", "", OdfDocumentKind.Chart)]
    [TestCase("image", "", OdfDocumentKind.Image)]
    [TestCase("text", "application/vnd.oasis.opendocument.text-master-template", OdfDocumentKind.TextMasterTemplate)]
    [TestCase("spreadsheet", "application/vnd.oasis.opendocument.spreadsheet-template", OdfDocumentKind.SpreadsheetTemplate)]
    public void DetectsStandardBodyKindsAndCompatibleTemplateMediaTypes(
        string bodyName,
        string mediaType,
        OdfDocumentKind expected)
    {
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(
            ReadFlat(
                "1.4",
                mediaType,
                "<office:body><office:" + bodyName + "/></office:body>"));

        Assert.Multiple(() =>
        {
            Assert.That(document.Kind, Is.EqualTo(expected));
            Assert.That(document.MediaType, Is.EqualTo(mediaType));
            Assert.That(document.Parts, Has.Length.EqualTo(1));
            Assert.That(document.Parts[0].IsFlatDocument, Is.True);
            Assert.That(document.Parts[0].Path, Is.Empty);
            Assert.That(document.Parts[0].MediaType, Is.EqualTo(mediaType));
        });
    }

    [Test]
    public void BodyKindTakesPrecedenceOverMismatchedMediaTypeFamily()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(
            ReadFlat(
                "1.4",
                OdfMediaTypes.Spreadsheet,
                "<office:body><office:text/></office:body>"));

        Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.Text));
    }

    [Test]
    public void RetainsIndependentSnapshotsOfLoadAndSaveOptions()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(
            ReadFlat(
                "1.4",
                OdfMediaTypes.Text,
                "<office:body><office:text/></office:body>"));
        OdfLoadOptions loadOptions = new OdfLoadOptions
        {
            AllowRecovery = true,
            PreserveUnknownContent = false,
            LeaveInputOpen = false,
            SecurityLimits = new OdfSecurityLimits
            {
                MaximumXmlDepth = 19
            },
            Cancellation = new OdfCancellationState
            {
                IsCancellationRequested = true,
                Reason = "load snapshot"
            },
            StreamSelection = new OdfStreamSelection
            {
                PartPaths = [OdfPartNames.Content],
                ElementNames =
                [
                    new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Text,
                        LocalName = "p",
                        Prefix = "text"
                    }
                ],
                IncludeCommentEvents = false
            }
        };
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            Deterministic = true,
            IndentXml = true,
            PreserveUnknownContent = false,
            CompatibilityProfile = OdfCompatibilityProfiles.MicrosoftOfficeExtended,
            TargetVersion = OdfVersion.Version13,
            CompressionMethod = OdfCompressionMethod.Stored,
            Cancellation = new OdfCancellationState
            {
                Reason = "save snapshot"
            }
        };

        OdfDocumentModel.ApplyContext(
            document,
            loadOptions,
            saveOptions,
            OdfMediaTypes.Text);
        loadOptions.SecurityLimits.MaximumXmlDepth = 99;
        loadOptions.StreamSelection.PartPaths[0] = OdfPartNames.Styles;
        loadOptions.StreamSelection.ElementNames[0].LocalName = "changed";
        saveOptions.TargetVersion = OdfVersion.Version10;
        saveOptions.Cancellation.Reason = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(document.LoadOptions, Is.Not.SameAs(loadOptions));
            Assert.That(document.LoadOptions.SecurityLimits, Is.Not.SameAs(loadOptions.SecurityLimits));
            Assert.That(document.LoadOptions.SecurityLimits.MaximumXmlDepth, Is.EqualTo(19));
            Assert.That(document.LoadOptions.AllowRecovery, Is.True);
            Assert.That(document.LoadOptions.PreserveUnknownContent, Is.False);
            Assert.That(document.LoadOptions.LeaveInputOpen, Is.False);
            Assert.That(document.LoadOptions.Cancellation.Reason, Is.EqualTo("load snapshot"));
            Assert.That(document.LoadOptions.StreamSelection.PartPaths, Is.EqualTo(new[] { OdfPartNames.Content }));
            Assert.That(document.LoadOptions.StreamSelection.ElementNames[0].LocalName, Is.EqualTo("p"));
            Assert.That(document.LoadOptions.StreamSelection.IncludeCommentEvents, Is.False);
            Assert.That(document.SaveOptions, Is.Not.SameAs(saveOptions));
            Assert.That(document.SaveOptions.Deterministic, Is.True);
            Assert.That(document.SaveOptions.IndentXml, Is.True);
            Assert.That(
                document.SaveOptions.CompatibilityProfile,
                Is.EqualTo(OdfCompatibilityProfiles.MicrosoftOfficeExtended));
            Assert.That(document.SaveOptions.TargetVersion, Is.EqualTo(OdfVersion.Version13));
            Assert.That(document.SaveOptions.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(document.SaveOptions.Cancellation.Reason, Is.EqualTo("save snapshot"));
        });
    }

    [Test]
    public void FutureVersionPassthroughSurvivesDefaultSavePreparation()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(
            ReadFlat(
                "2.1",
                OdfMediaTypes.Text,
                "<ext:future-section ext:value='retained'/>"
                    + "<office:body><office:text/></office:body>"));
        OdfPart part = document.Parts[0];
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            PreserveUnknownContent = false,
            TargetVersion = OdfVersion.Unknown
        };

        OdfNode[] prepared = OdfDocumentModel.PreparePartNodes(part, saveOptions);
        OdfPartRootMap preparedMap = OdfPartRootRules.MapRoot(prepared, "");

        Assert.Multiple(() =>
        {
            Assert.That(document.IsFutureVersion, Is.True);
            Assert.That(document.VersionInfo?.Version, Is.EqualTo(OdfVersion.Future));
            Assert.That(document.VersionInfo?.LexicalValue, Is.EqualTo("2.1"));
            Assert.That(part.IsFutureVersion, Is.True);
            Assert.That(part.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(document.PassthroughNodes, Is.EqualTo(part.PassthroughNodes));
            Assert.That(preparedMap.UnmappedSections, Has.Length.EqualTo(1));
            Assert.That(Version(preparedMap.Root), Is.EqualTo("2.1"));
            Assert.That(preparedMap.Root, Is.Not.SameAs(part.Root));
            Assert.That(part.PassthroughNodes[0].Name?.LocalName, Is.EqualTo("future-section"));
        });
    }

    [Test]
    public void ExplicitKnownTargetCanRemovePassthroughAndUpdateVersionOnClone()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(
            ReadFlat(
                "2.1",
                OdfMediaTypes.Text,
                "<ext:future-section/><office:body><office:text/></office:body>"));
        OdfPart part = document.Parts[0];
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            PreserveUnknownContent = false,
            TargetVersion = OdfVersion.Version14
        };

        OdfNode[] prepared = OdfDocumentModel.PreparePartNodes(part, saveOptions);
        OdfPartRootMap preparedMap = OdfPartRootRules.MapRoot(prepared, "");

        Assert.Multiple(() =>
        {
            Assert.That(preparedMap.UnmappedSections, Is.Empty);
            Assert.That(Version(preparedMap.Root), Is.EqualTo("1.4"));
            Assert.That(part.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(Version(part.Root!), Is.EqualTo("2.1"));
            Assert.That(part.Root?.Children.Any(
                node => node.Name?.LocalName == "future-section"), Is.True);
        });
    }

    [Test]
    public void OdfPartAndDocumentCollectionsHaveIndependentDefaults()
    {
        OdfPart firstPart = new OdfPart();
        OdfPart secondPart = new OdfPart();
        OdfDocument firstDocument = new OdfDocument();
        OdfDocument secondDocument = new OdfDocument();

        firstPart.Nodes = [new OdfNode()];
        firstPart.PassthroughNodes = [new OdfNode()];
        firstDocument.Parts = [firstPart];
        firstDocument.PassthroughNodes = [new OdfNode()];

        Assert.Multiple(() =>
        {
            Assert.That(secondPart.Nodes, Is.Empty);
            Assert.That(secondPart.Sections, Is.Empty);
            Assert.That(secondPart.PassthroughNodes, Is.Empty);
            Assert.That(secondDocument.Parts, Is.Empty);
            Assert.That(secondDocument.PassthroughNodes, Is.Empty);
            Assert.That(secondDocument.MediaType, Is.Empty);
            Assert.That(secondDocument.Body, Is.Null);
            Assert.That(secondDocument.IsFutureVersion, Is.False);
        });
    }

    private static OdfNode[] ReadFlat(
        string version,
        string mediaType,
        string children)
    {
        string mimetype = "";
        if (mediaType.Length > 0)
        {
            mimetype = " office:mimetype='" + mediaType + "'";
        }
        string xml = "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:ext='urn:example:future' office:version='"
            + version
            + "'"
            + mimetype
            + ">"
            + children
            + "</office:document>";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            "report.fodt",
            new OdfLoadOptions());
    }

    private static OdfNode[] ReadPart(
        string path,
        string rootName,
        string version,
        string children)
    {
        string xml = "<office:"
            + rootName
            + " xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:meta='"
            + OdfNamespaces.Metadata
            + "' xmlns:ext='urn:example:extension' office:version='"
            + version
            + "'>"
            + children
            + "</office:"
            + rootName
            + ">";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            path,
            new OdfLoadOptions());
    }

    private static string Version(OdfNode root)
    {
        return root.Attributes.Single(
            attribute => attribute.Name.NamespaceName == OdfNamespaces.Office
                && attribute.Name.LocalName == "version").Value;
    }
}
