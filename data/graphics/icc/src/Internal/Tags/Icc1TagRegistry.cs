namespace icc;

internal static class Icc1ProfileClassMasks
{
    internal const int Input = 1;
    internal const int Display = 2;
    internal const int Output = 4;
    internal const int DeviceLink = 8;
    internal const int ColorSpace = 16;
    internal const int Abstract = 32;
    internal const int NamedColor = 64;
    internal const int All = 127;
    internal const int Device = Input | Display | Output;
    internal const int DeviceOrColorSpace = Device | ColorSpace;
    internal const int ForwardTransform = DeviceOrColorSpace | DeviceLink | Abstract;
    internal const int Matrix = Input | Display | ColorSpace;
    internal const int OutputOrDeviceLink = Output | DeviceLink;
}

internal static class Icc1TagRegistry
{
    internal static readonly uint[] NoTypes = new uint[0];
    internal static readonly uint[] LutForwardV2 = new uint[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16 };
    internal static readonly uint[] LutForwardV4 = new uint[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16, IccTypeSignatures.LutAToB };
    internal static readonly uint[] LutReverseV4 = new uint[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16, IccTypeSignatures.LutBToA };
    internal static readonly uint[] LutReverseV2 = new uint[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16 };
    internal static readonly uint[] MultiProcessOnly = new uint[] { IccTypeSignatures.MultiProcessElements };
    internal static readonly uint[] CurveV2 = new uint[] { IccTypeSignatures.Curve };
    internal static readonly uint[] CurveV4 = new uint[] { IccTypeSignatures.Curve, IccTypeSignatures.ParametricCurve };
    internal static readonly uint[] XyzOnly = new uint[] { IccTypeSignatures.Xyz };
    internal static readonly uint[] TextOnly = new uint[] { IccTypeSignatures.Text };
    internal static readonly uint[] TextDescriptionOnly = new uint[] { IccTypeSignatures.TextDescription };
    internal static readonly uint[] LocalizedTextOnly = new uint[] { IccTypeSignatures.MultiLocalizedUnicode };
    internal static readonly uint[] DataOnly = new uint[] { IccTypeSignatures.Data };
    internal static readonly uint[] SignatureOnly = new uint[] { IccTypeSignatures.Signature };

