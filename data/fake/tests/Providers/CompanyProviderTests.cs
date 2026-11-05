using System.Globalization;
using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class CompanyProviderTests
{
    [Test]
    public void CompanyProfilesAreDeterministicNewCorrelatedInstances()
    {
        const long seed = 10015;
        CompanyProfile first = Fake.Create("en-US", seed).Company().Profile();
        CompanyProfile second = Fake.Create("en-US", seed).Company().Profile();
        string slug = Fake.Create("en-US", seed).Text().Slug(first.Name);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            AssertProfileEquivalent(first, second);
            Assert.That(first.Name,
                Is.EqualTo(Fake.Create("en-US", seed).Company().Name()));
            Assert.That(first.Domain, Is.EqualTo(slug + ".example.com"));
            Assert.That(first.Email, Is.EqualTo("contact@" + first.Domain));
            Assert.That(first.Locale, Is.EqualTo(first.Identifiers.Locale));
            Assert.That(first.CountryCode, Is.EqualTo(first.Identifiers.CountryCode));
            Assert.That(first.PhoneRegionCode,
                Is.EqualTo(first.Phone.ParsingMetadata.RegionCode));
            Assert.That(first.AddressFixtureCountryCode,
                Is.EqualTo(first.Address.CountryCode));
            Assert.That(first.Address.CountryCode,
                Is.EqualTo(first.Address.Geography.CountryCode));
            Assert.That(first.Address.SubdivisionCode,
                Is.EqualTo(first.Address.Geography.SubdivisionCode));
            Assert.That(first.Address.PostalCode,
                Is.EqualTo(first.Address.Geography.PostalCode));
        });
    }

    [Test]
    public void CompanyProfilesPreserveNestedSafetyContracts()
    {
        CompanyProfile profile = Fake.Create("en-US", 10016).Company().Profile();

        Assert.Multiple(() =>
        {
            Assert.That(profile.Domain, Does.EndWith(".example.com"));
            Assert.That(profile.DomainIsReservedExample, Is.True);
            Assert.That(profile.EmailIsReservedExample, Is.True);
            Assert.That(profile.Phone.SafetyMetadata.UsesAuthoritativeSafeRange, Is.True);
            Assert.That(profile.Address.NonDeliverabilityGuaranteed, Is.True);
            Assert.That(profile.Address.NonexistenceGuaranteed, Is.True);
            Assert.That(profile.Identifiers.IsBusinessIdentifierValid, Is.True);
            Assert.That(profile.Identifiers.IsTaxIdentifierValid, Is.True);
            Assert.That(profile.IsSynthetic, Is.True);
            Assert.That(profile.TestMarker, Is.EqualTo("fake.synthetic-company.v1"));
            Assert.That(profile.RepresentsRegisteredEntity, Is.False);
            Assert.That(profile.RepresentsReachableContact, Is.False);
            Assert.That(profile.RepresentsDeliverableAddress, Is.False);
        });
    }

    [Test]
    public void CompanyProfileUsesSafeAsciiFallbackForNonLatinNames()
    {
        CompanyProfile profile = Fake.Create("ja-JP", 10017).Company().Profile();

        Assert.Multiple(() =>
        {
            Assert.That(profile.Name,
                Does.Match("^(株式会社|合同会社|合名会社|合資会社)"));
            Assert.That(profile.Domain,
                Does.Match("^company-[0-9a-f]{16}\\.example\\.com$"));
            Assert.That(profile.Email, Is.EqualTo("contact@" + profile.Domain));
            Assert.That(profile.CountryCode, Is.EqualTo("JP"));
            Assert.That(profile.Identifiers.CountryCode, Is.EqualTo("JP"));
        });
    }

    [Test]
    public void CompanyProfilesKeepReservedDomainsAcrossSafetyModesAndHonorLimits()
    {
        FakeOptions plausibleOptions = FakeOptions.Create("en-US", 10018);
        plausibleOptions.SafetyMode = SafetyMode.SyntheticPlausible;
        CompanyProfile plausible = Fake.Create(plausibleOptions).Company().Profile();
        FakeOptions outputOptions = FakeOptions.Create("en-US", 10018);
        outputOptions.MaximumOutputSize = 100;

        Assert.Multiple(() =>
        {
            Assert.That(plausible.Domain, Does.EndWith(".example.com"));
            Assert.That(plausible.DomainIsReservedExample, Is.True);
            Assert.That(plausible.Phone.SafetyMetadata.AppliedMode,
                Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(plausible.Address.NonDeliverabilityGuaranteed, Is.False);
            Assert.That(() => Fake.Create(outputOptions).Company().Profile(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void CompanyProfileGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(10019);
        Fake untouched = Fake.Create(10019);

        int index = 0;
        while (index < 16)
        {
            inspected.Company().Profile();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }

    [Test]
    public void CatchAndBuzzPhrasesAreDeterministicComponentCompositions()
    {
        const long seed = 10011;
        CompanyProvider catchComponents = Fake.Create(seed).Company();
        string expectedCatch = catchComponents.CatchDescriptor() + " " +
            catchComponents.CatchSubject() + " for " +
            catchComponents.CatchBenefit();
        CompanyProvider buzzComponents = Fake.Create(seed).Company();
        string expectedBuzz = buzzComponents.BuzzVerb() + " " +
            buzzComponents.BuzzAdjective() + " " +
            buzzComponents.BuzzNoun();
        string firstCatch = Fake.Create(seed).Company().CatchPhrase();
        string secondCatch = Fake.Create(seed).Company().CatchPhrase();
        string firstBuzz = Fake.Create(seed).Company().BuzzPhrase();
        string secondBuzz = Fake.Create(seed).Company().BuzzPhrase();

        Assert.Multiple(() =>
        {
            Assert.That(firstCatch, Is.EqualTo(expectedCatch));
            Assert.That(firstBuzz, Is.EqualTo(expectedBuzz));
            Assert.That(firstCatch, Is.EqualTo(secondCatch));
            Assert.That(firstBuzz, Is.EqualTo(secondBuzz));
        });
    }

    [Test]
    public void PhraseComponentsComeFromTheAuthoredVocabularies()
    {
        CompanyProvider company = Fake.Create(10012).Company();

        int index = 0;
        while (index < 128)
        {
            Assert.Multiple(() =>
            {
                Assert.That(company.CatchDescriptor(), Is.AnyOf(
                    "Adaptable", "Clear", "Connected", "Practical", "Reliable", "Thoughtful"));
                Assert.That(company.CatchSubject(), Is.AnyOf(
                    "planning", "services", "support", "systems", "tools", "workflows"));
                Assert.That(company.CatchBenefit(), Is.AnyOf(
                    "clearer decisions", "everyday teams", "long-term work",
                    "shared projects", "simpler operations", "steady growth"));
                Assert.That(company.BuzzVerb(), Is.AnyOf(
                    "coordinate", "improve", "organize", "refine", "simplify", "support"));
                Assert.That(company.BuzzAdjective(), Is.AnyOf(
                    "adaptable", "clear", "measurable", "practical", "reliable", "shared"));
                Assert.That(company.BuzzNoun(), Is.AnyOf(
                    "outcomes", "processes", "services", "systems", "teams", "workflows"));
            });
            index++;
        }
    }

    [Test]
    public void PhraseCompositionProducesBroadVariationWithoutFixedSentenceLists()
    {
        CompanyProvider company = Fake.Create(10013).Company();
        HashSet<string> catches = new HashSet<string>();
        HashSet<string> buzzes = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            catches.Add(company.CatchPhrase());
            buzzes.Add(company.BuzzPhrase());
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(catches.Count, Is.GreaterThan(80));
            Assert.That(buzzes.Count, Is.GreaterThan(80));
            Assert.That(Fake.Create("ja-JP", 10013).Company().CatchPhrase(),
                Is.EqualTo(Fake.Create("en-US", 10013).Company().CatchPhrase()));
        });
    }

    [Test]
    public void PhraseApisHonorLimitsBeforeConsumingEntropy()
    {
        const long seed = 10014;
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 20;
        CompanyProvider inspected = Fake.Create(outputOptions).Company();
        CompanyProvider untouched = Fake.Create(outputOptions).Company();

        Assert.That(() => Fake.Create(countOptions).Company().CatchPhrase(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(countOptions).Company().BuzzPhrase(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => inspected.CatchPhrase(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        Assert.That(() => Fake.Create(outputOptions).Company().BuzzPhrase(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void CompanyNamesAreDeterministicLocalizedFictionalCompositions()
    {
        const long seed = 9990;
        string american = Fake.Create("en-US", seed).Company().Name();
        string british = Fake.Create("en-GB", seed).Company().Name();
        string german = Fake.Create("de-DE", seed).Company().Name();
        string french = Fake.Create("fr-FR", seed).Company().Name();
        string japanese = Fake.Create("ja-JP", seed).Company().Name();

        Assert.Multiple(() =>
        {
            Assert.That(american, Is.EqualTo(Fake.Create("en-US", seed).Company().Name()));
            Assert.That(british, Is.EqualTo(Fake.Create("en-GB", seed).Company().Name()));
            Assert.That(german, Is.EqualTo(Fake.Create("de-DE", seed).Company().Name()));
            Assert.That(french, Is.EqualTo(Fake.Create("fr-FR", seed).Company().Name()));
            Assert.That(japanese, Is.EqualTo(Fake.Create("ja-JP", seed).Company().Name()));
            Assert.That(american, Does.Match(" (LLC|Corp\\.|Inc\\.|LP)$"));
            Assert.That(british, Does.Match(" (Ltd|plc|LLP)$"));
            Assert.That(german,
                Does.Match(" (GmbH|AG|UG \\(haftungsbeschränkt\\))$"));
            Assert.That(french, Does.Match(" (SARL|SAS|SA|SNC)$"));
            Assert.That(japanese,
                Does.Match("^(株式会社|合同会社|合名会社|合資会社)"));
        });
    }

    [Test]
    public void LegalFormsIndustriesAndDepartmentsUseLocalizedVocabulary()
    {
        const long seed = 9991;
        CompanyProvider english = Fake.Create("en-US", seed).Company();
        CompanyProvider german = Fake.Create("de-DE", seed).Company();
        CompanyProvider french = Fake.Create("fr-FR", seed).Company();
        CompanyProvider japanese = Fake.Create("ja-JP", seed).Company();

        Assert.Multiple(() =>
        {
            Assert.That(english.LegalForm(), Is.AnyOf(
                "Limited Liability Company", "Corporation", "Incorporated", "Limited Partnership"));
            Assert.That(german.Suffix(), Is.AnyOf("GmbH", "AG", "UG (haftungsbeschränkt)"));
            Assert.That(german.Department(), Is.AnyOf(
                "Finanzen", "Forschung", "Kundenservice", "Personal", "Technik", "Vertrieb"));
            Assert.That(french.Industry(), Is.AnyOf(
                "conseil aux entreprises", "logistique", "recherche", "services éducatifs",
                "services logiciels", "technologies environnementales"));
            Assert.That(japanese.Department(), Is.AnyOf(
                "営業", "技術", "研究", "財務", "人事", "顧客サポート"));
        });
    }

    [Test]
    public void CountryProfilesGenerateAndValidateIdentifierShapes()
    {
        AssertIdentifiers("en-US", 9992, "^[0-9]{2}-[0-9]{7}$", "^[0-9]{2}-[0-9]{7}$");
        AssertIdentifiers("en-GB", 9993, "^[0-9]{8}$", "^GB[0-9]{9}$");
        AssertIdentifiers("de-DE", 9994, "^HRB [0-9]{6}$", "^DE[0-9]{9}$");
        AssertIdentifiers("fr-FR", 9995, "^[0-9]{9}$", "^FR[0-9]{11}$");
        AssertIdentifiers("en-CA", 9996, "^[0-9]{9}RC0001$", "^[0-9]{9}RT0001$");
        AssertIdentifiers("en-AU", 9997, "^[0-9]{11}$", "^[0-9]{11}$");
        AssertIdentifiers("ja-JP", 9998, "^[0-9]{13}$", "^T[0-9]{13}$");
        AssertIdentifiers("es-MX", 9999, "^MX-[0-9]{10}$", "^MX[0-9]{10}$");
        AssertIdentifiers("es", 10010, "^ES-[0-9]{10}$", "^ES[0-9]{10}$");
    }

    [Test]
    public void IdentifierRecordsCorrelateCountrySpecificRootsAndSafetyMetadata()
    {
        CompanyIdentifierRecord american = Fake.Create("en-US", 10000).Company().Identifiers();
        CompanyIdentifierRecord british = Fake.Create("en-GB", 10001).Company().Identifiers();
        CompanyIdentifierRecord french = Fake.Create("fr-FR", 10002).Company().Identifiers();
        CompanyIdentifierRecord canadian = Fake.Create("fr-CA", 10003).Company().Identifiers();
        CompanyIdentifierRecord australian = Fake.Create("en-AU", 10004).Company().Identifiers();
        CompanyIdentifierRecord japanese = Fake.Create("ja-JP", 10005).Company().Identifiers();

        Assert.Multiple(() =>
        {
            Assert.That(american.TaxIdentifier, Is.EqualTo(american.BusinessIdentifier));
            Assert.That(british.TaxIdentifier.Substring(2, 8),
                Is.EqualTo(british.BusinessIdentifier));
            Assert.That(french.TaxIdentifier, Does.EndWith(french.BusinessIdentifier));
            Assert.That(canadian.TaxIdentifier.Substring(0, 9),
                Is.EqualTo(canadian.BusinessIdentifier.Substring(0, 9)));
            Assert.That(australian.TaxIdentifier, Is.EqualTo(australian.BusinessIdentifier));
            Assert.That(japanese.TaxIdentifier, Is.EqualTo("T" + japanese.BusinessIdentifier));
            Assert.That(french.IsBusinessIdentifierValid, Is.True);
            Assert.That(french.IsTaxIdentifierValid, Is.True);
            Assert.That(french.IsSynthetic, Is.True);
            Assert.That(french.TestMarker, Is.EqualTo("fake.synthetic-company.v1"));
            Assert.That(french.RepresentsRegisteredEntity, Is.False);
            Assert.That(french.RepresentsTaxRegistration, Is.False);
        });
    }

    [Test]
    public void FrenchChecksumsAndAllStructuralValidatorsRejectMutations()
    {
        CompanyProvider french = Fake.Create("fr-FR", 10006).Company();
        string business = french.BusinessId();
        string tax = french.TaxId();
        char[] businessMutation = business.ToCharArray();
        businessMutation[8] = businessMutation[8] == '9'
            ? '0'
            : (char)(businessMutation[8] + 1);
        char[] taxMutation = tax.ToCharArray();
        taxMutation[3] = taxMutation[3] == '9'
            ? '0'
            : (char)(taxMutation[3] + 1);
        CompanyProvider american = Fake.Create("en-US", 10007).Company();

        Assert.Multiple(() =>
        {
            Assert.That(french.IsBusinessIdValid(business), Is.True);
            Assert.That(french.IsBusinessIdValid(new string(businessMutation)), Is.False);
            Assert.That(french.IsTaxIdValid(tax), Is.True);
            Assert.That(french.IsTaxIdValid(new string(taxMutation)), Is.False);
            Assert.That(american.IsBusinessIdValid("12-3456789"), Is.True);
            Assert.That(american.IsBusinessIdValid("12 3456789"), Is.False);
            Assert.That(american.IsTaxIdValid("12-345678A"), Is.False);
        });
    }

    [Test]
    public void LanguageFallbackAndNormalizationPreserveLocalizedText()
    {
        FakeOptions options = FakeOptions.Create("fr-BE", 10008);
        options.NormalizationMode = NormalizationMode.Nfd;
        CompanyProvider company = Fake.Create(options).Company();
        string legalForm = company.LegalForm();
        string business = Fake.Create(options).Company().BusinessId();

        Assert.Multiple(() =>
        {
            Assert.That(legalForm.IsNormalized(NormalizationForm.FormD), Is.True);
            Assert.That(legalForm, Does.Contain("e\u0301"));
            Assert.That(business, Does.Match("^BE-[0-9]{10}$"));
            Assert.That(company.IsBusinessIdValid(business), Is.True);
        });
    }

    [Test]
    public void CompanyApisHonorLimitsAndRejectNullWithoutConsumingEntropy()
    {
        const long seed = 10009;
        CompanyProvider inspected = Fake.Create(seed).Company();
        CompanyProvider untouched = Fake.Create(seed).Company();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 8;
        FakeOptions recordOptions = FakeOptions.Create(seed);
        recordOptions.MaximumOutputSize = 50;

        Assert.That(() => inspected.IsBusinessIdValid(null),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => inspected.IsTaxIdValid(null),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(inspected.BusinessId(), Is.EqualTo(untouched.BusinessId()));
        Assert.That(Fake.Create(seed).Company().BusinessIdentifier(),
            Is.EqualTo(Fake.Create(seed).Company().BusinessId()));
        Assert.That(Fake.Create(seed).Company().TaxIdentifier(),
            Is.EqualTo(Fake.Create(seed).Company().TaxId()));
        Assert.That(() => Fake.Create(countOptions).Company().Name(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Company().BusinessId(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Company().Name(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(recordOptions).Company().Identifiers(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static void AssertIdentifiers(
        string locale,
        long seed,
        string businessPattern,
        string taxPattern)
    {
        string business = Fake.Create(locale, seed).Company().BusinessId();
        string tax = Fake.Create(locale, seed).Company().TaxId();
        CompanyProvider validator = Fake.Create(locale, seed).Company();

        Assert.Multiple(() =>
        {
            Assert.That(business,
                Is.EqualTo(Fake.Create(locale, seed).Company().BusinessId()));
            Assert.That(tax, Is.EqualTo(Fake.Create(locale, seed).Company().TaxId()));
            Assert.That(business, Does.Match(businessPattern));
            Assert.That(tax, Does.Match(taxPattern));
            Assert.That(validator.IsBusinessIdValid(business), Is.True);
            Assert.That(validator.IsTaxIdValid(tax), Is.True);
        });
    }

    static void AssertProfileEquivalent(CompanyProfile actual, CompanyProfile expected)
    {
        Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
        Assert.That(actual.CountryCode, Is.EqualTo(expected.CountryCode));
        Assert.That(actual.Name, Is.EqualTo(expected.Name));
        Assert.That(actual.Domain, Is.EqualTo(expected.Domain));
        Assert.That(actual.Email, Is.EqualTo(expected.Email));
        Assert.That(actual.Industry, Is.EqualTo(expected.Industry));
        Assert.That(actual.CatchPhrase, Is.EqualTo(expected.CatchPhrase));
        Assert.That(actual.Phone.Number, Is.EqualTo(expected.Phone.Number));
        Assert.That(actual.Address.StreetAddress, Is.EqualTo(expected.Address.StreetAddress));
        Assert.That(actual.Address.Unit, Is.EqualTo(expected.Address.Unit));
        Assert.That(actual.Address.City, Is.EqualTo(expected.Address.City));
        Assert.That(actual.Address.PostalCode, Is.EqualTo(expected.Address.PostalCode));
        Assert.That(actual.Identifiers.BusinessIdentifier,
            Is.EqualTo(expected.Identifiers.BusinessIdentifier));
        Assert.That(actual.Identifiers.TaxIdentifier,
            Is.EqualTo(expected.Identifiers.TaxIdentifier));
    }
}
