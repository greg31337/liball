using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxCustomPropertiesTests
{
    [Test]
    public void CustomPropertiesParsing_ReadsTypedPropertiesAndUnknownChildren()
    {
        global::docx.DocxCustomProperties properties = ParseCustomProperties("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns:x=\"urn:x\"><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"Project\"><vt:lpwstr>Apollo</vt:lpwstr></property><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"3\" name=\"Count\"><vt:i4>7</vt:i4></property><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"4\" name=\"Flag\" linkTarget=\"Bookmark\"><vt:bool>true</vt:bool></property><x:custom x:data=\"1\" /></Properties>");

        Assert.That(properties.PropertyCount(), Is.EqualTo(3));
        Assert.That(properties.FindPropertyByName("Project")!.Value, Is.EqualTo("Apollo"));
        Assert.That(properties.FindPropertyByName("Count")!.Type, Is.EqualTo("i4"));
        Assert.That(properties.FindPropertyByName("Flag")!.LinkTarget, Is.EqualTo("Bookmark"));
        Assert.That(properties.FindPropertyByName("Flag")!.IsType("bool"), Is.True);
        Assert.That(properties.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(properties.GetUnknownChildXml(0), Does.Contain("x:data=\"1\""));
    }

    [Test]
    public void CustomPropertiesWriting_AddsUpdatesAndWritesTypedProperties()
    {
        global::docx.DocxCustomProperties properties = global::docx.DocxCustomProperties.Create();

        global::docx.DocxCustomProperty project = properties.AddStringProperty("Project", "Apollo");
        global::docx.DocxCustomProperty count = properties.AddIntProperty("Count", "7");
        properties.AddBoolProperty("Flag", "true");
        properties.SetProperty("Project", "lpwstr", "Zeus");
        properties.AddUnknownChild(new global::docx.DocxXmlBuffer("<x:custom xmlns:x=\"urn:x\" />"));

        string xml = properties.WriteXml();
        global::docx.DocxCustomProperties parsed = ParseCustomProperties(xml);

        Assert.That(project.PropertyId, Is.EqualTo("2"));
        Assert.That(count.PropertyId, Is.EqualTo("3"));
        Assert.That(properties.PropertyCount(), Is.EqualTo(3));
        Assert.That(xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\""));
        Assert.That(xml, Does.Contain("xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\""));
        Assert.That(parsed.FindPropertyByName("Project")!.Value, Is.EqualTo("Zeus"));
        Assert.That(parsed.FindPropertyByName("Count")!.Type, Is.EqualTo("i4"));
        Assert.That(parsed.FindPropertyByName("Flag")!.Value, Is.EqualTo("true"));
        Assert.That(parsed.UnknownChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void CustomPropertiesWriting_AllocatesFirstAvailablePropertyIdAndWritesLinkTarget()
    {
        global::docx.DocxCustomProperties properties = ParseCustomProperties("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"First\"><vt:lpwstr>A</vt:lpwstr></property><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"4\" name=\"Third\"><vt:i4>3</vt:i4></property></Properties>");

        global::docx.DocxCustomProperty inserted = properties.AddBoolProperty("Second", "false");

        inserted.SetLinkTarget("BookmarkName");

        global::docx.DocxCustomProperties parsed = ParseCustomProperties(properties.WriteXml());

        Assert.That(inserted.PropertyId, Is.EqualTo("3"));
        Assert.That(parsed.PropertyCount(), Is.EqualTo(3));
        Assert.That(parsed.FindPropertyByName("Second")!.PropertyId, Is.EqualTo("3"));
        Assert.That(parsed.FindPropertyByName("Second")!.Type, Is.EqualTo("bool"));
        Assert.That(parsed.FindPropertyByName("Second")!.Value, Is.EqualTo("false"));
        Assert.That(parsed.FindPropertyByName("Second")!.LinkTarget, Is.EqualTo("BookmarkName"));
    }

    [Test]
    public void DocumentCustomProperties_ParseCreateWriteAndReopen()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = package.GetPackageRelationshipSet()!;
        relationshipSet.CreateRelationship(global::docx.DocxConstants.RelationshipTypeCustomProperties, "docProps/custom.xml", false);
        package.ReplaceXmlPart("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());
        package.AddXmlPartWithContent("/docProps/custom.xml", global::docx.DocxConstants.ContentTypeCustomProperties, "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"Existing\"><vt:lpwstr>Value</vt:lpwstr></property></Properties>");

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HasCustomProperties(), Is.True);
        Assert.That(document.GetCustomProperties().FindPropertyByName("Existing")!.Value, Is.EqualTo("Value"));
        Assert.That(package.GetCustomPropertiesPartName(), Is.EqualTo("/docProps/custom.xml"));
        Assert.That(package.HasCustomPropertiesPart(), Is.True);

        document.GetCustomProperties().SetProperty("Existing", "lpwstr", "Updated");
        document.WriteCustomPropertiesPart();

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(reopened.GetCustomProperties().FindPropertyByName("Existing")!.Value, Is.EqualTo("Updated"));

        global::docx.DocxDocument created = global::docx.DocxDocument.Create();
        global::docx.DocxCustomProperties custom = created.GetOrCreateCustomProperties();

        custom.AddStringProperty("Created", "Yes");
        created.WriteCustomPropertiesPart();

        Assert.That(created.HasCustomProperties(), Is.True);
        Assert.That(created.Package.GetCustomPropertiesPartName(), Is.EqualTo("/docProps/custom.xml"));
        Assert.That(created.Package.GetCustomPropertiesPart()!.GetText(), Does.Contain("Created"));
        Assert.That(created.Package.FindCustomPropertiesRelationship(), Is.Not.Null);
    }

    [Test]
    public void CustomProperties_RejectInvalidArguments()
    {
        global::docx.DocxCustomProperties properties = global::docx.DocxCustomProperties.Create();
        global::docx.DocxCustomProperty property = global::docx.DocxCustomProperty.Create("Name", "2", "lpwstr", "Value");
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomProperties(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperties.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperties.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCustomProperties.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperties.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => properties.FindPropertyByName(null!));
        Assert.Throws<ArgumentException>(() => properties.FindPropertyByName(""));
        Assert.Throws<ArgumentNullException>(() => properties.AddProperty(null!, "lpwstr", "Value"));
        Assert.Throws<ArgumentNullException>(() => properties.AddProperty("Name", null!, "Value"));
        Assert.Throws<ArgumentNullException>(() => properties.AddProperty("Name", "lpwstr", null!));
        Assert.Throws<ArgumentNullException>(() => properties.SetProperty(null!, "lpwstr", "Value"));
        Assert.Throws<ArgumentNullException>(() => properties.SetProperty("Name", null!, "Value"));
        Assert.Throws<ArgumentNullException>(() => properties.SetProperty("Name", "lpwstr", null!));
        Assert.Throws<ArgumentNullException>(() => properties.AddStringProperty(null!, "Value"));
        Assert.Throws<ArgumentNullException>(() => properties.AddIntProperty("Count", null!));
        Assert.Throws<ArgumentNullException>(() => properties.AddBoolProperty("Flag", null!));
        Assert.Throws<ArgumentNullException>(() => properties.ContainsPropertyId(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperties.ValidatePropertyName(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxCustomProperties.ValidatePropertyName(""));
        Assert.Throws<ArgumentNullException>(() => properties.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => properties.WriteTo(null!));

        properties.AddStringProperty("Name", "Value");
        Assert.Throws<InvalidOperationException>(() => properties.AddStringProperty("Name", "Again"));

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomProperty(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.Create(null!, "2", "lpwstr", "Value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.Create("Name", null!, "lpwstr", "Value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.Create("Name", "2", null!, "Value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.Create("Name", "2", "lpwstr", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCustomProperty.ParseXml("<root />"));
        Assert.Throws<System.IO.InvalidDataException>(() => global::docx.DocxCustomProperty.ParseXml("<property xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\" pid=\"2\" name=\"MissingFmt\"><vt:lpwstr xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\">Value</vt:lpwstr></property>"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.ReadRequiredAttribute(null!, "name"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomProperty.ReadRequiredAttribute(global::docx.DocxXmlReader.Create("<root />"), null!));
        Assert.Throws<ArgumentNullException>(() => property.SetName(null!));
        Assert.Throws<ArgumentException>(() => property.SetName(""));
        Assert.Throws<ArgumentNullException>(() => property.SetPropertyId(null!));
        Assert.Throws<ArgumentException>(() => property.SetPropertyId(""));
        Assert.Throws<ArgumentNullException>(() => property.SetFormatId(null!));
        Assert.Throws<ArgumentException>(() => property.SetFormatId(""));
        Assert.Throws<ArgumentNullException>(() => property.SetLinkTarget(null!));
        Assert.Throws<ArgumentNullException>(() => property.SetType(null!));
        Assert.Throws<ArgumentException>(() => property.SetType(""));
        Assert.Throws<ArgumentNullException>(() => property.SetValue(null!));
        Assert.Throws<ArgumentNullException>(() => property.IsType(null!));
        Assert.Throws<ArgumentException>(() => property.IsType(""));
        Assert.Throws<ArgumentNullException>(() => property.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => property.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => property.WriteValueTo(null!));
        Assert.Throws<InvalidOperationException>(() => document.WriteCustomPropertiesPart());

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCustomProperties.Parse(reader));
    }

    static global::docx.DocxCustomProperties ParseCustomProperties(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("Properties", global::docx.DocxConstants.NamespaceCustomProperties), Is.True);

        return global::docx.DocxCustomProperties.Parse(reader);
    }
}
