using System.Linq;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraSemanticValidationCoverageTests
{
    [Test]
    public void ReportsEveryDefinitionAndWorkflowCollision()
    {
        OrchestraRepository repository = OrchestraSemanticValidatorTests.CreateValidRepository();
        repository.Datatypes.Add(new OrchestraDatatypeDefinition { Name = "String" });
        repository.EncodingStandards.Add(new OrchestraEncodingStandard { Name = "Wire" });
        repository.EncodingStandards.Add(new OrchestraEncodingStandard { Name = "Wire" });
        repository.Scenarios.Add(new OrchestraScenarioDefinition { Name = "base" });
        OrchestraCodeDefinition originalCode = repository.CodeSets[0].Codes[0];
        repository.CodeSets[0].Codes.Add(new OrchestraCodeDefinition { Id = originalCode.Id, Name = originalCode.Name, Scenario = "base", Value = originalCode.Value });
        repository.CodeSets.Add(new OrchestraCodeSetDefinition { Id = "10", Name = "SideCodeSet", Scenario = "base", Type = "String" });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Scenario = "base", Type = "NumInGroup" });
        repository.Components.Add(new OrchestraComponentDefinition { Id = "1000", Name = "OrderCore", Scenario = "base", Category = "Orders" });
        repository.Groups.Add(CreateGroup("2000", "Parties", "453", "54"));
        repository.Messages.Add(new OrchestraMessageDefinition { Id = "3000", Name = "NewOrderSingle", Scenario = "base", MessageType = "D", Category = "Orders", Flow = "Orders" });
        repository.Rules.Add(new OrchestraRule { Id = 1, Name = "duplicateRule", Kind = OrchestraRuleKind.Assignment, AssignmentTarget = "out.Side" });

        OrchestraActor buyer = repository.Actors[0];
        buyer.Timers.Add(new OrchestraTimer { Name = "timeout", Actor = "Buyer" });
        buyer.Variables.Add(new OrchestraVariable { Name = "sequence" });
        buyer.Variables.Add(new OrchestraVariable { Name = "sequence" });
        repository.Actors.Add(new OrchestraActor { Name = "Buyer" });
        repository.Flows.Add(new OrchestraFlow { Name = "Orders", SourceActor = "Buyer", DestinationActor = "Seller", Messages = ["NewOrderSingle"] });

        OrchestraStateMachine machine = repository.StateMachines[0];
        machine.States[0].Transitions.Add(new OrchestraTransition { Name = "send", TargetState = "Done" });
        machine.States.Add(new OrchestraState { Name = "Ready" });
        repository.StateMachines.Add(new OrchestraStateMachine { Name = "OrderLifecycle", Actor = "Buyer", InitialState = "Done", States = [new OrchestraState { Name = "Done" }] });

        OrchestraInterface orchestraInterface = repository.Interfaces[0];
        orchestraInterface.Services.Add(new OrchestraService { Name = "OrderEntry", Layer = "application" });
        orchestraInterface.Encodings.Add(new OrchestraProtocol { Name = "SBE", Kind = "encoding", Layer = "presentation" });
        orchestraInterface.Sessions.Add(new OrchestraSession { Name = "BUY-SELL" });
        repository.Interfaces.Add(new OrchestraInterface { Name = "Trading" });
        repository.RepositoryLinks.Add("repository.xml");

        OrchestraReadResult result = new OrchestraValidator().ValidateRepository(repository);

        AssertCodes(result,
            "duplicate-datatype-name",
            "duplicate-encoding-standard-name",
            "duplicate-scenario-name",
            "duplicate-code-set-id",
            "duplicate-code-set-name",
            "duplicate-code-id",
            "duplicate-code-name",
            "duplicate-code-value",
            "duplicate-field-id",
            "duplicate-field-name",
            "duplicate-component-id",
            "duplicate-component-name",
            "duplicate-group-id",
            "duplicate-group-name",
            "duplicate-message-id",
            "duplicate-message-name",
            "duplicate-message-type",
            "duplicate-rule-id",
            "duplicate-actor-name",
            "duplicate-timer-name",
            "duplicate-variable-name",
            "duplicate-flow-name",
            "duplicate-state-machine-name",
            "duplicate-state-name",
            "duplicate-transition-name",
            "duplicate-interface-name",
            "duplicate-service-name",
            "duplicate-protocol-name",
            "duplicate-session-name",
            "duplicate-repository-link");
    }

    [Test]
    public void ReportsEveryDefinitionReferenceAndTypeRule()
    {
        OrchestraRepository repository = OrchestraSemanticValidatorTests.CreateValidRepository();
        repository.Datatypes.Add(new OrchestraDatatypeDefinition
        {
            Name = "BrokenAlias",
            BaseType = "MissingBase",
            Mappings = [new OrchestraDatatypeMapping { Standard = "MissingEncoding" }]
        });
        repository.Categories.Add(new OrchestraCategory { Name = "BrokenCategory", Section = "MissingSection" });
        repository.CodeSetEncodings.Add(new OrchestraEncoding { Standard = "MissingEncoding" });
        repository.CodeSets.Add(new OrchestraCodeSetDefinition { Id = "20", Name = "MissingTypeSet", Scenario = "base", Type = "MissingType" });
        repository.Datatypes.Add(new OrchestraDatatypeDefinition { Name = "Boolean" });
        repository.CodeSets.Add(new OrchestraCodeSetDefinition
        {
            Id = "21",
            Name = "BrokenBooleanSet",
            Scenario = "base",
            Type = "Boolean",
            DefaultValue = "missing",
            Codes = [new OrchestraCodeDefinition { Id = "1", Name = "Invalid", Value = "yes" }]
        });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "600", Name = "MissingType", Scenario = "base" });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "601", Name = "AmbiguousType", Scenario = "base", Type = "String", CodeSet = "SideCodeSet" });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "602", Name = "UnknownType", Scenario = "base", Type = "MissingType" });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "603", Name = "UnknownCodeSet", Scenario = "base", CodeSet = "MissingCodeSet" });
        repository.Fields.Add(new OrchestraFieldDefinition { Id = "604", Name = "BrokenRelations", Scenario = "base", Type = "String", LengthId = "900", DiscriminatorId = "901" });

        OrchestraComponentDefinition component = new OrchestraComponentDefinition
        {
            Id = "4000",
            Name = "BrokenReferences",
            Scenario = "base",
            Category = "MissingCategory",
            Members =
            [
                new OrchestraMemberDefinition { Kind = "unknownRef", ReferenceId = "1", Scenario = "base", Position = 0 },
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "900", Scenario = "base", Position = 1 },
                new OrchestraMemberDefinition { Kind = "componentRef", ReferenceId = "901", Scenario = "base", Position = 2 },
                new OrchestraMemberDefinition { Kind = "groupRef", ReferenceId = "902", Scenario = "base", Position = 3 },
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 4, LengthId = "903" }
            ]
        };
        repository.Components.Add(component);
        repository.Components.Add(new OrchestraComponentDefinition { Id = "4001", Name = "UnknownScenario", Scenario = "missing" });
        repository.Components.Add(new OrchestraComponentDefinition { Id = "4002", Name = "UnknownParent", Scenario = "base", Attributes = { ["scenarioRef"] = "missing" } });
        repository.Components.Add(new OrchestraComponentDefinition { Id = "4003", Name = "SelfParent", Scenario = "base", Attributes = { ["scenarioRef"] = "base" } });
        repository.Messages[0].Category = "MissingCategory";
        repository.Messages[0].Flow = "MissingFlow";
        repository.Concepts.Add(new OrchestraConcept
        {
            Name = "BrokenConcept",
            Members =
            [
                new OrchestraConceptMember { Kind = "fieldRef", Id = "900", Scenario = "base" },
                new OrchestraConceptMember { Kind = "componentRef", Id = "901", Scenario = "base" },
                new OrchestraConceptMember { Kind = "groupRef", Id = "902", Scenario = "base" },
                new OrchestraConceptMember { Kind = "messageRef", Id = "903", Scenario = "base", Name = "MissingMessage" }
            ]
        });

        OrchestraReadResult result = new OrchestraValidator().ValidateRepository(repository);

        AssertCodes(result,
            "unresolved-datatype-base",
            "unresolved-encoding-standard",
            "unresolved-section-reference",
            "unresolved-code-type",
            "missing-field-type",
            "ambiguous-field-type",
            "unresolved-field-type",
            "unresolved-code-set-reference",
            "unresolved-length-field",
            "unresolved-field-discriminator",
            "unresolved-category-reference",
            "unresolved-flow-reference",
            "unknown-member-kind",
            "unresolved-member-reference",
            "unresolved-concept-member",
            "unresolved-scenario",
            "unresolved-parent-scenario",
            "scenario-inheritance-cycle",
            "invalid-code-value",
            "invalid-code-set-default");
    }

    [Test]
    public void ReportsEveryMemberAndGroupCardinalityRule()
    {
        OrchestraRepository repository = OrchestraSemanticValidatorTests.CreateValidRepository();
        repository.Components.Add(new OrchestraComponentDefinition
        {
            Id = "4100",
            Name = "BrokenCardinality",
            Scenario = "base",
            Members =
            [
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 5, ImplementationMinimum = "bad" },
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 1, ImplementationMaximum = "bad" },
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 2, ImplementationMinimum = "3", ImplementationMaximum = "1" },
                new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base", Position = 3, Presence = FixPresence.Required, ImplementationMaximum = "0" }
            ]
        });
        repository.Groups.Add(new OrchestraGroupDefinition { Id = "4200", Name = "MissingStructure", Scenario = "base" });
        repository.Groups.Add(new OrchestraGroupDefinition
        {
            Id = "4201",
            Name = "UnresolvedCount",
            Scenario = "base",
            CountMember = new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "999", Scenario = "base" },
            Members = [new OrchestraMemberDefinition { Kind = "componentRef", ReferenceId = "999", Scenario = "base", Position = 0 }]
        });
        repository.Groups.Add(CreateGroup("4202", "InvalidCountType", "54", "54"));
        OrchestraGroupDefinition invalidMinimum = CreateGroup("4203", "InvalidMinimum", "453", "54");
        invalidMinimum.Attributes["implMinOccurs"] = "bad";
        repository.Groups.Add(invalidMinimum);
        OrchestraGroupDefinition invalidMaximum = CreateGroup("4204", "InvalidMaximum", "453", "54");
        invalidMaximum.Attributes["implMaxOccurs"] = "bad";
        repository.Groups.Add(invalidMaximum);
        OrchestraGroupDefinition invalidBounds = CreateGroup("4205", "InvalidBounds", "453", "54");
        invalidBounds.Attributes["implMinOccurs"] = "4";
        invalidBounds.Attributes["implMaxOccurs"] = "2";
        repository.Groups.Add(invalidBounds);

        OrchestraReadResult result = new OrchestraValidator().ValidateRepository(repository);

        AssertCodes(result,
            "invalid-member-position",
            "invalid-member-minimum",
            "invalid-member-maximum",
            "invalid-member-cardinality",
            "contradictory-member-cardinality",
            "missing-group-count-field",
            "unresolved-group-count-field",
            "invalid-group-count-type",
            "missing-group-delimiter",
            "invalid-group-delimiter",
            "invalid-group-minimum",
            "invalid-group-maximum",
            "invalid-group-cardinality");
    }

    [Test]
    public void ReportsEveryRuleAndWorkflowReferenceRule()
    {
        OrchestraRepository repository = OrchestraSemanticValidatorTests.CreateValidRepository();
        repository.Rules.Add(new OrchestraRule { Id = 10, Name = "missingExpression", Kind = OrchestraRuleKind.Conditional });
        repository.Rules.Add(new OrchestraRule { Id = 11, Name = "missingTarget", Kind = OrchestraRuleKind.Assignment });
        repository.Rules.Add(new OrchestraRule { Id = 12, Name = "missingReferences", Kind = OrchestraRuleKind.Assignment, AssignmentTarget = "out.Value", Type = "MissingType", UniqueFieldIds = ["999"] });
        repository.Fields[0].Rules.Add(new OrchestraRule { Id = 13, Name = "detached", Kind = OrchestraRuleKind.Assignment, AssignmentTarget = "out.Value" });
        repository.Flows.Add(new OrchestraFlow { Name = "BrokenFlow", SourceActor = "MissingSource", DestinationActor = "MissingDestination", Messages = ["MissingMessage"] });
        repository.StateMachines.Add(new OrchestraStateMachine
        {
            Name = "BrokenMachine",
            Actor = "MissingActor",
            InitialState = "MissingInitial",
            States = [new OrchestraState { Name = "Only", Transitions = [new OrchestraTransition { Name = "leave", TargetState = "MissingTarget" }] }]
        });

        OrchestraResponse response = new OrchestraResponse
        {
            Name = "BrokenResponse",
            MessageName = "MissingMessage",
            Actions =
            [
                new OrchestraAction { Kind = "fieldRef", ReferenceId = "900", Scenario = "base" },
                new OrchestraAction { Kind = "componentRef", ReferenceId = "901", Scenario = "base" },
                new OrchestraAction { Kind = "groupRef", ReferenceId = "902", Scenario = "base" },
                new OrchestraAction { Kind = "messageRef", ReferenceId = "903", Name = "MissingMessage", Scenario = "base" },
                new OrchestraAction { Kind = "trigger", Actor = "Buyer", StateMachine = "MissingMachine", Name = "send" },
                new OrchestraAction { Kind = "trigger", Actor = "Buyer", StateMachine = "OrderLifecycle", Name = "missingTransition" },
                new OrchestraAction { Kind = "timerSchedule", Actor = "Buyer", Name = "missingTimer", Operation = "PAUSE" }
            ]
        };
        repository.Messages[0].Responses.Add(response);

        OrchestraReadResult result = new OrchestraValidator().ValidateRepository(repository);

        AssertCodes(result,
            "missing-rule-expression",
            "invalid-assignment-target",
            "unresolved-rule-type",
            "unresolved-rule-field",
            "unregistered-rule",
            "unresolved-flow-source",
            "unresolved-flow-destination",
            "unresolved-flow-message",
            "unresolved-state-machine-actor",
            "unresolved-initial-state",
            "unresolved-transition-target",
            "unresolved-response-message",
            "unresolved-action-field",
            "unresolved-action-component",
            "unresolved-action-group",
            "unresolved-action-message",
            "unresolved-trigger-state-machine",
            "unresolved-trigger-transition",
            "unresolved-timer-reference",
            "invalid-timer-operation");
    }

    [Test]
    public void ReportsEveryProtocolStackOverrideAndLinkRule()
    {
        OrchestraRepository repository = OrchestraSemanticValidatorTests.CreateValidRepository();
        OrchestraInterface orchestraInterface = repository.Interfaces[0];
        orchestraInterface.Services.Add(new OrchestraService { Name = "BrokenService", Layer = "transport", OrchestrationUri = "bad link" });
        orchestraInterface.UserInterfaces.Add(new OrchestraProtocol { Name = "BrokenUi", Kind = "userInterface", Layer = "session" });
        orchestraInterface.Encodings.Add(new OrchestraProtocol { Name = "BrokenEncoding", Kind = "encoding", Layer = "transport" });
        orchestraInterface.SessionProtocols.Add(new OrchestraProtocol { Name = "BrokenSession", Kind = "sessionProtocol", Layer = "application" });
        orchestraInterface.Transports.Add(new OrchestraProtocol { Name = "BrokenTransport", Kind = "transport", Layer = "presentation" });
        OrchestraSession session = orchestraInterface.Sessions[0];
        session.Role = "observer";
        session.ServiceOverrides.Add(new OrchestraService { Name = "MissingService" });
        session.UserInterfaceOverrides.Add(new OrchestraProtocol { Name = "MissingUi" });
        session.EncodingOverrides.Add(new OrchestraProtocol { Name = "MissingEncoding" });
        session.SessionProtocolOverrides.Add(new OrchestraProtocol { Name = "MissingSession" });
        session.ProtocolOverrides.Add(new OrchestraProtocol { Name = "MissingProtocol" });
        session.TransportOverrides.Add(new OrchestraProtocol { Name = "MissingTransport", Address = "" });
        session.IdentifierEntries.Add(new OrchestraIdentifier { Name = "SenderCompID", Value = "SECOND" });

        OrchestraReadResult result = new OrchestraValidator().ValidateRepository(repository);

        string[] codes = result.Diagnostics.Select(item => item.Code).ToArray();
        AssertCodes(result,
            "invalid-service-layer",
            "invalid-protocol-layer",
            "missing-transport-address",
            "invalid-session-role",
            "unresolved-service-override",
            "unresolved-protocol-override",
            "duplicate-session-identifier",
            "invalid-repository-link",
            "unregistered-repository-link");
        Assert.That(codes.Count(item => item == "invalid-protocol-layer"), Is.EqualTo(4));
        Assert.That(codes.Count(item => item == "unresolved-protocol-override"), Is.EqualTo(5));
    }

    private static OrchestraGroupDefinition CreateGroup(string id, string name, string countId, string delimiterId)
    {
        return new OrchestraGroupDefinition
        {
            Id = id,
            Name = name,
            Scenario = "base",
            Category = "Orders",
            CountMember = new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = countId, Scenario = "base" },
            Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = delimiterId, Scenario = "base", Position = 0 }]
        };
    }

    private static void AssertCodes(OrchestraReadResult result, params string[] expected)
    {
        string[] actual = result.Diagnostics.Select(item => item.Code).ToArray();
        Assert.That(result.Success, Is.False);
        for (int index = 0; index < expected.Length; index++) Assert.That(actual, Does.Contain(expected[index]), "Missing diagnostic " + expected[index]);
    }
}
