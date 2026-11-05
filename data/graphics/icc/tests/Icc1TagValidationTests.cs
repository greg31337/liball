using icc;

namespace icc.Tests;

public class Icc1TagValidationTests
{
    [Test]
    public void ValidDecodedClassicLutPassesTypeSpecificValidation()
    {
        IccProfile profile = CreateDecodedProfile(IccTagSignatures.AToB0,
            IccTypeSignatures.Lut8, IccTagDataKind.Lut);
        IccLut lut = new IccLut();
        lut.InputChannels = 1;
        lut.OutputChannels = 1;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = IdentityMatrix();
        lut.InputTables = new double[][] { Ramp(256) };
        lut.OutputTables = new double[][] { Ramp(256) };
        lut.Clut.InputChannels = 1;
        lut.Clut.OutputChannels = 1;
        lut.Clut.GridPoints = new int[] { 2 };
        lut.Clut.Values = new double[] { 0.0, 1.0 };
        lut.PcsEncoding = IccPcsEncodingKind.Normalized;
        profile.Tags[0].Data.Lut = lut;

        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[]
        {
            IccValidationRuleIds.RequiredTag,
            IccValidationRuleIds.ChannelCount
        };
        IccValidationReport report = new IccProfileValidator().ValidateProfileDataReport(
            profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ClassicLutReportsChannelsMatrixClutAndPcsEncodingIndependently()
    {
        IccProfile profile = CreateDecodedProfile(IccTagSignatures.AToB0,
            IccTypeSignatures.Lut8, IccTagDataKind.Lut);
        IccLut lut = new IccLut();
        lut.InputChannels = 0;
        lut.OutputChannels = 2;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = new double[9];
        lut.InputTables = new double[0][];
        lut.OutputTables = new double[][] { Ramp(2), Ramp(3) };
        lut.Clut.InputChannels = 0;
        lut.Clut.OutputChannels = 2;
        lut.Clut.GridPoints = new int[0];
        lut.Clut.Values = new double[0];
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        profile.Tags[0].Data.Lut = lut;

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            AssertRule(report, IccValidationRuleIds.ChannelCount);
            AssertRule(report, IccValidationRuleIds.MatrixInvertibility);
            AssertRule(report, IccValidationRuleIds.ClutDimensions);
            AssertRule(report, IccValidationRuleIds.TagTypeCompatibility);
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.Path.StartsWith("Tags[0].Data", StringComparison.Ordinal)
                && issue.Remediation.Length > 0));
        });
    }

    [Test]
    public void CurveFunctionsAndAdaptiveGainNodesAreValidatedFromDecodedModels()
    {
        IccProfile parametricProfile = CreateDecodedProfile(IccTagSignatures.RedTrc,
            IccTypeSignatures.ParametricCurve, IccTagDataKind.Curve);
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Parametric;
        curve.Parametric = new IccParametricCurve();
        curve.Parametric.FunctionType = 4;
        curve.Parametric.Parameters = new double[] { 2.2, 1.0, 0.0 };
        parametricProfile.Tags[0].Data.Curve = curve;
        IccProfile adaptiveProfile = CreateDecodedProfile(IccTagSignatures.AdaptiveGainCurve,
            IccTypeSignatures.AdaptiveGainCurve, IccTagDataKind.AdaptiveGainCurve);
        IccAdaptiveGainCurve adaptive = new IccAdaptiveGainCurve();
        adaptive.RedNodes = Nodes(0.0, 1.0);
        adaptive.GreenNodes = Nodes(0.0, 0.0);
        adaptive.BlueNodes = Nodes(0.0, 1.0);
        adaptiveProfile.Tags[0].Data.AdaptiveGainCurve = adaptive;

        IccValidationReport parametricReport = Validate(parametricProfile);
        IccValidationReport adaptiveReport = Validate(adaptiveProfile);

        Assert.Multiple(() =>
        {
            Assert.That(FindRule(parametricReport, IccValidationRuleIds.CurveValidity).Path,
                Is.EqualTo("Tags[0].Curve"));
            IccValidationIssue adaptiveIssue = FindRule(adaptiveReport,
                IccValidationRuleIds.CurveValidity);
            Assert.That(adaptiveIssue.Path, Is.EqualTo("Tags[0].Data.AdaptiveGainCurve"));
            Assert.That(adaptiveIssue.Message, Does.Contain("adaptive-gain"));
        });
    }

    [Test]
    public void NumericRangesAndNamedColourPcsDataAreValidated()
    {
        IccProfile fixedProfile = CreateDecodedProfile(IccTagSignatures.ChromaticAdaptation,
            IccTypeSignatures.S15Fixed16Array, IccTagDataKind.FixedPointArray);
        fixedProfile.Tags[0].Data.FixedPointValues = new double[] { double.PositiveInfinity };
        IccProfile namedProfile = CreateDecodedProfile(IccTagSignatures.NamedColor2,
            IccTypeSignatures.NamedColor2, IccTagDataKind.NamedColors);
        IccNamedColorSet set = new IccNamedColorSet();
        set.DeviceChannelCount = 2;
        IccNamedColor color = new IccNamedColor();
        color.Name = "Sample";
        color.PcsColor.Components = new double[] { 2.25, 0.5, 0.5 };
        color.DeviceColor.Components = new double[] { 0.5 };
        set.Colors = new IccNamedColor[] { color };
        namedProfile.Tags[0].Data.NamedColors = set;

        IccValidationReport fixedReport = Validate(fixedProfile);
        IccValidationReport namedReport = Validate(namedProfile);

        Assert.Multiple(() =>
        {
            Assert.That(FindRule(fixedReport, IccValidationRuleIds.TagSize).Message,
                Does.Contain("numeric range"));
            AssertRule(namedReport, IccValidationRuleIds.ChannelCount);
            AssertRule(namedReport, IccValidationRuleIds.TagTypeCompatibility);
        });
    }

    [Test]
    public void RetainedIcc1PayloadsAreDecodedForValidationWithoutMutation()
    {
        byte[] badCurve = CreatePayload(IccTypeSignatures.ParametricCurve, new byte[4]);
        WriteUInt16(badCurve, 8, 9);
        IccProfile curveProfile = CreateRawProfile(IccTagSignatures.RedTrc, badCurve);
        byte[] badOffset = CreatePayload(IccTypeSignatures.MultiLocalizedUnicode,
            new byte[23]);
        WriteUInt32(badOffset, 8, 1);
        WriteUInt32(badOffset, 12, 12);
        WriteUInt16(badOffset, 16, 0x656e);
        WriteUInt16(badOffset, 18, 0x5553);
        WriteUInt32(badOffset, 20, 2);
        WriteUInt32(badOffset, 24, 29);
        badOffset[29] = 0;
        badOffset[30] = 65;
        IccProfile offsetProfile = CreateRawProfile(IccTagSignatures.Copyright, badOffset);

        IccValidationReport curveReport = Validate(curveProfile);
        IccValidationReport offsetReport = Validate(offsetProfile);

        Assert.Multiple(() =>
        {
            AssertRule(curveReport, IccValidationRuleIds.CurveValidity);
            AssertRule(offsetReport, IccValidationRuleIds.TagNestedOffset);
            Assert.That(curveProfile.Tags[0].IsDecodeAttempted, Is.False);
            Assert.That(offsetProfile.Tags[0].IsDecodeAttempted, Is.False);
            Assert.That(curveProfile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(offsetProfile.Tags[0].Data.RawData.Bytes, Is.EqualTo(badOffset));
        });
    }

    [Test]
    public void MultiStageLutValidatesStageCurveAndClutDimensions()
    {
        IccProfile profile = CreateDecodedProfile(IccTagSignatures.AToB0,
            IccTypeSignatures.LutAToB, IccTagDataKind.Lut);
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.BCurves = new IccCurve[] { IdentityCurve(), IdentityCurve() };
        lut.ACurves = new IccCurve[] { IdentityCurve(), IdentityCurve(), IdentityCurve() };
        lut.Clut = new IccClut();
        lut.Clut.InputChannels = 3;
        lut.Clut.OutputChannels = 3;
        lut.Clut.GridPoints = new int[] { 2, 2, 1 };
        lut.Clut.Values = new double[24];
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        profile.Tags[0].Data.MultiStageLut = lut;

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            AssertRule(report, IccValidationRuleIds.ChannelCount);
            AssertRule(report, IccValidationRuleIds.ClutDimensions);
        });
    }

    [Test]
    public void LegacyNamedColorsUseTintCoordinatesAndValidateTerminatedAsciiNames()
    {
        IccProfile profile = CreateDecodedProfile(IccTagSignatures.NamedColor,
            IccTypeSignatures.NamedColor, IccTagDataKind.NamedColors);
        profile.Header.Version = HeaderCodec.DecodeVersion(0x02100000);
        IccNamedColorSet set = new IccNamedColorSet();
        set.DeviceChannelCount = 3;
        IccNamedColor color = new IccNamedColor();
        color.Name = "Ink";
        color.Tints = new double[] { 0.25, 0.5, 0.75 };
        set.Colors = new IccNamedColor[] { color };
        profile.Tags[0].Data.NamedColors = set;

        IccValidationReport validReport = Validate(profile);
        color.Name = new string('x', 32);
        IccValidationReport invalidReport = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(validReport.Issues.Select(issue => issue.RuleId),
                Does.Not.Contain(IccValidationRuleIds.ChannelCount));
            Assert.That(validReport.Issues.Select(issue => issue.RuleId),
                Does.Not.Contain(IccValidationRuleIds.TagTypeCompatibility));
            AssertRule(invalidReport, IccValidationRuleIds.TextEncoding);
        });
    }

    [Test]
    public void Icc1MpeSegmentedCurvesValidateTheirFormulaAndBreakpoints()
    {
        IccProfile profile = CreateDecodedProfile(IccTagSignatures.AToB0,
            IccTypeSignatures.MultiProcessElements,
            IccTagDataKind.MultiProcessElements);
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 1;
        pipeline.OutputChannels = 1;
        IccProcessingElement element = new IccProcessingElement();
        element.Kind = IccProcessingElementKind.CurveSet;
        element.Signature = IccProcessingElementSignatures.CurveSet;
        element.InputChannels = 1;
        element.OutputChannels = 1;
        IccCurve segmented = new IccCurve();
        segmented.Kind = IccCurveKind.Segmented;
        IccCurveSegment segment = new IccCurveSegment();
        segment.BreakPoint = double.NegativeInfinity;
        segment.FormulaType = 0;
        segment.Parameters = new double[] { 1.0, 1.0, 0.0, 0.0 };
        segmented.Segments = new IccCurveSegment[] { segment };
        element.Curves = new IccCurve[] { segmented };
        pipeline.Elements = new IccProcessingElement[] { element };
        profile.Tags[0].Data.MultiProcessElements = pipeline;

        IccValidationReport validReport = Validate(profile);
        segment.Parameters = new double[] { 1.0 };
        IccValidationReport invalidReport = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(validReport.Issues.Select(issue => issue.RuleId),
                Does.Not.Contain(IccValidationRuleIds.CurveValidity));
            AssertRule(invalidReport, IccValidationRuleIds.CurveValidity);
        });
    }

    [Test]
    public void NestedLocalesAndResponseCurveOrderingAreValidated()
    {
        IccProfile sequenceProfile = CreateDecodedProfile(IccTagSignatures.ProfileSequenceIdentifier,
            IccTypeSignatures.ProfileSequenceIdentifier, IccTagDataKind.ProfileSequence);
        IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
        IccLocalizedString description = new IccLocalizedString();
        description.Language = "EN";
        description.Country = "us";
        identifier.Descriptions = new IccLocalizedString[] { description };
        sequenceProfile.Tags[0].Data.ProfileSequenceIdentifiers =
            new IccProfileSequenceIdentifier[] { identifier };
        IccProfile responseProfile = CreateDecodedProfile(IccTagSignatures.OutputResponse,
            IccTypeSignatures.ResponseCurveSet16, IccTagDataKind.ResponseCurves);
        responseProfile.Tags[0].Data.ResponseCurveChannelCount = 1;
        IccResponseCurve response = new IccResponseCurve();
        response.MeasurementUnitSignature = IccMeasurementUnitSignatures.StatusA;
        response.MaximumColorantXyz = new IccXyzNumber[] { new IccXyzNumber() };
        response.DeviceCodes = new ushort[][] { new ushort[] { 10, 10 } };
        response.Measurements = new double[][] { new double[] { 0.1, 0.2 } };
        responseProfile.Tags[0].Data.ResponseCurves = new IccResponseCurve[] { response };

        IccValidationReport sequenceReport = Validate(sequenceProfile);
        IccValidationReport responseReport = Validate(responseProfile);

        Assert.Multiple(() =>
        {
            AssertRule(sequenceReport, IccValidationRuleIds.TagNestedOffset);
            AssertRule(responseReport, IccValidationRuleIds.ChannelCount);
        });
    }

    private static IccValidationReport Validate(IccProfile profile)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return new IccProfileValidator().ValidateProfileDataReport(profile,
            policy);
    }

    private static IccProfile CreateDecodedProfile(uint tagSignature, uint typeSignature,
        IccTagDataKind kind)
    {
        IccProfile profile = CreateBaseProfile(152);
        IccTag tag = AttachTag(profile, tagSignature, 8);
        tag.Data.Kind = kind;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = typeSignature;
        tag.IsDecodeAttempted = true;
        tag.DecodeResult.Status = IccOperationStatus.Success;
        return profile;
    }

    private static IccProfile CreateRawProfile(uint tagSignature, byte[] payload)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        IccProfile profile = CreateBaseProfile(declaredSize);
        IccTag tag = AttachTag(profile, tagSignature, payload.Length);
        tag.PaddingBytes = new byte[declaredSize - payloadEnd];
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.HasTypeSignature = payload.Length >= 4;
        tag.Data.RawData.TypeSignature = payload.Length >= 4
            ? ReadUInt32(payload, 0) : 0;
        tag.Data.RawData.Bytes = payload;
        return profile;
    }

    private static IccProfile CreateBaseProfile(int declaredSize)
    {
        IccProfile profile = new IccProfile();
        profile.Location.Offset = 0;
        profile.Location.Length = declaredSize;
        profile.Header.DeclaredSize = declaredSize;
        profile.Header.Version = HeaderCodec.DecodeVersion(0x04400000);
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.CreationDateTime.Month = 1;
        profile.Header.CreationDateTime.Day = 1;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        return profile;
    }

    private static IccTag AttachTag(IccProfile profile, uint signature, int size)
    {
        IccTagTableEntry entry = new IccTagTableEntry();
        entry.Signature = signature;
        entry.Offset = 144;
        entry.Size = size;
        entry.OriginalIndex = 0;
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry = entry;
        profile.TagTableEntries = new IccTagTableEntry[] { entry };
        profile.Tags = new IccTag[] { tag };
        return tag;
    }

    private static IccCurve IdentityCurve()
    {
        return new IccCurve();
    }

    private static IccAdaptiveGainCurveNode[] Nodes(double first, double second)
    {
        IccAdaptiveGainCurveNode firstNode = new IccAdaptiveGainCurveNode();
        firstNode.Input = first;
        IccAdaptiveGainCurveNode secondNode = new IccAdaptiveGainCurveNode();
        secondNode.Input = second;
        return new IccAdaptiveGainCurveNode[] { firstNode, secondNode };
    }

    private static double[] IdentityMatrix()
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
    }

    private static double[] Ramp(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = count == 1 ? 0.0 : index / (double)(count - 1);
            index = index + 1;
        }
        return values;
    }

    private static byte[] CreatePayload(uint typeSignature, byte[] body)
    {
        byte[] payload = new byte[8 + body.Length];
        WriteUInt32(payload, 0, typeSignature);
        Array.Copy(body, 0, payload, 8, body.Length);
        return payload;
    }

    private static void AssertRule(IccValidationReport report, string ruleId)
    {
        Assert.That(report.Issues.Select(issue => issue.RuleId), Does.Contain(ruleId));
    }

    private static IccValidationIssue FindRule(IccValidationReport report, string ruleId)
    {
        return report.Issues.First(issue => issue.RuleId == ruleId);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8) | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
