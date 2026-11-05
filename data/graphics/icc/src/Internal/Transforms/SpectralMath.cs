namespace icc;

internal static class SpectralMath
{
    internal const double LuminousEfficacy = 683.0;

    internal static double Sample(IccSpectralData spectrum, int spectrumIndex, double wavelength)
    {
        ValidateDenseSpectrum(spectrum, "spectral sampling");
        if (spectrumIndex < 0 || spectrumIndex >= spectrum.SpectrumCount)
        {
            throw new IccTransformException("The requested spectrum index is outside the spectral data.");
        }
        if (!System.Double.IsFinite(wavelength))
        {
            throw new IccTransformException("A sampling wavelength must be finite.");
        }

        IccSpectralRange range = spectrum.Range;
        int valueOffset = spectrumIndex * range.Steps;
        if (range.Steps == 1 || wavelength <= range.StartWavelength)
        {
            return spectrum.Values[valueOffset];
        }
        if (wavelength >= range.EndWavelength)
        {
            return spectrum.Values[valueOffset + range.Steps - 1];
        }

        double position = (wavelength - range.StartWavelength) * (range.Steps - 1) / (range.EndWavelength - range.StartWavelength);
        int lower = (int)System.Math.Floor(position);
        double fraction = position - lower;
        double first = spectrum.Values[valueOffset + lower];
        double second = spectrum.Values[valueOffset + lower + 1];
        return first + ((second - first) * fraction);
    }

    internal static IccSpectralData Resample(IccSpectralData spectrum, IccSpectralRange destinationRange)
    {
        ValidateDenseSpectrum(spectrum, "spectral resampling");
        ValidateRange(destinationRange, "destination spectral range");
        if (destinationRange.Steps > System.Array.MaxLength / spectrum.SpectrumCount)
        {
            throw new IccTransformException("The resampled spectral data exceeds the supported managed array size.");
        }

        IccSpectralData result = new IccSpectralData();
        result.Range = CopyRange(destinationRange);
        result.SpectrumCount = spectrum.SpectrumCount;
        result.Values = new double[destinationRange.Steps * spectrum.SpectrumCount];
        int spectrumIndex = 0;
        while (spectrumIndex < spectrum.SpectrumCount)
        {
            int index = 0;
            while (index < destinationRange.Steps)
            {
                double wavelength = GetWavelength(destinationRange, index);
                result.Values[(spectrumIndex * destinationRange.Steps) + index] = Sample(spectrum, spectrumIndex, wavelength);
                index = index + 1;
            }

            spectrumIndex = spectrumIndex + 1;
        }

        return result;
    }

    internal static double Integrate(IccSpectralData spectrum, int spectrumIndex)
    {
        ValidateDenseSpectrum(spectrum, "spectral integration");
        if (spectrumIndex < 0 || spectrumIndex >= spectrum.SpectrumCount)
        {
            throw new IccTransformException("The requested spectrum index is outside the spectral data.");
        }
        if (spectrum.Range.Steps == 1)
        {
            return 0.0;
        }

        int offset = spectrumIndex * spectrum.Range.Steps;
        double sum = (spectrum.Values[offset] + spectrum.Values[offset + spectrum.Range.Steps - 1]) * 0.5;
        double compensation = 0.0;
        int index = 1;
        while (index < spectrum.Range.Steps - 1)
        {
            double adjusted = spectrum.Values[offset + index] - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            index = index + 1;
        }

        double interval = (spectrum.Range.EndWavelength - spectrum.Range.StartWavelength) / (spectrum.Range.Steps - 1);
        double result = sum * interval;
        ValidateFiniteResult(result, "Spectral integration");
        return result;
    }

    internal static IccSpectralData ResolveIlluminant(IccSpectralViewingConditions conditions)
    {
        if (conditions == null)
        {
            throw new IccTransformException("Spectral viewing conditions cannot be null.");
        }
        if (conditions.IlluminantSpectrum != null)
        {
            ValidateDenseSpectrum(conditions.IlluminantSpectrum, "custom illuminant");
            if (conditions.IlluminantSpectrum.SpectrumCount != 1)
            {
                throw new IccTransformException("A custom illuminant must contain exactly one spectrum.");
            }

            return CopyDenseSpectrum(conditions.IlluminantSpectrum);
        }

        return SpectralStandardData.CreateIlluminant(conditions.Illuminant, conditions.ColorTemperature);
    }

