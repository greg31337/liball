namespace fix;

/// <summary>Defines message-rate and queue bounds for one session.</summary>
public class SessionThrottleSettings
{
    /// <summary>The maximum messages accepted during one interval.</summary>
    public int MaxMessages = 1000;
    /// <summary>The accounting interval in milliseconds.</summary>
    public int IntervalMilliseconds = 1000;
    /// <summary>The maximum burst above the steady-state rate.</summary>
    public int BurstMessages = 100;
    /// <summary>The maximum pending application messages.</summary>
    public int QueueCapacity = 1024;
}
