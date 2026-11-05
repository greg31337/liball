using fake;

namespace fake.Tests;

[TestFixture]
public class IdentifierMutationTests
{
    [Test]
    public void ValidUnusualFixturesExerciseEverySupportedBoundary()
    {
        Fake fake = Fake.Create(5501);
        IdentifierProvider identifiers = fake.Identifier();
        ChecksumProvider checksums = fake.Checksum();

        string checksum = identifiers.ValidUnusual(IdentifierMutationKind.Checksum);
        string length = identifiers.ValidUnusual(IdentifierMutationKind.Length);
        string character = identifiers.ValidUnusual(IdentifierMutationKind.Character);
        string prefix = identifiers.ValidUnusual(IdentifierMutationKind.Prefix);
        string version = identifiers.ValidUnusual(IdentifierMutationKind.Version);
        string overflowBoundary = identifiers.ValidUnusual(IdentifierMutationKind.Overflow);

        Assert.Multiple(() =>
        {
            Assert.That(checksum, Does.EndWith("X"));
            Assert.That(checksums.Isbn10().Validate(checksum), Is.True);
            Assert.That(length.Length, Is.EqualTo(8));
            Assert.That(identifiers.ValidateEan8(length), Is.True);
            Assert.That(character[8], Is.EqualTo('X'));
            Assert.That(identifiers.ValidateVin(character), Is.True);
            Assert.That(prefix, Does.StartWith("979"));
            Assert.That(checksums.Isbn13().Validate(prefix), Is.True);
            Assert.That(version, Is.EqualTo(identifiers.UuidNil()));
            Assert.That(identifiers.ValidateUuid(version), Is.True);
            Assert.That(overflowBoundary, Is.EqualTo(identifiers.UlidMax()));
            Assert.That(identifiers.ValidateUlid(overflowBoundary), Is.True);
        });
    }

    [Test]
    public void InvalidFixturesTargetTheirNamedValidators()
    {
        Fake fake = Fake.Create(5502);
        IdentifierProvider identifiers = fake.Identifier();
        ChecksumProvider checksums = fake.Checksum();

        string checksum = identifiers.Invalid(IdentifierMutationKind.Checksum);
        string length = identifiers.Invalid(IdentifierMutationKind.Length);
        string character = identifiers.Invalid(IdentifierMutationKind.Character);
        string prefix = identifiers.Invalid(IdentifierMutationKind.Prefix);
        string version = identifiers.Invalid(IdentifierMutationKind.Version);
        string overflow = identifiers.Invalid(IdentifierMutationKind.Overflow);

        Assert.Multiple(() =>
        {
            Assert.That(checksums.Isbn10().Validate(checksum), Is.False);
            Assert.That(identifiers.ValidateEan8(length), Is.False);
            Assert.That(identifiers.ValidateVin(character), Is.False);
            Assert.That(checksums.Isbn13().Validate(prefix), Is.False);
            Assert.That(checksums.Modulo10().Validate(prefix), Is.True);
            Assert.That(identifiers.ValidateUuid(version), Is.True);
            Assert.That(identifiers.ValidateUuid4(version), Is.False);
            Assert.That(identifiers.ValidateUuid7(version), Is.False);
            Assert.That(identifiers.ValidateUlid(overflow), Is.False);
        });
    }

    [Test]
    public void ChecksumMutationChangesTheImeiCheckDigitOnly()
    {
        IdentifierProvider identifiers = Fake.Create(5503).Identifier();
        string value = "490154203237518";

        string mutated = identifiers.InvalidFromValueAndKind(value, IdentifierMutationKind.Checksum);

        Assert.Multiple(() =>
        {
            Assert.That(mutated.Substring(0, 14), Is.EqualTo(value.Substring(0, 14)));
            Assert.That(mutated[14], Is.Not.EqualTo(value[14]));
            Assert.That(identifiers.ValidateImei(mutated), Is.False);
        });
    }

    [Test]
    public void ChecksumMutationRecognizesTheVinPositionNineCheckDigit()
    {
        IdentifierProvider identifiers = Fake.Create(5504).Identifier();
        string value = "1G4AH59H45G118341";

        string mutated = identifiers.InvalidFromValueAndKind(value, IdentifierMutationKind.Checksum);

        Assert.Multiple(() =>
        {
            Assert.That(mutated.Substring(0, 8), Is.EqualTo(value.Substring(0, 8)));
            Assert.That(mutated[8], Is.Not.EqualTo(value[8]));
            Assert.That(mutated.Substring(9), Is.EqualTo(value.Substring(9)));
            Assert.That(identifiers.ValidateVin(mutated), Is.False);
        });
    }

