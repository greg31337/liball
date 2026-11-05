using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfLayoutInfoTests
{
    [Test]
    public void GetLayoutInfo_AggregatesClusterWordLineScriptAndBidiMetadata()
    {
        string text = "A\u0301 \u05D0)\u4E00\U0001F600";

        UnicodePdfLayoutInfo info = new UnicodePdfText().GetLayoutInfo(text);

        Assert.That(info.GraphemeClusters, Has.Length.EqualTo(6));
        AssertCluster(info.GraphemeClusters[0], 0, 2, UnicodeScript.Latin, UnicodePdfBidiDirection.LeftToRight, 1);
        AssertCluster(info.GraphemeClusters[1], 2, 3, UnicodeScript.Latin, UnicodePdfBidiDirection.LeftToRight, 1);
        AssertCluster(info.GraphemeClusters[2], 3, 4, UnicodeScript.Hebrew, UnicodePdfBidiDirection.RightToLeft, 1);
        AssertCluster(info.GraphemeClusters[3], 4, 5, UnicodeScript.Hebrew, UnicodePdfBidiDirection.RightToLeft, 1);
        AssertCluster(info.GraphemeClusters[4], 5, 6, UnicodeScript.Han, UnicodePdfBidiDirection.LeftToRight, 2);
        AssertCluster(info.GraphemeClusters[5], 6, 8, UnicodeScript.Han, UnicodePdfBidiDirection.LeftToRight, 2);

        Assert.That(ToSegmentText(info.WordSegments), Is.EqualTo(ToSegmentText(new UnicodeSegments().GetWordSegments(text))));
        Assert.That(ToLineText(info.LineBreaks), Is.EqualTo(ToLineText(new UnicodeSegments().GetLineBreakOpportunities(text))));
        Assert.That(info.ScriptRuns, Has.Length.EqualTo(3));
        Assert.That(info.BidiRuns, Has.Length.EqualTo(3));
    }

    [Test]
    public void GetScalarLayout_ProvidesBracketMirrorAndWidthHints()
    {
        string text = "A\u0301\u05D0)\u4E00\U0001F600\u200D";

        UnicodePdfScalarLayout[] scalars = new UnicodePdfText().GetScalarLayout(text);

        Assert.That(scalars, Has.Length.EqualTo(7));
        Assert.That(scalars[0].Width, Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.That(scalars[1].Width, Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(scalars[3].CodePoint, Is.EqualTo(0x0029));
        Assert.That(scalars[3].Direction, Is.EqualTo(UnicodePdfBidiDirection.RightToLeft));
        Assert.That(scalars[3].PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.Close));
        Assert.That(scalars[3].PairedBracketCodePoint, Is.EqualTo(0x0028));
        Assert.That(scalars[3].IsBidiMirrored, Is.True);
        Assert.That(scalars[3].MirrorCodePoint, Is.EqualTo(0x0028));
        Assert.That(scalars[4].Width, Is.EqualTo(UnicodePdfTextWidth.Wide));
        Assert.That(scalars[5].Width, Is.EqualTo(UnicodePdfTextWidth.Wide));
        Assert.That(scalars[5].Utf16Length, Is.EqualTo(2));
        Assert.That(scalars[6].Width, Is.EqualTo(UnicodePdfTextWidth.Zero));
    }

    [Test]
    public void GetScalarLayout_ReportsOpenCloseAndNonBracketMirrorMetadata()
    {
        UnicodePdfScalarLayout[] scalars = new UnicodePdfText().GetScalarLayout("()\u2211A");

        Assert.That(scalars[0].PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.Open));
        Assert.That(scalars[0].PairedBracketCodePoint, Is.EqualTo(0x0029));
        Assert.That(scalars[0].IsBidiMirrored, Is.True);
        Assert.That(scalars[0].MirrorCodePoint, Is.EqualTo(0x0029));
        Assert.That(scalars[1].PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.Close));
        Assert.That(scalars[1].PairedBracketCodePoint, Is.EqualTo(0x0028));
        Assert.That(scalars[2].PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.None));
        Assert.That(scalars[2].IsBidiMirrored, Is.True);
        Assert.That(scalars[2].MirrorCodePoint, Is.EqualTo(0x2211));
        Assert.That(scalars[3].PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.None));
        Assert.That(scalars[3].PairedBracketCodePoint, Is.EqualTo(0x0041));
        Assert.That(scalars[3].IsBidiMirrored, Is.False);
        Assert.That(scalars[3].MirrorCodePoint, Is.EqualTo(0x0041));
    }

    [Test]
    public void TextWidthClassification_HandlesZeroNarrowAndWideScalars()
    {
        UnicodePdfText pdfText = new UnicodePdfText();

        Assert.That(pdfText.GetTextWidthClass(0x0301), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0xFE0F), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x200B), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x200D), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x2060), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x0000), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x000A), Is.EqualTo(UnicodePdfTextWidth.Zero));
        Assert.That(pdfText.GetTextWidthClass(0x0041), Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.That(pdfText.GetTextWidthClass(0x0391), Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.That(pdfText.GetTextWidthClass(0xFF66), Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.That(pdfText.GetTextWidthClass(0xFF01), Is.EqualTo(UnicodePdfTextWidth.Wide));
        Assert.That(pdfText.GetTextWidthClass(0x4E00), Is.EqualTo(UnicodePdfTextWidth.Wide));
        Assert.That(pdfText.GetTextWidthClass(0x1F600), Is.EqualTo(UnicodePdfTextWidth.Wide));
        Assert.Throws<ArgumentOutOfRangeException>(() => pdfText.GetTextWidthClass(0xD800));
        Assert.Throws<ArgumentOutOfRangeException>(() => pdfText.GetTextWidthClass(0x110000));
    }

    [Test]
    public void LayoutInfo_UsesClusterWidthForEmojiVariationSequences()
    {
        UnicodePdfLayoutInfo info = new UnicodePdfText().GetLayoutInfo("\u2764\uFE0F");

        Assert.That(info.GraphemeClusters, Has.Length.EqualTo(1));
        Assert.That(info.GraphemeClusters[0].Utf16Length, Is.EqualTo(2));
        Assert.That(info.GraphemeClusters[0].DisplayWidth, Is.EqualTo(2));
        Assert.That(info.Scalars[0].Width, Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.That(info.Scalars[1].Width, Is.EqualTo(UnicodePdfTextWidth.Zero));
    }

    [Test]
    public void LayoutInfo_KeepsCombiningAndEmojiZwjClustersIntact()
    {
        UnicodePdfLayoutInfo info = new UnicodePdfText().GetLayoutInfo(
            "A\u0301\U0001F469\u200D\U0001F4BB");

        Assert.That(info.GraphemeClusters, Has.Length.EqualTo(2));
        AssertCluster(
            info.GraphemeClusters[0],
            0,
            2,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            1);
        AssertCluster(
            info.GraphemeClusters[1],
            2,
            7,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            2);
    }

    [Test]
    public void LayoutInfo_ExposesConcreteWordAndLineBreakMetadata()
    {
        UnicodePdfLayoutInfo words = new UnicodePdfText().GetLayoutInfo("can't stop");
        UnicodePdfLayoutInfo lines = new UnicodePdfText().GetLayoutInfo("A B\r\nC");

        Assert.That(words.WordSegments, Has.Length.EqualTo(3));
        AssertSegment(words.WordSegments[0], 0, 5);
        AssertSegment(words.WordSegments[1], 5, 6);
        AssertSegment(words.WordSegments[2], 6, 10);
        Assert.That(lines.LineBreaks, Has.Length.EqualTo(3));
        AssertLineBreak(lines.LineBreaks[0], 2, UnicodeLineBreakStatus.Optional);
        AssertLineBreak(lines.LineBreaks[1], 5, UnicodeLineBreakStatus.Mandatory);
        AssertLineBreak(lines.LineBreaks[2], 6, UnicodeLineBreakStatus.Mandatory);
    }

    [Test]
    public void LayoutInfo_ReportsExactMixedDirectionRunsAndBracketDirections()
    {
        UnicodePdfLayoutInfo info = new UnicodePdfText().GetLayoutInfo("A (\u05D0\u05D1) 12");

        Assert.That(info.BidiRuns, Has.Length.EqualTo(3));
        AssertBidiRun(info.BidiRuns[0], 0, 3, UnicodePdfBidiDirection.LeftToRight);
        AssertBidiRun(info.BidiRuns[1], 3, 7, UnicodePdfBidiDirection.RightToLeft);
        AssertBidiRun(info.BidiRuns[2], 7, 9, UnicodePdfBidiDirection.LeftToRight);
        Assert.That(info.Scalars[2].CodePoint, Is.EqualTo(0x0028));
        Assert.That(info.Scalars[2].Direction, Is.EqualTo(UnicodePdfBidiDirection.LeftToRight));
        Assert.That(info.Scalars[5].CodePoint, Is.EqualTo(0x0029));
        Assert.That(info.Scalars[5].Direction, Is.EqualTo(UnicodePdfBidiDirection.RightToLeft));
    }

    [Test]
    public void LayoutInfo_HandlesEmptyMalformedAndNullText()
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        UnicodePdfLayoutInfo empty = pdfText.GetLayoutInfo("");
        UnicodePdfScalarLayout[] malformed = pdfText.GetScalarLayout("\uD800");

        Assert.That(empty.GraphemeClusters, Is.Empty);
        Assert.That(empty.WordSegments, Is.Empty);
        Assert.That(ToLineText(empty.LineBreaks), Is.EqualTo(ToLineText(new UnicodeSegments().GetLineBreakOpportunities(""))));
        Assert.That(empty.ScriptRuns, Is.Empty);
        Assert.That(empty.BidiRuns, Is.Empty);
        Assert.That(empty.Scalars, Is.Empty);
        Assert.That(malformed, Has.Length.EqualTo(1));
        Assert.That(malformed[0].CodePoint, Is.EqualTo(UnicodeScalar.ReplacementCharacter));
        Assert.That(malformed[0].Utf16Length, Is.EqualTo(1));
        Assert.Throws<ArgumentNullException>(() => pdfText.GetLayoutInfo(null!));
        Assert.Throws<ArgumentNullException>(() => pdfText.GetScalarLayout(null!));
    }

    static string ToSegmentText(UnicodeSegmentRecord[] segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            builder.Append(segments[index].StartUtf16Index);
            builder.Append("-");
            builder.Append(segments[index].EndUtf16Index);
            builder.Append(";");
            index = index + 1;
        }

        return builder.ToString();
    }

    static string ToLineText(UnicodeLineBreakRecord[] lines)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < lines.Length)
        {
            builder.Append(lines[index].Utf16Index);
            builder.Append(":");
            builder.Append((int)lines[index].Status);
            builder.Append(";");
            index = index + 1;
        }

        return builder.ToString();
    }

    static void AssertSegment(UnicodeSegmentRecord segment, int start, int end)
    {
        Assert.That(segment.StartUtf16Index, Is.EqualTo(start));
        Assert.That(segment.EndUtf16Index, Is.EqualTo(end));
        Assert.That(segment.Utf16Length, Is.EqualTo(end - start));
    }

    static void AssertLineBreak(
        UnicodeLineBreakRecord lineBreak,
        int utf16Index,
        UnicodeLineBreakStatus status)
    {
        Assert.That(lineBreak.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(lineBreak.Status, Is.EqualTo(status));
    }

    static void AssertBidiRun(
        UnicodePdfBidiRun run,
        int start,
        int end,
        UnicodePdfBidiDirection direction)
    {
        Assert.That(run.StartUtf16Index, Is.EqualTo(start));
        Assert.That(run.EndUtf16Index, Is.EqualTo(end));
        Assert.That(run.Utf16Length, Is.EqualTo(end - start));
        Assert.That(run.Direction, Is.EqualTo(direction));
    }

    static void AssertCluster(
        UnicodePdfLayoutCluster cluster,
        int start,
        int end,
        UnicodeScript script,
        UnicodePdfBidiDirection direction,
        int width)
    {
        Assert.That(cluster.StartUtf16Index, Is.EqualTo(start));
        Assert.That(cluster.EndUtf16Index, Is.EqualTo(end));
        Assert.That(cluster.Utf16Length, Is.EqualTo(end - start));
        Assert.That(cluster.Script, Is.EqualTo(script));
        Assert.That(cluster.Direction, Is.EqualTo(direction));
        Assert.That(cluster.DisplayWidth, Is.EqualTo(width));
    }
}
