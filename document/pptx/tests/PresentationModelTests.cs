using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PresentationModelTests
{
    [Test]
    public void LoadPresentation_FromGeneratedDeckLoadsSlidesMastersLayoutsThemeAndProperties()
    {
        PptxDocument document = Reopen(PptxDocument.CreateMinimumPresentation());

        PptxPresentation presentation = document.LoadPresentation();

        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        Assert.That(presentation.Part, Is.Not.Null);
        Assert.That(presentation.Part!.PartName, Is.EqualTo(PptxPartNames.Presentation));
        Assert.That(presentation.CoreProperties, Is.Not.Null);
        Assert.That(presentation.AppProperties, Is.Not.Null);
        Assert.That(presentation.CustomProperties, Is.Null);
        Assert.That(presentation.Properties, Is.Not.Null);
        Assert.That(presentation.ViewPropertiesPart, Is.Not.Null);
        Assert.That(presentation.TableStylesPart, Is.Not.Null);
        Assert.That(presentation.SlideCount(), Is.EqualTo(1));
        Assert.That(presentation.SlideMasterCount(), Is.EqualTo(1));
        Assert.That(presentation.SlideLayoutCount(), Is.EqualTo(1));
        Assert.That(presentation.ThemeCount(), Is.EqualTo(1));
        Assert.That(presentation.Theme, Is.Not.Null);
        Assert.That(presentation.Slides[0].PartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(presentation.Slides[0].Layout, Is.Not.Null);
        Assert.That(presentation.Slides[0].Layout!.PartName, Is.EqualTo(PptxPartNames.FirstSlideLayout));
        Assert.That(presentation.SlideLayouts[0].Master, Is.Not.Null);
        Assert.That(presentation.SlideLayouts[0].Master!.PartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
        Assert.That(presentation.SlideMasters[0].Layouts.Count, Is.EqualTo(1));
        Assert.That(presentation.SlideMasters[0].Themes.Count, Is.EqualTo(1));
    }

    [Test]
    public void LoadPresentation_LoadsNotesAndHandoutMasters()
    {
        PptxPackage package = CreatePackageWithNotesAndHandoutMasters();
        PptxDocument document = Reopen(new PptxDocument(package));

        PptxPresentation presentation = document.LoadPresentation();

        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        Assert.That(presentation.NotesMaster, Is.Not.Null);
        Assert.That(presentation.HandoutMaster, Is.Not.Null);
        Assert.That(presentation.NotesMaster!.PartName, Is.EqualTo(PptxPartNames.FirstNotesMaster));
        Assert.That(presentation.HandoutMaster!.PartName, Is.EqualTo(PptxPartNames.FirstHandoutMaster));
        Assert.That(presentation.NotesMaster.Themes.Count, Is.EqualTo(1));
        Assert.That(presentation.HandoutMaster.Themes.Count, Is.EqualTo(1));
        Assert.That(presentation.ThemeCount(), Is.EqualTo(1));
    }

    [Test]
    public void LoadPresentation_ReportsMissingRootPresentationRelationship()
    {
        PptxPackage package = PptxPackage.Create();

        PptxPresentation presentation = PptxPresentation.Load(package);

        Assert.That(presentation.Failed(), Is.True);
        Assert.That(presentation.Diagnostics.FormatDiagnostics(), Does.Contain("PPTM010"));
    }

    [Test]
    public void LoadPresentation_ReportsMissingRelatedPart()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.RemovePart(PptxPartNames.FirstSlideLayout);

        PptxPresentation presentation = PptxPresentation.Load(package);

        Assert.That(presentation.Failed(), Is.True);
        Assert.That(presentation.Diagnostics.FormatDiagnostics(), Does.Contain("PPTM032"));
        Assert.That(presentation.Diagnostics.FormatDiagnostics(), Does.Contain(PptxPartNames.FirstSlideLayout));
    }

    [Test]
    public void StaticLoad_AcceptsDocumentAndPackage()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();

        PptxPresentation fromDocument = PptxPresentation.Load(document);
        PptxPresentation fromPackage = PptxPresentation.Load(document.GetPackage());

        Assert.That(fromDocument.SlideCount(), Is.EqualTo(1));
        Assert.That(fromPackage.SlideCount(), Is.EqualTo(1));
    }

    static PptxDocument Reopen(PptxDocument document)
    {
        byte[] bytes = document.SaveBytes();
        return PptxDocument.OpenBytes(bytes);
    }

    static PptxPackage CreatePackageWithNotesAndHandoutMasters()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart(
            PptxPartNames.FirstNotesMaster,
            PptxContentTypes.NotesMaster,
            Encoding.UTF8.GetBytes("<p:notesMaster xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>"));
        package.AddPart(
            PptxPartNames.FirstHandoutMaster,
            PptxContentTypes.HandoutMaster,
            Encoding.UTF8.GetBytes("<p:handoutMaster xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>"));

        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.NotesMaster, "notesMasters/notesMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.HandoutMaster, "handoutMasters/handoutMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstNotesMaster, PptxRelationshipTypes.Theme, "../theme/theme1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstHandoutMaster, PptxRelationshipTypes.Theme, "../theme/theme1.xml", PptxRelationshipTargetMode.Internal);
        return package;
    }
}
