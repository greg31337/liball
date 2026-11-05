using System.Text;

namespace odf.Tests;

public class TextIndexDocumentModelTests
{
    [Test]
    public void MaterializesEveryIndexFamilyAndGeneratedBodies()
    {
        OdfDocument document = ReadFlat(
            "",
            SimpleIndex("table-of-content", "Contents", "TOC")
            + SimpleIndex("alphabetical-index", "Alphabetical", "Alpha")
            + SimpleIndex("bibliography", "Bibliography", "Bibliography")
            + SimpleIndex("user-index", "User", "User")
            + SimpleIndex("table-index", "Tables", "Tables")
            + SimpleIndex("illustration-index", "Figures", "Figures")
            + SimpleIndex("object-index", "Objects", "Objects"));

        Assert.Multiple(() =>
        {
            Assert.That(document.TextIndexes, Has.Length.EqualTo(7));
            Assert.That(document.TextIndexes.Select(item => item.Kind), Is.EqualTo(new[]
            {
                OdfTextIndexKind.TableOfContent,
                OdfTextIndexKind.Alphabetical,
                OdfTextIndexKind.Bibliography,
                OdfTextIndexKind.User,
                OdfTextIndexKind.Table,
                OdfTextIndexKind.Illustration,
                OdfTextIndexKind.Object
            }));
            Assert.That(document.TextIndexes[0].Name, Is.EqualTo("Contents"));
            Assert.That(document.TextIndexes[0].BodyBlocks.Single().Text, Is.EqualTo("TOC"));
            Assert.That(document.TextIndexes[6].BodyBlocks.Single().Text, Is.EqualTo("Objects"));
            Assert.That(document.TextIndexes.All(item => item.Source != null), Is.True);
        });
    }

