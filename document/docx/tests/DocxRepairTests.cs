using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRepairTests
{
    [Test]
    public void RepairMissingContentTypes_AddsDefaultsAndMissingPartDeclarations()
    {
        global::docx.DocxPackage package = CreatePackageWithoutContentTypes();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        global::docx.DocxValidationReport repairReport = repair.RepairMissingContentTypes(package);
        global::docx.DocxValidationReport validationReport = new global::docx.DocxPackageValidator().ValidateContentTypes(package);

        Assert.That(repairReport.ContainsCode(global::docx.DocxRepair.RepairedContentTypeDefaultCode), Is.True);
        Assert.That(repairReport.ContainsCode(global::docx.DocxRepair.RepairedPartContentTypeCode), Is.True);
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(global::docx.DocxConstants.ContentTypeRelationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(package.ContentTypes.GetContentType("/word/actual-main.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(package.ContentTypes.GetContentType("/word/styles.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeStyles));
        Assert.That(package.ContentTypes.GetContentType("/word/media/image1.png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(package.ContentTypes.GetContentType("/_rels/.rels"), Is.EqualTo(global::docx.DocxConstants.ContentTypeRelationships));
        Assert.That(package.FindPart("/word/actual-main.xml")!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(validationReport.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.False);
        Assert.That(validationReport.ContainsCode(global::docx.DocxPackageValidator.MissingPartContentTypeCode), Is.False);
        Assert.That(validationReport.ContainsCode(global::docx.DocxPackageValidator.UnexpectedPartContentTypeCode), Is.False);
    }

    [Test]
    public void RepairMissingContentTypes_PreservesExistingDeclarationsAndUsesCustomPartContentType()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.Parts.Add(new global::docx.DocxPart("/custom/data.bin", "application/x-custom", new byte[] { 1 }, false));

        global::docx.DocxValidationReport report = repair.RepairMissingContentTypes(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedContentTypeDefaultCode), Is.False);
        Assert.That(package.ContentTypes.GetContentType("/custom/data.bin"), Is.EqualTo("application/x-custom"));
        Assert.That(package.FindPart("/custom/data.bin")!.ContentType, Is.EqualTo("application/x-custom"));
        Assert.That(new global::docx.DocxPackageValidator().ValidateContentTypes(package).ContainsCode(global::docx.DocxPackageValidator.MissingPartContentTypeCode), Is.False);
    }

    [Test]
    public void RepairMissingContentTypes_InfersKnownDocxPartNames()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.Parts.Add(new global::docx.DocxPart("/word/header1.xml", "", Encoding.UTF8.GetBytes("<hdr />"), false));
        package.Parts.Add(new global::docx.DocxPart("/word/footer2.xml", "", Encoding.UTF8.GetBytes("<ftr />"), false));
        package.Parts.Add(new global::docx.DocxPart("/docProps/core.xml", "", Encoding.UTF8.GetBytes("<core />"), false));
        package.Parts.Add(new global::docx.DocxPart("/customXml/item1.xml", "", Encoding.UTF8.GetBytes("<root />"), false));

        repair.RepairMissingContentTypes(package);

        Assert.That(package.ContentTypes.GetContentType("/word/header1.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeHeader));
        Assert.That(package.ContentTypes.GetContentType("/word/footer2.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeFooter));
        Assert.That(package.ContentTypes.GetContentType("/docProps/core.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeCoreProperties));
        Assert.That(package.ContentTypes.GetContentType("/customXml/item1.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
    }

    [Test]
    public void DocxValidator_DelegatesMissingContentTypeRepair()
    {
        global::docx.DocxPackage package = CreatePackageWithoutContentTypes();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        global::docx.DocxValidationReport report = validator.RepairMissingContentTypes(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedPartContentTypeCode), Is.True);
        Assert.That(package.ContentTypes.GetContentType("/word/actual-main.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
    }

    [Test]
    public void RepairMissingRelationships_AddsPackageRelationshipSetAndPart()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());

        global::docx.DocxValidationReport report = repair.RepairMissingRelationships(package);
        global::docx.DocxRelationshipSet relationshipSet = package.GetPackageRelationshipSet()!;

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipSetCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipPartCode), Is.True);
        Assert.That(relationshipSet, Is.Not.Null);
        Assert.That(ContainsRelationshipTo(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeOfficeDocument, "/word/document.xml"), Is.True);
        Assert.That(package.FindPart("/_rels/.rels"), Is.Not.Null);
        Assert.That(package.FindPart("/_rels/.rels")!.IsRelationshipPart, Is.True);
        Assert.That(ValidatePackageRelationships(package).ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.False);
        Assert.That(validator.ValidateWordprocessingRequiredParts(package).ContainsCode(global::docx.DocxPackageValidator.MissingMainDocumentRelationshipCode), Is.False);
        Assert.That(ValidateRelationshipPartPresence(package).ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipPartCode), Is.False);
    }

    [Test]
    public void RepairMissingRelationships_AddsMissingRelationshipPartForExistingSet()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        RemovePartOnly(package, "/_rels/.rels");

        global::docx.DocxValidationReport before = ValidateRelationshipPartPresence(package);
        global::docx.DocxValidationReport report = repair.RepairMissingRelationships(package);
        global::docx.DocxValidationReport after = ValidateRelationshipPartPresence(package);

        Assert.That(before.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipPartCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipPartCode), Is.True);
        Assert.That(package.FindPart("/_rels/.rels"), Is.Not.Null);
        Assert.That(package.FindPart("/_rels/.rels")!.GetText(), Does.Contain(global::docx.DocxConstants.RelationshipTypeOfficeDocument));
        Assert.That(after.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipPartCode), Is.False);
    }

    [Test]
    public void RepairMissingRelationships_AddsKnownDocumentAndPackageRelationships()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.AddXmlPartWithContent("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles, "<styles />");
        package.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<hdr />");
        package.AddXmlPartWithContent("/docProps/core.xml", global::docx.DocxConstants.ContentTypeCoreProperties, "<core />");

        global::docx.DocxValidationReport report = repair.RepairMissingRelationships(package);
        global::docx.DocxRelationshipSet documentRelationships = package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationshipSet packageRelationships = package.GetPackageRelationshipSet()!;

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipCode), Is.True);
        Assert.That(documentRelationships, Is.Not.Null);
        Assert.That(ContainsRelationshipTo(package, documentRelationships, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml"), Is.True);
        Assert.That(ContainsRelationshipTo(package, documentRelationships, global::docx.DocxConstants.RelationshipTypeHeader, "/word/header1.xml"), Is.True);
        Assert.That(ContainsRelationshipTo(package, packageRelationships, global::docx.DocxConstants.RelationshipTypeCoreProperties, "/docProps/core.xml"), Is.True);
        Assert.That(package.FindPart("/word/_rels/document.xml.rels"), Is.Not.Null);
        Assert.That(ValidateRelationshipPartPresence(package).ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipPartCode), Is.False);
    }

    [Test]
    public void DocxValidator_DelegatesMissingRelationshipRepair()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());

        global::docx.DocxValidationReport report = validator.RepairMissingRelationships(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedRelationshipCode), Is.True);
        Assert.That(package.GetPackageRelationshipSet(), Is.Not.Null);
    }

    [Test]
    public void RepairOrphanedRelationships_RemovesInternalRelationshipsWithMissingTargets()
    {
        global::docx.DocxPackage package = CreatePackageWithOrphanedImageRelationship();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();
        global::docx.DocxRelationshipSet relationshipSet = package.FindRelationshipSet("/word/document.xml")!;

        global::docx.DocxValidationReport before = ValidateRelationshipTargets(package);
        global::docx.DocxValidationReport report = repair.RepairOrphanedRelationships(package);
        global::docx.DocxValidationReport after = ValidateRelationshipTargets(package);

        Assert.That(before.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedOrphanedRelationshipCode), Is.True);
        Assert.That(relationshipSet.FindById("rIdMissing"), Is.Null);
        Assert.That(relationshipSet.FindById("rIdExternal"), Is.Not.Null);
        Assert.That(package.FindPart("/word/_rels/document.xml.rels")!.GetText(), Does.Not.Contain("rIdMissing"));
        Assert.That(after.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode), Is.False);
    }

    [Test]
    public void RepairOrphanedRelationships_PreservesValidInternalAndExternalRelationships()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/word/document.xml");

        package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, CreatePngBytes());
        relationshipSet.CreateRelationshipWithId("rIdImage", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        relationshipSet.CreateRelationshipWithId("rIdExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());

        global::docx.DocxValidationReport report = repair.RepairOrphanedRelationships(package);
        global::docx.DocxRelationshipSet repairedSet = package.FindRelationshipSet("/word/document.xml")!;

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedOrphanedRelationshipCode), Is.False);
        Assert.That(repairedSet.FindById("rIdImage"), Is.Not.Null);
        Assert.That(repairedSet.FindById("rIdExternal"), Is.Not.Null);
    }

    [Test]
    public void RepairOrphanedRelationships_RemovesRelationshipSetWhoseSourcePartIsMissing()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/word/missing.xml");

        package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, CreatePngBytes());
        relationshipSet.CreateRelationshipWithId("rIdImage", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        package.AddXmlPartWithContent("/word/_rels/missing.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());

        global::docx.DocxValidationReport report = repair.RepairOrphanedRelationships(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedOrphanedRelationshipSetCode), Is.True);
        Assert.That(package.FindRelationshipSet("/word/missing.xml"), Is.Null);
        Assert.That(package.FindPart("/word/_rels/missing.xml.rels"), Is.Null);
    }

    [Test]
    public void DocxValidator_DelegatesOrphanedRelationshipRepair()
    {
        global::docx.DocxPackage package = CreatePackageWithOrphanedImageRelationship();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        global::docx.DocxValidationReport report = validator.RepairOrphanedRelationships(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedOrphanedRelationshipCode), Is.True);
        Assert.That(package.FindRelationshipSet("/word/document.xml")!.FindById("rIdMissing"), Is.Null);
    }

    [Test]
    public void RepairEmptyBody_AddsMissingMainDocumentPart()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        RemovePartOnly(package, "/word/document.xml");

        global::docx.DocxValidationReport report = repair.RepairEmptyBody(package);
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedMainDocumentPartCode), Is.True);
        Assert.That(package.FindPart("/word/document.xml"), Is.Not.Null);
        Assert.That(document.Body.BlockCount(), Is.EqualTo(1));
        Assert.That(package.FindPart("/word/document.xml")!.GetText(), Does.Contain("<w:body"));
    }

    [Test]
    public void RepairEmptyBody_AddsMissingBodyElementAndPreservesDocumentAttributes()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" data-id=\"7\"></w:document>");

        global::docx.DocxValidationReport report = repair.RepairEmptyBody(package);
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        string xml = package.FindPart("/word/document.xml")!.GetText();

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedBodyCode), Is.True);
        Assert.That(xml, Does.Contain("data-id=\"7\""));
        Assert.That(xml, Does.Contain("<w:body"));
        Assert.That(xml, Does.Contain("<w:p"));
        Assert.That(document.Body.BlockCount(), Is.EqualTo(1));
    }

    [Test]
    public void RepairEmptyBody_InsertsParagraphBeforeFinalSectionProperties()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:sectPr /></w:body></w:document>");

        global::docx.DocxValidationReport report = repair.RepairEmptyBody(package);
        string xml = package.FindPart("/word/document.xml")!.GetText();
        int paragraphIndex = xml.IndexOf("<w:p");
        int sectionIndex = xml.IndexOf("<w:sectPr");

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedBodyCode), Is.True);
        Assert.That(paragraphIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sectionIndex, Is.GreaterThan(paragraphIndex));
    }

    [Test]
    public void PublicRepair_PreservesStrictNamespaceAndDocumentAttributes()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationships = package.GetPackageRelationshipSet()!;
        relationships.Relationships[0].Type = global::docx.DocxConstants.RelationshipTypeStrictPrefix + "officeDocument";
        package.ReplaceXmlPart(
            "/_rels/.rels",
            global::docx.DocxConstants.ContentTypeRelationships,
            relationships.WriteXml());
        package.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMainDocument,
            "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceStrictWordprocessingMl + "\" data-id=\"strict\"><w:body><w:sectPr /></w:body></w:document>");
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();

        global::docx.DocxValidationReport report = validator.RepairEmptyBody(package);
        string xml = package.FindPart("/word/document.xml")!.GetText();

        Assert.That(report.ContainsCode(global::docx.DocxDiagnosticCodes.RepairedBody), Is.True);
        Assert.That(xml, Does.Contain(global::docx.DocxConstants.NamespaceStrictWordprocessingMl));
        Assert.That(xml, Does.Contain("data-id=\"strict\""));
        Assert.That(xml, Does.Contain("<w:p"));
        Assert.That(xml.IndexOf("<w:p"), Is.LessThan(xml.IndexOf("<w:sectPr")));
        Assert.That(validator.DetectNamespaceConformance(package), Is.EqualTo(global::docx.DocxNamespaceConformance.Strict));
        Assert.That(validator.ValidateWordprocessingStructure(package).Errors, Is.Empty);
    }

    [Test]
    public void DocxValidator_DelegatesEmptyBodyRepair()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "");

        global::docx.DocxValidationReport report = validator.RepairEmptyBody(package);

        Assert.That(report.ContainsCode(global::docx.DocxRepair.RepairedBodyCode), Is.True);
        Assert.That(package.FindPart("/word/document.xml")!.GetText(), Does.Contain("<w:p"));
    }

    [Test]
    public void RepairEmptyBody_DoesNotHideMalformedMainDocumentXml()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document>");

        global::docx.DocxValidationReport repairReport = repair.RepairEmptyBody(package);
        global::docx.DocxValidationReport validationReport = new global::docx.DocxPackageValidator().ValidateXmlWellFormedness(package);

        Assert.That(repairReport.ContainsCode(global::docx.DocxRepair.RepairedBodyCode), Is.False);
        Assert.That(package.FindPart("/word/document.xml")!.GetText(), Is.EqualTo("<w:document>"));
        Assert.That(validationReport.ContainsCode(global::docx.DocxPackageValidator.InvalidXmlPartCode), Is.True);
    }

    [Test]
    public void RepairOperations_RecoverPackageMissingCatalogRelationshipsBodyAndOrphanedTarget()
    {
        global::docx.DocxPackage package = CreateRecoverablyCorruptPackage();
        global::docx.DocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxRepair repair = new global::docx.DocxRepair();

        global::docx.DocxValidationReport before = validator.ValidatePackage(package);
        global::docx.DocxValidationReport contentTypesReport = repair.RepairMissingContentTypes(package);
        global::docx.DocxValidationReport relationshipsReport = repair.RepairMissingRelationships(package);
        global::docx.DocxValidationReport orphanedReport = repair.RepairOrphanedRelationships(package);
        global::docx.DocxValidationReport bodyReport = repair.RepairEmptyBody(package);
        global::docx.DocxValidationReport after = validator.ValidatePackage(package);
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(before.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(before.ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
        Assert.That(before.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode), Is.True);
        Assert.That(contentTypesReport.ContainsCode(global::docx.DocxRepair.RepairedPartContentTypeCode), Is.True);
        Assert.That(relationshipsReport.ContainsCode(global::docx.DocxRepair.RepairedRelationshipCode), Is.True);
        Assert.That(orphanedReport.ContainsCode(global::docx.DocxRepair.RepairedOrphanedRelationshipCode), Is.True);
        Assert.That(bodyReport.ContainsCode(global::docx.DocxRepair.RepairedBodyCode), Is.True);
        Assert.That(after.Count(), Is.EqualTo(0));
        Assert.That(document.Body.BlockCount(), Is.EqualTo(1));
        Assert.That(package.FindRelationshipSet("/word/document.xml")!.FindById("rIdMissing"), Is.Null);
        Assert.That(package.FindRelationshipSet("/word/document.xml")!.FindById("rIdExternal"), Is.Not.Null);
    }

    [Test]
    public void PublicRepair_AssignsMissingRelationshipIdsWithPreciseDiagnostics()
    {
        global::docx.IDocxValidator repair = new global::docx.DocxValidator();
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = package.GetPackageRelationshipSet()!;
        relationshipSet.Relationships[0].Id = "";

        global::docx.DocxValidationReport report = repair.RepairMissingRelationshipIds(package);
        global::docx.DocxError diagnostic = report.Errors[0];

        Assert.That(report.Errors, Has.Count.EqualTo(1));
        Assert.That(diagnostic.Severity, Is.EqualTo(global::docx.DocxErrorSeverity.Information));
        Assert.That(diagnostic.Code, Is.EqualTo(global::docx.DocxDiagnosticCodes.RepairedRelationshipId));
        Assert.That(diagnostic.Message, Is.EqualTo("Assigned missing relationship id."));
        Assert.That(diagnostic.Location, Is.EqualTo("/#rId1"));
        Assert.That(relationshipSet.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(package.FindPart("/_rels/.rels")!.GetText(), Does.Contain("Id=\"rId1\""));
        Assert.That(repair.RepairMissingRelationshipIds(package).Errors, Is.Empty);
    }

    [Test]
    public void PublicRepair_RepairsAllRecoverableDefectsAndReturnsStableDiagnostics()
    {
        global::docx.IDocxValidator repair = new global::docx.DocxValidator();
        global::docx.DocxPackage package = CreateRecoverablyCorruptPackage();

        global::docx.DocxValidationReport report = repair.RepairRecoverableDefects(package);
        global::docx.DocxValidationReport validation = repair.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxDiagnosticCodes.RepairedBody), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxDiagnosticCodes.RepairedPartContentType), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxDiagnosticCodes.RepairedRelationship), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxDiagnosticCodes.RepairedOrphanedRelationship), Is.True);
        Assert.That(validation.Errors, Is.Empty);

        foreach (global::docx.DocxError diagnostic in report.Errors)
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(global::docx.DocxErrorSeverity.Information));
            Assert.That(diagnostic.Code, Is.Not.Empty);
            Assert.That(diagnostic.Message, Is.Not.Empty);
            Assert.That(diagnostic.Location, Is.Not.Empty);
        }
    }

    [Test]
    public void DocxRepair_RejectsNullArguments()
    {
        global::docx.DocxRepair repair = new global::docx.DocxRepair();
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();
        global::docx.DocxPart part = new global::docx.DocxPart("/word/document.xml", "", Encoding.UTF8.GetBytes("<document />"), false);
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/");
        global::docx.DocxRelationship relationship = new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/document.xml", "");
        global::docx.DocxValidator facade = new global::docx.DocxValidator();
        System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument();
        xmlDocument.LoadXml("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body /></w:document>");
        System.Xml.XmlElement body = (System.Xml.XmlElement)xmlDocument.GetElementsByTagName("body", global::docx.DocxConstants.NamespaceWordprocessingMl)[0]!;

        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingContentTypes((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingContentTypesIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingContentTypesIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingRelationships((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingRelationshipsIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingRelationshipsIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationships((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipsIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipsIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipSets(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipSets(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipsInSet(null!, relationshipSet, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipsInSet(package, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairOrphanedRelationshipsInSet(package, relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => repair.IsOrphanedRelationship(null!, relationshipSet, relationship));
        Assert.Throws<ArgumentNullException>(() => repair.IsOrphanedRelationship(package, null!, relationship));
        Assert.Throws<ArgumentNullException>(() => repair.IsOrphanedRelationship(package, relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RemoveRelationshipPart(null!, "/_rels/.rels"));
        Assert.Throws<ArgumentNullException>(() => repair.RemoveRelationshipPart(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairEmptyBody((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairEmptyBodyIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairEmptyBodyIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMainDocumentBody(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMainDocumentBody(part, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMainDocumentBodyXml(null!));
        Assert.Throws<ArgumentNullException>(() => repair.FindMainDocumentBodyElement(null!));
        Assert.Throws<ArgumentNullException>(() => repair.BodyHasContent(null!));
        Assert.Throws<ArgumentNullException>(() => repair.CreateBodyElement(null!));
        Assert.Throws<ArgumentNullException>(() => repair.InsertMinimalParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => repair.FindFirstBodySectionProperties(null!));
        Assert.Throws<ArgumentNullException>(() => repair.CreateMinimalParagraphElement(null!));
        Assert.That(repair.BodyHasContent(body), Is.False);
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPackageRelationships(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPackageRelationships(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairKnownPartRelationships(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairKnownPartRelationships(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownHeaderFooterRelationship(null!, relationshipSet, part, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownHeaderFooterRelationship(package, null!, part, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownHeaderFooterRelationship(package, relationshipSet, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownHeaderFooterRelationship(package, relationshipSet, part, null!));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownRelationship(null!, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownRelationship(package, null!, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownRelationship(package, relationshipSet, null!, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownRelationship(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownRelationship(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", null!));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownPackageRelationship(null!, global::docx.DocxConstants.RelationshipTypeCoreProperties, "/docProps/core.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownPackageRelationship(package, null!, "/docProps/core.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownPackageRelationship(package, global::docx.DocxConstants.RelationshipTypeCoreProperties, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureKnownPackageRelationship(package, global::docx.DocxConstants.RelationshipTypeCoreProperties, "/docProps/core.xml", null!));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationshipSet(null!, "/", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationshipSet(package, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationshipSet(package, "/", null!));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationship(null!, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationship(package, null!, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationship(package, relationshipSet, null!, "/word/styles.xml", report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationship(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.EnsureRelationship(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml", null!));
        Assert.Throws<ArgumentNullException>(() => repair.ContainsInternalRelationship(null!, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml"));
        Assert.Throws<ArgumentNullException>(() => repair.ContainsInternalRelationship(package, null!, global::docx.DocxConstants.RelationshipTypeStyles, "/word/styles.xml"));
        Assert.Throws<ArgumentNullException>(() => repair.ContainsInternalRelationship(package, relationshipSet, null!, "/word/styles.xml"));
        Assert.Throws<ArgumentNullException>(() => repair.ContainsInternalRelationship(package, relationshipSet, global::docx.DocxConstants.RelationshipTypeStyles, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipParts(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipParts(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipPart(null!, relationshipSet, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipPart(package, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipPart(package, relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => repair.FindMainDocumentPartName(null!));
        Assert.Throws<ArgumentNullException>(() => repair.CreateRelationshipTarget(null!, "/word/styles.xml"));
        Assert.Throws<ArgumentNullException>(() => repair.CreateRelationshipTarget("/word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => repair.HasKnownDocumentRelationshipParts(null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairDefaultContentType(null!, "xml", global::docx.DocxConstants.ContentTypeXml, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairDefaultContentType(contentTypes, null!, global::docx.DocxConstants.ContentTypeXml, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairDefaultContentType(contentTypes, "xml", null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairDefaultContentType(contentTypes, "xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentTypes(null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentTypes(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentType(null!, relationshipSet, relationship, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentType(package, null!, relationship, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentType(package, relationshipSet, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairRelationshipTargetContentType(package, relationshipSet, relationship, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentType(null!, part, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentType(package, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentType(package, part, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentTypeWithKnownType(null!, part, global::docx.DocxConstants.ContentTypeXml, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentTypeWithKnownType(package, null!, global::docx.DocxConstants.ContentTypeXml, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentTypeWithKnownType(package, part, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairMissingPartContentTypeWithKnownType(package, part, global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => repair.RepairPartContentTypeMetadata(null!, global::docx.DocxConstants.ContentTypeXml, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairPartContentTypeMetadata(part, null!, report));
        Assert.Throws<ArgumentNullException>(() => repair.RepairPartContentTypeMetadata(part, global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentType(null!, part));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentType(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentTypeFromRelationships(null!, part));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentTypeFromRelationships(package, null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferRelationshipTargetContentType(null!, part));
        Assert.Throws<ArgumentNullException>(() => repair.InferRelationshipTargetContentType(relationship, null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentTypeFromPartName(null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentTypeFromExtension(null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferContentTypeFromBytes(null!));
        Assert.Throws<ArgumentNullException>(() => repair.InferImageContentType(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairMissingContentTypes(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairRecoverableDefects(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairMissingRelationshipIds(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairMissingRelationships(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairOrphanedRelationships(null!));
        Assert.Throws<ArgumentNullException>(() => facade.RepairEmptyBody(null!));
    }

    static global::docx.DocxPackage CreatePackageWithoutContentTypes()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = new global::docx.DocxRelationshipSet("/");

        rootRelationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/actual-main.xml", false);
        package.RelationshipSets.Add(rootRelationships);
        package.Parts.Add(new global::docx.DocxPart("/_rels/.rels", "", Encoding.UTF8.GetBytes(rootRelationships.WriteXml()), true));
        package.Parts.Add(new global::docx.DocxPart("/word/actual-main.xml", "", Encoding.UTF8.GetBytes("<document />"), false));
        package.Parts.Add(new global::docx.DocxPart("/word/styles.xml", "", Encoding.UTF8.GetBytes("<styles />"), false));
        package.Parts.Add(new global::docx.DocxPart("/word/media/image1.png", "", CreatePngBytes(), false));

        return package;
    }

    static global::docx.DocxPackage CreateRecoverablyCorruptPackage()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdMissing", global::docx.DocxConstants.RelationshipTypeImage, "media/missing.png", false);
        documentRelationships.CreateRelationshipWithId("rIdExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/", true);
        package.Parts.Add(new global::docx.DocxPart("/word/document.xml", "", Encoding.UTF8.GetBytes(""), false));
        package.Parts.Add(new global::docx.DocxPart("/word/_rels/document.xml.rels", "", Encoding.UTF8.GetBytes(documentRelationships.WriteXml()), true));
        package.RelationshipSets.Add(documentRelationships);

        return package;
    }

    static byte[] CreatePngBytes()
    {
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
    }

    static global::docx.DocxPackage CreatePackageWithOrphanedImageRelationship()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationshipSet.CreateRelationshipWithId("rIdMissing", global::docx.DocxConstants.RelationshipTypeImage, "media/missing.png", false);
        relationshipSet.CreateRelationshipWithId("rIdExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());

        return package;
    }

    static global::docx.DocxValidationReport ValidateRelationshipPartPresence(global::docx.DocxPackage package)
    {
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();
        new global::docx.DocxPackageValidator().ValidateRelationshipPartPresence(package, report);
        return report;
    }

    static global::docx.DocxValidationReport ValidateRelationshipTargets(global::docx.DocxPackage package)
    {
        return new global::docx.DocxPackageValidator().ValidateRelationshipTargets(package);
    }

    static global::docx.DocxValidationReport ValidatePackageRelationships(global::docx.DocxPackage package)
    {
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();
        new global::docx.DocxPackageValidator().ValidatePackageRelationships(package, report);
        return report;
    }

    static bool ContainsRelationshipTo(global::docx.DocxPackage package, global::docx.DocxRelationshipSet relationshipSet, string relationshipType, string targetPartName)
    {
        return new global::docx.DocxRepair().ContainsInternalRelationship(package, relationshipSet, relationshipType, targetPartName);
    }

    static void RemovePartOnly(global::docx.DocxPackage package, string partName)
    {
        for (int index = 0; index < package.Parts.Count; index = index + 1)
        {
            if (package.Parts[index].PartName == partName)
            {
                package.Parts.RemoveAt(index);
                return;
            }
        }
    }
}
