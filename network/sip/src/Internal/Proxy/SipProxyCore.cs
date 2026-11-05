namespace sip;

internal class SipProxyCore
{
    object sync;
    ISipDnsResolver resolver;
    SipDnsOptions dnsOptions;
    ISipRoutePolicy? routePolicy;
    ISipLocationService? locationService;
    ISipAuthenticationServer? authenticationServer;
    ISipAuthorizationPolicy? authorizationPolicy;
    SipNonceOptions? nonceOptions;
    SipProxySafetyOptions safetyOptions;
    SipProtocolTokenGenerator tokens;
    System.Collections.Generic.List<SipProxyBranchContext> branches;
    System.Collections.Generic.List<SipProxyBranchContext> lateBranches;
    System.Collections.Generic.List<SipProxyOperationContext> operations;
    System.Collections.Generic.List<SipProxyEvent> events;
    System.Collections.Generic.List<long> admissionTimes;
    long nowMilliseconds;
    const int LateBranchLimit = 128;

    internal SipProxyCore()
    {
        sync = new object();
        resolver = new SipDnsResolver();
        dnsOptions = new SipDnsOptions();
        safetyOptions = new SipProxySafetyOptions();
        tokens = new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());
        branches = new System.Collections.Generic.List<SipProxyBranchContext>();
        lateBranches = new System.Collections.Generic.List<SipProxyBranchContext>();
        operations = new System.Collections.Generic.List<SipProxyOperationContext>();
        events = new System.Collections.Generic.List<SipProxyEvent>();
        admissionTimes = new System.Collections.Generic.List<long>();
    }

    internal void ConfigureResolver(ISipDnsResolver configuredResolver,
        SipDnsOptions configuredOptions)
    {
        if (configuredResolver == null)
        {
            throw new System.ArgumentNullException("resolver");
        }
        if (configuredOptions == null)
        {
            throw new System.ArgumentNullException("options");
        }
        lock (sync)
        {
            resolver = configuredResolver;
            dnsOptions = CopyDnsOptions(configuredOptions);
        }
    }

    internal void ConfigureRoutePolicy(ISipRoutePolicy configuredPolicy)
    {
        if (configuredPolicy == null)
        {
            throw new System.ArgumentNullException("routePolicy");
        }
        lock (sync)
        {
            routePolicy = configuredPolicy;
        }
    }

    internal void ConfigureLocationService(
        ISipLocationService configuredService)
    {
        if (configuredService == null)
        {
            throw new System.ArgumentNullException("locationService");
        }
        lock (sync)
        {
            locationService = configuredService;
        }
    }

    internal void ConfigureSecurity(
        ISipAuthenticationServer configuredAuthenticationServer,
        ISipAuthorizationPolicy configuredAuthorizationPolicy,
        SipNonceOptions configuredNonceOptions)
    {
        if (configuredAuthenticationServer == null)
        {
            throw new System.ArgumentNullException("authenticationServer");
        }
        if (configuredAuthorizationPolicy == null)
        {
            throw new System.ArgumentNullException("authorizationPolicy");
        }
        if (configuredNonceOptions == null)
        {
            throw new System.ArgumentNullException("nonceOptions");
        }
        lock (sync)
        {
            authenticationServer = configuredAuthenticationServer;
            authorizationPolicy = configuredAuthorizationPolicy;
            nonceOptions = CopyNonceOptions(configuredNonceOptions);
        }
    }

    internal void ConfigureSafety(SipProxySafetyOptions configuredOptions)
    {
        if (configuredOptions == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (!ValidSafetyOptions(configuredOptions))
        {
            throw new System.ArgumentException(
                "The proxy safety options are invalid.", "options");
        }
        lock (sync)
        {
            safetyOptions = CopySafetyOptions(configuredOptions);
            TrimAdmissionTimes();
            while (events.Count > safetyOptions.MaxEventCount)
            {
                events.RemoveAt(0);
            }
        }
    }

    internal SipProxyResult ProcessRequest(SipProxyRequest? request)
    {
        lock (sync)
        {
            if (request == null || request.Options == null)
            {
                return RequestFailure(request, null,
                    SipStatusCodes.BadRequest,
                    "A proxy request and options are required.");
            }
            SipMessage? controlMessage = request.Message;
            if (controlMessage == null && request.TransportMessage != null)
            {
                controlMessage = request.TransportMessage.Message;
            }
            if (controlMessage != null
                && controlMessage.Method == SipMethod.Cancel)
            {
                return ProcessCancelRequest(request, controlMessage);
            }
            if (controlMessage != null
                && controlMessage.Method == SipMethod.Ack)
            {
                SipProxyBranchContext? failureContext =
                    FindFailureAckContext(controlMessage);
                if (failureContext != null)
                {
                    return ProcessFailureAck(request,
                        controlMessage, failureContext);
                }
            }
            SipProxyResult? admissionFailure =
                ApplyAdmissionSafety(request, controlMessage);
            if (admissionFailure != null)
            {
                return admissionFailure;
            }
            SipProxyRequest? effectiveValue = CopyRequest(request);
            SipProxyRequest effective = effectiveValue == null
                ? new SipProxyRequest() : effectiveValue;
            SipProxyResult? securityFailure = ApplyProxySecurity(effective);
            if (securityFailure != null)
            {
                return securityFailure;
            }
            SipRouteDecision? redirectDecision = null;
            if (routePolicy != null)
            {
                SipProxyRequest? policyValue = CopyRequest(effective);
                SipRouteDecision decision = routePolicy.ChooseRoute(
                    policyValue == null ? new SipProxyRequest() : policyValue);
                if (decision == null)
                {
                    return RequestFailure(request, request.Message,
                        SipStatusCodes.ServerInternalError,
                        "The proxy route policy returned no decision.");
                }
                redirectDecision = CopyRouteDecision(decision);
                if (decision.RejectStatusCode != 0)
                {
                    int rejection = decision.RejectStatusCode >= 300
                        && decision.RejectStatusCode <= 699
                        ? decision.RejectStatusCode : SipStatusCodes.Forbidden;
                    return RequestFailure(request, request.Message, rejection,
                        decision.Reason == null
                            ? "The proxy route policy rejected the request."
                            : decision.Reason);
                }
                if (decision.Mode != SipProxyMode.None)
                {
                    effective.Options.Mode = decision.Mode;
                }
                effective.Options.RecordRoute = decision.RecordRoute;
                effective.Targets = CopyTargets(decision.Targets);
                if (decision.Redirect)
                {
                    effective.Options.Mode = SipProxyMode.Redirect;
                }
            }
            if ((effective.Targets == null
                    || effective.Targets.Length == 0)
                && locationService != null)
            {
                SipMessage? source = effective.Message;
                if (source == null && effective.TransportMessage != null)
                {
                    source = effective.TransportMessage.Message;
                }
                if (source != null && source.RequestUri != null)
                {
                    SipLocationQuery query = new SipLocationQuery();
                    query.AddressOfRecord =
                        SipUriModelCopy.CreateNormalizedCopy(source.RequestUri);
                    query.NowMilliseconds = nowMilliseconds;
                    SipLocationResult located =
                        locationService.QueryLocation(query);
                    if (!located.Success)
                    {
                        return RequestFailure(request, source,
                            SipStatusCodes.BadGateway,
                            located.ErrorMessage == null
                                ? "The proxy location lookup failed."
                                : located.ErrorMessage);
                    }
                    effective.Targets =
                        CreateLocationTargets(located.Bindings);
                    if (effective.Targets.Length == 0
                        && effective.Options.Mode != SipProxyMode.Redirect)
                    {
                        return RequestFailure(request, source,
                            SipStatusCodes.NotFound,
                            "The proxy location lookup found no target.");
                    }
                }
            }
            if (effective.Options.Mode == SipProxyMode.Redirect)
            {
                if (redirectDecision == null)
                {
                    redirectDecision = new SipRouteDecision();
                    redirectDecision.Mode = SipProxyMode.Redirect;
                    redirectDecision.Redirect = true;
                }
                redirectDecision.Targets = CopyTargets(effective.Targets);
                return ProcessRedirectRequest(effective, redirectDecision);
            }
            if (controlMessage != null
                && controlMessage.Method == SipMethod.Ack)
            {
                effective.Options.Mode = SipProxyMode.Stateless;
            }
            if (effective.Options.Mode == SipProxyMode.Stateless)
            {
                return ProcessSingleRequest(effective, false);
            }
            if (effective.Options.Mode == SipProxyMode.StatefulParallel
                || effective.Options.Mode == SipProxyMode.StatefulSequential)
            {
                return ProcessStatefulRequest(effective);
            }
            return RequestFailure(effective, effective.Message,
                SipStatusCodes.BadRequest,
                "A supported proxy mode is required.");
        }
    }

    SipProxyResult ProcessRedirectRequest(SipProxyRequest request,
        SipRouteDecision decision)
    {
        SipMessage? source = request.Message;
        if (source == null && request.TransportMessage != null)
        {
            source = request.TransportMessage.Message;
        }
        SipMessage? responseRequest =
            SipSnapshotCopy.CopyMessageModel(source);
        if (responseRequest != null && request.TransportMessage != null)
        {
            SipViaRouting.ApplyReceivedAndRPort(responseRequest,
                request.TransportMessage.RemoteAddress);
        }
        SipRedirectServerCore redirectServer =
            new SipRedirectServerCore();
        SipRedirectResult redirect =
            redirectServer.Create(responseRequest, decision);
        SipProxyResult result = new SipProxyResult();
        result.Response = SipSnapshotCopy.CopyMessageModel(redirect.Response);
        result.Success = redirect.Response != null;
        result.Completed = redirect.Response != null;
        result.StatusCode = redirect.Response == null
            ? SipStatusCodes.ServerInternalError
            : redirect.Response.StatusCode;
        result.ErrorMessage = redirect.ErrorMessage;
        result.CorrelationId = request.CorrelationId;
        result.AuthenticatedIdentity = request.Authentication == null
            ? null : request.Authentication.Identity;
        result.Targets = CopyTargets(decision.Targets);
        if (redirect.Response != null && request.TransportMessage != null)
        {
            SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                redirect.Response, request.TransportMessage);
            send.CorrelationId = request.CorrelationId;
            result.SendRequest = CopySend(send);
        }
        QueueEvent(redirect.Success
                ? SipProxyEventKind.RequestAccepted
                : SipProxyEventKind.Failed,
            request, result, null, redirect.Response,
            redirect.Success
                ? "A redirect response was generated."
                : redirect.ErrorMessage == null
                    ? "Redirect processing failed."
                    : redirect.ErrorMessage);
        return CopyResult(result);
    }

    SipProxyResult ProcessSingleRequest(SipProxyRequest? request,
        bool statefulBranch)
    {
        lock (sync)
        {
            if (request == null || request.Options == null
                || request.Options.Mode != SipProxyMode.Stateless)
            {
                return RequestFailure(request, null,
                    SipStatusCodes.BadRequest,
                    "A stateless proxy request and options are required.");
            }
            SipMessage? source = request.Message;
            if (source == null && request.TransportMessage != null)
            {
                source = request.TransportMessage.Message;
            }
            SipTransportMessage? received = request.TransportMessage;
            SipEndpointAddress? local = received == null
                ? null : received.LocalAddress;
            if (source == null || local == null
                || !ValidEndpoint(local))
            {
                return RequestFailure(request, source,
                    SipStatusCodes.BadRequest,
                    "A received SIP request and local proxy endpoint are required.");
            }
            if (request.Targets == null || request.Targets.Length > 1)
            {
                return RequestFailure(request, source,
                    SipStatusCodes.BadRequest,
                    "A stateless proxy accepts at most one target.");
            }
            if (!ValidProxyOptions(request.Options))
            {
                return RequestFailure(request, source,
                    SipStatusCodes.BadRequest,
                    "The proxy options are invalid.");
            }
            if (RetainedBranchCount() >= safetyOptions.MaxBranchCount)
            {
                return CreatePolicyResponse(request, source,
                    SipStatusCodes.ServiceUnavailable,
                    SipReasonPhrases.ServiceUnavailable, true);
            }
            if (source.ContactValues != null
                && !ValidContactSecurity(source,
                    request.Options.PreventSecureDowngrade))
            {
                return RequestFailure(request, source,
                    SipStatusCodes.UnsupportedUriScheme,
                    "A secure dialog-forming request requires SIPS Contact values.");
            }

            SipStatelessResult forwardedResult =
                SipStatelessTransaction.ForwardRequest(source);
            if (!forwardedResult.Success
                || forwardedResult.OutboundMessages.Length == 0)
            {
                int failureStatus = forwardedResult.StatusCode == 0
                    ? SipStatusCodes.BadRequest : forwardedResult.StatusCode;
                return RequestFailure(request, source, failureStatus,
                    forwardedResult.ErrorMessage == null
                        ? "The proxy request is invalid."
                        : forwardedResult.ErrorMessage);
            }
            SipMessage forwarded = forwardedResult.OutboundMessages[0];
            ApplyIdentityPolicy(forwarded, request.Authentication);
            SipViaRouting.ApplyReceivedAndRPort(
                forwarded, received == null ? null : received.RemoteAddress);

            SipUri localRoute = CreateLocalRoute(local, forwarded);
            int routeStatus = ValidateRouteProcessing(
                forwarded, localRoute);
            if (routeStatus != 0)
            {
                return RequestFailure(request, source, routeStatus,
                    routeStatus == SipStatusCodes.UnsupportedUriScheme
                        ? "Proxy routing cannot downgrade a SIPS target."
                        : "The proxy Route set is invalid.");
            }
            SipRouteRouting.PreprocessIncomingRoute(forwarded, localRoute);

            SipProxyTarget selected = request.Targets.Length == 0
                ? new SipProxyTarget() : CopyProxyTarget(request.Targets[0]);
            if (!ValidTarget(selected))
            {
                return RequestFailure(request, source,
                    SipStatusCodes.BadRequest,
                    "The stateless proxy target is invalid.");
            }
            if (selected.Uri != null)
            {
                if (forwarded.RequestUri != null
                    && forwarded.RequestUri.Scheme == SipUriScheme.Sips
                    && selected.Uri.Scheme != SipUriScheme.Sips)
                {
                    return RequestFailure(request, source,
                        SipStatusCodes.UnsupportedUriScheme,
                        "Proxy target rewriting cannot downgrade a SIPS URI.");
                }
                if (selected.RouteSet.Length > 0)
                {
                    SipRouteRouting.ApplyRouteSet(
                        forwarded, selected.Uri, selected.RouteSet);
                }
                else
                {
                    SipRequestRouting.RewriteRequestTarget(
                        forwarded, selected.Uri);
                }
            }
            routeStatus = ValidateStrictRouteProcessing(forwarded);
            if (routeStatus != 0)
            {
                return RequestFailure(request, source, routeStatus,
                    "Strict routing cannot downgrade a SIPS target.");
            }
            bool strictRoute =
                SipRouteRouting.PostprocessStrictRoute(forwarded);
            SipUri nextHop = strictRoute && forwarded.RequestUri != null
                ? SipUriModelCopy.CreateNormalizedCopy(forwarded.RequestUri)
                : SipRouteRouting.SelectNextHop(forwarded, null);
            if (selected.Uri == null)
            {
                selected.Uri = SipUriModelCopy.CreateNormalizedCopy(nextHop);
            }

            SipServerCandidate? candidate = SelectCandidate(
                selected, nextHop, request.CorrelationId,
                request.Options.PreventSecureDowngrade);
            if (candidate == null)
            {
                return RequestFailure(request, source,
                    SipStatusCodes.BadGateway,
                    "The proxy target could not be resolved.");
            }
            if (!CandidateSupportsTarget(candidate, nextHop,
                    request.Options.PreventSecureDowngrade))
            {
                return RequestFailure(request, source,
                    SipStatusCodes.UnsupportedUriScheme,
                    "The resolved transport cannot securely reach the proxy target.");
            }
            selected.Candidate = CopyCandidate(candidate);

            SipEndpointAddress sentBy = CopyEndpoint(local);
            sentBy.Transport = candidate.Transport;
            sentBy.Secure = IsSecureTransport(candidate.Transport);
            if (request.Options.RecordRoute)
            {
                SipUri recordRoute = CreateRecordRoute(sentBy, forwarded);
                SipRouteRouting.InsertRecordRoute(forwarded, recordRoute);
            }
            string branch = tokens.CreateBranch();
            SipViaRouting.InsertRequestVia(forwarded, sentBy, branch, true);
            selected.Branch = branch;

            SipSendRequest send = CreateRequestSend(
                forwarded, selected, sentBy, request);
            SipProxyResult result = new SipProxyResult();
            result.Success = true;
            result.Request = CopyMessageRequired(forwarded);
            result.Requests = new SipMessage[] {
                CopyMessageRequired(forwarded) };
            result.SendRequest = CopySend(send);
            result.SendRequests = new SipSendRequest[] {
                CopySend(send) };
            result.Targets = new SipProxyTarget[] {
                CopyProxyTarget(selected) };
            result.CorrelationId = request.CorrelationId;
            result.AuthenticatedIdentity = request.Authentication == null
                ? null : request.Authentication.Identity;

            if (forwarded.Method == SipMethod.Ack)
            {
                result.Completed = true;
                QueueEvent(SipProxyEventKind.AckForwarded,
                    request, result, selected, forwarded,
                    "A successful-response ACK was forwarded statelessly.");
                return CopyResult(result);
            }

            SipProxyBranchContext context = new SipProxyBranchContext();
            context.Branch = branch;
            context.SentByHost = sentBy.Host;
            context.SentByPort = sentBy.Port;
            context.ReceivedRequest = CopyTransportMessage(received);
            context.Target = CopyProxyTarget(selected);
            context.CorrelationId = request.CorrelationId;
            context.OutboundRequest = CopyMessageRequired(forwarded);
            context.AuthenticatedIdentity = result.AuthenticatedIdentity;
            branches.Add(context);
            if (!statefulBranch)
            {
                QueueEvent(SipProxyEventKind.RequestAccepted,
                    request, result, null, forwarded,
                    "The stateless proxy request was accepted.");
                QueueEvent(SipProxyEventKind.BranchCreated,
                    request, result, selected, forwarded,
                    "A stateless proxy branch was created.");
            }
            return CopyResult(result);
        }
    }

    SipProxyResult ProcessStatefulRequest(SipProxyRequest request)
    {
        SipProxyOptions options = request.Options;
        SipProxyTarget[] sourceTargets = request.Targets == null
            ? new SipProxyTarget[0] : request.Targets;
        int validationIndex = 0;
        while (validationIndex < sourceTargets.Length)
        {
            if (!ValidTarget(sourceTargets[validationIndex]))
            {
                return RequestFailure(request, request.Message,
                    SipStatusCodes.BadRequest,
                    "A stateful proxy target is invalid.");
            }
            validationIndex = validationIndex + 1;
        }
        SipProxyTarget[] orderedTargets =
            CreateOrderedTargets(sourceTargets);
        if (orderedTargets.Length == 0)
        {
            orderedTargets = new SipProxyTarget[] {
                new SipProxyTarget() };
        }
        if (!ValidProxyOptions(options)
            || orderedTargets.Length > options.MaxBranchCount)
        {
            return RequestFailure(request, request.Message,
                SipStatusCodes.BadRequest,
                "The stateful proxy options or branch count are invalid.");
        }

        SipProxyOperationContext operation = new SipProxyOperationContext();
        operation.Id = tokens.CreateOpaqueToken();
        operation.Request = CopyRequest(request);
        operation.Mode = options.Mode;
        int targetIndex = 0;
        while (targetIndex < orderedTargets.Length)
        {
            operation.Targets.Add(
                CopyProxyTarget(orderedTargets[targetIndex]));
            targetIndex = targetIndex + 1;
        }
        operations.Add(operation);
        int initialBranchCount = branches.Count;
        SipProxyResult result = StartTargetGroup(operation);
        if (!result.Success || result.Request == null)
        {
            while (branches.Count > initialBranchCount)
            {
                branches.RemoveAt(branches.Count - 1);
            }
            RemoveOperation(operation.Id);
            return result;
        }
        QueueEvent(SipProxyEventKind.RequestAccepted,
            request, result, null, result.Request,
            "The stateful proxy request was accepted.");
        QueueCreatedBranchEvents(operation,
            initialBranchCount, result);
        return CopyResult(result);
    }

    SipProxyResult StartTargetGroup(SipProxyOperationContext operation)
    {
        SipProxyRequest? operationRequest = operation.Request;
        if (operationRequest == null
            || operation.NextTargetIndex >= operation.Targets.Count)
        {
            return RequestFailure(operationRequest, null,
                SipStatusCodes.ServerInternalError,
                "The proxy operation has no pending target.");
        }
        int groupCount = 1;
        int firstIndex = operation.NextTargetIndex;
        int quality = operation.Targets[firstIndex].QualityThousandths;
        if (operation.Mode == SipProxyMode.StatefulParallel)
        {
            int scan = firstIndex + 1;
            while (scan < operation.Targets.Count
                && operation.Targets[scan].QualityThousandths == quality)
            {
                groupCount = groupCount + 1;
                scan = scan + 1;
            }
        }
        SipMessage[] requests = new SipMessage[groupCount];
        SipSendRequest[] sends = new SipSendRequest[groupCount];
        int initialBranchCount = branches.Count;
        int initialNextIndex = operation.NextTargetIndex;
        int initialCreatedCount = operation.CreatedBranchCount;
        int index = 0;
        while (index < groupCount)
        {
            int targetIndex = operation.NextTargetIndex;
            SipProxyRequest? singleValue = CopyRequest(operationRequest);
            SipProxyRequest single = singleValue == null
                ? new SipProxyRequest() : singleValue;
            single.Options.Mode = SipProxyMode.Stateless;
            single.Targets = new SipProxyTarget[] {
                CopyProxyTarget(operation.Targets[targetIndex]) };
            SipProxyResult branchResult =
                ProcessSingleRequest(single, true);
            if (!branchResult.Success || branchResult.Request == null
                || branchResult.SendRequest == null
                || branchResult.Targets.Length == 0)
            {
                while (branches.Count > initialBranchCount)
                {
                    branches.RemoveAt(branches.Count - 1);
                }
                operation.NextTargetIndex = initialNextIndex;
                operation.CreatedBranchCount = initialCreatedCount;
                return branchResult;
            }
            operation.Targets[targetIndex] =
                CopyProxyTarget(branchResult.Targets[0]);
            operation.NextTargetIndex = operation.NextTargetIndex + 1;
            operation.CreatedBranchCount = operation.CreatedBranchCount + 1;
            SipProxyBranchContext context =
                branches[branches.Count - 1];
            context.Stateful = true;
            context.OperationId = operation.Id;
            bool invite = branchResult.Request.Method == SipMethod.Invite;
            context.TimerCIntervalMilliseconds =
                operationRequest.Options.TimerCMilliseconds;
            context.TimerCDeadlineMilliseconds = invite
                ? AddTime(nowMilliseconds,
                    operationRequest.Options.TimerCMilliseconds) : -1;
            requests[index] = CopyMessageRequired(branchResult.Request);
            sends[index] = CopySend(branchResult.SendRequest);
            index = index + 1;
        }
        SipProxyResult result = new SipProxyResult();
        result.Success = true;
        result.Request = CopyMessageRequired(requests[0]);
        result.Requests = CopyMessages(requests);
        result.SendRequest = CopySend(sends[0]);
        result.SendRequests = CopySends(sends);
        result.CorrelationId = operationRequest.CorrelationId;
        result.AuthenticatedIdentity =
            operationRequest.Authentication == null
            ? null : operationRequest.Authentication.Identity;
        PopulateOperationState(result, operation.Id);
        return result;
    }

    void QueueCreatedBranchEvents(SipProxyOperationContext operation,
        int initialBranchCount, SipProxyResult result)
    {
        int index = initialBranchCount;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.OperationId == operation.Id)
            {
                QueueEvent(SipProxyEventKind.BranchCreated,
                    operation.Request, result, context.Target,
                    context.OutboundRequest,
                    "A stateful proxy branch was created.");
            }
            index = index + 1;
        }
    }

    SipProxyOperationContext? FindOperation(string? operationId)
    {
        int index = 0;
        while (index < operations.Count)
        {
            if (operations[index].Id == operationId)
            {
                return operations[index];
            }
            index = index + 1;
        }
        return null;
    }

    void RemoveOperation(string? operationId)
    {
        int index = 0;
        while (index < operations.Count)
        {
            if (operations[index].Id == operationId)
            {
                operations.RemoveAt(index);
                return;
            }
            index = index + 1;
        }
    }

    internal SipProxyResult ProcessResponse(SipMessage? response)
    {
        lock (sync)
        {
            SipStatelessResult validated =
                SipStatelessTransaction.SendResponse(response);
            if (!validated.Success || response == null)
            {
                return ResponseFailure(SipStatusCodes.BadRequest,
                    validated.ErrorMessage == null
                        ? "A valid proxy response is required."
                        : validated.ErrorMessage);
            }
            int contextIndex = FindResponseContext(response);
            if (contextIndex < 0)
            {
                SipProxyBranchContext? late = FindLateResponseContext(response);
                if (late != null)
                {
                    if (response.StatusCode >= 200
                        && response.StatusCode < 300)
                    {
                        return ProcessAdditionalSuccessResponse(
                            response, late);
                    }
                    return LateResponseResult(late, response, true);
                }
                return ResponseFailure(
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "The response top Via does not identify a proxy branch.");
            }
            SipProxyBranchContext context = branches[contextIndex];
            if (context.Stateful)
            {
                return ProcessStatefulResponse(response, context);
            }
            SipStatelessResult forwardedResult =
                SipStatelessTransaction.ForwardResponse(response,
                    context.SentByHost, context.SentByPort,
                    context.Branch);
            if (!forwardedResult.Success)
            {
                return ResponseFailure(SipStatusCodes.BadRequest,
                    forwardedResult.ErrorMessage == null
                        ? "The proxy response cannot be forwarded."
                        : forwardedResult.ErrorMessage);
            }
            SipMessage forwarded = forwardedResult.Forwarded
                ? forwardedResult.OutboundMessages[0]
                : forwardedResult.DeliveredMessages[0];
            SipProxyResult result = new SipProxyResult();
            result.Success = true;
            result.Response = CopyMessageRequired(forwarded);
            result.StatusCode = forwarded.StatusCode;
            result.CorrelationId = context.CorrelationId;
            if (context.Target != null)
            {
                result.Targets = new SipProxyTarget[] {
                    CopyProxyTarget(context.Target) };
            }
            if (forwardedResult.Forwarded
                && context.ReceivedRequest != null)
            {
                SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                    forwarded, context.ReceivedRequest);
                send.CorrelationId = context.CorrelationId;
                result.SendRequest = CopySend(send);
            }
            SipProxyEventKind kind = forwarded.StatusCode < 200
                ? SipProxyEventKind.ProvisionalForwarded
                : SipProxyEventKind.FinalSelected;
            QueueEvent(kind, null, result, context.Target,
                forwarded, forwarded.StatusCode < 200
                    ? "A provisional stateless response was forwarded."
                    : "A final stateless response was forwarded.");
            if (forwarded.StatusCode >= 200)
            {
                branches.RemoveAt(contextIndex);
            }
            return CopyResult(result);
        }
    }

    SipProxyResult ProcessStatefulResponse(SipMessage response,
        SipProxyBranchContext context)
    {
        if (context.Completed)
        {
            return LateResponseResult(context, response, false);
        }
        SipStatelessResult forwardedResult =
            SipStatelessTransaction.ForwardResponse(response,
                context.SentByHost, context.SentByPort, context.Branch);
        if (!forwardedResult.Success)
        {
            return ResponseFailure(SipStatusCodes.BadRequest,
                forwardedResult.ErrorMessage == null
                    ? "The stateful proxy response cannot be forwarded."
                    : forwardedResult.ErrorMessage);
        }
        SipMessage forwarded = forwardedResult.Forwarded
            ? forwardedResult.OutboundMessages[0]
            : forwardedResult.DeliveredMessages[0];
        SipProxyResult result = CreateBranchResult(context);
        result.StatusCode = forwarded.StatusCode;
        if (forwarded.StatusCode < 200)
        {
            context.ProvisionalReceived = true;
            result.Response = CopyMessageRequired(forwarded);
            if (forwardedResult.Forwarded
                && context.ReceivedRequest != null)
            {
                SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                    forwarded, context.ReceivedRequest);
                send.CorrelationId = context.CorrelationId;
                result.SendRequest = CopySend(send);
            }
            if (forwarded.StatusCode > 100
                && context.TimerCDeadlineMilliseconds >= 0)
            {
                context.TimerCDeadlineMilliseconds =
                    AddTime(nowMilliseconds,
                        context.TimerCIntervalMilliseconds);
            }
            PopulateOperationState(result, context.OperationId);
            QueueEvent(SipProxyEventKind.ProvisionalForwarded,
                null, result, context.Target, forwarded,
                "A provisional stateful response was forwarded.");
            if (context.CancellationPending && !context.CancelSent)
            {
                AddBranchCancel(result, context);
            }
            return CopyResult(result);
        }

        context.Completed = true;
        context.FinalResponse = CopyMessageRequired(forwarded);
        context.TimerCDeadlineMilliseconds = -1;
        if (forwarded.StatusCode >= 300 && forwarded.StatusCode < 400)
        {
            AddRedirectTargets(context, forwarded);
        }
        if (forwarded.StatusCode >= 600
            || forwarded.StatusCode >= 200 && forwarded.StatusCode < 300)
        {
            SipProxyResult completed = CompleteOperation(
                context.OperationId, context,
                forwarded.StatusCode >= 600
                    ? "A global failure response completed the proxy operation."
                    : "A successful response completed the proxy operation.");
            if (forwarded.StatusCode >= 300)
            {
                AddFailureAck(completed, context);
            }
            return CopyResult(completed);
        }
        if (AllOperationBranchesCompleted(context.OperationId))
        {
            SipProxyOperationContext? operation =
                FindOperation(context.OperationId);
            if (operation != null
                && operation.NextTargetIndex < operation.Targets.Count)
            {
                int initialBranchCount = branches.Count;
                SipProxyResult next = StartTargetGroup(operation);
                if (next.Success)
                {
                    QueueCreatedBranchEvents(operation,
                        initialBranchCount, next);
                    AddFailureAck(next, context);
                    return CopyResult(next);
                }
                operation.NextTargetIndex = operation.Targets.Count;
                SipProxyBranchContext failedSelected =
                    SelectBestResponse(context.OperationId);
                SipProxyResult failedCompletion = CompleteOperation(
                    context.OperationId,
                    failedSelected,
                    "No remaining proxy target could be started.");
                AddFailureAck(failedCompletion, context);
                return CopyResult(failedCompletion);
            }
            SipProxyBranchContext selected =
                SelectBestResponse(context.OperationId);
            SipProxyResult completion = CompleteOperation(
                context.OperationId, selected,
                "The best collected final response was selected.");
            AddFailureAck(completion, context);
            return CopyResult(completion);
        }
        PopulateOperationState(result, context.OperationId);
        AddFailureAck(result, context);
        return CopyResult(result);
    }

    SipProxyResult ProcessCancelRequest(SipProxyRequest request,
        SipMessage cancel)
    {
        if (request.TransportMessage == null
            || request.TransportMessage.LocalAddress == null
            || cancel.CSeq == null
            || cancel.CSeq.Method != SipMethod.Cancel
            || cancel.ViaValues.Length == 0)
        {
            return RequestFailure(request, cancel,
                SipStatusCodes.BadRequest,
                "A complete received CANCEL request is required.");
        }
        SipProxyOperationContext? operation = FindCancelOperation(cancel);
        SipProxyBranchContext? completedContext = operation == null
            ? FindCompletedCancelContext(cancel) : null;
        int status = operation == null && completedContext == null
            ? SipStatusCodes.CallOrTransactionDoesNotExist
            : SipStatusCodes.Ok;
        string phrase = status == SipStatusCodes.Ok
            ? SipReasonPhrases.Ok
            : SipReasonPhrases.CallOrTransactionDoesNotExist;
        SipMessage response =
            SipTransactionMessageFactory.CreateRequestResponse(
                cancel, status, phrase, tokens.CreateTag());
        SipProxyResult result = new SipProxyResult();
        result.Success = true;
        result.Response = CopyMessageRequired(response);
        result.StatusCode = status;
        result.CorrelationId = operation != null
            && operation.Request != null
            ? operation.Request.CorrelationId : request.CorrelationId;
        SipSendRequest upstream = SipViaRouting.CreateResponseSendRequest(
            response, request.TransportMessage);
        upstream.CorrelationId = result.CorrelationId;
        result.SendRequest = CopySend(upstream);
        if (operation == null)
        {
            result.Completed = true;
            QueueEvent(SipProxyEventKind.RequestAccepted,
                request, result, completedContext == null
                    ? null : completedContext.Target,
                response, status == SipStatusCodes.Ok
                    ? "A late matching CANCEL was acknowledged."
                    : "An unmatched CANCEL was rejected.");
            return CopyResult(result);
        }
        result.Targets = CopyOperationTargets(operation.Id);
        bool firstCancellation = !operation.CancelRequested;
        operation.CancelRequested = true;
        operation.NextTargetIndex = operation.Targets.Count;
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.OperationId == operation.Id && !context.Completed)
            {
                context.CancellationPending = true;
                if (firstCancellation && context.ProvisionalReceived
                    && !context.CancelSent)
                {
                    AddBranchCancel(result, context);
                }
            }
            index = index + 1;
        }
        PopulateOperationState(result, operation.Id);
        QueueEvent(SipProxyEventKind.RequestAccepted,
            request, result, null, response,
            firstCancellation
                ? "The matching CANCEL was accepted."
                : "The duplicate matching CANCEL was acknowledged.");
        return CopyResult(result);
    }

    SipProxyOperationContext? FindCancelOperation(SipMessage cancel)
    {
        int index = 0;
        while (index < operations.Count)
        {
            SipProxyOperationContext operation = operations[index];
            SipMessage? invite = operation.Request == null
                ? null : operation.Request.Message;
            if (invite == null && operation.Request != null
                && operation.Request.TransportMessage != null)
            {
                invite = operation.Request.TransportMessage.Message;
            }
            if (invite != null && invite.Method == SipMethod.Invite)
            {
                SipTransactionKey key = SipTransactionMatching.CreateKey(
                    invite, SipTransactionRole.Server);
                if (SipTransactionMatching.MatchCancel(key, cancel, true))
                {
                    return operation;
                }
            }
            index = index + 1;
        }
        return null;
    }

    SipProxyBranchContext? FindCompletedCancelContext(SipMessage cancel)
    {
        int index = 0;
        while (index < lateBranches.Count)
        {
            SipProxyBranchContext context = lateBranches[index];
            SipMessage? invite = context.ReceivedRequest == null
                ? null : context.ReceivedRequest.Message;
            if (invite != null && invite.Method == SipMethod.Invite)
            {
                SipTransactionKey key = SipTransactionMatching.CreateKey(
                    invite, SipTransactionRole.Server);
                if (SipTransactionMatching.MatchCancel(key, cancel, true))
                {
                    return context;
                }
            }
            index = index + 1;
        }
        return null;
    }

    SipProxyBranchContext? FindFailureAckContext(SipMessage ack)
    {
        int index = 0;
        while (index < lateBranches.Count)
        {
            SipProxyBranchContext context = lateBranches[index];
            SipMessage? invite = context.ReceivedRequest == null
                ? null : context.ReceivedRequest.Message;
            if (context.SelectedFinal && context.FinalResponse != null
                && context.FinalResponse.StatusCode >= 300
                && invite != null && invite.Method == SipMethod.Invite)
            {
                SipTransactionKey key = SipTransactionMatching.CreateKey(
                    invite, SipTransactionRole.Server);
                if (SipTransactionMatching.MatchRequest(
                        key, ack, null, true))
                {
                    return context;
                }
            }
            index = index + 1;
        }
        return null;
    }

    SipProxyResult ProcessFailureAck(SipProxyRequest request,
        SipMessage ack, SipProxyBranchContext context)
    {
        SipProxyResult result = CreateBranchResult(context);
        result.Completed = true;
        result.CorrelationId = context.CorrelationId;
        result.AuthenticatedIdentity = context.AuthenticatedIdentity;
        QueueEvent(SipProxyEventKind.AckAbsorbed,
            request, result, context.Target, ack,
            "A failure-response ACK was absorbed by the proxy transaction.");
        return CopyResult(result);
    }

    SipProxyResult ProcessAdditionalSuccessResponse(
        SipMessage response, SipProxyBranchContext context)
    {
        SipStatelessResult forwardedResult =
            SipStatelessTransaction.ForwardResponse(response,
                context.SentByHost, context.SentByPort, context.Branch);
        if (!forwardedResult.Success)
        {
            return ResponseFailure(SipStatusCodes.BadRequest,
                "The additional successful response cannot be forwarded.");
        }
        SipMessage forwarded = forwardedResult.Forwarded
            ? forwardedResult.OutboundMessages[0]
            : forwardedResult.DeliveredMessages[0];
        context.FinalResponse = CopyMessageRequired(forwarded);
        SipProxyResult result = CreateBranchResult(context);
        result.Response = CopyMessageRequired(forwarded);
        result.StatusCode = forwarded.StatusCode;
        result.Completed = true;
        if (context.ReceivedRequest != null)
        {
            SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                forwarded, context.ReceivedRequest);
            send.CorrelationId = context.CorrelationId;
            result.SendRequest = CopySend(send);
        }
        QueueEvent(SipProxyEventKind.AdditionalSuccessForwarded,
            null, result, context.Target, forwarded,
            "An additional successful branch response was forwarded.");
        return CopyResult(result);
    }

    void AddBranchCancel(SipProxyResult result,
        SipProxyBranchContext context)
    {
        SipMessage cancel =
            SipTransactionMessageFactory.CreateCancel(
                context.OutboundRequest);
        SipSendRequest send = CreateBranchControlSend(cancel, context);
        result.Requests = AppendMessage(result.Requests, cancel);
        result.SendRequests = AppendSend(result.SendRequests, send);
        if (result.Request == null)
        {
            result.Request = CopyMessageRequired(cancel);
        }
        context.CancelSent = true;
        context.CancellationPending = false;
        QueueEvent(SipProxyEventKind.CancelForwarded,
            null, result, context.Target, cancel,
            "A CANCEL request was forwarded to a stateful branch.");
    }

    void AddFailureAck(SipProxyResult result,
        SipProxyBranchContext context)
    {
        if (context.OutboundRequest == null
            || context.FinalResponse == null)
        {
            return;
        }
        SipMessage ack = SipTransactionMessageFactory.CreateFailureAck(
            context.OutboundRequest, context.FinalResponse);
        SipSendRequest send = CreateBranchControlSend(ack, context);
        result.Requests = AppendMessage(result.Requests, ack);
        result.SendRequests = AppendSend(result.SendRequests, send);
        if (result.Request == null)
        {
            result.Request = CopyMessageRequired(ack);
        }
    }

    static SipSendRequest CreateBranchControlSend(SipMessage message,
        SipProxyBranchContext context)
    {
        SipSendRequest send = new SipSendRequest();
        send.Message = CopyMessageRequired(message);
        send.CorrelationId = context.CorrelationId;
        SipProxyTarget? target = context.Target;
        if (target != null && target.Candidate != null)
        {
            SipServerCandidate candidate = target.Candidate;
            SipEndpointAddress remote = new SipEndpointAddress();
            remote.Host = candidate.Host;
            remote.Port = candidate.Port;
            remote.Transport = candidate.Transport;
            remote.Secure = candidate.Secure;
            send.RemoteAddress = remote;
            send.Transport = candidate.Transport;
            if (target.Flow != null)
            {
                send.FlowId = target.Flow.Id;
                if (target.Flow.Connection != null)
                {
                    send.ConnectionId = target.Flow.Connection.Id;
                }
            }
        }
        if (context.ReceivedRequest != null
            && context.ReceivedRequest.LocalAddress != null)
        {
            send.LocalAddress =
                CopyEndpoint(context.ReceivedRequest.LocalAddress);
            send.LocalAddress.Transport = send.Transport;
            send.LocalAddress.Secure = IsSecureTransport(send.Transport);
        }
        return send;
    }

    SipProxyResult LateResponseResult(SipProxyBranchContext context,
        SipMessage response, bool operationCompleted)
    {
        SipProxyResult result = CreateBranchResult(context);
        result.StatusCode = response.StatusCode;
        result.Completed = operationCompleted;
        if (!operationCompleted)
        {
            PopulateOperationState(result, context.OperationId);
        }
        QueueEvent(SipProxyEventKind.LateResponseIgnored,
            null, result, context.Target, response,
            "A late stateful branch response was ignored.");
        return CopyResult(result);
    }

    SipProxyResult CompleteOperation(string? operationId,
        SipProxyBranchContext selected, string reason)
    {
        selected.SelectedFinal = true;
        SipProxyResult result = CreateBranchResult(selected);
        SipMessage response = selected.FinalResponse == null
            ? new SipMessage()
            : CopyMessageRequired(selected.FinalResponse);
        result.Response = response;
        result.StatusCode = response.StatusCode;
        result.Targets = CopyOperationTargets(operationId);
        result.Completed = true;
        result.TimedOut = selected.TimedOut;
        result.ActiveBranchCount = 0;
        result.TimerCDeadlines = new long[0];
        if (selected.ReceivedRequest != null)
        {
            SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                response, selected.ReceivedRequest);
            send.CorrelationId = selected.CorrelationId;
            result.SendRequest = CopySend(send);
        }
        if (response.StatusCode >= 200
            && (response.StatusCode < 300 || response.StatusCode >= 600))
        {
            int branchIndex = 0;
            while (branchIndex < branches.Count)
            {
                SipProxyBranchContext branch = branches[branchIndex];
                if (branch.OperationId == operationId
                    && !object.ReferenceEquals(branch, selected)
                    && !branch.Completed && branch.ProvisionalReceived
                    && !branch.CancelSent)
                {
                    AddBranchCancel(result, branch);
                }
                branchIndex = branchIndex + 1;
            }
        }
        QueueEvent(SipProxyEventKind.FinalSelected,
            null, result, selected.Target, response, reason);
        QueueEvent(SipProxyEventKind.Completed,
            null, result, selected.Target, response,
            "Stateful proxy processing completed.");
        RememberAndRemoveOperation(operationId);
        return CopyResult(result);
    }

    internal void AdvanceTime(long value)
    {
        lock (sync)
        {
            if (value < nowMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException(
                    "nowMilliseconds");
            }
            nowMilliseconds = value;
            int index = 0;
            while (index < branches.Count)
            {
                SipProxyBranchContext context = branches[index];
                if (!context.Stateful || context.Completed
                    || context.TimerCDeadlineMilliseconds < 0
                    || context.TimerCDeadlineMilliseconds > nowMilliseconds)
                {
                    index = index + 1;
                    continue;
                }
                SipMessage timeout =
                    SipTransactionMessageFactory.CreateRequestResponse(
                        context.OutboundRequest,
                        SipStatusCodes.RequestTimeout,
                        SipReasonPhrases.RequestTimeout,
                        tokens.CreateTag());
                SipStatelessResult forwardedResult =
                    SipStatelessTransaction.ForwardResponse(timeout,
                        context.SentByHost, context.SentByPort,
                        context.Branch);
                SipMessage forwarded = forwardedResult.Forwarded
                    ? forwardedResult.OutboundMessages[0]
                    : forwardedResult.DeliveredMessages[0];
                context.FinalResponse = CopyMessageRequired(forwarded);
                context.Completed = true;
                context.TimedOut = true;
                context.TimerCDeadlineMilliseconds = -1;
                SipProxyResult timeoutResult = CreateBranchResult(context);
                timeoutResult.StatusCode = SipStatusCodes.RequestTimeout;
                timeoutResult.TimedOut = true;
                PopulateOperationState(timeoutResult, context.OperationId);
                QueueEvent(SipProxyEventKind.BranchTimedOut,
                    null, timeoutResult, context.Target, forwarded,
                    "Timer C expired for a stateful proxy branch.");
                if (AllOperationBranchesCompleted(context.OperationId))
                {
                    SipProxyOperationContext? operation =
                        FindOperation(context.OperationId);
                    if (operation != null
                        && operation.NextTargetIndex
                            < operation.Targets.Count)
                    {
                        int initialBranchCount = branches.Count;
                        SipProxyResult next = StartTargetGroup(operation);
                        if (next.Success)
                        {
                            QueueCreatedBranchEvents(operation,
                                initialBranchCount, next);
                        }
                        else
                        {
                            operation.NextTargetIndex =
                                operation.Targets.Count;
                            SipProxyBranchContext failedSelected =
                                SelectBestResponse(context.OperationId);
                            CompleteOperation(context.OperationId,
                                failedSelected,
                                "No remaining proxy target could be started.");
                        }
                    }
                    else
                    {
                        SipProxyBranchContext selected =
                            SelectBestResponse(context.OperationId);
                        CompleteOperation(context.OperationId, selected,
                            "The stateful proxy operation timed out.");
                    }
                    index = 0;
                }
                else
                {
                    index = index + 1;
                }
            }
        }
    }

    internal void CancelBranches(SipCancellation? cancellation)
    {
        if (cancellation == null)
        {
            throw new System.ArgumentNullException("cancellation");
        }
        lock (sync)
        {
            if (!cancellation.Requested)
            {
                return;
            }
            int operationIndex = 0;
            while (operationIndex < operations.Count)
            {
                SipProxyOperationContext operation =
                    operations[operationIndex];
                if (!CancellationMatches(operation, cancellation)
                    || operation.CancelRequested)
                {
                    operationIndex = operationIndex + 1;
                    continue;
                }
                operation.CancelRequested = true;
                operation.NextTargetIndex = operation.Targets.Count;
                SipProxyResult result = new SipProxyResult();
                result.Success = true;
                result.CorrelationId = operation.Request == null
                    ? null : operation.Request.CorrelationId;
                int branchIndex = 0;
                while (branchIndex < branches.Count)
                {
                    SipProxyBranchContext context = branches[branchIndex];
                    if (context.OperationId == operation.Id
                        && !context.Completed)
                    {
                        context.CancellationPending = true;
                        if (context.ProvisionalReceived && !context.CancelSent)
                        {
                            AddBranchCancel(result, context);
                        }
                    }
                    branchIndex = branchIndex + 1;
                }
                PopulateOperationState(result, operation.Id);
                QueueEvent(SipProxyEventKind.RequestAccepted,
                    operation.Request, result, null, result.Request,
                    cancellation.Reason == null
                        ? "Proxy branch cancellation was requested."
                        : cancellation.Reason);
                operationIndex = operationIndex + 1;
            }
        }
    }

    static bool CancellationMatches(SipProxyOperationContext operation,
        SipCancellation cancellation)
    {
        if (cancellation.Id == null || cancellation.Id.Length == 0)
        {
            return true;
        }
        if (operation.Request == null)
        {
            return false;
        }
        if (operation.Request.CorrelationId == cancellation.Id)
        {
            return true;
        }
        return operation.Request.Cancellation != null
            && operation.Request.Cancellation.Id == cancellation.Id;
    }

    internal SipProxyEvent PollEvent()
    {
        lock (sync)
        {
            if (events.Count == 0)
            {
                return new SipProxyEvent();
            }
            SipProxyEvent value = events[0];
            events.RemoveAt(0);
            return CopyEvent(value);
        }
    }

    SipProxyResult? ApplyAdmissionSafety(SipProxyRequest request,
        SipMessage? message)
    {
        if (message != null && message.Method == SipMethod.Ack)
        {
            return null;
        }
        if (!ValidAdmissionMessage(request, message))
        {
            return RequestFailure(request, message,
                SipStatusCodes.BadRequest,
                "The proxy request is malformed.");
        }
        if (message == null)
        {
            return RequestFailure(request, null,
                SipStatusCodes.BadRequest,
                "The proxy request is malformed.");
        }
        if (ContainsProxyBranch(message))
        {
            return CreatePolicyResponse(request, message,
                SipStatusCodes.LoopDetected,
                SipReasonPhrases.LoopDetected, false);
        }
        int index = 0;
        bool comparable = CanFingerprintMessage(message);
        while (comparable && index < branches.Count)
        {
            SipMessage? existing = ReadReceivedRequest(branches[index]);
            if (existing != null && CanFingerprintMessage(existing))
            {
                bool sameTarget = existing.RequestUri != null
                    && message.RequestUri != null
                    && SipUriComparison.CompareUriModels(
                        existing.RequestUri, message.RequestUri);
                if (SipRequestRouting.RequestsFormLoop(existing, message))
                {
                    return CreatePolicyResponse(request, message,
                        SipStatusCodes.LoopDetected,
                        SipReasonPhrases.LoopDetected, false);
                }
                if (SipRequestRouting.IsMergedRequest(existing, message)
                    && (!SipRequestRouting.RequestsFormSpiral(
                        existing, message) || sameTarget))
                {
                    return CreatePolicyResponse(request, message,
                        SipStatusCodes.LoopDetected,
                        SipReasonPhrases.LoopDetected, false);
                }
            }
            index = index + 1;
        }
        if (LiveTransactionCount() >= safetyOptions.MaxTransactionCount
            || RetainedBranchCount() >= safetyOptions.MaxBranchCount
            || !HasEventCapacity(2))
        {
            return CreatePolicyResponse(request, message,
                SipStatusCodes.ServiceUnavailable,
                SipReasonPhrases.ServiceUnavailable, true);
        }
        TrimAdmissionTimes();
        if (admissionTimes.Count >= safetyOptions.MaxRequestCount)
        {
            return CreatePolicyResponse(request, message,
                SipStatusCodes.ServiceUnavailable,
                SipReasonPhrases.ServiceUnavailable, true);
        }
        admissionTimes.Add(nowMilliseconds);
        return null;
    }

    static bool ValidAdmissionMessage(SipProxyRequest request,
        SipMessage? message)
    {
        if (message == null || message.Kind != SipMessageKind.Request
            || message.RequestUri == null || message.Headers == null
            || message.ViaValues == null || message.ViaValues.Length == 0
            || message.From == null || message.To == null
            || message.CSeq == null || message.Body == null
            || message.OriginalBytes == null
            || request.TransportMessage == null
            || request.TransportMessage.LocalAddress == null
            || !ValidEndpoint(request.TransportMessage.LocalAddress))
        {
            return false;
        }
        if (request.TransportMessage != null
            && request.TransportMessage.ParseResult != null
            && request.TransportMessage.ParseResult.Status
                != SipParseStatus.Complete)
        {
            return false;
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(message, true);
        int index = 0;
        while (index < issues.Length)
        {
            SipValidationIssue issue = issues[index];
            bool permittedPolicyIssue = issue.Code == "header-missing"
                && SipAscii.EqualsIgnoreCase(
                    issue.HeaderName, SipHeaderNames.MaxForwards)
                || issue.Code == "sips-contact";
            if (issue.Severity == SipValidationSeverity.Error
                && !permittedPolicyIssue)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool CanFingerprintMessage(SipMessage message)
    {
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(message, true);
        return !SipMessageValidator.HasErrorIssues(issues);
    }

    bool ContainsProxyBranch(SipMessage message)
    {
        int viaIndex = 0;
        while (viaIndex < message.ViaValues.Length)
        {
            string? branch = message.ViaValues[viaIndex].Branch;
            int contextIndex = 0;
            while (contextIndex < branches.Count)
            {
                if (branch != null
                    && branch == branches[contextIndex].Branch)
                {
                    return true;
                }
                contextIndex = contextIndex + 1;
            }
            viaIndex = viaIndex + 1;
        }
        return false;
    }

    static SipMessage? ReadReceivedRequest(SipProxyBranchContext context)
    {
        return context.ReceivedRequest == null
            ? null : context.ReceivedRequest.Message;
    }

    int LiveTransactionCount()
    {
        int count = operations.Count;
        int index = 0;
        while (index < branches.Count)
        {
            if (!branches[index].Stateful && !branches[index].Completed)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    int RetainedBranchCount()
    {
        return branches.Count;
    }

    bool HasEventCapacity(int requiredCount)
    {
        return requiredCount >= 0
            && events.Count <= safetyOptions.MaxEventCount - requiredCount;
    }

    void TrimAdmissionTimes()
    {
        long cutoff = nowMilliseconds
            - safetyOptions.RequestWindowMilliseconds;
        int index = 0;
        while (index < admissionTimes.Count)
        {
            if (admissionTimes[index] > cutoff)
            {
                break;
            }
            admissionTimes.RemoveAt(index);
        }
    }

    SipProxyResult CreatePolicyResponse(SipProxyRequest request,
        SipMessage message, int statusCode, string reason, bool retryAfter)
    {
        string safeReason = SanitizeText(reason,
            SipReasonPhrases.ServiceUnavailable);
        SipMessage response =
            SipTransactionMessageFactory.CreateRequestResponse(
                message, statusCode, safeReason, tokens.CreateTag());
        if (retryAfter)
        {
            AppendHeader(response, SipHeaderNames.RetryAfter,
                safetyOptions.RetryAfterSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }
        SipProxyResult result = new SipProxyResult();
        result.Success = true;
        result.Response = CopyMessageRequired(response);
        result.StatusCode = statusCode;
        result.ErrorMessage = safeReason;
        result.CorrelationId = request.CorrelationId;
        result.Completed = true;
        if (request.TransportMessage != null)
        {
            SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                response, request.TransportMessage);
            send.CorrelationId = request.CorrelationId;
            result.SendRequest = CopySend(send);
        }
        QueueEvent(SipProxyEventKind.Failed,
            request, result, null, response, safeReason);
        return CopyResult(result);
    }

    SipProxyResult? ApplyProxySecurity(SipProxyRequest request)
    {
        if (authenticationServer == null || authorizationPolicy == null
            || nonceOptions == null)
        {
            return null;
        }
        SipMessage? message = request.Message;
        if (message == null && request.TransportMessage != null)
        {
            message = request.TransportMessage.Message;
        }
        if (message == null)
        {
            return RequestFailure(request, null,
                SipStatusCodes.BadRequest,
                "A message is required for proxy authentication.");
        }
        if (message.Method == SipMethod.Ack)
        {
            return null;
        }
        SipAuthenticationRequest authenticationRequest =
            new SipAuthenticationRequest();
        authenticationRequest.Message =
            SipSnapshotCopy.CopyMessageModel(message);
        authenticationRequest.Target = message.RequestUri == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(message.RequestUri);
        authenticationRequest.Body = CopyBytes(message.Body);
        authenticationRequest.Scope = SipCredentialScope.Proxy;
        authenticationRequest.CorrelationId = request.CorrelationId;
        SipAuthenticationResult authentication;
        if (request.Authentication != null)
        {
            authentication = CopyAuthenticationResultRequired(
                request.Authentication);
        }
        else if (TrustedPeerAvailable(request.TransportMessage))
        {
            authentication = new SipAuthenticationResult();
            authentication.Status = SipAuthenticationStatus.Success;
            authentication.Identity = request.TransportMessage == null
                || request.TransportMessage.Connection == null
                ? null : request.TransportMessage.Connection.PeerIdentity;
            authentication.CertificateIdentity =
                request.TransportMessage == null
                || request.TransportMessage.Connection == null
                ? null : SipCertificateIdentityCopy.Copy(
                    request.TransportMessage.Connection.CertificateIdentity);
            authentication.CorrelationId = request.CorrelationId;
            authentication.Message =
                SipSnapshotCopy.CopyMessageModel(message);
        }
        else
        {
            authentication = authenticationServer.VerifyAuthentication(
                authenticationRequest);
        }
        if (authentication == null
            || authentication.Status != SipAuthenticationStatus.Success)
        {
            SipAuthenticationResult challenged = authentication != null
                && authentication.Message != null
                ? authentication
                : authenticationServer.CreateAuthenticationChallenge(
                    authenticationRequest, nonceOptions);
            return CreateSecurityResponse(request, challenged,
                "Proxy authentication is required.");
        }
        SipAuthorizationDecision decision =
            authenticationServer.AuthorizeAuthenticatedOperation(
                authenticationRequest, authentication,
                authorizationPolicy);
        if (decision == null || !decision.Allowed)
        {
            int status = decision == null || decision.StatusCode < 300
                || decision.StatusCode > 699
                ? SipStatusCodes.Forbidden : decision.StatusCode;
            string reason = SanitizeText(
                decision == null ? null : decision.Reason,
                SipReasonPhrases.Forbidden);
            SipMessage denied =
                SipTransactionMessageFactory.CreateRequestResponse(
                    message, status, reason, tokens.CreateTag());
            SipAuthenticationResult deniedResult =
                new SipAuthenticationResult();
            deniedResult.Status = SipAuthenticationStatus.InvalidCredentials;
            deniedResult.Message = denied;
            deniedResult.ErrorMessage = reason;
            deniedResult.CorrelationId = request.CorrelationId;
            return CreateSecurityResponse(request, deniedResult, reason);
        }
        authentication.Identity = decision.Identity == null
            || decision.Identity.Length == 0
            ? authentication.Identity : decision.Identity;
        request.Authentication =
            CopyAuthenticationResult(authentication);
        return null;
    }

    static bool TrustedPeerAvailable(SipTransportMessage? transport)
    {
        return transport != null && transport.Connection != null
            && transport.Connection.Secure
            && transport.Connection.State == SipConnectionState.Connected
            && transport.Connection.PeerIdentity != null
            && transport.Connection.PeerIdentity.Length > 0;
    }

    SipProxyResult CreateSecurityResponse(SipProxyRequest request,
        SipAuthenticationResult authentication, string fallbackReason)
    {
        SipMessage? response = authentication.Message;
        if (response == null || response.Kind != SipMessageKind.Response)
        {
            SipMessage? source = request.Message;
            if (source == null && request.TransportMessage != null)
            {
                source = request.TransportMessage.Message;
            }
            response = SipTransactionMessageFactory.CreateRequestResponse(
                source, SipStatusCodes.ProxyAuthenticationRequired,
                SipReasonPhrases.ProxyAuthenticationRequired,
                tokens.CreateTag());
        }
        SipProxyResult result = new SipProxyResult();
        result.Success = true;
        result.Response = CopyMessageRequired(response);
        result.StatusCode = response.StatusCode;
        result.ErrorMessage = SanitizeText(authentication.ErrorMessage,
            fallbackReason);
        result.CorrelationId = request.CorrelationId;
        result.Completed = true;
        if (request.TransportMessage != null)
        {
            SipSendRequest send = SipViaRouting.CreateResponseSendRequest(
                response, request.TransportMessage);
            send.CorrelationId = request.CorrelationId;
            result.SendRequest = CopySend(send);
        }
        QueueEvent(SipProxyEventKind.Failed,
            request, result, null, response, result.ErrorMessage);
        return CopyResult(result);
    }

    void ApplyIdentityPolicy(SipMessage message,
        SipAuthenticationResult? authentication)
    {
        bool secured = authenticationServer != null
            && authorizationPolicy != null;
        bool removeIdentity = secured
            && (authentication == null
                || authentication.Status
                    != SipAuthenticationStatus.Success
                || PrivacyRequestsIdentity(message));
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            bool proxyCredential = SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.ProxyAuthorization);
            bool identity = SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.PAssertedIdentity)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.PPreferredIdentity);
            if (!proxyCredential && (!removeIdentity || !identity))
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        SipHeader[] filtered = new SipHeader[headers.Count];
        index = 0;
        while (index < headers.Count)
        {
            filtered[index] = headers[index];
            index = index + 1;
        }
        message.Headers = filtered;
        message.OriginalBytes = new byte[0];
    }

    static bool PrivacyRequestsIdentity(SipMessage message)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Privacy)
                && ContainsHeaderToken(header.Value, "id"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsHeaderToken(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && (value[index] == ' ' || value[index] == '\t'
                    || value[index] == ';' || value[index] == ','))
            {
                index = index + 1;
            }
            int start = index;
            while (index < value.Length && value[index] != ' '
                && value[index] != '\t' && value[index] != ';'
                && value[index] != ',')
            {
                index = index + 1;
            }
            if (index > start && string.Equals(
                    value.Substring(start, index - start), expected,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    SipServerCandidate? SelectCandidate(SipProxyTarget target,
        SipUri nextHop, string? correlationId, bool preventDowngrade)
    {
        if (target.Candidate != null)
        {
            return ValidCandidate(target.Candidate)
                ? CopyCandidate(target.Candidate) : null;
        }
        if (target.Flow != null && target.Flow.Connection != null
            && target.Flow.Connection.RemoteAddress != null
            && target.Flow.Connection.State == SipConnectionState.Connected)
        {
            SipEndpointAddress remote =
                target.Flow.Connection.RemoteAddress;
            SipServerCandidate flowCandidate = new SipServerCandidate();
            flowCandidate.Host = remote.Host;
            flowCandidate.Port = remote.Port;
            flowCandidate.Transport = remote.Transport;
            flowCandidate.Secure = remote.Secure;
            return ValidCandidate(flowCandidate) ? flowCandidate : null;
        }
        SipResolutionRequest resolution = new SipResolutionRequest();
        resolution.Uri = SipUriModelCopy.CreateNormalizedCopy(nextHop);
        resolution.Secure = nextHop.Scheme == SipUriScheme.Sips;
        resolution.CorrelationId = correlationId;
        SipResolutionResult resolved =
            resolver.ResolveTarget(resolution, dnsOptions);
        if (resolved.Status != SipResolutionStatus.Success
            || resolved.Candidates == null
            || resolved.Candidates.Length == 0)
        {
            return null;
        }
        SipServerCandidate? fallback = null;
        int index = 0;
        while (index < resolved.Candidates.Length)
        {
            SipServerCandidate candidate = resolved.Candidates[index];
            if (ValidCandidate(candidate))
            {
                if (fallback == null)
                {
                    fallback = CopyCandidate(candidate);
                }
                if (CandidateSupportsTarget(
                        candidate, nextHop, preventDowngrade))
                {
                    return CopyCandidate(candidate);
                }
            }
            index = index + 1;
        }
        return fallback;
    }

    static SipSendRequest CreateRequestSend(SipMessage message,
        SipProxyTarget target, SipEndpointAddress local,
        SipProxyRequest request)
    {
        SipServerCandidate candidate = target.Candidate == null
            ? new SipServerCandidate() : target.Candidate;
        SipEndpointAddress remote = new SipEndpointAddress();
        remote.Host = candidate.Host;
        remote.Port = candidate.Port;
        remote.Transport = candidate.Transport;
        remote.Secure = candidate.Secure;
        SipSendRequest send = new SipSendRequest();
        send.Message = CopyMessageRequired(message);
        send.RemoteAddress = remote;
        send.LocalAddress = CopyEndpoint(local);
        send.Transport = candidate.Transport;
        send.CorrelationId = request.CorrelationId;
        send.Cancellation = CopyCancellation(request.Cancellation);
        if (target.Flow != null)
        {
            send.FlowId = target.Flow.Id;
            if (target.Flow.Connection != null
                && target.Flow.Connection.State
                    == SipConnectionState.Connected)
            {
                send.ConnectionId = target.Flow.Connection.Id;
            }
        }
        return send;
    }

    static int ValidateRouteProcessing(SipMessage request,
        SipUri localRoute)
    {
        if (request.RequestUri == null
            || !SipUriComparison.CompareUriModels(
                request.RequestUri, localRoute))
        {
            return 0;
        }
        if (request.RouteValues.Length == 0)
        {
            return SipStatusCodes.BadRequest;
        }
        SipUri? restored = GetRouteUri(
            request.RouteValues[request.RouteValues.Length - 1]);
        if (restored == null)
        {
            return SipStatusCodes.BadRequest;
        }
        return request.RequestUri.Scheme == SipUriScheme.Sips
            && restored.Scheme != SipUriScheme.Sips
            ? SipStatusCodes.UnsupportedUriScheme : 0;
    }

    static int ValidateStrictRouteProcessing(SipMessage request)
    {
        if (request.RequestUri == null || request.RouteValues.Length == 0
            || RouteIsLoose(request.RouteValues[0]))
        {
            return 0;
        }
        SipUri? route = GetRouteUri(request.RouteValues[0]);
        if (route == null)
        {
            return SipStatusCodes.BadRequest;
        }
        return request.RequestUri.Scheme == SipUriScheme.Sips
            && route.Scheme != SipUriScheme.Sips
            ? SipStatusCodes.UnsupportedUriScheme : 0;
    }

    static bool ValidContactSecurity(SipMessage request,
        bool preventDowngrade)
    {
        if (!preventDowngrade || request.RequestUri == null
            || request.RequestUri.Scheme != SipUriScheme.Sips
            || request.Method != SipMethod.Invite
                && request.Method != SipMethod.Subscribe
                && request.Method != SipMethod.Refer)
        {
            return true;
        }
        int index = 0;
        while (index < request.ContactValues.Length)
        {
            SipContactValue contact = request.ContactValues[index];
            SipUri? uri = contact == null || contact.Address == null
                ? null : contact.Address.Uri;
            if (uri != null && uri.Scheme != SipUriScheme.Sips)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool CandidateSupportsTarget(SipServerCandidate candidate,
        SipUri nextHop, bool preventDowngrade)
    {
        if (!ValidCandidate(candidate))
        {
            return false;
        }
        bool secureTransport = IsSecureTransport(candidate.Transport);
        if (candidate.Secure != secureTransport)
        {
            return false;
        }
        return !preventDowngrade || nextHop.Scheme != SipUriScheme.Sips
            || secureTransport;
    }

    static bool ValidProxyOptions(SipProxyOptions options)
    {
        return options.MaxBranchCount > 0
            && options.MaxRedirectCount >= 0
            && options.TimerCMilliseconds > 0;
    }

    static SipProxyTarget[] CreateLocationTargets(
        SipRegistrationBinding[]? bindings)
    {
        if (bindings == null)
        {
            return new SipProxyTarget[0];
        }
        System.Collections.Generic.List<SipProxyTarget> targets =
            new System.Collections.Generic.List<SipProxyTarget>();
        int index = 0;
        while (index < bindings.Length)
        {
            SipRegistrationBinding binding = bindings[index];
            SipUri? uri = binding == null || binding.Contact == null
                || binding.Contact.Wildcard
                || binding.Contact.Address == null
                ? null : binding.Contact.Address.Uri;
            if (uri != null)
            {
                SipProxyTarget target = new SipProxyTarget();
                target.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
                target.QualityThousandths =
                    binding == null ? 1000 : binding.QualityThousandths;
                target.Flow = binding == null ? null
                    : SipSnapshotCopy.CopyFlowModel(binding.Flow);
                if (binding != null && binding.Path != null
                    && binding.Path.Length > 0)
                {
                    target.RouteSet =
                        SipRouteRouting.CopyRouteSet(binding.Path);
                }
                else if (binding != null && binding.ServiceRoute != null
                    && binding.ServiceRoute.Length > 0)
                {
                    target.RouteSet =
                        SipRouteRouting.CopyRouteSet(binding.ServiceRoute);
                }
                else if (target.Flow != null && target.Flow.Path != null)
                {
                    target.RouteSet =
                        SipRouteRouting.CopyRouteSet(target.Flow.Path);
                }
                if (target.Flow == null
                    && binding != null && binding.ReceivedAddress != null
                    && ValidEndpoint(binding.ReceivedAddress))
                {
                    SipServerCandidate candidate = new SipServerCandidate();
                    candidate.Host = binding.ReceivedAddress.Host;
                    candidate.Port = binding.ReceivedAddress.Port;
                    candidate.Transport = binding.ReceivedAddress.Transport;
                    candidate.Secure = binding.ReceivedAddress.Secure;
                    target.Candidate = candidate;
                }
                targets.Add(target);
            }
            index = index + 1;
        }
        SipProxyTarget[] result = new SipProxyTarget[targets.Count];
        index = 0;
        while (index < targets.Count)
        {
            result[index] = targets[index];
            index = index + 1;
        }
        return result;
    }

    static SipProxyTarget[] CreateOrderedTargets(
        SipProxyTarget[] source)
    {
        System.Collections.Generic.List<SipProxyTarget> ordered =
            new System.Collections.Generic.List<SipProxyTarget>();
        int index = 0;
        while (index < source.Length)
        {
            SipProxyTarget candidate = CopyProxyTarget(source[index]);
            int insertion = 0;
            while (insertion < ordered.Count
                && ordered[insertion].QualityThousandths
                    >= candidate.QualityThousandths)
            {
                insertion = insertion + 1;
            }
            ordered.Insert(insertion, candidate);
            index = index + 1;
        }
        System.Collections.Generic.List<SipProxyTarget> unique =
            new System.Collections.Generic.List<SipProxyTarget>();
        index = 0;
        while (index < ordered.Count)
        {
            if (!ContainsEquivalentTarget(unique, ordered[index]))
            {
                unique.Add(ordered[index]);
            }
            index = index + 1;
        }
        SipProxyTarget[] result = new SipProxyTarget[unique.Count];
        index = 0;
        while (index < unique.Count)
        {
            result[index] = unique[index];
            index = index + 1;
        }
        return result;
    }

    bool AddRedirectTargets(SipProxyBranchContext context,
        SipMessage response)
    {
        SipProxyOperationContext? operation =
            FindOperation(context.OperationId);
        if (operation == null || operation.Request == null
            || context.Target == null)
        {
            return false;
        }
        int depth = context.Target.RedirectDepth + 1;
        if (depth > operation.Request.Options.MaxRedirectCount)
        {
            return false;
        }
        bool added = false;
        int index = 0;
        while (index < response.ContactValues.Length
            && operation.Targets.Count
                < operation.Request.Options.MaxBranchCount)
        {
            SipContactValue contact = response.ContactValues[index];
            SipUri? uri = contact == null || contact.Wildcard
                || contact.ExpiresSeconds == 0
                || contact.Address == null
                ? null : contact.Address.Uri;
            bool secureDowngrade = operation.Request.Options.PreventSecureDowngrade
                && context.OutboundRequest != null
                && context.OutboundRequest.RequestUri != null
                && context.OutboundRequest.RequestUri.Scheme
                    == SipUriScheme.Sips
                && uri != null && uri.Scheme != SipUriScheme.Sips;
            if (uri != null && !secureDowngrade)
            {
                SipProxyTarget target = new SipProxyTarget();
                target.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
                target.QualityThousandths =
                    contact == null || contact.QualityThousandths < 0
                    ? 1000 : contact.QualityThousandths;
                target.ExpiresSeconds = contact == null
                    ? -1 : contact.ExpiresSeconds;
                target.RedirectDepth = depth;
                if (ValidTarget(target)
                    && !ContainsEquivalentTarget(
                        operation.Targets, target))
                {
                    InsertPendingTarget(operation, target);
                    added = true;
                }
            }
            index = index + 1;
        }
        return added;
    }

    static void InsertPendingTarget(SipProxyOperationContext operation,
        SipProxyTarget target)
    {
        int index = operation.NextTargetIndex;
        while (index < operation.Targets.Count
            && operation.Targets[index].QualityThousandths
                >= target.QualityThousandths)
        {
            index = index + 1;
        }
        operation.Targets.Insert(index, CopyProxyTarget(target));
    }

    static bool ContainsEquivalentTarget(
        System.Collections.Generic.List<SipProxyTarget> targets,
        SipProxyTarget candidate)
    {
        int index = 0;
        while (index < targets.Count)
        {
            if (TargetsEquivalent(targets[index], candidate))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool TargetsEquivalent(SipProxyTarget left,
        SipProxyTarget right)
    {
        string? leftFlow = left.Flow == null ? null : left.Flow.Id;
        string? rightFlow = right.Flow == null ? null : right.Flow.Id;
        if (leftFlow != rightFlow)
        {
            return false;
        }
        if (left.Uri != null && right.Uri != null)
        {
            return SipUriComparison.CompareUriModels(left.Uri, right.Uri);
        }
        if (left.Uri != null || right.Uri != null
            || left.Candidate == null || right.Candidate == null)
        {
            return false;
        }
        return SipAscii.EqualsIgnoreCase(
                left.Candidate.Host, right.Candidate.Host)
            && left.Candidate.Port == right.Candidate.Port
            && left.Candidate.Transport == right.Candidate.Transport;
    }

    static bool ValidTarget(SipProxyTarget target)
    {
        return target.QualityThousandths >= 0
            && target.QualityThousandths <= 1000
            && target.ExpiresSeconds >= -1
            && (target.Uri == null || target.Uri.Scheme == SipUriScheme.Sip
                || target.Uri.Scheme == SipUriScheme.Sips)
            && (target.Uri == null
                || SipUriValidator.ValidateUriModel(target.Uri).Length == 0)
            && (target.Candidate == null
                || ValidCandidate(target.Candidate))
            && ValidTargetRouteSet(target.RouteSet);
    }

    static bool ValidTargetRouteSet(SipRouteValue[]? routes)
    {
        if (routes == null)
        {
            return false;
        }
        int index = 0;
        while (index < routes.Length)
        {
            SipRouteValue route = routes[index];
            if (route == null || route.Address == null
                || route.Address.Uri == null
                || SipUriValidator.ValidateUriModel(
                    route.Address.Uri).Length != 0)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidCandidate(SipServerCandidate candidate)
    {
        return candidate.Host != null && candidate.Host.Length > 0
            && candidate.Port > 0 && candidate.Port <= 65535
            && candidate.Transport != SipTransportKind.Unknown
            && candidate.Transport != SipTransportKind.Custom;
    }

    static bool ValidEndpoint(SipEndpointAddress endpoint)
    {
        return endpoint.Host != null && endpoint.Host.Length > 0
            && endpoint.Port > 0 && endpoint.Port <= 65535
            && endpoint.Transport != SipTransportKind.Unknown;
    }

    static bool IsSecureTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Dtls;
    }

    static bool RouteIsLoose(SipRouteValue route)
    {
        SipUri? uri = GetRouteUri(route);
        if (uri == null)
        {
            return false;
        }
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter parameter = uri.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "lr"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static SipUri? GetRouteUri(SipRouteValue? route)
    {
        return route == null || route.Address == null
            ? null : route.Address.Uri;
    }

    static SipUri CreateLocalRoute(SipEndpointAddress local,
        SipMessage request)
    {
        SipUri uri = new SipUri();
        uri.Scheme = request.RequestUri != null
            && request.RequestUri.Scheme == SipUriScheme.Sips
            ? SipUriScheme.Sips : SipUriScheme.Sip;
        uri.SchemeToken = uri.Scheme == SipUriScheme.Sips
            ? "sips" : "sip";
        uri.Host = local.Host;
        uri.Port = local.Port;
        return uri;
    }

    static SipUri CreateRecordRoute(SipEndpointAddress local,
        SipMessage request)
    {
        SipUri uri = CreateLocalRoute(local, request);
        if (local.Transport != SipTransportKind.Udp
            && local.Transport != SipTransportKind.Unknown)
        {
            SipUriParameter transport = new SipUriParameter();
            transport.Name = "transport";
            transport.Value = TransportToken(local.Transport);
            transport.HasValue = true;
            uri.Parameters = new SipUriParameter[] { transport };
        }
        return uri;
    }

    static string TransportToken(SipTransportKind transport)
    {
        switch (transport)
        {
            case SipTransportKind.Tcp:
                return "tcp";
            case SipTransportKind.Tls:
                return "tls";
            case SipTransportKind.WebSocket:
                return "ws";
            case SipTransportKind.SecureWebSocket:
                return "wss";
            case SipTransportKind.Sctp:
                return "sctp";
            case SipTransportKind.Dtls:
                return "dtls";
            default:
                return "udp";
        }
    }

    int FindResponseContext(SipMessage response)
    {
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (SipViaRouting.TopViaMatches(response,
                    context.SentByHost, context.SentByPort,
                    context.Branch))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    SipProxyBranchContext? FindLateResponseContext(SipMessage response)
    {
        int index = 0;
        while (index < lateBranches.Count)
        {
            SipProxyBranchContext context = lateBranches[index];
            if (SipViaRouting.TopViaMatches(response,
                    context.SentByHost, context.SentByPort,
                    context.Branch))
            {
                return context;
            }
            index = index + 1;
        }
        return null;
    }

    SipProxyResult CreateBranchResult(SipProxyBranchContext context)
    {
        SipProxyResult result = new SipProxyResult();
        result.Success = true;
        result.CorrelationId = context.CorrelationId;
        result.AuthenticatedIdentity = context.AuthenticatedIdentity;
        if (context.Target != null)
        {
            result.Targets = new SipProxyTarget[] {
                CopyProxyTarget(context.Target) };
        }
        return result;
    }

    void PopulateOperationState(SipProxyResult result, string? operationId)
    {
        int activeCount = 0;
        int deadlineCount = 0;
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && !context.Completed)
            {
                activeCount = activeCount + 1;
                if (context.TimerCDeadlineMilliseconds >= 0)
                {
                    deadlineCount = deadlineCount + 1;
                }
            }
            index = index + 1;
        }
        long[] deadlines = new long[deadlineCount];
        int deadlineIndex = 0;
        index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && !context.Completed
                && context.TimerCDeadlineMilliseconds >= 0)
            {
                deadlines[deadlineIndex] =
                    context.TimerCDeadlineMilliseconds;
                deadlineIndex = deadlineIndex + 1;
            }
            index = index + 1;
        }
        result.ActiveBranchCount = activeCount;
        result.TimerCDeadlines = deadlines;
        result.Targets = CopyOperationTargets(operationId);
    }

    bool AllOperationBranchesCompleted(string? operationId)
    {
        int count = 0;
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId)
            {
                count = count + 1;
                if (!context.Completed)
                {
                    return false;
                }
            }
            index = index + 1;
        }
        return count > 0;
    }

    SipProxyBranchContext SelectBestResponse(string? operationId)
    {
        bool hasNonTimeout = false;
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && context.FinalResponse != null
                && context.FinalResponse.StatusCode
                    != SipStatusCodes.RequestTimeout)
            {
                hasNonTimeout = true;
            }
            index = index + 1;
        }
        SipProxyBranchContext? selected = null;
        int selectedScore = int.MaxValue;
        int selectedStatus = int.MaxValue;
        index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && context.FinalResponse != null
                && (!hasNonTimeout || context.FinalResponse.StatusCode
                    != SipStatusCodes.RequestTimeout))
            {
                int status = context.FinalResponse.StatusCode;
                int score = ResponseClassScore(status);
                if (selected == null || score < selectedScore
                    || score == selectedScore && status < selectedStatus)
                {
                    selected = context;
                    selectedScore = score;
                    selectedStatus = status;
                }
            }
            index = index + 1;
        }
        return selected == null ? new SipProxyBranchContext() : selected;
    }

    static int ResponseClassScore(int statusCode)
    {
        int responseClass = statusCode / 100;
        switch (responseClass)
        {
            case 6:
                return 0;
            case 2:
                return 1;
            case 3:
                return 2;
            case 4:
                return 3;
            case 5:
                return 4;
            default:
                return 5;
        }
    }

    SipProxyTarget[] CopyOperationTargets(string? operationId)
    {
        SipProxyOperationContext? operation = FindOperation(operationId);
        if (operation != null)
        {
            SipProxyTarget[] operationTargets =
                new SipProxyTarget[operation.Targets.Count];
            int operationIndex = 0;
            while (operationIndex < operation.Targets.Count)
            {
                operationTargets[operationIndex] =
                    CopyProxyTarget(operation.Targets[operationIndex]);
                operationIndex = operationIndex + 1;
            }
            return operationTargets;
        }
        int count = 0;
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && context.Target != null)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        SipProxyTarget[] result = new SipProxyTarget[count];
        int resultIndex = 0;
        index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (context.Stateful && context.OperationId == operationId
                && context.Target != null)
            {
                result[resultIndex] = CopyProxyTarget(context.Target);
                resultIndex = resultIndex + 1;
            }
            index = index + 1;
        }
        return result;
    }

    void RememberAndRemoveOperation(string? operationId)
    {
        int index = 0;
        while (index < branches.Count)
        {
            SipProxyBranchContext context = branches[index];
            if (!context.Stateful || context.OperationId != operationId)
            {
                index = index + 1;
                continue;
            }
            SipProxyBranchContext remembered = new SipProxyBranchContext();
            remembered.Branch = context.Branch;
            remembered.SentByHost = context.SentByHost;
            remembered.SentByPort = context.SentByPort;
            remembered.Target = context.Target == null
                ? null : CopyProxyTarget(context.Target);
            remembered.CorrelationId = context.CorrelationId;
            remembered.OperationId = context.OperationId;
            remembered.ReceivedRequest =
                CopyTransportMessage(context.ReceivedRequest);
            remembered.OutboundRequest =
                SipSnapshotCopy.CopyMessageModel(context.OutboundRequest);
            remembered.FinalResponse =
                SipSnapshotCopy.CopyMessageModel(context.FinalResponse);
            remembered.Stateful = true;
            remembered.Completed = true;
            remembered.SelectedFinal = context.SelectedFinal;
            remembered.AuthenticatedIdentity = context.AuthenticatedIdentity;
            lateBranches.Add(remembered);
            if (lateBranches.Count > LateBranchLimit)
            {
                lateBranches.RemoveAt(0);
            }
            branches.RemoveAt(index);
        }
        RemoveOperation(operationId);
    }

    static long AddTime(long current, int interval)
    {
        return current > long.MaxValue - interval
            ? long.MaxValue : current + interval;
    }

    SipProxyResult RequestFailure(SipProxyRequest? request,
        SipMessage? message, int statusCode, string reason)
    {
        string safeReason = SanitizeText(reason,
            "Proxy processing failed.");
        SipProxyResult result = new SipProxyResult();
        result.StatusCode = statusCode;
        result.ErrorMessage = safeReason;
        result.CorrelationId = request == null
            ? null : request.CorrelationId;
        QueueEvent(SipProxyEventKind.Failed, request,
            result, null, message, safeReason);
        return CopyResult(result);
    }

    SipProxyResult ResponseFailure(int statusCode, string reason)
    {
        string safeReason = SanitizeText(reason,
            "Proxy response processing failed.");
        SipProxyResult result = new SipProxyResult();
        result.StatusCode = statusCode;
        result.ErrorMessage = safeReason;
        QueueEvent(SipProxyEventKind.Failed, null,
            result, null, null, safeReason);
        return CopyResult(result);
    }

    void QueueEvent(SipProxyEventKind kind, SipProxyRequest? request,
        SipProxyResult result, SipProxyTarget? target,
        SipMessage? message, string reason)
    {
        if (events.Count >= safetyOptions.MaxEventCount)
        {
            return;
        }
        SipProxyEvent value = new SipProxyEvent();
        value.Kind = kind;
        value.Request = CopyRedactedRequest(request);
        value.Result = CopyRedactedResult(result);
        value.Target = target == null ? null : CopyProxyTarget(target);
        value.Message = CopyRedactedMessage(message);
        value.Reason = SanitizeText(reason, "Proxy event.");
        events.Add(value);
    }

    static SipProxyEvent CopyEvent(SipProxyEvent source)
    {
        SipProxyEvent copy = new SipProxyEvent();
        copy.Kind = source.Kind;
        copy.Request = CopyRequest(source.Request);
        copy.Result = source.Result == null
            ? null : CopyResult(source.Result);
        copy.Target = source.Target == null
            ? null : CopyProxyTarget(source.Target);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Reason = source.Reason;
        return copy;
    }

    static SipProxyRequest? CopyRedactedRequest(SipProxyRequest? source)
    {
        SipProxyRequest? copy = CopyRequest(source);
        if (copy == null)
        {
            return null;
        }
        copy.Message = CopyRedactedMessage(copy.Message);
        if (copy.TransportMessage != null)
        {
            copy.TransportMessage.Message =
                CopyRedactedMessage(copy.TransportMessage.Message);
            copy.TransportMessage.Bytes = new byte[0];
        }
        if (copy.Authentication != null)
        {
            copy.Authentication.Message =
                CopyRedactedMessage(copy.Authentication.Message);
            copy.Authentication.DigestAuthorization = null;
            if (copy.Authentication.BearerToken != null)
            {
                copy.Authentication.BearerToken.Token = null;
            }
        }
        return copy;
    }

    static SipProxyResult CopyRedactedResult(SipProxyResult source)
    {
        SipProxyResult copy = CopyResult(source);
        copy.Response = CopyRedactedMessage(copy.Response);
        copy.Request = CopyRedactedMessage(copy.Request);
        int index = 0;
        while (index < copy.Requests.Length)
        {
            SipMessage? message = CopyRedactedMessage(copy.Requests[index]);
            copy.Requests[index] = message == null
                ? new SipMessage() : message;
            index = index + 1;
        }
        if (copy.SendRequest != null)
        {
            RedactSend(copy.SendRequest);
        }
        index = 0;
        while (index < copy.SendRequests.Length)
        {
            RedactSend(copy.SendRequests[index]);
            index = index + 1;
        }
        copy.ErrorMessage = SanitizeText(copy.ErrorMessage,
            "Proxy processing failed.");
        return copy;
    }

    static void RedactSend(SipSendRequest send)
    {
        send.Message = CopyRedactedMessage(send.Message);
        send.Bytes = new byte[0];
    }

    static SipMessage? CopyRedactedMessage(SipMessage? source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            return null;
        }
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < copy.Headers.Length)
        {
            SipHeader header = copy.Headers[index];
            bool sensitive = SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Authorization)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.ProxyAuthorization)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.AuthenticationInfo);
            if (!sensitive)
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        SipHeader[] filtered = new SipHeader[headers.Count];
        index = 0;
        while (index < headers.Count)
        {
            filtered[index] = headers[index];
            index = index + 1;
        }
        copy.Headers = filtered;
        copy.OriginalBytes = new byte[0];
        return copy;
    }

    static SipProxyRequest? CopyRequest(SipProxyRequest? source)
    {
        if (source == null)
        {
            return null;
        }
        SipProxyRequest copy = new SipProxyRequest();
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.TransportMessage = CopyTransportMessage(source.TransportMessage);
        copy.Targets = CopyTargets(source.Targets);
        copy.Options = CopyProxyOptions(source.Options);
        copy.Cancellation = CopyCancellation(source.Cancellation);
        copy.CorrelationId = source.CorrelationId;
        copy.Authentication = CopyAuthenticationResult(source.Authentication);
        return copy;
    }

    static SipProxyResult CopyResult(SipProxyResult source)
    {
        SipProxyResult copy = new SipProxyResult();
        copy.Success = source.Success;
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.Request = SipSnapshotCopy.CopyMessageModel(source.Request);
        copy.Requests = CopyMessages(source.Requests);
        copy.SendRequest = source.SendRequest == null
            ? null : CopySend(source.SendRequest);
        copy.SendRequests = CopySends(source.SendRequests);
        copy.Targets = CopyTargets(source.Targets);
        copy.StatusCode = source.StatusCode;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
        copy.ActiveBranchCount = source.ActiveBranchCount;
        copy.TimerCDeadlines = CopyLongs(source.TimerCDeadlines);
        copy.Completed = source.Completed;
        copy.TimedOut = source.TimedOut;
        copy.AuthenticatedIdentity = source.AuthenticatedIdentity;
        return copy;
    }

    static SipMessage[] CopyMessages(SipMessage[]? source)
    {
        if (source == null)
        {
            return new SipMessage[0];
        }
        SipMessage[] result = new SipMessage[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyMessageRequired(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipSendRequest[] CopySends(SipSendRequest[]? source)
    {
        if (source == null)
        {
            return new SipSendRequest[0];
        }
        SipSendRequest[] result = new SipSendRequest[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopySend(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipMessage[] AppendMessage(SipMessage[]? source,
        SipMessage value)
    {
        SipMessage[] existing = source == null
            ? new SipMessage[0] : source;
        SipMessage[] result = new SipMessage[existing.Length + 1];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = CopyMessageRequired(existing[index]);
            index = index + 1;
        }
        result[index] = CopyMessageRequired(value);
        return result;
    }

    static SipSendRequest[] AppendSend(SipSendRequest[]? source,
        SipSendRequest value)
    {
        SipSendRequest[] existing = source == null
            ? new SipSendRequest[0] : source;
        SipSendRequest[] result =
            new SipSendRequest[existing.Length + 1];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = CopySend(existing[index]);
            index = index + 1;
        }
        result[index] = CopySend(value);
        return result;
    }

    static long[] CopyLongs(long[]? source)
    {
        if (source == null)
        {
            return new long[0];
        }
        long[] result = new long[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static SipProxyTarget[] CopyTargets(SipProxyTarget[]? source)
    {
        if (source == null)
        {
            return new SipProxyTarget[0];
        }
        SipProxyTarget[] result = new SipProxyTarget[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyProxyTarget(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipProxyTarget CopyProxyTarget(SipProxyTarget? source)
    {
        SipProxyTarget copy = new SipProxyTarget();
        if (source == null)
        {
            return copy;
        }
        copy.Uri = source.Uri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Uri);
        copy.Candidate = source.Candidate == null
            ? null : CopyCandidate(source.Candidate);
        copy.QualityThousandths = source.QualityThousandths;
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.Branch = source.Branch;
        copy.Flow = SipSnapshotCopy.CopyFlowModel(source.Flow);
        copy.RedirectDepth = source.RedirectDepth;
        copy.RouteSet = SipRouteRouting.CopyRouteSet(source.RouteSet);
        return copy;
    }

    static SipRouteDecision CopyRouteDecision(SipRouteDecision source)
    {
        SipRouteDecision copy = new SipRouteDecision();
        copy.Mode = source.Mode;
        copy.Targets = CopyTargets(source.Targets);
        copy.Redirect = source.Redirect;
        copy.RedirectStatusCode = source.RedirectStatusCode;
        copy.RejectStatusCode = source.RejectStatusCode;
        copy.Reason = source.Reason;
        copy.RecordRoute = source.RecordRoute;
        return copy;
    }

    static SipServerCandidate CopyCandidate(SipServerCandidate source)
    {
        SipServerCandidate copy = new SipServerCandidate();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Priority = source.Priority;
        copy.Weight = source.Weight;
        copy.RecordKind = source.RecordKind;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.Secure = source.Secure;
        return copy;
    }

    static SipSendRequest CopySend(SipSendRequest source)
    {
        SipSendRequest copy = new SipSendRequest();
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Bytes = CopyBytes(source.Bytes);
        copy.RemoteAddress = source.RemoteAddress == null
            ? null : CopyEndpoint(source.RemoteAddress);
        copy.LocalAddress = source.LocalAddress == null
            ? null : CopyEndpoint(source.LocalAddress);
        copy.Transport = source.Transport;
        copy.ConnectionId = source.ConnectionId;
        copy.FlowId = source.FlowId;
        copy.DeadlineMilliseconds = source.DeadlineMilliseconds;
        copy.Cancellation = CopyCancellation(source.Cancellation);
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipTransportMessage? CopyTransportMessage(
        SipTransportMessage? source)
    {
        if (source == null)
        {
            return null;
        }
        SipTransportMessage copy = new SipTransportMessage();
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Bytes = CopyBytes(source.Bytes);
        copy.LocalAddress = source.LocalAddress == null
            ? null : CopyEndpoint(source.LocalAddress);
        copy.RemoteAddress = source.RemoteAddress == null
            ? null : CopyEndpoint(source.RemoteAddress);
        copy.Transport = source.Transport;
        copy.Connection = CopyConnection(source.Connection);
        copy.Flow = SipSnapshotCopy.CopyFlowModel(source.Flow);
        copy.TimestampMilliseconds = source.TimestampMilliseconds;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipConnectionInfo? CopyConnection(SipConnectionInfo? source)
    {
        if (source == null)
        {
            return null;
        }
        SipConnectionInfo copy = new SipConnectionInfo();
        copy.Id = source.Id;
        copy.State = source.State;
        copy.Transport = source.Transport;
        copy.LocalAddress = source.LocalAddress == null
            ? null : CopyEndpoint(source.LocalAddress);
        copy.RemoteAddress = source.RemoteAddress == null
            ? null : CopyEndpoint(source.RemoteAddress);
        copy.Secure = source.Secure;
        copy.PeerIdentity = source.PeerIdentity;
        copy.CertificateIdentity = source.CertificateIdentity;
        return copy;
    }

    static SipEndpointAddress CopyEndpoint(SipEndpointAddress source)
    {
        SipEndpointAddress copy = new SipEndpointAddress();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Secure = source.Secure;
        return copy;
    }

    static SipCancellation? CopyCancellation(SipCancellation? source)
    {
        if (source == null)
        {
            return null;
        }
        SipCancellation copy = new SipCancellation();
        copy.Requested = source.Requested;
        copy.Id = source.Id;
        copy.Reason = source.Reason;
        return copy;
    }

    static SipProxyOptions CopyProxyOptions(SipProxyOptions? source)
    {
        SipProxyOptions copy = new SipProxyOptions();
        if (source == null)
        {
            return copy;
        }
        copy.Mode = source.Mode;
        copy.RecordRoute = source.RecordRoute;
        copy.MaxBranchCount = source.MaxBranchCount;
        copy.MaxRedirectCount = source.MaxRedirectCount;
        copy.TimerCMilliseconds = source.TimerCMilliseconds;
        copy.PreventSecureDowngrade = source.PreventSecureDowngrade;
        return copy;
    }

    static bool ValidSafetyOptions(SipProxySafetyOptions options)
    {
        return options.MaxTransactionCount > 0
            && options.MaxBranchCount > 0
            && options.MaxEventCount > 0
            && options.MaxRequestCount > 0
            && options.RequestWindowMilliseconds > 0
            && options.RetryAfterSeconds >= 0;
    }

    static SipProxySafetyOptions CopySafetyOptions(
        SipProxySafetyOptions source)
    {
        SipProxySafetyOptions copy = new SipProxySafetyOptions();
        copy.MaxTransactionCount = source.MaxTransactionCount;
        copy.MaxBranchCount = source.MaxBranchCount;
        copy.MaxEventCount = source.MaxEventCount;
        copy.MaxRequestCount = source.MaxRequestCount;
        copy.RequestWindowMilliseconds = source.RequestWindowMilliseconds;
        copy.RetryAfterSeconds = source.RetryAfterSeconds;
        return copy;
    }

    static SipDnsOptions CopyDnsOptions(SipDnsOptions source)
    {
        SipDnsOptions copy = new SipDnsOptions();
        SipEndpointAddress[] nameServers = source.NameServers == null
            ? new SipEndpointAddress[0] : source.NameServers;
        copy.NameServers = new SipEndpointAddress[nameServers.Length];
        int index = 0;
        while (index < nameServers.Length)
        {
            copy.NameServers[index] = CopyEndpoint(nameServers[index]);
            index = index + 1;
        }
        copy.TimeoutMilliseconds = source.TimeoutMilliseconds;
        copy.MaxAttempts = source.MaxAttempts;
        copy.MaxRecordCount = source.MaxRecordCount;
        copy.MaxCnameDepth = source.MaxCnameDepth;
        copy.EnableCache = source.EnableCache;
        copy.EnableEnum = source.EnableEnum;
        return copy;
    }

    static SipNonceOptions CopyNonceOptions(SipNonceOptions source)
    {
        SipNonceOptions copy = new SipNonceOptions();
        copy.LifetimeSeconds = source.LifetimeSeconds;
        copy.ClockSkewSeconds = source.ClockSkewSeconds;
        copy.Realm = source.Realm;
        copy.CurrentKey = CopyBytes(source.CurrentKey);
        copy.PreviousKey = CopyBytes(source.PreviousKey);
        copy.MaxReplayEntries = source.MaxReplayEntries;
        return copy;
    }

    static SipAuthenticationResult CopyAuthenticationResultRequired(
        SipAuthenticationResult source)
    {
        SipAuthenticationResult? copy = CopyAuthenticationResult(source);
        return copy == null ? new SipAuthenticationResult() : copy;
    }

    static SipAuthenticationResult? CopyAuthenticationResult(
        SipAuthenticationResult? source)
    {
        if (source == null)
        {
            return null;
        }
        SipAuthenticationResult copy = new SipAuthenticationResult();
        copy.Status = source.Status;
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.DigestAuthorization =
            CopyDigestAuthorization(source.DigestAuthorization);
        copy.BearerToken = CopyBearerToken(source.BearerToken);
        copy.CertificateIdentity =
            SipCertificateIdentityCopy.Copy(source.CertificateIdentity);
        copy.CertificateDecision =
            CopyAuthorizationDecision(source.CertificateDecision);
        copy.Identity = source.Identity;
        copy.RetryAllowed = source.RetryAllowed;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipDigestAuthorization? CopyDigestAuthorization(
        SipDigestAuthorization? source)
    {
        if (source == null)
        {
            return null;
        }
        SipDigestAuthorization copy = new SipDigestAuthorization();
        copy.Scheme = source.Scheme;
        copy.SchemeData = source.SchemeData;
        copy.UserName = source.UserName;
        copy.UserNameExtended = source.UserNameExtended;
        copy.Realm = source.Realm;
        copy.Nonce = source.Nonce;
        copy.DigestUri = source.DigestUri;
        copy.Response = source.Response;
        copy.Algorithm = source.Algorithm;
        copy.AlgorithmToken = source.AlgorithmToken;
        copy.Quality = source.Quality;
        copy.QualityToken = source.QualityToken;
        copy.NonceCount = source.NonceCount;
        copy.ClientNonce = source.ClientNonce;
        copy.Opaque = source.Opaque;
        copy.NextNonce = source.NextNonce;
        copy.UserHash = source.UserHash;
        copy.HasUserHash = source.HasUserHash;
        copy.Parameters = CopyUriParameters(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipBearerToken? CopyBearerToken(SipBearerToken? source)
    {
        if (source == null)
        {
            return null;
        }
        SipBearerToken copy = new SipBearerToken();
        copy.Token = null;
        copy.Realm = source.Realm;
        copy.AuthorizationServer = source.AuthorizationServer;
        copy.Scopes = CopyStrings(source.Scopes);
        copy.Subject = source.Subject;
        copy.Error = source.Error;
        copy.Parameters = CopyUriParameters(source.Parameters);
        return copy;
    }

    static SipAuthorizationDecision? CopyAuthorizationDecision(
        SipAuthorizationDecision? source)
    {
        if (source == null)
        {
            return null;
        }
        SipAuthorizationDecision copy = new SipAuthorizationDecision();
        copy.Allowed = source.Allowed;
        copy.Identity = source.Identity;
        copy.Reason = source.Reason;
        copy.Scopes = CopyStrings(source.Scopes);
        copy.StatusCode = source.StatusCode;
        return copy;
    }

    static SipUriParameter[] CopyUriParameters(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] copy = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter value = new SipUriParameter();
            value.Name = source[index].Name;
            value.Value = source[index].Value;
            value.HasValue = source[index].HasValue;
            value.OriginalText = source[index].OriginalText;
            value.ComparisonNameSource = source[index].ComparisonNameSource;
            value.ComparisonNameValue = source[index].ComparisonNameValue;
            value.ComparisonValueSource = source[index].ComparisonValueSource;
            value.ComparisonValueValue = source[index].ComparisonValueValue;
            value.Quoted = source[index].Quoted;
            copy[index] = value;
            index = index + 1;
        }
        return copy;
    }

    static string[] CopyStrings(string[]? source)
    {
        if (source == null)
        {
            return new string[0];
        }
        string[] copy = new string[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static void AppendHeader(SipMessage message, string name, string value)
    {
        SipHeader[] source = message.Headers == null
            ? new SipHeader[0] : message.Headers;
        SipHeader[] headers = new SipHeader[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            headers[index] = source[index];
            index = index + 1;
        }
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        headers[index] = added;
        message.Headers = headers;
        message.OriginalBytes = new byte[0];
    }

    static string SanitizeText(string? source, string fallback)
    {
        if (source == null)
        {
            return fallback;
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Length && builder.Length < 256)
        {
            char value = source[index];
            if (value >= ' ' && value != '\u007f')
            {
                builder.Append(value);
            }
            index = index + 1;
        }
        string result = builder.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static SipMessage CopyMessageRequired(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        return copy == null ? new SipMessage() : copy;
    }
}
