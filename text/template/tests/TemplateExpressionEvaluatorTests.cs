using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateExpressionEvaluatorTests
{
    [Test]
    public void Evaluate_Literals_ReturnTemplateValues()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("\"a\\nb\"")), "a\nb");
        AssertNumber(evaluator.Evaluate(ParseExpression("12.5")), 12.5m);
        Assert.That(evaluator.Evaluate(ParseExpression("true")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("null")).Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(evaluator.Evaluate(ParseExpression("empty")).Kind, Is.EqualTo(TemplateValueKind.List));

        TemplateValue array = evaluator.Evaluate(ParseExpression("[1, \"x\", false]"));
        TemplateValue map = evaluator.Evaluate(ParseExpression("{\"name\": \"Ada\", \"age\": 37}"));
        TemplateValue range = evaluator.Evaluate(ParseExpression("1..3"));

        Assert.That(array.Kind, Is.EqualTo(TemplateValueKind.List));
        AssertNumber(array.ListValue[0], 1m);
        AssertString(array.ListValue[1], "x");
        Assert.That(array.ListValue[2].BooleanValue, Is.False);
        Assert.That(map.Kind, Is.EqualTo(TemplateValueKind.Map));
        AssertString(map.GetMapItem("name"), "Ada");
        AssertNumber(map.GetMapItem("age"), 37m);
        Assert.That(range.Kind, Is.EqualTo(TemplateValueKind.List));
        AssertNumber(range.ListValue[0], 1m);
        AssertNumber(range.ListValue[2], 3m);
    }

    [Test]
    public void Evaluate_Paths_ResolveScopeRootCurrentParentLocalMembersAndIndexes()
    {
        TemplateValue root = CreateRoot();
        TemplateScopeStack scope = TemplateScopeStack.Create(root);
        scope.SetLocal("index", TemplateValue.FromNumber(2m));
        TemplateValue current = TemplateValue.FromMap(CreateMap("title", TemplateValue.FromString("Current")));
        scope.PushLoopScope(current);
        TemplateExpressionEvaluator evaluator = CreateEvaluator(scope);

        AssertString(evaluator.Evaluate(ParseExpression("user.name")), "Ada");
        AssertString(evaluator.Evaluate(ParseExpression("$.site.title")), "Root");
        AssertString(evaluator.Evaluate(ParseExpression("this.title")), "Current");
        AssertString(evaluator.Evaluate(ParseExpression("../site.title")), "Root");
        AssertNumber(evaluator.Evaluate(ParseExpression("@index")), 2m);
        AssertString(evaluator.Evaluate(ParseExpression("items[1].name")), "Second");
        Assert.That(evaluator.Evaluate(ParseExpression("missing.name")).IsMissing(), Is.True);
    }

    [Test]
    public void Evaluate_Operators_ApplyArithmeticComparisonLogicalCoalesceAndConditional()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertNumber(evaluator.Evaluate(ParseExpression("1 + 2 * 3")), 7m);
        AssertNumber(evaluator.Evaluate(ParseExpression("(10 - 4) / 3")), 2m);
        AssertNumber(evaluator.Evaluate(ParseExpression("10 % 4")), 2m);
        AssertString(evaluator.Evaluate(ParseExpression("\"A\" + 1")), "A1");
        Assert.That(evaluator.Evaluate(ParseExpression("7 >= 7 and not false")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("1 > 2 or true")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("\"a\" == \"a\"")).BooleanValue, Is.True);
        AssertString(evaluator.Evaluate(ParseExpression("missing ?? \"fallback\"")), "fallback");
        AssertString(evaluator.Evaluate(ParseExpression("true ? \"yes\" : \"no\"")), "yes");
    }

    [Test]
    public void Evaluate_CallsFiltersAndSubexpressions_InvokeRegisteredFunctions()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());
        evaluator.RegisterFunction("concat", delegate (List<TemplateValue> arguments)
        {
            return TemplateValue.FromString(arguments[0].StringValue + arguments[1].StringValue);
        });
        evaluator.RegisterFilter("append", delegate (List<TemplateValue> arguments)
        {
            return TemplateValue.FromString(arguments[0].StringValue + arguments[1].StringValue);
        });

        AssertString(evaluator.Evaluate(ParseExpression("concat(\"A\", \"B\")")), "AB");
        AssertString(evaluator.Evaluate(ParseExpression("\"A\" | append(\"B\") | append(\"C\")")), "ABC");
        AssertString(evaluator.Evaluate(ParseExpression("(concat first last)")), "AdaLovelace");
    }

    [Test]
    public void Evaluate_BuiltInStringFilters_ChangeCaseAndTrimText()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("\" ada lovelace \" | upper | trim")), "ADA LOVELACE");
        AssertString(evaluator.Evaluate(ParseExpression("\" ADA \" | lower | trim")), "ada");
        AssertString(evaluator.Evaluate(ParseExpression("\"aDA\" | capitalize")), "Ada");
        AssertString(evaluator.Evaluate(ParseExpression("\"hello WORLD\" | title")), "Hello World");
        AssertString(evaluator.Evaluate(ParseExpression("\"  x  \" | ltrim")), "x  ");
        AssertString(evaluator.Evaluate(ParseExpression("\"  x  \" | rtrim")), "  x");
    }

    [Test]
    public void Evaluate_BuiltInStringFilters_EditAndCombineText()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("\"hello hello\" | replace(\"hello\", \"hi\")")), "hi hi");
        AssertString(evaluator.Evaluate(ParseExpression("\"banana\" | remove(\"na\")")), "ba");
        AssertString(evaluator.Evaluate(ParseExpression("\"Ada\" | append(\" Lovelace\")")), "Ada Lovelace");
        AssertString(evaluator.Evaluate(ParseExpression("\"Lovelace\" | prepend(\"Ada \")")), "Ada Lovelace");
        AssertString(evaluator.Evaluate(ParseExpression("\"a\\nb\\r\\nc\" | newline_to_br")), "a<br>b<br>c");
    }

    [Test]
    public void Evaluate_BuiltInStringFilters_SplitJoinSliceAndTruncate()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        TemplateValue split = evaluator.Evaluate(ParseExpression("\"a,b,c\" | split(\",\")"));

        Assert.That(split.Kind, Is.EqualTo(TemplateValueKind.List));
        Assert.That(split.ListValue.Count, Is.EqualTo(3));
        AssertString(split.ListValue[1], "b");
        AssertString(evaluator.Evaluate(ParseExpression("\"a,b,c\" | split(\",\") | join(\"|\")")), "a|b|c");
        AssertString(evaluator.Evaluate(ParseExpression("\"abcdef\" | slice(2, 3)")), "cde");
        AssertString(evaluator.Evaluate(ParseExpression("\"abcdef\" | slice(-2, 2)")), "ef");
        AssertString(evaluator.Evaluate(ParseExpression("\"abcdef\" | slice(2, 2147483647)")), "cdef");
        AssertString(evaluator.Evaluate(ParseExpression("\"abcdef\" | truncate(5)")), "ab...");
        AssertString(evaluator.Evaluate(ParseExpression("\"one two three\" | truncate_words(2)")), "one two...");
    }

    [Test]
    public void Evaluate_RangeAtMaximumInteger_DoesNotAdvancePastEndpoint()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        TemplateValue range = evaluator.Evaluate(ParseExpression("2147483647..2147483647"));

        Assert.That(range.ListValue.Count, Is.EqualTo(1));
        AssertNumber(range.ListValue[0], 2147483647m);
    }

    [Test]
    public void Evaluate_BuiltInEncodingFilters_EscapeForHtmlJsonUrlAndCsv()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertSafeString(evaluator.Evaluate(ParseExpression("\"<a title=\\\"Tom & Ada\\\">\" | html_escape")), "&lt;a title=&quot;Tom &amp; Ada&quot;&gt;");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"<b>\" | escape")), "&lt;b&gt;");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"<i>\" | e")), "&lt;i&gt;");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a\\\"b\\nc\" | json_escape")), "a\\\"b\\nc");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a b&c=/\" | url_encode")), "a%20b%26c%3D%2F");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a,b\\\"c\" | csv_escape")), "\"a,b\"\"c\"");
    }

    [Test]
    public void Evaluate_BuiltInEncodingFilters_SupportShortAliasesAndSafe()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertSafeString(evaluator.Evaluate(ParseExpression("\"<b>\" | html")), "&lt;b&gt;");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a\\\"b\" | json")), "\"a\\u0022b\"");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a b\" | url")), "a%20b");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a,b\" | csv")), "\"a,b\"");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"a b\" | urlencode")), "a%20b");
        AssertSafeString(evaluator.Evaluate(ParseExpression("\"<b>safe</b>\" | safe")), "<b>safe</b>");
    }

    [Test]
    public void Evaluate_BuiltInNumberFilters_ApplyArithmeticAndRounding()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertNumber(evaluator.Evaluate(ParseExpression("(0 - 3.2) | abs")), 3.2m);
        AssertNumber(evaluator.Evaluate(ParseExpression("3.2 | ceil")), 4m);
        AssertNumber(evaluator.Evaluate(ParseExpression("3.8 | floor")), 3m);
        AssertNumber(evaluator.Evaluate(ParseExpression("3.145 | round(2)")), 3.15m);
        AssertNumber(evaluator.Evaluate(ParseExpression("7 | plus(5)")), 12m);
        AssertNumber(evaluator.Evaluate(ParseExpression("7 | minus(2)")), 5m);
        AssertNumber(evaluator.Evaluate(ParseExpression("7 | times(3)")), 21m);
        AssertNumber(evaluator.Evaluate(ParseExpression("7 | divided_by(2)")), 3.5m);
        AssertNumber(evaluator.Evaluate(ParseExpression("7 | modulo(4)")), 3m);
    }

    [Test]
    public void Evaluate_BuiltInNumberFilters_FormatUsingConfiguredCulture()
    {
        TemplateOptions options = new TemplateOptions();
        options.Culture = CultureInfo.GetCultureInfo("fr-FR");
        TemplateExpressionEvaluator evaluator = TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), options, new TemplateMemberResolver());

        AssertString(evaluator.Evaluate(ParseExpression("1234.5 | format(\"N1\")")), "1\u202f234,5");
        AssertString(evaluator.Evaluate(ParseExpression("1234.5 | number_format(\"F2\")")), "1234,50");
    }

    [Test]
    public void Evaluate_BuiltInDateTimeFilters_FormatObjectValuesAndStrings()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("started | date(\"yyyy-MM-dd\")")), "2026-08-04");
        AssertString(evaluator.Evaluate(ParseExpression("offsetStarted | datetime(\"yyyy-MM-dd HH:mm zzz\")")), "2026-08-04 09:15 -07:00");
        AssertString(evaluator.Evaluate(ParseExpression("duration | time")), "02:03:04");
        AssertString(evaluator.Evaluate(ParseExpression("\"2026-08-04T09:15:30Z\" | date_format(\"yyyy\")")), "2026");
        AssertString(evaluator.Evaluate(ParseExpression("\"02:03:04\" | time_format")), "02:03:04");
    }

    [Test]
    public void Evaluate_BuiltInDateTimeFilters_RejectOutOfRangeNumericTimestamps()
    {
        TemplateExpressionSyntax expression = ParseExpression("9223372036854775808 | date");
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());
        TemplateOptions strictOptions = new TemplateOptions();
        strictOptions.StrictTypeConversions = true;
        TemplateExpressionEvaluator strictEvaluator = TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), strictOptions, new TemplateMemberResolver());

        AssertString(evaluator.Evaluate(expression), "");
        Assert.Throws<InvalidOperationException>(() => strictEvaluator.Evaluate(expression));
    }

    [Test]
    public void Evaluate_BuiltInCollectionFilters_SelectMeasureReorderAndCleanLists()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        TemplateValue firstItem = evaluator.Evaluate(ParseExpression("items | first"));
        TemplateValue lastItem = evaluator.Evaluate(ParseExpression("items | last"));

        AssertString(firstItem.GetMapItem("name"), "First");
        AssertString(lastItem.GetMapItem("name"), "Second");
        AssertNumber(evaluator.Evaluate(ParseExpression("items | size")), 2m);
        AssertNumber(evaluator.Evaluate(ParseExpression("\"abc\" | length")), 3m);
        AssertString(evaluator.Evaluate(ParseExpression("\"abc\" | reverse | join(\"-\")")), "c-b-a");
        AssertString(evaluator.Evaluate(ParseExpression("[\"b\", \"a\", \"c\"] | sort | join(\",\")")), "a,b,c");
        AssertString(evaluator.Evaluate(ParseExpression("[\"b\", \"A\", \"c\"] | sort_natural | join(\",\")")), "A,b,c");
        AssertString(evaluator.Evaluate(ParseExpression("[\"b\", \"a\", \"b\"] | uniq | join(\",\")")), "b,a");
        AssertString(evaluator.Evaluate(ParseExpression("[null, \"a\", null, \"b\"] | compact | join(\",\")")), "a,b");
    }

    [Test]
    public void Evaluate_BuiltInCollectionFilters_MapWhereSortAndSumByMember()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("people | map(\"name\") | join(\",\")")), "Grace,Ada,Linus,");
        AssertString(evaluator.Evaluate(ParseExpression("people | where(\"active\") | map(\"name\") | join(\",\")")), "Ada,Linus");
        AssertString(evaluator.Evaluate(ParseExpression("people | where(\"name\", \"Ada\") | map(\"name\") | join(\",\")")), "Ada");
        AssertString(evaluator.Evaluate(ParseExpression("people | sort(\"score\") | map(\"name\") | join(\",\")")), ",Grace,Linus,Ada");
        AssertString(evaluator.Evaluate(ParseExpression("people | compact(\"name\") | map(\"name\") | join(\",\")")), "Grace,Ada,Linus");
        AssertNumber(evaluator.Evaluate(ParseExpression("people | where(\"active\") | sum(\"score\")")), 17m);
    }

    [Test]
    public void Evaluate_BuiltInObjectFilters_ReturnKeysValuesPresenceAndDefaults()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        AssertString(evaluator.Evaluate(ParseExpression("settings | keys | sort | join(\",\")")), "enabled,theme");
        AssertString(evaluator.Evaluate(ParseExpression("settings | values | sort_natural | join(\",\")")), "dark,true");
        Assert.That(evaluator.Evaluate(ParseExpression("settings | has_key(\"theme\")")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("settings | hasKey(\"missing\")")).BooleanValue, Is.False);
        AssertString(evaluator.Evaluate(ParseExpression("missing | default(\"fallback\")")), "fallback");
        AssertString(evaluator.Evaluate(ParseExpression("empty | default(\"fallback\")")), "fallback");
        AssertString(evaluator.Evaluate(ParseExpression("\"value\" | default(\"fallback\")")), "value");
    }

    [Test]
    public void Evaluate_Tests_InvokeBuiltInAndRegisteredPredicates()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());
        evaluator.RegisterTest("greaterthan", delegate (List<TemplateValue> arguments)
        {
            return TemplateValue.FromBoolean(arguments[0].NumberValue > arguments[1].NumberValue);
        });

        Assert.That(evaluator.Evaluate(ParseExpression("user.name is defined")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("missing is missing")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("\"\" is empty")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("\"x\" is not empty")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is greaterthan(10)")).BooleanValue, Is.True);
    }

    [Test]
    public void Evaluate_BuiltInTypeTests_CheckRuntimeValueKinds()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        Assert.That(evaluator.Evaluate(ParseExpression("score is number")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("user.name is string")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("ok is boolean")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("items is list")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("items is array")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("settings is map")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("started is object")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("safeText is safe")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is not string")).BooleanValue, Is.True);
    }

    [Test]
    public void Evaluate_BuiltInNumericAndEqualityTests_CheckPredicatesAndNegation()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        Assert.That(evaluator.Evaluate(ParseExpression("3 is odd")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("4 is even")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("3.5 is odd")).BooleanValue, Is.False);
        Assert.That(evaluator.Evaluate(ParseExpression("9 is divisible by 3")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("10 is not divisible by 3")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is equal to 42")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is not equal to 41")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is equal_to(42)")).BooleanValue, Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("score is divisible_by(7)")).BooleanValue, Is.True);
    }

    [Test]
    public void Evaluate_Functions_CanResolveTemplateFunctionValuesFromScope()
    {
        Dictionary<string, TemplateValue> root = CreateRootMap();
        root["countArgs"] = TemplateValue.FromFunction(delegate (List<TemplateValue> arguments)
        {
            return TemplateValue.FromNumber(arguments.Count);
        });
        TemplateExpressionEvaluator evaluator = CreateEvaluator(TemplateValue.FromMap(root));

        AssertNumber(evaluator.Evaluate(ParseExpression("countArgs(1, 2, 3)")), 3m);
    }

    [Test]
    public void Evaluate_UnknownFunctionHonorsStrictUnknownFunctions()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictUnknownFunctions = true;
        TemplateExpressionEvaluator evaluator = TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), options, new TemplateMemberResolver());

        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(ParseExpression("missingFunction()")));
    }

    [Test]
    public void ConstructorsAndMutators_RejectNullRequiredValues()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(CreateRoot());

        Assert.Throws<ArgumentNullException>(() => TemplateExpressionEvaluator.Create(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionEvaluator.Create(null!, new TemplateOptions(), new TemplateMemberResolver()));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), null!, new TemplateMemberResolver()));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), new TemplateOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => evaluator.RegisterFunction(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => evaluator.RegisterFunction("id", null!));
        Assert.Throws<ArgumentNullException>(() => evaluator.RegisterFilter(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => evaluator.RegisterTest(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(null!));
    }

    static TemplateExpressionSyntax ParseExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static TemplateExpressionEvaluator CreateEvaluator(TemplateValue root)
    {
        return CreateEvaluator(TemplateScopeStack.Create(root));
    }

    static TemplateExpressionEvaluator CreateEvaluator(TemplateScopeStack scope)
    {
        return TemplateExpressionEvaluator.Create(scope);
    }

    static TemplateValue CreateRoot()
    {
        return TemplateValue.FromMap(CreateRootMap());
    }

    static Dictionary<string, TemplateValue> CreateRootMap()
    {
        Dictionary<string, TemplateValue> user = CreateMap("name", TemplateValue.FromString("Ada"));
        Dictionary<string, TemplateValue> site = CreateMap("title", TemplateValue.FromString("Root"));
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromMap(CreateMap("name", TemplateValue.FromString("First"))));
        items.Add(TemplateValue.FromMap(CreateMap("name", TemplateValue.FromString("Second"))));
        List<TemplateValue> people = new List<TemplateValue>();
        people.Add(CreatePerson("Grace", false, 3m));
        people.Add(CreatePerson("Ada", true, 10m));
        people.Add(CreatePerson("Linus", true, 7m));
        people.Add(CreatePerson(null, false, 1m));
        Dictionary<string, TemplateValue> settings = new Dictionary<string, TemplateValue>();
        settings["theme"] = TemplateValue.FromString("dark");
        settings["enabled"] = TemplateValue.FromBoolean(true);

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["user"] = TemplateValue.FromMap(user);
        root["site"] = TemplateValue.FromMap(site);
        root["items"] = TemplateValue.FromList(items);
        root["people"] = TemplateValue.FromList(people);
        root["settings"] = TemplateValue.FromMap(settings);
        root["score"] = TemplateValue.FromNumber(42m);
        root["ok"] = TemplateValue.FromBoolean(true);
        root["first"] = TemplateValue.FromString("Ada");
        root["last"] = TemplateValue.FromString("Lovelace");
        root["safeText"] = TemplateValue.FromSafeString("<b>safe</b>");
        root["started"] = TemplateValue.FromObject(new DateTime(2026, 8, 4, 9, 15, 30));
        root["offsetStarted"] = TemplateValue.FromObject(new DateTimeOffset(2026, 8, 4, 9, 15, 30, TimeSpan.FromHours(-7)));
        root["duration"] = TemplateValue.FromObject(new TimeSpan(2, 3, 4));
        return root;
    }

    static Dictionary<string, TemplateValue> CreateMap(string key, TemplateValue value)
    {
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map[key] = value;
        return map;
    }

    static TemplateValue CreatePerson(string? name, bool active, decimal score)
    {
        Dictionary<string, TemplateValue> person = new Dictionary<string, TemplateValue>();
        if (name == null)
        {
            person["name"] = TemplateValue.Null();
        }
        else
        {
            person["name"] = TemplateValue.FromString(name);
        }

        person["active"] = TemplateValue.FromBoolean(active);
        person["score"] = TemplateValue.FromNumber(score);
        return TemplateValue.FromMap(person);
    }

    static Func<List<TemplateValue>, TemplateValue> CreateIdentityFunction()
    {
        return delegate (List<TemplateValue> arguments)
        {
            return arguments[0];
        };
    }

    static void AssertNumber(TemplateValue value, decimal expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.Number));
        Assert.That(value.NumberValue, Is.EqualTo(expected));
    }

    static void AssertString(TemplateValue value, string expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(value.StringValue, Is.EqualTo(expected));
    }

    static void AssertSafeString(TemplateValue value, string expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.SafeString));
        Assert.That(value.StringValue, Is.EqualTo(expected));
    }
}
