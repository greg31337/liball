using System;
using System.Collections.Generic;

namespace diameter;

internal class TranslationRequestDispatchResult
{
    internal DiameterTranslationResult Translation =
        new DiameterTranslationResult();
    internal DiameterMessage? Message;
    internal bool Routed;
    internal bool Error;
    internal string Code = string.Empty;
    internal string MessageText = string.Empty;
}

internal class TranslationAgent
{
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal DiameterIdentity LocalRealm = new DiameterIdentity();
    internal List<DiameterTranslationHandlerConfiguration> Handlers =
        new List<DiameterTranslationHandlerConfiguration>();
    internal DiameterValidator Validator = new DiameterValidator();

    internal TranslationAgent(
        DiameterIdentity localHost,
        DiameterIdentity localRealm,
        List<DiameterTranslationHandlerConfiguration> handlers)
    {
        DiameterValueResult host =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        DiameterValueResult realm =
            ValueCodecs.NormalizeDiameterRealm(localRealm);
        if (host.Status != DiameterCodecStatus.Success ||
            realm.Status != DiameterCodecStatus.Success || handlers == null)
        {
            throw new ArgumentException(
                "A translation agent requires local identities and handlers.");
        }

        LocalHost.Value = host.Text;
        LocalRealm.Value = realm.Text;
        int index = 0;
        while (index < handlers.Count)
        {
            DiameterTranslationHandlerConfiguration source = handlers[index];
            source.ValidateTranslationHandlerConfiguration();
            DiameterTranslationHandlerConfiguration registration =
                new DiameterTranslationHandlerConfiguration();
            registration.SourceApplicationId = source.SourceApplicationId;
            registration.TargetApplicationId = source.TargetApplicationId;
            registration.Handler = source.Handler;
            registration.Enabled = source.Enabled;
            Handlers.Add(registration);
            index = index + 1;
        }
    }

    internal TranslationRequestDispatchResult DispatchTranslationRequest(
        DiameterRouteDecision? decision)
    {
        if (decision == null || decision.SelectedCandidate == null ||
            decision.Action != DiameterRouteAction.Translate ||
            decision.AgentRole != DiameterAgentRole.Translation ||
            decision.SelectedCandidate.Action != DiameterRouteAction.Translate ||
            decision.SelectedCandidate.AgentRole !=
                DiameterAgentRole.Translation)
        {
            return CreateTranslationDispatchFailure(
                null,
                0U,
                DiameterResultCodes.UnableToDeliver,
                "translation-route-required",
                "A selected translation route is required.");
        }

        DiameterRouteRequest request = decision.Request;
        uint targetApplicationId =
            decision.SelectedCandidate.Configuration.TargetApplicationId;
        DiameterTranslationHandlerConfiguration? registration =
            FindTranslationHandler(
                request.ApplicationId,
                targetApplicationId);
        if (registration == null || registration.Handler == null)
        {
            TranslationRequestDispatchResult unavailable =
                new TranslationRequestDispatchResult();
            unavailable.Translation = CreateUntranslatedTranslation(
                request.Message,
                request.ApplicationId,
                targetApplicationId,
                "translation-handler-unavailable",
                "No enabled handler is registered for the application pair.");
            unavailable.Code = unavailable.Translation.Code;
            unavailable.MessageText = unavailable.Translation.Message;
            return unavailable;
        }

        DiameterTranslationResult handlerResult =
            registration.Handler.TranslateDiameterRequest(
                RouteTableCloner.CloneRouteRequest(request),
                targetApplicationId);
        DiameterTranslationResult translation =
            NormalizeRequestTranslation(
                handlerResult,
                request.Message,
                request.ApplicationId,
                targetApplicationId);
        TranslationRequestDispatchResult result =
            new TranslationRequestDispatchResult();
        result.Translation = translation;
        result.Code = translation.Code;
        result.MessageText = translation.Message;
        if (translation.Disposition ==
            DiameterTranslationDisposition.Translated &&
            translation.TranslatedMessage != null)
        {
            result.Message = RouteTableCloner.CloneRouteMessage(
                translation.TranslatedMessage);
            result.Routed = true;
        }
        else if (translation.Disposition ==
            DiameterTranslationDisposition.Error)
        {
            result.Message = BuildTranslationErrorAnswer(translation);
            result.Error = true;
        }

        return result;
    }

