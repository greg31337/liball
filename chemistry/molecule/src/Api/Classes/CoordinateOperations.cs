using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Normalizes two-dimensional atom coordinates and aligns mapped three-dimensional conformers without mutating inputs.
/// </summary>
public class CoordinateOperations : ICoordinateOperations
{
    /// <summary>
    /// Initializes deterministic coordinate operations.
    /// </summary>
    public CoordinateOperations()
    {
    }

    /// <summary>
    /// Centers assigned atom coordinates, aligns their principal axis, and applies the requested two-dimensional scale policy.
    /// </summary>
    /// <param name="molecule">The molecule whose atom coordinates are normalized without mutating the input.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized molecule and applied transform.</returns>
    public CoordinateNormalizationResult Normalize2D(Molecule? molecule, CoordinateNormalizationOptions? options)
    {
        CoordinateNormalizationResult result = new CoordinateNormalizationResult();
        if (molecule == null)
        {
            result.Report.AddError("COORDINATE_NORMALIZATION_NULL_MOLECULE", "Two-dimensional coordinate normalization requires a molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        CoordinateNormalizationOptions safeOptions = CopyNormalizationOptions(options);
        if (!NormalizationOptionsAreValid(safeOptions))
        {
            result.Report.AddError("COORDINATE_NORMALIZATION_OPTIONS_INVALID", "The coordinate scale policy and target average bond length are invalid.");
            return result;
        }

        int coordinateCount = CountAssignedAtomCoordinates(molecule);
        result.CoordinateCount = coordinateCount;
        if (coordinateCount == 0)
        {
            result.Report.AddError("COORDINATE_NORMALIZATION_NO_COORDINATES", "The molecule has no assigned two-dimensional atom coordinates.");
            return result;
        }

        if (!AtomCoordinatesAreFinite(molecule))
        {
            result.Report.AddError("COORDINATE_NORMALIZATION_NONFINITE", "Assigned atom coordinates must contain only finite values.");
            return result;
        }

        Coordinate2D centroid = CalculateAtomCentroid2D(molecule);
        result.TranslationX = -centroid.X;
        result.TranslationY = -centroid.Y;
        double rotation = CalculatePrincipalAxisRotation(molecule, centroid);
        rotation = ResolvePrincipalAxisDirection(molecule, centroid, rotation);
        rotation = NormalizeRadians(rotation);
        result.RotationDegrees = rotation * 180.0 / System.Math.PI;

        if (safeOptions.ScalePolicy == CoordinateNormalizationScalePolicy.AverageBondLength)
        {
            double averageBondLength = CalculateAverageBondLength2D(molecule);
            if (averageBondLength <= 0.0 || !IsFiniteNumber(averageBondLength))
            {
                result.Report.AddError("COORDINATE_NORMALIZATION_SCALE_UNAVAILABLE", "Average-bond-length scaling requires at least one finite nonzero bond length.");
                return result;
            }

            result.Scale = safeOptions.TargetAverageBondLength / averageBondLength;
        }

        ApplyNormalization2D(result.Molecule, result.TranslationX, result.TranslationY, rotation, result.Scale);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Aligns a moving conformer copy to a reference using mapped coordinates and a deterministic proper rotation.
    /// </summary>
    /// <param name="moving">The conformer to transform without mutating the input.</param>
    /// <param name="reference">The fixed reference conformer.</param>
    /// <param name="options">The mapping and scale options.</param>
    /// <returns>The aligned conformer, applied transform, RMSD, and diagnostics.</returns>
    public ConformerAlignmentResult AlignConformer(Conformer? moving, Conformer? reference, ConformerAlignmentOptions? options)
    {
        ConformerAlignmentResult result = new ConformerAlignmentResult();
        if (moving == null || reference == null)
        {
            result.Report.AddError("CONFORMER_ALIGNMENT_NULL_CONFORMER", "Conformer alignment requires moving and reference conformers.");
            return result;
        }

        result.Conformer = moving.Clone();
        ConformerAlignmentScalePolicy scalePolicy = ConformerAlignmentScalePolicy.Preserve;
        if (options != null)
        {
            scalePolicy = options.ScalePolicy;
        }

        if (scalePolicy != ConformerAlignmentScalePolicy.Preserve && scalePolicy != ConformerAlignmentScalePolicy.BestFit)
        {
            result.Report.AddError("CONFORMER_ALIGNMENT_OPTIONS_INVALID", "The conformer alignment scale policy is invalid.");
            return result;
        }

        if (!AssignedConformerCoordinatesAreFinite(moving))
        {
            result.Report.AddError("CONFORMER_ALIGNMENT_NONFINITE", "Assigned moving conformer coordinates must contain only finite values.");
            return result;
        }

        if (!CollectAlignmentMappings(moving, reference, options, result))
        {
            return result;
        }

        Coordinate3D movingCentroid = CalculateMappedCentroid(moving, result.AtomMappings, true);
        Coordinate3D referenceCentroid = CalculateMappedCentroid(reference, result.AtomMappings, false);
        double[] covariance = CalculateMappedCovariance(moving, reference, result.AtomMappings, movingCentroid, referenceCentroid);
        double[] quaternionMatrix = BuildQuaternionMatrix(covariance);
        double[] quaternion = FindLargestEigenvector(quaternionMatrix);
        SetRotationFromQuaternion(result, quaternion);

        if (scalePolicy == ConformerAlignmentScalePolicy.BestFit)
        {
            double scale = CalculateBestFitScale(moving, reference, result.AtomMappings, movingCentroid, referenceCentroid, result);
            if (!IsFiniteNumber(scale) || scale <= 0.0)
            {
                result.Report.AddError("CONFORMER_ALIGNMENT_SCALE_UNAVAILABLE", "Best-fit scaling requires noncoincident mapped moving coordinates and a positive finite scale.");
                return result;
            }

            result.Scale = scale;
        }

        Coordinate3D rotatedMovingCentroid = RotateCoordinate(movingCentroid, result);
        result.TranslationX = referenceCentroid.X - result.Scale * rotatedMovingCentroid.X;
        result.TranslationY = referenceCentroid.Y - result.Scale * rotatedMovingCentroid.Y;
        result.TranslationZ = referenceCentroid.Z - result.Scale * rotatedMovingCentroid.Z;
        ApplyConformerTransform(result.Conformer, result);
        result.Rmsd = CalculateMappedRmsd(result.Conformer, reference, result.AtomMappings);
        result.Success = true;
        return result;
    }

    internal CoordinateNormalizationOptions CopyNormalizationOptions(CoordinateNormalizationOptions? options)
    {
        CoordinateNormalizationOptions copy = new CoordinateNormalizationOptions();
        if (options != null)
        {
            copy.ScalePolicy = options.ScalePolicy;
            copy.TargetAverageBondLength = options.TargetAverageBondLength;
        }

        return copy;
    }

    internal bool NormalizationOptionsAreValid(CoordinateNormalizationOptions options)
    {
        if (options.ScalePolicy != CoordinateNormalizationScalePolicy.Preserve && options.ScalePolicy != CoordinateNormalizationScalePolicy.AverageBondLength)
        {
            return false;
        }

        if (options.ScalePolicy == CoordinateNormalizationScalePolicy.AverageBondLength)
        {
            return IsFiniteNumber(options.TargetAverageBondLength) && options.TargetAverageBondLength > 0.0;
        }

        return true;
    }

    internal int CountAssignedAtomCoordinates(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].HasCoordinates)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool AtomCoordinatesAreFinite(Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates && (!IsFiniteNumber(atom.X) || !IsFiniteNumber(atom.Y)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal Coordinate2D CalculateAtomCentroid2D(Molecule molecule)
    {
        double x = 0.0;
        double y = 0.0;
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                x = x + atom.X;
                y = y + atom.Y;
                count = count + 1;
            }

            index = index + 1;
        }

        return new Coordinate2D(x / (double)count, y / (double)count);
    }

    internal double CalculatePrincipalAxisRotation(Molecule molecule, Coordinate2D centroid)
    {
        double xx = 0.0;
        double yy = 0.0;
        double xy = 0.0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                double x = atom.X - centroid.X;
                double y = atom.Y - centroid.Y;
                xx = xx + x * x;
                yy = yy + y * y;
                xy = xy + x * y;
            }

            index = index + 1;
        }

        if (System.Math.Abs(xx - yy) <= 0.000000000000001 && System.Math.Abs(xy) <= 0.000000000000001)
        {
            return 0.0;
        }

        double principalAngle = 0.5 * System.Math.Atan2(2.0 * xy, xx - yy);
        return -principalAngle;
    }

