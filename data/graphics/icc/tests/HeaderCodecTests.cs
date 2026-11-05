using icc;

namespace icc.Tests;

public class HeaderCodecTests
{
    [Test]
    public void Version4HeaderDecodesEveryFieldAndOwnsRawBytes()
    {
        byte[] bytes = CreateHeader(0x04300000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        int index = 0;
        while (index < 16)
        {
            bytes[84 + index] = (byte)(index + 1);
            index = index + 1;
        }

        byte[] original = bytes.ToArray();
        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 1000L);
        bytes[0] = 0;
        bytes[84] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(header.DeclaredSize, Is.EqualTo(512));
            Assert.That(header.PreferredCmmSignature, Is.EqualTo(0x434d4d20));
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Version4));
            Assert.That(header.Version.Major, Is.EqualTo(4));
            Assert.That(header.Version.Minor, Is.EqualTo(3));
            Assert.That(header.Version.BugFix, Is.Zero);
            Assert.That(header.Version.IsSupported, Is.True);
            Assert.That(header.Version.IsFutureMinor, Is.False);
            Assert.That(header.Version.PackedValue, Is.EqualTo(0x04300000));
            Assert.That(header.ProfileClassSignature, Is.EqualTo(IccProfileClassSignatures.Display));
            Assert.That(header.ProfileClass, Is.EqualTo(IccProfileClass.Display));
            Assert.That(header.DataColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Rgb));
            Assert.That(header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Xyz));
            Assert.That(header.CreationDateTime.Year, Is.EqualTo(2026));
            Assert.That(header.CreationDateTime.Month, Is.EqualTo(9));
            Assert.That(header.CreationDateTime.Day, Is.EqualTo(11));
            Assert.That(header.CreationDateTime.Hour, Is.EqualTo(14));
            Assert.That(header.CreationDateTime.Minute, Is.EqualTo(35));
            Assert.That(header.CreationDateTime.Second, Is.EqualTo(59));
            Assert.That(header.ProfileFileSignature, Is.EqualTo(IccConstants.ProfileFileSignature));
            Assert.That(header.PrimaryPlatformSignature, Is.EqualTo(IccConstants.MacintoshPlatform));
            Assert.That(header.ProfileFlags, Is.EqualTo(3));
            Assert.That(header.IsEmbedded, Is.True);
            Assert.That(header.RequiresEmbeddedData, Is.True);
            Assert.That(header.RequiresMcsSubset, Is.False);
            Assert.That(header.UsesExtendedPcsRange, Is.False);
            Assert.That(header.DeviceManufacturerSignature, Is.EqualTo(0x4d414e55));
            Assert.That(header.DeviceModelSignature, Is.EqualTo(0x4d4f444c));
            Assert.That(header.DeviceAttributes, Is.EqualTo(0x0102030405060708));
            Assert.That(header.RenderingIntent, Is.EqualTo(3));
            Assert.That(header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.IccAbsoluteColorimetric));
            Assert.That(header.PcsIlluminantX, Is.EqualTo(0x0000f6d6));
            Assert.That(header.PcsIlluminantY, Is.EqualTo(0x00010000));
            Assert.That(header.PcsIlluminantZ, Is.EqualTo(0x0000d32d));
            Assert.That(header.ProfileCreatorSignature, Is.EqualTo(0x43524541));
            Assert.That(header.ProfileId.Bytes, Is.EqualTo(original[84..100]));
            Assert.That(header.ProfileId.IsPresent, Is.True);
            Assert.That(header.ContextualBytes, Is.EqualTo(new byte[28]));
            Assert.That(header.RawBytes, Is.EqualTo(original).And.Not.SameAs(bytes));
            Assert.That(header.ValidationIssues, Is.Empty);
        });
    }

    [Test]
    public void Version2TreatsTheEntireTailAsReservedAndRetainsItsBytes()
    {
        byte[] bytes = CreateHeader(0x02100000, IccProfileClassSignatures.Output, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab);
        bytes[10] = 0x12;
        bytes[84] = 0x34;
        bytes[127] = 0x56;

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 500L);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Version2));
            Assert.That(header.ProfileClass, Is.EqualTo(IccProfileClass.Output));
            Assert.That(header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Cmyk));
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Lab));
            Assert.That(header.ProfileId.Bytes[0], Is.EqualTo(0x34));
            Assert.That(header.ProfileId.IsPresent, Is.False);
            Assert.That(header.ContextualBytes[27], Is.EqualTo(0x56));
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(2));
            Assert.That(header.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderReserved));
            Assert.That(header.ValidationIssues[0].Location.Offset, Is.EqualTo(510));
            Assert.That(header.ValidationIssues[0].Location.Length, Is.EqualTo(2));
            Assert.That(header.ValidationIssues[1].Location.Offset, Is.EqualTo(584));
            Assert.That(header.ValidationIssues[1].Location.Length, Is.EqualTo(44));
        });
    }

    [Test]
    public void Version4ProfileIdIsNotReservedButFinalHeaderBytesAre()
    {
        byte[] bytes = CreateHeader(0x04200000, IccProfileClassSignatures.Input, IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        bytes[99] = 1;
        bytes[127] = 2;

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 20L);

        Assert.Multiple(() =>
        {
            Assert.That(header.ProfileId.IsPresent, Is.True);
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(header.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Warning));
            Assert.That(header.ValidationIssues[0].Location.Offset, Is.EqualTo(120));
            Assert.That(header.ValidationIssues[0].Location.Length, Is.EqualTo(28));
            Assert.That(header.ValidationIssues[0].Remediation, Does.Contain("zero"));
        });
    }

    [Test]
    public void UnknownSignaturesRetainRawValuesWithoutFalseClassifications()
    {
        byte[] bytes = CreateHeader(0x04100000, 0x3f3f3f3f, 0x41424344, 0x45464748);
        WriteUInt32(bytes, 64, 0xffffffff);

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 0L);

        Assert.Multiple(() =>
        {
            Assert.That(header.ProfileClassSignature, Is.EqualTo(0x3f3f3f3f));
            Assert.That(header.ProfileClass, Is.EqualTo(IccProfileClass.Unknown));
            Assert.That(header.DataColorSpaceSignature, Is.EqualTo(0x41424344));
            Assert.That(header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(header.ProfileConnectionSpaceSignature, Is.EqualTo(0x45464748));
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.None));
            Assert.That(header.RenderingIntent, Is.EqualTo(-1));
            Assert.That((int)header.RenderingIntentKind, Is.EqualTo(-1));
        });
    }

    [Test]
    public void HeaderCloneOwnsDecodedDiagnostics()
    {
        byte[] bytes = CreateHeader(0x04200000, IccProfileClassSignatures.Input, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        bytes[100] = 1;
        IccProfileHeader source = HeaderCodec.ReadHeader(bytes, 75L);

        IccProfileHeader clone = ProfileClone.CloneHeaderStandalone(source);
        clone.ValidationIssues[0].Location.Offset = 0;

        Assert.Multiple(() =>
        {
            Assert.That(clone.ValidationIssues, Is.Not.SameAs(source.ValidationIssues));
            Assert.That(clone.ValidationIssues[0], Is.Not.SameAs(source.ValidationIssues[0]));
            Assert.That(clone.ValidationIssues[0].Location, Is.Not.SameAs(source.ValidationIssues[0].Location));
            Assert.That(source.ValidationIssues[0].Location.Offset, Is.EqualTo(175));
        });
    }

    [Test]
    public void Version5HeaderDecodesExtendedFieldsAndRetainsContextualBytes()
    {
        byte[] bytes = CreateHeader(0x05000203, IccProfileClassSignatures.MultiplexVisualization, IccColorSpaceSignatures.ExtendedDevicePrefix | 20, 0);
        WriteUInt32(bytes, 44, 15);
        bytes[84] = 1;
        WriteUInt32(bytes, 100, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 36);
        WriteSpectralRange(bytes, 104, 380.0f, 730.0f, 36);
        WriteSpectralRange(bytes, 110, 400.5f, 700.0f, 31);
        WriteUInt32(bytes, 116, IccColorSpaceSignatures.MultiplexPrefix | 5);
        WriteUInt32(bytes, 120, 0x53554243);
        byte[] expectedContext = bytes[100..128];

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 200L);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Version5));
            Assert.That(header.Version.Major, Is.EqualTo(5));
            Assert.That(header.Version.Minor, Is.Zero);
            Assert.That(header.Version.BugFix, Is.Zero);
            Assert.That(header.Version.SubclassMajor, Is.EqualTo(2));
            Assert.That(header.Version.SubclassMinor, Is.EqualTo(3));
            Assert.That(header.Version.IsSupported, Is.True);
            Assert.That(header.Version.IsFutureMinor, Is.False);
            Assert.That(header.ProfileClass, Is.EqualTo(IccProfileClass.MultiplexVisualization));
            Assert.That(header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.ExtendedDevice));
            Assert.That(header.IsEmbedded, Is.True);
            Assert.That(header.RequiresEmbeddedData, Is.True);
            Assert.That(header.RequiresMcsSubset, Is.True);
            Assert.That(header.UsesExtendedPcsRange, Is.True);
            Assert.That(header.ProfileId.IsPresent, Is.True);
            Assert.That(header.SpectralPcsSignature, Is.EqualTo(IccColorSpaceSignatures.ReflectanceSpectralPrefix | 36));
            Assert.That(header.SpectralPcs, Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(header.SpectralPcsRangeStart, Is.EqualTo(380.0));
            Assert.That(header.SpectralPcsRangeEnd, Is.EqualTo(730.0));
            Assert.That(header.SpectralPcsRangeSteps, Is.EqualTo(36));
            Assert.That(header.BiSpectralPcsRangeStart, Is.EqualTo(400.5));
            Assert.That(header.BiSpectralPcsRangeEnd, Is.EqualTo(700.0));
            Assert.That(header.BiSpectralPcsRangeSteps, Is.EqualTo(31));
            Assert.That(header.MaterialConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.MultiplexPrefix | 5));
            Assert.That(header.MaterialConnectionSpace, Is.EqualTo(IccColorSpaceKind.Multiplex));
            Assert.That(header.ProfileSubclassSignature, Is.EqualTo(0x53554243));
            Assert.That(header.IccMaxReservedValue, Is.Zero);
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Spectral));
            Assert.That(header.ContextualBytes, Is.EqualTo(expectedContext));
            Assert.That(header.ValidationIssues, Is.Empty);
        });
    }

    [TestCase(IccColorSpaceSignatures.BiSpectralPrefix | 31, 0u, IccPcsKind.BiSpectral)]
    [TestCase(0u, IccColorSpaceSignatures.MultiplexPrefix | 7, IccPcsKind.Material)]
    public void Version5SelectsNonColorimetricConnectionSpace(uint spectralSignature, uint mcsSignature, IccPcsKind expected)
    {
        byte[] bytes = CreateHeader(0x05000000, IccProfileClassSignatures.MultiplexLink, IccColorSpaceSignatures.Rgb, 0);
        WriteUInt32(bytes, 100, spectralSignature);
        WriteUInt32(bytes, 116, mcsSignature);
        WriteUInt32(bytes, 120, 0x53554243);

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 0L);

        Assert.That(header.ProfileConnectionSpace, Is.EqualTo(expected));
    }

    [Test]
    public void Version5RetainsColorimetricConnectionSpaceAlongsideSpectralPcs()
    {
        byte[] bytes = CreateHeader(0x05000000, IccProfileClassSignatures.ColorEncodingSpace, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        WriteUInt32(bytes, 100, IccColorSpaceSignatures.TransmissionSpectralPrefix | 10);

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 0L);

        Assert.Multiple(() =>
        {
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Xyz));
            Assert.That(header.SpectralPcs, Is.EqualTo(IccColorSpaceKind.Spectral));
        });
    }

    [Test]
    public void SpectralDeviceSpaceAmendmentAppliesOnlyToVersion5Headers()
    {
        uint signature = IccColorSpaceSignatures.TransmissionSpectralPrefix | 31u;
        byte[] version5Bytes = CreateHeader(0x05000000, IccProfileClassSignatures.Abstract, signature, IccColorSpaceSignatures.Xyz);
        byte[] version4Bytes = CreateHeader(0x04400000, IccProfileClassSignatures.Abstract, signature, IccColorSpaceSignatures.Xyz);

        IccProfileHeader version5 = HeaderCodec.ReadHeader(version5Bytes, 0L);
        IccProfileHeader version4 = HeaderCodec.ReadHeader(version4Bytes, 0L);

        Assert.Multiple(() =>
        {
            Assert.That(version5.DataColorSpaceSignature, Is.EqualTo(signature));
            Assert.That(version5.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(version4.DataColorSpaceSignature, Is.EqualTo(signature));
            Assert.That(version4.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Unknown));
        });
    }

    [Test]
    public void Version5ReportsReservedBytesAndSubclassVersionWithoutSignature()
    {
        byte[] bytes = CreateHeader(0x05000102, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        WriteUInt32(bytes, 124, 0x01020304);

        IccProfileHeader header = HeaderCodec.ReadHeader(bytes, 600L);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.SubclassMajor, Is.EqualTo(1));
            Assert.That(header.Version.SubclassMinor, Is.EqualTo(2));
            Assert.That(header.ProfileSubclassSignature, Is.Zero);
            Assert.That(header.IccMaxReservedValue, Is.EqualTo(0x01020304));
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(2));
            Assert.That(header.ValidationIssues[0].Location.Offset, Is.EqualTo(610));
            Assert.That(header.ValidationIssues[0].Location.Length, Is.EqualTo(2));
            Assert.That(header.ValidationIssues[1].Location.Offset, Is.EqualTo(724));
            Assert.That(header.ValidationIssues[1].Location.Length, Is.EqualTo(4));
        });
    }

    [TestCase(0x02000000u, IccVersionKind.Version2)]
    [TestCase(0x02990000u, IccVersionKind.Version2)]
    [TestCase(0x04000000u, IccVersionKind.Version4)]
    [TestCase(0x04490000u, IccVersionKind.Version4)]
    [TestCase(0x05090000u, IccVersionKind.Version5)]
    public void StrictModeAcceptsSupportedVersionRanges(uint packedVersion, IccVersionKind expectedKind)
    {
        byte[] bytes = CreateHeader(packedVersion, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);

        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(bytes, 50L, IccReadMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(expectedKind));
            Assert.That(header.Version.IsSupported, Is.True);
            Assert.That(header.Version.IsFutureMinor, Is.False);
            Assert.That(header.Version.PackedValue, Is.EqualTo(packedVersion));
            Assert.That(header.ValidationIssues, Is.Empty);
        });
    }

    [TestCase(0x04520000u, IccVersionKind.Version4)]
    [TestCase(0x05130000u, IccVersionKind.Version5)]
    public void RecoveryModePreservesRecognizedFutureMinorVersions(uint packedVersion, IccVersionKind expectedKind)
    {
        byte[] bytes = CreateHeader(packedVersion, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);

        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(bytes, 300L, IccReadMode.Recovery);
        IccUnsupportedFeatureException exception = Assert.Throws<IccUnsupportedFeatureException>(() => HeaderCodec.ReadHeaderWithMode(bytes, 300L, IccReadMode.Strict))!;

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(expectedKind));
            Assert.That(header.Version.IsSupported, Is.False);
            Assert.That(header.Version.IsFutureMinor, Is.True);
            Assert.That(header.Version.PackedValue, Is.EqualTo(packedVersion));
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(header.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderVersion));
            Assert.That(header.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Warning));
            Assert.That(header.ValidationIssues[0].Location.Offset, Is.EqualTo(308));
            Assert.That(header.ValidationIssues[0].Location.Length, Is.EqualTo(4));
            Assert.That(header.ValidationIssues[0].Remediation, Does.Contain($"0x{packedVersion:X8}"));
            Assert.That(exception.Message, Does.Contain("byte offset 308"));
            Assert.That(exception.Message, Does.Contain($"0x{packedVersion:X8}"));
        });
    }

    [Test]
    public void RecoveryPreservesUnsupportedMajorWhileStrictModeRejectsItPrecisely()
    {
        byte[] bytes = CreateHeader(0x10210000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);

        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(bytes, 100L, IccReadMode.Recovery);
        IccUnsupportedFeatureException exception = Assert.Throws<IccUnsupportedFeatureException>(() => HeaderCodec.ReadHeaderWithMode(bytes, 100L, IccReadMode.Strict))!;

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Unknown));
            Assert.That(header.Version.Major, Is.EqualTo(10));
            Assert.That(header.Version.Minor, Is.EqualTo(2));
            Assert.That(header.Version.BugFix, Is.EqualTo(1));
            Assert.That(header.Version.IsSupported, Is.False);
            Assert.That(header.Version.IsFutureMinor, Is.False);
            Assert.That(header.Version.PackedValue, Is.EqualTo(0x10210000));
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(header.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderVersion));
            Assert.That(header.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Error));
            Assert.That(header.ValidationIssues[0].Location.Offset, Is.EqualTo(108));
            Assert.That(exception.Message, Does.Contain("major version 10"));
            Assert.That(exception.Message, Does.Contain("byte offset 108"));
            Assert.That(exception.Message, Does.Contain("0x10210000"));
        });
    }

    [Test]
    public void InvalidBinaryCodedDecimalVersionIsDiagnosedOrRejectedByMode()
    {
        byte[] bytes = CreateHeader(0x04a00000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);

        IccProfileHeader header = HeaderCodec.ReadHeaderWithMode(bytes, 0L, IccReadMode.Recovery);
        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => HeaderCodec.ReadHeaderWithMode(bytes, 0L, IccReadMode.Strict))!;

        Assert.Multiple(() =>
        {
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Version4));
            Assert.That(header.Version.Minor, Is.EqualTo(10));
            Assert.That(header.Version.IsSupported, Is.False);
            Assert.That(header.Version.IsFutureMinor, Is.False);
            Assert.That(header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(header.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderVersion));
            Assert.That(header.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Error));
            Assert.That(exception.Message, Does.Contain("binary-coded decimal"));
            Assert.That(exception.Message, Does.Contain("byte offset 8"));
            Assert.That(exception.Message, Does.Contain("0x04A00000"));
        });
    }

    [Test]
    public void HeaderReadRequiresAll128BytesBeforeDecoding()
    {
        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => HeaderCodec.ReadHeader(new byte[127], 900L))!;

        Assert.That(exception.Message, Does.Contain("exceeds"));
    }

    [TestCase(IccProfileClassSignatures.Input, IccProfileClass.Input)]
    [TestCase(IccProfileClassSignatures.Display, IccProfileClass.Display)]
    [TestCase(IccProfileClassSignatures.Output, IccProfileClass.Output)]
    [TestCase(IccProfileClassSignatures.DeviceLink, IccProfileClass.DeviceLink)]
    [TestCase(IccProfileClassSignatures.ColorSpace, IccProfileClass.ColorSpace)]
    [TestCase(IccProfileClassSignatures.Abstract, IccProfileClass.Abstract)]
    [TestCase(IccProfileClassSignatures.NamedColor, IccProfileClass.NamedColor)]
    [TestCase(IccProfileClassSignatures.ColorEncodingSpace, IccProfileClass.ColorEncodingSpace)]
    [TestCase(IccProfileClassSignatures.MultiplexIdentification, IccProfileClass.MultiplexIdentification)]
    [TestCase(IccProfileClassSignatures.MultiplexLink, IccProfileClass.MultiplexLink)]
    [TestCase(IccProfileClassSignatures.MultiplexVisualization, IccProfileClass.MultiplexVisualization)]
    public void ProfileClassSignaturesAreDecoded(uint signature, IccProfileClass expected)
    {
        Assert.That(HeaderCodec.DecodeProfileClass(signature), Is.EqualTo(expected));
    }

    [TestCase(IccColorSpaceSignatures.Xyz, IccColorSpaceKind.Xyz)]
    [TestCase(IccColorSpaceSignatures.Lab, IccColorSpaceKind.Lab)]
    [TestCase(IccColorSpaceSignatures.Luv, IccColorSpaceKind.Luv)]
    [TestCase(IccColorSpaceSignatures.YCbCr, IccColorSpaceKind.YCbCr)]
    [TestCase(IccColorSpaceSignatures.Yxy, IccColorSpaceKind.Yxy)]
    [TestCase(IccColorSpaceSignatures.Rgb, IccColorSpaceKind.Rgb)]
    [TestCase(IccColorSpaceSignatures.Gray, IccColorSpaceKind.Gray)]
    [TestCase(IccColorSpaceSignatures.Hsv, IccColorSpaceKind.Hsv)]
    [TestCase(IccColorSpaceSignatures.Hls, IccColorSpaceKind.Hls)]
    [TestCase(IccColorSpaceSignatures.Cmyk, IccColorSpaceKind.Cmyk)]
    [TestCase(IccColorSpaceSignatures.Cmy, IccColorSpaceKind.Cmy)]
    [TestCase(IccColorSpaceSignatures.NamedColor, IccColorSpaceKind.NamedColor)]
    [TestCase(IccColorSpaceSignatures.OneColor, IccColorSpaceKind.DeviceChannels)]
    [TestCase(IccColorSpaceSignatures.FifteenColor, IccColorSpaceKind.DeviceChannels)]
    public void Icc1ColorSpaceSignaturesAreDecoded(uint signature, IccColorSpaceKind expected)
    {
        Assert.That(HeaderCodec.DecodeColorSpace(signature), Is.EqualTo(expected));
    }

    [TestCase(IccColorSpaceSignatures.ExtendedDevicePrefix, IccColorSpaceKind.Unknown)]
    [TestCase(IccColorSpaceSignatures.ExtendedDevicePrefix | 1, IccColorSpaceKind.ExtendedDevice)]
    [TestCase(IccColorSpaceSignatures.ExtendedDevicePrefix | 65535, IccColorSpaceKind.ExtendedDevice)]
    [TestCase(0x6e640001u, IccColorSpaceKind.Unknown)]
    public void ExtendedDeviceSignaturesRequireNcPrefixAndNonzeroChannelCount(uint signature, IccColorSpaceKind expected)
    {
        Assert.That(HeaderCodec.DecodeColorSpace(signature), Is.EqualTo(expected));
    }

    [TestCase(IccColorSpaceSignatures.ReflectanceSpectralPrefix | 1, IccColorSpaceKind.Spectral)]
    [TestCase(IccColorSpaceSignatures.TransmissionSpectralPrefix | 2, IccColorSpaceKind.Spectral)]
    [TestCase(IccColorSpaceSignatures.RadiantSpectralPrefix | 3, IccColorSpaceKind.Spectral)]
    [TestCase(IccColorSpaceSignatures.SparseMatrixSpectralPrefix | 4, IccColorSpaceKind.BiSpectral)]
    [TestCase(IccColorSpaceSignatures.BiSpectralPrefix | 5, IccColorSpaceKind.BiSpectral)]
    [TestCase(IccColorSpaceSignatures.BiSpectralPrefix, IccColorSpaceKind.Unknown)]
    public void SpectralSignaturesRequireKnownPrefixAndNonzeroChannelCount(uint signature, IccColorSpaceKind expected)
    {
        Assert.That(HeaderCodec.DecodeSpectralPcs(signature), Is.EqualTo(expected));
    }

    [TestCase(IccColorSpaceSignatures.MultiplexPrefix, IccColorSpaceKind.Unknown)]
    [TestCase(IccColorSpaceSignatures.MultiplexPrefix | 1, IccColorSpaceKind.Multiplex)]
    [TestCase(IccColorSpaceSignatures.MultiplexPrefix | 65535, IccColorSpaceKind.Multiplex)]
    [TestCase(0x6d640001u, IccColorSpaceKind.Unknown)]
    public void MultiplexSignaturesRequireMcPrefixAndNonzeroChannelCount(uint signature, IccColorSpaceKind expected)
    {
        Assert.That(HeaderCodec.DecodeMaterialConnectionSpace(signature), Is.EqualTo(expected));
    }

    private static byte[] CreateHeader(uint version, uint profileClass, uint colorSpace, uint pcs)
    {
        byte[] bytes = new byte[128];
        WriteUInt32(bytes, 0, 512);
        WriteUInt32(bytes, 4, 0x434d4d20);
        WriteUInt32(bytes, 8, version);
        WriteUInt32(bytes, 12, profileClass);
        WriteUInt32(bytes, 16, colorSpace);
        WriteUInt32(bytes, 20, pcs);
        WriteUInt16(bytes, 24, 2026);
        WriteUInt16(bytes, 26, 9);
        WriteUInt16(bytes, 28, 11);
        WriteUInt16(bytes, 30, 14);
        WriteUInt16(bytes, 32, 35);
        WriteUInt16(bytes, 34, 59);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 40, IccConstants.MacintoshPlatform);
        WriteUInt32(bytes, 44, 3);
        WriteUInt32(bytes, 48, 0x4d414e55);
        WriteUInt32(bytes, 52, 0x4d4f444c);
        WriteUInt32(bytes, 56, 0x01020304);
        WriteUInt32(bytes, 60, 0x05060708);
        WriteUInt32(bytes, 64, 3);
        WriteUInt32(bytes, 68, 0x0000f6d6);
        WriteUInt32(bytes, 72, 0x00010000);
        WriteUInt32(bytes, 76, 0x0000d32d);
        WriteUInt32(bytes, 80, 0x43524541);
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

    private static void WriteSpectralRange(byte[] destination, int offset, float start, float end, ushort steps)
    {
        WriteUInt16(destination, offset, BitConverter.HalfToUInt16Bits((Half)start));
        WriteUInt16(destination, offset + 2, BitConverter.HalfToUInt16Bits((Half)end));
        WriteUInt16(destination, offset + 4, steps);
    }
}
