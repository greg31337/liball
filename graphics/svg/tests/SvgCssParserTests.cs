namespace svg.Tests;

public class SvgCssParserTests
{
    [Test]
    public void StylesheetsParseQualifiedRecognizedAndUnknownRulesInSourceOrder()
    {
        string input = "<!-- /*top*/ rect, .x { fill: red; stroke: url(data:a;b); } " +
            "@media screen { circle { opacity:.5; } } @vendor x; -->";
        SvgSourceUri source = new SvgSourceUri("memory:sheet.css");

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(input, Limits(), SvgStyleOrigin.User, source);
        SvgStyleSheet sheet = result.StyleSheet;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Mode, Is.EqualTo(SvgCssParseMode.StyleSheet));
            Assert.That(result.Declarations, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet.Origin, Is.EqualTo(SvgStyleOrigin.User));
            Assert.That(sheet.SourceText, Is.EqualTo(input));
            Assert.That(sheet.Source.Source, Is.SameAs(source));
            Assert.That(sheet.Rules.Select(rule => rule.Kind), Is.EqualTo(new[]
            {
                SvgCssRuleKind.Style,
                SvgCssRuleKind.AtRule,
                SvgCssRuleKind.AtRule
            }));
            Assert.That(sheet.Rules.Select(rule => rule.SourceOrder), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(sheet.Rules[0].Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "fill", "stroke" }));
            Assert.That(sheet.Rules[0].Declarations[1].ValueText, Is.EqualTo(" url(data:a;b)"));
            Assert.That(sheet.Rules[1].AtRule.Kind, Is.EqualTo(SvgCssAtRuleKind.Media));
            Assert.That(sheet.Rules[1].AtRule.HasBlock, Is.True);
            Assert.That(sheet.Rules[2].AtRule.Kind, Is.EqualTo(SvgCssAtRuleKind.Unknown));
            Assert.That(sheet.Rules[2].AtRule.UnknownSyntax.RawText, Is.EqualTo("@vendor x;"));
            Assert.That(sheet.IsValid, Is.True);
        });
    }

    [Test]
    public void QualifiedRulesWithoutBlocksAreDiscardedAsRetainedUnknownSyntax()
    {
        string input = "@vendor ok; broken";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(input, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.Rules, Has.Count.EqualTo(1));
            Assert.That(result.StyleSheet.Rules[0].AtRule.Name, Is.EqualTo("vendor"));
            Assert.That(result.UnknownSyntax.Select(unknown => unknown.RawText), Is.EqualTo(new[] { "@vendor ok;", "broken" }));
            Assert.That(result.UnknownSyntax.Select(unknown => unknown.IsValid), Is.EqualTo(new[] { true, false }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("curly block"));
            Assert.That(result.StyleSheet.IsValid, Is.False);
        });
    }

    [Test]
    public void DeclarationSemicolonsOnlySplitAtTopLevel()
    {
        string input = "a:fn(one;two); b:{x;y}; c:[u;v]; d:(q;r); e:url(data:a;b);";

        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits(input, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
            Assert.That(result.Declarations.Select(declaration => declaration.SourceOrder), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(result.Declarations[0].ValueText, Is.EqualTo("fn(one;two)"));
            Assert.That(result.Declarations[1].ValueText, Is.EqualTo("{x;y}"));
            Assert.That(result.Declarations[2].ValueText, Is.EqualTo("[u;v]"));
            Assert.That(result.Declarations[3].ValueText, Is.EqualTo("(q;r)"));
            Assert.That(result.Declarations[4].ValueText, Is.EqualTo("url(data:a;b)"));
            Assert.That(result.Declarations.Take(4).Select(declaration => declaration.Values.Single().Kind), Is.EqualTo(new[]
            {
                SvgCssComponentValueKind.Function,
                SvgCssComponentValueKind.SimpleBlock,
                SvgCssComponentValueKind.SimpleBlock,
                SvgCssComponentValueKind.SimpleBlock
            }));
        });
    }

    [Test]
    public void ImportantSuffixesAreRemovedFromValuesWhileRawDeclarationsRemainExact()
    {
        string input = "COLOR: red ! /*x*/ ImPoRtAnT ; --Theme: A !important; width: calc(1px + 2px)!important";

        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits(input, Limits());
        SvgCssDeclaration color = result.Declarations[0];
        SvgCssDeclaration custom = result.Declarations[1];
        SvgCssDeclaration width = result.Declarations[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(color.Name, Is.EqualTo("color"));
            Assert.That(color.RawName, Is.EqualTo("COLOR"));
            Assert.That(color.Important, Is.True);
            Assert.That(color.ValueText, Is.EqualTo(" red "));
            Assert.That(string.Concat(color.Values.Select(value => value.RawText)), Is.EqualTo(" red "));
            Assert.That(color.RawText, Is.EqualTo("COLOR: red ! /*x*/ ImPoRtAnT ;"));
            Assert.That(custom.Name, Is.EqualTo("--Theme"));
            Assert.That(custom.Important, Is.True);
            Assert.That(custom.ValueText, Is.EqualTo(" A "));
            Assert.That(width.Important, Is.True);
            Assert.That(width.ValueText, Is.EqualTo(" calc(1px + 2px)"));
            Assert.That(width.Values.Single(value => value.Kind == SvgCssComponentValueKind.Function).Name, Is.EqualTo("calc"));
        });
    }

    [Test]
    public void DeclarationRecoveryRetainsMalformedFragmentsAndContinues()
    {
        string input = "good:1; broken; :bad; bad:\"x\n; next:2; @media x; final:3";

        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits(input, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgCssParseError.None));
            Assert.That(result.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "good", "next", "final" }));
            Assert.That(result.Declarations.Select(declaration => declaration.SourceOrder), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.UnknownSyntax.Select(unknown => unknown.RawText), Is.EqualTo(new[]
            {
                "broken;",
                ":bad;",
                "bad:\"x\n;",
                "@media x;"
            }));
            Assert.That(result.UnknownSyntax.All(unknown => !unknown.IsValid), Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
        });
    }

    [Test]
    public void StyleAttributeModeRejectsAtRulesButContinuesWithDeclarations()
    {
        string input = "color:red; @media x {a:b}; width:1px;";

        SvgCssParseResult result = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(input, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Mode, Is.EqualTo(SvgCssParseMode.StyleAttribute));
            Assert.That(result.StyleSheet, Is.Null);
            Assert.That(result.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "color", "width" }));
            Assert.That(result.UnknownSyntax, Has.Count.EqualTo(1));
            Assert.That(result.UnknownSyntax[0].RawText, Is.EqualTo("@media x {a:b};"));
            Assert.That(result.Diagnostics.Single().Message, Does.Contain("style attribute"));
        });
    }

    [Test]
    public void UnclosedRuleBlocksRemainRepresentableAndInvalidateTheSheet()
    {
        string input = "a { color:red; nested:fn(1;2)";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(input, Limits());
        SvgCssRule rule = result.StyleSheet.Rules[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(rule.RawText, Is.EqualTo(input));
            Assert.That(rule.IsValid, Is.False);
            Assert.That(rule.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "color", "nested" }));
            Assert.That(rule.Declarations[1].ValueText, Is.EqualTo("fn(1;2)"));
            Assert.That(result.StyleSheet.IsValid, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
        });
    }

    [Test]
    public void SourceSpansUseTokenizerOffsetsLinesAndCallerSourceIdentity()
    {
        string input = "\r\na{b:c}";
        SvgSourceUri source = new SvgSourceUri("file:test.css");

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(input, Limits(), SvgStyleOrigin.Author, source);
        SvgCssRule rule = result.StyleSheet.Rules[0];
        SvgCssDeclaration declaration = rule.Declarations[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.StyleSheet.Source.StartOffset, Is.Zero);
            Assert.That(result.StyleSheet.Source.EndOffset, Is.EqualTo(input.Length));
            Assert.That(rule.Source.Source, Is.SameAs(source));
            Assert.That(rule.Source.StartOffset, Is.EqualTo(2));
            Assert.That(rule.Source.StartLine, Is.EqualTo(2));
            Assert.That(rule.Source.StartColumn, Is.EqualTo(1));
            Assert.That(declaration.Source.StartOffset, Is.EqualTo(4));
            Assert.That(declaration.Source.StartLine, Is.EqualTo(2));
            Assert.That(declaration.Source.StartColumn, Is.EqualTo(3));
            Assert.That(input.Substring((int)declaration.Source.StartOffset, (int)declaration.Source.Length()), Is.EqualTo("b:c"));
        });
    }

    [Test]
    public void CharacterTokenRuleDeclarationAndDepthLimitsFailDeterministically()
    {
        SvgCssLimits characterLimits = Limits();
        characterLimits.MaxCharacters = 3;
        SvgCssParseResult characters = new SvgCssParser().ParseStyleSheetWithValueAndLimits("a{ }", characterLimits);

        SvgCssLimits tokenLimits = Limits();
        tokenLimits.MaxTokens = 2;
        SvgCssParseResult tokenCount = new SvgCssParser().ParseStyleSheetWithValueAndLimits("a{}", tokenLimits);

        SvgCssLimits ruleLimits = Limits();
        ruleLimits.MaxRules = 1;
        SvgCssParseResult rules = new SvgCssParser().ParseStyleSheetWithValueAndLimits("a{}b{}", ruleLimits);

        SvgCssLimits declarationLimits = Limits();
        declarationLimits.MaxDeclarations = 1;
        SvgCssParseResult declarations = new SvgCssParser().ParseStyleAttributeWithValueAndLimits("a:1;b:2", declarationLimits);

        SvgCssLimits depthLimits = Limits();
        depthLimits.MaxBlockDepth = 1;
        SvgCssParseResult depth = new SvgCssParser().ParseStyleSheetWithValueAndLimits("a{b:c(d);}", depthLimits);

        Assert.Multiple(() =>
        {
            Assert.That(characters.Error, Is.EqualTo(SvgCssParseError.CharacterLimitExceeded));
            Assert.That(characters.ErrorOffset, Is.EqualTo(3));
            Assert.That(tokenCount.Error, Is.EqualTo(SvgCssParseError.TokenLimitExceeded));
            Assert.That(tokenCount.ErrorOffset, Is.EqualTo(2));
            Assert.That(rules.Error, Is.EqualTo(SvgCssParseError.RuleLimitExceeded));
            Assert.That(rules.ErrorOffset, Is.EqualTo(3));
            Assert.That(declarations.Error, Is.EqualTo(SvgCssParseError.DeclarationLimitExceeded));
            Assert.That(declarations.ErrorOffset, Is.EqualTo(4));
            Assert.That(declarations.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "a" }));
            Assert.That(depth.Error, Is.EqualTo(SvgCssParseError.BlockDepthExceeded));
            Assert.That(depth.ErrorOffset, Is.EqualTo(4));
            Assert.That(new[] { characters, tokenCount, rules, declarations, depth }.All(result => !result.Success), Is.True);
            Assert.That(new[] { characters, tokenCount, rules, declarations, depth }.All(result => result.Diagnostics.Last().Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    [Test]
    public void InvalidParserArgumentsReturnStableFailures()
    {
        SvgCssLimits invalidLimits = Limits();
        invalidLimits.MaxDeclarations = 0;

        SvgCssParseResult nullText = new SvgCssParser().ParseDeclarationListWithValueAndLimits(null, Limits());
        SvgCssParseResult nullLimits = new SvgCssParser().ParseStyleSheetWithValueAndLimits("", null);
        SvgCssParseResult invalid = new SvgCssParser().ParseStyleAttributeWithValueAndLimits("a:b", invalidLimits);
        SvgCssParseResult nullSource = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource("", Limits(), SvgStyleOrigin.Author, null);

        Assert.Multiple(() =>
        {
            Assert.That(nullText.Error, Is.EqualTo(SvgCssParseError.InvalidArgument));
            Assert.That(nullLimits.Error, Is.EqualTo(SvgCssParseError.InvalidArgument));
            Assert.That(invalid.Error, Is.EqualTo(SvgCssParseError.InvalidArgument));
            Assert.That(nullSource.Error, Is.EqualTo(SvgCssParseError.InvalidArgument));
            Assert.That(nullText.ErrorOffset, Is.Zero);
            Assert.That(nullText.Declarations, Is.Empty);
        });
    }

    static SvgCssLimits Limits()
    {
        return new SvgCssLimits();
    }
}
