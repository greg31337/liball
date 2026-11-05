namespace diameter.Tests;

public class StatisticsCollectorTests
{
    [Test]
    public void EveryStatisticMapsToItsSnapshotField()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        StatisticsCollector collector = new StatisticsCollector(clock);

        collector.SetDiameterStatistic(DiameterStatistic.BytesReceived, 1);
        collector.SetDiameterStatistic(DiameterStatistic.BytesSent, 2);
        collector.SetDiameterStatistic(DiameterStatistic.MessagesReceived, 3);
        collector.SetDiameterStatistic(DiameterStatistic.MessagesSent, 4);
        collector.SetDiameterStatistic(DiameterStatistic.AvpsDecoded, 5);
        collector.SetDiameterStatistic(DiameterStatistic.AvpsEncoded, 6);
        collector.SetDiameterStatistic(DiameterStatistic.DecodeErrors, 7);
        collector.SetDiameterStatistic(DiameterStatistic.ValidationErrors, 8);
        collector.SetDiameterStatistic(DiameterStatistic.PeersConfigured, 9);
        collector.SetDiameterStatistic(DiameterStatistic.PeersConnected, 10);
        collector.SetDiameterStatistic(DiameterStatistic.PeerConnectionsOpened, 11);
        collector.SetDiameterStatistic(DiameterStatistic.PeerConnectionsClosed, 12);
        collector.SetDiameterStatistic(DiameterStatistic.WatchdogsSent, 13);
        collector.SetDiameterStatistic(DiameterStatistic.WatchdogAnswersReceived, 14);
        collector.SetDiameterStatistic(DiameterStatistic.WatchdogTimeouts, 15);
        collector.SetDiameterStatistic(DiameterStatistic.RequestsReceived, 16);
        collector.SetDiameterStatistic(DiameterStatistic.RequestsSent, 17);
        collector.SetDiameterStatistic(DiameterStatistic.AnswersReceived, 18);
        collector.SetDiameterStatistic(DiameterStatistic.AnswersSent, 19);
        collector.SetDiameterStatistic(DiameterStatistic.RequestsPending, 20);
        collector.SetDiameterStatistic(DiameterStatistic.RequestTimeouts, 21);
        collector.SetDiameterStatistic(DiameterStatistic.Retransmissions, 22);
        collector.SetDiameterStatistic(DiameterStatistic.DuplicateRequests, 23);
        collector.SetDiameterStatistic(DiameterStatistic.RoutesAvailable, 24);
        collector.SetDiameterStatistic(DiameterStatistic.RouteSelections, 25);
        collector.SetDiameterStatistic(DiameterStatistic.RouteFailures, 26);
        collector.SetDiameterStatistic(DiameterStatistic.RedirectsReceived, 27);
        collector.SetDiameterStatistic(DiameterStatistic.RedirectsApplied, 28);
        collector.SetDiameterStatistic(DiameterStatistic.SessionsActive, 29);
        collector.SetDiameterStatistic(DiameterStatistic.SessionsStarted, 30);
        collector.SetDiameterStatistic(DiameterStatistic.SessionsEnded, 31);
        collector.SetDiameterStatistic(DiameterStatistic.AccountingRecordsSent, 32);
        collector.SetDiameterStatistic(DiameterStatistic.AccountingRecordsReceived, 33);
        collector.SetDiameterStatistic(DiameterStatistic.AccountingFailures, 34);
        collector.SetDiameterStatistic(DiameterStatistic.CreditControlRequests, 35);
        collector.SetDiameterStatistic(DiameterStatistic.CreditControlAnswers, 36);
        collector.SetDiameterStatistic(DiameterStatistic.CreditControlFailures, 37);
        collector.SetDiameterStatistic(DiameterStatistic.LoadReportsSent, 38);
        collector.SetDiameterStatistic(DiameterStatistic.LoadReportsReceived, 39);
        collector.SetDiameterStatistic(DiameterStatistic.OverloadReportsSent, 40);
        collector.SetDiameterStatistic(DiameterStatistic.OverloadReportsReceived, 41);
        collector.SetDiameterStatistic(DiameterStatistic.OverloadRejections, 42);
        collector.SetDiameterStatistic(DiameterStatistic.QueueDepth, 43);
        collector.SetDiameterStatistic(DiameterStatistic.QueueRejections, 44);
        collector.SetDiameterStatistic(DiameterStatistic.StoreReads, 45);
        collector.SetDiameterStatistic(DiameterStatistic.StoreWrites, 46);
        collector.SetDiameterStatistic(DiameterStatistic.StoreFailures, 47);

