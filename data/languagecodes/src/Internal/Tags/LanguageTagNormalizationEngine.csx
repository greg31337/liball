namespace languagecodes;

internal static class LanguageTagNormalizationEngine
{
    internal static LanguageNormalizationResult NormalizeTag(
        string? tag,
        LanguageNormalizationOptions options)
    {
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            new global::System.Collections.Generic.List<LanguageNormalizationStep>();
        if (tag == null)
        {
            return CreateTagNormalizationFailure(
                tag,
                LanguageNormalizationStatus.Invalid,
                LanguageCodeForm.Unknown,
                steps);
        }

        string prepared = tag;
        if (options.TrimWhitespace)
        {
            string trimmed = prepared.Trim();
            if (trimmed != prepared)
            {
                prepared = trimmed;
                steps.Add(LanguageNormalizationStep.TrimWhitespace);
            }
        }

        if (options.NormalizeTagSeparators)
        {
            string separated = prepared.Replace('_', '-');
            if (separated != prepared)
            {
                prepared = separated;
                steps.Add(LanguageNormalizationStep.NormalizeSeparators);
            }
        }

        LanguageValidationResult grammar =
            LanguageTagValidationEngine.ValidateTagWellFormed(prepared);
        if (!grammar.IsWellFormed)
        {
            return CreateTagNormalizationFailure(
                tag,
                LanguageNormalizationStatus.Invalid,
                LanguageCodeForm.Unknown,
                steps);
        }

        LanguageTag parsed = LanguageTagParser.ParseTag(prepared);
        LanguageCodeForm sourceForm = FindTagCodeForm(prepared, parsed);
        LanguageValidationOptions validationOptions = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };
        LanguageValidationResult initialValidation =
            LanguageTagValidationEngine.ValidateTagRegistry(prepared, validationOptions);
        if (!initialValidation.IsValid &&
            !CanShortenUnassignedPrimary(parsed, initialValidation))
        {
            return CreateTagNormalizationFailure(
                tag,
                ContainsUnassignedTagIssue(initialValidation.Issues)
                    ? LanguageNormalizationStatus.NotFound
                    : LanguageNormalizationStatus.Invalid,
                sourceForm,
                steps);
        }

        IanaSubtagRow? complete = SubtagIndex.FindCompleteTag(prepared);
        if (parsed.IsGrandfathered)
        {
            if (options.ReplaceGrandfatheredTags &&
                complete != null &&
                complete.PreferredValue != null)
            {
                prepared = complete.PreferredValue;
                steps.Add(LanguageNormalizationStep.ReplaceGrandfathered);
                parsed = LanguageTagParser.ParseTag(prepared);
            }
            else
            {
                prepared = ApplyRequestedTagCasing(prepared, parsed, options, steps);
                return CreateTagNormalizationSuccess(
                    tag,
                    prepared,
                    sourceForm,
                    FindTagCodeForm(prepared, LanguageTagParser.ParseTag(prepared)),
                    steps);
            }
        }

        complete = SubtagIndex.FindCompleteTag(prepared);
        if (complete != null &&
            complete.RecordType == LanguageSubtagType.Redundant &&
            complete.PreferredValue != null &&
            options.ReplaceDeprecated)
        {
            prepared = complete.PreferredValue;
            AddTagNormalizationStep(steps, LanguageNormalizationStep.ReplaceSubtag);
            parsed = LanguageTagParser.ParseTag(prepared);
        }

        global::System.Collections.Generic.List<string> parts =
            CopyTagPartsToList(prepared.Split('-'));
        if (options.ReplaceDeprecated && ApplyPreferredSubtags(parsed, parts))
        {
            AddTagNormalizationStep(steps, LanguageNormalizationStep.ReplaceSubtag);
            prepared = string.Join("-", parts.ToArray());
            parsed = LanguageTagParser.ParseTag(prepared);
        }

        if (parsed.Extlangs.Length == 1)
        {
            parts = CopyTagPartsToList(prepared.Split('-'));
            parts.RemoveAt(0);
            prepared = string.Join("-", parts.ToArray());
            AddTagNormalizationStep(steps, LanguageNormalizationStep.ReplaceSubtag);
            parsed = LanguageTagParser.ParseTag(prepared);
        }

