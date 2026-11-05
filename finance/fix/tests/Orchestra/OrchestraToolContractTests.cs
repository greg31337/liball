using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraToolContractTests
{
    [Test]
    public void PublicToolsComposeIntoADeterministicIndependentRoundTrip()
    {
        FixDictionary dictionary = DictionaryImplementationContractTests.CanonicalDictionary();
        IOrchestraMapper mapper = new OrchestraMapper();
        OrchestraReadResult mapped = mapper.MapDictionaryToRepository(dictionary, OrchestraVersion.Version11);
        Assert.That(mapped.Success, Is.True, OrchestraCodes(mapped));
        OrchestraRepository repository = mapped.Repository!;
        IOrchestraValidator validator = new OrchestraValidator();
        OrchestraReadResult validated = validator.ValidateRepository(repository);
        IOrchestraSlicer slicer = new OrchestraSlicer();
        OrchestraReadResult sliced = slicer.SliceRepository(repository, ["Order"], ["base"]);
        Assert.That(sliced.Success, Is.True, OrchestraCodes(sliced));
        using MemoryStream destination = new MemoryStream();
        IOrchestraWriter writer = new OrchestraWriter();
        OrchestraReadResult written = writer.WriteRepository(destination, sliced.Repository!, new OrchestraWriteOptions());
        using MemoryStream repeatedDestination = new MemoryStream();
        OrchestraReadResult repeatedWrite = writer.WriteRepository(repeatedDestination, sliced.Repository!, new OrchestraWriteOptions());
        destination.Position = 0;
        repeatedDestination.Position = 0;
        IOrchestraReader reader = new OrchestraReader();
        OrchestraReadResult read = reader.ReadRepository(destination, new OrchestraReadOptions { ValidateSchema = false });
        OrchestraReadResult repeatedRead = reader.ReadRepository(repeatedDestination, new OrchestraReadOptions { ValidateSchema = false });
        IOrchestraDiffer differ = new OrchestraDiffer();
        OrchestraDiff difference = differ.DiffRepositories(read.Repository!, repeatedRead.Repository!);
        DictionaryLoadResult roundTripped = mapper.MapRepositoryToDictionary(read.Repository!, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validated.Success, Is.True, OrchestraCodes(validated));
            Assert.That(written.Success, Is.True, OrchestraCodes(written));
            Assert.That(repeatedWrite.Success, Is.True, OrchestraCodes(repeatedWrite));
            Assert.That(read.Success, Is.True, OrchestraCodes(read));
            Assert.That(repeatedRead.Success, Is.True, OrchestraCodes(repeatedRead));
            Assert.That(roundTripped.Success, Is.True, DictionaryCodes(roundTripped));
            Assert.That(destination.CanRead, Is.True);
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(destination.ToArray(), Is.EqualTo(repeatedDestination.ToArray()));
            Assert.That(difference.Diagnostics, Is.Empty);
            Assert.That(difference.Changes, Is.Empty);
            Assert.That(sliced.Repository, Is.Not.SameAs(repository));
            Assert.That(sliced.Repository!.Messages.Single(), Is.Not.SameAs(repository.Messages.Single()));
            Assert.That(dictionary.Messages.Single().Name, Is.EqualTo("Order"));
            Assert.That(repository.Messages.Single().Name, Is.EqualTo("Order"));
            Assert.That(roundTripped.Dictionary!.Messages.Single().Name, Is.EqualTo("Order"));
        });
    }

    [Test]
    public void RuleEvaluatorUsesTheDictionaryWithoutMutatingInputs()
    {
        FixDictionary dictionary = DictionaryImplementationContractTests.CanonicalDictionary();
        FixMemberDefinition member = new FixMemberDefinition
        {
            Name = "ClOrdID",
            Kind = FixMemberKind.Field,
            ReferenceId = 11,
            Presence = FixPresence.Required,
            PresenceRule = "exists(11)"
        };
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields = [new FixField { Tag = 11, Value = Encoding.ASCII.GetBytes("ORDER-1") }]
        };
        byte[] original = message.BodyFields[0].Value.ToArray();
        IRuleEvaluator evaluator = new OrchestraRuleEvaluator(dictionary);

        RuleEvaluationResult result = evaluator.EvaluateRule(member, message, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.WasEvaluated, Is.True, result.ErrorCode + ":" + result.ErrorMessage);
            Assert.That(result.ConditionMatched, Is.True);
            Assert.That(result.Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(message.BodyFields[0].Value, Is.EqualTo(original));
            Assert.That(member.PresenceRule, Is.EqualTo("exists(11)"));
            Assert.That(dictionary.Fields.Single().Name, Is.EqualTo("ClOrdID"));
        });
    }

    private static string OrchestraCodes(OrchestraReadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code));
    }

    private static string DictionaryCodes(DictionaryLoadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code));
    }
}
