using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides layout 2d data and operations for molecular graphs.
/// </summary>
public class Layout2D : ILayout2D
{
    internal double BondLength;
    internal double ComponentSpacing;
    internal RingFinder Rings;
    internal Components Components;

    /// <summary>
    /// Initializes a new instance of the <see cref="Layout2D"/> class.
    /// </summary>
    public Layout2D()
    {
        BondLength = 1.5;
        ComponentSpacing = 6.0;
        Rings = new RingFinder();
        Components = new Components();
    }

    /// <summary>
    /// Assigns deterministic two-dimensional coordinates to the molecule's atoms.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    public void Layout(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        List<ComponentInfo> components = Components.FindComponents(molecule);
        RingSet ringSet = Rings.EnsureRingCache(molecule);
        int componentIndex = 0;
        while (componentIndex < components.Count)
        {
            LayoutComponent(molecule, components[componentIndex], ringSet, (double)componentIndex * ComponentSpacing);
            componentIndex = componentIndex + 1;
        }
    }

    /// <summary>
    /// Assigns the stereo depiction.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting count or integer value.</returns>
    public int AssignStereoDepiction(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsTetrahedral())
            {
                if (AssignTetrahedralWedgeHash(molecule, record))
                {
                    count = count + 1;
                }
            }
            else if (record.IsDoubleBond())
            {
                if (AssignDoubleBondStereo(molecule, record))
                {
                    count = count + 1;
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal int AssignWedgeHashFromStereo(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsTetrahedral() && AssignTetrahedralWedgeHash(molecule, record))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool AssignTetrahedralWedgeHash(Molecule molecule, StereoRecord? record)
    {
        if (record == null || !molecule.IsValidAtomId(record.CenterAtomId))
        {
            return false;
        }

        int ligandAtomId = ChooseDepictionLigand(molecule, record, true);
        if (ligandAtomId < 0)
        {
            ligandAtomId = ChooseDepictionLigand(molecule, record, false);
        }

        if (ligandAtomId < 0)
        {
            return false;
        }

        int bondId = molecule.GetBondIdBetween(record.CenterAtomId, ligandAtomId);
        if (bondId < 0)
        {
            return false;
        }

        return ApplyTetrahedralStereoToBond(molecule.Bonds[bondId], record.Chirality);
    }

    internal int ChooseDepictionLigand(Molecule molecule, StereoRecord record, bool requireUndecorated)
    {
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            int ligandAtomId = record.LigandAtomIds[index];
            if (ligandAtomId >= 0 && molecule.IsValidAtomId(ligandAtomId))
            {
                int bondId = molecule.GetBondIdBetween(record.CenterAtomId, ligandAtomId);
                if (bondId >= 0 && IsSingleDepictableBond(molecule.Bonds[bondId]))
                {
                    if (!requireUndecorated || !BondHasDepictionStereo(molecule.Bonds[bondId]))
                    {
                        return ligandAtomId;
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ApplyTetrahedralStereoToBond(Bond bond, Chirality chirality)
    {
        if (chirality == Chirality.Clockwise)
        {
            bond.Stereo = BondStereo.Wedge;
            bond.Direction = BondDirection.Up;
            return true;
        }

        if (chirality == Chirality.CounterClockwise)
        {
            bond.Stereo = BondStereo.Hash;
            bond.Direction = BondDirection.Down;
            return true;
        }

        if (chirality == Chirality.Unspecified || chirality == Chirality.Unknown)
        {
            bond.Stereo = BondStereo.UpOrDown;
            bond.Direction = BondDirection.Either;
            return true;
        }

        return false;
    }

    internal bool AssignDoubleBondStereo(Molecule molecule, StereoRecord? record)
    {
        if (record == null || record.StereoBondId < 0 || record.StereoBondId >= molecule.Bonds.Count)
        {
            return false;
        }

        Bond bond = molecule.Bonds[record.StereoBondId];
        if (bond.Order != BondOrder.Double)
        {
            return false;
        }

        if (record.Configuration == DoubleBondConfiguration.E)
        {
            bond.Stereo = BondStereo.E;
            return true;
        }

        if (record.Configuration == DoubleBondConfiguration.Z)
        {
            bond.Stereo = BondStereo.Z;
            return true;
        }

        if (record.Configuration == DoubleBondConfiguration.Cis)
        {
            bond.Stereo = BondStereo.Cis;
            return true;
        }

        if (record.Configuration == DoubleBondConfiguration.Trans)
        {
            bond.Stereo = BondStereo.Trans;
            return true;
        }

        if (record.Configuration == DoubleBondConfiguration.Unspecified || record.Configuration == DoubleBondConfiguration.Unknown)
        {
            bond.Stereo = BondStereo.Unknown;
            return true;
        }

        return false;
    }

    internal bool IsSingleDepictableBond(Bond? bond)
    {
        if (bond == null)
        {
            return false;
        }

        if (bond.Order != BondOrder.Single || bond.IsAromatic || bond.IsDative)
        {
            return false;
        }

        return true;
    }

    internal bool BondHasDepictionStereo(Bond bond)
    {
        if (bond.Stereo != BondStereo.None)
        {
            return true;
        }

        if (bond.Direction != BondDirection.None)
        {
            return true;
        }

        return false;
    }

    internal void LayoutComponent(Molecule molecule, ComponentInfo component, RingSet ringSet, double offsetX)
    {
        bool[] placed = new bool[molecule.Atoms.Count];
        if (ComponentHasRing(component, ringSet))
        {
            LayoutRingComponent(molecule, component, ringSet, placed, offsetX);
        }
        else
        {
            LayoutAcyclicComponent(molecule, component, placed, offsetX);
        }

        int index = 0;
        while (index < component.AtomIds.Count)
        {
            int atomId = component.AtomIds[index];
            if (!placed[atomId])
            {
                SetAtomPosition(molecule, atomId, offsetX + (double)index * BondLength, 0.0, placed);
            }

            index = index + 1;
        }
    }

    internal void LayoutAcyclicComponent(Molecule molecule, ComponentInfo component, bool[] placed, double offsetX)
    {
        if (component.AtomIds.Count == 0)
        {
            return;
        }

        int startAtomId = component.AtomIds[0];
        SetAtomPosition(molecule, startAtomId, offsetX, 0.0, placed);
        LayoutTreeFromAtom(molecule, startAtomId, -1, 0.0, placed);
    }

    internal void LayoutRingComponent(Molecule molecule, ComponentInfo component, RingSet ringSet, bool[] placed, double offsetX)
    {
        int placedRingCount = 0;
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            Ring ring = ringSet.Rings[ringIndex];
            if (RingBelongsToComponent(ring, component))
            {
                if (placedRingCount == 0)
                {
                    PlaceRingAtCenter(molecule, ring, offsetX, 0.0, placed);
                }
                else
                {
                    PlaceRingNearPlacedAtoms(molecule, ring, placed);
                }

                placedRingCount = placedRingCount + 1;
            }

            ringIndex = ringIndex + 1;
        }

        int atomIndex = 0;
        while (atomIndex < component.AtomIds.Count)
        {
            int atomId = component.AtomIds[atomIndex];
            if (placed[atomId])
            {
                LayoutUnplacedNeighbors(molecule, atomId, -1, 0.0, placed);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void PlaceRingAtCenter(Molecule molecule, Ring ring, double centerX, double centerY, bool[] placed)
    {
        if (ring.AtomIds.Count == 0)
        {
            return;
        }

        double radius = GetRingRadius(ring.AtomIds.Count);
        int index = 0;
        while (index < ring.AtomIds.Count)
        {
            double angle = 2.0 * System.Math.PI * (double)index / (double)ring.AtomIds.Count;
            double x = centerX + radius * System.Math.Cos(angle);
            double y = centerY + radius * System.Math.Sin(angle);
            SetAtomPosition(molecule, ring.AtomIds[index], x, y, placed);
            index = index + 1;
        }
    }

    internal void PlaceRingNearPlacedAtoms(Molecule molecule, Ring ring, bool[] placed)
    {
        int firstPlaced = FindFirstPlacedRingAtom(ring, placed);
        int secondPlaced = FindSecondPlacedRingAtom(ring, placed, firstPlaced);
        if (firstPlaced < 0 || secondPlaced < 0)
        {
            PlaceUnplacedRingAroundFirstAtom(molecule, ring, placed);
            return;
        }

        Atom first = molecule.Atoms[firstPlaced];
        Atom second = molecule.Atoms[secondPlaced];
        double dx = second.X - first.X;
        double dy = second.Y - first.Y;
        double length = System.Math.Sqrt(dx * dx + dy * dy);
        if (length == 0.0)
        {
            length = BondLength;
            dx = BondLength;
            dy = 0.0;
        }

        double normalX = -dy / length;
        double normalY = dx / length;
        double centerX = (first.X + second.X) / 2.0 + normalX * BondLength;
        double centerY = (first.Y + second.Y) / 2.0 + normalY * BondLength;
        PlaceUnplacedRingAtomsAroundCenter(molecule, ring, centerX, centerY, placed);
    }

    internal void PlaceUnplacedRingAroundFirstAtom(Molecule molecule, Ring ring, bool[] placed)
    {
        double centerX = 0.0;
        double centerY = 0.0;
        if (ring.AtomIds.Count > 0)
        {
            Atom atom = molecule.Atoms[ring.AtomIds[0]];
            centerX = atom.X + BondLength;
            centerY = atom.Y + BondLength;
        }

        PlaceUnplacedRingAtomsAroundCenter(molecule, ring, centerX, centerY, placed);
    }

    internal void PlaceUnplacedRingAtomsAroundCenter(Molecule molecule, Ring ring, double centerX, double centerY, bool[] placed)
    {
        double radius = GetRingRadius(ring.AtomIds.Count);
        int index = 0;
        while (index < ring.AtomIds.Count)
        {
            int atomId = ring.AtomIds[index];
            if (!placed[atomId])
            {
                double angle = 2.0 * System.Math.PI * (double)index / (double)ring.AtomIds.Count;
                double x = centerX + radius * System.Math.Cos(angle);
                double y = centerY + radius * System.Math.Sin(angle);
                SetAtomPosition(molecule, atomId, x, y, placed);
            }

            index = index + 1;
        }
    }

    internal void LayoutTreeFromAtom(Molecule molecule, int atomId, int parentAtomId, double baseAngle, bool[] placed)
    {
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int branchCount = CountUnplacedNeighbors(neighbors, placed, parentAtomId);
        int branchIndex = 0;
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            if (neighborAtomId != parentAtomId && !placed[neighborAtomId])
            {
                double angle = GetBranchAngle(baseAngle, branchIndex, branchCount);
                double x = molecule.Atoms[atomId].X + BondLength * System.Math.Cos(angle);
                double y = molecule.Atoms[atomId].Y + BondLength * System.Math.Sin(angle);
                SetAtomPosition(molecule, neighborAtomId, x, y, placed);
                LayoutTreeFromAtom(molecule, neighborAtomId, atomId, angle, placed);
                branchIndex = branchIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void LayoutUnplacedNeighbors(Molecule molecule, int atomId, int parentAtomId, double baseAngle, bool[] placed)
    {
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int branchCount = CountUnplacedNeighbors(neighbors, placed, parentAtomId);
        int branchIndex = 0;
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            if (neighborAtomId != parentAtomId && !placed[neighborAtomId])
            {
                double angle = GetBranchAngle(baseAngle, branchIndex, branchCount);
                double x = molecule.Atoms[atomId].X + BondLength * System.Math.Cos(angle);
                double y = molecule.Atoms[atomId].Y + BondLength * System.Math.Sin(angle);
                SetAtomPosition(molecule, neighborAtomId, x, y, placed);
                LayoutUnplacedNeighbors(molecule, neighborAtomId, atomId, angle, placed);
                branchIndex = branchIndex + 1;
            }

            index = index + 1;
        }
    }

    internal int CountUnplacedNeighbors(List<int> neighbors, bool[] placed, int parentAtomId)
    {
        int count = 0;
        int index = 0;
        while (index < neighbors.Count)
        {
            int atomId = neighbors[index];
            if (atomId != parentAtomId && !placed[atomId])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal double GetBranchAngle(double baseAngle, int branchIndex, int branchCount)
    {
        if (branchCount <= 1)
        {
            return baseAngle;
        }

        double spread = System.Math.PI / 3.0;
        double start = baseAngle - spread * (double)(branchCount - 1) / 2.0;
        return start + spread * (double)branchIndex;
    }

    internal double GetRingRadius(int atomCount)
    {
        if (atomCount < 3)
        {
            return BondLength;
        }

        return BondLength / (2.0 * System.Math.Sin(System.Math.PI / (double)atomCount));
    }

    internal void SetAtomPosition(Molecule molecule, int atomId, double x, double y, bool[] placed)
    {
        if (!molecule.IsValidAtomId(atomId))
        {
            return;
        }

        Atom atom = molecule.Atoms[atomId];
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = 0.0;
        placed[atomId] = true;
    }

    internal bool ComponentHasRing(ComponentInfo component, RingSet ringSet)
    {
        int index = 0;
        while (index < ringSet.Rings.Count)
        {
            if (RingBelongsToComponent(ringSet.Rings[index], component))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool RingBelongsToComponent(Ring ring, ComponentInfo component)
    {
        if (ring.AtomIds.Count == 0)
        {
            return false;
        }

        return ContainsInt(component.AtomIds, ring.AtomIds[0]);
    }

    internal int FindFirstPlacedRingAtom(Ring ring, bool[] placed)
    {
        int index = 0;
        while (index < ring.AtomIds.Count)
        {
            if (placed[ring.AtomIds[index]])
            {
                return ring.AtomIds[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindSecondPlacedRingAtom(Ring ring, bool[] placed, int firstAtomId)
    {
        int index = 0;
        while (index < ring.AtomIds.Count)
        {
            int atomId = ring.AtomIds[index];
            if (atomId != firstAtomId && placed[atomId])
            {
                return atomId;
            }

            index = index + 1;
        }

        return -1;
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
}

