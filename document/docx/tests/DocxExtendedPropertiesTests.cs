using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxExtendedPropertiesTests
{
    [Test]
    public void ExtendedPropertiesParsing_ReadsKnownFieldsVectorsAndUnknownChildren()
    {
        global::docx.DocxExtendedProperties properties = ParseExtendedProperties("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns:x=\"urn:x\"><Application>Word</Application><AppVersion>16.0000</AppVersion><Company>Acme</Company><Manager>Alice</Manager><Pages>3</Pages><Words>50</Words><Characters>200</Characters><CharactersWithSpaces>240</CharactersWithSpaces><DocSecurity>0</DocSecurity><ScaleCrop>false</ScaleCrop><LinksUpToDate>true</LinksUpToDate><SharedDoc>false</SharedDoc><HeadingPairs><vt:vector size=\"2\" baseType=\"variant\"><vt:variant><vt:lpstr>Title</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs><TitlesOfParts><vt:vector size=\"1\" baseType=\"lpstr\"><vt:lpstr>Part</vt:lpstr></vt:vector></TitlesOfParts><x:custom x:data=\"1\" /></Properties>");

        Assert.That(properties.Application, Is.EqualTo("Word"));
        Assert.That(properties.AppVersion, Is.EqualTo("16.0000"));
        Assert.That(properties.Company, Is.EqualTo("Acme"));
        Assert.That(properties.Manager, Is.EqualTo("Alice"));
        Assert.That(properties.Pages, Is.EqualTo("3"));
        Assert.That(properties.Words, Is.EqualTo("50"));
        Assert.That(properties.Characters, Is.EqualTo("200"));
        Assert.That(properties.CharactersWithSpaces, Is.EqualTo("240"));
        Assert.That(properties.DocSecurity, Is.EqualTo("0"));
        Assert.That(properties.ScaleCrop, Is.EqualTo("false"));
        Assert.That(properties.LinksUpToDate, Is.EqualTo("true"));
        Assert.That(properties.SharedDoc, Is.EqualTo("false"));
        Assert.That(properties.HeadingPairsXml, Does.Contain("vt:vector"));
        Assert.That(properties.TitlesOfPartsXml, Does.Contain("Part"));
        Assert.That(properties.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(properties.GetUnknownChildXml(0), Does.Contain("x:data=\"1\""));
    }

    [Test]
    public void ExtendedPropertiesWriting_WritesKnownFieldsVectorsAndUnknownChildren()
    {
        global::docx.DocxExtendedProperties properties = global::docx.DocxExtendedProperties.Create();

        properties.SetApplication("docx <lib>");
        properties.SetAppVersion("1.0");
        properties.SetCompany("Acme");
        properties.SetManager("Alice");
        properties.SetPages("4");
        properties.SetWords("80");
        properties.SetCharacters("300");
        properties.SetCharactersWithSpaces("350");
        properties.SetDocSecurity("0");
        properties.SetScaleCrop("false");
        properties.SetLinksUpToDate("true");
        properties.SetSharedDoc("false");
        properties.SetHeadingPairsXml("<HeadingPairs xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><vt:vector size=\"2\" baseType=\"variant\"><vt:variant><vt:lpstr>Title</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs>");
        properties.SetTitlesOfPartsXml("<TitlesOfParts xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><vt:vector size=\"1\" baseType=\"lpstr\"><vt:lpstr>Part</vt:lpstr></vt:vector></TitlesOfParts>");
        properties.AddUnknownChild(new global::docx.DocxXmlBuffer("<x:custom xmlns:x=\"urn:x\" />"));

        string xml = properties.WriteXml();
        global::docx.DocxExtendedProperties parsed = ParseExtendedProperties(xml);

        Assert.That(xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\""));
        Assert.That(xml, Does.Contain("xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\""));
        Assert.That(xml, Does.Contain("docx &lt;lib&gt;"));
        Assert.That(parsed.Application, Is.EqualTo("docx <lib>"));
        Assert.That(parsed.HeadingPairsXml, Does.Contain("vt:i4"));
        Assert.That(parsed.TitlesOfPartsXml, Does.Contain("Part"));
        Assert.That(parsed.UnknownChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void DocumentExtendedProperties_ParseCreateWriteAndReopen()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = package.GetPackageRelationshipSet()!;
        relationshipSet.CreateRelationship(global::docx.DocxConstants.RelationshipTypeExtendedProperties, "docProps/app.xml", false);
        package.ReplaceXmlPart("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());
        package.AddXmlPartWithContent("/docProps/app.xml", global::docx.DocxConstants.ContentTypeExtendedProperties, "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>Existing</Application></Properties>");

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HasExtendedProperties(), Is.True);
        Assert.That(document.GetExtendedProperties().Application, Is.EqualTo("Existing"));
        Assert.That(package.GetExtendedPropertiesPartName(), Is.EqualTo("/docProps/app.xml"));
        Assert.That(package.HasExtendedPropertiesPart(), Is.True);

        document.GetExtendedProperties().SetApplication("Updated");
        document.WriteExtendedPropertiesPart();

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(reopened.GetExtendedProperties().Application, Is.EqualTo("Updated"));

        global::docx.DocxDocument created = global::docx.DocxDocument.Create();
        global::docx.DocxExtendedProperties app = created.GetOrCreateExtendedProperties();

        app.SetCompany("Company");
        created.WriteExtendedPropertiesPart();

        Assert.That(created.HasExtendedProperties(), Is.True);
        Assert.That(created.Package.GetExtendedPropertiesPartName(), Is.EqualTo("/docProps/app.xml"));
        Assert.That(created.Package.GetExtendedPropertiesPart()!.GetText(), Does.Contain("Company"));
        Assert.That(created.Package.FindExtendedPropertiesRelationship(), Is.Not.Null);
    }

    [Test]
    public void ExtendedProperties_RejectInvalidArguments()
    {
        global::docx.DocxExtendedProperties properties = global::docx.DocxExtendedProperties.Create();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxExtendedProperties(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxExtendedProperties.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => properties.ReadKnownChild(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetKnownValue(null!, ""));
        Assert.Throws<ArgumentNullException>(() => properties.SetKnownValue("Application", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.CreateExtendedPropertyXml(null!, ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.CreateExtendedPropertyXml("Application", null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetApplication(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetAppVersion(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCompany(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetManager(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetPages(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetWords(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCharacters(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetCharactersWithSpaces(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetPresentationFormat(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetLines(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetParagraphs(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetSlides(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetNotes(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetTotalTime(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetHiddenSlides(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetMMClips(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetDocSecurity(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetScaleCrop(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetLinksUpToDate(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetSharedDoc(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetHyperlinksChanged(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetHyperlinkBase(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetHeadingPairsXml(null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetTitlesOfPartsXml(null!));
        Assert.Throws<ArgumentNullException>(() => properties.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => properties.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => properties.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.WriteTextElement(null!, "Application", "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.WriteTextElement(writer, null!, "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.WriteTextElement(writer, "Application", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.WriteRawChild(null!, ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxExtendedProperties.WriteRawChild(writer, null!));
        Assert.Throws<InvalidOperationException>(() => document.WriteExtendedPropertiesPart());

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxExtendedProperties.Parse(reader));
    }

    static global::docx.DocxExtendedProperties ParseExtendedProperties(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("Properties", global::docx.DocxConstants.NamespaceExtendedProperties), Is.True);

        return global::docx.DocxExtendedProperties.Parse(reader);
    }
}
