using System.Collections.Generic;
using System.IO;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SdfReader"/>.
/// </summary>
public interface ISdfReader
{

    /// <summary>
    /// Reads SDF records and preserves per-record diagnostics and data fields.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SdfRecord> ReadRecords(string? text);

    /// <summary>
    /// Reads SDF records and preserves per-record diagnostics and data fields.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SdfRecord> ReadRecordsFromReader(TextReader? reader);

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> ReadAll(string? text);

    /// <summary>
    /// Reads the next SDF record, including its molecule, data fields, and diagnostics.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting SDF record.</returns>
    SdfRecord ReadRecord(TextReader? reader);

    /// <summary>
    /// Reads SDF text using mixed-version, data-field, and per-record recovery policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses continuing mixed-version defaults.</param>
    /// <returns>Retained records, aggregate diagnostics, and record counts.</returns>
    SdfReadResult ReadSdfWithOptions(string? text, SdfReadOptions? options);

    /// <summary>
    /// Reads SDF records from a text reader using explicit policies.
    /// </summary>
    /// <param name="reader">The text reader that supplies SDF data.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses continuing mixed-version defaults.</param>
    /// <returns>Retained records, aggregate diagnostics, and record counts.</returns>
    SdfReadResult ReadSdfFromReaderWithOptions(TextReader? reader, SdfReadOptions? options);

    /// <summary>
    /// Reads one SDF record using explicit embedded-molfile and data-field policies.
    /// </summary>
    /// <param name="reader">The text reader that supplies SDF data.</param>
    /// <param name="options">The SDF read options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The next SDF record.</returns>
    SdfRecord ReadSdfRecordWithOptions(TextReader? reader, SdfReadOptions? options);
}
