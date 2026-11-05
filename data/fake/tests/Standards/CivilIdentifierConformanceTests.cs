using fake;

namespace fake.Tests;

[TestFixture]
public class CivilIdentifierConformanceTests
{
    static IdentifierKind[] Kinds = new IdentifierKind[]
    {
        IdentifierKind.Passport,
        IdentifierKind.DrivingLicence,
        IdentifierKind.NationalIdentity,
        IdentifierKind.SocialIdentity,
        IdentifierKind.PersonalTax,
        IdentifierKind.BusinessRegistration,
        IdentifierKind.VehicleRegistration
    };

    static string[] ValidVectors = new string[]
    {
        "AB123456",
        "02650306461",
        "12345678909",
        "10024126109",
        "12345678909",
        "12ABC34501DE35",
        "ABC1D23"
    };

    static string[] InvalidCharacters = new string[]
    {
        "aB123456",
        "0265030646A",
        "1234567890A",
        "1002412610A",
        "1234567890A",
        "12ABC34501De35",
        "ABCAD23"
    };

    static string[] WrongChecksums = new string[]
    {
        string.Empty,
        "02650306462",
        "12345678908",
        "10024126108",
        "12345678908",
        "12ABC34501DE34",
        string.Empty
    };

    static string[] Rules = new string[]
    {
        "br-passport-ordinary-v1",
        "br-cnh-v1",
        "br-cin-cpf-v1",
        "br-pis-nis-v1",
        "br-cpf-v1",
        "br-cnpj-alphanumeric-2026-v1",
        "br-mercosur-plate-v1"
    };

