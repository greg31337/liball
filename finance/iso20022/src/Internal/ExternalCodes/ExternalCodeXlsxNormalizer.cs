using System;
using System.Collections.Generic;
using System.Globalization;

namespace iso20022;

internal static class ExternalCodeXlsxNormalizer
{
    internal static ExternalCodeCatalog? NormalizeWorkbook(
        ExternalCodeXlsxWorkbook workbook,
        ExternalCodeXlsxSheet allCodes,
        ExternalCodeXlsxSheet definitions,
        ExternalCodeImportRequest request,
        ExternalCodeArtifactOpenResult opened,
        ExternalCodeImportResult result)
    {
        ExternalCodeCatalog catalog = new ExternalCodeCatalog();
        catalog.Artifact = opened.Artifact;
        catalog.Edition = ExternalCodeSetImporter.ResolveEdition(request, opened.EntryName);
        catalog.PublicationDate = opened.Artifact.PublicationDate;
        catalog.SchemaIdentifier = ExternalCodeXlsxReader.SpreadsheetNamespace;
        catalog.SchemaVersion = request.Artifact.Coordinate.Version;
        catalog.SourceFormat = "XLSX";
        if (catalog.Edition.Length == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.edition_missing",
                "The external-code edition is not declared and could not be inferred from the XLSX entry name.");
            return null;
        }

        if (!NormalizeCodeSetDefinitions(definitions, catalog, request, result) ||
            !NormalizeCodeValues(workbook, allCodes, catalog, request, result))
        {
            return null;
        }

