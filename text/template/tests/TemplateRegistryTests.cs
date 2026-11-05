using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateRegistryTests
{
    [Test]
    public void RegisterCallables_StoresFunctionsFiltersTestsHelpersAndBlockHelpers()
    {
        TemplateRegistry registry = new TemplateRegistry();
        List<TemplateValue> arguments = new List<TemplateValue>();
        arguments.Add(TemplateValue.FromString("Ada"));
        List<TemplateSyntaxNode> body = new List<TemplateSyntaxNode>();
        List<TemplateSyntaxNode> elseBody = new List<TemplateSyntaxNode>();

        registry.RegisterFunction("hello", delegate(List<TemplateValue> values)
        {
            return TemplateValue.FromString("Hello " + values[0].StringValue);
        });
        registry.RegisterFilter("bang", delegate(List<TemplateValue> values)
        {
            return TemplateValue.FromString(values[0].StringValue + "!");
        });
        registry.RegisterTest("longer", delegate(List<TemplateValue> values)
        {
            return TemplateValue.FromBoolean(values[0].StringValue.Length > values[1].NumberValue);
        });
        registry.RegisterHelper("helper", delegate(List<TemplateValue> values)
        {
            return TemplateValue.FromString("helper " + values[0].StringValue);
        });
        registry.RegisterBlockHelper("block", delegate(TemplateBlockHelperContext context)
        {
            return TemplateValue.FromNumber(context.Arguments.Count + context.Body.Count + context.ElseBody.Count);
        });

        TemplateBlockHelperContext blockContext = new TemplateBlockHelperContext(arguments, body, elseBody, null);

        AssertString(registry.Functions["hello"](arguments), "Hello Ada");
        AssertString(registry.Filters["bang"](arguments), "Ada!");
        Assert.That(registry.Tests["longer"](CreateArguments(TemplateValue.FromString("Ada"), TemplateValue.FromNumber(2m))).BooleanValue, Is.True);
        AssertString(registry.Helpers["helper"](arguments), "helper Ada");
        AssertNumber(registry.BlockHelpers["block"](blockContext), 1m);
    }

    [Test]
    public void RegisterPartial_StoresNamedTemplateSourcesAndReturnsCopies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        TemplateSource source = new TemplateSource("source", "/templates/source.tpl", "v7", "hash", "Source {{ name }}");
        source.Timestamp = "time-7";

        registry.RegisterPartial("card", "Card {{ name }}");
        registry.RegisterPartial("partials\\badge", "Badge {{ name }}");
        registry.RegisterPartialSource("source-card", source);
        TemplateSource card = registry.LoadPartial("card")!;
        TemplateSource badge = registry.LoadPartial("partials/badge")!;
        TemplateSource sourceCard = registry.LoadPartial("source-card")!;
        card.Text = "changed";

        Assert.That(registry.ContainsPartial("card"), Is.True);
        Assert.That(registry.ContainsPartial("partials/badge"), Is.True);
        Assert.That(registry.ContainsPartial("missing"), Is.False);
        Assert.That(card.Name, Is.EqualTo("card"));
        Assert.That(card.Version, Is.EqualTo("1"));
        Assert.That(card.ContentHash, Is.Not.Empty);
        Assert.That(badge.Name, Is.EqualTo("partials/badge"));
        Assert.That(badge.Text, Is.EqualTo("Badge {{ name }}"));
        Assert.That(sourceCard.Timestamp, Is.EqualTo("time-7"));
        Assert.That(sourceCard.Name, Is.EqualTo("source-card"));
        Assert.That(sourceCard.Path, Is.EqualTo("/templates/source.tpl"));
        Assert.That(sourceCard.Version, Is.EqualTo("v7"));
        Assert.That(sourceCard.ContentHash, Is.EqualTo("hash"));
        Assert.That(registry.LoadPartial("card")!.Text, Is.EqualTo("Card {{ name }}"));
        Assert.That(registry.GetPartialNames(), Does.Contain("card"));
        Assert.That(registry.RemovePartial("card"), Is.True);
        Assert.That(registry.RemovePartial("card"), Is.False);
    }

    [Test]
    public void ApplyTo_CopiesRegistryCallablesIntoEvaluator()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("hello", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("Hello " + arguments[0].StringValue);
        });
        registry.RegisterFilter("bang", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString(arguments[0].StringValue + "!");
        });
        registry.RegisterTest("same", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromBoolean(arguments[0].StringValue == arguments[1].StringValue);
        });
        registry.RegisterHelper("helperHello", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("Helper " + arguments[0].StringValue);
        });

        TemplateExpressionEvaluator evaluator = new TemplateExpressionEvaluator(TemplateScopeStack.Create(CreateRoot()), new TemplateOptions(), new TemplateMemberResolver(), registry);

        AssertString(evaluator.Evaluate(ParseExpression("hello(user.name)")), "Hello Ada");
        AssertString(evaluator.Evaluate(ParseExpression("user.name | bang")), "Ada!");
        Assert.That(evaluator.Evaluate(ParseExpression("user.name is same(\"Ada\")")).BooleanValue, Is.True);
        AssertString(evaluator.Evaluate(ParseExpression("helperHello(user.name)")), "Helper Ada");
    }

    [Test]
    public void Renderer_UsesRegisteredFunctionsFiltersAndTests()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("hello", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("Hello " + arguments[0].StringValue);
        });
        registry.RegisterFilter("bang", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString(arguments[0].StringValue + "!");
        });
        registry.RegisterTest("same", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromBoolean(arguments[0].StringValue == arguments[1].StringValue);
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{{ hello(user.name) }} {{ user.name | bang }} {{ user.name is same(\"Ada\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada Ada! true"));
    }

    [Test]
    public void ConstructorsAndRegistration_RejectNullRequiredValues()
    {
        TemplateRegistry registry = new TemplateRegistry();
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        List<TemplateValue> arguments = new List<TemplateValue>();
        List<TemplateSyntaxNode> body = new List<TemplateSyntaxNode>();
        List<TemplateSyntaxNode> elseBody = new List<TemplateSyntaxNode>();

        Assert.Throws<ArgumentNullException>(() => TemplateRenderer.Create((TemplateRegistry)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderer.Create(new TemplateOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(CreateRoot(), writer, new TemplateOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateExpressionEvaluator(TemplateScopeStack.Create(CreateRoot()), new TemplateOptions(), new TemplateMemberResolver(), null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateBlockHelperContext(null!, body, elseBody, null));
        Assert.Throws<ArgumentNullException>(() => new TemplateBlockHelperContext(arguments, null!, elseBody, null));
        Assert.Throws<ArgumentNullException>(() => new TemplateBlockHelperContext(arguments, body, null!, null));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterFunction(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterFunction("x", null!));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterFilter(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterTest(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterHelper(null!, CreateIdentityFunction()));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterBlockHelper(null!, CreateBlockHelper()));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterBlockHelper("x", null!));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterPartial(null!, "x"));
        Assert.Throws<ArgumentException>(() => registry.RegisterPartial("", "x"));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterPartial("x", (string)null!));
        Assert.Throws<ArgumentNullException>(() => registry.RegisterPartialSource("x", (TemplateSource)null!));
        Assert.Throws<InvalidOperationException>(() => registry.RegisterPartial("../x", "x"));
        Assert.Throws<InvalidOperationException>(() => registry.RegisterPartial("partials//x", "x"));
        Assert.Throws<InvalidOperationException>(() => registry.RegisterTemplate("/absolute", "x"));
        Assert.Throws<InvalidOperationException>(() => registry.ValidateTemplateName("partials/./x"));
        Assert.Throws<InvalidOperationException>(() => registry.ValidateTemplateName("c:name"));
        Assert.Throws<ArgumentNullException>(() => registry.SetMemoryLoader(null!));
        Assert.Throws<ArgumentNullException>(() => registry.SetFileLoader(null!));
        Assert.Throws<ArgumentNullException>(() => registry.LoadPartial(null!));
        Assert.Throws<ArgumentNullException>(() => registry.ContainsPartial(null!));
        Assert.Throws<ArgumentNullException>(() => registry.RemovePartial(null!));
        Assert.Throws<ArgumentNullException>(() => registry.ApplyTo(null!));
    }

    static TemplateExpressionSyntax ParseExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString("Ada");
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["user"] = TemplateValue.FromMap(user);
        return TemplateValue.FromMap(root);
    }

    static List<TemplateValue> CreateArguments(TemplateValue first, TemplateValue second)
    {
        List<TemplateValue> arguments = new List<TemplateValue>();
        arguments.Add(first);
        arguments.Add(second);
        return arguments;
    }

    static Func<List<TemplateValue>, TemplateValue> CreateIdentityFunction()
    {
        return delegate(List<TemplateValue> arguments)
        {
            return arguments[0];
        };
    }

    static Func<TemplateBlockHelperContext, TemplateValue> CreateBlockHelper()
    {
        return delegate(TemplateBlockHelperContext context)
        {
            return TemplateValue.FromNumber(context.Arguments.Count);
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
}
