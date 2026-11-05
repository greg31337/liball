using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class DiagnosticSinkTests
{
    [Test]
    public void CollectsDiagnosticsInInsertionOrderAndReportsErrors()
    {
        DiagnosticSink sink = new DiagnosticSink(3);

        Assert.That(sink.Add(DiagnosticLevel.Information, "I001", "First", null), Is.True);
        Assert.That(sink.Add(DiagnosticLevel.Warning, "W001", "Second", "/window"), Is.True);
        Assert.That(sink.HasErrors(), Is.False);
        Assert.That(sink.Add(DiagnosticLevel.Error, "E001", "Third", "/window/view"), Is.True);

        DiagnosticEntry[] entries = sink.CreateSnapshot();
        Assert.That(sink.GetCount(), Is.EqualTo(3));
        Assert.That(sink.HasErrors(), Is.True);
        Assert.That(entries.Select(entry => entry.Code), Is.EqualTo(new string[] { "I001", "W001", "E001" }));
        Assert.That(entries[0].Location, Is.Empty);
        Assert.That(entries[2].Level, Is.EqualTo(DiagnosticLevel.Error));
    }

    [Test]
    public void EnforcesBoundAndRecordsTruncationUntilCleared()
    {
        DiagnosticSink sink = new DiagnosticSink(1);
        sink.Add(DiagnosticLevel.Warning, "W001", "Accepted", "root");

        Assert.That(sink.Add(DiagnosticLevel.Error, "E001", "Dropped", "root"), Is.False);
        Assert.That(sink.GetCount(), Is.EqualTo(1));
        Assert.That(sink.HasErrors(), Is.False);
        Assert.That(sink.WasTruncated(), Is.True);

        sink.Clear();

        Assert.That(sink.GetCount(), Is.Zero);
        Assert.That(sink.WasTruncated(), Is.False);
        Assert.That(sink.Add(DiagnosticLevel.Error, "E002", "Accepted after clear", "root"), Is.True);
    }

    [Test]
    public void SnapshotDoesNotExposeStoredEntriesForMutation()
    {
        DiagnosticSink sink = new DiagnosticSink(1);
        sink.Add(DiagnosticLevel.Information, "I001", "Original", "root");

        DiagnosticEntry[] snapshot = sink.CreateSnapshot();
        snapshot[0].Message = "Changed";

        Assert.That(sink.CreateSnapshot()[0].Message, Is.EqualTo("Original"));
    }

    [Test]
    public void ValidatesCapacityCodeAndMessage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiagnosticSink(0));

        DiagnosticSink sink = new DiagnosticSink(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => sink.Add((DiagnosticLevel)99, "E000", "message", "root"));
        Assert.Throws<ArgumentNullException>(() => sink.Add(DiagnosticLevel.Error, null, "message", "root"));
        Assert.Throws<ArgumentException>(() => sink.Add(DiagnosticLevel.Error, " ", "message", "root"));
        Assert.Throws<ArgumentNullException>(() => sink.Add(DiagnosticLevel.Error, "E001", null, "root"));
        Assert.Throws<ArgumentException>(() => sink.Add(DiagnosticLevel.Error, "E001", " ", "root"));
    }
}
