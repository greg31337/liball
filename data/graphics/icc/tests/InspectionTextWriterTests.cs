using icc;

namespace icc.Tests;

public class InspectionTextWriterTests
{
    [Test]
    public void DetailedAndRawInspectionIncludeDumpCoverageAndRemainDeterministic()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateMatrixProfile();

        string summary = inspector.InspectProfileText(profile, IccInspectionDetail.Summary, 8);
        string normal = inspector.InspectProfileText(profile, IccInspectionDetail.Normal, 8);
        string detailed = inspector.InspectProfileText(profile, IccInspectionDetail.Detailed, 8);
        string repeated = inspector.InspectProfileText(profile, IccInspectionDetail.Detailed, 8);
        string raw = inspector.InspectProfileText(profile, IccInspectionDetail.Raw, 4);

        Assert.Multiple(() =>
        {
            Assert.That(summary, Does.Not.Contain("\nHeader:"));
            Assert.That(normal, Does.Contain("\nHeader: declared size="));
            Assert.That(normal, Does.Not.Contain("\nInspection tree:"));
            Assert.That(normal, Does.Not.Contain("raw="));
            Assert.That(detailed, Does.Contain("\nHeader: declared size="));
            Assert.That(detailed, Does.Contain("\nProfile ID: absent"));
            Assert.That(detailed, Does.Contain("\nTag table: 7 entries"));
            Assert.That(detailed, Does.Contain("Unknown/private tag signature ('test'"));
            Assert.That(detailed, Does.Contain("curve Sampled; 12 samples"));
            Assert.That(detailed, Does.Contain("\nTransform selection:"));
            Assert.That(detailed, Does.Contain("selected matrix/TRC"));
            Assert.That(detailed, Does.Contain("\nValidation: invalid; issues="));
            Assert.That(detailed, Does.Contain("\nInspection tree:"));
            Assert.That(detailed, Is.EqualTo(repeated));
            Assert.That(raw, Does.Contain("raw length=10 bytes"));
            Assert.That(raw, Does.Contain("raw=01020304"));
            Assert.That(raw, Does.Contain("[truncated]"));
        });
    }

    [Test]
    public void TransformSelectionNamesSelectedTagAndFallback()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateMatrixProfile();

        string perceptual = inspector.DescribeTransformSelection(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual);
        string relative = inspector.DescribeTransformSelection(profile,
            IccTransformDirection.PcsToDevice,
            IccRenderingIntent.MediaRelativeColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(perceptual, Does.Contain("requested Perceptual"));
            Assert.That(perceptual, Does.Contain("selected matrix/TRC"));
            Assert.That(perceptual, Does.Contain("fallback=True"));
            Assert.That(relative, Does.Contain("selected matrix/TRC"));
            Assert.That(relative, Does.Contain("fallback=False"));
        });
    }

    [Test]
    public void VersionFiveSelectionIsConsistentAcrossDescriptionTreeAndText()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = ProfileQualityTests.CreateVersionFiveIdentityProfile();
        IccInspectionOptions options = new IccInspectionOptions
        {
            Detail = IccInspectionDetail.Normal,
            MaximumDepth = 16,
            MaximumChildren = 64
        };

        string selected = inspector.DescribeTransformSelection(profile,
            IccTransformDirection.DeviceToPcs,
            IccRenderingIntent.MediaRelativeColorimetric);
        IccProfileSummary tree = inspector.InspectProfile(profile, options);
        string text = inspector.InspectProfileText(profile,
            IccInspectionDetail.Normal, 64);
        IccInspectionNode precedence = tree.Root.Children.Single(value =>
            value.Name == "TransformPrecedence");
        IccInspectionNode forward = precedence.Children.Single(value =>
            value.Path == "Transforms.DeviceToPcs.MediaRelativeColorimetric");

        Assert.Multiple(() =>
        {
            Assert.That(selected, Does.Contain("success=True"));
            Assert.That(selected, Does.Contain("A To B 1 ('A2B1'"));
            Assert.That(selected, Does.Contain("fallback=False"));
            Assert.That(forward.Value, Does.Contain("success=True"));
            Assert.That(forward.Value, Does.Contain("'A2B1'"));
            Assert.That(text, Does.Contain(selected));
        });
    }

    [Test]
    public void SelectedTagRawInspectionIncludesBoundedBytesAndOmissionCount()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccTag tag = RawTag();

        string normal = inspector.InspectTagText(tag, IccInspectionDetail.Normal, 3);
        string raw = inspector.InspectTagText(tag, IccInspectionDetail.Raw, 3);
        string repeated = inspector.InspectTagText(tag, IccInspectionDetail.Raw, 3);

        Assert.Multiple(() =>
        {
            Assert.That(normal, Does.Not.Contain("raw length="));
            Assert.That(normal, Does.Not.Contain("raw="));
            Assert.That(raw, Does.Contain("raw length=10 bytes"));
            Assert.That(raw, Does.Contain("raw=010203"));
            Assert.That(raw, Does.EndWith("… 7 bytes omitted"));
            Assert.That(repeated, Is.EqualTo(raw));
        });
    }

    [Test]
    public void ProfileIdTextDistinguishesValidMismatchAndAbsentStates()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        byte[] bytes = CreateMinimalProfileBytes();
        IccProfileId id = new IccProfileIdCalculator().ComputeProfileIdBytes(bytes);
        System.Array.Copy(id.Bytes, 0, bytes, 84, id.Bytes.Length);
        IccProfile profile = new IccProfileReader().ReadProfileBytes(bytes, new IccReadOptions());

        string valid = inspector.InspectProfileText(profile, IccInspectionDetail.Normal, 8);
        profile.Header.ProfileId.Bytes[0] = (byte)(profile.Header.ProfileId.Bytes[0] ^ 1);
        string mismatch = inspector.InspectProfileText(profile, IccInspectionDetail.Normal, 8);
        System.Array.Clear(profile.Header.ProfileId.Bytes);
        profile.Header.ProfileId.IsPresent = false;
        string absent = inspector.InspectProfileText(profile, IccInspectionDetail.Normal, 8);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Does.Contain("Profile ID: valid ("));
            Assert.That(mismatch, Does.Contain("Profile ID: mismatch ("));
            Assert.That(absent, Does.Contain("Profile ID: absent"));
        });
    }

    [Test]
    public void TraceInspectionBoundsVectorsComponentsAndMessages()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccTransformTrace trace = new IccTransformTrace();
        trace.Source = Vector(0.1, 0.2, 0.3, 0.4);
        trace.Intermediates = new IccColorVector[]
        {
            Vector(1.0, 2.0, 3.0),
            Vector(4.0, 5.0, 6.0),
            Vector(7.0, 8.0, 9.0)
        };
        trace.Messages = new string[] { "curves", "matrix", "CLUT" };
        trace.Destination = Vector(0.7, 0.8, 0.9);

        string text = inspector.InspectTransformTrace(trace, 2);
        string zero = inspector.InspectTransformTrace(trace, 0);
        string zeroRepeated = inspector.InspectTransformTrace(trace, 0);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("Source: RGB [0.1, 0.2, … 2 more]"));
            Assert.That(text, Does.Contain("Stage 0: RGB [1, 2, … 1 more] — curves"));
            Assert.That(text, Does.Contain("Stage 1: RGB [4, 5, … 1 more] — matrix"));
            Assert.That(text, Does.Contain("… 1 intermediate vectors omitted"));
            Assert.That(text, Does.EndWith("Destination: RGB [0.7, 0.8, … 1 more]"));
            Assert.That(zero, Does.StartWith("Source: RGB [… 4 more]"));
            Assert.That(zero, Does.Contain("… 3 intermediate vectors omitted"));
            Assert.That(zero, Does.Not.Contain("Stage 0:"));
            Assert.That(zero, Does.EndWith("Destination: RGB [… 3 more]"));
            Assert.That(zeroRepeated, Is.EqualTo(zero));
            Assert.Throws<IccProfileValidationException>(() =>
                inspector.InspectTransformTrace(trace, -1));
        });
    }

    [Test]
    public void UnknownHeaderValuesRemainNumericAndDeterministic()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateMatrixProfile();
        profile.Header.Version.Kind = (IccVersionKind)99;
        profile.Header.ProfileClass = (IccProfileClass)99;
        profile.Header.ProfileClassSignature = 0x3f3f3f3fu;
        profile.Header.DataColorSpace = (IccColorSpaceKind)99;
        profile.Header.DataColorSpaceSignature = 0x3f524742u;
        profile.Header.ProfileConnectionSpace = (IccPcsKind)99;
        profile.Header.ProfileConnectionSpaceSignature = 0x3f58595au;
        profile.Header.RenderingIntentKind = (IccRenderingIntent)99;

        string first = inspector.InspectProfileText(profile,
            IccInspectionDetail.Detailed, 16);
        string second = inspector.InspectProfileText(profile,
            IccInspectionDetail.Detailed, 16);

        Assert.Multiple(() =>
        {
            Assert.That(first, Does.Contain("Unknown ICC version (99)"));
            Assert.That(first, Does.Contain("Unknown profile class (99)"));
            Assert.That(first, Does.Contain("Unknown colour space (99)"));
            Assert.That(first, Does.Contain("Unknown PCS (99)"));
            Assert.That(first, Does.Contain("Unknown rendering intent (99)"));
            Assert.That(first, Is.EqualTo(second));
        });
    }

    private static IccProfile CreateMatrixProfile()
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.Version.Major = 4;
        profile.Header.Version.Minor = 4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        profile.Tags = new IccTag[]
        {
            CurveTag(IccTagSignatures.RedTrc, true),
            CurveTag(IccTagSignatures.GreenTrc, false),
            CurveTag(IccTagSignatures.BlueTrc, false),
            XyzTag(IccTagSignatures.RedMatrixColumn, 1.0, 0.0, 0.0),
            XyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0),
            XyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0),
            RawTag()
        };
        profile.TagTableEntries = profile.Tags.Select(value => value.TableEntry).ToArray();
        return profile;
    }

    private static IccTag CurveTag(uint signature, bool sampled)
    {
        IccTag tag = BaseTag(signature, IccTagDataKind.Curve, IccTypeSignatures.Curve);
        tag.Data.Curve = sampled
            ? new IccCurve
            {
                Kind = IccCurveKind.Sampled,
                Samples = Enumerable.Range(0, 12).Select(value => value / 11.0).ToArray()
            }
            : new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 };
        return tag;
    }

    private static IccTag XyzTag(uint signature, double x, double y, double z)
    {
        IccTag tag = BaseTag(signature, IccTagDataKind.XyzArray, IccTypeSignatures.Xyz);
        tag.Data.XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } };
        return tag;
    }

    private static IccTag RawTag()
    {
        return BaseTag(0x74657374, IccTagDataKind.Raw, 0x74657374);
    }

    private static IccTag BaseTag(uint signature, IccTagDataKind kind, uint type)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.TableEntry.Offset = 200;
        tag.TableEntry.Size = 10;
        tag.Data.Kind = kind;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = type;
        tag.Data.RawData.Bytes = Enumerable.Range(1, 10).Select(value => (byte)value).ToArray();
        return tag;
    }

    private static IccColorVector Vector(double first, double second, double third, double fourth)
    {
        return new IccColorVector
        {
            ColorSpace = IccColorSpaceKind.Rgb,
            ColorSpaceSignature = IccColorSpaceSignatures.Rgb,
            Components = new double[] { first, second, third, fourth }
        };
    }

    private static IccColorVector Vector(double first, double second, double third)
    {
        return new IccColorVector
        {
            ColorSpace = IccColorSpaceKind.Rgb,
            ColorSpaceSignature = IccColorSpaceSignatures.Rgb,
            Components = new double[] { first, second, third }
        };
    }

    private static byte[] CreateMinimalProfileBytes()
    {
        byte[] bytes = new byte[132];
        WriteUInt32(bytes, 0, 132);
        WriteUInt32(bytes, 8, 0x04400000);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2026);
        WriteUInt16(bytes, 26, 9);
        WriteUInt16(bytes, 28, 12);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 68, (uint)HeaderCodec.D50IlluminantX);
        WriteUInt32(bytes, 72, (uint)HeaderCodec.D50IlluminantY);
        WriteUInt32(bytes, 76, (uint)HeaderCodec.D50IlluminantZ);
        WriteUInt32(bytes, 128, 0);
        return bytes;
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
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
