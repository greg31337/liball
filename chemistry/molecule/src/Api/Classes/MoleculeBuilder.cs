using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Builds and edits molecular graphs while keeping atom and bond identifiers consistent.
/// </summary>
public class MoleculeBuilder : IMoleculeBuilder
{
    internal Molecule Molecule;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeBuilder"/> class.
    /// </summary>
    public MoleculeBuilder()
    {
        Molecule = new Molecule();
    }

    /// <summary>
    /// Replaces the editor contents with an independent copy of a molecule.
    /// </summary>
    /// <param name="molecule">The source molecule, or <c>null</c> to reset the editor.</param>
    public void Initialize(Molecule? molecule)
    {
        Molecule = CloneMolecule(molecule);
    }

    /// <summary>
    /// Creates a builder containing an independent copy of a molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>A builder containing a copy, or an empty builder when the input is <c>null</c>.</returns>
    internal static MoleculeBuilder CreateFrom(Molecule? molecule)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Initialize(molecule);
        return builder;
    }

    /// <summary>
    /// Adds the atom.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    public int AddAtom(AtomSpec spec)
    {
        int atomId = Molecule.Atoms.Count;
        Atom atom = new Atom(atomId, spec);
        Molecule.Atoms.Add(atom);
        Molecule.EnsureConformerCoordinateCounts();
        Molecule.ClearCaches();
        return atomId;
    }

    /// <summary>
    /// Adds the bond.
    /// </summary>
    /// <param name="beginAtomId">The begin atom identifier.</param>
    /// <param name="endAtomId">The end atom identifier.</param>
    /// <param name="spec">The spec.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    public int AddBond(int beginAtomId, int endAtomId, BondSpec spec)
    {
        if (!IsValidAtomId(beginAtomId) || !IsValidAtomId(endAtomId))
        {
            return -1;
        }

        int bondId = Molecule.Bonds.Count;
        Bond bond = new Bond(bondId, beginAtomId, endAtomId, spec);
        Molecule.Bonds.Add(bond);
        Molecule.ClearCaches();
        return bondId;
    }

    /// <summary>
    /// Removes the atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    public void RemoveAtom(int atomId)
    {
        if (!IsValidAtomId(atomId))
        {
            return;
        }

        int[] bondIdMap = CreateBondIdMapForRemovedAtom(atomId);

        int bondIndex = Molecule.Bonds.Count - 1;
        while (bondIndex >= 0)
        {
            Bond bond = Molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                Molecule.Bonds.RemoveAt(bondIndex);
            }

            bondIndex = bondIndex - 1;
        }

        Molecule.Atoms.RemoveAt(atomId);
        Molecule.RemoveAtomCoordinatesFromConformers(atomId);
        RemapStereoAfterAtomRemoval(atomId, bondIdMap);
        RemapResiduesAfterAtomRemoval(atomId);
        RemapModelsAfterAtomRemoval(atomId);

        int atomIndex = 0;
        while (atomIndex < Molecule.Atoms.Count)
        {
            Molecule.Atoms[atomIndex].Id = atomIndex;
            atomIndex = atomIndex + 1;
        }

        int index = 0;
        while (index < Molecule.Bonds.Count)
        {
            Bond bond = Molecule.Bonds[index];
            bond.Id = index;
            if (bond.BeginAtomId > atomId)
            {
                bond.BeginAtomId = bond.BeginAtomId - 1;
            }

            if (bond.EndAtomId > atomId)
            {
                bond.EndAtomId = bond.EndAtomId - 1;
            }

            index = index + 1;
        }

        Molecule.ClearCaches();
    }

    /// <summary>
    /// Removes the bond.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    public void RemoveBond(int bondId)
    {
        if (!IsValidBondId(bondId))
        {
            return;
        }

        int[] bondIdMap = CreateBondIdMapForRemovedBond(bondId);
        Molecule.Bonds.RemoveAt(bondId);
        RenumberBonds();
        RemapStereoBonds(bondIdMap);
        Molecule.ClearCaches();
    }

    /// <summary>
    /// Replace atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <param name="spec">The spec.</param>
    public void ReplaceAtom(int atomId, AtomSpec spec)
    {
        if (!IsValidAtomId(atomId))
        {
            return;
        }

        Atom oldAtom = Molecule.Atoms[atomId];
        Atom atom = new Atom(atomId, spec);
        atom.HasCoordinates = oldAtom.HasCoordinates;
        atom.X = oldAtom.X;
        atom.Y = oldAtom.Y;
        atom.Z = oldAtom.Z;
        CopyAtomFormatData(oldAtom, atom);
        Molecule.Atoms[atomId] = atom;
        Molecule.ClearCaches();
    }

    /// <summary>
    /// Normalizes atom, bond, stereo, residue, and group references to current storage order.
    /// </summary>
    public void Renumber()
    {
        List<int> oldAtomIds = CollectAtomIds();
        List<int> oldBondIds = CollectBondIds();
        int atomIndex = 0;
        while (atomIndex < Molecule.Atoms.Count)
        {
            Molecule.Atoms[atomIndex].Id = atomIndex;
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < Molecule.Bonds.Count)
        {
            Bond bond = Molecule.Bonds[bondIndex];
            bond.Id = bondIndex;
            bond.BeginAtomId = FindNewId(oldAtomIds, bond.BeginAtomId);
            bond.EndAtomId = FindNewId(oldAtomIds, bond.EndAtomId);
            bondIndex = bondIndex + 1;
        }

        RemapAssociatedIds(oldAtomIds, oldBondIds);
        Molecule.ClearCaches();
    }

    /// <summary>
    /// Returns the molecule currently held by the builder.
    /// </summary>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Build()
    {
        return Molecule;
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Clone()
    {
        return CloneMolecule(Molecule);
    }

    /// <summary>
    /// Appends a copy of another molecule and remaps its atom and bond identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    public void Merge(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        Molecule source = CloneMolecule(molecule);
        int atomOffset = Molecule.Atoms.Count;
        int bondOffset = Molecule.Bonds.Count;
        int stereoOffset = Molecule.StereoRecords.Count;
        int groupOffset = Molecule.StereoGroups.Groups.Count;
        int conformerOffset = Molecule.Conformers.Count;
        int residueOffset = Molecule.Residues.Count;
        int atomIndex = 0;
        while (atomIndex < source.Atoms.Count)
        {
            Atom atom = CloneAtom(source.Atoms[atomIndex], atomOffset + atomIndex);
            int sourceResidueIndex = FindResidueIndex(source.Residues, atom.ResidueId);
            if (sourceResidueIndex >= 0)
            {
                atom.ResidueId = residueOffset + sourceResidueIndex;
            }

            Molecule.Atoms.Add(atom);
            atomIndex = atomIndex + 1;
        }

        Molecule.EnsureConformerCoordinateCounts();

        int bondIndex = 0;
        while (bondIndex < source.Bonds.Count)
        {
            Bond sourceBond = source.Bonds[bondIndex];
            Bond bond = CloneBond(sourceBond, Molecule.Bonds.Count, sourceBond.BeginAtomId + atomOffset, sourceBond.EndAtomId + atomOffset);
            Molecule.Bonds.Add(bond);
            bondIndex = bondIndex + 1;
        }

        MergeConformers(source, atomOffset);
        MergeStereo(source, atomOffset, bondOffset, stereoOffset, groupOffset);
        MergeResiduesAndChains(source, atomOffset);
        MergePolymersAndModels(source, atomOffset, conformerOffset, residueOffset);
        MergeMetadata(source.Metadata);
        Molecule.ClearCaches();
    }

    /// <summary>
    /// Splits the components.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> SplitComponents()
    {
        Components components = new Components();
        return components.Split(Molecule);
    }

    internal void MergeConformers(Molecule source, int atomOffset)
    {
        int conformerIndex = 0;
        while (conformerIndex < source.Conformers.Count)
        {
            Conformer sourceConformer = source.Conformers[conformerIndex];
            Conformer conformer = new Conformer(Molecule.Atoms.Count);
            conformer.Id = Molecule.Conformers.Count;
            conformer.Name = sourceConformer.Name;
            conformer.Energy = sourceConformer.Energy;
            conformer.HasEnergy = sourceConformer.HasEnergy;
            int coordinateIndex = 0;
            while (coordinateIndex < sourceConformer.Coordinates.Count && coordinateIndex < source.Atoms.Count)
            {
                conformer.Coordinates[atomOffset + coordinateIndex] = sourceConformer.Coordinates[coordinateIndex].Clone();
                coordinateIndex = coordinateIndex + 1;
            }

            Molecule.Conformers.Add(conformer);
            conformerIndex = conformerIndex + 1;
        }
    }

    internal void MergeStereo(Molecule source, int atomOffset, int bondOffset, int stereoOffset, int groupOffset)
    {
        int stereoIndex = 0;
        while (stereoIndex < source.StereoRecords.Count)
        {
            StereoRecord record = CloneStereoRecord(source.StereoRecords[stereoIndex], stereoOffset + stereoIndex);
            RemapStereoForOffset(record, atomOffset, bondOffset, groupOffset);
            Molecule.StereoRecords.Add(record);
            stereoIndex = stereoIndex + 1;
        }

        int groupIndex = 0;
        while (groupIndex < source.StereoGroups.Groups.Count)
        {
            EnhancedStereoGroup sourceGroup = source.StereoGroups.Groups[groupIndex];
            EnhancedStereoGroup group = Molecule.StereoGroups.AddGroup(sourceGroup.Type, sourceGroup.Source, sourceGroup.Label);
            int recordIndex = 0;
            while (recordIndex < sourceGroup.StereoRecordIds.Count)
            {
                group.StereoRecordIds.Add(stereoOffset + sourceGroup.StereoRecordIds[recordIndex]);
                recordIndex = recordIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal void MergeResiduesAndChains(Molecule source, int atomOffset)
    {
        int residueOffset = Molecule.Residues.Count;
        int residueIndex = 0;
        while (residueIndex < source.Residues.Count)
        {
            Residue residue = CloneResidue(source.Residues[residueIndex], atomOffset);
            residue.Id = residueOffset + residueIndex;
            Molecule.Residues.Add(residue);
            residueIndex = residueIndex + 1;
        }

        int chainIndex = 0;
        while (chainIndex < source.Chains.Count)
        {
            Chain sourceChain = source.Chains[chainIndex];
            Chain chain = new Chain();
            chain.Id = sourceChain.Id;
            int memberIndex = 0;
            while (memberIndex < sourceChain.ResidueIds.Count)
            {
                int sourceIndex = FindResidueIndex(source.Residues, sourceChain.ResidueIds[memberIndex]);
                if (sourceIndex >= 0)
                {
                    chain.ResidueIds.Add(residueOffset + sourceIndex);
                }

                memberIndex = memberIndex + 1;
            }

            Molecule.Chains.Add(chain);
            chainIndex = chainIndex + 1;
        }
    }

    internal void MergeMetadata(MoleculeMetadata metadata)
    {
        MoleculeMetadata.CopyStrings(metadata.Names, Molecule.Metadata.Names);
        MoleculeMetadata.CopyStrings(metadata.Comments, Molecule.Metadata.Comments);
        MoleculeMetadata.CopyFields(metadata.SdfFields, Molecule.Metadata.SdfFields);
        MoleculeMetadata.CopyFields(metadata.PdbFields, Molecule.Metadata.PdbFields);
        MoleculeMetadata.CopyFields(metadata.VendorFields, Molecule.Metadata.VendorFields);
        MoleculeMetadata.CopyFields(metadata.StringValues, Molecule.Metadata.StringValues);
    }

    internal void MergePolymersAndModels(Molecule source, int atomOffset, int conformerOffset, int residueOffset)
    {
        int modelIndex = 0;
        while (modelIndex < source.Models.Count)
        {
            Molecule.Models.Add(CloneModel(source.Models[modelIndex], atomOffset, conformerOffset));
            modelIndex = modelIndex + 1;
        }

        int polymerIndex = 0;
        while (polymerIndex < source.Polymers.Count)
        {
            Polymer sourcePolymer = source.Polymers[polymerIndex];
            Polymer polymer = ClonePolymer(sourcePolymer);
            polymer.ResidueIds.Clear();
            int residueIndex = 0;
            while (residueIndex < sourcePolymer.ResidueIds.Count)
            {
                int sourceIndex = FindResidueIndex(source.Residues, sourcePolymer.ResidueIds[residueIndex]);
                if (sourceIndex >= 0)
                {
                    polymer.ResidueIds.Add(residueOffset + sourceIndex);
                }

                residueIndex = residueIndex + 1;
            }

            Molecule.Polymers.Add(polymer);
            polymerIndex = polymerIndex + 1;
        }
    }

    internal bool IsValidAtomId(int atomId)
    {
        return atomId >= 0 && atomId < Molecule.Atoms.Count;
    }

    internal bool IsValidBondId(int bondId)
    {
        return bondId >= 0 && bondId < Molecule.Bonds.Count;
    }

    internal int[] CreateBondIdMapForRemovedAtom(int atomId)
    {
        int[] map = new int[Molecule.Bonds.Count];
        int nextId = 0;
        int index = 0;
        while (index < Molecule.Bonds.Count)
        {
            Bond bond = Molecule.Bonds[index];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                map[index] = -1;
            }
            else
            {
                map[index] = nextId;
                nextId = nextId + 1;
            }

            index = index + 1;
        }

        return map;
    }

    internal int[] CreateBondIdMapForRemovedBond(int removedBondId)
    {
        int[] map = new int[Molecule.Bonds.Count];
        int index = 0;
        while (index < map.Length)
        {
            map[index] = index < removedBondId ? index : index - 1;
            index = index + 1;
        }

        map[removedBondId] = -1;
        return map;
    }

    internal void RemapStereoAfterAtomRemoval(int atomId, int[] bondIdMap)
    {
        int index = Molecule.StereoRecords.Count - 1;
        while (index >= 0)
        {
            StereoRecord record = Molecule.StereoRecords[index];
            if (StereoReferencesAtom(record, atomId) || StereoBondWasRemoved(record, bondIdMap))
            {
                RemoveStereoRecordAt(index);
            }
            else
            {
                record.CenterAtomId = DecrementIdAfter(record.CenterAtomId, atomId);
                record.BeginLigandAtomId = DecrementIdAfter(record.BeginLigandAtomId, atomId);
                record.EndLigandAtomId = DecrementIdAfter(record.EndLigandAtomId, atomId);
                int ligandIndex = 0;
                while (ligandIndex < record.LigandAtomIds.Count)
                {
                    record.LigandAtomIds[ligandIndex] = DecrementIdAfter(record.LigandAtomIds[ligandIndex], atomId);
                    ligandIndex = ligandIndex + 1;
                }

                record.StereoBondId = MapOptionalId(record.StereoBondId, bondIdMap);
            }

            index = index - 1;
        }

        NormalizeStereoGroups();
    }

    internal void RemapStereoBonds(int[] bondIdMap)
    {
        int index = Molecule.StereoRecords.Count - 1;
        while (index >= 0)
        {
            StereoRecord record = Molecule.StereoRecords[index];
            if (StereoBondWasRemoved(record, bondIdMap))
            {
                RemoveStereoRecordAt(index);
            }
            else
            {
                record.StereoBondId = MapOptionalId(record.StereoBondId, bondIdMap);
            }

            index = index - 1;
        }

        NormalizeStereoGroups();
    }

    internal void RemapResiduesAfterAtomRemoval(int atomId)
    {
        int residueIndex = 0;
        while (residueIndex < Molecule.Residues.Count)
        {
            Residue residue = Molecule.Residues[residueIndex];
            int memberIndex = residue.AtomIds.Count - 1;
            while (memberIndex >= 0)
            {
                if (residue.AtomIds[memberIndex] == atomId)
                {
                    residue.AtomIds.RemoveAt(memberIndex);
                }
                else
                {
                    residue.AtomIds[memberIndex] = DecrementIdAfter(residue.AtomIds[memberIndex], atomId);
                }

                memberIndex = memberIndex - 1;
            }

            if (residue.RootAtomId == atomId)
            {
                if (residue.AtomIds.Count > 0)
                {
                    residue.RootAtomId = residue.AtomIds[0];
                }
                else
                {
                    residue.RootAtomId = -1;
                }
            }
            else
            {
                residue.RootAtomId = DecrementIdAfter(residue.RootAtomId, atomId);
            }

            residueIndex = residueIndex + 1;
        }
    }

    internal bool StereoReferencesAtom(StereoRecord record, int atomId)
    {
        if (record.CenterAtomId == atomId || record.BeginLigandAtomId == atomId || record.EndLigandAtomId == atomId)
        {
            return true;
        }

        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            if (record.LigandAtomIds[index] == atomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool StereoBondWasRemoved(StereoRecord record, int[] bondIdMap)
    {
        if (record.StereoBondId < 0)
        {
            return false;
        }

        return record.StereoBondId >= bondIdMap.Length || bondIdMap[record.StereoBondId] < 0;
    }

    internal void RemoveStereoRecordAt(int stereoRecordId)
    {
        Molecule.StereoRecords.RemoveAt(stereoRecordId);
        int groupIndex = 0;
        while (groupIndex < Molecule.StereoGroups.Groups.Count)
        {
            List<int> ids = Molecule.StereoGroups.Groups[groupIndex].StereoRecordIds;
            int index = ids.Count - 1;
            while (index >= 0)
            {
                if (ids[index] == stereoRecordId)
                {
                    ids.RemoveAt(index);
                }
                else if (ids[index] > stereoRecordId)
                {
                    ids[index] = ids[index] - 1;
                }

                index = index - 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal void NormalizeStereoGroups()
    {
        int groupIndex = Molecule.StereoGroups.Groups.Count - 1;
        while (groupIndex >= 0)
        {
            if (Molecule.StereoGroups.Groups[groupIndex].StereoRecordIds.Count == 0)
            {
                Molecule.StereoGroups.Groups.RemoveAt(groupIndex);
            }

            groupIndex = groupIndex - 1;
        }

        NormalizeStereoRecordAndGroupIds();
    }

    internal void NormalizeStereoRecordAndGroupIds()
    {
        int recordIndex = 0;
        while (recordIndex < Molecule.StereoRecords.Count)
        {
            Molecule.StereoRecords[recordIndex].Id = recordIndex;
            Molecule.StereoRecords[recordIndex].GroupId = -1;
            recordIndex = recordIndex + 1;
        }

        int groupIndex = 0;
        while (groupIndex < Molecule.StereoGroups.Groups.Count)
        {
            EnhancedStereoGroup group = Molecule.StereoGroups.Groups[groupIndex];
            group.Id = groupIndex;
            int memberIndex = 0;
            while (memberIndex < group.StereoRecordIds.Count)
            {
                int id = group.StereoRecordIds[memberIndex];
                if (id >= 0 && id < Molecule.StereoRecords.Count)
                {
                    Molecule.StereoRecords[id].GroupId = groupIndex;
                }

                memberIndex = memberIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal int DecrementIdAfter(int id, int removedId)
    {
        return id > removedId ? id - 1 : id;
    }

    internal int MapOptionalId(int id, int[] map)
    {
        if (id < 0)
        {
            return id;
        }

        if (id >= map.Length)
        {
            return -1;
        }

        return map[id];
    }

    internal List<int> CollectAtomIds()
    {
        List<int> ids = new List<int>();
        int index = 0;
        while (index < Molecule.Atoms.Count)
        {
            ids.Add(Molecule.Atoms[index].Id);
            index = index + 1;
        }

        return ids;
    }

    internal List<int> CollectBondIds()
    {
        List<int> ids = new List<int>();
        int index = 0;
        while (index < Molecule.Bonds.Count)
        {
            ids.Add(Molecule.Bonds[index].Id);
            index = index + 1;
        }

        return ids;
    }

    internal int FindNewId(List<int> oldIds, int oldId)
    {
        if (oldId < 0)
        {
            return oldId;
        }

        int index = 0;
        while (index < oldIds.Count)
        {
            if (oldIds[index] == oldId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void RemapAssociatedIds(List<int> oldAtomIds, List<int> oldBondIds)
    {
        List<int> oldStereoIds = new List<int>();
        int recordIndex = 0;
        while (recordIndex < Molecule.StereoRecords.Count)
        {
            StereoRecord record = Molecule.StereoRecords[recordIndex];
            oldStereoIds.Add(record.Id);
            record.CenterAtomId = FindNewId(oldAtomIds, record.CenterAtomId);
            record.BeginLigandAtomId = FindNewId(oldAtomIds, record.BeginLigandAtomId);
            record.EndLigandAtomId = FindNewId(oldAtomIds, record.EndLigandAtomId);
            record.StereoBondId = FindNewId(oldBondIds, record.StereoBondId);
            int ligandIndex = 0;
            while (ligandIndex < record.LigandAtomIds.Count)
            {
                record.LigandAtomIds[ligandIndex] = FindNewId(oldAtomIds, record.LigandAtomIds[ligandIndex]);
                ligandIndex = ligandIndex + 1;
            }

            recordIndex = recordIndex + 1;
        }

        RemapResidueAtomIds(oldAtomIds);
        RemapModelAtomIds(oldAtomIds);
        RemapStereoGroupRecordIds(oldStereoIds);
        NormalizeStereoGroups();
    }

    internal void RemapResidueAtomIds(List<int> oldAtomIds)
    {
        int residueIndex = 0;
        while (residueIndex < Molecule.Residues.Count)
        {
            Residue residue = Molecule.Residues[residueIndex];
            residue.RootAtomId = FindNewId(oldAtomIds, residue.RootAtomId);
            int atomIndex = 0;
            while (atomIndex < residue.AtomIds.Count)
            {
                residue.AtomIds[atomIndex] = FindNewId(oldAtomIds, residue.AtomIds[atomIndex]);
                atomIndex = atomIndex + 1;
            }

            residueIndex = residueIndex + 1;
        }
    }

    internal void RemapModelsAfterAtomRemoval(int atomId)
    {
        int modelIndex = 0;
        while (modelIndex < Molecule.Models.Count)
        {
            List<int> atomIds = Molecule.Models[modelIndex].AtomIds;
            int index = atomIds.Count - 1;
            while (index >= 0)
            {
                if (atomIds[index] == atomId)
                {
                    atomIds.RemoveAt(index);
                }
                else
                {
                    atomIds[index] = DecrementIdAfter(atomIds[index], atomId);
                }

                index = index - 1;
            }

            modelIndex = modelIndex + 1;
        }
    }

    internal void RemapModelAtomIds(List<int> oldAtomIds)
    {
        int modelIndex = 0;
        while (modelIndex < Molecule.Models.Count)
        {
            List<int> atomIds = Molecule.Models[modelIndex].AtomIds;
            int atomIndex = atomIds.Count - 1;
            while (atomIndex >= 0)
            {
                int mappedId = FindNewId(oldAtomIds, atomIds[atomIndex]);
                if (mappedId < 0)
                {
                    atomIds.RemoveAt(atomIndex);
                }
                else
                {
                    atomIds[atomIndex] = mappedId;
                }

                atomIndex = atomIndex - 1;
            }

            modelIndex = modelIndex + 1;
        }
    }

    internal void RemapStereoGroupRecordIds(List<int> oldStereoIds)
    {
        int groupIndex = 0;
        while (groupIndex < Molecule.StereoGroups.Groups.Count)
        {
            List<int> ids = Molecule.StereoGroups.Groups[groupIndex].StereoRecordIds;
            int recordIndex = ids.Count - 1;
            while (recordIndex >= 0)
            {
                int mappedId = FindNewId(oldStereoIds, ids[recordIndex]);
                if (mappedId < 0)
                {
                    ids.RemoveAt(recordIndex);
                }
                else
                {
                    ids[recordIndex] = mappedId;
                }

                recordIndex = recordIndex - 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal void RemapStereoForOffset(StereoRecord record, int atomOffset, int bondOffset, int groupOffset)
    {
        record.CenterAtomId = OffsetId(record.CenterAtomId, atomOffset);
        record.BeginLigandAtomId = OffsetId(record.BeginLigandAtomId, atomOffset);
        record.EndLigandAtomId = OffsetId(record.EndLigandAtomId, atomOffset);
        record.StereoBondId = OffsetId(record.StereoBondId, bondOffset);
        record.GroupId = OffsetId(record.GroupId, groupOffset);
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            record.LigandAtomIds[index] = OffsetId(record.LigandAtomIds[index], atomOffset);
            index = index + 1;
        }
    }

    internal int FindResidueIndex(List<Residue> residues, int residueId)
    {
        int index = 0;
        while (index < residues.Count)
        {
            if (residues[index].Id == residueId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void RenumberBonds()
    {
        int index = 0;
        while (index < Molecule.Bonds.Count)
        {
            Molecule.Bonds[index].Id = index;
            index = index + 1;
        }
    }

    internal Molecule CreateComponent(int startAtomId, bool[] visited)
    {
        Molecule component = new Molecule();
        List<int> atomIds = new List<int>();
        List<int> queue = new List<int>();
        int[] oldToNewAtomIds = new int[Molecule.Atoms.Count];
        int index = 0;

        while (index < oldToNewAtomIds.Length)
        {
            oldToNewAtomIds[index] = -1;
            index = index + 1;
        }

        visited[startAtomId] = true;
        queue.Add(startAtomId);

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int atomId = queue[queueIndex];
            atomIds.Add(atomId);

            int bondIndex = 0;
            while (bondIndex < Molecule.Bonds.Count)
            {
                Bond bond = Molecule.Bonds[bondIndex];
                int neighborAtomId = -1;

                if (bond.BeginAtomId == atomId)
                {
                    neighborAtomId = bond.EndAtomId;
                }

                if (bond.EndAtomId == atomId)
                {
                    neighborAtomId = bond.BeginAtomId;
                }

                if (neighborAtomId >= 0 && !visited[neighborAtomId])
                {
                    visited[neighborAtomId] = true;
                    queue.Add(neighborAtomId);
                }

                bondIndex = bondIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        index = 0;
        while (index < atomIds.Count)
        {
            int oldAtomId = atomIds[index];
            int newAtomId = component.Atoms.Count;
            oldToNewAtomIds[oldAtomId] = newAtomId;
            component.Atoms.Add(CloneAtom(Molecule.Atoms[oldAtomId], newAtomId));
            index = index + 1;
        }

        int sourceBondIndex = 0;
        while (sourceBondIndex < Molecule.Bonds.Count)
        {
            Bond sourceBond = Molecule.Bonds[sourceBondIndex];
            if (oldToNewAtomIds[sourceBond.BeginAtomId] >= 0 && oldToNewAtomIds[sourceBond.EndAtomId] >= 0)
            {
                Bond bond = CloneBond(sourceBond, component.Bonds.Count, oldToNewAtomIds[sourceBond.BeginAtomId], oldToNewAtomIds[sourceBond.EndAtomId]);
                component.Bonds.Add(bond);
            }

            sourceBondIndex = sourceBondIndex + 1;
        }

        return component;
    }

    internal static Molecule CloneMolecule(Molecule? source)
    {
        Molecule molecule = new Molecule();

        if (source == null)
        {
            return molecule;
        }

        if (source.Metadata == null)
        {
            molecule.Metadata = new MoleculeMetadata();
        }
        else
        {
            molecule.Metadata = source.Metadata.Clone();
        }

        if (source.StereoGroups == null)
        {
            molecule.StereoGroups = new StereoGroups();
        }
        else
        {
            molecule.StereoGroups = source.StereoGroups.Clone();
        }

        int atomIndex = 0;
        while (atomIndex < source.Atoms.Count)
        {
            molecule.Atoms.Add(CloneAtom(source.Atoms[atomIndex], atomIndex));
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < source.Bonds.Count)
        {
            Bond sourceBond = source.Bonds[bondIndex];
            molecule.Bonds.Add(CloneBond(sourceBond, bondIndex, sourceBond.BeginAtomId, sourceBond.EndAtomId));
            bondIndex = bondIndex + 1;
        }

        int conformerIndex = 0;
        while (conformerIndex < source.Conformers.Count)
        {
            Conformer conformer = source.Conformers[conformerIndex].Clone();
            conformer.Id = conformerIndex;
            conformer.EnsureCoordinateCount(molecule.Atoms.Count);
            molecule.Conformers.Add(conformer);
            conformerIndex = conformerIndex + 1;
        }

        int stereoIndex = 0;
        while (stereoIndex < source.StereoRecords.Count)
        {
            molecule.StereoRecords.Add(CloneStereoRecord(source.StereoRecords[stereoIndex], stereoIndex));
            stereoIndex = stereoIndex + 1;
        }

        int residueIndex = 0;
        while (residueIndex < source.Residues.Count)
        {
            molecule.Residues.Add(CloneResidue(source.Residues[residueIndex], 0));
            residueIndex = residueIndex + 1;
        }

        int chainIndex = 0;
        while (chainIndex < source.Chains.Count)
        {
            molecule.Chains.Add(CloneChain(source.Chains[chainIndex]));
            chainIndex = chainIndex + 1;
        }

        int polymerIndex = 0;
        while (polymerIndex < source.Polymers.Count)
        {
            molecule.Polymers.Add(ClonePolymer(source.Polymers[polymerIndex]));
            polymerIndex = polymerIndex + 1;
        }

        int modelIndex = 0;
        while (modelIndex < source.Models.Count)
        {
            molecule.Models.Add(CloneModel(source.Models[modelIndex], 0, 0));
            modelIndex = modelIndex + 1;
        }

        molecule.ClearCaches();
        return molecule;
    }

    internal static Atom CloneAtom(Atom? source, int atomId)
    {
        Atom atom = Atom.CreateEmpty();

        if (source == null)
        {
            atom.Id = atomId;
            return atom;
        }

        atom.Id = atomId;
        atom.AtomicNumber = source.AtomicNumber;
        atom.IsotopeMassNumber = source.IsotopeMassNumber;
        atom.FormalCharge = source.FormalCharge;
        atom.ExplicitHydrogenCount = source.ExplicitHydrogenCount;
        atom.ImplicitHydrogenCount = source.ImplicitHydrogenCount;
        atom.RadicalElectronCount = source.RadicalElectronCount;
        atom.AtomMapNumber = source.AtomMapNumber;
        atom.IsAromatic = source.IsAromatic;
        atom.QueryOperation = source.QueryOperation;
        atom.QueryValue = source.QueryValue;
        atom.Alias = source.Alias;
        atom.Label = source.Label;
        atom.HasCoordinates = source.HasCoordinates;
        atom.X = source.X;
        atom.Y = source.Y;
        atom.Z = source.Z;
        CopyAtomFormatData(source, atom);
        return atom;
    }

    internal static void CopyAtomFormatData(Atom source, Atom target)
    {
        target.AtomType = source.AtomType;
        target.PartialCharge = source.PartialCharge;
        target.HasPartialCharge = source.HasPartialCharge;
        target.ResidueId = source.ResidueId;
        target.ResidueName = source.ResidueName;
        target.ChainId = source.ChainId;
        target.ModelId = source.ModelId;
        target.Occupancy = source.Occupancy;
        target.HasOccupancy = source.HasOccupancy;
        target.TemperatureFactor = source.TemperatureFactor;
        target.HasTemperatureFactor = source.HasTemperatureFactor;
        target.AlternateLocation = source.AlternateLocation;
        target.InsertionCode = source.InsertionCode;
    }

    internal static Bond CloneBond(Bond? source, int bondId, int beginAtomId, int endAtomId)
    {
        Bond bond = Bond.CreateEmpty();

        if (source == null)
        {
            bond.Id = bondId;
            bond.BeginAtomId = beginAtomId;
            bond.EndAtomId = endAtomId;
            return bond;
        }

        bond.Id = bondId;
        bond.BeginAtomId = beginAtomId;
        bond.EndAtomId = endAtomId;
        bond.Order = source.Order;
        bond.IsAromatic = source.IsAromatic;
        bond.Stereo = source.Stereo;
        bond.Direction = source.Direction;
        bond.IsDative = source.IsDative;
        bond.QueryOperation = source.QueryOperation;
        bond.QueryValue = source.QueryValue;
        bond.SourceFlags = source.SourceFlags;
        return bond;
    }

    internal static StereoRecord CloneStereoRecord(StereoRecord? source, int stereoId)
    {
        StereoRecord record = new StereoRecord();

        if (source == null)
        {
            record.Id = stereoId;
            return record;
        }

        record.Id = stereoId;
        record.Kind = source.Kind;
        record.Type = source.Type;
        record.IsPlaceholder = source.IsPlaceholder;
        record.CenterAtomId = source.CenterAtomId;
        record.Chirality = source.Chirality;
        record.StereoBondId = source.StereoBondId;
        record.BeginLigandAtomId = source.BeginLigandAtomId;
        record.EndLigandAtomId = source.EndLigandAtomId;
        record.Configuration = source.Configuration;
        record.GroupId = source.GroupId;
        record.Source = source.Source;

        int ligandIndex = 0;
        while (ligandIndex < source.LigandAtomIds.Count)
        {
            record.LigandAtomIds.Add(source.LigandAtomIds[ligandIndex]);
            ligandIndex = ligandIndex + 1;
        }

        return record;
    }

    internal static Residue CloneResidue(Residue source, int atomOffset)
    {
        Residue residue = new Residue();
        residue.Id = source.Id;
        residue.Name = source.Name;
        residue.ChainId = source.ChainId;
        residue.Type = source.Type;
        residue.InsertionCode = source.InsertionCode;
        residue.ModelId = source.ModelId;
        residue.RootAtomId = OffsetId(source.RootAtomId, atomOffset);
        int index = 0;
        while (index < source.AtomIds.Count)
        {
            residue.AtomIds.Add(OffsetId(source.AtomIds[index], atomOffset));
            index = index + 1;
        }

        return residue;
    }

    internal static Chain CloneChain(Chain source)
    {
        Chain chain = new Chain();
        chain.Id = source.Id;
        chain.ModelId = source.ModelId;
        int index = 0;
        while (index < source.ResidueIds.Count)
        {
            chain.ResidueIds.Add(source.ResidueIds[index]);
            index = index + 1;
        }

        return chain;
    }

    internal static Polymer ClonePolymer(Polymer source)
    {
        Polymer polymer = new Polymer();
        polymer.Id = source.Id;
        polymer.Name = source.Name;
        polymer.Type = source.Type;
        int index = 0;
        while (index < source.ChainIds.Count)
        {
            polymer.ChainIds.Add(source.ChainIds[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.ResidueIds.Count)
        {
            polymer.ResidueIds.Add(source.ResidueIds[index]);
            index = index + 1;
        }

        return polymer;
    }

    internal static MoleculeModel CloneModel(MoleculeModel source, int atomOffset, int conformerOffset)
    {
        MoleculeModel model = new MoleculeModel();
        model.Id = source.Id;
        model.Name = source.Name;
        int index = 0;
        while (index < source.AtomIds.Count)
        {
            model.AtomIds.Add(OffsetId(source.AtomIds[index], atomOffset));
            index = index + 1;
        }

        index = 0;
        while (index < source.ConformerIds.Count)
        {
            model.ConformerIds.Add(OffsetId(source.ConformerIds[index], conformerOffset));
            index = index + 1;
        }

        return model;
    }

    internal static int OffsetId(int id, int offset)
    {
        if (id < 0)
        {
            return id;
        }

        return id + offset;
    }
}
