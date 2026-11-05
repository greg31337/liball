using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageMutationAndConcurrencyTests
{
    [Test]
    public void ReturnedEntriesAndLookupGraphsCannotMutateSharedData()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions();

        LanguageCodeEntry[] entries = service.GetAllEntries();
        LanguageCodeEntry aao = entries.Single(entry => entry.Set3Code == "aao");
        LanguageLookupResult lookup = service.LookupCode("aao", LanguageCodeForm.Set3, options);
        LanguageMatch[] search = service.SearchNames(
            "Algerian Saharan Arabic",
            new LanguageLookupOptions
            {
                SearchMode = LanguageSearchMode.Exact,
                ResultLimit = 10
            });
        LanguageRetirement retirement = service.GetRetirement("agp")!;

        entries[0] = new LanguageCodeEntry { Set3Code = "changed-array" };
        aao.Set3Code = "changed-entry";
        aao.Names[0].Text = "changed-name";
        aao.Aliases[0].Value = "changed-alias";
        aao.Relationships[0].ParentCode = "changed-parent";
        lookup.Entry!.Set3Code = "changed-lookup-entry";
        lookup.Candidates[0].Entry!.Names[0].Text = "changed-candidate-name";
        lookup.Candidates[0] = new LanguageMatch { MatchedValue = "changed-candidate" };
        search[0].Entry!.Set3Code = "changed-search-entry";
        search[0] = new LanguageMatch { MatchedValue = "changed-search" };
        retirement.RetiredCode = "changed-retirement";
        retirement.ReplacementCodes[0] = "changed-replacement";

        LanguageCodeEntry reread = service.GetAllEntries().Single(entry => entry.Set3Code == "aao");
        LanguageLookupResult lookupReread = service.LookupCode("aao", LanguageCodeForm.Set3, options);
        LanguageMatch[] searchReread = service.SearchNames(
            "Algerian Saharan Arabic",
            new LanguageLookupOptions
            {
                SearchMode = LanguageSearchMode.Exact,
                ResultLimit = 10
            });
        LanguageRetirement retirementReread = service.GetRetirement("agp")!;

        Assert.Multiple(() =>
        {
            Assert.That(reread.ReferenceName, Is.EqualTo("Algerian Saharan Arabic"));
            Assert.That(reread.Names[0].Text, Is.Not.EqualTo("changed-name"));
            Assert.That(reread.Aliases[0].Value, Is.Not.EqualTo("changed-alias"));
            Assert.That(reread.Relationships[0].ParentCode, Is.EqualTo("ara"));
            Assert.That(lookupReread.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(lookupReread.Candidates[0].Entry?.Names[0].Text, Is.Not.EqualTo("changed-candidate-name"));
            Assert.That(lookupReread.Candidates[0].MatchedValue, Is.EqualTo("aao"));
            Assert.That(searchReread[0].Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(searchReread[0].MatchedValue, Is.EqualTo("Algerian Saharan Arabic"));
            Assert.That(retirementReread.RetiredCode, Is.EqualTo("agp"));
            Assert.That(retirementReread.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
        });
    }

    [Test]
    public void NestedResultArraysCannotMutateLaterResults()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageNormalizationOptions lexicalOptions = new LanguageNormalizationOptions
        {
            TrimWhitespace = true,
            NormalizeCase = true
        };
        LanguageNormalizationOptions splitOptions = new LanguageNormalizationOptions
        {
            ReplaceRetired = true
        };
        LanguageNormalizationOptions foldOptions = new LanguageNormalizationOptions
        {
            MacrolanguagePolicy = MacrolanguageNormalizationPolicy.FoldToMacrolanguage
        };
        const string completeTag = "zh-cmn-Latn-CN-rozaj-a-foo-x-private";

        LanguageNormalizationResult lexical = service.NormalizeCode("  ENG  ", lexicalOptions);
        LanguageNormalizationResult split = service.NormalizeCode("agp", splitOptions);
        LanguageNormalizationResult folded = service.NormalizeCode("aao", foldOptions);
        LanguageValidationResult validation = service.ValidateTag(
            "en--US",
            new LanguageValidationOptions());
        LanguageTag parsed = service.ParseTag(completeTag);
        LanguageSubtagMetadata metadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Variant,
            "1994")!;
        LanguageTagMatchResult[] filtered = service.FilterTags("zh", new[] { completeTag });

        lexical.AppliedSteps[0] = LanguageNormalizationStep.ReplaceRetired;
        split.ReplacementCandidates[0] = "changed-candidate";
        folded.MacrolanguageRelationship!.ParentCode = "changed-macro";
        validation.Issues[0].Message = "changed-issue";
        parsed.Extlangs[0] = "changed-extlang";
        parsed.Variants[0] = "changed-variant";
        parsed.Extensions[0] = "changed-extension";
        parsed.PrivateUse[0] = "changed-private-use";
        metadata.Descriptions[0] = "changed-description";
        metadata.Prefixes[0] = "changed-prefix";
        metadata.Comments[0] = "changed-comment";
        filtered[0].ParsedTag!.PrivateUse[0] = "changed-match-private-use";
        filtered[0] = new LanguageTagMatchResult { MatchedTag = "changed-match" };

        LanguageNormalizationResult lexicalReread = service.NormalizeCode("  ENG  ", lexicalOptions);
        LanguageNormalizationResult splitReread = service.NormalizeCode("agp", splitOptions);
        LanguageNormalizationResult foldedReread = service.NormalizeCode("aao", foldOptions);
        LanguageValidationResult validationReread = service.ValidateTag(
            "en--US",
            new LanguageValidationOptions());
        LanguageTag parsedReread = service.ParseTag(completeTag);
        LanguageSubtagMetadata metadataReread = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Variant,
            "1994")!;
        LanguageTagMatchResult[] filteredReread = service.FilterTags("zh", new[] { completeTag });

        Assert.Multiple(() =>
        {
            Assert.That(lexicalReread.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase
            }));
            Assert.That(splitReread.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(foldedReread.MacrolanguageRelationship?.ParentCode, Is.EqualTo("ara"));
            Assert.That(validationReread.Issues[0].Message, Is.Not.EqualTo("changed-issue"));
            Assert.That(parsedReread.Extlangs, Is.EqualTo(new[] { "cmn" }));
            Assert.That(parsedReread.Variants, Is.EqualTo(new[] { "rozaj" }));
            Assert.That(parsedReread.Extensions, Is.EqualTo(new[] { "a-foo" }));
            Assert.That(parsedReread.PrivateUse, Is.EqualTo(new[] { "private" }));
            Assert.That(metadataReread.Descriptions[0], Is.EqualTo("Standardized Resian orthography"));
            Assert.That(metadataReread.Prefixes[0], Is.EqualTo("sl-rozaj"));
            Assert.That(metadataReread.Comments[0], Does.Contain("orthography"));
            Assert.That(filteredReread[0].MatchedTag, Is.EqualTo(completeTag));
            Assert.That(filteredReread[0].ParsedTag?.PrivateUse, Is.EqualTo(new[] { "private" }));
        });
    }

    [Test]
    public void CallerOwnedOptionsRemainUnchangedAndCannotMutateSharedData()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions lookupOptions = new LanguageLookupOptions
        {
            NormalizeCase = true,
            SearchMode = LanguageSearchMode.Exact,
            ResultLimit = 10
        };
        LanguageNormalizationOptions normalizationOptions = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2,
            NormalizeCase = true
        };
        LanguageValidationOptions validationOptions = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set3,
            AllowedStatuses = new[] { LanguageCodeStatus.Active },
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowedTypes = new[] { LanguageType.Living }
        };

        LanguageLookupResult lookup = service.LookupCode("ENG", LanguageCodeForm.Set3, lookupOptions);
        LanguageNormalizationResult normalization = service.ConvertCode("ENG", normalizationOptions);
        LanguageValidationResult validation = service.ValidateCode("eng", validationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(lookupOptions.NormalizeCase, Is.True);
            Assert.That(lookupOptions.ResultLimit, Is.EqualTo(10));
            Assert.That(normalizationOptions.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(normalizationOptions.NormalizeCase, Is.True);
            Assert.That(validationOptions.AllowedStatuses, Is.EqualTo(new[] { LanguageCodeStatus.Active }));
            Assert.That(validationOptions.AllowedScopes, Is.EqualTo(new[] { LanguageScope.Individual }));
            Assert.That(validationOptions.AllowedTypes, Is.EqualTo(new[] { LanguageType.Living }));
        });

        lookupOptions.NormalizeCase = false;
        lookupOptions.ResultLimit = 0;
        normalizationOptions.TargetForm = LanguageCodeForm.Set5;
        normalizationOptions.NormalizeCase = false;
        validationOptions.AllowedStatuses[0] = LanguageCodeStatus.Retired;
        validationOptions.AllowedScopes[0] = LanguageScope.Collection;
        validationOptions.AllowedTypes[0] = LanguageType.Extinct;

        LanguageLookupResult freshLookup = service.LookupCode(
            "eng",
            LanguageCodeForm.Set3,
            new LanguageLookupOptions());
        LanguageNormalizationResult freshNormalization = service.ConvertCode(
            "eng",
            new LanguageNormalizationOptions { TargetForm = LanguageCodeForm.Alpha2 });
        LanguageValidationResult freshValidation = service.ValidateCode(
            "eng",
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(normalization.Output, Is.EqualTo("en"));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(freshLookup.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(freshNormalization.Output, Is.EqualTo("en"));
            Assert.That(freshValidation.IsValid, Is.True);
            Assert.That(service.GetAllEntries().Single(entry => entry.Set3Code == "eng").ReferenceName,
                Is.EqualTo("English"));
        });
    }

    [Test]
    public void ParallelReadOnlyCallsAreDeterministicAndExceptionFree()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions searchOptions = new LanguageLookupOptions
        {
            NormalizeCase = true,
            NormalizeNames = true,
            SearchMode = LanguageSearchMode.Contains,
            ResultLimit = 12
        };
        LanguageNormalizationOptions normalizationOptions = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2,
            NormalizeCase = true
        };
        LanguageValidationOptions validationOptions = new LanguageValidationOptions();
        const int operationCount = 64;
        string?[] signatures = new string?[operationCount];
        Exception?[] failures = new Exception?[operationCount];

        Parallel.For(0, operationCount, index =>
        {
            try
            {
                signatures[index] = BuildReadSignature(
                    service,
                    searchOptions,
                    normalizationOptions,
                    validationOptions);
            }
            catch (Exception exception)
            {
                failures[index] = exception;
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.All.Null);
            Assert.That(signatures, Is.All.EqualTo(signatures[0]));
            Assert.That(signatures[0], Is.Not.Null.And.Not.Empty);
        });
    }

    private static string BuildReadSignature(
        LanguageCodeService service,
        LanguageLookupOptions searchOptions,
        LanguageNormalizationOptions normalizationOptions,
        LanguageValidationOptions validationOptions)
    {
        LanguageCodeEntry[] set1 = service.GetEntriesBySet(Iso639Set.Set1);
        LanguageLookupResult lookup = service.LookupCode(
            "ENG",
            LanguageCodeForm.Set3,
            searchOptions);
        LanguageMatch[] search = service.SearchNames("Arabic", searchOptions);
        LanguageNormalizationResult normalized = service.ConvertCode("ENG", normalizationOptions);
        LanguageValidationResult validation = service.ValidateTag("en-US", validationOptions);
        LanguageTag tag = service.ParseTag("zh-cmn-Hans-CN-x-private");
        LanguageSubtagMetadata metadata = service.GetLanguageSubtagMetadata(
            LanguageSubtagType.Variant,
            "1994")!;
        LanguageCodeEntry? macro = service.GetMacrolanguageForMember("aao", false);
        LanguageRetirement retirement = service.GetRetirement("agp")!;
        LanguageTagMatchResult match = service.LookupBestTag(
            "en-US-x-private",
            new[] { "fr", "en", "en-US" });

        return string.Join(
            "|",
            set1.Length,
            set1[0].Alpha2Code,
            set1[set1.Length - 1].Alpha2Code,
            lookup.Entry?.Set3Code,
            string.Join(",", search.Select(item => item.Entry?.Set3Code ?? item.Entry?.Set5Code)),
            normalized.Output,
            validation.IsValid,
            tag.NormalizedValue,
            metadata.Descriptions[0],
            macro?.Set3Code,
            string.Join(",", retirement.ReplacementCodes),
            match.MatchedTag,
            match.MatchedIndex);
    }
}
