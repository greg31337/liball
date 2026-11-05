using System;
using System.Globalization;

namespace sdp;

internal static class SdpRemoteCandidatesCodecCore
{
    internal static SdpAttributeDecodeResult DecodeRemoteCandidates(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates value must contain at least one component, address, and port tuple.");
        }

        string[] tokens = ReadRemoteCandidateTokens(attribute.Value);
        if (tokens.Length == 0 || tokens.Length % 3 != 0)
        {
            return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates value must contain complete component, address, and port tuples.");
        }

        SdpRemoteCandidatesAttribute projection = new SdpRemoteCandidatesAttribute();
        projection.SourceAttribute = attribute;
        int index = 0;
        while (index < tokens.Length)
        {
            SdpNumberParseResult component = SdpNumberParser.ParseCharacterUnsignedDecimal(tokens[index].AsSpan());
            if (!component.Success || tokens[index].Length > 3 || component.UnsignedValue > 999)
            {
                string code = component.Success ? SdpDiagnosticCodes.ValidationAttribute : component.DiagnosticCode;
                return FailRemoteCandidatesDecode(attribute, result, code, "Every SDP remote-candidates component identifier must contain one through three decimal digits.");
            }
            int componentValue = (int)component.UnsignedValue;
            if (ContainsRemoteComponent(projection, componentValue))
            {
                return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates value must not repeat a component identifier.");
            }

            string address = tokens[index + 1];
            if (!SdpIceCandidateCodecCore.IsCandidateAddressValue(address))
            {
                return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationConnection, "Every SDP remote-candidates address must be a non-whitespace value.");
            }

            string portText = tokens[index + 2];
            SdpNumberParseResult port = SdpNumberParser.ParseCharacterPort(portText.AsSpan());
            if (!port.Success || portText.Length > 5)
            {
                return FailRemoteCandidatesDecode(attribute, result, SdpDiagnosticCodes.ValidationPort, "Every SDP remote-candidates port must contain one through five decimal digits from zero through 65535.");
            }

            SdpRemoteCandidate candidate = new SdpRemoteCandidate();
            candidate.Component = componentValue;
            candidate.Address = address;
            candidate.AddressKind = SdpIceCandidateCodecCore.ReadCandidateAddressKind(address);
            candidate.Port = port.Int32Value;
            projection.Candidates.Add(candidate);
            index = index + 3;
        }

        result.RemoteCandidatesAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeRemoteCandidates(SdpRemoteCandidatesAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Candidates.Count == 0)
        {
            result.Diagnostics.Add(CreateRemoteCandidatesEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates value must contain at least one component, address, and port tuple."));
            return result;
        }

        string value = string.Empty;
        int index = 0;
        while (index < projection.Candidates.Count)
        {
            SdpRemoteCandidate candidate = projection.Candidates[index];
            if (candidate.Component < 0 || candidate.Component > 999)
            {
                result.Diagnostics.Add(CreateRemoteCandidatesEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "Every SDP remote-candidates component identifier must contain one through three decimal digits."));
                return result;
            }
            if (ContainsEarlierRemoteComponent(projection, index, candidate.Component))
            {
                result.Diagnostics.Add(CreateRemoteCandidatesEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP remote-candidates value must not repeat a component identifier."));
                return result;
            }
            if (!SdpIceCandidateCodecCore.IsCandidateAddressValue(candidate.Address))
            {
                result.Diagnostics.Add(CreateRemoteCandidatesEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "Every SDP remote-candidates address must be a non-whitespace value."));
                return result;
            }
            if (candidate.Port < 0 || candidate.Port > 65535)
            {
                result.Diagnostics.Add(CreateRemoteCandidatesEncodeDiagnostic(SdpDiagnosticCodes.ValidationPort, "Every SDP remote-candidates port must be from zero through 65535."));
                return result;
            }
            if (index > 0)
            {
                value = value + " ";
            }
            value = value + candidate.Component.ToString(CultureInfo.InvariantCulture) + " " + candidate.Address + " " + candidate.Port.ToString(CultureInfo.InvariantCulture);
            index = index + 1;
        }

        result.Attribute = CreateRemoteCandidatesValueAttribute(value);
        result.Success = true;
        return result;
    }

    internal static string[] ReadRemoteCandidateTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static bool ContainsRemoteComponent(SdpRemoteCandidatesAttribute projection, int component)
    {
        int index = 0;
        while (index < projection.Candidates.Count)
        {
            if (projection.Candidates[index].Component == component)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsEarlierRemoteComponent(SdpRemoteCandidatesAttribute projection, int candidateIndex, int component)
    {
        int index = 0;
        while (index < candidateIndex)
        {
            if (projection.Candidates[index].Component == component)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpAttribute CreateRemoteCandidatesValueAttribute(string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.RemoteCandidates;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.RemoteCandidates + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailRemoteCandidatesDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateRemoteCandidatesEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.RemoteCandidates, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
