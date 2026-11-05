using System;

namespace avif;

internal class Av1SequenceHeaderParseResult
{
    internal AvifResultCode Code;
    internal ParsedAv1SequenceHeader? Header;

    internal Av1SequenceHeaderParseResult(
        AvifResultCode code,
        ParsedAv1SequenceHeader? header)
    {
        Code = code;
        Header = header;
    }
}

internal class Av1OperatingPointParseResult
{
    internal AvifResultCode Code;
    internal Av1TimingInfo? TimingInfo;
    internal Av1DecoderModelInfo? DecoderModelInfo;
    internal bool InitialDisplayDelayPresent;
    internal Av1OperatingPoint[] OperatingPoints;

    internal Av1OperatingPointParseResult(
        AvifResultCode code,
        Av1TimingInfo? timingInfo,
        Av1DecoderModelInfo? decoderModelInfo,
        bool initialDisplayDelayPresent,
        Av1OperatingPoint[] operatingPoints)
    {
        Code = code;
        TimingInfo = timingInfo;
        DecoderModelInfo = decoderModelInfo;
        InitialDisplayDelayPresent = initialDisplayDelayPresent;
        OperatingPoints = operatingPoints;
    }
}

internal class Av1Leb128ParseResult
{
    internal AvifResultCode Code;
    internal ulong Value;

    internal Av1Leb128ParseResult(AvifResultCode code, ulong value)
    {
        Code = code;
        Value = value;
    }
}

internal class Av1TimingInfo
{
    internal uint NumUnitsInDisplayTick;
    internal uint TimeScale;
    internal bool EqualPictureInterval;
    internal uint NumTicksPerPictureMinusOne;

    internal Av1TimingInfo(
        uint numUnitsInDisplayTick,
        uint timeScale,
        bool equalPictureInterval,
        uint numTicksPerPictureMinusOne)
    {
        NumUnitsInDisplayTick = numUnitsInDisplayTick;
        TimeScale = timeScale;
        EqualPictureInterval = equalPictureInterval;
        NumTicksPerPictureMinusOne = numTicksPerPictureMinusOne;
    }
}

internal class Av1DecoderModelInfo
{
    internal int BufferDelayLength;
    internal uint NumUnitsInDecodingTick;
    internal int BufferRemovalTimeLength;
    internal int FramePresentationTimeLength;

    internal Av1DecoderModelInfo(
        int bufferDelayLength,
        uint numUnitsInDecodingTick,
        int bufferRemovalTimeLength,
        int framePresentationTimeLength)
    {
        BufferDelayLength = bufferDelayLength;
        NumUnitsInDecodingTick = numUnitsInDecodingTick;
        BufferRemovalTimeLength = bufferRemovalTimeLength;
        FramePresentationTimeLength = framePresentationTimeLength;
    }
}

internal class Av1OperatingPoint
{
    internal int Idc;
    internal int Level;
    internal int Tier;
    internal bool DecoderModelPresent;
    internal ulong DecoderBufferDelay;
    internal ulong EncoderBufferDelay;
    internal bool LowDelayMode;
    internal bool InitialDisplayDelayPresent;
    internal int InitialDisplayDelayMinusOne;

    internal Av1OperatingPoint(int idc, int level, int tier)
    {
        Idc = idc;
        Level = level;
        Tier = tier;
        DecoderModelPresent = false;
        DecoderBufferDelay = 0;
        EncoderBufferDelay = 0;
        LowDelayMode = false;
        InitialDisplayDelayPresent = false;
        InitialDisplayDelayMinusOne = 0;
    }
}

internal class Av1SequenceFeatures
{
    internal int FrameWidthBits;
    internal int FrameHeightBits;
    internal bool FrameIdNumbersPresent;
    internal int DeltaFrameIdLength;
    internal int AdditionalFrameIdLength;
    internal bool Use128x128Superblock;
    internal bool EnableFilterIntra;
    internal bool EnableIntraEdgeFilter;
    internal bool EnableInterIntraCompound;
    internal bool EnableMaskedCompound;
    internal bool EnableWarpedMotion;
    internal bool EnableDualFilter;
    internal bool EnableOrderHint;
    internal bool EnableJointCompound;
    internal bool EnableReferenceFrameMotionVectors;
    internal bool ChooseScreenContentTools;
    internal int ScreenContentToolsMode;
    internal bool ChooseIntegerMotionVectors;
    internal int IntegerMotionVectorMode;
    internal int OrderHintBits;
    internal bool EnableSuperres;
    internal bool EnableCdef;
    internal bool EnableRestoration;
}

