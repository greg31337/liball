using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraDataTests
{
    [Test]
    public void RepositoryRetainsBehaviorAndInterfaceGraphs()
    {
        OrchestraAnnotation annotation = new OrchestraAnnotation { Purpose = "SYNOPSIS", Language = "en", Content = "Order workflow" };
        OrchestraVariable variable = new OrchestraVariable { Name = "marketOpen", Datatype = "Boolean", IsExternal = true };
        OrchestraRule assignment = new OrchestraRule
        {
            Id = 1,
            Name = "assignClOrdID",
            Kind = OrchestraRuleKind.Assignment,
            Expression = "in.ClOrdID",
            AssignmentTarget = "out.ClOrdID",
            Annotations = [annotation]
        };
        OrchestraResponse response = new OrchestraResponse
        {
            Name = "acknowledge",
            MessageName = "ExecutionReport",
            Scenario = "ack",
            Assignments = [assignment]
        };
        OrchestraTransition transition = new OrchestraTransition
        {
            Name = "acceptOrder",
            Trigger = "NewOrderSingle",
            Condition = "marketOpen == true",
            TargetState = "Working",
            Responses = [response]
        };
        OrchestraState pending = new OrchestraState { Name = "Pending", Transitions = [transition], Annotations = [annotation] };
        OrchestraState working = new OrchestraState { Name = "Working" };
        OrchestraStateMachine machine = new OrchestraStateMachine
        {
            Name = "OrderState",
            InitialState = "Pending",
            States = [pending, working],
            Variables = [variable]
        };
        OrchestraActor buyer = new OrchestraActor { Name = "Buyer", StateMachines = ["OrderState"] };
        OrchestraActor seller = new OrchestraActor { Name = "Seller" };
        OrchestraFlow flow = new OrchestraFlow
        {
            Name = "Orders",
            SourceActor = "Buyer",
            DestinationActor = "Seller",
            Messages = ["NewOrderSingle", "ExecutionReport"]
        };
        OrchestraProtocol protocol = new OrchestraProtocol
        {
            Name = "fixs",
            Layer = "transport",
            Version = "1.0",
            Encoding = "TagValue",
            Transport = "TLS",
            Settings = { ["alpn"] = "fix" }
        };
        OrchestraService service = new OrchestraService { Name = "OrderEntry", Address = "fix.example:9876", Protocols = ["fixs"] };
        OrchestraSession session = new OrchestraSession
        {
            Name = "BUY-SELL",
            Role = "initiator",
            Service = "OrderEntry",
            Protocols = ["fixs"],
            Identifiers = { ["SenderCompID"] = "BUY", ["TargetCompID"] = "SELL" }
        };
        OrchestraInterface orchestraInterface = new OrchestraInterface
        {
            Name = "Trading",
            Services = [service],
            Protocols = [protocol],
            Sessions = [session]
        };
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata
            {
                Name = "Venue",
                FixVersion = "FIX.Latest",
                HasExtensionPack = true,
                ExtensionPack = 309
            },
            Dictionary = new FixDictionary { Identity = new FixDictionaryIdentity { Name = "Venue" } },
            Annotations = [annotation],
            Variables = [variable],
            Rules = [assignment],
            Actors = [buyer, seller],
            Flows = [flow],
            StateMachines = [machine],
            Interfaces = [orchestraInterface]
        };

        Assert.Multiple(() =>
        {
            Assert.That(repository.Metadata.ExtensionPack, Is.EqualTo(309));
            Assert.That(repository.Dictionary?.Identity.Name, Is.EqualTo("Venue"));
            Assert.That(repository.StateMachines[0].States[0].Transitions[0], Is.SameAs(transition));
            Assert.That(transition.Responses[0], Is.SameAs(response));
            Assert.That(response.Assignments[0], Is.SameAs(assignment));
            Assert.That(repository.Actors[0].StateMachines[0], Is.EqualTo("OrderState"));
            Assert.That(repository.Flows[0].Messages[1], Is.EqualTo("ExecutionReport"));
            Assert.That(repository.Interfaces[0].Protocols[0].Settings["alpn"], Is.EqualTo("fix"));
            Assert.That(repository.Interfaces[0].Sessions[0].Identifiers["SenderCompID"], Is.EqualTo("BUY"));
        });
    }

    [Test]
    public void OrchestraOptionsAreSecureByDefaultAndResultsRetainLocations()
    {
        OrchestraReadOptions read = new OrchestraReadOptions();
        OrchestraWriteOptions write = new OrchestraWriteOptions();
        OrchestraRepository partial = new OrchestraRepository { Version = OrchestraVersion.Version11 };
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic
        {
            Severity = ValidationSeverity.Error,
            Code = "unresolved-message",
            Message = "Message reference is missing.",
            Source = "venue.xml",
            Line = 81,
            Column = 12,
            ElementPath = "/repository/actors/actor[1]"
        };
        OrchestraReadResult result = new OrchestraReadResult
        {
            Success = false,
            Repository = partial,
            Diagnostics = [diagnostic]
        };

        Assert.Multiple(() =>
        {
            Assert.That(read.ValidateSchema, Is.True);
            Assert.That(read.IncludeBehavior, Is.True);
            Assert.That(read.IncludeInterfaces, Is.True);
            Assert.That(read.ResolveXInclude, Is.False);
            Assert.That(read.AllowExternalEntities, Is.False);
            Assert.That(read.MaxXmlDepth, Is.EqualTo(FixDefaults.MaxXmlDepth));
            Assert.That(read.MaxSourceSize, Is.EqualTo(FixDefaults.MaxMessageSize));
            Assert.That(write.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(write.CanonicalOrder, Is.True);
            Assert.That(write.EncodingName, Is.EqualTo("utf-8"));
            Assert.That(result.Repository, Is.SameAs(partial));
            Assert.That(result.Diagnostics[0], Is.SameAs(diagnostic));
            Assert.That(diagnostic.Line, Is.EqualTo(81));
            Assert.That(diagnostic.ElementPath, Is.EqualTo("/repository/actors/actor[1]"));
        });
    }

    [Test]
    public void OrchestraDiffRetainsOrderedBreakingChanges()
    {
        OrchestraChange add = new OrchestraChange { Kind = "add", Path = "/messages/NewOrderSingle", After = "D" };
        OrchestraChange replace = new OrchestraChange
        {
            Kind = "replace",
            Path = "/states/Pending/transitions/acceptOrder/condition",
            Before = "true",
            After = "marketOpen == true",
            IsBreaking = true
        };
        OrchestraDiff diff = new OrchestraDiff
        {
            BeforeMetadata = new OrchestraMetadata { Name = "Venue", Release = "1" },
            AfterMetadata = new OrchestraMetadata { Name = "Venue", Release = "2" },
            Changes = [add, replace]
        };

        Assert.Multiple(() =>
        {
            Assert.That(diff.BeforeMetadata.Release, Is.EqualTo("1"));
            Assert.That(diff.AfterMetadata.Release, Is.EqualTo("2"));
            Assert.That(diff.Changes[0], Is.SameAs(add));
            Assert.That(diff.Changes[1], Is.SameAs(replace));
            Assert.That(diff.Changes[1].IsBreaking, Is.True);
        });
    }
}
