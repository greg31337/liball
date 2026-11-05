using System;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class ServerStatePublisher : IJmapStatePublisher
{
    internal EventSourceHub Events;
    internal IJmapStatePublisher? ExternalPublisher;
    internal PushDeliveryManager PushDelivery;

    internal ServerStatePublisher(
        EventSourceHub events,
        IJmapStatePublisher? externalPublisher,
        PushDeliveryManager pushDelivery)
    {
        if (events == null || pushDelivery == null)
        {
            throw new ArgumentNullException("events");
        }

        Events = events;
        ExternalPublisher = externalPublisher;
        PushDelivery = pushDelivery;
    }

    /// <summary>Publishes a state change to EventSource, push delivery, and any caller-supplied publisher.</summary>
    /// <param name="publication">The state change to publish.</param>
    /// <param name="cancellationToken">A token that can cancel publication.</param>
    /// <returns>A task representing publication.</returns>
    public Task PublishStateChangeAsync(
        JmapStatePublication publication,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => PublishStateChange(publication, cancellationToken), cancellationToken);
    }

    internal void PublishStateChange(
        JmapStatePublication publication,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Publish(publication);
        PushDelivery.QueuePublication(publication);
        if (ExternalPublisher != null)
        {
            ExternalPublisher.PublishStateChangeAsync(publication, cancellationToken)
                .GetAwaiter().GetResult();
        }

        PushDelivery.FlushAsync(cancellationToken).GetAwaiter().GetResult();
    }
}
