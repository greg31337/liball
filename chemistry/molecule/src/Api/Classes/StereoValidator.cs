using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides stereo validator data and operations for molecular graphs.
/// </summary>
public class StereoValidator : IStereoValidator
{
    /// <summary>
    /// Initializes a new stereo validator.
    /// </summary>
    public StereoValidator()
    {
    }

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MoleculeReport Validate(Molecule? molecule)
    {
        MoleculeReport report = new MoleculeReport();

        if (molecule == null)
        {
            AddError(report, "STEREO_NULL_MOLECULE", "Cannot validate stereo records for a null molecule.", -1, -1);
            return report;
        }

        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record == null)
            {
                AddError(report, "STEREO_NULL_RECORD", "Stereo record is null.", -1, -1);
            }
            else
            {
                if (record.Id < 0)
                {
                    record.Id = index;
                }

                if (record.IsTetrahedral())
                {
                    ValidateTetrahedral(molecule, record, report);
                }
                else if (record.IsDoubleBond())
                {
                    ValidateDoubleBond(molecule, record, report);
                }
                else if (record.IsExtendedStereo())
                {
                    ValidateExtendedStereo(molecule, record, report);
                }
                else
                {
                    AddWarning(report, "STEREO_UNKNOWN_RECORD", "Stereo record kind is not supported by the validator.", record.CenterAtomId, -1);
                }
            }

