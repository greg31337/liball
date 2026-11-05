namespace icc;

internal static class SpectralStandardData
{
    internal static IccSpectralData CreateObserver(IccMeasurementObserver observer)
    {
        switch (observer)
        {
            case IccMeasurementObserver.Cie1931TwoDegree:
                return CreateInterleavedSpectrum(SpectralStandardTables.ObserverTwoDegree, 360.0, 830.0, 95, 3);
            case IccMeasurementObserver.Cie1964TenDegree:
                return CreateInterleavedSpectrum(SpectralStandardTables.ObserverTenDegree, 360.0, 830.0, 95, 3);
            default:
                throw new IccTransformException("A custom observer requires explicit colour-matching functions.");
        }
    }

    internal static IccSpectralData CreateIlluminant(IccStandardIlluminant illuminant, double colorTemperature)
    {
        switch (illuminant)
        {
            case IccStandardIlluminant.D50:
                return CreateSingleTableSpectrum(SpectralStandardIlluminants.IlluminantD50, 300.0, 830.0, 107);
            case IccStandardIlluminant.D65:
                return CreateSingleTableSpectrum(SpectralStandardIlluminants.IlluminantD65, 300.0, 830.0, 107);
            case IccStandardIlluminant.D93:
                return CreateDaylight(9305.0);
            case IccStandardIlluminant.F2:
                return CreateFluorescent(1);
            case IccStandardIlluminant.D55:
                return CreateDaylight(5503.0);
            case IccStandardIlluminant.A:
                return CreateSingleTableSpectrum(SpectralStandardIlluminants.IlluminantA, 300.0, 830.0, 107);
            case IccStandardIlluminant.EqualEnergyE:
                return CreateEqualEnergy();
            case IccStandardIlluminant.F8:
                return CreateFluorescent(7);
            case IccStandardIlluminant.BlackBody:
                return CreateBlackBody(colorTemperature);
            case IccStandardIlluminant.Daylight:
                return CreateDaylight(colorTemperature);
            case IccStandardIlluminant.B:
                return CreatePlanckian(4874.0, 14387768.77);
            case IccStandardIlluminant.C:
                return CreateSingleTableSpectrum(SpectralStandardTables.IlluminantC, 300.0, 780.0, 97);
            case IccStandardIlluminant.F1:
                return CreateFluorescent(0);
            case IccStandardIlluminant.F3:
                return CreateFluorescent(2);
            case IccStandardIlluminant.F4:
                return CreateFluorescent(3);
            case IccStandardIlluminant.F5:
                return CreateFluorescent(4);
            case IccStandardIlluminant.F6:
                return CreateFluorescent(5);
            case IccStandardIlluminant.F7:
                return CreateFluorescent(6);
            case IccStandardIlluminant.F9:
                return CreateFluorescent(8);
            case IccStandardIlluminant.F10:
                return CreateFluorescent(9);
            case IccStandardIlluminant.F11:
                return CreateFluorescent(10);
            case IccStandardIlluminant.F12:
                return CreateFluorescent(11);
            default:
                throw new IccTransformException("A custom illuminant requires an explicit spectral power distribution.");
        }
    }

    internal static IccSpectralData CreateInterleavedSpectrum(double[] values, double start, double end, int steps, int spectrumCount)
    {
        if (values == null || values.Length != steps * spectrumCount)
        {
            throw new IccTransformException("A standard spectral data table has inconsistent dimensions.");
        }

        IccSpectralData result = new IccSpectralData();
        result.Range.StartWavelength = start;
        result.Range.EndWavelength = end;
        result.Range.Steps = steps;
        result.SpectrumCount = spectrumCount;
        result.Values = new double[values.Length];
        int spectrum = 0;
        while (spectrum < spectrumCount)
        {
            int index = 0;
            while (index < steps)
            {
                result.Values[(spectrum * steps) + index] = values[(index * spectrumCount) + spectrum];
                index = index + 1;
            }
            spectrum = spectrum + 1;
        }
        return result;
    }

    internal static IccSpectralData CreateSingleTableSpectrum(double[] values, double start, double end, int steps)
    {
        if (values == null || values.Length != steps)
        {
            throw new IccTransformException("A standard illuminant table has inconsistent dimensions.");
        }
        IccSpectralData result = new IccSpectralData();
        result.Range.StartWavelength = start;
        result.Range.EndWavelength = end;
        result.Range.Steps = steps;
        result.SpectrumCount = 1;
        result.Values = MatrixMath.CopyValues(values);
        return result;
    }

