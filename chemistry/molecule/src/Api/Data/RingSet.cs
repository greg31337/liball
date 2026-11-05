using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides ring set data and operations for molecular graphs.
/// </summary>
public class RingSet
{
    /// <summary>
    /// The rings.
    /// </summary>
    public List<Ring> Rings;
    /// <summary>
    /// The ring atom identifiers.
    /// </summary>
    public List<int> RingAtomIds;
    /// <summary>
    /// The ring bond identifiers.
    /// </summary>
    public List<int> RingBondIds;
    /// <summary>
    /// The ring systems.
    /// </summary>
    public List<RingSystem> RingSystems;

    /// <summary>
    /// Initializes a new instance of the <see cref="RingSet"/> class.
    /// </summary>
    public RingSet()
    {
        Rings = new List<Ring>();
        RingAtomIds = new List<int>();
        RingBondIds = new List<int>();
        RingSystems = new List<RingSystem>();
    }

    /// <summary>
    /// Returns the ring count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetRingCount()
    {
        return Rings.Count;
    }

    /// <summary>
    /// Returns the ring system count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetRingSystemCount()
    {
        return RingSystems.Count;
    }

    /// <summary>
    /// Atom is in ring.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AtomIsInRing(int atomId)
    {
        return ContainsInt(RingAtomIds, atomId);
    }

    /// <summary>
    /// Bond is in ring.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool BondIsInRing(int bondId)
    {
        return ContainsInt(RingBondIds, bondId);
    }

