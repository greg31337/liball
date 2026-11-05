using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class DocumentPropertiesTests
{
    [Test]
    public void CoreProperties_ParseAndWriteRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxCoreProperties core = presentation.GetCoreProperties();

        Assert.That(core.Creator, Is.EqualTo("pptx"));
        Assert.That(core.LastModifiedBy, Is.EqualTo("pptx"));

        core.Title = "Quarterly Review";
        core.Subject = "Planning";
        core.Creator = "Docs Team";
        core.Description = "A generated deck";
        core.Keywords = "quarterly;planning";
        core.Created = "2026-08-04T00:00:00Z";
        core.Modified = "2026-08-04T01:00:00Z";
        core.Save();

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxCoreProperties reopenedCore = reopened.GetCoreProperties();

        Assert.That(reopenedCore.Title, Is.EqualTo("Quarterly Review"));
        Assert.That(reopenedCore.Subject, Is.EqualTo("Planning"));
        Assert.That(reopenedCore.Creator, Is.EqualTo("Docs Team"));
        Assert.That(reopenedCore.Description, Is.EqualTo("A generated deck"));
        Assert.That(reopenedCore.Keywords, Is.EqualTo("quarterly;planning"));
        Assert.That(reopenedCore.Created, Is.EqualTo("2026-08-04T00:00:00Z"));
        Assert.That(reopenedCore.Modified, Is.EqualTo("2026-08-04T01:00:00Z"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AppProperties_ParseAndWriteRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxAppProperties app = presentation.GetAppProperties();

        Assert.That(app.Application, Is.EqualTo("pptx"));
        Assert.That(app.Slides, Is.EqualTo(1));

        app.Application = "pptx tests";
        app.PresentationFormat = "Custom";
        app.Slides = 4;
        app.Notes = 2;
        app.HiddenSlides = 1;
        app.Save();

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxAppProperties reopenedApp = reopened.GetAppProperties();

        Assert.That(reopenedApp.Application, Is.EqualTo("pptx tests"));
        Assert.That(reopenedApp.PresentationFormat, Is.EqualTo("Custom"));
        Assert.That(reopenedApp.Slides, Is.EqualTo(4));
        Assert.That(reopenedApp.Notes, Is.EqualTo(2));
        Assert.That(reopenedApp.HiddenSlides, Is.EqualTo(1));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CustomProperties_CreateWriteAndReadRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxCustomProperties custom = presentation.GetCustomProperties();

        custom.SetString("Department", "Finance");
        custom.SetInt("Version", 3);
        custom.SetBool("Approved", true);
        custom.Save();

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxCustomProperties reopenedCustom = reopened.GetCustomProperties();

        Assert.That(reopened.Package.HasPart(PptxPartNames.CustomProperties), Is.True);
        Assert.That(reopened.Package.ContentTypes.GetOverrideContentType(PptxPartNames.CustomProperties), Is.EqualTo(PptxContentTypes.CustomProperties));
        Assert.That(reopened.Package.RootRelationships.Relationships.Exists(relationship => relationship.Type == PptxRelationshipTypes.CustomProperties), Is.True);
        Assert.That(reopenedCustom.Count(), Is.EqualTo(3));
        Assert.That(reopenedCustom.GetValue("Department"), Is.EqualTo("Finance"));
        Assert.That(reopenedCustom.GetValue("Version"), Is.EqualTo("3"));
        Assert.That(reopenedCustom.GetValue("Approved"), Is.EqualTo("true"));
        Assert.That(reopenedCustom.Find("Version")!.ValueType, Is.EqualTo("i4"));
        Assert.That(reopenedCustom.Find("Approved")!.ValueType, Is.EqualTo("bool"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CustomProperties_UpdateAndRemoveValues()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxCustomProperties custom = presentation.GetCustomProperties();
        custom.SetString("Status", "Draft");
        custom.SetString("Owner", "A");
        custom.SetString("Status", "Final");

        bool removed = custom.Remove("Owner");
        custom.Save();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(removed, Is.True);
        Assert.That(reopened.GetCustomProperties().Count(), Is.EqualTo(1));
        Assert.That(reopened.GetCustomProperties().GetValue("Status"), Is.EqualTo("Final"));
        Assert.That(reopened.GetCustomProperties().GetValue("Owner"), Is.EqualTo(""));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }
}