internal class ParsedAv1SequenceHeader
{
    int sequenceProfile;
    int sequenceLevelIndex;
    int sequenceTier;
    int operatingPointCount;
    bool stillPicture;
    bool reducedStillPictureHeader;
    uint maximumWidth;
    uint maximumHeight;
    bool highBitDepth;
    bool twelveBit;
    bool monochrome;
    bool chromaSubsamplingX;
    bool chromaSubsamplingY;
    int chromaSamplePosition;
    int colorPrimaries;
    int transferCharacteristics;
    int matrixCoefficients;
    bool fullRange;
    bool colorDescriptionPresent;
    bool separateUvDeltaQuantizer;
    bool filmGrainParametersPresent;
    Av1TimingInfo? timingInfo;
    Av1DecoderModelInfo? decoderModelInfo;
    bool initialDisplayDelayPresent;
    Av1OperatingPoint[] operatingPoints;
    Av1SequenceFeatures features;
    byte[] rawPayload;

    internal ParsedAv1SequenceHeader(
        int sequenceProfile,
        bool stillPicture,
        bool reducedStillPictureHeader,
        Av1TimingInfo? timingInfo,
        Av1DecoderModelInfo? decoderModelInfo,
        bool initialDisplayDelayPresent,
        Av1OperatingPoint[] operatingPoints,
        uint maximumWidth,
        uint maximumHeight,
        Av1SequenceFeatures features,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool chromaSubsamplingX,
        bool chromaSubsamplingY,
        int chromaSamplePosition,
        int colorPrimaries,
        int transferCharacteristics,
        int matrixCoefficients,
        bool fullRange,
        bool colorDescriptionPresent,
        bool separateUvDeltaQuantizer,
        bool filmGrainParametersPresent,
        byte[] rawPayload)
    {
        this.sequenceProfile = sequenceProfile;
        sequenceLevelIndex = operatingPoints[0].Level;
        sequenceTier = operatingPoints[0].Tier;
        operatingPointCount = operatingPoints.Length;
        this.stillPicture = stillPicture;
        this.reducedStillPictureHeader = reducedStillPictureHeader;
        this.timingInfo = timingInfo;
        this.decoderModelInfo = decoderModelInfo;
        this.initialDisplayDelayPresent = initialDisplayDelayPresent;
        this.operatingPoints = (Av1OperatingPoint[])operatingPoints.Clone();
        this.maximumWidth = maximumWidth;
        this.maximumHeight = maximumHeight;
        this.features = features;
        this.highBitDepth = highBitDepth;
        this.twelveBit = twelveBit;
        this.monochrome = monochrome;
        this.chromaSubsamplingX = chromaSubsamplingX;
        this.chromaSubsamplingY = chromaSubsamplingY;
        this.chromaSamplePosition = chromaSamplePosition;
        this.colorPrimaries = colorPrimaries;
        this.transferCharacteristics = transferCharacteristics;
        this.matrixCoefficients = matrixCoefficients;
        this.fullRange = fullRange;
        this.colorDescriptionPresent = colorDescriptionPresent;
        this.separateUvDeltaQuantizer = separateUvDeltaQuantizer;
        this.filmGrainParametersPresent = filmGrainParametersPresent;
        this.rawPayload = (byte[])rawPayload.Clone();
    }

    internal int GetSequenceProfile()
    {
        return sequenceProfile;
    }

    internal int GetSequenceLevelIndex()
    {
        return sequenceLevelIndex;
    }

    internal int GetSequenceTier()
    {
        return sequenceTier;
    }

    internal int GetOperatingPointCount()
    {
        return operatingPointCount;
    }

    internal Av1OperatingPoint? GetOperatingPoint(int index)
    {
        if (index < 0 || index >= operatingPoints.Length)
        {
            return null;
        }

        return operatingPoints[index];
    }

    internal bool HasTimingInfo()
    {
        return timingInfo != null;
    }

    internal Av1TimingInfo? GetTimingInfo()
    {
        return timingInfo;
    }

    internal bool HasDecoderModelInfo()
    {
        return decoderModelInfo != null;
    }

    internal Av1DecoderModelInfo? GetDecoderModelInfo()
    {
        return decoderModelInfo;
    }

