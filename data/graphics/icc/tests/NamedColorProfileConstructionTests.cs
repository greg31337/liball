using icc;

namespace icc.Tests;

public class NamedColorProfileConstructionTests
{
    [Test]
    public void BuildsVersionFourLabProfileWithDeviceCoordinatesColorantsAndLocalizedDescriptions()
    {
        IccProfileBuildOptions options = NamedColorOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "fr", Country = "FR", Text = "Nuancier spécial" },
            new IccLocalizedString { Language = "en", Country = "US", Text = "Spot colour book" }
        };
        options.NamedColorDefinition!.NamedColors = new IccNamedColorSet
        {
            VendorFlags = 0x12340000,
            Prefix = "Book ",
            Suffix = " 100%",
            DeviceChannelCount = 4,
            Colors = new IccNamedColor[]
            {
                NamedColor("Orange", IccColorSpaceSignatures.Lab,
                    new double[] { 65.0, 35.0, 55.0 },
                    IccColorSpaceSignatures.Cmyk,
                    new double[] { 0.0, 0.55, 1.0, 0.0 }),
                NamedColor("Blue", IccColorSpaceSignatures.Lab,
                    new double[] { 40.0, 10.0, -50.0 },
                    IccColorSpaceSignatures.Cmyk,
                    new double[] { 1.0, 0.7, 0.0, 0.2 })
            }
        };
        options.NamedColorDefinition.Colorants = CmykColorants();
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateNamedColorProfile(options);
        IccProfile repeated = builder.CreateNamedColorProfile(options);
        options.NamedColorDefinition.NamedColors.Colors[0].Name = "Changed";
        options.NamedColorDefinition.Colorants.Colorants[0].Name = "Changed";
        IccTag names = FindTag(profile, IccTagSignatures.NamedColor2);
        IccTag colorants = FindTag(profile, IccTagSignatures.ColorantTable);
        IccTag description = FindTag(profile, IccTagSignatures.ProfileDescription);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.NamedColor));
            Assert.That(profile.Header.DataColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Cmyk));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Lab));
            Assert.That(names.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.NamedColor2));
            Assert.That(names.Data.NamedColors!.Prefix, Is.EqualTo("Book "));
            Assert.That(names.Data.NamedColors.Suffix, Is.EqualTo(" 100%"));
            Assert.That(names.Data.NamedColors.VendorFlags, Is.EqualTo(0x12340000));
            Assert.That(names.Data.NamedColors.DeviceChannelCount, Is.EqualTo(4));
            Assert.That(names.Data.NamedColors.Colors.Select(color => color.Name),
                Is.EqualTo(new[] { "Orange", "Blue" }));
            Assert.That(names.Data.NamedColors.Colors[0].Description.Name,
                Is.EqualTo("Book Orange 100%"));
            Assert.That(names.Data.NamedColors.Colors[0].PcsColor.Components,
                Is.EqualTo(new[] { 65.0, 35.0, 55.0 }).Within(0.01));
            Assert.That(names.Data.NamedColors.Colors[0].DeviceColor.Components,
                Is.EqualTo(new[] { 0.0, 0.55, 1.0, 0.0 }).Within(0.00002));
            Assert.That(colorants.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.ColorantTable));
            Assert.That(colorants.Data.Colorants!.ColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Cmyk));
            Assert.That(colorants.Data.Colorants.Colorants.Select(colorant => colorant.Name),
                Is.EqualTo(new[] { "Cyan", "Magenta", "Yellow", "Black" }));
            Assert.That(description.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(description.Data.LocalizedStrings.Select(value => value.Text),
                Is.EquivalentTo(new[] { "Nuancier spécial", "Spot colour book" }));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.NamedColor),
                Is.False);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsVersionTwoXyzProfileWithOptionalLegacyTintsAndNoDeviceCoordinates()
    {
        IccProfileBuildOptions options = NamedColorOptions(IccVersionKind.Version2,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Text = "Legacy named colours" }
        };
        options.Copyright.Text = "Copyright example";
        options.NamedColorDefinition!.IncludeLegacyTints = true;
        options.NamedColorDefinition.NamedColors = new IccNamedColorSet
        {
            Prefix = "Ink ",
            Suffix = " swatch",
            DeviceChannelCount = 0,
            Colors = new IccNamedColor[]
            {
                NamedColorWithTints("Paper",
                    new double[]
                    {
                        IccStandardColorimetry.D50X,
                        IccStandardColorimetry.D50Y,
                        IccStandardColorimetry.D50Z
                    },
                    new double[] { 0.0, 128.0 / 255.0, 1.0 })
            }
        };

        IccProfile profile = new IccProfileBuilder().CreateNamedColorProfile(options);
        IccTag modern = FindTag(profile, IccTagSignatures.NamedColor2);
        IccTag legacy = FindTag(profile, IccTagSignatures.NamedColor);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(modern.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.NamedColor2));
            Assert.That(modern.Data.NamedColors!.DeviceChannelCount, Is.Zero);
            Assert.That(modern.Data.NamedColors.Colors[0].DeviceColor.Components, Is.Empty);
            Assert.That(modern.Data.NamedColors.Colors[0].Description.Lab!.L,
                Is.EqualTo(100.0).Within(0.01));
            Assert.That(legacy.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.NamedColor));
            Assert.That(legacy.Data.NamedColors!.DeviceChannelCount, Is.EqualTo(3));
            Assert.That(legacy.Data.NamedColors.Colors[0].Tints,
                Is.EqualTo(new[] { 0.0, 128.0 / 255.0, 1.0 }));
            Assert.That(legacy.Data.NamedColors.Colors[0].Description.Name,
                Is.EqualTo("Ink Paper swatch"));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(FindTag(profile, IccTagSignatures.Copyright)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsIncompleteInconsistentAndGenerationInappropriateDefinitions()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions missing = NamedColorOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);
        missing.NamedColorDefinition = null;
        IccProfileBuildOptions wrongClass = ValidOptions();
        wrongClass.ProfileClass = IccProfileClass.Display;
        IccProfileBuildOptions empty = NamedColorOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);
        IccProfileBuildOptions mismatchedDevice = ValidOptions();
        mismatchedDevice.NamedColorDefinition!.NamedColors.DeviceChannelCount = 4;
        IccProfileBuildOptions duplicate = ValidOptions();
        duplicate.NamedColorDefinition!.NamedColors.Colors = new IccNamedColor[]
        {
            NamedColor("Spot", IccColorSpaceSignatures.Lab,
                new double[] { 50.0, 0.0, 0.0 }, IccColorSpaceSignatures.Rgb,
                new double[] { 1.0, 0.0, 0.0 }),
            NamedColor("Spot", IccColorSpaceSignatures.Lab,
                new double[] { 60.0, 0.0, 0.0 }, IccColorSpaceSignatures.Rgb,
                new double[] { 0.0, 1.0, 0.0 })
        };
        IccProfileBuildOptions versionFourTints = ValidOptions();
        versionFourTints.NamedColorDefinition!.IncludeLegacyTints = true;
        versionFourTints.NamedColorDefinition.NamedColors.Colors[0].Tints =
            new double[] { 1.0, 0.0, 0.0 };
        IccProfileBuildOptions implicitTints = ValidOptions();
        implicitTints.NamedColorDefinition!.NamedColors.Colors[0].Tints =
            new double[] { 1.0, 0.0, 0.0 };
        IccProfileBuildOptions wrongColorants = ValidOptions();
        wrongColorants.NamedColorDefinition!.Colorants = CmykColorants();
        IccProfileBuildOptions tintZero = ValidOptions();
        tintZero.NamedColorDefinition!.NamedColors.TintZero = new IccNamedColor();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(missing));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(wrongClass));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(empty));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(mismatchedDevice));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(duplicate));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(versionFourTints));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(implicitTints));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(wrongColorants));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateNamedColorProfile(tintZero));
        });
    }

    private static IccProfileBuildOptions ValidOptions()
    {
        IccProfileBuildOptions options = NamedColorOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);
        options.NamedColorDefinition!.NamedColors.DeviceChannelCount = 3;
        options.NamedColorDefinition.NamedColors.Colors = new IccNamedColor[]
        {
            NamedColor("Spot", IccColorSpaceSignatures.Lab,
                new double[] { 50.0, 0.0, 0.0 }, IccColorSpaceSignatures.Rgb,
                new double[] { 1.0, 0.0, 0.0 })
        };
        return options;
    }

    private static IccProfileBuildOptions NamedColorOptions(IccVersionKind version,
        uint dataSpace, uint pcs)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.NamedColor;
        options.DataColorSpaceSignature = dataSpace;
        options.ProfileConnectionSpaceSignature = pcs;
        options.Version.Kind = version;
        options.Version.Major = version == IccVersionKind.Version2 ? 2 : 4;
        options.Version.Minor = 4;
        options.NamedColorDefinition = new IccNamedColorProfileDefinition();
        return options;
    }

    private static IccNamedColor NamedColor(string name, uint pcs,
        double[] pcsComponents, uint deviceSpace, double[] deviceComponents)
    {
        IccNamedColor color = new IccNamedColor();
        color.Name = name;
        color.PcsColor.ColorSpaceSignature = pcs;
        color.PcsColor.ColorSpace = pcs == IccColorSpaceSignatures.Lab
            ? IccColorSpaceKind.Lab : IccColorSpaceKind.Xyz;
        color.PcsColor.Components = pcsComponents;
        color.DeviceColor.ColorSpaceSignature = deviceSpace;
        color.DeviceColor.ColorSpace = deviceSpace == IccColorSpaceSignatures.Cmyk
            ? IccColorSpaceKind.Cmyk : IccColorSpaceKind.Rgb;
        color.DeviceColor.Components = deviceComponents;
        return color;
    }

    private static IccNamedColor NamedColorWithTints(string name,
        double[] pcsComponents, double[] tints)
    {
        IccNamedColor color = NamedColor(name, IccColorSpaceSignatures.Xyz,
            pcsComponents, IccColorSpaceSignatures.Rgb, new double[0]);
        color.Tints = tints;
        return color;
    }

    private static IccColorantSet CmykColorants()
    {
        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        set.Colorants = new IccColorant[]
        {
            Colorant("Cyan", new double[] { 55.0, -35.0, -50.0 }),
            Colorant("Magenta", new double[] { 50.0, 75.0, -5.0 }),
            Colorant("Yellow", new double[] { 90.0, -5.0, 90.0 }),
            Colorant("Black", new double[] { 10.0, 0.0, 0.0 })
        };
        return set;
    }

    private static IccColorant Colorant(string name, double[] components)
    {
        IccColorant colorant = new IccColorant();
        colorant.Name = name;
        colorant.PcsColor.ColorSpace = IccColorSpaceKind.Lab;
        colorant.PcsColor.ColorSpaceSignature = IccColorSpaceSignatures.Lab;
        colorant.PcsColor.Components = components;
        return colorant;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
