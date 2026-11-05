using icc;

namespace icc.Tests;

public class Icc2MultiProcessElementsTagCodecTests
{
    [Test]
    public void RejectsMalformedCalculatorElementBody()
    {
        IccProcessingElement malformed = CreateRawElement(IccProcessingElementSignatures.Calculator, 1, 1, new byte[] { 1 });

        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(CreatePipeline(1, 1, malformed));
        TagDecodeState state = Decode(payload);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("calculator element requires"));
        });
    }

    [Test]
    public void PreservesFutureElementsSharedPositionsAndChannelContinuity()
    {
        IccProcessingElement shared = CreateRawElement(0x66757472, 2, 2, new byte[] { 0x41 });
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(CreatePipeline(2, 2, shared, shared));
        TagDecodeState state = Decode(payload);

        byte[] discontinuous = payload.ToArray();
        int elementOffset = (int)ReadUInt32(discontinuous, 16);
        WriteUInt16(discontinuous, elementOffset + 10, 3);
        TagDecodeState invalid = Decode(discontinuous);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(ReadUInt32(payload, 24)));
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(ReadUInt32(payload, 28)));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0], Is.SameAs(state.Data.MultiProcessElements.Elements[1]));
            Assert.That(state.Data.MultiProcessElements.Elements[0].Kind, Is.EqualTo(IccProcessingElementKind.Unknown));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.False);
            Assert.That(invalid.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalid.Result.Message, Does.Contain("previous element"));
        });
    }

    [Test]
    public void UsesProfileSpectralMaterialAngularAndPccChannelContext()
    {
        TagDecodeState standard = DecodeWithContext(CreatePayload(4, 3), IccTagSignatures.AToB0,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState spectral = DecodeWithContext(CreatePayload(4, 5), IccTagSignatures.DToB1,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 5, 0);
        TagDecodeState material = DecodeWithContext(CreatePayload(4, 2), IccTagSignatures.AToM0,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, IccColorSpaceSignatures.MultiplexPrefix | 2);
        TagDecodeState angular = DecodeWithContext(CreatePayload(8, 3), IccTagSignatures.BrdfAToB2,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState pcc = DecodeWithContext(CreatePayload(3, 3), IccTagSignatures.CustomToStandardPcc,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState hdr = DecodeWithContext(CreatePayload(3, 3), IccTagSignatures.HdrToSdr2,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState wrongPcc = DecodeWithContext(CreatePayload(3, 4), IccTagSignatures.StandardToCustomPcc,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState wrongHdr = DecodeWithContext(CreatePayload(4, 3), IccTagSignatures.HdrToSdr0,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);
        TagDecodeState missingSpectral = DecodeWithContext(CreatePayload(4, 3), IccTagSignatures.DToB0,
            IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(standard.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(spectral.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(material.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(angular.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(pcc.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(hdr.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(wrongPcc.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongPcc.Result.Message, Does.Contain("requires 3 input and 3 output channels"));
            Assert.That(wrongHdr.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongHdr.Result.Message, Does.Contain("requires 3 input and 3 output channels"));
            Assert.That(missingSpectral.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(missingSpectral.Result.Message, Does.Contain("absent from the profile header"));
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.AToM0), Is.True);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.BrdfAToB2), Is.True);
            Assert.That(MultiProcessElementsTagCodec.IsContextualTransformTag(IccTagSignatures.HdrToSdr3), Is.True);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.CustomToStandardPcc), Is.True);
        });
    }

    [Test]
    public void VersionFiveWriterUsesTheIcc2SignatureRegistry()
    {
        IccProcessingElement calculator = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = 1,
            OutputChannels = 1,
            Calculator = new IccCalculatorProgram
            {
                Instructions = new IccCalculatorInstruction[]
                {
                    CalculatorElementCodec.CreateInstruction(CalculatorOperationSignatures.NoOperation, 0)
                }
            }
        };
        IccMultiProcessElements pipeline = CreatePipeline(1, 1, calculator);
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(pipeline);

        IccProcessingElement mismatch = CreateRawElement(IccProcessingElementSignatures.Calculator, 1, 1, new byte[] { 1 });
        mismatch.Kind = IccProcessingElementKind.ToneMap;

        Assert.Multiple(() =>
        {
            Assert.That(Decode(payload).Data.MultiProcessElements!.Elements[0].Kind, Is.EqualTo(IccProcessingElementKind.Calculator));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(pipeline));
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(CreatePipeline(1, 1, mismatch)));
        });
    }

    [Test]
    public void Icc2AcsElementsRemainExecutablePassThroughStages()
    {
        IccProcessingElement begin = CreateRawElement(IccProcessingElementSignatures.BeginAcs, 2, 2, new byte[] { 0x41, 0x43, 0x53, 0x31 });
        begin.Kind = IccProcessingElementKind.BeginAcs;
        begin.AcsSignature = 0x41435331;
        IccProcessingElement end = CreateRawElement(IccProcessingElementSignatures.EndAcs, 2, 2, new byte[] { 0x41, 0x43, 0x53, 0x31 });
        end.Kind = IccProcessingElementKind.EndAcs;
        end.AcsSignature = 0x41435331;
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(CreatePipeline(2, 2, begin, end));
        TagDecodeState state = Decode(payload);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("pass-through stages"));
            Assert.That(state.Data.MultiProcessElements!.IsExecutable, Is.True);
            Assert.That(state.Data.MultiProcessElements.Elements.Select(element => element.AcsSignature), Is.EqualTo(new uint[] { 0x41435331, 0x41435331 }));
        });
    }

    [Test]
    public void NestedPccTransformsUseTheirMemberSignatureAsDecodeContext()
    {
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x70726976,
            Members = new IccStructureMember[]
            {
                new IccStructureMember
                {
                    Signature = IccTagSignatures.CustomToStandardPcc,
                    Data = CreateMultiProcessData(3, 4)
                }
            }
        };
        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.DataColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        state.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        TagCodecDispatcher.DecodeTagPayload(state);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("requires 3 input and 3 output channels"));
        });
    }

    private static byte[] CreatePayload(int inputChannels, int outputChannels)
    {
        IccProcessingElement element = CreateRawElement(0x66757472, inputChannels, outputChannels, new byte[0]);
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(CreatePipeline(inputChannels, outputChannels, element));
    }

    private static IccTagData CreateMultiProcessData(int inputChannels, int outputChannels)
    {
        IccProcessingElement element = CreateRawElement(0x66757472, inputChannels, outputChannels, new byte[0]);
        return new IccTagData
        {
            Kind = IccTagDataKind.MultiProcessElements,
            MultiProcessElements = CreatePipeline(inputChannels, outputChannels, element)
        };
    }

    private static IccMultiProcessElements CreatePipeline(int inputChannels, int outputChannels, params IccProcessingElement[] elements)
    {
        return new IccMultiProcessElements
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Elements = elements
        };
    }

    private static IccProcessingElement CreateRawElement(uint signature, int inputChannels, int outputChannels, byte[] body)
    {
        byte[] bytes = new byte[12 + body.Length];
        WriteUInt32(bytes, 0, signature);
        WriteUInt16(bytes, 8, (ushort)inputChannels);
        WriteUInt16(bytes, 10, (ushort)outputChannels);
        Array.Copy(body, 0, bytes, 12, body.Length);
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = signature,
            InputChannels = inputChannels,
            OutputChannels = outputChannels
        };
        element.RawData.TypeSignature = signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = bytes;
        return element;
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeWithContext(byte[] payload, uint tagSignature, uint dataSignature, uint pcsSignature, uint spectralSignature, uint materialSignature)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.DataColorSpaceSignature = dataSignature;
        state.ProfileConnectionSpaceSignature = pcsSignature;
        state.SpectralPcsSignature = spectralSignature;
        state.MaterialConnectionSpaceSignature = materialSignature;
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
}
