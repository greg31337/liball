using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class SourceTrackingTests
{
    [Test]
    public void TrackerAdvancesByScalarsAcrossNormalizedLinesAndRawBytes()
    {
        SourceTracker tracker = CreateByteTracker("A\r\n" + char.ConvertFromUtf32(0x1F600) + "B", "file:///document.xml");

        AssertLocation(tracker.CurrentTrackedLocation(), "file:///document.xml", 0, 3, 1, 1);
        Assert.That(tracker.MarkTrackedOffset(), Is.Zero);
        Assert.That(tracker.RemainingTrackedUtf16(), Is.EqualTo(5));

        tracker.AdvanceTrackedScalar();
        AssertLocation(tracker.CurrentTrackedLocation(), "file:///document.xml", 1, 4, 1, 2);
        tracker.AdvanceTrackedScalar();
        AssertLocation(tracker.CurrentTrackedLocation(), "file:///document.xml", 2, 6, 2, 1);
        tracker.AdvanceTrackedScalar();
        AssertLocation(tracker.CurrentTrackedLocation(), "file:///document.xml", 4, 10, 2, 2);
        tracker.AdvanceTrackedScalar();
        AssertLocation(tracker.CurrentTrackedLocation(), "file:///document.xml", 5, 11, 2, 3);
        Assert.That(tracker.IsTrackedSourceEnd(), Is.True);
        Assert.That(tracker.RemainingTrackedUtf16(), Is.Zero);
        Assert.That(() => tracker.AdvanceTrackedScalar(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void TrackerCreatesHalfOpenRangesAndRejectsMidScalarOffsets()
    {
        SourceTracker tracker = CreateByteTracker("A\r\n" + char.ConvertFromUtf32(0x1F600) + "B", "urn:source");
        tracker.AdvanceTrackedUtf16(2);
        int start = tracker.MarkTrackedOffset();
        tracker.AdvanceTrackedUtf16(2);

        SourceRange range = tracker.CreateTrackedRangeToCurrent(start);

        AssertLocation(range.Start!, "urn:source", 2, 6, 2, 1);
        AssertLocation(range.End!, "urn:source", 4, 10, 2, 2);
        Assert.That(() => tracker.SetTrackedOffset(3), Throws.ArgumentException);
        Assert.That(() => tracker.TrackedLocationAt(3), Throws.ArgumentException);
        tracker.SetTrackedOffset(2);
        Assert.That(() => tracker.AdvanceTrackedUtf16(1), Throws.ArgumentException);
        Assert.That(() => tracker.AdvanceTrackedUtf16(10), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => tracker.CreateTrackedRange(4, 2), Throws.ArgumentException);
        Assert.That(() => tracker.CreateTrackedRange(-1, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void CharacterSourcesRetainUnknownByteOffsetsAndOneBasedLocations()
    {
        SourceTracker tracker = CreateCharacterTracker("first\rsecond", null);

        SourceLocation start = tracker.CurrentTrackedLocation();
        tracker.AdvanceTrackedUtf16(6);
        SourceLocation secondLine = tracker.CurrentTrackedLocation();
        SourceRange range = tracker.CreateTrackedRange(0, 6);

        AssertLocation(start, null, 0, -1, 1, 1);
        AssertLocation(secondLine, null, 6, -1, 2, 1);
        AssertLocation(range.Start!, null, 0, -1, 1, 1);
        AssertLocation(range.End!, null, 6, -1, 2, 1);
    }

    [Test]
    public void SourceStackSwitchesNestedTrackersAndResumesParentCursors()
    {
        SourceTracker document = CreateCharacterTracker("doc", "file:///doc.xml");
        SourceTracker entity = CreateCharacterTracker("entity", "file:///entity.ent");
        SourceTracker include = CreateCharacterTracker("include", "file:///include.xml");
        SourceStack stack = new SourceStack();

        stack.PushDocumentSource(document, "document");
        document.AdvanceTrackedScalar();
        stack.PushEntitySource(entity, "general:chapter");
        entity.AdvanceTrackedUtf16(2);
        stack.PushIncludeSource(include, "include.xml");
        include.AdvanceTrackedUtf16(3);

        Assert.That(stack.SourceFrameDepth(), Is.EqualTo(3));
        Assert.That(stack.CurrentSourceTracker(), Is.SameAs(include));
        Assert.That(stack.CreateEntitySourceTrace(), Is.EqualTo(new string[] { "document", "general:chapter" }));
        Assert.That(stack.CreateIncludeSourceTrace(), Is.EqualTo(new string[] { "include.xml" }));

        Assert.That(stack.PopSourceFrame(), Is.SameAs(include));
        Assert.That(stack.CurrentSourceTracker(), Is.SameAs(entity));
        Assert.That(entity.MarkTrackedOffset(), Is.EqualTo(2));
        Assert.That(stack.PopSourceFrame(), Is.SameAs(entity));
        Assert.That(stack.CurrentSourceTracker(), Is.SameAs(document));
        Assert.That(document.MarkTrackedOffset(), Is.EqualTo(1));
    }

    [Test]
    public void DiagnosticTraceSnapshotsNestedEntityAndIncludeContexts()
    {
        SourceStack stack = new SourceStack();
        SourceTracker document = CreateCharacterTracker("root", "file:///root.xml");
        SourceTracker firstEntity = CreateCharacterTracker("entity", "file:///first.ent");
        SourceTracker include = CreateCharacterTracker("included", "file:///included.xml");
        SourceTracker secondEntity = CreateCharacterTracker("value", "file:///second.ent");
        stack.PushDocumentSource(document, "document:file:///root.xml");
        stack.PushEntitySource(firstEntity, "entity:first");
        stack.PushIncludeSource(include, "include:file:///included.xml");
        stack.PushEntitySource(secondEntity, "entity:second");
        secondEntity.AdvanceTrackedUtf16(3);
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.MalformedMarkup;
        diagnostic.TemplateStack = ["template"];

        XmlDiagnostic captured = stack.CaptureDiagnosticSourceTrace(diagnostic, 1);

        Assert.That(captured, Is.SameAs(diagnostic));
        Assert.That(diagnostic.EntityStack, Is.EqualTo(new string[] { "document:file:///root.xml", "entity:first", "entity:second" }));
        Assert.That(diagnostic.IncludeStack, Is.EqualTo(new string[] { "include:file:///included.xml" }));
        Assert.That(diagnostic.TemplateStack, Is.EqualTo(new string[] { "template" }));
        AssertLocation(diagnostic.Range!.Start!, "file:///second.ent", 1, -1, 1, 2);
        AssertLocation(diagnostic.Range.End!, "file:///second.ent", 3, -1, 1, 4);

        stack.PopSourceFrame();
        stack.PopSourceFrame();
        Assert.That(diagnostic.EntityStack, Is.EqualTo(new string[] { "document:file:///root.xml", "entity:first", "entity:second" }));
        Assert.That(diagnostic.IncludeStack, Is.EqualTo(new string[] { "include:file:///included.xml" }));
    }

    [Test]
    public void SourceStackRejectsInvalidNestingAndEmptyOperations()
    {
        SourceStack stack = new SourceStack();
        SourceTracker tracker = CreateCharacterTracker("x", "urn:x");

        Assert.That(() => stack.CurrentSourceTracker(), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => stack.PopSourceFrame(), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => stack.PushEntitySource(tracker, "entity"), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => stack.PushIncludeSource(tracker, "include"), Throws.TypeOf<InvalidOperationException>());
        stack.PushDocumentSource(tracker, "document");
        Assert.That(() => stack.PushDocumentSource(tracker, "other"), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => stack.PushEntitySource(tracker, ""), Throws.ArgumentException);
        Assert.That(() => stack.CaptureDiagnosticSourceTrace(new XmlDiagnostic(), 2), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static SourceTracker CreateByteTracker(string rawText, string sourceUri)
    {
        Encoding encoding = new UTF8Encoding(true, true);
        byte[] bytes = JoinBytes(encoding.GetPreamble(), encoding.GetBytes(rawText));
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        XmlDecodedInput decoded = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 3).DecodeXmlInput();
        XmlLineNormalizationResult normalized = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new SourceTracker(sourceUri, normalized);
    }

    private static SourceTracker CreateCharacterTracker(string rawText, string? sourceUri)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = rawText;
        XmlDecodedInput decoded = new XmlInputDecoder(input, XmlEncodingEntityKind.Document, EncodingPolicyKind.UnicodeOnly, 4).DecodeXmlInput();
        XmlLineNormalizationResult normalized = LineNormalizer.NormalizeXmlLineEndings(decoded, XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new SourceTracker(sourceUri, normalized);
    }

    private static byte[] JoinBytes(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private static void AssertLocation(SourceLocation location, string? sourceUri, long characterOffset, long byteOffset, int line, int column)
    {
        Assert.That(location.SourceUri, Is.EqualTo(sourceUri));
        Assert.That(location.CharacterOffset, Is.EqualTo(characterOffset));
        Assert.That(location.ByteOffset, Is.EqualTo(byteOffset));
        Assert.That(location.Line, Is.EqualTo(line));
        Assert.That(location.Column, Is.EqualTo(column));
    }
}
