using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Hosts registered Diameter applications, handlers, interceptors, and lifecycle.</summary>
public class DiameterApplicationHost : IDiameterApplicationHost
{
    internal IDiameterDictionary Dictionary;
    internal IDiameterValidator Validator;
    internal IDiameterClock Clock;
    internal ApplicationRegistry Registry = new ApplicationRegistry();
    internal ApplicationLifecycleManager Lifecycle;
    internal ApplicationDispatcher Dispatcher;
    internal ApplicationInterceptorPipeline Pipeline =
        new ApplicationInterceptorPipeline();
    internal IDiameterOverloadManager? OverloadManager;

    /// <summary>Initializes an application host over dictionary and validation dependencies.</summary>
    /// <param name="dictionary">The shared composed dictionary.</param>
    /// <param name="validator">The message and AVP validator.</param>
    /// <param name="clock">The UTC clock used for request and interceptor contexts.</param>
    /// <param name="limits">The application validation and codec limits.</param>
    /// <param name="originHost">The local Origin-Host identity.</param>
    /// <param name="originRealm">The local Origin-Realm identity.</param>
    public DiameterApplicationHost(
        IDiameterDictionary? dictionary,
        IDiameterValidator? validator,
        IDiameterClock? clock,
        DiameterLimitConfiguration? limits,
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm)
    {
        if (dictionary == null || validator == null || clock == null ||
            limits == null || originHost == null || originRealm == null)
        {
            throw new DiameterApplicationException(
                "An application host requires dictionary, validation, clock, limit, and identity dependencies.");
        }

        Dictionary = dictionary;
        Validator = validator;
        Clock = clock;
        Lifecycle = new ApplicationLifecycleManager(Registry);
        Dispatcher = new ApplicationDispatcher(
            Registry,
            dictionary,
            validator,
            clock,
            limits,
            originHost,
            originRealm);
    }

    /// <summary>Atomically registers one application module.</summary>
    /// <param name="application">The application module to register.</param>
    /// <returns>An owned snapshot of the active registration.</returns>
    public DiameterApplicationRegistration RegisterDiameterApplication(
        IDiameterApplication? application)
    {
        if (application == null)
        {
            throw new DiameterApplicationException(
                "An application module is required.");
        }

        DiameterApplicationRegistration registration =
            application.ReadDiameterApplicationRegistration();
        if (registration == null)
        {
            throw new DiameterApplicationException(
                "An application module must supply a registration.");
        }

        registration.ValidateApplicationRegistration();
        Pipeline.ReadOrderedApplicationInterceptors(application);
        int dictionaryIndex = 0;
        while (dictionaryIndex < registration.DictionaryModules.Count)
        {
            DiameterDictionaryRegistrationResult dictionaryResult =
                Dictionary.RegisterDictionaryModule(
                    registration.DictionaryModules[dictionaryIndex]);
            if (dictionaryResult.Status !=
                    DiameterDictionaryRegistrationStatus.Success &&
                dictionaryResult.Status !=
                    DiameterDictionaryRegistrationStatus.Duplicate)
            {
                throw new DiameterApplicationException(
                    dictionaryResult.Message);
            }

            dictionaryIndex = dictionaryIndex + 1;
        }

        return Lifecycle.StartApplicationModuleLifecycle(application);
    }

    /// <summary>Removes one application module by its stable registration name.</summary>
    /// <param name="registrationName">The stable application registration name.</param>
    /// <param name="drain">Allows queued and active work to drain before removal.</param>
    /// <returns><c>true</c> when a registered module was removed.</returns>
    public bool UnregisterDiameterApplication(
        string? registrationName,
        bool drain)
    {
        ApplicationLifecycleResult result =
            Lifecycle.UnregisterApplicationModuleLifecycle(
                registrationName,
                drain);
        return result.Found && result.Stopped;
    }

    /// <summary>Finds the application registered for one dispatch key.</summary>
    /// <param name="applicationId">The Diameter application identifier.</param>
    /// <param name="commandCode">The Diameter command code.</param>
    /// <param name="request">Indicates the request rather than answer direction.</param>
    /// <returns>An owned registration snapshot, or <c>null</c> when absent.</returns>
    public DiameterApplicationRegistration?
        FindDiameterApplicationRegistration(
            uint applicationId,
            uint commandCode,
            bool request)
    {
        ApplicationRegistryEntry? entry = Registry.ReadApplicationEntry(
            applicationId,
            commandCode,
            request);
        if (entry == null)
        {
            return null;
        }

        return ApplicationRegistryCloner.CloneApplicationRegistration(
            entry.Registration);
    }

