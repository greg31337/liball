namespace avif.Tests;

public class H273ColorTests
{
    [Test]
    public void PublicColorSignalingUsesH273CodePointsAndStableDefaults()
    {
        AvifColorInfo defaults = AvifColorInfo.Create();
        AvifColorInfo hdr = AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.ICtCp,
            AvifRange.Full);

        Assert.Multiple(() =>
        {
            Assert.That((int)AvifColorPrimaries.Bt709, Is.EqualTo(1));
            Assert.That((int)AvifColorPrimaries.Bt2020, Is.EqualTo(9));
            Assert.That((int)AvifTransferCharacteristics.Srgb, Is.EqualTo(13));
            Assert.That((int)AvifTransferCharacteristics.Smpte2084, Is.EqualTo(16));
            Assert.That((int)AvifTransferCharacteristics.Hlg, Is.EqualTo(18));
            Assert.That((int)AvifMatrixCoefficients.Identity, Is.Zero);
            Assert.That((int)AvifMatrixCoefficients.YCgCo, Is.EqualTo(8));
            Assert.That((int)AvifMatrixCoefficients.ICtCp, Is.EqualTo(14));
            Assert.That(defaults.ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Unspecified));
            Assert.That(defaults.TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Unspecified));
            Assert.That(defaults.MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.Unspecified));
            Assert.That(defaults.Range, Is.EqualTo(AvifRange.Limited));
            Assert.That(hdr.ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt2020));
            Assert.That(hdr.TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Smpte2084));
            Assert.That(hdr.MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.ICtCp));
            Assert.That(hdr.Range, Is.EqualTo(AvifRange.Full));
        });
    }

    [Test]
    public void ImagesCopyColorSignalingAcrossViewsAndIndependentCopies()
    {
        AvifImage image = AvifImage.Create(
            4, 4, 10, AvifChromaFormat.Yuv444, false);
        AvifColorInfo signaling = AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.ICtCp,
            AvifRange.Limited);

        AvifResultCode setResult = image.SetColorInfo(signaling);
        AvifImage view = image.CreateView(1, 1, 2, 2);
        AvifImage copy = image.CreateCopy();
        AvifColorInfo? returned = image.GetColorInfo();
        (returned ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries = AvifColorPrimaries.Bt709;
        AvifColorInfo invalid = AvifColorInfo.Create(
            (AvifColorPrimaries)256,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full);

        Assert.Multiple(() =>
        {
            Assert.That(setResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(image.GetRange(), Is.EqualTo(AvifRange.Limited));
            Assert.That((image.GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt2020));
            Assert.That((view.GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Smpte2084));
            Assert.That((copy.GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.ICtCp));
            Assert.That((copy.GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).Range,
                Is.EqualTo(AvifRange.Limited));
            Assert.That(image.SetColorInfo(invalid),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
        Assert.That(image.SetColorInfo(signaling),
            Is.EqualTo(AvifResultCode.Closed));
        view.Close();
        copy.Close();
    }

    [Test]
    public void StandardTransferFunctionsMatchReferencePoints()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                TransferFunctions.FromLinear(
                    0.0031308, AvifTransferCharacteristics.Srgb),
                Is.EqualTo(0.040449936).Within(1e-12));
            Assert.That(
                TransferFunctions.ToLinear(
                    0.04045, AvifTransferCharacteristics.Srgb),
                Is.EqualTo(0.0031308049535603713).Within(1e-15));
            Assert.That(
                TransferFunctions.FromLinear(
                    0.018053968510807,
                    AvifTransferCharacteristics.Bt709),
                Is.EqualTo(0.0812428582986315).Within(1e-14));
            Assert.That(
                TransferFunctions.FromLinear(
                    0.01, AvifTransferCharacteristics.Smpte2084),
                Is.EqualTo(0.508078421517399).Within(1e-12));
            Assert.That(
                TransferFunctions.ToLinear(
                    0.508078421517399,
                    AvifTransferCharacteristics.Smpte2084),
                Is.EqualTo(0.01).Within(1e-12));
            Assert.That(
                TransferFunctions.FromLinear(
                    1.0 / 12, AvifTransferCharacteristics.Hlg),
                Is.EqualTo(0.5).Within(1e-15));
            Assert.That(
                TransferFunctions.ToLinear(
                    0.5, AvifTransferCharacteristics.Hlg),
                Is.EqualTo(1.0 / 12).Within(1e-15));
            Assert.That(
                TransferFunctions.FromLinear(
                    1, AvifTransferCharacteristics.Smpte428),
                Is.EqualTo(Math.Pow(48.0 / 52.37, 1.0 / 2.6)).Within(1e-15));
        });
    }

    [Test]
    public void EveryDefinedTransferRoundTripsLinearLight()
    {
        AvifTransferCharacteristics[] transfers = new AvifTransferCharacteristics[]
        {
            AvifTransferCharacteristics.Bt709,
            AvifTransferCharacteristics.Gamma22,
            AvifTransferCharacteristics.Gamma28,
            AvifTransferCharacteristics.Bt601,
            AvifTransferCharacteristics.Smpte240,
            AvifTransferCharacteristics.Linear,
            AvifTransferCharacteristics.Logarithmic100,
            AvifTransferCharacteristics.Logarithmic316,
            AvifTransferCharacteristics.Iec61966_2_4,
            AvifTransferCharacteristics.Bt1361,
            AvifTransferCharacteristics.Srgb,
            AvifTransferCharacteristics.Bt2020TenBit,
            AvifTransferCharacteristics.Bt2020TwelveBit,
            AvifTransferCharacteristics.Smpte2084,
            AvifTransferCharacteristics.Smpte428,
            AvifTransferCharacteristics.Hlg
        };

        foreach (AvifTransferCharacteristics transfer in transfers)
        {
            double encoded = TransferFunctions.FromLinear(0.18, transfer);
            double decoded = TransferFunctions.ToLinear(encoded, transfer);
            Assert.Multiple(() =>
            {
                Assert.That(TransferFunctions.IsSupported(transfer), Is.True);
                Assert.That(double.IsFinite(encoded), Is.True);
                Assert.That(decoded, Is.EqualTo(0.18).Within(2e-12),
                    transfer.ToString());
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                TransferFunctions.ToLinear(
                    -0.25, AvifTransferCharacteristics.Iec61966_2_4),
                Is.LessThan(0));
            Assert.That(
                TransferFunctions.ToLinear(
                    TransferFunctions.FromLinear(
                        -0.1, AvifTransferCharacteristics.Bt1361),
                    AvifTransferCharacteristics.Bt1361),
                Is.EqualTo(-0.1).Within(1e-12));
            Assert.That(
                TransferFunctions.IsSupported(
                    AvifTransferCharacteristics.Unspecified),
                Is.False);
            Assert.That(
                TransferFunctions.ToLinear(
                    0.5, AvifTransferCharacteristics.Unspecified),
                Is.NaN);
        });
    }

    [Test]
    public void PrimaryMatricesMatchReferenceRgbToXyzValues()
    {
        ColorMatrix? bt709 = ColorTables.GetRgbToXyz(
            AvifColorPrimaries.Bt709);
        ColorVector white = (bt709 ?? throw new InvalidOperationException("Invalid internal state.")).Transform(new ColorVector(1, 1, 1));
        ColorVector? redIn2020 = ColorTables.ConvertPrimaries(
            new ColorVector(1, 0, 0),
            AvifColorPrimaries.Bt709,
            AvifColorPrimaries.Bt2020);

        Assert.Multiple(() =>
        {
            Assert.That(bt709.Values[0],
                Is.EqualTo(0.4123907992659595).Within(1e-12));
            Assert.That(bt709.Values[4],
                Is.EqualTo(0.7151686787677559).Within(1e-12));
            Assert.That(bt709.Values[8],
                Is.EqualTo(0.9505321522496607).Within(1e-12));
            Assert.That(white.First,
                Is.EqualTo(0.9504559270516716).Within(1e-12));
            Assert.That(white.Second, Is.EqualTo(1).Within(1e-12));
            Assert.That(white.Third,
                Is.EqualTo(1.0890577507598784).Within(1e-12));
            AssertVector(
                redIn2020,
                0.627403895934699,
                0.069097289358232,
                0.01639143887515,
                1e-11);
            Assert.That(
                ColorTables.GetRgbToXyz(AvifColorPrimaries.Unspecified),
                Is.Null);
        });
    }

    [Test]
    public void PrimaryConversionRoundTripsAcrossDifferentWhitePoints()
    {
        ColorVector source = new ColorVector(0.12, 0.45, 0.83);
        ColorVector? dci = ColorTables.ConvertPrimaries(
            source,
            AvifColorPrimaries.Bt709,
            AvifColorPrimaries.Smpte431);
        ColorVector? restored = ColorTables.ConvertPrimaries(
            dci,
            AvifColorPrimaries.Smpte431,
            AvifColorPrimaries.Bt709);

        AssertVector(restored, source.First, source.Second, source.Third, 1e-11);
    }

    [Test]
    public void EveryDefinedPrimaryTablePreservesItsReferenceWhite()
    {
        AvifColorPrimaries[] primariesValues = new AvifColorPrimaries[]
        {
            AvifColorPrimaries.Bt709,
            AvifColorPrimaries.Bt470M,
            AvifColorPrimaries.Bt470Bg,
            AvifColorPrimaries.Bt601,
            AvifColorPrimaries.Smpte240,
            AvifColorPrimaries.GenericFilm,
            AvifColorPrimaries.Bt2020,
            AvifColorPrimaries.Smpte428,
            AvifColorPrimaries.Smpte431,
            AvifColorPrimaries.Smpte432,
            AvifColorPrimaries.EbuTech3213
        };

        foreach (AvifColorPrimaries primaries in primariesValues)
        {
            ColorMatrix? matrix = ColorTables.GetRgbToXyz(primaries);
            ColorVector? expectedWhite = ColorTables.GetWhitePoint(primaries);
            ColorVector actualWhite = (matrix ?? throw new InvalidOperationException("Invalid internal state.")).Transform(new ColorVector(1, 1, 1));
            ColorMatrix? inverse = matrix.Invert();
            ColorVector restored = (inverse ?? throw new InvalidOperationException("Invalid internal state.")).Transform(
                matrix.Transform(new ColorVector(0.13, 0.47, 0.81)));

            Assert.Multiple(() =>
            {
                Assert.That(ColorTables.IsSupported(primaries), Is.True);
                AssertVector(
                    actualWhite,
                    (expectedWhite ?? throw new InvalidOperationException("Invalid internal state.")).First,
                    expectedWhite.Second,
                    expectedWhite.Third,
                    2e-12);
                AssertVector(restored, 0.13, 0.47, 0.81, 2e-12);
            });
        }
    }

    [Test]
    public void IdentityYcgcoAndStandardLumaMatricesUseTheirDefinedEquations()
    {
        ColorVector linear = new ColorVector(0.2, 0.4, 0.8);
        ColorVector? identity = ColorMatrixCoefficients.LinearRgbToComponents(
            linear,
            AvifMatrixCoefficients.Identity,
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear);
        ColorVector? ycgco = ColorMatrixCoefficients.LinearRgbToComponents(
            linear,
            AvifMatrixCoefficients.YCgCo,
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear);
        ColorVector? bt709 = ColorMatrixCoefficients.LinearRgbToComponents(
            new ColorVector(1, 0, 0),
            AvifMatrixCoefficients.Bt709,
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Linear);
        ColorVector? bt601 = ColorMatrixCoefficients.LinearRgbToComponents(
            new ColorVector(1, 0, 0),
            AvifMatrixCoefficients.Bt601,
            AvifColorPrimaries.Bt601,
            AvifTransferCharacteristics.Linear);
        ColorVector? bt2020 = ColorMatrixCoefficients.LinearRgbToComponents(
            new ColorVector(1, 0, 0),
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Linear);

        Assert.Multiple(() =>
        {
            AssertVector(identity, 0.4, 0.8, 0.2, 1e-15);
            AssertVector(ycgco, 0.45, -0.05, -0.3, 1e-15);
            AssertVector(
                bt709,
                0.2126,
                -0.11457210605733996,
                0.5,
                1e-14);
            AssertVector(
                bt601,
                0.299,
                -0.16873589164785552,
                0.5,
                1e-14);
            AssertVector(
                bt2020,
                0.2627,
                -0.13963006271925163,
                0.5,
                1e-14);
        });
    }

    [Test]
    public void AllSupportedMatrixPathsRoundTripToLinearRgb()
    {
        AvifMatrixCoefficients[] matrices = new AvifMatrixCoefficients[]
        {
            AvifMatrixCoefficients.Identity,
            AvifMatrixCoefficients.Bt709,
            AvifMatrixCoefficients.Fcc,
            AvifMatrixCoefficients.Bt470Bg,
            AvifMatrixCoefficients.Bt601,
            AvifMatrixCoefficients.Smpte240,
            AvifMatrixCoefficients.YCgCo,
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifMatrixCoefficients.Bt2020ConstantLuminance,
            AvifMatrixCoefficients.Smpte2085,
            AvifMatrixCoefficients.ChromaDerivedNonConstantLuminance,
            AvifMatrixCoefficients.ChromaDerivedConstantLuminance,
            AvifMatrixCoefficients.ICtCp,
            AvifMatrixCoefficients.IptC2
        };
        ColorVector source = new ColorVector(0.12, 0.45, 0.83);

        foreach (AvifMatrixCoefficients matrix in matrices)
        {
            AvifColorPrimaries primaries = matrix == AvifMatrixCoefficients.Bt2020ConstantLuminance
                || matrix == AvifMatrixCoefficients.Bt2020NonConstantLuminance
                || matrix == AvifMatrixCoefficients.ICtCp
                || matrix == AvifMatrixCoefficients.IptC2
                ? AvifColorPrimaries.Bt2020
                : AvifColorPrimaries.Bt709;
            AvifTransferCharacteristics transfer = matrix == AvifMatrixCoefficients.ICtCp
                || matrix == AvifMatrixCoefficients.IptC2
                ? AvifTransferCharacteristics.Smpte2084
                : AvifTransferCharacteristics.Srgb;
            ColorVector? components = ColorMatrixCoefficients.LinearRgbToComponents(
                source, matrix, primaries, transfer);
            ColorVector? restored = ColorMatrixCoefficients.ComponentsToLinearRgb(
                components, matrix, primaries, transfer);

            Assert.Multiple(() =>
            {
                Assert.That(
                    ColorMatrixCoefficients.IsSupported(
                        matrix, primaries, transfer),
                    Is.True,
                    matrix.ToString());
                AssertVector(
                    restored,
                    source.First,
                    source.Second,
                    source.Third,
                    2e-10);
            });
        }
    }

    [TestCase(AvifTransferCharacteristics.Smpte2084)]
    [TestCase(AvifTransferCharacteristics.Hlg)]
    public void IctcpNeutralAxisHasZeroChromaAndRoundTrips(
        AvifTransferCharacteristics transfer)
    {
        ColorVector gray = new ColorVector(0.18, 0.18, 0.18);
        ColorVector? ictcp = ColorMatrixCoefficients.LinearRgbToComponents(
            gray,
            AvifMatrixCoefficients.ICtCp,
            AvifColorPrimaries.Bt2020,
            transfer);
        ColorVector? restored = ColorMatrixCoefficients.ComponentsToLinearRgb(
            ictcp,
            AvifMatrixCoefficients.ICtCp,
            AvifColorPrimaries.Bt2020,
            transfer);

        Assert.Multiple(() =>
        {
            Assert.That((ictcp ?? throw new InvalidOperationException("Invalid internal state.")).First,
                Is.EqualTo(TransferFunctions.FromLinear(0.18, transfer))
                    .Within(2e-14));
            Assert.That(ictcp.Second, Is.EqualTo(0).Within(2e-14));
            Assert.That(ictcp.Third, Is.EqualTo(0).Within(2e-14));
            AssertVector(restored, 0.18, 0.18, 0.18, 2e-10);
        });
    }

    [Test]
    public void PqIctcpMatchesBt2100RedReferenceVector()
    {
        ColorVector? ictcp = ColorMatrixCoefficients.LinearRgbToComponents(
            new ColorVector(1, 0, 0),
            AvifMatrixCoefficients.ICtCp,
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084);

        AssertVector(
            ictcp,
            0.856694694763412,
            -0.198217945501596,
            0.4574999435163032,
            2e-13);
    }

    [Test]
    public void UnsupportedColorSignalsAreRejectedWithoutInventingMath()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ColorMatrixCoefficients.IsSupported(
                    AvifMatrixCoefficients.Unspecified,
                    AvifColorPrimaries.Bt709,
                    AvifTransferCharacteristics.Srgb),
                Is.False);
            Assert.That(
                ColorMatrixCoefficients.IsSupported(
                    AvifMatrixCoefficients.ChromaDerivedNonConstantLuminance,
                    AvifColorPrimaries.Unspecified,
                    AvifTransferCharacteristics.Srgb),
                Is.False);
            Assert.That(
                ColorMatrixCoefficients.IsSupported(
                    AvifMatrixCoefficients.YCgCoRe,
                    AvifColorPrimaries.Bt709,
                    AvifTransferCharacteristics.Srgb),
                Is.False);
            Assert.That(
                ColorMatrixCoefficients.LinearRgbToComponents(
                    new ColorVector(0, 0, 0),
                    AvifMatrixCoefficients.Unspecified,
                    AvifColorPrimaries.Bt709,
                    AvifTransferCharacteristics.Srgb),
                Is.Null);
        });
    }

    static void AssertVector(
        ColorVector? actual,
        double first,
        double second,
        double third,
        double tolerance)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That((actual ?? throw new InvalidOperationException("Invalid internal state.")).First, Is.EqualTo(first).Within(tolerance));
            Assert.That(actual.Second, Is.EqualTo(second).Within(tolerance));
            Assert.That(actual.Third, Is.EqualTo(third).Within(tolerance));
        });
    }
}
