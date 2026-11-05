using fake;

namespace fake.Tests;

[TestFixture]
public class MetadataValidationMatrixTests
{
    [Test]
    public void GeneralizedMetadataReportsEveryGenerationMode()
    {
        GenerationMode[] modes = new GenerationMode[]
        {
            GenerationMode.Typical,
            GenerationMode.Boundary,
            GenerationMode.ValidUnusual,
            GenerationMode.Invalid,
            GenerationMode.Mixed,
            GenerationMode.Adversarial
        };

        int index = 0;
        while (index < modes.Length)
        {
            GenerationMode mode = modes[index];
            FakeOptions options = FakeOptions.Create("en-US", 15000 + index);
            options.GenerationMode = mode;
            Fake value = Fake.Create(options);

            if (mode == GenerationMode.Typical ||
                mode == GenerationMode.Boundary)
            {
                IdentifierProvider identifier = value.Identifier();
                string generated = identifier.Ean8();
                GenerationMetadata metadata = identifier.IdentifierMetadata(
                    generated,
                    IdentifierKind.Ean8);

                Assert.Multiple(() =>
                {
                    Assert.That(metadata.GenerationMode, Is.EqualTo(mode));
                    Assert.That(metadata.Validation.Valid, Is.True);
                    Assert.That(metadata.RequestedEdgeCase, Is.Empty);
                    Assert.That(metadata.ViolatedRule, Is.Empty);
                    AssertDimensionsSynchronized(metadata);
                });
            }
            else
            {
                EdgeSelectionResult result = value.Edge().Select();

                Assert.Multiple(() =>
                {
                    Assert.That(result.GenerationMode, Is.EqualTo(mode));
                    Assert.That(result.Metadata.GenerationMode,
                        Is.EqualTo(mode));
                    Assert.That(result.Metadata.RequestedEdgeCase,
                        Is.EqualTo(result.SelectedEdge));
                    Assert.That(result.Metadata.ViolatedRule,
                        Is.EqualTo(result.ViolatedRule));
                    Assert.That(result.Metadata.Validation.Valid,
                        Is.EqualTo(result.Validity == ValidityMode.Valid));
                    Assert.That(result.ViolatedRule == string.Empty,
                        Is.EqualTo(result.Validity == ValidityMode.Valid));
                    AssertDimensionsSynchronized(result.Metadata);
                });
            }

            index++;
        }
    }

    [TestCase(SafetyMode.Reserved, true)]
    [TestCase(SafetyMode.SyntheticPlausible, false)]
    [TestCase(SafetyMode.UnrestrictedShape, false)]
    public void PhoneMetadataReportsEverySafetyMode(
        SafetyMode safetyMode,
        bool expectedSafeReserved)
    {
        FakeOptions options = FakeOptions.Create(
            "en-US",
            15010 + (int)safetyMode);
        options.SafetyMode = safetyMode;

        PhoneRecord record = Fake.Create(options).Phone().Record();

        Assert.Multiple(() =>
        {
            Assert.That(record.Metadata.SafetyMode,
                Is.EqualTo(safetyMode));
            Assert.That(record.SafetyMetadata.RequestedMode,
                Is.EqualTo(safetyMode));
            Assert.That(record.SafetyMetadata.AppliedMode,
                Is.EqualTo(safetyMode));
            Assert.That(record.SafetyMetadata.UsesAuthoritativeSafeRange,
                Is.EqualTo(expectedSafeReserved));
            Assert.That(record.Metadata.SafeReserved,
                Is.EqualTo(expectedSafeReserved));
            Assert.That(record.Metadata.Validation.SafeReserved,
                Is.EqualTo(expectedSafeReserved));
            Assert.That(record.Metadata.RepresentsAssignment, Is.False);
            Assert.That(record.Metadata.AllocationStatus,
                Is.EqualTo(AllocationStatus.Unknown));
            AssertDimensionsSynchronized(record.Metadata);
        });
    }