    /// <summary>Reads all active application registrations.</summary>
    /// <returns>Owned registration snapshots in deterministic registration order.</returns>
    public List<DiameterApplicationRegistration>
        ReadDiameterApplicationRegistrations()
    {
        return Registry.ReadApplicationRegistrations();
    }

    internal ApplicationHostDispatchResult DispatchDiameterApplicationRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity,
        string? routeIdentifier,
        bool duplicate,
        bool cancelled,
        bool shuttingDown,
        IApplicationDispatchCompletionHandler? completionHandler)
    {
        ApplicationHostDispatchResult result =
            new ApplicationHostDispatchResult();
        ApplicationDispatchPreparation preparation =
            Dispatcher.PrepareInboundApplicationRequest(
                request,
                peerIdentity,
                routeIdentifier,
                duplicate,
                cancelled,
                shuttingDown);
        if (!preparation.Ready || preparation.Entry == null ||
            preparation.Context == null || preparation.Handler == null)
        {
            result.Immediate = true;
            result.Result = preparation.Result;
            return result;
        }

        if (completionHandler == null)
        {
            throw new DiameterApplicationException(
                "Queued application dispatch requires a completion handler.");
        }

        ApplicationWorkQueue? queue =
            Lifecycle.ReadApplicationWorkQueue(
                preparation.Entry.Registration.Name);
        if (queue == null)
        {
            result.Immediate = true;
            result.Result = Dispatcher.CompleteInboundApplicationRequest(
                preparation,
                CreateApplicationHostUnavailableResult());
            return result;
        }

        queue.ConfigureDiameterOverloadAdmission(OverloadManager);

        ApplicationHostRequestHandler handler =
            new ApplicationHostRequestHandler(this, preparation);
        ApplicationHostWorkCompletionHandler completion =
            new ApplicationHostWorkCompletionHandler(
                this,
                preparation,
                completionHandler);
        ApplicationWorkAdmission admission =
            queue.EnqueueApplicationWork(
                handler,
                preparation.Context,
                completion);
        result.WorkIdentifier = admission.WorkIdentifier;
        result.Accepted = admission.Accepted;
        result.Started = admission.Started;
        result.Queued = admission.Queued;
        if (!admission.Accepted)
        {
            result.Immediate = true;
            DiameterApplicationResult rejection =
                admission.RejectionResult == null ?
                CreateApplicationHostUnavailableResult() :
                admission.RejectionResult;
            result.Result = Dispatcher.CompleteInboundApplicationRequest(
                preparation,
                rejection);
        }

        return result;
    }

    internal void ConfigureDiameterOverloadAdmission(
        IDiameterOverloadManager? manager)
    {
        OverloadManager = manager;
    }

    internal DiameterApplicationResult ProcessDiameterApplicationWork(
        ApplicationDispatchPreparation preparation,
        DiameterRequestContext context)
    {
        if (preparation.Entry == null || preparation.Handler == null ||
            preparation.Command == null)
        {
            return CreateApplicationHostUnavailableResult();
        }

        preparation.Context = context;
        DiameterInterceptorContext inboundContext =
            CreateApplicationHostInterceptorContext(
                context.Request,
                context,
                null,
                true);
        DiameterApplicationResult inbound =
            Pipeline.ProcessInboundApplicationInterceptors(
                preparation.Entry.Application,
                inboundContext);
        if (inbound.Replace && inbound.OutputMessage != null)
        {
            DiameterApplicationResult? replacementFailure =
                ApplyApplicationHostReplacement(
                    preparation,
                    context,
                    inbound.OutputMessage);
            if (replacementFailure != null)
            {
                return replacementFailure;
            }
        }
        else if (!inbound.Continue)
        {
            if (inbound.Redirect)
            {
                return inbound;
            }

            return Dispatcher.CompleteInboundApplicationRequest(
                preparation,
                inbound);
        }

        DiameterApplicationResult handlerResult =
            preparation.Handler.HandleDiameterApplicationRequest(context);
        DiameterApplicationResult processed =
            Dispatcher.CompleteInboundApplicationRequest(
                preparation,
                handlerResult);
        return ProcessDiameterApplicationOutboundInterceptors(
            preparation,
            processed);
    }

    internal DiameterApplicationResult ProcessDiameterApplicationOutboundInterceptors(
        ApplicationDispatchPreparation preparation,
        DiameterApplicationResult processed)
    {
        if (preparation.Entry == null || preparation.Context == null ||
            !processed.Answer || processed.OutputMessage == null)
        {
            return processed;
        }

        DiameterInterceptorContext outboundContext =
            CreateApplicationHostInterceptorContext(
                processed.OutputMessage,
                preparation.Context,
                processed,
                false);
        DiameterApplicationResult outbound =
            Pipeline.ProcessOutboundApplicationInterceptors(
                preparation.Entry.Application,
                outboundContext);
        if (outbound.Continue)
        {
            return processed;
        }

        if (outbound.Replace && outbound.OutputMessage != null &&
            preparation.Command != null &&
            Dispatcher.ValidateLocalApplicationAnswer(
                preparation.Request,
                outbound.OutputMessage,
                preparation.Command,
                preparation.DictionarySnapshot))
        {
            DiameterApplicationResult result =
                Pipeline.ClonePipelineResult(processed);
            result.RequestContext = preparation.Context;
            result.OutputMessage =
                Pipeline.ClonePipelineMessage(outbound.OutputMessage);
            result.Code = outbound.Code;
            result.Description = outbound.Description;
            return result;
        }

        return outbound;
    }

    internal DiameterApplicationResult? ApplyApplicationHostReplacement(
        ApplicationDispatchPreparation preparation,
        DiameterRequestContext context,
        DiameterMessage replacement)
    {
        if (replacement.ApplicationId != preparation.Request.ApplicationId ||
            replacement.CommandCode != preparation.Request.CommandCode ||
            !Dispatcher.ValidateInboundApplicationRequestHeader(replacement))
        {
            return Dispatcher.CreateApplicationErrorResult(
                preparation.Request,
                DiameterResultCodes.UnableToComply,
                "application-interceptor-replacement-invalid",
                "An inbound interceptor replaced the request with an incompatible message.");
        }

        DiameterMessage owned =
            Dispatcher.CloneApplicationMessage(replacement);
        if (preparation.Command != null &&
            preparation.Command.RequireValidation)
        {
            DiameterDictionarySnapshot snapshot =
                Dictionary.ReadDictionarySnapshot();
            DiameterValidationResult validation = Validator.ValidateMessage(
                owned,
                snapshot,
                Dispatcher.Limits);
            if (validation == null ||
                validation.Status != DiameterValidationStatus.Valid)
            {
                return Dispatcher.CreateValidationErrorResult(
                    owned,
                    validation);
            }

            preparation.DictionarySnapshot = snapshot;
            context.ValidationResult = validation;
        }

        preparation.Request = owned;
        context.Request = Dispatcher.CloneApplicationMessage(owned);
        context.SessionId =
            Dispatcher.ReadApplicationSessionIdentifier(owned);
        context.Retransmission =
            (owned.Flags &
            DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None;
        return null;
    }

    internal DiameterInterceptorContext CreateApplicationHostInterceptorContext(
        DiameterMessage message,
        DiameterRequestContext context,
        DiameterApplicationResult? previous,
        bool inbound)
    {
        DiameterInterceptorContext result =
            new DiameterInterceptorContext();
        result.Message = Pipeline.ClonePipelineMessage(message);
        if (inbound)
        {
            result.RequestContext =
                Pipeline.ClonePipelineRequestContext(context);
        }
        result.PreviousResult =
            Pipeline.ClonePipelineOptionalResult(previous);
        result.PeerIdentity =
            Pipeline.ClonePipelineIdentity(context.PeerIdentity);
        result.RouteIdentifier = context.RouteIdentifier;
        result.SessionId = context.SessionId;
        result.InvokedAt = Clock.ReadUtcTime();
        result.Inbound = inbound;
        result.Duplicate = context.Duplicate;
        result.Retransmission = context.Retransmission;
        result.Cancelled = context.Cancelled;
        result.ShuttingDown = context.ShuttingDown;
        return result;
    }

    internal void CompleteDiameterApplicationWork(
        ApplicationDispatchPreparation preparation,
        ApplicationWorkCompletion completion,
        IApplicationDispatchCompletionHandler completionHandler)
    {
        DiameterApplicationResult result = completion.HandlerFailed ?
            Dispatcher.CompleteInboundApplicationRequest(
                preparation,
                completion.Result) :
            completion.Result;
        completionHandler.CompleteDiameterApplicationDispatch(result);
    }

    internal ApplicationShutdownResult ShutdownDiameterApplications()
    {
        return Lifecycle.ShutdownApplicationModules();
    }

    internal DiameterApplicationResult CreateApplicationHostUnavailableResult()
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.ResultCode = DiameterResultCodes.TooBusy;
        result.Code = "application-host-unavailable";
        result.Description =
            "The selected application is not accepting work.";
        return result;
    }
}
