namespace uspto;

internal class CitationGraphMatchResult
{
    internal int Index = -1;
    internal CitationMatchConfidence Confidence = CitationMatchConfidence.None;
    internal string? Basis = null;
}

internal static class CitationGraphInputComposer
{
    internal static CitationGraphInputSet Build(OfficeActionCitationResponse officeActionCitations, PatentsViewCitationResponse patentsViewCitations)
    {
        if (officeActionCitations == null)
        {
            throw new ValidationException("Office Action citations are required.", "officeActionCitations", "REQUIRED", null);
        }

        if (patentsViewCitations == null)
        {
            throw new ValidationException("PatentsView citations are required.", "patentsViewCitations", "REQUIRED", null);
        }

        if (officeActionCitations.Citations == null)
        {
            throw new ValidationException("The Office Action citation collection is required.", "officeActionCitations.Citations", "REQUIRED", null);
        }

        if (patentsViewCitations.Citations == null)
        {
            throw new ValidationException("The PatentsView citation collection is required.", "patentsViewCitations.Citations", "REQUIRED", null);
        }

        CitationGraphInputSet result = CreateResultProvenance(officeActionCitations, patentsViewCitations);
        global::System.Collections.Generic.List<CitationGraphInput> inputs = new global::System.Collections.Generic.List<CitationGraphInput>();
        CitationGraphEvidence[] researchEvidence = CreatePatentsViewEvidence(patentsViewCitations.Citations);
        bool[] usedResearch = new bool[researchEvidence.Length];
        int index;
        for (index = 0; index < officeActionCitations.Citations.Length; index += 1)
        {
            OfficeActionCitation citation = officeActionCitations.Citations[index];
            if (citation == null)
            {
                throw new ValidationException("An Office Action citation cannot be null.", "officeActionCitations.Citations[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "REQUIRED", null);
            }

            CitationGraphEvidence officialEvidence = CreateOfficeActionEvidence(citation);
            CitationGraphMatchResult match = FindBestResearchMatch(officialEvidence, researchEvidence, usedResearch);
            if (match.Index >= 0)
            {
                usedResearch[match.Index] = true;
                inputs.Add(CreateMatchedInput(officialEvidence, researchEvidence[match.Index], match));
                result.MatchedCount += 1;
            }
            else
            {
                inputs.Add(CreateUnmatchedInput(officialEvidence));
                result.UnmatchedOfficeActionCount += 1;
            }
        }

        for (index = 0; index < researchEvidence.Length; index += 1)
        {
            if (!usedResearch[index])
            {
                inputs.Add(CreateUnmatchedInput(researchEvidence[index]));
                result.UnmatchedPatentsViewCount += 1;
            }
        }

        result.Inputs = inputs.ToArray();
        result.Count = result.Inputs.Length;
        result.LoadedSourceCount = 2;
        result.IsComplete = true;
        return result;
    }

    internal static CitationGraphInputSet CombineAvailableSources(CitationGraphAggregationRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A citation graph aggregation request is required.", "request", "REQUIRED", null);
        }

        if (request.OfficeActionCitations != null && request.OfficeActionError != null)
        {
            throw new ValidationException("Office Action citation data and an Office Action failure cannot both be supplied.", "OfficeActionError", "CONFLICTING_SOURCE_OUTCOME", null);
        }

        if (request.PatentsViewCitations != null && request.PatentsViewError != null)
        {
            throw new ValidationException("PatentsView citation data and a PatentsView failure cannot both be supplied.", "PatentsViewError", "CONFLICTING_SOURCE_OUTCOME", null);
        }

        OfficeActionCitationResponse official = request.OfficeActionCitations ?? new OfficeActionCitationResponse();
        PatentsViewCitationResponse research = request.PatentsViewCitations ?? new PatentsViewCitationResponse();
        CitationGraphInputSet result = Build(official, research);
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();
        result.Provenance.FieldSources = new FieldDataProvenance[0];
        result.LoadedSourceCount = 0;
        result.FailedSourceCount = 0;
        if (request.OfficeActionCitations != null)
        {
            result.LoadedSourceCount += 1;
            ProvenanceLabeler.AddPatentField(result.Provenance, "Inputs.Evidence.OfficeAction", request.OfficeActionCitations.Provenance);
        }
        else
        {
            errors.Add(CopySourceError("officeAction", request.OfficeActionError, "OFFICE_ACTION_SOURCE_UNAVAILABLE"));
            result.FailedSourceCount += 1;
        }

        if (request.PatentsViewCitations != null)
        {
            result.LoadedSourceCount += 1;
            ProvenanceLabeler.AddPatentField(result.Provenance, "Inputs.Evidence.PatentsView", request.PatentsViewCitations.Metadata.Provenance);
        }
        else
        {
            errors.Add(CopySourceError("patentsView", request.PatentsViewError, "PATENTSVIEW_SOURCE_UNAVAILABLE"));
            result.FailedSourceCount += 1;
        }

        result.Errors = errors.ToArray();
        result.IsComplete = result.FailedSourceCount == 0;
        result.HasPartialSuccess = result.Count > 0 && result.FailedSourceCount > 0;
        return result;
    }

