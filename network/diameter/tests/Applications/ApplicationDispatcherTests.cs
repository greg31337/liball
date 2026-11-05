using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class ApplicationDispatcherTests
{
    [Test]
    public void RawDispatchBuildsOwnedContextAndAcceptsCorrelatedAnswer()
    {
        DispatcherFixture fixture = CreateFixture(false, true);
        fixture.Handler.Mode = "raw-answer";
        DiameterMessage request = CreateRequest(true);
        request.Flags = request.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;
        request.Avps.Add(CreateUnknownAvp());
        DiameterIdentity peer = CreateIdentity("peer.example");

        DiameterApplicationResult result =
            fixture.Dispatcher.DispatchInboundApplicationRequest(
                request,
                peer,
                "local-route",
                true,
                true,
                true);
        request.Avps[0].Data[0] = 99;
        peer.Value = "changed.example";
        fixture.Handler.LastAnswer!.CommandCode = 999U;

        DiameterRequestContext context = fixture.Handler.LastContext!;
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Handler.CallCount, Is.EqualTo(1));
            Assert.That(context.ValidationResult, Is.Null);
            Assert.That(context.PeerIdentity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(context.RouteIdentifier, Is.EqualTo("local-route"));
            Assert.That(context.SessionId, Is.EqualTo("peer.example;1"));
            Assert.That(context.ReceivedAt,
                Is.EqualTo(fixture.ReceivedAt));
            Assert.That(context.Duplicate, Is.True);
            Assert.That(context.Retransmission, Is.True);
            Assert.That(context.Cancelled, Is.True);
            Assert.That(context.ShuttingDown, Is.True);
            Assert.That(context.Request.Avps[0].Data[0], Is.Not.EqualTo(99));
            Assert.That(result.Answer, Is.True);
            Assert.That(result.OutputMessage!.CommandCode, Is.EqualTo(300U));
            Assert.That(result.OutputMessage.Avps, Is.Empty);
            Assert.That(result.RequestContext, Is.SameAs(context));
        });
    }

    [Test]
    public void ValidatedDispatchSuppliesSuccessfulValidation()
    {
        DispatcherFixture fixture = CreateFixture(true, true);
        fixture.Handler.Mode = "answer";
        DiameterMessage request = CreateRequest(true);

        DiameterApplicationResult result =
            fixture.Dispatcher.DispatchInboundApplicationRequest(
                request,
                CreateIdentity("peer.example"),
                "local-route",
                false,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Handler.CallCount, Is.EqualTo(1));
            Assert.That(fixture.Handler.LastContext!.ValidationResult,
                Is.Not.Null);
            Assert.That(
                fixture.Handler.LastContext.ValidationResult!.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Answer, Is.True);
            Assert.That(result.Code, Is.EqualTo("handled"));
            Assert.That(ReadResultCode(result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.Success));
        });
    }

    [Test]
    public void ValidationFailureReturnsProtocolAnswerWithoutCallingHandler()
    {
        DispatcherFixture fixture = CreateFixture(true, true);
        DiameterMessage request = CreateRequest(false);

        DiameterApplicationResult result =
            fixture.Dispatcher.DispatchInboundApplicationRequest(
                request,
                CreateIdentity("peer.example"),
                "local-route",
                false,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Handler.CallCount, Is.Zero);
            Assert.That(result.Answer, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("application-request-validation-failed"));
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(ReadResultCode(result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(result.OutputMessage!.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(result.OutputMessage.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
        });
    }

    [Test]
    public void UnsupportedApplicationAndCommandUseDistinctProtocolErrors()
    {
        DispatcherFixture fixture = CreateFixture(false, true);
        DiameterMessage unsupportedApplication = CreateRequest(true);
        unsupportedApplication.ApplicationId = 999U;
        DiameterMessage unsupportedCommand = CreateRequest(true);
        unsupportedCommand.CommandCode = 301U;

        DiameterApplicationResult applicationResult =
            fixture.Dispatcher.DispatchInboundApplicationRequest(
                unsupportedApplication,
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false);
        DiameterApplicationResult commandResult =
            fixture.Dispatcher.DispatchInboundApplicationRequest(
                unsupportedCommand,
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(applicationResult.Code,
                Is.EqualTo("application-unsupported"));
            Assert.That(ReadResultCode(applicationResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.ApplicationUnsupported));
            Assert.That(commandResult.Code,
                Is.EqualTo("application-command-unsupported"));
            Assert.That(ReadResultCode(commandResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.CommandUnsupported));
            Assert.That(
                applicationResult.OutputMessage!.Flags &
                DiameterMessageFlags.Error,
                Is.EqualTo(DiameterMessageFlags.Error));
            Assert.That(fixture.Handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public void InvalidLocalAnswersFallBackToUnableToComply()
    {
        DispatcherFixture mismatched = CreateFixture(true, true);
        mismatched.Handler.Mode = "mismatched-answer";
        DiameterMessage firstRequest = CreateRequest(true);
        DispatcherFixture invalidDictionary = CreateFixture(true, true);
        invalidDictionary.Handler.Mode = "raw-answer";
        DiameterMessage secondRequest = CreateRequest(true);
        DispatcherFixture retransmitted = CreateFixture(false, true);
        retransmitted.Handler.Mode = "retransmitted-answer";
        DiameterMessage thirdRequest = CreateRequest(true);

        DiameterApplicationResult mismatchedResult =
            mismatched.Dispatcher.DispatchInboundApplicationRequest(
                firstRequest,
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false);
        DiameterApplicationResult dictionaryResult =
            invalidDictionary.Dispatcher.DispatchInboundApplicationRequest(
                secondRequest,
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false);
        DiameterApplicationResult retransmittedResult =
            retransmitted.Dispatcher.DispatchInboundApplicationRequest(
                thirdRequest,
                CreateIdentity("peer.example"),
                string.Empty,
                false,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(mismatchedResult.Code,
                Is.EqualTo("application-answer-invalid"));
            Assert.That(ReadResultCode(mismatchedResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(mismatchedResult.OutputMessage!.HopByHopIdentifier,
                Is.EqualTo(firstRequest.HopByHopIdentifier));
            Assert.That(dictionaryResult.Code,
                Is.EqualTo("application-answer-invalid"));
            Assert.That(ReadResultCode(dictionaryResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(mismatchedResult.RequestContext, Is.Not.Null);
            Assert.That(dictionaryResult.RequestContext, Is.Not.Null);
            Assert.That(retransmittedResult.Code,
                Is.EqualTo("application-answer-invalid"));
            Assert.That(ReadResultCode(retransmittedResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
        });
    }

    [Test]
    public void HandlerFailureDropAndInvalidOutcomeAreNormalized()
    {
        DispatcherFixture failure = CreateFixture(false, true);
        failure.Handler.Mode = "failure";
        DispatcherFixture drop = CreateFixture(false, true);
        drop.Handler.Mode = "drop";
        DispatcherFixture invalid = CreateFixture(false, true);
        invalid.Handler.Mode = "continue";

        DiameterApplicationResult failureResult =
            Dispatch(failure, CreateRequest(true));
        DiameterApplicationResult dropResult =
            Dispatch(drop, CreateRequest(true));
        DiameterApplicationResult invalidResult =
            Dispatch(invalid, CreateRequest(true));

        Assert.Multiple(() =>
        {
            Assert.That(failureResult.Answer, Is.True);
            Assert.That(ReadResultCode(failureResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(dropResult.Drop, Is.True);
            Assert.That(dropResult.OutputMessage, Is.Null);
            Assert.That(invalidResult.Code,
                Is.EqualTo("application-answer-invalid"));
            Assert.That(ReadResultCode(invalidResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
        });
    }

    [Test]
    public void MissingHandlerAndUnsafeMessagesUseSafeFallbacks()
    {
        DispatcherFixture missing = CreateFixture(false, false);
        DiameterApplicationResult missingResult =
            Dispatch(missing, CreateRequest(true));
        DiameterMessage answer = CreateRequest(true);
        answer.Flags = DiameterMessageFlags.None;
        DiameterApplicationResult answerResult = Dispatch(missing, answer);
        DiameterMessage invalidOwnership = CreateRequest(true);
        invalidOwnership.Avps[0].Data = null!;
        DiameterApplicationResult ownershipResult =
            Dispatch(missing, invalidOwnership);
        DiameterMessage reservedBits = CreateRequest(true);
        reservedBits.Flags = reservedBits.Flags |
            (DiameterMessageFlags)1;
        DiameterApplicationResult reservedResult =
            Dispatch(missing, reservedBits);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Code,
                Is.EqualTo("application-handler-missing"));
            Assert.That(ReadResultCode(missingResult.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(missingResult.RequestContext, Is.Not.Null);
            Assert.That(answerResult.Drop, Is.True);
            Assert.That(answerResult.OutputMessage, Is.Null);
            Assert.That(ownershipResult.Drop, Is.True);
            Assert.That(ownershipResult.OutputMessage, Is.Null);
            Assert.That(reservedResult.Drop, Is.True);
            Assert.That(reservedResult.OutputMessage, Is.Null);
        });
    }

    private static DiameterApplicationResult Dispatch(
        DispatcherFixture fixture,
        DiameterMessage request)
    {
        return fixture.Dispatcher.DispatchInboundApplicationRequest(
            request,
            CreateIdentity("peer.example"),
            string.Empty,
            false,
            false,
            false);
    }

    private static DispatcherFixture CreateFixture(
        bool validated,
        bool handlerAvailable)
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule module = CreateApplicationDictionary();
        DiameterDictionaryRegistrationResult dictionaryResult =
            dictionary.RegisterDictionaryModule(module);
        if (dictionaryResult.Status !=
            DiameterDictionaryRegistrationStatus.Success)
        {
            throw new InvalidOperationException(dictionaryResult.Message);
        }

        RecordingApplicationHandler handler =
            new RecordingApplicationHandler();
        DispatchApplication application =
            new DispatchApplication(handler, handlerAvailable);
        application.Registration.Commands[0].RequireValidation = validated;
        application.Registration.DictionaryModules.Add(module);
        ApplicationRegistry registry = new ApplicationRegistry();
        registry.RegisterApplicationModule(application);
        DateTimeOffset receivedAt = DateTimeOffset.FromUnixTimeSeconds(1000);
        TestDiameterClock clock = new TestDiameterClock(receivedAt, 5000L);
        ApplicationDispatcher dispatcher = new ApplicationDispatcher(
            registry,
            dictionary,
            new DiameterValidator(),
            clock,
            new DiameterLimitConfiguration(),
            CreateIdentity("local.example"),
            CreateIdentity("example"));
        return new DispatcherFixture(
            dispatcher,
            handler,
            receivedAt);
    }

    private static DiameterDictionaryModule CreateApplicationDictionary()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "test-application";
        module.Version = "1.0";
        module.Dependencies.Add("rfc6733");

        DiameterApplicationDefinition application =
            new DiameterApplicationDefinition();
        application.Id = 16777216U;
        application.Name = "Test-Application";
        module.Applications.Add(application);

        DiameterCommandDefinition request = new DiameterCommandDefinition();
        request.Code = 300U;
        request.ApplicationId = application.Id;
        request.Name = "Test-Request";
        request.IsRequest = true;
        request.RequiredFlags = DiameterMessageFlags.Request;
        request.ProhibitedFlags = DiameterMessageFlags.Error;
        request.Rules.Add(CreateRule(
            DiameterAvpCodes.SessionId,
            "Session-Id"));
        module.Commands.Add(request);

        DiameterCommandDefinition answer = new DiameterCommandDefinition();
        answer.Code = 300U;
        answer.ApplicationId = application.Id;
        answer.Name = "Test-Answer";
        answer.IsRequest = false;
        answer.ProhibitedFlags = DiameterMessageFlags.Request;
        answer.Rules.Add(CreateRule(
            DiameterAvpCodes.ResultCode,
            "Result-Code"));
        module.Commands.Add(answer);
        return module;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint code,
        string name)
    {
        DiameterAvpRuleDefinition rule =
            new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpName = name;
        rule.MinimumCount = 1;
        rule.MaximumCount = 1;
        return rule;
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

    private static DiameterAvp CreateUnknownAvp()
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 999999U;
        avp.Data = new byte[] { 1, 2, 3 };
        return avp;
    }

    private static DiameterAvp CreateResultCodeAvp(uint resultCode)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.ResultCode;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, resultCode);
        return avp;
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

        Assert.Fail("The answer did not contain Result-Code.");
        return 0U;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private sealed class DispatcherFixture
    {
        public ApplicationDispatcher Dispatcher;
        public RecordingApplicationHandler Handler;
        public DateTimeOffset ReceivedAt;

        public DispatcherFixture(
            ApplicationDispatcher dispatcher,
            RecordingApplicationHandler handler,
            DateTimeOffset receivedAt)
        {
            Dispatcher = dispatcher;
            Handler = handler;
            ReceivedAt = receivedAt;
        }
    }

    private sealed class DispatchApplication : IDiameterApplication
    {
        public DiameterApplicationRegistration Registration =
            new DiameterApplicationRegistration();
        public RecordingApplicationHandler Handler;
        public bool HandlerAvailable;

        public DispatchApplication(
            RecordingApplicationHandler handler,
            bool handlerAvailable)
        {
            Handler = handler;
            HandlerAvailable = handlerAvailable;
            Registration.Name = "dispatch-application";
            Registration.ApplicationId = 16777216U;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 300U;
            command.Name = "Test-Request";
            Registration.Commands.Add(command);
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            return Registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            if (HandlerAvailable && commandCode == 300U)
            {
                return Handler;
            }

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

    private sealed class RecordingApplicationHandler : IDiameterRequestHandler
    {
        public string Mode = "answer";
        public int CallCount;
        public DiameterRequestContext? LastContext;
        public DiameterMessage? LastAnswer;

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            CallCount = CallCount + 1;
            LastContext = context;
            if (Mode == "drop")
            {
                DiameterApplicationResult drop =
                    new DiameterApplicationResult();
                drop.Continue = false;
                drop.Drop = true;
                drop.Code = "dropped";
                return drop;
            }

            if (Mode == "failure")
            {
                DiameterApplicationResult failure =
                    new DiameterApplicationResult();
                failure.Continue = false;
                failure.Fail = true;
                failure.ResultCode = DiameterResultCodes.InvalidAvpValue;
                failure.Code = "application-failure";
                failure.Description = "The application rejected an AVP.";
                return failure;
            }

            if (Mode == "continue")
            {
                return new DiameterApplicationResult();
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
            if (Mode != "raw-answer")
            {
                answer.Avps.Add(CreateResultCodeAvp(
                    DiameterResultCodes.Success));
            }

            if (Mode == "mismatched-answer")
            {
                answer.HopByHopIdentifier = 999U;
            }

            if (Mode == "retransmitted-answer")
            {
                answer.Flags = answer.Flags |
                    DiameterMessageFlags.PotentiallyRetransmitted;
            }

            LastAnswer = answer;
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Answer = true;
            result.OutputMessage = answer;
            result.Code = "handled";
            return result;
        }
    }
}
