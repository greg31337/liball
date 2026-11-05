namespace uri.Tests;

public class TemplateScalarExpansionTests
{
    [TestCase("{var}", "value")]
    [TestCase("{+var}", "value")]
    [TestCase("{#var}", "#value")]
    [TestCase("{.var}", ".value")]
    [TestCase("{/var}", "/value")]
    [TestCase("{;var}", ";var=value")]
    [TestCase("{?var}", "?var=value")]
    [TestCase("{&var}", "&var=value")]
    [TestCase("{/var*}", "/value")]
    public void ScalarValuesExpandWithEveryOperator(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[] { ScalarBinding("var", "value") });

        AssertSuccess(result, expected);
    }

    [TestCase("A{empty}B", "AB")]
    [TestCase("A{+empty}B", "AB")]
    [TestCase("A{#empty}B", "A#B")]
    [TestCase("A{.empty}B", "A.B")]
    [TestCase("A{/empty}B", "A/B")]
    [TestCase("A{;empty}B", "A;emptyB")]
    [TestCase("A{?empty}B", "A?empty=B")]
    [TestCase("A{&empty}B", "A&empty=B")]
    public void DefinedEmptyScalarsUseOperatorSpecificEmptyBehavior(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[] { ScalarBinding("empty", "") });

        AssertSuccess(result, expected);
    }

    [Test]
    public void UndefinedAndMissingValuesDoNotEmitPrefixesOrSeparators()
    {
        UriTemplateValue undefined = new UriTemplateValue();
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            new UriTemplateBinding("undef", undefined),
            ScalarBinding("x", "1024"),
            ScalarBinding("empty", ""),
            ScalarBinding("y", "768")
        };

        UriTemplateExpansionResult query = Expand(
            "{?undef,x,missing,empty,y}",
            bindings);
        UriTemplateExpansionResult simple = Expand(
            "?{x,empty,undef,missing,y}",
            bindings);
        UriTemplateExpansionResult allUndefined = Expand(
            "A{#undef,missing}B",
            bindings);

        Assert.Multiple(() =>
        {
            AssertSuccess(query, "?x=1024&empty=&y=768");
            AssertSuccess(simple, "?1024,,768");
            AssertSuccess(allUndefined, "AB");
        });
    }

    [TestCase("{hello}", "Hello%20World%21")]
    [TestCase("{+hello}", "Hello%20World!")]
    [TestCase("{path}", "%2Ffoo%2Fbar")]
    [TestCase("{+path}", "/foo/bar")]
    [TestCase("{#path}", "#/foo/bar")]
    [TestCase("{id}", "admin%252F")]
    [TestCase("{+id}", "admin%2F")]
    [TestCase("{#id}", "#admin%2F")]
    [TestCase("{not_pct}", "%25foo")]
    [TestCase("{+not_pct}", "%25foo")]
    public void ScalarEncodingDistinguishesSimpleAndReservedExpansion(
        string templateText,
        string expected)
    {
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("hello", "Hello World!"),
            ScalarBinding("path", "/foo/bar"),
            ScalarBinding("id", "admin%2F"),
            ScalarBinding("not_pct", "%foo")
        };

        UriTemplateExpansionResult result = Expand(templateText, bindings);

        AssertSuccess(result, expected);
    }

    [TestCase("café/{var}", "caf%C3%A9/value")]
    [TestCase("x%20y/{var}", "x%20y/value")]
    [TestCase("😀/{var}", "%F0%9F%98%80/value")]
    public void LiteralExpansionUsesUtf8AndPreservesPercentTriplets(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[] { ScalarBinding("var", "value") });

        AssertSuccess(result, expected);
    }

    [TestCase("{greek:1}", "%CE%B1")]
    [TestCase("{greek:2}", "%CE%B1%CE%B2")]
    [TestCase("{+greek:2}", "%CE%B1%CE%B2")]
    [TestCase("{greek:30}", "%CE%B1%CE%B2%CE%B3%CE%B4%CE%B5")]
    [TestCase("{currency:1}", "%E2%82%AC")]
    [TestCase("{?currency:1}", "?currency=%E2%82%AC")]
    [TestCase("{clef:1}", "%F0%9D%84%9E")]
    [TestCase("{var:9999}", "value")]
    [TestCase("{+encoded:1}", "%2F")]
    [TestCase("{encoded:1}", "%252F")]
    public void PrefixLengthsCountUnicodeScalarsAndWholePercentTriplets(
        string templateText,
        string expected)
    {
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("greek", "αβγδε"),
            ScalarBinding("currency", "€uro"),
            ScalarBinding("clef", "𝄞stave"),
            ScalarBinding("var", "value"),
            ScalarBinding("encoded", "%2Ftail")
        };

        UriTemplateExpansionResult result = Expand(templateText, bindings);

        AssertSuccess(result, expected);
    }

    [Test]
    public void BindingLookupIsCaseSensitiveAndPreservesEncodedNames()
    {
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("Name", "upper"),
            ScalarBinding("name", "lower"),
            ScalarBinding("Some%20Thing", "encoded")
        };

        UriTemplateExpansionResult result = Expand(
            "{Name}/{name}{?Some%20Thing,NAME}",
            bindings);

        AssertSuccess(result, "upper/lower?Some%20Thing=encoded");
    }

    [Test]
    public void MalformedUtf16InAScalarFailsEvenBeyondTheSelectedPrefix()
    {
        string malformed = "ok" + new string('\ud800', 1);

        UriTemplateExpansionResult result = Expand(
            "{var:1}",
            new UriTemplateBinding[] { ScalarBinding("var", malformed) });

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateVariable,
            2,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void MalformedLiteralModelTextFailsWithItsTemplateRange()
    {
        UriTemplatePart literal = new UriTemplatePart();
        literal.Literal = new string('\udc00', 1);
        literal.Range = new TextRange(5, 1);
        UriTemplate template = new UriTemplate();
        template.Parts = new UriTemplatePart[] { literal };

        UriTemplateExpansionResult result = TemplateExpanderCore.ExpandValues(
            template,
            new UriTemplateBinding[0]);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral,
            5,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    private static UriTemplateExpansionResult Expand(
        string templateText,
        UriTemplateBinding[] bindings)
    {
        UriTemplateParseResult parsed = TemplateParserCore.ParseTemplate(
            templateText.AsSpan());
        Assert.That(parsed.Success, Is.True);
        return TemplateExpanderCore.ExpandValues(parsed.Template, bindings);
    }

    private static UriTemplateBinding ScalarBinding(string name, string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
    }

    private static void AssertSuccess(
        UriTemplateExpansionResult result,
        string expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriTemplateExpansionResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
