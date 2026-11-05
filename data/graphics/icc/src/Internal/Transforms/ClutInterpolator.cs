namespace icc;

internal static class ClutInterpolator
{
    internal static void InterpolateClut(IccClut clut, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> indexScratch, System.Span<double> weightScratch, IccInterpolationKind interpolation)
    {
        if (clut == null)
        {
            throw new IccProfileValidationException("A CLUT cannot be null.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccProfileValidationException("The requested CLUT interpolation algorithm is not defined.");
        }
        if (interpolation == IccInterpolationKind.Linear && clut.InputChannels != 1)
        {
            throw new IccProfileValidationException("Linear CLUT interpolation requires exactly one input channel.");
        }

        if (interpolation == IccInterpolationKind.Tetrahedral && clut.InputChannels == 3)
        {
            InterpolateTetrahedral(clut, inputs, outputs, indexScratch, weightScratch);
            return;
        }

        InterpolateMultilinear(clut, inputs, outputs, indexScratch, weightScratch);
    }

    internal static int GetMultilinearIndexScratchLength(int inputChannels)
    {
        if (inputChannels < 1 || inputChannels > 16)
        {
            throw new IccProfileValidationException("Multilinear CLUT interpolation requires between 1 and 16 input channels.");
        }

        return inputChannels * 2;
    }

    internal static int GetMultilinearWeightScratchLength(int inputChannels)
    {
        if (inputChannels < 1 || inputChannels > 16)
        {
            throw new IccProfileValidationException("Multilinear CLUT interpolation requires between 1 and 16 input channels.");
        }

        return inputChannels;
    }

    internal static void InterpolateTetrahedral(IccClut clut, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> indexScratch, System.Span<double> weightScratch)
    {
        if (clut == null || clut.InputChannels != 3)
        {
            throw new IccProfileValidationException("Tetrahedral CLUT interpolation requires exactly three input channels.");
        }

        int baseValueOffset = PrepareClutInterpolation(clut, inputs, outputs, indexScratch, weightScratch);
        int firstStride = indexScratch[3];
        int secondStride = indexScratch[4];
        int thirdStride = indexScratch[5];
        double firstFraction = weightScratch[0];
        double secondFraction = weightScratch[1];
        double thirdFraction = weightScratch[2];
        int offsetZero = baseValueOffset;
        int offsetOne;
        int offsetTwo;
        int offsetThree = baseValueOffset + firstStride + secondStride + thirdStride;
        double weightZero;
        double weightOne;
        double weightTwo;
        double weightThree;

        if (firstFraction >= secondFraction)
        {
            if (secondFraction >= thirdFraction)
            {
                offsetOne = baseValueOffset + firstStride;
                offsetTwo = offsetOne + secondStride;
                weightZero = 1.0 - firstFraction;
                weightOne = firstFraction - secondFraction;
                weightTwo = secondFraction - thirdFraction;
                weightThree = thirdFraction;
            }
            else if (firstFraction >= thirdFraction)
            {
                offsetOne = baseValueOffset + firstStride;
                offsetTwo = offsetOne + thirdStride;
                weightZero = 1.0 - firstFraction;
                weightOne = firstFraction - thirdFraction;
                weightTwo = thirdFraction - secondFraction;
                weightThree = secondFraction;
            }
            else
            {
                offsetOne = baseValueOffset + thirdStride;
                offsetTwo = offsetOne + firstStride;
                weightZero = 1.0 - thirdFraction;
                weightOne = thirdFraction - firstFraction;
                weightTwo = firstFraction - secondFraction;
                weightThree = secondFraction;
            }
        }
        else
        {
            if (firstFraction >= thirdFraction)
            {
                offsetOne = baseValueOffset + secondStride;
                offsetTwo = offsetOne + firstStride;
                weightZero = 1.0 - secondFraction;
                weightOne = secondFraction - firstFraction;
                weightTwo = firstFraction - thirdFraction;
                weightThree = thirdFraction;
            }
            else if (secondFraction >= thirdFraction)
            {
                offsetOne = baseValueOffset + secondStride;
                offsetTwo = offsetOne + thirdStride;
                weightZero = 1.0 - secondFraction;
                weightOne = secondFraction - thirdFraction;
                weightTwo = thirdFraction - firstFraction;
                weightThree = firstFraction;
            }
            else
            {
                offsetOne = baseValueOffset + thirdStride;
                offsetTwo = offsetOne + secondStride;
                weightZero = 1.0 - thirdFraction;
                weightOne = thirdFraction - secondFraction;
                weightTwo = secondFraction - firstFraction;
                weightThree = firstFraction;
            }
        }

        int output = 0;
        while (output < clut.OutputChannels)
        {
            double remainingWeightZero = weightZero;
            double remainingWeightOne = weightOne;
            double remainingWeightTwo = weightTwo;
            double remainingWeightThree = weightThree;
            int firstOffset;
            double firstWeight;
            if (remainingWeightZero != 0.0)
            {
                firstOffset = offsetZero;
                firstWeight = remainingWeightZero;
                remainingWeightZero = 0.0;
            }
            else if (remainingWeightOne != 0.0)
            {
                firstOffset = offsetOne;
                firstWeight = remainingWeightOne;
                remainingWeightOne = 0.0;
            }
            else if (remainingWeightTwo != 0.0)
            {
                firstOffset = offsetTwo;
                firstWeight = remainingWeightTwo;
                remainingWeightTwo = 0.0;
            }
            else
            {
                firstOffset = offsetThree;
                firstWeight = remainingWeightThree;
                remainingWeightThree = 0.0;
            }

            double result = GetTetrahedralContribution(clut, firstOffset, output, firstWeight);
            if (remainingWeightZero != 0.0)
            {
                result = AddTetrahedralContribution(result, GetTetrahedralContribution(clut, offsetZero, output, remainingWeightZero), output);
            }
            if (remainingWeightOne != 0.0)
            {
                result = AddTetrahedralContribution(result, GetTetrahedralContribution(clut, offsetOne, output, remainingWeightOne), output);
            }
            if (remainingWeightTwo != 0.0)
            {
                result = AddTetrahedralContribution(result, GetTetrahedralContribution(clut, offsetTwo, output, remainingWeightTwo), output);
            }
            if (remainingWeightThree != 0.0)
            {
                result = AddTetrahedralContribution(result, GetTetrahedralContribution(clut, offsetThree, output, remainingWeightThree), output);
            }

            outputs[output] = result;
            output = output + 1;
        }
    }

    internal static void InterpolateMultilinear(IccClut clut, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> indexScratch, System.Span<double> weightScratch)
    {
        int baseValueOffset = PrepareClutInterpolation(clut, inputs, outputs, indexScratch, weightScratch);
        int inputChannels = clut.InputChannels;
        int outputChannels = clut.OutputChannels;

        int output = 0;
        while (output < outputChannels)
        {
            outputs[output] = 0.0;
            output = output + 1;
        }

        int cornerCount = 1 << inputChannels;
        int corner = 0;
        int dimension;
        bool hasContribution = false;
        while (corner < cornerCount)
        {
            int valueOffset = baseValueOffset;
            double weight = 1.0;
            dimension = 0;
            while (dimension < inputChannels)
            {
                if ((corner & (1 << dimension)) == 0)
                {
                    weight = weight * (1.0 - weightScratch[dimension]);
                }
                else
                {
                    weight = weight * weightScratch[dimension];
                    valueOffset = valueOffset + indexScratch[inputChannels + dimension];
                }

                dimension = dimension + 1;
            }

            if (weight != 0.0)
            {
                output = 0;
                while (output < outputChannels)
                {
                    double value = clut.Values[valueOffset + output];
                    if (!System.Double.IsFinite(value))
                    {
                        throw new IccProfileValidationException($"Multilinear CLUT value {valueOffset + output} must be finite.");
                    }

                    double contribution = value * weight;
                    double result;
                    if (!hasContribution)
                    {
                        result = weight == 1.0 ? value : contribution;
                    }
                    else
                    {
                        result = outputs[output] + contribution;
                    }
                    if (!System.Double.IsFinite(result))
                    {
                        throw new IccProfileValidationException($"Multilinear CLUT output {output} is outside the finite double range.");
                    }

                    outputs[output] = result;
                    output = output + 1;
                }

                hasContribution = true;
            }

            corner = corner + 1;
        }
    }

    internal static int PrepareClutInterpolation(IccClut clut, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> indexScratch, System.Span<double> weightScratch)
    {
        if (clut == null || clut.GridPoints == null || clut.Values == null)
        {
            throw new IccProfileValidationException("A CLUT and its grid data cannot be null.");
        }

        int inputChannels = clut.InputChannels;
        int outputChannels = clut.OutputChannels;
        int requiredIndexScratchLength = GetMultilinearIndexScratchLength(inputChannels);
        int requiredWeightScratchLength = GetMultilinearWeightScratchLength(inputChannels);
        if (outputChannels < 1)
        {
            throw new IccProfileValidationException("CLUT interpolation requires at least one output channel.");
        }
        if (clut.GridPoints.Length != inputChannels)
        {
            throw new IccProfileValidationException("A CLUT requires exactly one grid-point count for each input channel.");
        }
        if (inputs.Length != inputChannels)
        {
            throw new IccProfileValidationException("A CLUT requires exactly one input value for each input channel.");
        }
        if (outputs.Length < outputChannels)
        {
            throw new IccProfileValidationException("A CLUT output span is too short for the table output channels.");
        }
        if (indexScratch.Length < requiredIndexScratchLength || weightScratch.Length < requiredWeightScratchLength)
        {
            throw new IccProfileValidationException("The supplied CLUT interpolation scratch spans are too short.");
        }

        int valueCount = outputChannels;
        int dimension = inputChannels - 1;
        while (dimension >= 0)
        {
            int gridPointCount = clut.GridPoints[dimension];
            if (gridPointCount < 2 || gridPointCount > 255)
            {
                throw new IccProfileValidationException($"CLUT grid dimension {dimension} must contain between 2 and 255 points.");
            }
            if (valueCount > System.Int32.MaxValue / gridPointCount)
            {
                throw new IccProfileValidationException("The CLUT value count exceeds the supported array size.");
            }

            indexScratch[inputChannels + dimension] = valueCount;
            valueCount = valueCount * gridPointCount;
            dimension = dimension - 1;
        }
        if (clut.Values.Length != valueCount)
        {
            throw new IccProfileValidationException($"The CLUT requires exactly {valueCount} values, but it contains {clut.Values.Length}.");
        }

        int baseValueOffset = 0;
        dimension = 0;
        while (dimension < inputChannels)
        {
            double input = inputs[dimension];
            if (!System.Double.IsFinite(input))
            {
                throw new IccProfileValidationException($"CLUT input {dimension} must be finite.");
            }

            int gridMaximum = clut.GridPoints[dimension] - 1;
            int lowerIndex;
            double fraction;
            if (input <= 0.0)
            {
                lowerIndex = 0;
                fraction = 0.0;
            }
            else if (input >= 1.0)
            {
                lowerIndex = gridMaximum - 1;
                fraction = 1.0;
            }
            else
            {
                double position = input * gridMaximum;
                lowerIndex = (int)System.Math.Floor(position);
                if (lowerIndex >= gridMaximum)
                {
                    lowerIndex = gridMaximum - 1;
                    fraction = 1.0;
                }
                else
                {
                    fraction = position - lowerIndex;
                }
            }

            indexScratch[dimension] = lowerIndex;
            weightScratch[dimension] = fraction;
            baseValueOffset = baseValueOffset + (lowerIndex * indexScratch[inputChannels + dimension]);
            dimension = dimension + 1;
        }

        return baseValueOffset;
    }

    internal static double GetTetrahedralContribution(IccClut clut, int valueOffset, int output, double weight)
    {
        double value = clut.Values[valueOffset + output];
        if (!System.Double.IsFinite(value))
        {
            throw new IccProfileValidationException($"Tetrahedral CLUT value {valueOffset + output} must be finite.");
        }

        double contribution = weight == 1.0 ? value : value * weight;
        if (!System.Double.IsFinite(contribution))
        {
            throw new IccProfileValidationException($"Tetrahedral CLUT output {output} is outside the finite double range.");
        }

        return contribution;
    }

    internal static double AddTetrahedralContribution(double current, double contribution, int output)
    {
        double result = current + contribution;
        if (!System.Double.IsFinite(result))
        {
            throw new IccProfileValidationException($"Tetrahedral CLUT output {output} is outside the finite double range.");
        }

        return result;
    }

    internal static double InterpolateOneDimensional(double[] samples, double input, double minimum, double maximum, bool extendLinearly)
    {
        CurveMath.ValidateSamples(samples, 2, "one-dimensional interpolation table");
        if (!System.Double.IsFinite(input))
        {
            throw new IccProfileValidationException("A one-dimensional interpolation input must be finite.");
        }
        if (!System.Double.IsFinite(minimum) || !System.Double.IsFinite(maximum) || minimum >= maximum)
        {
            throw new IccProfileValidationException("A one-dimensional interpolation domain must have finite, strictly increasing endpoints.");
        }

        if (input == minimum)
        {
            return samples[0];
        }
        if (input == maximum)
        {
            return samples[samples.Length - 1];
        }
        if (input < minimum && !extendLinearly)
        {
            return samples[0];
        }
        if (input > maximum && !extendLinearly)
        {
            return samples[samples.Length - 1];
        }

        if (input < minimum)
        {
            return ExtendBelowDomain(samples, input, minimum, maximum);
        }
        if (input > maximum)
        {
            return ExtendAboveDomain(samples, input, minimum, maximum);
        }

        double normalized = CalculateNormalizedPosition(input, minimum, maximum);
        double position = normalized * (samples.Length - 1);
        int lowerIndex = (int)System.Math.Floor(position);
        if (lowerIndex < 0)
        {
            lowerIndex = 0;
        }
        if (lowerIndex >= samples.Length - 1)
        {
            return samples[samples.Length - 1];
        }

        double fraction = position - lowerIndex;
        if (fraction == 0.0)
        {
            return samples[lowerIndex];
        }

        return InterpolateFiniteValues(samples[lowerIndex], samples[lowerIndex + 1], fraction);
    }

    internal static double[] ApplyLutShapers(double[][] tables, double[] inputs, bool extendLinearly)
    {
        if (tables == null || inputs == null)
        {
            throw new IccProfileValidationException("LUT shaper tables and inputs cannot be null.");
        }
        if (tables.Length != inputs.Length)
        {
            throw new IccProfileValidationException("A LUT shaper requires exactly one table for each input channel.");
        }
        if (tables.Length == 0)
        {
            throw new IccProfileValidationException("A LUT shaper requires at least one input channel.");
        }

        double[] outputs = new double[inputs.Length];
        int index = 0;
        while (index < inputs.Length)
        {
            if (tables[index] == null)
            {
                throw new IccProfileValidationException($"LUT shaper table {index} cannot be null.");
            }

            outputs[index] = InterpolateOneDimensional(tables[index], inputs[index], 0.0, 1.0, extendLinearly);
            index = index + 1;
        }

        return outputs;
    }

    internal static double ExtendBelowDomain(double[] samples, double input, double minimum, double maximum)
    {
        double difference = samples[1] - samples[0];
        if (difference == 0.0)
        {
            return samples[0];
        }
        if (!System.Double.IsFinite(difference))
        {
            throw new IccProfileValidationException("Lower linear interpolation extension has a non-finite endpoint difference.");
        }

        double normalized = CalculateNormalizedPosition(input, minimum, maximum);
        double offset = difference * normalized;
        offset = offset * (samples.Length - 1);
        double output = samples[0] + offset;
        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException("Lower linear interpolation extension produced a non-finite value.");
        }

        return output;
    }

