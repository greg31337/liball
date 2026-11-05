using System.Linq;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraSemanticValidatorTests
{
    [Test]
    public void ValidRepositoryPassesEverySemanticLayer()
    {
        OrchestraRepository repository = CreateValidRepository();
        OrchestraValidator validator = new OrchestraValidator();

        OrchestraReadResult result = validator.ValidateRepository(repository);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Repository, Is.SameAs(repository));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReportsAllRequestedSemanticViolationCategoriesInStableOrder()
    {
        OrchestraRepository repository = CreateInvalidRepository();
        OrchestraValidator validator = new OrchestraValidator();

        OrchestraReadResult first = validator.ValidateRepository(repository);
        OrchestraReadResult second = validator.ValidateRepository(repository);
        string[] codes = first.Diagnostics.Select(item => item.Code).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Repository, Is.SameAs(repository));
            Assert.That(codes, Does.Contain("duplicate-field-id"));
            Assert.That(codes, Does.Contain("duplicate-field-name"));
            Assert.That(codes, Does.Contain("unresolved-member-reference"));
            Assert.That(codes, Does.Contain("unresolved-parent-scenario"));
            Assert.That(codes, Does.Contain("scenario-inheritance-cycle"));
            Assert.That(codes, Does.Contain("invalid-member-cardinality"));
            Assert.That(codes, Does.Contain("invalid-member-position"));
            Assert.That(codes, Does.Contain("missing-group-count-field"));
            Assert.That(codes, Does.Contain("invalid-group-delimiter"));
            Assert.That(codes, Does.Contain("invalid-code-value"));
            Assert.That(codes, Does.Contain("missing-rule-expression"));
            Assert.That(codes, Does.Contain("unresolved-rule-field"));
            Assert.That(codes, Does.Contain("unregistered-rule"));
            Assert.That(codes, Does.Contain("unresolved-flow-destination"));
            Assert.That(codes, Does.Contain("unresolved-transition-target"));
            Assert.That(codes, Does.Contain("unresolved-timer-reference"));
            Assert.That(codes, Does.Contain("missing-transport-address"));
            Assert.That(codes, Does.Contain("unresolved-protocol-override"));
            Assert.That(codes, Does.Contain("duplicate-session-identifier"));
            Assert.That(codes, Does.Contain("invalid-repository-link"));
            Assert.That(codes, Does.Contain("duplicate-repository-link"));
            Assert.That(second.Diagnostics.Select(item => item.Code), Is.EqualTo(codes));
            Assert.That(first.Diagnostics.All(item => item.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(first.Diagnostics.All(item => item.ElementPath.StartsWith("/")), Is.True);
        });
    }

    [Test]
    public void MissingRepositoryProducesExplicitFailure()
    {
        OrchestraValidator validator = new OrchestraValidator();

        OrchestraReadResult result = validator.ValidateRepository(null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Repository, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("missing-orchestra-repository"));
        });
    }

    internal static OrchestraRepository CreateValidRepository()
    {
        OrchestraScenarioDefinition scenario = new OrchestraScenarioDefinition { Name = "base" };
        OrchestraDatatypeDefinition stringType = new OrchestraDatatypeDefinition { Name = "String" };
        OrchestraDatatypeDefinition integerType = new OrchestraDatatypeDefinition { Name = "Integer" };
        OrchestraDatatypeDefinition countType = new OrchestraDatatypeDefinition { Name = "NumInGroup", BaseType = "Integer" };
        OrchestraCodeSetDefinition sideSet = new OrchestraCodeSetDefinition
        {
            Id = "10",
            Name = "SideCodeSet",
            Scenario = "base",
            Type = "String",
            DefaultValue = "1",
            Codes = [new OrchestraCodeDefinition { Id = "100", Name = "Buy", Scenario = "base", Value = "1" }]
        };
        OrchestraFieldDefinition countField = new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Scenario = "base", Type = "NumInGroup" };
        OrchestraFieldDefinition sideField = new OrchestraFieldDefinition { Id = "54", Name = "Side", Scenario = "base", CodeSet = "SideCodeSet" };
        OrchestraRule sideRule = new OrchestraRule { Id = 1, Name = "sidePresent", Kind = OrchestraRuleKind.Presence, Presence = FixPresence.Required, PresenceText = "required", Expression = "in.Side != null", UniqueFieldIds = ["54"] };
        sideField.Rules.Add(sideRule);
        OrchestraComponentDefinition component = new OrchestraComponentDefinition
        {
            Id = "1000",
            Name = "OrderCore",
            Scenario = "base",
            Category = "Orders",
            Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 0 }]
        };
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
            Members =
            [
                new OrchestraMemberDefinition { Kind = "componentRef", ReferenceId = "1000", Scenario = "base", Position = 0 },
                new OrchestraMemberDefinition { Kind = "groupRef", ReferenceId = "2000", Scenario = "base", Position = 1, ImplementationMinimum = "0", ImplementationMaximum = "10" }
            ]
        };
        OrchestraTimer timer = new OrchestraTimer { Name = "timeout", Actor = "Buyer" };
        OrchestraAction timerAction = new OrchestraAction { Kind = "timerSchedule", Name = "timeout", Actor = "Buyer", Operation = "START", Interval = "PT5S" };
        OrchestraAction messageAction = new OrchestraAction { Kind = "messageRef", Name = "NewOrderSingle", ReferenceId = "3000", Scenario = "base", MessageType = "D" };
        OrchestraState ready = new OrchestraState { Name = "Ready", Transitions = [new OrchestraTransition { Name = "send", TargetState = "Done" }], EntryActions = [timerAction], ActivityActions = [messageAction] };
        OrchestraState done = new OrchestraState { Name = "Done" };
        OrchestraStateMachine machine = new OrchestraStateMachine { Name = "OrderLifecycle", Actor = "Buyer", InitialState = "Ready", States = [ready, done] };
        OrchestraActor buyer = new OrchestraActor { Name = "Buyer", Timers = [timer], StateMachines = ["OrderLifecycle"] };
        OrchestraActor seller = new OrchestraActor { Name = "Seller" };
        OrchestraFlow flow = new OrchestraFlow { Name = "Orders", SourceActor = "Buyer", DestinationActor = "Seller", Messages = ["NewOrderSingle"] };

        OrchestraService service = new OrchestraService { Name = "OrderEntry", Layer = "application", OrchestrationUri = "repository.xml" };
        OrchestraProtocol encoding = new OrchestraProtocol { Kind = "encoding", Name = "SBE", Layer = "presentation" };
        OrchestraProtocol sessionProtocol = new OrchestraProtocol { Kind = "sessionProtocol", Name = "FIXP", Layer = "session" };
        OrchestraProtocol framing = new OrchestraProtocol { Kind = "protocol", Name = "SOFH", Layer = "session" };
        OrchestraProtocol transport = new OrchestraProtocol { Kind = "transport", Name = "TCP", Layer = "transport", Address = "fix.example:9876" };
        OrchestraSession session = new OrchestraSession
        {
            Name = "BUY-SELL",
            Role = "initiator",
            ServiceOverrides = [new OrchestraService { Name = "OrderEntry", Layer = "application", OrchestrationUri = "repository.xml" }],
            EncodingOverrides = [new OrchestraProtocol { Kind = "encoding", Name = "SBE", Layer = "presentation" }],
            SessionProtocolOverrides = [new OrchestraProtocol { Kind = "sessionProtocol", Name = "FIXP", Layer = "session" }],
            ProtocolOverrides = [new OrchestraProtocol { Kind = "protocol", Name = "SOFH", Layer = "session" }],
            TransportOverrides = [new OrchestraProtocol { Kind = "transport", Name = "TCP", Layer = "transport", Address = "backup.example:9876" }],
            IdentifierEntries = [new OrchestraIdentifier { Name = "SenderCompID", Value = "BUY" }]
        };
        OrchestraInterface orchestraInterface = new OrchestraInterface
        {
            Name = "Trading",
            Services = [service],
            Encodings = [encoding],
            SessionProtocols = [sessionProtocol],
            Protocols = [framing],
            Transports = [transport],
            Sessions = [session]
        };

        return new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Scenarios = [scenario],
            Sections = [new OrchestraSection { Name = "Trade" }],
            Categories = [new OrchestraCategory { Name = "Orders", Section = "Trade" }],
            Datatypes = [stringType, integerType, countType],
            CodeSets = [sideSet],
            Fields = [countField, sideField],
            Components = [component],
            Groups = [group],
            Messages = [message],
            Rules = [sideRule],
            Actors = [buyer, seller],
            Timers = [timer],
            StateMachines = [machine],
            Flows = [flow],
            Interfaces = [orchestraInterface],
            RepositoryLinks = ["repository.xml"]
        };
    }

    private static OrchestraRepository CreateInvalidRepository()
    {
        OrchestraScenarioDefinition baseScenario = new OrchestraScenarioDefinition { Name = "base", ParentName = "alternate" };
        OrchestraScenarioDefinition alternateScenario = new OrchestraScenarioDefinition { Name = "alternate", ParentName = "base" };
        OrchestraScenarioDefinition orphanScenario = new OrchestraScenarioDefinition { Name = "orphan", ParentName = "missing" };
        OrchestraDatatypeDefinition integerType = new OrchestraDatatypeDefinition { Name = "Integer" };
        OrchestraDatatypeDefinition booleanType = new OrchestraDatatypeDefinition { Name = "Boolean" };
        OrchestraCodeSetDefinition codeSet = new OrchestraCodeSetDefinition
        {
            Id = "1",
            Name = "FlagCodeSet",
            Scenario = "base",
            Type = "Boolean",
            Codes = [new OrchestraCodeDefinition { Id = "1", Name = "Enabled", Value = "yes" }]
        };
        OrchestraFieldDefinition firstField = new OrchestraFieldDefinition { Id = "1", Name = "Duplicate", Scenario = "base", Type = "Integer" };
        OrchestraFieldDefinition secondField = new OrchestraFieldDefinition { Id = "1", Name = "Duplicate", Scenario = "base", Type = "Integer" };
        OrchestraRule registeredRule = new OrchestraRule { Id = 1, Name = "broken", Kind = OrchestraRuleKind.Conditional, Expression = "", UniqueFieldIds = ["999"] };
        OrchestraRule unregisteredRule = new OrchestraRule { Id = 2, Name = "detached", Kind = OrchestraRuleKind.Assignment, Expression = "value", AssignmentTarget = "target" };
        firstField.Rules.Add(unregisteredRule);
        OrchestraMemberDefinition badMember = new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "999", Scenario = "base", Position = 4, ImplementationMinimum = "3", ImplementationMaximum = "1" };
        OrchestraComponentDefinition component = new OrchestraComponentDefinition { Id = "10", Name = "BrokenComponent", Scenario = "base", Members = [badMember] };
        OrchestraGroupDefinition group = new OrchestraGroupDefinition
        {
            Id = "20",
            Name = "BrokenGroup",
            Scenario = "base",
            Members = [new OrchestraMemberDefinition { Kind = "componentRef", ReferenceId = "10", Scenario = "base", Position = 0 }]
        };
        OrchestraMessageDefinition message = new OrchestraMessageDefinition { Id = "30", Name = "BrokenMessage", Scenario = "base", MessageType = "Z", Flow = "missingFlow" };
        OrchestraAction timerAction = new OrchestraAction { Kind = "timerSchedule", Actor = "Buyer", Name = "missingTimer", Operation = "START" };
        OrchestraState state = new OrchestraState { Name = "Start", Transitions = [new OrchestraTransition { Name = "go", TargetState = "Missing" }], EntryActions = [timerAction] };
        OrchestraStateMachine machine = new OrchestraStateMachine { Name = "BrokenMachine", Actor = "Buyer", InitialState = "Unknown", States = [state] };
        OrchestraActor buyer = new OrchestraActor { Name = "Buyer", StateMachines = ["BrokenMachine"] };
        OrchestraFlow flow = new OrchestraFlow { Name = "BadFlow", SourceActor = "Buyer", DestinationActor = "MissingActor", Messages = ["BrokenMessage"] };

        OrchestraProtocol encoding = new OrchestraProtocol { Kind = "encoding", Name = "SBE", Layer = "presentation", OrchestrationUri = "bad link" };
        OrchestraProtocol transport = new OrchestraProtocol { Kind = "transport", Name = "TCP", Layer = "transport", Address = "" };
        OrchestraSession session = new OrchestraSession
        {
            Name = "BadSession",
            Role = "initiator",
            EncodingOverrides = [new OrchestraProtocol { Kind = "encoding", Name = "JSON", Layer = "presentation" }],
            IdentifierEntries = [new OrchestraIdentifier { Name = "SenderCompID", Value = "A" }, new OrchestraIdentifier { Name = "SenderCompID", Value = "B" }]
        };
        OrchestraInterface orchestraInterface = new OrchestraInterface { Name = "BrokenInterface", Encodings = [encoding], Transports = [transport], Sessions = [session] };

        return new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Scenarios = [baseScenario, alternateScenario, orphanScenario],
            Datatypes = [integerType, booleanType],
            CodeSets = [codeSet],
            Fields = [firstField, secondField],
            Components = [component],
            Groups = [group],
            Messages = [message],
            Rules = [registeredRule],
            Actors = [buyer],
            StateMachines = [machine],
            Flows = [flow],
            Interfaces = [orchestraInterface],
            RepositoryLinks = ["bad link", "bad link"]
        };
    }
}
