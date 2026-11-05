using iso20022;

namespace iso20022.Tests;

public class CompatibilityClassifierTests
{
    [Test]
    public void NoMaterialDifferencesAreCompatibleInEveryDimension()
    {
        RepositoryDiff difference = new RepositoryDiff { Success = true, Equivalent = true };

        CompatibilityClassifier.AppendAssessment(
            new RepositoryComparisonRequest(),
            difference);
        CompatibilityAssessment assessment = difference.Compatibility.Single();

        Assert.Multiple(() =>
        {
            Assert.That(assessment.Kind, Is.EqualTo(CompatibilityKind.Compatible));
            Assert.That(assessment.ReaderCompatible, Is.True);
            Assert.That(assessment.WriterCompatible, Is.True);
            Assert.That(assessment.SchemaCompatible, Is.True);
            Assert.That(assessment.Lossless, Is.True);
            Assert.That(assessment.SemanticallyCompatible, Is.True);
            Assert.That(assessment.Confidence, Is.EqualTo(1m));
            Assert.That(assessment.Rationale.Single(),
                Is.EqualTo("No material difference affects compatibility."));
        });
    }

    [Test]
    public void NarrowedCardinalityIsWriterIncompatibleAndRisksDataLoss()
    {
        DifferenceItem item = Difference(
            DifferenceKind.CardinalityChanged,
            RepositoryEntityKind.MessageElement,
            "Cardinality",
            "0|true|5",
            "1|true|3");
        RepositoryDiff difference = Assess(item, new RepositorySnapshot(), new RepositorySnapshot());
        CompatibilityAssessment assessment = difference.Compatibility.Single();

        Assert.Multiple(() =>
        {
            Assert.That(assessment.Kind, Is.EqualTo(CompatibilityKind.DataLoss));
            Assert.That(assessment.ReaderCompatible, Is.True);
            Assert.That(assessment.WriterCompatible, Is.False);
            Assert.That(assessment.SchemaCompatible, Is.False);
            Assert.That(assessment.Lossless, Is.False);
            Assert.That(assessment.SemanticallyCompatible, Is.True);
            Assert.That(item.BreakingChange, Is.True);
            Assert.That(item.DataLossRisk, Is.True);
            Assert.That(assessment.AffectedPaths.Select(path => path.Value),
                Is.EqualTo(new[] { "/Document/Amount" }));
            Assert.That(assessment.Rationale.Single(),
                Does.StartWith("/Document/Amount:"));
        });
    }

    [Test]
    public void WidenedCardinalityIsReaderIncompatibleButLossless()
    {
        DifferenceItem item = Difference(
            DifferenceKind.CardinalityChanged,
            RepositoryEntityKind.MessageElement,
            "Cardinality",
            "1|true|3",
            "0|true|5");
        RepositoryDiff difference = Assess(item, new RepositorySnapshot(), new RepositorySnapshot());
        CompatibilityAssessment assessment = difference.Compatibility.Single();

        Assert.Multiple(() =>
        {
            Assert.That(assessment.Kind, Is.EqualTo(CompatibilityKind.Incompatible));
            Assert.That(assessment.ReaderCompatible, Is.False);
            Assert.That(assessment.WriterCompatible, Is.True);
            Assert.That(assessment.Lossless, Is.True);
            Assert.That(item.DataLossRisk, Is.False);
        });
    }

    [Test]
    public void RenameAndMoveAreSchemaBreakingButSemanticallyLossless()
    {
        DifferenceItem renamed = Difference(
            DifferenceKind.Renamed,
            RepositoryEntityKind.MessageElement,
            "Name",
            "OldName",
            "NewName");
        DifferenceItem moved = Difference(
            DifferenceKind.Moved,
            RepositoryEntityKind.MessageElement,
            "Owner",
            "OldOwner",
            "NewOwner");
        moved.TargetPath.Value = "/Document/NewOwner/NewName";
        RepositoryDiff difference = new RepositoryDiff { Success = true };
        difference.Differences.Add(renamed);
        difference.Differences.Add(moved);

        CompatibilityClassifier.AppendAssessment(
            new RepositoryComparisonRequest(),
            difference);
        CompatibilityAssessment assessment = difference.Compatibility.Single();

        Assert.Multiple(() =>
        {
            Assert.That(assessment.Kind, Is.EqualTo(CompatibilityKind.Incompatible));
            Assert.That(assessment.ReaderCompatible, Is.False);
            Assert.That(assessment.WriterCompatible, Is.False);
            Assert.That(assessment.SchemaCompatible, Is.False);
            Assert.That(assessment.Lossless, Is.True);
            Assert.That(assessment.SemanticallyCompatible, Is.True);
            Assert.That(assessment.AffectedPaths, Has.Count.EqualTo(2));
            Assert.That(renamed.BreakingChange, Is.True);
            Assert.That(renamed.DataLossRisk, Is.False);
        });
    }

