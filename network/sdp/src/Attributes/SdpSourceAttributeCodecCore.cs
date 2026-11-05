using System;
using System.Globalization;

namespace sdp;

internal static class SdpSourceAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeSourceAttribute(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc value must contain an unsigned source identifier and source attribute.");
        }

        string value = attribute.Value;
        int sourceIdStart = SkipSourceWhitespace(value, 0);
        int sourceIdEnd = ReadSourceTokenEnd(value, sourceIdStart, value.Length);
        int sourceAttributeStart = SkipSourceWhitespace(value, sourceIdEnd);
        if (sourceIdEnd == sourceIdStart || sourceAttributeStart == sourceIdEnd || sourceAttributeStart >= value.Length)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc value requires an unsigned source identifier followed by a source attribute.");
        }

        string sourceIdText = value.Substring(sourceIdStart, sourceIdEnd - sourceIdStart);
        SdpNumberParseResult sourceId = SdpNumberParser.ParseCharacterSsrc(sourceIdText.AsSpan());
        if (!sourceId.Success)
        {
            return FailSourceDecode(attribute, result, sourceId.DiagnosticCode, "The SDP ssrc identifier must be a decimal unsigned 32-bit integer.");
        }

        string sourceText = value.Substring(sourceAttributeStart);
        int colonIndex = sourceText.IndexOf(':');
        bool hasValue = colonIndex >= 0;
        if (!hasValue)
        {
            int sourceTextEnd = TrimSourceWhitespaceEnd(sourceText, 0, sourceText.Length);
            sourceText = sourceText.Substring(0, sourceTextEnd);
        }
        string sourceName = hasValue ? sourceText.Substring(0, colonIndex) : sourceText;
        string sourceValue = hasValue ? sourceText.Substring(colonIndex + 1) : string.Empty;
        string sourceError = ReadNestedSourceValidationError(sourceName, hasValue, sourceValue);
        if (sourceError.Length > 0)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, sourceError);
        }

        SdpSourceAttribute projection = new SdpSourceAttribute();
        projection.SourceAttribute = attribute;
        projection.SynchronizationSource = sourceId.UInt32Value;
        projection.Name = sourceName;
        projection.HasValue = hasValue;
        projection.Value = sourceValue;
        result.SynchronizationSourceAttribute = projection;
        result.TypedAttribute = projection;
        if (string.Equals(sourceName, SdpAttributeNames.MediaStreamId, StringComparison.OrdinalIgnoreCase))
        {
            result.MediaStreamId = CreateMediaStreamIdProjection(attribute, sourceValue, true, sourceId.UInt32Value);
        }
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSourceAttribute(SdpSourceAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string validationError = ReadNestedSourceValidationError(projection.Name, projection.HasValue, projection.Value);
        if (validationError.Length > 0)
        {
            result.Diagnostics.Add(CreateSourceEncodeDiagnostic(SdpAttributeNames.SynchronizationSource, SdpDiagnosticCodes.ValidationAttribute, validationError));
            return result;
        }

        string value = projection.SynchronizationSource.ToString(CultureInfo.InvariantCulture) + " " + projection.Name;
        if (projection.HasValue)
        {
            value = value + ":" + projection.Value;
        }
        result.Attribute = CreateSourceValueAttribute(SdpAttributeNames.SynchronizationSource, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeSourceGroup(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc-group attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc-group value must contain a semantic and at least one source identifier.");
        }

        string[] tokens = ReadSourceTokens(attribute.Value);
        if (tokens.Length < 2 || !SdpCommonAttributeCodecCore.IsTokenValue(tokens[0]))
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc-group value requires a token semantic and at least one source identifier.");
        }

        SdpSourceGroupAttribute projection = new SdpSourceGroupAttribute();
        projection.SourceAttribute = attribute;
        projection.Semantic = tokens[0];
        int index = 1;
        while (index < tokens.Length)
        {
            SdpNumberParseResult sourceId = SdpNumberParser.ParseCharacterSsrc(tokens[index].AsSpan());
            if (!sourceId.Success)
            {
                return FailSourceDecode(attribute, result, sourceId.DiagnosticCode, "Every SDP ssrc-group member must be a decimal unsigned 32-bit integer.");
            }
            projection.SynchronizationSources.Add(sourceId.UInt32Value);
            index = index + 1;
        }

        result.SourceGroupAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSourceGroup(SdpSourceGroupAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.Semantic) || projection.SynchronizationSources.Count == 0)
        {
            result.Diagnostics.Add(CreateSourceEncodeDiagnostic(SdpAttributeNames.SynchronizationSourceGroup, SdpDiagnosticCodes.ValidationAttribute, "The SDP ssrc-group value requires a token semantic and at least one source identifier."));
            return result;
        }

        string value = projection.Semantic;
        int index = 0;
        while (index < projection.SynchronizationSources.Count)
        {
            value = value + " " + projection.SynchronizationSources[index].ToString(CultureInfo.InvariantCulture);
            index = index + 1;
        }
        result.Attribute = CreateSourceValueAttribute(SdpAttributeNames.SynchronizationSourceGroup, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeMediaStreamId(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP msid attribute requires a colon-delimited value.");
        }
        string validationError = ReadMediaStreamIdValidationError(attribute.Value);
        if (validationError.Length > 0)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, validationError);
        }

        SdpMediaStreamId projection = CreateMediaStreamIdProjection(attribute, attribute.Value, false, 0);
        result.MediaStreamId = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMediaStreamId(SdpMediaStreamId projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!projection.HasTrackId && projection.TrackId.Length > 0)
        {
            result.Diagnostics.Add(CreateSourceEncodeDiagnostic(SdpAttributeNames.MediaStreamId, SdpDiagnosticCodes.ValidationAttribute, "An SDP msid track identifier requires HasTrackId to be enabled."));
            return result;
        }

        string value = projection.StreamId;
        if (projection.HasTrackId)
        {
            value = value + " " + projection.TrackId;
        }
        string validationError = ReadMediaStreamIdValidationError(value);
        if (validationError.Length > 0)
        {
            result.Diagnostics.Add(CreateSourceEncodeDiagnostic(SdpAttributeNames.MediaStreamId, SdpDiagnosticCodes.ValidationAttribute, validationError));
            return result;
        }

        if (projection.SourceLevel)
        {
            string sourceValue = projection.SynchronizationSource.ToString(CultureInfo.InvariantCulture) + " " + SdpAttributeNames.MediaStreamId + ":" + value;
            result.Attribute = CreateSourceValueAttribute(SdpAttributeNames.SynchronizationSource, sourceValue);
        }
        else
        {
            result.Attribute = CreateSourceValueAttribute(SdpAttributeNames.MediaStreamId, value);
        }
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeMediaStreamSemantic(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP msid-semantic attribute requires a colon-delimited value.");
        }
        string validationError = ReadMediaStreamSemanticValidationError(attribute.Value);
        if (validationError.Length > 0)
        {
            return FailSourceDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, validationError);
        }

        string[] tokens = ReadSourceTokens(attribute.Value);
        SdpMediaStreamSemanticAttribute projection = new SdpMediaStreamSemanticAttribute();
        projection.SourceAttribute = attribute;
        projection.Semantic = tokens[0];
        int index = 1;
        while (index < tokens.Length)
        {
            projection.StreamIds.Add(tokens[index]);
            index = index + 1;
        }
        result.MediaStreamSemanticAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMediaStreamSemantic(SdpMediaStreamSemanticAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string value = projection.Semantic;
        int index = 0;
        while (index < projection.StreamIds.Count)
        {
            value = value + " " + projection.StreamIds[index];
            index = index + 1;
        }
        string validationError = ReadMediaStreamSemanticValidationError(value);
        if (validationError.Length > 0)
        {
            result.Diagnostics.Add(CreateSourceEncodeDiagnostic(SdpAttributeNames.MediaStreamIdSemantic, SdpDiagnosticCodes.ValidationAttribute, validationError));
            return result;
        }

        result.Attribute = CreateSourceValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, value);
        result.Success = true;
        return result;
    }

    internal static string ReadNestedSourceValidationError(string name, bool hasValue, string value)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
        {
            return "The SDP ssrc source attribute name must use non-empty token syntax.";
        }
        if (hasValue && !SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return "An SDP ssrc source attribute value must not be empty or contain prohibited line-ending characters.";
        }
        if (!hasValue && value.Length > 0)
        {
            return "An SDP ssrc source attribute value requires HasValue to be enabled.";
        }
        if (string.Equals(name, "cname", StringComparison.OrdinalIgnoreCase) && !hasValue)
        {
            return "The SDP ssrc cname source attribute requires a value.";
        }
        if (string.Equals(name, SdpAttributeNames.MediaStreamId, StringComparison.OrdinalIgnoreCase))
        {
            if (!hasValue)
            {
                return "The SDP ssrc msid source attribute requires a value.";
            }
            return ReadMediaStreamIdValidationError(value);
        }
        return string.Empty;
    }

    internal static string ReadMediaStreamIdValidationError(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return "The SDP msid value must contain a media-stream identifier.";
        }
        string[] tokens = ReadSourceTokens(value);
        if (tokens.Length < 1 || tokens.Length > 2)
        {
            return "The SDP msid value requires one stream identifier and at most one track identifier.";
        }
        if (!IsMediaStreamIdentifier(tokens[0]))
        {
            return "The SDP msid stream identifier must contain from one through 64 token characters.";
        }
        if (tokens.Length == 2 && !IsMediaStreamIdentifier(tokens[1]))
        {
            return "The SDP msid track identifier must contain from one through 64 token characters.";
        }
        return string.Empty;
    }

    internal static string ReadMediaStreamSemanticValidationError(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return "The SDP msid-semantic value must contain a semantic and at least one stream identifier.";
        }
        string[] tokens = ReadSourceTokens(value);
        if (tokens.Length < 2 || !SdpCommonAttributeCodecCore.IsTokenValue(tokens[0]))
        {
            return "The SDP msid-semantic value requires a token semantic and at least one stream identifier.";
        }
        bool wildcard = false;
        int index = 1;
        while (index < tokens.Length)
        {
            if (!IsMediaStreamIdentifier(tokens[index]))
            {
                return "Every SDP msid-semantic stream identifier must contain from one through 64 token characters.";
            }
            if (tokens[index] == "*")
            {
                wildcard = true;
            }
            index = index + 1;
        }
        if (wildcard && tokens.Length != 2)
        {
            return "The SDP msid-semantic wildcard must be the only stream identifier.";
        }
        return string.Empty;
    }

    internal static bool IsMediaStreamIdentifier(string value)
    {
        return value.Length <= 64 && SdpCommonAttributeCodecCore.IsTokenValue(value);
    }

    internal static SdpMediaStreamId CreateMediaStreamIdProjection(SdpAttribute source, string value, bool sourceLevel, uint synchronizationSource)
    {
        string[] tokens = ReadSourceTokens(value);
        SdpMediaStreamId projection = new SdpMediaStreamId();
        projection.SourceAttribute = source;
        projection.StreamId = tokens[0];
        if (tokens.Length == 2)
        {
            projection.HasTrackId = true;
            projection.TrackId = tokens[1];
        }
        projection.SourceLevel = sourceLevel;
        projection.SynchronizationSource = synchronizationSource;
        return projection;
    }

    internal static string[] ReadSourceTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static int SkipSourceWhitespace(string value, int start)
    {
        int index = start;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t'))
        {
            index = index + 1;
        }
        return index;
    }

    internal static int ReadSourceTokenEnd(string value, int start, int end)
    {
        int index = start;
        while (index < end && value[index] != ' ' && value[index] != '\t')
        {
            index = index + 1;
        }
        return index;
    }

    internal static int TrimSourceWhitespaceEnd(string value, int start, int end)
    {
        int index = end;
        while (index > start && (value[index - 1] == ' ' || value[index - 1] == '\t'))
        {
            index = index - 1;
        }
        return index;
    }

    internal static SdpAttribute CreateSourceValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailSourceDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateSourceEncodeDiagnostic(string attributeName, string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
