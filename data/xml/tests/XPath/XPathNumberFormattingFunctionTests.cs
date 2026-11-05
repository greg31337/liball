using xml;

namespace xml.Tests;

[TestFixture]
public class XPathNumberFormattingFunctionTests
{
    [Test]
    public void FormatsIntegersWithPaddingRegularGroupingAndDigitFamilies()
    {
        XdmSequence result = Evaluate("(format-integer(7, '001'), format-integer(1234567, '#,##0'), format-integer(1234, '٠٠٠٠'), format-integer(-42, '٠٠٠'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "007", "1,234,567", "١٢٣٤", "-٠٤٢" }));
    }

    [Test]
    public void FormatsIntegerSequencesWordsAndOrdinals()
    {
        XdmSequence result = Evaluate("(format-integer(27, 'A'), format-integer(49, 'i'), format-integer(42, 'Ww'), format-integer(22, '1;o'), format-integer(21, 'w;o'), format-integer((), '1'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "AA", "xlix", "Forty-Two", "22nd", "twenty-first", "" }));
    }

    [Test]
    public void AppliesIrregularIntegerGroupingTemplates()
    {
        XdmSequence result = Evaluate("format-integer(1234567, '0-00.000')", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "12-34.567" }));
    }

    [Test]
    public void FormatsFixedNumbersWithRoundingGroupingAndOptionalDigits()
    {
        XdmSequence result = Evaluate("(format-number(12345.6, '#,###.00'), format-number(123.9, '9999'), format-number(2.345, '0.00'), format-number(2.355, '0.00'), format-number(0.5, '#.##'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "12,345.60", "0124", "2.34", "2.36", ".5" }));
    }

    [Test]
    public void FormatsPercentPerMilleNegativeSubpicturesAndSpecialValues()
    {
        XdmSequence result = Evaluate("(format-number(0.14, '01%'), format-number(0.125, '0.0‰'), format-number(-6, '000;(000)'), format-number(xs:double('INF'), '$0.0'), format-number(xs:double('-INF'), '$0.0'), format-number((), '$0.0'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "14%", "125.0‰", "(006)", "$Infinity", "-$Infinity", "NaN" }));
    }

    [Test]
    public void ScalesFloatingValuesBeforeRoundingPerMillePictures()
    {
        XdmSequence result = Evaluate("(format-number(xs:double('1.102030405060708090'), '0.00000000000000000‰'), format-number(xs:double('1.102030405060708090') * 1000, '0.00000000000000000'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1102.03040506070800000‰", "1102.03040506070800000" }));
    }

    [Test]
    public void FormatsExponentialPicturesAndNegativeZero()
    {
        XdmSequence result = Evaluate("(format-number(1234.5678, '00.000e0'), format-number(0.234, '0.0e00'), format-number(0.234, '#.00e0'), format-number(xs:double('-0'), '0.0;(0.0)'))", new XPathStaticContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "12.346e2", "2.3e-01", "0.23e0", "(0.0)" }));
    }

    [Test]
    public void ResolvesDeclaredDecimalFormatNames()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "f", NamespaceUri = "urn:formats" }],
            DecimalFormats = [new XmlName { LocalName = "western", NamespaceUri = "urn:formats" }]
        };

        XdmSequence result = Evaluate("(format-number(1234.5, '#,##0.00', 'f:western'), format-number(3, '00', 'Q{urn:formats}western'), format-number(4, '0', ''))", context);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1,234.50", "03", "4" }));
    }

    [Test]
    public void AppliesDeclaredDecimalFormatSymbols()
    {
        XPathDecimalFormat custom = new XPathDecimalFormat
        {
            Name = new XmlName { LocalName = "custom", NamespaceUri = "urn:formats" },
            DecimalSeparator = "|",
            GroupingSeparator = ".",
            ExponentSeparator = "E",
            MinusSign = "_",
            Infinity = "INF"
        };
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "f", NamespaceUri = "urn:formats" }],
            DecimalFormats = [custom.Name],
            DecimalFormatDeclarations = [custom]
        };

        XdmSequence result = Evaluate("(format-number(-1234.5, '#.##0|0', 'f:custom'), format-number(xs:double('INF'), 'P0S', 'f:custom'), format-number(1234.5678, '00|000E0', 'f:custom'))", context);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "_1.234|5", "PINFS", "12|346E2" }));
    }

    [Test]
    public void ReportsPictureAndDecimalFormatErrors()
    {
        XPathDynamicException integer = Assert.Throws<XPathDynamicException>(() => Evaluate("format-integer(3, '#0#')", new XPathStaticContext()))!;
        XPathDynamicException number = Assert.Throws<XPathDynamicException>(() => Evaluate("format-number(3, '0.#0')", new XPathStaticContext()))!;
        XPathDynamicException format = Assert.Throws<XPathDynamicException>(() => Evaluate("format-number(3, '0', 'missing')", new XPathStaticContext()))!;
        XPathDynamicException prefix = Assert.Throws<XPathDynamicException>(() => Evaluate("format-number(3, '0', 'bad:name')", new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(integer.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODF1310));
            Assert.That(number.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODF1310));
            Assert.That(format.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODF1280));
            Assert.That(prefix.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODF1280));
        });
    }

    [TestCase("format-integer(1)")]
    [TestCase("format-number(1)")]
    [TestCase("format-number(1, '0', (), ())")]
    public void RejectsUnsupportedFormattingFunctionArities(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }
}
