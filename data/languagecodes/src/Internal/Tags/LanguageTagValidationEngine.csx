namespace languagecodes;

internal static class LanguageTagValidationEngine
{
    internal static LanguageValidationResult ValidateTag(
        string? value,
        LanguageValidationOptions options)
    {
        LanguageValidationResult registry = ValidateTagRegistry(value, options);
        if (value == null || !registry.IsValid)
        {
            return registry;
        }

        LanguageLookupResult resolution =
            LanguageTagResolutionEngine.ResolveTagLanguage(
                LanguageTagParser.ParseTag(value));
        registry.Entry = resolution.Entry;

        LanguageNormalizationOptions normalizationOptions =
            new LanguageNormalizationOptions
            {
                ReplaceDeprecated = true,
                NormalizeTagCasing = true,
                ReplaceGrandfatheredTags = true
            };
        LanguageNormalizationResult normalization =
            LanguageTagNormalizationEngine.NormalizeTag(value, normalizationOptions);
        if ((normalization.Status != LanguageNormalizationStatus.Success &&
                normalization.Status != LanguageNormalizationStatus.AlreadyCanonical) ||
            normalization.Output == null ||
            normalization.Output == value)
        {
            LanguageValidationResult canonical =
                PublicResultFactory.CopyLanguageValidationResult(registry);
            canonical.IsCanonical = true;
            return canonical;
        }

        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        int index;
        for (index = 0; index < registry.Issues.Length; index++)
        {
            issues.Add(registry.Issues[index]);
        }

        issues.Add(new LanguageValidationIssue
        {
            Severity = options.RequireCanonical
                ? LanguageValidationSeverity.Error
                : LanguageValidationSeverity.Warning,
            Error = LanguageValidationError.NonCanonical,
            Component = value,
            Message = "The language tag has a canonical IANA representation."
        });
        return PublicResultFactory.CopyLanguageValidationResult(
            new LanguageValidationResult
            {
                IsWellFormed = registry.IsWellFormed,
                IsAssigned = registry.IsAssigned,
                IsValid = !options.RequireCanonical,
                IsCanonical = false,
                Entry = registry.Entry,
                CanonicalValue = normalization.Output,
                Issues = issues.ToArray()
            });
    }