    /// <summary>
    /// Returns the atom ring count.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetAtomRingCount(int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < Rings.Count)
        {
            if (ContainsInt(Rings[index].AtomIds, atomId))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Returns the bond ring count.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetBondRingCount(int bondId)
    {
        int count = 0;
        int index = 0;
        while (index < Rings.Count)
        {
            if (ContainsInt(Rings[index].BondIds, bondId))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void AddRing(Ring ring)
    {
        ring.Id = Rings.Count;
        Rings.Add(ring);

        int atomIndex = 0;
        while (atomIndex < ring.AtomIds.Count)
        {
            AddUniqueInt(RingAtomIds, ring.AtomIds[atomIndex]);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < ring.BondIds.Count)
        {
            AddUniqueInt(RingBondIds, ring.BondIds[bondIndex]);
            bondIndex = bondIndex + 1;
        }
    }

    internal bool HasRingKey(string key)
    {
        int index = 0;
        while (index < Rings.Count)
        {
            if (Rings[index].Key == key)
            {
                return true;
            }

            index = index + 1;
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

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
    }

    internal void BuildRingSystems()
    {
        RingSystems.Clear();
        bool[] visited = new bool[Rings.Count];
        int ringIndex = 0;

        while (ringIndex < Rings.Count)
        {
            if (!visited[ringIndex])
            {
                RingSystem system = BuildRingSystem(ringIndex, visited);
                RingSystems.Add(system);
            }

            ringIndex = ringIndex + 1;
        }
    }

    internal RingSystem BuildRingSystem(int startRingId, bool[] visited)
    {
        RingSystem system = new RingSystem();
        system.Id = RingSystems.Count;
        bool hasSingleBondFusion = false;
        bool hasBridgeCandidate = false;

        List<int> queue = new List<int>();
        visited[startRingId] = true;
        queue.Add(startRingId);

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int ringId = queue[queueIndex];
            Ring ring = Rings[ringId];
            AddUniqueInt(system.RingIds, ringId);
            AddRingMembersToSystem(system, ring);

            int neighborRingId = 0;
            while (neighborRingId < Rings.Count)
            {
                if (!visited[neighborRingId] && RingsShareAtom(ring, Rings[neighborRingId]))
                {
                    visited[neighborRingId] = true;
                    queue.Add(neighborRingId);
                }

                if (ringId != neighborRingId)
                {
                    int sharedBondCount = CountSharedInts(ring.BondIds, Rings[neighborRingId].BondIds);
                    int sharedAtomCount = CountSharedInts(ring.AtomIds, Rings[neighborRingId].AtomIds);
                    if (sharedBondCount == 1)
                    {
                        hasSingleBondFusion = true;
                        system.IsFused = true;
                        AddSharedBonds(system, ring, Rings[neighborRingId]);
                    }

                    if (sharedBondCount > 1 || (sharedBondCount == 0 && sharedAtomCount > 1))
                    {
                        hasBridgeCandidate = true;
                    }
                }

                neighborRingId = neighborRingId + 1;
            }

            queueIndex = queueIndex + 1;
        }

        system.IsBridged = hasBridgeCandidate && !hasSingleBondFusion;
        if (system.IsBridged)
        {
            AddBridgeheadAtoms(system);
        }

        return system;
    }

    internal void AddRingMembersToSystem(RingSystem system, Ring ring)
    {
        int atomIndex = 0;
        while (atomIndex < ring.AtomIds.Count)
        {
            AddUniqueInt(system.AtomIds, ring.AtomIds[atomIndex]);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < ring.BondIds.Count)
        {
            AddUniqueInt(system.BondIds, ring.BondIds[bondIndex]);
            bondIndex = bondIndex + 1;
        }
    }

    internal bool RingsShareAtom(Ring left, Ring right)
    {
        int index = 0;
        while (index < left.AtomIds.Count)
        {
            if (ContainsInt(right.AtomIds, left.AtomIds[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddSharedBonds(RingSystem system, Ring left, Ring right)
    {
        int index = 0;
        while (index < left.BondIds.Count)
        {
            if (ContainsInt(right.BondIds, left.BondIds[index]))
            {
                AddUniqueInt(system.FusedBondIds, left.BondIds[index]);
            }

            index = index + 1;
        }
    }

    internal int CountSharedInts(List<int> left, List<int> right)
    {
        int count = 0;
        int index = 0;
        while (index < left.Count)
        {
            if (ContainsInt(right, left[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void AddBridgeheadAtoms(RingSystem system)
    {
        int atomIndex = 0;
        while (atomIndex < system.AtomIds.Count)
        {
            int atomId = system.AtomIds[atomIndex];
            if (CountSystemBondDegree(system, atomId) >= 3 && CountSystemRingMembership(system, atomId) >= 2)
            {
                AddUniqueInt(system.BridgeheadAtomIds, atomId);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal int CountSystemBondDegree(RingSystem system, int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < system.BondIds.Count)
        {
            int bondId = system.BondIds[index];
            int ringIndex = 0;
            bool counted = false;
            while (ringIndex < Rings.Count && !counted)
            {
                Ring ring = Rings[ringIndex];
                int memberIndex = 0;
                while (memberIndex < ring.BondIds.Count && !counted)
                {
                    if (ring.BondIds[memberIndex] == bondId)
                    {
                        int position = FindIntPosition(ring.BondIds, bondId);
                        if (position >= 0 && RingBondTouchesAtom(ring, position, atomId))
                        {
                            count = count + 1;
                            counted = true;
                        }
                    }

                    memberIndex = memberIndex + 1;
                }

                ringIndex = ringIndex + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int FindIntPosition(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool RingBondTouchesAtom(Ring ring, int bondPosition, int atomId)
    {
        if (bondPosition < 0 || bondPosition >= ring.AtomIds.Count)
        {
            return false;
        }

        int nextPosition = bondPosition + 1;
        if (nextPosition == ring.AtomIds.Count)
        {
            nextPosition = 0;
        }

        return ring.AtomIds[bondPosition] == atomId || ring.AtomIds[nextPosition] == atomId;
    }

    internal int CountSystemRingMembership(RingSystem system, int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < system.RingIds.Count)
        {
            if (ContainsInt(Rings[system.RingIds[index]].AtomIds, atomId))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Returns the ring system for ring.
    /// </summary>
    /// <param name="ringId">The ring identifier.</param>
    /// <returns>The resulting ring system.</returns>
    internal RingSystem GetRingSystemForRing(int ringId)
    {
        int systemIndex = 0;
        while (systemIndex < RingSystems.Count)
        {
            if (ContainsInt(RingSystems[systemIndex].RingIds, ringId))
            {
                return RingSystems[systemIndex];
            }

            systemIndex = systemIndex + 1;
        }

        return new RingSystem();
    }

    /// <summary>
    /// Returns the ring system for atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting ring system.</returns>
    internal RingSystem GetRingSystemForAtom(int atomId)
    {
        int systemIndex = 0;
        while (systemIndex < RingSystems.Count)
        {
            if (ContainsInt(RingSystems[systemIndex].AtomIds, atomId))
            {
                return RingSystems[systemIndex];
            }

            systemIndex = systemIndex + 1;
        }

        return new RingSystem();
    }
}
