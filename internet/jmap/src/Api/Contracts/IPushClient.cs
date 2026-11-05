using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed push-subscription operations.
/// </summary>
public interface IPushClient
{
    /// <summary>Gets push subscriptions owned by the current credentials.</summary>
    /// <param name="request">The PushSubscription/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the PushSubscription/get result.</returns>
    Task<PushSubscriptionGetResponse> GetPushSubscriptionsAsync(PushSubscriptionGetRequest request, CancellationToken cancellationToken);

    /// <summary>Creates, updates, or destroys push subscriptions.</summary>
    /// <param name="request">The PushSubscription/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the PushSubscription/set result.</returns>
    Task<PushSubscriptionSetResponse> SetPushSubscriptionsAsync(PushSubscriptionSetRequest request, CancellationToken cancellationToken);

}
