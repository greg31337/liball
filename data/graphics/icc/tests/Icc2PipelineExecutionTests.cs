using icc;

namespace icc.Tests;

public class Icc2PipelineExecutionTests
{
    [Test]
    public void ExtendedClutUsesItsInterpolationHintAndExecutes()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.ExtendedDevicePrefix | 2, IccColorSpaceSignatures.Xyz);
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.ExtendedClut,
            Kind = IccProcessingElementKind.ExtendedClut,
            InputChannels = 2,
            OutputChannels = 3,
            Clut = new IccClut
            {
                InputChannels = 2,
                OutputChannels = 3,
                GridPoints = new[] { 2, 2 },
                Values = new[]
                {
                    0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0,
                    1.0, 0.0, 0.0,
                    1.0, 1.0, 1.0
                },
                InterpolationHint = 1
            }
        };

        MpePipelinePlan plan = Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(2, 3, element), IccInterpolationKind.Tetrahedral);
        double[] result = Execute(plan, new[] { 0.25, 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages[0].Interpolation, Is.EqualTo(IccInterpolationKind.Multilinear));
            Assert.That(result, Is.EqualTo(new[] { 0.25, 0.5, 0.125 }).Within(1e-12));
        });
    }

    [Test]
    public void ExtendedClutExecutesTheIcc2SixteenInputLimit()
    {
        int pointCount = 1 << 16;
        double[] values = new double[pointCount * 3];
        values[values.Length - 3] = 0.2;
        values[values.Length - 2] = 0.4;
        values[values.Length - 1] = 0.8;
        IccProfile profile = Profile(IccColorSpaceSignatures.ExtendedDevicePrefix | 16, IccColorSpaceSignatures.Xyz);
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.ExtendedClut,
            Kind = IccProcessingElementKind.ExtendedClut,
            InputChannels = 16,
            OutputChannels = 3,
            Clut = new IccClut
            {
                InputChannels = 16,
                OutputChannels = 3,
                GridPoints = Enumerable.Repeat(2, 16).ToArray(),
                Values = values,
                InterpolationHint = 1
            }
        };
        MpePipelinePlan plan = Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(16, 3, element), IccInterpolationKind.Multilinear);

        double[] result = Execute(plan, Enumerable.Repeat(1.0, 16).ToArray());

        Assert.That(result, Is.EqualTo(new[] { 0.2, 0.4, 0.8 }).Within(1e-12));
    }

    [Test]
    public void SparseMatrixLutInterpolatesTheExpandedMatrixEntries()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccSparseMatrix first = Sparse(2, 2, new[] { 0, 1, 2 }, new[] { 0, 1 }, new[] { 1.0, 2.0 });
        IccSparseMatrix second = Sparse(2, 2, new[] { 0, 1, 2 }, new[] { 1, 0 }, new[] { 4.0, 8.0 });
        IccProcessingElement sparse = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.SparseMatrix,
            Kind = IccProcessingElementKind.SparseMatrix,
            InputChannels = 1,
            OutputChannels = 4,
            SparseMatrixLut = new IccSparseMatrixArray
            {
                EquivalentOutputChannels = 4,
                GridPoints = new[] { 2 },
                Matrices = new[] { first, second }
            }
        };
        IccProcessingElement select = Matrix(4, 3, new[]
        {
            1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0, 0.0
        });

        MpePipelinePlan plan = Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(1, 3, sparse, select), IccInterpolationKind.Multilinear);
        double[] result = Execute(plan, new[] { 0.25 });

        Assert.That(result, Is.EqualTo(new[] { 0.75, 1.0, 2.0 }).Within(1e-12));
    }

    [Test]
    public void TintArrayClipsAndLinearlyInterpolatesEveryOutputChannel()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccProcessingElement tint = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.TintArray,
            Kind = IccProcessingElementKind.TintArray,
            InputChannels = 1,
            OutputChannels = 3,
            Clut = new IccClut
            {
                InputChannels = 1,
                OutputChannels = 3,
                GridPoints = new[] { 3 },
                Values = new[] { 0.0, 0.0, 0.0, 0.5, 0.25, 0.75, 1.0, 0.5, 1.5 }
            }
        };
        MpePipelinePlan plan = Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(1, 3, tint), IccInterpolationKind.Tetrahedral);

        Assert.Multiple(() =>
        {
            Assert.That(Execute(plan, new[] { 0.25 }), Is.EqualTo(new[] { 0.25, 0.125, 0.375 }).Within(1e-12));
            Assert.That(Execute(plan, new[] { 2.0 }), Is.EqualTo(new[] { 1.0, 0.5, 1.5 }).Within(1e-12));
        });
    }

    [Test]
    public void HdrToSdrPipelineExecutesToneMapWithoutClippingExtendedPcsValues()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        profile.Header.UsesExtendedPcsRange = true;
        IccProcessingElement addLuminance = Matrix(3, 4, new[]
        {
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 1.0, 0.0, 0.0
        });
        IccProcessingElement toneMap = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.ToneMap,
            Kind = IccProcessingElementKind.ToneMap,
            InputChannels = 4,
            OutputChannels = 3,
            ToneMap = new IccToneMap
            {
                LuminanceCurve = FormulaCurve(1.0),
                Functions = new[]
                {
                    MapFunction(0.5, 1.0, 0.0),
                    MapFunction(1.0, 0.0, 0.0),
                    MapFunction(2.0, -0.5, 1.0)
                }
            }
        };

        MpePipelinePlan plan = Compile(profile, IccTagSignatures.HdrToSdr1, IccTransformDirection.DeviceToDevice, Pipeline(3, 3, addLuminance, toneMap), IccInterpolationKind.Multilinear);
        double[] result = Execute(plan, new[] { 2.0, 3.0, 4.0 });

        Assert.That(result, Is.EqualTo(new[] { 4.5, 9.0, 22.0 }).Within(1e-12));
    }

    [Test]
    public void EmissionObserverUsesPccObserverAndElementWhiteNormalization()
    {
        IccProfile profile = SpectralProfile(3);
        IccProcessingElement observer = SpectralElement(IccProcessingElementKind.EmissionObserver, IccProcessingElementSignatures.EmissionObserver, 3, RelativeSpectralData());

        MpePipelinePlan plan = Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(3, 3, observer), IccInterpolationKind.Multilinear);
        double[] result = Execute(plan, new[] { 2.0, 3.0, 4.0 });

        Assert.That(result, Is.EqualTo(new[] { 2.0, 3.0, 4.0 }).Within(1e-12));
    }

    [Test]
    public void EmissionMatrixAndInverseEmissionMatrixAreColorimetricallyLowered()
    {
        IccProfile profile = SpectralProfile(3);
        IccSpectralProcessingData spectral = RelativeSpectralData();
        double[] spectralRows = new[]
        {
            1.0, 0.0, 0.0,
            0.0, 2.0, 0.0,
            0.0, 0.0, 4.0,
            0.5, 1.0, 2.0
        };
        IccProcessingElement forward = SpectralMatrix(IccProcessingElementKind.EmissionMatrix, IccProcessingElementSignatures.EmissionMatrix, spectral, spectralRows);
        IccProcessingElement inverse = SpectralMatrix(IccProcessingElementKind.InverseEmissionMatrix, IccProcessingElementSignatures.InverseEmissionMatrix, spectral, spectralRows);

        double[] color = Execute(Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(3, 3, forward), IccInterpolationKind.Multilinear), new[] { 1.0, 2.0, 3.0 });
        double[] recovered = Execute(Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(3, 3, inverse), IccInterpolationKind.Multilinear), color);

        Assert.Multiple(() =>
        {
            Assert.That(color, Is.EqualTo(new[] { 1.5, 5.0, 14.0 }).Within(1e-12));
            Assert.That(recovered, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }).Within(1e-12));
        });
    }

    [Test]
    public void EmissionAndReflectanceSpectralClutsConvertEntriesBeforeInterpolation()
    {
        IccProfile profile = SpectralProfile(1);
        IccProcessingElement emission = SpectralClut(IccProcessingElementKind.EmissionClut, IccProcessingElementSignatures.EmissionClut, RelativeSpectralData());
        IccSpectralProcessingData reflectanceData = RelativeSpectralData();
        reflectanceData.Flags = 1;
        IccProcessingElement reflectance = SpectralClut(IccProcessingElementKind.ReflectanceClut, IccProcessingElementSignatures.ReflectanceClut, reflectanceData);

        double[] emitted = Execute(Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(1, 3, emission), IccInterpolationKind.Multilinear), new[] { 0.25 });
        double[] reflected = Execute(Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(1, 3, reflectance), IccInterpolationKind.Multilinear), new[] { 0.25 });

        Assert.Multiple(() =>
        {
            Assert.That(emitted, Is.EqualTo(new[] { 0.25, 0.5, 0.75 }).Within(1e-12));
            Assert.That(reflected, Is.EqualTo(new[] { 0.25, 0.5, 0.75 }).Within(1e-12));
        });
    }

    [Test]
    public void ReflectanceObserverAppliesThePccIlluminantBeforeObservation()
    {
        IccProfile profile = SpectralProfile(3);
        IccProcessingElement observer = SpectralElement(IccProcessingElementKind.ReflectanceObserver, IccProcessingElementSignatures.ReflectanceObserver, 3, AbsoluteSpectralData());

        double[] result = Execute(Compile(profile, IccTagSignatures.AToB0, IccTransformDirection.DeviceToPcs, Pipeline(3, 3, observer), IccInterpolationKind.Multilinear), new[] { 0.2, 0.4, 0.6 });

        Assert.That(result, Is.EqualTo(new[] { 0.2, 0.4, 0.6 }).Within(1e-12));
    }

    private static MpePipelinePlan Compile(IccProfile profile, uint signature, IccTransformDirection direction, IccMultiProcessElements pipeline, IccInterpolationKind interpolation)
    {
        IccTag tag = new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.MultiProcessElements,
                MultiProcessElements = pipeline,
                RawData = new IccRawData { HasTypeSignature = true, TypeSignature = IccTypeSignatures.MultiProcessElements }
            }
        };
        return MpePipelineCompiler.Compile(profile, tag, direction, interpolation);
    }

    private static double[] Execute(MpePipelinePlan plan, double[] input)
    {
        int values = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indices = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weights = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        double[] output = new double[plan.DestinationChannels];
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output, new double[values], new double[values], new int[indices], new double[weights]);
        return output;
    }

    private static IccProfile Profile(uint dataSpace, uint pcs)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.DataColorSpaceSignature = dataSpace;
        profile.Header.ProfileConnectionSpaceSignature = pcs;
        return profile;
    }

    private static IccProfile SpectralProfile(int dataChannels)
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.ExtendedDevicePrefix | (uint)dataChannels, IccColorSpaceSignatures.Xyz);
        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions
        {
            ObserverFunctions = Spectrum(3, new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0
            }),
            IlluminantSpectrum = Spectrum(1, new[] { 1.0, 1.0, 1.0 })
        };
        profile.Tags = new[]
        {
            new IccTag
            {
                Signature = IccTagSignatures.SpectralViewingConditions,
                Data = new IccTagData { Kind = IccTagDataKind.SpectralViewingConditions, SpectralViewingConditions = conditions }
            }
        };
        return profile;
    }

    private static IccMultiProcessElements Pipeline(int inputs, int outputs, params IccProcessingElement[] elements)
    {
        return new IccMultiProcessElements
        {
            InputChannels = inputs,
            OutputChannels = outputs,
            InputRanges = Ranges(inputs),
            OutputRanges = Ranges(outputs),
            Elements = elements,
            IsExecutable = true
        };
    }

    private static IccEncodingRange[] Ranges(int count)
    {
        return Enumerable.Range(0, count).Select(value => new IccEncodingRange { Minimum = float.MinValue, Maximum = float.MaxValue }).ToArray();
    }

    private static IccProcessingElement Matrix(int inputs, int outputs, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = inputs,
            OutputChannels = outputs,
            Matrix = new IccMatrix { Rows = outputs, Columns = inputs + 1, Values = values }
        };
    }

    private static IccSparseMatrix Sparse(int rows, int columns, int[] offsets, int[] columnIndices, double[] values)
    {
        return new IccSparseMatrix
        {
            Rows = rows,
            Columns = columns,
            RowStartOffsets = offsets,
            ColumnIndices = columnIndices,
            Values = values
        };
    }

    private static IccCurve FormulaCurve(double exponent)
    {
        return new IccCurve
        {
            Kind = IccCurveKind.Segmented,
            Segments = new[]
            {
                new IccCurveSegment
                {
                    Kind = IccCurveSegmentKind.Formula,
                    FormulaType = 0,
                    BreakPoint = double.NegativeInfinity,
                    Parameters = new[] { exponent, 1.0, 0.0, 0.0 }
                }
            }
        };
    }

    private static IccToneMapFunction MapFunction(double a, double b, double c)
    {
        return new IccToneMapFunction { FunctionType = 0, Parameters = new[] { a, b, c } };
    }

    private static IccSpectralData Spectrum(int count, double[] values)
    {
        return new IccSpectralData
        {
            Range = new IccSpectralRange { StartWavelength = 400.0, EndWavelength = 600.0, Steps = 3 },
            SpectrumCount = count,
            Values = values
        };
    }

    private static IccSpectralProcessingData RelativeSpectralData()
    {
        return new IccSpectralProcessingData { White = Spectrum(1, new[] { 1.0, 1.0, 1.0 }), Flags = 0 };
    }

    private static IccSpectralProcessingData AbsoluteSpectralData()
    {
        return new IccSpectralProcessingData { White = Spectrum(1, new[] { 1.0, 1.0, 1.0 }), Flags = 1 };
    }

    private static IccProcessingElement SpectralElement(IccProcessingElementKind kind, uint signature, int inputs, IccSpectralProcessingData spectral)
    {
        return new IccProcessingElement
        {
            Kind = kind,
            Signature = signature,
            InputChannels = inputs,
            OutputChannels = 3,
            SpectralProcessingData = spectral
        };
    }

    private static IccProcessingElement SpectralMatrix(IccProcessingElementKind kind, uint signature, IccSpectralProcessingData spectral, double[] values)
    {
        IccProcessingElement element = SpectralElement(kind, signature, 3, spectral);
        element.Matrix = new IccMatrix { Rows = 4, Columns = 3, Values = values };
        return element;
    }

    private static IccProcessingElement SpectralClut(IccProcessingElementKind kind, uint signature, IccSpectralProcessingData spectral)
    {
        IccProcessingElement element = SpectralElement(kind, signature, 1, spectral);
        element.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 3,
            GridPoints = new[] { 2 },
            Values = new[] { 0.0, 0.0, 0.0, 1.0, 2.0, 3.0 },
            InterpolationHint = 1
        };
        return element;
    }
}
