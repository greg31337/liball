using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Reads delimited vendor data into molecular objects.
/// </summary>
public class DelimitedVendorReader : IMoleculeReader, IDelimitedVendorReader
{
    internal SmilesParser SmilesReader;
    internal MolfileReader MolfileReader;
    internal SdfReader SdfReader;
    internal Mol2Reader Mol2Reader;
    internal PdbReader PdbReader;
    internal DeterministicSerializer DeterministicSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedVendorReader"/> class.
    /// </summary>
    public DelimitedVendorReader()
    {
        SmilesReader = new SmilesParser();
        MolfileReader = new MolfileReader();
        SdfReader = new SdfReader();
        Mol2Reader = new Mol2Reader();
        PdbReader = new PdbReader();
        DeterministicSerializer = new DeterministicSerializer();
    }

    /// <summary>
    /// Reads the first molecule from delimited vendor text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The first molecule, or an empty molecule when no row is available.</returns>
    public Molecule Read(string? text)
    {
        List<Molecule> molecules = ReadAll(text);
        if (molecules.Count > 0)
        {
            return molecules[0];
        }

        return new Molecule();
    }

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> ReadAll(string? text)
    {
        VendorReadResult result = ReadWithResult(text);
        return result.Molecules;
    }

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public VendorReadResult ReadWithResult(string? text)
    {
        return ReadWithFormat(text, new VendorFormat());
    }

    /// <summary>
    /// Reads delimited vendor data using the supplied delimiter and molecule-column configuration.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="format">The format.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public VendorReadResult ReadWithFormat(string? text, VendorFormat? format)
    {
        VendorReadResult result = new VendorReadResult();
        VendorFormat safeFormat = NormalizeFormat(format);
        result.Metadata.Format = safeFormat;
        result.Success = true;

        List<List<string>> records = ParseRecords(NormalizeString(text), safeFormat.GetSafeDelimiter());
        if (records.Count == 0)
        {
            AddError(result, "VENDOR_EMPTY_INPUT", "Delimited vendor text is empty.", 0);
            return result;
        }

        int rowStart = 0;
        List<string> firstFields = records[0];
        if (safeFormat.HasHeader)
        {
            CopyHeaders(firstFields, result.Metadata.Headers);
            rowStart = 1;
        }
        else
        {
            CreateGeneratedHeaders(firstFields.Count, result.Metadata.Headers);
        }

        result.Metadata.MoleculeColumnIndex = FindMoleculeColumn(result.Metadata.Headers, safeFormat.GetSafeMoleculeColumn());
        if (result.Metadata.MoleculeColumnIndex < 0)
        {
            AddError(result, "VENDOR_MOLECULE_COLUMN_MISSING", "Delimited vendor text has no molecule column.", 1);
            return result;
        }

        int recordIndex = rowStart;
        while (recordIndex < records.Count)
        {
            if (!IsEmptyRecord(records[recordIndex]))
            {
                ReadDataFields(result, records[recordIndex], recordIndex, safeFormat);
            }

            recordIndex = recordIndex + 1;
        }

        if (result.Molecules.Count == 0 && result.Report.GetErrorCount() == 0)
        {
            AddError(result, "VENDOR_NO_RECORDS", "Delimited vendor text has no data records.", records.Count);
        }

        if (result.Report.GetErrorCount() > 0)
        {
            result.Success = false;
        }

        return result;
    }

    internal void ReadDataLine(VendorReadResult result, string line, int lineIndex, VendorFormat format)
    {
        List<string> fields = ParseLine(line, format.GetSafeDelimiter());
        ReadDataFields(result, fields, lineIndex, format);
    }

    internal void ReadDataFields(VendorReadResult result, List<string> fields, int lineIndex, VendorFormat format)
    {
        NormalizeRowWidth(fields, result.Metadata.Headers.Count);
        result.Metadata.Rows.Add(fields);

        string moleculeText = "";
        if (result.Metadata.MoleculeColumnIndex >= 0 && result.Metadata.MoleculeColumnIndex < fields.Count)
        {
            moleculeText = fields[result.Metadata.MoleculeColumnIndex];
        }

        Molecule molecule = ReadMoleculeText(moleculeText, format.MoleculeFormat, result, lineIndex);
        AddVendorFields(molecule, result.Metadata.Headers, fields, format.PreserveMoleculeColumn, result.Metadata.MoleculeColumnIndex, lineIndex);
        result.Molecules.Add(molecule);
    }

