using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRangeTransposeTests
{
    [Test]
    public void Transpose_SwapsSquareRangeInPlace()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "a1");
        part.SetTextValue("B1", "b1");
        part.SetTextValue("A2", "a2");
        part.SetFormulaCachedValue("B2", "A1+B1", "3", "n");
        part.GetCell("A2")!.Style = 8u;
        part.GetCell("A2")!.HasStyle = true;

        XlsxWorksheetRangeResult result = part.GetRange("A1:B2").Transpose();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TransposedCellCount, Is.EqualTo(4));
        AssertCell(part.GetCell("A1"), "str", "a1", "", false);
        AssertCell(part.GetCell("B1"), "str", "a2", "", false);
        AssertCell(part.GetCell("A2"), "str", "b1", "", false);
        AssertCell(part.GetCell("B2"), "n", "3", "A1+B1", true);
        Assert.That(part.GetCell("B1")!.Style, Is.EqualTo(8u));
        Assert.That(part.GetCell("B1")!.Reference, Is.EqualTo("B1"));
    }

    [Test]
    public void TransposeTo_CopiesRectangularRangeToTransposedTarget()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "a1");
        part.SetTextValue("B1", "b1");
        part.SetTextValue("C1", "c1");
        part.SetTextValue("A2", "a2");
        part.SetTextValue("B2", "b2");
        part.SetTextValue("C2", "c2");

        XlsxWorksheetRangeResult result = part.GetRange("A1:C2").TransposeTo("E1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TransposedCellCount, Is.EqualTo(6));
        Assert.That(part.GetCell("E1")!.Value, Is.EqualTo("a1"));
        Assert.That(part.GetCell("F1")!.Value, Is.EqualTo("a2"));
        Assert.That(part.GetCell("E2")!.Value, Is.EqualTo("b1"));
        Assert.That(part.GetCell("F2")!.Value, Is.EqualTo("b2"));
        Assert.That(part.GetCell("E3")!.Value, Is.EqualTo("c1"));
        Assert.That(part.GetCell("F3")!.Value, Is.EqualTo("c2"));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("a1"));
        Assert.That(part.GetCell("C2")!.Value, Is.EqualTo("c2"));
    }

    [Test]
    public void TransposeOperations_ReportInvalidInputs()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult notSquare = part.GetRange("A1:B3").Transpose();
        XlsxWorksheetRangeResult badTarget = part.GetRange("A1:B2").TransposeTo("XFD1048576");
        XlsxWorksheetRangeResult badRange = part.GetRange("bad").Transpose();

        Assert.That(notSquare.IsValid, Is.False);
        Assert.That(badTarget.IsValid, Is.False);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(notSquare, "worksheetRange.transposeRequiresSquareRange"), Is.True);
        Assert.That(HasDiagnostic(badTarget, "worksheetRange.transposeTargetOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(badRange, "worksheetRange.referenceInvalid"), Is.True);
    }

    static void AssertCell(XlsxWorksheetCell? cell, string type, string value, string formula, bool hasFormula)
    {
        Assert.That(cell, Is.Not.Null);
        Assert.That(cell!.Type, Is.EqualTo(type));
        Assert.That(cell.Value, Is.EqualTo(value));
        Assert.That(cell.Formula, Is.EqualTo(formula));
        Assert.That(cell.HasFormula, Is.EqualTo(hasFormula));
    }

    static bool HasDiagnostic(XlsxWorksheetRangeResult result, string code)
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
