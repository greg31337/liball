using System;
using System.Globalization;
using System.Security.Cryptography;

namespace diameter;

/// <summary>
/// Creates Diameter protocol and session identifiers from time and process-local sequences.
/// </summary>
public class DiameterIdentifierSource : IDiameterIdentifierSource
{
    internal IDiameterClock Clock;
    internal object IdentifierLock = new object();
    internal uint HopByHopSequence;
    internal uint EndToEndSequence;
    internal uint SessionHighSequence;
    internal uint SessionSequence;
    internal long StartupUnixSeconds;
    internal uint OriginStateIdentifier;

    /// <summary>Initializes an identifier source using the supplied clock.</summary>
    /// <param name="clock">The UTC and monotonic clock used to seed and create identifiers.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>clock</c> is <c>null</c>.</exception>
    public DiameterIdentifierSource(IDiameterClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentException("The identifier source requires a clock.");
        }

        Clock = clock;
        DateTimeOffset startupTime = Clock.ReadUtcTime();
        StartupUnixSeconds = ReadUnixSeconds(startupTime);
        uint timeSeed = FoldUnsigned(StartupUnixSeconds);
        uint monotonicSeed = FoldUnsigned(Clock.ReadMonotonicMilliseconds());
        byte[] startupEntropy = new byte[8];
        RandomNumberGenerator.Fill(startupEntropy);
        uint hopByHopEntropy = ReadEntropyWord(startupEntropy, 0);
        uint endToEndEntropy = ReadEntropyWord(startupEntropy, 4);
        HopByHopSequence = CreateHopByHopSeed(hopByHopEntropy, timeSeed, monotonicSeed);
        EndToEndSequence = CreateEndToEndSeed(endToEndEntropy, timeSeed, monotonicSeed);
        SessionHighSequence = CreateSessionHighSequence(StartupUnixSeconds);
        SessionSequence = monotonicSeed;
        OriginStateIdentifier = timeSeed;
    }

    /// <summary>
    /// Creates a process-local Hop-by-Hop Identifier.
    /// </summary>
    /// <returns>The next Hop-by-Hop Identifier.</returns>
    public uint CreateHopByHopIdentifier()
    {
        lock (IdentifierLock)
        {
            HopByHopSequence = AdvanceUnsigned(HopByHopSequence);
            return HopByHopSequence;
        }
    }

    /// <summary>
    /// Creates an End-to-End Identifier with RFC-compatible time and sequence components.
    /// </summary>
    /// <returns>The next End-to-End Identifier.</returns>
    public uint CreateEndToEndIdentifier()
    {
        lock (IdentifierLock)
        {
            EndToEndSequence = AdvanceUnsigned(EndToEndSequence);
            return EndToEndSequence;
        }
    }

    /// <summary>
    /// Creates a Session-Id from the origin identity, startup time, sequence, and optional correlation text.
    /// </summary>
    /// <param name="originHost">The Diameter identity that owns the session.</param>
    /// <param name="correlationText">Optional caller correlation text, or <c>null</c> to omit it.</param>
    /// <returns>The formatted Session-Id value.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the origin identity or correlation text is invalid.</exception>
    public string CreateSessionIdentifier(DiameterIdentity originHost, string? correlationText)
    {
        DiameterValueResult normalizedOrigin = ValueCodecs.NormalizeDiameterIdentity(originHost);
        if (normalizedOrigin.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A Session-Id requires a valid Origin-Host identity.");
        }

        if (correlationText != null && ValueCodecs.FindInvalidUtf16Index(correlationText) >= 0)
        {
            throw new ArgumentException("The Session-Id correlation text is not valid UTF-8 text.");
        }

        uint highSequence;
        uint sequence;
        lock (IdentifierLock)
        {
            if (SessionSequence == uint.MaxValue)
            {
                SessionHighSequence = AdvanceUnsigned(SessionHighSequence);
                SessionSequence = 0U;
            }
            else
            {
                SessionSequence = SessionSequence + 1U;
            }

            highSequence = SessionHighSequence;
            sequence = SessionSequence;
        }

        string highText = highSequence.ToString(CultureInfo.InvariantCulture);
        string sequenceText = sequence.ToString(CultureInfo.InvariantCulture);
        if (correlationText == null || correlationText.Length == 0)
        {
            return $"{normalizedOrigin.Text};{highText};{sequenceText}";
        }

        return $"{normalizedOrigin.Text};{highText};{sequenceText};{correlationText}";
    }

    /// <summary>
    /// Creates the stable Origin-State-Id for this source lifetime.
    /// </summary>
    /// <returns>The source startup time represented as an unsigned identifier.</returns>
    public uint CreateOriginStateIdentifier()
    {
        return OriginStateIdentifier;
    }

    internal static uint AdvanceUnsigned(uint value)
    {
        if (value == uint.MaxValue)
        {
            return 0U;
        }

        return value + 1U;
    }

    internal static uint CreateHopByHopSeed(uint entropy, uint timeSeed, uint monotonicSeed)
    {
        return entropy ^ timeSeed ^ monotonicSeed;
    }

    internal static uint CreateEndToEndSeed(uint entropy, uint timeSeed, uint monotonicSeed)
    {
        uint timeBits = timeSeed & 4095U;
        uint randomBits = (entropy ^ monotonicSeed) & 1048575U;
        return (timeBits << 20) | randomBits;
    }

    internal static uint CreateSessionHighSequence(long startupUnixSeconds)
    {
        long ntpSeconds = startupUnixSeconds + DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;
        return FoldUnsigned(ntpSeconds);
    }

    internal static uint ReadEntropyWord(byte[] entropy, int offset)
    {
        uint first = entropy[offset];
        uint second = entropy[offset + 1];
        uint third = entropy[offset + 2];
        uint fourth = entropy[offset + 3];
        return (first << 24) | (second << 16) | (third << 8) | fourth;
    }

    internal static uint FoldUnsigned(long value)
    {
        long folded = value % 4294967296L;
        if (folded < 0L)
        {
            folded = folded + 4294967296L;
        }

        return (uint)folded;
    }

    internal static long ReadUnixSeconds(DateTimeOffset value)
    {
        long seconds = value.ToUnixTimeSeconds();
        if (seconds < 0L)
        {
            return 0L;
        }

        return seconds;
    }
}
