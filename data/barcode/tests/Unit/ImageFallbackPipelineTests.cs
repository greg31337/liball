namespace barcode.Tests;

public class ImageFallbackPipelineTests
{
    [Test]
    public void AutomaticSchedulesExpandInAStableEffortOrder()
    {
        ImagePipelineStage[] fast = ImageEffortSchedule.Create(
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Automatic,
            BarcodeLocalizationMode.Automatic);
        ImagePipelineStage[] balanced = ImageEffortSchedule.Create(
            BarcodeEffort.Balanced,
            BarcodeBinarizationMode.Automatic,
            BarcodeLocalizationMode.Automatic);
        ImagePipelineStage[] thorough = ImageEffortSchedule.Create(
            BarcodeEffort.Thorough,
            BarcodeBinarizationMode.Automatic,
            BarcodeLocalizationMode.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(fast, Has.Length.EqualTo(3));
            Assert.That(fast[0].Localization, Is.EqualTo(BarcodeLocalizationMode.Direct));
            Assert.That(fast[1].Localization, Is.EqualTo(BarcodeLocalizationMode.ConnectedBlocks));
            Assert.That(fast[2].Localization, Is.EqualTo(BarcodeLocalizationMode.FinderPatterns));
            Assert.That(fast[0].Binarization, Is.EqualTo(BarcodeBinarizationMode.Otsu));
            Assert.That(balanced, Has.Length.EqualTo(4));
            Assert.That(balanced[3].Preprocessing, Is.EqualTo(ImagePreprocessingKind.Smoothed));
            Assert.That(balanced[3].Binarization, Is.EqualTo(BarcodeBinarizationMode.Hybrid));
            Assert.That(thorough, Has.Length.EqualTo(8));
            Assert.That(thorough[4].Binarization, Is.EqualTo(BarcodeBinarizationMode.Row));
            Assert.That(thorough[6].Preprocessing, Is.EqualTo(ImagePreprocessingKind.Normalized));
            Assert.That(thorough[7].Localization, Is.EqualTo(BarcodeLocalizationMode.FinderPatterns));
        });

