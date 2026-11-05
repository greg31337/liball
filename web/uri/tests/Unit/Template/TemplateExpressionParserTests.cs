namespace uri.Tests;

public class TemplateExpressionParserTests
{
    [TestCase("{name}", UriTemplateOperator.Simple, 1)]
    [TestCase("{+name}", UriTemplateOperator.Reserved, 2)]
    [TestCase("{#name}", UriTemplateOperator.Fragment, 2)]
    [TestCase("{.name}", UriTemplateOperator.Label, 2)]
    [TestCase("{/name}", UriTemplateOperator.PathSegment, 2)]
    [TestCase("{;name}", UriTemplateOperator.PathParameter, 2)]
    [TestCase("{?name}", UriTemplateOperator.Query, 2)]
    [TestCase("{&name}", UriTemplateOperator.QueryContinuation, 2)]
    public void EveryDefinedOperatorProducesTheExpectedExpression(
        string text,
        UriTemplateOperator expectedOperator,
        int variableOffset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.OriginalText, Is.EqualTo(text));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(1));
            UriTemplatePart expression = result.Template.Parts[0];
            Assert.That(expression.Kind, Is.EqualTo(UriTemplatePartKind.Expression));
            Assert.That(expression.Operator, Is.EqualTo(expectedOperator));
            Assert.That(expression.Range.Offset, Is.EqualTo(0));
            Assert.That(expression.Range.Length, Is.EqualTo(text.Length));
            Assert.That(expression.Variables, Has.Length.EqualTo(1));
            AssertVariable(expression.Variables[0], "name", variableOffset, 4);
        });
    }

    [Test]
    public void VariableListsPreserveOrderCasePercentSpellingAndExactRanges()
    {
        const string text = "pre{?Var,var,Some%20Thing,a.b,%41,a%2Cb,a%2eb}post";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Parts, Has.Length.EqualTo(3));
            Assert.That(result.Template.Parts[0].Literal, Is.EqualTo("pre"));
            Assert.That(result.Template.Parts[2].Literal, Is.EqualTo("post"));

            UriTemplatePart expression = result.Template.Parts[1];
            Assert.That(expression.Operator, Is.EqualTo(UriTemplateOperator.Query));
            Assert.That(expression.Variables, Has.Length.EqualTo(7));
            AssertVariable(expression.Variables[0], "Var", 5, 3);
            AssertVariable(expression.Variables[1], "var", 9, 3);
            AssertVariable(expression.Variables[2], "Some%20Thing", 13, 12);
            AssertVariable(expression.Variables[3], "a.b", 26, 3);
            AssertVariable(expression.Variables[4], "%41", 30, 3);
            AssertVariable(expression.Variables[5], "a%2Cb", 34, 5);
            AssertVariable(expression.Variables[6], "a%2eb", 40, 5);
        });
    }

    [Test]
    public void CaseDistinctVariableNamesRemainDistinctOccurrences()
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            "{Name,NAME,name}".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.Parts[0].Variables, Has.Length.EqualTo(3));
            Assert.That(result.Template.Parts[0].Variables[0].Name, Is.EqualTo("Name"));
            Assert.That(result.Template.Parts[0].Variables[1].Name, Is.EqualTo("NAME"));
            Assert.That(result.Template.Parts[0].Variables[2].Name, Is.EqualTo("name"));
        });
    }

    [TestCase('=')]
    [TestCase(',')]
    [TestCase('!')]
    [TestCase('@')]
    [TestCase('|')]
    public void ReservedFutureOperatorsHaveAStableExactDiagnostic(char value)
    {
        string text = "{" + value + "name}";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.UnsupportedTemplateOperator,
            UriComponent.TemplateExpression,
            1,
            1,
            TemplateParserCore.UnsupportedOperatorMessage);
    }

    [TestCase("{}", 1)]
    [TestCase("{+}", 2)]
    [TestCase("{?}", 2)]
    public void ExpressionsWithoutVariablesAreRejectedAtTheEmptyPosition(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplateExpression,
            UriComponent.TemplateExpression,
            offset,
            0,
            TemplateParserCore.EmptyExpressionMessage);
    }

    [TestCase("{a,.b}", 3)]
    [TestCase("{a.}", 2)]
    [TestCase("{a..b}", 3)]
    [TestCase("{a,,b}", 3)]
    [TestCase("{a,}", 2)]
    [TestCase("{$name}", 1)]
    [TestCase("{a-name}", 2)]
    [TestCase("{?a b}", 3)]
    public void InvalidVariableNamesAndEmptyListItemsReportExactCharacters(
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

    [TestCase("{na%}", 3)]
    [TestCase("{na%0}", 3)]
    [TestCase("{na%G0}", 4)]
    [TestCase("{na%0G}", 5)]
    public void MalformedVariablePercentTripletsReportExactCharacters(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.TemplateVariable,
            offset,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void ParsingAReadOnlySpanUsesSliceRelativeRanges()
    {
        const string template = "x{&First,second}y";
        string padded = "before" + template + "after";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            padded.AsSpan(6, template.Length));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.OriginalText, Is.EqualTo(template));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(3));
            UriTemplatePart expression = result.Template.Parts[1];
            Assert.That(expression.Range.Offset, Is.EqualTo(1));
            Assert.That(expression.Range.Length, Is.EqualTo(15));
            AssertVariable(expression.Variables[0], "First", 3, 5);
            AssertVariable(expression.Variables[1], "second", 9, 6);
        });
    }

    private static void AssertVariable(
        UriTemplateVariable variable,
        string name,
        int offset,
        int length)
    {
        Assert.That(variable.Name, Is.EqualTo(name));
        Assert.That(variable.HasPrefix, Is.False);
        Assert.That(variable.PrefixLength, Is.EqualTo(0));
        Assert.That(variable.Explode, Is.False);
        Assert.That(variable.Range.Offset, Is.EqualTo(offset));
        Assert.That(variable.Range.Length, Is.EqualTo(length));
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
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
