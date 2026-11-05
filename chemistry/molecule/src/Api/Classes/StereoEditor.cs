namespace molecule;


/// <summary>
/// Creates and edits typed stereo records on independent molecule copies.
/// </summary>
public class StereoEditor : IStereoEditor
{
    internal StereoValidator Validator;

    /// <summary>
    /// Initializes a stereo editor using the built-in stereo validator.
    /// </summary>
    public StereoEditor()
    {
        Validator = new StereoValidator();
    }

    /// <summary>
    /// Creates a validated tetrahedral stereo record on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The tetrahedral center atom identifier.</param>
    /// <param name="ligandAtomIds">Exactly four ligand identifiers, with at most one negative identifier for an implicit ligand.</param>
    /// <param name="chirality">The specified clockwise or counterclockwise chirality.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult CreateTetrahedralStereo(Molecule? molecule, int centerAtomId, int[]? ligandAtomIds, Chirality chirality)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        if (molecule == null)
        {
            return result;
        }

        if (ligandAtomIds == null || ligandAtomIds.Length != 4)
        {
            AddStereoEditError(result, "STEREO_EDIT_TETRAHEDRAL_LIGANDS", "Tetrahedral stereo creation requires exactly four ligand identifiers.", centerAtomId, -1);
            return result;
        }

        if (chirality != Chirality.Clockwise && chirality != Chirality.CounterClockwise)
        {
            AddStereoEditError(result, "STEREO_EDIT_TETRAHEDRAL_CHIRALITY", "Tetrahedral stereo creation requires clockwise or counterclockwise chirality.", centerAtomId, -1);
            return result;
        }

        if (HasTetrahedralStereoAtCenter(result.Molecule, centerAtomId))
        {
            AddStereoEditError(result, "STEREO_EDIT_DUPLICATE_FOCUS", "The atom already has a tetrahedral stereo record.", centerAtomId, -1);
            return result;
        }

