using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageCodeServiceIntegrationTests
{
    private const string MaximumComponentTag =
        "abcdefgh-Abcd-123-1abc-abcdefgh-a-ab-b-abcdefgh-x-a-abcdefgh";

    [Test]
    public void CatalogMethodsIntegrateThroughInterfaceAndConcreteService()
    {
        LanguageCodeService service = new LanguageCodeService();
        ILanguageCatalog catalog = service;
        LanguageLookupOptions lookupOptions = new LanguageLookupOptions();
        LanguageLookupOptions searchOptions = new LanguageLookupOptions
        {
            SearchMode = LanguageSearchMode.Exact,
            ResultLimit = 10
        };

        LanguageCodeEntry[] interfaceAll = catalog.GetAllEntries();
        LanguageCodeEntry[] interfaceSet = catalog.GetEntriesBySet(Iso639Set.Set3);
        LanguageCodeEntry[] interfaceScope = catalog.GetEntriesByScope(LanguageScope.Individual);
        LanguageCodeEntry[] interfaceType = catalog.GetEntriesByType(LanguageType.Living);
        LanguageLookupResult interfaceLookupCode = catalog.LookupCode(
            "en",
            LanguageCodeForm.Alpha2,
            lookupOptions);
        LanguageCodeEntry interfaceRequiredCode = catalog.RequireCode(
            "eng",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageLookupResult interfaceLookupName = catalog.LookupName("English", lookupOptions);
        LanguageMatch[] interfaceSearch = catalog.SearchNames("English", searchOptions);
        LanguageLookupResult interfaceResolve = catalog.ResolveValue("en", lookupOptions);
        LanguageCodeEntry interfaceRequiredValue = catalog.RequireValue("English", lookupOptions);
        LanguageCodeEntry? interfaceMacro = catalog.GetMacrolanguageForMember("aao", false);
        LanguageCodeEntry[] interfaceMembers = catalog.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry[] interfaceParents = catalog.GetGroupParents("gmw", true);
        LanguageCodeEntry[] interfaceChildren = catalog.GetGroupChildren("ine", true);
        LanguageRetirement? interfaceRetirement = catalog.GetRetirement("agp");

        LanguageCodeEntry[] concreteAll = service.GetAllEntries();
        LanguageCodeEntry[] concreteSet = service.GetEntriesBySet(Iso639Set.Set1);
        LanguageCodeEntry[] concreteScope = service.GetEntriesByScope(LanguageScope.Macrolanguage);
        LanguageCodeEntry[] concreteType = service.GetEntriesByType(LanguageType.Constructed);
        LanguageLookupResult concreteLookupCode = service.LookupCode(
            "eng",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageCodeEntry concreteRequiredCode = service.RequireCode(
            "en",
            LanguageCodeForm.Alpha2,
            lookupOptions);
        LanguageLookupResult concreteLookupName = service.LookupName(null, lookupOptions);
        LanguageMatch[] concreteSearch = service.SearchNames(string.Empty, searchOptions);
        LanguageLookupResult concreteResolve = service.ResolveValue(null, lookupOptions);
        LanguageCodeEntry concreteRequiredValue = service.RequireValue("English", lookupOptions);
        LanguageCodeEntry? concreteMacro = service.GetMacrolanguageForMember(null, false);
        LanguageCodeEntry[] concreteMembers = service.GetMacrolanguageMembers(string.Empty, false);
        LanguageCodeEntry[] concreteParents = service.GetGroupParents(null, true);
        LanguageCodeEntry[] concreteChildren = service.GetGroupChildren(string.Empty, true);
        LanguageRetirement? concreteRetirement = service.GetRetirement(null);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceAll, Has.Length.EqualTo(LanguageDataInfo.ActiveEntityCount));
            Assert.That(interfaceSet, Has.Length.EqualTo(LanguageDataInfo.Set3RecordCount));
            Assert.That(interfaceScope, Is.Not.Empty);
            Assert.That(interfaceType, Is.Not.Empty);
            Assert.That(interfaceLookupCode.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceRequiredCode.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceLookupName.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceSearch.Select(match => match.Entry?.Set3Code), Does.Contain("eng"));
            Assert.That(interfaceResolve.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceRequiredValue.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceMacro?.Set3Code, Is.EqualTo("ara"));
            Assert.That(interfaceMembers.Select(entry => entry.Set3Code), Does.Contain("aao"));
            Assert.That(interfaceParents.Select(entry => entry.Set5Code), Does.Contain("ine"));
            Assert.That(interfaceChildren.Select(entry => entry.Set5Code), Does.Contain("gmw"));
            Assert.That(interfaceRetirement?.RetiredCode, Is.EqualTo("agp"));

            Assert.That(concreteAll, Has.Length.EqualTo(LanguageDataInfo.ActiveEntityCount));
            Assert.That(concreteSet, Has.Length.EqualTo(LanguageDataInfo.Set1RecordCount));
            Assert.That(concreteScope, Is.Not.Empty);
            Assert.That(concreteType, Is.Not.Empty);
            Assert.That(concreteLookupCode.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(concreteRequiredCode.Set3Code, Is.EqualTo("eng"));
            Assert.That(concreteLookupName.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(concreteSearch, Is.Empty);
            Assert.That(concreteResolve.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(concreteRequiredValue.Set3Code, Is.EqualTo("eng"));
            Assert.That(concreteMacro, Is.Null);
            Assert.That(concreteMembers, Is.Empty);
            Assert.That(concreteParents, Is.Empty);
            Assert.That(concreteChildren, Is.Empty);
            Assert.That(concreteRetirement, Is.Null);
        });
    }

    [Test]
    public void NormalizerMethodsIntegrateThroughInterfaceAndConcreteService()
    {
        LanguageCodeService service = new LanguageCodeService();
        ILanguageNormalizer normalizer = service;
        LanguageNormalizationOptions codeOptions = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2
        };
        LanguageNormalizationOptions tagOptions = new LanguageNormalizationOptions
        {
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true
        };

        LanguageNormalizationResult interfaceNormalized = normalizer.NormalizeCode("en", codeOptions);
        LanguageNormalizationResult interfaceConverted = normalizer.ConvertCode("eng", codeOptions);
        LanguageNormalizationResult interfaceTag = normalizer.NormalizeTag("EN_us", tagOptions);

        LanguageNormalizationResult concreteNormalized = service.NormalizeCode(null, codeOptions);
        LanguageNormalizationResult concreteConverted = service.ConvertCode(string.Empty, codeOptions);
        LanguageNormalizationResult concreteTag = service.NormalizeTag(string.Empty, tagOptions);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceNormalized.Status, Is.EqualTo(LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(interfaceNormalized.Output, Is.EqualTo("en"));
            Assert.That(interfaceConverted.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(interfaceConverted.Output, Is.EqualTo("en"));
            Assert.That(interfaceTag.Output, Is.EqualTo("en-US"));
            Assert.That(concreteNormalized.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(concreteConverted.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(concreteTag.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
        });
    }

    [Test]
    public void ValidatorMethodsIntegrateThroughInterfaceAndConcreteService()
    {
        LanguageCodeService service = new LanguageCodeService();
        ILanguageValidator validator = service;
        LanguageValidationOptions options = new LanguageValidationOptions();

        LanguageValidationResult interfaceCode = validator.ValidateCode("en", options);
        bool interfaceSpan = validator.ValidateCodeSpan("eng".AsSpan(), options);
        bool interfaceAssigned = validator.IsAssignedCode("en", LanguageCodeForm.Alpha2);
        LanguageValidationResult interfaceTag = validator.ValidateTag(MaximumComponentTag, options);

        LanguageValidationResult concreteCode = service.ValidateCode(null, options);
        bool concreteSpan = service.ValidateCodeSpan(ReadOnlySpan<char>.Empty, options);
        bool concreteAssigned = service.IsAssignedCode(string.Empty, LanguageCodeForm.Unknown);
        LanguageValidationResult concreteTag = service.ValidateTag(string.Empty, options);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceCode.IsValid, Is.True);
            Assert.That(interfaceCode.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(interfaceSpan, Is.True);
            Assert.That(interfaceAssigned, Is.True);
            Assert.That(interfaceTag.IsWellFormed, Is.True);
            Assert.That(concreteCode.IsValid, Is.False);
            Assert.That(concreteCode.Issues[0].Error, Is.EqualTo(LanguageValidationError.NullInput));
            Assert.That(concreteSpan, Is.False);
            Assert.That(concreteAssigned, Is.False);
            Assert.That(concreteTag.IsValid, Is.False);
            Assert.That(concreteTag.Issues[0].Error, Is.EqualTo(LanguageValidationError.EmptyInput));
        });
    }

    [Test]
    public void TagServiceMethodsIntegrateThroughInterfaceAndConcreteService()
    {
        LanguageCodeService service = new LanguageCodeService();
        ILanguageTagService tagService = service;

        LanguageTag interfaceParsed = tagService.ParseTag(MaximumComponentTag);
        LanguageLookupResult interfaceResolved = tagService.ResolveTagLanguage(interfaceParsed);
        LanguageSubtagMetadata? interfaceMetadata = tagService.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            "en");
        LanguageTagMatchResult[] interfaceFiltered = tagService.FilterTags(
            "en",
            new[] { "fr", "en", "en-US" });
        LanguageTagMatchResult interfaceBest = tagService.LookupBestTag(
            "en-US-x-private",
            new[] { "fr", "en", "en-US" });

        LanguageTag concreteParsed = service.ParseTag("en");
        LanguageLookupResult concreteResolved = service.ResolveTagLanguage(concreteParsed);
        LanguageSubtagMetadata? concreteMetadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            string.Empty);
        LanguageTagMatchResult[] concreteFiltered = service.FilterTags(
            null,
            new[] { "en" });
        LanguageTagMatchResult concreteBest = service.LookupBestTag(
            string.Empty,
            new[] { "en" });

        Assert.Multiple(() =>
        {
            Assert.That(interfaceParsed.Language, Is.EqualTo("abcdefgh"));
            Assert.That(interfaceParsed.Variants, Is.EqualTo(new[] { "1abc", "abcdefgh" }));
            Assert.That(interfaceResolved.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(interfaceMetadata?.Value, Is.EqualTo("en"));
            Assert.That(
                interfaceFiltered.Select(result => result.MatchedTag),
                Is.EqualTo(new[] { "en", "en-US" }));
            Assert.That(interfaceBest.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(concreteParsed.Language, Is.EqualTo("en"));
            Assert.That(concreteResolved.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(concreteMetadata, Is.Null);
            Assert.That(concreteFiltered, Has.Length.EqualTo(1));
            Assert.That(concreteFiltered[0].Status, Is.EqualTo(LanguageTagMatchStatus.InvalidRange));
            Assert.That(concreteBest.Status, Is.EqualTo(LanguageTagMatchStatus.InvalidRange));
        });
    }
}
