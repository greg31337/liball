using fix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace fix.Tests;

[TestFixture]
public class GeneratedSourceCompilationTests
{
    [Test]
    public void CompleteGenerationMatchesSnapshotAndCompilesAllOutputCategories()
    {
        FixDictionary dictionary = CreateDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 });
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            TypePrefix = "Venue"
        };

        GenerationResult result = new CSharpModelGenerator().GenerateModels(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(OutputCategories(result), Is.EqualTo(new[]
            {
                "Codes", "Components", "Cracking", "Fields", "Groups", "Messages", "Operations", "Scenarios"
            }));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("identifier-collision"));
            Assert.That(result.Manifest, Is.EqualTo("""
                48eb7679cd1295bfe0b91e9905265e25686b4c9a0ee123305bc26c691cad83e3 Codes/VenueOrder.cs
                2dc7d121dae835953e1dc6738bf72aa927ea75aad3cc4ff9540317c714e7fb10 Components/VenueOrderComponent10.cs
                a641c956f5e86eaf41363b5d0777feed0de130652b0d756e5e8be7f137f9b620 Cracking/IVenueGeneratedMessageCracker.cs
                dd9bcdac348aa5c8aaa2229ffd0cffac62ba31b030b63be7f955595e0af85f28 Cracking/IVenueGeneratedMessageHandler.cs
                e42609f684feb446e8539cbfd10a9a9fd8239a706843cfa70e3f41eddc952386 Cracking/VenueGeneratedMessageCracker.cs
                30b6e6d78aaed5350939286ac2684ac9ae5ee23002a91aa80cd267536c7eedd8 Fields/VenueFixFieldTags.cs
                2e5341b17776071c048b5a26738e97b558898e787468beab8038726d9fe4c91b Groups/VenueOrderGroup.cs
                df3e3b34ec91d2ff381583b31b0a285df9b0ac6e1e7e04966349f20aaa2c805b Groups/VenueOrderGroup20.cs
                ca9be9a4a175ea414fd834f58258f84322ef4bdb670abce319649f2857b51130 Messages/VenueOrderMsgTypeD.cs
                7d2a398209329b8ac55c68e16da9ed470510241b73f21f39d7686218465a8f91 Operations/IVenueOrderMsgTypeDOperations.cs
                721d0120a6620c6eccfdd4f322efc7d67355aacb44583f8ed9de7fc0602ccaa3 Operations/VenueOrderMsgTypeDOperations.cs
                fe5578708294fe60dba46edf64e9abbbfce1871707bd05b84804f577ff518326 Scenarios/Auction/Cracking/IVenueGeneratedMessageCracker.cs
                315adc6a3b94b19635719430855f496d37c6acbde960e726faa75107002505f8 Scenarios/Auction/Cracking/IVenueGeneratedMessageHandler.cs
                2f58d9b8e4049e690068f0db2553af1dc1a96d3b251241936c5709661dcf58d2 Scenarios/Auction/Cracking/VenueGeneratedMessageCracker.cs
                624dc887c8afdbff8bbd1726bd91fd853a3910db558289dc0433cc3830b97911 Scenarios/Auction/Messages/VenueOrderMsgTypeZScenarioAuction.cs
                bc19fcc74c0ee178440439899b1697b3bc1826e0fd973ec757bb3277edf07311 Scenarios/Auction/Operations/IVenueOrderMsgTypeZScenarioAuctionOperations.cs
                50f02dd05cc49e22bf21b6a9d209f60c8a7719347f92dfad2d79a1346af43a23 Scenarios/Auction/Operations/VenueOrderMsgTypeZScenarioAuctionOperations.cs
                """));
            AssertGeneratedSourcesCompile(result.Files, "CompleteGeneratedModels");
        });
    }

    [Test]
    public void SelectedSliceIncludesNestedGroupClosureAndCompiles()
    {
        FixDictionary dictionary = CreateDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 });
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            TypePrefix = "Venue",
            Scenario = "base",
            MessageNames = ["Order"]
        };

        GenerationResult result = new CSharpModelGenerator().GenerateModels(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Files.Select(item => item.Path), Does.Contain("Groups/VenueOrderGroup20.cs"));
            Assert.That(result.Files.Select(item => item.Path), Does.Contain("Groups/VenueOrderGroup.cs"));
            Assert.That(result.Files.Select(item => item.Path), Does.Not.Contain("Scenarios/Auction/Messages/VenueOrderMsgTypeZScenarioAuction.cs"));
            Assert.That(result.Files.Select(item => item.Path), Is.EqualTo(result.Files.Select(item => item.Path).OrderBy(item => item, StringComparer.Ordinal)));
            AssertGeneratedSourcesCompile(result.Files, "SelectedGeneratedModels");
        });
    }

    [Test]
    public void FixtAndApplicationOutputsUseSeparateNamespacesAndCompileTogether()
    {
        FixDictionary transportDictionary = CreateDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 });
        FixDictionary applicationDictionary = CreateDictionary(new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2 });
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            TypePrefix = "Venue"
        };
        CSharpModelGenerator generator = new CSharpModelGenerator();

        GenerationResult transport = generator.GenerateModels(transportDictionary, options);
        GenerationResult application = generator.GenerateModels(applicationDictionary, options);
        List<GeneratedFile> combined = PrefixPaths("Transport", transport.Files);
        combined.AddRange(PrefixPaths("Application", application.Files));

        Assert.Multiple(() =>
        {
            Assert.That(transport.Success, Is.True, DiagnosticText(transport));
            Assert.That(application.Success, Is.True, DiagnosticText(application));
            Assert.That(transport.Files, Is.All.Matches<GeneratedFile>(item => item.Content.Contains("namespace Venue.Generated.Fixt11", StringComparison.Ordinal)));
            Assert.That(application.Files, Is.All.Matches<GeneratedFile>(item => item.Content.Contains("namespace Venue.Generated.Fix50Sp2", StringComparison.Ordinal)));
            AssertGeneratedSourcesCompile(combined, "CombinedTransportAndApplicationModels");
        });
    }

    [Test]
    public void RepeatedGenerationProducesIdenticalFilesAndManifest()
    {
        FixDictionary dictionary = CreateDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 });
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Namespace = "Venue.Generated",
            TypePrefix = "Venue"
        };
        CSharpModelGenerator generator = new CSharpModelGenerator();

        GenerationResult first = generator.GenerateModels(dictionary, options);
        GenerationResult second = generator.GenerateModels(dictionary, options);
        GenerationResult third = generator.GenerateModels(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, DiagnosticText(first));
            Assert.That(second.Manifest, Is.EqualTo(first.Manifest));
            Assert.That(third.Manifest, Is.EqualTo(first.Manifest));
            Assert.That(GeneratedText(second), Is.EqualTo(GeneratedText(first)));
            Assert.That(GeneratedText(third), Is.EqualTo(GeneratedText(first)));
        });
    }

    private static void AssertGeneratedSourcesCompile(List<GeneratedFile> files, string assemblyName)
    {
        List<SyntaxTree> trees = files.Select(file => CSharpSyntaxTree.ParseText(file.Content, path: file.Path)).ToList();
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            trees,
            CompilationReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream assembly = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emission = compilation.Emit(assembly);
        List<Diagnostic> errors = emission.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ToList();

        Assert.That(emission.Success, Is.True, string.Join(Environment.NewLine, errors));
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
    }

    private static List<MetadataReference> CompilationReferences()
    {
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        List<MetadataReference> references = trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(FixMessage).Assembly.Location));
        return references;
    }

    private static string[] OutputCategories(GenerationResult result)
    {
        return result.Files
            .Select(item => item.Path.Split('/')[0])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    private static List<GeneratedFile> PrefixPaths(string prefix, List<GeneratedFile> files)
    {
        return files.Select(item => new GeneratedFile
        {
            Path = prefix + "/" + item.Path,
            Content = item.Content,
            ContentHash = item.ContentHash,
            IsUnchanged = item.IsUnchanged
        }).ToList();
    }

    private static string GeneratedText(GenerationResult result)
    {
        return string.Join("\n---\n", result.Files.Select(item => item.Path + "\n" + item.Content + "\n" + item.ContentHash));
    }

    private static string DiagnosticText(GenerationResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }

    private static FixDictionary CreateDictionary(FixDictionaryIdentity identity)
    {
        return new FixDictionary
        {
            Identity = identity,
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "Order",
                    Codes =
                    [
                        new FixCodeDefinition { Name = "class", Value = "1" },
                        new FixCodeDefinition { Name = "Class", Value = "2" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "class" },
                new FixFieldDefinition { Tag = 12, Name = "Order-ID", CodeSetName = "Order" },
                new FixFieldDefinition { Tag = 13, Name = "Order ID" },
                new FixFieldDefinition { Tag = 1000, Name = "NoItems" },
                new FixFieldDefinition { Tag = 1001, Name = "ItemID" },
                new FixFieldDefinition { Tag = 2000, Name = "NoContainers" }
            ],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "Order",
                    Members =
                    [
                        new FixMemberDefinition { Name = "class", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0 }
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 20,
                    Name = "Order",
                    CountTag = 1000,
                    DelimiterTag = 1001,
                    Members =
                    [
                        new FixMemberDefinition { Name = "ItemID", Kind = FixMemberKind.Field, ReferenceId = 1001, Position = 0 }
                    ]
                },
                new FixGroupDefinition
                {
                    Id = 21,
                    Name = "Order Group",
                    CountTag = 2000,
                    DelimiterTag = 1001,
                    Members =
                    [
                        new FixMemberDefinition { Name = "Items", Kind = FixMemberKind.Group, ReferenceId = 20, Position = 0 }
                    ]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 30,
                    Name = "Order",
                    MsgType = "D",
                    Category = "Orders",
                    Scenario = "base",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Order", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 0 },
                        new FixMemberDefinition { Name = "Order-ID", Kind = FixMemberKind.Field, ReferenceId = 12, Position = 1 },
                        new FixMemberDefinition { Name = "Containers", Kind = FixMemberKind.Group, ReferenceId = 21, Position = 2 }
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 31,
                    Name = "Order",
                    MsgType = "Z",
                    Category = "Orders",
                    Scenario = "auction",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Order ID", Kind = FixMemberKind.Field, ReferenceId = 13, Position = 0 }
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
