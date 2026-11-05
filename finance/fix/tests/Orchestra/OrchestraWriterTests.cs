using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraWriterTests
{
    [Test]
    public void CanonicalRepositoryOutputIsStableAndRoundTripsAllMajorContent()
    {
        OrchestraRepository repository = CreateRepository();
        OrchestraWriteOptions options = new OrchestraWriteOptions { Indent = false };

        string first = Write(repository, options, out OrchestraReadResult firstResult);
        string second = Write(repository, options, out OrchestraReadResult secondResult);
        OrchestraReadResult roundTrip = Read(first, "canonical.xml");

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True, Codes(firstResult));
            Assert.That(secondResult.Success, Is.True, Codes(secondResult));
            Assert.That(firstResult.Repository, Is.SameAs(repository));
            Assert.That(Encoding.UTF8.GetBytes(second), Is.EqualTo(Encoding.UTF8.GetBytes(first)));
            Assert.That(first, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
            Assert.That(first, Does.Contain("xmlns:fixr=\"http://fixprotocol.io/2026/orchestra/repository\""));
            Assert.That(first.IndexOf("name=\"Integer\""), Is.LessThan(first.IndexOf("name=\"NumInGroup\"")));
            Assert.That(first.IndexOf("name=\"NumInGroup\""), Is.LessThan(first.IndexOf("name=\"String\"")));
            Assert.That(first.IndexOf("name=\"NoPartyIDs\""), Is.LessThan(first.IndexOf("name=\"Side\"")));
            Assert.That(first, Does.Contain("<fixr:rule name=\"sideRequired\" presence=\"required\""));
            Assert.That(first, Does.Contain("added=\"FIX.4.0\""));
            Assert.That(first, Does.Contain("<fixr:states name=\"OrderLifecycle\""));
            Assert.That(first, Does.Contain("<venue:settings xmlns:venue=\"urn:venue:writer\" mode=\"strict\""));
            Assert.That(roundTrip.Success, Is.True, Codes(roundTrip));
            Assert.That(roundTrip.Repository!.Metadata.Name, Is.EqualTo("Venue"));
            Assert.That(roundTrip.Repository.Fields.Select(item => item.Name), Is.EqualTo(new[] { "NoPartyIDs", "Side" }));
            Assert.That(roundTrip.Repository.Groups.Single().CountMember!.ReferenceId, Is.EqualTo("453"));
            Assert.That(roundTrip.Repository.Messages.Single().Members.Single().ReferenceId, Is.EqualTo("2000"));
            Assert.That(roundTrip.Repository.Rules.Single(item => item.Name == "sideRequired").Name, Is.EqualTo("sideRequired"));
            Assert.That(roundTrip.Repository.StateMachines.Single().States.Select(item => item.Name), Is.EqualTo(new[] { "Ready", "Done" }));
            Assert.That(roundTrip.Repository.Fields.Single(item => item.Name == "Side").Annotations.Single(item => item.Kind == "appinfo").Extensions.Single().NamespaceUri, Is.EqualTo("urn:venue:writer"));
        });
    }

    [Test]
    public void CanonicalRepositoryBytesRemainStableAfterReadingAndWritingAgain()
    {
        OrchestraWriteOptions options = new OrchestraWriteOptions { Indent = false };
        string first = Write(CreateRepository(), options, out OrchestraReadResult firstWrite);
        OrchestraReadResult read = ReadWithoutSchema(first, "first-canonical.xml");
        string second = Write(read.Repository!, options, out OrchestraReadResult secondWrite);

        Assert.Multiple(() =>
        {
            Assert.That(firstWrite.Success, Is.True, Codes(firstWrite));
            Assert.That(read.Success, Is.True, Codes(read));
            Assert.That(secondWrite.Success, Is.True, Codes(secondWrite));
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [Test]
    public void RichRepositoryRoundTripPreservesMetadataBehaviorAndExtensions()
    {
        OrchestraRepository repository = CreateRepository();
        repository.Metadata.Terms.Add(new OrchestraMetadataTerm { NamespaceUri = "urn:venue:metadata", Name = "owner", Language = "en", Value = "Trading" });
        repository.Extensions.Add(AttributeExtension("profile", "A&amp;B"));
        repository.Extensions.Add(ElementExtension("repositorySettings", "scope=\"global\""));
        repository.EncodingStandards.Add(new OrchestraEncodingStandard { Name = "SBE", DisplayName = "Simple Binary Encoding" });
        repository.Datatypes.Single(item => item.Name == "String").Mappings.Add(new OrchestraDatatypeMapping { Standard = "SBE", Base = "char" });
        repository.CodeSets.Add(new OrchestraCodeSetDefinition
        {
            Id = "10",
            Name = "SideCodeSet",
            Scenario = "base",
            Type = "String",
            DefaultValue = "1",
            Codes =
            [
                new OrchestraCodeDefinition { Id = "100", Name = "Buy", Scenario = "base", Value = "1" },
                new OrchestraCodeDefinition { Id = "101", Name = "Sell", Scenario = "base", Value = "2" }
            ]
        });
        OrchestraFieldDefinition side = repository.Fields.Single(item => item.Name == "Side");
        side.Type = "";
        side.CodeSet = "SideCodeSet";
        side.Encodings.Add(new OrchestraEncoding { Standard = "SBE" });
        side.Extensions.Add(AttributeExtension("source", "venue"));
        repository.CodeSetEncodings.Add(new OrchestraEncoding { Standard = "SBE" });
        repository.Components.Add(new OrchestraComponentDefinition
        {
            Id = "1000",
            Name = "OrderCore",
            Scenario = "base",
            Category = "Orders",
            Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 0 }]
        });
        repository.Messages.Single().Members.Add(new OrchestraMemberDefinition { Kind = "componentRef", ReferenceId = "1000", Scenario = "base", Position = 1 });
        repository.Concepts.Add(new OrchestraConcept { Name = "SideConcept", Members = [new OrchestraConceptMember { Kind = "fieldRef", Id = "54", Scenario = "base" }] });
        repository.Scenarios.Add(new OrchestraScenarioDefinition { Name = "alternate", ParentName = "base" });

        OrchestraActor buyer = repository.Actors.Single(item => item.Name == "Buyer");
        OrchestraTimer timer = new OrchestraTimer { Name = "timeout", Actor = "Buyer", Extensions = [ElementExtension("timerSettings", "unit=\"seconds\"")] };
        buyer.Timers.Add(timer);
        repository.Timers.Add(timer);
        buyer.Variables.Add(new OrchestraVariable { Kind = "fieldRef", Name = "side", ReferenceId = "54", Scenario = "base" });
        OrchestraStateMachine machine = repository.StateMachines.Single();
        machine.Variables.Add(new OrchestraVariable { Kind = "field", Id = "700", Name = "attempts", Scenario = "base", Datatype = "Integer", InitialValue = "0" });
        OrchestraState ready = machine.States.Single(item => item.Name == "Ready");
        ready.EntryActions.Add(new OrchestraAction
        {
            Kind = "timerSchedule",
            Name = "timeout",
            Actor = "Buyer",
            Operation = "START",
            Interval = "PT5S",
            Actions = [new OrchestraAction { Kind = "messageRef", ReferenceId = "3000", Name = "NewOrderSingle", MessageType = "D", Scenario = "base" }]
        });
        ready.ActivityActions.Add(new OrchestraAction { Kind = "assign", Expression = "out.Side = in.Side", AssignmentTarget = "out.Side" });
        ready.ExitActions.Add(new OrchestraAction { Kind = "trigger", Name = "send", Actor = "Buyer", StateMachine = "OrderLifecycle" });
        OrchestraTransition transition = ready.Transitions.Single();
        transition.Trigger = "NewOrderSingle";
        transition.Responses.Add(new OrchestraResponse
        {
            Name = "accepted",
            Synchronization = "asynchronous",
            Condition = "in.Side != null",
            Actions = [new OrchestraAction { Kind = "messageRef", ReferenceId = "3000", Name = "NewOrderSingle", MessageType = "D", Scenario = "base" }],
            Extensions = [ElementExtension("responseSettings", "mode=\"fast\"")]
        });
        repository.Messages.Single().Responses.Add(new OrchestraResponse { Name = "echo", MessageName = "NewOrderSingle", Scenario = "base" });

        OrchestraWriteOptions options = new OrchestraWriteOptions { Indent = false };
        string first = Write(repository, options, out OrchestraReadResult firstWrite);
        OrchestraReadResult read = ReadWithoutSchema(first, "rich-repository.xml");
        string second = Write(read.Repository!, options, out OrchestraReadResult secondWrite);
        OrchestraRepository roundTrip = read.Repository!;
        OrchestraStateMachine roundTripMachine = roundTrip.StateMachines.Single();
        OrchestraState roundTripReady = roundTripMachine.States.Single(item => item.Name == "Ready");

        Assert.Multiple(() =>
        {
            Assert.That(firstWrite.Success, Is.True, Codes(firstWrite));
            Assert.That(read.Success, Is.True, Codes(read));
            Assert.That(secondWrite.Success, Is.True, Codes(secondWrite));
            Assert.That(Encoding.UTF8.GetBytes(second), Is.EqualTo(Encoding.UTF8.GetBytes(first)));
            Assert.That(roundTrip.Metadata.Terms.Single(item => item.Name == "owner").Value, Is.EqualTo("Trading"));
            Assert.That(roundTrip.Metadata.Terms.Single(item => item.Name == "owner").Language, Is.EqualTo("en"));
            Assert.That(roundTrip.Extensions.Any(item => item.Name == "profile" && item.Xml.Contains("A&amp;B")), Is.True);
            Assert.That(roundTrip.Extensions.Any(item => item.Name == "repositorySettings"), Is.True);
            Assert.That(roundTrip.Fields.Single(item => item.Name == "Side").Extensions.Any(item => item.Name == "source"), Is.True);
            Assert.That(roundTrip.EncodingStandards.Single().Name, Is.EqualTo("SBE"));
            Assert.That(roundTrip.Datatypes.Single(item => item.Name == "String").Mappings.Single().Base, Is.EqualTo("char"));
            Assert.That(roundTrip.CodeSets.Single().Codes.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(roundTrip.CodeSets.Single().DefaultValue, Is.EqualTo("1"));
            Assert.That(roundTrip.Fields.Single(item => item.Name == "Side").CodeSet, Is.EqualTo("SideCodeSet"));
            Assert.That(roundTrip.CodeSetEncodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(roundTrip.Components.Single().Members.Single().ReferenceId, Is.EqualTo("54"));
            Assert.That(roundTrip.Messages.Single().Members.Select(item => item.ReferenceId), Is.EqualTo(new[] { "2000", "1000" }));
            Assert.That(roundTrip.Concepts.Single().Members.Single().Id, Is.EqualTo("54"));
            Assert.That(roundTrip.Scenarios.Single(item => item.Name == "alternate").ParentName, Is.EqualTo("base"));
            Assert.That(roundTrip.Actors.Single(item => item.Name == "Buyer").Variables.Single().ReferenceId, Is.EqualTo("54"));
            Assert.That(roundTrip.Actors.Single(item => item.Name == "Buyer").Timers.Single().Name, Is.EqualTo("timeout"));
            Assert.That(roundTripMachine.Variables.Single().Name, Is.EqualTo("attempts"));
            Assert.That(roundTripReady.Transitions.Single().Trigger, Is.EqualTo("NewOrderSingle"));
            Assert.That(roundTripReady.Transitions.Single().Responses.Single().Name, Is.EqualTo("accepted"));
            Assert.That(roundTripReady.EntryActions.Single().Actions.Single().MessageType, Is.EqualTo("D"));
            Assert.That(roundTripReady.ActivityActions.Single().AssignmentTarget, Is.EqualTo("out.Side"));
            Assert.That(roundTripReady.ExitActions.Single().StateMachine, Is.EqualTo("OrderLifecycle"));
            Assert.That(roundTrip.Messages.Single().Responses.Single().MessageName, Is.EqualTo("NewOrderSingle"));
            Assert.That(roundTrip.Flows.Single().Messages, Is.EqualTo(new[] { "NewOrderSingle" }));
        });
    }

    [Test]
    public void RichInterfacesRoundTripPreservesStackMetadataExtensionsAndBytes()
    {
        OrchestraRepository repository = CreateInterfacesRepository();
        repository.Metadata.Terms.Add(new OrchestraMetadataTerm { NamespaceUri = "urn:venue:metadata", Name = "owner", Value = "Connectivity" });
        repository.Extensions.Add(AttributeExtension("environment", "production"));
        repository.Extensions.Add(ElementExtension("interfaceSettings", "region=\"us-east\""));
        OrchestraInterface trading = repository.Interfaces.Single(item => item.Name == "Trading");
        trading.Extensions.Add(AttributeExtension("priority", "primary"));
        trading.UserInterfaces.Add(new OrchestraProtocol { Kind = "userInterface", Name = "TraderDesktop", Layer = "application", Version = "3" });
        trading.SessionProtocols.Add(new OrchestraProtocol { Kind = "sessionProtocol", Name = "FIXP", Layer = "session", Version = "1.0" });
        trading.Protocols.Add(new OrchestraProtocol { Kind = "protocol", Name = "SOFH", Layer = "session", Version = "1.1" });
        trading.Services.Single().Extensions.Add(ElementExtension("serviceSettings", "mode=\"orders\""));
        trading.Encodings.Single().Extensions.Add(AttributeExtension("format", "binary"));
        OrchestraProtocol transport = trading.Transports.Single();
        transport.Version = "6";
        transport.MessageCast = "unicast";
        transport.Use = "primary";
        OrchestraSession session = trading.Sessions.Single();
        session.ActivationTime = "2026-01-01T00:00:00Z";
        session.DeactivationTime = "2027-01-01T00:00:00Z";
        session.SecurityKeys = "venue-key";
        session.ServiceOverrides.Add(new OrchestraService { Name = "OrderEntry", Layer = "application", OrchestrationUri = "repository.xml" });
        session.UserInterfaceOverrides.Add(new OrchestraProtocol { Kind = "userInterface", Name = "TraderDesktop", Layer = "application" });
        session.SessionProtocolOverrides.Add(new OrchestraProtocol { Kind = "sessionProtocol", Name = "FIXP", Layer = "session" });
        session.ProtocolOverrides.Add(new OrchestraProtocol { Kind = "protocol", Name = "SOFH", Layer = "session" });
        session.Extensions.Add(ElementExtension("sessionSettings", "recovery=\"enabled\""));
        session.IdentifierEntries.Single().Extensions.Add(AttributeExtension("scope", "local"));

        OrchestraWriteOptions options = new OrchestraWriteOptions { Indent = false };
        string first = Write(repository, options, out OrchestraReadResult firstWrite);
        OrchestraReadResult read = ReadWithoutSchema(first, "rich-interfaces.xml");
        string second = Write(read.Repository!, options, out OrchestraReadResult secondWrite);
        OrchestraRepository roundTrip = read.Repository!;
        OrchestraInterface roundTripInterface = roundTrip.Interfaces.Single(item => item.Name == "Trading");
        OrchestraSession roundTripSession = roundTripInterface.Sessions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(firstWrite.Success, Is.True, Codes(firstWrite));
            Assert.That(read.Success, Is.True, Codes(read));
            Assert.That(secondWrite.Success, Is.True, Codes(secondWrite));
            Assert.That(Encoding.UTF8.GetBytes(second), Is.EqualTo(Encoding.UTF8.GetBytes(first)));
            Assert.That(roundTrip.Metadata.Terms.Single(item => item.Name == "owner").Value, Is.EqualTo("Connectivity"));
            Assert.That(roundTrip.Extensions.Any(item => item.Name == "environment"), Is.True);
            Assert.That(roundTrip.Extensions.Any(item => item.Name == "interfaceSettings"), Is.True);
            Assert.That(roundTripInterface.Extensions.Any(item => item.Name == "priority"), Is.True);
            Assert.That(roundTripInterface.UserInterfaces.Single().Name, Is.EqualTo("TraderDesktop"));
            Assert.That(roundTripInterface.SessionProtocols.Single().Name, Is.EqualTo("FIXP"));
            Assert.That(roundTripInterface.Protocols.Single().Name, Is.EqualTo("SOFH"));
            Assert.That(roundTripInterface.Encodings.Single().Extensions.Any(item => item.Name == "format"), Is.True);
            Assert.That(roundTripInterface.Services.Single().Extensions.Any(item => item.Name == "serviceSettings"), Is.True);
            Assert.That(roundTripInterface.Transports.Single().MessageCast, Is.EqualTo("unicast"));
            Assert.That(roundTripInterface.Transports.Single().Use, Is.EqualTo("primary"));
            Assert.That(roundTripSession.ServiceOverrides.Single().Name, Is.EqualTo("OrderEntry"));
            Assert.That(roundTripSession.UserInterfaceOverrides.Single().Name, Is.EqualTo("TraderDesktop"));
            Assert.That(roundTripSession.SessionProtocolOverrides.Single().Name, Is.EqualTo("FIXP"));
            Assert.That(roundTripSession.ProtocolOverrides.Single().Name, Is.EqualTo("SOFH"));
            Assert.That(roundTripSession.SecurityKeys, Is.EqualTo("venue-key"));
            Assert.That(roundTripSession.IdentifierEntries.Single().Extensions.Any(item => item.Name == "scope"), Is.True);
            Assert.That(roundTripSession.Extensions.Any(item => item.Name == "sessionSettings"), Is.True);
            Assert.That(roundTrip.RepositoryLinks, Is.EqualTo(new[] { "repository.xml" }));
        });
    }

    [Test]
    public void VersionOrderAndBehaviorOptionsControlRepositoryOutput()
    {
        OrchestraRepository repository = CreateRepository();
        OrchestraWriteOptions options = new OrchestraWriteOptions
        {
            Version = OrchestraVersion.Version10,
            CanonicalOrder = false,
            IncludeBehavior = false,
            Indent = false
        };

        string xml = Write(repository, options, out OrchestraReadResult result);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(xml, Does.Contain("xmlns:fixr=\"http://fixprotocol.io/2020/orchestra/repository\""));
            Assert.That(xml.IndexOf("name=\"NumInGroup\""), Is.LessThan(xml.IndexOf("name=\"String\"")));
            Assert.That(xml.IndexOf("name=\"String\""), Is.LessThan(xml.IndexOf("name=\"Integer\"")));
            Assert.That(xml.IndexOf("name=\"Side\""), Is.LessThan(xml.IndexOf("name=\"NoPartyIDs\"")));
            Assert.That(xml, Does.Not.Contain("<fixr:actors"));
            Assert.That(xml, Does.Not.Contain("<fixr:rule"));
            Assert.That(xml, Does.Not.Contain("<fixr:scenarios"));
        });
    }

    [Test]
    public void InterfaceOnlyRepositoryWritesCanonicalInterfacesDocumentAndRoundTrips()
    {
        OrchestraRepository repository = CreateInterfacesRepository();
        OrchestraWriteOptions options = new OrchestraWriteOptions { Indent = false };

        string xml = Write(repository, options, out OrchestraReadResult result);
        OrchestraReadResult roundTrip = Read(xml, "interfaces.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Codes(result));
            Assert.That(xml, Does.Contain("<fixi:interfaces"));
            Assert.That(xml, Does.Contain("xmlns:fixi=\"http://fixprotocol.io/2026/orchestra/interfaces\""));
            Assert.That(xml, Does.Contain("<fixi:metadata>"));
            Assert.That(xml.IndexOf("name=\"Alpha\""), Is.LessThan(xml.IndexOf("name=\"Trading\"")));
            Assert.That(xml, Does.Contain("<fixi:transport name=\"TCP\" layer=\"transport\" address=\"fix.example:9876\""));
            Assert.That(xml, Does.Contain("<fixi:identifier name=\"SenderCompID\">BUY</fixi:identifier>"));
            Assert.That(roundTrip.Success, Is.True, Codes(roundTrip));
            Assert.That(roundTrip.Repository!.Interfaces.Select(item => item.Name), Is.EqualTo(new[] { "Alpha", "Trading" }));
            Assert.That(roundTrip.Repository.Interfaces[1].Sessions.Single().Role, Is.EqualTo("initiator"));
            Assert.That(roundTrip.Repository.Interfaces[1].Sessions.Single().IdentifierEntries.Single().Value, Is.EqualTo("BUY"));
            Assert.That(roundTrip.Repository.RepositoryLinks, Is.EqualTo(new[] { "repository.xml" }));
        });
    }

    [Test]
    public void MixedContentAndInvalidOptionsProduceExplicitDiagnostics()
    {
        OrchestraRepository mixed = CreateRepository();
        mixed.Interfaces.Add(new OrchestraInterface { Name = "Trading" });
        using MemoryStream mixedDestination = new MemoryStream();
        OrchestraReadResult mixedResult = new OrchestraWriter().WriteRepository(mixedDestination, mixed, new OrchestraWriteOptions { Indent = false });
        using MemoryStream invalidDestination = new MemoryStream();
        OrchestraReadResult invalidEncoding = new OrchestraWriter().WriteRepository(invalidDestination, mixed, new OrchestraWriteOptions { EncodingName = "unsupported" });
        OrchestraReadResult invalidVersion = new OrchestraWriter().WriteRepository(new MemoryStream(), mixed, new OrchestraWriteOptions { Version = OrchestraVersion.Unknown });
        MemoryStream closed = new MemoryStream();
        closed.Dispose();
        OrchestraReadResult unwritable = new OrchestraWriter().WriteRepository(closed, mixed, new OrchestraWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mixedResult.Success, Is.True);
            Assert.That(mixedResult.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { "interfaces-not-written" }));
            Assert.That(Encoding.UTF8.GetString(mixedDestination.ToArray()), Does.StartWith("<?xml"));
            Assert.That(invalidEncoding.Success, Is.False);
            Assert.That(invalidEncoding.Diagnostics.Single().Code, Is.EqualTo("unsupported-orchestra-encoding"));
            Assert.That(invalidDestination.Length, Is.Zero);
            Assert.That(invalidVersion.Diagnostics.Single().Code, Is.EqualTo("unsupported-orchestra-write-version"));
            Assert.That(unwritable.Diagnostics.Single().Code, Is.EqualTo("unwritable-orchestra-destination"));
        });
    }

    private static OrchestraRepository CreateRepository()
    {
        OrchestraRule rule = new OrchestraRule
        {
            Id = 1,
            Name = "sideRequired",
            Kind = OrchestraRuleKind.Presence,
            Presence = FixPresence.Required,
            Expression = "in.Side != null",
            UniqueFieldIds = ["54"]
        };
        OrchestraFieldDefinition side = new OrchestraFieldDefinition
        {
            Id = "54",
            Name = "Side",
            Scenario = "base",
            Type = "String",
            Rules = [rule],
            Annotations =
            [
                new OrchestraAnnotation { Kind = "documentation", Content = "Order side" },
                new OrchestraAnnotation { Kind = "appinfo", XmlContent = "<venue:settings xmlns:venue=\"urn:venue:writer\" mode=\"strict\"/>" }
            ],
            Pedigree = new OrchestraPedigree { AddedVersion = "FIX.4.0" }
        };
        OrchestraFieldDefinition count = new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Scenario = "base", Type = "NumInGroup" };
        OrchestraGroupDefinition group = new OrchestraGroupDefinition
        {
            Id = "2000",
            Name = "Parties",
            Scenario = "base",
            Category = "Orders",
            CountMember = new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "453", Scenario = "base" },
            Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 0 }]
        };
        OrchestraMessageDefinition message = new OrchestraMessageDefinition
        {
            Id = "3000",
            Name = "NewOrderSingle",
            Scenario = "base",
            MessageType = "D",
            Category = "Orders",
            Flow = "Orders",
            Members = [new OrchestraMemberDefinition { Kind = "groupRef", ReferenceId = "2000", Scenario = "base", Position = 0 }]
        };
        OrchestraState ready = new OrchestraState
        {
            Name = "Ready",
            Transitions = [new OrchestraTransition { Name = "send", TargetState = "Done", Condition = "in.Side != null" }]
        };
        OrchestraState done = new OrchestraState { Name = "Done" };
        OrchestraStateMachine machine = new OrchestraStateMachine { Name = "OrderLifecycle", Actor = "Buyer", InitialState = "Ready", States = [ready, done] };
        OrchestraActor buyer = new OrchestraActor { Name = "Buyer", StateMachines = ["OrderLifecycle"] };
        OrchestraActor seller = new OrchestraActor { Name = "Seller" };

        return new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata
            {
                Name = "Venue",
                Release = "FIX.Latest",
                RepositoryNamespace = "urn:venue:repository",
                Terms = [new OrchestraMetadataTerm { NamespaceUri = "http://purl.org/dc/terms/", Name = "title", Value = "Venue repository" }]
            },
            Sections = [new OrchestraSection { Name = "Trade" }],
            Categories = [new OrchestraCategory { Name = "Orders", Section = "Trade" }],
            Datatypes =
            [
                new OrchestraDatatypeDefinition { Name = "NumInGroup", BaseType = "Integer" },
                new OrchestraDatatypeDefinition { Name = "String" },
                new OrchestraDatatypeDefinition { Name = "Integer" }
            ],
            Fields = [side, count],
            Groups = [group],
            Messages = [message],
            Scenarios = [new OrchestraScenarioDefinition { Name = "base" }],
            Rules = [rule],
            Actors = [seller, buyer],
            Flows = [new OrchestraFlow { Name = "Orders", SourceActor = "Buyer", DestinationActor = "Seller", Messages = ["NewOrderSingle"] }],
            StateMachines = [machine]
        };
    }

    private static OrchestraRepository CreateInterfacesRepository()
    {
        OrchestraInterface trading = new OrchestraInterface
        {
            Name = "Trading",
            Services = [new OrchestraService { Name = "OrderEntry", Layer = "application", OrchestrationUri = "repository.xml" }],
            Encodings = [new OrchestraProtocol { Kind = "encoding", Name = "SBE", Layer = "presentation" }],
            Transports = [new OrchestraProtocol { Kind = "transport", Name = "TCP", Layer = "transport", Address = "fix.example:9876" }],
            Sessions =
            [
                new OrchestraSession
                {
                    Name = "BUY-SELL",
                    Role = "initiator",
                    EncodingOverrides = [new OrchestraProtocol { Kind = "encoding", Name = "SBE", Layer = "presentation" }],
                    TransportOverrides = [new OrchestraProtocol { Kind = "transport", Name = "TCP", Layer = "transport", Address = "backup.example:9876" }],
                    IdentifierEntries = [new OrchestraIdentifier { Name = "SenderCompID", Value = "BUY" }]
                }
            ]
        };
        return new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata { Terms = [new OrchestraMetadataTerm { NamespaceUri = "http://purl.org/dc/terms/", Name = "title", Value = "Connectivity" }] },
            Interfaces = [trading, new OrchestraInterface { Name = "Alpha" }],
            RepositoryLinks = ["repository.xml"]
        };
    }

    private static OrchestraExtension AttributeExtension(string name, string encodedValue)
    {
        return new OrchestraExtension
        {
            NamespaceUri = "urn:venue:writer",
            Name = name,
            Prefix = "venue",
            Xml = "venue:" + name + "=\"" + encodedValue + "\""
        };
    }

    private static OrchestraExtension ElementExtension(string name, string attributes)
    {
        return new OrchestraExtension
        {
            NamespaceUri = "urn:venue:writer",
            Name = name,
            Prefix = "venue",
            Xml = "<venue:" + name + " xmlns:venue=\"urn:venue:writer\" " + attributes + "/>"
        };
    }

    private static string Write(OrchestraRepository repository, OrchestraWriteOptions options, out OrchestraReadResult result)
    {
        using MemoryStream destination = new MemoryStream();
        result = new OrchestraWriter().WriteRepository(destination, repository, options);
        return Encoding.UTF8.GetString(destination.ToArray());
    }

    private static OrchestraReadResult Read(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new OrchestraReader().ReadRepository(source, new OrchestraReadOptions { BaseUri = sourceName });
    }

    private static OrchestraReadResult ReadWithoutSchema(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new OrchestraReader().ReadRepository(source, new OrchestraReadOptions { BaseUri = sourceName, ValidateSchema = false });
    }

    private static string Codes(OrchestraReadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
