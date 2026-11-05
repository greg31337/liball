using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxWebSettingsTests
{
    [Test]
    public void OpenPackage_DiscoversWebSettingsPartFromMainDocumentRelationship()
    {
        global::docx.DocxPackage package = CreatePackageWithWebSettings();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(package.FindWebSettingsRelationship()!.Id, Is.EqualTo("rIdWebSettings"));
        Assert.That(package.GetWebSettingsPartName(), Is.EqualTo("/word/webSettings.xml"));
        Assert.That(package.GetWebSettingsPart(), Is.Not.Null);
        Assert.That(package.HasWebSettingsPart(), Is.True);
        Assert.That(document.HasWebSettingsPart(), Is.True);
        Assert.That(document.GetWebSettingsPart(), Is.SameAs(package.GetWebSettingsPart()));
        Assert.That(document.GetWebSettingsPart()!.GetText(), Is.EqualTo(CreateWebSettingsXml()));
    }

    [Test]
    public void OpenPackage_IgnoresExternalWebSettingsRelationship()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdWebSettings", global::docx.DocxConstants.RelationshipTypeWebSettings, "https://example.com/webSettings.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(package.FindWebSettingsRelationship(), Is.Null);
        Assert.That(package.GetWebSettingsPartName(), Is.EqualTo(""));
        Assert.That(package.GetWebSettingsPart(), Is.Null);
        Assert.That(package.HasWebSettingsPart(), Is.False);
        Assert.That(document.ParseWebSettings(), Is.False);
        Assert.That(document.GetWebSettingsPart(), Is.Null);
        Assert.That(document.HasWebSettingsPart(), Is.False);
    }

    [Test]
    public void SaveAndReopen_PreservesDiscoveredWebSettingsPart()
    {
        global::docx.DocxPackage package = CreatePackageWithWebSettings();
        MemoryStream stream = new MemoryStream();

        package.Save(stream);
        stream.Position = 0;

        global::docx.DocxPackage roundTrippedPackage = global::docx.DocxPackage.Open(stream);
        global::docx.DocxDocument roundTrippedDocument = global::docx.DocxDocument.OpenPackage(roundTrippedPackage);

        Assert.That(roundTrippedPackage.GetWebSettingsPart()!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeWebSettings));
        Assert.That(roundTrippedPackage.GetWebSettingsPart()!.GetText(), Is.EqualTo(CreateWebSettingsXml()));
        Assert.That(roundTrippedDocument.GetWebSettingsPart()!.GetText(), Is.EqualTo(CreateWebSettingsXml()));
        Assert.That(roundTrippedPackage.FindRelationshipSet("/word/document.xml")!.FindById("rIdWebSettings")!.Target, Is.EqualTo("webSettings.xml"));
    }

    static global::docx.DocxPackage CreatePackageWithWebSettings()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdWebSettings", global::docx.DocxConstants.RelationshipTypeWebSettings, "webSettings.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdExternalWebSettings", global::docx.DocxConstants.RelationshipTypeWebSettings, "https://example.com/webSettings.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/webSettings.xml", global::docx.DocxConstants.ContentTypeWebSettings, CreateWebSettingsXml());

        return package;
    }

    static string CreateWebSettingsXml()
    {
        return "<w:webSettings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:optimizeForBrowser /><w:allowPNG /></w:webSettings>";
    }
}