    [Test]
    public void OptionalAndRequiredTargetAdditionsHaveDirectionalWriterImpact()
    {
        RepositorySnapshot optionalTarget = SnapshotWithElement("_optional", 0);
        DifferenceItem optional = Difference(
            DifferenceKind.Added,
            RepositoryEntityKind.MessageElement,
            string.Empty,
            string.Empty,
            string.Empty);
        optional.SourceIdentifier = string.Empty;
        optional.TargetIdentifier = "_optional";
        RepositoryDiff optionalDiff = Assess(
            optional,
            new RepositorySnapshot(),
            optionalTarget);

        RepositorySnapshot requiredTarget = SnapshotWithElement("_required", 1);
        DifferenceItem required = Difference(
            DifferenceKind.Added,
            RepositoryEntityKind.MessageElement,
            string.Empty,
            string.Empty,
            string.Empty);
        required.SourceIdentifier = string.Empty;
        required.TargetIdentifier = "_required";
        RepositoryDiff requiredDiff = Assess(
            required,
            new RepositorySnapshot(),
            requiredTarget);

        Assert.Multiple(() =>
        {
            Assert.That(optionalDiff.Compatibility.Single().ReaderCompatible, Is.False);
            Assert.That(optionalDiff.Compatibility.Single().WriterCompatible, Is.True);
            Assert.That(requiredDiff.Compatibility.Single().ReaderCompatible, Is.False);
            Assert.That(requiredDiff.Compatibility.Single().WriterCompatible, Is.False);
            Assert.That(requiredDiff.Compatibility.Single().Lossless, Is.True);
        });
    }

    [Test]
    public void CodeRemovalTakesDataLossPrecedenceOverConditionalConstraintChange()
    {
        DifferenceItem code = Difference(
            DifferenceKind.CodeRemoved,
            RepositoryEntityKind.CodeValue,
            "CodeValue",
            "OLD",
            string.Empty);
        DifferenceItem constraint = Difference(
            DifferenceKind.ConstraintChanged,
            RepositoryEntityKind.Constraint,
            "ConstraintExpression",
            "old",
            "new");
        constraint.TargetPath.Value = code.TargetPath.Value;
        RepositoryDiff difference = new RepositoryDiff { Success = true };
        difference.Differences.Add(code);
        difference.Differences.Add(constraint);

        CompatibilityClassifier.AppendAssessment(
            new RepositoryComparisonRequest(),
            difference);
        CompatibilityAssessment assessment = difference.Compatibility.Single();

        Assert.Multiple(() =>
        {
            Assert.That(assessment.Kind, Is.EqualTo(CompatibilityKind.DataLoss));
            Assert.That(assessment.Lossless, Is.False);
            Assert.That(assessment.SemanticallyCompatible, Is.False);
            Assert.That(assessment.Confidence, Is.EqualTo(0.75m));
            Assert.That(assessment.AffectedPaths, Has.Count.EqualTo(1));
            Assert.That(code.DataLossRisk, Is.True);
            Assert.That(constraint.DataLossRisk, Is.False);
        });
    }

    [Test]
    public void RegistrationIsWarningAndAssessmentCanBeDisabled()
    {
        DifferenceItem registration = Difference(
            DifferenceKind.RegistrationChanged,
            RepositoryEntityKind.BusinessComponent,
            "Registration",
            "Registered",
            "Obsolete");
        RepositoryDiff warning = Assess(
            registration,
            new RepositorySnapshot(),
            new RepositorySnapshot());

        RepositorySnapshot source = new RepositorySnapshot { Identifier = "_repository" };
        RepositorySnapshot target = new RepositorySnapshot { Identifier = "_repository" };
        source.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_component",
            Name = "Account",
            Definition = "source"
        });
        target.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_component",
            Name = "Account",
            Definition = "target"
        });
        RepositoryComparisonRequest request = new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            AssessCompatibility = false
        };
        RepositoryDiff disabled = new RepositoryComparer().CompareRepositories(request);

        Assert.Multiple(() =>
        {
            Assert.That(warning.Compatibility.Single().Kind,
                Is.EqualTo(CompatibilityKind.CompatibleWithWarnings));
            Assert.That(warning.Compatibility.Single().ReaderCompatible, Is.True);
            Assert.That(registration.BreakingChange, Is.False);
            Assert.That(disabled.Success, Is.True);
            Assert.That(disabled.Compatibility, Is.Empty);
            Assert.That(disabled.Differences.All(item => !item.BreakingChange), Is.True);
        });
    }

    private static RepositoryDiff Assess(
        DifferenceItem item,
        RepositorySnapshot source,
        RepositorySnapshot target)
    {
        RepositoryDiff difference = new RepositoryDiff { Success = true };
        difference.Differences.Add(item);
        CompatibilityClassifier.AppendAssessment(
            new RepositoryComparisonRequest { Source = source, Target = target },
            difference);
        return difference;
    }

    private static DifferenceItem Difference(
        DifferenceKind kind,
        RepositoryEntityKind entityKind,
        string propertyName,
        string sourceValue,
        string targetValue)
    {
        return new DifferenceItem
        {
            Kind = kind,
            EntityKind = entityKind,
            SourceIdentifier = "_source",
            TargetIdentifier = "_target",
            PropertyName = propertyName,
            SourceValue = sourceValue,
            TargetValue = targetValue,
            MatchConfidence = 1m,
            SourcePath = new XmlPathData { Value = "/Document/Amount" },
            TargetPath = new XmlPathData { Value = "/Document/Amount" }
        };
    }

    private static RepositorySnapshot SnapshotWithElement(
        string identifier,
        int minimumOccurs)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = identifier,
            Name = identifier,
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = minimumOccurs,
                HasMaximumOccurs = true,
                MaximumOccurs = 1
            }
        });
        return snapshot;
    }
}
