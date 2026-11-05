namespace pop3.Tests;

public class Pop3ClientStateMachineTests
{
    [Test]
    public void NewStateMachineProducesIndependentDisconnectedSnapshots()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();

        Pop3ClientSnapshot first = machine.GetSnapshot();
        first.State = Pop3SessionState.Transaction;
        first.Capabilities.SupportsTop = true;
        first.Messages = new[] { new Pop3MessageInfo() };
        Pop3ClientSnapshot second = machine.GetSnapshot();

        Assert.That(machine.GetCurrentState(), Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(second.State, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(second.IsConnected, Is.False);
        Assert.That(second.IsSecure, Is.False);
        Assert.That(second.IsAuthenticated, Is.False);
        Assert.That(second.Greeting, Is.Null);
        Assert.That(second.LastResponse, Is.Null);
        Assert.That(second.Capabilities.SupportsTop, Is.False);
        Assert.That(second.MailboxStatus, Is.Null);
        Assert.That(second.Messages, Is.Empty);
        Assert.That(second.UniqueIds, Is.Empty);
        Assert.That(second.Capabilities, Is.Not.SameAs(first.Capabilities));
        Assert.That(second.Messages, Is.Not.SameAs(first.Messages));
    }

    [Test]
    public void GuardsAcceptOnlyTheirLegalProtocolStates()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();

        Assert.DoesNotThrow(() => machine.RequireDisconnected("Connect"));
        AssertStateFailure(() => machine.RequireAuthorization(Pop3Commands.Auth), Pop3Commands.Auth, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.RequireTransaction(Pop3Commands.Stat), Pop3Commands.Stat, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.RequireActive(Pop3Commands.Capa), Pop3Commands.Capa, Pop3SessionState.Disconnected);

        machine.EnterAuthorization(CreateGreeting(), false);

        Assert.DoesNotThrow(() => machine.RequireAuthorization(Pop3Commands.Auth));
        Assert.DoesNotThrow(() => machine.RequireActive(Pop3Commands.Capa));
        AssertStateFailure(() => machine.RequireDisconnected("Connect"), "Connect", Pop3SessionState.Authorization);
        AssertStateFailure(() => machine.RequireTransaction(Pop3Commands.Stat), Pop3Commands.Stat, Pop3SessionState.Authorization);

        machine.EnterTransaction("alice");

        Assert.DoesNotThrow(() => machine.RequireTransaction(Pop3Commands.Stat));
        Assert.DoesNotThrow(() => machine.RequireActive(Pop3Commands.Capa));
        AssertStateFailure(() => machine.RequireAuthorization(Pop3Commands.Auth), Pop3Commands.Auth, Pop3SessionState.Transaction);

        machine.EnterUpdate();

        AssertStateFailure(() => machine.RequireActive(Pop3Commands.Noop), Pop3Commands.Noop, Pop3SessionState.Update);
        AssertStateFailure(() => machine.RequireTransaction(Pop3Commands.Stat), Pop3Commands.Stat, Pop3SessionState.Update);
    }

    [Test]
    public void ConnectionAndAuthenticationTransitionsUpdateObservableState()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3Response greeting = CreateGreeting();

        machine.EnterAuthorization(greeting, false);
        machine.MarkSecure();
        machine.EnableUtf8(true);
        machine.SelectLanguage("fr");
        machine.EnterTransaction("alice");

