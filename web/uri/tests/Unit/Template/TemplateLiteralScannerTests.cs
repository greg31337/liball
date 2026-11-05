namespace uri.Tests;

public class TemplateLiteralScannerTests
{
    private static readonly int[] PrintableUnicodeBoundaries = new int[]
    {
        0x00A0, 0xD7FF,
        0xF900, 0xFDCF,
        0xFDF0, 0xFFEF,
        0x10000, 0x1FFFD,
        0x20000, 0x2FFFD,
        0x30000, 0x3FFFD,
        0x40000, 0x4FFFD,
        0x50000, 0x5FFFD,
        0x60000, 0x6FFFD,
        0x70000, 0x7FFFD,
        0x80000, 0x8FFFD,
        0x90000, 0x9FFFD,
        0xA0000, 0xAFFFD,
        0xB0000, 0xBFFFD,
        0xC0000, 0xCFFFD,
        0xD0000, 0xDFFFD,
        0xE1000, 0xEFFFD,
        0xE000, 0xF8FF,
        0xF0000, 0xFFFFD,
        0x100000, 0x10FFFD
    };

    [Test]
    public void EveryAllowedAsciiLiteralRangeIsPreservedInOnePart()
    {
        const string text = "!#$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~";

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.OriginalText, Is.EqualTo(text));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(1));
            AssertLiteralPart(result.Template.Parts[0], text, 0, text.Length);
        });
    }

    [TestCase("\u0000")]
    [TestCase("\u001F")]
    [TestCase(" ")]
    [TestCase("\"")]
    [TestCase("<")]
    [TestCase(">")]
    [TestCase("\\")]
    [TestCase("^")]
    [TestCase("`")]
    [TestCase("|")]
    [TestCase("\u007F")]
    [TestCase("\u0080")]
    [TestCase("\u009F")]
    [TestCase("\uFDD0")]
    [TestCase("\uFFFF")]
    public void ForbiddenLiteralScalarsReturnExactTemplateDiagnostics(string value)
    {
        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            ("a" + value + "b").AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidTemplate,
            UriComponent.TemplateLiteral,
            1,
            value.Length,
            TemplateParserCore.InvalidLiteralMessage);
    }

    [TestCaseSource(nameof(PrintableUnicodeBoundaries))]
    public void UcscharAndIprivateBoundariesAreLegalPrintableLiterals(
        int scalarValue)
    {
        string value = char.ConvertFromUtf32(scalarValue);
        string text = "a" + value + "b";

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "U+" + scalarValue.ToString("X"));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Parts, Has.Length.EqualTo(1));
            AssertLiteralPart(result.Template.Parts[0], text, 0, text.Length);
        });
    }

    [Test]
    public void CompletePercentTripletsOfEitherHexCaseRemainLiteralText()
    {
        const string text = "a%20%af%FFb";

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Parts, Has.Length.EqualTo(1));
            AssertLiteralPart(result.Template.Parts[0], text, 0, text.Length);
        });
    }

    [TestCase("a%", 1)]
    [TestCase("a%0", 1)]
    [TestCase("a%G0", 2)]
    [TestCase("a%0G", 3)]
    public void MalformedLiteralPercentTripletsReportTheExactCharacter(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.TemplateLiteral,
            offset,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void LiteralAndExpressionPartsRetainOrderedExactSourceRanges()
    {
        const string text = "café/{var}/x{?q}尾";

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.OriginalText, Is.EqualTo(text));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(5));
            AssertLiteralPart(result.Template.Parts[0], "café/", 0, 5);
            AssertExpressionPart(result.Template.Parts[1], 5, 5);
            AssertLiteralPart(result.Template.Parts[2], "/x", 10, 2);
            AssertExpressionPart(result.Template.Parts[3], 12, 4);
            AssertLiteralPart(result.Template.Parts[4], "尾", 16, 1);
        });
    }

    [Test]
    public void EmptyAndAdjacentExpressionTemplatesDoNotCreateEmptyLiterals()
    {
        UriTemplateParseResult empty = TemplateParserCore.ScanTemplateStructure(
            "".AsSpan());
        UriTemplateParseResult adjacent = TemplateParserCore.ScanTemplateStructure(
            "{a}{b}".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Template.OriginalText, Is.Empty);
            Assert.That(empty.Template.Parts, Is.Empty);
            Assert.That(adjacent.Success, Is.True);
            Assert.That(adjacent.Template.Parts, Has.Length.EqualTo(2));
            AssertExpressionPart(adjacent.Template.Parts[0], 0, 3);
            AssertExpressionPart(adjacent.Template.Parts[1], 3, 3);
        });
    }

    [Test]
    public void UnmatchedUnclosedAndNestedBracesHaveDistinctExactDiagnostics()
    {
        UriTemplateParseResult closing = TemplateParserCore.ScanTemplateStructure(
            "a}b".AsSpan());
        UriTemplateParseResult unclosed = TemplateParserCore.ScanTemplateStructure(
            "a{bc".AsSpan());
        UriTemplateParseResult nested = TemplateParserCore.ScanTemplateStructure(
            "a{b{c}d}".AsSpan());

        Assert.Multiple(() =>
        {
            AssertFailure(
                closing,
                UriDiagnosticCode.InvalidTemplateExpression,
                UriComponent.TemplateExpression,
                1,
                1,
                TemplateParserCore.UnmatchedClosingBraceMessage);
            AssertFailure(
                unclosed,
                UriDiagnosticCode.InvalidTemplateExpression,
                UriComponent.TemplateExpression,
                1,
                3,
                TemplateParserCore.UnclosedExpressionMessage);
            AssertFailure(
                nested,
                UriDiagnosticCode.InvalidTemplateExpression,
                UriComponent.TemplateExpression,
                3,
                1,
                TemplateParserCore.NestedExpressionMessage);
        });
    }

    [TestCase(0xD800, true)]
    [TestCase(0xDC00, true)]
    [TestCase(0xD800, false)]
    public void MalformedUtf16OutsideExpressionsIsRejectedPrecisely(
        int characterValue,
        bool appendTrailingCharacter)
    {
        string text = "a" + new string((char)characterValue, 1);
        if (appendTrailingCharacter)
        {
            text = text + "b";
        }

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            text.AsSpan());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral,
            1,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void SpanScanningUsesOnlyTheProvidedSliceAndRelativeRanges()
    {
        const string template = "α{var}β";
        string padded = "before" + template + "after";

        UriTemplateParseResult result = TemplateParserCore.ScanTemplateStructure(
            padded.AsSpan(6, template.Length));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.OriginalText, Is.EqualTo(template));
            Assert.That(result.Template.Parts, Has.Length.EqualTo(3));
            AssertLiteralPart(result.Template.Parts[0], "α", 0, 1);
            AssertExpressionPart(result.Template.Parts[1], 1, 5);
            AssertLiteralPart(result.Template.Parts[2], "β", 6, 1);
        });
    }

    private static void AssertLiteralPart(
        UriTemplatePart part,
        string literal,
        int offset,
        int length)
    {
        Assert.That(part.Kind, Is.EqualTo(UriTemplatePartKind.Literal));
        Assert.That(part.Literal, Is.EqualTo(literal));
        Assert.That(part.Operator, Is.EqualTo(UriTemplateOperator.Simple));
        Assert.That(part.Variables, Is.Empty);
        Assert.That(part.Range.Offset, Is.EqualTo(offset));
        Assert.That(part.Range.Length, Is.EqualTo(length));
    }

    private static void AssertExpressionPart(
        UriTemplatePart part,
        int offset,
        int length)
    {
        Assert.That(part.Kind, Is.EqualTo(UriTemplatePartKind.Expression));
        Assert.That(part.Literal, Is.Empty);
        Assert.That(part.Operator, Is.EqualTo(UriTemplateOperator.Simple));
        Assert.That(part.Variables, Is.Empty);
        Assert.That(part.Range.Offset, Is.EqualTo(offset));
        Assert.That(part.Range.Length, Is.EqualTo(length));
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
