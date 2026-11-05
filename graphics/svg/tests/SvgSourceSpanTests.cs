namespace svg.Tests;

public class SvgSourceSpanTests
{
    [Test]
    public void PositionSupportsOffsetAndLineCoordinateSystems()
    {
        SvgSourcePosition first = new SvgSourcePosition(4, 2, 3);
        SvgSourcePosition second = new SvgSourcePosition(9, 2, 8);
        SvgSourcePosition lineOnly = new SvgSourcePosition(-1, 7, 1);
        SvgSourcePosition unknown = SvgSourcePosition.Unknown();

        Assert.Multiple(() =>
        {
            Assert.That(first.HasOffset(), Is.True);
            Assert.That(first.HasLineColumn(), Is.True);
            Assert.That(first.CompareTo(second), Is.LessThan(0));
            Assert.That(lineOnly.HasOffset(), Is.False);
            Assert.That(lineOnly.HasLineColumn(), Is.True);
            Assert.That(unknown.HasOffset(), Is.False);
            Assert.That(unknown.HasLineColumn(), Is.False);
            Assert.That(unknown.Offset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void PositionRejectsMalformedCoordinates()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSourcePosition(-2, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSourcePosition(0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSourcePosition(0, 0, -1));
            Assert.Throws<ArgumentException>(() => new SvgSourcePosition(0, 1, 0));
            Assert.Throws<ArgumentException>(() => new SvgSourcePosition(0, 0, 1));
            Assert.Throws<InvalidOperationException>(() => SvgSourcePosition.Unknown().CompareTo(new SvgSourcePosition(0, 0, 0)));
        });
    }

    [Test]
    public void SpanUsesHalfOpenContainmentAndOverlap()
    {
        SvgSourceUri source = new SvgSourceUri("file:///drawing.svg");
        SvgSourceSpan span = new SvgSourceSpan(source, 10, 2, 1, 20, 2, 11);
        SvgSourceSpan inner = new SvgSourceSpan(source, 12, 2, 3, 18, 2, 9);
        SvgSourceSpan touching = new SvgSourceSpan(source, 20, 2, 11, 25, 2, 16);
        SvgSourceSpan emptyAtEnd = new SvgSourceSpan(source, 20, 2, 11, 20, 2, 11);

        Assert.Multiple(() =>
        {
            Assert.That(span.Length(), Is.EqualTo(10));
            Assert.That(span.ContainsOffset(10), Is.True);
            Assert.That(span.ContainsOffset(19), Is.True);
            Assert.That(span.ContainsOffset(20), Is.False);
            Assert.That(span.ContainsSpan(inner), Is.True);
            Assert.That(span.ContainsSpan(emptyAtEnd), Is.True);
            Assert.That(span.Overlaps(inner), Is.True);
            Assert.That(span.Overlaps(touching), Is.False);
            Assert.That(span.Overlaps(emptyAtEnd), Is.False);
        });
    }

    [Test]
    public void SpanFallsBackToLineAndColumnWhenOffsetsAreUnavailable()
    {
        SvgSourceUri source = new SvgSourceUri(string.Empty);
        SvgSourceSpan outer = new SvgSourceSpan(source, -1, 3, 4, -1, 5, 2);
        SvgSourceSpan inner = new SvgSourceSpan(source, -1, 4, 1, -1, 4, 9);
        SvgSourceSpan after = new SvgSourceSpan(source, -1, 5, 2, -1, 6, 1);

        Assert.Multiple(() =>
        {
            Assert.That(outer.Length(), Is.EqualTo(-1));
            Assert.That(outer.ContainsSpan(inner), Is.True);
            Assert.That(outer.Overlaps(inner), Is.True);
            Assert.That(outer.Overlaps(after), Is.False);
        });
    }

    [Test]
    public void SpanRejectsMismatchedOrReversedEndpoints()
    {
        SvgSourceUri source = new SvgSourceUri("memory:source");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => SvgSourceSpan.FromPositions(
                source,
                new SvgSourcePosition(1, 0, 0),
                new SvgSourcePosition(-1, 0, 0)));
            Assert.Throws<ArgumentException>(() => new SvgSourceSpan(source, 8, 1, 1, 7, 1, 2));
            Assert.Throws<ArgumentException>(() => new SvgSourceSpan(source, 1, 3, 4, 2, 3, 3));
        });
    }

    [Test]
    public void NestedResourcesRetainStablePathsAndProvenance()
    {
        SvgSourceUri root = new SvgSourceUri("https://example.test/root.svg");
        SvgSourceSpan rootReference = new SvgSourceSpan(root, 30, 3, 5, 42, 3, 17);
        SvgSourceUri child = SvgSourceUri.CreateNested("icons.svg", root, rootReference, 2);
        SvgSourceSpan childReference = new SvgSourceSpan(child, 6, 1, 7, 14, 1, 15);
        SvgSourceUri grandchild = SvgSourceUri.CreateNested("more.svg", child, childReference, 1);
        SvgSourceSpan leaf = new SvgSourceSpan(grandchild, 4, 1, 5, 9, 1, 10);

        List<SvgSourceSpan> provenance = new SvgSourceMapping().GetProvenance(leaf);

        Assert.Multiple(() =>
        {
            Assert.That(root.ResourcePath, Is.EqualTo("0"));
            Assert.That(child.ResourcePath, Is.EqualTo("0/0000000002"));
            Assert.That(grandchild.ResourcePath, Is.EqualTo("0/0000000002/0000000001"));
            Assert.That(grandchild.Depth, Is.EqualTo(2));
            Assert.That(provenance, Has.Count.EqualTo(3));
            Assert.That(provenance[0], Is.SameAs(leaf));
            Assert.That(provenance[1], Is.SameAs(childReference));
            Assert.That(provenance[2], Is.SameAs(rootReference));
        });
    }

    [Test]
    public void NestedResourceRejectsReferenceFromAnotherSource()
    {
        SvgSourceUri parent = new SvgSourceUri("parent.svg");
        SvgSourceUri other = new SvgSourceUri("other.svg");
        SvgSourceSpan wrongReference = new SvgSourceSpan(other, 0, 1, 1, 1, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => SvgSourceUri.CreateNested("child.svg", parent, wrongReference, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgSourceUri.CreateNested("child.svg", parent, SvgSourceSpan.Unknown(parent), -1));
        });
    }

    [Test]
    public void MappingComparisonIsDeterministicForOffsetsAndResourceOrdinals()
    {
        SvgSourceUri root = new SvgSourceUri("root.svg");
        SvgSourceSpan reference = new SvgSourceSpan(root, 0, 1, 1, 1, 1, 2);
        SvgSourceUri childTwo = SvgSourceUri.CreateNested("z-last-by-name.svg", root, reference, 2);
        SvgSourceUri childTen = SvgSourceUri.CreateNested("a-first-by-name.svg", root, reference, 10);
        SvgSourceSpan early = new SvgSourceSpan(childTwo, 3, 1, 4, 5, 1, 6);
        SvgSourceSpan late = new SvgSourceSpan(childTwo, 8, 1, 9, 9, 1, 10);
        SvgSourceSpan otherResource = new SvgSourceSpan(childTen, 0, 1, 1, 1, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(new SvgSourceMapping().Compare(early, late), Is.LessThan(0));
            Assert.That(new SvgSourceMapping().Compare(late, early), Is.GreaterThan(0));
            Assert.That(new SvgSourceMapping().Compare(early, otherResource), Is.LessThan(0));
            Assert.That(new SvgSourceMapping().Compare(early, early), Is.Zero);
        });
    }

    [Test]
    public void MappingComparisonOrdersRootsAndUnknownLocationsDeterministically()
    {
        SvgSourceUri firstRoot = new SvgSourceUri("a.svg");
        SvgSourceUri secondRoot = new SvgSourceUri("b.svg");
        SvgSourceSpan known = new SvgSourceSpan(firstRoot, 2, 1, 3, 3, 1, 4);
        SvgSourceSpan unknown = SvgSourceSpan.Unknown(firstRoot);
        SvgSourceSpan otherRoot = new SvgSourceSpan(secondRoot, 0, 1, 1, 1, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(new SvgSourceMapping().Compare(known, unknown), Is.LessThan(0));
            Assert.That(new SvgSourceMapping().Compare(unknown, known), Is.GreaterThan(0));
            Assert.That(new SvgSourceMapping().Compare(unknown, SvgSourceSpan.Unknown(firstRoot)), Is.Zero);
            Assert.That(new SvgSourceMapping().Compare(known, otherRoot), Is.LessThan(0));
        });
    }
}