        DiameterNodeStatistics result = collector.ReadDiameterNodeStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(result.CapturedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(result.BytesReceived, Is.EqualTo(1));
            Assert.That(result.BytesSent, Is.EqualTo(2));
            Assert.That(result.MessagesReceived, Is.EqualTo(3));
            Assert.That(result.MessagesSent, Is.EqualTo(4));
            Assert.That(result.AvpsDecoded, Is.EqualTo(5));
            Assert.That(result.AvpsEncoded, Is.EqualTo(6));
            Assert.That(result.DecodeErrors, Is.EqualTo(7));
            Assert.That(result.ValidationErrors, Is.EqualTo(8));
            Assert.That(result.PeersConfigured, Is.EqualTo(9));
            Assert.That(result.PeersConnected, Is.EqualTo(10));
            Assert.That(result.PeerConnectionsOpened, Is.EqualTo(11));
            Assert.That(result.PeerConnectionsClosed, Is.EqualTo(12));
            Assert.That(result.WatchdogsSent, Is.EqualTo(13));
            Assert.That(result.WatchdogAnswersReceived, Is.EqualTo(14));
            Assert.That(result.WatchdogTimeouts, Is.EqualTo(15));
            Assert.That(result.RequestsReceived, Is.EqualTo(16));
            Assert.That(result.RequestsSent, Is.EqualTo(17));
            Assert.That(result.AnswersReceived, Is.EqualTo(18));
            Assert.That(result.AnswersSent, Is.EqualTo(19));
            Assert.That(result.RequestsPending, Is.EqualTo(20));
            Assert.That(result.RequestTimeouts, Is.EqualTo(21));
            Assert.That(result.Retransmissions, Is.EqualTo(22));
            Assert.That(result.DuplicateRequests, Is.EqualTo(23));
            Assert.That(result.RoutesAvailable, Is.EqualTo(24));
            Assert.That(result.RouteSelections, Is.EqualTo(25));
            Assert.That(result.RouteFailures, Is.EqualTo(26));
            Assert.That(result.RedirectsReceived, Is.EqualTo(27));
            Assert.That(result.RedirectsApplied, Is.EqualTo(28));
            Assert.That(result.SessionsActive, Is.EqualTo(29));
            Assert.That(result.SessionsStarted, Is.EqualTo(30));
            Assert.That(result.SessionsEnded, Is.EqualTo(31));
            Assert.That(result.AccountingRecordsSent, Is.EqualTo(32));
            Assert.That(result.AccountingRecordsReceived, Is.EqualTo(33));
            Assert.That(result.AccountingFailures, Is.EqualTo(34));
            Assert.That(result.CreditControlRequests, Is.EqualTo(35));
            Assert.That(result.CreditControlAnswers, Is.EqualTo(36));
            Assert.That(result.CreditControlFailures, Is.EqualTo(37));
            Assert.That(result.LoadReportsSent, Is.EqualTo(38));
            Assert.That(result.LoadReportsReceived, Is.EqualTo(39));
            Assert.That(result.OverloadReportsSent, Is.EqualTo(40));
            Assert.That(result.OverloadReportsReceived, Is.EqualTo(41));
            Assert.That(result.OverloadRejections, Is.EqualTo(42));
            Assert.That(result.QueueDepth, Is.EqualTo(43));
            Assert.That(result.QueueRejections, Is.EqualTo(44));
            Assert.That(result.StoreReads, Is.EqualTo(45));
            Assert.That(result.StoreWrites, Is.EqualTo(46));
            Assert.That(result.StoreFailures, Is.EqualTo(47));
        });
    }

    [Test]
    public void ConcurrentChangesDoNotLoseUpdates()
    {
        StatisticsCollector collector =
            new StatisticsCollector(new FakeDiscoveryClock());

        Parallel.For(
            0,
            50000,
            index =>
            {
                collector.ChangeDiameterStatistic(
                    DiameterStatistic.MessagesReceived,
                    1);
                collector.ChangeDiameterStatistic(
                    DiameterStatistic.QueueDepth,
                    1);
                collector.ChangeDiameterStatistic(
                    DiameterStatistic.QueueDepth,
                    -1);
            });

        DiameterNodeStatistics result = collector.ReadDiameterNodeStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(result.MessagesReceived, Is.EqualTo(50000));
            Assert.That(result.QueueDepth, Is.Zero);
        });
    }

    [Test]
    public void SnapshotsRemainIndependentFromCollectorAndEachOther()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        StatisticsCollector collector = new StatisticsCollector(clock);
        collector.SetDiameterStatistic(DiameterStatistic.RequestsSent, 3);

        DiameterNodeStatistics first = collector.ReadDiameterNodeStatistics();
        first.RequestsSent = 900;
        clock.AdvanceDiscoveryTime(1);
        collector.ChangeDiameterStatistic(DiameterStatistic.RequestsSent, 2);
        DiameterNodeStatistics second = collector.ReadDiameterNodeStatistics();
        collector.ChangeDiameterStatistic(DiameterStatistic.RequestsSent, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.RequestsSent, Is.EqualTo(900));
            Assert.That(second.RequestsSent, Is.EqualTo(5));
            Assert.That(second.CapturedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [Test]
    public void InvalidUpdatesAreRejectedWithoutChangingCounters()
    {
        StatisticsCollector collector =
            new StatisticsCollector(new FakeDiscoveryClock());
        collector.SetDiameterStatistic(DiameterStatistic.StoreWrites, 4);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                collector.ChangeDiameterStatistic(
                    DiameterStatistic.StoreWrites,
                    -5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                collector.SetDiameterStatistic(
                    DiameterStatistic.QueueDepth,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                collector.ChangeDiameterStatistic(
                    (DiameterStatistic)1000,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                collector.SetDiameterStatistic(
                    (DiameterStatistic)(-1),
                    1));
        });

        Assert.That(
            collector.ReadDiameterNodeStatistics().StoreWrites,
            Is.EqualTo(4));
    }

    [Test]
    public void CollectorRequiresAClock()
    {
        Assert.Throws<ArgumentException>(() =>
            new StatisticsCollector(null!));
    }
}
