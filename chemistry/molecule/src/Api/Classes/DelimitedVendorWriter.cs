using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular graphs and vendor metadata as quoted delimited records.
/// </summary>
public class DelimitedVendorWriter : IMoleculeWriter, IDelimitedVendorWriter
{
    internal SmilesWriter SmilesWriter;
    internal MolfileWriter MolfileWriter;
    internal SdfWriter SdfWriter;
    internal Mol2Writer Mol2Writer;
    internal PdbWriter PdbWriter;
    internal DeterministicSerializer DeterministicSerializer;

    /// <summary>
    /// Initializes a delimited vendor writer with built-in molecule codecs.
    /// </summary>
    public DelimitedVendorWriter()
    {
        SmilesWriter = new SmilesWriter();
        MolfileWriter = new MolfileWriter();
        SdfWriter = new SdfWriter();
        Mol2Writer = new Mol2Writer();
        PdbWriter = new PdbWriter();
        DeterministicSerializer = new DeterministicSerializer();
    }

    /// <summary>
    /// Writes one molecule as CSV with an embedded SMILES column and vendor metadata columns.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting delimited text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return WriteDelimitedVendor(molecule, null);
    }

    /// <summary>
    /// Writes one molecule using explicit delimited vendor options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The output options; <c>null</c> uses CSV with embedded SMILES.</param>
    /// <returns>The resulting delimited text; an empty string indicates that no value could be produced.</returns>
    public string WriteDelimitedVendor(Molecule? molecule, VendorWriteOptions? options)
    {
        if (molecule == null)
        {
            return "";
        }

        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(molecule);
        return WriteDelimitedVendorRecords(molecules, options).Text;
    }

    /// <summary>
    /// Writes molecule records using explicit columns, quoting, delimiter, and embedded format policies.
    /// </summary>
    /// <param name="molecules">The molecules to write in record order.</param>
    /// <param name="options">The output options; <c>null</c> uses CSV with embedded SMILES.</param>
    /// <returns>The output text, selected columns, record count, and diagnostics.</returns>
    public VendorWriteResult WriteDelimitedVendorRecords(List<Molecule>? molecules, VendorWriteOptions? options)
    {
        VendorWriteResult result = new VendorWriteResult();
        if (molecules == null)
        {
            AddError(result, "VENDOR_NULL_MOLECULES", "Cannot write a null vendor molecule collection.", 0);
            return result;
        }

        VendorWriteOptions safeOptions = NormalizeOptions(options);
        if (!IsSupportedMoleculeFormat(safeOptions.MoleculeFormat))
        {
            AddError(result, "VENDOR_MOLECULE_FORMAT_UNSUPPORTED", "Delimited vendor molecule format is not supported for writing.", 0);
            return result;
        }

        CollectColumns(molecules, safeOptions, result.Columns);
        result.MoleculeColumnIndex = FindColumn(result.Columns, GetSafeMoleculeColumn(safeOptions));
        if (result.MoleculeColumnIndex < 0)
        {
            AddError(result, "VENDOR_MOLECULE_COLUMN_MISSING", "Delimited vendor output columns do not contain the molecule column.", 0);
            return result;
        }

        StringBuilder builder = new StringBuilder();
        char delimiter = GetSafeDelimiter(safeOptions);
        if (safeOptions.WriteHeader)
        {
            AppendRow(builder, result.Columns, delimiter, safeOptions.QuoteAllFields);
        }

        int rowIndex = 0;
        while (rowIndex < molecules.Count)
        {
            Molecule? molecule = molecules[rowIndex];
            if (molecule == null)
            {
                AddError(result, "VENDOR_NULL_MOLECULE", "Cannot write a null vendor molecule record.", rowIndex + 1);
            }
            else
            {
                int errorCount = result.Report.GetErrorCount();
                List<string> fields = CreateFields(molecule, result.Columns, result.MoleculeColumnIndex, safeOptions, result, rowIndex + 1);
                if (result.Report.GetErrorCount() == errorCount)
                {
                    AppendRow(builder, fields, delimiter, safeOptions.QuoteAllFields);
                    result.WrittenCount = result.WrittenCount + 1;
                }
            }

            rowIndex = rowIndex + 1;
        }

        result.Text = builder.ToString();
        result.Success = result.Report.GetErrorCount() == 0;
        return result;
    }

    internal List<string> CreateFields(Molecule molecule, List<string> columns, int moleculeColumnIndex, VendorWriteOptions options, VendorWriteResult result, int rowNumber)
    {
        List<string> fields = new List<string>();
        int columnIndex = 0;
        while (columnIndex < columns.Count)
        {
            if (columnIndex == moleculeColumnIndex)
            {
                fields.Add(WriteMoleculeText(molecule, options.MoleculeFormat, result, rowNumber));
            }
            else
            {
                fields.Add(GetVendorValue(molecule, columns[columnIndex]));
            }

            columnIndex = columnIndex + 1;
        }

        return fields;
    }

    internal string WriteMoleculeText(Molecule molecule, FormatKind format, VendorWriteResult result, int rowNumber)
    {
        if (format == FormatKind.Unknown || format == FormatKind.Smiles)
        {
            return SmilesWriter.Write(molecule);
        }

        if (format == FormatKind.MolV2000 || format == FormatKind.MolV3000)
        {
            MolfileWriteOptions options = new MolfileWriteOptions();
            if (format == FormatKind.MolV2000)
            {
                options.Version = MolfileVersion.V2000;
            }
            else
            {
                options.Version = MolfileVersion.V3000;
            }

            MolfileWriteResult writeResult = MolfileWriter.WriteMolfileWithOptions(molecule, options);
            MergeReport(result.Report, writeResult.Report, rowNumber);
            return writeResult.Text;
        }

        if (format == FormatKind.Sdf)
        {
            return SdfWriter.Write(molecule);
        }

        if (format == FormatKind.Mol2)
        {
            Mol2WriteResult writeResult = Mol2Writer.WriteMol2WithResult(molecule);
            MergeReport(result.Report, writeResult.Report, rowNumber);
            return writeResult.Text;
        }

        if (format == FormatKind.Pdb)
        {
            PdbWriteResult writeResult = PdbWriter.WritePdbWithResult(molecule);
            MergeReport(result.Report, writeResult.Report, rowNumber);
            return writeResult.Text;
        }

        if (format == FormatKind.DeterministicText)
        {
            return DeterministicSerializer.Serialize(molecule);
        }

        AddError(result, "VENDOR_MOLECULE_FORMAT_UNSUPPORTED", "Delimited vendor molecule format is not supported for writing.", rowNumber);
        return "";
    }

    internal void CollectColumns(List<Molecule> molecules, VendorWriteOptions options, List<string> columns)
    {
        if (options.Columns != null)
        {
            int requestedIndex = 0;
            while (requestedIndex < options.Columns.Count)
            {
                AddUniqueColumn(columns, options.Columns[requestedIndex]);
                requestedIndex = requestedIndex + 1;
            }
        }

        if (columns.Count == 0)
        {
            columns.Add(GetSafeMoleculeColumn(options));
        }

        if (options.IncludeUnlistedVendorFields)
        {
            int moleculeIndex = 0;
            while (moleculeIndex < molecules.Count)
            {
                Molecule? molecule = molecules[moleculeIndex];
                if (molecule != null)
                {
                    int fieldIndex = 0;
                    while (fieldIndex < molecule.Metadata.VendorFields.Count)
                    {
                        string key = molecule.Metadata.VendorFields[fieldIndex].Key;
                        if (!IsInternalColumn(key))
                        {
                            AddUniqueColumn(columns, key);
                        }

                        fieldIndex = fieldIndex + 1;
                    }
                }

                moleculeIndex = moleculeIndex + 1;
            }
        }
    }

    internal void AddUniqueColumn(List<string> columns, string value)
    {
        string column = NormalizeString(value).Trim();
        if (column == "")
        {
            return;
        }

        if (FindColumn(columns, column) < 0)
        {
            columns.Add(column);
        }
    }

    internal int FindColumn(List<string> columns, string name)
    {
        string normalizedName = NormalizeHeader(name);
        int index = 0;
        while (index < columns.Count)
        {
            if (NormalizeHeader(columns[index]) == normalizedName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string GetVendorValue(Molecule molecule, string column)
    {
        string normalizedColumn = NormalizeHeader(column);
        int index = 0;
        while (index < molecule.Metadata.VendorFields.Count)
        {
            MoleculeMetadataEntry field = molecule.Metadata.VendorFields[index];
            if (NormalizeHeader(field.Key) == normalizedColumn)
            {
                return field.Value;
            }

            index = index + 1;
        }

        return "";
    }

    internal void AppendRow(StringBuilder builder, List<string> fields, char delimiter, bool quoteAllFields)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (index > 0)
            {
                builder.Append(delimiter);
            }

            AppendField(builder, fields[index], delimiter, quoteAllFields);
            index = index + 1;
        }

        builder.Append('\n');
    }

    internal void AppendField(StringBuilder builder, string value, char delimiter, bool quoteAllFields)
    {
        string text = NormalizeString(value);
        bool quote = quoteAllFields || RequiresQuoting(text, delimiter);
        if (quote)
        {
            builder.Append('"');
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(text[index]);
            }

            index = index + 1;
        }

        if (quote)
        {
            builder.Append('"');
        }
    }

    internal bool RequiresQuoting(string value, char delimiter)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == delimiter || character == '"' || character == '\r' || character == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal VendorWriteOptions NormalizeOptions(VendorWriteOptions? options)
    {
        if (options == null)
        {
            return new VendorWriteOptions();
        }

        return options;
    }

    internal char GetSafeDelimiter(VendorWriteOptions options)
    {
        if (options.Delimiter == '\0' || options.Delimiter == '\r' || options.Delimiter == '\n' || options.Delimiter == '"')
        {
            return ',';
        }

        return options.Delimiter;
    }

    internal string GetSafeMoleculeColumn(VendorWriteOptions options)
    {
        string value = NormalizeString(options.MoleculeColumn).Trim();
        if (value == "")
        {
            return "smiles";
        }

        return value;
    }

    internal bool IsSupportedMoleculeFormat(FormatKind format)
    {
        return format == FormatKind.Unknown || format == FormatKind.Smiles || format == FormatKind.MolV2000 || format == FormatKind.MolV3000 || format == FormatKind.Sdf || format == FormatKind.Mol2 || format == FormatKind.Pdb || format == FormatKind.DeterministicText;
    }

    internal bool IsInternalColumn(string value)
    {
        return NormalizeHeader(value) == "_vendor_row";
    }

    internal void MergeReport(MoleculeReport target, MoleculeReport source, int rowNumber)
    {
        int index = 0;
        while (index < source.GetCount())
        {
            MoleculeError sourceError = source.Get(index);
            target.Add(new MoleculeError(sourceError.Code, sourceError.Message, new DiagnosticLocation("vendor-delimited", rowNumber, 1, -1, ""), sourceError.AtomId, sourceError.BondId, sourceError.Severity));
            index = index + 1;
        }
    }

    internal void AddError(VendorWriteResult result, string code, string message, int rowNumber)
    {
        result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("vendor-delimited", rowNumber, 1, -1, ""), -1, -1, ErrorSeverity.Error));
        result.Success = false;
    }

    internal string NormalizeHeader(string value)
    {
        return NormalizeString(value).Trim().ToLowerInvariant();
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
