using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class ExternalCodeXlsxReader
{
    internal const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    internal const string OfficeRelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    internal static ExternalCodeCatalog? ReadWorkbookCatalog(
        byte[] content,
        ExternalCodeImportRequest request,
        ExternalCodeArtifactOpenResult opened,
        ExternalCodeImportResult result)
    {
        using (MemoryStream source = new MemoryStream(content, false))
        using (ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, false))
        {
            if (!ValidateWorkbookPackage(archive, request, result))
            {
                return null;
            }

            ZipArchiveEntry? contentTypesEntry = FindWorkbookEntry(archive, "[Content_Types].xml");
            ZipArchiveEntry? workbookEntry = FindWorkbookEntry(archive, "xl/workbook.xml");
            ZipArchiveEntry? relationshipsEntry = FindWorkbookEntry(
                archive,
                "xl/_rels/workbook.xml.rels");
            if (contentTypesEntry == null || workbookEntry == null || relationshipsEntry == null)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.package",
                    "The XLSX package is missing required workbook parts.");
                return null;
            }

            byte[]? workbookBytes = ReadWorkbookEntryBytes(workbookEntry, request, result);
            byte[]? relationshipBytes = ReadWorkbookEntryBytes(relationshipsEntry, request, result);
            if (workbookBytes == null || relationshipBytes == null)
            {
                return null;
            }

            ExternalCodeXlsxWorkbook? workbook = ReadWorkbookPart(
                workbookBytes,
                request,
                result);
            List<ExternalCodeXlsxRelationship>? relationships = ReadWorkbookRelationships(
                relationshipBytes,
                request,
                result);
            if (workbook == null || relationships == null ||
                !ResolveWorkbookPaths(workbook, relationships, result))
            {
                return null;
            }

            ZipArchiveEntry? allCodesEntry = FindWorkbookEntry(
                archive,
                workbook.AllCodeSetsPath);
            ZipArchiveEntry? definitionsEntry = FindWorkbookEntry(
                archive,
                workbook.CodeSetDefinitionsPath);
            if (allCodesEntry == null || definitionsEntry == null)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.worksheet_missing",
                    "The XLSX package is missing the AllCodeSets or CodeSetsDefinition worksheet.");
                return null;
            }

            List<string> sharedStrings = new List<string>();
            ZipArchiveEntry? sharedStringsEntry = FindWorkbookEntry(
                archive,
                "xl/sharedStrings.xml");
            if (sharedStringsEntry != null)
            {
                byte[]? sharedBytes = ReadWorkbookEntryBytes(sharedStringsEntry, request, result);
                if (sharedBytes == null)
                {
                    return null;
                }

                List<string>? parsedSharedStrings = ReadSharedStringsPart(
                    sharedBytes,
                    request,
                    result);
                if (parsedSharedStrings == null)
                {
                    return null;
                }

                sharedStrings = parsedSharedStrings;
            }

            byte[]? allCodesBytes = ReadWorkbookEntryBytes(allCodesEntry, request, result);
            byte[]? definitionsBytes = ReadWorkbookEntryBytes(definitionsEntry, request, result);
            if (allCodesBytes == null || definitionsBytes == null)
            {
                return null;
            }

            ExternalCodeXlsxSheet? allCodes = ReadWorksheetPart(
                allCodesBytes,
                sharedStrings,
                request,
                result);
            ExternalCodeXlsxSheet? definitions = ReadWorksheetPart(
                definitionsBytes,
                sharedStrings,
                request,
                result);
            if (allCodes == null || definitions == null)
            {
                return null;
            }

            return ExternalCodeXlsxNormalizer.NormalizeWorkbook(
                workbook,
                allCodes,
                definitions,
                request,
                opened,
                result);
        }
    }

    internal static bool ValidateWorkbookPackage(
        ZipArchive archive,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        if (archive.Entries.Count > request.MaxArchiveEntryCount)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.entry_count",
                "The XLSX package exceeds the configured entry-count limit.");
            return false;
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        long totalExpandedBytes = 0;
        int index = 0;
        while (index < archive.Entries.Count)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            ZipArchiveEntry entry = archive.Entries[index];
            if (!RepositoryArtifactOpener.IsSafeEntryName(entry.FullName) ||
                !names.Add(entry.FullName))
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.entry_path",
                    "The XLSX package contains an unsafe or duplicated entry path.");
                return false;
            }

            if (!RepositoryArtifactOpener.IsDirectoryEntry(entry.FullName))
            {
                if (entry.Length > request.MaxArchiveEntryBytes)
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.entry_size",
                        "An XLSX package entry exceeds the configured byte limit.");
                    return false;
                }

                if (totalExpandedBytes > request.MaxArchiveExpandedBytes - entry.Length)
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.total_size",
                        "The XLSX package exceeds the configured expanded-byte limit.");
                    return false;
                }

                totalExpandedBytes = totalExpandedBytes + entry.Length;
                if (!ExternalCodeArtifactOpener.CompressionRatioIsAllowed(
                    entry,
                    request.MaxArchiveCompressionRatio))
                {
                    ExternalCodeSetImporter.SetImportError(
                        result,
                        "external-code.xlsx.ratio",
                        "An XLSX package entry exceeds the configured compression ratio.");
                    return false;
                }
            }

            index++;
        }

        return true;
    }

    internal static ZipArchiveEntry? FindWorkbookEntry(ZipArchive archive, string name)
    {
        int index = 0;
        while (index < archive.Entries.Count)
        {
            ZipArchiveEntry entry = archive.Entries[index];
            if (entry.FullName == name)
            {
                return entry;
            }

            index++;
        }

        return null;
    }

    internal static byte[]? ReadWorkbookEntryBytes(
        ZipArchiveEntry entry,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        using (Stream stream = entry.Open())
        {
            BoundedArtifactCopyResult copied = ExternalCodeArtifactOpener.CopyArchiveEntry(
                stream,
                request);
            if (!copied.Success || copied.Bytes.LongLength != entry.Length)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.entry_read",
                    "An XLSX package entry could not be read within the configured limits.");
                return null;
            }

            return copied.Bytes;
        }
    }

    internal static ExternalCodeXlsxWorkbook? ReadWorkbookPart(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        ExternalCodeXlsxWorkbook workbook = new ExternalCodeXlsxWorkbook();
        bool sawRoot = false;
        MessageReadOptions options = CreateWorkbookXmlOptions(request);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(bytes, options))
        {
            while (reader.ReadNext())
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                if (reader.GetNodeType() != XmlNodeType.Element)
                {
                    continue;
                }

                string localName = reader.GetLocalName();
                string namespaceUri = reader.GetNamespaceUri();
                if (reader.GetDepth() == 0)
                {
                    if (localName != "workbook" || namespaceUri != SpreadsheetNamespace)
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.workbook_root",
                            "The XLSX workbook part has an unexpected root element.");
                        return null;
                    }

                    sawRoot = true;
                }
                else if (namespaceUri == SpreadsheetNamespace && localName == "workbookPr")
                {
                    string date1904 = ReadWorkbookAttribute(reader, "date1904", string.Empty);
                    workbook.Date1904 = date1904 == "1" ||
                        string.Equals(date1904, "true", StringComparison.OrdinalIgnoreCase);
                }
                else if (namespaceUri == SpreadsheetNamespace && localName == "sheet")
                {
                    string sheetName = ReadWorkbookAttribute(reader, "name", string.Empty);
                    string relationship = ReadWorkbookAttribute(
                        reader,
                        "id",
                        OfficeRelationshipNamespace);
                    if (sheetName == "AllCodeSets")
                    {
                        workbook.AllCodeSetsRelationship = relationship;
                    }
                    else if (sheetName == "CodeSetsDefinition")
                    {
                        workbook.CodeSetDefinitionsRelationship = relationship;
                    }
                }
            }
        }

        if (!sawRoot || workbook.AllCodeSetsRelationship.Length == 0 ||
            workbook.CodeSetDefinitionsRelationship.Length == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.workbook_sheets",
                "The workbook does not declare the required external-code worksheets.");
            return null;
        }

        return workbook;
    }

    internal static List<ExternalCodeXlsxRelationship>? ReadWorkbookRelationships(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        List<ExternalCodeXlsxRelationship> relationships = new List<ExternalCodeXlsxRelationship>();
        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        bool sawRoot = false;
        MessageReadOptions options = CreateWorkbookXmlOptions(request);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(bytes, options))
        {
            while (reader.ReadNext())
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                if (reader.GetNodeType() != XmlNodeType.Element)
                {
                    continue;
                }

                if (reader.GetDepth() == 0)
                {
                    if (reader.GetLocalName() != "Relationships" ||
                        reader.GetNamespaceUri() != PackageRelationshipNamespace)
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.relationship_root",
                            "The workbook relationship part has an unexpected root element.");
                        return null;
                    }

                    sawRoot = true;
                }
                else if (reader.GetLocalName() == "Relationship" &&
                    reader.GetNamespaceUri() == PackageRelationshipNamespace)
                {
                    ExternalCodeXlsxRelationship relationship = new ExternalCodeXlsxRelationship();
                    relationship.Identifier = ReadWorkbookAttribute(reader, "Id", string.Empty);
                    relationship.Type = ReadWorkbookAttribute(reader, "Type", string.Empty);
                    relationship.Target = ReadWorkbookAttribute(reader, "Target", string.Empty);
                    relationship.IsExternal = string.Equals(
                        ReadWorkbookAttribute(reader, "TargetMode", string.Empty),
                        "External",
                        StringComparison.OrdinalIgnoreCase);
                    if (relationship.Identifier.Length == 0 || relationship.Type.Length == 0 ||
                        relationship.Target.Length == 0 ||
                        !identifiers.Add(relationship.Identifier))
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.relationship",
                            "The workbook contains an incomplete or duplicated relationship.");
                        return null;
                    }

                    if (relationships.Count >= request.Limits.MaxCollectionCount)
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.relationship_limit",
                            "The workbook exceeds the configured relationship limit.");
                        return null;
                    }

                    relationships.Add(relationship);
                }
            }
        }

        if (!sawRoot)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.relationship_root",
                "The workbook relationship part is empty.");
            return null;
        }

        return relationships;
    }

    internal static bool ResolveWorkbookPaths(
        ExternalCodeXlsxWorkbook workbook,
        List<ExternalCodeXlsxRelationship> relationships,
        ExternalCodeImportResult result)
    {
        ExternalCodeXlsxRelationship? allCodes = FindWorkbookRelationship(
            relationships,
            workbook.AllCodeSetsRelationship);
        ExternalCodeXlsxRelationship? definitions = FindWorkbookRelationship(
            relationships,
            workbook.CodeSetDefinitionsRelationship);
        if (allCodes == null || definitions == null || allCodes.IsExternal ||
            definitions.IsExternal ||
            !allCodes.Type.EndsWith("/worksheet", StringComparison.Ordinal) ||
            !definitions.Type.EndsWith("/worksheet", StringComparison.Ordinal))
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.worksheet_relationship",
                "A required worksheet relationship is missing, external, or has an invalid type.");
            return false;
        }

        workbook.AllCodeSetsPath = ResolveWorkbookTarget(allCodes.Target);
        workbook.CodeSetDefinitionsPath = ResolveWorkbookTarget(definitions.Target);
        if (workbook.AllCodeSetsPath.Length == 0 ||
            workbook.CodeSetDefinitionsPath.Length == 0)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.worksheet_path",
                "A required worksheet relationship has an unsafe package path.");
            return false;
        }

        return true;
    }

    internal static ExternalCodeXlsxRelationship? FindWorkbookRelationship(
        List<ExternalCodeXlsxRelationship> relationships,
        string identifier)
    {
        int index = 0;
        while (index < relationships.Count)
        {
            if (relationships[index].Identifier == identifier)
            {
                return relationships[index];
            }

            index++;
        }

        return null;
    }

    internal static string ResolveWorkbookTarget(string target)
    {
        if (target.Length == 0 || target[0] == '/' || target[0] == '\\' ||
            target.IndexOf('\\') >= 0 || target.IndexOf(':') >= 0 ||
            target.IndexOf('?') >= 0 || target.IndexOf('#') >= 0)
        {
            return string.Empty;
        }

        string path = target;
        if (!path.StartsWith("xl/", StringComparison.Ordinal))
        {
            path = "xl/" + path;
        }

        if (!RepositoryArtifactOpener.IsSafeEntryName(path))
        {
            return string.Empty;
        }

        return path;
    }

    internal static List<string>? ReadSharedStringsPart(
        byte[] bytes,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        List<string> values = new List<string>();
        StringBuilder text = new StringBuilder();
        bool sawRoot = false;
        bool inItem = false;
        bool inText = false;
        bool inPhonetic = false;
        int itemDepth = -1;
        int textDepth = -1;
        int phoneticDepth = -1;
        MessageReadOptions options = CreateWorkbookXmlOptions(request);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(bytes, options))
        {
            while (reader.ReadNext())
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                XmlNodeType nodeType = reader.GetNodeType();
                if (nodeType == XmlNodeType.Element)
                {
                    string localName = reader.GetLocalName();
                    string namespaceUri = reader.GetNamespaceUri();
                    int depth = reader.GetDepth();
                    if (depth == 0)
                    {
                        if (localName != "sst" || namespaceUri != SpreadsheetNamespace)
                        {
                            ExternalCodeSetImporter.SetImportError(
                                result,
                                "external-code.xlsx.shared_strings_root",
                                "The shared-string part has an unexpected root element.");
                            return null;
                        }

                        sawRoot = true;
                    }
                    else if (namespaceUri == SpreadsheetNamespace && localName == "si")
                    {
                        if (inItem)
                        {
                            ExternalCodeSetImporter.SetImportError(
                                result,
                                "external-code.xlsx.shared_string",
                                "The shared-string part contains nested string items.");
                            return null;
                        }

                        inItem = true;
                        itemDepth = depth;
                        text.Clear();
                        if (reader.GetIsEmptyElement())
                        {
                            if (!AddSharedStringValue(values, string.Empty, request, result))
                            {
                                return null;
                            }

                            inItem = false;
                            itemDepth = -1;
                        }
                    }
                    else if (inItem && namespaceUri == SpreadsheetNamespace &&
                        localName == "rPh")
                    {
                        inPhonetic = true;
                        phoneticDepth = depth;
                    }
                    else if (inItem && !inPhonetic && namespaceUri == SpreadsheetNamespace &&
                        localName == "t")
                    {
                        inText = true;
                        textDepth = depth;
                        if (reader.GetIsEmptyElement())
                        {
                            inText = false;
                            textDepth = -1;
                        }
                    }
                }
                else if ((nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA) &&
                    inText)
                {
                    if (text.Length > request.Limits.MaxTextLength - reader.GetValue().Length)
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.shared_string_length",
                            "A shared string exceeds the configured text limit.");
                        return null;
                    }

                    text.Append(reader.GetValue());
                }
                else if (nodeType == XmlNodeType.EndElement)
                {
                    int depth = reader.GetDepth();
                    if (inText && depth == textDepth)
                    {
                        inText = false;
                        textDepth = -1;
                    }

                    if (inPhonetic && depth == phoneticDepth)
                    {
                        inPhonetic = false;
                        phoneticDepth = -1;
                    }

                    if (inItem && depth == itemDepth)
                    {
                        if (!AddSharedStringValue(
                            values,
                            DecodeSpreadsheetEscapes(text.ToString()),
                            request,
                            result))
                        {
                            return null;
                        }

                        inItem = false;
                        itemDepth = -1;
                    }
                }
            }
        }

        if (!sawRoot || inItem)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.shared_strings",
                "The shared-string part is incomplete.");
            return null;
        }

        return values;
    }

    internal static bool AddSharedStringValue(
        List<string> values,
        string value,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        if (values.Count >= request.Limits.MaxCollectionCount)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.shared_string_limit",
                "The workbook exceeds the configured shared-string limit.");
            return false;
        }

        values.Add(value);
        return true;
    }

    internal static ExternalCodeXlsxSheet? ReadWorksheetPart(
        byte[] bytes,
        List<string> sharedStrings,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        ExternalCodeXlsxSheetReadState state = new ExternalCodeXlsxSheetReadState();
        MessageReadOptions options = CreateWorkbookXmlOptions(request);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(bytes, options))
        {
            while (reader.ReadNext())
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                XmlNodeType nodeType = reader.GetNodeType();
                if (nodeType == XmlNodeType.Element)
                {
                    if (!ReadWorksheetStart(reader, state, sharedStrings, request, result))
                    {
                        return null;
                    }
                }
                else if ((nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA) &&
                    state.CaptureKind != 0)
                {
                    if (state.CaptureText.Length >
                        request.Limits.MaxTextLength - reader.GetValue().Length)
                    {
                        ExternalCodeSetImporter.SetImportError(
                            result,
                            "external-code.xlsx.cell_length",
                            "A worksheet cell exceeds the configured text limit.");
                        return null;
                    }

                    state.CaptureText.Append(reader.GetValue());
                }
                else if (nodeType == XmlNodeType.EndElement)
                {
                    if (!ReadWorksheetEnd(
                        reader.GetLocalName(),
                        reader.GetNamespaceUri(),
                        reader.GetDepth(),
                        state,
                        sharedStrings,
                        request,
                        result))
                    {
                        return null;
                    }
                }
            }
        }

        if (!state.SawWorksheet || state.CurrentRow != null || state.CurrentCell != null)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.worksheet",
                "A required worksheet is incomplete.");
            return null;
        }

        return state.Sheet;
    }

    internal static bool ReadWorksheetStart(
        BoundedXmlReader reader,
        ExternalCodeXlsxSheetReadState state,
        List<string> sharedStrings,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        string localName = reader.GetLocalName();
        string namespaceUri = reader.GetNamespaceUri();
        int depth = reader.GetDepth();
        if (depth == 0)
        {
            if (localName != "worksheet" || namespaceUri != SpreadsheetNamespace)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.worksheet_root",
                    "A worksheet part has an unexpected root element.");
                return false;
            }

            state.SawWorksheet = true;
            return true;
        }

        if (namespaceUri != SpreadsheetNamespace)
        {
            return true;
        }

        if (localName == "sheetData")
        {
            state.InSheetData = true;
        }
        else if (state.InSheetData && localName == "row")
        {
            if (state.CurrentRow != null)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.row",
                    "A worksheet contains nested rows.");
                return false;
            }

            IntegerLexicalResult rowNumber = IntegerLexical.ParseInt32(
                ReadWorkbookAttribute(reader, "r", string.Empty).AsSpan());
            if (!rowNumber.Success || rowNumber.Value <= 0)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.row_number",
                    "A worksheet row has an invalid row number.");
                return false;
            }

            state.CurrentRow = new ExternalCodeXlsxRow();
            state.CurrentRow.RowNumber = rowNumber.Value;
            state.Columns.Clear();
            if (reader.GetIsEmptyElement())
            {
                return FinishWorksheetRow(state, request, result);
            }
        }
        else if (state.CurrentRow != null && localName == "c")
        {
            if (state.CurrentCell != null)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.cell",
                    "A worksheet contains nested cells.");
                return false;
            }

            int columnIndex = ParseCellColumn(
                ReadWorkbookAttribute(reader, "r", string.Empty),
                state.CurrentRow.RowNumber);
            if (columnIndex < 0 || !state.Columns.Add(columnIndex))
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.cell_reference",
                    "A worksheet cell has an invalid or duplicated reference.");
                return false;
            }

            state.CurrentCell = new ExternalCodeXlsxCell();
            state.CurrentCell.ColumnIndex = columnIndex;
            state.CurrentCell.Type = ReadWorkbookAttribute(reader, "t", string.Empty);
            state.CaptureText.Clear();
            state.CaptureKind = 0;
            state.CaptureDepth = -1;
            if (reader.GetIsEmptyElement())
            {
                return FinishWorksheetCell(state, sharedStrings, request, result);
            }
        }
        else if (state.CurrentCell != null && localName == "v")
        {
            state.CaptureKind = 1;
            state.CaptureDepth = depth;
            state.CaptureText.Clear();
            if (reader.GetIsEmptyElement())
            {
                state.CaptureKind = 0;
                state.CaptureDepth = -1;
            }
        }
        else if (state.CurrentCell != null && state.CurrentCell.Type == "inlineStr" &&
            localName == "t")
        {
            state.CaptureKind = 2;
            state.CaptureDepth = depth;
            if (reader.GetIsEmptyElement())
            {
                state.CaptureKind = 0;
                state.CaptureDepth = -1;
            }
        }

        return true;
    }

    internal static bool ReadWorksheetEnd(
        string localName,
        string namespaceUri,
        int depth,
        ExternalCodeXlsxSheetReadState state,
        List<string> sharedStrings,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        if (state.CaptureKind != 0 && depth == state.CaptureDepth)
        {
            state.CaptureKind = 0;
            state.CaptureDepth = -1;
        }

        if (namespaceUri != SpreadsheetNamespace)
        {
            return true;
        }

        if (localName == "c" && state.CurrentCell != null)
        {
            return FinishWorksheetCell(state, sharedStrings, request, result);
        }

        if (localName == "row" && state.CurrentRow != null)
        {
            return FinishWorksheetRow(state, request, result);
        }

        if (localName == "sheetData")
        {
            state.InSheetData = false;
        }

        return true;
    }

    internal static bool FinishWorksheetCell(
        ExternalCodeXlsxSheetReadState state,
        List<string> sharedStrings,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        ExternalCodeXlsxCell? cell = state.CurrentCell;
        ExternalCodeXlsxRow? row = state.CurrentRow;
        if (cell == null || row == null)
        {
            return false;
        }

        string rawValue = state.CaptureText.ToString();
        if (cell.Type == "s")
        {
            IntegerLexicalResult sharedIndex = IntegerLexical.ParseInt32(rawValue.AsSpan());
            if (!sharedIndex.Success || sharedIndex.Value < 0 ||
                sharedIndex.Value >= sharedStrings.Count)
            {
                ExternalCodeSetImporter.SetImportError(
                    result,
                    "external-code.xlsx.shared_string_index",
                    "A worksheet cell references an unavailable shared string.");
                return false;
            }

            cell.Value = sharedStrings[sharedIndex.Value];
        }
        else
        {
            cell.Value = DecodeSpreadsheetEscapes(rawValue);
        }

        if (row.Cells.Count >= request.Limits.MaxCollectionCount)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.cell_limit",
                "A worksheet row exceeds the configured cell limit.");
            return false;
        }

        row.Cells.Add(cell);
        state.CurrentCell = null;
        state.CaptureText.Clear();
        state.CaptureKind = 0;
        state.CaptureDepth = -1;
        return true;
    }

    internal static bool FinishWorksheetRow(
        ExternalCodeXlsxSheetReadState state,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        ExternalCodeXlsxRow? row = state.CurrentRow;
        if (row == null || state.CurrentCell != null)
        {
            return false;
        }

        if (state.Sheet.Rows.Count >= request.Limits.MaxCollectionCount)
        {
            ExternalCodeSetImporter.SetImportError(
                result,
                "external-code.xlsx.row_limit",
                "A worksheet exceeds the configured row limit.");
            return false;
        }

        state.Sheet.Rows.Add(row);
        state.CurrentRow = null;
        state.Columns.Clear();
        return true;
    }

    internal static int ParseCellColumn(string reference, int rowNumber)
    {
        if (reference.Length < 2)
        {
            return -1;
        }

        int index = 0;
        int column = 0;
        while (index < reference.Length && reference[index] >= 'A' && reference[index] <= 'Z')
        {
            if (column > (16384 - (reference[index] - 'A' + 1)) / 26)
            {
                return -1;
            }

            column = column * 26 + reference[index] - 'A' + 1;
            index++;
        }

        if (column <= 0 || column > 16384 || index == reference.Length)
        {
            return -1;
        }

        IntegerLexicalResult parsedRow = IntegerLexical.ParseInt32(
            reference.AsSpan().Slice(index));
        if (!parsedRow.Success || parsedRow.Value != rowNumber)
        {
            return -1;
        }

        return column - 1;
    }

    internal static string DecodeSpreadsheetEscapes(string value)
    {
        StringBuilder decoded = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int code = ReadSpreadsheetEscape(value, index);
            if (code < 0)
            {
                decoded.Append(value[index]);
                index++;
            }
            else if (code == 95 && ReadSpreadsheetEscape(value, index + 7) >= 0)
            {
                decoded.Append(value.AsSpan().Slice(index + 7, 7));
                index = index + 14;
            }
            else
            {
                decoded.Append((char)code);
                index = index + 7;
            }
        }

        return decoded.ToString();
    }

    internal static int ReadSpreadsheetEscape(string value, int index)
    {
        if (index < 0 || index > value.Length - 7 || value[index] != '_' ||
            value[index + 1] != 'x' || value[index + 6] != '_')
        {
            return -1;
        }

        int result = 0;
        int offset = 2;
        while (offset < 6)
        {
            int digit = ReadSpreadsheetHexDigit(value[index + offset]);
            if (digit < 0)
            {
                return -1;
            }

            result = result * 16 + digit;
            offset++;
        }

        return result;
    }

    internal static int ReadSpreadsheetHexDigit(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal static string ReadWorkbookAttribute(
        BoundedXmlReader reader,
        string localName,
        string namespaceUri)
    {
        string value = string.Empty;
        if (reader.MoveToFirstAttribute())
        {
            bool more = true;
            while (more)
            {
                if (reader.GetLocalName() == localName &&
                    reader.GetNamespaceUri() == namespaceUri)
                {
                    value = reader.GetValue();
                    more = false;
                }
                else
                {
                    more = reader.MoveToNextAttribute();
                }
            }

            reader.MoveBackToElement();
        }

        return value;
    }

    internal static MessageReadOptions CreateWorkbookXmlOptions(
        ExternalCodeImportRequest request)
    {
        MessageReadOptions options = new MessageReadOptions();
        options.Limits = request.Limits;
        options.PreserveWhitespace = false;
        return options;
    }
}
