namespace uri.Tests;

public class TemplateCompositionInspectionTests
{
    [Test]
    public void CompositionRoundTripsModelTextWithoutSemanticRewriting()
    {
        const string source =
            "café/%2f{var}{+reserved}{#fragment}{.label}{/path}" +
            "{;parameter}{?query,list*}{&name:12}";
        UriTemplate template = Parse(source);

        UriTextResult result = TemplateComposerCore.ComposeTemplate(
            template,
            new OperationLimits());
        UriTemplateParseResult reparsed = TemplateParserCore.ParseTemplate(
            result.Text.AsSpan());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, source);
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level4));
        });
    }

    [Test]
    public void CompositionUsesModelFieldsInsteadOfOriginalTextOrRanges()
    {
        UriTemplate template = Parse("old{var}");
        template.OriginalText = "ignored";
        template.Level = UriTemplateLevel.Level4;
        template.Parts[0].Literal = "new/";
        template.Parts[0].Range = new TextRange(90, 80);
        template.Parts[1].Operator = UriTemplateOperator.PathSegment;
        template.Parts[1].Variables[0].Name = "path";
        template.Parts[1].Variables[0].Range = new TextRange(70, 60);

        UriTextResult result = TemplateComposerCore.ComposeTemplate(
            template,
            new OperationLimits());

        AssertSuccess(result, "new/{/path}");
    }

    [Test]
    public void PercentCaseCanonicalizationIsExplicitAndDoesNotMutateModel()
    {
        const string source = "%2f/%aa{?Some%2athing,%41:2,list*}";
        UriTemplate template = Parse(source);

        UriTextResult preserved = TemplateComposerCore.ComposeTemplate(
            template,
            new OperationLimits());
        UriTextResult canonical =
            TemplateComposerCore.CanonicalizePercentCase(
                template,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertSuccess(preserved, source);
            AssertSuccess(
                canonical,
                "%2F/%AA{?Some%2Athing,%41:2,list*}");
            Assert.That(template.OriginalText, Is.EqualTo(source));
            Assert.That(template.Parts[0].Literal, Is.EqualTo("%2f/%aa"));
            Assert.That(
                template.Parts[1].Variables[0].Name,
                Is.EqualTo("Some%2athing"));
        });
    }

    [Test]
    public void CompositionEnforcesExpressionVariableAndOutputLimits()
    {
        UriTemplate template = Parse("a{one,two}{?three}");
        UriTextResult exact = TemplateComposerCore.ComposeTemplate(
            template,
            Limits(18, 2, 3));
        UriTextResult expression = TemplateComposerCore.ComposeTemplate(
            template,
            Limits(100, 1, 3));
        UriTextResult variable = TemplateComposerCore.ComposeTemplate(
            template,
            Limits(100, 2, 2));
        UriTextResult output = TemplateComposerCore.ComposeTemplate(
            template,
            Limits(17, 2, 3));

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, "a{one,two}{?three}");
            AssertFailure(
                expression,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateExpression);
            AssertFailure(
                variable,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable);
            AssertFailure(
                output,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
        });
    }

    [Test]
    public void CompositionRejectsModelsThatWouldChangeGrammarMeaning()
    {
        UriTemplate literalTemplate = new UriTemplate();
        UriTemplatePart literal = new UriTemplatePart();
        literal.Literal = "bad{x}";
        literalTemplate.Parts = new UriTemplatePart[] { literal };

        UriTemplate variableTemplate = Parse("{name}");
        variableTemplate.Parts[0].Variables[0].Name = "one,two";

        UriTemplate modifierTemplate = Parse("{name}");
        modifierTemplate.Parts[0].Variables[0].HasPrefix = true;
        modifierTemplate.Parts[0].Variables[0].PrefixLength = 0;

        UriTextResult literalResult = TemplateComposerCore.ComposeTemplate(
            literalTemplate,
            new OperationLimits());
        UriTextResult variableResult = TemplateComposerCore.ComposeTemplate(
            variableTemplate,
            new OperationLimits());
        UriTextResult modifierResult = TemplateComposerCore.ComposeTemplate(
            modifierTemplate,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(
                literalResult,
                UriDiagnosticCode.InvalidTemplate,
                UriComponent.TemplateLiteral);
            AssertFailure(
                variableResult,
                UriDiagnosticCode.InvalidTemplateVariable,
                UriComponent.TemplateVariable);
            AssertFailure(
                modifierResult,
                UriDiagnosticCode.InvalidTemplateModifier,
                UriComponent.TemplateVariable);
        });
    }

    [Test]
    public void OrderedVariableInspectionIsUniqueCaseSensitiveAndEncoded()
    {
        UriTemplate template = Parse("{?b,a,b,A,%41,a}{x:2,x*,b}");

        string[] names = TemplateInspectorCore.ListVariables(template);
        UriTemplateVariable[] occurrences =
            TemplateInspectorCore.ListOccurrences(template);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new string[]
            {
                "b",
                "a",
                "A",
                "%41",
                "x"
            }));
            Assert.That(occurrences, Has.Length.EqualTo(9));
            Assert.That(occurrences[0], Is.SameAs(
                template.Parts[0].Variables[0]));
            Assert.That(occurrences[6], Is.SameAs(
                template.Parts[1].Variables[0]));
            Assert.That(occurrences[6].HasPrefix, Is.True);
            Assert.That(occurrences[6].PrefixLength, Is.EqualTo(2));
            Assert.That(occurrences[7].Explode, Is.True);
            Assert.That(occurrences[8].Name, Is.EqualTo("b"));
        });
    }

    [TestCase("", UriTemplateLevel.Level1)]
    [TestCase("{var}", UriTemplateLevel.Level1)]
    [TestCase("{+var}", UriTemplateLevel.Level2)]
    [TestCase("{#var}", UriTemplateLevel.Level2)]
    [TestCase("{one,two}", UriTemplateLevel.Level3)]
    [TestCase("{/var}", UriTemplateLevel.Level3)]
    [TestCase("{var:1}", UriTemplateLevel.Level4)]
    [TestCase("{var*}", UriTemplateLevel.Level4)]
    public void LevelInspectionCalculatesRequiredFeaturesFromTheModel(
        string source,
        UriTemplateLevel expected)
    {
        UriTemplate template = Parse(source);
        template.Level = UriTemplateLevel.Level4;

        UriTemplateLevel actual = TemplateInspectorCore.DetermineLevel(template);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void EmptyAndInvalidInspectionBoundariesAreDeterministic()
    {
        UriTemplate empty = new UriTemplate();
        UriTemplate invalid = new UriTemplate();
        invalid.Parts = null!;

        Assert.Multiple(() =>
        {
            Assert.That(TemplateInspectorCore.ListVariables(empty), Is.Empty);
            Assert.That(TemplateInspectorCore.ListOccurrences(empty), Is.Empty);
            Assert.That(
                TemplateInspectorCore.DetermineLevel(empty),
                Is.EqualTo(UriTemplateLevel.Level1));
            Assert.Throws<ArgumentException>(() =>
                TemplateInspectorCore.ListVariables(null));
            Assert.Throws<ArgumentException>(() =>
                TemplateInspectorCore.ListOccurrences(invalid));
            Assert.Throws<ArgumentException>(() =>
                TemplateInspectorCore.DetermineLevel(invalid));
        });
    }

    private static UriTemplate Parse(string text)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Template;
    }

    private static OperationLimits Limits(
        int outputCharacters,
        int expressions,
        int variables)
    {
        OperationLimits limits = new OperationLimits();
        limits.MaxOutputCharacters = outputCharacters;
        limits.MaxTemplateExpressions = expressions;
        limits.MaxTemplateVariables = variables;
        return limits;
    }

    private static void AssertSuccess(UriTextResult result, string expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
            DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
