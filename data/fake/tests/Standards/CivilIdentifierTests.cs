using fake;

namespace fake.Tests;

[TestFixture]
public class CivilIdentifierTests
{
    static IdentifierKind[] CivilKinds = new IdentifierKind[]
    {
        IdentifierKind.Passport,
        IdentifierKind.DrivingLicence,
        IdentifierKind.NationalIdentity,
        IdentifierKind.SocialIdentity,
        IdentifierKind.PersonalTax,
        IdentifierKind.BusinessRegistration,
        IdentifierKind.VehicleRegistration
    };

    [Test]
    public void BrazilianConvenienceOperationsGenerateValidCivilIdentifiers()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5701).Identifier();
        string[] values = new string[]
        {
            identifiers.PassportIdentifier(),
            identifiers.DrivingLicenceIdentifier(),
            identifiers.NationalIdentityIdentifier(),
            identifiers.SocialIdentityIdentifier(),
            identifiers.PersonalTaxIdentifier(),
            identifiers.BusinessRegistrationIdentifier(),
            identifiers.VehicleRegistrationIdentifier()
        };

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Does.Match("^[A-Z]{2}[0-9]{6}$"));
            Assert.That(values[1], Does.Match("^[0-9]{11}$"));
            Assert.That(values[2], Does.Match("^[0-9]{11}$"));
            Assert.That(values[3], Does.Match("^[0-9]{11}$"));
            Assert.That(values[4], Does.Match("^[0-9]{11}$"));
            Assert.That(values[5], Does.Match("^[A-Z0-9]{12}[0-9]{2}$"));
            Assert.That(values[6], Does.Match("^[A-Z]{3}[0-9][A-Z][0-9]{2}$"));
            int index = 0;
            while (index < CivilKinds.Length)
            {
                Assert.That(
                    identifiers.IsCivilIdentifierValid(values[index], CivilKinds[index]),
                    Is.True,
                    CivilKinds[index].ToString());
                Assert.That(
                    identifiers.ValidateIdentifier(values[index], CivilKinds[index]).Valid,
                    Is.True,
                    CivilKinds[index].ToString());
                index++;
            }
        });
    }

    [Test]
    public void SeededCivilGenerationIsReproducible()
    {
        IdentifierProvider first = Fake.Create("pt-BR", 5702).Identifier();
        IdentifierProvider second = Fake.Create("pt-BR", 5702).Identifier();

        int index = 0;
        while (index < CivilKinds.Length)
        {
            Assert.That(
                first.CivilIdentifierFromKind(CivilKinds[index]),
                Is.EqualTo(second.CivilIdentifierFromKind(CivilKinds[index])),
                CivilKinds[index].ToString());
            index++;
        }
    }

    [Test]
    public void MetadataReportsRuleValidityFallbackAndNoAssignmentClaim()
    {
        IdentifierProvider direct = Fake.Create("pt-BR", 5703).Identifier();
        string directValue = direct.PersonalTaxIdentifier();
        GenerationMetadata directMetadata = direct.CivilIdentifierMetadata(
            directValue,
            IdentifierKind.PersonalTax);

        FakeOptions fallbackOptions = FakeOptions.Create("en-US", "pt-BR", 5703);
        IdentifierProvider fallback = Fake.Create(fallbackOptions).Identifier();
        string fallbackValue = fallback.BusinessRegistrationIdentifier();
        GenerationMetadata fallbackMetadata = fallback.IdentifierMetadata(
            fallbackValue,
            IdentifierKind.BusinessRegistration);

        Assert.Multiple(() =>
        {
            Assert.That(directMetadata.Locale, Is.EqualTo("pt-BR"));
            Assert.That(directMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(directMetadata.UsedLocaleFallback, Is.False);
            Assert.That(directMetadata.Rule, Is.EqualTo("br-cpf-v1"));
            Assert.That(directMetadata.Format, Does.Contain("CPF"));
            Assert.That(directMetadata.Provenance, Does.Contain("reviewed 2026-08-25"));
            Assert.That(directMetadata.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(directMetadata.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(directMetadata.AllocationStatus, Is.EqualTo(AllocationStatus.Unknown));
            Assert.That(directMetadata.FictionalReserved, Is.False);
            Assert.That(directMetadata.RepresentsAssignment, Is.False);
            Assert.That(fallbackMetadata.Locale, Is.EqualTo("en-US"));
            Assert.That(fallbackMetadata.SourceLocale, Is.EqualTo("pt-BR"));
            Assert.That(fallbackMetadata.UsedLocaleFallback, Is.True);
            Assert.That(fallbackMetadata.Rule, Is.EqualTo("br-cnpj-alphanumeric-2026-v1"));
            Assert.That(fallbackMetadata.Validation.Valid, Is.True);
        });
    }

    [Test]
    public void ChecksumMutationsAreDeterministicAndInvalid()
    {
        IdentifierKind[] checksumKinds = new IdentifierKind[]
        {
            IdentifierKind.DrivingLicence,
            IdentifierKind.NationalIdentity,
            IdentifierKind.SocialIdentity,
            IdentifierKind.PersonalTax,
            IdentifierKind.BusinessRegistration
        };
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5704).Identifier();

        int index = 0;
        while (index < checksumKinds.Length)
        {
            IdentifierKind kind = checksumKinds[index];
            string value = identifiers.CivilIdentifierFromKind(kind);
            string first = identifiers.MutateCivilIdentifier(
                value,
                kind,
                IdentifierMutationKind.Checksum);
            string second = identifiers.MutateCivilIdentifier(
                value,
                kind,
                IdentifierMutationKind.Checksum);
            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.Length, Is.EqualTo(value.Length));
                Assert.That(identifiers.IsCivilIdentifierValid(first, kind), Is.False);
                Assert.That(
                    identifiers.ValidateIdentifier(first, kind).ChecksumValidity,
                    Is.EqualTo(ValidityMode.Invalid));
            });
            index++;
        }
    }

    [Test]
    public void StructuralMutationsInvalidateEveryCivilKind()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5705).Identifier();

        int index = 0;
        while (index < CivilKinds.Length)
        {
            IdentifierKind kind = CivilKinds[index];
            string value = identifiers.CivilIdentifierFromKind(kind);
            string length = identifiers.MutateCivilIdentifier(
                value,
                kind,
                IdentifierMutationKind.Length);
            string character = identifiers.MutateCivilIdentifier(
                value,
                kind,
                IdentifierMutationKind.Character);
            string prefix = identifiers.MutateCivilIdentifier(
                value,
                kind,
                IdentifierMutationKind.Prefix);
            Assert.Multiple(() =>
            {
                Assert.That(identifiers.IsCivilIdentifierValid(length, kind), Is.False);
                Assert.That(identifiers.IsCivilIdentifierValid(character, kind), Is.False);
                Assert.That(identifiers.IsCivilIdentifierValid(prefix, kind), Is.False);
            });
            index++;
        }
    }

    [Test]
    public void KnownCpfCnpjAndCnhVectorsAreAccepted()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5706).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(
                identifiers.IsCivilIdentifierValid("52998224725", IdentifierKind.PersonalTax),
                Is.True);
            Assert.That(
                identifiers.IsCivilIdentifierValid("11222333000181", IdentifierKind.BusinessRegistration),
                Is.True);
            Assert.That(
                identifiers.IsCivilIdentifierValid("12ABC34501DE35", IdentifierKind.BusinessRegistration),
                Is.True);
            Assert.That(
                identifiers.IsCivilIdentifierValid("02650306461", IdentifierKind.DrivingLicence),
                Is.True);
            Assert.That(
                identifiers.IsCivilIdentifierValid("52998224724", IdentifierKind.PersonalTax),
                Is.False);
            Assert.That(
                identifiers.ValidateIdentifier("00000000000", IdentifierKind.NationalIdentity).StructuralValidity,
                Is.EqualTo(ValidityMode.Invalid));
        });
    }

    [Test]
    public void UnsupportedLocalesAndStrictFallbackFailExplicitly()
    {
        IdentifierProvider unsupported = Fake.Create("en-US", 5707).Identifier();
        FakeOptions strictOptions = FakeOptions.Create("en-US", "pt-BR", 5707);
        strictOptions.StrictnessMode = StrictnessMode.Strict;
        IdentifierProvider strict = Fake.Create(strictOptions).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<FakeException>(() => unsupported.PassportIdentifier()).Message,
                Does.Contain("unsupported for locale 'en-US'"));
            Assert.That(
                Assert.Throws<FakeException>(() => strict.PersonalTaxIdentifier()).Message,
                Does.Contain("unsupported for locale 'en-US'"));
        });
    }

    [Test]
    public void InvalidArgumentsAndUnsupportedMutationsAreRejected()
    {
        IdentifierProvider identifiers = Fake.Create("pt-BR", 5708).Identifier();
        string passport = identifiers.PassportIdentifier();
        string tax = identifiers.PersonalTaxIdentifier();

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() =>
                    identifiers.IsCivilIdentifierValid((string)null, IdentifierKind.PersonalTax)).ParamName,
                Is.EqualTo("value"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    identifiers.CivilIdentifierFromKind(IdentifierKind.Uuid4)).ParamName,
                Is.EqualTo("kind"));
            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    identifiers.MutateCivilIdentifier(
                        passport,
                        IdentifierKind.Passport,
                        IdentifierMutationKind.Checksum)).ParamName,
                Is.EqualTo("mutation"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    identifiers.MutateCivilIdentifier(
                        tax,
                        IdentifierKind.PersonalTax,
                        IdentifierMutationKind.Version)).ParamName,
                Is.EqualTo("mutation"));
            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    identifiers.MutateCivilIdentifier(
                        "52998224724",
                        IdentifierKind.PersonalTax,
                        IdentifierMutationKind.Length)).ParamName,
                Is.EqualTo("value"));
        });
    }
}
