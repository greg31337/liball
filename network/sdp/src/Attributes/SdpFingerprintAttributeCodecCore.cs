using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpFingerprintAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeFingerprint(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailFingerprintDecode(attribute, result, "The SDP fingerprint attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailFingerprintDecode(attribute, result, "The SDP fingerprint attribute has invalid field syntax.");
        }

        string[] tokens = attribute.Value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
        {
            return FailFingerprintDecode(attribute, result, "The SDP fingerprint attribute requires one hash-function token and one digest.");
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[0]))
        {
            return FailFingerprintDecode(attribute, result, "The SDP fingerprint hash function must use non-empty token syntax.");
        }

        List<byte> digest = new List<byte>();
        string error = ReadFingerprintError(tokens[1], digest);
        if (error.Length > 0)
        {
            return FailFingerprintDecode(attribute, result, error);
        }

        SdpFingerprintAttribute projection = new SdpFingerprintAttribute();
        projection.SourceAttribute = attribute;
        projection.Algorithm = tokens[0];
        projection.Fingerprint = tokens[1];
        int index = 0;
        while (index < digest.Count)
        {
            projection.Digest.Add(digest[index]);
            index = index + 1;
        }
        result.FingerprintAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeFingerprint(SdpFingerprintAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.Algorithm))
        {
            result.Diagnostics.Add(CreateFingerprintEncodeDiagnostic("The SDP fingerprint hash function must use non-empty token syntax."));
            return result;
        }
        if (projection.Digest == null || projection.Digest.Count == 0)
        {
            result.Diagnostics.Add(CreateFingerprintEncodeDiagnostic("The SDP fingerprint digest must contain at least one octet."));
            return result;
        }

        string fingerprint = ReadFingerprintText(projection.Digest);
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.Fingerprint;
        attribute.Value = projection.Algorithm + " " + fingerprint;
        attribute.RawText = SdpAttributeNames.Fingerprint + ":" + attribute.Value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static string ReadFingerprintError(string fingerprint, List<byte> digest)
    {
        if (fingerprint.Length < 2 || (fingerprint.Length + 1) % 3 != 0)
        {
            return "The SDP fingerprint digest must contain colon-separated uppercase hexadecimal octets.";
        }
        int index = 0;
        while (index < fingerprint.Length)
        {
            if (!IsUpperHexCharacter(fingerprint[index]) || !IsUpperHexCharacter(fingerprint[index + 1]))
            {
                return "The SDP fingerprint digest must contain colon-separated uppercase hexadecimal octets.";
            }
            int high = ReadHexValue(fingerprint[index]);
            int low = ReadHexValue(fingerprint[index + 1]);
            digest.Add((byte)(high * 16 + low));
            index = index + 2;
            if (index < fingerprint.Length)
            {
                if (fingerprint[index] != ':')
                {
                    return "The SDP fingerprint digest must contain colon-separated uppercase hexadecimal octets.";
                }
                index = index + 1;
            }
        }
        return string.Empty;
    }

    internal static bool IsUpperHexCharacter(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F';
    }

    internal static int ReadHexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        return value - 'A' + 10;
    }

    internal static string ReadFingerprintText(List<byte> digest)
    {
        string result = string.Empty;
        int index = 0;
        while (index < digest.Count)
        {
            if (index > 0)
            {
                result = result + ":";
            }
            result = result + digest[index].ToString("X2", CultureInfo.InvariantCulture);
            index = index + 1;
        }
        return result;
    }

    internal static SdpAttributeDecodeResult FailFingerprintDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateFingerprintEncodeDiagnostic(string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.Fingerprint, detail);
    }
}
