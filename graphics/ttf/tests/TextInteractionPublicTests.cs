using ttf;

namespace ttf.Tests;

internal class TextInteractionPublicTests
{
    [Test]
    public void ExposesOnlyOriginalGraphemeAndShapingSafeCaretBoundaries()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutResult graphemes = Prepare(
            face,
            "A\U0001F600B",
            new TextLayoutOptions()).Reflow(1000);
        TextLayoutResult ligature = Prepare(
            face,
            "fi",
            new TextLayoutOptions()).Reflow(1000);

        Assert.Multiple(() =>
        {
            Assert.That(CaretUtf16Indexes(graphemes),
                Is.EqualTo(new int[] { 0, 1, 3, 4 }));
            Assert.That(graphemes.IsCaretBoundary(1), Is.True);
            Assert.That(graphemes.IsCaretBoundary(2), Is.False);
            Assert.That(graphemes.IsCaretBoundary(3), Is.True);
            Assert.That(ligature.GetVisualRun(0).GetGlyphCount(), Is.EqualTo(1));
            Assert.That(ligature.IsCaretBoundary(1), Is.False);
            Assert.That(CaretUtf16Indexes(ligature),
                Is.EqualTo(new int[] { 0, 2 }));
            Assert.Throws<ArgumentException>(() =>
                graphemes.GetSelectionRectangles(2, 1));
            Assert.Throws<ArgumentException>(() =>
                ligature.GetSelectionRectangles(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                graphemes.IsCaretBoundary(5));
        });
    }