    internal static IccSpectralData ResolveObserver(IccSpectralViewingConditions conditions)
    {
        if (conditions == null)
        {
            throw new IccTransformException("Spectral viewing conditions cannot be null.");
        }
        if (conditions.ObserverFunctions != null)
        {
            ValidateDenseSpectrum(conditions.ObserverFunctions, "custom observer");
            if (conditions.ObserverFunctions.SpectrumCount != 3)
            {
                throw new IccTransformException("A custom observer must contain exactly three colour-matching functions.");
            }

            return CopyDenseSpectrum(conditions.ObserverFunctions);
        }

        return SpectralStandardData.CreateObserver(conditions.Observer);
    }

    internal static IccSpectralData ApplyIlluminant(IccSpectralData reflectance, IccSpectralData illuminant)
    {
        ValidateSingleDenseSpectrum(reflectance, "reflectance or transmittance");
        ValidateSingleDenseSpectrum(illuminant, "illuminant");
        IccSpectralData adjusted = RangesMatch(reflectance.Range, illuminant.Range) ? CopyDenseSpectrum(reflectance) : Resample(reflectance, illuminant.Range);
        IccSpectralData result = CreateSingleSpectrum(illuminant.Range);
        int index = 0;
        while (index < result.Values.Length)
        {
            result.Values[index] = adjusted.Values[index] * illuminant.Values[index];
            ValidateFiniteResult(result.Values[index], "Illuminant application");
            index = index + 1;
        }

        return result;
    }

    internal static IccSpectralData RemoveIlluminant(IccSpectralData emission, IccSpectralData illuminant)
    {
        ValidateSingleDenseSpectrum(emission, "radiance or emission");
        ValidateSingleDenseSpectrum(illuminant, "illuminant");
        IccSpectralData adjusted = RangesMatch(emission.Range, illuminant.Range) ? CopyDenseSpectrum(emission) : Resample(emission, illuminant.Range);
        IccSpectralData result = CreateSingleSpectrum(illuminant.Range);
        int index = 0;
        while (index < result.Values.Length)
        {
            if (illuminant.Values[index] == 0.0)
            {
                throw new IccTransformException($"Reflectance or transmittance cannot be recovered because illuminant sample {index} is zero.");
            }

            result.Values[index] = adjusted.Values[index] / illuminant.Values[index];
            ValidateFiniteResult(result.Values[index], "Illuminant removal");
            index = index + 1;
        }

        return result;
    }

    internal static IccSpectralData ApplyFluorescence(IccSpectralData fluorescence, IccSpectralRange incidentRange, IccSpectralData illuminant)
    {
        if (fluorescence == null || fluorescence.Range == null || fluorescence.Values == null)
        {
            throw new IccTransformException("Fluorescent spectral data cannot be null.");
        }
        ValidateRange(fluorescence.Range, "fluorescent output range");
        ValidateRange(incidentRange, "fluorescent incident range");
        ValidateSingleDenseSpectrum(illuminant, "illuminant");
        IccSpectralData adjusted = RangesMatch(illuminant.Range, incidentRange) ? CopyDenseSpectrum(illuminant) : Resample(illuminant, incidentRange);
        IccSparseMatrix? sparse = fluorescence.SparseValues;
        double[] resultValues;
        if (sparse == null)
        {
            int valueCount = ValidateDenseFluorescence(fluorescence, incidentRange);
            resultValues = MultiplyDenseFluorescence(fluorescence.Values, fluorescence.Range.Steps, incidentRange.Steps, adjusted.Values, valueCount);
        }
        else
        {
            ValidateSparseFluorescence(fluorescence, incidentRange, sparse);
            resultValues = MultiplySparseFluorescence(sparse, adjusted.Values);
        }

        IccSpectralData result = CreateSingleSpectrum(fluorescence.Range);
        result.Values = resultValues;
        return result;
    }

