using System.Collections.Generic;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class FixtureDeckTests
{
    [Test]
    public void FixtureDecks_CreateAllRequestedFamilies()
    {
        List<PptxFixtureDeck> decks = PptxFixtureDecks.CreateAll();

        Assert.That(decks.Count, Is.EqualTo(5));
        AssertFixture(decks[0], PptxFixtureDecks.PowerPoint, "Microsoft PowerPoint");
        AssertFixture(decks[1], PptxFixtureDecks.LibreOffice, "LibreOffice");
        AssertFixture(decks[2], PptxFixtureDecks.GoogleSlides, "Google");
        AssertFixture(decks[3], PptxFixtureDecks.Keynote, "Keynote");
        AssertFixture(decks[4], PptxFixtureDecks.LibraryGenerated, "pptx");
    }

    [TestCase(PptxFixtureDecks.PowerPoint, "Microsoft PowerPoint")]
    [TestCase(PptxFixtureDecks.LibreOffice, "LibreOffice")]
    [TestCase(PptxFixtureDecks.GoogleSlides, "Google")]
    [TestCase(PptxFixtureDecks.Keynote, "Keynote")]
    [TestCase(PptxFixtureDecks.LibraryGenerated, "pptx")]
    public void FixtureDecks_OpenLoadValidateAndExposeFamilyMetadata(string family, string application)
    {
        PptxFixtureDeck deck = PptxFixtureDecks.Create(family);

        PptxPackage package = PptxPackage.OpenBytes(deck.Bytes);
        PptxDocument document = PptxDocument.OpenBytes(deck.Bytes);
        PptxPresentation presentation = document.LoadPresentation();

        Assert.That(package.Succeeded(), Is.True, package.Diagnostics.FormatDiagnostics());
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        Assert.That(presentation.SlideCount(), Is.EqualTo(1));
        Assert.That(presentation.GetAppProperties().Application, Is.EqualTo(application));
        Assert.That(presentation.GetCustomProperties().GetValue("FixtureFamily"), Is.EqualTo(family));
        Assert.That(presentation.ExtractText(), Does.Contain("fixture-marker:" + family));
    }

    static void AssertFixture(PptxFixtureDeck deck, string family, string application)
    {
        Assert.That(deck.Family, Is.EqualTo(family));
        Assert.That(deck.Application, Is.EqualTo(application));
        Assert.That(deck.Bytes.Length, Is.GreaterThan(0));
    }
}
