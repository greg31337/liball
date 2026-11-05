using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides stereo perceiver data and operations for molecular graphs.
/// </summary>
public class StereoPerceiver : IStereoPerceiver
{
    /// <summary>
    /// Initializes a new stereo perceiver with the library's built-in perception rules.
    /// </summary>
    public StereoPerceiver()
    {
    }

    /// <summary>
    /// Perceives the from coordinates.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public StereoPerceptionResult PerceiveFromCoordinates(Molecule? molecule)
    {
        StereoPerceptionResult result = new StereoPerceptionResult();

        if (molecule == null)
        {
            result.Report.AddError("STEREO_NULL_MOLECULE", "Cannot perceive coordinate stereo for a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        result.Success = true;
        AddTetrahedralRecordsFrom3D(molecule, result);
        AddDoubleBondRecordsFrom2D(molecule, result);
        return result;
    }

    internal void AddTetrahedralRecordsFrom3D(Molecule molecule, StereoPerceptionResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (!HasTetrahedralRecordForCenter(molecule, atomIndex))
            {
                List<int> ligands = molecule.GetNeighborAtomIds(atomIndex);
                if (ligands.Count == 4 && AtomAndLigandsHaveCoordinates(molecule, atomIndex, ligands))
                {
                    double volume = CalculateSignedTetrahedralVolume(molecule, ligands[0], ligands[1], ligands[2], ligands[3]);
                    if (volume > 0.000001 || volume < -0.000001)
                    {
                        StereoRecord record = new StereoRecord();
                        Chirality chirality = Chirality.Clockwise;
                        if (volume < 0.0)
                        {
                            chirality = Chirality.CounterClockwise;
                        }

                        record.SetTetrahedral(atomIndex, ligands[0], ligands[1], ligands[2], ligands[3], chirality);
                        record.Id = molecule.StereoRecords.Count;
                        record.Source = "coordinates3d";
                        molecule.StereoRecords.Add(record);
                        result.Records.Add(record);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AddDoubleBondRecordsFrom2D(Molecule molecule, StereoPerceptionResult result)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.Order == BondOrder.Double && !HasDoubleBondRecordForBond(molecule, bond.Id))
            {
                DirectionalLigand beginLigand = FindCoordinateLigand(molecule, bond.BeginAtomId, bond.EndAtomId);
                DirectionalLigand endLigand = FindCoordinateLigand(molecule, bond.EndAtomId, bond.BeginAtomId);
                if (beginLigand.Found && endLigand.Found && DoubleBondAtomsAndLigandsHaveCoordinates(molecule, bond, beginLigand.AtomId, endLigand.AtomId))
                {
                    double beginSide = CalculateSide2D(molecule.Atoms[bond.BeginAtomId], molecule.Atoms[bond.EndAtomId], molecule.Atoms[beginLigand.AtomId]);
                    double endSide = CalculateSide2D(molecule.Atoms[bond.BeginAtomId], molecule.Atoms[bond.EndAtomId], molecule.Atoms[endLigand.AtomId]);
                    if ((beginSide > 0.000001 || beginSide < -0.000001) && (endSide > 0.000001 || endSide < -0.000001))
                    {
                        StereoRecord record = new StereoRecord();
                        record.SetDoubleBond(bond.Id, beginLigand.AtomId, endLigand.AtomId, GetCoordinateDoubleBondConfiguration(beginSide, endSide));
                        record.Id = molecule.StereoRecords.Count;
                        record.Source = "coordinates2d";
                        molecule.StereoRecords.Add(record);
                        result.Records.Add(record);
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    /// <summary>
    /// Perceives the from molfile wedges.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public StereoPerceptionResult PerceiveFromMolfileWedges(Molecule? molecule)
    {
        StereoPerceptionResult result = new StereoPerceptionResult();

        if (molecule == null)
        {
            result.Report.AddError("STEREO_NULL_MOLECULE", "Cannot perceive MOL stereo for a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        result.Success = true;

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (IsMolfileWedgeHashStereo(bond.Stereo))
            {
                if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
                {
                    result.Report.AddWarning("MOL_STEREO_INVALID_BOND", "MOL wedge/hash bond references invalid atoms.");
                }
                else
                {
                    ApplyMolfileBondDirection(bond);
                    if (!HasTetrahedralRecordForCenter(molecule, bond.BeginAtomId))
                    {
                        StereoRecord record = new StereoRecord();
                        FillTetrahedralRecordFromNeighbors(molecule, record, bond.BeginAtomId, GetMolfileChirality(bond.Stereo));
                        record.Id = molecule.StereoRecords.Count;
                        record.Source = "molfile";
                        molecule.StereoRecords.Add(record);
                        result.Records.Add(record);
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }

        return result;
    }

    /// <summary>
    /// Perceives the from SMILES.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public StereoPerceptionResult PerceiveFromSmiles(Molecule? molecule, string? smiles)
    {
        StereoPerceptionResult result = new StereoPerceptionResult();

        if (molecule == null)
        {
            result.Report.AddError("STEREO_NULL_MOLECULE", "Cannot perceive SMILES stereo for a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        result.Success = true;

        if (smiles == null)
        {
            result.Report.AddError("SMILES_STEREO_NULL", "Cannot perceive stereo from a null SMILES string.");
            result.Success = false;
            return result;
        }

        List<int> bracketAtomIds = new List<int>();
        List<Chirality> bracketChiralities = new List<Chirality>();
        ApplyDirectionalBondMarkers(molecule, smiles, bracketAtomIds, bracketChiralities);
        AddTetrahedralRecords(molecule, bracketAtomIds, bracketChiralities, result);
        AddDoubleBondRecords(molecule, result);
        return result;
    }

    internal bool IsMolfileWedgeHashStereo(BondStereo stereo)
    {
        if (stereo == BondStereo.Wedge || stereo == BondStereo.Hash || stereo == BondStereo.UpOrDown)
        {
            return true;
        }

        return false;
    }

    internal Chirality GetMolfileChirality(BondStereo stereo)
    {
        if (stereo == BondStereo.Hash)
        {
            return Chirality.CounterClockwise;
        }

        if (stereo == BondStereo.UpOrDown)
        {
            return Chirality.Unspecified;
        }

        return Chirality.Clockwise;
    }

    internal void ApplyMolfileBondDirection(Bond bond)
    {
        if (bond.Stereo == BondStereo.Wedge)
        {
            bond.Direction = BondDirection.Up;
        }
        else if (bond.Stereo == BondStereo.Hash)
        {
            bond.Direction = BondDirection.Down;
        }
        else if (bond.Stereo == BondStereo.UpOrDown)
        {
            bond.Direction = BondDirection.Either;
        }
    }

    internal bool HasTetrahedralRecordForCenter(Molecule molecule, int centerAtomId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record != null && record.IsTetrahedral() && record.CenterAtomId == centerAtomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasDoubleBondRecordForBond(Molecule molecule, int bondId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record != null && record.IsDoubleBond() && record.StereoBondId == bondId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AtomAndLigandsHaveCoordinates(Molecule molecule, int atomId, List<int> ligands)
    {
        if (!molecule.Atoms[atomId].HasCoordinates)
        {
            return false;
        }

        int index = 0;
        while (index < ligands.Count)
        {
            if (!molecule.Atoms[ligands[index]].HasCoordinates)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool DoubleBondAtomsAndLigandsHaveCoordinates(Molecule molecule, Bond bond, int beginLigandAtomId, int endLigandAtomId)
    {
        if (!molecule.Atoms[bond.BeginAtomId].HasCoordinates)
        {
            return false;
        }

        if (!molecule.Atoms[bond.EndAtomId].HasCoordinates)
        {
            return false;
        }

        if (!molecule.Atoms[beginLigandAtomId].HasCoordinates)
        {
            return false;
        }

        if (!molecule.Atoms[endLigandAtomId].HasCoordinates)
        {
            return false;
        }

        return true;
    }

    internal DirectionalLigand FindCoordinateLigand(Molecule molecule, int atomId, int excludedAtomId)
    {
        DirectionalLigand result = new DirectionalLigand();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && neighborAtomId != excludedAtomId)
            {
                result.Found = true;
                result.AtomId = neighborAtomId;
                result.BondId = bond.Id;
                result.Direction = BondDirection.None;
                return result;
            }

            bondIndex = bondIndex + 1;
        }

        return result;
    }

    internal double CalculateSignedTetrahedralVolume(Molecule molecule, int ligand0AtomId, int ligand1AtomId, int ligand2AtomId, int ligand3AtomId)
    {
        Atom a = molecule.Atoms[ligand0AtomId];
        Atom b = molecule.Atoms[ligand1AtomId];
        Atom c = molecule.Atoms[ligand2AtomId];
        Atom d = molecule.Atoms[ligand3AtomId];
        double ax = a.X - d.X;
        double ay = a.Y - d.Y;
        double az = a.Z - d.Z;
        double bx = b.X - d.X;
        double by = b.Y - d.Y;
        double bz = b.Z - d.Z;
        double cx = c.X - d.X;
        double cy = c.Y - d.Y;
        double cz = c.Z - d.Z;
        return ax * (by * cz - bz * cy) - ay * (bx * cz - bz * cx) + az * (bx * cy - by * cx);
    }

    internal double CalculateSide2D(Atom begin, Atom end, Atom point)
    {
        double axisX = end.X - begin.X;
        double axisY = end.Y - begin.Y;
        double pointX = point.X - begin.X;
        double pointY = point.Y - begin.Y;
        return axisX * pointY - axisY * pointX;
    }

    internal DoubleBondConfiguration GetCoordinateDoubleBondConfiguration(double beginSide, double endSide)
    {
        if (beginSide * endSide > 0.0)
        {
            return DoubleBondConfiguration.Cis;
        }

        return DoubleBondConfiguration.Trans;
    }

    internal void ApplyDirectionalBondMarkers(Molecule molecule, string smiles, List<int> bracketAtomIds, List<Chirality> bracketChiralities)
    {
        int atomId = 0;
        int previousAtomId = -1;
        BondDirection pendingDirection = BondDirection.None;
        int index = 0;

        while (index < smiles.Length)
        {
            char value = smiles[index];
            if (value == '/')
            {
                pendingDirection = BondDirection.Up;
                index = index + 1;
            }
            else if (value == '\\')
            {
                pendingDirection = BondDirection.Down;
                index = index + 1;
            }
            else if (value == '[')
            {
                int endIndex = FindBracketEnd(smiles, index);
                if (atomId < molecule.Atoms.Count)
                {
                    ApplyPendingDirection(molecule, previousAtomId, atomId, pendingDirection);
                    Chirality chirality = GetBracketChirality(smiles, index, endIndex);
                    if (chirality != Chirality.None)
                    {
                        bracketAtomIds.Add(atomId);
                        bracketChiralities.Add(chirality);
                    }

                    previousAtomId = atomId;
                    atomId = atomId + 1;
                }

                pendingDirection = BondDirection.None;
                index = endIndex + 1;
            }
            else if (IsAtomStart(smiles, index))
            {
                if (atomId < molecule.Atoms.Count)
                {
                    ApplyPendingDirection(molecule, previousAtomId, atomId, pendingDirection);
                    previousAtomId = atomId;
                    atomId = atomId + 1;
                }

                pendingDirection = BondDirection.None;
                index = index + GetAtomTokenLength(smiles, index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal void AddTetrahedralRecords(Molecule molecule, List<int> bracketAtomIds, List<Chirality> bracketChiralities, StereoPerceptionResult result)
    {
        int index = 0;
        while (index < bracketAtomIds.Count)
        {
            int centerAtomId = bracketAtomIds[index];
            StereoRecord record = new StereoRecord();
            FillTetrahedralRecordFromNeighbors(molecule, record, centerAtomId, bracketChiralities[index]);
            record.Id = molecule.StereoRecords.Count;
            record.Source = "smiles";
            molecule.StereoRecords.Add(record);
            result.Records.Add(record);
            index = index + 1;
        }
    }

    internal void AddDoubleBondRecords(Molecule molecule, StereoPerceptionResult result)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.Order == BondOrder.Double)
            {
                DirectionalLigand beginLigand = FindDirectionalLigand(molecule, bond.BeginAtomId, bond.EndAtomId);
                DirectionalLigand endLigand = FindDirectionalLigand(molecule, bond.EndAtomId, bond.BeginAtomId);
                if (beginLigand.Found && endLigand.Found)
                {
                    StereoRecord record = new StereoRecord();
                    record.SetDoubleBond(bond.Id, beginLigand.AtomId, endLigand.AtomId, GetDoubleBondConfiguration(beginLigand.Direction, endLigand.Direction));
                    record.Id = molecule.StereoRecords.Count;
                    record.Source = "smiles";
                    molecule.StereoRecords.Add(record);
                    result.Records.Add(record);
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void FillTetrahedralRecordFromNeighbors(Molecule molecule, StereoRecord record, int centerAtomId, Chirality chirality)
    {
        record.Kind = "tetrahedral";
        record.Type = StereoKind.Tetrahedral;
        record.IsPlaceholder = false;
        record.CenterAtomId = centerAtomId;
        record.Chirality = chirality;
        record.LigandAtomIds.Clear();

        List<int> neighbors = molecule.GetNeighborAtomIds(centerAtomId);
        int index = 0;
        while (index < neighbors.Count && record.LigandAtomIds.Count < 4)
        {
            record.LigandAtomIds.Add(neighbors[index]);
            index = index + 1;
        }

        while (record.LigandAtomIds.Count < 4)
        {
            record.LigandAtomIds.Add(-1);
        }
    }

    internal DirectionalLigand FindDirectionalLigand(Molecule molecule, int atomId, int excludedAtomId)
    {
        DirectionalLigand result = new DirectionalLigand();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && neighborAtomId != excludedAtomId)
            {
                if (bond.Direction == BondDirection.Up || bond.Direction == BondDirection.Down)
                {
                    result.Found = true;
                    result.AtomId = neighborAtomId;
                    result.BondId = bond.Id;
                    result.Direction = bond.Direction;
                    return result;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return result;
    }

    internal DoubleBondConfiguration GetDoubleBondConfiguration(BondDirection beginDirection, BondDirection endDirection)
    {
        if (beginDirection == endDirection)
        {
            return DoubleBondConfiguration.Trans;
        }

        return DoubleBondConfiguration.Cis;
    }

    internal void ApplyPendingDirection(Molecule molecule, int previousAtomId, int atomId, BondDirection direction)
    {
        if (direction == BondDirection.None)
        {
            return;
        }

        if (previousAtomId < 0)
        {
            return;
        }

        int bondId = molecule.GetBondIdBetween(previousAtomId, atomId);
        if (bondId >= 0)
        {
            molecule.Bonds[bondId].Direction = direction;
        }
    }

    internal Chirality GetBracketChirality(string smiles, int startIndex, int endIndex)
    {
        int index = startIndex;
        while (index <= endIndex && index < smiles.Length)
        {
            if (smiles[index] == '@')
            {
                if (index + 1 <= endIndex && smiles[index + 1] == '@')
                {
                    return Chirality.CounterClockwise;
                }

                return Chirality.Clockwise;
            }

            index = index + 1;
        }

        return Chirality.None;
    }

    internal int FindBracketEnd(string smiles, int startIndex)
    {
        int index = startIndex + 1;
        while (index < smiles.Length)
        {
            if (smiles[index] == ']')
            {
                return index;
            }

            index = index + 1;
        }

        return smiles.Length - 1;
    }

    internal bool IsAtomStart(string smiles, int index)
    {
        char value = smiles[index];
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value == 'c' || value == 'n' || value == 'o' || value == 'p' || value == 's')
        {
            return true;
        }

        return false;
    }

    internal int GetAtomTokenLength(string smiles, int index)
    {
        if (index + 1 < smiles.Length)
        {
            string two = smiles.Substring(index, 2);
            if (two == "Cl" || two == "Br")
            {
                return 2;
            }
        }

        return 1;
    }

    internal int GetNeighborAtomId(Bond bond, int atomId)
    {
        if (bond.BeginAtomId == atomId)
        {
            return bond.EndAtomId;
        }

        if (bond.EndAtomId == atomId)
        {
            return bond.BeginAtomId;
        }

        return -1;
    }
}
