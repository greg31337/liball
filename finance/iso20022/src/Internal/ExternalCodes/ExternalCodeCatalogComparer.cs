using System;
using System.Collections.Generic;

namespace iso20022;

internal static class ExternalCodeCatalogComparer
{
    internal const int SetNameField = 1;
    internal const int SetDefinitionField = 2;
    internal const int SetRegistrationStatusField = 3;
    internal const int SetPublicationDateField = 4;
    internal const int SetActivationDateField = 5;
    internal const int SetDeactivationDateField = 6;
    internal const int ValueDefinitionField = 1;
    internal const int ValueNameField = 2;
    internal const int ValueRequesterField = 3;
    internal const int ValueRegistrationStatusField = 4;
    internal const int ValueLastUpdatedDateField = 5;
    internal const int ValueCreationDateField = 6;
    internal const int ValueActivationDateField = 7;
    internal const int ValueDeactivationDateField = 8;
    internal const int ValueRemovalDateField = 9;
    internal const int ValueReplacedByField = 10;
    internal const int ValueAdditionalInformationField = 11;

    internal static ExternalCodeCatalogComparisonResult CompareCatalogs(
        ExternalCodeCatalogComparisonRequest request)
    {
        ExternalCodeCatalogComparisonResult result = new ExternalCodeCatalogComparisonResult();
        request.CancellationToken.ThrowIfCancellationRequested();
        if (request.MaxDisagreementCount <= 0)
        {
            return CreateComparisonFailure(
                "external-code.compare.limit_invalid",
                "The external-code disagreement limit must be positive.");
        }

        if (request.JsonCatalog == null || request.XsdCatalog == null ||
            request.XlsxCatalog == null)
        {
            return CreateComparisonFailure(
                "external-code.compare.catalog_missing",
                "JSON, XSD, and XLSX catalogues are all required for comparison.");
        }

        if (!ValidateComparisonCatalog(request.JsonCatalog, "JSON", result) ||
            !ValidateComparisonCatalog(request.XsdCatalog, "XSD", result) ||
            !ValidateComparisonCatalog(request.XlsxCatalog, "XLSX", result))
        {
            return result;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        AddCoverageDiagnostics(result);
        result.Success = true;
        result.Equivalent = true;
        if (request.JsonCatalog.Edition != request.XsdCatalog.Edition ||
            request.JsonCatalog.Edition != request.XlsxCatalog.Edition)
        {
            ExternalCodeDisagreement edition = new ExternalCodeDisagreement();
            edition.Kind = ExternalCodeDisagreementKind.Edition;
            edition.Code = "external-code.compare.edition";
            edition.FieldName = "Edition";
            edition.JsonPresent = true;
            edition.JsonValue = request.JsonCatalog.Edition;
            edition.XsdPresent = true;
            edition.XsdValue = request.XsdCatalog.Edition;
            edition.XlsxPresent = true;
            edition.XlsxValue = request.XlsxCatalog.Edition;
            AddComparisonDisagreement(result, request, edition);
            return result;
        }

        result.Edition = request.JsonCatalog.Edition;
        List<string> identifiers = BuildCodeSetIdentifierUnion(
            request.JsonCatalog,
            request.XsdCatalog,
            request.XlsxCatalog);
        int identifierIndex = 0;
        while (identifierIndex < identifiers.Count)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            string identifier = identifiers[identifierIndex];
            ExternalCodeSet? jsonSet = FindComparisonCodeSet(request.JsonCatalog, identifier);
            ExternalCodeSet? xsdSet = FindComparisonCodeSet(request.XsdCatalog, identifier);
            ExternalCodeSet? xlsxSet = FindComparisonCodeSet(request.XlsxCatalog, identifier);
            result.ComparedCodeSetCount++;
            CompareCodeSetPresence(
                identifier,
                jsonSet,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareCodeSetFields(
                identifier,
                jsonSet,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareCodeSetValues(
                identifier,
                jsonSet,
                xsdSet,
                xlsxSet,
                request,
                result);
            identifierIndex++;
        }

        return result;
    }

    internal static bool ValidateComparisonCatalog(
        ExternalCodeCatalog catalog,
        string expectedFormat,
        ExternalCodeCatalogComparisonResult result)
    {
        if (!string.Equals(catalog.SourceFormat, expectedFormat, StringComparison.OrdinalIgnoreCase))
        {
            SetComparisonError(
                result,
                "external-code.compare.format",
                "Each comparison input must declare its expected JSON, XSD, or XLSX source format.");
            return false;
        }

        if (catalog.Edition == null || catalog.Edition.Length == 0 ||
            catalog.CodeSets == null)
        {
            SetComparisonError(
                result,
                "external-code.compare.catalog_invalid",
                "A comparison catalogue has missing edition or code-set data.");
            return false;
        }

        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        int setIndex = 0;
        while (setIndex < catalog.CodeSets.Count)
        {
            ExternalCodeSet? codeSet = catalog.CodeSets[setIndex];
            if (codeSet == null || codeSet.Identifier == null || codeSet.Identifier.Length == 0 ||
                codeSet.Name == null || codeSet.Definition == null ||
                codeSet.RegistrationStatus == null || codeSet.Pattern == null ||
                codeSet.Values == null || !identifiers.Add(codeSet.Identifier) ||
                !ComparisonDateIsValid(codeSet.PublicationDate) ||
                !ComparisonDateIsValid(codeSet.ActivationDate) ||
                !ComparisonDateIsValid(codeSet.DeactivationDate))
            {
                SetComparisonError(
                    result,
                    "external-code.compare.catalog_invalid",
                    "A comparison catalogue contains invalid or duplicated code-set data.");
                return false;
            }

            int valueIndex = 0;
            while (valueIndex < codeSet.Values.Count)
            {
                ExternalCodeValue? value = codeSet.Values[valueIndex];
                if (!ValidateComparisonValue(value))
                {
                    SetComparisonError(
                        result,
                        "external-code.compare.catalog_invalid",
                        "A comparison catalogue contains invalid code-value data.");
                    return false;
                }

                valueIndex++;
            }

            setIndex++;
        }

        return true;
    }

    internal static bool ValidateComparisonValue(ExternalCodeValue? value)
    {
        if (value == null || value.Value == null || value.Value.Length == 0 ||
            value.Name == null || value.Definition == null || value.Requester == null ||
            value.RegistrationStatus == null || value.ReplacedBy == null ||
            value.AdditionalInformation == null || value.MappingStandards == null ||
            value.MappingValues == null ||
            value.MappingStandards.Count != value.MappingValues.Count ||
            !ComparisonDateIsValid(value.LastUpdatedDate) ||
            !ComparisonDateIsValid(value.CreationDate) ||
            !ComparisonDateIsValid(value.ActivationDate) ||
            !ComparisonDateIsValid(value.DeactivationDate) ||
            !ComparisonDateIsValid(value.RemovalDate))
        {
            return false;
        }

        int index = 0;
        while (index < value.MappingStandards.Count)
        {
            if (value.MappingStandards[index] == null || value.MappingValues[index] == null)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool ComparisonDateIsValid(IsoDateValue? value)
    {
        return value == null || DateTimeLexical.FormatDate(value) != null;
    }

    internal static void AddCoverageDiagnostics(ExternalCodeCatalogComparisonResult result)
    {
        result.DiagnosticCodes.Add("external-code.compare.coverage.json_lifecycle");
        result.DiagnosticMessages.Add(
            "JSON does not publish code lifecycle, requester, replacement, or additional-information fields; those fields are compared between XSD and XLSX only.");
        result.DiagnosticCodes.Add("external-code.compare.coverage.xlsx_facets");
        result.DiagnosticMessages.Add(
            "XLSX does not publish JSON Schema or XSD lexical facets; length and pattern fields are compared between JSON and XSD only.");
        result.DiagnosticCodes.Add("external-code.compare.coverage.mappings");
        result.DiagnosticMessages.Add(
            "Mapping columns are published only by XLSX and are retained but cannot be cross-checked against JSON or XSD.");
    }

    internal static List<string> BuildCodeSetIdentifierUnion(
        ExternalCodeCatalog json,
        ExternalCodeCatalog xsd,
        ExternalCodeCatalog xlsx)
    {
        List<string> identifiers = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        AddCatalogIdentifiers(json, identifiers, seen);
        AddCatalogIdentifiers(xsd, identifiers, seen);
        AddCatalogIdentifiers(xlsx, identifiers, seen);
        return identifiers;
    }

    internal static void AddCatalogIdentifiers(
        ExternalCodeCatalog catalog,
        List<string> identifiers,
        HashSet<string> seen)
    {
        int index = 0;
        while (index < catalog.CodeSets.Count)
        {
            string identifier = catalog.CodeSets[index].Identifier;
            if (seen.Add(identifier))
            {
                identifiers.Add(identifier);
            }

            index++;
        }
    }

    internal static ExternalCodeSet? FindComparisonCodeSet(
        ExternalCodeCatalog catalog,
        string identifier)
    {
        int index = 0;
        while (index < catalog.CodeSets.Count)
        {
            if (catalog.CodeSets[index].Identifier == identifier)
            {
                return catalog.CodeSets[index];
            }

            index++;
        }

        return null;
    }

    internal static void CompareCodeSetPresence(
        string identifier,
        ExternalCodeSet? jsonSet,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        if (jsonSet != null && xsdSet != null && xlsxSet != null)
        {
            return;
        }

        ExternalCodeDisagreement disagreement = new ExternalCodeDisagreement();
        disagreement.Kind = ExternalCodeDisagreementKind.CodeSetPresence;
        disagreement.Code = "external-code.compare.code_set_presence";
        disagreement.CodeSetIdentifier = identifier;
        disagreement.FieldName = "Identifier";
        disagreement.JsonPresent = jsonSet != null;
        disagreement.JsonValue = jsonSet == null ? string.Empty : identifier;
        disagreement.XsdPresent = xsdSet != null;
        disagreement.XsdValue = xsdSet == null ? string.Empty : identifier;
        disagreement.XlsxPresent = xlsxSet != null;
        disagreement.XlsxValue = xlsxSet == null ? string.Empty : identifier;
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static void CompareCodeSetFields(
        string identifier,
        ExternalCodeSet? jsonSet,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        if (jsonSet != null && xsdSet != null && xlsxSet != null)
        {
            CompareThreeSetField(
                identifier,
                "Name",
                SetNameField,
                jsonSet,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareThreeSetField(
                identifier,
                "Definition",
                SetDefinitionField,
                jsonSet,
                xsdSet,
                xlsxSet,
                request,
                result);
        }

        if (xsdSet != null && xlsxSet != null)
        {
            CompareTwoSetField(
                identifier,
                "RegistrationStatus",
                SetRegistrationStatusField,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareTwoSetField(
                identifier,
                "PublicationDate",
                SetPublicationDateField,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareTwoSetField(
                identifier,
                "ActivationDate",
                SetActivationDateField,
                xsdSet,
                xlsxSet,
                request,
                result);
            CompareTwoSetField(
                identifier,
                "DeactivationDate",
                SetDeactivationDateField,
                xsdSet,
                xlsxSet,
                request,
                result);
        }

        if (jsonSet != null && xsdSet != null)
        {
            CompareFacetField(
                identifier,
                "MinimumLength",
                FormatMinimumFacet(jsonSet),
                FormatMinimumFacet(xsdSet),
                request,
                result);
            CompareFacetField(
                identifier,
                "MaximumLength",
                FormatMaximumFacet(jsonSet),
                FormatMaximumFacet(xsdSet),
                request,
                result);
            CompareFacetField(
                identifier,
                "Pattern",
                jsonSet.Pattern,
                xsdSet.Pattern,
                request,
                result);
        }
    }

    internal static void CompareThreeSetField(
        string identifier,
        string fieldName,
        int fieldKind,
        ExternalCodeSet jsonSet,
        ExternalCodeSet xsdSet,
        ExternalCodeSet xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        string jsonValue = ReadComparisonSetField(jsonSet, fieldKind);
        string xsdValue = ReadComparisonSetField(xsdSet, fieldKind);
        string xlsxValue = ReadComparisonSetField(xlsxSet, fieldKind);
        if (jsonValue == xsdValue && jsonValue == xlsxValue)
        {
            return;
        }

        ExternalCodeDisagreement disagreement = CreateFieldDisagreement(
            identifier,
            string.Empty,
            fieldName);
        disagreement.JsonPresent = true;
        disagreement.JsonValue = jsonValue;
        disagreement.XsdPresent = true;
        disagreement.XsdValue = xsdValue;
        disagreement.XlsxPresent = true;
        disagreement.XlsxValue = xlsxValue;
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static void CompareTwoSetField(
        string identifier,
        string fieldName,
        int fieldKind,
        ExternalCodeSet xsdSet,
        ExternalCodeSet xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        string xsdValue = ReadComparisonSetField(xsdSet, fieldKind);
        string xlsxValue = ReadComparisonSetField(xlsxSet, fieldKind);
        if (xsdValue == xlsxValue)
        {
            return;
        }

        ExternalCodeDisagreement disagreement = CreateFieldDisagreement(
            identifier,
            string.Empty,
            fieldName);
        disagreement.XsdPresent = true;
        disagreement.XsdValue = xsdValue;
        disagreement.XlsxPresent = true;
        disagreement.XlsxValue = xlsxValue;
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static void CompareFacetField(
        string identifier,
        string fieldName,
        string jsonValue,
        string xsdValue,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        if (jsonValue == xsdValue)
        {
            return;
        }

        ExternalCodeDisagreement disagreement = CreateFieldDisagreement(
            identifier,
            string.Empty,
            fieldName);
        disagreement.JsonPresent = true;
        disagreement.JsonValue = jsonValue;
        disagreement.XsdPresent = true;
        disagreement.XsdValue = xsdValue;
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static string ReadComparisonSetField(ExternalCodeSet codeSet, int fieldKind)
    {
        switch (fieldKind)
        {
            case SetNameField:
                return NormalizeComparisonText(codeSet.Name);
            case SetDefinitionField:
                return NormalizeComparisonText(codeSet.Definition);
            case SetRegistrationStatusField:
                return NormalizeComparisonText(codeSet.RegistrationStatus);
            case SetPublicationDateField:
                return FormatComparisonDate(codeSet.PublicationDate);
            case SetActivationDateField:
                return FormatComparisonDate(codeSet.ActivationDate);
            case SetDeactivationDateField:
                return FormatComparisonDate(codeSet.DeactivationDate);
            default:
                return string.Empty;
        }
    }

    internal static string FormatMinimumFacet(ExternalCodeSet codeSet)
    {
        if (!codeSet.HasMinimumLength)
        {
            return string.Empty;
        }

        return IntegerLexical.FormatInt32(codeSet.MinimumLength);
    }

    internal static string FormatMaximumFacet(ExternalCodeSet codeSet)
    {
        if (!codeSet.HasMaximumLength)
        {
            return string.Empty;
        }

        return IntegerLexical.FormatInt32(codeSet.MaximumLength);
    }

    internal static void CompareCodeSetValues(
        string identifier,
        ExternalCodeSet? jsonSet,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        List<string> codeValues = BuildCodeValueUnion(jsonSet, xsdSet, xlsxSet);
        int codeIndex = 0;
        while (codeIndex < codeValues.Count)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            string code = codeValues[codeIndex];
            int jsonCount = CountComparisonCodeValue(jsonSet, code);
            int xsdCount = CountComparisonCodeValue(xsdSet, code);
            int xlsxCount = CountComparisonCodeValue(xlsxSet, code);
            result.ComparedCodeValueCount++;
            if (jsonCount != xsdCount || jsonCount != xlsxCount)
            {
                ExternalCodeDisagreement multiplicity = new ExternalCodeDisagreement();
                multiplicity.Kind = ExternalCodeDisagreementKind.CodeValueMultiplicity;
                multiplicity.Code = "external-code.compare.code_value_multiplicity";
                multiplicity.CodeSetIdentifier = identifier;
                multiplicity.CodeValue = code;
                multiplicity.FieldName = "OccurrenceCount";
                multiplicity.JsonPresent = jsonCount > 0;
                multiplicity.JsonValue = IntegerLexical.FormatInt32(jsonCount);
                multiplicity.XsdPresent = xsdCount > 0;
                multiplicity.XsdValue = IntegerLexical.FormatInt32(xsdCount);
                multiplicity.XlsxPresent = xlsxCount > 0;
                multiplicity.XlsxValue = IntegerLexical.FormatInt32(xlsxCount);
                AddComparisonDisagreement(result, request, multiplicity);
            }

            if (jsonCount > 0 && xsdCount > 0 && xlsxCount > 0)
            {
                CompareThreeValueField(
                    identifier,
                    code,
                    "Definition",
                    ValueDefinitionField,
                    jsonSet,
                    xsdSet,
                    xlsxSet,
                    request,
                    result);
            }

            if (xsdCount > 0 && xlsxCount > 0)
            {
                CompareTwoValueField(identifier, code, "Name", ValueNameField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "Requester", ValueRequesterField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "RegistrationStatus", ValueRegistrationStatusField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "LastUpdatedDate", ValueLastUpdatedDateField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "CreationDate", ValueCreationDateField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "ActivationDate", ValueActivationDateField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "DeactivationDate", ValueDeactivationDateField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "RemovalDate", ValueRemovalDateField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "ReplacedBy", ValueReplacedByField, xsdSet, xlsxSet, request, result);
                CompareTwoValueField(identifier, code, "AdditionalInformation", ValueAdditionalInformationField, xsdSet, xlsxSet, request, result);
            }

            codeIndex++;
        }
    }

    internal static List<string> BuildCodeValueUnion(
        ExternalCodeSet? jsonSet,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet)
    {
        List<string> values = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        AddCodeValuesToUnion(jsonSet, values, seen);
        AddCodeValuesToUnion(xsdSet, values, seen);
        AddCodeValuesToUnion(xlsxSet, values, seen);
        return values;
    }

    internal static void AddCodeValuesToUnion(
        ExternalCodeSet? codeSet,
        List<string> values,
        HashSet<string> seen)
    {
        if (codeSet == null)
        {
            return;
        }

        int index = 0;
        while (index < codeSet.Values.Count)
        {
            string value = codeSet.Values[index].Value;
            if (seen.Add(value))
            {
                values.Add(value);
            }

            index++;
        }
    }

    internal static int CountComparisonCodeValue(ExternalCodeSet? codeSet, string code)
    {
        if (codeSet == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < codeSet.Values.Count)
        {
            if (codeSet.Values[index].Value == code)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal static void CompareThreeValueField(
        string identifier,
        string code,
        string fieldName,
        int fieldKind,
        ExternalCodeSet? jsonSet,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        List<string> jsonValues = BuildComparisonValueFieldGroup(jsonSet, code, fieldKind);
        List<string> xsdValues = BuildComparisonValueFieldGroup(xsdSet, code, fieldKind);
        List<string> xlsxValues = BuildComparisonValueFieldGroup(xlsxSet, code, fieldKind);
        if (ComparisonGroupsEqual(jsonValues, xsdValues) &&
            ComparisonGroupsEqual(jsonValues, xlsxValues))
        {
            return;
        }

        ExternalCodeDisagreement disagreement = CreateFieldDisagreement(identifier, code, fieldName);
        disagreement.JsonPresent = true;
        disagreement.JsonValue = FormatComparisonGroup(jsonValues);
        disagreement.XsdPresent = true;
        disagreement.XsdValue = FormatComparisonGroup(xsdValues);
        disagreement.XlsxPresent = true;
        disagreement.XlsxValue = FormatComparisonGroup(xlsxValues);
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static void CompareTwoValueField(
        string identifier,
        string code,
        string fieldName,
        int fieldKind,
        ExternalCodeSet? xsdSet,
        ExternalCodeSet? xlsxSet,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeCatalogComparisonResult result)
    {
        List<string> xsdValues = BuildComparisonValueFieldGroup(xsdSet, code, fieldKind);
        List<string> xlsxValues = BuildComparisonValueFieldGroup(xlsxSet, code, fieldKind);
        if (ComparisonGroupsEqual(xsdValues, xlsxValues))
        {
            return;
        }

        ExternalCodeDisagreement disagreement = CreateFieldDisagreement(identifier, code, fieldName);
        disagreement.XsdPresent = true;
        disagreement.XsdValue = FormatComparisonGroup(xsdValues);
        disagreement.XlsxPresent = true;
        disagreement.XlsxValue = FormatComparisonGroup(xlsxValues);
        AddComparisonDisagreement(result, request, disagreement);
    }

    internal static List<string> BuildComparisonValueFieldGroup(
        ExternalCodeSet? codeSet,
        string code,
        int fieldKind)
    {
        List<string> values = new List<string>();
        if (codeSet == null)
        {
            return values;
        }

        int index = 0;
        while (index < codeSet.Values.Count)
        {
            ExternalCodeValue value = codeSet.Values[index];
            if (value.Value == code)
            {
                values.Add(ReadComparisonValueField(value, fieldKind));
            }

            index++;
        }

        values.Sort(StringComparer.Ordinal);
        return values;
    }

    internal static string ReadComparisonValueField(ExternalCodeValue value, int fieldKind)
    {
        switch (fieldKind)
        {
            case ValueDefinitionField:
                return NormalizeComparisonText(value.Definition);
            case ValueNameField:
                return NormalizeComparisonText(value.Name);
            case ValueRequesterField:
                return NormalizeComparisonText(value.Requester);
            case ValueRegistrationStatusField:
                return NormalizeComparisonText(value.RegistrationStatus);
            case ValueLastUpdatedDateField:
                return FormatComparisonDate(value.LastUpdatedDate);
            case ValueCreationDateField:
                return FormatComparisonDate(value.CreationDate);
            case ValueActivationDateField:
                return FormatComparisonDate(value.ActivationDate);
            case ValueDeactivationDateField:
                return FormatComparisonDate(value.DeactivationDate);
            case ValueRemovalDateField:
                return FormatComparisonDate(value.RemovalDate);
            case ValueReplacedByField:
                return NormalizeComparisonText(value.ReplacedBy);
            case ValueAdditionalInformationField:
                return NormalizeComparisonText(value.AdditionalInformation);
            default:
                return string.Empty;
        }
    }

    internal static bool ComparisonGroupsEqual(List<string> first, List<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static string FormatComparisonGroup(List<string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        if (values.Count == 1)
        {
            return values[0];
        }

        string result = "[";
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                result = result + " | ";
            }

            result = result + values[index];
            index++;
        }

        return result + "]";
    }

    internal static ExternalCodeDisagreement CreateFieldDisagreement(
        string identifier,
        string code,
        string fieldName)
    {
        ExternalCodeDisagreement disagreement = new ExternalCodeDisagreement();
        disagreement.Kind = ExternalCodeDisagreementKind.FieldValue;
        disagreement.Code = "external-code.compare.field_value";
        disagreement.CodeSetIdentifier = identifier;
        disagreement.CodeValue = code;
        disagreement.FieldName = fieldName;
        return disagreement;
    }

    internal static string FormatComparisonDate(IsoDateValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        string? formatted = DateTimeLexical.FormatDate(value);
        if (formatted == null)
        {
            return string.Empty;
        }

        return formatted;
    }

    internal static string NormalizeComparisonText(string value)
    {
        return value.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    internal static void AddComparisonDisagreement(
        ExternalCodeCatalogComparisonResult result,
        ExternalCodeCatalogComparisonRequest request,
        ExternalCodeDisagreement disagreement)
    {
        result.Equivalent = false;
        if (result.Disagreements.Count < request.MaxDisagreementCount)
        {
            result.Disagreements.Add(disagreement);
        }
        else
        {
            result.Truncated = true;
        }
    }

    internal static void SetComparisonError(
        ExternalCodeCatalogComparisonResult result,
        string errorCode,
        string errorMessage)
    {
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
    }

    internal static ExternalCodeCatalogComparisonResult CreateComparisonFailure(
        string errorCode,
        string errorMessage)
    {
        ExternalCodeCatalogComparisonResult result = new ExternalCodeCatalogComparisonResult();
        SetComparisonError(result, errorCode, errorMessage);
        return result;
    }
}
