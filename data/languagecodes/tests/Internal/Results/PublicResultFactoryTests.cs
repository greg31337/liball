using languagecodes;

namespace languagecodes.Tests;

public class PublicResultFactoryTests
{
    [Test]
    public void InternalTableRowsNeverShareArraysWithCreatedPublicResults()
    {
        RetirementRow retirementRow = RetirementIndex.Data.RecordsByCode["agp"];
        string key = SubtagIndexBuilder.CreateRecordKey(LanguageSubtagType.Variant, "1994");
        IanaSubtagRow subtagRow =
            SubtagIndex.Data.Records[SubtagIndex.Data.RecordIndexesByKey[key]];

        LanguageRetirement retirement = PublicResultFactory.CreateLanguageRetirement(retirementRow);
        LanguageSubtagMetadata metadata =
            PublicResultFactory.CreateLanguageSubtagMetadata(subtagRow);
        retirement.ReplacementCodes[0] = "changed";
        metadata.Descriptions[0] = "changed";
        metadata.Prefixes[0] = "changed";
        metadata.Comments[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(retirement.Reason, Is.EqualTo(LanguageRetirementReason.Split));
            Assert.That(retirementRow.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(metadata.Type, Is.EqualTo(LanguageSubtagType.Variant));
            Assert.That(subtagRow.Descriptions[0], Is.EqualTo("Standardized Resian orthography"));
            Assert.That(subtagRow.Prefixes[0], Is.EqualTo("sl-rozaj"));
            Assert.That(
                subtagRow.Comments[0],
                Is.EqualTo("For standardized Resian an orthography was published in 1994."));
        });
    }

    [Test]
    public void InternalRelationshipNameAndAliasRowsMapTheirSourceSets()
    {
        LanguageName name = PublicResultFactory.CreateLanguageName(
            new NameRow(
                "set3:eng",
                "English",
                "en",
                LanguageNameKind.Reference,
                ReconciledSource.Iso639Set3,
                true));
        LanguageAlias alias = PublicResultFactory.CreateLanguageAlias(
            new AliasRow(
                "set3:heb",
                "iw",
                LanguageAliasKind.IanaDeprecated,
                "he",
                ReconciledSource.Iana,
                "language:iw",
                true));
        LanguageRelationship relationship = PublicResultFactory.CreateLanguageRelationship(
            new RelationshipRow(
                "afa",
                "ber",
                LanguageRelationshipKind.Set5ParentChild,
                ReconciledRelationshipStatus.Retired,
                ReconciledSource.Iso639Set5));

        Assert.Multiple(() =>
        {
            Assert.That(name.Text, Is.EqualTo("English"));
            Assert.That(name.SourceSet, Is.EqualTo(Iso639Set.Set3));
            Assert.That(name.IsReferenceName, Is.True);
            Assert.That(alias.Value, Is.EqualTo("iw"));
            Assert.That(alias.PreferredCode, Is.EqualTo("he"));
            Assert.That(alias.SourceSet, Is.EqualTo(Iso639Set.None));
            Assert.That(relationship.SourceSet, Is.EqualTo(Iso639Set.Set5));
            Assert.That(relationship.IsRetired, Is.True);
            Assert.That(
                PublicResultFactory.ConvertSourceSet(ReconciledSource.Iso639Set1),
                Is.EqualTo(Iso639Set.Set1));
            Assert.That(
                PublicResultFactory.ConvertSourceSet(ReconciledSource.Iso639Set2),
                Is.EqualTo(Iso639Set.Set2));
            Assert.That(
                PublicResultFactory.ConvertSourceSet(ReconciledSource.Unknown),
                Is.EqualTo(Iso639Set.None));
        });
    }

