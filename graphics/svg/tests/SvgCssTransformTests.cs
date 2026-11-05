namespace svg.Tests;

public class SvgCssTransformTests
{
    [Test]
    public void CssParserSupportsEveryTwoDimensionalFunctionAndDistinctUnits()
    {
        string input = "MATRIX(1,0,0,1,2,3) translate(10px,25%) translateX(2em) translateY(3vh) scale(2,.5) scaleX(-1) scaleY(3) rotate(.5turn) skew(10deg,.1rad) skewX(100grad) skewY(0)";

        SvgCssTransformParseResult result = SvgCssTransformParser.ParseTransform(input, 20, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.TransformList.Items, Has.Count.EqualTo(11));
            Assert.That(result.TransformList.Items[0].Kind, Is.EqualTo(SvgCssTransformKind.Matrix));
            Assert.That(result.TransformList.Items[1].Lengths[0].Unit, Is.EqualTo(SvgLengthUnit.Px));
            Assert.That(result.TransformList.Items[1].Lengths[1].Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(result.TransformList.Items[2].Lengths[0].Unit, Is.EqualTo(SvgLengthUnit.Em));
            Assert.That(result.TransformList.Items[3].Lengths[0].Unit, Is.EqualTo(SvgLengthUnit.Vh));
            Assert.That(result.TransformList.Items[7].Angles[0].Unit, Is.EqualTo(SvgAngleUnit.Turn));
            Assert.That(result.TransformList.Items[8].Angles[1].Unit, Is.EqualTo(SvgAngleUnit.Rad));
            Assert.That(result.TransformList.Items[9].Angles[0].Unit, Is.EqualTo(SvgAngleUnit.Grad));
            Assert.That(result.TransformList.ToText(), Is.EqualTo("matrix(1, 0, 0, 1, 2, 3) translate(10px, 25%) translateX(2em) translateY(3vh) scale(2, 0.5) scaleX(-1) scaleY(3) rotate(0.5turn) skew(10deg, 0.1rad) skewX(100grad) skewY(0)"));
        });
    }

    [Test]
    public void NoneIsCaseInsensitiveAndMustBeTheCompleteCssValue()
    {
        SvgCssTransformParseResult none = SvgCssTransformParser.ParseTransform(" NoNe ", 5, 16, false);
        SvgCssTransformParseResult trailing = SvgCssTransformParser.ParseTransform("none scale(2)", 5, 16, false);
        SvgCssTransformMatrixResult identity = none.TransformList.ToMatrix(null, null, (SvgTransformBox)999);

        Assert.Multiple(() =>
        {
            Assert.That(none.Success, Is.True);
            Assert.That(none.TransformList.IsNone, Is.True);
            Assert.That(none.TransformList.ToText(), Is.EqualTo("none"));
            Assert.That(identity.Success, Is.True);
            Assert.That(identity.Matrix.A, Is.EqualTo(1));
            Assert.That(identity.Matrix.D, Is.EqualTo(1));
            Assert.That(trailing.Error, Is.EqualTo(SvgCssTransformParseError.TrailingCharacters));
            Assert.That(trailing.ErrorOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void AdjacentCssFunctionsComposeWithoutSvgListSeparators()
    {
        SvgCssTransformParseResult result = SvgCssTransformParser.ParseTransform("translateX(10px)scale(2)", 5, 32, false);
        SvgCssTransformContext context = ContextWithViewBox(0, 0, 100, 100);
        SvgTransformOrigin origin = Origin(0, 0);

        SvgCssTransformMatrixResult matrix = result.TransformList.ToMatrix(context, origin, SvgTransformBox.ViewBox);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransformList.Items, Has.Count.EqualTo(2));
        });
        AssertMatrix(matrix, 2, 0, 0, 2, 10, 0);
    }

    [Test]
    public void LengthAndPercentageTranslationsResolveAgainstExplicitContext()
    {
        SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform("translate(50%,25%) translateX(2em) translateY(10px)", 5, 32, false);
        SvgCssTransformContext context = ContextWithViewBox(10, 20, 200, 100);
        context.LengthContext.HasFontSize = true;
        context.LengthContext.FontSize = 10;

        SvgCssTransformMatrixResult matrix = parsed.TransformList.ToMatrix(context, Origin(0, 0), SvgTransformBox.ViewBox);

        AssertMatrix(matrix, 1, 0, 0, 1, 120, 35);
    }

    [Test]
    public void AbsoluteCssLengthsUseExistingNinetySixDpiEvaluation()
    {
        SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform("translate(1in,2.54cm)", 5, 32, false);

        SvgCssTransformMatrixResult matrix = parsed.TransformList.ToMatrix(ContextWithViewBox(0, 0, 10, 10), Origin(0, 0), SvgTransformBox.ViewBox);

        AssertMatrix(matrix, 1, 0, 0, 1, 96, 96);
    }

    [Test]
    public void AngleUnitsProduceEquivalentRotationMatrices()
    {
        string[] values = { "180deg", "200grad", "3.141592653589793rad", ".5turn" };

        foreach (string value in values)
        {
            SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform("rotate(" + value + ")", 2, 32, false);
            SvgCssTransformMatrixResult matrix = parsed.TransformList.ToMatrix(ContextWithViewBox(0, 0, 10, 10), Origin(0, 0), SvgTransformBox.ViewBox);

            AssertMatrix(matrix, -1, 0, 0, -1, 0, 0);
        }
    }

    [Test]
    public void ScaleRotateAndSkewFunctionsComposeInCssOrder()
    {
        SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform("scaleX(2) scaleY(3) skewX(45deg) skewY(45deg)", 10, 32, false);

        SvgCssTransformMatrixResult matrix = parsed.TransformList.ToMatrix(ContextWithViewBox(0, 0, 10, 10), Origin(0, 0), SvgTransformBox.ViewBox);

        AssertMatrix(matrix, 4, 3, 2, 3, 0, 0);
    }

    [Test]
    public void MatrixAndTwoAngleSkewPreserveAllAffineComponents()
    {
        SvgCssTransformParseResult matrixValue = SvgCssTransformParser.ParseTransform("matrix(1,2,3,4,5,6)", 5, 32, false);
        SvgCssTransformParseResult skewValue = SvgCssTransformParser.ParseTransform("skew(45deg,45deg)", 5, 32, false);
        SvgCssTransformContext context = ContextWithViewBox(0, 0, 10, 10);

        SvgCssTransformMatrixResult matrix = matrixValue.TransformList.ToMatrix(context, Origin(0, 0), SvgTransformBox.ViewBox);
        SvgCssTransformMatrixResult skew = skewValue.TransformList.ToMatrix(context, Origin(0, 0), SvgTransformBox.ViewBox);

        AssertMatrix(matrix, 1, 2, 3, 4, 5, 6);
        AssertMatrix(skew, 1, 1, 1, 1, 0, 0);
    }

    [Test]
    public void TransformOriginKeywordsLengthsAndDepthParseCanonically()
    {
        SvgTransformOriginParseResult one = SvgCssTransformParser.ParseOrigin("bottom", 32);
        SvgTransformOriginParseResult swapped = SvgCssTransformParser.ParseOrigin("top right", 32);
        SvgTransformOriginParseResult centeredFirst = SvgCssTransformParser.ParseOrigin("center left", 32);
        SvgTransformOriginParseResult mixed = SvgCssTransformParser.ParseOrigin("25% 10px", 32);
        SvgTransformOriginParseResult depth = SvgCssTransformParser.ParseOrigin("bottom right 2em", 32);

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(one.Origin.ToText(), Is.EqualTo("50% 100%"));
            Assert.That(swapped.Origin.ToText(), Is.EqualTo("100% 0%"));
            Assert.That(centeredFirst.Origin.ToText(), Is.EqualTo("0% 50%"));
            Assert.That(mixed.Origin.ToText(), Is.EqualTo("25% 10px"));
            Assert.That(depth.Origin.ToText(), Is.EqualTo("100% 100% 2em"));
            Assert.That(depth.Origin.HasZ, Is.True);
        });
    }

    [Test]
    public void OriginWrapsTheComposedMatrixAroundTheSelectedBoxPosition()
    {
        SvgCssTransformParseResult transform = SvgCssTransformParser.ParseTransform("scale(2)", 5, 16, false);
        SvgTransformOriginParseResult origin = SvgCssTransformParser.ParseOrigin("center", 16);
        SvgCssTransformContext context = ContextWithViewBox(10, 20, 200, 100);

        SvgCssTransformMatrixResult matrix = transform.TransformList.ToMatrix(context, origin.Origin, SvgTransformBox.ViewBox);

        AssertMatrix(matrix, 2, 0, 0, 2, -110, -70);
    }

    [TestCase("content-box", SvgTransformBox.ContentBox)]
    [TestCase("BORDER-BOX", SvgTransformBox.BorderBox)]
    [TestCase("fill-box", SvgTransformBox.FillBox)]
    [TestCase("stroke-box", SvgTransformBox.StrokeBox)]
    [TestCase("view-box", SvgTransformBox.ViewBox)]
    public void TransformBoxParsesEveryKeywordCaseInsensitively(string text, SvgTransformBox expected)
    {
        SvgTransformBoxParseResult result = SvgCssTransformParser.ParseTransformBox(" " + text + " ", 16);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransformBox, Is.EqualTo(expected));
            Assert.That(SvgCssTransformRules.BoxText(result.TransformBox), Is.EqualTo(text.ToLowerInvariant()));
        });
    }

    [Test]
    public void SelectedTransformBoxChangesPercentageAndOriginResolution()
    {
        SvgCssTransformParseResult transform = SvgCssTransformParser.ParseTransform("translate(100%,100%) scale(2)", 5, 32, false);
        SvgCssTransformContext context = new SvgCssTransformContext();
        context.FillBox = new SvgCssTransformReferenceBox(true, 10, 20, 100, 50);
        context.ViewBox = new SvgCssTransformReferenceBox(true, 0, 0, 200, 100);
        SvgTransformOrigin center = Origin(50, 50);

        SvgCssTransformMatrixResult fill = transform.TransformList.ToMatrix(context, center, SvgTransformBox.FillBox);
        SvgCssTransformMatrixResult view = transform.TransformList.ToMatrix(context, center, SvgTransformBox.ViewBox);

        AssertMatrix(fill, 2, 0, 0, 2, 40, 5);
        AssertMatrix(view, 2, 0, 0, 2, 100, 50);
    }

    [Test]
    public void CompatibleRecoverySkipsInvalidFunctionsAndRetainsValidOrder()
    {
        string input = "translateX(10px) bogus(1) rotate(10) scale(2)";
        SvgCssTransformParseResult strict = SvgCssTransformParser.ParseTransform(input, 10, 32, false);
        SvgCssTransformParseResult recovered = SvgCssTransformParser.ParseTransform(input, 10, 32, true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Error, Is.EqualTo(SvgCssTransformParseError.UnknownFunction));
            Assert.That(strict.TransformList.Items, Has.Count.EqualTo(1));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.Error, Is.EqualTo(SvgCssTransformParseError.UnknownFunction));
            Assert.That(recovered.ErrorOffset, Is.EqualTo(17));
            Assert.That(recovered.TransformList.Items, Has.Count.EqualTo(2));
            Assert.That(recovered.TransformList.ToText(), Is.EqualTo("translateX(10px) scale(2)"));
        });
    }

    [Test]
    public void StrictTransformFailuresReportExactOffsets()
    {
        SvgCssTransformParseResult empty = SvgCssTransformParser.ParseTransform(" ", 5, 32, false);
        SvgCssTransformParseResult unknown = SvgCssTransformParser.ParseTransform("spin(1)", 5, 32, false);
        SvgCssTransformParseResult missingOpen = SvgCssTransformParser.ParseTransform("translate 10px", 5, 32, false);
        SvgCssTransformParseResult missingClose = SvgCssTransformParser.ParseTransform("translate(10px", 5, 32, false);
        SvgCssTransformParseResult emptyArgs = SvgCssTransformParser.ParseTransform("scale()", 5, 32, false);
        SvgCssTransformParseResult unitlessAngle = SvgCssTransformParser.ParseTransform("rotate(10)", 5, 32, false);
        SvgCssTransformParseResult unitlessLength = SvgCssTransformParser.ParseTransform("translate(10)", 5, 32, false);
        SvgCssTransformParseResult missingComma = SvgCssTransformParser.ParseTransform("translate(10px 20px)", 5, 32, false);
        SvgCssTransformParseResult commaFunctions = SvgCssTransformParser.ParseTransform("scale(2), rotate(1deg)", 5, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgCssTransformParseError.ExpectedValue));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(unknown.Error, Is.EqualTo(SvgCssTransformParseError.UnknownFunction));
            Assert.That(unknown.ErrorOffset, Is.Zero);
            Assert.That(missingOpen.Error, Is.EqualTo(SvgCssTransformParseError.ExpectedOpenParenthesis));
            Assert.That(missingOpen.ErrorOffset, Is.EqualTo(10));
            Assert.That(missingClose.Error, Is.EqualTo(SvgCssTransformParseError.ExpectedCloseParenthesis));
            Assert.That(missingClose.ErrorOffset, Is.EqualTo(14));
            Assert.That(emptyArgs.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentCount));
            Assert.That(emptyArgs.ErrorOffset, Is.EqualTo(6));
            Assert.That(unitlessAngle.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentValue));
            Assert.That(unitlessAngle.ErrorOffset, Is.EqualTo(7));
            Assert.That(unitlessLength.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentValue));
            Assert.That(unitlessLength.ErrorOffset, Is.EqualTo(10));
            Assert.That(missingComma.Error, Is.EqualTo(SvgCssTransformParseError.UnexpectedSeparator));
            Assert.That(missingComma.ErrorOffset, Is.EqualTo(15));
            Assert.That(commaFunctions.Error, Is.EqualTo(SvgCssTransformParseError.UnexpectedSeparator));
            Assert.That(commaFunctions.ErrorOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void ParserBoundsAndOriginBoxFailuresAreStable()
    {
        SvgCssTransformParseResult functionLimit = SvgCssTransformParser.ParseTransform("scale(1) rotate(0)", 1, 32, false);
        SvgCssTransformParseResult nameLimit = SvgCssTransformParser.ParseTransform("translate(0)", 5, 5, false);
        SvgCssTransformParseResult tokenLimit = SvgCssTransformParser.ParseTransform("scale(123456)", 5, 5, false);
        SvgCssTransformParseResult nullTransform = SvgCssTransformParser.ParseTransform(null, 5, 32, false);
        SvgTransformOriginParseResult badOriginPair = SvgCssTransformParser.ParseOrigin("left right", 16);
        SvgTransformOriginParseResult badDepth = SvgCssTransformParser.ParseOrigin("center center 10%", 16);
        SvgTransformOriginParseResult tooManyOrigin = SvgCssTransformParser.ParseOrigin("left top 0 1px", 16);
        SvgTransformBoxParseResult badBox = SvgCssTransformParser.ParseTransformBox("padding-box", 16);
        SvgTransformBoxParseResult longBox = SvgCssTransformParser.ParseTransformBox("content-box", 5);

        Assert.Multiple(() =>
        {
            Assert.That(functionLimit.Error, Is.EqualTo(SvgCssTransformParseError.FunctionLimitExceeded));
            Assert.That(functionLimit.ErrorOffset, Is.EqualTo(9));
            Assert.That(nameLimit.Error, Is.EqualTo(SvgCssTransformParseError.TokenTooLong));
            Assert.That(nameLimit.ErrorOffset, Is.EqualTo(5));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgCssTransformParseError.TokenTooLong));
            Assert.That(tokenLimit.ErrorOffset, Is.EqualTo(11));
            Assert.That(nullTransform.Error, Is.EqualTo(SvgCssTransformParseError.InvalidParserArgument));
            Assert.That(badOriginPair.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentValue));
            Assert.That(badOriginPair.ErrorOffset, Is.EqualTo(5));
            Assert.That(badDepth.ErrorOffset, Is.EqualTo(16));
            Assert.That(tooManyOrigin.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentCount));
            Assert.That(tooManyOrigin.ErrorOffset, Is.EqualTo(11));
            Assert.That(badBox.Error, Is.EqualTo(SvgCssTransformParseError.InvalidArgumentValue));
            Assert.That(longBox.Error, Is.EqualTo(SvgCssTransformParseError.TokenTooLong));
            Assert.That(longBox.ErrorOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void MatrixEvaluationReportsMissingContextInvalidMutationAndOverflow()
    {
        SvgCssTransformParseResult relative = SvgCssTransformParser.ParseTransform("translate(1em)", 5, 32, false);
        SvgCssTransformContext missingBoxContext = new SvgCssTransformContext();
        SvgCssTransformMatrixResult missingBox = relative.TransformList.ToMatrix(missingBoxContext, Origin(0, 0), SvgTransformBox.ViewBox);
        SvgCssTransformContext missingFontContext = ContextWithViewBox(0, 0, 100, 100);
        SvgCssTransformMatrixResult missingFont = relative.TransformList.ToMatrix(missingFontContext, Origin(0, 0), SvgTransformBox.ViewBox);
        SvgCssTransformMatrixResult unknownBox = relative.TransformList.ToMatrix(missingFontContext, Origin(0, 0), (SvgTransformBox)999);
        SvgCssTransformContext invalidBoxContext = ContextWithViewBox(0, 0, 100, 100);
        invalidBoxContext.ViewBox.Width = -1;
        SvgCssTransformMatrixResult invalidBox = relative.TransformList.ToMatrix(invalidBoxContext, Origin(0, 0), SvgTransformBox.ViewBox);

        SvgCssTransformParseResult percentage = SvgCssTransformParser.ParseTransform("translateX(200%)", 5, 32, false);
        SvgCssTransformContext overflowContext = ContextWithViewBox(0, 0, 1, 1);
        overflowContext.ViewBox.Width = double.MaxValue;
        SvgCssTransformMatrixResult overflow = percentage.TransformList.ToMatrix(overflowContext, Origin(0, 0), SvgTransformBox.ViewBox);

        SvgCssTransform invalid = relative.TransformList.Items[0];
        invalid.Lengths[0].Value = double.NaN;
        SvgCssTransformMatrixResult invalidState = relative.TransformList.ToMatrix(missingFontContext, Origin(0, 0), SvgTransformBox.ViewBox);

        Assert.Multiple(() =>
        {
            Assert.That(missingBox.Error, Is.EqualTo(SvgCssTransformError.MissingReferenceBox));
            Assert.That(missingFont.Error, Is.EqualTo(SvgCssTransformError.MissingLengthContext));
            Assert.That(unknownBox.Error, Is.EqualTo(SvgCssTransformError.InvalidArgument));
            Assert.That(invalidBox.Error, Is.EqualTo(SvgCssTransformError.InvalidArgument));
            Assert.That(overflow.Error, Is.EqualTo(SvgCssTransformError.NonFiniteResult));
            Assert.That(invalidState.Error, Is.EqualTo(SvgCssTransformError.InvalidArgument));
        });
    }

    [Test]
    public void ProgrammaticConstructorsValidateAndCopyCollections()
    {
        double[] numbers = { 2, 3 };
        SvgCssTransform transform = new SvgCssTransform(SvgCssTransformKind.Scale, numbers, Array.Empty<SvgLength>(), Array.Empty<SvgAngle>());
        numbers[0] = 9;
        List<SvgCssTransform> items = new() { transform };
        SvgCssTransformList list = new SvgCssTransformList(false, items);
        items.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(transform.Numbers, Is.EqualTo(new[] { 2.0, 3.0 }));
            Assert.That(list.Items, Has.Count.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCssTransform((SvgCssTransformKind)999, new[] { 1.0 }, Array.Empty<SvgLength>(), Array.Empty<SvgAngle>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCssTransform(SvgCssTransformKind.Scale, new[] { double.NaN }, Array.Empty<SvgLength>(), Array.Empty<SvgAngle>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCssTransformList(true, new List<SvgCssTransform> { transform }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransformOrigin(new SvgLength(0, SvgLengthUnit.Px), new SvgLength(0, SvgLengthUnit.Px), new SvgLength(1, SvgLengthUnit.Percentage), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCssTransformReferenceBox(true, 0, 0, -1, 1));
        });
    }

    [Test]
    [NonParallelizable]
    public void CssTransformParsingAndFormattingAreCultureInvariant()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgCssTransformParseResult transform = SvgCssTransformParser.ParseTransform("translate(1.5px,2.5%) rotate(.5turn)", 5, 32, false);
            SvgTransformOriginParseResult origin = SvgCssTransformParser.ParseOrigin("12.5% 2.5px", 32);

            Assert.Multiple(() =>
            {
                Assert.That(transform.Success, Is.True);
                Assert.That(transform.TransformList.ToText(), Is.EqualTo("translate(1.5px, 2.5%) rotate(0.5turn)"));
                Assert.That(origin.Success, Is.True);
                Assert.That(origin.Origin.ToText(), Is.EqualTo("12.5% 2.5px"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    static SvgCssTransformContext ContextWithViewBox(double x, double y, double width, double height)
    {
        SvgCssTransformContext context = new SvgCssTransformContext();
        context.ViewBox = new SvgCssTransformReferenceBox(true, x, y, width, height);
        return context;
    }

    static SvgTransformOrigin Origin(double xPercent, double yPercent)
    {
        return new SvgTransformOrigin(new SvgLength(xPercent, SvgLengthUnit.Percentage), new SvgLength(yPercent, SvgLengthUnit.Percentage), null, false);
    }

    static void AssertMatrix(SvgCssTransformMatrixResult result, double a, double b, double c, double d, double e, double f)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgCssTransformError.None));
            Assert.That(result.Matrix.A, Is.EqualTo(a).Within(1e-11));
            Assert.That(result.Matrix.B, Is.EqualTo(b).Within(1e-11));
            Assert.That(result.Matrix.C, Is.EqualTo(c).Within(1e-11));
            Assert.That(result.Matrix.D, Is.EqualTo(d).Within(1e-11));
            Assert.That(result.Matrix.E, Is.EqualTo(e).Within(1e-11));
            Assert.That(result.Matrix.F, Is.EqualTo(f).Within(1e-11));
        });
    }
}