    internal static SectionError CopySourceError(string section, SectionError? source, string fallbackCode)
    {
        SectionError result = new SectionError();
        result.Section = section;
        if (source == null)
        {
            result.ExceptionType = "ServiceUnavailableException";
            result.Message = "The citation source was not available.";
            result.ErrorCode = fallbackCode;
            result.IsRetryable = true;
            return result;
        }

        result.ExceptionType = source.ExceptionType;
        result.Message = source.Message;
        result.ErrorCode = source.ErrorCode;
        result.Service = source.Service;
        result.StatusCode = source.StatusCode;
        result.HasStatusCode = source.HasStatusCode;
        result.RequestIdentifier = source.RequestIdentifier;
        result.IsRetryable = source.IsRetryable;
        return result;
    }

    internal static CitationGraphInputSet CreateResultProvenance(OfficeActionCitationResponse officeActionCitations, PatentsViewCitationResponse patentsViewCitations)
    {
        CitationGraphInputSet result = new CitationGraphInputSet();
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.DatasetIdentifier = "Office Action and PatentsView citation graph inputs";
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        ProvenanceLabeler.AddPatentField(result.Provenance, "Inputs.Evidence.OfficeAction", officeActionCitations.Provenance);
        ProvenanceLabeler.AddPatentField(result.Provenance, "Inputs.Evidence.PatentsView", patentsViewCitations.Metadata.Provenance);
        return result;
    }

