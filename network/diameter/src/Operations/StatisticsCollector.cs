using System;

namespace diameter;

internal enum DiameterStatistic
{
    BytesReceived = 0,
    BytesSent = 1,
    MessagesReceived = 2,
    MessagesSent = 3,
    AvpsDecoded = 4,
    AvpsEncoded = 5,
    DecodeErrors = 6,
    ValidationErrors = 7,
    PeersConfigured = 8,
    PeersConnected = 9,
    PeerConnectionsOpened = 10,
    PeerConnectionsClosed = 11,
    WatchdogsSent = 12,
    WatchdogAnswersReceived = 13,
    WatchdogTimeouts = 14,
    RequestsReceived = 15,
    RequestsSent = 16,
    AnswersReceived = 17,
    AnswersSent = 18,
    RequestsPending = 19,
    RequestTimeouts = 20,
    Retransmissions = 21,
    DuplicateRequests = 22,
    RoutesAvailable = 23,
    RouteSelections = 24,
    RouteFailures = 25,
    RedirectsReceived = 26,
    RedirectsApplied = 27,
    SessionsActive = 28,
    SessionsStarted = 29,
    SessionsEnded = 30,
    AccountingRecordsSent = 31,
    AccountingRecordsReceived = 32,
    AccountingFailures = 33,
    CreditControlRequests = 34,
    CreditControlAnswers = 35,
    CreditControlFailures = 36,
    LoadReportsSent = 37,
    LoadReportsReceived = 38,
    OverloadReportsSent = 39,
    OverloadReportsReceived = 40,
    OverloadRejections = 41,
    QueueDepth = 42,
    QueueRejections = 43,
    StoreReads = 44,
    StoreWrites = 45,
    StoreFailures = 46,
    Count = 47
}

internal class StatisticsCollector
{
    internal object StatisticsLock = new object();
    internal long[] Values = new long[(int)DiameterStatistic.Count];
    internal IDiameterClock Clock;

    internal StatisticsCollector(IDiameterClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentException(
                "The statistics collector requires a clock.");
        }

        Clock = clock;
    }

    internal void ChangeDiameterStatistic(
        DiameterStatistic statistic,
        long amount)
    {
        int index = (int)statistic;
        if (index < 0 || index >= (int)DiameterStatistic.Count)
        {
            throw new ArgumentOutOfRangeException("statistic");
        }

        lock (StatisticsLock)
        {
            long updated = Values[index] + amount;
            if (updated < 0L)
            {
                throw new ArgumentOutOfRangeException("amount");
            }

            Values[index] = updated;
        }
    }

    internal void SetDiameterStatistic(
        DiameterStatistic statistic,
        long value)
    {
        int index = (int)statistic;
        if (index < 0 || index >= (int)DiameterStatistic.Count)
        {
            throw new ArgumentOutOfRangeException("statistic");
        }

        if (value < 0L)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        lock (StatisticsLock)
        {
            Values[index] = value;
        }
    }

    internal DiameterNodeStatistics ReadDiameterNodeStatistics()
    {
        DateTimeOffset capturedAt = Clock.ReadUtcTime();
        lock (StatisticsLock)
        {
            DiameterNodeStatistics result = new DiameterNodeStatistics();
            result.CapturedAt = capturedAt;
            result.BytesReceived = Values[(int)DiameterStatistic.BytesReceived];
            result.BytesSent = Values[(int)DiameterStatistic.BytesSent];
            result.MessagesReceived = Values[(int)DiameterStatistic.MessagesReceived];
            result.MessagesSent = Values[(int)DiameterStatistic.MessagesSent];
            result.AvpsDecoded = Values[(int)DiameterStatistic.AvpsDecoded];
            result.AvpsEncoded = Values[(int)DiameterStatistic.AvpsEncoded];
            result.DecodeErrors = Values[(int)DiameterStatistic.DecodeErrors];
            result.ValidationErrors = Values[(int)DiameterStatistic.ValidationErrors];
            result.PeersConfigured = Values[(int)DiameterStatistic.PeersConfigured];
            result.PeersConnected = Values[(int)DiameterStatistic.PeersConnected];
            result.PeerConnectionsOpened = Values[(int)DiameterStatistic.PeerConnectionsOpened];
            result.PeerConnectionsClosed = Values[(int)DiameterStatistic.PeerConnectionsClosed];
            result.WatchdogsSent = Values[(int)DiameterStatistic.WatchdogsSent];
            result.WatchdogAnswersReceived = Values[(int)DiameterStatistic.WatchdogAnswersReceived];
            result.WatchdogTimeouts = Values[(int)DiameterStatistic.WatchdogTimeouts];
            result.RequestsReceived = Values[(int)DiameterStatistic.RequestsReceived];
            result.RequestsSent = Values[(int)DiameterStatistic.RequestsSent];
            result.AnswersReceived = Values[(int)DiameterStatistic.AnswersReceived];
            result.AnswersSent = Values[(int)DiameterStatistic.AnswersSent];
            result.RequestsPending = Values[(int)DiameterStatistic.RequestsPending];
            result.RequestTimeouts = Values[(int)DiameterStatistic.RequestTimeouts];
            result.Retransmissions = Values[(int)DiameterStatistic.Retransmissions];
            result.DuplicateRequests = Values[(int)DiameterStatistic.DuplicateRequests];
            result.RoutesAvailable = Values[(int)DiameterStatistic.RoutesAvailable];
            result.RouteSelections = Values[(int)DiameterStatistic.RouteSelections];
            result.RouteFailures = Values[(int)DiameterStatistic.RouteFailures];
            result.RedirectsReceived = Values[(int)DiameterStatistic.RedirectsReceived];
            result.RedirectsApplied = Values[(int)DiameterStatistic.RedirectsApplied];
            result.SessionsActive = Values[(int)DiameterStatistic.SessionsActive];
            result.SessionsStarted = Values[(int)DiameterStatistic.SessionsStarted];
            result.SessionsEnded = Values[(int)DiameterStatistic.SessionsEnded];
            result.AccountingRecordsSent = Values[(int)DiameterStatistic.AccountingRecordsSent];
            result.AccountingRecordsReceived = Values[(int)DiameterStatistic.AccountingRecordsReceived];
            result.AccountingFailures = Values[(int)DiameterStatistic.AccountingFailures];
            result.CreditControlRequests = Values[(int)DiameterStatistic.CreditControlRequests];
            result.CreditControlAnswers = Values[(int)DiameterStatistic.CreditControlAnswers];
            result.CreditControlFailures = Values[(int)DiameterStatistic.CreditControlFailures];
            result.LoadReportsSent = Values[(int)DiameterStatistic.LoadReportsSent];
            result.LoadReportsReceived = Values[(int)DiameterStatistic.LoadReportsReceived];
            result.OverloadReportsSent = Values[(int)DiameterStatistic.OverloadReportsSent];
            result.OverloadReportsReceived = Values[(int)DiameterStatistic.OverloadReportsReceived];
            result.OverloadRejections = Values[(int)DiameterStatistic.OverloadRejections];
            result.QueueDepth = Values[(int)DiameterStatistic.QueueDepth];
            result.QueueRejections = Values[(int)DiameterStatistic.QueueRejections];
            result.StoreReads = Values[(int)DiameterStatistic.StoreReads];
            result.StoreWrites = Values[(int)DiameterStatistic.StoreWrites];
            result.StoreFailures = Values[(int)DiameterStatistic.StoreFailures];
            return result;
        }
    }
}
