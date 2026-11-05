namespace languagecodes;

internal static class PublicResultFactory
{
    internal static LanguageName CreateLanguageName(NameRow source)
    {
        return new LanguageName
        {
            Text = source.Text,
            NameLanguage = source.NameLanguage,
            Kind = source.Kind,
            SourceSet = ConvertSourceSet(source.Source),
            IsReferenceName = source.IsReferenceName
        };
    }

    internal static LanguageAlias CreateLanguageAlias(AliasRow source)
    {
        return new LanguageAlias
        {
            Value = source.Value,
            Kind = source.Kind,
            PreferredCode = source.PreferredValue,
            IsDeprecated = source.IsDeprecated,
            SourceSet = ConvertSourceSet(source.Source)
        };
    }

    internal static LanguageRelationship CreateLanguageRelationship(RelationshipRow source)
    {
        return new LanguageRelationship
        {
            ParentCode = source.ParentCode,
            ChildCode = source.ChildCode,
            Kind = source.Kind,
            SourceSet = ConvertSourceSet(source.Source),
            IsRetired = source.Status == ReconciledRelationshipStatus.Retired
        };
    }

    internal static LanguageRetirement CreateLanguageRetirement(RetirementRow source)
    {
        return new LanguageRetirement
        {
            RetiredCode = source.RetiredCode,
            FormerReferenceName = source.FormerReferenceName,
            Reason = source.Reason,
            ReplacementCodes = CopyTextValues(source.ReplacementCodes),
            Remedy = source.Remedy,
            EffectiveDate = source.EffectiveDate
        };
    }

    internal static LanguageSubtagMetadata CreateLanguageSubtagMetadata(IanaSubtagRow source)
    {
        return new LanguageSubtagMetadata
        {
            Type = source.RecordType,
            Value = source.Value,
            Descriptions = CopyTextValues(source.Descriptions),
            AddedDate = source.AddedDate,
            DeprecatedDate = source.DeprecatedDate,
            PreferredValue = source.PreferredValue,
            Prefixes = CopyTextValues(source.Prefixes),
            SuppressScript = source.SuppressScript,
            Macrolanguage = source.Macrolanguage,
            Scope = source.Scope,
            Comments = CopyTextValues(source.Comments)
        };
    }

    internal static LanguageCodeEntry CreateLanguageCodeEntry(
        LanguageRow source,
        string? referenceName,
        LanguageName[]? names,
        LanguageAlias[]? aliases,
        LanguageRelationship[]? relationships,
        LanguageRetirement? retirement)
    {
        return new LanguageCodeEntry
        {
            Alpha2Code = source.Alpha2Code,
            Set2TerminologyCode = source.Set2TerminologyCode,
            Set2BibliographicCode = source.Set2BibliographicCode,
            Set3Code = source.Set3Code,
            Set5Code = source.Set5Code,
            InSet1 = source.Alpha2Code != null,
            InSet2 = source.Set2TerminologyCode != null || source.Set2BibliographicCode != null,
            InSet3 = source.Set3Code != null,
            InSet5 = source.Set5Code != null,
            Scope = source.Scope,
            LanguageType = source.LanguageType,
            Status = source.Status,
            ReferenceName = referenceName,
            Names = CopyLanguageNames(names),
            Aliases = CopyLanguageAliases(aliases),
            Relationships = CopyLanguageRelationships(relationships),
            Retirement = retirement == null ? null : CopyLanguageRetirement(retirement),
            Comment = source.Comment
        };
    }

    internal static LanguageName CopyLanguageName(LanguageName source)
    {
        return new LanguageName
        {
            Text = source.Text,
            NameLanguage = source.NameLanguage,
            Kind = source.Kind,
            SourceSet = source.SourceSet,
            IsReferenceName = source.IsReferenceName
        };
    }

    internal static LanguageName[] CopyLanguageNames(LanguageName[]? source)
    {
        if (source == null)
        {
            return new LanguageName[0];
        }

        LanguageName[] destination = new LanguageName[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageName(source[index]);
        }

        return destination;
    }

