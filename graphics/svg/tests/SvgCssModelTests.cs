namespace svg.Tests;

public class SvgCssModelTests
{
    [Test]
    public void ComponentValuesPreserveNestedFunctionOrderAndIndependentCollections()
    {
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters("calc(1px + var(--x))", 32, 128);
        SvgCssComponentValue dimension = Leaf(tokenized.Tokens[1]);
        SvgCssComponentValue whitespaceBefore = Leaf(tokenized.Tokens[2]);
        SvgCssComponentValue plus = Leaf(tokenized.Tokens[3]);
        SvgCssComponentValue whitespaceAfter = Leaf(tokenized.Tokens[4]);
        SvgCssComponentValue variableName = Leaf(tokenized.Tokens[6]);
        List<SvgCssComponentValue> variableValues = new List<SvgCssComponentValue> { variableName };
        SvgCssComponentValue variable = new SvgCssComponentValue(
            SvgCssComponentValueKind.Function,
            tokenized.Tokens[5],
            tokenized.Tokens[7],
            "var",
            "var(--x)",
            variableValues,
            null,
            true);
        List<SvgCssComponentValue> calculationValues = new List<SvgCssComponentValue>
        {
            dimension,
            whitespaceBefore,
            plus,
            whitespaceAfter,
            variable
        };

        SvgCssComponentValue calculation = new SvgCssComponentValue(
            SvgCssComponentValueKind.Function,
            tokenized.Tokens[0],
            tokenized.Tokens[8],
            "calc",
            "calc(1px + var(--x))",
            calculationValues,
            null,
            true);
        variableValues.Clear();
        calculationValues.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(calculation.Kind, Is.EqualTo(SvgCssComponentValueKind.Function));
            Assert.That(calculation.Token.RawText, Is.EqualTo("calc("));
            Assert.That(calculation.EndToken.RawText, Is.EqualTo(")"));
            Assert.That(calculation.Values.Select(value => value.RawText), Is.EqualTo(new[] { "1px", " ", "+", " ", "var(--x)" }));
            Assert.That(calculation.Values[4].Values, Has.Count.EqualTo(1));
            Assert.That(calculation.Values[4].Values[0].RawText, Is.EqualTo("--x"));
            Assert.That(calculation.RawText, Is.EqualTo("calc(1px + var(--x))"));
        });
    }

    [Test]
    public void DeclarationsRetainOriginalTokensImportanceSourceOrderAndParsedPayload()
    {
        string text = "FILL: red !important;";
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(text, 32, 128);
        SvgSourceSpan source = Span(text);
        SvgCssComponentValue red = Leaf(tokenized.Tokens[3]);
        List<SvgCssToken> tokens = new List<SvgCssToken>(tokenized.Tokens);
        List<SvgCssComponentValue> values = new List<SvgCssComponentValue> { red };

        SvgCssDeclaration declaration = new SvgCssDeclaration(
            "fill",
            "FILL",
            text,
            " red ",
            tokens,
            values,
            true,
            7,
            source,
            true);
        object parsed = new object();
        declaration.ParsedValue = parsed;
        tokens.Clear();
        values.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Name, Is.EqualTo("fill"));
            Assert.That(declaration.RawName, Is.EqualTo("FILL"));
            Assert.That(declaration.RawText, Is.EqualTo(text));
            Assert.That(string.Concat(declaration.Tokens.Select(token => token.RawText)), Is.EqualTo(text));
            Assert.That(declaration.Values.Select(value => value.RawText), Is.EqualTo(new[] { "red" }));
            Assert.That(declaration.Important, Is.True);
            Assert.That(declaration.SourceOrder, Is.EqualTo(7));
            Assert.That(declaration.Source, Is.SameAs(source));
            Assert.That(declaration.ParsedValue, Is.SameAs(parsed));
            Assert.That(declaration.IsValid, Is.True);
        });
    }

    [Test]
    public void UnknownAtRulesPreservePreludeBlockAndUninterpretedSyntax()
    {
        string text = "@-vendor mode { raw(value); }";
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(text, 32, 128);
        SvgCssComponentValue prelude = Leaf(tokenized.Tokens[2]);
        SvgCssComponentValue blockValue = Leaf(tokenized.Tokens[6]);
        SvgCssUnknownSyntax unknown = new SvgCssUnknownSyntax(
            SvgCssUnknownSyntaxKind.AtRule,
            text,
            tokenized.Tokens,
            "vendor at-rule",
            null,
            true);
        SvgCssAtRule atRule = new SvgCssAtRule(
            SvgCssAtRuleKind.Unknown,
            "-vendor",
            text,
            new List<SvgCssComponentValue> { prelude },
            new List<SvgCssComponentValue> { blockValue },
            true,
            3,
            null,
            true);
        atRule.UnknownSyntax = unknown;
        SvgCssRule rule = new SvgCssRule(
            SvgCssRuleKind.AtRule,
            text,
            new List<SvgCssComponentValue>(),
            new List<SvgCssDeclaration>(),
            new List<SvgCssRule>(),
            atRule,
            3,
            null,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(rule.AtRule, Is.SameAs(atRule));
            Assert.That(atRule.Name, Is.EqualTo("-vendor"));
            Assert.That(atRule.HasBlock, Is.True);
            Assert.That(atRule.Prelude.Select(value => value.RawText), Is.EqualTo(new[] { "mode" }));
            Assert.That(atRule.Block.Select(value => value.RawText), Is.EqualTo(new[] { "raw(" }));
            Assert.That(atRule.SourceOrder, Is.EqualTo(3));
            Assert.That(atRule.UnknownSyntax.IsValid, Is.True);
            Assert.That(atRule.UnknownSyntax.Reason, Is.EqualTo("vendor at-rule"));
            Assert.That(string.Concat(atRule.UnknownSyntax.Tokens.Select(token => token.RawText)), Is.EqualTo(text));
        });
    }

    [Test]
    public void RulesCopyOrderedDeclarationsAndNestedRulesWithoutSubclassing()
    {
        SvgCssDeclaration first = Declaration("fill", "red", false, 0);
        SvgCssDeclaration second = Declaration("stroke", "blue", true, 1);
        SvgCssRule child = StyleRule("circle { opacity: .5; }", 2, new List<SvgCssDeclaration> { Declaration("opacity", ".5", false, 0) });
        List<SvgCssDeclaration> declarations = new List<SvgCssDeclaration> { first, second };
        List<SvgCssRule> children = new List<SvgCssRule> { child };

        SvgCssRule parent = StyleRule("g { fill:red; stroke:blue!important; circle { opacity:.5; } }", 1, declarations, children);
        declarations.Reverse();
        children.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(parent.Kind, Is.EqualTo(SvgCssRuleKind.Style));
            Assert.That(parent.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "fill", "stroke" }));
            Assert.That(parent.Declarations.Select(declaration => declaration.SourceOrder), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(parent.Declarations[1].Important, Is.True);
            Assert.That(parent.ChildRules, Has.Count.EqualTo(1));
            Assert.That(parent.ChildRules[0], Is.SameAs(child));
            Assert.That(parent.SourceOrder, Is.EqualTo(1));
            Assert.That(parent.AtRule, Is.Null);
        });
    }

    [Test]
    public void StyleSheetsRetainOriginLayerSourceValidityDiagnosticsAndVersion()
    {
        SvgCssRule first = StyleRule("a{}", 0, new List<SvgCssDeclaration>());
        SvgCssRule second = StyleRule("b{}", 1, new List<SvgCssDeclaration>());
        List<SvgCssRule> rules = new List<SvgCssRule> { first, second };
        SvgSourceSpan source = Span("a{}b{}");
        List<SvgDiagnostic> diagnostics = new List<SvgDiagnostic>();

        SvgStyleSheet sheet = new SvgStyleSheet(rules, SvgStyleOrigin.Author, "theme.controls", 2, "a{}b{}", source, diagnostics);
        rules.Reverse();
        long firstVersion = sheet.MarkChanged();
        long secondVersion = sheet.MarkChanged();

        Assert.Multiple(() =>
        {
            Assert.That(sheet.Rules.Select(rule => rule.RawText), Is.EqualTo(new[] { "a{}", "b{}" }));
            Assert.That(sheet.Rules.Select(rule => rule.SourceOrder), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(sheet.Origin, Is.EqualTo(SvgStyleOrigin.Author));
            Assert.That(sheet.LayerName, Is.EqualTo("theme.controls"));
            Assert.That(sheet.LayerOrder, Is.EqualTo(2));
            Assert.That(sheet.SourceText, Is.EqualTo("a{}b{}"));
            Assert.That(sheet.Source, Is.SameAs(source));
            Assert.That(sheet.IsValid, Is.True);
            Assert.That(firstVersion, Is.EqualTo(1));
            Assert.That(secondVersion, Is.EqualTo(2));
            Assert.That(sheet.Version, Is.EqualTo(2));
        });

        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "invalid", source);
        SvgStyleSheet invalidByDiagnostic = new SvgStyleSheet(new List<SvgCssRule> { first }, SvgStyleOrigin.User, "", -1, "a{}", source, new List<SvgDiagnostic> { diagnostic });
        SvgDiagnostic warning = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.PreservedFeature, "preserved", source);
        SvgStyleSheet validWithWarning = new SvgStyleSheet(new List<SvgCssRule> { first }, SvgStyleOrigin.Author, "", -1, "a{}", source, new List<SvgDiagnostic> { warning });
        SvgCssRule invalidRule = StyleRule("broken{}", 0, new List<SvgCssDeclaration>());
        invalidRule.IsValid = false;
        SvgStyleSheet invalidByRule = new SvgStyleSheet(new List<SvgCssRule> { invalidRule }, SvgStyleOrigin.UserAgent, "", -1, "broken{}", source, new List<SvgDiagnostic>());

        Assert.Multiple(() =>
        {
            Assert.That(invalidByDiagnostic.IsValid, Is.False);
            Assert.That(invalidByDiagnostic.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(validWithWarning.IsValid, Is.True);
            Assert.That(invalidByRule.IsValid, Is.False);
        });
    }

    [Test]
    public void ModelConstructorsRejectInvalidKindsOwnershipAndSourceOrders()
    {
        SvgCssToken token = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters("x", 4, 4).Tokens[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCssComponentValue((SvgCssComponentValueKind)99, token, null, "", "x", new List<SvgCssComponentValue>(), null, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => Declaration("x", "y", false, -1));
            Assert.Throws<ArgumentNullException>(() => new SvgCssRule(SvgCssRuleKind.AtRule, "@x;", new List<SvgCssComponentValue>(), new List<SvgCssDeclaration>(), new List<SvgCssRule>(), null, 0, null, true));
            Assert.Throws<ArgumentException>(() => new SvgCssRule(SvgCssRuleKind.Style, "x{}", new List<SvgCssComponentValue>(), new List<SvgCssDeclaration>(), new List<SvgCssRule>(), AtRule(), 0, null, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgStyleSheet(new List<SvgCssRule>(), SvgStyleOrigin.Author, "", -2, "", null, new List<SvgDiagnostic>()));
        });
    }

    static SvgCssComponentValue Leaf(SvgCssToken token)
    {
        return new SvgCssComponentValue(SvgCssComponentValueKind.Token, token, null, "", token.RawText, new List<SvgCssComponentValue>(), null, true);
    }

    static SvgCssDeclaration Declaration(string name, string value, bool important, int sourceOrder)
    {
        string text = name + ":" + value + (important ? "!important" : "") + ";";
        SvgCssTokenizeResult tokens = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(text, 32, 128);
        return new SvgCssDeclaration(name, name, text, value, tokens.Tokens, new List<SvgCssComponentValue>(), important, sourceOrder, null, true);
    }

    static SvgCssRule StyleRule(string rawText, int sourceOrder, List<SvgCssDeclaration> declarations)
    {
        return StyleRule(rawText, sourceOrder, declarations, new List<SvgCssRule>());
    }

    static SvgCssRule StyleRule(string rawText, int sourceOrder, List<SvgCssDeclaration> declarations, List<SvgCssRule> childRules)
    {
        return new SvgCssRule(SvgCssRuleKind.Style, rawText, new List<SvgCssComponentValue>(), declarations, childRules, null, sourceOrder, null, true);
    }

    static SvgCssAtRule AtRule()
    {
        return new SvgCssAtRule(SvgCssAtRuleKind.Unknown, "x", "@x;", new List<SvgCssComponentValue>(), new List<SvgCssComponentValue>(), false, 0, null, true);
    }

    static SvgSourceSpan Span(string text)
    {
        return new SvgSourceSpan(new SvgSourceUri("memory:test.css"), 0, 1, 1, text.Length, 1, text.Length + 1);
    }
}
