namespace languagecodes;

internal class CodeCanonicalValidationSelection
{
    internal string Value;
    internal string Message;

    internal CodeCanonicalValidationSelection(string value, string message)
    {
        Value = value;
        Message = message;
    }
}

internal static class LanguageValidationEngine
{
    internal static LanguageValidationResult ValidateTag(
        string? tag,
        LanguageValidationOptions options)
    {
        return LanguageTagValidationEngine.ValidateTag(tag, options);
    }

    internal static LanguageValidationResult ValidateCode(
        string? code,
        LanguageValidationOptions options)
    {
        LanguageValidationResult registry = ValidateCodeRegistry(code);
        if (code == null || !registry.IsWellFormed)
        {
            return PublicResultFactory.CopyLanguageValidationResult(registry);
        }

        string prepared = registry.CanonicalValue == null
            ? code
            : registry.CanonicalValue;
        CodeIndexEntry[] entries = CodeIndex.FindAll(prepared);
        CodeIndexEntry[] preferred =
            LanguageLookupEngine.SelectPreferredCodeEntries(entries, true);
        bool privateUse = ContainsCodeForm(preferred, LanguageCodeForm.PrivateUse);
        if (!registry.IsAssigned && !privateUse)
        {
            return PublicResultFactory.CopyLanguageValidationResult(registry);
        }

        LanguageCodeStatus status = SelectValidationStatus(registry, preferred);
        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        bool statusAllowed = options.AllowedStatuses.Length == 0 ||
            ContainsCodeStatus(options.AllowedStatuses, status);
        if (status == LanguageCodeStatus.Retired)
        {
            issues.Add(CreateCodeValidationIssue(
                statusAllowed
                    ? LanguageValidationSeverity.Warning
                    : LanguageValidationSeverity.Error,
                LanguageValidationError.RetiredCode,
                code,
                statusAllowed
                    ? "The code is retired but remains an authoritative historical identifier."
                    : "Retired codes are not accepted by the validation policy."));
        }
        else if (status == LanguageCodeStatus.Deprecated)
        {
            issues.Add(CreateCodeValidationIssue(
                statusAllowed
                    ? LanguageValidationSeverity.Warning
                    : LanguageValidationSeverity.Error,
                LanguageValidationError.DeprecatedCode,
                code,
                statusAllowed
                    ? "The code is deprecated by its authoritative registry."
                    : "Deprecated codes are not accepted by the validation policy."));
        }
        else if (!statusAllowed)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.UnknownCode,
                code,
                "The code status is not accepted by the validation policy."));
        }

        if (options.RequiredSet != Iso639Set.None &&
            !ContainsCodeSet(entries, options.RequiredSet))
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.WrongSet,
                code,
                "The code does not belong to the required ISO 639 set."));
        }

        if (options.RequiredForm != LanguageCodeForm.Unknown &&
            !ContainsCodeForm(entries, options.RequiredForm))
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.WrongCodeForm,
                code,
                "The code does not have the required code form."));
        }

        LanguageCodeEntry? entry = registry.Entry;
        if (entry != null)
        {
            bool scopeAllowed = options.AllowedScopes.Length == 0 ||
                ContainsLanguageScope(options.AllowedScopes, entry.Scope);
            if (entry.Scope == LanguageScope.Macrolanguage &&
                !options.AllowMacrolanguages)
            {
                scopeAllowed = false;
            }

            if (!scopeAllowed)
            {
                issues.Add(CreateCodeValidationIssue(
                    LanguageValidationSeverity.Error,
                    LanguageValidationError.DisallowedScope,
                    code,
                    "The language scope is not accepted by the validation policy."));
            }

            if (options.AllowedTypes.Length > 0 &&
                !ContainsLanguageType(options.AllowedTypes, entry.LanguageType))
            {
                issues.Add(CreateCodeValidationIssue(
                    LanguageValidationSeverity.Error,
                    LanguageValidationError.DisallowedType,
                    code,
                    "The language type is not accepted by the validation policy."));
            }

            if ((entry.Scope == LanguageScope.Special ||
                    entry.LanguageType == LanguageType.Special) &&
                !options.AllowSpecialCodes)
            {
                issues.Add(CreateCodeValidationIssue(
                    LanguageValidationSeverity.Error,
                    LanguageValidationError.SpecialCodeNotAllowed,
                    code,
                    "Special-purpose language codes are not accepted by the validation policy."));
            }

            if ((entry.Scope == LanguageScope.Collection || entry.InSet5) &&
                !options.AllowGroupCodes)
            {
                issues.Add(CreateCodeValidationIssue(
                    LanguageValidationSeverity.Error,
                    LanguageValidationError.GroupCodeNotAllowed,
                    code,
                    "Language collection, family, and group codes are not accepted by the validation policy."));
            }
        }

        if (privateUse)
        {
            issues.Add(CreateCodeValidationIssue(
                options.AllowPrivateUseCodes
                    ? LanguageValidationSeverity.Warning
                    : LanguageValidationSeverity.Error,
                options.AllowPrivateUseCodes
                    ? LanguageValidationError.ReservedCode
                    : LanguageValidationError.PrivateUseNotAllowed,
                code,
                options.AllowPrivateUseCodes
                    ? "The code is reserved for local or private use."
                    : "Private-use language codes are not accepted by the validation policy."));
        }

        string? canonicalValue = registry.CanonicalValue;
        if (!ContainsValidationError(issues))
        {
            CodeCanonicalValidationSelection? canonical =
                SelectCanonicalValidationCode(
                    prepared,
                    registry,
                    preferred,
                    options);
            if (canonical != null)
            {
                canonicalValue = canonical.Value;
                issues.Add(CreateCodeValidationIssue(
                    options.RequireCanonical
                        ? LanguageValidationSeverity.Error
                        : LanguageValidationSeverity.Warning,
                    LanguageValidationError.NonCanonical,
                    code,
                    canonical.Message));
            }
            else
            {
                AppendCanonicalCaseIssue(
                    issues,
                    registry.Issues,
                    options.RequireCanonical);
            }
        }
        else
        {
            AppendCanonicalCaseIssue(issues, registry.Issues, false);
        }

        bool valid = !ContainsValidationError(issues);
        return PublicResultFactory.CopyLanguageValidationResult(
            new LanguageValidationResult
            {
                IsWellFormed = true,
                IsAssigned = registry.IsAssigned,
                IsValid = valid,
                IsCanonical = valid && canonicalValue == null,
                Entry = valid ? entry : null,
                CanonicalValue = canonicalValue,
                Issues = issues.ToArray()
            });
    }

    internal static bool ValidateCodeSpan(
        global::System.ReadOnlySpan<char> code,
        LanguageValidationOptions options)
    {
        if (!AsciiCode.IsLexicallyValid(code))
        {
            return false;
        }

        CodeIndexEntry[] entries = CodeIndex.FindAllSpan(code);
        if (entries.Length == 0)
        {
            return false;
        }

        int preferredLevel = FindSpanPreferredResolutionLevel(entries);
        if (preferredLevel < 0)
        {
            return false;
        }

        CodeIndexEntry? selected = null;
        int entityIndex = -1;
        bool foundEntity = false;
        bool foundRetired = false;
        bool foundPrivateUse = false;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            CodeIndexEntry candidate = entries[index];
            if (LanguageLookupEngine.FindCodeResolutionLevel(candidate, true) !=
                preferredLevel)
            {
                continue;
            }

            if (selected == null)
            {
                selected = candidate;
            }

            if (candidate.Form == LanguageCodeForm.RetiredSet3)
            {
                foundRetired = true;
            }
            else if (candidate.Form == LanguageCodeForm.PrivateUse)
            {
                foundPrivateUse = true;
            }
            else if (candidate.EntityIndex >= 0 &&
                candidate.EntityIndex < LanguageCatalogEngine.Data.EntriesByEntityIndex.Length)
            {
                if (foundEntity && entityIndex != candidate.EntityIndex)
                {
                    return false;
                }

                foundEntity = true;
                entityIndex = candidate.EntityIndex;
            }
        }

        if (!foundEntity && !foundRetired && !foundPrivateUse)
        {
            for (index = 0; index < entries.Length; index++)
            {
                CodeIndexEntry candidate = entries[index];
                if (candidate.Form == LanguageCodeForm.RetiredSet3 &&
                    LanguageCatalogEngine.Data.RetiredEntriesByCode.ContainsKey(
                        candidate.Value))
                {
                    selected = candidate;
                    foundRetired = true;
                    break;
                }
            }
        }

        if (selected == null || (!foundEntity && !foundRetired && !foundPrivateUse))
        {
            return false;
        }

        LanguageCodeStatus status = foundRetired
            ? LanguageCodeStatus.Retired
            : selected.Status;
        if (options.AllowedStatuses.Length > 0 &&
            !ContainsCodeStatus(options.AllowedStatuses, status))
        {
            return false;
        }

        if (options.RequiredSet != Iso639Set.None &&
            !ContainsCodeSet(entries, options.RequiredSet))
        {
            return false;
        }

        if (options.RequiredForm != LanguageCodeForm.Unknown &&
            !ContainsCodeForm(entries, options.RequiredForm))
        {
            return false;
        }

        LanguageCodeEntry? entry = null;
        if (foundEntity)
        {
            entry = LanguageCatalogEngine.Data.EntriesByEntityIndex[entityIndex];
        }
        else if (foundRetired &&
            LanguageCatalogEngine.Data.RetiredEntriesByCode.ContainsKey(selected.Value))
        {
            entry = LanguageCatalogEngine.Data.RetiredEntriesByCode[selected.Value];
        }

        if (foundRetired && entry == null)
        {
            return false;
        }

        if (entry != null && !SpanEntityPolicyAllows(entry, options))
        {
            return false;
        }

        if (foundPrivateUse && !options.AllowPrivateUseCodes)
        {
            return false;
        }

        if (options.RequireCanonical &&
            !IsSpanCanonical(code, selected, entry, options))
        {
            return false;
        }

        return true;
    }

    internal static bool IsAssignedCode(string? code, LanguageCodeForm form)
    {
        if (code == null || !AsciiCode.IsLexicallyValid(code))
        {
            return false;
        }

        CodeIndexEntry[] entries = CodeIndex.FindAllSpan(code);
        int entityIndex = -1;
        bool foundEntity = false;
        bool foundRetired = false;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            CodeIndexEntry entry = entries[index];
            if (form != LanguageCodeForm.Unknown && entry.Form != form)
            {
                continue;
            }

            if (entry.Form == LanguageCodeForm.RetiredSet3)
            {
                foundRetired =
                    LanguageCatalogEngine.Data.RetiredEntriesByCode.ContainsKey(entry.Value);
            }
            else if (entry.Form != LanguageCodeForm.PrivateUse &&
                entry.EntityIndex >= 0 &&
                entry.EntityIndex < LanguageCatalogEngine.Data.EntriesByEntityIndex.Length)
            {
                if (foundEntity && entityIndex != entry.EntityIndex)
                {
                    return false;
                }

                foundEntity = true;
                entityIndex = entry.EntityIndex;
            }
        }

        return foundEntity || foundRetired;
    }

    internal static bool IsNormalizationOutputValid(LanguageNormalizationResult result)
    {
        if (result.Output == null)
        {
            return false;
        }

        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredForm = result.TargetForm,
            AllowSpecialCodes = true,
            AllowGroupCodes = true,
            AllowMacrolanguages = true,
            AllowPrivateUseCodes = true
        };
        return ValidateCode(result.Output, options).IsValid;
    }

    internal static int FindSpanPreferredResolutionLevel(CodeIndexEntry[] entries)
    {
        int preferredLevel = int.MaxValue;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            int level = LanguageLookupEngine.FindCodeResolutionLevel(entries[index], true);
            if (level >= 0 && level < preferredLevel)
            {
                preferredLevel = level;
            }
        }

        return preferredLevel == int.MaxValue ? -1 : preferredLevel;
    }

    internal static bool SpanEntityPolicyAllows(
        LanguageCodeEntry entry,
        LanguageValidationOptions options)
    {
        bool scopeAllowed = options.AllowedScopes.Length == 0 ||
            ContainsLanguageScope(options.AllowedScopes, entry.Scope);
        if (entry.Scope == LanguageScope.Macrolanguage &&
            !options.AllowMacrolanguages)
        {
            scopeAllowed = false;
        }

        if (!scopeAllowed)
        {
            return false;
        }

        if (options.AllowedTypes.Length > 0 &&
            !ContainsLanguageType(options.AllowedTypes, entry.LanguageType))
        {
            return false;
        }

        if ((entry.Scope == LanguageScope.Special ||
                entry.LanguageType == LanguageType.Special) &&
            !options.AllowSpecialCodes)
        {
            return false;
        }

        if ((entry.Scope == LanguageScope.Collection || entry.InSet5) &&
            !options.AllowGroupCodes)
        {
            return false;
        }

        return true;
    }

    internal static bool IsSpanCanonical(
        global::System.ReadOnlySpan<char> code,
        CodeIndexEntry selected,
        LanguageCodeEntry? entry,
        LanguageValidationOptions options)
    {
        int index;
        for (index = 0; index < code.Length; index++)
        {
            if (code[index] >= 'A' && code[index] <= 'Z')
            {
                return false;
            }
        }

        if ((selected.Form == LanguageCodeForm.RetiredSet3 ||
                selected.Status == LanguageCodeStatus.Deprecated) &&
            selected.PreferredValue != null &&
            !SpanEqualsCode(code, selected.PreferredValue))
        {
            return false;
        }

        if (entry == null)
        {
            return true;
        }

        if (selected.Form == LanguageCodeForm.Set2Bibliographic &&
            entry.Set2TerminologyCode != null &&
            !SpanEqualsCode(code, entry.Set2TerminologyCode) &&
            options.RequiredForm != LanguageCodeForm.Set2Bibliographic)
        {
            return false;
        }

        if (options.RequiredForm != LanguageCodeForm.Unknown ||
            options.RequiredSet != Iso639Set.None)
        {
            return true;
        }

        if (entry.Alpha2Code != null &&
            !SpanEqualsCode(code, entry.Alpha2Code))
        {
            return false;
        }

        return true;
    }

    internal static bool SpanEqualsCode(
        global::System.ReadOnlySpan<char> value,
        string expected)
    {
        if (value.Length != expected.Length)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            if (value[index] != expected[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static LanguageValidationResult ValidateCodeRegistry(string? code)
    {
        LanguageValidationResult lexical = ValidateCodeLexically(code);
        if (code == null || !lexical.IsWellFormed)
        {
            return PublicResultFactory.CopyLanguageValidationResult(lexical);
        }

        string prepared = lexical.CanonicalValue == null
            ? code
            : lexical.CanonicalValue;
        CodeIndexEntry[] entries = CodeIndex.FindAll(prepared);
        CodeIndexEntry[] preferred =
            LanguageLookupEngine.SelectPreferredCodeEntries(entries, true);
        LanguageLookupResult lookup = LanguageLookupEngine.BuildCodeLookupResult(
            prepared,
            preferred,
            LanguageCodeForm.Unknown,
            true,
            LanguageCatalogEngine.Data);
        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        bool assigned = false;
        bool valid = false;
        LanguageCodeEntry? entry = null;
        if (lookup.Status == LanguageLookupStatus.Found)
        {
            assigned = true;
            valid = true;
            entry = lookup.Entry;
            if (preferred.Length > 0 &&
                preferred[0].Status == LanguageCodeStatus.Deprecated)
            {
                issues.Add(CreateCodeValidationIssue(
                    LanguageValidationSeverity.Warning,
                    LanguageValidationError.DeprecatedCode,
                    code,
                    "The code is deprecated by its authoritative registry."));
            }
        }
        else if (lookup.Status == LanguageLookupStatus.Retired ||
            (lookup.Status == LanguageLookupStatus.NotFound &&
                LanguageCatalogEngine.Data.RetiredEntriesByCode.ContainsKey(prepared)))
        {
            assigned = true;
            valid = true;
            entry = lookup.Entry;
            if (entry == null)
            {
                entry = PublicResultFactory.CopyLanguageCodeEntry(
                    LanguageCatalogEngine.Data.RetiredEntriesByCode[prepared]);
            }

            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Warning,
                LanguageValidationError.RetiredCode,
                code,
                "The code is retired but remains an authoritative historical identifier."));
        }
        else if (lookup.Status == LanguageLookupStatus.Reserved)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.ReservedCode,
                code,
                "The code is reserved for local or private use and is not assigned."));
        }
        else if (lookup.Status == LanguageLookupStatus.Ambiguous)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.UnknownCode,
                code,
                "The code does not resolve to one authoritative language entity."));
        }
        else
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.UnassignedCode,
                code,
                "The well-formed code is not assigned."));
        }

        AppendCodeValidationIssues(issues, lexical.Issues);
        return PublicResultFactory.CopyLanguageValidationResult(
            new LanguageValidationResult
            {
                IsWellFormed = true,
                IsAssigned = assigned,
                IsValid = valid,
                IsCanonical = assigned && lexical.IsCanonical,
                Entry = entry,
                CanonicalValue = lexical.CanonicalValue,
                Issues = issues.ToArray()
            });
    }

    internal static LanguageValidationResult ValidateCodeLexically(string? code)
    {
        global::System.Collections.Generic.List<LanguageValidationIssue> issues =
            new global::System.Collections.Generic.List<LanguageValidationIssue>();
        if (code == null)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.NullInput,
                null,
                "A language code is required."));
            return CreateLexicalCodeValidationResult(
                false,
                false,
                null,
                issues);
        }

        if (code.Length == 0)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.EmptyInput,
                code,
                "A language code cannot be empty."));
            return CreateLexicalCodeValidationResult(
                false,
                false,
                null,
                issues);
        }

        bool validLength = AsciiCode.ClassifyLength(code) != AsciiCodeLength.Invalid;
        bool containsNonAscii = false;
        bool containsInvalidCharacter = false;
        bool containsSeparator = false;
        bool containsUppercase = false;
        int index;
        for (index = 0; index < code.Length; index++)
        {
            char character = code[index];
            if (character > 127)
            {
                containsNonAscii = true;
            }
            else if (character == '-' || character == '_')
            {
                containsSeparator = true;
            }
            else if (!AsciiCode.IsAsciiLetter(character))
            {
                containsInvalidCharacter = true;
            }
            else if (character >= 'A' && character <= 'Z')
            {
                containsUppercase = true;
            }
        }

        if (!validLength)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.InvalidLength,
                code,
                "Language codes must contain two or three letters."));
        }

        if (containsNonAscii)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.NonAscii,
                code,
                "Language codes contain ASCII letters only."));
        }

        if (containsInvalidCharacter)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.InvalidCharacter,
                code,
                "Language codes cannot contain digits, whitespace, or punctuation."));
        }

        if (containsSeparator)
        {
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Error,
                LanguageValidationError.InvalidSeparator,
                code,
                "Language codes cannot contain separators."));
        }

        if (issues.Count > 0)
        {
            return CreateLexicalCodeValidationResult(
                false,
                false,
                null,
                issues);
        }

        if (containsUppercase)
        {
            string normalized = LowercaseCodeForValidation(code);
            issues.Add(CreateCodeValidationIssue(
                LanguageValidationSeverity.Warning,
                LanguageValidationError.NonCanonical,
                code,
                "Language codes use lowercase ASCII letters."));
            return CreateLexicalCodeValidationResult(
                true,
                false,
                normalized,
                issues);
        }

        return CreateLexicalCodeValidationResult(
            true,
            true,
            null,
            issues);
    }

    internal static LanguageValidationIssue CreateCodeValidationIssue(
        LanguageValidationSeverity severity,
        LanguageValidationError error,
        string? component,
        string message)
    {
        return new LanguageValidationIssue
        {
            Severity = severity,
            Error = error,
            Component = component,
            Message = message
        };
    }

    internal static LanguageValidationResult CreateLexicalCodeValidationResult(
        bool wellFormed,
        bool canonical,
        string? canonicalValue,
        global::System.Collections.Generic.List<LanguageValidationIssue> issues)
    {
        return PublicResultFactory.CopyLanguageValidationResult(
            new LanguageValidationResult
            {
                IsWellFormed = wellFormed,
                IsAssigned = false,
                IsValid = wellFormed,
                IsCanonical = canonical,
                CanonicalValue = canonicalValue,
                Issues = issues.ToArray()
            });
    }

    internal static string LowercaseCodeForValidation(string value)
    {
        char[] characters = value.ToCharArray();
        int index;
        for (index = 0; index < characters.Length; index++)
        {
            characters[index] = AsciiCode.ToLowerAscii(characters[index]);
        }

        return new string(characters);
    }

    internal static void AppendCodeValidationIssues(
        global::System.Collections.Generic.List<LanguageValidationIssue> destination,
        LanguageValidationIssue[] source)
    {
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination.Add(PublicResultFactory.CopyLanguageValidationIssue(source[index]));
        }
    }

    internal static void AppendCanonicalCaseIssue(
        global::System.Collections.Generic.List<LanguageValidationIssue> destination,
        LanguageValidationIssue[] source,
        bool requireCanonical)
    {
        int index;
        for (index = 0; index < source.Length; index++)
        {
            if (source[index].Error == LanguageValidationError.NonCanonical)
            {
                LanguageValidationIssue issue =
                    PublicResultFactory.CopyLanguageValidationIssue(source[index]);
                if (requireCanonical)
                {
                    issue.Severity = LanguageValidationSeverity.Error;
                }

                destination.Add(issue);
            }
        }
    }

    internal static CodeCanonicalValidationSelection? SelectCanonicalValidationCode(
        string prepared,
        LanguageValidationResult registry,
        CodeIndexEntry[] preferred,
        LanguageValidationOptions options)
    {
        if (registry.Entry != null &&
            registry.Entry.Status == LanguageCodeStatus.Retired)
        {
            RetirementChainRow? chain =
                LanguageRetirementEngine.ResolveReplacementChain(prepared);
            if (chain != null &&
                chain.Status == LanguageNormalizationStatus.Success &&
                chain.FinalCode != null &&
                chain.FinalCode != prepared)
            {
                return new CodeCanonicalValidationSelection(
                    chain.FinalCode,
                    $"The authoritative replacement code is {chain.FinalCode}.");
            }

            return null;
        }

        string? preferredValue = preferred.Length == 0
            ? null
            : preferred[0].PreferredValue;
        if (preferred.Length > 0 &&
            preferred[0].Status == LanguageCodeStatus.Deprecated &&
            preferredValue != null &&
            preferredValue != prepared)
        {
            return new CodeCanonicalValidationSelection(
                preferredValue,
                $"The authoritative preferred code is {preferredValue}.");
        }

        if (registry.Entry == null)
        {
            return null;
        }

        LanguageCodeForm sourceForm = preferred.Length == 0
            ? LanguageCodeForm.Unknown
            : preferred[0].Form;
        if (sourceForm == LanguageCodeForm.Set2Bibliographic &&
            registry.Entry.Set2TerminologyCode != null &&
            registry.Entry.Set2TerminologyCode != prepared &&
            options.RequiredForm != LanguageCodeForm.Set2Bibliographic)
        {
            return new CodeCanonicalValidationSelection(
                registry.Entry.Set2TerminologyCode,
                $"The terminology code {registry.Entry.Set2TerminologyCode} is preferred.");
        }

        if (options.RequiredForm != LanguageCodeForm.Unknown ||
            options.RequiredSet != Iso639Set.None)
        {
            return null;
        }

        if (registry.Entry.Alpha2Code != null &&
            registry.Entry.Alpha2Code != prepared)
        {
            return new CodeCanonicalValidationSelection(
                registry.Entry.Alpha2Code,
                $"The shorter alpha-2 code {registry.Entry.Alpha2Code} is preferred.");
        }

        return null;
    }

    internal static LanguageCodeStatus SelectValidationStatus(
        LanguageValidationResult registry,
        CodeIndexEntry[] preferred)
    {
        if (registry.Entry != null && registry.Entry.Status == LanguageCodeStatus.Retired)
        {
            return LanguageCodeStatus.Retired;
        }

        if (preferred.Length > 0)
        {
            return preferred[0].Status;
        }

        return LanguageCodeStatus.Unknown;
    }

    internal static bool ContainsCodeStatus(
        LanguageCodeStatus[] values,
        LanguageCodeStatus expected)
    {
        int index;
        for (index = 0; index < values.Length; index++)
        {
            if (values[index] == expected)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsLanguageScope(
        LanguageScope[] values,
        LanguageScope expected)
    {
        int index;
        for (index = 0; index < values.Length; index++)
        {
            if (values[index] == expected)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsLanguageType(
        LanguageType[] values,
        LanguageType expected)
    {
        int index;
        for (index = 0; index < values.Length; index++)
        {
            if (values[index] == expected)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsCodeSet(CodeIndexEntry[] entries, Iso639Set expected)
    {
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            if (entries[index].Set == expected)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsCodeForm(
        CodeIndexEntry[] entries,
        LanguageCodeForm expected)
    {
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            if (entries[index].Form == expected)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsValidationError(
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
}
