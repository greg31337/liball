using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaParserTests
{
    [Test]
    public void Parse_RecognizesFunctionsRangesStructuredReferencesAndNames()
    {
        XlsxFormulaParseResult result = new XlsxFormulaParser().Parse("SUM(A1:B2,Table1[Column],TaxRate)");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasRoot, Is.True);
        AssertNode(result.Root, XlsxFormulaNodeKind.FunctionCall, "SUM(A1:B2,Table1[Column],TaxRate)", "SUM");
        Assert.That(result.Root.Children.Count, Is.EqualTo(3));
        AssertNode(result.Root.Children[0], XlsxFormulaNodeKind.Range, "A1:B2", ":");
        AssertNode(result.Root.Children[0].Children[0], XlsxFormulaNodeKind.Reference, "A1", "");
        AssertNode(result.Root.Children[0].Children[1], XlsxFormulaNodeKind.Reference, "B2", "");
        AssertNode(result.Root.Children[1], XlsxFormulaNodeKind.StructuredReference, "Table1[Column]", "");
        AssertNode(result.Root.Children[2], XlsxFormulaNodeKind.Name, "TaxRate", "");
    }

    [Test]
    public void Parse_RecognizesSheetAndExternalReferences()
    {
        XlsxFormulaParseResult result = new XlsxFormulaParser().Parse("'Sheet 1'!A1+[Book.xlsx]Sheet2!$B$2");

        Assert.That(result.IsValid, Is.True);
        AssertNode(result.Root, XlsxFormulaNodeKind.BinaryOperation, "'Sheet 1'!A1+[Book.xlsx]Sheet2!$B$2", "+");
        AssertNode(result.Root.Children[0], XlsxFormulaNodeKind.Reference, "'Sheet 1'!A1", "");
        AssertNode(result.Root.Children[1], XlsxFormulaNodeKind.ExternalReference, "[Book.xlsx]Sheet2!$B$2", "");
    }

    [Test]
    public void Parse_RecognizesLiteralsOperatorsPrecedenceAndArrays()
    {
        XlsxFormulaParseResult arithmetic = new XlsxFormulaParser().Parse("A1+2*B1^3");
        XlsxFormulaParseResult array = new XlsxFormulaParser().Parse("{1,2;TRUE,#N/A}");

        Assert.That(arithmetic.IsValid, Is.True);
        AssertNode(arithmetic.Root, XlsxFormulaNodeKind.BinaryOperation, "A1+2*B1^3", "+");
        AssertNode(arithmetic.Root.Children[0], XlsxFormulaNodeKind.Reference, "A1", "");
        AssertNode(arithmetic.Root.Children[1], XlsxFormulaNodeKind.BinaryOperation, "2*B1^3", "*");
        AssertNode(arithmetic.Root.Children[1].Children[1], XlsxFormulaNodeKind.BinaryOperation, "B1^3", "^");
        Assert.That(array.IsValid, Is.True);
        AssertNode(array.Root, XlsxFormulaNodeKind.ArrayConstant, "{1,2;TRUE,#N/A}", "");
        Assert.That(array.Root.Children.Count, Is.EqualTo(4));
        AssertNode(array.Root.Children[0], XlsxFormulaNodeKind.Literal, "1", "");
        AssertNode(array.Root.Children[2], XlsxFormulaNodeKind.Literal, "TRUE", "");
        AssertNode(array.Root.Children[3], XlsxFormulaNodeKind.Literal, "#N/A", "");
    }

    [Test]
    public void Parse_ReportsInvalidInputsAndIncompleteSyntax()
    {
        XlsxFormulaParseResult? nullFormula = new XlsxFormulaParser().Parse(null!);
        XlsxFormulaParseResult empty = new XlsxFormulaParser().Parse("");
        XlsxFormulaParseResult missingClose = new XlsxFormulaParser().Parse("SUM(A1");
        XlsxFormulaParseResult badArray = new XlsxFormulaParser().Parse("{1 2}");

        Assert.That(nullFormula.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullFormula, "formulaTokenizer.formulaNull"), Is.True);
        Assert.That(empty.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "formulaParser.formulaEmpty"), Is.True);
        Assert.That(missingClose.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingClose, "formulaParser.closeParenMissing"), Is.True);
        Assert.That(badArray.IsValid, Is.False);
        Assert.That(HasDiagnostic(badArray, "formulaParser.arraySeparatorMissing"), Is.True);
    }

    static void AssertNode(XlsxFormulaNode node, XlsxFormulaNodeKind kind, string text, string op)
    {
        Assert.That(node.Kind, Is.EqualTo(kind));
        Assert.That(node.Text, Is.EqualTo(text));
        Assert.That(node.Operator, Is.EqualTo(op));
        Assert.That(node.Length, Is.EqualTo(text.Length));
    }

    static bool HasDiagnostic(XlsxFormulaParseResult result, string code)
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