    internal static IccSpectralData CreateDaylight(double colorTemperature)
    {
        if (!System.Double.IsFinite(colorTemperature) || colorTemperature < 4000.0 || colorTemperature > 25000.0)
        {
            throw new IccTransformException("A daylight illuminant colour temperature must be between 4000 K and 25000 K.");
        }

        double inverse = 1.0 / colorTemperature;
        double x;
        if (colorTemperature <= 7000.0)
        {
            x = (-4607000000.0 * inverse * inverse * inverse) + (2967800.0 * inverse * inverse) + (99.11 * inverse) + 0.244063;
        }
        else
        {
            x = (-2006400000.0 * inverse * inverse * inverse) + (1901800.0 * inverse * inverse) + (247.48 * inverse) + 0.237040;
        }
        double y = (-3.0 * x * x) + (2.87 * x) - 0.275;
        double denominator = 0.0241 + (0.2562 * x) - (0.7341 * y);
        double firstWeight = (-1.3515 - (1.7703 * x) + (5.9114 * y)) / denominator;
        double secondWeight = (0.03 - (31.4424 * x) + (30.0717 * y)) / denominator;
        int steps = 107;
        IccSpectralData result = CreateEmptyIlluminant(300.0, 830.0, steps);
        int index = 0;
        while (index < steps)
        {
            int offset = index * 3;
            result.Values[index] = SpectralStandardTables.DaylightComponents[offset]
                + (firstWeight * SpectralStandardTables.DaylightComponents[offset + 1])
                + (secondWeight * SpectralStandardTables.DaylightComponents[offset + 2]);
            if (!System.Double.IsFinite(result.Values[index]))
            {
                throw new IccTransformException("Daylight illuminant generation produced a non-finite sample.");
            }
            index = index + 1;
        }
        return result;
    }

    internal static IccSpectralData CreateBlackBody(double colorTemperature)
    {
        if (!System.Double.IsFinite(colorTemperature) || colorTemperature <= 0.0)
        {
            throw new IccTransformException("A black-body illuminant requires a positive finite colour temperature.");
        }
        return CreatePlanckian(colorTemperature, 14387768.77);
    }

    internal static IccSpectralData CreatePlanckian(double colorTemperature, double secondRadiationConstant)
    {
        IccSpectralData result = CreateEmptyIlluminant(300.0, 830.0, 107);
        double referenceExponent = secondRadiationConstant / (colorTemperature * 560.0);
        double reference = System.Math.Exp(referenceExponent) - 1.0;
        int index = 0;
        while (index < result.Range.Steps)
        {
            double wavelength = 300.0 + (5.0 * index);
            double ratio = 560.0 / wavelength;
            double denominator = System.Math.Exp(secondRadiationConstant / (colorTemperature * wavelength)) - 1.0;
            result.Values[index] = 100.0 * ratio * ratio * ratio * ratio * ratio * reference / denominator;
            if (!System.Double.IsFinite(result.Values[index]))
            {
                throw new IccTransformException("Planckian illuminant generation produced a non-finite sample.");
            }
            index = index + 1;
        }
        return result;
    }

    internal static IccSpectralData CreateEqualEnergy()
    {
        IccSpectralData result = CreateEmptyIlluminant(300.0, 830.0, 107);
        int index = 0;
        while (index < result.Values.Length)
        {
            result.Values[index] = 100.0;
            index = index + 1;
        }
        return result;
    }

    internal static IccSpectralData CreateFluorescent(int lampIndex)
    {
        int steps = 81;
        if (lampIndex < 0 || lampIndex >= 12 || SpectralStandardTables.FluorescentIlluminants.Length != steps * 12)
        {
            throw new IccTransformException("The standard fluorescent illuminant table is inconsistent.");
        }
        IccSpectralData result = CreateEmptyIlluminant(380.0, 780.0, steps);
        int index = 0;
        while (index < steps)
        {
            result.Values[index] = SpectralStandardTables.FluorescentIlluminants[(index * 12) + lampIndex];
            index = index + 1;
        }
        return result;
    }

    internal static IccSpectralData CreateEmptyIlluminant(double start, double end, int steps)
    {
        IccSpectralData result = new IccSpectralData();
        result.Range.StartWavelength = start;
        result.Range.EndWavelength = end;
        result.Range.Steps = steps;
        result.SpectrumCount = 1;
        result.Values = new double[steps];
        return result;
    }
}
