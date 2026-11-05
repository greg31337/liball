using System.IO;
using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class OdfMetadataEditorTests
{
    [Test]
    public void EditsSimpleUserAndStatisticalMetadataRoundTrip()
    {
        OdfDocument document = new OdfDocumentFactory().CreateFlatDocument(
            OdfDocumentKind.Text);
        IOdfMetadataEditor editor = new OdfMetadataEditor();
        OdfDocumentMetadata replacement = new OdfDocumentMetadata
        {
            Title = "Edited title",
            Description = "Edited description",
            Subject = "Edited subject",
            Creator = "Metadata editor",
            Date = "2026-09-02T12:00:00Z",
            Language = "en-US",
            Generator = "odf SDK",
            InitialCreator = "Original author",
            CreationDate = "2026-08-01",
            PrintedBy = "Print operator",
            PrintDate = "2026-09-02T13:00:00Z",
            EditingDuration = "PT20M",
            EditingCycles = 3,
            Keywords = new[] { "editor", "metadata" }
        };
        replacement.Template.IsPresent = true;
        replacement.Template.Href = "Templates/base.ott";
        replacement.Template.XLinkType = "simple";
        replacement.Template.Actuate = "onRequest";
        replacement.Template.Title = "Base template";
        replacement.Template.Date = "2026-09-01T10:00:00Z";
        replacement.AutoReload.IsPresent = true;
        replacement.AutoReload.Href = "https://example.test/current.odt";
        replacement.AutoReload.XLinkType = "simple";
        replacement.AutoReload.Show = "replace";
        replacement.AutoReload.Actuate = "onLoad";
        replacement.AutoReload.Delay = "PT5M";
        replacement.HyperlinkBehavior.IsPresent = true;
        replacement.HyperlinkBehavior.TargetFrameName = "_blank";
        replacement.HyperlinkBehavior.Show = "new";
        OdfDocumentMetadata edited = editor.SetSimpleMetadata(
            document, replacement);
        Assert.That(editor.GetDocumentMetadata(document), Is.SameAs(edited));

        OdfUserDefinedMetadata owner = editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Owner",
                Kind = OdfValueKind.String,
                StringValue = "Alice"
            });
        OdfUserDefinedMetadata replacedOwner = editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Owner",
                Kind = OdfValueKind.String,
                StringValue = "Bob"
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Score",
                Kind = OdfValueKind.Float,
                NumericValue = 12.5
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Completion",
                Kind = OdfValueKind.Percentage,
                NumericValue = 0.75
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Cost",
                Kind = OdfValueKind.Currency,
                NumericValue = 19.95,
                Currency = "USD"
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Due",
                Kind = OdfValueKind.Date,
                DateValue = "2026-09-30"
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Elapsed",
                Kind = OdfValueKind.Duration,
                DurationValue = "PT45M"
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Approved",
                Kind = OdfValueKind.Boolean,
                BooleanValue = true
            });
        editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Temporary",
                Kind = OdfValueKind.Boolean,
                BooleanValue = true
            });
        Assert.That(editor.RemoveUserMetadata(document, "Temporary"), Is.True);
        Assert.That(owner, Is.SameAs(replacedOwner));
        Assert.That(editor.FindUserMetadata(document, "Owner"),
            Is.SameAs(owner));

        OdfDocumentStatistics statistics = editor.SetDocumentStatistics(
            document,
            new OdfDocumentStatistics
            {
                PageCount = 4,
                TableCount = 5,
                DrawCount = 6,
                ImageCount = 7,
                ObjectCount = 8,
                OleObjectCount = 9,
                ParagraphCount = 12,
                WordCount = 250,
                CharacterCount = 1400,
                NonWhitespaceCharacterCount = 1200,
                RowCount = 14,
                CellCount = 15,
                FrameCount = 16,
                SentenceCount = 17,
                SyllableCount = 18
            });
        Assert.That(statistics.IsPresent, Is.True);
        Assert.That(edited.IsDateValid, Is.True);
        Assert.That(edited.IsEditingDurationValid, Is.True);

        OdfDocument roundTrip = WriteAndRead(document, "metadata.fodt");
        Assert.That(roundTrip.Metadata.Title, Is.EqualTo("Edited title"));
        Assert.That(roundTrip.Metadata.Description,
            Is.EqualTo("Edited description"));
        Assert.That(roundTrip.Metadata.Subject, Is.EqualTo("Edited subject"));
        Assert.That(roundTrip.Metadata.Creator, Is.EqualTo("Metadata editor"));
        Assert.That(roundTrip.Metadata.Date,
            Is.EqualTo("2026-09-02T12:00:00Z"));
        Assert.That(roundTrip.Metadata.Language, Is.EqualTo("en-US"));
        Assert.That(roundTrip.Metadata.Generator, Is.EqualTo("odf SDK"));
        Assert.That(roundTrip.Metadata.InitialCreator,
            Is.EqualTo("Original author"));
        Assert.That(roundTrip.Metadata.CreationDate,
            Is.EqualTo("2026-08-01"));
        Assert.That(roundTrip.Metadata.PrintedBy,
            Is.EqualTo("Print operator"));
        Assert.That(roundTrip.Metadata.PrintDate,
            Is.EqualTo("2026-09-02T13:00:00Z"));
        Assert.That(roundTrip.Metadata.EditingDuration, Is.EqualTo("PT20M"));
        Assert.That(roundTrip.Metadata.EditingCycles, Is.EqualTo(3));
        Assert.That(roundTrip.Metadata.Keywords,
            Is.EqualTo(new[] { "editor", "metadata" }));
        Assert.That(roundTrip.Metadata.Template.Href,
            Is.EqualTo("Templates/base.ott"));
        Assert.That(roundTrip.Metadata.Template.Actuate,
            Is.EqualTo("onRequest"));
        Assert.That(roundTrip.Metadata.Template.Title,
            Is.EqualTo("Base template"));
        Assert.That(roundTrip.Metadata.AutoReload.Delay, Is.EqualTo("PT5M"));
        Assert.That(roundTrip.Metadata.AutoReload.Show, Is.EqualTo("replace"));
        Assert.That(roundTrip.Metadata.AutoReload.Actuate,
            Is.EqualTo("onLoad"));
        Assert.That(roundTrip.Metadata.HyperlinkBehavior.TargetFrameName,
            Is.EqualTo("_blank"));
        OdfUserDefinedMetadata? loadedOwner = editor.FindUserMetadata(
            roundTrip, "Owner");
        Assert.That(loadedOwner, Is.Not.Null);
        Assert.That(loadedOwner?.StringValue, Is.EqualTo("Bob"));
        Assert.That(editor.FindUserMetadata(roundTrip, "Score")?.NumericValue,
            Is.EqualTo(12.5));
        Assert.That(editor.FindUserMetadata(roundTrip, "Completion")?.Kind,
            Is.EqualTo(OdfValueKind.Percentage));
        Assert.That(editor.FindUserMetadata(roundTrip, "Completion")
            ?.NumericValue, Is.EqualTo(0.75));
        Assert.That(editor.FindUserMetadata(roundTrip, "Cost")?.Currency,
            Is.EqualTo("USD"));
        Assert.That(editor.FindUserMetadata(roundTrip, "Due")?.DateValue,
            Is.EqualTo("2026-09-30"));
        Assert.That(editor.FindUserMetadata(roundTrip, "Elapsed")
            ?.DurationValue, Is.EqualTo("PT45M"));
        Assert.That(editor.FindUserMetadata(roundTrip, "Approved")
            ?.BooleanValue, Is.True);
        Assert.That(roundTrip.Metadata.Statistics.PageCount, Is.EqualTo(4));
        Assert.That(roundTrip.Metadata.Statistics.TableCount, Is.EqualTo(5));
        Assert.That(roundTrip.Metadata.Statistics.DrawCount, Is.EqualTo(6));
        Assert.That(roundTrip.Metadata.Statistics.ImageCount, Is.EqualTo(7));
        Assert.That(roundTrip.Metadata.Statistics.ObjectCount, Is.EqualTo(8));
        Assert.That(roundTrip.Metadata.Statistics.OleObjectCount, Is.EqualTo(9));
        Assert.That(roundTrip.Metadata.Statistics.ParagraphCount,
            Is.EqualTo(12));
        Assert.That(roundTrip.Metadata.Statistics.WordCount, Is.EqualTo(250));
        Assert.That(roundTrip.Metadata.Statistics.CharacterCount,
            Is.EqualTo(1400));
        Assert.That(roundTrip.Metadata.Statistics
            .NonWhitespaceCharacterCount, Is.EqualTo(1200));
        Assert.That(roundTrip.Metadata.Statistics.RowCount, Is.EqualTo(14));
        Assert.That(roundTrip.Metadata.Statistics.CellCount, Is.EqualTo(15));
        Assert.That(roundTrip.Metadata.Statistics.FrameCount, Is.EqualTo(16));
        Assert.That(roundTrip.Metadata.Statistics.SentenceCount,
            Is.EqualTo(17));
        Assert.That(roundTrip.Metadata.Statistics.SyllableCount,
            Is.EqualTo(18));

        editor.ClearDocumentStatistics(roundTrip);
        OdfDocument cleared = WriteAndRead(roundTrip, "cleared.fodt");
        Assert.That(cleared.Metadata.Statistics.IsPresent, Is.False);
    }

    [Test]
    public void AddsModifiesRemovesAndQueriesRdfaTriples()
    {
        OdfDocument document = ReadFlat(RdfaEditorXml());
        IOdfMetadataEditor editor = new OdfMetadataEditor();
        OdfRdfTriple current = editor.FindRdfTriples(
            document, "urn:example:item", "urn:example:name", null)[0];
        OdfRdfTriple obsolete = editor.FindRdfTriples(
            document, "urn:example:item", "urn:example:obsolete", null)[0];
        Assert.Multiple(() =>
        {
            Assert.That(current.SubjectLexical, Is.EqualTo("[ex:item]"));
            Assert.That(current.PredicateLexical, Is.EqualTo("ex:name"));
            Assert.That(current.IsLiteral, Is.True);
            Assert.That(current.IsValid, Is.True);
        });
        editor.ModifyRdfTriple(
            document,
            current,
            new OdfRdfTriple
            {
                Subject = "urn:example:item",
                Predicate = "urn:example:name",
                Object = "Updated name",
                IsLiteral = true
            });
        Assert.That(editor.RemoveRdfTriple(document, obsolete), Is.True);
        OdfNode target = FindStyleNode(document.Body, "MetadataTarget");
        editor.AddRdfTriple(
            document,
            new OdfRdfTriple
            {
                SourceKind = OdfRdfSourceKind.Rdfa,
                Subject = "urn:example:item",
                Predicate = "urn:example:note",
                Object = "Added note",
                IsLiteral = true,
                Node = target
            });
        OdfNode resourceTarget = FindStyleNode(
            document.Body,
            "ResourceTarget");
        editor.AddRdfTriple(
            document,
            new OdfRdfTriple
            {
                SourceKind = OdfRdfSourceKind.Rdfa,
                Subject = "urn:example:item",
                SubjectLexical = "[ex:item]",
                Predicate = "urn:example:related",
                PredicateLexical = "ex:related",
                Object = "urn:example:asset",
                ObjectLexical = "[ex:asset]",
                IsLiteral = false,
                Node = resourceTarget
            });

        OdfDocument roundTrip = WriteAndRead(document, "rdfa.fodt");
        Assert.That(editor.FindRdfTriples(
            roundTrip, "urn:example:item", "urn:example:name", "Updated name"),
            Has.Length.EqualTo(1));
        Assert.That(editor.FindRdfTriples(
            roundTrip, "urn:example:item", "urn:example:note", "Added note"),
            Has.Length.EqualTo(1));
        OdfRdfTriple related = editor.FindRdfTriples(
            roundTrip,
            "urn:example:item",
            "urn:example:related",
            "urn:example:asset").Single();
        Assert.That(related.SubjectLexical, Is.EqualTo("[ex:item]"));
        Assert.That(related.PredicateLexical, Is.EqualTo("ex:related"));
        Assert.That(related.ObjectLexical, Is.EqualTo("[ex:asset]"));
        Assert.That(related.IsLiteral, Is.False);
        Assert.That(editor.FindRdfTriples(
            roundTrip, null, "urn:example:obsolete", null), Is.Empty);
        Assert.That(editor.FindRdfTriples(roundTrip, null, null, null),
            Has.Length.EqualTo(3));
    }

    [Test]
    public void EditsPackageMetadataAndRdfXmlThroughThePublicEditor()
    {
        OdfDocument document = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        IOdfMetadataEditor editor = new OdfMetadataEditor();
        editor.AddPackageMetadataTriple(
            document,
            new OdfRdfTriple
            {
                Subject = OdfPartNames.MetadataManifest,
                Predicate = OdfNamespaces.PackageMetadata + "hasPart",
                Object = "content.xml",
                IsLiteral = false
            });
        editor.AddRdfTriple(
            document,
            new OdfRdfTriple
            {
                SourceKind = OdfRdfSourceKind.RdfXml,
                PartPath = "Metadata/editor.rdf",
                Subject = "urn:editor:item",
                Predicate = OdfNamespaces.DublinCore + "title",
                Object = "Editor graph",
                IsLiteral = true
            });

        OdfDocument loaded = WriteAndRead(document, "editor.odt");
        Assert.That(editor.GetPackageMetadataTriples(loaded),
            Has.Length.EqualTo(1));
        OdfRdfTriple packageTriple = editor.GetPackageMetadataTriples(loaded)[0];
        editor.ModifyRdfTriple(
            loaded,
            packageTriple,
            new OdfRdfTriple
            {
                Subject = OdfPartNames.MetadataManifest,
                Predicate = OdfNamespaces.PackageMetadata + "hasPart",
                Object = "styles.xml",
                IsLiteral = false
            });
        OdfRdfTriple graphTriple = editor.FindRdfTriples(
            loaded,
            "urn:editor:item",
            OdfNamespaces.DublinCore + "title",
            null)[0];
        Assert.That(editor.RemoveRdfTriple(loaded, graphTriple), Is.True);

        OdfDocument roundTrip = WriteAndRead(loaded, "updated.odt");
        Assert.That(editor.FindRdfTriples(
            roundTrip,
            OdfPartNames.MetadataManifest,
            OdfNamespaces.PackageMetadata + "hasPart",
            "styles.xml"), Has.Length.EqualTo(1));
        Assert.That(editor.FindRdfTriples(
            roundTrip, "urn:editor:item", null, null), Is.Empty);
    }

    [Test]
    public void PreservesForeignRdfXmlAndPackageOntologyContent()
    {
        OdfDocument document = ReadRdfPackage();
        IOdfMetadataEditor editor = new OdfMetadataEditor();
        OdfRdfTriple title = editor.FindRdfTriples(
            document,
            "urn:example:item",
            OdfNamespaces.DublinCore + "title",
            null).Single();
        OdfRdfTriple packagePart =
            editor.GetPackageMetadataTriples(document).Single(item =>
                item.Predicate == OdfNamespaces.PackageMetadata + "hasPart"
                && item.Object == "content.xml");

        editor.ModifyRdfTriple(
            document,
            title,
            new OdfRdfTriple
            {
                Subject = "urn:example:item",
                Predicate = OdfNamespaces.DublinCore + "title",
                Object = "Updated graph title",
                Language = "fr",
                IsLiteral = true
            });
        editor.ModifyRdfTriple(
            document,
            packagePart,
            new OdfRdfTriple
            {
                Subject = OdfPartNames.MetadataManifest,
                Predicate = OdfNamespaces.PackageMetadata + "hasPart",
                Object = "styles.xml",
                IsLiteral = false
            });
        editor.AddPackageMetadataTriple(
            document,
            new OdfRdfTriple
            {
                Subject = OdfPartNames.MetadataManifest,
                Predicate = OdfNamespaces.PackageMetadata + "hasPart",
                Object = OdfPartNames.Metadata,
                IsLiteral = false
            });

        OdfDocument roundTrip = WriteAndRead(document, "preserved.odt");
        OdfRdfTriple changed = editor.FindRdfTriples(
            roundTrip,
            "urn:example:item",
            OdfNamespaces.DublinCore + "title",
            "Updated graph title").Single();
        string graphXml = ReadPackageText(roundTrip, "Metadata/graph.rdf");
        string packageXml = ReadPackageText(
            roundTrip,
            OdfPartNames.MetadataManifest);

        Assert.Multiple(() =>
        {
            Assert.That(changed.SourceKind,
                Is.EqualTo(OdfRdfSourceKind.RdfXml));
            Assert.That(changed.Language, Is.EqualTo("fr"));
            Assert.That(editor.GetPackageMetadataTriples(roundTrip)
                .Select(item => item.Object), Does.Contain("styles.xml"));
            Assert.That(editor.GetPackageMetadataTriples(roundTrip)
                .Select(item => item.Object), Does.Contain(OdfPartNames.Metadata));
            Assert.That(graphXml, Does.Contain("foreign:tag=\"preserved\""));
            Assert.That(graphXml, Does.Contain("foreign graph comment"));
            Assert.That(packageXml,
                Does.Contain("foreign:tag=\"package-preserved\""));
            Assert.That(packageXml,
                Does.Contain("package metadata comment"));
        });
    }

    [Test]
    public void RejectsInvalidTypedMetadataWithoutChangingTheDocument()
    {
        OdfDocument document = new OdfDocumentFactory().CreateFlatDocument(
            OdfDocumentKind.Text);
        IOdfMetadataEditor editor = new OdfMetadataEditor();

        Assert.Throws<OdfDocumentException>(() => editor.SetUserMetadata(
            document,
            new OdfUserDefinedMetadata
            {
                Name = "Due",
                Kind = OdfValueKind.Date,
                DateValue = "not-a-date"
            }));
        Assert.That(document.Metadata.UserDefined, Is.Empty);
    }

    static OdfDocument WriteAndRead(OdfDocument document, string name)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes, name, new OdfLoadOptions());
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "metadata-editor.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument ReadRdfPackage()
    {
        OdfDocument skeleton = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage? package = skeleton.Package;
        Assert.That(package, Is.Not.Null);
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult packageMetadata = writer.AddPackageEntry(
            package,
            OdfPartNames.MetadataManifest,
            Encoding.UTF8.GetBytes(PackageMetadataRdf()),
            OdfCompressionMethod.Deflated);
        OdfEditResult graph = writer.AddPackageEntry(
            package,
            "Metadata/graph.rdf",
            Encoding.UTF8.GetBytes(GraphRdf()),
            OdfCompressionMethod.Deflated);
        Assert.That(packageMetadata.Status, Is.EqualTo(OdfEditStatus.Success));
        Assert.That(graph.Status, Is.EqualTo(OdfEditStatus.Success));
        byte[] bytes = writer.WritePackageBytes(
            package,
            new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "metadata-graph.odt",
            new OdfLoadOptions());
    }

    static string ReadPackageText(OdfDocument document, string path)
    {
        OdfPackage? package = document.Package;
        Assert.That(package, Is.Not.Null);
        using (Stream input = new OdfPackageReader().OpenPackageEntry(
            package,
            path))
        using (StreamReader reader = new StreamReader(input, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    static OdfNode FindStyleNode(OdfNode? node, string styleName)
    {
        if (node == null)
        {
            Assert.Fail("The document body was not found.");
            return new OdfNode();
        }
        OdfNode? found = FindStyleNodeOrNull(node, styleName);
        if (found == null)
        {
            Assert.Fail("The metadata target node was not found.");
            return new OdfNode();
        }
        return found;
    }

    static OdfNode? FindStyleNodeOrNull(OdfNode node, string styleName)
    {
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (attribute.Name.NamespaceName == OdfNamespaces.Text
                && attribute.Name.LocalName == "style-name"
                && attribute.Value == styleName)
            {
                return node;
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode? found = FindStyleNodeOrNull(
                node.Children[childIndex], styleName);
            if (found != null)
            {
                return found;
            }
            childIndex = childIndex + 1;
        }
        return null;
    }

    static string RdfaEditorXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:xhtml='" + OdfNamespaces.Xhtml
            + "' xmlns:ex='urn:example:' office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:body><office:text><text:p>"
            + "<text:span xhtml:about='[ex:item]' "
            + "xhtml:property='ex:name' xhtml:content='Old name'/>"
            + "<text:span xhtml:about='[ex:item]' "
            + "xhtml:property='ex:obsolete' xhtml:content='remove me'/>"
            + "<text:span text:style-name='MetadataTarget'>target</text:span>"
            + "<text:span text:style-name='ResourceTarget'>resource</text:span>"
            + "</text:p></office:text></office:body></office:document>";
    }

    static string PackageMetadataRdf()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<rdf:RDF xmlns:rdf='" + OdfNamespaces.Rdf
            + "' xmlns:pkg='" + OdfNamespaces.PackageMetadata
            + "' xmlns:foreign='urn:foreign:'>"
            + "<!--package metadata comment-->"
            + "<rdf:Description rdf:about='' "
            + "foreign:tag='package-preserved'>"
            + "<pkg:hasPart rdf:resource='content.xml'/>"
            + "</rdf:Description></rdf:RDF>";
    }

    static string GraphRdf()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<rdf:RDF xmlns:rdf='" + OdfNamespaces.Rdf
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:foreign='urn:foreign:'>"
            + "<!--foreign graph comment-->"
            + "<rdf:Description rdf:about='urn:example:item' "
            + "foreign:tag='preserved'>"
            + "<dc:title xml:lang='en'>Graph title</dc:title>"
            + "</rdf:Description></rdf:RDF>";
    }
}
