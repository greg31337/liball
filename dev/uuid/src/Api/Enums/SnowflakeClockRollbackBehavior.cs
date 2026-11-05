namespace uuid;

/// <summary>Controls how a Snowflake generator responds when its input clock moves backwards.</summary>
public enum SnowflakeClockRollbackBehavior
{
    /// <summary>Reject the request immediately to avoid emitting an out-of-order or duplicate identifier.</summary>
    FailFast = 0,

    /// <summary>Wait for the system clock to reach the last observed millisecond during real-time generation.</summary>
    /// <remarks>Explicit-timestamp generation remains deterministic and continues using the last observed millisecond without sleeping.</remarks>
    Wait = 1
}