    internal double ResolvePrincipalAxisDirection(Molecule molecule, Coordinate2D centroid, double rotation)
    {
        double cosine = System.Math.Cos(rotation);
        double sine = System.Math.Sin(rotation);
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                double centeredX = atom.X - centroid.X;
                double centeredY = atom.Y - centroid.Y;
                double rotatedX = cosine * centeredX - sine * centeredY;
                double rotatedY = sine * centeredX + cosine * centeredY;
                if (System.Math.Abs(rotatedX) > 0.000000000001 || System.Math.Abs(rotatedY) > 0.000000000001)
                {
                    if (rotatedX < 0.0 || (System.Math.Abs(rotatedX) <= 0.000000000001 && rotatedY < 0.0))
                    {
                        return rotation + System.Math.PI;
                    }

                    return rotation;
                }
            }

            index = index + 1;
        }

        return rotation;
    }

    internal double NormalizeRadians(double radians)
    {
        while (radians <= -System.Math.PI)
        {
            radians = radians + 2.0 * System.Math.PI;
        }

        while (radians > System.Math.PI)
        {
            radians = radians - 2.0 * System.Math.PI;
        }

        return radians;
    }

    internal double CalculateAverageBondLength2D(Molecule molecule)
    {
        double total = 0.0;
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId))
            {
                Atom begin = molecule.Atoms[bond.BeginAtomId];
                Atom end = molecule.Atoms[bond.EndAtomId];
                if (begin.HasCoordinates && end.HasCoordinates)
                {
                    double dx = end.X - begin.X;
                    double dy = end.Y - begin.Y;
                    double length = System.Math.Sqrt(dx * dx + dy * dy);
                    if (length > 0.0 && IsFiniteNumber(length))
                    {
                        total = total + length;
                        count = count + 1;
                    }
                }
            }

            index = index + 1;
        }

        if (count == 0)
        {
            return 0.0;
        }

        return total / (double)count;
    }

    internal void ApplyNormalization2D(Molecule molecule, double translationX, double translationY, double rotation, double scale)
    {
        double cosine = System.Math.Cos(rotation);
        double sine = System.Math.Sin(rotation);
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                double x = atom.X + translationX;
                double y = atom.Y + translationY;
                atom.X = scale * (cosine * x - sine * y);
                atom.Y = scale * (sine * x + cosine * y);
            }

            index = index + 1;
        }
    }

    internal bool AssignedConformerCoordinatesAreFinite(Conformer conformer)
    {
        int index = 0;
        while (index < conformer.Coordinates.Count)
        {
            Coordinate3D coordinate = conformer.Coordinates[index];
            if (coordinate.IsSet && (!IsFiniteNumber(coordinate.X) || !IsFiniteNumber(coordinate.Y) || !IsFiniteNumber(coordinate.Z)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool CollectAlignmentMappings(Conformer moving, Conformer reference, ConformerAlignmentOptions? options, ConformerAlignmentResult result)
    {
        if (options == null || options.AtomMappings == null || options.AtomMappings.Count == 0)
        {
            int count = moving.Coordinates.Count;
            if (reference.Coordinates.Count < count)
            {
                count = reference.Coordinates.Count;
            }

            int index = 0;
            while (index < count)
            {
                if (moving.Coordinates[index].IsSet && reference.Coordinates[index].IsSet)
                {
                    if (!CoordinateIsFinite(reference.Coordinates[index]))
                    {
                        result.Report.AddError("CONFORMER_ALIGNMENT_NONFINITE", "Mapped reference conformer coordinates must contain only finite values.");
                        return false;
                    }

                    result.AtomMappings.Add(new ConformerAtomMapping(index, index));
                }

                index = index + 1;
            }
        }
        else
        {
            List<ConformerAtomMapping> requestedMappings = options.AtomMappings;
            int mappingIndex = 0;
            while (mappingIndex < requestedMappings.Count)
            {
                ConformerAtomMapping mapping = requestedMappings[mappingIndex];
                if (mapping == null || mapping.MovingAtomId < 0 || mapping.MovingAtomId >= moving.Coordinates.Count || mapping.ReferenceAtomId < 0 || mapping.ReferenceAtomId >= reference.Coordinates.Count)
                {
                    result.Report.AddError("CONFORMER_ALIGNMENT_INVALID_MAPPING", "Every atom mapping must reference coordinates present in both conformer collections.");
                    return false;
                }

                if (MappingUsesMovingAtom(result.AtomMappings, mapping.MovingAtomId) || MappingUsesReferenceAtom(result.AtomMappings, mapping.ReferenceAtomId))
                {
                    result.Report.AddError("CONFORMER_ALIGNMENT_DUPLICATE_MAPPING", "Conformer atom mappings must be one-to-one.");
                    return false;
                }

                Coordinate3D movingCoordinate = moving.Coordinates[mapping.MovingAtomId];
                Coordinate3D referenceCoordinate = reference.Coordinates[mapping.ReferenceAtomId];
                if (!movingCoordinate.IsSet || !referenceCoordinate.IsSet)
                {
                    result.Report.AddError("CONFORMER_ALIGNMENT_COORDINATE_MISSING", "Every mapped atom must have assigned coordinates in both conformers.");
                    return false;
                }

                if (!CoordinateIsFinite(referenceCoordinate))
                {
                    result.Report.AddError("CONFORMER_ALIGNMENT_NONFINITE", "Mapped reference conformer coordinates must contain only finite values.");
                    return false;
                }

                result.AtomMappings.Add(new ConformerAtomMapping(mapping.MovingAtomId, mapping.ReferenceAtomId));
                mappingIndex = mappingIndex + 1;
            }
        }

        if (result.AtomMappings.Count == 0)
        {
            result.Report.AddError("CONFORMER_ALIGNMENT_NO_MAPPING", "Conformer alignment requires at least one pair of assigned mapped coordinates.");
            return false;
        }

        return true;
    }

    internal bool MappingUsesMovingAtom(List<ConformerAtomMapping> mappings, int atomId)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].MovingAtomId == atomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MappingUsesReferenceAtom(List<ConformerAtomMapping> mappings, int atomId)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].ReferenceAtomId == atomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal Coordinate3D CalculateMappedCentroid(Conformer conformer, List<ConformerAtomMapping> mappings, bool useMovingIds)
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
        int index = 0;
        while (index < mappings.Count)
        {
            int atomId = mappings[index].ReferenceAtomId;
            if (useMovingIds)
            {
                atomId = mappings[index].MovingAtomId;
            }

            Coordinate3D coordinate = conformer.Coordinates[atomId];
            x = x + coordinate.X;
            y = y + coordinate.Y;
            z = z + coordinate.Z;
            index = index + 1;
        }

        return new Coordinate3D(x / (double)mappings.Count, y / (double)mappings.Count, z / (double)mappings.Count);
    }

    internal double[] CalculateMappedCovariance(Conformer moving, Conformer reference, List<ConformerAtomMapping> mappings, Coordinate3D movingCentroid, Coordinate3D referenceCentroid)
    {
        double[] covariance = new double[9];
        int index = 0;
        while (index < mappings.Count)
        {
            Coordinate3D movingCoordinate = moving.Coordinates[mappings[index].MovingAtomId];
            Coordinate3D referenceCoordinate = reference.Coordinates[mappings[index].ReferenceAtomId];
            double mx = movingCoordinate.X - movingCentroid.X;
            double my = movingCoordinate.Y - movingCentroid.Y;
            double mz = movingCoordinate.Z - movingCentroid.Z;
            double rx = referenceCoordinate.X - referenceCentroid.X;
            double ry = referenceCoordinate.Y - referenceCentroid.Y;
            double rz = referenceCoordinate.Z - referenceCentroid.Z;
            covariance[0] = covariance[0] + mx * rx;
            covariance[1] = covariance[1] + mx * ry;
            covariance[2] = covariance[2] + mx * rz;
            covariance[3] = covariance[3] + my * rx;
            covariance[4] = covariance[4] + my * ry;
            covariance[5] = covariance[5] + my * rz;
            covariance[6] = covariance[6] + mz * rx;
            covariance[7] = covariance[7] + mz * ry;
            covariance[8] = covariance[8] + mz * rz;
            index = index + 1;
        }

        return covariance;
    }

    internal double[] BuildQuaternionMatrix(double[] covariance)
    {
        double xx = covariance[0];
        double xy = covariance[1];
        double xz = covariance[2];
        double yx = covariance[3];
        double yy = covariance[4];
        double yz = covariance[5];
        double zx = covariance[6];
        double zy = covariance[7];
        double zz = covariance[8];
        double[] matrix = new double[16];
        matrix[0] = xx + yy + zz;
        matrix[1] = yz - zy;
        matrix[2] = zx - xz;
        matrix[3] = xy - yx;
        matrix[4] = matrix[1];
        matrix[5] = xx - yy - zz;
        matrix[6] = xy + yx;
        matrix[7] = zx + xz;
        matrix[8] = matrix[2];
        matrix[9] = matrix[6];
        matrix[10] = -xx + yy - zz;
        matrix[11] = yz + zy;
        matrix[12] = matrix[3];
        matrix[13] = matrix[7];
        matrix[14] = matrix[11];
        matrix[15] = -xx - yy + zz;
        return matrix;
    }

    internal double[] FindLargestEigenvector(double[] source)
    {
        double[] matrix = new double[16];
        double[] vectors = new double[16];
        int index = 0;
        while (index < 16)
        {
            matrix[index] = source[index];
            vectors[index] = 0.0;
            index = index + 1;
        }

        vectors[0] = 1.0;
        vectors[5] = 1.0;
        vectors[10] = 1.0;
        vectors[15] = 1.0;
        int iteration = 0;
        while (iteration < 64)
        {
            int p = 0;
            int q = 1;
            double maximum = System.Math.Abs(matrix[1]);
            int row = 0;
            while (row < 4)
            {
                int column = row + 1;
                while (column < 4)
                {
                    double value = System.Math.Abs(matrix[row * 4 + column]);
                    if (value > maximum)
                    {
                        maximum = value;
                        p = row;
                        q = column;
                    }

                    column = column + 1;
                }

                row = row + 1;
            }

            if (maximum <= 0.000000000000001)
            {
                iteration = 64;
            }
            else
            {
                ApplyJacobiRotation(matrix, vectors, p, q);
                iteration = iteration + 1;
            }
        }

        int largest = 0;
        index = 1;
        while (index < 4)
        {
            if (matrix[index * 4 + index] > matrix[largest * 4 + largest])
            {
                largest = index;
            }

            index = index + 1;
        }

        double[] vector = new double[4];
        double lengthSquared = 0.0;
        index = 0;
        while (index < 4)
        {
            vector[index] = vectors[index * 4 + largest];
            lengthSquared = lengthSquared + vector[index] * vector[index];
            index = index + 1;
        }

        double length = System.Math.Sqrt(lengthSquared);
        if (length == 0.0)
        {
            vector[0] = 1.0;
            return vector;
        }

        index = 0;
        while (index < 4)
        {
            vector[index] = vector[index] / length;
            index = index + 1;
        }

        ResolveQuaternionSign(vector);
        return vector;
    }

    internal void ApplyJacobiRotation(double[] matrix, double[] vectors, int p, int q)
    {
        double app = matrix[p * 4 + p];
        double aqq = matrix[q * 4 + q];
        double apq = matrix[p * 4 + q];
        double angle = 0.5 * System.Math.Atan2(2.0 * apq, aqq - app);
        double cosine = System.Math.Cos(angle);
        double sine = System.Math.Sin(angle);
        int index = 0;
        while (index < 4)
        {
            if (index != p && index != q)
            {
                double aip = matrix[index * 4 + p];
                double aiq = matrix[index * 4 + q];
                double rotatedP = cosine * aip - sine * aiq;
                double rotatedQ = sine * aip + cosine * aiq;
                matrix[index * 4 + p] = rotatedP;
                matrix[p * 4 + index] = rotatedP;
                matrix[index * 4 + q] = rotatedQ;
                matrix[q * 4 + index] = rotatedQ;
            }

            index = index + 1;
        }

        matrix[p * 4 + p] = cosine * cosine * app - 2.0 * sine * cosine * apq + sine * sine * aqq;
        matrix[q * 4 + q] = sine * sine * app + 2.0 * sine * cosine * apq + cosine * cosine * aqq;
        matrix[p * 4 + q] = 0.0;
        matrix[q * 4 + p] = 0.0;
        index = 0;
        while (index < 4)
        {
            double vip = vectors[index * 4 + p];
            double viq = vectors[index * 4 + q];
            vectors[index * 4 + p] = cosine * vip - sine * viq;
            vectors[index * 4 + q] = sine * vip + cosine * viq;
            index = index + 1;
        }
    }

    internal void ResolveQuaternionSign(double[] quaternion)
    {
        int first = 0;
        while (first < 4 && System.Math.Abs(quaternion[first]) <= 0.000000000000001)
        {
            first = first + 1;
        }

        if (first < 4 && quaternion[first] < 0.0)
        {
            int index = 0;
            while (index < 4)
            {
                quaternion[index] = -quaternion[index];
                index = index + 1;
            }
        }
    }

    internal void SetRotationFromQuaternion(ConformerAlignmentResult result, double[] quaternion)
    {
        double w = quaternion[0];
        double x = quaternion[1];
        double y = quaternion[2];
        double z = quaternion[3];
        result.RotationXX = 1.0 - 2.0 * (y * y + z * z);
        result.RotationXY = 2.0 * (x * y - z * w);
        result.RotationXZ = 2.0 * (x * z + y * w);
        result.RotationYX = 2.0 * (x * y + z * w);
        result.RotationYY = 1.0 - 2.0 * (x * x + z * z);
        result.RotationYZ = 2.0 * (y * z - x * w);
        result.RotationZX = 2.0 * (x * z - y * w);
        result.RotationZY = 2.0 * (y * z + x * w);
        result.RotationZZ = 1.0 - 2.0 * (x * x + y * y);
    }

    internal double CalculateBestFitScale(Conformer moving, Conformer reference, List<ConformerAtomMapping> mappings, Coordinate3D movingCentroid, Coordinate3D referenceCentroid, ConformerAlignmentResult result)
    {
        double numerator = 0.0;
        double denominator = 0.0;
        int index = 0;
        while (index < mappings.Count)
        {
            Coordinate3D movingCoordinate = moving.Coordinates[mappings[index].MovingAtomId];
            Coordinate3D referenceCoordinate = reference.Coordinates[mappings[index].ReferenceAtomId];
            Coordinate3D centeredMoving = new Coordinate3D(movingCoordinate.X - movingCentroid.X, movingCoordinate.Y - movingCentroid.Y, movingCoordinate.Z - movingCentroid.Z);
            Coordinate3D centeredReference = new Coordinate3D(referenceCoordinate.X - referenceCentroid.X, referenceCoordinate.Y - referenceCentroid.Y, referenceCoordinate.Z - referenceCentroid.Z);
            Coordinate3D rotated = RotateCoordinate(centeredMoving, result);
            numerator = numerator + rotated.X * centeredReference.X + rotated.Y * centeredReference.Y + rotated.Z * centeredReference.Z;
            denominator = denominator + centeredMoving.X * centeredMoving.X + centeredMoving.Y * centeredMoving.Y + centeredMoving.Z * centeredMoving.Z;
            index = index + 1;
        }

        if (denominator <= 0.000000000000001)
        {
            return 0.0;
        }

        return numerator / denominator;
    }

    internal Coordinate3D RotateCoordinate(Coordinate3D coordinate, ConformerAlignmentResult result)
    {
        double x = result.RotationXX * coordinate.X + result.RotationXY * coordinate.Y + result.RotationXZ * coordinate.Z;
        double y = result.RotationYX * coordinate.X + result.RotationYY * coordinate.Y + result.RotationYZ * coordinate.Z;
        double z = result.RotationZX * coordinate.X + result.RotationZY * coordinate.Y + result.RotationZZ * coordinate.Z;
        return new Coordinate3D(x, y, z);
    }

    internal void ApplyConformerTransform(Conformer conformer, ConformerAlignmentResult result)
    {
        int index = 0;
        while (index < conformer.Coordinates.Count)
        {
            Coordinate3D coordinate = conformer.Coordinates[index];
            if (coordinate.IsSet)
            {
                Coordinate3D rotated = RotateCoordinate(coordinate, result);
                coordinate.X = result.Scale * rotated.X + result.TranslationX;
                coordinate.Y = result.Scale * rotated.Y + result.TranslationY;
                coordinate.Z = result.Scale * rotated.Z + result.TranslationZ;
            }

            index = index + 1;
        }
    }

    internal double CalculateMappedRmsd(Conformer aligned, Conformer reference, List<ConformerAtomMapping> mappings)
    {
        double sum = 0.0;
        int index = 0;
        while (index < mappings.Count)
        {
            Coordinate3D alignedCoordinate = aligned.Coordinates[mappings[index].MovingAtomId];
            Coordinate3D referenceCoordinate = reference.Coordinates[mappings[index].ReferenceAtomId];
            double dx = alignedCoordinate.X - referenceCoordinate.X;
            double dy = alignedCoordinate.Y - referenceCoordinate.Y;
            double dz = alignedCoordinate.Z - referenceCoordinate.Z;
            sum = sum + dx * dx + dy * dy + dz * dz;
            index = index + 1;
        }

        return System.Math.Sqrt(sum / (double)mappings.Count);
    }

    internal bool CoordinateIsFinite(Coordinate3D coordinate)
    {
        return IsFiniteNumber(coordinate.X) && IsFiniteNumber(coordinate.Y) && IsFiniteNumber(coordinate.Z);
    }

    internal bool IsFiniteNumber(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