    [Test]
    public void MaterializesIndexSourceTitleStylesTemplatesAndComponents()
    {
        string body = "<text:table-of-content text:name='Contents'>"
            + "<text:table-of-content-source text:outline-level='3' "
            + "text:use-index-marks='true' text:use-index-source-styles='true' "
            + "text:index-scope='chapter' text:relative-tab-stop-position='false'>"
            + "<text:index-title-template text:style-name='IndexTitle'>Contents</text:index-title-template>"
            + "<text:table-of-content-entry-template text:outline-level='1' text:style-name='IndexOne'>"
            + "<text:index-entry-link-start text:style-name='Link'/>"
            + "<text:index-entry-chapter text:display='number' text:outline-level='1'/>"
            + "<text:index-entry-span text:style-name='Literal'>: </text:index-entry-span>"
            + "<text:index-entry-text/><text:index-entry-tab-stop style:type='right' style:leader-char='.'/>"
            + "<text:index-entry-page-number/><text:index-entry-link-end/>"
            + "</text:table-of-content-entry-template>"
            + "<text:index-source-styles text:outline-level='1'>"
            + "<text:index-source-style text:style-name='Heading 1'/>"
            + "<text:index-source-style text:style-name='Appendix Heading'/>"
            + "</text:index-source-styles></text:table-of-content-source>"
            + "<text:index-body><text:p>Generated</text:p></text:index-body>"
            + "</text:table-of-content>";
        OdfTextIndex item = ReadFlat("", body).TextIndexes.Single();
        OdfTextIndexSource source = item.Source!;
        OdfIndexEntryTemplate template = source.Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(source.OutlineLevel, Is.EqualTo("3"));
            Assert.That(source.UseIndexMarks, Is.EqualTo("true"));
            Assert.That(source.UseIndexSourceStyles, Is.EqualTo("true"));
            Assert.That(source.IndexScope, Is.EqualTo("chapter"));
            Assert.That(source.Title, Is.EqualTo("Contents"));
            Assert.That(source.TitleStyleName, Is.EqualTo("IndexTitle"));
            Assert.That(template.OutlineLevel, Is.EqualTo("1"));
            Assert.That(template.StyleName, Is.EqualTo("IndexOne"));
            Assert.That(template.Components.Select(component => component.Kind), Is.EqualTo(new[]
            {
                OdfIndexEntryComponentKind.LinkStart,
                OdfIndexEntryComponentKind.Chapter,
                OdfIndexEntryComponentKind.Span,
                OdfIndexEntryComponentKind.Text,
                OdfIndexEntryComponentKind.TabStop,
                OdfIndexEntryComponentKind.PageNumber,
                OdfIndexEntryComponentKind.LinkEnd
            }));
            Assert.That(template.Components[1].Display, Is.EqualTo("number"));
            Assert.That(template.Components[2].Text, Is.EqualTo(": "));
            Assert.That(template.Components[4].TabType, Is.EqualTo("right"));
            Assert.That(template.Components[4].LeaderCharacter, Is.EqualTo("."));
            Assert.That(source.SourceStyles.Single().StyleNames,
                Is.EqualTo(new[] { "Heading 1", "Appendix Heading" }));
        });
    }

    [Test]
    public void WritesIndexSourceTemplateGeneratedBodyConfigurationAndEntryChanges()
    {
        OdfDocument document = ReadFlat(
            BibliographyConfiguration(),
            "<text:bibliography text:name='References'>"
            + "<text:bibliography-source><text:index-title-template text:style-name='TitleStyle'>References</text:index-title-template>"
            + "<text:bibliography-entry-template text:bibliography-type='book' text:style-name='EntryStyle'>"
            + "<text:index-entry-bibliography text:bibliography-data-field='author'/>"
            + "<text:index-entry-span>, </text:index-entry-span>"
            + "<text:index-entry-bibliography text:bibliography-data-field='title'/>"
            + "</text:bibliography-entry-template></text:bibliography-source>"
            + "<text:index-body><text:p>Old generated body</text:p></text:index-body>"
            + "</text:bibliography>"
            + "<text:p>Citation <text:bibliography-mark text:identifier='id-1' "
            + "text:bibliography-type='book' text:author='Old Author' "
            + "text:title='Old Title' text:year='2020' text:url='old'>[1]</text:bibliography-mark></text:p>");
        OdfTextIndex item = document.TextIndexes.Single();
        OdfTextIndexSource source = item.Source!;
        OdfIndexEntryTemplate template = source.Templates.Single();
        OdfBibliographyConfiguration configuration =
            document.BibliographyConfigurations.Single();
        OdfBibliographyEntry entry = document.BibliographyEntries.Single();

        item.Name = "Works";
        item.BodyBlocks[0].Text = "New generated body";
        source.Title = "Works cited";
        source.TitleStyleName = "NewTitleStyle";
        template.StyleName = "NewEntryStyle";
        template.Components[1].Text = "; ";
        configuration.Prefix = "(";
        configuration.Suffix = ")";
        configuration.SortKeys =
        [
            new OdfBibliographySortKey { Key = "year", SortAscending = "false" },
            new OdfBibliographySortKey { Key = "author", SortAscending = "true" }
        ];
        entry.Identifier = "id-2";
        entry.Author = "New Author";
        entry.Title = "New Title";
        entry.Year = "2026";
        entry.Url = "https://example.test/book";
        entry.DisplayText = "[2]";
        OdfDocument reread = WriteAndRead(document);
        OdfTextIndex rereadIndex = reread.TextIndexes.Single();
        OdfBibliographyEntry rereadEntry = reread.BibliographyEntries.Single();
        OdfBibliographyConfiguration rereadConfiguration =
            reread.BibliographyConfigurations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(rereadIndex.Name, Is.EqualTo("Works"));
            Assert.That(rereadIndex.BodyBlocks.Single().Text, Is.EqualTo("New generated body"));
            Assert.That(rereadIndex.Source?.Title, Is.EqualTo("Works cited"));
            Assert.That(rereadIndex.Source?.TitleStyleName, Is.EqualTo("NewTitleStyle"));
            Assert.That(rereadIndex.Source?.Templates[0].StyleName, Is.EqualTo("NewEntryStyle"));
            Assert.That(rereadIndex.Source?.Templates[0].Components[1].Text, Is.EqualTo("; "));
            Assert.That(rereadConfiguration.Prefix, Is.EqualTo("("));
            Assert.That(rereadConfiguration.Suffix, Is.EqualTo(")"));
            Assert.That(rereadConfiguration.SortKeys.Select(key => key.Key),
                Is.EqualTo(new[] { "year", "author" }));
            Assert.That(rereadConfiguration.SortKeys[0].SortAscending, Is.EqualTo("false"));
            Assert.That(rereadEntry.Identifier, Is.EqualTo("id-2"));
            Assert.That(rereadEntry.Author, Is.EqualTo("New Author"));
            Assert.That(rereadEntry.Title, Is.EqualTo("New Title"));
            Assert.That(rereadEntry.Year, Is.EqualTo("2026"));
            Assert.That(rereadEntry.Url, Is.EqualTo("https://example.test/book"));
            Assert.That(rereadEntry.DisplayText, Is.EqualTo("[2]"));
            Assert.That(rereadEntry.Run, Is.Not.Null);
            Assert.That(rereadEntry.TextBlock, Is.Not.Null);
        });
    }

    [Test]
    public void AddsTemplatesComponentsAndConfigurationAndRemovesIndexesAndEntries()
    {
        OdfDocument document = ReadFlat(
            "",
            SimpleIndex("table-of-content", "Remove", "Remove body")
            + "<text:alphabetical-index text:name='Keep'>"
            + "<text:alphabetical-index-source><text:alphabetical-index-entry-template "
            + "text:outline-level='1' text:style-name='Alpha'/></text:alphabetical-index-source>"
            + "<text:index-body><text:p>Keep body</text:p></text:index-body></text:alphabetical-index>"
            + "<text:p><text:bibliography-mark text:bibliography-type='book' text:identifier='remove'>[1]</text:bibliography-mark></text:p>");
        OdfTextIndex kept = document.TextIndexes[1];
        OdfIndexEntryTemplate template = kept.Source!.Templates[0];
        template.Components =
        [
            new OdfIndexEntryComponent
            {
                Kind = OdfIndexEntryComponentKind.Span,
                StyleName = "Separator",
                Text = " — "
            },
            new OdfIndexEntryComponent
            {
                Kind = OdfIndexEntryComponentKind.PageNumber,
                StyleName = "Page"
            }
        ];
        document.TextIndexes = [kept];
        document.BibliographyEntries = [];
        document.BibliographyConfigurations =
        [
            new OdfBibliographyConfiguration
            {
                Prefix = "[",
                Suffix = "]",
                NumberedEntries = "true",
                SortByPosition = "false",
                Language = "en",
                Country = "US",
                SortKeys =
                [
                    new OdfBibliographySortKey
                    {
                        Key = "author",
                        SortAscending = "true"
                    }
                ]
            }
        ];
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextIndexes, Has.Length.EqualTo(1));
            Assert.That(reread.TextIndexes[0].Name, Is.EqualTo("Keep"));
            Assert.That(reread.TextIndexes[0].Source?.Templates[0].Components,
                Has.Length.EqualTo(2));
            Assert.That(reread.TextIndexes[0].Source?.Templates[0].Components[0].Text,
                Is.EqualTo(" — "));
            Assert.That(reread.BibliographyEntries, Is.Empty);
            Assert.That(reread.BibliographyConfigurations, Has.Length.EqualTo(1));
            Assert.That(reread.BibliographyConfigurations[0].NumberedEntries,
                Is.EqualTo("true"));
            Assert.That(reread.BibliographyConfigurations[0].SortKeys[0].Key,
                Is.EqualTo("author"));
        });
    }

    [Test]
    public void StyleRenameUpdatesIndexSectionTemplateSourceAndComponentReferences()
    {
        OdfDocument document = ReadFlat(
            "<style:style style:name='IndexSection' style:family='section'/>"
            + "<style:style style:name='IndexParagraph' style:family='paragraph'/>"
            + "<style:style style:name='IndexText' style:family='text'/>",
            "<text:table-of-content text:name='Contents' text:style-name='IndexSection'>"
            + "<text:table-of-content-source text:main-entry-style-name='IndexText'>"
            + "<text:index-title-template text:style-name='IndexParagraph'>Title</text:index-title-template>"
            + "<text:table-of-content-entry-template text:outline-level='1' text:style-name='IndexParagraph'>"
            + "<text:index-entry-text text:style-name='IndexText'/></text:table-of-content-entry-template>"
            + "<text:index-source-styles text:outline-level='1'>"
            + "<text:index-source-style text:style-name='IndexParagraph'/></text:index-source-styles>"
            + "</text:table-of-content-source><text:index-body><text:p>Body</text:p>"
            + "</text:index-body></text:table-of-content>");
        OdfStyle section = document.Styles.Single(
            item => item.Family == OdfStyleFamily.Section);
        OdfStyle paragraph = document.Styles.Single(
            item => item.Family == OdfStyleFamily.Paragraph);
        OdfStyle textStyle = document.Styles.Single(
            item => item.Family == OdfStyleFamily.Text);

        StyleMaintenanceModel.RenameStyle(document, section, "NewSection");
        StyleMaintenanceModel.RenameStyle(document, paragraph, "NewParagraph");
        StyleMaintenanceModel.RenameStyle(document, textStyle, "NewText");
        OdfTextIndex reread = WriteAndRead(document).TextIndexes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(reread.StyleName, Is.EqualTo("NewSection"));
            Assert.That(reread.Source?.TitleStyleName, Is.EqualTo("NewParagraph"));
            Assert.That(reread.Source?.Templates[0].StyleName, Is.EqualTo("NewParagraph"));
            Assert.That(reread.Source?.SourceStyles[0].StyleNames[0], Is.EqualTo("NewParagraph"));
            Assert.That(reread.Source?.MainEntryStyleName, Is.EqualTo("NewText"));
            Assert.That(reread.Source?.Templates[0].Components[0].StyleName,
                Is.EqualTo("NewText"));
        });
    }

    private static string SimpleIndex(string element, string name, string body)
    {
        string source = element == "table-of-content"
            ? "table-of-content-source"
            : element + "-source";
        return "<text:" + element + " text:name='" + name + "'>"
            + "<text:" + source + "/>"
            + "<text:index-body><text:p>" + body + "</text:p></text:index-body>"
            + "</text:" + element + ">";
    }

    private static string BibliographyConfiguration()
    {
        return "<text:bibliography-configuration text:prefix='[' text:suffix=']' "
            + "text:numbered-entries='true' text:sort-by-position='false' "
            + "text:sort-algorithm='alphanumeric' fo:language='en' fo:country='US'>"
            + "<text:sort-key text:key='author' text:sort-ascending='true'/>"
            + "<text:sort-key text:key='title' text:sort-ascending='true'/>"
            + "</text:bibliography-configuration>";
    }

    private static OdfDocument ReadFlat(string styles, string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles>" + styles + "</office:styles>"
            + "<office:body><office:text>" + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "indexes.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions { Deterministic = true });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "indexes.fodt",
            new OdfLoadOptions());
    }
}
