using icc;

namespace icc.Tests;

public class SpectralMathTests
{
    [Test]
    public void SamplingInterpolatesAndExtendsNearestEndpointForEverySpectrum()
    {
        IccSpectralData spectrum = Dense(400.0, 700.0, 4, 2, new double[]
        {
            1.0, 3.0, 5.0, 7.0,
            10.0, 20.0, 30.0, 40.0
        });

        Assert.Multiple(() =>
        {
            Assert.That(SpectralMath.Sample(spectrum, 0, 350.0), Is.EqualTo(1.0));
            Assert.That(SpectralMath.Sample(spectrum, 0, 450.0), Is.EqualTo(2.0).Within(1e-12));
            Assert.That(SpectralMath.Sample(spectrum, 0, 700.0), Is.EqualTo(7.0));
            Assert.That(SpectralMath.Sample(spectrum, 1, 750.0), Is.EqualTo(40.0));
        });
    }

    [Test]
    public void ResamplingUsesUniformDestinationGridAndDoesNotAliasInput()
    {
        IccSpectralData source = Dense(400.0, 700.0, 4, 1, new double[] { 1.0, 2.0, 3.0, 4.0 });
        IccSpectralRange destination = Range(350.0, 750.0, 9);

        IccSpectralData result = SpectralMath.Resample(source, destination);
        source.Values[0] = 99.0;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Range, Is.Not.SameAs(destination));
            Assert.That(result.Values, Is.EqualTo(new double[] { 1.0, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.0 }).Within(1e-12));
        });
    }

    [Test]
    public void IntegrationUsesWavelengthSpacingAndTrapezoidalEndpoints()
    {
        IccSpectralData triangle = Dense(400.0, 600.0, 3, 1, new double[] { 0.0, 2.0, 0.0 });

        Assert.That(SpectralMath.Integrate(triangle, 0), Is.EqualTo(200.0).Within(1e-12));
    }

    [Test]
    public void ExplicitViewingDataTakesPrecedenceAndIsCopied()
    {
        IccSpectralViewingConditions conditions = CustomConditions();
        conditions.Illuminant = IccStandardIlluminant.D65;
        conditions.Observer = IccMeasurementObserver.Cie1964TenDegree;

        IccSpectralData illuminant = SpectralMath.ResolveIlluminant(conditions);
        IccSpectralData observer = SpectralMath.ResolveObserver(conditions);
        conditions.IlluminantSpectrum!.Values[0] = 99.0;
        conditions.ObserverFunctions!.Values[0] = 99.0;

        Assert.Multiple(() =>
        {
            Assert.That(illuminant.Values[0], Is.EqualTo(2.0));
            Assert.That(observer.Values[0], Is.EqualTo(1.0));
            Assert.That(illuminant.Values, Is.Not.SameAs(conditions.IlluminantSpectrum.Values));
            Assert.That(observer.Values, Is.Not.SameAs(conditions.ObserverFunctions.Values));
        });
    }

    [TestCase(IccStandardIlluminant.D50)]
    [TestCase(IccStandardIlluminant.D65)]
    [TestCase(IccStandardIlluminant.D93)]
    [TestCase(IccStandardIlluminant.F2)]
    [TestCase(IccStandardIlluminant.D55)]
    [TestCase(IccStandardIlluminant.A)]
    [TestCase(IccStandardIlluminant.EqualEnergyE)]
    [TestCase(IccStandardIlluminant.F8)]
    [TestCase(IccStandardIlluminant.BlackBody)]
    [TestCase(IccStandardIlluminant.Daylight)]
    [TestCase(IccStandardIlluminant.B)]
    [TestCase(IccStandardIlluminant.C)]
    [TestCase(IccStandardIlluminant.F1)]
    [TestCase(IccStandardIlluminant.F3)]
    [TestCase(IccStandardIlluminant.F4)]
    [TestCase(IccStandardIlluminant.F5)]
    [TestCase(IccStandardIlluminant.F6)]
    [TestCase(IccStandardIlluminant.F7)]
    [TestCase(IccStandardIlluminant.F9)]
    [TestCase(IccStandardIlluminant.F10)]
    [TestCase(IccStandardIlluminant.F11)]
    [TestCase(IccStandardIlluminant.F12)]
    public void EveryStandardIlluminantSelectorResolvesFiniteSamples(IccStandardIlluminant selector)
    {
        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions
        {
            Illuminant = selector,
            ColorTemperature = selector == IccStandardIlluminant.BlackBody || selector == IccStandardIlluminant.Daylight ? 6500.0 : 0.0
        };

        IccSpectralData result = SpectralMath.ResolveIlluminant(conditions);

        Assert.Multiple(() =>
        {
            Assert.That(result.SpectrumCount, Is.EqualTo(1));
            Assert.That(result.Values, Has.Length.EqualTo(result.Range.Steps));
            Assert.That(result.Values.All(double.IsFinite), Is.True);
            Assert.That(result.Values, Has.All.GreaterThanOrEqualTo(0.0));
        });
    }

    [Test]
    public void StandardObserversResolvePublishedColorMatchingFunctions()
    {
        IccSpectralData twoDegree = SpectralMath.ResolveObserver(new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Cie1931TwoDegree
        });
        IccSpectralData tenDegree = SpectralMath.ResolveObserver(new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Cie1964TenDegree
        });

        Assert.Multiple(() =>
        {
            Assert.That(twoDegree.SpectrumCount, Is.EqualTo(3));
            Assert.That(twoDegree.Range.StartWavelength, Is.EqualTo(360.0));
            Assert.That(SpectralMath.Sample(twoDegree, 1, 555.0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SpectralMath.Sample(twoDegree, 2, 650.0), Is.Zero);
            Assert.That(tenDegree.SpectrumCount, Is.EqualTo(3));
            Assert.That(SpectralMath.Sample(tenDegree, 1, 560.0), Is.EqualTo(0.99734).Within(1e-12));
            Assert.That(SpectralMath.Sample(tenDegree, 2, 700.0), Is.Zero);
        });
    }

    [Test]
    public void IlluminantApplicationAndRemovalResampleAndRoundTrip()
    {
        IccSpectralData reflectance = Dense(400.0, 600.0, 3, 1, new double[] { 0.25, 0.5, 0.75 });
        IccSpectralData illuminant = Dense(400.0, 600.0, 2, 1, new double[] { 2.0, 4.0 });

        IccSpectralData emission = SpectralMath.ApplyIlluminant(reflectance, illuminant);
        IccSpectralData recovered = SpectralMath.RemoveIlluminant(emission, illuminant);

        Assert.Multiple(() =>
        {
            Assert.That(emission.Values, Is.EqualTo(new double[] { 0.5, 3.0 }).Within(1e-12));
            Assert.That(recovered.Values, Is.EqualTo(new double[] { 0.25, 0.75 }).Within(1e-12));
        });
    }

    [Test]
    public void RelativeAndAbsoluteObserversUseTheirDefinedNormalization()
    {
        IccSpectralViewingConditions conditions = CustomConditions();
        IccSpectralData illuminant = SpectralMath.ResolveIlluminant(conditions);
        IccSpectralData observer = SpectralMath.ResolveObserver(conditions);
        IccSpectralData emission = Dense(400.0, 500.0, 2, 1, new double[] { 2.0, 4.0 });

        IccXyzColor relative = SpectralMath.ObserveRelative(emission, observer, illuminant);
        IccXyzColor absolute = SpectralMath.ObserveAbsolute(emission, observer);

        Assert.Multiple(() =>
        {
            Assert.That(new double[] { relative.X, relative.Y, relative.Z }, Is.EqualTo(new double[] { 0.5, 1.0, 1.5 }).Within(1e-12));
            Assert.That(relative.IsAbsolute, Is.False);
            Assert.That(new double[] { absolute.X, absolute.Y, absolute.Z }, Is.EqualTo(new double[] { 136600.0, 273200.0, 409800.0 }).Within(1e-8));
            Assert.That(absolute.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void PerfectReflectorUnderLateBoundD65ProducesItsNormalizedWhite()
    {
        IccProfileConnectionConditions pcc = new IccProfileConnectionConditions();
        pcc.ViewingConditions.Illuminant = IccStandardIlluminant.D65;
        pcc.ViewingConditions.Observer = IccMeasurementObserver.Cie1931TwoDegree;
        IccSpectralData reflector = Dense(400.0, 700.0, 31, 1, Enumerable.Repeat(1.0, 31).ToArray());

        IccXyzColor result = SpectralMath.ConvertToPccXyz(IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u, reflector, null, pcc);

        Assert.Multiple(() =>
        {
            Assert.That(result.X, Is.EqualTo(IccStandardColorimetry.D65X).Within(0.004));
            Assert.That(result.Y, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(result.Z, Is.EqualTo(IccStandardColorimetry.D65Z).Within(0.006));
            Assert.That(result.IsAbsolute, Is.False);
        });
    }

    [Test]
    public void DenseAndSparseDonaldsonMatricesProduceTheSameEmission()
    {
        IccSpectralRange incidentRange = Range(400.0, 500.0, 2);
        IccSpectralData illuminant = Dense(400.0, 500.0, 2, 1, new double[] { 10.0, 20.0 });
        IccSpectralData dense = Dense(500.0, 600.0, 2, 2, new double[] { 1.0, 2.0, 3.0, 4.0 });
        IccSpectralData sparse = new IccSpectralData
        {
            Range = Range(500.0, 600.0, 2),
            SpectrumCount = 2,
            SparseValues = new IccSparseMatrix
            {
                Rows = 2,
                Columns = 2,
                RowStartOffsets = new int[] { 0, 2, 4 },
                ColumnIndices = new int[] { 0, 1, 0, 1 },
                Values = new double[] { 1.0, 2.0, 3.0, 4.0 }
            }
        };

        IccSpectralData denseResult = SpectralMath.ApplyFluorescence(dense, incidentRange, illuminant);
        IccSpectralData sparseResult = SpectralMath.ApplyFluorescence(sparse, incidentRange, illuminant);

        Assert.Multiple(() =>
        {
            Assert.That(denseResult.Values, Is.EqualTo(new double[] { 50.0, 110.0 }).Within(1e-12));
            Assert.That(sparseResult.Values, Is.EqualTo(denseResult.Values).Within(1e-12));
        });
    }

    [Test]
    public void PccConversionDispatchesReflectanceRadianceAndFluorescence()
    {
        IccProfileConnectionConditions pcc = new IccProfileConnectionConditions
        {
            ViewingConditions = CustomConditions()
        };
        IccSpectralData vector = Dense(400.0, 500.0, 2, 1, new double[] { 1.0, 1.0 });
        IccSpectralData fluorescence = Dense(400.0, 500.0, 2, 2, new double[] { 1.0, 0.0, 0.0, 1.0 });
        IccSpectralRange incidentRange = Range(400.0, 500.0, 2);

        IccXyzColor reflective = SpectralMath.ConvertToPccXyz(IccColorSpaceSignatures.TransmissionSpectralPrefix | 2u, vector, null, pcc);
        IccXyzColor radiant = SpectralMath.ConvertToPccXyz(IccColorSpaceSignatures.RadiantSpectralPrefix | 2u, vector, null, pcc);
        IccXyzColor fluorescent = SpectralMath.ConvertToPccXyz(IccColorSpaceSignatures.BiSpectralPrefix | 4u, fluorescence, incidentRange, pcc);

        Assert.Multiple(() =>
        {
            Assert.That(new double[] { reflective.X, reflective.Y, reflective.Z }, Is.EqualTo(new double[] { 0.5, 1.0, 1.5 }).Within(1e-12));
            Assert.That(radiant.IsAbsolute, Is.True);
            Assert.That(new double[] { fluorescent.X, fluorescent.Y, fluorescent.Z }, Is.EqualTo(new double[] { 0.5, 1.0, 1.5 }).Within(1e-12));
        });
    }

    [Test]
    public void InvalidRangesSelectorsDimensionsAndZeroIlluminantsAreRejected()
    {
        IccSpectralData invalidRange = Dense(400.0, 500.0, 2, 1, new double[] { 1.0, 2.0 });
        invalidRange.Range.EndWavelength = 300.0;
        IccSpectralData emission = Dense(400.0, 500.0, 2, 1, new double[] { 1.0, 2.0 });
        IccSpectralData zeroIlluminant = Dense(400.0, 500.0, 2, 1, new double[] { 1.0, 0.0 });
        IccSpectralViewingConditions missing = new IccSpectralViewingConditions();
        IccSpectralViewingConditions coldDaylight = new IccSpectralViewingConditions
        {
            Illuminant = IccStandardIlluminant.Daylight,
            ColorTemperature = 3000.0
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => SpectralMath.Sample(invalidRange, 0, 450.0));
            Assert.Throws<IccTransformException>(() => SpectralMath.ResolveIlluminant(missing));
            Assert.Throws<IccTransformException>(() => SpectralMath.ResolveObserver(missing));
            Assert.Throws<IccTransformException>(() => SpectralMath.ResolveIlluminant(coldDaylight));
            Assert.Throws<IccTransformException>(() => SpectralMath.RemoveIlluminant(emission, zeroIlluminant));
        });
    }

    private static IccSpectralViewingConditions CustomConditions()
    {
        return new IccSpectralViewingConditions
        {
            IlluminantSpectrum = Dense(400.0, 500.0, 2, 1, new double[] { 2.0, 4.0 }),
            ObserverFunctions = Dense(400.0, 500.0, 2, 3, new double[]
            {
                1.0, 0.0,
                0.0, 1.0,
                1.0, 1.0
            })
        };
    }

    private static IccSpectralData Dense(double start, double end, int steps, int spectra, double[] values)
    {
        return new IccSpectralData
        {
            Range = Range(start, end, steps),
            Values = values,
            SpectrumCount = spectra
        };
    }

    private static IccSpectralRange Range(double start, double end, int steps)
    {
        return new IccSpectralRange
        {
            StartWavelength = start,
            EndWavelength = end,
            Steps = steps
        };
    }
}
