namespace jpeg.Tests;

internal class JpegEncoderTableOptionsTests
{
    [Test]
    public void WriteOptions_UseSuppliedTablesAndPerScanAssignments()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        global::jpeg.JpegEncoderComponent component = new global::jpeg.JpegEncoderComponent();
        component.SourceComponentIndex = 0;
        component.Identifier = 9;
        component.QuantizationTableIdentifier = 2;
        options.Components.Add(component);
        options.Tables.QuantizationTables.Add(CreateQuantizationTable(2, 7));
        options.Tables.HuffmanTables.Add(new global::jpeg.JpegImageWriter().CreateComprehensiveHuffmanTable(0, 2, false));
        options.Tables.HuffmanTables.Add(new global::jpeg.JpegImageWriter().CreateComprehensiveHuffmanTable(1, 3, false));
        options.ScanTableAssignments.Add(CreateScanAssignment(0, 9, 2, -1));
        options.ScanTableAssignments.Add(CreateScanAssignment(1, 9, -1, 3));

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegPlanarImageBuffer decoded = new global::jpeg.JpegProgressiveSof2FullImageDecoder().Decode(
            info.Frame,
            scans,
            reader.CreateTableStore(info));

        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.QuantizationTables[0].Identifier, Is.EqualTo(2));
        Assert.That(info.QuantizationTables[0].Values, Is.All.EqualTo(7));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(2));
        Assert.That(info.Components[0].QuantizationTableIdentifier, Is.EqualTo(2));
        Assert.That(info.ScanHeaders[0].Components[0].Selector, Is.EqualTo(9));
        Assert.That(info.ScanHeaders[0].Components[0].DcEntropyTableIdentifier, Is.EqualTo(2));
        Assert.That(info.ScanHeaders[1].Components[0].AcEntropyTableIdentifier, Is.EqualTo(3));
        Assert.That(decoded.Components[0].Samples[0], Is.InRange(137, 139));
        Assert.That(decoded.Components[0].Samples[8], Is.InRange(142, 144));
    }

    [Test]
    public void WriteOptions_PreserveDecodedQuantizationTablesAndAssignments()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        global::jpeg.JpegWriteOptions firstOptions = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegEncoderComponent firstComponent = new global::jpeg.JpegEncoderComponent();
        firstComponent.SourceComponentIndex = 0;
        firstComponent.Identifier = 1;
        firstComponent.QuantizationTableIdentifier = 2;
        firstOptions.Components.Add(firstComponent);
        firstOptions.Tables.QuantizationTables.Add(CreateQuantizationTable(2, 13));

        byte[] firstData = new global::jpeg.JpegWriter().WriteWithOptions(source, firstOptions);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(firstData);
        global::jpeg.JpegWriteOptions preserveOptions = new global::jpeg.JpegWriteOptions();
        preserveOptions.PreserveQuantizationTables = true;
        byte[] preservedData = new global::jpeg.JpegWriter().WriteWithOptions(decoded, preserveOptions);
        global::jpeg.JpegInfo preserved = global::jpeg.JpegIdentifier.Identify(preservedData);

        Assert.That(decoded.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(decoded.ComponentQuantizationTableIdentifiers, Is.EqualTo(new int[] { 2 }));
        Assert.That(preserved.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(preserved.QuantizationTables[0].Identifier, Is.EqualTo(2));
        Assert.That(preserved.QuantizationTables[0].Values, Is.All.EqualTo(13));
        Assert.That(preserved.Components[0].QuantizationTableIdentifier, Is.EqualTo(2));
        Assert.That(global::jpeg.Jpeg.Read(preservedData).Pixels[0], Is.InRange(137, 139));
    }

    [Test]
    public void WriteOptions_UseSuppliedArithmeticConditioningTable()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(3, 2, 130);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequentialArithmetic;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Arithmetic;
        global::jpeg.JpegArithmeticConditioningTable table = new global::jpeg.JpegArithmeticConditioningTable();
        table.TableClass = 0;
        table.Identifier = 2;
        table.Lower = 1;
        table.Upper = 3;
        options.Tables.ArithmeticConditioningTables.Add(table);
        options.ScanTableAssignments.Add(CreateScanAssignment(0, 1, 2, -1));

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegLosslessSof11ArithmeticDecoder().Decode(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData);

        Assert.That(info.ArithmeticConditioningTableCount, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTables[0].Identifier, Is.EqualTo(2));
        Assert.That(info.ArithmeticConditioningTables[0].Lower, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTables[0].Upper, Is.EqualTo(3));
        Assert.That(info.LastScanHeader.Components[0].DcEntropyTableIdentifier, Is.EqualTo(2));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(130));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 2], Is.EqualTo(130));
    }

    [Test]
    public void WriteOptions_RejectInvalidOrIncompatibleTableInputs()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        global::jpeg.JpegWriteOptions missingTable = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegEncoderComponent component = new global::jpeg.JpegEncoderComponent();
        component.QuantizationTableIdentifier = 3;
        missingTable.Components.Add(component);
        global::jpeg.JpegWriteOptions duplicateTable = new global::jpeg.JpegWriteOptions();
        duplicateTable.Tables.QuantizationTables.Add(CreateQuantizationTable(0, 7));
        duplicateTable.Tables.QuantizationTables.Add(CreateQuantizationTable(0, 9));
        global::jpeg.JpegWriteOptions incompatibleTable = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegArithmeticConditioningTable arithmetic = new global::jpeg.JpegArithmeticConditioningTable();
        arithmetic.TableClass = 0;
        arithmetic.Identifier = 0;
        arithmetic.Lower = 0;
        arithmetic.Upper = 1;
        incompatibleTable.Tables.ArithmeticConditioningTables.Add(arithmetic);

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, missingTable); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, duplicateTable); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate { new global::jpeg.JpegWriter().WriteWithOptions(source, incompatibleTable); });
    }

    internal static global::jpeg.JpegQuantizationTable CreateQuantizationTable(int identifier, int value)
    {
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTable();
        table.Identifier = identifier;
        table.Precision = 8;
        int index = 0;
        while (index < table.Values.Length)
        {
            table.Values[index] = value;
            index = index + 1;
        }

        return table;
    }

    internal static global::jpeg.JpegEncoderScanTableAssignment CreateScanAssignment(
        int scanIndex,
        int componentIdentifier,
        int dcTableIdentifier,
        int acTableIdentifier)
    {
        global::jpeg.JpegEncoderScanTableAssignment assignment = new global::jpeg.JpegEncoderScanTableAssignment();
        assignment.ScanIndex = scanIndex;
        assignment.ComponentIdentifier = componentIdentifier;
        assignment.DcEntropyTableIdentifier = dcTableIdentifier;
        assignment.AcEntropyTableIdentifier = acTableIdentifier;
        return assignment;
    }
}
