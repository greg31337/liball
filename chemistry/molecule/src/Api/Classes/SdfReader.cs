using System.Collections.Generic;
using System.IO;
using System.Text;

namespace molecule;


/// <summary>
/// Reads sdf data into molecular objects.
/// </summary>
public class SdfReader : IMoleculeReader, ISdfReader
{
    internal MolfileReader MolfileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SdfReader"/> class.
    /// </summary>
    public SdfReader()
    {
        MolfileReader = new MolfileReader();
    }

    /// <summary>
    /// Reads the first SDF molecular graph from text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The first molecule, or an empty molecule when no record is available.</returns>
    public Molecule Read(string? text)
    {
        List<SdfRecord> records = ReadRecords(text);
        if (records.Count > 0)
        {
            return records[0].Molecule;
        }

        return new Molecule();
    }

    /// <summary>
    /// Reads SDF records and preserves per-record diagnostics and data fields.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SdfRecord> ReadRecords(string? text)
    {
        return ReadSdfWithOptions(text, null).Records;
    }

    /// <summary>
    /// Reads SDF records and preserves per-record diagnostics and data fields.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SdfRecord> ReadRecordsFromReader(TextReader? reader)
    {
        return ReadSdfFromReaderWithOptions(reader, null).Records;
    }

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> ReadAll(string? text)
    {
        List<SdfRecord> records = ReadRecords(text);
        List<Molecule> molecules = new List<Molecule>();
        int index = 0;
        while (index < records.Count)
        {
            molecules.Add(records[index].Molecule);
            index = index + 1;
        }

        return molecules;
    }

    /// <summary>
    /// Reads the next SDF record, including its molecule, data fields, and diagnostics.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting SDF record.</returns>
    public SdfRecord ReadRecord(TextReader? reader)
    {
        return ReadSdfRecordWithOptions(reader, null);
    }

    /// <summary>
    /// Reads SDF text using mixed-version, data-field, and per-record recovery policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses continuing mixed-version defaults.</param>
    /// <returns>Retained records, aggregate diagnostics, and record counts.</returns>
    public SdfReadResult ReadSdfWithOptions(string? text, SdfReadOptions? options)
    {
        StringReader reader = new StringReader(NormalizeString(text));
        return ReadSdfFromReaderWithOptions(reader, options);
    }

