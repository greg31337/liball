using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class DiameterApplicationHostTests
{
    [Test]
    public void PublicHostRegistersDictionaryStartsAndGracefullyRemovesModule()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("answer", log);
        HostApplication application =
            new HostApplication(handler, log, true);

        DiameterApplicationRegistration registered =
            host.RegisterDiameterApplication(application);
        registered.Name = "mutated";
        registered.Commands[0].CommandCode = 999U;
        DiameterApplicationRegistration? found =
            host.FindDiameterApplicationRegistration(
                16777216U,
                300U,
                true);
        List<DiameterApplicationRegistration> all =
            host.ReadDiameterApplicationRegistrations();
        DiameterApplicationDefinition? dictionaryApplication =
            dictionary.FindDictionaryApplicationById(
                DiameterVendorIds.Ietf,
                16777216U);
        bool removed = host.UnregisterDiameterApplication(
            "host-test",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(application.StartCount, Is.EqualTo(1));
            Assert.That(application.DrainCount, Is.EqualTo(1));
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Name, Is.EqualTo("host-test"));
            Assert.That(found.Commands[0].CommandCode, Is.EqualTo(300U));
            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0].Name, Is.EqualTo("host-test"));
            Assert.That(dictionaryApplication, Is.Not.Null);
            Assert.That(dictionaryApplication!.Name,
                Is.EqualTo("Host-Test"));
            Assert.That(removed, Is.True);
            Assert.That(host.FindDiameterApplicationRegistration(
                16777216U,
                300U,
                true), Is.Null);
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start",
                    "drain",
                    "stop"
                }));
        });
    }

    [Test]
    public void ValidatedQueuedDispatchRunsInterceptorsAndHandlerInOrder()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("answer", log);
        HostApplication application =
            new HostApplication(handler, log, true);
        application.Interceptors.Add(
            new HostInterceptor("ordered", 10, "continue", log));
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(1);
        HostDispatchCompletion callback =
            new HostDispatchCompletion(completed, log);

        ApplicationHostDispatchResult dispatch =
            host.DispatchDiameterApplicationRequest(
                CreateRequest(true),
                CreateIdentity("peer.example"),
                "route-a",
                false,
                false,
                false,
                callback);

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        DiameterApplicationResult result = callback.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Accepted, Is.True);
            Assert.That(dispatch.Immediate, Is.False);
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.LastContext, Is.Not.Null);
            Assert.That(handler.LastContext!.ValidationResult, Is.Not.Null);
            Assert.That(handler.LastContext.ValidationResult!.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(handler.LastContext.RouteIdentifier,
                Is.EqualTo("route-a"));
            Assert.That(result.Answer, Is.True);
            Assert.That(ReadResultCode(result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start",
                    "inbound:ordered",
                    "handler",
                    "outbound:ordered",
                    "callback"
                }));
        });
    }

    [Test]
    public void TerminalInboundInterceptorSkipsRequestHandler()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("answer", log);
        HostApplication application =
            new HostApplication(handler, log, false);
        application.Interceptors.Add(
            new HostInterceptor("dropper", 10, "drop", log));
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(1);
        HostDispatchCompletion callback =
            new HostDispatchCompletion(completed, log);

        ApplicationHostDispatchResult dispatch =
            host.DispatchDiameterApplicationRequest(
                CreateRequest(false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                callback);

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Accepted, Is.True);
            Assert.That(handler.CallCount, Is.Zero);
            Assert.That(callback.Result!.Drop, Is.True);
            Assert.That(callback.Result.Code, Is.EqualTo("interceptor-drop"));
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start",
                    "inbound:dropper",
                    "callback"
                }));
        });
    }

    [Test]
    public void HandlerExceptionIsIsolatedAndMappedToProtocolAnswer()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("throw", log);
        HostApplication application =
            new HostApplication(handler, log, false);
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(1);
        HostDispatchCompletion callback =
            new HostDispatchCompletion(completed, log);

        ApplicationHostDispatchResult dispatch =
            host.DispatchDiameterApplicationRequest(
                CreateRequest(false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                callback);

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Accepted, Is.True);
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(callback.Result!.Answer, Is.True);
            Assert.That(callback.Result.Code,
                Is.EqualTo("application-handler-exception"));
            Assert.That(ReadResultCode(callback.Result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
        });
    }

    [Test]
    public void PublicRegistrationConflictsAndCapabilityChangesAreAtomic()
    {
        DiameterApplicationHost host = CreateHost(
            new DiameterDictionary());
        OutcomeApplication first = new OutcomeApplication(
            CreateOutcomeRegistration("first", 700U, 310U, false),
            new OutcomeHandler());
        first.Registration.AdvertiseAuthentication = true;
        OutcomeApplication duplicateCapability = new OutcomeApplication(
            CreateOutcomeRegistration("second", 700U, 311U, false),
            new OutcomeHandler());
        duplicateCapability.Registration.AdvertiseAuthentication = true;
        OutcomeApplication duplicateName = new OutcomeApplication(
            CreateOutcomeRegistration("FIRST", 701U, 312U, false),
            new OutcomeHandler());
        OutcomeApplication duplicateDispatch = new OutcomeApplication(
            CreateOutcomeRegistration("third", 700U, 310U, false),
            new OutcomeHandler());

        host.RegisterDiameterApplication(first);
        Assert.That(host.Registry.ReadApplicationCapabilityRevision(),
            Is.EqualTo(1L));
        Assert.Throws<DiameterApplicationException>(() =>
            host.RegisterDiameterApplication(duplicateName));
        Assert.Throws<DiameterApplicationException>(() =>
            host.RegisterDiameterApplication(duplicateDispatch));
        host.RegisterDiameterApplication(duplicateCapability);
        DiameterPeerCapabilities bothCapabilities =
            host.Registry.ReadApplicationCapabilities();
        long bothRevision =
            host.Registry.ReadApplicationCapabilityRevision();
        bool removedFirst = host.UnregisterDiameterApplication(
            "first",
            true);
        DiameterPeerCapabilities retainedCapabilities =
            host.Registry.ReadApplicationCapabilities();
        long retainedRevision =
            host.Registry.ReadApplicationCapabilityRevision();
        bool removedSecond = host.UnregisterDiameterApplication(
            "second",
            false);
        bool removedMissing = host.UnregisterDiameterApplication(
            "missing",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(host.ReadDiameterApplicationRegistrations(),
                Is.Empty);
            Assert.That(duplicateName.StartCount, Is.Zero);
            Assert.That(duplicateDispatch.StartCount, Is.Zero);
            Assert.That(bothCapabilities.AuthApplicationIds,
                Is.EqualTo(new uint[] { 700U }));
            Assert.That(bothRevision, Is.EqualTo(1L));
            Assert.That(removedFirst, Is.True);
            Assert.That(retainedCapabilities.AuthApplicationIds,
                Is.EqualTo(new uint[] { 700U }));
            Assert.That(retainedRevision, Is.EqualTo(1L));
            Assert.That(removedSecond, Is.True);
            Assert.That(removedMissing, Is.False);
            Assert.That(host.Registry.ReadApplicationCapabilities()
                .AuthApplicationIds, Is.Empty);
            Assert.That(host.Registry.ReadApplicationCapabilityRevision(),
                Is.EqualTo(2L));
        });
    }

    [Test]
    public void HostQueueSaturationRejectsOnlyExcessWork()
    {
        DiameterApplicationHost host = CreateHost(
            new DiameterDictionary());
        BlockingOutcomeHandler handler = new BlockingOutcomeHandler();
        DiameterApplicationRegistration registration =
            CreateOutcomeRegistration("bounded", 701U, 313U, false);
        registration.MaximumConcurrency = 1;
        registration.QueueCapacity = 1;
        OutcomeApplication application = new OutcomeApplication(
            registration,
            handler);
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(2);
        OutcomeCompletion firstCompletion =
            new OutcomeCompletion(completed);
        OutcomeCompletion secondCompletion =
            new OutcomeCompletion(completed);

        ApplicationHostDispatchResult first =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(701U, 313U, false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                firstCompletion);
        Assert.That(handler.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationHostDispatchResult second =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(701U, 313U, false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                secondCompletion);
        ApplicationHostDispatchResult rejected =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(701U, 313U, false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                new OutcomeCompletion(new CountdownEvent(1)));

        handler.Release.Set();
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        host.UnregisterDiameterApplication("bounded", true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Started, Is.True);
            Assert.That(second.Accepted, Is.True);
            Assert.That(second.Queued, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Immediate, Is.True);
            Assert.That(rejected.Result!.Answer, Is.True);
            Assert.That(rejected.Result.Code,
                Is.EqualTo("application-queue-overloaded"));
            Assert.That(ReadResultCode(rejected.Result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(firstCompletion.Result!.Answer, Is.True);
            Assert.That(secondCompletion.Result!.Answer, Is.True);
        });
    }

    [Test]
    public void GracefulUnregisterRetractsDispatchBeforeAcceptedWorkCompletes()
    {
        DiameterApplicationHost host = CreateHost(
            new DiameterDictionary());
        BlockingOutcomeHandler handler = new BlockingOutcomeHandler();
        OutcomeApplication application = new OutcomeApplication(
            CreateOutcomeRegistration("racing", 702U, 314U, false),
            handler);
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(1);
        OutcomeCompletion completion = new OutcomeCompletion(completed);
        ApplicationHostDispatchResult accepted =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(702U, 314U, false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                completion);
        Assert.That(handler.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        Task<bool> removal = Task.Run(() =>
            host.UnregisterDiameterApplication("racing", true));
        Assert.That(application.DrainEntered.Wait(
            TimeSpan.FromSeconds(5)), Is.True);
        ApplicationHostDispatchResult raced =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(702U, 314U, false),
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false,
                null);

        handler.Release.Set();
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(removal.Wait(TimeSpan.FromSeconds(5)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(raced.Immediate, Is.True);
            Assert.That(raced.Accepted, Is.False);
            Assert.That(raced.Result!.Code,
                Is.EqualTo("application-unsupported"));
            Assert.That(ReadResultCode(raced.Result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.ApplicationUnsupported));
            Assert.That(removal.Result, Is.True);
            Assert.That(application.DrainCount, Is.EqualTo(1));
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RawAndValidatedDispatchReachTypedAdapterWithDistinctContexts()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        AdapterOutcomeHandler handler = new AdapterOutcomeHandler(dictionary);
        DiameterApplicationRegistration registration =
            CreateOutcomeRegistration("adapter", 703U, 315U, false);
        DiameterCommandRegistration validated =
            new DiameterCommandRegistration();
        validated.CommandCode = 316U;
        validated.Name = "Validated-Request";
        validated.RequireValidation = true;
        registration.Commands.Add(validated);
        registration.DictionaryModules.Add(
            CreateOutcomeDictionaryModule(703U, 315U, 316U));
        OutcomeApplication application = new OutcomeApplication(
            registration,
            handler);
        host.RegisterDiameterApplication(application);
        CountdownEvent completed = new CountdownEvent(2);

        ApplicationHostDispatchResult raw =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(703U, 315U, false),
                CreateIdentity("peer.example"),
                "raw-route",
                false,
                false,
                false,
                new OutcomeCompletion(completed));
        ApplicationHostDispatchResult validatedDispatch =
            host.DispatchDiameterApplicationRequest(
                CreateOutcomeRequest(703U, 316U, true),
                CreateIdentity("peer.example"),
                "validated-route",
                false,
                false,
                false,
                new OutcomeCompletion(completed));

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        host.UnregisterDiameterApplication("adapter", true);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Accepted, Is.True);
            Assert.That(validatedDispatch.Accepted, Is.True);
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(handler.RawContextCount, Is.EqualTo(1));
            Assert.That(handler.ValidatedContextCount, Is.EqualTo(1));
            Assert.That(handler.KnownSessionCount, Is.EqualTo(1));
            Assert.That(handler.PreservedUnknownCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void HostRejectsEveryMissingDependencyRegistrationAndCompletion()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterValidator validator = new DiameterValidator();
        TestDiameterClock clock = new TestDiameterClock(
            DateTimeOffset.UtcNow,
            0L);
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        DiameterIdentity hostIdentity = CreateIdentity("local.example");
        DiameterIdentity realmIdentity = CreateIdentity("example");

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    null, validator, clock, limits, hostIdentity, realmIdentity));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    dictionary, null, clock, limits, hostIdentity, realmIdentity));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    dictionary, validator, null, limits, hostIdentity, realmIdentity));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    dictionary, validator, clock, null, hostIdentity, realmIdentity));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    dictionary, validator, clock, limits, null, realmIdentity));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationHost(
                    dictionary, validator, clock, limits, hostIdentity, null));
        });

        DiameterApplicationHost publicHost = CreateHost(dictionary);
        Assert.Throws<DiameterApplicationException>(() =>
            publicHost.RegisterDiameterApplication(null));
        Assert.Throws<DiameterApplicationException>(() =>
            publicHost.RegisterDiameterApplication(
                new NullRegistrationApplication()));

        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("answer", log);
        HostApplication application = new HostApplication(handler, log, false);
        publicHost.RegisterDiameterApplication(application);
        Assert.Throws<DiameterApplicationException>(() =>
            publicHost.DispatchDiameterApplicationRequest(
                CreateRequest(false),
                CreateIdentity("peer.example"),
                "route",
                false,
                false,
                false,
                null));
    }

    [Test]
    public void HostInternalDispatchHandlesMissingQueueAndPreparationShapes()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostHandler handler = new HostHandler("answer", log);
        HostApplication application = new HostApplication(handler, log, false);
        host.Registry.RegisterApplicationModule(application);
        CountdownEvent completed = new CountdownEvent(1);
        HostDispatchCompletion completion =
            new HostDispatchCompletion(completed, log);

        ApplicationHostDispatchResult unavailable =
            host.DispatchDiameterApplicationRequest(
                CreateRequest(false),
                CreateIdentity("peer.example"),
                "route",
                false,
                false,
                false,
                completion);
        Assert.That(unavailable.Immediate, Is.True);
        Assert.That(unavailable.Result, Is.Not.Null);

        DiameterApplicationRegistration registration =
            application.ReadDiameterApplicationRegistration();
        ApplicationRegistryEntry entry = new ApplicationRegistryEntry(
            application,
            registration);
        DiameterRequestContext context = new DiameterRequestContext();
        context.Request = CreateRequest(false);
        ApplicationDispatchPreparation missingAll =
            new ApplicationDispatchPreparation();
        ApplicationDispatchPreparation missingHandler =
            new ApplicationDispatchPreparation();
        missingHandler.Entry = entry;
        ApplicationDispatchPreparation missingCommand =
            new ApplicationDispatchPreparation();
        missingCommand.Entry = entry;
        missingCommand.Handler = handler;

        Assert.Multiple(() =>
        {
            Assert.That(host.ProcessDiameterApplicationWork(
                missingAll, context).Code, Is.EqualTo("application-host-unavailable"));
            Assert.That(host.ProcessDiameterApplicationWork(
                missingHandler, context).Code, Is.EqualTo("application-host-unavailable"));
            Assert.That(host.ProcessDiameterApplicationWork(
                missingCommand, context).Code, Is.EqualTo("application-host-unavailable"));
        });

        DiameterApplicationResult processed = new DiameterApplicationResult();
        ApplicationDispatchPreparation outboundMissingEntry =
            new ApplicationDispatchPreparation();
        ApplicationDispatchPreparation outboundMissingContext =
            new ApplicationDispatchPreparation();
        outboundMissingContext.Entry = entry;
        ApplicationDispatchPreparation outboundNoAnswer =
            new ApplicationDispatchPreparation();
        outboundNoAnswer.Entry = entry;
        outboundNoAnswer.Context = context;
        ApplicationDispatchPreparation outboundNoMessage =
            new ApplicationDispatchPreparation();
        outboundNoMessage.Entry = entry;
        outboundNoMessage.Context = context;
        processed.Answer = false;
        host.ProcessDiameterApplicationOutboundInterceptors(
            outboundMissingEntry,
            processed);
        host.ProcessDiameterApplicationOutboundInterceptors(
            outboundMissingContext,
            processed);
        host.ProcessDiameterApplicationOutboundInterceptors(
            outboundNoAnswer,
            processed);
        processed.Answer = true;
        host.ProcessDiameterApplicationOutboundInterceptors(
            outboundNoMessage,
            processed);
        Assert.That(processed.OutputMessage, Is.Null);
    }

    [Test]
    public void InboundReplacementRejectsEachIncompatibleHeaderAndValidationFailure()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        dictionary.RegisterDictionaryModule(CreateDictionaryModule());
        DiameterApplicationHost host = CreateHost(dictionary);
        DiameterMessage request = CreateRequest(true);
        DiameterRequestContext context = new DiameterRequestContext();
        context.Request = request;
        ApplicationDispatchPreparation preparation =
            new ApplicationDispatchPreparation();
        preparation.Request = request;

        DiameterMessage wrongApplication = CreateRequest(true);
        wrongApplication.ApplicationId = 1U;
        DiameterMessage wrongCommand = CreateRequest(true);
        wrongCommand.CommandCode = 1U;
        DiameterMessage invalidHeader = CreateRequest(true);
        invalidHeader.Flags = DiameterMessageFlags.None;
        DiameterMessage compatible = CreateRequest(true);

        Assert.Multiple(() =>
        {
            Assert.That(host.ApplyApplicationHostReplacement(
                preparation, context, wrongApplication), Is.Not.Null);
            Assert.That(host.ApplyApplicationHostReplacement(
                preparation, context, wrongCommand), Is.Not.Null);
            Assert.That(host.ApplyApplicationHostReplacement(
                preparation, context, invalidHeader), Is.Not.Null);
            Assert.That(host.ApplyApplicationHostReplacement(
                preparation, context, compatible), Is.Null);
        });

        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = 300U;
        command.Name = "Host-Test-Request";
        command.RequireValidation = true;
        preparation.Command = command;
        DiameterMessage invalidBody = CreateRequest(false);
        Assert.That(host.ApplyApplicationHostReplacement(
            preparation,
            context,
            invalidBody), Is.Not.Null);
    }

    [Test]
    public void HostProcessesReplacementRedirectAndTerminalOutboundInterceptors()
    {
        DiameterApplicationResult replaced =
            ProcessHostInterceptorMode("replace");
        DiameterApplicationResult invalidReplacement =
            ProcessHostInterceptorMode("invalid-replace");
        DiameterApplicationResult redirected =
            ProcessHostInterceptorMode("redirect");
        DiameterApplicationResult outboundDrop =
            ProcessHostInterceptorMode("outbound-drop");
        DiameterApplicationResult outboundReplace =
            ProcessHostInterceptorMode("outbound-replace");

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Answer, Is.True);
            Assert.That(invalidReplacement.Code,
                Is.EqualTo("application-interceptor-replacement-invalid"));
            Assert.That(redirected.Code, Is.Not.Empty);
            Assert.That(outboundDrop.Drop, Is.True);
            Assert.That(outboundReplace.OutputMessage, Is.Not.Null);
        });
    }

    [Test]
    public void DuplicateApplicationDictionaryIsAcceptedDuringRegistration()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        Assert.That(dictionary.RegisterDictionaryModule(
            CreateDictionaryModule()).Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostApplication application = new HostApplication(
            new HostHandler("answer", log),
            log,
            false);
        Assert.DoesNotThrow(() =>
            host.RegisterDiameterApplication(application));
    }

    private static DiameterApplicationResult ProcessHostInterceptorMode(
        string mode)
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = CreateHost(dictionary);
        List<string> log = new List<string>();
        HostApplication application = new HostApplication(
            new HostHandler("answer", log),
            log,
            false);
        application.Interceptors.Add(
            new HostInterceptor("matrix", 1, mode, log));
        host.RegisterDiameterApplication(application);
        ApplicationDispatchPreparation preparation = host.Dispatcher
            .PrepareInboundApplicationRequest(
                CreateRequest(false),
                CreateIdentity("peer.example"),
                "route",
                false,
                false,
                false);
        return host.ProcessDiameterApplicationWork(
            preparation,
            preparation.Context!);
    }

    private static DiameterApplicationRegistration CreateOutcomeRegistration(
        string name,
        uint applicationId,
        uint commandCode,
        bool requireValidation)
    {
        DiameterApplicationRegistration registration =
            new DiameterApplicationRegistration();
        registration.Name = name;
        registration.ApplicationId = applicationId;
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = commandCode;
        command.Name = name + "-request";
        command.RequireValidation = requireValidation;
        registration.Commands.Add(command);
        return registration;
    }

    private static DiameterMessage CreateOutcomeRequest(
        uint applicationId,
        uint commandCode,
        bool includeSession)
    {
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.CommandCode = commandCode;
        request.ApplicationId = applicationId;
        request.HopByHopIdentifier = commandCode;
        request.EndToEndIdentifier = commandCode + 1U;
        if (includeSession)
        {
            request.Avps.Add(
                new DiameterBaseAvpFactory().CreateSessionIdAvp(
                    "peer.example;adapter"));
        }

        DiameterAvp unknown = new DiameterAvp();
        unknown.Code = 999U;
        unknown.VendorId = 9999U;
        unknown.Flags = DiameterAvpFlags.VendorSpecific;
        unknown.Data = new byte[] { 9, 8, 7 };
        request.Avps.Add(unknown);
        return request;
    }

    private static DiameterMessage CreateOutcomeAnswer(
        DiameterMessage request)
    {
        DiameterMessage answer = new DiameterMessage();
        answer.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        answer.CommandCode = request.CommandCode;
        answer.ApplicationId = request.ApplicationId;
        answer.HopByHopIdentifier = request.HopByHopIdentifier;
        answer.EndToEndIdentifier = request.EndToEndIdentifier;
        DiameterAvp resultCode = new DiameterAvp();
        resultCode.Code = DiameterAvpCodes.ResultCode;
        resultCode.Flags = DiameterAvpFlags.Mandatory;
        resultCode.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            resultCode.Data,
            DiameterResultCodes.Success);
        answer.Avps.Add(resultCode);
        return answer;
    }

    private static DiameterApplicationResult CreateOutcomeAnswerResult(
        DiameterRequestContext context)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Answer = true;
        result.OutputMessage = CreateOutcomeAnswer(context.Request);
        result.Code = "handled";
        return result;
    }

    private static DiameterDictionaryModule CreateOutcomeDictionaryModule(
        uint applicationId,
        uint rawCommandCode,
        uint validatedCommandCode)
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "application-outcome-dictionary";
        module.Version = "1";
        module.Dependencies.Add("rfc6733");
        DiameterApplicationDefinition application =
            new DiameterApplicationDefinition();
        application.Id = applicationId;
        application.Name = "Application-Outcome";
        module.Applications.Add(application);
        module.Commands.Add(CreateOutcomeRequestDefinition(
            applicationId,
            rawCommandCode,
            false));
        module.Commands.Add(CreateOutcomeAnswerDefinition(
            applicationId,
            rawCommandCode));
        module.Commands.Add(CreateOutcomeRequestDefinition(
            applicationId,
            validatedCommandCode,
            true));
        module.Commands.Add(CreateOutcomeAnswerDefinition(
            applicationId,
            validatedCommandCode));
        return module;
    }

    private static DiameterCommandDefinition CreateOutcomeRequestDefinition(
        uint applicationId,
        uint commandCode,
        bool requireSession)
    {
        DiameterCommandDefinition request =
            new DiameterCommandDefinition();
        request.Code = commandCode;
        request.ApplicationId = applicationId;
        request.Name = "Application-Outcome-Request-" + commandCode;
        request.IsRequest = true;
        request.RequiredFlags = DiameterMessageFlags.Request;
        request.ProhibitedFlags = DiameterMessageFlags.Error;
        if (requireSession)
        {
            DiameterAvpRuleDefinition session =
                new DiameterAvpRuleDefinition();
            session.AvpCode = DiameterAvpCodes.SessionId;
            session.AvpName = "Session-Id";
            session.MinimumCount = 1;
            session.MaximumCount = 1;
            request.Rules.Add(session);
        }

        return request;
    }

    private static DiameterCommandDefinition CreateOutcomeAnswerDefinition(
        uint applicationId,
        uint commandCode)
    {
        DiameterCommandDefinition answer =
            new DiameterCommandDefinition();
        answer.Code = commandCode;
        answer.ApplicationId = applicationId;
        answer.Name = "Application-Outcome-Answer-" + commandCode;
        answer.ProhibitedFlags = DiameterMessageFlags.Request;
        DiameterAvpRuleDefinition resultCode =
            new DiameterAvpRuleDefinition();
        resultCode.AvpCode = DiameterAvpCodes.ResultCode;
        resultCode.AvpName = "Result-Code";
        resultCode.MinimumCount = 1;
        resultCode.MaximumCount = 1;
        answer.Rules.Add(resultCode);
        return answer;
    }

    private static DiameterApplicationHost CreateHost(
        DiameterDictionary dictionary)
    {
        return new DiameterApplicationHost(
            dictionary,
            new DiameterValidator(),
            new TestDiameterClock(
                DateTimeOffset.FromUnixTimeSeconds(1000),
                5000L),
            new DiameterLimitConfiguration(),
            CreateIdentity("local.example"),
            CreateIdentity("example"));
    }

    private static DiameterMessage CreateRequest(bool includeSession)
    {
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.CommandCode = 300U;
        request.ApplicationId = 16777216U;
        request.HopByHopIdentifier = 10U;
        request.EndToEndIdentifier = 20U;
        if (includeSession)
        {
            request.Avps.Add(
                new DiameterBaseAvpFactory().CreateSessionIdAvp(
                    "peer.example;1"));
        }

        return request;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static uint ReadResultCode(DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == DiameterAvpCodes.ResultCode &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                return NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        Assert.Fail("Result-Code was not present.");
        return 0U;
    }

    private static DiameterDictionaryModule CreateDictionaryModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "host-test-dictionary";
        module.Version = "1";
        module.Dependencies.Add("rfc6733");
        DiameterApplicationDefinition application =
            new DiameterApplicationDefinition();
        application.Id = 16777216U;
        application.Name = "Host-Test";
        module.Applications.Add(application);

        DiameterCommandDefinition request =
            new DiameterCommandDefinition();
        request.Code = 300U;
        request.ApplicationId = application.Id;
        request.Name = "Host-Test-Request";
        request.IsRequest = true;
        request.RequiredFlags = DiameterMessageFlags.Request;
        request.ProhibitedFlags = DiameterMessageFlags.Error;
        DiameterAvpRuleDefinition session =
            new DiameterAvpRuleDefinition();
        session.AvpCode = DiameterAvpCodes.SessionId;
        session.AvpName = "Session-Id";
        session.MinimumCount = 1;
        session.MaximumCount = 1;
        request.Rules.Add(session);
        module.Commands.Add(request);

        DiameterCommandDefinition answer =
            new DiameterCommandDefinition();
        answer.Code = 300U;
        answer.ApplicationId = application.Id;
        answer.Name = "Host-Test-Answer";
        answer.ProhibitedFlags = DiameterMessageFlags.Request;
        DiameterAvpRuleDefinition resultCode =
            new DiameterAvpRuleDefinition();
        resultCode.AvpCode = DiameterAvpCodes.ResultCode;
        resultCode.AvpName = "Result-Code";
        resultCode.MinimumCount = 1;
        resultCode.MaximumCount = 1;
        answer.Rules.Add(resultCode);
        module.Commands.Add(answer);
        return module;
    }

    private class HostApplication : IDiameterApplication
    {
        internal HostHandler Handler;
        internal List<string> Log;
        internal bool Validated;
        internal List<IMessageInterceptor> Interceptors =
            new List<IMessageInterceptor>();
        internal int StartCount;
        internal int DrainCount;
        internal int StopCount;

        internal HostApplication(
            HostHandler handler,
            List<string> log,
            bool validated)
        {
            Handler = handler;
            Log = log;
            Validated = validated;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = "host-test";
            registration.ApplicationId = 16777216U;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 300U;
            command.Name = "Host-Test-Request";
            command.RequireValidation = Validated;
            registration.Commands.Add(command);
            registration.DictionaryModules.Add(
                CreateDictionaryModule());
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return commandCode == 300U ? Handler : null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>(Interceptors);
        }

        public void StartDiameterApplication()
        {
            StartCount = StartCount + 1;
            lock (Log)
            {
                Log.Add("start");
            }
        }

        public void DrainDiameterApplication()
        {
            DrainCount = DrainCount + 1;
            lock (Log)
            {
                Log.Add("drain");
            }
        }

        public void StopDiameterApplication()
        {
            StopCount = StopCount + 1;
            lock (Log)
            {
                Log.Add("stop");
            }
        }
    }

    private class HostHandler : IDiameterRequestHandler
    {
        internal string Mode;
        internal List<string> Log;
        internal int CallCount;
        internal DiameterRequestContext? LastContext;

        internal HostHandler(string mode, List<string> log)
        {
            Mode = mode;
            Log = log;
        }

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            CallCount = CallCount + 1;
            LastContext = context;
            lock (Log)
            {
                Log.Add("handler");
            }

            if (Mode == "throw")
            {
                throw new InvalidOperationException("handler failed");
            }

            DiameterMessage answer = new DiameterMessage();
            answer.Flags = context.Request.Flags &
                DiameterMessageFlags.Proxyable;
            answer.CommandCode = context.Request.CommandCode;
            answer.ApplicationId = context.Request.ApplicationId;
            answer.HopByHopIdentifier =
                context.Request.HopByHopIdentifier;
            answer.EndToEndIdentifier =
                context.Request.EndToEndIdentifier;
            DiameterAvp resultCode = new DiameterAvp();
            resultCode.Code = DiameterAvpCodes.ResultCode;
            resultCode.Flags = DiameterAvpFlags.Mandatory;
            resultCode.Data = new byte[4];
            NetworkByteOrder.WriteUInt32(
                resultCode.Data,
                DiameterResultCodes.Success);
            answer.Avps.Add(resultCode);
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Answer = true;
            result.OutputMessage = answer;
            result.Code = "handled";
            return result;
        }
    }

    private class HostInterceptor : IMessageInterceptor
    {
        internal string Name;
        internal int Order;
        internal string Mode;
        internal List<string> Log;

        internal HostInterceptor(
            string name,
            int order,
            string mode,
            List<string> log)
        {
            Name = name;
            Order = order;
            Mode = mode;
            Log = log;
        }

        public string ReadDiameterMessageInterceptorName()
        {
            return Name;
        }

        public int ReadDiameterMessageInterceptorOrder()
        {
            return Order;
        }

        public DiameterApplicationResult InterceptInboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            lock (Log)
            {
                Log.Add("inbound:" + Name);
            }

            if (Mode == "drop")
            {
                DiameterApplicationResult drop =
                    new DiameterApplicationResult();
                drop.Continue = false;
                drop.Drop = true;
                drop.Code = "interceptor-drop";
                return drop;
            }

            if (Mode == "replace" || Mode == "invalid-replace")
            {
                DiameterApplicationResult replacement =
                    new DiameterApplicationResult();
                replacement.Continue = false;
                replacement.Replace = true;
                replacement.OutputMessage = context.Message;
                if (Mode == "invalid-replace")
                {
                    replacement.OutputMessage.ApplicationId = 1U;
                }

                return replacement;
            }

            if (Mode == "redirect")
            {
                DiameterApplicationResult redirect =
                    new DiameterApplicationResult();
                redirect.Continue = false;
                redirect.Redirect = true;
                redirect.Code = "interceptor-redirect";
                DiameterUri uri = new DiameterUri();
                uri.Host.Value = "redirect.example";
                redirect.RedirectHosts.Add(uri);
                return redirect;
            }

            return new DiameterApplicationResult();
        }

        public DiameterApplicationResult InterceptOutboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            lock (Log)
            {
                Log.Add("outbound:" + Name);
            }

            if (Mode == "outbound-drop")
            {
                DiameterApplicationResult drop =
                    new DiameterApplicationResult();
                drop.Continue = false;
                drop.Drop = true;
                return drop;
            }

            if (Mode == "outbound-replace")
            {
                DiameterApplicationResult replacement =
                    new DiameterApplicationResult();
                replacement.Continue = false;
                replacement.Replace = true;
                replacement.OutputMessage = context.Message;
                replacement.Code = "outbound-replaced";
                return replacement;
            }

            return new DiameterApplicationResult();
        }
    }

    private class HostDispatchCompletion :
        IApplicationDispatchCompletionHandler
    {
        internal CountdownEvent Completed;
        internal List<string> Log;
        internal DiameterApplicationResult? Result;

        internal HostDispatchCompletion(
            CountdownEvent completed,
            List<string> log)
        {
            Completed = completed;
            Log = log;
        }

        public void CompleteDiameterApplicationDispatch(
            DiameterApplicationResult result)
        {
            Result = result;
            lock (Log)
            {
                Log.Add("callback");
            }

            Completed.Signal();
        }
    }

    private class OutcomeApplication : IDiameterApplication
    {
        internal DiameterApplicationRegistration Registration;
        internal IDiameterRequestHandler Handler;
        internal List<IMessageInterceptor> Interceptors =
            new List<IMessageInterceptor>();
        internal ManualResetEventSlim DrainEntered =
            new ManualResetEventSlim(false);
        internal int StartCount;
        internal int DrainCount;
        internal int StopCount;

        internal OutcomeApplication(
            DiameterApplicationRegistration registration,
            IDiameterRequestHandler handler)
        {
            Registration = registration;
            Handler = handler;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            return Registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            int index = 0;
            while (index < Registration.Commands.Count)
            {
                if (Registration.Commands[index].CommandCode == commandCode)
                {
                    return Handler;
                }

                index = index + 1;
            }

            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>(Interceptors);
        }

        public void StartDiameterApplication()
        {
            StartCount = StartCount + 1;
        }

        public void DrainDiameterApplication()
        {
            DrainCount = DrainCount + 1;
            DrainEntered.Set();
        }

        public void StopDiameterApplication()
        {
            StopCount = StopCount + 1;
        }
    }

    private class OutcomeHandler : IDiameterRequestHandler
    {
        internal int CallCount;

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            CallCount = CallCount + 1;
            return CreateOutcomeAnswerResult(context);
        }
    }

    private class BlockingOutcomeHandler : IDiameterRequestHandler
    {
        internal CountdownEvent Entered = new CountdownEvent(1);
        internal ManualResetEventSlim Release =
            new ManualResetEventSlim(false);
        internal int CallCount;

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            int call = Interlocked.Increment(ref CallCount);
            if (call == 1)
            {
                Entered.Signal();
            }

            Release.Wait();
            return CreateOutcomeAnswerResult(context);
        }
    }

    private class AdapterOutcomeHandler : IDiameterRequestHandler
    {
        internal DiameterApplicationAdapter Adapter;
        internal int CallCount;
        internal int RawContextCount;
        internal int ValidatedContextCount;
        internal int KnownSessionCount;
        internal int PreservedUnknownCount;

        internal AdapterOutcomeHandler(IDiameterDictionary dictionary)
        {
            Adapter = new DiameterApplicationAdapter(
                dictionary,
                new DiameterAvpCodec(),
                new DiameterLimitConfiguration());
        }

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            CallCount = CallCount + 1;
            if (context.ValidationResult == null)
            {
                RawContextCount = RawContextCount + 1;
            }
            else
            {
                ValidatedContextCount = ValidatedContextCount + 1;
            }

            DiameterTypedMessage typed = Adapter.DecodeDiameterTypedMessage(
                context.Request,
                context.ReceivedAt);
            int index = 0;
            while (index < typed.Avps.Count)
            {
                DiameterTypedAvp avp = typed.Avps[index];
                if (avp.Known &&
                    avp.RawAvp.Code == DiameterAvpCodes.SessionId)
                {
                    KnownSessionCount = KnownSessionCount + 1;
                }

                if (!avp.Known && avp.PreserveRaw &&
                    avp.RawAvp.Code == 999U &&
                    avp.RawAvp.VendorId == 9999U)
                {
                    PreservedUnknownCount = PreservedUnknownCount + 1;
                }

                index = index + 1;
            }

            return CreateOutcomeAnswerResult(context);
        }
    }

    private class OutcomeCompletion :
        IApplicationDispatchCompletionHandler
    {
        internal CountdownEvent Completed;
        internal DiameterApplicationResult? Result;

        internal OutcomeCompletion(CountdownEvent completed)
        {
            Completed = completed;
        }

        public void CompleteDiameterApplicationDispatch(
            DiameterApplicationResult result)
        {
            Result = result;
            Completed.Signal();
        }
    }

    private class NullRegistrationApplication : IDiameterApplication
    {
        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            return null!;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>();
        }

        public void StartDiameterApplication()
        {
        }

        public void DrainDiameterApplication()
        {
        }

        public void StopDiameterApplication()
        {
        }
    }
}
