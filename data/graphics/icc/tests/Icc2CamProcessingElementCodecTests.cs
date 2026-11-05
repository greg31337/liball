using icc;

namespace icc.Tests;

public class Icc2CamProcessingElementCodecTests
{
    [TestCase(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab)]
    [TestCase(IccProcessingElementSignatures.JabToXyz, IccProcessingElementKind.JabToXyz)]
    public void AppearanceElementRoundTripsEveryCamViewingCondition(uint signature, IccProcessingElementKind kind)
    {
        IccProcessingElement source = CreateAppearanceElement(signature, kind);
        byte[] payload = Serialize(source);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];
        IccCamViewingConditions conditions = decoded.CamViewingConditions!;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(kind));
            Assert.That(decoded.InputChannels, Is.EqualTo(3));
            Assert.That(decoded.OutputChannels, Is.EqualTo(3));
            Assert.That(decoded.RawData.Bytes, Has.Length.EqualTo(44));
            Assert.That(conditions.WhitePoint.X, Is.EqualTo(0.9642).Within(0.000001));
            Assert.That(conditions.WhitePoint.Y, Is.EqualTo(1.0));
            Assert.That(conditions.WhitePoint.Z, Is.EqualTo(0.8249).Within(0.000001));
            Assert.That(conditions.Luminance, Is.EqualTo(64.0));
            Assert.That(conditions.BackgroundLuminance, Is.EqualTo(12.8).Within(0.000001));
            Assert.That(conditions.ImpactOfSurround, Is.EqualTo(0.69).Within(0.000001));
            Assert.That(conditions.ChromaticInductionFactor, Is.EqualTo(1.0));
            Assert.That(conditions.AdaptationFactor, Is.EqualTo(1.0));
            Assert.That(clone.MultiProcessElements!.Elements[0].CamViewingConditions, Is.Not.SameAs(conditions));
            Assert.That(clone.MultiProcessElements.Elements[0].CamViewingConditions!.WhitePoint, Is.Not.SameAs(conditions.WhitePoint));
            Assert.That(Serialize(decoded), Is.EqualTo(payload));
        });
    }

    [Test]
    public void DecoderRejectsWrongChannelsTrailingDataNonFiniteValuesAndInvalidSurround()
    {
        byte[] valid = Serialize(CreateAppearanceElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab));
        int elementOffset = (int)ReadUInt32(valid, 16);

        byte[] wrongChannels = valid.ToArray();
        WriteUInt16(wrongChannels, elementOffset + 8, 2);

        byte[] trailing = new byte[valid.Length + 4];
        Array.Copy(valid, trailing, valid.Length);
        WriteUInt32(trailing, 20, 48);

        byte[] nonFinite = valid.ToArray();
        WriteUInt32(nonFinite, elementOffset + 24, 0x7fc00000);

        byte[] invalidSurround = valid.ToArray();
        WriteFloat32(invalidSurround, elementOffset + 32, 1.25f);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(wrongChannels).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(wrongChannels).Result.Message, Does.Contain("exactly three input and three output channels"));
            Assert.That(Decode(trailing).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(trailing).Result.Message, Does.Contain("exactly 44 bytes"));
            Assert.That(Decode(nonFinite).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(nonFinite).Result.Message, Does.Contain("finite normal float32Number"));
            Assert.That(Decode(invalidSurround).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(invalidSurround).Result.Message, Does.Contain("range from zero to one"));
        });
    }

    [Test]
    public void WriterRejectsMismatchedAppearanceKindsMissingDataAndInvalidValues()
    {
        IccProcessingElement mismatch = CreateAppearanceElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.JabToXyz);
        IccProcessingElement missing = CreateAppearanceElement(IccProcessingElementSignatures.JabToXyz, IccProcessingElementKind.JabToXyz);
        missing.CamViewingConditions = null;
        IccProcessingElement invalidSurround = CreateAppearanceElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab);
        invalidSurround.CamViewingConditions!.ImpactOfSurround = -0.01;
        IccProcessingElement nonFinite = CreateAppearanceElement(IccProcessingElementSignatures.JabToXyz, IccProcessingElementKind.JabToXyz);
        nonFinite.CamViewingConditions!.AdaptationFactor = double.PositiveInfinity;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => Serialize(mismatch));
            Assert.Throws<IccProfileWriteException>(() => Serialize(missing));
            Assert.Throws<IccProfileWriteException>(() => Serialize(invalidSurround));
            Assert.Throws<IccProfileWriteException>(() => Serialize(nonFinite));
        });
    }

    private static IccProcessingElement CreateAppearanceElement(uint signature, IccProcessingElementKind kind)
    {
        return new IccProcessingElement
        {
            Signature = signature,
            Kind = kind,
            InputChannels = 3,
            OutputChannels = 3,
            CamViewingConditions = new IccCamViewingConditions
            {
                WhitePoint = new IccXyzColor { X = 0.9642, Y = 1.0, Z = 0.8249 },
                Luminance = 64.0,
                BackgroundLuminance = 12.8,
                ImpactOfSurround = 0.69,
                ChromaticInductionFactor = 1.0,
                AdaptationFactor = 1.0
            }
        };
    }

    private static byte[] Serialize(IccProcessingElement element)
    {
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            Elements = new IccProcessingElement[] { element }
        });
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void WriteFloat32(byte[] bytes, int offset, float value)
    {
        WriteUInt32(bytes, offset, (uint)BitConverter.SingleToInt32Bits(value));
    }
}