    internal static LanguageAlias CopyLanguageAlias(LanguageAlias source)
    {
        return new LanguageAlias
        {
            Value = source.Value,
            Kind = source.Kind,
            PreferredCode = source.PreferredCode,
            IsDeprecated = source.IsDeprecated,
            SourceSet = source.SourceSet
        };
    }

    internal static LanguageAlias[] CopyLanguageAliases(LanguageAlias[]? source)
    {
        if (source == null)
        {
            return new LanguageAlias[0];
        }

        LanguageAlias[] destination = new LanguageAlias[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageAlias(source[index]);
        }

        return destination;
    }

    internal static LanguageRelationship CopyLanguageRelationship(LanguageRelationship source)
    {
        return new LanguageRelationship
        {
            ParentCode = source.ParentCode,
            ChildCode = source.ChildCode,
            Kind = source.Kind,
            SourceSet = source.SourceSet,
            IsRetired = source.IsRetired
        };
    }

    internal static LanguageRelationship[] CopyLanguageRelationships(
        LanguageRelationship[]? source)
    {
        if (source == null)
        {
            return new LanguageRelationship[0];
        }

        LanguageRelationship[] destination = new LanguageRelationship[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageRelationship(source[index]);
        }

        return destination;
    }

    internal static LanguageRetirement CopyLanguageRetirement(LanguageRetirement source)
    {
        return new LanguageRetirement
        {
            RetiredCode = source.RetiredCode,
            FormerReferenceName = source.FormerReferenceName,
            Reason = source.Reason,
            ReplacementCodes = CopyTextValues(source.ReplacementCodes),
            Remedy = source.Remedy,
            EffectiveDate = source.EffectiveDate
        };
    }

    internal static LanguageCodeEntry CopyLanguageCodeEntry(LanguageCodeEntry source)
    {
        return new LanguageCodeEntry
        {
            Alpha2Code = source.Alpha2Code,
            Set2TerminologyCode = source.Set2TerminologyCode,
            Set2BibliographicCode = source.Set2BibliographicCode,
            Set3Code = source.Set3Code,
            Set5Code = source.Set5Code,
            InSet1 = source.InSet1,
            InSet2 = source.InSet2,
            InSet3 = source.InSet3,
            InSet5 = source.InSet5,
            Scope = source.Scope,
            LanguageType = source.LanguageType,
            Status = source.Status,
            ReferenceName = source.ReferenceName,
            Names = CopyLanguageNames(source.Names),
            Aliases = CopyLanguageAliases(source.Aliases),
            Relationships = CopyLanguageRelationships(source.Relationships),
            Retirement = source.Retirement == null
                ? null
                : CopyLanguageRetirement(source.Retirement),
            Comment = source.Comment
        };
    }

    internal static LanguageCodeEntry[] CopyLanguageCodeEntries(LanguageCodeEntry[]? source)
    {
        if (source == null)
        {
            return new LanguageCodeEntry[0];
        }

        LanguageCodeEntry[] destination = new LanguageCodeEntry[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageCodeEntry(source[index]);
        }

        return destination;
    }

    internal static LanguageMatch CopyLanguageMatch(LanguageMatch source)
    {
        return new LanguageMatch
        {
            Entry = source.Entry == null ? null : CopyLanguageCodeEntry(source.Entry),
            MatchedValue = source.MatchedValue,
            Kind = source.Kind,
            Rank = source.Rank
        };
    }

    internal static LanguageMatch[] CopyLanguageMatches(LanguageMatch[]? source)
    {
        if (source == null)
        {
            return new LanguageMatch[0];
        }

        LanguageMatch[] destination = new LanguageMatch[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageMatch(source[index]);
        }

        return destination;
    }

    internal static LanguageLookupResult CopyLanguageLookupResult(LanguageLookupResult source)
    {
        return new LanguageLookupResult
        {
            Status = source.Status,
            Entry = source.Entry == null ? null : CopyLanguageCodeEntry(source.Entry),
            Candidates = CopyLanguageMatches(source.Candidates),
            NormalizedQuery = source.NormalizedQuery
        };
    }