    [Test]
    public void MetadataDistinguishesLocaleFallbackFromStrictResolution()
    {
        FakeOptions fallbackOptions = FakeOptions.Create(
            "pt-BR-x-fixture",
            "en",
            15020);
        IdentifierProvider fallback = Fake.Create(fallbackOptions).Identifier();
        string fallbackValue = fallback.PassportIdentifier();
        GenerationMetadata fallbackMetadata = fallback.CivilIdentifierMetadata(
            fallbackValue,
            IdentifierKind.Passport);

        FakeOptions strictOptions = FakeOptions.Create(
            "pt-BR",
            "en",
            15021);
        strictOptions.StrictnessMode = StrictnessMode.Strict;
        IdentifierProvider strict = Fake.Create(strictOptions).Identifier();
        string strictValue = strict.PersonalTaxIdentifier();
        GenerationMetadata strictMetadata = strict.CivilIdentifierMetadata(
            strictValue,
            IdentifierKind.PersonalTax);

        FakeOptions rejectedOptions = FakeOptions.Create(
            "pt-BR-x-fixture",
            "pt-BR",
            15022);
        rejectedOptions.StrictnessMode = StrictnessMode.Strict;
        IdentifierProvider rejected = Fake.Create(rejectedOptions).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(fallbackMetadata.StrictnessMode,
                Is.EqualTo(StrictnessMode.AllowFallback));
            Assert.That(fallbackMetadata.Locale,
                Is.EqualTo("pt-BR-x-fixture"));
            Assert.That(fallbackMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(fallbackMetadata.UsedLocaleFallback, Is.True);
            Assert.That(fallbackMetadata.Validation.Valid, Is.True);
            Assert.That(strictMetadata.StrictnessMode,
                Is.EqualTo(StrictnessMode.Strict));
            Assert.That(strictMetadata.Locale, Is.EqualTo("pt-BR"));
            Assert.That(strictMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(strictMetadata.UsedLocaleFallback, Is.False);
            Assert.That(strictMetadata.Validation.Valid, Is.True);
            Assert.That(
                Assert.Throws<FakeException>(() =>
                    rejected.PassportIdentifier()).Message,
                Does.Contain("unsupported for locale 'pt-BR-x-fixture'"));
            AssertDimensionsSynchronized(fallbackMetadata);
            AssertDimensionsSynchronized(strictMetadata);
        });
    }

