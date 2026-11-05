using icc;
using System.Reflection;

namespace icc.Tests;

public class ProfileCorpusTests
{
    [Test]
    public void LicensedIccDevReadableProfilesExerciseSupportedGenerations()
    {
        string directory = Path.Combine(CorpusDirectory(), "valid");
        string[] files = Directory.GetFiles(directory, "*.icc");
        Array.Sort(files, StringComparer.Ordinal);
        IIccProfileReader reader = new IccProfileReader();
        HashSet<IccVersionKind> generations = new HashSet<IccVersionKind>();

        Assert.That(files, Has.Length.EqualTo(8));
        int index = 0;
        while (index < files.Length)
        {
            byte[] bytes = File.ReadAllBytes(files[index]);
            IccOperationResult probe = reader.ProbeProfileBytes(bytes,
                new IccReadOptions());
            IccReadOptions recovery = RecoveryOptions();
            IccProfile profile = reader.ReadProfileBytes(bytes, recovery);
            generations.Add(profile.Header.Version.Kind);

            Assert.Multiple(() =>
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.Success), files[index]);
                Assert.That(profile.Header.ProfileFileSignature,
                    Is.EqualTo(IccConstants.ProfileFileSignature), files[index]);
                Assert.That(profile.OriginalBytes, Is.EqualTo(bytes),
                    files[index]);
                Assert.That(profile.Tags.All(tag => tag.IsDecodeAttempted),
                    Is.True, files[index]);
            });
            index = index + 1;
        }

        Assert.That(generations, Is.EquivalentTo(new IccVersionKind[]
        {
            IccVersionKind.Version2,
            IccVersionKind.Version4,
            IccVersionKind.Version5
        }));
    }

    [Test]
    public void LicensedIccDevMalformedProfilesProduceBoundedValidationReports()
    {
        string directory = Path.Combine(CorpusDirectory(), "malformed");
        string[] files = Directory.GetFiles(directory, "*.icc");
        Array.Sort(files, StringComparer.Ordinal);
        IIccProfileValidator validator = new IccProfileValidator();

        Assert.That(files, Has.Length.EqualTo(13));
        int index = 0;
        while (index < files.Length)
        {
            byte[] bytes = File.ReadAllBytes(files[index]);
            IccValidationReport report = validator.ValidateProfileBytesReport(
                bytes, RecoveryOptions());

            Assert.Multiple(() =>
            {
                Assert.That(report.IsValid, Is.False, files[index]);
                Assert.That(report.Result.Status,
                    Is.Not.EqualTo(IccOperationStatus.Unspecified), files[index]);
                Assert.That(report.TotalIssueCount, Is.GreaterThan(0),
                    files[index]);
                Assert.That(report.Issues.Length,
                    Is.LessThanOrEqualTo(1024), files[index]);
            });
            index = index + 1;
        }
    }

    [Test]
    public void HandBuiltHeadersCoverEverySupportedVersionAndProfileClass()
    {
        IIccProfileReader reader = new IccProfileReader();
        uint[] versions = new uint[]
        {
            0x02100000, 0x02400000, 0x04000000, 0x04100000,
            0x04200000, 0x04300000, 0x04400000, 0x05000000,
            0x05000101
        };
        IccVersionKind[] versionKinds = new IccVersionKind[]
        {
            IccVersionKind.Version2, IccVersionKind.Version2,
            IccVersionKind.Version4, IccVersionKind.Version4,
            IccVersionKind.Version4, IccVersionKind.Version4,
            IccVersionKind.Version4, IccVersionKind.Version5,
            IccVersionKind.Version5
        };
        (uint Signature, IccProfileClass Kind, uint Version)[] classes =
        {
            (IccProfileClassSignatures.Input, IccProfileClass.Input, 0x04400000),
            (IccProfileClassSignatures.Display, IccProfileClass.Display, 0x04400000),
            (IccProfileClassSignatures.Output, IccProfileClass.Output, 0x04400000),
            (IccProfileClassSignatures.DeviceLink, IccProfileClass.DeviceLink, 0x04400000),
            (IccProfileClassSignatures.ColorSpace, IccProfileClass.ColorSpace, 0x04400000),
            (IccProfileClassSignatures.Abstract, IccProfileClass.Abstract, 0x04400000),
            (IccProfileClassSignatures.NamedColor, IccProfileClass.NamedColor, 0x04400000),
            (IccProfileClassSignatures.ColorEncodingSpace, IccProfileClass.ColorEncodingSpace, 0x05000000),
            (IccProfileClassSignatures.MultiplexIdentification, IccProfileClass.MultiplexIdentification, 0x05000000),
            (IccProfileClassSignatures.MultiplexLink, IccProfileClass.MultiplexLink, 0x05000000),
            (IccProfileClassSignatures.MultiplexVisualization, IccProfileClass.MultiplexVisualization, 0x05000000)
        };

        int index = 0;
        while (index < versions.Length)
        {
            byte[] readableBytes = CreateHeaderProfile(
                versions[index], IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, 0);
            IccProfile profile = reader.ReadProfileBytes(readableBytes,
                RecoveryOptions());
            Assert.That(profile.Header.Version.Kind,
                Is.EqualTo(versionKinds[index]));

            byte[] malformedBytes = readableBytes.ToArray();
            IndependentBigEndian.WriteUInt32(malformedBytes, 36,
                0x42414421);
            IccOperationResult malformed = reader.ProbeProfileBytes(
                malformedBytes, new IccReadOptions());
            Assert.That(malformed.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            index = index + 1;
        }

        index = 0;
        while (index < classes.Length)
        {
            IccProfile profile = reader.ReadProfileBytes(CreateHeaderProfile(
                classes[index].Version, classes[index].Signature,
                IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, 0),
                RecoveryOptions());
            Assert.That(profile.Header.ProfileClass,
                Is.EqualTo(classes[index].Kind));
            index = index + 1;
        }
    }

    [Test]
    public void HandBuiltHeadersCoverEveryDataSpaceAndRenderingIntent()
    {
        IIccProfileReader reader = new IccProfileReader();
        (uint Signature, IccColorSpaceKind Kind)[] spaces =
        {
            (IccColorSpaceSignatures.Xyz, IccColorSpaceKind.Xyz),
            (IccColorSpaceSignatures.Lab, IccColorSpaceKind.Lab),
            (IccColorSpaceSignatures.Luv, IccColorSpaceKind.Luv),
            (IccColorSpaceSignatures.YCbCr, IccColorSpaceKind.YCbCr),
            (IccColorSpaceSignatures.Yxy, IccColorSpaceKind.Yxy),
            (IccColorSpaceSignatures.Rgb, IccColorSpaceKind.Rgb),
            (IccColorSpaceSignatures.Gray, IccColorSpaceKind.Gray),
            (IccColorSpaceSignatures.Hsv, IccColorSpaceKind.Hsv),
            (IccColorSpaceSignatures.Hls, IccColorSpaceKind.Hls),
            (IccColorSpaceSignatures.Cmyk, IccColorSpaceKind.Cmyk),
            (IccColorSpaceSignatures.Cmy, IccColorSpaceKind.Cmy),
            (IccColorSpaceSignatures.NamedColor, IccColorSpaceKind.NamedColor),
            (IccColorSpaceSignatures.OneColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.TwoColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.ThreeColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.FourColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.FiveColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.SixColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.SevenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.EightColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.NineColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.TenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.ElevenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.TwelveColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.ThirteenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.FourteenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.FifteenColor, IccColorSpaceKind.DeviceChannels),
            (IccColorSpaceSignatures.ExtendedDevicePrefix | 16u, IccColorSpaceKind.ExtendedDevice),
            (IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u, IccColorSpaceKind.Spectral),
            (IccColorSpaceSignatures.TransmissionSpectralPrefix | 31u, IccColorSpaceKind.Spectral),
            (IccColorSpaceSignatures.RadiantSpectralPrefix | 31u, IccColorSpaceKind.Spectral),
            (IccColorSpaceSignatures.BiSpectralPrefix | 31u, IccColorSpaceKind.BiSpectral),
            (IccColorSpaceSignatures.SparseMatrixSpectralPrefix | 31u, IccColorSpaceKind.BiSpectral)
        };

        int index = 0;
        while (index < spaces.Length)
        {
            IccProfile profile = reader.ReadProfileBytes(CreateHeaderProfile(
                0x05000000, IccProfileClassSignatures.ColorSpace,
                spaces[index].Signature, IccColorSpaceSignatures.Xyz, 0),
                RecoveryOptions());
            Assert.That(profile.Header.DataColorSpace,
                Is.EqualTo(spaces[index].Kind));
            index = index + 1;
        }

        byte[] materialBytes = CreateHeaderProfile(0x05000000,
            IccProfileClassSignatures.MultiplexLink,
            IccColorSpaceSignatures.Rgb, 0, 0);
        IndependentBigEndian.WriteUInt32(materialBytes, 116,
            IccColorSpaceSignatures.MultiplexPrefix | 5u);
        IccProfile material = reader.ReadProfileBytes(materialBytes,
            RecoveryOptions());
        Assert.That(material.Header.MaterialConnectionSpace,
            Is.EqualTo(IccColorSpaceKind.Multiplex));

        int intent = 0;
        while (intent <= 3)
        {
            IccProfile profile = reader.ReadProfileBytes(CreateHeaderProfile(
                0x04400000, IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz,
                intent), RecoveryOptions());
            Assert.That(profile.Header.RenderingIntentKind,
                Is.EqualTo((IccRenderingIntent)intent));
            intent = intent + 1;
        }
    }

    [Test]
    public void HandBuiltCorpusContainsEveryRegisteredTagType()
    {
        FieldInfo[] fields = typeof(IccTypeSignatures).GetFields(
            BindingFlags.Public | BindingFlags.Static);
        IIccProfileReader reader = new IccProfileReader();

        Assert.That(fields, Has.Length.EqualTo(56));
        int index = 0;
        while (index < fields.Length)
        {
            uint signature = (uint)fields[index].GetRawConstantValue()!;
            byte[] bytes = CreateTaggedProfile(signature);
            IccProfile profile = reader.ReadProfileBytes(bytes,
                RecoveryOptions());
            IccTag tag = profile.Tags[0];

            Assert.Multiple(() =>
            {
                Assert.That(tag.IsDecodeAttempted, Is.True, fields[index].Name);
                Assert.That(tag.Data.RawData.HasTypeSignature, Is.True,
                    fields[index].Name);
                Assert.That(tag.Data.RawData.TypeSignature,
                    Is.EqualTo(signature), fields[index].Name);
                Assert.That(tag.Data.RawData.Bytes, Has.Length.GreaterThanOrEqualTo(8),
                    fields[index].Name);
            });
            index = index + 1;
        }
    }

    [Test]
    public void HandBuiltAmendedVersion4ProfilesDecodeCicpAndAdaptiveGainCurve()
    {
        IIccProfileReader reader = new IccProfileReader();
        IccProfile cicp = reader.ReadProfileBytes(CreateAmendedCicpProfile(),
            RecoveryOptions());
        IccProfile gain = reader.ReadProfileBytes(
            CreateAdaptiveGainCurveProfile(), RecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(cicp.Header.Version.PackedValue,
                Is.EqualTo(0x04400000));
            Assert.That(cicp.Tags[0].DecodeResult.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(cicp.Tags[0].Data.Kind,
                Is.EqualTo(IccTagDataKind.Cicp));
            Assert.That(cicp.Tags[0].Data.Cicp!.UsesProfilePrimaries,
                Is.True);
            Assert.That(gain.Header.Version.PackedValue,
                Is.EqualTo(0x04400000));
            Assert.That(gain.Tags[0].DecodeResult.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(gain.Tags[0].Data.Kind,
                Is.EqualTo(IccTagDataKind.AdaptiveGainCurve));
            Assert.That(gain.Tags[0].Data.AdaptiveGainCurve!.RedNodes,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void HandBuiltMalformedCorpusExercisesEveryProcessingElementSignature()
    {
        FieldInfo[] fields = typeof(IccProcessingElementSignatures).GetFields(
            BindingFlags.Public | BindingFlags.Static);
        IIccProfileReader reader = new IccProfileReader();

        Assert.That(fields, Has.Length.EqualTo(18));
        int index = 0;
        while (index < fields.Length)
        {
            uint signature = (uint)fields[index].GetRawConstantValue()!;
            byte[] bytes = CreateProcessingElementProfile(signature);
            IccProfile profile = reader.ReadProfileBytes(bytes,
                RecoveryOptions());
            IccTag tag = profile.Tags[0];

            Assert.Multiple(() =>
            {
                Assert.That(tag.IsDecodeAttempted, Is.True, fields[index].Name);
                Assert.That(tag.Data.RawData.TypeSignature,
                    Is.EqualTo(IccTypeSignatures.MultiProcessElements),
                    fields[index].Name);
                Assert.That(tag.DecodeResult.Status,
                    Is.Not.EqualTo(IccOperationStatus.Unspecified),
                    fields[index].Name);
                Assert.That(tag.Data.RawData.Bytes.Skip(24).Take(4),
                    Is.EqualTo(IndependentBigEndian.EncodeUInt32(signature)),
                    fields[index].Name);
            });
            index = index + 1;
        }
    }

    private static string CorpusDirectory()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..",
            "Corpus", "iccdev"));
    }

    private static IccReadOptions RecoveryOptions()
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ValidationMode = IccValidationMode.SecurityStrict;
        options.DecodeTagsLazily = false;
        return options;
    }

    private static byte[] CreateHeaderProfile(uint version,
        uint profileClass, uint dataSpace, uint connectionSpace, int intent)
    {
        return IccBinaryFixture.CreateProfile(version, profileClass,
            dataSpace, connectionSpace, intent);
    }

    private static byte[] CreateTaggedProfile(uint typeSignature)
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(typeSignature, 8);
        return IccBinaryFixture.CreateProfile(0x05000000,
            IccProfileClassSignatures.ColorSpace,
            IccColorSpaceSignatures.OneColor, IccColorSpaceSignatures.Xyz, 0,
            new IccBinaryTagFixture(0x74657374, payload));
    }

    private static byte[] CreateProcessingElementProfile(uint signature)
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.MultiProcessElements, 36);
        IndependentBigEndian.WriteUInt16(payload, 8, 1);
        IndependentBigEndian.WriteUInt16(payload, 10, 1);
        IndependentBigEndian.WriteUInt32(payload, 12, 1);
        IndependentBigEndian.WriteUInt32(payload, 16, 24);
        IndependentBigEndian.WriteUInt32(payload, 20, 12);
        IndependentBigEndian.WriteUInt32(payload, 24, signature);
        IndependentBigEndian.WriteUInt16(payload, 32, 1);
        IndependentBigEndian.WriteUInt16(payload, 34, 1);
        return IccBinaryFixture.CreateProfile(0x05000000,
            IccProfileClassSignatures.ColorSpace,
            IccColorSpaceSignatures.OneColor, IccColorSpaceSignatures.Xyz, 0,
            new IccBinaryTagFixture(0x74657374, payload));
    }

    private static byte[] CreateAmendedCicpProfile()
    {
        byte[] cicp = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.Cicp, 12);
        cicp[8] = 2;
        cicp[9] = 16;
        cicp[10] = 0;
        cicp[11] = 1;
        byte[] red = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.Xyz, 20);
        byte[] green = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.Xyz, 20);
        byte[] blue = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.Xyz, 20);
        return IccBinaryFixture.CreateProfile(0x04400000,
            IccProfileClassSignatures.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, 0,
            new IccBinaryTagFixture(IccTagSignatures.Cicp, cicp),
            new IccBinaryTagFixture(IccTagSignatures.RedMatrixColumn, red),
            new IccBinaryTagFixture(IccTagSignatures.GreenMatrixColumn, green),
            new IccBinaryTagFixture(IccTagSignatures.BlueMatrixColumn, blue));
    }

    private static byte[] CreateAdaptiveGainCurveProfile()
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.AdaptiveGainCurve, 156);
        IndependentBigEndian.WriteUInt32(payload, 8, 1);
        IndependentBigEndian.WriteUInt32(payload, 100, 128);
        IndependentBigEndian.WriteUInt32(payload, 104, 28);
        IndependentBigEndian.WriteUInt32(payload, 108, 128);
        IndependentBigEndian.WriteUInt32(payload, 112, 28);
        IndependentBigEndian.WriteUInt32(payload, 116, 128);
        IndependentBigEndian.WriteUInt32(payload, 120, 28);
        IndependentBigEndian.WriteUInt32(payload, 128, 2);
        IndependentBigEndian.WriteSingle(payload, 140, 1.0f);
        IndependentBigEndian.WriteSingle(payload, 144, 1.0f);
        IndependentBigEndian.WriteSingle(payload, 148, 1.0f);
        IndependentBigEndian.WriteSingle(payload, 152, 1.0f);
        return IccBinaryFixture.CreateProfile(0x04400000,
            IccProfileClassSignatures.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, 0,
            new IccBinaryTagFixture(
                IccTagSignatures.AdaptiveGainCurve, payload));
    }
}
