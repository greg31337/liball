namespace uri.Tests;

public class TemplateListExpansionTests
{
    [TestCase("{list}", "red,green,blue")]
    [TestCase("{list*}", "red,green,blue")]
    [TestCase("{+list}", "red,green,blue")]
    [TestCase("{+list*}", "red,green,blue")]
    [TestCase("{#list}", "#red,green,blue")]
    [TestCase("{#list*}", "#red,green,blue")]
    [TestCase("{.list}", ".red,green,blue")]
    [TestCase("{.list*}", ".red.green.blue")]
    [TestCase("{/list}", "/red,green,blue")]
    [TestCase("{/list*}", "/red/green/blue")]
    [TestCase("{;list}", ";list=red,green,blue")]
    [TestCase("{;list*}", ";list=red;list=green;list=blue")]
    [TestCase("{?list}", "?list=red,green,blue")]
    [TestCase("{?list*}", "?list=red&list=green&list=blue")]
    [TestCase("{&list}", "&list=red,green,blue")]
    [TestCase("{&list*}", "&list=red&list=green&list=blue")]
    public void ListsExpandWithAndWithoutExplodeForEveryOperator(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "red", "green", "blue" })
            });

        AssertSuccess(result, expected);
    }

    [TestCase("{list}", "red%2525,%252Fgreen,blue%20,caf%C3%A9")]
    [TestCase("{list*}", "red%2525,%252Fgreen,blue%20,caf%C3%A9")]
    [TestCase("{+list}", "red%25,%2Fgreen,blue%20,caf%C3%A9")]
    [TestCase("{+list*}", "red%25,%2Fgreen,blue%20,caf%C3%A9")]
    [TestCase("{#list}", "#red%25,%2Fgreen,blue%20,caf%C3%A9")]
    [TestCase("{#list*}", "#red%25,%2Fgreen,blue%20,caf%C3%A9")]
    public void EveryListMemberUsesTheOperatorEncodingRules(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[]
                {
                    "red%25",
                    "%2Fgreen",
                    "blue ",
                    "café"
                })
            });

        AssertSuccess(result, expected);
    }

    [TestCase("{list}", ",x,")]
    [TestCase("{list*}", ",x,")]
    [TestCase("{#list*}", "#,x,")]
    [TestCase("{.list*}", "..x.")]
    [TestCase("{/list*}", "//x/")]
    [TestCase("{;list}", ";list=,x,")]
    [TestCase("{;list*}", ";list;list=x;list")]
    [TestCase("{?list}", "?list=,x,")]
    [TestCase("{?list*}", "?list=&list=x&list=")]
    [TestCase("{&list*}", "&list=&list=x&list=")]
    public void EmptyMembersRemainDefinedAndUseNamedEmptyBehavior(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "", "x", "" })
            });

        AssertSuccess(result, expected);
    }

    [TestCase("A{list}B")]
    [TestCase("A{list*}B")]
    [TestCase("A{#list}B")]
    [TestCase("A{.list*}B")]
    [TestCase("A{/list}B")]
    [TestCase("A{;list*}B")]
    [TestCase("A{?list}B")]
    [TestCase("A{&list*}B")]
    public void EmptyListsAreUndefinedAndEmitNoOperatorText(string templateText)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[0])
            });

        AssertSuccess(result, "AB");
    }

    [Test]
    public void ExplodedListsComposeWithAdjacentScalarVariables()
    {
        UriTemplateExpansionResult result = Expand(
            "{?x,list*,missing,y}",
            new UriTemplateBinding[]
            {
                ScalarBinding("x", "1"),
                ListBinding("list", new string[] { "a", "b" }),
                ScalarBinding("y", "2")
            });

        AssertSuccess(result, "?x=1&list=a&list=b&y=2");
    }

    [Test]
    public void EncodedVariableNamesAreRepeatedWithoutReencoding()
    {
        UriTemplateExpansionResult result = Expand(
            "{?Some%20Thing*}",
            new UriTemplateBinding[]
            {
                ListBinding("Some%20Thing", new string[] { "a", "b" })
            });

        AssertSuccess(result, "?Some%20Thing=a&Some%20Thing=b");
    }

    [TestCase("{list:1}", 5)]
    [TestCase("{?list:9999}", 6)]
    public void PrefixModifiersAreRejectedForListValuesAtTheColon(
        string templateText,
        int offset)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "red" })
            });

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateModifier,
            offset,
            1,
            TemplateExpanderCore.CompositePrefixMessage);
    }

    [Test]
    public void MalformedUtf16InAnyListMemberFailsStrictly()
    {
        string malformed = "x" + new string('\ud800', 1);

        UriTemplateExpansionResult result = Expand(
            "{/list*}",
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "valid", malformed })
            });

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUnicode,
            1,
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

    private static UriTemplateBinding ListBinding(
        string name,
        string[] members)
    {
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = members;
        return new UriTemplateBinding(name, list);
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
        Assert.That(result.Diagnostic.Component, Is.EqualTo(
            UriComponent.TemplateVariable));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
