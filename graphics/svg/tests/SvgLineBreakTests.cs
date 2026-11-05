namespace svg.Tests;

public class SvgLineBreakTests
{
    [Test]
    public void NormalModeCollapsesWhitespaceAndTrimsLineEdges()
    {
        SvgInlineTextResult result = new SvgInlineTextLayout().LayoutWithText("  a\t \r\n  b  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProcessedText, Is.EqualTo(" a b "));
            Assert.That(result.RenderedText, Is.EqualTo("a b"));
            Assert.That(result.Lines, Has.Count.EqualTo(1));
            Assert.That(result.Lines[0].Text, Is.EqualTo("a b"));
            Assert.That(result.Lines[0].InlineAdvance, Is.EqualTo(3));
            Assert.That(result.Items.Count(value => value.Discarded), Is.GreaterThan(0));
            Assert.That(result.Items.Count(value => value.Trimmed), Is.EqualTo(2));
            Assert.That(result.ClusterToLine.Count(value => value < 0), Is.GreaterThan(0));
        });
    }

    [Test]
    public void PreservedAndPreLineModesKeepForcedBreaksWithTheirWhitespaceRules()
    {
        SvgInlineTextOptions preOptions = new SvgInlineTextOptions();
        preOptions.WhiteSpace = SvgWhiteSpaceMode.Pre;
        preOptions.TabSize = 4;
        SvgInlineTextOptions preLineOptions = new SvgInlineTextOptions();
        preLineOptions.WhiteSpace = SvgWhiteSpaceMode.PreLine;

        SvgInlineTextResult pre = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions(" a\tb\r\n c ", preOptions);
        SvgInlineTextResult preLine = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("  a  \n  b  ", preLineOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pre.Success, Is.True);
            Assert.That(pre.ProcessedText, Is.EqualTo(" a\tb\n c "));
            Assert.That(pre.RenderedText, Is.EqualTo(" a\tb\n c "));
            Assert.That(pre.Lines, Has.Count.EqualTo(2));
            Assert.That(pre.Lines[0].HardBreak, Is.True);
            Assert.That(pre.Lines[0].InlineAdvance, Is.EqualTo(5));
            Assert.That(pre.Lines[0].ItemIndices.Select(index => pre.Items[index].Text), Does.Contain("\t"));
            Assert.That(preLine.Success, Is.True);
            Assert.That(preLine.ProcessedText, Is.EqualTo(" a\nb "));
            Assert.That(preLine.RenderedText, Is.EqualTo("a\nb"));
            Assert.That(preLine.Lines.Select(line => line.Text), Is.EqualTo(new[] { "a", "b" }));
        });
    }

    [Test]
    public void NoWrapIgnoresInlineConstraintsWhileBreakSpacesWrapsPreservedSpaces()
    {
        SvgInlineTextOptions noWrapOptions = new SvgInlineTextOptions();
        noWrapOptions.WhiteSpace = SvgWhiteSpaceMode.NoWrap;
        noWrapOptions.MaxInlineSize = 2;
        SvgInlineTextOptions breakSpacesOptions = new SvgInlineTextOptions();
        breakSpacesOptions.WhiteSpace = SvgWhiteSpaceMode.BreakSpaces;
        breakSpacesOptions.MaxInlineSize = 2;

        SvgInlineTextResult noWrap = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a   b", noWrapOptions);
        SvgInlineTextResult breakSpaces = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a  b", breakSpacesOptions);

        Assert.Multiple(() =>
        {
            Assert.That(noWrap.Success, Is.True);
            Assert.That(noWrap.Lines.Select(line => line.Text), Is.EqualTo(new[] { "a b" }));
            Assert.That(noWrap.Lines[0].InlineAdvance, Is.EqualTo(3));
            Assert.That(breakSpaces.Success, Is.True);
            Assert.That(breakSpaces.ProcessedText, Is.EqualTo("a  b"));
            Assert.That(breakSpaces.Lines.Select(line => line.Text), Is.EqualTo(new[] { "a ", " b" }));
            Assert.That(breakSpaces.Items.Where(item => item.PreservedSpace).Select(item => item.BreakAfter), Is.All.EqualTo(SvgLineBreakOpportunity.Allowed));
        });
    }

    [Test]
    public void WrapsAtNaturalOpportunitiesUsingCallerProvidedClusterAdvances()
    {
        SvgInlineTextOptions options = new SvgInlineTextOptions();
        options.MaxInlineSize = 3;
        options.ClusterAdvances.AddRange(new[] { 2.0, 1.0, 2.0, 1.0, 1.0, 1.0, 1.0 });

        SvgInlineTextResult result = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a b-cde", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Lines.Select(line => line.Text), Is.EqualTo(new[] { "a", "b-", "cde" }));
            Assert.That(result.Lines.Select(line => line.InlineAdvance), Is.EqualTo(new[] { 2.0, 3.0, 3.0 }));
            Assert.That(result.Items[1].BreakAfter, Is.EqualTo(SvgLineBreakOpportunity.Allowed));
            Assert.That(result.Items[3].BreakAfter, Is.EqualTo(SvgLineBreakOpportunity.Allowed));
            Assert.That(result.ClusterToLine, Is.EqualTo(new[] { 0, -1, 1, 1, 2, 2, 2 }));
        });
    }

    [Test]
    public void KeepsGraphemeClustersWholeAndSupportsEmergencyOverflowWrapping()
    {
        SvgInlineTextOptions noEmergency = new SvgInlineTextOptions();
        noEmergency.MaxInlineSize = 2;
        SvgInlineTextOptions emergency = new SvgInlineTextOptions();
        emergency.MaxInlineSize = 2;
        emergency.OverflowWrap = true;

        SvgInlineTextResult unbroken = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("abcd", noEmergency);
        SvgInlineTextResult broken = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("A\u0301BCD", emergency);
        SvgInlineTextResult cjk = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("漢字かな", noEmergency);
        SvgInlineTextResult zeroWidth = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("ab\u200Bcd", noEmergency);
        SvgInlineTextResult wordJoiner = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("ab\u2060cd", noEmergency);

        Assert.Multiple(() =>
        {
            Assert.That(unbroken.Lines.Select(line => line.Text), Is.EqualTo(new[] { "abcd" }));
            Assert.That(unbroken.Lines[0].InlineAdvance, Is.EqualTo(4));
            Assert.That(broken.Lines.Select(line => line.Text), Is.EqualTo(new[] { "A\u0301B", "CD" }));
            Assert.That(broken.Items[0].Text, Is.EqualTo("A\u0301"));
            Assert.That(broken.Items[0].SourceUtf16Length, Is.EqualTo(2));
            Assert.That(cjk.Lines.Select(line => line.Text), Is.EqualTo(new[] { "漢字", "かな" }));
            Assert.That(zeroWidth.Lines.Select(line => line.Text), Is.EqualTo(new[] { "ab", "cd" }));
            Assert.That(zeroWidth.Items[2].Advance, Is.Zero);
            Assert.That(zeroWidth.Items[2].BreakAfter, Is.EqualTo(SvgLineBreakOpportunity.Allowed));
            Assert.That(wordJoiner.Lines.Select(line => line.Text), Is.EqualTo(new[] { "abcd" }));
            Assert.That(wordJoiner.Items[2].Advance, Is.Zero);
            Assert.That(wordJoiner.Items[2].BreakAfter, Is.EqualTo(SvgLineBreakOpportunity.Prohibited));
        });
    }

    [Test]
    public void ProducesBidiVisualLinesForTheRequestedDirection()
    {
        SvgInlineTextOptions automatic = new SvgInlineTextOptions();
        SvgInlineTextOptions rightToLeft = new SvgInlineTextOptions();
        rightToLeft.Direction = SvgTextDirection.RightToLeft;

        SvgInlineTextResult autoResult = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("abc אבג", automatic);
        SvgInlineTextResult rtlResult = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("abc אבג", rightToLeft);

        Assert.Multiple(() =>
        {
            Assert.That(autoResult.Success, Is.True);
            Assert.That(autoResult.Lines[0].BaseLevel, Is.Zero);
            Assert.That(autoResult.Lines[0].VisualText, Is.EqualTo("abc גבא"));
            Assert.That(autoResult.Lines[0].VisualItemIndices.Select(index => autoResult.Items[index].Text), Is.EqualTo(new[] { "a", "b", "c", " ", "ג", "ב", "א" }));
            Assert.That(rtlResult.Success, Is.True);
            Assert.That(rtlResult.Lines[0].BaseLevel, Is.EqualTo(1));
            Assert.That(rtlResult.Lines[0].VisualText, Is.EqualTo("גבא abc"));
        });
    }

    [Test]
    public void ResolvesVerticalOrientationAndBlockAxisLineOffsets()
    {
        SvgInlineTextOptions vertical = new SvgInlineTextOptions();
        vertical.WritingMode = SvgWritingMode.VerticalRightToLeft;
        vertical.WhiteSpace = SvgWhiteSpaceMode.PreLine;
        vertical.LineAdvance = 12;
        SvgInlineTextOptions sideways = new SvgInlineTextOptions();
        sideways.WritingMode = SvgWritingMode.SidewaysLeftToRight;

        SvgInlineTextResult mixed = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("A漢1😀\nB", vertical);
        SvgInlineTextResult rotated = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("A漢", sideways);

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Success, Is.True);
            Assert.That(mixed.Items[0].Orientation, Is.EqualTo(SvgGlyphOrientation.Sideways));
            Assert.That(mixed.Items[1].Orientation, Is.EqualTo(SvgGlyphOrientation.Upright));
            Assert.That(mixed.Items[2].Orientation, Is.EqualTo(SvgGlyphOrientation.Sideways));
            Assert.That(mixed.Items[3].Orientation, Is.EqualTo(SvgGlyphOrientation.Upright));
            Assert.That(mixed.Lines.Select(line => line.BlockOffset), Is.EqualTo(new[] { 0.0, -12.0 }));
            Assert.That(rotated.Items.Select(item => item.Orientation), Is.All.EqualTo(SvgGlyphOrientation.Sideways));
        });
    }

    [Test]
    public void HandlesTrailingForcedBreaksAndValidatesLimitsCancellationAndAdvances()
    {
        SvgInlineTextOptions pre = new SvgInlineTextOptions();
        pre.WhiteSpace = SvgWhiteSpaceMode.Pre;
        SvgInlineTextOptions lineLimit = new SvgInlineTextOptions();
        lineLimit.WhiteSpace = SvgWhiteSpaceMode.Pre;
        lineLimit.MaxLines = 1;
        SvgInlineTextOptions clusterLimit = new SvgInlineTextOptions();
        clusterLimit.MaxClusters = 1;
        SvgInlineTextOptions mismatched = new SvgInlineTextOptions();
        mismatched.ClusterAdvances.Add(1);
        SvgInlineTextOptions invalid = new SvgInlineTextOptions();
        invalid.MaxInlineSize = double.NaN;
        SvgInlineTextOptions cancelled = new SvgInlineTextOptions();
        System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;

        SvgInlineTextResult trailingBreak = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a\n", pre);
        SvgInlineTextResult tooManyLines = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a\nb", lineLimit);
        SvgInlineTextResult tooManyClusters = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("ab", clusterLimit);
        SvgInlineTextResult wrongAdvances = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("ab", mismatched);
        SvgInlineTextResult invalidOptions = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a", invalid);
        SvgInlineTextResult stopped = new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions("a", cancelled);
        SvgInlineTextResult nullText = new SvgInlineTextLayout().LayoutWithText(null);

        Assert.Multiple(() =>
        {
            Assert.That(trailingBreak.Success, Is.True);
            Assert.That(trailingBreak.Lines.Select(line => line.Text), Is.EqualTo(new[] { "a", "" }));
            Assert.That(tooManyLines.Error, Is.EqualTo(SvgInlineTextError.LimitExceeded));
            Assert.That(tooManyClusters.Error, Is.EqualTo(SvgInlineTextError.LimitExceeded));
            Assert.That(wrongAdvances.Error, Is.EqualTo(SvgInlineTextError.InvalidArgument));
            Assert.That(invalidOptions.Error, Is.EqualTo(SvgInlineTextError.InvalidArgument));
            Assert.That(stopped.Error, Is.EqualTo(SvgInlineTextError.Cancelled));
            Assert.That(nullText.Error, Is.EqualTo(SvgInlineTextError.InvalidArgument));
        });
    }
}
