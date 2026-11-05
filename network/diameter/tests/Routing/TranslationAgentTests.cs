using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class TranslationAgentTests
{
    [Test]
    public void TranslationRouteDispatchesPairAndPreservesSourceSemantics()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        handler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        RouteSelector selector = CreateTranslationSelector(handler, true);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.Rejected, Is.False);
            Assert.That(decision.Action,
                Is.EqualTo(DiameterRouteAction.Translate));
            Assert.That(decision.AgentRole,
                Is.EqualTo(DiameterAgentRole.Translation));
            Assert.That(decision.Code, Is.EqualTo("request-translated"));
            Assert.That(decision.Request.ApplicationId, Is.EqualTo(4U));
            Assert.That(decision.Request.Message.ApplicationId, Is.EqualTo(4U));
            Assert.That(decision.RoutedMessage!.ApplicationId, Is.EqualTo(9U));
            Assert.That(decision.RoutedMessage.CommandCode, Is.EqualTo(301U));
            Assert.That(decision.RoutedMessage.HopByHopIdentifier,
                Is.EqualTo(request.Message.HopByHopIdentifier));
            Assert.That(decision.RoutedMessage.EndToEndIdentifier,
                Is.EqualTo(request.Message.EndToEndIdentifier));
            Assert.That(decision.TranslationResult!.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Translated));
            Assert.That(decision.TranslationResult.SourceApplicationId,
                Is.EqualTo(4U));
            Assert.That(decision.TranslationResult.TargetApplicationId,
                Is.EqualTo(9U));
            Assert.That(decision.TranslationResult.TranslationState,
                Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(handler.ObservedRequest, Is.Not.SameAs(request));
        });

        handler.ObservedRequest!.Message.ApplicationId = 99U;
        handler.RequestResult.TranslatedMessage!.ApplicationId = 88U;
        handler.RequestResult.TranslationState[0] = 0;
        Assert.Multiple(() =>
        {
            Assert.That(decision.Request.Message.ApplicationId, Is.EqualTo(4U));
            Assert.That(decision.RoutedMessage!.ApplicationId, Is.EqualTo(9U));
            Assert.That(decision.TranslationResult!.TranslationState[0],
                Is.EqualTo(7));
        });
    }

    [Test]
    public void TranslatedAnswerRestoresOriginalApplicationAndIdentifiers()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        handler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        DiameterMessage targetAnswer = CreateTargetAnswer(
            decision.RoutedMessage!);
        handler.AnswerResult = CreateTranslatedAnswerResult(
            request.Message,
            targetAnswer);

        DiameterTranslationResult result =
            selector.Translations.TranslateDispatchedAnswer(
                decision.TranslationResult,
                targetAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Translated));
            Assert.That(result.SourceMessage, Is.Not.SameAs(targetAnswer));
            Assert.That(result.SourceMessage!.ApplicationId, Is.EqualTo(9U));
            Assert.That(result.TranslatedMessage!.ApplicationId, Is.EqualTo(4U));
            Assert.That(result.TranslatedMessage.CommandCode, Is.EqualTo(300U));
            Assert.That(result.TranslatedMessage.HopByHopIdentifier,
                Is.EqualTo(request.Message.HopByHopIdentifier));
            Assert.That(result.TranslatedMessage.EndToEndIdentifier,
                Is.EqualTo(request.Message.EndToEndIdentifier));
            Assert.That((result.TranslatedMessage.Flags &
                DiameterMessageFlags.Request), Is.EqualTo(
                DiameterMessageFlags.None));
            Assert.That(handler.AnswerCount, Is.EqualTo(1));
            Assert.That(handler.ObservedRequestTranslation,
                Is.Not.SameAs(decision.TranslationResult));
            Assert.That(handler.ObservedAnswer, Is.Not.SameAs(targetAnswer));
        });

        handler.ObservedAnswer!.ApplicationId = 77U;
        handler.AnswerResult.TranslatedMessage!.ApplicationId = 66U;
        Assert.That(result.SourceMessage.ApplicationId, Is.EqualTo(9U));
        Assert.That(result.TranslatedMessage!.ApplicationId, Is.EqualTo(4U));
    }

    [Test]
    public void UntranslatedOutcomeNeverForwardsMessageUnderTargetApplication()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        handler.RequestResult.Disposition =
            DiameterTranslationDisposition.Untranslated;
        handler.RequestResult.Code = "translation-declined";
        handler.RequestResult.Message = "The handler declined translation.";
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteRequest request = CreateTranslationRequest();

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Rejected, Is.False);
            Assert.That(decision.RoutedMessage, Is.Null);
            Assert.That(decision.ResultCode, Is.Zero);
            Assert.That(decision.Code, Is.EqualTo("translation-declined"));
            Assert.That(decision.TranslationResult!.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Untranslated));
            Assert.That(decision.TranslationResult.SourceMessage!.ApplicationId,
                Is.EqualTo(4U));
            Assert.That(decision.TranslationResult.TranslatedMessage, Is.Null);
        });
    }

    [Test]
    public void TranslationErrorProducesSourceApplicationErrorAnswer()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        handler.RequestResult.Disposition = DiameterTranslationDisposition.Error;
        handler.RequestResult.ResultCode =
            DiameterResultCodes.InvalidAvpValue;
        handler.RequestResult.Code = "translation-invalid-value";
        handler.RequestResult.Message = "A source value cannot be translated.";
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteRequest request = CreateTranslationRequest();

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Rejected, Is.True);
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(decision.TranslationResult!.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Error));
            Assert.That(decision.RoutedMessage!.ApplicationId, Is.EqualTo(4U));
            Assert.That(decision.RoutedMessage.CommandCode, Is.EqualTo(300U));
            Assert.That(decision.RoutedMessage.HopByHopIdentifier,
                Is.EqualTo(request.Message.HopByHopIdentifier));
            Assert.That(decision.RoutedMessage.EndToEndIdentifier,
                Is.EqualTo(request.Message.EndToEndIdentifier));
            Assert.That((decision.RoutedMessage.Flags &
                DiameterMessageFlags.Error), Is.EqualTo(
                DiameterMessageFlags.None));
            Assert.That(ReadResultCode(decision.RoutedMessage),
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
        });
    }

    [Test]
    public void MissingDisabledAndMalformedHandlersHaveExplicitOutcomes()
    {
        RecordingTranslationHandler disabledHandler =
            new RecordingTranslationHandler();
        RouteSelector disabled = CreateTranslationSelector(
            disabledHandler,
            false);
        DiameterRouteDecision unavailable = disabled.SelectRoute(
            CreateTranslationRequest(),
            new List<DiameterPeerSnapshot>());
        RecordingTranslationHandler malformedHandler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        malformedHandler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        malformedHandler.RequestResult.TranslatedMessage!.ApplicationId = 4U;
        RouteSelector malformed = CreateTranslationSelector(
            malformedHandler,
            true);

        DiameterRouteDecision invalid = malformed.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Routed, Is.False);
            Assert.That(unavailable.Rejected, Is.False);
            Assert.That(unavailable.Code,
                Is.EqualTo("translation-handler-unavailable"));
            Assert.That(unavailable.TranslationResult!.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Untranslated));
            Assert.That(disabledHandler.RequestCount, Is.Zero);
            Assert.That(invalid.Routed, Is.False);
            Assert.That(invalid.Rejected, Is.True);
            Assert.That(invalid.TranslationResult!.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Error));
            Assert.That(invalid.TranslationResult.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(ReadResultCode(invalid.RoutedMessage!),
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
        });
    }

    [Test]
    public void MismatchedTargetAnswerIsRejectedBeforeHandlerDispatch()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        handler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        DiameterMessage answer = CreateTargetAnswer(decision.RoutedMessage!);
        answer.EndToEndIdentifier = answer.EndToEndIdentifier + 1U;

        DiameterTranslationResult result =
            selector.Translations.TranslateDispatchedAnswer(
                decision.TranslationResult,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Error));
            Assert.That(result.Code,
                Is.EqualTo("translation-answer-correlation-invalid"));
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(result.TranslatedMessage!.ApplicationId,
                Is.EqualTo(4U));
            Assert.That(ReadResultCode(result.TranslatedMessage),
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(handler.AnswerCount, Is.Zero);
        });
    }

    [Test]
    public void AnswerTranslationErrorReturnsSourceApplicationFailure()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        handler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        DiameterMessage answer = CreateTargetAnswer(decision.RoutedMessage!);
        handler.AnswerResult.Disposition =
            DiameterTranslationDisposition.Error;
        handler.AnswerResult.ResultCode = DiameterResultCodes.TooBusy;
        handler.AnswerResult.Code = "answer-translation-failed";
        handler.AnswerResult.Message = "The answer cannot be translated.";

        DiameterTranslationResult result =
            selector.Translations.TranslateDispatchedAnswer(
                decision.TranslationResult,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Error));
            Assert.That(result.Code, Is.EqualTo("answer-translation-failed"));
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(result.TranslatedMessage!.ApplicationId,
                Is.EqualTo(4U));
            Assert.That(result.TranslatedMessage.CommandCode, Is.EqualTo(300U));
            Assert.That((result.TranslatedMessage.Flags &
                DiameterMessageFlags.Error), Is.EqualTo(
                DiameterMessageFlags.Error));
            Assert.That(ReadResultCode(result.TranslatedMessage),
                Is.EqualTo(DiameterResultCodes.TooBusy));
        });
    }

    [Test]
    public void UntranslatedTargetAnswerIsNotExposedAsSourceAnswer()
    {
        RecordingTranslationHandler handler =
            new RecordingTranslationHandler();
        DiameterRouteRequest request = CreateTranslationRequest();
        handler.RequestResult = CreateTranslatedRequestResult(
            request.Message,
            9U);
        RouteSelector selector = CreateTranslationSelector(handler, true);
        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        DiameterMessage answer = CreateTargetAnswer(decision.RoutedMessage!);
        handler.AnswerResult.Disposition =
            DiameterTranslationDisposition.Untranslated;
        handler.AnswerResult.Code = "answer-translation-declined";
        handler.AnswerResult.Message = "The answer was not translated.";

        DiameterTranslationResult result =
            selector.Translations.TranslateDispatchedAnswer(
                decision.TranslationResult,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Untranslated));
            Assert.That(result.Code,
                Is.EqualTo("answer-translation-declined"));
            Assert.That(result.SourceMessage!.ApplicationId, Is.EqualTo(9U));
            Assert.That(result.TranslatedMessage, Is.Null);
            Assert.That(handler.AnswerCount, Is.EqualTo(1));
        });
    }

    private static RouteSelector CreateTranslationSelector(
        RecordingTranslationHandler handler,
        bool enabled)
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "node.local.example";
        configuration.OriginRealm.Value = "local.example";
        configuration.ProductName = "diameter.Tests";
        configuration.Roles = DiameterNodeRole.Translation;
        configuration.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        configuration.Applications.Add(application);
        DiameterApplicationId targetApplication = new DiameterApplicationId();
        targetApplication.Id = 9U;
        configuration.Applications.Add(targetApplication);
        DiameterTranslationHandlerConfiguration registration =
            new DiameterTranslationHandlerConfiguration();
        registration.SourceApplicationId = 4U;
        registration.TargetApplicationId = 9U;
        registration.Handler = handler;
        registration.Enabled = enabled;
        configuration.TranslationHandlers.Add(registration);
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(new long[] { 0L }));
        DiameterRouteConfiguration route = new DiameterRouteConfiguration();
        route.RouteIdentifier = "translate-four-to-nine";
        route.DestinationRealm.Value = "remote.example";
        route.ApplicationId = 4U;
        route.Action = DiameterRouteAction.Translate;
        route.AgentRole = DiameterAgentRole.Translation;
        route.TargetApplicationId = 9U;
        table.AddOrReplaceStaticRoute(route);
        return new RouteSelector(configuration, table);
    }

    private static DiameterRouteRequest CreateTranslationRequest()
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.Message.HopByHopIdentifier = 101U;
        result.Message.EndToEndIdentifier = 202U;
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 1U;
        avp.Data = new byte[] { 1, 2, 3 };
        result.Message.Avps.Add(avp);
        result.SourcePeer = CreateIdentity("ingress.example");
        result.DestinationRealm = CreateIdentity("remote.example");
        result.ApplicationId = 4U;
        return result;
    }

    private static DiameterTranslationResult CreateTranslatedRequestResult(
        DiameterMessage source,
        uint targetApplicationId)
    {
        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceApplicationId = source.ApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.TranslatedMessage = CloneMessage(source);
        result.TranslatedMessage.CommandCode = 301U;
        result.TranslatedMessage.ApplicationId = targetApplicationId;
        result.TranslationState = new byte[] { 7, 8, 9 };
        result.Code = "request-translated";
        result.Message = "The request was translated.";
        return result;
    }

    private static DiameterTranslationResult CreateTranslatedAnswerResult(
        DiameterMessage sourceRequest,
        DiameterMessage targetAnswer)
    {
        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceApplicationId = sourceRequest.ApplicationId;
        result.TargetApplicationId = targetAnswer.ApplicationId;
        result.TranslatedMessage = CloneMessage(targetAnswer);
        result.TranslatedMessage.CommandCode = sourceRequest.CommandCode;
        result.TranslatedMessage.ApplicationId = sourceRequest.ApplicationId;
        result.TranslationState = new byte[] { 4, 5, 6 };
        result.Code = "answer-translated";
        result.Message = "The answer was translated.";
        return result;
    }

    private static DiameterMessage CreateTargetAnswer(DiameterMessage request)
    {
        DiameterMessage result = CloneMessage(request);
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        return result;
    }

    private static DiameterMessage CloneMessage(DiameterMessage source)
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
            DiameterAvp avp = new DiameterAvp();
            avp.Code = source.Avps[index].Code;
            avp.Flags = source.Avps[index].Flags;
            avp.VendorId = source.Avps[index].VendorId;
            avp.Data = new byte[source.Avps[index].Data.Length];
            Array.Copy(
                source.Avps[index].Data,
                avp.Data,
                avp.Data.Length);
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static uint ReadResultCode(DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == DiameterAvpCodes.ResultCode)
            {
                return NetworkByteOrder.ReadUInt32(message.Avps[index].Data);
            }

            index = index + 1;
        }

        throw new AssertionException("A Result-Code AVP was expected.");
    }
}

internal class RecordingTranslationHandler : ITranslationHandler
{
    internal DiameterTranslationResult RequestResult =
        new DiameterTranslationResult();
    internal DiameterTranslationResult AnswerResult =
        new DiameterTranslationResult();
    internal DiameterRouteRequest? ObservedRequest;
    internal DiameterTranslationResult? ObservedRequestTranslation;
    internal DiameterMessage? ObservedAnswer;
    internal int RequestCount;
    internal int AnswerCount;

    public DiameterTranslationResult TranslateDiameterRequest(
        DiameterRouteRequest? request,
        uint targetApplicationId)
    {
        RequestCount = RequestCount + 1;
        ObservedRequest = request;
        return RequestResult;
    }

    public DiameterTranslationResult TranslateDiameterAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer)
    {
        AnswerCount = AnswerCount + 1;
        ObservedRequestTranslation = requestTranslation;
        ObservedAnswer = answer;
        return AnswerResult;
    }
}