            index = index + 1;
        }

        return report;
    }

    internal void ValidateTetrahedral(Molecule molecule, StereoRecord record, MoleculeReport report)
    {
        if (!molecule.IsValidAtomId(record.CenterAtomId))
        {
            AddError(report, "TETRAHEDRAL_INVALID_CENTER", "Tetrahedral stereo center does not reference an atom.", record.CenterAtomId, -1);
            return;
        }

        if (!IsTetrahedralChirality(record.Chirality))
        {
            AddError(report, "TETRAHEDRAL_CHIRALITY", "Tetrahedral stereo record has no tetrahedral chirality value.", record.CenterAtomId, -1);
        }

        if (record.LigandAtomIds.Count != 4)
        {
            AddError(report, "TETRAHEDRAL_LIGAND_COUNT", "Tetrahedral stereo record must contain exactly four ligands.", record.CenterAtomId, -1);
            return;
        }

        int implicitLigandCount = record.GetImplicitLigandCount();
        if (implicitLigandCount > 1)
        {
            AddError(report, "TETRAHEDRAL_IMPLICIT_LIGAND", "Tetrahedral stereo record can contain at most one implicit ligand.", record.CenterAtomId, -1);
        }

        ValidateTetrahedralLigands(molecule, record, report);
    }

    internal void ValidateDoubleBond(Molecule molecule, StereoRecord record, MoleculeReport report)
    {
        if (record.StereoBondId < 0 || record.StereoBondId >= molecule.Bonds.Count)
        {
            AddError(report, "DOUBLE_BOND_INVALID_BOND", "Double-bond stereo record does not reference a bond.", -1, record.StereoBondId);
            return;
        }

        Bond bond = molecule.Bonds[record.StereoBondId];
        if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
        {
            AddError(report, "DOUBLE_BOND_INVALID_BOND", "Double-bond stereo record references a bond with invalid atoms.", -1, record.StereoBondId);
            return;
        }

        if (bond.Order != BondOrder.Double)
        {
            AddError(report, "DOUBLE_BOND_ORDER", "Double-bond stereo record must reference a double bond.", -1, record.StereoBondId);
        }

        if (!IsDoubleBondConfiguration(record.Configuration))
        {
            AddError(report, "DOUBLE_BOND_CONFIGURATION", "Double-bond stereo record has no double-bond configuration value.", -1, record.StereoBondId);
        }

        if (record.BeginLigandAtomId >= 0 && record.BeginLigandAtomId == record.EndLigandAtomId)
        {
            AddError(report, "DOUBLE_BOND_DUPLICATE_LIGAND", "Double-bond stereo ligands must be distinct.", -1, record.StereoBondId);
        }

        ValidateDoubleBondLigand(molecule, bond.BeginAtomId, bond.EndAtomId, record.BeginLigandAtomId, record.StereoBondId, report);
        ValidateDoubleBondLigand(molecule, bond.EndAtomId, bond.BeginAtomId, record.EndLigandAtomId, record.StereoBondId, report);
    }

    internal void ValidateExtendedStereo(Molecule molecule, StereoRecord record, MoleculeReport report)
    {
        StereoKind type = record.Type;
        if (type == StereoKind.Unknown)
        {
            type = record.ResolveStereoKind();
        }

        if (type == StereoKind.Allene || type == StereoKind.NonTetrahedral)
        {
            if (!molecule.IsValidAtomId(record.CenterAtomId))
            {
                AddError(report, "EXTENDED_STEREO_INVALID_CENTER", "The typed stereo placeholder does not reference a valid focus atom.", record.CenterAtomId, -1);
            }
        }
        else if (type == StereoKind.Atropisomeric)
        {
            if (record.StereoBondId < 0 || record.StereoBondId >= molecule.Bonds.Count)
            {
                AddError(report, "EXTENDED_STEREO_INVALID_AXIS", "The atropisomeric placeholder does not reference a valid axis bond.", -1, record.StereoBondId);
            }
        }
        else
        {
            AddError(report, "EXTENDED_STEREO_INVALID_TYPE", "The extended stereo record has an unsupported typed form.", record.CenterAtomId, record.StereoBondId);
        }

        ValidateExtendedStereoCarriers(molecule, record, report);
    }

    internal void ValidateExtendedStereoCarriers(Molecule molecule, StereoRecord record, MoleculeReport report)
    {
        List<int> seenCarrierIds = new List<int>();
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            int carrierAtomId = record.LigandAtomIds[index];
            if (carrierAtomId < -1 || carrierAtomId >= molecule.Atoms.Count)
            {
                AddError(report, "EXTENDED_STEREO_INVALID_CARRIER", "A typed stereo placeholder carrier does not reference an atom or the implicit carrier sentinel.", record.CenterAtomId, record.StereoBondId);
            }
            else if (carrierAtomId >= 0)
            {
                if (ContainsInt(seenCarrierIds, carrierAtomId))
                {
                    AddError(report, "EXTENDED_STEREO_DUPLICATE_CARRIER", "A typed stereo placeholder contains a duplicate carrier atom.", record.CenterAtomId, record.StereoBondId);
                }
                else
                {
                    seenCarrierIds.Add(carrierAtomId);
                }
            }

            index = index + 1;
        }
    }

    internal void ValidateDoubleBondLigand(Molecule molecule, int terminalAtomId, int otherTerminalAtomId, int ligandAtomId, int stereoBondId, MoleculeReport report)
    {
        if (ligandAtomId < 0)
        {
            Atom terminal = molecule.Atoms[terminalAtomId];
            if (terminal.ExplicitHydrogenCount + terminal.ImplicitHydrogenCount <= 0)
            {
                AddError(report, "DOUBLE_BOND_IMPLICIT_LIGAND", "Implicit double-bond stereo ligand requires an implicit or explicit hydrogen on that side.", terminalAtomId, stereoBondId);
            }

            return;
        }

        if (!molecule.IsValidAtomId(ligandAtomId))
        {
            AddError(report, "DOUBLE_BOND_INVALID_LIGAND", "Double-bond stereo ligand does not reference an atom.", terminalAtomId, stereoBondId);
            return;
        }

        if (ligandAtomId == terminalAtomId || ligandAtomId == otherTerminalAtomId)
        {
            AddError(report, "DOUBLE_BOND_SELF_LIGAND", "Double-bond stereo ligand cannot be a double-bond atom.", terminalAtomId, stereoBondId);
            return;
        }

        if (molecule.GetBondIdBetween(terminalAtomId, ligandAtomId) < 0)
        {
            AddError(report, "DOUBLE_BOND_UNBONDED_LIGAND", "Double-bond stereo ligand is not bonded to the expected double-bond atom.", terminalAtomId, stereoBondId);
        }
    }

    internal void ValidateTetrahedralLigands(Molecule molecule, StereoRecord record, MoleculeReport report)
    {
        List<int> seenLigands = new List<int>();
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            int ligandAtomId = record.LigandAtomIds[index];
            if (ligandAtomId >= 0)
            {
                if (!molecule.IsValidAtomId(ligandAtomId))
                {
                    AddError(report, "TETRAHEDRAL_INVALID_LIGAND", "Tetrahedral ligand does not reference an atom.", record.CenterAtomId, -1);
                }
                else
                {
                    if (ligandAtomId == record.CenterAtomId)
                    {
                        AddError(report, "TETRAHEDRAL_SELF_LIGAND", "Tetrahedral ligand cannot be the stereo center.", record.CenterAtomId, -1);
                    }
                    else
                    {
                        if (ContainsInt(seenLigands, ligandAtomId))
                        {
                            AddError(report, "TETRAHEDRAL_DUPLICATE_LIGAND", "Tetrahedral ligand appears more than once.", record.CenterAtomId, -1);
                        }
                        else
                        {
                            seenLigands.Add(ligandAtomId);
                        }

                        if (molecule.GetBondIdBetween(record.CenterAtomId, ligandAtomId) < 0)
                        {
                            AddError(report, "TETRAHEDRAL_UNBONDED_LIGAND", "Tetrahedral ligand is not bonded to the stereo center.", record.CenterAtomId, -1);
                        }
                    }
                }
            }

            index = index + 1;
        }
    }

    internal bool IsTetrahedralChirality(Chirality chirality)
    {
        if (chirality == Chirality.Clockwise || chirality == Chirality.CounterClockwise || chirality == Chirality.Unspecified || chirality == Chirality.Unknown)
        {
            return true;
        }

        return false;
    }

    internal bool IsDoubleBondConfiguration(DoubleBondConfiguration configuration)
    {
        if (configuration == DoubleBondConfiguration.E || configuration == DoubleBondConfiguration.Z || configuration == DoubleBondConfiguration.Cis || configuration == DoubleBondConfiguration.Trans || configuration == DoubleBondConfiguration.Unspecified || configuration == DoubleBondConfiguration.Unknown)
        {
            return true;
        }

        return false;
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddError(MoleculeReport report, string code, string message, int atomId, int bondId)
    {
        MoleculeError error = new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), atomId, bondId, ErrorSeverity.Error);
        report.Add(error);
    }

    internal void AddWarning(MoleculeReport report, string code, string message, int atomId, int bondId)
    {
        MoleculeError error = new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), atomId, bondId, ErrorSeverity.Warning);
        report.Add(error);
    }
}
