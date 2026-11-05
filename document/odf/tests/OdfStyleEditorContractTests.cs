using System.Text;

namespace odf.Tests;

public class OdfStyleEditorContractTests
{
    static object[][] Families =
    [
        [OdfStyleFamily.Paragraph],
        [OdfStyleFamily.Text],
        [OdfStyleFamily.Section],
        [OdfStyleFamily.Table],
        [OdfStyleFamily.TableColumn],
        [OdfStyleFamily.TableRow],
        [OdfStyleFamily.TableCell],
        [OdfStyleFamily.Graphic],
        [OdfStyleFamily.Chart],
        [OdfStyleFamily.DrawingPage],
        [OdfStyleFamily.Presentation],
        [OdfStyleFamily.Ruby],
        [OdfStyleFamily.List]
    ];

    static object[][] RenameCases =
    [
        [OdfStyleFamily.Paragraph, OdfNamespaces.Text, "p", OdfNamespaces.Text],
        [OdfStyleFamily.Text, OdfNamespaces.Text, "span", OdfNamespaces.Text],
        [OdfStyleFamily.Section, OdfNamespaces.Text, "section", OdfNamespaces.Text],
        [OdfStyleFamily.Table, OdfNamespaces.Table, "table", OdfNamespaces.Table],
        [OdfStyleFamily.TableColumn, OdfNamespaces.Table, "table-column", OdfNamespaces.Table],
        [OdfStyleFamily.TableRow, OdfNamespaces.Table, "table-row", OdfNamespaces.Table],
        [OdfStyleFamily.TableCell, OdfNamespaces.Table, "table-cell", OdfNamespaces.Table],
        [OdfStyleFamily.Graphic, OdfNamespaces.Drawing, "frame", OdfNamespaces.Drawing],
        [OdfStyleFamily.Chart, OdfNamespaces.Chart, "chart", OdfNamespaces.Chart],
        [OdfStyleFamily.DrawingPage, OdfNamespaces.Drawing, "page", OdfNamespaces.Drawing],
        [OdfStyleFamily.Presentation, OdfNamespaces.Presentation, "placeholder", OdfNamespaces.Presentation],
        [OdfStyleFamily.Ruby, OdfNamespaces.Text, "ruby", OdfNamespaces.Text],
        [OdfStyleFamily.List, "", "", ""]
    ];

