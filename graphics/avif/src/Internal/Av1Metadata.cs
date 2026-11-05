namespace avif;

internal class Av1MetadataParseResult
{
    internal AvifResultCode Code;
    internal ParsedAv1Metadata? Metadata;

    internal Av1MetadataParseResult(AvifResultCode code, ParsedAv1Metadata? metadata)
    {
        Code = code;
        Metadata = metadata;
    }
}

internal class ParsedAv1Metadata
{
    internal ulong MetadataType;
    internal Av1ContentLightLevelMetadata? ContentLightLevel;
    internal Av1MasteringDisplayColorVolumeMetadata? MasteringDisplayColorVolume;
    internal Av1ScalabilityMetadata? Scalability;
    internal Av1ItuT35Metadata? ItuT35;
    internal Av1TimecodeMetadata? Timecode;
    internal byte[] OpaquePayload;

    internal ParsedAv1Metadata(ulong metadataType)
    {
        MetadataType = metadataType;
        OpaquePayload = System.Array.Empty<byte>();
    }

    internal bool IsKnownType()
    {
        return MetadataType >= (ulong)Av1MetadataType.HdrContentLightLevel
            && MetadataType <= (ulong)Av1MetadataType.Timecode;
    }
}

internal class Av1ContentLightLevelMetadata
{
    internal int MaximumContentLightLevel;
    internal int MaximumFrameAverageLightLevel;
}

internal class Av1MasteringDisplayColorVolumeMetadata
{
    internal int[] PrimaryChromaticityX;
    internal int[] PrimaryChromaticityY;
    internal int WhitePointChromaticityX;
    internal int WhitePointChromaticityY;
    internal uint MaximumLuminance;
    internal uint MinimumLuminance;

    internal Av1MasteringDisplayColorVolumeMetadata()
    {
        PrimaryChromaticityX = new int[3];
        PrimaryChromaticityY = new int[3];
    }
}

internal class Av1TemporalGroupMetadata
{
    internal int TemporalId;
    internal bool TemporalSwitchingUpPoint;
    internal bool SpatialSwitchingUpPoint;
    internal int[] ReferencePictureDifferences;

    internal Av1TemporalGroupMetadata()
    {
        ReferencePictureDifferences = System.Array.Empty<int>();
    }
}

internal class Av1ScalabilityMetadata
{
    internal int ModeIdc;
    internal bool HasScalabilityStructure;
    internal int SpatialLayerCount;
    internal bool SpatialLayerDimensionsPresent;
    internal bool SpatialLayerDescriptionsPresent;
    internal bool TemporalGroupDescriptionsPresent;
    internal int[] SpatialLayerMaximumWidths;
    internal int[] SpatialLayerMaximumHeights;
    internal int[] SpatialLayerReferenceIds;
    internal Av1TemporalGroupMetadata[] TemporalGroups;

    internal Av1ScalabilityMetadata()
    {
        SpatialLayerMaximumWidths = System.Array.Empty<int>();
        SpatialLayerMaximumHeights = System.Array.Empty<int>();
        SpatialLayerReferenceIds = System.Array.Empty<int>();
        TemporalGroups = System.Array.Empty<Av1TemporalGroupMetadata>();
    }
}

internal class Av1ItuT35Metadata
{
    internal int CountryCode;
    internal bool HasCountryCodeExtension;
    internal int CountryCodeExtension;
    internal byte[] Payload;
    internal bool HasHdr10PlusEnvelope;
    internal int TerminalProviderCode;
    internal int TerminalProviderOrientedCode;
    internal int ApplicationIdentifier;
    internal int ApplicationMode;
    internal byte[] Hdr10PlusPayload;

    internal Av1ItuT35Metadata()
    {
        Payload = System.Array.Empty<byte>();
        Hdr10PlusPayload = System.Array.Empty<byte>();
    }
}

internal class Av1TimecodeMetadata
{
    internal int CountingType;
    internal bool FullTimestamp;
    internal bool Discontinuity;
    internal bool CountDropped;
    internal int FrameCount;
    internal bool SecondsPresent;
    internal int Seconds;
    internal bool MinutesPresent;
    internal int Minutes;
    internal bool HoursPresent;
    internal int Hours;
    internal int TimeOffsetLength;
    internal ulong TimeOffsetValue;
}
