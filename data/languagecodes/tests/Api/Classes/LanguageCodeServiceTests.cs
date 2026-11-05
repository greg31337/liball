using languagecodes;

namespace languagecodes.Tests;

public class LanguageCodeServiceTests
{
    [Test]
    public void PublicResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions lookupOptions = new LanguageLookupOptions();
        LanguageNormalizationOptions normalizationOptions =
            new LanguageNormalizationOptions
            {
                TargetForm = LanguageCodeForm.Alpha2
            };
        LanguageValidationOptions validationOptions = new LanguageValidationOptions();

        LanguageCodeEntry firstEntry = service.GetAllEntries().Single(
            entry => entry.Set3Code == "eng");
        LanguageCodeEntry secondEntry = service.GetAllEntries().Single(
            entry => entry.Set3Code == "eng");
        LanguageLookupResult firstLookup = service.LookupCode(
            "eng",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageLookupResult secondLookup = service.LookupCode(
            "eng",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageNormalizationResult firstNormalization = service.ConvertCode(
            "eng",
            normalizationOptions);
        LanguageNormalizationResult secondNormalization = service.ConvertCode(
            "eng",
            normalizationOptions);
        LanguageValidationResult firstValidation = service.ValidateCode(
            "eng",
            validationOptions);
        LanguageValidationResult secondValidation = service.ValidateCode(
            "eng",
            validationOptions);
        LanguageTag firstTag = service.ParseTag("en-US");
        LanguageTag secondTag = service.ParseTag("en-US");
        LanguageSubtagMetadata firstMetadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            "en")!;
        LanguageSubtagMetadata secondMetadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            "en")!;
        LanguageTagMatchResult firstMatch = service.LookupBestTag(
            "en-US",
            new[] { "en-US" });
        LanguageTagMatchResult secondMatch = service.LookupBestTag(
            "en-US",
            new[] { "en-US" });
        LanguageRetirement firstRetirement = service.GetRetirement("agp")!;
        LanguageRetirement secondRetirement = service.GetRetirement("agp")!;

