namespace svg.Tests;

public class SvgCssAdvancedAtRuleTests
{
    [Test]
    public void LayerStatementsEstablishStableDottedFirstAppearanceOrder()
    {
        string css = "@layer reset, theme.base; @layer theme { .a { color:red } } @layer reset { .b{} }";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        SvgStyleSheet sheet = result.StyleSheet;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(sheet.Layers.Select(layer => layer.Name), Is.EqualTo(new[] { "reset", "theme", "theme.base" }));
            Assert.That(sheet.Layers.Select(layer => layer.Order), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(sheet.Layers[2].ParentName, Is.EqualTo("theme"));
            Assert.That(sheet.Rules[0].AtRule.LayerNames, Is.EqualTo(new[] { "reset", "theme.base" }));
            Assert.That(sheet.Rules[1].AtRule.LayerOrder, Is.EqualTo(1));
            Assert.That(sheet.Rules[1].ChildRules, Has.Count.EqualTo(1));
            Assert.That(sheet.Rules[1].ChildRules[0].LayerName, Is.EqualTo("theme"));
            Assert.That(sheet.Rules[1].ChildRules[0].LayerOrder, Is.EqualTo(1));
            Assert.That(sheet.Rules[2].AtRule.LayerOrder, Is.Zero);
        });
    }

    [Test]
    public void NestedAndAnonymousLayersHaveDeterministicDistinctStorage()
    {
        string css = "@layer outer { @layer inner { a{} } @layer { b{} } } @layer { c{} }";

        SvgStyleSheet sheet = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits()).StyleSheet;
        SvgCssRule outer = sheet.Rules[0];
        SvgCssRule nested = outer.ChildRules[0];
        SvgCssRule nestedAnonymous = outer.ChildRules[1];
        SvgCssRule topAnonymous = sheet.Rules[1];

        Assert.Multiple(() =>
        {
            Assert.That(sheet.Layers.Select(layer => layer.Name), Is.EqualTo(new[]
            {
                "outer",
                "outer.inner",
                "outer.<anonymous:0>",
                "<anonymous:1>"
            }));
            Assert.That(sheet.Layers.Select(layer => layer.IsAnonymous), Is.EqualTo(new[] { false, false, true, true }));
            Assert.That(nested.AtRule.LayerName, Is.EqualTo("outer.inner"));
            Assert.That(nested.ChildRules[0].LayerName, Is.EqualTo("outer.inner"));
            Assert.That(nestedAnonymous.ChildRules[0].LayerName, Is.EqualTo("outer.<anonymous:0>"));
            Assert.That(topAnonymous.ChildRules[0].LayerName, Is.EqualTo("<anonymous:1>"));
        });
    }

    [Test]
    public void InitialLayerOrderingMayPrecedeImportsButBodyLayersCloseTheHeader()
    {
        SvgStyleSheet initial = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@layer base; @import 'a.css'; @namespace s 'urn:s'; s|a{}", new SvgCssLimits()).StyleSheet;
        SvgStyleSheet body = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@import 'a.css'; @layer base; @import 'b.css';", new SvgCssLimits()).StyleSheet;

        Assert.Multiple(() =>
        {
            Assert.That(initial.Rules.Select(rule => rule.IsValid), Is.All.True);
            Assert.That(initial.Namespaces.NamedNamespaces["s"], Is.EqualTo("urn:s"));
            Assert.That(body.Rules[0].IsValid, Is.True);
            Assert.That(body.Rules[1].IsValid, Is.True);
            Assert.That(body.Rules[2].IsValid, Is.False);
            Assert.That(body.Rules[2].Diagnostics.Single().Message, Does.Contain("must precede"));
        });
    }

    [Test]
    public void FontFaceStoresOrderedDuplicateDescriptorsAndRejectsImportantDescriptors()
    {
        string validCss = "@font-face { font-family: One; src:url(one); FONT-FAMILY:'Two'; }";
        SvgCssRule validRule = new SvgCssParser().ParseStyleSheetWithValueAndLimits(validCss, new SvgCssLimits()).StyleSheet.Rules[0];
        SvgCssFontFace fontFace = validRule.AtRule.FontFace;
        SvgCssRule invalidRule = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@font-face x { src:url(a)!important }", new SvgCssLimits()).StyleSheet.Rules[0];

        Assert.Multiple(() =>
        {
            Assert.That(fontFace, Is.Not.Null);
            Assert.That(fontFace.IsValid, Is.True);
            Assert.That(fontFace.Descriptors.Select(descriptor => descriptor.Name), Is.EqualTo(new[] { "font-family", "src", "font-family" }));
            Assert.That(fontFace.FindDescriptor("FONT-FAMILY").ValueText, Is.EqualTo("'Two'"));
            Assert.That(validRule.Declarations, Is.EqualTo(fontFace.Descriptors));
            Assert.That(fontFace.Source.StartOffset, Is.Zero);
            Assert.That(fontFace.Source.EndOffset, Is.EqualTo(validCss.Length));
            Assert.That(invalidRule.IsValid, Is.False);
            Assert.That(invalidRule.AtRule.FontFace.IsValid, Is.False);
            Assert.That(invalidRule.Diagnostics.Select(diagnostic => diagnostic.Message), Has.Some.Contains("prelude"));
            Assert.That(invalidRule.Diagnostics.Select(diagnostic => diagnostic.Message), Has.Some.Contains("important"));
        });
    }

    [Test]
    public void KeyframesStoreDecodedNamesOffsetsDeclarationsAndExactStepText()
    {
        string css = "@keyframes \"Fade\" { from, 50% { opacity:0; transform:none } to { opacity: 1 } }";

        SvgCssRule rule = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits()).StyleSheet.Rules[0];
        SvgCssKeyframes keyframes = rule.AtRule.Keyframes;

        Assert.Multiple(() =>
        {
            Assert.That(rule.IsValid, Is.True);
            Assert.That(keyframes.IsValid, Is.True);
            Assert.That(keyframes.Name, Is.EqualTo("Fade"));
            Assert.That(keyframes.RawName, Is.EqualTo("\"Fade\""));
            Assert.That(keyframes.Keyframes, Has.Count.EqualTo(2));
            Assert.That(keyframes.Keyframes[0].Offsets, Is.EqualTo(new[] { 0d, 0.5d }));
            Assert.That(keyframes.Keyframes[0].Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "opacity", "transform" }));
            Assert.That(keyframes.Keyframes[0].RawText, Is.EqualTo("from, 50% { opacity:0; transform:none }"));
            Assert.That(keyframes.Keyframes[1].Offsets, Is.EqualTo(new[] { 1d }));
            Assert.That(keyframes.Keyframes[1].RawText, Is.EqualTo("to { opacity: 1 }"));
        });
    }

    [Test]
    public void InvalidSpecializedAtRuleGrammarProducesLocatedRecovery()
    {
        string css = "@layer a,b {} @layer; @font-face; @keyframes none { 101% { opacity:1!important } bad }";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.IsValid, Is.False);
            Assert.That(result.StyleSheet.Rules.Select(rule => rule.IsValid), Is.All.False);
            Assert.That(result.Diagnostics, Has.Some.Matches<SvgDiagnostic>(diagnostic => diagnostic.Message.Contains("at most one")));
            Assert.That(result.Diagnostics, Has.Some.Matches<SvgDiagnostic>(diagnostic => diagnostic.Message.Contains("at least one")));
            Assert.That(result.Diagnostics, Has.Some.Matches<SvgDiagnostic>(diagnostic => diagnostic.Message.Contains("descriptor block")));
            Assert.That(result.Diagnostics, Has.Some.Matches<SvgDiagnostic>(diagnostic => diagnostic.Message.Contains("non-reserved")));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
        });
    }

    [Test]
    public void UnknownAtRulesRemainLosslessAtTopLevelAndInsideLayers()
    {
        string css = "@vendor /*a*/ alpha; @layer x { @other foo(1, /*b*/ 2) { odd:{x;y}; } @third z; }";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        SvgCssRule nestedBlock = result.StyleSheet.Rules[1].ChildRules[0];
        SvgCssRule nestedStatement = result.StyleSheet.Rules[1].ChildRules[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.IsValid, Is.True);
            Assert.That(result.UnknownSyntax.Select(value => value.RawText), Is.EqualTo(new[]
            {
                "@vendor /*a*/ alpha;",
                "@other foo(1, /*b*/ 2) { odd:{x;y}; }",
                "@third z;"
            }));
            Assert.That(nestedBlock.AtRule.RawText, Is.EqualTo(nestedBlock.AtRule.UnknownSyntax.RawText));
            Assert.That(string.Concat(nestedBlock.AtRule.UnknownSyntax.Tokens.Select(token => token.RawText)), Is.EqualTo(nestedBlock.RawText));
            Assert.That(nestedStatement.AtRule.UnknownSyntax.RawText, Is.EqualTo("@third z;"));
            Assert.That(nestedBlock.LayerName, Is.EqualTo("x"));
        });
    }

    [Test]
    public void KeyframeRulesAndDeclarationsUseExistingParserBounds()
    {
        SvgCssLimits ruleLimits = new SvgCssLimits();
        ruleLimits.MaxRules = 2;
        SvgCssParseResult rules = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@keyframes x { from{} to{} }", ruleLimits);
        SvgCssLimits declarationLimits = new SvgCssLimits();
        declarationLimits.MaxDeclarations = 1;
        SvgCssParseResult declarations = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@keyframes x { from{a:1;b:2} }", declarationLimits);

        Assert.Multiple(() =>
        {
            Assert.That(rules.Success, Is.False);
            Assert.That(rules.Error, Is.EqualTo(SvgCssParseError.RuleLimitExceeded));
            Assert.That(declarations.Success, Is.False);
            Assert.That(declarations.Error, Is.EqualTo(SvgCssParseError.DeclarationLimitExceeded));
        });
    }
}
