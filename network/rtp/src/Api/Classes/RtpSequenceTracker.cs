namespace rtp;

/// <summary>
/// Tracks RTP receive-sequence validation, extension, and sequence advancement.
/// </summary>
public class RtpSequenceTracker : IRtpSequenceTracker
{
    internal object syncRoot = new object();
    internal int minimumSequential;
    internal int maximumDropout;
    internal int maximumMisorder;
    internal int probationRemaining;
    internal bool hasReceiveSequence;
    internal bool receiveSequenceValidated;
    internal bool hasBadSequence;
    internal ushort maximumReceiveSequence;
    internal ushort badSequence;
    internal ushort nextOutboundSequence;
    internal uint sequenceCycles;
    internal uint baseExtendedSequenceNumber;
    internal uint highestExtendedSequenceNumber;
    internal uint[] observedExtendedSequences;
    internal bool[] observedSequenceSlots;
    internal RtpSequenceUpdate currentUpdate = new RtpSequenceUpdate();

    /// <summary>
    /// Initializes an RTP sequence tracker using an independent configuration snapshot.
    /// </summary>
    /// <param name="configuration">The receive-sequence validation settings.</param>
    /// <param name="randomSource">The source of the initial outbound sequence number.</param>
    public RtpSequenceTracker(
        RtpSessionConfiguration configuration,
        IRtpRandomSource randomSource)
    {
        RtpConfigurationValidator.ValidateRtpSequenceTrackerConfiguration(configuration);
        if (randomSource == null)
        {
            throw new RtpConfigurationException("An RTP random source is required.");
        }

        minimumSequential = configuration.MinimumSequential;
        maximumDropout = configuration.JitterBuffer.MaximumDropout;
        maximumMisorder = configuration.JitterBuffer.MaximumMisorder;
        observedExtendedSequences = new uint[maximumMisorder + 1];
        observedSequenceSlots = new bool[maximumMisorder + 1];
        probationRemaining = minimumSequential;
        nextOutboundSequence = randomSource.CreateSequenceNumber();
        currentUpdate.Status = RtpSequenceStatus.Probation;
        currentUpdate.ProbationRemaining = probationRemaining;
    }

    /// <summary>Processes one received RTP sequence number.</summary>
    /// <param name="sequenceNumber">The 16-bit sequence number to process.</param>
    /// <returns>The resulting sequence state update.</returns>
    public RtpSequenceUpdate ProcessRtpSequenceNumber(ushort sequenceNumber)
    {
        lock (syncRoot)
        {
            if (!receiveSequenceValidated)
            {
                if (!hasReceiveSequence)
                {
                    hasReceiveSequence = true;
                    maximumReceiveSequence = sequenceNumber;
                    probationRemaining = minimumSequential - 1;
                }
                else if (IsNextSequenceNumber(sequenceNumber))
                {
                    maximumReceiveSequence = sequenceNumber;
                    probationRemaining = probationRemaining - 1;
                }
                else
                {
                    maximumReceiveSequence = sequenceNumber;
                    probationRemaining = minimumSequential - 1;
                }

                if (probationRemaining == 0)
                {
                    InitializeValidatedReceiveSequence(sequenceNumber);
                    currentUpdate = CreateSequenceUpdate(
                        RtpSequenceStatus.Accepted,
                        sequenceNumber,
                        sequenceNumber,
                        true);
                }
                else
                {
                    currentUpdate = CreateProbationUpdate(sequenceNumber);
                }

                return currentUpdate.Copy();
            }

            int unsignedDelta = sequenceNumber - maximumReceiveSequence;
            if (unsignedDelta < 0)
            {
                unsignedDelta = unsignedDelta + RtpProtocolConstants.SequenceNumberModulus;
            }

            if (unsignedDelta < maximumDropout)
            {
                uint extendedSequenceNumber = sequenceCycles + sequenceNumber;
                if (unsignedDelta == 0)
                {
                    currentUpdate = CreateSequenceUpdate(
                        RtpSequenceStatus.Duplicate,
                        sequenceNumber,
                        extendedSequenceNumber,
                        false);
                    return currentUpdate.Copy();
                }

                if (sequenceNumber < maximumReceiveSequence)
                {
                    IncrementSequenceCycles();
                    extendedSequenceNumber = sequenceCycles + sequenceNumber;
                }

                maximumReceiveSequence = sequenceNumber;
                highestExtendedSequenceNumber = extendedSequenceNumber;
                MarkObservedSequence(extendedSequenceNumber);
                currentUpdate = CreateSequenceUpdate(
                    RtpSequenceStatus.Accepted,
                    sequenceNumber,
                    extendedSequenceNumber,
                    true);
                return currentUpdate.Copy();
            }

            if (unsignedDelta <=
                RtpProtocolConstants.SequenceNumberModulus - maximumMisorder)
            {
                if (hasBadSequence && sequenceNumber == badSequence)
                {
                    InitializeValidatedReceiveSequence(sequenceNumber);
                    currentUpdate = CreateSequenceUpdate(
                        RtpSequenceStatus.Restarted,
                        sequenceNumber,
                        sequenceNumber,
                        true);
                    return currentUpdate.Copy();
                }

                hasBadSequence = true;
                if (sequenceNumber == ushort.MaxValue)
                {
                    badSequence = 0;
                }
                else
                {
                    badSequence = (ushort)(sequenceNumber + 1);
                }

                currentUpdate = CreateSequenceUpdate(
                    RtpSequenceStatus.LargeJump,
                    sequenceNumber,
                    sequenceCycles + sequenceNumber,
                    false);
                return currentUpdate.Copy();
            }

            uint reorderedExtendedSequenceNumber =
                GetReorderedExtendedSequenceNumber(sequenceNumber);
            if (IsObservedSequence(reorderedExtendedSequenceNumber))
            {
                currentUpdate = CreateSequenceUpdate(
                    RtpSequenceStatus.Duplicate,
                    sequenceNumber,
                    reorderedExtendedSequenceNumber,
                    false);
                return currentUpdate.Copy();
            }

            MarkObservedSequence(reorderedExtendedSequenceNumber);
            currentUpdate = CreateSequenceUpdate(
                RtpSequenceStatus.Reordered,
                sequenceNumber,
                reorderedExtendedSequenceNumber,
                true);
            return currentUpdate.Copy();
        }
    }

