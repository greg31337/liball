using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackageValidatorTests
{
    [Test]
    public void ValidateDuplicateZipEntries_ReportsDuplicatePackageEntries()
    {
        MemoryStream stream = CreateZipWithDuplicateEntries();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateDuplicateZipEntries(stream);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.HasErrors(), Is.True);
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.DuplicateZipEntryCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml"));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void ValidateDuplicateZipEntries_ReportsEachDuplicateNameOnce()
    {
        MemoryStream stream = CreateZipWithTripleDuplicateEntries();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateDuplicateZipEntries(stream);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void ValidateDuplicateZipEntries_IgnoresDirectoriesAndPassesUniqueEntries()
    {
        MemoryStream stream = CreateZipWithUniqueEntriesAndDuplicateDirectories();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateDuplicateZipEntries(stream);

        Assert.That(report.Count(), Is.EqualTo(0));
        Assert.That(report.HasErrors(), Is.False);
    }

    [Test]
    public void ValidateRequiredPackageItems_PassesWhenRequiredOpcItemsExist()
    {
        MemoryStream stream = CreateZipWithRequiredOpcItems(true, true);
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRequiredPackageItems(stream);

        Assert.That(report.Count(), Is.EqualTo(0));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void ValidateRequiredPackageItems_ReportsMissingContentTypes()
    {
        MemoryStream stream = CreateZipWithRequiredOpcItems(false, true);
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRequiredPackageItems(stream);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingContentTypesCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/[Content_Types].xml"));
    }

    [Test]
    public void ValidateRequiredPackageItems_ReportsMissingPackageRelationships()
    {
        MemoryStream stream = CreateZipWithRequiredOpcItems(true, false);
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRequiredPackageItems(stream);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/_rels/.rels"));
    }

    [Test]
    public void ValidateRequiredPackageItems_ReportsAllMissingRequiredItems()
    {
        MemoryStream stream = CreateZipWithRequiredOpcItems(false, false);
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRequiredPackageItems(stream);

        Assert.That(report.Count(), Is.EqualTo(2));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
    }

    [Test]
    public void ValidateStream_CombinesOpcZipEntryAndRequiredItemChecks()
    {
        MemoryStream stream = CreateZipWithDuplicateEntriesAndMissingRequiredItems();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.Validate(stream);

        Assert.That(report.Count(), Is.EqualTo(3));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicateZipEntryCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void ValidatePackage_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidatePackage_ReportsOpcCatalogProblems()
    {
        global::docx.DocxPackage package = CreatePackageWithOpcCatalogProblems();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.Count(), Is.GreaterThanOrEqualTo(5));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicatePartNameCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicateRelationshipSetSourceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicateRelationshipIdCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipPartCode), Is.True);
        Assert.That(report.Errors[1].Location, Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void ValidatePackage_ReportsMissingPackageRelationshipSet()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<document />");

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
        Assert.That(report.Errors[0].Location, Is.EqualTo("/_rels/.rels"));
    }

    [Test]
    public void ValidateWordprocessingRequiredParts_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateWordprocessingRequiredParts(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateWordprocessingRequiredParts_ReportsMissingMainDocumentRelationshipSet()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<document />");

        global::docx.DocxValidationReport report = validator.ValidateWordprocessingRequiredParts(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingMainDocumentRelationshipCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/_rels/.rels"));
    }

    [Test]
    public void ValidateWordprocessingRequiredParts_ReportsExternalOnlyMainDocumentRelationship()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxRelationshipSet rootRelationships = new global::docx.DocxRelationshipSet("/");

        rootRelationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "https://example.com/document.xml", true);
        package.AddXmlPartWithContent("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        global::docx.DocxValidationReport report = validator.ValidateWordprocessingRequiredParts(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingMainDocumentRelationshipCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/_rels/.rels"));
    }

    [Test]
    public void ValidateWordprocessingRequiredParts_ReportsMissingMainDocumentPart()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/missing.xml");

        package.AddXmlPartWithContent("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        global::docx.DocxValidationReport report = validator.ValidateWordprocessingRequiredParts(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingMainDocumentPartCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/missing.xml"));
    }

    [Test]
    public void ValidateWordprocessingRequiredParts_ReportsMultipleMainDocumentRelationships()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxRelationshipSet rootRelationships = new global::docx.DocxRelationshipSet("/");

        rootRelationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/document.xml", false);
        rootRelationships.CreateRelationshipWithId("rId2", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/alternate.xml", false);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<document />");
        package.AddXmlPartWithContent("/word/alternate.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<document />");
        package.AddXmlPartWithContent("/_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        global::docx.DocxValidationReport report = validator.ValidateWordprocessingRequiredParts(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MultipleMainDocumentRelationshipsCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/_rels/.rels"));
    }

    [Test]
    public void ValidateRelationshipTargets_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRelationshipTargets(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateRelationshipTargets_ReportsInvalidInternalTargets()
    {
        global::docx.DocxPackage package = CreatePackageWithInvalidInternalRelationshipTargets();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRelationshipTargets(package);

        Assert.That(report.Count(), Is.EqualTo(3));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.InvalidRelationshipTargetCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml#rIdOutside"));
        Assert.That(report.Errors[1].Location, Is.EqualTo("/word/document.xml#rIdUri"));
        Assert.That(report.Errors[2].Location, Is.EqualTo("/word/document.xml#rIdSpace"));
    }

    [Test]
    public void ValidateRelationshipTargets_ReportsInvalidExternalTargets()
    {
        global::docx.DocxPackage package = CreatePackageWithInvalidExternalRelationshipTargets();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRelationshipTargets(package);

        Assert.That(report.Count(), Is.EqualTo(2));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.InvalidRelationshipTargetCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml#rIdRelativeExternal"));
        Assert.That(report.Errors[1].Location, Is.EqualTo("/word/document.xml#rIdSpaceExternal"));
    }

    [Test]
    public void ValidateRelationshipTargets_ReportsMissingInternalTargetPart()
    {
        global::docx.DocxPackage package = CreatePackageWithMissingRelationshipTargetPart();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateRelationshipTargets(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/media/missing.png"));
    }

    [Test]
    public void ValidateRelationshipTargets_ReportsInvalidTargetMode()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.Relationships.Add(new global::docx.DocxRelationship("rIdBadMode", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", "Bogus"));
        package.RelationshipSets.Add(documentRelationships);

        global::docx.DocxValidationReport report = validator.ValidateRelationshipTargets(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.InvalidRelationshipTargetModeCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml#rIdBadMode"));
    }

    [Test]
    public void ValidatePackage_IncludesRelationshipTargetValidation()
    {
        global::docx.DocxPackage package = CreatePackageWithMissingRelationshipTargetPart();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode), Is.True);
    }

    [Test]
    public void ValidateContentTypes_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateContentTypes(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateContentTypes_ReportsDuplicateCatalogEntries()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.ContentTypes.Defaults.Add(new global::docx.DocxContentTypeDefault("XML", global::docx.DocxConstants.ContentTypeXml));
        package.ContentTypes.Overrides.Add(new global::docx.DocxContentTypeOverride("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument));

        global::docx.DocxValidationReport report = validator.ValidateContentTypes(package);

        Assert.That(report.Count(), Is.EqualTo(2));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicateContentTypeDefaultCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.DuplicateContentTypeOverrideCode), Is.True);
    }

    [Test]
    public void ValidateContentTypes_ReportsInvalidCatalogEntries()
    {
        global::docx.DocxPackage package = CreatePackageWithInvalidContentTypeCatalogEntries();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateContentTypes(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.InvalidContentTypeDefaultExtensionCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.InvalidContentTypeOverridePartNameCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.InvalidContentTypeCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypeOverridePartCode), Is.True);
    }

    [Test]
    public void ValidateContentTypes_ReportsMissingAndMismatchedPartContentTypes()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxPart documentPart = package.FindPart("/word/document.xml")!;

        documentPart.ContentType = global::docx.DocxConstants.ContentTypeXml;
        package.Parts.Add(new global::docx.DocxPart("/custom/data.bin", "application/octet-stream", new byte[] { 1 }, false));

        global::docx.DocxValidationReport report = validator.ValidateContentTypes(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.PartContentTypeMismatchCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingPartContentTypeCode), Is.True);
    }

    [Test]
    public void ValidateContentTypes_ReportsUnexpectedKnownRelationshipTargetContentType()
    {
        global::docx.DocxPackage package = CreatePackageWithUnexpectedKnownRelationshipTargetContentType();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateContentTypes(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.UnexpectedPartContentTypeCode), Is.True);
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/styles.xml"));
    }

    [Test]
    public void ValidatePackage_IncludesContentTypeValidation()
    {
        global::docx.DocxPackage package = CreatePackageWithUnexpectedKnownRelationshipTargetContentType();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.UnexpectedPartContentTypeCode), Is.True);
    }

    [Test]
    public void ValidateXmlWellFormedness_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateXmlWellFormedness(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateXmlWellFormedness_ReportsMalformedXmlPart()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.FindPart("/word/document.xml")!.ReplaceText("<w:document>");

        global::docx.DocxValidationReport report = validator.ValidateXmlWellFormedness(package);

        Assert.That(report.Count(), Is.EqualTo(1));
        Assert.That(report.Errors[0].Code, Is.EqualTo(global::docx.DocxPackageValidator.InvalidXmlPartCode));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void ValidateXmlWellFormedness_ReportsMalformedRelationshipPartAndXmlExtensionFallback()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.Parts.Add(new global::docx.DocxPart("/word/_rels/bad.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, Encoding.UTF8.GetBytes("<Relationships>"), true));
        package.Parts.Add(new global::docx.DocxPart("/custom/raw.xml", "application/octet-stream", Encoding.UTF8.GetBytes("<root>"), false));

        global::docx.DocxValidationReport report = validator.ValidateXmlWellFormedness(package);

        Assert.That(report.Count(), Is.EqualTo(2));
        Assert.That(report.Errors[0].Location, Is.EqualTo("/word/_rels/bad.xml.rels"));
        Assert.That(report.Errors[1].Location, Is.EqualTo("/custom/raw.xml"));
    }

    [Test]
    public void ValidatePackage_IncludesXmlWellFormednessValidation()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        package.FindPart("/word/document.xml")!.ReplaceText("<w:document>");

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.InvalidXmlPartCode), Is.True);
    }

    [Test]
    public void ValidateReferences_PassesForCreatedPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateReferences(package);

        Assert.That(report.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ValidateReferences_ReportsMissingStyleNumberingNoteCommentAndBookmarkReferences()
    {
        global::docx.DocxPackage package = CreatePackageWithReferenceProblems();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateReferences(package);

        Assert.That(report.Count(), Is.EqualTo(7));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingStyleReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingNumberingReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingFootnoteReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingEndnoteReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingCommentReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.UnmatchedBookmarkStartCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.UnmatchedBookmarkEndCode), Is.True);
    }

    [Test]
    public void ValidateReferences_ReportsHeaderFooterAndMediaRelationshipReferenceProblems()
    {
        global::docx.DocxPackage package = CreatePackageWithRelationshipReferenceProblems();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidateReferences(package);

        Assert.That(report.Count(), Is.EqualTo(4));
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipReferenceCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.UnexpectedRelationshipReferenceTypeCode), Is.True);
    }

    [Test]
    public void ValidatePackage_IncludesReferenceValidation()
    {
        global::docx.DocxPackage package = CreatePackageWithReferenceProblems();
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingStyleReferenceCode), Is.True);
    }

    [Test]
    public void ValidatorFacade_CombinesOpcAndWordprocessingValidation()
    {
        global::docx.DocxValidator validator = new global::docx.DocxValidator();
        MemoryStream stream = CreateZipWithRequiredOpcItems(false, false);
        global::docx.DocxPackage package = new global::docx.DocxPackage();

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<document />");

        Assert.That(validator.ValidatePackage(global::docx.DocxPackage.Create()).Count(), Is.EqualTo(0));
        Assert.That(validator.ValidatePackage(package).ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
        Assert.That(validator.ValidatePackage(package).ContainsCode(global::docx.DocxPackageValidator.MissingMainDocumentRelationshipCode), Is.True);
        Assert.That(validator.Validate(stream).ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(validator.ValidateWordprocessingRequiredParts(package).ContainsCode(global::docx.DocxPackageValidator.MissingMainDocumentRelationshipCode), Is.True);
        Assert.That(validator.ValidateRelationshipTargets(CreatePackageWithMissingRelationshipTargetPart()).ContainsCode(global::docx.DocxPackageValidator.MissingRelationshipTargetPartCode), Is.True);
        Assert.That(validator.ValidateContentTypes(CreatePackageWithUnexpectedKnownRelationshipTargetContentType()).ContainsCode(global::docx.DocxPackageValidator.UnexpectedPartContentTypeCode), Is.True);
        Assert.That(validator.ValidateXmlWellFormedness(CreatePackageWithMalformedMainDocumentXml()).ContainsCode(global::docx.DocxPackageValidator.InvalidXmlPartCode), Is.True);
        Assert.That(validator.ValidateReferences(CreatePackageWithReferenceProblems()).ContainsCode(global::docx.DocxPackageValidator.MissingStyleReferenceCode), Is.True);
    }

    [Test]
    public void ValidatorFacade_ReportsWordprocessingStructureIdentifiersRangesAndCompatibility()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxPackage package = CreatePackageWithSemanticValidationProblems();

        global::docx.DocxValidationReport structure = validator.ValidateWordprocessingStructure(package);
        global::docx.DocxValidationReport identifiers = validator.ValidateIdentifiersAndRanges(package);
        global::docx.DocxValidationReport compatibility = validator.ValidateCompatibility(package);
        global::docx.DocxValidationReport combined = validator.ValidatePackage(package);

        Assert.That(structure.ContainsCode(global::docx.DocxDiagnosticCodes.InvalidDocumentBody), Is.True);
        Assert.That(structure.ContainsCode(global::docx.DocxDiagnosticCodes.MissingRequiredAttribute), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateStyleId), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateNumberingId), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateNoteId), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateCommentId), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateBookmarkId), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.MissingAbstractNumberingReference), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.UnmatchedCommentRangeStart), Is.True);
        Assert.That(identifiers.ContainsCode(global::docx.DocxDiagnosticCodes.UnmatchedCommentRangeEnd), Is.True);
        Assert.That(compatibility.ContainsCode(global::docx.DocxDiagnosticCodes.InvalidCompatibilityMode), Is.True);
        Assert.That(combined.ContainsCode(global::docx.DocxDiagnosticCodes.InvalidDocumentBody), Is.True);
        Assert.That(combined.ContainsCode(global::docx.DocxDiagnosticCodes.DuplicateStyleId), Is.True);
        Assert.That(combined.ContainsCode(global::docx.DocxDiagnosticCodes.InvalidCompatibilityMode), Is.True);
    }

    [Test]
    public void ValidatorFacade_ValidatesNamespaceAndMacroPackageConsistency()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxPackage mixed = global::docx.DocxPackage.Create();
        mixed.AddXmlPartWithContent(
            "/custom/strict.xml",
            global::docx.DocxConstants.ContentTypeXml,
            "<w:strict xmlns:w=\"" + global::docx.DocxConstants.NamespaceStrictWordprocessingMl + "\" />");

        global::docx.DocxPackage inconsistentMacro = global::docx.DocxPackage.Create();
        inconsistentMacro.AddBinaryPart(
            "/word/vbaProject.bin",
            global::docx.DocxConstants.ContentTypeVbaProject,
            new byte[] { 1, 2, 3 });

        global::docx.DocxPackage validMacro = global::docx.DocxPackage.Create();
        validMacro.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument,
            global::docx.DocxPackage.CreateMinimalDocumentXml());
        validMacro.AddBinaryPart(
            "/word/vbaProject.bin",
            global::docx.DocxConstants.ContentTypeVbaProject,
            new byte[] { 4, 5, 6 });
        validMacro.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeVbaProject,
            "vbaProject.bin",
            false);

        Assert.That(
            validator.ValidateNamespaceConformance(mixed).ContainsCode(global::docx.DocxDiagnosticCodes.MixedNamespaceConformance),
            Is.True);
        Assert.That(
            validator.ValidateMacroPackage(inconsistentMacro).ContainsCode(global::docx.DocxDiagnosticCodes.MacroMainDocumentRequired),
            Is.True);
        Assert.That(
            validator.ValidateMacroPackage(inconsistentMacro).ContainsCode(global::docx.DocxDiagnosticCodes.MissingVbaProjectRelationship),
            Is.True);
        Assert.That(validator.ValidateMacroPackage(validMacro).Errors, Is.Empty);
    }

    [Test]
    public void ValidatorFacade_AcceptsStrictMainDocumentNamespacesAndRelationships()
    {
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
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
            "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceStrictWordprocessingMl + "\"><w:body /></w:document>");

        Assert.That(validator.DetectNamespaceConformance(package), Is.EqualTo(global::docx.DocxNamespaceConformance.Strict));
        Assert.That(validator.ValidateNamespaceConformance(package).Errors, Is.Empty);
        Assert.That(validator.ValidateWordprocessingRequiredParts(package).Errors, Is.Empty);
        Assert.That(validator.ValidateWordprocessingStructure(package).Errors, Is.Empty);
    }

    [Test]
    public void PackageValidator_RejectsNullArguments()
    {
        global::docx.DocxPackageValidator validator = new global::docx.DocxPackageValidator();
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet("/");
        global::docx.DocxRelationship relationship = new global::docx.DocxRelationship("rId1", "type", "target", "");
        global::docx.DocxContentTypes contentTypes = new global::docx.DocxContentTypes();
        global::docx.DocxPart part = package.FindPart("/word/document.xml")!;
        List<string> ids = new List<string>();
        System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(new StringReader("<root />"));
        global::docx.DocxValidator facade = new global::docx.DocxValidator();

        xmlReader.Read();

        Assert.Throws<ArgumentNullException>(() => validator.Validate((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePackage((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateDuplicateZipEntries(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRequiredPackageItems(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ContainsName(null!, "name"));
        Assert.Throws<ArgumentNullException>(() => validator.ContainsName(new List<string>(), null!));
        Assert.Throws<ArgumentNullException>(() => validator.AppendReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.AppendReport(report, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ResetStream(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypes((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypesIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypesIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypeDefaults(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypeDefaults(contentTypes, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypeOverrides(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateContentTypeOverrides(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartContentTypes(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartContentTypes(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentTypes(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentTypes(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentType(null!, relationshipSet, relationship, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentType(package, null!, relationship, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentType(package, relationshipSet, null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetContentType(package, relationshipSet, relationship, null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsExpectedContentTypeForRelationship(null!, global::docx.DocxConstants.ContentTypeXml));
        Assert.Throws<ArgumentNullException>(() => validator.IsExpectedContentTypeForRelationship(global::docx.DocxConstants.RelationshipTypeStyles, null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetExpectedContentTypeForRelationship(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsValidContentType(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsValidContentTypeDefaultExtension(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ContainsNameIgnoreCase(null!, "name"));
        Assert.Throws<ArgumentNullException>(() => validator.ContainsNameIgnoreCase(new List<string>(), null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateXmlWellFormedness(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateXmlWellFormednessIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateXmlWellFormednessIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateXmlPartWellFormedness(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateXmlPartWellFormedness(part, null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsXmlPart(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsXmlContentType(null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetXmlWellFormednessError(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ReadXmlToEnd(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateReferences(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateReferencesIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateReferencesIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(null!, part, ids, ids, ids, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, null!, ids, ids, ids, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, null!, ids, ids, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, ids, null!, ids, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, ids, ids, null!, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, ids, ids, ids, null!, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, ids, ids, ids, ids, null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartReferences(package, part, ids, ids, ids, ids, ids, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipReference(null!, part, "rId1", "", report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipReference(package, null!, "rId1", "", report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipReference(package, part, null!, "", report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipReference(package, part, "rId1", null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipReference(package, part, "rId1", "", null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateBookmarkPairs(null!, ids, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateBookmarkPairs(part, null!, ids, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateBookmarkPairs(part, ids, null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateBookmarkPairs(part, ids, ids, null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectStyleIds(null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectNumberingIds(null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectFootnoteIds(null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectEndnoteIds(null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectCommentIds(null!));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(null!, "root", "", "id", "", ids));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(part, null!, "", "id", "", ids));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(part, "root", null!, "id", "", ids));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(part, "root", "", null!, "", ids));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(part, "root", "", "id", null!, ids));
        Assert.Throws<ArgumentNullException>(() => validator.CollectElementAttributeValues(part, "root", "", "id", "", null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsStyleReferenceElement(null!));
        Assert.Throws<ArgumentNullException>(() => validator.AddNameIfMissing(null!, "id"));
        Assert.Throws<ArgumentNullException>(() => validator.AddNameIfMissing(ids, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ReadAttribute(null!, "id", ""));
        Assert.Throws<ArgumentNullException>(() => validator.ReadAttribute(xmlReader, null!, ""));
        Assert.Throws<ArgumentNullException>(() => validator.ReadAttribute(xmlReader, "id", null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePackageRelationships(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePackageRelationships(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateWordprocessingRequiredParts(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateMainDocumentRelationship(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateMainDocumentRelationship(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.CountMainDocumentRelationships(null!));
        Assert.Throws<ArgumentNullException>(() => validator.FindFirstMainDocumentRelationship(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargets(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetsIntoReport(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTargetsIntoReport(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTarget(null!, relationshipSet, relationship, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTarget(package, null!, relationship, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTarget(package, relationshipSet, null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipTarget(package, relationshipSet, relationship, null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsValidRelationshipTargetMode(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsExternalRelationshipTargetMode(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsValidExternalRelationshipTarget(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ResolveInternalRelationshipTargetPartName(null!, relationship));
        Assert.Throws<ArgumentNullException>(() => validator.ResolveInternalRelationshipTargetPartName(relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ResolveRelationshipTargetPathSegments(null!));
        Assert.Throws<ArgumentNullException>(() => validator.IsAbsoluteUriTarget(null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetUriSchemeSeparatorIndex(null!));
        Assert.Throws<ArgumentNullException>(() => validator.ContainsWhitespaceOrControl(null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetRelationshipLocation(null!, relationship));
        Assert.Throws<ArgumentNullException>(() => validator.GetRelationshipLocation(relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartNames(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidatePartNames(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipSets(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipSets(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipIds(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipIds(relationshipSet, null!));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipPartPresence(null!, report));
        Assert.Throws<ArgumentNullException>(() => validator.ValidateRelationshipPartPresence(package, null!));
        Assert.Throws<ArgumentNullException>(() => validator.GetRelationshipPartName(null!));
        Assert.Throws<ArgumentNullException>(() => facade.Validate((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidatePackage((global::docx.DocxPackage)null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateWordprocessingRequiredParts(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateContentTypes(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateXmlWellFormedness(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateReferences(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateRelationshipTargets(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateWordprocessingStructure(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateIdentifiersAndRanges(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateCompatibility(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateNamespaceConformance(null!));
        Assert.Throws<ArgumentNullException>(() => facade.ValidateMacroPackage(null!));
    }

    static MemoryStream CreateZipWithDuplicateEntries()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddTextEntry(archive, "word/document.xml", "first");
            AddTextEntry(archive, "word/document.xml", "second");
            AddTextEntry(archive, "word/styles.xml", "styles");
        }

        stream.Position = 0;
        return stream;
    }

    static MemoryStream CreateZipWithTripleDuplicateEntries()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddTextEntry(archive, "word/document.xml", "first");
            AddTextEntry(archive, "word/document.xml", "second");
            AddTextEntry(archive, "word/document.xml", "third");
        }

        stream.Position = 0;
        return stream;
    }

    static MemoryStream CreateZipWithUniqueEntriesAndDuplicateDirectories()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            archive.CreateEntry("word/");
            archive.CreateEntry("word/");
            AddTextEntry(archive, "word/document.xml", "document");
            AddTextEntry(archive, "word/styles.xml", "styles");
        }

        stream.Position = 0;
        return stream;
    }

    static MemoryStream CreateZipWithRequiredOpcItems(bool includeContentTypes, bool includePackageRelationships)
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            if (includeContentTypes)
            {
                AddTextEntry(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\" />");
            }

            if (includePackageRelationships)
            {
                AddTextEntry(archive, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\" />");
            }

            AddTextEntry(archive, "word/document.xml", "document");
        }

        stream.Position = 0;
        return stream;
    }

    static MemoryStream CreateZipWithDuplicateEntriesAndMissingRequiredItems()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddTextEntry(archive, "word/document.xml", "first");
            AddTextEntry(archive, "word/document.xml", "second");
        }

        stream.Position = 0;
        return stream;
    }

    static global::docx.DocxPackage CreatePackageWithOpcCatalogProblems()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = new global::docx.DocxRelationshipSet("/");
        global::docx.DocxRelationshipSet duplicateRootRelationships = new global::docx.DocxRelationshipSet("/");

        package.Parts.Add(new global::docx.DocxPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, Encoding.UTF8.GetBytes("<document />"), false));
        package.Parts.Add(new global::docx.DocxPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, Encoding.UTF8.GetBytes("<document />"), false));
        rootRelationships.Relationships.Add(new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/document.xml", ""));
        rootRelationships.Relationships.Add(new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "word/document.xml", ""));
        package.RelationshipSets.Add(rootRelationships);
        package.RelationshipSets.Add(duplicateRootRelationships);

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithInvalidInternalRelationshipTargets()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdOutside", global::docx.DocxConstants.RelationshipTypeImage, "../../outside.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdUri", global::docx.DocxConstants.RelationshipTypeImage, "http://example.com/item.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdSpace", global::docx.DocxConstants.RelationshipTypeImage, "media/image 1.png", false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithInvalidExternalRelationshipTargets()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdRelativeExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "word/document.xml", true);
        documentRelationships.CreateRelationshipWithId("rIdSpaceExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/item one", true);
        documentRelationships.CreateRelationshipWithId("rIdExternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/item-one", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithMissingRelationshipTargetPart()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdMissing", global::docx.DocxConstants.RelationshipTypeImage, "media/missing.png", false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithInvalidContentTypeCatalogEntries()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();

        package.ContentTypes.Defaults.Add(new global::docx.DocxContentTypeDefault("word/xml", global::docx.DocxConstants.ContentTypeXml));
        package.ContentTypes.Defaults.Add(new global::docx.DocxContentTypeDefault("badtype", "application xml"));
        package.ContentTypes.Overrides.Add(new global::docx.DocxContentTypeOverride("word/../bad.xml", global::docx.DocxConstants.ContentTypeXml));
        package.ContentTypes.Overrides.Add(new global::docx.DocxContentTypeOverride("/word/missing.xml", global::docx.DocxConstants.ContentTypeXml));

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithUnexpectedKnownRelationshipTargetContentType()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdStyles", global::docx.DocxConstants.RelationshipTypeStyles, "styles.xml", false);
        package.AddXmlPartWithContent("/word/styles.xml", global::docx.DocxConstants.ContentTypeXml, "<w:styles />");
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithMalformedMainDocumentXml()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();

        package.FindPart("/word/document.xml")!.ReplaceText("<w:document>");

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithReferenceProblems()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdStyles", global::docx.DocxConstants.RelationshipTypeStyles, "styles.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdNumbering", global::docx.DocxConstants.RelationshipTypeNumbering, "numbering.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdFootnotes", global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdEndnotes", global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rIdComments", global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", false);

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentWithReferenceProblemsXml());
        package.AddXmlPartWithContent("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles, CreateStylesXml());
        package.AddXmlPartWithContent("/word/numbering.xml", global::docx.DocxConstants.ContentTypeNumbering, CreateNumberingXml());
        package.AddXmlPartWithContent("/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, CreateFootnotesXml());
        package.AddXmlPartWithContent("/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, CreateEndnotesXml());
        package.AddXmlPartWithContent("/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, CreateCommentsXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithRelationshipReferenceProblems()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeader", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", false);
        documentRelationships.CreateRelationshipWithId("rImage", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentWithRelationshipReferenceProblemsXml());
        package.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 1, 2, 3 });
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithSemanticValidationProblems()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        string wordNamespace = global::docx.DocxConstants.NamespaceWordprocessingMl;

        package.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMainDocument,
            "<w:document xmlns:w=\"" + wordNamespace + "\"><w:p>"
                + "<w:bookmarkStart w:id=\"1\" w:name=\"first\" />"
                + "<w:bookmarkStart w:id=\"1\" w:name=\"duplicate\" />"
                + "<w:commentRangeStart w:id=\"8\" />"
                + "<w:commentRangeEnd w:id=\"9\" />"
                + "</w:p></w:document>");
        package.AddXmlPartWithContent(
            "/word/styles.xml",
            global::docx.DocxConstants.ContentTypeStyles,
            "<w:styles xmlns:w=\"" + wordNamespace + "\">"
                + "<w:style w:styleId=\"Duplicate\" />"
                + "<w:style w:type=\"paragraph\" w:styleId=\"Duplicate\" />"
                + "</w:styles>");
        package.AddXmlPartWithContent(
            "/word/numbering.xml",
            global::docx.DocxConstants.ContentTypeNumbering,
            "<w:numbering xmlns:w=\"" + wordNamespace + "\">"
                + "<w:abstractNum w:abstractNumId=\"1\" /><w:abstractNum w:abstractNumId=\"1\" />"
                + "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"7\" /></w:num>"
                + "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"1\" /></w:num>"
                + "</w:numbering>");
        package.AddXmlPartWithContent(
            "/word/footnotes.xml",
            global::docx.DocxConstants.ContentTypeFootnotes,
            "<w:footnotes xmlns:w=\"" + wordNamespace + "\"><w:footnote w:id=\"2\" /><w:footnote w:id=\"2\" /></w:footnotes>");
        package.AddXmlPartWithContent(
            "/word/comments.xml",
            global::docx.DocxConstants.ContentTypeComments,
            "<w:comments xmlns:w=\"" + wordNamespace + "\"><w:comment w:id=\"5\" /><w:comment w:id=\"5\" /></w:comments>");
        package.AddXmlPartWithContent(
            "/word/settings.xml",
            global::docx.DocxConstants.ContentTypeSettings,
            "<w:settings xmlns:w=\"" + wordNamespace + "\"><w:compat><w:compatSetting w:name=\"compatibilityMode\" w:uri=\"urn:test\" w:val=\"16\" /></w:compat></w:settings>");

        return package;
    }

    static string CreateDocumentWithReferenceProblemsXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">",
            "  <w:body>",
            "    <w:p>",
            "      <w:pPr><w:pStyle w:val=\"MissingStyle\" /><w:numPr><w:numId w:val=\"9\" /></w:numPr></w:pPr>",
            "      <w:r><w:rPr><w:rStyle w:val=\"KnownStyle\" /></w:rPr><w:t>Text</w:t><w:footnoteReference w:id=\"8\" /><w:endnoteReference w:id=\"7\" /><w:commentReference w:id=\"6\" /></w:r>",
            "      <w:bookmarkStart w:id=\"1\" w:name=\"StartOnly\" />",
            "      <w:bookmarkEnd w:id=\"2\" />",
            "    </w:p>",
            "  </w:body>",
            "</w:document>"
        });
    }

    static string CreateDocumentWithRelationshipReferenceProblemsXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">",
            "  <w:body>",
            "    <w:sectPr><w:headerReference r:id=\"rMissingHeader\" /><w:footerReference r:id=\"rImage\" /></w:sectPr>",
            "    <w:p><w:r><w:drawing><a:blip r:embed=\"rMissingImage\" r:link=\"rHeader\" /></w:drawing></w:r></w:p>",
            "  </w:body>",
            "</w:document>"
        });
    }

    static string CreateStylesXml()
    {
        return "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:style w:styleId=\"KnownStyle\" /></w:styles>";
    }

    static string CreateNumberingXml()
    {
        return "<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:abstractNum w:abstractNumId=\"1\" /><w:num w:numId=\"1\"><w:abstractNumId w:val=\"1\" /></w:num></w:numbering>";
    }

    static string CreateFootnotesXml()
    {
        return "<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:footnote w:id=\"2\" /></w:footnotes>";
    }

    static string CreateEndnotesXml()
    {
        return "<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:endnote w:id=\"3\" /></w:endnotes>";
    }

    static string CreateCommentsXml()
    {
        return "<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"4\" w:author=\"A\" /></w:comments>";
    }

    static void AddTextEntry(ZipArchive archive, string entryName, string text)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName);

        using (Stream stream = entry.Open())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
