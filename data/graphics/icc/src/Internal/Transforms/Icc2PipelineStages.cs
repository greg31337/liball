namespace icc;

internal static class Icc2PipelineStages
{
    internal static PipelineStage CreateExtendedClutStage(IccProcessingElement element, int index, IccInterpolationKind requested)
    {
        IccClut clut = RequireClut(element, index, IccProcessingElementSignatures.ExtendedClut, "extended CLUT");
        IccInterpolationKind interpolation = ResolveInterpolation(clut, requested, index);
        ValidateClutShape(clut, element.InputChannels, element.OutputChannels, 16, index, "extended CLUT");
        PipelineStage stage = CreateClutExecutionStage(clut, element.InputChannels, element.OutputChannels, interpolation, $"ICC.2 extended CLUT element {index}");
        return stage;
    }

    internal static PipelineStage CreateTintArrayStage(IccProcessingElement element, int index)
    {
        IccClut clut = RequireClut(element, index, IccProcessingElementSignatures.TintArray, "tint array");
        if (element.InputChannels != 1)
        {
            throw new IccTransformException($"ICC.2 tint-array element {index} requires one input channel.");
        }

        ValidateClutShape(clut, 1, element.OutputChannels, 1, index, "tint array");
        return CreateClutExecutionStage(clut, 1, element.OutputChannels, IccInterpolationKind.Linear, $"ICC.2 tint-array element {index}");
    }

    internal static PipelineStage CreateSparseMatrixStage(IccProcessingElement element, int index, IccInterpolationKind requested)
    {
        IccSparseMatrixArray? source = element.SparseMatrixLut;
        if (element.Signature != IccProcessingElementSignatures.SparseMatrix || source == null || source.GridPoints == null || source.Matrices == null
            || source.EquivalentOutputChannels != element.OutputChannels || source.GridPoints.Length != element.InputChannels || element.InputChannels > 16)
        {
            throw new IccTransformException($"ICC.2 sparse-matrix element {index} has inconsistent channels or LUT data.");
        }

        int matrixCount = ValidateGrid(source.GridPoints, element.InputChannels, index, "sparse-matrix");
        if (source.Matrices.Length != matrixCount)
        {
            throw new IccTransformException($"ICC.2 sparse-matrix element {index} requires exactly {matrixCount} matrices.");
        }

        double[] values = new double[matrixCount * element.OutputChannels];
        int matrixIndex = 0;
        int rows = 0;
        int columns = 0;
        while (matrixIndex < source.Matrices.Length)
        {
            IccSparseMatrix matrix = source.Matrices[matrixIndex];
            ValidateSparseMatrix(matrix, element.OutputChannels, matrixIndex, index);
            if (matrixIndex == 0)
            {
                rows = matrix.Rows;
                columns = matrix.Columns;
            }
            else if (matrix.Rows != rows || matrix.Columns != columns)
            {
                throw new IccTransformException($"ICC.2 sparse-matrix element {index} matrices must have identical dimensions.");
            }

            ExpandSparseMatrix(matrix, values, matrixIndex * element.OutputChannels);
            matrixIndex = matrixIndex + 1;
        }

        IccClut clut = new IccClut();
        clut.InputChannels = element.InputChannels;
        clut.OutputChannels = element.OutputChannels;
        clut.GridPoints = CopyIntegers(source.GridPoints);
        clut.Values = values;
        IccInterpolationKind interpolation = requested == IccInterpolationKind.Linear && element.InputChannels != 1 ? IccInterpolationKind.Multilinear : requested;
        return CreateClutExecutionStage(clut, element.InputChannels, element.OutputChannels, interpolation, $"ICC.2 sparse-matrix element {index}");
    }

