namespace sip.Tests;

public class OperationalEnumTests
{
    [Test]
    public void NetworkEnumsHaveStableValues()
    {
        int[] actual =
        [
            (int)SipTransportKind.Unknown, (int)SipTransportKind.Udp, (int)SipTransportKind.Tcp,
            (int)SipTransportKind.Tls, (int)SipTransportKind.WebSocket,
            (int)SipTransportKind.SecureWebSocket, (int)SipTransportKind.Sctp,
            (int)SipTransportKind.Dtls, (int)SipTransportKind.Custom,
            (int)SipConnectionState.Closed, (int)SipConnectionState.Connecting,
            (int)SipConnectionState.Connected, (int)SipConnectionState.Closing,
            (int)SipConnectionState.Failed,
            (int)SipDnsRecordKind.Unknown, (int)SipDnsRecordKind.Naptr,
            (int)SipDnsRecordKind.Srv, (int)SipDnsRecordKind.A,
            (int)SipDnsRecordKind.Aaaa, (int)SipDnsRecordKind.Cname,
            (int)SipResolutionStatus.None, (int)SipResolutionStatus.Success,
            (int)SipResolutionStatus.NoRecords, (int)SipResolutionStatus.Timeout,
            (int)SipResolutionStatus.Failed, (int)SipResolutionStatus.Cancelled,
            (int)SipTransportEventKind.None, (int)SipTransportEventKind.Bound,
            (int)SipTransportEventKind.Connected, (int)SipTransportEventKind.Disconnected,
            (int)SipTransportEventKind.MessageReceived, (int)SipTransportEventKind.MessageSent,
            (int)SipTransportEventKind.SendFailed, (int)SipTransportEventKind.KeepAliveReceived,
            (int)SipTransportEventKind.FlowFailed, (int)SipTransportEventKind.Closed
        ];

        int[] expected =
        [
            0, 1, 2, 3, 4, 5, 6, 7, 8,
            0, 1, 2, 3, 4,
            0, 1, 2, 3, 4, 5,
            0, 1, 2, 3, 4, 5,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void AuthenticationEnumsHaveStableValues()
    {
        int[] actual =
        [
            (int)SipDigestAlgorithm.Unknown, (int)SipDigestAlgorithm.Md5,
            (int)SipDigestAlgorithm.Md5Sess, (int)SipDigestAlgorithm.Sha256,
            (int)SipDigestAlgorithm.Sha256Sess, (int)SipDigestAlgorithm.Sha512256,
            (int)SipDigestAlgorithm.Sha512256Sess,
            (int)SipDigestQuality.None, (int)SipDigestQuality.Auth, (int)SipDigestQuality.AuthInt,
            (int)SipAuthenticationStatus.None, (int)SipAuthenticationStatus.Success,
            (int)SipAuthenticationStatus.ChallengeRequired,
            (int)SipAuthenticationStatus.InvalidCredentials, (int)SipAuthenticationStatus.StaleNonce,
            (int)SipAuthenticationStatus.ReplayDetected, (int)SipAuthenticationStatus.Unsupported,
            (int)SipAuthenticationStatus.Failed,
            (int)SipCredentialScope.None, (int)SipCredentialScope.Origin, (int)SipCredentialScope.Proxy
        ];

        Assert.That(actual, Is.EqualTo(new int[]
        {
            0, 1, 2, 3, 4, 5, 6,
            0, 1, 2,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2
        }));
    }

    [Test]
    public void TransactionAndDialogEnumsHaveStableValues()
    {
        int[] actual =
        [
            (int)SipTransactionKind.Unknown, (int)SipTransactionKind.Invite,
            (int)SipTransactionKind.NonInvite,
            (int)SipTransactionRole.Unknown, (int)SipTransactionRole.Client,
            (int)SipTransactionRole.Server,
            (int)SipTransactionState.None, (int)SipTransactionState.Calling,
            (int)SipTransactionState.Trying, (int)SipTransactionState.Proceeding,
            (int)SipTransactionState.Completed, (int)SipTransactionState.Confirmed,
            (int)SipTransactionState.Accepted, (int)SipTransactionState.Terminated,
            (int)SipTransactionEventKind.None, (int)SipTransactionEventKind.Created,
            (int)SipTransactionEventKind.StateChanged, (int)SipTransactionEventKind.Retransmitted,
            (int)SipTransactionEventKind.MessageDelivered, (int)SipTransactionEventKind.TimerFired,
            (int)SipTransactionEventKind.TransportError, (int)SipTransactionEventKind.Terminated,
            (int)SipDialogRole.Unknown, (int)SipDialogRole.Uac, (int)SipDialogRole.Uas,
            (int)SipDialogState.None, (int)SipDialogState.Early,
            (int)SipDialogState.Confirmed, (int)SipDialogState.Terminated,
            (int)SipOfferAnswerState.None, (int)SipOfferAnswerState.Stable,
            (int)SipOfferAnswerState.LocalOfferPending, (int)SipOfferAnswerState.RemoteOfferPending,
            (int)SipOfferAnswerState.Completed, (int)SipOfferAnswerState.Failed,
            (int)SipInviteSessionState.None, (int)SipInviteSessionState.Calling,
            (int)SipInviteSessionState.Proceeding, (int)SipInviteSessionState.Early,
            (int)SipInviteSessionState.Confirmed, (int)SipInviteSessionState.Terminating,
            (int)SipInviteSessionState.Terminated
        ];

        Assert.That(actual, Is.EqualTo(new int[]
        {
            0, 1, 2,
            0, 1, 2,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2,
            0, 1, 2, 3,
            0, 1, 2, 3, 4, 5,
            0, 1, 2, 3, 4, 5, 6
        }));
    }

    [Test]
    public void ServiceAndUsageEnumsHaveStableValues()
    {
        int[] actual =
        [
            (int)SipRegistrationState.None, (int)SipRegistrationState.Registering,
            (int)SipRegistrationState.Registered, (int)SipRegistrationState.Refreshing,
            (int)SipRegistrationState.Unregistering, (int)SipRegistrationState.Failed,
            (int)SipRegistrationState.Terminated,
            (int)SipProxyMode.None, (int)SipProxyMode.Stateless,
            (int)SipProxyMode.StatefulSequential, (int)SipProxyMode.StatefulParallel,
            (int)SipProxyMode.Redirect,
            (int)SipProxyEventKind.None, (int)SipProxyEventKind.RequestAccepted,
            (int)SipProxyEventKind.BranchCreated, (int)SipProxyEventKind.ProvisionalForwarded,
            (int)SipProxyEventKind.FinalSelected, (int)SipProxyEventKind.BranchCancelled,
            (int)SipProxyEventKind.Completed, (int)SipProxyEventKind.Failed,
            (int)SipSubscriptionState.None, (int)SipSubscriptionState.Pending,
            (int)SipSubscriptionState.Active, (int)SipSubscriptionState.Terminated,
            (int)SipPublicationState.None, (int)SipPublicationState.Publishing,
            (int)SipPublicationState.Published, (int)SipPublicationState.Refreshing,
            (int)SipPublicationState.Removing, (int)SipPublicationState.Failed,
            (int)SipPublicationState.Terminated,
            (int)SipBackToBackState.None, (int)SipBackToBackState.Establishing,
            (int)SipBackToBackState.Confirmed, (int)SipBackToBackState.Terminating,
            (int)SipBackToBackState.Terminated, (int)SipBackToBackState.Failed,
            (int)SipStackEventKind.None, (int)SipStackEventKind.Started,
            (int)SipStackEventKind.Stopped, (int)SipStackEventKind.IncomingRequest,
            (int)SipStackEventKind.OutgoingResponse, (int)SipStackEventKind.Transport,
            (int)SipStackEventKind.Transaction, (int)SipStackEventKind.Dialog,
            (int)SipStackEventKind.Registration, (int)SipStackEventKind.Authentication,
            (int)SipStackEventKind.Error
        ];

        Assert.That(actual, Is.EqualTo(new int[]
        {
            0, 1, 2, 3, 4, 5, 6,
            0, 1, 2, 3, 4,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2, 3,
            0, 1, 2, 3, 4, 5, 6,
            0, 1, 2, 3, 4, 5,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
        }));
    }
}
