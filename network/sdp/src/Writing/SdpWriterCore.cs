using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace sdp;

internal static class SdpWriterCore
{
    internal static SdpWriteResult WriteText(SdpSessionDescription description, SdpWriteOptions options)
    {
        SdpTrickleIceFragmentCore.ValidateWriteOptions(options);
        SdpWriteResult result = new SdpWriteResult();
        result.AppliedOptions = SdpTrickleIceFragmentCore.CopyWriteOptions(options);
        if (!SdpGenerationValidationCore.ValidateGenerationDescription(description, options, result))
        {
            return result;
        }
        if (options.Mode == SdpWriteMode.Lossless)
        {
            return SdpLosslessWriterCore.WriteLosslessText(description, options, result);
        }

        string lineEnding = SdpTrickleIceFragmentCore.ReadLineEnding(options.LineEnding);
        long buildLimit = (long)options.MaximumOutputCharacters + lineEnding.Length;
        StringBuilder builder = new StringBuilder();
        if (!WriteSessionFields(builder, description, options, lineEnding, buildLimit, result))
        {
            return result;
        }
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (!WriteMediaFields(builder, description.MediaDescriptions[mediaIndex], mediaIndex, options, lineEnding, buildLimit, result))
            {
                return result;
            }
            mediaIndex = mediaIndex + 1;
        }
        if (!options.IncludeFinalLineEnding && builder.Length >= lineEnding.Length)
        {
            builder.Remove(builder.Length - lineEnding.Length, lineEnding.Length);
        }
        if (builder.Length > options.MaximumOutputCharacters)
        {
            AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Session, '\0', string.Empty, -1, "The generated SDP description exceeds the configured character limit.", new SdpSourceLocation());
            return result;
        }
        result.Text = builder.ToString();
        result.CharactersWritten = result.Text.Length;
        result.Success = true;
        return result;
    }

    internal static bool WriteSessionFields(StringBuilder builder, SdpSessionDescription description, SdpWriteOptions options, string lineEnding, long buildLimit, SdpWriteResult result)
    {
        if (!AppendLine(builder, "v=" + description.ProtocolVersion.ToString(CultureInfo.InvariantCulture), lineEnding, buildLimit, result, options, SdpScope.Session, 'v', -1, new SdpSourceLocation()))
        {
            return false;
        }
        SdpOrigin origin = description.Origin == null ? new SdpOrigin() : description.Origin;
        string originLine = "o=" + origin.Username + " " + origin.SessionId + " " + origin.SessionVersion + " " + origin.NetworkType + " " + origin.AddressType + " " + origin.UnicastAddress;
        if (!AppendLine(builder, originLine, lineEnding, buildLimit, result, options, SdpScope.Session, 'o', -1, origin.Location))
        {
            return false;
        }
        if (!AppendLine(builder, "s=" + description.SessionName, lineEnding, buildLimit, result, options, SdpScope.Session, 's', -1, new SdpSourceLocation()))
        {
            return false;
        }
        if (description.Information != null && !AppendLine(builder, "i=" + description.Information, lineEnding, buildLimit, result, options, SdpScope.Session, 'i', -1, new SdpSourceLocation()))
        {
            return false;
        }
        if (description.Uri != null && !AppendLine(builder, "u=" + description.Uri, lineEnding, buildLimit, result, options, SdpScope.Session, 'u', -1, new SdpSourceLocation()))
        {
            return false;
        }
        if (!WriteTextFields(builder, description.Emails, 'e', lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (!WriteTextFields(builder, description.Phones, 'p', lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (description.Connection != null && !WriteConnection(builder, description.Connection, SdpScope.Session, -1, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (!WriteBandwidths(builder, description.Bandwidths, SdpScope.Session, -1, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (!WriteTimings(builder, description.Timings, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (!WriteTimeZones(builder, description.TimeZoneAdjustments, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (options.IncludeObsoleteFields && description.EncryptionKey != null && !WriteEncryptionKey(builder, description.EncryptionKey, SdpScope.Session, -1, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        return WriteAttributes(builder, description.Attributes, SdpScope.Session, -1, lineEnding, buildLimit, result, options);
    }

    internal static bool WriteMediaFields(StringBuilder builder, SdpMediaDescription media, int mediaIndex, SdpWriteOptions options, string lineEnding, long buildLimit, SdpWriteResult result)
    {
        string port = media.Port.ToString(CultureInfo.InvariantCulture);
        if (media.HasPortCount)
        {
            port = port + "/" + media.PortCount.ToString(CultureInfo.InvariantCulture);
        }
        string line = "m=" + media.MediaType + " " + port + " " + media.Protocol;
        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            line = line + " " + media.Formats[formatIndex];
            formatIndex = formatIndex + 1;
        }
        if (!AppendLine(builder, line, lineEnding, buildLimit, result, options, SdpScope.Media, 'm', mediaIndex, media.Location))
        {
            return false;
        }
        if (media.Information != null && !AppendLine(builder, "i=" + media.Information, lineEnding, buildLimit, result, options, SdpScope.Media, 'i', mediaIndex, media.Location))
        {
            return false;
        }
        int connectionIndex = 0;
        while (connectionIndex < media.Connections.Count)
        {
            if (!WriteConnection(builder, media.Connections[connectionIndex], SdpScope.Media, mediaIndex, lineEnding, buildLimit, result, options))
            {
                return false;
            }
            connectionIndex = connectionIndex + 1;
        }
        if (!WriteBandwidths(builder, media.Bandwidths, SdpScope.Media, mediaIndex, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        if (options.IncludeObsoleteFields && media.EncryptionKey != null && !WriteEncryptionKey(builder, media.EncryptionKey, SdpScope.Media, mediaIndex, lineEnding, buildLimit, result, options))
        {
            return false;
        }
        return WriteAttributes(builder, media.Attributes, SdpScope.Media, mediaIndex, lineEnding, buildLimit, result, options);
    }

    internal static bool WriteTextFields(StringBuilder builder, List<string> values, char fieldName, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (!AppendLine(builder, fieldName + "=" + values[index], lineEnding, buildLimit, result, options, SdpScope.Session, fieldName, -1, new SdpSourceLocation()))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool WriteConnection(StringBuilder builder, SdpConnectionData connection, SdpScope scope, int mediaIndex, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        string address = connection.Address;
        if (connection.HasTimeToLive)
        {
            address = address + "/" + connection.TimeToLive.ToString(CultureInfo.InvariantCulture);
        }
        if (connection.HasAddressCount)
        {
            address = address + "/" + connection.AddressCount.ToString(CultureInfo.InvariantCulture);
        }
        string line = "c=" + connection.NetworkType + " " + connection.AddressType + " " + address;
        return AppendLine(builder, line, lineEnding, buildLimit, result, options, scope, 'c', mediaIndex, connection.Location);
    }

    internal static bool WriteBandwidths(StringBuilder builder, List<SdpBandwidth> bandwidths, SdpScope scope, int mediaIndex, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            SdpBandwidth bandwidth = bandwidths[index];
            string line = "b=" + bandwidth.Type + ":" + bandwidth.Value.ToString(CultureInfo.InvariantCulture);
            if (!AppendLine(builder, line, lineEnding, buildLimit, result, options, scope, 'b', mediaIndex, bandwidth.Location))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool WriteTimings(StringBuilder builder, List<SdpTiming> timings, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        int timingIndex = 0;
        while (timingIndex < timings.Count)
        {
            SdpTiming timing = timings[timingIndex];
            if (!AppendLine(builder, "t=" + timing.StartTime + " " + timing.StopTime, lineEnding, buildLimit, result, options, SdpScope.Session, 't', -1, timing.Location))
            {
                return false;
            }
            int repeatIndex = 0;
            while (repeatIndex < timing.RepeatTimes.Count)
            {
                SdpRepeatTime repeat = timing.RepeatTimes[repeatIndex];
                string repeatLine = "r=" + ReadTypedTime(repeat.RepeatInterval) + " " + ReadTypedTime(repeat.ActiveDuration);
                int offsetIndex = 0;
                while (offsetIndex < repeat.Offsets.Count)
                {
                    repeatLine = repeatLine + " " + ReadTypedTime(repeat.Offsets[offsetIndex]);
                    offsetIndex = offsetIndex + 1;
                }
                if (!AppendLine(builder, repeatLine, lineEnding, buildLimit, result, options, SdpScope.Session, 'r', -1, repeat.Location))
                {
                    return false;
                }
                repeatIndex = repeatIndex + 1;
            }
            timingIndex = timingIndex + 1;
        }
        return true;
    }

    internal static bool WriteTimeZones(StringBuilder builder, List<SdpTimeZoneAdjustment> adjustments, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        if (adjustments.Count == 0)
        {
            return true;
        }
        string line = "z=";
        int index = 0;
        while (index < adjustments.Count)
        {
            if (index > 0)
            {
                line = line + " ";
            }
            line = line + adjustments[index].AdjustmentTime + " " + ReadTypedTime(adjustments[index].Offset);
            index = index + 1;
        }
        return AppendLine(builder, line, lineEnding, buildLimit, result, options, SdpScope.Session, 'z', -1, adjustments[0].Location);
    }

    internal static string ReadTypedTime(SdpTypedTime value)
    {
        SdpTypedTimeFormatResult formatted = SdpTypedTimeParser.FormatCanonicalTypedTime(value);
        return formatted.Text;
    }

    internal static bool WriteEncryptionKey(StringBuilder builder, SdpEncryptionKey key, SdpScope scope, int mediaIndex, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        string line = "k=" + key.Method;
        if (key.HasValue)
        {
            line = line + ":" + key.Value;
        }
        return AppendLine(builder, line, lineEnding, buildLimit, result, options, scope, 'k', mediaIndex, key.Location);
    }

    internal static bool WriteAttributes(StringBuilder builder, List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name);
            if (!options.IncludeObsoleteFields && (canonicalName == SdpAttributeNames.Category || canonicalName == SdpAttributeNames.Keywords))
            {
                index = index + 1;
                continue;
            }
            SdpAttribute outputAttribute = attribute;
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            if (!decoded.Recognized)
            {
                if (!options.IncludeUnknownFields)
                {
                    index = index + 1;
                    continue;
                }
            }
            else
            {
                if (!decoded.Success || decoded.TypedAttribute == null)
                {
                    AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, 'a', attribute.Name, mediaIndex, ReadAttributeDecodeFailure(decoded), attribute.Location);
                    return false;
                }
                SdpAttributeEncodeResult encoded = SdpAttributeEncoderCore.EncodeAttribute(decoded.TypedAttribute);
                if (!encoded.Success || encoded.Attribute == null)
                {
                    AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, 'a', attribute.Name, mediaIndex, ReadAttributeEncodeFailure(encoded), attribute.Location);
                    return false;
                }
                outputAttribute = encoded.Attribute;
            }
            string line = SdpTrickleIceFragmentCore.ReadAttributeLine(outputAttribute);
            if (!AppendLine(builder, line, lineEnding, buildLimit, result, options, scope, 'a', mediaIndex, attribute.Location))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string ReadAttributeDecodeFailure(SdpAttributeDecodeResult decoded)
    {
        if (decoded.Diagnostics.Count > 0 && decoded.Diagnostics[0].Detail.Length > 0)
        {
            return decoded.Diagnostics[0].Detail;
        }
        return "The recognized SDP attribute cannot be decoded for canonical generation.";
    }

    internal static string ReadAttributeEncodeFailure(SdpAttributeEncodeResult encoded)
    {
        if (encoded.Diagnostics.Count > 0 && encoded.Diagnostics[0].Detail.Length > 0)
        {
            return encoded.Diagnostics[0].Detail;
        }
        return "The recognized SDP attribute cannot be encoded for canonical generation.";
    }

    internal static bool AppendLine(StringBuilder builder, string line, string lineEnding, long buildLimit, SdpWriteResult result, SdpWriteOptions options, SdpScope scope, char fieldName, int mediaIndex, SdpSourceLocation location)
    {
        if (line.Length == 0 || !SdpCommonAttributeCodecCore.IsByteStringValue(line))
        {
            AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, scope, fieldName, string.Empty, mediaIndex, "An SDP field cannot be represented as a canonical line.", location);
            return false;
        }
        if (!SdpTrickleIceFragmentCore.AppendFragmentLine(builder, line, lineEnding, buildLimit))
        {
            AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, scope, fieldName, string.Empty, mediaIndex, "The generated SDP description exceeds the configured character limit.", location);
            return false;
        }
        return true;
    }

    internal static void CopyValidationDiagnostics(SdpWriteResult result, SdpValidationResult validation, SdpWriteOptions options)
    {
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, options.MaximumDiagnosticCount);
        int index = 0;
        while (index < validation.Issues.Count && !limiter.DiagnosticLimitReached)
        {
            SdpValidationIssue issue = validation.Issues[index];
            if (SdpGenerationValidationCore.ShouldApplyValidationIssue(issue, options))
            {
                SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(SdpDiagnosticCodes.WriteInvalidDescription, issue.Severity, issue.Scope, issue.FieldName, issue.AttributeName, issue.MediaIndex, issue.Detail, SdpDiagnosticBuilder.CopySourcePath(issue.Path), SdpDiagnosticBuilder.CopySourceLocation(issue.Location));
                limiter.AddDiagnostic(diagnostic);
            }
            index = index + 1;
        }
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached || validation.DiagnosticLimitReached;
    }

    internal static void AddWriteDiagnostic(SdpWriteResult result, SdpWriteOptions options, string code, SdpScope scope, char fieldName, string attributeName, int mediaIndex, string detail, SdpSourceLocation location)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = fieldName;
        path.AttributeName = attributeName;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(code, SdpValidationSeverity.Error, scope, fieldName, attributeName, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, options.MaximumDiagnosticCount);
        limiter.AddDiagnostic(diagnostic);
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
    }

    internal static string ReadFailureCode(SdpWriteResult result)
    {
        if (result.Diagnostics.Count > 0 && result.Diagnostics[0].Code != null && result.Diagnostics[0].Code.Length > 0)
        {
            return result.Diagnostics[0].Code;
        }
        return SdpDiagnosticCodes.WriteInvalidDescription;
    }
}