    internal static PipelineStage CreateToneMapStage(IccProcessingElement element, int index)
    {
        IccToneMap? toneMap = element.ToneMap;
        if (element.Signature != IccProcessingElementSignatures.ToneMap || element.InputChannels != element.OutputChannels + 1 || toneMap == null
            || toneMap.LuminanceCurve == null || toneMap.Functions == null || toneMap.Functions.Length != element.OutputChannels)
        {
            throw new IccTransformException($"ICC.2 tone-map element {index} has inconsistent channels, curve, or functions.");
        }

        MatrixTrcCompiler.ValidateCurve(toneMap.LuminanceCurve, $"ICC.2 tone-map element {index} luminance curve", false);
        int channel = 0;
        while (channel < toneMap.Functions.Length)
        {
            IccToneMapFunction function = toneMap.Functions[channel];
            if (function == null || function.FunctionType != 0 || function.Parameters == null || function.Parameters.Length != 3)
            {
                throw new IccTransformException($"ICC.2 tone-map element {index} function {channel} is not the standard three-parameter mapping function.");
            }
            ValidateFinite(function.Parameters, $"ICC.2 tone-map element {index} function {channel}");
            channel = channel + 1;
        }

        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.ToneMap;
        stage.InputChannels = element.InputChannels;
        stage.OutputChannels = element.OutputChannels;
        stage.ToneMap = ExtendedTagDataClone.CloneToneMap(toneMap, new ProfileCloneContext());
        stage.Description = $"ICC.2 tone-map element {index}";
        return stage;
    }

    internal static PipelineStage CreateSpectralStage(IccProfile profile, IccProcessingElement element, int index, IccInterpolationKind requested)
    {
        IccSpectralViewingConditions conditions = ResolveViewingConditions(profile);
        if (element.Kind == IccProcessingElementKind.EmissionObserver || element.Kind == IccProcessingElementKind.ReflectanceObserver)
        {
            IccMatrix observerMatrix = CreateObserverMatrix(element, conditions, index);
            return CreateMatrixExecutionStage(observerMatrix, element.InputChannels, 3, $"ICC.2 spectral observer element {index}");
        }
        if (element.Kind == IccProcessingElementKind.EmissionMatrix || element.Kind == IccProcessingElementKind.InverseEmissionMatrix)
        {
            IccMatrix matrix = CreateEmissionMatrix(element, conditions, index);
            return CreateMatrixExecutionStage(matrix, element.InputChannels, 3, $"ICC.2 spectral matrix element {index}");
        }
        if (element.Kind == IccProcessingElementKind.EmissionClut || element.Kind == IccProcessingElementKind.ReflectanceClut)
        {
            IccClut clut = CreateSpectralClut(element, conditions, index);
            IccInterpolationKind interpolation = ResolveInterpolation(clut, requested, index);
            return CreateClutExecutionStage(clut, element.InputChannels, 3, interpolation, $"ICC.2 spectral CLUT element {index}");
        }

        throw new IccTransformException($"ICC.2 spectral element {index} has an unsupported kind.");
    }

    internal static void ApplyToneMap(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        IccToneMap? toneMap = stage.ToneMap;
        if (toneMap == null)
        {
            throw new IccTransformException("A tone-map execution stage has no tone-map data.");
        }

        double mappedLuminance = CurveMath.EvaluateCurve(toneMap.LuminanceCurve, inputs[stage.InputChannels - 1]);
        int channel = 0;
        while (channel < stage.OutputChannels)
        {
            double[] parameters = toneMap.Functions[channel].Parameters;
            double value = parameters[0] * mappedLuminance * (inputs[channel] + parameters[1]) + parameters[2];
            if (!System.Double.IsFinite(value))
            {
                throw new IccTransformException($"Tone-map stage output {channel} is outside the finite double range.");
            }
            outputs[channel] = value;
            channel = channel + 1;
        }
    }

    internal static void ValidateToneMapStage(PipelineStage stage, int index)
    {
        IccToneMap? toneMap = stage.ToneMap;
        if (stage.InputChannels != stage.OutputChannels + 1 || toneMap == null || toneMap.Functions == null || toneMap.Functions.Length != stage.OutputChannels)
        {
            throw new IccTransformException($"Tone-map stage {index} has inconsistent channels or functions.");
        }
    }

