using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Looks up pinned or latest internal and external code-set editions.</summary>
public class CodeSetProvider : ICodeSetProvider
{
    internal List<RepositorySnapshot> RepositorySnapshots;
    internal List<ExternalCodeCatalog> ExternalCatalogs;

    /// <summary>Initializes a provider from caller-supplied repository snapshots and external catalogues.</summary>
    /// <param name="repositorySnapshots">The available internal code-set repository editions.</param><param name="externalCatalogs">The independently versioned external code-set editions.</param>
    public CodeSetProvider(
        List<RepositorySnapshot> repositorySnapshots,
        List<ExternalCodeCatalog> externalCatalogs)
    {
        if (repositorySnapshots == null)
        {
            throw new ArgumentNullException("repositorySnapshots");
        }

        if (externalCatalogs == null)
        {
            throw new ArgumentNullException("externalCatalogs");
        }

        RepositorySnapshots = new List<RepositorySnapshot>(repositorySnapshots);
        ExternalCatalogs = new List<ExternalCodeCatalog>(externalCatalogs);
    }

    /// <summary>Looks up one exact code in the selected internal or external code-set edition.</summary>
    /// <param name="request">The code-set source, edition policy, value, and active date.</param><returns>The lookup result and selected provenance.</returns>
    public CodeLookupResult LookupCode(CodeLookupRequest? request)
    {
        if (request == null)
        {
            return CreateLookupFailure(
                "code.lookup.request_missing",
                "A code lookup request is required.");
        }

        CodeLookupResult? invalid = ValidateLookupRequest(request);
        if (invalid != null)
        {
            return invalid;
        }

        if (request.Kind == CodeSetKind.Internal)
        {
            return LookupInternalCode(request);
        }

        return LookupExternalCode(request);
    }

    internal static CodeLookupResult? ValidateLookupRequest(CodeLookupRequest request)
    {
        if (request.CodeSetIdentifier == null || request.Value == null ||
            request.Edition == null || request.CodeSetIdentifier.Length == 0 ||
            request.Value.Length == 0)
        {
            return CreateLookupFailure(
                "code.lookup.value_invalid",
                "A code-set identifier and exact code value are required.");
        }

        if (request.Kind != CodeSetKind.Internal && request.Kind != CodeSetKind.External)
        {
            return CreateLookupFailure(
                "code.lookup.kind_invalid",
                "The code-set source kind must be internal or external.");
        }

        if ((request.SelectLatestEdition && request.Edition.Length > 0) ||
            (!request.SelectLatestEdition && request.Edition.Length == 0))
        {
            return CreateLookupFailure(
                "code.lookup.edition_policy_invalid",
                "Select either one pinned edition or the latest-edition policy, but not both.");
        }

        if (request.ActiveOn != null && DateTimeLexical.FormatDate(request.ActiveOn) == null)
        {
            return CreateLookupFailure(
                "code.lookup.active_date_invalid",
                "The active-date filter is not a valid XSD date.");
        }

        return null;
    }

    internal CodeLookupResult LookupInternalCode(CodeLookupRequest request)
    {
        CodeSetProviderSelection selection = SelectRepositorySnapshot(request);
        if (!selection.Success)
        {
            return CreateLookupFailure(selection.ErrorCode, selection.ErrorMessage);
        }

        if (!selection.Found || selection.Snapshot == null)
        {
            return CreateLookupNotFound();
        }

        RepositorySnapshot snapshot = selection.Snapshot;
        if (snapshot.Provenance == null || snapshot.Provenance.Artifact == null ||
            snapshot.CodeSets == null)
        {
            return CreateLookupFailure(
                "code.lookup.catalog_invalid",
                "The selected repository snapshot has invalid code-set provenance or data.");
        }

        CodeLookupResult result = CreateInternalLookupBase(snapshot);
        List<CodeSetDefinition> matches = FindInternalCodeSets(snapshot, request.CodeSetIdentifier);
        if (matches.Count == 0)
        {
            return result;
        }

        if (matches.Count > 1)
        {
            return CreateSelectedLookupFailure(
                result,
                "code.lookup.code_set_ambiguous",
                "The selected repository edition contains multiple matching internal code sets.");
        }

        CodeSetDefinition codeSet = matches[0];
        if (!InternalCodeSetIsEligible(codeSet, request, result))
        {
            return result;
        }

        List<CodeValueDefinition>? values = FindEligibleInternalValues(
            codeSet,
            request,
            result);
        if (values == null)
        {
            return result;
        }

        if (values.Count == 0)
        {
            return result;
        }

        if (values.Count > 1)
        {
            return CreateSelectedLookupFailure(
                result,
                "code.lookup.value_ambiguous",
                "The selected internal code set contains multiple values matching the lookup policy.");
        }

        result.Found = true;
        result.InternalCodeSet = codeSet;
        result.InternalCodeValue = values[0];
        return result;
    }

