namespace odf.Tests;

public class FormulaFormatterTests
{
    [Test]
    public void PreservationFormattingReturnsExactOriginalSource()
    {
        string source =
            "of:=  SUM ( 1 ; \"a\"\"b\" ; ( 2 + 3 ) )  ";
        OdfFormula formula = FormulaParser.Parse(source, 32);

        string formatted = FormulaFormatter.FormatPreserved(formula);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formatted, Is.EqualTo(source));
            Assert.That(formula.Root!.Children[1].Value, Is.EqualTo("a\"b"));
        });
    }

    [Test]
    public void CanonicalFormattingRemovesTriviaAndRetainsRequiredParentheses()
    {
        OdfFormula formula = FormulaParser.Parse(
            "of:= 1 + 2 * ( 3 + 4 )",
            32);

        string formatted = FormulaFormatter.FormatCanonical(formula);

        Assert.That(formatted, Is.EqualTo("of:=1+2*(3+4)"));
    }

    [Test]
    public void EscapesDecodesAndEditsFormulaStrings()
    {
        string escaped = FormulaFormatter.EscapeString("He said \"Hi\"");
        string decoded = FormulaFormatter.UnescapeString(escaped);
        OdfFormula formula = FormulaParser.Parse("of:=\"a\"\"b\"", 32);
        formula.Root!.Value = "c\"d";

        string edited = FormulaFormatter.FormatPreserved(formula);
        OdfFormula reread = FormulaParser.Parse(edited, 32);

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("\"He said \"\"Hi\"\"\""));
            Assert.That(decoded, Is.EqualTo("He said \"Hi\""));
            Assert.That(edited, Is.EqualTo("of:=\"c\"\"d\""));
            Assert.That(reread.IsValid, Is.True);
            Assert.That(reread.Root!.Value, Is.EqualTo("c\"d"));
        });
    }

    [Test]
    public void PreservesInlineArrayRowSeparatorsInCanonicalOutput()
    {
        OdfFormula formula = FormulaParser.Parse("={ 1 ; 2 | 3 ; 4 }", 32);

        string formatted = FormulaFormatter.FormatCanonical(formula);

        Assert.Multiple(() =>
        {
            Assert.That(formula.Root!.Separators,
                Is.EqualTo(new[] { ";", "|", ";" }));
            Assert.That(formatted, Is.EqualTo("={1;2|3;4}"));
        });
    }

    [Test]
    public void CanonicalizesFunctionCaseAndEmptyArguments()
    {
        OdfFormula formula = FormulaParser.Parse(
            "acme:=com.microsoft.foo( ; tax ; )",
            32);

        string formatted = FormulaFormatter.FormatCanonical(formula);

        Assert.Multiple(() =>
        {
            Assert.That(formula.Root!.Separators,
                Is.EqualTo(new[] { ";", ";" }));
            Assert.That(formatted,
                Is.EqualTo("acme:=COM.MICROSOFT.FOO(;tax;)"));
        });
    }

    [TestCase("of:=SUM(1;2*3)")]
    [TestCase("==-(2+3)%")]
    [TestCase("=[.A1]:[.B2]![.B1]~[.C1]")]
    [TestCase("={1;\"a\"\"b\"|TRUE;#N/A}")]
    [TestCase("='Row Label'!!'Column Label'")]
    public void CanonicalFormattingIsStableAcrossParseRoundTrips(string source)
    {
        OdfFormula first = FormulaParser.Parse(source, 32);
        string firstCanonical = FormulaFormatter.FormatCanonical(first);
        OdfFormula second = FormulaParser.Parse(firstCanonical, 32);
        string secondCanonical = FormulaFormatter.FormatCanonical(second);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.True);
            Assert.That(second.IsValid, Is.True);
            Assert.That(secondCanonical, Is.EqualTo(firstCanonical));
        });
    }

    [Test]
    public void CanonicalFormattingReflectsAstEdits()
    {
        OdfFormula formula = FormulaParser.Parse("=1+2*3", 32);
        formula.Root!.Text = "*";

        string formatted = FormulaFormatter.FormatPreserved(formula);

        Assert.That(formatted, Is.EqualTo("=1*(2*3)"));
    }

    [Test]
    public void RejectsInvalidFormatterInputsAndStringLexemes()
    {
        OdfFormula empty = new OdfFormula();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => FormulaFormatter.FormatPreserved(null!),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => FormulaFormatter.FormatCanonical(empty),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => FormulaFormatter.EscapeString(null!),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => FormulaFormatter.UnescapeString("\"bad\"quote\""),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => FormulaFormatter.EscapeString("bad\0value"),
                Throws.TypeOf<OdfFormulaException>());
        });
    }
}
