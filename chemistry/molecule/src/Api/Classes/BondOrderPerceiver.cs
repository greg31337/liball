using System;

namespace molecule;


/// <summary>
/// Conservatively proposes bond orders from distances over existing molecular connectivity.
/// </summary>
public class BondOrderPerceiver : IBondOrderPerceiver
{
    internal ElementTable Elements;
    internal ValenceCalculator Valences;

    /// <summary>
    /// Initializes a bond-order perceiver using the built-in element and valence tables.
    /// </summary>
    public BondOrderPerceiver()
    {
        Elements = new ElementTable();
        Valences = new ValenceCalculator();
    }

    /// <summary>
    /// Perceives bond orders from atom or conformer coordinates on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule whose existing connectivity is evaluated.</param>
    /// <param name="options">The coordinate and confidence policy; <c>null</c> uses conservative defaults.</param>
    /// <returns>The copied molecule, per-bond proposals, confidence data, and diagnostics.</returns>
    public BondOrderPerceptionResult PerceiveBondOrders(Molecule? molecule, BondOrderPerceptionOptions? options)
    {
        BondOrderPerceptionResult result = new BondOrderPerceptionResult();
        if (molecule == null)
        {
            result.Report.AddError("BOND_ORDER_NULL_MOLECULE", "Cannot perceive bond orders for a null molecule.");
            return result;
        }

        BondOrderPerceptionOptions safeOptions = NormalizeBondOrderOptions(options);
        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        if (!ValidateBondOrderOptions(safeOptions, result))
        {
            return result;
        }

        if (safeOptions.ConformerId >= 0 && !result.Molecule.IsValidConformerId(safeOptions.ConformerId))
        {
            result.Report.AddError("BOND_ORDER_INVALID_CONFORMER", "The selected conformer identifier is outside the conformer collection.");
            return result;
        }

        BondOrderCoordinateSet coordinates = CollectBondOrderCoordinates(result.Molecule, safeOptions);
        result.UsedCoordinateDimension = ResolveBondOrderDimension(coordinates, safeOptions.CoordinateDimension);
        if (result.Molecule.Bonds.Count > 0 && !HasAnyBondOrderCoordinate(coordinates))
        {
            result.Report.AddError("BOND_ORDER_COORDINATES_MISSING", "The selected coordinate source contains no usable coordinates.");
            return result;
        }

        result.Success = true;
        int bondIndex = 0;
        while (bondIndex < result.Molecule.Bonds.Count)
        {
            EvaluateBondOrder(result.Molecule, result.Molecule.Bonds[bondIndex], coordinates, safeOptions, result);
            bondIndex = bondIndex + 1;
        }

        if (result.Changed)
        {
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal BondOrderPerceptionOptions NormalizeBondOrderOptions(BondOrderPerceptionOptions? options)
    {
        BondOrderPerceptionOptions safeOptions = new BondOrderPerceptionOptions();
        if (options != null)
        {
            safeOptions.CoordinateDimension = options.CoordinateDimension;
            safeOptions.ConformerId = options.ConformerId;
            safeOptions.CoordinateScale = options.CoordinateScale;
            safeOptions.PreserveAssignedBondOrders = options.PreserveAssignedBondOrders;
            safeOptions.MaximumRelativeError = options.MaximumRelativeError;
            safeOptions.MinimumAlternativeGap = options.MinimumAlternativeGap;
            safeOptions.MinimumConfidenceToApply = options.MinimumConfidenceToApply;
        }

        return safeOptions;
    }

    internal bool ValidateBondOrderOptions(BondOrderPerceptionOptions options, BondOrderPerceptionResult result)
    {
        if (options.CoordinateDimension != BondOrderCoordinateDimension.Automatic
            && options.CoordinateDimension != BondOrderCoordinateDimension.TwoDimensional
            && options.CoordinateDimension != BondOrderCoordinateDimension.ThreeDimensional)
        {
            result.Report.AddError("BOND_ORDER_INVALID_DIMENSION", "The requested coordinate dimension is not supported.");
            return false;
        }

        if (options.ConformerId < -1)
        {
            result.Report.AddError("BOND_ORDER_INVALID_CONFORMER", "The conformer identifier must be -1 or a valid collection index.");
            return false;
        }

        if (!IsFiniteBondOrderNumber(options.CoordinateScale) || options.CoordinateScale <= 0.0)
        {
            result.Report.AddError("BOND_ORDER_INVALID_SCALE", "The coordinate scale must be a finite positive number.");
            return false;
        }

        if (!IsFiniteBondOrderNumber(options.MaximumRelativeError) || options.MaximumRelativeError < 0.0 || options.MaximumRelativeError > 1.0)
        {
            result.Report.AddError("BOND_ORDER_INVALID_ERROR_LIMIT", "The maximum relative error must be between zero and one.");
            return false;
        }

        if (!IsFiniteBondOrderNumber(options.MinimumAlternativeGap) || options.MinimumAlternativeGap < 0.0 || options.MinimumAlternativeGap > 1.0)
        {
            result.Report.AddError("BOND_ORDER_INVALID_ALTERNATIVE_GAP", "The minimum alternative gap must be between zero and one.");
            return false;
        }

        if (options.MinimumConfidenceToApply != BondOrderConfidence.Low
            && options.MinimumConfidenceToApply != BondOrderConfidence.Medium
            && options.MinimumConfidenceToApply != BondOrderConfidence.High)
        {
            result.Report.AddError("BOND_ORDER_INVALID_CONFIDENCE", "The minimum confidence must be low, medium, or high.");
            return false;
        }

        return true;
    }

    internal BondOrderCoordinateSet CollectBondOrderCoordinates(Molecule molecule, BondOrderPerceptionOptions options)
    {
        BondOrderCoordinateSet coordinates = new BondOrderCoordinateSet();
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            bool isSet = false;
            if (options.ConformerId >= 0)
            {
                Conformer conformer = molecule.Conformers[options.ConformerId];
                if (atomIndex < conformer.Coordinates.Count)
                {
                    Coordinate3D coordinate = conformer.Coordinates[atomIndex];
                    x = coordinate.X;
                    y = coordinate.Y;
                    z = coordinate.Z;
                    isSet = coordinate.IsSet;
                }
            }
            else
            {
                Atom atom = molecule.Atoms[atomIndex];
                x = atom.X;
                y = atom.Y;
                z = atom.Z;
                isSet = atom.HasCoordinates;
            }

            if (!IsFiniteBondOrderNumber(x) || !IsFiniteBondOrderNumber(y) || !IsFiniteBondOrderNumber(z))
            {
                isSet = false;
            }

            coordinates.X.Add(x);
            coordinates.Y.Add(y);
            coordinates.Z.Add(z);
            coordinates.IsSet.Add(isSet);
            atomIndex = atomIndex + 1;
        }

        return coordinates;
    }

    internal BondOrderCoordinateDimension ResolveBondOrderDimension(BondOrderCoordinateSet coordinates, BondOrderCoordinateDimension requested)
    {
        if (requested != BondOrderCoordinateDimension.Automatic)
        {
            return requested;
        }

        int index = 0;
        while (index < coordinates.Z.Count)
        {
            if (coordinates.IsSet[index] && Math.Abs(coordinates.Z[index]) > 0.00000001)
            {
                return BondOrderCoordinateDimension.ThreeDimensional;
            }

            index = index + 1;
        }

        return BondOrderCoordinateDimension.TwoDimensional;
    }

    internal bool HasAnyBondOrderCoordinate(BondOrderCoordinateSet coordinates)
    {
        int index = 0;
        while (index < coordinates.IsSet.Count)
        {
            if (coordinates.IsSet[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void EvaluateBondOrder(Molecule molecule, Bond bond, BondOrderCoordinateSet coordinates, BondOrderPerceptionOptions options, BondOrderPerceptionResult result)
    {
        if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
        {
            AddBondOrderError(result, "BOND_ORDER_INVALID_BOND", "The bond has an endpoint outside the atom collection.", bond.Id);
            result.Success = false;
            return;
        }

        Atom beginAtom = molecule.Atoms[bond.BeginAtomId];
        Atom endAtom = molecule.Atoms[bond.EndAtomId];
        if (BondOrderIsAromatic(bond, beginAtom, endAtom))
        {
            AddSpecialBondOrderAssignment(result, bond, BondOrder.Aromatic, "Aromatic bond orders cannot be resolved safely from one distance.", "BOND_ORDER_AROMATIC_AMBIGUOUS");
            return;
        }

        if (BondOrderIsMetalCoordination(beginAtom, endAtom))
        {
            AddSpecialBondOrderAssignment(result, bond, BondOrder.Dative, "Metal coordination requires explicit chemical context beyond distance.", "BOND_ORDER_METAL_COORDINATION");
            return;
        }

        if (options.PreserveAssignedBondOrders && BondOrderIsAssigned(bond.Order))
        {
            return;
        }

        BondOrderAssignment assignment = new BondOrderAssignment();
        assignment.BondId = bond.Id;
        assignment.PreviousOrder = bond.Order;
        result.Assignments.Add(assignment);
        if (!CoordinatesCoverBond(coordinates, bond))
        {
            assignment.IsAmbiguous = true;
            assignment.Reason = "One or both bond endpoints have no finite coordinate.";
            result.UnresolvedCount = result.UnresolvedCount + 1;
            AddBondOrderWarning(result, "BOND_ORDER_COORDINATE_MISSING", assignment.Reason, bond.Id);
            return;
        }

        assignment.Distance = CalculateBondOrderDistance(coordinates, bond, options, result.UsedCoordinateDimension);
        Element beginElement = Elements.GetElement(beginAtom.AtomicNumber);
        Element endElement = Elements.GetElement(endAtom.AtomicNumber);
        assignment.ReferenceSingleBondLength = beginElement.CovalentRadius + endElement.CovalentRadius;
        if (assignment.ReferenceSingleBondLength <= 0.0)
        {
            assignment.IsAmbiguous = true;
            assignment.Reason = "One or both endpoint elements have no covalent-radius reference.";
            result.UnresolvedCount = result.UnresolvedCount + 1;
            AddBondOrderWarning(result, "BOND_ORDER_REFERENCE_MISSING", assignment.Reason, bond.Id);
            return;
        }

        int maximumOrder = GetMaximumPerceivedOrder(beginAtom.AtomicNumber, endAtom.AtomicNumber);
        BondOrderProposal proposal = FindBondOrderProposal(assignment.Distance, assignment.ReferenceSingleBondLength, maximumOrder);
        assignment.PerceivedOrder = proposal.Order;
        assignment.Confidence = GetBondOrderConfidence(proposal);
        if (proposal.RelativeError > options.MaximumRelativeError || proposal.AlternativeGap < options.MinimumAlternativeGap)
        {
            assignment.Confidence = BondOrderConfidence.Low;
            assignment.IsAmbiguous = true;
            assignment.Reason = "The distance does not distinguish a plausible bond order within the configured limits.";
            result.UnresolvedCount = result.UnresolvedCount + 1;
            AddBondOrderWarning(result, "BOND_ORDER_AMBIGUOUS_DISTANCE", assignment.Reason, bond.Id);
            return;
        }

        if ((int)assignment.Confidence < (int)options.MinimumConfidenceToApply)
        {
            assignment.Reason = "The proposal is below the configured confidence threshold.";
            result.UnresolvedCount = result.UnresolvedCount + 1;
            AddBondOrderWarning(result, "BOND_ORDER_LOW_CONFIDENCE", assignment.Reason, bond.Id);
            return;
        }

        BondOrder previousOrder = bond.Order;
        bool previousAromatic = bond.IsAromatic;
        bool previousDative = bond.IsDative;
        bond.Order = proposal.Order;
        bond.IsAromatic = false;
        bond.IsDative = false;
        if (BondOrderCreatesHypervalence(molecule, bond))
        {
            bond.Order = previousOrder;
            bond.IsAromatic = previousAromatic;
            bond.IsDative = previousDative;
            assignment.Confidence = BondOrderConfidence.Low;
            assignment.IsAmbiguous = true;
            assignment.Reason = "The proposed order would exceed a supported endpoint valence.";
            result.UnresolvedCount = result.UnresolvedCount + 1;
            AddBondOrderWarning(result, "BOND_ORDER_HYPERVALENT_REJECTED", assignment.Reason, bond.Id);
            return;
        }

        assignment.Applied = true;
        assignment.Reason = "The proposal satisfies the distance, confidence, and valence checks.";
        result.AppliedCount = result.AppliedCount + 1;
        if (previousOrder != proposal.Order || previousAromatic || previousDative)
        {
            result.Changed = true;
        }
    }

    internal bool BondOrderIsAromatic(Bond bond, Atom beginAtom, Atom endAtom)
    {
        return bond.IsAromatic || bond.Order == BondOrder.Aromatic || beginAtom.IsAromatic || endAtom.IsAromatic;
    }

    internal bool BondOrderIsMetalCoordination(Atom beginAtom, Atom endAtom)
    {
        return IsBondOrderMetal(beginAtom.AtomicNumber) || IsBondOrderMetal(endAtom.AtomicNumber);
    }

    internal bool IsBondOrderMetal(int atomicNumber)
    {
        if (atomicNumber == 3 || atomicNumber == 4 || atomicNumber == 11 || atomicNumber == 12 || atomicNumber == 13)
        {
            return true;
        }

        if ((atomicNumber >= 19 && atomicNumber <= 31) || (atomicNumber >= 37 && atomicNumber <= 50))
        {
            return true;
        }

        return (atomicNumber >= 55 && atomicNumber <= 84) || (atomicNumber >= 87 && atomicNumber <= 116);
    }

    internal bool BondOrderIsAssigned(BondOrder order)
    {
        return order != BondOrder.Unknown && order != BondOrder.None;
    }

    internal bool CoordinatesCoverBond(BondOrderCoordinateSet coordinates, Bond bond)
    {
        return bond.BeginAtomId < coordinates.IsSet.Count
            && bond.EndAtomId < coordinates.IsSet.Count
            && coordinates.IsSet[bond.BeginAtomId]
            && coordinates.IsSet[bond.EndAtomId];
    }

    internal double CalculateBondOrderDistance(BondOrderCoordinateSet coordinates, Bond bond, BondOrderPerceptionOptions options, BondOrderCoordinateDimension dimension)
    {
        double dx = coordinates.X[bond.BeginAtomId] - coordinates.X[bond.EndAtomId];
        double dy = coordinates.Y[bond.BeginAtomId] - coordinates.Y[bond.EndAtomId];
        double squaredDistance = dx * dx + dy * dy;
        if (dimension == BondOrderCoordinateDimension.ThreeDimensional)
        {
            double dz = coordinates.Z[bond.BeginAtomId] - coordinates.Z[bond.EndAtomId];
            squaredDistance = squaredDistance + dz * dz;
        }

        return Math.Sqrt(squaredDistance) * options.CoordinateScale;
    }

    internal int GetMaximumPerceivedOrder(int beginAtomicNumber, int endAtomicNumber)
    {
        if (IsSingleOrderOnlyElement(beginAtomicNumber) || IsSingleOrderOnlyElement(endAtomicNumber))
        {
            return 1;
        }

        if (beginAtomicNumber == 8 || endAtomicNumber == 8)
        {
            return 2;
        }

        return 3;
    }

    internal bool IsSingleOrderOnlyElement(int atomicNumber)
    {
        return atomicNumber == 1 || atomicNumber == 9 || atomicNumber == 17 || atomicNumber == 35 || atomicNumber == 53;
    }

    internal BondOrderProposal FindBondOrderProposal(double distance, double referenceLength, int maximumOrder)
    {
        BondOrderProposal proposal = new BondOrderProposal();
        double ratio = distance / referenceLength;
        ConsiderBondOrderCandidate(proposal, BondOrder.Single, Math.Abs(ratio - 1.0));
        if (maximumOrder >= 2)
        {
            ConsiderBondOrderCandidate(proposal, BondOrder.Double, Math.Abs(ratio - 0.88));
        }

        if (maximumOrder >= 3)
        {
            ConsiderBondOrderCandidate(proposal, BondOrder.Triple, Math.Abs(ratio - 0.78));
        }

        return proposal;
    }

    internal void ConsiderBondOrderCandidate(BondOrderProposal proposal, BondOrder order, double error)
    {
        if (error < proposal.RelativeError)
        {
            proposal.AlternativeGap = proposal.RelativeError - error;
            proposal.RelativeError = error;
            proposal.Order = order;
        }
        else
        {
            double gap = error - proposal.RelativeError;
            if (proposal.AlternativeGap == 0.0 || gap < proposal.AlternativeGap)
            {
                proposal.AlternativeGap = gap;
            }
        }

        if (proposal.AlternativeGap > 1.0)
        {
            proposal.AlternativeGap = 1.0;
        }
    }

    internal BondOrderConfidence GetBondOrderConfidence(BondOrderProposal proposal)
    {
        if (proposal.RelativeError <= 0.035 && proposal.AlternativeGap >= 0.07)
        {
            return BondOrderConfidence.High;
        }

        if (proposal.RelativeError <= 0.07 && proposal.AlternativeGap >= 0.045)
        {
            return BondOrderConfidence.Medium;
        }

        return BondOrderConfidence.Low;
    }

    internal bool BondOrderCreatesHypervalence(Molecule molecule, Bond bond)
    {
        ValenceResult beginValence = Valences.CalculateAtomValence(molecule, bond.BeginAtomId);
        ValenceResult endValence = Valences.CalculateAtomValence(molecule, bond.EndAtomId);
        return BondOrderValenceExceeded(beginValence) || BondOrderValenceExceeded(endValence);
    }

    internal bool BondOrderValenceExceeded(ValenceResult valence)
    {
        if (valence.TotalValence <= valence.MaximumKnownValence)
        {
            return false;
        }

        return !valence.IsChargeAdjustedAllowed && !valence.IsRadicalAdjustedAllowed && !valence.IsHypervalentAllowed;
    }

    internal void AddSpecialBondOrderAssignment(BondOrderPerceptionResult result, Bond bond, BondOrder proposedOrder, string reason, string code)
    {
        BondOrderAssignment assignment = new BondOrderAssignment();
        assignment.BondId = bond.Id;
        assignment.PreviousOrder = bond.Order;
        assignment.PerceivedOrder = proposedOrder;
        assignment.Confidence = BondOrderConfidence.Low;
        assignment.IsAmbiguous = true;
        assignment.Reason = reason;
        result.Assignments.Add(assignment);
        result.UnresolvedCount = result.UnresolvedCount + 1;
        AddBondOrderWarning(result, code, reason, bond.Id);
    }

    internal void AddBondOrderWarning(BondOrderPerceptionResult result, string code, string message, int bondId)
    {
        MoleculeError error = result.Report.AddWarning(code, message);
        error.BondId = bondId;
    }

    internal void AddBondOrderError(BondOrderPerceptionResult result, string code, string message, int bondId)
    {
        MoleculeError error = result.Report.AddError(code, message);
        error.BondId = bondId;
    }

    internal bool IsFiniteBondOrderNumber(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
