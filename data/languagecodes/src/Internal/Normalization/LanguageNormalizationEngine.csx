namespace languagecodes;

internal class CanonicalCodeSelection
{
    internal string? Value;
    internal LanguageCodeForm Form;

    internal CanonicalCodeSelection(string? value, LanguageCodeForm form)
    {
        Value = value;
        Form = form;
    }
}

internal static class LanguageNormalizationEngine
{
    internal static LanguageNormalizationResult NormalizeTag(
        string? tag,
        LanguageNormalizationOptions options)
    {
        return LanguageTagNormalizationEngine.NormalizeTag(tag, options);
    }

    internal static LanguageNormalizationResult NormalizeCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        LanguageNormalizationResult lexical = NormalizeCodeLexically(code, options);
        if (lexical.Status != LanguageNormalizationStatus.Success &&
            lexical.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return CompleteCodeNormalizationResult(
                lexical,
                lexical.SourceForm,
                options.TargetForm,
                null);
        }

        LanguageNormalizationResult historical = NormalizeHistoricalCodeResult(
            lexical,
            options,
            RetirementIndex.Data);
        if (historical.Status != LanguageNormalizationStatus.Success &&
            historical.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return CompleteCodeNormalizationResult(
                historical,
                lexical.SourceForm,
                options.TargetForm,
                null);
        }

        LanguageNormalizationResult macrolanguage = NormalizeMacrolanguageCodeResult(
            historical,
            options,
            RelationshipIndex.Data,
            LanguageCatalogEngine.Data);
        if (options.TargetForm == LanguageCodeForm.Unknown)
        {
            return CompleteCodeNormalizationResult(
                macrolanguage,
                lexical.SourceForm,
                options.TargetForm,
                macrolanguage.MacrolanguageRelationship);
        }

