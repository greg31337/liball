namespace sip.Tests;

public class UsageAndEndpointModelTests
{
    [Test]
    public void SubscriptionAndPublicationModelsRetainUsageState()
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        SipSubscriptionRequest subscriptionRequest = new SipSubscriptionRequest();
        subscriptionRequest.Body = [1];
        subscriptionRequest.Cancellation = cancellation;
        SipSubscriptionEvent subscriptionEvent = new SipSubscriptionEvent();
        subscriptionEvent.State = SipSubscriptionState.Active;
        subscriptionEvent.Request = subscriptionRequest;

        SipPublicationRequest publicationRequest = new SipPublicationRequest();
        publicationRequest.Body = [2];
        publicationRequest.Cancellation = cancellation;
        SipPublicationEvent publicationEvent = new SipPublicationEvent();
        publicationEvent.State = SipPublicationState.Published;
        publicationEvent.Request = publicationRequest;

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.Requested, Is.True);
            Assert.That(subscriptionRequest.ExpiresSeconds, Is.EqualTo(SipDefaults.SubscriptionExpiresSeconds));
            Assert.That(subscriptionRequest.Body, Is.EqualTo(new byte[] { 1 }));
            Assert.That(subscriptionRequest.Cancellation, Is.SameAs(cancellation));
            Assert.That(subscriptionEvent.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(subscriptionEvent.Request, Is.SameAs(subscriptionRequest));
            Assert.That(subscriptionEvent.ExpiresSeconds, Is.EqualTo(-1));
            Assert.That(publicationRequest.ExpiresSeconds, Is.EqualTo(SipDefaults.SubscriptionExpiresSeconds));
            Assert.That(publicationRequest.Body, Is.EqualTo(new byte[] { 2 }));
            Assert.That(publicationRequest.Cancellation, Is.SameAs(cancellation));
            Assert.That(publicationEvent.State, Is.EqualTo(SipPublicationState.Published));
            Assert.That(publicationEvent.Request, Is.SameAs(publicationRequest));
            Assert.That(publicationEvent.ExpiresSeconds, Is.EqualTo(-1));
        });
    }

    [Test]
    public void MessageInfoOptionsAndExtensionModelsCompose()
    {
        SipMediaType mediaType = new SipMediaType();
        SipInstantMessageRequest instantRequest = new SipInstantMessageRequest();
        instantRequest.Body = [3, 4];
        instantRequest.ContentType = mediaType;
        SipInstantMessageEvent instantEvent = new SipInstantMessageEvent();
        instantEvent.Request = instantRequest;

        SipInfoRequest infoRequest = new SipInfoRequest();
        infoRequest.Body = [5];
        infoRequest.ContentType = mediaType;
        SipInfoResult infoResult = new SipInfoResult();
        infoResult.Accepted = true;
        infoResult.StatusCode = SipStatusCodes.Ok;

        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.Methods = [SipMethods.Options];
        capabilities.OptionTags = [SipOptionTags.Timer];
        capabilities.MediaTypes = [SipMediaTypes.Sdp];
        capabilities.EventPackages = ["presence"];
        capabilities.HeaderNames = [SipHeaderNames.SessionExpires];
        SipOptionsResult optionsResult = new SipOptionsResult();
        optionsResult.Success = true;
        optionsResult.Capabilities = capabilities;
        SipExtensionResult extensionResult = new SipExtensionResult();
        extensionResult.Handled = true;

        Assert.Multiple(() =>
        {
            Assert.That(instantRequest.Body, Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(instantRequest.ContentType, Is.SameAs(mediaType));
            Assert.That(instantEvent.Request, Is.SameAs(instantRequest));
            Assert.That(infoRequest.Body, Is.EqualTo(new byte[] { 5 }));
            Assert.That(infoRequest.ContentType, Is.SameAs(mediaType));
            Assert.That(infoResult.Accepted, Is.True);
            Assert.That(infoResult.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(capabilities.Methods, Is.EqualTo(new string[] { SipMethods.Options }));
            Assert.That(capabilities.OptionTags, Is.EqualTo(new string[] { SipOptionTags.Timer }));
            Assert.That(capabilities.MediaTypes, Is.EqualTo(new string[] { SipMediaTypes.Sdp }));
            Assert.That(capabilities.EventPackages, Is.EqualTo(new string[] { "presence" }));
            Assert.That(capabilities.HeaderNames, Is.EqualTo(new string[] { SipHeaderNames.SessionExpires }));
            Assert.That(optionsResult.Success, Is.True);
            Assert.That(optionsResult.Capabilities, Is.SameAs(capabilities));
            Assert.That(extensionResult.Handled, Is.True);
        });
    }

    [Test]
    public void BackToBackAndStackModelsComposeAllEventFamilies()
    {
        SipDialogSnapshot left = new SipDialogSnapshot();
        SipDialogSnapshot right = new SipDialogSnapshot();
        SipBackToBackEvent backToBack = new SipBackToBackEvent();
        backToBack.State = SipBackToBackState.Confirmed;
        backToBack.LeftDialog = left;
        backToBack.RightDialog = right;

        SipStackOptions options = new SipStackOptions();
        SipTransportOptions transport = new SipTransportOptions();
        options.Transports = [transport];

        SipTransportEvent transportEvent = new SipTransportEvent();
        SipTransactionEvent transactionEvent = new SipTransactionEvent();
        SipDialogEvent dialogEvent = new SipDialogEvent();
        SipRegistrationEvent registrationEvent = new SipRegistrationEvent();
        SipProxyEvent proxyEvent = new SipProxyEvent();
        SipInviteEvent inviteEvent = new SipInviteEvent();
        SipStackEvent stackEvent = new SipStackEvent();
        stackEvent.Kind = SipStackEventKind.Transport;
        stackEvent.TransportEvent = transportEvent;
        stackEvent.TransactionEvent = transactionEvent;
        stackEvent.DialogEvent = dialogEvent;
        stackEvent.RegistrationEvent = registrationEvent;
        stackEvent.ProxyEvent = proxyEvent;
        stackEvent.InviteEvent = inviteEvent;

        Assert.Multiple(() =>
        {
            Assert.That(backToBack.State, Is.EqualTo(SipBackToBackState.Confirmed));
            Assert.That(backToBack.LeftDialog, Is.SameAs(left));
            Assert.That(backToBack.RightDialog, Is.SameAs(right));
            Assert.That(options.ParseOptions, Is.Not.Null);
            Assert.That(options.WriteOptions, Is.Not.Null);
            Assert.That(options.DnsOptions, Is.Not.Null);
            Assert.That(options.TransactionOptions, Is.Not.Null);
            Assert.That(options.DialogOptions, Is.Not.Null);
            Assert.That(options.Transports, Is.EqualTo(new SipTransportOptions[] { transport }));
            Assert.That(options.MaxEventCount, Is.EqualTo(SipDefaults.MaxQueueCount));
            Assert.That(stackEvent.Kind, Is.EqualTo(SipStackEventKind.Transport));
            Assert.That(stackEvent.TransportEvent, Is.SameAs(transportEvent));
            Assert.That(stackEvent.TransactionEvent, Is.SameAs(transactionEvent));
            Assert.That(stackEvent.DialogEvent, Is.SameAs(dialogEvent));
            Assert.That(stackEvent.RegistrationEvent, Is.SameAs(registrationEvent));
            Assert.That(stackEvent.ProxyEvent, Is.SameAs(proxyEvent));
            Assert.That(stackEvent.InviteEvent, Is.SameAs(inviteEvent));
        });
    }

    [Test]
    public void UsageMutableDefaultsAreOwnedPerInstance()
    {
        SipSubscriptionRequest firstSubscription = new SipSubscriptionRequest();
        SipSubscriptionRequest secondSubscription = new SipSubscriptionRequest();
        SipPublicationRequest firstPublication = new SipPublicationRequest();
        SipPublicationRequest secondPublication = new SipPublicationRequest();
        SipInstantMessageRequest firstMessage = new SipInstantMessageRequest();
        SipInstantMessageRequest secondMessage = new SipInstantMessageRequest();
        SipInfoRequest firstInfo = new SipInfoRequest();
        SipInfoRequest secondInfo = new SipInfoRequest();
        SipCapabilitySet firstCapabilities = new SipCapabilitySet();
        SipCapabilitySet secondCapabilities = new SipCapabilitySet();
        SipStackOptions firstOptions = new SipStackOptions();
        SipStackOptions secondOptions = new SipStackOptions();

        Assert.Multiple(() =>
        {
            Assert.That(firstSubscription.Body, Is.Not.SameAs(secondSubscription.Body));
            Assert.That(firstPublication.Body, Is.Not.SameAs(secondPublication.Body));
            Assert.That(firstMessage.Body, Is.Not.SameAs(secondMessage.Body));
            Assert.That(firstInfo.Body, Is.Not.SameAs(secondInfo.Body));
            Assert.That(firstCapabilities.Methods, Is.Not.SameAs(secondCapabilities.Methods));
            Assert.That(firstOptions.Transports, Is.Not.SameAs(secondOptions.Transports));
        });
    }
}