    internal static readonly Icc1TagDefinition[] Definitions = new Icc1TagDefinition[]
    {
        CreateTransformDefinition(IccTagSignatures.AToB0, LutForwardV2, LutForwardV4, Icc1TagRole.ForwardTransform, 0, Icc1ProfileClassMasks.ForwardTransform, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.AToB1, LutForwardV2, LutForwardV4, Icc1TagRole.ForwardTransform, 1, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.AToB2, LutForwardV2, LutForwardV4, Icc1TagRole.ForwardTransform, 2, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.BToA0, LutReverseV2, LutReverseV4, Icc1TagRole.ReverseTransform, 0, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToA1, LutReverseV2, LutReverseV4, Icc1TagRole.ReverseTransform, 1, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToA2, LutReverseV2, LutReverseV4, Icc1TagRole.ReverseTransform, 2, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.DToB0, NoTypes, MultiProcessOnly, Icc1TagRole.ForwardTransform, 0, Icc1ProfileClassMasks.ForwardTransform, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.DToB1, NoTypes, MultiProcessOnly, Icc1TagRole.ForwardTransform, 1, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.DToB2, NoTypes, MultiProcessOnly, Icc1TagRole.ForwardTransform, 2, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.DToB3, NoTypes, MultiProcessOnly, Icc1TagRole.ForwardTransform, 3, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.DataColorSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.BToD0, NoTypes, MultiProcessOnly, Icc1TagRole.ReverseTransform, 0, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToD1, NoTypes, MultiProcessOnly, Icc1TagRole.ReverseTransform, 1, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToD2, NoTypes, MultiProcessOnly, Icc1TagRole.ReverseTransform, 2, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.BToD3, NoTypes, MultiProcessOnly, Icc1TagRole.ReverseTransform, 3, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.DataColorSpace),
        CreateTransformDefinition(IccTagSignatures.Gamut, LutReverseV2, LutReverseV4, Icc1TagRole.Gamut, -1, Icc1ProfileClassMasks.DeviceOrColorSpace, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.One),
        CreateTransformDefinition(IccTagSignatures.Preview0, LutReverseV2, new uint[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16, IccTypeSignatures.LutAToB, IccTypeSignatures.LutBToA }, Icc1TagRole.Preview, 0, Icc1ProfileClassMasks.Output, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.Preview1, LutReverseV2, LutReverseV4, Icc1TagRole.Preview, 1, Icc1ProfileClassMasks.Output, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateTransformDefinition(IccTagSignatures.Preview2, LutReverseV2, LutReverseV4, Icc1TagRole.Preview, 2, Icc1ProfileClassMasks.Output, Icc1ChannelSource.ProfileConnectionSpace, Icc1ChannelSource.ProfileConnectionSpace),
        CreateRecordDefinition(IccTagSignatures.BlueMatrixColumn, XyzOnly, XyzOnly, Icc1TagRole.MatrixColumn, Icc1ProfileClassMasks.Matrix, 3, 1),
        CreateRecordDefinition(IccTagSignatures.GreenMatrixColumn, XyzOnly, XyzOnly, Icc1TagRole.MatrixColumn, Icc1ProfileClassMasks.Matrix, 3, 1),
        CreateRecordDefinition(IccTagSignatures.RedMatrixColumn, XyzOnly, XyzOnly, Icc1TagRole.MatrixColumn, Icc1ProfileClassMasks.Matrix, 3, 1),
        CreateRecordDefinition(IccTagSignatures.BlueTrc, CurveV2, CurveV4, Icc1TagRole.ToneCurve, Icc1ProfileClassMasks.Matrix, 3, 0),
        CreateRecordDefinition(IccTagSignatures.GreenTrc, CurveV2, CurveV4, Icc1TagRole.ToneCurve, Icc1ProfileClassMasks.Matrix, 3, 0),
        CreateRecordDefinition(IccTagSignatures.RedTrc, CurveV2, CurveV4, Icc1TagRole.ToneCurve, Icc1ProfileClassMasks.Matrix, 3, 0),
        CreateRecordDefinition(IccTagSignatures.GrayTrc, CurveV2, CurveV4, Icc1TagRole.ToneCurve, Icc1ProfileClassMasks.Device, 1, 0),
        CreateRecordDefinition(IccTagSignatures.CalibrationDateTime, new uint[] { IccTypeSignatures.DateTime }, new uint[] { IccTypeSignatures.DateTime }, Icc1TagRole.Calibration, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.CharacterizationTarget, TextOnly, TextOnly, Icc1TagRole.Characterization, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ChromaticAdaptation, new uint[] { IccTypeSignatures.S15Fixed16Array }, new uint[] { IccTypeSignatures.S15Fixed16Array }, Icc1TagRole.Calibration, Icc1ProfileClassMasks.All, 0, 9),
        CreateRecordDefinition(IccTagSignatures.Chromaticity, new uint[] { IccTypeSignatures.Chromaticity }, new uint[] { IccTypeSignatures.Chromaticity }, Icc1TagRole.Measurement, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Cicp, NoTypes, new uint[] { IccTypeSignatures.Cicp }, Icc1TagRole.Metadata, Icc1ProfileClassMasks.Input | Icc1ProfileClassMasks.Display, 3, 0),
        CreateRecordDefinition(IccTagSignatures.ColorantOrder, new uint[] { IccTypeSignatures.ColorantOrder }, new uint[] { IccTypeSignatures.ColorantOrder }, Icc1TagRole.ColorantOrder, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ColorantTable, new uint[] { IccTypeSignatures.ColorantTable }, new uint[] { IccTypeSignatures.ColorantTable }, Icc1TagRole.ColorantTable, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ColorantTableOut, NoTypes, new uint[] { IccTypeSignatures.ColorantTable }, Icc1TagRole.ColorantTable, Icc1ProfileClassMasks.DeviceLink, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ColorimetricIntentImageState, NoTypes, SignatureOnly, Icc1TagRole.ImageState, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Copyright, TextOnly, LocalizedTextOnly, Icc1TagRole.Description, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.DeviceManufacturerDescription, TextDescriptionOnly, LocalizedTextOnly, Icc1TagRole.Description, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.DeviceModelDescription, TextDescriptionOnly, LocalizedTextOnly, Icc1TagRole.Description, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Luminance, XyzOnly, XyzOnly, Icc1TagRole.Measurement, Icc1ProfileClassMasks.Display, 0, 1),
        CreateRecordDefinition(IccTagSignatures.Measurement, new uint[] { IccTypeSignatures.Measurement }, new uint[] { IccTypeSignatures.Measurement }, Icc1TagRole.Measurement, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Metadata, NoTypes, new uint[] { IccTypeSignatures.Dictionary }, Icc1TagRole.Metadata, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.MediaBlackPoint, XyzOnly, NoTypes, Icc1TagRole.Measurement, Icc1ProfileClassMasks.All, 0, 1),
        CreateRecordDefinition(IccTagSignatures.MediaWhitePoint, XyzOnly, XyzOnly, Icc1TagRole.Measurement, Icc1ProfileClassMasks.All, 0, 1),
        CreateRecordDefinition(IccTagSignatures.NamedColor, new uint[] { IccTypeSignatures.NamedColor }, NoTypes, Icc1TagRole.NamedColor, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.NamedColor2, new uint[] { IccTypeSignatures.NamedColor2 }, new uint[] { IccTypeSignatures.NamedColor2 }, Icc1TagRole.NamedColor, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.OutputResponse, new uint[] { IccTypeSignatures.ResponseCurveSet16 }, new uint[] { IccTypeSignatures.ResponseCurveSet16 }, Icc1TagRole.Calibration, Icc1ProfileClassMasks.Output, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PerceptualRenderingIntentGamut, NoTypes, SignatureOnly, Icc1TagRole.Metadata, Icc1ProfileClassMasks.DeviceOrColorSpace, 0, 0),
        CreateRecordDefinition(IccTagSignatures.SaturationRenderingIntentGamut, NoTypes, SignatureOnly, Icc1TagRole.Metadata, Icc1ProfileClassMasks.DeviceOrColorSpace, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ProfileDescription, TextDescriptionOnly, LocalizedTextOnly, Icc1TagRole.Description, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ProfileSequenceDescription, new uint[] { IccTypeSignatures.ProfileSequenceDescription }, new uint[] { IccTypeSignatures.ProfileSequenceDescription }, Icc1TagRole.ProfileSequence, Icc1ProfileClassMasks.DeviceLink, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ProfileSequenceIdentifier, NoTypes, new uint[] { IccTypeSignatures.ProfileSequenceIdentifier }, Icc1TagRole.ProfileSequence, Icc1ProfileClassMasks.DeviceLink, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Technology, SignatureOnly, SignatureOnly, Icc1TagRole.Technology, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ViewingConditionDescription, TextDescriptionOnly, LocalizedTextOnly, Icc1TagRole.Description, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ViewingConditions, new uint[] { IccTypeSignatures.ViewingConditions }, new uint[] { IccTypeSignatures.ViewingConditions }, Icc1TagRole.ViewingConditions, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.CrdInfo, new uint[] { IccTypeSignatures.CrdInfo }, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptCrd0, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptCrd1, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptCrd2, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptCrd3, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptCsa, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.PostScriptRenderingIntent, DataOnly, NoTypes, Icc1TagRole.PostScript, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.ScreeningDescription, TextDescriptionOnly, NoTypes, Icc1TagRole.Description, Icc1ProfileClassMasks.Output, 0, 0),
        CreateRecordDefinition(IccTagSignatures.Screening, new uint[] { IccTypeSignatures.Screening }, NoTypes, Icc1TagRole.Calibration, Icc1ProfileClassMasks.Output, 0, 0),
        CreateRecordDefinition(IccTagSignatures.UcrBg, new uint[] { IccTypeSignatures.UcrBg }, NoTypes, Icc1TagRole.Calibration, Icc1ProfileClassMasks.Output, 0, 0),
        CreateRecordDefinition(IccTagSignatures.DeviceSettings, new uint[] { IccTypeSignatures.DeviceSettings }, NoTypes, Icc1TagRole.Calibration, Icc1ProfileClassMasks.Device, 0, 0),
        CreateRecordDefinition(IccTagSignatures.EmbeddedIccMaxProfile, new uint[] { IccTypeSignatures.EmbeddedProfile }, new uint[] { IccTypeSignatures.EmbeddedProfile }, Icc1TagRole.EmbeddedProfile, Icc1ProfileClassMasks.All, 0, 0),
        CreateRecordDefinition(IccTagSignatures.AdaptiveGainCurve, NoTypes, new uint[] { IccTypeSignatures.AdaptiveGainCurve }, Icc1TagRole.Calibration, Icc1ProfileClassMasks.Input | Icc1ProfileClassMasks.Display, 3, 0)
    };

    internal static Icc1TagDefinition? FindDefinition(uint tagSignature)
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

    internal static uint[] GetPermittedTypes(Icc1TagDefinition definition, IccVersionKind version)
    {
        if (version == IccVersionKind.Version2)
        {
            return definition.Version2Types;
        }

        if (version == IccVersionKind.Version4)
        {
            return definition.Version4Types;
        }

        return NoTypes;
    }

    internal static bool ValidateTagContext(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!state.HasProfileContext || (state.ProfileVersion != IccVersionKind.Version2 && state.ProfileVersion != IccVersionKind.Version4))
        {
            return true;
        }

        Icc1TagDefinition? definition = FindDefinition(state.TagSignature);
        if (definition == null)
        {
            return true;
        }

        uint[] permittedTypes = GetPermittedTypes(definition, state.ProfileVersion);
        if (permittedTypes.Length == 0)
        {
            CompleteUnavailableTag(state, payload, typeSignature);
            return false;
        }

        if (!ContainsType(permittedTypes, typeSignature))
        {
            string permitted = FormatPermittedTypes(permittedTypes);
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"ICC {DescribeVersion(state.ProfileVersion)} tag '{FourCcCodec.Format(state.TagSignature)}' permits {permitted}, not '{FourCcCodec.Format(typeSignature)}'.");
            return false;
        }

