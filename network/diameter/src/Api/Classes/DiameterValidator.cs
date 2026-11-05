using System;
using System.Text;

namespace diameter;

/// <summary>
/// Validates Diameter messages and AVPs and constructs validation error answers.
/// </summary>
public class DiameterValidator : IDiameterValidator
{
    /// <summary>Initializes a Diameter validator.</summary>
    public DiameterValidator()
    {
    }

    /// <summary>Classifies a Diameter Result-Code by its thousands digit.</summary>
    /// <param name="resultCode">The Result-Code to classify.</param>
    /// <returns>The RFC 6733 result class, or permanent failure for an unknown class.</returns>
    public DiameterResultClass ClassifyResultCode(uint resultCode)
    {
        uint classValue = resultCode / 1000U;
        if (classValue == 1U)
        {
            return DiameterResultClass.Informational;
        }

        if (classValue == 2U)
        {
            return DiameterResultClass.Success;
        }

        if (classValue == 3U)
        {
            return DiameterResultClass.ProtocolError;
        }

        if (classValue == 4U)
        {
            return DiameterResultClass.TransientFailure;
        }

        return DiameterResultClass.PermanentFailure;
    }

    /// <summary>Validates a message against a dictionary snapshot and limits.</summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="snapshot">The dictionary snapshot.</param>
    /// <param name="limits">The validation limits.</param>
    /// <returns>The structured validation outcome.</returns>
    public DiameterValidationResult ValidateMessage(
        DiameterMessage? message,
        DiameterDictionarySnapshot? snapshot,
        DiameterLimitConfiguration? limits)
    {
        CommandValidationResult command =
            new CommandValidator(snapshot).ValidateCommand(message);
        AvpValidationResult? avps = null;
        if (message != null)
        {
            avps = new AvpValidator(snapshot, limits).ValidateMessageAvps(message);
        }

        return new ValidationResultBuilder(snapshot).BuildValidationResult(
            message,
            command,
            avps);
    }

    /// <summary>Validates one AVP, including its Grouped children when applicable.</summary>
    /// <param name="avp">The AVP to validate.</param>
    /// <param name="snapshot">The dictionary snapshot.</param>
    /// <param name="limits">The validation limits.</param>
    /// <returns>The structured validation outcome.</returns>
    public DiameterValidationResult ValidateGroupedAvp(
        DiameterAvp? avp,
        DiameterDictionarySnapshot? snapshot,
        DiameterLimitConfiguration? limits)
    {
        AvpValidationResult avpResult =
            new AvpValidator(snapshot, limits).ValidateSingleAvp(avp);
        return new ValidationResultBuilder(snapshot).BuildValidationResult(
            null,
            null,
            avpResult);
    }

    /// <summary>Builds a Diameter answer for a structured validation failure.</summary>
    /// <param name="request">The validation error-answer input.</param>
    /// <param name="originHost">The answering node identity.</param>
    /// <param name="originRealm">The answering node realm.</param>
    /// <returns>The protocol error answer.</returns>
    /// <exception cref="DiameterValidationException">Required input is absent or invalid.</exception>
    public DiameterMessage BuildValidationErrorAnswer(
        DiameterErrorAnswerRequest? request,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm)
    {
        return BuildErrorAnswerCore(request, originHost, originRealm);
    }

    /// <summary>Builds a Diameter answer carrying a protocol, transient, or permanent failure.</summary>
    /// <param name="request">The failed request and error values.</param>
    /// <param name="originHost">The answering node identity.</param>
    /// <param name="originRealm">The answering node realm.</param>
    /// <returns>The error answer.</returns>
    /// <exception cref="DiameterValidationException">Required input is absent or invalid.</exception>
    public DiameterMessage BuildResultErrorAnswer(
        DiameterErrorAnswerRequest? request,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm)
    {
        return BuildErrorAnswerCore(request, originHost, originRealm);
    }

    internal DiameterMessage BuildErrorAnswerCore(
        DiameterErrorAnswerRequest? request,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm)
    {
        DiameterErrorAnswerRequest input = ValidateErrorAnswerInput(request);
        DiameterValueResult normalizedHost = ValueCodecs.NormalizeDiameterIdentity(originHost);
        if (normalizedHost.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "A valid Origin-Host identity is required for an error answer.");
        }

