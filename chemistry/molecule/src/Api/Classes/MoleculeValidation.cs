namespace molecule;


/// <summary>
/// Provides molecule validation data and operations for molecular graphs.
/// </summary>
public class MoleculeValidation : IMoleculeValidation
{
    internal ElementTable Elements;
    internal ValenceCalculator ValenceCalculator;
    internal StereoValidator Stereo;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeValidation"/> class.
    /// </summary>
    public MoleculeValidation()
    {
        Elements = new ElementTable();
        ValenceCalculator = new ValenceCalculator();
        Stereo = new StereoValidator();
    }

    /// <summary>
    /// Returns the report severity corresponding to a diagnostic.
    /// </summary>
    /// <param name="error">The diagnostic to inspect.</param>
    /// <returns>The corresponding report severity.</returns>
    public ReportSeverity GetReportSeverity(MoleculeError? error)
    {
        if (error == null || error.Severity == ErrorSeverity.Info)
        {
            return ReportSeverity.Info;
        }

        if (error.Severity == ErrorSeverity.Warning)
        {
            return ReportSeverity.Warning;
        }

        if (error.Severity == ErrorSeverity.Fatal)
        {
            return ReportSeverity.Fatal;
        }

        return ReportSeverity.Error;
    }

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MoleculeReport Validate(Molecule? molecule)
    {
        return ValidateWithOptions(molecule, null);
    }

    /// <summary>
    /// Checks a molecule using configurable empty policy and optional parser-originated diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The combined parser and structural validation diagnostics.</returns>
    public MoleculeReport ValidateWithOptions(Molecule? molecule, MoleculeValidationOptions? options)
    {
        MoleculeReport report = new MoleculeReport();
        MoleculeValidationOptions safeOptions = CopyOptions(options);
        CopyDiagnostics(safeOptions.ParserReport, report);

        if (!EmptyPolicyIsValid(safeOptions.EmptyMoleculePolicy))
        {
            AddError(report, "VALIDATION_OPTIONS_INVALID", "The empty-molecule validation policy is invalid.", -1, -1);
            return report;
        }

        if (molecule == null)
        {
            AddError(report, "NULL_MOLECULE", "Molecule is null.", -1, -1);
            return report;
        }

        if (molecule.Atoms.Count == 0)
        {
            AddEmptyMoleculeDiagnostic(report, safeOptions.EmptyMoleculePolicy);
        }

        ValidateAtoms(molecule, report);
        ValidateBonds(molecule, report);
        ValidateDuplicateBonds(molecule, report);
        ValidateValences(molecule, report);
        ValidateAromaticity(molecule, report);
        ValidateAtomCoordinates(molecule, report);
        ValidateConformers(molecule, report);
        ValidateStereoDepiction(molecule, report);
        MergeDiagnostics(Stereo.Validate(molecule), report);
        return report;
    }

    /// <summary>
    /// Checks a query graph and its disconnected component-group constraints.
    /// </summary>
    /// <param name="query">The query graph to process.</param>
    /// <returns>The query validation diagnostics.</returns>
    public MoleculeReport ValidateQuery(QueryMolecule? query)
    {
        MoleculeReport report = new MoleculeReport();
        if (query == null)
        {
            AddError(report, "NULL_QUERY", "Query molecule is null.", -1, -1);
            return report;
        }

        if (query.Atoms.Count == 0)
        {
            AddError(report, "EMPTY_QUERY", "Query molecule has no atoms.", -1, -1);
        }

        if (query.ComponentGroupIdsByAtomId.Count != query.Atoms.Count)
        {
            AddError(report, "QUERY_COMPONENT_GROUP_SIZE", "Query component-group identifiers must have one entry per atom.", -1, -1);
        }

        ValidateQueryAtoms(query, report);
        ValidateQueryBonds(query, report);
        ValidateQueryGroupSequence(query, report);
        return report;
    }

    internal MoleculeValidationOptions CopyOptions(MoleculeValidationOptions? options)
    {
        MoleculeValidationOptions copy = new MoleculeValidationOptions();
        if (options != null)
        {
            copy.EmptyMoleculePolicy = options.EmptyMoleculePolicy;
            copy.ParserReport = options.ParserReport;
        }

        return copy;
    }

