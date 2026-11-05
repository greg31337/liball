using fake;

namespace fake.Tests;

[TestFixture]
public class IdentifierMetadataTests
{
    [Test]
    public void GeneratedIdentifiersPassEveryDetailedValidationContract()
    {
        Fake fake = Fake.Create(5601);
        IdentifierProvider identifiers = fake.Identifier();
        string[] values = new string[]
        {
            identifiers.Uuid4(),
            identifiers.Uuid4(),
            identifiers.Uuid7(),
            identifiers.Ulid(),
            identifiers.ObjectId(),
            identifiers.SortableTimestampId(),
            identifiers.Doi(),
            identifiers.Isbn10(),
            identifiers.Isbn13(),
            identifiers.Issn(),
            identifiers.Ean8(),
            identifiers.UpcA(),
            identifiers.Gtin(),
            identifiers.Imei(),
            identifiers.ImeiSv(),
            identifiers.Vin(),
            identifiers.Sku(),
            identifiers.Serial(),
            identifiers.Lot(),
            identifiers.Order(),
            identifiers.Invoice(),
            identifiers.Tracking()
        };
        IdentifierKind[] kinds = new IdentifierKind[]
        {
            IdentifierKind.Uuid,
            IdentifierKind.Uuid4,
            IdentifierKind.Uuid7,
            IdentifierKind.Ulid,
            IdentifierKind.ObjectId,
            IdentifierKind.SortableTimestampId,
            IdentifierKind.Doi,
            IdentifierKind.Isbn10,
            IdentifierKind.Isbn13,
            IdentifierKind.Issn,
            IdentifierKind.Ean8,
            IdentifierKind.UpcA,
            IdentifierKind.Gtin,
            IdentifierKind.Imei,
            IdentifierKind.ImeiSv,
            IdentifierKind.Vin,
            IdentifierKind.Sku,
            IdentifierKind.Serial,
            IdentifierKind.Lot,
            IdentifierKind.Order,
            IdentifierKind.Invoice,
            IdentifierKind.Tracking
        };

        int index = 0;
        while (index < kinds.Length)
        {
            ValidationResult result = identifiers.ValidateIdentifier(values[index], kinds[index]);
            Assert.That(result.Valid, Is.True, kinds[index].ToString());
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid), kinds[index].ToString());
            index++;
        }
    }

    [Test]
    public void WrongCheckDigitRetainsValidStructureButReportsInvalidChecksum()
    {
        IdentifierProvider identifiers = Fake.Create(5602).Identifier();

        ValidationResult result = identifiers.ValidateIdentifier(
            "490154203237519",
            IdentifierKind.Imei);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.ConstraintValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(result.FictionalReserved, Is.False);
            Assert.That(result.Detail, Does.Contain("checksum is invalid"));
        });
    }

    [Test]
    public void MalformedChecksumIdentifierReportsUnknownChecksum()
    {
        IdentifierProvider identifiers = Fake.Create(5603).Identifier();

        ValidationResult badCharacter = identifiers.ValidateIdentifier(
            "1G4AH59I45G118341",
            IdentifierKind.Vin);
        ValidationResult badLength = identifiers.ValidateIdentifier(
            "49015420323751",
            IdentifierKind.Imei);

        Assert.Multiple(() =>
        {
            Assert.That(badCharacter.Valid, Is.False);
            Assert.That(badCharacter.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(badCharacter.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
            Assert.That(badLength.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(badLength.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
        });
    }

    [Test]
    public void InvalidIsbnPrefixCanStillHaveAValidChecksumDimension()
    {
        IdentifierProvider identifiers = Fake.Create(5604).Identifier();

        ValidationResult result = identifiers.ValidateIdentifier(
            "9771090636073",
            IdentifierKind.Isbn13);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
        });
    }

    [Test]
    public void WrongUuidVersionIsSeparateFromCanonicalStructure()
    {
        IdentifierProvider identifiers = Fake.Create(5605).Identifier();
        string wrongVersion = "919108f7-52d1-f320-9bac-f847db4148a8";

        ValidationResult result = identifiers.ValidateIdentifier(
            wrongVersion,
            IdentifierKind.Uuid4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.ConstraintValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.NotApplicable));
            Assert.That(result.Detail, Does.Contain("kind-specific constraint"));
        });
    }

    [Test]
    public void UlidOverflowIsStructurallyEncodedButOutsideItsRangeConstraint()
    {
        IdentifierProvider identifiers = Fake.Create(5606).Identifier();

        ValidationResult result = identifiers.ValidateIdentifier(
            "80000000000000000000000000",
            IdentifierKind.Ulid);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.ConstraintValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.NotApplicable));
        });
    }

    [Test]
    public void ChecksumNotApplicableDiffersFromUnknownAllocation()
    {
        IdentifierProvider identifiers = Fake.Create(5607).Identifier();
        string value = identifiers.ImeiSvFromSoftwareVersion(42);

        ValidationResult result = identifiers.ValidateIdentifier(
            value,
            IdentifierKind.ImeiSv);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(result.FictionalReserved, Is.False);
        });
    }

    [Test]
    public void GenericKindsUseProjectFictionalReservationWithoutExternalClaims()
    {
        IdentifierProvider identifiers = Fake.Create(5608).Identifier();

        ValidationResult result = identifiers.ValidateIdentifier(
            identifiers.Sku(),
            IdentifierKind.Sku);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(result.FictionalReserved, Is.True);
            Assert.That(result.AllocationStatus, Is.EqualTo(AllocationStatus.FictionalReserved));
        });
    }

    [Test]
    public void MetadataAttachesCapturedSettingsAndTheDetailedResult()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 5609L);
        options.GenerationMode = GenerationMode.ValidUnusual;
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        Fake fake = Fake.Create(options);
        string value = fake.Identifier().VinFromModelYear(2025);

        options.Locale = "en-US";
        options.GenerationMode = GenerationMode.Invalid;
        options.SafetyMode = SafetyMode.UnrestrictedShape;
        GenerationMetadata metadata = fake.Identifier().IdentifierMetadata(
            value,
            IdentifierKind.Vin);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Value, Is.SameAs(value));
            Assert.That(metadata.Provider, Is.EqualTo("identifier"));
            Assert.That(metadata.Kind, Is.EqualTo(IdentifierKind.Vin));
            Assert.That(metadata.Locale, Is.EqualTo("fr-CA"));
            Assert.That(metadata.GenerationMode, Is.EqualTo(GenerationMode.ValidUnusual));
            Assert.That(metadata.SafetyMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(metadata.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(metadata.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(metadata.ConstraintValidity, Is.EqualTo(ValidityMode.NotApplicable));
            Assert.That(metadata.FictionalReserved, Is.False);
            Assert.That(metadata.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(metadata.Validation.Valid, Is.True);
            Assert.That(metadata.Validation.Kind, Is.EqualTo(metadata.Kind));
        });
    }

    [Test]
    public void ResultObjectsAreIndependentAcrossCalls()
    {
        IdentifierProvider identifiers = Fake.Create(5610).Identifier();
        string value = identifiers.Ean8();
        ValidationResult first = identifiers.ValidateIdentifier(value, IdentifierKind.Ean8);

        first.Valid = false;
        first.Detail = "caller mutation";
        ValidationResult second = identifiers.ValidateIdentifier(value, IdentifierKind.Ean8);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Valid, Is.True);
            Assert.That(second.Detail, Is.Not.EqualTo(first.Detail));
        });
    }

    [Test]
    public void DetailedValidationAndMetadataDoNotConsumeEntropy()
    {
        IdentifierProvider actual = Fake.Create(5611).Identifier();
        IdentifierProvider expected = Fake.Create(5611).Identifier();

        actual.ValidateIdentifier("490154203237518", IdentifierKind.Imei);
        actual.IdentifierMetadata("1G4AH59H45G118341", IdentifierKind.Vin);

        Assert.That(actual.Uuid4(), Is.EqualTo(expected.Uuid4()));
    }

    [Test]
    public void GenericStructureRejectsEmptyAndMalformedUtf16WithoutThrowing()
    {
        IdentifierProvider identifiers = Fake.Create(5612).Identifier();
        string malformed = new string(new char[] { 'A', '\ud800' });

        ValidationResult empty = identifiers.ValidateIdentifier("", IdentifierKind.Serial);
        ValidationResult invalidUnicode = identifiers.ValidateIdentifier(
            malformed,
            IdentifierKind.Tracking);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Valid, Is.False);
            Assert.That(empty.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(invalidUnicode.Valid, Is.False);
            Assert.That(invalidUnicode.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
        });
    }

    [Test]
    public void NullKindsAndCountLimitsUseConsistentErrors()
    {
        IdentifierProvider identifiers = Fake.Create(5613).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateIdentifier(null, IdentifierKind.Vin)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.IdentifierMetadata(null, IdentifierKind.Vin)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.ValidateIdentifier("value", (IdentifierKind)99)).ParamName, Is.EqualTo("kind"));
        });

        FakeOptions options = FakeOptions.Create(5614L);
        options.MaximumCount = 5;
        IdentifierProvider limited = Fake.Create(options).Identifier();

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => limited.ValidateIdentifier("123456", IdentifierKind.Serial)).ParamName,
            Is.EqualTo("value"));
    }
}
