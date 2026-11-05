using System;

namespace diameter;

internal class ApplicationDispatchPreparation
{
    internal bool Ready;
    internal DiameterMessage Request = new DiameterMessage();
    internal ApplicationRegistryEntry? Entry;
    internal DiameterCommandRegistration? Command;
    internal DiameterDictionarySnapshot? DictionarySnapshot;
    internal DiameterRequestContext? Context;
    internal IDiameterRequestHandler? Handler;
    internal DiameterApplicationResult Result =
        new DiameterApplicationResult();
}

internal class ApplicationDispatcher
{
    internal ApplicationRegistry Registry;
    internal IDiameterDictionary Dictionary;
    internal IDiameterValidator Validator;
    internal IDiameterClock Clock;
    internal DiameterLimitConfiguration Limits;
    internal DiameterIdentity OriginHost;
    internal DiameterIdentity OriginRealm;

    internal ApplicationDispatcher(
        ApplicationRegistry registry,
        IDiameterDictionary dictionary,
        IDiameterValidator validator,
        IDiameterClock clock,
        DiameterLimitConfiguration limits,
        DiameterIdentity originHost,
        DiameterIdentity originRealm)
    {
        if (registry == null || dictionary == null || validator == null ||
            clock == null || limits == null || originHost == null ||
            originRealm == null)
        {
            throw new ArgumentException(
                "Application dispatch dependencies are required.");
        }

        limits.ValidateConfiguration();
        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Application dispatch requires valid local identities.");
        }

