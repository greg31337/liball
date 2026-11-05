using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpSimulcastAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeSimulcast(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSimulcastDecode(attribute, result, "The SDP simulcast attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailSimulcastDecode(attribute, result, "The SDP simulcast value must not be empty or contain prohibited line-ending characters.");
        }

        SdpSimulcastAttribute projection = new SdpSimulcastAttribute();
        projection.SourceAttribute = attribute;
        projection.RawValue = attribute.Value;
        if (ParseModernSimulcast(attribute.Value, projection))
        {
            result.SimulcastAttribute = projection;
            result.TypedAttribute = projection;
            return result;
        }
        projection.SendStreams.Clear();
        projection.ReceiveStreams.Clear();
        if (IsLegacySimulcastValue(attribute.Value))
        {
            projection.LegacySyntax = true;
            result.SimulcastAttribute = projection;
            result.TypedAttribute = projection;
            return result;
        }
        return FailSimulcastDecode(attribute, result, "The SDP simulcast value must use RFC 8853 stream-list syntax or a supported legacy draft form.");
    }

    internal static bool ParseModernSimulcast(string value, SdpSimulcastAttribute projection)
    {
        string[] tokens = value.Split(' ');
        if (tokens.Length != 2 && tokens.Length != 4)
        {
            return false;
        }
        if (!IsSimulcastDirection(tokens[0]) || tokens[1].Length == 0)
        {
            return false;
        }
        if (tokens.Length == 4 && (!IsSimulcastDirection(tokens[2]) || tokens[3].Length == 0 || tokens[0] == tokens[2]))
        {
            return false;
        }

        projection.SendFirst = tokens[0] == "send";
        List<SdpSimulcastStream> firstStreams = projection.SendFirst ? projection.SendStreams : projection.ReceiveStreams;
        if (!ParseModernStreamList(tokens[1], firstStreams))
        {
            return false;
        }
        if (tokens.Length == 4)
        {
            List<SdpSimulcastStream> secondStreams = projection.SendFirst ? projection.ReceiveStreams : projection.SendStreams;
            if (!ParseModernStreamList(tokens[3], secondStreams))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool ParseModernStreamList(string value, List<SdpSimulcastStream> streams)
    {
        int streamStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ';')
            {
                if (streamStart == index)
                {
                    return false;
                }
                SdpSimulcastStream stream = new SdpSimulcastStream();
                if (!ParseModernAlternatives(value.Substring(streamStart, index - streamStart), stream))
                {
                    return false;
                }
                streams.Add(stream);
                streamStart = index + 1;
            }
            index = index + 1;
        }
        return streams.Count > 0;
    }

    internal static bool ParseModernAlternatives(string value, SdpSimulcastStream stream)
    {
        int alternativeStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ',')
            {
                if (alternativeStart == index)
                {
                    return false;
                }
                string identifier = value.Substring(alternativeStart, index - alternativeStart);
                bool paused = identifier[0] == '~';
                if (paused)
                {
                    identifier = identifier.Substring(1);
                }
                if (!SdpRidAttributeCodecCore.IsRidIdentifier(identifier))
                {
                    return false;
                }
                SdpSimulcastAlternative alternative = new SdpSimulcastAlternative();
                alternative.RidId = identifier;
                alternative.Paused = paused;
                stream.Alternatives.Add(alternative);
                alternativeStart = index + 1;
            }
            index = index + 1;
        }
        return stream.Alternatives.Count > 0;
    }

    internal static SdpAttributeEncodeResult EncodeSimulcast(SdpSimulcastAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.SendStreams == null || projection.ReceiveStreams == null)
        {
            result.Diagnostics.Add(CreateSimulcastEncodeDiagnostic("The SDP simulcast send and receive stream collections must be available."));
            return result;
        }
        if (projection.LegacySyntax)
        {
            if (projection.SendStreams.Count > 0 || projection.ReceiveStreams.Count > 0 || !IsLegacySimulcastValue(projection.RawValue))
            {
                result.Diagnostics.Add(CreateSimulcastEncodeDiagnostic("Legacy SDP simulcast fallback requires an unchanged valid raw value and no projected modern streams."));
                return result;
            }
            result.Attribute = CreateSimulcastAttribute(projection.RawValue);
            result.Success = true;
            return result;
        }
        if (projection.SendStreams.Count == 0 && projection.ReceiveStreams.Count == 0)
        {
            result.Diagnostics.Add(CreateSimulcastEncodeDiagnostic("The SDP simulcast attribute requires at least one send or receive stream."));
            return result;
        }

        string sendList = string.Empty;
        if (projection.SendStreams.Count > 0)
        {
            sendList = EncodeModernStreamList(projection.SendStreams);
            if (sendList.Length == 0)
            {
                result.Diagnostics.Add(CreateSimulcastEncodeDiagnostic("Every SDP simulcast send stream requires one or more valid RID alternatives."));
                return result;
            }
        }
        string receiveList = string.Empty;
        if (projection.ReceiveStreams.Count > 0)
        {
            receiveList = EncodeModernStreamList(projection.ReceiveStreams);
            if (receiveList.Length == 0)
            {
                result.Diagnostics.Add(CreateSimulcastEncodeDiagnostic("Every SDP simulcast receive stream requires one or more valid RID alternatives."));
                return result;
            }
        }

        string value;
        if (sendList.Length > 0 && receiveList.Length > 0)
        {
            value = projection.SendFirst ? "send " + sendList + " recv " + receiveList : "recv " + receiveList + " send " + sendList;
        }
        else if (sendList.Length > 0)
        {
            value = "send " + sendList;
        }
        else
        {
            value = "recv " + receiveList;
        }
        result.Attribute = CreateSimulcastAttribute(value);
        result.Success = true;
        return result;
    }

    internal static string EncodeModernStreamList(List<SdpSimulcastStream> streams)
    {
        string value = string.Empty;
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            SdpSimulcastStream stream = streams[streamIndex];
            if (stream == null || stream.Alternatives == null || stream.Alternatives.Count == 0)
            {
                return string.Empty;
            }
            if (streamIndex > 0)
            {
                value = value + ";";
            }
            int alternativeIndex = 0;
            while (alternativeIndex < stream.Alternatives.Count)
            {
                SdpSimulcastAlternative alternative = stream.Alternatives[alternativeIndex];
                if (alternative == null || !SdpRidAttributeCodecCore.IsRidIdentifier(alternative.RidId))
                {
                    return string.Empty;
                }
                if (alternativeIndex > 0)
                {
                    value = value + ",";
                }
                if (alternative.Paused)
                {
                    value = value + "~";
                }
                value = value + alternative.RidId;
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
        return value;
    }

    internal static bool IsLegacySimulcastValue(string value)
    {
        if (value == null || value.Length < 2 || value[0] != ' ' && value[0] != '\t')
        {
            return false;
        }
        string[] tokens = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2 || tokens.Length > 5 || !IsLegacyDirection(tokens[0]) || !IsLegacyDirectionList(tokens[1]))
        {
            return false;
        }
        int index = 2;
        if (index < tokens.Length && IsLegacyDirection(tokens[index]))
        {
            if (string.Equals(tokens[index], tokens[0], StringComparison.OrdinalIgnoreCase) || index + 1 >= tokens.Length || !IsLegacyDirectionList(tokens[index + 1]))
            {
                return false;
            }
            index = index + 2;
        }
        if (index < tokens.Length)
        {
            if (index + 1 != tokens.Length || !IsLegacyPauseList(tokens[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return index == tokens.Length;
    }

    internal static bool IsLegacyDirectionList(string value)
    {
        int equalsIndex = value.IndexOf('=');
        if (equalsIndex <= 0 || equalsIndex + 1 >= value.Length || value.IndexOf('=', equalsIndex + 1) >= 0)
        {
            return false;
        }
        string identificationType = value.Substring(0, equalsIndex);
        string streamList = value.Substring(equalsIndex + 1);
        return SdpCommonAttributeCodecCore.IsTokenValue(identificationType) && IsLegacyIdentifierList(streamList, true);
    }

    internal static bool IsLegacyPauseList(string value)
    {
        if (value.Length <= 7 || !string.Equals(value.Substring(0, 7), "paused=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return IsLegacyIdentifierList(value.Substring(7), false);
    }

    internal static bool IsLegacyIdentifierList(string value, bool allowStreams)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int identifierStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            bool separator = index < value.Length && (value[index] == ',' || allowStreams && value[index] == ';');
            if (index == value.Length || separator)
            {
                if (identifierStart == index || !SdpCommonAttributeCodecCore.IsTokenValue(value.Substring(identifierStart, index - identifierStart)))
                {
                    return false;
                }
                identifierStart = index + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsSimulcastDirection(string value)
    {
        return value == "send" || value == "recv";
    }

    internal static bool IsLegacyDirection(string value)
    {
        return string.Equals(value, "send", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "recv", StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpAttribute CreateSimulcastAttribute(string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.Simulcast;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.Simulcast + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailSimulcastDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateSimulcastEncodeDiagnostic(string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.Simulcast, detail);
    }
}
