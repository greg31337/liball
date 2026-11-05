using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PresentationSettingsApiTests
{
    [Test]
    public void Settings_EditEverySupportedCategoryAndRoundTrip()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationSettings settings = operations.GetSettings(presentation);

        settings.View.LastView = "slideView";
        settings.View.ShowComments = true;
        settings.View.GridSpacingX = 72008L;
        settings.View.GridSpacingY = 72008L;
        settings.View.ShowGuides = true;
        settings.View.SnapToGrid = true;
        settings.View.SnapToObjects = true;
        PptxPresentationGuide horizontal = new PptxPresentationGuide();
        horizontal.Orientation = "horz";
        horizontal.Position = 100;
        settings.View.Guides.Add(horizontal);
        PptxPresentationGuide vertical = new PptxPresentationGuide();
        vertical.Orientation = "vert";
        vertical.Position = 200;
        settings.View.Guides.Add(vertical);
        settings.DefaultTextStyle.Xml =
            "<p:defaultTextStyle xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:a=\"" + PptxNamespaces.Drawing + "\">" +
            "<a:defPPr algn=\"ctr\"/><a:lvl1pPr marL=\"342900\"/>" +
            "</p:defaultTextStyle>";
        settings.FirstSlideNumber = 5;
        settings.ShowSlideNumbers = false;
        settings.ShowSpecialPlaceholdersOnTitleSlides = false;
        settings.Orientation = "portrait";
        settings.Loop = true;
        settings.UseTimings = false;
        settings.SlideTimings[0].AdvanceOnClick = false;
        settings.SlideTimings[0].AdvanceAfter = true;
        settings.SlideTimings[0].AdvanceAfterMilliseconds = 2500L;

        operations.SetSettings(presentation, settings);
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        PptxPresentationSettings actual = operations.GetSettings(reopened);

        Assert.That(actual.View.LastView, Is.EqualTo("slideView"));
        Assert.That(actual.View.ShowComments, Is.True);
        Assert.That(actual.View.GridSpacingX, Is.EqualTo(72008L));
        Assert.That(actual.View.GridSpacingY, Is.EqualTo(72008L));
        Assert.That(actual.View.ShowGuides, Is.True);
        Assert.That(actual.View.SnapToGrid, Is.True);
        Assert.That(actual.View.SnapToObjects, Is.True);
        Assert.That(actual.View.Guides.Count, Is.EqualTo(2));
        Assert.That(actual.View.Guides[0].Orientation, Is.EqualTo("horz"));
        Assert.That(actual.View.Guides[0].Position, Is.EqualTo(100));
        Assert.That(actual.View.Guides[1].Orientation, Is.EqualTo("vert"));
        Assert.That(actual.View.Guides[1].Position, Is.EqualTo(200));
        Assert.That(actual.DefaultTextStyle.Xml, Does.Contain("a:defPPr"));
        Assert.That(actual.DefaultTextStyle.Xml, Does.Contain("a:lvl1pPr"));
        Assert.That(actual.FirstSlideNumber, Is.EqualTo(5));
        Assert.That(actual.ShowSlideNumbers, Is.False);
        Assert.That(actual.ShowSpecialPlaceholdersOnTitleSlides, Is.False);
        Assert.That(actual.Orientation, Is.EqualTo("portrait"));
        Assert.That(reopened.SlideWidth, Is.LessThan(reopened.SlideHeight));
        Assert.That(actual.Loop, Is.True);
        Assert.That(actual.UseTimings, Is.False);
        Assert.That(actual.SlideTimings.Count, Is.EqualTo(1));
        Assert.That(actual.SlideTimings[0].SlideId, Is.EqualTo(reopened.GetSlide(0).SlideId));
        Assert.That(actual.SlideTimings[0].AdvanceOnClick, Is.False);
        Assert.That(actual.SlideTimings[0].AdvanceAfter, Is.True);
        Assert.That(actual.SlideTimings[0].AdvanceAfterMilliseconds, Is.EqualTo(2500L));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Settings_PreserveUnrelatedViewAndShowMarkup()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPackage package = document.GetPackage();
        package.ReplacePart(PptxPartNames.ViewProperties, PptxContentTypes.ViewProperties, Encoding.UTF8.GetBytes(CreateViewPropertiesXml()));
        package.ReplacePart(PptxPartNames.PresentationProperties, PptxContentTypes.PresentationProperties, Encoding.UTF8.GetBytes(CreatePresentationPropertiesXml()));
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxPresentationSettings settings = operations.GetSettings(presentation);
        settings.View.GridSpacingX = 1000L;
        settings.View.GridSpacingY = 2000L;
        settings.Loop = true;
        settings.UseTimings = false;

        operations.SetSettings(presentation, settings);
        PptxDocument reopened = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPackage reopenedPackage = reopened.GetPackage();
        string viewXml = Encoding.UTF8.GetString(reopenedPackage.GetPart(PptxPartNames.ViewProperties).Bytes);
        string propertiesXml = Encoding.UTF8.GetString(reopenedPackage.GetPart(PptxPartNames.PresentationProperties).Bytes);

        Assert.That(viewXml, Does.Contain("preserve-view"));
        Assert.That(viewXml, Does.Contain("p:gridSpacing"));
        Assert.That(propertiesXml, Does.Contain("showAnimation=\"0\""));
        Assert.That(propertiesXml, Does.Contain("p:present"));
        Assert.That(propertiesXml, Does.Contain("preserve-show"));
        Assert.That(operations.GetSettings(reopened.LoadPresentation()).Loop, Is.True);
    }

    [Test]
    public void Settings_CreateMissingViewAndPresentationPropertiesParts()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPackage package = document.GetPackage();
        package.RemovePart(PptxPartNames.ViewProperties);
        package.RemovePart(PptxPartNames.PresentationProperties);
        PptxDocument withoutSettingsParts = PptxDocument.OpenBytes(document.SaveBytes());
        PptxPresentation presentation = withoutSettingsParts.LoadPresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxPresentationSettings settings = operations.GetSettings(presentation);
        settings.View.LastView = "slideView";
        settings.Loop = true;

        operations.SetSettings(presentation, settings);
        PptxDocument reopened = PptxDocument.OpenBytes(withoutSettingsParts.SaveBytes());
        PptxPresentationSettings actual = operations.GetSettings(reopened.LoadPresentation());

        Assert.That(reopened.GetPackage().HasPart(PptxPartNames.ViewProperties), Is.True);
        Assert.That(reopened.GetPackage().HasPart(PptxPartNames.PresentationProperties), Is.True);
        Assert.That(actual.View.LastView, Is.EqualTo("slideView"));
        Assert.That(actual.Loop, Is.True);
        Assert.That(reopened.Validate().Succeeded(), Is.True, reopened.Validate().FormatDiagnostics());
    }

    [Test]
    public void SetSettings_RejectsInvalidValuesBeforeChangingPresentation()
    {
        PptxDocument document = PptxDocument.CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        PptxPackageOperations packageOperations = new PptxPackageOperations();
        PptxPackage before = packageOperations.ClonePackage(document.GetPackage());
        PptxPresentationSettings settings = operations.GetSettings(presentation);
        settings.DefaultTextStyle.Xml = "<a:defPPr xmlns:a=\"" + PptxNamespaces.Drawing + "\"/>";

        Assert.Throws<System.ArgumentException>(() => operations.SetSettings(presentation, settings));
        Assert.That(packageOperations.CompareManifests(before, document.GetPackage()).Equal, Is.True);

        settings = operations.GetSettings(presentation);
        settings.View.GridSpacingX = -1L;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetSettings(presentation, settings));
    }

    static string CreateViewPropertiesXml()
    {
        return
            "<p:viewPr xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"urn:test\" lastView=\"normalView\">" +
            "<p:normalViewPr/><p:extLst><p:ext uri=\"preserve-view\"><x:data/></p:ext></p:extLst>" +
            "</p:viewPr>";
    }

    static string CreatePresentationPropertiesXml()
    {
        return
            "<p:presentationPr xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"urn:test\">" +
            "<p:showPr showAnimation=\"0\"><p:present/><p:extLst><p:ext uri=\"preserve-show\"><x:data/></p:ext></p:extLst></p:showPr>" +
            "</p:presentationPr>";
    }
}
