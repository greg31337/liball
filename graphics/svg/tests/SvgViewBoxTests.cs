namespace svg.Tests;

public class SvgViewBoxTests
{
    [TestCase("0 0 100 50", 0.0, 0.0, 100.0, 50.0, "0 0 100 50")]
    [TestCase(" -10.5,20, 30.25 40 ", -10.5, 20.0, 30.25, 40.0, "-10.5 20 30.25 40")]
    [TestCase("1e1 2e1 0 0", 10.0, 20.0, 0.0, 0.0, "10 20 0 0")]
    public void ViewBoxParsesFourFiniteComponentsAndSerializesCanonically(string input, double x, double y, double width, double height, string text)
    {
        SvgViewBoxParseResult result = SvgViewBoxParser.ParseViewBox(input, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgViewBoxParseError.None));
            Assert.That(result.NumberError, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.ViewBox.X, Is.EqualTo(x));
            Assert.That(result.ViewBox.Y, Is.EqualTo(y));
            Assert.That(result.ViewBox.Width, Is.EqualTo(width));
            Assert.That(result.ViewBox.Height, Is.EqualTo(height));
            Assert.That(result.ViewBox.ToText(), Is.EqualTo(text));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [TestCase("none", SvgAspectRatioAlignment.None)]
    [TestCase("xMinYMin", SvgAspectRatioAlignment.XMinYMin)]
    [TestCase("xMidYMin", SvgAspectRatioAlignment.XMidYMin)]
    [TestCase("xMaxYMin", SvgAspectRatioAlignment.XMaxYMin)]
    [TestCase("xMinYMid", SvgAspectRatioAlignment.XMinYMid)]
    [TestCase("xMidYMid", SvgAspectRatioAlignment.XMidYMid)]
    [TestCase("xMaxYMid", SvgAspectRatioAlignment.XMaxYMid)]
    [TestCase("xMinYMax", SvgAspectRatioAlignment.XMinYMax)]
    [TestCase("xMidYMax", SvgAspectRatioAlignment.XMidYMax)]
    [TestCase("xMaxYMax", SvgAspectRatioAlignment.XMaxYMax)]
    public void AspectRatioParsesEveryAlignmentAndDefaultsToMeet(string input, SvgAspectRatioAlignment alignment)
    {
        SvgAspectRatioParseResult result = SvgViewBoxParser.ParseAspectRatio(input, 16);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AspectRatio.Alignment, Is.EqualTo(alignment));
            Assert.That(result.AspectRatio.Scale, Is.EqualTo(SvgAspectRatioScale.Meet));
            Assert.That(result.AspectRatio.Defer, Is.False);
            Assert.That(result.AspectRatio.ToText(), Is.EqualTo(input + " meet"));
        });
    }

    [Test]
    public void AspectRatioPreservesDeferAndExplicitScaleModes()
    {
        SvgAspectRatioParseResult deferred = SvgViewBoxParser.ParseAspectRatio("  defer xMaxYMin slice  ", 16);
        SvgAspectRatioParseResult explicitMeet = SvgViewBoxParser.ParseAspectRatio("xMinYMax meet", 16);
        SvgAspectRatioParseResult noneSlice = SvgViewBoxParser.ParseAspectRatio("none slice", 16);

        Assert.Multiple(() =>
        {
            Assert.That(deferred.Success, Is.True);
            Assert.That(deferred.AspectRatio.Defer, Is.True);
            Assert.That(deferred.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMaxYMin));
            Assert.That(deferred.AspectRatio.Scale, Is.EqualTo(SvgAspectRatioScale.Slice));
            Assert.That(deferred.AspectRatio.ToText(), Is.EqualTo("defer xMaxYMin slice"));
            Assert.That(explicitMeet.AspectRatio.ToText(), Is.EqualTo("xMinYMax meet"));
            Assert.That(noneSlice.AspectRatio.ToText(), Is.EqualTo("none slice"));
        });
    }

    [Test]
    public void NoneUsesIndependentHorizontalAndVerticalScaling()
    {
        SvgViewBox viewBox = new SvgViewBox(10, 20, 100, 100);
        SvgAspectRatio aspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.None, SvgAspectRatioScale.Meet, false);

        SvgViewBoxFitResult result = aspectRatio.Fit(viewBox, 5, 7, 200, 100);

        AssertMatrix(result, 2, 0, 0, 1, -15, -13);
    }

    [Test]
    public void MeetAndSliceChooseTheCorrectUniformScale()
    {
        SvgViewBox viewBox = new SvgViewBox(0, 0, 100, 100);
        SvgAspectRatio meet = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        SvgAspectRatio slice = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Slice, false);

        SvgViewBoxFitResult meetResult = meet.Fit(viewBox, 0, 0, 200, 100);
        SvgViewBoxFitResult sliceResult = slice.Fit(viewBox, 0, 0, 200, 100);

        AssertMatrix(meetResult, 1, 0, 0, 1, 50, 0);
        AssertMatrix(sliceResult, 2, 0, 0, 2, 0, -50);
    }

    [TestCase(SvgAspectRatioAlignment.XMinYMin, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMin, 60.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMin, 110.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMinYMid, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMid, 60.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMid, 110.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMinYMax, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMax, 60.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMax, 110.0, 20.0)]
    public void MeetHonorsEveryHorizontalAlignment(SvgAspectRatioAlignment alignment, double expectedX, double expectedY)
    {
        SvgAspectRatio aspectRatio = new SvgAspectRatio(alignment, SvgAspectRatioScale.Meet, false);

        SvgViewBoxFitResult result = aspectRatio.Fit(new SvgViewBox(0, 0, 100, 100), 10, 20, 300, 200);

        AssertMatrix(result, 2, 0, 0, 2, expectedX, expectedY);
    }

    [TestCase(SvgAspectRatioAlignment.XMinYMin, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMin, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMin, 10.0, 20.0)]
    [TestCase(SvgAspectRatioAlignment.XMinYMid, 10.0, 70.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMid, 10.0, 70.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMid, 10.0, 70.0)]
    [TestCase(SvgAspectRatioAlignment.XMinYMax, 10.0, 120.0)]
    [TestCase(SvgAspectRatioAlignment.XMidYMax, 10.0, 120.0)]
    [TestCase(SvgAspectRatioAlignment.XMaxYMax, 10.0, 120.0)]
    public void MeetHonorsEveryVerticalAlignment(SvgAspectRatioAlignment alignment, double expectedX, double expectedY)
    {
        SvgAspectRatio aspectRatio = new SvgAspectRatio(alignment, SvgAspectRatioScale.Meet, false);

        SvgViewBoxFitResult result = aspectRatio.Fit(new SvgViewBox(0, 0, 100, 100), 10, 20, 200, 300);

        AssertMatrix(result, 2, 0, 0, 2, expectedX, expectedY);
    }

    [Test]
    public void FittingAccountsForViewBoxOriginsAndViewportOrigins()
    {
        SvgAspectRatio aspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMaxYMax, SvgAspectRatioScale.Meet, true);

        SvgViewBoxFitResult result = aspectRatio.Fit(new SvgViewBox(10, 20, 100, 50), 5, 7, 200, 200);

        AssertMatrix(result, 2, 0, 0, 2, -15, 67);
    }

    [Test]
    public void FitReportsDegenerateInvalidAndOverflowStatesWithoutThrowing()
    {
        SvgAspectRatio aspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        SvgViewBoxFitResult zeroWidth = aspectRatio.Fit(new SvgViewBox(0, 0, 0, 10), 0, 0, 100, 100);
        SvgViewBoxFitResult negativeViewport = aspectRatio.Fit(new SvgViewBox(0, 0, 10, 10), 0, 0, -1, 100);
        SvgViewBoxFitResult nonFiniteViewport = aspectRatio.Fit(new SvgViewBox(0, 0, 10, 10), double.NaN, 0, 100, 100);
        SvgAspectRatio slice = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Slice, false);
        SvgViewBoxFitResult overflow = slice.Fit(new SvgViewBox(0, 0, double.Epsilon, 1), 0, 0, double.MaxValue, 1);

        SvgViewBox mutatedViewBox = new SvgViewBox(0, 0, 10, 10);
        mutatedViewBox.Width = -1;
        SvgViewBoxFitResult invalidViewBox = aspectRatio.Fit(mutatedViewBox, 0, 0, 100, 100);
        SvgAspectRatio mutatedAspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        mutatedAspectRatio.Alignment = (SvgAspectRatioAlignment)999;
        SvgViewBoxFitResult invalidAspectRatio = mutatedAspectRatio.Fit(new SvgViewBox(0, 0, 10, 10), 0, 0, 100, 100);

        Assert.Multiple(() =>
        {
            Assert.That(zeroWidth.Error, Is.EqualTo(SvgViewBoxFitError.DegenerateViewBox));
            Assert.That(negativeViewport.Error, Is.EqualTo(SvgViewBoxFitError.InvalidArgument));
            Assert.That(nonFiniteViewport.Error, Is.EqualTo(SvgViewBoxFitError.InvalidArgument));
            Assert.That(overflow.Error, Is.EqualTo(SvgViewBoxFitError.NonFiniteResult));
            Assert.That(invalidViewBox.Error, Is.EqualTo(SvgViewBoxFitError.InvalidArgument));
            Assert.That(invalidAspectRatio.Error, Is.EqualTo(SvgViewBoxFitError.InvalidArgument));
        });
    }

    [Test]
    public void ViewBoxParsingReportsMalformedCountsNumbersAndDimensionsAtExactOffsets()
    {
        SvgViewBoxParseResult empty = SvgViewBoxParser.ParseViewBox(" ", 32);
        SvgViewBoxParseResult missing = SvgViewBoxParser.ParseViewBox("0 0 10", 32);
        SvgViewBoxParseResult repeatedComma = SvgViewBoxParser.ParseViewBox("0,,0 10 20", 32);
        SvgViewBoxParseResult excess = SvgViewBoxParser.ParseViewBox("0 0 10 20 30", 32);
        SvgViewBoxParseResult invalidExponent = SvgViewBoxParser.ParseViewBox("0 0 1e+ 2", 32);
        SvgViewBoxParseResult overflow = SvgViewBoxParser.ParseViewBox("0 0 1e309 2", 32);
        SvgViewBoxParseResult negativeWidth = SvgViewBoxParser.ParseViewBox("0 0 -1 2", 32);
        SvgViewBoxParseResult negativeHeight = SvgViewBoxParser.ParseViewBox("0 0 1 -2", 32);
        SvgViewBoxParseResult longToken = SvgViewBoxParser.ParseViewBox("123 0 1 1", 2);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgViewBoxParseError.ExpectedValue));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(missing.Error, Is.EqualTo(SvgViewBoxParseError.ComponentCount));
            Assert.That(missing.ErrorOffset, Is.EqualTo(6));
            Assert.That(repeatedComma.Error, Is.EqualTo(SvgViewBoxParseError.ExpectedValue));
            Assert.That(repeatedComma.ErrorOffset, Is.EqualTo(2));
            Assert.That(excess.Error, Is.EqualTo(SvgViewBoxParseError.ComponentCount));
            Assert.That(excess.ErrorOffset, Is.EqualTo(10));
            Assert.That(invalidExponent.Error, Is.EqualTo(SvgViewBoxParseError.InvalidNumber));
            Assert.That(invalidExponent.NumberError, Is.EqualTo(SvgNumberParseError.InvalidExponent));
            Assert.That(invalidExponent.ErrorOffset, Is.EqualTo(7));
            Assert.That(overflow.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(negativeWidth.Error, Is.EqualTo(SvgViewBoxParseError.NegativeDimension));
            Assert.That(negativeWidth.ErrorOffset, Is.EqualTo(4));
            Assert.That(negativeHeight.ErrorOffset, Is.EqualTo(6));
            Assert.That(longToken.NumberError, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(longToken.ErrorOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void AspectRatioParsingReportsKeywordAndBoundaryFailuresAtExactOffsets()
    {
        SvgAspectRatioParseResult empty = SvgViewBoxParser.ParseAspectRatio(" ", 16);
        SvgAspectRatioParseResult missingAfterDefer = SvgViewBoxParser.ParseAspectRatio("defer", 16);
        SvgAspectRatioParseResult gluedDefer = SvgViewBoxParser.ParseAspectRatio("deferxMidYMid", 16);
        SvgAspectRatioParseResult unknownAlignment = SvgViewBoxParser.ParseAspectRatio("center meet", 16);
        SvgAspectRatioParseResult wrongCase = SvgViewBoxParser.ParseAspectRatio("xmidymid meet", 16);
        SvgAspectRatioParseResult unknownScale = SvgViewBoxParser.ParseAspectRatio("xMidYMid crop", 16);
        SvgAspectRatioParseResult comma = SvgViewBoxParser.ParseAspectRatio("xMidYMid,meet", 16);
        SvgAspectRatioParseResult excess = SvgViewBoxParser.ParseAspectRatio("xMidYMid meet extra", 16);
        SvgAspectRatioParseResult longToken = SvgViewBoxParser.ParseAspectRatio("xMidYMiddle", 8);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgViewBoxParseError.ExpectedValue));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(missingAfterDefer.Error, Is.EqualTo(SvgViewBoxParseError.ExpectedValue));
            Assert.That(missingAfterDefer.ErrorOffset, Is.EqualTo(5));
            Assert.That(gluedDefer.Error, Is.EqualTo(SvgViewBoxParseError.UnknownAlignment));
            Assert.That(gluedDefer.ErrorOffset, Is.Zero);
            Assert.That(unknownAlignment.Error, Is.EqualTo(SvgViewBoxParseError.UnknownAlignment));
            Assert.That(wrongCase.Error, Is.EqualTo(SvgViewBoxParseError.UnknownAlignment));
            Assert.That(unknownScale.Error, Is.EqualTo(SvgViewBoxParseError.UnknownScale));
            Assert.That(unknownScale.ErrorOffset, Is.EqualTo(9));
            Assert.That(comma.Error, Is.EqualTo(SvgViewBoxParseError.TrailingCharacters));
            Assert.That(comma.ErrorOffset, Is.EqualTo(8));
            Assert.That(excess.Error, Is.EqualTo(SvgViewBoxParseError.TrailingCharacters));
            Assert.That(excess.ErrorOffset, Is.EqualTo(14));
            Assert.That(longToken.Error, Is.EqualTo(SvgViewBoxParseError.TokenTooLong));
            Assert.That(longToken.ErrorOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void ConstructorsRejectInvalidValuesAndEnums()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgViewBox(double.NaN, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgViewBox(0, 0, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgAspectRatio((SvgAspectRatioAlignment)999, SvgAspectRatioScale.Meet, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgAspectRatio(SvgAspectRatioAlignment.None, (SvgAspectRatioScale)999, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgMatrix(1, 0, 0, 1, double.PositiveInfinity, 0));
        });
    }

    [Test]
    public void ParserArgumentsAreValidatedWithoutThrowing()
    {
        SvgViewBoxParseResult nullViewBox = SvgViewBoxParser.ParseViewBox(null, 32);
        SvgViewBoxParseResult invalidViewBoxLimit = SvgViewBoxParser.ParseViewBox("0 0 1 1", 0);
        SvgAspectRatioParseResult nullAspectRatio = SvgViewBoxParser.ParseAspectRatio(null, 16);
        SvgAspectRatioParseResult invalidAspectRatioLimit = SvgViewBoxParser.ParseAspectRatio("none", 0);

        Assert.Multiple(() =>
        {
            Assert.That(nullViewBox.Error, Is.EqualTo(SvgViewBoxParseError.InvalidArgument));
            Assert.That(invalidViewBoxLimit.Error, Is.EqualTo(SvgViewBoxParseError.InvalidArgument));
            Assert.That(nullAspectRatio.Error, Is.EqualTo(SvgViewBoxParseError.InvalidArgument));
            Assert.That(invalidAspectRatioLimit.Error, Is.EqualTo(SvgViewBoxParseError.InvalidArgument));
        });
    }

    [Test]
    [NonParallelizable]
    public void ParsingAndSerializationAreCultureInvariant()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgViewBoxParseResult viewBox = SvgViewBoxParser.ParseViewBox("1.5 2.5 3.5 4.5", 16);
            SvgAspectRatioParseResult aspectRatio = SvgViewBoxParser.ParseAspectRatio("defer xMidYMid slice", 16);

            Assert.Multiple(() =>
            {
                Assert.That(viewBox.Success, Is.True);
                Assert.That(viewBox.ViewBox.ToText(), Is.EqualTo("1.5 2.5 3.5 4.5"));
                Assert.That(aspectRatio.Success, Is.True);
                Assert.That(aspectRatio.AspectRatio.ToText(), Is.EqualTo("defer xMidYMid slice"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    static void AssertMatrix(SvgViewBoxFitResult result, double a, double b, double c, double d, double e, double f)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgViewBoxFitError.None));
            Assert.That(result.Matrix.A, Is.EqualTo(a).Within(1e-12));
            Assert.That(result.Matrix.B, Is.EqualTo(b).Within(1e-12));
            Assert.That(result.Matrix.C, Is.EqualTo(c).Within(1e-12));
            Assert.That(result.Matrix.D, Is.EqualTo(d).Within(1e-12));
            Assert.That(result.Matrix.E, Is.EqualTo(e).Within(1e-12));
            Assert.That(result.Matrix.F, Is.EqualTo(f).Within(1e-12));
        });
    }
}
