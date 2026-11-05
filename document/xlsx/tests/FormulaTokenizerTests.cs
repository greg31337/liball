using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaTokenizerTests
{
    [Test]
    public void Tokenize_RecognizesFunctionsReferencesStructuredReferencesAndStrings()
    {
        XlsxFormulaTokenizeResult result = new XlsxFormulaTokenizer().Tokenize("SUM($A$1:B2,Table1[[#All],[Column 1]])+\"x\"\"y\"");

        Assert.That(result.IsValid, Is.True);
        AssertToken(result, 0, XlsxFormulaTokenKind.Identifier, "SUM", 0);
        AssertToken(result, 1, XlsxFormulaTokenKind.OpenParen, "(", 3);
        AssertToken(result, 2, XlsxFormulaTokenKind.CellReference, "$A$1", 4);
        AssertToken(result, 3, XlsxFormulaTokenKind.RangeOperator, ":", 8);
        AssertToken(result, 4, XlsxFormulaTokenKind.CellReference, "B2", 9);
        AssertToken(result, 5, XlsxFormulaTokenKind.UnionOperator, ",", 11);
        AssertToken(result, 6, XlsxFormulaTokenKind.Identifier, "Table1", 12);
        AssertToken(result, 7, XlsxFormulaTokenKind.StructuredReference, "[[#All],[Column 1]]", 18);
        AssertToken(result, 8, XlsxFormulaTokenKind.CloseParen, ")", 37);
        AssertToken(result, 9, XlsxFormulaTokenKind.Operator, "+", 38);
        AssertToken(result, 10, XlsxFormulaTokenKind.String, "\"x\"\"y\"", 39);
    }

    [Test]
    public void Tokenize_RecognizesQuotedSheetsExternalReferencesArraysErrorsAndBooleans()
    {
        string formula = "'O''Brien Sheet'!A1 + [Book1.xlsx]Sheet2!$B$2 + {1,2;TRUE,#DIV/0!}";

        XlsxFormulaTokenizeResult result = new XlsxFormulaTokenizer().Tokenize(formula);

        Assert.That(result.IsValid, Is.True);
        AssertToken(result, 0, XlsxFormulaTokenKind.SheetName, "'O''Brien Sheet'", 0);
        AssertToken(result, 1, XlsxFormulaTokenKind.Bang, "!", 16);
        AssertToken(result, 2, XlsxFormulaTokenKind.CellReference, "A1", 17);
        AssertToken(result, 3, XlsxFormulaTokenKind.Whitespace, " ", 19);
        AssertToken(result, 4, XlsxFormulaTokenKind.Operator, "+", 20);
        AssertToken(result, 6, XlsxFormulaTokenKind.StructuredReference, "[Book1.xlsx]", 22);
        AssertToken(result, 7, XlsxFormulaTokenKind.Identifier, "Sheet2", 34);
        AssertToken(result, 8, XlsxFormulaTokenKind.Bang, "!", 40);
        AssertToken(result, 9, XlsxFormulaTokenKind.CellReference, "$B$2", 41);
        AssertToken(result, 13, XlsxFormulaTokenKind.ArrayOpen, "{", 48);
        AssertToken(result, 14, XlsxFormulaTokenKind.Number, "1", 49);
        AssertToken(result, 15, XlsxFormulaTokenKind.UnionOperator, ",", 50);
        AssertToken(result, 16, XlsxFormulaTokenKind.Number, "2", 51);
        AssertToken(result, 17, XlsxFormulaTokenKind.ArgumentSeparator, ";", 52);
        AssertToken(result, 18, XlsxFormulaTokenKind.Boolean, "TRUE", 53);
        AssertToken(result, 19, XlsxFormulaTokenKind.UnionOperator, ",", 57);
        AssertToken(result, 20, XlsxFormulaTokenKind.Error, "#DIV/0!", 58);
        AssertToken(result, 21, XlsxFormulaTokenKind.ArrayClose, "}", 65);
    }

    [Test]
    public void Tokenize_RecognizesNumbersWhitespacePercentAndComparisonOperators()
    {
        XlsxFormulaTokenizeResult result = new XlsxFormulaTokenizer().Tokenize("A1 >= 1.25E-3 * 50%");

        Assert.That(result.IsValid, Is.True);
        AssertToken(result, 0, XlsxFormulaTokenKind.CellReference, "A1", 0);
        AssertToken(result, 1, XlsxFormulaTokenKind.Whitespace, " ", 2);
        AssertToken(result, 2, XlsxFormulaTokenKind.Operator, ">=", 3);
        AssertToken(result, 4, XlsxFormulaTokenKind.Number, "1.25E-3", 6);
        AssertToken(result, 6, XlsxFormulaTokenKind.Operator, "*", 14);
        AssertToken(result, 8, XlsxFormulaTokenKind.Number, "50", 16);
        AssertToken(result, 9, XlsxFormulaTokenKind.Percent, "%", 18);
    }

    [Test]
    public void Tokenize_ReportsInvalidInputsAndUnterminatedLiterals()
    {
        XlsxFormulaTokenizeResult nullFormula = new XlsxFormulaTokenizer().Tokenize(null);
        XlsxFormulaTokenizeResult badString = new XlsxFormulaTokenizer().Tokenize("\"unterminated");
        XlsxFormulaTokenizeResult badSheet = new XlsxFormulaTokenizer().Tokenize("'Sheet 1");
        XlsxFormulaTokenizeResult badStructuredReference = new XlsxFormulaTokenizer().Tokenize("Table1[[Column]");
        XlsxFormulaTokenizeResult badCharacter = new XlsxFormulaTokenizer().Tokenize("A1~B1");

        Assert.That(nullFormula.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullFormula, "formulaTokenizer.formulaNull"), Is.True);
        Assert.That(badString.IsValid, Is.False);
        Assert.That(HasDiagnostic(badString, "formulaTokenizer.stringUnterminated"), Is.True);
        Assert.That(badSheet.IsValid, Is.False);
        Assert.That(HasDiagnostic(badSheet, "formulaTokenizer.sheetNameUnterminated"), Is.True);
        Assert.That(badStructuredReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(badStructuredReference, "formulaTokenizer.structuredReferenceUnterminated"), Is.True);
        Assert.That(badCharacter.IsValid, Is.False);
        Assert.That(HasDiagnostic(badCharacter, "formulaTokenizer.characterUnsupported"), Is.True);
        Assert.That(badCharacter.Tokens[1].Kind, Is.EqualTo(XlsxFormulaTokenKind.Unknown));
    }

    static void AssertToken(XlsxFormulaTokenizeResult result, int index, XlsxFormulaTokenKind kind, string text, int start)
    {
        Assert.That(result.Tokens[index].Kind, Is.EqualTo(kind));
        Assert.That(result.Tokens[index].Text, Is.EqualTo(text));
        Assert.That(result.Tokens[index].Start, Is.EqualTo(start));
        Assert.That(result.Tokens[index].Length, Is.EqualTo(text.Length));
    }

    static bool HasDiagnostic(XlsxFormulaTokenizeResult result, string code)
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