    internal static CitationGraphEvidence[] CreatePatentsViewEvidence(PatentsViewCitation[] citations)
    {
        CitationGraphEvidence[] result = new CitationGraphEvidence[citations.Length];
        int index;
        for (index = 0; index < citations.Length; index += 1)
        {
            PatentsViewCitation citation = citations[index];
            if (citation == null)
            {
                throw new ValidationException("A PatentsView citation cannot be null.", "patentsViewCitations.Citations[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "REQUIRED", null);
            }

            result[index] = CreatePatentsViewEvidenceItem(citation);
        }

        return result;
    }

    internal static CitationGraphEvidence CreateOfficeActionEvidence(OfficeActionCitation citation)
    {
        CitationGraphEvidence result = new CitationGraphEvidence();
        result.Source = citation.Provenance.Source;
        result.Direction = CitationDirection.Outgoing;
        result.SourceIdentifier = citation.ApplicationNumber;
        result.TargetIdentifier = ChooseOfficeActionTarget(citation);
        result.CitationCategory = ChooseOfficeActionCategory(citation);
        result.ReferenceText = citation.ReferenceIdentifier;
        result.DocumentCategory = ClassifyOfficeActionCitation(citation);
        if (citation.CitedIdentifier.IsNonPatentLiterature)
        {
            result.NonPatentLiterature = citation.ReferenceIdentifier;
        }

        result.OfficeActionCitation = citation;
        result.Provenance = ProvenanceLabeler.CopyPatent(citation.Provenance);
        return result;
    }

    internal static CitationGraphEvidence CreatePatentsViewEvidenceItem(PatentsViewCitation citation)
    {
        CitationGraphEvidence result = new CitationGraphEvidence();
        result.Source = citation.Provenance.Source;
        result.Direction = CitationDirection.Outgoing;
        result.SourceIdentifier = citation.PatentIdentifier ?? citation.DocumentNumber;
        result.TargetIdentifier = ChoosePatentsViewTarget(citation);
        result.CitationCategory = citation.CitationCategory;
        result.ReferenceText = ChoosePatentsViewReference(citation);
        result.DocumentCategory = ClassifyPatentsViewCitation(citation);
        if (global::System.String.Equals(citation.CitationType, "other", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.NonPatentLiterature = citation.ReferenceText;
        }

        result.PatentsViewCitation = citation;
        result.Provenance = ProvenanceLabeler.CopyPatent(citation.Provenance);
        return result;
    }

    internal static string? ChooseOfficeActionTarget(OfficeActionCitation citation)
    {
        if (!global::System.String.IsNullOrWhiteSpace(citation.CitedIdentifier.CanonicalPatentIdentifier))
        {
            return citation.CitedIdentifier.CanonicalPatentIdentifier;
        }

        if (citation.CitedIdentifier.IsPatentDocument && !global::System.String.IsNullOrWhiteSpace(citation.ParsedReferenceIdentifier))
        {
            return citation.ParsedReferenceIdentifier.Trim();
        }

        if (citation.CitedIdentifier.IsPatentDocument && !global::System.String.IsNullOrWhiteSpace(citation.ReferenceIdentifier))
        {
            return citation.ReferenceIdentifier.Trim();
        }

        return null;
    }

    internal static string? ChoosePatentsViewTarget(PatentsViewCitation citation)
    {
        string? candidate = citation.CitedPatentIdentifier;
        if (global::System.String.IsNullOrWhiteSpace(candidate))
        {
            candidate = citation.CitedDocumentNumber;
        }

        if (global::System.String.IsNullOrWhiteSpace(candidate))
        {
            candidate = citation.CitationNumber;
        }

        if (global::System.String.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (global::System.String.Equals(citation.CitationType, "us_patent", global::System.StringComparison.OrdinalIgnoreCase) || global::System.String.Equals(citation.CitationType, "us_application", global::System.StringComparison.OrdinalIgnoreCase))
        {
            IdentifierNormalizationResult normalized = IdentifierNormalization.NormalizePatent(candidate);
            if (normalized.IsValid)
            {
                return normalized.CanonicalNumber;
            }
        }

        return candidate.Trim();
    }

    internal static string? ChoosePatentsViewReference(PatentsViewCitation citation)
    {
        if (!global::System.String.IsNullOrWhiteSpace(citation.ReferenceText))
        {
            return citation.ReferenceText;
        }

        if (!global::System.String.IsNullOrWhiteSpace(citation.CitedPatentIdentifier))
        {
            return citation.CitedPatentIdentifier;
        }

        if (!global::System.String.IsNullOrWhiteSpace(citation.CitedDocumentNumber))
        {
            return citation.CitedDocumentNumber;
        }

        return citation.CitationNumber;
    }

    internal static string? ChooseOfficeActionCategory(OfficeActionCitation citation)
    {
        if (citation.HasApplicantCitedExaminerReferenceIndicator && citation.ApplicantCitedExaminerReferenceIndicator && citation.HasExaminerCitedReferenceIndicator && citation.ExaminerCitedReferenceIndicator)
        {
            return "applicantAndExaminer";
        }

        if (citation.HasApplicantCitedExaminerReferenceIndicator && citation.ApplicantCitedExaminerReferenceIndicator)
        {
            return "applicant";
        }

        if (citation.HasExaminerCitedReferenceIndicator && citation.ExaminerCitedReferenceIndicator)
        {
            return "examiner";
        }

        return citation.ActionTypeCategory;
    }

    internal static CitationDocumentCategory ClassifyOfficeActionCitation(OfficeActionCitation citation)
    {
        if (citation.CitedIdentifier.IsNonPatentLiterature)
        {
            return CitationDocumentCategory.NonPatentLiterature;
        }

        if (citation.CitedIdentifier.PatentIdentifierKind == IdentifierKind.PatentGrant)
        {
            return CitationDocumentCategory.UnitedStatesPatent;
        }

        if (citation.CitedIdentifier.PatentIdentifierKind == IdentifierKind.PatentPublication)
        {
            return CitationDocumentCategory.UnitedStatesApplication;
        }

        if (citation.CitedIdentifier.IsPatentDocument && CitedIdentifierNormalizer.LooksLikeForeignPatentDocument(citation.ReferenceIdentifier))
        {
            return CitationDocumentCategory.ForeignPatent;
        }

        if (citation.CitedIdentifier.IsPatentDocument)
        {
            return CitationDocumentCategory.Other;
        }

        return CitationDocumentCategory.Unknown;
    }

    internal static CitationDocumentCategory ClassifyPatentsViewCitation(PatentsViewCitation citation)
    {
        if (global::System.String.Equals(citation.CitationType, "us_patent", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CitationDocumentCategory.UnitedStatesPatent;
        }

        if (global::System.String.Equals(citation.CitationType, "us_application", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CitationDocumentCategory.UnitedStatesApplication;
        }

        if (global::System.String.Equals(citation.CitationType, "foreign", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CitationDocumentCategory.ForeignPatent;
        }

        if (global::System.String.Equals(citation.CitationType, "other", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CitationDocumentCategory.Other;
        }

        return CitationDocumentCategory.Unknown;
    }

    internal static CitationGraphMatchResult FindBestResearchMatch(CitationGraphEvidence officialEvidence, CitationGraphEvidence[] researchEvidence, bool[] usedResearch)
    {
        CitationGraphMatchResult best = new CitationGraphMatchResult();
        int index;
        for (index = 0; index < researchEvidence.Length; index += 1)
        {
            if (!usedResearch[index])
            {
                CitationGraphMatchResult candidate = CompareEvidence(officialEvidence, researchEvidence[index]);
                if ((int)candidate.Confidence > (int)best.Confidence)
                {
                    best = candidate;
                    best.Index = index;
                }
            }
        }

        return best;
    }

    internal static CitationGraphMatchResult CompareEvidence(CitationGraphEvidence officialEvidence, CitationGraphEvidence researchEvidence)
    {
        CitationGraphMatchResult result = new CitationGraphMatchResult();
        if (!global::System.String.IsNullOrWhiteSpace(officialEvidence.TargetIdentifier) && !global::System.String.IsNullOrWhiteSpace(researchEvidence.TargetIdentifier))
        {
            string officialTarget = officialEvidence.TargetIdentifier.Trim();
            string researchTarget = researchEvidence.TargetIdentifier.Trim();
            if (global::System.String.Equals(officialTarget, researchTarget, global::System.StringComparison.Ordinal))
            {
                result.Confidence = CitationMatchConfidence.Exact;
                result.Basis = "canonicalIdentifier";
                return result;
            }

            string officialKey = NormalizeIdentifierForComparison(officialTarget);
            string researchKey = NormalizeIdentifierForComparison(researchTarget);
            if (officialKey.Length > 0 && global::System.String.Equals(officialKey, researchKey, global::System.StringComparison.Ordinal))
            {
                result.Confidence = CitationMatchConfidence.High;
                result.Basis = "normalizedIdentifier";
                return result;
            }
        }

        if (!global::System.String.IsNullOrWhiteSpace(officialEvidence.NonPatentLiterature) && !global::System.String.IsNullOrWhiteSpace(researchEvidence.NonPatentLiterature))
        {
            string officialText = officialEvidence.NonPatentLiterature.Trim();
            string researchText = researchEvidence.NonPatentLiterature.Trim();
            if (global::System.String.Equals(officialText, researchText, global::System.StringComparison.Ordinal))
            {
                result.Confidence = CitationMatchConfidence.Exact;
                result.Basis = "nonPatentLiterature";
                return result;
            }

            if (global::System.String.Equals(officialText, researchText, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result.Confidence = CitationMatchConfidence.High;
                result.Basis = "caseInsensitiveNonPatentLiterature";
                return result;
            }

            if (global::System.String.Equals(NormalizeReferenceText(officialText), NormalizeReferenceText(researchText), global::System.StringComparison.Ordinal))
            {
                result.Confidence = CitationMatchConfidence.Medium;
                result.Basis = "normalizedNonPatentLiterature";
            }
        }

        return result;
    }

    internal static string NormalizeIdentifierForComparison(string value)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(value.Length);
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))
            {
                builder.Append(global::System.Char.ToUpperInvariant(character));
            }
        }

        string normalized = builder.ToString();
        if (normalized.StartsWith("US", global::System.StringComparison.Ordinal))
        {
            normalized = normalized.Substring(2);
        }

        if (normalized.Length > 2 && IsKindCode(normalized.Substring(normalized.Length - 2)))
        {
            normalized = normalized.Substring(0, normalized.Length - 2);
        }

        return normalized;
    }

    internal static bool IsKindCode(string value)
    {
        return (value[0] >= 'A' && value[0] <= 'Z') && (value[1] >= '0' && value[1] <= '9');
    }

    internal static string NormalizeReferenceText(string value)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(value.Length);
        bool pendingSpace = false;
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsWhiteSpace(character))
            {
                if (builder.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(global::System.Char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    internal static CitationGraphInput CreateMatchedInput(CitationGraphEvidence officialEvidence, CitationGraphEvidence researchEvidence, CitationGraphMatchResult match)
    {
        CitationGraphInput result = CreateInputCore(officialEvidence);
        result.Evidence = new CitationGraphEvidence[] { officialEvidence, researchEvidence };
        result.MatchConfidence = match.Confidence;
        result.MatchBasis = match.Basis;
        if (result.TargetIdentifier == null)
        {
            result.TargetIdentifier = researchEvidence.TargetIdentifier;
        }

        if (result.NonPatentLiterature == null)
        {
            result.NonPatentLiterature = researchEvidence.NonPatentLiterature;
        }

        if (result.Direction != researchEvidence.Direction)
        {
            result.Direction = CitationDirection.Unknown;
        }

        result.DocumentCategory = CombineDocumentCategories(result.DocumentCategory, researchEvidence.DocumentCategory);

        ProvenanceLabeler.AddPatentField(result.Provenance, "Evidence.PatentsView", researchEvidence.Provenance);
        return result;
    }

    internal static CitationDocumentCategory CombineDocumentCategories(CitationDocumentCategory first, CitationDocumentCategory second)
    {
        if (first == second)
        {
            return first;
        }

        if (first == CitationDocumentCategory.Unknown)
        {
            return second;
        }

        if (second == CitationDocumentCategory.Unknown)
        {
            return first;
        }

        if ((first == CitationDocumentCategory.NonPatentLiterature && second == CitationDocumentCategory.Other) || (first == CitationDocumentCategory.Other && second == CitationDocumentCategory.NonPatentLiterature))
        {
            return CitationDocumentCategory.NonPatentLiterature;
        }

        return CitationDocumentCategory.Unknown;
    }

    internal static CitationGraphInput CreateUnmatchedInput(CitationGraphEvidence evidence)
    {
        CitationGraphInput result = CreateInputCore(evidence);
        result.Evidence = new CitationGraphEvidence[] { evidence };
        return result;
    }

    internal static CitationGraphInput CreateInputCore(CitationGraphEvidence evidence)
    {
        CitationGraphInput result = new CitationGraphInput();
        result.TargetIdentifier = evidence.TargetIdentifier;
        result.Direction = evidence.Direction;
        result.DocumentCategory = evidence.DocumentCategory;
        result.NonPatentLiterature = evidence.NonPatentLiterature;
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.DatasetIdentifier = "Citation graph input";
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        string fieldPath = "Evidence.OfficeAction";
        if (evidence.PatentsViewCitation != null)
        {
            fieldPath = "Evidence.PatentsView";
        }

        ProvenanceLabeler.AddPatentField(result.Provenance, fieldPath, evidence.Provenance);
        return result;
    }
}
