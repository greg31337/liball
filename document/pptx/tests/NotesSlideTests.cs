using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class NotesSlideTests
{
    [Test]
    public void Slide_LoadsNotesSlideTextAndShapeInventory()
    {
        PptxPresentation presentation = LoadNotesPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxNotesSlide? notes = slide.GetNotes();

        Assert.That(notes, Is.Not.Null);
        Assert.That(notes!.PartName, Is.EqualTo(PptxPartNames.FirstNotesSlide));
        Assert.That(notes.RelationshipId, Is.EqualTo("rIdNotes"));
        Assert.That(notes.GetText(), Is.EqualTo("Existing\nNotes"));
        Assert.That(slide.GetNotesText(), Is.EqualTo("Existing\nNotes"));
        Assert.That(presentation.GetSlideNotesText(0), Is.EqualTo("Existing\nNotes"));
        Assert.That(notes.ShapeCount(), Is.EqualTo(2));
        Assert.That(notes.GetShapeName(0), Is.EqualTo("Preserved Notes Shape"));
        Assert.That(notes.GetShapeName(1), Is.EqualTo("Notes Placeholder 1"));
        Assert.That(notes.GetShapeXml(0), Does.Contain("Preserved Unknown Notes XML"));
    }

    [Test]
    public void NotesShapesPreserveOnRoundTripAndSpeakerNotesEdit()
    {
        PptxPresentation presentation = LoadNotesPresentation();
        byte[] originalNotesBytes = presentation.Package.GetPart(PptxPartNames.FirstNotesSlide).GetBytes();
        PptxPresentation preserved = SaveReopenAndLoad(presentation);

        Assert.That(preserved.Package.GetPart(PptxPartNames.FirstNotesSlide).GetBytes(), Is.EqualTo(originalNotesBytes));

        PptxNotesSlide notes = preserved.GetSlide(0).GetNotes()!;
        notes.SetText("Updated\nSpeaker\tNotes");
        PptxPresentation reopened = SaveReopenAndLoad(preserved);
        PptxNotesSlide? reopenedNotes = reopened.GetSlide(0).GetNotes();
        PptxRelationship slideRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstNotesSlide), PptxRelationshipTypes.Slide);

        Assert.That(reopenedNotes, Is.Not.Null);
        Assert.That(reopenedNotes!.GetText(), Is.EqualTo("Updated\nSpeaker\tNotes"));
        Assert.That(reopenedNotes.ShapeCount(), Is.EqualTo(2));
        Assert.That(reopenedNotes.GetShapeName(0), Is.EqualTo("Preserved Notes Shape"));
        Assert.That(reopenedNotes.GetShapeXml(0), Does.Contain("Preserved Unknown Notes XML"));
        Assert.That(reopenedNotes.GetShapeXml(0), Does.Contain("Do Not Edit"));
        Assert.That(slideRelationship.TargetPartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_GetOrCreateNotesCreatesPartAndTextShape()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        Assert.That(slide.GetNotes(), Is.Null);

        slide.SetNotesText("Created\nNotes");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxNotesSlide? notes = reopened.GetSlide(0).GetNotes();
        PptxRelationship notesRelationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.NotesSlide);

        Assert.That(notes, Is.Not.Null);
        Assert.That(notes!.GetText(), Is.EqualTo("Created\nNotes"));
        Assert.That(notes.ShapeCount(), Is.EqualTo(1));
        Assert.That(notes.GetShapeName(0), Is.EqualTo("Notes Placeholder 1"));
        Assert.That(notesRelationship.TargetPartName, Is.EqualTo(PptxPartNames.FirstNotesSlide));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadNotesPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart(PptxPartNames.FirstNotesSlide, PptxContentTypes.NotesSlide, Encoding.UTF8.GetBytes(CreateNotesSlideXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdNotes", PptxRelationshipTypes.NotesSlide, "../notesSlides/notesSlide1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstNotesSlide, "rIdSlide", PptxRelationshipTypes.Slide, "../slides/slide1.xml", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateNotesSlideXml()
    {
        return
            "<p:notes xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/notes\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreatePreservedShapeXml() +
            CreateNotesPlaceholderShapeXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:notes>";
    }

    static string CreatePreservedShapeXml()
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"3\" name=\"Preserved Notes Shape\"><a:extLst><a:ext uri=\"notes-ext\"><x:data>Preserved Unknown Notes XML</x:data></a:ext></a:extLst></p:cNvPr><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Do Not Edit</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    static string CreateNotesPlaceholderShapeXml()
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Notes Placeholder 1\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Existing</a:t></a:r></a:p><a:p><a:r><a:t>Notes</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }
}