    [TestCaseSource(nameof(Families))]
    public void CreatesFindsModifiesAndRoundTripsEveryStyleFamily(
        OdfStyleFamily family)
    {
        OdfDocument document = EmptyStyleDocument();
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle definition = new OdfStyle
        {
            Name = "Common",
            Family = family,
            DisplayName = "Initial"
        };

        OdfStyle common = editor.CreateStyle(document, definition);
        OdfStyle automatic = editor.CreateAutomaticStyle(
            document,
            common,
            "Automatic");
        OdfStyle modified = editor.ModifyStyle(
            document,
            common,
            new OdfStyle
            {
                Name = "Common",
                Family = family,
                DisplayName = "Modified"
            });
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(common, Is.Not.SameAs(definition));
            Assert.That(modified, Is.SameAs(common));
            Assert.That(common.DisplayName, Is.EqualTo("Modified"));
            Assert.That(common.IsNamed, Is.True);
            Assert.That(common.IsCommon, Is.True);
            Assert.That(common.IsAutomatic, Is.False);
            Assert.That(automatic.Name, Is.EqualTo("Automatic"));
            Assert.That(automatic.Family, Is.EqualTo(family));
            Assert.That(automatic.IsNamed, Is.True);
            Assert.That(automatic.IsCommon, Is.False);
            Assert.That(automatic.IsAutomatic, Is.True);
            Assert.That(
                editor.FindStyle(document, "Common", family),
                Is.SameAs(common));
            Assert.That(
                editor.FindStyle(document, "Automatic", family),
                Is.SameAs(automatic));
            Assert.That(editor.FindStyles(document, family), Has.Length.EqualTo(2));
            Assert.That(
                editor.FindStyle(document, "Missing", family),
                Is.Null);
            Assert.That(
                reread.Styles.Count(item => item.Family == family),
                Is.EqualTo(2));
            Assert.That(
                reread.Styles.Single(item => item.Name == "Common").DisplayName,
                Is.EqualTo("Modified"));
            Assert.That(
                reread.Styles.Single(item => item.Name == "Automatic").IsAutomatic,
                Is.True);
        });
    }

    [Test]
    public void ResolvesDefaultsInheritanceCyclesAndRelatedStyleModels()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
                + "<style:default-style style:family='paragraph'>"
                + "<style:paragraph-properties fo:margin-left='1cm'/>"
                + "</style:default-style>"
                + "<text:list-style style:name='ListOne'>"
                + "<text:list-level-style-bullet text:level='1' text:bullet-char='•'/>"
                + "</text:list-style>"
                + "<number:number-style style:name='NumberOne'>"
                + "<number:number number:decimal-places='2'/>"
                + "</number:number-style>"
                + "<style:style style:name='Parent' style:family='paragraph' "
                + "style:next-style-name='Parent'>"
                + "<style:paragraph-properties fo:margin-right='2cm'/>"
                + "</style:style>"
                + "<style:style style:name='Child' style:family='paragraph' "
                + "style:parent-style-name='Parent' style:list-style-name='ListOne' "
                + "style:master-page-name='MasterOne' style:data-style-name='NumberOne'>"
                + "<style:paragraph-properties fo:margin-left='3cm'/>"
                + "</style:style>"
                + "<style:style style:name='CycleA' style:family='paragraph' "
                + "style:parent-style-name='CycleB'/>"
                + "<style:style style:name='CycleB' style:family='paragraph' "
                + "style:parent-style-name='CycleA'/>"
                + "</office:styles>",
            "<office:automatic-styles>"
                + "<style:page-layout style:name='PageOne'>"
                + "<style:page-layout-properties fo:page-width='21cm'/>"
                + "</style:page-layout>"
                + "</office:automatic-styles>",
            "<office:master-styles>"
                + "<style:master-page style:name='MasterOne' "
                + "style:page-layout-name='PageOne'/>"
                + "</office:master-styles>",
            "");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle defaultStyle = editor.FindStyle(
            document,
            "",
            OdfStyleFamily.Paragraph)!;
        OdfStyle parent = editor.FindStyle(
            document,
            "Parent",
            OdfStyleFamily.Paragraph)!;
        OdfStyle child = editor.FindStyle(
            document,
            "Child",
            OdfStyleFamily.Paragraph)!;
        OdfStyle cycle = editor.FindStyle(
            document,
            "CycleA",
            OdfStyleFamily.Paragraph)!;

        OdfStyleResolution resolution = editor.ResolveEffectiveProperties(
            document,
            child);
        OdfStyleResolution cycleResolution = editor.ResolveEffectiveProperties(
            document,
            cycle);
        OdfNode properties = resolution.EffectiveProperties.Single(
            item => item.Name?.LocalName == "paragraph-properties");

        Assert.Multiple(() =>
        {
            Assert.That(defaultStyle.IsDefault, Is.True);
            Assert.That(defaultStyle.IsNamed, Is.False);
            Assert.That(child.DefaultStyle, Is.SameAs(defaultStyle));
            Assert.That(child.ParentStyle, Is.SameAs(parent));
            Assert.That(parent.HasNextCycle, Is.True);
            Assert.That(parent.HasParentCycle, Is.False);
            Assert.That(resolution.IsValid, Is.True);
            Assert.That(
                resolution.Sources,
                Is.EqualTo(new[] { defaultStyle, parent, child }));
            Assert.That(
                PropertyValue(properties, OdfNamespaces.Fo, "margin-left"),
                Is.EqualTo("3cm"));
            Assert.That(
                PropertyValue(properties, OdfNamespaces.Fo, "margin-right"),
                Is.EqualTo("2cm"));
            Assert.That(child.ListStyleNode, Is.Not.Null);
            Assert.That(child.ListStyle?.Name, Is.EqualTo("ListOne"));
            Assert.That(child.DataStyleNode, Is.Not.Null);
            Assert.That(child.DataStyle?.Name, Is.EqualTo("NumberOne"));
            Assert.That(child.DataStyle?.Kind, Is.EqualTo(OdfDataStyleKind.Number));
            Assert.That(child.MasterPageNode, Is.Not.Null);
            Assert.That(document.PageLayouts.Single().Name, Is.EqualTo("PageOne"));
            Assert.That(document.MasterPages.Single().Name, Is.EqualTo("MasterOne"));
            Assert.That(
                document.MasterPages.Single().PageLayout,
                Is.SameAs(document.PageLayouts.Single()));
            Assert.That(cycle.HasParentCycle, Is.True);
            Assert.That(cycleResolution.IsValid, Is.False);
            Assert.That(cycleResolution.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                cycleResolution.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidStyleReference));
        });
    }

    [Test]
    public void CreatesAndReusesEquivalentAutomaticStyles()
    {
        OdfDocument document = EmptyStyleDocument();
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle source = ParagraphStyle("Source", "center");

        OdfStyle first = editor.CreateAutomaticStyle(
            document,
            source,
            "P1");
        OdfStyle reused = editor.CreateAutomaticStyle(
            document,
            source,
            "P2");
        source.ParagraphProperties[0].LexicalValue = "right";
        OdfStyle second = editor.CreateAutomaticStyle(
            document,
            source,
            "P1");

        Assert.Multiple(() =>
        {
            Assert.That(reused, Is.SameAs(first));
            Assert.That(first.Name, Is.EqualTo("P1"));
            Assert.That(first.ParagraphProperties[0].LexicalValue, Is.EqualTo("center"));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Name, Is.EqualTo("P12"));
            Assert.That(second.ParagraphProperties[0].LexicalValue, Is.EqualTo("right"));
            Assert.That(document.Styles, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CopiesDependencyClosureAcrossCyclesAndNameCollisions()
    {
        OdfDocument sourceDocument = ReadFlat(
            "<office:styles>"
                + "<style:style style:name='A' style:family='paragraph' "
                + "style:parent-style-name='B' style:next-style-name='C'/>"
                + "<style:style style:name='B' style:family='paragraph' "
                + "style:parent-style-name='A'>"
                + "<style:text-properties fo:font-weight='bold'/>"
                + "</style:style>"
                + "<style:style style:name='C' style:family='paragraph'/>"
                + "</office:styles>",
            "",
            "",
            "");
        OdfDocument targetDocument = ReadFlat(
            "<office:styles>"
                + "<style:style style:name='B' style:family='paragraph'/>"
                + "</office:styles>",
            "",
            "",
            "");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle source = editor.FindStyle(
            sourceDocument,
            "A",
            OdfStyleFamily.Paragraph)!;

        OdfStyle copied = editor.CopyStyle(
            sourceDocument,
            targetDocument,
            source,
            "Imported");
        OdfStyle copiedParent = editor.FindStyle(
            targetDocument,
            "B2",
            OdfStyleFamily.Paragraph)!;
        OdfStyle copiedNext = editor.FindStyle(
            targetDocument,
            "C",
            OdfStyleFamily.Paragraph)!;
        copiedParent.TextProperties[0].LexicalValue = "normal";

        Assert.Multiple(() =>
        {
            Assert.That(copied.Name, Is.EqualTo("Imported"));
            Assert.That(copied.ParentStyleName, Is.EqualTo("B2"));
            Assert.That(copied.NextStyleName, Is.EqualTo("C"));
            Assert.That(copied.ParentStyle, Is.SameAs(copiedParent));
            Assert.That(copied.NextStyle, Is.SameAs(copiedNext));
            Assert.That(copiedParent.ParentStyle, Is.SameAs(copied));
            Assert.That(copied.HasParentCycle, Is.True);
            Assert.That(targetDocument.Styles, Has.Length.EqualTo(4));
            Assert.That(copied, Is.Not.SameAs(source));
            Assert.That(
                sourceDocument.Styles.Single(item => item.Name == "B")
                    .TextProperties[0].LexicalValue,
                Is.EqualTo("bold"));
        });
    }

    [TestCaseSource(nameof(RenameCases))]
    public void RenamesEveryStyleFamilyAndMatchingReferences(
        OdfStyleFamily family,
        string elementNamespace,
        string elementName,
        string attributeNamespace)
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle original = editor.CreateStyle(
            document,
            new OdfStyle
            {
                Name = "Original",
                Family = family
            });
        OdfStyle dependent = editor.CreateStyle(
            document,
            new OdfStyle
            {
                Name = "Dependent",
                Family = family,
                ParentStyleName = "Original"
            });
        OdfAttribute? reference = null;
        if (elementName.Length > 0)
        {
            OdfNode referenceNode = Element(elementNamespace, elementName);
            reference = Attribute(
                attributeNamespace,
                "style-name",
                "Original");
            referenceNode.Attributes = [reference];
            AppendChild(document.Parts[0].Root!, referenceNode);
        }

        editor.RenameStyle(document, original, "Renamed");

        Assert.Multiple(() =>
        {
            Assert.That(original.Name, Is.EqualTo("Renamed"));
            Assert.That(dependent.ParentStyleName, Is.EqualTo("Renamed"));
            Assert.That(dependent.ParentStyle, Is.SameAs(original));
            Assert.That(
                editor.FindStyle(document, "Original", family),
                Is.Null);
            Assert.That(
                editor.FindStyle(document, "Renamed", family),
                Is.SameAs(original));
            if (reference != null)
            {
                Assert.That(reference.Value, Is.EqualTo("Renamed"));
            }
        });
    }

    [Test]
    public void CleanupMethodsDeduplicateReferencesAndRemoveOnlyUnusedStyles()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle definition = ParagraphStyle("Definition", "center");
        OdfStyle canonical = editor.CreateAutomaticStyle(
            document,
            definition,
            "A1");
        OdfStyle duplicateDefinition = ParagraphStyle("A2", "center");
        duplicateDefinition.IsAutomatic = true;
        OdfStyle duplicate = editor.CreateStyle(
            document,
            duplicateDefinition);
        OdfStyle unused = editor.CreateStyle(
            document,
            new OdfStyle
            {
                Name = "Unused",
                Family = OdfStyleFamily.Paragraph
            });
        OdfNode paragraph = Element(OdfNamespaces.Text, "p");
        OdfAttribute reference = Attribute(
            OdfNamespaces.Text,
            "style-name",
            "A2");
        paragraph.Attributes = [reference];
        AppendChild(document.Parts[0].Root!, paragraph);

        OdfStyle[] deduplicated = editor.DeduplicateAutomaticStyles(document);
        OdfStyle[] foundUnused = editor.FindUnusedStyles(document);
        OdfStyle[] removedUnused = editor.RemoveUnusedStyles(document);

        Assert.Multiple(() =>
        {
            Assert.That(deduplicated, Is.EqualTo(new[] { duplicate }));
            Assert.That(reference.Value, Is.EqualTo("A1"));
            Assert.That(editor.FindStyle(
                document,
                "A2",
                OdfStyleFamily.Paragraph), Is.Null);
            Assert.That(foundUnused, Is.EqualTo(new[] { unused }));
            Assert.That(removedUnused, Is.EqualTo(foundUnused));
            Assert.That(document.Styles, Is.EqualTo(new[] { canonical }));
            Assert.That(editor.FindUnusedStyles(document), Is.Empty);
        });
    }

    static OdfStyle ParagraphStyle(string name, string alignment)
    {
        return new OdfStyle
        {
            Name = name,
            Family = OdfStyleFamily.Paragraph,
            ParagraphProperties =
            [
                new OdfStyleProperty
                {
                    Name = new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Fo,
                        LocalName = "text-align",
                        Prefix = OdfNamespaces.FoPrefix
                    },
                    LexicalValue = alignment
                }
            ]
        };
    }

    static OdfDocument EmptyStyleDocument()
    {
        return ReadFlat("<office:styles/>", "", "", "");
    }

    static OdfDocument ReadFlat(
        string styles,
        string automaticStyles,
        string masterStyles,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:number='" + OdfNamespaces.Number + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + automaticStyles
            + masterStyles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "style-contract.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] data = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "style-contract.fodt",
            new OdfLoadOptions());
    }

    static string PropertyValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName).Value;
    }

    static OdfNode Element(string namespaceName, string localName)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName
            }
        };
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string value)
    {
        return new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName
            },
            Value = value
        };
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        child.Parent = parent;
        parent.Children = [.. parent.Children, child];
    }
}
