using System.Linq;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class GeneratorContractTests
{
    [TestCase("csharp")]
    [TestCase("sbe")]
    public void ImplementationsShareDeterministicCallerOwnedGeneration(string target)
    {
        FixDictionary dictionary = DictionaryImplementationContractTests.CanonicalDictionary();

        GenerationResult first = Generate(target, dictionary);
        GenerationResult second = Generate(target, dictionary);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Codes(first));
            Assert.That(second.Success, Is.True, Codes(second));
            Assert.That(first.Files, Is.Not.Empty);
            Assert.That(second.Manifest, Is.EqualTo(first.Manifest));
            Assert.That(second.Files.Select(file => file.Path), Is.EqualTo(first.Files.Select(file => file.Path)));
            Assert.That(second.Files.Select(file => file.Content), Is.EqualTo(first.Files.Select(file => file.Content)));
            Assert.That(first.Files, Is.All.Matches<GeneratedFile>(file => file.ContentHash.Length == 64));
            Assert.That(dictionary.Messages.Single().Name, Is.EqualTo("Order"));
            Assert.That(dictionary.Messages.Single().Members.Single().ReferenceId, Is.EqualTo(11));
            Assert.That(dictionary.Fields.Single().Name, Is.EqualTo("ClOrdID"));
        });
    }

    private static GenerationResult Generate(string target, FixDictionary dictionary)
    {
        if (target == "csharp")
        {
            IModelGenerator generator = new CSharpModelGenerator();
            return generator.GenerateModels(dictionary, new ModelGenerationOptions { Namespace = "Contract.Generated" });
        }
        ISbeSchemaGenerator sbe = new SbeSchemaGenerator();
        return sbe.GenerateSbeSchema(dictionary, new SbeGenerationOptions { Package = "contract.generated", SchemaId = 7, FirstTemplateId = 100 });
    }

    private static string Codes(GenerationResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code));
    }
}
