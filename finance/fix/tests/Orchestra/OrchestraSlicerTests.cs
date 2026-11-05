using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraSlicerTests
{
    [Test]
    public void CategoryAndScenarioSliceRetainsRecursiveDependencyClosure()
    {
        OrchestraRepository source = CreateRepository();

        OrchestraReadResult result = new OrchestraSlicer().SliceRepository(source, ["Orders"], ["auction"]);

        Assert.That(result.Success, Is.True, Codes(result));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Repository, Is.Not.Null);
        OrchestraRepository sliced = result.Repository!;
        Assert.Multiple(() =>
        {
            Assert.That(sliced.Messages.Select(item => item.Name), Is.EqualTo(new[] { "Order" }));
            Assert.That(sliced.Messages.Single().Scenario, Is.EqualTo("auction"));
            Assert.That(sliced.Components.Select(item => item.Id), Is.EqualTo(new[] { "100" }));
            Assert.That(sliced.Groups.Select(item => item.Id), Is.EqualTo(new[] { "200" }));
            Assert.That(sliced.Fields.Select(item => item.Id), Is.EqualTo(new[] { "11", "54", "453", "448" }));
            Assert.That(sliced.CodeSets.Select(item => item.Name), Is.EqualTo(new[] { "SideCodeSet" }));
            Assert.That(sliced.CodeSets.Single().Codes.Select(item => item.Value), Is.EqualTo(new[] { "1" }));
            Assert.That(sliced.Datatypes.Select(item => item.Name), Is.EqualTo(new[] { "String", "NumInGroup" }));
            Assert.That(sliced.Rules.Select(item => item.Id), Is.EqualTo(new[] { 1 }));
            Assert.That(sliced.Scenarios.Select(item => item.Name), Is.EqualTo(new[] { "base", "auction" }));
            Assert.That(sliced.EncodingStandards.Select(item => item.Name), Is.EqualTo(new[] { "tagvalue" }));
            Assert.That(sliced.MessageEncodings.Single().Standard, Is.EqualTo("tagvalue"));
            Assert.That(sliced.Categories.Select(item => item.Name), Is.EqualTo(new[] { "Orders" }));
            Assert.That(sliced.Sections.Select(item => item.Name), Is.EqualTo(new[] { "Trade" }));
            Assert.That(sliced.Concepts.Single().Members.Select(item => item.Id), Is.EqualTo(new[] { "54" }));
            Assert.That(sliced.Flows.Select(item => item.Name), Is.EqualTo(new[] { "OrderFlow" }));
            Assert.That(sliced.Actors.Select(item => item.Name), Is.EqualTo(new[] { "Trader", "Venue" }));
            Assert.That(sliced.Interfaces.Select(item => item.Name), Is.EqualTo(new[] { "Trading" }));
            Assert.That(sliced.Components.Single().Members.Select(item => item.ReferenceId), Is.EqualTo(new[] { "11", "200" }));
        });

        OrchestraReadResult validation = new OrchestraValidator().ValidateRepository(sliced);
        Assert.That(validation.Success, Is.True, Codes(validation));
    }

    [Test]
    public void SliceIsStableIndependentAndSupportsMessageTypeSelection()
    {
        OrchestraRepository source = CreateRepository();
        OrchestraSlicer slicer = new OrchestraSlicer();

        OrchestraReadResult first = slicer.SliceRepository(source, ["D"], ["auction"]);
        OrchestraReadResult second = slicer.SliceRepository(source, ["D"], ["auction"]);

        Assert.That(first.Success, Is.True, Codes(first));
        Assert.That(second.Success, Is.True, Codes(second));
        OrchestraDiff repeatDiff = new OrchestraDiffer().DiffRepositories(first.Repository!, second.Repository!);
        Assert.Multiple(() =>
        {
            Assert.That(repeatDiff.Changes, Is.Empty);
            Assert.That(first.Repository, Is.Not.SameAs(source));
            Assert.That(first.Repository!.Metadata, Is.Not.SameAs(source.Metadata));
            Assert.That(first.Repository.Fields[0], Is.Not.SameAs(source.Fields[0]));
            Assert.That(source.Messages.Count, Is.EqualTo(2));
            Assert.That(source.Components.Single(item => item.Id == "100").Members.Count, Is.EqualTo(3));
            Assert.That(source.CodeSets[0].Codes.Count, Is.EqualTo(2));
            Assert.That(source.Concepts.Single().Members.Count, Is.EqualTo(2));
        });

        first.Repository!.Fields[0].Name = "ChangedSliceOnly";
        Assert.That(source.Fields[0].Name, Is.EqualTo("ClOrdID"));
    }

    [Test]
    public void SlicePreservesNestedScenarioAndEncodingMetadataWithoutSharingReferences()
    {
        OrchestraRepository source = CreateRepository();
        source.Scenarios.Single(item => item.Name == "auction").Attributes["role"] = "selected";
        source.EncodingStandards.Single(item => item.Name == "tagvalue").Encoding = new OrchestraEncoding
        {
            Standard = "tagvalue",
            Attributes = { ["character-set"] = "US-ASCII" }
        };
        source.Datatypes.Single(item => item.Name == "String").Mappings.Add(new OrchestraDatatypeMapping
        {
            Standard = "tagvalue",
            Base = "String",
            Encoding = new OrchestraEncoding
            {
                Standard = "tagvalue",
                Attributes = { ["variable-length"] = "true" }
            }
        });
        source.Messages.Single(item => item.Name == "Order").Encodings.Add(new OrchestraEncoding
        {
            Standard = "tagvalue",
            Attributes = { ["message-type"] = "D" }
        });
        source.MessageEncodings[0].Attributes["delimiter"] = "SOH";
        source.MessageEncodings[0].SchemaReferences.Add(new OrchestraExternalSchemaReference
        {
            Name = "wire",
            NamespaceUri = "urn:example:wire",
            Location = "wire.xsd"
        });
        source.Fields.Single(item => item.Id == "54").Encodings[0].Attributes["width"] = "1";

        OrchestraReadResult result = new OrchestraSlicer().SliceRepository(source, ["D"], ["auction"]);

        Assert.That(result.Success, Is.True, Codes(result));
        OrchestraRepository sliced = result.Repository!;
        OrchestraScenarioDefinition slicedScenario = sliced.Scenarios.Single(item => item.Name == "auction");
        OrchestraEncodingStandard slicedStandard = sliced.EncodingStandards.Single(item => item.Name == "tagvalue");
        OrchestraDatatypeMapping slicedMapping = sliced.Datatypes.Single(item => item.Name == "String").Mappings.Single();
        OrchestraEncoding slicedMessageEncoding = sliced.Messages.Single().Encodings.Single();
        OrchestraEncoding slicedDefaultEncoding = sliced.MessageEncodings.Single();
        OrchestraEncoding slicedFieldEncoding = sliced.Fields.Single(item => item.Id == "54").Encodings.Single();
        Assert.Multiple(() =>
        {
            Assert.That(sliced.Scenarios.Select(item => item.Name), Is.EqualTo(new[] { "base", "auction" }));
            Assert.That(slicedScenario.ParentName, Is.EqualTo("base"));
            Assert.That(slicedScenario.Attributes["role"], Is.EqualTo("selected"));
            Assert.That(slicedStandard.Encoding!.Attributes["character-set"], Is.EqualTo("US-ASCII"));
            Assert.That(slicedMapping.Standard, Is.EqualTo("tagvalue"));
            Assert.That(slicedMapping.Encoding!.Attributes["variable-length"], Is.EqualTo("true"));
            Assert.That(slicedMessageEncoding.Attributes["message-type"], Is.EqualTo("D"));
            Assert.That(slicedDefaultEncoding.Attributes["delimiter"], Is.EqualTo("SOH"));
            Assert.That(slicedDefaultEncoding.SchemaReferences.Single().Location, Is.EqualTo("wire.xsd"));
            Assert.That(slicedFieldEncoding.Attributes["width"], Is.EqualTo("1"));
            Assert.That(slicedScenario, Is.Not.SameAs(source.Scenarios.Single(item => item.Name == "auction")));
            Assert.That(slicedStandard.Encoding, Is.Not.SameAs(source.EncodingStandards.Single(item => item.Name == "tagvalue").Encoding));
            Assert.That(slicedMapping.Encoding, Is.Not.SameAs(source.Datatypes.Single(item => item.Name == "String").Mappings.Single().Encoding));
            Assert.That(slicedDefaultEncoding.SchemaReferences[0], Is.Not.SameAs(source.MessageEncodings[0].SchemaReferences[0]));
        });

        slicedScenario.Attributes["role"] = "changed";
        slicedStandard.Encoding!.Attributes["character-set"] = "UTF-8";
        slicedMapping.Encoding!.Attributes["variable-length"] = "false";
        slicedMessageEncoding.Attributes["message-type"] = "changed";
        slicedDefaultEncoding.SchemaReferences[0].Location = "changed.xsd";
        slicedFieldEncoding.Attributes["width"] = "2";

        Assert.Multiple(() =>
        {
            Assert.That(source.Scenarios.Single(item => item.Name == "auction").Attributes["role"], Is.EqualTo("selected"));
            Assert.That(source.EncodingStandards.Single(item => item.Name == "tagvalue").Encoding!.Attributes["character-set"], Is.EqualTo("US-ASCII"));
            Assert.That(source.Datatypes.Single(item => item.Name == "String").Mappings.Single().Encoding!.Attributes["variable-length"], Is.EqualTo("true"));
            Assert.That(source.Messages.Single(item => item.Name == "Order").Encodings.Single().Attributes["message-type"], Is.EqualTo("D"));
            Assert.That(source.MessageEncodings[0].SchemaReferences[0].Location, Is.EqualTo("wire.xsd"));
            Assert.That(source.Fields.Single(item => item.Id == "54").Encodings[0].Attributes["width"], Is.EqualTo("1"));
        });
    }

    [Test]
    public void EmptySelectionIncludesAllActiveMessagesAndReportsUnknownSelectors()
    {
        OrchestraRepository source = CreateRepository();
        OrchestraSlicer slicer = new OrchestraSlicer();

        OrchestraReadResult all = slicer.SliceRepository(source, [], []);
        OrchestraReadResult unknown = slicer.SliceRepository(source, ["Missing"], ["missing-scenario"]);

        Assert.Multiple(() =>
        {
            Assert.That(all.Success, Is.True, Codes(all));
            Assert.That(all.Repository!.Messages.Select(item => item.Name), Is.EqualTo(new[] { "Order", "Quote" }));
            Assert.That(all.Repository.Fields.Select(item => item.Id), Does.Not.Contain("9001"));
            Assert.That(all.Repository.CodeSets.Single(item => item.Name == "SideCodeSet").Codes.Select(item => item.Value), Does.Not.Contain("2"));
            Assert.That(unknown.Success, Is.True);
            Assert.That(unknown.Repository!.Messages, Is.Empty);
            Assert.That(unknown.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "unknown-message-or-category-selection",
                "unknown-scenario-selection"
            }));
        });
    }

    [Test]
    public void MissingInputsProduceExplicitFailures()
    {
        OrchestraSlicer slicer = new OrchestraSlicer();

        OrchestraReadResult missingRepository = slicer.SliceRepository(null!, [], []);
        OrchestraReadResult missingMessages = slicer.SliceRepository(new OrchestraRepository(), null!, []);
        OrchestraReadResult missingScenarios = slicer.SliceRepository(new OrchestraRepository(), [], null!);

        Assert.Multiple(() =>
        {
            Assert.That(missingRepository.Success, Is.False);
            Assert.That(missingRepository.Diagnostics.Single().Code, Is.EqualTo("missing-orchestra-repository"));
            Assert.That(missingMessages.Success, Is.False);
            Assert.That(missingMessages.Diagnostics.Single().Code, Is.EqualTo("missing-message-selection"));
            Assert.That(missingScenarios.Success, Is.False);
            Assert.That(missingScenarios.Diagnostics.Single().Code, Is.EqualTo("missing-scenario-selection"));
        });
    }

    private static OrchestraRepository CreateRepository()
    {
        OrchestraRule sideRule = new OrchestraRule
        {
            Id = 1,
            Name = "sideRequired",
            Kind = OrchestraRuleKind.Presence,
            Expression = "exists(54)",
            Presence = FixPresence.Required,
            UniqueFieldIds = ["54"]
        };
        OrchestraMemberDefinition clOrdId = Member("fieldRef", "11", 0);
        clOrdId.Rules.Add(sideRule);
        OrchestraMemberDefinition future = Member("fieldRef", "9001", 2);
        future.Pedigree.AddedVersion = "FIX.5.0";
        OrchestraCodeDefinition futureCode = new OrchestraCodeDefinition
        {
            Id = "2",
            Name = "FutureSide",
            Value = "2",
            Pedigree = new OrchestraPedigree { AddedVersion = "FIX.5.0" }
        };
        return new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata { Name = "Venue", FixVersion = "FIX.4.4", Release = "FIX.4.4" },
            Categories =
            [
                new OrchestraCategory { Name = "Orders", Section = "Trade" },
                new OrchestraCategory { Name = "MarketData", Section = "Prices" }
            ],
            Sections = [new OrchestraSection { Name = "Trade" }, new OrchestraSection { Name = "Prices" }],
            Concepts =
            [
                new OrchestraConcept
                {
                    Name = "Sides",
                    Members =
                    [
                        new OrchestraConceptMember { Kind = "fieldRef", Id = "54", Scenario = "base" },
                        new OrchestraConceptMember { Kind = "fieldRef", Id = "999", Scenario = "base" }
                    ]
                }
            ],
            EncodingStandards =
            [
                new OrchestraEncodingStandard { Name = "tagvalue" },
                new OrchestraEncodingStandard { Name = "json" }
            ],
            Datatypes =
            [
                new OrchestraDatatypeDefinition { Name = "String" },
                new OrchestraDatatypeDefinition { Name = "NumInGroup" },
                new OrchestraDatatypeDefinition { Name = "Price" }
            ],
            CodeSets =
            [
                new OrchestraCodeSetDefinition
                {
                    Id = "54",
                    Name = "SideCodeSet",
                    Scenario = "base",
                    Type = "String",
                    Codes = [new OrchestraCodeDefinition { Id = "1", Name = "Buy", Value = "1" }, futureCode]
                },
                new OrchestraCodeSetDefinition { Id = "999", Name = "UnusedCodeSet", Scenario = "base", Type = "String" }
            ],
            Fields =
            [
                new OrchestraFieldDefinition { Id = "11", Name = "ClOrdID", Scenario = "base", Type = "String" },
                new OrchestraFieldDefinition
                {
                    Id = "54",
                    Name = "Side",
                    Scenario = "base",
                    CodeSet = "SideCodeSet",
                    Encodings = [new OrchestraEncoding { Standard = "tagvalue" }]
                },
                new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Scenario = "base", Type = "NumInGroup" },
                new OrchestraFieldDefinition { Id = "448", Name = "PartyID", Scenario = "base", Type = "String" },
                new OrchestraFieldDefinition { Id = "9001", Name = "FutureField", Scenario = "base", Type = "String", Pedigree = new OrchestraPedigree { AddedVersion = "FIX.5.0" } },
                new OrchestraFieldDefinition { Id = "999", Name = "Unused", Scenario = "base", Type = "Price" }
            ],
            Components =
            [
                new OrchestraComponentDefinition
                {
                    Id = "100",
                    Name = "OrderCore",
                    Scenario = "base",
                    Category = "Orders",
                    Members = [clOrdId, Member("groupRef", "200", 1), future]
                },
                new OrchestraComponentDefinition { Id = "101", Name = "UnusedComponent", Scenario = "base" }
            ],
            Groups =
            [
                new OrchestraGroupDefinition
                {
                    Id = "200",
                    Name = "Parties",
                    Scenario = "base",
                    Category = "Orders",
                    CountMember = Member("fieldRef", "453", 0),
                    Members = [Member("fieldRef", "448", 0), Member("fieldRef", "54", 1)]
                }
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "1",
                    Name = "Order",
                    MessageType = "D",
                    Category = "Orders",
                    Scenario = "auction",
                    Members = [Member("componentRef", "100", 0)]
                },
                new OrchestraMessageDefinition
                {
                    Id = "2",
                    Name = "Quote",
                    MessageType = "S",
                    Category = "MarketData",
                    Scenario = "base",
                    Members = [Member("fieldRef", "999", 0)]
                }
            ],
            MessageEncodings = [new OrchestraEncoding { Standard = "tagvalue" }],
            Scenarios =
            [
                new OrchestraScenarioDefinition { Name = "base" },
                new OrchestraScenarioDefinition { Name = "auction", ParentName = "base" },
                new OrchestraScenarioDefinition { Name = "night", ParentName = "base" }
            ],
            Rules = [sideRule, new OrchestraRule { Id = 2, Name = "unused", Kind = OrchestraRuleKind.Conditional, Expression = "true" }],
            Actors = [new OrchestraActor { Name = "Trader" }, new OrchestraActor { Name = "Venue" }, new OrchestraActor { Name = "UnusedActor" }],
            Flows =
            [
                new OrchestraFlow { Name = "OrderFlow", SourceActor = "Trader", DestinationActor = "Venue", Messages = ["Order"] },
                new OrchestraFlow { Name = "QuoteFlow", SourceActor = "UnusedActor", DestinationActor = "Venue", Messages = ["Quote"] }
            ],
            Interfaces = [new OrchestraInterface { Name = "Trading" }]
        };
    }

    private static OrchestraMemberDefinition Member(string kind, string id, int position)
    {
        return new OrchestraMemberDefinition
        {
            Kind = kind,
            ReferenceId = id,
            Scenario = "base",
            Position = position,
            Presence = FixPresence.Optional
        };
    }

    private static string Codes(OrchestraReadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
