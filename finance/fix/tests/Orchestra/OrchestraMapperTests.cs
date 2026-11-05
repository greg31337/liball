using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraMapperTests
{
    [Test]
    public void RepositoryToDictionaryMapsStructureAndReportsAggregateLosses()
    {
        OrchestraRule presenceRule = new OrchestraRule
        {
            Id = 7,
            Kind = OrchestraRuleKind.Presence,
            Expression = "exists(54)",
            Presence = FixPresence.Required,
            PresenceText = "required"
        };
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata
            {
                Name = "Venue",
                FixVersion = "FIX.4.4_EP2",
                HasExtensionPack = true,
                ExtensionPack = 2
            },
            Datatypes =
            [
                new OrchestraDatatypeDefinition { Name = "String" },
                new OrchestraDatatypeDefinition { Name = "NumInGroup", BaseType = "int" }
            ],
            CodeSets =
            [
                new OrchestraCodeSetDefinition
                {
                    Id = "54",
                    Name = "SideCodeSet",
                    Scenario = "base",
                    Type = "String",
                    Codes =
                    [
                        new OrchestraCodeDefinition { Id = "1", Name = "Buy", Scenario = "base", Value = "1", Sort = "1" },
                        new OrchestraCodeDefinition { Id = "2", Name = "Sell", Scenario = "base", Value = "2", Sort = "2" }
                    ]
                }
            ],
            Fields =
            [
                new OrchestraFieldDefinition { Id = "8", Name = "BeginString", Scenario = "base", Type = "String" },
                new OrchestraFieldDefinition { Id = "10", Name = "CheckSum", Scenario = "base", Type = "String" },
                new OrchestraFieldDefinition { Id = "54", Name = "Side", Scenario = "base", CodeSet = "SideCodeSet" },
                new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Scenario = "base", Type = "NumInGroup" },
                new OrchestraFieldDefinition { Id = "54", Name = "AuctionSide", Scenario = "auction", CodeSet = "SideCodeSet" }
            ],
            Components =
            [
                new OrchestraComponentDefinition
                {
                    Id = "1024",
                    Name = "StandardHeader",
                    Scenario = "base",
                    Members = [Member("fieldRef", "8", FixPresence.Required)]
                },
                new OrchestraComponentDefinition
                {
                    Id = "1025",
                    Name = "StandardTrailer",
                    Scenario = "base",
                    Members = [Member("fieldRef", "10", FixPresence.Required)]
                },
                new OrchestraComponentDefinition
                {
                    Id = "1100",
                    Name = "Parties",
                    Scenario = "base",
                    Annotations = [new OrchestraAnnotation { Kind = "documentation", Content = "Party details." }],
                    Members =
                    [
                        new OrchestraMemberDefinition
                        {
                            Kind = "fieldRef",
                            ReferenceId = "54",
                            Scenario = "base",
                            Presence = FixPresence.Optional,
                            Rules = [presenceRule]
                        }
                    ]
                }
            ],
            Groups =
            [
                new OrchestraGroupDefinition
                {
                    Id = "2000",
                    Name = "PartyGroup",
                    Scenario = "base",
                    CountMember = Member("fieldRef", "453", FixPresence.Required),
                    Members = [Member("fieldRef", "54", FixPresence.Required)]
                }
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "1",
                    Name = "Order",
                    MessageType = "D",
                    Scenario = "base",
                    Members =
                    [
                        Member("componentRef", "1024", FixPresence.Required),
                        Member("componentRef", "1100", FixPresence.Optional),
                        Member("groupRef", "2000", FixPresence.Optional),
                        Member("componentRef", "1025", FixPresence.Required)
                    ]
                }
            ],
            Scenarios =
            [
                new OrchestraScenarioDefinition { Name = "base" },
                new OrchestraScenarioDefinition { Name = "auction", ParentName = "base" }
            ],
            Rules = [presenceRule],
            Actors = [new OrchestraActor { Name = "Trader" }],
            Interfaces = [new OrchestraInterface { Name = "Trading" }],
            EncodingStandards = [new OrchestraEncodingStandard { Name = "tagvalue" }]
        };

        DictionaryLoadResult result = new OrchestraMapper().MapRepositoryToDictionary(repository, new FixDictionaryOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Identity.Name, Is.EqualTo("Venue"));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo("FIX.4.4"));
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(2));
            Assert.That(dictionary.Fields.Select(item => item.Name), Does.Not.Contain("AuctionSide"));
            Assert.That(dictionary.CodeSets.Single().Codes.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(dictionary.Header.Single().ReferenceId, Is.EqualTo(8));
            Assert.That(dictionary.Trailer.Single().ReferenceId, Is.EqualTo(10));
            Assert.That(dictionary.Components.Single().Description, Is.EqualTo("Party details."));
            Assert.That(dictionary.Components.Single().Members.Single().Presence, Is.EqualTo(FixPresence.Conditional));
            Assert.That(dictionary.Components.Single().Members.Single().PresenceRule, Is.EqualTo("exists(54)"));
            Assert.That(dictionary.Groups.Single().CountTag, Is.EqualTo(453));
            Assert.That(dictionary.Groups.Single().DelimiterTag, Is.EqualTo(54));
            Assert.That(dictionary.Messages.Single().Members.Select(item => item.ReferenceId), Is.EqualTo(new[] { 1100, 2000 }));
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EquivalentTo(new[]
            {
                "orchestra-behavior-loss",
                "orchestra-encoding-metadata-loss",
                "orchestra-interface-loss",
                "orchestra-scenario-definition-loss"
            }));
        });
    }

    [Test]
    public void DictionaryToRepositoryMapsEnvelopeNestedMembersAndConditions()
    {
        FixDictionary dictionary = CreateCanonicalDictionary();

        OrchestraReadResult result = new OrchestraMapper().MapDictionaryToRepository(dictionary, OrchestraVersion.Version11);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Repository, Is.Not.Null);
        OrchestraRepository repository = result.Repository!;
        OrchestraFieldDefinition side = repository.Fields.Single(item => item.Id == "54");
        OrchestraMessageDefinition message = repository.Messages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(repository.Dictionary, Is.SameAs(dictionary));
            Assert.That(repository.Metadata.Release, Is.EqualTo("FIX.4.4_EP2"));
            Assert.That(side.CodeSet, Is.EqualTo("SideCodeSet"));
            Assert.That(side.Type, Is.Empty);
            Assert.That(repository.Components.Select(item => item.Name), Does.Contain("StandardHeader"));
            Assert.That(repository.Components.Select(item => item.Name), Does.Contain("StandardTrailer"));
            Assert.That(message.Members.Select(item => item.Kind), Is.EqualTo(new[] { "componentRef", "componentRef", "groupRef", "componentRef" }));
            Assert.That(message.Members.First().Scenario, Is.EqualTo("base"));
            Assert.That(repository.Groups.Single().CountMember?.ReferenceId, Is.EqualTo("453"));
            Assert.That(repository.Components.Single(item => item.Name == "Parties").Members.Single().Rules.Single().Expression, Is.EqualTo("exists(54)"));
            Assert.That(repository.Rules.Single().Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(repository.Scenarios.Select(item => item.Name), Is.EqualTo(new[] { "base", "auction" }));
        });

        OrchestraReadResult validation = new OrchestraValidator().ValidateRepository(repository);
        Assert.That(validation.Success, Is.True, string.Join(Environment.NewLine, validation.Diagnostics.Select(item => item.Code + ": " + item.Message)));
    }

    [Test]
    public void DictionaryToRepositoryReportsCanonicalLossesWithoutFailing()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Overlay", Source = "overlay.xml" },
            Fields = [new FixFieldDefinition { Tag = 5000, Name = "RemovedField", IsRemoved = true }],
            Scenarios =
            [
                new FixScenarioDefinition
                {
                    Name = "auction",
                    Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 54 }],
                    Rules = ["exists(54)"]
                }
            ],
            Diagnostics = [new DictionaryDiagnostic { Severity = ValidationSeverity.Warning, Code = "source-warning" }]
        };

        OrchestraReadResult result = new OrchestraMapper().MapDictionaryToRepository(dictionary, OrchestraVersion.Version10);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Repository, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Repository!.Fields, Is.Empty);
            Assert.That(result.Repository.Scenarios.Select(item => item.Name), Is.EqualTo(new[] { "base", "auction" }));
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EquivalentTo(new[]
            {
                "dictionary-scenario-member-loss",
                "dictionary-scenario-rule-loss",
                "dictionary-diagnostics-loss",
                "dictionary-overlay-removal-loss",
                "dictionary-source-provenance-loss"
            }));
        });
    }

    [Test]
    public void InvalidInputsProduceExplicitFailures()
    {
        OrchestraMapper mapper = new OrchestraMapper();

        DictionaryLoadResult missingRepository = mapper.MapRepositoryToDictionary(null!, new FixDictionaryOptions());
        DictionaryLoadResult missingOptions = mapper.MapRepositoryToDictionary(new OrchestraRepository(), null!);
        OrchestraReadResult missingDictionary = mapper.MapDictionaryToRepository(null!, OrchestraVersion.Version11);
        OrchestraReadResult unsupportedVersion = mapper.MapDictionaryToRepository(new FixDictionary(), OrchestraVersion.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(missingRepository.Success, Is.False);
            Assert.That(missingRepository.Diagnostics.Single().Code, Is.EqualTo("missing-orchestra-repository"));
            Assert.That(missingOptions.Success, Is.False);
            Assert.That(missingOptions.Diagnostics.Single().Code, Is.EqualTo("missing-dictionary-options"));
            Assert.That(missingDictionary.Success, Is.False);
            Assert.That(missingDictionary.Diagnostics.Single().Code, Is.EqualTo("missing-fix-dictionary"));
            Assert.That(unsupportedVersion.Success, Is.False);
            Assert.That(unsupportedVersion.Diagnostics.Single().Code, Is.EqualTo("unsupported-orchestra-mapping-version"));
        });
    }

    private static FixDictionary CreateCanonicalDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "Venue",
                ApplicationVersion = "FIX.4.4",
                HasExtensionPack = true,
                ExtensionPack = 2
            },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String },
                new FixDatatypeDefinition { Name = "NumInGroup", FieldType = FixFieldType.NumInGroup }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.String,
                    Codes =
                    [
                        new FixCodeDefinition { Id = 1, Name = "Buy", Value = "1", SortOrder = 1 },
                        new FixCodeDefinition { Id = 2, Name = "Sell", Value = "2", SortOrder = 2 }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 8, Name = "BeginString", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 10, Name = "CheckSum", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 54, Name = "Side", CodeSetName = "SideCodeSet", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", DatatypeName = "NumInGroup", FieldType = FixFieldType.NumInGroup }
            ],
            Header = [CanonicalMember(FixMemberKind.Field, 8, FixPresence.Required)],
            Trailer = [CanonicalMember(FixMemberKind.Field, 10, FixPresence.Required)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 1100,
                    Name = "Parties",
                    Members =
                    [
                        new FixMemberDefinition
                        {
                            Kind = FixMemberKind.Field,
                            ReferenceId = 54,
                            Scenario = "base",
                            Presence = FixPresence.Conditional,
                            PresenceRule = "exists(54)",
                            RulePresence = FixPresence.Required
                        }
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 2000,
                    Name = "PartyGroup",
                    CountTag = 453,
                    DelimiterTag = 54,
                    Members = [CanonicalMember(FixMemberKind.Field, 54, FixPresence.Required)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "Order",
                    MsgType = "D",
                    Scenario = "auction",
                    Members =
                    [
                        CanonicalMember(FixMemberKind.Component, 1100, FixPresence.Optional),
                        CanonicalMember(FixMemberKind.Group, 2000, FixPresence.Optional)
                    ]
                }
            ],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "auction", ParentName = "base" }
            ]
        };
    }

    private static OrchestraMemberDefinition Member(string kind, string id, FixPresence presence)
    {
        return new OrchestraMemberDefinition
        {
            Kind = kind,
            ReferenceId = id,
            Scenario = "base",
            Presence = presence,
            PresenceText = presence == FixPresence.Required ? "required" : "optional"
        };
    }

    private static FixMemberDefinition CanonicalMember(FixMemberKind kind, int id, FixPresence presence)
    {
        return new FixMemberDefinition
        {
            Kind = kind,
            ReferenceId = id,
            Scenario = "base",
            Presence = presence
        };
    }
}
