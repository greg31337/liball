namespace uri.Tests;

public class UriTemplateDataTests
{
    [Test]
    public void TemplateSyntaxModelsHaveSafeIndependentDefaults()
    {
        UriTemplate firstTemplate = new UriTemplate();
        UriTemplate secondTemplate = new UriTemplate();
        UriTemplatePart firstPart = new UriTemplatePart();
        UriTemplatePart secondPart = new UriTemplatePart();
        UriTemplateVariable firstVariable = new UriTemplateVariable();
        UriTemplateVariable secondVariable = new UriTemplateVariable();

        Assert.Multiple(() =>
        {
            Assert.That(firstTemplate.Parts, Is.Empty);
            Assert.That(firstTemplate.Parts, Is.Not.SameAs(secondTemplate.Parts));
            Assert.That(firstTemplate.Level, Is.EqualTo(UriTemplateLevel.Level1));
            Assert.That(firstTemplate.OriginalText, Is.Empty);

            Assert.That(firstPart.Kind, Is.EqualTo(UriTemplatePartKind.Literal));
            Assert.That(firstPart.Literal, Is.Empty);
            Assert.That(firstPart.Operator, Is.EqualTo(UriTemplateOperator.Simple));
            Assert.That(firstPart.Variables, Is.Empty);
            Assert.That(firstPart.Variables, Is.Not.SameAs(secondPart.Variables));
            Assert.That(firstPart.Range.Offset, Is.EqualTo(0));
            Assert.That(firstPart.Range.Length, Is.EqualTo(0));
            Assert.That(firstPart.Range, Is.Not.SameAs(secondPart.Range));

            Assert.That(firstVariable.Name, Is.Empty);
            Assert.That(firstVariable.HasPrefix, Is.False);
            Assert.That(firstVariable.PrefixLength, Is.EqualTo(0));
            Assert.That(firstVariable.Explode, Is.False);
            Assert.That(firstVariable.Range.Offset, Is.EqualTo(0));
            Assert.That(firstVariable.Range.Length, Is.EqualTo(0));
            Assert.That(firstVariable.Range, Is.Not.SameAs(secondVariable.Range));
        });
    }

    [Test]
    public void TemplatePartsPreserveLiteralExpressionAndVariableState()
    {
        UriTemplateVariable prefixed = new UriTemplateVariable();
        prefixed.Name = "Some%20Thing";
        prefixed.HasPrefix = true;
        prefixed.PrefixLength = 12;
        prefixed.Range = new TextRange(8, 15);
        UriTemplateVariable exploded = new UriTemplateVariable();
        exploded.Name = "items";
        exploded.Explode = true;
        exploded.Range = new TextRange(24, 6);

        UriTemplatePart literal = new UriTemplatePart();
        literal.Literal = "café/";
        literal.Range = new TextRange(0, 5);
        UriTemplatePart expression = new UriTemplatePart();
        expression.Kind = UriTemplatePartKind.Expression;
        expression.Operator = UriTemplateOperator.Query;
        expression.Variables = new UriTemplateVariable[] { prefixed, exploded };
        expression.Range = new TextRange(5, 27);

        UriTemplate template = new UriTemplate();
        template.Parts = new UriTemplatePart[] { literal, expression };
        template.Level = UriTemplateLevel.Level4;
        template.OriginalText = "café/{?Some%20Thing:12,items*}";

        Assert.Multiple(() =>
        {
            Assert.That(template.Parts, Is.EqualTo(new UriTemplatePart[]
            {
                literal,
                expression
            }));
            Assert.That(template.Level, Is.EqualTo(UriTemplateLevel.Level4));
            Assert.That(template.OriginalText, Is.EqualTo(
                "café/{?Some%20Thing:12,items*}"));
            Assert.That(literal.Kind, Is.EqualTo(UriTemplatePartKind.Literal));
            Assert.That(literal.Literal, Is.EqualTo("café/"));
            Assert.That(expression.Kind, Is.EqualTo(UriTemplatePartKind.Expression));
            Assert.That(expression.Operator, Is.EqualTo(UriTemplateOperator.Query));
            Assert.That(expression.Variables, Is.EqualTo(new UriTemplateVariable[]
            {
                prefixed,
                exploded
            }));
            Assert.That(prefixed.Name, Is.EqualTo("Some%20Thing"));
            Assert.That(prefixed.HasPrefix, Is.True);
            Assert.That(prefixed.PrefixLength, Is.EqualTo(12));
            Assert.That(prefixed.Explode, Is.False);
            Assert.That(prefixed.Range.Offset, Is.EqualTo(8));
            Assert.That(prefixed.Range.Length, Is.EqualTo(15));
            Assert.That(exploded.Name, Is.EqualTo("items"));
            Assert.That(exploded.HasPrefix, Is.False);
            Assert.That(exploded.Explode, Is.True);
            Assert.That(exploded.Range.Offset, Is.EqualTo(24));
            Assert.That(exploded.Range.Length, Is.EqualTo(6));
        });
    }

