using System;
using System.Collections.Generic;

namespace rtp;

/// <summary>Coordinates synchronous RTP and RTCP session processing.</summary>
public class RtpSession : IRtpSession
{
    internal object syncRoot = new object();
    internal RtpSessionConfiguration configuration;
    internal IRtpClock clock;
    internal IRtpRandomSource randomSource;
    internal IRtpPayloadTypeRegistry payloadTypeRegistry;
    internal IRtpHeaderExtensionRegistry headerExtensionRegistry;
    internal IRtpPayloadPacketizer? payloadPacketizer;
    internal IRtpPayloadDepacketizer? payloadDepacketizer;
    internal IRtpRepairEncoder? repairEncoder;
    internal IRtpRepairDecoder? repairDecoder;
    internal RtpSeparatePortIngress ingress;
    internal RtpSessionPoller poller;
    internal RtpOutboundStream outboundStream;

    /// <summary>Initializes a transport-neutral session from injected services.</summary>
    /// <param name="configuration">The validated initial session configuration.</param>
    /// <param name="clock">The monotonic and wall clock.</param>
    /// <param name="randomSource">The RTP identifier and interval random source.</param>
    /// <param name="payloadTypeRegistry">The signaling-supplied payload mappings.</param>
    /// <param name="headerExtensionRegistry">The signaling-supplied extension mappings.</param>
    /// <param name="payloadPacketizer">The optional outbound payload packetizer.</param>
    /// <param name="payloadDepacketizer">The optional inbound payload depacketizer.</param>
    /// <param name="repairEncoder">The optional repair encoder.</param>
    /// <param name="repairDecoder">The optional repair decoder.</param>
    public RtpSession(
        RtpSessionConfiguration configuration,
        IRtpClock clock,
        IRtpRandomSource randomSource,
        IRtpPayloadTypeRegistry payloadTypeRegistry,
        IRtpHeaderExtensionRegistry headerExtensionRegistry,
        IRtpPayloadPacketizer? payloadPacketizer,
        IRtpPayloadDepacketizer? payloadDepacketizer,
        IRtpRepairEncoder? repairEncoder,
        IRtpRepairDecoder? repairDecoder)
    {
        RtpConfigurationValidator.ValidateSessionConfiguration(configuration);
        if (clock == null || randomSource == null ||
            payloadTypeRegistry == null || headerExtensionRegistry == null)
        {
            throw new RtpConfigurationException(
                "RTP session clock, randomness, and registries are required.");
        }

        this.clock = clock;
        this.randomSource = randomSource;
        this.payloadTypeRegistry = payloadTypeRegistry;
        this.headerExtensionRegistry = headerExtensionRegistry;
        this.payloadPacketizer = payloadPacketizer;
        this.payloadDepacketizer = payloadDepacketizer;
        this.repairEncoder = repairEncoder;
        this.repairDecoder = repairDecoder;
        this.configuration = configuration.Copy();
        ingress = new RtpSeparatePortIngress(
            this.configuration,
            randomSource,
            payloadTypeRegistry);
        ingress.ConfigureRepairHooks(repairEncoder, repairDecoder);
        if (payloadDepacketizer != null)
        {
            ingress.ConfigurePayloadDepacketizer(payloadDepacketizer);
        }

        poller = new RtpSessionPoller(
            this.configuration,
            clock,
            ingress);
        RtpLocalSourceState? localSource = ingress.localSourceTable
            .FindLocalSourceState(
                ingress.localSourceTable.GetLocalSourceIdentifiers()[0]);
        if (localSource == null)
        {
            throw new RtpSessionException(
                "The RTP session has no local source.");
        }

        outboundStream = new RtpOutboundStream(
            this.configuration,
            localSource,
            payloadTypeRegistry,
            headerExtensionRegistry,
            payloadPacketizer,
            clock);
    }

