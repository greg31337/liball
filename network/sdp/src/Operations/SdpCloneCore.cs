using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpCloneCore
{
    internal static SdpSessionDescription CopyDescription(SdpSessionDescription source)
    {
        if (source.Emails == null || source.Phones == null || source.Bandwidths == null || source.Timings == null || source.TimeZoneAdjustments == null || source.Attributes == null || source.MediaDescriptions == null || source.RawLines == null)
        {
            throw new ArgumentException("Every session-description collection must be initialized.", "description");
        }
        SdpSessionDescription result = new SdpSessionDescription();
        result.HasProtocolVersion = source.HasProtocolVersion;
        result.ProtocolVersion = source.ProtocolVersion;
        result.Origin = source.Origin == null ? null : CopyOrigin(source.Origin);
        result.HasSessionName = source.HasSessionName;
        result.SessionName = source.SessionName;
        result.Information = source.Information;
        result.Uri = source.Uri;
        CopyStringList(source.Emails, result.Emails);
        CopyStringList(source.Phones, result.Phones);
        result.Connection = source.Connection == null ? null : CopyConnection(source.Connection);
        CopyBandwidthList(source.Bandwidths, result.Bandwidths);
        CopyTimingList(source.Timings, result.Timings);
        CopyZoneList(source.TimeZoneAdjustments, result.TimeZoneAdjustments);
        result.EncryptionKey = source.EncryptionKey == null ? null : CopyEncryptionKey(source.EncryptionKey);
        CopyAttributeList(source.Attributes, result.Attributes);
        CopyMediaList(source.MediaDescriptions, result.MediaDescriptions);
        CopyRawLineList(source.RawLines, result.RawLines);
        result.LineEnding = source.LineEnding;
        return result;
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
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
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
        result.RawAddress = source.RawAddress;
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpBandwidth CopyBandwidth(SdpBandwidth source)
    {
        SdpBandwidth result = new SdpBandwidth();
        result.Type = source.Type;
        result.Value = source.Value;
        result.RawValue = source.RawValue;
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpTiming CopyTiming(SdpTiming source)
    {
        if (source.RepeatTimes == null)
        {
            throw new ArgumentException("Every timing recurrence collection must be initialized.", "description");
        }
        SdpTiming result = new SdpTiming();
        result.StartTime = source.StartTime;
        result.StopTime = source.StopTime;
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        int index = 0;
        while (index < source.RepeatTimes.Count)
        {
            SdpRepeatTime repeat = source.RepeatTimes[index];
            if (repeat == null)
            {
                throw new ArgumentException("Every timing recurrence must be initialized.", "description");
            }
            result.RepeatTimes.Add(CopyRepeatTime(repeat));
            index = index + 1;
        }
        return result;
    }

    internal static SdpRepeatTime CopyRepeatTime(SdpRepeatTime source)
    {
        if (source.RepeatInterval == null || source.ActiveDuration == null || source.Offsets == null)
        {
            throw new ArgumentException("Every recurrence value and offset collection must be initialized.", "description");
        }
        SdpRepeatTime result = new SdpRepeatTime();
        result.RepeatInterval = CopyTypedTime(source.RepeatInterval);
        result.ActiveDuration = CopyTypedTime(source.ActiveDuration);
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        int index = 0;
        while (index < source.Offsets.Count)
        {
            SdpTypedTime offset = source.Offsets[index];
            if (offset == null)
            {
                throw new ArgumentException("Every recurrence offset must be initialized.", "description");
            }
            result.Offsets.Add(CopyTypedTime(offset));
            index = index + 1;
        }
        return result;
    }

    internal static SdpTypedTime CopyTypedTime(SdpTypedTime source)
    {
        SdpTypedTime result = new SdpTypedTime();
        result.Value = source.Value;
        result.Unit = source.Unit;
        result.RawValue = source.RawValue;
        return result;
    }

    internal static SdpTimeZoneAdjustment CopyZone(SdpTimeZoneAdjustment source)
    {
        if (source.Offset == null)
        {
            throw new ArgumentException("Every time-zone offset must be initialized.", "description");
        }
        SdpTimeZoneAdjustment result = new SdpTimeZoneAdjustment();
        result.AdjustmentTime = source.AdjustmentTime;
        result.Offset = CopyTypedTime(source.Offset);
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpEncryptionKey CopyEncryptionKey(SdpEncryptionKey source)
    {
        SdpEncryptionKey result = new SdpEncryptionKey();
        result.Method = source.Method;
        result.HasValue = source.HasValue;
        result.Value = source.Value;
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpAttribute CopyAttribute(SdpAttribute source)
    {
        SdpAttribute result = new SdpAttribute();
        result.Kind = source.Kind;
        result.Name = source.Name;
        result.Value = source.Value;
        result.RawText = source.RawText;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpMediaDescription CopyMedia(SdpMediaDescription source)
    {
        if (source.Formats == null || source.Connections == null || source.Bandwidths == null || source.Attributes == null || source.RawLines == null)
        {
            throw new ArgumentException("Every media-description collection must be initialized.", "description");
        }
        SdpMediaDescription result = new SdpMediaDescription();
        result.MediaType = source.MediaType;
        result.MediaKind = source.MediaKind;
        result.Port = source.Port;
        result.HasPortCount = source.HasPortCount;
        result.PortCount = source.PortCount;
        result.Protocol = source.Protocol;
        CopyStringList(source.Formats, result.Formats);
        result.Information = source.Information;
        CopyConnectionList(source.Connections, result.Connections);
        CopyBandwidthList(source.Bandwidths, result.Bandwidths);
        result.EncryptionKey = source.EncryptionKey == null ? null : CopyEncryptionKey(source.EncryptionKey);
        CopyAttributeList(source.Attributes, result.Attributes);
        CopyRawLineList(source.RawLines, result.RawLines);
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpRawLine CopyRawLine(SdpRawLine source)
    {
        SdpRawLine result = new SdpRawLine();
        result.FieldName = source.FieldName;
        result.Text = source.Text;
        result.Value = source.Value;
        result.LineEnding = source.LineEnding;
        result.IsMalformed = source.IsMalformed;
        result.Location = CopyLocation(source.Location);
        return result;
    }

    internal static SdpSourceLocation CopyLocation(SdpSourceLocation source)
    {
        if (source == null)
        {
            throw new ArgumentException("Every source location must be initialized.", "description");
        }
        SdpSourceLocation result = new SdpSourceLocation();
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.LineNumber = source.LineNumber;
        result.ColumnNumber = source.ColumnNumber;
        return result;
    }

    internal static void CopyStringList(List<string> source, List<string> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CopyConnectionList(List<SdpConnectionData> source, List<SdpConnectionData> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpConnectionData connection = source[index];
            if (connection == null)
            {
                throw new ArgumentException("Every connection entry must be initialized.", "description");
            }
            destination.Add(CopyConnection(connection));
            index = index + 1;
        }
    }

    internal static void CopyBandwidthList(List<SdpBandwidth> source, List<SdpBandwidth> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpBandwidth bandwidth = source[index];
            if (bandwidth == null)
            {
                throw new ArgumentException("Every bandwidth entry must be initialized.", "description");
            }
            destination.Add(CopyBandwidth(bandwidth));
            index = index + 1;
        }
    }

    internal static void CopyTimingList(List<SdpTiming> source, List<SdpTiming> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpTiming timing = source[index];
            if (timing == null)
            {
                throw new ArgumentException("Every timing entry must be initialized.", "description");
            }
            destination.Add(CopyTiming(timing));
            index = index + 1;
        }
    }

    internal static void CopyZoneList(List<SdpTimeZoneAdjustment> source, List<SdpTimeZoneAdjustment> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpTimeZoneAdjustment zone = source[index];
            if (zone == null)
            {
                throw new ArgumentException("Every time-zone adjustment must be initialized.", "description");
            }
            destination.Add(CopyZone(zone));
            index = index + 1;
        }
    }

    internal static void CopyAttributeList(List<SdpAttribute> source, List<SdpAttribute> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpAttribute attribute = source[index];
            if (attribute == null)
            {
                throw new ArgumentException("Every attribute entry must be initialized.", "description");
            }
            destination.Add(CopyAttribute(attribute));
            index = index + 1;
        }
    }

    internal static void CopyMediaList(List<SdpMediaDescription> source, List<SdpMediaDescription> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpMediaDescription media = source[index];
            if (media == null)
            {
                throw new ArgumentException("Every media-description entry must be initialized.", "description");
            }
            destination.Add(CopyMedia(media));
            index = index + 1;
        }
    }

    internal static void CopyRawLineList(List<SdpRawLine> source, List<SdpRawLine> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            SdpRawLine line = source[index];
            if (line == null)
            {
                throw new ArgumentException("Every retained source line must be initialized.", "description");
            }
            destination.Add(CopyRawLine(line));
            index = index + 1;
        }
    }
}
