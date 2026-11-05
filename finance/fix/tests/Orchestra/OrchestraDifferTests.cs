using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraDifferTests
{
    [Test]
    public void DiffReportsAllMajorGraphAreasInStablePathOrder()
    {
        OrchestraRepository before = CreateRepository(false);
        OrchestraRepository after = CreateRepository(true);
        OrchestraDiffer differ = new OrchestraDiffer();

        OrchestraDiff first = differ.DiffRepositories(before, after);
        OrchestraDiff second = differ.DiffRepositories(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(first.Changes.Select(item => item.Path), Is.EqualTo(new[]
            {
                "/code-sets/54/scenarios/base",
                "/components/100/scenarios/base",
                "/datatypes/Boolean",
                "/encodings/messages/tagvalue",
                "/fields/54/scenarios/base",
                "/flows/Orders",
                "/interfaces/Trading",
                "/messages/Order/scenarios/base",
                "/metadata",
                "/rules/1",
                "/scenarios/auction",
                "/state-machines/Trader/Lifecycle"
            }));
            Assert.That(second.Changes.Select(ChangeText), Is.EqualTo(first.Changes.Select(ChangeText)));
            Assert.That(FindChange(first, "/components/100/scenarios/base").Kind, Is.EqualTo("remove"));
            Assert.That(FindChange(first, "/components/100/scenarios/base").IsBreaking, Is.True);
            Assert.That(FindChange(first, "/datatypes/Boolean").Kind, Is.EqualTo("add"));
            Assert.That(FindChange(first, "/datatypes/Boolean").IsBreaking, Is.False);
            Assert.That(FindChange(first, "/metadata").IsBreaking, Is.False);
            Assert.That(FindChange(first, "/rules/1").Before, Does.Contain("exists(54)"));
            Assert.That(FindChange(first, "/state-machines/Trader/Lifecycle").After, Does.Contain("sessionOpen"));
            Assert.That(first.BeforeMetadata, Is.Not.SameAs(before.Metadata));
            Assert.That(first.AfterMetadata, Is.Not.SameAs(after.Metadata));
            Assert.That(first.BeforeMetadata.Terms[0], Is.Not.SameAs(before.Metadata.Terms[0]));
        });
    }

    [Test]
    public void DiffIsIndependentOfTopLevelOrderAndDictionaryInsertionOrder()
    {
        OrchestraRepository before = CreateRepository(false);
        OrchestraRepository after = CreateRepository(false);
        after.Fields.Reverse();
        after.Datatypes.Reverse();
        after.Interfaces[0].Protocols[0].Settings.Clear();
        after.Interfaces[0].Protocols[0].Settings["heartbeat"] = "30";
        after.Interfaces[0].Protocols[0].Settings["framing"] = "SOFH";

        OrchestraDiff diff = new OrchestraDiffer().DiffRepositories(before, after);

        Assert.That(diff.Changes, Is.Empty);
        Assert.That(diff.Diagnostics, Is.Empty);
    }

    [Test]
    public void DiffOrderingIsStableAcrossReorderedInputsAndLeavesTheirOrderUntouched()
    {
        OrchestraRepository before = CreateRepository(false);
        OrchestraRepository after = CreateRepository(true);
        OrchestraDiffer differ = new OrchestraDiffer();
        OrchestraDiff baseline = differ.DiffRepositories(before, after);

        before.Fields.Reverse();
        after.Fields.Reverse();
        after.Datatypes.Reverse();
        before.Interfaces[0].Protocols[0].Settings.Clear();
        before.Interfaces[0].Protocols[0].Settings["heartbeat"] = "30";
        before.Interfaces[0].Protocols[0].Settings["framing"] = "SOFH";
        after.Interfaces[0].Protocols[0].Settings.Clear();
        after.Interfaces[0].Protocols[0].Settings["heartbeat"] = "30";
        after.Interfaces[0].Protocols[0].Settings["framing"] = "SOFH";
        string[] beforeFieldOrder = before.Fields.Select(item => item.Id).ToArray();
        string[] afterFieldOrder = after.Fields.Select(item => item.Id).ToArray();
        string[] afterDatatypeOrder = after.Datatypes.Select(item => item.Name).ToArray();
        string[] beforeSettingOrder = before.Interfaces[0].Protocols[0].Settings.Keys.ToArray();
        string[] afterSettingOrder = after.Interfaces[0].Protocols[0].Settings.Keys.ToArray();

        OrchestraDiff reordered = differ.DiffRepositories(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Changes.Select(ChangeText), Is.EqualTo(baseline.Changes.Select(ChangeText)));
            Assert.That(reordered.Changes.Select(item => item.Path), Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(before.Fields.Select(item => item.Id), Is.EqualTo(beforeFieldOrder));
            Assert.That(after.Fields.Select(item => item.Id), Is.EqualTo(afterFieldOrder));
            Assert.That(after.Datatypes.Select(item => item.Name), Is.EqualTo(afterDatatypeOrder));
            Assert.That(before.Interfaces[0].Protocols[0].Settings.Keys, Is.EqualTo(beforeSettingOrder));
            Assert.That(after.Interfaces[0].Protocols[0].Settings.Keys, Is.EqualTo(afterSettingOrder));
            Assert.That(before.Metadata.Release, Is.EqualTo("FIX.4.4_EP1"));
            Assert.That(after.Metadata.Release, Is.EqualTo("FIX.4.4_EP2"));
        });
    }

    [Test]
    public void DiffEscapesStableKeysAndDoesNotMutateInputs()
    {
        OrchestraRepository before = new OrchestraRepository
        {
            RepositoryLinks = ["old/repository~1.xml"],
            Interfaces = [new OrchestraInterface { Name = "Venue/One~Primary" }]
        };
        OrchestraRepository after = new OrchestraRepository
        {
            RepositoryLinks = ["new/repository.xml"],
            Interfaces = [new OrchestraInterface { Name = "Venue/One~Primary", Sessions = [new OrchestraSession { Name = "Session" }] }]
        };

        OrchestraDiff diff = new OrchestraDiffer().DiffRepositories(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(diff.Changes.Select(item => item.Path), Is.EqualTo(new[]
            {
                "/interfaces/Venue~1One~0Primary",
                "/repository-links/new~1repository.xml",
                "/repository-links/old~1repository~01.xml"
            }));
            Assert.That(diff.Changes.Select(item => item.Kind), Is.EqualTo(new[] { "replace", "add", "remove" }));
            Assert.That(before.Interfaces[0].Sessions, Is.Empty);
            Assert.That(after.Interfaces[0].Sessions.Single().Name, Is.EqualTo("Session"));
        });
    }

    [Test]
    public void MissingInputsProduceOrderedDiagnostics()
    {
        OrchestraDiffer differ = new OrchestraDiffer();

        OrchestraDiff missingBefore = differ.DiffRepositories(null!, new OrchestraRepository());
        OrchestraDiff missingAfter = differ.DiffRepositories(new OrchestraRepository(), null!);
        OrchestraDiff missingBoth = differ.DiffRepositories(null!, null!);

        Assert.Multiple(() =>
        {
            Assert.That(missingBefore.Changes, Is.Empty);
            Assert.That(missingBefore.Diagnostics.Single().Code, Is.EqualTo("missing-before-orchestra-repository"));
            Assert.That(missingAfter.Diagnostics.Single().Code, Is.EqualTo("missing-after-orchestra-repository"));
            Assert.That(missingBoth.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "missing-before-orchestra-repository",
                "missing-after-orchestra-repository"
            }));
        });
    }

    private static OrchestraRepository CreateRepository(bool changed)
    {
        OrchestraRule rule = new OrchestraRule
        {
            Id = 1,
            Name = "sideRequired",
            Kind = OrchestraRuleKind.Presence,
            Expression = changed ? "exists(54) and sessionOpen" : "exists(54)",
            Presence = FixPresence.Required
        };
        OrchestraProtocol protocol = new OrchestraProtocol
        {
            Name = "FIXP",
            Layer = "session",
            Address = changed ? "fix.example:9000" : "fix.example:8000",
            Settings =
            {
                ["framing"] = "SOFH",
                ["heartbeat"] = "30"
            }
        };
        OrchestraTransition transition = new OrchestraTransition
        {
            Name = "send",
            Trigger = "Order",
            Condition = changed ? "sessionOpen" : "true",
            TargetState = "Ready"
        };
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata
            {
                Name = "Venue",
                FixVersion = "FIX.4.4",
                Release = changed ? "FIX.4.4_EP2" : "FIX.4.4_EP1",
                Terms = [new OrchestraMetadataTerm { Name = "title", Value = "Venue dictionary" }]
            },
            Datatypes = [new OrchestraDatatypeDefinition { Name = "String" }],
            CodeSets =
            [
                new OrchestraCodeSetDefinition
                {
                    Id = "54",
                    Name = "SideCodeSet",
                    Scenario = "base",
                    Type = "String",
                    Codes = [new OrchestraCodeDefinition { Id = "1", Name = changed ? "Bid" : "Buy", Value = "1" }]
                }
            ],
            Fields =
            [
                new OrchestraFieldDefinition
                {
                    Id = "54",
                    Name = changed ? "OrderSide" : "Side",
                    Scenario = "base",
                    CodeSet = "SideCodeSet",
                    Encodings = [new OrchestraEncoding { Standard = "tagvalue", Attributes = { ["width"] = changed ? "2" : "1" } }]
                },
                new OrchestraFieldDefinition { Id = "55", Name = "Symbol", Scenario = "base", Type = "String" }
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "1",
                    Name = "Order",
                    MessageType = "D",
                    Scenario = "base",
                    Selection = changed ? "all" : "any",
                    Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "54", Scenario = "base" }]
                }
            ],
            MessageEncodings = [new OrchestraEncoding { Standard = "tagvalue", Attributes = { ["delimiter"] = changed ? "pipe" : "soh" } }],
            Scenarios =
            [
                new OrchestraScenarioDefinition { Name = "base" },
                new OrchestraScenarioDefinition { Name = "auction", ParentName = changed ? "venue" : "base" }
            ],
            Rules = [rule],
            Actors = [new OrchestraActor { Name = "Trader", StateMachines = ["Lifecycle"] }],
            Flows =
            [
                new OrchestraFlow
                {
                    Name = "Orders",
                    SourceActor = "Trader",
                    DestinationActor = changed ? "VenueTwo" : "VenueOne",
                    Messages = ["Order"]
                }
            ],
            StateMachines =
            [
                new OrchestraStateMachine
                {
                    Name = "Lifecycle",
                    Actor = "Trader",
                    InitialState = "Ready",
                    States = [new OrchestraState { Name = "Ready", Transitions = [transition] }]
                }
            ],
            Interfaces = [new OrchestraInterface { Name = "Trading", Protocols = [protocol] }],
            RepositoryLinks = ["venue.xml"]
        };
        if (changed) repository.Datatypes.Add(new OrchestraDatatypeDefinition { Name = "Boolean" });
        if (!changed) repository.Components.Add(new OrchestraComponentDefinition { Id = "100", Name = "Legacy", Scenario = "base" });
        return repository;
    }

    private static OrchestraChange FindChange(OrchestraDiff diff, string path)
    {
        return diff.Changes.Single(item => item.Path == path);
    }

    private static string ChangeText(OrchestraChange change)
    {
        return change.Kind + ":" + change.Path + ":" + change.Before + ":" + change.After + ":" + change.IsBreaking;
    }
}
