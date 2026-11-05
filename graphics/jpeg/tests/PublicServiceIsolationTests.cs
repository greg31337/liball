namespace jpeg.Tests;

internal class PublicServiceIsolationTests
{
    [Test]
    public void SharedPublicServiceInstances_IsolateConcurrentOperationsAndCallerBuffers()
    {
        global::jpeg.JpegImageFactory imageFactory = new global::jpeg.JpegImageFactory();
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.JpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegMetadataEditor metadataEditor = new global::jpeg.JpegMetadataEditor();
        global::jpeg.JpegTransformer transformer = new global::jpeg.JpegTransformer();
        System.Threading.ManualResetEventSlim start = new System.Threading.ManualResetEventSlim(false);
        System.Threading.Tasks.Task<int>[] operations = new System.Threading.Tasks.Task<int>[24];

        int index = 0;
        while (index < operations.Length)
        {
            int operationIndex = index;
            int expectedWidth = operationIndex % 2 == 0 ? 8 : 16;
            byte expectedSample = (byte)(96 + operationIndex);
            byte[] callerPixels = new byte[expectedWidth * 8];
            System.Array.Fill(callerPixels, expectedSample);
            operations[index] = System.Threading.Tasks.Task.Run(
                () => RunConcurrentOperation(
                    start,
                    imageFactory,
                    writer,
                    reader,
                    identifier,
                    inspector,
                    metadataEditor,
                    transformer,
                    callerPixels,
                    expectedSample,
                    expectedWidth));
            index = index + 1;
        }

        start.Set();
        System.Threading.Tasks.Task.WaitAll(operations);

        index = 0;
        while (index < operations.Length)
        {
            Assert.That(operations[index].Result, Is.EqualTo(index % 2 == 0 ? 8 : 16));
            index = index + 1;
        }
    }

    internal static int RunConcurrentOperation(
        System.Threading.ManualResetEventSlim start,
        global::jpeg.JpegImageFactory imageFactory,
        global::jpeg.JpegWriter writer,
        global::jpeg.JpegReader reader,
        global::jpeg.JpegIdentifierService identifier,
        global::jpeg.JpegInspectionService inspector,
        global::jpeg.JpegMetadataEditor metadataEditor,
        global::jpeg.JpegTransformer transformer,
        byte[] callerPixels,
        byte expectedSample,
        int expectedWidth)
    {
        start.Wait();
        global::jpeg.JpegImage image = imageFactory.CreateImage(
            callerPixels,
            expectedWidth,
            8,
            global::jpeg.JpegPixelFormat.L8);
        System.Array.Fill(callerPixels, (byte)0);
        if (image.Pixels[0] != expectedSample)
        {
            return -1;
        }

        byte[] encoded = writer.Write(image);
        global::jpeg.JpegImage decoded = reader.Read(encoded);
        global::jpeg.JpegInfo identified = identifier.Identify(encoded);
        global::jpeg.JpegInspectionResult inspected = inspector.InspectMarkers(encoded);
        byte[] stripped = metadataEditor.StripMetadata(encoded);
        byte[] rotated = transformer.Rotate90WithOptionsAndCancellation(
            encoded,
            new global::jpeg.JpegTransformOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegInfo strippedInfo = identifier.Identify(stripped);
        global::jpeg.JpegInfo rotatedInfo = identifier.Identify(rotated);

        if (decoded.Width != expectedWidth
            || identified.Width != expectedWidth
            || inspected.Info.Width != expectedWidth
            || strippedInfo.Width != expectedWidth
            || rotatedInfo.Width != 8
            || rotatedInfo.Height != expectedWidth)
        {
            return -1;
        }

        return expectedWidth;
    }
}