    internal static IccXyzColor ObserveRelative(IccSpectralData emission, IccSpectralData observer, IccSpectralData illuminant)
    {
        ValidateSingleDenseSpectrum(emission, "relative emission");
        ValidateObserverSpectrum(observer);
        ValidateSingleDenseSpectrum(illuminant, "relative illuminant");
        IccSpectralData adjustedEmission = RangesMatch(emission.Range, observer.Range) ? CopyDenseSpectrum(emission) : Resample(emission, observer.Range);
        IccSpectralData adjustedIlluminant = RangesMatch(illuminant.Range, observer.Range) ? CopyDenseSpectrum(illuminant) : Resample(illuminant, observer.Range);
        double denominator = DotSpectrum(adjustedIlluminant, 0, observer, 1);
        if (denominator == 0.0 || !System.Double.IsFinite(denominator))
        {
            throw new IccTransformException("Relative observer normalization requires a finite non-zero illuminant Y response.");
        }

        IccXyzColor result = new IccXyzColor();
        result.X = DotSpectrum(adjustedEmission, 0, observer, 0) / denominator;
        result.Y = DotSpectrum(adjustedEmission, 0, observer, 1) / denominator;
        result.Z = DotSpectrum(adjustedEmission, 0, observer, 2) / denominator;
        result.IsAbsolute = false;
        ValidateXyzResult(result);
        return result;
    }

    internal static IccXyzColor ObserveAbsolute(IccSpectralData emission, IccSpectralData observer)
    {
        ValidateSingleDenseSpectrum(emission, "absolute emission");
        ValidateObserverSpectrum(observer);
        IccSpectralData adjustedEmission = RangesMatch(emission.Range, observer.Range) ? CopyDenseSpectrum(emission) : Resample(emission, observer.Range);
        double interval = observer.Range.Steps == 1 ? 1.0 : (observer.Range.EndWavelength - observer.Range.StartWavelength) / (observer.Range.Steps - 1);
        IccXyzColor result = new IccXyzColor();
        result.X = LuminousEfficacy * interval * DotSpectrum(adjustedEmission, 0, observer, 0);
        result.Y = LuminousEfficacy * interval * DotSpectrum(adjustedEmission, 0, observer, 1);
        result.Z = LuminousEfficacy * interval * DotSpectrum(adjustedEmission, 0, observer, 2);
        result.IsAbsolute = true;
        ValidateXyzResult(result);
        return result;
    }

    internal static IccXyzColor ConvertToPccXyz(uint spectralSignature, IccSpectralData spectralData, IccSpectralRange? incidentRange, IccProfileConnectionConditions pcc)
    {
        if (pcc == null || pcc.ViewingConditions == null)
        {
            throw new IccTransformException("PCC spectral conversion requires spectral viewing conditions.");
        }

        int channels = ColorSpaceChannelCount.Decode(spectralSignature);
        uint prefix = spectralSignature & 0xffff0000;
        if (channels <= 0)
        {
            throw new IccTransformException("A spectral PCS signature must declare at least one channel.");
        }

        IccSpectralData observer = ResolveObserver(pcc.ViewingConditions);
        if (prefix == IccColorSpaceSignatures.RadiantSpectralPrefix)
        {
            ValidateSignatureSampleCount(spectralData, channels, false);
            return ObserveAbsolute(spectralData, observer);
        }

        IccSpectralData illuminant = ResolveIlluminant(pcc.ViewingConditions);
        if (prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix || prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix)
        {
            ValidateSignatureSampleCount(spectralData, channels, false);
            IccSpectralData emission = ApplyIlluminant(spectralData, illuminant);
            return ObserveRelative(emission, observer, illuminant);
        }

        if (prefix == IccColorSpaceSignatures.BiSpectralPrefix || prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix)
        {
            if (incidentRange == null)
            {
                throw new IccTransformException("A fluorescent PCS conversion requires its incident spectral range.");
            }
            if (prefix == IccColorSpaceSignatures.BiSpectralPrefix)
            {
                ValidateSignatureSampleCount(spectralData, channels, true);
            }
            else if (spectralData.SparseValues == null || spectralData.SparseValues.Rows != channels)
            {
                throw new IccTransformException("A sparse fluorescent PCS channel count must match the matrix output row count.");
            }

            IccSpectralData emission = ApplyFluorescence(spectralData, incidentRange, illuminant);
            IccSpectralData outputIlluminant = RangesMatch(illuminant.Range, spectralData.Range) ? illuminant : Resample(illuminant, spectralData.Range);
            return ObserveRelative(emission, observer, outputIlluminant);
        }

        throw new IccTransformException("The signature does not identify a supported spectral PCS.");
    }

    internal static IccSpectralData CopyDenseSpectrum(IccSpectralData source)
    {
        ValidateDenseSpectrum(source, "spectral copy");
        IccSpectralData result = new IccSpectralData();
        result.Range = CopyRange(source.Range);
        result.SpectrumCount = source.SpectrumCount;
        result.Values = MatrixMath.CopyValues(source.Values);
        return result;
    }