    [Test]
    public void MapsLogicalBidiCaretsToVisualRunsAndHitResults()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace face = FontClusterFallbackTests.Clone(
            source,
            "Mixed",
            new int[] { 0x0041, 0x05D0, 0x05D1 });
        TextLayoutResult layout = Prepare(
            face,
            "A\u05D0\u05D1",
            new TextLayoutOptions()).Reflow(1000);
        TextCaretStopResult selected = FindMappedCaret(layout, 1);
        TextHitTestResult hit = layout.HitTest(
            selected.GetX(),
            selected.GetY());

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLine(0).GetRunCount(), Is.EqualTo(2));
            Assert.That(selected.HasRunMapping(), Is.True);
            Assert.That(selected.GetVisualRunIndex(), Is.EqualTo(
                layout.GetLine(0).GetVisualRunIndex(
                    selected.GetLogicalRunIndex())));
            Assert.That(hit.GetLineIndex(), Is.EqualTo(selected.GetLineIndex()));
            Assert.That(hit.GetUtf16Index(), Is.EqualTo(selected.GetUtf16Index()));
            Assert.That(hit.GetClusterBoundary(),
                Is.EqualTo(selected.GetClusterBoundary()));
            Assert.That(hit.GetDistance(), Is.Zero.Within(0.000001));
            Assert.That(hit.GetCaretStop().GetVisualRunIndex(),
                Is.EqualTo(selected.GetVisualRunIndex()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.HitTest(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetCaretStop(layout.GetCaretStopCount()));
        });
    }

    [Test]
    public void ReturnsSelectionRectanglesAcrossReflowedAndVerticalLines()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions horizontalOptions = new TextLayoutOptions();
        PreparedTextResult horizontalPrepared = Prepare(
            face,
            "A A",
            horizontalOptions);
        double narrowWidth = horizontalPrepared.Reflow(1000)
            .GetLine(0).GetAdvance() * 0.45;
        TextLayoutResult horizontal = horizontalPrepared.Reflow(narrowWidth);
        TextSelectionRectangleResult[] horizontalSelection =
            horizontal.GetSelectionRectangles(0, 3);

        TextLayoutOptions verticalOptions = new TextLayoutOptions();
        verticalOptions.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        TextLayoutResult vertical = Prepare(
            face,
            "AB",
            verticalOptions).Reflow(1000);
        TextSelectionRectangleResult[] verticalSelection =
            vertical.GetSelectionRectangles(0, 2);
        TextCaretStopResult verticalEnd = FindCaret(vertical, 2);
        TextHitTestResult verticalHit = vertical.HitTest(
            verticalEnd.GetX(),
            verticalEnd.GetY());

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.GetLineCount(), Is.EqualTo(2));
            Assert.That(horizontalSelection.Length, Is.EqualTo(2));
            Assert.That(horizontalSelection[0].GetLineIndex(), Is.Zero);
            Assert.That(horizontalSelection[1].GetLineIndex(), Is.EqualTo(1));
            Assert.That(horizontalSelection[0].GetWidth(), Is.GreaterThan(0));
            Assert.That(horizontalSelection[0].GetHeight(), Is.GreaterThan(0));
            Assert.That(horizontalSelection[0].GetXMaximum(), Is.EqualTo(
                horizontalSelection[0].GetX() +
                horizontalSelection[0].GetWidth()));
            Assert.That(verticalSelection.Length, Is.EqualTo(1));
            Assert.That(verticalSelection[0].GetWidth(), Is.GreaterThan(0));
            Assert.That(verticalSelection[0].GetHeight(), Is.GreaterThan(0));
            Assert.That(verticalSelection[0].GetYMaximum(), Is.EqualTo(
                verticalSelection[0].GetY() +
                verticalSelection[0].GetHeight()));
            Assert.That(verticalHit.GetUtf16Index(), Is.EqualTo(2));
            Assert.That(horizontal.GetSelectionRectangles(1, 0), Is.Empty);
        });
    }

    [Test]
    public void RepresentsTheEmptyLineCaretWithoutARunMapping()
    {
        TextLayoutResult layout = new TextLayoutEngine().Prepare(
            new FontSet(),
            string.Empty,
            new TextLayoutStyle[0],
            new TextLayoutOptions()).Reflow(0);
        TextCaretStopResult stop = layout.GetCaretStop(0);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetCaretStopCount(), Is.EqualTo(1));
            Assert.That(stop.GetLineIndex(), Is.Zero);
            Assert.That(stop.GetClusterBoundary(), Is.Zero);
            Assert.That(stop.GetUtf16Index(), Is.Zero);
            Assert.That(stop.HasRunMapping(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                stop.GetLogicalRunIndex());
            Assert.Throws<InvalidOperationException>(() =>
                stop.GetVisualRunIndex());
        });
    }

    static PreparedTextResult Prepare(
        FontFace face,
        string text,
        TextLayoutOptions options)
    {
        FontSet set = new FontSet();
        set.Add(face);
        FontNameValue? familyName = face.GetNames().GetFamilyName();
        if (familyName == null)
        {
            familyName = face.GetNames().GetTypographicFamilyName();
        }
        string family = familyName!.GetValue();
        return new TextLayoutEngine().Prepare(
            set,
            text,
            new TextLayoutStyle[]
            {
                new TextLayoutStyle(
                    0,
                    text.Length,
                    new FontRequest(family),
                    new TextStyle(12))
            },
            options);
    }

    static int[] CaretUtf16Indexes(TextLayoutResult layout)
    {
        int[] values = new int[layout.GetCaretStopCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = layout.GetCaretStop(index).GetUtf16Index();
            index += 1;
        }
        return values;
    }

    static TextCaretStopResult FindMappedCaret(
        TextLayoutResult layout,
        int utf16Index)
    {
        int index = 0;
        while (index < layout.GetCaretStopCount())
        {
            TextCaretStopResult stop = layout.GetCaretStop(index);
            if (stop.GetUtf16Index() == utf16Index && stop.HasRunMapping())
            {
                return stop;
            }
            index += 1;
        }
        throw new InvalidOperationException("No mapped caret was found.");
    }

    static TextCaretStopResult FindCaret(
        TextLayoutResult layout,
        int utf16Index)
    {
        int index = 0;
        while (index < layout.GetCaretStopCount())
        {
            TextCaretStopResult stop = layout.GetCaretStop(index);
            if (stop.GetUtf16Index() == utf16Index)
            {
                return stop;
            }
            index += 1;
        }
        throw new InvalidOperationException("No caret was found.");
    }
}
