using System;
using System.Collections.Generic;

namespace avif;

internal class Av1ItemConfigurationParseResult
{
    internal AvifResultCode Code;
    internal ParsedAv1Configuration? Configuration;

    internal Av1ItemConfigurationParseResult(
        AvifResultCode code,
        ParsedAv1Configuration? configuration)
    {
        Code = code;
        Configuration = configuration;
    }
}

internal class ImageSpatialExtentsParseResult
{
    internal AvifResultCode Code;
    internal ParsedImageSpatialExtents? Extents;

    internal ImageSpatialExtentsParseResult(
        AvifResultCode code,
        ParsedImageSpatialExtents? extents)
    {
        Code = code;
        Extents = extents;
    }
}

internal class PixelInformationParseResult
{
    internal AvifResultCode Code;
    internal ParsedPixelInformation? Information;

    internal PixelInformationParseResult(
        AvifResultCode code,
        ParsedPixelInformation? information)
    {
        Code = code;
        Information = information;
    }
}

internal class ParsedAv1Configuration
{
    int sequenceProfile;
    int sequenceLevelIndex;
    int sequenceTier;
    bool highBitDepth;
    bool twelveBit;
    bool monochrome;
    bool chromaSubsamplingX;
    bool chromaSubsamplingY;
    int chromaSamplePosition;
    bool initialPresentationDelayPresent;
    int initialPresentationDelayMinusOne;
    byte[] configObus;
    ParsedAv1SequenceHeader? configSequenceHeader;
    long configObusSourceOffset;

