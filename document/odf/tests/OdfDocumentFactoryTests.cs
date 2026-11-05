namespace odf.Tests;

public class OdfDocumentFactoryTests
{
    static object[][] DocumentKinds =
    [
        [OdfDocumentKind.Text, OdfMediaTypes.Text, "text"],
        [OdfDocumentKind.TextTemplate, OdfMediaTypes.TextTemplate, "text"],
        [OdfDocumentKind.TextMaster, OdfMediaTypes.TextMaster, "text"],
        [OdfDocumentKind.TextMasterTemplate, OdfMediaTypes.TextMasterTemplate, "text"],
        [OdfDocumentKind.Spreadsheet, OdfMediaTypes.Spreadsheet, "spreadsheet"],
        [OdfDocumentKind.SpreadsheetTemplate, OdfMediaTypes.SpreadsheetTemplate, "spreadsheet"],
        [OdfDocumentKind.Presentation, OdfMediaTypes.Presentation, "presentation"],
        [OdfDocumentKind.PresentationTemplate, OdfMediaTypes.PresentationTemplate, "presentation"],
        [OdfDocumentKind.Drawing, OdfMediaTypes.Graphics, "drawing"],
        [OdfDocumentKind.DrawingTemplate, OdfMediaTypes.GraphicsTemplate, "drawing"],
        [OdfDocumentKind.Chart, OdfMediaTypes.Chart, "chart"],
        [OdfDocumentKind.ChartTemplate, OdfMediaTypes.ChartTemplate, "chart"],
        [OdfDocumentKind.Formula, OdfMediaTypes.Formula, "text"],
        [OdfDocumentKind.FormulaTemplate, OdfMediaTypes.FormulaTemplate, "text"],
        [OdfDocumentKind.Image, OdfMediaTypes.Image, "image"],
        [OdfDocumentKind.ImageTemplate, OdfMediaTypes.ImageTemplate, "image"],
        [OdfDocumentKind.Database, OdfMediaTypes.Base, "database"]
    ];

    static object[][] EditableDocumentKinds =
    [
        [OdfDocumentKind.Text, OdfMediaTypes.Text],
        [OdfDocumentKind.TextTemplate, OdfMediaTypes.TextTemplate],
        [OdfDocumentKind.TextMaster, OdfMediaTypes.TextMaster],
        [OdfDocumentKind.TextMasterTemplate, OdfMediaTypes.TextMasterTemplate],
        [OdfDocumentKind.Spreadsheet, OdfMediaTypes.Spreadsheet],
        [OdfDocumentKind.SpreadsheetTemplate, OdfMediaTypes.SpreadsheetTemplate],
        [OdfDocumentKind.Presentation, OdfMediaTypes.Presentation],
        [OdfDocumentKind.PresentationTemplate, OdfMediaTypes.PresentationTemplate],
        [OdfDocumentKind.Drawing, OdfMediaTypes.Graphics],
        [OdfDocumentKind.DrawingTemplate, OdfMediaTypes.GraphicsTemplate],
        [OdfDocumentKind.Chart, OdfMediaTypes.Chart],
        [OdfDocumentKind.ChartTemplate, OdfMediaTypes.ChartTemplate],
        [OdfDocumentKind.Formula, OdfMediaTypes.Formula],
        [OdfDocumentKind.FormulaTemplate, OdfMediaTypes.FormulaTemplate],
        [OdfDocumentKind.Image, OdfMediaTypes.Image],
        [OdfDocumentKind.ImageTemplate, OdfMediaTypes.ImageTemplate]
    ];

    [TestCaseSource(nameof(DocumentKinds))]
    public void CreatesMinimalPackageSkeletonForEveryKind(
        OdfDocumentKind kind,
        string mediaType,
        string bodyName)
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        IOdfDocumentReader reader = new OdfDocumentReader();