        int index = 0;
        while (index < thorough.Length)
        {
            Assert.That(thorough[index].Index, Is.EqualTo(index));
            index = index + 1;
        }
    }

    [Test]
    public void ExplicitModesStayFixedWhileThoroughEffortAddsPreprocessing()
    {
        ImagePipelineStage[] stages = ImageEffortSchedule.Create(
            BarcodeEffort.Thorough,
            BarcodeBinarizationMode.Global,
            BarcodeLocalizationMode.Lines);

        Assert.That(stages, Has.Length.EqualTo(4));
        Assert.That(
            stages,
            Has.All.Matches<ImagePipelineStage>(stage =>
                stage.Binarization == BarcodeBinarizationMode.Global &&
                stage.Localization == BarcodeLocalizationMode.Lines));
        Assert.That(
            stages,
            Has.Exactly(1).Matches<ImagePipelineStage>(stage =>
                stage.Preprocessing == ImagePreprocessingKind.Original));
        Assert.That(
            stages,
            Has.Exactly(1).Matches<ImagePipelineStage>(stage =>
                stage.Preprocessing == ImagePreprocessingKind.Smoothed));
        Assert.That(
            stages,
            Has.Exactly(1).Matches<ImagePipelineStage>(stage =>
                stage.Preprocessing == ImagePreprocessingKind.Sharpened));
        Assert.That(
            stages,
            Has.Exactly(1).Matches<ImagePipelineStage>(stage =>
                stage.Preprocessing == ImagePreprocessingKind.Normalized));
    }

    [Test]
    public void ExpectedCountStopsAfterTheFirstSuccessfulDirectStage()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            new byte[] { 65, 66, 67, 49, 50, 51 },
            null);
        ManagedGrayImage image = DrawLinearGrayImage(encoded.Modules, 20);

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Thorough,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Automatic,
            BarcodePolarity.DarkOnLight,
            1,
            10,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.StagesExecuted, Is.EqualTo(1));
            Assert.That(result.Stages[0].Stage.Localization,
                Is.EqualTo(BarcodeLocalizationMode.Direct));
            Assert.That(result.Stages[0].StoppedAfterStage, Is.True);
            Assert.That(result.ExpectedCountReached, Is.True);
            Assert.That(result.MaximumCountReached, Is.False);
            Assert.That(result.Candidates[0].StageIndex, Is.Zero);
            Assert.That(result.Candidates[0].Localization.Candidate.Format,
                Is.EqualTo(BarcodeFormat.Code128));
        });
    }

    [Test]
    public void PipelineCollectsBoundedTransformThresholdConfidenceAndCoordinates()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            new byte[] { 68, 73, 65, 71 },
            null);
        ManagedGrayImage image = DrawLinearGrayImage(encoded.Modules, 20);

        ImageFallbackResult complete = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Direct,
            BarcodePolarity.DarkOnLight,
            1,
            5,
            10);
        ImageFallbackResult bounded = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Direct,
            BarcodePolarity.DarkOnLight,
            1,
            5,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Diagnostics, Has.Length.EqualTo(4));
            Assert.That(complete.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.PreprocessOriginal));
            Assert.That(complete.Diagnostics[1].Code,
                Is.EqualTo(DiagnosticCodes.BinarizationThreshold));
            Assert.That(complete.Diagnostics[2].Code,
                Is.EqualTo(DiagnosticCodes.CandidateConfidence));
            Assert.That(complete.Diagnostics[3].Code,
                Is.EqualTo(DiagnosticCodes.CandidateCoordinates));
            Assert.That(complete.Diagnostics[3].Location, Is.Not.Null);
            Assert.That(complete.Diagnostics[3].Region, Is.Not.Null);
            Assert.That(bounded.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(bounded.DroppedDiagnosticCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void EmptyLocalizationRecordsCandidateRejection()
    {
        ManagedGrayImage image = CreateGrayImage(12, 8, 255);

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Direct,
            BarcodePolarity.DarkOnLight,
            0,
            5,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(3));
            Assert.That(result.Diagnostics[2].Code,
                Is.EqualTo(DiagnosticCodes.CandidateNotFound));
            Assert.That(result.Diagnostics[2].Value, Is.EqualTo(1));
        });
    }

    [Test]
    public void AutomaticLocalizationFallsThroughToFinderPatterns()
    {
        ManagedGrayImage image = DrawDataMatrixGrayImage("FALLBACK");

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.DataMatrix },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Automatic,
            BarcodePolarity.DarkOnLight,
            1,
            5,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.StagesExecuted, Is.EqualTo(3));
            Assert.That(result.Stages[0].LocatedCount, Is.Zero);
            Assert.That(result.Stages[1].LocatedCount, Is.Zero);
            Assert.That(result.Stages[2].Stage.Localization,
                Is.EqualTo(BarcodeLocalizationMode.FinderPatterns));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].StageIndex, Is.EqualTo(2));
            Assert.That(result.Candidates[0].Localization.Kind,
                Is.EqualTo(CommonCandidateKind.DataMatrix));
            Assert.That(result.ExpectedCountReached, Is.True);
        });
    }

    [Test]
    public void NormalizedBinaryCandidatesRetainTheInputPolarityInResults()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            new byte[] { 76, 73, 71, 72, 84 },
            null);
        ManagedGrayImage image = DrawLinearGrayImage(encoded.Modules, 20);
        InvertGrayImage(image);

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.Direct,
            BarcodePolarity.LightOnDark,
            1,
            5,
            0);

        Assert.That(result.Candidates, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.Stages[0].ActualPolarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(result.Candidates[0].Localization.Candidate.Polarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(result.Candidates[0].Score.Candidate.Polarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
        });
    }

    [Test]
    public void MaximumCountIsAHardLimitEvenWhenAStageFindsMore()
    {
        ManagedGrayImage image = CreateGrayImage(34, 16, 240);
        DrawBars(image, 3, 3, 11);
        DrawBars(image, 21, 3, 11);

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodeEffort.Fast,
            BarcodeBinarizationMode.Otsu,
            BarcodeLocalizationMode.ConnectedBlocks,
            BarcodePolarity.DarkOnLight,
            0,
            1,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.StagesExecuted, Is.EqualTo(1));
            Assert.That(result.Stages[0].LocatedCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Stages[0].AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.ExpectedCountReached, Is.False);
            Assert.That(result.MaximumCountReached, Is.True);
            Assert.That(result.Stages[0].StoppedAfterStage, Is.True);
        });
    }

    [Test]
    public void ThoroughFallbackSuppressesTheSamePhysicalCandidateAcrossStages()
    {
        ManagedGrayImage image = DrawDataMatrixGrayImage("REPEAT");

        ImageFallbackResult result = ImageFallbackPipeline.Execute(
            image,
            new BarcodeFormat[] { BarcodeFormat.DataMatrix },
            BarcodeEffort.Thorough,
            BarcodeBinarizationMode.Automatic,
            BarcodeLocalizationMode.Automatic,
            BarcodePolarity.DarkOnLight,
            0,
            10,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.StagesExecuted, Is.EqualTo(8));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.DuplicateCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.ConflictCount, Is.Zero);
            Assert.That(result.MaximumCountReached, Is.False);
            Assert.That(
                HasDiagnosticCode(result.Diagnostics, DiagnosticCodes.CandidateDuplicate),
                Is.True);
        });
    }

    [Test]
    public void InvalidScheduleValuesAndNegativeLimitsAreRejected()
    {
        ManagedGrayImage image = CreateGrayImage(4, 4, 255);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageEffortSchedule.Create(
                    (BarcodeEffort)999,
                    BarcodeBinarizationMode.Automatic,
                    BarcodeLocalizationMode.Automatic),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageEffortSchedule.Create(
                    BarcodeEffort.Fast,
                    (BarcodeBinarizationMode)999,
                    BarcodeLocalizationMode.Automatic),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageEffortSchedule.Create(
                    BarcodeEffort.Fast,
                    BarcodeBinarizationMode.Automatic,
                    (BarcodeLocalizationMode)999),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFallbackPipeline.Execute(
                    image,
                    null,
                    BarcodeEffort.Fast,
                    BarcodeBinarizationMode.Otsu,
                    BarcodeLocalizationMode.Direct,
                    BarcodePolarity.DarkOnLight,
                    -1,
                    1,
                    0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFallbackPipeline.Execute(
                    image,
                    null,
                    BarcodeEffort.Fast,
                    BarcodeBinarizationMode.Otsu,
                    BarcodeLocalizationMode.Direct,
                    BarcodePolarity.DarkOnLight,
                    0,
                    -1,
                    0),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static ManagedGrayImage DrawLinearGrayImage(byte[] modules, int barHeight)
    {
        ManagedGrayImage image = CreateGrayImage(modules.Length + 20, barHeight + 4, 240);
        int y = 2;
        while (y < barHeight + 2)
        {
            int x = 0;
            while (x < modules.Length)
            {
                if (modules[x] != 0)
                {
                    image.WriteSample(x + 10, y, 20);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static ManagedGrayImage DrawDataMatrixGrayImage(string text)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(3);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = data;
        DataMatrixEncodingResult encoded = DataMatrixEncoder.Encode(
            data,
            payload,
            options,
            null,
            false);
        ManagedGrayImage image = CreateGrayImage(55, 55, 240);
        int y = 0;
        while (y < encoded.SymbolMatrix.Height)
        {
            int x = 0;
            while (x < encoded.SymbolMatrix.Width)
            {
                if (encoded.SymbolMatrix.GetModule(x, y))
                {
                    FillGrayRectangle(image, 7 + (x * 3), 8 + (y * 3), 3, 3, 20);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static void DrawBars(ManagedGrayImage image, int left, int top, int height)
    {
        FillGrayRectangle(image, left, top, 1, height, 20);
        FillGrayRectangle(image, left + 2, top, 1, height, 20);
        FillGrayRectangle(image, left + 4, top, 1, height, 20);
    }

    static void InvertGrayImage(ManagedGrayImage image)
    {
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                image.WriteSample(x, y, 255 - image.ReadSample(x, y));
                x = x + 1;
            }

            y = y + 1;
        }
    }

    static ManagedGrayImage CreateGrayImage(int width, int height, int value)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                image.WriteSample(x, y, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static void FillGrayRectangle(
        ManagedGrayImage image,
        int left,
        int top,
        int width,
        int height,
        int value)
    {
        int y = top;
        while (y < top + height)
        {
            int x = left;
            while (x < left + width)
            {
                image.WriteSample(x, y, value);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    static bool HasDiagnosticCode(BarcodeDiagnostic[] diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
