using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpSessionFactoryCore
{
    internal static SdpSessionDescription CreateSession(SdpOrigin origin, string sessionName, SdpConnectionData connection, SdpTiming timing)
    {
        ValidateOrigin(origin);
        ValidateSessionName(sessionName);
        ValidateConnection(connection);
        ValidateTiming(timing);
        SdpSessionDescription result = new SdpSessionDescription();
        result.HasProtocolVersion = true;
        result.ProtocolVersion = 0;
        result.Origin = CopyOrigin(origin);
        result.HasSessionName = true;
        result.SessionName = sessionName;
        result.Connection = CopyConnection(connection);
        result.Timings.Add(CopyTiming(timing));
        return result;
    }

    internal static SdpMediaDescription CreateMedia(string mediaType, int port, string protocol, List<string> formats)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(mediaType))
        {
            throw new ArgumentException("The media type must use nonempty token syntax.", "mediaType");
        }
        if (!SdpCoreValidationCore.IsValidProtocolToken(protocol))
        {
            throw new ArgumentException("The media transport protocol must contain nonempty slash-delimited tokens.", "protocol");
        }
        ValidateFormats(formats);
        SdpMediaDescription result = new SdpMediaDescription();
        result.MediaType = mediaType;
        result.MediaKind = SdpMediaLineParser.ReadMediaKind(mediaType);
        result.Port = port;
        result.HasPortCount = false;
        result.PortCount = 1;
        result.Protocol = protocol;
        int index = 0;
        while (index < formats.Count)
        {
            result.Formats.Add(formats[index]);
            index = index + 1;
        }
        return result;
    }

    internal static void ValidateOrigin(SdpOrigin origin)
    {
        bool identifiersValid = origin.SessionId != null && origin.SessionVersion != null && SdpNumberParser.ParseCharacterUnsignedDecimal(origin.SessionId.AsSpan()).Success && SdpNumberParser.ParseCharacterUnsignedDecimal(origin.SessionVersion.AsSpan()).Success;
        bool tokensValid = origin.Username != null && origin.NetworkType != null && origin.AddressType != null && SdpCommonAttributeCodecCore.IsNonWhitespaceValue(origin.Username) && SdpCommonAttributeCodecCore.IsTokenValue(origin.NetworkType) && SdpCommonAttributeCodecCore.IsTokenValue(origin.AddressType);
        if (!identifiersValid || !tokensValid || !SdpCoreValidationCore.IsValidOriginAddress(origin))
        {
            throw new ArgumentException("The origin must contain valid identity, version, network, and unicast-address values.", "origin");
        }
    }

    internal static void ValidateSessionName(string sessionName)
    {
        if (sessionName.Length == 0 || !SdpCommonAttributeCodecCore.IsByteStringValue(sessionName))
        {
            throw new ArgumentException("The session name must be nonempty and contain no NUL or line-ending characters.", "sessionName");
        }
    }

    internal static void ValidateConnection(SdpConnectionData connection)
    {
        bool tokensValid = connection.NetworkType != null && connection.AddressType != null && SdpCommonAttributeCodecCore.IsTokenValue(connection.NetworkType) && SdpCommonAttributeCodecCore.IsTokenValue(connection.AddressType);
        if (!tokensValid || !SdpCoreValidationCore.IsValidConnectionAddress(connection))
        {
            throw new ArgumentException("The connection must contain a valid network type, address type, address, and multicast suffix state.", "connection");
        }
    }

    internal static void ValidateTiming(SdpTiming timing)
    {
        string startTime = timing.StartTime == null ? string.Empty : timing.StartTime;
        string stopTime = timing.StopTime == null ? string.Empty : timing.StopTime;
        bool valid = SdpCoreValidationCore.IsValidRfcTime(startTime, true) && SdpCoreValidationCore.IsValidRfcTime(stopTime, true);
        if (valid && stopTime != "0")
        {
            valid = SdpCoreValidationCore.CompareRfcTimes(stopTime, startTime) > 0;
        }
        if (!valid || timing.RepeatTimes == null)
        {
            throw new ArgumentException("The timing period must contain valid RFC time values and an initialized recurrence collection.", "timing");
        }
        int repeatIndex = 0;
        while (repeatIndex < timing.RepeatTimes.Count)
        {
            if (!IsValidRepeatTime(timing.RepeatTimes[repeatIndex]))
            {
                throw new ArgumentException("Every timing recurrence must contain positive interval and duration values plus nonnegative offsets.", "timing");
            }
            repeatIndex = repeatIndex + 1;
        }
    }

    internal static bool IsValidRepeatTime(SdpRepeatTime repeat)
    {
        if (repeat == null || repeat.Offsets == null || repeat.Offsets.Count == 0 || !IsValidTypedTime(repeat.RepeatInterval, true) || !IsValidTypedTime(repeat.ActiveDuration, true))
        {
            return false;
        }
        int offsetIndex = 0;
        while (offsetIndex < repeat.Offsets.Count)
        {
            if (!IsValidTypedTime(repeat.Offsets[offsetIndex], false))
            {
                return false;
            }
            offsetIndex = offsetIndex + 1;
        }
        return true;
    }

    internal static bool IsValidTypedTime(SdpTypedTime value, bool requirePositive)
    {
        if (value == null || requirePositive && value.Value <= 0 || !requirePositive && value.Value < 0)
        {
            return false;
        }
        return SdpTimingServiceCore.NormalizeTypedTime(value).Success;
    }

    internal static void ValidateFormats(List<string> formats)
    {
        if (formats.Count == 0)
        {
            throw new ArgumentException("At least one media format token is required.", "formats");
        }
        int index = 0;
        while (index < formats.Count)
        {
            string format = formats[index];
            if (format == null || !SdpCommonAttributeCodecCore.IsTokenValue(format))
            {
                throw new ArgumentException("Every media format must use nonempty token syntax.", "formats");
            }
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (formats[earlierIndex] == format)
                {
                    throw new ArgumentException("Media format tokens must be unique.", "formats");
                }
                earlierIndex = earlierIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static SdpOrigin CopyOrigin(SdpOrigin source)
    {
        SdpOrigin result = new SdpOrigin();
        result.Username = source.Username;
        result.SessionId = source.SessionId;
        result.SessionVersion = source.SessionVersion;
        result.NetworkType = source.NetworkType;
        result.AddressType = source.AddressType;
        result.UnicastAddress = source.UnicastAddress;
        result.AddressKind = source.AddressKind;
        return result;
    }

    internal static SdpConnectionData CopyConnection(SdpConnectionData source)
    {
        SdpConnectionData result = new SdpConnectionData();
        result.NetworkType = source.NetworkType;
        result.AddressType = source.AddressType;
        result.Address = source.Address;
        result.AddressKind = source.AddressKind;
        result.HasTimeToLive = source.HasTimeToLive;
        result.TimeToLive = source.TimeToLive;
        result.HasAddressCount = source.HasAddressCount;
        result.AddressCount = source.AddressCount;
        return result;
    }

    internal static SdpTiming CopyTiming(SdpTiming source)
    {
        SdpTiming result = new SdpTiming();
        result.StartTime = source.StartTime;
        result.StopTime = source.StopTime;
        int repeatIndex = 0;
        while (repeatIndex < source.RepeatTimes.Count)
        {
            result.RepeatTimes.Add(CopyRepeatTime(source.RepeatTimes[repeatIndex]));
            repeatIndex = repeatIndex + 1;
        }
        return result;
    }

    internal static SdpRepeatTime CopyRepeatTime(SdpRepeatTime source)
    {
        SdpRepeatTime result = new SdpRepeatTime();
        result.RepeatInterval = CopyTypedTime(source.RepeatInterval);
        result.ActiveDuration = CopyTypedTime(source.ActiveDuration);
        int offsetIndex = 0;
        while (offsetIndex < source.Offsets.Count)
        {
            result.Offsets.Add(CopyTypedTime(source.Offsets[offsetIndex]));
            offsetIndex = offsetIndex + 1;
        }
        return result;
    }

    internal static SdpTypedTime CopyTypedTime(SdpTypedTime source)
    {
        SdpTypedTime result = new SdpTypedTime();
        result.Value = source.Value;
        result.Unit = source.Unit;
        return result;
    }
}