    internal static IccSpectralRange CopyRange(IccSpectralRange source)
    {
        ValidateRange(source, "spectral range");
        IccSpectralRange result = new IccSpectralRange();
        result.StartWavelength = source.StartWavelength;
        result.EndWavelength = source.EndWavelength;
        result.Steps = source.Steps;
        return result;
    }

    internal static IccSpectralData CreateSingleSpectrum(IccSpectralRange range)
    {
        ValidateRange(range, "spectral range");
        IccSpectralData result = new IccSpectralData();
        result.Range = CopyRange(range);
        result.SpectrumCount = 1;
        result.Values = new double[range.Steps];
        return result;
    }

    internal static double GetWavelength(IccSpectralRange range, int index)
    {
        ValidateRange(range, "spectral range");
        if (index < 0 || index >= range.Steps)
        {
            throw new IccTransformException("The spectral sample index is outside the wavelength range.");
        }
        if (range.Steps == 1)
        {
            return range.StartWavelength;
        }

        return range.StartWavelength + ((range.EndWavelength - range.StartWavelength) * index / (range.Steps - 1));
    }

    internal static void ValidateRange(IccSpectralRange range, string description)
    {
        if (range == null || range.Steps < 1 || range.Steps > System.Array.MaxLength || !System.Double.IsFinite(range.StartWavelength) || !System.Double.IsFinite(range.EndWavelength)
            || (range.Steps == 1 && range.StartWavelength != range.EndWavelength)
            || (range.Steps > 1 && range.StartWavelength >= range.EndWavelength))
        {
            throw new IccTransformException($"The {description} is not a valid uniformly sampled wavelength range.");
        }
    }

    internal static void ValidateDenseSpectrum(IccSpectralData spectrum, string description)
    {
        if (spectrum == null || spectrum.Range == null || spectrum.Values == null || spectrum.SparseValues != null || spectrum.SpectrumCount < 1)
        {
            throw new IccTransformException($"The {description} requires dense spectral data.");
        }
        ValidateRange(spectrum.Range, description);
        if (spectrum.Range.Steps > System.Array.MaxLength / spectrum.SpectrumCount || spectrum.Values.Length != spectrum.Range.Steps * spectrum.SpectrumCount)
        {
            throw new IccTransformException($"The {description} sample count is inconsistent with its range and spectrum count.");
        }

        int index = 0;
        while (index < spectrum.Values.Length)
        {
            if (!System.Double.IsFinite(spectrum.Values[index]))
            {
                throw new IccTransformException($"The {description} sample {index} must be finite.");
            }
            index = index + 1;
        }
    }

    internal static void ValidateSingleDenseSpectrum(IccSpectralData spectrum, string description)
    {
        ValidateDenseSpectrum(spectrum, description);
        if (spectrum.SpectrumCount != 1)
        {
            throw new IccTransformException($"The {description} must contain exactly one spectrum.");
        }
    }

    internal static void ValidateObserverSpectrum(IccSpectralData observer)
    {
        ValidateDenseSpectrum(observer, "observer");
        if (observer.SpectrumCount != 3)
        {
            throw new IccTransformException("An observer must contain X, Y, and Z colour-matching functions.");
        }
    }

    internal static bool RangesMatch(IccSpectralRange first, IccSpectralRange second)
    {
        return first.Steps == second.Steps && first.StartWavelength == second.StartWavelength && first.EndWavelength == second.EndWavelength;
    }

    internal static double DotSpectrum(IccSpectralData first, int firstIndex, IccSpectralData second, int secondIndex)
    {
        if (!RangesMatch(first.Range, second.Range))
        {
            throw new IccTransformException("A spectral dot product requires matching wavelength ranges.");
        }

        int firstOffset = firstIndex * first.Range.Steps;
        int secondOffset = secondIndex * second.Range.Steps;
        double sum = 0.0;
        double compensation = 0.0;
        int index = 0;
        while (index < first.Range.Steps)
        {
            double product = first.Values[firstOffset + index] * second.Values[secondOffset + index];
            double adjusted = product - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            index = index + 1;
        }

        ValidateFiniteResult(sum, "Spectral integration");
        return sum;
    }