        Pop3ClientSnapshot snapshot = machine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.IsAuthenticated, Is.True);
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(snapshot.Greeting, Is.EqualTo("server ready"));
        Assert.That(snapshot.LastResponse, Is.Not.Null);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("server ready"));
        Assert.That(snapshot.IsUtf8Enabled, Is.True);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.True);
        Assert.That(snapshot.SelectedLanguage, Is.EqualTo("fr"));
    }

    [Test]
    public void NegativeGreetingCannotOpenAClientSession()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3Response greeting = new Pop3Response();
        greeting.Kind = Pop3ResponseKind.Negative;
        greeting.Text = "unavailable";

        Assert.Throws<InvalidDataException>(() => machine.EnterAuthorization(greeting, false));
        Assert.That(machine.GetCurrentState(), Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(machine.GetSnapshot().LastResponse, Is.Null);
    }

    [Test]
    public void LastResponseIsTrackedByDeepCopy()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "temporarily unavailable";
        response.ResponseCodeLevels = new[] { "SYS", "TEMP" };
        response.IsMultiline = true;
        response.AdvisoryOctetCount = 99;

        machine.TrackResponse(response);
        response.Text = "changed";
        response.ResponseCodeLevels[0] = "CHANGED";
        Pop3ClientSnapshot first = machine.GetSnapshot();
        first.LastResponse!.Text = "caller mutation";
        first.LastResponse.ResponseCodeLevels[0] = "CALLER";
        Pop3ClientSnapshot second = machine.GetSnapshot();

        Assert.That(second.LastResponse, Is.Not.Null);
        Assert.That(second.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(second.LastResponse.Text, Is.EqualTo("temporarily unavailable"));
        Assert.That(second.LastResponse.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(second.LastResponse.IsMultiline, Is.True);
        Assert.That(second.LastResponse.AdvisoryOctetCount, Is.EqualTo(99));
        Assert.That(second.LastResponse, Is.Not.SameAs(first.LastResponse));
        Assert.That(second.LastResponse.ResponseCodeLevels, Is.Not.SameAs(first.LastResponse.ResponseCodeLevels));
    }

    [Test]
    public void SnapshotCollectionsAndNestedDataAreDefensiveCopies()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3Capability capability = new Pop3Capability();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        Pop3MessageInfo message = new Pop3MessageInfo();
        Pop3UniqueId uniqueId = new Pop3UniqueId();
        capability.Name = "SASL";
        capability.Parameters = new[] { "PLAIN" };
        capabilities.Entries = new[] { capability };
        capabilities.SaslMechanisms = new[] { "PLAIN" };
        capabilities.SupportsTop = true;
        capabilities.SupportsPipelining = true;
        capabilities.Implementation = "example";
        status.MessageCount = 1;
        status.TotalOctets = 50;
        message.MessageNumber = 1;
        message.OctetCount = 50;
        uniqueId.MessageNumber = 1;
        uniqueId.Value = "id-1";

        machine.EnterAuthorization(CreateGreeting(), true);
        machine.SetCapabilities(capabilities);
        machine.EnterTransaction("alice");
        machine.SetMailboxStatus(status);
        machine.SetMessages(new[] { message });
        machine.SetUniqueIds(new[] { uniqueId });

        capability.Name = "CHANGED";
        capability.Parameters[0] = "CHANGED";
        capabilities.SaslMechanisms[0] = "CHANGED";
        status.MessageCount = 99;
        message.OctetCount = 99;
        uniqueId.Value = "changed";

        Pop3ClientSnapshot first = machine.GetSnapshot();
        first.Capabilities.Entries[0].Name = "CALLER";
        first.Capabilities.Entries[0].Parameters[0] = "CALLER";
        first.Capabilities.SaslMechanisms[0] = "CALLER";
        first.MailboxStatus!.MessageCount = 88;
        first.Messages[0].OctetCount = 88;
        first.UniqueIds[0].Value = "caller";
        Pop3ClientSnapshot second = machine.GetSnapshot();

        Assert.That(second.Capabilities.SupportsTop, Is.True);
        Assert.That(second.Capabilities.SupportsPipelining, Is.True);
        Assert.That(second.Capabilities.Implementation, Is.EqualTo("example"));
        Assert.That(second.Capabilities.Entries[0].Name, Is.EqualTo("SASL"));
        Assert.That(second.Capabilities.Entries[0].Parameters, Is.EqualTo(new[] { "PLAIN" }));
        Assert.That(second.Capabilities.SaslMechanisms, Is.EqualTo(new[] { "PLAIN" }));
        Assert.That(second.MailboxStatus!.MessageCount, Is.EqualTo(1));
        Assert.That(second.MailboxStatus.TotalOctets, Is.EqualTo(50));
        Assert.That(second.Messages[0].OctetCount, Is.EqualTo(50));
        Assert.That(second.UniqueIds[0].Value, Is.EqualTo("id-1"));
        Assert.That(second.Capabilities.Entries[0], Is.Not.SameAs(first.Capabilities.Entries[0]));
        Assert.That(second.MailboxStatus, Is.Not.SameAs(first.MailboxStatus));
        Assert.That(second.Messages[0], Is.Not.SameAs(first.Messages[0]));
        Assert.That(second.UniqueIds[0], Is.Not.SameAs(first.UniqueIds[0]));
    }

    [Test]
    public void FailureInvalidationClosesAndClearsLiveSessionState()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3Response response = new Pop3Response();
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = "operation accepted";
        status.MessageCount = 0;

        machine.EnterAuthorization(CreateGreeting(), true);
        machine.EnableUtf8(true);
        machine.SelectLanguage("de");
        machine.EnterTransaction("alice");
        machine.SetMailboxStatus(status);
        machine.TrackResponse(response);
        machine.InvalidateAfterFailure();

        Pop3ClientSnapshot snapshot = machine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(snapshot.IsConnected, Is.False);
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.IsAuthenticated, Is.False);
        Assert.That(snapshot.AuthenticatedIdentity, Is.Null);
        Assert.That(snapshot.Capabilities.Entries, Is.Empty);
        Assert.That(snapshot.MailboxStatus, Is.Null);
        Assert.That(snapshot.IsUtf8Enabled, Is.False);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.False);
        Assert.That(snapshot.SelectedLanguage, Is.Null);
        Assert.That(snapshot.Messages, Is.Empty);
        Assert.That(snapshot.UniqueIds, Is.Empty);
        Assert.That(snapshot.Greeting, Is.EqualTo("server ready"));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("operation accepted"));
        AssertStateFailure(() => machine.RequireActive(Pop3Commands.Noop), Pop3Commands.Noop, Pop3SessionState.Closed);
    }

    [Test]
    public void StateSpecificUpdatesRejectIllegalStatesWithoutMutation()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();
        Pop3MailboxStatus status = new Pop3MailboxStatus();

        AssertStateFailure(() => machine.MarkSecure(), Pop3Commands.Stls, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.EnterTransaction("alice"), "Authenticate", Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.SetCapabilities(capabilities), Pop3Commands.Capa, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.SetMailboxStatus(status), Pop3Commands.Stat, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.SetMessages(new Pop3MessageInfo[0]), Pop3Commands.List, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.SetUniqueIds(new Pop3UniqueId[0]), Pop3Commands.Uidl, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.EnableUtf8(false), Pop3Commands.Utf8, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.SelectLanguage("en"), Pop3Commands.Lang, Pop3SessionState.Disconnected);
        AssertStateFailure(() => machine.EnterUpdate(), Pop3Commands.Quit, Pop3SessionState.Disconnected);
        Assert.That(machine.GetCurrentState(), Is.EqualTo(Pop3SessionState.Disconnected));
    }

    [Test]
    public void ClosedStateIsTerminalForNewConnections()
    {
        Pop3ClientStateMachine machine = new Pop3ClientStateMachine();
        machine.CloseSession();

        Assert.That(machine.GetCurrentState(), Is.EqualTo(Pop3SessionState.Closed));
        AssertStateFailure(() => machine.RequireDisconnected("Connect"), "Connect", Pop3SessionState.Closed);
        AssertStateFailure(() => machine.EnterAuthorization(CreateGreeting(), false), "Connect", Pop3SessionState.Closed);
    }

    private static Pop3Response CreateGreeting()
    {
        Pop3Response greeting = new Pop3Response();
        greeting.Kind = Pop3ResponseKind.Positive;
        greeting.Text = "server ready";
        greeting.GreetingTimestamp = "<1@example>";
        return greeting;
    }

    private static void AssertStateFailure(TestDelegate action, string operation, Pop3SessionState state)
    {
        Pop3StateException exception = Assert.Throws<Pop3StateException>(action)!;
        Assert.That(exception.Operation, Is.EqualTo(operation));
        Assert.That(exception.SessionState, Is.EqualTo(state));
    }
}
