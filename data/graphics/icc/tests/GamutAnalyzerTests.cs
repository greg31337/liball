using icc;

namespace icc.Tests;

public class GamutAnalyzerTests
{
    [Test]
    public void GamutBoundaryClassifiesInsideOutsideAndBoundaryPoints()
    {
        IIccGamutAnalyzer analyzer = new IccGamutAnalyzer();
        IccProfile profile = CreateBoundaryProfile(CreateTetrahedron());

        IccGamutResult inside = analyzer.EvaluateProfileGamutDetails(profile, new float[] { 0.1f, 0.1f, 0.1f });
        IccGamutResult outside = analyzer.EvaluateProfileGamutDetails(profile, new float[] { 0.5f, 0.5f, 0.5f });
        IccGamutResult boundary = analyzer.EvaluateProfileGamutDetails(profile, new float[] { 0.2f, 0.3f, 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(inside.Status, Is.EqualTo(IccGamutStatus.Inside));
            Assert.That(outside.Status, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(boundary.Status, Is.EqualTo(IccGamutStatus.Boundary));
            Assert.That(analyzer.EvaluateProfileGamut(profile, new float[] { 0.1f, 0.1f, 0.1f }),
                Is.EqualTo(IccGamutStatus.Inside));
            Assert.That(analyzer.EvaluateProfileGamut(profile, new float[] { 0.5f, 0.5f, 0.5f }),
                Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(analyzer.EvaluateProfileGamut(profile, new float[] { 0.2f, 0.3f, 0.5f }),
                Is.EqualTo(IccGamutStatus.Boundary));
            Assert.That(boundary.Distance, Is.EqualTo(0.0).Within(0.000001));
            Assert.That(boundary.NearestBoundaryColor, Is.Not.Null);
            Assert.That(boundary.NearestBoundaryColor!.Components,
                Is.EqualTo(new double[] { 0.2, 0.3, 0.5 }).Within(0.000001));
            Assert.That(analyzer.ProbeProfileGamut(profile).Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void GamutBoundaryReportsNearestPointDistanceAndMeshSummary()
    {
        IIccGamutAnalyzer analyzer = new IccGamutAnalyzer();
        IccProfile profile = CreateBoundaryProfile(CreateTetrahedron());

        IccGamutResult result = analyzer.EvaluateProfileGamutDetails(profile, new float[] { 0.5f, 0.5f, 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Color.Components, Is.EqualTo(new double[] { 0.5, 0.5, 0.5 }));
            Assert.That(result.Color.ColorSpace, Is.EqualTo(IccColorSpaceKind.Xyz));
            Assert.That(result.NearestBoundaryColor, Is.Not.Null);
            Assert.That(result.NearestBoundaryColor!.Components,
                Is.EqualTo(new double[] { 1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0 }).Within(0.000001));
            Assert.That(result.NearestBoundaryColor.ColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(result.Distance, Is.EqualTo(System.Math.Sqrt(1.0 / 12.0)).Within(0.000001));
            Assert.That(result.Result.Message, Does.Contain("4 vertices and 4 triangles"));
        });
    }

    [Test]
    public void IccGamutTagExecutesThreeToOneLutClassification()
    {
        IIccGamutAnalyzer analyzer = new IccGamutAnalyzer();
        IccProfile insideProfile = CreateGamutTagProfile(0.0);
        IccProfile outsideProfile = CreateGamutTagProfile(1.0);

        IccGamutResult inside = analyzer.EvaluateProfileGamutDetails(insideProfile, new float[] { 0.2f, 0.3f, 0.4f });
        IccGamutResult outside = analyzer.EvaluateProfileGamutDetails(outsideProfile, new float[] { 0.2f, 0.3f, 0.4f });

        Assert.Multiple(() =>
        {
            Assert.That(inside.Status, Is.EqualTo(IccGamutStatus.Inside));
            Assert.That(outside.Status, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(inside.NearestBoundaryColor, Is.Null);
            Assert.That(outside.NearestBoundaryColor, Is.Null);
            Assert.That(analyzer.ProbeProfileGamut(insideProfile).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(analyzer.ProbeProfileGamut(outsideProfile).Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void MissingOrInvalidGamutModelsProduceExplicitUnknownResults()
    {
        IIccGamutAnalyzer analyzer = new IccGamutAnalyzer();
        IccProfile missing = CreateBoundaryProfile(null);
        IccProfile invalid = CreateBoundaryProfile(CreateCoplanarBoundary());

        IccGamutResult missingResult = analyzer.EvaluateProfileGamutDetails(missing, new float[] { 0.1f, 0.1f, 0.1f });
        IccGamutResult invalidResult = analyzer.EvaluateProfileGamutDetails(invalid, new float[] { 0.1f, 0.1f, 0.1f });

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Status, Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(missingResult.Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(missingResult.Result.Message, Does.Contain("no valid executable gamut model"));
            Assert.That(missingResult.NearestBoundaryColor, Is.Null);
            Assert.That(missingResult.Distance, Is.Zero);
            Assert.That(invalidResult.Status, Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(invalidResult.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(analyzer.EvaluateProfileGamut(missing, new float[] { 0.1f, 0.1f, 0.1f }),
                Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(analyzer.EvaluateProfileGamut(invalid, new float[] { 0.1f, 0.1f, 0.1f }),
                Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(analyzer.ProbeProfileGamut(missing).Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(analyzer.ProbeProfileGamut(invalid).Status, Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void ExecutableGamutModelsRequireThreeFinitePcsChannels()
    {
        IIccGamutAnalyzer analyzer = new IccGamutAnalyzer();
        IccProfile profile = CreateBoundaryProfile(CreateTetrahedron());

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() =>
                analyzer.EvaluateProfileGamut(profile, new float[] { 0.1f, 0.1f }));
            Assert.Throws<IccProfileValidationException>(() =>
                analyzer.EvaluateProfileGamut(profile, new float[] { 0.1f, float.NaN, 0.1f }));
        });
    }

    private static IccProfile CreateBoundaryProfile(IccGamutBoundary? boundary)
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version5);
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        if (boundary != null)
        {
            IccTag tag = new IccTag();
            tag.Signature = IccTagSignatures.GamutBoundary1;
            tag.Data.Kind = IccTagDataKind.GamutBoundary;
            tag.Data.GamutBoundary = boundary;
            profile.Tags = new IccTag[] { tag };
        }
        return profile;
    }

    private static IccProfile CreateGamutTagProfile(double classification)
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4);
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.Gamut;
        tag.Data.Kind = IccTagDataKind.Lut;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.Lut16;
        tag.Data.Lut = new IccLut
        {
            InputChannels = 3,
            OutputChannels = 1,
            PcsEncoding = IccPcsEncodingKind.Version2,
            Matrix = new IccMatrix
            {
                Rows = 3,
                Columns = 3,
                Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
            },
            InputTables = new double[][]
            {
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 }
            },
            Clut = new IccClut
            {
                InputChannels = 3,
                OutputChannels = 1,
                GridPoints = new int[] { 2, 2, 2 },
                Values = new double[]
                {
                    classification, classification, classification, classification,
                    classification, classification, classification, classification
                }
            },
            OutputTables = new double[][] { new double[] { 0.0, 0.5, 1.0 } }
        };
        profile.Tags = new IccTag[] { tag };
        return profile;
    }

    private static IccProfile CreateProfile(IccVersionKind version)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = IccProfileClass.Output;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return profile;
    }

    private static IccGamutBoundary CreateTetrahedron()
    {
        return new IccGamutBoundary
        {
            PcsChannels = 3,
            DeviceChannels = 0,
            Vertices = new IccGamutVertex[]
            {
                CreateVertex(0.0, 0.0, 0.0),
                CreateVertex(1.0, 0.0, 0.0),
                CreateVertex(0.0, 1.0, 0.0),
                CreateVertex(0.0, 0.0, 1.0)
            },
            Triangles = CreateTetrahedronTriangles()
        };
    }

    private static IccGamutBoundary CreateCoplanarBoundary()
    {
        return new IccGamutBoundary
        {
            PcsChannels = 3,
            DeviceChannels = 0,
            Vertices = new IccGamutVertex[]
            {
                CreateVertex(0.0, 0.0, 0.0),
                CreateVertex(1.0, 0.0, 0.0),
                CreateVertex(1.0, 1.0, 0.0),
                CreateVertex(0.0, 1.0, 0.0)
            },
            Triangles = CreateTetrahedronTriangles()
        };
    }

    private static IccGamutTriangle[] CreateTetrahedronTriangles()
    {
        return new IccGamutTriangle[]
        {
            new IccGamutTriangle { First = 0, Second = 2, Third = 1 },
            new IccGamutTriangle { First = 0, Second = 1, Third = 3 },
            new IccGamutTriangle { First = 1, Second = 2, Third = 3 },
            new IccGamutTriangle { First = 2, Second = 0, Third = 3 }
        };
    }

    private static IccGamutVertex CreateVertex(double x, double y, double z)
    {
        return new IccGamutVertex
        {
            PcsColor = new IccColorVector
            {
                Components = new double[] { x, y, z },
                ColorSpace = IccColorSpaceKind.Xyz,
                ColorSpaceSignature = IccColorSpaceSignatures.Xyz
            },
            DeviceColor = new IccColorVector()
        };
    }
}
