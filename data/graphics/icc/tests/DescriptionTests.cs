using icc;

namespace icc.Tests;

public class DescriptionTests
{
    [Test]
    public void ProfileSummaryUsesLocalizedNameAndHumanReadableHeaderDescriptions()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateDisplayProfile();

        string summary = inspector.SummarizeProfile(profile);

        Assert.That(summary, Is.EqualTo(
            "\"Studio Display\" — ICC.1 v4 (4.4.0); Display device profile; RGB; 3 channels (Red, Green, Blue); "
            + "FourCC ('RGB ', 0x52474220); CIE XYZ PCS; FourCC ('XYZ ', 0x58595A20); "
            + "Media-relative colorimetric; 2 tags"));
    }

    [Test]
    public void LocalizedTagInspectionRetainsLocalesAndAppliesDeterministicFallback()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccTag tag = CreateLocalizedDescriptionTag();

        string all = inspector.InspectTagText(tag, IccInspectionDetail.Normal, 10);
        string bounded = inspector.InspectTagText(tag, IccInspectionDetail.Normal, 1);

        Assert.Multiple(() =>
        {
            Assert.That(all, Is.EqualTo(
                "Profile Description ('desc', 0x64657363): localized text "
                + "[fr-CA=\"Écran\", en-GB=\"Display\", en-US=\"Studio Display\"]; preferred=\"Studio Display\""));
            Assert.That(bounded, Does.Contain("fr-CA=\"Écran\", … 2 more"));
            Assert.That(bounded, Does.EndWith("preferred=\"Studio Display\""));
        });
    }

    [Test]
    public void CicpTagInspectionNamesCodePointsRangeAndProfilePrimariesAmendment()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.Cicp;
        tag.Data.Kind = IccTagDataKind.Cicp;
        tag.Data.Cicp = new IccCicp();
        tag.Data.Cicp.ColorPrimaries = 2;
        tag.Data.Cicp.TransferCharacteristics = 16;
        tag.Data.Cicp.MatrixCoefficients = 9;
        tag.Data.Cicp.FullRange = true;
        tag.Data.Cicp.UsesProfilePrimaries = true;

        string result = inspector.InspectTagText(tag, IccInspectionDetail.Normal, 10);

        Assert.That(result, Is.EqualTo(
            "Cicp ('cicp', 0x63696370): CICP primaries: profile RGB matrix primaries (code 2 amendment); "
            + "transfer: SMPTE ST 2084 PQ (code 16); matrix: ITU-R BT.2020 non-constant luminance (code 9); full range"));
    }

    [Test]
    public void ProfileInspectionCombinesSummaryAndBoundedHumanReadableTags()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateDisplayProfile();

        string summary = inspector.InspectProfileText(profile, IccInspectionDetail.Summary, 1);
        string normal = inspector.InspectProfileText(profile, IccInspectionDetail.Normal, 1);

        Assert.Multiple(() =>
        {
            Assert.That(summary, Does.Not.Contain("\n- "));
            Assert.That(normal, Does.Contain("\n- Profile Description ('desc', 0x64657363)"));
            Assert.That(normal, Does.Contain("\n- Cicp ('cicp', 0x63696370)"));
        });
    }

    [Test]
    public void SignatureClassificationsUseStableNamesForKnownAndUnknownValues()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            Assert.That(describer.DescribeVersionKind(IccVersionKind.Version5), Is.EqualTo("ICC.2 v5"));
            Assert.That(describer.DescribeProfileClass(IccProfileClass.DeviceLink), Is.EqualTo("DeviceLink profile"));
            Assert.That(describer.DescribeColorSpaceKind(IccColorSpaceKind.Lab), Is.EqualTo("CIELAB"));
            Assert.That(describer.DescribePcsKind(IccPcsKind.Material), Is.EqualTo("Material Connection Space"));
            Assert.That(describer.DescribeRenderingIntent(IccRenderingIntent.IccAbsoluteColorimetric), Is.EqualTo("ICC-absolute colorimetric"));
            Assert.That(describer.DescribeProfileClass((IccProfileClass)99), Is.EqualTo("Unknown profile class (99)"));
            Assert.That(describer.DescribeRenderingIntent((IccRenderingIntent)99), Is.EqualTo("Unknown rendering intent (99)"));
        });
    }

    [Test]
    public void ExtendedColorSpaceSummaryReportsGenericChannelRangeAndFourCc()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.Version.Major = 5;
        profile.Header.ProfileClass = IccProfileClass.ColorEncodingSpace;
        profile.Header.DataColorSpace = IccColorSpaceKind.ExtendedDevice;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.ExtendedDevicePrefix | 20u;

        string result = inspector.SummarizeProfile(profile);

        Assert.That(result, Does.Contain(
            "ICC.2 extended device channels; 20 channels (Channels 1-20); FourCC (0x6E630014)"));
    }

    [Test]
    public void NamedColorInspectionFormatsStableEquivalentRepresentations()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.NamedColor2;
        tag.Data.Kind = IccTagDataKind.NamedColors;
        tag.Data.NamedColors = new IccNamedColorSet();
        IccNamedColor color = new IccNamedColor();
        color.Name = "Orange";
        color.Description.Name = "Book Orange";
        color.Description.Lab = new IccLabColor();
        color.Description.Lab.L = 50.0;
        color.Description.Lab.A = 10.0;
        color.Description.Lab.B = -20.0;
        color.Description.Lch = new IccLchColor();
        color.Description.Lch.L = 50.0;
        color.Description.Lch.C = 22.360679775;
        color.Description.Lch.HueDegrees = 296.56505118;
        tag.Data.NamedColors.Colors = new IccNamedColor[] { color };

        string result = inspector.InspectTagText(tag, IccInspectionDetail.Normal, 1);

        Assert.That(result, Is.EqualTo(
            "Named Color 2 ('ncl2', 0x6E636C32): 1 named colours: \"Book Orange\"; "
            + "Lab (50, 10, -20); LCh (50, 22.36067978, 296.5650512)"));
    }

    [Test]
    public void DescriptionOperationsRejectInvalidObjectsAndLimits()
    {
        IIccProfileInspector inspector = new IccProfileInspector();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => inspector.SummarizeProfile(null!));
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectProfileText(new IccProfile(), (IccInspectionDetail)99, 1));
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectTagText(new IccTag(), IccInspectionDetail.Normal, -1));
        });
    }

    private static IccProfile CreateDisplayProfile()
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.Version.Major = 4;
        profile.Header.Version.Minor = 4;
        profile.Header.Version.BugFix = 0;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        IccTag cicp = new IccTag();
        cicp.Signature = IccTagSignatures.Cicp;
        cicp.Data.Kind = IccTagDataKind.Cicp;
        cicp.Data.Cicp = new IccCicp();
        profile.Tags = new IccTag[] { CreateLocalizedDescriptionTag(), cicp };
        return profile;
    }

    private static IccTag CreateLocalizedDescriptionTag()
    {
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.ProfileDescription;
        tag.Data.Kind = IccTagDataKind.LocalizedText;
        tag.Data.LocalizedStrings = new IccLocalizedString[]
        {
            CreateLocalizedString("fr", "CA", "Écran"),
            CreateLocalizedString("en", "GB", "Display"),
            CreateLocalizedString("en", "US", "Studio Display")
        };
        return tag;
    }

    private static IccLocalizedString CreateLocalizedString(string language, string country, string text)
    {
        IccLocalizedString value = new IccLocalizedString();
        value.Language = language;
        value.Country = country;
        value.Text = text;
        return value;
    }
}
