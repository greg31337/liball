using System;

namespace sdp;

internal static class SdpGroupAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeGroup(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailGroupDecode(attribute, result, "The SDP group attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailGroupDecode(attribute, result, "The SDP group value must contain a grouping semantic.");
        }

        string[] tokens = ReadGroupTokens(attribute.Value);
        if (tokens.Length == 0 || !SdpCommonAttributeCodecCore.IsTokenValue(tokens[0]))
        {
            return FailGroupDecode(attribute, result, "The SDP group semantic must use non-empty token syntax.");
        }

        SdpGroupAttribute projection = new SdpGroupAttribute();
        projection.SourceAttribute = attribute;
        projection.SemanticToken = tokens[0];
        projection.Semantic = ReadGroupSemantic(tokens[0]);
        int index = 1;
        while (index < tokens.Length)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[index]))
            {
                return FailGroupDecode(attribute, result, "Every SDP group identification tag must use non-empty token syntax.");
            }
            projection.IdentificationTags.Add(tokens[index]);
            index = index + 1;
        }

        result.GroupAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeGroup(SdpGroupAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string semanticToken = ReadGroupSemanticToken(projection);
        if (semanticToken.Length == 0)
        {
            result.Diagnostics.Add(CreateGroupEncodeDiagnostic(SdpAttributeNames.Group, "The SDP group semantic kind and semantic token are invalid or inconsistent."));
            return result;
        }

        string value = semanticToken;
        int index = 0;
        while (index < projection.IdentificationTags.Count)
        {
            string tag = projection.IdentificationTags[index];
            if (!SdpCommonAttributeCodecCore.IsTokenValue(tag))
            {
                result.Diagnostics.Add(CreateGroupEncodeDiagnostic(SdpAttributeNames.Group, "Every SDP group identification tag must use non-empty token syntax."));
                return result;
            }
            value = value + " " + tag;
            index = index + 1;
        }

        result.Attribute = CreateGroupValueAttribute(SdpAttributeNames.Group, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeMediaId(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailGroupDecode(attribute, result, "The SDP mid attribute requires a colon-delimited value.");
        }
        string mediaId = TrimGroupWhitespace(attribute.Value);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(mediaId))
        {
            return FailGroupDecode(attribute, result, "The SDP mid identification tag must use non-empty token syntax.");
        }

        SdpMediaIdAttribute projection = new SdpMediaIdAttribute();
        projection.SourceAttribute = attribute;
        projection.Id = mediaId;
        result.MediaIdAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMediaId(SdpMediaIdAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.Id))
        {
            result.Diagnostics.Add(CreateGroupEncodeDiagnostic(SdpAttributeNames.MediaId, "The SDP mid identification tag must use non-empty token syntax."));
            return result;
        }

        result.Attribute = CreateGroupValueAttribute(SdpAttributeNames.MediaId, projection.Id);
        result.Success = true;
        return result;
    }

    internal static SdpGroupSemantic ReadGroupSemantic(string token)
    {
        if (token == SdpGroupSemantics.LipSynchronization)
        {
            return SdpGroupSemantic.LipSynchronization;
        }
        if (token == SdpGroupSemantics.FlowIdentification)
        {
            return SdpGroupSemantic.FlowIdentification;
        }
        if (token == SdpGroupSemantics.SingleReservationFlow)
        {
            return SdpGroupSemantic.SingleReservationFlow;
        }
        if (token == SdpGroupSemantics.AlternativeNetworkAddressTypes)
        {
            return SdpGroupSemantic.AlternativeNetworkAddressTypes;
        }
        if (token == SdpGroupSemantics.ForwardErrorCorrection)
        {
            return SdpGroupSemantic.ForwardErrorCorrection;
        }
        if (token == SdpGroupSemantics.ForwardErrorCorrectionFramework)
        {
            return SdpGroupSemantic.ForwardErrorCorrectionFramework;
        }
        if (token == SdpGroupSemantics.CompositeSession)
        {
            return SdpGroupSemantic.CompositeSession;
        }
        if (token == SdpGroupSemantics.DecodingDependency)
        {
            return SdpGroupSemantic.DecodingDependency;
        }
        if (token == SdpGroupSemantics.Duplication)
        {
            return SdpGroupSemantic.Duplication;
        }
        if (token == SdpGroupSemantics.Splice)
        {
            return SdpGroupSemantic.Splice;
        }
        if (token == SdpGroupSemantics.Bundle)
        {
            return SdpGroupSemantic.Bundle;
        }
        if (token == SdpGroupSemantics.Clue)
        {
            return SdpGroupSemantic.Clue;
        }
        if (token == SdpGroupSemantics.V3c)
        {
            return SdpGroupSemantic.VolumetricVideo;
        }
        return SdpGroupSemantic.Unknown;
    }

    internal static string ReadGroupSemanticToken(SdpGroupAttribute projection)
    {
        string expected = string.Empty;
        if (projection.Semantic == SdpGroupSemantic.LipSynchronization)
        {
            expected = SdpGroupSemantics.LipSynchronization;
        }
        else if (projection.Semantic == SdpGroupSemantic.FlowIdentification)
        {
            expected = SdpGroupSemantics.FlowIdentification;
        }
        else if (projection.Semantic == SdpGroupSemantic.SingleReservationFlow)
        {
            expected = SdpGroupSemantics.SingleReservationFlow;
        }
        else if (projection.Semantic == SdpGroupSemantic.AlternativeNetworkAddressTypes)
        {
            expected = SdpGroupSemantics.AlternativeNetworkAddressTypes;
        }
        else if (projection.Semantic == SdpGroupSemantic.ForwardErrorCorrection)
        {
            expected = SdpGroupSemantics.ForwardErrorCorrection;
        }
        else if (projection.Semantic == SdpGroupSemantic.ForwardErrorCorrectionFramework)
        {
            expected = SdpGroupSemantics.ForwardErrorCorrectionFramework;
        }
        else if (projection.Semantic == SdpGroupSemantic.CompositeSession)
        {
            expected = SdpGroupSemantics.CompositeSession;
        }
        else if (projection.Semantic == SdpGroupSemantic.DecodingDependency)
        {
            expected = SdpGroupSemantics.DecodingDependency;
        }
        else if (projection.Semantic == SdpGroupSemantic.Duplication)
        {
            expected = SdpGroupSemantics.Duplication;
        }
        else if (projection.Semantic == SdpGroupSemantic.Splice)
        {
            expected = SdpGroupSemantics.Splice;
        }
        else if (projection.Semantic == SdpGroupSemantic.Bundle)
        {
            expected = SdpGroupSemantics.Bundle;
        }
        else if (projection.Semantic == SdpGroupSemantic.Clue)
        {
            expected = SdpGroupSemantics.Clue;
        }
        else if (projection.Semantic == SdpGroupSemantic.VolumetricVideo)
        {
            expected = SdpGroupSemantics.V3c;
        }
        if (expected.Length > 0)
        {
            if (projection.SemanticToken.Length == 0 || projection.SemanticToken == expected)
            {
                return expected;
            }
            return string.Empty;
        }
        return SdpCommonAttributeCodecCore.IsTokenValue(projection.SemanticToken) ? projection.SemanticToken : string.Empty;
    }

    internal static string[] ReadGroupTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static string TrimGroupWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t'))
        {
            start = start + 1;
        }
        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t'))
        {
            end = end - 1;
        }
        return value.Substring(start, end - start);
    }

    internal static SdpAttribute CreateGroupValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailGroupDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateGroupEncodeDiagnostic(string attributeName, string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail);
    }
}
