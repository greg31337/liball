using System.Collections.Generic;

namespace uri.Tests;

public class TemplateExpansionOperatorMatrixTests
{
    private static readonly string[] OperatorTokens = new string[]
    {
        "",
        "+",
        "#",
        ".",
        "/",
        ";",
        "?",
        "&"
    };

    private static readonly string[] ScalarExpected = new string[]
    {
        "a%2Fb%20caf%C3%A9",
        "a/b%20caf%C3%A9",
        "#a/b%20caf%C3%A9",
        ".a%2Fb%20caf%C3%A9",
        "/a%2Fb%20caf%C3%A9",
        ";value=a%2Fb%20caf%C3%A9",
        "?value=a%2Fb%20caf%C3%A9",
        "&value=a%2Fb%20caf%C3%A9"
    };

    private static readonly string[] CompactListExpected = new string[]
    {
        "a%2Fb,caf%C3%A9",
        "a/b,caf%C3%A9",
        "#a/b,caf%C3%A9",
        ".a%2Fb,caf%C3%A9",
        "/a%2Fb,caf%C3%A9",
        ";value=a%2Fb,caf%C3%A9",
        "?value=a%2Fb,caf%C3%A9",
        "&value=a%2Fb,caf%C3%A9"
    };

    private static readonly string[] ExplodedListExpected = new string[]
    {
        "a%2Fb,caf%C3%A9",
        "a/b,caf%C3%A9",
        "#a/b,caf%C3%A9",
        ".a%2Fb.caf%C3%A9",
        "/a%2Fb/caf%C3%A9",
        ";value=a%2Fb;value=caf%C3%A9",
        "?value=a%2Fb&value=caf%C3%A9",
        "&value=a%2Fb&value=caf%C3%A9"
    };

    private static readonly string[] CompactMapExpected = new string[]
    {
        "first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC",
        "first/key,a/b,caf%C3%A9,%E2%82%AC",
        "#first/key,a/b,caf%C3%A9,%E2%82%AC",
        ".first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC",
        "/first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC",
        ";value=first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC",
        "?value=first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC",
        "&value=first%2Fkey,a%2Fb,caf%C3%A9,%E2%82%AC"
    };

    private static readonly string[] ExplodedMapExpected = new string[]
    {
        "first%2Fkey=a%2Fb,caf%C3%A9=%E2%82%AC",
        "first/key=a/b,caf%C3%A9=%E2%82%AC",
        "#first/key=a/b,caf%C3%A9=%E2%82%AC",
        ".first%2Fkey=a%2Fb.caf%C3%A9=%E2%82%AC",
        "/first%2Fkey=a%2Fb/caf%C3%A9=%E2%82%AC",
        ";first%2Fkey=a%2Fb;caf%C3%A9=%E2%82%AC",
        "?first%2Fkey=a%2Fb&caf%C3%A9=%E2%82%AC",
        "&first%2Fkey=a%2Fb&caf%C3%A9=%E2%82%AC"
    };

    private static readonly string[] EmptyScalarExpected = new string[]
    {
        "AB",
        "AB",
        "A#B",
        "A.B",
        "A/B",
        "A;valueB",
        "A?value=B",
        "A&value=B"
    };

    private static readonly string[] PrefixExpected = new string[]
    {
        "%F0%9F%98%80%CE%B1",
        "%F0%9F%98%80%CE%B1",
        "#%F0%9F%98%80%CE%B1",
        ".%F0%9F%98%80%CE%B1",
        "/%F0%9F%98%80%CE%B1",
        ";value=%F0%9F%98%80%CE%B1",
        "?value=%F0%9F%98%80%CE%B1",
        "&value=%F0%9F%98%80%CE%B1"
    };

    public static IEnumerable<TestCaseData> ValueOperatorCases()
    {
        int operatorIndex = 0;
        while (operatorIndex < OperatorTokens.Length)
        {
            yield return CreateMatrixCase(
                operatorIndex,
                "scalar",
                false,
                ScalarExpected[operatorIndex]);
            yield return CreateMatrixCase(
                operatorIndex,
                "scalar",
                true,
                ScalarExpected[operatorIndex]);
            yield return CreateMatrixCase(
                operatorIndex,
                "list",
                false,
                CompactListExpected[operatorIndex]);
            yield return CreateMatrixCase(
                operatorIndex,
                "list",
                true,
                ExplodedListExpected[operatorIndex]);
            yield return CreateMatrixCase(
                operatorIndex,
                "map",
                false,
                CompactMapExpected[operatorIndex]);
            yield return CreateMatrixCase(
                operatorIndex,
                "map",
                true,
                ExplodedMapExpected[operatorIndex]);
            operatorIndex = operatorIndex + 1;
        }
    }

    public static IEnumerable<TestCaseData> EmptyOperatorCases()
    {
        int operatorIndex = 0;
        while (operatorIndex < OperatorTokens.Length)
        {
            TestCaseData data = new TestCaseData(
                OperatorTokens[operatorIndex],
                EmptyScalarExpected[operatorIndex]);
            data.SetName(
                "Empty and undefined operator " + OperatorName(operatorIndex));
            yield return data;
            operatorIndex = operatorIndex + 1;
        }
    }