    [Test]
    public void IdentifierMetadataKeepsValidityDimensionsIndependent()
    {
        IdentifierProvider identifier = Fake.Create(15030).Identifier();
        string validValue = identifier.Ean8();
        char replacement = validValue[7] == '0' ? '1' : '0';
        string badChecksumValue = validValue.Substring(0, 7) + replacement;

        GenerationMetadata valid = identifier.IdentifierMetadata(
            validValue,
            IdentifierKind.Ean8);
        GenerationMetadata badChecksum = identifier.IdentifierMetadata(
            badChecksumValue,
            IdentifierKind.Ean8);
        GenerationMetadata malformed = identifier.IdentifierMetadata(
            "123",
            IdentifierKind.Ean8);
        GenerationMetadata wrongConstraint = identifier.IdentifierMetadata(
            "919108f7-52d1-f320-9bac-f847db4148a8",
            IdentifierKind.Uuid4);
        string serialValue = identifier.Serial();
        GenerationMetadata noChecksum = identifier.IdentifierMetadata(
            serialValue,
            IdentifierKind.Serial);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validation.Valid, Is.True);
            Assert.That(valid.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(valid.ChecksumValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(valid.ConstraintValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(valid.CorrelationValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(badChecksum.Validation.Valid, Is.False);
            Assert.That(badChecksum.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(badChecksum.ChecksumValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(malformed.Validation.Valid, Is.False);
            Assert.That(malformed.StructuralValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(malformed.ChecksumValidity,
                Is.EqualTo(ValidityMode.Unknown));
            Assert.That(wrongConstraint.Validation.Valid, Is.False);
            Assert.That(wrongConstraint.StructuralValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(wrongConstraint.ChecksumValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(wrongConstraint.ConstraintValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(noChecksum.Validation.Valid, Is.True);
            Assert.That(noChecksum.ChecksumValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(noChecksum.AllocationStatus,
                Is.EqualTo(AllocationStatus.FictionalReserved));
            Assert.That(noChecksum.FictionalReserved, Is.True);
            Assert.That(noChecksum.SafeReserved, Is.True);
            AssertDimensionsSynchronized(valid);
            AssertDimensionsSynchronized(badChecksum);
            AssertDimensionsSynchronized(malformed);
            AssertDimensionsSynchronized(wrongConstraint);
            AssertDimensionsSynchronized(noChecksum);
        });
    }

    [Test]
    public void CorrelatedAndContradictoryResultsReportOppositeValidity()
    {
        PhoneRecord correlated = Fake.Create(15040).Phone().Record();
        EdgeProvider edge = Fake.Create(15041).Edge();
        EdgeSelectionResult contradictory = edge.Invalid();
        int attempts = 1;
        while (contradictory.Category != EdgeCaseCategory.ContradictoryInput &&
            attempts < 2000)
        {
            contradictory = edge.Invalid();
            attempts++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(correlated.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Valid));
            Assert.That(correlated.Metadata.Validation.Valid, Is.True);
            Assert.That(contradictory.Category,
                Is.EqualTo(EdgeCaseCategory.ContradictoryInput));
            Assert.That(contradictory.Metadata.StructuralValidity,
                Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(contradictory.Metadata.CorrelationValidity,
                Is.EqualTo(ValidityMode.Invalid));
            Assert.That(contradictory.Metadata.Validation.Valid, Is.False);
            Assert.That(contradictory.Metadata.ViolatedRule, Is.Not.Empty);
            AssertDimensionsSynchronized(correlated.Metadata);
            AssertDimensionsSynchronized(contradictory.Metadata);
        });
    }

    [Test]
    public void EveryMetadataBearingResultSynchronizesDetailedValidation()
    {
        Fake value = Fake.Create(15050);
        PhoneProvider phone = value.Phone();
        PhoneRecord record = phone.Record();
        PhoneEdgeCaseRecord phoneEdge = phone.EdgeCase(
            PhoneEdgeCase.Extension);
        IccidResult iccid = phone.IccidResult();
        ImsiResult imsi = phone.ImsiResult();
        EdgeSelectionResult edge = value.Edge().Adversarial();
        IdentifierProvider identifier = value.Identifier();
        string identifierValue = identifier.Uuid4();
        GenerationMetadata identifierMetadata = identifier.IdentifierMetadata(
            identifierValue,
            IdentifierKind.Uuid4);

        Assert.Multiple(() =>
        {
            AssertDimensionsSynchronized(record.Metadata);
            AssertDimensionsSynchronized(phoneEdge.Metadata);
            AssertDimensionsSynchronized(iccid.Metadata);
            AssertDimensionsSynchronized(imsi.Metadata);
            AssertDimensionsSynchronized(edge.Metadata);
            AssertDimensionsSynchronized(identifierMetadata);
        });
    }

    static void AssertDimensionsSynchronized(GenerationMetadata metadata)
    {
        ValidationResult validation = metadata.Validation;
        Assert.That(metadata.StructuralValidity,
            Is.EqualTo(validation.StructuralValidity));
        Assert.That(metadata.ChecksumValidity,
            Is.EqualTo(validation.ChecksumValidity));
        Assert.That(metadata.ConstraintValidity,
            Is.EqualTo(validation.ConstraintValidity));
        Assert.That(metadata.CorrelationValidity,
            Is.EqualTo(validation.CorrelationValidity));
        Assert.That(metadata.FictionalReserved,
            Is.EqualTo(validation.FictionalReserved));
        Assert.That(metadata.SafeReserved,
            Is.EqualTo(validation.SafeReserved));
        Assert.That(metadata.AllocationStatus,
            Is.EqualTo(validation.AllocationStatus));
        Assert.That(metadata.RequestedEdgeCase,
            Is.EqualTo(validation.RequestedEdgeCase));
        Assert.That(metadata.ViolatedRule,
            Is.EqualTo(validation.ViolatedRule));
    }
}
