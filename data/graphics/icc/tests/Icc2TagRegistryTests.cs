using icc;

namespace icc.Tests;

public class Icc2TagRegistryTests
{
    [Test]
    public void CoversEveryIcc2PublicTagExactlyOnce()
    {
        uint[] expected = new uint[]
        {
            IccTagSignatures.AToB0, IccTagSignatures.AToB1, IccTagSignatures.AToB2, IccTagSignatures.AToB3,
            IccTagSignatures.AToM0,
            IccTagSignatures.BrdfColorimetricParameter0, IccTagSignatures.BrdfColorimetricParameter1,
            IccTagSignatures.BrdfColorimetricParameter2, IccTagSignatures.BrdfColorimetricParameter3,
            IccTagSignatures.BrdfSpectralParameter0, IccTagSignatures.BrdfSpectralParameter1,
            IccTagSignatures.BrdfSpectralParameter2, IccTagSignatures.BrdfSpectralParameter3,
            IccTagSignatures.BrdfAToB0, IccTagSignatures.BrdfAToB1, IccTagSignatures.BrdfAToB2, IccTagSignatures.BrdfAToB3,
            IccTagSignatures.BrdfBToA0, IccTagSignatures.BrdfBToA1, IccTagSignatures.BrdfBToA2, IccTagSignatures.BrdfBToA3,
            IccTagSignatures.BrdfBToD0, IccTagSignatures.BrdfBToD1, IccTagSignatures.BrdfBToD2, IccTagSignatures.BrdfBToD3,
            IccTagSignatures.BrdfDToB0, IccTagSignatures.BrdfDToB1, IccTagSignatures.BrdfDToB2, IccTagSignatures.BrdfDToB3,
            IccTagSignatures.BrdfMToB0, IccTagSignatures.BrdfMToB1, IccTagSignatures.BrdfMToB2, IccTagSignatures.BrdfMToB3,
            IccTagSignatures.BrdfMToS0, IccTagSignatures.BrdfMToS1, IccTagSignatures.BrdfMToS2, IccTagSignatures.BrdfMToS3,
            IccTagSignatures.BToA0, IccTagSignatures.BToA1, IccTagSignatures.BToA2, IccTagSignatures.BToA3,
            IccTagSignatures.BToD0, IccTagSignatures.BToD1, IccTagSignatures.BToD2, IccTagSignatures.BToD3,
            IccTagSignatures.CalibrationDateTime, IccTagSignatures.CharacterizationTarget, IccTagSignatures.Cicp,
            IccTagSignatures.ColorEncodingParameters, IccTagSignatures.ColorSpaceName,
            IccTagSignatures.ColorantOrder, IccTagSignatures.ColorantOrderOut,
            IccTagSignatures.ColorantInfo, IccTagSignatures.ColorantInfoOut,
            IccTagSignatures.ColorimetricIntentImageState, IccTagSignatures.Copyright,
            IccTagSignatures.CustomToStandardPcc, IccTagSignatures.Cxf,
            IccTagSignatures.DeviceManufacturerDescription, IccTagSignatures.DeviceModelDescription,
            IccTagSignatures.DevicePcc, IccTagSignatures.DeviceSpectralRange,
            IccTagSignatures.DirectionalAToB0, IccTagSignatures.DirectionalAToB1,
            IccTagSignatures.DirectionalAToB2, IccTagSignatures.DirectionalAToB3,
            IccTagSignatures.DirectionalBToA0, IccTagSignatures.DirectionalBToA1,
            IccTagSignatures.DirectionalBToA2, IccTagSignatures.DirectionalBToA3,
            IccTagSignatures.DirectionalBToD0, IccTagSignatures.DirectionalBToD1,
            IccTagSignatures.DirectionalBToD2, IccTagSignatures.DirectionalBToD3,
            IccTagSignatures.DirectionalDToB0, IccTagSignatures.DirectionalDToB1,
            IccTagSignatures.DirectionalDToB2, IccTagSignatures.DirectionalDToB3,
            IccTagSignatures.DToB0, IccTagSignatures.DToB1, IccTagSignatures.DToB2, IccTagSignatures.DToB3,
            IccTagSignatures.GamutBoundary0, IccTagSignatures.GamutBoundary1,
            IccTagSignatures.GamutBoundary2, IccTagSignatures.GamutBoundary3,
            IccTagSignatures.HdrToSdr0, IccTagSignatures.HdrToSdr1,
            IccTagSignatures.HdrToSdr2, IccTagSignatures.HdrToSdr3,
            IccTagSignatures.MultiplexDefaultValues, IccTagSignatures.MultiplexTypeArray,
            IccTagSignatures.MeasurementInformation, IccTagSignatures.MeasurementInputInformation,
            IccTagSignatures.MediaWhitePoint, IccTagSignatures.Metadata,
            IccTagSignatures.MToA0,
            IccTagSignatures.MToB0, IccTagSignatures.MToB1, IccTagSignatures.MToB2, IccTagSignatures.MToB3,
            IccTagSignatures.MToS0, IccTagSignatures.MToS1, IccTagSignatures.MToS2, IccTagSignatures.MToS3,
            IccTagSignatures.NamedColors, IccTagSignatures.PerceptualRenderingIntentGamut,
            IccTagSignatures.ProfileDescription, IccTagSignatures.ProfileSequenceInformation,
            IccTagSignatures.ReferenceName, IccTagSignatures.SaturationRenderingIntentGamut,
            IccTagSignatures.SourcePcc, IccTagSignatures.SpectralViewingConditions,
            IccTagSignatures.SpectralWhitePoint, IccTagSignatures.StandardToCustomPcc,
            IccTagSignatures.SurfaceMap, IccTagSignatures.Technology
        };

        Assert.Multiple(() =>
        {
            Assert.That(Icc2TagRegistry.Definitions, Has.Length.EqualTo(117));
            Assert.That(Icc2TagRegistry.Definitions.Select(item => item.TagSignature), Is.Unique);
            Assert.That(Icc2TagRegistry.Definitions.Select(item => item.TagSignature), Is.EquivalentTo(expected));
        });
    }