    internal CodeLookupResult LookupExternalCode(CodeLookupRequest request)
    {
        CodeSetProviderSelection selection = SelectExternalCatalog(request);
        if (!selection.Success)
        {
            return CreateLookupFailure(selection.ErrorCode, selection.ErrorMessage);
        }

        if (!selection.Found || selection.Catalog == null)
        {
            return CreateLookupNotFound();
        }

        ExternalCodeCatalog catalog = selection.Catalog;
        if (catalog.Artifact == null || catalog.CodeSets == null)
        {
            return CreateLookupFailure(
                "code.lookup.catalog_invalid",
                "The selected external catalogue has invalid code-set provenance or data.");
        }

        CodeLookupResult result = CreateExternalLookupBase(catalog);
        List<ExternalCodeSet> matches = FindExternalCodeSets(catalog, request.CodeSetIdentifier);
        if (matches.Count == 0)
        {
            return result;
        }

        if (matches.Count > 1)
        {
            return CreateSelectedLookupFailure(
                result,
                "code.lookup.code_set_ambiguous",
                "The selected external edition contains multiple matching code sets.");
        }

        ExternalCodeSet codeSet = matches[0];
        if (!ExternalCodeSetIsEligible(codeSet, request, result))
        {
            return result;
        }

        List<ExternalCodeValue>? values = FindEligibleExternalValues(
            codeSet,
            request,
            result);
        if (values == null)
        {
            return result;
        }

        if (values.Count == 0)
        {
            return result;
        }

        if (values.Count > 1)
        {
            return CreateSelectedLookupFailure(
                result,
                "code.lookup.value_ambiguous",
                "The selected external code set contains multiple values matching the lookup policy.");
        }

        result.Found = true;
        result.ExternalCodeSet = codeSet;
        result.ExternalCodeValue = values[0];
        return result;
    }

    internal CodeSetProviderSelection SelectRepositorySnapshot(CodeLookupRequest request)
    {
        CodeSetProviderSelection selection = new CodeSetProviderSelection();
        string selectedEdition = request.Edition;
        if (request.SelectLatestEdition)
        {
            selectedEdition = FindLatestRepositoryEdition();
        }

        if (selectedEdition.Length == 0)
        {
            int validationIndex = 0;
            while (validationIndex < RepositorySnapshots.Count)
            {
                RepositorySnapshot? available = RepositorySnapshots[validationIndex];
                if (available == null || available.Edition == null ||
                    available.Edition.Length == 0)
                {
                    selection.ErrorCode = "code.lookup.catalog_invalid";
                    selection.ErrorMessage = "An available repository snapshot has invalid edition data.";
                    return selection;
                }

                validationIndex++;
            }

            selection.Success = true;
            return selection;
        }

        int matchCount = 0;
        int index = 0;
        while (index < RepositorySnapshots.Count)
        {
            RepositorySnapshot? snapshot = RepositorySnapshots[index];
            if (snapshot == null || snapshot.Edition == null || snapshot.Edition.Length == 0)
            {
                selection.ErrorCode = "code.lookup.catalog_invalid";
                selection.ErrorMessage = "An available repository snapshot has invalid edition data.";
                return selection;
            }

            if (snapshot.Edition == selectedEdition)
            {
                selection.Snapshot = snapshot;
                matchCount++;
            }

            index++;
        }

        if (matchCount > 1)
        {
            selection.ErrorCode = "code.lookup.edition_ambiguous";
            selection.ErrorMessage = "Multiple repository snapshots declare the selected edition.";
            return selection;
        }

        selection.Success = true;
        selection.Found = matchCount == 1;
        return selection;
    }

