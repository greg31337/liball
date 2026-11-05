using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Provides components data and operations for molecular graphs.
/// </summary>
public class Components : IComponents
{
    /// <summary>
    /// Initializes a connected-component analyzer.
    /// </summary>
    public Components()
    {
    }

    /// <summary>
    /// Finds the components.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<ComponentInfo> FindComponents(Molecule? molecule)
    {
        List<ComponentInfo> components = new List<ComponentInfo>();

        if (molecule == null)
        {
            return components;
        }

        bool[] visited = new bool[molecule.Atoms.Count];
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (!visited[atomIndex])
            {
                ComponentInfo component = FindComponent(molecule, atomIndex, visited, components.Count);
                components.Add(component);
            }

            atomIndex = atomIndex + 1;
        }

        return components;
    }

    internal ComponentInfo FindComponent(Molecule molecule, int startAtomId, bool[] visited, int componentId)
    {
        ComponentInfo component = new ComponentInfo();
        component.ComponentId = componentId;
        component.FirstAtomId = startAtomId;

        List<int> queue = new List<int>();
        visited[startAtomId] = true;
        queue.Add(startAtomId);

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int atomId = queue[queueIndex];
            component.AtomIds.Add(atomId);

            int bondIndex = 0;
            while (bondIndex < molecule.Bonds.Count)
            {
                Bond bond = molecule.Bonds[bondIndex];
                int neighborAtomId = -1;

                if (bond.BeginAtomId == atomId)
                {
                    neighborAtomId = bond.EndAtomId;
                }

                if (bond.EndAtomId == atomId)
                {
                    neighborAtomId = bond.BeginAtomId;
                }

                if (neighborAtomId >= 0)
                {
                    AddUniqueInt(component.BondIds, bond.Id);

                    if (!visited[neighborAtomId])
                    {
                        visited[neighborAtomId] = true;
                        queue.Add(neighborAtomId);
                    }
                }

                bondIndex = bondIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        FillComponentSummary(molecule, component);
        return component;
    }

    internal void FillComponentSummary(Molecule molecule, ComponentInfo component)
    {
        component.AtomCount = component.AtomIds.Count;
        component.BondCount = component.BondIds.Count;
        component.HeavyAtomCount = 0;

        int index = 0;
        while (index < component.AtomIds.Count)
        {
            Atom atom = molecule.Atoms[component.AtomIds[index]];
            if (atom.AtomicNumber > 1)
            {
                component.HeavyAtomCount = component.HeavyAtomCount + 1;
            }

            index = index + 1;
        }

        component.AtomKey = CreateAtomKey(molecule, component);
    }

    /// <summary>
    /// Returns the component identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<int> GetComponentIds(Molecule? molecule)
    {
        List<int> ids = new List<int>();

        if (molecule == null)
        {
            return ids;
        }

        if (molecule.IsComponentCacheValid && molecule.ComponentIdsByAtomId.Count == molecule.Atoms.Count)
        {
            int cachedIndex = 0;
            while (cachedIndex < molecule.ComponentIdsByAtomId.Count)
            {
                ids.Add(molecule.ComponentIdsByAtomId[cachedIndex]);
                cachedIndex = cachedIndex + 1;
            }

            return ids;
        }

        List<ComponentInfo> components = FindComponents(molecule);
        molecule.ComponentIdsByAtomId.Clear();

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            molecule.ComponentIdsByAtomId.Add(-1);
            atomIndex = atomIndex + 1;
        }

        int componentIndex = 0;
        while (componentIndex < components.Count)
        {
            ComponentInfo component = components[componentIndex];
            int memberIndex = 0;
            while (memberIndex < component.AtomIds.Count)
            {
                molecule.ComponentIdsByAtomId[component.AtomIds[memberIndex]] = component.ComponentId;
                memberIndex = memberIndex + 1;
            }

            componentIndex = componentIndex + 1;
        }

        molecule.IsComponentCacheValid = true;

        int index = 0;
        while (index < molecule.ComponentIdsByAtomId.Count)
        {
            ids.Add(molecule.ComponentIdsByAtomId[index]);
            index = index + 1;
        }

        return ids;
    }

    internal List<ComponentInfo> OrderComponents(List<ComponentInfo> components)
    {
        List<ComponentInfo> ordered = new List<ComponentInfo>();
        int copyIndex = 0;
        while (copyIndex < components.Count)
        {
            ordered.Add(components[copyIndex]);
            copyIndex = copyIndex + 1;
        }

        int index = 1;
        while (index < ordered.Count)
        {
            ComponentInfo value = ordered[index];
            int scan = index - 1;

            while (scan >= 0 && CompareComponents(ordered[scan], value) > 0)
            {
                ordered[scan + 1] = ordered[scan];
                scan = scan - 1;
            }

            ordered[scan + 1] = value;
            index = index + 1;
        }

        return ordered;
    }

    internal int CompareComponents(ComponentInfo left, ComponentInfo right)
    {
        if (left.HeavyAtomCount != right.HeavyAtomCount)
        {
            return right.HeavyAtomCount - left.HeavyAtomCount;
        }

        if (left.AtomCount != right.AtomCount)
        {
            return right.AtomCount - left.AtomCount;
        }

        if (left.BondCount != right.BondCount)
        {
            return right.BondCount - left.BondCount;
        }

        int atomKeyComparison = string.CompareOrdinal(left.AtomKey, right.AtomKey);
        if (atomKeyComparison != 0)
        {
            return atomKeyComparison;
        }

        return left.FirstAtomId - right.FirstAtomId;
    }

    /// <summary>
    /// Split.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> Split(Molecule? molecule)
    {
        List<Molecule> fragments = new List<Molecule>();
        if (molecule == null)
        {
            return fragments;
        }

        List<ComponentInfo> components = OrderComponents(FindComponents(molecule));

        int index = 0;
        while (index < components.Count)
        {
            fragments.Add(CreateMoleculeForComponent(molecule, components[index]));
            index = index + 1;
        }

        return fragments;
    }

    internal Molecule CreateMoleculeForComponent(Molecule molecule, ComponentInfo component)
    {
        Molecule fragment = new Molecule();
        int[] oldToNewAtomIds = new int[molecule.Atoms.Count];
        int[] oldToNewBondIds = new int[molecule.Bonds.Count];
        int index = 0;

        while (index < oldToNewAtomIds.Length)
        {
            oldToNewAtomIds[index] = -1;
            index = index + 1;
        }

        index = 0;
        while (index < oldToNewBondIds.Length)
        {
            oldToNewBondIds[index] = -1;
            index = index + 1;
        }

        index = 0;
        while (index < component.AtomIds.Count)
        {
            int oldAtomId = component.AtomIds[index];
            int newAtomId = fragment.Atoms.Count;
            oldToNewAtomIds[oldAtomId] = newAtomId;
            fragment.Atoms.Add(MoleculeBuilder.CloneAtom(molecule.Atoms[oldAtomId], newAtomId));
            index = index + 1;
        }

        index = 0;
        while (index < component.BondIds.Count)
        {
            int oldBondId = component.BondIds[index];
            Bond sourceBond = molecule.Bonds[oldBondId];
            int beginAtomId = oldToNewAtomIds[sourceBond.BeginAtomId];
            int endAtomId = oldToNewAtomIds[sourceBond.EndAtomId];
            if (beginAtomId >= 0 && endAtomId >= 0)
            {
                oldToNewBondIds[oldBondId] = fragment.Bonds.Count;
                fragment.Bonds.Add(MoleculeBuilder.CloneBond(sourceBond, fragment.Bonds.Count, beginAtomId, endAtomId));
            }

            index = index + 1;
        }

        CopyAssociatedData(molecule, fragment, component, oldToNewAtomIds, oldToNewBondIds);
        fragment.ClearCaches();
        return fragment;
    }

    internal void CopyAssociatedData(Molecule source, Molecule fragment, ComponentInfo component, int[] atomMap, int[] bondMap)
    {
        fragment.Metadata = source.Metadata.Clone();
        CopyComponentConformers(source, fragment, component);
        CopyComponentResiduesAndChains(source, fragment, atomMap);
        CopyComponentModelsAndPolymers(source, fragment, atomMap);
        CopyComponentStereo(source, fragment, atomMap, bondMap);
    }

    internal void CopyComponentConformers(Molecule source, Molecule fragment, ComponentInfo component)
    {
        int conformerIndex = 0;
        while (conformerIndex < source.Conformers.Count)
        {
            Conformer sourceConformer = source.Conformers[conformerIndex];
            Conformer conformer = new Conformer(component.AtomIds.Count);
            conformer.Id = conformerIndex;
            conformer.Name = sourceConformer.Name;
            conformer.Energy = sourceConformer.Energy;
            conformer.HasEnergy = sourceConformer.HasEnergy;
            int atomIndex = 0;
            while (atomIndex < component.AtomIds.Count)
            {
                int oldAtomId = component.AtomIds[atomIndex];
                if (oldAtomId < sourceConformer.Coordinates.Count)
                {
                    conformer.Coordinates[atomIndex] = sourceConformer.Coordinates[oldAtomId].Clone();
                }

                atomIndex = atomIndex + 1;
            }

            fragment.Conformers.Add(conformer);
            conformerIndex = conformerIndex + 1;
        }
    }

    internal void CopyComponentResiduesAndChains(Molecule source, Molecule fragment, int[] atomMap)
    {
        int residueIndex = 0;
        while (residueIndex < source.Residues.Count)
        {
            Residue sourceResidue = source.Residues[residueIndex];
            Residue residue = new Residue();
            residue.Id = sourceResidue.Id;
            residue.Name = sourceResidue.Name;
            residue.ChainId = sourceResidue.ChainId;
            residue.Type = sourceResidue.Type;
            residue.InsertionCode = sourceResidue.InsertionCode;
            residue.ModelId = sourceResidue.ModelId;
            residue.RootAtomId = MapId(sourceResidue.RootAtomId, atomMap);
            int atomIndex = 0;
            while (atomIndex < sourceResidue.AtomIds.Count)
            {
                int mappedId = MapId(sourceResidue.AtomIds[atomIndex], atomMap);
                if (mappedId >= 0)
                {
                    residue.AtomIds.Add(mappedId);
                }

                atomIndex = atomIndex + 1;
            }

            if (residue.AtomIds.Count > 0)
            {
                if (residue.RootAtomId < 0)
                {
                    residue.RootAtomId = residue.AtomIds[0];
                }

                fragment.Residues.Add(residue);
            }

            residueIndex = residueIndex + 1;
        }

        CopyComponentChains(source, fragment);
    }

    internal void CopyComponentChains(Molecule source, Molecule fragment)
    {
        int chainIndex = 0;
        while (chainIndex < source.Chains.Count)
        {
            Chain sourceChain = source.Chains[chainIndex];
            Chain chain = new Chain();
            chain.Id = sourceChain.Id;
            chain.ModelId = sourceChain.ModelId;
            int residueIndex = 0;
            while (residueIndex < sourceChain.ResidueIds.Count)
            {
                int residueId = sourceChain.ResidueIds[residueIndex];
                if (ContainsResidue(fragment.Residues, residueId))
                {
                    chain.ResidueIds.Add(residueId);
                }

                residueIndex = residueIndex + 1;
            }

            if (chain.ResidueIds.Count > 0)
            {
                fragment.Chains.Add(chain);
            }

            chainIndex = chainIndex + 1;
        }
    }

    internal bool ContainsResidue(List<Residue> residues, int residueId)
    {
        int index = 0;
        while (index < residues.Count)
        {
            if (residues[index].Id == residueId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void CopyComponentModelsAndPolymers(Molecule source, Molecule fragment, int[] atomMap)
    {
        int modelIndex = 0;
        while (modelIndex < source.Models.Count)
        {
            MoleculeModel sourceModel = source.Models[modelIndex];
            MoleculeModel model = new MoleculeModel();
            model.Id = sourceModel.Id;
            model.Name = sourceModel.Name;
            int atomIndex = 0;
            while (atomIndex < sourceModel.AtomIds.Count)
            {
                int atomId = MapId(sourceModel.AtomIds[atomIndex], atomMap);
                if (atomId >= 0)
                {
                    model.AtomIds.Add(atomId);
                }

                atomIndex = atomIndex + 1;
            }

            int conformerIndex = 0;
            while (conformerIndex < sourceModel.ConformerIds.Count)
            {
                model.ConformerIds.Add(sourceModel.ConformerIds[conformerIndex]);
                conformerIndex = conformerIndex + 1;
            }

            if (model.AtomIds.Count > 0)
            {
                fragment.Models.Add(model);
            }

            modelIndex = modelIndex + 1;
        }

        int polymerIndex = 0;
        while (polymerIndex < source.Polymers.Count)
        {
            Polymer sourcePolymer = source.Polymers[polymerIndex];
            Polymer polymer = new Polymer();
            polymer.Id = sourcePolymer.Id;
            polymer.Name = sourcePolymer.Name;
            polymer.Type = sourcePolymer.Type;
            int residueIndex = 0;
            while (residueIndex < sourcePolymer.ResidueIds.Count)
            {
                int residueId = sourcePolymer.ResidueIds[residueIndex];
                if (ContainsResidue(fragment.Residues, residueId))
                {
                    polymer.ResidueIds.Add(residueId);
                }

                residueIndex = residueIndex + 1;
            }

            int chainIndex = 0;
            while (chainIndex < sourcePolymer.ChainIds.Count)
            {
                if (ContainsChain(fragment.Chains, sourcePolymer.ChainIds[chainIndex]))
                {
                    polymer.ChainIds.Add(sourcePolymer.ChainIds[chainIndex]);
                }

                chainIndex = chainIndex + 1;
            }

            if (polymer.ResidueIds.Count > 0 || polymer.ChainIds.Count > 0)
            {
                fragment.Polymers.Add(polymer);
            }

            polymerIndex = polymerIndex + 1;
        }
    }

    internal bool ContainsChain(List<Chain> chains, string chainId)
    {
        int index = 0;
        while (index < chains.Count)
        {
            if (chains[index].Id == chainId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void CopyComponentStereo(Molecule source, Molecule fragment, int[] atomMap, int[] bondMap)
    {
        int[] stereoMap = new int[source.StereoRecords.Count];
        int index = 0;
        while (index < stereoMap.Length)
        {
            stereoMap[index] = -1;
            index = index + 1;
        }

        index = 0;
        while (index < source.StereoRecords.Count)
        {
            StereoRecord sourceRecord = source.StereoRecords[index];
            if (StereoRecordBelongs(sourceRecord, atomMap, bondMap))
            {
                StereoRecord record = MoleculeBuilder.CloneStereoRecord(sourceRecord, fragment.StereoRecords.Count);
                RemapStereoRecord(record, atomMap, bondMap);
                record.GroupId = -1;
                stereoMap[index] = record.Id;
                fragment.StereoRecords.Add(record);
            }

            index = index + 1;
        }

        CopyComponentStereoGroups(source, fragment, stereoMap);
    }

    internal bool StereoRecordBelongs(StereoRecord record, int[] atomMap, int[] bondMap)
    {
        bool hasReference = false;
        if (!ReferenceBelongs(record.CenterAtomId, atomMap))
        {
            return false;
        }

        hasReference = hasReference || record.CenterAtomId >= 0;
        if (!ReferenceBelongs(record.BeginLigandAtomId, atomMap) || !ReferenceBelongs(record.EndLigandAtomId, atomMap))
        {
            return false;
        }

        hasReference = hasReference || record.BeginLigandAtomId >= 0 || record.EndLigandAtomId >= 0;
        if (!ReferenceBelongs(record.StereoBondId, bondMap))
        {
            return false;
        }

        hasReference = hasReference || record.StereoBondId >= 0;
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            if (!ReferenceBelongs(record.LigandAtomIds[index], atomMap))
            {
                return false;
            }

            hasReference = hasReference || record.LigandAtomIds[index] >= 0;
            index = index + 1;
        }

        return hasReference;
    }

    internal bool ReferenceBelongs(int id, int[] map)
    {
        if (id < 0)
        {
            return true;
        }

        return id < map.Length && map[id] >= 0;
    }

    internal void RemapStereoRecord(StereoRecord record, int[] atomMap, int[] bondMap)
    {
        record.CenterAtomId = MapId(record.CenterAtomId, atomMap);
        record.BeginLigandAtomId = MapId(record.BeginLigandAtomId, atomMap);
        record.EndLigandAtomId = MapId(record.EndLigandAtomId, atomMap);
        record.StereoBondId = MapId(record.StereoBondId, bondMap);
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            record.LigandAtomIds[index] = MapId(record.LigandAtomIds[index], atomMap);
            index = index + 1;
        }
    }

    internal void CopyComponentStereoGroups(Molecule source, Molecule fragment, int[] stereoMap)
    {
        int groupIndex = 0;
        while (groupIndex < source.StereoGroups.Groups.Count)
        {
            EnhancedStereoGroup sourceGroup = source.StereoGroups.Groups[groupIndex];
            EnhancedStereoGroup group = new EnhancedStereoGroup();
            group.Type = sourceGroup.Type;
            group.Source = sourceGroup.Source;
            group.Label = sourceGroup.Label;
            int recordIndex = 0;
            while (recordIndex < sourceGroup.StereoRecordIds.Count)
            {
                int oldId = sourceGroup.StereoRecordIds[recordIndex];
                if (oldId >= 0 && oldId < stereoMap.Length && stereoMap[oldId] >= 0)
                {
                    group.StereoRecordIds.Add(stereoMap[oldId]);
                }

                recordIndex = recordIndex + 1;
            }

            if (group.StereoRecordIds.Count > 0)
            {
                group.Id = fragment.StereoGroups.Groups.Count;
                fragment.StereoGroups.Groups.Add(group);
                AssignGroupId(fragment, group);
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal void AssignGroupId(Molecule molecule, EnhancedStereoGroup group)
    {
        int index = 0;
        while (index < group.StereoRecordIds.Count)
        {
            int recordId = group.StereoRecordIds[index];
            molecule.StereoRecords[recordId].GroupId = group.Id;
            index = index + 1;
        }
    }

    internal int MapId(int id, int[] map)
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

    internal string CreateAtomKey(Molecule molecule, ComponentInfo component)
    {
        List<int> atomicNumbers = new List<int>();
        int index = 0;
        while (index < component.AtomIds.Count)
        {
            atomicNumbers.Add(molecule.Atoms[component.AtomIds[index]].AtomicNumber);
            index = index + 1;
        }

        SortInts(atomicNumbers);

        StringBuilder builder = new StringBuilder();
        index = 0;
        while (index < atomicNumbers.Count)
        {
            if (index > 0)
            {
                builder.Append(".");
            }

            builder.Append(NumberFormatting.FormatInt(atomicNumbers[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal void SortInts(List<int> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            int value = values[index];
            int scan = index - 1;

            while (scan >= 0 && values[scan] > value)
            {
                values[scan + 1] = values[scan];
                scan = scan - 1;
            }

            values[scan + 1] = value;
            index = index + 1;
        }
    }

    internal void AddUniqueInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return;
            }

            index = index + 1;
        }

        values.Add(value);
    }
}
