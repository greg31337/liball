namespace avif.Tests;

public class Av1EncoderQualityTests
{
    [Test]
    public void ConstantQualityMapsMonotonicallyAcrossTheAv1QuantizerRange()
    {
        Av1EncoderOptions bestOptions = new Av1EncoderOptions();
        bestOptions.Quality = 100;
        Av1EncoderOptions middleOptions = new Av1EncoderOptions();
        middleOptions.Quality = 50;
        Av1EncoderOptions lowestOptions = new Av1EncoderOptions();
        lowestOptions.Quality = 0;

        Av1EncoderQualityDecision best = Av1EncoderQualityResolver.Resolve(
            bestOptions,
            System.Array.Empty<Av1EncodedSizeTrial>());
        Av1EncoderQualityDecision middle = Av1EncoderQualityResolver.Resolve(
            middleOptions,
            System.Array.Empty<Av1EncodedSizeTrial>());
        Av1EncoderQualityDecision lowest = Av1EncoderQualityResolver.Resolve(
            lowestOptions,
            System.Array.Empty<Av1EncodedSizeTrial>());

        Assert.Multiple(() =>
        {
            Assert.That(best.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(best.QIndex, Is.Zero);
            Assert.That(best.Lossless, Is.True);
            Assert.That(middle.QIndex, Is.EqualTo(128));
            Assert.That(middle.Lossless, Is.False);
            Assert.That(lowest.QIndex, Is.EqualTo(255));
        });
    }

    [Test]
    public void ExplicitQuantizerAndLosslessModesConfigureTransformEncoding()
    {
        Av1EncoderOptions quantizerOptions = new Av1EncoderOptions();
        quantizerOptions.Mode = AvifEncoderMode.Quantizer;
        quantizerOptions.Quantizer = 173;
        Av1EncoderQualityDecision quantizer =
            Av1EncoderQualityResolver.Resolve(
                quantizerOptions,
                System.Array.Empty<Av1EncodedSizeTrial>());

        int[] residual = new int[]
        {
            -7, 3, 12, -1,
            5, -9, 0, 4,
            17, 2, -6, 8,
            -3, 11, 1, -13
        };
        Av1EncoderOptions losslessOptions = new Av1EncoderOptions();
        losslessOptions.Mode = AvifEncoderMode.Lossless;
        Av1EncoderQualityDecision lossless =
            Av1EncoderQualityResolver.Resolve(
                losslessOptions,
                System.Array.Empty<Av1EncodedSizeTrial>());
        Av1TransformBlockEncodeRequest request =
            new Av1TransformBlockEncodeRequest();
        request.QIndex = 200;
        request.DcDelta = 12;
        request.AcDelta = -8;
        request.UseQuantizerMatrix = true;
        request.Residual = residual;
        lossless.ApplyTo(request);

        Av1TransformBlockEncodeResult encoded =
            Av1TransformBlockEncoder.Encode(request);

        Assert.Multiple(() =>
        {
            Assert.That(quantizer.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(quantizer.QIndex, Is.EqualTo(173));
            Assert.That(quantizer.Lossless, Is.False);
            Assert.That(lossless.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(request.QIndex, Is.Zero);
            Assert.That(request.Lossless, Is.True);
            Assert.That(request.DcDelta, Is.Zero);
            Assert.That(request.AcDelta, Is.Zero);
            Assert.That(request.UseQuantizerMatrix, Is.False);
            Assert.That(encoded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.ReconstructedResidual, Is.EqualTo(residual));
        });
    }

    [Test]
    public void TargetSizeSearchUsesMeasuredBytesAndConvergesWithinOnePercent()
    {
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.Mode = AvifEncoderMode.TargetSize;
        options.TargetSizeBytes = 1000;

        Av1EncoderQualityDecision first = Av1EncoderQualityResolver.Resolve(
            options,
            System.Array.Empty<Av1EncodedSizeTrial>());
        Av1EncoderQualityDecision second = Av1EncoderQualityResolver.Resolve(
            options,
            new Av1EncodedSizeTrial[]
            {
                new Av1EncodedSizeTrial(128, 1300)
            });
        Av1EncoderQualityDecision bracketed =
            Av1EncoderQualityResolver.Resolve(
                options,
                new Av1EncodedSizeTrial[]
                {
                    new Av1EncodedSizeTrial(128, 1300),
                    new Av1EncodedSizeTrial(192, 700)
                });
        Av1EncoderQualityDecision completed =
            Av1EncoderQualityResolver.Resolve(
                options,
                new Av1EncodedSizeTrial[]
                {
                    new Av1EncodedSizeTrial(128, 1300),
                    new Av1EncodedSizeTrial(192, 700),
                    new Av1EncodedSizeTrial(160, 995)
                });

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.NeedsSizeTrial, Is.True);
            Assert.That(first.QIndex, Is.EqualTo(128));
            Assert.That(second.NeedsSizeTrial, Is.True);
            Assert.That(second.QIndex, Is.EqualTo(192));
            Assert.That(bracketed.NeedsSizeTrial, Is.True);
            Assert.That(bracketed.QIndex, Is.EqualTo(160));
            Assert.That(completed.NeedsSizeTrial, Is.False);
            Assert.That(completed.TargetMet, Is.True);
            Assert.That(completed.QIndex, Is.EqualTo(160));
            Assert.That(completed.SelectedSizeBytes, Is.EqualTo(995));
        });
    }

    [Test]
    public void AlphaAndGainMapQualitiesAreResolvedIndependentlyFromColor()
    {
        AvifEncoderOptions options = new AvifEncoderOptions();
        options.Mode = AvifEncoderMode.Quantizer;
        options.Quantizer = 42;
        options.AlphaQuality = 100;
        options.GainMapQuality = 20;

        AvifEncoderQualityPlan plan = AvifEncoderQualityResolver.Resolve(
            options,
            true,
            true,
            System.Array.Empty<Av1EncodedSizeTrial>());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((plan.Color ?? throw new InvalidOperationException("Invalid internal state.")).QIndex, Is.EqualTo(42));
            Assert.That(plan.Color.Lossless, Is.False);
            Assert.That(plan.HasAlpha, Is.True);
            Assert.That((plan.Alpha ?? throw new InvalidOperationException("Invalid internal state.")).QIndex, Is.Zero);
            Assert.That(plan.Alpha.Lossless, Is.True);
            Assert.That(plan.HasGainMap, Is.True);
            Assert.That((plan.GainMap ?? throw new InvalidOperationException("Invalid internal state.")).QIndex, Is.EqualTo(204));
            Assert.That(plan.GainMap.Lossless, Is.False);
        });
    }

    [Test]
    public void InvalidQualityAndTargetMeasurementsAreRejected()
    {
        Av1EncoderOptions qualityOptions = new Av1EncoderOptions();
        qualityOptions.Quality = 101;
        Av1EncoderQualityDecision quality = Av1EncoderQualityResolver.Resolve(
            qualityOptions,
            System.Array.Empty<Av1EncodedSizeTrial>());

        Av1EncoderOptions targetOptions = new Av1EncoderOptions();
        targetOptions.Mode = AvifEncoderMode.TargetSize;
        targetOptions.TargetSizeBytes = 1000;
        Av1EncoderQualityDecision target = Av1EncoderQualityResolver.Resolve(
            targetOptions,
            new Av1EncodedSizeTrial[]
            {
                new Av1EncodedSizeTrial(100, 1100),
                new Av1EncodedSizeTrial(100, 900)
            });

        Assert.Multiple(() =>
        {
            Assert.That(quality.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(target.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }
}
