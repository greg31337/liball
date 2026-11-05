using icc;

namespace icc.Tests;

public class Icc1ConformanceValidationTests
{
    [Test]
    public void EveryIcc1ClassAndDeviceModelAcceptsItsCompleteRequiredTagSet()
    {
        IccProfile inputLut = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Input, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        AddTag(inputLut, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);
        IccProfile inputMatrix = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Input, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz));
        AddMatrixTags(inputMatrix);
        IccProfile inputMonochrome = CompleteMonochrome(IccProfileClass.Input);

        IccProfile displayLut = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Display, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        AddTag(displayLut, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);
        AddTag(displayLut, IccTagSignatures.BToA0, IccTypeSignatures.LutBToA);
        IccProfile displayMatrix = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz));
        AddMatrixTags(displayMatrix);
        IccProfile displayMonochrome = CompleteMonochrome(IccProfileClass.Display);

        IccProfile outputLut = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Output, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        AddOutputTags(outputLut);
        IccProfile outputMonochrome = CompleteMonochrome(IccProfileClass.Output);

        IccProfile colorSpace = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.ColorSpace, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Lab));
        AddTag(colorSpace, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);
        AddTag(colorSpace, IccTagSignatures.BToA0, IccTypeSignatures.LutBToA);
        IccProfile abstractProfile = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Abstract, IccColorSpaceSignatures.Lab,
            IccColorSpaceSignatures.Lab));
        AddTag(abstractProfile, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);
        IccProfile named = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.NamedColor, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        AddTag(named, IccTagSignatures.NamedColor2, IccTypeSignatures.NamedColor2);
        IccProfile deviceLink = CreateProfile(IccVersionKind.Version4,
            IccProfileClass.DeviceLink, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Cmyk);
        AddTag(deviceLink, IccTagSignatures.ProfileDescription,
            IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(deviceLink, IccTagSignatures.Copyright,
            IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(deviceLink, IccTagSignatures.ProfileSequenceDescription,
            IccTypeSignatures.ProfileSequenceDescription);
        AddTag(deviceLink, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);

        IccProfile[] profiles = new IccProfile[]
        {
            inputLut, inputMatrix, inputMonochrome,
            displayLut, displayMatrix, displayMonochrome,
            outputLut, outputMonochrome, colorSpace, abstractProfile, named, deviceLink
        };

        Assert.That(profiles.SelectMany(profile =>
            Icc1ConformanceValidationCore.Validate(profile)), Is.Empty);
    }

    [Test]
    public void VersionTypeAndProfileClassCompatibilityProduceStableRules()
    {
        IccProfile wrongType = CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz);
        wrongType.Tags = new IccTag[]
        {
            Tag(IccTagSignatures.Copyright, IccTypeSignatures.Text, 144)
        };
        IccProfile unavailable = CreateProfile(IccVersionKind.Version2,
            IccProfileClass.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz);
        unavailable.Tags = new IccTag[]
        {
            Tag(IccTagSignatures.Metadata, IccTypeSignatures.Dictionary, 144)
        };
        IccProfile wrongClass = CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Output, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab);
        wrongClass.Tags = new IccTag[]
        {
            Tag(IccTagSignatures.RedMatrixColumn, IccTypeSignatures.Xyz, 200)
        };

        IccValidationIssue[] wrongTypeIssues =
            Icc1ConformanceValidationCore.Validate(wrongType);
        IccValidationIssue[] unavailableIssues =
            Icc1ConformanceValidationCore.Validate(unavailable);
        IccValidationIssue[] wrongClassIssues =
            Icc1ConformanceValidationCore.Validate(wrongClass);

        Assert.Multiple(() =>
        {
            Assert.That(Find(wrongTypeIssues, IccValidationRuleIds.TagTypeCompatibility).Message,
                Does.Contain("version 4").And.Contain("'mluc'"));
            Assert.That(Find(unavailableIssues, IccValidationRuleIds.ForbiddenTag).Message,
                Does.Contain("not defined").And.Contain("version 2"));
            IccValidationIssue classIssue = Find(wrongClassIssues,
                IccValidationRuleIds.ForbiddenTag);
            Assert.That(classIssue.Message, Does.Contain("Output profile class"));
            Assert.That(classIssue.Path, Is.EqualTo("Tags[0]"));
            Assert.That(classIssue.Location.Offset, Is.EqualTo(200));
        });
    }

    [Test]
    public void DeviceModelsSelectLutMatrixAndMonochromeRequirements()
    {
        IccProfile inputLut = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Input, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        IccProfile displayLut = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Display, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz));
        IccProfile inputMatrix = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Input, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz));
        AddTag(inputMatrix, IccTagSignatures.RedMatrixColumn, IccTypeSignatures.Xyz);
        IccProfile displayMonochrome = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Display, IccColorSpaceSignatures.Gray,
            IccColorSpaceSignatures.Xyz));

        IccValidationIssue[] inputLutIssues =
            Icc1ConformanceValidationCore.Validate(inputLut);
        IccValidationIssue[] displayLutIssues =
            Icc1ConformanceValidationCore.Validate(displayLut);
        IccValidationIssue[] matrixIssues =
            Icc1ConformanceValidationCore.Validate(inputMatrix);
        IccValidationIssue[] monochromeIssues =
            Icc1ConformanceValidationCore.Validate(displayMonochrome);

        Assert.Multiple(() =>
        {
            AssertRequired(inputLutIssues, IccTagSignatures.AToB0);
            AssertRequired(displayLutIssues, IccTagSignatures.AToB0);
            AssertRequired(displayLutIssues, IccTagSignatures.BToA0);
            Assert.That(matrixIssues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(5));
            AssertRequired(matrixIssues, IccTagSignatures.GreenMatrixColumn);
            AssertRequired(matrixIssues, IccTagSignatures.RedTrc);
            AssertRequired(monochromeIssues, IccTagSignatures.GrayTrc);
        });
    }

    [Test]
    public void OutputAndNonDeviceClassMatricesRequireTheirCompleteModels()
    {
        IccProfile output = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Output, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));
        IccProfile colorSpace = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.ColorSpace, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Lab));
        IccProfile abstractProfile = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Abstract, IccColorSpaceSignatures.Lab,
            IccColorSpaceSignatures.Lab));
        IccProfile named = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.NamedColor, IccColorSpaceSignatures.Cmyk,
            IccColorSpaceSignatures.Lab));

        IccValidationIssue[] outputIssues = Icc1ConformanceValidationCore.Validate(output);
        IccValidationIssue[] colorSpaceIssues =
            Icc1ConformanceValidationCore.Validate(colorSpace);
        IccValidationIssue[] abstractIssues =
            Icc1ConformanceValidationCore.Validate(abstractProfile);
        IccValidationIssue[] namedIssues = Icc1ConformanceValidationCore.Validate(named);

        Assert.Multiple(() =>
        {
            Assert.That(outputIssues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(7));
            AssertRequired(outputIssues, IccTagSignatures.Gamut);
            AssertRequired(colorSpaceIssues, IccTagSignatures.AToB0);
            AssertRequired(colorSpaceIssues, IccTagSignatures.BToA0);
            AssertRequired(abstractIssues, IccTagSignatures.AToB0);
            AssertRequired(namedIssues, IccTagSignatures.NamedColor2);
        });
    }

    [Test]
    public void DeviceLinkAndXclrModelsRequireSequenceAndColorantTables()
    {
        IccProfile deviceLink = CreateProfile(IccVersionKind.Version4,
            IccProfileClass.DeviceLink, IccColorSpaceSignatures.FiveColor,
            IccColorSpaceSignatures.FourColor);
        AddTag(deviceLink, IccTagSignatures.ProfileDescription,
            IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(deviceLink, IccTagSignatures.Copyright,
            IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(deviceLink, IccTagSignatures.ProfileSequenceDescription,
            IccTypeSignatures.ProfileSequenceDescription);
        AddTag(deviceLink, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);

        IccValidationIssue[] issues = Icc1ConformanceValidationCore.Validate(deviceLink);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(2));
            AssertRequired(issues, IccTagSignatures.ColorantTable);
            AssertRequired(issues, IccTagSignatures.ColorantTableOut);
            Assert.That(issues.Any(issue => issue.Path.Contains("'wtpt'")), Is.False);
        });
    }

    [Test]
    public void VersionFourMpeTagsFollowClassMatrixAndDoNotReplaceRequiredLegacyModel()
    {
        IccProfile abstractProfile = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            IccProfileClass.Abstract, IccColorSpaceSignatures.Lab,
            IccColorSpaceSignatures.Lab));
        AddTag(abstractProfile, IccTagSignatures.DToB0,
            IccTypeSignatures.MultiProcessElements);
        AddTag(abstractProfile, IccTagSignatures.DToB1,
            IccTypeSignatures.MultiProcessElements);
        IccProfile versionTwo = WithCommonTags(CreateProfile(IccVersionKind.Version2,
            IccProfileClass.Input, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz));
        AddTag(versionTwo, IccTagSignatures.DToB0,
            IccTypeSignatures.MultiProcessElements);

        IccValidationIssue[] abstractIssues =
            Icc1ConformanceValidationCore.Validate(abstractProfile);
        IccValidationIssue[] versionTwoIssues =
            Icc1ConformanceValidationCore.Validate(versionTwo);

        Assert.Multiple(() =>
        {
            Assert.That(abstractIssues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag && issue.Message.Contains("'D2B0'")),
                Is.False);
            Assert.That(abstractIssues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag && issue.Message.Contains("'D2B1'")),
                Is.True);
            AssertRequired(abstractIssues, IccTagSignatures.AToB0);
            Assert.That(versionTwoIssues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag && issue.Message.Contains("version 2")),
                Is.True);
        });
    }

    private static IccProfile CreateProfile(IccVersionKind version, IccProfileClass profileClass,
        uint dataColorSpace, uint connectionSpace)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version = HeaderCodec.DecodeVersion(version == IccVersionKind.Version2
            ? 0x02100000u : 0x04400000u);
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpaceSignature = connectionSpace;
        return profile;
    }

    private static IccProfile WithCommonTags(IccProfile profile)
    {
        AddTag(profile, IccTagSignatures.ProfileDescription,
            profile.Header.Version.Kind == IccVersionKind.Version2
                ? IccTypeSignatures.TextDescription
                : IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(profile, IccTagSignatures.Copyright,
            profile.Header.Version.Kind == IccVersionKind.Version2
                ? IccTypeSignatures.Text : IccTypeSignatures.MultiLocalizedUnicode);
        AddTag(profile, IccTagSignatures.MediaWhitePoint, IccTypeSignatures.Xyz);
        return profile;
    }

    private static IccProfile CompleteMonochrome(IccProfileClass profileClass)
    {
        IccProfile profile = WithCommonTags(CreateProfile(IccVersionKind.Version4,
            profileClass, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz));
        AddTag(profile, IccTagSignatures.GrayTrc, IccTypeSignatures.Curve);
        return profile;
    }

    private static void AddMatrixTags(IccProfile profile)
    {
        AddTag(profile, IccTagSignatures.RedMatrixColumn, IccTypeSignatures.Xyz);
        AddTag(profile, IccTagSignatures.GreenMatrixColumn, IccTypeSignatures.Xyz);
        AddTag(profile, IccTagSignatures.BlueMatrixColumn, IccTypeSignatures.Xyz);
        AddTag(profile, IccTagSignatures.RedTrc, IccTypeSignatures.Curve);
        AddTag(profile, IccTagSignatures.GreenTrc, IccTypeSignatures.Curve);
        AddTag(profile, IccTagSignatures.BlueTrc, IccTypeSignatures.Curve);
    }

    private static void AddOutputTags(IccProfile profile)
    {
        AddTag(profile, IccTagSignatures.AToB0, IccTypeSignatures.LutAToB);
        AddTag(profile, IccTagSignatures.AToB1, IccTypeSignatures.LutAToB);
        AddTag(profile, IccTagSignatures.AToB2, IccTypeSignatures.LutAToB);
        AddTag(profile, IccTagSignatures.BToA0, IccTypeSignatures.LutBToA);
        AddTag(profile, IccTagSignatures.BToA1, IccTypeSignatures.LutBToA);
        AddTag(profile, IccTagSignatures.BToA2, IccTypeSignatures.LutBToA);
        AddTag(profile, IccTagSignatures.Gamut, IccTypeSignatures.LutBToA);
    }

    private static void AddTag(IccProfile profile, uint signature, uint typeSignature)
    {
        IccTag[] expanded = new IccTag[profile.Tags.Length + 1];
        Array.Copy(profile.Tags, expanded, profile.Tags.Length);
        expanded[expanded.Length - 1] = Tag(signature, typeSignature,
            144 + (expanded.Length * 16));
        profile.Tags = expanded;
    }

    private static IccTag Tag(uint signature, uint typeSignature, int offset)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.TableEntry.Offset = offset;
        tag.TableEntry.Size = 8;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = typeSignature;
        return tag;
    }

    private static void AssertRequired(IccValidationIssue[] issues, uint signature)
    {
        string formatted = FourCcCodec.Format(signature);
        Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.RequiredTag
            && issue.Message.Contains($"'{formatted}'")), Is.True);
    }

    private static IccValidationIssue Find(IccValidationIssue[] issues, string ruleId)
    {
        return issues.First(issue => issue.RuleId == ruleId);
    }
}
