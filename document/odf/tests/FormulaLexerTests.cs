namespace odf.Tests;

public class FormulaLexerTests
{
    [Test]
    public void TokenizesLiteralsFunctionsSeparatorsAndPreservedWhitespace()
    {
        string formula = "of:=SUM(12.5E-2; 25%; \"a\"\"b\"; TRUE; FALSE)";

        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(item => item.Kind), Is.EqualTo(new[]
            {
                OdfFormulaTokenKind.Prefix,
                OdfFormulaTokenKind.Function,
                OdfFormulaTokenKind.LeftParenthesis,
                OdfFormulaTokenKind.Number,
                OdfFormulaTokenKind.Separator,
                OdfFormulaTokenKind.Whitespace,
                OdfFormulaTokenKind.Percentage,
                OdfFormulaTokenKind.Separator,
                OdfFormulaTokenKind.Whitespace,
                OdfFormulaTokenKind.String,
                OdfFormulaTokenKind.Separator,
                OdfFormulaTokenKind.Whitespace,
                OdfFormulaTokenKind.Boolean,
                OdfFormulaTokenKind.Separator,
                OdfFormulaTokenKind.Whitespace,
                OdfFormulaTokenKind.Boolean,
                OdfFormulaTokenKind.RightParenthesis,
                OdfFormulaTokenKind.End
            }));
            Assert.That(tokens[0].Text, Is.EqualTo("of:="));
            Assert.That(tokens[3].Text, Is.EqualTo("12.5E-2"));
            Assert.That(tokens[6].Text, Is.EqualTo("25%"));
            Assert.That(tokens[9].Text, Is.EqualTo("\"a\"\"b\""));
            Assert.That(Reconstruct(tokens), Is.EqualTo(formula));
        });
    }

    [Test]
    public void DistinguishesCellRangeExternalAndReferenceOperators()
    {
        string formula =
            "=[.A1]:[.$B$2:.C3]~['file.ods'#Sheet1.D4]![#REF!]";

        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(item => item.Kind), Is.EqualTo(new[]
            {
                OdfFormulaTokenKind.Prefix,
                OdfFormulaTokenKind.CellReference,
                OdfFormulaTokenKind.Operator,
                OdfFormulaTokenKind.RangeReference,
                OdfFormulaTokenKind.Operator,
                OdfFormulaTokenKind.ExternalReference,
                OdfFormulaTokenKind.Operator,
                OdfFormulaTokenKind.CellReference,
                OdfFormulaTokenKind.End
            }));
            Assert.That(tokens[1].Text, Is.EqualTo("[.A1]"));
            Assert.That(tokens[3].Text, Is.EqualTo("[.$B$2:.C3]"));
            Assert.That(tokens[5].Text,
                Is.EqualTo("['file.ods'#Sheet1.D4]"));
            Assert.That(tokens[7].Text, Is.EqualTo("[#REF!]"));
        });
    }

    [Test]
    public void TokenizesInlineArraysNamedExpressionsAndQuotedLabels()
    {
        string formula =
            "={1;2|3;4}+Tax+$$'Gross Profit'+'Sheet 1'.$$Local+'Row Label'";

        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Count(
                item => item.Kind == OdfFormulaTokenKind.NamedExpression),
                Is.EqualTo(3));
            Assert.That(tokens.Count(
                item => item.Kind == OdfFormulaTokenKind.Identifier),
                Is.EqualTo(1));
            Assert.That(tokens.Count(
                item => item.Kind == OdfFormulaTokenKind.Separator),
                Is.EqualTo(2));
            Assert.That(tokens.Count(
                item => item.Kind == OdfFormulaTokenKind.ArrayRowSeparator),
                Is.EqualTo(1));
            Assert.That(tokens.Any(
                item => item.Kind == OdfFormulaTokenKind.LeftBrace), Is.True);
            Assert.That(tokens.Any(
                item => item.Kind == OdfFormulaTokenKind.RightBrace), Is.True);
            Assert.That(tokens.Single(
                item => item.Kind == OdfFormulaTokenKind.Identifier).Text,
                Is.EqualTo("'Row Label'"));
            Assert.That(Reconstruct(tokens), Is.EqualTo(formula));
        });
    }

    [Test]
    public void RecognizesStandardAndImplementationDefinedErrors()
    {
        string formula = "=#DIV/0!+#NAME?+#N/A+#CUSTOM!";

        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);
        string[] errors = tokens
            .Where(item => item.Kind == OdfFormulaTokenKind.Error)
            .Select(item => item.Text)
            .ToArray();

        Assert.That(errors, Is.EqualTo(new[]
        {
            "#DIV/0!",
            "#NAME?",
            "#N/A",
            "#CUSTOM!"
        }));
    }

    [Test]
    public void ReportsExactUtf16TokenSpans()
    {
        string formula = "=SÜM(éclair;A\u0301rea)";

        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);

        Assert.Multiple(() =>
        {
            Assert.That(tokens[1].Kind, Is.EqualTo(OdfFormulaTokenKind.Function));
            Assert.That(tokens[1].Text, Is.EqualTo("SÜM"));
            Assert.That(tokens[1].Start, Is.EqualTo(1));
            Assert.That(tokens[1].Length, Is.EqualTo(3));
            Assert.That(tokens[5].Text, Is.EqualTo("A\u0301rea"));
            Assert.That(tokens[^1].Start, Is.EqualTo(formula.Length));
            Assert.That(tokens[^1].Length, Is.Zero);
        });
    }

    [TestCase("=\"unterminated")]
    [TestCase("=[.A1")]
    [TestCase("=#BAD")]
    [TestCase("=\"")]
    public void EmitsUnknownTokensForMalformedLexemes(string formula)
    {
        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);

        Assert.That(tokens.Any(
            item => item.Kind == OdfFormulaTokenKind.Unknown), Is.True);
    }

    [Test]
    public void RejectsNullInput()
    {
        Assert.That(
            () => FormulaLexer.Tokenize(null!),
            Throws.TypeOf<OdfFormulaException>());
    }

    private static string Reconstruct(OdfFormulaToken[] tokens)
    {
        return string.Concat(tokens
            .Where(item => item.Kind != OdfFormulaTokenKind.End)
            .Select(item => item.Text));
    }
}
