using System.IO;
using fix;

namespace fix.Tests;

[TestFixture]
public class Ep309ImportCoverageTests
{
    [Test]
    public void OfficialRepositoryEnumeratesCompleteRawAndResolvableCanonicalInventories()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        OrchestraReadResult repositoryResult;
        DictionaryLoadResult dictionaryResult;
        using (FileStream repositorySource = File.OpenRead(path))
        {
            repositoryResult = new OrchestraReader().ReadRepository(repositorySource, new OrchestraReadOptions { BaseUri = path });
        }
        using (FileStream dictionarySource = File.OpenRead(path))
        {
            dictionaryResult = new OrchestraDictionaryLoader().LoadDictionary(dictionarySource, new FixDictionaryOptions());
        }

        Assert.That(repositoryResult.Success, Is.True, OrchestraCodes(repositoryResult));
        Assert.That(repositoryResult.Repository, Is.Not.Null);
        Assert.That(dictionaryResult.Success, Is.True, DictionaryCodes(dictionaryResult));
        Assert.That(dictionaryResult.Dictionary, Is.Not.Null);
        OrchestraRepository repository = repositoryResult.Repository!;
        FixDictionary dictionary = dictionaryResult.Dictionary!;
        string[] categoryNames = repository.Categories.Select(item => item.Name).Order(StringComparer.Ordinal).ToArray();
        string[] messageCategoryNames = dictionary.Messages.Select(item => item.Category).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] unresolved = dictionaryResult.Diagnostics
            .Where(item => item.Code.StartsWith("unresolved-", StringComparison.Ordinal) || item.Code == "invalid-length-data-pair")
            .Select(item => item.Code + ":" + item.DefinitionName)
            .ToArray();
        int rawCodeCount = repository.CodeSets.Sum(item => item.Codes.Count);
        int canonicalCodeCount = dictionary.CodeSets.Sum(item => item.Codes.Count);
        DictionaryIndex index = DictionaryNormalizer.CreateDictionaryIndex(dictionary, dictionary.Options);

        Assert.Multiple(() =>
        {
            Assert.That(repository.Metadata.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(repository.Metadata.ExtensionPack, Is.EqualTo(309));
            Assert.That(repository.Categories, Has.Count.EqualTo(34));
            Assert.That(repository.Datatypes, Has.Count.EqualTo(38));
            Assert.That(repository.CodeSets, Has.Count.EqualTo(694));
            Assert.That(repository.Fields, Has.Count.EqualTo(6203));
            Assert.That(repository.Components, Has.Count.EqualTo(167));
            Assert.That(repository.Groups, Has.Count.EqualTo(580));
            Assert.That(repository.Messages, Has.Count.EqualTo(181));
            Assert.That(rawCodeCount, Is.EqualTo(5745));
            Assert.That(categoryNames, Is.EqualTo(ExpectedCategories));

            Assert.That(dictionary.Identity.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(dictionary.Identity.HasExtensionPack, Is.True);
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(dictionary.Datatypes, Has.Count.EqualTo(38));
            Assert.That(dictionary.CodeSets, Has.Count.EqualTo(694));
            Assert.That(dictionary.Fields, Has.Count.EqualTo(6203));
            Assert.That(dictionary.Components, Has.Count.EqualTo(165));
            Assert.That(dictionary.Groups, Has.Count.EqualTo(580));
            Assert.That(dictionary.Messages, Has.Count.EqualTo(181));
            Assert.That(canonicalCodeCount, Is.EqualTo(5745));
            Assert.That(index.DefinitionCount, Is.EqualTo(13606));
            Assert.That(dictionary.Header, Is.Not.Empty);
            Assert.That(dictionary.Trailer, Is.Not.Empty);
            Assert.That(dictionary.Components.Select(item => item.Name), Does.Not.Contain("StandardHeader"));
            Assert.That(dictionary.Components.Select(item => item.Name), Does.Not.Contain("StandardTrailer"));
            Assert.That(messageCategoryNames, Has.Length.EqualTo(31));
            Assert.That(messageCategoryNames, Is.SubsetOf(categoryNames));
            Assert.That(unresolved, Is.Empty, string.Join(Environment.NewLine, unresolved));
            Assert.That(dictionaryResult.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(item => item.Severity == ValidationSeverity.Warning));
        });
    }

    private static readonly string[] ExpectedCategories =
    [
        "AccountReporting",
        "Allocation",
        "Application",
        "BusinessReject",
        "CollateralManagement",
        "Common",
        "Confirmation",
        "CrossOrders",
        "EventCommunication",
        "Fields",
        "ImplFields",
        "Indication",
        "MarginRequirementManagement",
        "MarketData",
        "MarketStructureReferenceData",
        "MultilegOrders",
        "Network",
        "OrderMassHandling",
        "PartiesAction",
        "PartiesReferenceData",
        "PayManagement",
        "PositionMaintenance",
        "ProgramTrading",
        "QuotationNegotiation",
        "RegistrationInstruction",
        "SecuritiesReferenceData",
        "Session",
        "SettlementInstruction",
        "SettlementStatusManagement",
        "SingleGeneralOrderHandling",
        "Testing",
        "TradeCapture",
        "TradeManagement",
        "UserManagement"
    ];

    private static string OrchestraCodes(OrchestraReadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }

    private static string DictionaryCodes(DictionaryLoadResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ":" + item.DefinitionName));
    }
}
