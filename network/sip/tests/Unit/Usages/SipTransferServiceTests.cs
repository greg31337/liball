namespace sip.Tests;

public class SipTransferServiceTests
{
    [Test]
    public void BlindReferIsValidatedAndAuthorizedWithReferrerContext()
    {
        SipMessage message = Request(SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo, "<sip:carol@example.com>"),
            Header(SipHeaderNames.ReferredBy, "<sip:alice@example.com>"));
        CapturingTransferPolicy policy = new CapturingTransferPolicy();
        policy.Decision.Allowed = true;
        policy.Decision.Identity = "alice";
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(policy);
        SipTransferRequest request = TransferRequest(message);
        request.AuthenticatedIdentity = "alice";
        request.CorrelationId = "transfer-1";

        SipTransferResult result = service.EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.AuthorizationEvaluated, Is.True);
            Assert.That(result.Authorized, Is.True);
            Assert.That(result.Operation, Is.EqualTo(SipTransferOperation.BlindTransfer));
            Assert.That(result.ReferTo!.Uri!.User, Is.EqualTo("carol"));
            Assert.That(result.ReferredBy!.Uri!.User, Is.EqualTo("alice"));
            Assert.That(result.StatusCode, Is.Zero);
            Assert.That(policy.Context!.AuthenticatedIdentity, Is.EqualTo("alice"));
            Assert.That(policy.Context.CorrelationId, Is.EqualTo("transfer-1"));
        });
    }

    [Test]
    public void ReferToReplacesCorrelatesAnAttendedTransfer()
    {
        SipMessage message = Request(SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo,
                "<sip:carol@example.com?Replaces=call-1%40example.com%3Bto-tag%3Dlocal%3Bfrom-tag%3Dremote%3Bearly-only>"));
        SipDialogSnapshot dialog = Dialog("call-1@example.com", "local",
            "remote", SipDialogState.Early, SipDialogRole.Uac, false);
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[] { dialog };

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Operation, Is.EqualTo(SipTransferOperation.AttendedTransfer));
            Assert.That(result.EarlyOnly, Is.True);
            Assert.That(result.ReferencedDialog!.Id!.CallId,
                Is.EqualTo("call-1@example.com"));
            Assert.That(result.ReferencedDialog, Is.Not.SameAs(dialog));
            Assert.That(result.AuthorizationEvaluated, Is.False);
        });
    }

    [Test]
    public void InviteReplacesInfersCallPickupAndMatchesTagsInUasOrientation()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Replaces,
                "pickup@example.com;to-tag=ours;from-tag=theirs"));
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[]
        {
            Dialog("pickup@example.com", "ours", "theirs",
                SipDialogState.Confirmed, SipDialogRole.Uas, false)
        };

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Operation, Is.EqualTo(SipTransferOperation.CallPickup));
            Assert.That(result.ReferencedDialog, Is.Not.Null);
            Assert.That(result.StatusCode, Is.Zero);
        });
    }

    [Test]
    public void ReplacesRejectsConfirmedDialogWhenEarlyOnlyWasRequested()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Replaces,
                "call@example.com;to-tag=local;from-tag=remote;early-only"));
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[]
        {
            Dialog("call@example.com", "local", "remote",
                SipDialogState.Confirmed, SipDialogRole.Uac, false)
        };

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BusyHere));
            Assert.That(result.ErrorMessage, Does.Contain("early-only"));
        });
    }

    [Test]
    public void ReplacesRejectsEarlyDialogThatWasNotInitiatedLocally()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Replaces,
                "call@example.com;to-tag=local;from-tag=remote"));
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[]
        {
            Dialog("call@example.com", "local", "remote",
                SipDialogState.Early, SipDialogRole.Uas, false)
        };

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.That(result.StatusCode,
            Is.EqualTo(SipStatusCodes.CallOrTransactionDoesNotExist));
    }

    [Test]
    public void JoinSupportsEarlyDialogsAndApplicationDenial()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Join,
                "join@example.com;to-tag=local;from-tag=remote"),
            Header(SipHeaderNames.ReferredBy, "<sip:supervisor@example.com>"));
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[]
        {
            Dialog("join@example.com", "local", "remote",
                SipDialogState.Early, SipDialogRole.Uas, true)
        };
        CapturingTransferPolicy policy = new CapturingTransferPolicy();
        policy.Decision.Allowed = false;
        policy.Decision.StatusCode = SipStatusCodes.NotAcceptableHere;
        policy.Decision.Reason = "Mixing is unavailable.";
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(policy);

        SipTransferResult result = service.EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Operation, Is.EqualTo(SipTransferOperation.DialogJoin));
            Assert.That(result.AuthorizationEvaluated, Is.True);
            Assert.That(result.Authorized, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.NotAcceptableHere));
            Assert.That(result.ErrorMessage, Is.EqualTo("Mixing is unavailable."));
            Assert.That(policy.Context!.ReferencedDialog!.State,
                Is.EqualTo(SipDialogState.Early));
        });
    }

    [Test]
    public void ContradictoryReplacesAndJoinAreRejected()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Replaces,
                "call@example.com;to-tag=local;from-tag=remote"),
            Header(SipHeaderNames.Join,
                "call@example.com;to-tag=local;from-tag=remote"));

        SipTransferResult result = new SipTransferService().EvaluateTransfer(
            TransferRequest(message));

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.ErrorMessage, Does.Contain("contradictory"));
        });
    }

    [Test]
    public void TargetDialogCorrelatesAuthorizationContextForOutOfDialogRefer()
    {
        SipMessage message = Request(SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo, "<sip:carol@example.com>"),
            Header(SipHeaderNames.TargetDialog,
                "target@example.com;local-tag=local;remote-tag=remote"));
        SipDialogSnapshot dialog = Dialog("target@example.com", "local",
            "remote", SipDialogState.Confirmed, SipDialogRole.Uac, true);
        SipTransferRequest request = TransferRequest(message);
        request.Operation = SipTransferOperation.TargetDialogAssociation;
        request.Dialogs = new SipDialogSnapshot[] { dialog };
        CapturingTransferPolicy policy = new CapturingTransferPolicy();
        policy.Decision.Allowed = true;
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(policy);

        SipTransferResult result = service.EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Authorized, Is.True);
            Assert.That(result.Operation,
                Is.EqualTo(SipTransferOperation.TargetDialogAssociation));
            Assert.That(result.TargetDialog!.Secure, Is.True);
            Assert.That(policy.Context!.TargetDialog!.Id!.CallId,
                Is.EqualTo("target@example.com"));
        });
    }

    [Test]
    public void UnmatchedTargetDialogIsIgnoredBeforeApplicationAuthorization()
    {
        SipMessage message = Request(SipMethod.Subscribe, SipMethods.Subscribe,
            Header(SipHeaderNames.TargetDialog,
                "missing@example.com;local-tag=local;remote-tag=remote"));
        SipTransferRequest request = TransferRequest(message);
        CapturingTransferPolicy policy = new CapturingTransferPolicy();
        policy.Decision.Allowed = false;
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(policy);

        SipTransferResult result = service.EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.TargetDialog, Is.Null);
            Assert.That(result.AuthorizationEvaluated, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(policy.Context!.TargetDialog, Is.Null);
        });
    }

    [Test]
    public void RequiredReferredByUsesProvideReferrerIdentityFailure()
    {
        SipTransferRequest request = TransferRequest(Request(
            SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo, "<sip:carol@example.com>")));
        request.RequireReferredBy = true;

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.That(result.StatusCode,
            Is.EqualTo(SipStatusCodes.ProvideReferrerIdentity));
    }

    [Test]
    public void DuplicateDialogMatchesFailCorrelation()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Join,
                "join@example.com;to-tag=local;from-tag=remote"));
        SipDialogSnapshot first = Dialog("join@example.com", "local",
            "remote", SipDialogState.Confirmed, SipDialogRole.Uac, false);
        SipDialogSnapshot second = Dialog("join@example.com", "local",
            "remote", SipDialogState.Confirmed, SipDialogRole.Uac, false);
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[] { first, second };

        SipTransferResult result = new SipTransferService().EvaluateTransfer(request);

        Assert.That(result.StatusCode,
            Is.EqualTo(SipStatusCodes.CallOrTransactionDoesNotExist));
    }

    [Test]
    public void MismatchedMethodTokenCannotInferTransferOperation()
    {
        SipMessage message = Request(
            SipMethod.Refer, SipMethods.Invite,
            Header(SipHeaderNames.ReferTo,
                "<sip:carol@example.com>"));

        SipTransferResult result = new SipTransferService()
            .EvaluateTransfer(TransferRequest(message));

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Operation,
                Is.EqualTo(SipTransferOperation.None));
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.ErrorMessage,
                Does.Contain("valid only on a REFER"));
        });
    }

    [Test]
    public void ReplacesRejectsTerminatedDialog()
    {
        SipMessage message = Request(SipMethod.Invite, SipMethods.Invite,
            Header(SipHeaderNames.Replaces,
                "ended@example.com;to-tag=local;from-tag=remote"));
        SipTransferRequest request = TransferRequest(message);
        request.Dialogs = new SipDialogSnapshot[]
        {
            Dialog("ended@example.com", "local", "remote",
                SipDialogState.Terminated, SipDialogRole.Uac, false)
        };

        SipTransferResult result = new SipTransferService()
            .EvaluateTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.Decline));
            Assert.That(result.ErrorMessage, Does.Contain("terminated"));
        });
    }

    [Test]
    public void RepeatedTransferHeadersAreRejectedBeforeAuthorization()
    {
        SipMessage message = Request(SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo,
                "<sip:carol@example.com>"),
            Header(SipHeaderNames.ReferredBy,
                "<sip:alice@example.com>"),
            Header(SipHeaderNames.ReferredBy,
                "<sip:mallory@example.com>"));
        CapturingTransferPolicy policy = new CapturingTransferPolicy();
        policy.Decision.Allowed = true;
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(policy);

        SipTransferResult result = service.EvaluateTransfer(
            TransferRequest(message));

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.ErrorMessage, Does.Contain("repeated"));
            Assert.That(result.AuthorizationEvaluated, Is.False);
            Assert.That(policy.Context, Is.Null);
        });
    }

    [Test]
    public void MissingAuthorizationDecisionProducesServerFailure()
    {
        SipMessage message = Request(SipMethod.Refer, SipMethods.Refer,
            Header(SipHeaderNames.ReferTo,
                "<sip:carol@example.com>"));
        SipTransferService service = new SipTransferService();
        service.ConfigureTransferAuthorizationPolicy(
            new NullTransferPolicy());

        SipTransferResult result = service.EvaluateTransfer(
            TransferRequest(message));

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.AuthorizationEvaluated, Is.True);
            Assert.That(result.Authorized, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(result.ErrorMessage, Does.Contain("no decision"));
        });
    }

    static SipTransferRequest TransferRequest(SipMessage message)
    {
        SipTransferRequest request = new SipTransferRequest();
        request.Message = message;
        return request;
    }

    static SipMessage Request(
        SipMethod method, string token, params SipHeader[] headers)
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = method;
        message.MethodToken = token;
        message.Headers = headers;
        return message;
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static SipDialogSnapshot Dialog(
        string callId, string localTag, string remoteTag,
        SipDialogState state, SipDialogRole role, bool secure)
    {
        SipDialogId id = new SipDialogId();
        id.CallId = callId;
        id.LocalTag = localTag;
        id.RemoteTag = remoteTag;
        SipDialogSnapshot dialog = new SipDialogSnapshot();
        dialog.Id = id;
        dialog.State = state;
        dialog.Role = role;
        dialog.Secure = secure;
        return dialog;
    }

    sealed class CapturingTransferPolicy : ISipTransferAuthorizationPolicy
    {
        internal SipAuthorizationDecision Decision =
            new SipAuthorizationDecision();
        internal SipTransferAuthorizationContext? Context;

        public SipAuthorizationDecision AuthorizeTransfer(
            SipTransferAuthorizationContext context)
        {
            Context = context;
            return Decision;
        }
    }

    sealed class NullTransferPolicy : ISipTransferAuthorizationPolicy
    {
        public SipAuthorizationDecision AuthorizeTransfer(
            SipTransferAuthorizationContext context)
        {
            return null!;
        }
    }
}
