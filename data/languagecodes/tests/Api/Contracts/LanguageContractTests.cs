using languagecodes;

namespace languagecodes.Tests;

public class LanguageContractTests
{
    [Test]
    public void CatalogContractReturnsTypedEntriesAndLookupOutcomes()
    {
        ContractStub service = new ContractStub();
        ILanguageCatalog catalog = service;

        Assert.Multiple(() =>
        {
            Assert.That(catalog.GetAllEntries(), Is.EqualTo(new[] { service.Entry }));
            Assert.That(catalog.GetEntriesBySet(Iso639Set.Set3), Is.EqualTo(new[] { service.Entry }));
            Assert.That(
                catalog.LookupCode("eng", LanguageCodeForm.Set3, service.LookupOptions).Status,
                Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(
                catalog.RequireCode("eng", LanguageCodeForm.Set3, service.LookupOptions),
                Is.SameAs(service.Entry));
            Assert.That(catalog.GetRetirement("eng"), Is.Null);
        });
    }

    [Test]
    public void NormalizerContractReturnsAuditedResults()
    {
        ContractStub service = new ContractStub();
        ILanguageNormalizer normalizer = service;

        LanguageNormalizationResult result = normalizer.ConvertCode("eng", service.NormalizationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo("en"));
            Assert.That(result.Entry, Is.SameAs(service.Entry));
        });
    }

    [Test]
    public void ValidatorContractSupportsStringAndSpanChecks()
    {
        ContractStub service = new ContractStub();
        ILanguageValidator validator = service;

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateCode("eng", service.ValidationOptions).IsValid, Is.True);
            Assert.That(validator.ValidateCodeSpan("eng".AsSpan(), service.ValidationOptions), Is.True);
            Assert.That(validator.IsAssignedCode("eng", LanguageCodeForm.Set3), Is.True);
            Assert.That(validator.ValidateTag("en", service.ValidationOptions).IsValid, Is.True);
        });
    }

    [Test]
    public void TagServiceContractReturnsParsedMetadataAndMatchResults()
    {
        ContractStub service = new ContractStub();
        ILanguageTagService tagService = service;

        LanguageTag parsed = tagService.ParseTag("en-US");
        LanguageSubtagMetadata? metadata = tagService.GetLanguageSubtagMetadata(
            LanguageSubtagType.Language,
            "en");
        LanguageTagMatchResult[] filtered = tagService.FilterTags("en", new[] { "fr", "en-US" });
        LanguageTagMatchResult best = tagService.LookupBestTag("en", new[] { "fr", "en-US" });

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Language, Is.EqualTo("en"));
            Assert.That(tagService.ResolveTagLanguage(parsed).Entry, Is.SameAs(service.Entry));
            Assert.That(metadata?.Value, Is.EqualTo("en"));
            Assert.That(filtered, Has.Length.EqualTo(1));
            Assert.That(filtered[0].MatchedTag, Is.EqualTo("en-US"));
            Assert.That(best.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(best.MatchedIndex, Is.EqualTo(1));
        });
    }

    private sealed class ContractStub :
        ILanguageCatalog,
        ILanguageNormalizer,
        ILanguageValidator,
        ILanguageTagService
    {
        internal readonly LanguageCodeEntry Entry = new LanguageCodeEntry
        {
            Alpha2Code = "en",
            Set3Code = "eng",
            InSet1 = true,
            InSet3 = true,
            Scope = LanguageScope.Individual,
            LanguageType = LanguageType.Living,
            Status = LanguageCodeStatus.Active,
            ReferenceName = "English"
        };

        internal readonly LanguageLookupOptions LookupOptions = new LanguageLookupOptions();
        internal readonly LanguageNormalizationOptions NormalizationOptions = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2
        };
        internal readonly LanguageValidationOptions ValidationOptions = new LanguageValidationOptions();

        public LanguageCodeEntry[] GetAllEntries()
        {
            return new[] { Entry };
        }

        public LanguageCodeEntry[] GetEntriesBySet(Iso639Set set)
        {
            return set == Iso639Set.Set3 ? new[] { Entry } : Array.Empty<LanguageCodeEntry>();
        }

        public LanguageCodeEntry[] GetEntriesByScope(LanguageScope scope)
        {
            return scope == LanguageScope.Individual ? new[] { Entry } : Array.Empty<LanguageCodeEntry>();
        }

        public LanguageCodeEntry[] GetEntriesByType(LanguageType languageType)
        {
            return languageType == LanguageType.Living ? new[] { Entry } : Array.Empty<LanguageCodeEntry>();
        }

        public LanguageLookupResult LookupCode(
            string? code,
            LanguageCodeForm form,
            LanguageLookupOptions options)
        {
            return FoundLookup(code);
        }

        public LanguageCodeEntry RequireCode(
            string? code,
            LanguageCodeForm form,
            LanguageLookupOptions options)
        {
            return Entry;
        }

        public LanguageLookupResult LookupName(string? name, LanguageLookupOptions options)
        {
            return FoundLookup(name);
        }

        public LanguageMatch[] SearchNames(string? query, LanguageLookupOptions options)
        {
            return new[]
            {
                new LanguageMatch
                {
                    Entry = Entry,
                    MatchedValue = query,
                    Kind = LanguageMatchKind.ExactName,
                    Rank = 0
                }
            };
        }

        public LanguageLookupResult ResolveValue(string? value, LanguageLookupOptions options)
        {
            return FoundLookup(value);
        }

        public LanguageCodeEntry RequireValue(string? value, LanguageLookupOptions options)
        {
            return Entry;
        }

        public LanguageCodeEntry? GetMacrolanguageForMember(
            string? memberCode,
            bool includeRetiredRelationships)
        {
            return null;
        }

        public LanguageCodeEntry[] GetMacrolanguageMembers(
            string? macrolanguageCode,
            bool includeRetiredRelationships)
        {
            return Array.Empty<LanguageCodeEntry>();
        }

        public LanguageCodeEntry[] GetGroupParents(string? groupCode, bool transitive)
        {
            return Array.Empty<LanguageCodeEntry>();
        }

        public LanguageCodeEntry[] GetGroupChildren(string? groupCode, bool transitive)
        {
            return Array.Empty<LanguageCodeEntry>();
        }

        public LanguageRetirement? GetRetirement(string? code)
        {
            return null;
        }

        public LanguageNormalizationResult NormalizeCode(
            string? code,
            LanguageNormalizationOptions options)
        {
            return ConvertedResult(code);
        }

        public LanguageNormalizationResult ConvertCode(
            string? code,
            LanguageNormalizationOptions options)
        {
            return ConvertedResult(code);
        }

        public LanguageNormalizationResult NormalizeTag(
            string? tag,
            LanguageNormalizationOptions options)
        {
            return new LanguageNormalizationResult
            {
                Status = LanguageNormalizationStatus.AlreadyCanonical,
                OriginalInput = tag,
                Output = tag,
                Entry = Entry
            };
        }

        public LanguageValidationResult ValidateCode(
            string? code,
            LanguageValidationOptions options)
        {
            return ValidResult();
        }

        public bool ValidateCodeSpan(
            ReadOnlySpan<char> code,
            LanguageValidationOptions options)
        {
            return code.Length == 3 && code[0] == 'e' && code[1] == 'n' && code[2] == 'g';
        }

        public bool IsAssignedCode(string? code, LanguageCodeForm form)
        {
            return code == "eng";
        }

        public LanguageValidationResult ValidateTag(
            string? tag,
            LanguageValidationOptions options)
        {
            return ValidResult();
        }

        public LanguageTag ParseTag(string? tag)
        {
            return new LanguageTag
            {
                Value = tag,
                NormalizedValue = tag,
                Language = "en",
                Region = "US"
            };
        }

        public LanguageLookupResult ResolveTagLanguage(LanguageTag tag)
        {
            return FoundLookup(tag.Language);
        }

        public LanguageSubtagMetadata? GetLanguageSubtagMetadata(
            LanguageSubtagType type,
            string? value)
        {
            return new LanguageSubtagMetadata
            {
                Type = type,
                Value = value,
                Descriptions = new[] { "English" }
            };
        }

        public LanguageTagMatchResult[] FilterTags(string? range, string[] tags)
        {
            return new[] { MatchResult(tags[1], 1) };
        }

        public LanguageTagMatchResult LookupBestTag(string? range, string[] tags)
        {
            return MatchResult(tags[1], 1);
        }

        private LanguageLookupResult FoundLookup(string? value)
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Found,
                Entry = Entry,
                NormalizedQuery = value
            };
        }

        private LanguageNormalizationResult ConvertedResult(string? value)
        {
            return new LanguageNormalizationResult
            {
                Status = LanguageNormalizationStatus.Success,
                OriginalInput = value,
                Output = "en",
                SourceForm = LanguageCodeForm.Set3,
                TargetForm = LanguageCodeForm.Alpha2,
                Entry = Entry,
                AppliedSteps = new[] { LanguageNormalizationStep.ConvertCodeForm }
            };
        }

        private static LanguageValidationResult ValidResult()
        {
            return new LanguageValidationResult
            {
                IsWellFormed = true,
                IsAssigned = true,
                IsValid = true,
                IsCanonical = true,
                CanonicalValue = "eng"
            };
        }

        private static LanguageTagMatchResult MatchResult(string tag, int index)
        {
            return new LanguageTagMatchResult
            {
                Status = LanguageTagMatchStatus.Matched,
                MatchedTag = tag,
                MatchedIndex = index,
                NormalizedRange = "en"
            };
        }
    }
}