    internal Molecule ReadMoleculeText(string text, FormatKind kind, VendorReadResult result, int lineIndex)
    {
        string safeText = NormalizeString(text);
        if (safeText.Length == 0)
        {
            AddError(result, "VENDOR_MOLECULE_EMPTY", "Delimited vendor row has an empty molecule value.", lineIndex + 1);
            return new Molecule();
        }

        if (kind == FormatKind.Unknown || kind == FormatKind.Smiles)
        {
            SmilesParseResult parseResult = SmilesReader.ParseWithResult(safeText);
            MergeReport(result.Report, parseResult.Report, lineIndex + 1);
            if (!parseResult.Success)
            {
                result.Success = false;
            }

            return parseResult.Molecule;
        }

        if (kind == FormatKind.MolV2000 || kind == FormatKind.MolV3000)
        {
            return MolfileReader.Read(safeText);
        }

        if (kind == FormatKind.Sdf)
        {
            return SdfReader.Read(safeText);
        }

        if (kind == FormatKind.Mol2)
        {
            return Mol2Reader.Read(safeText);
        }

        if (kind == FormatKind.Pdb)
        {
            return PdbReader.Read(safeText);
        }

        if (kind == FormatKind.DeterministicText)
        {
            return DeterministicSerializer.Deserialize(safeText);
        }

        AddError(result, "VENDOR_MOLECULE_FORMAT_UNSUPPORTED", "Delimited vendor molecule format is not supported.", lineIndex + 1);
        return new Molecule();
    }

    internal void AddVendorFields(Molecule molecule, List<string> headers, List<string> fields, bool preserveMoleculeColumn, int moleculeColumnIndex, int lineIndex)
    {
        int index = 0;
        while (index < headers.Count)
        {
            if (preserveMoleculeColumn || index != moleculeColumnIndex)
            {
                molecule.Metadata.AddVendorField(headers[index], GetField(fields, index));
            }

            index = index + 1;
        }

        molecule.Metadata.AddVendorField("_vendor_row", lineIndex.ToString());
    }

    internal List<string> ParseLine(string line, char delimiter)
    {
        List<string> fields = new List<string>();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        string safeLine = NormalizeString(line);
        bool inQuotes = false;
        int index = 0;

        while (index < safeLine.Length)
        {
            char character = safeLine[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < safeLine.Length && safeLine[index + 1] == '"')
                {
                    builder.Append('"');
                    index = index + 2;
                }
                else
                {
                    inQuotes = !inQuotes;
                    index = index + 1;
                }
            }
            else if (character == delimiter && !inQuotes)
            {
                fields.Add(builder.ToString());
                builder.Clear();
                index = index + 1;
            }
            else
            {
                builder.Append(character);
                index = index + 1;
            }
        }

