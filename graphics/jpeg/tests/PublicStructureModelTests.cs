namespace jpeg.Tests;

internal class PublicStructureModelTests
{
    [Test]
    public void Identify_ExposesFrameScanTablesAndPreservedRawMarkers()
    {
        byte[] data = GoldenDecodeTests.CreateReadableMetadataHeavyJpeg();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Frame.Width, Is.EqualTo(8));
        Assert.That(info.Frame.Height, Is.EqualTo(8));
        Assert.That(info.Frame.Components.Count, Is.EqualTo(1));
        Assert.That(info.Frame.Components[0].Identifier, Is.EqualTo(1));
        Assert.That(info.Components[0], Is.SameAs(info.Frame.Components[0]));
        Assert.That(info.ScanHeaders.Count, Is.EqualTo(1));
        Assert.That(info.LastScanHeader, Is.SameAs(info.ScanHeaders[0]));
        Assert.That(info.LastScanHeader.Components.Count, Is.EqualTo(1));
        Assert.That(info.LastScanHeader.Components[0].Selector, Is.EqualTo(1));
        Assert.That(info.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(info.QuantizationTables[0].Values.Length, Is.EqualTo(64));
        Assert.That(info.HuffmanTables.Count, Is.EqualTo(2));
        Assert.That(info.HuffmanTables[0].CodeCounts.Length, Is.EqualTo(16));
        Assert.That(info.HuffmanTables[0].Symbols.Length, Is.EqualTo(info.HuffmanTables[0].SymbolCount));
        Assert.That(info.Metadata.RawMarkers, Is.Not.Empty);
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.Not.Null);
    }

    [Test]
    public void Identify_ExposesArithmeticConditioningTables()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xA3, 0x11, 0x2A });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame10, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame10, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(writer.ToArray());

        Assert.That(info.ArithmeticConditioningTables.Count, Is.EqualTo(2));
        Assert.That(info.ArithmeticConditioningTables[0].TableClass, Is.EqualTo(0));
        Assert.That(info.ArithmeticConditioningTables[0].Lower, Is.EqualTo(3));
        Assert.That(info.ArithmeticConditioningTables[0].Upper, Is.EqualTo(10));
        Assert.That(info.ArithmeticConditioningTables[1].TableClass, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTables[1].Kx, Is.EqualTo(42));
    }

    [Test]
    public void CoefficientWriter_AcceptsCallerConstructedStructureModels()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegCoefficientImage parsed = inspector.ReadCoefficients(data);
        global::jpeg.JpegCoefficientImage supplied = CopyCoefficientImageUsingPublicFields(parsed);

        byte[] rewritten = inspector.WriteCoefficients(supplied);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(rewritten);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(rewritten);

        Assert.That(decoded.Pixels, Is.All.EqualTo(138));
        Assert.That(info.Frame.Components[0].Identifier, Is.EqualTo(supplied.Frame.Components[0].Identifier));
        Assert.That(info.ScanHeaders[0].Components[0].Selector, Is.EqualTo(supplied.ScanHeaders[0].Components[0].Selector));
        Assert.That(info.QuantizationTables[0].Values, Is.EqualTo(supplied.QuantizationTables[0].Values));
        Assert.That(info.HuffmanTables.Count, Is.EqualTo(supplied.HuffmanTables.Count));
    }

    [Test]
    public void Writer_AcceptsCallerConstructedPreservedRawMarker()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegRawMarker marker = new global::jpeg.JpegRawMarker();
        marker.Marker = global::jpeg.JpegMarker.Comment;
        marker.Value = (int)global::jpeg.JpegMarker.Comment;
        marker.Code = 0xFE;
        marker.Order = 0;
        marker.Payload = System.Text.Encoding.Latin1.GetBytes("public marker");
        marker.PayloadLength = marker.Payload.Length;
        marker.IsComment = true;
        image.Metadata.RawMarkers.Add(marker);

        byte[] data = global::jpeg.Jpeg.Write(image);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(marker.Payload));
    }

    public static global::jpeg.JpegCoefficientImage CopyCoefficientImageUsingPublicFields(global::jpeg.JpegCoefficientImage source)
    {
        global::jpeg.JpegCoefficientImage result = new global::jpeg.JpegCoefficientImage();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = source.ComponentCount;
        result.Process = source.Process;
        result.ColorSpace = source.ColorSpace;
        result.Subsampling = source.Subsampling;
        result.Frame = CopyFrame(source.Frame);
        result.HasRestartInterval = source.HasRestartInterval;
        result.RestartInterval = source.RestartInterval;
        result.Metadata = source.Metadata;

        int index = 0;
        while (index < source.ScanHeaders.Count)
        {
            result.ScanHeaders.Add(CopyScan(source.ScanHeaders[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.QuantizationTables.Count)
        {
            result.QuantizationTables.Add(CopyQuantizationTable(source.QuantizationTables[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.HuffmanTables.Count)
        {
            result.HuffmanTables.Add(CopyHuffmanTable(source.HuffmanTables[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.ArithmeticConditioningTables.Count)
        {
            result.ArithmeticConditioningTables.Add(CopyArithmeticTable(source.ArithmeticConditioningTables[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Components.Count)
        {
            result.Components.Add(CopyCoefficientComponent(source.Components[index]));
            index = index + 1;
        }

        return result;
    }

    public static global::jpeg.JpegFrameHeader CopyFrame(global::jpeg.JpegFrameHeader source)
    {
        global::jpeg.JpegFrameHeader result = new global::jpeg.JpegFrameHeader();
        result.Marker = source.Marker;
        result.Value = source.Value;
        result.Process = source.Process;
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = source.ComponentCount;
        result.Progressive = source.Progressive;
        result.ArithmeticCoding = source.ArithmeticCoding;
        int index = 0;
        while (index < source.Components.Count)
        {
            global::jpeg.JpegFrameComponent component = new global::jpeg.JpegFrameComponent();
            component.Identifier = source.Components[index].Identifier;
            component.HorizontalSamplingFactor = source.Components[index].HorizontalSamplingFactor;
            component.VerticalSamplingFactor = source.Components[index].VerticalSamplingFactor;
            component.QuantizationTableIdentifier = source.Components[index].QuantizationTableIdentifier;
            result.Components.Add(component);
            index = index + 1;
        }

        return result;
    }

    public static global::jpeg.JpegScanHeader CopyScan(global::jpeg.JpegScanHeader source)
    {
        global::jpeg.JpegScanHeader result = new global::jpeg.JpegScanHeader();
        result.ComponentCount = source.ComponentCount;
        result.SpectralStart = source.SpectralStart;
        result.SpectralEnd = source.SpectralEnd;
        result.SuccessiveHigh = source.SuccessiveHigh;
        result.SuccessiveLow = source.SuccessiveLow;
        int index = 0;
        while (index < source.Components.Count)
        {
            global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
            component.Selector = source.Components[index].Selector;
            component.DcEntropyTableIdentifier = source.Components[index].DcEntropyTableIdentifier;
            component.AcEntropyTableIdentifier = source.Components[index].AcEntropyTableIdentifier;
            result.Components.Add(component);
            index = index + 1;
        }

        return result;
    }

    public static global::jpeg.JpegQuantizationTable CopyQuantizationTable(global::jpeg.JpegQuantizationTable source)
    {
        global::jpeg.JpegQuantizationTable result = new global::jpeg.JpegQuantizationTable();
        result.Identifier = source.Identifier;
        result.Precision = source.Precision;
        result.Is16Bit = source.Is16Bit;
        result.Values = CopyIntArray(source.Values);
        result.ZigZagValues = CopyIntArray(source.ZigZagValues);
        return result;
    }

    public static global::jpeg.JpegHuffmanTable CopyHuffmanTable(global::jpeg.JpegHuffmanTable source)
    {
        global::jpeg.JpegHuffmanTable result = new global::jpeg.JpegHuffmanTable();
        result.TableClass = source.TableClass;
        result.Identifier = source.Identifier;
        result.CodeCounts = CopyIntArray(source.CodeCounts);
        result.SymbolCount = source.SymbolCount;
        result.Symbols = CopyIntArray(source.Symbols);
        return result;
    }

    public static global::jpeg.JpegArithmeticConditioningTable CopyArithmeticTable(global::jpeg.JpegArithmeticConditioningTable source)
    {
        global::jpeg.JpegArithmeticConditioningTable result = new global::jpeg.JpegArithmeticConditioningTable();
        result.TableClass = source.TableClass;
        result.Identifier = source.Identifier;
        result.ConditioningValue = source.ConditioningValue;
        result.Lower = source.Lower;
        result.Upper = source.Upper;
        result.Kx = source.Kx;
        return result;
    }

    public static global::jpeg.JpegCoefficientComponent CopyCoefficientComponent(global::jpeg.JpegCoefficientComponent source)
    {
        global::jpeg.JpegCoefficientComponent result = new global::jpeg.JpegCoefficientComponent();
        result.ComponentIndex = source.ComponentIndex;
        result.Identifier = source.Identifier;
        result.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        result.VerticalSamplingFactor = source.VerticalSamplingFactor;
        result.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        result.BlocksPerRow = source.BlocksPerRow;
        result.BlockRows = source.BlockRows;
        result.Coefficients = CopyIntArray(source.Coefficients);
        return result;
    }

    public static int[] CopyIntArray(int[] source)
    {
        int[] result = new int[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }
}