    [Test]
    public void LanguageEntryCreationAndCopyAreRecursive()
    {
        LanguageName[] names =
        {
            new LanguageName
            {
                Text = "French",
                NameLanguage = "en",
                Kind = LanguageNameKind.Reference,
                SourceSet = Iso639Set.Set3,
                IsReferenceName = true
            }
        };
        LanguageAlias[] aliases =
        {
            new LanguageAlias
            {
                Value = "fre",
                Kind = LanguageAliasKind.Set2Bibliographic,
                PreferredCode = "fra",
                SourceSet = Iso639Set.Set2
            }
        };
        LanguageRelationship[] relationships =
        {
            new LanguageRelationship
            {
                ParentCode = "roa",
                ChildCode = "fra",
                Kind = LanguageRelationshipKind.Set5ParentChild,
                SourceSet = Iso639Set.Set5
            }
        };
        LanguageRetirement retirement = new LanguageRetirement
        {
            RetiredCode = "old",
            ReplacementCodes = new[] { "fra" }
        };
        LanguageRow row = new LanguageRow(
            "set3:fra",
            "fr",
            "fra",
            "fre",
            "fra",
            null,
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            "Comment");

        LanguageCodeEntry created = PublicResultFactory.CreateLanguageCodeEntry(
            row,
            "French",
            names,
            aliases,
            relationships,
            retirement);
        names[0].Text = "changed";
        aliases[0].Value = "changed";
        relationships[0].ParentCode = "changed";
        retirement.ReplacementCodes[0] = "changed";
        LanguageCodeEntry copied = PublicResultFactory.CopyLanguageCodeEntry(created);
        LanguageCodeEntry[] copiedArray =
            PublicResultFactory.CopyLanguageCodeEntries(new[] { created });

        Assert.Multiple(() =>
        {
            Assert.That(created.InSet1, Is.True);
            Assert.That(created.InSet2, Is.True);
            Assert.That(created.InSet3, Is.True);
            Assert.That(created.InSet5, Is.False);
            Assert.That(created.Names[0].Text, Is.EqualTo("French"));
            Assert.That(created.Aliases[0].Value, Is.EqualTo("fre"));
            Assert.That(created.Relationships[0].ParentCode, Is.EqualTo("roa"));
            Assert.That(created.Retirement?.ReplacementCodes[0], Is.EqualTo("fra"));
            Assert.That(copied, Is.Not.SameAs(created));
            Assert.That(copied.Names[0], Is.Not.SameAs(created.Names[0]));
            Assert.That(copied.Aliases[0], Is.Not.SameAs(created.Aliases[0]));
            Assert.That(copied.Relationships[0], Is.Not.SameAs(created.Relationships[0]));
            Assert.That(copied.Retirement, Is.Not.SameAs(created.Retirement));
            Assert.That(copiedArray[0], Is.Not.SameAs(created));
        });
    }

    [Test]
    public void LookupResultCopyDoesNotExposeInternalOperationState()
    {
        LanguageCodeEntry entry = CreateEntry("eng");
        LanguageLookupResult result = new LanguageLookupResult
        {
            Status = LanguageLookupStatus.Found,
            Entry = entry,
            Candidates = new[]
            {
                new LanguageMatch
                {
                    Entry = entry,
                    MatchedValue = "en",
                    Kind = LanguageMatchKind.Alpha2,
                    Rank = 0
                }
            },
            NormalizedQuery = "en"
        };
        LookupOperationRow operation = new LookupOperationRow(new LanguageLookupOptions(), result);

        LanguageLookupResult returned =
            PublicResultFactory.CopyLanguageLookupResult(operation.Result);
        returned.Entry!.Set3Code = "changed";
        returned.Candidates[0].MatchedValue = "changed";
        returned.Candidates[0].Entry!.Set3Code = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(operation.Result.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(operation.Result.Candidates[0].MatchedValue, Is.EqualTo("en"));
            Assert.That(operation.Result.Candidates[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(returned.Candidates, Is.Not.SameAs(operation.Result.Candidates));
            Assert.That(returned.Candidates[0], Is.Not.SameAs(operation.Result.Candidates[0]));
        });
    }

    [Test]
    public void NormalizationResultCopyClonesEveryNestedValue()
    {
        LanguageNormalizationResult source = new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.Success,
            OriginalInput = "aao",
            Output = "ara",
            SourceForm = LanguageCodeForm.Set3,
            TargetForm = LanguageCodeForm.Set3,
            Entry = CreateEntry("ara"),
            MacrolanguageRelationship = new LanguageRelationship
            {
                ParentCode = "ara",
                ChildCode = "aao",
                Kind = LanguageRelationshipKind.MacrolanguageMember,
                SourceSet = Iso639Set.Set3
            },
            AppliedSteps = new[] { LanguageNormalizationStep.FoldMacrolanguage },
            Warnings = new[] { "Folded" },
            ReplacementCandidates = new[] { "ara" }
        };

        LanguageNormalizationResult copy =
            PublicResultFactory.CopyLanguageNormalizationResult(source);
        copy.Entry!.Set3Code = "changed";
        copy.MacrolanguageRelationship!.ParentCode = "changed";
        copy.AppliedSteps[0] = LanguageNormalizationStep.TrimWhitespace;
        copy.Warnings[0] = "changed";
        copy.ReplacementCandidates[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(source.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(source.MacrolanguageRelationship?.ParentCode, Is.EqualTo("ara"));
            Assert.That(source.AppliedSteps[0], Is.EqualTo(LanguageNormalizationStep.FoldMacrolanguage));
            Assert.That(source.Warnings[0], Is.EqualTo("Folded"));
            Assert.That(source.ReplacementCandidates[0], Is.EqualTo("ara"));
        });
    }

