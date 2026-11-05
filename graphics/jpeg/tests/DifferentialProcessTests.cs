namespace jpeg.Tests;

internal class DifferentialProcessTests
{
    [Test]
    public void Process_RecognizesDifferentialMarkersAndProcessesAsCorpusGated()
    {
        int[] markers = CreateDifferentialMarkers();
        global::jpeg.JpegProcess[] processes = CreateDifferentialProcesses();
        global::jpeg.JpegDifferentialProcess differential = new global::jpeg.JpegDifferentialProcess();

        int index = 0;
        while (index < markers.Length)
        {
            Assert.That(differential.IsDifferentialMarker(markers[index]), Is.True);
            Assert.That(differential.IsDifferentialProcess(processes[index]), Is.True);
            Assert.That(differential.RequiresCorpusBeforeDecoderSupport(processes[index]), Is.True);
            Assert.That(differential.GetUnsupportedMessage(processes[index]), Does.Contain("Differential"));
            Assert.That(differential.GetUnsupportedMessage(processes[index]), Does.Contain("not supported"));
            index = index + 1;
        }
    }

    [Test]
    public void Process_DoesNotTreatImplementedOriginalProcessesAsDifferential()
    {
        global::jpeg.JpegDifferentialProcess differential = new global::jpeg.JpegDifferentialProcess();

        Assert.That(differential.IsDifferentialMarker(global::jpeg.JpegConstants.MarkerStartOfFrame0), Is.False);
        Assert.That(differential.IsDifferentialMarker(global::jpeg.JpegConstants.MarkerStartOfFrame3), Is.False);
        Assert.That(differential.IsDifferentialMarker(global::jpeg.JpegConstants.MarkerStartOfFrame11), Is.False);
        Assert.That(differential.IsDifferentialProcess(global::jpeg.JpegProcess.BaselineDct), Is.False);
        Assert.That(differential.IsDifferentialProcess(global::jpeg.JpegProcess.LosslessSequential), Is.False);
        Assert.That(differential.IsDifferentialProcess(global::jpeg.JpegProcess.LosslessSequentialArithmetic), Is.False);
        Assert.That(differential.RequiresCorpusBeforeDecoderSupport(global::jpeg.JpegProcess.ProgressiveDct), Is.False);
    }

    [Test]
    public void Identifier_UsesDifferentialPolicyForUnsupportedDiagnostics()
    {
        global::jpeg.JpegIdentifier identifier = new global::jpeg.JpegIdentifier(IdentifierTests.CreateMinimalBaselineJpeg());

        Assert.That(identifier.IsDifferentialProcess(global::jpeg.JpegConstants.MarkerStartOfFrame5), Is.True);
        Assert.That(identifier.IsDifferentialProcess(global::jpeg.JpegConstants.MarkerStartOfFrame0), Is.False);
    }

    public static int[] CreateDifferentialMarkers()
    {
        return new int[]
        {
            global::jpeg.JpegConstants.MarkerStartOfFrame5,
            global::jpeg.JpegConstants.MarkerStartOfFrame6,
            global::jpeg.JpegConstants.MarkerStartOfFrame7,
            global::jpeg.JpegConstants.MarkerStartOfFrame13,
            global::jpeg.JpegConstants.MarkerStartOfFrame14,
            global::jpeg.JpegConstants.MarkerStartOfFrame15
        };
    }

    public static global::jpeg.JpegProcess[] CreateDifferentialProcesses()
    {
        return new global::jpeg.JpegProcess[]
        {
            global::jpeg.JpegProcess.DifferentialSequentialDct,
            global::jpeg.JpegProcess.DifferentialProgressiveDct,
            global::jpeg.JpegProcess.DifferentialLosslessSequential,
            global::jpeg.JpegProcess.DifferentialSequentialDctArithmetic,
            global::jpeg.JpegProcess.DifferentialProgressiveDctArithmetic,
            global::jpeg.JpegProcess.DifferentialLosslessSequentialArithmetic
        };
    }
}
