namespace uri.Tests;

public class TemplateParserBoundaryTests
{
    [Test]
    public void UnicodeLiteralsAroundAdjacentExpressionsRetainUtf16Ranges()
    {
        const string text = "α😀{x}{y}尾\uE000";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.OriginalText, Is.EqualTo(text));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(4));
            AssertPart(
                result.Template.Parts[0],
                UriTemplatePartKind.Literal,
                0,
                3);
            Assert.That(result.Template.Parts[0].Literal, Is.EqualTo("α😀"));
            AssertPart(
                result.Template.Parts[1],
                UriTemplatePartKind.Expression,
                3,
                3);
            AssertPart(
                result.Template.Parts[2],
                UriTemplatePartKind.Expression,
                6,
                3);
            AssertPart(
                result.Template.Parts[3],
                UriTemplatePartKind.Literal,
                9,
                2);
            Assert.That(result.Template.Parts[3].Literal, Is.EqualTo("尾\uE000"));
        });
    }

    [Test]
    public void PercentEncodedNamesCombineWithDotsAndModifiersWithoutDecoding()
    {
        const string text = "{?a.%32,b%2Ec,%41:1,d.%2e*}";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level4));
            UriTemplateVariable[] variables =
                result.Template.Parts[0].Variables;
            Assert.That(variables, Has.Length.EqualTo(4));
            Assert.That(variables[0].Name, Is.EqualTo("a.%32"));
            Assert.That(variables[1].Name, Is.EqualTo("b%2Ec"));
            Assert.That(variables[2].Name, Is.EqualTo("%41"));
            Assert.That(variables[2].HasPrefix, Is.True);
            Assert.That(variables[2].PrefixLength, Is.EqualTo(1));
            Assert.That(variables[3].Name, Is.EqualTo("d.%2e"));
            Assert.That(variables[3].Explode, Is.True);
        });
    }

    [TestCase("{}", 1, 0)]
    [TestCase("{+}", 2, 0)]
    [TestCase("{?}", 2, 0)]
    public void EmptyVariableListsIdentifyTheInsertionPoint(
        string text,
        int offset,
        int length)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateExpression,
            UriComponent.TemplateExpression,
            offset,
            length,
            TemplateParserCore.EmptyExpressionMessage);
    }

    [TestCase("{a,,b}", 3)]
    [TestCase("{a,}", 2)]
    [TestCase("{?a,,b}", 4)]
    [TestCase("{?a,}", 3)]
    public void EmptyCommaSeparatedItemsIdentifyTheComma(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateVariable,
            UriComponent.TemplateVariable,
            offset,
            1,
            TemplateParserCore.InvalidVariableMessage);
    }

    [Test]
    public void LeadingCommaIsTheReservedFutureOperatorNotAnEmptyItem()
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            "{,a}".AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.UnsupportedTemplateOperator,
            UriComponent.TemplateExpression,
            1,
            1,
            TemplateParserCore.UnsupportedOperatorMessage);
    }

    [TestCase("pre}post", 3, 1, TemplateParserCore.UnmatchedClosingBraceMessage)]
    [TestCase("pre{value", 3, 6, TemplateParserCore.UnclosedExpressionMessage)]
    [TestCase("pre{a{b}c}", 5, 1, TemplateParserCore.NestedExpressionMessage)]
    public void BraceFailuresUseAbsoluteOffsetsAfterLiteralText(
        string text,
        int offset,
        int length,
        string message)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateExpression,
            UriComponent.TemplateExpression,
            offset,
            length,
            message);
    }

    [TestCase('=')]
    [TestCase(',')]
    [TestCase('!')]
    [TestCase('@')]
    [TestCase('|')]
    public void FutureOperatorsUseAbsoluteOffsetsAfterLiteralText(char value)
    {
        string text = "pre{" + value + "name}";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.UnsupportedTemplateOperator,
            UriComponent.TemplateExpression,
            4,
            1,
            TemplateParserCore.UnsupportedOperatorMessage);
    }

    [Test]
    public void PrefixEndpointsCoexistInOneOrderedVariableList()
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            "{a:1,b:9999}".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level4));
            UriTemplateVariable[] variables =
                result.Template.Parts[0].Variables;
            Assert.That(variables, Has.Length.EqualTo(2));
            AssertPrefix(variables[0], "a", 1, 3, 1);
            AssertPrefix(variables[1], "b", 5, 6, 9999);
        });
    }

    [TestCase("{?ok,bad:0}", 9, TemplateParserCore.InvalidModifierMessage)]
    [TestCase("pre{/ok,bad:10000}post", 16, TemplateParserCore.InvalidModifierMessage)]
    [TestCase("{a*,b:1*}", 7, TemplateParserCore.IncompatibleModifierMessage)]
    [TestCase("{a:1,b*:2}", 7, TemplateParserCore.IncompatibleModifierMessage)]
    public void InvalidLaterModifiersReportAbsoluteOffendingCharacters(
        string text,
        int offset,
        string message)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateModifier,
            UriComponent.TemplateVariable,
            offset,
            1,
            message);
    }

    private static void AssertPart(
        UriTemplatePart part,
        UriTemplatePartKind kind,
        int offset,
        int length)
    {
        Assert.That(part.Kind, Is.EqualTo(kind));
        Assert.That(part.Range.Offset, Is.EqualTo(offset));
        Assert.That(part.Range.Length, Is.EqualTo(length));
    }

    private static void AssertPrefix(
        UriTemplateVariable variable,
        string name,
        int offset,
        int length,
        int prefixLength)
    {
        Assert.That(variable.Name, Is.EqualTo(name));
        Assert.That(variable.Range.Offset, Is.EqualTo(offset));
        Assert.That(variable.Range.Length, Is.EqualTo(length));
        Assert.That(variable.HasPrefix, Is.True);
        Assert.That(variable.PrefixLength, Is.EqualTo(prefixLength));
        Assert.That(variable.Explode, Is.False);
    }

    private static void AssertFailure(
        UriTemplateParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Template.OriginalText, Is.Empty);
        Assert.That(result.Template.Parts, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
            DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
