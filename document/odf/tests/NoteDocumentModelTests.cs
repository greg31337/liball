using System.Text;

namespace odf.Tests;

public class NoteDocumentModelTests
{
    [Test]
    public void MaterializesNotesBodiesConfigurationsAndContinuationNotices()
    {
        OdfDocument document = ReadFlat(
            Configurations(),
            "<text:p>Text"
            + Footnote("foot-1", "custom", "7", "Footnote body")
            + "</text:p><text:p>More"
            + Endnote("end-1", "", "A", "Endnote body")
            + "</text:p>");
        OdfNote footnote = NoteNavigator.FindNote(document, "foot-1")!;
        OdfNote endnote = NoteNavigator.FindNote(document, "end-1")!;
        OdfNoteConfiguration footnoteConfiguration =
            NoteNavigator.FindConfiguration(document, OdfNoteKind.Footnote)!;

        Assert.Multiple(() =>
        {
            Assert.That(document.Notes, Has.Length.EqualTo(2));
            Assert.That(document.NoteConfigurations, Has.Length.EqualTo(2));
            Assert.That(footnote.Kind, Is.EqualTo(OdfNoteKind.Footnote));
            Assert.That(footnote.CitationLabel, Is.EqualTo("custom"));
            Assert.That(footnote.CitationText, Is.EqualTo("7"));
            Assert.That(footnote.TextBlocks.Single().Text, Is.EqualTo("Footnote body"));
            Assert.That(footnote.CitationBlock?.Text, Does.StartWith("Text"));
            Assert.That(footnote.Configuration, Is.SameAs(footnoteConfiguration));
            Assert.That(endnote.Kind, Is.EqualTo(OdfNoteKind.Endnote));
            Assert.That(endnote.TextBlocks.Single().Text, Is.EqualTo("Endnote body"));
            Assert.That(footnoteConfiguration.CitationStyleName, Is.EqualTo("FootCitation"));
            Assert.That(footnoteConfiguration.CitationBodyStyleName, Is.EqualTo("FootBodyCitation"));
            Assert.That(footnoteConfiguration.DefaultStyleName, Is.EqualTo("FootBody"));
            Assert.That(footnoteConfiguration.FootnotesPosition, Is.EqualTo("section"));
            Assert.That(footnoteConfiguration.ForwardNotice, Is.EqualTo("Continued forward"));
            Assert.That(footnoteConfiguration.BackwardNotice, Is.EqualTo("Continued backward"));
        });
    }

    [Test]
    public void ComputesConfiguredNumberingAndRestartsAtChapters()
    {
        OdfDocument document = ReadFlat(
            Configurations(),
            "<text:h text:outline-level='1'>First chapter</text:h>"
            + "<text:p>One" + Footnote("f1", "", "old", "First") + "</text:p>"
            + "<text:p>Two" + Footnote("f2", "", "old", "Second") + "</text:p>"
            + "<text:h text:outline-level='1'>Second chapter</text:h>"
            + "<text:p>Three" + Footnote("f3", "", "old", "Third") + "</text:p>"
            + "<text:p>End" + Endnote("e1", "", "old", "End") + "</text:p>");

        Assert.Multiple(() =>
        {
            Assert.That(NoteNavigator.FindNote(document, "f1")?.Ordinal, Is.EqualTo(3));
            Assert.That(NoteNavigator.FindNote(document, "f1")?.ComputedCitation, Is.EqualTo("[iii]"));
            Assert.That(NoteNavigator.FindNote(document, "f2")?.Ordinal, Is.EqualTo(4));
            Assert.That(NoteNavigator.FindNote(document, "f2")?.ComputedCitation, Is.EqualTo("[iv]"));
            Assert.That(NoteNavigator.FindNote(document, "f3")?.Ordinal, Is.EqualTo(3));
            Assert.That(NoteNavigator.FindNote(document, "f3")?.ComputedCitation, Is.EqualTo("[iii]"));
            Assert.That(NoteNavigator.FindNote(document, "e1")?.Ordinal, Is.EqualTo(28));
            Assert.That(NoteNavigator.FindNote(document, "e1")?.ComputedCitation, Is.EqualTo("BB."));
        });
    }

