using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateBuiltInRegistrationTests
{
    [Test]
    public void BuiltIns_ComposeAcrossFiltersAndTestsDuringRendering()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string template = "{{ \" ada \" | trim | upper }} {{ [3, 1, 2] | sort | join(\",\") }} {{ 5 | plus(1) is even }} {{ \"<\" | html_escape }}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("ADA 1,2,3 true &lt;"));
    }

    [Test]
    public void EvaluatorCustomRegistrations_OverrideBuiltIns()
    {
        TemplateExpressionEvaluator evaluator = TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()));
        evaluator.RegisterFunction("name", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("Ada");
        });
        evaluator.RegisterFilter("upper", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("custom " + arguments[0].StringValue);
        });
        evaluator.RegisterTest("number", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromBoolean(false);
        });

        AssertString(evaluator.Evaluate(ParseExpression("name()")), "Ada");
        AssertString(evaluator.Evaluate(ParseExpression("\"x\" | upper")), "custom x");
        Assert.That(evaluator.Evaluate(ParseExpression("42 is number")).BooleanValue, Is.False);
    }

    [Test]
    public void RegistryCustomRegistrations_OverrideBuiltInsDuringRendering()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("name", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("Ada");
        });
        registry.RegisterFilter("upper", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("custom " + arguments[0].StringValue);
        });
        registry.RegisterTest("number", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromBoolean(false);
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{{ name() }} {{ \"x\" | upper }} {{ 42 is number }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada custom x false"));
    }

    static TemplateExpressionSyntax ParseExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["value"] = TemplateValue.FromString("x");
        return TemplateValue.FromMap(root);
    }

    static void AssertString(TemplateValue value, string expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(value.StringValue, Is.EqualTo(expected));
    }
}
