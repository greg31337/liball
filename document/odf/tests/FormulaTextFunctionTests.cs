namespace odf.Tests;

public class FormulaTextFunctionTests
{
    [Test]
    public void PublishesCompleteTextFunctionMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions,
                Has.Length.EqualTo(393));
            Assert.That(FormulaFunctionCatalog.Functions.Count(
                item => item.Group == "Text"), Is.EqualTo(33));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.LEFT)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.CHAR)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.UNICHAR)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.DOLLAR)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Unknown));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.CONCATENATE)!
                .MaximumArguments, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void CountsAndSlicesUnicodeCodePoints()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult value = Text("A😀é");

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.LEN,
                [value], context).NumericValue, Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.LEFT,
                [value, Number(2)], context).StringValue, Is.EqualTo("A😀"));
            Assert.That(Evaluate(OdfFormulaNames.RIGHT,
                [value, Number(2)], context).StringValue, Is.EqualTo("😀é"));
            Assert.That(Evaluate(OdfFormulaNames.MID,
                [value, Number(2), Number(1)], context).StringValue,
                Is.EqualTo("😀"));
            Assert.That(Evaluate(OdfFormulaNames.FIND,
                [Text("😀"), Text("a😀b")], context).NumericValue,
                Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.SEARCH,
                [Text("É"), Text("aéb")], context).NumericValue,
                Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.REPLACE,
                [Text("A😀C"), Number(2), Number(1), Text("B")], context)
                .StringValue, Is.EqualTo("ABC"));
        });
    }

    [Test]
    public void ConvertsUnicodeCharactersAndLegacyCharacterCodes()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult unicode = Evaluate(OdfFormulaNames.UNICHAR,
            [Number(0x1f642)], context);
        OdfFormulaResult legacy = Evaluate(OdfFormulaNames.CHAR,
            [Number(233)], context);

        Assert.Multiple(() =>
        {
            Assert.That(unicode.StringValue, Is.EqualTo("🙂"));
            Assert.That(Evaluate(OdfFormulaNames.UNICODE,
                [unicode], context).NumericValue, Is.EqualTo(0x1f642));
            Assert.That(legacy.StringValue, Is.EqualTo("é"));
            Assert.That(Evaluate(OdfFormulaNames.CODE,
                [legacy], context).NumericValue, Is.EqualTo(233));
            Assert.That(Evaluate(OdfFormulaNames.UNICHAR,
                [Number(0xd800)], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
            Assert.That(Evaluate(OdfFormulaNames.UNICODE,
                [Text("")], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void UsesUtf8ForBytePositionFunctions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult value = Text("Aé😀Z");

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.LENB,
                [value], context).NumericValue, Is.EqualTo(8));
            Assert.That(Evaluate(OdfFormulaNames.FINDB,
                [Text("😀"), value], context).NumericValue, Is.EqualTo(4));
            Assert.That(Evaluate(OdfFormulaNames.LEFTB,
                [value, Number(3)], context).StringValue, Is.EqualTo("Aé"));
            Assert.That(Evaluate(OdfFormulaNames.RIGHTB,
                [value, Number(5)], context).StringValue, Is.EqualTo("😀Z"));
            Assert.That(Evaluate(OdfFormulaNames.MIDB,
                [value, Number(4), Number(4)], context).StringValue,
                Is.EqualTo("😀"));
            Assert.That(Evaluate(OdfFormulaNames.REPLACEB,
                [value, Number(4), Number(4), Text("X")], context).StringValue,
                Is.EqualTo("AéXZ"));
            Assert.That(Evaluate(OdfFormulaNames.LEFTB,
                [value, Number(2)], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void AppliesCaseCleanupWhitespaceAndWidthConversions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult fullWidth = Evaluate(OdfFormulaNames.JIS,
            [Text("ABC ｶﾞﾊﾟ")], context);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.LOWER,
                [Text("ÄBC")], context).StringValue, Is.EqualTo("äbc"));
            Assert.That(Evaluate(OdfFormulaNames.UPPER,
                [Text("äbc")], context).StringValue, Is.EqualTo("ÄBC"));
            Assert.That(Evaluate(OdfFormulaNames.PROPER,
                [Text("hELLO-world 42FOO")], context).StringValue,
                Is.EqualTo("Hello-World 42Foo"));
            Assert.That(Evaluate(OdfFormulaNames.CLEAN,
                [Text("a\u0001b\u0378c")], context).StringValue,
                Is.EqualTo("abc"));
            Assert.That(Evaluate(OdfFormulaNames.TRIM,
                [Text(" \t alpha\n beta \r")], context).StringValue,
                Is.EqualTo("alpha beta"));
            Assert.That(Evaluate(OdfFormulaNames.EXACT,
                [Text("Case"), Text("case")], context).BooleanValue, Is.False);
            Assert.That(fullWidth.StringValue, Is.EqualTo("ＡＢＣ ガパ"));
            Assert.That(Evaluate(OdfFormulaNames.ASC,
                [fullWidth], context).StringValue, Is.EqualTo("ABC ｶﾞﾊﾟ"));
        });
    }

    [Test]
    public void ReplacesRepeatsConcatenatesAndSelectsText()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.SUBSTITUTE,
                [Text("one two one"), Text("one"), Text("1")], context)
                .StringValue, Is.EqualTo("1 two 1"));
            Assert.That(Evaluate(OdfFormulaNames.SUBSTITUTE,
                [Text("one two one"), Text("one"), Text("1"), Number(2)],
                context).StringValue, Is.EqualTo("one two 1"));
            Assert.That(Evaluate(OdfFormulaNames.REPT,
                [Text("ab"), Number(3)], context).StringValue,
                Is.EqualTo("ababab"));
            Assert.That(Evaluate(OdfFormulaNames.REPLACE,
                [Text("abc"), Number(10), Number(1), Text("X")], context)
                .StringValue, Is.EqualTo("abX"));
            Assert.That(Evaluate(OdfFormulaNames.CONCATENATE,
                [Text("value="), Number(12), Boolean(true)], context)
                .StringValue, Is.EqualTo("value=12TRUE"));
            Assert.That(Evaluate(OdfFormulaNames.T,
                [Text("kept")], context).StringValue, Is.EqualTo("kept"));
            Assert.That(Evaluate(OdfFormulaNames.T,
                [Number(12)], context).StringValue, Is.Empty);
            Assert.That(Evaluate(OdfFormulaNames.REPT,
                [Text("x"), Number(-1)], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void SearchesWithUnicodeWildcardsAndFormatsNumbers()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.SpreadsheetSettings.UseRegularExpressions = false;
        context.SpreadsheetSettings.UseWildcards = true;
        FormulaFunctionContext regexContext = new FormulaFunctionContext();

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.SEARCH,
                [Text("b?d"), Text("xxB😀dyy")], context).NumericValue,
                Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.SEARCH,
                [Text("b~?d"), Text("xxB?dyy")], context).NumericValue,
                Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.SEARCH,
                [Text("b.d"), Text("xxB😀dyy")], regexContext).NumericValue,
                Is.EqualTo(3));
            Assert.That(Evaluate(OdfFormulaNames.FIXED,
                [Number(12345.678), Number(2)], context).StringValue,
                Is.EqualTo("12,345.68"));
            Assert.That(Evaluate(OdfFormulaNames.FIXED,
                [Number(12345.678), Number(2), Boolean(true)], context)
                .StringValue, Is.EqualTo("12345.68"));
            Assert.That(Evaluate(OdfFormulaNames.DOLLAR,
                [Number(-1234.5), Number(2)], context).StringValue,
                Is.EqualTo("-$1,234.50"));
            Assert.That(Evaluate(OdfFormulaNames.TEXT,
                [Number(0.256), Text("0.0%")], context).StringValue,
                Is.EqualTo("25.6%"));
            Assert.That(Evaluate(OdfFormulaNames.TEXT,
                [Number(1234.5), Text("#,##0.00")], context).StringValue,
                Is.EqualTo("1,234.50"));
        });
    }

    static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, arguments, context);
    }

    static OdfFormulaResult Text(string value)
    {
        return FormulaValueModel.CreateText(value);
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult Boolean(bool value)
    {
        return FormulaValueModel.CreateBoolean(value);
    }
}
