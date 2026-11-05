namespace svg.Tests;

public class SvgColorTests
{
    [Test]
    public void ProgrammaticColorsValidateFiniteChannelsAndCurrentColorState()
    {
        SvgColor color = new SvgColor(SvgColorSpace.Srgb, -0.25, 0.5, 1.25, 0.75);
        SvgColor current = SvgColor.CurrentColor();

        Assert.Multiple(() =>
        {
            Assert.That(color.Space, Is.EqualTo(SvgColorSpace.Srgb));
            Assert.That(color.Red, Is.EqualTo(-0.25));
            Assert.That(color.Green, Is.EqualTo(0.5));
            Assert.That(color.Blue, Is.EqualTo(1.25));
            Assert.That(color.Alpha, Is.EqualTo(0.75));
            Assert.That(color.IsValid, Is.True);
            Assert.That(color.Source, Is.Empty);
            Assert.That(current.IsCurrentColor, Is.True);
            Assert.That(current.Alpha, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgColor(SvgColorSpace.Srgb, double.NaN, 0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgColor((SvgColorSpace)999, 0, 0, 0, 1));
        });
    }

    [Test]
    public void EveryCssNamedColorParsesCaseInsensitively()
    {
        string names = "aliceblue antiquewhite aqua aquamarine azure beige bisque black blanchedalmond blue blueviolet brown burlywood cadetblue chartreuse chocolate coral cornflowerblue cornsilk crimson cyan darkblue darkcyan darkgoldenrod darkgray darkgreen darkgrey darkkhaki darkmagenta darkolivegreen darkorange darkorchid darkred darksalmon darkseagreen darkslateblue darkslategray darkslategrey darkturquoise darkviolet deeppink deepskyblue dimgray dimgrey dodgerblue firebrick floralwhite forestgreen fuchsia gainsboro ghostwhite gold goldenrod gray green greenyellow grey honeydew hotpink indianred indigo ivory khaki lavender lavenderblush lawngreen lemonchiffon lightblue lightcoral lightcyan lightgoldenrodyellow lightgray lightgreen lightgrey lightpink lightsalmon lightseagreen lightskyblue lightslategray lightslategrey lightsteelblue lightyellow lime limegreen linen magenta maroon mediumaquamarine mediumblue mediumorchid mediumpurple mediumseagreen mediumslateblue mediumspringgreen mediumturquoise mediumvioletred midnightblue mintcream mistyrose moccasin navajowhite navy oldlace olive olivedrab orange orangered orchid palegoldenrod palegreen paleturquoise palevioletred papayawhip peachpuff peru pink plum powderblue purple rebeccapurple red rosybrown royalblue saddlebrown salmon sandybrown seagreen seashell sienna silver skyblue slateblue slategray slategrey snow springgreen steelblue tan teal thistle tomato turquoise violet wheat white whitesmoke yellow yellowgreen";
        string[] values = names.Split(' ');

        foreach (string name in values)
        {
            SvgColorParseResult result = new SvgColorParser().Parse(name.ToUpperInvariant(), 32);
            Assert.That(result.Success, Is.True, name);
            Assert.That(result.Color.Alpha, Is.EqualTo(1), name);
        }

        Assert.That(values, Has.Length.EqualTo(148));
        AssertColor(new SvgColorParser().Parse("rebeccapurple", 32), 102 / 255.0, 51 / 255.0, 153 / 255.0, 1);
        AssertColor(new SvgColorParser().Parse("green", 32), 0, 128 / 255.0, 0, 1);
        AssertColor(new SvgColorParser().Parse("lime", 32), 0, 1, 0, 1);
        AssertEquivalent(new SvgColorParser().Parse("aqua", 32), new SvgColorParser().Parse("cyan", 32));
        AssertEquivalent(new SvgColorParser().Parse("gray", 32), new SvgColorParser().Parse("grey", 32));
    }

    [Test]
    public void TransparentCurrentColorEscapesAndSourceArePreserved()
    {
        SvgColorParseResult transparent = new SvgColorParser().Parse("  TrAnSpArEnT  ", 32);
        SvgColorParseResult current = new SvgColorParser().Parse("CURRENTcolor", 32);
        SvgColorParseResult escapedNamed = new SvgColorParser().Parse("r\\65 d", 32);
        SvgColorParseResult escapedFunction = new SvgColorParser().Parse("r\\67 b(255 0 0)", 32);

        AssertColor(transparent, 0, 0, 0, 0);
        AssertColor(escapedNamed, 1, 0, 0, 1);
        AssertColor(escapedFunction, 1, 0, 0, 1);
        Assert.Multiple(() =>
        {
            Assert.That(transparent.Color.Source, Is.EqualTo("  TrAnSpArEnT  "));
            Assert.That(current.Success, Is.True);
            Assert.That(current.Color.IsCurrentColor, Is.True);
            Assert.That(current.Color.Source, Is.EqualTo("CURRENTcolor"));
        });
    }

    [TestCase("#123", 0x11, 0x22, 0x33, 0xff)]
    [TestCase("#1234", 0x11, 0x22, 0x33, 0x44)]
    [TestCase("#A1b2C3", 0xa1, 0xb2, 0xc3, 0xff)]
    [TestCase("#A1b2C380", 0xa1, 0xb2, 0xc3, 0x80)]
    public void HexColorsSupportEveryCssDigitCount(string text, int red, int green, int blue, int alpha)
    {
        AssertColor(new SvgColorParser().Parse(text, 32), red / 255.0, green / 255.0, blue / 255.0, alpha / 255.0);
    }

    [Test]
    public void LegacyRgbSupportsNumbersPercentagesAlphaAliasesAndClamping()
    {
        SvgColorParseResult numbers = new SvgColorParser().Parse("rgb(255, 127.5, 0)", 32);
        SvgColorParseResult percentages = new SvgColorParser().Parse("RGB(100%, 50%, 0%, 25%)", 32);
        SvgColorParseResult rgba = new SvgColorParser().Parse("rgba(300, -10, 127.5, 1.5)", 32);
        SvgColorParseResult rgbaWithoutAlpha = new SvgColorParser().Parse("rgba(1, 2, 3)", 32);

        AssertColor(numbers, 1, 0.5, 0, 1);
        AssertColor(percentages, 1, 0.5, 0, 0.25);
        AssertColor(rgba, 1, 0, 0.5, 1);
        AssertColor(rgbaWithoutAlpha, 1 / 255.0, 2 / 255.0, 3 / 255.0, 1);
    }

    [Test]
    public void ModernRgbSupportsMixedUnitsSlashAlphaAndMissingComponents()
    {
        SvgColorParseResult mixed = new SvgColorParser().Parse("rgb(100% 128 0% / 25%)", 32);
        SvgColorParseResult alias = new SvgColorParser().Parse("rgba(255 0 127.5 / .5)", 32);
        SvgColorParseResult missing = new SvgColorParser().Parse("rgb(none 20 none / none)", 32);
        SvgColorParseResult clamped = new SvgColorParser().Parse("rgb(-10% 300 -1 / -20%)", 32);

        AssertColor(mixed, 1, 128 / 255.0, 0, 0.25);
        AssertColor(alias, 1, 0, 0.5, 0.5);
        AssertColor(missing, 0, 20 / 255.0, 0, 0);
        AssertColor(clamped, 0, 1, 0, 0);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Color.FirstMissing, Is.True);
            Assert.That(missing.Color.SecondMissing, Is.False);
            Assert.That(missing.Color.ThirdMissing, Is.True);
            Assert.That(missing.Color.AlphaMissing, Is.True);
        });
    }

    [Test]
    public void LegacyHslSupportsHueUnitsAliasesAndAlpha()
    {
        SvgColorParseResult red = new SvgColorParser().Parse("hsl(0, 100%, 50%)", 32);
        SvgColorParseResult green = new SvgColorParser().Parse("hsla(133.333333grad, 100%, 50%, 50%)", 32);
        SvgColorParseResult blue = new SvgColorParser().Parse("hsl(0.6666666666666666turn, 100%, 50%)", 32);
        SvgColorParseResult wrapped = new SvgColorParser().Parse("hsl(-6.283185307179586rad, 100%, 50%)", 32);

        AssertColor(red, 1, 0, 0, 1);
        AssertColor(green, 0, 1, 0, 0.5, 1e-8);
        AssertColor(blue, 0, 0, 1, 1);
        AssertColor(wrapped, 1, 0, 0, 1, 1e-12);
    }

    [Test]
    public void ModernHslSupportsNumberComponentsMissingValuesAndNegativeSaturationClamping()
    {
        SvgColorParseResult numbers = new SvgColorParser().Parse("hsl(120 100 50 / .25)", 32);
        SvgColorParseResult gray = new SvgColorParser().Parse("hsl(20 -10% 50%)", 32);
        SvgColorParseResult missing = new SvgColorParser().Parse("hsla(none none 50% / none)", 32);
        SvgColorParseResult hueWrap = new SvgColorParser().Parse("hsl(480deg 100% 50%)", 32);
        SvgColorParseResult hugeTurns = new SvgColorParser().Parse("hsl(1e308turn 100% 50%)", 32);

        AssertColor(numbers, 0, 1, 0, 0.25);
        AssertColor(gray, 0.5, 0.5, 0.5, 1);
        AssertColor(missing, 0.5, 0.5, 0.5, 0);
        AssertColor(hueWrap, 0, 1, 0, 1);
        Assert.That(hugeTurns.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Color.FirstMissing, Is.True);
            Assert.That(missing.Color.SecondMissing, Is.True);
            Assert.That(missing.Color.ThirdMissing, Is.False);
            Assert.That(missing.Color.AlphaMissing, Is.True);
        });
    }

    [Test]
    public void HwbParsesNumbersPercentagesMissingValuesAndNormalizesToSrgb()
    {
        SvgColorParseResult green = new SvgColorParser().Parse("hwb(150 20% 10% / 50%)", 32);
        SvgColorParseResult gray = new SvgColorParser().Parse("hwb(45 40 80)", 32);
        SvgColorParseResult missing = new SvgColorParser().Parse("hwb(none none 30% / none)", 32);

        AssertNative(green, SvgColorSpace.Hwb, 150, 20, 10, 0.5);
        AssertNative(gray, SvgColorSpace.Hwb, 45, 40, 80, 1);
        AssertNative(missing, SvgColorSpace.Hwb, 0, 0, 30, 0);
        AssertOperationColor(green.Color.ConvertTo(SvgColorSpace.Srgb), 0.2, 0.9, 0.55, 0.5, 1e-12);
        AssertOperationColor(gray.Color.ConvertTo(SvgColorSpace.Srgb), 1.0 / 3, 1.0 / 3, 1.0 / 3, 1, 1e-12);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Color.FirstMissing, Is.True);
            Assert.That(missing.Color.SecondMissing, Is.True);
            Assert.That(missing.Color.ThirdMissing, Is.False);
            Assert.That(missing.Color.AlphaMissing, Is.True);
        });
    }

    [Test]
    public void LabAndOklabFamiliesParsePercentageReferenceRangesAndHueUnits()
    {
        SvgColorParseResult lab = new SvgColorParser().Parse("lab(56.2% 0 66.88% / 93%)", 32);
        SvgColorParseResult lch = new SvgColorParser().Parse("lch(37% 70% 305deg)", 32);
        SvgColorParseResult oklab = new SvgColorParser().Parse("oklab(54% -25% -5%)", 32);
        SvgColorParseResult oklch = new SvgColorParser().Parse("oklch(54% 25% 400grad / .5)", 32);

        AssertNative(lab, SvgColorSpace.Lab, 56.2, 0, 83.6, 0.93);
        AssertNative(lch, SvgColorSpace.Lch, 37, 105, 305, 1);
        AssertNative(oklab, SvgColorSpace.Oklab, 0.54, -0.1, -0.02, 1);
        AssertNative(oklch, SvgColorSpace.Oklch, 0.54, 0.1, 0, 0.5);
    }

    [Test]
    public void ColorSrgbParsesExtendedChannelsAlphaAndMissingValuesWithoutClamping()
    {
        SvgColorParseResult extended = new SvgColorParser().Parse("color(SRGB 1.2 50% -0.1 / 40%)", 32);
        SvgColorParseResult linear = new SvgColorParser().Parse("color(srgb-linear 1 0.5 0)", 32);
        SvgColorParseResult missing = new SvgColorParser().Parse("color(srgb none 0 none / none)", 32);

        AssertNative(extended, SvgColorSpace.Srgb, 1.2, 0.5, -0.1, 0.4);
        AssertNative(linear, SvgColorSpace.SrgbLinear, 1, 0.5, 0, 1);
        AssertNative(missing, SvgColorSpace.Srgb, 0, 0, 0, 0);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Color.FirstMissing, Is.True);
            Assert.That(missing.Color.ThirdMissing, Is.True);
            Assert.That(missing.Color.AlphaMissing, Is.True);
        });
    }

    [Test]
    public void ColorConversionUsesCssColorFourReferenceSpacesAndRoundTrips()
    {
        SvgColor red = new SvgColor(SvgColorSpace.Srgb, 1, 0, 0, 0.75);
        SvgColorOperationResult linear = red.ConvertTo(SvgColorSpace.SrgbLinear);
        SvgColorOperationResult lab = red.ConvertTo(SvgColorSpace.Lab);
        SvgColorOperationResult oklab = red.ConvertTo(SvgColorSpace.Oklab);

        AssertOperationColor(linear, 1, 0, 0, 0.75, 1e-12);
        AssertOperationColor(lab, 54.2905, 80.8049, 69.8909, 0.75, 0.002);
        AssertOperationColor(oklab, 0.627955, 0.224863, 0.125846, 0.75, 0.000002);

        foreach (SvgColorSpace space in new[] { SvgColorSpace.SrgbLinear, SvgColorSpace.Hwb, SvgColorSpace.Lab, SvgColorSpace.Lch, SvgColorSpace.Oklab, SvgColorSpace.Oklch })
        {
            SvgColorOperationResult converted = red.ConvertTo(space);
            Assert.That(converted.Success, Is.True, space.ToString());
            AssertOperationColor(converted.Color.ConvertTo(SvgColorSpace.Srgb), 1, 0, 0, 0.75, 0.0000001);
            Assert.That(converted.Color.IsInSrgbGamut(), Is.True, space.ToString());
        }
    }

    [Test]
    public void InterpolationUsesPremultipliedAlphaAndShortestPolarHue()
    {
        SvgColor opaqueRed = new SvgColor(SvgColorSpace.Srgb, 1, 0, 0, 1);
        SvgColor transparentBlue = new SvgColor(SvgColorSpace.Srgb, 0, 0, 1, 0);
        SvgColor firstHue = new SvgColor(SvgColorSpace.Oklch, 0.5, 0.2, 350, 1);
        SvgColor secondHue = new SvgColor(SvgColorSpace.Oklch, 0.7, 0.4, 10, 1);
        SvgColor missingRed = new SvgColorParser().Parse("color(srgb 1 0 0 / none)", 32).Color;
        SvgColor missingBlue = new SvgColorParser().Parse("color(srgb 0 0 1 / none)", 32).Color;

        AssertOperationColor(opaqueRed.Interpolate(transparentBlue, 0.5, SvgColorSpace.Srgb), 1, 0, 0, 0.5, 1e-12);
        SvgColorOperationResult polar = firstHue.Interpolate(secondHue, 0.5, SvgColorSpace.Oklch);
        AssertOperationColor(polar, 0.6, 0.3, 0, 1, 1e-12);
        SvgColorOperationResult missingAlpha = missingRed.Interpolate(missingBlue, 0.5, SvgColorSpace.Srgb);
        AssertOperationColor(missingAlpha, 0.5, 0, 0.5, 0, 1e-12);
        Assert.That(missingAlpha.Color.AlphaMissing, Is.True);
        Assert.That(opaqueRed.Interpolate(transparentBlue, -0.1, SvgColorSpace.Srgb).Error, Is.EqualTo(SvgColorOperationError.InvalidArgument));
        Assert.That(SvgColor.CurrentColor().Interpolate(opaqueRed, 0.5, SvgColorSpace.Srgb).Error, Is.EqualTo(SvgColorOperationError.CurrentColorUnresolved));
    }

    [Test]
    public void GamutChecksAndClippingOperateOnConvertedSrgbChannels()
    {
        SvgColor extended = new SvgColor(SvgColorSpace.Srgb, 1.2, -0.1, 0.5, 0.4);
        SvgColor lab = new SvgColor(SvgColorSpace.Lab, 50, 0, 0, 1);

        Assert.That(extended.IsInSrgbGamut(), Is.False);
        AssertOperationColor(extended.ClipToSrgbGamut(), 1, 0, 0.5, 0.4, 1e-12);
        Assert.That(lab.IsInSrgbGamut(), Is.True);
        Assert.That(SvgColor.CurrentColor().ClipToSrgbGamut().Error, Is.EqualTo(SvgColorOperationError.CurrentColorUnresolved));
    }

    [Test]
    public void CanonicalFormattingPreservesNativeSpacesAlphaAndMissingComponents()
    {
        Assert.That(new SvgColorParser().Parse("hwb(none none 30% / none)", 32).Color.ToText(), Is.EqualTo("hwb(none none 30% / none)"));
        Assert.That(new SvgColorParser().Parse("lab(56.200% 0.000 66.88%)", 32).Color.ToText(), Is.EqualTo("lab(56.2 0 83.6)"));
        Assert.That(new SvgColorParser().Parse("lch(37% 105.0 305.00 / 93%)", 32).Color.ToText(), Is.EqualTo("lch(37 105 305 / 0.93)"));
        Assert.That(new SvgColorParser().Parse("oklab(54.0% -25% -5%)", 32).Color.ToText(), Is.EqualTo("oklab(0.54 -0.1 -0.02)"));
        Assert.That(new SvgColorParser().Parse("color(srgb none 0 1 / .5)", 32).Color.ToText(), Is.EqualTo("color(srgb none 0 1 / 0.5)"));
        SvgColor linear = new SvgColorParser().Parse("color(srgb-linear 1 .5 0)", 32).Color;
        Assert.That(new SvgColorParser().Parse(linear.ToText(), 32).Color.ToText(), Is.EqualTo("color(srgb-linear 1 0.5 0)"));
        Assert.That(SvgColor.CurrentColor().ToText(), Is.EqualTo("currentColor"));
    }

    [Test]
    public void ModernColorFunctionsRejectLegacySeparatorsAndUnknownProfiles()
    {
        Assert.That(new SvgColorParser().Parse("hwb(0, 0%, 0%)", 32).Error, Is.EqualTo(SvgColorParseError.InvalidSeparator));
        Assert.That(new SvgColorParser().Parse("lab(50, 0, 0)", 32).Error, Is.EqualTo(SvgColorParseError.InvalidSeparator));
        Assert.That(new SvgColorParser().Parse("color(display-p3 1 0 0)", 32).Error, Is.EqualTo(SvgColorParseError.InvalidUnit));
    }

    [Test]
    public void InvalidColorsReturnStableKindsAndOffsets()
    {
        SvgColorParseResult empty = new SvgColorParser().Parse(" ", 32);
        SvgColorParseResult unknown = new SvgColorParser().Parse("notacolor", 32);
        SvgColorParseResult hexCount = new SvgColorParser().Parse("#12", 32);
        SvgColorParseResult hexDigit = new SvgColorParser().Parse("#12345g", 32);
        SvgColorParseResult function = new SvgColorParser().Parse("devicecmyk(0 0 0 0)", 32);
        SvgColorParseResult mixed = new SvgColorParser().Parse("rgb(10%, 20, 30%)", 32);
        SvgColorParseResult separator = new SvgColorParser().Parse("rgb(1 2, 3)", 32);
        SvgColorParseResult hslUnit = new SvgColorParser().Parse("hsl(0, 100, 50%)", 32);
        SvgColorParseResult exponent = new SvgColorParser().Parse("rgb(1e309 0 0)", 32);
        SvgColorParseResult trailing = new SvgColorParser().Parse("red blue", 32);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgColorParseError.ExpectedColor));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(unknown.Error, Is.EqualTo(SvgColorParseError.UnknownColor));
            Assert.That(hexCount.Error, Is.EqualTo(SvgColorParseError.InvalidHex));
            Assert.That(hexDigit.Error, Is.EqualTo(SvgColorParseError.InvalidHex));
            Assert.That(function.Error, Is.EqualTo(SvgColorParseError.UnknownFunction));
            Assert.That(mixed.Error, Is.EqualTo(SvgColorParseError.MixedLegacyUnits));
            Assert.That(separator.Error, Is.EqualTo(SvgColorParseError.InvalidSeparator));
            Assert.That(hslUnit.Error, Is.EqualTo(SvgColorParseError.InvalidUnit));
            Assert.That(exponent.Error, Is.EqualTo(SvgColorParseError.InvalidNumber));
            Assert.That(trailing.Error, Is.EqualTo(SvgColorParseError.TrailingCharacters));
        });
    }

    [Test]
    public void ParserHonorsBoundsAndRejectsInvalidArguments()
    {
        SvgColorParseResult nullInput = new SvgColorParser().Parse(null, 32);
        SvgColorParseResult invalidBound = new SvgColorParser().Parse("red", 0);
        SvgColorParseResult nameBound = new SvgColorParser().Parse("rebeccapurple", 5);
        SvgColorParseResult functionBound = new SvgColorParser().Parse("rgba(0 0 0)", 3);
        SvgColorParseResult numberBound = new SvgColorParser().Parse("rgb(123456 0 0)", 5);
        SvgColorParseResult hexBound = new SvgColorParser().Parse("#123456", 5);

        Assert.Multiple(() =>
        {
            Assert.That(nullInput.Error, Is.EqualTo(SvgColorParseError.InvalidArgument));
            Assert.That(invalidBound.Error, Is.EqualTo(SvgColorParseError.InvalidArgument));
            Assert.That(nameBound.Error, Is.EqualTo(SvgColorParseError.TokenTooLong));
            Assert.That(functionBound.Error, Is.EqualTo(SvgColorParseError.TokenTooLong));
            Assert.That(numberBound.Error, Is.EqualTo(SvgColorParseError.TokenTooLong));
            Assert.That(hexBound.Error, Is.EqualTo(SvgColorParseError.TokenTooLong));
        });
    }

    static void AssertEquivalent(SvgColorParseResult first, SvgColorParseResult second)
    {
        AssertColor(first, second.Color.Red, second.Color.Green, second.Color.Blue, second.Color.Alpha);
    }

    static void AssertNative(SvgColorParseResult result, SvgColorSpace space, double first, double second, double third, double alpha)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Color.Space, Is.EqualTo(space));
        Assert.That(result.Color.Red, Is.EqualTo(first).Within(1e-12));
        Assert.That(result.Color.Green, Is.EqualTo(second).Within(1e-12));
        Assert.That(result.Color.Blue, Is.EqualTo(third).Within(1e-12));
        Assert.That(result.Color.Alpha, Is.EqualTo(alpha).Within(1e-12));
    }

    static void AssertOperationColor(SvgColorOperationResult result, double first, double second, double third, double alpha, double tolerance)
    {
        Assert.That(result.Success, Is.True, result.Error.ToString());
        Assert.That(result.Error, Is.EqualTo(SvgColorOperationError.None));
        Assert.That(result.Color.Red, Is.EqualTo(first).Within(tolerance));
        Assert.That(result.Color.Green, Is.EqualTo(second).Within(tolerance));
        Assert.That(result.Color.Blue, Is.EqualTo(third).Within(tolerance));
        Assert.That(result.Color.Alpha, Is.EqualTo(alpha).Within(tolerance));
    }

    static void AssertColor(SvgColorParseResult result, double red, double green, double blue, double alpha)
    {
        AssertColor(result, red, green, blue, alpha, 1e-12);
    }

    static void AssertColor(SvgColorParseResult result, double red, double green, double blue, double alpha, double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgColorParseError.None));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
            Assert.That(result.Color, Is.Not.Null);
            Assert.That(result.Color.Red, Is.EqualTo(red).Within(tolerance));
            Assert.That(result.Color.Green, Is.EqualTo(green).Within(tolerance));
            Assert.That(result.Color.Blue, Is.EqualTo(blue).Within(tolerance));
            Assert.That(result.Color.Alpha, Is.EqualTo(alpha).Within(tolerance));
            Assert.That(result.Color.IsValid, Is.True);
        });
    }
}