    internal bool EmptyPolicyIsValid(EmptyMoleculePolicy policy)
    {
        return policy == EmptyMoleculePolicy.Error || policy == EmptyMoleculePolicy.Warning || policy == EmptyMoleculePolicy.Allow;
    }

    internal void AddEmptyMoleculeDiagnostic(MoleculeReport report, EmptyMoleculePolicy policy)
    {
        if (policy == EmptyMoleculePolicy.Error)
        {
            AddError(report, "EMPTY_MOLECULE", "Molecule has no atoms.", -1, -1);
        }
        else if (policy == EmptyMoleculePolicy.Warning)
        {
            AddWarning(report, "EMPTY_MOLECULE", "Molecule has no atoms.", -1, -1);
        }
    }

    internal void ValidateAtoms(Molecule molecule, MoleculeReport report)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (!Elements.IsKnownElement(atom.AtomicNumber))
            {
                AddError(report, "INVALID_ATOM", "Atom has an unknown atomic number.", atomIndex, -1);
                AddError(report, "UNSUPPORTED_ELEMENT", "Atom element is not supported by the built-in periodic table.", atomIndex, -1);
            }

            if (atom.FormalCharge < -8 || atom.FormalCharge > 8)
            {
                AddError(report, "INVALID_CHARGE", "Atom formal charge is outside the supported range.", atomIndex, -1);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ValidateBonds(Molecule molecule, MoleculeReport report)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
            {
                AddError(report, "INVALID_BOND_ATOM", "Bond references an atom id that does not exist.", -1, bondIndex);
            }
            else
            {
                if (bond.BeginAtomId == bond.EndAtomId)
                {
                    AddError(report, "SELF_BOND", "Bond connects an atom to itself.", bond.BeginAtomId, bondIndex);
                }
            }

            if (bond.Order == BondOrder.Unknown || bond.Order == BondOrder.None)
            {
                AddError(report, "INVALID_BOND_ORDER", "Bond order is not chemically defined.", -1, bondIndex);
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void ValidateDuplicateBonds(Molecule molecule, MoleculeReport report)
    {
        int leftIndex = 0;
        while (leftIndex < molecule.Bonds.Count)
        {
            Bond left = molecule.Bonds[leftIndex];
            if (molecule.IsValidAtomId(left.BeginAtomId) && molecule.IsValidAtomId(left.EndAtomId) && left.BeginAtomId != left.EndAtomId)
            {
                int rightIndex = leftIndex + 1;
                while (rightIndex < molecule.Bonds.Count)
                {
                    Bond right = molecule.Bonds[rightIndex];
                    if (IsSameAtomPair(left, right))
                    {
                        AddError(report, "DUPLICATE_BOND", "Multiple bonds connect the same atom pair.", -1, rightIndex);
                    }

                    rightIndex = rightIndex + 1;
                }
            }

            leftIndex = leftIndex + 1;
        }
    }

    internal void ValidateValences(Molecule molecule, MoleculeReport report)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (Elements.IsKnownElement(atom.AtomicNumber))
            {
                ValenceResult result = ValenceCalculator.CalculateAtomValence(molecule, atomIndex);
                if (!result.IsAllowed)
                {
                    AddError(report, "VALENCE", "Atom valence is not allowed by current valence rules.", atomIndex, -1);
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ValidateAromaticity(Molecule molecule, MoleculeReport report)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            bool aromaticOrder = bond.Order == BondOrder.Aromatic;
            if (aromaticOrder != bond.IsAromatic)
            {
                AddError(report, "AROMATICITY_BOND_MISMATCH", "Aromatic bond order and aromatic flag must agree.", -1, bondIndex);
            }

            if ((aromaticOrder || bond.IsAromatic) && molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId))
            {
                if (!molecule.Atoms[bond.BeginAtomId].IsAromatic || !molecule.Atoms[bond.EndAtomId].IsAromatic)
                {
                    AddError(report, "AROMATICITY_ATOM_BOND_MISMATCH", "An aromatic bond must connect two aromatic atoms.", -1, bondIndex);
                }
            }

            bondIndex = bondIndex + 1;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (molecule.Atoms[atomIndex].IsAromatic && !AtomHasAromaticBond(molecule, atomIndex))
            {
                AddError(report, "AROMATICITY_ATOM_WITHOUT_BOND", "An aromatic atom must participate in an aromatic bond.", atomIndex, -1);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal bool AtomHasAromaticBond(Molecule molecule, int atomId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && (bond.IsAromatic || bond.Order == BondOrder.Aromatic))
            {
                return true;
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }

    internal void ValidateAtomCoordinates(Molecule molecule, MoleculeReport report)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (atom.HasCoordinates)
            {
                if (!CoordinatesAreFinite(atom.X, atom.Y, atom.Z))
                {
                    AddError(report, "NONFINITE_COORDINATE", "Assigned atom coordinates must contain only finite values.", atomIndex, -1);
                }
                else if (!CoordinatesArePossible(atom.X, atom.Y, atom.Z))
                {
                    AddError(report, "IMPOSSIBLE_COORDINATE", "Assigned atom coordinates exceed the supported magnitude.", atomIndex, -1);
                }
            }

            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId))
            {
                Atom begin = molecule.Atoms[bond.BeginAtomId];
                Atom end = molecule.Atoms[bond.EndAtomId];
                if (begin.HasCoordinates && end.HasCoordinates && CoordinatesAreFinite(begin.X, begin.Y, begin.Z) && CoordinatesAreFinite(end.X, end.Y, end.Z))
                {
                    double dx = begin.X - end.X;
                    double dy = begin.Y - end.Y;
                    double dz = begin.Z - end.Z;
                    if (dx * dx + dy * dy + dz * dz <= 0.000000000000000000000001)
                    {
                        AddError(report, "IMPOSSIBLE_BOND_COORDINATES", "A bond cannot have coincident endpoint coordinates.", -1, bondIndex);
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void ValidateConformers(Molecule molecule, MoleculeReport report)
    {
        int conformerIndex = 0;
        while (conformerIndex < molecule.Conformers.Count)
        {
            Conformer conformer = molecule.Conformers[conformerIndex];
            if (conformer == null)
            {
                AddError(report, "NULL_CONFORMER", "Molecule conformer collection contains a null entry.", -1, -1);
            }
            else
            {
                if (conformer.Coordinates.Count != molecule.Atoms.Count)
                {
                    AddError(report, "CONFORMER_SIZE_MISMATCH", "Conformer coordinate count must equal molecule atom count.", -1, -1);
                }

                ValidateConformerCoordinates(conformer, conformerIndex, report);
            }

            conformerIndex = conformerIndex + 1;
        }
    }

    internal void ValidateConformerCoordinates(Conformer conformer, int conformerIndex, MoleculeReport report)
    {
        int coordinateIndex = 0;
        while (coordinateIndex < conformer.Coordinates.Count)
        {
            Coordinate3D coordinate = conformer.Coordinates[coordinateIndex];
            if (coordinate == null)
            {
                AddError(report, "NULL_CONFORMER_COORDINATE", "Conformer contains a null coordinate entry.", coordinateIndex, -1);
            }
            else if (coordinate.IsSet)
            {
                if (!CoordinatesAreFinite(coordinate.X, coordinate.Y, coordinate.Z))
                {
                    AddError(report, "NONFINITE_CONFORMER_COORDINATE", "Assigned conformer coordinates must contain only finite values.", coordinateIndex, -1);
                }
                else if (!CoordinatesArePossible(coordinate.X, coordinate.Y, coordinate.Z))
                {
                    AddError(report, "IMPOSSIBLE_CONFORMER_COORDINATE", "Assigned conformer coordinates exceed the supported magnitude.", coordinateIndex, -1);
                }
            }

            coordinateIndex = coordinateIndex + 1;
        }
    }

    internal bool CoordinatesAreFinite(double x, double y, double z)
    {
        return IsFiniteNumber(x) && IsFiniteNumber(y) && IsFiniteNumber(z);
    }

    internal bool CoordinatesArePossible(double x, double y, double z)
    {
        double limit = 1000000000.0;
        return System.Math.Abs(x) <= limit && System.Math.Abs(y) <= limit && System.Math.Abs(z) <= limit;
    }

    internal bool IsFiniteNumber(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal void ValidateStereoDepiction(Molecule molecule, MoleculeReport report)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (BondUsesSingleBondStereo(bond.Stereo) && bond.Order != BondOrder.Single)
            {
                AddError(report, "STEREO_DEPICTION_SINGLE_BOND_REQUIRED", "Wedge, hash, or unspecified bond stereo requires a single bond.", -1, bondIndex);
            }

            if (BondUsesDoubleBondStereo(bond.Stereo) && bond.Order != BondOrder.Double)
            {
                AddError(report, "STEREO_DEPICTION_DOUBLE_BOND_REQUIRED", "Cis, trans, E, or Z bond stereo requires a double bond.", -1, bondIndex);
            }

            if (bond.Stereo == BondStereo.Unknown)
            {
                AddError(report, "STEREO_DEPICTION_UNKNOWN", "Bond stereo depiction cannot use an unknown value.", -1, bondIndex);
            }

            if (BondUsesSingleBondStereo(bond.Stereo) && molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId))
            {
                if (!molecule.Atoms[bond.BeginAtomId].HasCoordinates || !molecule.Atoms[bond.EndAtomId].HasCoordinates)
                {
                    AddError(report, "STEREO_DEPICTION_COORDINATES_MISSING", "Wedge, hash, or unspecified bond stereo requires coordinates on both bond atoms.", -1, bondIndex);
                }
            }

            if ((bond.Direction == BondDirection.Up || bond.Direction == BondDirection.Down || bond.Direction == BondDirection.Either) && bond.Order != BondOrder.Single)
            {
                AddError(report, "STEREO_DIRECTION_SINGLE_BOND_REQUIRED", "Directional up, down, or either depiction requires a single bond.", -1, bondIndex);
            }

            if (bond.Direction == BondDirection.Crossed && bond.Order != BondOrder.Double)
            {
                AddError(report, "STEREO_DIRECTION_DOUBLE_BOND_REQUIRED", "Crossed-bond depiction requires a double bond.", -1, bondIndex);
            }

            if (bond.Stereo != BondStereo.None && bond.Direction != BondDirection.None)
            {
                AddError(report, "STEREO_DEPICTION_CONFLICT", "Bond stereo and directional depiction fields cannot both be assigned.", -1, bondIndex);
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal bool BondUsesSingleBondStereo(BondStereo stereo)
    {
        return stereo == BondStereo.Wedge || stereo == BondStereo.Hash || stereo == BondStereo.UpOrDown;
    }

    internal bool BondUsesDoubleBondStereo(BondStereo stereo)
    {
        return stereo == BondStereo.Cis || stereo == BondStereo.Trans || stereo == BondStereo.E || stereo == BondStereo.Z;
    }

    internal void ValidateQueryAtoms(QueryMolecule query, MoleculeReport report)
    {
        int atomIndex = 0;
        while (atomIndex < query.Atoms.Count)
        {
            QueryAtom atom = query.Atoms[atomIndex];
            if (atom == null)
            {
                AddError(report, "QUERY_NULL_ATOM", "Query atom collection contains a null entry.", atomIndex, -1);
            }
            else
            {
                if (atom.HasAtomicNumber && !Elements.IsKnownElement(atom.AtomicNumber))
                {
                    AddError(report, "QUERY_UNSUPPORTED_ELEMENT", "Query atom references an unsupported element.", atomIndex, -1);
                }

                int numberIndex = 0;
                while (numberIndex < atom.AtomicNumbers.Count)
                {
                    if (!Elements.IsKnownElement(atom.AtomicNumbers[numberIndex]))
                    {
                        AddError(report, "QUERY_UNSUPPORTED_ELEMENT", "Query atom list references an unsupported element.", atomIndex, -1);
                    }

                    numberIndex = numberIndex + 1;
                }

                if (atomIndex < query.ComponentGroupIdsByAtomId.Count)
                {
                    int groupId = query.ComponentGroupIdsByAtomId[atomIndex];
                    if (groupId < -1)
                    {
                        AddError(report, "QUERY_COMPONENT_GROUP_INVALID", "Query component-group identifiers must be minus one or nonnegative.", atomIndex, -1);
                    }

                    if (atom.ComponentGroupId != groupId)
                    {
                        AddError(report, "QUERY_COMPONENT_GROUP_MISMATCH", "Query atom and molecule component-group identifiers must agree.", atomIndex, -1);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ValidateQueryBonds(QueryMolecule query, MoleculeReport report)
    {
        int bondIndex = 0;
        while (bondIndex < query.Bonds.Count)
        {
            QueryBond bond = query.Bonds[bondIndex];
            if (bond == null)
            {
                AddError(report, "QUERY_NULL_BOND", "Query bond collection contains a null entry.", -1, bondIndex);
            }
            else if (!query.IsValidAtomId(bond.BeginQueryAtomId) || !query.IsValidAtomId(bond.EndQueryAtomId))
            {
                AddError(report, "QUERY_INVALID_BOND_ATOM", "Query bond references an atom id that does not exist.", -1, bondIndex);
            }
            else
            {
                int beginGroup = GetQueryGroupId(query, bond.BeginQueryAtomId);
                int endGroup = GetQueryGroupId(query, bond.EndQueryAtomId);
                if (beginGroup != endGroup && (beginGroup >= 0 || endGroup >= 0))
                {
                    AddError(report, "QUERY_COMPONENT_GROUP_BOND_CONFLICT", "Bonded query atoms must use the same component-group constraint.", -1, bondIndex);
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal int GetQueryGroupId(QueryMolecule query, int atomId)
    {
        if (atomId < 0 || atomId >= query.ComponentGroupIdsByAtomId.Count)
        {
            return -1;
        }

        return query.ComponentGroupIdsByAtomId[atomId];
    }

    internal void ValidateQueryGroupSequence(QueryMolecule query, MoleculeReport report)
    {
        int maximum = -1;
        int index = 0;
        while (index < query.ComponentGroupIdsByAtomId.Count)
        {
            if (query.ComponentGroupIdsByAtomId[index] > maximum)
            {
                maximum = query.ComponentGroupIdsByAtomId[index];
            }

            index = index + 1;
        }

        int groupId = 0;
        while (groupId <= maximum)
        {
            if (!QueryContainsGroup(query, groupId))
            {
                AddError(report, "QUERY_COMPONENT_GROUP_GAP", "Query component-group identifiers must form a contiguous sequence beginning at zero.", -1, -1);
            }

            groupId = groupId + 1;
        }
    }

    internal bool QueryContainsGroup(QueryMolecule query, int groupId)
    {
        int index = 0;
        while (index < query.ComponentGroupIdsByAtomId.Count)
        {
            if (query.ComponentGroupIdsByAtomId[index] == groupId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void CopyDiagnostics(MoleculeReport? source, MoleculeReport target)
    {
        if (source == null)
        {
            return;
        }

        int index = 0;
        while (index < source.Errors.Count)
        {
            MoleculeError error = source.Errors[index];
            if (error != null)
            {
                DiagnosticLocation location = new DiagnosticLocation(error.Location.SourceName, error.Location.LineNumber, error.Location.ColumnNumber, error.Location.CharacterIndex, error.Location.Context);
                target.Add(new MoleculeError(error.Code, error.Message, location, error.AtomId, error.BondId, error.Severity));
            }

            index = index + 1;
        }
    }

    internal void MergeDiagnostics(MoleculeReport source, MoleculeReport target)
    {
        CopyDiagnostics(source, target);
    }

    internal bool IsSameAtomPair(Bond left, Bond right)
    {
        if (left.BeginAtomId == right.BeginAtomId && left.EndAtomId == right.EndAtomId)
        {
            return true;
        }

        if (left.BeginAtomId == right.EndAtomId && left.EndAtomId == right.BeginAtomId)
        {
            return true;
        }

        return false;
    }

    internal void AddError(MoleculeReport report, string code, string message, int atomId, int bondId)
    {
        report.Add(new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), atomId, bondId, ErrorSeverity.Error));
    }

    internal void AddWarning(MoleculeReport report, string code, string message, int atomId, int bondId)
    {
        report.Add(new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), atomId, bondId, ErrorSeverity.Warning));
    }
}
