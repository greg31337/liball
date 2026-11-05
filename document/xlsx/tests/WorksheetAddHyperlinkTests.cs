using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetAddHyperlinkTests
{
    [Test]
    public void AddHyperlink_InfersExternalTargetsAndAssignsRelationshipIds()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetHyperlinkResult first = part.AddHyperlink("A1", "https://example.com/report");
        XlsxWorksheetHyperlinkResult second = part.AddHyperlinkWithDisplay("B2", "mailto:test@example.com", "Email", "Send");

        Assert.That(first.IsValid, Is.True);
        Assert.That(first.HasHyperlink, Is.True);
        Assert.That(first.Hyperlink!.Reference, Is.EqualTo("A1"));
        Assert.That(first.Hyperlink.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(first.Hyperlink.Target, Is.EqualTo("https://example.com/report"));
        Assert.That(first.Hyperlink.IsExternal, Is.True);
        Assert.That(first.Hyperlink.Display, Is.EqualTo("https://example.com/report"));
        Assert.That(second.IsValid, Is.True);
        Assert.That(second.Hyperlink!.RelationshipId, Is.EqualTo("rId2"));
        Assert.That(second.Hyperlink.Target, Is.EqualTo("mailto:test@example.com"));
        Assert.That(second.Hyperlink.Display, Is.EqualTo("Email"));
        Assert.That(second.Hyperlink.Tooltip, Is.EqualTo("Send"));
        Assert.That(part.Hyperlinks.Count, Is.EqualTo(2));
    }

    [Test]
    public void AddHyperlink_TreatsHashAndSheetTargetsAsInternalLocations()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetHyperlinkResult hash = part.AddHyperlinkWithDisplay("B2:C2", "#'Summary'!A1", "Summary", "Jump");
        XlsxWorksheetHyperlinkResult sheet = part.AddHyperlink("D4", "Sheet1!A1");

        Assert.That(hash.IsValid, Is.True);
        Assert.That(hash.Hyperlink!.Reference, Is.EqualTo("B2:C2"));
        Assert.That(hash.Hyperlink.RelationshipId, Is.EqualTo(""));
        Assert.That(hash.Hyperlink.Location, Is.EqualTo("'Summary'!A1"));
        Assert.That(hash.Hyperlink.Display, Is.EqualTo("Summary"));
        Assert.That(hash.Hyperlink.Tooltip, Is.EqualTo("Jump"));
        Assert.That(sheet.IsValid, Is.True);
        Assert.That(sheet.Hyperlink!.Location, Is.EqualTo("Sheet1!A1"));
        Assert.That(sheet.Hyperlink.Display, Is.EqualTo("Sheet1!A1"));
    }

    [Test]
    public void AddExternalHyperlink_WritesWorksheetXmlAndRelationshipPart()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.AddExternalHyperlink("A1", "https://example.com", "Example", "Open");
        part.AddInternalHyperlink("B2", "'Sheet 2'!A1", "Internal", "Jump");

        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetHyperlinkResult relationshipResult = part.CreateHyperlinkRelationshipsForPartPath(XlsxPackagePaths.Worksheet1);
        XlsxRelationshipsWriteResult relationshipsWrite = new XlsxPartRelationshipsWriter().WriteString(relationshipResult.Relationships);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadStringWithRelationships(worksheetWrite.Xml, relationshipResult.Relationships);

        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.Xml, Does.Contain("ref=\"A1\""));
        Assert.That(worksheetWrite.Xml, Does.Contain("r:id=\"rId1\""));
        Assert.That(worksheetWrite.Xml, Does.Contain("location=\"'Sheet 2'!A1\""));
        Assert.That(relationshipResult.IsValid, Is.True);
        Assert.That(relationshipResult.Relationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(relationshipsWrite.IsValid, Is.True);
        Assert.That(relationshipsWrite.Xml, Does.Contain("Target=\"https://example.com\""));
        Assert.That(relationshipsWrite.Xml, Does.Contain("TargetMode=\"External\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Hyperlinks[0].HasRelationship, Is.True);
        Assert.That(roundTrip.Hyperlinks[0].ExternalTarget, Is.EqualTo("https://example.com"));
        Assert.That(roundTrip.Hyperlinks[1].Location, Is.EqualTo("'Sheet 2'!A1"));
    }

    [Test]
    public void AddHyperlink_RejectsInvalidReferencesMissingTargetsAndNullCollections()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetPart nullHyperlinks = new XlsxWorksheetPart();
        nullHyperlinks.Hyperlinks = null!;

        XlsxWorksheetHyperlinkResult invalidReference = part.AddHyperlink("XFE1", "https://example.com");
        XlsxWorksheetHyperlinkResult missingTarget = part.AddHyperlink("A1", " ");
        XlsxWorksheetHyperlinkResult nullCollection = nullHyperlinks.AddHyperlink("A1", "https://example.com");

        Assert.That(invalidReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReference, "worksheet.hyperlinkReferenceInvalid"), Is.True);
        Assert.That(part.Hyperlinks.Count, Is.EqualTo(0));
        Assert.That(missingTarget.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTarget, "worksheet.hyperlinkTargetMissing"), Is.True);
        Assert.That(nullCollection.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCollection, "worksheet.hyperlinksNull"), Is.True);
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
