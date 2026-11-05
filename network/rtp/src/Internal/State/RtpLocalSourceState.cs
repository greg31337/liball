namespace rtp;

internal class RtpLocalSourceState
{
    internal object syncRoot = new object();
    internal uint sourceIdentifier;
    internal ushort nextSequenceNumber;
    internal uint initialTimestamp;
    internal bool hasMediaClockOrigin;
    internal long mediaClockOrigin;
    internal long lastMediaClockTimestamp;
    internal int clockRate;
    internal uint lastTimestamp;
    internal bool isSender;
    internal string canonicalName = "";
    internal byte payloadType;
    internal string mediaIdentity = "";
    internal int channelCount;
    internal long lastRtpActivityTicks;
    internal string transportIdentity = "";
    internal RtpTransmissionStatistics? transmissionStatistics;
    internal RtpPacketHistory? packetHistory;

    internal RtpLocalSourceState(IRtpRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new RtpConfigurationException("An RTP random source is required.");
        }

        sourceIdentifier = randomSource.CreateSynchronizationSource();
        nextSequenceNumber = randomSource.CreateSequenceNumber();
        initialTimestamp = randomSource.CreateTimestamp();
        lastTimestamp = initialTimestamp;
    }

    internal void AssignLocalCanonicalName(string canonicalName)
    {
        this.canonicalName = canonicalName;
    }

    internal void ResetLocalSourceIdentity(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp)
    {
        lock (syncRoot)
        {
            this.sourceIdentifier = sourceIdentifier;
            nextSequenceNumber = sequenceNumber;
            initialTimestamp = timestamp;
            hasMediaClockOrigin = false;
            mediaClockOrigin = 0L;
            lastMediaClockTimestamp = 0L;
            clockRate = 0;
            lastTimestamp = initialTimestamp;
            isSender = false;
            payloadType = 0;
            mediaIdentity = "";
            channelCount = 0;
            lastRtpActivityTicks = 0L;
            transportIdentity = "";
            transmissionStatistics = null;
            packetHistory = null;
        }
    }

    internal RtpSourceSnapshot CreateLocalSourceSnapshot()
    {
        lock (syncRoot)
        {
            RtpSourceSnapshot snapshot = new RtpSourceSnapshot();
            snapshot.SourceIdentifier = sourceIdentifier;
            snapshot.Status = RtpSourceStatus.ActiveMember;
            snapshot.IsLocal = true;
            snapshot.IsSender = isSender;
            if (isSender)
            {
                snapshot.Status = RtpSourceStatus.ActiveSender;
            }

            snapshot.CanonicalName = canonicalName;
            snapshot.TransportIdentity = transportIdentity;
            snapshot.PayloadType = payloadType;
            snapshot.ClockRate = clockRate;
            snapshot.MediaIdentity = mediaIdentity;
            snapshot.ChannelCount = channelCount;
            snapshot.LastRtpActivityTicks = lastRtpActivityTicks;
            if (transmissionStatistics != null)
            {
                snapshot.TransmissionStatistics = transmissionStatistics
                    .GetTransmissionStatisticsSnapshot();
                snapshot.RemoteReports = transmissionStatistics
                    .GetRemoteReportSnapshots();
            }

            return snapshot;
        }
    }
}
