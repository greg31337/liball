using fix;

namespace fix.Tests;

[TestFixture]
public class CSharpEmitterTests
{
    [Test]
    public void EmitterProducesConstantsModelsOperationsAndScenarioCrackers()
    {
        FixDictionary dictionary = CreateDictionary();
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            TypePrefix = "Venue"
        };
        CSharpNamePlan names = CSharpNameResolver.ResolveNames(dictionary, options);

        CSharpEmissionResult result = CSharpEmitter.EmitSources(dictionary, options, names);

        string baseName = names.MessageNames[CSharpNameResolver.CreateMessageKey("D", "base")];
        string auctionName = names.MessageNames[CSharpNameResolver.CreateMessageKey("D", "auction")];
        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False);
            Assert.That(result.Files, Has.Count.EqualTo(16));
            Assert.That(result.Files.Select(item => item.Path), Is.EqualTo(result.Files.Select(item => item.Path).OrderBy(item => item, StringComparer.Ordinal)));
            Assert.That(FileContent(result, "Fields/VenueFixFieldTags.cs"), Does.Contain("public const int ClOrdID = 11;"));
            Assert.That(FileContent(result, "Codes/VenueSideCode.cs"), Does.Contain("public const string Escaped = \"A\\\"\\\\\\n\";"));
            Assert.That(FileContent(result, "Components/VenueInstrument.cs"), Does.Contain("public global::fix.FixField Symbol = new global::fix.FixField();"));
            Assert.That(FileContent(result, "Groups/VenueLegs.cs"), Does.Contain("public global::fix.FixField LegSymbol = new global::fix.FixField();"));
            Assert.That(FileContent(result, "Messages/Venue" + baseName + ".cs"), Does.Contain("global::Venue.Generated.Fix44.Components.VenueInstrument"));
            Assert.That(FileContent(result, "Messages/Venue" + baseName + ".cs"), Does.Contain("global::System.Collections.Generic.List<global::Venue.Generated.Fix44.Groups.VenueLegs>"));
            Assert.That(FileContent(result, "Operations/Venue" + baseName + "Operations.cs"), Does.Contain("model.Message.MsgType = \"D\";"));
            Assert.That(FileContent(result, "Cracking/VenueGeneratedMessageCracker.cs"), Does.Contain("return handler.HandleVenue" + baseName + "Message(model);"));
            Assert.That(FileContent(result, "Scenarios/Auction/Cracking/VenueGeneratedMessageCracker.cs"), Does.Contain("return handler.HandleVenue" + auctionName + "Message(model);"));
        });
    }

    [Test]
    public void EmissionIsIndependentOfDictionaryInsertionOrder()
    {
        FixDictionary firstDictionary = CreateDictionary();
        FixDictionary secondDictionary = CreateDictionary();
        secondDictionary.Fields.Reverse();
        secondDictionary.CodeSets.Reverse();
        secondDictionary.CodeSets[0].Codes.Reverse();
        secondDictionary.Components.Reverse();
        secondDictionary.Groups.Reverse();
        secondDictionary.Messages.Reverse();
        secondDictionary.Scenarios.Reverse();
        ModelGenerationOptions options = new ModelGenerationOptions { Namespace = "Venue.Generated" };

        CSharpEmissionResult first = EmitDictionary(firstDictionary, options);
        CSharpEmissionResult second = EmitDictionary(secondDictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(EmissionText(second), Is.EqualTo(EmissionText(first)));
            Assert.That(secondDictionary.Fields[0].Tag, Is.EqualTo(6001));
            Assert.That(secondDictionary.Messages[0].Scenario, Is.EqualTo("auction"));
        });
    }

    [Test]
    public void UnresolvedMemberProducesAnExplicitDiagnosticAndUsableModelFile()
    {
        FixDictionary dictionary = CreateDictionary();
        dictionary.Messages[0].Members.Add(new FixMemberDefinition
        {
            Name = "MissingPart",
            Kind = FixMemberKind.Component,
            ReferenceId = 999,
            Position = 99
        });
        ModelGenerationOptions options = new ModelGenerationOptions { Namespace = "Venue.Generated" };

        CSharpEmissionResult result = EmitDictionary(dictionary, options);
        string messageName = CSharpNameResolver.ResolveNames(dictionary, options).MessageNames[CSharpNameResolver.CreateMessageKey("D", "base")];

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-model-member"));
            Assert.That(FileContent(result, "Messages/" + messageName + ".cs"), Does.Contain("public global::fix.FixMessage Message"));
            Assert.That(FileContent(result, "Messages/" + messageName + ".cs"), Does.Not.Contain("MissingPart"));
        });
    }

    private static CSharpEmissionResult EmitDictionary(FixDictionary dictionary, ModelGenerationOptions options)
    {
        CSharpNamePlan names = CSharpNameResolver.ResolveNames(dictionary, options);
        return CSharpEmitter.EmitSources(dictionary, options, names);
    }

    private static string FileContent(CSharpEmissionResult result, string path)
    {
        return result.Files.Single(item => item.Path == path).Content;
    }

    private static string EmissionText(CSharpEmissionResult result)
    {
        return string.Join("\n---\n", result.Files.Select(item => item.Path + "\n" + item.Content));
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 },
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", Description = "Client order identifier & value." },
                new FixFieldDefinition { Tag = 55, Name = "Symbol" },
                new FixFieldDefinition { Tag = 6001, Name = "LegSymbol" }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCode",
                    Codes =
                    [
                        new FixCodeDefinition { Name = "Buy", Value = "1" },
                        new FixCodeDefinition { Name = "Escaped", Value = "A\"\\\n" }
                    ]
                }
            ],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "Instrument",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Symbol", Kind = FixMemberKind.Field, ReferenceId = 55, Position = 0, Presence = FixPresence.Required }
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 20,
                    Name = "Legs",
                    CountTag = 6000,
                    DelimiterTag = 6001,
                    Members =
                    [
                        new FixMemberDefinition { Name = "LegSymbol", Kind = FixMemberKind.Field, ReferenceId = 6001, Position = 0 }
                    ]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 30,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Scenario = "base",
                    Members =
                    [
                        new FixMemberDefinition { Name = "ClOrdID", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0, Presence = FixPresence.Required },
                        new FixMemberDefinition { Name = "Instrument", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 1 },
                        new FixMemberDefinition { Name = "Legs", Kind = FixMemberKind.Group, ReferenceId = 20, Position = 2 }
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 31,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Scenario = "auction",
                    Members =
                    [
                        new FixMemberDefinition { Name = "ClOrdID", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0, Presence = FixPresence.Required }
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
}