        if (!IsProfileClassAllowed(definition, state.ProfileClass))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature,
                $"ICC tag '{FourCcCodec.Format(state.TagSignature)}' is not permitted for the {state.ProfileClass} profile class.");
            return false;
        }

        string channelError = GetChannelError(definition, state, payload, typeSignature);
        if (channelError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, channelError);
            return false;
        }

        return true;
    }

    internal static bool IsProfileClassAllowed(Icc1TagDefinition definition, IccProfileClass profileClass)
    {
        int mask = GetProfileClassMask(profileClass);
        return mask != 0 && (definition.ProfileClassMask & mask) != 0;
    }

    internal static int ResolveChannelCount(Icc1ChannelSource source, TagDecodeState state)
    {
        if (source == Icc1ChannelSource.DataColorSpace)
        {
            return ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        }

        if (source == Icc1ChannelSource.ProfileConnectionSpace)
        {
            return ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        }

        if (source == Icc1ChannelSource.One)
        {
            return 1;
        }

        if (source == Icc1ChannelSource.DataOrDeviceLinkOutput)
        {
            if (state.ProfileClass == IccProfileClass.DeviceLink)
            {
                return ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
            }

            return ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        }

        return 0;
    }

    internal static bool ContainsType(uint[] values, uint value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string GetChannelError(Icc1TagDefinition definition, TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        int dataChannels = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        if (definition.RequiredDataChannels != 0 && dataChannels != 0 && dataChannels != definition.RequiredDataChannels)
        {
            return $"ICC tag '{FourCcCodec.Format(definition.TagSignature)}' requires a {definition.RequiredDataChannels}-channel profile data colour space, but the header declares {dataChannels} channels.";
        }

        if (definition.ExpectedValueCount != 0)
        {
            int valueWidth = typeSignature == IccTypeSignatures.Xyz ? 12 : 4;
            int expectedSize = 8 + (definition.ExpectedValueCount * valueWidth);
            if (payload.Length != expectedSize)
            {
                return $"ICC tag '{FourCcCodec.Format(definition.TagSignature)}' requires exactly {definition.ExpectedValueCount} encoded values and {expectedSize} payload bytes.";
            }
        }

        if (definition.InputChannels == Icc1ChannelSource.None && definition.OutputChannels == Icc1ChannelSource.None)
        {
            return string.Empty;
        }

        int encodedInput = 0;
        int encodedOutput = 0;
        if (typeSignature == IccTypeSignatures.Lut8 || typeSignature == IccTypeSignatures.Lut16 || typeSignature == IccTypeSignatures.LutAToB || typeSignature == IccTypeSignatures.LutBToA)
        {
            if (payload.Length < 10)
            {
                return string.Empty;
            }

            encodedInput = payload.Span[8];
            encodedOutput = payload.Span[9];
        }
        else if (typeSignature == IccTypeSignatures.MultiProcessElements)
        {
            if (payload.Length < 12)
            {
                return string.Empty;
            }

            BigEndianReader reader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
            encodedInput = reader.ReadUInt16();
            encodedOutput = reader.ReadUInt16();
        }
        else
        {
            return string.Empty;
        }

        int expectedInput = ResolveChannelCount(definition.InputChannels, state);
        int expectedOutput = ResolveChannelCount(definition.OutputChannels, state);
        if (expectedInput != 0 && encodedInput != expectedInput)
        {
            return $"ICC tag '{FourCcCodec.Format(definition.TagSignature)}' declares {encodedInput} input channels, but its {definition.Role} semantics require {expectedInput}.";
        }

        if (expectedOutput != 0 && encodedOutput != expectedOutput)
        {
            return $"ICC tag '{FourCcCodec.Format(definition.TagSignature)}' declares {encodedOutput} output channels, but its {definition.Role} semantics require {expectedOutput}.";
        }

        return string.Empty;
    }

    internal static string FormatPermittedTypes(uint[] types)
    {
        string result = string.Empty;
        int index = 0;
        while (index < types.Length)
        {
            if (index > 0)
            {
                result = $"{result}, ";
            }

            result = $"{result}'{FourCcCodec.Format(types[index])}'";
            index = index + 1;
        }

        return result;
    }

    internal static int GetProfileClassMask(IccProfileClass profileClass)
    {
        switch (profileClass)
        {
            case IccProfileClass.Input:
                return Icc1ProfileClassMasks.Input;
            case IccProfileClass.Display:
                return Icc1ProfileClassMasks.Display;
            case IccProfileClass.Output:
                return Icc1ProfileClassMasks.Output;
            case IccProfileClass.DeviceLink:
                return Icc1ProfileClassMasks.DeviceLink;
            case IccProfileClass.ColorSpace:
                return Icc1ProfileClassMasks.ColorSpace;
            case IccProfileClass.Abstract:
                return Icc1ProfileClassMasks.Abstract;
            case IccProfileClass.NamedColor:
                return Icc1ProfileClassMasks.NamedColor;
            default:
                return 0;
        }
    }

    internal static Icc1TagDefinition CreateTransformDefinition(uint tagSignature, uint[] version2Types, uint[] version4Types, Icc1TagRole role, int renderingIntent, int profileClassMask, Icc1ChannelSource inputChannels, Icc1ChannelSource outputChannels)
    {
        return new Icc1TagDefinition(tagSignature, version2Types, version4Types, role, renderingIntent, profileClassMask, inputChannels, outputChannels, Icc1ChannelSource.None, 0, 0);
    }

    internal static Icc1TagDefinition CreateRecordDefinition(uint tagSignature, uint[] version2Types, uint[] version4Types, Icc1TagRole role, int profileClassMask, int requiredDataChannels, int expectedValueCount)
    {
        Icc1ChannelSource associatedChannels = GetAssociatedChannelSource(tagSignature);
        return new Icc1TagDefinition(tagSignature, version2Types, version4Types, role, -1, profileClassMask, Icc1ChannelSource.None, Icc1ChannelSource.None, associatedChannels, requiredDataChannels, expectedValueCount);
    }

    internal static Icc1ChannelSource GetAssociatedChannelSource(uint tagSignature)
    {
        switch (tagSignature)
        {
            case IccTagSignatures.BlueMatrixColumn:
            case IccTagSignatures.GreenMatrixColumn:
            case IccTagSignatures.RedMatrixColumn:
            case IccTagSignatures.BlueTrc:
            case IccTagSignatures.GreenTrc:
            case IccTagSignatures.RedTrc:
            case IccTagSignatures.GrayTrc:
            case IccTagSignatures.Chromaticity:
            case IccTagSignatures.Cicp:
            case IccTagSignatures.ColorantTable:
            case IccTagSignatures.NamedColor:
            case IccTagSignatures.NamedColor2:
            case IccTagSignatures.OutputResponse:
            case IccTagSignatures.AdaptiveGainCurve:
                return Icc1ChannelSource.DataColorSpace;
            case IccTagSignatures.ColorantOrder:
                return Icc1ChannelSource.DataOrDeviceLinkOutput;
            case IccTagSignatures.ColorantTableOut:
                return Icc1ChannelSource.ProfileConnectionSpace;
            default:
                return Icc1ChannelSource.None;
        }
    }

    internal static void CompleteUnavailableTag(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        IccTagData data = new IccTagData();
        UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.UnsupportedFeature);
        result.Message = $"ICC tag '{FourCcCodec.Format(state.TagSignature)}' is not defined for {DescribeVersion(state.ProfileVersion)} profiles; the payload was retained losslessly as raw data.";
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static string DescribeVersion(IccVersionKind version)
    {
        if (version == IccVersionKind.Version2)
        {
            return "version 2";
        }

        if (version == IccVersionKind.Version4)
        {
            return "version 4";
        }

        return "non-ICC.1";
    }
}
