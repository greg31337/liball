namespace uri.Tests;

public class TemplateMapExpansionTests
{
    [TestCase("{keys}", "semi,%3B,dot,.,comma,%2C")]
    [TestCase("{keys*}", "semi=%3B,dot=.,comma=%2C")]
    [TestCase("{+keys}", "semi,;,dot,.,comma,,")]
    [TestCase("{+keys*}", "semi=;,dot=.,comma=,")]
    [TestCase("{#keys}", "#semi,;,dot,.,comma,,")]
    [TestCase("{#keys*}", "#semi=;,dot=.,comma=,")]
    [TestCase("{.keys}", ".semi,%3B,dot,.,comma,%2C")]
    [TestCase("{.keys*}", ".semi=%3B.dot=..comma=%2C")]
    [TestCase("{/keys}", "/semi,%3B,dot,.,comma,%2C")]
    [TestCase("{/keys*}", "/semi=%3B/dot=./comma=%2C")]
    [TestCase("{;keys}", ";keys=semi,%3B,dot,.,comma,%2C")]
    [TestCase("{;keys*}", ";semi=%3B;dot=.;comma=%2C")]
    [TestCase("{?keys}", "?keys=semi,%3B,dot,.,comma,%2C")]
    [TestCase("{?keys*}", "?semi=%3B&dot=.&comma=%2C")]
    [TestCase("{&keys}", "&keys=semi,%3B,dot,.,comma,%2C")]
    [TestCase("{&keys*}", "&semi=%3B&dot=.&comma=%2C")]
    public void OrderedMapsExpandWithAndWithoutExplodeForEveryOperator(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[] { MapBinding("keys", StandardEntries()) });

        AssertSuccess(result, expected);
    }

    [Test]
    public void UndefinedMembersAreOmittedWithoutChangingDefinedMemberOrder()
    {
        UriTemplateMapEntry[] members = new UriTemplateMapEntry[]
        {
            new UriTemplateMapEntry("ignored", false, "bad"),
            new UriTemplateMapEntry("first", true, "1"),
            new UriTemplateMapEntry("empty", true, ""),
            new UriTemplateMapEntry("last", true, "3")
        };
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            MapBinding("map", members)
        };

        UriTemplateExpansionResult compact = Expand("{map}", bindings);
        UriTemplateExpansionResult query = Expand("{?map*}", bindings);
        UriTemplateExpansionResult path = Expand("{/map*}", bindings);

        Assert.Multiple(() =>
        {
            AssertSuccess(compact, "first,1,empty,,last,3");
            AssertSuccess(query, "?first=1&empty=&last=3");
            AssertSuccess(path, "/first=1/empty/last=3");
        });
    }

    [TestCase("A{map}B")]
    [TestCase("A{map*}B")]
    [TestCase("A{#map}B")]
    [TestCase("A{/map*}B")]
    [TestCase("A{;map}B")]
    [TestCase("A{?map*}B")]
    public void EmptyAndAllUndefinedMapsEmitNoOperatorText(string templateText)
    {
        UriTemplateExpansionResult empty = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[0])
            });
        UriTemplateExpansionResult undefined = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("one", false, "1"),
                    new UriTemplateMapEntry("two", false, "2")
                })
            });

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, "AB");
            AssertSuccess(undefined, "AB");
        });
    }

    [TestCase("{map}", "a%252Fb,x%252Fy,caf%C3%A9,%E2%82%AC")]
    [TestCase("{map*}", "a%252Fb=x%252Fy,caf%C3%A9=%E2%82%AC")]
    [TestCase("{+map}", "a%2Fb,x%2Fy,caf%C3%A9,%E2%82%AC")]
    [TestCase("{+map*}", "a%2Fb=x%2Fy,caf%C3%A9=%E2%82%AC")]
    [TestCase("{?map*}", "?a%252Fb=x%252Fy&caf%C3%A9=%E2%82%AC")]
    public void MapKeysAndValuesUseTheOperatorEncodingRules(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("a%2Fb", true, "x%2Fy"),
                    new UriTemplateMapEntry("café", true, "€")
                })
            });

        AssertSuccess(result, expected);
    }

    [TestCase("{map*}", "empty")]
    [TestCase("{+map*}", "empty")]
    [TestCase("{#map*}", "#empty")]
    [TestCase("{.map*}", ".empty")]
    [TestCase("{/map*}", "/empty")]
    [TestCase("{;map*}", ";empty")]
    [TestCase("{?map*}", "?empty=")]
    [TestCase("{&map*}", "&empty=")]
    public void ExplodedEmptyValuesUseOperatorSpecificEmptyBehavior(
        string templateText,
        string expected)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("empty", true, "")
                })
            });

        AssertSuccess(result, expected);
    }

    [Test]
    public void ExplodedMapsComposeWithScalarAndListVariables()
    {
        UriTemplateExpansionResult result = Expand(
            "{?x,map*,list*,y}",
            new UriTemplateBinding[]
            {
                ScalarBinding("x", "1"),
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("a", true, "2"),
                    new UriTemplateMapEntry("ignored", false, "3")
                }),
                ListBinding("list", new string[] { "4", "5" }),
                ScalarBinding("y", "6")
            });

        AssertSuccess(result, "?x=1&a=2&list=4&list=5&y=6");
    }

    [TestCase("{map:1}", 4)]
    [TestCase("{?map:9999}", 5)]
    public void PrefixModifiersAreRejectedForMapValuesAtTheColon(
        string templateText,
        int offset)
    {
        UriTemplateExpansionResult result = Expand(
            templateText,
            new UriTemplateBinding[]
            {
                MapBinding("map", StandardEntries())
            });

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateModifier,
            offset,
            1,
            TemplateExpanderCore.CompositePrefixMessage);
    }

    [Test]
    public void MalformedUtf16InDefinedMapDataFailsButUndefinedDataIsIgnored()
    {
        string malformed = "x" + new string('\ud800', 1);
        UriTemplateExpansionResult ignored = Expand(
            "{?map*}",
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry(malformed, false, malformed),
                    new UriTemplateMapEntry("valid", true, "yes")
                })
            });
        UriTemplateExpansionResult invalidKey = Expand(
            "{?map*}",
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry(malformed, true, "value")
                })
            });
        UriTemplateExpansionResult invalidValue = Expand(
            "{?map*}",
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("key", true, malformed)
                })
            });

        Assert.Multiple(() =>
        {
            AssertSuccess(ignored, "?valid=yes");
            AssertFailure(
                invalidKey,
                UriDiagnosticCode.InvalidUnicode,
                1,
                1,
                DiagnosticFactory.InvalidUnicodeMessage);
            AssertFailure(
                invalidValue,
                UriDiagnosticCode.InvalidUnicode,
                1,
                1,
                DiagnosticFactory.InvalidUnicodeMessage);
        });
    }

    private static UriTemplateMapEntry[] StandardEntries()
    {
        return new UriTemplateMapEntry[]
        {
            new UriTemplateMapEntry("semi", true, ";"),
            new UriTemplateMapEntry("dot", true, "."),
            new UriTemplateMapEntry("comma", true, ",")
        };
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

    private static UriTemplateBinding MapBinding(
        string name,
        UriTemplateMapEntry[] members)
    {
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Map = members;
        return new UriTemplateBinding(name, map);
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
