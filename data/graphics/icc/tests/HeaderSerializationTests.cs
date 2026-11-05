using icc;

namespace icc.Tests;

public class HeaderSerializationTests
{
    [TestCase(IccVersionKind.Version2, 2, 1)]
    [TestCase(IccVersionKind.Version4, 4, 4)]
    public void Icc1HeadersSerializeCommonFieldsAndZeroVersionSpecificReservedRegions(IccVersionKind kind, int major, int minor)
    {
        IccProfileHeader header = CreateHeader(kind, major, minor);
        IccWriteOptions options = EditableCreationOptions(IccWriteMode.Canonical);

        byte[] bytes = HeaderCodec.SerializeHeader(header, options, 256);
        IccProfileHeader decoded = HeaderCodec.ReadHeader(bytes, 0);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.DeclaredSize, Is.EqualTo(256));
            Assert.That(decoded.Version.Kind, Is.EqualTo(kind));
            Assert.That(decoded.Version.Minor, Is.EqualTo(minor));
            Assert.That(decoded.CreationDateTime.Year, Is.EqualTo(2030));
            Assert.That(decoded.ProfileFileSignature, Is.EqualTo(IccConstants.ProfileFileSignature));
            Assert.That(decoded.ProfileFlags, Is.EqualTo(0x80000003));
            Assert.That(decoded.IsEmbedded, Is.True);
            Assert.That(decoded.RequiresEmbeddedData, Is.True);
            Assert.That(decoded.DeviceAttributes, Is.EqualTo(0x8000000000000009));
            Assert.That(decoded.PcsIlluminantX, Is.EqualTo(HeaderCodec.D50IlluminantX));
            Assert.That(decoded.PcsIlluminantY, Is.EqualTo(HeaderCodec.D50IlluminantY));
            Assert.That(decoded.PcsIlluminantZ, Is.EqualTo(HeaderCodec.D50IlluminantZ));
            Assert.That(bytes.Skip(kind == IccVersionKind.Version2 ? 84 : 100), Is.All.Zero);
        });

        if (kind == IccVersionKind.Version2)
        {
            Assert.That(decoded.ProfileId.IsPresent, Is.False);
        }
        else
        {
            Assert.That(decoded.ProfileId.Bytes, Is.EqualTo(header.ProfileId.Bytes));
        }
    }

    [Test]
    public void Icc2HeaderRoundTripsFlagsProfileIdAndSpectralContext()
    {
        IccProfileHeader header = CreateHeader(IccVersionKind.Version5, 5, 0);
        header.Version.SubclassMajor = 2;
        header.Version.SubclassMinor = 3;
        header.ProfileFlags = 0x80000000;
        header.RequiresMcsSubset = true;
        header.UsesExtendedPcsRange = true;
        header.SpectralPcsSignature = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 36;
        header.SpectralPcsRangeStart = 380.0;
        header.SpectralPcsRangeEnd = 730.0;
        header.SpectralPcsRangeSteps = 36;
        header.BiSpectralPcsRangeStart = 400.5;
        header.BiSpectralPcsRangeEnd = 700.0;
        header.BiSpectralPcsRangeSteps = 31;
        header.MaterialConnectionSpaceSignature = IccColorSpaceSignatures.MultiplexPrefix | 5;
        header.ProfileSubclassSignature = 0x53554243;
        header.IccMaxReservedValue = 0x12345678;

        byte[] bytes = HeaderCodec.SerializeHeader(header, EditableCreationOptions(IccWriteMode.Canonical), 1024);
        IccProfileHeader decoded = HeaderCodec.ReadHeader(bytes, 0);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Version.PackedValue, Is.EqualTo(0x05000203));
            Assert.That(decoded.ProfileFlags, Is.EqualTo(0x8000000f));
            Assert.That(decoded.ProfileId.Bytes, Is.EqualTo(header.ProfileId.Bytes));
            Assert.That(decoded.SpectralPcsSignature, Is.EqualTo(header.SpectralPcsSignature));
            Assert.That(decoded.SpectralPcsRangeStart, Is.EqualTo(380.0));
            Assert.That(decoded.SpectralPcsRangeEnd, Is.EqualTo(730.0));
            Assert.That(decoded.SpectralPcsRangeSteps, Is.EqualTo(36));
            Assert.That(decoded.BiSpectralPcsRangeStart, Is.EqualTo(400.5));
            Assert.That(decoded.BiSpectralPcsRangeEnd, Is.EqualTo(700.0));
            Assert.That(decoded.BiSpectralPcsRangeSteps, Is.EqualTo(31));
            Assert.That(decoded.MaterialConnectionSpaceSignature, Is.EqualTo(header.MaterialConnectionSpaceSignature));
            Assert.That(decoded.ProfileSubclassSignature, Is.EqualTo(0x53554243));
            Assert.That(decoded.IccMaxReservedValue, Is.Zero);
            Assert.That(bytes.Skip(124), Is.All.Zero);
            Assert.That(decoded.ValidationIssues, Is.Empty);
        });
    }

    [Test]
    public void CreationTimeAndReservedAttributeBitsFollowCallerWritePolicy()
    {
        IccProfileHeader header = CreateHeader(IccVersionKind.Version4, 4, 3);
        WriteDateTime(header.RawBytes, 24, 2020, 1, 2, 3, 4, 5);
        IccWriteOptions preserve = new IccWriteOptions();
        IccWriteOptions editCanonical = EditableCreationOptions(IccWriteMode.Canonical);
        IccWriteOptions editPreservation = EditableCreationOptions(IccWriteMode.Preservation);

        byte[] preserved = HeaderCodec.SerializeHeader(header, preserve, 132);
        byte[] canonical = HeaderCodec.SerializeHeader(header, editCanonical, 132);
        byte[] preservation = HeaderCodec.SerializeHeader(header, editPreservation, 132);
        IccProfile profile = new IccProfile();
        profile.Header = header;
        byte[] profileBytes = new IccProfileWriter().WriteProfileBytes(profile, editCanonical);

        Assert.Multiple(() =>
        {
            Assert.That(HeaderCodec.ReadHeader(preserved, 0).CreationDateTime.Year, Is.EqualTo(2020));
            Assert.That(HeaderCodec.ReadHeader(canonical, 0).CreationDateTime.Year, Is.EqualTo(2030));
            Assert.That(ReadUInt64(canonical, 56), Is.EqualTo(0x8000000000000009));
            Assert.That(ReadUInt64(preservation, 56), Is.EqualTo(0x8000000080000009));
            Assert.That(profileBytes, Has.Length.EqualTo(132));
            Assert.That(HeaderCodec.ReadHeader(profileBytes, 0).PreferredCmmSignature, Is.EqualTo(header.PreferredCmmSignature));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(header, editCanonical, 131));
        });
    }

    [Test]
    public void HeaderWriterRejectsUnencodableVersionsTimesIdsAndIcc2Context()
    {
        IccProfileHeader missingSource = CreateHeader(IccVersionKind.Version4, 4, 3);
        missingSource.RawBytes = new byte[0];
        IccProfileHeader badTime = CreateHeader(IccVersionKind.Version4, 4, 3);
        badTime.CreationDateTime.Month = 13;
        IccProfileHeader futureVersion = CreateHeader(IccVersionKind.Version4, 4, 5);
        IccProfileHeader badId = CreateHeader(IccVersionKind.Version4, 4, 3);
        badId.ProfileId.Bytes = new byte[15];
        IccProfileHeader badSubclass = CreateHeader(IccVersionKind.Version5, 5, 0);
        badSubclass.Version.SubclassMajor = 1;
        IccProfileHeader badRange = CreateHeader(IccVersionKind.Version5, 5, 0);
        badRange.SpectralPcsRangeSteps = 65536;
        IccWriteOptions editable = EditableCreationOptions(IccWriteMode.Canonical);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(missingSource, new IccWriteOptions(), 132));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(badTime, editable, 132));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(futureVersion, editable, 132));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(badId, editable, 132));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(badSubclass, editable, 132));
            Assert.Throws<IccProfileWriteException>(() => HeaderCodec.SerializeHeader(badRange, editable, 132));
        });
    }

    private static IccProfileHeader CreateHeader(IccVersionKind kind, int major, int minor)
    {
        IccProfileHeader header = new IccProfileHeader();
        header.Version.Kind = kind;
        header.Version.Major = major;
        header.Version.Minor = minor;
        header.Version.BugFix = 0;
        header.PreferredCmmSignature = 0x434d4d20;
        header.ProfileClassSignature = IccProfileClassSignatures.Display;
        header.ProfileClass = IccProfileClass.Display;
        header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        header.DataColorSpace = IccColorSpaceKind.Rgb;
        header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        header.ProfileConnectionSpace = IccPcsKind.Xyz;
        SetDateTime(header.CreationDateTime, 2030, 6, 7, 8, 9, 10);
        header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        header.PrimaryPlatformSignature = IccConstants.MacintoshPlatform;
        header.ProfileFlags = 0x80000000;
        header.IsEmbedded = true;
        header.RequiresEmbeddedData = true;
        header.DeviceManufacturerSignature = 0x4d414e55;
        header.DeviceModelSignature = 0x4d4f444c;
        header.DeviceAttributes = 0x8000000080000009;
        header.RenderingIntent = 3;
        header.RenderingIntentKind = IccRenderingIntent.IccAbsoluteColorimetric;
        header.PcsIlluminantX = HeaderCodec.D50IlluminantX;
        header.PcsIlluminantY = HeaderCodec.D50IlluminantY;
        header.PcsIlluminantZ = HeaderCodec.D50IlluminantZ;
        header.ProfileCreatorSignature = 0x43524541;
        int index = 0;
        while (index < header.ProfileId.Bytes.Length)
        {
            header.ProfileId.Bytes[index] = (byte)(index + 1);
            index = index + 1;
        }

        header.ProfileId.IsPresent = true;
        return header;
    }

    private static IccWriteOptions EditableCreationOptions(IccWriteMode mode)
    {
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = mode;
        options.PreserveOriginalCreationDateTime = false;
        return options;
    }

    private static void SetDateTime(IccDateTime value, int year, int month, int day, int hour, int minute, int second)
    {
        value.Year = year;
        value.Month = month;
        value.Day = day;
        value.Hour = hour;
        value.Minute = minute;
        value.Second = second;
    }

    private static void WriteDateTime(byte[] destination, int offset, int year, int month, int day, int hour, int minute, int second)
    {
        WriteUInt16(destination, offset, year);
        WriteUInt16(destination, offset + 2, month);
        WriteUInt16(destination, offset + 4, day);
        WriteUInt16(destination, offset + 6, hour);
        WriteUInt16(destination, offset + 8, minute);
        WriteUInt16(destination, offset + 10, second);
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static ulong ReadUInt64(byte[] source, int offset)
    {
        return ((ulong)source[offset] << 56) | ((ulong)source[offset + 1] << 48) | ((ulong)source[offset + 2] << 40) | ((ulong)source[offset + 3] << 32)
            | ((ulong)source[offset + 4] << 24) | ((ulong)source[offset + 5] << 16) | ((ulong)source[offset + 6] << 8) | source[offset + 7];
    }
}