    internal bool HasInitialDisplayDelay()
    {
        return initialDisplayDelayPresent;
    }

    internal bool IsStillPicture()
    {
        return stillPicture;
    }

    internal bool HasReducedStillPictureHeader()
    {
        return reducedStillPictureHeader;
    }

    internal uint GetMaximumWidth()
    {
        return maximumWidth;
    }

    internal uint GetMaximumHeight()
    {
        return maximumHeight;
    }

    internal bool IsHighBitDepth()
    {
        return highBitDepth;
    }

    internal bool IsTwelveBit()
    {
        return twelveBit;
    }

    internal int GetBitDepth()
    {
        if (twelveBit)
        {
            return 12;
        }

        return highBitDepth ? 10 : 8;
    }

    internal bool IsMonochrome()
    {
        return monochrome;
    }

    internal int GetPlaneCount()
    {
        return monochrome ? 1 : 3;
    }

    internal bool HasChromaSubsamplingX()
    {
        return chromaSubsamplingX;
    }

    internal bool HasChromaSubsamplingY()
    {
        return chromaSubsamplingY;
    }

    internal int GetChromaSamplePosition()
    {
        return chromaSamplePosition;
    }

    internal int GetColorPrimaries()
    {
        return colorPrimaries;
    }

    internal int GetTransferCharacteristics()
    {
        return transferCharacteristics;
    }

    internal int GetMatrixCoefficients()
    {
        return matrixCoefficients;
    }

    internal bool IsFullRange()
    {
        return fullRange;
    }

    internal bool HasColorDescription()
    {
        return colorDescriptionPresent;
    }

    internal bool HasSeparateUvDeltaQuantizer()
    {
        return separateUvDeltaQuantizer;
    }

    internal bool HasFilmGrainParameters()
    {
        return filmGrainParametersPresent;
    }

    internal int GetFrameWidthBits()
    {
        return features.FrameWidthBits;
    }

    internal int GetFrameHeightBits()
    {
        return features.FrameHeightBits;
    }

    internal bool HasFrameIdNumbers()
    {
        return features.FrameIdNumbersPresent;
    }

    internal int GetDeltaFrameIdLength()
    {
        return features.DeltaFrameIdLength;
    }

    internal int GetAdditionalFrameIdLength()
    {
        return features.AdditionalFrameIdLength;
    }

    internal int GetFrameIdLength()
    {
        return features.DeltaFrameIdLength + features.AdditionalFrameIdLength;
    }

    internal bool Uses128x128Superblocks()
    {
        return features.Use128x128Superblock;
    }

    internal bool IsFilterIntraEnabled()
    {
        return features.EnableFilterIntra;
    }

    internal bool IsIntraEdgeFilterEnabled()
    {
        return features.EnableIntraEdgeFilter;
    }

    internal bool IsInterIntraCompoundEnabled()
    {
        return features.EnableInterIntraCompound;
    }

    internal bool IsMaskedCompoundEnabled()
    {
        return features.EnableMaskedCompound;
    }

    internal bool IsWarpedMotionEnabled()
    {
        return features.EnableWarpedMotion;
    }

    internal bool IsDualFilterEnabled()
    {
        return features.EnableDualFilter;
    }

    internal bool IsOrderHintEnabled()
    {
        return features.EnableOrderHint;
    }

    internal bool IsJointCompoundEnabled()
    {
        return features.EnableJointCompound;
    }

    internal bool AreReferenceFrameMotionVectorsEnabled()
    {
        return features.EnableReferenceFrameMotionVectors;
    }

    internal int GetScreenContentToolsMode()
    {
        return features.ScreenContentToolsMode;
    }

    internal bool ChoosesScreenContentTools()
    {
        return features.ChooseScreenContentTools;
    }

    internal int GetIntegerMotionVectorMode()
    {
        return features.IntegerMotionVectorMode;
    }

    internal bool ChoosesIntegerMotionVectors()
    {
        return features.ChooseIntegerMotionVectors;
    }

    internal int GetOrderHintBits()
    {
        return features.OrderHintBits;
    }

    internal bool IsSuperresEnabled()
    {
        return features.EnableSuperres;
    }

    internal bool IsCdefEnabled()
    {
        return features.EnableCdef;
    }

    internal bool IsRestorationEnabled()
    {
        return features.EnableRestoration;
    }

