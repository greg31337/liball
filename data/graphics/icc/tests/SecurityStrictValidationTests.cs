using icc;

namespace icc.Tests;

public class SecurityStrictValidationTests
{
    [Test]
    public void StrictValidationStopsAtOversizedProfileCollections()
    {
        IccProfile profile = CreateProfile(new IccTagData());
        profile.Tags = new IccTag[] { profile.Tags[0], new IccTag() };
        IccValidationPolicy policy = CreateStrictPolicy();
        policy.ResourceLimits.MaximumTagCount = 1;

        IccValidationReport report = Validate(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.SecurityResourceLimit
                && issue.Message.Contains("2 tag models")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.Path == "Profile.Tags"), Is.True);
        });
    }

    [Test]
    public void SecurityGraphLimitsDoNotApplyInConformingMode()
    {
        IccProfile profile = CreateProfile(new IccTagData());
        profile.Tags = new IccTag[] { profile.Tags[0], new IccTag() };
        IccValidationPolicy policy = CreateStrictPolicy();
        policy.Mode = IccValidationMode.Conforming;
        policy.ResourceLimits.MaximumTagCount = 1;

        IccValidationReport report = Validate(profile, policy);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.SecurityResourceLimit
            && issue.Path == "Profile.Tags"), Is.False);
    }

    [Test]
    public void StrictValidationUsesCheckedClutCountProducts()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.Lut = new IccLut();
        data.Lut.InputChannels = 2;
        data.Lut.OutputChannels = 3;
        data.Lut.Clut.InputChannels = 2;
        data.Lut.Clut.OutputChannels = 3;
        data.Lut.Clut.GridPoints = new int[] { 50000, 50000 };
        data.Lut.Clut.Values = new double[0];
        IccValidationPolicy policy = CreateStrictPolicy();
        policy.ResourceLimits.MaximumDecompressedBytes = 1024;

        IccValidationReport report = Validate(CreateProfile(data), policy);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.SecurityResourceLimit
            && issue.Path.Contains("Clut")
            && issue.Message.Contains("count product")), Is.True);
    }

    [Test]
    public void StrictValidationRejectsRecursiveTagGraphsWithoutRecursingIndefinitely()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.TagArray;
        data.TagArray = new IccTagArray();
        data.TagArray.Values = new IccTagData[] { data };

        IccValidationReport report = Validate(CreateProfile(data), CreateStrictPolicy());

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.IccMaxHierarchy
            && issue.Message.Contains("reference cycle")), Is.True);
    }

    [Test]
    public void StrictValidationBoundsCompressedTextExpansion()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Text;
        data.Text = new IccTextData();
        data.Text.IsCompressed = true;
        data.Text.Bytes = new byte[64];
        IccValidationPolicy policy = CreateStrictPolicy();
        policy.ResourceLimits.MaximumDecompressedBytes = 32;

        IccValidationReport report = Validate(CreateProfile(data), policy);

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.CompressionLimit
            && issue.Message.Contains("64 bytes")), Is.True);
    }

    [Test]
    public void StrictValidationInspectsMalformedEmbeddedImages()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.EmbeddedImage;
        data.EmbeddedImage = new IccEmbeddedImage();
        data.EmbeddedImage.Width = 1;
        data.EmbeddedImage.Height = 1;
        data.EmbeddedImage.Components = 3;
        data.EmbeddedImage.EncodingSignature = 0;
        data.EmbeddedImage.Bytes = new byte[] { 1, 2, 3, 4 };

        IccValidationReport report = Validate(CreateProfile(data), CreateStrictPolicy());

        Assert.That(report.Issues.Any(issue => issue.RuleId == IccValidationRuleIds.TagSize
            && issue.Path.Contains("EmbeddedImage.Bytes")), Is.True);
    }

    [Test]
    public void StrictValidationRejectsNonFiniteDecodedValues()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Curve;
        data.Curve = new IccCurve();
        data.Curve.Gamma = double.NaN;

        IccValidationReport report = Validate(CreateProfile(data), CreateStrictPolicy());

        Assert.That(report.Issues.Any(issue => issue.RuleId == IccValidationRuleIds.TagSize
            && issue.Message.Contains("NaN or infinity")
            && issue.Path.Contains("Curve")), Is.True);
    }

    [Test]
    public void StrictValidationBoundsCalculatorCostStackAndPrivateOperations()
    {
        IccCalculatorProgram program = new IccCalculatorProgram();
        IccCalculatorInstruction first = new IccCalculatorInstruction();
        first.Signature = CalculatorOperationSignatures.NoOperation;
        IccCalculatorInstruction second = new IccCalculatorInstruction();
        second.Signature = 0x70726976;
        program.Instructions = new IccCalculatorInstruction[] { first, second };
        program.InstructionCount = 2;
        program.RequiredStackDepth = 3;
        program.UnknownOperationCount = 0;
        program.IsExecutable = true;
        IccProcessingElement element = new IccProcessingElement();
        element.Kind = IccProcessingElementKind.Calculator;
        element.Calculator = program;
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.Elements = new IccProcessingElement[] { element };
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        IccValidationPolicy policy = CreateStrictPolicy();
        policy.ResourceLimits.MaximumCalculatorInstructions = 1;
        policy.ResourceLimits.MaximumCalculatorStackDepth = 1;

        IccValidationReport report = Validate(CreateProfile(data), policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.CalculatorSafety
                && issue.Message.Contains("instructions")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.CalculatorSafety
                && issue.Message.Contains("stack depth")), Is.True);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.CalculatorSafety
                && issue.Message.Contains("private or unknown operations")), Is.True);
        });
    }

    [Test]
    public void StrictValidationRejectsExecutableUnknownProcessingElements()
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = 0x70726976;
        element.Kind = IccProcessingElementKind.Unknown;
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.IsExecutable = true;
        pipeline.Elements = new IccProcessingElement[] { element };
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;

        IccValidationReport report = Validate(CreateProfile(data), CreateStrictPolicy());

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.CalculatorSafety
            && issue.Message.Contains("private or unknown processing element")), Is.True);
    }

    [Test]
    public void StrictValidationRejectsAliasesWithDifferentRetainedPayloads()
    {
        IccTagData firstData = new IccTagData();
        firstData.Kind = IccTagDataKind.Raw;
        firstData.RawData.Bytes = new byte[] { 1, 2, 3, 4, 0, 0, 0, 0 };
        IccProfile profile = CreateProfile(firstData);
        IccTag secondTag = new IccTag();
        secondTag.Signature = IccTagSignatures.ProfileDescription;
        secondTag.TableEntry.Signature = secondTag.Signature;
        secondTag.TableEntry.Offset = 156;
        secondTag.TableEntry.Size = 8;
        secondTag.TableEntry.SharedDataId = 7;
        secondTag.Data.Kind = IccTagDataKind.Raw;
        secondTag.Data.RawData.Bytes = new byte[] { 9, 2, 3, 4, 0, 0, 0, 0 };
        profile.Tags[0].TableEntry.Offset = 156;
        profile.Tags[0].TableEntry.SharedDataId = 7;
        profile.TagTableEntries = new IccTagTableEntry[]
        {
            profile.Tags[0].TableEntry,
            secondTag.TableEntry
        };
        profile.Tags = new IccTag[] { profile.Tags[0], secondTag };
        profile.Header.DeclaredSize = 164;

        IccValidationReport report = Validate(profile, CreateStrictPolicy());

        Assert.That(report.Issues.Any(issue => issue.RuleId
            == IccValidationRuleIds.TagTableOverlap
            && issue.Message.Contains("retain different bytes")), Is.True);
    }

    private static IccValidationReport Validate(IccProfile profile, IccValidationPolicy policy)
    {
        return new IccProfileValidator().ValidateProfileDataReport(profile, policy);
    }

    private static IccValidationPolicy CreateStrictPolicy()
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = IccValidationMode.SecurityStrict;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return policy;
    }

    private static IccProfile CreateProfile(IccTagData data)
    {
        IccProfile profile = new IccProfile();
        profile.Header.DeclaredSize = 152;
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
        profile.Header.RenderingIntent = (int)IccRenderingIntent.Perceptual;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.Copyright;
        tag.Data = data;
        tag.TableEntry.Signature = tag.Signature;
        tag.TableEntry.Offset = 144;
        tag.TableEntry.Size = 8;
        profile.TagTableEntries = new IccTagTableEntry[] { tag.TableEntry };
        profile.Tags = new IccTag[] { tag };
        return profile;
    }
}
