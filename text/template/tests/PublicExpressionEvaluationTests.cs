using System;
using System.Collections;
using System.Collections.Generic;

namespace template.Tests;

[TestFixture]
public class PublicExpressionEvaluationTests
{
    [Test]
    public void Parse_ContainmentOperators_ExposeExpectedPublicOperatorKinds()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();

        AssertOperator(profiles.Parse("{{ text contains part }}", "native"), TemplateExpressionOperatorKind.Contains);
        AssertOperator(profiles.Parse("{{ text starts with part }}", "native"), TemplateExpressionOperatorKind.StartsWith);
        AssertOperator(profiles.Parse("{{ text starts-with part }}", "native"), TemplateExpressionOperatorKind.StartsWith);
        AssertOperator(profiles.Parse("{{ text ends_with part }}", "native"), TemplateExpressionOperatorKind.EndsWith);
    }

    [Test]
    public void Render_ContainmentOperators_HandleStringsListsAndMaps()
    {
        Hashtable model = new Hashtable();
        model["text"] = "template";
        model["letters"] = new string[] { "a", "b" };
        Hashtable settings = new Hashtable();
        settings["theme"] = "dark";
        model["settings"] = settings;
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ text contains \"plat\" }}|{{ text starts-with \"tem\" }}|{{ text ends with \"late\" }}|{{ letters contains \"b\" }}|{{ settings contains \"theme\" }}", Convert(model));

        Assert.That(result, Is.EqualTo("true|true|true|true|true"));
    }

    [Test]
    public void ParseAndRender_LiquidUsesRightToLeftOperatorAssociation()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateParseResult nativeParse = profiles.Parse("{{ false and true or true }}", "native");
        TemplateParseResult liquidParse = profiles.Parse("{{ false and true or true }}", "liquid");
        TemplateRenderer nativeRenderer = CreateRenderer(profiles.CreateOptions("native", TemplateCompatibilityMode.Pragmatic), new TemplateRegistry());
        TemplateRenderer liquidRenderer = CreateRenderer(profiles.CreateOptions("liquid", TemplateCompatibilityMode.Pragmatic), new TemplateRegistry());

        Assert.That(nativeParse.Document.Children[0].Output!.Expression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.Or));
        Assert.That(liquidParse.Document.Children[0].Output!.Expression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.And));
        Assert.That(nativeRenderer.RenderToString("{{ false and true or true }}", EmptyModel()), Is.EqualTo("true"));
        Assert.That(liquidRenderer.RenderToString("{{ false and true or true }}", EmptyModel()), Is.EqualTo("false"));
    }

    [Test]
    public void ParseAndRender_StandardNotBindsOutsideComparisonsAndInsideLogicalOperators()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateParseResult comparison = profiles.Parse("{{ not 1 == 2 }}", "native");
        TemplateParseResult logical = profiles.Parse("{{ not false and false }}", "native");
        TemplateRenderer renderer = new TemplateRenderer();

        Assert.That(comparison.Document.Children[0].Output!.Expression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.UnaryNot));
        Assert.That(comparison.Document.Children[0].Output!.Expression.Children[0].OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.Equal));
        Assert.That(logical.Document.Children[0].Output!.Expression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.And));
        Assert.That(renderer.RenderToString("{{ not 1 == 2 }}|{{ not false and false }}", EmptyModel()), Is.EqualTo("true|false"));
    }

    [Test]
    public void Render_CoalesceNullSafeAndConditional_ShortCircuitInStrictMode()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("explode", delegate (List<TemplateValue> arguments)
        {
            throw new InvalidOperationException("This branch must not run.");
        });
        TemplateRenderer renderer = CreateRenderer(options, registry);

        string result = renderer.RenderToString("{{ missing ?? \"fallback\" }}|{{ missing?.name ?? \"safe\" }}|{{ true ? \"yes\" : explode() }}|{{ not missing is defined }}", EmptyModel());

        Assert.That(result, Is.EqualTo("fallback|safe|yes|true"));
    }

    [Test]
    public void Render_RangesAndCollectionLiterals_HandleNegativeDescendingAndNamedKeys()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ -2..1 | join(\",\") }}|{{ 2..0 | join(\",\") }}|{{ [\"a\", \"b\"] | join(\"\") }}|{{ {name: \"Ada\"} | has_key(\"name\") }}", EmptyModel());

        Assert.That(result, Is.EqualTo("-2,-1,0,1|2,1,0|ab|true"));
    }

    [Test]
    public void Render_RangeAndArithmeticBoundaries_ReportInvalidOperations()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumLoopIterations = 3;
        options.StrictTypeConversions = true;
        TemplateRenderer renderer = CreateRenderer(options, new TemplateRegistry());

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ 1..4 | join(\",\") }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ 1 / 0 }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ 1 % 0 }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ 1 contains 1 }}", EmptyModel()));
    }

    [Test]
    public void Render_FunctionFiltersTestsAndSubexpressions_UseRegisteredPublicCallables()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("concat", delegate (List<TemplateValue> arguments)
        {
            return StringValue(arguments[0].StringValue + arguments[1].StringValue);
        });
        registry.RegisterFunction("wrap", delegate (List<TemplateValue> arguments)
        {
            return StringValue("[" + arguments[0].StringValue + "]");
        });
        registry.RegisterFilter("suffix", delegate (List<TemplateValue> arguments)
        {
            return StringValue(arguments[0].StringValue + arguments[1].StringValue);
        });
        registry.RegisterTest("long_enough", delegate (List<TemplateValue> arguments)
        {
            return BooleanValue(arguments[0].StringValue.Length >= arguments[1].NumberValue);
        });
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);

        string result = renderer.RenderToString("{{ wrap((concat \"A\" \"B\")) | suffix(\"!\") }}|{{ \"abcd\" is long_enough(4) }}", EmptyModel());

        Assert.That(result, Is.EqualTo("[AB]!|true"));
    }

    [Test]
    public void Render_NamedAndHashArguments_ArriveAsOneFinalMapArgument()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("describe", delegate (List<TemplateValue> arguments)
        {
            TemplateValue options = arguments[arguments.Count - 1];
            return StringValue(arguments[0].StringValue + ":" + options.MapValue["tone"].StringValue);
        });
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);

        string result = renderer.RenderToString("{{ describe(\"named\", tone=\"quiet\") }}|{{ describe \"hash\" tone=\"loud\" }}", EmptyModel());

        Assert.That(result, Is.EqualTo("named:quiet|hash:loud"));
    }

    static void AssertOperator(TemplateParseResult result, TemplateExpressionOperatorKind expected)
    {
        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children[0].Output!.Expression.OperatorKind, Is.EqualTo(expected));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options, TemplateRegistry registry)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, registry, new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue Convert(object value)
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(value);
    }

    static TemplateValue EmptyModel()
    {
        return Convert(new Hashtable());
    }

    static TemplateValue StringValue(string value)
    {
        TemplateValue result = new TemplateValue(TemplateValueKind.String);
        result.StringValue = value;
        return result;
    }

    static TemplateValue BooleanValue(bool value)
    {
        TemplateValue result = new TemplateValue(TemplateValueKind.Boolean);
        result.BooleanValue = value;
        return result;
    }
}
