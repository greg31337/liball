namespace rtp;

/// <summary>
/// Contains one RTCP extended-report block.
/// </summary>
public class RtcpExtendedReportBlock
{
    /// <summary>The extended-report block type.</summary>
    public RtcpExtendedReportBlockType Type;

    /// <summary>The original eight-bit block type, including an unrecognized value.</summary>
    public byte WireType;

    /// <summary>The block's type-specific header byte.</summary>
    public byte TypeSpecific;

    /// <summary>The block length in 32-bit words minus one.</summary>
    public ushort BlockLengthInWordsMinusOne;

    /// <summary>The SSRC of the RTP source described by a source-specific block.</summary>
    public uint SourceIdentifier;

    /// <summary>The thinning exponent used by packet-by-packet report blocks.</summary>
    public byte Thinning;

    /// <summary>The first sequence number represented by a packet-by-packet block.</summary>
    public ushort BeginSequenceNumber;

    /// <summary>The sequence number following the final represented packet.</summary>
    public ushort EndSequenceNumber;

    /// <summary>The chunks of a loss or duplicate run-length report.</summary>
    public RtcpRunLengthChunk[] RunLengthChunks = [];

    /// <summary>The receipt timestamps of a packet-receipt-times block.</summary>
    public uint[] ReceiptTimes = [];

    /// <summary>The NTP timestamp of a receiver-reference-time block.</summary>
    public ulong ReceiverReferenceTime;

    /// <summary>The sub-blocks of a delay-since-last-receiver-report block.</summary>
    public RtcpDelaySinceLastReceiverReport[] DelayReports = [];

    /// <summary>Whether a statistics-summary block reports packet loss.</summary>
    public bool ReportsLoss;

    /// <summary>Whether a statistics-summary block reports duplicate packets.</summary>
    public bool ReportsDuplicates;

    /// <summary>Whether a statistics-summary block reports jitter.</summary>
    public bool ReportsJitter;

    /// <summary>The two-bit TTL or hop-limit reporting mode.</summary>
    public byte TimeToLiveOrHopLimitMode;

    /// <summary>The number of lost packets in a statistics-summary interval.</summary>
    public uint LostPackets;

    /// <summary>The number of duplicate packets in a statistics-summary interval.</summary>
    public uint DuplicatePackets;

    /// <summary>The minimum interarrival jitter in a statistics-summary interval.</summary>
    public uint MinimumJitter;

    /// <summary>The maximum interarrival jitter in a statistics-summary interval.</summary>
    public uint MaximumJitter;

    /// <summary>The mean interarrival jitter in a statistics-summary interval.</summary>
    public uint MeanJitter;

    /// <summary>The interarrival jitter standard deviation.</summary>
    public uint JitterDeviation;

    /// <summary>The minimum TTL or hop-limit value.</summary>
    public byte MinimumTimeToLiveOrHopLimit;

    /// <summary>The maximum TTL or hop-limit value.</summary>
    public byte MaximumTimeToLiveOrHopLimit;

    /// <summary>The mean TTL or hop-limit value.</summary>
    public byte MeanTimeToLiveOrHopLimit;

    /// <summary>The TTL or hop-limit standard deviation.</summary>
    public byte TimeToLiveOrHopLimitDeviation;

    /// <summary>The VoIP metrics packet loss rate.</summary>
    public byte LossRate;

    /// <summary>The VoIP metrics packet discard rate.</summary>
    public byte DiscardRate;

    /// <summary>The VoIP metrics burst density.</summary>
    public byte BurstDensity;

    /// <summary>The VoIP metrics gap density.</summary>
    public byte GapDensity;

    /// <summary>The VoIP metrics burst duration in milliseconds.</summary>
    public ushort BurstDuration;

    /// <summary>The VoIP metrics gap duration in milliseconds.</summary>
    public ushort GapDuration;

    /// <summary>The VoIP metrics round-trip delay in milliseconds.</summary>
    public ushort RoundTripDelay;

    /// <summary>The VoIP metrics end-system delay in milliseconds.</summary>
    public ushort EndSystemDelay;

    /// <summary>The VoIP metrics signal level.</summary>
    public byte SignalLevel;

    /// <summary>The VoIP metrics noise level.</summary>
    public byte NoiseLevel;

    /// <summary>The VoIP metrics residual echo return loss.</summary>
    public byte ResidualEchoReturnLoss;

    /// <summary>The VoIP metrics minimum gap threshold.</summary>
    public byte MinimumGapThreshold;

    /// <summary>The VoIP metrics R factor.</summary>
    public byte RFactor;