        DiameterValueResult normalizedRealm = ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedRealm.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterValidationException(
                "A valid Origin-Realm is required for an error answer.");
        }

        DiameterMessage? sourceValue = input.Request;
        if (sourceValue == null)
        {
            throw new DiameterValidationException(
                "A failed Diameter request is required to build an error answer.");
        }

        DiameterMessage source = sourceValue;
        uint effectiveResultCode = ReadEffectiveResultCode(input);
        DiameterMessage answer = new DiameterMessage();
        answer.Version = source.Version;
        answer.Flags = source.Flags & DiameterMessageFlags.Proxyable;
        if (ClassifyResultCode(effectiveResultCode) == DiameterResultClass.ProtocolError)
        {
            answer.Flags = answer.Flags | DiameterMessageFlags.Error;
        }

        answer.CommandCode = source.CommandCode;
        answer.ApplicationId = source.ApplicationId;
        answer.HopByHopIdentifier = source.HopByHopIdentifier;
        answer.EndToEndIdentifier = source.EndToEndIdentifier;
        CopyFirstAvpByCode(source, answer, DiameterAvpCodes.SessionId);
        answer.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginHost,
            DiameterAvpFlags.Mandatory,
            normalizedHost.Text));
        answer.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginRealm,
            DiameterAvpFlags.Mandatory,
            normalizedRealm.Text));
        if (input.ExperimentalResult == null)
        {
            answer.Avps.Add(CreateUnsigned32Avp(
                DiameterAvpCodes.ResultCode,
                input.ResultCode));
        }
        else
        {
            answer.Avps.Add(new DiameterBaseAvpFactory().CreateExperimentalResultAvp(
                input.ExperimentalResult));
        }

        if (!string.IsNullOrEmpty(input.ErrorMessage))
        {
            if (ValueCodecs.FindInvalidUtf16Index(input.ErrorMessage) >= 0)
            {
                throw new DiameterValidationException(
                    "The Error-Message contains invalid UTF-8 text.");
            }

            answer.Avps.Add(CreateTextAvp(
                DiameterAvpCodes.ErrorMessage,
                DiameterAvpFlags.None,
                input.ErrorMessage));
        }

        if (input.ErrorReportingHost != null)
        {
            DiameterValueResult reportingHost = ValueCodecs.NormalizeDiameterIdentity(
                input.ErrorReportingHost);
            if (reportingHost.Status != DiameterCodecStatus.Success)
            {
                throw new DiameterValidationException(
                    "A valid Error-Reporting-Host identity is required.");
            }

            if (reportingHost.Text != normalizedHost.Text)
            {
                answer.Avps.Add(CreateTextAvp(
                    DiameterAvpCodes.ErrorReportingHost,
                    DiameterAvpFlags.None,
                    reportingHost.Text));
            }
        }

        if (input.FailedAvp != null)
        {
            answer.Avps.Add(CloneProtocolAvp(input.FailedAvp));
        }

        CopyAllAvpsByCode(source, answer, DiameterAvpCodes.ProxyInfo);
        return answer;
    }

    internal DiameterErrorAnswerRequest ValidateErrorAnswerInput(
        DiameterErrorAnswerRequest? request)
    {
        if (request == null || request.Request == null)
        {
            throw new DiameterValidationException(
                "A failed Diameter request is required to build an error answer.");
        }

        if ((request.Request.Flags & DiameterMessageFlags.Request) ==
            DiameterMessageFlags.None)
        {
            throw new DiameterValidationException(
                "A Diameter error answer can only be built for a request.");
        }

        bool hasResultCode = request.ResultCode != 0U;
        bool hasExperimentalResult = request.ExperimentalResult != null;
        if (hasResultCode == hasExperimentalResult)
        {
            throw new DiameterValidationException(
                "An error answer requires either Result-Code or Experimental-Result.");
        }

        uint effectiveResultCode = ReadEffectiveResultCode(request);
        if (effectiveResultCode < 3000U || effectiveResultCode >= 6000U)
        {
            throw new DiameterValidationException(
                "A failure Result-Code is required to build an error answer.");
        }

        return request;
    }

    internal uint ReadEffectiveResultCode(DiameterErrorAnswerRequest request)
    {
        if (request.ExperimentalResult == null)
        {
            return request.ResultCode;
        }

        if (request.ExperimentalResult.VendorId == 0U)
        {
            throw new DiameterValidationException(
                "An Experimental-Result requires a nonzero Vendor-Id.");
        }

        return request.ExperimentalResult.ResultCode;
    }

    internal DiameterAvp CreateUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        return avp;
    }

    internal DiameterAvp CreateTextAvp(
        uint code,
        DiameterAvpFlags flags,
        string value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = flags;
        avp.Data = Encoding.UTF8.GetBytes(value);
        return avp;
    }

    internal void CopyFirstAvpByCode(
        DiameterMessage source,
        DiameterMessage destination,
        uint code)
    {
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp avp = source.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                destination.Avps.Add(CloneProtocolAvp(avp));
                return;
            }

            index = index + 1;
        }
    }

    internal void CopyAllAvpsByCode(
        DiameterMessage source,
        DiameterMessage destination,
        uint code)
    {
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp avp = source.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                destination.Avps.Add(CloneProtocolAvp(avp));
            }

            index = index + 1;
        }
    }

    internal DiameterAvp CloneProtocolAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        if (source.Data == null)
        {
            throw new DiameterValidationException(
                "An error-answer AVP requires owned payload data.");
        }

        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }
}