    internal static LanguageNormalizationResult CopyLanguageNormalizationResult(
        LanguageNormalizationResult source)
    {
        return new LanguageNormalizationResult
        {
            Status = source.Status,
            OriginalInput = source.OriginalInput,
            Output = source.Output,
            SourceForm = source.SourceForm,
            TargetForm = source.TargetForm,
            Entry = source.Entry == null ? null : CopyLanguageCodeEntry(source.Entry),
            MacrolanguageRelationship = source.MacrolanguageRelationship == null
                ? null
                : CopyLanguageRelationship(source.MacrolanguageRelationship),
            AppliedSteps = CopyNormalizationSteps(source.AppliedSteps),
            Warnings = CopyTextValues(source.Warnings),
            ReplacementCandidates = CopyTextValues(source.ReplacementCandidates)
        };
    }

    internal static LanguageSubtagMetadata CopyLanguageSubtagMetadata(
        LanguageSubtagMetadata source)
    {
        return new LanguageSubtagMetadata
        {
            Type = source.Type,
            Value = source.Value,
            Descriptions = CopyTextValues(source.Descriptions),
            AddedDate = source.AddedDate,
            DeprecatedDate = source.DeprecatedDate,
            PreferredValue = source.PreferredValue,
            Prefixes = CopyTextValues(source.Prefixes),
            SuppressScript = source.SuppressScript,
            Macrolanguage = source.Macrolanguage,
            Scope = source.Scope,
            Comments = CopyTextValues(source.Comments)
        };
    }

    internal static LanguageTag CopyLanguageTag(LanguageTag source)
    {
        return new LanguageTag
        {
            Value = source.Value,
            NormalizedValue = source.NormalizedValue,
            Language = source.Language,
            Extlangs = CopyTextValues(source.Extlangs),
            Script = source.Script,
            Region = source.Region,
            Variants = CopyTextValues(source.Variants),
            Extensions = CopyTextValues(source.Extensions),
            PrivateUse = CopyTextValues(source.PrivateUse),
            IsPrivateUseOnly = source.IsPrivateUseOnly,
            IsGrandfathered = source.IsGrandfathered,
            IsRedundant = source.IsRedundant
        };
    }

    internal static LanguageTagMatchResult CopyLanguageTagMatchResult(
        LanguageTagMatchResult source)
    {
        return new LanguageTagMatchResult
        {
            Status = source.Status,
            MatchedTag = source.MatchedTag,
            MatchedIndex = source.MatchedIndex,
            NormalizedRange = source.NormalizedRange,
            ParsedTag = source.ParsedTag == null ? null : CopyLanguageTag(source.ParsedTag)
        };
    }

    internal static LanguageTagMatchResult[] CopyLanguageTagMatchResults(
        LanguageTagMatchResult[]? source)
    {
        if (source == null)
        {
            return new LanguageTagMatchResult[0];
        }

        LanguageTagMatchResult[] destination = new LanguageTagMatchResult[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageTagMatchResult(source[index]);
        }

        return destination;
    }

    internal static LanguageValidationIssue CopyLanguageValidationIssue(
        LanguageValidationIssue source)
    {
        return new LanguageValidationIssue
        {
            Severity = source.Severity,
            Error = source.Error,
            Component = source.Component,
            Message = source.Message
        };
    }

    internal static LanguageValidationIssue[] CopyLanguageValidationIssues(
        LanguageValidationIssue[]? source)
    {
        if (source == null)
        {
            return new LanguageValidationIssue[0];
        }

        LanguageValidationIssue[] destination = new LanguageValidationIssue[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyLanguageValidationIssue(source[index]);
        }

        return destination;
    }

    internal static LanguageValidationResult CopyLanguageValidationResult(
        LanguageValidationResult source)
    {
        return new LanguageValidationResult
        {
            IsWellFormed = source.IsWellFormed,
            IsAssigned = source.IsAssigned,
            IsValid = source.IsValid,
            IsCanonical = source.IsCanonical,
            Entry = source.Entry == null ? null : CopyLanguageCodeEntry(source.Entry),
            CanonicalValue = source.CanonicalValue,
            Issues = CopyLanguageValidationIssues(source.Issues)
        };
    }