    internal CodeSetProviderSelection SelectExternalCatalog(CodeLookupRequest request)
    {
        CodeSetProviderSelection selection = new CodeSetProviderSelection();
        string selectedEdition = request.Edition;
        if (request.SelectLatestEdition)
        {
            selectedEdition = FindLatestExternalEdition();
        }

        if (selectedEdition.Length == 0)
        {
            int validationIndex = 0;
            while (validationIndex < ExternalCatalogs.Count)
            {
                ExternalCodeCatalog? available = ExternalCatalogs[validationIndex];
                if (available == null || available.Edition == null ||
                    available.Edition.Length == 0)
                {
                    selection.ErrorCode = "code.lookup.catalog_invalid";
                    selection.ErrorMessage = "An available external catalogue has invalid edition data.";
                    return selection;
                }

                validationIndex++;
            }

            selection.Success = true;
            return selection;
        }

        int matchCount = 0;
        int index = 0;
        while (index < ExternalCatalogs.Count)
        {
            ExternalCodeCatalog? catalog = ExternalCatalogs[index];
            if (catalog == null || catalog.Edition == null || catalog.Edition.Length == 0)
            {
                selection.ErrorCode = "code.lookup.catalog_invalid";
                selection.ErrorMessage = "An available external catalogue has invalid edition data.";
                return selection;
            }

            if (catalog.Edition == selectedEdition)
            {
                selection.Catalog = catalog;
                matchCount++;
            }

            index++;
        }

        if (matchCount > 1)
        {
            selection.ErrorCode = "code.lookup.edition_ambiguous";
            selection.ErrorMessage = "Multiple external catalogues declare the selected edition.";
            return selection;
        }

        selection.Success = true;
        selection.Found = matchCount == 1;
        return selection;
    }

    internal string FindLatestRepositoryEdition()
    {
        string edition = string.Empty;
        int index = 0;
        while (index < RepositorySnapshots.Count)
        {
            RepositorySnapshot? snapshot = RepositorySnapshots[index];
            if (snapshot != null && snapshot.Edition != null &&
                (edition.Length == 0 || string.CompareOrdinal(snapshot.Edition, edition) > 0))
            {
                edition = snapshot.Edition;
            }

            index++;
        }

        return edition;
    }

    internal string FindLatestExternalEdition()
    {
        string edition = string.Empty;
        int index = 0;
        while (index < ExternalCatalogs.Count)
        {
            ExternalCodeCatalog? catalog = ExternalCatalogs[index];
            if (catalog != null && catalog.Edition != null &&
                (edition.Length == 0 || string.CompareOrdinal(catalog.Edition, edition) > 0))
            {
                edition = catalog.Edition;
            }

            index++;
        }

        return edition;
    }

    internal static List<CodeSetDefinition> FindInternalCodeSets(
        RepositorySnapshot snapshot,
        string identifier)
    {
        List<CodeSetDefinition> matches = new List<CodeSetDefinition>();
        int index = 0;
        while (index < snapshot.CodeSets.Count)
        {
            CodeSetDefinition? codeSet = snapshot.CodeSets[index];
            if (codeSet != null && codeSet.Kind == CodeSetKind.Internal &&
                (codeSet.Identifier == identifier || codeSet.Name == identifier))
            {
                matches.Add(codeSet);
            }

            index++;
        }

        return matches;
    }