    internal byte[] GetRawPayload()
    {
        return (byte[])rawPayload.Clone();
    }

    internal int GetRawPayloadLength()
    {
        return rawPayload.Length;
    }

    internal bool PayloadEquals(ReadOnlySpan<byte> candidate)
    {
        if (candidate.Length != rawPayload.Length)
        {
            return false;
        }

        int index = 0;
        while (index < rawPayload.Length)
        {
            if (candidate[index] != rawPayload[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}

internal static class Av1SequenceHeaderParser
{
    internal static Av1SequenceHeaderParseResult ParseObuStream(
        ReadOnlySpan<byte> source,
        bool configurationStream,
        bool requireSequenceHeader)
    {
        ByteReader reader = new ByteReader(source.Length);
        ParsedAv1SequenceHeader? sequenceHeader = null;
        int obuIndex = 0;
        while (reader.Remaining() > 0)
        {
            byte header = reader.ReadUInt8(source);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateResult(NormalizeCode(reader.Code), null);
            }

            int forbidden = header >> 7;
            int obuType = header >> 3 & 15;
            bool extensionFlag = (header & 4) != 0;
            bool hasSizeField = (header & 2) != 0;
            int reserved = header & 1;
            if (forbidden != 0 || reserved != 0 || obuType == 0)
            {
                return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            if (extensionFlag)
            {
                byte extension = reader.ReadUInt8(source);
                if (reader.Code != AvifResultCode.Success)
                {
                    return CreateResult(NormalizeCode(reader.Code), null);
                }

                if ((extension & 7) != 0)
                {
                    return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
                }
            }

            if (obuType == 1 && extensionFlag)
            {
                return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            if (configurationStream && !hasSizeField)
            {
                return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            int payloadSize;
            if (hasSizeField)
            {
                Av1Leb128ParseResult size = ReadLeb128(source, reader);
                if (size.Code != AvifResultCode.Success)
                {
                    return CreateResult(size.Code, null);
                }

                if (size.Value > int.MaxValue || size.Value > (ulong)reader.Remaining())
                {
                    return CreateResult(AvifResultCode.TruncatedData, null);
                }

                payloadSize = (int)size.Value;
            }
            else
            {
                payloadSize = reader.Remaining();
            }

            int payloadOffset = reader.Position;
            if (!configurationStream
                && sequenceHeader == null
                && (obuType == 3 || obuType == 6))
            {
                return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            if (obuType == 1)
            {
                if (sequenceHeader != null || (configurationStream && obuIndex != 0))
                {
                    return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
                }

                ReadOnlySpan<byte> payload = source.Slice(payloadOffset, payloadSize);
                Av1SequenceHeaderParseResult parsed = ParsePayload(payload);
                if (parsed.Code != AvifResultCode.Success)
                {
                    return parsed;
                }

                sequenceHeader = parsed.Header;
            }

            reader.Position += payloadSize;

            obuIndex++;
        }

        if (requireSequenceHeader && sequenceHeader == null)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        return CreateResult(AvifResultCode.Success, sequenceHeader);
    }

    internal static Av1SequenceHeaderParseResult ParsePayload(ReadOnlySpan<byte> payload)
    {
        BitReader reader = BitReader.Create(payload.Length);
        int sequenceProfile = (int)reader.ReadBits(payload, 3);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(reader.Code), null);
        }

        if (sequenceProfile > 2)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        bool stillPicture = reader.ReadBit(payload) != 0;
        bool reducedStillPictureHeader = reader.ReadBit(payload) != 0;
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(reader.Code), null);
        }

        if (reducedStillPictureHeader && !stillPicture)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        Av1TimingInfo? timingInfo = null;
        Av1DecoderModelInfo? decoderModelInfo = null;
        bool initialDisplayDelayPresent = false;
        Av1OperatingPoint[] operatingPoints;
        if (reducedStillPictureHeader)
        {
            int level = (int)reader.ReadBits(payload, 5);
            if (!IsValidLevel(level))
            {
                return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
            }

            operatingPoints = new Av1OperatingPoint[]
            {
                new Av1OperatingPoint(0, level, 0)
            };
        }
        else
        {
            Av1OperatingPointParseResult parsedPoints = ParseOperatingPoints(payload, reader);
            if (parsedPoints.Code != AvifResultCode.Success)
            {
                return CreateResult(parsedPoints.Code, null);
            }

            timingInfo = parsedPoints.TimingInfo;
            decoderModelInfo = parsedPoints.DecoderModelInfo;
            initialDisplayDelayPresent = parsedPoints.InitialDisplayDelayPresent;
            operatingPoints = parsedPoints.OperatingPoints;
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(reader.Code), null);
        }

        int widthBits = (int)reader.ReadBits(payload, 4) + 1;
        int heightBits = (int)reader.ReadBits(payload, 4) + 1;
        uint maximumWidth = (uint)reader.ReadBits(payload, widthBits) + 1;
        uint maximumHeight = (uint)reader.ReadBits(payload, heightBits) + 1;

        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = widthBits;
        features.FrameHeightBits = heightBits;
        if (reducedStillPictureHeader)
        {
            features.FrameIdNumbersPresent = false;
        }
        else
        {
            features.FrameIdNumbersPresent = reader.ReadBit(payload) != 0;
        }

        if (features.FrameIdNumbersPresent)
        {
            features.DeltaFrameIdLength = (int)reader.ReadBits(payload, 4) + 2;
            features.AdditionalFrameIdLength = (int)reader.ReadBits(payload, 3) + 1;
        }

        features.Use128x128Superblock = reader.ReadBit(payload) != 0;
        features.EnableFilterIntra = reader.ReadBit(payload) != 0;
        features.EnableIntraEdgeFilter = reader.ReadBit(payload) != 0;
        if (reducedStillPictureHeader)
        {
            features.EnableInterIntraCompound = false;
            features.EnableMaskedCompound = false;
            features.EnableWarpedMotion = false;
            features.EnableDualFilter = false;
            features.EnableOrderHint = false;
            features.EnableJointCompound = false;
            features.EnableReferenceFrameMotionVectors = false;
            features.ChooseScreenContentTools = false;
            features.ScreenContentToolsMode = Av1SyntaxConstants.SelectScreenContentTools;
            features.ChooseIntegerMotionVectors = false;
            features.IntegerMotionVectorMode = Av1SyntaxConstants.SelectIntegerMotionVector;
            features.OrderHintBits = 0;
        }
        else
        {
            features.EnableInterIntraCompound = reader.ReadBit(payload) != 0;
            features.EnableMaskedCompound = reader.ReadBit(payload) != 0;
            features.EnableWarpedMotion = reader.ReadBit(payload) != 0;
            features.EnableDualFilter = reader.ReadBit(payload) != 0;
            features.EnableOrderHint = reader.ReadBit(payload) != 0;
            if (features.EnableOrderHint)
            {
                features.EnableJointCompound = reader.ReadBit(payload) != 0;
                features.EnableReferenceFrameMotionVectors = reader.ReadBit(payload) != 0;
            }

            features.ChooseScreenContentTools = reader.ReadBit(payload) != 0;
            if (features.ChooseScreenContentTools)
            {
                features.ScreenContentToolsMode =
                    Av1SyntaxConstants.SelectScreenContentTools;
            }
            else
            {
                features.ScreenContentToolsMode = reader.ReadBit(payload);
            }

            if (features.ScreenContentToolsMode > 0)
            {
                features.ChooseIntegerMotionVectors = reader.ReadBit(payload) != 0;
                if (features.ChooseIntegerMotionVectors)
                {
                    features.IntegerMotionVectorMode =
                        Av1SyntaxConstants.SelectIntegerMotionVector;
                }
                else
                {
                    features.IntegerMotionVectorMode = reader.ReadBit(payload);
                }
            }
            else
            {
                features.ChooseIntegerMotionVectors = false;
                features.IntegerMotionVectorMode =
                    Av1SyntaxConstants.SelectIntegerMotionVector;
            }

            if (features.EnableOrderHint)
            {
                features.OrderHintBits = (int)reader.ReadBits(payload, 3) + 1;
            }
        }

        features.EnableSuperres = reader.ReadBit(payload) != 0;
        features.EnableCdef = reader.ReadBit(payload) != 0;
        features.EnableRestoration = reader.ReadBit(payload) != 0;
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(reader.Code), null);
        }

        Av1ColorConfigurationParseResult color = ParseColorConfiguration(
            payload,
            reader,
            sequenceProfile);
        if (color.Code != AvifResultCode.Success)
        {
            return CreateResult(color.Code, null);
        }

        bool filmGrainParametersPresent = reader.ReadBit(payload) != 0;
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(reader.Code), null);
        }

        AvifResultCode trailingCode = ReadTrailingBits(payload, reader);
        if (trailingCode != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(trailingCode), null);
        }

        ParsedAv1SequenceHeader header = new ParsedAv1SequenceHeader(
            sequenceProfile,
            stillPicture,
            reducedStillPictureHeader,
            timingInfo,
            decoderModelInfo,
            initialDisplayDelayPresent,
            operatingPoints,
            maximumWidth,
            maximumHeight,
            features,
            color.HighBitDepth,
            color.TwelveBit,
            color.Monochrome,
            color.ChromaSubsamplingX,
            color.ChromaSubsamplingY,
            color.ChromaSamplePosition,
            color.ColorPrimaries,
            color.TransferCharacteristics,
            color.MatrixCoefficients,
            color.FullRange,
            color.ColorDescriptionPresent,
            color.SeparateUvDeltaQuantizer,
            filmGrainParametersPresent,
            payload.ToArray());
        return CreateResult(AvifResultCode.Success, header);
    }

    internal static Av1OperatingPointParseResult ParseOperatingPoints(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        bool timingInfoPresent = reader.ReadBit(payload) != 0;
        Av1TimingInfo? timingInfo = null;
        Av1DecoderModelInfo? decoderModelInfo = null;
        if (timingInfoPresent)
        {
            uint numUnitsInDisplayTick = (uint)reader.ReadBits(payload, 32);
            uint timeScale = (uint)reader.ReadBits(payload, 32);
            bool equalPictureInterval = reader.ReadBit(payload) != 0;
            uint numTicksPerPictureMinusOne = 0;
            if (equalPictureInterval)
            {
                numTicksPerPictureMinusOne = reader.ReadUvlc(payload);
                if (numTicksPerPictureMinusOne == uint.MaxValue)
                {
                    return new Av1OperatingPointParseResult(
                        AvifResultCode.InvalidAv1Bitstream,
                        null,
                        null,
                        false,
                        System.Array.Empty<Av1OperatingPoint>());
                }
            }

            if (reader.Code != AvifResultCode.Success)
            {
                return new Av1OperatingPointParseResult(
                    NormalizeCode(reader.Code),
                    null,
                    null,
                    false,
                    System.Array.Empty<Av1OperatingPoint>());
            }

            if (numUnitsInDisplayTick == 0 || timeScale == 0)
            {
                return new Av1OperatingPointParseResult(
                    AvifResultCode.InvalidAv1Bitstream,
                    null,
                    null,
                    false,
                    System.Array.Empty<Av1OperatingPoint>());
            }

            timingInfo = new Av1TimingInfo(
                numUnitsInDisplayTick,
                timeScale,
                equalPictureInterval,
                numTicksPerPictureMinusOne);

            bool decoderModelInfoPresent = reader.ReadBit(payload) != 0;
            if (decoderModelInfoPresent)
            {
                int bufferDelayLength = (int)reader.ReadBits(payload, 5) + 1;
                uint numUnitsInDecodingTick = (uint)reader.ReadBits(payload, 32);
                int bufferRemovalTimeLength = (int)reader.ReadBits(payload, 5) + 1;
                int framePresentationTimeLength =
                    (int)reader.ReadBits(payload, 5) + 1;
                if (reader.Code != AvifResultCode.Success)
                {
                    return new Av1OperatingPointParseResult(
                        NormalizeCode(reader.Code),
                        null,
                        null,
                        false,
                        System.Array.Empty<Av1OperatingPoint>());
                }

                if (numUnitsInDecodingTick == 0)
                {
                    return new Av1OperatingPointParseResult(
                        AvifResultCode.InvalidAv1Bitstream,
                        null,
                        null,
                        false,
                        System.Array.Empty<Av1OperatingPoint>());
                }

                decoderModelInfo = new Av1DecoderModelInfo(
                    bufferDelayLength,
                    numUnitsInDecodingTick,
                    bufferRemovalTimeLength,
                    framePresentationTimeLength);
            }
        }

        bool initialDisplayDelayPresent = reader.ReadBit(payload) != 0;
        int operatingPointCount = (int)reader.ReadBits(payload, 5) + 1;
        Av1OperatingPoint[] operatingPoints = new Av1OperatingPoint[operatingPointCount];
        int index = 0;
        while (index < operatingPointCount)
        {
            int idc = (int)reader.ReadBits(payload, 12);
            int level = (int)reader.ReadBits(payload, 5);
            if (!IsValidLevel(level))
            {
                return new Av1OperatingPointParseResult(
                    AvifResultCode.InvalidAv1Bitstream,
                    null,
                    null,
                    false,
                    System.Array.Empty<Av1OperatingPoint>());
            }

            int tier = 0;
            if (level > 7)
            {
                tier = reader.ReadBit(payload);
            }

            Av1OperatingPoint operatingPoint = new Av1OperatingPoint(idc, level, tier);
            if (decoderModelInfo != null)
            {
                operatingPoint.DecoderModelPresent = reader.ReadBit(payload) != 0;
                if (operatingPoint.DecoderModelPresent)
                {
                    operatingPoint.DecoderBufferDelay = reader.ReadBits(
                        payload,
                        decoderModelInfo.BufferDelayLength);
                    operatingPoint.EncoderBufferDelay = reader.ReadBits(
                        payload,
                        decoderModelInfo.BufferDelayLength);
                    operatingPoint.LowDelayMode = reader.ReadBit(payload) != 0;
                }
            }

            if (initialDisplayDelayPresent)
            {
                operatingPoint.InitialDisplayDelayPresent =
                    reader.ReadBit(payload) != 0;
                if (operatingPoint.InitialDisplayDelayPresent)
                {
                    operatingPoint.InitialDisplayDelayMinusOne =
                        (int)reader.ReadBits(payload, 4);
                }
            }

            operatingPoints[index] = operatingPoint;
            index++;
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return new Av1OperatingPointParseResult(
                NormalizeCode(reader.Code),
                null,
                null,
                false,
                System.Array.Empty<Av1OperatingPoint>());
        }

        return new Av1OperatingPointParseResult(
            AvifResultCode.Success,
            timingInfo,
            decoderModelInfo,
            initialDisplayDelayPresent,
            operatingPoints);
    }

    internal static Av1ColorConfigurationParseResult ParseColorConfiguration(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        int sequenceProfile)
    {
        bool highBitDepth = reader.ReadBit(payload) != 0;
        bool twelveBit = false;
        int bitDepth;
        if (sequenceProfile == 2 && highBitDepth)
        {
            twelveBit = reader.ReadBit(payload) != 0;
            bitDepth = twelveBit ? 12 : 10;
        }
        else
        {
            bitDepth = highBitDepth ? 10 : 8;
        }

        bool monochrome = false;
        if (sequenceProfile != 1)
        {
            monochrome = reader.ReadBit(payload) != 0;
        }

        bool colorDescriptionPresent = reader.ReadBit(payload) != 0;
        int colorPrimaries = 2;
        int transferCharacteristics = 2;
        int matrixCoefficients = 2;
        if (colorDescriptionPresent)
        {
            colorPrimaries = (int)reader.ReadBits(payload, 8);
            transferCharacteristics = (int)reader.ReadBits(payload, 8);
            matrixCoefficients = (int)reader.ReadBits(payload, 8);
        }

        bool fullRange;
        bool subsamplingX;
        bool subsamplingY;
        int chromaSamplePosition = 0;
        bool separateUvDeltaQuantizer = false;
        if (monochrome)
        {
            fullRange = reader.ReadBit(payload) != 0;
            subsamplingX = true;
            subsamplingY = true;
        }
        else if (colorPrimaries == 1
            && transferCharacteristics == 13
            && matrixCoefficients == 0)
        {
            fullRange = true;
            subsamplingX = false;
            subsamplingY = false;
        }
        else
        {
            fullRange = reader.ReadBit(payload) != 0;
            if (sequenceProfile == 0)
            {
                subsamplingX = true;
                subsamplingY = true;
            }
            else if (sequenceProfile == 1)
            {
                subsamplingX = false;
                subsamplingY = false;
            }
            else if (bitDepth == 12)
            {
                subsamplingX = reader.ReadBit(payload) != 0;
                subsamplingY = subsamplingX && reader.ReadBit(payload) != 0;
            }
            else
            {
                subsamplingX = true;
                subsamplingY = false;
            }

            if (subsamplingX && subsamplingY)
            {
                chromaSamplePosition = (int)reader.ReadBits(payload, 2);
            }
        }

        if (!monochrome)
        {
            separateUvDeltaQuantizer = reader.ReadBit(payload) != 0;
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return new Av1ColorConfigurationParseResult(
                NormalizeCode(reader.Code),
                false,
                false,
                false,
                false,
                false,
                0,
                0,
                0,
                0,
                false,
                false,
                false);
        }

        return new Av1ColorConfigurationParseResult(
            AvifResultCode.Success,
            highBitDepth,
            twelveBit,
            monochrome,
            subsamplingX,
            subsamplingY,
            chromaSamplePosition,
            colorPrimaries,
            transferCharacteristics,
            matrixCoefficients,
            fullRange,
            colorDescriptionPresent,
            separateUvDeltaQuantizer);
    }

    internal static bool IsValidLevel(int level)
    {
        return level >= 0 && (level <= 23 || level == 31);
    }

    internal static AvifResultCode NormalizeCode(AvifResultCode code)
    {
        if (code == AvifResultCode.TruncatedData)
        {
            return code;
        }

        return AvifResultCode.InvalidAv1Bitstream;
    }

    internal static Av1Leb128ParseResult ReadLeb128(
        ReadOnlySpan<byte> source,
        ByteReader reader)
    {
        if (reader == null)
        {
            return new Av1Leb128ParseResult(AvifResultCode.InvalidArgument, 0);
        }

        ulong value = 0;
        int index = 0;
        while (index < 8)
        {
            byte encoded = reader.ReadUInt8(source);
            if (reader.Code != AvifResultCode.Success)
            {
                return new Av1Leb128ParseResult(
                    NormalizeCode(reader.Code),
                    0);
            }

            value |= (ulong)(encoded & 127) << index * 7;
            if ((encoded & 128) == 0)
            {
                return new Av1Leb128ParseResult(AvifResultCode.Success, value);
            }

            index++;
        }

        return new Av1Leb128ParseResult(
            AvifResultCode.InvalidAv1Bitstream,
            0);
    }

    internal static AvifResultCode ReadTrailingBits(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        if (reader == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (reader.RemainingBits() <= 0 || reader.ReadBit(payload) != 1)
        {
            return reader.Code == AvifResultCode.Success
                ? AvifResultCode.InvalidAv1Bitstream
                : NormalizeCode(reader.Code);
        }

        while (reader.RemainingBits() > 0)
        {
            if (reader.ReadBit(payload) != 0)
            {
                return AvifResultCode.InvalidAv1Bitstream;
            }
        }

        return reader.Code == AvifResultCode.Success
            ? AvifResultCode.Success
            : NormalizeCode(reader.Code);
    }

    internal static Av1SequenceHeaderParseResult CreateResult(
        AvifResultCode code,
        ParsedAv1SequenceHeader? header)
    {
        return new Av1SequenceHeaderParseResult(code, header);
    }
}

