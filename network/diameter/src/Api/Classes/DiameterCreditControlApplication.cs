using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Provides the client and server sides of RFC 8506 credit control.</summary>
public class DiameterCreditControlApplication :
    IDiameterCreditControlClient,
    IDiameterCreditControlApplication,
    IDiameterAnswerHandler,
    IDiameterApplication,
    IDiameterRequestHandler
{
    internal CreditControlClientStateMachine ClientRuntime;
    internal CreditControlServerStateMachine ServerRuntime;
    internal CreditControlCommandFactory CommandFactory;
    internal CreditControlCommandParser CommandParser;
    internal IDiameterRequestClient RequestClient;
    internal ICreditControlEventHandler? EventHandler;
    internal object PendingLock = new object();
    internal List<CreditControlApplicationPendingRequest> PendingRequests =
        new List<CreditControlApplicationPendingRequest>();
    internal long NextOperationIdentifier = 1;
    internal DiameterApplicationRegistration Registration;

    /// <summary>Initializes credit control over the Diameter request pipeline.</summary>
    /// <param name="originHost">The local Diameter host identity.</param>
    /// <param name="originRealm">The local Diameter realm.</param>
    /// <param name="identifierSource">The protocol identifier source.</param>
    /// <param name="requestClient">The outbound request client.</param>
    /// <param name="store">The persistent credit-control session store.</param>
    /// <param name="handler">The inbound rating and credit handler.</param>
    /// <param name="eventHandler">The optional terminal outcome handler.</param>
    /// <param name="clock">The UTC clock used for credit-control state.</param>
    /// <exception cref="ArgumentException">A required dependency is invalid.</exception>
    public DiameterCreditControlApplication(
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm,
        IDiameterIdentifierSource? identifierSource,
        IDiameterRequestClient? requestClient,
        ICreditControlStore? store,
        ICreditControlRequestHandler? handler,
        ICreditControlEventHandler? eventHandler,
        IDiameterClock? clock)
    {
        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success ||
            identifierSource == null || requestClient == null ||
            store == null || handler == null || clock == null)
        {
            throw new ArgumentException(
                "Valid credit-control identities and dependencies are required.");
        }

        DiameterIdentity host = new DiameterIdentity();
        host.Value = normalizedHost.Text;
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = normalizedRealm.Text;
        RequestClient = requestClient;
        EventHandler = eventHandler;
        ClientRuntime = new CreditControlClientStateMachine(store, clock);
        ServerRuntime = new CreditControlServerStateMachine(
            store,
            handler,
            clock);
        CommandFactory = new CreditControlCommandFactory(
            identifierSource,
            host,
            realm);
        CommandParser = new CreditControlCommandParser(clock);
        Registration = CreateCreditControlApplicationRegistration();
        InitializeCreditControlApplicationOperationIdentifier(store);
        RecoverCreditControlApplicationRequests();
    }

    /// <summary>Begins one stateful credit-control session.</summary>
    /// <param name="request">The initial credit-control request.</param>
    /// <returns>The send-admission or terminal failure outcome.</returns>
    public DiameterCreditControlResult BeginDiameterCreditControlSession(
        DiameterCreditControlRequest? request)
    {
        return SendCreditControlApplicationSessionOperation(
            request,
            DiameterCreditControlRequestType.Initial);
    }

    /// <summary>Requests updated quota for one active session.</summary>
    /// <param name="request">The update credit-control request.</param>
    /// <returns>The send-admission or terminal failure outcome.</returns>
    public DiameterCreditControlResult UpdateDiameterCreditControlSession(
        DiameterCreditControlRequest? request)
    {
        return SendCreditControlApplicationSessionOperation(
            request,
            DiameterCreditControlRequestType.Update);
    }

    /// <summary>Reports final usage and terminates one active session.</summary>
    /// <param name="request">The termination credit-control request.</param>
    /// <returns>The send-admission or terminal failure outcome.</returns>
    public DiameterCreditControlResult TerminateDiameterCreditControlSession(
        DiameterCreditControlRequest? request)
    {
        return SendCreditControlApplicationSessionOperation(
            request,
            DiameterCreditControlRequestType.Termination);
    }

    /// <summary>Executes one stateless direct-debit, refund, balance, or price event.</summary>
    /// <param name="request">The event credit-control request.</param>
    /// <returns>The send-admission or terminal failure outcome.</returns>
    public DiameterCreditControlResult ExecuteDiameterCreditControlEvent(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A credit-control event request is required.");
        }

        CreditControlEventState.ValidateCreditControlEventRequest(request);
        DiameterCreditControlResult pending =
            new DiameterCreditControlResult();
        pending.Request = CreditControlDataCloner
            .CloneCreditControlRequest(request);
        pending.State = DiameterCreditControlState.Idle;
        pending.Accepted = true;
        pending.Code = "credit-control-event-pending";
        pending.Message =
            "The one-time credit-control event is awaiting an outcome.";
        long operationIdentifier =
            CreateCreditControlApplicationOperationIdentifier();
        return DispatchCreditControlApplicationRequest(
            pending,
            operationIdentifier,
            false,
            true);
    }

    /// <summary>Finds one retained client credit-control session.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned session snapshot, or <c>null</c>.</returns>
    public DiameterCreditControlSession? FindDiameterCreditControlSession(
        string? sessionId)
    {
        return ClientRuntime.FindCreditControlClientSession(sessionId);
    }

    /// <summary>Reads owned client credit-control sessions.</summary>
    /// <returns>The sessions in deterministic storage order.</returns>
    public List<DiameterCreditControlSession>
        ReadDiameterCreditControlSessions()
    {
        return ClientRuntime.ReadCreditControlClientSessions();
    }

    /// <summary>Correlates one credit-control answer or terminal request outcome.</summary>
    /// <param name="result">The owned outbound request outcome.</param>
    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        if (result == null)
        {
            return;
        }

        CreditControlApplicationPendingRequest? pending =
            TakeCreditControlApplicationPendingRequest(
                result.RequestIdentifier);
        if (pending == null)
        {
            return;
        }

        bool delivered = result.AnswerReceived;
        if (result.PendingRequest != null &&
            result.PendingRequest.Delivered)
        {
            delivered = true;
        }

        DiameterCreditControlResult processed;
        if (result.AnswerReceived && result.Correlated &&
            result.Answer != null)
        {
            DiameterCreditControlAnswer answer =
                CommandParser.ParseCreditControlAnswer(
                    result.Answer,
                    pending.Request);
            if (pending.Event)
            {
                processed = ClientRuntime.ProcessCreditControlClientEvent(
                    pending.Request,
                    answer);
            }
            else
            {
                processed = ClientRuntime.ProcessCreditControlClientAnswer(
                    pending.OperationIdentifier,
                    answer);
            }
        }
        else if (pending.Event)
        {
            processed = ClientRuntime
                .ProcessCreditControlClientEventFailure(
                    pending.Request,
                    delivered);
        }
        else
        {
            processed = ClientRuntime
                .ProcessCreditControlClientDeliveryFailure(
                    pending.OperationIdentifier,
                    delivered);
        }

        processed.Accepted = true;
        processed.Delivered = delivered;
        if (pending.Retransmission || result.PendingRequest != null &&
            result.PendingRequest.AttemptCount > 1)
        {
            processed.Retransmission = true;
        }

        NotifyCreditControlApplicationResult(processed);
    }

    /// <summary>Processes one inbound Credit-Control-Request.</summary>
    /// <param name="request">The raw Credit-Control-Request message.</param>
    /// <param name="peerIdentity">The transport peer identity, or <c>null</c>.</param>
    /// <returns>The credit-control decision and encoded answer.</returns>
    public DiameterCreditControlResult ProcessDiameterCreditControlRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity)
    {
        DiameterCreditControlRequest parsed =
            CommandParser.ParseCreditControlRequest(request);
        if (peerIdentity != null)
        {
            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
            if (normalized.Status != DiameterCodecStatus.Success)
            {
                throw new ArgumentException(
                    "The credit-control peer identity is invalid.");
            }

            parsed.PeerIdentity = new DiameterIdentity();
            parsed.PeerIdentity.Value = normalized.Text;
        }

        DiameterCreditControlResult processed = ServerRuntime
            .ProcessCreditControlServerRequest(parsed);
        if (processed.Answer == null || request == null)
        {
            throw new DiameterCreditControlException(
                "Credit-control server processing requires an answer.");
        }

        processed.Answer.Message = CommandFactory
            .CreateCreditControlAnswer(request, processed.Answer);
        NotifyCreditControlApplicationResult(processed);
        return processed;
    }

    /// <summary>Reads the capability advertised by credit control.</summary>
    /// <returns>An owned authentication application identifier.</returns>
    public DiameterApplicationId ReadDiameterCreditControlCapability()
    {
        DiameterApplicationId result = new DiameterApplicationId();
        result.Id = DiameterApplicationIds.CreditControl;
        result.VendorId = DiameterVendorIds.Ietf;
        result.IsAccounting = false;
        return result;
    }

    /// <summary>Reads the module registration supplied to the application host.</summary>
    /// <returns>An owned DCCA application registration.</returns>
    public DiameterApplicationRegistration
        ReadDiameterApplicationRegistration()
    {
        return ApplicationRegistryCloner.CloneApplicationRegistration(
            Registration);
    }

    /// <summary>Finds the handler for Credit-Control requests.</summary>
    /// <param name="commandCode">The registered command code.</param>
    /// <returns>This application for command 272, or <c>null</c>.</returns>
    public IDiameterRequestHandler?
        FindDiameterApplicationRequestHandler(uint commandCode)
    {
        if (commandCode == DiameterCommandCodes.CreditControl)
        {
            return this;
        }

        return null;
    }

    /// <summary>Reads the interceptors contributed by credit control.</summary>
    /// <returns>An empty interceptor collection.</returns>
    public List<IMessageInterceptor>
        ReadDiameterApplicationInterceptors()
    {
        return new List<IMessageInterceptor>();
    }

    /// <summary>Starts credit-control application work.</summary>
    public void StartDiameterApplication()
    {
    }

    /// <summary>Drains accepted credit-control application work.</summary>
    public void DrainDiameterApplication()
    {
    }

    /// <summary>Stops credit-control application work.</summary>
    public void StopDiameterApplication()
    {
    }

    /// <summary>Processes one application-host Credit-Control request.</summary>
    /// <param name="context">The validated inbound request context.</param>
    /// <returns>A terminal encoded Credit-Control-Answer decision.</returns>
    public DiameterApplicationResult HandleDiameterApplicationRequest(
        DiameterRequestContext context)
    {
        if (context == null)
        {
            throw new ArgumentException(
                "A credit-control request context is required.");
        }

        DiameterCreditControlResult processed =
            ProcessDiameterCreditControlRequest(
                context.Request,
                context.PeerIdentity);
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Answer = true;
        result.OutputMessage = processed.Answer == null ?
            null : processed.Answer.Message;
        result.ResultCode = processed.ResultCode;
        result.Code = processed.Code;
        result.Description = processed.Message;
        return result;
    }

    internal DiameterCreditControlResult
        SendCreditControlApplicationSessionOperation(
            DiameterCreditControlRequest? request,
            DiameterCreditControlRequestType requestType)
    {
        long operationIdentifier =
            CreateCreditControlApplicationOperationIdentifier();
        DiameterCreditControlResult pending;
        if (requestType == DiameterCreditControlRequestType.Initial)
        {
            pending = ClientRuntime.BeginCreditControlClientSession(
                request,
                operationIdentifier);
        }
        else if (requestType == DiameterCreditControlRequestType.Update)
        {
            pending = ClientRuntime.UpdateCreditControlClientSession(
                request,
                operationIdentifier);
        }
        else
        {
            pending = ClientRuntime.TerminateCreditControlClientSession(
                request,
                operationIdentifier);
        }

        return DispatchCreditControlApplicationRequest(
            pending,
            operationIdentifier,
            false,
            false);
    }

    internal DiameterCreditControlResult
        DispatchCreditControlApplicationRequest(
            DiameterCreditControlResult pending,
            long operationIdentifier,
            bool retransmission,
            bool eventRequest)
    {
        DiameterSendRequest request =
            CreateCreditControlApplicationSendRequest(
                pending.Request);
        DiameterSendResult send = RequestClient.SendDiameterRequest(
            request,
            this);
        if (send != null && send.Accepted &&
            send.PendingRequest != null)
        {
            CreditControlApplicationPendingRequest link =
                new CreditControlApplicationPendingRequest();
            link.RequestIdentifier =
                send.PendingRequest.RequestIdentifier;
            link.OperationIdentifier = operationIdentifier;
            link.Event = eventRequest;
            link.Retransmission = retransmission;
            link.Request = CreditControlDataCloner
                .CloneCreditControlRequest(pending.Request);
            AddCreditControlApplicationPendingRequest(link);
            pending.Accepted = true;
            pending.Delivered = send.Delivered;
            pending.Retransmission = retransmission;
            if (send.Code.Length > 0)
            {
                pending.Code = send.Code;
                pending.Message = send.Message;
            }

            return pending;
        }

        bool delivered = send != null && send.Delivered;
        DiameterCreditControlResult failed;
        if (eventRequest)
        {
            failed = ClientRuntime
                .ProcessCreditControlClientEventFailure(
                    pending.Request,
                    delivered);
        }
        else
        {
            failed = ClientRuntime
                .ProcessCreditControlClientDeliveryFailure(
                    operationIdentifier,
                    delivered);
        }

        failed.Accepted = false;
        failed.Delivered = delivered;
        if (send != null && send.Code.Length > 0)
        {
            failed.Code = send.Code;
            failed.Message = send.Message;
        }

        NotifyCreditControlApplicationResult(failed);
        return failed;
    }

    internal DiameterSendRequest CreateCreditControlApplicationSendRequest(
        DiameterCreditControlRequest request)
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request = CommandFactory.CreateCreditControlRequest(request);
        result.PreferredPeer = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(request.PeerIdentity);
        result.TimeoutMilliseconds = request.TimeoutMilliseconds;
        result.AllowFailover = request.AllowFailover;
        result.AllowRetransmission = true;
        result.CorrelationText = request.CorrelationText;
        return result;
    }

    internal void AddCreditControlApplicationPendingRequest(
        CreditControlApplicationPendingRequest pending)
    {
        lock (PendingLock)
        {
            int index = 0;
            while (index < PendingRequests.Count)
            {
                if (PendingRequests[index].RequestIdentifier ==
                    pending.RequestIdentifier)
                {
                    throw new DiameterCreditControlException(
                        "The request client reused a pending identifier.");
                }

                index = index + 1;
            }

            PendingRequests.Add(pending);
        }
    }

    internal CreditControlApplicationPendingRequest?
        TakeCreditControlApplicationPendingRequest(long requestIdentifier)
    {
        lock (PendingLock)
        {
            int index = 0;
            while (index < PendingRequests.Count)
            {
                if (PendingRequests[index].RequestIdentifier ==
                    requestIdentifier)
                {
                    CreditControlApplicationPendingRequest result =
                        PendingRequests[index];
                    PendingRequests.RemoveAt(index);
                    return result;
                }

                index = index + 1;
            }
        }

        return null;
    }

    internal long CreateCreditControlApplicationOperationIdentifier()
    {
        lock (PendingLock)
        {
            if (NextOperationIdentifier == long.MaxValue)
            {
                throw new DiameterCreditControlException(
                    "The credit-control operation identifier is exhausted.");
            }

            long result = NextOperationIdentifier;
            NextOperationIdentifier = NextOperationIdentifier + 1;
            return result;
        }
    }

    internal void InitializeCreditControlApplicationOperationIdentifier(
        ICreditControlStore store)
    {
        List<DiameterCreditControlSession> sessions =
            store.ReadStoredDiameterCreditControlSessions();
        int index = 0;
        while (index < sessions.Count)
        {
            long identifier = sessions[index].PendingRequestIdentifier;
            if (identifier >= NextOperationIdentifier)
            {
                if (identifier == long.MaxValue)
                {
                    throw new DiameterCreditControlException(
                        "The persisted operation identifier is exhausted.");
                }

                NextOperationIdentifier = identifier + 1;
            }

            index = index + 1;
        }
    }

    internal void RecoverCreditControlApplicationRequests()
    {
        List<DiameterCreditControlResult> recovered =
            ClientRuntime.RecoverCreditControlClientTransactions();
        int index = 0;
        while (index < recovered.Count)
        {
            DiameterCreditControlResult result = recovered[index];
            if (result.Session == null)
            {
                throw new DiameterCreditControlException(
                    "A recovered credit-control operation requires a session.");
            }

            DispatchCreditControlApplicationRequest(
                result,
                result.Session.PendingRequestIdentifier,
                true,
                false);
            index = index + 1;
        }
    }

    internal void NotifyCreditControlApplicationResult(
        DiameterCreditControlResult result)
    {
        if (EventHandler != null)
        {
            EventHandler.HandleDiameterCreditControlEvent(
                CreditControlDataCloner.CloneCreditControlResult(result));
        }
    }

    internal DiameterApplicationRegistration
        CreateCreditControlApplicationRegistration()
    {
        DiameterApplicationRegistration result =
            new DiameterApplicationRegistration();
        result.Name = "diameter-credit-control";
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.VendorId = DiameterVendorIds.Ietf;
        result.AdvertiseAuthentication = true;
        result.Roles = DiameterNodeRole.Client | DiameterNodeRole.Server;
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = DiameterCommandCodes.CreditControl;
        command.Name = "Credit-Control";
        command.IsRequest = true;
        command.RequireValidation = true;
        command.SessionAware = true;
        result.Commands.Add(command);
        return result;
    }
}