        Registry = registry;
        Dictionary = dictionary;
        Validator = validator;
        Clock = clock;
        Limits = CloneApplicationLimits(limits);
        OriginHost = new DiameterIdentity();
        OriginHost.Value = normalizedHost.Text;
        OriginRealm = new DiameterIdentity();
        OriginRealm.Value = normalizedRealm.Text;
    }

    internal DiameterApplicationResult DispatchInboundApplicationRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity,
        string? routeIdentifier,
        bool duplicate,
        bool cancelled,
        bool shuttingDown)
    {
        ApplicationDispatchPreparation preparation =
            PrepareInboundApplicationRequest(
                request,
                peerIdentity,
                routeIdentifier,
                duplicate,
                cancelled,
                shuttingDown);
        if (!preparation.Ready || preparation.Handler == null ||
            preparation.Context == null)
        {
            return preparation.Result;
        }

        DiameterApplicationResult handlerResult =
            preparation.Handler.HandleDiameterApplicationRequest(
                preparation.Context);
        return CompleteInboundApplicationRequest(
            preparation,
            handlerResult);
    }

    internal ApplicationDispatchPreparation PrepareInboundApplicationRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity,
        string? routeIdentifier,
        bool duplicate,
        bool cancelled,
        bool shuttingDown)
    {
        ApplicationDispatchPreparation preparation =
            new ApplicationDispatchPreparation();
        if (request == null ||
            !ValidateInboundApplicationRequestHeader(request))
        {
            preparation.Result = CreateApplicationDropResult(
                "application-request-invalid",
                "The inbound message is not a safe Diameter request.");
            return preparation;
        }

        DiameterMessage workingRequest = CloneApplicationMessage(request);
        preparation.Request = workingRequest;
        if (peerIdentity == null ||
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity).Status !=
                DiameterCodecStatus.Success ||
            routeIdentifier == null ||
            ValueCodecs.FindInvalidUtf16Index(routeIdentifier) >= 0)
        {
            preparation.Result = CreateApplicationErrorResult(
                workingRequest,
                DiameterResultCodes.UnableToComply,
                "application-request-context-invalid",
                "The inbound request context is invalid.");
            return preparation;
        }

        ApplicationRegistryLookup lookup = Registry.ReadApplicationLookup(
            workingRequest.ApplicationId,
            workingRequest.CommandCode,
            true);
        ApplicationRegistryEntry? entry = lookup.Entry;
        if (entry == null)
        {
            uint resultCode = lookup.ApplicationRegistered ?
                DiameterResultCodes.CommandUnsupported :
                DiameterResultCodes.ApplicationUnsupported;
            string code = lookup.ApplicationRegistered ?
                "application-command-unsupported" :
                "application-unsupported";
            string description = lookup.ApplicationRegistered ?
                "No local handler is registered for the command." :
                "No local application is registered for the Application-Id.";
            preparation.Result = CreateApplicationErrorResult(
                workingRequest,
                resultCode,
                code,
                description);
            return preparation;
        }

        preparation.Entry = entry;

        DiameterCommandRegistration? command =
            ReadApplicationCommandRegistration(
                entry.Registration,
                workingRequest.CommandCode,
                true);
        if (command == null)
        {
            preparation.Result = CreateApplicationErrorResult(
                workingRequest,
                DiameterResultCodes.CommandUnsupported,
                "application-command-registration-missing",
                "The selected application command registration is missing.");
            return preparation;
        }

        preparation.Command = command;

        DiameterDictionarySnapshot? snapshot = null;
        DiameterValidationResult? validation = null;
        if (command.RequireValidation)
        {
            snapshot = Dictionary.ReadDictionarySnapshot();
            validation = Validator.ValidateMessage(
                workingRequest,
                snapshot,
                Limits);
            if (validation == null ||
                validation.Status != DiameterValidationStatus.Valid)
            {
                preparation.Result = CreateValidationErrorResult(
                    workingRequest,
                    validation);
                return preparation;
            }
        }

        preparation.DictionarySnapshot = snapshot;

        DiameterRequestContext context = CreateApplicationRequestContext(
            entry.Registration,
            command,
            workingRequest,
            peerIdentity,
            routeIdentifier,
            validation,
            duplicate,
            cancelled,
            shuttingDown);
        IDiameterRequestHandler? handler =
            entry.Application.FindDiameterApplicationRequestHandler(
                workingRequest.CommandCode);
        if (handler == null)
        {
            DiameterApplicationResult missingHandler =
                CreateApplicationErrorResult(
                workingRequest,
                DiameterResultCodes.UnableToComply,
                "application-handler-missing",
                "The registered application did not supply a request handler.");
            missingHandler.RequestContext = context;
            preparation.Result = missingHandler;
            return preparation;
        }

        preparation.Context = context;
        preparation.Handler = handler;
        preparation.Ready = true;
        return preparation;
    }

    internal DiameterApplicationResult CompleteInboundApplicationRequest(
        ApplicationDispatchPreparation preparation,
        DiameterApplicationResult? handlerResult)
    {
        if (!preparation.Ready || preparation.Context == null ||
            preparation.Command == null)
        {
            return preparation.Result;
        }

        return ProcessApplicationHandlerResult(
            preparation.Request,
            preparation.Context,
            preparation.Command,
            preparation.DictionarySnapshot,
            handlerResult);
    }

    internal bool ValidateInboundApplicationRequestHeader(
        DiameterMessage? request)
    {
        int flags = request == null ? 0 : (int)request.Flags;
        if (request == null || request.Avps == null ||
            request.Version != DiameterProtocolConstants.Version ||
            request.CommandCode == 0U ||
            request.CommandCode > DiameterProtocolConstants.MaximumCommandCode ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (request.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            flags < 0 || flags > 255 ||
            (flags & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            return false;
        }

        return IsOwnedApplicationMessage(request);
    }

    internal DiameterCommandRegistration? ReadApplicationCommandRegistration(
        DiameterApplicationRegistration registration,
        uint commandCode,
        bool request)
    {
        int index = 0;
        while (index < registration.Commands.Count)
        {
            DiameterCommandRegistration command =
                registration.Commands[index];
            if (command.CommandCode == commandCode &&
                command.IsRequest == request)
            {
                return command;
            }

            index = index + 1;
        }

        return null;
    }

    internal DiameterRequestContext CreateApplicationRequestContext(
        DiameterApplicationRegistration registration,
        DiameterCommandRegistration command,
        DiameterMessage request,
        DiameterIdentity peerIdentity,
        string routeIdentifier,
        DiameterValidationResult? validation,
        bool duplicate,
        bool cancelled,
        bool shuttingDown)
    {
        DiameterRequestContext context = new DiameterRequestContext();
        context.Registration =
            ApplicationRegistryCloner.CloneApplicationRegistration(
                registration);
        context.Command =
            ApplicationRegistryCloner.CloneApplicationCommand(command);
        context.Request = CloneApplicationMessage(request);
        context.PeerIdentity = CloneApplicationIdentity(peerIdentity);
        context.RouteIdentifier = routeIdentifier;
        context.SessionId = ReadApplicationSessionIdentifier(request);
        context.ValidationResult = validation;
        context.ReceivedAt = Clock.ReadUtcTime();
        context.Duplicate = duplicate;
        context.Retransmission =
            (request.Flags &
            DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None;
        context.Cancelled = cancelled;
        context.ShuttingDown = shuttingDown;
        context.ValidateRequestContext();
        return context;
    }

    internal string ReadApplicationSessionIdentifier(DiameterMessage request)
    {
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp.Code == DiameterAvpCodes.SessionId &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status == DiameterCodecStatus.Success)
                {
                    return decoded.Text;
                }

                return string.Empty;
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal DiameterApplicationResult ProcessApplicationHandlerResult(
        DiameterMessage request,
        DiameterRequestContext context,
        DiameterCommandRegistration command,
        DiameterDictionarySnapshot? snapshot,
        DiameterApplicationResult? handlerResult)
    {
        if (handlerResult == null ||
            CountApplicationResultActions(handlerResult) != 1 ||
            handlerResult.RedirectHosts == null ||
            handlerResult.Code == null ||
            handlerResult.Description == null ||
            (handlerResult.ResultCode != 0U &&
            (handlerResult.ResultCode < 1000U ||
            handlerResult.ResultCode > 5999U)) ||
            ValueCodecs.FindInvalidUtf16Index(handlerResult.Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(
                handlerResult.Description) >= 0)
        {
            DiameterApplicationResult invalidResult =
                CreateApplicationErrorResult(
                request,
                DiameterResultCodes.UnableToComply,
                "application-handler-result-invalid",
                "The application returned an invalid processing result.");
            invalidResult.RequestContext = context;
            return invalidResult;
        }

        if (handlerResult.Drop && handlerResult.OutputMessage == null &&
            handlerResult.RedirectHosts != null &&
            handlerResult.RedirectHosts.Count == 0 &&
            handlerResult.ResultCode == 0U)
        {
            DiameterApplicationResult result = CreateApplicationDropResult(
                handlerResult.Code,
                handlerResult.Description);
            result.RequestContext = context;
            return result;
        }

        if (handlerResult.Fail && handlerResult.OutputMessage == null &&
            handlerResult.RedirectHosts != null &&
            handlerResult.RedirectHosts.Count == 0 &&
            handlerResult.ResultCode >= 3000U &&
            handlerResult.ResultCode < 6000U)
        {
            DiameterApplicationResult result =
                CreateApplicationErrorResult(
                    request,
                    handlerResult.ResultCode,
                    handlerResult.Code,
                    handlerResult.Description);
            result.RequestContext = context;
            return result;
        }

        if (handlerResult.Answer &&
            handlerResult.OutputMessage != null &&
            handlerResult.RedirectHosts != null &&
            handlerResult.RedirectHosts.Count == 0 &&
            ValidateLocalApplicationAnswer(
                request,
                handlerResult.OutputMessage,
                command,
                snapshot))
        {
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Answer = true;
            result.RequestContext = context;
            result.OutputMessage = CloneApplicationMessage(
                handlerResult.OutputMessage);
            result.Code = handlerResult.Code;
            result.Description = handlerResult.Description;
            return result;
        }

        DiameterApplicationResult invalidAnswer =
            CreateApplicationErrorResult(
            request,
            DiameterResultCodes.UnableToComply,
            "application-answer-invalid",
            "The application did not produce a valid correlated answer.");
        invalidAnswer.RequestContext = context;
        return invalidAnswer;
    }

    internal bool ValidateLocalApplicationAnswer(
        DiameterMessage request,
        DiameterMessage? answer,
        DiameterCommandRegistration command,
        DiameterDictionarySnapshot? snapshot)
    {
        if (answer == null || !IsOwnedApplicationMessage(answer) ||
            !AreApplicationMessageHeadersCorrelated(request, answer))
        {
            return false;
        }

        if (!command.RequireValidation)
        {
            return true;
        }

        if (snapshot == null)
        {
            return false;
        }

        DiameterValidationResult validation = Validator.ValidateMessage(
            answer,
            snapshot,
            Limits);
        return validation != null &&
            validation.Status == DiameterValidationStatus.Valid;
    }

    internal bool AreApplicationMessageHeadersCorrelated(
        DiameterMessage request,
        DiameterMessage answer)
    {
        return answer.Version == request.Version &&
            answer.CommandCode == request.CommandCode &&
            answer.ApplicationId == request.ApplicationId &&
            answer.HopByHopIdentifier == request.HopByHopIdentifier &&
            answer.EndToEndIdentifier == request.EndToEndIdentifier &&
            (answer.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None &&
            (answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) ==
                DiameterMessageFlags.None &&
            (((int)answer.Flags) &
                DiameterProtocolConstants.MessageReservedFlagMask) == 0;
    }

    internal bool IsOwnedApplicationMessage(DiameterMessage message)
    {
        if (message.Avps == null)
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

    internal int CountApplicationResultActions(
        DiameterApplicationResult result)
    {
        int count = 0;
        if (result.Continue)
        {
            count = count + 1;
        }

        if (result.Replace)
        {
            count = count + 1;
        }

        if (result.Answer)
        {
            count = count + 1;
        }

        if (result.Drop)
        {
            count = count + 1;
        }

        if (result.Redirect)
        {
            count = count + 1;
        }

        if (result.Fail)
        {
            count = count + 1;
        }

        return count;
    }

    internal DiameterApplicationResult CreateValidationErrorResult(
        DiameterMessage request,
        DiameterValidationResult? validation)
    {
        if (validation == null || validation.ErrorAnswer == null)
        {
            return CreateApplicationErrorResult(
                request,
                DiameterResultCodes.UnableToComply,
                "application-request-validation-unavailable",
                "The request validation failure could not be described.");
        }

        DiameterMessage answer = Validator.BuildValidationErrorAnswer(
            validation.ErrorAnswer,
            OriginHost,
            OriginRealm);
        DiameterApplicationResult result = new DiameterApplicationResult();
        result.Continue = false;
        result.Answer = true;
        result.OutputMessage = CloneApplicationMessage(answer);
        result.ResultCode = validation.ErrorAnswer.ResultCode;
        result.Code = "application-request-validation-failed";
        result.Description = validation.ErrorAnswer.ErrorMessage;
        return result;
    }

    internal DiameterApplicationResult CreateApplicationErrorResult(
        DiameterMessage request,
        uint resultCode,
        string code,
        string description)
    {
        DiameterErrorAnswerRequest error =
            new DiameterErrorAnswerRequest();
        error.Request = CloneApplicationMessage(request);
        error.ResultCode = resultCode;
        error.ErrorMessage = description;
        DiameterMessage answer = Validator.BuildResultErrorAnswer(
            error,
            OriginHost,
            OriginRealm);
        DiameterApplicationResult result = new DiameterApplicationResult();
        result.Continue = false;
        result.Answer = true;
        result.OutputMessage = CloneApplicationMessage(answer);
        result.ResultCode = resultCode;
        result.Code = code;
        result.Description = description;
        return result;
    }

    internal DiameterApplicationResult CreateApplicationDropResult(
        string code,
        string description)
    {
        DiameterApplicationResult result = new DiameterApplicationResult();
        result.Continue = false;
        result.Drop = true;
        result.Code = code;
        result.Description = description;
        return result;
    }

    internal DiameterMessage CloneApplicationMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[index];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = new byte[sourceAvp.Data.Length];
            Array.Copy(sourceAvp.Data, avp.Data, sourceAvp.Data.Length);
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }

    internal DiameterIdentity CloneApplicationIdentity(
        DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal DiameterLimitConfiguration CloneApplicationLimits(
        DiameterLimitConfiguration source)
    {
        DiameterLimitConfiguration result =
            new DiameterLimitConfiguration();
        result.MaximumMessageSize = source.MaximumMessageSize;
        result.MaximumAvpCount = source.MaximumAvpCount;
        result.MaximumGroupedDepth = source.MaximumGroupedDepth;
        result.MaximumGroupedAvpCount = source.MaximumGroupedAvpCount;
        result.MaximumGroupedBytes = source.MaximumGroupedBytes;
        result.MaximumCodecIssueCount = source.MaximumCodecIssueCount;
        result.MaximumConnectionCount = source.MaximumConnectionCount;
        result.MaximumPendingRequests = source.MaximumPendingRequests;
        result.IngressQueueCapacity = source.IngressQueueCapacity;
        result.EgressQueueCapacity = source.EgressQueueCapacity;
        result.ApplicationQueueCapacity = source.ApplicationQueueCapacity;
        return result;
    }
}
