namespace jpeg.Tests;

internal class PublicApiRobustnessWorkflowTests
{
    [Test]
    public void PublicServices_RejectNullPrimaryArguments()
    {
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();
        global::jpeg.JpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegMetadataEditor metadata = new global::jpeg.JpegMetadataEditor();
        global::jpeg.JpegTransformer transformer = new global::jpeg.JpegTransformer();

        Assert.Throws<System.ArgumentNullException>(delegate { identifier.Identify(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { reader.Read(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { writer.Write(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { inspector.InspectMarkers(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { inspector.ReadCoefficients(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { inspector.WriteCoefficients(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { inspector.WriteTableStream(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { metadata.StripMetadata(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { transformer.Rotate90(null!); });
    }

    [Test]
    public void PublicStreamServices_RejectWrongCapabilitiesBeforeIo()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);

        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegIdentifierService().IdentifyStream(new JpegIoThrowingWriteStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegReader().ReadStream(new JpegIoThrowingWriteStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegInspectionService().InspectMarkersStream(new JpegIoThrowingWriteStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegInspectionService().ReadCoefficientsStream(new JpegIoThrowingWriteStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegMetadataEditor().StripMetadataStream(
                new JpegIoThrowingWriteStream(),
                new System.IO.MemoryStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                new JpegIoThrowingWriteStream(),
                new System.IO.MemoryStream(),
                new global::jpeg.JpegTransformOptions(),
                default(System.Threading.CancellationToken));
        });

        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegWriter().WriteToStream(image, CreateReadOnlyStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegInspectionService().WriteCoefficientsStream(coefficients, CreateReadOnlyStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegMetadataEditor().StripMetadataStream(
                new System.IO.MemoryStream(source),
                CreateReadOnlyStream());
        });
        Assert.Throws<System.ArgumentException>(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                new System.IO.MemoryStream(source),
                CreateReadOnlyStream(),
                new global::jpeg.JpegTransformOptions(),
                default(System.Threading.CancellationToken));
        });
    }

    [Test]
    public void PublicStreamServices_AcceptNonSeekableInputsAndOutputs()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().IdentifyStream(new NonSeekableReadStream(source));
        global::jpeg.JpegInspectionResult inspection = new global::jpeg.JpegInspectionService().InspectMarkersStream(new NonSeekableReadStream(source));
        NonSeekableWriteStream metadataOutput = new NonSeekableWriteStream(new byte[0]);
        new global::jpeg.JpegMetadataEditor().StripMetadataStream(new NonSeekableReadStream(source), metadataOutput);
        NonSeekableWriteStream transformOutput = new NonSeekableWriteStream(new byte[0]);
        new global::jpeg.JpegTransformer().Rotate180StreamWithOptionsAndCancellation(
            new NonSeekableReadStream(source),
            transformOutput,
            new global::jpeg.JpegTransformOptions(),
            default(System.Threading.CancellationToken));

        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(inspection.Info.SawEndOfImage, Is.True);
        Assert.That(global::jpeg.Jpeg.Read(metadataOutput.ToArray()).Pixels[0], Is.EqualTo(138));
        Assert.That(global::jpeg.Jpeg.Read(transformOutput.ToArray()).Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void EveryTruncatedPrefix_IsRejectedWithoutRuntimeExceptions()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        int length = 2;
        while (length < source.Length)
        {
            byte[] truncated = MalformedInputTests.CopyBytes(source, length);
            global::jpeg.JpegInvalidDataException exception = Assert.Throws<global::jpeg.JpegInvalidDataException>(
                delegate
                {
                    new global::jpeg.JpegReader().Read(truncated);
                })!;
            Assert.That(exception.Message, Is.Not.Empty, "prefix length " + length);
            length = length + 1;
        }
    }

    [Test]
    public void RedefinedTables_ReplaceActiveDefinitionsAndDecodeWithTheLatestValues()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegInfo original = global::jpeg.Jpeg.Identify(source);
        byte[] quantization = FindMarkerPayload(source, global::jpeg.JpegConstants.MarkerDefineQuantizationTable);
        quantization[1] = 44;
        byte[] huffman = FindMarkerPayload(source, global::jpeg.JpegConstants.MarkerDefineHuffmanTable);
        byte[] redefined = MalformedInputTests.InsertSegmentBeforeMarker(
            source,
            global::jpeg.JpegConstants.MarkerStartOfFrame0,
            global::jpeg.JpegConstants.MarkerDefineQuantizationTable,
            quantization);
        redefined = MalformedInputTests.InsertSegmentBeforeMarker(
            redefined,
            global::jpeg.JpegConstants.MarkerStartOfFrame0,
            global::jpeg.JpegConstants.MarkerDefineHuffmanTable,
            huffman);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(redefined);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(redefined);

        Assert.That(info.QuantizationTableCount, Is.GreaterThan(info.QuantizationTables.Count));
        Assert.That(info.HuffmanTableCount, Is.GreaterThan(info.HuffmanTables.Count));
        Assert.That(info.QuantizationTables.Count, Is.EqualTo(original.QuantizationTables.Count));
        Assert.That(info.HuffmanTables.Count, Is.EqualTo(original.HuffmanTables.Count));
        Assert.That(info.QuantizationTables[0].Values[0], Is.EqualTo(44));
        Assert.That(decoded.Pixels[0], Is.EqualTo(139));
    }

    [Test]
    public void RedefinedArithmeticConditioningTable_ReplacesTheActiveDefinition()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xA3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xB4 });
        writer.WriteMarkerSegment(
            global::jpeg.JpegMarker.StartOfFrame9,
            IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame9, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(writer.ToArray());

        Assert.That(info.ArithmeticConditioningTableCount, Is.EqualTo(2));
        Assert.That(info.ArithmeticConditioningTables.Count, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTables[0].ConditioningValue, Is.EqualTo(0xB4));
    }

    [Test]
    public void MissingTablesAndBadScans_AreRejectedAcrossDecodeAndCoefficientApis()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        byte[] missingQuantization = RemoveFirstMarker(source, global::jpeg.JpegConstants.MarkerDefineQuantizationTable);
        byte[] missingHuffman = RemoveAllMarkers(source, global::jpeg.JpegConstants.MarkerDefineHuffmanTable);
        byte[] badScan = (byte[])source.Clone();
        int scanOffset = MalformedInputTests.FindMarkerOffset(badScan, global::jpeg.JpegConstants.MarkerStartOfScan);
        badScan[scanOffset + 7] = 1;

        AssertDecodeAndCoefficientFailure(missingQuantization);
        AssertDecodeAndCoefficientFailure(missingHuffman);
        AssertDecodeAndCoefficientFailure(badScan);
    }

    [Test]
    public void CoefficientWriter_RejectsBufferArithmeticOverflowDeterministically()
    {
        global::jpeg.JpegCoefficientImage image = JpegLosslessTransformerTests.CreateFourBlockCoefficientImage();
        image.Components[0].BlocksPerRow = int.MaxValue;
        image.Components[0].BlockRows = int.MaxValue;
        image.Components[0].Coefficients = new int[0];

        global::jpeg.JpegWriteValidationException exception = Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                new global::jpeg.JpegInspectionService().WriteCoefficients(image);
            })!;

        Assert.That(exception.Message, Is.EqualTo("Coefficient component buffer is too large."));
    }

    [Test]
    public void TolerantRecovery_ProducesDeterministicDiagnosticsAcrossPublicServices()
    {
        byte[] malformed = MarkerInspectionResultTests.CreateDuplicateFrameJpeg();
        global::jpeg.JpegReadOptions options = TolerantParserModeTests.CreateTolerantOptions();
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();

        global::jpeg.JpegInfo first = identifier.IdentifyWithOptions(malformed, options);
        global::jpeg.JpegInfo second = identifier.IdentifyWithOptions(malformed, options);
        global::jpeg.JpegInspectionResult inspected = inspector.InspectMarkersWithOptions(malformed, options);

        Assert.That(first.SawEndOfImage, Is.True);
        Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
        AssertDiagnosticEqual(first.Diagnostics[0], second.Diagnostics[0]);
        AssertDiagnosticEqual(first.Diagnostics[0], inspected.Diagnostics[0]);
        Assert.That(first.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.InvalidMarkerOrder));
        Assert.That(first.Diagnostics[0].Severity, Is.EqualTo(global::jpeg.JpegDiagnosticSeverity.Warning));
    }

    [Test]
    public void ConfiguredLimits_AreConsistentAcrossIdentifierReaderAndInspector()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxMarkerPayloadBytes = 1;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(delegate
        {
            new global::jpeg.JpegIdentifierService().IdentifyWithOptions(source, options);
        });
        Assert.Throws<global::jpeg.JpegResourceLimitException>(delegate
        {
            new global::jpeg.JpegReader().ReadWithOptions(source, options);
        });
        Assert.Throws<global::jpeg.JpegResourceLimitException>(delegate
        {
            new global::jpeg.JpegInspectionService().InspectMarkersWithOptions(source, options);
        });
        Assert.Throws<global::jpeg.JpegResourceLimitException>(delegate
        {
            new global::jpeg.JpegInspectionService().ReadCoefficientsWithOptions(source, options);
        });
    }

    public static System.IO.Stream CreateReadOnlyStream()
    {
        return new System.IO.MemoryStream(new byte[1], false);
    }

    public static byte[] FindMarkerPayload(byte[] data, int markerValue)
    {
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);
        foreach (global::jpeg.JpegMarkerSegment marker in markers)
        {
            if (marker.Value == markerValue)
            {
                return (byte[])marker.Payload.Clone();
            }
        }

        throw new System.InvalidOperationException("Marker payload was not found.");
    }

    public static byte[] RemoveFirstMarker(byte[] data, int markerValue)
    {
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);
        foreach (global::jpeg.JpegMarkerSegment marker in markers)
        {
            if (marker.Value == markerValue)
            {
                return RemoveByteRange(data, (int)marker.MarkerOffset, (int)marker.EndOffset);
            }
        }

        throw new System.InvalidOperationException("Marker was not found.");
    }

    public static byte[] RemoveAllMarkers(byte[] data, int markerValue)
    {
        byte[] result = data;
        while (ContainsMarker(result, markerValue))
        {
            result = RemoveFirstMarker(result, markerValue);
        }

        return result;
    }

    public static bool ContainsMarker(byte[] data, int markerValue)
    {
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);
        foreach (global::jpeg.JpegMarkerSegment marker in markers)
        {
            if (marker.Value == markerValue)
            {
                return true;
            }
        }

        return false;
    }

    public static byte[] RemoveByteRange(byte[] data, int start, int end)
    {
        byte[] result = new byte[data.Length - (end - start)];
        System.Array.Copy(data, 0, result, 0, start);
        System.Array.Copy(data, end, result, start, data.Length - end);
        return result;
    }

    public static void AssertDecodeAndCoefficientFailure(byte[] data)
    {
        Assert.Throws<global::jpeg.JpegInvalidDataException>(delegate
        {
            new global::jpeg.JpegReader().Read(data);
        });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(delegate
        {
            new global::jpeg.JpegInspectionService().ReadCoefficients(data);
        });
    }

    public static void AssertDiagnosticEqual(global::jpeg.JpegDiagnostic expected, global::jpeg.JpegDiagnostic actual)
    {
        Assert.That(actual.Code, Is.EqualTo(expected.Code));
        Assert.That(actual.Severity, Is.EqualTo(expected.Severity));
        Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(actual.Offset, Is.EqualTo(expected.Offset));
        Assert.That(actual.Marker, Is.EqualTo(expected.Marker));
    }
}