    internal static IccClut RequireClut(IccProcessingElement element, int index, uint signature, string description)
    {
        IccClut? clut = element.Clut;
        if (element.Signature != signature || clut == null)
        {
            throw new IccTransformException($"ICC.2 {description} element {index} has inconsistent signature or table data.");
        }
        return clut;
    }

    internal static void ValidateClutShape(IccClut clut, int inputChannels, int outputChannels, int maximumInputs, int index, string description)
    {
        if (inputChannels < 1 || inputChannels > maximumInputs || clut.InputChannels != inputChannels || clut.OutputChannels != outputChannels
            || clut.GridPoints == null || clut.Values == null || clut.GridPoints.Length != inputChannels)
        {
            throw new IccTransformException($"ICC.2 {description} element {index} has inconsistent channels or table data.");
        }
        int points = ValidateGrid(clut.GridPoints, inputChannels, index, description);
        if (points > System.Int32.MaxValue / outputChannels || clut.Values.Length != points * outputChannels)
        {
            throw new IccTransformException($"ICC.2 {description} element {index} has an invalid table value count.");
        }
        ValidateFinite(clut.Values, $"ICC.2 {description} element {index}");
    }

    internal static int ValidateGrid(int[] grids, int inputChannels, int index, string description)
    {
        int points = 1;
        int dimension = 0;
        while (dimension < inputChannels)
        {
            int grid = grids[dimension];
            if (grid < 2 || grid > 255 || points > System.Int32.MaxValue / grid)
            {
                throw new IccTransformException($"ICC.2 {description} element {index} grid dimension {dimension} is invalid or overflows.");
            }
            points = points * grid;
            dimension = dimension + 1;
        }
        return points;
    }

    internal static IccInterpolationKind ResolveInterpolation(IccClut clut, IccInterpolationKind requested, int index)
    {
        IccInterpolationKind result = requested;
        if (clut.InterpolationHint == 1)
        {
            result = IccInterpolationKind.Multilinear;
        }
        else if (clut.InterpolationHint == 2)
        {
            result = IccInterpolationKind.Tetrahedral;
        }
        else if (clut.InterpolationHint != 0)
        {
            throw new IccTransformException($"ICC.2 CLUT element {index} has an unknown interpolation hint.");
        }
        if (result == IccInterpolationKind.Linear && clut.InputChannels != 1)
        {
            throw new IccTransformException($"ICC.2 CLUT element {index} can use linear interpolation only with one input.");
        }
        if (result == IccInterpolationKind.Tetrahedral && clut.InputChannels != 3)
        {
            result = IccInterpolationKind.Multilinear;
        }
        return result;
    }

    internal static PipelineStage CreateClutExecutionStage(IccClut source, int inputs, int outputs, IccInterpolationKind interpolation, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Clut;
        stage.InputChannels = inputs;
        stage.OutputChannels = outputs;
        stage.Clut = ExtendedTagDataClone.CloneClut(source);
        stage.Interpolation = interpolation;
        stage.Description = description;
        return stage;
    }

    internal static PipelineStage CreateMatrixExecutionStage(IccMatrix matrix, int inputs, int outputs, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Matrix;
        stage.InputChannels = inputs;
        stage.OutputChannels = outputs;
        stage.Matrix = matrix;
        stage.Description = description;
        return stage;
    }

