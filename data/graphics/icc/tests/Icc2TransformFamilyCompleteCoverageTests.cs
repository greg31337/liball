using icc;

namespace icc.Tests;

public class Icc2TransformFamilyCompleteCoverageTests
{
    [TestCase(IccProcessingElementSignatures.CurveSet, IccProcessingElementKind.CurveSet)]
    [TestCase(IccProcessingElementSignatures.Matrix, IccProcessingElementKind.Matrix)]
    [TestCase(IccProcessingElementSignatures.Clut, IccProcessingElementKind.Clut)]
    [TestCase(IccProcessingElementSignatures.BeginAcs, IccProcessingElementKind.BeginAcs)]
    [TestCase(IccProcessingElementSignatures.EndAcs, IccProcessingElementKind.EndAcs)]
    [TestCase(IccProcessingElementSignatures.Calculator, IccProcessingElementKind.Calculator)]
    [TestCase(IccProcessingElementSignatures.ExtendedClut, IccProcessingElementKind.ExtendedClut)]
    [TestCase(IccProcessingElementSignatures.SparseMatrix, IccProcessingElementKind.SparseMatrix)]
    [TestCase(IccProcessingElementSignatures.TintArray, IccProcessingElementKind.TintArray)]
    [TestCase(IccProcessingElementSignatures.ToneMap, IccProcessingElementKind.ToneMap)]
    [TestCase(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab)]
    [TestCase(IccProcessingElementSignatures.JabToXyz, IccProcessingElementKind.JabToXyz)]
    [TestCase(IccProcessingElementSignatures.EmissionClut, IccProcessingElementKind.EmissionClut)]
    [TestCase(IccProcessingElementSignatures.EmissionMatrix, IccProcessingElementKind.EmissionMatrix)]
    [TestCase(IccProcessingElementSignatures.EmissionObserver, IccProcessingElementKind.EmissionObserver)]
    [TestCase(IccProcessingElementSignatures.InverseEmissionMatrix, IccProcessingElementKind.InverseEmissionMatrix)]
    [TestCase(IccProcessingElementSignatures.ReflectanceClut, IccProcessingElementKind.ReflectanceClut)]
    [TestCase(IccProcessingElementSignatures.ReflectanceObserver, IccProcessingElementKind.ReflectanceObserver)]
    public void EveryIcc2ProcessingElementSignatureSelectsItsBodyCodec(
        uint signature, IccProcessingElementKind expectedKind)
    {
        IccProcessingElementKind kind = MultiProcessElementsTagCodec
            .IdentifyVersion5Kind(signature);

        Assert.Multiple(() =>
        {
            Assert.That(kind, Is.EqualTo(expectedKind));
            Assert.That(Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(kind), Is.True);
        });
    }

    [TestCase(IccTagSignatures.AToB0, 4, 5, 3, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.BToA0, 4, 3, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.DToB0, 4, 5, 7, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.BToD0, 4, 7, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.AToM0, 1, 5, 2, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.MToA0, 1, 2, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.MToB0, 4, 2, 3, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.MToS0, 4, 2, 7, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.HdrToSdr0, 4, 3, 3, IccTransformDirection.DeviceToDevice)]
    [TestCase(IccTagSignatures.CustomToStandardPcc, 1, 3, 3, IccTransformDirection.DeviceToDevice)]
    [TestCase(IccTagSignatures.StandardToCustomPcc, 1, 3, 3, IccTransformDirection.DeviceToDevice)]
    [TestCase(IccTagSignatures.BrdfAToB0, 4, 9, 3, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.BrdfBToA0, 4, 7, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.BrdfDToB0, 4, 9, 7, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.BrdfBToD0, 4, 11, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.BrdfMToB0, 4, 6, 3, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.BrdfMToS0, 4, 6, 7, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.DirectionalAToB0, 4, 9, 3, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.DirectionalBToA0, 4, 7, 5, IccTransformDirection.PcsToDevice)]
    [TestCase(IccTagSignatures.DirectionalDToB0, 4, 9, 7, IccTransformDirection.DeviceToPcs)]
    [TestCase(IccTagSignatures.DirectionalBToD0, 4, 11, 5, IccTransformDirection.PcsToDevice)]
    public void EveryIcc2TransformFamilyAndIntentUsesItsDeclaredDirectionAndChannels(
        uint firstSignature, int signatureCount, int inputChannels,
        int outputChannels, IccTransformDirection direction)
    {
        int intent = 0;
        while (intent < signatureCount)
        {
            uint signature = firstSignature + (uint)intent;
            TagDecodeState state = ContextState(signature);
            string valid = MultiProcessElementsTagCodec.ValidateProfileContext(
                state, inputChannels, outputChannels);
            string invalid = MultiProcessElementsTagCodec.ValidateProfileContext(
                state, inputChannels + 1, outputChannels);
            IccTransformDirection wrongDirection = direction == IccTransformDirection.DeviceToPcs
                ? IccTransformDirection.PcsToDevice : IccTransformDirection.DeviceToPcs;

            Assert.Multiple(() =>
            {
                Assert.That(MultiProcessElementsTagCodec.IsContextualTransformTag(signature),
                    Is.True, $"signature 0x{signature:X8}");
                Assert.That(valid, Is.Empty, $"signature 0x{signature:X8}");
                Assert.That(invalid, Does.Contain($"requires {inputChannels} input"),
                    $"signature 0x{signature:X8}");
                Assert.DoesNotThrow(() => MpePipelineCompiler.ValidateVersionFiveRequest(
                    signature, direction), $"signature 0x{signature:X8}");
                Assert.Throws<IccTransformException>(() =>
                    MpePipelineCompiler.ValidateVersionFiveRequest(signature, wrongDirection),
                    $"signature 0x{signature:X8}");
            });
            intent = intent + 1;
        }
    }