        fields.Add(builder.ToString());
        return fields;
    }

    internal List<List<string>> ParseRecords(string text, char delimiter)
    {
        List<List<string>> records = new List<List<string>>();
        List<string> fields = new List<string>();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        string safeText = NormalizeString(text);
        bool inQuotes = false;
        int index = 0;
        while (index < safeText.Length)
        {
            char character = safeText[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < safeText.Length && safeText[index + 1] == '"')
                {
                    builder.Append('"');
                    index = index + 2;
                }
                else
                {
                    inQuotes = !inQuotes;
                    index = index + 1;
                }
            }
            else if (character == delimiter && !inQuotes)
            {
                fields.Add(builder.ToString());
                builder.Clear();
                index = index + 1;
            }
            else if ((character == '\r' || character == '\n') && !inQuotes)
            {
                fields.Add(builder.ToString());
                builder.Clear();
                records.Add(fields);
                fields = new List<string>();
                if (character == '\r' && index + 1 < safeText.Length && safeText[index + 1] == '\n')
                {
                    index = index + 1;
                }

                index = index + 1;
            }
            else if ((character == '\r' || character == '\n') && inQuotes)
            {
                builder.Append('\n');
                if (character == '\r' && index + 1 < safeText.Length && safeText[index + 1] == '\n')
                {
                    index = index + 1;
                }

                index = index + 1;
            }
            else
            {
                builder.Append(character);
                index = index + 1;
            }
        }

        if (builder.Length > 0 || fields.Count > 0)
        {
            fields.Add(builder.ToString());
            records.Add(fields);
        }

        return records;
    }

    internal bool IsEmptyRecord(List<string> fields)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index] != "")
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal List<string> SplitLines(string text)
    {
        List<string> lines = new List<string>();
        string safeText = NormalizeString(text);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < safeText.Length)
        {
            char character = safeText[index];
            if (character == '\r')
            {
                lines.Add(builder.ToString());
                builder.Clear();
                if (index + 1 < safeText.Length && safeText[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else if (character == '\n')
            {
                lines.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        if (builder.Length > 0)
        {
            lines.Add(builder.ToString());
        }

        return lines;
    }

    internal void CopyHeaders(List<string> fields, List<string> headers)
    {
        int index = 0;
        while (index < fields.Count)
        {
            string header = fields[index].Trim();
            if (header.Length == 0)
            {
                header = "field_" + (index + 1).ToString();
            }

            headers.Add(header);
            index = index + 1;
        }
    }

    internal void CreateGeneratedHeaders(int count, List<string> headers)
    {
        int index = 0;
        while (index < count)
        {
            headers.Add("field_" + (index + 1).ToString());
            index = index + 1;
        }
    }

    internal int FindMoleculeColumn(List<string> headers, string requestedColumn)
    {
        int requestedIndex = FindHeader(headers, requestedColumn);
        if (requestedIndex >= 0)
        {
            return requestedIndex;
        }

        int index = FindHeader(headers, "smiles");
        if (index >= 0)
        {
            return index;
        }

        index = FindHeader(headers, "structure");
        if (index >= 0)
        {
            return index;
        }

        index = FindHeader(headers, "mol");
        if (index >= 0)
        {
            return index;
        }

        index = FindHeader(headers, "molecule");
        if (index >= 0)
        {
            return index;
        }

        if (headers.Count > 0)
        {
            return 0;
        }

        return -1;
    }

    internal int FindHeader(List<string> headers, string name)
    {
        string normalizedName = NormalizeHeader(name);
        int index = 0;
        while (index < headers.Count)
        {
            if (NormalizeHeader(headers[index]) == normalizedName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void NormalizeRowWidth(List<string> fields, int count)
    {
        while (fields.Count < count)
        {
            fields.Add("");
        }
    }

    internal string GetField(List<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
        {
            return "";
        }

        return fields[index];
    }

    internal VendorFormat NormalizeFormat(VendorFormat? format)
    {
        if (format == null)
        {
            return new VendorFormat();
        }

        return format;
    }

    internal void MergeReport(MoleculeReport destination, MoleculeReport source, int lineNumber)
    {
        int index = 0;
        while (source != null && index < source.Errors.Count)
        {
            MoleculeError sourceError = source.Errors[index];
            MoleculeError error = new MoleculeError(sourceError.Code, sourceError.Message, new DiagnosticLocation("vendor-delimited", lineNumber, 1, -1, ""), sourceError.AtomId, sourceError.BondId, sourceError.Severity);
            destination.Add(error);
            index = index + 1;
        }
    }

    internal void AddError(VendorReadResult result, string code, string message, int lineNumber)
    {
        result.Success = false;
        MoleculeError error = new MoleculeError(code, message, new DiagnosticLocation("vendor-delimited", lineNumber, 1, -1, ""), -1, -1, ErrorSeverity.Error);
        result.Report.Add(error);
    }

    internal string NormalizeHeader(string value)
    {
        string text = NormalizeString(value).Trim();
        return text.ToLowerInvariant();
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
