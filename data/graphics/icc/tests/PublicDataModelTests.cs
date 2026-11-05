using icc;

namespace icc.Tests;

public class PublicDataModelTests
{
    [Test]
    public void ConstructorsProvideUsableIndependentDataGraphs()
    {
        IccTagData firstTagData = new IccTagData();
        IccTagData secondTagData = new IccTagData();
        IccCurve curve = new IccCurve();
        IccEncodingRange range = new IccEncodingRange();
        IccValidationReport validation = new IccValidationReport();
        IccProfileSummary summary = new IccProfileSummary();
        IccProfileBuildOptions build = new IccProfileBuildOptions();
        IccTransformPlan transform = new IccTransformPlan();
        IccComparisonReport comparison = new IccComparisonReport();
        IccEmbeddedProfileResult embedding = new IccEmbeddedProfileResult();
        IccValidationIssue issue = new IccValidationIssue();
        IccRgbProfileDefinition rgb = new IccRgbProfileDefinition();
        IccGrayProfileDefinition gray = new IccGrayProfileDefinition();
        IccTransformStage stage = new IccTransformStage();
        IccTransformTrace trace = new IccTransformTrace();
        IccProfileLinkOptions link = new IccProfileLinkOptions();
        IccGamutResult gamut = new IccGamutResult();
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();

        firstTagData.IntegerValues = new long[] { 17 };
        summary.BuildOptions = build;
        summary.Comparison = comparison;
        summary.Embedding = embedding;
        comparison.TransformPlan = transform;
        transform.LinkOptions = link;
        transform.Trace = trace;
        transform.Stages = new IccTransformStage[] { stage };
        build.RgbDefinition = rgb;
        build.GrayDefinition = gray;
        summary.Gamut = gamut;
        embedded.Summary = summary;

        Assert.Multiple(() =>
        {
            Assert.That(secondTagData.IntegerValues, Is.Empty);
            Assert.That(secondTagData.IntegerValues, Is.Not.SameAs(firstTagData.IntegerValues));
            Assert.That(secondTagData.RawData, Is.Not.SameAs(firstTagData.RawData));
            Assert.That(curve.Kind, Is.EqualTo(IccCurveKind.Identity));
            Assert.That(curve.Gamma, Is.EqualTo(1.0));
            Assert.That(range.Minimum, Is.Zero);
            Assert.That(range.Maximum, Is.EqualTo(1.0));
            Assert.That(validation.Policy, Is.Not.Null);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(validation.TotalIssueCount, Is.Zero);
            Assert.That(validation.InformationCount, Is.Zero);
            Assert.That(validation.WarningCount, Is.Zero);
            Assert.That(validation.ErrorCount, Is.Zero);
            Assert.That(validation.FatalCount, Is.Zero);
            Assert.That(validation.WasTruncated, Is.False);
            Assert.That(validation.Result, Is.Not.Null);
            Assert.That(summary.Root, Is.Not.Null);
            Assert.That(summary.Validation, Is.Not.Null);
            Assert.That(summary.BuildOptions, Is.SameAs(build));
            Assert.That(summary.Comparison, Is.SameAs(comparison));
            Assert.That(summary.Embedding, Is.SameAs(embedding));
            Assert.That(comparison.TransformPlan, Is.SameAs(transform));
            Assert.That(transform.Options, Is.Not.Null);
            Assert.That(transform.Options.ClipOutput, Is.True);
            Assert.That(transform.Stages, Is.EqualTo(new IccTransformStage[] { stage }));
            Assert.That(transform.Trace, Is.SameAs(trace));
            Assert.That(transform.LinkOptions, Is.SameAs(link));
            Assert.That(trace.Source, Is.Not.SameAs(trace.Destination));
            Assert.That(trace.Intermediates, Is.Empty);
            Assert.That(trace.Messages, Is.Empty);
            Assert.That(stage.Name, Is.Empty);
            Assert.That(issue.Location, Is.Not.Null);
            Assert.That(rgb.RedPrimary, Is.Not.SameAs(rgb.GreenPrimary));
            Assert.That(rgb.RedCurve, Is.Not.SameAs(rgb.GreenCurve));
            Assert.That(gray.WhitePoint, Is.Not.Null);
            Assert.That(gray.GrayCurve.Kind, Is.EqualTo(IccCurveKind.Identity));
            Assert.That(build.RgbDefinition, Is.SameAs(rgb));
            Assert.That(build.GrayDefinition, Is.SameAs(gray));
            Assert.That(summary.Gamut, Is.SameAs(gamut));
            Assert.That(gamut.Status, Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(gamut.Result, Is.Not.Null);
            Assert.That(embedding.Options, Is.Not.Null);
            Assert.That(embedding.Profiles, Is.Empty);
            Assert.That(embedding.CarrierBytes, Is.Empty);
            Assert.That(embedded.Profile, Is.Not.Null);
            Assert.That(embedded.Parts, Is.Empty);
            Assert.That(embedded.Summary, Is.SameAs(summary));
        });
    }

    [Test]
    public void CloneTagDataCopiesTypedPayloadsWithoutSourceAliases()
    {
        IccTagData source = CreatePopulatedTagData();

        IccTagData clone = ProfileClone.CloneTagDataStandalone(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone.IntegerValues, Is.EqualTo(source.IntegerValues).And.Not.SameAs(source.IntegerValues));
            Assert.That(clone.FixedPointValues, Is.EqualTo(source.FixedPointValues).And.Not.SameAs(source.FixedPointValues));
            Assert.That(clone.XyzValues, Is.Not.SameAs(source.XyzValues));
            Assert.That(clone.XyzValues[0], Is.Not.SameAs(source.XyzValues[0]));
            Assert.That(clone.XyzValues[0].Y, Is.EqualTo(0.5));
            Assert.That(clone.BinaryData, Is.EqualTo(source.BinaryData).And.Not.SameAs(source.BinaryData));
            Assert.That(clone.Text, Is.Not.SameAs(source.Text));
            Assert.That(clone.Text!.Text, Is.EqualTo("payload"));
            Assert.That(clone.LocalizedStrings[0], Is.Not.SameAs(source.LocalizedStrings[0]));
            Assert.That(clone.Metadata[0], Is.Not.SameAs(source.Metadata[0]));
            Assert.That(clone.Curve, Is.Not.SameAs(source.Curve));
            Assert.That(clone.Curve!.Samples, Is.EqualTo(new double[] { 0.0, 1.0 }));
            Assert.That(clone.Lut, Is.Not.SameAs(source.Lut));
            Assert.That(clone.Lut!.InputTables[0], Is.Not.SameAs(source.Lut!.InputTables[0]));
            Assert.That(clone.MultiStageLut, Is.Not.SameAs(source.MultiStageLut));
            Assert.That(clone.Measurement, Is.Not.SameAs(source.Measurement));
            Assert.That(clone.ViewingConditions, Is.Not.SameAs(source.ViewingConditions));
            Assert.That(clone.Chromaticity, Is.Not.SameAs(source.Chromaticity));
            Assert.That(clone.Cicp!.ColorPrimaries, Is.EqualTo(9));
            Assert.That(clone.Cicp.UsesProfilePrimaries, Is.True);
            Assert.That(clone.NamedColors!.Colors[0].Name, Is.EqualTo("sample"));
            Assert.That(clone.Colorants!.Colorants[0].Name, Is.EqualTo("cyan"));
            Assert.That(clone.ResponseCurves[0].DeviceCodes[0], Is.EqualTo(new ushort[] { 2, 4 }));
            Assert.That(clone.ProfileSequence[0].DeviceModelSignature, Is.EqualTo(0x6d6f646c));
            Assert.That(clone.ProfileSequenceIdentifiers[0].Descriptions[0].Text, Is.EqualTo("profile"));
            Assert.That(clone.AdaptiveGainCurve!.RedNodes[0].Gain, Is.EqualTo(1.25));
            Assert.That(clone.SpectralData!.SparseValues!.Values, Is.EqualTo(new double[] { 0.75 }));
            Assert.That(clone.GamutBoundary!.Vertices[0].PcsColor.Components, Is.EqualTo(new double[] { 0.2, 0.3, 0.4 }));
            Assert.That(clone.EmbeddedImage!.Bytes, Is.EqualTo(new byte[] { 7, 8 }).And.Not.SameAs(source.EmbeddedImage!.Bytes));
            Assert.That(clone.ColorEncodingParameters!.Metadata[0].Value, Is.EqualTo("value"));
            Assert.That(clone.ProfileConnectionConditions!.MediaWhitePoint.Y, Is.EqualTo(1.0));
            Assert.That(clone.MultiplexChannels!.Names, Is.EqualTo(new string[] { "material" }));
            Assert.That(clone.Brdf!.TypeSignature, Is.EqualTo(StructuredTagProjectionCodec.MonochromeBrdf));
            Assert.That(clone.Brdf!.Parameters, Is.EqualTo(new double[] { 0.4 }));
            Assert.That(clone.SurfaceMap!.Scale, Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }));
        });

        clone.IntegerValues[0] = 99;
        clone.Curve!.Samples[0] = 0.5;
        clone.SpectralData!.SparseValues!.Values[0] = 0.25;
        clone.EmbeddedImage!.Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(source.IntegerValues[0], Is.EqualTo(3));
            Assert.That(source.Curve!.Samples[0], Is.Zero);
            Assert.That(source.SpectralData!.SparseValues!.Values[0], Is.EqualTo(0.75));
            Assert.That(source.EmbeddedImage!.Bytes[0], Is.EqualTo(7));
        });
    }

    [Test]
    public void CloneTagDataPreservesRecursiveHierarchyAndCalculatorIdentity()
    {
        IccTagData source = new IccTagData();
        IccTagArray array = new IccTagArray();
        IccTagStructure structure = new IccTagStructure();
        IccStructureMember member = new IccStructureMember();
        IccProcessingElement element = new IccProcessingElement();
        IccCalculatorProgram calculator = new IccCalculatorProgram();
        IccCalculatorInstruction instruction = new IccCalculatorInstruction();
        IccMultiProcessElements elements = new IccMultiProcessElements();
        array.Values = new IccTagData[] { source };
        member.Data = source;
        structure.Members = new IccStructureMember[] { member };
        instruction.Signature = 0x64617461;
        instruction.Operand = 0x3f800000;
        instruction.FloatingPointValue = 1.0;
        instruction.IsKnown = true;
        calculator.Instructions = new IccCalculatorInstruction[] { instruction };
        calculator.SubElements = new IccProcessingElement[] { element };
        element.Calculator = calculator;
        elements.Elements = new IccProcessingElement[] { element };
        source.TagArray = array;
        source.TagStructure = structure;
        source.Calculator = calculator;
        source.MultiProcessElements = elements;

        IccTagData clone = ProfileClone.CloneTagDataStandalone(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone.TagArray, Is.Not.SameAs(array));
            Assert.That(clone.TagArray!.Values[0], Is.SameAs(clone));
            Assert.That(clone.TagStructure, Is.Not.SameAs(structure));
            Assert.That(clone.TagStructure!.Members[0].Data, Is.SameAs(clone));
            Assert.That(clone.Calculator, Is.Not.SameAs(calculator));
            Assert.That(clone.Calculator!.Instructions[0], Is.Not.SameAs(instruction));
            Assert.That(clone.Calculator.Instructions[0].Operand, Is.EqualTo(0x3f800000));
            Assert.That(clone.MultiProcessElements!.Elements[0].Calculator, Is.SameAs(clone.Calculator));
            Assert.That(clone.Calculator!.SubElements[0], Is.SameAs(clone.MultiProcessElements.Elements[0]));
        });
    }

    private static IccTagData CreatePopulatedTagData()
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.TagStructure;
        data.IntegerValues = new long[] { 3, 5 };
        data.FixedPointValues = new double[] { -0.25, 1.5 };
        data.XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = 0.25, Y = 0.5, Z = 0.75 } };
        data.Signature = 0x74657374;
        data.DateTime = new IccDateTime { Year = 2026, Month = 9, Day = 11 };
        data.BinaryData = new byte[] { 1, 2, 3 };
        data.Text = new IccTextData { Text = "payload", Bytes = new byte[] { 4 } };
        data.LocalizedStrings = new IccLocalizedString[] { new IccLocalizedString { Language = "en", Country = "US", Text = "name" } };
        data.Metadata = new IccMetadataEntry[] { new IccMetadataEntry { Name = "key", Value = "value" } };
        data.Curve = new IccCurve
        {
            Kind = IccCurveKind.Sampled,
            Samples = new double[] { 0.0, 1.0 },
            Parametric = new IccParametricCurve { FunctionType = 1, Parameters = new double[] { 2.2 } },
            Segments = new IccCurveSegment[] { new IccCurveSegment { BreakPoint = 0.5, Samples = new double[] { 0.0, 1.0 } } },
            Analysis = new IccCurveAnalysis { IsMonotonic = true, IsInvertible = true, Maximum = 1.0 }
        };
        data.Lut = new IccLut
        {
            InputChannels = 1,
            OutputChannels = 1,
            Matrix = new IccMatrix { Rows = 1, Columns = 1, Values = new double[] { 1.0 } },
            InputTables = new double[][] { new double[] { 0.0, 1.0 } },
            Clut = new IccClut { InputChannels = 1, OutputChannels = 1, GridPoints = new int[] { 2 }, Values = new double[] { 0.0, 1.0 } },
            OutputTables = new double[][] { new double[] { 0.0, 1.0 } }
        };
        data.MultiStageLut = new IccMultiStageLut { ACurves = new IccCurve[] { new IccCurve() } };
        data.MultiProcessElements = new IccMultiProcessElements { Elements = new IccProcessingElement[] { new IccProcessingElement() } };
        data.Measurement = new IccMeasurement { Flare = 0.01 };
        data.ViewingConditions = new IccViewingConditions { IlluminantKind = IccStandardIlluminant.D50 };
        data.Chromaticity = new IccChromaticity { XCoordinates = new double[] { 0.64 }, YCoordinates = new double[] { 0.33 } };
        data.Cicp = new IccCicp { ColorPrimaries = 9, TransferCharacteristics = 16, MatrixCoefficients = 9, UsesProfilePrimaries = true };
        data.NamedColors = new IccNamedColorSet { Colors = new IccNamedColor[] { new IccNamedColor { Name = "sample" } } };
        data.Colorants = new IccColorantSet { Colorants = new IccColorant[] { new IccColorant { Name = "cyan" } } };
        data.ResponseCurves = new IccResponseCurve[]
        {
            new IccResponseCurve
            {
                MaximumColorantXyz = new IccXyzNumber[] { new IccXyzNumber { X = 1.0 } },
                DeviceCodes = new ushort[][] { new ushort[] { 2, 4 } },
                Measurements = new double[][] { new double[] { 0.1, 0.2 } }
            }
        };
        data.ProfileSequence = new IccProfileSequenceEntry[] { new IccProfileSequenceEntry { DeviceModelSignature = 0x6d6f646c } };
        data.ProfileSequenceIdentifiers = new IccProfileSequenceIdentifier[]
        {
            new IccProfileSequenceIdentifier { Descriptions = new IccLocalizedString[] { new IccLocalizedString { Text = "profile" } } }
        };
        data.AdaptiveGainCurve = new IccAdaptiveGainCurve
        {
            Identifier = new byte[] { 1, 2 },
            RedNodes = new IccAdaptiveGainCurveNode[] { new IccAdaptiveGainCurveNode { Input = 0.5, Gain = 1.25 } }
        };
        data.SpectralData = new IccSpectralData
        {
            Range = new IccSpectralRange { StartWavelength = 380.0, EndWavelength = 730.0, Steps = 36 },
            Values = new double[] { 0.5 },
            SparseValues = new IccSparseMatrix
            {
                Rows = 1,
                Columns = 1,
                RowStartOffsets = new int[] { 0, 1 },
                ColumnIndices = new int[] { 0 },
                Values = new double[] { 0.75 }
            }
        };
        data.GamutBoundary = new IccGamutBoundary
        {
            Vertices = new IccGamutVertex[]
            {
                new IccGamutVertex { PcsColor = new IccColorVector { Components = new double[] { 0.2, 0.3, 0.4 } } }
            },
            Triangles = new IccGamutTriangle[] { new IccGamutTriangle { First = 0, Second = 0, Third = 0 } }
        };
        data.EmbeddedImage = new IccEmbeddedImage { Width = 1, Height = 1, Components = 2, Bytes = new byte[] { 7, 8 } };
        data.Calculator = new IccCalculatorProgram { Bytecode = new byte[] { 10 }, InstructionCount = 1 };
        data.ColorEncodingParameters = new IccColorEncodingParameters
        {
            ReferenceWhite = new IccXyzColor { Y = 1.0 },
            Metadata = new IccMetadataEntry[] { new IccMetadataEntry { Name = "key", Value = "value" } }
        };
        data.ProfileConnectionConditions = new IccProfileConnectionConditions { MediaWhitePoint = new IccXyzColor { Y = 1.0 } };
        data.MultiplexChannels = new IccMultiplexChannels { Names = new string[] { "material" }, DefaultValues = new double[] { 0.5 }, Order = new int[] { 0 } };
        data.Brdf = new IccBrdf { TypeSignature = StructuredTagProjectionCodec.MonochromeBrdf, ModelSignature = 0x62726466, Parameters = new double[] { 0.4 } };
        data.SurfaceMap = new IccSurfaceMap { Width = 1, Height = 1, Scale = new double[] { 1.0, 2.0, 3.0 } };
        return data;
    }
}