        if (parsed.Language != null)
        {
            string? shorter = FindShorterLanguageSubtag(parsed.Language);
            if (shorter != null)
            {
                parts = CopyTagPartsToList(prepared.Split('-'));
                parts[0] = shorter;
                prepared = string.Join("-", parts.ToArray());
                AddTagNormalizationStep(steps, LanguageNormalizationStep.ReplaceSubtag);
                parsed = LanguageTagParser.ParseTag(prepared);
            }
        }

        if (options.RemoveSuppressScript &&
            parsed.Language != null &&
            parsed.Script != null)
        {
            string? suppressScript = SubtagIndex.FindSuppressScript(parsed.Language);
            if (suppressScript != null &&
                SubtagIndexBuilder.NormalizeValue(suppressScript) ==
                    SubtagIndexBuilder.NormalizeValue(parsed.Script))
            {
                parts = CopyTagPartsToList(prepared.Split('-'));
                parts.RemoveAt(1 + parsed.Extlangs.Length);
                prepared = string.Join("-", parts.ToArray());
                steps.Add(LanguageNormalizationStep.RemoveSuppressScript);
                parsed = LanguageTagParser.ParseTag(prepared);
            }
        }

        prepared = ApplyRequestedTagCasing(prepared, parsed, options, steps);
        LanguageValidationResult finalValidation =
            LanguageTagValidationEngine.ValidateTagRegistry(prepared, validationOptions);
        if (!finalValidation.IsValid)
        {
            return CreateTagNormalizationFailure(
                tag,
                ContainsUnassignedTagIssue(finalValidation.Issues)
                    ? LanguageNormalizationStatus.NotFound
                    : LanguageNormalizationStatus.Invalid,
                sourceForm,
                steps);
        }

