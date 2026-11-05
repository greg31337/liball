using icc;

namespace icc.Tests;

public class HeaderAndTagTableCoverageTests
{
    [TestCase(0x02190000u, 2, 1, 9, 0, 0, IccVersionKind.Version2)]
    [TestCase(0x04490000u, 4, 4, 9, 0, 0, IccVersionKind.Version4)]
    [TestCase(0x050901ffu, 5, 0, 9, 1, 255, IccVersionKind.Version5)]
    [TestCase(0x0509ff01u, 5, 0, 9, 255, 1, IccVersionKind.Version5)]
    public void PackedVersionFieldsAreDecodedIndependently(uint packed,
        int major, int minor, int bugFix, int subclassMajor,
        int subclassMinor, IccVersionKind kind)
    {
        byte[] bytes = IccBinaryFixture.CreateProfile(packed,
            0x6d6e7472, 0x52474220, 0x58595a20, 0);

        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(bytes, 0,
            IccReadMode.Recovery);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.PackedValue, Is.EqualTo(packed));
            Assert.That(header.Version.Kind, Is.EqualTo(kind));
            Assert.That(header.Version.Major, Is.EqualTo(major));
            Assert.That(header.Version.Minor, Is.EqualTo(minor));
            Assert.That(header.Version.BugFix, Is.EqualTo(bugFix));
            Assert.That(header.Version.SubclassMajor,
                Is.EqualTo(subclassMajor));
            Assert.That(header.Version.SubclassMinor,
                Is.EqualTo(subclassMinor));
        });
    }

    [Test]
    public void EveryClassDataSpacePcsAndIntentCombinationIsDecoded()
    {
        (uint Signature, IccProfileClass Kind)[] classes = ProfileClasses();
        (uint Signature, IccColorSpaceKind Kind)[] spaces = DataSpaces();
        (uint Signature, IccPcsKind Kind)[] connectionSpaces =
        {
            (0x58595a20, IccPcsKind.Xyz),
            (0x4c616220, IccPcsKind.Lab)
        };
        int caseCount = 0;

        foreach ((uint classSignature, IccProfileClass classKind) in classes)
        {
            foreach ((uint spaceSignature, IccColorSpaceKind spaceKind)
                in spaces)
            {
                foreach ((uint pcsSignature, IccPcsKind pcsKind)
                    in connectionSpaces)
                {
                    int intent = 0;
                    while (intent <= 3)
                    {
                        byte[] bytes = IccBinaryFixture.CreateProfile(
                            0x05000000, classSignature, spaceSignature,
                            pcsSignature, intent);
                        IccProfileHeader header = HeaderCodec.ReadHeader(
                            bytes, 0);

                        Assert.That(header.ProfileClass,
                            Is.EqualTo(classKind), "class signature");
                        Assert.That(header.DataColorSpace,
                            Is.EqualTo(spaceKind), "data-space signature");
                        Assert.That(header.ProfileConnectionSpace,
                            Is.EqualTo(pcsKind), "PCS signature");
                        Assert.That(header.RenderingIntent,
                            Is.EqualTo(intent), "rendering-intent integer");
                        Assert.That(header.RenderingIntentKind,
                            Is.EqualTo((IccRenderingIntent)intent),
                            "rendering-intent kind");
                        caseCount++;
                        intent++;
                    }
                }
            }
        }

        Assert.That(caseCount, Is.EqualTo(2904));
    }

    [TestCase(0x58595a20u, 0u, 0u, IccPcsKind.Xyz)]
    [TestCase(0x4c616220u, 0u, 0u, IccPcsKind.Lab)]
    [TestCase(0u, 0x7273001fu, 0u, IccPcsKind.Spectral)]
    [TestCase(0u, 0x6273001fu, 0u, IccPcsKind.BiSpectral)]
    [TestCase(0u, 0x736d001fu, 0u, IccPcsKind.BiSpectral)]
    [TestCase(0u, 0u, 0x6d630005u, IccPcsKind.Material)]
    [TestCase(0u, 0u, 0u, IccPcsKind.None)]
    public void EveryConnectionSpaceRouteHasStablePrecedence(uint pcs,
        uint spectral, uint material, IccPcsKind expected)
    {
        byte[] bytes = IccBinaryFixture.CreateProfile(0x05000000,
            0x6d6c6e6b, 0x52474220, pcs, 0);
        IndependentBigEndian.WriteUInt32(bytes, 100, spectral);
        IndependentBigEndian.WriteUInt32(bytes, 116, material);

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 0);

        Assert.That(header.ProfileConnectionSpace, Is.EqualTo(expected));
    }

    [Test]
    public void EveryPayloadPaddingLengthAndDirectoryOrderIsPreserved()
    {
        IccBinaryTagFixture[] tags = new IccBinaryTagFixture[]
        {
            Tag(0x74303031, 8),
            Tag(0x74303032, 9),
            Tag(0x74303033, 10),
            Tag(0x74303034, 11)
        };
        byte[] bytes = IccBinaryFixture.CreateProfile(0x04400000,
            0x6d6e7472, 0x52474220, 0x58595a20, 0, tags);

        IccProfile profile = new IccProfileReader().ReadProfileBytes(bytes,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(224));
            Assert.That(profile.TagTableEntries.Select(entry => entry.Signature),
                Is.EqualTo(new uint[]
                {
                    0x74303031, 0x74303032, 0x74303033, 0x74303034
                }));
            Assert.That(profile.TagTableEntries.Select(entry => entry.Offset),
                Is.EqualTo(new long[] { 180, 188, 200, 212 }));
            Assert.That(profile.TagTableEntries.Select(entry => entry.Size),
                Is.EqualTo(new int[] { 8, 9, 10, 11 }));
            Assert.That(profile.Tags.Select(tag => tag.PaddingBytes.Length),
                Is.EqualTo(new int[] { 0, 3, 2, 1 }));
        });
    }

    [TestCase(0x80000000u)]
    [TestCase(uint.MaxValue)]
    public void UnsignedPayloadOffsetsCannotOverflowTheManagedModel(
        uint encodedOffset)
    {
        byte[] bytes = CreateSingleEntryProfile(152);
        WriteEntry(bytes, 0x63707274, encodedOffset, 8);
        IIccProfileReader reader = new IccProfileReader();

        IccProfileFormatException exception =
            Assert.Throws<IccProfileFormatException>(() =>
                reader.ReadProfileBytes(bytes, new IccReadOptions()))!;
        IccOperationResult probe = reader.ProbeProfileBytes(bytes,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message,
                Does.Contain("payload offset").And.Contain(
                    encodedOffset.ToString()));
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(probe.Location.Offset, Is.EqualTo(136));
        });
    }

    [TestCase(0x80000000u)]
    [TestCase(uint.MaxValue)]
    public void UnsignedPayloadSizesCannotOverflowTheManagedModel(
        uint encodedSize)
    {
        byte[] bytes = CreateSingleEntryProfile(152);
        WriteEntry(bytes, 0x63707274, 144, encodedSize);
        IIccProfileReader reader = new IccProfileReader();

        IccProfileFormatException exception =
            Assert.Throws<IccProfileFormatException>(() =>
                reader.ReadProfileBytes(bytes, new IccReadOptions()))!;
        IccOperationResult probe = reader.ProbeProfileBytes(bytes,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message,
                Does.Contain("payload size").And.Contain(
                    encodedSize.ToString()));
            Assert.That(probe.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(probe.Location.Offset, Is.EqualTo(140));
        });
    }

    [Test]
    public void AlignmentPaddingCannotExtendPastTheDeclaredProfile()
    {
        byte[] bytes = CreateSingleEntryProfile(153);
        WriteEntry(bytes, 0x63707274, 144, 9);
        IndependentBigEndian.WriteUInt32(bytes, 144, 0x74657874);
        IIccProfileReader reader = new IccProfileReader();

        IccProfileFormatException exception =
            Assert.Throws<IccProfileFormatException>(() =>
                reader.ReadProfileBytes(bytes, new IccReadOptions()))!;
        IccOperationResult probe = reader.ProbeProfileBytes(bytes,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("align"));
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(probe.Message, Does.Contain("alignment padding"));
        });
    }

    [Test]
    public void SharedPayloadsUseOneIdentityWithoutChangingDirectoryOrder()
    {
        byte[] bytes = CreateRawProfile(176, 3);
        WriteEntry(bytes, 0, 0x74303033, 168, 8);
        WriteEntry(bytes, 1, 0x74303031, 168, 8);
        WriteEntry(bytes, 2, 0x74303032, 168, 8);
        IndependentBigEndian.WriteUInt32(bytes, 168, 0x74657874);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Conforming;

        IccProfile profile = new IccProfileReader().ReadProfileBytes(bytes,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags.Select(tag => tag.Signature),
                Is.EqualTo(new uint[]
                {
                    0x74303033, 0x74303031, 0x74303032
                }));
            Assert.That(profile.TagTableEntries.Select(entry =>
                entry.SharedDataId), Is.EqualTo(new int[] { 0, 0, 0 }));
            Assert.That(profile.Tags[0].DecodeState,
                Is.SameAs(profile.Tags[1].DecodeState));
            Assert.That(profile.Tags[1].DecodeState,
                Is.SameAs(profile.Tags[2].DecodeState));
        });
    }

    private static (uint Signature, IccProfileClass Kind)[] ProfileClasses()
    {
        return new (uint, IccProfileClass)[]
        {
            (0x73636e72, IccProfileClass.Input),
            (0x6d6e7472, IccProfileClass.Display),
            (0x70727472, IccProfileClass.Output),
            (0x6c696e6b, IccProfileClass.DeviceLink),
            (0x73706163, IccProfileClass.ColorSpace),
            (0x61627374, IccProfileClass.Abstract),
            (0x6e6d636c, IccProfileClass.NamedColor),
            (0x63656e63, IccProfileClass.ColorEncodingSpace),
            (0x6d696420, IccProfileClass.MultiplexIdentification),
            (0x6d6c6e6b, IccProfileClass.MultiplexLink),
            (0x6d766973, IccProfileClass.MultiplexVisualization)
        };
    }

    private static (uint Signature, IccColorSpaceKind Kind)[] DataSpaces()
    {
        return new (uint, IccColorSpaceKind)[]
        {
            (0x58595a20, IccColorSpaceKind.Xyz),
            (0x4c616220, IccColorSpaceKind.Lab),
            (0x4c757620, IccColorSpaceKind.Luv),
            (0x59436272, IccColorSpaceKind.YCbCr),
            (0x59787920, IccColorSpaceKind.Yxy),
            (0x52474220, IccColorSpaceKind.Rgb),
            (0x47524159, IccColorSpaceKind.Gray),
            (0x48535620, IccColorSpaceKind.Hsv),
            (0x484c5320, IccColorSpaceKind.Hls),
            (0x434d594b, IccColorSpaceKind.Cmyk),
            (0x434d5920, IccColorSpaceKind.Cmy),
            (0x6e6d636c, IccColorSpaceKind.NamedColor),
            (0x31434c52, IccColorSpaceKind.DeviceChannels),
            (0x32434c52, IccColorSpaceKind.DeviceChannels),
            (0x33434c52, IccColorSpaceKind.DeviceChannels),
            (0x34434c52, IccColorSpaceKind.DeviceChannels),
            (0x35434c52, IccColorSpaceKind.DeviceChannels),
            (0x36434c52, IccColorSpaceKind.DeviceChannels),
            (0x37434c52, IccColorSpaceKind.DeviceChannels),
            (0x38434c52, IccColorSpaceKind.DeviceChannels),
            (0x39434c52, IccColorSpaceKind.DeviceChannels),
            (0x41434c52, IccColorSpaceKind.DeviceChannels),
            (0x42434c52, IccColorSpaceKind.DeviceChannels),
            (0x43434c52, IccColorSpaceKind.DeviceChannels),
            (0x44434c52, IccColorSpaceKind.DeviceChannels),
            (0x45434c52, IccColorSpaceKind.DeviceChannels),
            (0x46434c52, IccColorSpaceKind.DeviceChannels),
            (0x6e630010, IccColorSpaceKind.ExtendedDevice),
            (0x7273001f, IccColorSpaceKind.Spectral),
            (0x7473001f, IccColorSpaceKind.Spectral),
            (0x6573001f, IccColorSpaceKind.Spectral),
            (0x6273001f, IccColorSpaceKind.BiSpectral),
            (0x736d001f, IccColorSpaceKind.BiSpectral)
        };
    }

    private static IccBinaryTagFixture Tag(uint signature, int length)
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x74657874,
            length);
        return new IccBinaryTagFixture(signature, payload);
    }

    private static byte[] CreateSingleEntryProfile(int declaredSize)
    {
        return CreateRawProfile(declaredSize, 1);
    }

    private static byte[] CreateRawProfile(int declaredSize, uint tagCount)
    {
        byte[] bytes = new byte[declaredSize];
        IndependentBigEndian.WriteUInt32(bytes, 0, (uint)declaredSize);
        IndependentBigEndian.WriteUInt32(bytes, 8, 0x04400000);
        IndependentBigEndian.WriteUInt32(bytes, 12, 0x6d6e7472);
        IndependentBigEndian.WriteUInt32(bytes, 16, 0x52474220);
        IndependentBigEndian.WriteUInt32(bytes, 20, 0x58595a20);
        IndependentBigEndian.WriteUInt16(bytes, 24, 2026);
        IndependentBigEndian.WriteUInt16(bytes, 26, 1);
        IndependentBigEndian.WriteUInt16(bytes, 28, 1);
        IndependentBigEndian.WriteUInt32(bytes, 36, 0x61637370);
        IndependentBigEndian.WriteUInt32(bytes, 68, 0x0000f6d6);
        IndependentBigEndian.WriteUInt32(bytes, 72, 0x00010000);
        IndependentBigEndian.WriteUInt32(bytes, 76, 0x0000d32d);
        IndependentBigEndian.WriteUInt32(bytes, 128, tagCount);
        return bytes;
    }

    private static void WriteEntry(byte[] bytes, uint signature,
        uint payloadOffset, uint payloadSize)
    {
        WriteEntry(bytes, 0, signature, payloadOffset, payloadSize);
    }

    private static void WriteEntry(byte[] bytes, int index, uint signature,
        uint payloadOffset, uint payloadSize)
    {
        int offset = 132 + (index * 12);
        IndependentBigEndian.WriteUInt32(bytes, offset, signature);
        IndependentBigEndian.WriteUInt32(bytes, offset + 4, payloadOffset);
        IndependentBigEndian.WriteUInt32(bytes, offset + 8, payloadSize);
    }
}
