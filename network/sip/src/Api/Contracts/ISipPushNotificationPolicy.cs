namespace sip;

/// <summary>Requests delivery through an application-provided push service.</summary>
public interface ISipPushNotificationPolicy
{
    /// <summary>Requests a push notification for one dormant registration binding.</summary>
    /// <param name="request">The detached provider and binding metadata.</param>
    /// <returns>The application push decision.</returns>
    SipPushNotificationResult RequestPushNotification(
        SipPushNotificationRequest request);
}
