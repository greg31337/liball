namespace molecule;


/// <summary>
/// Stores, parses, exports, and compares existing InChI and InChIKey metadata.
/// </summary>
public class InChIWorkflows : IInChIWorkflows
{
    internal InChIParser Parser;
    internal InChIKeyParser KeyParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="InChIWorkflows"/> class.
    /// </summary>
    public InChIWorkflows()
    {
        Parser = new InChIParser();
        KeyParser = new InChIKeyParser();
    }

    /// <summary>
    /// Attaches the in ch i.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AttachInChI(Molecule? molecule, string? text)
    {
        if (molecule == null)
        {
            return false;
        }

        InChIRecord record = Parser.Parse(text);
        if (!record.Success)
        {
            return false;
        }

        AttachInChIRecord(molecule, record);
        return true;
    }

    /// <summary>
    /// Attaches the in ch irecord.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AttachInChIRecord(Molecule? molecule, InChIRecord? record)
    {
        if (molecule == null || !CanCompare(record))
        {
            return false;
        }

        if (record == null)
        {
            return false;
        }

        molecule.Metadata.SetStringValue("INCHI", record.OriginalText);
        molecule.Metadata.SetStringValue("INCHI_VERSION", record.Version);
        molecule.Metadata.SetStringValue("INCHI_STANDARD", BoolText(record.IsStandard));
        molecule.Metadata.SetStringValue("INCHI_FORMULA", record.FormulaLayer);
        molecule.Metadata.SetStringValue("INCHI_CONNECTIVITY", record.ConnectivityLayer);
        molecule.Metadata.SetStringValue("INCHI_HYDROGEN", record.HydrogenLayer);
        molecule.Metadata.SetStringValue("INCHI_TAUTOMER", record.TautomerLayer);
        molecule.Metadata.SetStringValue("INCHI_CHARGE", record.ChargeLayer);
        molecule.Metadata.SetStringValue("INCHI_PROTON", record.ProtonLayer);
        molecule.Metadata.SetStringValue("INCHI_STEREO", record.StereoLayer);
        molecule.Metadata.SetStringValue("INCHI_ISOTOPE", record.IsotopeLayer);
        molecule.Metadata.SetStringValue("INCHI_FIXED_H", record.FixedHLayer);
        return true;
    }

    /// <summary>
    /// Attaches the in ch ikey.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AttachInChIKey(Molecule? molecule, string? text)
    {
        if (molecule == null)
        {
            return false;
        }

        InChIKeyRecord record = KeyParser.Parse(text);
        if (!record.Success)
        {
            return false;
        }

        AttachInChIKeyRecord(molecule, record);
        return true;
    }

    /// <summary>
    /// Attaches the in ch ikey record.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AttachInChIKeyRecord(Molecule? molecule, InChIKeyRecord? record)
    {
        if (molecule == null || record == null || !record.Success)
        {
            return false;
        }

        molecule.Metadata.SetStringValue("INCHI_KEY", record.NormalizedText);
        molecule.Metadata.SetStringValue("INCHI_KEY_FIRST_BLOCK", record.FirstBlock);
        molecule.Metadata.SetStringValue("INCHI_KEY_SECOND_BLOCK", record.SecondBlock);
        molecule.Metadata.SetStringValue("INCHI_KEY_REMAINING_HASH", record.RemainingHashBlock);
        molecule.Metadata.SetStringValue("INCHI_KEY_VERSION", record.VersionBlock);
        molecule.Metadata.SetStringValue("INCHI_KEY_VERSION_CHARACTER", record.VersionCharacter);
        molecule.Metadata.SetStringValue("INCHI_KEY_STANDARD_CHARACTER", record.StandardCharacter);
        molecule.Metadata.SetStringValue("INCHI_KEY_PROTONATION", record.ProtonationIndicator);
        return true;
    }

    /// <summary>
    /// Attaches the identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="inchi">The inchi.</param>
    /// <param name="inchiKey">The inchi key.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AttachIdentifiers(Molecule? molecule, string? inchi, string? inchiKey)
    {
        if (molecule == null)
        {
            return false;
        }

        InChIRecord inchiRecord = Parser.Parse(inchi);
        InChIKeyRecord keyRecord = KeyParser.Parse(inchiKey);
        if (!inchiRecord.Success || !keyRecord.Success)
        {
            return false;
        }

        AttachInChIRecord(molecule, inchiRecord);
        AttachInChIKeyRecord(molecule, keyRecord);
        return true;
    }

    /// <summary>
    /// Exports the in ch i.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string ExportInChI(Molecule? molecule)
    {
        return ReadMetadataValue(molecule, "INCHI", "InChI");
    }

    /// <summary>
    /// Exports the in ch ikey.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string ExportInChIKey(Molecule? molecule)
    {
        return ReadMetadataValue(molecule, "INCHI_KEY", "InChIKey");
    }

    /// <summary>
    /// Returns the in ch irecord.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting in ch irecord.</returns>
    public InChIRecord GetInChIRecord(Molecule? molecule)
    {
        return Parser.Parse(ExportInChI(molecule));
    }

    /// <summary>
    /// Returns the in ch ikey record.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting in ch ikey record.</returns>
    public InChIKeyRecord GetInChIKeyRecord(Molecule? molecule)
    {
        return KeyParser.Parse(ExportInChIKey(molecule));
    }

