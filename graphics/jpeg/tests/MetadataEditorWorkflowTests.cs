using System.Collections.Generic;

namespace jpeg.Tests;

internal class MetadataEditorWorkflowTests
{
    [Test]
    public void EditMarkers_ReplacesGenericSequenceAndPreservesEncodedBytes()
    {
        byte[] source = JpegMetadataUpdaterTests.CreateMetadataUpdateSource();
        List<global::jpeg.JpegRawMarker> markers = new List<global::jpeg.JpegRawMarker>();
        markers.Add(RawMarkerEditingTests.CreateRawMarker(global::jpeg.JpegMarker.Unknown, 0xFFF0, new byte[] { 9, 8 }));
        markers.Add(RawMarkerEditingTests.CreateRawMarker(global::jpeg.JpegMarker.App3, global::jpeg.JpegConstants.MarkerApp3, new byte[] { 7 }));
        markers.Add(RawMarkerEditingTests.CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, System.Text.Encoding.Latin1.GetBytes("edited")));
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();

        byte[] result = editor.EditMarkers(source, markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result);

        Assert.That(info.Metadata.RawMarkers.Select(item => item.Value), Is.EqualTo(new int[] { 0xFFF0, global::jpeg.JpegConstants.MarkerApp3, global::jpeg.JpegConstants.MarkerComment }));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(new byte[] { 9, 8 }));
        Assert.That(info.Metadata.RawMarkers[1].Payload, Is.EqualTo(new byte[] { 7 }));
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "edited" }));
        Assert.That(GetNonMetadataSuffix(result), Is.EqualTo(GetNonMetadataSuffix(source)));
    }

    [Test]
    public void ApplyMetadataPolicy_FiltersExistingMarkersAndStripRemovesAll()
    {
        byte[] source = MetadataPolicyTests.CreateMetadataHeavyJpeg();
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();

        byte[] preserved = editor.ApplyMetadataPolicy(source, global::jpeg.JpegMetadataPolicy.PreserveAll);
        byte[] knownData = editor.ApplyMetadataPolicy(source, global::jpeg.JpegMetadataPolicy.PreserveKnown);
        byte[] safeData = editor.ApplyMetadataPolicy(source, global::jpeg.JpegMetadataPolicy.StripUnsafe);
        byte[] strippedData = editor.StripMetadata(source);
        global::jpeg.JpegInfo known = global::jpeg.Jpeg.Identify(knownData);
        global::jpeg.JpegInfo safe = global::jpeg.Jpeg.Identify(safeData);
        global::jpeg.JpegInfo stripped = global::jpeg.Jpeg.Identify(strippedData);

        Assert.That(preserved, Is.EqualTo(source));
        Assert.That(known.Metadata.RawMarkers.Any(item => item.Value == 0xFFF0), Is.False);
        Assert.That(known.Metadata.HasExif, Is.True);
        Assert.That(known.Metadata.Comments, Is.Not.Empty);
        Assert.That(safe.Metadata.HasJfif, Is.True);
        Assert.That(safe.Metadata.HasAdobe, Is.True);
        Assert.That(safe.Metadata.HasExif, Is.False);
        Assert.That(safe.Metadata.Comments, Is.Empty);
        Assert.That(stripped.Metadata.RawMarkers, Is.Empty);
        Assert.That(GetNonMetadataSuffix(knownData), Is.EqualTo(GetNonMetadataSuffix(source)));
        Assert.That(GetNonMetadataSuffix(safeData), Is.EqualTo(GetNonMetadataSuffix(source)));
        Assert.That(GetNonMetadataSuffix(strippedData), Is.EqualTo(GetNonMetadataSuffix(source)));
    }

    [Test]
    public void SpanAndStreamWorkflows_ProduceTheSameContainerEdits()
    {
        byte[] source = JpegMetadataUpdaterTests.CreateMetadataUpdateSource();
        List<global::jpeg.JpegRawMarker> markers = new List<global::jpeg.JpegRawMarker>();
        markers.Add(RawMarkerEditingTests.CreateRawMarker(global::jpeg.JpegMarker.App4, global::jpeg.JpegConstants.MarkerApp4, new byte[] { 4, 4 }));
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();
        byte[] expectedEdit = editor.EditMarkers(source, markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected);
        byte[] expectedSafe = editor.ApplyMetadataPolicy(source, global::jpeg.JpegMetadataPolicy.StripUnsafe);
        byte[] expectedStrip = editor.StripMetadata(source);

        byte[] spanEdit = editor.EditMarkersSpan(new System.ReadOnlySpan<byte>(source), markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected);
        byte[] spanSafe = editor.ApplyMetadataPolicySpan(new System.ReadOnlySpan<byte>(source), global::jpeg.JpegMetadataPolicy.StripUnsafe);
        byte[] spanStrip = editor.StripMetadataSpan(new System.ReadOnlySpan<byte>(source));
        System.IO.MemoryStream editInput = new System.IO.MemoryStream(source);
        System.IO.MemoryStream editOutput = new System.IO.MemoryStream();
        editor.EditMarkersStream(editInput, editOutput, markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected);
        System.IO.MemoryStream safeInput = new System.IO.MemoryStream(source);
        System.IO.MemoryStream safeOutput = new System.IO.MemoryStream();
        editor.ApplyMetadataPolicyStream(safeInput, safeOutput, global::jpeg.JpegMetadataPolicy.StripUnsafe);
        System.IO.MemoryStream stripInput = new System.IO.MemoryStream(source);
        System.IO.MemoryStream stripOutput = new System.IO.MemoryStream();
        editor.StripMetadataStream(stripInput, stripOutput);

        Assert.That(spanEdit, Is.EqualTo(expectedEdit));
        Assert.That(spanSafe, Is.EqualTo(expectedSafe));
        Assert.That(spanStrip, Is.EqualTo(expectedStrip));
        Assert.That(editOutput.ToArray(), Is.EqualTo(expectedEdit));
        Assert.That(safeOutput.ToArray(), Is.EqualTo(expectedSafe));
        Assert.That(stripOutput.ToArray(), Is.EqualTo(expectedStrip));
        Assert.That(editInput.CanRead, Is.True);
        Assert.That(editOutput.CanWrite, Is.True);
    }

    [Test]
    public void FileWorkflows_EditFilterAndStripWithoutReencoding()
    {
        byte[] source = JpegMetadataUpdaterTests.CreateMetadataUpdateSource();
        List<global::jpeg.JpegRawMarker> markers = new List<global::jpeg.JpegRawMarker>();
        markers.Add(RawMarkerEditingTests.CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, new byte[] { 102, 105, 108, 101 }));
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jpeg-metadata-" + System.Guid.NewGuid().ToString("N"));
        string sourcePath = System.IO.Path.Combine(directory, "source.jpg");
        string editedPath = System.IO.Path.Combine(directory, "edited.jpg");
        string filteredPath = System.IO.Path.Combine(directory, "filtered.jpg");
        string strippedPath = System.IO.Path.Combine(directory, "stripped.jpg");
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            System.IO.File.WriteAllBytes(sourcePath, source);
            editor.EditMarkersFile(sourcePath, editedPath, markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected);
            editor.ApplyMetadataPolicyFile(sourcePath, filteredPath, global::jpeg.JpegMetadataPolicy.StripUnsafe);
            editor.StripMetadataFile(sourcePath, strippedPath);

            Assert.That(System.IO.File.ReadAllBytes(editedPath), Is.EqualTo(editor.EditMarkers(source, markers, global::jpeg.JpegMetadataPolicy.ReplaceSelected)));
            Assert.That(System.IO.File.ReadAllBytes(filteredPath), Is.EqualTo(editor.ApplyMetadataPolicy(source, global::jpeg.JpegMetadataPolicy.StripUnsafe)));
            Assert.That(System.IO.File.ReadAllBytes(strippedPath), Is.EqualTo(editor.StripMetadata(source)));
        }
        finally
        {
            System.IO.Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SelectedMetadataSetters_PreserveEncodedBytesExactly()
    {
        byte[] source = JpegMetadataUpdaterTests.CreateMetadataUpdateSource();
        List<string> comments = new List<string>();
        comments.Add("container rewrite");

        byte[] result = new global::jpeg.JpegMetadataEditor().SetComments(source, comments);

        Assert.That(global::jpeg.Jpeg.Identify(result).Metadata.Comments, Is.EqualTo(comments));
        Assert.That(GetNonMetadataSuffix(result), Is.EqualTo(GetNonMetadataSuffix(source)));
    }

    public static byte[] GetNonMetadataSuffix(byte[] data)
    {
        global::jpeg.JpegMarkerSegment[] segments = global::jpeg.JpegInspector.ReadMarkers(data);
        global::jpeg.JpegRawMarkerCollection rawMarkers = new global::jpeg.JpegRawMarkerCollection();
        foreach (global::jpeg.JpegMarkerSegment segment in segments)
        {
            if (segment.Value != global::jpeg.JpegConstants.MarkerStartOfImage && !rawMarkers.ShouldPreserve(segment))
            {
                int offset = (int)segment.MarkerOffset;
                byte[] suffix = new byte[data.Length - offset];
                System.Array.Copy(data, offset, suffix, 0, suffix.Length);
                return suffix;
            }
        }
        return new byte[0];
    }
}
