using icc;

namespace icc.Tests;

public class Icc2ValidationTests
{
    [Test]
    public void RegisteredTagsValidateTypeClassContainerAndDirectionalChannels()
    {
        IccProfile profile = CreateVersionFiveProfile();
        IccTag pcc = CreateTag(IccTagSignatures.DevicePcc,
            IccTagDataKind.TagStructure, IccTypeSignatures.TagStructure);
        pcc.Data.TagStructure = new IccTagStructure
        {
            StructureSignature = IccStructureSignatures.MeasurementInformation
        };
        IccTag copyright = CreateTag(IccTagSignatures.Copyright,
            IccTagDataKind.Signature, IccTypeSignatures.Signature);
        IccTag directional = CreateTag(IccTagSignatures.DirectionalAToB0,
            IccTagDataKind.MultiProcessElements, IccTypeSignatures.MultiProcessElements);
        directional.Data.MultiProcessElements = CreatePipeline(3, 3,
            CreateUnknownElement(3, 3));
        IccTag hdr = CreateTag(IccTagSignatures.HdrToSdr0,
            IccTagDataKind.MultiProcessElements, IccTypeSignatures.MultiProcessElements);
        hdr.Data.MultiProcessElements = CreatePipeline(3, 3,
            CreateUnknownElement(3, 3));
        profile.Tags = new IccTag[] { pcc, copyright, directional, hdr };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.ForbiddenTag
                && issue.Path == "Tags[0].Data"), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.IccMaxHierarchy
                && issue.Message.Contains("requires container")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.TagTypeCompatibility
                && issue.Path == "Tags[1].Data"), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.ChannelCount
                && issue.Message.Contains("7-to-3")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("extended PCS range flag")), Is.True);
        });
    }

    [Test]
    public void HierarchiesAndMpeElementsAreValidatedRecursively()
    {
        IccProfile profile = CreateVersionFiveProfile();
        IccTag hierarchy = CreateTag(0x74657374u, IccTagDataKind.TagArray,
            IccTypeSignatures.TagArray);
        IccTagData number = new IccTagData
        {
            Kind = IccTagDataKind.FloatingPointArray,
            FloatingPointValues = new double[] { double.NaN }
        };
        SetType(number, IccTypeSignatures.Float32Array);
        IccTagData text = new IccTagData { Kind = IccTagDataKind.Text };
        SetType(text, IccTypeSignatures.Utf8);
        hierarchy.Data.TagArray = new IccTagArray
        {
            ArraySignature = 0x74657374u,
            Values = new IccTagData[] { number, text }
        };
        IccTag mpe = CreateTag(IccTagSignatures.AToB0,
            IccTagDataKind.MultiProcessElements, IccTypeSignatures.MultiProcessElements);
        IccProcessingElement matrix = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 3,
            OutputChannels = 3,
            Matrix = new IccMatrix
            {
                Rows = 2,
                Columns = 2,
                Values = new double[] { 1.0, 0.0, 0.0, 1.0 }
            }
        };
        mpe.Data.MultiProcessElements = CreatePipeline(3, 3, matrix);
        IccTag curve = CreateTag(0x63757276u, IccTagDataKind.Curve,
            IccTypeSignatures.Curve);
        curve.Data.Curve = new IccCurve
        {
            Kind = IccCurveKind.Gamma,
            Gamma = -1.0
        };
        profile.Tags = new IccTag[] { hierarchy, mpe, curve };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.TagSize
                && issue.Path.Contains("FloatingPointValues")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.IccMaxHierarchy
                && issue.Message.Contains("same tag type")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.PipelineChannelContinuity
                && issue.Message.Contains("matrix element")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.CurveValidity
                && issue.Path == "Tags[2].Curve"), Is.True);
        });
    }

    [Test]
    public void SpectralPccAndMultiplexRelationshipsUseHeaderChannelCounts()
    {
        IccProfile profile = CreateVersionFiveProfile();
        profile.Header.SpectralPcsSignature =
            IccColorSpaceSignatures.ReflectanceSpectralPrefix | 3u;
        profile.Header.SpectralPcs = IccColorSpaceKind.Spectral;
        profile.Header.MaterialConnectionSpaceSignature =
            IccColorSpaceSignatures.MultiplexPrefix | 2u;
        profile.Header.MaterialConnectionSpace = IccColorSpaceKind.Multiplex;
        IccTag white = CreateTag(IccTagSignatures.SpectralWhitePoint,
            IccTagDataKind.FloatingPointArray, IccTypeSignatures.Float32Array);
        white.Data.FloatingPointValues = new double[] { 0.9, 1.0 };
        IccTag pcc = CreateTag(IccTagSignatures.SourcePcc,
            IccTagDataKind.Unknown, 0);
        pcc.Data.ProfileConnectionConditions = new IccProfileConnectionConditions
        {
            ToStandardPcs = CreatePipeline(3, 3, CreateUnknownElement(3, 3))
        };
        IccTag multiplex = CreateTag(IccTagSignatures.MultiplexTypeArray,
            IccTagDataKind.Unknown, 0);
        multiplex.Data.MultiplexChannels = new IccMultiplexChannels
        {
            Names = new string[] { "Gloss", "Gloss" },
            DefaultValues = new double[0],
            Order = new int[] { 0, 0 }
        };
        profile.Tags = new IccTag[] { white, pcc, multiplex };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.IccMaxSpectral
                && issue.Message.Contains("spectral white point")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.IccMaxPcc
                && issue.Path.Contains("ProfileConnectionConditions")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.IccMaxMcs
                && issue.Message.Contains("2-channel")), Is.True);
        });
    }

    [Test]
    public void GamutBrdfAndSurfaceModelsReportSemanticInconsistencies()
    {
        IccProfile profile = CreateVersionFiveProfile();
        IccTag gamut = CreateTag(IccTagSignatures.GamutBoundary0,
            IccTagDataKind.GamutBoundary, IccTypeSignatures.GamutBoundaryDescription);
        gamut.Data.GamutBoundary = new IccGamutBoundary();
        IccTag brdf = CreateTag(IccTagSignatures.BrdfColorimetricParameter0,
            IccTagDataKind.Unknown, 0);
        brdf.Data.Brdf = new IccBrdf
        {
            TypeSignature = StructuredTagProjectionCodec.ColorBrdf,
            ModelSignature = BrdfEvaluator.BlinnPhongColor,
            Parameters = new double[] { 9.0 }
        };
        IccTag surface = CreateTag(IccTagSignatures.SurfaceMap,
            IccTagDataKind.Unknown, 0);
        surface.Data.SurfaceMap = new IccSurfaceMap
        {
            Width = 10,
            Height = 10
        };
        profile.Tags = new IccTag[] { gamut, brdf, surface };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("gamut")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("BRDF")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.ProfileClassSemantics
                && issue.Message.Contains("surface map")), Is.True);
        });
    }

    [Test]
    public void ValidBrdfAndHdrModelsDoNotProduceIcc2SemanticIssues()
    {
        IccProfile profile = CreateVersionFiveProfile();
        profile.Header.UsesExtendedPcsRange = true;
        IccTag brdf = CreateTag(IccTagSignatures.BrdfColorimetricParameter0,
            IccTagDataKind.Unknown, 0);
        brdf.Data.Brdf = new IccBrdf
        {
            TypeSignature = StructuredTagProjectionCodec.ColorBrdf,
            ModelSignature = BrdfEvaluator.BlinnPhongColor,
            Parameters = new double[] { 3.0 },
            ColorimetricTransform = CreatePipeline(3, 9,
                CreateUnknownElement(3, 9))
        };
        IccTag hdr = CreateTag(IccTagSignatures.HdrToSdr0,
            IccTagDataKind.MultiProcessElements, IccTypeSignatures.MultiProcessElements);
        hdr.Data.MultiProcessElements = CreatePipeline(3, 3,
            CreateUnknownElement(3, 3));
        profile.Tags = new IccTag[] { brdf, hdr };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void AbbreviatedSubclassAndExtendedDeviceRulesAreAppliedTogether()
    {
        IccProfile profile = CreateVersionFiveProfile();
        profile.Header.ProfileClass = IccProfileClass.ColorEncodingSpace;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.ColorEncodingSpace;
        profile.Header.ProfileSubclassSignature = 0x73756263u;
        profile.Header.DataColorSpace = IccColorSpaceKind.ExtendedDevice;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Tags = new IccTag[]
        {
            CreateTag(IccTagSignatures.AToB0, IccTagDataKind.Unknown, 0)
        };

        IccValidationIssue[] issues = Icc2ValidationCore.Validate(profile,
            new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Any(issue => issue.RuleId == IccValidationRuleIds.ForbiddenTag
                && issue.Message.Contains("abbreviated")), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.HeaderProfileSubclass), Is.True);
            Assert.That(issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.HeaderColorSpace), Is.True);
        });
    }

    [Test]
    public void ValidationReportDecodesRetainedVersionFivePayloads()
    {
        IccProfile profile = CreateVersionFiveProfile();
        IccTag tag = CreateTag(IccTagSignatures.MultiplexTypeArray,
            IccTagDataKind.Raw, IccTypeSignatures.TagArray);
        tag.Data.RawData.Bytes = new byte[]
        {
            0x74, 0x61, 0x72, 0x79, 0, 0, 0, 0
        };
        tag.TableEntry.Signature = tag.Signature;
        tag.TableEntry.Offset = IccConstants.MinimumProfileSize;
        tag.TableEntry.Size = tag.Data.RawData.Bytes.Length;
        profile.Tags = new IccTag[] { tag };
        profile.TagTableEntries = new IccTagTableEntry[] { tag.TableEntry };
        profile.Header.DeclaredSize = IccConstants.MinimumProfileSize
            + tag.Data.RawData.Bytes.Length;
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);

        Assert.That(report.Issues.Any(issue => issue.RuleId == IccValidationRuleIds.TagSize
            && issue.Path == "Tags[0]" && issue.Message.Contains("tagArray")),
            Is.True);
    }

    private static IccProfile CreateVersionFiveProfile()
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version = HeaderCodec.DecodeVersion(0x05000000u);
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.CreationDateTime.Month = 1;
        profile.Header.CreationDateTime.Day = 1;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        return profile;
    }

    private static IccTag CreateTag(uint signature, IccTagDataKind kind,
        uint typeSignature)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = kind;
        if (typeSignature != 0)
        {
            SetType(tag.Data, typeSignature);
        }
        return tag;
    }

    private static void SetType(IccTagData data, uint typeSignature)
    {
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = typeSignature;
    }

    private static IccMultiProcessElements CreatePipeline(int inputChannels,
        int outputChannels, IccProcessingElement element)
    {
        return new IccMultiProcessElements
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Elements = new IccProcessingElement[] { element }
        };
    }

    private static IccProcessingElement CreateUnknownElement(int inputChannels,
        int outputChannels)
    {
        return new IccProcessingElement
        {
            Signature = 0x70726976u,
            Kind = IccProcessingElementKind.Unknown,
            InputChannels = inputChannels,
            OutputChannels = outputChannels
        };
    }
}