        StereoRecord record = new StereoRecord();
        record.Id = result.Molecule.StereoRecords.Count;
        record.SetTetrahedral(centerAtomId, ligandAtomIds[0], ligandAtomIds[1], ligandAtomIds[2], ligandAtomIds[3], chirality);
        record.Source = "editor";
        return AddCreatedStereoRecord(result, record);
    }

    /// <summary>
    /// Creates a validated double-bond stereo record on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoBondId">The double-bond identifier.</param>
    /// <param name="beginLigandAtomId">The begin-side ligand identifier, or a negative value for an implicit hydrogen.</param>
    /// <param name="endLigandAtomId">The end-side ligand identifier, or a negative value for an implicit hydrogen.</param>
    /// <param name="configuration">The specified E, Z, cis, or trans configuration.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult CreateDoubleBondStereo(Molecule? molecule, int stereoBondId, int beginLigandAtomId, int endLigandAtomId, DoubleBondConfiguration configuration)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        if (molecule == null)
        {
            return result;
        }

        if (!IsSpecifiedDoubleBondConfiguration(configuration))
        {
            AddStereoEditError(result, "STEREO_EDIT_DOUBLE_BOND_CONFIGURATION", "Double-bond stereo creation requires E, Z, cis, or trans configuration.", -1, stereoBondId);
            return result;
        }

        if (HasDoubleBondStereoAtBond(result.Molecule, stereoBondId))
        {
            AddStereoEditError(result, "STEREO_EDIT_DUPLICATE_FOCUS", "The bond already has a double-bond stereo record.", -1, stereoBondId);
            return result;
        }

        StereoRecord record = new StereoRecord();
        record.Id = result.Molecule.StereoRecords.Count;
        record.SetDoubleBond(stereoBondId, beginLigandAtomId, endLigandAtomId, configuration);
        record.Source = "editor";
        return AddCreatedStereoRecord(result, record);
    }

    /// <summary>
    /// Creates an unspecified typed allene stereo placeholder on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The central allene atom identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult CreateAlleneStereoPlaceholder(Molecule? molecule, int centerAtomId, int[]? carrierAtomIds)
    {
        return CreateExtendedStereoRecord(molecule, StereoKind.Allene, centerAtomId, -1, carrierAtomIds);
    }

    /// <summary>
    /// Creates an unspecified typed atropisomeric stereo placeholder on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="axisBondId">The atropisomeric axis bond identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult CreateAtropisomericStereoPlaceholder(Molecule? molecule, int axisBondId, int[]? carrierAtomIds)
    {
        return CreateExtendedStereoRecord(molecule, StereoKind.Atropisomeric, -1, axisBondId, carrierAtomIds);
    }

    /// <summary>
    /// Creates an unspecified typed placeholder for a future non-tetrahedral atom-centered stereo form.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The stereo focus atom identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult CreateNonTetrahedralStereoPlaceholder(Molecule? molecule, int centerAtomId, int[]? carrierAtomIds)
    {
        return CreateExtendedStereoRecord(molecule, StereoKind.NonTetrahedral, centerAtomId, -1, carrierAtomIds);
    }

    /// <summary>
    /// Inverts a specified tetrahedral, double-bond, or supported axial orientation on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult InvertStereo(Molecule? molecule, int stereoRecordId)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        StereoRecord? record = GetStereoEditRecord(result, molecule, stereoRecordId);
        if (record == null)
        {
            return result;
        }

        StereoKind type = GetStereoRecordKind(record);
        result.StereoKind = type;
        if (type == StereoKind.Tetrahedral)
        {
            if (record.Chirality == Chirality.Clockwise)
            {
                record.Chirality = Chirality.CounterClockwise;
            }
            else if (record.Chirality == Chirality.CounterClockwise)
            {
                record.Chirality = Chirality.Clockwise;
            }
            else
            {
                AddStereoEditError(result, "STEREO_EDIT_NOT_INVERTIBLE", "An unspecified tetrahedral stereo record cannot be inverted.", record.CenterAtomId, -1);
                return result;
            }
        }
        else if (type == StereoKind.DoubleBond)
        {
            if (record.Configuration == DoubleBondConfiguration.E)
            {
                record.Configuration = DoubleBondConfiguration.Z;
            }
            else if (record.Configuration == DoubleBondConfiguration.Z)
            {
                record.Configuration = DoubleBondConfiguration.E;
            }
            else if (record.Configuration == DoubleBondConfiguration.Cis)
            {
                record.Configuration = DoubleBondConfiguration.Trans;
            }
            else if (record.Configuration == DoubleBondConfiguration.Trans)
            {
                record.Configuration = DoubleBondConfiguration.Cis;
            }
            else
            {
                AddStereoEditError(result, "STEREO_EDIT_NOT_INVERTIBLE", "An unspecified double-bond stereo record cannot be inverted.", -1, record.StereoBondId);
                return result;
            }
        }
        else if (record.IsExtendedStereo())
        {
            if (record.Chirality == Chirality.Clockwise)
            {
                record.Chirality = Chirality.CounterClockwise;
            }
            else if (record.Chirality == Chirality.CounterClockwise)
            {
                record.Chirality = Chirality.Clockwise;
            }
            else
            {
                AddStereoEditError(result, "STEREO_EDIT_NOT_INVERTIBLE", "An unspecified typed stereo placeholder cannot be inverted.", record.CenterAtomId, record.StereoBondId);
                return result;
            }
        }
        else
        {
            AddStereoEditError(result, "STEREO_EDIT_UNSUPPORTED_RECORD", "The stereo record kind cannot be inverted.", record.CenterAtomId, record.StereoBondId);
            return result;
        }

        result.Success = true;
        result.Changed = true;
        result.StereoRecordId = stereoRecordId;
        return result;
    }

    /// <summary>
    /// Marks a tetrahedral, double-bond, or typed placeholder record as unspecified on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult MarkStereoUnspecified(Molecule? molecule, int stereoRecordId)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        StereoRecord? record = GetStereoEditRecord(result, molecule, stereoRecordId);
        if (record == null)
        {
            return result;
        }

        StereoKind type = GetStereoRecordKind(record);
        result.StereoKind = type;
        if (type == StereoKind.Tetrahedral || record.IsExtendedStereo())
        {
            result.Changed = record.Chirality != Chirality.Unspecified;
            record.Chirality = Chirality.Unspecified;
        }
        else if (type == StereoKind.DoubleBond)
        {
            result.Changed = record.Configuration != DoubleBondConfiguration.Unspecified;
            record.Configuration = DoubleBondConfiguration.Unspecified;
        }
        else
        {
            AddStereoEditError(result, "STEREO_EDIT_UNSUPPORTED_RECORD", "The stereo record kind cannot be marked unspecified.", record.CenterAtomId, record.StereoBondId);
            return result;
        }

        result.Success = true;
        result.StereoRecordId = stereoRecordId;
        return result;
    }

    /// <summary>
    /// Removes a stereo record and repairs enhanced stereo group identifiers on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    public StereoEditResult RemoveStereo(Molecule? molecule, int stereoRecordId)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        StereoRecord? record = GetStereoEditRecord(result, molecule, stereoRecordId);
        if (record == null)
        {
            return result;
        }

        result.StereoKind = GetStereoRecordKind(record);
        MoleculeBuilder builder = MoleculeBuilder.CreateFrom(result.Molecule);
        builder.RemoveStereoRecordAt(stereoRecordId);
        builder.NormalizeStereoGroups();
        result.Molecule = builder.Build();
        result.Success = true;
        result.Changed = true;
        result.StereoRecordId = stereoRecordId;
        return result;
    }

    internal StereoEditResult BeginStereoEdit(Molecule? molecule)
    {
        StereoEditResult result = new StereoEditResult();
        if (molecule == null)
        {
            result.Report.AddError("STEREO_EDIT_NULL_MOLECULE", "Cannot edit stereo records for a null molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        return result;
    }

    internal StereoEditResult CreateExtendedStereoRecord(Molecule? molecule, StereoKind type, int centerAtomId, int stereoBondId, int[]? carrierAtomIds)
    {
        StereoEditResult result = BeginStereoEdit(molecule);
        if (molecule == null)
        {
            return result;
        }

        if (HasExtendedStereoAtFocus(result.Molecule, type, centerAtomId, stereoBondId))
        {
            AddStereoEditError(result, "STEREO_EDIT_DUPLICATE_FOCUS", "The selected focus already has this typed stereo placeholder.", centerAtomId, stereoBondId);
            return result;
        }

        int[] safeCarrierAtomIds = carrierAtomIds == null ? new int[] { } : carrierAtomIds;
        StereoRecord record = new StereoRecord();
        record.Id = result.Molecule.StereoRecords.Count;
        record.SetExtendedStereoPlaceholder(type, centerAtomId, stereoBondId, safeCarrierAtomIds);
        record.Source = "editor";
        return AddCreatedStereoRecord(result, record);
    }

    internal StereoEditResult AddCreatedStereoRecord(StereoEditResult result, StereoRecord record)
    {
        Molecule validationMolecule = MoleculeBuilder.CloneMolecule(result.Molecule);
        validationMolecule.StereoRecords.Clear();
        validationMolecule.StereoGroups = new StereoGroups();
        validationMolecule.StereoRecords.Add(MoleculeBuilder.CloneStereoRecord(record, 0));
        MoleculeReport validationReport = Validator.Validate(validationMolecule);
        MergeStereoEditReport(result.Report, validationReport);
        if (validationReport.HasErrors())
        {
            return result;
        }

        result.Molecule.StereoRecords.Add(record);
        result.Success = true;
        result.Changed = true;
        result.StereoRecordId = record.Id;
        result.StereoKind = GetStereoRecordKind(record);
        return result;
    }

    internal StereoRecord? GetStereoEditRecord(StereoEditResult result, Molecule? source, int stereoRecordId)
    {
        if (source == null)
        {
            return null;
        }

        if (stereoRecordId < 0 || stereoRecordId >= result.Molecule.StereoRecords.Count)
        {
            AddStereoEditError(result, "STEREO_EDIT_INVALID_RECORD", "The stereo record identifier is outside the stereo record collection.", -1, -1);
            return null;
        }

        result.StereoRecordId = stereoRecordId;
        return result.Molecule.StereoRecords[stereoRecordId];
    }

    internal bool HasTetrahedralStereoAtCenter(Molecule molecule, int centerAtomId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsTetrahedral() && record.CenterAtomId == centerAtomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasDoubleBondStereoAtBond(Molecule molecule, int stereoBondId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsDoubleBond() && record.StereoBondId == stereoBondId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasExtendedStereoAtFocus(Molecule molecule, StereoKind type, int centerAtomId, int stereoBondId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (GetStereoRecordKind(record) == type && record.CenterAtomId == centerAtomId && record.StereoBondId == stereoBondId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsSpecifiedDoubleBondConfiguration(DoubleBondConfiguration configuration)
    {
        return configuration == DoubleBondConfiguration.E
            || configuration == DoubleBondConfiguration.Z
            || configuration == DoubleBondConfiguration.Cis
            || configuration == DoubleBondConfiguration.Trans;
    }

    internal StereoKind GetStereoRecordKind(StereoRecord record)
    {
        if (record.Type != StereoKind.Unknown)
        {
            return record.Type;
        }

        return record.ResolveStereoKind();
    }

    internal void MergeStereoEditReport(MoleculeReport target, MoleculeReport source)
    {
        int index = 0;
        while (index < source.Errors.Count)
        {
            target.Add(source.Errors[index]);
            index = index + 1;
        }
    }

    internal void AddStereoEditError(StereoEditResult result, string code, string message, int atomId, int bondId)
    {
        MoleculeError error = new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), atomId, bondId, ErrorSeverity.Error);
        result.Report.Add(error);
    }
}
