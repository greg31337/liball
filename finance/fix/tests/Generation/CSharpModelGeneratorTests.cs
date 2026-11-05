using fix;

namespace fix.Tests;

[TestFixture]
public class CSharpModelGeneratorTests
{
    [Test]
    public void MissingGenerationOptionsReturnAStableDiagnostic()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateModels(CreateDictionary(), null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Files, Is.Empty);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("missing-generation-options"));
        });
    }

    [Test]
    public void SelectionUsesVersionCategoryMessageScenarioAndDependencyClosure()
    {
        FixDictionary dictionary = CreateDictionary();
        ModelGenerationOptions options = CreateSelectionOptions();

        GeneratedApiPlan plan = GeneratedApiPlanner.PlanGeneration(dictionary, options);
        GenerationResult result = new CSharpModelGenerator().GenerateModels(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanEmit, Is.True);
            Assert.That(plan.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False);
            Assert.That(plan.Dictionary.Messages.Select(item => item.MsgType), Is.EquivalentTo(new[] { "D", "8" }));
            Assert.That(plan.Dictionary.Messages.Single(item => item.MsgType == "D").Scenario, Is.EqualTo("algo"));
            Assert.That(plan.Dictionary.Messages.Single(item => item.MsgType == "8").Scenario, Is.EqualTo("base"));
            Assert.That(plan.Dictionary.Components.Select(item => item.Id), Is.EquivalentTo(new[] { 10, 11 }));
            Assert.That(plan.Dictionary.Groups, Is.Empty);
            Assert.That(plan.Dictionary.Fields.Select(item => item.Tag), Is.EquivalentTo(new[] { 11, 17, 54, 55, 95, 96, 100 }));
            Assert.That(plan.Dictionary.CodeSets.Select(item => item.Name), Is.EqualTo(new[] { "SideCode" }));
            Assert.That(plan.Dictionary.Scenarios.Select(item => item.Name), Is.EquivalentTo(new[] { "base", "algo" }));
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.Select(item => item.Code)));
            Assert.That(result.Files.Select(item => item.Path), Does.Contain("Messages/ExecutionReport.cs"));
            Assert.That(result.Files.Select(item => item.Path), Does.Contain("Scenarios/Algo/Messages/NewOrderSingle.cs"));
            Assert.That(result.Files.Any(item => item.Path.StartsWith("Groups/", StringComparison.Ordinal)), Is.False);
            Assert.That(result.Files, Is.All.Matches<GeneratedFile>(item => item.ContentHash.Length == 64));
            Assert.That(result.Manifest.Split('\n'), Has.Length.EqualTo(result.Files.Count));
        });
    }

    [Test]
    public void GeneratedFilesAndManifestAreIndependentOfDefinitionInsertionOrder()
    {
        FixDictionary firstDictionary = CreateDictionary();
        FixDictionary secondDictionary = CreateDictionary();
        ReverseDictionary(secondDictionary);
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            Scenario = "base"
        };

        GenerationResult first = new CSharpModelGenerator().GenerateModels(firstDictionary, options);
        GenerationResult second = new CSharpModelGenerator().GenerateModels(secondDictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, string.Join(",", first.Diagnostics.Select(item => item.Code)));
            Assert.That(second.Success, Is.True, string.Join(",", second.Diagnostics.Select(item => item.Code)));
            Assert.That(second.Manifest, Is.EqualTo(first.Manifest));
            Assert.That(GeneratedText(second), Is.EqualTo(GeneratedText(first)));
            Assert.That(secondDictionary.Fields[0].Tag, Is.EqualTo(453));
            Assert.That(secondDictionary.Messages[0].MsgType, Is.EqualTo("A"));
        });
    }

    [Test]
    public void PriorManifestMarksOnlyMatchingOutputsAsUnchanged()
    {
        FixDictionary dictionary = CreateDictionary();
        ModelGenerationOptions firstOptions = CreateSelectionOptions();
        CSharpModelGenerator generator = new CSharpModelGenerator();
        GenerationResult first = generator.GenerateModels(dictionary, firstOptions);
        ModelGenerationOptions sameOptions = CreateSelectionOptions();
        sameOptions.PreviousManifest = first.Manifest;
        ModelGenerationOptions partialOptions = CreateSelectionOptions();
        partialOptions.PreviousManifest = first.Manifest.Split('\n')[0];

        GenerationResult same = generator.GenerateModels(dictionary, sameOptions);
        GenerationResult partial = generator.GenerateModels(dictionary, partialOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Files, Is.All.Matches<GeneratedFile>(item => !item.IsUnchanged));
            Assert.That(same.Success, Is.True);
            Assert.That(same.Files, Is.All.Matches<GeneratedFile>(item => item.IsUnchanged));
            Assert.That(partial.Files.Count(item => item.IsUnchanged), Is.EqualTo(1));
            Assert.That(partial.Manifest, Is.EqualTo(first.Manifest));
        });
    }

    [Test]
    public void InvalidVersionLimitTargetAndManifestReturnStableDiagnostics()
    {
        FixDictionary dictionary = CreateDictionary();
        CSharpModelGenerator generator = new CSharpModelGenerator();
        ModelGenerationOptions wrongVersion = CreateSelectionOptions();
        wrongVersion.Version = "FIX.4.2";
        ModelGenerationOptions smallLimit = CreateSelectionOptions();
        smallLimit.MaxFiles = 1;
        ModelGenerationOptions wrongTarget = CreateSelectionOptions();
        wrongTarget.Target = GenerationTarget.SbeSchema;
        ModelGenerationOptions malformedManifest = CreateSelectionOptions();
        malformedManifest.PreviousManifest = "not a manifest line";

        GenerationResult versionResult = generator.GenerateModels(dictionary, wrongVersion);
        GenerationResult limitResult = generator.GenerateModels(dictionary, smallLimit);
        GenerationResult targetResult = generator.GenerateModels(dictionary, wrongTarget);
        GenerationResult manifestResult = generator.GenerateModels(dictionary, malformedManifest);

        Assert.Multiple(() =>
        {
            Assert.That(versionResult.Success, Is.False);
            Assert.That(versionResult.Files, Is.Empty);
            Assert.That(versionResult.Diagnostics.Select(item => item.Code), Does.Contain("generation-version-mismatch"));
            Assert.That(limitResult.Success, Is.False);
            Assert.That(limitResult.Files, Is.Empty);
            Assert.That(limitResult.Diagnostics.Select(item => item.Code), Does.Contain("generation-file-limit-exceeded"));
            Assert.That(targetResult.Success, Is.False);
            Assert.That(targetResult.Diagnostics.Select(item => item.Code), Does.Contain("unsupported-generation-target"));
            Assert.That(manifestResult.Success, Is.True);
            Assert.That(manifestResult.Diagnostics.Select(item => item.Code), Does.Contain("invalid-previous-manifest-line"));
        });
    }

    [Test]
    public void UnknownSelectionsAndMissingDependenciesAreReportedWithoutMutatingInput()
    {
        FixDictionary dictionary = CreateDictionary();
        dictionary.Components[1].Members.Add(new FixMemberDefinition
        {
            Name = "MissingGroup",
            Kind = FixMemberKind.Group,
            ReferenceId = 999,
            Position = 2
        });
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            MessageNames = ["NewOrderSingle", "UnknownMessage"],
            CategoryNames = ["UnknownCategory"],
            Scenario = "algo"
        };

        GenerationResult result = new CSharpModelGenerator().GenerateModels(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("unknown-generation-message"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("unknown-generation-category"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("unresolved-generation-group"));
            Assert.That(dictionary.Components[1].Members, Has.Count.EqualTo(3));
            Assert.That(dictionary.Messages, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void LatestDictionaryCanGenerateOneSelectedMessageWithItsDependencies()
    {
        DictionaryLoadResult loaded = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { Name = "FIX.Latest" });
        Assert.That(loaded.Success, Is.True, string.Join(",", loaded.Diagnostics.Select(item => item.Code)));
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Fix.Generated",
            Version = "FIX.Latest EP309",
            Scenario = "base",
            MessageNames = ["NewOrderSingle"],
            MaxFiles = 5000
        };

        GenerationResult result = new CSharpModelGenerator().GenerateModels(loaded.Dictionary!, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.Select(item => item.Code)));
            Assert.That(result.Files.Select(item => item.Path), Does.Contain("Messages/NewOrderSingle.cs"));
            Assert.That(result.Files.Count, Is.LessThan(options.MaxFiles));
            Assert.That(result.Manifest.Split('\n'), Has.Length.EqualTo(result.Files.Count));
        });
    }

    private static ModelGenerationOptions CreateSelectionOptions()
    {
        return new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            Version = "FIX.Latest EP309",
            Scenario = "algo",
            CategoryNames = ["Orders"],
            MessageNames = ["ExecutionReport"],
            FieldTags = [95]
        };
    }

    private static string GeneratedText(GenerationResult result)
    {
        return string.Join("\n---\n", result.Files.Select(item => item.Path + "\n" + item.Content + "\n" + item.ContentHash));
    }

    private static void ReverseDictionary(FixDictionary dictionary)
    {
        dictionary.Fields.Reverse();
        dictionary.CodeSets.Reverse();
        dictionary.CodeSets[0].Codes.Reverse();
        dictionary.Components.Reverse();
        for (int index = 0; index < dictionary.Components.Count; index++) dictionary.Components[index].Members.Reverse();
        dictionary.Groups.Reverse();
        for (int index = 0; index < dictionary.Groups.Count; index++) dictionary.Groups[index].Members.Reverse();
        dictionary.Messages.Reverse();
        for (int index = 0; index < dictionary.Messages.Count; index++) dictionary.Messages[index].Members.Reverse();
        dictionary.Scenarios.Reverse();
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX.Latest",
                ApplicationVersion = FixApplicationVersions.FixLatest,
                HasExtensionPack = true,
                ExtensionPack = 309
            },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCode",
                    FieldType = FixFieldType.Char,
                    Codes =
                    [
                        new FixCodeDefinition { Name = "Buy", Value = "1" },
                        new FixCodeDefinition { Name = "Sell", Value = "2" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", DatatypeName = "String" },
                new FixFieldDefinition { Tag = 17, Name = "ExecID", DatatypeName = "String" },
                new FixFieldDefinition { Tag = 54, Name = "Side", CodeSetName = "SideCode" },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", DatatypeName = "String" },
                new FixFieldDefinition { Tag = 95, Name = "RawDataLength", HasDataTag = true, DataTag = 96 },
                new FixFieldDefinition { Tag = 96, Name = "RawData", HasLengthTag = true, LengthTag = 95 },
                new FixFieldDefinition { Tag = 100, Name = "ExDestination", DatatypeName = "String" },
                new FixFieldDefinition { Tag = 448, Name = "PartyID", DatatypeName = "String" },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs" }
            ],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "Instrument",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Symbol", Kind = FixMemberKind.Field, ReferenceId = 55, Position = 0 }
                    ]
                },
                new FixComponentDefinition
                {
                    Id = 11,
                    Name = "OrderDetails",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Instrument", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 0 },
                        new FixMemberDefinition { Name = "ExDestination", Kind = FixMemberKind.Field, ReferenceId = 100, Position = 1 }
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 20,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members =
                    [
                        new FixMemberDefinition { Name = "PartyID", Kind = FixMemberKind.Field, ReferenceId = 448, Position = 0 },
                        new FixMemberDefinition { Name = "Instrument", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 1 }
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
                    Category = "Orders",
                    Scenario = "base",
                    Members =
                    [
                        new FixMemberDefinition { Name = "ClOrdID", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0 },
                        new FixMemberDefinition { Name = "Side", Kind = FixMemberKind.Field, ReferenceId = 54, Position = 1 },
                        new FixMemberDefinition { Name = "OrderDetails", Kind = FixMemberKind.Component, ReferenceId = 11, Position = 2 },
                        new FixMemberDefinition { Name = "Parties", Kind = FixMemberKind.Group, ReferenceId = 20, Position = 3 }
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 31,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Category = "Orders",
                    Scenario = "algo",
                    Members =
                    [
                        new FixMemberDefinition { Name = "ClOrdID", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0 },
                        new FixMemberDefinition { Name = "Side", Kind = FixMemberKind.Field, ReferenceId = 54, Position = 1 },
                        new FixMemberDefinition { Name = "OrderDetails", Kind = FixMemberKind.Component, ReferenceId = 11, Position = 2 }
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 32,
                    Name = "ExecutionReport",
                    MsgType = "8",
                    Category = "Reports",
                    Scenario = "base",
                    Members =
                    [
                        new FixMemberDefinition { Name = "ExecID", Kind = FixMemberKind.Field, ReferenceId = 17, Position = 0 }
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 33,
                    Name = "Logon",
                    MsgType = "A",
                    Category = "Session",
                    Scenario = "base"
                }
            ],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "algo", ParentName = "base" }
            ]
        };
    }
}
