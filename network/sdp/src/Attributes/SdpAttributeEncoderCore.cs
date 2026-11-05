namespace sdp;

internal static class SdpAttributeEncoderCore
{
    internal static SdpAttributeEncodeResult EncodeAttribute(object typedAttribute)
    {
        string? typeName = typedAttribute.GetType().FullName;
        if (typeName == "sdp.SdpCommonAttribute")
        {
            return EncodeCommonAttribute((SdpCommonAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpRtpMapAttribute")
        {
            return SdpRtpMapCodecCore.EncodeRtpMap((SdpRtpMapAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpFormatParametersAttribute")
        {
            return SdpFormatParametersCodecCore.EncodeFormatParameters((SdpFormatParametersAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpRtcpAttribute")
        {
            return SdpRtcpAttributeCodecCore.EncodeRtcp((SdpRtcpAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpRtcpFeedbackAttribute")
        {
            return SdpRtcpAttributeCodecCore.EncodeRtcpFeedback((SdpRtcpFeedbackAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpRtcpExtendedReportAttribute")
        {
            return SdpRtcpAttributeCodecCore.EncodeRtcpExtendedReport((SdpRtcpExtendedReportAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSourceAttribute")
        {
            return SdpSourceAttributeCodecCore.EncodeSourceAttribute((SdpSourceAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSourceGroupAttribute")
        {
            return SdpSourceAttributeCodecCore.EncodeSourceGroup((SdpSourceGroupAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpMediaStreamId")
        {
            return SdpSourceAttributeCodecCore.EncodeMediaStreamId((SdpMediaStreamId)typedAttribute);
        }
        if (typeName == "sdp.SdpMediaStreamSemanticAttribute")
        {
            return SdpSourceAttributeCodecCore.EncodeMediaStreamSemantic((SdpMediaStreamSemanticAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSourceFilterAttribute")
        {
            return SdpSourceFilterCodecCore.EncodeSourceFilter((SdpSourceFilterAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpClockReferenceAttribute")
        {
            return SdpClockAttributeCodecCore.EncodeClockReference((SdpClockReferenceAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpMediaClockAttribute")
        {
            return SdpClockAttributeCodecCore.EncodeMediaClock((SdpMediaClockAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpImageAttribute")
        {
            return SdpImageAttributeCodecCore.EncodeImageAttribute((SdpImageAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpGroupAttribute")
        {
            return SdpGroupAttributeCodecCore.EncodeGroup((SdpGroupAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpMediaIdAttribute")
        {
            return SdpGroupAttributeCodecCore.EncodeMediaId((SdpMediaIdAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpIceUsernameFragmentAttribute")
        {
            return SdpIceAttributeCodecCore.EncodeUsernameFragment((SdpIceUsernameFragmentAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpIcePasswordAttribute")
        {
            return SdpIceAttributeCodecCore.EncodePassword((SdpIcePasswordAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpIceOptionsAttribute")
        {
            return SdpIceAttributeCodecCore.EncodeOptions((SdpIceOptionsAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpIceCandidate")
        {
            return SdpIceCandidateCodecCore.EncodeCandidate((SdpIceCandidate)typedAttribute);
        }
        if (typeName == "sdp.SdpRemoteCandidatesAttribute")
        {
            return SdpRemoteCandidatesCodecCore.EncodeRemoteCandidates((SdpRemoteCandidatesAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpFingerprintAttribute")
        {
            return SdpFingerprintAttributeCodecCore.EncodeFingerprint((SdpFingerprintAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSetupAttribute")
        {
            return SdpDtlsAttributeCodecCore.EncodeSetup((SdpSetupAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpConnectionAttribute")
        {
            return SdpDtlsAttributeCodecCore.EncodeConnectionRole((SdpConnectionAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpTlsIdAttribute")
        {
            return SdpDtlsAttributeCodecCore.EncodeTlsId((SdpTlsIdAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpCryptoAttribute")
        {
            return SdpCryptoAttributeCodecCore.EncodeCrypto((SdpCryptoAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpExtMapAttribute")
        {
            return SdpExtMapAttributeCodecCore.EncodeExtMap((SdpExtMapAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpRidAttribute")
        {
            return SdpRidAttributeCodecCore.EncodeRid((SdpRidAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSimulcastAttribute")
        {
            return SdpSimulcastAttributeCodecCore.EncodeSimulcast((SdpSimulcastAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSctpPortAttribute")
        {
            return SdpSctpAttributeCodecCore.EncodeSctpPort((SdpSctpPortAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpMaximumMessageSizeAttribute")
        {
            return SdpSctpAttributeCodecCore.EncodeMaximumMessageSize((SdpMaximumMessageSizeAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpSctpMapAttribute")
        {
            return SdpSctpAttributeCodecCore.EncodeSctpMap((SdpSctpMapAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpDataChannelAttribute")
        {
            return SdpDataChannelAttributeCodecCore.EncodeMap((SdpDataChannelAttribute)typedAttribute);
        }
        if (typeName == "sdp.SdpDataChannelSubprotocolAttribute")
        {
            return SdpDataChannelAttributeCodecCore.EncodeSubprotocolAttribute((SdpDataChannelSubprotocolAttribute)typedAttribute);
        }
        return CreateUnsupportedResult(typedAttribute);
    }

    internal static SdpAttributeEncodeResult EncodeCommonAttribute(SdpCommonAttribute common)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = common;
        string canonicalName = SdpCommonAttributeCodecCore.ReadEncodingName(common);
        if (canonicalName.Length == 0)
        {
            result.Diagnostics.Add(CreateEncodeDiagnostic(string.Empty, "The common attribute does not identify a supported name or direction."));
            return result;
        }

        string value = SdpCommonAttributeCodecCore.ReadEncodingValue(common, canonicalName);
        SdpAttributeKind kind = SdpAttributeKind.Value;
        if (SdpCommonAttributeCodecCore.IsPropertyName(canonicalName))
        {
            kind = SdpAttributeKind.Property;
        }
        SdpCommonAttribute validation = new SdpCommonAttribute();
        validation.Name = canonicalName;
        validation.Value = value;
        validation.Direction = SdpCommonAttributeCodecCore.ReadDirection(canonicalName);
        string validationError = SdpCommonAttributeCodecCore.ReadValidationError(validation, kind);
        if (validationError.Length > 0)
        {
            result.Diagnostics.Add(CreateEncodeDiagnostic(canonicalName, validationError));
            return result;
        }

        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = kind;
        attribute.Name = canonicalName;
        attribute.Value = value;
        attribute.RawText = canonicalName;
        if (kind == SdpAttributeKind.Value)
        {
            attribute.RawText = canonicalName + ":" + value;
        }
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static SdpAttributeEncodeResult CreateUnsupportedResult(object typedAttribute)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = typedAttribute;
        result.Diagnostics.Add(CreateEncodeDiagnostic(string.Empty, "The supplied typed attribute data does not have a supported encoder."));
        return result;
    }

    internal static SdpDiagnostic CreateEncodeDiagnostic(string attributeName, string detail)
    {
        SdpDiagnostic diagnostic = new SdpDiagnostic();
        diagnostic.Code = SdpDiagnosticCodes.ValidationAttribute;
        diagnostic.Severity = SdpValidationSeverity.Error;
        diagnostic.FieldName = 'a';
        diagnostic.AttributeName = attributeName;
        diagnostic.Detail = detail;
        diagnostic.Path.FieldName = 'a';
        diagnostic.Path.AttributeName = attributeName;
        return diagnostic;
    }
}
