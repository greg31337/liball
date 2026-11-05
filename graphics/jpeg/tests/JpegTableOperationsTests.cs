namespace jpeg.Tests;

internal class JpegTableOperationsTests
{
    [Test]
    public void CompleteJpegTableOperations_ReplaceOnlyTableDefinitionBytes()
    {
        byte[] source = GoldenDecodeTests.CreateReadableMetadataHeavyJpeg();
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegTableSet tables = inspector.ExtractTables(source);
        SetQuantizationValue(tables.QuantizationTables[0], 20);
        global::jpeg.JpegArithmeticConditioningTable arithmetic = new global::jpeg.JpegArithmeticConditioningTable();
        arithmetic.TableClass = 0;
        arithmetic.Identifier = 0;
        arithmetic.ConditioningValue = 0xA3;
        arithmetic.Lower = 3;
        arithmetic.Upper = 10;
        tables.ArithmeticConditioningTables.Add(arithmetic);

        byte[] injected = inspector.InjectTables(source, tables);

        global::jpeg.JpegTableSet extracted = inspector.ExtractTables(injected);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(injected);
        Assert.That(extracted.QuantizationTables[0].Values, Is.All.EqualTo(20));
        Assert.That(extracted.HuffmanTables.Count, Is.EqualTo(tables.HuffmanTables.Count));
        Assert.That(extracted.ArithmeticConditioningTables.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.Comments, Is.EqualTo(global::jpeg.Jpeg.Identify(source).Metadata.Comments));
        Assert.That(RemoveTableDefinitionBytes(injected), Is.EqualTo(RemoveTableDefinitionBytes(source)));
    }

    [Test]
    public void AbbreviatedTableOperations_ExtractTableOnlyAndCompleteImageOnlyStreams()
    {
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegTableSet tables = AbbreviatedStreamApiTests.CreateBaselineTableSet();
        byte[] tableOnly = inspector.WriteTableStream(tables);
        global::jpeg.JpegImage sourceImage = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] imageOnly = new global::jpeg.JpegWriter().WriteAbbreviatedImage(sourceImage, tables);

        global::jpeg.JpegTableSet extractedTableOnly = inspector.ExtractTables(tableOnly);
        global::jpeg.JpegTableSet extractedImageOnly = inspector.ExtractTables(imageOnly);
        SetQuantizationValue(extractedTableOnly.QuantizationTables[0], 35);
        byte[] replacedTableOnly = inspector.InjectTables(tableOnly, extractedTableOnly);
        byte[] completed = inspector.InjectTables(imageOnly, extractedTableOnly);

        global::jpeg.JpegInfo tableOnlyInfo = global::jpeg.Jpeg.Identify(replacedTableOnly);
        global::jpeg.JpegInfo completedInfo = global::jpeg.Jpeg.Identify(completed);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(completed);
        Assert.That(extractedTableOnly.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(extractedTableOnly.HuffmanTables.Count, Is.EqualTo(2));
        Assert.That(extractedImageOnly.QuantizationTables, Is.Empty);
        Assert.That(extractedImageOnly.HuffmanTables, Is.Empty);
        Assert.That(tableOnlyInfo.IsAbbreviatedTableStream, Is.True);
        Assert.That(tableOnlyInfo.QuantizationTables[0].Values, Is.All.EqualTo(35));
        Assert.That(RemoveTableDefinitionBytes(replacedTableOnly), Is.EqualTo(RemoveTableDefinitionBytes(tableOnly)));
        Assert.That(completedInfo.IsAbbreviatedImageStream, Is.False);
        Assert.That(completedInfo.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(completedInfo.HuffmanTableCount, Is.EqualTo(2));
        Assert.That(decoded.Width, Is.EqualTo(8));
        Assert.That(decoded.Height, Is.EqualTo(8));
        Assert.That(RemoveTableDefinitionBytes(completed), Is.EqualTo(imageOnly));
    }

    [Test]
    public void CoefficientTableOperations_CopyAndReplaceOnlyTableCollections()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegCoefficientImage image = inspector.ReadCoefficients(source);
        global::jpeg.JpegCoefficientComponent component = image.Components[0];
        global::jpeg.JpegMetadata metadata = image.Metadata;
        global::jpeg.JpegTableSet tables = inspector.ExtractCoefficientTables(image);
        SetQuantizationValue(tables.QuantizationTables[0], 30);

        global::jpeg.JpegCoefficientImage result = inspector.InjectCoefficientTables(image, tables);

        Assert.That(result, Is.SameAs(image));
        Assert.That(result.Components[0], Is.SameAs(component));
        Assert.That(result.Metadata, Is.SameAs(metadata));
        Assert.That(result.QuantizationTables[0], Is.Not.SameAs(tables.QuantizationTables[0]));
        Assert.That(result.QuantizationTables[0].Values, Is.All.EqualTo(30));
        tables.QuantizationTables[0].Values[0] = 99;
        Assert.That(result.QuantizationTables[0].Values[0], Is.EqualTo(30));
        byte[] rewritten = inspector.WriteCoefficients(result);
        Assert.That(inspector.ReadCoefficients(rewritten).Components[0].Coefficients[0], Is.EqualTo(2));
    }

    [Test]
    public void TableOperations_RejectNullAndEmptyInputs()
    {
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                inspector.ExtractTables(null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                inspector.InjectTables(source, null!);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                inspector.InjectTables(source, new global::jpeg.JpegTableSet());
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                inspector.ExtractCoefficientTables(null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                inspector.InjectCoefficientTables(null!, new global::jpeg.JpegTableSet());
            });
    }

    public static void SetQuantizationValue(global::jpeg.JpegQuantizationTable table, int value)
    {
        int index = 0;
        while (index < 64)
        {
            table.Values[index] = value;
            table.ZigZagValues[index] = value;
            index = index + 1;
        }
    }

    public static byte[] RemoveTableDefinitionBytes(byte[] data)
    {
        global::jpeg.JpegInspectionResult inspection = new global::jpeg.JpegInspectionService().InspectMarkers(data);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        int sourceOffset = 0;
        foreach (global::jpeg.JpegMarkerSegment marker in inspection.Markers)
        {
            if (marker.Marker != global::jpeg.JpegMarker.DefineQuantizationTable
                && marker.Marker != global::jpeg.JpegMarker.DefineHuffmanTable
                && marker.Marker != global::jpeg.JpegMarker.DefineArithmeticConditioning)
            {
                continue;
            }

            int markerOffset = (int)marker.MarkerOffset;
            int endOffset = (int)marker.EndOffset;
            output.Write(data, sourceOffset, markerOffset - sourceOffset);
            sourceOffset = endOffset;
        }

        output.Write(data, sourceOffset, data.Length - sourceOffset);
        return output.ToArray();
    }
}
