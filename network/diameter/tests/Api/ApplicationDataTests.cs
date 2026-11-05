using System;

namespace diameter.Tests;

public class ApplicationDataTests
{
    [Test]
    public void ApplicationDataDefaultsAreSafeAndIndependentlyOwned()
    {
        DiameterApplicationRegistration first =
            new DiameterApplicationRegistration();
        DiameterApplicationRegistration second =
            new DiameterApplicationRegistration();
        DiameterApplicationResult firstResult =
            new DiameterApplicationResult();
        DiameterApplicationResult secondResult =
            new DiameterApplicationResult();
        DiameterRequestContext firstContext =
            new DiameterRequestContext();
        DiameterRequestContext secondContext =
            new DiameterRequestContext();
        DiameterInterceptorContext interceptor =
            new DiameterInterceptorContext();

        first.Commands.Add(new DiameterCommandRegistration());
        first.DictionaryModules.Add(new DiameterDictionaryModule());
        firstResult.RedirectHosts.Add(new DiameterUri());

        Assert.Multiple(() =>
        {
            Assert.That(first.Roles, Is.EqualTo(DiameterNodeRole.Server));
            Assert.That(first.MaximumConcurrency, Is.EqualTo(1));
            Assert.That(first.QueueCapacity, Is.EqualTo(1024));
            Assert.That(first.DrainTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(second.Commands, Is.Empty);
            Assert.That(second.DictionaryModules, Is.Empty);
            Assert.That(firstResult.Continue, Is.True);
            Assert.That(secondResult.RedirectHosts, Is.Empty);
            Assert.That(firstContext.Registration,
                Is.Not.SameAs(secondContext.Registration));
            Assert.That(firstContext.Command,
                Is.Not.SameAs(secondContext.Command));
            Assert.That(firstContext.Request,
                Is.Not.SameAs(secondContext.Request));
            Assert.That(firstContext.PeerIdentity,
                Is.Not.SameAs(secondContext.PeerIdentity));
            Assert.That(interceptor.Message, Is.Not.Null);
            Assert.That(interceptor.RequestContext, Is.Null);
            Assert.That(interceptor.PreviousResult, Is.Null);
        });
    }

    [Test]
    public void CommandRegistrationValidatesCodeAndName()
    {
        DiameterCommandRegistration valid = CreateCommand(300U, true);
        DiameterCommandRegistration missingCode = CreateCommand(0U, true);
        DiameterCommandRegistration excessiveCode = CreateCommand(
            DiameterProtocolConstants.MaximumCommandCode + 1U,
            true);
        DiameterCommandRegistration missingName = CreateCommand(300U, true);
        missingName.Name = " ";
        DiameterCommandRegistration invalidText = CreateCommand(300U, true);
        invalidText.Name = "bad\ud800name";

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateCommandRegistration);
            Assert.Throws<ArgumentOutOfRangeException>(
                missingCode.ValidateCommandRegistration);
            Assert.Throws<ArgumentOutOfRangeException>(
                excessiveCode.ValidateCommandRegistration);
            Assert.Throws<ArgumentException>(
                missingName.ValidateCommandRegistration);
            Assert.Throws<ArgumentException>(
                invalidText.ValidateCommandRegistration);
        });
    }

    [Test]
    public void ApplicationRegistrationValidatesDispatchKeysAndLimits()
    {
        DiameterApplicationRegistration valid = CreateRegistration();
        valid.Roles = DiameterNodeRole.Client | DiameterNodeRole.Server;
        valid.AdvertiseAuthentication = true;
        DiameterCommandRegistration answer = CreateCommand(300U, false);
        valid.Commands.Add(answer);
        DiameterDictionaryModule dictionary = new DiameterDictionaryModule();
        dictionary.Name = "sample-dictionary";
        valid.DictionaryModules.Add(dictionary);

        DiameterApplicationRegistration duplicate = CreateRegistration();
        duplicate.Commands.Add(CreateCommand(300U, true));
        DiameterApplicationRegistration invalidRole = CreateRegistration();
        invalidRole.Roles = DiameterNodeRole.Proxy;
        DiameterApplicationRegistration noCommands = CreateRegistration();
        noCommands.Commands.Clear();
        DiameterApplicationRegistration invalidLimits = CreateRegistration();
        invalidLimits.MaximumConcurrency = 0;
        DiameterApplicationRegistration duplicateDictionaries =
            CreateRegistration();
        DiameterDictionaryModule first = new DiameterDictionaryModule();
        first.Name = "sample";
        DiameterDictionaryModule second = new DiameterDictionaryModule();
        second.Name = "SAMPLE";
        duplicateDictionaries.DictionaryModules.Add(first);
        duplicateDictionaries.DictionaryModules.Add(second);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateApplicationRegistration);
            Assert.Throws<ArgumentException>(
                duplicate.ValidateApplicationRegistration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidRole.ValidateApplicationRegistration);
            Assert.Throws<ArgumentException>(
                noCommands.ValidateApplicationRegistration);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidLimits.ValidateApplicationRegistration);
            Assert.Throws<ArgumentException>(
                duplicateDictionaries.ValidateApplicationRegistration);
        });
    }

    [Test]
    public void RequestContextValidatesDispatchAndValidationState()
    {
        DiameterRequestContext valid = CreateRequestContext(true);
        DiameterRequestContext raw = CreateRequestContext(false);
        raw.ValidationResult = null;

        DiameterRequestContext mismatchedCommand =
            CreateRequestContext(true);
        mismatchedCommand.Request.CommandCode = 301U;
        DiameterRequestContext answer = CreateRequestContext(true);
        answer.Request.Flags = DiameterMessageFlags.None;
        DiameterRequestContext invalidPeer = CreateRequestContext(true);
        invalidPeer.PeerIdentity.Value = "bad peer";
        DiameterRequestContext missingValidation =
            CreateRequestContext(true);
        missingValidation.ValidationResult = null;
        DiameterRequestContext failedValidation =
            CreateRequestContext(true);
        DiameterValidationResult invalidValidation =
            new DiameterValidationResult();
        invalidValidation.Status = DiameterValidationStatus.Invalid;
        failedValidation.ValidationResult = invalidValidation;
        DiameterRequestContext missingTime = CreateRequestContext(true);
        missingTime.ReceivedAt = default(DateTimeOffset);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateRequestContext);
            Assert.DoesNotThrow(raw.ValidateRequestContext);
            Assert.Throws<ArgumentException>(
                mismatchedCommand.ValidateRequestContext);
            Assert.Throws<ArgumentException>(answer.ValidateRequestContext);
            Assert.Throws<ArgumentException>(
                invalidPeer.ValidateRequestContext);
            Assert.Throws<ArgumentException>(
                missingValidation.ValidateRequestContext);
            Assert.Throws<ArgumentException>(
                failedValidation.ValidateRequestContext);
            Assert.Throws<ArgumentException>(
                missingTime.ValidateRequestContext);
        });
    }

    [Test]
    public void ApplicationResultValidatesEveryPipelineOutcome()
    {
        DiameterApplicationResult continuing =
            new DiameterApplicationResult();
        DiameterApplicationResult replacement = CreateResult();
        replacement.Replace = true;
        replacement.OutputMessage = CreateMessage(true);
        DiameterApplicationResult answer = CreateResult();
        answer.Answer = true;
        answer.OutputMessage = CreateMessage(false);
        DiameterApplicationResult drop = CreateResult();
        drop.Drop = true;
        DiameterApplicationResult redirect = CreateResult();
        redirect.Redirect = true;
        redirect.RedirectHosts.Add(CreateRedirect());
        DiameterApplicationResult failure = CreateResult();
        failure.Fail = true;
        failure.ResultCode = DiameterResultCodes.UnableToComply;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(continuing.ValidateApplicationResult);
            Assert.DoesNotThrow(replacement.ValidateApplicationResult);
            Assert.DoesNotThrow(answer.ValidateApplicationResult);
            Assert.DoesNotThrow(drop.ValidateApplicationResult);
            Assert.DoesNotThrow(redirect.ValidateApplicationResult);
            Assert.DoesNotThrow(failure.ValidateApplicationResult);
        });
    }

    [Test]
    public void ApplicationResultRejectsAmbiguousOrIncompleteOutcomes()
    {
        DiameterApplicationResult ambiguous = new DiameterApplicationResult();
        ambiguous.Drop = true;
        DiameterApplicationResult missingReplacement = CreateResult();
        missingReplacement.Replace = true;
        DiameterApplicationResult requestAnswer = CreateResult();
        requestAnswer.Answer = true;
        requestAnswer.OutputMessage = CreateMessage(true);
        DiameterApplicationResult missingRedirect = CreateResult();
        missingRedirect.Redirect = true;
        DiameterApplicationResult unexpectedRedirect =
            new DiameterApplicationResult();
        unexpectedRedirect.RedirectHosts.Add(CreateRedirect());
        DiameterApplicationResult missingFailureCode = CreateResult();
        missingFailureCode.Fail = true;
        DiameterApplicationResult invalidFailureCode = CreateResult();
        invalidFailureCode.Fail = true;
        invalidFailureCode.ResultCode = 999U;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                ambiguous.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                missingReplacement.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                requestAnswer.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                missingRedirect.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                unexpectedRedirect.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                missingFailureCode.ValidateApplicationResult);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidFailureCode.ValidateApplicationResult);
        });
    }

    [Test]
    public void InterceptorContextValidatesMessageAndCorrelation()
    {
        DiameterInterceptorContext inbound = CreateInterceptorContext();
        inbound.Inbound = true;
        inbound.RequestContext = CreateRequestContext(false);
        inbound.PeerIdentity = CreateIdentity("peer.example");
        inbound.RouteIdentifier = "local-application";
        inbound.SessionId = "peer.example;1";
        inbound.Duplicate = true;
        inbound.Retransmission = true;

        DiameterInterceptorContext outbound = CreateInterceptorContext();
        outbound.Inbound = false;
        outbound.PeerIdentity = null;
        DiameterInterceptorContext invalidPeer = CreateInterceptorContext();
        invalidPeer.PeerIdentity = CreateIdentity("bad peer");
        DiameterInterceptorContext mismatched = CreateInterceptorContext();
        mismatched.RequestContext = CreateRequestContext(false);
        mismatched.Message.CommandCode = 301U;
        DiameterInterceptorContext missingTime = CreateInterceptorContext();
        missingTime.InvokedAt = default(DateTimeOffset);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(inbound.ValidateInterceptorContext);
            Assert.DoesNotThrow(outbound.ValidateInterceptorContext);
            Assert.Throws<ArgumentException>(
                invalidPeer.ValidateInterceptorContext);
            Assert.Throws<ArgumentException>(
                mismatched.ValidateInterceptorContext);
            Assert.Throws<ArgumentException>(
                missingTime.ValidateInterceptorContext);
        });
    }

    [Test]
    public void ApplicationRegistrationRejectsEveryOwnedCollectionShape()
    {
        AssertEveryMutationIsRejected(
            CreateRegistration,
            value => value.ValidateApplicationRegistration(),
            value => value.Name = null!,
            value => value.Roles = DiameterNodeRole.None,
            value => value.Commands = null!,
            value => value.DictionaryModules = null!,
            value => value.Commands[0] = null!,
            value => value.Commands.Add(null!),
            value => value.DictionaryModules.Add(null!),
            value =>
            {
                DiameterDictionaryModule module =
                    new DiameterDictionaryModule();
                module.Name = " ";
                value.DictionaryModules.Add(module);
            },
            value =>
            {
                DiameterDictionaryModule module =
                    new DiameterDictionaryModule();
                module.Name = "bad\ud800module";
                value.DictionaryModules.Add(module);
            },
            value =>
            {
                DiameterDictionaryModule module =
                    new DiameterDictionaryModule();
                module.Name = "valid";
                value.DictionaryModules.Add(module);
                value.DictionaryModules.Add(null!);
            });

        DiameterApplicationRegistration distinctDictionaries =
            CreateRegistration();
        DiameterDictionaryModule first = new DiameterDictionaryModule();
        first.Name = "first";
        DiameterDictionaryModule second = new DiameterDictionaryModule();
        second.Name = "second";
        distinctDictionaries.DictionaryModules.Add(first);
        distinctDictionaries.DictionaryModules.Add(second);

        Assert.DoesNotThrow(
            distinctDictionaries.ValidateApplicationRegistration);
    }

    [Test]
    public void RequestContextRejectsEveryMissingDispatchDependency()
    {
        AssertEveryMutationIsRejected(
            () => CreateRequestContext(false),
            value => value.ValidateRequestContext(),
            value => value.Registration = null!,
            value => value.Command = null!,
            value => value.Request = null!,
            value => value.Request.Avps = null!,
            value => value.PeerIdentity = null!);
    }

    [Test]
    public void ApplicationResultRejectsEveryInvalidOwnedResultValue()
    {
        AssertEveryMutationIsRejected(
            CreateFailureResult,
            value => value.ValidateApplicationResult(),
            value => value.RedirectHosts = null!,
            value => value.Code = null!,
            value => value.Description = null!,
            value => value.Code = "bad\ud800code",
            value => value.Description = "bad\ud800description");

        AssertEveryMutationIsRejected(
            CreateReplacementResult,
            value => value.ValidateApplicationResult(),
            value => value.OutputMessage = null,
            value => value.OutputMessage!.Avps = null!);

        AssertEveryMutationIsRejected(
            CreateAnswerResult,
            value => value.ValidateApplicationResult(),
            value => value.OutputMessage = null,
            value => value.OutputMessage!.Avps = null!);

        DiameterApplicationResult unexpectedMessage = CreateResult();
        unexpectedMessage.Drop = true;
        unexpectedMessage.OutputMessage = CreateMessage(false);

        DiameterApplicationResult nullRedirect = CreateRedirectResult();
        nullRedirect.RedirectHosts[0] = null!;
        DiameterApplicationResult invalidRedirect = CreateRedirectResult();
        invalidRedirect.RedirectHosts[0].Host.Value = "bad host";
        DiameterApplicationResult wrongProtocol = CreateRedirectResult();
        wrongProtocol.RedirectHosts[0].Protocol = "aaa";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                unexpectedMessage.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                nullRedirect.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                invalidRedirect.ValidateApplicationResult);
            Assert.Throws<ArgumentException>(
                wrongProtocol.ValidateApplicationResult);
        });
    }

    [Test]
    public void InterceptorContextRejectsEveryMessageAndCorrelationShape()
    {
        AssertEveryMutationIsRejected(
            CreateInterceptorContext,
            value => value.ValidateInterceptorContext(),
            value => value.Message = null!,
            value => value.Message.Avps = null!,
            value => value.Message.Version = 2,
            value => value.Message.CommandCode = 0U,
            value => value.Message.CommandCode =
                DiameterProtocolConstants.MaximumCommandCode + 1U,
            value => value.RouteIdentifier = null!,
            value => value.SessionId = null!,
            value => value.RouteIdentifier = "bad\ud800route",
            value => value.SessionId = "bad\ud800session");
    }

    private static DiameterApplicationRegistration CreateRegistration()
    {
        DiameterApplicationRegistration registration =
            new DiameterApplicationRegistration();
        registration.Name = "sample-application";
        registration.ApplicationId = 16777216U;
        registration.VendorId = 10415U;
        registration.Commands.Add(CreateCommand(300U, true));
        return registration;
    }

    private static DiameterCommandRegistration CreateCommand(
        uint commandCode,
        bool isRequest)
    {
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = commandCode;
        command.Name = "Sample-Command";
        command.IsRequest = isRequest;
        return command;
    }

    private static DiameterRequestContext CreateRequestContext(
        bool requireValidation)
    {
        DiameterRequestContext context = new DiameterRequestContext();
        context.Registration = CreateRegistration();
        context.Command = context.Registration.Commands[0];
        context.Command.RequireValidation = requireValidation;
        context.Request = CreateMessage(true);
        context.Request.ApplicationId = context.Registration.ApplicationId;
        context.PeerIdentity = CreateIdentity("peer.example");
        context.RouteIdentifier = "local-application";
        context.SessionId = "peer.example;1";
        context.ReceivedAt = DateTimeOffset.FromUnixTimeSeconds(1000);
        context.ValidationResult = new DiameterValidationResult();
        context.ValidationResult.Status = DiameterValidationStatus.Valid;
        return context;
    }

    private static DiameterApplicationResult CreateResult()
    {
        DiameterApplicationResult result = new DiameterApplicationResult();
        result.Continue = false;
        return result;
    }

    private static DiameterApplicationResult CreateFailureResult()
    {
        DiameterApplicationResult result = CreateResult();
        result.Fail = true;
        result.ResultCode = DiameterResultCodes.UnableToComply;
        return result;
    }

    private static DiameterApplicationResult CreateReplacementResult()
    {
        DiameterApplicationResult result = CreateResult();
        result.Replace = true;
        result.OutputMessage = CreateMessage(true);
        return result;
    }

    private static DiameterApplicationResult CreateAnswerResult()
    {
        DiameterApplicationResult result = CreateResult();
        result.Answer = true;
        result.OutputMessage = CreateMessage(false);
        return result;
    }

    private static DiameterApplicationResult CreateRedirectResult()
    {
        DiameterApplicationResult result = CreateResult();
        result.Redirect = true;
        result.RedirectHosts.Add(CreateRedirect());
        return result;
    }

    private static DiameterInterceptorContext CreateInterceptorContext()
    {
        DiameterInterceptorContext context =
            new DiameterInterceptorContext();
        context.Message = CreateMessage(true);
        context.Message.ApplicationId = 16777216U;
        context.InvokedAt = DateTimeOffset.FromUnixTimeSeconds(1000);
        return context;
    }

    private static DiameterMessage CreateMessage(bool isRequest)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = isRequest ?
            DiameterMessageFlags.Request : DiameterMessageFlags.None;
        message.CommandCode = 300U;
        message.ApplicationId = 16777216U;
        message.HopByHopIdentifier = 10U;
        message.EndToEndIdentifier = 20U;
        return message;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static DiameterUri CreateRedirect()
    {
        DiameterUri redirect = new DiameterUri();
        redirect.Host = CreateIdentity("redirect.example");
        return redirect;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }
}
