namespace pop3.Tests;

public class Pop3CoreDataTests
{
    [Test]
    public void CommandFieldsHaveUsableDefaultsAndRoundTrip()
    {
        Pop3Command command = new Pop3Command();

        Assert.That(command.Keyword, Is.Empty);
        Assert.That(command.Arguments, Is.Empty);
        Assert.That(command.ContainsSensitiveData, Is.False);
        Assert.That(command.ExpectsMultilineResponse, Is.False);

        command.Keyword = Pop3Commands.Retr;
        command.Arguments = new[] { "7" };
        command.ContainsSensitiveData = true;
        command.ExpectsMultilineResponse = true;

        Assert.That(command.Keyword, Is.EqualTo("RETR"));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "7" }));
        Assert.That(command.ContainsSensitiveData, Is.True);
        Assert.That(command.ExpectsMultilineResponse, Is.True);
    }

    [Test]
    public void ResponseFieldsHaveUsableDefaultsAndRoundTrip()
    {
        Pop3Response response = new Pop3Response();

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(response.Text, Is.Empty);
        Assert.That(response.ResponseCodeLevels, Is.Empty);
        Assert.That(response.GreetingTimestamp, Is.Null);
        Assert.That(response.IsMultiline, Is.False);
        Assert.That(response.AdvisoryOctetCount, Is.EqualTo(-1));

        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "maildrop unavailable";
        response.ResponseCodeLevels = new[] { "SYS", "TEMP" };
        response.GreetingTimestamp = "<1.2@example.test>";
        response.IsMultiline = true;
        response.AdvisoryOctetCount = 4096;

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(response.Text, Is.EqualTo("maildrop unavailable"));
        Assert.That(response.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(response.GreetingTimestamp, Is.EqualTo("<1.2@example.test>"));
        Assert.That(response.IsMultiline, Is.True);
        Assert.That(response.AdvisoryOctetCount, Is.EqualTo(4096));
    }

    [Test]
    public void CapabilityFieldsPreserveRawAndInterpretedValues()
    {
        Pop3Capability capability = new Pop3Capability();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();

        Assert.That(capability.Name, Is.Empty);
        Assert.That(capability.Parameters, Is.Empty);
        Assert.That(capabilities.Entries, Is.Empty);
        Assert.That(capabilities.SaslMechanisms, Is.Empty);
        Assert.That(capabilities.Implementation, Is.Null);
        Assert.That(capabilities.SupportsTop, Is.False);
        Assert.That(capabilities.SupportsUser, Is.False);
        Assert.That(capabilities.SupportsResponseCodes, Is.False);
        Assert.That(capabilities.HasLoginDelay, Is.False);
        Assert.That(capabilities.LoginDelaySeconds, Is.Zero);
        Assert.That(capabilities.LoginDelayPerUser, Is.False);
        Assert.That(capabilities.SupportsPipelining, Is.False);
        Assert.That(capabilities.HasExpire, Is.False);
        Assert.That(capabilities.ExpireNever, Is.False);
        Assert.That(capabilities.ExpireDays, Is.Zero);
        Assert.That(capabilities.ExpirePerUser, Is.False);
        Assert.That(capabilities.SupportsUidl, Is.False);
        Assert.That(capabilities.SupportsStls, Is.False);
        Assert.That(capabilities.SupportsAuthenticationResponseCode, Is.False);
        Assert.That(capabilities.SupportsUtf8, Is.False);
        Assert.That(capabilities.SupportsUtf8User, Is.False);
        Assert.That(capabilities.SupportsLang, Is.False);

        capability.Name = Pop3Capabilities.LoginDelay;
        capability.Parameters = new[] { "300", "USER" };
        capabilities.Entries = new[] { capability };
        capabilities.SupportsTop = true;
        capabilities.SupportsUser = true;
        capabilities.SaslMechanisms = new[] { "SCRAM-SHA-256", "PLAIN" };
        capabilities.SupportsResponseCodes = true;
        capabilities.HasLoginDelay = true;
        capabilities.LoginDelaySeconds = 300;
        capabilities.LoginDelayPerUser = true;
        capabilities.SupportsPipelining = true;
        capabilities.HasExpire = true;
        capabilities.ExpireNever = true;
        capabilities.ExpireDays = 30;
        capabilities.ExpirePerUser = true;
        capabilities.SupportsUidl = true;
        capabilities.Implementation = "example-pop3";
        capabilities.SupportsStls = true;
        capabilities.SupportsAuthenticationResponseCode = true;
        capabilities.SupportsUtf8 = true;
        capabilities.SupportsUtf8User = true;
        capabilities.SupportsLang = true;

        Assert.That(capabilities.Entries, Is.EqualTo(new[] { capability }));
        Assert.That(capability.Name, Is.EqualTo(Pop3Capabilities.LoginDelay));
        Assert.That(capabilities.Entries[0].Parameters, Is.EqualTo(new[] { "300", "USER" }));
        Assert.That(capabilities.SaslMechanisms, Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(capabilities.LoginDelaySeconds, Is.EqualTo(300));
        Assert.That(capabilities.LoginDelayPerUser, Is.True);
        Assert.That(capabilities.ExpireDays, Is.EqualTo(30));
        Assert.That(capabilities.ExpirePerUser, Is.True);
        Assert.That(capabilities.Implementation, Is.EqualTo("example-pop3"));
        Assert.That(capabilities.SupportsTop, Is.True);
        Assert.That(capabilities.SupportsUser, Is.True);
        Assert.That(capabilities.SupportsResponseCodes, Is.True);
        Assert.That(capabilities.HasLoginDelay, Is.True);
        Assert.That(capabilities.SupportsPipelining, Is.True);
        Assert.That(capabilities.HasExpire, Is.True);
        Assert.That(capabilities.ExpireNever, Is.True);
        Assert.That(capabilities.SupportsUidl, Is.True);
        Assert.That(capabilities.SupportsStls, Is.True);
        Assert.That(capabilities.SupportsAuthenticationResponseCode, Is.True);
        Assert.That(capabilities.SupportsUtf8, Is.True);
        Assert.That(capabilities.SupportsUtf8User, Is.True);
        Assert.That(capabilities.SupportsLang, Is.True);
    }

    [Test]
    public void MailboxDataDefaultsAndFieldsRoundTrip()
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        Pop3MessageInfo message = new Pop3MessageInfo();
        Pop3UniqueId uniqueId = new Pop3UniqueId();

        Assert.That(status.MessageCount, Is.Zero);
        Assert.That(status.TotalOctets, Is.Zero);
        Assert.That(message.MessageNumber, Is.Zero);
        Assert.That(message.OctetCount, Is.Zero);
        Assert.That(message.IsDeleted, Is.False);
        Assert.That(message.IsUtf8Representation, Is.False);
        Assert.That(uniqueId.MessageNumber, Is.Zero);
        Assert.That(uniqueId.Value, Is.Empty);

        status.MessageCount = 1;
        status.TotalOctets = 12345678901;
        message.MessageNumber = 1;
        message.OctetCount = 12345678901;
        message.IsDeleted = true;
        message.IsUtf8Representation = true;
        uniqueId.MessageNumber = 1;
        uniqueId.Value = "stable-id";

        Assert.That(status.MessageCount, Is.EqualTo(1));
        Assert.That(status.TotalOctets, Is.EqualTo(12345678901));
        Assert.That(message.MessageNumber, Is.EqualTo(1));
        Assert.That(message.OctetCount, Is.EqualTo(12345678901));
        Assert.That(message.IsDeleted, Is.True);
        Assert.That(message.IsUtf8Representation, Is.True);
        Assert.That(uniqueId.MessageNumber, Is.EqualTo(1));
        Assert.That(uniqueId.Value, Is.EqualTo("stable-id"));
    }

    [Test]
    public void ClientSnapshotOwnsEmptyCoreCollectionsByDefault()
    {
        Pop3ClientSnapshot first = new Pop3ClientSnapshot();
        Pop3ClientSnapshot second = new Pop3ClientSnapshot();

        Assert.That(first.State, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(first.IsConnected, Is.False);
        Assert.That(first.IsSecure, Is.False);
        Assert.That(first.IsAuthenticated, Is.False);
        Assert.That(first.AuthenticatedIdentity, Is.Null);
        Assert.That(first.Greeting, Is.Null);
        Assert.That(first.LastResponse, Is.Null);
        Assert.That(first.Capabilities, Is.Not.Null);
        Assert.That(first.Capabilities.Entries, Is.Empty);
        Assert.That(first.MailboxStatus, Is.Null);
        Assert.That(first.IsUtf8Enabled, Is.False);
        Assert.That(first.IsUtf8UserEnabled, Is.False);
        Assert.That(first.SelectedLanguage, Is.Null);
        Assert.That(first.Messages, Is.Empty);
        Assert.That(first.UniqueIds, Is.Empty);
        Assert.That(first.Capabilities, Is.Not.SameAs(second.Capabilities));
        Assert.That(first.Messages, Is.Not.SameAs(second.Messages));
        Assert.That(first.UniqueIds, Is.Not.SameAs(second.UniqueIds));
    }

    [Test]
    public void ClientSnapshotReferenceAndCollectionFieldsRoundTrip()
    {
        Pop3ClientSnapshot snapshot = new Pop3ClientSnapshot();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        Pop3MessageInfo[] messages = new[] { new Pop3MessageInfo() };
        Pop3UniqueId[] uniqueIds = new[] { new Pop3UniqueId() };

        snapshot.Capabilities = capabilities;
        snapshot.MailboxStatus = status;
        snapshot.Messages = messages;
        snapshot.UniqueIds = uniqueIds;

        Assert.That(snapshot.Capabilities, Is.SameAs(capabilities));
        Assert.That(snapshot.MailboxStatus, Is.SameAs(status));
        Assert.That(snapshot.Messages, Is.SameAs(messages));
        Assert.That(snapshot.UniqueIds, Is.SameAs(uniqueIds));
    }
}