internal class Av1ColorConfigurationParseResult
{
    internal AvifResultCode Code;
    internal bool HighBitDepth;
    internal bool TwelveBit;
    internal bool Monochrome;
    internal bool ChromaSubsamplingX;
    internal bool ChromaSubsamplingY;
    internal int ChromaSamplePosition;
    internal int ColorPrimaries;
    internal int TransferCharacteristics;
    internal int MatrixCoefficients;
    internal bool FullRange;
    internal bool ColorDescriptionPresent;
    internal bool SeparateUvDeltaQuantizer;

    internal Av1ColorConfigurationParseResult(
        AvifResultCode code,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool chromaSubsamplingX,
        bool chromaSubsamplingY,
        int chromaSamplePosition,
        int colorPrimaries,
        int transferCharacteristics,
        int matrixCoefficients,
        bool fullRange,
        bool colorDescriptionPresent,
        bool separateUvDeltaQuantizer)
    {
        Code = code;
        HighBitDepth = highBitDepth;
        TwelveBit = twelveBit;
        Monochrome = monochrome;
        ChromaSubsamplingX = chromaSubsamplingX;
        ChromaSubsamplingY = chromaSubsamplingY;
        ChromaSamplePosition = chromaSamplePosition;
        ColorPrimaries = colorPrimaries;
        TransferCharacteristics = transferCharacteristics;
        MatrixCoefficients = matrixCoefficients;
        FullRange = fullRange;
        ColorDescriptionPresent = colorDescriptionPresent;
        SeparateUvDeltaQuantizer = separateUvDeltaQuantizer;
    }
}
