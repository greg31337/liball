namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="InChIWorkflows"/>.
/// </summary>
public interface IInChIWorkflows
{
    /// <summary>
    /// Attaches the in ch i.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AttachInChI(Molecule? molecule, string? text);

    /// <summary>
    /// Attaches the in ch irecord.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AttachInChIRecord(Molecule? molecule, InChIRecord? record);

    /// <summary>
    /// Attaches the in ch ikey.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AttachInChIKey(Molecule? molecule, string? text);

    /// <summary>
    /// Attaches the in ch ikey record.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AttachInChIKeyRecord(Molecule? molecule, InChIKeyRecord? record);

    /// <summary>
    /// Attaches the identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="inchi">The inchi.</param>
    /// <param name="inchiKey">The inchi key.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AttachIdentifiers(Molecule? molecule, string? inchi, string? inchiKey);

    /// <summary>
    /// Exports the in ch i.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string ExportInChI(Molecule? molecule);

    /// <summary>
    /// Exports the in ch ikey.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string ExportInChIKey(Molecule? molecule);

    /// <summary>
    /// Returns the in ch irecord.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting in ch irecord.</returns>
    InChIRecord GetInChIRecord(Molecule? molecule);

    /// <summary>
    /// Returns the in ch ikey record.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting in ch ikey record.</returns>
    InChIKeyRecord GetInChIKeyRecord(Molecule? molecule);

    /// <summary>
    /// Determines whether the supplied values are equivalent.
    /// </summary>
    /// <param name="leftText">The left text.</param>
    /// <param name="rightText">The right text.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AreEquivalent(string? leftText, string? rightText, IdentityPolicy policy);

    /// <summary>
    /// Determines whether the supplied values are equivalent.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AreRecordsEquivalent(InChIRecord? left, InChIRecord? right, IdentityPolicy policy);

    /// <summary>
    /// Determines whether the value has same protonation.
    /// </summary>
    /// <param name="leftText">The left text.</param>
    /// <param name="rightText">The right text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool HasSameProtonation(string? leftText, string? rightText);

    /// <summary>
    /// Determines whether the value has same protonation.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool HaveSameRecordProtonation(InChIRecord left, InChIRecord right);

    /// <summary>
    /// Builds the comparison key.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string BuildComparisonKey(string? text, IdentityPolicy policy);

    /// <summary>
    /// Builds the comparison key.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string BuildComparisonKeyForRecord(InChIRecord? record, IdentityPolicy policy);
}