    [Test]
    public void TagMetadataAndMatchCopiesCloneAllArraysAndParsedTags()
    {
        LanguageSubtagMetadata metadata = new LanguageSubtagMetadata
        {
            Type = LanguageSubtagType.Variant,
            Value = "1994",
            Descriptions = new[] { "Resian" },
            Prefixes = new[] { "sl-rozaj" },
            Comments = new[] { "Comment" }
        };
        LanguageTag parsed = new LanguageTag
        {
            Value = "sl-rozaj-1994",
            NormalizedValue = "sl-rozaj-1994",
            Language = "sl",
            Variants = new[] { "rozaj", "1994" },
            Extensions = new[] { "u-ca-gregory" },
            PrivateUse = new[] { "example" }
        };
        LanguageTagMatchResult match = new LanguageTagMatchResult
        {
            Status = LanguageTagMatchStatus.Matched,
            MatchedTag = parsed.Value,
            MatchedIndex = 1,
            NormalizedRange = "sl",
            ParsedTag = parsed
        };

        LanguageSubtagMetadata metadataCopy =
            PublicResultFactory.CopyLanguageSubtagMetadata(metadata);
        LanguageTagMatchResult[] matchCopies =
            PublicResultFactory.CopyLanguageTagMatchResults(new[] { match });
        LanguageTag returnedTag = matchCopies[0].ParsedTag!;
        metadataCopy.Descriptions[0] = "changed";
        metadataCopy.Prefixes[0] = "changed";
        metadataCopy.Comments[0] = "changed";
        returnedTag.Variants[0] = "changed";
        returnedTag.Extensions[0] = "changed";
        returnedTag.PrivateUse[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Descriptions[0], Is.EqualTo("Resian"));
            Assert.That(metadata.Prefixes[0], Is.EqualTo("sl-rozaj"));
            Assert.That(metadata.Comments[0], Is.EqualTo("Comment"));
            Assert.That(parsed.Variants[0], Is.EqualTo("rozaj"));
            Assert.That(parsed.Extensions[0], Is.EqualTo("u-ca-gregory"));
            Assert.That(parsed.PrivateUse[0], Is.EqualTo("example"));
            Assert.That(matchCopies[0], Is.Not.SameAs(match));
            Assert.That(matchCopies[0].ParsedTag, Is.Not.SameAs(parsed));
        });
    }

    [Test]
    public void ValidationResultAndOptionsCopiesAreIndependent()
    {
        LanguageValidationResult result = new LanguageValidationResult
        {
            IsWellFormed = true,
            IsAssigned = true,
            IsValid = true,
            IsCanonical = false,
            Entry = CreateEntry("fra"),
            CanonicalValue = "fra",
            Issues = new[]
            {
                new LanguageValidationIssue
                {
                    Severity = LanguageValidationSeverity.Warning,
                    Error = LanguageValidationError.NonCanonical,
                    Component = "fre",
                    Message = "Use fra"
                }
            }
        };
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set3,
            RequiredForm = LanguageCodeForm.Set3,
            AllowedStatuses = new[] { LanguageCodeStatus.Active },
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowedTypes = new[] { LanguageType.Living },
            RequireCanonical = true
        };

        LanguageValidationResult resultCopy =
            PublicResultFactory.CopyLanguageValidationResult(result);
        LanguageValidationOptions optionsCopy =
            PublicResultFactory.CopyLanguageValidationOptions(options);
        resultCopy.Entry!.Set3Code = "changed";
        resultCopy.Issues[0].Message = "changed";
        optionsCopy.AllowedStatuses[0] = LanguageCodeStatus.Retired;
        optionsCopy.AllowedScopes[0] = LanguageScope.Collection;
        optionsCopy.AllowedTypes[0] = LanguageType.Extinct;

        Assert.Multiple(() =>
        {
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(result.Issues[0].Message, Is.EqualTo("Use fra"));
            Assert.That(options.AllowedStatuses[0], Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(options.AllowedScopes[0], Is.EqualTo(LanguageScope.Individual));
            Assert.That(options.AllowedTypes[0], Is.EqualTo(LanguageType.Living));
        });
    }

    [Test]
    public void ScalarOptionCopiesPreserveEveryPolicy()
    {
        LanguageLookupOptions lookup = new LanguageLookupOptions
        {
            TrimWhitespace = true,
            NormalizeCase = true,
            NormalizeNames = true,
            IncludeRetiredCodes = true,
            NameLanguage = "fr",
            SearchMode = LanguageSearchMode.Contains,
            ResultLimit = 12
        };
        LanguageNormalizationOptions normalization = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2,
            ReplaceDeprecated = true,
            ReplaceRetired = true,
            MacrolanguagePolicy = MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            TrimWhitespace = true,
            NormalizeCase = true,
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true,
            ReplaceGrandfatheredTags = true,
            RemoveSuppressScript = true
        };

        LanguageLookupOptions lookupCopy = PublicResultFactory.CopyLanguageLookupOptions(lookup);
        LanguageNormalizationOptions normalizationCopy =
            PublicResultFactory.CopyLanguageNormalizationOptions(normalization);

        Assert.Multiple(() =>
        {
            Assert.That(lookupCopy, Is.Not.SameAs(lookup));
            Assert.That(lookupCopy.TrimWhitespace, Is.True);
            Assert.That(lookupCopy.NormalizeCase, Is.True);
            Assert.That(lookupCopy.NormalizeNames, Is.True);
            Assert.That(lookupCopy.IncludeRetiredCodes, Is.True);
            Assert.That(lookupCopy.NameLanguage, Is.EqualTo("fr"));
            Assert.That(lookupCopy.SearchMode, Is.EqualTo(LanguageSearchMode.Contains));
            Assert.That(lookupCopy.ResultLimit, Is.EqualTo(12));
            Assert.That(normalizationCopy, Is.Not.SameAs(normalization));
            Assert.That(normalizationCopy.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(normalizationCopy.ReplaceDeprecated, Is.True);
            Assert.That(normalizationCopy.ReplaceRetired, Is.True);
            Assert.That(
                normalizationCopy.MacrolanguagePolicy,
                Is.EqualTo(MacrolanguageNormalizationPolicy.FoldToMacrolanguage));
            Assert.That(normalizationCopy.TrimWhitespace, Is.True);
            Assert.That(normalizationCopy.NormalizeCase, Is.True);
            Assert.That(normalizationCopy.NormalizeTagSeparators, Is.True);
            Assert.That(normalizationCopy.NormalizeTagCasing, Is.True);
            Assert.That(normalizationCopy.ReplaceGrandfatheredTags, Is.True);
            Assert.That(normalizationCopy.RemoveSuppressScript, Is.True);
        });
    }

    [Test]
    public void NullCollectionInputsBecomeIndependentEmptySnapshots()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PublicResultFactory.CopyLanguageNames(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageAliases(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageRelationships(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageCodeEntries(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageMatches(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageTagMatchResults(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyLanguageValidationIssues(null), Is.Empty);
            Assert.That(PublicResultFactory.CopyTextValues(null), Is.Empty);
        });
    }

    private static LanguageCodeEntry CreateEntry(string code)
    {
        return new LanguageCodeEntry
        {
            Set3Code = code,
            InSet3 = true,
            Scope = LanguageScope.Individual,
            LanguageType = LanguageType.Living,
            Status = LanguageCodeStatus.Active,
            Names = new[]
            {
                new LanguageName
                {
                    Text = code,
                    NameLanguage = "en",
                    Kind = LanguageNameKind.Reference,
                    SourceSet = Iso639Set.Set3,
                    IsReferenceName = true
                }
            }
        };
    }
}