    /// <summary>
    /// Reads SDF records from a text reader using explicit policies.
    /// </summary>
    /// <param name="reader">The text reader that supplies SDF data.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses continuing mixed-version defaults.</param>
    /// <returns>Retained records, aggregate diagnostics, and record counts.</returns>
    public SdfReadResult ReadSdfFromReaderWithOptions(TextReader? reader, SdfReadOptions? options)
    {
        SdfReadResult result = new SdfReadResult();
        if (reader == null)
        {
            return result;
        }

        SdfReadOptions safeOptions = NormalizeReadOptions(options);
        MolfileVersion observedVersion = MolfileVersion.Auto;
        bool done = false;
        while (!done)
        {
            SdfRecord record = ReadSdfRecordWithOptions(reader, safeOptions);
            if (record.EndOfStream)
            {
                done = true;
            }
            else
            {
                record.Index = result.ReadCount;
                result.ReadCount = result.ReadCount + 1;
                if (record.Version != MolfileVersion.Auto)
                {
                    if (observedVersion == MolfileVersion.Auto)
                    {
                        observedVersion = record.Version;
                    }
                    else if (observedVersion != record.Version)
                    {
                        result.MixedMolfileVersions = true;
                        if (!safeOptions.AllowMixedMolfileVersions)
                        {
                            AddError(record, "SDF_MIXED_MOLFILE_VERSIONS", "Mixed MOL V2000 and V3000 records are not allowed by the selected policy.");
                        }
                    }
                }

                MergeReports(result.Report, record.Report);
                if (!record.Success)
                {
                    result.FailedCount = result.FailedCount + 1;
                    result.Success = false;
                }

                if (record.Success || safeOptions.IncludeFailedRecords)
                {
                    result.Records.Add(record);
                }

                if (!record.Success && !safeOptions.ContinueAfterError)
                {
                    done = true;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Reads one SDF record using explicit embedded-molfile and data-field policies.
    /// </summary>
    /// <param name="reader">The text reader that supplies SDF data.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The next SDF record.</returns>
    public SdfRecord ReadSdfRecordWithOptions(TextReader? reader, SdfReadOptions? options)
    {
        SdfRecord record = new SdfRecord();
        if (reader == null)
        {
            record.EndOfStream = true;
            return record;
        }

        StringBuilder builder = new StringBuilder();
        bool readAny = false;
        bool done = false;
        while (!done)
        {
            string? line = reader.ReadLine();
            if (line == null)
            {
                done = true;
            }
            else
            {
                readAny = true;
                if (line == "$$$$")
                {
                    record.HasRecordSeparator = true;
                    done = true;
                }
                else
                {
                    builder.Append(line);
                    builder.Append('\n');
                }
            }
        }

        if (!readAny && builder.Length == 0)
        {
            record.EndOfStream = true;
            return record;
        }

        record.RawText = builder.ToString();
        ParseRecordWithOptions(record, NormalizeReadOptions(options));
        return record;
    }

    internal void ParseRecord(SdfRecord record)
    {
        ParseRecordWithOptions(record, new SdfReadOptions());
    }

    internal void ParseRecordWithOptions(SdfRecord record, SdfReadOptions options)
    {
        if (NormalizeString(record.RawText).Trim() == "")
        {
            AddError(record, "SDF_EMPTY_RECORD", "SDF record is empty.");
            return;
        }

        MolfileReadResult molfileResult = MolfileReader.ReadMolfileWithOptions(record.RawText, options.Molfile);
        record.Molecule = molfileResult.Molecule;
        record.Version = molfileResult.Version;
        MergeReports(record.Report, molfileResult.Report);
        if (options.ReadDataFields)
        {
            ReadDataFields(record);
        }

        record.Success = record.Report.GetErrorCount() == 0;
    }

    internal void ReadDataFields(SdfRecord record)
    {
        List<string> lines = SplitLines(record.RawText);
        int index = FindDataStartLine(lines);
        while (index < lines.Count)
        {
            string line = lines[index];
            if (IsDataHeader(line))
            {
                string key = ReadDataHeaderKey(line);
                if (key == "")
                {
                    AddWarning(record, "SDF_DATA_HEADER_INVALID", "SDF data header does not contain a field name.");
                }

                index = index + 1;
                StringBuilder value = new StringBuilder();
                bool firstValueLine = true;
                while (index < lines.Count && !IsDataHeader(lines[index]) && lines[index] != "")
                {
                    if (!firstValueLine)
                    {
                        value.Append('\n');
                    }

                    value.Append(lines[index]);
                    firstValueLine = false;
                    index = index + 1;
                }

                record.Molecule.Metadata.AddSdfField(key, value.ToString());
                record.DataFieldCount = record.DataFieldCount + 1;
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal int FindDataStartLine(List<string> lines)
    {
        int index = 0;
        while (index < lines.Count)
        {
            if (lines[index].StartsWith("M  END"))
            {
                return index + 1;
            }

            index = index + 1;
        }

        return lines.Count;
    }

    internal bool IsDataHeader(string line)
    {
        string value = NormalizeString(line).Trim();
        return value.StartsWith(">");
    }

    internal string ReadDataHeaderKey(string line)
    {
        string value = NormalizeString(line);
        int start = value.IndexOf("<");
        int end = value.IndexOf(">", start + 1);
        if (start < 0 || end <= start)
        {
            return "";
        }

        return value.Substring(start + 1, end - start - 1);
    }

    internal void MergeReports(MoleculeReport target, MoleculeReport source)
    {
        int index = 0;
        while (index < source.GetCount())
        {
            target.Add(source.Get(index));
            index = index + 1;
        }
    }

    internal List<string> SplitLines(string text)
    {
        List<string> lines = new List<string>();
        string normalized = NormalizeString(text);
        int start = 0;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '\r' || character == '\n')
            {
                lines.Add(normalized.Substring(start, index - start));
                if (character == '\r' && index + 1 < normalized.Length && normalized[index + 1] == '\n')
                {
                    index = index + 1;
                }

                index = index + 1;
                start = index;
            }
            else
            {
                index = index + 1;
            }
        }

        if (start < normalized.Length)
        {
            lines.Add(normalized.Substring(start, normalized.Length - start));
        }

        return lines;
    }

    internal void AddError(SdfRecord record, string code, string message)
    {
        record.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("SDF", 0, 0, -1, ""), -1, -1, ErrorSeverity.Error));
        record.Success = false;
    }

    internal void AddWarning(SdfRecord record, string code, string message)
    {
        record.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("SDF", 0, 0, -1, ""), -1, -1, ErrorSeverity.Warning));
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal SdfReadOptions NormalizeReadOptions(SdfReadOptions? options)
    {
        SdfReadOptions safeOptions = new SdfReadOptions();
        if (options == null)
        {
            return safeOptions;
        }

        safeOptions.ContinueAfterError = options.ContinueAfterError;
        safeOptions.IncludeFailedRecords = options.IncludeFailedRecords;
        safeOptions.ReadDataFields = options.ReadDataFields;
        safeOptions.AllowMixedMolfileVersions = options.AllowMixedMolfileVersions;
        if (options.Molfile != null)
        {
            safeOptions.Molfile = options.Molfile;
        }

        return safeOptions;
    }
}
