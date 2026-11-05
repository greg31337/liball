using System;
using System.Linq;

namespace sdp.Tests.Api;

public class SdpLimitAndDiagnosticCodeTests
{
    [Test]
    public void DefaultLimits_ArePositiveAndBoundNestedAllocations()
    {
        int[] limits =
        {
            SdpLimits.DefaultMaximumInputCharacters,
            SdpLimits.DefaultMaximumInputBytes,
            SdpLimits.DefaultMaximumOutputCharacters,
            SdpLimits.DefaultMaximumOutputBytes,
            SdpLimits.DefaultMaximumLineLength,
            SdpLimits.DefaultMaximumLineCount,
            SdpLimits.DefaultMaximumMediaCount,
            SdpLimits.DefaultMaximumAttributesPerScope,
            SdpLimits.DefaultMaximumTokenLength,
            SdpLimits.DefaultMaximumCandidateCount,
            SdpLimits.DefaultMaximumDiagnosticCount
        };

        Assert.Multiple(() =>
        {
            Assert.That(limits, Has.All.Positive);
            Assert.That(SdpLimits.DefaultMaximumInputBytes, Is.GreaterThanOrEqualTo(SdpLimits.DefaultMaximumInputCharacters));
            Assert.That(SdpLimits.DefaultMaximumOutputCharacters, Is.GreaterThanOrEqualTo(SdpLimits.DefaultMaximumInputCharacters));
            Assert.That(SdpLimits.DefaultMaximumOutputBytes, Is.GreaterThanOrEqualTo(SdpLimits.DefaultMaximumOutputCharacters));
            Assert.That(SdpLimits.DefaultMaximumTokenLength, Is.LessThanOrEqualTo(SdpLimits.DefaultMaximumLineLength));
            Assert.That(SdpLimits.DefaultMaximumMediaCount, Is.LessThan(SdpLimits.DefaultMaximumLineCount));
            Assert.That(SdpLimits.DefaultMaximumCandidateCount, Is.LessThanOrEqualTo(SdpLimits.DefaultMaximumAttributesPerScope));
            Assert.That(SdpLimits.DefaultMaximumDiagnosticCount, Is.LessThan(SdpLimits.DefaultMaximumLineCount));
        });
    }