    public static IEnumerable<TestCaseData> PrefixOperatorCases()
    {
        int operatorIndex = 0;
        while (operatorIndex < OperatorTokens.Length)
        {
            TestCaseData data = new TestCaseData(
                OperatorTokens[operatorIndex],
                PrefixExpected[operatorIndex]);
            data.SetName(
                "Unicode prefix operator " + OperatorName(operatorIndex));
            yield return data;
            operatorIndex = operatorIndex + 1;
        }
    }

    public static IEnumerable<TestCaseData> CompositePrefixCases()
    {
        int operatorIndex = 0;
        while (operatorIndex < OperatorTokens.Length)
        {
            TestCaseData list = new TestCaseData(
                OperatorTokens[operatorIndex],
                "list");
            list.SetName(
                "List prefix rejected for " + OperatorName(operatorIndex));
            yield return list;

            TestCaseData map = new TestCaseData(
                OperatorTokens[operatorIndex],
                "map");
            map.SetName(
                "Map prefix rejected for " + OperatorName(operatorIndex));
            yield return map;
            operatorIndex = operatorIndex + 1;
        }
    }

    [TestCaseSource(nameof(ValueOperatorCases))]
    public void ScalarListAndMapValuesExpandAcrossEveryOperator(
        string operatorToken,
        string shape,
        bool explode,
        string expected)
    {
        string modifier = "";
        if (explode)
        {
            modifier = "*";
        }

        UriTemplate template = Parse(
            "{" + operatorToken + "value" + modifier + "}");
        UriTemplateBinding binding = BindingForShape(shape);

        UriTemplateExpansionResult result = Expand(
            template,
            new UriTemplateBinding[] { binding });

        AssertSuccess(result, expected);
    }

    [TestCaseSource(nameof(EmptyOperatorCases))]
    public void EmptyAndUndefinedShapesHaveOperatorSpecificBehavior(
        string operatorToken,
        string emptyScalarExpected)
    {
        UriTemplate template = Parse("A{" + operatorToken + "value}B");

        UriTemplateExpansionResult scalar = Expand(
            template,
            new UriTemplateBinding[] { ScalarBinding("value", "") });
        UriTemplateExpansionResult list = Expand(
            template,
            new UriTemplateBinding[]
            {
                ListBinding("value", new string[0])
            });
        UriTemplateExpansionResult map = Expand(
            template,
            new UriTemplateBinding[]
            {
                MapBinding("value", new UriTemplateMapEntry[0])
            });
        UriTemplateExpansionResult undefined = Expand(
            template,
            new UriTemplateBinding[]
            {
                new UriTemplateBinding("value", new UriTemplateValue())
            });
        UriTemplateExpansionResult missing = Expand(
            template,
            new UriTemplateBinding[0]);

        Assert.Multiple(() =>
        {
            AssertSuccess(scalar, emptyScalarExpected);
            AssertSuccess(list, "AB");
            AssertSuccess(map, "AB");
            AssertSuccess(undefined, "AB");
            AssertSuccess(missing, "AB");
        });
    }

    [TestCaseSource(nameof(PrefixOperatorCases))]
    public void UnicodeScalarPrefixesWorkAcrossEveryOperator(
        string operatorToken,
        string expected)
    {
        UriTemplate template = Parse(
            "{" + operatorToken + "value:2}");

        UriTemplateExpansionResult result = Expand(
            template,
            new UriTemplateBinding[]
            {
                ScalarBinding("value", "😀α/b")
            });

        AssertSuccess(result, expected);
    }

    [TestCaseSource(nameof(CompositePrefixCases))]
    public void CompositePrefixesFailAcrossEveryOperator(
        string operatorToken,
        string shape)
    {
        UriTemplate template = Parse(
            "{" + operatorToken + "value:2}");
        UriTemplateBinding binding = BindingForShape(shape);

        UriTemplateExpansionResult result = Expand(
            template,
            new UriTemplateBinding[] { binding });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
            DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidTemplateModifier));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(
            UriComponent.TemplateVariable));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(
            TemplateExpanderCore.CompositePrefixMessage));
    }

    private static TestCaseData CreateMatrixCase(
        int operatorIndex,
        string shape,
        bool explode,
        string expected)
    {
        TestCaseData data = new TestCaseData(
            OperatorTokens[operatorIndex],
            shape,
            explode,
            expected);
        string modifier = " compact";
        if (explode)
        {
            modifier = " exploded";
        }

        data.SetName(
            shape + modifier + " operator " + OperatorName(operatorIndex));
        return data;
    }

    private static string OperatorName(int operatorIndex)
    {
        if (operatorIndex == 0)
        {
            return "simple";
        }

        return OperatorTokens[operatorIndex];
    }

    private static UriTemplateBinding BindingForShape(string shape)
    {
        if (shape == "scalar")
        {
            return ScalarBinding("value", "a/b café");
        }

        if (shape == "list")
        {
            return ListBinding(
                "value",
                new string[] { "a/b", "café" });
        }

        return MapBinding(
            "value",
            new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("first/key", true, "a/b"),
                new UriTemplateMapEntry("café", true, "€")
            });
    }

    private static UriTemplate Parse(string text)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Template;
    }

    private static UriTemplateExpansionResult Expand(
        UriTemplate template,
        UriTemplateBinding[] bindings)
    {
        return TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            new UriTemplateExpansionOptions());
    }

    private static UriTemplateBinding ScalarBinding(string name, string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
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

    private static UriTemplateBinding MapBinding(
        string name,
        UriTemplateMapEntry[] members)
    {
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Map = members;
        return new UriTemplateBinding(name, map);
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
}
