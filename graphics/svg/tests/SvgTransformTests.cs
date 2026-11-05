namespace svg.Tests;

public class SvgTransformTests
{
    [TestCase("matrix(1, 2,3 4,5,6)", SvgTransformKind.Matrix, 6, "matrix(1 2 3 4 5 6)")]
    [TestCase("translate(10)", SvgTransformKind.Translate, 1, "translate(10)")]
    [TestCase("translate(10,-20)", SvgTransformKind.Translate, 2, "translate(10 -20)")]
    [TestCase("translate(10-20)", SvgTransformKind.Translate, 2, "translate(10 -20)")]
    [TestCase("scale(2)", SvgTransformKind.Scale, 1, "scale(2)")]
    [TestCase("scale(2 .5)", SvgTransformKind.Scale, 2, "scale(2 0.5)")]
    [TestCase("rotate(45)", SvgTransformKind.Rotate, 1, "rotate(45)")]
    [TestCase("rotate(45,10,20)", SvgTransformKind.Rotate, 3, "rotate(45 10 20)")]
    [TestCase("skewX(-15)", SvgTransformKind.SkewX, 1, "skewX(-15)")]
    [TestCase("skewY(1e1)", SvgTransformKind.SkewY, 1, "skewY(10)")]
    public void EverySvgTransformParsesWithValidAritiesAndCanonicalFormatting(string input, SvgTransformKind kind, int count, string expectedText)
    {
        SvgTransformParseResult result = SvgTransformParser.Parse(input, 10, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgTransformParseError.None));
            Assert.That(result.NumberError, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.TransformList.Items, Has.Count.EqualTo(1));
            Assert.That(result.TransformList.Items[0].Kind, Is.EqualTo(kind));
            Assert.That(result.TransformList.Items[0].Values, Has.Length.EqualTo(count));
            Assert.That(result.TransformList.ToText(), Is.EqualTo(expectedText));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void TransformListsAcceptCommaOrWhitespaceBetweenFunctions()
    {
        SvgTransformParseResult result = SvgTransformParser.Parse(" translate(10,20),scale(2) rotate(30 5 6) ", 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransformList.Items, Has.Count.EqualTo(3));
            Assert.That(result.TransformList.Items[0].Kind, Is.EqualTo(SvgTransformKind.Translate));
            Assert.That(result.TransformList.Items[1].Kind, Is.EqualTo(SvgTransformKind.Scale));
            Assert.That(result.TransformList.Items[2].Kind, Is.EqualTo(SvgTransformKind.Rotate));
            Assert.That(result.TransformList.ToText(), Is.EqualTo("translate(10 20) scale(2) rotate(30 5 6)"));
        });
    }

    [Test]
    public void EmptySvgTransformListParsesFormatsAndBuildsTheIdentityMatrix()
    {
        SvgTransformParseResult result = SvgTransformParser.Parse("    ", 5, 16);
        SvgTransformMatrixResult matrix = result.TransformList.ToMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransformList.Items, Is.Empty);
            Assert.That(result.TransformList.ToText(), Is.Empty);
        });
        AssertMatrix(matrix, 1, 0, 0, 1, 0, 0);
    }

    [Test]
    public void MatrixTranslateAndScaleConstructExpectedMatrices()
    {
        SvgTransformMatrixResult matrix = Transform(SvgTransformKind.Matrix, 1, 2, 3, 4, 5, 6).ToMatrix();
        SvgTransformMatrixResult translateOne = Transform(SvgTransformKind.Translate, 10).ToMatrix();
        SvgTransformMatrixResult translateTwo = Transform(SvgTransformKind.Translate, 10, -20).ToMatrix();
        SvgTransformMatrixResult scaleOne = Transform(SvgTransformKind.Scale, 2).ToMatrix();
        SvgTransformMatrixResult scaleTwo = Transform(SvgTransformKind.Scale, 2, 3).ToMatrix();

        AssertMatrix(matrix, 1, 2, 3, 4, 5, 6);
        AssertMatrix(translateOne, 1, 0, 0, 1, 10, 0);
        AssertMatrix(translateTwo, 1, 0, 0, 1, 10, -20);
        AssertMatrix(scaleOne, 2, 0, 0, 2, 0, 0);
        AssertMatrix(scaleTwo, 2, 0, 0, 3, 0, 0);
    }

    [Test]
    public void RotateAndSkewConstructExpectedMatrices()
    {
        SvgTransformMatrixResult rotate = Transform(SvgTransformKind.Rotate, 90).ToMatrix();
        SvgTransformMatrixResult centered = Transform(SvgTransformKind.Rotate, 90, 10, 20).ToMatrix();
        SvgTransformMatrixResult skewX = Transform(SvgTransformKind.SkewX, 45).ToMatrix();
        SvgTransformMatrixResult skewY = Transform(SvgTransformKind.SkewY, 45).ToMatrix();

        AssertMatrix(rotate, 0, 1, -1, 0, 0, 0);
        AssertMatrix(centered, 0, 1, -1, 0, 30, 10);
        AssertMatrix(skewX, 1, 0, 1, 1, 0, 0);
        AssertMatrix(skewY, 1, 1, 0, 1, 0, 0);
    }

    [Test]
    public void TransformListCompositionUsesSvgMatrixOrder()
    {
        SvgTransformParseResult translateThenScale = SvgTransformParser.Parse("translate(10 20) scale(2)", 5, 32);
        SvgTransformParseResult scaleThenTranslate = SvgTransformParser.Parse("scale(2) translate(10 20)", 5, 32);

        SvgTransformMatrixResult first = translateThenScale.TransformList.ToMatrix();
        SvgTransformMatrixResult second = scaleThenTranslate.TransformList.ToMatrix();

        AssertMatrix(first, 2, 0, 0, 2, 10, 20);
        AssertMatrix(second, 2, 0, 0, 2, 20, 40);
    }

    [Test]
    public void CanonicalTransformTextParsesBackToTheSameMatrix()
    {
        SvgTransformParseResult original = SvgTransformParser.Parse("matrix(1 0.25 -0.5 1 3 4), rotate(30 5 6) skewX(10)", 10, 32);
        string canonical = original.TransformList.ToText();
        SvgTransformParseResult reparsed = SvgTransformParser.Parse(canonical, 10, 32);

        Assert.Multiple(() =>
        {
            Assert.That(original.Success, Is.True);
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.TransformList.ToText(), Is.EqualTo(canonical));
        });
        AssertEquivalentMatrices(original.TransformList.ToMatrix(), reparsed.TransformList.ToMatrix());
    }

    [Test]
    public void FunctionNamesAreCaseSensitiveAndRequireParentheses()
    {
        SvgTransformParseResult wrongCase = SvgTransformParser.Parse("Translate(1)", 5, 32);
        SvgTransformParseResult unknown = SvgTransformParser.Parse("reflect(1)", 5, 32);
        SvgTransformParseResult missingOpen = SvgTransformParser.Parse("translate 1)", 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(wrongCase.Error, Is.EqualTo(SvgTransformParseError.UnknownTransform));
            Assert.That(wrongCase.ErrorOffset, Is.Zero);
            Assert.That(unknown.Error, Is.EqualTo(SvgTransformParseError.UnknownTransform));
            Assert.That(unknown.ErrorOffset, Is.Zero);
            Assert.That(missingOpen.Error, Is.EqualTo(SvgTransformParseError.ExpectedOpenParenthesis));
            Assert.That(missingOpen.ErrorOffset, Is.EqualTo(10));
        });
    }

    [Test]
    public void ParameterSyntaxFailuresReportExactOffsets()
    {
        SvgTransformParseResult empty = SvgTransformParser.Parse("scale()", 5, 32);
        SvgTransformParseResult invalidExponent = SvgTransformParser.Parse("scale(1e+)", 5, 32);
        SvgTransformParseResult overflow = SvgTransformParser.Parse("scale(1e309)", 5, 32);
        SvgTransformParseResult invalidText = SvgTransformParser.Parse("translate(1x)", 5, 32);
        SvgTransformParseResult trailingComma = SvgTransformParser.Parse("translate(1,)", 5, 32);
        SvgTransformParseResult missingClose = SvgTransformParser.Parse("translate(1", 5, 32);
        SvgTransformParseResult rotateTwo = SvgTransformParser.Parse("rotate(1 2)", 5, 32);
        SvgTransformParseResult skewTwo = SvgTransformParser.Parse("skewX(1 2)", 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgTransformParseError.InvalidParameterCount));
            Assert.That(empty.ErrorOffset, Is.EqualTo(6));
            Assert.That(invalidExponent.Error, Is.EqualTo(SvgTransformParseError.InvalidNumber));
            Assert.That(invalidExponent.NumberError, Is.EqualTo(SvgNumberParseError.InvalidExponent));
            Assert.That(invalidExponent.ErrorOffset, Is.EqualTo(9));
            Assert.That(overflow.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(10));
            Assert.That(invalidText.Error, Is.EqualTo(SvgTransformParseError.ExpectedParameter));
            Assert.That(invalidText.ErrorOffset, Is.EqualTo(11));
            Assert.That(trailingComma.Error, Is.EqualTo(SvgTransformParseError.ExpectedParameter));
            Assert.That(trailingComma.ErrorOffset, Is.EqualTo(12));
            Assert.That(missingClose.Error, Is.EqualTo(SvgTransformParseError.ExpectedCloseParenthesis));
            Assert.That(missingClose.ErrorOffset, Is.EqualTo(11));
            Assert.That(rotateTwo.Error, Is.EqualTo(SvgTransformParseError.InvalidParameterCount));
            Assert.That(rotateTwo.ErrorOffset, Is.EqualTo(10));
            Assert.That(skewTwo.Error, Is.EqualTo(SvgTransformParseError.InvalidParameterCount));
            Assert.That(skewTwo.ErrorOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void ListSyntaxFailuresRetainTheValidPrefix()
    {
        SvgTransformParseResult empty = SvgTransformParser.Parse(" ", 5, 32);
        SvgTransformParseResult missingSeparator = SvgTransformParser.Parse("translate(1)scale(2)", 5, 32);
        SvgTransformParseResult trailingComma = SvgTransformParser.Parse("scale(1),", 5, 32);
        SvgTransformParseResult none = SvgTransformParser.Parse("none", 5, 32);
        SvgTransformParseResult badSecond = SvgTransformParser.Parse("scale(2) rotate()", 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.TransformList.Items, Is.Empty);
            Assert.That(missingSeparator.Error, Is.EqualTo(SvgTransformParseError.MissingSeparator));
            Assert.That(missingSeparator.ErrorOffset, Is.EqualTo(12));
            Assert.That(missingSeparator.TransformList.Items, Has.Count.EqualTo(1));
            Assert.That(trailingComma.Error, Is.EqualTo(SvgTransformParseError.ExpectedTransform));
            Assert.That(trailingComma.ErrorOffset, Is.EqualTo(9));
            Assert.That(none.Error, Is.EqualTo(SvgTransformParseError.UnknownTransform));
            Assert.That(none.ErrorOffset, Is.Zero);
            Assert.That(badSecond.TransformList.Items, Has.Count.EqualTo(1));
            Assert.That(badSecond.TransformList.Items[0].Kind, Is.EqualTo(SvgTransformKind.Scale));
        });
    }

    [Test]
    public void ParserHonorsTransformAndTokenBounds()
    {
        SvgTransformParseResult transformLimit = SvgTransformParser.Parse("scale(1) translate(2)", 1, 32);
        SvgTransformParseResult nameLimit = SvgTransformParser.Parse("translate(1)", 5, 5);
        SvgTransformParseResult numberLimit = SvgTransformParser.Parse("scale(123456)", 5, 5);
        SvgTransformParseResult nullInput = SvgTransformParser.Parse(null, 5, 32);
        SvgTransformParseResult invalidTransformLimit = SvgTransformParser.Parse("scale(1)", 0, 32);
        SvgTransformParseResult invalidTokenLimit = SvgTransformParser.Parse("scale(1)", 5, 0);

        Assert.Multiple(() =>
        {
            Assert.That(transformLimit.Error, Is.EqualTo(SvgTransformParseError.TransformLimitExceeded));
            Assert.That(transformLimit.ErrorOffset, Is.EqualTo(9));
            Assert.That(transformLimit.TransformList.Items, Has.Count.EqualTo(1));
            Assert.That(nameLimit.Error, Is.EqualTo(SvgTransformParseError.TokenTooLong));
            Assert.That(nameLimit.ErrorOffset, Is.EqualTo(5));
            Assert.That(numberLimit.Error, Is.EqualTo(SvgTransformParseError.InvalidNumber));
            Assert.That(numberLimit.NumberError, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(numberLimit.ErrorOffset, Is.EqualTo(11));
            Assert.That(nullInput.Error, Is.EqualTo(SvgTransformParseError.InvalidArgument));
            Assert.That(invalidTransformLimit.Error, Is.EqualTo(SvgTransformParseError.InvalidArgument));
            Assert.That(invalidTokenLimit.Error, Is.EqualTo(SvgTransformParseError.InvalidArgument));
        });
    }

    [Test]
    public void ConstructorsValidateAndCopyProgrammaticValues()
    {
        double[] values = { 2, 3 };
        SvgTransform transform = new SvgTransform(SvgTransformKind.Scale, values);
        values[0] = 99;
        List<SvgTransform> items = new() { transform };
        SvgTransformList list = new SvgTransformList(items);
        items.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(transform.Values, Is.EqualTo(new[] { 2.0, 3.0 }));
            Assert.That(list.Items, Has.Count.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransform((SvgTransformKind)999, new[] { 1.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransform(SvgTransformKind.Matrix, new[] { 1.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransform(SvgTransformKind.Rotate, new[] { 1.0, 2.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransform(SvgTransformKind.Scale, new[] { double.NaN }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransformList(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTransformList(new List<SvgTransform> { null }));
        });
    }

    [Test]
    public void MatrixConstructionRejectsMutatedAndOverflowingState()
    {
        SvgTransform badKind = Transform(SvgTransformKind.Scale, 2);
        badKind.Kind = (SvgTransformKind)999;
        SvgTransform badValues = Transform(SvgTransformKind.Translate, 1);
        badValues.Values[0] = double.NaN;
        SvgTransform overflowRotate = Transform(SvgTransformKind.Rotate, -45, double.MaxValue, double.MaxValue);
        SvgTransformList overflowList = List(Transform(SvgTransformKind.Matrix, double.MaxValue, 0, 0, 1, 0, 0), Transform(SvgTransformKind.Scale, 2));
        SvgTransformList invalidList = List(Transform(SvgTransformKind.Scale, 2));
        invalidList.Items = null;

        Assert.Multiple(() =>
        {
            Assert.That(badKind.ToMatrix().Error, Is.EqualTo(SvgTransformMatrixError.InvalidArgument));
            Assert.That(badKind.ToText(), Is.Empty);
            Assert.That(badValues.ToMatrix().Error, Is.EqualTo(SvgTransformMatrixError.InvalidArgument));
            Assert.That(badValues.ToText(), Is.Empty);
            Assert.That(overflowRotate.ToMatrix().Error, Is.EqualTo(SvgTransformMatrixError.NonFiniteResult));
            Assert.That(overflowList.ToMatrix().Error, Is.EqualTo(SvgTransformMatrixError.NonFiniteResult));
            Assert.That(invalidList.ToMatrix().Error, Is.EqualTo(SvgTransformMatrixError.InvalidArgument));
            Assert.That(invalidList.ToText(), Is.Empty);
        });
    }

    [Test]
    [NonParallelizable]
    public void TransformParsingAndFormattingAreCultureInvariant()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgTransformParseResult result = SvgTransformParser.Parse("translate(1.5 2.5) scale(.5)", 5, 16);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.TransformList.ToText(), Is.EqualTo("translate(1.5 2.5) scale(0.5)"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    static SvgTransform Transform(SvgTransformKind kind, params double[] values)
    {
        return new SvgTransform(kind, values);
    }

    static SvgTransformList List(params SvgTransform[] transforms)
    {
        return new SvgTransformList(new List<SvgTransform>(transforms));
    }

    static void AssertEquivalentMatrices(SvgTransformMatrixResult left, SvgTransformMatrixResult right)
    {
        Assert.Multiple(() =>
        {
            Assert.That(left.Success, Is.True);
            Assert.That(right.Success, Is.True);
            Assert.That(right.Matrix.A, Is.EqualTo(left.Matrix.A).Within(1e-12));
            Assert.That(right.Matrix.B, Is.EqualTo(left.Matrix.B).Within(1e-12));
            Assert.That(right.Matrix.C, Is.EqualTo(left.Matrix.C).Within(1e-12));
            Assert.That(right.Matrix.D, Is.EqualTo(left.Matrix.D).Within(1e-12));
            Assert.That(right.Matrix.E, Is.EqualTo(left.Matrix.E).Within(1e-12));
            Assert.That(right.Matrix.F, Is.EqualTo(left.Matrix.F).Within(1e-12));
        });
    }

    static void AssertMatrix(SvgTransformMatrixResult result, double a, double b, double c, double d, double e, double f)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgTransformMatrixError.None));
            Assert.That(result.Matrix.A, Is.EqualTo(a).Within(1e-12));
            Assert.That(result.Matrix.B, Is.EqualTo(b).Within(1e-12));
            Assert.That(result.Matrix.C, Is.EqualTo(c).Within(1e-12));
            Assert.That(result.Matrix.D, Is.EqualTo(d).Within(1e-12));
            Assert.That(result.Matrix.E, Is.EqualTo(e).Within(1e-12));
            Assert.That(result.Matrix.F, Is.EqualTo(f).Within(1e-12));
        });
    }
}
