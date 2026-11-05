using System.IO;
using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraQuickFixMappingTests
{
    [Test]
    public void OrchestraThroughQuickFixAndBackPreservesNestedGroupsAndReportsLosses()
    {
        OrchestraRepository source = CreateRepository();
        OrchestraMapper mapper = new OrchestraMapper();

        DictionaryLoadResult mapped = mapper.MapRepositoryToDictionary(source, new FixDictionaryOptions());
        using MemoryStream quickFixXml = new MemoryStream();
        DictionaryLoadResult written = new QuickFixDictionaryWriter().WriteDictionary(quickFixXml, mapped.Dictionary!, new FixDictionaryOptions());
        quickFixXml.Position = 0;
        DictionaryLoadResult loaded = new QuickFixDictionaryLoader().LoadDictionary(quickFixXml, new FixDictionaryOptions());
        OrchestraReadResult reversed = mapper.MapDictionaryToRepository(loaded.Dictionary!, OrchestraVersion.Version10);

        FixDictionary quickFixDictionary = loaded.Dictionary!;
        FixGroupDefinition outer = quickFixDictionary.Groups.Single(item => item.CountTag == 453);
        FixGroupDefinition inner = quickFixDictionary.Groups.Single(item => item.CountTag == 802);
        OrchestraGroupDefinition reversedOuter = reversed.Repository!.Groups.Single(item => item.CountMember!.ReferenceId == "453");
        OrchestraGroupDefinition reversedInner = reversed.Repository.Groups.Single(item => item.CountMember!.ReferenceId == "802");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, DictionaryCodes(mapped));
            Assert.That(mapped.Diagnostics.Select(item => item.Code), Is.EquivalentTo(new[]
            {
                "orchestra-behavior-loss",
                "orchestra-encoding-metadata-loss",
                "orchestra-interface-loss",
                "orchestra-scenario-definition-loss"
            }));
            Assert.That(mapped.Dictionary!.Fields.Any(item => item.Name == "AuctionOnly"), Is.False);
            Assert.That(mapped.Dictionary.Identity.Scenario, Is.EqualTo("auction"));

            Assert.That(written.Success, Is.True, DictionaryCodes(written));
            Assert.That(written.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(item => item.Severity == ValidationSeverity.Warning));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-dictionary-name"));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-dictionary-scenario"));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-scenario"));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-message-scenario"));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-member-scenario"));
            Assert.That(written.Diagnostics.Select(item => item.Code), Does.Contain("quickfix-loss-group-name"));

            Assert.That(loaded.Success, Is.True, DictionaryCodes(loaded));
            Assert.That(loaded.Diagnostics, Is.Empty);
            Assert.That(quickFixDictionary.Identity.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(quickFixDictionary.Identity.Scenario, Is.Empty);
            Assert.That(quickFixDictionary.Messages.Single().Scenario, Is.Empty);
            Assert.That(outer.DelimiterTag, Is.EqualTo(448));
            Assert.That(inner.DelimiterTag, Is.EqualTo(523));
            Assert.That(outer.Members.Select(item => item.Kind), Is.EqualTo(new[] { FixMemberKind.Field, FixMemberKind.Group }));
            Assert.That(outer.Members[1].ReferenceId, Is.EqualTo(inner.Id));
            Assert.That(quickFixDictionary.Messages.Single().Members.Single().ReferenceId, Is.EqualTo(outer.Id));

            Assert.That(reversed.Success, Is.True, OrchestraCodes(reversed));
            Assert.That(reversed.Diagnostics, Is.Empty);
            Assert.That(reversed.Repository.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(reversed.Repository.Messages.Single().Scenario, Is.EqualTo("base"));
            Assert.That(reversedOuter.Members.Select(item => item.Kind), Is.EqualTo(new[] { "fieldRef", "groupRef" }));
            Assert.That(reversedOuter.Members[1].ReferenceId, Is.EqualTo(reversedInner.Id));
            Assert.That(reversed.Repository.Messages.Single().Members.Any(item => item.Kind == "groupRef" && item.ReferenceId == reversedOuter.Id), Is.True);
        });
    }

    [Test]
    public void CanonicalMappingPreservesSelectedScenarioAndHonorsTargetVersion()
    {
        OrchestraMapper mapper = new OrchestraMapper();
        DictionaryLoadResult mapped = mapper.MapRepositoryToDictionary(CreateRepository(), new FixDictionaryOptions());
        FixDictionary dictionary = mapped.Dictionary!;
        FixMessageDefinition? selected = new FixDictionaryQuery().ResolveMessageDefinition(dictionary, "D", "auction");

        OrchestraReadResult version10 = mapper.MapDictionaryToRepository(dictionary, OrchestraVersion.Version10);
        OrchestraReadResult version11 = mapper.MapDictionaryToRepository(dictionary, OrchestraVersion.Version11);
        DictionaryLoadResult mappedAgain = mapper.MapRepositoryToDictionary(version11.Repository!, new FixDictionaryOptions());
        FixMessageDefinition? selectedAgain = new FixDictionaryQuery().ResolveMessageDefinition(mappedAgain.Dictionary!, "D", "auction");

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True, DictionaryCodes(mapped));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo("FIX.4.4"));
            Assert.That(dictionary.Identity.Scenario, Is.EqualTo("auction"));
            Assert.That(selected, Is.Not.Null);
            Assert.That(selected!.Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(selected.Members.Single().Kind, Is.EqualTo(FixMemberKind.Group));

            Assert.That(version10.Success, Is.True, OrchestraCodes(version10));
            Assert.That(version11.Success, Is.True, OrchestraCodes(version11));
            Assert.That(version10.Repository!.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(version11.Repository!.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(version10.Repository.Messages.Single().Scenario, Is.EqualTo("auction"));
            Assert.That(version11.Repository.Messages.Single().Scenario, Is.EqualTo("auction"));
            Assert.That(version11.Repository.Scenarios.Single(item => item.Name == "auction").ParentName, Is.EqualTo("base"));
            Assert.That(version10.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { "dictionary-diagnostics-loss" }));
            Assert.That(version11.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { "dictionary-diagnostics-loss" }));

            Assert.That(mappedAgain.Success, Is.True, DictionaryCodes(mappedAgain));
            Assert.That(mappedAgain.Dictionary!.Identity.Scenario, Is.EqualTo("auction"));
            Assert.That(selectedAgain, Is.Not.Null);
            Assert.That(selectedAgain!.Scenario, Is.EqualTo("auction"));
            Assert.That(mappedAgain.Dictionary.Groups.Single(item => item.CountTag == 453).Members[1].Kind, Is.EqualTo(FixMemberKind.Group));
        });
    }

    private static OrchestraRepository CreateRepository()
    {
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata { Name = "Venue", FixVersion = "FIX.4.4", Release = "FIX.4.4" },
            Dictionary = new FixDictionary { Identity = new FixDictionaryIdentity { Scenario = "auction" } },
            Datatypes =
            [
                new OrchestraDatatypeDefinition { Name = "String" },
                new OrchestraDatatypeDefinition { Name = "NumInGroup" }
            ],
            Fields =
            [
                Field("8", "BeginString", "String", "base"),
                Field("9", "BodyLength", "String", "base"),
                Field("10", "CheckSum", "String", "base"),
                Field("35", "MsgType", "String", "base"),
                Field("448", "PartyID", "String", "base"),
                Field("453", "NoPartyIDs", "NumInGroup", "base"),
                Field("523", "PartySubID", "String", "base"),
                Field("802", "NoPartySubIDs", "NumInGroup", "base"),
                Field("9001", "AuctionOnly", "String", "auction")
            ],
            Components =
            [
                Component("1024", "StandardHeader", [Member("fieldRef", "8", 0), Member("fieldRef", "9", 1), Member("fieldRef", "35", 2)]),
                Component("1025", "StandardTrailer", [Member("fieldRef", "10", 0)])
            ],
            Groups =
            [
                Group("201", "PartySubIDs", "802", [Member("fieldRef", "523", 0)]),
                Group("200", "Parties", "453", [Member("fieldRef", "448", 0), Member("groupRef", "201", 1)])
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "14",
                    Name = "NewOrderSingle",
                    MessageType = "D",
                    Scenario = "auction",
                    Members =
                    [
                        Member("componentRef", "1024", 0),
                        Member("groupRef", "200", 1),
                        Member("componentRef", "1025", 2)
                    ]
                }
            ],
            Scenarios =
            [
                new OrchestraScenarioDefinition { Name = "base" },
                new OrchestraScenarioDefinition { Name = "auction", ParentName = "base" }
            ],
            Actors = [new OrchestraActor { Name = "Trader" }],
            Interfaces = [new OrchestraInterface { Name = "Trading" }],
            EncodingStandards = [new OrchestraEncodingStandard { Name = "tagvalue" }]
        };
        return repository;
    }

    private static OrchestraFieldDefinition Field(string id, string name, string type, string scenario)
    {
        return new OrchestraFieldDefinition { Id = id, Name = name, Type = type, Scenario = scenario };
    }

    private static OrchestraComponentDefinition Component(string id, string name, OrchestraMemberDefinition[] members)
    {
        return new OrchestraComponentDefinition { Id = id, Name = name, Scenario = "base", Members = [.. members] };
    }

    private static OrchestraGroupDefinition Group(string id, string name, string countId, OrchestraMemberDefinition[] members)
    {
        return new OrchestraGroupDefinition
        {
            Id = id,
            Name = name,
            Scenario = "base",
            CountMember = Member("fieldRef", countId, 0),
            Members = [.. members]
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
            Presence = FixPresence.Required,
            PresenceText = "required"
        };
    }

    private static string DictionaryCodes(DictionaryLoadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }

    private static string OrchestraCodes(OrchestraReadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
