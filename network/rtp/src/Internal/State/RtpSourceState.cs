namespace rtp;

internal class RtpSourceState
{
    internal uint sourceIdentifier;
    internal RtpSourceStatus status;
    internal bool isSender;
    internal string canonicalName = "";
    internal string transportIdentity = "";
    internal byte payloadType;
    internal int clockRate;
    internal string mediaIdentity = "";
    internal int channelCount;
    internal long lastRtpActivityTicks;
    internal long lastRtcpActivityTicks;
    internal bool hasRtpActivity;
    internal bool hasRtcpActivity;
    internal RtpSequenceTracker sequenceTracker;
    internal RtpTimestampTracker timestampTracker = new RtpTimestampTracker();
    internal RtpReceptionStatistics receptionStatistics;
    internal RtpSequenceUpdate? sequenceUpdate;
    internal RtpTimestampUpdate? timestampUpdate;
    internal RtcpSourceDescriptionItem[] sourceDescriptionItems = [];
    internal byte[] goodbyeReason = [];
    internal string collisionTransportIdentity = "";
    internal RtcpExtendedReportBlock[] extendedReportBlocks = [];
    internal long lastExtendedReportTimeTicks;

    internal RtpSourceState(
        uint sourceIdentifier,
        RtpSourceStatus status,
        RtpSequenceTracker sequenceTracker)
    {
        this.sourceIdentifier = sourceIdentifier;
        this.status = status;
        this.sequenceTracker = sequenceTracker;
        receptionStatistics = new RtpReceptionStatistics(sourceIdentifier);
    }

    internal RtpSourceSnapshot CreateSourceSnapshot()
    {
        RtpSourceSnapshot snapshot = new RtpSourceSnapshot();
        snapshot.SourceIdentifier = sourceIdentifier;
        snapshot.Status = status;
        snapshot.IsSender = isSender;
        snapshot.CanonicalName = canonicalName;
        snapshot.TransportIdentity = transportIdentity;
        snapshot.PayloadType = payloadType;
        snapshot.ClockRate = clockRate;
        snapshot.MediaIdentity = mediaIdentity;
        snapshot.ChannelCount = channelCount;
        snapshot.LastRtpActivityTicks = lastRtpActivityTicks;
        snapshot.LastRtcpActivityTicks = lastRtcpActivityTicks;
        if (sequenceUpdate != null)
        {
            snapshot.SequenceUpdate = sequenceUpdate.Copy();
        }

        if (timestampUpdate != null)
        {
            snapshot.TimestampUpdate = timestampUpdate.Copy();
        }

        return snapshot;
    }

    internal RtpSourceSnapshot CreateSourceStatisticsSnapshot(
        long currentTimeTicks)
    {
        RtpSourceSnapshot snapshot = CreateSourceSnapshot();
        snapshot.ReceptionStatistics =
            receptionStatistics.GetReceptionStatisticsSnapshot(
                currentTimeTicks);
        return snapshot;
    }
}
