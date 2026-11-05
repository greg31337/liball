using fake;

namespace fake.Tests;

[TestFixture]
public class ColorProviderTests
{
    static Dictionary<string, string> NamedHex = new Dictionary<string, string>
    {
        ["black"] = "#000000",
        ["blue"] = "#0000FF",
        ["cyan"] = "#00FFFF",
        ["gray"] = "#808080",
        ["green"] = "#008000",
        ["lime"] = "#00FF00",
        ["magenta"] = "#FF00FF",
        ["maroon"] = "#800000",
        ["navy"] = "#000080",
        ["orange"] = "#FFA500",
        ["purple"] = "#800080",
        ["red"] = "#FF0000",
        ["silver"] = "#C0C0C0",
        ["teal"] = "#008080",
        ["white"] = "#FFFFFF",
        ["yellow"] = "#FFFF00"
    };

    [Test]
    public void RandomColorModelsAreDeterministicAndBounded()
    {
        const long seed = 12200;
        ColorProvider first = Fake.Create(seed).Color();
        ColorProvider second = Fake.Create(seed).Color();

        int index = 0;
        while (index < 128)
        {
            string name = first.Name();
            string sameName = second.Name();
            string hex = first.Hex();
            string sameHex = second.Hex();
            RgbColor rgb = first.Rgb();
            RgbColor sameRgb = second.Rgb();
            HslColor hsl = first.Hsl();
            HslColor sameHsl = second.Hsl();
            HwbColor hwb = first.Hwb();
            HwbColor sameHwb = second.Hwb();
            CmykColor cmyk = first.Cmyk();
            CmykColor sameCmyk = second.Cmyk();
            LabColor lab = first.Lab();
            LabColor sameLab = second.Lab();
            LchColor lch = first.Lch();
            LchColor sameLch = second.Lch();
            string css = first.Css();
            string sameCss = second.Css();

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo(sameName));
                Assert.That(hex, Is.EqualTo(sameHex));
                Assert.That(rgb.Red, Is.InRange(0, 255));
                Assert.That(rgb.Green, Is.InRange(0, 255));
                Assert.That(rgb.Blue, Is.InRange(0, 255));
                Assert.That(rgb.Red, Is.EqualTo(sameRgb.Red));
                Assert.That(rgb.Green, Is.EqualTo(sameRgb.Green));
                Assert.That(rgb.Blue, Is.EqualTo(sameRgb.Blue));
                Assert.That(hsl.Hue, Is.InRange(0.0, 360.0));
                Assert.That(hsl.Saturation, Is.InRange(0.0, 100.0));
                Assert.That(hsl.Lightness, Is.InRange(0.0, 100.0));
                Assert.That(hsl.Hue, Is.EqualTo(sameHsl.Hue));
                Assert.That(hsl.Saturation,
                    Is.EqualTo(sameHsl.Saturation));
                Assert.That(hsl.Lightness, Is.EqualTo(sameHsl.Lightness));
                Assert.That(hwb.Hue, Is.InRange(0.0, 360.0));
                Assert.That(hwb.Whiteness, Is.InRange(0.0, 100.0));
                Assert.That(hwb.Blackness, Is.InRange(0.0, 100.0));
                Assert.That(hwb.Whiteness + hwb.Blackness,
                    Is.LessThanOrEqualTo(100.000001));
                Assert.That(hwb.Hue, Is.EqualTo(sameHwb.Hue));
                Assert.That(hwb.Whiteness, Is.EqualTo(sameHwb.Whiteness));
                Assert.That(hwb.Blackness, Is.EqualTo(sameHwb.Blackness));
                Assert.That(cmyk.Cyan, Is.InRange(0.0, 100.0));
                Assert.That(cmyk.Magenta, Is.InRange(0.0, 100.0));
                Assert.That(cmyk.Yellow, Is.InRange(0.0, 100.0));
                Assert.That(cmyk.Key, Is.InRange(0.0, 100.0));
                Assert.That(cmyk.Cyan, Is.EqualTo(sameCmyk.Cyan));
                Assert.That(cmyk.Magenta, Is.EqualTo(sameCmyk.Magenta));
                Assert.That(cmyk.Yellow, Is.EqualTo(sameCmyk.Yellow));
                Assert.That(cmyk.Key, Is.EqualTo(sameCmyk.Key));
                Assert.That(lab.Lightness, Is.InRange(0.0, 100.0));
                Assert.That(lab.Lightness, Is.EqualTo(sameLab.Lightness));
                Assert.That(lab.A, Is.EqualTo(sameLab.A));
                Assert.That(lab.B, Is.EqualTo(sameLab.B));
                Assert.That(lch.Lightness, Is.InRange(0.0, 100.0));
                Assert.That(lch.Chroma, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(lch.Hue, Is.InRange(0.0, 360.0));
                Assert.That(lch.Lightness, Is.EqualTo(sameLch.Lightness));
                Assert.That(lch.Chroma, Is.EqualTo(sameLch.Chroma));
                Assert.That(lch.Hue, Is.EqualTo(sameLch.Hue));
                Assert.That(NamedHex.ContainsKey(name), Is.True);
                Assert.That(hex, Does.Match("^#[0-9A-F]{6}$"));
                Assert.That(css, Is.EqualTo(sameCss));
                Assert.That(css, Does.Match(
                    "^rgb\\((?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]) (?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]) (?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\)$"));
            });
            index++;
        }
    }

    [Test]
    public void RgbFactoriesHexAndCssCoverChannelBoundaries()
    {
        ColorProvider color = Fake.Create(12201).Color();
        RgbColor black = color.RgbFromRedAndGreenAndBlue(0, 0, 0);
        RgbColor white = color.RgbFromRedAndGreenAndBlue(255, 255, 255);
        RgbColor parsed = color.RgbFromHex("#0a80Ff");
        RgbColor parsedWithoutPrefix = color.RgbFromHex("0A80FF");

        Assert.Multiple(() =>
        {
            Assert.That(color.HexFromValue(black), Is.EqualTo("#000000"));
            Assert.That(color.HexFromValue(white), Is.EqualTo("#FFFFFF"));
            Assert.That(color.HexFromValue(parsed), Is.EqualTo("#0A80FF"));
            Assert.That(parsed.Red, Is.EqualTo(10));
            Assert.That(parsed.Green, Is.EqualTo(128));
            Assert.That(parsed.Blue, Is.EqualTo(255));
            Assert.That(parsedWithoutPrefix.Red, Is.EqualTo(parsed.Red));
            Assert.That(parsedWithoutPrefix.Green, Is.EqualTo(parsed.Green));
            Assert.That(parsedWithoutPrefix.Blue, Is.EqualTo(parsed.Blue));
            Assert.That(color.CssFromValue(black), Is.EqualTo("rgb(0 0 0)"));
            Assert.That(color.CssFromValue(white), Is.EqualTo("rgb(255 255 255)"));
            Assert.That(() => color.RgbFromRedAndGreenAndBlue(-1, 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => color.RgbFromRedAndGreenAndBlue(0, 256, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => color.RgbFromHex("#FFF"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => color.RgbFromHex("#GG0000"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => color.RgbFromHex((string)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => color.HexFromValue(null),
                Throws.TypeOf<ArgumentNullException>());
        });

        parsed.Red = 300;
        Assert.That(() => color.HslFromValue(parsed),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void PrimaryAndNeutralConversionsHaveKnownHslHwbAndCmykValues()
    {
        ColorProvider color = Fake.Create(12202).Color();
        RgbColor red = color.RgbFromRedAndGreenAndBlue(255, 0, 0);
        RgbColor green = color.RgbFromRedAndGreenAndBlue(0, 255, 0);
        RgbColor blue = color.RgbFromRedAndGreenAndBlue(0, 0, 255);
        RgbColor black = color.RgbFromRedAndGreenAndBlue(0, 0, 0);
        RgbColor white = color.RgbFromRedAndGreenAndBlue(255, 255, 255);
        HslColor redHsl = color.HslFromValue(red);
        HslColor greenHsl = color.HslFromValue(green);
        HslColor blueHsl = color.HslFromValue(blue);
        HwbColor redHwb = color.HwbFromValue(red);
        HwbColor whiteHwb = color.HwbFromValue(white);
        CmykColor redCmyk = color.CmykFromValue(red);
        CmykColor blackCmyk = color.CmykFromValue(black);
        CmykColor whiteCmyk = color.CmykFromValue(white);

        Assert.Multiple(() =>
        {
            Assert.That(redHsl.Hue, Is.EqualTo(0.0));
            Assert.That(greenHsl.Hue, Is.EqualTo(120.0));
            Assert.That(blueHsl.Hue, Is.EqualTo(240.0));
            Assert.That(redHsl.Saturation, Is.EqualTo(100.0));
            Assert.That(redHsl.Lightness, Is.EqualTo(50.0));
            Assert.That(redHwb.Hue, Is.EqualTo(0.0));
            Assert.That(redHwb.Whiteness, Is.EqualTo(0.0));
            Assert.That(redHwb.Blackness, Is.EqualTo(0.0));
            Assert.That(whiteHwb.Hue, Is.EqualTo(0.0));
            Assert.That(whiteHwb.Whiteness, Is.EqualTo(100.0));
            Assert.That(whiteHwb.Blackness, Is.EqualTo(0.0));
            Assert.That(redCmyk.Cyan, Is.EqualTo(0.0));
            Assert.That(redCmyk.Magenta, Is.EqualTo(100.0));
            Assert.That(redCmyk.Yellow, Is.EqualTo(100.0));
            Assert.That(redCmyk.Key, Is.EqualTo(0.0));
            Assert.That(blackCmyk.Cyan, Is.EqualTo(0.0));
            Assert.That(blackCmyk.Magenta, Is.EqualTo(0.0));
            Assert.That(blackCmyk.Yellow, Is.EqualTo(0.0));
            Assert.That(blackCmyk.Key, Is.EqualTo(100.0));
            Assert.That(whiteCmyk.Cyan, Is.EqualTo(0.0));
            Assert.That(whiteCmyk.Magenta, Is.EqualTo(0.0));
            Assert.That(whiteCmyk.Yellow, Is.EqualTo(0.0));
            Assert.That(whiteCmyk.Key, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void LabAndLchUseD65ReferenceConversions()
    {
        ColorProvider color = Fake.Create(12203).Color();
        LabColor black = color.LabFromValue(color.RgbFromRedAndGreenAndBlue(0, 0, 0));
        LabColor white = color.LabFromValue(color.RgbFromRedAndGreenAndBlue(255, 255, 255));
        LabColor red = color.LabFromValue(color.RgbFromRedAndGreenAndBlue(255, 0, 0));
        LchColor redLch = color.LchFromValue(color.RgbFromRedAndGreenAndBlue(255, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(black.Lightness, Is.EqualTo(0.0).Within(0.000001));
            Assert.That(black.A, Is.EqualTo(0.0).Within(0.000001));
            Assert.That(black.B, Is.EqualTo(0.0).Within(0.000001));
            Assert.That(white.Lightness, Is.EqualTo(100.0).Within(0.0001));
            Assert.That(white.A, Is.EqualTo(0.0).Within(0.0001));
            Assert.That(white.B, Is.EqualTo(0.0).Within(0.0001));
            Assert.That(red.Lightness, Is.EqualTo(53.2408).Within(0.001));
            Assert.That(red.A, Is.EqualTo(80.0925).Within(0.001));
            Assert.That(red.B, Is.EqualTo(67.2032).Within(0.001));
            Assert.That(redLch.Lightness,
                Is.EqualTo(red.Lightness).Within(0.000001));
            Assert.That(redLch.Chroma,
                Is.EqualTo(104.5518).Within(0.001));
            Assert.That(redLch.Hue, Is.EqualTo(40.0).Within(0.01));
        });
    }

    [Test]
    public void RecordsKeepEveryRepresentationCorrelated()
    {
        const long seed = 12204;
        ColorProvider first = Fake.Create(seed).Color();
        ColorProvider second = Fake.Create(seed).Color();

        int index = 0;
        while (index < 128)
        {
            ColorRecord value = first.Record();
            ColorRecord same = second.Record();
            HslColor hsl = first.HslFromValue(value.Rgb);
            HwbColor hwb = first.HwbFromValue(value.Rgb);
            CmykColor cmyk = first.CmykFromValue(value.Rgb);
            LabColor lab = first.LabFromValue(value.Rgb);
            LchColor lch = first.LchFromValue(value.Rgb);

            Assert.Multiple(() =>
            {
                Assert.That(value.Name, Is.EqualTo(same.Name));
                Assert.That(value.Hex, Is.EqualTo(same.Hex));
                Assert.That(value.Hex, Is.EqualTo(NamedHex[value.Name]));
                Assert.That(value.Hex, Is.EqualTo(first.HexFromValue(value.Rgb)));
                Assert.That(value.Css, Is.EqualTo(first.CssFromValue(value.Rgb)));
                Assert.That(value.Hsl.Hue, Is.EqualTo(hsl.Hue));
                Assert.That(value.Hsl.Saturation,
                    Is.EqualTo(hsl.Saturation));
                Assert.That(value.Hsl.Lightness, Is.EqualTo(hsl.Lightness));
                Assert.That(value.Hwb.Hue, Is.EqualTo(hwb.Hue));
                Assert.That(value.Hwb.Whiteness, Is.EqualTo(hwb.Whiteness));
                Assert.That(value.Hwb.Blackness, Is.EqualTo(hwb.Blackness));
                Assert.That(value.Cmyk.Cyan, Is.EqualTo(cmyk.Cyan));
                Assert.That(value.Cmyk.Magenta, Is.EqualTo(cmyk.Magenta));
                Assert.That(value.Cmyk.Yellow, Is.EqualTo(cmyk.Yellow));
                Assert.That(value.Cmyk.Key, Is.EqualTo(cmyk.Key));
                Assert.That(value.Lab.Lightness, Is.EqualTo(lab.Lightness));
                Assert.That(value.Lab.A, Is.EqualTo(lab.A));
                Assert.That(value.Lab.B, Is.EqualTo(lab.B));
                Assert.That(value.Lch.Lightness, Is.EqualTo(lch.Lightness));
                Assert.That(value.Lch.Chroma, Is.EqualTo(lch.Chroma));
                Assert.That(value.Lch.Hue, Is.EqualTo(lch.Hue));
                Assert.That(value.IsSynthetic, Is.True);
            });
            index++;
        }
    }

    [Test]
    public void PalettesHaveRequestedCountsAndNoDuplicateNames()
    {
        const long seed = 12205;
        ColorProvider first = Fake.Create(seed).Color();
        ColorProvider second = Fake.Create(seed).Color();
        ColorRecord[] empty = first.PaletteFromCount(0);
        ColorRecord[] defaultPalette = first.Palette();
        ColorRecord[] sameDefault = second.PaletteFromCount(0);
        sameDefault = second.Palette();
        ColorRecord[] complete = first.PaletteFromCount(16);
        ColorRecord[] sameComplete = second.PaletteFromCount(16);

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(defaultPalette, Has.Length.EqualTo(5));
            Assert.That(complete, Has.Length.EqualTo(16));
        });
        AssertPalette(defaultPalette, sameDefault);
        AssertPalette(complete, sameComplete);
    }

    [Test]
    public void LimitsAndInvalidInputsFailBeforeColorEntropy()
    {
        const long seed = 12206;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 2;
        ColorProvider tinyInspected = Fake.Create(tinyOptions).Color();
        ColorProvider tinyUntouched = Fake.Create(tinyOptions).Color();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 15;
        ColorProvider countInspected = Fake.Create(countOptions).Color();
        ColorProvider countUntouched = Fake.Create(countOptions).Color();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Name(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Hex(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Rgb(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Hsl(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Hwb(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Cmyk(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Lab(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Lch(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Css(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Palette(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Name(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.PaletteFromCount(1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.PaletteFromCount(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.PaletteFromCount(17),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.RgbFromHex("bad"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void ColorGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12207);
        Fake untouched = Fake.Create(12207);

        int index = 0;
        while (index < 32)
        {
            RgbColor rgb = inspected.Color().Rgb();
            inspected.Color().Name();
            inspected.Color().Hex();
            inspected.Color().HexFromValue(rgb);
            inspected.Color().Hsl();
            inspected.Color().HslFromValue(rgb);
            inspected.Color().Hwb();
            inspected.Color().HwbFromValue(rgb);
            inspected.Color().Cmyk();
            inspected.Color().CmykFromValue(rgb);
            inspected.Color().Lab();
            inspected.Color().LabFromValue(rgb);
            inspected.Color().Lch();
            inspected.Color().LchFromValue(rgb);
            inspected.Color().Css();
            inspected.Color().CssFromValue(rgb);
            inspected.Color().Record();
            inspected.Color().Palette();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertPalette(ColorRecord[] values, ColorRecord[] same)
    {
        HashSet<string> names = new HashSet<string>();
        int index = 0;
        while (index < values.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(names.Add(values[index].Name), Is.True);
                Assert.That(values[index].Name, Is.EqualTo(same[index].Name));
                Assert.That(values[index].Hex,
                    Is.EqualTo(NamedHex[values[index].Name]));
                Assert.That(values[index].IsSynthetic, Is.True);
                if (index > 0)
                {
                    Assert.That(ReferenceEquals(
                        values[index - 1],
                        values[index]), Is.False);
                    Assert.That(ReferenceEquals(
                        values[index - 1].Rgb,
                        values[index].Rgb), Is.False);
                }
            });
            index++;
        }
    }
}
