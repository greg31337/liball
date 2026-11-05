using System.Collections.Generic;

namespace molecule;


internal class SmartsExpressionMatcher
{
    internal bool MatchesAtomExpression(QueryExpression? expression, Molecule molecule, int atomId)
    {
        if (expression == null || expression.Operation == QueryOperation.None)
        {
            return true;
        }

        if (expression.Operation == QueryOperation.And)
        {
            int childIndex = 0;
            while (childIndex < expression.Children.Count)
            {
                if (!MatchesAtomExpression(expression.Children[childIndex], molecule, atomId))
                {
                    return false;
                }

                childIndex = childIndex + 1;
            }

            return true;
        }

        if (expression.Operation == QueryOperation.Or)
        {
            int childIndex = 0;
            while (childIndex < expression.Children.Count)
            {
                if (MatchesAtomExpression(expression.Children[childIndex], molecule, atomId))
                {
                    return true;
                }

                childIndex = childIndex + 1;
            }

            return false;
        }

        if (expression.Operation == QueryOperation.Not)
        {
            if (expression.Children.Count == 0)
            {
                return false;
            }

            return !MatchesAtomExpression(expression.Children[0], molecule, atomId);
        }

        if (expression.Operation == QueryOperation.Recursive || expression.Primitive == QueryPrimitive.Recursive)
        {
            return new SubgraphMatcher().MatchesRecursiveQuery(molecule, expression.RecursiveQuery, atomId);
        }

        return MatchesAtomLeaf(expression, molecule, atomId);
    }

    internal bool MatchesAtomLeaf(QueryExpression expression, Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (expression.Primitive == QueryPrimitive.Any)
        {
            return true;
        }

        if (expression.Primitive == QueryPrimitive.AtomicNumber)
        {
            return atom.AtomicNumber == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.Aromatic)
        {
            return atom.IsAromatic;
        }

        if (expression.Primitive == QueryPrimitive.Aliphatic)
        {
            return !atom.IsAromatic;
        }

        if (expression.Primitive == QueryPrimitive.Isotope)
        {
            return atom.IsotopeMassNumber == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.HydrogenCount)
        {
            return atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.ImplicitHydrogenCount)
        {
            return atom.ImplicitHydrogenCount == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.Degree)
        {
            return molecule.GetDegree(atomId) == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.Connectivity)
        {
            return molecule.GetDegree(atomId) + atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.Valence)
        {
            return GetValence(molecule, atomId) == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.RingMembership)
        {
            int ringCount = new RingFinder().EnsureRingCache(molecule).GetAtomRingCount(atomId);
            if (expression.Value == 0)
            {
                return ringCount == 0;
            }

            return ringCount > 0;
        }

        if (expression.Primitive == QueryPrimitive.RingCount)
        {
            return new RingFinder().EnsureRingCache(molecule).GetAtomRingCount(atomId) == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.RingSize)
        {
            return AtomBelongsToRingSize(new RingFinder().EnsureRingCache(molecule), atomId, expression.Value);
        }

        if (expression.Primitive == QueryPrimitive.RingConnectivity)
        {
            return CountIncidentRingBonds(molecule, atomId) == expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.FormalCharge)
        {
            return atom.FormalCharge == expression.Value;
        }

        return false;
    }

    internal bool MatchesBondExpression(QueryExpression? expression, Molecule molecule, int bondId)
    {
        if (expression == null || expression.Operation == QueryOperation.None)
        {
            return true;
        }

        if (expression.Operation == QueryOperation.And)
        {
            int childIndex = 0;
            while (childIndex < expression.Children.Count)
            {
                if (!MatchesBondExpression(expression.Children[childIndex], molecule, bondId))
                {
                    return false;
                }

                childIndex = childIndex + 1;
            }

            return true;
        }

        if (expression.Operation == QueryOperation.Or)
        {
            int childIndex = 0;
            while (childIndex < expression.Children.Count)
            {
                if (MatchesBondExpression(expression.Children[childIndex], molecule, bondId))
                {
                    return true;
                }

                childIndex = childIndex + 1;
            }

            return false;
        }

        if (expression.Operation == QueryOperation.Not)
        {
            if (expression.Children.Count == 0)
            {
                return false;
            }

            return !MatchesBondExpression(expression.Children[0], molecule, bondId);
        }

        Bond bond = molecule.Bonds[bondId];
        if (expression.Primitive == QueryPrimitive.Any)
        {
            return true;
        }

        if (expression.Primitive == QueryPrimitive.BondOrder)
        {
            return bond.Order == (BondOrder)expression.Value;
        }

        if (expression.Primitive == QueryPrimitive.BondAromatic)
        {
            return bond.IsAromatic || bond.Order == BondOrder.Aromatic;
        }

        if (expression.Primitive == QueryPrimitive.BondRing)
        {
            return RingSetContainsBond(new RingFinder().EnsureRingCache(molecule), bondId);
        }

        if (expression.Primitive == QueryPrimitive.BondDirection)
        {
            return bond.Direction == (BondDirection)expression.Value;
        }

        return false;
    }

    internal int GetValence(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        int value = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                value = value + GetBondOrderValue(bond);
            }

            bondIndex = bondIndex + 1;
        }

        return value;
    }

    internal int GetBondOrderValue(Bond bond)
    {
        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        if (bond.Order == BondOrder.Quadruple)
        {
            return 4;
        }

        if (bond.Order == BondOrder.None || bond.Order == BondOrder.Unknown)
        {
            return 0;
        }

        return 1;
    }

    internal bool AtomBelongsToRingSize(RingSet ringSet, int atomId, int ringSize)
    {
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            Ring ring = ringSet.Rings[ringIndex];
            if (ring.Size == ringSize && ContainsInt(ring.AtomIds, atomId))
            {
                return true;
            }

            ringIndex = ringIndex + 1;
        }

        return false;
    }

    internal int CountIncidentRingBonds(Molecule molecule, int atomId)
    {
        RingSet ringSet = new RingFinder().EnsureRingCache(molecule);
        List<int> countedBondIds = new List<int>();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && RingSetContainsBond(ringSet, bondIndex) && !ContainsInt(countedBondIds, bondIndex))
            {
                countedBondIds.Add(bondIndex);
            }

            bondIndex = bondIndex + 1;
        }

        return countedBondIds.Count;
    }

    internal bool RingSetContainsBond(RingSet ringSet, int bondId)
    {
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            if (ContainsInt(ringSet.Rings[ringIndex].BondIds, bondId))
            {
                return true;
            }

            ringIndex = ringIndex + 1;
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
}
