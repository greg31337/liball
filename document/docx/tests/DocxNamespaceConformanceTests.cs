using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxNamespaceConformanceTests
{
    [Test]
    public void DetectNamespaceConformance_ReturnsTransitionalForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxNamespaceConformance conformance = validator.DetectNamespaceConformance(package);

        Assert.That(conformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Transitional));
    }

    [Test]
    public void DetectNamespaceConformance_ReturnsStrictForStrictNamespacesAndRelationships()
    {
        global::docx.DocxPackage package = CreateStrictPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxNamespaceConformance conformance = validator.DetectNamespaceConformance(package);

        Assert.That(conformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Strict));
    }

    [Test]
    public void DetectNamespaceConformance_ReturnsMixedWhenStrictAndTransitionalArePresent()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.AddXmlPartWithContent("/customXml/strict.xml", global::docx.DocxConstants.ContentTypeXml, CreateStrictDocumentXml());

        global::docx.DocxNamespaceConformance conformance = validator.DetectNamespaceConformance(package);

        Assert.That(conformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Mixed));
    }

    [Test]
    public void DetectNamespaceConformance_ReturnsUnknownForNoRecognizedNamespaces()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.Parts.Add(new global::docx.DocxPart("/custom/data.xml", global::docx.DocxConstants.ContentTypeXml, Encoding.UTF8.GetBytes("<root xmlns=\"urn:custom\" />"), false));

        global::docx.DocxNamespaceConformance conformance = validator.DetectNamespaceConformance(package);

        Assert.That(conformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Unknown));
    }

    [Test]
    public void DetectNamespaceConformance_ReadsNamespacesAndRelationshipTypeValuesFromXml()
    {
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        string xml = "<w:document xmlns:w=\"http://purl.oclc.org/ooxml/wordprocessingml/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:body /></w:document>";
        string relationshipsXml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://purl.oclc.org/ooxml/officeDocument/relationships/officeDocument\" Target=\"word/document.xml\" /></Relationships>";

        Assert.That(validator.DetectXmlNamespaceConformance(xml), Is.EqualTo(global::docx.DocxNamespaceConformance.Mixed));
        Assert.That(validator.DetectXmlNamespaceConformance(relationshipsXml), Is.EqualTo(global::docx.DocxNamespaceConformance.Strict));
    }

    [Test]
    public void DocxValidator_DelegatesNamespaceConformanceDetection()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        global::docx.DocxNamespaceConformance conformance = validator.DetectNamespaceConformance(package);

        Assert.That(conformance, Is.EqualTo(global::docx.DocxNamespaceConformance.Transitional));
    }

    [Test]
    public void NamespaceConformanceDetection_RejectsNullArguments()
    {
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxValidator facade = new global::docx.DocxValidator();
        global::docx.DocxPart part = new global::docx.DocxPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, Encoding.UTF8.GetBytes(CreateTransitionalDocumentXml()), false);
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/");

        Assert.Throws<ArgumentNullException>(() => validator.DetectNamespaceConformance((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => validator.DetectPartNamespaceConformance((global::docx.DocxPart)null!));
        Assert.Throws<ArgumentNullException>(() => validator.DetectXmlNamespaceConformance((string)null!));
        Assert.Throws<ArgumentNullException>(() => validator.DetectRelationshipSetNamespaceConformance(null!));
        Assert.Throws<ArgumentNullException>(() => validator.DetectNamespaceValue(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsTransitionalNamespace(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsStrictNamespace(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsTransitionalRelationshipType(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsStrictRelationshipType(null!));
        Assert.Throws<ArgumentNullException>(() => facade.DetectNamespaceConformance(null!));
        Assert.That(validator.DetectPartNamespaceConformance(part), Is.EqualTo(global::docx.DocxNamespaceConformance.Transitional));
        Assert.That(validator.DetectRelationshipSetNamespaceConformance(relationshipSet), Is.EqualTo(global::docx.DocxNamespaceConformance.Unknown));
    }

    static global::docx.DocxPackage CreateStrictPackage()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/");

        relationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeStrictPrefix + "officeDocument", "word/document.xml", false);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateStrictDocumentXml());
        package.AddXmlPartWithContent("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, relationships.WriteXml());

        return package;
    }

    static string CreateStrictDocumentXml()
    {
        return "<w:document xmlns:w=\"http://purl.oclc.org/ooxml/wordprocessingml/main\"><w:body /></w:document>";
    }

    static string CreateTransitionalDocumentXml()
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body /></w:document>";
    }
}