    internal string ReadMetadataValue(Molecule? molecule, string upperKey, string mixedKey)
    {
        if (molecule == null || molecule.Metadata == null)
        {
            return "";
        }

        string value = molecule.Metadata.GetStringValue(upperKey);
        if (value.Length > 0)
        {
            return value;
        }

        value = molecule.Metadata.GetStringValue(mixedKey);
        if (value.Length > 0)
        {
            return value;
        }

        value = molecule.Metadata.GetSdfField(upperKey);
        if (value.Length > 0)
        {
            return value;
        }

        value = molecule.Metadata.GetSdfField(mixedKey);
        if (value.Length > 0)
        {
            return value;
        }

        value = molecule.Metadata.GetVendorField(upperKey);
        if (value.Length > 0)
        {
            return value;
        }

        return molecule.Metadata.GetVendorField(mixedKey);
    }

    /// <summary>
    /// Determines whether the supplied values are equivalent.
    /// </summary>
    /// <param name="leftText">The left text.</param>
    /// <param name="rightText">The right text.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AreEquivalent(string? leftText, string? rightText, IdentityPolicy policy)
    {
        InChIRecord left = Parser.Parse(leftText);
        InChIRecord right = Parser.Parse(rightText);
        return AreRecordsEquivalent(left, right, policy);
    }

    /// <summary>
    /// Determines whether the supplied values are equivalent.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AreRecordsEquivalent(InChIRecord? left, InChIRecord? right, IdentityPolicy policy)
    {
        if (!CanCompare(left) || !CanCompare(right))
        {
            return false;
        }

        return BuildComparisonKeyForRecord(left, policy) == BuildComparisonKeyForRecord(right, policy);
    }

    /// <summary>
    /// Determines whether the value has same protonation.
    /// </summary>
    /// <param name="leftText">The left text.</param>
    /// <param name="rightText">The right text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HasSameProtonation(string? leftText, string? rightText)
    {
        InChIRecord left = Parser.Parse(leftText);
        InChIRecord right = Parser.Parse(rightText);
        return HaveSameRecordProtonation(left, right);
    }

    /// <summary>
    /// Determines whether the value has same protonation.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HaveSameRecordProtonation(InChIRecord left, InChIRecord right)
    {
        if (!CanCompare(left) || !CanCompare(right))
        {
            return false;
        }

        return left.ProtonLayer == right.ProtonLayer;
    }

    /// <summary>
    /// Builds the comparison key.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string BuildComparisonKey(string? text, IdentityPolicy policy)
    {
        InChIRecord record = Parser.Parse(text);
        return BuildComparisonKeyForRecord(record, policy);
    }

    /// <summary>
    /// Builds the comparison key.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string BuildComparisonKeyForRecord(InChIRecord? record, IdentityPolicy policy)
    {
        if (!CanCompare(record))
        {
            return "";
        }

        if (record == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (policy == IdentityPolicy.Exact)
        {
            AppendKey(builder, "version", record.Version);
            AppendKey(builder, "standard", BoolText(record.IsStandard));
        }

        AppendKey(builder, "formula", record.FormulaLayer);
        AppendKey(builder, "connectivity", record.ConnectivityLayer);

        if (policy == IdentityPolicy.Connectivity)
        {
            return builder.ToString();
        }

        if (IncludesHydrogen(policy))
        {
            AppendKey(builder, "hydrogen", record.HydrogenLayer);
        }

        if (IncludesTautomer(policy))
        {
            AppendKey(builder, "tautomer", record.TautomerLayer);
        }

        if (IncludesCharge(policy))
        {
            AppendKey(builder, "charge", record.ChargeLayer);
        }

        if (IncludesProtonation(policy))
        {
            AppendKey(builder, "proton", record.ProtonLayer);
        }

        if (IncludesStereo(policy))
        {
            AppendKey(builder, "stereo", record.StereoLayer);
        }

        if (IncludesIsotope(policy))
        {
            AppendKey(builder, "isotope", record.IsotopeLayer);
        }

        if (IncludesFixedH(policy))
        {
            AppendKey(builder, "fixedh", record.FixedHLayer);
        }

        if (policy == IdentityPolicy.Exact)
        {
            AppendOtherLayers(builder, record);
        }

        return builder.ToString();
    }

    internal bool CanCompare(InChIRecord? record)
    {
        if (record == null)
        {
            return false;
        }

        if (!record.Success)
        {
            return false;
        }

        if (!record.HasFormula())
        {
            return false;
        }

        return true;
    }

    internal bool IncludesHydrogen(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesTautomer(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.TautomerInsensitive || policy == IdentityPolicy.TautomerParent || policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesCharge(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.ChargeInsensitive || policy == IdentityPolicy.ChargeParent || policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesProtonation(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesStereo(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.StereoInsensitive || policy == IdentityPolicy.StereoParent || policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesIsotope(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.IsotopeInsensitive || policy == IdentityPolicy.IsotopeParent || policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal bool IncludesFixedH(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return false;
        }

        if (policy == IdentityPolicy.TautomerInsensitive || policy == IdentityPolicy.TautomerParent || policy == IdentityPolicy.SuperParent)
        {
            return false;
        }

        return true;
    }

    internal void AppendOtherLayers(System.Text.StringBuilder builder, InChIRecord record)
    {
        int index = 0;
        while (index < record.OtherLayers.Count)
        {
            AppendKey(builder, "other:" + record.OtherLayers[index].Key, record.OtherLayers[index].Value);
            index = index + 1;
        }
    }

    internal void AppendKey(System.Text.StringBuilder builder, string key, string value)
    {
        builder.Append(key);
        builder.Append("=");
        builder.Append(NormalizeString(value));
        builder.Append("|");
    }

    internal string BoolText(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
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
