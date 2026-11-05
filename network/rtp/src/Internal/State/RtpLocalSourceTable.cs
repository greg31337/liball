using System.Collections.Generic;

namespace rtp;

internal class RtpLocalSourceTable
{
    internal const int MaximumReselectionAttempts = 32;

    internal object syncRoot = new object();
    internal List<RtpLocalSourceState> sources =
        new List<RtpLocalSourceState>();
    internal IRtpRandomSource randomSource;
    internal int maximumSourceCount;
    internal string canonicalName;

    internal RtpLocalSourceTable(
        RtpSessionConfiguration configuration,
        IRtpRandomSource randomSource)
    {
        RtpConfigurationValidator.ValidateRtpSourceTableConfiguration(
            configuration);
        if (randomSource == null)
        {
            throw new RtpConfigurationException(
                "A local RTP source-table random source is required.");
        }

        if (configuration.CanonicalName == null)
        {
            throw new RtpConfigurationException(
                "A local RTP canonical name is required.");
        }

        this.randomSource = randomSource;
        maximumSourceCount = configuration.MaximumSourceCount;
        canonicalName = configuration.CanonicalName;
    }

    internal RtpLocalSourceState CreateLocalSourceState()
    {
        lock (syncRoot)
        {
            if (sources.Count >= maximumSourceCount)
            {
                throw new RtpBufferCapacityException(
                    "The local RTP source table capacity has been reached.");
            }

            RtpLocalSourceState source =
                new RtpLocalSourceState(randomSource);
            if (FindLocalSourceIndex(source.sourceIdentifier) >= 0)
            {
                throw new RtpSessionException(
                    "The generated local RTP source identifier is already in use.");
            }

            source.AssignLocalCanonicalName(canonicalName);
            InsertLocalSourceState(source);
            return source;
        }
    }

    internal bool RemoveLocalSourceState(uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            int sourceIndex = FindLocalSourceIndex(sourceIdentifier);
            if (sourceIndex < 0)
            {
                return false;
            }

            sources.RemoveAt(sourceIndex);
            return true;
        }
    }

    internal RtpLocalSourceState ReselectCollidingLocalSource(
        uint sourceIdentifier,
        uint[] forbiddenSourceIdentifiers)
    {
        if (forbiddenSourceIdentifiers == null)
        {
            throw new RtpConfigurationException(
                "The forbidden RTP source identifier array is required.");
        }

        lock (syncRoot)
        {
            int sourceIndex = FindLocalSourceIndex(sourceIdentifier);
            if (sourceIndex < 0)
            {
                throw new RtpSessionException(
                    "The colliding local RTP source is not known.");
            }

            uint replacementIdentifier = 0U;
            bool foundReplacement = false;
            int attemptCount = 0;
            while (!foundReplacement &&
                attemptCount < MaximumReselectionAttempts)
            {
                uint candidate = randomSource.CreateSynchronizationSource();
                if (FindLocalSourceIndex(candidate) < 0 &&
                    !ContainsForbiddenSourceIdentifier(
                        forbiddenSourceIdentifiers,
                        candidate))
                {
                    replacementIdentifier = candidate;
                    foundReplacement = true;
                }

                attemptCount = attemptCount + 1;
            }

            if (!foundReplacement)
            {
                throw new RtpSessionException(
                    "A unique local RTP source identifier could not be generated.");
            }

            ushort replacementSequenceNumber =
                randomSource.CreateSequenceNumber();
            uint replacementTimestamp = randomSource.CreateTimestamp();
            RtpLocalSourceState source = sources[sourceIndex];
            sources.RemoveAt(sourceIndex);
            source.ResetLocalSourceIdentity(
                replacementIdentifier,
                replacementSequenceNumber,
                replacementTimestamp);
            InsertLocalSourceState(source);
            return source;
        }
    }

    internal RtpLocalSourceState? FindLocalSourceState(
        uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            int sourceIndex = FindLocalSourceIndex(sourceIdentifier);
            if (sourceIndex < 0)
            {
                return null;
            }

            return sources[sourceIndex];
        }
    }

    internal RtpSourceSnapshot? FindLocalSourceSnapshot(
        uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            int sourceIndex = FindLocalSourceIndex(sourceIdentifier);
            if (sourceIndex < 0)
            {
                return null;
            }

            return sources[sourceIndex].CreateLocalSourceSnapshot();
        }
    }

    internal RtpSourceSnapshot[] GetLocalSourceSnapshots()
    {
        lock (syncRoot)
        {
            RtpSourceSnapshot[] snapshots =
                new RtpSourceSnapshot[sources.Count];
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                snapshots[sourceIndex] =
                    sources[sourceIndex].CreateLocalSourceSnapshot();
                sourceIndex = sourceIndex + 1;
            }

            return snapshots;
        }
    }

    internal uint[] GetLocalSourceIdentifiers()
    {
        lock (syncRoot)
        {
            uint[] sourceIdentifiers = new uint[sources.Count];
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                sourceIdentifiers[sourceIndex] =
                    sources[sourceIndex].sourceIdentifier;
                sourceIndex = sourceIndex + 1;
            }

            return sourceIdentifiers;
        }
    }

    internal void ClearLocalSourceStates()
    {
        lock (syncRoot)
        {
            sources.Clear();
        }
    }

    internal void InsertLocalSourceState(RtpLocalSourceState source)
    {
        int insertionIndex = 0;
        while (insertionIndex < sources.Count &&
            sources[insertionIndex].sourceIdentifier < source.sourceIdentifier)
        {
            insertionIndex = insertionIndex + 1;
        }

        sources.Insert(insertionIndex, source);
    }

    internal int FindLocalSourceIndex(uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (sources[sourceIndex].sourceIdentifier == sourceIdentifier)
            {
                return sourceIndex;
            }

            sourceIndex = sourceIndex + 1;
        }

        return -1;
    }

    internal bool ContainsForbiddenSourceIdentifier(
        uint[] sourceIdentifiers,
        uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sourceIdentifiers.Length)
        {
            if (sourceIdentifiers[sourceIndex] == sourceIdentifier)
            {
                return true;
            }

            sourceIndex = sourceIndex + 1;
        }

        return false;
    }
}