        return CreateTagNormalizationSuccess(
            tag,
            prepared,
            sourceForm,
            FindTagCodeForm(prepared, LanguageTagParser.ParseTag(prepared)),
            steps);
    }

    internal static bool CanShortenUnassignedPrimary(
        LanguageTag parsed,
        LanguageValidationResult validation)
    {
        if (parsed.Language == null ||
            parsed.Extlangs.Length > 0 ||
            FindShorterLanguageSubtag(parsed.Language) == null)
        {
            return false;
        }

        int index;
        for (index = 0; index < validation.Issues.Length; index++)
        {
            LanguageValidationIssue issue = validation.Issues[index];
            if (issue.Severity == LanguageValidationSeverity.Error &&
                (issue.Error != LanguageValidationError.UnassignedCode ||
                    SubtagIndexBuilder.NormalizeValue(issue.Component == null
                        ? string.Empty
                        : issue.Component) != parsed.Language))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ApplyPreferredSubtags(
        LanguageTag parsed,
        global::System.Collections.Generic.List<string> parts)
    {
        bool changed = ApplyPreferredSubtag(
            LanguageSubtagType.Language,
            parsed.Language,
            0,
            parts);
        int partIndex = 1;
        int index;
        for (index = 0; index < parsed.Extlangs.Length; index++)
        {
            if (ApplyPreferredSubtag(
                LanguageSubtagType.Extlang,
                parsed.Extlangs[index],
                partIndex,
                parts))
            {
                changed = true;
            }

            partIndex++;
        }

        if (parsed.Script != null)
        {
            if (ApplyPreferredSubtag(
                LanguageSubtagType.Script,
                parsed.Script,
                partIndex,
                parts))
            {
                changed = true;
            }

            partIndex++;
        }

        if (parsed.Region != null)
        {
            if (ApplyPreferredSubtag(
                LanguageSubtagType.Region,
                parsed.Region,
                partIndex,
                parts))
            {
                changed = true;
            }

            partIndex++;
        }

        for (index = 0; index < parsed.Variants.Length; index++)
        {
            if (ApplyPreferredSubtag(
                LanguageSubtagType.Variant,
                parsed.Variants[index],
                partIndex,
                parts))
            {
                changed = true;
            }

            partIndex++;
        }

        return changed;
    }

    internal static bool ApplyPreferredSubtag(
        LanguageSubtagType type,
        string? value,
        int partIndex,
        global::System.Collections.Generic.List<string> parts)
    {
        string? preferred = SubtagIndex.FindPreferredValue(type, value);
        if (preferred == null || partIndex < 0 || partIndex >= parts.Count)
        {
            return false;
        }

        if (SubtagIndexBuilder.NormalizeValue(parts[partIndex]) ==
            SubtagIndexBuilder.NormalizeValue(preferred))
        {
            return false;
        }

        parts[partIndex] = preferred;
        return true;
    }

    internal static string? FindShorterLanguageSubtag(string value)
    {
        if (value.Length != 3)
        {
            return null;
        }

        LanguageLookupOptions options = new LanguageLookupOptions
        {
            NormalizeCase = true
        };
        LanguageLookupResult lookup =
            LanguageLookupEngine.LookupPreferredCode(value, options);
        if (lookup.Status != LanguageLookupStatus.Found ||
            lookup.Entry == null ||
            lookup.Entry.Status != LanguageCodeStatus.Active ||
            lookup.Entry.Alpha2Code == null)
        {
            return null;
        }

        return lookup.Entry.Alpha2Code;
    }

    internal static string ApplyRequestedTagCasing(
        string value,
        LanguageTag parsed,
        LanguageNormalizationOptions options,
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps)
    {
        if (!options.NormalizeTagCasing ||
            parsed.NormalizedValue == null ||
            parsed.NormalizedValue == value)
        {
            return value;
        }

        steps.Add(LanguageNormalizationStep.NormalizeTagCasing);
        return parsed.NormalizedValue;
    }

    internal static LanguageCodeForm FindTagCodeForm(
        string value,
        LanguageTag parsed)
    {
        if (parsed.IsGrandfathered)
        {
            return LanguageCodeForm.GrandfatheredTag;
        }

        if (parsed.IsRedundant)
        {
            return LanguageCodeForm.RedundantTag;
        }

        if (value.IndexOf('-') < 0 &&
            parsed.Language != null &&
            SubtagIndex.IsAssigned(LanguageSubtagType.Language, parsed.Language))
        {
            return LanguageCodeForm.IanaLanguage;
        }

        return LanguageCodeForm.Unknown;
    }

    internal static bool ContainsUnassignedTagIssue(LanguageValidationIssue[] issues)
    {
        int index;
        for (index = 0; index < issues.Length; index++)
        {
            if (issues[index].Error == LanguageValidationError.UnassignedCode)
            {
                return true;
            }
        }

        return false;
    }

    internal static global::System.Collections.Generic.List<string> CopyTagPartsToList(
        string[] source)
    {
        global::System.Collections.Generic.List<string> destination =
            new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination.Add(source[index]);
        }

        return destination;
    }

    internal static void AddTagNormalizationStep(
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps,
        LanguageNormalizationStep step)
    {
        if (!steps.Contains(step))
        {
            steps.Add(step);
        }
    }

    internal static LanguageNormalizationResult CreateTagNormalizationFailure(
        string? original,
        LanguageNormalizationStatus status,
        LanguageCodeForm sourceForm,
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps)
    {
        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = status,
                OriginalInput = original,
                SourceForm = sourceForm,
                TargetForm = LanguageCodeForm.Unknown,
                AppliedSteps = steps.ToArray()
            });
    }

    internal static LanguageNormalizationResult CreateTagNormalizationSuccess(
        string original,
        string output,
        LanguageCodeForm sourceForm,
        LanguageCodeForm targetForm,
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps)
    {
        LanguageLookupResult resolution =
            LanguageTagResolutionEngine.ResolveTagLanguage(
                LanguageTagParser.ParseTag(output));
        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = steps.Count == 0
                    ? LanguageNormalizationStatus.AlreadyCanonical
                    : LanguageNormalizationStatus.Success,
                OriginalInput = original,
                Output = output,
                SourceForm = sourceForm,
                TargetForm = targetForm,
                Entry = resolution.Entry,
                AppliedSteps = steps.ToArray()
            });
    }
}
