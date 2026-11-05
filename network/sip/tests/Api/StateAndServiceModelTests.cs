namespace sip.Tests;

public class StateAndServiceModelTests
{
    [Test]
    public void TransactionAndDialogModelsComposeWithSafeDefaults()
    {
        SipTimerOptions timers = new SipTimerOptions();
        SipTransactionKey key = new SipTransactionKey();
        key.Method = SipMethod.Invite;
        key.Role = SipTransactionRole.Client;
        SipTransactionOptions transactionOptions = new SipTransactionOptions();
        SipTransactionSnapshot transaction = new SipTransactionSnapshot();
        transaction.Key = key;
        transaction.TimerNames = [SipTimerNames.TimerA];
        transaction.TimerDeadlines = [500L];
        SipTransactionEvent transactionEvent = new SipTransactionEvent();
        transactionEvent.Kind = SipTransactionEventKind.StateChanged;
        transactionEvent.State = SipTransactionState.Proceeding;

        SipDialogId dialogId = new SipDialogId();
        SipDialogOptions dialogOptions = new SipDialogOptions();
        SipDialogSnapshot dialog = new SipDialogSnapshot();
        dialog.Id = dialogId;
        dialog.Role = SipDialogRole.Uac;
        dialog.State = SipDialogState.Early;
        SipDialogEvent dialogEvent = new SipDialogEvent();
        dialogEvent.DialogId = dialogId;
        dialogEvent.Snapshot = dialog;

        SipOfferAnswer offerAnswer = new SipOfferAnswer();
        offerAnswer.State = SipOfferAnswerState.LocalOfferPending;
        offerAnswer.LocalOffer = [1, 2, 3];
        SipInviteOptions inviteOptions = new SipInviteOptions();
        SipCancellation cancellation = new SipCancellation();
        SipInviteRequest inviteRequest = new SipInviteRequest();
        inviteRequest.OfferAnswer = offerAnswer;
        inviteRequest.Options = inviteOptions;
        inviteRequest.Cancellation = cancellation;
        SipInviteEvent inviteEvent = new SipInviteEvent();
        inviteEvent.State = SipInviteSessionState.Early;
        inviteEvent.Dialog = dialog;
        inviteEvent.OfferAnswer = offerAnswer;

        Assert.Multiple(() =>
        {
            Assert.That(timers.T1Milliseconds, Is.EqualTo(SipDefaults.T1Milliseconds));
            Assert.That(timers.T2Milliseconds, Is.EqualTo(SipDefaults.T2Milliseconds));
            Assert.That(timers.T4Milliseconds, Is.EqualTo(SipDefaults.T4Milliseconds));
            Assert.That(key.CSeq, Is.EqualTo(-1));
            Assert.That(key.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(transactionOptions.Timers, Is.Not.Null);
            Assert.That(transactionOptions.AllowLegacyMatching, Is.True);
            Assert.That(transactionOptions.MaxTransactionCount, Is.EqualTo(SipDefaults.MaxTransactionCount));
            Assert.That(transaction.Key, Is.SameAs(key));
            Assert.That(transaction.State, Is.EqualTo(SipTransactionState.None));
            Assert.That(transaction.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerA }));
            Assert.That(transaction.TimerDeadlines, Is.EqualTo(new long[] { 500L }));
            Assert.That(transaction.CreatedAtMilliseconds, Is.EqualTo(-1));
            Assert.That(transaction.UpdatedAtMilliseconds, Is.EqualTo(-1));
            Assert.That(transactionEvent.Kind, Is.EqualTo(SipTransactionEventKind.StateChanged));
            Assert.That(transactionEvent.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(dialogOptions.MaxDialogCount, Is.EqualTo(SipDefaults.MaxDialogCount));
            Assert.That(dialogOptions.StrictRemoteCSeq, Is.True);
            Assert.That(dialogOptions.PreventSecureDowngrade, Is.True);
            Assert.That(dialog.Id, Is.SameAs(dialogId));
            Assert.That(dialog.Role, Is.EqualTo(SipDialogRole.Uac));
            Assert.That(dialog.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(dialog.LocalCSeq, Is.EqualTo(-1));
            Assert.That(dialog.RemoteCSeq, Is.EqualTo(-1));
            Assert.That(dialog.RouteSet, Is.Empty);
            Assert.That(dialog.UsageIds, Is.Empty);
            Assert.That(dialogEvent.DialogId, Is.SameAs(dialogId));
            Assert.That(dialogEvent.Snapshot, Is.SameAs(dialog));
            Assert.That(offerAnswer.State, Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(offerAnswer.LocalOffer, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(offerAnswer.NegotiatedBody, Is.Empty);
            Assert.That(inviteOptions.SessionTimers, Is.True);
            Assert.That(inviteOptions.SessionExpiresSeconds, Is.EqualTo(1800));
            Assert.That(inviteOptions.MinSessionExpiresSeconds, Is.EqualTo(90));
            Assert.That(inviteRequest.OfferAnswer, Is.SameAs(offerAnswer));
            Assert.That(inviteRequest.Options, Is.SameAs(inviteOptions));
            Assert.That(inviteRequest.Cancellation, Is.SameAs(cancellation));
            Assert.That(inviteEvent.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(inviteEvent.Dialog, Is.SameAs(dialog));
        });
    }

    [Test]
    public void RegistrationAndLocationModelsComposeWithPolicyDefaults()
    {
        SipRegistrationOptions options = new SipRegistrationOptions();
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.Options = options;
        SipRegistrationBinding binding = new SipRegistrationBinding();
        SipRegistrationSnapshot snapshot = new SipRegistrationSnapshot();
        snapshot.State = SipRegistrationState.Registered;
        snapshot.Bindings = [binding];
        SipRegistrationEvent registrationEvent = new SipRegistrationEvent();
        registrationEvent.Snapshot = snapshot;
        SipLocationQuery query = new SipLocationQuery();
        SipLocationResult location = new SipLocationResult();
        location.Bindings = [binding];
        SipRegistrarOptions registrar = new SipRegistrarOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ExpiresSeconds, Is.EqualTo(SipDefaults.RegistrationExpiresSeconds));
            Assert.That(options.MaxAuthenticationAttempts, Is.EqualTo(SipDefaults.MaxAuthenticationAttempts));
            Assert.That(request.Options, Is.SameAs(options));
            Assert.That(request.Contacts, Is.Empty);
            Assert.That(binding.CSeq, Is.EqualTo(-1));
            Assert.That(binding.ExpiresAtMilliseconds, Is.EqualTo(-1));
            Assert.That(binding.QualityThousandths, Is.EqualTo(1000));
            Assert.That(binding.Path, Is.Empty);
            Assert.That(binding.ServiceRoute, Is.Empty);
            Assert.That(binding.RegistrationId, Is.EqualTo(-1));
            Assert.That(snapshot.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(snapshot.Bindings, Is.EqualTo(new SipRegistrationBinding[] { binding }));
            Assert.That(snapshot.RefreshAtMilliseconds, Is.EqualTo(-1));
            Assert.That(registrationEvent.Snapshot, Is.SameAs(snapshot));
            Assert.That(query.RegistrationId, Is.EqualTo(-1));
            Assert.That(query.NowMilliseconds, Is.EqualTo(-1));
            Assert.That(location.Bindings, Is.EqualTo(new SipRegistrationBinding[] { binding }));
            Assert.That(registrar.DefaultExpiresSeconds, Is.EqualTo(SipDefaults.RegistrationExpiresSeconds));
            Assert.That(registrar.MaxBindingCount, Is.EqualTo(SipDefaults.MaxRegistrationBindingCount));
            Assert.That(registrar.RequireAuthentication, Is.True);
            Assert.That(registrar.Domains, Is.Empty);
        });
    }

    [Test]
    public void ProxyAndRedirectModelsRetainBranchRelationships()
    {
        SipProxyOptions options = new SipProxyOptions();
        SipServerCandidate candidate = new SipServerCandidate();
        SipFlow flow = new SipFlow();
        SipProxyTarget target = new SipProxyTarget();
        target.Candidate = candidate;
        target.Flow = flow;
        SipProxyRequest request = new SipProxyRequest();
        request.Targets = [target];
        request.Options = options;
        SipProxyResult result = new SipProxyResult();
        result.Targets = [target];
        SipProxyEvent proxyEvent = new SipProxyEvent();
        proxyEvent.Kind = SipProxyEventKind.BranchCreated;
        proxyEvent.Request = request;
        proxyEvent.Result = result;
        proxyEvent.Target = target;
        SipRouteDecision decision = new SipRouteDecision();
        decision.Mode = SipProxyMode.Redirect;
        decision.Targets = [target];
        SipRedirectResult redirect = new SipRedirectResult();
        SipContactValue contact = new SipContactValue();
        redirect.Contacts = [contact];

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(SipProxyMode.StatefulParallel));
            Assert.That(options.RecordRoute, Is.True);
            Assert.That(options.MaxBranchCount, Is.EqualTo(SipDefaults.MaxProxyBranchCount));
            Assert.That(options.TimerCMilliseconds, Is.EqualTo(180000));
            Assert.That(target.Candidate, Is.SameAs(candidate));
            Assert.That(target.Flow, Is.SameAs(flow));
            Assert.That(target.QualityThousandths, Is.EqualTo(1000));
            Assert.That(request.Targets, Is.EqualTo(new SipProxyTarget[] { target }));
            Assert.That(request.Options, Is.SameAs(options));
            Assert.That(result.Targets, Is.EqualTo(new SipProxyTarget[] { target }));
            Assert.That(proxyEvent.Kind, Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(proxyEvent.Request, Is.SameAs(request));
            Assert.That(proxyEvent.Result, Is.SameAs(result));
            Assert.That(proxyEvent.Target, Is.SameAs(target));
            Assert.That(decision.Mode, Is.EqualTo(SipProxyMode.Redirect));
            Assert.That(decision.Targets, Is.EqualTo(new SipProxyTarget[] { target }));
            Assert.That(redirect.Contacts, Is.EqualTo(new SipContactValue[] { contact }));
        });
    }
}