    internal static void ValidateSparseMatrix(IccSparseMatrix matrix, int equivalentOutputs, int matrixIndex, int elementIndex)
    {
        if (matrix == null || matrix.Rows < 1 || matrix.Columns < 1 || matrix.Rows > System.Int32.MaxValue / matrix.Columns || matrix.Rows * matrix.Columns != equivalentOutputs
            || matrix.RowStartOffsets == null || matrix.ColumnIndices == null || matrix.Values == null || matrix.RowStartOffsets.Length != matrix.Rows + 1
            || matrix.ColumnIndices.Length != matrix.Values.Length || matrix.RowStartOffsets[0] != 0 || matrix.RowStartOffsets[matrix.Rows] != matrix.Values.Length)
        {
            throw new IccTransformException($"ICC.2 sparse-matrix element {elementIndex} matrix {matrixIndex} is malformed.");
        }
        int row = 0;
        while (row < matrix.Rows)
        {
            if (matrix.RowStartOffsets[row] > matrix.RowStartOffsets[row + 1])
            {
                throw new IccTransformException($"ICC.2 sparse-matrix element {elementIndex} matrix {matrixIndex} has unordered row offsets.");
            }
            int entry = matrix.RowStartOffsets[row];
            int previousColumn = -1;
            while (entry < matrix.RowStartOffsets[row + 1])
            {
                int column = matrix.ColumnIndices[entry];
                if (column <= previousColumn || column >= matrix.Columns || column < 0 || !System.Double.IsFinite(matrix.Values[entry]))
                {
                    throw new IccTransformException($"ICC.2 sparse-matrix element {elementIndex} matrix {matrixIndex} entry {entry} is invalid.");
                }
                previousColumn = column;
                entry = entry + 1;
            }
            row = row + 1;
        }
    }

    internal static void ExpandSparseMatrix(IccSparseMatrix matrix, double[] destination, int offset)
    {
        int row = 0;
        while (row < matrix.Rows)
        {
            int entry = matrix.RowStartOffsets[row];
            while (entry < matrix.RowStartOffsets[row + 1])
            {
                destination[offset + (row * matrix.Columns) + matrix.ColumnIndices[entry]] = matrix.Values[entry];
                entry = entry + 1;
            }
            row = row + 1;
        }
    }

