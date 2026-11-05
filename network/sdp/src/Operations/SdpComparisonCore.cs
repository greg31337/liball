using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpComparisonCore
{
    internal static SdpComparisonResult CompareDescriptions(SdpSessionDescription originalDescription, SdpSessionDescription comparedDescription, SdpComparisonOptions options)
    {
        SdpComparisonResult result = new SdpComparisonResult();
        result.AppliedOptions = CopyComparisonOptions(options);
        SdpSessionDescription preparedOriginal = SdpSemanticComparisonCore.PrepareDescription(originalDescription, options);
        SdpSessionDescription preparedCompared = SdpSemanticComparisonCore.PrepareDescription(comparedDescription, options);
        CompareSessionFields(preparedOriginal, preparedCompared, result);
        result.Equal = result.Differences.Count == 0;
        return result;
    }

    internal static SdpComparisonOptions CopyComparisonOptions(SdpComparisonOptions options)
    {
        if (options.IgnoredAttributeNames == null)
        {
            throw new ArgumentException("The ignored-attribute collection must be initialized.", "options");
        }
        SdpComparisonOptions result = new SdpComparisonOptions();
        result.CompareSemanticValues = options.CompareSemanticValues;
        result.IgnoreOriginSessionVersion = options.IgnoreOriginSessionVersion;
        result.IgnoreInsignificantOrdering = options.IgnoreInsignificantOrdering;
        result.IgnoreRawFormatting = options.IgnoreRawFormatting;
        int index = 0;
        while (index < options.IgnoredAttributeNames.Count)
        {
            result.IgnoredAttributeNames.Add(options.IgnoredAttributeNames[index]);
            index = index + 1;
        }
        return result;
    }

    internal static void CompareSessionFields(SdpSessionDescription originalDescription, SdpSessionDescription comparedDescription, SdpComparisonResult result)
    {
        SdpSourcePath versionPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'v', string.Empty, 0, -1);
        AddScalarDifference(result, versionPath, "The protocol-version presence differs.", ReadBooleanValue(originalDescription.HasProtocolVersion), ReadBooleanValue(comparedDescription.HasProtocolVersion), false);
        AddScalarDifference(result, versionPath, "The protocol version differs.", ReadInt32Value(originalDescription.ProtocolVersion), ReadInt32Value(comparedDescription.ProtocolVersion), false);
        CompareOriginField(originalDescription.Origin, comparedDescription.Origin, result);
        SdpSourcePath namePath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 's', string.Empty, 0, -1);
        AddScalarDifference(result, namePath, "The session-name presence differs.", ReadBooleanValue(originalDescription.HasSessionName), ReadBooleanValue(comparedDescription.HasSessionName), false);
        AddScalarDifference(result, namePath, "The session name differs.", ReadStringValue(originalDescription.SessionName), ReadStringValue(comparedDescription.SessionName), false);
        SdpSourcePath informationPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'i', string.Empty, 0, -1);
        AddScalarDifference(result, informationPath, "The session information differs.", ReadStringValue(originalDescription.Information), ReadStringValue(comparedDescription.Information), false);
        SdpSourcePath uriPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'u', string.Empty, 0, -1);
        AddScalarDifference(result, uriPath, "The session URI differs.", ReadStringValue(originalDescription.Uri), ReadStringValue(comparedDescription.Uri), false);
        CompareStringCollection(originalDescription.Emails, comparedDescription.Emails, result, SdpScope.Session, -1, string.Empty, 'e', "email");
        CompareStringCollection(originalDescription.Phones, comparedDescription.Phones, result, SdpScope.Session, -1, string.Empty, 'p', "phone");
        CompareConnectionField(originalDescription.Connection, comparedDescription.Connection, result, SdpScope.Session, -1, string.Empty, 0);
        CompareBandwidthFields(originalDescription.Bandwidths, comparedDescription.Bandwidths, result, SdpScope.Session, -1, string.Empty);
        CompareTimingFields(originalDescription.Timings, comparedDescription.Timings, result);
        CompareZoneFields(originalDescription.TimeZoneAdjustments, comparedDescription.TimeZoneAdjustments, result);
        CompareEncryptionKeyField(originalDescription.EncryptionKey, comparedDescription.EncryptionKey, result, SdpScope.Session, -1, string.Empty);
        CompareAttributeFields(originalDescription.Attributes, comparedDescription.Attributes, null, null, result, SdpScope.Session, -1, string.Empty);
        CompareMediaFields(originalDescription.MediaDescriptions, comparedDescription.MediaDescriptions, result);
        CompareRawLineFields(originalDescription.RawLines, comparedDescription.RawLines, result, SdpScope.Session, -1, string.Empty);
        SdpSourcePath lineEndingPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, '\0', string.Empty, -1, -1);
        AddScalarDifference(result, lineEndingPath, "The preferred line ending differs.", ReadInt32Value((int)originalDescription.LineEnding), ReadInt32Value((int)comparedDescription.LineEnding), false);
    }

    internal static void CompareOriginField(SdpOrigin? originalOrigin, SdpOrigin? comparedOrigin, SdpComparisonResult result)
    {
        SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'o', string.Empty, 0, -1);
        if (originalOrigin == null || comparedOrigin == null)
        {
            AddOptionalDifference(result, path, "The origin field", originalOrigin != null, comparedOrigin != null, false);
            return;
        }
        AddScalarDifference(result, path, "The origin username differs.", ReadStringValue(originalOrigin.Username), ReadStringValue(comparedOrigin.Username), false);
        AddScalarDifference(result, path, "The origin session identifier differs.", ReadStringValue(originalOrigin.SessionId), ReadStringValue(comparedOrigin.SessionId), false);
        AddScalarDifference(result, path, "The origin session version differs.", ReadStringValue(originalOrigin.SessionVersion), ReadStringValue(comparedOrigin.SessionVersion), false);
        AddScalarDifference(result, path, "The origin network type differs.", ReadStringValue(originalOrigin.NetworkType), ReadStringValue(comparedOrigin.NetworkType), false);
        AddScalarDifference(result, path, "The origin address type differs.", ReadStringValue(originalOrigin.AddressType), ReadStringValue(comparedOrigin.AddressType), false);
        AddScalarDifference(result, path, "The origin address differs.", ReadStringValue(originalOrigin.UnicastAddress), ReadStringValue(comparedOrigin.UnicastAddress), false);
        AddScalarDifference(result, path, "The origin address kind differs.", ReadInt32Value((int)originalOrigin.AddressKind), ReadInt32Value((int)comparedOrigin.AddressKind), false);
        AddScalarDifference(result, path, "The retained origin text differs.", ReadStringValue(originalOrigin.RawText), ReadStringValue(comparedOrigin.RawText), false);
        CompareLocationFields(originalOrigin.Location, comparedOrigin.Location, result, path, "origin");
    }

    internal static void CompareConnectionField(SdpConnectionData? originalConnection, SdpConnectionData? comparedConnection, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId, int occurrenceIndex)
    {
        SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'c', string.Empty, occurrenceIndex, -1);
        if (originalConnection == null || comparedConnection == null)
        {
            AddOptionalDifference(result, path, "The connection field", originalConnection != null, comparedConnection != null, false);
            return;
        }
        AddScalarDifference(result, path, "The connection network type differs.", ReadStringValue(originalConnection.NetworkType), ReadStringValue(comparedConnection.NetworkType), false);
        AddScalarDifference(result, path, "The connection address type differs.", ReadStringValue(originalConnection.AddressType), ReadStringValue(comparedConnection.AddressType), false);
        AddScalarDifference(result, path, "The connection address differs.", ReadStringValue(originalConnection.Address), ReadStringValue(comparedConnection.Address), false);
        AddScalarDifference(result, path, "The connection address kind differs.", ReadInt32Value((int)originalConnection.AddressKind), ReadInt32Value((int)comparedConnection.AddressKind), false);
        AddScalarDifference(result, path, "The connection time-to-live presence differs.", ReadBooleanValue(originalConnection.HasTimeToLive), ReadBooleanValue(comparedConnection.HasTimeToLive), false);
        AddScalarDifference(result, path, "The connection time to live differs.", ReadInt32Value(originalConnection.TimeToLive), ReadInt32Value(comparedConnection.TimeToLive), false);
        AddScalarDifference(result, path, "The connection address-count presence differs.", ReadBooleanValue(originalConnection.HasAddressCount), ReadBooleanValue(comparedConnection.HasAddressCount), false);
        AddScalarDifference(result, path, "The connection address count differs.", ReadInt32Value(originalConnection.AddressCount), ReadInt32Value(comparedConnection.AddressCount), false);
        AddScalarDifference(result, path, "The retained connection address differs.", ReadStringValue(originalConnection.RawAddress), ReadStringValue(comparedConnection.RawAddress), false);
        AddScalarDifference(result, path, "The retained connection text differs.", ReadStringValue(originalConnection.RawText), ReadStringValue(comparedConnection.RawText), false);
        CompareLocationFields(originalConnection.Location, comparedConnection.Location, result, path, "connection");
    }

    internal static void CompareConnectionFields(List<SdpConnectionData>? originalConnections, List<SdpConnectionData>? comparedConnections, SdpComparisonResult result, int mediaIndex, string mediaId)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Media, mediaIndex, mediaId, 'c', string.Empty, -1, -1);
        if (originalConnections == null || comparedConnections == null)
        {
            AddOptionalDifference(result, collectionPath, "The media connection collection", originalConnections != null, comparedConnections != null, false);
            return;
        }
        int sharedCount = originalConnections.Count < comparedConnections.Count ? originalConnections.Count : comparedConnections.Count;
        int index = 0;
        while (index < sharedCount)
        {
            CompareConnectionField(originalConnections[index], comparedConnections[index], result, SdpScope.Media, mediaIndex, mediaId, index);
            index = index + 1;
        }
        while (index < originalConnections.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Media, mediaIndex, mediaId, 'c', string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The connection field", true, false, false);
            index = index + 1;
        }
        while (index < comparedConnections.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Media, mediaIndex, mediaId, 'c', string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The connection field", false, true, false);
            index = index + 1;
        }
    }

    internal static void CompareBandwidthFields(List<SdpBandwidth>? originalBandwidths, List<SdpBandwidth>? comparedBandwidths, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(scope, mediaIndex, mediaId, 'b', string.Empty, -1, -1);
        if (originalBandwidths == null || comparedBandwidths == null)
        {
            AddOptionalDifference(result, collectionPath, "The bandwidth collection", originalBandwidths != null, comparedBandwidths != null, false);
            return;
        }
        int sharedCount = originalBandwidths.Count < comparedBandwidths.Count ? originalBandwidths.Count : comparedBandwidths.Count;
        int index = 0;
        while (index < sharedCount)
        {
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'b', string.Empty, index, -1);
            SdpBandwidth? originalBandwidth = originalBandwidths[index];
            SdpBandwidth? comparedBandwidth = comparedBandwidths[index];
            if (originalBandwidth == null || comparedBandwidth == null)
            {
                AddOptionalDifference(result, path, "The bandwidth field", originalBandwidth != null, comparedBandwidth != null, false);
            }
            else
            {
                AddScalarDifference(result, path, "The bandwidth type differs.", ReadStringValue(originalBandwidth.Type), ReadStringValue(comparedBandwidth.Type), false);
                AddScalarDifference(result, path, "The bandwidth value differs.", ReadInt64Value(originalBandwidth.Value), ReadInt64Value(comparedBandwidth.Value), false);
                AddScalarDifference(result, path, "The retained bandwidth value differs.", ReadStringValue(originalBandwidth.RawValue), ReadStringValue(comparedBandwidth.RawValue), false);
                AddScalarDifference(result, path, "The retained bandwidth text differs.", ReadStringValue(originalBandwidth.RawText), ReadStringValue(comparedBandwidth.RawText), false);
                CompareLocationFields(originalBandwidth.Location, comparedBandwidth.Location, result, path, "bandwidth");
            }
            index = index + 1;
        }
        while (index < originalBandwidths.Count)
        {
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'b', string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The bandwidth field", true, false, false);
            index = index + 1;
        }
        while (index < comparedBandwidths.Count)
        {
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'b', string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The bandwidth field", false, true, false);
            index = index + 1;
        }
    }

    internal static void CompareTimingFields(List<SdpTiming>? originalTimings, List<SdpTiming>? comparedTimings, SdpComparisonResult result)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 't', string.Empty, -1, -1);
        if (originalTimings == null || comparedTimings == null)
        {
            AddOptionalDifference(result, collectionPath, "The timing collection", originalTimings != null, comparedTimings != null, false);
            return;
        }
        int sharedCount = originalTimings.Count < comparedTimings.Count ? originalTimings.Count : comparedTimings.Count;
        int timingIndex = 0;
        while (timingIndex < sharedCount)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 't', string.Empty, timingIndex, -1);
            SdpTiming? originalTiming = originalTimings[timingIndex];
            SdpTiming? comparedTiming = comparedTimings[timingIndex];
            if (originalTiming == null || comparedTiming == null)
            {
                AddOptionalDifference(result, path, "The timing field", originalTiming != null, comparedTiming != null, false);
            }
            else
            {
                AddScalarDifference(result, path, "The timing start value differs.", ReadStringValue(originalTiming.StartTime), ReadStringValue(comparedTiming.StartTime), false);
                AddScalarDifference(result, path, "The timing stop value differs.", ReadStringValue(originalTiming.StopTime), ReadStringValue(comparedTiming.StopTime), false);
                AddScalarDifference(result, path, "The retained timing text differs.", ReadStringValue(originalTiming.RawText), ReadStringValue(comparedTiming.RawText), false);
                CompareLocationFields(originalTiming.Location, comparedTiming.Location, result, path, "timing");
                CompareRepeatFields(originalTiming.RepeatTimes, comparedTiming.RepeatTimes, result, originalTimings, comparedTimings, timingIndex);
            }
            timingIndex = timingIndex + 1;
        }
        while (timingIndex < originalTimings.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 't', string.Empty, timingIndex, -1);
            AddCollectionElementDifference(result, path, "The timing field", true, false, false);
            timingIndex = timingIndex + 1;
        }
        while (timingIndex < comparedTimings.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 't', string.Empty, timingIndex, -1);
            AddCollectionElementDifference(result, path, "The timing field", false, true, false);
            timingIndex = timingIndex + 1;
        }
    }

    internal static void CompareRepeatFields(List<SdpRepeatTime>? originalRepeats, List<SdpRepeatTime>? comparedRepeats, SdpComparisonResult result, List<SdpTiming> originalTimings, List<SdpTiming> comparedTimings, int timingIndex)
    {
        int originalBase = ReadRepeatBaseIndex(originalTimings, timingIndex);
        int comparedBase = ReadRepeatBaseIndex(comparedTimings, timingIndex);
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, originalBase, -1);
        if (originalRepeats == null || comparedRepeats == null)
        {
            AddOptionalDifference(result, collectionPath, "The repeat-time collection", originalRepeats != null, comparedRepeats != null, false);
            return;
        }
        int sharedCount = originalRepeats.Count < comparedRepeats.Count ? originalRepeats.Count : comparedRepeats.Count;
        int repeatIndex = 0;
        while (repeatIndex < sharedCount)
        {
            int occurrenceIndex = originalBase + repeatIndex;
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, occurrenceIndex, -1);
            SdpRepeatTime? originalRepeat = originalRepeats[repeatIndex];
            SdpRepeatTime? comparedRepeat = comparedRepeats[repeatIndex];
            if (originalRepeat == null || comparedRepeat == null)
            {
                AddOptionalDifference(result, path, "The repeat-time field", originalRepeat != null, comparedRepeat != null, false);
            }
            else
            {
                CompareTypedTimeField(originalRepeat.RepeatInterval, comparedRepeat.RepeatInterval, result, occurrenceIndex, 0, "repeat interval");
                CompareTypedTimeField(originalRepeat.ActiveDuration, comparedRepeat.ActiveDuration, result, occurrenceIndex, 1, "active duration");
                CompareTypedTimeFields(originalRepeat.Offsets, comparedRepeat.Offsets, result, occurrenceIndex);
                AddScalarDifference(result, path, "The retained repeat-time text differs.", ReadStringValue(originalRepeat.RawText), ReadStringValue(comparedRepeat.RawText), false);
                CompareLocationFields(originalRepeat.Location, comparedRepeat.Location, result, path, "repeat-time");
            }
            repeatIndex = repeatIndex + 1;
        }
        while (repeatIndex < originalRepeats.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, originalBase + repeatIndex, -1);
            AddCollectionElementDifference(result, path, "The repeat-time field", true, false, false);
            repeatIndex = repeatIndex + 1;
        }
        while (repeatIndex < comparedRepeats.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, comparedBase + repeatIndex, -1);
            AddCollectionElementDifference(result, path, "The repeat-time field", false, true, false);
            repeatIndex = repeatIndex + 1;
        }
    }

    internal static int ReadRepeatBaseIndex(List<SdpTiming> timings, int timingIndex)
    {
        int result = 0;
        int index = 0;
        while (index < timingIndex && index < timings.Count)
        {
            SdpTiming? timing = timings[index];
            if (timing != null && timing.RepeatTimes != null)
            {
                result = result + timing.RepeatTimes.Count;
            }
            index = index + 1;
        }
        return result;
    }

    internal static void CompareTypedTimeFields(List<SdpTypedTime>? originalOffsets, List<SdpTypedTime>? comparedOffsets, SdpComparisonResult result, int occurrenceIndex)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, occurrenceIndex, 2);
        if (originalOffsets == null || comparedOffsets == null)
        {
            AddOptionalDifference(result, collectionPath, "The repeat-offset collection", originalOffsets != null, comparedOffsets != null, false);
            return;
        }
        int sharedCount = originalOffsets.Count < comparedOffsets.Count ? originalOffsets.Count : comparedOffsets.Count;
        int offsetIndex = 0;
        while (offsetIndex < sharedCount)
        {
            CompareTypedTimeField(originalOffsets[offsetIndex], comparedOffsets[offsetIndex], result, occurrenceIndex, offsetIndex + 2, "repeat offset");
            offsetIndex = offsetIndex + 1;
        }
        while (offsetIndex < originalOffsets.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, occurrenceIndex, offsetIndex + 2);
            AddCollectionElementDifference(result, path, "The repeat offset", true, false, false);
            offsetIndex = offsetIndex + 1;
        }
        while (offsetIndex < comparedOffsets.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, occurrenceIndex, offsetIndex + 2);
            AddCollectionElementDifference(result, path, "The repeat offset", false, true, false);
            offsetIndex = offsetIndex + 1;
        }
    }

    internal static void CompareTypedTimeField(SdpTypedTime? originalValue, SdpTypedTime? comparedValue, SdpComparisonResult result, int occurrenceIndex, int valueIndex, string componentName)
    {
        SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'r', string.Empty, occurrenceIndex, valueIndex);
        if (originalValue == null || comparedValue == null)
        {
            AddOptionalDifference(result, path, "The " + componentName, originalValue != null, comparedValue != null, false);
            return;
        }
        AddScalarDifference(result, path, "The " + componentName + " numeric value differs.", ReadInt64Value(originalValue.Value), ReadInt64Value(comparedValue.Value), false);
        AddScalarDifference(result, path, "The " + componentName + " unit differs.", ReadStringValue(originalValue.Unit), ReadStringValue(comparedValue.Unit), false);
        AddScalarDifference(result, path, "The retained " + componentName + " text differs.", ReadStringValue(originalValue.RawValue), ReadStringValue(comparedValue.RawValue), false);
    }

    internal static void CompareZoneFields(List<SdpTimeZoneAdjustment>? originalZones, List<SdpTimeZoneAdjustment>? comparedZones, SdpComparisonResult result)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'z', string.Empty, 0, -1);
        if (originalZones == null || comparedZones == null)
        {
            AddOptionalDifference(result, collectionPath, "The time-zone collection", originalZones != null, comparedZones != null, false);
            return;
        }
        int sharedCount = originalZones.Count < comparedZones.Count ? originalZones.Count : comparedZones.Count;
        int index = 0;
        while (index < sharedCount)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'z', string.Empty, 0, index);
            SdpTimeZoneAdjustment? originalZone = originalZones[index];
            SdpTimeZoneAdjustment? comparedZone = comparedZones[index];
            if (originalZone == null || comparedZone == null)
            {
                AddOptionalDifference(result, path, "The time-zone adjustment", originalZone != null, comparedZone != null, false);
            }
            else
            {
                AddScalarDifference(result, path, "The time-zone adjustment time differs.", ReadStringValue(originalZone.AdjustmentTime), ReadStringValue(comparedZone.AdjustmentTime), false);
                CompareZoneOffsetField(originalZone.Offset, comparedZone.Offset, result, path);
                CompareLocationFields(originalZone.Location, comparedZone.Location, result, path, "time-zone adjustment");
            }
            index = index + 1;
        }
        while (index < originalZones.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'z', string.Empty, 0, index);
            AddCollectionElementDifference(result, path, "The time-zone adjustment", true, false, false);
            index = index + 1;
        }
        while (index < comparedZones.Count)
        {
            SdpSourcePath path = CreateComparisonPath(SdpScope.Session, -1, string.Empty, 'z', string.Empty, 0, index);
            AddCollectionElementDifference(result, path, "The time-zone adjustment", false, true, false);
            index = index + 1;
        }
    }

    internal static void CompareZoneOffsetField(SdpTypedTime? originalOffset, SdpTypedTime? comparedOffset, SdpComparisonResult result, SdpSourcePath path)
    {
        if (originalOffset == null || comparedOffset == null)
        {
            AddOptionalDifference(result, path, "The time-zone offset", originalOffset != null, comparedOffset != null, false);
            return;
        }
        AddScalarDifference(result, path, "The time-zone offset numeric value differs.", ReadInt64Value(originalOffset.Value), ReadInt64Value(comparedOffset.Value), false);
        AddScalarDifference(result, path, "The time-zone offset unit differs.", ReadStringValue(originalOffset.Unit), ReadStringValue(comparedOffset.Unit), false);
        AddScalarDifference(result, path, "The retained time-zone offset text differs.", ReadStringValue(originalOffset.RawValue), ReadStringValue(comparedOffset.RawValue), false);
    }

    internal static void CompareEncryptionKeyField(SdpEncryptionKey? originalKey, SdpEncryptionKey? comparedKey, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId)
    {
        SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'k', string.Empty, 0, -1);
        if (originalKey == null || comparedKey == null)
        {
            AddOptionalDifference(result, path, "The encryption-key field", originalKey != null, comparedKey != null, true);
            return;
        }
        AddScalarDifference(result, path, "The encryption-key method differs.", ReadStringValue(originalKey.Method), ReadStringValue(comparedKey.Method), false);
        AddScalarDifference(result, path, "The encryption-key value presence differs.", ReadBooleanValue(originalKey.HasValue), ReadBooleanValue(comparedKey.HasValue), false);
        AddScalarDifference(result, path, "The encryption-key value differs.", ReadStringValue(originalKey.Value), ReadStringValue(comparedKey.Value), true);
        AddScalarDifference(result, path, "The retained encryption-key text differs.", ReadStringValue(originalKey.RawText), ReadStringValue(comparedKey.RawText), true);
        CompareLocationFields(originalKey.Location, comparedKey.Location, result, path, "encryption-key");
    }

    internal static void CompareAttributeFields(List<SdpAttribute>? originalAttributes, List<SdpAttribute>? comparedAttributes, List<string>? originalFormats, List<string>? comparedFormats, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(scope, mediaIndex, mediaId, 'a', string.Empty, -1, -1);
        if (originalAttributes == null || comparedAttributes == null)
        {
            AddOptionalDifference(result, collectionPath, "The attribute collection", originalAttributes != null, comparedAttributes != null, true);
            return;
        }
        int[] matches = new int[originalAttributes.Count];
        bool[] comparedMatched = new bool[comparedAttributes.Count];
        int index = 0;
        while (index < matches.Length)
        {
            matches[index] = -1;
            index = index + 1;
        }
        MatchExactAttributes(originalAttributes, comparedAttributes, matches, comparedMatched);
        MatchLogicalAttributes(originalAttributes, comparedAttributes, matches, comparedMatched);
        index = 0;
        while (index < originalAttributes.Count)
        {
            int comparedIndex = matches[index];
            SdpAttribute? originalAttribute = originalAttributes[index];
            if (comparedIndex < 0)
            {
                int occurrenceIndex = ReadAttributeOccurrenceIndex(originalAttributes, index);
                int valueIndex = ReadAttributeValueIndex(originalAttribute, originalFormats);
                SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'a', ReadAttributePathName(originalAttribute, null), occurrenceIndex, valueIndex);
                AddDifference(result, path, SdpDifferenceKind.Removed, ReadAttributeDisplayValue(originalAttribute), string.Empty, IsSensitiveAttributePair(originalAttribute, null), "The attribute was removed.");
            }
            else
            {
                SdpAttribute? comparedAttribute = comparedAttributes[comparedIndex];
                int occurrenceIndex = ReadAttributeOccurrenceIndex(comparedAttributes, comparedIndex);
                int valueIndex = ReadAttributeValueIndex(comparedAttribute, comparedFormats);
                if (valueIndex < 0)
                {
                    valueIndex = ReadAttributeValueIndex(originalAttribute, originalFormats);
                }
                SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'a', ReadAttributePathName(originalAttribute, comparedAttribute), occurrenceIndex, valueIndex);
                bool sensitive = IsSensitiveAttributePair(originalAttribute, comparedAttribute);
                if (index != comparedIndex)
                {
                    AddDifference(result, path, SdpDifferenceKind.Reordered, ReadAttributeDisplayValue(originalAttribute), ReadAttributeDisplayValue(comparedAttribute), sensitive, "The attribute moved from index " + ReadInt32Value(index) + " to index " + ReadInt32Value(comparedIndex) + ".");
                }
                CompareMatchedAttributeFields(originalAttribute, comparedAttribute, result, path, sensitive);
            }
            index = index + 1;
        }
        index = 0;
        while (index < comparedAttributes.Count)
        {
            if (!comparedMatched[index])
            {
                SdpAttribute? attribute = comparedAttributes[index];
                int occurrenceIndex = ReadAttributeOccurrenceIndex(comparedAttributes, index);
                int valueIndex = ReadAttributeValueIndex(attribute, comparedFormats);
                SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, 'a', ReadAttributePathName(null, attribute), occurrenceIndex, valueIndex);
                AddDifference(result, path, SdpDifferenceKind.Added, string.Empty, ReadAttributeDisplayValue(attribute), IsSensitiveAttributePair(null, attribute), "The attribute was added.");
            }
            index = index + 1;
        }
    }

    internal static void MatchExactAttributes(List<SdpAttribute> originalAttributes, List<SdpAttribute> comparedAttributes, int[] matches, bool[] comparedMatched)
    {
        int originalIndex = 0;
        while (originalIndex < originalAttributes.Count)
        {
            int comparedIndex = 0;
            while (comparedIndex < comparedAttributes.Count)
            {
                if (!comparedMatched[comparedIndex] && AttributesExactlyEqual(originalAttributes[originalIndex], comparedAttributes[comparedIndex]))
                {
                    matches[originalIndex] = comparedIndex;
                    comparedMatched[comparedIndex] = true;
                    break;
                }
                comparedIndex = comparedIndex + 1;
            }
            originalIndex = originalIndex + 1;
        }
    }

    internal static void MatchLogicalAttributes(List<SdpAttribute> originalAttributes, List<SdpAttribute> comparedAttributes, int[] matches, bool[] comparedMatched)
    {
        int originalIndex = 0;
        while (originalIndex < originalAttributes.Count)
        {
            if (matches[originalIndex] < 0)
            {
                int comparedIndex = 0;
                while (comparedIndex < comparedAttributes.Count)
                {
                    if (!comparedMatched[comparedIndex] && AttributesShareIdentity(originalAttributes[originalIndex], comparedAttributes[comparedIndex]))
                    {
                        matches[originalIndex] = comparedIndex;
                        comparedMatched[comparedIndex] = true;
                        break;
                    }
                    comparedIndex = comparedIndex + 1;
                }
            }
            originalIndex = originalIndex + 1;
        }
    }

    internal static bool AttributesExactlyEqual(SdpAttribute? originalAttribute, SdpAttribute? comparedAttribute)
    {
        if (originalAttribute == null || comparedAttribute == null)
        {
            return originalAttribute == null && comparedAttribute == null;
        }
        return originalAttribute.Kind == comparedAttribute.Kind
            && string.Equals(originalAttribute.Name, comparedAttribute.Name, StringComparison.Ordinal)
            && string.Equals(originalAttribute.Value, comparedAttribute.Value, StringComparison.Ordinal)
            && string.Equals(originalAttribute.RawText, comparedAttribute.RawText, StringComparison.Ordinal);
    }

    internal static bool AttributesShareIdentity(SdpAttribute? originalAttribute, SdpAttribute? comparedAttribute)
    {
        if (originalAttribute == null || comparedAttribute == null)
        {
            return originalAttribute == null && comparedAttribute == null;
        }
        if (!string.Equals(originalAttribute.Name, comparedAttribute.Name, StringComparison.Ordinal))
        {
            return false;
        }
        if (!IsPayloadAttributeName(originalAttribute.Name))
        {
            return true;
        }
        return string.Equals(ReadAttributePayloadSelector(originalAttribute), ReadAttributePayloadSelector(comparedAttribute), StringComparison.Ordinal);
    }

    internal static void CompareMatchedAttributeFields(SdpAttribute? originalAttribute, SdpAttribute? comparedAttribute, SdpComparisonResult result, SdpSourcePath path, bool sensitive)
    {
        if (originalAttribute == null || comparedAttribute == null)
        {
            AddOptionalDifference(result, path, "The attribute", originalAttribute != null, comparedAttribute != null, sensitive);
            return;
        }
        AddScalarDifference(result, path, "The attribute kind differs.", ReadInt32Value((int)originalAttribute.Kind), ReadInt32Value((int)comparedAttribute.Kind), false);
        AddScalarDifference(result, path, "The attribute name differs.", ReadStringValue(originalAttribute.Name), ReadStringValue(comparedAttribute.Name), false);
        AddScalarDifference(result, path, "The attribute value differs.", ReadStringValue(originalAttribute.Value), ReadStringValue(comparedAttribute.Value), sensitive);
        AddScalarDifference(result, path, "The retained attribute text differs.", ReadStringValue(originalAttribute.RawText), ReadStringValue(comparedAttribute.RawText), sensitive);
        CompareLocationFields(originalAttribute.Location, comparedAttribute.Location, result, path, "attribute");
    }

    internal static int ReadAttributeOccurrenceIndex(List<SdpAttribute> attributes, int targetIndex)
    {
        SdpAttribute? target = attributes[targetIndex];
        string targetName = target == null || target.Name == null ? string.Empty : target.Name;
        int occurrenceIndex = 0;
        int index = 0;
        while (index < targetIndex)
        {
            SdpAttribute? attribute = attributes[index];
            string name = attribute == null || attribute.Name == null ? string.Empty : attribute.Name;
            if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return occurrenceIndex;
    }

    internal static int ReadAttributeValueIndex(SdpAttribute? attribute, List<string>? formats)
    {
        if (attribute == null || formats == null || !IsPayloadAttributeName(attribute.Name))
        {
            return -1;
        }
        string selector = ReadAttributePayloadSelector(attribute);
        int index = 0;
        while (index < formats.Count)
        {
            if (string.Equals(formats[index], selector, StringComparison.Ordinal))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsPayloadAttributeName(string? name)
    {
        return string.Equals(name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.FormatParameters, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.RtcpFeedback, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ReadAttributePayloadSelector(SdpAttribute attribute)
    {
        string value = attribute.Value == null ? string.Empty : attribute.Value;
        int index = 0;
        while (index < value.Length && value[index] != ' ' && value[index] != '\t')
        {
            index = index + 1;
        }
        return value.Substring(0, index);
    }

    internal static string ReadAttributeDisplayValue(SdpAttribute? attribute)
    {
        if (attribute == null)
        {
            return "<null>";
        }
        if (attribute.Kind == SdpAttributeKind.Value)
        {
            return ReadStringValue(attribute.Name) + ":" + ReadStringValue(attribute.Value);
        }
        return ReadStringValue(attribute.Name);
    }

    internal static string ReadAttributePathName(SdpAttribute? originalAttribute, SdpAttribute? comparedAttribute)
    {
        if (originalAttribute != null && originalAttribute.Name != null)
        {
            return originalAttribute.Name;
        }
        if (comparedAttribute != null && comparedAttribute.Name != null)
        {
            return comparedAttribute.Name;
        }
        return string.Empty;
    }

    internal static bool IsSensitiveAttributePair(SdpAttribute? originalAttribute, SdpAttribute? comparedAttribute)
    {
        if (originalAttribute != null && SdpValidationIssueMetadataCore.IsSensitiveAttribute(originalAttribute.Name))
        {
            return true;
        }
        return comparedAttribute != null && SdpValidationIssueMetadataCore.IsSensitiveAttribute(comparedAttribute.Name);
    }

    internal static void CompareMediaFields(List<SdpMediaDescription>? originalMedia, List<SdpMediaDescription>? comparedMedia, SdpComparisonResult result)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(SdpScope.Media, -1, string.Empty, 'm', string.Empty, -1, -1);
        if (originalMedia == null || comparedMedia == null)
        {
            AddOptionalDifference(result, collectionPath, "The media-description collection", originalMedia != null, comparedMedia != null, false);
            return;
        }
        int[] matches = new int[originalMedia.Count];
        bool[] comparedMatched = new bool[comparedMedia.Count];
        int index = 0;
        while (index < matches.Length)
        {
            matches[index] = -1;
            index = index + 1;
        }
        MatchMediaByIdentifier(originalMedia, comparedMedia, matches, comparedMatched);
        MatchRemainingMedia(originalMedia, comparedMedia, matches, comparedMatched);
        index = 0;
        while (index < originalMedia.Count)
        {
            int comparedIndex = matches[index];
            SdpMediaDescription? originalDescription = originalMedia[index];
            if (comparedIndex < 0)
            {
                string mediaId = ReadPreferredMediaId(originalDescription, null);
                SdpSourcePath path = CreateComparisonPath(SdpScope.Media, index, mediaId, 'm', string.Empty, 0, -1);
                AddDifference(result, path, SdpDifferenceKind.Removed, ReadMediaDisplayValue(originalDescription), string.Empty, false, "The media description was removed.");
            }
            else
            {
                SdpMediaDescription? comparedDescription = comparedMedia[comparedIndex];
                string mediaId = ReadPreferredMediaId(originalDescription, comparedDescription);
                SdpSourcePath path = CreateComparisonPath(SdpScope.Media, comparedIndex, mediaId, 'm', string.Empty, 0, -1);
                if (index != comparedIndex && MediaIdentifiersMatch(originalDescription, comparedDescription))
                {
                    AddDifference(result, path, SdpDifferenceKind.Reordered, ReadMediaDisplayValue(originalDescription), ReadMediaDisplayValue(comparedDescription), false, "The media description moved from index " + ReadInt32Value(index) + " to index " + ReadInt32Value(comparedIndex) + ".");
                }
                CompareMatchedMediaFields(originalDescription, comparedDescription, result, comparedIndex, mediaId, path);
            }
            index = index + 1;
        }
        index = 0;
        while (index < comparedMedia.Count)
        {
            if (!comparedMatched[index])
            {
                SdpMediaDescription? media = comparedMedia[index];
                SdpSourcePath path = CreateComparisonPath(SdpScope.Media, index, ReadPreferredMediaId(null, media), 'm', string.Empty, 0, -1);
                AddDifference(result, path, SdpDifferenceKind.Added, string.Empty, ReadMediaDisplayValue(media), false, "The media description was added.");
            }
            index = index + 1;
        }
    }

    internal static void MatchMediaByIdentifier(List<SdpMediaDescription> originalMedia, List<SdpMediaDescription> comparedMedia, int[] matches, bool[] comparedMatched)
    {
        int originalIndex = 0;
        while (originalIndex < originalMedia.Count)
        {
            string mediaId = ReadMediaIdValue(originalMedia[originalIndex]);
            if (mediaId.Length > 0)
            {
                int comparedIndex = 0;
                while (comparedIndex < comparedMedia.Count)
                {
                    if (!comparedMatched[comparedIndex] && string.Equals(mediaId, ReadMediaIdValue(comparedMedia[comparedIndex]), StringComparison.Ordinal))
                    {
                        matches[originalIndex] = comparedIndex;
                        comparedMatched[comparedIndex] = true;
                        break;
                    }
                    comparedIndex = comparedIndex + 1;
                }
            }
            originalIndex = originalIndex + 1;
        }
    }

    internal static void MatchRemainingMedia(List<SdpMediaDescription> originalMedia, List<SdpMediaDescription> comparedMedia, int[] matches, bool[] comparedMatched)
    {
        int originalIndex = 0;
        while (originalIndex < originalMedia.Count)
        {
            if (matches[originalIndex] < 0 && originalIndex < comparedMedia.Count && !comparedMatched[originalIndex])
            {
                matches[originalIndex] = originalIndex;
                comparedMatched[originalIndex] = true;
            }
            originalIndex = originalIndex + 1;
        }
    }

    internal static bool MediaIdentifiersMatch(SdpMediaDescription? originalMedia, SdpMediaDescription? comparedMedia)
    {
        string originalId = ReadMediaIdValue(originalMedia);
        return originalId.Length > 0 && string.Equals(originalId, ReadMediaIdValue(comparedMedia), StringComparison.Ordinal);
    }

    internal static void CompareMatchedMediaFields(SdpMediaDescription? originalDescription, SdpMediaDescription? comparedDescription, SdpComparisonResult result, int mediaIndex, string mediaId, SdpSourcePath path)
    {
        if (originalDescription == null || comparedDescription == null)
        {
            AddOptionalDifference(result, path, "The media description", originalDescription != null, comparedDescription != null, false);
            return;
        }
        AddScalarDifference(result, path, "The media type differs.", ReadStringValue(originalDescription.MediaType), ReadStringValue(comparedDescription.MediaType), false);
        AddScalarDifference(result, path, "The media kind differs.", ReadInt32Value((int)originalDescription.MediaKind), ReadInt32Value((int)comparedDescription.MediaKind), false);
        AddScalarDifference(result, path, "The media port differs.", ReadInt32Value(originalDescription.Port), ReadInt32Value(comparedDescription.Port), false);
        AddScalarDifference(result, path, "The media port-count presence differs.", ReadBooleanValue(originalDescription.HasPortCount), ReadBooleanValue(comparedDescription.HasPortCount), false);
        AddScalarDifference(result, path, "The media port count differs.", ReadInt32Value(originalDescription.PortCount), ReadInt32Value(comparedDescription.PortCount), false);
        AddScalarDifference(result, path, "The media transport protocol differs.", ReadStringValue(originalDescription.Protocol), ReadStringValue(comparedDescription.Protocol), false);
        CompareStringCollection(originalDescription.Formats, comparedDescription.Formats, result, SdpScope.Media, mediaIndex, mediaId, 'm', "media format");
        SdpSourcePath informationPath = CreateComparisonPath(SdpScope.Media, mediaIndex, mediaId, 'i', string.Empty, 0, -1);
        AddScalarDifference(result, informationPath, "The media information differs.", ReadStringValue(originalDescription.Information), ReadStringValue(comparedDescription.Information), false);
        CompareConnectionFields(originalDescription.Connections, comparedDescription.Connections, result, mediaIndex, mediaId);
        CompareBandwidthFields(originalDescription.Bandwidths, comparedDescription.Bandwidths, result, SdpScope.Media, mediaIndex, mediaId);
        CompareEncryptionKeyField(originalDescription.EncryptionKey, comparedDescription.EncryptionKey, result, SdpScope.Media, mediaIndex, mediaId);
        CompareAttributeFields(originalDescription.Attributes, comparedDescription.Attributes, originalDescription.Formats, comparedDescription.Formats, result, SdpScope.Media, mediaIndex, mediaId);
        CompareRawLineFields(originalDescription.RawLines, comparedDescription.RawLines, result, SdpScope.Media, mediaIndex, mediaId);
        CompareLocationFields(originalDescription.Location, comparedDescription.Location, result, path, "media description");
    }

    internal static string ReadMediaDisplayValue(SdpMediaDescription? media)
    {
        if (media == null)
        {
            return "<null>";
        }
        string mediaId = ReadMediaIdValue(media);
        if (mediaId.Length > 0)
        {
            return ReadStringValue(media.MediaType) + ";mid=" + mediaId;
        }
        return ReadStringValue(media.MediaType);
    }

    internal static string ReadPreferredMediaId(SdpMediaDescription? originalMedia, SdpMediaDescription? comparedMedia)
    {
        string originalId = ReadMediaIdValue(originalMedia);
        if (originalId.Length > 0)
        {
            return originalId;
        }
        return ReadMediaIdValue(comparedMedia);
    }

    internal static string ReadMediaIdValue(SdpMediaDescription? media)
    {
        if (media == null || media.Attributes == null)
        {
            return string.Empty;
        }
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute? attribute = media.Attributes[index];
            if (attribute != null && string.Equals(attribute.Name, SdpAttributeNames.MediaId, StringComparison.OrdinalIgnoreCase))
            {
                return attribute.Value == null ? string.Empty : attribute.Value;
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static void CompareRawLineFields(List<SdpRawLine>? originalLines, List<SdpRawLine>? comparedLines, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(scope, mediaIndex, mediaId, '\0', string.Empty, -1, -1);
        if (originalLines == null || comparedLines == null)
        {
            AddOptionalDifference(result, collectionPath, "The retained raw-line collection", originalLines != null, comparedLines != null, true);
            return;
        }
        int sharedCount = originalLines.Count < comparedLines.Count ? originalLines.Count : comparedLines.Count;
        int index = 0;
        while (index < sharedCount)
        {
            SdpRawLine? originalLine = originalLines[index];
            SdpRawLine? comparedLine = comparedLines[index];
            char fieldName = originalLine == null ? comparedLine == null ? '\0' : comparedLine.FieldName : originalLine.FieldName;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, index, -1);
            if (originalLine == null || comparedLine == null)
            {
                AddOptionalDifference(result, path, "The retained raw line", originalLine != null, comparedLine != null, true);
            }
            else
            {
                AddScalarDifference(result, path, "The retained raw-line field name differs.", ReadCharacterValue(originalLine.FieldName), ReadCharacterValue(comparedLine.FieldName), false);
                AddScalarDifference(result, path, "The retained raw-line text differs.", ReadStringValue(originalLine.Text), ReadStringValue(comparedLine.Text), true);
                AddScalarDifference(result, path, "The retained raw-line value differs.", ReadStringValue(originalLine.Value), ReadStringValue(comparedLine.Value), true);
                AddScalarDifference(result, path, "The retained raw-line ending differs.", ReadInt32Value((int)originalLine.LineEnding), ReadInt32Value((int)comparedLine.LineEnding), false);
                AddScalarDifference(result, path, "The retained raw-line malformed state differs.", ReadBooleanValue(originalLine.IsMalformed), ReadBooleanValue(comparedLine.IsMalformed), false);
                CompareLocationFields(originalLine.Location, comparedLine.Location, result, path, "retained raw line");
            }
            index = index + 1;
        }
        while (index < originalLines.Count)
        {
            SdpRawLine? line = originalLines[index];
            char fieldName = line == null ? '\0' : line.FieldName;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The retained raw line", true, false, true);
            index = index + 1;
        }
        while (index < comparedLines.Count)
        {
            SdpRawLine? line = comparedLines[index];
            char fieldName = line == null ? '\0' : line.FieldName;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, index, -1);
            AddCollectionElementDifference(result, path, "The retained raw line", false, true, true);
            index = index + 1;
        }
    }

    internal static void CompareLocationFields(SdpSourceLocation? originalLocation, SdpSourceLocation? comparedLocation, SdpComparisonResult result, SdpSourcePath path, string componentName)
    {
        if (originalLocation == null || comparedLocation == null)
        {
            AddOptionalDifference(result, path, "The " + componentName + " source location", originalLocation != null, comparedLocation != null, false);
            return;
        }
        AddScalarDifference(result, path, "The " + componentName + " character offset differs.", ReadInt32Value(originalLocation.CharacterOffset), ReadInt32Value(comparedLocation.CharacterOffset), false);
        AddScalarDifference(result, path, "The " + componentName + " byte offset differs.", ReadInt32Value(originalLocation.ByteOffset), ReadInt32Value(comparedLocation.ByteOffset), false);
        AddScalarDifference(result, path, "The " + componentName + " line number differs.", ReadInt32Value(originalLocation.LineNumber), ReadInt32Value(comparedLocation.LineNumber), false);
        AddScalarDifference(result, path, "The " + componentName + " column number differs.", ReadInt32Value(originalLocation.ColumnNumber), ReadInt32Value(comparedLocation.ColumnNumber), false);
    }

    internal static void CompareStringCollection(List<string>? originalValues, List<string>? comparedValues, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId, char fieldName, string componentName)
    {
        SdpSourcePath collectionPath = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, -1, -1);
        if (originalValues == null || comparedValues == null)
        {
            AddOptionalDifference(result, collectionPath, "The " + componentName + " collection", originalValues != null, comparedValues != null, false);
            return;
        }
        if (StringCollectionsArePermutation(originalValues, comparedValues))
        {
            AddStringReorderDifferences(originalValues, comparedValues, result, scope, mediaIndex, mediaId, fieldName, componentName);
            return;
        }
        int sharedCount = originalValues.Count < comparedValues.Count ? originalValues.Count : comparedValues.Count;
        int index = 0;
        while (index < sharedCount)
        {
            int valueIndex = fieldName == 'm' ? index : -1;
            int occurrenceIndex = fieldName == 'm' ? 0 : index;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, occurrenceIndex, valueIndex);
            AddScalarDifference(result, path, "The " + componentName + " differs.", ReadStringValue(originalValues[index]), ReadStringValue(comparedValues[index]), false);
            index = index + 1;
        }
        while (index < originalValues.Count)
        {
            int valueIndex = fieldName == 'm' ? index : -1;
            int occurrenceIndex = fieldName == 'm' ? 0 : index;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, occurrenceIndex, valueIndex);
            AddDifference(result, path, SdpDifferenceKind.Removed, ReadStringValue(originalValues[index]), string.Empty, false, "The " + componentName + " was removed.");
            index = index + 1;
        }
        while (index < comparedValues.Count)
        {
            int valueIndex = fieldName == 'm' ? index : -1;
            int occurrenceIndex = fieldName == 'm' ? 0 : index;
            SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, occurrenceIndex, valueIndex);
            AddDifference(result, path, SdpDifferenceKind.Added, string.Empty, ReadStringValue(comparedValues[index]), false, "The " + componentName + " was added.");
            index = index + 1;
        }
    }

    internal static bool StringCollectionsArePermutation(List<string> originalValues, List<string> comparedValues)
    {
        if (originalValues.Count != comparedValues.Count)
        {
            return false;
        }
        bool[] matched = new bool[comparedValues.Count];
        int originalIndex = 0;
        while (originalIndex < originalValues.Count)
        {
            int comparedIndex = 0;
            while (comparedIndex < comparedValues.Count)
            {
                if (!matched[comparedIndex] && string.Equals(originalValues[originalIndex], comparedValues[comparedIndex], StringComparison.Ordinal))
                {
                    matched[comparedIndex] = true;
                    break;
                }
                comparedIndex = comparedIndex + 1;
            }
            if (comparedIndex == comparedValues.Count)
            {
                return false;
            }
            originalIndex = originalIndex + 1;
        }
        return true;
    }

    internal static void AddStringReorderDifferences(List<string> originalValues, List<string> comparedValues, SdpComparisonResult result, SdpScope scope, int mediaIndex, string mediaId, char fieldName, string componentName)
    {
        bool[] matched = new bool[comparedValues.Count];
        int originalIndex = 0;
        while (originalIndex < originalValues.Count)
        {
            int comparedIndex = 0;
            while (comparedIndex < comparedValues.Count)
            {
                if (!matched[comparedIndex] && string.Equals(originalValues[originalIndex], comparedValues[comparedIndex], StringComparison.Ordinal))
                {
                    matched[comparedIndex] = true;
                    break;
                }
                comparedIndex = comparedIndex + 1;
            }
            if (originalIndex != comparedIndex)
            {
                int valueIndex = fieldName == 'm' ? comparedIndex : -1;
                int occurrenceIndex = fieldName == 'm' ? 0 : comparedIndex;
                SdpSourcePath path = CreateComparisonPath(scope, mediaIndex, mediaId, fieldName, string.Empty, occurrenceIndex, valueIndex);
                AddDifference(result, path, SdpDifferenceKind.Reordered, ReadStringValue(originalValues[originalIndex]), ReadStringValue(comparedValues[comparedIndex]), false, "The " + componentName + " moved from index " + ReadInt32Value(originalIndex) + " to index " + ReadInt32Value(comparedIndex) + ".");
            }
            originalIndex = originalIndex + 1;
        }
    }

    internal static void AddScalarDifference(SdpComparisonResult result, SdpSourcePath path, string detail, string originalValue, string comparedValue, bool sensitive)
    {
        if (!string.Equals(originalValue, comparedValue, StringComparison.Ordinal))
        {
            AddDifference(result, path, SdpDifferenceKind.Changed, originalValue, comparedValue, sensitive, detail);
        }
    }

    internal static void AddOptionalDifference(SdpComparisonResult result, SdpSourcePath path, string componentName, bool originalPresent, bool comparedPresent, bool sensitive)
    {
        if (originalPresent == comparedPresent)
        {
            return;
        }
        if (comparedPresent)
        {
            AddDifference(result, path, SdpDifferenceKind.Added, string.Empty, "present", sensitive, componentName + " was added.");
        }
        else
        {
            AddDifference(result, path, SdpDifferenceKind.Removed, "present", string.Empty, sensitive, componentName + " was removed.");
        }
    }

    internal static void AddCollectionElementDifference(SdpComparisonResult result, SdpSourcePath path, string componentName, bool originalPresent, bool comparedPresent, bool sensitive)
    {
        AddOptionalDifference(result, path, componentName, originalPresent, comparedPresent, sensitive);
    }

    internal static void AddDifference(SdpComparisonResult result, SdpSourcePath path, SdpDifferenceKind kind, string originalValue, string comparedValue, bool sensitive, string detail)
    {
        SdpDifference difference = new SdpDifference();
        difference.Kind = kind;
        difference.Path = CopyComparisonPath(path);
        difference.Detail = detail;
        if (sensitive)
        {
            difference.ValuesRedacted = true;
        }
        else
        {
            difference.OriginalValue = originalValue;
            difference.ComparedValue = comparedValue;
        }
        result.Differences.Add(difference);
    }

    internal static SdpSourcePath CreateComparisonPath(SdpScope scope, int mediaIndex, string mediaId, char fieldName, string attributeName, int occurrenceIndex, int valueIndex)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.MediaId = mediaId;
        path.FieldName = fieldName;
        path.AttributeName = attributeName;
        path.OccurrenceIndex = occurrenceIndex;
        path.ValueIndex = valueIndex;
        return path;
    }

    internal static SdpSourcePath CopyComparisonPath(SdpSourcePath source)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = source.Scope;
        path.MediaIndex = source.MediaIndex;
        path.MediaId = source.MediaId;
        path.FieldName = source.FieldName;
        path.AttributeName = source.AttributeName;
        path.OccurrenceIndex = source.OccurrenceIndex;
        path.ValueIndex = source.ValueIndex;
        return path;
    }

    internal static string ReadStringValue(string? value)
    {
        if (value == null)
        {
            return "<null>";
        }
        return value;
    }

    internal static string ReadBooleanValue(bool value)
    {
        return value ? "true" : "false";
    }

    internal static string ReadInt32Value(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string ReadInt64Value(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string ReadCharacterValue(char value)
    {
        return value.ToString();
    }
}
