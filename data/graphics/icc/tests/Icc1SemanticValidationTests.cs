using icc;

namespace icc.Tests;

public class Icc1SemanticValidationTests
{
    [Test]
    public void TransformDirectionsAndIntentRolesUseHeaderEndpointChannels()
    {
        IccProfile profile = CreateProfile(IccProfileClass.Output,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag forward = Tag(IccTagSignatures.AToB0);
        forward.Data.Lut = new IccLut { InputChannels = 4, OutputChannels = 3 };
        IccTag reverse = Tag(IccTagSignatures.BToA1);
        reverse.Data.MultiStageLut = new IccMultiStageLut
        {
            InputChannels = 3,
            OutputChannels = 4
        };
        IccTag gamut = Tag(IccTagSignatures.Gamut);
        gamut.Data.MultiProcessElements = new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 2
        };
        IccTag valid = Tag(IccTagSignatures.AToB2);
        valid.Data.Lut = new IccLut { InputChannels = 3, OutputChannels = 3 };
        profile.Tags = new IccTag[] { forward, reverse, gamut, valid };

        IccValidationIssue[] issues = Icc1SemanticValidationCore.Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId == IccValidationRuleIds.ChannelCount),
                Is.EqualTo(3));
            Assert.That(issues.Any(issue => issue.Message.Contains("rendering-intent index 0")
                && issue.Message.Contains("3-to-3")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("rendering-intent index 1")),
                Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("Gamut")
                && issue.Message.Contains("3-to-1")), Is.True);
            Assert.That(issues.Any(issue => issue.Path == "Tags[3].Data"), Is.False);
        });
    }

    [Test]
    public void AssociatedColorantAndMeasurementCountsFollowHeaderSpaces()
    {
        IccProfile profile = CreateProfile(IccProfileClass.Output,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab);
        IccTag order = Tag(IccTagSignatures.ColorantOrder);
        order.Data.IntegerValues = new long[] { 0, 1, 1, 3 };
        IccTag table = Tag(IccTagSignatures.ColorantTable);
        table.Data.Colorants = CreateColorants(IccColorSpaceSignatures.Rgb, 3,
            IccColorSpaceSignatures.Lab);
        IccTag named = Tag(IccTagSignatures.NamedColor2);
        named.Data.NamedColors = new IccNamedColorSet { DeviceChannelCount = 3 };
        IccTag chromaticity = Tag(IccTagSignatures.Chromaticity);
        chromaticity.Data.Chromaticity = new IccChromaticity
        {
            XCoordinates = new double[3],
            YCoordinates = new double[3]
        };
        IccTag response = Tag(IccTagSignatures.OutputResponse);
        response.Data.ResponseCurveChannelCount = 3;
        profile.Tags = new IccTag[] { order, table, named, chromaticity, response };

        IccValidationIssue[] issues = Icc1SemanticValidationCore.Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId == IccValidationRuleIds.ChannelCount),
                Is.EqualTo(5));
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.IntegerValues")), Is.True);
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.Colorants")), Is.True);
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.NamedColors")), Is.True);
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.Chromaticity")), Is.True);
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.ResponseCurveChannelCount")),
                Is.True);
        });
    }

    [Test]
    public void MatchingColorantAndNamedColorModelsAreAccepted()
    {
        IccProfile profile = CreateProfile(IccProfileClass.Output,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab);
        IccTag order = Tag(IccTagSignatures.ColorantOrder);
        order.Data.IntegerValues = new long[] { 2, 0, 3, 1 };
        IccTag table = Tag(IccTagSignatures.ColorantTable);
        table.Data.Colorants = CreateColorants(IccColorSpaceSignatures.Cmyk, 4,
            IccColorSpaceSignatures.Lab);
        IccTag named = Tag(IccTagSignatures.NamedColor2);
        IccNamedColor color = new IccNamedColor();
        color.PcsColor = Vector(IccColorSpaceSignatures.Lab, 3);
        color.DeviceColor = Vector(IccColorSpaceSignatures.Cmyk, 4);
        named.Data.NamedColors = new IccNamedColorSet
        {
            DeviceChannelCount = 4,
            Colors = new IccNamedColor[] { color }
        };
        profile.Tags = new IccTag[] { order, table, named };

        IccValidationIssue[] issues = Icc1SemanticValidationCore.Validate(profile);

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void MediaWhiteAndChromaticAdaptationDependenciesAreValidated()
    {
        IccProfile display = CreateProfile(IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag white = Tag(IccTagSignatures.MediaWhitePoint);
        white.Data.XyzValues = new IccXyzNumber[]
        {
            new IccXyzNumber { X = 0.95, Y = 1.0, Z = 1.09 }
        };
        display.Tags = new IccTag[] { white, Tag(IccTagSignatures.ChromaticAdaptation) };
        IccProfile link = CreateProfile(IccProfileClass.DeviceLink,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk);
        link.Tags = new IccTag[] { Tag(IccTagSignatures.ChromaticAdaptation) };

        IccValidationIssue[] displayIssues = Icc1SemanticValidationCore.Validate(display);
        IccValidationIssue[] linkIssues = Icc1SemanticValidationCore.Validate(link);

        Assert.Multiple(() =>
        {
            Assert.That(displayIssues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("D50")), Is.True);
            Assert.That(linkIssues.Any(issue => issue.RuleId == IccValidationRuleIds.RequiredTag
                && issue.Path == "Tags['wtpt']"), Is.True);
        });
    }

    [Test]
    public void ImageStateRequiresViewingAndHardcopyAdaptationAndLuminanceMustAgree()
    {
        IccProfile incomplete = CreateProfile(IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag image = Tag(IccTagSignatures.ColorimetricIntentImageState);
        image.Data.Signature = IccImageStateSignatures.ReflectionHardcopyOriginalColorimetry;
        incomplete.Tags = new IccTag[] { image };

        IccProfile inconsistent = CreateProfile(IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag viewing = Tag(IccTagSignatures.ViewingConditions);
        viewing.Data.ViewingConditions = new IccViewingConditions();
        viewing.Data.ViewingConditions.Illuminant.Y = 120.0;
        IccTag luminance = Tag(IccTagSignatures.Luminance);
        luminance.Data.XyzValues = new IccXyzNumber[]
        {
            new IccXyzNumber { X = 1.0, Y = 100.0, Z = 1.0 }
        };
        inconsistent.Tags = new IccTag[]
        {
            viewing, luminance, Tag(IccTagSignatures.ChromaticAdaptation)
        };

        IccValidationIssue[] missingIssues = Icc1SemanticValidationCore.Validate(incomplete);
        IccValidationIssue[] inconsistentIssues =
            Icc1SemanticValidationCore.Validate(inconsistent);

        Assert.Multiple(() =>
        {
            Assert.That(missingIssues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(2));
            Assert.That(missingIssues.Any(issue => issue.Path == "Tags['view']"), Is.True);
            Assert.That(missingIssues.Any(issue => issue.Path == "Tags['chad']"), Is.True);
            Assert.That(inconsistentIssues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("absolute Y luminance")), Is.True);
        });
    }

    [Test]
    public void CicpContextProfilePrimariesAndMetadataRegistrationAreConsistent()
    {
        IccProfile profile = CreateProfile(IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag cicp = Tag(IccTagSignatures.Cicp);
        cicp.Data.Cicp = new IccCicp
        {
            ColorPrimaries = 2,
            TransferCharacteristics = 1,
            MatrixCoefficients = 0,
            UsesProfilePrimaries = false
        };
        IccTag metadata = Tag(IccTagSignatures.Metadata);
        metadata.Data.Metadata = new IccMetadataEntry[]
        {
            new IccMetadataEntry { Name = "PrivateKey", IsRegisteredName = false }
        };
        profile.Tags = new IccTag[]
        {
            cicp,
            Tag(IccTagSignatures.RedMatrixColumn),
            Tag(IccTagSignatures.GreenMatrixColumn),
            Tag(IccTagSignatures.BlueMatrixColumn),
            metadata
        };

        IccValidationIssue[] issues = Icc1SemanticValidationCore.Validate(profile);
        cicp.Data.Cicp.UsesProfilePrimaries = true;
        metadata.Data.Metadata[0].Name = "DeviceModel";
        metadata.Data.Metadata[0].IsRegisteredName = true;
        IccValidationIssue[] corrected = Icc1SemanticValidationCore.Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics), Is.EqualTo(2));
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.Cicp")), Is.True);
            Assert.That(issues.Any(issue => issue.Path.EndsWith("Data.Metadata")), Is.True);
            Assert.That(corrected, Is.Empty);
        });
    }

    [Test]
    public void ProfileSequenceCountsAndIdentifierPresenceStayAligned()
    {
        IccProfile profile = CreateProfile(IccProfileClass.DeviceLink,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk);
        IccTag descriptions = Tag(IccTagSignatures.ProfileSequenceDescription);
        descriptions.Data.ProfileSequence = new IccProfileSequenceEntry[]
        {
            new IccProfileSequenceEntry(), new IccProfileSequenceEntry()
        };
        IccTag identifiers = Tag(IccTagSignatures.ProfileSequenceIdentifier);
        IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
        identifier.ProfileId.Bytes[4] = 1;
        identifiers.Data.ProfileSequenceIdentifiers =
            new IccProfileSequenceIdentifier[] { identifier };
        profile.Tags = new IccTag[] { descriptions, identifiers };

        IccValidationIssue[] issues = Icc1SemanticValidationCore.Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("2 descriptions but 1 identifiers")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.HeaderProfileId
                && issue.Path.Contains("[0].ProfileId")), Is.True);
        });
    }

    [Test]
    public void NormalValidationReportIncludesCrossTagSemanticIssues()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateProfile(IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccTag image = Tag(IccTagSignatures.ColorimetricIntentImageState);
        image.Data.Signature = IccImageStateSignatures.SceneColorimetryEstimates;
        profile.Tags = new IccTag[] { image };
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[]
        {
            IccValidationRuleIds.HeaderProfileSize,
            IccValidationRuleIds.HeaderProfileClass,
            IccValidationRuleIds.HeaderFileSignature,
            IccValidationRuleIds.HeaderDateTime,
            IccValidationRuleIds.HeaderColorSpace,
            IccValidationRuleIds.HeaderPcs,
            IccValidationRuleIds.HeaderPcsIlluminant,
            IccValidationRuleIds.TagTableCount,
            IccValidationRuleIds.TagTableBounds,
            IccValidationRuleIds.TagSize,
            IccValidationRuleIds.TagTypeCompatibility
        };

        IccValidationReport report = validator.ValidateProfileDataReport(profile, policy);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.ProfileClassSemantics
            || issue.Path == "Tags['view']"), Is.True);
    }

    private static IccProfile CreateProfile(IccProfileClass profileClass,
        uint dataColorSpace, uint connectionSpace)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version = HeaderCodec.DecodeVersion(0x04400000u);
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.DataColorSpace = HeaderCodec.DecodeColorSpace(dataColorSpace);
        profile.Header.ProfileConnectionSpaceSignature = connectionSpace;
        profile.Header.ProfileConnectionSpace = HeaderCodec.DecodePcs(connectionSpace);
        return profile;
    }

    private static IccTag Tag(uint signature)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.TableEntry.Offset = 144;
        tag.TableEntry.Size = 16;
        return tag;
    }

    private static IccColorantSet CreateColorants(uint associatedSignature, int count,
        uint pcsSignature)
    {
        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = associatedSignature;
        set.Colorants = new IccColorant[count];
        int index = 0;
        while (index < count)
        {
            IccColorant colorant = new IccColorant();
            colorant.PcsColor = Vector(pcsSignature, 3);
            set.Colorants[index] = colorant;
            index = index + 1;
        }
        return set;
    }

    private static IccColorVector Vector(uint signature, int count)
    {
        IccColorVector vector = new IccColorVector();
        vector.ColorSpaceSignature = signature;
        vector.ColorSpace = HeaderCodec.DecodeColorSpace(signature);
        vector.Components = new double[count];
        return vector;
    }
}