    /// <summary>Processes one datagram received on a dedicated RTP transport.</summary>
    /// <param name="datagram">The received RTP datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    public RtpSessionUpdate ProcessInboundRtpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        RtpSeparatePortIngress currentIngress;
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            currentIngress = ingress;
        }

        return currentIngress.ProcessSeparateRtpDatagram(datagram, context);
    }

    /// <summary>Processes one datagram received on a dedicated RTCP transport.</summary>
    /// <param name="datagram">The received RTCP datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    public RtpSessionUpdate ProcessInboundRtcpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        RtpSeparatePortIngress currentIngress;
        RtpSessionPoller currentPoller;
        lock (syncRoot)
        {
            EnsureSessionAcceptsControl();
            currentIngress = ingress;
            currentPoller = poller;
        }

        RtpSessionUpdate update = currentIngress.ProcessSeparateRtcpDatagram(
            datagram,
            context);
        if (!HasRecoverableError(update))
        {
            currentPoller.RecordReceivedControlPacketSize(datagram.Length);
        }

        return update;
    }

    /// <summary>Classifies and processes one RFC 5761 multiplexed datagram.</summary>
    /// <param name="datagram">The received multiplexed datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    public RtpSessionUpdate ProcessInboundMultiplexedDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        RtpSeparatePortIngress currentIngress;
        RtpSessionPoller currentPoller;
        lock (syncRoot)
        {
            EnsureSessionAcceptsControl();
            currentIngress = ingress;
            currentPoller = poller;
        }

        RtpSessionUpdate update = currentIngress.ProcessMultiplexedDatagram(
            datagram,
            context);
        if (HasPacketKind(update, RtpPacketKind.Rtcp) &&
            !HasRecoverableError(update))
        {
            currentPoller.RecordReceivedControlPacketSize(datagram.Length);
        }

        return update;
    }

    /// <summary>Prepares one application RTP packet using local source state.</summary>
    /// <param name="packet">The packet template and owned media payload.</param>
    /// <param name="context">The send-time and media-clock context.</param>
    /// <returns>The prepared packet and resulting session events.</returns>
    public RtpSessionUpdate PrepareOutboundRtpPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        RtpOutboundStream currentOutboundStream;
        bool shouldPacketize;
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            currentOutboundStream = outboundStream;
            shouldPacketize = payloadPacketizer != null;
        }

        if (shouldPacketize)
        {
            return currentOutboundStream.PacketizeOutboundRtpPayload(
                packet,
                context);
        }

        return currentOutboundStream.PrepareOutboundRtpPacket(
            packet,
            context);
    }

    /// <summary>Queues one feedback request for deterministic scheduling.</summary>
    /// <param name="request">The feedback request.</param>
    /// <returns>The retained request and current snapshot.</returns>
    public RtpSessionUpdate RequestSessionFeedback(
        RtpFeedbackRequest request)
    {
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            poller.QueueSessionFeedbackRequest(request);
            RtpSessionUpdate update = new RtpSessionUpdate();
            update.FeedbackRequests = new RtpFeedbackRequest[]
            {
                request.Copy()
            };
            update.Snapshot = poller.GetSessionSnapshot();
            return update;
        }
    }

    /// <summary>Applies packets and metadata returned by a repair mechanism.</summary>
    /// <param name="result">The repair result to process.</param>
    /// <param name="context">The repaired-packet timing and transport context.</param>
    /// <returns>The resulting packet releases and session events.</returns>
    public RtpSessionUpdate ApplySessionRepairResult(
        RtpRepairResult result,
        RtpPacketContext context)
    {
        RtpSeparatePortIngress currentIngress;
        RtpSessionPoller currentPoller;
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            currentIngress = ingress;
            currentPoller = poller;
        }

        if (result == null)
        {
            throw new RtpConfigurationException(
                "An RTP repair result is required.");
        }

        if (context == null)
        {
            throw new RtpConfigurationException(
                "An RTP repaired-packet context is required.");
        }

        RtpRepairResult retainedResult = currentIngress.repairCoordinator
            .CopyValidatedResult(result, result.Kind);
        if (!currentIngress.CanRetainRtpPacketSources(
            retainedResult.RepairedPackets))
        {
            RtpSessionUpdate capacityUpdate = currentIngress
                .CreateRemoteSourceCapacityIngressUpdate(
                    RtpPacketKind.Rtp,
                    context);
            capacityUpdate.RepairResults = new RtpRepairResult[]
            {
                retainedResult.Copy()
            };
            capacityUpdate.Snapshot = currentPoller.GetSessionSnapshot();
            return capacityUpdate;
        }

        List<RtpSessionEvent> events = new List<RtpSessionEvent>();
        List<RtpPacket> acceptedPackets = new List<RtpPacket>();
        List<RtpPacket> releasedPackets = new List<RtpPacket>();
        List<RtpFeedbackRequest> feedbackRequests =
            new List<RtpFeedbackRequest>();
        List<RtpRepairRequest> repairRequests =
            new List<RtpRepairRequest>();
        byte[][] repairedDatagrams =
            new byte[retainedResult.RepairedPackets.Length][];
        int packetIndex = 0;
        while (packetIndex < retainedResult.RepairedPackets.Length)
        {
            repairedDatagrams[packetIndex] = currentIngress.rtpPacketCodec
                .EncodeRtpPacketToArray(
                    retainedResult.RepairedPackets[packetIndex]);
            packetIndex = packetIndex + 1;
        }

        packetIndex = 0;
        while (packetIndex < retainedResult.RepairedPackets.Length)
        {
            RtpPacketContext repairedContext = context.Copy();
            repairedContext.IsRepaired = true;
            RtpSessionUpdate packetUpdate = currentIngress
                .ProcessSeparateRtpDatagram(
                    repairedDatagrams[packetIndex],
                    repairedContext);
            AppendSessionUpdate(
                packetUpdate,
                events,
                acceptedPackets,
                releasedPackets,
                feedbackRequests,
                repairRequests);
            packetIndex = packetIndex + 1;
        }

        events.Add(CreateRepairResultEvent(retainedResult, context));
        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        update.AcceptedPackets = acceptedPackets.ToArray();
        update.ReleasedPackets = releasedPackets.ToArray();
        update.FeedbackRequests = feedbackRequests.ToArray();
        update.RepairRequests = repairRequests.ToArray();
        update.RepairResults = new RtpRepairResult[]
        {
            retainedResult.Copy()
        };
        update.Snapshot = currentPoller.GetSessionSnapshot();
        return update;
    }

    /// <summary>Requests local goodbye processing and RTCP scheduling.</summary>
    /// <param name="reason">The optional UTF-8 goodbye reason bytes.</param>
    /// <returns>The resulting goodbye scheduling work.</returns>
    public RtpSessionUpdate RequestSessionGoodbye(
        ReadOnlySpan<byte> reason)
    {
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            return poller.RequestSessionGoodbye(reason);
        }
    }

    /// <summary>Flushes currently releasable buffered media and queued work.</summary>
    /// <returns>The released packets and queued session work.</returns>
    public RtpSessionUpdate FlushSession()
    {
        lock (syncRoot)
        {
            return poller.FlushSession();
        }
    }

    /// <summary>Atomically replaces configuration and resets dependent state.</summary>
    /// <param name="configuration">The replacement session configuration.</param>
    /// <returns>The configuration transition event and new snapshot.</returns>
    public RtpSessionUpdate UpdateSessionConfiguration(
        RtpSessionConfiguration configuration)
    {
        lock (syncRoot)
        {
            EnsureSessionAcceptsMedia();
            ReplaceSessionComponents(configuration);
            RtpSessionEvent sessionEvent = new RtpSessionEvent();
            sessionEvent.Type = RtpSessionEventType.Source;
            sessionEvent.Message =
                "The RTP session configuration was replaced.";
            sessionEvent.Configuration = this.configuration.Copy();
            RtpSessionUpdate update = new RtpSessionUpdate();
            update.Events = new RtpSessionEvent[] { sessionEvent };
            update.Snapshot = poller.GetSessionSnapshot();
            return update;
        }
    }

    /// <summary>Polls the session for pending events and control work.</summary>
    /// <returns>The update produced by the poll.</returns>
    public RtpSessionUpdate PollSession()
    {
        lock (syncRoot)
        {
            return poller.PollSession();
        }
    }

    /// <summary>Returns an independent snapshot of current session state.</summary>
    /// <returns>The current session snapshot.</returns>
    public RtpSessionSnapshot GetSessionSnapshot()
    {
        lock (syncRoot)
        {
            return poller.GetSessionSnapshot();
        }
    }

    /// <summary>Resets all source, scheduling, buffering, and repair state.</summary>
    public void ResetSession()
    {
        lock (syncRoot)
        {
            ReplaceSessionComponents(configuration);
        }
    }

    internal void ReplaceSessionComponents(
        RtpSessionConfiguration replacementConfiguration)
    {
        RtpConfigurationValidator.ValidateSessionConfiguration(
            replacementConfiguration);
        RtpSessionConfiguration retainedConfiguration =
            replacementConfiguration.Copy();
        RtpSeparatePortIngress replacementIngress =
            new RtpSeparatePortIngress(
                retainedConfiguration,
                randomSource,
                payloadTypeRegistry);
        replacementIngress.ConfigureRepairHooks(
            repairEncoder,
            repairDecoder);
        if (payloadDepacketizer != null)
        {
            replacementIngress.ConfigurePayloadDepacketizer(
                payloadDepacketizer);
        }

        RtpSessionPoller replacementPoller = new RtpSessionPoller(
            retainedConfiguration,
            clock,
            replacementIngress);
        RtpLocalSourceState? replacementSource = replacementIngress
            .localSourceTable.FindLocalSourceState(
                replacementIngress.localSourceTable
                    .GetLocalSourceIdentifiers()[0]);
        if (replacementSource == null)
        {
            throw new RtpSessionException(
                "The replacement RTP session has no local source.");
        }

        RtpOutboundStream replacementOutbound = new RtpOutboundStream(
            retainedConfiguration,
            replacementSource,
            payloadTypeRegistry,
            headerExtensionRegistry,
            payloadPacketizer,
            clock);

        configuration = retainedConfiguration;
        ingress = replacementIngress;
        poller = replacementPoller;
        outboundStream = replacementOutbound;
    }

    internal void AppendSessionUpdate(
        RtpSessionUpdate update,
        List<RtpSessionEvent> events,
        List<RtpPacket> acceptedPackets,
        List<RtpPacket> releasedPackets,
        List<RtpFeedbackRequest> feedbackRequests,
        List<RtpRepairRequest> repairRequests)
    {
        int eventIndex = 0;
        while (eventIndex < update.Events.Length)
        {
            events.Add(update.Events[eventIndex].Copy());
            eventIndex = eventIndex + 1;
        }

        int acceptedIndex = 0;
        while (acceptedIndex < update.AcceptedPackets.Length)
        {
            acceptedPackets.Add(update.AcceptedPackets[acceptedIndex].Copy());
            acceptedIndex = acceptedIndex + 1;
        }

        int releasedIndex = 0;
        while (releasedIndex < update.ReleasedPackets.Length)
        {
            releasedPackets.Add(update.ReleasedPackets[releasedIndex].Copy());
            releasedIndex = releasedIndex + 1;
        }

        int feedbackIndex = 0;
        while (feedbackIndex < update.FeedbackRequests.Length)
        {
            feedbackRequests.Add(update.FeedbackRequests[feedbackIndex].Copy());
            feedbackIndex = feedbackIndex + 1;
        }

        int repairIndex = 0;
        while (repairIndex < update.RepairRequests.Length)
        {
            repairRequests.Add(update.RepairRequests[repairIndex].Copy());
            repairIndex = repairIndex + 1;
        }
    }

    internal RtpSessionEvent CreateRepairResultEvent(
        RtpRepairResult result,
        RtpPacketContext context)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.RepairKind = result.Kind;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.RepairResult = result.Copy();
        if (result.Success)
        {
            sessionEvent.Message = "The RTP repair result was applied.";
        }
        else if (result.ErrorMessage != null)
        {
            sessionEvent.Message = result.ErrorMessage;
        }
        else
        {
            sessionEvent.Message = "The RTP repair operation did not succeed.";
        }

        return sessionEvent;
    }

    internal bool HasRecoverableError(RtpSessionUpdate update)
    {
        int eventIndex = 0;
        while (eventIndex < update.Events.Length)
        {
            if (update.Events[eventIndex].Type ==
                RtpSessionEventType.RecoverableError)
            {
                return true;
            }

            eventIndex = eventIndex + 1;
        }

        return false;
    }

    internal bool HasPacketKind(
        RtpSessionUpdate update,
        RtpPacketKind packetKind)
    {
        int eventIndex = 0;
        while (eventIndex < update.Events.Length)
        {
            if (update.Events[eventIndex].PacketKind == packetKind)
            {
                return true;
            }

            eventIndex = eventIndex + 1;
        }

        return false;
    }

    internal void EnsureSessionAcceptsMedia()
    {
        if (poller.goodbyeRequested || poller.goodbyeCompleted)
        {
            throw new RtpSessionException(
                "The RTP session is closing or has completed goodbye.");
        }
    }

    internal void EnsureSessionAcceptsControl()
    {
        if (poller.goodbyeCompleted)
        {
            throw new RtpSessionException(
                "The RTP session has completed goodbye.");
        }
    }
}