    [Test]
    public void ValuesDistinguishUndefinedEmptyScalarListAndOrderedMapState()
    {
        UriTemplateValue undefined = new UriTemplateValue();
        UriTemplateValue emptyScalar = new UriTemplateValue();
        emptyScalar.Kind = UriTemplateValueKind.Scalar;
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = new string[] { "red", "", "blue" };
        UriTemplateMapEntry first = new UriTemplateMapEntry("semi", true, ";");
        UriTemplateMapEntry second = new UriTemplateMapEntry("missing", false, "");
        UriTemplateMapEntry third = new UriTemplateMapEntry("empty", true, "");
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Map = new UriTemplateMapEntry[] { first, second, third };

        UriTemplateBinding upper = new UriTemplateBinding("Var", emptyScalar);
        UriTemplateBinding lower = new UriTemplateBinding("var", map);

        Assert.Multiple(() =>
        {
            Assert.That(undefined.Kind, Is.EqualTo(UriTemplateValueKind.Undefined));
            Assert.That(undefined.Scalar, Is.Empty);
            Assert.That(undefined.List, Is.Empty);
            Assert.That(undefined.Map, Is.Empty);
            Assert.That(emptyScalar.Kind, Is.EqualTo(UriTemplateValueKind.Scalar));
            Assert.That(emptyScalar.Scalar, Is.Empty);
            Assert.That(list.List, Is.EqualTo(new string[] { "red", "", "blue" }));
            Assert.That(map.Map, Is.EqualTo(new UriTemplateMapEntry[]
            {
                first,
                second,
                third
            }));
            Assert.That(first.Key, Is.EqualTo("semi"));
            Assert.That(first.Defined, Is.True);
            Assert.That(first.Value, Is.EqualTo(";"));
            Assert.That(second.Key, Is.EqualTo("missing"));
            Assert.That(second.Defined, Is.False);
            Assert.That(third.Defined, Is.True);
            Assert.That(third.Value, Is.Empty);
            Assert.That(upper.Name, Is.EqualTo("Var"));
            Assert.That(upper.Value, Is.SameAs(emptyScalar));
            Assert.That(lower.Name, Is.EqualTo("var"));
            Assert.That(lower.Value, Is.SameAs(map));
            Assert.That(upper.Name, Is.Not.EqualTo(lower.Name));
        });
    }

