namespace icc;

internal static class Icc2ProfileClassMasks
{
    internal const int Input = 1;
    internal const int Display = 2;
    internal const int Output = 4;
    internal const int DeviceLink = 8;
    internal const int ColorSpace = 16;
    internal const int Abstract = 32;
    internal const int NamedColor = 64;
    internal const int ColorEncodingSpace = 128;
    internal const int MultiplexIdentification = 256;
    internal const int MultiplexLink = 512;
    internal const int MultiplexVisualization = 1024;
    internal const int All = 2047;
}

internal static class Icc2TagRegistry
{
    internal static readonly uint[] LutForwardOrMpe = new uint[] { IccTypeSignatures.LutAToB, IccTypeSignatures.MultiProcessElements };
    internal static readonly uint[] LutReverseOrMpe = new uint[] { IccTypeSignatures.LutBToA, IccTypeSignatures.MultiProcessElements };
    internal static readonly uint[] MpeOnly = new uint[] { IccTypeSignatures.MultiProcessElements };
    internal static readonly uint[] StructureOnly = new uint[] { IccTypeSignatures.TagStructure };
    internal static readonly uint[] ArrayOnly = new uint[] { IccTypeSignatures.TagArray };
    internal static readonly uint[] DateTimeOnly = new uint[] { IccTypeSignatures.DateTime };
    internal static readonly uint[] Utf8OrZippedUtf8 = new uint[] { IccTypeSignatures.Utf8, IccTypeSignatures.ZippedUtf8 };
    internal static readonly uint[] CicpOnly = new uint[] { IccTypeSignatures.Cicp };
    internal static readonly uint[] Utf8Only = new uint[] { IccTypeSignatures.Utf8 };
    internal static readonly uint[] ColorantOrderOnly = new uint[] { IccTypeSignatures.ColorantOrder };
    internal static readonly uint[] SignatureOnly = new uint[] { IccTypeSignatures.Signature };
    internal static readonly uint[] CopyrightTypes = new uint[] { IccTypeSignatures.MultiLocalizedUnicode, IccTypeSignatures.Text, IccTypeSignatures.Utf8 };
    internal static readonly uint[] LocalizedTextOnly = new uint[] { IccTypeSignatures.MultiLocalizedUnicode };
    internal static readonly uint[] GamutBoundaryOnly = new uint[] { IccTypeSignatures.GamutBoundaryDescription };
    internal static readonly uint[] MultiplexDefaultTypes = new uint[] { IccTypeSignatures.UInt8Array, IccTypeSignatures.UInt16Array, IccTypeSignatures.Float16Array, IccTypeSignatures.Float32Array };
    internal static readonly uint[] XyzOnly = new uint[] { IccTypeSignatures.Xyz };
    internal static readonly uint[] DictionaryOnly = new uint[] { IccTypeSignatures.Dictionary };
    internal static readonly uint[] SpectralViewingConditionsOnly = new uint[] { IccTypeSignatures.SpectralViewingConditions };
    internal static readonly uint[] SpectralRangeOnly = new uint[] { IccTypeSignatures.SpectralRange };
    internal static readonly uint[] SpectralWhitePointTypes = new uint[] { IccTypeSignatures.Float16Array, IccTypeSignatures.Float32Array, IccTypeSignatures.UInt16Array };
    internal static readonly uint[] SurfaceMapTypes = new uint[] { IccTypeSignatures.EmbeddedNormalImage, IccTypeSignatures.EmbeddedHeightImage };

