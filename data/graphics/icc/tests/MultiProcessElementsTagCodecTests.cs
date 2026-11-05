using icc;

namespace icc.Tests;

public class MultiProcessElementsTagCodecTests
{
    [Test]
    public void RoundTripsFutureElementsWithFloat32RangesAndAlignmentPadding()
    {
        IccProcessingElement first = CreateRawElement(0x66757431, 2, 3, new byte[] { 0x41 });
        IccProcessingElement second = CreateRawElement(0x66757432, 3, 1, new byte[] { 0x42, 0x43 });
        IccMultiProcessElements source = CreatePipeline(2, 1, first, second);

        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(source);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;
        byte[] roundTrip = MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(64));
            Assert.That(ReadUInt32(payload, 0), Is.EqualTo(IccTypeSignatures.MultiProcessElements));
            Assert.That(ReadUInt16(payload, 8), Is.EqualTo(2));
            Assert.That(ReadUInt16(payload, 10), Is.EqualTo(1));
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(2));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(32));
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(13));
            Assert.That(ReadUInt32(payload, 24), Is.EqualTo(48));
            Assert.That(ReadUInt32(payload, 28), Is.EqualTo(14));
            Assert.That(payload[45], Is.Zero);
            Assert.That(payload[46], Is.Zero);
            Assert.That(payload[47], Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("future element"));
            Assert.That(state.Result.Message, Does.Contain("not executable"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.MultiProcessElements));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(decoded.InputChannels, Is.EqualTo(2));
            Assert.That(decoded.OutputChannels, Is.EqualTo(1));
            Assert.That(decoded.IsExecutable, Is.False);
            Assert.That(decoded.Elements, Has.Length.EqualTo(2));
            Assert.That(decoded.Elements[0].Kind, Is.EqualTo(IccProcessingElementKind.Unknown));
            Assert.That(decoded.Elements[0].RawData.Bytes, Is.EqualTo(first.RawData.Bytes));
            Assert.That(decoded.Elements[1].RawData.Bytes, Is.EqualTo(second.RawData.Bytes));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(decoded.InputRanges, Has.Length.EqualTo(2));
            Assert.That(decoded.InputRanges[0].Minimum, Is.EqualTo(float.MinValue));
            Assert.That(decoded.InputRanges[0].Maximum, Is.EqualTo(float.MaxValue));
            Assert.That(decoded.OutputRanges, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void IdentifiesV4ElementsAndAssignsClutInputRange()
    {
        IccProcessingElement matrix = CreateMatrixElement(2, 3);
        IccProcessingElement clut = CreateClutElement(3, 1);
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 1, matrix, clut));

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.Elements[0].Kind, Is.EqualTo(IccProcessingElementKind.Matrix));
            Assert.That(decoded.Elements[0].InputRanges[0].Minimum, Is.EqualTo(float.MinValue));
            Assert.That(decoded.Elements[0].OutputRanges[0].Maximum, Is.EqualTo(float.MaxValue));
            Assert.That(decoded.Elements[1].Kind, Is.EqualTo(IccProcessingElementKind.Clut));
            Assert.That(decoded.Elements[1].InputRanges.Select(range => range.Minimum), Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
            Assert.That(decoded.Elements[1].InputRanges.Select(range => range.Maximum), Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
        });
    }

    [Test]
    public void CanonicalWriterSharesIdenticalElementsAndClonePreservesSharing()
    {
        IccProcessingElement shared = CreateRawElement(0x73686172, 2, 2, new byte[0]);
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 2, shared, shared));
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(44));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(ReadUInt32(payload, 24)));
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(ReadUInt32(payload, 28)));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0], Is.SameAs(state.Data.MultiProcessElements.Elements[1]));
            Assert.That(clone.MultiProcessElements!.Elements[0], Is.SameAs(clone.MultiProcessElements.Elements[1]));
            Assert.That(clone.MultiProcessElements.Elements[0], Is.Not.SameAs(state.Data.MultiProcessElements.Elements[0]));
        });
    }

    [Test]
    public void DecoderUsesPositionTableOrderWhenStorageOrderDiffers()
    {
        IccProcessingElement first = CreateRawElement(0x66697273, 1, 2, new byte[0]);
        IccProcessingElement second = CreateRawElement(0x7365636f, 2, 1, new byte[0]);
        byte[] canonical = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, first, second));
        byte[] reordered = canonical.Take(32).Concat(canonical.Skip(44).Take(12)).Concat(canonical.Skip(32).Take(12)).ToArray();
        WriteUInt32(reordered, 16, 44);
        WriteUInt32(reordered, 24, 32);

        TagDecodeState state = DecodePayload(reordered, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Signature, Is.EqualTo(0x66697273));
            Assert.That(state.Data.MultiProcessElements.Elements[1].Signature, Is.EqualTo(0x7365636f));
            Assert.That(state.Data.MultiProcessElements.Elements[0].OutputChannels, Is.EqualTo(state.Data.MultiProcessElements.Elements[1].InputChannels));
        });
    }

    [Test]
    public void VersionDispatchSupportsVersion4AndVersion5Containers()
    {
        IccProcessingElement element = CreateRawElement(0x66757472, 1, 1, new byte[0]);
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, element));

        TagDecodeState version2 = DecodePayload(payload, IccVersionKind.Version2, false);
        TagDecodeState version4 = DecodePayload(payload, IccVersionKind.Version4, false);
        TagDecodeState version5 = DecodePayload(payload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(version2.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version2.Result.Message, Does.Contain("ICC.1 version 2"));
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version5.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version5.Result.Message, Does.Contain("future element"));
            Assert.That(version5.Data.Kind, Is.EqualTo(IccTagDataKind.MultiProcessElements));
            Assert.That(version5.Data.MultiProcessElements!.Elements[0].RawData.Bytes, Is.EqualTo(element.RawData.Bytes));
        });
    }

    [Test]
    public void DecoderRejectsMalformedHeaderCountAndPositionBounds()
    {
        byte[] valid = CreateValidPayload();
        byte[] reserved = valid.ToArray();
        reserved[4] = 1;
        byte[] zeroChannels = valid.ToArray();
        WriteUInt16(zeroChannels, 8, 0);
        byte[] zeroCount = valid.ToArray();
        WriteUInt32(zeroCount, 12, 0);
        byte[] hugeCount = valid.ToArray();
        WriteUInt32(hugeCount, 12, uint.MaxValue);
        byte[] tableOffset = valid.ToArray();
        WriteUInt32(tableOffset, 16, 16);
        byte[] unaligned = valid.ToArray();
        WriteUInt32(unaligned, 16, 25);
        byte[] tooSmall = valid.ToArray();
        WriteUInt32(tooSmall, 20, 11);
        byte[] tooLarge = valid.ToArray();
        WriteUInt32(tooLarge, 20, 1000);

        Assert.Multiple(() =>
        {
            AssertInvalid(reserved, "reserved bytes");
            AssertInvalid(zeroChannels, "channel counts");
            AssertInvalid(zeroCount, "at least one processing element");
            AssertInvalid(hugeCount, "supported managed range");
            AssertInvalid(tableOffset, "beyond the position table");
            AssertInvalid(unaligned, "four-byte-aligned");
            AssertInvalid(tooSmall, "at least 12 bytes");
            AssertInvalid(tooLarge, "at least 12 bytes");
        });
    }

    [Test]
    public void DecoderRejectsPaddingOverlapGapsTrailingDataAndConflictingShares()
    {
        IccProcessingElement odd = CreateRawElement(0x6f646421, 1, 1, new byte[] { 1 });
        byte[] padded = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, odd));
        padded[padded.Length - 1] = 1;

        IccProcessingElement first = CreateRawElement(0x6f6e6531, 1, 1, new byte[0]);
        IccProcessingElement second = CreateRawElement(0x74776f32, 1, 1, new byte[0]);
        byte[] two = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, first, second));
        byte[] overlap = two.ToArray();
        WriteUInt32(overlap, 24, 36);
        byte[] gap = new byte[60];
        Array.Copy(two, 0, gap, 0, 44);
        Array.Copy(two, 44, gap, 48, 12);
        WriteUInt32(gap, 24, 48);
        byte[] trailing = two.Append((byte)0).Append((byte)0).Append((byte)0).Append((byte)0).ToArray();
        byte[] conflictingShare = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, odd, odd));
        WriteUInt32(conflictingShare, 28, 12);

        Assert.Multiple(() =>
        {
            AssertInvalid(padded, "alignment padding");
            AssertInvalid(overlap, "overlap illegally");
            AssertInvalid(gap, "gap or hidden unreferenced data");
            AssertInvalid(trailing, "trailing or unreferenced data");
            AssertInvalid(conflictingShare, "same element size");
        });
    }

    [Test]
    public void DecoderRejectsMalformedElementHeadersAndChannelDiscontinuities()
    {
        byte[] valid = CreateValidPayload();
        int elementOffset = (int)ReadUInt32(valid, 16);
        byte[] elementReserved = valid.ToArray();
        elementReserved[elementOffset + 4] = 1;
        byte[] elementChannels = valid.ToArray();
        WriteUInt16(elementChannels, elementOffset + 8, 0);

        IccProcessingElement first = CreateRawElement(0x66697273, 2, 3, new byte[0]);
        IccProcessingElement second = CreateRawElement(0x7365636f, 2, 1, new byte[0]);
        byte[] manuallyDiscontinuous = new byte[56];
        WriteUInt32(manuallyDiscontinuous, 0, IccTypeSignatures.MultiProcessElements);
        WriteUInt16(manuallyDiscontinuous, 8, 2);
        WriteUInt16(manuallyDiscontinuous, 10, 1);
        WriteUInt32(manuallyDiscontinuous, 12, 2);
        WriteUInt32(manuallyDiscontinuous, 16, 32);
        WriteUInt32(manuallyDiscontinuous, 20, 12);
        WriteUInt32(manuallyDiscontinuous, 24, 44);
        WriteUInt32(manuallyDiscontinuous, 28, 12);
        Array.Copy(first.RawData.Bytes, 0, manuallyDiscontinuous, 32, 12);
        Array.Copy(second.RawData.Bytes, 0, manuallyDiscontinuous, 44, 12);

        Assert.Multiple(() =>
        {
            AssertInvalid(elementReserved, "reserved bytes");
            AssertInvalid(elementChannels, "channel counts");
            AssertInvalid(manuallyDiscontinuous, "previous element");
        });
    }

    [Test]
    public void WriterRejectsMissingRawDataMismatchedHeadersAndChannelTransitions()
    {
        IccProcessingElement missingRaw = new IccProcessingElement();
        missingRaw.Signature = 0x6d697373;
        missingRaw.InputChannels = 1;
        missingRaw.OutputChannels = 1;
        IccProcessingElement mismatchedSignature = CreateRawElement(0x73696731, 1, 1, new byte[0]);
        mismatchedSignature.Signature = 0x73696732;
        IccProcessingElement mismatchedChannels = CreateRawElement(0x63686e31, 1, 1, new byte[0]);
        mismatchedChannels.OutputChannels = 2;
        IccProcessingElement badReserved = CreateRawElement(0x72657331, 1, 1, new byte[0]);
        badReserved.RawData.Bytes[4] = 1;
        IccProcessingElement first = CreateRawElement(0x66697273, 1, 2, new byte[0]);
        IccProcessingElement second = CreateRawElement(0x7365636f, 3, 1, new byte[0]);
        IccMultiProcessElements noElements = new IccMultiProcessElements();
        noElements.InputChannels = 1;
        noElements.OutputChannels = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, missingRaw)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, mismatchedSignature)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, mismatchedChannels)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, badReserved)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, first, second)));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(noElements));
        });
    }

    private static byte[] CreateValidPayload()
    {
        IccProcessingElement element = CreateRawElement(0x76616c64, 1, 1, new byte[0]);
        return MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, element));
    }

    private static IccMultiProcessElements CreatePipeline(int inputChannels, int outputChannels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.Elements = elements;
        return pipeline;
    }

    private static IccProcessingElement CreateRawElement(uint signature, int inputChannels, int outputChannels, byte[] body)
    {
        byte[] bytes = new byte[12 + body.Length];
        WriteUInt32(bytes, 0, signature);
        WriteUInt16(bytes, 8, (ushort)inputChannels);
        WriteUInt16(bytes, 10, (ushort)outputChannels);
        Array.Copy(body, 0, bytes, 12, body.Length);
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = signature;
        element.Kind = MultiProcessElementsTagCodec.IdentifyVersion4Kind(signature);
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.RawData.TypeSignature = signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = bytes;
        return element;
    }

    private static IccProcessingElement CreateMatrixElement(int inputChannels, int outputChannels)
    {
        IccProcessingElement element = CreateRawElement(IccProcessingElementSignatures.Matrix, inputChannels, outputChannels, new byte[4 * outputChannels * (inputChannels + 1)]);
        element.Matrix = new IccMatrix();
        element.Matrix.Rows = outputChannels;
        element.Matrix.Columns = inputChannels + 1;
        element.Matrix.Values = new double[outputChannels * (inputChannels + 1)];
        return element;
    }

    private static IccProcessingElement CreateClutElement(int inputChannels, int outputChannels)
    {
        int valueCount = outputChannels;
        byte[] body = new byte[16];
        int index = 0;
        while (index < inputChannels)
        {
            body[index] = 2;
            valueCount *= 2;
            index++;
        }

        IccProcessingElement element = CreateRawElement(IccProcessingElementSignatures.Clut, inputChannels, outputChannels, body.Concat(new byte[valueCount * 4]).ToArray());
        element.Clut = new IccClut();
        element.Clut.InputChannels = inputChannels;
        element.Clut.OutputChannels = outputChannels;
        element.Clut.GridPoints = body.Take(inputChannels).Select(value => (int)value).ToArray();
        element.Clut.Values = new double[valueCount];
        return element;
    }

    private static void AssertInvalid(byte[] payload, string message)
    {
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)(((uint)source[offset] << 8) | source[offset + 1]);
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
