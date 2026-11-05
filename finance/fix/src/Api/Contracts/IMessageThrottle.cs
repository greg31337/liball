namespace fix;

/// <summary>Applies message-rate and backpressure policy.</summary>
public interface IMessageThrottle
{
    /// <summary>Checks whether a message may proceed now.</summary>
    /// <param name="context">The message context.</param>
    /// <param name="settings">The throttle settings.</param>
    /// <returns>The throttle disposition.</returns>
    ApplicationDecision CheckMessageThrottle(ApplicationMessageContext context, SessionThrottleSettings settings);
}
