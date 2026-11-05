using icc;

namespace icc.Tests;

public class NamedColorMathTests
{
    [Test]
    public void ExactLookupAcceptsRootComposedAndPreparedNamesWithCaseSensitivity()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor color = CreateColor("Orange", new double[] { 1.0 }, new double[] { 50.0, 20.0, 30.0 }, new double[] { 0.1, 0.2, 0.3 });
        color.Description.Name = "Catalog Orange";
        IccNamedColorSet set = CreateSet("Book ", " 100%", color);

        IccNamedColor? root = colorMath.FindNamedColorByExactName(set, "Orange");
        IccNamedColor? composed = colorMath.FindNamedColorByExactName(set, "Book Orange 100%");
        IccNamedColor? prepared = colorMath.FindNamedColorByExactName(set, "Catalog Orange");
        IccNamedColor? wrongCase = colorMath.FindNamedColorByExactName(set, "orange");

        Assert.Multiple(() =>
        {
            Assert.That(root, Is.Not.Null.And.Not.SameAs(color));
            Assert.That(composed!.Name, Is.EqualTo("Orange"));
            Assert.That(prepared!.Name, Is.EqualTo("Orange"));
            Assert.That(wrongCase, Is.Null);
            Assert.That(root!.PcsColor.Components, Is.Not.SameAs(color.PcsColor.Components));
        });
    }

    [Test]
    public void NormalizedLookupAppliesUnicodeCaseAndWhitespaceNormalization()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor color = CreateColor("Orange", new double[] { 1.0 }, new double[] { 50.0, 20.0, 30.0 }, new double[0]);
        IccNamedColorSet set = CreateSet("Book ", " 100%", color);

        IccNamedColor? result = colorMath.FindNamedColorByNormalizedName(set, "  ＢＯＯＫ   orange  １００％  ");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Orange"));
    }

    [Test]
    public void TintSelectionInterpolatesFromSharedZeroAndBetweenSamples()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor color = CreateColor(
            "Orange",
            new double[] { 0.5, 1.0 },
            new double[] { 60.0, 10.0, 20.0, 20.0, 30.0, 40.0 },
            new double[] { 0.2, 0.4, 0.6, 0.6, 0.8, 1.0 });
        IccNamedColor zero = CreateColor(
            string.Empty,
            new double[0],
            new double[] { 100.0, 0.0, 0.0 },
            new double[] { 0.0, 0.0, 0.0 });
        IccNamedColorSet set = CreateSet("Book ", "", color);
        set.TintZero = zero;

        IccColorVector quarterPcs = colorMath.SelectNamedColorPcs(set, color, 0.25);
        IccColorVector threeQuarterDevice = colorMath.SelectNamedColorDevice(set, color, 0.75);
        IccColorDescription description = colorMath.EvaluateNamedColorTint(set, color, 0.75);

        Assert.Multiple(() =>
        {
            Assert.That(quarterPcs.Components, Is.EqualTo(new double[] { 80.0, 5.0, 10.0 }).Within(0.000000001));
            Assert.That(quarterPcs.ColorSpace, Is.EqualTo(IccColorSpaceKind.Lab));
            Assert.That(threeQuarterDevice.Components, Is.EqualTo(new double[] { 0.4, 0.6, 0.8 }).Within(0.000000001));
            Assert.That(description.Name, Is.EqualTo("Book Orange"));
            Assert.That(description.Lab, Is.Not.Null);
            Assert.That(new double[] { description.Lab!.L, description.Lab.A, description.Lab.B },
                Is.EqualTo(new double[] { 40.0, 20.0, 30.0 }).Within(0.000000001));
            Assert.That(description.Xyz, Is.Not.Null);
            Assert.That(description.XyY, Is.Not.Null);
            Assert.That(description.Lch, Is.Not.Null);
            Assert.That(description.DeviceColor!.Components, Is.EqualTo(new double[] { 0.4, 0.6, 0.8 }).Within(0.000000001));
        });
    }

    [Test]
    public void TintSelectionPreservesExactEndpointsAndIndependentComponents()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor color = CreateColor("Ink", new double[0], new double[] { 25.0, 4.0, -8.0 }, new double[0]);
        IccNamedColor zero = CreateColor(string.Empty, new double[0], new double[] { 100.0, 0.0, 0.0 }, new double[0]);
        IccNamedColorSet set = CreateSet("", "", color);
        set.TintZero = zero;

        IccColorVector zeroResult = colorMath.SelectNamedColorPcs(set, color, 0.0);
        IccColorVector fullResult = colorMath.SelectNamedColorPcs(set, color, 1.0);

        fullResult.Components[0] = 0.0;
        Assert.Multiple(() =>
        {
            Assert.That(zeroResult.Components, Is.EqualTo(new double[] { 100.0, 0.0, 0.0 }));
            Assert.That(color.PcsColor.Components[0], Is.EqualTo(25.0));
        });
    }

    [Test]
    public void NearestLookupUsesCiede2000AndStableSourceOrder()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor aAxis = CreateColor("A axis", new double[0], new double[] { 50.0, 10.0, 0.0 }, new double[0]);
        IccNamedColor bAxis = CreateColor("B axis", new double[0], new double[] { 50.0, 0.0, 10.0 }, new double[0]);
        IccNamedColor duplicate = CreateColor("B axis duplicate", new double[0], new double[] { 50.0, 0.0, 10.0 }, new double[0]);
        IccNamedColorSet set = CreateSet("", "", aAxis, bAxis, duplicate);

        IccNamedColor? nearest = colorMath.FindNearestNamedColorByLab(set, new double[] { 50.0, 0.0, 0.0 });

        Assert.That(nearest, Is.Not.Null.And.Not.SameAs(bAxis));
        Assert.That(nearest!.Name, Is.EqualTo("B axis"));
    }

    [Test]
    public void NearestLookupConvertsXyzPcsToD50Lab()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColor white = CreateColor(
            "Paper",
            new double[0],
            new double[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z },
            new double[0]);
        white.PcsColor.ColorSpace = IccColorSpaceKind.Xyz;
        white.PcsColor.ColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        IccNamedColor gray = CreateColor("Gray", new double[0], new double[] { 50.0, 0.0, 0.0 }, new double[0]);
        IccNamedColorSet set = CreateSet("", "", gray, white);

        IccNamedColor? nearest = colorMath.FindNearestNamedColorByLab(set, new double[] { 100.0, 0.0, 0.0 });

        Assert.That(nearest!.Name, Is.EqualTo("Paper"));
    }

    [Test]
    public void EmptyLookupReturnsNullAndInvalidTintDataIsRejected()
    {
        IIccColorMath colorMath = new IccColorMath();
        IccNamedColorSet empty = new IccNamedColorSet();
        IccNamedColor color = CreateColor("Ink", new double[] { 0.5, 1.0 }, new double[] { 50.0, 0.0, 0.0, 20.0 }, new double[0]);
        IccNamedColorSet set = CreateSet("", "", color);

        Assert.Multiple(() =>
        {
            Assert.That(colorMath.FindNamedColorByExactName(empty, "Missing"), Is.Null);
            Assert.That(colorMath.FindNearestNamedColorByLab(empty, new double[] { 50.0, 0.0, 0.0 }), Is.Null);
            Assert.Throws<IccTransformException>(() => colorMath.SelectNamedColorPcs(set, color, 0.25));
            Assert.Throws<IccTransformException>(() => colorMath.SelectNamedColorPcs(set, color, double.NaN));
            Assert.Throws<IccTransformException>(() => colorMath.FindNamedColorByNormalizedName(null!, "Ink"));
            Assert.Throws<IccTransformException>(() => colorMath.FindNearestNamedColorByLab(set, new double[] { 50.0, double.PositiveInfinity, 0.0 }));
        });
    }

    private static IccNamedColorSet CreateSet(string prefix, string suffix, params IccNamedColor[] colors)
    {
        IccNamedColorSet set = new IccNamedColorSet();
        set.Prefix = prefix;
        set.Suffix = suffix;
        set.Colors = colors;
        return set;
    }

    private static IccNamedColor CreateColor(string name, double[] tints, double[] pcs, double[] device)
    {
        IccNamedColor color = new IccNamedColor();
        color.Name = name;
        color.Tints = tints;
        color.PcsColor.ColorSpace = IccColorSpaceKind.Lab;
        color.PcsColor.ColorSpaceSignature = IccColorSpaceSignatures.Lab;
        color.PcsColor.Components = pcs;
        color.DeviceColor.ColorSpace = IccColorSpaceKind.Rgb;
        color.DeviceColor.ColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        color.DeviceColor.Components = device;
        color.Description.Name = name;
        return color;
    }
}
