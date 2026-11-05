using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace sdp;

internal static class SdpLosslessWriterCore
{
    internal static SdpWriteResult WriteLosslessText(SdpSessionDescription description, SdpWriteOptions options, SdpWriteResult result)
    {
        if (!SdpGenerationValidationCore.ValidateRetainedLines(description, options, result))
        {
            return result;
        }
        if (description.RawLines.Count == 0)
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteLosslessDataUnavailable, SdpScope.Session, '\0', string.Empty, -1, "Lossless output requires source lines retained by lossless parsing or PreserveRawLines.", new SdpSourceLocation());
            return result;
        }

        string source = ReadRetainedSource(description);
        SdpParseOptions parseOptions = CreateSnapshotOptions(source);
        SdpParseResult parsed = SdpParserCore.ParseTextInput(source, parseOptions, false);
        if (parsed.Description == null || parsed.CharactersConsumed != source.Length)
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteLosslessDataUnavailable, SdpScope.Session, '\0', string.Empty, -1, "The retained source lines are incomplete or cannot be reconstructed safely.", new SdpSourceLocation());
            return result;
        }

        StringBuilder builder = new StringBuilder();
        long buildLimit = (long)options.MaximumOutputCharacters + 2L;
        if (!WriteSessionRawLines(builder, description, parsed.Description, options, buildLimit, result))
        {
            return result;
        }
        int mediaIndex = 0;
        int originalMediaIndex = 0;
        string generatedLineEnding = SdpTrickleIceFragmentCore.ReadLineEnding(options.LineEnding);
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media.RawLines.Count == 0)
            {
                if (!EnsureLineBoundary(builder, generatedLineEnding, buildLimit, result, options, SdpScope.Media, mediaIndex))
                {
                    return result;
                }
                if (!SdpWriterCore.WriteMediaFields(builder, media, mediaIndex, options, generatedLineEnding, buildLimit, result))
                {
                    return result;
                }
            }
            else
            {
                if (originalMediaIndex >= parsed.Description.MediaDescriptions.Count)
                {
                    SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteLosslessDataUnavailable, SdpScope.Media, 'm', string.Empty, mediaIndex, "The retained media source lines cannot be associated with a parsed media description.", media.Location);
                    return result;
                }
                if (!WriteMediaRawLines(builder, media, parsed.Description.MediaDescriptions[originalMediaIndex], mediaIndex, options, buildLimit, result))
                {
                    return result;
                }
                originalMediaIndex = originalMediaIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }

        if (builder.Length > options.MaximumOutputCharacters)
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Session, '\0', string.Empty, -1, "The generated SDP description exceeds the configured character limit.", new SdpSourceLocation());
            return result;
        }
        result.Text = builder.ToString();
        result.CharactersWritten = result.Text.Length;
        result.Success = true;
        return result;
    }

    internal static string ReadRetainedSource(SdpSessionDescription description)
    {
        StringBuilder builder = new StringBuilder();
        AppendRetainedLines(builder, description.RawLines);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (description.MediaDescriptions[mediaIndex].RawLines.Count > 0)
            {
                AppendRetainedLines(builder, description.MediaDescriptions[mediaIndex].RawLines);
            }
            mediaIndex = mediaIndex + 1;
        }
        return builder.ToString();
    }

    internal static void AppendRetainedLines(StringBuilder builder, List<SdpRawLine> lines)
    {
        int index = 0;
        while (index < lines.Count)
        {
            builder.Append(lines[index].Text);
            builder.Append(ReadPreservedLineEnding(lines[index].LineEnding));
            index = index + 1;
        }
    }

    internal static SdpParseOptions CreateSnapshotOptions(string source)
    {
        SdpParseOptions options = new SdpParseOptions();
        options.Mode = SdpParseMode.Lossless;
        options.AllowLfLineEndings = true;
        options.AllowCrLineEndings = true;
        options.PreserveRawLines = true;
        options.PreserveMalformedLines = true;
        options.MaximumInputCharacters = source.Length;
        options.MaximumLineLength = source.Length;
        options.MaximumLineCount = int.MaxValue;
        options.MaximumMediaCount = int.MaxValue;
        options.MaximumAttributesPerScope = int.MaxValue;
        options.MaximumTokenLength = source.Length;
        options.MaximumCandidateCount = int.MaxValue;
        options.MaximumDiagnosticCount = int.MaxValue;
        return options;
    }

    internal static bool WriteSessionRawLines(StringBuilder builder, SdpSessionDescription current, SdpSessionDescription original, SdpWriteOptions options, long buildLimit, SdpWriteResult result)
    {
        int emailIndex = 0;
        int phoneIndex = 0;
        int bandwidthIndex = 0;
        int timingIndex = 0;
        int repeatIndex = 0;
        int attributeIndex = 0;
        int index = 0;
        while (index < current.RawLines.Count)
        {
            SdpRawLine raw = current.RawLines[index];
            bool written = true;
            if (raw.IsMalformed)
            {
                written = AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
            }
            else if (raw.FieldName == 'v')
            {
                written = WriteProtocolVersionLine(builder, raw, current, original, buildLimit, result, options);
            }
            else if (raw.FieldName == 'o')
            {
                written = WriteOriginLine(builder, raw, current, original, buildLimit, result, options);
            }
            else if (raw.FieldName == 's')
            {
                written = WriteSessionNameLine(builder, raw, current, original, buildLimit, result, options);
            }
            else if (raw.FieldName == 'i')
            {
                written = WriteOptionalTextLine(builder, raw, current.Information, original.Information, SdpScope.Session, -1, buildLimit, result, options);
            }
            else if (raw.FieldName == 'u')
            {
                written = WriteOptionalTextLine(builder, raw, current.Uri, original.Uri, SdpScope.Session, -1, buildLimit, result, options);
            }
            else if (raw.FieldName == 'e')
            {
                written = WriteRepeatedTextLine(builder, raw, current.Emails, original.Emails, emailIndex, 'e', buildLimit, result, options);
                emailIndex = emailIndex + 1;
            }
            else if (raw.FieldName == 'p')
            {
                written = WriteRepeatedTextLine(builder, raw, current.Phones, original.Phones, phoneIndex, 'p', buildLimit, result, options);
                phoneIndex = phoneIndex + 1;
            }
            else if (raw.FieldName == 'c')
            {
                written = WriteSessionConnectionLine(builder, raw, current.Connection, original.Connection, buildLimit, result, options);
            }
            else if (raw.FieldName == 'b')
            {
                written = WriteBandwidthLine(builder, raw, current.Bandwidths, original.Bandwidths, bandwidthIndex, SdpScope.Session, -1, buildLimit, result, options);
                bandwidthIndex = bandwidthIndex + 1;
            }
            else if (raw.FieldName == 't')
            {
                written = WriteTimingLine(builder, raw, current.Timings, original.Timings, timingIndex, buildLimit, result, options);
                timingIndex = timingIndex + 1;
            }
            else if (raw.FieldName == 'r')
            {
                written = WriteRepeatLine(builder, raw, current, original, repeatIndex, buildLimit, result, options);
                repeatIndex = repeatIndex + 1;
            }
            else if (raw.FieldName == 'z')
            {
                written = WriteZoneLine(builder, raw, current.TimeZoneAdjustments, original.TimeZoneAdjustments, buildLimit, result, options);
            }
            else if (raw.FieldName == 'k')
            {
                written = WriteKeyLine(builder, raw, current.EncryptionKey, original.EncryptionKey, SdpScope.Session, -1, buildLimit, result, options);
            }
            else if (raw.FieldName == 'a')
            {
                written = WriteAttributeLine(builder, raw, current.Attributes, original.Attributes, attributeIndex, SdpScope.Session, -1, buildLimit, result, options);
                attributeIndex = attributeIndex + 1;
            }
            else if (options.IncludeUnknownFields)
            {
                written = AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
            }
            if (!written)
            {
                return false;
            }
            index = index + 1;
        }
        return WriteAdditionalAttributes(builder, current.Attributes, original.Attributes.Count, SdpScope.Session, -1, options, buildLimit, result);
    }

    internal static bool WriteMediaRawLines(StringBuilder builder, SdpMediaDescription current, SdpMediaDescription original, int mediaIndex, SdpWriteOptions options, long buildLimit, SdpWriteResult result)
    {
        int connectionIndex = 0;
        int bandwidthIndex = 0;
        int attributeIndex = 0;
        int index = 0;
        while (index < current.RawLines.Count)
        {
            SdpRawLine raw = current.RawLines[index];
            bool written = true;
            if (raw.IsMalformed)
            {
                written = AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Media, mediaIndex);
            }
            else if (raw.FieldName == 'm')
            {
                written = WriteMediaHeaderLine(builder, raw, current, original, mediaIndex, buildLimit, result, options);
            }
            else if (raw.FieldName == 'i')
            {
                written = WriteOptionalTextLine(builder, raw, current.Information, original.Information, SdpScope.Media, mediaIndex, buildLimit, result, options);
            }
            else if (raw.FieldName == 'c')
            {
                written = WriteConnectionLine(builder, raw, current.Connections, original.Connections, connectionIndex, mediaIndex, buildLimit, result, options);
                connectionIndex = connectionIndex + 1;
            }
            else if (raw.FieldName == 'b')
            {
                written = WriteBandwidthLine(builder, raw, current.Bandwidths, original.Bandwidths, bandwidthIndex, SdpScope.Media, mediaIndex, buildLimit, result, options);
                bandwidthIndex = bandwidthIndex + 1;
            }
            else if (raw.FieldName == 'k')
            {
                written = WriteKeyLine(builder, raw, current.EncryptionKey, original.EncryptionKey, SdpScope.Media, mediaIndex, buildLimit, result, options);
            }
            else if (raw.FieldName == 'a')
            {
                written = WriteAttributeLine(builder, raw, current.Attributes, original.Attributes, attributeIndex, SdpScope.Media, mediaIndex, buildLimit, result, options);
                attributeIndex = attributeIndex + 1;
            }
            else if (options.IncludeUnknownFields)
            {
                written = AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Media, mediaIndex);
            }
            if (!written)
            {
                return false;
            }
            index = index + 1;
        }
        return WriteAdditionalAttributes(builder, current.Attributes, original.Attributes.Count, SdpScope.Media, mediaIndex, options, buildLimit, result);
    }

    internal static bool WriteProtocolVersionLine(StringBuilder builder, SdpRawLine raw, SdpSessionDescription current, SdpSessionDescription original, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (!current.HasProtocolVersion)
        {
            return true;
        }
        if (original.HasProtocolVersion && current.ProtocolVersion == original.ProtocolVersion)
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        return AppendReplacementLine(builder, "v=" + current.ProtocolVersion.ToString(CultureInfo.InvariantCulture), raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteOriginLine(StringBuilder builder, SdpRawLine raw, SdpSessionDescription current, SdpSessionDescription original, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (current.Origin == null)
        {
            return true;
        }
        if (original.Origin != null && OriginsEqual(current.Origin, original.Origin))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        SdpOrigin origin = current.Origin;
        string line = "o=" + origin.Username + " " + origin.SessionId + " " + origin.SessionVersion + " " + origin.NetworkType + " " + origin.AddressType + " " + origin.UnicastAddress;
        return AppendReplacementLine(builder, line, raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteSessionNameLine(StringBuilder builder, SdpRawLine raw, SdpSessionDescription current, SdpSessionDescription original, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (!current.HasSessionName)
        {
            return true;
        }
        if (original.HasSessionName && string.Equals(current.SessionName, original.SessionName, StringComparison.Ordinal))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        return AppendReplacementLine(builder, "s=" + current.SessionName, raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteOptionalTextLine(StringBuilder builder, SdpRawLine raw, string? current, string? original, SdpScope scope, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (current == null)
        {
            return true;
        }
        if (original != null && string.Equals(current, original, StringComparison.Ordinal))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, scope, mediaIndex);
        }
        return AppendReplacementLine(builder, raw.FieldName + "=" + current, raw, buildLimit, result, options, scope, mediaIndex);
    }

    internal static bool WriteRepeatedTextLine(StringBuilder builder, SdpRawLine raw, List<string> current, List<string> original, int itemIndex, char fieldName, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (itemIndex >= current.Count)
        {
            return true;
        }
        if (itemIndex < original.Count && string.Equals(current[itemIndex], original[itemIndex], StringComparison.Ordinal))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        return AppendReplacementLine(builder, fieldName + "=" + current[itemIndex], raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteSessionConnectionLine(StringBuilder builder, SdpRawLine raw, SdpConnectionData? current, SdpConnectionData? original, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (current == null)
        {
            return true;
        }
        if (original != null && ConnectionsEqual(current, original))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        return SdpWriterCore.WriteConnection(builder, current, SdpScope.Session, -1, ReadPreservedLineEnding(raw.LineEnding), buildLimit, result, options);
    }

    internal static bool WriteConnectionLine(StringBuilder builder, SdpRawLine raw, List<SdpConnectionData> current, List<SdpConnectionData> original, int itemIndex, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (itemIndex >= current.Count)
        {
            return true;
        }
        if (itemIndex < original.Count && ConnectionsEqual(current[itemIndex], original[itemIndex]))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Media, mediaIndex);
        }
        return SdpWriterCore.WriteConnection(builder, current[itemIndex], SdpScope.Media, mediaIndex, ReadPreservedLineEnding(raw.LineEnding), buildLimit, result, options);
    }

    internal static bool WriteBandwidthLine(StringBuilder builder, SdpRawLine raw, List<SdpBandwidth> current, List<SdpBandwidth> original, int itemIndex, SdpScope scope, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (itemIndex >= current.Count)
        {
            return true;
        }
        if (itemIndex < original.Count && BandwidthsEqual(current[itemIndex], original[itemIndex]))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, scope, mediaIndex);
        }
        string line = "b=" + current[itemIndex].Type + ":" + current[itemIndex].Value.ToString(CultureInfo.InvariantCulture);
        return AppendReplacementLine(builder, line, raw, buildLimit, result, options, scope, mediaIndex);
    }

    internal static bool WriteTimingLine(StringBuilder builder, SdpRawLine raw, List<SdpTiming> current, List<SdpTiming> original, int itemIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (itemIndex >= current.Count)
        {
            return true;
        }
        if (itemIndex < original.Count && string.Equals(current[itemIndex].StartTime, original[itemIndex].StartTime, StringComparison.Ordinal) && string.Equals(current[itemIndex].StopTime, original[itemIndex].StopTime, StringComparison.Ordinal))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        return AppendReplacementLine(builder, "t=" + current[itemIndex].StartTime + " " + current[itemIndex].StopTime, raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteRepeatLine(StringBuilder builder, SdpRawLine raw, SdpSessionDescription current, SdpSessionDescription original, int itemIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        SdpRepeatTime? currentRepeat = ReadRepeatAt(current.Timings, itemIndex);
        if (currentRepeat == null)
        {
            return true;
        }
        SdpRepeatTime? originalRepeat = ReadRepeatAt(original.Timings, itemIndex);
        if (originalRepeat != null && RepeatsEqual(currentRepeat, originalRepeat))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        string line = "r=" + SdpWriterCore.ReadTypedTime(currentRepeat.RepeatInterval) + " " + SdpWriterCore.ReadTypedTime(currentRepeat.ActiveDuration);
        int offsetIndex = 0;
        while (offsetIndex < currentRepeat.Offsets.Count)
        {
            line = line + " " + SdpWriterCore.ReadTypedTime(currentRepeat.Offsets[offsetIndex]);
            offsetIndex = offsetIndex + 1;
        }
        return AppendReplacementLine(builder, line, raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteZoneLine(StringBuilder builder, SdpRawLine raw, List<SdpTimeZoneAdjustment> current, List<SdpTimeZoneAdjustment> original, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (current.Count == 0)
        {
            return true;
        }
        if (ZonesEqual(current, original))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Session, -1);
        }
        string line = "z=";
        int index = 0;
        while (index < current.Count)
        {
            if (index > 0)
            {
                line = line + " ";
            }
            line = line + current[index].AdjustmentTime + " " + SdpWriterCore.ReadTypedTime(current[index].Offset);
            index = index + 1;
        }
        return AppendReplacementLine(builder, line, raw, buildLimit, result, options, SdpScope.Session, -1);
    }

    internal static bool WriteKeyLine(StringBuilder builder, SdpRawLine raw, SdpEncryptionKey? current, SdpEncryptionKey? original, SdpScope scope, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (!options.IncludeObsoleteFields || current == null)
        {
            return true;
        }
        if (original != null && KeysEqual(current, original))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, scope, mediaIndex);
        }
        return SdpWriterCore.WriteEncryptionKey(builder, current, scope, mediaIndex, ReadPreservedLineEnding(raw.LineEnding), buildLimit, result, options);
    }

    internal static bool WriteAttributeLine(StringBuilder builder, SdpRawLine raw, List<SdpAttribute> current, List<SdpAttribute> original, int itemIndex, SdpScope scope, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (itemIndex >= current.Count)
        {
            if (itemIndex >= original.Count)
            {
                return AppendPreservedLine(builder, raw, buildLimit, result, options, scope, mediaIndex);
            }
            return true;
        }
        SdpAttribute attribute = current[itemIndex];
        if (!ShouldIncludeAttribute(attribute, options))
        {
            return true;
        }
        if (itemIndex < original.Count && AttributesEqual(attribute, original[itemIndex]))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, scope, mediaIndex);
        }
        List<SdpAttribute> single = new List<SdpAttribute>();
        single.Add(attribute);
        return SdpWriterCore.WriteAttributes(builder, single, scope, mediaIndex, ReadPreservedLineEnding(raw.LineEnding), buildLimit, result, options);
    }

    internal static bool WriteAdditionalAttributes(StringBuilder builder, List<SdpAttribute> current, int originalCount, SdpScope scope, int mediaIndex, SdpWriteOptions options, long buildLimit, SdpWriteResult result)
    {
        if (current.Count <= originalCount)
        {
            return true;
        }
        string lineEnding = SdpTrickleIceFragmentCore.ReadLineEnding(options.LineEnding);
        if (!EnsureLineBoundary(builder, lineEnding, buildLimit, result, options, scope, mediaIndex))
        {
            return false;
        }
        List<SdpAttribute> additional = new List<SdpAttribute>();
        int index = originalCount;
        while (index < current.Count)
        {
            additional.Add(current[index]);
            index = index + 1;
        }
        return SdpWriterCore.WriteAttributes(builder, additional, scope, mediaIndex, lineEnding, buildLimit, result, options);
    }

    internal static bool WriteMediaHeaderLine(StringBuilder builder, SdpRawLine raw, SdpMediaDescription current, SdpMediaDescription original, int mediaIndex, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (MediaHeadersEqual(current, original))
        {
            return AppendPreservedLine(builder, raw, buildLimit, result, options, SdpScope.Media, mediaIndex);
        }
        string port = current.Port.ToString(CultureInfo.InvariantCulture);
        if (current.HasPortCount)
        {
            port = port + "/" + current.PortCount.ToString(CultureInfo.InvariantCulture);
        }
        string line = "m=" + current.MediaType + " " + port + " " + current.Protocol;
        int index = 0;
        while (index < current.Formats.Count)
        {
            line = line + " " + current.Formats[index];
            index = index + 1;
        }
        return AppendReplacementLine(builder, line, raw, buildLimit, result, options, SdpScope.Media, mediaIndex);
    }

    internal static bool AppendPreservedLine(StringBuilder builder, SdpRawLine raw, long buildLimit, SdpWriteResult result, SdpWriteOptions options, SdpScope scope, int mediaIndex)
    {
        return AppendLosslessLine(builder, raw.Text, ReadPreservedLineEnding(raw.LineEnding), raw, buildLimit, result, options, scope, mediaIndex);
    }

    internal static bool AppendReplacementLine(StringBuilder builder, string line, SdpRawLine raw, long buildLimit, SdpWriteResult result, SdpWriteOptions options, SdpScope scope, int mediaIndex)
    {
        return AppendLosslessLine(builder, line, ReadPreservedLineEnding(raw.LineEnding), raw, buildLimit, result, options, scope, mediaIndex);
    }

    internal static bool AppendLosslessLine(StringBuilder builder, string line, string lineEnding, SdpRawLine raw, long buildLimit, SdpWriteResult result, SdpWriteOptions options, SdpScope scope, int mediaIndex)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(line))
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, raw.FieldName, string.Empty, mediaIndex, "An SDP field cannot be represented as a lossless line.", raw.Location);
            return false;
        }
        if (!SdpTrickleIceFragmentCore.AppendFragmentLine(builder, line, lineEnding, buildLimit))
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, scope, raw.FieldName, string.Empty, mediaIndex, "The generated SDP description exceeds the configured character limit.", raw.Location);
            return false;
        }
        return true;
    }

    internal static bool EnsureLineBoundary(StringBuilder builder, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options, SdpScope scope, int mediaIndex)
    {
        if (builder.Length == 0 || builder[builder.Length - 1] == '\r' || builder[builder.Length - 1] == '\n')
        {
            return true;
        }
        if (builder.Length + lineEnding.Length > buildLimit)
        {
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, scope, '\0', string.Empty, mediaIndex, "The generated SDP description exceeds the configured character limit.", new SdpSourceLocation());
            return false;
        }
        builder.Append(lineEnding);
        return true;
    }

    internal static string ReadPreservedLineEnding(SdpLineEnding ending)
    {
        if (ending == SdpLineEnding.CrLf)
        {
            return "\r\n";
        }
        if (ending == SdpLineEnding.Lf)
        {
            return "\n";
        }
        if (ending == SdpLineEnding.Cr)
        {
            return "\r";
        }
        return string.Empty;
    }

    internal static bool ShouldIncludeAttribute(SdpAttribute attribute, SdpWriteOptions options)
    {
        string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name);
        if (!options.IncludeObsoleteFields && (canonicalName == SdpAttributeNames.Category || canonicalName == SdpAttributeNames.Keywords))
        {
            return false;
        }
        if (!options.IncludeUnknownFields)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            return decoded.Recognized;
        }
        return true;
    }

    internal static SdpRepeatTime? ReadRepeatAt(List<SdpTiming> timings, int targetIndex)
    {
        int currentIndex = 0;
        int timingIndex = 0;
        while (timingIndex < timings.Count)
        {
            int repeatIndex = 0;
            while (repeatIndex < timings[timingIndex].RepeatTimes.Count)
            {
                if (currentIndex == targetIndex)
                {
                    return timings[timingIndex].RepeatTimes[repeatIndex];
                }
                currentIndex = currentIndex + 1;
                repeatIndex = repeatIndex + 1;
            }
            timingIndex = timingIndex + 1;
        }
        return null;
    }

    internal static bool OriginsEqual(SdpOrigin left, SdpOrigin right)
    {
        return string.Equals(left.Username, right.Username, StringComparison.Ordinal) && string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) && string.Equals(left.SessionVersion, right.SessionVersion, StringComparison.Ordinal) && string.Equals(left.NetworkType, right.NetworkType, StringComparison.Ordinal) && string.Equals(left.AddressType, right.AddressType, StringComparison.Ordinal) && string.Equals(left.UnicastAddress, right.UnicastAddress, StringComparison.Ordinal);
    }

    internal static bool ConnectionsEqual(SdpConnectionData left, SdpConnectionData right)
    {
        return string.Equals(left.NetworkType, right.NetworkType, StringComparison.Ordinal) && string.Equals(left.AddressType, right.AddressType, StringComparison.Ordinal) && string.Equals(left.Address, right.Address, StringComparison.Ordinal) && left.HasTimeToLive == right.HasTimeToLive && left.TimeToLive == right.TimeToLive && left.HasAddressCount == right.HasAddressCount && left.AddressCount == right.AddressCount;
    }

    internal static bool BandwidthsEqual(SdpBandwidth left, SdpBandwidth right)
    {
        return string.Equals(left.Type, right.Type, StringComparison.Ordinal) && left.Value == right.Value;
    }

    internal static bool RepeatsEqual(SdpRepeatTime left, SdpRepeatTime right)
    {
        if (!TypedTimesEqual(left.RepeatInterval, right.RepeatInterval) || !TypedTimesEqual(left.ActiveDuration, right.ActiveDuration) || left.Offsets.Count != right.Offsets.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Offsets.Count)
        {
            if (!TypedTimesEqual(left.Offsets[index], right.Offsets[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool ZonesEqual(List<SdpTimeZoneAdjustment> left, List<SdpTimeZoneAdjustment> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Count)
        {
            if (!string.Equals(left[index].AdjustmentTime, right[index].AdjustmentTime, StringComparison.Ordinal) || !TypedTimesEqual(left[index].Offset, right[index].Offset))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool TypedTimesEqual(SdpTypedTime left, SdpTypedTime right)
    {
        return left.Value == right.Value && string.Equals(left.Unit, right.Unit, StringComparison.Ordinal);
    }

    internal static bool KeysEqual(SdpEncryptionKey left, SdpEncryptionKey right)
    {
        return string.Equals(left.Method, right.Method, StringComparison.Ordinal) && left.HasValue == right.HasValue && string.Equals(left.Value, right.Value, StringComparison.Ordinal);
    }

    internal static bool AttributesEqual(SdpAttribute left, SdpAttribute right)
    {
        return left.Kind == right.Kind && string.Equals(left.Name, right.Name, StringComparison.Ordinal) && string.Equals(left.Value, right.Value, StringComparison.Ordinal);
    }

    internal static bool MediaHeadersEqual(SdpMediaDescription left, SdpMediaDescription right)
    {
        if (!string.Equals(left.MediaType, right.MediaType, StringComparison.Ordinal) || left.Port != right.Port || left.HasPortCount != right.HasPortCount || left.PortCount != right.PortCount || !string.Equals(left.Protocol, right.Protocol, StringComparison.Ordinal) || left.Formats.Count != right.Formats.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Formats.Count)
        {
            if (!string.Equals(left.Formats[index], right.Formats[index], StringComparison.Ordinal))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