    internal static LanguageLookupOptions CopyLanguageLookupOptions(LanguageLookupOptions source)
    {
        return new LanguageLookupOptions
        {
            TrimWhitespace = source.TrimWhitespace,
            NormalizeCase = source.NormalizeCase,
            NormalizeNames = source.NormalizeNames,
            IncludeRetiredCodes = source.IncludeRetiredCodes,
            NameLanguage = source.NameLanguage,
            SearchMode = source.SearchMode,
            ResultLimit = source.ResultLimit
        };
    }

    internal static LanguageNormalizationOptions CopyLanguageNormalizationOptions(
        LanguageNormalizationOptions source)
    {
        return new LanguageNormalizationOptions
        {
            TargetForm = source.TargetForm,
            ReplaceDeprecated = source.ReplaceDeprecated,
            ReplaceRetired = source.ReplaceRetired,
            MacrolanguagePolicy = source.MacrolanguagePolicy,
            TrimWhitespace = source.TrimWhitespace,
            NormalizeCase = source.NormalizeCase,
            NormalizeTagSeparators = source.NormalizeTagSeparators,
            NormalizeTagCasing = source.NormalizeTagCasing,
            ReplaceGrandfatheredTags = source.ReplaceGrandfatheredTags,
            RemoveSuppressScript = source.RemoveSuppressScript
        };
    }

    internal static LanguageValidationOptions CopyLanguageValidationOptions(
        LanguageValidationOptions source)
    {
        return new LanguageValidationOptions
        {
            RequiredSet = source.RequiredSet,
            RequiredForm = source.RequiredForm,
            AllowedStatuses = CopyCodeStatuses(source.AllowedStatuses),
            AllowedScopes = CopyLanguageScopes(source.AllowedScopes),
            AllowedTypes = CopyLanguageTypes(source.AllowedTypes),
            AllowSpecialCodes = source.AllowSpecialCodes,
            AllowGroupCodes = source.AllowGroupCodes,
            AllowMacrolanguages = source.AllowMacrolanguages,
            AllowPrivateUseCodes = source.AllowPrivateUseCodes,
            RequireCanonical = source.RequireCanonical
        };
    }

    internal static string[] CopyTextValues(string[]? source)
    {
        if (source == null)
        {
            return new string[0];
        }

        string[] destination = new string[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }

    internal static LanguageNormalizationStep[] CopyNormalizationSteps(
        LanguageNormalizationStep[]? source)
    {
        if (source == null)
        {
            return new LanguageNormalizationStep[0];
        }

        LanguageNormalizationStep[] destination = new LanguageNormalizationStep[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }

    internal static LanguageCodeStatus[] CopyCodeStatuses(LanguageCodeStatus[]? source)
    {
        if (source == null)
        {
            return new LanguageCodeStatus[0];
        }

        LanguageCodeStatus[] destination = new LanguageCodeStatus[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }

    internal static LanguageScope[] CopyLanguageScopes(LanguageScope[]? source)
    {
        if (source == null)
        {
            return new LanguageScope[0];
        }

        LanguageScope[] destination = new LanguageScope[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }

    internal static LanguageType[] CopyLanguageTypes(LanguageType[]? source)
    {
        if (source == null)
        {
            return new LanguageType[0];
        }

        LanguageType[] destination = new LanguageType[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = source[index];
        }

        return destination;
    }

    internal static Iso639Set ConvertSourceSet(ReconciledSource source)
    {
        switch (source)
        {
            case ReconciledSource.Iso639Set1:
                return Iso639Set.Set1;
            case ReconciledSource.Iso639Set2:
                return Iso639Set.Set2;
            case ReconciledSource.Iso639Set3:
                return Iso639Set.Set3;
            case ReconciledSource.Iso639Set5:
                return Iso639Set.Set5;
            default:
                return Iso639Set.None;
        }
    }
}