    [Test]
    public void TemplateEnumsExposeEveryRfcModelChoice()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<UriTemplatePartKind>(), Is.EqualTo(
                new UriTemplatePartKind[]
                {
                    UriTemplatePartKind.Literal,
                    UriTemplatePartKind.Expression
                }));
            Assert.That(Enum.GetValues<UriTemplateOperator>(), Is.EqualTo(
                new UriTemplateOperator[]
                {
                    UriTemplateOperator.Simple,
                    UriTemplateOperator.Reserved,
                    UriTemplateOperator.Fragment,
                    UriTemplateOperator.Label,
                    UriTemplateOperator.PathSegment,
                    UriTemplateOperator.PathParameter,
                    UriTemplateOperator.Query,
                    UriTemplateOperator.QueryContinuation
                }));
            Assert.That(Enum.GetValues<UriTemplateValueKind>(), Is.EqualTo(
                new UriTemplateValueKind[]
                {
                    UriTemplateValueKind.Undefined,
                    UriTemplateValueKind.Scalar,
                    UriTemplateValueKind.List,
                    UriTemplateValueKind.Map
                }));
            Assert.That(Enum.GetValues<UriTemplateLevel>(), Is.EqualTo(
                new UriTemplateLevel[]
                {
                    UriTemplateLevel.Level1,
                    UriTemplateLevel.Level2,
                    UriTemplateLevel.Level3,
                    UriTemplateLevel.Level4
                }));
            Assert.That((int)UriTemplateLevel.Level1, Is.EqualTo(1));
            Assert.That((int)UriTemplateLevel.Level4, Is.EqualTo(4));
        });
    }

    [Test]
    public void ParseAndExpansionResultsRetainSuccessFailureAndPartialOutput()
    {
        UriTemplate template = new UriTemplate();
        template.OriginalText = "{/path}";
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.InvalidTemplateExpression,
            DiagnosticSeverity.Error,
            UriComponent.TemplateExpression,
            new TextRange(4, 2),
            "invalid expression");

        UriTemplateParseResult parsed = new UriTemplateParseResult(
            true,
            template,
            null);
        UriTemplateParseResult failedParse = new UriTemplateParseResult(
            false,
            new UriTemplate(),
            diagnostic);
        UriTemplateExpansionResult expanded = new UriTemplateExpansionResult(
            true,
            "/one/two",
            8,
            null);
        UriTemplateExpansionResult partial = new UriTemplateExpansionResult(
            false,
            "/one{bad}",
            9,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Template, Is.SameAs(template));
            Assert.That(parsed.Diagnostic, Is.Null);
            Assert.That(failedParse.Success, Is.False);
            Assert.That(failedParse.Template.OriginalText, Is.Empty);
            Assert.That(failedParse.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(expanded.Success, Is.True);
            Assert.That(expanded.Text, Is.EqualTo("/one/two"));
            Assert.That(expanded.Length, Is.EqualTo(8));
            Assert.That(expanded.Diagnostic, Is.Null);
            Assert.That(partial.Success, Is.False);
            Assert.That(partial.Text, Is.EqualTo("/one{bad}"));
            Assert.That(partial.Length, Is.EqualTo(9));
            Assert.That(partial.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void TemplateExceptionsRetainTheirDiagnosticAndMessage()
    {
        UriDiagnostic syntaxDiagnostic = new UriDiagnostic(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.InvalidTemplate,
            DiagnosticSeverity.Error,
            UriComponent.Template,
            new TextRange(2, 1),
            "invalid template");
        UriDiagnostic expansionDiagnostic = new UriDiagnostic(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.LimitExceeded,
            DiagnosticSeverity.Error,
            UriComponent.Output,
            new TextRange(8, 3),
            "expansion failed");

        UriTemplateSyntaxException syntax = new UriTemplateSyntaxException(
            syntaxDiagnostic);
        UriTemplateExpansionException expansion =
            new UriTemplateExpansionException(expansionDiagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Diagnostic, Is.SameAs(syntaxDiagnostic));
            Assert.That(syntax.Message, Is.EqualTo(syntaxDiagnostic.Message));
            Assert.That(expansion.Diagnostic, Is.SameAs(expansionDiagnostic));
            Assert.That(expansion.Message, Is.EqualTo(expansionDiagnostic.Message));
        });
    }
}