    [Test]
    public void LengthMutationRemovesOneCompleteUnicodeScalar()
    {
        IdentifierProvider identifiers = Fake.Create(5505).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.InvalidFromValueAndKind("ABC", IdentifierMutationKind.Length), Is.EqualTo("AB"));
            Assert.That(identifiers.InvalidFromValueAndKind("A🙂", IdentifierMutationKind.Length), Is.EqualTo("A"));
            Assert.That(identifiers.ValidateUuid(identifiers.InvalidFromValueAndKind(identifiers.Uuid4(), IdentifierMutationKind.Length)), Is.False);
        });
    }

    [Test]
    public void CharacterMutationPreservesLengthAndBreaksRestrictedSyntax()
    {
        IdentifierProvider identifiers = Fake.Create(5506).Identifier();
        string doi = "10.1234/abcdef12";

        string explicitMutation = identifiers.InvalidFromValueAndKind(doi, IdentifierMutationKind.Character);
        string defaultMutation = identifiers.InvalidFromValue(doi);

        Assert.Multiple(() =>
        {
            Assert.That(explicitMutation.Length, Is.EqualTo(doi.Length));
            Assert.That(explicitMutation, Does.Contain(" "));
            Assert.That(identifiers.ValidateDoi(explicitMutation), Is.False);
            Assert.That(defaultMutation, Is.EqualTo(explicitMutation));
        });
    }

    [Test]
    public void PrefixMutationKeepsAnIsbn13ChecksumWhileBreakingItsPrefixRule()
    {
        Fake fake = Fake.Create(5507);
        IdentifierProvider identifiers = fake.Identifier();
        ChecksumProvider checksums = fake.Checksum();
        string value = "9791090636071";

        string mutated = identifiers.InvalidFromValueAndKind(value, IdentifierMutationKind.Prefix);

        Assert.Multiple(() =>
        {
            Assert.That(mutated, Is.EqualTo("9771090636073"));
            Assert.That(checksums.Modulo10().Validate(mutated), Is.True);
            Assert.That(checksums.Isbn13().Validate(mutated), Is.False);
        });
    }

    [Test]
    public void VersionMutationPreservesCanonicalUuidSyntaxButBreaksVersionValidation()
    {
        IdentifierProvider identifiers = Fake.Create(5508).Identifier();
        string value = identifiers.Uuid4();

        string mutated = identifiers.InvalidFromValueAndKind(value, IdentifierMutationKind.Version);

        Assert.Multiple(() =>
        {
            Assert.That(mutated[14], Is.EqualTo('f'));
            Assert.That(identifiers.ValidateUuid(mutated), Is.True);
            Assert.That(identifiers.ValidateUuid4(mutated), Is.False);
            Assert.That(identifiers.ValidateUuid7(mutated), Is.False);
        });
    }

    [Test]
    public void OverflowMutationMovesAValidUlidPastItsTextualMaximum()
    {
        IdentifierProvider identifiers = Fake.Create(5509).Identifier();
        string value = identifiers.UlidMax();

        string mutated = identifiers.InvalidFromValueAndKind(value, IdentifierMutationKind.Overflow);

        Assert.Multiple(() =>
        {
            Assert.That(mutated, Is.EqualTo("8ZZZZZZZZZZZZZZZZZZZZZZZZZ"));
            Assert.That(identifiers.ValidateUlid(mutated), Is.False);
        });
    }

    [Test]
    public void NonIsbnPrefixMutationUsesUnsupportedWhitespace()
    {
        IdentifierProvider identifiers = Fake.Create(5510).Identifier();
        string doi = "10.1234/abcdef12";

        string mutated = identifiers.InvalidFromValueAndKind(doi, IdentifierMutationKind.Prefix);

        Assert.Multiple(() =>
        {
            Assert.That(mutated.Substring(1), Is.EqualTo(doi.Substring(1)));
            Assert.That(mutated[0], Is.EqualTo(' '));
            Assert.That(identifiers.ValidateDoi(mutated), Is.False);
        });
    }

    [Test]
    public void MutationsDoNotConsumeIdentifierEntropy()
    {
        IdentifierProvider actual = Fake.Create(5511).Identifier();
        IdentifierProvider expected = Fake.Create(5511).Identifier();

        actual.ValidUnusual(IdentifierMutationKind.Checksum);
        actual.Invalid(IdentifierMutationKind.Overflow);
        actual.InvalidFromValueAndKind("490154203237518", IdentifierMutationKind.Checksum);
        actual.InvalidFromValue("10.1234/abcdef12");

        Assert.That(actual.Uuid4(), Is.EqualTo(expected.Uuid4()));
    }

    [Test]
    public void InvalidArgumentsUseConsistentParameters()
    {
        IdentifierProvider identifiers = Fake.Create(5512).Identifier();
        string malformed = new string(new char[] { 'A', '\ud800' });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.InvalidFromValue((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.InvalidFromValueAndKind("", IdentifierMutationKind.Length)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.InvalidFromValueAndKind(malformed, IdentifierMutationKind.Character)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.InvalidFromValueAndKind("not-a-uuid", IdentifierMutationKind.Version)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.InvalidFromValueAndKind("not-a-ulid", IdentifierMutationKind.Overflow)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.InvalidFromValueAndKind("ABC", (IdentifierMutationKind)99)).ParamName, Is.EqualTo("kind"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.ValidUnusual((IdentifierMutationKind)(-1))).ParamName, Is.EqualTo("kind"));
        });
    }

    [Test]
    public void CountAndOutputLimitsAreEnforcedWithoutEntropyUse()
    {
        FakeOptions countOptions = FakeOptions.Create(5513L);
        countOptions.MaximumCount = 5;
        IdentifierProvider countActual = Fake.Create(countOptions).Identifier();
        IdentifierProvider countExpected = Fake.Create(countOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => countActual.InvalidFromValueAndKind("123456", IdentifierMutationKind.Character));
        Assert.That(countActual.RandomValue.Int(), Is.EqualTo(countExpected.RandomValue.Int()));

        FakeOptions outputOptions = FakeOptions.Create(5514L);
        outputOptions.MaximumOutputSize = 5;
        IdentifierProvider outputActual = Fake.Create(outputOptions).Identifier();
        IdentifierProvider outputExpected = Fake.Create(outputOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => outputActual.Invalid(IdentifierMutationKind.Version));
        Assert.Throws<ArgumentOutOfRangeException>(() => outputActual.InvalidFromValueAndKind("123456", IdentifierMutationKind.Character));
        Assert.That(outputActual.RandomValue.Int(), Is.EqualTo(outputExpected.RandomValue.Int()));
    }
}
