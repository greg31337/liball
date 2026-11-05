using System.Collections.Generic;
using System.IO;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SdfWriter"/>.
/// </summary>
public interface ISdfWriter
{

    /// <summary>
    /// Writes SDF records in order with record separators.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteRecords(List<SdfRecord> records);

    /// <summary>
    /// Writes SDF records in order with record separators.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="records">The records.</param>
    void WriteRecordsToWriter(TextWriter? writer, List<SdfRecord>? records);

    /// <summary>
    /// Writes one SDF record, including its molecule and data fields.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="record">The record.</param>
    void WriteRecord(TextWriter? writer, SdfRecord? record);

    /// <summary>
    /// Writes one SDF record, including its molecule and data fields.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="molecule">The molecule to process.</param>
    void WriteMoleculeRecord(TextWriter? writer, Molecule? molecule);

    /// <summary>
    /// Writes SDF records using explicit embedded-molfile, data-field, and separator policies.
    /// </summary>
    /// <param name="records">The records to write.</param>
    /// <param name="options">The SDF write options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The output text, diagnostics, and emitted count.</returns>
    SdfWriteResult WriteSdfWithOptions(List<SdfRecord>? records, SdfWriteOptions? options);
}
