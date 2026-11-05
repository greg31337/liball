using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="DuplicateDetector"/>.
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// Configures the detector with explicit identity options and clears previously tracked entries.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    void Configure(IdentityOptions? options);

    /// <summary>
    /// Configures the detector with explicit identity options and a format service, then clears previously tracked entries.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    /// <param name="formats">The format service used by text ingestion.</param>
    void ConfigureWithFormats(IdentityOptions? options, IMoleculeFormatService? formats);

    /// <summary>
    /// Adds the supplied value to this collection.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    DuplicateResult Add(Molecule? molecule);

    /// <summary>
    /// Adds the string.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="formatOptions">The options used to detect and read the input format.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    DuplicateResult AddString(string? text, FormatOptions? formatOptions);

    /// <summary>
    /// Adds the SDF record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    DuplicateResult AddSdfRecord(SdfRecord? record);

    /// <summary>
    /// Adds the many.
    /// </summary>
    /// <param name="molecules">The molecules.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<DuplicateResult> AddMany(List<Molecule>? molecules);

    /// <summary>
    /// Adds the many.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<DuplicateResult> AddManyFromReader(MoleculeStreamReader? reader);

    /// <summary>
    /// Adds the SDF records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<DuplicateResult> AddSdfRecords(List<SdfRecord>? records);

    /// <summary>
    /// Adds the SDF text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<DuplicateResult> AddSdfText(string? text);

    /// <summary>
    /// Determines whether the value is duplicate.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsDuplicate(Molecule? molecule);

    /// <summary>
    /// Returns the entry count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    int GetEntryCount();

    /// <summary>
    /// Clears all identity keys and molecules retained by the duplicate detector.
    /// </summary>
    void Reset();

    /// <summary>
    /// Returns the duplicate groups.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    List<DuplicateGroup> GetDuplicateGroups();
}
