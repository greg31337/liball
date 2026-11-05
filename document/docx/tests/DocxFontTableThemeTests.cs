using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxFontTableThemeTests
{
    [Test]
    public void OpenPackage_DiscoversFontTableAndThemePartsFromMainDocumentRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithFontTableAndTheme();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(package.FindFontTableRelationship()!.Id, Is.EqualTo("rIdFonts"));
        Assert.That(package.GetFontTablePartName(), Is.EqualTo("/word/fontTable.xml"));
        Assert.That(package.HasFontTablePart(), Is.True);
        Assert.That(package.FindThemeRelationship()!.Id, Is.EqualTo("rIdTheme"));
        Assert.That(package.GetThemePartName(), Is.EqualTo("/word/theme/theme1.xml"));
        Assert.That(package.HasThemePart(), Is.True);
        Assert.That(document.HasFontTablePart(), Is.True);
        Assert.That(document.GetFontTablePart(), Is.SameAs(package.GetFontTablePart()));
        Assert.That(document.GetFontTablePart()!.GetText(), Is.EqualTo(CreateFontTableXml()));
        Assert.That(document.HasThemePart(), Is.True);
        Assert.That(document.GetThemePart(), Is.SameAs(package.GetThemePart()));
        Assert.That(document.GetThemePart()!.GetText(), Is.EqualTo(CreateThemeXml()));
    }

    [Test]
    public void OpenPackage_IgnoresExternalFontTableAndThemeRelationships()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdFonts", global::docx.DocxConstants.RelationshipTypeFontTable, "https://example.com/fontTable.xml", true);
        documentRelationships.CreateRelationshipWithId("rIdTheme", global::docx.DocxConstants.RelationshipTypeTheme, "https://example.com/theme.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(package.FindFontTableRelationship(), Is.Null);
        Assert.That(package.GetFontTablePartName(), Is.EqualTo(""));
        Assert.That(package.GetFontTablePart(), Is.Null);
        Assert.That(package.HasFontTablePart(), Is.False);
        Assert.That(package.FindThemeRelationship(), Is.Null);
        Assert.That(package.GetThemePartName(), Is.EqualTo(""));
        Assert.That(package.GetThemePart(), Is.Null);
        Assert.That(package.HasThemePart(), Is.False);
        Assert.That(document.ParseFontTable(), Is.False);
        Assert.That(document.GetFontTablePart(), Is.Null);
        Assert.That(document.HasFontTablePart(), Is.False);
        Assert.That(document.ParseTheme(), Is.False);
        Assert.That(document.GetThemePart(), Is.Null);
        Assert.That(document.HasThemePart(), Is.False);
    }

    [Test]
    public void SaveAndReopen_PreservesDiscoveredFontTableAndThemeParts()
    {
        global::docx.DocxPackage package = CreatePackageWithFontTableAndTheme();
        MemoryStream stream = new MemoryStream();

        package.Save(stream);
        stream.Position = 0;

        global::docx.DocxPackage roundTrippedPackage = global::docx.DocxPackage.Open(stream);
        global::docx.DocxDocument roundTrippedDocument = global::docx.DocxDocument.OpenPackage(roundTrippedPackage);

        Assert.That(roundTrippedPackage.GetFontTablePart()!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeFontTable));
        Assert.That(roundTrippedPackage.GetFontTablePart()!.GetText(), Is.EqualTo(CreateFontTableXml()));
        Assert.That(roundTrippedPackage.GetThemePart()!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeTheme));
        Assert.That(roundTrippedPackage.GetThemePart()!.GetText(), Is.EqualTo(CreateThemeXml()));
        Assert.That(roundTrippedDocument.GetFontTablePart()!.GetText(), Is.EqualTo(CreateFontTableXml()));
        Assert.That(roundTrippedDocument.GetThemePart()!.GetText(), Is.EqualTo(CreateThemeXml()));
        Assert.That(roundTrippedPackage.FindRelationshipSet("/word/document.xml")!.FindById("rIdFonts")!.Target, Is.EqualTo("fontTable.xml"));
        Assert.That(roundTrippedPackage.FindRelationshipSet("/word/document.xml")!.FindById("rIdTheme")!.Target, Is.EqualTo("theme/theme1.xml"));
    }

    static global::docx.DocxPackage CreatePackageWithFontTableAndTheme()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdFonts", global::docx.DocxConstants.RelationshipTypeFontTable, "fontTable.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdTheme", global::docx.DocxConstants.RelationshipTypeTheme, "theme/theme1.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdExternalTheme", global::docx.DocxConstants.RelationshipTypeTheme, "https://example.com/theme.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/fontTable.xml", global::docx.DocxConstants.ContentTypeFontTable, CreateFontTableXml());
        package.AddXmlPartWithContent("/word/theme/theme1.xml", global::docx.DocxConstants.ContentTypeTheme, CreateThemeXml());

        return package;
    }

    static string CreateFontTableXml()
    {
        return "<w:fonts xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:font w:name=\"Aptos\"><w:family w:val=\"swiss\" /></w:font><w:font w:name=\"Courier New\" /></w:fonts>";
    }

    static string CreateThemeXml()
    {
        return "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Office\"><a:themeElements><a:clrScheme name=\"Office\" /></a:themeElements></a:theme>";
    }
}