    [Test]
    public void AuthoritativeBrazilianVectorsCoverEverySupportedKind()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5801).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            ValidationResult result = identifiers.ValidateIdentifier(
                ValidVectors[index],
                Kinds[index]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Kind, Is.EqualTo(Kinds[index]));
                Assert.That(result.Valid, Is.True, Kinds[index].ToString());
                Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
                Assert.That(result.ConstraintValidity, Is.EqualTo(ValidityMode.NotApplicable));
                Assert.That(
                    result.ChecksumValidity,
                    Is.EqualTo(HasChecksum(Kinds[index])
                        ? ValidityMode.Valid
                        : ValidityMode.NotApplicable));
            });
            index++;
        }
    }

    [Test]
    public void EveryKindRejectsShortLongAndInvalidCharacterForms()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5802).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            string valid = ValidVectors[index];
            ValidationResult shortResult = identifiers.ValidateIdentifier(
                valid.Substring(0, valid.Length - 1),
                Kinds[index]);
            ValidationResult longResult = identifiers.ValidateIdentifier(
                valid + "0",
                Kinds[index]);
            ValidationResult characterResult = identifiers.ValidateIdentifier(
                InvalidCharacters[index],
                Kinds[index]);
            Assert.Multiple(() =>
            {
                Assert.That(shortResult.Valid, Is.False, Kinds[index].ToString());
                Assert.That(shortResult.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
                Assert.That(longResult.Valid, Is.False, Kinds[index].ToString());
                Assert.That(longResult.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
                Assert.That(characterResult.Valid, Is.False, Kinds[index].ToString());
                Assert.That(characterResult.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
                Assert.That(
                    shortResult.ChecksumValidity,
                    Is.EqualTo(HasChecksum(Kinds[index])
                        ? ValidityMode.Unknown
                        : ValidityMode.NotApplicable));
            });
            index++;
        }
    }

    [Test]
    public void EveryChecksumKindSeparatesChecksumFailureFromStructure()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5803).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            if (HasChecksum(Kinds[index]))
            {
                ValidationResult result = identifiers.ValidateIdentifier(
                    WrongChecksums[index],
                    Kinds[index]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Valid, Is.False, Kinds[index].ToString());
                    Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
                    Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
                    Assert.That(result.Detail, Does.Contain("checksum is invalid"));
                });
            }
            index++;
        }
    }

    [Test]
    public void EqualDigitSensitiveIdentifiersAreRejectedBeforeChecksum()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5804).Identifier();
        IdentifierKind[] kinds = new IdentifierKind[]
        {
            IdentifierKind.DrivingLicence,
            IdentifierKind.NationalIdentity,
            IdentifierKind.SocialIdentity,
            IdentifierKind.PersonalTax
        };

        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            int digit = 0;
            while (digit < 10)
            {
                string value = new string((char)('0' + digit), 11);
                ValidationResult result = identifiers.ValidateIdentifier(
                    value,
                    kinds[kindIndex]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
                    Assert.That(result.ChecksumValidity, Is.EqualTo(ValidityMode.Unknown));
                    Assert.That(result.Valid, Is.False);
                });
                digit++;
            }
            kindIndex++;
        }
    }

    [Test]
    public void MetadataForEveryKindNeverClaimsReservationOrAssignment()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5805).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            GenerationMetadata metadata = identifiers.CivilIdentifierMetadata(
                ValidVectors[index],
                Kinds[index]);
            Assert.Multiple(() =>
            {
                Assert.That(metadata.Value, Is.EqualTo(ValidVectors[index]));
                Assert.That(metadata.Provider, Is.EqualTo("identifier"));
                Assert.That(metadata.Kind, Is.EqualTo(Kinds[index]));
                Assert.That(metadata.Locale, Is.EqualTo("pt-BR"));
                Assert.That(metadata.SourceLocale, Is.EqualTo("pt-BR"));
                Assert.That(metadata.UsedLocaleFallback, Is.False);
                Assert.That(metadata.Rule, Is.EqualTo(Rules[index]));
                Assert.That(metadata.Format, Is.Not.Empty);
                Assert.That(metadata.Provenance, Does.Contain("Brazilian federal"));
                Assert.That(metadata.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
                Assert.That(metadata.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
                Assert.That(metadata.Validation.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
                Assert.That(metadata.FictionalReserved, Is.False);
                Assert.That(metadata.Validation.FictionalReserved, Is.False);
                Assert.That(metadata.RepresentsAssignment, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void ParentAndConfiguredFallbackAreObservableAndStrictModeRejectsThem()
    {
        FakeOptions parentOptions = FakeOptions.Create("pt-BR-x-fixture", "en", 5806);
        IdentifierProvider parent = Fake.Create(parentOptions).Identifier();
        string parentValue = parent.PassportIdentifier();
        GenerationMetadata parentMetadata = parent.CivilIdentifierMetadata(
            parentValue,
            IdentifierKind.Passport);

        FakeOptions configuredOptions = FakeOptions.Create("de-DE", "pt-BR", 5806);
        IdentifierProvider configured = Fake.Create(configuredOptions).Identifier();
        string configuredValue = configured.VehicleRegistrationIdentifier();
        GenerationMetadata configuredMetadata = configured.CivilIdentifierMetadata(
            configuredValue,
            IdentifierKind.VehicleRegistration);

        FakeOptions strictOptions = FakeOptions.Create(
            "pt-BR-x-fixture",
            "pt-BR",
            5806);
        strictOptions.StrictnessMode = StrictnessMode.Strict;
        IdentifierProvider strict = Fake.Create(strictOptions).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(parentMetadata.Locale, Is.EqualTo("pt-BR-x-fixture"));
            Assert.That(parentMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(parentMetadata.UsedLocaleFallback, Is.True);
            Assert.That(parentMetadata.Validation.Valid, Is.True);
            Assert.That(configuredMetadata.Locale, Is.EqualTo("de-DE"));
            Assert.That(configuredMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(configuredMetadata.UsedLocaleFallback, Is.True);
            Assert.That(configuredMetadata.Validation.Valid, Is.True);
            Assert.That(
                Assert.Throws<FakeException>(() => strict.PassportIdentifier()).Message,
                Does.Contain("unsupported for locale 'pt-BR-x-fixture'"));
        });
    }

    [Test]
    public void StructuralMutationsHaveExactDeterministicResultsForEveryKind()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5807).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            string value = ValidVectors[index];
            string length = identifiers.MutateCivilIdentifier(
                value,
                Kinds[index],
                IdentifierMutationKind.Length);
            string character = identifiers.MutateCivilIdentifier(
                value,
                Kinds[index],
                IdentifierMutationKind.Character);
            string prefix = identifiers.MutateCivilIdentifier(
                value,
                Kinds[index],
                IdentifierMutationKind.Prefix);
            char expectedPrefix = Kinds[index] == IdentifierKind.Passport ||
                Kinds[index] == IdentifierKind.VehicleRegistration ? '0' :
                (Kinds[index] == IdentifierKind.BusinessRegistration ? 'a' : 'A');
            Assert.Multiple(() =>
            {
                Assert.That(length, Is.EqualTo(value.Substring(0, value.Length - 1)));
                Assert.That(character, Is.EqualTo(" " + value.Substring(1)));
                Assert.That(prefix, Is.EqualTo(expectedPrefix + value.Substring(1)));
                Assert.That(identifiers.IsCivilIdentifierValid(length, Kinds[index]), Is.False);
                Assert.That(identifiers.IsCivilIdentifierValid(character, Kinds[index]), Is.False);
                Assert.That(identifiers.IsCivilIdentifierValid(prefix, Kinds[index]), Is.False);
            });
            index++;
        }
    }

    [Test]
    public void ChecksumMutationChangesOnlyTheFinalDigitForEveryChecksumKind()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5808).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            if (HasChecksum(Kinds[index]))
            {
                string value = ValidVectors[index];
                string first = identifiers.MutateCivilIdentifier(
                    value,
                    Kinds[index],
                    IdentifierMutationKind.Checksum);
                string second = identifiers.MutateCivilIdentifier(
                    value,
                    Kinds[index],
                    IdentifierMutationKind.Checksum);
                Assert.Multiple(() =>
                {
                    Assert.That(first, Is.EqualTo(second));
                    Assert.That(
                        first.Substring(0, first.Length - 1),
                        Is.EqualTo(value.Substring(0, value.Length - 1)));
                    Assert.That(first[first.Length - 1], Is.Not.EqualTo(value[value.Length - 1]));
                    Assert.That(identifiers.IsCivilIdentifierValid(first, Kinds[index]), Is.False);
                });
            }
            index++;
        }
    }

    [Test]
    public void UnsupportedLocaleFailuresApplyToEveryCivilOperation()
    {
        IdentifierProvider identifiers = Fake.Create("en-US", 5809).Identifier();

        int index = 0;
        while (index < Kinds.Length)
        {
            IdentifierKind kind = Kinds[index];
            Assert.Multiple(() =>
            {
                Assert.That(
                    Assert.Throws<FakeException>(() =>
                        identifiers.CivilIdentifierFromKind(kind)).Message,
                    Does.Contain(kind.ToString()));
                Assert.That(
                    Assert.Throws<FakeException>(() =>
                        identifiers.IsCivilIdentifierValid(ValidVectors[index], kind)).Message,
                    Does.Contain("no authoritative rule"));
                Assert.That(
                    Assert.Throws<FakeException>(() =>
                        identifiers.CivilIdentifierMetadata(ValidVectors[index], kind)).Message,
                    Does.Contain("no authoritative rule"));
                Assert.That(
                    Assert.Throws<FakeException>(() =>
                        identifiers.MutateCivilIdentifier(
                            ValidVectors[index],
                            kind,
                            IdentifierMutationKind.Length)).Message,
                    Does.Contain("no authoritative rule"));
            });
            index++;
        }
    }

    static bool HasChecksum(IdentifierKind kind)
    {
        return kind == IdentifierKind.DrivingLicence ||
            kind == IdentifierKind.NationalIdentity ||
            kind == IdentifierKind.SocialIdentity ||
            kind == IdentifierKind.PersonalTax ||
            kind == IdentifierKind.BusinessRegistration;
    }
}