    /// <summary>The VoIP metrics external R factor.</summary>
    public byte ExternalRFactor;

    /// <summary>The VoIP metrics listening-quality mean opinion score.</summary>
    public byte MeanOpinionScoreListeningQuality;

    /// <summary>The VoIP metrics conversational-quality mean opinion score.</summary>
    public byte MeanOpinionScoreConversationalQuality;

    /// <summary>The VoIP metrics receiver configuration byte.</summary>
    public byte ReceiverConfiguration;

    /// <summary>The nominal jitter-buffer delay.</summary>
    public ushort JitterBufferNominalDelay;

    /// <summary>The maximum jitter-buffer delay.</summary>
    public ushort JitterBufferMaximumDelay;

    /// <summary>The absolute maximum jitter-buffer delay.</summary>
    public ushort JitterBufferAbsoluteMaximumDelay;

    /// <summary>The opaque content of an unrecognized or losslessly preserved block.</summary>
    public byte[] Data = [];

    internal RtcpExtendedReportBlock Copy()
    {
        RtcpExtendedReportBlock copy = new RtcpExtendedReportBlock();
        copy.Type = Type;
        copy.WireType = WireType;
        copy.TypeSpecific = TypeSpecific;
        copy.BlockLengthInWordsMinusOne = BlockLengthInWordsMinusOne;
        copy.SourceIdentifier = SourceIdentifier;
        copy.Thinning = Thinning;
        copy.BeginSequenceNumber = BeginSequenceNumber;
        copy.EndSequenceNumber = EndSequenceNumber;
        copy.RunLengthChunks = new RtcpRunLengthChunk[RunLengthChunks.Length];
        int chunkIndex = 0;
        while (chunkIndex < RunLengthChunks.Length)
        {
            copy.RunLengthChunks[chunkIndex] = RunLengthChunks[chunkIndex].Copy();
            chunkIndex = chunkIndex + 1;
        }

        copy.ReceiptTimes = (uint[])ReceiptTimes.Clone();
        copy.ReceiverReferenceTime = ReceiverReferenceTime;
        copy.DelayReports = new RtcpDelaySinceLastReceiverReport[DelayReports.Length];
        int delayIndex = 0;
        while (delayIndex < DelayReports.Length)
        {
            copy.DelayReports[delayIndex] = DelayReports[delayIndex].Copy();
            delayIndex = delayIndex + 1;
        }

        copy.ReportsLoss = ReportsLoss;
        copy.ReportsDuplicates = ReportsDuplicates;
        copy.ReportsJitter = ReportsJitter;
        copy.TimeToLiveOrHopLimitMode = TimeToLiveOrHopLimitMode;
        copy.LostPackets = LostPackets;
        copy.DuplicatePackets = DuplicatePackets;
        copy.MinimumJitter = MinimumJitter;
        copy.MaximumJitter = MaximumJitter;
        copy.MeanJitter = MeanJitter;
        copy.JitterDeviation = JitterDeviation;
        copy.MinimumTimeToLiveOrHopLimit = MinimumTimeToLiveOrHopLimit;
        copy.MaximumTimeToLiveOrHopLimit = MaximumTimeToLiveOrHopLimit;
        copy.MeanTimeToLiveOrHopLimit = MeanTimeToLiveOrHopLimit;
        copy.TimeToLiveOrHopLimitDeviation = TimeToLiveOrHopLimitDeviation;
        copy.LossRate = LossRate;
        copy.DiscardRate = DiscardRate;
        copy.BurstDensity = BurstDensity;
        copy.GapDensity = GapDensity;
        copy.BurstDuration = BurstDuration;
        copy.GapDuration = GapDuration;
        copy.RoundTripDelay = RoundTripDelay;
        copy.EndSystemDelay = EndSystemDelay;
        copy.SignalLevel = SignalLevel;
        copy.NoiseLevel = NoiseLevel;
        copy.ResidualEchoReturnLoss = ResidualEchoReturnLoss;
        copy.MinimumGapThreshold = MinimumGapThreshold;
        copy.RFactor = RFactor;
        copy.ExternalRFactor = ExternalRFactor;
        copy.MeanOpinionScoreListeningQuality = MeanOpinionScoreListeningQuality;
        copy.MeanOpinionScoreConversationalQuality = MeanOpinionScoreConversationalQuality;
        copy.ReceiverConfiguration = ReceiverConfiguration;
        copy.JitterBufferNominalDelay = JitterBufferNominalDelay;
        copy.JitterBufferMaximumDelay = JitterBufferMaximumDelay;
        copy.JitterBufferAbsoluteMaximumDelay = JitterBufferAbsoluteMaximumDelay;
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
