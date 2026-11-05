using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxCustomXmlPartsTests
{
    [Test]
    public void Discover_FindsCustomXmlPartsFromPackageAndDocumentRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithCustomXmlParts();

        global::docx.DocxCustomXmlParts customXmlParts = global::docx.DocxCustomXmlParts.Discover(package);
        global::docx.DocxCustomXmlPart? rootPart = customXmlParts.FindByRelationshipId("/", "rCustomRoot");
        global::docx.DocxCustomXmlPart? documentPart = customXmlParts.FindByRelationshipId("/word/document.xml", "rCustomDocument");

        Assert.That(customXmlParts.Count(), Is.EqualTo(2));
        Assert.That(rootPart, Is.Not.Null);
        Assert.That(rootPart!.SourcePartName, Is.EqualTo("/"));
        Assert.That(rootPart.PartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(rootPart.RelationshipId, Is.EqualTo("rCustomRoot"));
        Assert.That(rootPart.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(rootPart.GetXml(), Is.EqualTo(CreateCustomXml("Root")));
        Assert.That(rootPart.HasXml(), Is.True);
        Assert.That(documentPart, Is.Not.Null);
        Assert.That(documentPart!.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(documentPart.PartName, Is.EqualTo("/customXml/item2.xml"));
        Assert.That(documentPart.GetXml(), Is.EqualTo(CreateCustomXml("Document")));
        Assert.That(customXmlParts.FindByPartName("customXml/item1.xml"), Is.SameAs(rootPart));
        Assert.That(customXmlParts.FindByPartName("\\customXml\\item2.xml"), Is.SameAs(documentPart));
    }

    [Test]
    public void OpenPackage_ExposesDiscoveredCustomXmlParts()
    {
        global::docx.DocxPackage package = CreatePackageWithCustomXmlParts();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.CustomXmlPartCount(), Is.EqualTo(2));
        Assert.That(document.GetCustomXmlParts().Count(), Is.EqualTo(2));
        Assert.That(document.GetCustomXmlPart(0).GetXml(), Does.Contain("<value>"));
        Assert.That(document.GetCustomXmlParts().FindByRelationshipId("/", "rCustomRoot"), Is.Not.Null);
    }

    [Test]
    public void Discover_IgnoresExternalAndMissingCustomXmlRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithCustomXmlParts();
        global::docx.DocxCustomXmlParts customXmlParts = global::docx.DocxCustomXmlParts.Discover(package);

        Assert.That(customXmlParts.FindByRelationshipId("/", "rExternalCustom"), Is.Null);
        Assert.That(customXmlParts.FindByRelationshipId("/word/document.xml", "rMissingCustom"), Is.Null);
        Assert.That(customXmlParts.FindByPartName("/customXml/missing.xml"), Is.Null);
    }

    [Test]
    public void CustomXmlPartLookup_NormalizesSourceAndPartNames()
    {
        global::docx.DocxCustomXmlParts customXmlParts = new global::docx.DocxCustomXmlParts();
        global::docx.DocxCustomXmlPart part = new global::docx.DocxCustomXmlPart("word\\document.xml", "customXml\\item3.xml", "rCustom3", global::docx.DocxConstants.ContentTypeXml, "<root />");

        customXmlParts.Parts.Add(part);

        Assert.That(part.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(part.PartName, Is.EqualTo("/customXml/item3.xml"));
        Assert.That(customXmlParts.FindByRelationshipId("\\word\\document.xml", "rCustom3"), Is.SameAs(part));
        Assert.That(customXmlParts.FindByPartName("customXml/item3.xml"), Is.SameAs(part));
    }

    [Test]
    public void SaveAndReopen_PreservesDiscoveredCustomXmlPartsAndRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithCustomXmlParts();
        MemoryStream stream = new MemoryStream();

        package.Save(stream);
        stream.Position = 0;

        global::docx.DocxPackage roundTrippedPackage = global::docx.DocxPackage.Open(stream);
        global::docx.DocxDocument roundTrippedDocument = global::docx.DocxDocument.OpenPackage(roundTrippedPackage);

        Assert.That(roundTrippedDocument.CustomXmlPartCount(), Is.EqualTo(2));
        Assert.That(roundTrippedPackage.FindPart("/customXml/item1.xml")!.GetText(), Is.EqualTo(CreateCustomXml("Root")));
        Assert.That(roundTrippedPackage.FindPart("/customXml/item2.xml")!.GetText(), Is.EqualTo(CreateCustomXml("Document")));
        Assert.That(roundTrippedPackage.GetPackageRelationshipSet()!.FindById("rCustomRoot")!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeCustomXml));
        Assert.That(roundTrippedPackage.FindRelationshipSet("/word/document.xml")!.FindById("rCustomDocument")!.Target, Is.EqualTo("../customXml/item2.xml"));
    }

    [Test]
    public void PublicManagement_InsertsReplacesRelatesAndRemovesCustomXmlParts()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxCustomXmlManagement management = new global::docx.DocxCustomXmlManagement();
        string originalXml = "<customer xmlns:vendor=\"urn:vendor\"><name>Alice</name><vendor:extension code=\"7\" /></customer>";

        global::docx.DocxCustomXmlPart part = management.AddCustomXmlPart(document, "/word/document.xml", originalXml, "{11111111-1111-1111-1111-111111111111}");
        global::docx.DocxCustomXmlPartInfo info = management.GetCustomXmlPartInfo(document, part);

        Assert.That(part.PartName, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(part.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(management.GetCustomXmlContent(part), Is.EqualTo(originalXml));
        Assert.That(info.ItemPropertiesPartName, Is.EqualTo("/customXml/itemProps1.xml"));
        Assert.That(info.ItemId, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(document.Package.ResolveRelationshipTarget(part.SourcePartName, part.RelationshipId), Is.EqualTo(part.PartName));
        Assert.That(management.FindCustomXmlPartByName(document, "customXml/item1.xml"), Is.SameAs(part));
        Assert.That(management.FindCustomXmlPartByRelationship(document, "/word/document.xml", part.RelationshipId), Is.SameAs(part));

        string replacementXml = "<customer xmlns:vendor=\"urn:vendor\"><name>Bob</name><vendor:extension code=\"8\"><vendor:unknown /></vendor:extension></customer>";
        management.ReplaceCustomXmlContent(document, part, replacementXml);
        global::docx.DocxRelationship externalRelationship = management.AddCustomXmlRelationship(document, part, "urn:example:metadata", "https://example.com/customer", true);

        Assert.That(management.GetCustomXmlContent(part), Is.EqualTo(replacementXml));
        Assert.That(document.Package.FindPart(part.PartName)!.GetText(), Is.EqualTo(replacementXml));
        Assert.That(management.GetCustomXmlRelationships(document, part), Has.Count.EqualTo(2));
        Assert.That(management.RemoveCustomXmlRelationship(document, part, externalRelationship.Id), Is.True);

        string itemPropertiesPartName = info.ItemPropertiesPartName;
        string customXmlPartName = part.PartName;
        Assert.That(management.RemoveCustomXmlPart(document, part), Is.True);
        Assert.That(document.CustomXmlPartCount(), Is.EqualTo(0));
        Assert.That(document.Package.FindPart(customXmlPartName), Is.Null);
        Assert.That(document.Package.FindPart(itemPropertiesPartName), Is.Null);
        Assert.That(document.Package.GetRelationships("/word/document.xml").Any(value => value.Id == part.RelationshipId), Is.False);
    }

    [Test]
    public void PublicManagement_RejectsCustomXmlPartsOwnedByAnotherDocument()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.Create();
        global::docx.DocxDocument source = global::docx.DocxDocument.Create();
        global::docx.DocxCustomXmlManagement management = new global::docx.DocxCustomXmlManagement();
        string targetXml = "<target><value>Keep</value></target>";
        string sourceXml = "<source><value>Foreign</value></source>";
        global::docx.DocxCustomXmlPart targetPart = management.AddCustomXmlPart(target, "/word/document.xml", targetXml, "");
        global::docx.DocxCustomXmlPart sourcePart = management.AddCustomXmlPart(source, "/word/document.xml", sourceXml, "");

        Assert.That(sourcePart.PartName, Is.EqualTo(targetPart.PartName));
        Assert.Throws<ArgumentException>(() => management.GetCustomXmlPartInfo(target, sourcePart));
        Assert.Throws<ArgumentException>(() => management.ReplaceCustomXmlContent(target, sourcePart, "<replacement />"));
        Assert.Throws<ArgumentException>(() => management.RemoveCustomXmlPart(target, sourcePart));
        Assert.That(management.GetCustomXmlContent(targetPart), Is.EqualTo(targetXml));
        Assert.That(target.Package.FindPart(targetPart.PartName)!.GetText(), Is.EqualTo(targetXml));
        Assert.That(management.GetCustomXmlContent(sourcePart), Is.EqualTo(sourceXml));
        Assert.That(target.CustomXmlPartCount(), Is.EqualTo(1));
    }

    [Test]
    public void PublicManagement_UpdatesItemPropertiesAndPreservesUnsupportedXml()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxCustomXmlManagement management = new global::docx.DocxCustomXmlManagement();
        global::docx.DocxCustomXmlPart part = management.AddCustomXmlPart(document, "/", "<root><value>One</value></root>", "{22222222-2222-2222-2222-222222222222}");
        string itemPropertiesXml = "<ds:datastoreItem xmlns:ds=\"http://schemas.openxmlformats.org/officeDocument/2006/customXml\" xmlns:vendor=\"urn:vendor\" ds:itemID=\"{22222222-2222-2222-2222-222222222222}\" vendor:flag=\"keep\"><ds:schemaRefs><ds:schemaRef ds:uri=\"urn:old\" /><vendor:inside value=\"keep\" /></ds:schemaRefs><vendor:extension value=\"preserve\" /></ds:datastoreItem>";

        management.ReplaceCustomXmlItemPropertiesXml(document, part, itemPropertiesXml);
        management.SetCustomXmlItemProperties(document, part, "{33333333-3333-3333-3333-333333333333}", new List<string> { "urn:first", "urn:second" });
        global::docx.DocxCustomXmlPartInfo info = management.GetCustomXmlPartInfo(document, part);

        Assert.That(info.ItemId, Is.EqualTo("{33333333-3333-3333-3333-333333333333}"));
        Assert.That(info.SchemaReferences, Is.EqualTo(new[] { "urn:first", "urn:second" }));
        Assert.That(info.ItemPropertiesXml, Does.Contain("vendor:flag=\"keep\""));
        Assert.That(info.ItemPropertiesXml, Does.Contain("vendor:inside value=\"keep\""));
        Assert.That(info.ItemPropertiesXml, Does.Contain("vendor:extension value=\"preserve\""));
        Assert.That(info.Relationships.Any(value => value.Type == global::docx.DocxConstants.RelationshipTypeCustomXmlProperties), Is.True);
        Assert.That(management.FindCustomXmlPartByItemId(document, "{33333333-3333-3333-3333-333333333333}"), Is.SameAs(part));

        MemoryStream stream = new MemoryStream();
        document.Package.Save(stream);
        stream.Position = 0;
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(global::docx.DocxPackage.Open(stream));
        global::docx.DocxCustomXmlPart reopenedPart = management.FindCustomXmlPartByItemId(reopened, "{33333333-3333-3333-3333-333333333333}")!;
        global::docx.DocxCustomXmlPartInfo reopenedInfo = management.GetCustomXmlPartInfo(reopened, reopenedPart);

        Assert.That(reopenedInfo.ItemPropertiesXml, Does.Contain("vendor:extension value=\"preserve\""));
        Assert.That(reopenedInfo.SchemaReferences, Is.EqualTo(new[] { "urn:first", "urn:second" }));
        Assert.That(management.RemoveCustomXmlItemProperties(reopened, reopenedPart), Is.True);
        Assert.That(management.GetCustomXmlPartInfo(reopened, reopenedPart).ItemPropertiesPartName, Is.EqualTo(""));
    }

    [Test]
    public void PublicManagement_LocatesCustomXmlPartsForBoundContentControls()
    {
        string itemId = "{44444444-4444-4444-4444-444444444444}";
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        string documentXml = "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:sdt><w:sdtPr><w:dataBinding w:storeItemID=\"" + itemId + "\" w:xpath=\"/root/value\" /></w:sdtPr><w:sdtContent><w:p><w:r><w:t>Bound</w:t></w:r></w:p></w:sdtContent></w:sdt></w:body></w:document>";
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, documentXml);
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxCustomXmlManagement management = new global::docx.DocxCustomXmlManagement();
        global::docx.DocxCustomXmlPart part = management.AddCustomXmlPart(document, "/word/document.xml", "<root><value>Bound</value></root>", itemId);
        global::docx.DocxContentControl contentControl = document.GetContentControls()[0];

        Assert.That(management.FindCustomXmlPartForContentControl(document, contentControl), Is.SameAs(part));
        Assert.That(management.GetBoundContentControls(document, part), Is.EqualTo(new[] { contentControl }));
        Assert.That(management.FindCustomXmlPartByItemId(document, itemId.ToLowerInvariant()), Is.SameAs(part));
    }

    [Test]
    public void CustomXmlParts_RejectInvalidArguments()
    {
        global::docx.DocxCustomXmlParts customXmlParts = new global::docx.DocxCustomXmlParts();
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/");
        global::docx.DocxRelationship relationship = new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", "");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomXmlParts.Discover(null!));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.DiscoverParts(null!));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.DiscoverPartsFromRelationshipSet(null!, relationshipSet));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.DiscoverPartsFromRelationshipSet(package, null!));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.AddPartFromRelationship(null!, relationshipSet, relationship));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.AddPartFromRelationship(package, null!, relationship));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.AddPartFromRelationship(package, relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.FindByPartName(null!));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.FindByRelationshipId(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => customXmlParts.FindByRelationshipId("/", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCustomXmlParts.NormalizeSourcePartName(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomXmlPart(null!, "/customXml/item1.xml", "rId1", global::docx.DocxConstants.ContentTypeXml, "<root />"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomXmlPart("/", null!, "rId1", global::docx.DocxConstants.ContentTypeXml, "<root />"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomXmlPart("/", "/customXml/item1.xml", null!, global::docx.DocxConstants.ContentTypeXml, "<root />"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomXmlPart("/", "/customXml/item1.xml", "rId1", null!, "<root />"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCustomXmlPart("/", "/customXml/item1.xml", "rId1", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.That(customXmlParts.AddPartFromRelationship(package, relationshipSet, relationship), Is.False);
        Assert.That(global::docx.DocxCustomXmlParts.NormalizeSourcePartName("/"), Is.EqualTo("/"));
    }

    static global::docx.DocxPackage CreatePackageWithCustomXmlParts()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet rootRelationships = package.GetPackageRelationshipSet()!;
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        rootRelationships.CreateRelationshipWithId("rCustomRoot", global::docx.DocxConstants.RelationshipTypeCustomXml, "customXml/item1.xml", false);
        rootRelationships.CreateRelationshipWithId("rExternalCustom", global::docx.DocxConstants.RelationshipTypeCustomXml, "https://example.com/item.xml", true);
        documentRelationships.CreateRelationshipWithId("rCustomDocument", global::docx.DocxConstants.RelationshipTypeCustomXml, "../customXml/item2.xml", false);
        documentRelationships.CreateRelationshipWithId("rMissingCustom", global::docx.DocxConstants.RelationshipTypeCustomXml, "../customXml/missing.xml", false);
        package.ReplaceXmlPart("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent("/customXml/item1.xml", global::docx.DocxConstants.ContentTypeXml, CreateCustomXml("Root"));
        package.AddXmlPartWithContent("/customXml/item2.xml", global::docx.DocxConstants.ContentTypeXml, CreateCustomXml("Document"));

        return package;
    }

    static string CreateCustomXml(string value)
    {
        return "<root><value>" + value + "</value></root>";
    }
}
