using System;
using System.Collections.Generic;
using System.Globalization;

namespace template.Tests;

[TestFixture]
public class PublicBuiltInCallableTests
{
    static object[] StringFilterCases = new object[]
    {
        Case("{{ \"Ab c\" | upper }}", "AB C"),
        Case("{{ \"Ab c\" | upcase }}", "AB C"),
        Case("{{ \"Ab C\" | lower }}", "ab c"),
        Case("{{ \"Ab C\" | downcase }}", "ab c"),
        Case("{{ \"aDA\" | capitalize }}", "Ada"),
        Case("{{ \"hello WORLD\" | title }}", "Hello World"),
        Case("{{ \"  x  \" | trim }}", "x"),
        Case("{{ \"  x  \" | strip }}", "x"),
        Case("{{ \"  x  \" | ltrim }}", "x  "),
        Case("{{ \"  x  \" | lstrip }}", "x  "),
        Case("{{ \"  x  \" | rtrim }}", "  x"),
        Case("{{ \"  x  \" | rstrip }}", "  x"),
        Case("{{ \"banana\" | replace(\"na\", \"NA\") }}", "baNANA"),
        Case("{{ \"banana\" | remove(\"na\") }}", "ba"),
        Case("{{ \"a\" | append(\"b\") }}", "ab"),
        Case("{{ \"b\" | prepend(\"a\") }}", "ab"),
        Case("{{ \"a\\nb\" | newline_to_br }}", "a<br>b"),
        Case("{{ \"a\\nb\" | newlineToBr }}", "a<br>b"),
        Case("{{ \"a,b\" | split(\",\") | join(\"|\") }}", "a|b"),
        Case("{{ [\"a\", \"b\"] | join(\"|\") }}", "a|b"),
        Case("{{ \"abcd\" | slice(1, 2) }}", "bc"),
        Case("{{ \"abcdef\" | truncate(5) }}", "ab..."),
        Case("{{ \"one two three\" | truncate_words(2) }}", "one two..."),
        Case("{{ \"one two three\" | truncateWords(2) }}", "one two..."),
        Case("{{ \"one two three\" | truncatewords(2) }}", "one two...")
    };

    static object[] EncodingFilterCases = new object[]
    {
        Case("{{ \"<&>\" | html }}", "&lt;&amp;&gt;"),
        Case("{{ \"<&>\" | html_escape }}", "&lt;&amp;&gt;"),
        Case("{{ \"<&>\" | escape }}", "&lt;&amp;&gt;"),
        Case("{{ \"<&>\" | e }}", "&lt;&amp;&gt;"),
        Case("{{ \"<&>\" | force_escape }}", "&lt;&amp;&gt;"),
        Case("{{ \"<&>\" | forceescape }}", "&lt;&amp;&gt;"),
        Case("{{ \"&lt;b&gt; &\" | escape_once }}", "&lt;b&gt; &amp;"),
        Case("{{ \"<b title='1 > 0'>Hi</b><!--x-->!\" | strip_html }}", "Hi!"),
        Case("{{ \"<b>Hi</b>\" | striptags }}", "Hi"),
        Case("{{ data | json }}", "{\"name\":\"Ada\",\"active\":true}"),
        Case("{{ data | to_json }}", "{\"name\":\"Ada\",\"active\":true}"),
        Case("{{ data | tojson }}", "{\"name\":\"Ada\",\"active\":true}"),
        Case("{{ \"a\\\"b\" | json_escape }}", "a\\\"b"),
        Case("{{ \"a b&\" | url }}", "a%20b%26"),
        Case("{{ \"a b&\" | url_encode }}", "a%20b%26"),
        Case("{{ \"a b&\" | urlencode }}", "a%20b%26"),
        Case("{{ \"a%20b%26c\" | url_decode }}", "a b&amp;c"),
        Case("{{ \"a+b\" | urldecode }}", "a b"),
        Case("{{ \"a,b\" | csv }}", "\"a,b\""),
        Case("{{ \"a,b\" | csv_escape }}", "\"a,b\""),
        Case("{{ \"<b>safe</b>\" | safe }}", "<b>safe</b>"),
        Case("{{ \"<b>raw</b>\" | raw }}", "<b>raw</b>")
    };

