namespace barcode.Tests;

public class CommonCandidatePipelineTests
{
    [Test]
    public void EveryFormatFilterRetainsOnlyItsRequestedLocalizationProfile()
    {
        Code128EncodedData linearEncoded = Code128Encoder.EncodeBytes(
            new byte[] { 70, 73, 76, 84, 69, 82 },
            null);
        ModuleMatrix linearImage = DrawLinearSymbol(
            linearEncoded.Modules,
            12);
        BarcodeFormat[] linearFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Code128,
            BarcodeFormat.Gs1128,
            BarcodeFormat.Ean8,
            BarcodeFormat.Ean13,
            BarcodeFormat.UpcA,
            BarcodeFormat.UpcE,
            BarcodeFormat.EanUpcAddon2,
            BarcodeFormat.EanUpcAddon5,
            BarcodeFormat.Code39,
            BarcodeFormat.Itf,
            BarcodeFormat.Itf14,
            BarcodeFormat.Codabar
        };
        int index = 0;
        while (index < linearFormats.Length)
        {
            BarcodeFormat format = linearFormats[index];
            CommonLocalizationCandidate[] found =
                CommonCandidatePipeline.Locate(
                    linearImage,
                    new BarcodeFormat[] { format },
                    BarcodePolarity.DarkOnLight,
                    10,
                    4);

            Assert.That(found, Has.Length.EqualTo(1), format.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(
                    found[0].Candidate.Format,
                    Is.EqualTo(format),
                    format.ToString());
                Assert.That(
                    found[0].Candidate.Dimension,
                    Is.EqualTo(BarcodeDimension.Linear),
                    format.ToString());
                Assert.That(
                    found[0].Family,
                    Is.EqualTo(FormatDispatcher.ResolveCodecFamily(format)),
                    format.ToString());
            });
            index = index + 1;
        }

        DataMatrixEncodingResult dataMatrixEncoded =
            EncodeDataMatrix("FILTER");
        ModuleMatrix dataMatrixImage = new ModuleMatrix(60, 60);
        DrawScaledMatrix(
            dataMatrixImage,
            dataMatrixEncoded.SymbolMatrix,
            7,
            8,
            3);
        BarcodeFormat[] dataMatrixFormats = new BarcodeFormat[]
        {
            BarcodeFormat.DataMatrix,
            BarcodeFormat.Gs1DataMatrix
        };
        index = 0;
        while (index < dataMatrixFormats.Length)
        {
            BarcodeFormat format = dataMatrixFormats[index];
            CommonLocalizationCandidate[] found =
                CommonCandidatePipeline.Locate(
                    dataMatrixImage,
                    new BarcodeFormat[] { format },
                    BarcodePolarity.DarkOnLight,
                    3,
                    3);

            Assert.That(found, Has.Length.EqualTo(1), format.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(
                    found[0].Candidate.Format,
                    Is.EqualTo(format),
                    format.ToString());
                Assert.That(
                    found[0].Candidate.Dimension,
                    Is.EqualTo(BarcodeDimension.Matrix),
                    format.ToString());
                Assert.That(
                    found[0].Family,
                    Is.EqualTo(BarcodeCodecFamily.DataMatrix),
                    format.ToString());
            });
            index = index + 1;
        }

        Pdf417EncodingResult standard = EncodePdf417(Pdf417Variant.Standard);
        ModuleMatrix standardImage = DrawPdf417(standard.Symbol, 2, 10, 9);
        BarcodeFormat[] standardFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Pdf417,
            BarcodeFormat.MacroPdf417
        };
        index = 0;
        while (index < standardFormats.Length)
        {
            BarcodeFormat format = standardFormats[index];
            CommonLocalizationCandidate[] found =
                CommonCandidatePipeline.Locate(
                    standardImage,
                    new BarcodeFormat[] { format },
                    BarcodePolarity.DarkOnLight,
                    3,
                    3);

            Assert.That(found, Has.Length.EqualTo(1), format.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(
                    found[0].Candidate.Format,
                    Is.EqualTo(format),
                    format.ToString());
                Assert.That(
                    found[0].Candidate.Dimension,
                    Is.EqualTo(BarcodeDimension.StackedLinear),
                    format.ToString());
                Assert.That(
                    found[0].Family,
                    Is.EqualTo(BarcodeCodecFamily.Pdf417),
                    format.ToString());
            });
            index = index + 1;
        }

        Pdf417EncodingResult compact = EncodePdf417(Pdf417Variant.Compact);
        ModuleMatrix compactImage = DrawPdf417(compact.Symbol, 2, 10, 9);
        CommonLocalizationCandidate[] compactFound =
            CommonCandidatePipeline.Locate(
                compactImage,
                new BarcodeFormat[] { BarcodeFormat.CompactPdf417 },
                BarcodePolarity.DarkOnLight,
                3,
                3);
        CommonLocalizationCandidate[] automatic =
            CommonCandidatePipeline.Locate(
                linearImage,
                new BarcodeFormat[] { BarcodeFormat.Automatic },
                BarcodePolarity.DarkOnLight,
                10,
                4);

        Assert.That(compactFound, Has.Length.EqualTo(1));
        Assert.That(automatic, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(
                compactFound[0].Candidate.Format,
                Is.EqualTo(BarcodeFormat.CompactPdf417));
            Assert.That(
                compactFound[0].Candidate.Dimension,
                Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(
                automatic[0].Candidate.Format,
                Is.EqualTo(BarcodeFormat.Automatic));
        });
    }

    [Test]
    public void LinearCandidatesUseTheRequestedFormatAndPolarity()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            new byte[] { 65, 66, 67, 49, 50, 51 },
            null);
        ModuleMatrix image = DrawLinearSymbol(encoded.Modules, 12);
        ModuleMatrix inverted = Invert(image);

        CommonLocalizationCandidate[] dark = CommonCandidatePipeline.Locate(
            image,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodePolarity.DarkOnLight,
            10,
            4);
        CommonLocalizationCandidate[] light = CommonCandidatePipeline.Locate(
            inverted,
            new BarcodeFormat[] { BarcodeFormat.Code128 },
            BarcodePolarity.LightOnDark,
            10,
            4);

        Assert.That(dark, Has.Length.EqualTo(1));
        Assert.That(light, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(dark[0].Kind, Is.EqualTo(CommonCandidateKind.Linear));
            Assert.That(dark[0].Family, Is.EqualTo(BarcodeCodecFamily.Code128));
            Assert.That(dark[0].Candidate.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(dark[0].Candidate.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(dark[0].Candidate.Polarity, Is.EqualTo(BarcodePolarity.DarkOnLight));
            Assert.That(dark[0].LinearEvidence, Is.Not.Null);
            Assert.That(dark[0].DataMatrixEvidence, Is.Null);
            Assert.That(dark[0].Pdf417Evidence, Is.Null);
            Assert.That(light[0].Candidate.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(light[0].Candidate.Location!.Bounds!.Width,
                Is.EqualTo(dark[0].Candidate.Location!.Bounds!.Width));
        });
    }

    [Test]
    public void DataMatrixFinderEvidenceFlowsThroughACommonDefensiveCandidate()
    {
        DataMatrixEncodingResult encoded = EncodeDataMatrix("PIPELINE");
        ModuleMatrix image = new ModuleMatrix(55, 55);
        DrawScaledMatrix(image, encoded.SymbolMatrix, 7, 8, 3);

        CommonLocalizationCandidate[] found = CommonCandidatePipeline.Locate(
            image,
            new BarcodeFormat[] { BarcodeFormat.DataMatrix },
            BarcodePolarity.DarkOnLight,
            3,
            3);

        Assert.That(found, Has.Length.EqualTo(1));
        CommonLocalizationCandidate common = found[0];
        Assert.That(common.DataMatrixEvidence, Is.Not.Null);
        double evidenceX = common.DataMatrixEvidence!.Location.TopLeft!.X;
        common.Candidate.Location!.TopLeft!.X = 999.0;

        Assert.Multiple(() =>
        {
            Assert.That(common.Kind, Is.EqualTo(CommonCandidateKind.DataMatrix));
            Assert.That(common.Family, Is.EqualTo(BarcodeCodecFamily.DataMatrix));
            Assert.That(common.Candidate.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(common.Candidate.Dimension, Is.EqualTo(BarcodeDimension.Matrix));
            Assert.That(common.Candidate.Confidence, Is.GreaterThan(0.9));
            Assert.That(common.LinearEvidence, Is.Null);
            Assert.That(common.Pdf417Evidence, Is.Null);
            Assert.That(common.DataMatrixEvidence.Location.TopLeft.X, Is.EqualTo(evidenceX));
        });
    }

    [Test]
    public void Gs1DataMatrixFilterRetainsFinderEvidenceWithRequestedProfile()
    {
        DataMatrixEncodingResult encoded = EncodeDataMatrix("GS1");
        ModuleMatrix image = new ModuleMatrix(50, 50);
        DrawScaledMatrix(image, encoded.SymbolMatrix, 6, 6, 3);

        CommonLocalizationCandidate[] found = CommonCandidatePipeline.Locate(
            image,
            new BarcodeFormat[] { BarcodeFormat.Gs1DataMatrix },
            BarcodePolarity.DarkOnLight,
            3,
            3);

        Assert.That(found, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(found[0].Candidate.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(found[0].DataMatrixEvidence, Is.Not.Null);
            Assert.That(found[0].DataMatrixEvidence!.Candidate.Format,
                Is.EqualTo(BarcodeFormat.DataMatrix));
        });
    }

    [Test]
    public void StandardAndCompactPdf417UseTheirSpecializedFinderPaths()
    {
        Pdf417EncodingResult standard = EncodePdf417(Pdf417Variant.Standard);
        Pdf417EncodingResult compact = EncodePdf417(Pdf417Variant.Compact);
        ModuleMatrix standardImage = DrawPdf417(standard.Symbol, 2, 10, 9);
        ModuleMatrix compactImage = DrawPdf417(compact.Symbol, 2, 10, 9);

        CommonLocalizationCandidate[] standardFound = CommonCandidatePipeline.Locate(
            standardImage,
            new BarcodeFormat[] { BarcodeFormat.Pdf417 },
            BarcodePolarity.DarkOnLight,
            3,
            3);
        CommonLocalizationCandidate[] compactFound = CommonCandidatePipeline.Locate(
            compactImage,
            new BarcodeFormat[] { BarcodeFormat.CompactPdf417 },
            BarcodePolarity.DarkOnLight,
            3,
            3);

        Assert.That(standardFound, Has.Length.EqualTo(1));
        Assert.That(compactFound, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(standardFound[0].Kind, Is.EqualTo(CommonCandidateKind.Pdf417));
            Assert.That(standardFound[0].Family, Is.EqualTo(BarcodeCodecFamily.Pdf417));
            Assert.That(standardFound[0].Candidate.Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(standardFound[0].Pdf417Evidence, Is.Not.Null);
            Assert.That(standardFound[0].Pdf417Evidence!.Variant, Is.EqualTo(Pdf417Variant.Standard));
            Assert.That(compactFound[0].Candidate.Format, Is.EqualTo(BarcodeFormat.CompactPdf417));
            Assert.That(compactFound[0].Pdf417Evidence, Is.Not.Null);
            Assert.That(compactFound[0].Pdf417Evidence!.Variant, Is.EqualTo(Pdf417Variant.Compact));
        });
    }

    [Test]
    public void FormatSelectionSkipsOtherFamiliesAndRejectsInvalidControls()
    {
        ModuleMatrix image = new ModuleMatrix(20, 20);

        Assert.Multiple(() =>
        {
            Assert.That(
                CommonCandidatePipeline.Locate(
                    image,
                    new BarcodeFormat[] { BarcodeFormat.DataMatrix, BarcodeFormat.DataMatrix },
                    BarcodePolarity.DarkOnLight,
                    3,
                    3),
                Is.Empty);
            Assert.That(
                () => CommonCandidatePipeline.Locate(
                    image,
                    new BarcodeFormat[] { (BarcodeFormat)999 },
                    BarcodePolarity.DarkOnLight,
                    3,
                    3),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CommonCandidatePipeline.Locate(
                    image,
                    null,
                    (BarcodePolarity)999,
                    3,
                    3),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CommonCandidatePipeline.Locate(image, null, BarcodePolarity.Automatic, 1, 3),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static ModuleMatrix DrawLinearSymbol(byte[] modules, int barHeight)
    {
        ModuleMatrix image = new ModuleMatrix(modules.Length + 20, barHeight + 4);
        int y = 2;
        while (y < barHeight + 2)
        {
            int x = 0;
            while (x < modules.Length)
            {
                if (modules[x] != 0)
                {
                    image.SetModule(x + 10, y, true);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static ModuleMatrix Invert(ModuleMatrix source)
    {
        ModuleMatrix inverted = new ModuleMatrix(source.Width, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                inverted.SetModule(x, y, !source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return inverted;
    }

    static DataMatrixEncodingResult EncodeDataMatrix(string text)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(3);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = data;
        return DataMatrixEncoder.Encode(data, payload, options, null, false);
    }

    static void DrawScaledMatrix(
        ModuleMatrix image,
        ModuleMatrix symbol,
        int left,
        int top,
        int scale)
    {
        int y = 0;
        while (y < symbol.Height)
        {
            int x = 0;
            while (x < symbol.Width)
            {
                if (symbol.GetModule(x, y))
                {
                    FillRectangle(
                        image,
                        left + (x * scale),
                        top + (y * scale),
                        scale,
                        scale);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    static Pdf417EncodingResult EncodePdf417(Pdf417Variant variant)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("PIPELINE");
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = (byte[])data.Clone();
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.ErrorCorrectionLevel = 2;
        options.Variant = variant;
        return Pdf417Encoder.Encode(data, payload, options, null);
    }

    static ModuleMatrix DrawPdf417(BarcodeSymbol symbol, int scale, int left, int top)
    {
        ModuleMatrix image = new ModuleMatrix(
            (symbol.WidthModules * scale) + (left * 2),
            (symbol.HeightModules * scale) + (top * 2));
        int y = 0;
        while (y < symbol.HeightModules)
        {
            int x = 0;
            while (x < symbol.WidthModules)
            {
                if (symbol.Modules![(y * symbol.WidthModules) + x] != 0)
                {
                    FillRectangle(
                        image,
                        left + (x * scale),
                        top + (y * scale),
                        scale,
                        scale);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static void FillRectangle(
        ModuleMatrix matrix,
        int left,
        int top,
        int width,
        int height)
    {
        int y = top;
        while (y < top + height)
        {
            int x = left;
            while (x < left + width)
            {
                matrix.SetModule(x, y, true);
                x = x + 1;
            }

            y = y + 1;
        }
    }
}