    [Test]
    public void RestartsPageScopedNumberingAtExplicitPageBoundaries()
    {
        OdfDocument document = ReadFlat(
            "<text:notes-configuration text:note-class='footnote' "
            + "text:start-value='5' text:start-numbering-at='page'/>",
            "<text:p>One" + Footnote("p1", "", "old", "First") + "</text:p>"
            + "<text:p>Two" + Footnote("p2", "", "old", "Second") + "</text:p>"
            + "<text:p><text:soft-page-break/>Three"
            + Footnote("p3", "", "old", "Third") + "</text:p>");

        Assert.Multiple(() =>
        {
            Assert.That(NoteNavigator.FindNote(document, "p1")?.Ordinal, Is.EqualTo(5));
            Assert.That(NoteNavigator.FindNote(document, "p2")?.Ordinal, Is.EqualTo(6));
            Assert.That(NoteNavigator.FindNote(document, "p3")?.Ordinal, Is.EqualTo(5));
        });
    }

    [Test]
    public void WritesNoteConfigurationCitationBodyAndReferenceChanges()
    {
        OdfDocument document = ReadFlat(
            Configurations(),
            "<text:p>Text" + Footnote("old-id", "old-label", "1", "Old body")
            + " and <text:note-ref text:ref-name='old-id' text:reference-format='text'>1</text:note-ref>"
            + "</text:p>");
        OdfNote note = NoteNavigator.FindNote(document, "old-id")!;
        OdfNoteConfiguration configuration =
            NoteNavigator.FindConfiguration(document, OdfNoteKind.Footnote)!;

        NoteNavigator.RenameNoteId(document, note, "new-id");
        note.CitationLabel = "edited-label";
        note.CitationText = "edited-citation";
        note.TextBlocks[0].Text = "Edited body";
        configuration.NumberPrefix = "(";
        configuration.NumberSuffix = ")";
        configuration.ForwardNotice = "New forward notice";
        configuration.BackwardNotice = "New backward notice";
        OdfDocument reread = WriteAndRead(document);
        OdfNote rereadNote = NoteNavigator.FindNote(reread, "new-id")!;
        OdfNoteConfiguration rereadConfiguration =
            NoteNavigator.FindConfiguration(reread, OdfNoteKind.Footnote)!;
        OdfTextField reference = reread.TextFields.Single(
            item => item.ElementName == "note-ref");

        Assert.Multiple(() =>
        {
            Assert.That(NoteNavigator.FindNote(reread, "old-id"), Is.Null);
            Assert.That(rereadNote.CitationLabel, Is.EqualTo("edited-label"));
            Assert.That(rereadNote.CitationText, Is.EqualTo("edited-citation"));
            Assert.That(rereadNote.TextBlocks.Single().Text, Is.EqualTo("Edited body"));
            Assert.That(reference.ReferenceName, Is.EqualTo("new-id"));
            Assert.That(rereadConfiguration.NumberPrefix, Is.EqualTo("("));
            Assert.That(rereadConfiguration.NumberSuffix, Is.EqualTo(")"));
            Assert.That(rereadConfiguration.ForwardNotice, Is.EqualTo("New forward notice"));
            Assert.That(rereadConfiguration.BackwardNotice, Is.EqualTo("New backward notice"));
        });
    }

