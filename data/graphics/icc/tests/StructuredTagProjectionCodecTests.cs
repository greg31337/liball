namespace icc.Tests;

public class StructuredTagProjectionCodecTests
{
    [Test]
    public void BrdfStructureProjectsRequiredTypedMembers()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.BrdfTransform,
            Member(StructuredTagProjectionCodec.BrdfType, Signature(StructuredTagProjectionCodec.MonochromeBrdf)),
            Member(StructuredTagProjectionCodec.BrdfFunction, Signature(0x42506830)),
            Member(StructuredTagProjectionCodec.BrdfParametersPerChannel, Integers(IccTypeSignatures.UInt16Array, 4)),
            Member(StructuredTagProjectionCodec.BrdfTransform, MultiProcess()));

        TagDecodeState state = RoundTrip(structure);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.TagStructure, Is.Not.Null);
            Assert.That(state.Data.Brdf, Is.Not.Null);
            Assert.That(state.Data.Brdf!.TypeSignature, Is.EqualTo(StructuredTagProjectionCodec.MonochromeBrdf));
            Assert.That(state.Data.Brdf!.ModelSignature, Is.EqualTo(0x42506830));
            Assert.That(state.Data.Brdf.Parameters, Is.EqualTo(new double[] { 4.0 }));
        });
    }

    [Test]
    public void BrdfStructureRejectsRegisteredModelCountAndTypeMismatches()
    {
        IccTagStructure wrongCount = CreateStructure(IccStructureSignatures.BrdfTransform,
            Member(StructuredTagProjectionCodec.BrdfType, Signature(StructuredTagProjectionCodec.MonochromeBrdf)),
            Member(StructuredTagProjectionCodec.BrdfFunction, Signature(BrdfEvaluator.BlinnPhongMonochrome)),
            Member(StructuredTagProjectionCodec.BrdfParametersPerChannel, Integers(IccTypeSignatures.UInt16Array, 3)),
            Member(StructuredTagProjectionCodec.BrdfTransform, MultiProcess()));
        IccTagStructure wrongType = CreateStructure(IccStructureSignatures.BrdfTransform,
            Member(StructuredTagProjectionCodec.BrdfType, Signature(StructuredTagProjectionCodec.ColorBrdf)),
            Member(StructuredTagProjectionCodec.BrdfFunction, Signature(BrdfEvaluator.BlinnPhongMonochrome)),
            Member(StructuredTagProjectionCodec.BrdfParametersPerChannel, Integers(IccTypeSignatures.UInt16Array, 4)),
            Member(StructuredTagProjectionCodec.BrdfTransform, MultiProcess()));

        Assert.Multiple(() =>
        {
            Assert.That(() => HierarchicalTagCodec.SerializeTagStructure(wrongCount), Throws.TypeOf<IccProfileWriteException>().With.Message.Contains("requires 4"));
            Assert.That(() => HierarchicalTagCodec.SerializeTagStructure(wrongType), Throws.TypeOf<IccProfileWriteException>().With.Message.Contains("does not match"));
        });
    }

    [Test]
    public void SpectralBrdfParameterTagProjectsItsTransformToTheSpectralSlot()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.BrdfTransform,
            Member(StructuredTagProjectionCodec.BrdfType, Signature(StructuredTagProjectionCodec.MonochromeBrdf)),
            Member(StructuredTagProjectionCodec.BrdfFunction, Signature(BrdfEvaluator.BlinnPhongMonochrome)),
            Member(StructuredTagProjectionCodec.BrdfParametersPerChannel, Integers(IccTypeSignatures.UInt16Array, 4)),
            Member(StructuredTagProjectionCodec.BrdfTransform, MultiProcess()));
        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.TagSignature = IccTagSignatures.BrdfSpectralParameter0;

        TagCodecDispatcher.DecodeTagPayload(state);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Brdf!.ColorimetricTransform, Is.Null);
            Assert.That(state.Data.Brdf.SpectralTransform, Is.Not.Null);
        });
    }

    [Test]
    public void ColorantInfoProjectsNameAndNormalizedPcsValues()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.ColorantInformation,
            Member(StructuredTagProjectionCodec.Name, Text("Process Blue")),
            Member(StructuredTagProjectionCodec.PcsData, Integers(IccTypeSignatures.UInt16Array, 0, 32768, 65535)),
            Member(0x66757472, Raw(0x7a7a7a7a)));

        TagDecodeState state = RoundTripWithContext(structure, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Colorants, Is.Not.Null);
            Assert.That(state.Data.Colorants!.Colorants[0].Name, Is.EqualTo("Process Blue"));
            Assert.That(state.Data.Colorants.Colorants[0].PcsColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Lab));
            Assert.That(state.Data.Colorants.Colorants[0].PcsColor.Components, Is.EqualTo(new[] { 0.0, 32768.0 / 65535.0, 1.0 }));
            Assert.That(state.Data.TagStructure!.Members.Last().Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
        });
    }

    [Test]
    public void ColorEncodingParametersProjectWhitePointRangeMatrixAndCurves()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.ColorEncodingParameters,
            Member(StructuredTagProjectionCodec.WhitePointChromaticity, Floats(0.3127, 0.3290)),
            Member(StructuredTagProjectionCodec.EncodingRange, Floats(16.0, 235.0)),
            Member(StructuredTagProjectionCodec.LumaChromaMatrix, Floats(1, 0, 0, 0, 1, 0, 0, 0, 1)),
            Member(StructuredTagProjectionCodec.ViewingFlare, Floats(0.01)));

        TagDecodeState state = RoundTrip(structure);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ColorEncodingParameters, Is.Not.Null);
            Assert.That(state.Data.ColorEncodingParameters!.ReferenceWhite.X, Is.EqualTo(0.3127).Within(0.00001));
            Assert.That(state.Data.ColorEncodingParameters.ReferenceWhite.Z, Is.EqualTo(0.3583).Within(0.00001));
            Assert.That(state.Data.ColorEncodingParameters.ChannelRanges[0].Minimum, Is.EqualTo(16.0));
            Assert.That(state.Data.ColorEncodingParameters.Matrix!.Values, Is.EqualTo(new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }));
        });
    }

    [Test]
    public void MeasurementInfoProjectsDefaultsGeometryAndFlare()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.MeasurementInformation,
            Member(StructuredTagProjectionCodec.MeasurementBacking, Integers(IccTypeSignatures.UInt32Array, 1)),
            Member(StructuredTagProjectionCodec.MeasurementGeometry, Integers(IccTypeSignatures.UInt32Array, 2)),
            Member(StructuredTagProjectionCodec.MeasurementFlare, Floats(0.125)),
            Member(StructuredTagProjectionCodec.MeasurementMode, Integers(IccTypeSignatures.UInt32Array, 3)),
            Member(StructuredTagProjectionCodec.MeasurementIlluminant, FloatsWithType(IccTypeSignatures.Float16Array, 0.5, 1.0)),
            Member(StructuredTagProjectionCodec.MeasurementIlluminantRange, SpectralRanges()));

        TagDecodeState state = RoundTrip(structure);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Measurement, Is.Not.Null);
            Assert.That(state.Data.Measurement!.Geometry, Is.EqualTo(IccMeasurementGeometry.Degree0OrDiffuse));
            Assert.That(state.Data.Measurement.Flare, Is.EqualTo(0.125).Within(0.00001));
        });
    }

    [Test]
    public void NamedColorProjectsTintVectorsAndNormalizesIntegerTints()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.NamedColor,
            Member(StructuredTagProjectionCodec.Name, Text("Orange")),
            Member(StructuredTagProjectionCodec.TintValues, Integers(IccTypeSignatures.UInt8Array, 128, 255)),
            Member(StructuredTagProjectionCodec.DeviceData, Floats(0.1, 0.2, 0.3, 0.4, 0.5, 0.6)),
            Member(StructuredTagProjectionCodec.PcsData, Floats(0.2, 0.3, 0.4, 0.5, 0.6, 0.7)),
            Member(0x70726976, Raw(0x78787878)));

        TagDecodeState state = RoundTripWithContext(structure, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.NamedColors, Is.Not.Null);
            Assert.That(state.Data.NamedColors!.Colors[0].Name, Is.EqualTo("Orange"));
            Assert.That(state.Data.NamedColors.Colors[0].Tints, Is.EqualTo(new[] { 128.0 / 255.0, 1.0 }));
            Assert.That(state.Data.NamedColors.Colors[0].DeviceColor.Components.Length, Is.EqualTo(6));
            Assert.That(state.Data.NamedColors.Colors[0].PcsColor.Components.Length, Is.EqualTo(6));
        });
    }

    [Test]
    public void TintZeroProjectsSingleDeviceAndPcsVectors()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.TintZero,
            Member(StructuredTagProjectionCodec.DeviceData, Integers(IccTypeSignatures.UInt8Array, 255, 255, 255)),
            Member(StructuredTagProjectionCodec.PcsData, Floats(1.0, 0.0, 0.0)));

        TagDecodeState state = RoundTripWithContext(structure, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.NamedColors, Is.Not.Null);
            Assert.That(state.Data.NamedColors!.Colors[0].DeviceColor.Components, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(state.Data.NamedColors.Colors[0].PcsColor.Components, Is.EqualTo(new[] { 1.0, 0.0, 0.0 }));
            Assert.That(state.Data.NamedColors.Colors[0].Tints, Is.Empty);
        });
    }

    [Test]
    public void PccProjectsIlluminantMediaWhiteAndRequiresTransformPairs()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.ProfileConnectionConditions,
            Member(StructuredTagProjectionCodec.PccIlluminant, Xyz(0.9642, 1.0, 0.8249)),
            Member(StructuredTagProjectionCodec.PccMediaWhitePoint, Xyz(0.8, 0.9, 0.7)),
            Member(StructuredTagProjectionCodec.PccSpectralWhitePoint, Floats(0.75, 0.8, 0.85)),
            Member(StructuredTagProjectionCodec.PccCustomToStandard, MultiProcess()),
            Member(StructuredTagProjectionCodec.PccStandardToCustom, MultiProcess()));

        TagDecodeState state = RoundTrip(structure);
        IccTagStructure unpaired = CreateStructure(IccStructureSignatures.ProfileConnectionConditions,
            Member(StructuredTagProjectionCodec.PccIlluminant, Xyz(0.9642, 1.0, 0.8249)),
            Member(StructuredTagProjectionCodec.PccMediaWhitePoint, Xyz(0.8, 0.9, 0.7)),
            Member(StructuredTagProjectionCodec.PccCustomToStandard, MultiProcess()));

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ProfileConnectionConditions, Is.Not.Null);
            Assert.That(state.Data.ProfileConnectionConditions!.ViewingConditions.IlluminantColor.X, Is.EqualTo(0.9642).Within(0.00002));
            Assert.That(state.Data.ProfileConnectionConditions.MediaWhitePoint.Y, Is.EqualTo(0.9).Within(0.00002));
            Assert.That(state.Data.ProfileConnectionConditions.SpectralWhitePoint, Is.EqualTo(new double[] { 0.75, 0.8, 0.85 }).Within(0.00001));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(unpaired));
        });
    }

    [Test]
    public void DevicePccRequiresAbstractPcsContextAndProjectsSpectralWhitePoint()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.ProfileConnectionConditions,
            Member(StructuredTagProjectionCodec.PccIlluminant, Xyz(0.9642, 1.0, 0.8249)),
            Member(StructuredTagProjectionCodec.PccMediaWhitePoint, Xyz(0.8, 0.9, 0.7)),
            Member(StructuredTagProjectionCodec.PccSpectralWhitePoint, Floats(0.75, 0.8, 0.85)),
            Member(StructuredTagProjectionCodec.PccViewingConditions, SpectralViewingConditions()),
            Member(StructuredTagProjectionCodec.PccCustomToStandard, MultiProcessThree()),
            Member(StructuredTagProjectionCodec.PccStandardToCustom, MultiProcessThree()));
        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);

        TagDecodeState valid = DecodeDevicePcc(payload, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 3u, IccProfileClass.Abstract);
        TagDecodeState wrongClass = DecodeDevicePcc(payload, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 3u, IccProfileClass.Display);
        TagDecodeState wrongSpace = DecodeDevicePcc(payload, IccColorSpaceSignatures.Rgb, IccProfileClass.Abstract);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(valid.Data.ProfileConnectionConditions, Is.Not.Null);
            Assert.That(valid.Data.ProfileConnectionConditions!.SpectralWhitePoint, Is.EqualTo(new double[] { 0.75, 0.8, 0.85 }).Within(0.00001));
            Assert.That(valid.Data.ProfileConnectionConditions.ViewingConditions.Observer, Is.EqualTo(IccMeasurementObserver.Cie1931TwoDegree));
            Assert.That(wrongClass.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongSpace.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void ProfileInfoProjectsHeaderIdentityAndLocalizedDescriptions()
    {
        IccTagStructure structure = CreateStructure(IccStructureSignatures.ProfileInformation,
            Member(StructuredTagProjectionCodec.ProfileAttributes, Integers(IccTypeSignatures.UInt64Array, 9)),
            Member(StructuredTagProjectionCodec.ManufacturerSignature, Signature(0x41424344)),
            Member(StructuredTagProjectionCodec.ModelSignature, Signature(0x45464748)),
            Member(StructuredTagProjectionCodec.Technology, Signature(0x696a6b6c)),
            Member(StructuredTagProjectionCodec.ManufacturerDescription, Localized("Maker")),
            Member(StructuredTagProjectionCodec.ModelDescription, Localized("Model")),
            Member(StructuredTagProjectionCodec.ProfileId, Integers(IccTypeSignatures.UInt8Array, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)));

        TagDecodeState state = RoundTrip(structure);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ProfileSequence, Has.Length.EqualTo(1));
            Assert.That(state.Data.ProfileSequence[0].DeviceAttributes, Is.EqualTo(9));
            Assert.That(state.Data.ProfileSequence[0].DeviceManufacturerSignature, Is.EqualTo(0x41424344));
            Assert.That(state.Data.ProfileSequence[0].DeviceModelSignature, Is.EqualTo(0x45464748));
            Assert.That(state.Data.ProfileSequence[0].ManufacturerDescription.Text, Is.EqualTo("Maker"));
            Assert.That(state.Data.ProfileSequence[0].ModelDescription.Text, Is.EqualTo("Model"));
        });
    }

    [Test]
    public void RegisteredStructuresRejectMissingWrongAndInconsistentMembers()
    {
        IccTagStructure missingBrdf = CreateStructure(0x756e6b6e, Member(0x70726976, Raw(0x71717171)));
        byte[] missingPayload = HierarchicalTagCodec.SerializeTagStructure(missingBrdf);
        WriteUInt32(missingPayload, 8, IccStructureSignatures.BrdfTransform);
        TagDecodeState missingState = Decode(missingPayload, 0, 0);

        IccTagStructure badColorant = CreateStructure(IccStructureSignatures.ColorantInformation,
            Member(StructuredTagProjectionCodec.Name, Signature(0x6e616d65)));
        IccTagStructure badTints = CreateStructure(IccStructureSignatures.NamedColor,
            Member(StructuredTagProjectionCodec.Name, Text("Bad")),
            Member(StructuredTagProjectionCodec.TintValues, Floats(0.75, 0.5)));
        IccTagStructure badTintZero = CreateStructure(IccStructureSignatures.TintZero,
            Member(StructuredTagProjectionCodec.TintValues, Floats(0.5)));

        Assert.Multiple(() =>
        {
            Assert.That(missingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(badColorant));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(badTints));
            Assert.Throws<IccProfileWriteException>(() => HierarchicalTagCodec.SerializeTagStructure(badTintZero));
        });
    }

    private static IccTagStructure CreateStructure(uint signature, params IccStructureMember[] members)
    {
        return new IccTagStructure { StructureSignature = signature, Members = members };
    }

    private static IccStructureMember Member(uint signature, IccTagData data)
    {
        return new IccStructureMember { Signature = signature, Data = data };
    }

    private static IccTagData Text(string value)
    {
        return new IccTagData { Kind = IccTagDataKind.Text, Text = new IccTextData { TypeSignature = IccTypeSignatures.Utf8, Text = value } };
    }

    private static IccTagData Localized(string value)
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.LocalizedText,
            LocalizedStrings = new IccLocalizedString[] { new IccLocalizedString { Language = "en", Country = "US", Text = value } }
        };
    }

    private static IccTagData Signature(uint value)
    {
        return new IccTagData { Kind = IccTagDataKind.Signature, Signature = value };
    }

    private static IccTagData Integers(uint typeSignature, params long[] values)
    {
        IccTagData data = new IccTagData { Kind = IccTagDataKind.IntegerArray, IntegerValues = values };
        data.RawData.TypeSignature = typeSignature;
        data.RawData.HasTypeSignature = true;
        return data;
    }

    private static IccTagData Floats(params double[] values)
    {
        return FloatsWithType(IccTypeSignatures.Float32Array, values);
    }

    private static IccTagData FloatsWithType(uint typeSignature, params double[] values)
    {
        IccTagData data = new IccTagData { Kind = IccTagDataKind.FloatingPointArray, FloatingPointValues = values };
        data.RawData.TypeSignature = typeSignature;
        data.RawData.HasTypeSignature = true;
        return data;
    }

    private static IccTagData Xyz(double x, double y, double z)
    {
        return new IccTagData { Kind = IccTagDataKind.XyzArray, XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } } };
    }

    private static IccTagData SpectralRanges()
    {
        IccTagData data = new IccTagData
        {
            Kind = IccTagDataKind.SpectralRange,
            SpectralRanges = new IccSpectralRanges
            {
                SpectralRange = new IccSpectralRange { StartWavelength = 400.0, EndWavelength = 700.0, Steps = 31 }
            }
        };
        data.RawData.TypeSignature = IccTypeSignatures.SpectralRange;
        data.RawData.HasTypeSignature = true;
        return data;
    }

    private static IccTagData Raw(uint typeSignature)
    {
        byte[] bytes = new byte[8];
        WriteUInt32(bytes, 0, typeSignature);
        IccTagData data = new IccTagData { Kind = IccTagDataKind.Raw };
        data.RawData.TypeSignature = typeSignature;
        data.RawData.HasTypeSignature = true;
        data.RawData.Bytes = bytes;
        return data;
    }

    private static IccTagData MultiProcess()
    {
        byte[] bytes = new byte[12];
        WriteUInt32(bytes, 0, 0x66757472);
        bytes[9] = 1;
        bytes[11] = 1;
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = 0x66757472,
            InputChannels = 1,
            OutputChannels = 1
        };
        element.RawData.TypeSignature = element.Signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = bytes;
        return new IccTagData
        {
            Kind = IccTagDataKind.MultiProcessElements,
            MultiProcessElements = new IccMultiProcessElements
            {
                InputChannels = 1,
                OutputChannels = 1,
                Elements = new IccProcessingElement[] { element }
            }
        };
    }

    private static IccTagData MultiProcessThree()
    {
        IccTagData data = MultiProcess();
        data.MultiProcessElements!.InputChannels = 3;
        data.MultiProcessElements.OutputChannels = 3;
        data.MultiProcessElements.Elements[0].InputChannels = 3;
        data.MultiProcessElements.Elements[0].OutputChannels = 3;
        data.MultiProcessElements.Elements[0].RawData.Bytes[9] = 3;
        data.MultiProcessElements.Elements[0].RawData.Bytes[11] = 3;
        return data;
    }

    private static IccTagData SpectralViewingConditions()
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.SpectralViewingConditions,
            SpectralViewingConditions = new IccSpectralViewingConditions
            {
                Observer = IccMeasurementObserver.Cie1931TwoDegree,
                Illuminant = IccStandardIlluminant.D50
            }
        };
    }

    private static TagDecodeState RoundTrip(IccTagStructure structure)
    {
        return Decode(HierarchicalTagCodec.SerializeTagStructure(structure), 0, 0);
    }

    private static TagDecodeState RoundTripWithContext(IccTagStructure structure, uint dataSignature, uint pcsSignature)
    {
        return Decode(HierarchicalTagCodec.SerializeTagStructure(structure), dataSignature, pcsSignature);
    }

    private static TagDecodeState Decode(byte[] payload, uint dataSignature, uint pcsSignature)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = dataSignature != 0 || pcsSignature != 0;
        state.DataColorSpaceSignature = dataSignature;
        state.DataColorSpace = dataSignature == IccColorSpaceSignatures.Rgb ? IccColorSpaceKind.Rgb : dataSignature == IccColorSpaceSignatures.Cmyk ? IccColorSpaceKind.Cmyk : IccColorSpaceKind.Unknown;
        state.ProfileConnectionSpaceSignature = pcsSignature;
        state.ProfileConnectionSpace = pcsSignature == IccColorSpaceSignatures.Xyz ? IccPcsKind.Xyz : pcsSignature == IccColorSpaceSignatures.Lab ? IccPcsKind.Lab : IccPcsKind.None;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeDevicePcc(byte[] payload, uint dataSignature, IccProfileClass profileClass)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = IccTagSignatures.DevicePcc;
        state.ProfileClass = profileClass;
        state.DataColorSpaceSignature = dataSignature;
        state.DataColorSpace = HeaderCodec.DecodeDataColorSpace(dataSignature, IccVersionKind.Version5);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
