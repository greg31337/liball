using System;

namespace sdp;

internal static class SdpAttributeDecoderCore
{
    internal static SdpAttributeDecodeResult DecodeAttribute(SdpAttribute attribute)
    {
        SdpAttributeDecodeResult result = new SdpAttributeDecodeResult();
        result.Success = true;
        result.SourceAttribute = attribute;
        string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name);
        if (canonicalName.Length == 0)
        {
            if (string.Equals(attribute.Name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRtpMapCodecCore.DecodeRtpMap(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.FormatParameters, StringComparison.OrdinalIgnoreCase))
            {
                return SdpFormatParametersCodecCore.DecodeFormatParameters(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Rtcp, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRtcpAttributeCodecCore.DecodeRtcp(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.RtcpFeedback, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRtcpAttributeCodecCore.DecodeRtcpFeedback(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.RtcpExtendedReports, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRtcpAttributeCodecCore.DecodeRtcpExtendedReport(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.SynchronizationSource, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSourceAttributeCodecCore.DecodeSourceAttribute(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.SynchronizationSourceGroup, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSourceAttributeCodecCore.DecodeSourceGroup(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaStreamId, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSourceAttributeCodecCore.DecodeMediaStreamId(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaStreamIdSemantic, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSourceAttributeCodecCore.DecodeMediaStreamSemantic(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.SourceFilter, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSourceFilterCodecCore.DecodeSourceFilter(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.TimestampReferenceClock, StringComparison.OrdinalIgnoreCase))
            {
                return SdpClockAttributeCodecCore.DecodeClockReference(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaClock, StringComparison.OrdinalIgnoreCase))
            {
                return SdpClockAttributeCodecCore.DecodeMediaClock(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.ImageAttribute, StringComparison.OrdinalIgnoreCase))
            {
                return SdpImageAttributeCodecCore.DecodeImageAttribute(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase))
            {
                return SdpGroupAttributeCodecCore.DecodeGroup(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaId, StringComparison.OrdinalIgnoreCase))
            {
                return SdpGroupAttributeCodecCore.DecodeMediaId(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.IceUsernameFragment, StringComparison.OrdinalIgnoreCase))
            {
                return SdpIceAttributeCodecCore.DecodeUsernameFragment(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.IcePassword, StringComparison.OrdinalIgnoreCase))
            {
                return SdpIceAttributeCodecCore.DecodePassword(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.IceOptions, StringComparison.OrdinalIgnoreCase))
            {
                return SdpIceAttributeCodecCore.DecodeOptions(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase))
            {
                return SdpIceCandidateCodecCore.DecodeCandidate(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRemoteCandidatesCodecCore.DecodeRemoteCandidates(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                return SdpFingerprintAttributeCodecCore.DecodeFingerprint(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Setup, StringComparison.OrdinalIgnoreCase))
            {
                return SdpDtlsAttributeCodecCore.DecodeSetup(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Connection, StringComparison.OrdinalIgnoreCase))
            {
                return SdpDtlsAttributeCodecCore.DecodeConnectionRole(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.TlsId, StringComparison.OrdinalIgnoreCase))
            {
                return SdpDtlsAttributeCodecCore.DecodeTlsId(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Crypto, StringComparison.OrdinalIgnoreCase))
            {
                return SdpCryptoAttributeCodecCore.DecodeCrypto(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.ExtMap, StringComparison.OrdinalIgnoreCase))
            {
                return SdpExtMapAttributeCodecCore.DecodeExtMap(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Rid, StringComparison.OrdinalIgnoreCase))
            {
                return SdpRidAttributeCodecCore.DecodeRid(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.Simulcast, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSimulcastAttributeCodecCore.DecodeSimulcast(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.SctpPort, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSctpAttributeCodecCore.DecodeSctpPort(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.MaximumMessageSize, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSctpAttributeCodecCore.DecodeMaximumMessageSize(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.SctpMap, StringComparison.OrdinalIgnoreCase))
            {
                return SdpSctpAttributeCodecCore.DecodeSctpMap(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.DataChannelMap, StringComparison.OrdinalIgnoreCase))
            {
                return SdpDataChannelAttributeCodecCore.DecodeMap(attribute, result);
            }
            if (string.Equals(attribute.Name, SdpAttributeNames.DataChannelSubprotocol, StringComparison.OrdinalIgnoreCase))
            {
                return SdpDataChannelAttributeCodecCore.DecodeSubprotocolAttribute(attribute, result);
            }
            return result;
        }

        result.Recognized = true;
        SdpCommonAttribute common = new SdpCommonAttribute();
        common.SourceAttribute = attribute;
        common.Name = canonicalName;
        common.Value = attribute.Value;
        common.Direction = SdpCommonAttributeCodecCore.ReadDirection(canonicalName);
        common.Obsolete = canonicalName == SdpAttributeNames.Category || canonicalName == SdpAttributeNames.Keywords;
        string validationError = SdpCommonAttributeCodecCore.ReadValidationError(common, attribute.Kind);
        if (validationError.Length > 0)
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, validationError);
        }

        SdpCommonAttributeCodecCore.PopulateNumericProjection(common);
        result.CommonAttribute = common;
        result.TypedAttribute = common;
        return result;
    }

    internal static SdpDiagnostic CreateDecodeDiagnostic(SdpAttribute attribute, string detail)
    {
        SdpDiagnostic diagnostic = new SdpDiagnostic();
        diagnostic.Code = SdpDiagnosticCodes.ValidationAttribute;
        diagnostic.Severity = SdpValidationSeverity.Error;
        diagnostic.FieldName = 'a';
        diagnostic.AttributeName = attribute.Name;
        diagnostic.Detail = detail;
        diagnostic.Path.FieldName = 'a';
        diagnostic.Path.AttributeName = attribute.Name;
        diagnostic.Location = SdpDiagnosticBuilder.CopySourceLocation(attribute.Location);
        return diagnostic;
    }

    internal static SdpAttributeDecodeResult FailDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        result.Success = false;
        result.SourceAttribute = attribute;
        result.TypedAttribute = null;
        result.CommonAttribute = null;
        result.RtpMapAttribute = null;
        result.FormatParametersAttribute = null;
        result.RtcpAttribute = null;
        result.RtcpFeedbackAttribute = null;
        result.RtcpExtendedReportAttribute = null;
        result.SynchronizationSourceAttribute = null;
        result.SourceGroupAttribute = null;
        result.MediaStreamId = null;
        result.MediaStreamSemanticAttribute = null;
        result.SourceFilterAttribute = null;
        result.ClockReferenceAttribute = null;
        result.MediaClockAttribute = null;
        result.ImageAttribute = null;
        result.GroupAttribute = null;
        result.MediaIdAttribute = null;
        result.IceUsernameFragmentAttribute = null;
        result.IcePasswordAttribute = null;
        result.IceOptionsAttribute = null;
        result.IceCandidate = null;
        result.RemoteCandidatesAttribute = null;
        result.FingerprintAttribute = null;
        result.SetupAttribute = null;
        result.ConnectionAttribute = null;
        result.TlsIdAttribute = null;
        result.CryptoAttribute = null;
        result.ExtMapAttribute = null;
        result.RidAttribute = null;
        result.SimulcastAttribute = null;
        result.SctpPortAttribute = null;
        result.MaximumMessageSizeAttribute = null;
        result.SctpMapAttribute = null;
        result.DataChannelAttribute = null;
        result.DataChannelSubprotocolAttribute = null;
        SdpDiagnostic diagnostic = CreateDecodeDiagnostic(attribute, detail);
        diagnostic.Code = code;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