    [Test]
    public void AddsRemovesAndDetectsAmbiguousConfigurations()
    {
        OdfDocument document = ReadFlat(
            "<text:notes-configuration text:note-class='footnote'/>"
            + "<text:notes-configuration text:note-class='footnote'/>",
            "<text:p>Text" + Footnote("f1", "", "1", "Body") + "</text:p>");
        OdfNote note = document.Notes.Single();

        Assert.That(note.Configuration, Is.Null);
        Assert.That(note.HasAmbiguousConfiguration, Is.True);

        OdfNoteConfiguration added = new OdfNoteConfiguration
        {
            Kind = OdfNoteKind.Endnote,
            StartValue = "5",
            NumberFormat = "A",
            NumberSuffix = ")",
            ForwardNotice = "Forward"
        };
        document.NoteConfigurations = [added];
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.NoteConfigurations, Has.Length.EqualTo(1));
            Assert.That(reread.NoteConfigurations[0].Kind, Is.EqualTo(OdfNoteKind.Endnote));
            Assert.That(reread.NoteConfigurations[0].StartValue, Is.EqualTo("5"));
            Assert.That(reread.NoteConfigurations[0].NumberFormat, Is.EqualTo("A"));
            Assert.That(reread.NoteConfigurations[0].NumberSuffix, Is.EqualTo(")"));
            Assert.That(reread.NoteConfigurations[0].ForwardNotice, Is.EqualTo("Forward"));
            Assert.That(reread.NoteConfigurations[0].Node, Is.Not.Null);
            Assert.That(reread.Notes.Single().Configuration, Is.Null);
            Assert.That(reread.Notes.Single().HasAmbiguousConfiguration, Is.False);
        });
    }

    [Test]
    public void StyleRenameUpdatesTypedNoteConfigurationReferences()
    {
        OdfDocument document = ReadFlat(
            "<style:style style:name='FootCitation' style:family='text'/>"
            + "<style:style style:name='FootBody' style:family='paragraph'/>"
            + "<text:notes-configuration text:note-class='footnote' "
            + "text:citation-style-name='FootCitation' "
            + "text:citation-body-style-name='FootCitation' "
            + "text:default-style-name='FootBody'/>",
            "<text:p>Text" + Footnote("f1", "", "1", "Body") + "</text:p>");
        OdfStyle textStyle = document.Styles.Single(
            item => item.Family == OdfStyleFamily.Text);
        OdfStyle paragraphStyle = document.Styles.Single(
            item => item.Family == OdfStyleFamily.Paragraph);

        StyleMaintenanceModel.RenameStyle(document, textStyle, "NewCitation");
        StyleMaintenanceModel.RenameStyle(document, paragraphStyle, "NewBody");
        OdfDocument reread = WriteAndRead(document);
        OdfNoteConfiguration configuration = reread.NoteConfigurations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.CitationStyleName, Is.EqualTo("NewCitation"));
            Assert.That(configuration.CitationBodyStyleName, Is.EqualTo("NewCitation"));
            Assert.That(configuration.DefaultStyleName, Is.EqualTo("NewBody"));
        });
    }

    private static string Configurations()
    {
        return "<text:notes-configuration text:note-class='footnote' "
            + "text:citation-style-name='FootCitation' "
            + "text:citation-body-style-name='FootBodyCitation' "
            + "text:default-style-name='FootBody' text:start-value='3' "
            + "text:start-numbering-at='chapter' text:footnotes-position='section' "
            + "style:num-format='i' style:num-prefix='[' style:num-suffix=']' "
            + "style:num-letter-sync='true'>"
            + "<text:note-continuation-notice-forward>Continued forward</text:note-continuation-notice-forward>"
            + "<text:note-continuation-notice-backward>Continued backward</text:note-continuation-notice-backward>"
            + "</text:notes-configuration>"
            + "<text:notes-configuration text:note-class='endnote' "
            + "text:start-value='28' style:num-format='A' style:num-suffix='.'/>";
    }

    private static string Footnote(
        string id,
        string label,
        string citation,
        string body)
    {
        return "<text:note text:id='" + id + "' text:note-class='footnote'>"
            + "<text:note-citation text:label='" + label + "'>" + citation
            + "</text:note-citation><text:note-body><text:p>" + body
            + "</text:p></text:note-body></text:note>";
    }

    private static string Endnote(
        string id,
        string label,
        string citation,
        string body)
    {
        return "<text:note text:id='" + id + "' text:note-class='endnote'>"
            + "<text:note-citation text:label='" + label + "'>" + citation
            + "</text:note-citation>"
            + "<text:note-body><text:p>" + body
            + "</text:p></text:note-body></text:note>";
    }

    private static OdfDocument ReadFlat(string styles, string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles>" + styles + "</office:styles>"
            + "<office:body><office:text>" + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "notes.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "notes.fodt",
            new OdfLoadOptions());
    }
}
