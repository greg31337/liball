using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SectionCustomShowOperationsTests
{
    [Test]
    public void PublicOperations_LookupRenameAndOrderedAssignmentRoundTrip()
    {
        PptxPresentation presentation = LoadPresentationWithThreeSlides();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSectionOperations sections = new PptxSectionOperations();
        PptxCustomShowOperations shows = new PptxCustomShowOperations();
        int firstId = presentation.Slides[0].SlideId;
        int secondId = presentation.Slides[1].SlideId;
        int thirdId = presentation.Slides[2].SlideId;

        PptxSection section = presentations.AddSection(presentation, "Opening");
        PptxCustomShow show = presentations.AddCustomShow(presentation, "Presenter");
        sections.Rename(section, "Overview");
        shows.Rename(show, "Main Show");
        sections.SetSlideIds(section, new List<int> { thirdId, firstId });
        sections.InsertSlideId(section, 1, secondId);
        sections.RemoveSlideIdAt(section, 2);
        shows.SetSlideIds(show, new List<int> { secondId, firstId });
        shows.InsertSlideId(show, 1, thirdId);

        Assert.That(presentations.SectionCount(presentation), Is.EqualTo(1));
        Assert.That(presentations.CustomShowCount(presentation), Is.EqualTo(1));
        Assert.That(presentations.GetSection(presentation, 0), Is.SameAs(section));
        Assert.That(presentations.GetCustomShow(presentation, 0), Is.SameAs(show));
        Assert.That(presentations.FindSectionByName(presentation, "Overview"), Is.SameAs(section));
        Assert.That(presentations.FindCustomShowByName(presentation, "Main Show"), Is.SameAs(show));
        Assert.That(sections.SlideCount(section), Is.EqualTo(2));
        Assert.That(sections.GetSlideId(section, 0), Is.EqualTo(thirdId));
        Assert.That(sections.GetSlideId(section, 1), Is.EqualTo(secondId));
        Assert.That(shows.SlideCount(show), Is.EqualTo(3));
        Assert.That(shows.GetSlideId(show, 0), Is.EqualTo(secondId));
        Assert.That(shows.GetSlideId(show, 1), Is.EqualTo(thirdId));
        Assert.That(shows.GetSlideId(show, 2), Is.EqualTo(firstId));
        string presentationXml = Encoding.UTF8.GetString(presentation.Package.GetPartBytesForSave(presentation.Part!));
        Assert.That(presentationXml, Does.Contain("r:id=\"" + presentation.Slides[1].RelationshipId + "\""));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSection reopenedSection = presentations.FindSectionByName(reopened, "Overview")!;
        PptxCustomShow reopenedShow = presentations.FindCustomShowByName(reopened, "Main Show")!;
        Assert.That(reopenedSection.SlideIds, Is.EqualTo(new[] { thirdId, secondId }));
        Assert.That(reopenedShow.SlideIds, Is.EqualTo(new[] { secondId, thirdId, firstId }));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());

        Assert.That(sections.Remove(reopenedSection), Is.True);
        presentations.RemoveCustomShowAt(reopened, 0);
        PptxPresentation removed = SaveReopenAndLoad(reopened);
        Assert.That(presentations.SectionCount(removed), Is.Zero);
        Assert.That(presentations.CustomShowCount(removed), Is.Zero);
    }

    [Test]
    public void AttachedOperations_RejectUnknownSlideIdsWithoutChangingOrder()
    {
        PptxPresentation presentation = LoadPresentationWithThreeSlides();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSectionOperations sections = new PptxSectionOperations();
        PptxCustomShowOperations shows = new PptxCustomShowOperations();
        int firstId = presentation.Slides[0].SlideId;
        int secondId = presentation.Slides[1].SlideId;
        PptxSection section = presentations.AddSection(presentation, "Valid");
        PptxCustomShow show = presentations.AddCustomShow(presentation, "Valid");
        sections.SetSlideIds(section, new List<int> { firstId, secondId });
        shows.SetSlideIds(show, new List<int> { secondId, firstId });

        Assert.That(() => presentations.AddSlideToSection(presentation, 0, 999999), Throws.ArgumentException);
        Assert.That(() => presentations.AddSlideToCustomShow(presentation, 0, 999999), Throws.ArgumentException);
        Assert.That(() => sections.SetSlideIds(section, new List<int> { secondId, 999999 }), Throws.ArgumentException);
        Assert.That(() => shows.InsertSlideId(show, 1, 999999), Throws.ArgumentException);

        Assert.That(section.SlideIds, Is.EqualTo(new[] { firstId, secondId }));
        Assert.That(show.SlideIds, Is.EqualTo(new[] { secondId, firstId }));
    }

    [Test]
    public void RemovingSlide_RemovesAllSectionAndCustomShowReferences()
    {
        PptxPresentation presentation = LoadPresentationWithThreeSlides();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSectionOperations sections = new PptxSectionOperations();
        PptxCustomShowOperations shows = new PptxCustomShowOperations();
        int firstId = presentation.Slides[0].SlideId;
        int removedId = presentation.Slides[1].SlideId;
        int thirdId = presentation.Slides[2].SlideId;
        PptxSection section = presentations.AddSection(presentation, "All");
        PptxCustomShow show = presentations.AddCustomShow(presentation, "All");
        sections.SetSlideIds(section, new List<int> { firstId, removedId, thirdId, removedId });
        shows.SetSlideIds(show, new List<int> { removedId, thirdId, removedId, firstId });

        presentations.RemoveSlideAt(presentation, 1);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.Sections[0].SlideIds, Is.EqualTo(new[] { firstId, thirdId }));
        Assert.That(reopened.CustomShows[0].SlideIds, Is.EqualTo(new[] { thirdId, firstId }));
        Assert.That(reopened.Sections[0].SlideIds, Does.Not.Contain(removedId));
        Assert.That(reopened.CustomShows[0].SlideIds, Does.Not.Contain(removedId));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void LoadPresentation_ReportsBrokenSectionAndCustomShowReferences()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxPart part = package.GetPart(PptxPartNames.Presentation);
        XmlDocument document = PptxXml.LoadBytes(part.GetBytes());
        XmlElement root = document.DocumentElement!;
        XmlElement sectionList = PptxXml.AppendChildElement(root, "p", "sectionLst", PptxNamespaces.Presentation);
        XmlElement section = PptxXml.AppendChildElement(sectionList, "p", "section", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(section, "", "", "name", "Broken Section");
        PptxXml.SetAttributeValue(section, "", "", "id", "1");
        XmlElement sectionSlides = PptxXml.AppendChildElement(section, "p", "sldIdLst", PptxNamespaces.Presentation);
        XmlElement sectionSlide = PptxXml.AppendChildElement(sectionSlides, "p", "sld", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(sectionSlide, "", "", "id", "999999");
        XmlElement showList = PptxXml.AppendChildElement(root, "p", "custShowLst", PptxNamespaces.Presentation);
        XmlElement show = PptxXml.AppendChildElement(showList, "p", "custShow", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(show, "", "", "name", "Broken Show");
        PptxXml.SetAttributeValue(show, "", "", "id", "1");
        XmlElement showSlides = PptxXml.AppendChildElement(show, "p", "sldLst", PptxNamespaces.Presentation);
        XmlElement showSlide = PptxXml.AppendChildElement(showSlides, "p", "sld", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(showSlide, "r", PptxNamespaces.Relationships, "id", "rId999");
        part.SetXmlDocument(document);

        PptxPresentation presentation = PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();

        Assert.That(presentation.Failed(), Is.True);
        Assert.That(presentation.Diagnostics.FormatDiagnostics(), Does.Contain("PPTM037"));
        Assert.That(presentation.Diagnostics.FormatDiagnostics(), Does.Contain("PPTM038"));
    }

    static PptxPresentation LoadPresentationWithThreeSlides()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
        PptxPresentationOperations operations = new PptxPresentationOperations();
        operations.AddSlide(presentation);
        operations.AddSlide(presentation);
        return presentation;
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