    internal static LanguageValidationResult ValidateTagRegistry(
        string? value,
        LanguageValidationOptions options)
    {
        LanguageValidationResult grammar = ValidateTagWellFormed(value);
        if (!grammar.IsWellFormed || value == null)
        {
            return grammar;
        }

        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        int index;
        for (index = 0; index < grammar.Issues.Length; index++)
        {
            issues.Add(grammar.Issues[index]);
        }

        LanguageTag tag = LanguageTagParser.ParseTag(value);
        if (tag.IsGrandfathered)
        {
            return CreateTagValidationResult(
                true,
                true,
                !ContainsTagValidationError(issues),
                issues);
        }

        bool assigned = true;
        if (tag.IsPrivateUseOnly)
        {
            assigned = false;
            issues.Add(CreatePrivateUseIssue(value, options));
            return CreateTagValidationResult(
                true,
                assigned,
                !ContainsTagValidationError(issues),
                issues);
        }

        string language = tag.Language == null ? string.Empty : tag.Language;
        if (IsPrivateUseLanguage(language))
        {
            assigned = false;
            issues.Add(CreatePrivateUseIssue(language, options));
        }
        else if (!ValidateAssignedSubtag(
            LanguageSubtagType.Language,
            language,
            issues))
        {
            assigned = false;
        }

        string preceding = language;
        for (index = 0; index < tag.Extlangs.Length; index++)
        {
            string extlang = tag.Extlangs[index];
            bool extlangAssigned = ValidateAssignedSubtag(
                LanguageSubtagType.Extlang,
                extlang,
                issues);
            if (!extlangAssigned)
            {
                assigned = false;
            }
            else if (index > 0)
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.PrefixMismatch,
                    extlang,
                    "A registered extlang can only occupy the first extlang position."));
            }
            else
            {
                ValidatePrefix(LanguageSubtagType.Extlang, extlang, preceding, issues);
            }

            preceding = preceding + "-" + extlang;
        }

        if (tag.Script != null)
        {
            if (IsPrivateUseScript(tag.Script))
            {
                assigned = false;
                issues.Add(CreatePrivateUseIssue(tag.Script, options));
            }
            else if (!ValidateAssignedSubtag(
                LanguageSubtagType.Script,
                tag.Script,
                issues))
            {
                assigned = false;
            }

            preceding = preceding + "-" + tag.Script;
        }

        if (tag.Region != null)
        {
            if (IsPrivateUseRegion(tag.Region))
            {
                assigned = false;
                issues.Add(CreatePrivateUseIssue(tag.Region, options));
            }
            else if (!ValidateAssignedSubtag(
                LanguageSubtagType.Region,
                tag.Region,
                issues))
            {
                assigned = false;
            }

            preceding = preceding + "-" + tag.Region;
        }

        for (index = 0; index < tag.Variants.Length; index++)
        {
            string variant = tag.Variants[index];
            bool variantAssigned = ValidateAssignedSubtag(
                LanguageSubtagType.Variant,
                variant,
                issues);
            if (!variantAssigned)
            {
                assigned = false;
            }
            else
            {
                ValidatePrefix(LanguageSubtagType.Variant, variant, preceding, issues);
            }

            preceding = preceding + "-" + variant;
        }

        if (tag.PrivateUse.Length > 0)
        {
            assigned = false;
            issues.Add(CreatePrivateUseIssue("x", options));
        }

        return CreateTagValidationResult(
            true,
            assigned,
            !ContainsTagValidationError(issues),
            issues);
    }

    internal static LanguageValidationResult ValidateTagWellFormed(string? value)
    {
        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        if (value == null)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.NullInput,
                null,
                "A language tag is required."));
            return CreateTagValidationResult(false, false, false, issues);
        }

        if (value.Length == 0)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.EmptyInput,
                value,
                "A language tag cannot be empty."));
            return CreateTagValidationResult(false, false, false, issues);
        }

        bool containsInvalidSeparator = false;
        bool containsNonAscii = false;
        bool containsInvalidCharacter = false;
        int index;
        for (index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character > 127)
            {
                containsNonAscii = true;
            }
            else if (character == '_')
            {
                containsInvalidSeparator = true;
            }
            else if (!LanguageTagParser.IsAsciiTagLetter(character) &&
                (character < '0' || character > '9') &&
                character != '-')
            {
                containsInvalidCharacter = true;
            }
        }

        if (containsInvalidSeparator)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.InvalidSeparator,
                value,
                "RFC 5646 language tags use hyphen separators."));
        }

        if (containsNonAscii)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.NonAscii,
                value,
                "Language tags contain ASCII letters and digits only."));
        }

        if (containsInvalidCharacter)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.InvalidCharacter,
                value,
                "Language tags cannot contain whitespace or punctuation other than hyphens."));
        }

        string[] subtags = value.Split('-');
        bool containsInvalidLength = false;
        for (index = 0; index < subtags.Length; index++)
        {
            if (subtags[index].Length == 0 || subtags[index].Length > 8)
            {
                containsInvalidLength = true;
            }
        }

        if (containsInvalidLength)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.InvalidLength,
                value,
                "Each language-tag subtag must contain between one and eight characters."));
        }

        if (containsInvalidSeparator || containsNonAscii ||
            containsInvalidCharacter || containsInvalidLength)
        {
            return CreateTagValidationResult(false, false, false, issues);
        }

        IanaSubtagRow? complete = SubtagIndex.FindCompleteTag(value);
        if (complete != null && complete.RecordType == LanguageSubtagType.Grandfathered)
        {
            return CreateTagValidationResult(true, false, true, issues);
        }

        ValidateTagGrammar(value, subtags, issues);
        bool wellFormed = !ContainsTagWellFormednessError(issues);
        return CreateTagValidationResult(
            wellFormed,
            false,
            issues.Count == 0,
            issues);
    }

    internal static bool ValidateAssignedSubtag(
        LanguageSubtagType type,
        string value,
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        if (SubtagIndex.IsAssigned(type, value))
        {
            return true;
        }

        issues.Add(CreateTagValidationIssue(
            LanguageValidationError.UnassignedCode,
            value,
            "The well-formed subtag is not assigned in the IANA Language Subtag Registry."));
        return false;
    }

    internal static void ValidatePrefix(
        LanguageSubtagType type,
        string value,
        string preceding,
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        IanaSubtagRow? record = SubtagIndex.FindRecord(type, value);
        if (record == null || record.Prefixes.Length == 0)
        {
            return;
        }

        string normalizedPreceding = SubtagIndexBuilder.NormalizeValue(preceding);
        int index;
        for (index = 0; index < record.Prefixes.Length; index++)
        {
            string prefix = SubtagIndexBuilder.NormalizeValue(record.Prefixes[index]);
            if (MatchesExtendedPrefix(normalizedPreceding, prefix))
            {
                return;
            }
        }

        issues.Add(CreateTagValidationIssue(
            LanguageValidationError.PrefixMismatch,
            value,
            "The subtag does not follow one of its registered IANA prefixes."));
    }

    internal static bool MatchesExtendedPrefix(string value, string prefix)
    {
        string[] valueSubtags = value.Split('-');
        string[] prefixSubtags = prefix.Split('-');
        if (valueSubtags.Length == 0 ||
            prefixSubtags.Length == 0 ||
            valueSubtags[0] != prefixSubtags[0])
        {
            return false;
        }

        int valueIndex = 1;
        int prefixIndex = 1;
        while (valueIndex < valueSubtags.Length &&
            prefixIndex < prefixSubtags.Length)
        {
            if (valueSubtags[valueIndex] == prefixSubtags[prefixIndex])
            {
                valueIndex++;
                prefixIndex++;
            }
            else
            {
                if (valueSubtags[valueIndex].Length == 1)
                {
                    return false;
                }

                valueIndex++;
            }
        }

        return prefixIndex == prefixSubtags.Length;
    }

    internal static bool IsPrivateUseLanguage(string value)
    {
        string normalized = SubtagIndexBuilder.NormalizeValue(value);
        return normalized.Length == 3 &&
            global::System.String.CompareOrdinal(normalized, "qaa") >= 0 &&
            global::System.String.CompareOrdinal(normalized, "qtz") <= 0;
    }

    internal static bool IsPrivateUseScript(string value)
    {
        string normalized = SubtagIndexBuilder.NormalizeValue(value);
        return normalized.Length == 4 &&
            global::System.String.CompareOrdinal(normalized, "qaaa") >= 0 &&
            global::System.String.CompareOrdinal(normalized, "qabx") <= 0;
    }

    internal static bool IsPrivateUseRegion(string value)
    {
        string normalized = SubtagIndexBuilder.NormalizeValue(value);
        return normalized == "aa" ||
            normalized == "zz" ||
            (global::System.String.CompareOrdinal(normalized, "qm") >= 0 &&
                global::System.String.CompareOrdinal(normalized, "qz") <= 0) ||
            (global::System.String.CompareOrdinal(normalized, "xa") >= 0 &&
                global::System.String.CompareOrdinal(normalized, "xz") <= 0);
    }

    internal static LanguageValidationIssue CreatePrivateUseIssue(
        string component,
        LanguageValidationOptions options)
    {
        return new LanguageValidationIssue
        {
            Severity = options.AllowPrivateUseCodes
                ? LanguageValidationSeverity.Warning
                : LanguageValidationSeverity.Error,
            Error = options.AllowPrivateUseCodes
                ? LanguageValidationError.ReservedCode
                : LanguageValidationError.PrivateUseNotAllowed,
            Component = component,
            Message = options.AllowPrivateUseCodes
                ? "The component is reserved for local or private use."
                : "Private-use language-tag components are not accepted by the validation policy."
        };
    }

    internal static void ValidateTagGrammar(
        string value,
        string[] subtags,
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        if (LanguageTagParser.IsPrivateUseSingleton(subtags[0]))
        {
            if (subtags.Length == 1)
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.InvalidTag,
                    value,
                    "A private-use singleton requires at least one payload subtag."));
            }

            return;
        }

        if (!LanguageTagParser.IsLanguageSubtag(subtags[0]))
        {
            LanguageValidationError error =
                LanguageTagParser.IsAlphabeticSubtag(subtags[0], 1, 8)
                    ? LanguageValidationError.InvalidLength
                    : LanguageValidationError.InvalidTag;
            issues.Add(CreateTagValidationIssue(
                error,
                subtags[0],
                "The first subtag is not a valid primary language subtag."));
            return;
        }

        global::System.Collections.Generic.HashSet<string> variants =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> extensionSingletons =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        int index = 1;
        if (subtags[0].Length <= 3)
        {
            int extlangCount = 0;
            while (index < subtags.Length &&
                extlangCount < 3 &&
                LanguageTagParser.IsAlphabeticSubtag(subtags[index], 3, 3))
            {
                extlangCount++;
                index++;
            }
        }

        if (index < subtags.Length &&
            LanguageTagParser.IsAlphabeticSubtag(subtags[index], 4, 4))
        {
            index++;
        }

        if (index < subtags.Length &&
            LanguageTagParser.IsRegionSubtag(subtags[index]))
        {
            index++;
        }

        while (index < subtags.Length &&
            LanguageTagParser.IsVariantSubtag(subtags[index]))
        {
            string normalizedVariant =
                LanguageTagParser.LowercaseTagSubtag(subtags[index]);
            if (variants.Contains(normalizedVariant))
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.DuplicateSubtag,
                    subtags[index],
                    "A variant subtag cannot appear more than once."));
            }
            else
            {
                variants.Add(normalizedVariant);
            }

            index++;
        }

        while (index < subtags.Length &&
            LanguageTagParser.IsExtensionSingleton(subtags[index]))
        {
            string normalizedSingleton =
                LanguageTagParser.LowercaseTagSubtag(subtags[index]);
            if (extensionSingletons.Contains(normalizedSingleton))
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.DuplicateSubtag,
                    subtags[index],
                    "An extension singleton cannot appear more than once."));
            }
            else
            {
                extensionSingletons.Add(normalizedSingleton);
            }

            string singleton = subtags[index];
            index++;
            int payloadCount = 0;
            while (index < subtags.Length &&
                LanguageTagParser.IsAlphanumericSubtag(subtags[index], 2, 8))
            {
                payloadCount++;
                index++;
            }

            if (payloadCount == 0)
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.InvalidTag,
                    singleton,
                    "An extension singleton requires at least one payload subtag."));
            }
        }

        if (index < subtags.Length &&
            LanguageTagParser.IsPrivateUseSingleton(subtags[index]))
        {
            string singleton = subtags[index];
            index++;
            int payloadCount = 0;
            while (index < subtags.Length &&
                LanguageTagParser.IsAlphanumericSubtag(subtags[index], 1, 8))
            {
                payloadCount++;
                index++;
            }

            if (payloadCount == 0)
            {
                issues.Add(CreateTagValidationIssue(
                    LanguageValidationError.InvalidTag,
                    singleton,
                    "A private-use singleton requires at least one payload subtag."));
            }
        }

        if (index < subtags.Length)
        {
            issues.Add(CreateTagValidationIssue(
                LanguageValidationError.InvalidTag,
                subtags[index],
                "The subtag is invalid or appears outside RFC 5646 order."));
        }
    }

    internal static LanguageValidationIssue CreateTagValidationIssue(
        LanguageValidationError error,
        string? component,
        string message)
    {
        return new LanguageValidationIssue
        {
            Severity = LanguageValidationSeverity.Error,
            Error = error,
            Component = component,
            Message = message
        };
    }

    internal static bool ContainsTagWellFormednessError(
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        int index;
        for (index = 0; index < issues.Count; index++)
        {
            if (issues[index].Error != LanguageValidationError.DuplicateSubtag)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsTagValidationError(
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        int index;
        for (index = 0; index < issues.Count; index++)
        {
            if (issues[index].Severity == LanguageValidationSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }

    internal static LanguageValidationResult CreateTagValidationResult(
        bool wellFormed,
        bool assigned,
        bool valid,
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        return PublicResultFactory.CopyLanguageValidationResult(
            new LanguageValidationResult
            {
                IsWellFormed = wellFormed,
                IsAssigned = assigned,
                IsValid = valid,
                IsCanonical = false,
                Issues = issues.ToArray()
            });
    }
}