    [TestCase(1)]
    [TestCase(16)]
    [TestCase(255)]
    [TestCase(65535)]
    public void ExtendedDeviceSpacesSupplyTheirFullEncodedChannelCount(int channels)
    {
        uint signature = IccColorSpaceSignatures.ExtendedDevicePrefix | (uint)channels;
        TagDecodeState state = ContextState(IccTagSignatures.AToB0);
        state.DataColorSpaceSignature = signature;

        Assert.Multiple(() =>
        {
            Assert.That(ColorSpaceChannelCount.Decode(signature), Is.EqualTo(channels));
            Assert.That(MultiProcessElementsTagCodec.ValidateProfileContext(
                state, channels, 3), Is.Empty);
            Assert.That(MultiProcessElementsTagCodec.ValidateProfileContext(
                state, channels == 1 ? 2 : channels - 1, 3), Does.Contain("requires"));
        });
    }

    [Test]
    public void GamutBrdfHdrAndSurfaceFamiliesExposeTheirRuntimePayloadContracts()
    {
        Icc2TagDefinition[] gamut = Definitions(
            IccTagSignatures.GamutBoundary0, IccTagSignatures.GamutBoundary3);
        Icc2TagDefinition[] hdr = Definitions(
            IccTagSignatures.HdrToSdr0, IccTagSignatures.HdrToSdr3);
        Icc2TagDefinition surface = Find(IccTagSignatures.SurfaceMap);
        Icc2TagDefinition brdfParameter = Find(
            IccTagSignatures.BrdfColorimetricParameter2);
        Icc2TagDefinition brdfTransform = Find(IccTagSignatures.BrdfAToB2);

        Assert.Multiple(() =>
        {
            Assert.That(gamut.Select(item => item.RenderingIntent),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(gamut.All(item => item.Role == Icc2TagRole.GamutBoundary
                && item.PermittedTypes.SequenceEqual(new uint[]
                { IccTypeSignatures.GamutBoundaryDescription })), Is.True);
            Assert.That(hdr.Select(item => item.RenderingIntent),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(hdr.All(item => item.Role == Icc2TagRole.HdrToSdrTransform
                && item.PermittedTypes.SequenceEqual(new uint[]
                { IccTypeSignatures.MultiProcessElements })), Is.True);
            Assert.That(surface.Role, Is.EqualTo(Icc2TagRole.Surface));
            Assert.That(surface.PermittedTypes, Is.EqualTo(new uint[]
            {
                IccTypeSignatures.EmbeddedNormalImage,
                IccTypeSignatures.EmbeddedHeightImage
            }));
            Assert.That(brdfParameter.Role, Is.EqualTo(Icc2TagRole.BrdfParameterTransform));
            Assert.That(brdfParameter.ContainerSignature,
                Is.EqualTo(IccStructureSignatures.BrdfTransform));
            Assert.That(brdfTransform.Role, Is.EqualTo(Icc2TagRole.BrdfTransform));
        });
    }

    private static TagDecodeState ContextState(uint tagSignature)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(new byte[24]),
            0, 24, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.DataColorSpaceSignature = IccColorSpaceSignatures.ExtendedDevicePrefix | 5u;
        state.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        state.SpectralPcsSignature = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 7u;
        state.MaterialConnectionSpaceSignature = IccColorSpaceSignatures.MultiplexPrefix | 2u;
        return state;
    }

    private static Icc2TagDefinition[] Definitions(uint first, uint last)
    {
        return Icc2TagRegistry.Definitions.Where(item => item.TagSignature >= first
            && item.TagSignature <= last).OrderBy(item => item.TagSignature).ToArray();
    }

    private static Icc2TagDefinition Find(uint signature)
    {
        Icc2TagDefinition? definition = Icc2TagRegistry.FindDefinition(signature);
        Assert.That(definition, Is.Not.Null);
        return definition!;
    }
}
