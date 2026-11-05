using ttf;

namespace ttf.Tests;

internal class TextLayoutEnginePublicTests
{
    [Test]
    public void PreparesOnceAndReflowsIndependentLayoutsAtMultipleWidths()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontSet set = new FontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        TextStyle textStyle = new TextStyle(12);
        TextLayoutOptions options = new TextLayoutOptions();
        TextLayoutStyle[] styles = new TextLayoutStyle[]
        {
            new TextLayoutStyle(
                0,
                8,
                new FontRequest(family),
                textStyle)
        };
        PreparedTextResult prepared = new TextLayoutEngine().Prepare(
            set,
            "AA AA AA",
            styles,
            options);

        TextLayoutResult wide = prepared.Reflow(1000);
        double naturalAdvance = wide.GetLineAdvance(0);
        TextLayoutResult narrow = prepared.Reflow(naturalAdvance * 0.55);
        TextLayoutResult wideAgain = prepared.Reflow(1000);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.GetSourceText(), Is.EqualTo("AA AA AA"));
            Assert.That(prepared.GetStyleCount(), Is.EqualTo(1));
            Assert.That(prepared.GetClusterCount(), Is.EqualTo(8));
            Assert.That(prepared.GetShapingRunCount(), Is.EqualTo(1));
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(narrow.GetLineCount(), Is.GreaterThan(1));
            Assert.That(wideAgain.GetLineCount(), Is.EqualTo(1));
            Assert.That(wideAgain, Is.Not.SameAs(wide));
            Assert.That(wide.GetLineAdvance(0), Is.EqualTo(naturalAdvance));
            Assert.That(wide.GetSourceText(), Is.EqualTo("AA AA AA"));
            Assert.That(wide.GetMaximumAdvance(), Is.EqualTo(1000));
            Assert.That(wide.GetLineUtf16Start(0), Is.Zero);
            Assert.That(wide.GetLineUtf16Length(0), Is.EqualTo(8));
            Assert.That(wide.IsTruncated(), Is.False);
        });
    }

    [Test]
    public void SnapshotsMultipleStylesAndLayoutOptionsDuringPreparation()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace first = FontClusterFallbackTests.Clone(
            source,
            "First",
            new int[] { 0x0020, 0x0041 });
        FontFace second = FontClusterFallbackTests.Clone(
            source,
            "Second",
            new int[] { 0x0042 });
        FontSet set = new FontSet();
        set.Add(first);
        set.Add(second);
        TextStyle firstTextStyle = new TextStyle(10);
        TextStyle secondTextStyle = new TextStyle(20);
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.End);
        options.SetAllowEmergencyBreaks(true);
        options.SetTabWidth(17);
        TextLayoutStyle[] styles = new TextLayoutStyle[]
        {
            new TextLayoutStyle(
                0,
                3,
                new FontRequest("First"),
                firstTextStyle),
            new TextLayoutStyle(
                3,
                2,
                new FontRequest("Second"),
                secondTextStyle)
        };
        PreparedTextResult prepared = new TextLayoutEngine().Prepare(
            set,
            "AA BB",
            styles,
            options);
        TextLayoutResult beforeMutation = prepared.Reflow(1000);

        firstTextStyle.SetPointSize(100);
        secondTextStyle.SetCharacterSpacing(50);
        options.SetMaximumLines(1);
        options.SetAlignment(TextAlignment.Start);
        options.SetTabWidth(99);
        styles[0] = styles[1];
        TextLayoutResult afterMutation = prepared.Reflow(1000);
        TextLayoutOptions retained = prepared.GetLayoutOptions();
        retained.SetMaximumLines(1);
        retained.SetAlignment(TextAlignment.Center);
        retained.SetTabWidth(88);
        TextLayoutOptions retainedAgain = prepared.GetLayoutOptions();

        Assert.Multiple(() =>
        {
            Assert.That(prepared.GetStyleCount(), Is.EqualTo(2));
            Assert.That(prepared.GetShapingRunCount(), Is.EqualTo(2));
            Assert.That(retainedAgain.GetAlignment(),
                Is.EqualTo(TextAlignment.End));
            Assert.That(retainedAgain.AllowsEmergencyBreaks(), Is.True);
            Assert.That(retainedAgain.GetTabWidth(), Is.EqualTo(17));
            Assert.That(retainedAgain.GetMaximumLines(), Is.Zero);
            Assert.That(beforeMutation.GetLineCount(), Is.EqualTo(1));
            Assert.That(afterMutation.GetLineCount(), Is.EqualTo(1));
            Assert.That(afterMutation.GetLineAdvance(0),
                Is.EqualTo(beforeMutation.GetLineAdvance(0)));
            Assert.That(afterMutation.GetLineUtf16Length(0), Is.EqualTo(5));
        });
    }

    [Test]
    public void PreservesGraphemeBoundariesAndValidatesStyleCoverage()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontSet set = new FontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        FontRequest request = new FontRequest(family);
        TextStyle style = new TextStyle(12);
        TextLayoutEngine engine = new TextLayoutEngine();
        TextLayoutOptions options = new TextLayoutOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => engine.Prepare(
                set,
                "A\u0301B",
                new TextLayoutStyle[]
                {
                    new TextLayoutStyle(0, 1, request, style),
                    new TextLayoutStyle(1, 2, request, style)
                },
                options));
            Assert.Throws<ArgumentException>(() => engine.Prepare(
                set,
                "AB",
                new TextLayoutStyle[]
                {
                    new TextLayoutStyle(0, 1, request, style)
                },
                options));
            Assert.Throws<ArgumentException>(() => engine.Prepare(
                set,
                "AB",
                new TextLayoutStyle[] { null! },
                options));
            Assert.Throws<ArgumentNullException>(() => engine.Prepare(
                null!,
                "AB",
                new TextLayoutStyle[0],
                options));
            Assert.Throws<ArgumentNullException>(() => engine.Prepare(
                set,
                null!,
                new TextLayoutStyle[0],
                options));
            Assert.Throws<ArgumentNullException>(() => engine.Prepare(
                set,
                "AB",
                null!,
                options));
            Assert.Throws<ArgumentNullException>(() => engine.Prepare(
                set,
                "AB",
                new TextLayoutStyle[0],
                null!));
        });
    }

    [Test]
    public void ReflowsEmptyTextAndRejectsInvalidWidths()
    {
        PreparedTextResult prepared = new TextLayoutEngine().Prepare(
            new FontSet(),
            string.Empty,
            new TextLayoutStyle[0],
            new TextLayoutOptions());
        TextLayoutResult layout = prepared.Reflow(0);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.GetClusterCount(), Is.Zero);
            Assert.That(prepared.GetShapingRunCount(), Is.Zero);
            Assert.That(layout.GetLineCount(), Is.EqualTo(1));
            Assert.That(layout.GetLineUtf16Start(0), Is.Zero);
            Assert.That(layout.GetLineUtf16Length(0), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                prepared.Reflow(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                prepared.Reflow(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetLineAdvance(1));
        });
    }
}