    [Test]
    public void MapsPermittedTypesAndRegisteredContainerIdentifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Find(IccTagSignatures.AToB3).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.LutAToB, IccTypeSignatures.MultiProcessElements }));
            Assert.That(Find(IccTagSignatures.BToA3).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.LutBToA, IccTypeSignatures.MultiProcessElements }));
            Assert.That(Find(IccTagSignatures.Copyright).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.MultiLocalizedUnicode, IccTypeSignatures.Text, IccTypeSignatures.Utf8 }));
            Assert.That(Find(IccTagSignatures.MultiplexDefaultValues).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.UInt8Array, IccTypeSignatures.UInt16Array, IccTypeSignatures.Float16Array, IccTypeSignatures.Float32Array }));
            Assert.That(Find(IccTagSignatures.SpectralWhitePoint).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.Float16Array, IccTypeSignatures.Float32Array, IccTypeSignatures.UInt16Array }));
            Assert.That(Find(IccTagSignatures.SurfaceMap).PermittedTypes,
                Is.EqualTo(new[] { IccTypeSignatures.EmbeddedNormalImage, IccTypeSignatures.EmbeddedHeightImage }));
            Assert.That(Find(IccTagSignatures.BrdfMToS2).ContainerSignature, Is.EqualTo(IccStructureSignatures.BrdfTransform));
            Assert.That(Find(IccTagSignatures.ColorEncodingParameters).ContainerSignature, Is.EqualTo(IccStructureSignatures.ColorEncodingParameters));
            Assert.That(Find(IccTagSignatures.ColorantInfo).ContainerSignature, Is.EqualTo(IccArraySignatures.ColorantInformation));
            Assert.That(Find(IccTagSignatures.MultiplexTypeArray).ContainerSignature, Is.EqualTo(IccArraySignatures.MultiplexType));
            Assert.That(Find(IccTagSignatures.NamedColors).ContainerSignature, Is.EqualTo(IccArraySignatures.NamedColor));
            Assert.That(Find(IccTagSignatures.ProfileSequenceInformation).ContainerSignature, Is.EqualTo(IccArraySignatures.ProfileInformation));
            Assert.That(Find(IccTagSignatures.SourcePcc).ContainerSignature, Is.EqualTo(IccStructureSignatures.ProfileConnectionConditions));
            Assert.That(Find(IccTagSignatures.DevicePcc).PermittedTypes, Is.EqualTo(new[] { IccTypeSignatures.TagStructure }));
            Assert.That(Find(IccTagSignatures.DevicePcc).ContainerSignature, Is.EqualTo(IccStructureSignatures.ProfileConnectionConditions));
            Assert.That(Find(IccTagSignatures.DeviceSpectralRange).PermittedTypes, Is.EqualTo(new[] { IccTypeSignatures.SpectralRange }));
        });
    }

    [Test]
    public void RecordsRolesIntentsClassesAndChannelSemantics()
    {
        Icc2TagDefinition brdf = Find(IccTagSignatures.BrdfBToD2);
        Icc2TagDefinition directional = Find(IccTagSignatures.DirectionalDToB3);
        Icc2TagDefinition hdr = Find(IccTagSignatures.HdrToSdr1);
        Icc2TagDefinition sourcePcc = Find(IccTagSignatures.SourcePcc);
        TagDecodeState state = CreateState(IccTagSignatures.AToB0);
        state.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        state.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        state.SpectralPcsSignature = 31;
        state.MaterialConnectionSpaceSignature = 5;

        Assert.Multiple(() =>
        {
            Assert.That(brdf.Role, Is.EqualTo(Icc2TagRole.BrdfTransform));
            Assert.That(brdf.RenderingIntent, Is.EqualTo(2));
            Assert.That(brdf.InputChannels, Is.EqualTo(Icc2ChannelSource.SpectralPcsWithGeometry));
            Assert.That(directional.Role, Is.EqualTo(Icc2TagRole.DirectionalTransform));
            Assert.That(directional.RenderingIntent, Is.EqualTo(3));
            Assert.That(hdr.Role, Is.EqualTo(Icc2TagRole.HdrToSdrTransform));
            Assert.That(hdr.InputChannels, Is.EqualTo(Icc2ChannelSource.ColorimetricPcs));
            Assert.That(hdr.OutputChannels, Is.EqualTo(Icc2ChannelSource.ColorimetricPcs));
            Assert.That(Icc2TagRegistry.IsProfileClassAllowed(sourcePcc, IccProfileClass.Abstract), Is.True);
            Assert.That(Icc2TagRegistry.IsProfileClassAllowed(sourcePcc, IccProfileClass.Input), Is.False);
            Assert.That(Icc2TagRegistry.IsProfileClassAllowed(Find(IccTagSignatures.ColorantInfoOut), IccProfileClass.DeviceLink), Is.True);
            Assert.That(Icc2TagRegistry.IsProfileClassAllowed(Find(IccTagSignatures.ColorantInfoOut), IccProfileClass.Output), Is.False);
            Assert.That(Icc2TagRegistry.ResolveChannelCount(Icc2ChannelSource.DataColorSpaceWithGeometry, state), Is.EqualTo(7));
            Assert.That(Icc2TagRegistry.ResolveChannelCount(Icc2ChannelSource.SpectralPcsWithGeometry, state), Is.EqualTo(35));
            Assert.That(Icc2TagRegistry.ResolveChannelCount(Icc2ChannelSource.MaterialConnectionSpace, state), Is.EqualTo(5));
            Assert.That(Icc2TagRegistry.ResolveChannelCount(Icc2ChannelSource.Three, state), Is.EqualTo(3));
        });
    }

    [Test]
    public void ValidatesTagTypesAndTypedContainerIdentifiers()
    {
        TagDecodeState wrongType = CreateState(IccTagSignatures.SpectralWhitePoint);
        byte[] wrongTypePayload = CreatePayload(IccTypeSignatures.Float64Array, 0);
        bool wrongTypeValid = Icc2TagRegistry.ValidateTagContext(wrongType, wrongTypePayload, IccTypeSignatures.Float64Array);

        TagDecodeState wrongContainer = CreateState(IccTagSignatures.ColorantInfo);
        byte[] wrongContainerPayload = CreatePayload(IccTypeSignatures.TagArray, IccArraySignatures.ProfileInformation);
        bool wrongContainerValid = Icc2TagRegistry.ValidateTagContext(wrongContainer, wrongContainerPayload, IccTypeSignatures.TagArray);

        TagDecodeState valid = CreateState(IccTagSignatures.ColorantInfo);
        byte[] validPayload = CreatePayload(IccTypeSignatures.TagArray, IccArraySignatures.ColorantInformation);
        bool isValid = Icc2TagRegistry.ValidateTagContext(valid, validPayload, IccTypeSignatures.TagArray);

        TagDecodeState privateTag = CreateState(0x70726976);
        bool privateValid = Icc2TagRegistry.ValidateTagContext(privateTag, wrongTypePayload, IccTypeSignatures.Float64Array);

        TagDecodeState version4 = CreateState(IccTagSignatures.ColorantInfo);
        version4.ProfileVersion = IccVersionKind.Version4;
        bool version4Valid = Icc2TagRegistry.ValidateTagContext(version4, wrongContainerPayload, IccTypeSignatures.TagArray);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTypeValid, Is.False);
            Assert.That(wrongType.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongType.Result.Message, Does.Contain("'swpt'").And.Contain("'fl64'"));
            Assert.That(wrongContainerValid, Is.False);
            Assert.That(wrongContainer.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongContainer.Result.Message, Does.Contain("'cinf'").And.Contain("'pinf'"));
            Assert.That(isValid, Is.True);
            Assert.That(privateValid, Is.True);
            Assert.That(version4Valid, Is.True);
        });
    }

    [Test]
    public void RegistersCurrentSourcePccAndContextualMappings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccTagSignatures.SourcePcc, Is.EqualTo(0x73504343));
            Assert.That(IccArraySignatures.ColorantInformation, Is.EqualTo(0x63696e66));
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.HdrToSdr2), Is.True);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.ColorantInfo), Is.True);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.SourcePcc), Is.True);
        });
    }

    private static Icc2TagDefinition Find(uint signature)
    {
        Icc2TagDefinition? definition = Icc2TagRegistry.FindDefinition(signature);
        Assert.That(definition, Is.Not.Null);
        return definition!;
    }

    private static TagDecodeState CreateState(uint tagSignature)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(new byte[16]), 0, 16, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        return state;
    }

    private static byte[] CreatePayload(uint typeSignature, uint containerSignature)
    {
        byte[] payload = new byte[16];
        WriteUInt32(payload, 0, typeSignature);
        WriteUInt32(payload, 8, containerSignature);
        return payload;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
