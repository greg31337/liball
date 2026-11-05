using icc;

namespace icc.Tests;

public class AcsProcessingElementTagCodecTests
{
    [Test]
    public void RoundTripsAcsBookendsWithVendorDataAsExecutablePassThroughStages()
    {
        IccProcessingElement begin = CreateAcs(IccProcessingElementKind.BeginAcs, 2, 0x76656e64, new byte[] { 0xde, 0xad, 0xbe });
        IccProcessingElement end = CreateAcs(IccProcessingElementKind.EndAcs, 2, 0x76656e64, new byte[] { 0xca, 0xfe });
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, begin, end));

        TagDecodeState state = DecodePayload(payload);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        byte[] roundTrip = MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(72));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(32));
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(19));
            Assert.That(ReadUInt32(payload, 24), Is.EqualTo(52));
            Assert.That(ReadUInt32(payload, 28), Is.EqualTo(18));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("pass-through"));
            Assert.That(decoded.IsExecutable, Is.True);
            Assert.That(decoded.Elements.Select(element => element.Kind), Is.EqualTo(new[]
            {
                IccProcessingElementKind.BeginAcs,
                IccProcessingElementKind.EndAcs
            }));
            Assert.That(decoded.Elements[0].AcsSignature, Is.EqualTo(0x76656e64));
            Assert.That(decoded.Elements[0].AcsData, Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe }));
            Assert.That(decoded.Elements[1].AcsSignature, Is.EqualTo(0x76656e64));
            Assert.That(decoded.Elements[1].AcsData, Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(decoded.Elements[0].InputRanges.Select(range => range.Minimum), Is.EqualTo(new[] { (double)float.MinValue, (double)float.MinValue }));
            Assert.That(decoded.Elements[0].OutputRanges.Select(range => range.Maximum), Is.EqualTo(new[] { (double)float.MaxValue, (double)float.MaxValue }));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(clone.MultiProcessElements!.IsExecutable, Is.True);
            Assert.That(clone.MultiProcessElements.Elements[0].AcsSignature, Is.EqualTo(begin.AcsSignature));
            Assert.That(clone.MultiProcessElements.Elements[0].AcsData, Is.EqualTo(begin.AcsData));
            Assert.That(clone.MultiProcessElements.Elements[0].AcsData, Is.Not.SameAs(decoded.Elements[0].AcsData));
        });
    }

    [Test]
    public void AcceptsZeroAcsSignatureAndRejectsTruncatedOrChannelChangingBookends()
    {
        byte[] valid = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, CreateAcs(IccProcessingElementKind.BeginAcs, 2, 0, new byte[0])));
        TagDecodeState zeroSignature = DecodePayload(valid);
        int elementOffset = (int)ReadUInt32(valid, 16);
        byte[] truncated = valid.Take(valid.Length - 4).ToArray();
        WriteUInt32(truncated, 20, 12);
        byte[] channelChanging = valid.ToArray();
        WriteUInt16(channelChanging, 10, 1);
        WriteUInt16(channelChanging, elementOffset + 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(zeroSignature.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(zeroSignature.Data.MultiProcessElements!.Elements[0].AcsSignature, Is.Zero);
            AssertInvalid(truncated, "16-byte header");
            AssertInvalid(channelChanging, "equal input and output channel counts");
        });
    }

    [Test]
    public void RetainsUnknownVendorStageAndMarksTheAcsPipelineNonExecutable()
    {
        IccProcessingElement begin = CreateAcs(IccProcessingElementKind.BeginAcs, 2, 0x76656e64, new byte[] { 1 });
        IccProcessingElement vendorStage = CreateUnknown(0x766e6431, 2, new byte[] { 9, 8, 7 });
        IccProcessingElement end = CreateAcs(IccProcessingElementKind.EndAcs, 2, 0x76656e64, new byte[] { 2 });
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, begin, vendorStage, end));

        TagDecodeState state = DecodePayload(payload);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("not executable"));
            Assert.That(decoded.IsExecutable, Is.False);
            Assert.That(decoded.Elements.Select(element => element.Kind), Is.EqualTo(new[]
            {
                IccProcessingElementKind.BeginAcs,
                IccProcessingElementKind.Unknown,
                IccProcessingElementKind.EndAcs
            }));
            Assert.That(decoded.Elements[0].AcsData, Is.EqualTo(new byte[] { 1 }));
            Assert.That(decoded.Elements[1].RawData.Bytes, Is.EqualTo(vendorStage.RawData.Bytes));
            Assert.That(decoded.Elements[2].AcsData, Is.EqualTo(new byte[] { 2 }));
            Assert.That(MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded), Is.EqualTo(payload));
        });
    }

    [Test]
    public void WriterRejectsInvalidAcsModels()
    {
        IccProcessingElement wrongSignature = CreateAcs(IccProcessingElementKind.BeginAcs, 1, 0, new byte[0]);
        wrongSignature.Signature = IccProcessingElementSignatures.EndAcs;
        IccProcessingElement channelChanging = CreateAcs(IccProcessingElementKind.EndAcs, 1, 0, new byte[0]);
        channelChanging.OutputChannels = 2;
        IccProcessingElement nullData = CreateAcs(IccProcessingElementKind.BeginAcs, 1, 0, new byte[0]);
        nullData.AcsData = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, wrongSignature)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, channelChanging)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, nullData)));
        });
    }

    private static IccProcessingElement CreateAcs(IccProcessingElementKind kind, int channels, uint acsSignature, byte[] data)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = kind == IccProcessingElementKind.BeginAcs
            ? IccProcessingElementSignatures.BeginAcs
            : IccProcessingElementSignatures.EndAcs;
        element.Kind = kind;
        element.InputChannels = channels;
        element.OutputChannels = channels;
        element.AcsSignature = acsSignature;
        element.AcsData = data;
        return element;
    }

    private static IccProcessingElement CreateUnknown(uint signature, int channels, byte[] data)
    {
        byte[] bytes = new byte[12 + data.Length];
        WriteUInt32(bytes, 0, signature);
        WriteUInt16(bytes, 8, (ushort)channels);
        WriteUInt16(bytes, 10, (ushort)channels);
        Array.Copy(data, 0, bytes, 12, data.Length);
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = signature;
        element.Kind = IccProcessingElementKind.Unknown;
        element.InputChannels = channels;
        element.OutputChannels = channels;
        element.RawData.TypeSignature = signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = bytes;
        return element;
    }

    private static IccMultiProcessElements CreatePipeline(int channels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = channels;
        pipeline.OutputChannels = channels;
        pipeline.Elements = elements;
        return pipeline;
    }

    private static void AssertInvalid(byte[] payload, string message)
    {
        TagDecodeState state = DecodePayload(payload);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
    }

    private static TagDecodeState DecodePayload(byte[] payload)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
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