    internal static List<ExternalCodeSet> FindExternalCodeSets(
        ExternalCodeCatalog catalog,
        string identifier)
    {
        List<ExternalCodeSet> matches = new List<ExternalCodeSet>();
        int index = 0;
        while (index < catalog.CodeSets.Count)
        {
            ExternalCodeSet? codeSet = catalog.CodeSets[index];
            if (codeSet != null &&
                (codeSet.Identifier == identifier || codeSet.Name == identifier))
            {
                matches.Add(codeSet);
            }

            index++;
        }

        return matches;
    }

    internal static bool InternalCodeSetIsEligible(
        CodeSetDefinition codeSet,
        CodeLookupRequest request,
        CodeLookupResult result)
    {
        if (codeSet.Registration == null || codeSet.Codes == null ||
            !RegistrationDatesAreValid(codeSet.Registration))
        {
            SetLookupError(
                result,
                "code.lookup.catalog_invalid",
                "The selected internal code set has invalid lifecycle or value data.");
            return false;
        }

        if (!LifecycleDateMatches(
            codeSet.Registration.RegistrationDate,
            codeSet.Registration.RemovalDate,
            request.ActiveOn))
        {
            return false;
        }

        return request.IncludeInactive || !InternalRegistrationIsInactive(codeSet.Registration);
    }

    internal static bool ExternalCodeSetIsEligible(
        ExternalCodeSet codeSet,
        CodeLookupRequest request,
        CodeLookupResult result)
    {
        if (codeSet.Identifier == null || codeSet.Name == null ||
            codeSet.RegistrationStatus == null || codeSet.Values == null ||
            !LifecycleDatesAreValid(codeSet.ActivationDate, codeSet.DeactivationDate, null))
        {
            SetLookupError(
                result,
                "code.lookup.catalog_invalid",
                "The selected external code set has invalid lifecycle or value data.");
            return false;
        }

        if (!LifecycleDateMatches(
            codeSet.ActivationDate,
            codeSet.DeactivationDate,
            request.ActiveOn))
        {
            return false;
        }

        return request.IncludeInactive || !ExternalStatusIsInactive(codeSet.RegistrationStatus);
    }

    internal static List<CodeValueDefinition>? FindEligibleInternalValues(
        CodeSetDefinition codeSet,
        CodeLookupRequest request,
        CodeLookupResult result)
    {
        List<CodeValueDefinition> matches = new List<CodeValueDefinition>();
        int index = 0;
        while (index < codeSet.Codes.Count)
        {
            CodeValueDefinition? value = codeSet.Codes[index];
            if (value == null || value.Value == null || value.Registration == null ||
                !RegistrationDatesAreValid(value.Registration))
            {
                SetLookupError(
                    result,
                    "code.lookup.catalog_invalid",
                    "The selected internal code set contains invalid code-value data.");
                return null;
            }

            if (value.Value == request.Value &&
                LifecycleDateMatches(
                    value.Registration.RegistrationDate,
                    value.Registration.RemovalDate,
                    request.ActiveOn) &&
                (request.IncludeInactive || !InternalRegistrationIsInactive(value.Registration)))
            {
                matches.Add(value);
            }

            index++;
        }

        return matches;
    }

    internal static List<ExternalCodeValue>? FindEligibleExternalValues(
        ExternalCodeSet codeSet,
        CodeLookupRequest request,
        CodeLookupResult result)
    {
        List<ExternalCodeValue> matches = new List<ExternalCodeValue>();
        int index = 0;
        while (index < codeSet.Values.Count)
        {
            ExternalCodeValue? value = codeSet.Values[index];
            if (value == null || value.Value == null || value.RegistrationStatus == null ||
                !LifecycleDatesAreValid(
                    value.ActivationDate,
                    value.DeactivationDate,
                    value.RemovalDate))
            {
                SetLookupError(
                    result,
                    "code.lookup.catalog_invalid",
                    "The selected external code set contains invalid code-value data.");
                return null;
            }

            IsoDateValue? endDate = value.DeactivationDate;
            endDate = SelectEarlierLookupEndDate(endDate, value.RemovalDate);

            if (value.Value == request.Value &&
                LifecycleDateMatches(value.ActivationDate, endDate, request.ActiveOn) &&
                (request.IncludeInactive || !ExternalStatusIsInactive(value.RegistrationStatus)))
            {
                matches.Add(value);
            }

            index++;
        }

        return matches;
    }

