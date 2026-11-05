using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class CompatibilityRoundTripTests
{
    [TestCase(PptxFixtureDecks.PowerPoint, "Microsoft PowerPoint")]
    [TestCase(PptxFixtureDecks.LibreOffice, "LibreOffice")]
    [TestCase(PptxFixtureDecks.GoogleSlides, "Google")]
    [TestCase(PptxFixtureDecks.Keynote, "Keynote")]
    [TestCase(PptxFixtureDecks.LibraryGenerated, "pptx")]
    public void FixtureFamily_OpenSaveReopenPreservesLoadableValidPresentation(string family, string application)
    {
        PptxFixtureDeck deck = PptxFixtureDecks.Create(family);
        PptxDocument openedDocument = PptxDocument.OpenBytes(deck.Bytes);
        PptxPresentation openedPresentation = openedDocument.LoadPresentation();

        byte[] savedBytes = openedDocument.SaveBytes();
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(savedBytes);
        PptxPresentation reopenedPresentation = reopenedDocument.LoadPresentation();

        AssertPresentationSucceeded(openedDocument.Package, openedPresentation);
        AssertPresentationSucceeded(reopenedDocument.Package, reopenedPresentation);
        Assert.That(reopenedPresentation.SlideCount(), Is.EqualTo(openedPresentation.SlideCount()));
        Assert.That(reopenedPresentation.GetAppProperties().Application, Is.EqualTo(application));
        Assert.That(reopenedPresentation.GetCoreProperties().Category, Is.EqualTo("compatibility-fixture"));
        Assert.That(reopenedPresentation.GetCustomProperties().GetValue("FixtureFamily"), Is.EqualTo(family));
        Assert.That(reopenedPresentation.GetCustomProperties().GetValue("FixtureSource"), Is.EqualTo(deck.Source));
        Assert.That(reopenedPresentation.ExtractText(), Is.EqualTo(openedPresentation.ExtractText()));
        Assert.That(reopenedPresentation.ExtractText(), Does.Contain("fixture-marker:" + family));
    }

    [TestCase(PptxFixtureDecks.PowerPoint)]
    [TestCase(PptxFixtureDecks.LibreOffice)]
    [TestCase(PptxFixtureDecks.GoogleSlides)]
    [TestCase(PptxFixtureDecks.Keynote)]
    [TestCase(PptxFixtureDecks.LibraryGenerated)]
    public void FixtureFamily_OpenSaveReopenPreservesPackageFootprint(string family)
    {
        PptxFixtureDeck deck = PptxFixtureDecks.Create(family);
        PptxPackage opened = PptxPackage.OpenBytes(deck.Bytes);

        PptxPackage reopened = PptxPackage.OpenBytes(opened.SaveBytes());

        Assert.That(reopened.Parts.Count, Is.EqualTo(opened.Parts.Count));
        Assert.That(reopened.RootRelationships.Count(), Is.EqualTo(opened.RootRelationships.Count()));
        Assert.That(reopened.ContentTypes.DefaultCount(), Is.EqualTo(opened.ContentTypes.DefaultCount()));
        Assert.That(reopened.ContentTypes.OverrideCount(), Is.EqualTo(opened.ContentTypes.OverrideCount()));
        Assert.That(reopened.HasPart(PptxPartNames.CoreProperties), Is.True);
        Assert.That(reopened.HasPart(PptxPartNames.ExtendedProperties), Is.True);
        Assert.That(reopened.HasPart(PptxPartNames.CustomProperties), Is.True);
        Assert.That(reopened.HasPart(PptxPartNames.FirstSlide), Is.True);
        Assert.That(reopened.Validate().Succeeded(), Is.True, reopened.Validate().FormatDiagnostics());
    }

    [TestCase(PptxFixtureDecks.PowerPoint, "val=\"powerpoint\"")]
    [TestCase(PptxFixtureDecks.LibreOffice, "val=\"libreoffice\"")]
    [TestCase(PptxFixtureDecks.GoogleSlides, "val=\"google-slides\"")]
    [TestCase(PptxFixtureDecks.Keynote, "val=\"keynote\"")]
    public void ExportedFixtureFamily_OpenSaveReopenPreservesUnknownExtensionMarkup(string family, string marker)
    {
        PptxFixtureDeck deck = PptxFixtureDecks.Create(family);
        PptxDocument openedDocument = PptxDocument.OpenBytes(deck.Bytes);

        PptxPackage reopened = PptxPackage.OpenBytes(openedDocument.SaveBytes());
        string slideXml = Encoding.UTF8.GetString(reopened.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(slideXml, Does.Contain("mc:Ignorable="));
        Assert.That(slideXml, Does.Contain(marker));
    }

    static void AssertPresentationSucceeded(PptxPackage package, PptxPresentation presentation)
    {
        Assert.That(package.Succeeded(), Is.True, package.Diagnostics.FormatDiagnostics());
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
    }
}
