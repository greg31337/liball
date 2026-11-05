namespace molecule;


/// <summary>
/// Calculates valence information from molecular graphs.
/// </summary>
internal class ValenceCalculator
{
    internal ValenceTable Valences;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValenceCalculator"/> class.
    /// </summary>
    public ValenceCalculator()
    {
        Valences = new ValenceTable();
    }

    internal ValenceResult CalculateAtomValence(Molecule? molecule, int atomId)
    {
        ValenceResult result = new ValenceResult();

        if (molecule == null)
        {
            return result;
        }

        if (!molecule.IsValidAtomId(atomId))
        {
            return result;
        }

        Atom atom = molecule.Atoms[atomId];
        result.AtomId = atomId;
        result.AtomicNumber = atom.AtomicNumber;
        result.HydrogenValence = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        result.FormalCharge = atom.FormalCharge;
        result.RadicalElectronCount = atom.RadicalElectronCount;
        result.MaximumKnownValence = GetMaximumKnownValence(atom.AtomicNumber);

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (IsDativeBond(bond))
                {
                    result.DativeBondCount = result.DativeBondCount + 1;
                }
                else
                {
                    result.BondValence = result.BondValence + GetBondValence(bond);
                }

                if (IsAromaticBond(bond))
                {
                    result.AromaticBondCount = result.AromaticBondCount + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        result.TotalValence = result.BondValence + result.HydrogenValence;
        result.ElectronValence = result.TotalValence + atom.RadicalElectronCount;
        FillAllowedFlags(atom, result);
        return result;
    }

    internal void FillAllowedFlags(Atom atom, ValenceResult result)
    {
        result.IsTableAllowed = Valences.IsAllowedValence(atom.AtomicNumber, result.TotalValence);
        result.IsChargeAdjustedAllowed = IsChargeAdjustedAllowed(atom, result.TotalValence);
        result.IsRadicalAdjustedAllowed = IsRadicalAdjustedAllowed(atom, result);
        result.IsAromaticAllowed = IsAromaticAllowed(atom, result);
        result.IsHypervalentAllowed = result.TotalValence > 4 && Valences.IsAllowedValence(atom.AtomicNumber, result.TotalValence);
        result.IsAllowed = result.IsTableAllowed || result.IsChargeAdjustedAllowed || result.IsRadicalAdjustedAllowed || result.IsAromaticAllowed || result.IsHypervalentAllowed;
    }

    internal int GetBondValence(Bond? bond)
    {
        if (bond == null)
        {
            return 0;
        }

        if (IsDativeBond(bond))
        {
            return 0;
        }

        if (IsAromaticBond(bond))
        {
            return 1;
        }

        if (bond.Order == BondOrder.Single)
        {
            return 1;
        }

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

        return 0;
    }

    internal bool IsDativeBond(Bond? bond)
    {
        if (bond == null)
        {
            return false;
        }

        return bond.IsDative || bond.Order == BondOrder.Dative;
    }

    internal bool IsAromaticBond(Bond? bond)
    {
        if (bond == null)
        {
            return false;
        }

        return bond.IsAromatic || bond.Order == BondOrder.Aromatic;
    }

    internal bool IsChargeAdjustedAllowed(Atom atom, int totalValence)
    {
        if (atom.AtomicNumber == 7 && atom.FormalCharge == 1 && totalValence == 4)
        {
            return true;
        }

        if (atom.AtomicNumber == 8 && atom.FormalCharge == -1 && totalValence == 1)
        {
            return true;
        }

        if (atom.AtomicNumber == 5 && atom.FormalCharge == -1 && totalValence == 4)
        {
            return true;
        }

        if (atom.AtomicNumber == 6 && atom.FormalCharge != 0 && totalValence == 3)
        {
            return true;
        }

        if ((atom.AtomicNumber == 9 || atom.AtomicNumber == 17 || atom.AtomicNumber == 35 || atom.AtomicNumber == 53) && atom.FormalCharge == -1 && totalValence == 0)
        {
            return true;
        }

        return false;
    }

    internal bool IsRadicalAdjustedAllowed(Atom atom, ValenceResult result)
    {
        if (atom.RadicalElectronCount <= 0)
        {
            return false;
        }

        if (Valences.IsAllowedValence(atom.AtomicNumber, result.ElectronValence))
        {
            return true;
        }

        return false;
    }

    internal bool IsAromaticAllowed(Atom atom, ValenceResult result)
    {
        if (!atom.IsAromatic && result.AromaticBondCount == 0)
        {
            return false;
        }

        if (atom.AtomicNumber == 6 && result.TotalValence == 3)
        {
            return true;
        }

        if (atom.AtomicNumber == 7 && (result.TotalValence == 2 || result.TotalValence == 3))
        {
            return true;
        }

        if (atom.AtomicNumber == 8 && result.TotalValence == 2)
        {
            return true;
        }

        if (atom.AtomicNumber == 15 && result.TotalValence == 3)
        {
            return true;
        }

        if (atom.AtomicNumber == 16 && result.TotalValence == 2)
        {
            return true;
        }

        return false;
    }

    internal int GetMaximumKnownValence(int atomicNumber)
    {
        int maximum = 0;
        int index = 0;
        while (index < Valences.Elements.GetElement(atomicNumber).CommonValences.Count)
        {
            int valence = Valences.Elements.GetElement(atomicNumber).CommonValences[index];
            if (valence > maximum)
            {
                maximum = valence;
            }

            index = index + 1;
        }

        return maximum;
    }
}