        OdfDocument document = factory.CreatePackageDocument(kind);
        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = reader.ReadDocumentBytes(
            written,
            "roundtrip.odf",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(document.Kind, Is.EqualTo(kind));
            Assert.That(document.MediaType, Is.EqualTo(mediaType));
            Assert.That(document.Parts, Has.Length.EqualTo(1));
            Assert.That(document.PackageEntries, Has.Length.EqualTo(3));
            Assert.That(document.Manifest, Is.Not.Null);
            Assert.That(document.Manifest?.Entries, Has.Length.EqualTo(2));
            Assert.That(document.PackageEntries[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(document.PackageEntries[0].IsFirstEntry, Is.True);
            Assert.That(
                document.PackageEntries[0].CompressionMethod,
                Is.EqualTo(OdfCompressionMethod.Stored));
            Assert.That(document.PackageEntries[0].LocalExtraFieldLength, Is.Zero);
            Assert.That(RootManifestMediaType(document), Is.EqualTo(mediaType));
            Assert.That(reread.Kind, Is.EqualTo(kind));
            Assert.That(reread.MediaType, Is.EqualTo(mediaType));
        });

        if (kind == OdfDocumentKind.Formula
            || kind == OdfDocumentKind.FormulaTemplate)
        {
            Assert.Multiple(() =>
            {
                Assert.That(document.Parts[0].RootName, Is.EqualTo("math"));
                Assert.That(document.Parts[0].Root?.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Math));
                Assert.That(document.Body, Is.Null);
            });
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(document.Parts[0].RootName, Is.EqualTo("document-content"));
                Assert.That(document.VersionInfo?.Version, Is.EqualTo(OdfVersion.Version14));
                Assert.That(BodyChildName(document), Is.EqualTo(bodyName));
            });
        }
    }

    [TestCaseSource(nameof(DocumentKinds))]
    public void CreatesMinimalFlatSkeletonForEveryKind(
        OdfDocumentKind kind,
        string mediaType,
        string bodyName)
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        IOdfDocumentReader reader = new OdfDocumentReader();

        OdfDocument document = factory.CreateFlatDocument(kind);
        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = reader.ReadDocumentBytes(
            written,
            "roundtrip.xml",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(document.Kind, Is.EqualTo(kind));
            Assert.That(document.MediaType, Is.EqualTo(mediaType));
            Assert.That(document.Parts, Has.Length.EqualTo(1));
            Assert.That(document.Parts[0].RootName, Is.EqualTo("document"));
            Assert.That(document.Parts[0].IsFlatDocument, Is.True);
            Assert.That(document.VersionInfo?.Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(BodyChildName(document), Is.EqualTo(bodyName));
            Assert.That(reread.Kind, Is.EqualTo(kind));
            Assert.That(reread.MediaType, Is.EqualTo(mediaType));
            Assert.That(BodyChildName(reread), Is.EqualTo(bodyName));
        });
    }

    [TestCaseSource(nameof(EditableDocumentKinds))]
    public void CreatesModifiesAndRoundTripsEveryDocumentAndTemplateKind(
        OdfDocumentKind kind,
        string mediaType)
    {
        const string marker = "EndToEndMarker";
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        IOdfDocumentReader reader = new OdfDocumentReader();
        OdfDocument document = factory.CreatePackageDocument(kind);

        ModifyDocument(document, kind, marker);
        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = reader.ReadDocumentBytes(
            written,
            "modified.odf",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reread.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(reread.Kind, Is.EqualTo(kind));
            Assert.That(reread.MediaType, Is.EqualTo(mediaType));
            Assert.That(RootManifestMediaType(reread), Is.EqualTo(mediaType));
            AssertModification(reread, kind, marker);
        });
    }

    [Test]
    public void ChartAndImageSkeletonsContainTheirRequiredMainContent()
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        OdfDocument chart = factory.CreateFlatDocument(OdfDocumentKind.Chart);
        OdfDocument image = factory.CreatePackageDocument(OdfDocumentKind.Image);

        OdfNode chartBody = RequireBodyChild(chart);
        OdfNode chartElement = RequireElementChild(chartBody);
        OdfNode plotArea = RequireElementChild(chartElement);
        OdfNode imageBody = RequireBodyChild(image);
        OdfNode imageFrame = RequireElementChild(imageBody);

        Assert.Multiple(() =>
        {
            Assert.That(chartElement.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Chart));
            Assert.That(chartElement.Name?.LocalName, Is.EqualTo("chart"));
            Assert.That(FindAttribute(chartElement, OdfNamespaces.Chart, "class"), Is.EqualTo("chart:bar"));
            Assert.That(plotArea.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Chart));
            Assert.That(plotArea.Name?.LocalName, Is.EqualTo("plot-area"));
            Assert.That(imageFrame.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Drawing));
            Assert.That(imageFrame.Name?.LocalName, Is.EqualTo("frame"));
        });
    }

    [Test]
    public void RejectsUnknownKindsAndCreatesIndependentDocuments()
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        OdfDocument first = factory.CreateFlatDocument(OdfDocumentKind.Text);
        OdfDocument second = factory.CreateFlatDocument(OdfDocumentKind.Text);

        RequireBodyChild(first).Children =
        [
            new OdfNode
            {
                Kind = OdfNodeKind.Text,
                Value = "first only"
            }
        ];

        Assert.Multiple(() =>
        {
            Assert.That(first.Nodes[0], Is.Not.SameAs(second.Nodes[0]));
            Assert.That(RequireBodyChild(second).Children, Is.Empty);
            Assert.Throws<OdfDocumentException>(
                () => factory.CreatePackageDocument(OdfDocumentKind.Unknown));
            Assert.Throws<OdfDocumentException>(
                () => factory.CreateFlatDocument(OdfDocumentKind.Unknown));
        });
    }

    static void ModifyDocument(
        OdfDocument document,
        OdfDocumentKind kind,
        string marker)
    {
        if (kind == OdfDocumentKind.Text
            || kind == OdfDocumentKind.TextTemplate
            || kind == OdfDocumentKind.TextMaster
            || kind == OdfDocumentKind.TextMasterTemplate)
        {
            new OdfTextEditor().AppendParagraph(document, marker, "");
            return;
        }
        if (kind == OdfDocumentKind.Spreadsheet
            || kind == OdfDocumentKind.SpreadsheetTemplate)
        {
            new OdfSpreadsheetEditor().CreateSheet(document, marker, 2, 3);
            return;
        }
        if (kind == OdfDocumentKind.Presentation
            || kind == OdfDocumentKind.PresentationTemplate)
        {
            new OdfPresentationEditor().CreateSlide(document, marker);
            return;
        }
        if (kind == OdfDocumentKind.Drawing
            || kind == OdfDocumentKind.DrawingTemplate)
        {
            new OdfDrawingEditor().CreateDrawingObject(
                document,
                null,
                OdfDrawingKind.Page,
                marker);
            return;
        }
        if (kind == OdfDocumentKind.Chart
            || kind == OdfDocumentKind.ChartTemplate)
        {
            OdfChartEditor editor = new OdfChartEditor();
            OdfChart chart = editor.CreateChart(document, OdfChartKind.Bar);
            chart.Width = "12cm";
            editor.ApplyChartChanges(document);
            return;
        }
        if (kind == OdfDocumentKind.Formula
            || kind == OdfDocumentKind.FormulaTemplate)
        {
            OdfNode identifier = AppendElement(
                document.Parts[0].Root!,
                OdfNamespaces.Math,
                "mi",
                "math");
            AppendText(identifier, marker);
            return;
        }

        new OdfDrawingEditor().CreateDrawingObject(
            document,
            null,
            OdfDrawingKind.Frame,
            marker);
    }

    static void AssertModification(
        OdfDocument document,
        OdfDocumentKind kind,
        string marker)
    {
        if (kind == OdfDocumentKind.Text
            || kind == OdfDocumentKind.TextTemplate
            || kind == OdfDocumentKind.TextMaster
            || kind == OdfDocumentKind.TextMasterTemplate)
        {
            Assert.That(
                new OdfTextEditor().ExtractPlainText(document),
                Is.EqualTo(marker));
            return;
        }
        if (kind == OdfDocumentKind.Spreadsheet
            || kind == OdfDocumentKind.SpreadsheetTemplate)
        {
            OdfTable? sheet = new OdfSpreadsheetEditor().FindSheet(
                document,
                marker);
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet?.RowCount, Is.EqualTo(2));
            Assert.That(sheet?.ColumnCount, Is.EqualTo(3));
            return;
        }
        if (kind == OdfDocumentKind.Presentation
            || kind == OdfDocumentKind.PresentationTemplate)
        {
            OdfSlide[] slides = new OdfPresentationEditor().GetSlides(document);
            Assert.That(slides, Has.Length.EqualTo(1));
            Assert.That(slides[0].Name, Is.EqualTo(marker));
            return;
        }
        if (kind == OdfDocumentKind.Drawing
            || kind == OdfDocumentKind.DrawingTemplate)
        {
            Assert.That(
                new OdfDrawingEditor().FindDrawingObject(document, marker),
                Is.Not.Null);
            return;
        }
        if (kind == OdfDocumentKind.Chart
            || kind == OdfDocumentKind.ChartTemplate)
        {
            OdfChart[] charts = new OdfChartEditor().GetCharts(document);
            Assert.That(charts, Has.Length.EqualTo(1));
            Assert.That(charts[0].Width, Is.EqualTo("12cm"));
            return;
        }
        if (kind == OdfDocumentKind.Formula
            || kind == OdfDocumentKind.FormulaTemplate)
        {
            Assert.That(ContainsText(document.Parts[0].Root!, marker), Is.True);
            return;
        }

        Assert.That(
            new OdfDrawingEditor().FindDrawingObject(document, marker),
            Is.Not.Null);
    }

    static OdfNode AppendElement(
        OdfNode parent,
        string namespaceName,
        string localName,
        string prefix)
    {
        OdfNode child = new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            },
            Parent = parent,
            WasEmptyElement = true
        };
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        parent.Children = children;
        parent.WasEmptyElement = false;
        return child;
    }

    static void AppendText(OdfNode parent, string value)
    {
        OdfNode text = new OdfNode
        {
            Kind = OdfNodeKind.Text,
            Parent = parent,
            Value = value
        };
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = text;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static bool ContainsText(OdfNode node, string value)
    {
        if (node.Kind == OdfNodeKind.Text && node.Value == value)
        {
            return true;
        }
        foreach (OdfNode child in node.Children)
        {
            if (ContainsText(child, value))
            {
                return true;
            }
        }
        return false;
    }

    static string RootManifestMediaType(OdfDocument document)
    {
        if (document.Manifest == null)
        {
            return "";
        }
        foreach (OdfManifestEntry entry in document.Manifest.Entries)
        {
            if (entry.FullPath == OdfPartNames.Root)
            {
                return entry.MediaType;
            }
        }
        return "";
    }

    static string BodyChildName(OdfDocument document)
    {
        return RequireBodyChild(document).Name?.LocalName ?? "";
    }

    static OdfNode RequireBodyChild(OdfDocument document)
    {
        Assert.That(document.Body, Is.Not.Null);
        return RequireElementChild(document.Body!);
    }

    static OdfNode RequireElementChild(OdfNode parent)
    {
        foreach (OdfNode child in parent.Children)
        {
            if (child.Kind == OdfNodeKind.Element)
            {
                return child;
            }
        }
        Assert.Fail("An element child was required.");
        return new OdfNode();
    }

    static string FindAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
        }
        return "";
    }
}