    internal static double ExtendAboveDomain(double[] samples, double input, double minimum, double maximum)
    {
        int last = samples.Length - 1;
        double difference = samples[last] - samples[last - 1];
        if (difference == 0.0)
        {
            return samples[last];
        }
        if (!System.Double.IsFinite(difference))
        {
            throw new IccProfileValidationException("Upper linear interpolation extension has a non-finite endpoint difference.");
        }

        double normalized = CalculateNormalizedPosition(input, minimum, maximum) - 1.0;
        double offset = difference * normalized;
        offset = offset * (samples.Length - 1);
        double output = samples[last] + offset;
        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException("Upper linear interpolation extension produced a non-finite value.");
        }

        return output;
    }

    internal static double CalculateNormalizedPosition(double input, double minimum, double maximum)
    {
        double span = maximum - minimum;
        double offset = input - minimum;
        double result;
        if (System.Double.IsFinite(span) && System.Double.IsFinite(offset))
        {
            result = offset / span;
        }
        else
        {
            double scale = System.Math.Max(System.Math.Abs(input), System.Math.Max(System.Math.Abs(minimum), System.Math.Abs(maximum)));
            double scaledMinimum = minimum / scale;
            double scaledSpan = (maximum / scale) - scaledMinimum;
            result = ((input / scale) - scaledMinimum) / scaledSpan;
        }

        if (!System.Double.IsFinite(result))
        {
            throw new IccProfileValidationException("A one-dimensional interpolation position is outside the finite double range.");
        }

        return result;
    }

    internal static double InterpolateFiniteValues(double lower, double upper, double fraction)
    {
        if (fraction == 0.0)
        {
            return lower;
        }
        if (fraction == 1.0)
        {
            return upper;
        }

        double output = (lower * (1.0 - fraction)) + (upper * fraction);
        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException("One-dimensional interpolation produced a non-finite value.");
        }

        return output;
    }
}
