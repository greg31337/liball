namespace json.Tests;

public class JsonPathTrackerTests
{
    [Test]
    public void RootTokensUseTheEmptyPointerAndResetForReuse()
    {
        JsonPathTracker tracker = new JsonPathTracker();

        AssertPointer(tracker.GetCurrentPointer(), "");
        JsonPointer scalar = tracker.AcceptScalarValue();

        Assert.Multiple(() =>
        {
            AssertPointer(scalar, "");
            Assert.That(tracker.GetDepth(), Is.Zero);
            Assert.That(tracker.IsRootValueComplete(), Is.True);
        });

        Assert.Throws<InvalidOperationException>(() => tracker.AcceptScalarValue());

        tracker.ResetPath();

        Assert.Multiple(() =>
        {
            AssertPointer(tracker.GetCurrentPointer(), "");
            Assert.That(tracker.IsRootValueComplete(), Is.False);
        });
    }

    [Test]
    public void DecodedObjectNamesAreEscapedWithoutNormalization()
    {
        JsonPathTracker tracker = new JsonPathTracker();
        AssertPointer(tracker.OpenObject(), "");

        JsonPointer escapedName = tracker.AcceptPropertyName("a/~b");
        AssertPointer(escapedName, "/a~1~0b", "a/~b");
        AssertPointer(tracker.GetCurrentPointer(), "/a~1~0b", "a/~b");
        AssertPointer(tracker.AcceptScalarValue(), "/a~1~0b", "a/~b");

        JsonPointer emptyName = tracker.AcceptPropertyName("");
        AssertPointer(emptyName, "/", "");
        AssertPointer(tracker.AcceptScalarValue(), "/", "");

        string composedAndDecomposed = "é/e\u0301~";
        AssertPointer(
            tracker.AcceptPropertyName(composedAndDecomposed),
            "/é~1e\u0301~0",
            composedAndDecomposed);
        AssertPointer(tracker.AcceptScalarValue(), "/é~1e\u0301~0", composedAndDecomposed);

        AssertPointer(tracker.CloseObject(), "");
        Assert.That(tracker.IsRootValueComplete(), Is.True);
    }

    [Test]
    public void NestedArraysAndObjectsAdvanceIndexesAfterCompleteValues()
    {
        JsonPathTracker tracker = new JsonPathTracker();

        AssertPointer(tracker.OpenArray(), "");
        AssertPointer(tracker.AcceptScalarValue(), "/0", "0");
        AssertPointer(tracker.OpenObject(), "/1", "1");
        Assert.That(tracker.GetDepth(), Is.EqualTo(2));

        AssertPointer(tracker.AcceptPropertyName("x/y"), "/1/x~1y", "1", "x/y");
        AssertPointer(tracker.OpenArray(), "/1/x~1y", "1", "x/y");
        AssertPointer(tracker.AcceptScalarValue(), "/1/x~1y/0", "1", "x/y", "0");
        AssertPointer(tracker.CloseArray(), "/1/x~1y", "1", "x/y");

        AssertPointer(tracker.CloseObject(), "/1", "1");
        AssertPointer(tracker.GetCurrentPointer(), "/2", "2");
        AssertPointer(tracker.AcceptScalarValue(), "/2", "2");
        AssertPointer(tracker.CloseArray(), "");

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetDepth(), Is.Zero);
            Assert.That(tracker.IsRootValueComplete(), Is.True);
        });
    }

    [Test]
    public void ReturnedPointersAreIndependentSnapshots()
    {
        JsonPathTracker tracker = new JsonPathTracker();
        tracker.OpenArray();

        JsonPointer first = tracker.AcceptScalarValue();
        JsonPointer second = tracker.AcceptScalarValue();
        tracker.ResetPath();

        Assert.Multiple(() =>
        {
            AssertPointer(first, "/0", "0");
            AssertPointer(second, "/1", "1");
            Assert.That(first.ReferenceTokens, Is.Not.SameAs(second.ReferenceTokens));
        });
    }

    [Test]
    public void InvalidEventsDoNotCorruptTheActivePath()
    {
        JsonPathTracker tracker = new JsonPathTracker();
        tracker.OpenArray();

        Assert.Throws<InvalidOperationException>(() => tracker.AcceptPropertyName("name"));
        Assert.Throws<InvalidOperationException>(() => tracker.CloseObject());
        AssertPointer(tracker.GetCurrentPointer(), "/0", "0");

        tracker.OpenObject();
        Assert.Throws<InvalidOperationException>(() => tracker.AcceptScalarValue());
        AssertPointer(tracker.AcceptPropertyName("value"), "/0/value", "0", "value");
        Assert.Throws<InvalidOperationException>(() => tracker.AcceptPropertyName("other"));
        AssertPointer(tracker.AcceptScalarValue(), "/0/value", "0", "value");
        AssertPointer(tracker.CloseObject(), "/0", "0");
        AssertPointer(tracker.CloseArray(), "");
    }

    [Test]
    public void DeepPathsAreTrackedIteratively()
    {
        JsonPathTracker tracker = new JsonPathTracker();
        string expectedValuePointer = "";

        for (int depth = 0; depth < 64; depth++)
        {
            AssertPointer(tracker.OpenArray(), expectedValuePointer, CreateZeroTokens(depth));
            expectedValuePointer += "/0";
        }

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetDepth(), Is.EqualTo(64));
            AssertPointer(tracker.AcceptScalarValue(), expectedValuePointer, CreateZeroTokens(64));
        });

        for (int depth = 63; depth >= 0; depth--)
        {
            string expectedContainerPointer = CreateZeroPointer(depth);
            AssertPointer(tracker.CloseArray(), expectedContainerPointer, CreateZeroTokens(depth));
        }

        Assert.That(tracker.IsRootValueComplete(), Is.True);
    }

    static string[] CreateZeroTokens(int count)
    {
        string[] tokens = new string[count];
        for (int index = 0; index < count; index++)
        {
            tokens[index] = "0";
        }

        return tokens;
    }

    static string CreateZeroPointer(int count)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        for (int index = 0; index < count; index++)
        {
            builder.Append("/0");
        }

        return builder.ToString();
    }

    static void AssertPointer(JsonPointer pointer, string expectedRepresentation, params string[] expectedTokens)
    {
        Assert.Multiple(() =>
        {
            Assert.That(pointer.Encoding, Is.EqualTo(JsonPointerEncoding.String));
            Assert.That(pointer.OriginalRepresentation, Is.EqualTo(expectedRepresentation));
            Assert.That(pointer.ReferenceTokens, Is.EqualTo(expectedTokens));
        });
    }
}
