
namespace UniversalHtml.Tests;

[TestFixture]
public class CssDeclarationParserTests
{
    [Test]
    public void ParsesDeclarationsAndKeepsStructuredValueTokens()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            " COLOR : red ; width: calc(100% - 2px)");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(2));
            Assert.That(declarations[0].GetName(), Is.EqualTo("COLOR"));
            Assert.That(declarations[0].GetCanonicalName(),
                Is.EqualTo("color"));
            Assert.That(declarations[0].GetValueTokens(),
                Has.Length.EqualTo(1));
            Assert.That(declarations[0].GetValueTokens()[0].GetValue(),
                Is.EqualTo("red"));
            Assert.That(declarations[1].GetValueTokens().Select(token =>
                token.GetKind()), Is.EqualTo(new[]
                {
                    CssTokenKind.Function,
                    CssTokenKind.Percentage,
                    CssTokenKind.Whitespace,
                    CssTokenKind.Delimiter,
                    CssTokenKind.Whitespace,
                    CssTokenKind.Dimension,
                    CssTokenKind.CloseParenthesis
                }));
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetErrorCount(), Is.Zero);
        });
    }

    [Test]
    public void ExtractsOnlyTrailingTopLevelImportantMarkers()
    {
        CssDeclaration[] declarations = CssDeclarationParser.Parse(
            @"color:red ! IMPORTANT; nested:fn(!important); escaped:blue !\69mportant; literal:!important now")
            .GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(4));
            Assert.That(declarations[0].IsImportant(), Is.True);
            Assert.That(declarations[0].GetValueTokens(),
                Has.Length.EqualTo(1));
            Assert.That(declarations[0].GetValueTokens()[0].GetValue(),
                Is.EqualTo("red"));
            Assert.That(declarations[0].GetImportantSpan().IsKnown(), Is.True);
            Assert.That(declarations[1].IsImportant(), Is.False);
            Assert.That(declarations[1].GetImportantSpan().IsKnown(), Is.False);
            Assert.That(declarations[2].IsImportant(), Is.True);
            Assert.That(declarations[3].IsImportant(), Is.False);
        });
    }

    [Test]
    public void RecoversAtTopLevelSemicolonsAfterMalformedDeclarations()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "; broken; @media print; good:one; no-colon value; also:two");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations.Select(value => value.GetName()),
                Is.EqualTo(new[] { "good", "also" }));
            Assert.That(result.GetErrorCount(), Is.EqualTo(3));
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
        });
    }

    [Test]
    public void SemicolonsInsideNestedComponentsDoNotSplitDeclarations()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "one:fn(a;b);two:[x;y];three:{z;q};four:ok");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations.Select(value => value.GetName()),
                Is.EqualTo(new[] { "one", "two", "three", "four" }));
            Assert.That(declarations[0].GetValueTokens().Any(token =>
                token.GetKind() == CssTokenKind.Semicolon), Is.True);
            Assert.That(declarations[1].GetValueTokens().Any(token =>
                token.GetKind() == CssTokenKind.Semicolon), Is.True);
            Assert.That(declarations[2].GetValueTokens().Any(token =>
                token.GetKind() == CssTokenKind.Semicolon), Is.True);
            Assert.That(result.GetErrorCount(), Is.Zero);
        });
    }

    [Test]
    public void UnclosedComponentsConsumeToEndAndReportRecovery()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "first:fn(a;b; later:red");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(1));
            Assert.That(declarations[0].GetName(), Is.EqualTo("first"));
            Assert.That(declarations[0].GetValueTokens().Count(token =>
                token.GetKind() == CssTokenKind.Semicolon), Is.EqualTo(2));
            Assert.That(result.GetErrorCount(), Is.EqualTo(1));
            Assert.That(result.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void KeepsDuplicatesAndUsesCssPropertyNameComparisonRules()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "COLOR:red;color:blue;--Theme:a;--Theme:b;--theme:c");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations.Select(value => value.IsDuplicate()),
                Is.EqualTo(new[] { false, true, false, true, false }));
            Assert.That(result.GetDuplicateCount(), Is.EqualTo(2));
            Assert.That(declarations[0].GetCanonicalName(),
                Is.EqualTo("color"));
            Assert.That(declarations[1].GetCanonicalName(),
                Is.EqualTo("color"));
            Assert.That(declarations[2].IsCustomProperty(), Is.True);
            Assert.That(declarations[2].GetCanonicalName(),
                Is.EqualTo("--Theme"));
            Assert.That(declarations[4].IsCustomProperty(), Is.True);
            Assert.That(declarations[4].IsDuplicate(), Is.False);
        });
    }

    [Test]
    public void EscapedNamesCanFormCustomPropertiesAndDuplicates()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            @"-\2d Theme:first;--Theme:second");
        CssDeclaration[] declarations = result.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(2));
            Assert.That(declarations[0].GetName(), Is.EqualTo("--Theme"));
            Assert.That(declarations[0].IsCustomProperty(), Is.True);
            Assert.That(declarations[1].IsDuplicate(), Is.True);
            Assert.That(result.GetDuplicateCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void SourceSpansCoverNamesValuesImportanceAndSeparators()
    {
        CssDeclaration[] declarations = CssDeclarationParser.Parse(
            "  color:\r\n red !important ; width:2px")
            .GetDeclarations();
        CssDeclaration color = declarations[0];
        CssDeclaration width = declarations[1];

        Assert.Multiple(() =>
        {
            Assert.That(color.GetNameSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(2));
            Assert.That(color.GetNameSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(7));
            Assert.That(color.GetValueSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(11));
            Assert.That(color.GetValueSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(14));
            Assert.That(color.GetImportantSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(15));
            Assert.That(color.GetImportantSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(25));
            Assert.That(color.GetSourceSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(2));
            Assert.That(color.GetSourceSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(27));
            Assert.That(color.GetValueSpan().GetStart()
                .GetNormalizedCharacterOffset(), Is.EqualTo(10));
            Assert.That(width.GetSourceSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(37));
        });
    }

    [Test]
    public void SupportsEmptyValuesAndImportantOnlyValues()
    {
        CssDeclaration[] declarations = CssDeclarationParser.Parse(
            "empty: ; flag:!important;").GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(2));
            Assert.That(declarations[0].GetValueTokens(), Is.Empty);
            Assert.That(declarations[0].GetValueSpan().GetCharacterLength(),
                Is.Zero);
            Assert.That(declarations[0].IsImportant(), Is.False);
            Assert.That(declarations[1].GetValueTokens(), Is.Empty);
            Assert.That(declarations[1].IsImportant(), Is.True);
        });
    }

    [Test]
    public void DeclarationLimitReturnsOnlyTheBoundedPrefix()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumCssDeclarations(2);

        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "a:1;b:2;c:3", limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDeclarations().Select(value =>
                value.GetName()), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetErrorCount(), Is.Zero);
        });
    }

    [Test]
    public void TokenLimitKeepsOnlyDeclarationsEndingBeforeTheCutoff()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumCssTokens(4);

        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "a:1;b:2", limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDeclarations(), Has.Length.EqualTo(1));
            Assert.That(result.GetDeclarations()[0].GetName(), Is.EqualTo("a"));
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
        });
    }

    [Test]
    public void TokenizerErrorsAreReportedWithoutPreventingRecovery()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "image:url(bad\"part); color:red");

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDeclarations().Select(value =>
                value.GetName()), Is.EqualTo(new[] { "image", "color" }));
            Assert.That(result.GetDeclarations()[0].GetValueTokens()[0]
                .GetKind(), Is.EqualTo(CssTokenKind.BadUrl));
            Assert.That(result.GetErrorCount(), Is.EqualTo(1));
            Assert.That(result.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void ResultsAndDeclarationValuesOwnTheirArrays()
    {
        CssDeclarationParseResult result = CssDeclarationParser.Parse(
            "color:red");
        CssDeclaration[] declarations = result.GetDeclarations();
        CssToken[] values = declarations[0].GetValueTokens();
        declarations[0] = null;
        values[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDeclarations()[0].GetName(),
                Is.EqualTo("color"));
            Assert.That(result.GetDeclarations()[0].GetValueTokens()[0]
                .GetValue(), Is.EqualTo("red"));
            Assert.That(() => CssDeclarationParser.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(() => CssDeclarationParser.Parse("x:y",
                (HtmlLimits)null), Throws.ArgumentNullException);
        });
    }
}