        ExternalCodeSetImporter.AddDiagnostic(
            result,
            request,
            "external-code.xlsx.validity_unavailable",
            "The official workbook does not declare activation or deactivation dates; those normalized fields remain absent.");
        return catalog;
    }

    internal static bool NormalizeCodeSetDefinitions(
        ExternalCodeXlsxSheet definitions,
        ExternalCodeCatalog catalog,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        if (definitions.Rows.Count == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.definition_header",
                "The CodeSetsDefinition worksheet is empty.");
            return false;
        }

        string[] headerNames = new string[]
        {
            "Code Set",
            "Code Set Definition",
            "Status"
        };
        int[]? columns = FindRequiredHeaderColumns(
            definitions.Rows[0],
            headerNames,
            "definition",
            result);
        if (columns == null)
        {
            return false;
        }

        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        int rowIndex = 1;
        while (rowIndex < definitions.Rows.Count)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            ExternalCodeXlsxRow row = definitions.Rows[rowIndex];
            if (!SpreadsheetRowIsBlank(row))
            {
                string identifier = ReadSpreadsheetCellValue(row, columns[0]);
                if (identifier.Length == 0 || identifier.Length > request.Limits.MaxTextLength ||
                    !identifiers.Add(identifier))
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.definition_identifier",
                        "A workbook code-set identifier is empty, duplicated, or too long.");
                    return false;
                }

                if (catalog.CodeSets.Count >= request.Limits.MaxSchemaCount)
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.schema_limit",
                        "The workbook exceeds the configured code-set limit.");
                    return false;
                }

                ExternalCodeSet codeSet = new ExternalCodeSet();
                codeSet.Identifier = identifier;
                codeSet.Name = identifier;
                codeSet.Definition = ReadSpreadsheetCellValue(row, columns[1]);
                codeSet.RegistrationStatus = ReadSpreadsheetCellValue(row, columns[2]);
                catalog.CodeSets.Add(codeSet);
            }

            rowIndex++;
        }

        if (catalog.CodeSets.Count == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.definitions",
                "The CodeSetsDefinition worksheet does not contain code-set definitions.");
            return false;
        }

        return true;
    }

    internal static bool NormalizeCodeValues(
        ExternalCodeXlsxWorkbook workbook,
        ExternalCodeXlsxSheet allCodes,
        ExternalCodeCatalog catalog,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        if (allCodes.Rows.Count == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.code_header",
                "The AllCodeSets worksheet is empty.");
            return false;
        }

        string[] headerNames = new string[]
        {
            "Code Set",
            "Code Value",
            "Code Name",
            "Code Definition",
            "Requester",
            "Status",
            "Last Update",
            "Creation Date",
            "Replaced By",
            "MappingStandards 1",
            "MappingValue 1",
            "MappingStandards 2",
            "MappingValue 2",
            "Additional Information"
        };
        int[]? columns = FindRequiredHeaderColumns(
            allCodes.Rows[0],
            headerNames,
            "code",
            result);
        if (columns == null)
        {
            return false;
        }

        int valueCount = 0;
        int rowIndex = 1;
        while (rowIndex < allCodes.Rows.Count)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            ExternalCodeXlsxRow row = allCodes.Rows[rowIndex];
            if (!SpreadsheetRowIsBlank(row))
            {
                string identifier = ReadSpreadsheetCellValue(row, columns[0]);
                ExternalCodeSet? codeSet = FindNormalizedCodeSet(catalog.CodeSets, identifier);
                if (identifier.Length == 0 || codeSet == null)
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.code_set_reference",
                        "A workbook code row has an empty or unknown code-set identifier.");
                    return false;
                }

                ExternalCodeValue? value = NormalizeCodeValueRow(
                    workbook,
                    row,
                    columns,
                    request,
                    result);
                if (value == null)
                {
                    return false;
                }

                if (codeSet.Values.Count >= request.Limits.MaxCollectionCount ||
                    valueCount >= request.Limits.MaxElementCount - catalog.CodeSets.Count)
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.code_limit",
                        "The workbook exceeds the configured code-value limit.");
                    return false;
                }

                codeSet.Values.Add(value);
                valueCount++;
            }

            rowIndex++;
        }

        if (valueCount == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.codes",
                "The AllCodeSets worksheet does not contain code values.");
            return false;
        }

        return true;
    }

    internal static ExternalCodeValue? NormalizeCodeValueRow(
        ExternalCodeXlsxWorkbook workbook,
        ExternalCodeXlsxRow row,
        int[] columns,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        string code = ReadSpreadsheetCellValue(row, columns[1]);
        if (code.Length == 0 || code.Length > request.Limits.MaxTextLength)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.code_value",
                "A workbook code value is empty or exceeds the configured text limit.");
            return null;
        }

        ExternalCodeValue value = new ExternalCodeValue();
        value.Value = code;
        value.Name = ReadSpreadsheetCellValue(row, columns[2]);
        value.Definition = ReadSpreadsheetCellValue(row, columns[3]);
        value.Requester = ReadSpreadsheetCellValue(row, columns[4]);
        value.RegistrationStatus = ReadSpreadsheetCellValue(row, columns[5]);
        value.LastUpdatedDate = ReadSpreadsheetDate(
            FindSpreadsheetCell(row, columns[6]),
            workbook.Date1904,
            "last-updated",
            result);
        if (result.ErrorCode.Length > 0)
        {
            return null;
        }

        value.CreationDate = ReadSpreadsheetDate(
            FindSpreadsheetCell(row, columns[7]),
            workbook.Date1904,
            "creation",
            result);
        if (result.ErrorCode.Length > 0)
        {
            return null;
        }

        value.ReplacedBy = ReadSpreadsheetCellValue(row, columns[8]);
        AddSpreadsheetMapping(
            value,
            ReadSpreadsheetCellValue(row, columns[9]),
            ReadSpreadsheetCellValue(row, columns[10]));
        AddSpreadsheetMapping(
            value,
            ReadSpreadsheetCellValue(row, columns[11]),
            ReadSpreadsheetCellValue(row, columns[12]));
        value.AdditionalInformation = ReadSpreadsheetCellValue(row, columns[13]);
        value.RemovalDate = ReadRemovalDate(value.RegistrationStatus, result);
        if (result.ErrorCode.Length > 0)
        {
            return null;
        }

        return value;
    }

    internal static int[]? FindRequiredHeaderColumns(
        ExternalCodeXlsxRow row,
        string[] headerNames,
        string worksheetKind,
        ExternalCodeImportResult result)
    {
        int[] columns = new int[headerNames.Length];
        int headerIndex = 0;
        while (headerIndex < headerNames.Length)
        {
            int column = FindSpreadsheetHeaderColumn(row, headerNames[headerIndex]);
            if (column < 0)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx." + worksheetKind + "_header",
                    "A required " + worksheetKind + " worksheet header is missing or duplicated: " +
                    headerNames[headerIndex] + ".");
                return null;
            }

            columns[headerIndex] = column;
            headerIndex++;
        }

        return columns;
    }

    internal static int FindSpreadsheetHeaderColumn(
        ExternalCodeXlsxRow row,
        string headerName)
    {
        int found = -1;
        int index = 0;
        while (index < row.Cells.Count)
        {
            if (row.Cells[index].Value == headerName)
            {
                if (found >= 0)
                {
                    return -2;
                }

                found = row.Cells[index].ColumnIndex;
            }

            index++;
        }

        return found;
    }

    internal static ExternalCodeXlsxCell? FindSpreadsheetCell(
        ExternalCodeXlsxRow row,
        int columnIndex)
    {
        int index = 0;
        while (index < row.Cells.Count)
        {
            if (row.Cells[index].ColumnIndex == columnIndex)
            {
                return row.Cells[index];
            }

            index++;
        }

        return null;
    }

    internal static string ReadSpreadsheetCellValue(
        ExternalCodeXlsxRow row,
        int columnIndex)
    {
        ExternalCodeXlsxCell? cell = FindSpreadsheetCell(row, columnIndex);
        if (cell == null)
        {
            return string.Empty;
        }

        return cell.Value;
    }

    internal static bool SpreadsheetRowIsBlank(ExternalCodeXlsxRow row)
    {
        int index = 0;
        while (index < row.Cells.Count)
        {
            if (row.Cells[index].Value.Length > 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static ExternalCodeSet? FindNormalizedCodeSet(
        List<ExternalCodeSet> codeSets,
        string identifier)
    {
        int index = 0;
        while (index < codeSets.Count)
        {
            if (codeSets[index].Identifier == identifier)
            {
                return codeSets[index];
            }

            index++;
        }

        return null;
    }

    internal static void AddSpreadsheetMapping(
        ExternalCodeValue value,
        string standard,
        string mappedValue)
    {
        if (standard.Length == 0 && mappedValue.Length == 0)
        {
            return;
        }

        value.MappingStandards.Add(standard);
        value.MappingValues.Add(mappedValue);
    }

    internal static IsoDateValue? ReadSpreadsheetDate(
        ExternalCodeXlsxCell? cell,
        bool date1904,
        string fieldName,
        ExternalCodeImportResult result)
    {
        if (cell == null || cell.Value.Length == 0)
        {
            return null;
        }

        if (cell.Type == "s" || cell.Type == "inlineStr" || cell.Type == "str")
        {
            IsoDateValue? textDate = DateTimeLexical.ParseDate(cell.Value.AsSpan());
            if (textDate == null)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.date",
                    "A workbook " + fieldName + " date is not a valid XSD date.");
            }

            return textDate;
        }

        if (cell.Type.Length > 0 && cell.Type != "n")
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.date_type",
                "A workbook " + fieldName + " date uses an unsupported cell type.");
            return null;
        }

        IsoDecimalValue? numeric = DecimalLexical.ParseDecimal(cell.Value.AsSpan());
        if (numeric == null || numeric.Value != decimal.Truncate(numeric.Value) ||
            numeric.Value < decimal.Zero || numeric.Value > int.MaxValue)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.date_serial",
                "A workbook " + fieldName + " date is not a whole Excel serial date.");
            return null;
        }

        int serial = (int)numeric.Value;
        DateTime origin;
        int dayOffset;
        if (date1904)
        {
            origin = new DateTime(1904, 1, 1);
            dayOffset = serial;
        }
        else
        {
            if (serial <= 0)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.date_serial",
                    "A workbook 1900-system date must use a positive serial value.");
                return null;
            }

            origin = new DateTime(1899, 12, 31);
            dayOffset = serial;
            if (serial >= 60)
            {
                dayOffset--;
            }
        }

        TimeSpan available = DateTime.MaxValue - origin;
        if (dayOffset < 0 || dayOffset > available.Days)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.date_range",
                "A workbook " + fieldName + " date is outside the supported Gregorian range.");
            return null;
        }

        DateTime date = origin.AddDays(dayOffset);
        IsoDateValue value = new IsoDateValue();
        value.Year = date.Year;
        value.Month = date.Month;
        value.Day = date.Day;
        value.LexicalMode = DateTimeLexicalMode.Preserve;
        value.LexicalValue = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return value;
    }

    internal static IsoDateValue? ReadRemovalDate(
        string registrationStatus,
        ExternalCodeImportResult result)
    {
        if (!registrationStatus.StartsWith("Obsolete [", StringComparison.Ordinal) ||
            !registrationStatus.EndsWith("]", StringComparison.Ordinal))
        {
            return null;
        }

        string lexical = registrationStatus.Substring(10, registrationStatus.Length - 11).Trim();
        IsoDateValue? date = DateTimeLexical.ParseDate(lexical.AsSpan());
        if (date == null)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.removal_date",
                "An obsolete workbook status contains an invalid removal date.");
        }

        return date;
    }
}