    internal static IccSpectralViewingConditions ResolveViewingConditions(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == IccTagSignatures.SpectralViewingConditions && tag.Data != null && tag.Data.SpectralViewingConditions != null)
            {
                return tag.Data.SpectralViewingConditions;
            }
            index = index + 1;
        }
        throw new IccTransformException("ICC.2 spectral execution requires a spectral-viewing-conditions tag in the profile.");
    }

    internal static IccMatrix CreateObserverMatrix(IccProcessingElement element, IccSpectralViewingConditions conditions, int index)
    {
        uint signature = element.Kind == IccProcessingElementKind.EmissionObserver ? IccProcessingElementSignatures.EmissionObserver : IccProcessingElementSignatures.ReflectanceObserver;
        IccSpectralProcessingData spectral = RequireSpectralData(element, signature, index);
        int steps = spectral.White.Range.Steps;
        if (element.InputChannels != steps || element.OutputChannels != 3)
        {
            throw new IccTransformException($"ICC.2 spectral observer element {index} input channels must equal its spectral sample count.");
        }
        double[] coefficients = CreateSpectralCoefficients(element.Kind, spectral, conditions);
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = 3;
        matrix.Columns = steps;
        matrix.Values = coefficients;
        return matrix;
    }

    internal static IccMatrix CreateEmissionMatrix(IccProcessingElement element, IccSpectralViewingConditions conditions, int index)
    {
        uint signature = element.Kind == IccProcessingElementKind.EmissionMatrix ? IccProcessingElementSignatures.EmissionMatrix : IccProcessingElementSignatures.InverseEmissionMatrix;
        IccSpectralProcessingData spectral = RequireSpectralData(element, signature, index);
        IccMatrix? source = element.Matrix;
        int steps = spectral.White.Range.Steps;
        if (element.InputChannels != 3 || element.OutputChannels != 3 || source == null || source.Rows != 4 || source.Columns != steps || source.Values == null || source.Values.Length != 4 * steps)
        {
            throw new IccTransformException($"ICC.2 spectral matrix element {index} requires three inputs and four spectral rows.");
        }
        ValidateFinite(source.Values, $"ICC.2 spectral matrix element {index}");
        double[] coefficients = CreateEmissionCoefficients(spectral, conditions);
        double[] affine = new double[12];
        int xyz = 0;
        while (xyz < 3)
        {
            int spectrum = 0;
            while (spectrum < 4)
            {
                affine[(xyz * 4) + spectrum] = DotRows(coefficients, xyz, source.Values, spectrum, steps);
                spectrum = spectrum + 1;
            }
            xyz = xyz + 1;
        }
        if (element.Kind == IccProcessingElementKind.InverseEmissionMatrix)
        {
            affine = InvertAffineThreeByThree(affine);
        }
        IccMatrix result = new IccMatrix();
        result.Rows = 3;
        result.Columns = 4;
        result.Values = affine;
        return result;
    }

    internal static IccClut CreateSpectralClut(IccProcessingElement element, IccSpectralViewingConditions conditions, int index)
    {
        uint signature = element.Kind == IccProcessingElementKind.EmissionClut ? IccProcessingElementSignatures.EmissionClut : IccProcessingElementSignatures.ReflectanceClut;
        IccSpectralProcessingData spectral = RequireSpectralData(element, signature, index);
        IccClut? source = element.Clut;
        int steps = spectral.White.Range.Steps;
        if (source == null || source.InputChannels != element.InputChannels || source.OutputChannels != steps || source.GridPoints == null || source.Values == null)
        {
            throw new IccTransformException($"ICC.2 spectral CLUT element {index} has inconsistent spectral table data.");
        }
        int pointCount = ValidateGrid(source.GridPoints, element.InputChannels, index, "spectral CLUT");
        if (source.Values.Length != pointCount * steps)
        {
            throw new IccTransformException($"ICC.2 spectral CLUT element {index} has an invalid spectral value count.");
        }
        ValidateFinite(source.Values, $"ICC.2 spectral CLUT element {index}");
        double[] coefficients = CreateSpectralCoefficients(element.Kind, spectral, conditions);
        double[] values = new double[pointCount * 3];
        int point = 0;
        while (point < pointCount)
        {
            int xyz = 0;
            while (xyz < 3)
            {
                values[(point * 3) + xyz] = DotRows(coefficients, xyz, source.Values, point, steps);
                xyz = xyz + 1;
            }
            point = point + 1;
        }
        IccClut result = new IccClut();
        result.InputChannels = element.InputChannels;
        result.OutputChannels = 3;
        result.GridPoints = CopyIntegers(source.GridPoints);
        result.Values = values;
        result.InterpolationHint = source.InterpolationHint;
        result.Interpolation = source.Interpolation;
        result.Encoding = source.Encoding;
        return result;
    }

    internal static IccSpectralProcessingData RequireSpectralData(IccProcessingElement element, uint signature, int index)
    {
        IccSpectralProcessingData? spectral = element.SpectralProcessingData;
        if (element.Signature != signature || spectral == null || spectral.White == null || spectral.White.Range == null || spectral.White.Values == null
            || spectral.White.SpectrumCount != 1 || spectral.White.Range.Steps < 2 || spectral.White.Values.Length != spectral.White.Range.Steps || spectral.Flags > 1)
        {
            throw new IccTransformException($"ICC.2 spectral element {index} has invalid signature, white spectrum, or flags.");
        }
        ValidateFinite(spectral.White.Values, $"ICC.2 spectral element {index} white spectrum");
        return spectral;
    }

    internal static double[] CreateSpectralCoefficients(IccProcessingElementKind kind, IccSpectralProcessingData spectral, IccSpectralViewingConditions conditions)
    {
        if (kind == IccProcessingElementKind.EmissionObserver || kind == IccProcessingElementKind.EmissionClut || kind == IccProcessingElementKind.EmissionMatrix || kind == IccProcessingElementKind.InverseEmissionMatrix)
        {
            return CreateEmissionCoefficients(spectral, conditions);
        }
        IccSpectralData observer = SpectralMath.Resample(SpectralMath.ResolveObserver(conditions), spectral.White.Range);
        IccSpectralData illuminant = SpectralMath.Resample(SpectralMath.ResolveIlluminant(conditions), spectral.White.Range);
        int steps = spectral.White.Range.Steps;
        double denominator = DotSpectra(illuminant.Values, 0, observer.Values, steps, 1);
        if (denominator == 0.0 || !System.Double.IsFinite(denominator))
        {
            throw new IccTransformException("ICC.2 reflectance observer normalization is degenerate.");
        }
        double[] coefficients = new double[3 * steps];
        int xyz = 0;
        while (xyz < 3)
        {
            int sample = 0;
            while (sample < steps)
            {
                coefficients[(xyz * steps) + sample] = observer.Values[(xyz * steps) + sample] * illuminant.Values[sample] / denominator;
                sample = sample + 1;
            }
            xyz = xyz + 1;
        }
        if (spectral.Flags == 0)
        {
            double[] media = new double[3];
            double[] illuminantWhite = new double[3];
            xyz = 0;
            while (xyz < 3)
            {
                media[xyz] = DotRows(coefficients, xyz, spectral.White.Values, 0, steps);
                int sample = 0;
                while (sample < steps)
                {
                    illuminantWhite[xyz] = illuminantWhite[xyz] + coefficients[(xyz * steps) + sample];
                    sample = sample + 1;
                }
                if (media[xyz] == 0.0 || !System.Double.IsFinite(media[xyz]))
                {
                    throw new IccTransformException("ICC.2 relative reflectance white normalization is degenerate.");
                }
                double scale = illuminantWhite[xyz] / media[xyz];
                sample = 0;
                while (sample < steps)
                {
                    coefficients[(xyz * steps) + sample] = coefficients[(xyz * steps) + sample] * scale;
                    sample = sample + 1;
                }
                xyz = xyz + 1;
            }
        }
        return coefficients;
    }

    internal static double[] CreateEmissionCoefficients(IccSpectralProcessingData spectral, IccSpectralViewingConditions conditions)
    {
        IccSpectralData observer = SpectralMath.Resample(SpectralMath.ResolveObserver(conditions), spectral.White.Range);
        int steps = spectral.White.Range.Steps;
        double scale = 1.0;
        if (spectral.Flags == 0)
        {
            double denominator = DotSpectra(spectral.White.Values, 0, observer.Values, steps, 1);
            if (denominator == 0.0 || !System.Double.IsFinite(denominator))
            {
                throw new IccTransformException("ICC.2 emission observer white normalization is degenerate.");
            }
            scale = 1.0 / denominator;
        }
        double[] coefficients = new double[3 * steps];
        int index = 0;
        while (index < coefficients.Length)
        {
            coefficients[index] = observer.Values[index] * scale;
            index = index + 1;
        }
        return coefficients;
    }

    internal static double DotRows(double[] left, int leftRow, double[] right, int rightRow, int columns)
    {
        double sum = 0.0;
        int column = 0;
        while (column < columns)
        {
            sum = sum + (left[(leftRow * columns) + column] * right[(rightRow * columns) + column]);
            column = column + 1;
        }
        if (!System.Double.IsFinite(sum))
        {
            throw new IccTransformException("ICC.2 spectral conversion produced a non-finite value.");
        }
        return sum;
    }

    internal static double DotSpectra(double[] left, int leftRow, double[] right, int steps, int rightRow)
    {
        return DotRows(left, leftRow, right, rightRow, steps);
    }

    internal static double[] InvertAffineThreeByThree(double[] affine)
    {
        double[] matrix = new double[]
        {
            affine[0], affine[1], affine[2],
            affine[4], affine[5], affine[6],
            affine[8], affine[9], affine[10]
        };
        double[] inverse = MatrixMath.InvertMatrix3x3(matrix);
        double[] result = new double[12];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                result[(row * 4) + column] = inverse[(row * 3) + column];
                column = column + 1;
            }
            result[(row * 4) + 3] = -((inverse[(row * 3)] * affine[3]) + (inverse[(row * 3) + 1] * affine[7]) + (inverse[(row * 3) + 2] * affine[11]));
            row = row + 1;
        }
        return result;
    }

    internal static int[] CopyIntegers(int[] source)
    {
        int[] result = new int[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static void ValidateFinite(double[] values, string description)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} value {index} must be finite.");
            }
            index = index + 1;
        }
    }
}
