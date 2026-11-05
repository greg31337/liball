using System.Text;
using iso20022;

namespace iso20022.Tests;

public class OfficialRepositoryMigrationTests
{
    private RepositorySnapshot repository = new RepositorySnapshot();
    private RepositorySnapshotIndex repositoryIndex = new RepositorySnapshotIndex();

    [OneTimeSetUp]
    public void ImportOfficialRepository()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/eRepository_ISO20022_20260626.zip"));
        RepositoryImportResult result = new RepositoryImporter().ImportZipFile(
            archivePath,
            new RepositoryImportOptions
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Repository,
                    Edition = "2026-06",
                    Identifier = "iso20022-e-repository",
                    Version = "20260626"
                },
                IsApproved = true
            });

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Snapshot, Is.Not.Null, result.ErrorMessage);
        repository = result.Snapshot!;
        repositoryIndex = RepositorySnapshotIndexer.BuildIndex(repository);
    }

    [TestCase("payments", "pacs", "008", "001", "13", "14")]
    [TestCase("securities", "semt", "018", "001", "14", "15")]
    [TestCase("cards", "caaa", "001", "001", "14", "15")]
    [TestCase("FX", "fxtr", "008", "001", "07", "08")]
    [TestCase("trade", "tsmt", "009", "001", "04", "05")]
    public void ConsecutiveRealVersionsComparePlanAndMigrate(
        string domain,
        string area,
        string functionality,
        string variant,
        string sourceVersion,
        string targetVersion)
    {
        MessageIdentifier sourceIdentifier = Identifier(
            area,
            functionality,
            variant,
            sourceVersion);
        MessageIdentifier targetIdentifier = Identifier(
            area,
            functionality,
            variant,
            targetVersion);
        MessageGraphSelection sourceGraph = SelectGraph(sourceIdentifier);
        MessageGraphSelection targetGraph = SelectGraph(targetIdentifier);
        RepositorySnapshot sourceRepository = CreateGraphSnapshot(
            sourceIdentifier.Value,
            sourceGraph.Identifiers);
        RepositorySnapshot targetRepository = CreateGraphSnapshot(
            targetIdentifier.Value,
            targetGraph.Identifiers);

        Assert.That(
            HasVersionLink(sourceGraph.Message!, targetGraph.Message!),
            Is.True,
            domain + " versions are not consecutive in the official repository");

        RepositoryDiff difference = new MessageVersionComparer().CompareMessageVersions(
            new RepositoryComparisonRequest
            {
                Source = sourceRepository,
                Target = targetRepository,
                SourceMessageIdentifier = sourceIdentifier,
                TargetMessageIdentifier = targetIdentifier
            });

        MigrationPlan plan = new MigrationPlanner().PlanMigration(new MigrationRequest
        {
            SourceRepository = sourceRepository,
            TargetRepository = targetRepository,
            SourceMessageIdentifier = sourceIdentifier,
            TargetMessageIdentifier = targetIdentifier,
            Difference = difference
        });

        string rootElement = sourceGraph.Message!.RootElement;
        string sourceNamespace = sourceGraph.Message.NamespaceUri;
        MigrationResult migration = new MessageMigrator().MigrateMessage(new MigrationRequest
        {
            SourceMessageIdentifier = sourceIdentifier,
            TargetMessageIdentifier = targetIdentifier,
            Xml = Encoding.UTF8.GetBytes(
                "<" + rootElement + " xmlns=\"" + sourceNamespace + "\"/>"),
            Plan = plan,
            ValidateTarget = false
        });
        string migratedXml = Encoding.UTF8.GetString(migration.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(sourceGraph.Message.MessageIdentifier.Value,
                Is.EqualTo(sourceIdentifier.Value));
            Assert.That(targetGraph.Message!.MessageIdentifier.Value,
                Is.EqualTo(targetIdentifier.Value));
            Assert.That(difference.Success, Is.True, difference.ErrorMessage);
            Assert.That(difference.Differences, Is.Not.Empty);
            Assert.That(difference.Compatibility, Has.Count.EqualTo(1));
            Assert.That(plan.Success, Is.True, plan.ErrorMessage);
            Assert.That(plan.Difference, Is.SameAs(difference));
            Assert.That(plan.Difference?.Success, Is.True);
            Assert.That(plan.Steps, Is.Not.Empty);
            Assert.That(migration.Success, Is.True, migration.ErrorMessage);
            Assert.That(migration.FailedSteps, Is.Empty);
            Assert.That(migration.AppliedSteps, Has.Count.EqualTo(plan.Steps.Count));
            Assert.That(migration.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(targetIdentifier.Value));
            Assert.That(migratedXml, Does.Contain(targetIdentifier.Value));
        });
    }

    private MessageGraphSelection SelectGraph(MessageIdentifier identifier)
    {
        RepositoryIndexEntry entry = repositoryIndex.FindMessageIdentifier(identifier.Value).Single(
            candidate => candidate.Kind == RepositoryEntityKind.MessageDefinition);
        MessageGraphSelection selection = new MessageGraphSelection
        {
            Message = (MessageDefinitionMetadata)entry.Value!
        };
        MessageGraphSelector.BuildReachableIdentifiers(selection, repositoryIndex);
        selection.Success = true;
        return selection;
    }

    private bool HasVersionLink(
        MessageDefinitionMetadata source,
        MessageDefinitionMetadata target)
    {
        return repository.References.Any(reference =>
            reference.SourceIdentifier == source.Identifier &&
            reference.TargetIdentifier == target.Identifier &&
            reference.PropertyName == "nextVersions") ||
            repository.References.Any(reference =>
                reference.SourceIdentifier == target.Identifier &&
                reference.TargetIdentifier == source.Identifier &&
                reference.PropertyName == "previousVersion");
    }

    private RepositorySnapshot CreateGraphSnapshot(
        string messageIdentifier,
        HashSet<string> identifiers)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = repository.Identifier,
            Edition = repository.Edition + ":" + messageIdentifier,
            Provenance = repository.Provenance,
            DataDictionary = repository.DataDictionary,
            BusinessProcessCatalogue = repository.BusinessProcessCatalogue,
            References = repository.References.Where(reference =>
                identifiers.Contains(reference.SourceIdentifier) &&
                identifiers.Contains(reference.TargetIdentifier)).ToList(),
            Constraints = repository.Constraints.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            MessageDefinitions = repository.MessageDefinitions.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            MessageBuildingBlocks = repository.MessageBuildingBlocks.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            MessageComponents = repository.MessageComponents.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            MessageElements = repository.MessageElements.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            Choices = repository.Choices.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            DataTypes = repository.DataTypes.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            CodeSets = repository.CodeSets.Where(item =>
                identifiers.Contains(item.Identifier)).ToList(),
            ExternalSchemas = repository.ExternalSchemas.Where(item =>
                identifiers.Contains(item.Identifier)).ToList()
        };
        return snapshot;
    }

    private static MessageIdentifier Identifier(
        string area,
        string functionality,
        string variant,
        string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = functionality,
            Variant = variant,
            Version = version,
            Value = area + "." + functionality + "." + variant + "." + version
        };
    }
}
