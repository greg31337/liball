using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaDependencyGraphTests
{
    [Test]
    public void BuildFormulaDependencyGraph_RecordsPrecedentsAndDependents()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        row.Cells.Add(CreateValueCell("A1", "1"));
        row.Cells.Add(CreateFormulaCell("B1", "A1*2"));
        row.Cells.Add(CreateFormulaCell("C1", "SUM(A1:B1)"));
        part.Rows.Add(row);

        XlsxFormulaDependencyGraphResult result = part.BuildFormulaDependencyGraph();
        IXlsxFormulaDependencyGraphResultOperations operations = new XlsxFormulaDependencyGraphResultOperations();

        Assert.That(result.IsValid, Is.True);
        Assert.That(operations.GetNode(result, "B1")!.Formula, Is.EqualTo("A1*2"));
        Assert.That(operations.GetNode(result, "B1")!.Precedents.Count, Is.EqualTo(1));
        Assert.That(operations.GetNode(result, "B1")!.Precedents[0], Is.EqualTo("A1"));
        Assert.That(operations.GetNode(result, "C1")!.Precedents.Count, Is.EqualTo(2));
        Assert.That(ContainsText(operations.GetNode(result, "C1")!.Precedents, "A1"), Is.True);
        Assert.That(ContainsText(operations.GetNode(result, "C1")!.Precedents, "B1"), Is.True);
        Assert.That(ContainsText(operations.GetNode(result, "A1")!.Dependents, "B1"), Is.True);
        Assert.That(ContainsText(operations.GetNode(result, "A1")!.Dependents, "C1"), Is.True);
        Assert.That(ContainsText(operations.GetNode(result, "B1")!.Dependents, "C1"), Is.True);
    }

    [Test]
    public void BuildFormulaDependencyGraph_RecordsNamesStructuredAndExternalReferences()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        row.Cells.Add(CreateFormulaCell("D1", "TaxRate+Table1[Amount]+'Other Sheet'!A1+[Book.xlsx]Sheet1!B2"));
        part.Rows.Add(row);

        XlsxFormulaDependencyGraphResult result = part.BuildFormulaDependencyGraph();
        XlsxFormulaDependencyNode? node = result.GetNode("D1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(ContainsText(node!.Names, "TaxRate"), Is.True);
        Assert.That(ContainsText(node.Names, "Table1[Amount]"), Is.True);
        Assert.That(ContainsText(node.ExternalPrecedents, "'Other Sheet'!A1"), Is.True);
        Assert.That(ContainsText(node.ExternalPrecedents, "[Book.xlsx]Sheet1!B2"), Is.True);
        Assert.That(node.Precedents.Count, Is.EqualTo(0));
    }

    [Test]
    public void BuildFormulaDependencyGraph_ExpandsMultiCellRanges()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        row.Cells.Add(CreateFormulaCell("E1", "SUM(A1:B2)"));
        part.Rows.Add(row);

        XlsxFormulaDependencyGraphResult result = part.BuildFormulaDependencyGraph();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.GetNode("E1")!.Precedents.Count, Is.EqualTo(4));
        Assert.That(ContainsText(result.GetNode("E1")!.Precedents, "A1"), Is.True);
        Assert.That(ContainsText(result.GetNode("E1")!.Precedents, "B1"), Is.True);
        Assert.That(ContainsText(result.GetNode("E1")!.Precedents, "A2"), Is.True);
        Assert.That(ContainsText(result.GetNode("E1")!.Precedents, "B2"), Is.True);
        Assert.That(ContainsText(result.GetNode("A2")!.Dependents, "E1"), Is.True);
    }

    [Test]
    public void BuildFormulaDependencyGraph_ReportsFormulaParseDiagnostics()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        row.Cells.Add(CreateFormulaCell("A1", "SUM(B1"));
        part.Rows.Add(row);

        XlsxFormulaDependencyGraphResult result = part.BuildFormulaDependencyGraph();

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "formulaParser.closeParenMissing"), Is.True);
        Assert.That(result.GetNode("A1"), Is.Not.Null);
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        return row;
    }

    static XlsxWorksheetCell CreateFormulaCell(string reference, string formula)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Formula = formula;
        cell.FormulaInfo = XlsxCellFormula.CreateNormal(formula);
        cell.HasFormula = true;
        return cell;
    }

    static XlsxWorksheetCell CreateValueCell(string reference, string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Type = "n";
        cell.Value = value;
        return cell;
    }

    static bool ContainsText(List<string> items, string text)
    {
        foreach (string item in items)
        {
            if (item == text)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxFormulaDependencyGraphResult result, string code)
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