    /// <summary>Advances and returns the next outbound RTP sequence number.</summary>
    /// <returns>The next 16-bit sequence number.</returns>
    public ushort AdvanceRtpSequenceNumber()
    {
        lock (syncRoot)
        {
            ushort sequenceNumber = nextOutboundSequence;
            if (nextOutboundSequence == ushort.MaxValue)
            {
                nextOutboundSequence = 0;
            }
            else
            {
                nextOutboundSequence = (ushort)(nextOutboundSequence + 1);
            }

            return sequenceNumber;
        }
    }

    /// <summary>Returns an independent snapshot of the current sequence state.</summary>
    /// <returns>The current sequence state.</returns>
    public RtpSequenceUpdate GetRtpSequenceState()
    {
        lock (syncRoot)
        {
            return currentUpdate.Copy();
        }
    }

    /// <summary>Restarts receive and outbound sequence tracking at a supplied value.</summary>
    /// <param name="sequenceNumber">The receive candidate and next outbound value.</param>
    /// <returns>The reset sequence state.</returns>
    public RtpSequenceUpdate ResetRtpSequenceTracker(ushort sequenceNumber)
    {
        lock (syncRoot)
        {
            hasReceiveSequence = true;
            receiveSequenceValidated = false;
            maximumReceiveSequence = sequenceNumber;
            nextOutboundSequence = sequenceNumber;
            probationRemaining = minimumSequential - 1;
            if (probationRemaining == 0)
            {
                InitializeValidatedReceiveSequence(sequenceNumber);
                currentUpdate = CreateSequenceUpdate(
                    RtpSequenceStatus.Accepted,
                    sequenceNumber,
                    sequenceNumber,
                    true);
            }
            else
            {
                sequenceCycles = 0;
                baseExtendedSequenceNumber = 0;
                highestExtendedSequenceNumber = 0;
                hasBadSequence = false;
                ResetObservedSequences();
                currentUpdate = CreateProbationUpdate(sequenceNumber);
            }

            return currentUpdate.Copy();
        }
    }

    internal bool IsNextSequenceNumber(ushort sequenceNumber)
    {
        if (maximumReceiveSequence == ushort.MaxValue)
        {
            return sequenceNumber == 0;
        }

        return sequenceNumber == maximumReceiveSequence + 1;
    }

    internal RtpSequenceUpdate CreateProbationUpdate(ushort sequenceNumber)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = RtpSequenceStatus.Probation;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = sequenceNumber;
        update.ProbationRemaining = probationRemaining;
        return update;
    }

    internal RtpSequenceUpdate CreateSequenceUpdate(
        RtpSequenceStatus status,
        ushort sequenceNumber,
        uint extendedSequenceNumber,
        bool accepted)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = status;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = extendedSequenceNumber;
        update.BaseExtendedSequenceNumber = baseExtendedSequenceNumber;
        update.HighestExtendedSequenceNumber = highestExtendedSequenceNumber;
        update.ProbationRemaining = 0;
        update.Accepted = accepted;
        return update;
    }

    internal void InitializeValidatedReceiveSequence(ushort sequenceNumber)
    {
        receiveSequenceValidated = true;
        maximumReceiveSequence = sequenceNumber;
        sequenceCycles = 0;
        baseExtendedSequenceNumber = sequenceNumber;
        highestExtendedSequenceNumber = sequenceNumber;
        probationRemaining = 0;
        hasBadSequence = false;
        ResetObservedSequences();
        MarkObservedSequence(sequenceNumber);
    }

    internal void IncrementSequenceCycles()
    {
        if (sequenceCycles == 4294901760U)
        {
            sequenceCycles = 0;
        }
        else
        {
            sequenceCycles = sequenceCycles +
                (uint)RtpProtocolConstants.SequenceNumberModulus;
        }
    }

    internal uint GetReorderedExtendedSequenceNumber(ushort sequenceNumber)
    {
        if (sequenceNumber > maximumReceiveSequence &&
            sequenceCycles >= (uint)RtpProtocolConstants.SequenceNumberModulus)
        {
            return sequenceCycles -
                (uint)RtpProtocolConstants.SequenceNumberModulus + sequenceNumber;
        }

        return sequenceCycles + sequenceNumber;
    }

    internal bool IsObservedSequence(uint extendedSequenceNumber)
    {
        int slot = (int)(extendedSequenceNumber % (uint)observedExtendedSequences.Length);
        return observedSequenceSlots[slot] &&
            observedExtendedSequences[slot] == extendedSequenceNumber;
    }

    internal void MarkObservedSequence(uint extendedSequenceNumber)
    {
        int slot = (int)(extendedSequenceNumber % (uint)observedExtendedSequences.Length);
        observedExtendedSequences[slot] = extendedSequenceNumber;
        observedSequenceSlots[slot] = true;
    }

    internal void ResetObservedSequences()
    {
        int slot = 0;
        while (slot < observedSequenceSlots.Length)
        {
            observedSequenceSlots[slot] = false;
            slot = slot + 1;
        }
    }
}
