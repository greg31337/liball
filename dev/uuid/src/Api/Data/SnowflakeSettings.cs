using System;

namespace uuid;

/// <summary>Defines the epoch, machine allocation, sequence allocation, and rollback policy for Snowflake IDs.</summary>
/// <remarks>The 63 usable bits are fixed as 41 timestamp bits plus 22 bits shared by the machine ID and sequence. Settings are copied when a generator or value is created.</remarks>
public class SnowflakeSettings
{
    /// <summary>The Unix-millisecond epoch used by Twitter Snowflake identifiers.</summary>
    public const long DefaultTwitterEpochUnixMilliseconds = 1288834974657;

    /// <summary>The fixed number of bits used for the timestamp offset.</summary>
    public const int TimestampBitCount = 41;

    /// <summary>The number of bits that must be divided between machine ID and sequence.</summary>
    public const int AvailableWorkerAndSequenceBitCount = 22;

    /// <summary>The custom epoch subtracted from each Unix-millisecond timestamp before packing.</summary>
    public long EpochUnixMilliseconds;

    /// <summary>The generator's machine or worker identifier.</summary>
    public long MachineId;

    /// <summary>The number of bits allocated to <see cref="MachineId"/>.</summary>
    public int MachineBitCount;

    /// <summary>The number of per-millisecond counter bits.</summary>
    public int SequenceBitCount;

    /// <summary>The action taken when generation observes a timestamp earlier than the last one used.</summary>
    public SnowflakeClockRollbackBehavior ClockRollbackBehavior;

    /// <summary>Creates and validates custom Snowflake settings.</summary>
    /// <param name="epochUnixMilliseconds">The custom epoch in Unix milliseconds.</param>
    /// <param name="machineId">The non-negative machine value that fits the allocated bits.</param>
    /// <param name="machineBitCount">Machine bits from zero through 22.</param>
    /// <param name="sequenceBitCount">Sequence bits from zero through 22; together with machine bits this must total 22.</param>
    /// <param name="clockRollbackBehavior">The clock rollback policy.</param>
    /// <exception cref="ArgumentException">The bit allocation, machine ID, or rollback behavior is invalid.</exception>
    public SnowflakeSettings(long epochUnixMilliseconds, long machineId, int machineBitCount, int sequenceBitCount, SnowflakeClockRollbackBehavior clockRollbackBehavior)
    {
        EpochUnixMilliseconds = epochUnixMilliseconds;
        MachineId = machineId;
        MachineBitCount = machineBitCount;
        SequenceBitCount = sequenceBitCount;
        ClockRollbackBehavior = clockRollbackBehavior;
        ThrowIfInvalid();
    }

    /// <summary>Creates a new mutable copy of the Twitter-compatible defaults.</summary>
    /// <returns>Settings with the Twitter epoch, 10 machine bits, 12 sequence bits, and machine ID zero.</returns>
    internal static SnowflakeSettings TwitterDefault()
    {
        return new SnowflakeSettings(DefaultTwitterEpochUnixMilliseconds, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
    }

    /// <summary>Calculates the largest machine ID allowed by the current allocation.</summary>
    /// <returns><c>2^MachineBitCount - 1</c>, or zero when no machine bits are allocated.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The machine bit count is outside zero through 22.</exception>
    internal long MaxMachineId()
    {
        return MaxValueForBitCount(MachineBitCount);
    }

    /// <summary>Calculates the largest sequence allowed in one millisecond.</summary>
    /// <returns><c>2^SequenceBitCount - 1</c>, or zero when no sequence bits are allocated.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sequence bit count is outside zero through 22.</exception>
    internal long MaxSequence()
    {
        return MaxValueForBitCount(SequenceBitCount);
    }

    /// <summary>Returns the largest millisecond offset representable by the 41 timestamp bits.</summary>
    /// <returns><c>2^41 - 1</c> milliseconds.</returns>
    internal long MaxTimestampOffsetMilliseconds()
    {
        return (1L << TimestampBitCount) - 1;
    }

    /// <summary>Checks that the bit counts, machine ID, and rollback behavior form usable settings.</summary>
    /// <returns>A successful result or a specific failure reason. The epoch itself may be any <c>long</c> value whose generated offsets remain in range.</returns>
    internal IdentifierValidationResult Validate()
    {
        if (MachineBitCount < 0 || MachineBitCount > AvailableWorkerAndSequenceBitCount)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidValue, "machine bit count must be between 0 and 22");
        }

        if (SequenceBitCount < 0 || SequenceBitCount > AvailableWorkerAndSequenceBitCount)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidValue, "sequence bit count must be between 0 and 22");
        }

        if (MachineBitCount + SequenceBitCount != AvailableWorkerAndSequenceBitCount)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidValue, "machine and sequence bit counts must total 22");
        }

        if (MachineId < 0 || MachineId > MaxMachineId())
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidValue, "machine ID is outside configured bit range");
        }

        if (ClockRollbackBehavior != SnowflakeClockRollbackBehavior.FailFast && ClockRollbackBehavior != SnowflakeClockRollbackBehavior.Wait)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidValue, "clock rollback behavior is not recognized");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Snowflake, "");
    }

    internal void ThrowIfInvalid()
    {
        IdentifierValidationResult validation = Validate();
        if (!validation.Ok)
        {
            throw new ArgumentException(validation.Error, "SnowflakeSettings");
        }
    }

    internal static long MaxValueForBitCount(int bitCount)
    {
        if (bitCount < 0 || bitCount > AvailableWorkerAndSequenceBitCount)
        {
            throw new ArgumentOutOfRangeException("bitCount");
        }

        if (bitCount == 0)
        {
            return 0;
        }

        return (1L << bitCount) - 1;
    }
}
