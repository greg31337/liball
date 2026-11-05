using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CalculationChainTests
{
    [Test]
    public void ReadString_ParsesCalculationChainCellsAndUnknowns()
    {
        string xml = "<calcChain xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<c r=\"A1\" i=\"1\" s=\"1\" l=\"0\" t=\"1\" a=\"0\" x:cell=\"c\" />" +
            "<c r=\"Sheet1!B2\" />" +
            "<x:future />" +
            "</calcChain>";

        XlsxCalculationChainPart part = new XlsxCalculationChainPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "calculationChain.cellReferenceInvalid"), Is.True);
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Cells.Count, Is.EqualTo(2));
        Assert.That(part.Cells[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.Cells[0].SheetId, Is.EqualTo(1));
        Assert.That(part.Cells[0].InChildChain, Is.True);
        Assert.That(part.Cells[0].NewDependencyLevel, Is.False);
        Assert.That(part.Cells[0].NewThread, Is.True);
        Assert.That(part.Cells[0].Array, Is.False);
        Assert.That(part.Cells[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesCalculationChainAndRoundTrips()
    {
        XlsxCalculationChainPart part = new XlsxCalculationChainPart();
        part.Cells.Add(CreateCell("A1", 1, true, true, false, true, false));
        part.Cells.Add(CreateCell("B2", 2, true, false, true, false, true));

        XlsxCalculationChainPartWriteResult result = new XlsxCalculationChainPartWriter().WriteString(part);
        XlsxCalculationChainPart roundTrip = new XlsxCalculationChainPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<calcChain"));
        Assert.That(result.Xml, Does.Contain("<c r=\"A1\" i=\"1\" s=\"1\" l=\"0\" t=\"1\" a=\"0\" />"));
        Assert.That(result.Xml, Does.Contain("<c r=\"B2\" i=\"2\" s=\"0\" l=\"1\" t=\"0\" a=\"1\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Cells.Count, Is.EqualTo(2));
        Assert.That(roundTrip.Cells[1].Reference, Is.EqualTo("B2"));
        Assert.That(roundTrip.Cells[1].SheetId, Is.EqualTo(2));
        Assert.That(roundTrip.Cells[1].Array, Is.True);
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        XlsxCalculationChainPart missingRoot = new XlsxCalculationChainPartReader().ReadString("");
        XlsxCalculationChainPart invalidRoot = new XlsxCalculationChainPartReader().ReadString("<bad xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");
        XlsxCalculationChainPart invalidValues = new XlsxCalculationChainPartReader().ReadString("<calcChain xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><c i=\"bad\" s=\"maybe\" /></calcChain>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "calculationChain.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "calculationChain.invalidRoot"), Is.True);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "calculationChain.cellReferenceMissing"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsInvalidCalculationChainDefinitions()
    {
        XlsxCalculationChainPart? nullPart = null;
        XlsxCalculationChainPart nullCells = new XlsxCalculationChainPart();
        XlsxCalculationChainPart nullCell = new XlsxCalculationChainPart();
        XlsxCalculationChainPart missingReference = new XlsxCalculationChainPart();
        XlsxCalculationChainPart invalidReference = new XlsxCalculationChainPart();
        XlsxCalculationChainPart nullUnknowns = new XlsxCalculationChainPart();

        nullCells.Cells = null!;
        nullCell.Cells.Add(null!);
        missingReference.Cells.Add(new XlsxCalculationChainCell());
        invalidReference.Cells.Add(CreateCell("Bad", 0, false, false, false, false, false));
        XlsxCalculationChainCell badUnknowns = CreateCell("A1", 0, false, false, false, false, false);
        badUnknowns.UnknownAttributes = null!;
        nullUnknowns.Cells.Add(badUnknowns);

        XlsxCalculationChainPartWriter writer = new XlsxCalculationChainPartWriter();
        XlsxCalculationChainPartWriteResult nullPartResult = writer.WriteBytes(nullPart);
        XlsxCalculationChainPartWriteResult nullCellsResult = writer.WriteBytes(nullCells);
        XlsxCalculationChainPartWriteResult nullCellResult = writer.WriteBytes(nullCell);
        XlsxCalculationChainPartWriteResult missingReferenceResult = writer.WriteBytes(missingReference);
        XlsxCalculationChainPartWriteResult invalidReferenceResult = writer.WriteBytes(invalidReference);
        XlsxCalculationChainPartWriteResult nullUnknownsResult = writer.WriteBytes(nullUnknowns);

        Assert.That(HasDiagnostic(nullPartResult, "calculationChain.partNull"), Is.True);
        Assert.That(HasDiagnostic(nullCellsResult, "calculationChain.cellsNull"), Is.True);
        Assert.That(HasDiagnostic(nullCellResult, "calculationChain.cellNull"), Is.True);
        Assert.That(HasDiagnostic(missingReferenceResult, "calculationChain.cellReferenceMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidReferenceResult, "calculationChain.cellReferenceInvalid"), Is.True);
        Assert.That(HasDiagnostic(nullUnknownsResult, "calculationChain.cellUnknownAttributesNull"), Is.True);
    }

    static XlsxCalculationChainCell CreateCell(string reference, uint sheetId, bool hasSheetId, bool inChildChain, bool newDependencyLevel, bool newThread, bool array)
    {
        XlsxCalculationChainCell cell = new XlsxCalculationChainCell();
        cell.Reference = reference;
        cell.SheetId = sheetId;
        cell.HasSheetId = hasSheetId;
        cell.InChildChain = inChildChain;
        cell.HasInChildChain = true;
        cell.NewDependencyLevel = newDependencyLevel;
        cell.HasNewDependencyLevel = true;
        cell.NewThread = newThread;
        cell.HasNewThread = true;
        cell.Array = array;
        cell.HasArray = true;
        return cell;
    }

    static bool HasDiagnostic(XlsxCalculationChainPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxCalculationChainPartWriteResult result, string code)
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
