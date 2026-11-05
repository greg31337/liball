using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetHyperlinkTests
{
    [Test]
    public void ReadString_ResolvesExternalHyperlinkRelationshipsAndKeepsInternalLocations()
    {
        string worksheetXml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheetData />" +
            "<hyperlinks>" +
            "<hyperlink ref=\"A1\" r:id=\"rId1\" display=\"External\" tooltip=\"Open\" />" +
            "<hyperlink ref=\"B2\" location=\"'Sheet 2'!A1\" display=\"Internal\" />" +
            "</hyperlinks>" +
            "</worksheet>";
        string relationshipsXml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/report\" TargetMode=\"External\" />" +
            "</Relationships>";
        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadString(XlsxPackagePaths.Worksheet1, relationshipsXml);

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadStringWithRelationships(worksheetXml, relationships);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Hyperlinks.Count, Is.EqualTo(2));
        Assert.That(part.Hyperlinks[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.Hyperlinks[0].RelationshipId, Is.EqualTo("rId1"));
        Assert.That(part.Hyperlinks[0].HasRelationship, Is.True);
        Assert.That(part.Hyperlinks[0].IsExternal, Is.True);
        Assert.That(part.Hyperlinks[0].Target, Is.EqualTo("https://example.com/report"));
        Assert.That(part.Hyperlinks[0].ExternalTarget, Is.EqualTo("https://example.com/report"));
        Assert.That(part.Hyperlinks[1].Reference, Is.EqualTo("B2"));
        Assert.That(part.Hyperlinks[1].Location, Is.EqualTo("'Sheet 2'!A1"));
        Assert.That(part.Hyperlinks[1].HasRelationship, Is.False);
    }

    [Test]
    public void WriteString_EmitsExternalAndInternalHyperlinksAndBuildsRelationshipsPart()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Hyperlinks.Add(CreateExternalHyperlink("A1", "rId2", "https://example.com", "Example"));
        part.Hyperlinks.Add(CreateInternalHyperlink("B2:C2", "'Summary'!A1", "Summary"));

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetHyperlinkResult relationshipsResult = part.CreateHyperlinkRelationshipsForPartPath(XlsxPackagePaths.Worksheet1);
        XlsxRelationshipsWriteResult relationshipsWriteResult = new XlsxPartRelationshipsWriter().WriteString(relationshipsResult.Relationships);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<hyperlinks>"));
        Assert.That(writeResult.Xml, Does.Contain("ref=\"A1\""));
        Assert.That(writeResult.Xml, Does.Contain("r:id=\"rId2\""));
        Assert.That(writeResult.Xml, Does.Contain("location=\"'Summary'!A1\""));
        Assert.That(relationshipsResult.IsValid, Is.True);
        Assert.That(relationshipsResult.Relationships.PartPath, Is.EqualTo("/xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.That(relationshipsResult.Relationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(relationshipsWriteResult.IsValid, Is.True);
        Assert.That(relationshipsWriteResult.Xml, Does.Contain("Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\""));
        Assert.That(relationshipsWriteResult.Xml, Does.Contain("Target=\"https://example.com\""));
        Assert.That(relationshipsWriteResult.Xml, Does.Contain("TargetMode=\"External\""));
    }

    [Test]
    public void ResolveHyperlinkRelationships_ReportsMissingAndWrongRelationshipTargets()
    {
        XlsxWorksheetPart missingPart = new XlsxWorksheetPart();
        XlsxWorksheetPart wrongTypePart = new XlsxWorksheetPart();
        XlsxRelationshipsPart wrongTypeRelationships = new XlsxRelationshipsPart();
        XlsxRelationship wrongType = new XlsxRelationship();

        missingPart.Hyperlinks.Add(CreateExternalHyperlink("A1", "rId1", "", "Missing"));
        wrongTypePart.Hyperlinks.Add(CreateExternalHyperlink("A1", "rId1", "", "Wrong"));
        wrongTypeRelationships.SourcePartPath = XlsxPackagePaths.Worksheet1;
        wrongTypeRelationships.PartPath = "/xl/worksheets/_rels/sheet1.xml.rels";
        wrongType.Id = "rId1";
        wrongType.Type = XlsxRelationshipTypes.Worksheet;
        wrongType.Target = "sheet2.xml";
        wrongTypeRelationships.Relationships.Add(wrongType);

        XlsxWorksheetHyperlinkResult? missingRelationships = missingPart.ResolveHyperlinkRelationships(null!);
        XlsxWorksheetHyperlinkResult missingRelationship = missingPart.ResolveHyperlinkRelationships(new XlsxRelationshipsPart());
        XlsxWorksheetHyperlinkResult wrongTypeResult = wrongTypePart.ResolveHyperlinkRelationships(wrongTypeRelationships);

        Assert.That(missingRelationships.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRelationships, "worksheet.hyperlinkRelationshipsMissing"), Is.True);
        Assert.That(missingRelationship.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRelationship, "worksheet.hyperlinkRelationshipMissing"), Is.True);
        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(wrongTypeResult, "worksheet.hyperlinkRelationshipType"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsInvalidHyperlinkDefinitions()
    {
        XlsxWorksheetPart nullHyperlinks = new XlsxWorksheetPart();
        XlsxWorksheetPart nullHyperlink = new XlsxWorksheetPart();
        XlsxWorksheetPart missingReference = new XlsxWorksheetPart();
        XlsxWorksheetPart invalidReference = new XlsxWorksheetPart();
        XlsxWorksheetPart missingTarget = new XlsxWorksheetPart();
        XlsxWorksheetPart invalidRelationshipId = new XlsxWorksheetPart();

        nullHyperlinks.Hyperlinks = null!;
        nullHyperlink.Hyperlinks.Add(null!);
        missingReference.Hyperlinks.Add(CreateInternalHyperlink("", "Sheet1!A1", "Missing ref"));
        invalidReference.Hyperlinks.Add(CreateInternalHyperlink("XFE1", "Sheet1!A1", "Bad ref"));
        missingTarget.Hyperlinks.Add(new XlsxWorksheetHyperlink());
        missingTarget.Hyperlinks[0].Reference = "A1";
        invalidRelationshipId.Hyperlinks.Add(CreateExternalHyperlink("A1", "1bad", "https://example.com", "Bad id"));

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullHyperlinksResult = writer.WriteBytes(nullHyperlinks);
        XlsxWorksheetPartWriteResult nullHyperlinkResult = writer.WriteBytes(nullHyperlink);
        XlsxWorksheetPartWriteResult missingReferenceResult = writer.WriteBytes(missingReference);
        XlsxWorksheetPartWriteResult invalidReferenceResult = writer.WriteBytes(invalidReference);
        XlsxWorksheetPartWriteResult missingTargetResult = writer.WriteBytes(missingTarget);
        XlsxWorksheetPartWriteResult invalidRelationshipIdResult = writer.WriteBytes(invalidRelationshipId);

        Assert.That(nullHyperlinksResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullHyperlinksResult, "worksheet.hyperlinksNull"), Is.True);
        Assert.That(nullHyperlinkResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullHyperlinkResult, "worksheet.hyperlinkNull"), Is.True);
        Assert.That(missingReferenceResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingReferenceResult, "worksheet.hyperlinkReferenceMissing"), Is.True);
        Assert.That(invalidReferenceResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReferenceResult, "worksheet.hyperlinkReferenceInvalid"), Is.True);
        Assert.That(missingTargetResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTargetResult, "worksheet.hyperlinkTargetMissing"), Is.True);
        Assert.That(invalidRelationshipIdResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRelationshipIdResult, "worksheet.hyperlinkRelationshipIdInvalid"), Is.True);
    }

    [Test]
    public void CreateHyperlinkRelationships_RejectsRelationshipBackedLinksWithoutTargets()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Hyperlinks.Add(CreateExternalHyperlink("A1", "rId1", "", "Missing"));

        XlsxWorksheetHyperlinkResult result = part.CreateHyperlinkRelationshipsForPartPath(XlsxPackagePaths.Worksheet1);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "worksheet.hyperlinkRelationshipTargetMissing"), Is.True);
    }

    static XlsxWorksheetHyperlink CreateExternalHyperlink(string reference, string relationshipId, string target, string display)
    {
        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = reference;
        hyperlink.RelationshipId = relationshipId;
        hyperlink.Target = target;
        hyperlink.TargetMode = "External";
        hyperlink.IsExternal = true;
        hyperlink.Display = display;
        hyperlink.Tooltip = "tip";
        return hyperlink;
    }

    static XlsxWorksheetHyperlink CreateInternalHyperlink(string reference, string location, string display)
    {
        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = reference;
        hyperlink.Location = location;
        hyperlink.Display = display;
        return hyperlink;
    }

    static bool HasDiagnostic(XlsxWorksheetPartWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxWorksheetHyperlinkResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
