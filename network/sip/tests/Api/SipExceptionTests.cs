namespace sip.Tests;

public class SipExceptionTests
{
    [Test]
    public void ParsingProtocolAndTransportExceptionsRetainContext()
    {
        SipParseException parse = new SipParseException("Invalid header", 17, "header-value");
        SipProtocolException protocol = new SipProtocolException("Unexpected response", 481, "INVITE");
        SipTransportException transport = new SipTransportException("Connection closed", SipTransportKind.Tcp, "connection-7");

        Assert.Multiple(() =>
        {
            Assert.That(parse.Message, Is.EqualTo("Invalid header"));
            Assert.That(parse.ByteOffset, Is.EqualTo(17));
            Assert.That(parse.ErrorCode, Is.EqualTo("header-value"));
            Assert.That(protocol.Message, Is.EqualTo("Unexpected response"));
            Assert.That(protocol.StatusCode, Is.EqualTo(481));
            Assert.That(protocol.MethodToken, Is.EqualTo("INVITE"));
            Assert.That(transport.Message, Is.EqualTo("Connection closed"));
            Assert.That(transport.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(transport.ConnectionId, Is.EqualTo("connection-7"));
        });
    }

    [Test]
    public void ResolutionTransactionAndDialogExceptionsRetainContext()
    {
        SipDialogId dialogId = new SipDialogId();
        dialogId.CallId = "call-1";
        SipResolutionException resolution = new SipResolutionException("No records", SipResolutionStatus.NoRecords, "example.com");
        SipTransactionException transaction = new SipTransactionException("Timer expired", "branch-1", SipTransactionState.Terminated);
        SipDialogException dialog = new SipDialogException("Dialog ended", dialogId, SipDialogState.Terminated);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Message, Is.EqualTo("No records"));
            Assert.That(resolution.Status, Is.EqualTo(SipResolutionStatus.NoRecords));
            Assert.That(resolution.Domain, Is.EqualTo("example.com"));
            Assert.That(transaction.Message, Is.EqualTo("Timer expired"));
            Assert.That(transaction.TransactionId, Is.EqualTo("branch-1"));
            Assert.That(transaction.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(dialog.Message, Is.EqualTo("Dialog ended"));
            Assert.That(dialog.DialogId, Is.SameAs(dialogId));
            Assert.That(dialog.State, Is.EqualTo(SipDialogState.Terminated));
        });
    }

    [Test]
    public void AuthenticationRegistrationAndPolicyExceptionsRetainContext()
    {
        SipUri addressOfRecord = new SipUri();
        addressOfRecord.Host = "example.com";
        SipAuthenticationException authentication = new SipAuthenticationException("Bad credentials", SipAuthenticationStatus.InvalidCredentials, "example.com");
        SipRegistrationException registration = new SipRegistrationException("Registration failed", SipRegistrationState.Failed, addressOfRecord);
        SipPolicyException policy = new SipPolicyException("Request forbidden", 403, "Blocked destination");

        Assert.Multiple(() =>
        {
            Assert.That(authentication.Message, Is.EqualTo("Bad credentials"));
            Assert.That(authentication.Status, Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(authentication.Realm, Is.EqualTo("example.com"));
            Assert.That(registration.Message, Is.EqualTo("Registration failed"));
            Assert.That(registration.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(registration.AddressOfRecord, Is.SameAs(addressOfRecord));
            Assert.That(policy.Message, Is.EqualTo("Request forbidden"));
            Assert.That(policy.StatusCode, Is.EqualTo(403));
            Assert.That(policy.Reason, Is.EqualTo("Blocked destination"));
        });
    }

    [Test]
    public void OptionalExceptionContextCanBeAbsent()
    {
        SipParseException parse = new SipParseException("Parse failed", 0, null);
        SipProtocolException protocol = new SipProtocolException("Protocol failed", 0, null);
        SipTransportException transport = new SipTransportException("Transport failed", SipTransportKind.Unknown, null);
        SipResolutionException resolution = new SipResolutionException("Resolution failed", SipResolutionStatus.None, null);
        SipTransactionException transaction = new SipTransactionException("Transaction failed", null, SipTransactionState.None);
        SipDialogException dialog = new SipDialogException("Dialog failed", null, SipDialogState.None);
        SipAuthenticationException authentication = new SipAuthenticationException("Authentication failed", SipAuthenticationStatus.None, null);
        SipRegistrationException registration = new SipRegistrationException("Registration failed", SipRegistrationState.None, null);
        SipPolicyException policy = new SipPolicyException("Policy failed", 0, null);

        Assert.Multiple(() =>
        {
            Assert.That(parse.ErrorCode, Is.Null);
            Assert.That(protocol.MethodToken, Is.Null);
            Assert.That(transport.ConnectionId, Is.Null);
            Assert.That(resolution.Domain, Is.Null);
            Assert.That(transaction.TransactionId, Is.Null);
            Assert.That(dialog.DialogId, Is.Null);
            Assert.That(authentication.Realm, Is.Null);
            Assert.That(registration.AddressOfRecord, Is.Null);
            Assert.That(policy.Reason, Is.Null);
        });
    }
}
