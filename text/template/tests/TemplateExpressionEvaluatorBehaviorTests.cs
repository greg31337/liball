using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateExpressionEvaluatorBehaviorTests
{
    [Test]
    public void Evaluate_MissingValues_ReturnMissingByDefault()
    {
        TemplateExpressionEvaluator evaluator = CreateEvaluator(new TemplateOptions());

        Assert.That(evaluator.Evaluate(ParseExpression("missing")).IsMissing(), Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("user.missing")).IsMissing(), Is.True);
        Assert.That(evaluator.Evaluate(ParseExpression("nullUser.name")).IsMissing(), Is.True);
        AssertString(evaluator.Evaluate(ParseExpression("missing ?? \"fallback\"")), "fallback");
    }

    [Test]
    public void Evaluate_NullSafePaths_ReturnNullForMissingOrNullTraversal()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateExpressionEvaluator evaluator = CreateEvaluator(options);

        Assert.That(evaluator.Evaluate(ParseExpression("missing?.name")).Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(evaluator.Evaluate(ParseExpression("nullUser?.name")).Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(evaluator.Evaluate(ParseExpression("items?[5].name")).Kind, Is.EqualTo(TemplateValueKind.Null));
        AssertString(evaluator.Evaluate(ParseExpression("user?.name")), "Ada");
    }

    [Test]
    public void Evaluate_NullSafePathParser_MarksSafeMemberAndIndexerSegments()
    {
        TemplateExpressionSyntax member = ParseExpression("user?.name");
        TemplateExpressionSyntax indexer = ParseExpression("items?[0]");

        Assert.That(member.PathSegments[1].NullSafe, Is.True);
        Assert.That(member.PathSegments[1].Text, Is.EqualTo("name"));
        Assert.That(indexer.PathSegments[1].NullSafe, Is.True);
        Assert.That(indexer.PathSegments[1].SegmentKind, Is.EqualTo(TemplatePathSegmentSyntaxKind.Indexer));
    }

    [Test]
    public void Evaluate_Conversions_UseLenientDefaultsAndStrictTypeConversions()
    {
        TemplateExpressionEvaluator lenient = CreateEvaluator(new TemplateOptions());
        TemplateOptions strictOptions = new TemplateOptions();
        strictOptions.StrictTypeConversions = true;
        TemplateExpressionEvaluator strict = CreateEvaluator(strictOptions);

        AssertNumber(lenient.Evaluate(ParseExpression("true + 2")), 3m);
        AssertNumber(lenient.Evaluate(ParseExpression("\"x\" - 1")), -1m);
        Assert.Throws<InvalidOperationException>(() => strict.Evaluate(ParseExpression("\"x\" - 1")));
    }

    [Test]
    public void Evaluate_Truthiness_UsesConfiguredTemplateOptions()
    {
        TemplateOptions nativeOptions = new TemplateOptions();
        nativeOptions.TruthinessMode = TemplateTruthinessMode.Native;
        TemplateExpressionEvaluator native = CreateEvaluator(nativeOptions);
        TemplateOptions liquidOptions = new TemplateOptions();
        liquidOptions.TruthinessMode = TemplateTruthinessMode.Liquid;
        TemplateExpressionEvaluator liquid = CreateEvaluator(liquidOptions);

        AssertString(native.Evaluate(ParseExpression("0 ? \"yes\" : \"no\"")), "no");
        AssertString(liquid.Evaluate(ParseExpression("0 ? \"yes\" : \"no\"")), "yes");
        Assert.That(native.Evaluate(ParseExpression("0 is truthy")).BooleanValue, Is.False);
        Assert.That(liquid.Evaluate(ParseExpression("0 is truthy")).BooleanValue, Is.True);
    }

    [Test]
    public void Evaluate_StrictMissingValues_ThrowsForUnsafeMissingAccess()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateExpressionEvaluator evaluator = CreateEvaluator(options);

        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(ParseExpression("missing")));
        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(ParseExpression("user.missing")));
        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(ParseExpression("nullUser.name")));
        Assert.That(evaluator.Evaluate(ParseExpression("missing?.name")).Kind, Is.EqualTo(TemplateValueKind.Null));
    }

    [Test]
    public void Evaluate_StrictUnknownFunctions_AlsoAppliesToFiltersAndTests()
    {
        TemplateExpressionEvaluator lenient = CreateEvaluator(new TemplateOptions());
        TemplateOptions strictOptions = new TemplateOptions();
        strictOptions.StrictUnknownFunctions = true;
        TemplateExpressionEvaluator strict = CreateEvaluator(strictOptions);

        Assert.That(lenient.Evaluate(ParseExpression("\"x\" | unknown")).IsMissing(), Is.True);
        Assert.Throws<InvalidOperationException>(() => strict.Evaluate(ParseExpression("\"x\" | unknown")));
        Assert.Throws<InvalidOperationException>(() => strict.Evaluate(ParseExpression("\"x\" is unknown")));
    }

    static TemplateExpressionSyntax ParseExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static TemplateExpressionEvaluator CreateEvaluator(TemplateOptions options)
    {
        return TemplateExpressionEvaluator.Create(TemplateScopeStack.Create(CreateRoot()), options, new TemplateMemberResolver());
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString("Ada");

        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromMap(user));

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["user"] = TemplateValue.FromMap(user);
        root["nullUser"] = TemplateValue.Null();
        root["items"] = TemplateValue.FromList(items);
        return TemplateValue.FromMap(root);
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
