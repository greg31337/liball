using geo;

namespace geo.Tests;

public class SweepLineIndexTests
{
    [Test]
    public void MonotoneChainsSplitAtDirectionQuadrantChanges()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(3.0, 1.0),
            CoordinateValue(4.0, 0.0), CoordinateValue(3.0, -1.0)
        ], 0);

        MonotoneChain[] chains = MonotoneChain.CreateChains(source, 10);

        Assert.Multiple(() =>
        {
            Assert.That(chains, Has.Length.EqualTo(3));
            AssertChain(chains[0], 0, 2, 10, 0.0, 0.0, 2.0, 2.0);
            AssertChain(chains[1], 2, 4, 11, 2.0, 0.0, 4.0, 2.0);
            AssertChain(chains[2], 4, 5, 12, 3.0, -1.0, 4.0, 0.0);
            Assert.That(chains[0].Source, Is.SameAs(source));
        });
    }

    [Test]
    public void ChainDirectionHandlesOppositeExtremeFiniteCoordinates()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(-double.MaxValue, -double.MaxValue),
            CoordinateValue(double.MaxValue, double.MaxValue),
            CoordinateValue(double.MaxValue, -double.MaxValue)
        ], 0);

        MonotoneChain[] chains = MonotoneChain.CreateChains(source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(chains, Has.Length.EqualTo(2));
            Assert.That(chains[0].StartIndex, Is.Zero);
            Assert.That(chains[0].EndIndex, Is.EqualTo(1));
            Assert.That(chains[1].StartIndex, Is.EqualTo(1));
            Assert.That(chains[1].EndIndex, Is.EqualTo(2));
            Assert.That(chains[0].Bounds.MinX, Is.EqualTo(-double.MaxValue));
            Assert.That(chains[0].Bounds.MaxX, Is.EqualTo(double.MaxValue));
        });
    }

    [Test]
    public void MutualSweepEmitsOnlySegmentPairsWhoseEnvelopesOverlap()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0),
            CoordinateValue(20.0, 0.0)
        ], 0);
        SegmentString bent = SegmentStringValue([
            CoordinateValue(5.0, -1.0), CoordinateValue(5.0, 1.0),
            CoordinateValue(15.0, 1.0), CoordinateValue(15.0, -1.0)
        ], 1);
        SweepLineIndex index = new SweepLineIndex([horizontal, bent]);

        SegmentPairCandidate[] candidates = index.GetCandidatePairs(false);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Length.EqualTo(2));
            AssertCandidate(candidates[0], horizontal, 0, bent, 0);
            AssertCandidate(candidates[1], horizontal, 1, bent, 2);
            Assert.That(candidates[0].FirstChainId, Is.LessThan(candidates[0].SecondChainId));
            Assert.That(candidates[1].FirstChainId, Is.LessThan(candidates[1].SecondChainId));
        });
    }

    [Test]
    public void SweepIncludesTouchingZeroWidthIntervalsAtTheSameOrdinate()
    {
        SegmentString horizontal = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0)
        ], 0);
        SegmentString vertical = SegmentStringValue([
            CoordinateValue(1.0, 0.0), CoordinateValue(1.0, 1.0)
        ], 1);
        SweepLineIndex index = new SweepLineIndex([horizontal, vertical]);

        SegmentPairCandidate[] candidates = index.GetCandidatePairs(false);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Length.EqualTo(1));
            AssertCandidate(candidates[0], horizontal, 0, vertical, 0);
            SweepLineEvent[] atOne = index.Events.Where(value => value.X == 1.0).ToArray();
            Assert.That(atOne[0].IsInsert, Is.True);
            Assert.That(atOne[atOne.Length - 1].IsInsert, Is.False);
        });
    }

    [Test]
    public void SelfCandidateModeFindsAdjacentAndCrossingChainPairsOnce()
    {
        SegmentString bowTie = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 2.0), CoordinateValue(2.0, 0.0)
        ], 0);
        SweepLineIndex index = new SweepLineIndex([bowTie]);

        SegmentPairCandidate[] mutualOnly = index.GetCandidatePairs(false);
        SegmentPairCandidate[] self = index.GetCandidatePairs(true);

        Assert.Multiple(() =>
        {
            Assert.That(mutualOnly, Is.Empty);
            Assert.That(self, Has.Length.EqualTo(3));
            AssertCandidate(self[0], bowTie, 0, bowTie, 1);
            AssertCandidate(self[1], bowTie, 0, bowTie, 2);
            AssertCandidate(self[2], bowTie, 1, bowTie, 2);
            Assert.That(self.Select(value =>
                value.FirstSegmentIndex + ":" + value.SecondSegmentIndex).Distinct().Count(),
                Is.EqualTo(3));
        });
    }

    [Test]
    public void CandidateOrderingIsStableAcrossRepeatedEnumeration()
    {
        SegmentString first = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 4.0), CoordinateValue(8.0, 0.0)
        ], 0);
        SegmentString second = SegmentStringValue([
            CoordinateValue(0.0, 3.0), CoordinateValue(8.0, 3.0)
        ], 1);
        SegmentString third = SegmentStringValue([
            CoordinateValue(2.0, -1.0), CoordinateValue(2.0, 5.0)
        ], 2);
        SweepLineIndex index = new SweepLineIndex([first, second, third]);

        SegmentPairCandidate[] firstRun = index.GetCandidatePairs(true);
        SegmentPairCandidate[] secondRun = index.GetCandidatePairs(true);

        Assert.Multiple(() =>
        {
            Assert.That(firstRun, Has.Length.EqualTo(secondRun.Length));
            int candidateIndex = 0;
            while (candidateIndex < firstRun.Length)
            {
                Assert.That(firstRun[candidateIndex].First, Is.SameAs(secondRun[candidateIndex].First));
                Assert.That(firstRun[candidateIndex].FirstSegmentIndex,
                    Is.EqualTo(secondRun[candidateIndex].FirstSegmentIndex));
                Assert.That(firstRun[candidateIndex].Second, Is.SameAs(secondRun[candidateIndex].Second));
                Assert.That(firstRun[candidateIndex].SecondSegmentIndex,
                    Is.EqualTo(secondRun[candidateIndex].SecondSegmentIndex));
                candidateIndex++;
            }
        });
    }

    [Test]
    public void IndexRejectsNullSourcesAndInvalidChainRanges()
    {
        SegmentString source = SegmentStringValue([
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0)
        ], 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => new SweepLineIndex(null!));
            Assert.Throws<InvalidGeometryException>(() => new SweepLineIndex([source, null!]));
            Assert.Throws<InvalidGeometryException>(() => MonotoneChain.CreateChains(source, -1));
            Assert.Throws<InvalidGeometryException>(() => new MonotoneChain(source, 1, 0, 0));
            MonotoneChain chain = new MonotoneChain(source, 0, 1, 0);
            Assert.Throws<InvalidGeometryException>(() => chain.GetSegmentEnvelope(1));
        });
    }

    private static void AssertChain(
        MonotoneChain chain, int start, int end, int id,
        double minX, double minY, double maxX, double maxY)
    {
        Assert.That(chain.StartIndex, Is.EqualTo(start));
        Assert.That(chain.EndIndex, Is.EqualTo(end));
        Assert.That(chain.Id, Is.EqualTo(id));
        Assert.That(chain.Bounds.MinX, Is.EqualTo(minX));
        Assert.That(chain.Bounds.MinY, Is.EqualTo(minY));
        Assert.That(chain.Bounds.MaxX, Is.EqualTo(maxX));
        Assert.That(chain.Bounds.MaxY, Is.EqualTo(maxY));
    }

    private static void AssertCandidate(
        SegmentPairCandidate candidate,
        SegmentString first, int firstIndex,
        SegmentString second, int secondIndex)
    {
        Assert.That(candidate.First, Is.SameAs(first));
        Assert.That(candidate.FirstSegmentIndex, Is.EqualTo(firstIndex));
        Assert.That(candidate.Second, Is.SameAs(second));
        Assert.That(candidate.SecondSegmentIndex, Is.EqualTo(secondIndex));
    }

    private static SegmentString SegmentStringValue(Coordinate[] coordinates, int sourceIndex)
    {
        return SegmentNoding.CreateSegmentString(coordinates, null, sourceIndex);
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