    static object[] NumberAndTemporalFilterCases = new object[]
    {
        Case("{{ (0 - 3.2) | abs }}", "3.2"),
        Case("{{ 3.2 | ceil }}", "4"),
        Case("{{ 3.2 | ceiling }}", "4"),
        Case("{{ 3.8 | floor }}", "3"),
        Case("{{ 3.145 | round(2) }}", "3.15"),
        Case("{{ 7 | plus(5) }}", "12"),
        Case("{{ 7 | minus(2) }}", "5"),
        Case("{{ 7 | times(3) }}", "21"),
        Case("{{ 7 | multiply(3) }}", "21"),
        Case("{{ 7 | divide(2) }}", "3.5"),
        Case("{{ 7 | divided_by(2) }}", "3.5"),
        Case("{{ 7 | modulo(4) }}", "3"),
        Case("{{ 7 | mod(4) }}", "3"),
        Case("{{ 12.5 | format(\"F1\") }}", "12.5"),
        Case("{{ 12.5 | number_format(\"F2\") }}", "12.50"),
        Case("{{ started | date(\"yyyy-MM-dd\") }}", "2026-08-04"),
        Case("{{ started | date_format(\"yyyy-MM-dd\") }}", "2026-08-04"),
        Case("{{ started | format_date(\"yyyy-MM-dd\") }}", "2026-08-04"),
        Case("{{ duration | time }}", "02:03:04"),
        Case("{{ duration | time_format }}", "02:03:04"),
        Case("{{ duration | format_time }}", "02:03:04"),
        Case("{{ started | datetime(\"yyyy-MM-dd HH:mm\") }}", "2026-08-04 09:15"),
        Case("{{ started | date_time(\"yyyy-MM-dd HH:mm\") }}", "2026-08-04 09:15"),
        Case("{{ started | datetime_format(\"yyyy-MM-dd HH:mm\") }}", "2026-08-04 09:15"),
        Case("{{ started | format_datetime(\"yyyy-MM-dd HH:mm\") }}", "2026-08-04 09:15")
    };

    static object[] CollectionAndObjectFilterCases = new object[]
    {
        Case("{{ letters | first }}", "b"),
        Case("{{ letters | last }}", "a"),
        Case("{{ letters | size }}", "3"),
        Case("{{ letters | length }}", "3"),
        Case("{{ letters | reverse | join(\",\") }}", "a,b,b"),
        Case("{{ letters | sort | join(\",\") }}", "a,b,b"),
        Case("{{ mixedCase | sort_natural | join(\",\") }}", "A,b,c"),
        Case("{{ mixedCase | sortNatural | join(\",\") }}", "A,b,c"),
        Case("{{ letters | uniq | join(\",\") }}", "b,a"),
        Case("{{ letters | unique | join(\",\") }}", "b,a"),
        Case("{{ nullable | compact | join(\",\") }}", "a,b"),
        Case("{{ people | map(\"name\") | join(\",\") }}", "Ada,Bea"),
        Case("{{ people | where(\"active\") | map(\"name\") | join(\",\") }}", "Ada"),
        Case("{{ people | sum(\"score\") }}", "12"),
        Case("{{ data | keys | sort | join(\",\") }}", "active,name"),
        Case("{{ data | values | sort_natural | join(\",\") }}", "Ada,true"),
        Case("{{ data | has_key(\"name\") }}", "true"),
        Case("{{ data | hasKey(\"name\") }}", "true"),
        Case("{{ data | haskey(\"name\") }}", "true"),
        Case("{{ missing | default(\"fallback\") }}", "fallback"),
        Case("{{ missing | d(\"fallback\") }}", "fallback"),
        Case("{{ null | default_if_none(\"fallback\") }}", "fallback")
    };

    static object[] PredicateCases = new object[]
    {
        Case("{{ name is defined }}", "true"),
        Case("{{ missing is missing }}", "true"),
        Case("{{ missing is undefined }}", "true"),
        Case("{{ null is null }}", "true"),
        Case("{{ null is none }}", "true"),
        Case("{{ \"\" is empty }}", "true"),
        Case("{{ name is truthy }}", "true"),
        Case("{{ false is falsy }}", "true"),
        Case("{{ true is true }}", "true"),
        Case("{{ false is false }}", "true"),
        Case("{{ name is not defined }}", "false"),
        Case("{{ name is not null }}", "true"),
        Case("{{ name is not empty }}", "true"),
        Case("{{ 3 is number }}", "true"),
        Case("{{ name is string }}", "true"),
        Case("{{ \"x\" | safe is safe }}", "true"),
        Case("{{ true is boolean }}", "true"),
        Case("{{ true is bool }}", "true"),
        Case("{{ letters is list }}", "true"),
        Case("{{ letters is array }}", "true"),
        Case("{{ data is map }}", "true"),
        Case("{{ data is mapping }}", "true"),
        Case("{{ letters is sequence }}", "true"),
        Case("{{ data is iterable }}", "true"),
        Case("{{ started is object }}", "true"),
        Case("{{ 3 is odd }}", "true"),
        Case("{{ 4 is even }}", "true"),
        Case("{{ 9 is divisible_by(3) }}", "true"),
        Case("{{ 9 is divisibleby(3) }}", "true"),
        Case("{{ 9 is divisible by 3 }}", "true"),
        Case("{{ 3 is equal_to(3) }}", "true"),
        Case("{{ 3 is equalto(3) }}", "true"),
        Case("{{ 3 is equal to 3 }}", "true"),
        Case("{{ 3 is eq(3) }}", "true")
    };

