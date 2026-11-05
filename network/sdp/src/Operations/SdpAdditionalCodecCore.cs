using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpAdditionalCodecCore
{
    internal static bool IsVp8Encoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.Vp8);
    }

    internal static bool IsVp9Encoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.Vp9);
    }

    internal static bool IsHevcEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.H265) || NameEquals(encodingName, SdpCodecNames.Hevc);
    }

    internal static bool IsRetransmissionEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.Retransmission);
    }

    internal static bool IsRedEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.RedundantAudioData);
    }

    internal static bool IsUlpFecEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.UnequalLossProtectionFec);
    }

    internal static bool IsFlexFecEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.FlexibleFec);
    }

    internal static bool IsComfortNoiseEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.ComfortNoise);
    }

    internal static bool IsTelephoneEventEncoding(string encodingName)
    {
        return NameEquals(encodingName, SdpCodecNames.TelephoneEvent);
    }

    internal static bool IsAdditionalTypedEncoding(string encodingName)
    {
        return IsVp8Encoding(encodingName)
            || IsVp9Encoding(encodingName)
            || IsHevcEncoding(encodingName)
            || IsRetransmissionEncoding(encodingName)
            || IsRedEncoding(encodingName)
            || IsUlpFecEncoding(encodingName)
            || IsFlexFecEncoding(encodingName)
            || IsComfortNoiseEncoding(encodingName)
            || IsTelephoneEventEncoding(encodingName);
    }

    internal static bool UsesRawPayloadList(string encodingName)
    {
        return IsRedEncoding(encodingName) || IsTelephoneEventEncoding(encodingName);
    }

    internal static bool AreCodecParametersValid(SdpCodec codec)
    {
        if (UsesRawPayloadList(codec.EncodingName))
        {
            if (codec.FormatParameters.Count == 0)
            {
                return true;
            }
            if (codec.FormatParameters.Count != 1)
            {
                return false;
            }
            string raw = codec.FormatParameters[0].RawParameters;
            if (raw.Length == 0 && codec.FormatParameters[0].Parameters.Count == 1 && !codec.FormatParameters[0].Parameters[0].HasValue)
            {
                raw = codec.FormatParameters[0].Parameters[0].Name;
            }
            return IsRawFormatValueValid(codec.EncodingName, raw);
        }
        if (SdpCodecServiceCore.HasUnparsedFormatParameters(codec))
        {
            return false;
        }
        return AreParameterValuesValid(codec.EncodingName, SdpCodecServiceCore.CollectCodecParameters(codec));
    }

    internal static bool AreCapabilityParametersValid(SdpCodecCapability capability)
    {
        if (UsesRawPayloadList(capability.EncodingName))
        {
            if (capability.FormatParameters.Count == 0)
            {
                return true;
            }
            if (capability.FormatParameters.Count != 1 || capability.FormatParameters[0].HasValue)
            {
                return false;
            }
            return IsRawFormatValueValid(capability.EncodingName, capability.FormatParameters[0].Name);
        }
        return AreParameterValuesValid(capability.EncodingName, capability.FormatParameters);
    }

    internal static bool AreParameterValuesValid(string encodingName, List<SdpFormatParameter> parameters)
    {
        List<string> recognizedNames = new List<string>();
        int index = 0;
        while (index < parameters.Count)
        {
            SdpFormatParameter parameter = parameters[index];
            if (parameter == null || ReadParameterValidationError(encodingName, parameter).Length > 0)
            {
                return false;
            }
            if (IsRecognizedParameter(encodingName, parameter.Name))
            {
                if (SdpOpusCodecCore.ContainsParameterName(recognizedNames, parameter.Name))
                {
                    return false;
                }
                recognizedNames.Add(parameter.Name);
            }
            index = index + 1;
        }
        return ReadParameterCollectionValidationError(encodingName, parameters).Length == 0;
    }

    internal static string ReadParameterCollectionValidationError(string encodingName, List<SdpFormatParameter> parameters)
    {
        if (IsRetransmissionEncoding(encodingName) && !ContainsParameter(parameters, "apt"))
        {
            return "An RTX format requires an apt parameter.";
        }
        if (IsFlexFecEncoding(encodingName) && !ContainsParameter(parameters, "repair-window"))
        {
            return "A FlexFEC format requires a repair-window parameter.";
        }
        if ((IsVp8Encoding(encodingName) || IsVp9Encoding(encodingName)) && ContainsParameter(parameters, "max-fr") != ContainsParameter(parameters, "max-fs"))
        {
            return "VP8 and VP9 receiver limits must provide max-fr and max-fs together.";
        }
        if (IsHevcEncoding(encodingName) && ContainsParameter(parameters, "max-recv-level-id") && ReadIntegerParameter(parameters, "max-recv-level-id", 0) <= ReadIntegerParameter(parameters, "level-id", 93))
        {
            return "The HEVC maximum receive level must be higher than the default level.";
        }
        return string.Empty;
    }

    internal static string ReadParameterValidationError(string encodingName, SdpFormatParameter parameter)
    {
        if (!IsRecognizedParameter(encodingName, parameter.Name))
        {
            return string.Empty;
        }
        if (!parameter.HasValue)
        {
            return "A recognized codec format parameter requires an equals-delimited value.";
        }
        if ((IsVp8Encoding(encodingName) || IsVp9Encoding(encodingName)) && (NameEquals(parameter.Name, "max-fr") || NameEquals(parameter.Name, "max-fs")) && !IsPositiveInteger(parameter.Value))
        {
            return "A VP8 or VP9 frame limit must be a positive canonical integer.";
        }
        if (IsVp9Encoding(encodingName) && NameEquals(parameter.Name, "profile-id") && !IsIntegerInRange(parameter.Value, 0, 3))
        {
            return "The VP9 profile-id must be an integer from zero through three.";
        }
        if (IsHevcEncoding(encodingName))
        {
            return ReadHevcParameterValidationError(parameter);
        }
        if (IsRetransmissionEncoding(encodingName) && NameEquals(parameter.Name, "apt") && !IsIntegerInRange(parameter.Value, 0, 127))
        {
            return "The RTX associated payload type must be an integer from zero through 127.";
        }
        if (IsRetransmissionEncoding(encodingName) && NameEquals(parameter.Name, "rtx-time") && !IsNonnegativeInteger(parameter.Value))
        {
            return "The RTX retention time must be a nonnegative canonical integer.";
        }
        if (IsUlpFecEncoding(encodingName) && NameEquals(parameter.Name, "onelevelonly") && parameter.Value != "0" && parameter.Value != "1")
        {
            return "The ULPFEC onelevelonly value must be zero or one.";
        }
        if (IsFlexFecEncoding(encodingName) && NameEquals(parameter.Name, "repair-window") && !IsPositiveInteger(parameter.Value))
        {
            return "The FlexFEC repair window must be a positive canonical integer number of microseconds.";
        }
        return string.Empty;
    }

    internal static string ReadHevcParameterValidationError(SdpFormatParameter parameter)
    {
        if (NameEquals(parameter.Name, "profile-space") && !IsIntegerInRange(parameter.Value, 0, 3))
        {
            return "The HEVC profile-space must be an integer from zero through three.";
        }
        if (NameEquals(parameter.Name, "tier-flag") && parameter.Value != "0" && parameter.Value != "1")
        {
            return "The HEVC tier-flag must be zero or one.";
        }
        if (NameEquals(parameter.Name, "profile-id") && !IsIntegerInRange(parameter.Value, 0, 31))
        {
            return "The HEVC profile-id must be an integer from zero through 31.";
        }
        if (NameEquals(parameter.Name, "profile-compatibility-indicator") && !IsHexValue(parameter.Value, 8))
        {
            return "The HEVC profile compatibility indicator must contain exactly four hexadecimal octets.";
        }
        if (NameEquals(parameter.Name, "interop-constraints") && !IsHexValue(parameter.Value, 12))
        {
            return "The HEVC interoperability constraints must contain exactly six hexadecimal octets.";
        }
        if ((NameEquals(parameter.Name, "level-id") || NameEquals(parameter.Name, "max-recv-level-id")) && !IsIntegerInRange(parameter.Value, 0, 255))
        {
            return "An HEVC level identifier must be an integer from zero through 255.";
        }
        if ((NameEquals(parameter.Name, "sprop-sub-layer-id") || NameEquals(parameter.Name, "recv-sub-layer-id")) && !IsIntegerInRange(parameter.Value, 0, 6))
        {
            return "An HEVC sub-layer identifier must be an integer from zero through six.";
        }
        if (NameEquals(parameter.Name, "tx-mode") && parameter.Value != "SRST" && parameter.Value != "MRST" && parameter.Value != "MRMT")
        {
            return "The HEVC transmission mode must be SRST, MRST, or MRMT.";
        }
        if (NameEquals(parameter.Name, "max-dpb") && !IsIntegerInRange(parameter.Value, 1, 16))
        {
            return "The HEVC maximum decoded picture buffer value must be an integer from one through 16.";
        }
        if ((NameEquals(parameter.Name, "sprop-max-don-diff") || NameEquals(parameter.Name, "sprop-depack-buf-nalus")) && !IsIntegerInRange(parameter.Value, 0, 32767))
        {
            return "An HEVC decoding-order or NAL-unit buffer value must be an integer from zero through 32767.";
        }
        if (NameEquals(parameter.Name, "sprop-segmentation-id") && !IsIntegerInRange(parameter.Value, 0, 3))
        {
            return "The HEVC segmentation identifier must be an integer from zero through three.";
        }
        if (NameEquals(parameter.Name, "include-dph") && parameter.Value != "0" && parameter.Value != "1")
        {
            return "The HEVC include-dph value must be zero or one.";
        }
        if (IsHevcPositiveIntegerParameter(parameter.Name) && !IsPositiveInteger(parameter.Value))
        {
            return "A recognized HEVC size, rate, or buffer parameter must be a positive canonical integer.";
        }
        return string.Empty;
    }

    internal static bool IsRecognizedParameter(string encodingName, string name)
    {
        if (IsVp8Encoding(encodingName))
        {
            return NameEquals(name, "max-fr") || NameEquals(name, "max-fs");
        }
        if (IsVp9Encoding(encodingName))
        {
            return NameEquals(name, "max-fr") || NameEquals(name, "max-fs") || NameEquals(name, "profile-id");
        }
        if (IsHevcEncoding(encodingName))
        {
            return IsHevcParameter(name);
        }
        if (IsRetransmissionEncoding(encodingName))
        {
            return NameEquals(name, "apt") || NameEquals(name, "rtx-time");
        }
        if (IsUlpFecEncoding(encodingName))
        {
            return NameEquals(name, "onelevelonly");
        }
        if (IsFlexFecEncoding(encodingName))
        {
            return NameEquals(name, "repair-window");
        }
        return false;
    }

    internal static bool IsHevcParameter(string name)
    {
        return NameEquals(name, "profile-space")
            || NameEquals(name, "tier-flag")
            || NameEquals(name, "profile-id")
            || NameEquals(name, "profile-compatibility-indicator")
            || NameEquals(name, "interop-constraints")
            || NameEquals(name, "level-id")
            || NameEquals(name, "sprop-sub-layer-id")
            || NameEquals(name, "recv-sub-layer-id")
            || NameEquals(name, "max-recv-level-id")
            || NameEquals(name, "tx-mode")
            || NameEquals(name, "max-lsr")
            || NameEquals(name, "max-lps")
            || NameEquals(name, "max-cpb")
            || NameEquals(name, "max-dpb")
            || NameEquals(name, "max-br")
            || NameEquals(name, "max-tr")
            || NameEquals(name, "max-tc")
            || NameEquals(name, "max-fps")
            || NameEquals(name, "sprop-max-don-diff")
            || NameEquals(name, "sprop-depack-buf-nalus")
            || NameEquals(name, "sprop-depack-buf-bytes")
            || NameEquals(name, "depack-buf-cap")
            || NameEquals(name, "sprop-segmentation-id")
            || NameEquals(name, "include-dph");
    }

    internal static bool IsHevcPositiveIntegerParameter(string name)
    {
        return NameEquals(name, "max-lsr")
            || NameEquals(name, "max-lps")
            || NameEquals(name, "max-cpb")
            || NameEquals(name, "max-br")
            || NameEquals(name, "max-tr")
            || NameEquals(name, "max-tc")
            || NameEquals(name, "max-fps")
            || NameEquals(name, "sprop-depack-buf-bytes")
            || NameEquals(name, "depack-buf-cap");
    }

    internal static bool AreCompatible(SdpCodec codec, SdpCodecCapability capability)
    {
        if (!AreCodecParametersValid(codec) || !AreCapabilityParametersValid(capability))
        {
            return false;
        }
        List<SdpFormatParameter> offered = SdpCodecServiceCore.CollectCodecParameters(codec);
        if (IsVp9Encoding(codec.EncodingName))
        {
            return ReadIntegerParameter(offered, "profile-id", 0) == ReadIntegerParameter(capability.FormatParameters, "profile-id", 0);
        }
        if (IsHevcEncoding(codec.EncodingName))
        {
            return HevcConfigurationsMatch(offered, capability.FormatParameters);
        }
        if (IsRetransmissionEncoding(codec.EncodingName))
        {
            return ReadIntegerParameter(offered, "apt", -1) == ReadIntegerParameter(capability.FormatParameters, "apt", -2);
        }
        if (IsFlexFecEncoding(codec.EncodingName))
        {
            long offeredWindow = ReadLongParameter(offered, "repair-window", -1);
            long supportedWindow = ReadLongParameter(capability.FormatParameters, "repair-window", -1);
            return offeredWindow >= 0 && supportedWindow >= offeredWindow;
        }
        if (IsTelephoneEventEncoding(codec.EncodingName) && capability.FormatParameters.Count > 0)
        {
            string offeredEvents = ReadRawCodecFormat(codec);
            return EventListsOverlap(offeredEvents, capability.FormatParameters[0].Name);
        }
        if (IsRedEncoding(codec.EncodingName) && capability.FormatParameters.Count > 0)
        {
            return NameEquals(ReadRawCodecFormat(codec), capability.FormatParameters[0].Name);
        }
        return true;
    }

    internal static bool RawFormatParametersMatch(SdpCodec codec, SdpCodecCapability capability)
    {
        if (codec.FormatParameters.Count == 0 || capability.FormatParameters.Count == 0)
        {
            return codec.FormatParameters.Count == 0 && capability.FormatParameters.Count == 0;
        }
        if (codec.FormatParameters.Count != 1 || capability.FormatParameters.Count != 1 || capability.FormatParameters[0].HasValue)
        {
            return false;
        }
        return ReadRawCodecFormat(codec) == capability.FormatParameters[0].Name;
    }

    internal static bool AreMappingValuesValid(SdpCodec codec, SdpCodecCapability capability)
    {
        bool codecVideo = codec.MediaKind == SdpMediaKind.Video || NameEquals(codec.MediaType, SdpMediaTypes.Video);
        bool capabilityVideo = capability.MediaKind == SdpMediaKind.Video || NameEquals(capability.MediaType, SdpMediaTypes.Video);
        bool codecAudio = codec.MediaKind == SdpMediaKind.Audio || NameEquals(codec.MediaType, SdpMediaTypes.Audio);
        bool capabilityAudio = capability.MediaKind == SdpMediaKind.Audio || NameEquals(capability.MediaType, SdpMediaTypes.Audio);
        if (IsVp8Encoding(codec.EncodingName) || IsVp9Encoding(codec.EncodingName) || IsHevcEncoding(codec.EncodingName))
        {
            return codecVideo && capabilityVideo && codec.ClockRate == 90000 && capability.ClockRate == 90000 && !capability.HasChannelCount && (codec.RtpMap == null || !codec.RtpMap.HasEncodingParameters);
        }
        if (IsComfortNoiseEncoding(codec.EncodingName) || IsTelephoneEventEncoding(codec.EncodingName))
        {
            return codecAudio && capabilityAudio && (!capability.HasChannelCount || capability.ChannelCount == 1) && (codec.RtpMap == null || !codec.RtpMap.HasEncodingParameters || codec.RtpMap.HasChannelCount && codec.RtpMap.ChannelCount == 1);
        }
        if (IsUlpFecEncoding(codec.EncodingName) || IsFlexFecEncoding(codec.EncodingName))
        {
            return codec.ClockRate > 1000 && capability.ClockRate > 1000;
        }
        return true;
    }

    internal static bool HevcConfigurationsMatch(List<SdpFormatParameter> offered, List<SdpFormatParameter> supported)
    {
        if (ReadIntegerParameter(offered, "profile-space", 0) != ReadIntegerParameter(supported, "profile-space", 0)
            || ReadIntegerParameter(offered, "tier-flag", 0) != ReadIntegerParameter(supported, "tier-flag", 0)
            || ReadIntegerParameter(offered, "profile-id", 1) != ReadIntegerParameter(supported, "profile-id", 1))
        {
            return false;
        }
        if (ReadHevcCompatibilityIndicator(offered) != ReadHevcCompatibilityIndicator(supported)
            || !StringParametersMatch(offered, supported, "interop-constraints", "B00000000000")
            || !StringParametersMatch(offered, supported, "tx-mode", "SRST"))
        {
            return false;
        }
        return true;
    }

    internal static long ReadHevcCompatibilityIndicator(List<SdpFormatParameter> parameters)
    {
        string value = ReadStringParameter(parameters, "profile-compatibility-indicator", string.Empty);
        if (value.Length == 0)
        {
            int profileId = ReadIntegerParameter(parameters, "profile-id", 1);
            return 1L << (31 - profileId);
        }
        long result = 0;
        int index = 0;
        while (index < value.Length)
        {
            result = result * 16 + SdpH264CodecCore.ReadHexDigit(value[index]);
            index = index + 1;
        }
        return result;
    }

    internal static bool StringParametersMatch(List<SdpFormatParameter> first, List<SdpFormatParameter> second, string name, string defaultValue)
    {
        string firstValue = ReadStringParameter(first, name, defaultValue);
        string secondValue = ReadStringParameter(second, name, defaultValue);
        return string.Equals(firstValue, secondValue, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ReadRtpMapValidationError(SdpRtpMapAttribute mapping, bool audio, bool video)
    {
        if ((IsVp8Encoding(mapping.EncodingName) || IsVp9Encoding(mapping.EncodingName) || IsHevcEncoding(mapping.EncodingName)) && (!video || mapping.ClockRate != 90000 || mapping.HasEncodingParameters))
        {
            return "A VP8, VP9, H.265, or HEVC RTP mapping must use video media, a 90000 Hz clock rate, and no encoding parameters.";
        }
        if ((IsComfortNoiseEncoding(mapping.EncodingName) || IsTelephoneEventEncoding(mapping.EncodingName)) && (!audio || mapping.HasEncodingParameters && (!mapping.HasChannelCount || mapping.ChannelCount != 1)))
        {
            return "A comfort-noise or telephone-event RTP mapping must use audio media and at most one channel.";
        }
        if ((IsUlpFecEncoding(mapping.EncodingName) || IsFlexFecEncoding(mapping.EncodingName)) && mapping.ClockRate <= 1000)
        {
            return "A ULPFEC or FlexFEC RTP mapping must use a clock rate greater than 1000 Hz.";
        }
        return string.Empty;
    }

    internal static string ReadMissingFormatParametersError(SdpCodec codec)
    {
        if (codec.FormatParameters.Count == 0 && IsRetransmissionEncoding(codec.EncodingName))
        {
            return "An RTX codec requires an fmtp attribute containing an associated payload type.";
        }
        if (codec.FormatParameters.Count == 0 && IsFlexFecEncoding(codec.EncodingName))
        {
            return "A FlexFEC codec requires an fmtp attribute containing a repair-window value.";
        }
        return string.Empty;
    }

    internal static string ReadAssociationValidationError(SdpCodec codec, List<SdpCodec> codecs)
    {
        if (!AreCodecParametersValid(codec))
        {
            return string.Empty;
        }
        if (IsRetransmissionEncoding(codec.EncodingName))
        {
            int targetPayload = ReadIntegerParameter(SdpCodecServiceCore.CollectCodecParameters(codec), "apt", -1);
            SdpCodec? target = FindCodec(codecs, targetPayload);
            if (target == null || target == codec || IsRetransmissionEncoding(target.EncodingName))
            {
                return "The RTX associated payload type must identify a non-RTX media format in the same media description.";
            }
            if (!CodecRatesAndChannelsMatch(codec, target))
            {
                return "An RTX codec must use the clock rate and channel count of its associated media format.";
            }
        }
        if (IsRedEncoding(codec.EncodingName))
        {
            if (codec.FormatParameters.Count == 0)
            {
                return string.Empty;
            }
            string[] payloads = ReadRawCodecFormat(codec).Split('/');
            int index = 0;
            while (index < payloads.Length)
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(payloads[index].AsSpan());
                SdpCodec? target = payload.Success ? FindCodec(codecs, payload.Int32Value) : null;
                if (target == null || target == codec)
                {
                    return "Every RED payload reference must identify another media format in the same media description.";
                }
                if (!CodecRatesAndChannelsMatch(codec, target))
                {
                    return "A RED codec must use the clock rate and channel count of every referenced media format.";
                }
                index = index + 1;
            }
        }
        if (IsComfortNoiseEncoding(codec.EncodingName) || IsTelephoneEventEncoding(codec.EncodingName))
        {
            if (!HasAssociatedPrimaryCodec(codec, codecs))
            {
                return "A comfort-noise or telephone-event format requires an audio media codec with the same clock rate.";
            }
        }
        return string.Empty;
    }

    internal static bool HasAssociatedPrimaryCodec(SdpCodec codec, List<SdpCodec> codecs)
    {
        int index = 0;
        while (index < codecs.Count)
        {
            SdpCodec candidate = codecs[index];
            if (candidate != codec && candidate.ClockRate == codec.ClockRate && !IsRepairOrSupplementalEncoding(candidate.EncodingName))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsRepairOrSupplementalEncoding(string encodingName)
    {
        return IsRetransmissionEncoding(encodingName)
            || IsRedEncoding(encodingName)
            || IsUlpFecEncoding(encodingName)
            || IsFlexFecEncoding(encodingName)
            || IsComfortNoiseEncoding(encodingName)
            || IsTelephoneEventEncoding(encodingName);
    }

    internal static bool HasRedAssociation(SdpCodec codec, List<SdpCodec> codecs)
    {
        int index = 0;
        while (index < codecs.Count)
        {
            SdpCodec candidate = codecs[index];
            if (IsRedEncoding(candidate.EncodingName) && RawPayloadListContains(ReadRawCodecFormat(candidate), codec.PayloadType))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool CodecRatesAndChannelsMatch(SdpCodec first, SdpCodec second)
    {
        if (first.ClockRate != second.ClockRate)
        {
            return false;
        }
        int firstChannels = first.HasChannelCount ? first.ChannelCount : 1;
        int secondChannels = second.HasChannelCount ? second.ChannelCount : 1;
        return firstChannels == secondChannels;
    }

    internal static SdpCodec? FindCodec(List<SdpCodec> codecs, int payloadType)
    {
        int index = 0;
        while (index < codecs.Count)
        {
            if (codecs[index].PayloadType == payloadType)
            {
                return codecs[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool IsRawFormatValueValid(string encodingName, string value)
    {
        if (IsRedEncoding(encodingName))
        {
            return IsPayloadListValid(value, '/');
        }
        if (IsTelephoneEventEncoding(encodingName))
        {
            return IsEventListValid(value);
        }
        return false;
    }

    internal static bool IsPayloadListValid(string value, char separator)
    {
        if (value.Length == 0)
        {
            return false;
        }
        string[] values = value.Split(separator);
        int index = 0;
        while (index < values.Length)
        {
            if (!IsIntegerInRange(values[index], 0, 127))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsEventListValid(string value)
    {
        bool[] events = new bool[256];
        return PopulateEventList(value, events);
    }

    internal static bool EventListsOverlap(string first, string second)
    {
        bool[] firstEvents = new bool[256];
        bool[] secondEvents = new bool[256];
        if (!PopulateEventList(first, firstEvents) || !PopulateEventList(second, secondEvents))
        {
            return false;
        }
        int index = 0;
        while (index < firstEvents.Length)
        {
            if (firstEvents[index] && secondEvents[index])
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool PopulateEventList(string value, bool[] events)
    {
        if (value.Length == 0)
        {
            return false;
        }
        string[] parts = value.Split(',');
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index];
            int dash = part.IndexOf('-');
            if (dash < 0)
            {
                if (!IsIntegerInRange(part, 0, 255))
                {
                    return false;
                }
                int eventCode = ReadSmallInteger(part);
                events[eventCode] = true;
            }
            else
            {
                string firstText = part.Substring(0, dash);
                string lastText = part.Substring(dash + 1);
                if (!IsIntegerInRange(firstText, 0, 255) || !IsIntegerInRange(lastText, 0, 255))
                {
                    return false;
                }
                int firstCode = ReadSmallInteger(firstText);
                int lastCode = ReadSmallInteger(lastText);
                if (firstCode >= lastCode)
                {
                    return false;
                }
                while (firstCode <= lastCode)
                {
                    events[firstCode] = true;
                    firstCode = firstCode + 1;
                }
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool RawPayloadListContains(string value, int payloadType)
    {
        string[] values = value.Split('/');
        int index = 0;
        while (index < values.Length)
        {
            if (IsIntegerInRange(values[index], 0, 127) && ReadSmallInteger(values[index]) == payloadType)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static string ReadRawCodecFormat(SdpCodec codec)
    {
        if (codec.FormatParameters.Count == 0)
        {
            if (IsTelephoneEventEncoding(codec.EncodingName))
            {
                return "0-15";
            }
            return string.Empty;
        }
        SdpFormatParametersAttribute format = codec.FormatParameters[0];
        if (format.RawParameters.Length > 0)
        {
            return format.RawParameters;
        }
        if (format.Parameters.Count == 1 && !format.Parameters[0].HasValue)
        {
            return format.Parameters[0].Name;
        }
        return string.Empty;
    }

    internal static bool ContainsParameter(List<SdpFormatParameter> parameters, string name)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (NameEquals(parameters[index].Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int ReadIntegerParameter(List<SdpFormatParameter> parameters, string name, int defaultValue)
    {
        string value = ReadStringParameter(parameters, name, string.Empty);
        if (value.Length == 0)
        {
            return defaultValue;
        }
        return ReadSmallInteger(value);
    }

    internal static long ReadLongParameter(List<SdpFormatParameter> parameters, string name, long defaultValue)
    {
        string value = ReadStringParameter(parameters, name, string.Empty);
        if (value.Length == 0)
        {
            return defaultValue;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        if (!number.Success || number.UnsignedValue > 9223372036854775807UL)
        {
            return defaultValue;
        }
        return (long)number.UnsignedValue;
    }

    internal static string ReadStringParameter(List<SdpFormatParameter> parameters, string name, string defaultValue)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (NameEquals(parameters[index].Name, name))
            {
                return parameters[index].Value;
            }
            index = index + 1;
        }
        return defaultValue;
    }

    internal static int ReadSmallInteger(string value)
    {
        return (int)SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan()).UnsignedValue;
    }

    internal static bool IsIntegerInRange(string value, int minimum, int maximum)
    {
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return number.Success && number.UnsignedValue >= (ulong)minimum && number.UnsignedValue <= (ulong)maximum;
    }

    internal static bool IsPositiveInteger(string value)
    {
        return SdpOpusCodecCore.IsPositiveInteger(value);
    }

    internal static bool IsNonnegativeInteger(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return number.Success && number.UnsignedValue <= 9223372036854775807UL;
    }

    internal static bool IsHexValue(string value, int requiredLength)
    {
        if (value.Length != requiredLength)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (SdpH264CodecCore.ReadHexDigit(value[index]) < 0)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
