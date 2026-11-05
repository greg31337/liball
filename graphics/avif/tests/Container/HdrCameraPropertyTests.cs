using System.Buffers.Binary;

namespace avif.Tests;

public class HdrCameraPropertyTests
{
    [Test]
    public void HdrAndViewingPropertiesExposeAllEncodedValues()
    {
        byte[] source = BuildDocument(writer =>
        {
            writer.BeginBox("clli");
            WriteUInt16(writer, 1000);
            WriteUInt16(writer, 400);
            writer.EndBox();

            writer.BeginBox("mdcv");
            WriteUInt16(writer, 34000);
            WriteUInt16(writer, 16000);
            WriteUInt16(writer, 13250);
            WriteUInt16(writer, 34500);
            WriteUInt16(writer, 7500);
            WriteUInt16(writer, 3000);
            WriteUInt16(writer, 15635);
            WriteUInt16(writer, 16450);
            WriteUInt32(writer, 10000000);
            WriteUInt32(writer, 50);
            writer.EndBox();

            writer.BeginBox("cclv");
            writer.Write(new byte[] { 60 });
            WriteInt32(writer, -10);
            WriteInt32(writer, 20);
            WriteInt32(writer, 30);
            WriteInt32(writer, 40);
            WriteInt32(writer, 50);
            WriteInt32(writer, 60);
            WriteUInt32(writer, 1);
            WriteUInt32(writer, 1000);
            WriteUInt32(writer, 250);
            writer.EndBox();

            writer.BeginBox("amve");
            WriteUInt32(writer, 100000);
            WriteUInt16(writer, 15635);
            WriteUInt16(writer, 16450);
            writer.EndBox();

            writer.BeginFullBox("reve", 0, 0);
            WriteUInt32(writer, 50000);
            WriteUInt16(writer, 3127);
            WriteUInt16(writer, 3290);
            WriteUInt32(writer, 10000);
            WriteUInt16(writer, 3000);
            WriteUInt16(writer, 3200);
            writer.EndBox();

            writer.BeginFullBox("ndwt", 0, 0);
            WriteUInt32(writer, 2030000);
            writer.EndBox();
        }, new int[] { 1, 2, 3, 4, 5, 6 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedContentLightLevel? clli = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetContentLightLevel();
        ParsedMasteringDisplayColourVolume? mdcv = (result.Meta
            .GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetMasteringDisplayColourVolume();
        ParsedContentColourVolume? cclv = (result.Meta.GetProperty(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetContentColourVolume();
        ParsedAmbientViewingEnvironment? amve = (result.Meta
            .GetProperty(3) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAmbientViewingEnvironment();
        ParsedReferenceViewingEnvironment? reve = (result.Meta
            .GetProperty(4) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetReferenceViewingEnvironment();
        ParsedNominalDiffuseWhite? ndwt = (result.Meta.GetProperty(5) ?? throw new InvalidOperationException("Invalid internal state.")).GetNominalDiffuseWhite();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((clli ?? throw new InvalidOperationException("Invalid internal state.")).GetMaxContentLightLevel(), Is.EqualTo(1000));
            Assert.That(clli.GetMaxPictureAverageLightLevel(), Is.EqualTo(400));
            Assert.That((mdcv ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryX(1), Is.EqualTo(13250));
            Assert.That(mdcv.GetPrimaryY(2), Is.EqualTo(3000));
            Assert.That(mdcv.GetWhitePointX(), Is.EqualTo(15635));
            Assert.That(mdcv.GetWhitePointY(), Is.EqualTo(16450));
            Assert.That(mdcv.GetMaximumLuminance(), Is.EqualTo(10000000));
            Assert.That(mdcv.GetMinimumLuminance(), Is.EqualTo(50));
            Assert.That((cclv ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaries(), Is.True);
            Assert.That(cclv.GetPrimaryX(0), Is.EqualTo(-10));
            Assert.That(cclv.GetPrimaryY(2), Is.EqualTo(60));
            Assert.That(cclv.GetMinimumLuminance(), Is.EqualTo(1));
            Assert.That(cclv.GetMaximumLuminance(), Is.EqualTo(1000));
            Assert.That(cclv.GetAverageLuminance(), Is.EqualTo(250));
            Assert.That((amve ?? throw new InvalidOperationException("Invalid internal state.")).GetIlluminance(), Is.EqualTo(100000));
            Assert.That(amve.GetLightX(), Is.EqualTo(15635));
            Assert.That((reve ?? throw new InvalidOperationException("Invalid internal state.")).GetSurroundLuminance(), Is.EqualTo(50000));
            Assert.That(reve.GetPeripheryLightY(), Is.EqualTo(3200));
            Assert.That((ndwt ?? throw new InvalidOperationException("Invalid internal state.")).GetLuminance(), Is.EqualTo(2030000));
            Assert.That(ndwt.UsesDefaultLuminance(), Is.False);
        });
    }

    [Test]
    public void ContentColourVolumeSupportsAbsentOptionalFields()
    {
        MetaParseResult result = ParseSingleProperty(writer =>
        {
            writer.BeginBox("cclv");
            writer.Write(new byte[] { 8 });
            WriteUInt32(writer, 999);
            writer.EndBox();
        });
        ParsedContentColourVolume? volume = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetContentColourVolume();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((volume ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaries(), Is.False);
            Assert.That(volume.HasMinimumLuminance(), Is.False);
            Assert.That(volume.HasMaximumLuminance(), Is.True);
            Assert.That(volume.HasAverageLuminance(), Is.False);
            Assert.That(volume.GetMaximumLuminance(), Is.EqualTo(999));
            Assert.That(volume.GetPrimaryX(0), Is.Zero);
        });
    }

    [Test]
    public void HdrPropertiesRejectReservedBitsAndIncorrectPayloadSizes()
    {
        MetaParseResult shortClli = ParseSingleProperty(writer =>
        {
            writer.BeginBox("clli");
            WriteUInt16(writer, 1);
            writer.EndBox();
        });
        MetaParseResult longMdcv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("mdcv");
            writer.Write(new byte[25]);
            writer.EndBox();
        });
        MetaParseResult shortAmve = ParseSingleProperty(writer =>
        {
            writer.BeginBox("amve");
            writer.Write(new byte[7]);
            writer.EndBox();
        });
        MetaParseResult reservedCclv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("cclv");
            writer.Write(new byte[] { 128 });
            writer.EndBox();
        });
        MetaParseResult trailingCclv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("cclv");
            writer.Write(new byte[] { 8, 0, 0, 0, 0, 0 });
            writer.EndBox();
        });
        MetaParseResult emptyCclv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("cclv");
            writer.Write(new byte[] { 0 });
            writer.EndBox();
        });
        MetaParseResult unorderedCclv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("cclv");
            writer.Write(new byte[] { 28 });
            WriteUInt32(writer, 10);
            WriteUInt32(writer, 5);
            WriteUInt32(writer, 7);
            writer.EndBox();
        });
        MetaParseResult zeroAmbient = ParseSingleProperty(writer =>
        {
            writer.BeginBox("amve");
            writer.Write(new byte[8]);
            writer.EndBox();
        });
        MetaParseResult unorderedMdcv = ParseSingleProperty(writer =>
        {
            writer.BeginBox("mdcv");
            writer.Write(new byte[16]);
            WriteUInt32(writer, 1);
            WriteUInt32(writer, 1);
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(shortClli.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(longMdcv.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(shortAmve.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(reservedCclv.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailingCclv.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(emptyCclv.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(unorderedCclv.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroAmbient.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(unorderedMdcv.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void FullBoxViewingPropertiesPreserveVersionAndEnforceFlagsRangeAndSize()
    {
        MetaParseResult version = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("reve", 1, 0);
            writer.Write(new byte[16]);
            writer.EndBox();
        });
        MetaParseResult flags = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("ndwt", 0, 1);
            WriteUInt32(writer, 0);
            writer.EndBox();
        });
        MetaParseResult chromaticity = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("reve", 0, 0);
            WriteUInt32(writer, 0);
            WriteUInt16(writer, 10001);
            WriteUInt16(writer, 0);
            WriteUInt32(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            writer.EndBox();
        });
        MetaParseResult shortWhite = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("ndwt", 0, 0);
            WriteUInt16(writer, 0);
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                ((version.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceViewingEnvironment(),
                Is.Null);
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(chromaticity.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(shortWhite.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void CameraIntrinsicMatrixParsesSimplifiedAndFullForms()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteCameraIntrinsic(writer, 512, 400, 200, 100, 0, 0);
            WriteCameraIntrinsic(writer, 197121, 400, 200, 100, 800, 8);
        }, new int[] { 1, 2 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedCameraIntrinsicMatrix? simplified = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetCameraIntrinsicMatrix();
        ParsedCameraIntrinsicMatrix? full = (result.Meta
            .GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetCameraIntrinsicMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((simplified ?? throw new InvalidOperationException("Invalid internal state.")).HasFullIntrinsics(), Is.False);
            Assert.That(simplified.GetDenominatorShift(), Is.EqualTo(2));
            Assert.That(simplified.GetDenominator(), Is.EqualTo(4));
            Assert.That(simplified.GetFocalLengthY(), Is.Zero);
            Assert.That(simplified.GetSkewFactor(), Is.Zero);
            Assert.That((full ?? throw new InvalidOperationException("Invalid internal state.")).HasFullIntrinsics(), Is.True);
            Assert.That(full.GetDenominatorShift(), Is.EqualTo(2));
            Assert.That(full.GetSkewDenominatorShift(), Is.EqualTo(3));
            Assert.That(full.GetSkewDenominator(), Is.EqualTo(8));
            Assert.That(full.GetPrincipalPointX(), Is.EqualTo(200));
            Assert.That(full.GetPrincipalPointY(), Is.EqualTo(100));
            Assert.That(full.GetFocalLengthY(), Is.EqualTo(800));
            Assert.That(full.GetSkewFactor(), Is.EqualTo(8));
        });
    }

    [Test]
    public void CameraIntrinsicMatrixPreservesVersionAndRejectsFlagsAndWrongFormSize()
    {
        MetaParseResult version = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmin", 1, 0);
            writer.Write(new byte[12]);
            writer.EndBox();
        });
        MetaParseResult reserved = ParseSingleProperty(writer =>
            WriteCameraIntrinsic(writer, 2, 1, 2, 3, 0, 0));
        MetaParseResult wrongSize = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmin", 0, 1);
            writer.Write(new byte[12]);
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((version.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetCameraIntrinsicMatrix(), Is.Null);
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(wrongSize.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void CameraExtrinsicMatrixParsesPositionsOrientationsAndIdentifier()
    {
        byte[] source = BuildDocument(writer =>
        {
            writer.BeginFullBox("cmex", 0, 47);
            WriteInt32(writer, 10);
            WriteInt32(writer, -20);
            WriteInt32(writer, 30);
            WriteInt16(writer, 8192);
            WriteInt16(writer, 0);
            WriteInt16(writer, 0);
            WriteUInt32(writer, 42);
            writer.EndBox();

            writer.BeginFullBox("cmex", 0, 24);
            WriteInt32(writer, 536870912);
            WriteInt32(writer, 0);
            WriteInt32(writer, 0);
            writer.EndBox();
        }, new int[] { 1, 2 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedCameraExtrinsicMatrix? small = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetCameraExtrinsicMatrix();
        ParsedCameraExtrinsicMatrix? large = (result.Meta
            .GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetCameraExtrinsicMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((small ?? throw new InvalidOperationException("Invalid internal state.")).HasPositionX(), Is.True);
            Assert.That(small.GetPositionX(), Is.EqualTo(10));
            Assert.That(small.GetPositionY(), Is.EqualTo(-20));
            Assert.That(small.GetPositionZ(), Is.EqualTo(30));
            Assert.That(small.HasOrientation(), Is.True);
            Assert.That(small.UsesLargeOrientationFields(), Is.False);
            Assert.That(small.GetQuaternionX(), Is.EqualTo(0.5));
            Assert.That(small.GetQuaternionW(), Is.EqualTo(System.Math.Sqrt(0.75)).Within(0.000001));
            Assert.That(small.HasWorldCoordinateSystemId(), Is.True);
            Assert.That(small.GetWorldCoordinateSystemId(), Is.EqualTo(42));
            Assert.That((large ?? throw new InvalidOperationException("Invalid internal state.")).UsesLargeOrientationFields(), Is.True);
            Assert.That(large.GetQuaternionXRaw(), Is.EqualTo(536870912));
            Assert.That(large.GetQuaternionX(), Is.EqualTo(0.5));
        });
    }

    [Test]
    public void CameraExtrinsicMatrixRejectsInvalidFlagsSizeAndQuaternion()
    {
        MetaParseResult reserved = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmex", 0, 64);
            writer.EndBox();
        });
        MetaParseResult largeWithoutOrientation = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmex", 0, 16);
            writer.EndBox();
        });
        MetaParseResult wrongSize = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmex", 0, 1);
            writer.EndBox();
        });
        MetaParseResult quaternion = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("cmex", 0, 8);
            WriteInt16(writer, 16384);
            WriteInt16(writer, 1);
            WriteInt16(writer, 0);
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(largeWithoutOrientation.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(wrongSize.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(quaternion.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    private static void WriteCameraIntrinsic(
        IsoBoxWriter writer,
        int flags,
        int focalLengthX,
        int principalPointX,
        int principalPointY,
        int focalLengthY,
        int skewFactor)
    {
        writer.BeginFullBox("cmin", 0, flags);
        WriteInt32(writer, focalLengthX);
        WriteInt32(writer, principalPointX);
        WriteInt32(writer, principalPointY);
        if ((flags & 1) != 0)
        {
            WriteInt32(writer, focalLengthY);
            WriteInt32(writer, skewFactor);
        }

        writer.EndBox();
    }

    private static MetaParseResult ParseSingleProperty(Action<IsoBoxWriter> writeProperty)
    {
        byte[] source = BuildDocument(writeProperty, new int[] { 1 });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static byte[] BuildDocument(
        Action<IsoBoxWriter> writeProperties,
        int[] propertyIndices)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 131072);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer);
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writeProperties(writer);
        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 1);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { (byte)propertyIndices.Length });
        int index = 0;
        while (index < propertyIndices.Length)
        {
            writer.Write(new byte[] { (byte)propertyIndices[index] });
            index++;
        }

        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 1);
        writer.BeginFullBox("infe", 3, 0);
        byte[] entry = new byte[11];
        BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), 1);
        WriteFourCc(entry.AsSpan(6, 4), "av01");
        writer.Write(entry);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static void WriteUInt16(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);
        writer.Write(bytes);
    }

    private static void WriteInt16(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, (short)value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteInt32(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 262144;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxMetadataBytes = 65536;
        limits.MaxIccBytes = 32768;
        limits.MaxTemporaryBytes = 131072;
        return limits;
    }
}