    [TestCaseSource(nameof(StringFilterCases))]
    public void Render_StringFilterAndAlias(string templateText, string expected)
    {
        Assert.That(new TemplateRenderer().RenderToString(templateText, CreateRoot()), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(EncodingFilterCases))]
    public void Render_EncodingFilterAndAlias(string templateText, string expected)
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = CreateRenderer(options);

        Assert.That(renderer.RenderToString(templateText, CreateRoot()), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(NumberAndTemporalFilterCases))]
    public void Render_NumberAndTemporalFilterAndAlias(string templateText, string expected)
    {
        Assert.That(new TemplateRenderer().RenderToString(templateText, CreateRoot()), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(CollectionAndObjectFilterCases))]
    public void Render_CollectionAndObjectFilterAndAlias(string templateText, string expected)
    {
        Assert.That(new TemplateRenderer().RenderToString(templateText, CreateRoot()), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(PredicateCases))]
    public void Render_PredicateAndAlias(string templateText, string expected)
    {
        Assert.That(new TemplateRenderer().RenderToString(templateText, CreateRoot()), Is.EqualTo(expected));
    }

    [Test]
    public void Render_CollectionSliceHandlesNegativeBoundsAndLargeLengths()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ letters | slice(-2, 2147483647) | join(\",\") }}|{{ letters | slice(1, 0) | join(\",\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("b,a|"));
    }

    [Test]
    public void Render_LiquidAliasesUseLiquidUrlDateAndDefaultSemantics()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateOptions options = catalog.CreateLiquidOptions();
        TemplateRenderer renderer = CreateRenderer(options);
        string templateText = "{{ \"a b\" | url_encode }}|{{ started | date: \"%Y-%m-%d %H:%M\" }}|{{ false | default: \"fallback\" }}|{{ false | default: \"fallback\", allow_false: true }}|{{ 7 | divided_by: 2 }}|{{ 7 | divided_by: 2.0 }}|{{ (0 - 7) | modulo: 4 }}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("a+b|2026-08-04 09:15|fallback|false|3|3.5|1"));
    }

    [Test]
    public void Render_JinjaDefaultAliasDistinguishesUndefinedFromFalseyValues()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateOptions options = catalog.CreateJinjaOptions();
        TemplateRenderer renderer = CreateRenderer(options);

        string result = renderer.RenderToString("{{ missing | d(\"fallback\") }}|{{ null | default(\"fallback\") }}|{{ \"\" | default(\"fallback\", true) }}", CreateRoot());

        Assert.That(result, Is.EqualTo("fallback||fallback"));
    }

    [Test]
    public void Render_StrictTemporalConversionAndZeroDivisorsFailClearly()
    {
        TemplateOptions strictOptions = new TemplateOptions();
        strictOptions.StrictTypeConversions = true;
        TemplateRenderer strictRenderer = CreateRenderer(strictOptions);
        TemplateRenderer renderer = new TemplateRenderer();

        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{{ \"not-a-date\" | date }}", CreateRoot()));
        Assert.Throws<DivideByZeroException>(() => renderer.RenderToString("{{ 1 | divide(0) }}", CreateRoot()));
        Assert.Throws<DivideByZeroException>(() => renderer.RenderToString("{{ 1 | modulo(0) }}", CreateRoot()));
    }

    static object[] Case(string templateText, string expected)
    {
        return new object[] { templateText, expected };
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, object?> data = new Dictionary<string, object?>();
        data["name"] = "Ada";
        data["active"] = true;

        List<object?> letters = new List<object?>();
        letters.Add("b");
        letters.Add("b");
        letters.Add("a");

        List<object?> mixedCase = new List<object?>();
        mixedCase.Add("b");
        mixedCase.Add("A");
        mixedCase.Add("c");

        List<object?> nullable = new List<object?>();
        nullable.Add(null);
        nullable.Add("a");
        nullable.Add(null);
        nullable.Add("b");

        List<object?> people = new List<object?>();
        people.Add(CreatePerson("Ada", true, 7));
        people.Add(CreatePerson("Bea", false, 5));

        Dictionary<string, object?> root = new Dictionary<string, object?>();
        root["name"] = "Ada";
        root["data"] = data;
        root["letters"] = letters;
        root["mixedCase"] = mixedCase;
        root["nullable"] = nullable;
        root["people"] = people;
        root["started"] = new DateTimeOffset(2026, 8, 4, 9, 15, 30, TimeSpan.Zero);
        root["duration"] = new TimeSpan(2, 3, 4);

        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(root);
    }

    static Dictionary<string, object?> CreatePerson(string name, bool active, int score)
    {
        Dictionary<string, object?> person = new Dictionary<string, object?>();
        person["name"] = name;
        person["active"] = active;
        person["score"] = score;
        return person;
    }
}