    internal DiameterTranslationResult TranslateDispatchedAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer)
    {
        if (requestTranslation == null || answer == null ||
            requestTranslation.Disposition !=
                DiameterTranslationDisposition.Translated ||
            requestTranslation.SourceMessage == null ||
            requestTranslation.TranslatedMessage == null ||
            requestTranslation.TranslationState == null ||
            requestTranslation.Code == null ||
            requestTranslation.Message == null)
        {
            return CreateAnswerTranslationError(
                null,
                0U,
                0U,
                DiameterResultCodes.UnableToDeliver,
                "translation-answer-context-invalid",
                "A completed request translation and answer are required.");
        }

        DiameterMessage sourceRequest = requestTranslation.SourceMessage;
        DiameterMessage translatedRequest =
            requestTranslation.TranslatedMessage;
        if (requestTranslation.SourceApplicationId !=
                sourceRequest.ApplicationId ||
            requestTranslation.TargetApplicationId !=
                translatedRequest.ApplicationId ||
            !IsValidTranslatedRequest(
                sourceRequest,
                translatedRequest,
                requestTranslation.TargetApplicationId))
        {
            return CreateAnswerTranslationError(
                sourceRequest,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-answer-context-invalid",
                "The request translation context is inconsistent.");
        }

        if (!IsCorrelatedTranslationAnswer(translatedRequest, answer))
        {
            return CreateAnswerTranslationError(
                sourceRequest,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-answer-correlation-invalid",
                "The answer does not match the translated request.");
        }

        DiameterTranslationHandlerConfiguration? registration =
            FindTranslationHandler(
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId);
        if (registration == null || registration.Handler == null)
        {
            return CreateUntranslatedTranslation(
                answer,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                "translation-handler-unavailable",
                "The request translation handler is no longer available.");
        }

        DiameterTranslationResult handlerResult =
            registration.Handler.TranslateDiameterAnswer(
                RouteTableCloner.CloneTranslationResult(requestTranslation),
                RouteTableCloner.CloneRouteMessage(answer));
        return NormalizeAnswerTranslation(
            handlerResult,
            requestTranslation,
            answer);
    }

    internal DiameterTranslationResult NormalizeRequestTranslation(
        DiameterTranslationResult? handlerResult,
        DiameterMessage source,
        uint sourceApplicationId,
        uint targetApplicationId)
    {
        if (handlerResult == null || handlerResult.Code == null ||
            handlerResult.Message == null ||
            handlerResult.TranslationState == null ||
            ValueCodecs.FindInvalidUtf16Index(handlerResult.Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(handlerResult.Message) >= 0)
        {
            return CreateTranslationError(
                source,
                sourceApplicationId,
                targetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-result-invalid",
                "The translation handler returned an invalid result.");
        }

        if (handlerResult.Disposition ==
            DiameterTranslationDisposition.Untranslated)
        {
            return CreateUntranslatedTranslation(
                source,
                sourceApplicationId,
                targetApplicationId,
                ReadTranslationCode(
                    handlerResult.Code,
                    "translation-untranslated"),
                handlerResult.Message);
        }

        if (handlerResult.Disposition == DiameterTranslationDisposition.Error)
        {
            uint resultCode = ReadTranslationErrorResultCode(
                handlerResult.ResultCode);

            return CreateTranslationError(
                source,
                sourceApplicationId,
                targetApplicationId,
                resultCode,
                ReadTranslationCode(
                    handlerResult.Code,
                    "translation-error"),
                handlerResult.Message);
        }

        if (handlerResult.Disposition !=
                DiameterTranslationDisposition.Translated ||
            handlerResult.TranslatedMessage == null ||
            handlerResult.SourceApplicationId != sourceApplicationId ||
            handlerResult.TargetApplicationId != targetApplicationId ||
            !IsValidTranslatedRequest(
                source,
                handlerResult.TranslatedMessage,
                targetApplicationId))
        {
            return CreateTranslationError(
                source,
                sourceApplicationId,
                targetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-result-invalid",
                "The translated request does not preserve routing semantics.");
        }

        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceMessage = RouteTableCloner.CloneRouteMessage(source);
        result.TranslatedMessage = RouteTableCloner.CloneRouteMessage(
            handlerResult.TranslatedMessage);
        result.SourceApplicationId = sourceApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.TranslationState = CloneTranslationState(
            handlerResult.TranslationState);
        result.Code = ReadTranslationCode(
            handlerResult.Code,
            "translation-translated");
        result.Message = handlerResult.Message;
        return result;
    }

    internal DiameterTranslationResult NormalizeAnswerTranslation(
        DiameterTranslationResult? handlerResult,
        DiameterTranslationResult requestTranslation,
        DiameterMessage answer)
    {
        DiameterMessage? sourceValue = requestTranslation.SourceMessage;
        if (sourceValue == null)
        {
            return CreateAnswerTranslationError(
                null,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                DiameterResultCodes.UnableToDeliver,
                "translation-answer-context-invalid",
                "The original request is unavailable for answer translation.");
        }

        DiameterMessage sourceRequest = sourceValue;
        if (handlerResult == null || handlerResult.Code == null ||
            handlerResult.Message == null ||
            handlerResult.TranslationState == null ||
            ValueCodecs.FindInvalidUtf16Index(handlerResult.Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(handlerResult.Message) >= 0)
        {
            return CreateAnswerTranslationError(
                sourceRequest,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-answer-result-invalid",
                "The answer translation handler returned an invalid result.");
        }

        if (handlerResult.Disposition ==
            DiameterTranslationDisposition.Untranslated)
        {
            return CreateUntranslatedTranslation(
                answer,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                ReadTranslationCode(
                    handlerResult.Code,
                    "translation-answer-untranslated"),
                handlerResult.Message);
        }

        if (handlerResult.Disposition == DiameterTranslationDisposition.Error)
        {
            uint resultCode = ReadTranslationErrorResultCode(
                handlerResult.ResultCode);

            return CreateAnswerTranslationError(
                sourceRequest,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                resultCode,
                ReadTranslationCode(
                    handlerResult.Code,
                    "translation-answer-error"),
                handlerResult.Message);
        }

        if (handlerResult.Disposition !=
                DiameterTranslationDisposition.Translated ||
            handlerResult.TranslatedMessage == null ||
            handlerResult.SourceApplicationId !=
                requestTranslation.SourceApplicationId ||
            handlerResult.TargetApplicationId !=
                requestTranslation.TargetApplicationId ||
            !IsValidTranslatedAnswer(
                sourceRequest,
                handlerResult.TranslatedMessage))
        {
            return CreateAnswerTranslationError(
                sourceRequest,
                requestTranslation.SourceApplicationId,
                requestTranslation.TargetApplicationId,
                DiameterResultCodes.InvalidAvpValue,
                "translation-answer-result-invalid",
                "The translated answer does not restore source semantics.");
        }

        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceMessage = RouteTableCloner.CloneRouteMessage(answer);
        result.TranslatedMessage = RouteTableCloner.CloneRouteMessage(
            handlerResult.TranslatedMessage);
        result.SourceApplicationId = requestTranslation.SourceApplicationId;
        result.TargetApplicationId = requestTranslation.TargetApplicationId;
        result.TranslationState = CloneTranslationState(
            handlerResult.TranslationState);
        result.Code = ReadTranslationCode(
            handlerResult.Code,
            "translation-answer-translated");
        result.Message = handlerResult.Message;
        return result;
    }

    internal DiameterTranslationHandlerConfiguration? FindTranslationHandler(
        uint sourceApplicationId,
        uint targetApplicationId)
    {
        int index = 0;
        while (index < Handlers.Count)
        {
            DiameterTranslationHandlerConfiguration registration =
                Handlers[index];
            if (registration.Enabled &&
                registration.SourceApplicationId == sourceApplicationId &&
                registration.TargetApplicationId == targetApplicationId)
            {
                return registration;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsValidTranslatedRequest(
        DiameterMessage source,
        DiameterMessage translated,
        uint targetApplicationId)
    {
        return translated.Version == source.Version &&
            IsOwnedTranslationMessage(translated) &&
            translated.CommandCode != 0U &&
            translated.CommandCode <=
                DiameterProtocolConstants.MaximumCommandCode &&
            translated.ApplicationId == targetApplicationId &&
            translated.HopByHopIdentifier == source.HopByHopIdentifier &&
            translated.EndToEndIdentifier == source.EndToEndIdentifier &&
            (translated.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None &&
            (translated.Flags & DiameterMessageFlags.Error) ==
                DiameterMessageFlags.None;
    }

    internal bool IsCorrelatedTranslationAnswer(
        DiameterMessage request,
        DiameterMessage answer)
    {
        return answer.Version == request.Version &&
            IsOwnedTranslationMessage(answer) &&
            answer.CommandCode == request.CommandCode &&
            answer.ApplicationId == request.ApplicationId &&
            answer.HopByHopIdentifier == request.HopByHopIdentifier &&
            answer.EndToEndIdentifier == request.EndToEndIdentifier &&
            (answer.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None &&
            (answer.Flags & DiameterMessageFlags.Proxyable) ==
                (request.Flags & DiameterMessageFlags.Proxyable);
    }

    internal bool IsValidTranslatedAnswer(
        DiameterMessage sourceRequest,
        DiameterMessage translated)
    {
        return translated.Version == sourceRequest.Version &&
            IsOwnedTranslationMessage(translated) &&
            translated.CommandCode == sourceRequest.CommandCode &&
            translated.ApplicationId == sourceRequest.ApplicationId &&
            translated.HopByHopIdentifier ==
                sourceRequest.HopByHopIdentifier &&
            translated.EndToEndIdentifier ==
                sourceRequest.EndToEndIdentifier &&
            (translated.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None &&
            (translated.Flags & DiameterMessageFlags.Proxyable) ==
                (sourceRequest.Flags & DiameterMessageFlags.Proxyable);
    }

    internal DiameterTranslationResult CreateUntranslatedTranslation(
        DiameterMessage? source,
        uint sourceApplicationId,
        uint targetApplicationId,
        string code,
        string message)
    {
        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Untranslated;
        if (source != null)
        {
            result.SourceMessage = RouteTableCloner.CloneRouteMessage(source);
        }

        result.SourceApplicationId = sourceApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal DiameterTranslationResult CreateTranslationError(
        DiameterMessage? source,
        uint sourceApplicationId,
        uint targetApplicationId,
        uint resultCode,
        string code,
        string message)
    {
        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Error;
        if (source != null)
        {
            result.SourceMessage = RouteTableCloner.CloneRouteMessage(source);
        }

        result.SourceApplicationId = sourceApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.ResultCode = resultCode;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal TranslationRequestDispatchResult CreateTranslationDispatchFailure(
        DiameterMessage? source,
        uint targetApplicationId,
        uint resultCode,
        string code,
        string message)
    {
        TranslationRequestDispatchResult result =
            new TranslationRequestDispatchResult();
        uint sourceApplicationId = 0U;
        if (source != null)
        {
            sourceApplicationId = source.ApplicationId;
        }

        result.Translation = CreateTranslationError(
            source,
            sourceApplicationId,
            targetApplicationId,
            resultCode,
            code,
            message);
        result.Error = true;
        result.Code = code;
        result.MessageText = message;
        if (source != null)
        {
            result.Message = BuildTranslationErrorAnswer(result.Translation);
        }

        return result;
    }

    internal DiameterTranslationResult CreateAnswerTranslationError(
        DiameterMessage? source,
        uint sourceApplicationId,
        uint targetApplicationId,
        uint resultCode,
        string code,
        string message)
    {
        DiameterTranslationResult result = CreateTranslationError(
            source,
            sourceApplicationId,
            targetApplicationId,
            ReadTranslationErrorResultCode(resultCode),
            code,
            message);
        result.TranslatedMessage = BuildTranslationErrorAnswer(result);
        return result;
    }

    internal DiameterMessage? BuildTranslationErrorAnswer(
        DiameterTranslationResult translation)
    {
        if (translation.SourceMessage == null)
        {
            return null;
        }

        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = RouteTableCloner.CloneRouteMessage(
            translation.SourceMessage);
        error.ResultCode = translation.ResultCode;
        error.ErrorMessage = translation.Message;
        return Validator.BuildResultErrorAnswer(
            error,
            LocalHost,
            LocalRealm);
    }

    internal byte[] CloneTranslationState(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal string ReadTranslationCode(string value, string fallback)
    {
        return value.Length == 0 ? fallback : value;
    }

    internal uint ReadTranslationErrorResultCode(uint value)
    {
        return value < 3000U || value >= 6000U ?
            DiameterResultCodes.UnableToDeliver : value;
    }

    internal bool IsOwnedTranslationMessage(DiameterMessage message)
    {
        if (message.Avps == null)
        {
            return false;
        }

        int flagValue = (int)message.Flags;
        if (flagValue < 0 || flagValue > 255 ||
            (flagValue & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            return false;
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