    [Test]
    public void DefaultLimits_HaveStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpLimits.DefaultMaximumInputCharacters, Is.EqualTo(1048576));
            Assert.That(SdpLimits.DefaultMaximumInputBytes, Is.EqualTo(4194304));
            Assert.That(SdpLimits.DefaultMaximumOutputCharacters, Is.EqualTo(4194304));
            Assert.That(SdpLimits.DefaultMaximumOutputBytes, Is.EqualTo(16777216));
            Assert.That(SdpLimits.DefaultMaximumLineLength, Is.EqualTo(16384));
            Assert.That(SdpLimits.DefaultMaximumLineCount, Is.EqualTo(16384));
            Assert.That(SdpLimits.DefaultMaximumMediaCount, Is.EqualTo(256));
            Assert.That(SdpLimits.DefaultMaximumAttributesPerScope, Is.EqualTo(2048));
            Assert.That(SdpLimits.DefaultMaximumTokenLength, Is.EqualTo(4096));
            Assert.That(SdpLimits.DefaultMaximumCandidateCount, Is.EqualTo(1024));
            Assert.That(SdpLimits.DefaultMaximumDiagnosticCount, Is.EqualTo(256));
        });
    }

    [Test]
    public void DiagnosticCodes_AreNonemptyAndUnique()
    {
        string[] codes =
        {
            SdpDiagnosticCodes.DiagnosticLimitReached,
            SdpDiagnosticCodes.ParseInputCharacterLimitExceeded,
            SdpDiagnosticCodes.ParseInputByteLimitExceeded,
            SdpDiagnosticCodes.ParseLineLengthLimitExceeded,
            SdpDiagnosticCodes.ParseLineCountLimitExceeded,
            SdpDiagnosticCodes.ParseMediaCountLimitExceeded,
            SdpDiagnosticCodes.ParseAttributeCountLimitExceeded,
            SdpDiagnosticCodes.ParseTokenLengthLimitExceeded,
            SdpDiagnosticCodes.ParseCandidateCountLimitExceeded,
            SdpDiagnosticCodes.ParseInvalidUtf8,
            SdpDiagnosticCodes.ParseByteOrderMarkNotAllowed,
            SdpDiagnosticCodes.ParseNulCharacterNotAllowed,
            SdpDiagnosticCodes.ParseControlCharacterNotAllowed,
            SdpDiagnosticCodes.ParseInvalidLineEnding,
            SdpDiagnosticCodes.ParseEmptyLine,
            SdpDiagnosticCodes.ParseMalformedLine,
            SdpDiagnosticCodes.ParseUnknownField,
            SdpDiagnosticCodes.ParseFieldOutOfOrder,
            SdpDiagnosticCodes.ParseDuplicateField,
            SdpDiagnosticCodes.ParseMissingRequiredField,
            SdpDiagnosticCodes.ParseFieldInWrongScope,
            SdpDiagnosticCodes.ParseInvalidToken,
            SdpDiagnosticCodes.ParseInvalidNumber,
            SdpDiagnosticCodes.ParseNumberOverflow,
            SdpDiagnosticCodes.ParseMalformedAttribute,
            SdpDiagnosticCodes.ParseRecoveredMalformedLine,
            SdpDiagnosticCodes.ValidationProtocolVersion,
            SdpDiagnosticCodes.ValidationMissingConnection,
            SdpDiagnosticCodes.ValidationOrigin,
            SdpDiagnosticCodes.ValidationConnection,
            SdpDiagnosticCodes.ValidationBandwidth,
            SdpDiagnosticCodes.ValidationTiming,
            SdpDiagnosticCodes.ValidationMedia,
            SdpDiagnosticCodes.ValidationPort,
            SdpDiagnosticCodes.ValidationPayloadType,
            SdpDiagnosticCodes.ValidationAttribute,
            SdpDiagnosticCodes.ValidationAttributeScope,
            SdpDiagnosticCodes.ValidationAttributeCardinality,
            SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap,
            SdpDiagnosticCodes.ValidationDanglingPayloadReference,
            SdpDiagnosticCodes.ValidationIce,
            SdpDiagnosticCodes.ValidationDtls,
            SdpDiagnosticCodes.ValidationExtMap,
            SdpDiagnosticCodes.ValidationGroup,
            SdpDiagnosticCodes.ValidationBundle,
            SdpDiagnosticCodes.ValidationDataChannel,
            SdpDiagnosticCodes.ValidationBfcp,
            SdpDiagnosticCodes.ValidationObsoleteSyntax,
            SdpDiagnosticCodes.WriteInvalidDescription,
            SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded,
            SdpDiagnosticCodes.WriteOutputByteLimitExceeded,
            SdpDiagnosticCodes.WriteBufferTooSmall,
            SdpDiagnosticCodes.WriteLosslessDataUnavailable,
            SdpDiagnosticCodes.NegotiationInvalidOffer,
            SdpDiagnosticCodes.NegotiationInvalidAnswer,
            SdpDiagnosticCodes.NegotiationUnsupportedMedia,
            SdpDiagnosticCodes.NegotiationUnsupportedProtocol,
            SdpDiagnosticCodes.NegotiationNoCompatibleCodec,
            SdpDiagnosticCodes.NegotiationPayloadAllocationFailed,
            SdpDiagnosticCodes.NegotiationExtMapAllocationFailed,
            SdpDiagnosticCodes.NegotiationIceIncompatible,
            SdpDiagnosticCodes.NegotiationDtlsIncompatible,
            SdpDiagnosticCodes.NegotiationBundleIncompatible,
            SdpDiagnosticCodes.NegotiationDataChannelIncompatible
        };

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Not.Contain(string.Empty));
            Assert.That(codes.All(code => code.StartsWith("SDP_", StringComparison.Ordinal)), Is.True);
            Assert.That(codes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(codes.Length));
        });
    }

    [Test]
    public void DiagnosticCodes_KeepStableCategoryNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpDiagnosticCodes.DiagnosticLimitReached, Is.EqualTo("SDP_DIAGNOSTIC_LIMIT_REACHED"));
            Assert.That(SdpDiagnosticCodes.ParseInvalidUtf8, Is.EqualTo("SDP_PARSE_INVALID_UTF8"));
            Assert.That(SdpDiagnosticCodes.ParseNumberOverflow, Is.EqualTo("SDP_PARSE_NUMBER_OVERFLOW"));
            Assert.That(SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap, Is.EqualTo("SDP_VALIDATION_DYNAMIC_PAYLOAD_MISSING_RTPMAP"));
            Assert.That(SdpDiagnosticCodes.WriteBufferTooSmall, Is.EqualTo("SDP_WRITE_BUFFER_TOO_SMALL"));
            Assert.That(SdpDiagnosticCodes.NegotiationNoCompatibleCodec, Is.EqualTo("SDP_NEGOTIATION_NO_COMPATIBLE_CODEC"));
        });
    }
}