    internal static readonly Icc2TagDefinition[] Definitions = new Icc2TagDefinition[]
    {
        CreateTransformDefinition(IccTagSignatures.AToB0, LutForwardOrMpe, Icc2TagRole.ColorimetricTransform, 0, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateTransformDefinition(IccTagSignatures.AToB1, LutForwardOrMpe, Icc2TagRole.ColorimetricTransform, 1, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateTransformDefinition(IccTagSignatures.AToB2, LutForwardOrMpe, Icc2TagRole.ColorimetricTransform, 2, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateTransformDefinition(IccTagSignatures.AToB3, LutForwardOrMpe, Icc2TagRole.ColorimetricTransform, 3, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateTransformDefinition(IccTagSignatures.AToM0, MpeOnly, Icc2TagRole.MaterialTransform, -1, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.MaterialConnectionSpace),

        CreateBrdfDefinition(IccTagSignatures.BrdfColorimetricParameter0, 0, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfColorimetricParameter1, 1, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfColorimetricParameter2, 2, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfColorimetricParameter3, 3, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfSpectralParameter0, 0, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfSpectralParameter1, 1, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfSpectralParameter2, 2, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateBrdfDefinition(IccTagSignatures.BrdfSpectralParameter3, 3, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),

        CreateIntentFamilyDefinition(IccTagSignatures.BrdfAToB0, MpeOnly, Icc2TagRole.BrdfTransform, 0, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfAToB1, MpeOnly, Icc2TagRole.BrdfTransform, 1, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfAToB2, MpeOnly, Icc2TagRole.BrdfTransform, 2, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfAToB3, MpeOnly, Icc2TagRole.BrdfTransform, 3, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToA0, MpeOnly, Icc2TagRole.BrdfTransform, 0, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToA1, MpeOnly, Icc2TagRole.BrdfTransform, 1, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToA2, MpeOnly, Icc2TagRole.BrdfTransform, 2, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToA3, MpeOnly, Icc2TagRole.BrdfTransform, 3, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToD0, MpeOnly, Icc2TagRole.BrdfTransform, 0, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToD1, MpeOnly, Icc2TagRole.BrdfTransform, 1, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToD2, MpeOnly, Icc2TagRole.BrdfTransform, 2, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfBToD3, MpeOnly, Icc2TagRole.BrdfTransform, 3, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfDToB0, MpeOnly, Icc2TagRole.BrdfTransform, 0, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfDToB1, MpeOnly, Icc2TagRole.BrdfTransform, 1, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfDToB2, MpeOnly, Icc2TagRole.BrdfTransform, 2, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateIntentFamilyDefinition(IccTagSignatures.BrdfDToB3, MpeOnly, Icc2TagRole.BrdfTransform, 3, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToB0, 0, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToB1, 1, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToB2, 2, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToB3, 3, Icc2ChannelSource.ColorimetricPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToS0, 0, Icc2ChannelSource.SpectralPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToS1, 1, Icc2ChannelSource.SpectralPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToS2, 2, Icc2ChannelSource.SpectralPcs),
        CreateBrdfMaterialDefinition(IccTagSignatures.BrdfMToS3, 3, Icc2ChannelSource.SpectralPcs),

        CreateTransformDefinition(IccTagSignatures.BToA0, LutReverseOrMpe, Icc2TagRole.ColorimetricTransform, 0, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToA1, LutReverseOrMpe, Icc2TagRole.ColorimetricTransform, 1, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToA2, LutReverseOrMpe, Icc2TagRole.ColorimetricTransform, 2, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToA3, LutReverseOrMpe, Icc2TagRole.ColorimetricTransform, 3, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.DataColorSpace),
        CreateSpectralFamilyDefinition(IccTagSignatures.BToD0, 0, Icc2ChannelSource.SpectralPcs, Icc2ChannelSource.DataColorSpace),
        CreateSpectralFamilyDefinition(IccTagSignatures.BToD1, 1, Icc2ChannelSource.SpectralPcs, Icc2ChannelSource.DataColorSpace),
        CreateSpectralFamilyDefinition(IccTagSignatures.BToD2, 2, Icc2ChannelSource.SpectralPcs, Icc2ChannelSource.DataColorSpace),
        CreateSpectralFamilyDefinition(IccTagSignatures.BToD3, 3, Icc2ChannelSource.SpectralPcs, Icc2ChannelSource.DataColorSpace),

        CreateRecordDefinition(IccTagSignatures.CalibrationDateTime, DateTimeOnly, Icc2TagRole.Calibration, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.CharacterizationTarget, Utf8OrZippedUtf8, Icc2TagRole.Characterization, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.Cicp, CicpOnly, Icc2TagRole.Metadata, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.Input | Icc2ProfileClassMasks.Display),
        CreateRecordDefinition(IccTagSignatures.ColorEncodingParameters, StructureOnly, Icc2TagRole.ColorEncoding, Icc2ChannelSource.DataColorSpace, IccStructureSignatures.ColorEncodingParameters, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ColorSpaceName, Utf8Only, Icc2TagRole.ColorEncoding, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ColorantOrder, ColorantOrderOnly, Icc2TagRole.ColorantOrder, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ColorantOrderOut, ColorantOrderOnly, Icc2TagRole.ColorantOrder, Icc2ChannelSource.ColorimetricPcs, 0, Icc2ProfileClassMasks.DeviceLink),
        CreateRecordDefinition(IccTagSignatures.ColorantInfo, ArrayOnly, Icc2TagRole.ColorantInformation, Icc2ChannelSource.DataColorSpace, IccArraySignatures.ColorantInformation, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ColorantInfoOut, ArrayOnly, Icc2TagRole.ColorantInformation, Icc2ChannelSource.ColorimetricPcs, IccArraySignatures.ColorantInformation, Icc2ProfileClassMasks.DeviceLink),
        CreateRecordDefinition(IccTagSignatures.ColorimetricIntentImageState, SignatureOnly, Icc2TagRole.ImageState, Icc2ChannelSource.ColorimetricPcs, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.Copyright, CopyrightTypes, Icc2TagRole.Description, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateTransformDefinition(IccTagSignatures.CustomToStandardPcc, MpeOnly, Icc2TagRole.PccTransform, -1, Icc2ChannelSource.Three, Icc2ChannelSource.Three),
        CreateRecordDefinition(IccTagSignatures.Cxf, Utf8OrZippedUtf8, Icc2TagRole.MeasurementData, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.DeviceManufacturerDescription, LocalizedTextOnly, Icc2TagRole.Description, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.DeviceModelDescription, LocalizedTextOnly, Icc2TagRole.Description, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.DevicePcc, StructureOnly, Icc2TagRole.DevicePcc, Icc2ChannelSource.DataColorSpace, IccStructureSignatures.ProfileConnectionConditions, Icc2ProfileClassMasks.Abstract),
        CreateRecordDefinition(IccTagSignatures.DeviceSpectralRange, SpectralRangeOnly, Icc2TagRole.DeviceSpectralRange, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.All),

        CreateDirectionalDefinition(IccTagSignatures.DirectionalAToB0, 0, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalAToB1, 1, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalAToB2, 2, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalAToB3, 3, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.ColorimetricPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToA0, 0, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToA1, 1, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToA2, 2, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToA3, 3, Icc2ChannelSource.ColorimetricPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToD0, 0, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToD1, 1, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToD2, 2, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalBToD3, 3, Icc2ChannelSource.SpectralPcsWithGeometry, Icc2ChannelSource.DataColorSpace),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalDToB0, 0, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalDToB1, 1, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalDToB2, 2, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),
        CreateDirectionalDefinition(IccTagSignatures.DirectionalDToB3, 3, Icc2ChannelSource.DataColorSpaceWithGeometry, Icc2ChannelSource.SpectralPcs),

        CreateSpectralFamilyDefinition(IccTagSignatures.DToB0, 0, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateSpectralFamilyDefinition(IccTagSignatures.DToB1, 1, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateSpectralFamilyDefinition(IccTagSignatures.DToB2, 2, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateSpectralFamilyDefinition(IccTagSignatures.DToB3, 3, Icc2ChannelSource.DataColorSpace, Icc2ChannelSource.SpectralPcs),
        CreateGamutDefinition(IccTagSignatures.GamutBoundary0, 0),
        CreateGamutDefinition(IccTagSignatures.GamutBoundary1, 1),
        CreateGamutDefinition(IccTagSignatures.GamutBoundary2, 2),
        CreateGamutDefinition(IccTagSignatures.GamutBoundary3, 3),
        CreateHdrDefinition(IccTagSignatures.HdrToSdr0, 0),
        CreateHdrDefinition(IccTagSignatures.HdrToSdr1, 1),
        CreateHdrDefinition(IccTagSignatures.HdrToSdr2, 2),
        CreateHdrDefinition(IccTagSignatures.HdrToSdr3, 3),
        CreateRecordDefinition(IccTagSignatures.MultiplexDefaultValues, MultiplexDefaultTypes, Icc2TagRole.Multiplex, Icc2ChannelSource.MaterialConnectionSpace, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.MultiplexTypeArray, ArrayOnly, Icc2TagRole.Multiplex, Icc2ChannelSource.MaterialConnectionSpace, IccArraySignatures.MultiplexType, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.MeasurementInformation, StructureOnly, Icc2TagRole.Measurement, Icc2ChannelSource.ColorimetricPcs, IccStructureSignatures.MeasurementInformation, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.MeasurementInputInformation, StructureOnly, Icc2TagRole.Measurement, Icc2ChannelSource.DataColorSpace, IccStructureSignatures.MeasurementInformation, Icc2ProfileClassMasks.Abstract),
        CreateRecordDefinition(IccTagSignatures.MediaWhitePoint, XyzOnly, Icc2TagRole.Measurement, Icc2ChannelSource.ColorimetricPcs, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.Metadata, DictionaryOnly, Icc2TagRole.Metadata, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateTransformDefinition(IccTagSignatures.MToA0, MpeOnly, Icc2TagRole.MaterialTransform, -1, Icc2ChannelSource.MaterialConnectionSpace, Icc2ChannelSource.DataColorSpace),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToB0, 0, Icc2ChannelSource.ColorimetricPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToB1, 1, Icc2ChannelSource.ColorimetricPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToB2, 2, Icc2ChannelSource.ColorimetricPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToB3, 3, Icc2ChannelSource.ColorimetricPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToS0, 0, Icc2ChannelSource.SpectralPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToS1, 1, Icc2ChannelSource.SpectralPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToS2, 2, Icc2ChannelSource.SpectralPcs),
        CreateMaterialFamilyDefinition(IccTagSignatures.MToS3, 3, Icc2ChannelSource.SpectralPcs),
        CreateRecordDefinition(IccTagSignatures.NamedColors, ArrayOnly, Icc2TagRole.NamedColor, Icc2ChannelSource.DataColorSpace, IccArraySignatures.NamedColor, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.PerceptualRenderingIntentGamut, SignatureOnly, Icc2TagRole.Metadata, Icc2ChannelSource.ColorimetricPcs, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ProfileDescription, LocalizedTextOnly, Icc2TagRole.Description, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ProfileSequenceInformation, ArrayOnly, Icc2TagRole.ProfileSequence, Icc2ChannelSource.None, IccArraySignatures.ProfileInformation, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.ReferenceName, Utf8Only, Icc2TagRole.ColorEncoding, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.SaturationRenderingIntentGamut, SignatureOnly, Icc2TagRole.Metadata, Icc2ChannelSource.ColorimetricPcs, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.SourcePcc, StructureOnly, Icc2TagRole.Pcc, Icc2ChannelSource.ColorimetricPcs, IccStructureSignatures.ProfileConnectionConditions, Icc2ProfileClassMasks.Abstract),
        CreateRecordDefinition(IccTagSignatures.SpectralViewingConditions, SpectralViewingConditionsOnly, Icc2TagRole.SpectralConditions, Icc2ChannelSource.SpectralPcs, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.SpectralWhitePoint, SpectralWhitePointTypes, Icc2TagRole.SpectralWhitePoint, Icc2ChannelSource.SpectralPcs, 0, Icc2ProfileClassMasks.All),
        CreateTransformDefinition(IccTagSignatures.StandardToCustomPcc, MpeOnly, Icc2TagRole.PccTransform, -1, Icc2ChannelSource.Three, Icc2ChannelSource.Three),
        CreateRecordDefinition(IccTagSignatures.SurfaceMap, SurfaceMapTypes, Icc2TagRole.Surface, Icc2ChannelSource.DataColorSpace, 0, Icc2ProfileClassMasks.All),
        CreateRecordDefinition(IccTagSignatures.Technology, SignatureOnly, Icc2TagRole.Technology, Icc2ChannelSource.None, 0, Icc2ProfileClassMasks.All)
    };

    internal static Icc2TagDefinition? FindDefinition(uint tagSignature)
    {
        int index = 0;
        while (index < Definitions.Length)
        {
            if (Definitions[index].TagSignature == tagSignature)
            {
                return Definitions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool ValidateTagContext(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (state.ProfileVersion != IccVersionKind.Version5 || state.TagSignature == 0)
        {
            return true;
        }

        Icc2TagDefinition? definition = FindDefinition(state.TagSignature);
        if (definition == null)
        {
            return true;
        }

        if (!ContainsType(definition.PermittedTypes, typeSignature))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"ICC.2 tag '{FourCcCodec.Format(state.TagSignature)}' permits {FormatPermittedTypes(definition.PermittedTypes)}, not '{FourCcCodec.Format(typeSignature)}'.");
            return false;
        }

        if (definition.ContainerSignature != 0 && payload.Length >= 12)
        {
            uint actualContainer = ReadUInt32(payload, 8);
            if (actualContainer != definition.ContainerSignature)
            {
                string kind = typeSignature == IccTypeSignatures.TagArray ? "array" : "structure";
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"ICC.2 tag '{FourCcCodec.Format(state.TagSignature)}' requires {kind} identifier '{FourCcCodec.Format(definition.ContainerSignature)}', not '{FourCcCodec.Format(actualContainer)}'.");
                return false;
            }
        }

        return true;
    }

    internal static bool IsProfileClassAllowed(Icc2TagDefinition definition, IccProfileClass profileClass)
    {
        int mask = GetProfileClassMask(profileClass);
        return mask != 0 && (definition.ProfileClassMask & mask) != 0;
    }

    internal static int ResolveChannelCount(Icc2ChannelSource source, TagDecodeState state)
    {
        int count = 0;
        if (source == Icc2ChannelSource.DataColorSpace || source == Icc2ChannelSource.DataColorSpaceWithGeometry)
        {
            count = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        }
        else if (source == Icc2ChannelSource.ColorimetricPcs || source == Icc2ChannelSource.ColorimetricPcsWithGeometry)
        {
            count = ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        }
        else if (source == Icc2ChannelSource.SpectralPcs || source == Icc2ChannelSource.SpectralPcsWithGeometry)
        {
            count = (int)(state.SpectralPcsSignature & 0x0000ffff);
        }
        else if (source == Icc2ChannelSource.MaterialConnectionSpace || source == Icc2ChannelSource.MaterialConnectionSpaceWithGeometry)
        {
            count = (int)(state.MaterialConnectionSpaceSignature & 0x0000ffff);
        }
        else if (source == Icc2ChannelSource.Three)
        {
            count = 3;
        }

        if (count > 0 && (source == Icc2ChannelSource.DataColorSpaceWithGeometry
            || source == Icc2ChannelSource.ColorimetricPcsWithGeometry
            || source == Icc2ChannelSource.SpectralPcsWithGeometry
            || source == Icc2ChannelSource.MaterialConnectionSpaceWithGeometry))
        {
            count = count + 4;
        }

        return count;
    }

    internal static bool ContainsType(uint[] permittedTypes, uint typeSignature)
    {
        int index = 0;
        while (index < permittedTypes.Length)
        {
            if (permittedTypes[index] == typeSignature)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string FormatPermittedTypes(uint[] permittedTypes)
    {
        string value = string.Empty;
        int index = 0;
        while (index < permittedTypes.Length)
        {
            if (index > 0)
            {
                value = value + (index == permittedTypes.Length - 1 ? " or " : ", ");
            }

            value = value + $"'{FourCcCodec.Format(permittedTypes[index])}'";
            index = index + 1;
        }

        return value;
    }

    internal static int GetProfileClassMask(IccProfileClass profileClass)
    {
        switch (profileClass)
        {
            case IccProfileClass.Input:
                return Icc2ProfileClassMasks.Input;
            case IccProfileClass.Display:
                return Icc2ProfileClassMasks.Display;
            case IccProfileClass.Output:
                return Icc2ProfileClassMasks.Output;
            case IccProfileClass.DeviceLink:
                return Icc2ProfileClassMasks.DeviceLink;
            case IccProfileClass.ColorSpace:
                return Icc2ProfileClassMasks.ColorSpace;
            case IccProfileClass.Abstract:
                return Icc2ProfileClassMasks.Abstract;
            case IccProfileClass.NamedColor:
                return Icc2ProfileClassMasks.NamedColor;
            case IccProfileClass.ColorEncodingSpace:
                return Icc2ProfileClassMasks.ColorEncodingSpace;
            case IccProfileClass.MultiplexIdentification:
                return Icc2ProfileClassMasks.MultiplexIdentification;
            case IccProfileClass.MultiplexLink:
                return Icc2ProfileClassMasks.MultiplexLink;
            case IccProfileClass.MultiplexVisualization:
                return Icc2ProfileClassMasks.MultiplexVisualization;
            default:
                return 0;
        }
    }

    internal static uint ReadUInt32(System.ReadOnlyMemory<byte> payload, int offset)
    {
        System.ReadOnlySpan<byte> span = payload.Span;
        return ((uint)span[offset] << 24)
            | ((uint)span[offset + 1] << 16)
            | ((uint)span[offset + 2] << 8)
            | span[offset + 3];
    }

    internal static Icc2TagDefinition CreateTransformDefinition(uint signature, uint[] types, Icc2TagRole role, int intent, Icc2ChannelSource input, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, types, role, intent, Icc2ProfileClassMasks.All, input, output, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateIntentFamilyDefinition(uint signature, uint[] types, Icc2TagRole role, int intent, Icc2ChannelSource input, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, types, role, intent, Icc2ProfileClassMasks.All, input, output, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateBrdfDefinition(uint signature, int intent, Icc2ChannelSource input, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, StructureOnly, Icc2TagRole.BrdfParameterTransform, intent, Icc2ProfileClassMasks.All, input, Icc2ChannelSource.Dynamic, output, IccStructureSignatures.BrdfTransform);
    }

    internal static Icc2TagDefinition CreateBrdfMaterialDefinition(uint signature, int intent, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, StructureOnly, Icc2TagRole.BrdfParameterTransform, intent, Icc2ProfileClassMasks.All, Icc2ChannelSource.MaterialConnectionSpace, Icc2ChannelSource.Dynamic, output, IccStructureSignatures.BrdfTransform);
    }

    internal static Icc2TagDefinition CreateSpectralFamilyDefinition(uint signature, int intent, Icc2ChannelSource input, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, MpeOnly, Icc2TagRole.SpectralTransform, intent, Icc2ProfileClassMasks.All, input, output, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateDirectionalDefinition(uint signature, int intent, Icc2ChannelSource input, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, MpeOnly, Icc2TagRole.DirectionalTransform, intent, Icc2ProfileClassMasks.All, input, output, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateGamutDefinition(uint signature, int intent)
    {
        return new Icc2TagDefinition(signature, GamutBoundaryOnly, Icc2TagRole.GamutBoundary, intent, Icc2ProfileClassMasks.All, Icc2ChannelSource.None, Icc2ChannelSource.None, Icc2ChannelSource.DataColorSpace, 0);
    }

    internal static Icc2TagDefinition CreateHdrDefinition(uint signature, int intent)
    {
        return new Icc2TagDefinition(signature, MpeOnly, Icc2TagRole.HdrToSdrTransform, intent, Icc2ProfileClassMasks.All, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.ColorimetricPcs, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateMaterialFamilyDefinition(uint signature, int intent, Icc2ChannelSource output)
    {
        return new Icc2TagDefinition(signature, MpeOnly, Icc2TagRole.MaterialTransform, intent, Icc2ProfileClassMasks.All, Icc2ChannelSource.MaterialConnectionSpace, output, Icc2ChannelSource.None, 0);
    }

    internal static Icc2TagDefinition CreateRecordDefinition(uint signature, uint[] types, Icc2TagRole role, Icc2ChannelSource associated, uint containerSignature, int profileClassMask)
    {
        return new Icc2TagDefinition(signature, types, role, -1, profileClassMask, Icc2ChannelSource.None, Icc2ChannelSource.None, associated, containerSignature);
    }
}
