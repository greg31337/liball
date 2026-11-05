using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class NumberFormatParserTests
{
    [Test]
    public void ParseBuiltIn_RecognizesCommonBuiltInFormats()
    {
        XlsxNumberFormatParseResult general = XlsxNumberFormatParser.ParseBuiltIn(0);
        XlsxNumberFormatParseResult number = XlsxNumberFormatParser.ParseBuiltIn(4);
        XlsxNumberFormatParseResult date = XlsxNumberFormatParser.ParseBuiltIn(14);
        XlsxNumberFormatParseResult text = XlsxNumberFormatParser.ParseBuiltIn(49);

        Assert.That(general.IsValid, Is.True);
        Assert.That(general.IsBuiltIn, Is.True);
        Assert.That(general.FormatCode, Is.EqualTo("General"));
        Assert.That(number.IsValid, Is.True);
        Assert.That(number.FormatCode, Is.EqualTo("#,##0.00"));
        Assert.That(HasTokenKind(number.Sections[0], XlsxNumberFormatTokenKind.ThousandsSeparator), Is.True);
        Assert.That(HasTokenKind(number.Sections[0], XlsxNumberFormatTokenKind.DecimalPoint), Is.True);
        Assert.That(date.IsValid, Is.True);
        Assert.That(date.FormatCode, Is.EqualTo("m/d/yy"));
        Assert.That(HasTokenKind(date.Sections[0], XlsxNumberFormatTokenKind.DateTimeCode), Is.True);
        Assert.That(text.IsValid, Is.True);
        Assert.That(text.Sections[0].Tokens[0].Kind, Is.EqualTo(XlsxNumberFormatTokenKind.TextPlaceholder));
    }

    [Test]
    public void ParseCustom_SplitsSectionsAndReadsColorConditionAndTextSection()
    {
        XlsxNumberFormatParseResult result = XlsxNumberFormatParser.ParseCustom("#,##0.00;[Red](#,##0.00);\"-\";@");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Sections.Count, Is.EqualTo(4));
        Assert.That(result.Sections[0].FormatCode, Is.EqualTo("#,##0.00"));
        Assert.That(result.Sections[0].Tokens[0].Kind, Is.EqualTo(XlsxNumberFormatTokenKind.NumberPlaceholder));
        Assert.That(HasTokenKind(result.Sections[0], XlsxNumberFormatTokenKind.ThousandsSeparator), Is.True);
        Assert.That(HasTokenKind(result.Sections[0], XlsxNumberFormatTokenKind.DecimalPoint), Is.True);
        Assert.That(result.Sections[1].HasColor, Is.True);
        Assert.That(result.Sections[1].Color, Is.EqualTo("Red"));
        Assert.That(result.Sections[2].Tokens[0].Kind, Is.EqualTo(XlsxNumberFormatTokenKind.Literal));
        Assert.That(result.Sections[2].Tokens[0].Text, Is.EqualTo("-"));
        Assert.That(result.Sections[3].Tokens[0].Kind, Is.EqualTo(XlsxNumberFormatTokenKind.TextPlaceholder));
    }

    [Test]
    public void ParseCustom_ReadsConditionsDateTimeExponentPercentFillAndSpacingTokens()
    {
        XlsxNumberFormatParseResult conditional = XlsxNumberFormatParser.ParseCustom("[>=1]m/d/yy h:mm AM/PM");
        XlsxNumberFormatParseResult scientific = XlsxNumberFormatParser.ParseCustom("0.00E+00%");
        XlsxNumberFormatParseResult fillAndSpacing = XlsxNumberFormatParser.ParseCustom("_-* #,##0");

        Assert.That(conditional.IsValid, Is.True);
        Assert.That(conditional.Sections[0].HasCondition, Is.True);
        Assert.That(conditional.Sections[0].Condition, Is.EqualTo(">=1"));
        Assert.That(HasTokenKind(conditional.Sections[0], XlsxNumberFormatTokenKind.DateTimeCode), Is.True);
        Assert.That(scientific.IsValid, Is.True);
        Assert.That(HasTokenKind(scientific.Sections[0], XlsxNumberFormatTokenKind.Exponent), Is.True);
        Assert.That(HasTokenKind(scientific.Sections[0], XlsxNumberFormatTokenKind.Percent), Is.True);
        Assert.That(fillAndSpacing.IsValid, Is.True);
        Assert.That(HasTokenKind(fillAndSpacing.Sections[0], XlsxNumberFormatTokenKind.Spacing), Is.True);
        Assert.That(HasTokenKind(fillAndSpacing.Sections[0], XlsxNumberFormatTokenKind.Fill), Is.True);
    }

    [Test]
    public void ParseCustom_ReportsInvalidFormatSyntax()
    {
        XlsxNumberFormatParseResult? nullFormat = XlsxNumberFormatParser.ParseCustom(null!);
        XlsxNumberFormatParseResult tooManySections = XlsxNumberFormatParser.ParseCustom("0;0;0;0;0");
        XlsxNumberFormatParseResult unclosedQuote = XlsxNumberFormatParser.ParseCustom("0\"bad");
        XlsxNumberFormatParseResult unclosedBracket = XlsxNumberFormatParser.ParseCustom("[Red");
        XlsxNumberFormatParseResult unknownBuiltIn = XlsxNumberFormatParser.ParseBuiltIn(999);

        Assert.That(nullFormat.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullFormat, "numberFormat.formatNull"), Is.True);
        Assert.That(tooManySections.IsValid, Is.False);
        Assert.That(HasDiagnostic(tooManySections, "numberFormat.tooManySections"), Is.True);
        Assert.That(unclosedQuote.IsValid, Is.False);
        Assert.That(HasDiagnostic(unclosedQuote, "numberFormat.quoteUnclosed"), Is.True);
        Assert.That(unclosedBracket.IsValid, Is.False);
        Assert.That(HasDiagnostic(unclosedBracket, "numberFormat.bracketUnclosed"), Is.True);
        Assert.That(unknownBuiltIn.IsValid, Is.False);
        Assert.That(HasDiagnostic(unknownBuiltIn, "numberFormat.builtInUnknown"), Is.True);
    }

    static bool HasTokenKind(XlsxNumberFormatSection section, XlsxNumberFormatTokenKind kind)
    {
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (token.Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxNumberFormatParseResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
