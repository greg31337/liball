using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class NotesAndHandoutOperationsTests
{
    [Test]
    public void PublicOperations_ReadMetadataTreesAndPreserveUnsupportedContentDuringSpeakerEdit()
    {
        PptxPresentation presentation = LoadPresentation(CreateRichPackage());
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxNotesSlideOperations notesSlides = new PptxNotesSlideOperations();
        PptxNotesMasterOperations notesMasters = new PptxNotesMasterOperations();
        PptxHandoutMasterOperations handoutMasters = new PptxHandoutMasterOperations();
        PptxNotesSlide notes = presentations.GetNotesSlide(presentation, 0)!;
        PptxNotesMaster notesMaster = presentations.GetNotesMaster(presentation)!;
        PptxHandoutMaster handoutMaster = presentations.GetHandoutMaster(presentation)!;

        PptxPartMetadata notesMetadata = notesSlides.GetMetadata(notes);
        PptxPartMetadata notesMasterMetadata = notesMasters.GetMetadata(notesMaster);
        PptxPartMetadata handoutMetadata = handoutMasters.GetMetadata(handoutMaster);
        PptxPartShapeTree notesTree = notesSlides.GetShapeTree(notes);
        PptxPartShapeTree notesMasterTree = notesMasters.GetShapeTree(notesMaster);
        PptxPartShapeTree handoutTree = handoutMasters.GetShapeTree(handoutMaster);

        Assert.That(presentations.NotesSlideCount(presentation), Is.EqualTo(1));
        Assert.That(notesMetadata.Name, Is.EqualTo("Slide Notes"));
        Assert.That(notesMetadata.ShowMasterShapes, Is.False);
        Assert.That(notesMetadata.ShapeCount, Is.EqualTo(2));
        Assert.That(notesMetadata.PlaceholderCount, Is.EqualTo(1));
        Assert.That(notesMasterMetadata.Name, Is.EqualTo("Notes Master"));
        Assert.That(notesMasterMetadata.ThemeCount, Is.EqualTo(1));
        Assert.That(notesMasterMetadata.PlaceholderCount, Is.EqualTo(1));
        Assert.That(handoutMetadata.Name, Is.EqualTo("Handout Master"));
        Assert.That(handoutMetadata.ThemeCount, Is.EqualTo(1));
        Assert.That(notesTree.Shapes[0].Name, Is.EqualTo("Preserved Notes Shape"));
        Assert.That(notesTree.Shapes[0].RawXml, Does.Contain("notes-unknown"));
        Assert.That(notesTree.Shapes[1].PlaceholderType, Is.EqualTo("body"));
        Assert.That(notesMasterTree.Shapes[0].Name, Is.EqualTo("Master Footer"));
        Assert.That(notesMasterTree.Shapes[0].PlaceholderType, Is.EqualTo("ftr"));
        Assert.That(handoutTree.Shapes[0].Name, Is.EqualTo("Handout Shape"));
        Assert.That(notesMasters.GetTheme(notesMaster), Is.Not.Null);
        Assert.That(handoutMasters.GetTheme(handoutMaster), Is.Not.Null);

        notesSlides.SetText(notes, "Updated\nSpeaker Notes");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxNotesSlide reopenedNotes = presentations.GetNotesSlide(reopened, 0)!;

        Assert.That(notesSlides.GetText(reopenedNotes), Is.EqualTo("Updated\nSpeaker Notes"));
        Assert.That(notesSlides.GetShapeTree(reopenedNotes).Shapes[0].RawXml, Does.Contain("notes-unknown"));
        Assert.That(Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstNotesMaster).GetBytes()), Does.Contain("master-unknown"));
        Assert.That(Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstHandoutMaster).GetBytes()), Does.Contain("handout-unknown"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CreationAndRemoval_PersistCompleteRelationshipGraph()
    {
        PptxPresentation presentation = LoadMinimumPresentation();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxNotesSlideOperations notesSlides = new PptxNotesSlideOperations();
        PptxSlide slide = presentation.Slides[0];
        PptxNotesSlide notes = presentations.GetOrCreateNotesForSlide(presentation, slide);
        notesSlides.SetText(notes, "Created speaker notes");
        PptxNotesMaster notesMaster = presentations.GetOrCreateNotesMaster(presentation);
        PptxHandoutMaster handoutMaster = presentations.GetOrCreateHandoutMaster(presentation);

        Assert.That(notesMaster.PartName, Is.EqualTo(PptxPartNames.FirstNotesMaster));
        Assert.That(handoutMaster.PartName, Is.EqualTo(PptxPartNames.FirstHandoutMaster));
        Assert.That(FindRelationship(presentation.Package.GetRelationships(notes.PartName), PptxRelationshipTypes.NotesMaster), Is.Not.Null);

        PptxPresentation created = SaveReopenAndLoad(presentation);
        Assert.That(presentations.GetNotesMaster(created), Is.Not.Null);
        Assert.That(presentations.GetHandoutMaster(created), Is.Not.Null);
        Assert.That(presentations.GetNotesSlide(created, 0), Is.Not.Null);
        Assert.That(notesSlides.GetText(presentations.GetNotesSlide(created, 0)!), Is.EqualTo("Created speaker notes"));
        Assert.That(created.Package.Validate().Succeeded(), Is.True, created.Package.Validate().FormatDiagnostics());

        Assert.That(notesSlides.Remove(presentations.GetNotesSlide(created, 0)!), Is.True);
        Assert.That(presentations.RemoveNotesMaster(created), Is.True);
        Assert.That(presentations.RemoveHandoutMaster(created), Is.True);
        PptxPresentation removed = SaveReopenAndLoad(created);

        Assert.That(presentations.NotesSlideCount(removed), Is.Zero);
        Assert.That(presentations.GetNotesMaster(removed), Is.Null);
        Assert.That(presentations.GetHandoutMaster(removed), Is.Null);
        Assert.That(removed.Package.HasPart(PptxPartNames.FirstNotesSlide), Is.False);
        Assert.That(removed.Package.HasPart(PptxPartNames.FirstNotesMaster), Is.False);
        Assert.That(removed.Package.HasPart(PptxPartNames.FirstHandoutMaster), Is.False);
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void RemovingSlide_RemovesItsOwnedNotesPartOnly()
    {
        PptxPresentation presentation = LoadMinimumPresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxSlide secondSlide = operations.AddSlide(presentation);
        operations.GetOrCreateNotesForSlide(presentation, presentation.Slides[0]).SetText("First");
        operations.GetOrCreateNotesForSlide(presentation, secondSlide).SetText("Second");
        string firstNotesPartName = operations.GetNotesSlide(presentation, 0)!.PartName;
        string secondNotesPartName = operations.GetNotesSlide(presentation, 1)!.PartName;

        operations.RemoveSlideAt(presentation, 0);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.Package.HasPart(firstNotesPartName), Is.False);
        Assert.That(reopened.Package.HasPart(secondNotesPartName), Is.True);
        Assert.That(operations.NotesSlideCount(reopened), Is.EqualTo(1));
        Assert.That(operations.GetNotesSlide(reopened, 0)!.GetText(), Is.EqualTo("Second"));
    }

    static PptxPackage CreateRichPackage()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart(PptxPartNames.FirstNotesSlide, PptxContentTypes.NotesSlide, Encoding.UTF8.GetBytes(CreateNotesSlideXml()));
        package.AddPart(PptxPartNames.FirstNotesMaster, PptxContentTypes.NotesMaster, Encoding.UTF8.GetBytes(CreateNotesMasterXml()));
        package.AddPart(PptxPartNames.FirstHandoutMaster, PptxContentTypes.HandoutMaster, Encoding.UTF8.GetBytes(CreateHandoutMasterXml()));
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.NotesMaster, "notesMasters/notesMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.HandoutMaster, "handoutMasters/handoutMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdNotes", PptxRelationshipTypes.NotesSlide, "../notesSlides/notesSlide1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstNotesSlide, PptxRelationshipTypes.Slide, "../slides/slide1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstNotesSlide, PptxRelationshipTypes.NotesMaster, "../notesMasters/notesMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstNotesMaster, PptxRelationshipTypes.Theme, "../theme/theme1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstHandoutMaster, PptxRelationshipTypes.Theme, "../theme/theme1.xml", PptxRelationshipTargetMode.Internal);
        return package;
    }

    static PptxPresentation LoadMinimumPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation LoadPresentation(PptxPackage package)
    {
        return PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxRelationship? FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
            }
        }

        return null;
    }

    static string CreateNotesSlideXml()
    {
        return
            "<p:notes xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/notes\" showMasterSp=\"0\">" +
            "<p:cSld name=\"Slide Notes\"><p:spTree>" + GroupPropertiesXml() +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"3\" name=\"Preserved Notes Shape\"><a:extLst><a:ext uri=\"notes-unknown\"><x:data>Keep Notes</x:data></a:ext></a:extLst></p:cNvPr><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Keep</a:t></a:r></a:p></p:txBody></p:sp>" +
            PlaceholderShapeXml("2", "Notes Placeholder", "body", "Existing Notes") +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:notes>";
    }

    static string CreateNotesMasterXml()
    {
        return
            "<p:notesMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/master\">" +
            "<p:cSld name=\"Notes Master\"><p:spTree>" + GroupPropertiesXml() + PlaceholderShapeXml("2", "Master Footer", "ftr", "Footer") +
            "</p:spTree><p:extLst><p:ext uri=\"master-unknown\"><x:data>Keep Master</x:data></p:ext></p:extLst></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/><p:hf/><p:notesStyle/></p:notesMaster>";
    }

    static string CreateHandoutMasterXml()
    {
        return
            "<p:handoutMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/handout\">" +
            "<p:cSld name=\"Handout Master\"><p:spTree>" + GroupPropertiesXml() +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Handout Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Handout</a:t></a:r></a:p></p:txBody></p:sp>" +
            "</p:spTree><p:extLst><p:ext uri=\"handout-unknown\"><x:data>Keep Handout</x:data></p:ext></p:extLst></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/><p:hf/></p:handoutMaster>";
    }

    static string GroupPropertiesXml()
    {
        return "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";
    }

    static string PlaceholderShapeXml(string id, string name, string type, string text)
    {
        return
            "<p:sp><p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"" + type + "\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></p:txBody></p:sp>";
    }
}