    internal ParsedAv1Configuration(
        int sequenceProfile,
        int sequenceLevelIndex,
        int sequenceTier,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool chromaSubsamplingX,
        bool chromaSubsamplingY,
        int chromaSamplePosition,
        bool initialPresentationDelayPresent,
        int initialPresentationDelayMinusOne,
        byte[] configObus,
        ParsedAv1SequenceHeader? configSequenceHeader)
    {
        this.sequenceProfile = sequenceProfile;
        this.sequenceLevelIndex = sequenceLevelIndex;
        this.sequenceTier = sequenceTier;
        this.highBitDepth = highBitDepth;
        this.twelveBit = twelveBit;
        this.monochrome = monochrome;
        this.chromaSubsamplingX = chromaSubsamplingX;
        this.chromaSubsamplingY = chromaSubsamplingY;
        this.chromaSamplePosition = chromaSamplePosition;
        this.initialPresentationDelayPresent = initialPresentationDelayPresent;
        this.initialPresentationDelayMinusOne = initialPresentationDelayMinusOne;
        this.configObus = (byte[])configObus.Clone();
        this.configSequenceHeader = configSequenceHeader;
        configObusSourceOffset = -1;
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

    internal bool IsHighBitDepth()
    {
        return highBitDepth;
    }

    internal bool IsTwelveBit()
    {
        return twelveBit;
    }

    internal bool IsMonochrome()
    {
        return monochrome;
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

    internal bool HasInitialPresentationDelay()
    {
        return initialPresentationDelayPresent;
    }

    internal int GetInitialPresentationDelayMinusOne()
    {
        return initialPresentationDelayPresent
            ? initialPresentationDelayMinusOne
            : -1;
    }

    internal int GetBitDepth()
    {
        if (twelveBit)
        {
            return 12;
        }

        return highBitDepth ? 10 : 8;
    }

    internal int GetChannelCount()
    {
        return monochrome ? 1 : 3;
    }

    internal byte[] GetConfigObus()
    {
        return (byte[])configObus.Clone();
    }

    internal long GetConfigObusSourceOffset()
    {
        return configObusSourceOffset;
    }

    internal void SetConfigObusSourceOffset(long value)
    {
        configObusSourceOffset = value;
    }

    internal long GetRetainedByteCount()
    {
        long byteCount = configObus.Length;
        if (configSequenceHeader != null)
        {
            byteCount += configSequenceHeader.GetRawPayloadLength();
        }

        return byteCount;
    }

    internal bool HasConfigSequenceHeader()
    {
        return configSequenceHeader != null;
    }

    internal ParsedAv1SequenceHeader? GetConfigSequenceHeader()
    {
        return configSequenceHeader;
    }

    internal bool MatchesSequenceHeader(ParsedAv1SequenceHeader? header)
    {
        if (header == null)
        {
            return false;
        }

        return sequenceProfile == header.GetSequenceProfile()
            && sequenceLevelIndex == header.GetSequenceLevelIndex()
            && sequenceTier == header.GetSequenceTier()
            && highBitDepth == header.IsHighBitDepth()
            && twelveBit == header.IsTwelveBit()
            && monochrome == header.IsMonochrome()
            && chromaSubsamplingX == header.HasChromaSubsamplingX()
            && chromaSubsamplingY == header.HasChromaSubsamplingY()
            && chromaSamplePosition == header.GetChromaSamplePosition();
    }

    internal AvifResultCode ReconcileSequenceHeader(ReadOnlySpan<byte> itemData)
    {
        return ReconcileSequenceHeaderResult(itemData).Code;
    }

    internal Av1SequenceHeaderParseResult ReconcileSequenceHeaderResult(
        ReadOnlySpan<byte> itemData)
    {
        Av1SequenceHeaderParseResult parsed = Av1SequenceHeaderParser.ParseObuStream(
            itemData,
            false,
            true);
        if (parsed.Code != AvifResultCode.Success)
        {
            return parsed;
        }

        if (!MatchesSequenceHeader(parsed.Header))
        {
            return new Av1SequenceHeaderParseResult(
                AvifResultCode.NonConformingData,
                null);
        }

        if (configSequenceHeader != null
            && !configSequenceHeader.PayloadEquals((parsed.Header ?? throw new InvalidOperationException("Invalid internal state.")).GetRawPayload()))
        {
            return new Av1SequenceHeaderParseResult(
                AvifResultCode.NonConformingData,
                null);
        }

        return parsed;
    }

    internal AvifResultCode ReconcilePixelInformation(ParsedPixelInformation information)
    {
        if (information == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (information.GetChannelCount() != GetChannelCount())
        {
            return AvifResultCode.NonConformingData;
        }

        int index = 0;
        while (index < information.GetChannelCount())
        {
            if ((information.GetChannel(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth() != GetBitDepth())
            {
                return AvifResultCode.NonConformingData;
            }

            index++;
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode ReconcileColourInformation(
        ParsedColourInformation information)
    {
        return ReconcileColourInformationWithSequenceHeader(
            information, configSequenceHeader);
    }

    internal AvifResultCode ReconcileColourInformationWithSequenceHeader(
        ParsedColourInformation information,
        ParsedAv1SequenceHeader? sequenceHeader)
    {
        if (information == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sequenceHeader == null
            || information.GetColourType() != "nclx")
        {
            return AvifResultCode.Success;
        }

        if ((sequenceHeader.GetColorPrimaries() != 2
                && information.GetColourPrimaries()
                    != sequenceHeader.GetColorPrimaries())
            || (sequenceHeader.GetTransferCharacteristics() != 2
                && information.GetTransferCharacteristics()
                    != sequenceHeader.GetTransferCharacteristics())
            || (sequenceHeader.GetMatrixCoefficients() != 2
                && information.GetMatrixCoefficients()
                    != sequenceHeader.GetMatrixCoefficients())
            || information.IsFullRange() != sequenceHeader.IsFullRange())
        {
            return AvifResultCode.NonConformingData;
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode ReconcileOperatingPointSelector(
        ParsedOperatingPointSelector selector)
    {
        if (selector == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (configSequenceHeader != null
            && selector.GetOperatingPointIndex()
                >= configSequenceHeader.GetOperatingPointCount())
        {
            return AvifResultCode.NonConformingData;
        }

        return AvifResultCode.Success;
    }
}

internal class ParsedImageSpatialExtents
{
    uint width;
    uint height;

    internal ParsedImageSpatialExtents(uint width, uint height)
    {
        this.width = width;
        this.height = height;
    }

    internal uint GetWidth()
    {
        return width;
    }

    internal uint GetHeight()
    {
        return height;
    }
}

internal class ParsedPixelChannel
{
    int bitDepth;
    int channelIdentifier;
    int componentFormat;
    bool subsampled;
    int subsamplingType;
    int subsamplingLocation;
    string label;

    internal ParsedPixelChannel(
        int bitDepth,
        int channelIdentifier,
        int componentFormat,
        bool subsampled,
        int subsamplingType,
        int subsamplingLocation,
        string label)
    {
        this.bitDepth = bitDepth;
        this.channelIdentifier = channelIdentifier;
        this.componentFormat = componentFormat;
        this.subsampled = subsampled;
        this.subsamplingType = subsamplingType;
        this.subsamplingLocation = subsamplingLocation;
        this.label = label;
    }

    internal int GetBitDepth()
    {
        return bitDepth;
    }

    internal int GetChannelIdentifier()
    {
        return channelIdentifier;
    }

    internal int GetComponentFormat()
    {
        return componentFormat;
    }

    internal bool IsSubsampled()
    {
        return subsampled;
    }

    internal int GetSubsamplingType()
    {
        return subsampled ? subsamplingType : -1;
    }

    internal int GetSubsamplingLocation()
    {
        return subsampled ? subsamplingLocation : -1;
    }

    internal string GetLabel()
    {
        return label;
    }
}

internal class ParsedPixelInformation
{
    bool extended;
    List<ParsedPixelChannel> channels;

    internal ParsedPixelInformation(
        bool extended,
        List<ParsedPixelChannel> channels)
    {
        this.extended = extended;
        this.channels = new List<ParsedPixelChannel>(channels);
    }

    internal bool IsExtended()
    {
        return extended;
    }

    internal int GetChannelCount()
    {
        return channels.Count;
    }

    internal ParsedPixelChannel? GetChannel(int index)
    {
        if (index < 0 || index >= channels.Count)
        {
            return null;
        }

        return channels[index];
    }
}

internal static class Av1ItemConfigurationBox
{
    internal static Av1ItemConfigurationParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox? box)
    {
        if (box == null || box.Type != "av1C")
        {
            return CreateResult(AvifResultCode.InvalidArgument, null);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            box.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code, null);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)box.PayloadOffset,
            (int)box.PayloadSize);
        Av1ItemConfigurationParseResult result = ParsePayload(payload);
        if (result.Code == AvifResultCode.Success
            && result.Configuration != null)
        {
            CheckedIntegerResult offset = CheckedMath.AddInt64(
                box.PayloadOffset,
                4);
            if (!offset.Success)
            {
                return CreateResult(offset.Code, null);
            }
            result.Configuration.SetConfigObusSourceOffset(offset.Value);
        }
        return result;
    }

    internal static Av1ItemConfigurationParseResult ParsePayload(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        int marker = payload[0] >> 7;
        int version = payload[0] & 127;
        int sequenceProfile = payload[1] >> 5;
        int sequenceLevelIndex = payload[1] & 31;
        int sequenceTier = payload[2] >> 7;
        bool highBitDepth = (payload[2] & 64) != 0;
        bool twelveBit = (payload[2] & 32) != 0;
        bool monochrome = (payload[2] & 16) != 0;
        bool chromaSubsamplingX = (payload[2] & 8) != 0;
        bool chromaSubsamplingY = (payload[2] & 4) != 0;
        int chromaSamplePosition = payload[2] & 3;
        int reserved = payload[3] >> 5;
        bool delayPresent = (payload[3] & 16) != 0;
        int delayValue = payload[3] & 15;

        if (marker != 1 || version != 1 || reserved != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        if (!delayPresent && delayValue != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        if (sequenceProfile > 2
            || (sequenceLevelIndex > 23 && sequenceLevelIndex != 31))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (sequenceLevelIndex <= 7 && sequenceTier != 0)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (twelveBit && (!highBitDepth || sequenceProfile != 2))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (sequenceProfile == 1 && monochrome)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (!monochrome && sequenceProfile == 0
            && (!chromaSubsamplingX || !chromaSubsamplingY))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (sequenceProfile == 1
            && (chromaSubsamplingX || chromaSubsamplingY))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (!monochrome && sequenceProfile == 2 && !twelveBit
            && (!chromaSubsamplingX || chromaSubsamplingY))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (!monochrome && sequenceProfile == 2 && twelveBit
            && !chromaSubsamplingX && chromaSubsamplingY)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if (monochrome
            && (!chromaSubsamplingX
                || !chromaSubsamplingY
                || chromaSamplePosition != 0))
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        if ((!chromaSubsamplingX || !chromaSubsamplingY)
            && chromaSamplePosition != 0)
        {
            return CreateResult(AvifResultCode.InvalidAv1Bitstream, null);
        }

        byte[] configObus = payload.Slice(4).ToArray();
        Av1SequenceHeaderParseResult sequence = Av1SequenceHeaderParser.ParseObuStream(
            configObus,
            true,
            false);
        if (sequence.Code != AvifResultCode.Success)
        {
            return CreateResult(sequence.Code, null);
        }

        ParsedAv1Configuration configuration = new ParsedAv1Configuration(
            sequenceProfile,
            sequenceLevelIndex,
            sequenceTier,
            highBitDepth,
            twelveBit,
            monochrome,
            chromaSubsamplingX,
            chromaSubsamplingY,
            chromaSamplePosition,
            delayPresent,
            delayValue,
            configObus,
            sequence.Header);
        if (sequence.Header != null && !configuration.MatchesSequenceHeader(sequence.Header))
        {
            return CreateResult(AvifResultCode.NonConformingData, null);
        }

        return CreateResult(AvifResultCode.Success, configuration);
    }

    internal static Av1ItemConfigurationParseResult CreateResult(
        AvifResultCode code,
        ParsedAv1Configuration? configuration)
    {
        return new Av1ItemConfigurationParseResult(code, configuration);
    }
}

internal static class ImageSpatialExtentsProperty
{
    internal static ImageSpatialExtentsParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ResourceGuard resources)
    {
        if (box == null || box.Type != "ispe" || resources == null)
        {
            return CreateResult(AvifResultCode.InvalidArgument, null);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code, null);
        }

        if (fullBox.Version != 0)
        {
            return CreateResult(AvifResultCode.UnsupportedFeature, null);
        }

        if (fullBox.Flags != 0 || fullBox.PayloadSize != 8)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code, null);
        }

        ReadOnlySpan<byte> payload = source.Slice((int)fullBox.PayloadOffset, 8);
        ByteReader reader = new ByteReader(payload.Length);
        uint width = reader.ReadUInt32BigEndian(payload);
        uint height = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success || width == 0 || height == 0)
        {
            return CreateResult(AvifResultCode.InvalidItem, null);
        }

        if (width > int.MaxValue || height > int.MaxValue)
        {
            return CreateResult(AvifResultCode.LimitExceeded, null);
        }

        AvifResultCode dimensionCode = resources.CheckImageDimensions(
            (int)width,
            (int)height);
        if (dimensionCode != AvifResultCode.Success)
        {
            return CreateResult(dimensionCode, null);
        }

        return CreateResult(
            AvifResultCode.Success,
            new ParsedImageSpatialExtents(width, height));
    }

    internal static ImageSpatialExtentsParseResult CreateResult(
        AvifResultCode code,
        ParsedImageSpatialExtents? extents)
    {
        return new ImageSpatialExtentsParseResult(code, extents);
    }
}

internal static class PixelInformationProperty
{
    internal static PixelInformationParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        if (box == null || box.Type != "pixi")
        {
            return CreateResult(AvifResultCode.InvalidArgument, null);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code, null);
        }

        if (fullBox.Version != 0)
        {
            return CreateResult(AvifResultCode.UnsupportedFeature, null);
        }

        if (fullBox.Flags != 0 && fullBox.Flags != 1)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code, null);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        byte channelCount = reader.ReadUInt8(payload);
        if (reader.Code != AvifResultCode.Success || channelCount == 0)
        {
            return CreateResult(AvifResultCode.InvalidItem, null);
        }

        List<int> bitDepths = new List<int>();
        int channelIndex = 0;
        while (channelIndex < channelCount)
        {
            byte bitDepth = reader.ReadUInt8(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateResult(AvifResultCode.InvalidContainer, null);
            }

            if (bitDepth == 0)
            {
                return CreateResult(AvifResultCode.InvalidItem, null);
            }

            bitDepths.Add(bitDepth);
            channelIndex++;
        }

        List<ParsedPixelChannel> channels = new List<ParsedPixelChannel>();
        int alphaChannelCount = 0;
        channelIndex = 0;
        while (channelIndex < channelCount)
        {
            int channelIdentifier = 0;
            int componentFormat = 0;
            bool subsampled = false;
            int subsamplingType = 0;
            int subsamplingLocation = 0;
            string label = string.Empty;
            if (fullBox.Flags == 1)
            {
                byte channelFields = reader.ReadUInt8(payload);
                if (reader.Code != AvifResultCode.Success)
                {
                    return CreateResult(AvifResultCode.InvalidContainer, null);
                }

                channelIdentifier = channelFields >> 5;
                int reserved = channelFields >> 4 & 1;
                componentFormat = channelFields >> 2 & 3;
                subsampled = (channelFields & 2) != 0;
                bool labelPresent = (channelFields & 1) != 0;
                if (channelIdentifier > 2 || reserved != 0)
                {
                    return CreateResult(AvifResultCode.InvalidItem, null);
                }

                if (channelIdentifier == 1)
                {
                    alphaChannelCount++;
                    if (alphaChannelCount > 1)
                    {
                        return CreateResult(AvifResultCode.InvalidItem, null);
                    }
                }

                if (subsampled)
                {
                    byte subsampling = reader.ReadUInt8(payload);
                    subsamplingType = subsampling >> 4;
                    subsamplingLocation = subsampling & 15;
                    if (reader.Code != AvifResultCode.Success)
                    {
                        return CreateResult(AvifResultCode.InvalidContainer, null);
                    }

                    if (subsamplingType > 4 || subsamplingLocation > 4)
                    {
                        return CreateResult(AvifResultCode.InvalidItem, null);
                    }
                }

                if (labelPresent)
                {
                    label = reader.ReadNullTerminatedUtf8(payload);
                    if (reader.Code != AvifResultCode.Success)
                    {
                        return CreateResult(AvifResultCode.InvalidContainer, null);
                    }
                }
            }

            channels.Add(new ParsedPixelChannel(
                bitDepths[channelIndex],
                channelIdentifier,
                componentFormat,
                subsampled,
                subsamplingType,
                subsamplingLocation,
                label));
            channelIndex++;
        }

        if (reader.Remaining() != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer, null);
        }

        return CreateResult(
            AvifResultCode.Success,
            new ParsedPixelInformation(fullBox.Flags == 1, channels));
    }

    internal static PixelInformationParseResult CreateResult(
        AvifResultCode code,
        ParsedPixelInformation? information)
    {
        return new PixelInformationParseResult(code, information);
    }
}