        firstEntry.Names[0].Text = "changed";
        firstLookup.Entry!.Set3Code = "changed";
        firstLookup.Candidates[0].Entry!.Set3Code = "changed";
        firstNormalization.Entry!.Set3Code = "changed";
        firstValidation.Entry!.Set3Code = "changed";
        firstTag.Language = "changed";
        firstMetadata.Descriptions[0] = "changed";
        firstMatch.ParsedTag!.Language = "changed";
        firstRetirement.ReplacementCodes[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(secondEntry.Names[0].Text, Is.Not.EqualTo("changed"));
            Assert.That(secondLookup.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(secondLookup.Candidates[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(secondNormalization.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(secondValidation.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(secondTag.Language, Is.EqualTo("en"));
            Assert.That(secondMetadata.Descriptions[0], Is.EqualTo("English"));
            Assert.That(secondMatch.ParsedTag?.Language, Is.EqualTo("en"));
            Assert.That(secondRetirement.ReplacementCodes,
                Is.EqualTo(new[] { "apf", "prf" }));
        });
    }

    [Test]
    public void PublicCollectionsUseDeterministicOrdering()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageCodeEntry[] entries = service.GetAllEntries();
        LanguageCodeEntry[] members = service.GetMacrolanguageMembers("ara", true);
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            SearchMode = LanguageSearchMode.Contains,
            ResultLimit = 20,
            NormalizeCase = true,
            NormalizeNames = true
        };
        LanguageMatch[] firstSearch = service.SearchNames("Arabic", options);
        LanguageMatch[] secondSearch = service.SearchNames("Arabic", options);
        LanguageTagMatchResult[] filtered = service.FilterTags(
            "en",
            new[] { "en-GB", "fr", "en", "en-US" });

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(StableCode), Is.EqualTo(
                entries.Select(StableCode).OrderBy(
                    code => code,
                    StringComparer.Ordinal)));
            Assert.That(members.Select(StableCode), Is.EqualTo(
                members.Select(StableCode).OrderBy(
                    code => code,
                    StringComparer.Ordinal)));
            Assert.That(firstSearch.Select(StableMatch),
                Is.EqualTo(secondSearch.Select(StableMatch)));
            Assert.That(firstSearch.Select(match => match.Rank),
                Is.EqualTo(Enumerable.Range(0, firstSearch.Length)));
            Assert.That(filtered.Select(result => result.MatchedIndex),
                Is.EqualTo(new[] { 0, 2, 3 }));
        });
    }

    [Test]
    public void NullableInputsReturnTypedOutcomes()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions lookupOptions = new LanguageLookupOptions();
        LanguageNormalizationOptions normalizationOptions =
            new LanguageNormalizationOptions();
        LanguageValidationOptions validationOptions = new LanguageValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(service.LookupCode(
                null,
                LanguageCodeForm.Unknown,
                lookupOptions).Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(service.LookupName(null, lookupOptions).Status,
                Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(service.SearchNames(null, lookupOptions), Is.Empty);
            Assert.That(service.ResolveValue(null, lookupOptions).Status,
                Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(service.GetMacrolanguageForMember(null, true), Is.Null);
            Assert.That(service.GetMacrolanguageMembers(null, true), Is.Empty);
            Assert.That(service.GetGroupParents(null, true), Is.Empty);
            Assert.That(service.GetGroupChildren(null, true), Is.Empty);
            Assert.That(service.GetRetirement(null), Is.Null);
            Assert.That(service.NormalizeCode(null, normalizationOptions).Status,
                Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(service.ConvertCode(null, normalizationOptions).Status,
                Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(service.NormalizeTag(null, normalizationOptions).Status,
                Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(service.ValidateCode(null, validationOptions).IsValid, Is.False);
            Assert.That(service.IsAssignedCode(null, LanguageCodeForm.Unknown), Is.False);
            Assert.That(service.ValidateTag(null, validationOptions).IsValid, Is.False);
            Assert.That(service.ResolveTagLanguage(null!).Status,
                Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(service.GetLanguageSubtagMetadata(
                LanguageSubtagType.Language,
                null), Is.Null);
            Assert.That(service.FilterTags(null, new[] { "en" })[0].Status,
                Is.EqualTo(LanguageTagMatchStatus.InvalidRange));
            Assert.That(service.FilterTags("en", null!)[0].Status,
                Is.EqualTo(LanguageTagMatchStatus.InvalidTag));
            Assert.That(service.LookupBestTag(null, new[] { "en" }).Status,
                Is.EqualTo(LanguageTagMatchStatus.InvalidRange));
            Assert.That(service.LookupBestTag("en", null!).Status,
                Is.EqualTo(LanguageTagMatchStatus.InvalidTag));
        });
    }

    [Test]
    public void PublicFailuresUseTheDocumentedDomainExceptions()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidLanguageCodeException>(() => service.RequireCode(
                "e1g",
                LanguageCodeForm.Set3,
                options));
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireCode(
                "zzz",
                LanguageCodeForm.Set3,
                options));
            Assert.Throws<InvalidLanguageCodeException>(() =>
                service.RequireValue("e1g", options));
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireValue(
                "Not A Registered Language",
                options));
            Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag("en--US"));
            Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag(null));
        });
    }

    [Test]
    public void CatalogContractExposesEveryCatalogOperation()
    {
        ILanguageCatalog service = new LanguageCodeService();
        LanguageLookupOptions lookupOptions = new LanguageLookupOptions
        {
            NormalizeCase = true
        };
        LanguageLookupOptions searchOptions = new LanguageLookupOptions
        {
            SearchMode = LanguageSearchMode.Exact,
            ResultLimit = 5
        };

        LanguageCodeEntry[] all = service.GetAllEntries();
        LanguageCodeEntry[] set1 = service.GetEntriesBySet(Iso639Set.Set1);
        LanguageCodeEntry[] individuals = service.GetEntriesByScope(
            LanguageScope.Individual);
        LanguageCodeEntry[] living = service.GetEntriesByType(LanguageType.Living);
        LanguageLookupResult code = service.LookupCode(
            "ENG",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageCodeEntry requiredCode = service.RequireCode(
            "eng",
            LanguageCodeForm.Set3,
            lookupOptions);
        LanguageLookupResult name = service.LookupName("English", searchOptions);
        LanguageMatch[] search = service.SearchNames("English", searchOptions);
        LanguageLookupResult value = service.ResolveValue("eng", lookupOptions);
        LanguageCodeEntry requiredValue = service.RequireValue("English", searchOptions);
        LanguageCodeEntry? macro = service.GetMacrolanguageForMember("aao", false);
        LanguageCodeEntry[] members = service.GetMacrolanguageMembers("ara", false);
        LanguageCodeEntry[] parents = service.GetGroupParents("gmw", true);
        LanguageCodeEntry[] children = service.GetGroupChildren("ine", true);
        LanguageRetirement? retirement = service.GetRetirement("agp");

        Assert.Multiple(() =>
        {
            Assert.That(all, Is.Not.Empty);
            Assert.That(set1.Any(entry => entry.Set3Code == "eng"), Is.True);
            Assert.That(individuals.Any(entry => entry.Set3Code == "eng"), Is.True);
            Assert.That(living.Any(entry => entry.Set3Code == "eng"), Is.True);
            Assert.That(code.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(code.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(requiredCode.Set3Code, Is.EqualTo("eng"));
            Assert.That(name.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(search.Select(match => match.Entry?.Set3Code),
                Does.Contain("eng"));
            Assert.That(value.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(requiredValue.Set3Code, Is.EqualTo("eng"));
            Assert.That(macro?.Set3Code, Is.EqualTo("ara"));
            Assert.That(members.Any(entry => entry.Set3Code == "aao"), Is.True);
            Assert.That(parents.Any(entry => entry.Set5Code == "ine"), Is.True);
            Assert.That(children.Any(entry => entry.Set5Code == "gmw"), Is.True);
            Assert.That(retirement?.RetiredCode, Is.EqualTo("agp"));
        });
    }

    [Test]
    public void NormalizerContractRoutesCodeAndTagOperations()
    {
        ILanguageNormalizer service = new LanguageCodeService();
        LanguageNormalizationResult normalized = service.NormalizeCode(
            "  ENG  ",
            new LanguageNormalizationOptions
            {
                TargetForm = LanguageCodeForm.Alpha2,
                TrimWhitespace = true,
                NormalizeCase = true
            });
        LanguageNormalizationResult converted = service.ConvertCode(
            "fra",
            new LanguageNormalizationOptions
            {
                TargetForm = LanguageCodeForm.Set2Bibliographic
            });
        LanguageNormalizationResult tag = service.NormalizeTag(
            "EN_us",
            new LanguageNormalizationOptions
            {
                NormalizeTagSeparators = true,
                NormalizeTagCasing = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Output, Is.EqualTo("en"));
            Assert.That(normalized.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(converted.Output, Is.EqualTo("fre"));
            Assert.That(converted.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(tag.Output, Is.EqualTo("en-US"));
        });
    }

    [Test]
    public void ValidatorContractRoutesDetailedAndFastOperations()
    {
        ILanguageValidator service = new LanguageCodeService();
        LanguageValidationOptions options = new LanguageValidationOptions();

        LanguageValidationResult code = service.ValidateCode("eng", options);
        bool span = service.ValidateCodeSpan("eng".AsSpan(), options);
        bool assigned = service.IsAssignedCode("eng", LanguageCodeForm.Set3);
        LanguageValidationResult tag = service.ValidateTag("en-US", options);

        Assert.Multiple(() =>
        {
            Assert.That(code.IsValid, Is.True);
            Assert.That(code.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(span, Is.True);
            Assert.That(assigned, Is.True);
            Assert.That(tag.IsValid, Is.True);
            Assert.That(tag.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [Test]
    public void TagContractRoutesParsingMetadataResolutionAndMatching()
    {
        ILanguageTagService service = new LanguageCodeService();

        LanguageTag parsed = service.ParseTag("en-US");
        LanguageLookupResult resolved = service.ResolveTagLanguage(parsed);
        LanguageSubtagMetadata? metadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            "EN");
        LanguageTagMatchResult[] filtered = service.FilterTags(
            "en",
            new[] { "fr", "en-US", "de" });
        LanguageTagMatchResult best = service.LookupBestTag(
            "en-US-x-private",
            new[] { "fr", "en", "en-US" });

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Language, Is.EqualTo("en"));
            Assert.That(parsed.Region, Is.EqualTo("US"));
            Assert.That(resolved.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(metadata?.Value, Is.EqualTo("en"));
            Assert.That(filtered.Select(result => result.MatchedTag),
                Is.EqualTo(new[] { "en-US" }));
            Assert.That(best.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(best.MatchedIndex, Is.EqualTo(2));
        });
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        if (entry.Set3Code != null)
        {
            return entry.Set3Code;
        }

        if (entry.Set5Code != null)
        {
            return entry.Set5Code;
        }

        if (entry.Set2TerminologyCode != null)
        {
            return entry.Set2TerminologyCode;
        }

        if (entry.Set2BibliographicCode != null)
        {
            return entry.Set2BibliographicCode;
        }

        return entry.Alpha2Code == null ? string.Empty : entry.Alpha2Code;
    }

    private static string StableMatch(LanguageMatch match)
    {
        string code = match.Entry == null ? string.Empty : StableCode(match.Entry);
        string value = match.MatchedValue == null ? string.Empty : match.MatchedValue;
        return match.Rank + ":" + code + ":" + value;
    }
}
