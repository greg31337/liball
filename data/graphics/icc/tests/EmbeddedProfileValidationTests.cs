using icc;

namespace icc.Tests;

public class EmbeddedProfileValidationTests
{
    [Test]
    public void ValidIcc1AndIcc2ReplacementHasNoEmbeddedProfileIssue()
    {
        IccProfile profile = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);

        IccValidationReport report = Validate(profile);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.EmbeddedProfile), Is.False);
    }

    [Test]
    public void EmbeddedProfileFlagsRequireEmbeddedAndForbidDataDependence()
    {
        IccProfile missingFlag = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccProfile dependent = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccProfile missingChild = GetEmbedded(missingFlag);
        missingChild.Header.ProfileFlags = 0;
        missingChild.Header.IsEmbedded = false;
        WriteUInt32(missingChild.OriginalBytes, 44, 0);
        IccProfile dependentChild = GetEmbedded(dependent);
        dependentChild.Header.ProfileFlags = IccConstants.EmbeddedProfileFlag
            | IccConstants.UseIndependentOfDataFlag;
        dependentChild.Header.RequiresEmbeddedData = true;
        WriteUInt32(dependentChild.OriginalBytes, 44,
            dependentChild.Header.ProfileFlags);

        IccValidationReport missingReport = Validate(missingFlag);
        IccValidationReport dependentReport = Validate(dependent);

        Assert.Multiple(() =>
        {
            Assert.That(missingReport.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("flag bit 0")), Is.True);
            Assert.That(dependentReport.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("flag bit 1")), Is.True);
        });
    }

    [Test]
    public void ContainerMustBeIcc1AndReplacementMustBeSupportedIcc2()
    {
        IccProfile wrongOuter = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        wrongOuter.Header.Version = HeaderCodec.DecodeVersion(0x05000000);
        IccProfile wrongChild = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccProfile child = GetEmbedded(wrongChild);
        child.Header.Version = HeaderCodec.DecodeVersion(0x04400000);
        WriteUInt32(child.OriginalBytes, 8, 0x04400000);

        IccValidationReport outerReport = Validate(wrongOuter);
        IccValidationReport childReport = Validate(wrongChild);

        Assert.Multiple(() =>
        {
            Assert.That(outerReport.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("ICC.1 version 2 or version 4")), Is.True);
            Assert.That(childReport.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("ICC.2 version 5")), Is.True);
        });
    }

    [Test]
    public void Icc2OnlyProfileClassesCannotReplaceAnIcc1Profile()
    {
        IccProfile profile = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccProfile child = GetEmbedded(profile);
        child.Header.ProfileClass = IccProfileClass.ColorEncodingSpace;
        child.Header.ProfileClassSignature = IccProfileClassSignatures.ColorEncodingSpace;
        WriteUInt32(child.OriginalBytes, 12,
            IccProfileClassSignatures.ColorEncodingSpace);

        IccValidationReport report = Validate(profile);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.EmbeddedProfile
            && issue.Message.Contains("not representable")), Is.True);
    }

    [Test]
    public void ReplacementClassAndDeviceSpaceMustEqualTheFallback()
    {
        IccProfile profile = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccProfile child = GetEmbedded(profile);
        child.Header.ProfileClass = IccProfileClass.Output;
        child.Header.ProfileClassSignature = IccProfileClassSignatures.Output;
        child.Header.DataColorSpace = IccColorSpaceKind.Cmyk;
        child.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        WriteUInt32(child.OriginalBytes, 12, IccProfileClassSignatures.Output);
        WriteUInt32(child.OriginalBytes, 16, IccColorSpaceSignatures.Cmyk);

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("profile class does not equal")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("device colour space does not equal")), Is.True);
        });
    }

    [Test]
    public void ContainerSizeLocationAndTrailerMustDescribeExactlyOneProfile()
    {
        IccProfile profile = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        IccTag tag = profile.Tags[0];
        IccProfile child = GetEmbedded(profile);
        tag.TableEntry.Size = tag.TableEntry.Size + 4;
        child.Location.Length = child.Location.Length - 4;
        child.TrailingBytes = new byte[] { 1 };

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Message.Contains("8-byte type header")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Path.EndsWith("Location")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.EmbeddedProfile
                && issue.Path.EndsWith("TrailingBytes")), Is.True);
        });
    }

    [Test]
    public void RetainedPayloadMustContainTheExactDecodedProfileBytes()
    {
        IccProfile profile = CreateDecodedOuter(0x05000000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        profile.Tags[0].Data.RawData.Bytes[12] = 9;

        IccValidationReport report = Validate(profile);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.EmbeddedProfile
            && issue.Message.Contains("in its entirety")), Is.True);
    }

    [Test]
    public void EmbeddedDecodeFailuresUseTheStableEmbeddedProfileRule()
    {
        byte[] child = CreateProfile(0x04400000,
            IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb,
            IccConstants.EmbeddedProfileFlag);
        byte[] outer = CreateOuter(CreatePayload(child));
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = false;

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileBytesReport(outer, options);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.EmbeddedProfile
            && issue.Message.Contains("version 5")), Is.True);
    }

    private static IccValidationReport Validate(IccProfile profile)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = IccValidationMode.Conforming;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return new IccProfileValidator().ValidateProfileDataReport(profile, policy);
    }

    private static IccProfile GetEmbedded(IccProfile outer)
    {
        return outer.Tags[0].Data.EmbeddedProfile!;
    }

    private static IccProfile CreateDecodedOuter(uint childVersion, uint childClass,
        uint childSpace, uint childFlags)
    {
        byte[] child = CreateProfile(childVersion, childClass, childSpace, childFlags);
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = false;
        return ProfileCodec.ReadEnvelope(CreateOuter(CreatePayload(child)), 0, 0,
            options);
    }

    private static byte[] CreateProfile(uint version, uint profileClass,
        uint dataColorSpace, uint flags)
    {
        byte[] profile = new byte[IccConstants.MinimumProfileSize];
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteUInt32(profile, 8, version);
        WriteUInt32(profile, 12, profileClass);
        WriteUInt32(profile, 16, dataColorSpace);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 44, flags);
        return profile;
    }

    private static byte[] CreatePayload(byte[] child)
    {
        byte[] payload = new byte[EmbeddedProfileTagCodec.TypeHeaderSize + child.Length];
        WriteUInt32(payload, 0, IccTypeSignatures.EmbeddedProfile);
        Array.Copy(child, 0, payload, EmbeddedProfileTagCodec.TypeHeaderSize,
            child.Length);
        return payload;
    }

    private static byte[] CreateOuter(byte[] payload)
    {
        int size = 144 + payload.Length;
        byte[] profile = new byte[size];
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteUInt32(profile, 8, 0x04400000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Display);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.EmbeddedIccMaxProfile);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