    internal static IsoDateValue? SelectEarlierLookupEndDate(
        IsoDateValue? first,
        IsoDateValue? second)
    {
        if (first == null)
        {
            return second;
        }

        if (second == null)
        {
            return first;
        }

        return CompareLookupDates(first, second) <= 0 ? first : second;
    }

    internal static bool RegistrationDatesAreValid(RegistrationMetadata registration)
    {
        return LifecycleDatesAreValid(
            registration.RegistrationDate,
            registration.RemovalDate,
            null);
    }

    internal static bool LifecycleDatesAreValid(
        IsoDateValue? startDate,
        IsoDateValue? endDate,
        IsoDateValue? secondEndDate)
    {
        if ((startDate != null && DateTimeLexical.FormatDate(startDate) == null) ||
            (endDate != null && DateTimeLexical.FormatDate(endDate) == null) ||
            (secondEndDate != null && DateTimeLexical.FormatDate(secondEndDate) == null))
        {
            return false;
        }

        if (startDate != null && endDate != null && CompareLookupDates(startDate, endDate) > 0)
        {
            return false;
        }

        if (startDate != null && secondEndDate != null &&
            CompareLookupDates(startDate, secondEndDate) > 0)
        {
            return false;
        }

        return true;
    }

    internal static bool LifecycleDateMatches(
        IsoDateValue? startDate,
        IsoDateValue? endDate,
        IsoDateValue? activeOn)
    {
        if (activeOn == null)
        {
            return true;
        }

        if (startDate != null && CompareLookupDates(activeOn, startDate) < 0)
        {
            return false;
        }

        return endDate == null || CompareLookupDates(activeOn, endDate) < 0;
    }

    internal static int CompareLookupDates(IsoDateValue left, IsoDateValue right)
    {
        if (left.Year != right.Year)
        {
            return left.Year < right.Year ? -1 : 1;
        }

        if (left.Month != right.Month)
        {
            return left.Month < right.Month ? -1 : 1;
        }

        if (left.Day != right.Day)
        {
            return left.Day < right.Day ? -1 : 1;
        }

        return 0;
    }

    internal static bool InternalRegistrationIsInactive(RegistrationMetadata registration)
    {
        return registration.Status == RegistrationStatus.Obsolete || registration.IsDeprecated;
    }

    internal static bool ExternalStatusIsInactive(string status)
    {
        return status.StartsWith("Obsolete", StringComparison.OrdinalIgnoreCase) ||
            status.StartsWith("Inactive", StringComparison.OrdinalIgnoreCase);
    }

    internal static CodeLookupResult CreateInternalLookupBase(RepositorySnapshot snapshot)
    {
        CodeLookupResult result = new CodeLookupResult();
        result.Success = true;
        result.Artifact = snapshot.Provenance.Artifact;
        result.Edition = snapshot.Edition;
        return result;
    }

    internal static CodeLookupResult CreateExternalLookupBase(ExternalCodeCatalog catalog)
    {
        CodeLookupResult result = new CodeLookupResult();
        result.Success = true;
        result.Artifact = catalog.Artifact;
        result.Edition = catalog.Edition;
        return result;
    }

    internal static CodeLookupResult CreateLookupNotFound()
    {
        CodeLookupResult result = new CodeLookupResult();
        result.Success = true;
        return result;
    }

    internal static CodeLookupResult CreateSelectedLookupFailure(
        CodeLookupResult result,
        string errorCode,
        string errorMessage)
    {
        result.Success = false;
        SetLookupError(result, errorCode, errorMessage);
        return result;
    }

    internal static void SetLookupError(
        CodeLookupResult result,
        string errorCode,
        string errorMessage)
    {
        result.Success = false;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
    }

    internal static CodeLookupResult CreateLookupFailure(
        string errorCode,
        string errorMessage)
    {
        CodeLookupResult result = new CodeLookupResult();
        SetLookupError(result, errorCode, errorMessage);
        return result;
    }
}