    internal static int ValidateDenseFluorescence(IccSpectralData fluorescence, IccSpectralRange incidentRange)
    {
        int rows = fluorescence.Range.Steps;
        int columns = incidentRange.Steps;
        if (rows > System.Array.MaxLength / columns || fluorescence.Values.Length != rows * columns)
        {
            throw new IccTransformException("A dense Donaldson matrix must contain one value for every output and incident wavelength pair.");
        }
        if (fluorescence.SpectrumCount != rows)
        {
            throw new IccTransformException("A dense Donaldson matrix spectrum count must match its output wavelength count.");
        }

        int index = 0;
        while (index < fluorescence.Values.Length)
        {
            if (!System.Double.IsFinite(fluorescence.Values[index]))
            {
                throw new IccTransformException($"Donaldson matrix sample {index} must be finite.");
            }
            index = index + 1;
        }
        return rows * columns;
    }

    internal static double[] MultiplyDenseFluorescence(double[] matrix, int rows, int columns, double[] illuminant, int valueCount)
    {
        if (matrix.Length != valueCount)
        {
            throw new IccTransformException("The dense Donaldson matrix size is inconsistent.");
        }
        return MatrixMath.ApplyMatrixToVector(matrix, rows, columns, illuminant);
    }

    internal static void ValidateSparseFluorescence(IccSpectralData fluorescence, IccSpectralRange incidentRange, IccSparseMatrix sparse)
    {
        if (fluorescence.Values.Length != 0 || fluorescence.SpectrumCount != sparse.Rows || sparse.Rows != fluorescence.Range.Steps || sparse.Columns != incidentRange.Steps
            || sparse.RowStartOffsets == null || sparse.ColumnIndices == null || sparse.Values == null || sparse.RowStartOffsets.Length != sparse.Rows + 1
            || sparse.ColumnIndices.Length != sparse.Values.Length || sparse.RowStartOffsets[0] != 0 || sparse.RowStartOffsets[sparse.Rows] != sparse.Values.Length)
        {
            throw new IccTransformException("The sparse Donaldson matrix dimensions or compressed-row arrays are inconsistent.");
        }

        int row = 0;
        while (row < sparse.Rows)
        {
            int start = sparse.RowStartOffsets[row];
            int end = sparse.RowStartOffsets[row + 1];
            if (start < 0 || start > end || end > sparse.Values.Length)
            {
                throw new IccTransformException($"Sparse Donaldson matrix row {row} has invalid entry offsets.");
            }
            int index = start;
            int previousColumn = -1;
            while (index < end)
            {
                if (sparse.ColumnIndices[index] <= previousColumn || sparse.ColumnIndices[index] >= sparse.Columns || !System.Double.IsFinite(sparse.Values[index]))
                {
                    throw new IccTransformException($"Sparse Donaldson matrix entry {index} is invalid.");
                }
                previousColumn = sparse.ColumnIndices[index];
                index = index + 1;
            }
            row = row + 1;
        }
    }

    internal static double[] MultiplySparseFluorescence(IccSparseMatrix sparse, double[] illuminant)
    {
        double[] result = new double[sparse.Rows];
        int row = 0;
        while (row < sparse.Rows)
        {
            double sum = 0.0;
            double compensation = 0.0;
            int index = sparse.RowStartOffsets[row];
            while (index < sparse.RowStartOffsets[row + 1])
            {
                double product = sparse.Values[index] * illuminant[sparse.ColumnIndices[index]];
                double adjusted = product - compensation;
                double next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                sum = next;
                index = index + 1;
            }
            ValidateFiniteResult(sum, "Sparse Donaldson matrix application");
            result[row] = sum;
            row = row + 1;
        }
        return result;
    }

    internal static void ValidateSignatureSampleCount(IccSpectralData spectrum, int channels, bool matrix)
    {
        if (spectrum == null || spectrum.Range == null || spectrum.Values == null)
        {
            throw new IccTransformException("Spectral PCS data cannot be null.");
        }
        int expected = matrix ? spectrum.Values.Length : spectrum.Range.Steps;
        if (expected != channels)
        {
            throw new IccTransformException($"The spectral PCS signature declares {channels} channels, but the data contains {expected} channel values.");
        }
    }

    internal static void ValidateXyzResult(IccXyzColor result)
    {
        if (!System.Double.IsFinite(result.X) || !System.Double.IsFinite(result.Y) || !System.Double.IsFinite(result.Z))
        {
            throw new IccTransformException("Spectral conversion produced non-finite XYZ colorimetry.");
        }
    }

    internal static void ValidateFiniteResult(double value, string description)
    {
        if (!System.Double.IsFinite(value))
        {
            throw new IccTransformException($"{description} produced a value outside the finite double range.");
        }
    }
}