        LanguageNormalizationResult canonicalInput =
            PublicResultFactory.CopyLanguageNormalizationResult(macrolanguage);
        canonicalInput.SourceForm = macrolanguage.TargetForm;
        LanguageNormalizationResult canonical = CanonicalizeActiveCodeResult(
            canonicalInput,
            options);
        return CompleteCodeNormalizationResult(
            canonical,
            lexical.SourceForm,
            options.TargetForm,
            macrolanguage.MacrolanguageRelationship);
    }

    internal static LanguageNormalizationResult CompleteCodeNormalizationResult(
        LanguageNormalizationResult result,
        LanguageCodeForm originalSourceForm,
        LanguageCodeForm requestedTargetForm,
        LanguageRelationship? macrolanguageRelationship)
    {
        LanguageNormalizationResult completed =
            PublicResultFactory.CopyLanguageNormalizationResult(result);
        completed.SourceForm = originalSourceForm;
        if (requestedTargetForm != LanguageCodeForm.Unknown &&
            !IsSuccessfulCodeNormalizationStatus(completed.Status))
        {
            completed.TargetForm = requestedTargetForm;
        }

        if (macrolanguageRelationship != null)
        {
            completed.MacrolanguageRelationship =
                PublicResultFactory.CopyLanguageRelationship(macrolanguageRelationship);
        }

        return EnforceSuccessfulCodeNormalizationResult(completed);
    }

    internal static LanguageNormalizationResult ConvertCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return EnforceSuccessfulCodeNormalizationResult(
            ConvertCodeResult(NormalizeCodeLexically(code, options), options));
    }

    internal static LanguageNormalizationResult ConvertCodeResult(
        LanguageNormalizationResult lexical,
        LanguageNormalizationOptions options)
    {
        LanguageCodeForm requestedForm = options.TargetForm == LanguageCodeForm.Unknown
            ? lexical.SourceForm
            : options.TargetForm;
        if (lexical.Status != LanguageNormalizationStatus.Success &&
            lexical.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            LanguageNormalizationResult failure =
                PublicResultFactory.CopyLanguageNormalizationResult(lexical);
            failure.TargetForm = requestedForm;
            return failure;
        }

        if (lexical.Entry == null ||
            lexical.Entry.Status != LanguageCodeStatus.Active ||
            !IsConvertibleCodeForm(lexical.SourceForm))
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.IncompatibleEntity,
                    OriginalInput = lexical.OriginalInput,
                    SourceForm = lexical.SourceForm,
                    TargetForm = requestedForm,
                    Entry = lexical.Entry,
                    AppliedSteps = lexical.AppliedSteps,
                    Warnings = lexical.Warnings,
                    ReplacementCandidates = lexical.ReplacementCandidates
                });
        }

        if (options.TargetForm != LanguageCodeForm.Unknown &&
            !IsConvertibleCodeForm(options.TargetForm))
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.TargetUnavailable,
                    OriginalInput = lexical.OriginalInput,
                    SourceForm = lexical.SourceForm,
                    TargetForm = options.TargetForm,
                    Entry = lexical.Entry,
                    AppliedSteps = lexical.AppliedSteps,
                    Warnings = lexical.Warnings,
                    ReplacementCandidates = lexical.ReplacementCandidates
                });
        }

        CanonicalCodeSelection selection = SelectExactConversionCode(
            lexical.Entry,
            options.TargetForm,
            lexical.SourceForm,
            lexical.Output);
        if (selection.Value == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.TargetUnavailable,
                    OriginalInput = lexical.OriginalInput,
                    SourceForm = lexical.SourceForm,
                    TargetForm = requestedForm,
                    Entry = lexical.Entry,
                    AppliedSteps = lexical.AppliedSteps,
                    Warnings = lexical.Warnings,
                    ReplacementCandidates = lexical.ReplacementCandidates
                });
        }

        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            CopyNormalizationStepsToList(lexical.AppliedSteps);
        bool converted = selection.Value != lexical.Output;
        if (converted)
        {
            steps.Add(LanguageNormalizationStep.ConvertCodeForm);
        }

        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = lexical.Status == LanguageNormalizationStatus.Success || converted
                    ? LanguageNormalizationStatus.Success
                    : LanguageNormalizationStatus.AlreadyCanonical,
                OriginalInput = lexical.OriginalInput,
                Output = selection.Value,
                SourceForm = lexical.SourceForm,
                TargetForm = selection.Form,
                Entry = lexical.Entry,
                AppliedSteps = steps.ToArray(),
                Warnings = lexical.Warnings,
                ReplacementCandidates = lexical.ReplacementCandidates
            });
    }

    internal static CanonicalCodeSelection SelectExactConversionCode(
        LanguageCodeEntry entry,
        LanguageCodeForm requestedForm,
        LanguageCodeForm sourceForm,
        string? sourceValue)
    {
        if (requestedForm == LanguageCodeForm.Unknown)
        {
            return new CanonicalCodeSelection(sourceValue, sourceForm);
        }

        if (requestedForm == LanguageCodeForm.Alpha2)
        {
            return new CanonicalCodeSelection(entry.Alpha2Code, LanguageCodeForm.Alpha2);
        }

        if (requestedForm == LanguageCodeForm.Set2Terminology)
        {
            return new CanonicalCodeSelection(
                entry.Set2TerminologyCode,
                LanguageCodeForm.Set2Terminology);
        }

        if (requestedForm == LanguageCodeForm.Set2Bibliographic)
        {
            return new CanonicalCodeSelection(
                entry.Set2BibliographicCode,
                LanguageCodeForm.Set2Bibliographic);
        }

        if (requestedForm == LanguageCodeForm.Set3)
        {
            return new CanonicalCodeSelection(entry.Set3Code, LanguageCodeForm.Set3);
        }

        return new CanonicalCodeSelection(null, requestedForm);
    }

    internal static bool IsConvertibleCodeForm(LanguageCodeForm form)
    {
        return form == LanguageCodeForm.Alpha2 ||
            form == LanguageCodeForm.Set2Terminology ||
            form == LanguageCodeForm.Set2Bibliographic ||
            form == LanguageCodeForm.Set3;
    }

    internal static LanguageNormalizationResult NormalizeHistoricalCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return EnforceSuccessfulCodeNormalizationResult(
            NormalizeHistoricalCodeResult(
                NormalizeCodeLexically(code, options),
                options,
                RetirementIndex.Data));
    }

    internal static LanguageNormalizationResult NormalizeHistoricalCodeResult(
        LanguageNormalizationResult lexical,
        LanguageNormalizationOptions options,
        RetirementIndexData retirementData)
    {
        if (lexical.Status != LanguageNormalizationStatus.Success &&
            lexical.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(lexical);
        }

        if (lexical.Output == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(lexical);
        }

        CodeIndexEntry? sourceEntry = CodeIndex.FindExact(
            lexical.Output,
            lexical.SourceForm);
        bool deprecated = sourceEntry != null &&
            sourceEntry.Status == LanguageCodeStatus.Deprecated;
        if (deprecated &&
            options.ReplaceDeprecated &&
            sourceEntry != null &&
            sourceEntry.PreferredValue != null)
        {
            return ApplyDeprecatedCodeReplacement(
                lexical,
                sourceEntry.PreferredValue,
                options);
        }

        RetirementChainRow? retirementChain =
            LanguageRetirementEngine.ResolveReplacementChainEntry(
                lexical.Output,
                retirementData);
        bool retiredSource = lexical.SourceForm == LanguageCodeForm.RetiredSet3;
        if (options.ReplaceRetired &&
            retirementChain != null &&
            (retiredSource || deprecated))
        {
            return ApplyRetiredCodeReplacement(
                lexical,
                retirementChain,
                options);
        }

        if (options.ReplaceRetired && retiredSource)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.NoReplacement,
                new string[0]);
        }

        if (options.ReplaceDeprecated && deprecated)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.NoReplacement,
                new string[0]);
        }

        return PublicResultFactory.CopyLanguageNormalizationResult(lexical);
    }

    internal static LanguageNormalizationResult ApplyDeprecatedCodeReplacement(
        LanguageNormalizationResult lexical,
        string preferredValue,
        LanguageNormalizationOptions options)
    {
        LanguageNormalizationResult replacement =
            NormalizeCodeLexically(preferredValue, options);
        if (replacement.Status != LanguageNormalizationStatus.Success &&
            replacement.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return CreateHistoricalFailureResult(
                lexical,
                replacement.Status,
                new string[0]);
        }

        if (replacement.Entry == null ||
            replacement.Entry.Status != LanguageCodeStatus.Active)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.IncompatibleEntity,
                new string[0]);
        }

        if (replacement.Output == lexical.Output)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(lexical);
        }

        return CreateHistoricalSuccessResult(
            lexical,
            replacement,
            LanguageNormalizationStep.ReplaceDeprecated);
    }

    internal static LanguageNormalizationResult ApplyRetiredCodeReplacement(
        LanguageNormalizationResult lexical,
        RetirementChainRow chain,
        LanguageNormalizationOptions options)
    {
        if (chain.Status != LanguageNormalizationStatus.Success)
        {
            return CreateHistoricalFailureResult(
                lexical,
                chain.Status,
                chain.ReplacementCandidates);
        }

        if (chain.FinalCode == null)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.NoReplacement,
                new string[0]);
        }

        CodeIndexEntry? targetEntry = CodeIndex.FindExact(
            chain.FinalCode,
            LanguageCodeForm.Set3);
        if (targetEntry == null)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.NotFound,
                new string[0]);
        }

        if (targetEntry.Status != LanguageCodeStatus.Active)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.IncompatibleEntity,
                new string[0]);
        }

        LanguageNormalizationResult replacement =
            NormalizeCodeLexically(chain.FinalCode, options);
        if (replacement.Status != LanguageNormalizationStatus.Success &&
            replacement.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return CreateHistoricalFailureResult(
                lexical,
                replacement.Status,
                new string[0]);
        }

        if (replacement.Entry == null ||
            replacement.Entry.Status != LanguageCodeStatus.Active)
        {
            return CreateHistoricalFailureResult(
                lexical,
                LanguageNormalizationStatus.IncompatibleEntity,
                new string[0]);
        }

        replacement.TargetForm = LanguageCodeForm.Set3;

        return CreateHistoricalSuccessResult(
            lexical,
            replacement,
            LanguageNormalizationStep.ReplaceRetired);
    }

    internal static LanguageNormalizationResult CreateHistoricalSuccessResult(
        LanguageNormalizationResult lexical,
        LanguageNormalizationResult replacement,
        LanguageNormalizationStep step)
    {
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            CopyNormalizationStepsToList(lexical.AppliedSteps);
        steps.Add(step);
        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = LanguageNormalizationStatus.Success,
                OriginalInput = lexical.OriginalInput,
                Output = replacement.Output,
                SourceForm = lexical.SourceForm,
                TargetForm = replacement.TargetForm,
                Entry = replacement.Entry,
                AppliedSteps = steps.ToArray(),
                Warnings = lexical.Warnings,
                ReplacementCandidates = lexical.ReplacementCandidates
            });
    }

    internal static LanguageNormalizationResult CreateHistoricalFailureResult(
        LanguageNormalizationResult lexical,
        LanguageNormalizationStatus status,
        string[] replacementCandidates)
    {
        LanguageCodeEntry? entry = lexical.Entry;
        if (lexical.Output != null &&
            LanguageCatalogEngine.Data.RetiredEntriesByCode.ContainsKey(lexical.Output))
        {
            entry = PublicResultFactory.CopyLanguageCodeEntry(
                LanguageCatalogEngine.Data.RetiredEntriesByCode[lexical.Output]);
        }

        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = status,
                OriginalInput = lexical.OriginalInput,
                SourceForm = lexical.SourceForm,
                TargetForm = lexical.SourceForm,
                Entry = entry,
                AppliedSteps = lexical.AppliedSteps,
                Warnings = lexical.Warnings,
                ReplacementCandidates = replacementCandidates
            });
    }

    internal static LanguageNormalizationResult NormalizeMacrolanguageCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return EnforceSuccessfulCodeNormalizationResult(
            NormalizeMacrolanguageCodeResult(
                NormalizeCodeLexically(code, options),
                options,
                RelationshipIndex.Data,
                LanguageCatalogEngine.Data));
    }

    internal static LanguageNormalizationResult NormalizeMacrolanguageCodeResult(
        LanguageNormalizationResult normalized,
        LanguageNormalizationOptions options,
        RelationshipIndexData relationships,
        LanguageCatalogData catalog)
    {
        if (normalized.Status != LanguageNormalizationStatus.Success &&
            normalized.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(normalized);
        }

        if (options.MacrolanguagePolicy !=
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(normalized);
        }

        if (normalized.Entry == null ||
            normalized.Entry.Status != LanguageCodeStatus.Active ||
            normalized.Entry.Set3Code == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(normalized);
        }

        string memberCode = normalized.Entry.Set3Code;
        LanguageCodeEntry? parent =
            LanguageRelationshipEngine.GetMacrolanguageForMemberEntries(
                memberCode,
                false,
                relationships,
                catalog);
        if (parent == null ||
            parent.Status != LanguageCodeStatus.Active ||
            parent.Set3Code == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(normalized);
        }

        RelationshipIndexEntry? applied = FindAppliedMacrolanguageRelationship(
            memberCode,
            parent.Set3Code,
            relationships);
        if (applied == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(normalized);
        }

        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            CopyNormalizationStepsToList(normalized.AppliedSteps);
        steps.Add(LanguageNormalizationStep.FoldMacrolanguage);
        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = LanguageNormalizationStatus.Success,
                OriginalInput = normalized.OriginalInput,
                Output = parent.Set3Code,
                SourceForm = normalized.SourceForm,
                TargetForm = LanguageCodeForm.Set3,
                Entry = parent,
                MacrolanguageRelationship = new LanguageRelationship
                {
                    ParentCode = applied.ParentCode,
                    ChildCode = applied.ChildCode,
                    Kind = applied.Kind,
                    SourceSet = PublicResultFactory.ConvertSourceSet(applied.Source),
                    IsRetired = false
                },
                AppliedSteps = steps.ToArray(),
                Warnings = normalized.Warnings,
                ReplacementCandidates = normalized.ReplacementCandidates
            });
    }

    internal static RelationshipIndexEntry? FindAppliedMacrolanguageRelationship(
        string memberCode,
        string parentCode,
        RelationshipIndexData relationships)
    {
        if (!relationships.MacrolanguagesByMember.ContainsKey(memberCode))
        {
            return null;
        }

        RelationshipIndexEntry[] candidates =
            relationships.MacrolanguagesByMember[memberCode];
        int index;
        for (index = 0; index < candidates.Length; index++)
        {
            RelationshipIndexEntry candidate = candidates[index];
            if (candidate.ParentCode == parentCode &&
                candidate.ChildCode == memberCode &&
                candidate.Kind == LanguageRelationshipKind.MacrolanguageMember &&
                candidate.Status == ReconciledRelationshipStatus.Active)
            {
                return RelationshipIndexBuilder.CopyRelationshipEntry(candidate);
            }
        }

        return null;
    }

    internal static LanguageNormalizationResult CanonicalizeActiveCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return EnforceSuccessfulCodeNormalizationResult(
            CanonicalizeActiveCodeResult(
                NormalizeCodeLexically(code, options),
                options));
    }

    internal static LanguageNormalizationResult EnforceSuccessfulCodeNormalizationResult(
        LanguageNormalizationResult result)
    {
        LanguageNormalizationResult checkedResult =
            PublicResultFactory.CopyLanguageNormalizationResult(result);
        if (!IsSuccessfulCodeNormalizationStatus(checkedResult.Status))
        {
            return checkedResult;
        }

        if (LanguageValidationEngine.IsNormalizationOutputValid(checkedResult))
        {
            return checkedResult;
        }

        checkedResult.Status = LanguageNormalizationStatus.IncompatibleEntity;
        checkedResult.Output = null;
        checkedResult.Entry = null;
        return PublicResultFactory.CopyLanguageNormalizationResult(checkedResult);
    }

    internal static bool IsSuccessfulCodeNormalizationStatus(
        LanguageNormalizationStatus status)
    {
        return status == LanguageNormalizationStatus.Success ||
            status == LanguageNormalizationStatus.AlreadyCanonical;
    }

    internal static LanguageNormalizationResult CanonicalizeActiveCodeResult(
        LanguageNormalizationResult lexical,
        LanguageNormalizationOptions options)
    {
        LanguageCodeForm requestedForm = options.TargetForm == LanguageCodeForm.Unknown
            ? lexical.SourceForm
            : options.TargetForm;
        if (lexical.Status != LanguageNormalizationStatus.Success &&
            lexical.Status != LanguageNormalizationStatus.AlreadyCanonical)
        {
            LanguageNormalizationResult failure =
                PublicResultFactory.CopyLanguageNormalizationResult(lexical);
            failure.TargetForm = requestedForm;
            return failure;
        }

        if (lexical.Entry == null || lexical.Entry.Status != LanguageCodeStatus.Active)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.IncompatibleEntity,
                    OriginalInput = lexical.OriginalInput,
                    SourceForm = lexical.SourceForm,
                    TargetForm = requestedForm,
                    Entry = lexical.Entry,
                    AppliedSteps = lexical.AppliedSteps,
                    Warnings = lexical.Warnings,
                    ReplacementCandidates = lexical.ReplacementCandidates
                });
        }

        CanonicalCodeSelection selection = SelectCanonicalActiveCode(
            lexical.Entry,
            options.TargetForm,
            lexical.SourceForm,
            lexical.Output);
        if (selection.Value == null)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.TargetUnavailable,
                    OriginalInput = lexical.OriginalInput,
                    SourceForm = lexical.SourceForm,
                    TargetForm = requestedForm,
                    Entry = lexical.Entry,
                    AppliedSteps = lexical.AppliedSteps,
                    Warnings = lexical.Warnings,
                    ReplacementCandidates = lexical.ReplacementCandidates
                });
        }

        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            CopyNormalizationStepsToList(lexical.AppliedSteps);
        bool converted = selection.Value != lexical.Output;
        if (converted)
        {
            steps.Add(LanguageNormalizationStep.ConvertCodeForm);
        }

        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = lexical.Status == LanguageNormalizationStatus.Success || converted
                    ? LanguageNormalizationStatus.Success
                    : LanguageNormalizationStatus.AlreadyCanonical,
                OriginalInput = lexical.OriginalInput,
                Output = selection.Value,
                SourceForm = lexical.SourceForm,
                TargetForm = selection.Form,
                Entry = lexical.Entry,
                AppliedSteps = steps.ToArray(),
                Warnings = lexical.Warnings,
                ReplacementCandidates = lexical.ReplacementCandidates
            });
    }

    internal static CanonicalCodeSelection SelectCanonicalActiveCode(
        LanguageCodeEntry entry,
        LanguageCodeForm requestedForm,
        LanguageCodeForm sourceForm,
        string? sourceValue)
    {
        if (requestedForm == LanguageCodeForm.Unknown)
        {
            return new CanonicalCodeSelection(sourceValue, sourceForm);
        }

        if (requestedForm == LanguageCodeForm.Alpha2)
        {
            return new CanonicalCodeSelection(entry.Alpha2Code, LanguageCodeForm.Alpha2);
        }

        if (requestedForm == LanguageCodeForm.Set3)
        {
            if (entry.Set3Code != null)
            {
                return new CanonicalCodeSelection(entry.Set3Code, LanguageCodeForm.Set3);
            }

            return new CanonicalCodeSelection(
                entry.Set2TerminologyCode,
                LanguageCodeForm.Set2Terminology);
        }

        if (requestedForm == LanguageCodeForm.Set2Terminology)
        {
            if (entry.Set2TerminologyCode != null)
            {
                return new CanonicalCodeSelection(
                    entry.Set2TerminologyCode,
                    LanguageCodeForm.Set2Terminology);
            }

            return new CanonicalCodeSelection(entry.Set3Code, LanguageCodeForm.Set3);
        }

        if (requestedForm == LanguageCodeForm.Set2Bibliographic)
        {
            return new CanonicalCodeSelection(
                entry.Set2BibliographicCode,
                LanguageCodeForm.Set2Bibliographic);
        }

        return new CanonicalCodeSelection(null, requestedForm);
    }

    internal static global::System.Collections.Generic.List<LanguageNormalizationStep>
        CopyNormalizationStepsToList(LanguageNormalizationStep[] source)
    {
        global::System.Collections.Generic.List<LanguageNormalizationStep> destination =
            new global::System.Collections.Generic.List<LanguageNormalizationStep>();
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination.Add(source[index]);
        }

        return destination;
    }

    internal static LanguageNormalizationResult NormalizeCodeLexically(
        string? code,
        LanguageNormalizationOptions options)
    {
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps =
            new global::System.Collections.Generic.List<LanguageNormalizationStep>();
        if (code == null)
        {
            return CreateInvalidLexicalResult(code, steps);
        }

        string prepared = code;
        if (options.TrimWhitespace)
        {
            string trimmed = prepared.Trim();
            if (trimmed != prepared)
            {
                prepared = trimmed;
                steps.Add(LanguageNormalizationStep.TrimWhitespace);
            }
        }

        if (!AsciiCode.IsLexicallyValid(prepared))
        {
            return CreateInvalidLexicalResult(code, steps);
        }

        if (options.NormalizeCase)
        {
            string lowercase = LowercaseAsciiCode(prepared);
            if (lowercase != prepared)
            {
                prepared = lowercase;
                steps.Add(LanguageNormalizationStep.NormalizeCase);
            }
        }

        CodeIndexEntry[] entries = CodeIndex.FindAll(prepared);
        CodeIndexEntry[] recognized =
            LanguageLookupEngine.SelectPreferredCodeEntries(entries, true);
        if (recognized.Length == 0)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.NotFound,
                    OriginalInput = code,
                    SourceForm = LanguageCodeForm.Unknown,
                    TargetForm = LanguageCodeForm.Unknown,
                    AppliedSteps = steps.ToArray()
                });
        }

        LanguageCodeForm sourceForm = recognized[0].Form;
        LanguageLookupResult lookup = LanguageLookupEngine.BuildCodeLookupResult(
            prepared,
            recognized,
            LanguageCodeForm.Unknown,
            true,
            LanguageCatalogEngine.Data);
        if (lookup.Status == LanguageLookupStatus.Ambiguous)
        {
            return PublicResultFactory.CopyLanguageNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.Ambiguous,
                    OriginalInput = code,
                    SourceForm = sourceForm,
                    TargetForm = sourceForm,
                    AppliedSteps = steps.ToArray()
                });
        }

        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = steps.Count == 0
                    ? LanguageNormalizationStatus.AlreadyCanonical
                    : LanguageNormalizationStatus.Success,
                OriginalInput = code,
                Output = prepared,
                SourceForm = sourceForm,
                TargetForm = sourceForm,
                Entry = lookup.Entry,
                AppliedSteps = steps.ToArray()
            });
    }

    internal static LanguageNormalizationResult CreateInvalidLexicalResult(
        string? code,
        global::System.Collections.Generic.List<LanguageNormalizationStep> steps)
    {
        return PublicResultFactory.CopyLanguageNormalizationResult(
            new LanguageNormalizationResult
            {
                Status = LanguageNormalizationStatus.Invalid,
                OriginalInput = code,
                SourceForm = LanguageCodeForm.Unknown,
                TargetForm = LanguageCodeForm.Unknown,
                AppliedSteps = steps.ToArray()
            });
    }

    internal static string LowercaseAsciiCode(string value)
    {
        char[] characters = value.ToCharArray();
        int index;
        for (index = 0; index < characters.Length; index++)
        {
            characters[index] = AsciiCode.ToLowerAscii(characters[index]);
        }

        return new string(characters);
    }
}
