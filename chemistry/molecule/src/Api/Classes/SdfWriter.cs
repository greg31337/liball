using System.Collections.Generic;
using System.IO;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as sdf data.
/// </summary>
public class SdfWriter : IMoleculeWriter, ISdfWriter
{
    internal MolfileWriter MolfileWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SdfWriter"/> class.
    /// </summary>
    public SdfWriter()
    {
        MolfileWriter = new MolfileWriter();
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        if (molecule == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        WriteMoleculeRecord(writer, molecule);
        return builder.ToString();
    }

    internal string WriteMolecules(List<Molecule> molecules)
    {
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        WriteMoleculesToWriter(writer, molecules);
        return builder.ToString();
    }

    /// <summary>
    /// Writes SDF records in order with record separators.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteRecords(List<SdfRecord> records)
    {
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        WriteRecordsToWriter(writer, records);
        return builder.ToString();
    }

    /// <summary>
    /// Writes SDF records using explicit embedded-molfile, data-field, and separator policies.
    /// </summary>
    /// <param name="records">The records to write.</param>
    /// <param name="options">The SDF write options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The output text, diagnostics, and emitted count.</returns>
    public SdfWriteResult WriteSdfWithOptions(List<SdfRecord>? records, SdfWriteOptions? options)
    {
        SdfWriteResult result = new SdfWriteResult();
        if (records == null)
        {
            result.Report.AddError("SDF_NULL_RECORDS", "Cannot write a null SDF record collection.");
            return result;
        }

        SdfWriteOptions safeOptions = NormalizeWriteOptions(options);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        int index = 0;
        while (index < records.Count)
        {
            SdfRecord? record = records[index];
            if (record == null)
            {
                result.Report.AddError("SDF_NULL_RECORD", "Cannot write a null SDF record.");
            }
            else
            {
                MolfileWriteResult molfileResult = WriteMoleculeRecordWithOptions(writer, record.Molecule, safeOptions);
                MergeReports(result.Report, molfileResult.Report);
                if (molfileResult.Success)
                {
                    result.WrittenCount = result.WrittenCount + 1;
                }
            }

            index = index + 1;
        }

        result.Text = builder.ToString();
        result.Success = result.Report.GetErrorCount() == 0;
        return result;
    }

    internal void WriteMoleculesToWriter(TextWriter? writer, List<Molecule>? molecules)
    {
        if (writer == null || molecules == null)
        {
            return;
        }

        int index = 0;
        while (index < molecules.Count)
        {
            WriteMoleculeRecord(writer, molecules[index]);
            index = index + 1;
        }
    }

    /// <summary>
    /// Writes SDF records in order with record separators.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="records">The records.</param>
    public void WriteRecordsToWriter(TextWriter? writer, List<SdfRecord>? records)
    {
        if (writer == null || records == null)
        {
            return;
        }

        int index = 0;
        while (index < records.Count)
        {
            WriteRecord(writer, records[index]);
            index = index + 1;
        }
    }

    /// <summary>
    /// Writes one SDF record, including its molecule and data fields.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="record">The record.</param>
    public void WriteRecord(TextWriter? writer, SdfRecord? record)
    {
        if (record == null)
        {
            return;
        }

        WriteMoleculeRecord(writer, record.Molecule);
    }

    /// <summary>
    /// Writes one SDF record, including its molecule and data fields.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="molecule">The molecule to process.</param>
    public void WriteMoleculeRecord(TextWriter? writer, Molecule? molecule)
    {
        if (writer == null || molecule == null)
        {
            return;
        }

        writer.Write(MolfileWriter.Write(molecule));
        AppendDataFields(writer, molecule);
        writer.Write("$$$$");
        writer.Write('\n');
    }

    internal MolfileWriteResult WriteMoleculeRecordWithOptions(TextWriter writer, Molecule molecule, SdfWriteOptions options)
    {
        MolfileWriteResult result = MolfileWriter.WriteMolfileWithOptions(molecule, options.Molfile);
        if (!result.Success)
        {
            return result;
        }

        writer.Write(result.Text);
        if (options.WriteDataFields)
        {
            AppendDataFields(writer, molecule);
        }

        if (options.WriteRecordSeparator)
        {
            writer.Write("$$$$");
            writer.Write('\n');
        }

        return result;
    }

    internal void AppendDataFields(TextWriter writer, Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Metadata.SdfFields.Count)
        {
            MoleculeMetadataEntry field = molecule.Metadata.SdfFields[index];
            writer.Write(">  <");
            writer.Write(NormalizeFieldName(field.Key));
            writer.Write(">");
            writer.Write('\n');
            writer.Write(NormalizeDataValue(field.Value));
            writer.Write('\n');
            writer.Write('\n');
            index = index + 1;
        }
    }

    internal string NormalizeFieldName(string value)
    {
        string text = NormalizeString(value);
        text = text.Replace('\r', ' ');
        text = text.Replace('\n', ' ');
        text = text.Replace('<', '_');
        text = text.Replace('>', '_');
        return text;
    }

    internal string NormalizeDataValue(string value)
    {
        string text = NormalizeString(value);
        text = text.Replace("\r\n", "\n");
        text = text.Replace('\r', '\n');
        return text;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
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

    internal SdfWriteOptions NormalizeWriteOptions(SdfWriteOptions? options)
    {
        SdfWriteOptions safeOptions = new SdfWriteOptions();
        if (options == null)
        {
            return safeOptions;
        }

        safeOptions.WriteDataFields = options.WriteDataFields;
        safeOptions.WriteRecordSeparator = options.WriteRecordSeparator;
        if (options.Molfile != null)
        {
            safeOptions.Molfile = options.Molfile;
        }

        return safeOptions;
    }
}
